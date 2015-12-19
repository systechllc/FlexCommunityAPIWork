using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cat.CatMain.Ports
{
	public class PortInfo
	{
		public PortInfo()
		{
		}

		private string port_type = string.Empty;
		public string PortType
		{
			get { return port_type; }
			set { port_type = value; }
		}

		private string serial_number = string.Empty;
		public string SerialNo
		{
			get { return serial_number; }
			set { serial_number = value; }
		}

		private string first_port = string.Empty;
		public string FirstPort
		{
			get { return first_port; }
			set { first_port = value; }
		}

		private string first_app = string.Empty;
		public string FirstApp
		{
			get { return first_app; }
			set { first_app = value; }
		}

		private string second_port = string.Empty;
		public string SecondPort
		{
			get { return second_port; }
			set { second_port = value; }
		}

		private string second_app = string.Empty;
		public string SecondApp
		{
			get { return second_app; }
			set { second_app = value; }
		}
	}
}
