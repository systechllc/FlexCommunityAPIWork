﻿/*! \class Cat.Cat.CatStateManager
 * \brief       Interface between the API and CAT.
 * 
 *               Monitors property changes to/from from the radio and translates them to the appropriate CAT command.  
 *              
 * \author      Bob Tracy, K5KDN
 * \version     0.1 Beta
 * \date        01/2011
 * \copyright   FlexRadio Systems 
 */


using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Reflection;
using System.Timers;
using System.Diagnostics;
using log4net;
using Flex.Smoothlake.FlexLib;
using Cat.CatMain;
using Cat.Cat.Formatters;
using Cat.Cat.Ports;
using System.Threading;

namespace Cat.Cat
{
	internal class CatStateManager : ICatStateMgr
	{
		#region Local Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		//private static List<Radio> radio_list = new List<Radio>();
		private static Slice slice0 = null;
		private static Slice slice1 = null;
		private static Slice slice2 = null;
		private static Slice slice3 = null;
		private static Radio this_radio;
		private CatStatusWord csw = new CatStatusWord();
		private ConvertDemodMode cdm = new ConvertDemodMode();
		//private double split_offset = 0.005;
		private static bool AI = false;
		private static ObjectManager _om;
		private string separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
		//private CatSerialPort otrsp_port;

		#endregion Local Variables

		#region Constructor

		public CatStateManager()
		{
			_om = ObjectManager.Instance;
			//this.otrsp_port = SerialPortManager.OTRSPPort;
		}

		#endregion Constructor

		#region Properties

		/*! \var		SelectedSN
		 * \brief		Receives serial number changes from Main.cs and updates the radio information accordingly.	
		 */
 
		private string selected_sn;
		public string SelectedSN
		{
			get { return selected_sn; }
			set 
			{ 
				selected_sn = value;
				SNChanged(selected_sn);
			}
		}

		/*! \var		IFStatus
		 * \brief		Holds the current value of the Kenwood transceiver status command.  
		 */
				
		private static string if_status = "000141000001000+0000000000020000000";
		public string IFStatus
		{
			get { return if_status; }
			set { if_status = value; }
		}

		/*! \var		ZZIFStatus
		* \brief		Holds the current value of the FlexRadio transceiver status command.  
		*/

		private static string zzif_status = "000141000001000+00000000000010000000";
		public string ZZIFStatus
		{
			get { return zzif_status; }
			set { zzif_status = value; }
		}

		/*! \var		IF2Status
		* \brief		Holds the current value of the Kenwood transceiver number 2 status command (SO2R slice1).  
		*/

		private static string if2_status = "000141000001000+0000000000020000000";
		public string IF2Status
		{
			get { return if2_status; }
			set { if2_status = value; }
		}

		/*! \var		ZZIFStatus
		* \brief		Holds the current value of the FlexRadio transceiver number 2 status command (SO2R slice1).  
		*/

		private static string zzif2_status = "000141000001000+00000000000010000000";
		public string ZZIF2Status
		{
			get { return zzif2_status; }
			set { zzif2_status = value; }
		}

		private CatStatus current_status;		//this appears to be unused 3/30/2015 bt
		public CatStatus CurrentStatus
		{
			get { return current_status; }
			set { current_status = value; }
		}

		//private static int current_transmit_slice = 0;
		//public int CurrentTransmitSlice
		//{
		//    get { return current_transmit_slice; }
		//    set { current_transmit_slice = value; }
		//}

		private static Slice current_receive_slice = null;
		public static Slice CurrentReceiveSlice
		{
			get { return current_receive_slice; }
			set { current_receive_slice = value; }
		}

		private static float ksmeter_value;
		public static float KSMeterValue
		{
			get { return ksmeter_value; }
			set
			{
				// round to 1 dBm
				float new_val = (float)Math.Round(value, 0);
				new_val = Math.Max(-140, new_val);
				new_val = Math.Min(-10, new_val);
				//scale to kenwood 0 - 30
				double sx = (new_val + 140) / 4.3333;
				int sm = Math.Abs((int)sx);
				if (sm < 0) sm = 0;
				if (sm > 30) sm = 30;
				if (KSMeterValue != sm)
				{
					ksmeter_value = sm;
				}
			}
		}

		private static float fsmeter_value;
		public static float FSMeterValue
		{
			get { return fsmeter_value; }
			set
			{
				// round to 1 dBm
				float new_val = (float)Math.Round(value, 0);
				new_val = Math.Max(-140, new_val);
				new_val = Math.Min(-10, new_val);
				//scale to Flex 0 to 260 (-10 to -140 dBm) * 2
				float sm = (new_val + 140) * 2;
				if (sm < 0) sm = 0;
				if (sm > 260) sm = 260;
				if (FSMeterValue != sm)
				{
					fsmeter_value = sm;
				}
			}
		}

		//private static bool active_track = false;
		//public bool ActiveTrack
		//{
		//    get { return active_track; }
		//    set { active_track = value; }
		//}

		private double last_fb;
		public double LastFB
		{
			get { return last_fb; }
			set { last_fb = value; }
		}

		//private static bool center_panadapter_on_frequency_change = false;
		//public bool CenterOnFreqChange
		//{
		//    get { return center_panadapter_on_frequency_change; }
		//    set { center_panadapter_on_frequency_change = value; }
		//}

		public static Radio CSMRadio
		{
			get { return this_radio; }
		}

		private static bool is_so2r = false;
		public bool IsS02R
		{
			get { return is_so2r; }
			set { is_so2r = value; }
		}
		/// <summary>
		/// /////////////////////placeholder
		/// </summary>
		public static List<string> Slice0_AvailableNodes
		{
			get { return slice0.ModeList; }
		}

		#endregion Properties

		#region Methods

		/*! \var SNChanged
		 * \brief		Gets the radio matching the selected serial number from the API.RadioList.
		 */
 
		private void SNChanged(string sn)
		{
            Radio[] local_radio_list = new Radio[API.RadioList.Count];
            try
            {
                API.RadioList.CopyTo(local_radio_list);
            }
            catch (Exception)
            {

            }

            foreach (Radio radio in local_radio_list)
			{
				if (radio.Serial == SelectedSN)
				{
					this_radio = radio;
				}
			}
		}

