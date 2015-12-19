using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using log4net;
using Flex.Smoothlake.FlexLib;
using System.Threading;


namespace Cat.CatMain.Ports
{
	public class OTRSP
	{

		#region Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private Regex first_pass = new Regex(@"^(?'prefix'\?)*(?'keyword'[A-Z]{2,4})");
		//first_pass:  ^ starting at the beginning of the string, capture a group named 'prefix' if it contains a "?".
		//capture a group named 'keyword' that is alpha for a minimum of two and a maximum of four characters.
		//There are so many variations in the keyword values that is is easier just to parse them later.
		private ObjectManager _om;


		#endregion Variables

		#region Properties
		#endregion Properties

		#region Constructor

		public OTRSP()
		{
			_om = ObjectManager.Instance;
		}

		#endregion Constructor

		#region Methods

		public void ProcessCommand(string cmd)
		{
			try
			{
				//Debug.WriteLine("OTRSP received:  " + cmd);
				bool is_query = false;
				string keyword = string.Empty;
				string item = string.Empty;
				string value = string.Empty;

				Match m = first_pass.Match(cmd);
				if (m != null)
				{
					if (m.Groups["prefix"].Value.Contains("?"))
						is_query = true;

					keyword = m.Groups["keyword"].Value.ToUpper();
					//Debug.WriteLine("OTRSP Keyword:  " + keyword);
					switch (keyword)
					{
						case "RX":
							if (!is_query)
							{
								value = cmd.Substring(2);
							}
							else
								value = cmd.Substring(3);
							//Debug.WriteLine("Value:  " + value);
							switch (value)
							{
								case "1":	//radio1 to both headphones, radio1 focus
									_om.StateManager.Set("ActiveRX", "0");
									_om.StateManager.Set("AudioMute0", "0");
									_om.StateManager.Set("AudioMute1", "1");
									_om.StateManager.Set("AudioPan0", "050");
									break;
								case "2":	//radio2 to both headphones, radio2 focus
									_om.StateManager.Set("ActiveRX", "1");
									_om.StateManager.Set("AudioMute0", "1");
									_om.StateManager.Set("AudioMute1", "0");
									_om.StateManager.Set("AudioPan1", "050");
									break;
								case "1S":	//radio1 to left headphone, radio2 to right headphone, radio1 focus
									_om.StateManager.Set("ActiveRX", "0");
									_om.StateManager.Set("AudioMute0", "0");
									_om.StateManager.Set("AudioMute1", "0");
									_om.StateManager.Set("AudioPan0", "000");
									_om.StateManager.Set("AudioPan1", "100");
									break;
								case "2S":	//radio1 to left headphone, radio2 to right headphone, radio 2 focus
									_om.StateManager.Set("ActiveRX", "1");
									_om.StateManager.Set("AudioMute0", "0");
									_om.StateManager.Set("AudioMute1", "0");
									_om.StateManager.Set("AudioPan0", "000");
									_om.StateManager.Set("AudioPan1", "100");
									break;
								case "1R":	//radio2 to left headphone, radio1 to right headphone, radio1 focus
									_om.StateManager.Set("ActiveRX", "0");
									_om.StateManager.Set("AudioMute0", "0");
									_om.StateManager.Set("AudioMute1", "0");
									_om.StateManager.Set("AudioPan0", "100");
									_om.StateManager.Set("AudioPan1", "000");
									break;
								case "2R":	//radio2 to left headphone, radio1 to right headphone, radio2 focus
									_om.StateManager.Set("ActiveRX", "1");
									_om.StateManager.Set("AudioMute0", "0");
									_om.StateManager.Set("AudioMute1", "0");
									_om.StateManager.Set("AudioPan0", "100");
									_om.StateManager.Set("AudioPan1", "000");
									break;
								default:
									_log.Error("Fell thru value with " + value);
									break;
							}
							break;
						case "TX":
							value = string.Empty;
							string radio_value = string.Empty;
							value = cmd.Substring(2);
							if (value == "1")
								radio_value = "0";
							else if (value == "2")
								radio_value = "1";

                            // EW 2015-08: Ensure that the Interlock State is correct before allowing the transmit slice to change.
                            // Otherwise you get a pop-up message about not switching the antenna while transmitting sometimes due
                            // to trying to change this during the ramp down -- note that this happens even after the Mox has been
                            // set to false due to the ramp and other unkey timing.
                            //_om.StateManager
                            for (int i = 0; i < 50; i++)
                            {
                                if (_om.StateManager.Get("InterlockState") != "Ready")
                                    Thread.Sleep(5);
                                else
                                {
                                    _om.StateManager.Set("ActiveTX", radio_value);
                                    break;
                                }
                            }
							break;
						case "AUX":
							break;
						default:
							_log.Error("Fell thru keyword with " + keyword);
							break;
					}
				}
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}
		}

		private void ProcessQuery(string cmd)
		{
			//TBD
		}

		#endregion Methods
	}
}
