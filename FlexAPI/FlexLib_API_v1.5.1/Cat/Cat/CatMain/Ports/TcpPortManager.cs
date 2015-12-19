using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cat.Cat.Ports
{
	public class TcpPortManager
	{
		ObjectManager _om;

		internal TcpPortManager(ObjectManager om)
		{
			_om = om;
		}

		public string HandleRequest(string args)
		{
			string ans = string.Empty;
			if(!args.Contains("\r\n"))
				ans = (string)_om.CatCmdProcessor.ExecuteCommand(args);
			return ans;
		}
	}
}