        private Panadapter FindBestPanadapter(double freq)
        {
            // First, are there any Panadapters?  If not, we're done here.
            if (this_radio.PanadapterList.Count == 0) return null;
            
            Panadapter panadapter = null;
            // yes -- let's look through them to find the most appropriate one to use
            // Did the user specify a non-zero frequency?
            if (freq != 0.0)
            {
                // yes -- lets see if we can find a Panadapter that would show our new Slice
                foreach (Panadapter pan in this_radio.PanadapterList)
                {
                    if (freq >= pan.CenterFreq - pan.Bandwidth / 2 &&
                        freq <= pan.CenterFreq + pan.Bandwidth / 2)
                    {
                        panadapter = pan;
                        break;
                    }
                }
            }

            // Did we still need to find a good Panadapter to use?
            if (panadapter == null)
            {
                // Yes -- let's just find the one with the smallest StreamID
                uint min_stream_id = uint.MaxValue;
                foreach (Panadapter pan in this_radio.PanadapterList)
                {
                    if (pan.StreamID < min_stream_id)
                    {
                        panadapter = pan;
                        min_stream_id = pan.StreamID;
                    }
                }
            }

            return panadapter;
        }

        private bool VerifySliceA()
        {
            return VerifySliceA(0.0, "");
        }

        private bool VerifySliceA(double freq, string mode)
        {
            // are we talking to a radio?  If not, we're done.
            if (this_radio == null) return false;

            // If there is already a Slice A, then we don't need to do anything else
            if (slice0 != null) return true;

            // If we made it this far, we know we have a radio, but no Slice A.
            // So we need to create one

            // First, we should figure out the best panadapter 
            Panadapter pan = FindBestPanadapter(freq);

            // we have done our best to figure out a good Panadapter.  
            // If we still don't have one by now, that's OK as we will just pass a null
            // and the radio will handle it appropriately.

            // now we create Slice A
            slice0 = this_radio.CreateSlice(pan, mode, freq);

            // then let the radio know about our request
            slice0.RequestSliceFromRadio();

            // wait until the radio acknowledges the request
            int timeout_count = 0;
            while (!slice0.RadioAck)
            {
                // Have we been waiting too long?
                if (timeout_count++ > 300)
                    break;
                Thread.Sleep(10);
            }

            return slice0.RadioAck;
        }

        /// <summary>
        /// Ensures that a Slice B is present or it creates one if possible
        /// </summary>
        /// <param name="freq">The frequency to use in MHz if a Slice B needs to be created</param>
        /// <param name="mode">The mode to use if a Slice B needs to be created</param>
        /// <returns>True if a Slice B was created, False otherwise</returns>
        private bool VerifySliceB()
        {
            // are we talking to a radio?  If not, we're done.
            if (this_radio == null) return false;

            // If there is already a Slice B, then we don't need to do anything else
            if (slice1 != null) return false;

            // If we made it this far, we know we have a radio, but no Slice A.
            // So we need to create one

            // First, we need to verify Slice A
            //VerifySliceA(freq, mode);

            // Now, we should figure out the best panadapter 
            Panadapter pan = null;
            if(slice0 != null && slice0.Panadapter != null)
                pan = slice0.Panadapter;

            // we have done our best to figure out a good Panadapter.  
            // If we still don't have one by now, that's OK as we will just pass a null
            // and the radio will handle it appropriately.

            // now we create Slice B
            slice1 = this_radio.CreateSlice(pan, "");

            // then let the radio know about our request
            slice1.RequestSliceFromRadio();

            // wait until the radio acknowledges the request
            int timeout_count = 0;
            while (!slice1.RadioAck)
            {
                // Have we been waiting too long?
                if (timeout_count++ > 300)
                    break;
                Thread.Sleep(10);
            }

            if (slice1.RadioAck)
                last_fb = slice1.Freq;

            return slice1.RadioAck;
        }

		#region Radio Event Handlers

		public void SliceAdded(Slice slc)
		{
			//Debug.WriteLine("Slice added:  " + slc.ToString());
			switch (slc.Index)
			{
				case 0:
					if (slice0 == null)
					{
						slice0 = slc;
						slice0.PropertyChanged += new PropertyChangedEventHandler(slice0_PropertyChanged);
						slice0.SMeterDataReady += new Slice.SMeterDataReadyEventHandler(slice0_SMeterDataReady);
						//initialize the current_receive_slice
						if (current_receive_slice == null)
						{
							current_receive_slice = slice0;
							//Set("Freq0", slice0.Freq.ToString());
						}

						//string split_status = "00";
						//if (slice0 != null && slice1 == null && slice0.Transmit)
						//{
						//    split_status = "00";
						//    this.CurrentTransmitSlice = 0;
						//}
						////else if (slice0 != null && slice1 != null && slice0.Transmit)
						////{
						////    split_status = "10";
						////    this.CurrentTransmitSlice = 0;
						////}
						//else if (slice0 != null && slice1 != null && slice1.Transmit)
						//{
						//    split_status = "01";
						//    this.CurrentTransmitSlice = 1;
						//}
						//string[] args = { "Transmit", split_status };
						//csw.Update(args);
						//UpdateStatusWord();

						//Get("DemodMode"); // EW 2014-12: ensure that the IF status word is updated with accurate mode info
						//Get("RITOn");
						//Get("RITFreq");
						//Get("XITOn");
						//Get("XITFreq");
						//Get("Freq0");
						TransceiverStatus status = new TransceiverStatus(slc, this_radio.CWL_Enabled);
						this.IFStatus = status.KWStatus;
						this.ZZIFStatus = status.FlexStatus;
					}
					break;
				case 1:
					if (slice1 == null)
					{
						slice1 = slc;
						slice1.PropertyChanged += new PropertyChangedEventHandler(slice1_PropertyChanged);

						//Get("DemodMode", 1);
						//Get("RITOn", 1);
						//Get("RITFreq", 1);
						//Get("XITOn", 1);
						//Get("XITFreq", 1);
						//Get("Freq", 1);
						TransceiverStatus status = new TransceiverStatus(slc, this_radio.CWL_Enabled);
						this.IF2Status = status.KWStatus;
						this.ZZIF2Status = status.FlexStatus;
						//Debug.WriteLine("slice 1 added");
					}
					break;
				case 2:
					if (slice2 == null)
					{
						slice2 = slc;
						slice2.PropertyChanged += new PropertyChangedEventHandler(slice2_PropertyChanged);
					}
					break;
				case 3:
					if (slice3 == null)
					{
						slice3 = slc;
						slice3.PropertyChanged += new PropertyChangedEventHandler(slice3_PropertyChanged);
					}
					break;
				default:
					_log.Error("Couldn't add slice handler for " + slc.Index.ToString());
					break;
			}
		}

		void slice0_SMeterDataReady(float data)
		{
			KSMeterValue = data;
			FSMeterValue = data;
		}

