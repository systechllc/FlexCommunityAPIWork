using System;
using Cat.Cat.Interfaces;

namespace Cat.Cat.Formatters
{
	class AddLeadingZeros : ICatReturnFormat
	{
		public AddLeadingZeros()
		{
		}
 
		public string Format(object[] args)
		{
			string value = (string)args[0];
			int length = (int)args[1];
			return value.PadLeft(length, '0');
		}

		#region Documentation

		/*! \class Cat.Cat.Formatters.CatAddLeadingZeros
		 * \brief       Generic formatter to add leading zeros to a CAT command answer.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \fn Format
		 *  \brief      Inserts leading zeros in a CAT answer to achieve the correct AnswerLength.  Parameter args[0] is the raw 
		 *              answer, args[1] is the length required.
		 */

		#endregion Documentation
	}
}
