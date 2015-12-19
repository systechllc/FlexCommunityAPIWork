using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cat.Cat.Formatters
{
	public class ValidRange
	{
		private int _min = 0;
		public int Min
		{
			get { return _min; }
			set { _min = value; }
		}

		private int _max = 1;
		public int Max
		{
			get { return _max; }
			set { _max = value; }
		}

		#region Documentation

		/*! \class Cat.Cat.Formatters
		 *.CatRange
		 * \brief      Generic class to hold the suffix range of a set-type CAT command.  
		 
					   Injected into each CAT command from specified in the Cat.Cfg.Libs.FlexCatCmdr.xml file.  Min/Max variable are specific 
					   for each CAT command, the defaults (Min = 0/Max = 1) match the requirements for a binary-type command.  
		  
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \var Min
		 *  \brief      Holds the minimum range value for a set-type CAT command suffix (string::default = 0).
		 */

		/*! \var Max
		 *  \brief      Holds the maximum range value for a set-type CAT command suffix (string::default = 1).
		 */

		#endregion Documentation
	}
}
