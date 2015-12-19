using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading;
using Flex.Smoothlake.FlexLib;
using Cat.Cat.Ports;
using log4net;

namespace Cat.CatMain.Ports
{
	public class WinKeyer2
	{
		#region Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private ObjectManager _om;
		private Radio this_radio;
		private CWX cwx;
		private CatSerialPort port;

		private ConcurrentQueue<byte> byte_queue = new ConcurrentQueue<byte>();
		private ConcurrentQueue<string> cw_queue = new ConcurrentQueue<string>();

		private byte[] buffer_ready = new byte[1] { Convert.ToByte('\xC0') };	// N1MM buffer ready
		private byte[] stop_cw = new byte[1] { Convert.ToByte('\xC2') };		// N1NN
		private byte[] buffer_busy = new byte[1] { Convert.ToByte('\xC4') };	//N1MM buffer busy
		private byte[] version = new byte[1] { Convert.ToByte('\x0A') };		//Winkeyer version 1.0

		private byte param1, param2, param3;
		private bool p1_filled, p2_filled, p3_filled;
		private int bytes_available = 0;
		private byte current_byte;
		private byte cmd_prefix;
		private bool data_in_sync = false;
		private bool sending = false;

		#endregion Variables

		#region Constructor

		public WinKeyer2(CatSerialPort port)
		{
			_om = ObjectManager.Instance;
			//this_radio = _om.StateManager.CSMRadio;
			this_radio = Cat.CatStateManager.CSMRadio;
			cwx = this_radio.GetCWX();
			this.cwx.CharSent += new CWX.CharSentEventHandler(cwx_CharSent);
			this.port = port;
		}

		#endregion Constructor

		#region Methods

		public void ProcessCommand(byte[] in_bytes)
		{
			foreach (byte b in in_bytes)
			{
				byte_queue.Enqueue(b);
				Debug.WriteLine("In input:  " + b.ToString());
			}

			while (byte_queue.Count > 0)
			{
				bytes_available = byte_queue.Count();
				bool more_params = false;
				int pass = 0;

				foreach (byte b in byte_queue)
				{
					//Debug.WriteLine("Pass:  " + pass.ToString());
					byte_queue.TryDequeue(out current_byte);
					//Debug.WriteLine("current_byte:  " + current_byte.ToString());

					if (!data_in_sync || (data_in_sync && !more_params && (current_byte >= 0x00 && current_byte <= 0x1F)))
					{
						cmd_prefix = current_byte;
					}

					if (bytes_available > 0 && (current_byte >= 0 && current_byte <= 31))
					//if (bytes_available > 0)
					{
						//Debug.WriteLine("cmd_prefix:  " + cmd_prefix.ToString());
						switch (cmd_prefix)
						{
							case 0x00:							//admin commands
								if (bytes_available >= 1)
								{
									byte_queue.TryDequeue(out param1);
									switch (param1)
									{
										case 0x02:
											data_in_sync = true;
											port.Write(version);
											continue;
										case 0x03:
											Terminate();
											break;
									}
								}
								else
									continue;
								break;
							case 0x01:
								byte_queue.TryDequeue(out param1);
								SetSidetone(param1);
								break;
							case 0x02:
								byte_queue.TryDequeue(out param1);
								SetCWSpeed(param1);
								break;
							case 0x03:
								byte_queue.TryDequeue(out param1);
								SetWeighting(param1);
								break;
							case 0x04:
								byte_queue.TryDequeue(out param1);
								p1_filled = true;

								if (byte_queue.Count() >= 1 && !p2_filled)
								{
									byte_queue.TryDequeue(out param2);
									SetPTTLead_Tail(param1, param2);
									p1_filled = false;
									p2_filled = false;
									more_params = false;
								}
								else
								{
									more_params = true;
									continue;
								}
								break;
							case 0x05:
								byte_queue.TryDequeue(out param1);
								p1_filled = true;
								if (byte_queue.Count() >= 1 && !p2_filled)
								{
									byte_queue.TryDequeue(out param2);
									p2_filled = true;
								}
								else
								{
									more_params = true;
									continue;
								}
								if (byte_queue.Count() >= 1 && !p3_filled)
								{
									byte_queue.TryDequeue(out param3);
									SetupSpeedPot(param1, param2, param3);
									more_params = false;
									p1_filled = false;
									p2_filled = false;
									p3_filled = false;
								}
								break;
							case 0x06:
								byte_queue.TryDequeue(out param1);
								SetPauseState(param1);
								break;
							case 0x07:
								GetSpeedPot();
								break;
							case 0x08:
								Backspace();
								break;
							case 0x09:
								byte_queue.TryDequeue(out param1);
								SetPinConfig(param1);
								break;
							case 0x0A:
								ClearBuffer();
								break;
							case 0x0B:
								byte_queue.TryDequeue(out param1);
								KeyImmediate(param1);
								break;
							case 0x0C:
								byte_queue.TryDequeue(out param1);
								SetHSCW(param1);
								break;
							case 0x0D:
								byte_queue.TryDequeue(out param1);
								SetFarnsworth(param1);
								break;
							case 0x0E:
								byte_queue.TryDequeue(out param1);
								SetWKMode(param1);
								break;
							case 0x0F:
								byte_queue.TryDequeue(out param1);
								LoadDefaults(param1);
								break;
							case 0x10:
								byte_queue.TryDequeue(out param1);
								SetFirstExtension(param1);
								break;
							case 0x11:
								byte_queue.TryDequeue(out param1);
								SetKeyComp(param1);
								break;
							case 0x12:
								byte_queue.TryDequeue(out param1);
								SetPaddleSwitchpoint(param1);
								break;
							case 0x13:
								NullCommand();
								break;
							case 0x14:
								byte_queue.TryDequeue(out param1);
								SoftwarePaddle(param1);
								break;
							case 0x15:
								RequestWKStatus();
								break;
							case 0x16:
								byte_queue.TryDequeue(out param1);
								if (param1 == 0)
								{
									SetPointer(param1);
								}
								else if (param1 >= 1 && param1 <= 3)
								{
									if (byte_queue.Count() >= 1 && !p2_filled)
									{
										byte_queue.TryDequeue(out param2);
										SetPointer(param1, param2);
										more_params = false;
										p1_filled = false;
										p2_filled = false;
									}
									else
									{
										more_params = true;
										continue;
									}
								}
								break;
							case 0x17:
								byte_queue.TryDequeue(out param1);
								SetDitDahRatio(param1);
								break;
							case 0x18:
								byte_queue.TryDequeue(out param1);
								PTTOn_Off(param1);
								break;
							case 0x19:
								byte_queue.TryDequeue(out param1);
								KeyBuffered(param1);
								break;
							case 0x1A:
								byte_queue.TryDequeue(out param1);
								Wait(param1);
								break;
							case 0x1B:
								byte_queue.TryDequeue(out param1);
								MergeLetters(param1);
								break;
							case 0x1C:
								byte_queue.TryDequeue(out param1);
								BufferedSpeedChange(param1);
								break;
							case 0x1D:
								byte_queue.TryDequeue(out param1);
								BufferedHSCWSpeedChange(param1);
								break;
							case 0x1E:
								CancelBufferedSpeedChange();
								break;
							case 0x1F:
								BufferedNOP();
								break;
							default:
								break;
						}	//end of switch
					}	//if bytes_available > 0
					else if(current_byte >= 0x20 && current_byte <= 0x3F)
					{
						string c = current_byte.ToString();
						Debug.WriteLine("queued:  " + c);
						cw_queue.Enqueue(c);
					}
					if (cw_queue.Count() > 0 && bytes_available == 0 && !sending)
					{
						SendCW();
						ClearQueue();
					}
					bytes_available = byte_queue.Count();
					if (!data_in_sync)
						pass++;
				}	//foreach
			}
		}

