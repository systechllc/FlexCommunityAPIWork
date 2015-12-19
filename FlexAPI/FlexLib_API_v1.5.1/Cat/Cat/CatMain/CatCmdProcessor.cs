using System;
using System.Diagnostics;
using System.Reflection;
using log4net;
using Cat.Cat.Interfaces;

namespace Cat.Cat
{
	internal sealed class CatCmdProcessor
	{

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		ObjectManager _om;


		internal CatCmdProcessor(ObjectManager om)

		{
			_om = om;
		}

		internal object ExecuteCommand(string cmd)
		{
			 cmd = cmd.TrimEnd(';').ToUpper();
			 //Debug.WriteLine("cmd processor received:" + cmd);
			 ICatCmd catCmd = _om.Factory.GetCatCommand(cmd);
			 if (catCmd != null)
			 {
				 return catCmd.Execute(cmd);
			 }
			 else
			 {
				 _log.Error(catCmd);
				 return "?;";
			 }

		}

		#region Documentation

		/*! \class Cat.Cat.CatCmdProcessor
		 * \brief       Receives commands from the CatCache queue and calls the Factory to create a CAT command instance. 
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \var _om
		 *  \brief      An instance of the Object Manager.
		 */

		/*! \fn ExecuteCommand
		 *  \brief      Removes the command terminator, converts to upper case, calls the Factory to create a command, and executes 
		 *              the returned command.
		 */

		#endregion Documentation


	}
}
