using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.IO.Ports;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using Cat.Cat.PortValidationTests;
using Cat.Cat.Interfaces;
using log4net;
using Cat.CatMain.Ports;

namespace Cat.Cat.Ports
{
 
	public class CatSerialPort : ISerialPort
	{

		#region Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private ObjectManager _om;
        private SerialPort this_port;
		private ISerialComSpec serialComSpec;		
		string port_name;
		string port_type;	//3/29/2015 so2r bt
		WinKeyer wk;
		//WinKeyer2 wk2;

		#endregion Variables

		#region Properties

        private int _portNumber;
        public int PortNumber
        {
            get { return _portNumber; }
        }

		private bool can_ptt = false;
		public bool CanPTT
		{
			get { return can_ptt; }
			set { can_ptt = value; }
		}

		private bool ptt_on_dtr = false;
		public bool PTTOnDTR
		{
			get { return ptt_on_dtr; }
			set { ptt_on_dtr = value; }
		}

		private bool ptt_on_rts = false;
		public bool PTTOnRTS
		{
			get { return ptt_on_rts; }
			set { ptt_on_rts = value; }
		}

		private CatCache _cache;
		public CatCache Cache
		{
			get { return _cache; }
			set { _cache = (CatCache)value; }

		}

		#endregion Properties

		#region Constructor
   

		public CatSerialPort(int portNumber, ISerialComSpec comSpec, string port_type)	//3/29/2015 so2r bt
		{
			this._portNumber = portNumber;
			this.serialComSpec = comSpec;
			this.port_type = port_type;
			_om = ObjectManager.Instance;
			wk = new WinKeyer(this);
			//wk2 = new WinKeyer2(this);
			CreatePort();
		}

		#endregion Constructor

		#region Methods

		private void CreatePort()
		{
			port_name = "COM";

			if (IsValidPortSpec())
			{
				port_name += this._portNumber.ToString();
				try
				{
					this_port = new SerialPort(port_name);
					this_port.DataReceived += new SerialDataReceivedEventHandler(this.SerialReceivedData);
					this_port.PinChanged += new SerialPinChangedEventHandler(this_port_PinChanged);
					this_port.DtrEnable = true;
					this_port.RtsEnable = true;
					this_port.Encoding = Encoding.UTF8;
					//if (port_type == "winkeyer")
					//    this_port.BaudRate = 1200;
					//else
						this_port.BaudRate = serialComSpec.baudRate;
					this_port.DataBits = serialComSpec.dataBits;
					this_port.Parity = serialComSpec.parity;
					//if (port_type == "winkeyer")
					//    this_port.StopBits = StopBits.Two;
					//else
						this_port.StopBits = serialComSpec.stopBits;
					this_port.Handshake = Handshake.None;
					this_port.ReadTimeout = 5000;
					this_port.WriteTimeout = 500;
					this_port.ReceivedBytesThreshold = 1;
					System.Threading.Thread.Sleep(20);
					this_port.Open();
				}
				catch (Exception ex)
				{
					_log.Error(ex.Message);
				}

			}
		}

		void this_port_PinChanged(object sender, SerialPinChangedEventArgs e)
		{
			//Console.WriteLine("Port: " + this_port.PortName + " ptt_on_rts: " + this.ptt_on_rts.ToString() + " ptt_on_dtr: " + this.ptt_on_dtr.ToString());
			if (can_ptt)
			{
				switch (e.EventType)
				{
					case SerialPinChange.CtsChanged:
						//Console.WriteLine("cts holding = " + this_port.CtsHolding.ToString());
						if (ptt_on_rts)
						{
							if (this_port.CtsHolding)
								_om.StateManager.Set("Mox", "1");
							else
								_om.StateManager.Set("Mox", "0");
						}
						break;
					case SerialPinChange.DsrChanged:
						//Console.WriteLine("dsr_holding ="+this_port.DsrHolding.ToString());
						if (ptt_on_dtr)
						{
							if (this_port.DsrHolding)
								_om.StateManager.Set("Mox", "1");
							else
								_om.StateManager.Set("Mox", "0");
						}
						break;
					default:
						_log.Error("Fell thru EventType ");
						break;
				}
			}
		}

		public void OpenPort()
		{
			this_port.Open();
		}

		public void ClosePort()
		{
			this_port.Close();
		}

		public void RemovePort()
		{
			this_port.Dispose();
		}

		public void Write(string data)
		{
			if(!can_ptt)
				this_port.Write(data);
		}

		public void Write(byte[] data)
		{
			if (!can_ptt)
				this_port.Write(data, 0, data.Length);
		}

		public void DumpOutBuffer()
		{
			this_port.DiscardOutBuffer();
		}

		StringBuilder comBuffer = new StringBuilder();
		//regex:  capture any string that ends in a semicolon.
		Regex regex = new Regex(".*?;");
		//winkeyer_regex:  capture any hex character in the range
		Regex winkeyer_regex = new Regex(@"^[\x00-\xFF]*");
		//otrsp_regex:  ^starting at the beginning of the string, capture a group named 'prefix' that contains zero or one "?"'s 
		//capture a group named 'keyword' that is alpha for a minimum of two and a maximum of four characters 
		//capture a group named 'value' that starts with a digit between 0 and 9, may have a decimal point behind that, and
		//may have additional alpha characters zero or more times.
		Regex otrsp_regex = new Regex(@"^(?'prefix'\?)*(?'keyword'[A-Z]{2,4})(?'value'[0-9]*\.*-*[0-9]*[A-Z]*)");

