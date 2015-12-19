using System;
using System.Diagnostics;
using System.ComponentModel;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using System.Text;
using System.IO;
using System.IO.Ports;
using System.Xml;
using System.Xml.Linq;
using System.Xml.XPath;
using System.Threading;
using Flex.Smoothlake.FlexLib;
using FabulaTech.VSPK;
using log4net;
using Cat.CatMain;
using Cat.CatMain.Ports;


namespace Cat.Cat.Ports
{
	internal sealed class SerialPortManager
	{
		#region Variables

		private ObjectManager _om;
		private Radio this_radio;
		private CatCache current_cache;
		private string[] os_port_list = new string[257];
        private List<CatSerialPort> open_port_list = new List<CatSerialPort>();
		private uint next_port;
		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);

		#endregion Variables

		#region Constructor

		internal SerialPortManager(ObjectManager om)
	    {
			_om = om;
		}

		#endregion Constructor

		#region Properties

		private static CatSerialPort current_port;
		public static CatSerialPort CurrentPort
		{
			get { return current_port; }
		}

		#endregion Properties

		#region Port Enumeration Routines

		internal void EnumerateWindowsPorts()
		{
			bool found_empty_pair = false;
			int cnt = 1;
			os_port_list = new string[256];
			try
			{
				UInt32 physical_count = EnumeratePhysical();
				for (UInt32 n = 0; n < physical_count; n++)
				{
					UInt32 port_number = 0;
					port_number = GetPhysical(n);
					os_port_list[port_number] = "COM" + port_number.ToString();
				}
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " " + e.ErrorMessage.ToString());
			}

