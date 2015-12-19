using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using Flex.Smoothlake.FlexLib;
using log4net;
using Cat.Cat.Ports;
using System.IO.Ports;

namespace Cat.CatMain.Ports
{
	public class WinKeyer
	{
		#region Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private ObjectManager _om;
		private Radio this_radio;
		private CWX cwx;

		private byte[] buffer_ready = new byte[1] { Convert.ToByte('\xC0') };	// N1MM buffer ready
		private byte[] stop_cw = new byte[1] { Convert.ToByte('\xC2') };		// N1NN
		private byte[] buffer_busy = new byte[1] { Convert.ToByte('\xC4') };	// N1MM buffer busy
		//private byte[] version = new byte[1] { Convert.ToByte('\x1E') };		// Keyer version # (3)
		private byte[] version = new byte[1] { Convert.ToByte('\x0A') };		// Keyer version # (1)

		private bool param1_filled;		//input buffer data byte 
		private bool param2_filled;
		private bool param3_filled;
		private int param1;
		private int param2;
		private int param3;

		private string cw_buffer = string.Empty;
		string msg_to_send = string.Empty;
		private bool sending = false;
		private bool canned_msg_detected = false;
		private bool first_pass = true;
		private bool stop_sending = false;


		#endregion Variables


		#region Properties

		private CatSerialPort port;
		public CatSerialPort Port
		{
			get { return port; }
			set { port = value; }
		}

		#endregion Properties

		#region Constructor

		public WinKeyer(CatSerialPort port)
		{
			_om = ObjectManager.Instance;
			this_radio = Cat.CatStateManager.CSMRadio;
			cwx = this_radio.GetCWX();
			this.cwx.CharSent += new CWX.CharSentEventHandler(cwx_CharSent);
			this.cwx.EraseSent += new CWX.EraseSentEventHandler(cwx_EraseSent);
			this.port = port;
		}





		#endregion Constructor

		#region Parser

		string data_block = string.Empty;
		string sub_block = string.Empty;
		string data = string.Empty;				//input buffer

