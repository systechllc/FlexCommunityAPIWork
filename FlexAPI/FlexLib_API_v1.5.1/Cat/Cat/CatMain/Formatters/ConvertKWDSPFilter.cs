using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Cat.Cat.Interfaces;
using log4net;

namespace Cat.Cat.Formatters
{
	public class ConvertKWDSPFilter : ICatReturnFormat
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public ConvertKWDSPFilter()
		{

		}

		public string Format(object[] args)
		{
			string source = (string)args[0];
			string value = (string)args[1];
			string api = (string)args[2];
			string mode = (string)args[3];
			string result = "?;";

			if (value.Contains('.'))	//a get request with filter frequency
			{
				int freq = Math.Abs((int) double.Parse(value));
				switch (source)
				{
					case "SL":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
							case "USB":
							case "DIGU":
							case "CW":
							case "CWL":
								if (freq >= 0 && freq <= 25)
									result = "00";
								else if (freq > 25 && freq <= 75)
									result = "01";
								else if (freq > 75 && freq <= 150)
									result = "02";
								else if (freq > 150 && freq <= 250)
									result = "03";
								else if (freq > 250 && freq <= 350)
									result = "04";
								else if (freq > 350 && freq <= 450)
									result = "05";
								else if (freq > 450 && freq <= 550)
									result = "06";
								else if (freq > 550 && freq <= 650)
									result = "07";
								else if (freq > 650 && freq <= 750)
									result = "08";
								else if (freq > 750 && freq <= 850)
									result = "09";
								else if (freq > 850 && freq <= 950)
									result = "10";
								else if (freq > 950)
									result = "11";
								break;
							case "AM":
                            case "SAM":
								if (freq >= 0 && freq <= 50)
									result = "00";
								else if (freq > 50 && freq <= 150)
									result = "01";
								else if (freq > 150 && freq <= 350)
									result = "02";
								else if (freq > 350)
									result = "03";
								break;
							default:
								_log.Error("Fell thru SL " + mode);
								break;
						}
						break;
					case "SH":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
							case "USB":
							case "DIGU":
                            case "CW":
							case "CWL":
                                if (freq >= 0 && freq <= 1500)
                                    result = "00";
                                else if (freq > 1500 && freq <= 1700)
                                    result = "01";
                                else if (freq > 1700 && freq <= 1900)
                                    result = "02";
                                else if (freq > 1900 && freq <= 2100)
                                    result = "03";
                                else if (freq > 2100 && freq <= 2300)
                                    result = "04";
                                else if (freq > 2300 && freq <= 2500)
                                    result = "05";
                                else if (freq > 2500 && freq <= 2700)
                                    result = "06";
                                else if (freq > 2700 && freq <= 2900)
                                    result = "07";
                                else if (freq > 2900 && freq <= 3200)
                                    result = "08";
                                else if (freq > 3200 && freq <= 3700)
                                    result = "09";
                                else if (freq > 3700 && freq <= 4500)
                                    result = "10";
                                else if (freq > 4500)
                                    result = "11";
                                break;							
							case "AM":
                            case "SAM":
								if (freq >= 0 && freq <= 2750)
									result = "00";
								else if (freq > 2750 && freq <= 3500)
									result = "01";
								else if (freq > 3500 && freq <= 4500)
									result = "02";
								else if (freq > 4500)
									result = "03";
								break;
							default:
								_log.Error("Fell thru SH " + mode);
								break;
						}
						break;
				}
			}
			else
			{
				switch (value)
				{
					case "00":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-1400";
								else if (source == "SH")
								    result = "0";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "0";
								else if (source == "SH")
									result = "1400";
								break;
							case "AM":
                            case "SAM":
								if (source == "SL")
									result = "0";
								else if (source == "SH")
									result = "2500";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 00");
								break;
						}
						break;
					case "01":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-1600";
								else if (source == "SH")
								    result = "-50";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "50";
								else if (source == "SH")
									result = "1600";
								break;
							case "AM":
                            case "SAM":
								if (source == "SL")
									result = "100";
								else if (source == "SH")
									result = "3000";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 01");
								break;
						}
						break;
					case "02":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-1800";
								else if (source == "SH")
								    result = "-100";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "100";
								else if (source == "SH")
									result = "1800";
								break;
							case "AM":
                            case "SAM":
								if (source == "SL")
									result = "200";
								else if (source == "SH")
									result = "4000";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 02");
								break;
						}
						break;
					case "03":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-2000";
								else if (source == "SH")
								    result = "-200";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "200";
								else if (source == "SH")
									result = "2000";
								break;
							case "AM":
                            case "SAM":
								if (source == "SL")
									result = "500";
								else if (source == "SH")
									result = "5000";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 03");
								break;
						}
						break;
					case "04":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-2200";
								else if (source == "SH")
								    result = "-300";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "300";
								else if (source == "SH")
									result = "2200";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 04");
								break;
						}
						break;
					case "05":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-2400";
								else if (source == "SH")
								    result = "-400";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "400";
								else if (source == "SH")
									result = "2400";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 05");
								break;
						}
						break;
					case "06":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-2600";
								else if (source == "SH")
								    result = "-500";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "500";
								else if (source == "SH")
									result = "2600";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 06");
								break;
						}
						break;
					case "07":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-2800";
								else if (source == "SH")
								    result = "-600";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "600";
								else if (source == "SH")
									result = "2800";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 07");
								break;
						}
						break;
					case "08":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-3000";
								else if (source == "SH")
								    result = "-700";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "700";
								else if (source == "SH")
									result = "3000";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 08");
								break;
						}
						break;
					case "09":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-3400";
								else if (source == "SH")
								    result = "-800";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "800";
								else if (source == "SH")
									result = "3400";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 09");
								break;
						}
						break;
					case "10":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-4000";
								else if (source == "SH")
								    result = "-900";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "900";
								else if (source == "SH")
									result = "4000";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 10");
								break;
						}
						break;
					case "11":
						switch (mode)
						{
							case "LSB":
							case "DIGL":
								if (source == "SL")
								    result = "-5000";
								else if (source == "SH")
								    result = "-1000";
								break;
							case "USB":
							case "DIGU":
							case "CW":
								if (source == "SL")
									result = "1000";
								else if (source == "SH")
									result = "5000";
								break;
							default:
								_log.Error("Fell thru mode " + mode + " with value of 11");
								break;
						}
						break;
				}
			}
			if (!result.Contains('?'))
				return api + ":" + result;
			else
				return result;
		}
		private string GetFilterForMode(string value)
		{
			string ans = string.Empty;

			return ans;
		}
	}
}