		public void SliceRemoved(Slice slc)
		{
			//Debug.WriteLine("Slice removed:  " + slc.ToString());
			switch (slc.Index)
			{
				case 0:
					if (slice0 != null)
					{
						slice0.PropertyChanged -= new PropertyChangedEventHandler(slice0_PropertyChanged);
						slice0 = null;
					}
					break;
				case 1:
					if (slice1 != null)
					{
						slice1.PropertyChanged -= new PropertyChangedEventHandler(slice1_PropertyChanged);
						slice1 = null;
					}
					break;
				case 2:
					if (slice2 != null)
					{
						slice2.PropertyChanged -= new PropertyChangedEventHandler(slice2_PropertyChanged);
						slice2 = null;
					}
					break;
				case 3:
					if (slice3 != null)
					{
						slice3.PropertyChanged -= new PropertyChangedEventHandler(slice3_PropertyChanged);
						slice3 = null;
					}
					break;
				default:
					_log.Error("Couldn't remove slice handler for " + slc.Index.ToString());
					break;
			}

		}

		internal void RadioPropertyChanged(string prop_name)
		{
			Get(prop_name);
		}

		/*! \var	slice[n]_PropertyChanged
		 * \brief	Event handlers for slice property changes.
		 */

		void slice0_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//Debug.WriteLine("CSM:  Slice0 Property Change Event:  " + e.PropertyName);
			Get(e.PropertyName, 0);
		}