		private void Parse(ConcurrentQueue<byte> byte_queue)
		{

		}	//Parse

		private void Terminate()
		{
			Debug.WriteLine("Terminated");
		}

		string echo_msg = string.Empty;
		private void SendCW()
		{
			if (cw_queue.Count() > 0)
			{
				sending = true;
				string send_msg = string.Empty;
				string a = string.Empty;
				while (cw_queue.TryDequeue(out a))
				{
					send_msg += a;
					echo_msg += a;
				}
				port.Write(buffer_busy);
				cwx.Send(send_msg);
			}
		}

		#region Command Methods

		private void SetSidetone(byte freq)
		{
			int out_freq = 600;
			switch(freq)
			{
				case 1:
					out_freq = 4000;
					break;
				case 2:
					out_freq = 2000;
					break;
				case 3:
					out_freq = 1500;
					break;
				case 4:
					out_freq = 1000;
					break;
				case 5:
					out_freq = 800;
					break;
				case 6:
					out_freq = 650;
					break;
				case 7:
					out_freq = 600;
					break;
				case 8:
					out_freq = 500;
					break;
				case 9:
					out_freq = 450;
					break;
				case 10:
					out_freq = 400;
					break;
			}
			this_radio.CWPitch = out_freq;
			Debug.WriteLine("Sidetone:  " + out_freq.ToString());
		}

		private void SetCWSpeed(byte wpm)
		{
			Debug.WriteLine("Set CW Speed:  " + Convert.ToInt32(wpm).ToString());
			int speed = Convert.ToInt32(wpm);
			this_radio.CWSpeed = speed;
		}

		private void SetWeighting(byte weight)
		{
			Debug.WriteLine("SetWeighting:  " + Convert.ToInt32(weight).ToString());
		}

		private void SetPTTLead_Tail(byte p1, byte p2)
		{
			Debug.WriteLine("SetPTT:  " + Convert.ToInt32(p1).ToString() + " " + Convert.ToInt32(p2).ToString());
		}

