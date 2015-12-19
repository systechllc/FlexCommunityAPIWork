using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Text;
using Flex.Smoothlake.FlexLib;
using log4net;

namespace Cat.CatMain
{
	public class TransceiverStatus	//Updates the IF/ZZIF transceiver status words when slice0 or slice1 added
	{
		#region Variables
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		ObjectManager _om;
		Slice slc;
		StringBuilder flex_status_word = new StringBuilder();
		StringBuilder kw_status_word = new StringBuilder();
		private string separator = System.Globalization.CultureInfo.CurrentCulture.NumberFormat.NumberDecimalSeparator;
		bool cwl_active = false;
		string kw_mode = string.Empty;
		string flex_mode = string.Empty;

		#endregion Variables

		#region Properties

		private string kw_status = string.Empty;
		public string KWStatus
		{
			get { return kw_status; }
			set { kw_status = value; }
		}

		private string flex_status;
		public string FlexStatus
		{
			get { return flex_status; }
			set { flex_status = value; }
		}

		#endregion Properties

		#region Constructor

		public TransceiverStatus(Slice slice, bool cwl)
		{
			_om = ObjectManager.Instance;
			slc = slice;
			cwl_active = cwl;
			Update();
		}
		#endregion Constructor

		#region Methods

		private void Update()
		{
			bool is_so2r = _om.StateManager.IsS02R;
			//Debug.WriteLine("TransceiverStatus.is_so2r = " + is_so2r.ToString());
			string[] parts;
			string freq = slc.Freq.ToString();
			if (freq.Contains(separator))
				parts = freq.Split(Convert.ToChar(separator));
			else
				parts = new string[] { freq.ToString(), "000000" };

			if (parts[1].Length > 6)
				parts[1] = parts[1].Substring(0, 6);

			flex_status_word.Append((parts[0] + parts[1].PadRight(6, '0')).PadLeft(11, '0'));	//add the slice frequency

			string step_size = slc.TuneStep.ToString();
			if (step_size.Length > 4)
				step_size = step_size.Substring(0, 4);	//get the four most significant digits
			else
				step_size = step_size.PadLeft(4, '0');

			flex_status_word.Append(step_size);	// add the step size

			string sign = "+";
			if (slc.RITFreq < 0)
				sign = "-";
			string rit_freq = Math.Abs(slc.RITFreq).ToString();
			if (rit_freq.Length > 5)
				rit_freq = rit_freq.Substring(0, 5);
			else
				rit_freq = rit_freq.PadLeft(5, '0');

			flex_status_word.Append(sign + rit_freq);	//add the rit frequency

			if (slc.RITOn)
				flex_status_word.Append("1");
			else
				flex_status_word.Append("0");			//add the rit enable status

			if (slc.XITOn)
				flex_status_word.Append("1");
			else
				flex_status_word.Append("0");			//add the xit enable status

			flex_status_word.Append("0000");				//dummy status channel bank & mox


			kw_status_word.Append(flex_status_word);	//update the kw status word

			string mode = slc.DemodMode;
			if (mode == "CW")
			{
				if (cwl_active)
					mode = "CWL";
				else
					mode = "CWU";
			}

			ConvertMode(mode);
			flex_status_word.Append(flex_mode);
			kw_status_word.Append(kw_mode);		//append the mode values

			string active_rx = "0";		//default to RX A
			if (!is_so2r)
			{
				if (slc.Index == 1 && slc.Active)
					active_rx = "1";
			}
			flex_status_word.Append(active_rx + "0");
			kw_status_word.Append(active_rx + "0");		//update RX split status and unused Scan status

			string active_tx = "0";
			if (!is_so2r)
			{
				if (slc.Index == 1 && slc.Transmit)
					active_tx = "1";
			}
			flex_status_word.Append(active_tx + "0000");
			kw_status_word.Append(active_tx + "0000");		//update TX split status and remaining unused status bits

			this.KWStatus = kw_status_word.ToString();
			this.FlexStatus = flex_status_word.ToString();
		 }

		private void ConvertMode(string mode)
		{
			switch(mode.ToUpper())
			{
				case "LSB":
					flex_mode = "00";
					kw_mode = "1";
					break;
				case "USB":
					flex_mode = "01";
					kw_mode = "2";
					break;
				case "CWL":
					flex_mode = "03";
					kw_mode = "7";
					break;
				case "CWU":
					flex_mode = "04";
					kw_mode = "3";
					break;
				case "FM":
					flex_mode = "05";
					kw_mode = "4";
					break;
				case "DIGU":
					flex_mode = "07";
					kw_mode = "9";
					break;
				case "DIGL":
					flex_mode = "09";
					kw_mode = "6";
					break;
				case "SAM":
					flex_mode = "10";
					kw_mode = "5";
					break;
				case "NFM":
					flex_mode = "11";
					kw_mode = "4";
					break;
				case "DFM":
					flex_mode = "12";
					kw_mode = "4";
					break;
				case "FDV":
					flex_mode = "20";
					kw_mode = "4";
					break;
				case "RTTY":
					flex_mode = "30";
					kw_mode = "6";
					break;
				default:
					_log.Error("Fell thru mode with " + mode);
					break;
			}
		}

		#endregion Methods
	}
}
