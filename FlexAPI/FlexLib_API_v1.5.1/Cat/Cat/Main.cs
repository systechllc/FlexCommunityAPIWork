/*! \class Cat.Main
 * \brief       Entry point for the CAT program.
 * \author      Bob Tracy, K5KDN
 * \version     1.2.7.0
 * \date        07/08/2013
 * \copyright   FlexRadio Systems
 */

/*! \mainpage   notitle
 *  \section intro_section Introduction
 *  
 * CAT is the serial third party interface for SmartSDR.
 */

/**
 *    Date     Author   Ver               Description
 * ----------------------------------------------------------------------------
 * 2014-02-27  KE5DTO  1.2.5  Fixed crash on removing dedicated ports
 * 2014-03-07  KE5DTO  1.2.6  Fixed crash due to new SAM mode
 * 2014-04-01  KE5DTO  1.2.7  Fixed crash on a TCP reconnect, Added radio properties
 * 2014-04-22  KE5DTO  1.2.8  Added more Radio/Slice properties to clean up error log
 * 2015-03-29  K5KDN   1.4.2  Added changes for so2r
 * ----------------------------------------------------------------------------
 */

using System;
using System.Diagnostics;
using System.Data;
using System.Windows.Forms;
using System.Drawing;
using System.Collections.Generic;
using System.ServiceProcess;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Timers;
using System.Reflection;
using System.Xml.Linq;
using System.Linq;
using Spring.Core;
using Spring.Context;
using Spring.Context.Support;
using Flex.Smoothlake.FlexLib;
using Cat.CatMain;
using Cat.CatMain.Ports;
using AsyncSocketsV2;
using log4net;


namespace Cat
{
	public partial class Main : Form
	{

		#region Properties

		private string current_port = string.Empty;
		public string CurrentPort
		{
			get { return current_port; }
			set { current_port = value; }
		}


		#endregion Properties

		#region Variables

		private static readonly ILog _log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
		ObjectManager _om;
		Radio this_radio;
		//private static bool dlyExit = false;
		private static System.Timers.Timer dlyTimer = new System.Timers.Timer(1000);
		private server broadcast_server;
		private const int BROADCAST_PORT = 5002;
		private bool radio_selected = false;
		private List<object> current_ports;
		private List<object> current_options = new List<object>();
		private List<string> so2r_ports = new List<string>();

		#endregion Variables

		#region Constructor

		public Main()
		{
            // check to make sure the that Virtual Serial Port Driver is present and functional
            if (!VSPDriverIsPresent())
            {
                // without these drivers, this application is useless, exit immediately                
                Environment.Exit(0);
            }

            // Check to make sure the service is installed and running
            ServiceController sc = new ServiceController("Virtual Serial Port Kit service");
            bool service_running = false;
            int count = 0;

            while (!service_running)
            {
                // refresh the ServiceController object
                sc.Refresh();

                // check the Status to see whether the service is running
                switch (sc.Status)
                {
                    case ServiceControllerStatus.Running:
                        service_running = true;
                        break;
                }

                // wait only if the service is not running
                if (!service_running)
                {
                    // timeout condition
                    if (count++ == 60) break;
                    Thread.Sleep(1000);
                }
            }

            if (!service_running)
            {
                // without these drivers, this application is useless, exit immediately  
                MessageBox.Show("SmartSDR CAT requires the FlexRadio Systems FlexVSP service to be installed and running. Please submit a HelpDesk support ticket for assistance in resolving this error.",
                    "FlexRadio Systems SmartSDR CAT Error",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
                Environment.Exit(0);
            }


			InitializeComponent();
			//IApplicationContext ctx = ContextRegistry.GetContext();

			this.FormClosed += new FormClosedEventHandler(Main_FormClosed);
			broadcast_server = new server(BROADCAST_PORT);
			broadcast_server.RxDataAvailable += new server.ServerRxDataAvailable(broadcast_server_RxDataAvailable);

#if(!DEBUG)
			this.WindowState = FormWindowState.Minimized;
#endif

			_om = ObjectManager.Instance;            
			
			_om.SerialPortManager.EnumerateWindowsPorts();
			GetVersionInfo();
			this.lstAvailableRadios.DrawItem += new System.Windows.Forms.DrawItemEventHandler(this.lstAvailableRadios_DrawItem);
			grpPortType.Visible = false;
			CheckForNewInstall();
			so2r_ports.Add("so2r");
			so2r_ports.Add("otrsp");
			so2r_ports.Add("winkeyer");

            // this block needs to be at the end of the constructor to avoid race conditions which may cause crashes
            API.RadioAdded += new API.RadioAddedEventHandler(API_RadioAdded);
            API.RadioRemoved += new API.RadioRemovedEventHandler(API_RadioRemoved);
            API.ProgramName = "Cat";
            API.Init();
		}
        
		#endregion Constructor

		#region Radio Events

		/*! \var    API_RadioAdded
		*  \brief      Adds the PropertyChanged Event for the selected radio and updates the radio list.
		*/
        Radio _radio = null;
		void API_RadioAdded(Radio radio)
		{
            if (_radio != null)
            {
                Debug.WriteLine("CAT: Already connected to a radio");
                return;
            }

            _radio = radio;

			Console.WriteLine("API_RadioAdded Fired:  " + radio.ToString());
			radio.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(radio_PropertyChanged);
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate
				{
					UpdateRadioList();
				});
			}
			else
				UpdateRadioList();
		}