		private void SetupSpeedPot(byte p1, byte p2, byte p3)
		{
			Debug.WriteLine("Speed Pot:  " + Convert.ToInt32(p1).ToString() + " " + Convert.ToInt32(p2).ToString() + " " + Convert.ToInt32(p3).ToString());
		}

		private void SetPauseState(byte pause)
		{
			Debug.WriteLine("Set Pause:  " + Convert.ToInt32(pause).ToString());
		}

		private void GetSpeedPot()
		{
			int cwx_speed = cwx.Speed;
			//Debug.WriteLine("Speed is set at:  "+cwx_speed.ToString()+" wpm");
			byte[] speed = new byte[] { (byte)(cwx_speed + 64) };

			port.Write(speed);
		}

		private void Backspace()
		{
			Debug.WriteLine("Backspace");
		}

		private void SetPinConfig(byte config)
		{
			Debug.WriteLine("Set Pin Config:  " + Convert.ToInt32(config).ToString());
		}

		private void ClearBuffer()
		{
			Debug.WriteLine("Clear Buffer");
		}

		private void KeyImmediate(byte action)
		{
			Debug.WriteLine("Key Immediate");
		}

		private void SetHSCW(byte lpm)
		{
			Debug.WriteLine("Set HSCW:  " + Convert.ToInt32(lpm).ToString());
		}

		private void SetFarnsworth(byte wpm)
		{
			Debug.WriteLine("Set Farnsworth:  " + Convert.ToInt32(wpm).ToString());
		}

		private void SetWKMode(byte mode)
		{
			Debug.WriteLine("Set WinKeyer Mode:  " + Convert.ToInt32(mode).ToString());
		}

		private void LoadDefaults(byte defaults)
		{
			Debug.WriteLine("Load Defaults:  " + Convert.ToInt32(defaults).ToString());
		}

		private void SetFirstExtension(byte ext)
		{
			Debug.WriteLine("Set 1st Extension:  " + Convert.ToInt32(ext).ToString());
		}

		private void SetKeyComp(byte comp)
		{
			Debug.WriteLine("Set Key Comp:  " + Convert.ToInt32(comp).ToString());
		}

		private void SetPaddleSwitchpoint(byte sp)
		{
			Debug.WriteLine("Set Paddle Switchpoint:  " + Convert.ToInt32(sp).ToString());
		}

		private void NullCommand()
		{
			Debug.WriteLine("Null Command");
		}

		private void SoftwarePaddle(byte paddle)
		{
			Debug.WriteLine("Software Paddle:  " + Convert.ToInt32(paddle).ToString());
		}

		private void RequestWKStatus()
		{
			Debug.WriteLine("Request WinKeyer Status");
		}

		private void SetPointer(byte p1)
		{
			Debug.WriteLine("Set Pointer Single:  " + Convert.ToInt32(p1).ToString());
		}

		private void SetPointer(byte p1, byte p2)
		{
			Debug.WriteLine("Set Pointer Double:  " + Convert.ToInt32(p1).ToString() + " " + Convert.ToInt32(p2).ToString());
		}

		private void SetDitDahRatio(byte ratio)
		{
			Debug.WriteLine("Set Dit/Dah Ratio:  " + Convert.ToInt32(ratio).ToString());
		}

		private void PTTOn_Off(byte ptt)
		{
			Debug.WriteLine("PTT:  " + Convert.ToInt32(ptt).ToString());
		}

		private void KeyBuffered(byte buff)
		{
			Debug.WriteLine("Key Buffered:  " + Convert.ToInt32(buff).ToString());
		}

		private void Wait(byte sec)
		{
			Debug.WriteLine("Wait for:  " + Convert.ToInt32(sec).ToString());
		}

		private void MergeLetters(byte ltrs)
		{
			Debug.WriteLine("Merge Letters:");
		}

		private void BufferedSpeedChange(byte wpm)
		{
			Debug.WriteLine("Buffered Speed Change:  " + Convert.ToInt32(wpm).ToString());
		}

		private void BufferedHSCWSpeedChange(byte lpm)
		{
			Debug.WriteLine("Buffered HSCW Speed Change:  " + Convert.ToInt32(lpm).ToString());
		}

		private void CancelBufferedSpeedChange()
		{
			Debug.WriteLine("Cancel Buffered Speed Change");
		}

		private void BufferedNOP()
		{
			Debug.WriteLine("Buffered NOP");
		}

		#endregion Command Methods

		#region Helper Methods

		private void ClearQueue()
		{
			string x;
			while (cw_queue.TryDequeue(out x))
			{ }
		}
		#endregion Helper Methods

		#endregion Methods

		#region Events

		void cwx_CharSent(int index)								//CWX CharSent event.  Used to echo to N1MM
		{
			if(echo_msg != null && echo_msg.Length > 0)
			{
				string c_to_send = echo_msg.Substring(0, 1);
				port.Write(c_to_send);
				echo_msg = echo_msg.Remove(0, 1);
				if (echo_msg.Length <= 1)
				{
					port.Write(buffer_ready);
					sending = false;
				}
			}
		}

		#endregion Events


	}
}
