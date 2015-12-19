using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Cat.Cat.Interfaces;
using System.Reflection;
using log4net;

namespace Cat.Cat.Formatters
{

	public class ConvertAGCMode : ICatReturnFormat
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public ConvertAGCMode()
		{

		}

		public string Format(object[] args)
		{
			string source = (string)args[0];
			string value = (string)args[1];
			string api = (string)args[2];
			string mode = (string)args[3];
			string result = "?;";

			if (api == "AGCModeK")
			{
				switch (value.ToUpper())
				{
					case "OFF":
						result = "000";
						break;
					case "SLOW":
						result = "002";
						break;
					case "MED":
						result = "003";
						break;
					case "FAST":
						result = "004";
						break;
					case "000":
						result = "OFF";
						break;
					case "002":
						result = "SLOW";
						break;
					case "003":
						result = "MED";
						break;
					case "004":
						result = "FAST";
						break;
					default:
						_log.Error("Fell thru AGCModeK value " + value);
						break;
				}

			}
			else if (api == "AGCModeF")
			{
				switch (value.ToUpper())
				{
					case "OFF":
						result = "0";
						break;
					case "SLOW":
						result = "2";
						break;
					case "MED":
						result = "3";
						break;
					case "FAST":
						result = "4";
						break;
					case "0":
						result = "OFF";
						break;
					case "2":
						result = "SLOW";
						break;
					case "3":
						result = "MED";
						break;
					case "4":
						result = "FAST";
						break;
					default:
						_log.Error("Fell thru AGCModeF " + value);
						break;
				}
			}
			if (!result.Contains('?'))
				return api + ":" + result;
			else
				return result;
		}

	}
}
