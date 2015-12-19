using System;
using Spring.Context;
using Cat.Cat.Formatters;
using Cat.Cat.Interfaces;
using System.Diagnostics;


namespace Cat.Cat
{
	//Base template for CAT commands
	public class CatBase : ICatCmd
	{
		#region Injected Properties

		private string _api = string.Empty;
		public string API
		{
			get { return _api; }
			set { _api = value; }
		}
		private string _prefix = string.Empty;
		public string Prefix
		{
			get { return _prefix; }
			set { _prefix = value; }
		}

		private bool _isreadonly = false;
		public bool IsReadOnly
		{
			get { return _isreadonly; }
			set { _isreadonly = value; }
		}

		private bool _iswriteonly = false;
		public bool IsWriteOnly
		{
			get { return _iswriteonly; }
			set { _iswriteonly = value; }
		}

		private bool _ispolarized = false;
		public bool IsPolarized
		{
			get { return _ispolarized; }
			set { _ispolarized = value; }
		}

		private bool _isindexed = false;
		public bool IsIndexed
		{
			get { return _isindexed; }
			set { _isindexed = value; }
		}

		private int _setlength = 1;
		public int SetLength
		{
			get { return _setlength; }
			set { _setlength = value; }
		}

		private int _getlength = 0;
		public int GetLength
		{
			get { return _getlength; }
			set { _getlength = value; }
		}

		private int _answerlength = 1;
		public int AnswerLength
		{
			get { return _answerlength; }
			set { _answerlength = value; }
		}

		private int _indexlength = 0;
		public int IndexLength
		{
			get { return _indexlength; }
			set { _indexlength = value; }
		}

		private ValidRange _range;
		public ValidRange Range
		{
			get { return _range; }
			set { _range = (ValidRange)value; }
		}

		private ICatStateMgr state_mgr;
		internal ICatStateMgr StateMgr
		{
			get { return state_mgr; }
			set { state_mgr = (ICatStateMgr)value; }
		}

		private ICatReturnFormat _catreturnformatter;
		public ICatReturnFormat CatReturnFormatter
		{
			get { return _catreturnformatter; }
			set { _catreturnformatter = (ICatReturnFormat)value; }
		}

		private ICatReturnFormat _catapiformatter;
		public ICatReturnFormat CatAPIFormatter
		{
			get { return _catapiformatter; }
			set { _catapiformatter = (ICatReturnFormat)value; }
		}
		#endregion Injected Properties

		#region Local Variables

		private string _suffix = string.Empty;
		private string _index = string.Empty;
		private bool IsSetCmd;

		#endregion Local Variables

		public object Execute(string cmd)
		{
			try
			{
				if (IsIndexed)
				{
					_index = cmd.Substring(_prefix.Length, _indexlength);
					_suffix = cmd.Substring(_prefix.Length + _indexlength);
				}
				else
				{
					_suffix = cmd.Substring(_prefix.Length);
				}

				if (_suffix.Length > 0)
				{
					if (IsWriteOnly || !IsReadOnly)
						IsSetCmd = true;
					else
						IsSetCmd = false;
				}
				else
				{
					if (IsReadOnly || !IsWriteOnly)
						IsSetCmd = false;
					else
						IsSetCmd = true;
				}

				string result = string.Empty;


				if (IsValidCommand(cmd))
				{
                    string mode_property = "DemodMode0F";

                    // handle ZZFJ needing Slice B DemodMode
                    if (cmd.StartsWith("ZZFJ"))
                        mode_property = "DemodMode1F";

					if (IsSetCmd)
					{
                        object[] args = { _prefix, _suffix, _api, StateMgr.Get(mode_property) };
						string[] ans = { null, null };

						if (CatAPIFormatter != null)
						{
							ans = CatAPIFormatter.Format(args).Split(':');
							StateMgr.Set(ans[0], ans[1]);
						}
						else
							StateMgr.Set(args[2].ToString(), args[1].ToString());
					}

					else
					{
                        object[] args_in = { _prefix, StateMgr.Get(API), _api, StateMgr.Get(mode_property) };
						string[] ans = { null, null };
						if (CatAPIFormatter != null)
						{
							ans = CatAPIFormatter.Format(args_in).Split(':');
							result = _prefix + _index + ans[1] + ";";
						}
						else
						{
							if ((string)args_in[1] != "?;")
							{
								object[] args_out = { args_in[1].ToString(), _answerlength };
								result = _prefix + _index + CatReturnFormatter.Format(args_out) + ";";
							}
							else
								result = args_in[1].ToString();
						}
					}
				}
				return result;
			}
			catch (Exception e)
			{
                Debug.WriteLine("Error handling CAT command: (" + cmd + ")");
				string x = e.Message;
				return "?;";
			}
		}


