using System;
using System.Diagnostics;
using System.Reflection;
using Cat.Cat;
using Cat.Cat.Interfaces;
using log4net;
using AopAlliance.Intercept;
using Spring.Aop.Framework;
using Spring.Context;
using Spring.Context.Support;
using Spring.Core.IO;
using Spring.Objects.Factory;
using Spring.Objects.Factory.Config;
using Spring.Objects.Factory.Xml;


namespace Cat
{
	internal sealed class Factory
	{
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		private readonly IObjectFactory _factory;
 
		internal Factory()
		{
		   IResource input = new FileSystemResource("Cfg/Libs/CatClasses.xml");
			_factory = new XmlObjectFactory(input);
		}


		internal ICatCmd GetCatCommand(string cmd)
		{
			string ncmd = string.Empty;
			try
			{
				if (cmd.Substring(0, 2) == "ZZ")
					ncmd = cmd.Substring(0, 4);
				else
					ncmd = cmd.Substring(0, 2);
				var catCmd = (ICatCmd)_factory.GetObject(ncmd);
				ProxyFactory pfactory = new ProxyFactory(typeof(ICatCmd));
				pfactory.AddAdvice(new CatAroundAdvice());
				switch (ncmd)
				{
					case "FA":
					case "ZZFA":
					case "FB":
					case "ZZFB":
					case "MD":
					case "ZZMD":
						pfactory.AddAdvice(new CatAfterAdvice());
						break;
					default:
						break;
				}
				//if(ncmd == "FA" || ncmd == "ZZFA")
				//    pfactory.AddAdvice(new CatAfterAdvice());
				pfactory.Target = catCmd;
				ICatCmd pcmd = (ICatCmd)pfactory.GetProxy();
				return pcmd;
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message + "@ " + ncmd); 
			}
			return null;
		}

		#region Documentation

		/*! \class Cat.Factory
		 * \brief       Creates CAT commands on demand.
		 * 
		 *              Uses the Spring.NET XMLObjectFactory to create CAT commands based on parameters in an external XML file.  
		 *              
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \fn     Factory
		 *  \brief      The constructor.
		 *  
		 *              Reads the XML data from a resource file and creates a factory from the input.
		 *              
		 */

		/*! \fn     GetCatCommand
		 *  \brief      Creates an instance of a CAT command from the XML object factory.
		 *  
		 *              The class name of a CAT command is the command prefix.  Any suffix (radio index or set data) is stripped 
		 *              from the incoming command and what is left is used to try to create a new instance of a class.  If this 
		 *              succeeds, a proxy is created around the class and an around-type method interceptor is added.  If the 
		 *              class requires post-processing an after-returning method interceptor is also added.
		 *              
		 */


		#endregion Documentation
	}
}