using System;
using System.Reflection;
using Spring.Aop;
using Cat.Cat;
using log4net;

namespace Cat
{
	class CatAfterAdvice : IAfterReturningAdvice
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private ICatStateMgr stateManager = new CatStateManager();
		private CatBase _target;

		public void AfterReturning(object returnValue, MethodInfo method, object[] args, object target)
		{
			_target = (CatBase)target;
			string _api = _target.API;
			string _args = args[0].ToString();
			
			switch (_api)
			{
                case "CWSend":
                    break;
				case "Freq0":
					//string freq = string.Empty;

					//if (_args.Length == 13 || _args.Length == 15)
					//{
					//    if (_args.StartsWith("FA"))
					//        freq = _args.Substring(2);
					//    else if (_args.StartsWith("ZZFA"))
					//        freq = _args.Substring(4);
					//string[] fargs = { "freq", freq };
					//csw.Update(fargs);
					//}
					break;
				case "DemodMode0F":
				case "DemodMode0K":
					//string mode = string.Empty;

					//if (_args.Length == 3 || _args.Length == 6)
					//{
					//    if (_args.StartsWith("MD"))
					//    {
					//        mode = _args.Substring(2);
					//    }
					//    else if (_args.StartsWith("ZZMD"))
					//    {
					//        mode = _args.Substring(4);
					//    }
					//}
					//string[] dmm_args = { _api, mode };
					//csw.Update(dmm_args);
					break;
				case "Transmit":
					break;
				default:
					break;

			}
		}

		#region Documentation

		/*! \class Cat.CatAfterAdvice
		 * \brief       Dynamic after-returning method interceptor for post-processing CAT commands if required.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */


		#endregion Documentation
	}
}