        private static object lock_obj = new Object();
		void SerialReceivedData(object source, SerialDataReceivedEventArgs e)
		{
            lock (lock_obj) // ensure that only one command can be processed at a time
            {
                //string debug_string = this_port.PortName + " ";

                if (port_type == "winkeyer")						//bypass the cache if wk
                {
                    int cnt = this_port.BytesToRead;
                    byte[] bytes = new byte[cnt];
                    this_port.Read(bytes, 0, cnt);
                    Debug.WriteLine("serial port bytes:  " + cnt.ToString());
                    wk.ProcessCommand(bytes);
                    //Thread.Sleep(100);
                    //wk2.ProcessCommand(bytes);
                }
                else
                {
                    string s = this_port.ReadExisting();
                    //Debug.WriteLine("port received:  " + s);
                    //if (port_type != "winkeyer")
                    //{
                    s = s.Replace("\r", "");
                    s = s.Replace("\n", "");
                    //}
                    comBuffer.Append(s);

                    switch (port_type.ToLower())
                    {
                        //case "winkeyer":
                        //    regex = winkeyer_regex;
                        //    break;
                        case "otrsp":
                            regex = otrsp_regex;
                            //debug_string += "(OTRSP): ";
                            break;
                        case "so2r":
                            //debug_string += "(SO2R): ";
                            break;
                        case "master":
                            //debug_string += "(master): ";
                            break;
                        case "shared":
                            break;
                        default:
                            _log.Error("Fell thru port_type with " + port_type);
                            break;
                    }

                    for (Match m = regex.Match(comBuffer.ToString()); m.Success; m = m.NextMatch())
                    {
                        //Debug.WriteLine("Match:  "+m.Value.ToString());
                        try
                        {
                            //_cache.AddCmd(m.Value + ":" + port_type);
                            _cache.ProcessCommand(m.Value + ":" + port_type);
                        }
                        catch (Exception ex)
                        {
                            _log.Error(ex.Message);
                        }
                        finally
                        {
                            if (m.Length > 0)
                                comBuffer = comBuffer.Replace(m.Value, "", 0, m.Length);
                            //Debug.WriteLine("comBuffer:  " + comBuffer);
                        }
                    }

                    //debug_string += s;
                    //_log.Debug(debug_string);
                }
            }
		}


		#endregion Methods 

		#region Validation Tests

		private bool IsValidPortSpec()
		{
			if (!PortValidation.ValidPortRange(this._portNumber, 1, 255))
				throw new Exception("Port Number " + this._portNumber.ToString() + " is not valid");

			if (!PortValidation.ValidBaudRate(serialComSpec.baudRate))
				throw new Exception("Baud rate " + serialComSpec.baudRate.ToString() + " is not valid");

			if (!PortValidation.ValidDataBits(serialComSpec.dataBits))
				throw new Exception("Data bits " + serialComSpec.dataBits.ToString() + " is not valid");

			if (!PortValidation.ValidParity(serialComSpec.parity.ToString()))
				throw new Exception("Parity " + serialComSpec.parity.ToString() + " is not valid");

			if (!PortValidation.ValidStopBits(serialComSpec.stopBits.ToString()))
				throw new Exception("Stop bits " + serialComSpec.stopBits.ToString() + " is not valid");

			if (!PortValidation.ValidCombo(serialComSpec.dataBits, serialComSpec.stopBits.ToString()))
				throw new Exception("The StopBit/DataBit combination is not valid");

			return true;
		}


		#endregion Validation Texts

		#region Documentation

		/*! \class Cat.Cat.Ports.CatSerialPort
		 * \brief       CAT Serial Port Class
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        12/2011
		 * \copyright   FlexRadio Systems
		 */

		/*! \fn CatSerialPort
		 *  \brief  CAT Serial Port Constructor.
		 *  \param portNumber integer from app.config. 
		 *  \param comSpec ISerialComSpec from app.config.
		 */

		/*! \var this_port
		 *  \brief instance name for the SerialPort.
		 */

		/*! \var _om
		 *  \brief ObjectManager instance for reference to factory.
		 */

		/*! \var _cache
		 *  \brief Instance of the CatCache class.  See app.config serial port property "Cache".
		 */

		/*! \var _timer
		 *  \brief dot NET Timer class for polling of _queue using timerDelegate.
		 */

		/*! \var serialComSpec
		 *  \brief instance of SerialComSpec to hold the current port parameters.
		 */

		/*! \var _portNumber
		 *  \brief integer port number from the constructor.
		 */

		/*! \var port_name
		 *  \brief string port name derived in CreatePort().
		 */

		/*! \var comBuffer
		 *  \brief string buffer to hold raw incoming port data.
		 */

		/*! \var regex
		 *  \brief Regular Expression to extract complete CAT commands 
		 *  from the comBuffer by recognizing the terminal semicolon.
		 */

		/*! \fn CreatePort()
		 *  \brief Structures and opens the port using parameters from serialComSpec.
		 */

		/*! \fn Write
		 *  \brief Implementation of the ISerialPort Write method, writes data to the serial port.
		 */

		/*! \fn SerialReceivedData
		 *  \brief Delegate for the port DataReceived event.
		 *  Serial data is concatenated in the string buffer (comBuffer) and
		 *  enqueued when a completed (terminated) CAT command is received.  The 
		 *  current regex match is then removed from the comBuffer.
		 */


		/*! \fn IsValidPortSpec
		 *  \brief Checks the parameters of serialComSpec for correctness.
		 */

		#endregion Documentation


	}
}
