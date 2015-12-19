using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Cat.Cat.Interfaces;
using log4net;

namespace Cat.Cat.Formatters
{
	public class ConvertDemodMode : ICatReturnFormat
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public ConvertDemodMode()
		{

		}

		public string Format(object[] args)
		{
			string source = (string)args[0];
			string value = (string)args[1];
			string api = (string)args[2];
			string result = string.Empty;

			switch (source.ToUpper())
			{
				case "MD":
					switch (value)
					{
//-----------decode by suffix-----------------------------
						case "1":
							result = "LSB";
							break;
						case "2":
							result = "USB";
							break;
						case "3":
                            result = "CW";
                            break;
						case "7":
							result = "CWL";
							break;
                        case "4":
                            result = "FM";
                            break;
						case "5":
							result = "AM";
							break;
						case "6":
							result = "DIGL";
							break;
						case "9":
							result = "DIGU";
							break;
						//case "10":
						//    result = "SAM";
						//    break;
//------------decode by name-----------------------------
						case "LSB":
							result = "1";
							break;
						case "USB":
							result = "2";
							break;
                        case "CW":
						case "CWU":
							result = "3";
							break;
                        case "CWL":
                            result = "7";
                            break;
                        case "FM":
                        case "NFM":
                        case "DFM":
                            result = "4";
                            break;
						case "AM":
                        case "SAM":
							result = "5";
							break;
						case "DIGL":
							result = "6";
							break;
						case "DIGU":
							result = "9";
							break;
						default:
							result = "?";
							_log.Error("Unable to convert KW demod mode:  " + value + source);
							break;
					}
					break;
				case "ZZMD":
				case "ZZME":
					switch (value)
					{
//-----------decode by suffix------------------------------
						case "00":
							result = "LSB";
							break;
						case "01":
							result = "USB";
							break;
						//case "02":
						//    result = "DSB";
						//    break;
						case "03":
                            result = "CWL";
                            break;
						case "04":
                            result = "CWU";
							break;
                        case "05":
                            result = "FM";
                            break;
						case "06":
							result = "AM";
							break;
						case "07":
							result = "DIGU";
							break;
						//case "08":
						//    result = "SPEC";
						//    break;
						case "09":
							result = "DIGL";
							break;
                        case "10":
                            result = "SAM";
                            break;
                        case "11":
                            result = "NFM";
                            break;
                        case "12":
                            result = "DFM";
                            break;
						case "20":
							result = "FDV";
							break;
						case "30":
							result = "RTTY";
							break;
//----------decode by name--------------------------
						case "LSB":
							result = "00";
							break;
						case "USB":
							result = "01";
							break;
						//case "DSB":
						//    result = "02";
						//    break;
                        case "CWL":
                            result = "03";
                            break;
						case "CW":						
						case "CWU":
							result = "04";
							break;
                        case "FM":
                            result = "05";
                            break;
						case "AM":
							result = "06";
							break;
						case "DIGU":
							result = "07";
							break;
						//case "SPEC":
						//    result = "08";
						//    break;
						case "DIGL":
							result = "09";
							break;
                        case "SAM":
                            result = "10";
                            break;
                        case "NFM":
                            result = "11";
                            break;
                        case "DFM":
                            result = "12";
                            break;
						case "FDV":
							result = "20";
							break;
						case "RTTY":
							result = "30";
							break;
						default:
							result = "?";
							_log.Error("Unable to convert Flex demod mode:  " + value + source);
							break;
					}
					break;
				case "KW2FLEX":
					//if (mfg.ToUpper() == "KW")
					{
						switch (value)
						{
							case "1":
								result = "00";
								break;
							case "2":
								result = "01";
								break;
							case "3":
								result = "04";
								break;
                            case "4":
                                result = "05";
                                break;
							case "5":
								result = "06";
								break;
							case "6":
								result = "09";
								break;
							case "7":
								result = "03";
								break;
							case "9":
								result = "07";
								break;
							default:
								result = "00";
								break;
						}
					}
					break;
				case "FLEX2KW":
					{
						switch (value)
						{
							case "00":
								result = "1";
								break;
							case "01":
								result = "2";
								break;
							case "03":
								result = "7";
								break;
							case "04":
								result = "3";
								break;
							case "05":
								result = "4";
								break;
							case "06":
								result = "5";
								break;
							case "07":
								result = "9";
								break;
							case "09":
							case "30":
								result = "6";
								break;
                            case "10":
                                result = "5";
                                break;
                            case "11": // NFM
                            case "12": // DFM
							case "20":  //FDV
                                result = "4";
                                break;
							default:
								_log.Error("Fell thru FLEX2KW " + value);
								break;
						}
					}
					break;
			}
			if (!result.Contains('?'))
				return api + ":" + result;
			else
				return result;
		}
	}
}
