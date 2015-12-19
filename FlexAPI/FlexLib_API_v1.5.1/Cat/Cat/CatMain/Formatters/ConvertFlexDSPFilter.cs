using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Cat.Cat;
using Cat.Cat.Interfaces;
using log4net;
using Flex.Smoothlake.FlexLib;

namespace Cat.Cat.Formatters
{

	public class ConvertFlexDSPFilter : ICatReturnFormat
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static Radio this_radio = CatStateManager.CSMRadio;

		public ConvertFlexDSPFilter()
		{
		}

		public string Format(object[] args)
		{
			string source = (string)args[0];
			string value = (string)args[1];
			string api = (string)args[2];
			string mode = (string)args[3];
			string result = "?;";

			if (mode.StartsWith("SAM"))
				mode = mode.Replace("SAM", "AM");

			if (mode.Contains("CWL"))
				mode = "CW";

			switch(value)
			{
				case "00":
				case "01":
				case "02":
				case "03":
				case "04":
				case "05":
				case "06":
				case "07":
					result = GetFilterForMode(mode, value);
					break;
				default:
					result = GetCodeForWidth(mode, value);
					break;
			}


		//    switch (value)
		//    {
		//        case "100:4100":
		//        case "-4100:-100":
		//        case "-10000:10000":
		//        case "-1500:1500":
		//        case "-5000:0":
		//        case "0:5000":
		//            result = "00";
		//            break;
		//        case "100:3400":
		//        case "-3400:-100":
		//        case "-8000:8000":
		//        case "-750:750":
		//        case "-3000:0":
		//        case "0:3000":
		//            result = "01";
		//            break;
		//        case "100:3000":
		//        case "-3000:-100":
		//        case "-7000:7000":
		//        case "-500:500":
		//        case "-2500:-500":
		//        case "500:2500":
		//            result = "02";
		//            break;
		//        case "100:2800":
		//        case "-2800:-100":
		//        case "-6000:6000":
		//        case "-5500:5500":
		//        case "-400:400":
		//        case "-2250:-750":
		//        case "750:2250":
		//            result = "03";
		//            break;
		//        case "100:2500":
		//        case "-2500:-100":
		//        case "-5000:5000":
		//        case "-200:200":
		//        case "-2000:-1000":
		//        case "1000:2000":
		//            result = "04";
		//            break;
		//        case "100:2200":
		//        case "-2200:-100":
		//        case "-4000:4000":
		//        case "-125:125":
		//        case "-1800:-1200":
		//        case "1200:1800":
		//            result = "05";
		//            break;
		//        case "100:1900":
		//        case "-1900:-100":
		//        case "-3000:3000":
		//        case "-50:50":
		//        case "-1650:-1350":
		//        case "1350:1650":
		//            result = "06";
		//            break;
		//        case "100:1700":
		//        case "-1700:-100":
		//        case "-2800:2800":
		//        case "-25:25":
		//        case "-1550:-1450":
		//        case "1450:1550":
		//            result = "07";
		//            break;
		//        case "00":
		//        case "01":
		//        case "02":
		//        case "03":
		//        case "04":
		//        case "05":
		//        case "06":
		//        case "07":
		//            result = GetFilterForMode(mode + ":" + value);
		//            break;
		//    }

			if (!result.Contains('?'))
				return api + ":" + result;
			else
				return result;
		}

