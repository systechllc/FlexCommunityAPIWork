/*! \class Cat.DataManager
  * \brief       Manages the creation and recovery of data files for Cat Commander.  
  * \			 An instance of DataManager is created in ObjectManager at start time.
  * \author      Bob Tracy, K5KDN
  * \version     0.1Alpha
  * \date        03/2013
  * \copyright   FlexRadio Systems
  */


using System;
using System.Data;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Concurrent;
using System.Reflection;
using System.Linq;
using System.Xml;
using System.Xml.Serialization;
using System.Xml.Linq;
using System.Text;
using System.Net.Sockets;
using log4net;
using System.Windows.Forms;
using Flex.Smoothlake.FlexLib;
using Cat.CatMain;
using Cat.CatMain.Ports;

namespace Cat.Cfg
{
	internal sealed class DataManager
	{
		#region Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		private string radio_file = "radios.xml";
		private XElement doc;
		private string flex_app_data_path = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData)
			+ @"\FlexRadio Systems\SmartSDR\Cat\";
		private DirectoryInfo di;
		private DirectoryInfo current_folder;

		#endregion Variables

		#region Properties

		private Radio current_radio = null;
		public Radio CurrentRadio
		{
			get { return current_radio; }
			set 
			{
				current_radio = value;
				GetPathToCatAppData();
			}
		}

		private static CatStatus current_state;
		public CatStatus CurrentState
		{
			get { return current_state; }
			set { current_state = value; }
		}

		#endregion Properties

		#region Constructor


		internal DataManager()

		{
			//GetPathToCatAppData();
			di = new DirectoryInfo(flex_app_data_path);
		}

		#endregion Constructor

		#region Methods

		/*! \fn GetPathToCatAppData
		 *  \brief      Checks to see if the directory path and xml file exist.  If not, creates them. 
		 */

		internal void GetPathToCatAppData()
		{

			if (!Directory.Exists(flex_app_data_path))
			{
				try
				{
					Directory.CreateDirectory(flex_app_data_path);
				}
				catch (Exception e)
				{

					_log.Error(e.ToString());
				}
			}

			if (!File.Exists(flex_app_data_path + radio_file))
			{
				try
				{
					FileStream fs = File.Create(flex_app_data_path + radio_file);
					fs.Close();
					doc = new XElement("radios");
					doc.Save(flex_app_data_path + radio_file);
				}
				catch (Exception e)
				{
					_log.Error(e.Message);
				}
			}
			if(!di.Exists)
				try
				{
					di.Create();
				}
				catch (Exception e)
				{
					_log.Error(e.Message);
				}
			current_folder = di.CreateSubdirectory(current_radio.Serial);
		}

		//internal void CreatePortInfoForSerialNumber(CatStatus port_info)
		//{
		//    XmlSerializer serializer = new XmlSerializer(typeof(CatStatus));
		//    string this_path = (string)current_folder.FullName + "\\PortInfo.xml";
		//    if (File.Exists(this_path))
		//    {
		//        DialogResult ans = MessageBox.Show("Overwrite the existing state for serial number " + current_radio.Serial + "?",
		//            "Existing State", MessageBoxButtons.YesNo);
		//        if (ans == DialogResult.Yes)
		//            File.Delete(this_path);
		//        else
		//            return;
		//    }
		//    TextWriter writer = new StreamWriter(this_path);
		//    CatStatus new_state = new CatStatus();
		//    new_state.radio_model = current_radio.Model;
		//    new_state.radio_sn = current_radio.Serial;
		//    new_state.ip_address = current_radio.IP.ToString();
		//    new_state. = port_info.port;
		//    serializer.Serialize(writer, new_state);
		//    writer.Close();

		//}

		internal void DeleteRadioXML()
		{
			try
			{
				File.Delete(flex_app_data_path + radio_file);
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}

		}

 		/*! \fn GetRadioXML
		 *  \brief      Retrieves an XElement copy of the radios that have been attached.
		 */

		internal XElement GetRadioXML()
		{
			XElement doc = XElement.Load(flex_app_data_path+radio_file);
			return doc;
		}

		internal XmlDocument GetRadioXmlDocType()
		{
			XmlDocument doc = new System.Xml.XmlDocument();
			doc.Load(flex_app_data_path + radio_file);
			return doc;
		}

		/*! \fn Save
		 *  \brief       Saves the edited copy of radios.xml.
		 */

		internal void Save(XElement doc)
		{
			try
			{
				//XElement _doc = doc;
				doc.Save(flex_app_data_path + radio_file);
			}
			catch(Exception e)
			{
				_log.Error(e.Message);
			}
		}

		internal void SaveDoc(XmlDocument doc)
		{

			try
			{
				doc.Save(flex_app_data_path + radio_file);
			}
			catch (Exception e)
			{
				_log.Error(e.Message);
			}
		}

		internal void SavePortState(List<object> port_info, List<object> option_info, Radio this_radio)
		{
			if (port_info != null && this_radio != null)
			{
				XmlSerializer serializer = new XmlSerializer(typeof(CatStatus));
				string this_path = (string)current_folder.FullName + "\\PortInfo.xml";
				TextWriter writer = new StreamWriter(this_path);
				CatStatus new_info = new CatStatus();
				new_info.radio_model = this_radio.Model;
				new_info.radio_sn = this_radio.Serial;
				new_info.ip_address = this_radio.IP.ToString();
				PortList[] port_array = new PortList[port_info.Count];
				int n = 0;
				foreach (PortInfo pi in port_info)
				{
					PortList this_port = new PortList();
					this_port.type = pi.PortType;
					this_port.lo_port = pi.FirstPort;
					this_port.lo_port_app = pi.FirstApp;
					this_port.hi_port = pi.SecondPort;
					this_port.hi_port_app = pi.SecondApp;
					port_array[n] = this_port;
					n++;
				}
				new_info.Ports = port_array;

				if (option_info != null)
				{
					OptionList[] option_array = new OptionList[option_info.Count];
					for (int x = 0; x < option_info.Count;x++ )
					{
						string[] info = option_info[x].ToString().Split('=');
						OptionList option_item = new OptionList();
						option_item.name = info[0];
						option_item.state = info[1];
						option_array[x] = option_item;
					}
					new_info.Options = option_array;
				}

				serializer.Serialize(writer, new_info);
				writer.Close();
			}
		}

		//internal CatStatus RetrievePortState()
		internal void RetrievePortState()
		{
			string this_path = (string)current_folder.FullName + "\\PortInfo.xml";
			//CatStatus this_state = new CatStatus();
			current_state = new CatStatus();
			if (File.Exists(this_path))
			{
				//XmlSerializer serializer = new XmlSerializer(this_state.GetType());
				XmlSerializer serializer = new XmlSerializer(current_state.GetType());
				StreamReader reader = new StreamReader(this_path);
				object deserialized = serializer.Deserialize(reader.BaseStream);
				//this_state = (CatStatus)deserialized;
				current_state = (CatStatus)deserialized;
				reader.Close();
			}
//			return this_state;
		}


		#endregion Methods
	}
}
