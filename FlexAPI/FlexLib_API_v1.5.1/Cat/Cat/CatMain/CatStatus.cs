using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using System.IO;

namespace Cat.CatMain
{
	[XmlRoot(ElementName="CatStatus", DataType="string", IsNullable=false)]
	public class CatStatus
	{
		[XmlAttribute]
		public string radio_sn;
		[XmlAttribute]
		public string radio_model;
		[XmlAttribute]
		public string ip_address;
		[XmlArray("Options")]
		public OptionList[] Options;
		[XmlArray("Ports")]
		public PortList[] Ports;
	}

	public class PortList
	{
		[XmlAttribute]
		public string type;
		[XmlAttribute]
		public string lo_port;
		[XmlAttribute]
		public string lo_port_app;
		[XmlAttribute]
		public string hi_port;
		[XmlAttribute]
		public string hi_port_app;
		[XmlAttribute]
		public bool ptt;
		[XmlAttribute]
		public bool dtr;
		[XmlAttribute]
		public bool rts;
	}

	public class OptionList
	{
		[XmlAttribute]
		public string name;
		[XmlAttribute]
		public string state;
	}
}