		private string GetFilterForMode(string mode, string value)
		{
            // handle SAM modes
			//if (value.StartsWith("SAM"))
			//    value = value.Replace("SAM", "AM");

			//if (mode.Contains("CWL"))
			//    value = "CW:" + value.Substring(3);
			string key = mode + ":" + value;
			string ans = "?;";
			switch (key)
			{
				case "USB:00":
					ans = "100#4100";
					break;
				case "USB:01":
					ans = "100#3400";
					break;
				case "USB:02":
					ans = "100#3000";
					break;
				case "USB:03":
					ans = "100#2800";
					break;
				case "USB:04":
					ans = "100#2500";
					break;
				case "USB:05":
					ans = "100#2200";
					break;
				case "USB:06":
					ans = "100#1900";
					break;
				case "USB:07":
					ans = "100#1700";
					break;
				case "LSB:00":
					ans = "-4100#-100";
					break;
				case "LSB:01":
					ans = "-3400#-100";
					break;
				case "LSB:02":
					ans = "-3000#-100";
					break;
				case "LSB:03":
					ans = "-2800#-100";
					break;
				case "LSB:04":
					ans = "-2500#-100";
					break;
				case "LSB:05":
					ans = "-2200#-100";
					break;
				case "LSB:06":
					ans = "-1900#-100";
					break;
				case "LSB:07":
					ans = "-1700#-100";
					break;
				case "AM:00":
					ans = "-10000#10000";
					break;
				case "AM:01":
					ans = "-8000#8000";
					break;
				case "AM:02":
					ans = "-7000#7000";
					break;
				case "AM:03":
					ans = "-6000#6000";
					break;
				case "AM:04":
					ans = "-5000#5000";
					break;
				case "AM:05":
					ans = "-4000#4000";
					break;
				case "AM:06":
					ans = "-3000#3000";
					break;
				case "AM:07":
					ans = "-2800#2800";
					break;
                case "FM:00":
                case "FM:01":
                case "FM:02":
                case "FM:03":
                case "FM:04":
                case "FM:05":
                case "FM:06":
                case "FM:07":
					ans = "-8000#8000";
					break;
                case "DFM:00":
					ans = "-10000#10000";
					break;
                case "DFM:01":
					ans = "-9000#9000";
					break;
                case "DFM:02":
					ans = "-8000#8000";
					break;
                case "DFM:03":
					ans = "-7000#7000";
					break;
                case "DFM:04":
					ans = "-6000#6000";
					break;
                case "DFM:05":
					ans = "-5000#5000";
					break;
                case "DFM:06":
					ans = "-4000#4000";
					break;
                case "DFM:07":
                    ans = "-3000#3000";
                    break;
                case "NFM:00":
                case "NFM:01":
                case "NFM:02":
                case "NFM:03":
                case "NFM:04":
                case "NFM:05":
                case "NFM:06":
                case "NFM:07":
                    ans = "-5500#5500";
					break;
				case "CW:00":
					//ans = "-1500#1500";
					ans = CW_DSPFilter_Offsets("-1500#1500");
					break;
				case "CW:01":
					//ans = "-750#750";
					ans = CW_DSPFilter_Offsets("-750#750");
					break;
				case "CW:02":
					//ans = "-500#500";
					ans = CW_DSPFilter_Offsets("-500#500");
					break;
				case "CW:03":
					//ans = "-400#400";
					ans = CW_DSPFilter_Offsets("-400#400");
					break;
				case "CW:04":
					//ans = "-200#200";
					ans = CW_DSPFilter_Offsets("-200#200");
					break;
				case "CW:05":
					//ans = "-125#125";
					ans = CW_DSPFilter_Offsets("-125#125");
					break;
				case "CW:06":
					//ans = "-50#50";
					ans = CW_DSPFilter_Offsets("-50#50");
					break;
				case "CW:07":
					//ans = "-25#25";
					ans = CW_DSPFilter_Offsets("-25#25");
					break;
				case "DIGL:00":
					//ans = "-5000#0";
					ans = DIGI_DSPFilter_Offsets("L", 5000);
					break;
				case "DIGL:01":
					//ans = "-3000#0";
					ans = DIGI_DSPFilter_Offsets("L", 3000);
					break;
				case "DIGL:02":
					//ans = "-2500#-500";
					ans = DIGI_DSPFilter_Offsets("L", 2000);
					break;
				case "DIGL:03":
					//ans = "-2250#-750";
					ans = DIGI_DSPFilter_Offsets("L", 1500);
					break;
				case "DIGL:04":
					//ans = "-2000#-1000";
					ans = DIGI_DSPFilter_Offsets("L", 1000);
					break;
				case "DIGL:05":
                    //ans = "-1800#-1200";		
					ans = DIGI_DSPFilter_Offsets("L", 600);
					break;
				case "DIGL:06":
                    //ans = "-1650#-1350";	
					ans = DIGI_DSPFilter_Offsets("L", 300);
					break;
				case "DIGL:07":
                    //ans = "-1550#-1450";
					ans = DIGI_DSPFilter_Offsets("L", 100);
					break;
				case "DIGU:00":
					//ans = "0#5000";
					ans = DIGI_DSPFilter_Offsets("U", 5000);
					break;
				case "DIGU:01":
					//ans = "0#3000";
					ans = DIGI_DSPFilter_Offsets("U", 3000);
					break;
				case "DIGU:02":
					//ans = "500#2500";
					ans = DIGI_DSPFilter_Offsets("U", 2000);
					break;
				case "DIGU:03":
                    //ans = "750#2250";
					ans = DIGI_DSPFilter_Offsets("U", 1500);
					break;
				case "DIGU:04":
                    //ans = "1000#2000";		
					ans = DIGI_DSPFilter_Offsets("U", 1000);
					break;
				case "DIGU:05":
                    //ans = "1200#1800";
					ans = DIGI_DSPFilter_Offsets("U", 600);
					break;
				case "DIGU:06":
                    //ans = "1350#1650";	
					ans = DIGI_DSPFilter_Offsets("U", 300);
					break;
				case "DIGU:07":
					//ans = "1450#1550";
					ans = DIGI_DSPFilter_Offsets("U", 100);
					break;
				case "RTTY:00":
					ans = RTTY_DSPFilter_Offsets(3000);
					break;
				case "RTTY:01":
					ans = RTTY_DSPFilter_Offsets(1500);
					break;
				case "RTTY:02":
					ans = RTTY_DSPFilter_Offsets(1000);
					break;
				case "RTTY:03":
					ans = RTTY_DSPFilter_Offsets(500);
					break;
				case "RTTY:04":
					ans = RTTY_DSPFilter_Offsets(400);
					break;
				case "RTTY:05":
					ans = RTTY_DSPFilter_Offsets(350);
					break;
				case "RTTY:06":
					ans = RTTY_DSPFilter_Offsets(300);
					break;
				case "RTTY:07":
					ans = RTTY_DSPFilter_Offsets(250);
					break;
				default:
					_log.Error("Fell thru GetFilterForMode with " + mode);
					break;
			}
			return ans;
		}

