using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using System.Text;
using System.Threading;
using log4net;
using Cat.Cat.Ports;
using Cat.CatMain.Ports;

namespace Cat.Cat
{
	public class CatCache 
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private ObjectManager _om;
		private ConcurrentQueue<string> _queue;
		private Timer _timer;
		private static string cmd_filter = @"^(?'prefix'[a-zA-Z]{4}|^[a-zA-Z]{2})(?'suffix'\+*\-*\d*)";
		private Regex regex = new Regex(cmd_filter);
		private OTRSP _otrsp = new OTRSP();
		//private WinKeyer wk = new WinKeyer();
		private string echo = string.Empty;

		private CatSerialPort _port;
		public CatSerialPort Port
		{
			get { return _port; }
			set { _port = (CatSerialPort)value; }
		}

		internal CatCache()
		{
			_om = ObjectManager.Instance;
			_queue = new ConcurrentQueue<string>();
			//wk = new WinKeyer(this.Port);
			TimerCallback timerDelegate = new TimerCallback(OnTimer);
			_timer = new Timer(timerDelegate, this, 0, 10);	//original 50, 10
		}

        internal void Close()
        {
            _timer.Change(Timeout.Infinite, Timeout.Infinite);
        }

		internal void AddCmd(string cmd)
		{
			try
			{
				_queue.Enqueue(cmd);
			}
			catch(Exception ex)
			{
				_log.Error(ex.Message);
			}
		}


		private void OnTimer(object info)
		{
			ProcessCommands();
		}


		private void ProcessCommands()
		{
			try
			{
				while (_queue.Count > 0)
				{
					string catCmd;
					_queue.TryDequeue(out catCmd);
					if (catCmd != null)
					{
					  
						ProcessCommand(catCmd);
					}
				}
			}
			catch(Exception ex)
			{
				_log.Error(ex.Message);
 			}
		}

		//3/29/2015 so2r bt  accommodate so2r tag added to 'cmd'
		internal void ProcessCommand(string raw_cmd)
		{
            try
            {
                string[] cmd_parts = raw_cmd.Split(':');
                string cmd_value = cmd_parts[0].Replace(";", "");
                string cmd_type = cmd_parts[1];
                //Debug.WriteLine(raw_cmd);
                switch (cmd_type.ToLower())
                {
                    case "so2r":
                        Convert2SO2R(cmd_parts[0]);	//modify this to same form as OTSRP
                        break;
                    case "otrsp":
                        //Debug.WriteLine("OTRSP Received:  " + cmd_value);
                        this._otrsp.ProcessCommand(cmd_value);
                        break;
                    case "winkeyer":
                        try
                        {
                            raw_cmd = raw_cmd.Replace(":winkeyer", "");
                            byte[] bytes = Encoding.ASCII.GetBytes(raw_cmd);
                            Debug.WriteLine("cache:  " + raw_cmd);
                            //this.wk.ProcessCommand(this._port, bytes);
                            //string hex = BitConverter.ToString(bytes);
                            //Debug.WriteLine("cache:  " + hex);
                            //string[] data = hex.Split('-');
                            //this.wk.ProcessCommand(data);
                            //this.wk.ProcessCommand(raw_cmd.Replace(":winkeyer", ""));
                        }
                        catch (Exception e)
                        {
                            _log.Error(e.Message + " @ cache wk");
                        }

                        break;
                    case "master":
                    case "dedicated":
                    case "shared":
                        string cmd_to_process = string.Empty;
                        object answer = _om.CatCmdProcessor.ExecuteCommand(cmd_value);	//
                        this._port.Write(answer.ToString());
                        //Debug.WriteLine("COM" + this._port.PortNumber.ToString() + " sent:  " + answer.ToString());
                        break;
                    default:
                        _log.Error("Fell thru cmd_type with " + cmd_type);
                        break;
                }
            }
            catch (Exception e)
            {
                _log.Error(e.Message);
            }
		}
		

		private void Convert2SO2R(string cmd)
		{
			string orig_prefix = string.Empty;
			string so2r_prefix = string.Empty;
			string suffix = string.Empty;
			bool is_write;

			try
			{
				Match m = regex.Match(cmd);
				orig_prefix = m.Groups[1].ToString().ToUpper();
				suffix = m.Groups[2].ToString();

				if ((orig_prefix.Length == 2 || orig_prefix.Length == 4) && suffix.Length > 0)
					is_write = true;
				else
					is_write = false;

				switch(orig_prefix)
				{
					case "FA":
						so2r_prefix = "FB";
						break;
					case "IF":
						so2r_prefix = "ZZIX";
						break;
					case "ZZAG":
						so2r_prefix = "ZZLE";
						break;
					case "ZZFA":
						so2r_prefix = "ZZFB";
						break;
					case "ZZFB":
						so2r_prefix = "ZZFA";
						break;
					case "ZZIF":
						so2r_prefix = "ZZIX";		//gets the ZZIF2 status word
						break;
					case "ZZMD":
						so2r_prefix = "ZZME";
						break;
					case "ZZRF":
						so2r_prefix = "ZZRW";
						break;
					case "ZZRT":
						so2r_prefix = "ZZRY";
						break;
					case "ZZFI":
						so2r_prefix = "ZZFJ";
						break;
					default:
						so2r_prefix = orig_prefix;
						break;
				}

				string so2r_answer = _om.CatCmdProcessor.ExecuteCommand(so2r_prefix + suffix).ToString();
				if (is_write)
					this._port.Write("");
				else
					this._port.Write(so2r_answer.Replace(so2r_prefix, orig_prefix));

				//Console.WriteLine("COM" + this._port.PortNumber.ToString() + " sent:  " + so2r_answer.Replace(so2r_prefix, orig_prefix));
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}

		}

		#region Documentation

		/*! \class Cat.Cat.CatCache
		 * \brief       CAT Concurrent Cache
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        12/2011
		 * \copyright   FlexRadio Systems
		 */

		/*! \var    _om
		 *  \brief  Local instance of the CAT ObjectManager.
		 */

		/*! \var    _queue  
		 *  \brief  Local instance of a dot NET ConcurrentQueue used as a FIFO for incoming CAT commands.
		 */

		/*! \var    _timer
		 *  \brief  dot NET threading timer provides polling interval for dequeueing CAT commands.  
		 */

		/*! \var    _port
		 *  \brief  A reference to the serial port receiving the CAT commands.  See app.config id="Cache"+n.
		 */

		/*! \fn CatCache 
		 *  \brief Gets an instance of the ObjectManager, creates a new cache, and sets up the polling timer.
		 *  \param  _om ObjectManager instance of the object manager.
		 *  \param  _queue ConcurrentQueue FIFO buffer for incoming CAT commands.
		 *  \param  _timer Timer polling timer for dequeueing CAT commands.
		 *  \param  timerDelegate TimerCallback delegate for timer event (OnTimer).
		 */

		/*! \fn AddCmd
		 *  \brief  Adds an incoming CAT command to the top of the queue.
		 *  \param  String cmd.
		 */

		/*! \fn OnTimer
		 *  \brief  Called 50 ms after timer start and every 10 ms thereafter.
		 */

		/*! \fn ProcessCommands
		 *  \brief  Starts the dequeueing process.  If the queue element is not null, the contents are passed to ProcessCommand.
		 */

		/*! \fn ProcessCommand
		  *  \brief  Sends the CAT command to the CatCmdProcessor for execution and writes the result to the serial port.
		  */

		#endregion Documentation

	}
}