        private bool IsValidCommand(string cmd)
        {
            // if the command is KY, use a different set of rules for more lenient processing
            if (_prefix == "KY")
            {
                if (cmd.Length < 3)
                    throw new Exception(_prefix + " command length error");
                else return true;
            }

            //Overall command lengths for get and set
            if (!CatValidation.CmdLength(cmd, _prefix.Length, _prefix.Length + _setlength))
                throw new Exception(_prefix + " command length error");

            //Suffix length for get and set
            if (!CatValidation.CmdLength(_suffix, _setlength, _getlength))
                throw new Exception(_prefix + " suffix length error");

            //Range of the suffix
            if (_suffix != string.Empty)
            {
                if (_prefix == "FA" || _prefix == "FB" ||
                    _prefix == "ZZFA" || _prefix == "ZZFB")
                    return true;

                if (!CatValidation.SuffixRange(cmd, int.Parse(_suffix), _range.Min, _range.Max))
                    throw new Exception(_prefix + " suffix range error");
            }

            return true;
        }

		#region Documentation

		/*! \class Cat.Cat.CatBase
		 * \brief       Base template for CAT classes
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \var Prefix
		 *  \brief      Holds the CAT command prefix (string::2 or 4 characters).  
		 */
  
		/*! \var IsReadOnly
		 *  \brief      Indicates the CAT command is read-only (bool::default = false).
		 */

		/*! \var IsWriteOnly
		 *  \brief      Indicates the CAT command is write-only (bool::default = false).
		 */

		/*! \var IsPolarized
		 *  \brief      Indicates the CAT command contains a plus or minus sign (bool::default = false).
		 */

		/*! \var IsIndexed
		 *  \brief      Indicates the CAT command contains a radio index (bool::default = false).
		 */

		/*! \var SetLength
		 *  \brief      The length of the suffix for a set-type CAT command (string::default = 1).
		 */

		/*! \var GetLength
		 *  \brief      The length of the suffix for a get-type CAT command (string::default = 0).
		 */

		/*! \var AnswerLength
		 *  \brief      The length of the answer for a get-type CAT command (string::command dependent).
		 */

		/*! \var IndexLength
		 *  \brief      The length of the radio index (if used) (string::default = 0).
		 */

		/*! \var Range
		 *  \brief      The allowable range of the set-type suffix. (string::default min = 0, max = 1).
		 */

		/*! \var StateMgr
		 *  \brief      An instance of the dictionary of CAT commands to targets.
		 */

		/*! \var CatReturnFormatter
		 *  \brief      An instance of the particular formatting class required for the CAT command.
		 */

		/*! \var _suffix
		 *  \brief      A local variable to hold the suffix of the current CAT command.
		 */

		/*! \var _index
		 *  \brief      A local variable to hold the index of the current CAT command.
		 */

		/*! \var IsSetCommand
		 *  \brief      A local variable to hold the set/get state of the current CAT command.
		 */

		/*! \fn Execute
		 *  \brief      Decodes the various parameters of the CAT command and performs the indicated operation on the StateMgr.  
		 *              The Execute method is wrapped in a proxy that calls an around-type method interceptor that handles error 
		 *              logging and an after-type method interceptor for command post processing (if required).
		 */

		/*! \fn IsValidCommand
		 *  \brief      Test the various CAT command parameters for validity.
		 */

		#endregion Documentation
	}
}