		private string CW_DSPFilter_Offsets(string bw)
		{
			int lo;
			int hi;
			int offset;
			int pitch;
			int set_lo;
			int set_hi;

			string[] lohi = bw.Split('#');
			lo = int.Parse(lohi[0]);
			hi = int.Parse(lohi[1]);

			//Add this to get slice pitch when pitch by slice implemented
			//Slice current_slice = null;
			//List<Slice> slices = this_radio.SliceList;

			//foreach (Slice sl in slices)
			//{
			//    if (sl.Active)
			//        current_slice = sl;
			//    break;
			//}

			//lo = current_slice.FilterLow;
			//hi = current_slice.FilterHigh;
			//pitch = current_slice.CWPitch;

			pitch = this_radio.CWPitch;

			if (this_radio.CWL_Enabled)
			{
				offset = Math.Max(pitch, hi) - pitch;
				set_lo = lo - offset;
				set_hi = hi - offset;
			}
			else
			{
				offset = Math.Max(pitch, -lo) - pitch;
				set_lo = lo + offset;
				set_hi = hi + offset;
			}

			return set_lo + "#" + set_hi;
		}

		private string GetCodeForWidth(string _mode, string _value)
		{
			int width = GetFilterWidth(_value);
			string ans = "?";

			switch(_mode)
			{
				case "USB":
				case "LSB":
					if (width >= 0 && width <= 1600)
						ans = "07";
					else if (width > 1600 && width <= 1800)
						ans = "06";
					else if (width > 1800 && width <= 2100)
						ans = "05";
					else if (width > 2100 && width <= 2400)
						ans = "04";
					else if (width > 2400 && width <= 2700)
						ans = "03";
					else if (width > 2700 && width <= 2900)
						ans = "02";
					else if (width > 2900 && width <= 3300)
						ans = "01";
					else if (width > 3300)
						ans = "00";
					break;
				case "AM":
					if (width >= 0 && width <= 5600)
						ans = "07";
					else if (width > 5600 && width <= 6000)
						ans = "06";
					else if (width > 6000 && width <= 8000)
						ans = "05";
					else if (width > 8000 && width <= 10000)
						ans = "04";
					else if (width > 10000 && width <= 12000)
						ans = "03";
					else if (width > 12000 && width <= 14000)
						ans = "02";
					else if (width > 14000 && width <= 16000)
						ans = "01";
					else if (width > 16000)
						ans = "00";
					break;
				case "CW":
				case "CWL":
				case "CWU":
					if (width > 0 && width <= 50)
						ans = "07";
					else if (width > 50 && width <= 100)
						ans = "06";
					else if (width > 100 && width <= 250)
						ans = "05";
					else if (width > 250 && width <= 400)
						ans = "04";
					else if (width > 400 && width <= 800)
						ans = "03";
					else if (width > 800 && width <= 1000)
						ans = "02";
					else if (width > 1000 && width <= 1500)
						ans = "01";
					else if (width > 1500)
						ans = "00";
					break;
				case "DIGL":
				case "DIGU":
					if (width > 0 && width <= 100)
						ans = "07";
					else if (width > 100 && width <= 300)
						ans = "06";
					else if (width > 300 && width <= 600)
						ans = "05";
					else if (width > 600 && width <= 1000)
						ans = "04";
					else if (width > 1000 && width <= 1500)
						ans = "03";
					else if (width > 1500 && width <= 2000)
						ans = "02";
					else if (width > 2000 && width <= 3000)
						ans = "01";
					else if (width > 3000)
						ans = "00";
					break;
				case "FM":
				case "DFM":
					if (width > 0 && width <= 6000)
						ans = "07";
					else if (width > 6000 && width <= 8000)
						ans = "06";
					else if (width > 8000 && width <= 10000)
						ans = "05";
					else if (width > 10000 && width <= 12000)
						ans = "04";
					else if (width > 12000 && width <= 14000)
						ans = "03";
					else if (width > 14000 && width <= 16000)
						ans = "02";
					else if (width > 16000 && width <= 18000)
						ans = "01";
					if (width > 18000)
						ans = "00";
					break;
				case "NFM":
					if (width == 11000)
						ans = "03";
					break;
				case "FDV":
					if (width == 1800)
						ans = "03";
					break;
				case "RTTY":
					if (width > 0 && width <= 250)
						ans = "07";
					else if (width > 250 && width <= 300)
						ans = "06";
					else if (width > 300 && width <= 350)
						ans = "05";
					else if (width > 350 && width <= 400)
						ans = "04";
					else if (width > 400 && width <= 500)
						ans = "03";
					else if (width > 500 && width <= 1000)
						ans = "02";
					else if (width > 1000 && width <= 1500)
						ans = "01";
					else if (width > 1500)
						ans = "00";
					break;
				default:
					_log.Error("Fell thru GetCodeForWidth with " + _value);
					break;
			}
			return ans;
		}


		private int GetFilterWidth(string _value)
		{
			string[] lohi = _value.Split(':');
			int bw = Math.Abs(int.Parse(lohi[1]) - int.Parse(lohi[0]));
			return bw;
		}

		private string RTTY_DSPFilter_Offsets(int bw)
		{
			int shift = this_radio.ActiveSlice.RTTYShift;
			int fh = (bw - shift) / 2;
			int fl = fh - bw;
			return fl.ToString()+"#"+fh.ToString();
		}

		private string DIGI_DSPFilter_Offsets(string side, int bw)
		{
			int offset_lower = this_radio.ActiveSlice.DIGLOffset * -1;
			int offset_upper = this_radio.ActiveSlice.DIGUOffset;
			int fl;
			int fh;

			if (bw >= 3000)
			{
				if (side == "L")
				{
					fl = -bw;
					fh = 0;
				}
				else
				{
					fh = bw;
					fl = 0;
				}
			}
			else
			{
				if (side == "L")
				{
					fl = offset_lower - (bw / 2);
					fh = offset_lower + (bw / 2);
				}
				else
				{
					fl = offset_upper - (bw / 2);
					fh = offset_upper + (bw / 2);
				}
			}


			return fl.ToString() + "#" + fh.ToString();
		}
	}


}
