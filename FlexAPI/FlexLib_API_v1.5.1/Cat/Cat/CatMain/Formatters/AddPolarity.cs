using System;
using Cat.Cat.Interfaces;

namespace Cat.Cat.Formatters
{
	public class AddPolarity : ICatReturnFormat
	{
		public string Format(object[] args)
		{
			string value = args[0].ToString();
			int length = (int)args[1];
			bool IsNegative = value.Contains("-") || int.Parse(value) < 0;

			value = value.TrimStart('+', '-');
			value = value.PadLeft(length-1, '0');
			if (IsNegative)
				return "-" + value;
			else
				return "+" + value;
		}

		#region Documentation

		/*! \class Cat.Cat.Formatters.CatAddPolarity
		 * \brief       Generic formatter to add polarity to signed CAT command answers.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \fn Format
		 *  \brief      Formats a CAT command answer that is polarized.  Determines the polarity of the answer (args[0]), adds the
		 *              correct sign, and inserts leading zeros to achieve the correct length (args[1]).
		 */

		#endregion Documentation
	}
}
