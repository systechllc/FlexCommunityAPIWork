using System;
using System.Reflection;
using Cat.Cat.Interfaces;
using log4net;

namespace Cat.Cat.Formatters
{
	public class ReadMultiMeter : ICatReturnFormat
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

	   public string  Format(object[] args)
		{
			string value = args[0].ToString();
			int length = (int)args[1];
			string index = args[2].ToString();
			string sign = "+";
			bool IsNegative = value.Contains("-") || double.Parse(value) < 0;
 
			if (IsNegative)
				sign = "-";


			value = value.TrimStart('-', '+');

		   switch(index)
		   {
			   case "0":
			   case "1":
				   value = sign + value + " dBm";
				   break;
			   case "2":
			   case "3":
				   value = sign + value + " dBFS";
				   break;
			   case "4":
				   value = sign + value + " dB";
				   break;
			   case "5":
			   case "7":
				   value = value + " W";
				   break;
			   default:
				   _log.Error("Fell thru index selection with index " + index);
				   break;
		   }
		   return value.PadLeft(length, '0');
		}

		#region Documentation

		/*! \class Cat.Cat.Formatters.CatReadMultiMeter
		 *  \brief      Custom class to read the PowerSDR 2.0 Console Multimeter.
		 *  
		 * Determines the polarity of the answer (args[0]), adds the correct sign, determines the engineering units from the 
		 * index (args[2]), and pads the answer with the correct number of leading zeros to equal the command length (args[1]).
		 *  
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		#endregion Documentation
	}
}