			try
			{
				UInt32 virtual_count = EnumerateVirtual();
				string[] port_pair;
				for (UInt32 n = 0; n < virtual_count; n++)
				{
					port_pair = GetPair(n);
					UInt32 port1 = Convert.ToUInt32(port_pair[0]);
					UInt32 port2 = Convert.ToUInt32(port_pair[1]);
					os_port_list[port1] = "COM" + port_pair[0];
					os_port_list[port2] = "COM" + port_pair[1];

                    // This really only needs to happen once on startup to fensure existing pairs have the right details
                    // force port details to use our favorite settings
                    //FTVSPKPair ft_pair = new FTVSPKPair();
                    //ft_pair.PortNo1 = port1;
                    //ft_pair.PortNo2 = port2;
                    //ft_pair.BitrateEmulation = false;
                    //ft_pair.Pinout = FTVSPKSerialPinout.ftvspkPinoutFull;
                    //ft_pair.DtrToDcd = true;
                    //ft_pair.DtrToRi = false;

                    //_om.vspkControl.SetPairEx(ref ft_pair);  

				}
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " " + e.ErrorMessage.ToString());
			}

            // use the .NET serial port class to try to collect any additional information about serial ports
            // since we know that some virtual serial ports won't show up in the VSPK lists
            string[] win_portnames = SerialPort.GetPortNames();
            foreach (string portname in win_portnames)
            {
                string temp = portname;
                temp = temp.Replace("COM", "");

                int port;
                bool b = int.TryParse(temp, out port);

                if (!b) continue;

                os_port_list[port] = portname;
            }

            if (os_port_list.Count(n => n == null) == os_port_list.Length)
			{
				found_empty_pair = true;
				cnt = 4;
			}

			while (!found_empty_pair && cnt < 256)
			{
                // TODO: note that this strategy will fail for cnt > 246
                // ultimately would like a strategy that allows the user to specify the ports
				if (cnt > 3 && os_port_list[cnt] == null && os_port_list[cnt + 10] == null)
					found_empty_pair = true;
				else
					cnt++;
			}

			if (found_empty_pair)
				this.next_port = (uint)cnt;
			else
				_log.Error("No empty port pairs available");

		}


		internal UInt32 EnumerateVirtual()
		{
			UInt32 PairsCount = 0;
			try
			{
				PairsCount = _om.vspkControl.EnumPairs();
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " " + e.ErrorMessage.ToString());
			}
			return PairsCount;
		}

		internal UInt32 EnumeratePhysical()
		{
			UInt32 PortsCount = 0;
			try
			{
				PortsCount = _om.vspkControl.EnumPhysical();
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " " + e.ErrorMessage.ToString());
			}
			return PortsCount;
		}

		internal string[] GetPair(uint pair_number)
		{
			string[] output = new string[2];
			UInt32 p1 = 0;
			UInt32 p2 = 0;
			_om.vspkControl.GetPair(pair_number, ref p1, ref p2);
			output[0] = p1.ToString();
			output[1] = p2.ToString();
			return output;
		}

		internal List<object> GetPairInfo()
		{
			List<object> map = new List<object>();
			PortInfo port_map;
			try
			{
				UInt32 PairsCount = _om.vspkControl.EnumPairs();
				FTVSPKPairInfo Info = new FTVSPKPairInfo();
				for (UInt32 i = 0; i < PairsCount; ++i)
				{
					port_map = new PortInfo();
					string no_port = "UNKNOWN";
					string no_app = "UNKNOWN";
					string app = string.Empty;
					UInt32 PortNo1 = 0;
					UInt32 PortNo2 = 0;
					UInt32 Pid1 = 0;
					UInt32 Pid2 = 0;

					_om.vspkControl.GetPair(i, ref PortNo1, ref PortNo2);
					_om.vspkControl.GetPairInfo(PortNo1, PortNo2, ref Info);
					Pid1 = Info.Pid1;
					Pid2 = Info.Pid2;
					if ((int)PortNo1 > 0)
						port_map.FirstPort = "COM" + PortNo1.ToString().PadRight(8, ' ');
					else
						port_map.FirstPort = no_port.PadRight(8, ' ');
					if ((int)Pid1 > 0)
					{
                        ProcessModule processModule = Process.GetProcessById((int)Pid1).MainModule;
                        if (processModule != null)
                        {
                            app = processModule.ModuleName.ToUpper();
                            port_map.FirstApp = app.Remove(app.IndexOf(".")).PadRight(20, ' ');
                        }
					}
					else
						port_map.FirstApp = no_app.PadRight(20, ' ');
					if ((int)PortNo2 > 0)
						port_map.SecondPort = "COM" + PortNo2.ToString().PadRight(8, ' ');
					else
						port_map.SecondPort = no_port.PadRight(8, ' ');

					if ((int)Pid2 > 0)
					{
                        ProcessModule processModule = Process.GetProcessById((int)Pid2).MainModule;
                        if (processModule != null)
                        {
                            app = processModule.ModuleName.ToUpper();
                            port_map.SecondApp = app.Remove(app.IndexOf(".")).PadRight(20, ' ');
                        }

					}
					else
						port_map.SecondApp = no_app.PadRight(20, ' ');

					map.Add(port_map);
				}
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}
			return map;
		}


		internal UInt32 GetPhysical(UInt32 port_no)
		{
			UInt32 port_number = 0;
			try
			{
					port_number = _om.vspkControl.GetPhysical(port_no);
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " " + e.ErrorMessage.ToString());
			}
			return port_number;
		}

		#endregion Port Enumeration routines

		#region Methods

		public string[] GetLicenseInfo()
		{
			string[] license_info = new string[] { string.Empty, string.Empty };
			try
			{
				FTVSPKLicenseType license_type = _om.vspkControl.LicenseType;
				switch(license_type)
				{
					case FTVSPKLicenseType.ftvspkLicenseDemo:
						license_info[0] = "Demo";
						break;
					case FTVSPKLicenseType.ftvspkLicenseFull:
						license_info[0] = "Full";
						break;
					case FTVSPKLicenseType.ftvspkLicenseOem:
						license_info[0] = "OEM";
						break;
					default:
						_log.Error("Fell thru license_type with " + license_type);
						break;
				}
				license_info[1] = _om.vspkControl.LicensedCompany;
			}
			catch(FTVSPKException e)
			{
						 _log.Error(e.Message);
			}
			return license_info;
		}

		internal bool CreatePair(uint port1, uint port2)
		{
			bool success = false;
			try
			{
                //  
                // Add pair with custom settings  
                //  
                FTVSPKPair Pair = new FTVSPKPair();
                Pair.PortNo1 = port1;
                Pair.PortNo2 = port2;
                Pair.BitrateEmulation = false;
                Pair.Pinout = FTVSPKSerialPinout.ftvspkPinoutFull;
                Pair.DtrToDcd = true;
                Pair.DtrToRi = false;

                _om.vspkControl.SetPairEx(ref Pair);  

				//_om.vspkControl.SetPair(port1, port2);
                Thread.Sleep(3000);
				EnumerateWindowsPorts();
				success = true;
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " "+ e.ErrorMessage.ToString());
			}
			return success;
		}

		public void DeletePair(uint port1, uint port2)
		{
            // look through the list of open ports to ensure they are closed before we pull the plug
            for(int i=0; i<open_port_list.Count; i++)
            {
                CatSerialPort csp = open_port_list[i];
                if (csp.PortNumber == port1 ||
                    csp.PortNumber == port2)
                {
                    csp.ClosePort();
                    csp.RemovePort();
                    csp.Cache.Close();
                    csp.Cache = null;

                    open_port_list.Remove(csp);
                    i--;
                }
            }

			try
			{
				_om.vspkControl.RemovePair(port1, port2);
                Thread.Sleep(3000);
				EnumerateWindowsPorts();
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.ErrorCode.ToString() + " " + e.ErrorMessage.ToString());
			}
		}

		public void DeleteAllVirtualPairs()
		{
			try
			{
				_om.vspkControl.RemoveAll();
				RemoveRadio(this_radio);
			}
			catch (FTVSPKException e)
			{
				_log.Error(e.Message);
			}
		}

		internal void CreateCATPort(string port, string port_type, bool[] args)
		{
            // skip the shared ports as they are not opened in CAT
            if (port_type == "shared") return;
			
			CatSerialPort new_port;
			CatCache new_cache;

			if (port_type == "master" && current_port != null)
			{
				current_port.ClosePort();
				current_port.RemovePort();
			}

			if (port_type == "master" && current_cache != null)
				current_cache = null;

			try
			{
				int port_number = Convert.ToInt16(port.Substring(3));
				DefaultSerialComSpec spec = new DefaultSerialComSpec();
				new_port = new CatSerialPort(port_number, spec, port_type);

                // add the port to the list of open ports
                open_port_list.Add(new_port);
		
				new_cache = new CatCache();
				new_cache.Port = new_port;
				new_port.Cache = new_cache;

				if (args[0])
					new_port.CanPTT = true;
				if (args[1])
					new_port.PTTOnDTR = true;
				if (args[2])
					new_port.PTTOnRTS = true;

				if (port_type == "master")
				{
					current_port = new_port;
					current_cache = new_cache;
				}

			}
			catch (Exception e)
			{
				_log.Error(e.Message.ToString());
			}
		}

        // EW: This code commented as it caused problems on a clean load starting with commit hash f2534
        // older version of this function is copied below
        //internal string GetPortForSerialNumber(Radio radio)
        //{
        //    _om.DataManager.RetrievePortState();
        //    int port_count = _om.DataManager.CurrentState.Ports.Count();
        //    PortList[] port_array = new PortList[port_count];
        //    string current_port = string.Empty;

        //    foreach (PortList pl in _om.DataManager.CurrentState.Ports)
        //    {
        //        if (pl.type == "master")
        //            current_port = pl.hi_port;
        //    }
        //    // don't mess with the code below
        //    //string current_port = string.Empty;
        //    this_radio = radio;
        //    XElement doc = _om.DataManager.GetRadioXML();
        //    IEnumerable<string> port = 
        //        from r in doc.Descendants("radio")
        //        where (string)r.Attribute("serial_number") == radio.Serial
        //        select (string)r.Attribute("port");
        //    foreach (string p in port)
        //         current_port = p;

        //    if (!current_port.Contains("COM"))
        //    {
        //        current_port = AssignNewPort(radio.Serial);
        //        AddRadio(radio, current_port);
        //    }

        //    // Make sure the assigned virtual port exists
        //    bool port_exists = false;
        //    if (os_port_list.Contains(current_port))
        //        port_exists = true;

        //    if (!port_exists)
        //    {
        //        string just_port;
        //        if (current_port.Contains('-'))
        //            just_port = current_port.Remove(current_port.IndexOf('-'));
        //        else
        //            just_port = current_port;
        //        //uint port1 = uint.Parse(current_port.Substring(3));
        //        uint port1 = uint.Parse(just_port.Substring(3));

        //        uint port2 = port1 + 10;
        //        CreatePair(port1, port2);
        //    }

        //    //Create CAT ports for any slave ports found in the xml doc
        //    string port_name = string.Empty;
        //    string port_type = string.Empty;
        //    int port_number = 0;
        //    bool[] args = new bool[3];
        //    XmlDocument doc2 = _om.DataManager.GetRadioXmlDocType();
        //    XmlNodeList nodes;
        //    nodes = doc2.SelectNodes("./radios/radio[@serial_number='"+radio.Serial+"']/slave");
        //    foreach (XmlNode n in nodes)
        //    {
        //        if (n.Attributes["port"].Value != string.Empty) 
        //        {
        //            port_number = int.Parse(n.Attributes["port"].Value.Substring(3))+10;
        //            port_name = n.Attributes["port"].Value.Substring(0,3) + port_number.ToString();
        //            port_type = n.Attributes["port_type"].Value;
        //            args[0] = Convert.ToBoolean(n.Attributes["ptt"].Value);
        //            args[1] = Convert.ToBoolean(n.Attributes["dtr"].Value);
        //            args[2] = Convert.ToBoolean(n.Attributes["rts"].Value);
        //            if(port_type != "shared")
        //                CreateCATPort(port_name, port_type, args);
        //        }
        //    }
        //    return current_port;
        //}

        internal string GetPortForSerialNumber(Radio radio)
        {
            string current_port = string.Empty;
            this_radio = radio;
            XElement doc = _om.DataManager.GetRadioXML();
            IEnumerable<string> port =
                    from r in doc.Descendants("radio")
                    where (string)r.Attribute("serial_number") == radio.Serial
                    select (string)r.Attribute("port");
            foreach (string p in port)
                current_port = p;

            if (!current_port.Contains("COM"))
            {
                current_port = AssignNewPort(radio.Serial);
                AddRadio(radio, current_port);
            }

            // Make sure the assigned virtual port exists
            bool port_exists = false;
            if (os_port_list.Contains(current_port))
                port_exists = true;

            if (!port_exists)
            {
                string just_port;
                if (current_port.Contains('-'))
                    just_port = current_port.Remove(current_port.IndexOf('-'));
                else
                    just_port = current_port;
                //uint port1 = uint.Parse(current_port.Substring(3));
                uint port1 = uint.Parse(just_port.Substring(3));

                uint port2 = port1 + 10;
                CreatePair(port1, port2);
            }

            //Create CAT ports for any slave ports found in the xml doc
            string port_name = string.Empty;
            string port_type = string.Empty;
            int port_number = 0;
            bool[] args = new bool[3];
            XmlDocument doc2 = _om.DataManager.GetRadioXmlDocType();
            XmlNodeList nodes = doc2.SelectNodes("./radios/radio[@serial_number='" + radio.Serial + "']/slave");
            if (nodes == null) return current_port;

            foreach (XmlNode n in nodes)
            {
                XmlAttributeCollection attrs = n.Attributes;
                if (attrs == null) continue;

                if (attrs["port"].Value != string.Empty)
                {
                    port_number = int.Parse(attrs["port"].Value.Substring(3)) + 10;
                    port_name = attrs["port"].Value.Substring(0, 3) + port_number.ToString();
                    port_type = attrs["port_type"].Value;
                    args[0] = Convert.ToBoolean(attrs["ptt"].Value);
                    args[1] = Convert.ToBoolean(attrs["dtr"].Value);
                    args[2] = Convert.ToBoolean(attrs["rts"].Value);
                    if (port_type != "shared")
                        CreateCATPort(port_name, port_type, args);
                }
            }
            return current_port;
        }


		internal string CreateNewVirtualPair()
		{
			string this_port = next_port.ToString();
			string new_port;
			bool success = CreatePair(next_port, next_port + 10);
			if (success)
				new_port = "COM" + this_port;
			else
				new_port = "FAILED";
			return new_port;
		}

		internal string AssignNewPort(string serialno)
		{
			string this_port = next_port.ToString();
			CreatePair(next_port, next_port + 10); 
			string port_assigned = "COM" + this_port;
			return port_assigned;
		}

		private void AddRadio(Radio radio, string port)
		{
			XElement doc = _om.DataManager.GetRadioXML();
			doc.Add(new XElement("radio", 
						new XAttribute("model", radio.Model),
						new XAttribute("serial_number", radio.Serial),
						new XAttribute("ip_address", radio.IP.ToString()), 
						new XAttribute("port", port),
						new XAttribute("app", "")));
			_om.DataManager.Save(doc);


//            CatStatus new_port = new CatStatus();
//            PortInfo port_info = new PortInfo();
//            new_port.radio_model = radio.Model;
//            new_port.radio_sn = radio.Serial;
//            new_port.ip_address = radio.IP.ToString();
//            port_info.FirstPort = port;
//            new_port.Ports[0].lo_port = port;
////			_om.DataManager.CreatePortInfoForSerialNumber(new_port);
		}

		public void AddSlavePort2XML(string sn, string port, string port_type, bool[] args)
		{
			bool ptt = args[0];
			bool dtr = args[1];
			bool rts = args[2];
			
			XElement doc = _om.DataManager.GetRadioXML();
			XElement new_slave = new XElement("slave", 
				new XAttribute("port", port),
				new XAttribute("port_type", port_type),
				new XAttribute("ptt", ptt),
				new XAttribute("dtr", dtr),
				new XAttribute("rts", rts),
				new XAttribute("app", ""));
					;
			try
			{
				doc.Descendants("radio").Single(s => s.Attributes("serial_number").Single().Value == sn)
					.Add(new_slave);
				_om.DataManager.Save(doc);
		    }
			catch (Exception e)
			{
				_log.Error(e.Message);
			}
		}

		public void DeleteSlavePortFromXML(string sn, string port)
		{
			XmlDocument doc = _om.DataManager.GetRadioXmlDocType();
			XmlNode node;
			node = doc.SelectSingleNode("./radios/radio[@serial_number='" + this_radio.Serial + "']/slave[@port='" + port + "']");
			if (node != null && node.ParentNode != null)
				node.ParentNode.RemoveChild(node);

			_om.DataManager.SaveDoc(doc);
		}

		public string GetPortType(string sn, string port)
		{
			//string port_type = string.Empty;
			string port_type = "-" + (int.Parse(port.Substring(3)) + 10).ToString();
			XmlDocument doc = _om.DataManager.GetRadioXmlDocType();			
            
            XmlNode node = doc.SelectSingleNode("./radios/radio[@serial_number='" + this_radio.Serial + "']/slave[@port='" + port + "']");
            if (node == null) return port_type;

            XmlAttributeCollection attrs = node.Attributes;
            if (attrs == null) return port_type;

			if (attrs["port"].Value == port)
				port_type += " ["+attrs["port_type"].Value;

			if (port_type.Contains("ptt"))
			{
				if (attrs["rts"].Value == "true" && attrs["dtr"].Value == "false")
				{
					port_type += " rts";
				}
				else if (attrs["dtr"].Value == "true" && attrs["rts"].Value == "false")
				{
					port_type += " dtr";
				}
				else if (attrs["dtr"].Value == "true" && attrs["rts"].Value == "true")
				{
					port_type += " dtr/rts";
				}
			}
			port_type += "]";
								
			return port_type;
		}

		public void UpdatePortAssignment(string sn, string port)
		{
			XElement doc = _om.DataManager.GetRadioXML();
			IEnumerable<XElement> el =
				from r in doc.Descendants("radio")
				where (string)r.Attribute("serial_number") == sn
				select (XElement)r;

			IEnumerator<XElement> cnt = el.GetEnumerator();
			cnt.MoveNext();
			cnt.Current.SetAttributeValue("port", port);
			_om.DataManager.Save(doc);
		}

		public void RemoveRadio(Radio radio)
		{
			XElement doc = _om.DataManager.GetRadioXML();
			doc.Descendants("radio").Where( x => (string)x.Attribute("serial_number") == radio.Serial).Remove();
			_om.DataManager.Save(doc);
		}

		//internal void UpdateMainAppAssignment(string sn, string port, string app)
		//{
		//    string this_app = app.Substring(8, 20).Trim();
		//    XElement doc = _om.DataManager.GetRadioXML();
		//    IEnumerable<XElement> el =
		//        from r in doc.Descendants("radio")
		//        where (string)r.Attribute("serial_number") == sn
		//        select (XElement)r;

		//    IEnumerator<XElement> cnt = el.GetEnumerator();
		//    cnt.MoveNext();
		//    if (cnt.Current.Value != this_app)
		//    {
		//        cnt.Current.SetAttributeValue("app", this_app);
		//        _om.DataManager.Save(doc);
		//    }
		//}

		//internal void UpdateSlaveAppAssignment(string sn, string app)
		//{
		//    string port1 = app.Substring(0, 8).Trim();
		//    string app1 = app.Substring(8, 20).Trim();
		//    string port2 = app.Substring(28, 8).Trim();
		//    string app2 = app.Substring(36, 20).Trim();

		//    XmlDocument doc = _om.DataManager.GetRadioXmlDocType();
		//    XmlNode node;
		//    XmlAttributeCollection attrs;
		//    node = doc.SelectSingleNode("./radios/radio[@serial_number='" + this_radio.Serial + "']/slave[@port='" + port1 + "']");
		//    if (node != null)
		//    {
		//        attrs = node.Attributes;
		//        if (attrs["app"].Value != "UNKNOWN" && attrs["app"].Value != "CAT" && attrs["app"] != null)
		//            attrs["app"].Value = app1;
		//    }
		//    _om.DataManager.SaveDoc(doc);
		//}

		//internal void RestorePortState(CatStatus this_state)
		//{
		//    for (int n = 0; n < this_state.Ports.Count(); n++)
		//    {
		//        uint port1 = uint.Parse(this_state.Ports[n].lo_port.Substring(3));
		//        uint port2 = uint.Parse(this_state.Ports[n].hi_port.Substring(3));
		//        CreatePair(port1, port2);
		//    }
		//}

		//internal bool IsProcessRunning(string name)
		//{
		//    foreach (Process p in Process.GetProcesses())
		//    {
		//        if (p.ProcessName.Contains(name))
		//        {
		//            return true;
		//        }
		//    }
		//    return false;
		//}

		#endregion Methods
	}
}