		void slice1_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//Debug.WriteLine("CSM:  Slice1 Property Change Event:  " + e.PropertyName);
			Get(e.PropertyName, 1);
		}

		void slice2_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//Debug.WriteLine("CSM:  Slice2 Property Change Event:  " + e.PropertyName);
			Get(e.PropertyName, 2);
		}

		void slice3_PropertyChanged(object sender, PropertyChangedEventArgs e)
		{
			//Debug.WriteLine("CSM:  Slice3 Property Change Event:  " + e.PropertyName);
			Get(e.PropertyName, 3);
		}


		#endregion Radio Event Handlers


		/*! \var    Set
		 *  \brief      Selects on the Key (API = value token in the CAT command XML file) and routes the value to the appropriate
		 *				API call to set a radio property state.  Keys prefixed with "int_" are internal CAT functions.
		 */

		public string Set(string Key, string Value)
		{
			string result = string.Empty;
			//string[] args = null;
			string[] args = new string[] { string.Empty, string.Empty, string.Empty };
			double nfreq;

			switch (Key)
			{
				case "int_AI":
				case "int_ZZAI":
					if (Value == "1")
						AI = true;
					else
						AI = false;
					break;
				case "int_IF":
					this.IFStatus = Value;
					break;
				case "int_ZZIF":
					this.ZZIFStatus = Value;
					break;
				case "int_ZZIX":
					this.ZZIF2Status = Value;
					break;
				case "Active":
					break;
				case "ActiveRX":								// works like Kenwood FR incoming CAT command
					//Debug.WriteLine("Set ActiveRX fired");
					if (slice0 != null && Value == "0")
						slice0.Active = true;
					else if (slice1 != null && Value == "1")
						slice1.Active = true;

					if (!is_so2r)
					{

						args = new string[] { "ActiveRX", Value, "0" };
						csw.Update(args);
						args = new string[] { "ActiveRX", Value, "1" };
						csw.Update(args);
					}
					else if (is_so2r)
					{
						args = new string[] { "ActiveRX", "0", "0" };		//never split in so2r
						csw.Update(args);
						args = new string[] { "ActiveRX", "0", "1" };
						csw.Update(args);

						//otrsp_port.Write("$RX" + Value);
					}

					break;
				case "ActiveRXToggle":							// toggles active receive slice from incoming CAT command
					string rx_state = "0";
                    if (slice0 != null && slice1 != null && slice0.Active)
					{
						slice1.Active = true;
						rx_state = "1";
					}
					else if (slice0 != null && slice1 != null && slice1.Active)
					{
						slice0.Active = true;
						rx_state = "0";
					}
					args = new string[] { "ActiveRX", rx_state, "0" };
					csw.Update(args);
					args = new string[] { "ActiveRX", rx_state, "1" };
					csw.Update(args);
					break;
				case "ActiveTX":								// works like Kenwood FT from incoming CAT command
					if (slice0 != null && Value == "0")
						slice0.Transmit = true;
					else if (slice1 != null && Value == "1")
						slice1.Transmit = true;

					if (!is_so2r)											// normal split status word
					{
						args = new string[] { "ActiveTX", Value, "0" };
						csw.Update(args);
						args = new string[] { "ActiveTX", Value, "1" };
						csw.Update(args);
					}
					else if (is_so2r)
					{
						args = new string[] { "ActiveTX", "0", "0" };		//never split in so2r
						csw.Update(args);
						args = new string[] { "ActiveTX", "0", "1" };
						csw.Update(args);
					}
					break;
				case "ActiveTXToggle":							// toggles active transmitter flag between slice0 and 1 from incoming CAT change
					string tx_state = "0";
					if (slice0 != null && slice1 != null && slice0.Transmit)
					{
						slice1.Transmit = true;
						tx_state = "1";
					}
					else if (slice0 != null && slice1 != null && slice1.Transmit)
					{
						slice0.Transmit = true;
						tx_state = "0";
					}
					args = new string[] { "ActiveTX", tx_state, "0" };
					csw.Update(args);
					args = new string[] { "ActiveTX", tx_state, "1" };
					csw.Update(args);
					break;
				case "AFGain0":
					if (slice0 != null)
						slice0.AudioGain = int.Parse(Value);
					break;
				case "AFGain1":
					if (slice1 != null)
						slice1.AudioGain = int.Parse(Value);
					break;
				case "AGCModeK":
				case "AGCModeF":
					if (slice0 != null)
						slice0.AGCMode = StringToAGCMode(Value);
					break;
				case "AGCThreshold0":
					if (slice0 != null)
						slice0.AGCThreshold = int.Parse(Value);
					break;
				case "AudioMute0":
                    if (slice0 != null)
                    {
                        if (Value == "1")
                            slice0.Mute = true;
                        else if(Value == "0")
                            slice0.Mute = false;
                    }
					break;
				case "AudioMute1":
                    if (slice1 != null)
                    {
                        if (Value == "1")
                            slice1.Mute = true;
                        else if (Value == "0")
                            slice1.Mute = false;
                    }
					break;
				case "AudioPan":
					break;
				case "AudioPan0":
                    if(slice0 != null)
    					slice0.AudioPan = int.Parse(Value);
					break;
				case "AudioPan1":
					if (slice1 != null)
						slice1.AudioPan = int.Parse(Value);
					break;
				case "BinauralRX":
                    if (this_radio != null)
                    {
                        if (Value == "0")
                            this_radio.BinauralRX = false;
                        else if (Value == "1")
                            this_radio.BinauralRX = true;
                    }
					break;
				case "CWSend":
					if (this_radio != null)
						this_radio.GetCWX().Send(Value.Substring(1));
					break;
				case "CWSpeed":
					if (this_radio != null)
						this_radio.CWSpeed = int.Parse(Value);
					break;
				case "DemodMode0K":
				case "DemodMode0F":
					if (slice0 != null)
					{
						if (Value == "CWL")
						{
							slice0.DemodMode = "CW";
							this_radio.CWL_Enabled = true;
						}
						else if (Value == "CWU" || Value == "CW")
						{
							slice0.DemodMode = "CW";
							this_radio.CWL_Enabled = false;
						}
						else slice0.DemodMode = Value;
					}
					break;
				case "DemodMode1F":
					if (slice1 != null)
					{
						if (Value == "CWL")
						{
							slice1.DemodMode = "CW";
							this_radio.CWL_Enabled = true;
						}
						else if (Value == "CWU" || Value == "CW")
						{
							slice1.DemodMode = "CW";
							this_radio.CWL_Enabled = false;
						}
						else slice1.DemodMode = Value;
					}
					break;
				case "Diversity":
					if (slice0 != null && this_radio.Model.Contains("6700"))
					{
						if (Value == "1")
							slice0.DiversityOn = true;
						else
							slice0.DiversityOn = false;
					}
					break;
				case "Freq0":
					nfreq = Double.Parse(Value) / 10e5;
					if (slice0 != null)
						slice0.Freq = nfreq;
					//if (center_panadapter_on_frequency_change) 
					//     slice0.Panadapter.CenterFreq = Math.Round(nfreq, 3);
					break;
				case "Freq1":
					nfreq = Double.Parse(Value) / 10e5;
					if (slice1 != null)
					{
						slice1.Freq = nfreq;
						last_fb = nfreq;
					}
					break;
				case "KWFilterHi":
					if (slice0 != null)
						slice0.FilterHigh = int.Parse(Value);
					break;
				case "KWFilterLo":
					if (slice0 != null)
						slice0.FilterLow = int.Parse(Value);
					break;
				case "MicLevel":
					if (this_radio != null)
						this_radio.MicLevel = int.Parse(Value);
					break;
				case "Mox":
					//if (this_radio != null && this.CurrentTransmitSlice <= 1)
					if (this_radio != null)
					{
						if (Value == "1")
						{
							this_radio.Mox = true;
						}
						else
						{
							this_radio.Mox = false;
						}
					}
					//Debug.WriteLine("MOX: " + this_radio.Mox.ToString());
					break;
				case "MultiRX":
					//TODO:  Figure out what to do here
					break;
				case "Name":
					break;
				case "NBLevel0":
					if (slice0 != null)
						slice0.NBLevel = int.Parse(Value);
					break;
				case "NROn":
					if (slice0 != null)
					{
						if (Value == "1")
							slice0.NROn = true;
						else
							slice0.NROn = false;
					}
					break;
				case "Owner":
					break;
				case "PanadapterCenterFreq":
					nfreq = Double.Parse(Value) / 10e5;
					if (current_receive_slice != null)
						current_receive_slice.Panadapter.CenterFreq = nfreq;
					break;
				case "PowerOn":
					break;
				case "RFPower":
					if (this_radio != null)
						this_radio.RFPower = int.Parse(Value);
					break;
				case "RITClear":
					if (slice0 != null)
						slice0.RITFreq = 0;
					break;
				case "RITDown":
					if (slice0 != null)
					{
						int freq_change = 0;
						if (Value == string.Empty)
						{
							switch (slice0.DemodMode.ToUpper())
							{
								case "USB":
								case "LSB":
								case "AM":
									freq_change = 50;
									break;
								case "CW":
								case "DIGL":
								case "DIGU":
									freq_change = 10;
									break;
								default:
									_log.Error("Fell thru RITDown");
									break;
							}
						}
						else
							freq_change = int.Parse(Value);

						slice0.RITFreq = slice0.RITFreq - freq_change;
					}
					break;
				case "RITFreq":
					if (slice0 != null)
						slice0.RITFreq = int.Parse(Value);
					break;
				case "RITFreqB":
					if (slice1 != null)
						slice1.RITFreq = int.Parse(Value);
					break;
				case "RITOn":
					if (slice0 != null)
					{
						if (Value == "1")
							slice0.RITOn = true;
						else
							slice0.RITOn = false;
					}
					break;
				case "RITOnB":
					if (slice1 != null)
					{
						if (Value == "1")
							slice1.RITOn = true;
						else
							slice1.RITOn = false;
					}
					break;
				case "RITUp":
					if (slice0 != null)
					{
						int freq_change = 0;
						if (Value == string.Empty)
						{
							switch (slice0.DemodMode.ToUpper())
							{
								case "USB":
								case "LSB":
								case "AM":
									freq_change = 50;
									break;
								case "CW":
								case "DIGL":
								case "DIGU":
									freq_change = 10;
									break;
								default:
									_log.Error("Fell thru RITUp");
									break;
							}
						}
						else
							freq_change = int.Parse(Value);

						slice0.RITFreq = slice0.RITFreq + freq_change;
					}
					break;
				case "RX":
					if (this_radio != null)
					{
						this_radio.Mox = false;
						if (is_so2r)
						{
							if (slice0 != null && slice0.Active)
								args = new string[] { "RX", "0", "0" };	//update IF/ZZIF
							else if (slice1 != null && slice1.Active)
								args = new string[] { "RX", "0", "1" };	//update IF2/ZZIF2
						}
						else
							args = new string[] { "RX", "0", "0" };		//just update IF/ZZIF
						csw.Update(args);
					}
					break;
				case "RXFilter0":
					if (slice0 != null)
					{
						string[] lohi = Value.Split('#');
						slice0.UpdateFilter(int.Parse(lohi[0]), int.Parse(lohi[1]));
					}
					break;
				case "RXFilter1":
					if (slice1 != null)
					{
						string[] lohi = Value.Split('#');
						slice1.UpdateFilter(int.Parse(lohi[0]), int.Parse(lohi[1]));
					}
					break;
				case "RXVFO":
					if (slice0 != null && Value == "0")
						slice0.Active = true;
					else if (slice1 != null && Value == "1")
						slice1.Active = true;
					break;
				case "TX":
					if (this_radio != null)
					//if (this_radio != null && this.CurrentTransmitSlice <= 1)
					{
						this_radio.Mox = true;
						if (is_so2r)
						{
							if (slice0 != null && slice0.Transmit)
								args = new string[] { "TX", "1", "0" };		//update IF/ZZIF
							else if (slice1 != null && slice1.Transmit)
								args = new string[] { "TX", "1", "1" };		//update IF2/ZZIF2
						}
						else
							args = new string[] { "TX", "1", "0" };			//just update IF/ZZIF
						csw.Update(args);
					}
					break;
				case "TXVFO":
					if (Value == "0")
					{
						if (slice0 != null)
							slice0.Transmit = true;
					}
					else if (Value == "1")
					{
						// create a Slice B if necessary
						VerifySliceB();
						if (slice1 != null)
							slice1.Transmit = true;
					}

					//Debug.WriteLine("slice0 = " + slice0.Transmit.ToString() + "   slice1 = " + slice1.Transmit.ToString());
					break;
				case "XITClear":
					if (slice0 != null)
						slice0.XITFreq = 0;
					break;
				case "XITFreq":
					if (slice0 != null)
						slice0.XITFreq = int.Parse(Value);
					break;

				case "XITOn":
					if (slice0 != null)
					{
						if (Value == "1")
							slice0.XITOn = true;
						else
							slice0.XITOn = false;
					}
					break;

				default:
					_log.Error(this);
					result = "?;";
					break;
			}
			return result;
		}

		/*! \var    Get
		 *  \brief  Filters on the Key (token from the CAT command XML file) and 
		 *			creates API call to read a radio property state.
		 */
		public string Get(string key)
		{
			return Get(key, 0);
		}

		//Translates the CAT XML file token to API property name where required.
	 
		public string Get(string key, int slc)
		{
			string selection = string.Empty;
			string[] args;
				string[] parts;
				string result = "?;";
				Slice slice_to_use = null;
				if (slc == 0)
					slice_to_use = slice0;
				else if (slc == 1)
					slice_to_use = slice1;

				switch (key)
				{
					case "Active":
						//Debug.WriteLine("Get Active fired");
						if (!is_so2r)
						{
							if (slice0 != null && slice0.Active)
							{
								args = new string[] { "ACTIVERX", "0", "0" };
								csw.Update(args);
								args = new string[] { "ACTIVERX", "0", "1" };
								csw.Update(args);
							}
							else if (slice1 != null && slice1.Active)
							{
								args = new string[] { "ACTIVERX", "1", "1" };
								csw.Update(args);
								args = new string[] { "ACTIVERX", "1", "0" };
								csw.Update(args);
							}
						}
						else if (is_so2r)
						{
							args = new string[] { "ActiveRX", "0", "0" };
							csw.Update(args);
							args = new string[] { "ActiveRX", "0", "1" };
							csw.Update(args);
						}
						result = "";
						break;
					case "ActiveRX":
						Debug.WriteLine("Get ActiveRX fired");
						if (slice0 != null && slice0.Active)
							result = "0";
						else if (slice1 != null && slice1.Active)
							result = "1";
						break;
					case "ActiveTX":
						if (slice0 != null && slice0.Transmit)
							result = "0";
						else if (slice1 != null && slice1.Transmit)
							result = "1";
						break;
                    case "ActiveSlice":
					case "ACCOn":
						break;
					case "AFGain0":
                        if (slice0 != null)
							result = slice0.AudioGain.ToString();
						//result = current_receive_slice.AudioGain.ToString();
						break;
					case "AFGain1":
						if(slice1 != null)
							result = slice1.AudioGain.ToString();
						break;
					case "AFPan0":
                        if (slice0 != null)
							result = slice0.AudioPan.ToString();
						//result = current_receive_slice.AudioPan.ToString();
						break;
					case "AGCMode":
						break;
					case "AGCModeF":
                        if (slice0 != null)
							result =  slice0.AGCMode.ToString().ToLower();
						//result = current_receive_slice.AGCMode;
						break;
					case "AGCModeK":
                        if (slice0 != null)
							result = slice0.AGCMode.ToString().ToLower();
						//result = current_receive_slice.AGCMode;
						break;
					case "AGCOffLevel":
                        if (slice0 != null)
							result = slice0.AGCOffLevel.ToString();
						//result = current_receive_slice.AGCOffLevel.ToString();
						break;
					case "AGCThreshold":
						break;
					case "AGCThreshold0":
                        if (slice0 != null)
							result = "+" + slice0.AGCThreshold.ToString();
						//result = current_receive_slice.AGCThreshold.ToString();
						break;
					case "AGCThreshold1":
                        if (slice0 != null)
							result = "+" + slice1.AGCThreshold.ToString();
						//result = current_receive_slice.AGCThreshold.ToString();
						break;
					case "AMCarrierLevel":
						break;
					case "ANFOn":
                        if (slice0 != null)
							result = slice0.ANFOn.ToString();
						//result = current_receive_slice.ANFOn.ToString();
						break;
					case "ANFLevel":
                        if (slice0 != null)
							result = slice0.ANFLevel.ToString();
						//result = current_receive_slice.ANFLevel.ToString();
						break;
                    case "APFLevel":
                    case "APFOn":
					case "ATUEnabled":
					case "ATUMemoriesEnabled":
                    case "ATUPresent":
 					case "ATUTuneStatus":
					case "ATUUsingMemory":
						break;
					case "AudioGain":
                        if (slice_to_use != null)
                        {
                            if (slice_to_use.Index == 0)
                                result = slice_to_use.AudioGain.ToString();
                            else if (slice_to_use.Index == 1)
                                result = slice_to_use.AudioGain.ToString();
                        }
						break;
					case "AudioMute0":
                        if (slice0 != null)
                        {
                            if (slice0.Mute)
                                result = "1";
                            else
                                result = "0";
                        }
						break;
					case "AudioMute1":
						if (slice1 != null)
						{
							if (slice1.Mute)
								result = "1";
							else
								result = "0";
						}
						break;
					case "AudioPan":
						break;
					case "AudioPan0":
                        if(slice0 != null)
    						result = slice0.AudioPan.ToString();
						break;
					case "AudioPan1":
						if (slice1 != null)
							result = slice1.AudioPan.ToString();
						break;
					case "BinauralRX":
                        if (this_radio != null)
                        {
                            if (this_radio.BinauralRX)
                                result = "1";
                            else
                                result = "0";
                        }
						break;
					case "CalFreq":
                    case "Callsign":
					case "Connected":
					case "ConnectedState":
						break;
					case "CWSpeed":
						if (this_radio != null)
							result = this_radio.CWSpeed.ToString();
						break;
					case "ClientID":
					case "CompanderLevel":
					case "CompanderOn":
					case "CWBreakIn":
					case "CWDelay":
					case "CWIambic":
					case "CWIambicModeB":
					case "CWL_Enabled":
						break;
					case "CWPitch":
                        if(this_radio != null)
    						result = this_radio.CWPitch.ToString();
						break;
                    case "CWSend":
                        result = "0";
                        break;
					case "CWSidetone":
					case "CWSwapPaddles":
					case "DavinciFirmwareVersion":
					case "DAXChannel":
                    case "DAXOn":
					case "DelayTX":
					case "DelayTX1":
					case "DelayTX2":
					case "DelayTX3":
					case "DelayTXACC":
						break;
					case "DemodMode":
                        //if (slice0 != null)
						if(slice_to_use != null)	
						{
							object[] args_in = { "ZZMD", slice_to_use.DemodMode, "DemodMode" };
							string[] ans = cdm.Format(args_in).Split(':');
                            if (ans.Length > 1)
                            {
                                string FMode = ans[1];
                                args = new string[]{ "DemodMode", FMode, slc.ToString() };
                                csw.Update(args);
                            }
						}
						break;
					case "DemodMode0K":
                        if (slice0 != null)
							result = slice0.DemodMode.ToString();
                        if (result == "CW" && this_radio.CWL_Enabled)
                            result = "CWL";
						//result = current_receive_slice.DemodMode.ToString();
						break;
					case "DemodMode0F":
                        if (slice0 != null)
							result = slice0.DemodMode.ToString();
                        if (result == "CW" && this_radio.CWL_Enabled)
                            result = "CWL";
						//result = current_receive_slice.DemodMode.ToString();
						break;
					case "DemodMode1F":
                        if (slice1 != null)
							result = slice1.DemodMode.ToString();
                        if (result == "CW" && this_radio.CWL_Enabled)
                            result = "CWL";
						//result = current_receive_slice.DemodMode.ToString();
						break;
                    case "DiscoveryProtocolVersion":
                        break;
					case "Diversity":
						if(slice0 != null)
						{
							if(slice0.DiversityOn)
								result = "1";
							else
								result = "0";
						}
						break;
					case "FilterLow":
                        if (slice0 != null)
							result = slice0.FilterLow.ToString();
						//result = current_receive_slice.FilterLow.ToString();
						break;
					case "FilterHigh":
                        if (slice0 != null)
							result = slice0.FilterHigh.ToString();
						//result = current_receive_slice.FilterHigh.ToString();
						break;
					case "FPGAVersion":
						break;
					case "Freq":
						string freq = string.Empty;
						if (slice_to_use != null)
						{
							//Get the frequency of the affected slice
							//if (slc == 0)
							//    freq = slice0.Freq.ToString();
							//else if (slc == 1)
							//{
							//    freq = slice1.Freq.ToString();
							//    if (last_fb == 0.0)
							//        last_fb = slice1.Freq;
							//}
							freq = slice_to_use.Freq.ToString();
							if (last_fb == 0.0 && slc == 1)
								last_fb = slice_to_use.Freq;
							//Get the correct separator for the locale
							if (freq.Contains(separator))
								parts = freq.Split(Convert.ToChar(separator));
							else
								parts = new string[] { freq.ToString(), "000000" };
							//Set the frequency format and create a parameter array
							if (parts[1].Length > 6)
								parts[1] = parts[1].Substring(0, 6);
							result = parts[0] + parts[1].PadRight(6, '0');
							args = new string[]{ "Freq", result, slc.ToString() };
							//Update the Cat Status Word only if slice0
							//if (slc == 0)
							//Debug.WriteLine("slice_to_use:  " + slice_to_use.Index.ToString());
							if(slice_to_use != null)
								csw.Update(args);
							//Debug.WriteLine("Frequency:  "+args[0]+"  "+args[1]+"  "+args[2]);
							//Broadcast change if AI enabled
							if (AI && (slc == 0 || slc == 1))
								SendAI(args[1], slc);
						}
						break; ;
					case "Freq0":
                        if (slice0 != null)
						{
							if (slice0.Freq.ToString().Contains(separator))
								parts = slice0.Freq.ToString().Split(Convert.ToChar(separator));
							else
								parts = new string[] { slice0.Freq.ToString(), "000000" };
							if (parts[1].Length > 6)
								parts[1] = parts[1].Substring(0, 6);
							result = parts[0] + parts[1].PadRight(6, '0');
						}
						break;
					case "Freq1":
                        if (slice1 != null)
						{
							if (slice1.Freq.ToString().Contains(separator))
								parts = slice1.Freq.ToString().Split(Convert.ToChar(separator));
							else
								parts = new string[] { slice1.Freq.ToString(), "000000" };
							if (parts[1].Length > 6)
								parts[1] = parts[1].Substring(0, 6);
							result = parts[0] + parts[1].PadRight(6, '0');
						}
						else
							result = "?;";
						break;
					case "FreqErrorPPB":
						break;
					case "FSmeter":
                        if (slice0 != null)
							result = fsmeter_value.ToString();
						break;
					case "FullDuplexEnabled":
						break;
					case "Gain":
						break;
					case "GPSAltitude":
					case "GPSFreqError":
					case "GPSGrid":
					case "GPSInstalled":
					case "GPSLatitude":
					case "GPSLongitude":
					case "GPSSatellitesTracked":
					case "GPSSatellitesVisible":
					case "GPSSpeed":
					case "GPSStatus":
					case "GPSUtcTime":
					case "HWAlcEnabled":
					case "HeadphoneGain":
					case "HeadphoneMute":
						break;
					case "ID":
                        if (this_radio != null)
                        {
                            if (this_radio.Model == "FLEX-6700")
                                result = "904";
                            else if (this_radio.Model == "FLEX-6500")
                                result = "905";
                            else if (this_radio.Model == "FLEX-6700R")
                                result = "906";
                            else if (this_radio.Model == "FLEX-6300")
                                result = "907";
                            else
                                result = "000";
                        }
						break;
					case "InterlockReason":
                        break;
					case "InterlockState":
                        result = this_radio.InterlockState.ToString();
                        break;
					case "InterlockTimeout":
					case "InUseHost":
					case "InUseIP":
						break;
					case "KPitch":
                        if (this_radio != null)
                        {
                            int cw_pitch = this_radio.CWPitch;
                            cw_pitch = Math.Min(cw_pitch, 1100);
                            cw_pitch = Math.Max(cw_pitch, 300);
                            int kw_pitch = (cw_pitch - 300) / 10;
                            result = kw_pitch.ToString();
                        }
						break;
					case "KSmeter":
                        if (slice0 != null)
							result = ksmeter_value.ToString();
						break;
					case "KWFilterHi":
						if (slice0 != null)
						{
							if (slice0.DemodMode == "LSB" ||
								slice0.DemodMode == "DIGL" ||
								(slice0.DemodMode == "CW" && this_radio.CWL_Enabled))
								result = slice0.FilterLow.ToString("F");
							else
								result = slice0.FilterHigh.ToString("F");
						}
						break;
					case "KWFilterLo":
						if (slice0 != null)
						{
							if (slice0.DemodMode == "LSB" || 
								slice0.DemodMode == "DIGL" || 
								slice0.DemodMode == "CWL")
								result = slice0.FilterHigh.ToString("F");
							else
								result = slice0.FilterLow.ToString("F");
						}
						break;
					case "LineoutGain":
					case "LineoutMute":
                    case "Lock":
					case "LoopA":
					case "LoopB":
					case "MemoryList":
					case "MeterPacketErrorCount":
					case "MeterPacketTotalCount":
					case "MetInRX":
					case "MicBias":
					case "MicBoost":
                    case "MicInputList":
						break;
					case "MicLevel":
						if(this_radio != null)
							result = this_radio.MicLevel.ToString();
						break;
					case "MicInput":
					case "ModeList":
						break;
					case "Mox":
                        if (this_radio != null)
                        {
							if(this_radio != null)
                            //if (this_radio.Mox && this.CurrentTransmitSlice <= 1)
                               result = "1";
                            else
                                result = "0";
                        }
						break;
					case "MultiRX":
						result = "0";
						break;
					case "Mute":
						break;
					case "Name":
						break;
					case "NBOn":
                        if (slice0 != null)
						{
							if (slice0.NBOn)
								result = "1";
							else
								result = "0";
						}
						break;
					case "NBLevel":
						break;
					case "NBLevel0":
                        if (slice0 != null)
							result = slice0.NBLevel.ToString();
						break;
					case "NBLevel1":
                        if (slice1 != null)
							result = slice1.NBLevel.ToString();
						break;
                    case "Nickname":
                        break;
					case "NROn":
                        if (slice0 != null)
						{
							if (slice0.NROn)
								result = "1";
							else
								result = "0";
						}
						break;
					case "NRLevel":
					case "NumTX":
					case "Owner":
					case "Panadapter":
						break;
					case "PanadapterCenterFreq":
                        if (current_receive_slice != null)
                        {
                            if (current_receive_slice.Panadapter.CenterFreq.ToString().Contains(separator))
                                parts = current_receive_slice.Panadapter.CenterFreq.ToString().ToString().Split(Convert.ToChar(separator));
                            else
                                parts = new string[] { current_receive_slice.Panadapter.CenterFreq.ToString(), "000000" };
                            if (parts[1].Length > 6)
                                parts[1] = parts[1].Substring(0, 6);
                            result = parts[0] + parts[1].PadRight(6, '0');
                        }
						break;
					case "PanadapterList":
						break;
					case "PanadaptersRemaining":
					case "PAPsocVersion":
                    case "PlayEnabled":
 					case "PowerOn":		//for so2r to stop logging errors
                    case "ProfileGlobalList":
                        break;
					case "ProfileGlobalSelection":
                        if (this_radio != null)
                       {
                            selection = this_radio.ProfileGlobalSelection;
							if (selection != null && (selection.ToLower().Contains("so2r")))
							{
								Thread.Sleep(1000);
								Cat.CatStateManager.is_so2r = true;
								this_radio.FullDuplexEnabled = true;
								//Debug.WriteLine("this_radio.SO2REnabled = " + this_radio.SO2REnabled.ToString());
							}
							else
							{
								Cat.CatStateManager.is_so2r = false;
							}
                        }
                        //Debug.WriteLine("PGS:  " + selection + "  " + is_so2r.ToString());
						break;
					case "ProfileMicSelection":
                    case "ProfileTXList":
					case "ProfileTXSelection":
                    case "ProfileMicList":
                    case "ProfileDisplayList":
					case "PTTSource":
					case "QSK":
                    case "RadioOptions":
                    case "RecordOn":
 					case "RegionCode":
					case "RemoteOnEnabled":
						break; 
					case "RITFreq":
						//Added changes 12/30/14 BT Add RIT/XIT to status word.
						//If RIT is enabled the frequency will be reported, otherwise zeros are reported.
						int rit_freq = 0;
						string rit_sign = "+";
						//if (slice0 != null && slice0.RITOn)
						if(slice_to_use != null)
						{
							//result = slice0.RITFreq.ToString();
							//rit_freq = slice0.RITFreq;
							rit_freq = slice_to_use.RITFreq;
							result = rit_freq.ToString();
							if (rit_freq < 0)
							{
								rit_sign = "-";
								rit_freq = Math.Abs(rit_freq);
							}
						}
						string out_freq = rit_sign + rit_freq.ToString().PadLeft(5, '0');
						args = new string[]{ "RITFreq", out_freq, slc.ToString() };
						csw.Update(args);

						break;
					case "RITOn":
                        //if (slice0 != null)
						if(slice_to_use != null)
						{
							//if (slice0.RITOn)
							if(slice_to_use.RITOn)
								result = "1";
							else
								result = "0";

							// Added changes 12/30/14 BT Add RIT/XIT to status word.
							args = new string[]{ "RITStat", result, slc.ToString() };
							csw.Update(args);
						}
						break;
					case "RFPower":
                        if(this_radio != null)
    						result = this_radio.RFPower.ToString();
						break;
					case "RXAnt":
                        if (slice0 != null)
							result = slice0.RXAnt.ToString();
						break;
					case "RXAntList":
						break;
					case "RXFilter0":
						string lo = string.Empty;
						string hi = string.Empty;

                        if (slice0 != null)
						{
							lo = Math.Round((double)slice0.FilterLow, 0).ToString();
							hi = Math.Round((double)slice0.FilterHigh, 0).ToString();
							result = lo + ":" + hi;
						}
						break;
					case "RXFilter1":
						lo = string.Empty;
						hi = string.Empty;

                        if (slice1 != null)
						{
							lo = Math.Round((double)slice1.FilterLow, 0).ToString();
							hi = Math.Round((double)slice1.FilterHigh, 0).ToString();
							result = lo + ":" + hi;
						}
						break;
					case "SliceList":
					case "SlicesRemaining":
					case "SimpleVOXEnable":
					case "SimpleVOXDelay":
					case "SimpleVOXLevel":
					case "SimpleVOXVisible":
                    case "Screensaver":
                    case "ShowTxInWaterfall":
						break;
					case "SO2REnabled":
						Debug.WriteLine("SO2REnable = " + this_radio.FullDuplexEnabled.ToString());
						break;
                    case "SnapTune":
                    case "SpeechProcessorEnable":
                    case "SpeechProcessorLevel":
                    case "Status":
                     case "SubnetMask":
 					case "SyncCWX":
                    case "TNFEnabled":
                        break;
					case "Transmit":						//updates tx split status word for local console tx flag change
						if (!is_so2r)
						{
							if (slice0 != null && slice0.Transmit)
							{
								args = new string[] { "ACTIVETX", "0", "0" };
								csw.Update(args);
								args = new string[] { "ACTIVETX", "0", "1" };
								csw.Update(args);
							}
							else if (slice1 != null && slice1.Transmit)
							{
								args = new string[] { "ACTIVETX", "1", "0" };
								csw.Update(args);
								args = new string[] { "ACTIVETX", "1", "1" };
								csw.Update(args);
							}
						}
						else if (is_so2r)
						{
							args = new string[] { "ActiveTX", "0", "0" };
							csw.Update(args);
							args = new string[] { "ActiveTx", "0", "1" };
							csw.Update(args);
						}

						//string split_status = "00";
						//if (slice0 != null && slice0.Transmit)
						//{
						//    split_status = "00";
						//    this.CurrentTransmitSlice = 0;
						//}
						//else if (slice1 != null && slice1.Transmit)
						//{
						//    split_status = "01";
						//    this.CurrentTransmitSlice = 1;
						//}
						//string[] vfoargs = { "Transmit", split_status };
						//csw.Update(vfoargs);
						result = "";
						break;
					case "TunePower":
						break;
                    case "TuneStep":
                        //if (slice0 != null)
						if(slice_to_use != null)
                        {
                           // string[] temp = { "TuneStep", slice0.TuneStep.ToString() };
							string[] temp = { "TuneStep", slice_to_use.TuneStep.ToString(), slc.ToString() };
                            csw.Update(temp);
                        }
                        break;
					case "TuneStepList":
					case "TXACCDelay":
					case "TXACCEnabled":
					case "TXAllowed":
					case "TX1Delay":
					case "TX1Enabled":
					case "TX2Delay":
					case "TX2Enabled":
					case "TX3Delay":
					case "TX3Enabled":
					case "TXAnt":
                    case "TXCWMonitorGain":
                    case "TXCWMonitorPan":
                    case "TXSBMonitorGain":
                    case "TXSBMonitorPan":
					case "TXFilterChangesAllowed":
					case "TXFilterHigh":
					case "TXFilterLow":
                    case "TXInhibit":
					case "TxMonAvailable":
					case "TXMonitor":
					case "TXMonitorGain":
					case "TXReqACCEnabled":
					case "TXReqRCAEnabled":
					case "TXReqACCPolarity":
					case "TXReqRCAPolarity":
					case "TXRFPowerChangesAllowed":
					case "TRXPsocVersion":
					case "TXTune":
						break;
					case "TXVFO":
						if (!is_so2r)
						{
							if (slice0 != null && slice0.Transmit)
								result = "0";
							else if (slice1 != null && slice1.Transmit)
								result = "1";
							else
								result = "0";
						}
						else
							result = "0";		//always 0 in so2r
						break;
					case "RXVFO":
						if (!is_so2r)
						{
							if (slice0 != null && !slice0.Transmit)
								result = "0";
							else if (slice1 != null && !slice1.Transmit)
								result = "1";
							else
								result = "0";
						}
						else
							result = "0";		//always 0 in so2r
						break;
					case "int_AI":
						if (AI)
							result = "1";
						else result = "0";
						break;
					case "int_IF":
                        if(slice0 != null)                           
						    result = this.IFStatus;
						break;
					case "int_ZZIF":
                        if (slice0 != null)
						    result = this.ZZIFStatus;
						break;
					case "int_ZZIX":
                        if(slice1 != null)
    						result = this.ZZIF2Status;
						break;
					case "RadioAck":
                        if (slice0 != null)
							result = slice0.RadioAck.ToString();
						break;
					case "Versions":
					case "WaveformsInstalledList":
						break;
					case "Wide":
                        if (slice0 != null)
							result = slice0.Wide.ToString();
						break;
					case "Index":
                        if (slice0 != null)
							result = slice0.Index.ToString();
						break;
					case "XITFreq":
						if (slice0 != null)
							result = slice0.XITFreq.ToString();
						break;
					case "XITOn":
                        //if (slice0 != null)
						if(slice_to_use != null)
						{
							//if (slice0.XITOn)
							if(slice_to_use.XITOn)
								result = "1";
							else
								result = "0";

							// Added changes 12/30/14 BT Add RIT/XIT to status word.
							args = new string[]{ "XITStat", result, slc.ToString() };
							csw.Update(args);
						}
						break;

					default:
						_log.Error(this + " " + key);
						break;

				}
				//Debug.WriteLine(key+":  "+result);
				return result;
		}

		/*! \var    Verify
		 *  \brief      Currently not used.
		 */

		public string Verify(string key)
		{
			return "";
		}

		public string[] GetModes()
		{
			string[] modes = new string[2] {string.Empty, string.Empty};
			if (slice0 != null)
				modes[0] = slice0.DemodMode;
			if (slice1 != null)
				modes[1] = slice1.DemodMode;

			return modes;
		}

        //public void CreateSice()
        //{
        //    slice1 = this_radio.CreateSlice(14.150, "XVTR", "USB");
        //    while (!slice0.RadioAck) { }
        //    slice1.RequestSliceFromRadio();
        //}

		private void SendAI(string freq, int slc)
		{
			string prefix = string.Empty;
			AddLeadingZeros zeros = new AddLeadingZeros();
			if (slc == 0)
			    prefix = "FA";
			else if (slc == 1)
			    prefix = "FB";
			object[] f = {freq, 11};
			SerialPortManager.CurrentPort.Write(prefix+zeros.Format(f) + ";");
		}

        private AGCMode StringToAGCMode(string s)
        {
            AGCMode mode = AGCMode.None;
            switch (s.ToLower())
            {
                case "off": mode = AGCMode.Off; break;
                case "med": mode = AGCMode.Medium; break;
                case "slow": mode = AGCMode.Slow; break;
                case "fast": mode = AGCMode.Fast; break;
				default: _log.Error("Fell thru AGCMode"); break;
            }

            return mode;
        }

		//No longer used 4/10/2015.  See TransceiverStatus class and SliceAdded() BT
		private void UpdateStatusWord()
		{
	
			//Updates the IF/ZZIF status word when slice0 is added to
			//make sure the status word is up to date after radio startup
			//Frequency step size, split, and mode are updated elsewhere.
			if (slice0 != null)
			{
				string[] parts;
				//Update the status word VFO frequency 
				string freq = slice0.Freq.ToString();
				//Get the correct separator for the locale
				if (freq.Contains(separator))
					parts = freq.Split(Convert.ToChar(separator));
				else
					parts = new string[] { freq.ToString(), "000000" };
				//Set the frequency format and create a parameter array
				if (parts[1].Length > 6)
					parts[1] = parts[1].Substring(0, 6);

				string result = parts[0] + parts[1].PadRight(6, '0');
				string[] args = new string[]{ "Freq", result };
				csw.Update(args);
				//Update the RIT status and frequency section
				int rit_nfreq = slice0.RITFreq;
				string rit_sign = "+";
				if (slice0.RITOn && !slice0.Transmit)
				{
					args = new string[] { "RITOn", "1" };
					if (rit_nfreq < 0)
						rit_sign = "-";
					string rit_sfreq = rit_sign + Math.Abs(rit_nfreq).ToString().PadLeft(5, '0');
					args = new string[] { "RITFreq", rit_sfreq };
					csw.Update(args);
				}
				//Update the XIT status
				if (slice0.XITOn && !slice0.RITOn)
				{
					args = new string[] { "XITOn", "1" };
					csw.Update(args);
				}
			}
		}

		#endregion Methods

	}
}
