using System;
using System.Reflection;
using AopAlliance.Intercept;

using log4net;

namespace Cat
{
	public class CatAroundAdvice : IMethodInterceptor
	{

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		public object Invoke(IMethodInvocation invocation)
		{
			object retVal;
			try
			{
				retVal = invocation.Proceed();
				return retVal;
			}
			catch (Exception e)
			{
				retVal = "?;";
				_log.Error(e.Message);
			}
			return retVal;
		}

		#region Documentation

		/*! \class Cat.CatAroundAdvice
		 * \brief       Dynamic around method interceptor for trapping exceptions.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		#endregion Documentation
	}
}