		public void ProcessCommand(byte[] bytes)
		{
			foreach (byte b in bytes)
			{
				data += b.ToString("X2");
			}
			while (data != string.Empty)
			{
				// See Notes at bottom of file for detailed explanation of code function
				Debug.WriteLine("parsing data:  "+data);
				if (data_block == string.Empty)
				{
					data_block = data.Substring(0, 2);				//Get the first two bytes, should represent the command
					//Debug.WriteLine("data block:  " + data_block);
					data = data.Remove(0, 2);						//Remove the command from the string, next bytes should be data
				}
				try
				{
					if (data_block.Length >= 2)
					{
						switch (data_block)								//Look up the command
						{
							case "00":									//this is an admin command
								if (data.Length >= 2)
								{
									sub_block = data.Substring(0, 2);
									switch (sub_block)
									{
										case "02":						//must be detected to send reply to N1MM and keep going
											SendVersion(port);			//commands 04 - 20 don't appear to be important
											sub_block = string.Empty;
											data_block = string.Empty;
											data = data.Remove(0, 2);
											break;
										case "03":
											Terminate();
											sub_block = string.Empty;
											data_block = string.Empty;
											data = data.Remove(0, 2);
											break;
										default:
											_log.Error("Fell thru sub_block with " + sub_block);
											break;
									}
									break;

								}
								break;									//start of N1MM operational commands
							case "C0":
							case "C4":
								Debug.WriteLine("C0/C4 detected");
								break;
							case "01":									//probably could redo this as some sort of "decode" class and save some code									
								if (data.Length >= 2)
								{
									int freq = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetSidetone(freq);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "02":
								if (data.Length >= 2)
								{
									int speed = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetCWSpeed(speed);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "03":
								if (data.Length >= 2)
								{
									int weight = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetWeighting(weight);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "04":			
								if (!param1_filled && data.Length >= 2)
								{
									param1 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									data = data.Remove(0, 2);
									param1_filled = true;
								}
								if (!param2_filled && data.Length >= 2)
								{
									param2 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetPTTLead_Tail(param1, param2);
									data = data.Remove(0, 2);
									data_block = string.Empty;
									param1_filled = false;
									param2_filled = false;
									param1 = 0;
									param2 = 0;
								}
								break;
							case "05":
								if (!param1_filled && data.Length >= 2)
								{
									param1 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									data = data.Remove(0, 2);
									param1_filled = true;
								}
								if (!param2_filled && data.Length >= 2)
								{
									param2 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									data = data.Remove(0, 2);
									param2_filled = true;
								}
								if (!param3_filled && data.Length >= 2)
								{
									param3 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetSpeedPot(param1, param2, param3);
									data_block = string.Empty;
									data = data.Remove(0, 2);
									param1_filled = false;
									param2_filled = false;
									param3_filled = false;
									param1 = 0;
									param2 = 0;
									param3 = 0;
								}
								break;
							case "06":
								if (data.Length >= 2)
								{
									int pause = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetPauseState(pause);
									data_block = string.Empty;
									data = data.Remove(0, 2);
								}
								break;
							case "07":
								GetSpeedPot(port);
								data_block = string.Empty;
								break;
							case "08":							//not implemented yet
								Backspace();
								data_block = string.Empty;
								break;
							case "09":
								if (data.Length >= 2)
								{
									int config = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetPINCFGRegister(config);
									data_block = string.Empty;
									data = data.Remove(0, 2);
								}
								break;
							case "0A":
								ClearBuffer();
								data_block = string.Empty;
								break;
							case "0B":
								if (data.Length >= 2)
								{
									int immediate = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									KeyImmediate(immediate);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "0C":
								if (data.Length >= 2)
								{
									int rate = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetHSCW(rate);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "0D":
								if (data.Length >= 2)
								{
									int rate = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetFarns(rate);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "0E":
								if (data.Length >= 2)
								{
									int mode = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetWKMode(mode);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "0F":
								if (data.Length >= 2)
								{
									LoadDefaults();			//figure this out later
									data_block = string.Empty;
								}
								break;
							case "10":
								if (data.Length >= 2)
								{
									int first_extension = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetFirstExt(first_extension);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "11":
								if (data.Length >= 2)
								{
									int comp = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetKeyComp(comp);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "12":
								if (data.Length >= 2)
								{
									int sp = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetPaddleSwitchpoint(sp);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "13":	//null command, ignored
								break;
							case "14":
								if (data.Length >= 2)
								{
									int paddle = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SoftwarePaddle(paddle);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "15":
								RequestWKStatus();
								data_block = string.Empty;
								break;
							case "16":
								try
								{
									if (data.Length >= 2)
									{
										switch (data.Substring(0, 2))
										{
											case "00":
												if (!param1_filled && data.Length >= 2)
												{
													param1 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
													PointerCommand(param1);
													data = data.Remove(0, 2);
													data_block = string.Empty;
												}
												break;
											case "01":
											case "02":
											case "03":
												if (!param1_filled && data.Length >= 2)
												{
													param1 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
													param1_filled = true;
													data = data.Remove(0, 2);
												}
												if (!param2_filled && data.Length >= 2)
												{
													param2 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
													data = data.Remove(0, 2);

													PointerCommand(param1, param2);
													data_block = string.Empty;
													param1_filled = false;
													param2_filled = false;
													param1 = 0;
													param2 = 0;
												}
												else
													continue;
												break;
											default:
												continue;
										}
									}
									else
										continue;
									break;
								}
								catch (Exception e)
								{
									_log.Error("@ 16 with "+ data + e.Message);
								}
								break;
							case "17":
								if (data.Length >= 2)
								{
									int ratio = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									SetDitDahRatio(ratio);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "18":
								if (data.Length >= 2)
								{
									int sw = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									PTTOn_OFF(sw);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "19":
								if (data.Length >= 2)
								{
									int sec = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									KeyBuffered(sec);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "1A":
								if (data.Length >= 2)
								{
									int sec = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									Wait(sec);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "1B":
								if (!param1_filled && data.Length >= 2)
								{
									param1 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									data = data.Remove(0, 2);
									param1_filled = true;
								}
								if (!param2_filled && data.Length >= 2)
								{
									param2 = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									MergeLetters(param1.ToString(), param2.ToString());
									data = data.Remove(0, 2);
									data_block = string.Empty;
									param1_filled = false;
									param2_filled = false;
									param1 = 0;
									param2 = 0;
								}
								break;
							case "1C":
								if (data.Length >= 2)
								{
									int wpm = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									BufferedSpeedChange(wpm);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "1D":
								if (data.Length >= 2)
								{
									int lpm = int.Parse(data.Substring(0, 2), NumberStyles.HexNumber);
									HSCWBufferedSpeedChange(lpm);
									data = data.Remove(0, 2);
									data_block = string.Empty;
								}
								break;
							case "1E":
								CancelBufferedSpeedChange();
								data_block = string.Empty;
								break;
							case "1F":
								BufferedNOP();
								data_block = string.Empty;
								break;
							default:									//if it falls through the switch block, it must be CW
								cw_buffer += data_block;
								data_block = string.Empty;
								break;
						}
						first_pass = false;
					}
					if(data.Length == 0)								//we've used up all the input, see if there is something in the cw buffer
					{
						string send_buffer = string.Empty;
						//Debug.WriteLine("CW buffer: " + cw_buffer);
						while(cw_buffer.Length > 0)
						{
							send_buffer = cw_buffer;
							cw_buffer = string.Empty;
							ThreadStart send_thread = delegate
							{
								CWSend(port, send_buffer);
							};
							new Thread(send_thread).Start();
						}
					}
				}
				catch (Exception e)
				{
					_log.Error(e.Message);
				}
			}
		}

		#endregion Parser

		#region Methods

		#region WinKeyer Admin Commands

		private void SendVersion(CatSerialPort port)							//response to \x00\x02 host open command
		{
			port.Write(version);
			Debug.WriteLine("version sent");
		}

		private void Terminate()												//\x00\x03
		{
			Debug.WriteLine("wk closed");
		}

		private void SendStandaloneMessage(int msg_no)							//\x00\x14
		{
			Debug.WriteLine("sent standalone message " + msg_no.ToString());
		}

		#endregion WinKeyer Admin Commands

		#region WinKeyer Op Commands

		private void SetSidetone(int freq)										///x01
		{
			//Debug.WriteLine("set sidetone "+ (62500/freq).ToString());
		}

		private void SetCWSpeed(int speed)										//\x02
		{
			//Debug.WriteLine("CW Speed: " + speed.ToString());
			cwx.Speed = speed;
		}


		private void SetSpeedPot(int min, int range, int zero)					//\x05
		{
			//Debug.WriteLine("Set speed pot: "+min.ToString()+", "+range.ToString()+", "+zero);
		}

		private void SetWeighting(int weight)									//\x03
		{
			//Debug.WriteLine("Set Weight:  " + weight.ToString());
		}

		private void SetPTTLead_Tail(int lead, int tail)						//\x04
		{
			//Debug.WriteLine("Set PTT Lead/Tail:  lead " + lead.ToString() + ", tail " + tail.ToString());
		}

		private void SetPauseState(int pause)									//\x06
		{
			//Debug.WriteLine("Set Pause State:  " + pause.ToString());
		}

		private void GetSpeedPot(CatSerialPort port)							//\x07
		{
			int cwx_speed = cwx.Speed;
			//Debug.WriteLine("Speed is set at:  "+cwx_speed.ToString()+" wpm");
			byte[] speed = new byte[]{(byte)(cwx_speed+128)};
			port.Write(speed);
		}

		private void Backspace()												//\x08
		{
			//Debug.WriteLine("Backspace input buffer");
		}

		private void SetPINCFGRegister(int config)								//\x09
		{
			//Debug.WriteLine("Set PinConfig" + config.ToString());
		}

		private void ClearBuffer()												//\x0A
		{
			//Debug.WriteLine("Clear Buffer");
			HandleBufferClear();
			//cw_buffer = string.Empty;
		}

		private void KeyImmediate(int when)										//\x0B
		{
			//Debug.WriteLine("Key Immediate:  " + when.ToString());
		}

		private void SetHSCW(int lpm)											//\x0C
		{
			//Debug.WriteLine("High Speed CW:  " + lpm.ToString());
		}

		private void SetFarns(int rate)											//\x0D
		{
			//Debug.WriteLine("Set Farnsworth:  " + rate.ToString());
		}

		private void SetWKMode(int mode)										//\x0E
		{
			//Debug.WriteLine("WinKeyer Mode:  " + mode.ToString());
		}

		private void LoadDefaults()												//\x0F
		{
			//Debug.WriteLine("Load Defaults");
		}

		private void SetFirstExt(int ext)										//\x10
		{
			//Debug.WriteLine("Set First Extension:  " + ext.ToString());
		}

		private void SetKeyComp(int comp)										//\x11
		{
			//Debug.WriteLine("Set Key Comp:  " + comp.ToString());
		}

		private void SetPaddleSwitchpoint(int sp)								//\x12
		{
			//Debug.WriteLine("Set Paddle Switchpoint:  " + sp.ToString());
		}
		//\x13 Null is not used
		private void SoftwarePaddle(int mode)									//\x14
		{
			//Debug.WriteLine("software paddle mode: " + mode);
		}

		private void RequestWKStatus()											//\x15
		{
			//Debug.WriteLine("Request WK Status");
		}

		private void PointerCommand(int cmd)
		{
			canned_msg_detected = true;
			//Debug.WriteLine("Canned Message Detected 1");				//\x16\nn
		}

		private void PointerCommand(int cmd, int nulls)							//\x16\nn\nn
		{
			canned_msg_detected = true;
			//Debug.WriteLine("Canned Message Detected 2");
		}

		private void SetDitDahRatio(int ratio)									//\x17
		{
			//Debug.WriteLine("Set Dit/Dah Ratio:  " + ratio.ToString());
		}

		private void PTTOn_OFF(int sw)											//\x18
		{
			 //Debug.WriteLine("PTT On/OFF:  "+sw.ToString());
		}

		private void KeyBuffered(int sec)										//\x19
		{
			//Debug.WriteLine("Key Buffered:  " + sec.ToString());
		}

		private void Wait(int sec)												//\x1A
		{
			//Debug.WriteLine("Wait:  " + sec.ToString());
		}

		private void MergeLetters(string A, string B)							//\x1B
		{
			//Debug.WriteLine("Merge Letters:  " + A + " " + B);
		}

		private void BufferedSpeedChange(int wpm)								//\x1C
		{
			//Debug.WriteLine("Buffered Speed Change:  " + wpm.ToString());
		}

		private void HSCWBufferedSpeedChange(int lpm)							//\x1D
		{
			//Debug.WriteLine("High Speed CW Buffered Speed Change:  " + lpm.ToString());
		}

		private void CancelBufferedSpeedChange()								//\x1E
		{
			//Debug.WriteLine("Cancel Buffered Speed Change");
		}

		private void BufferedNOP()												//\x1F
		{
			//Debug.WriteLine("Buffered NOP");
		}

		#endregion WinKeyer Op Commands

		string msg_to_echo = string.Empty;
		private void CWSend(CatSerialPort port, string msg)
		{
			sending = true;
			Debug.WriteLine("Message from buffer:  " + msg);
			try
			{
				msg_to_send = string.Empty;
				for (int n = 0; n < msg.Length; n += 2)					//convert the string hex to ASCII characters
				{
					string this_byte = msg.Substring(n, 2);
					int dec_value = Convert.ToInt32(this_byte, 16);
					char value = (char)dec_value;
					msg_to_send += value;
				}
				msg_to_echo = msg_to_send;
				Debug.WriteLine("Message to send:  " + msg_to_send);
				cwx.Send(msg_to_send);
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}
		}

		void cwx_CharSent(int index)								//CWX CharSent event.  Used to echo to N1MM
		{
			if (msg_to_echo != null && msg_to_echo.Length > 0 && port != null)
			{
				Debug.WriteLine("echo:  " + msg_to_echo);
				string c_to_send = msg_to_echo.Substring(0, 1);
				Debug.WriteLine("char to send:  " + c_to_send);
				port.Write(buffer_busy);
				port.Write(c_to_send);
				msg_to_echo = msg_to_echo.Remove(0, 1);
				if (msg_to_echo.Length <= 1)
				{
					port.Write(buffer_ready);
					//Debug.WriteLine("buffer ready from char sent");
					msg_to_echo = string.Empty;
					sending = false;
				}
			}
		}

		void cwx_EraseSent(int start, int stop)
		{
			Debug.WriteLine("buffer clear from cwx");
			HandleBufferClear();
		}

		private void HandleBufferClear()
		{
			if (first_pass)							//ignore Clear Buffer on first pass
			{
				canned_msg_detected = false;
			}
			else if (canned_msg_detected && sending)	//Processing a repeating message
			{
				Debug.WriteLine("first");
				//stop_sending = true;
				//cwx.ClearBuffer();
				//port.Write(buffer_ready);
				Debug.WriteLine("buffer ready from handle buffer clear canned msg...");
				canned_msg_detected = false;
			}
			else if (sending)
			{
				Debug.WriteLine("second");
				//stop_sending = true;
				//cwx.ClearBuffer();
				cw_buffer = string.Empty;
				msg_to_send = string.Empty;
				msg_to_echo = string.Empty;
				//port.Write(buffer_ready);
				Debug.WriteLine("buffer ready from handle buffer clear sending");
			}
			sending = false;
			cwx.ClearBuffer();
			port.Write(buffer_ready);
		}

		#endregion Methods
	}

	/*  NOTES:
	 * 
	 * GENERAL ASSUMPTION:  Once the data stream is in sync (see 1 below), no serial bytes are ever lost.  This is not true but
	 *						it is the best we can do since there are no delimiters or consistent data patterns in the WinKeyer 
	 *						data to resync on. 
	 * DEFINITIONS:
	 * 
	 *	data_block = command prefix, i.e., "04" is "SetPTTLead_Tail.
	 *	data = parameters, if any, following a data_block.
	 *	param[n] = two bytes of data following a data_block.
	 *	param[n]_filled = true if data stored in param[n].
	 *	
	 * 1.	Data sync:  The third-party program must initialize WinKeyer by sending the "Host Open" command 0x00 0x02.  WinKeyer
	 *					will respond with a version number and opens the host interface.  N1MM will try two times and then
	 *					give up if no response is received.
	 *	
	 *	2.  There is no guarantee that the asynchronous serial data will always deliver a complete command (data_block plus all 
	 *		parameters required in a monlithic set of bytes.  To anticipate this, the parser doesn't release the data_block 
	 *		until the required number of parameters for that particular command are received.  Reference case "05" above.
	 *		The "05" data_block (SetSpeedPot) requries three paramters.  The data_block value is retained, and the while loop continues,
	 *		until all three parameter values are filled.
	 *		
	 *	3.	Data_block "16" is the big exception to how data is parsed.  The command moves the WinKeyer buffer pointer to facilitate
	 *		resetting, inserting, appending, or deleting buffered CW characters.  This command will have one of four subcommands:  
	 *		00 thru 03.  With the exception of 00, each subcommand will be followed by a single parameter indicating the required 
	 *		buffer position to modify.  Technically speaking, the documentation SUCKS!
	 *		
	 *		
	 *		
	*/
}