		/*! \var   radio_PropertyChanged
		*  \brief      Passes the name of the changed radio property to the State Manager.
		*/

		void radio_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
		{
			//Console.WriteLine("Radio property changed: " + e.PropertyName);
			_om.StateManager.RadioPropertyChanged(e.PropertyName);
		}

		/*! \var    API_RadioRemoved
		*  \brief      Updates the radio list when a radio is deselected.
		*/

		void API_RadioRemoved(Radio radio)
		{
            // is this our radio?  If not, we are done.
            if (radio != _radio) return;

			Console.WriteLine("Radio removed: " + radio.ToString());
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate
				{
					UpdateRadioList();

				});
			}
			else
				UpdateRadioList();
				
			_radio = null;
		}

		/*! \var    radio_SliceRemoved
		*  \brief      Passes the index of a removed slice to the State Manager.
		*/

		void radio_SliceRemoved(Slice slc)
		{
			_om.StateManager.SliceRemoved(slc);
		}

		/*! \var    radio_SliceRemoved
		*  \brief      Passes the index of an added slice to the State Manager.
		*/

		void radio_SliceAdded(Slice slc)
		{
			_om.StateManager.SliceAdded(slc);
		}

		#endregion Radio Events

		#region Methods

		private void CheckForNewInstall()
		{
			uint pair_count = _om.SerialPortManager.EnumerateVirtual();
			if (pair_count == 0)
				_om.DataManager.DeleteRadioXML();
		}

		/*! \var    GetVersionInfo
		*  \brief      Gets current CAT project version for display on title bar.
		*/

		private void GetVersionInfo()
		{
			Assembly assem = Assembly.GetExecutingAssembly();
			AssemblyName assem_name = assem.GetName();
			Version ver = assem_name.Version;
			this.Text += " Ver " + ver;
		}

		/*! \var    GetLicenseInfo
		*  \brief      Gets FabulaTech license info.  Mainly used during development
		 *  to verify correct license key installed.
		*/

		//TODO:  Move this to somewhere other than the title bar.
		private void GetLicenseInfo()
		{
			string[] license_info = _om.SerialPortManager.GetLicenseInfo();
			string owner = "User";
			if (license_info[1].Contains("Flex") == true)
				owner = "FlexRadio Systems";
			if (license_info[0] != string.Empty)
				this.Text += "    " + license_info[0] + " serial port license issued to " + owner;
		}

        private bool VSPDriverIsPresent()
        {
            string windir = Environment.ExpandEnvironmentVariables("%windir%");
            string driver_path = Path.Combine(windir, "System32", "ftvspkapi.dll");            

            if (System.IO.File.Exists(driver_path) == false)
            {
                MessageBox.Show("SmartSDR CAT requires the FlexRadio Systems FlexVSP driver to be installed.  Please reinstall SmartSDR to install this driver.",
                    "FlexRadio Systems SmartSDR CAT Warning",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Exclamation,
                    MessageBoxDefaultButton.Button1);
                return false;
            }
            else
            {
                // %windir%/System32/ftvspkapi.dll has been found, proceed with using SmartSDR CAT
                return true;
            }

        }

		/*! \var    UpdateRadioList
		*  \brief      Updates the displayed list of discovered radios.  If only one radio is discovered
		 *  CAT starts automatically, otherwise, it waits for the user to select the desired radio.
		*/

		//TODO: This needs a total rewrite, won't work in multi environment at all.
		internal void UpdateRadioList()
		{
			//int cnt = 0;
			//Give the API time to update.
            //dlyTimer.Elapsed += new ElapsedEventHandler(dlyTimer_Elapsed);
            //dlyTimer.Enabled = true;
            //dlyTimer.Start();
            //while (!dlyExit  && cnt < 5000)
            //{
            //    cnt++;
            //}

            // create a local list of the radios to operate on
            Radio[] local_radio_list = new Radio[API.RadioList.Count];
            try
            {
                // avoid enumeration exceptions
                API.RadioList.CopyTo(local_radio_list);
            }
            catch (Exception)
            {
                // catch any exceptions
            }

            foreach (Radio radio in local_radio_list)
			{
				string lstString = radio.Model.PadRight(20, ' ') + "|      "+
				radio.Serial.PadRight(22, ' ') + "|      " +
				radio.IP.ToString().PadRight(50, ' ');

				if (InvokeRequired)
				{
					Invoke((MethodInvoker)delegate
					{
						lstAvailableRadios.Items.Clear();
						lstAvailableRadios.Items.Add(lstString);
					});
				}
				else
				{
					lstAvailableRadios.Items.Clear();
					lstAvailableRadios.Items.Add(lstString);
				}
			}
			//TODO:  Add notification of no radio attached
            if (local_radio_list.Length == 0)
			{
				_log.Error("No radio attached");
				if (InvokeRequired)
				{
					Invoke((MethodInvoker)delegate
					{
						lstAvailableRadios.Items.Clear();
					});
				}
				else
				{
					lstAvailableRadios.Items.Clear();
				}
				return;
			}

			if (local_radio_list.Length == 1)
			{
				if (InvokeRequired)
				{
					Invoke((MethodInvoker)delegate
					{
						lstAvailableRadios.SelectedIndex = 0;
						lstAvailableRadios.Enabled = false;
					});
				}
				else
				{
					lstAvailableRadios.SelectedIndex = 0;
					lstAvailableRadios.Enabled = false;
				}
			}
			else if(!radio_selected)
			{
				this.Show();
				string message = "Select Radio To Use";
				string caption = "Select Available Radio";
				MessageBoxButtons buttons = MessageBoxButtons.OKCancel;
				DialogResult result;
				result = MessageBox.Show(message, caption, buttons);

				if (result == System.Windows.Forms.DialogResult.Cancel)
				{
					this.Hide();
				}
				else
				{
					if (InvokeRequired)
					{ 
						Invoke((MethodInvoker)delegate
						{
							lstAvailableRadios.Enabled = true;
						});
					}
					else lstAvailableRadios.Enabled = true;
				}

			}
		}

		/*! \var    Connect Radio
		*  \brief      Adds slice event handlers for the selected radio and gets a serial port number for it.
		 *  Also calls for update of the Flex virtual port and hardware/foreign serial port lists.
		*/

		internal void ConnectRadio(Radio radio)
		{
			if (radio != null)
			{
				radio.SliceAdded += new Radio.SliceAddedEventHandler(radio_SliceAdded);
				radio.SliceRemoved += new Radio.SliceRemovedEventHandler(radio_SliceRemoved);
				radio.Connect();
				_om.StateManager.SelectedSN = radio.Serial;
				_om.DataManager.CurrentRadio = radio;
				radio_selected = true;
				//
				//if (_om.SerialPortManager.EnumerateVirtual() == 0)
				//{
				//    CatStatus this_state = _om.DataManager.RetrievePortState(radio.Serial);
				//    if (this_state != null)
				//        _om.SerialPortManager.RestorePortState(this_state);
				//}
				//
				GetAssignedPort();
			}
			GetAvailableVirtualPorts();
			GetAvailablePhysicalPorts();
		}

		/*! \var    GetAvailableVirtualPorts
		*  \brief      Updates the Flex virtual port list display with data requested from 
		 *  the Serial Port Manager.
		*/

		internal void GetAvailableVirtualPorts()
		{
			string port_name = string.Empty;
			UInt32 pair_count = _om.SerialPortManager.EnumerateVirtual();
			if (pair_count > 0)
			{
				lstFlexPorts.Items.Clear();
				string[] ports;
				for (UInt32 i = 0; i < pair_count; ++i)
				{
					ports = _om.SerialPortManager.GetPair(i);
					port_name = "COM"+ports[0];
					if (!this.current_port.Contains(port_name))	//Don't add the current port
						UpdateVirtualPortList("add", port_name +
							_om.SerialPortManager.GetPortType(this_radio.Serial, port_name ));
				}	
			}
			UpdatePortMapping();
		}

		/*! \var    GetAvailablePhysicalPorts
		*  \brief      Updates the foreign virtual/hardware port list display with data requested from 
		*  the Serial Port Manager.
		*/

		internal void GetAvailablePhysicalPorts()
		{
			UInt32 port_count = _om.vspkControl.EnumPhysical();
			if (port_count > 0)
			{
				lstPhysical.Items.Clear();
				for (uint n = 0; n < port_count; n++)
				{
					UpdatePhysicalPortList("add", "COM" + _om.vspkControl.GetPhysical(n));
				}
			}
		}

		void GetAssignedPort()
		{
			string assigned_port = _om.SerialPortManager.GetPortForSerialNumber(this_radio);
			current_port = assigned_port;


			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate
				{
					if (!cboSerialPorts.Items.Contains(assigned_port))
						cboSerialPorts.Items.Add(assigned_port);
					cboSerialPorts.SelectedIndex = cboSerialPorts.Items.IndexOf(assigned_port);
				});
			}
			else
			{
				if (!cboSerialPorts.Items.Contains(assigned_port)) 
					cboSerialPorts.Items.Add(assigned_port);
				cboSerialPorts.SelectedIndex = cboSerialPorts.Items.IndexOf(assigned_port);
			}

		}

		void UpdateVirtualPortList(string action, string port)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate
				{
					if (action == "add")
						lstFlexPorts.Items.Add(port.ToString());
					else if (action == "del")
						lstFlexPorts.Items.Remove(port.ToString());
				});
			}
			else
			{
				if (action == "add")
					lstFlexPorts.Items.Add(port.ToString());
				else if (action == "delete")
					lstFlexPorts.Items.Remove(port.ToString());
			}
		}

		
		void UpdatePhysicalPortList(string action, string port)
		{
			if (InvokeRequired)
			{
				Invoke((MethodInvoker)delegate
				{
					if (action == "add")
			
						lstPhysical.Items.Add(port.ToString());
					else if (action == "del")
						lstPhysical.Items.Remove(port.ToString());

					if (lstPhysical.Items.Count > 0)
						lstPhysical.SelectedIndex = 0;
				});
			}
			else
			{
				if (action == "add")
					lstPhysical.Items.Add(port.ToString());
				else if (action == "delete")
					lstPhysical.Items.Remove(port.ToString());
			}

		}

		private void UpdatePortMapping()
		{
			lstConnections.Items.Clear();
			List<object> map = _om.SerialPortManager.GetPairInfo();
			foreach (PortInfo p in map)
			{
				string line = p.FirstPort + p.FirstApp + p.SecondPort + p.SecondApp;
				lstConnections.Items.Add(line);
			}
			current_ports = map;
			//_om.DataManager.SavePortState(map, this_radio);
		}

		private void UpdateOptionList(string option)
		{
			current_options.Add(option);
		}

		//From MS docs
		//public static void RestartService(string serviceName, int timeoutMilliseconds)
		//{
		//    ServiceController service = new ServiceController(serviceName);
		//    try
		//    {
		//        int millisec1 = Environment.TickCount;
		//        TimeSpan timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds);

		//        service.Stop();
		//        service.WaitForStatus(ServiceControllerStatus.Stopped, timeout);

		//        // count the rest of the timeout
		//        int millisec2 = Environment.TickCount;
		//        timeout = TimeSpan.FromMilliseconds(timeoutMilliseconds - (millisec2 - millisec1));

		//        service.Start();
		//        service.WaitForStatus(ServiceControllerStatus.Running, timeout);
		//    }
		//    catch(Exception e)
		//    {
		//        _log.Error(e.Message);
		//    }
		//}

		#endregion Methods

		#region Control Events

		private void notifyIcon1_MouseDoubleClick(object sender, MouseEventArgs e)
		{
			this.Show();
			this.WindowState = FormWindowState.Normal;
			this.ShowInTaskbar = true;
		}

		private void Main_Load(object sender, EventArgs e)
		{
			this.WindowState = FormWindowState.Minimized;
		}

		private void toolStripMenuItem1_Click(object sender, EventArgs e)
		{
			this.Show();
			this.WindowState = FormWindowState.Normal;
			this.ShowInTaskbar = true;
		}

		private void toolStripMenuItem2_Click(object sender, EventArgs e)
		{
			this.Close();
		}

		private void btnExecCmd_Click_1(object sender, EventArgs e)
		{
            if (cboCmd.Text == null || cboCmd.Text == "") return;

			if (cboCmd.Text.Length > 0)
			{
                object result = _om.CatCmdProcessor.ExecuteCommand(cboCmd.Text);
				if (result != null)
					txtResult.Text = result.ToString();
			}

		}

		private void tabTest_Enter(object sender, EventArgs e)
		{
			cboCmd.SelectedIndex = 0;
		}

        private void btnHide_Click(object sender, EventArgs e)
		{
			this.WindowState = FormWindowState.Minimized;
		}

		private void btnQuit_Click(object sender, EventArgs e)
		{
			string message = "This will close the CAT system and disconnect any active serial ports.  "+  
			"Continue to exit?";
			DialogResult result = MessageBox.Show(message, "Close the CAT application?", MessageBoxButtons.YesNo);

			if (result == DialogResult.Yes)
				this.Close();
		}

		void Main_FormClosed(object sender, FormClosedEventArgs e)
		{
			notifyIcon1.Visible = false;
		}

		private void lstFlexPorts_SelectedIndexChanged(object sender, EventArgs e)
		{
			btnChangePort.Enabled = true;
			btnDeletePort.Enabled = true;
		}

		private void cboSerialPorts_SelectedIndexChanged(object sender, EventArgs e)
		{
			bool[] args = new bool[3] { false, false, false };
			if (cboSerialPorts.Text != current_port)
			{
				string message = "Change the assigned port for this radio?";
				string caption = "Change Serial Port Assignment";
				MessageBoxButtons buttons = MessageBoxButtons.YesNo;
				DialogResult result;
				result = MessageBox.Show(message, caption, buttons);

				if (result == System.Windows.Forms.DialogResult.Yes)
				{
					_om.SerialPortManager.CreateCATPort(cboSerialPorts.Text, "master", args);
					_om.SerialPortManager.UpdatePortAssignment(this_radio.Serial, cboSerialPorts.Text);
					this.current_port = cboSerialPorts.Text;
				}
			}
			else
			{
				string just_port = cboSerialPorts.Text;
                if (just_port == null || just_port == "") return;

				if(just_port.Contains('-'))
					just_port = just_port.Remove(just_port.IndexOf('-'));
				int low_port_number = int.Parse(just_port.Substring(3));
				//int low_port_number = int.Parse(cboSerialPorts.Text.Substring(3));
				string high_port_number = "COM" + (low_port_number + 10).ToString();
				_om.SerialPortManager.CreateCATPort(high_port_number, "master", args);
			}
		}

		private void lstAvailableRadios_DrawItem(object sender, DrawItemEventArgs e)
		{
            if (lstAvailableRadios.SelectedIndex < 0) return;

			int index = e.Index;
			Graphics g = e.Graphics;
			foreach (int selectedIndex in this.lstAvailableRadios.SelectedIndices)
			{
				if (index == selectedIndex)
				{
					e.DrawBackground();
					g.FillRectangle(new SolidBrush(Color.Yellow), e.Bounds);
				}
			}

			Font font = lstAvailableRadios.Font;
			Color colour = lstAvailableRadios.ForeColor;
			string text = lstAvailableRadios.Items[index].ToString();
			g.DrawString(text, font, new SolidBrush(Color.Black), (float)e.Bounds.X, (float)e.Bounds.Y);
			e.DrawFocusRectangle();
		}

		private void lstAvailableRadios_SelectedIndexChanged(object sender, EventArgs e)
		{
            //Debug.WriteLine("lstAvailableRadios index changed");

            if (lstAvailableRadios.Items.Count <= 0) return;
            if (lstAvailableRadios.Text == null || lstAvailableRadios.Text == "") return;
			
            // split the Text data up (Model, Serial, IP)
			string[] data = lstAvailableRadios.Text.Split('|');
            if (data.Length < 2) return;
            
            // save off the serial number
			string sn = data[1].Trim();
                
            // make a local list of radios to avoid enumeration exceptions
            Radio[] local_radio_list = new Radio[API.RadioList.Count];
            try
            {
                // copy the radio list from FlexLib
                API.RadioList.CopyTo(local_radio_list);
            }
            catch (Exception)
            {
                // handle any odd exceptions
            }

            // now loop over the local list of radios to find the Radio object to connect with
            foreach (Radio radio in local_radio_list)
			{
                // does the serial number match?
                if (radio.Serial == sn)
                {
                    // yes -- connect to the radio
                    this_radio = radio;
                    ConnectRadio(radio);
                    return;
                }
			}

            _log.Error("Didn't find radio serial number" + sn);
		}

		private void Main_FormClosing(object sender, FormClosingEventArgs e)
		{
            // EW 2014-02-04: no longer used -- using the older long hand method of decoding the xml form instead of the serialized classes
			//_om.DataManager.SavePortState(current_ports, current_options, this_radio);
            //if (this_radio != null)
            //    this_radio.FullDuplexEnabled = false;
		}

		private void btnAddPort_Click(object sender, EventArgs e)
		{
			btnAddPort.Enabled = false;
			rbDedicated.Checked = false;
			rbPTT.Checked = false;
			rbShared.Checked = false;
			rbSO2R.Checked = false;
			rbOTRSP.Checked = false;
			rbWinKeyer.Checked = false;
			chkDTR.Checked = false;
			chkRTS.Checked = false;
			chkDTR.Visible = false;
			chkRTS.Visible = false;
			btnAccept.Enabled = false;
			lblPinChange.Visible = false;
			grpPortType.Visible = true;
		}

		private void btnDeletePort_Click(object sender, EventArgs e)
		{
			string port_to_remove = (string)lstFlexPorts.SelectedItem;
            if (port_to_remove == null || port_to_remove == "") return;

			port_to_remove = port_to_remove.Substring(0, port_to_remove.IndexOf('-')).Trim();

			uint port1;
			uint port2;
			if (port_to_remove != null)
			{
				port1 = uint.Parse(port_to_remove.Substring(3));
				port2 = port1 + 10;

                // ensure that we don't have either of these ports open


				_om.SerialPortManager.DeletePair(port1, port2);
				_om.SerialPortManager.DeleteSlavePortFromXML(this_radio.Serial, port_to_remove);
			}
			lstFlexPorts.Items.Clear();
			GetAvailableVirtualPorts();
			btnDeletePort.Enabled = false;
			btnChangePort.Enabled = false;
		}

		private void btnChangePort_Click(object sender, EventArgs e)
		{
			string old_port = this.current_port;
			string selected_text = (string)lstFlexPorts.SelectedItem;
            if (selected_text == null || selected_text == "") return;

			string port_to_assign = selected_text.Substring(0, selected_text.IndexOf("-"));
			_om.SerialPortManager.UpdatePortAssignment(this_radio.Serial, port_to_assign);
			cboSerialPorts.Items.Remove(current_port);
			this.current_port = port_to_assign;
			cboSerialPorts.Items.Add(port_to_assign);
			cboSerialPorts.SelectedIndex = cboSerialPorts.Items.IndexOf(port_to_assign);
			lstFlexPorts.Items.Clear();
			_om.SerialPortManager.DeleteSlavePortFromXML(this_radio.Serial, port_to_assign);
			bool[] args = { false, false, false };
			_om.SerialPortManager.AddSlavePort2XML(this_radio.Serial, old_port, "dedicated", args);
			GetAvailableVirtualPorts();
			btnChangePort.Enabled = false;
			btnDeletePort.Enabled = false;
		}


		private void btnDeleteAll_Click(object sender, EventArgs e)
		{
			string message = "Do you really want to delete all Flex virtual ports?";
			string caption = "Really?";
			MessageBoxButtons buttons = MessageBoxButtons.YesNo;
			DialogResult result;
			result = MessageBox.Show(message, caption, buttons);

			if (result == System.Windows.Forms.DialogResult.Yes)
			{

				_om.SerialPortManager.DeleteAllVirtualPairs();
				lstFlexPorts.Items.Clear();

				cboSerialPorts.Items.Clear();
				cboSerialPorts.Text = "";
				cboSerialPorts.Refresh();
			}
		}

		//modified 3/29/2015 for so2r bt
		private void btnAccept_Click(object sender, EventArgs e)
		{
			string port_type = string.Empty;
			bool dtr = chkDTR.Checked;
			bool rts = chkRTS.Checked;
			if (rbDedicated.Checked == true)
				port_type = "dedicated";
			else if (rbShared.Checked == true)
				port_type = "shared";
			else if (rbPTT.Checked == true)
				port_type = "ptt";
			else if (rbSO2R.Checked == true)
			//{
			//    AddSO2RPorts();
			//    return;
			//}
				port_type = "so2r";
			else if (rbWinKeyer.Checked == true)
				port_type = "winkeyer";
			else if (rbOTRSP.Checked == true)
				port_type = "otrsp";
			else
				port_type = "dedicated";


			BuildPort(port_type, dtr, rts);
			grpPortType.Visible = false;
			port_type = string.Empty;
			chkDTR.Checked = false;
			chkRTS.Checked = false;


			btnAddPort.Enabled = true;
		}

		private void AddSO2RPorts()
		{
			foreach (string s in so2r_ports)
			{
				BuildPort(s, false, false);
			}

		}

		private void BuildPort(string portType, bool dtr, bool rts)
		{			
			bool ptt = false;
			if (portType == "ptt")
				ptt = true;
			bool[] args = new bool[3] { ptt, dtr, rts };
			try
			{
				string new_port = _om.SerialPortManager.CreateNewVirtualPair();
				if (!new_port.Contains("FAILED"))
				{
					int high_port = int.Parse(new_port.Substring(3)) + 10;
					string cat_hi_port = "COM" + high_port.ToString();
					if (!portType.Contains("shared"))
						_om.SerialPortManager.CreateCATPort(cat_hi_port, portType, args);

					_om.SerialPortManager.AddSlavePort2XML(this_radio.Serial, new_port, portType, args);
					string port_display = new_port + _om.SerialPortManager.GetPortType(this_radio.Serial, new_port);
					UpdateVirtualPortList("add", port_display);
				}
				else
				{
					string message = "Unable to create a new pair, please restart CAT";
					string caption = "PORT CREATION FAILED";
					MessageBoxButtons buttons = MessageBoxButtons.OK;
					DialogResult result;
					result = MessageBox.Show(message, caption, buttons);
				}
			}
			catch (Exception ex)
			{
				_log.Error(ex.Message);
			}

		}

		private void rbPTT_CheckedChanged(object sender, EventArgs e)
		{
			btnAccept.Enabled = true;
			if (rbPTT.Checked)
			{
				lblPinChange.Visible = true;
				chkDTR.Visible = true;
				chkRTS.Visible = true;
			}
			else
			{
				lblPinChange.Visible = false;
				chkDTR.Visible = false;
				chkRTS.Visible = false;
			}
		}

		private void btnCancel_Click(object sender, EventArgs e)
		{
			grpPortType.Visible = false;
			btnAddPort.Enabled = true;
		}

		private void rbShared_CheckedChanged(object sender, EventArgs e)
		{
			btnAccept.Enabled = true;
		}

		private void rbDedicated_CheckedChanged(object sender, EventArgs e)
		{
			btnAccept.Enabled = true;
		}

		private void btnRefresh_Click(object sender, EventArgs e)
		{
			UpdatePortMapping();
		}

		//added 3/29/2015 for so2r bt
		private void rbSO2R_CheckedChanged(object sender, EventArgs e)
		{
			btnAccept.Enabled = true;
		}


		//If there is an associated application for the port, add it to the XML file
		private void lstConnections_ItemCheck(object sender, ItemCheckEventArgs e)
		{
			//if (lstConnections.Text.Contains(cboSerialPorts.Text))
			//    _om.SerialPortManager.UpdateMainAppAssignment(this_radio.Serial, cboSerialPorts.Text, lstConnections.Text);
			//else
			//    _om.SerialPortManager.UpdateSlaveAppAssignment(this_radio.Serial, lstConnections.SelectedItem.ToString());
		}

		private void chkTrackCmds_CheckedChanged(object sender, EventArgs e)
		{
			//_om.StateManager.ActiveTrack = chkTrackCmds.Checked;
			//UpdateOptionList("ActiveTrack="+chkTrackCmds.Checked.ToString());
		}

		private void chkCenterOnFreqChange_CheckedChanged(object sender, EventArgs e)
		{
			//_om.StateManager.CenterOnFreqChange = chkCenterOnFreqChange.Checked;
			//UpdateOptionList("CenterOnFreqChange="+chkCenterOnFreqChange.Checked.ToString());
		}


		#endregion Control Events

		#region Misc Events

		/*! \var    broadcast_server_RXDataAvailable
		*  \brief      Event handler for broadcast port data.  
		*/
		StringBuilder tcpBuffer = new StringBuilder();
		Regex regex = new Regex(".*?;");

		void broadcast_server_RxDataAvailable(string args, int id)
		{
			string ans = string.Empty;
            args = args.Replace("\r", "");
            args = args.Replace("\n", "");

			tcpBuffer.Append(args);
			for (Match m = regex.Match(tcpBuffer.ToString()); m.Success; m = m.NextMatch())
			{
				try
				{
					//Console.WriteLine("Buffer:  " + comBuffer.Length.ToString());
					//Console.WriteLine("request:  " + m.Value);
					ans = _om.TcpPortManager.HandleRequest(m.Value.TrimEnd(';'));
					//Console.WriteLine("answer:  " + ans);
				}
				catch (Exception ex)
				{
					_log.Error(ex.Message);
				}
				finally
				{
					tcpBuffer = tcpBuffer.Replace(m.Value, "", 0, m.Length);
				}
			}
			//string ans = _om.TcpPortManager.HandleRequest(args);
			broadcast_server.Send(ans, id);
		}

		/*! \var   dlyTimer_Elapsed
		*  \brief      Event handler for the delay timer.  
		*/

        //void dlyTimer_Elapsed(object sender, ElapsedEventArgs e)
        //{
        //    dlyTimer.Stop();
        //    //dlyExit = true;
        //}

		#endregion Misc Events

        private void cboCmd_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.KeyCode == Keys.Enter)
                btnExecCmd.PerformClick();
        }

        protected override void WndProc(ref Message m)
        {
            const int WM_QUERYENDSESSION = 0x0011;

            // Listen for operating system messages. 

            switch(m.Msg)
            {
                case WM_QUERYENDSESSION:
                    this.Close();
                    break;
            }

            base.WndProc(ref m);
        }

		private void rbWinKeyer_CheckedChanged(object sender, EventArgs e)
		{
			btnAccept.Enabled = true;
		}

		private void rbOTRSP_CheckedChanged(object sender, EventArgs e)
		{
			btnAccept.Enabled = true;
		}



	}
}
