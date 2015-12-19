namespace Cat
{
    partial class Main
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.components = new System.ComponentModel.Container();
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Main));
            this.notifyIcon1 = new System.Windows.Forms.NotifyIcon(this.components);
            this.contextMenuStrip1 = new System.Windows.Forms.ContextMenuStrip(this.components);
            this.toolStripMenuItem1 = new System.Windows.Forms.ToolStripMenuItem();
            this.toolStripMenuItem2 = new System.Windows.Forms.ToolStripMenuItem();
            this.label5 = new System.Windows.Forms.Label();
            this.tabCtlSetup = new System.Windows.Forms.TabControl();
            this.tabMain = new System.Windows.Forms.TabPage();
            this.label9 = new System.Windows.Forms.Label();
            this.label8 = new System.Windows.Forms.Label();
            this.label7 = new System.Windows.Forms.Label();
            this.lstAvailableRadios = new System.Windows.Forms.ListBox();
            this.panel1 = new System.Windows.Forms.Panel();
            this.panel2 = new System.Windows.Forms.Panel();
            this.cboSerialPorts = new System.Windows.Forms.ComboBox();
            this.label6 = new System.Windows.Forms.Label();
            this.tabSerialPorts = new System.Windows.Forms.TabPage();
            this.btnDeleteAll = new System.Windows.Forms.Button();
            this.btnDeletePort = new System.Windows.Forms.Button();
            this.btnChangePort = new System.Windows.Forms.Button();
            this.label3 = new System.Windows.Forms.Label();
            this.lstFlexPorts = new System.Windows.Forms.ListBox();
            this.label4 = new System.Windows.Forms.Label();
            this.btnAddPort = new System.Windows.Forms.Button();
            this.lstPhysical = new System.Windows.Forms.ListBox();
            this.grpPortType = new System.Windows.Forms.GroupBox();
            this.rbOTRSP = new System.Windows.Forms.RadioButton();
            this.rbWinKeyer = new System.Windows.Forms.RadioButton();
            this.rbSO2R = new System.Windows.Forms.RadioButton();
            this.lblPinChange = new System.Windows.Forms.Label();
            this.btnCancel = new System.Windows.Forms.Button();
            this.chkDTR = new System.Windows.Forms.CheckBox();
            this.chkRTS = new System.Windows.Forms.CheckBox();
            this.rbShared = new System.Windows.Forms.RadioButton();
            this.btnAccept = new System.Windows.Forms.Button();
            this.rbDedicated = new System.Windows.Forms.RadioButton();
            this.rbPTT = new System.Windows.Forms.RadioButton();
            this.tabMapping = new System.Windows.Forms.TabPage();
            this.label13 = new System.Windows.Forms.Label();
            this.label12 = new System.Windows.Forms.Label();
            this.label11 = new System.Windows.Forms.Label();
            this.label10 = new System.Windows.Forms.Label();
            this.btnRefresh = new System.Windows.Forms.Button();
            this.lstConnections = new System.Windows.Forms.CheckedListBox();
            this.tabTest = new System.Windows.Forms.TabPage();
            this.label2 = new System.Windows.Forms.Label();
            this.label1 = new System.Windows.Forms.Label();
            this.btnExecCmd = new System.Windows.Forms.Button();
            this.txtResult = new System.Windows.Forms.TextBox();
            this.cboCmd = new System.Windows.Forms.ComboBox();
            this.btnQuit = new System.Windows.Forms.Button();
            this.btnHide = new System.Windows.Forms.Button();
            this.toolTip1 = new System.Windows.Forms.ToolTip(this.components);
            this.contextMenuStrip1.SuspendLayout();
            this.tabCtlSetup.SuspendLayout();
            this.tabMain.SuspendLayout();
            this.panel2.SuspendLayout();
            this.tabSerialPorts.SuspendLayout();
            this.grpPortType.SuspendLayout();
            this.tabMapping.SuspendLayout();
            this.tabTest.SuspendLayout();
            this.SuspendLayout();
            // 
            // notifyIcon1
            // 
            this.notifyIcon1.ContextMenuStrip = this.contextMenuStrip1;
            this.notifyIcon1.Icon = ((System.Drawing.Icon)(resources.GetObject("notifyIcon1.Icon")));
            this.notifyIcon1.Text = "CAT";
            this.notifyIcon1.Visible = true;
            this.notifyIcon1.MouseDoubleClick += new System.Windows.Forms.MouseEventHandler(this.notifyIcon1_MouseDoubleClick);
            // 
            // contextMenuStrip1
            // 
            this.contextMenuStrip1.Items.AddRange(new System.Windows.Forms.ToolStripItem[] {
            this.toolStripMenuItem1,
            this.toolStripMenuItem2});
            this.contextMenuStrip1.Name = "contextMenuStrip1";
            this.contextMenuStrip1.Size = new System.Drawing.Size(104, 48);
            // 
            // toolStripMenuItem1
            // 
            this.toolStripMenuItem1.Name = "toolStripMenuItem1";
            this.toolStripMenuItem1.Size = new System.Drawing.Size(103, 22);
            this.toolStripMenuItem1.Text = "Show";
            this.toolStripMenuItem1.Click += new System.EventHandler(this.toolStripMenuItem1_Click);
            // 
            // toolStripMenuItem2
            // 
            this.toolStripMenuItem2.Name = "toolStripMenuItem2";
            this.toolStripMenuItem2.Size = new System.Drawing.Size(103, 22);
            this.toolStripMenuItem2.Text = "Exit";
            this.toolStripMenuItem2.Click += new System.EventHandler(this.toolStripMenuItem2_Click);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(14, 23);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(86, 13);
            this.label5.TabIndex = 18;
            this.label5.Text = "Available Radios";
            // 
            // tabCtlSetup
            // 
            this.tabCtlSetup.Controls.Add(this.tabMain);
            this.tabCtlSetup.Controls.Add(this.tabSerialPorts);
            this.tabCtlSetup.Controls.Add(this.tabMapping);
            this.tabCtlSetup.Controls.Add(this.tabTest);
            this.tabCtlSetup.Location = new System.Drawing.Point(0, -1);
            this.tabCtlSetup.Name = "tabCtlSetup";
            this.tabCtlSetup.SelectedIndex = 0;
            this.tabCtlSetup.Size = new System.Drawing.Size(538, 359);
            this.tabCtlSetup.TabIndex = 20;
            // 
            // tabMain
            // 
            this.tabMain.BackColor = System.Drawing.Color.LightBlue;
            this.tabMain.BorderStyle = System.Windows.Forms.BorderStyle.Fixed3D;
            this.tabMain.Controls.Add(this.label9);
            this.tabMain.Controls.Add(this.label8);
            this.tabMain.Controls.Add(this.label7);
            this.tabMain.Controls.Add(this.lstAvailableRadios);
            this.tabMain.Controls.Add(this.label5);
            this.tabMain.Controls.Add(this.panel1);
            this.tabMain.Controls.Add(this.panel2);
            this.tabMain.Location = new System.Drawing.Point(4, 22);
            this.tabMain.Name = "tabMain";
            this.tabMain.Size = new System.Drawing.Size(530, 333);
            this.tabMain.TabIndex = 0;
            this.tabMain.Text = "Main";
            // 
            // label9
            // 
            this.label9.AutoSize = true;
            this.label9.Location = new System.Drawing.Point(258, 45);
            this.label9.Name = "label9";
            this.label9.Size = new System.Drawing.Size(58, 13);
            this.label9.TabIndex = 28;
            this.label9.Text = "IP Address";
            // 
            // label8
            // 
            this.label8.AutoSize = true;
            this.label8.Location = new System.Drawing.Point(130, 45);
            this.label8.Name = "label8";
            this.label8.Size = new System.Drawing.Size(73, 13);
            this.label8.TabIndex = 27;
            this.label8.Text = "Serial Number";
            // 
            // label7
            // 
            this.label7.AutoSize = true;
            this.label7.Location = new System.Drawing.Point(17, 45);
            this.label7.Name = "label7";
            this.label7.Size = new System.Drawing.Size(67, 13);
            this.label7.TabIndex = 26;
            this.label7.Text = "Radio Model";
            // 
            // lstAvailableRadios
            // 
            this.lstAvailableRadios.DrawMode = System.Windows.Forms.DrawMode.OwnerDrawFixed;
            this.lstAvailableRadios.FormattingEnabled = true;
            this.lstAvailableRadios.Location = new System.Drawing.Point(17, 64);
            this.lstAvailableRadios.Name = "lstAvailableRadios";
            this.lstAvailableRadios.Size = new System.Drawing.Size(343, 121);
            this.lstAvailableRadios.TabIndex = 25;
            this.toolTip1.SetToolTip(this.lstAvailableRadios, "Flex radios discovered on the network.");
            this.lstAvailableRadios.SelectedIndexChanged += new System.EventHandler(this.lstAvailableRadios_SelectedIndexChanged);
            // 
            // panel1
            // 
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Location = new System.Drawing.Point(6, 13);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(363, 190);
            this.panel1.TabIndex = 29;
            // 
            // panel2
            // 
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.cboSerialPorts);
            this.panel2.Controls.Add(this.label6);
            this.panel2.Location = new System.Drawing.Point(375, 13);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(146, 190);
            this.panel2.TabIndex = 30;
            // 
            // cboSerialPorts
            // 
            this.cboSerialPorts.FormattingEnabled = true;
            this.cboSerialPorts.Location = new System.Drawing.Point(14, 50);
            this.cboSerialPorts.Name = "cboSerialPorts";
            this.cboSerialPorts.Size = new System.Drawing.Size(121, 21);
            this.cboSerialPorts.TabIndex = 0;
            this.toolTip1.SetToolTip(this.cboSerialPorts, "Connect your third party program to this COM port.");
            this.cboSerialPorts.SelectedIndexChanged += new System.EventHandler(this.cboSerialPorts_SelectedIndexChanged);
            // 
            // label6
            // 
            this.label6.AutoSize = true;
            this.label6.Location = new System.Drawing.Point(16, 9);
            this.label6.Name = "label6";
            this.label6.Size = new System.Drawing.Size(92, 39);
            this.label6.TabIndex = 24;
            this.label6.Text = "Connect 3rd Party\r\nProgram To Port:\r\n\r\n";
            // 
            // tabSerialPorts
            // 
            this.tabSerialPorts.BackColor = System.Drawing.Color.LightGray;
            this.tabSerialPorts.Controls.Add(this.btnDeleteAll);
            this.tabSerialPorts.Controls.Add(this.btnDeletePort);
            this.tabSerialPorts.Controls.Add(this.btnChangePort);
            this.tabSerialPorts.Controls.Add(this.label3);
            this.tabSerialPorts.Controls.Add(this.lstFlexPorts);
            this.tabSerialPorts.Controls.Add(this.label4);
            this.tabSerialPorts.Controls.Add(this.btnAddPort);
            this.tabSerialPorts.Controls.Add(this.lstPhysical);
            this.tabSerialPorts.Controls.Add(this.grpPortType);
            this.tabSerialPorts.Location = new System.Drawing.Point(4, 22);
            this.tabSerialPorts.Name = "tabSerialPorts";
            this.tabSerialPorts.Size = new System.Drawing.Size(530, 333);
            this.tabSerialPorts.TabIndex = 2;
            this.tabSerialPorts.Text = "Serial Ports";
            // 
            // btnDeleteAll
            // 
            this.btnDeleteAll.Location = new System.Drawing.Point(384, 292);
            this.btnDeleteAll.Name = "btnDeleteAll";
            this.btnDeleteAll.Size = new System.Drawing.Size(120, 23);
            this.btnDeleteAll.TabIndex = 37;
            this.btnDeleteAll.Text = "Delete All Virtual Ports";
            this.toolTip1.SetToolTip(this.btnDeleteAll, "Deletes all FlexRadio virtual ports.");
            this.btnDeleteAll.UseVisualStyleBackColor = true;
            this.btnDeleteAll.Click += new System.EventHandler(this.btnDeleteAll_Click);
            // 
            // btnDeletePort
            // 
            this.btnDeletePort.Enabled = false;
            this.btnDeletePort.Location = new System.Drawing.Point(196, 78);
            this.btnDeletePort.Name = "btnDeletePort";
            this.btnDeletePort.Size = new System.Drawing.Size(120, 23);
            this.btnDeletePort.TabIndex = 31;
            this.btnDeletePort.Text = "Remove A Port";
            this.toolTip1.SetToolTip(this.btnDeletePort, "Remove the highlighted port\r\n");
            this.btnDeletePort.UseVisualStyleBackColor = true;
            this.btnDeletePort.Click += new System.EventHandler(this.btnDeletePort_Click);
            // 
            // btnChangePort
            // 
            this.btnChangePort.Enabled = false;
            this.btnChangePort.Location = new System.Drawing.Point(196, 49);
            this.btnChangePort.Name = "btnChangePort";
            this.btnChangePort.Size = new System.Drawing.Size(120, 23);
            this.btnChangePort.TabIndex = 32;
            this.btnChangePort.Text = "Change Assigned Port";
            this.toolTip1.SetToolTip(this.btnChangePort, "Change the primary port to the port highlighted");
            this.btnChangePort.UseVisualStyleBackColor = true;
            this.btnChangePort.Click += new System.EventHandler(this.btnChangePort_Click);
            // 
            // label3
            // 
            this.label3.Location = new System.Drawing.Point(381, 14);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(100, 32);
            this.label3.TabIndex = 34;
            this.label3.Text = "Available Flex Virtual Ports";
            // 
            // lstFlexPorts
            // 
            this.lstFlexPorts.FormattingEnabled = true;
            this.lstFlexPorts.Location = new System.Drawing.Point(384, 49);
            this.lstFlexPorts.Name = "lstFlexPorts";
            this.lstFlexPorts.Size = new System.Drawing.Size(120, 225);
            this.lstFlexPorts.TabIndex = 33;
            this.lstFlexPorts.SelectedIndexChanged += new System.EventHandler(this.lstFlexPorts_SelectedIndexChanged);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(16, 14);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(108, 26);
            this.label4.TabIndex = 16;
            this.label4.Text = "Physical Ports and/or\r\nForeign Virtual Ports\r\n";
            // 
            // btnAddPort
            // 
            this.btnAddPort.Location = new System.Drawing.Point(196, 107);
            this.btnAddPort.Name = "btnAddPort";
            this.btnAddPort.Size = new System.Drawing.Size(120, 23);
            this.btnAddPort.TabIndex = 22;
            this.btnAddPort.Text = "Add A Port";
            this.toolTip1.SetToolTip(this.btnAddPort, "Add a new virtual port.\r\n");
            this.btnAddPort.UseVisualStyleBackColor = true;
            this.btnAddPort.Click += new System.EventHandler(this.btnAddPort_Click);
            // 
            // lstPhysical
            // 
            this.lstPhysical.FormattingEnabled = true;
            this.lstPhysical.Location = new System.Drawing.Point(19, 49);
            this.lstPhysical.Name = "lstPhysical";
            this.lstPhysical.Size = new System.Drawing.Size(120, 225);
            this.lstPhysical.TabIndex = 15;
            // 
            // grpPortType
            // 
            this.grpPortType.Controls.Add(this.rbOTRSP);
            this.grpPortType.Controls.Add(this.rbWinKeyer);
            this.grpPortType.Controls.Add(this.rbSO2R);
            this.grpPortType.Controls.Add(this.lblPinChange);
            this.grpPortType.Controls.Add(this.btnCancel);
            this.grpPortType.Controls.Add(this.chkDTR);
            this.grpPortType.Controls.Add(this.chkRTS);
            this.grpPortType.Controls.Add(this.rbShared);
            this.grpPortType.Controls.Add(this.btnAccept);
            this.grpPortType.Controls.Add(this.rbDedicated);
            this.grpPortType.Controls.Add(this.rbPTT);
            this.grpPortType.Location = new System.Drawing.Point(166, 148);
            this.grpPortType.Name = "grpPortType";
            this.grpPortType.Size = new System.Drawing.Size(200, 167);
            this.grpPortType.TabIndex = 46;
            this.grpPortType.TabStop = false;
            this.grpPortType.Text = "Port Type";
            // 
            // rbOTRSP
            // 
            this.rbOTRSP.AutoSize = true;
            this.rbOTRSP.Location = new System.Drawing.Point(110, 29);
            this.rbOTRSP.Name = "rbOTRSP";
            this.rbOTRSP.Size = new System.Drawing.Size(62, 17);
            this.rbOTRSP.TabIndex = 50;
            this.rbOTRSP.TabStop = true;
            this.rbOTRSP.Text = "OTRSP";
            this.rbOTRSP.UseVisualStyleBackColor = true;
            this.rbOTRSP.CheckedChanged += new System.EventHandler(this.rbOTRSP_CheckedChanged);
            // 
            // rbWinKeyer
            // 
            this.rbWinKeyer.AutoSize = true;
            this.rbWinKeyer.Location = new System.Drawing.Point(110, 52);
            this.rbWinKeyer.Name = "rbWinKeyer";
            this.rbWinKeyer.Size = new System.Drawing.Size(71, 17);
            this.rbWinKeyer.TabIndex = 49;
            this.rbWinKeyer.TabStop = true;
            this.rbWinKeyer.Text = "WinKeyer";
            this.rbWinKeyer.UseVisualStyleBackColor = true;
            this.rbWinKeyer.CheckedChanged += new System.EventHandler(this.rbWinKeyer_CheckedChanged);
            this.rbWinKeyer.Visible = false;
            // 
            // rbSO2R
            // 
            this.rbSO2R.AutoSize = true;
            this.rbSO2R.Location = new System.Drawing.Point(7, 99);
            this.rbSO2R.Name = "rbSO2R";
            this.rbSO2R.Size = new System.Drawing.Size(54, 17);
            this.rbSO2R.TabIndex = 48;
            this.rbSO2R.TabStop = true;
            this.rbSO2R.Text = "SO2R";
            this.rbSO2R.UseVisualStyleBackColor = true;
            this.rbSO2R.CheckedChanged += new System.EventHandler(this.rbSO2R_CheckedChanged);
            // 
            // lblPinChange
            // 
            this.lblPinChange.AutoSize = true;
            this.lblPinChange.Location = new System.Drawing.Point(123, 52);
            this.lblPinChange.Name = "lblPinChange";
            this.lblPinChange.Size = new System.Drawing.Size(62, 13);
            this.lblPinChange.TabIndex = 47;
            this.lblPinChange.Text = "Pin Change";
            // 
            // btnCancel
            // 
            this.btnCancel.Location = new System.Drawing.Point(110, 127);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 23);
            this.btnCancel.TabIndex = 46;
            this.btnCancel.Text = "Cancel";
            this.toolTip1.SetToolTip(this.btnCancel, "Cancels adding a port.");
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // chkDTR
            // 
            this.chkDTR.AutoSize = true;
            this.chkDTR.Location = new System.Drawing.Point(121, 76);
            this.chkDTR.Name = "chkDTR";
            this.chkDTR.Size = new System.Drawing.Size(49, 17);
            this.chkDTR.TabIndex = 44;
            this.chkDTR.Text = "DTR";
            this.toolTip1.SetToolTip(this.chkDTR, "Key transmitter on DTR");
            this.chkDTR.UseVisualStyleBackColor = true;
            // 
            // chkRTS
            // 
            this.chkRTS.AutoSize = true;
            this.chkRTS.Location = new System.Drawing.Point(67, 76);
            this.chkRTS.Name = "chkRTS";
            this.chkRTS.Size = new System.Drawing.Size(48, 17);
            this.chkRTS.TabIndex = 43;
            this.chkRTS.Text = "RTS";
            this.toolTip1.SetToolTip(this.chkRTS, "Key transmitter on RTS");
            this.chkRTS.UseVisualStyleBackColor = true;
            // 
            // rbShared
            // 
            this.rbShared.AutoSize = true;
            this.rbShared.Location = new System.Drawing.Point(6, 29);
            this.rbShared.Name = "rbShared";
            this.rbShared.Size = new System.Drawing.Size(59, 17);
            this.rbShared.TabIndex = 39;
            this.rbShared.TabStop = true;
            this.rbShared.Text = "Shared";
            this.toolTip1.SetToolTip(this.rbShared, "Open virtual pair, not connected to CAT.");
            this.rbShared.UseVisualStyleBackColor = true;
            this.rbShared.CheckedChanged += new System.EventHandler(this.rbShared_CheckedChanged);
            // 
            // btnAccept
            // 
            this.btnAccept.Location = new System.Drawing.Point(17, 127);
            this.btnAccept.Name = "btnAccept";
            this.btnAccept.Size = new System.Drawing.Size(75, 23);
            this.btnAccept.TabIndex = 45;
            this.btnAccept.Text = "Accept";
            this.toolTip1.SetToolTip(this.btnAccept, "Save changes");
            this.btnAccept.UseVisualStyleBackColor = true;
            this.btnAccept.Click += new System.EventHandler(this.btnAccept_Click);
            // 
            // rbDedicated
            // 
            this.rbDedicated.AutoSize = true;
            this.rbDedicated.Location = new System.Drawing.Point(6, 52);
            this.rbDedicated.Name = "rbDedicated";
            this.rbDedicated.Size = new System.Drawing.Size(74, 17);
            this.rbDedicated.TabIndex = 40;
            this.rbDedicated.Text = "Dedicated";
            this.toolTip1.SetToolTip(this.rbDedicated, "Always connected to CAT.");
            this.rbDedicated.UseVisualStyleBackColor = true;
            this.rbDedicated.CheckedChanged += new System.EventHandler(this.rbDedicated_CheckedChanged);
            // 
            // rbPTT
            // 
            this.rbPTT.AutoSize = true;
            this.rbPTT.Location = new System.Drawing.Point(6, 75);
            this.rbPTT.Name = "rbPTT";
            this.rbPTT.Size = new System.Drawing.Size(46, 17);
            this.rbPTT.TabIndex = 41;
            this.rbPTT.Text = "PTT";
            this.toolTip1.SetToolTip(this.rbPTT, "Serial PTT.");
            this.rbPTT.UseVisualStyleBackColor = true;
            this.rbPTT.CheckedChanged += new System.EventHandler(this.rbPTT_CheckedChanged);
            // 
            // tabMapping
            // 
            this.tabMapping.BackColor = System.Drawing.Color.PaleGreen;
            this.tabMapping.Controls.Add(this.label13);
            this.tabMapping.Controls.Add(this.label12);
            this.tabMapping.Controls.Add(this.label11);
            this.tabMapping.Controls.Add(this.label10);
            this.tabMapping.Controls.Add(this.btnRefresh);
            this.tabMapping.Controls.Add(this.lstConnections);
            this.tabMapping.Location = new System.Drawing.Point(4, 22);
            this.tabMapping.Name = "tabMapping";
            this.tabMapping.Size = new System.Drawing.Size(530, 333);
            this.tabMapping.TabIndex = 4;
            this.tabMapping.Text = "Port Map";
            // 
            // label13
            // 
            this.label13.AutoSize = true;
            this.label13.Location = new System.Drawing.Point(353, 20);
            this.label13.Name = "label13";
            this.label13.Size = new System.Drawing.Size(78, 13);
            this.label13.TabIndex = 5;
            this.label13.Text = "Connected To:";
            // 
            // label12
            // 
            this.label12.AutoSize = true;
            this.label12.Location = new System.Drawing.Point(275, 20);
            this.label12.Name = "label12";
            this.label12.Size = new System.Drawing.Size(36, 13);
            this.label12.TabIndex = 4;
            this.label12.Text = "Port #";
            // 
            // label11
            // 
            this.label11.AutoSize = true;
            this.label11.Location = new System.Drawing.Point(134, 20);
            this.label11.Name = "label11";
            this.label11.Size = new System.Drawing.Size(78, 13);
            this.label11.TabIndex = 3;
            this.label11.Text = "Connected To:";
            // 
            // label10
            // 
            this.label10.AutoSize = true;
            this.label10.Location = new System.Drawing.Point(59, 20);
            this.label10.Name = "label10";
            this.label10.Size = new System.Drawing.Size(36, 13);
            this.label10.TabIndex = 2;
            this.label10.Text = "Port #";
            // 
            // btnRefresh
            // 
            this.btnRefresh.Location = new System.Drawing.Point(220, 294);
            this.btnRefresh.Name = "btnRefresh";
            this.btnRefresh.Size = new System.Drawing.Size(75, 23);
            this.btnRefresh.TabIndex = 1;
            this.btnRefresh.Text = "Refresh";
            this.toolTip1.SetToolTip(this.btnRefresh, "Refresh the connection map");
            this.btnRefresh.UseVisualStyleBackColor = true;
            this.btnRefresh.Click += new System.EventHandler(this.btnRefresh_Click);
            // 
            // lstConnections
            // 
            this.lstConnections.Font = new System.Drawing.Font("Courier New", 8.25F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.lstConnections.FormattingEnabled = true;
            this.lstConnections.Location = new System.Drawing.Point(40, 39);
            this.lstConnections.Name = "lstConnections";
            this.lstConnections.Size = new System.Drawing.Size(450, 244);
            this.lstConnections.TabIndex = 0;
            this.toolTip1.SetToolTip(this.lstConnections, "Checkboxes reserved for future enhancement");
            this.lstConnections.ItemCheck += new System.Windows.Forms.ItemCheckEventHandler(this.lstConnections_ItemCheck);
            // 
            // tabTest
            // 
            this.tabTest.BackColor = System.Drawing.Color.LightGray;
            this.tabTest.Controls.Add(this.label2);
            this.tabTest.Controls.Add(this.label1);
            this.tabTest.Controls.Add(this.btnExecCmd);
            this.tabTest.Controls.Add(this.txtResult);
            this.tabTest.Controls.Add(this.cboCmd);
            this.tabTest.Location = new System.Drawing.Point(4, 22);
            this.tabTest.Name = "tabTest";
            this.tabTest.Size = new System.Drawing.Size(530, 333);
            this.tabTest.TabIndex = 3;
            this.tabTest.Text = "Test";
            this.tabTest.Enter += new System.EventHandler(this.tabTest_Enter);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Location = new System.Drawing.Point(34, 14);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(87, 13);
            this.label2.TabIndex = 4;
            this.label2.Text = "Select Command";
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Location = new System.Drawing.Point(349, 15);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(37, 13);
            this.label1.TabIndex = 3;
            this.label1.Text = "Result";
            // 
            // btnExecCmd
            // 
            this.btnExecCmd.Location = new System.Drawing.Point(157, 31);
            this.btnExecCmd.Name = "btnExecCmd";
            this.btnExecCmd.Size = new System.Drawing.Size(75, 23);
            this.btnExecCmd.TabIndex = 2;
            this.btnExecCmd.Text = "Execute";
            this.btnExecCmd.UseVisualStyleBackColor = true;
            this.btnExecCmd.Click += new System.EventHandler(this.btnExecCmd_Click_1);
            // 
            // txtResult
            // 
            this.txtResult.Location = new System.Drawing.Point(250, 31);
            this.txtResult.Name = "txtResult";
            this.txtResult.Size = new System.Drawing.Size(263, 20);
            this.txtResult.TabIndex = 1;
            // 
            // cboCmd
            // 
            this.cboCmd.FormattingEnabled = true;
            this.cboCmd.Items.AddRange(new object[] {
            "FA;",
            "FB;",
            "FR;",
            "FT;",
            "IF;",
            "KS;",
            "MD;",
            "SH;",
            "SL;",
            "ZZFA;",
            "ZZFB;",
            "ZZFI;",
            "ZZFJ;",
            "ZZIF;",
            "ZZMD;",
            "ZZME;",
            "ZZSW;"});
            this.cboCmd.Location = new System.Drawing.Point(19, 30);
            this.cboCmd.Name = "cboCmd";
            this.cboCmd.Size = new System.Drawing.Size(121, 21);
            this.cboCmd.TabIndex = 0;
            this.cboCmd.KeyDown += new System.Windows.Forms.KeyEventHandler(this.cboCmd_KeyDown);
            // 
            // btnQuit
            // 
            this.btnQuit.Location = new System.Drawing.Point(23, 386);
            this.btnQuit.Name = "btnQuit";
            this.btnQuit.Size = new System.Drawing.Size(75, 23);
            this.btnQuit.TabIndex = 21;
            this.btnQuit.Text = "QuitCAT";
            this.toolTip1.SetToolTip(this.btnQuit, "Shut down CAT program.");
            this.btnQuit.UseVisualStyleBackColor = true;
            this.btnQuit.Click += new System.EventHandler(this.btnQuit_Click);
            // 
            // btnHide
            // 
            this.btnHide.Location = new System.Drawing.Point(442, 386);
            this.btnHide.Name = "btnHide";
            this.btnHide.Size = new System.Drawing.Size(75, 23);
            this.btnHide.TabIndex = 20;
            this.btnHide.Text = "Hide";
            this.toolTip1.SetToolTip(this.btnHide, "Return CAT form to task bar.");
            this.btnHide.UseVisualStyleBackColor = true;
            this.btnHide.Click += new System.EventHandler(this.btnHide_Click);
            // 
            // Main
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(539, 421);
            this.Controls.Add(this.btnQuit);
            this.Controls.Add(this.btnHide);
            this.Controls.Add(this.tabCtlSetup);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "Main";
            this.Text = "SmartSDR CAT";
            this.FormClosing += new System.Windows.Forms.FormClosingEventHandler(this.Main_FormClosing);
            this.Load += new System.EventHandler(this.Main_Load);
            this.contextMenuStrip1.ResumeLayout(false);
            this.tabCtlSetup.ResumeLayout(false);
            this.tabMain.ResumeLayout(false);
            this.tabMain.PerformLayout();
            this.panel2.ResumeLayout(false);
            this.panel2.PerformLayout();
            this.tabSerialPorts.ResumeLayout(false);
            this.tabSerialPorts.PerformLayout();
            this.grpPortType.ResumeLayout(false);
            this.grpPortType.PerformLayout();
            this.tabMapping.ResumeLayout(false);
            this.tabMapping.PerformLayout();
            this.tabTest.ResumeLayout(false);
            this.tabTest.PerformLayout();
            this.ResumeLayout(false);

        }

        #endregion

		private System.Windows.Forms.NotifyIcon notifyIcon1;
        private System.Windows.Forms.ContextMenuStrip contextMenuStrip1;
        private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem1;
		private System.Windows.Forms.ToolStripMenuItem toolStripMenuItem2;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.TabControl tabCtlSetup;
		private System.Windows.Forms.TabPage tabMain;
		private System.Windows.Forms.TabPage tabSerialPorts;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.ListBox lstPhysical;
		private System.Windows.Forms.Button btnHide;
		private System.Windows.Forms.Button btnQuit;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.ListBox lstAvailableRadios;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.Panel panel1;
		private System.Windows.Forms.Panel panel2;
		private System.Windows.Forms.ComboBox cboSerialPorts;
		private System.Windows.Forms.Button btnAddPort;
		private System.Windows.Forms.ToolTip toolTip1;
		private System.Windows.Forms.Button btnDeletePort;
		private System.Windows.Forms.Button btnChangePort;
		private System.Windows.Forms.ListBox lstFlexPorts;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Button btnDeleteAll;
		private System.Windows.Forms.CheckBox chkRTS;
		private System.Windows.Forms.Button btnAccept;
		private System.Windows.Forms.CheckBox chkDTR;
		private System.Windows.Forms.RadioButton rbDedicated;
		private System.Windows.Forms.RadioButton rbPTT;
		private System.Windows.Forms.RadioButton rbShared;
		private System.Windows.Forms.GroupBox grpPortType;
		private System.Windows.Forms.Label lblPinChange;
		private System.Windows.Forms.Button btnCancel;
		private System.Windows.Forms.TabPage tabTest;
		private System.Windows.Forms.Button btnExecCmd;
		private System.Windows.Forms.TextBox txtResult;
		private System.Windows.Forms.ComboBox cboCmd;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.TabPage tabMapping;
		private System.Windows.Forms.CheckedListBox lstConnections;
		private System.Windows.Forms.Button btnRefresh;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.RadioButton rbSO2R;
		private System.Windows.Forms.RadioButton rbWinKeyer;
		private System.Windows.Forms.RadioButton rbOTRSP;
    }
}

