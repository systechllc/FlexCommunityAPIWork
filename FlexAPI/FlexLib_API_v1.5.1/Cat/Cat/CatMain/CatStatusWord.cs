using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using log4net;
using Cat.Cat.Formatters;

namespace Cat.Cat
{
	public class CatStatusWord
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private static CatStateManager csm = new CatStateManager();
		private ConvertDemodMode cdm = new ConvertDemodMode();

		public CatStatusWord()
		{
		}

		public void Update(string[] args)
		{
			try
			{
				//int nargs = args.Count();
				string prop_name = args[0];
				string new_value = args[1];
				//this probably isn't needed anymore since array creation in csm rewritten bt
				//if (nargs == 2)
				//{
				//    args = new string[] { prop_name, new_value, "0" };
				//}
				string slice = args[2];
				string oldIFState = string.Empty;
				string oldZZIFState = string.Empty;
				switch (slice)
				{
					case "0":
						oldIFState = csm.IFStatus;
						oldZZIFState = csm.ZZIFStatus;
						break;
					case "1":
						oldIFState = csm.IF2Status;
						oldZZIFState = csm.ZZIF2Status;
						break;
					default:
						_log.Error("Fell thru slice selection");
						break;
				}

				string[] cdm_args;
				string kfreq = oldIFState.Substring(0, 11);
				string kstep = oldIFState.Substring(11, 4);
				string krit = oldIFState.Substring(15, 6);
				string krit_stat = oldIFState.Substring(21, 1);
				string kxit_stat = oldIFState.Substring(22, 1);
				string kcbank1 = oldIFState.Substring(23, 1);
				string kcbank2 = oldIFState.Substring(24, 2);
				string kmox = oldIFState.Substring(26, 1);
				string kmode = oldIFState.Substring(27, 1);
				string krsplit = oldIFState.Substring(28, 1);
				string kscan = oldIFState.Substring(29, 1);
				string ktsplit = oldIFState.Substring(30, 1);
				string kctcss = oldIFState.Substring(31, 1);
				string ktone = oldIFState.Substring(32, 2);
				string kshift = oldIFState.Substring(34, 1);

				string ffreq = oldZZIFState.Substring(0, 11);
				string fstep = oldZZIFState.Substring(11, 4);
				string frit = oldZZIFState.Substring(15, 6);
				string frit_stat = oldZZIFState.Substring(21, 1);
				string fxit_stat = oldZZIFState.Substring(22, 1);
				string fcbank1 = oldZZIFState.Substring(23, 1);
				string fcbank2 = oldZZIFState.Substring(24, 2);
				string fmox = oldZZIFState.Substring(26, 1);
				string fmode = oldZZIFState.Substring(27, 2);
				string frsplit = oldZZIFState.Substring(29, 1);
				string fscan = oldZZIFState.Substring(30, 1);
				string ftsplit = oldZZIFState.Substring(31, 1);
				string fctcss = oldZZIFState.Substring(32, 1);
				string ftone = oldZZIFState.Substring(34, 2);
				string fshift = oldZZIFState.Substring(35, 1);
				bool updated = false;

				if (new_value != "")
				{
					switch (prop_name.ToUpper())
					{
						case "ACTIVERX":
							krsplit = new_value;
							frsplit = new_value;
							updated = true;
							break;
						case "ACTIVETX":
							ktsplit = new_value;
							ftsplit = new_value;
							updated = true;
							break;
						case "FREQ":
							kfreq = new_value.PadLeft(11, '0');
							ffreq = new_value.PadLeft(11, '0');
							updated = true;
							break;
						case "RITFREQ":
							krit = new_value;
							frit = new_value;
							updated = true;
							break;
						case "RITSTAT":
							krit_stat = new_value;
							frit_stat = new_value;
							updated = true;
							break;
						case "XITSTAT":
							kxit_stat = new_value;
							fxit_stat = new_value;
							updated = true;
							break;
						case "MOX":
							fmox = new_value;
							kmox = new_value;
							updated = true;
							break;
						case "DEMODMODE":
						case "DEMODMODE0F":
							//fstep = GetStepSize(new_value);
							//kstep = fstep;
							fmode = new_value;
							cdm_args = new string[] { "Flex2KW", new_value, null };
							kmode = cdm.Format(cdm_args).Split(':')[1];
							updated = true;
							break;
						case "DEMODMODE0K":
							kmode = new_value;
							cdm_args = new string[] { "KW2Flex", new_value, null };
							fmode = cdm.Format(cdm_args).Split(':')[1];
							//kstep = GetStepSize(fmode);
							//fstep = kstep;
							updated = true;
							break;
						case "RX":
							kmox = new_value;
							fmox = new_value;
							updated = true;
							break;
						case "TX":
							kmox = new_value;
							fmox = new_value;
							updated = true;
							break;
						//case "TRANSMIT":
						//    krsplit = new_value.Substring(0, 1);
						//    ktsplit = new_value.Substring(1, 1);
						//    frsplit = new_value.Substring(0, 1);
						//    ftsplit = new_value.Substring(1, 1);
						//    updated = true;
						//    break;
						case "TUNESTEP":
							uint new_step;
							bool b = uint.TryParse(new_value, out new_step);

							if (b)
							{
								new_step = Math.Min(9999, new_step);
								kstep = fstep = new_step.ToString().PadLeft(4, '0');
								updated = true;
							}
							break;
						case "TXVFO":
							ktsplit = new_value;
							ftsplit = new_value;
							updated = true;
							break;
						default:
							_log.Error("Fell thru prop_name" + prop_name);
							break;
					}

					if (updated && slice == "0")
					{
						csm.IFStatus = kfreq +
							kstep +
							krit +
							krit_stat +
							kxit_stat +
							kcbank1 +
							kcbank2 +
							kmox +
							kmode +
							krsplit +
							kscan +
							ktsplit +
							kctcss +
							ktone +
							kshift;
						csm.ZZIFStatus = ffreq +
							fstep +
							frit +
							frit_stat +
							fxit_stat +
							fcbank1 +
							fcbank2 +
							fmox +
							fmode +
							frsplit +
							fscan +
							ftsplit +
							fctcss +
							ftone +
							fshift;
					}
					else if (updated && slice == "1")
					{
						csm.IF2Status = kfreq +
							kstep +
							krit +
							krit_stat +
							kxit_stat +
							kcbank1 +
							kcbank2 +
							kmox +
							kmode +
							krsplit +
							kscan +
							ktsplit +
							kctcss +
							ktone +
							kshift;
						csm.ZZIF2Status = ffreq +
							fstep +
							frit +
							frit_stat +
							fxit_stat +
							fcbank1 +
							fcbank2 +
							fmox +
							fmode +
							frsplit +
							fscan +
							ftsplit +
							fctcss +
							ftone +
							fshift;
					}

				}
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}
		}


		private string GetStepSize(string value)
		{
			string result = string.Empty;
			switch(value)
			{
				case "00":
				case "01":
				case "02":
				case "05":
				case "06":
					result = "1000";
					break;
				case "03":
				case "04":
				case "07":
				case "09":
					result = "0001";
					break;
				default:
					result = "1000";
					break;
			}

			return result;
		}

	}
}
