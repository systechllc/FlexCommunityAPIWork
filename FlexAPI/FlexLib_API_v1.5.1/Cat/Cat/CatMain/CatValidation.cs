using System.Reflection;
using log4net;
using Spring.Context.Support;
using Spring.Context;
using Spring.Aop.Framework;

using System;


namespace Cat.Cat
{
	public class CatValidation 
	{

		//Validation to make user the cat command is not null or empty
		public static bool IsEmptyOrNull(string cmd)
		{
			bool passed = string.IsNullOrEmpty(cmd);
			return passed;
		}

		//Validation test to validate the length of the command
		public static bool CmdLength(string cmd, int get_length, int set_length)
		{
			bool passed = (cmd.Length == get_length || cmd.Length == set_length);
			return passed;
		}

		//Validation test to validate the range of the suffix
		public static bool SuffixRange(string cmd, int suffix, int min, int max)
		{
			bool passed = (suffix >= min && suffix <= max);
			return passed;
		}

		//Tests for validity of key in State Manager dictionary
		//TODO:  Add connection to StateMgr
		public static bool IsValidKey(string Prefix)
		{
			bool passed = true;
			return passed;
		}

		#region Documentation

		/*! \class Cat.Cat.CatValidation
		 * \brief       Validation tests for CAT command parameters.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \var    IsEmptyOrNull
		 *  \brief      Tests for an empty command.
		 */

		/*! \var    CmdLength
		 *  \brief      Tests the overall length of the command.
		 */

		/*! \var    SuffixRange
		 *  \brief      Tests for the correct range in a Set command suffix.
		 */

		/*! \var    IsValidKey
		 *  \brief      Test to see if a key exists in the CAT command dictionary.
		 */

		#endregion Documentation
	}
}
