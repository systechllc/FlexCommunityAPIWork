namespace Sssdrcontest
{
    partial class Contest
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(Contest));
            this.panel5 = new System.Windows.Forms.Panel();
            this.Nummer = new System.Windows.Forms.Button();
            this.teller = new System.Windows.Forms.NumericUpDown();
            this.stopKnop = new System.Windows.Forms.Button();
            this.sendExch = new System.Windows.Forms.Button();
            this.tellerUp = new System.Windows.Forms.Button();
            this.panel4 = new System.Windows.Forms.Panel();
            this.C6 = new System.Windows.Forms.Button();
            this.C5 = new System.Windows.Forms.Button();
            this.C4 = new System.Windows.Forms.Button();
            this.C3 = new System.Windows.Forms.Button();
            this.C1 = new System.Windows.Forms.Button();
            this.C2 = new System.Windows.Forms.Button();
            this.panel3 = new System.Windows.Forms.Panel();
            this.F75 = new System.Windows.Forms.Button();
            this.F50 = new System.Windows.Forms.Button();
            this.F100 = new System.Windows.Forms.Button();
            this.panel2 = new System.Windows.Forms.Panel();
            this.Pw40 = new System.Windows.Forms.Button();
            this.Pw20 = new System.Windows.Forms.Button();
            this.Pw80 = new System.Windows.Forms.Button();
            this.panel1 = new System.Windows.Forms.Panel();
            this.B3 = new System.Windows.Forms.Button();
            this.B28 = new System.Windows.Forms.Button();
            this.B21 = new System.Windows.Forms.Button();
            this.B14 = new System.Windows.Forms.Button();
            this.B7 = new System.Windows.Forms.Button();
            this.B1 = new System.Windows.Forms.Button();
            this.WPMpanel = new System.Windows.Forms.Panel();
            this.WM20 = new System.Windows.Forms.Button();
            this.WM30 = new System.Windows.Forms.Button();
            this.WM27 = new System.Windows.Forms.Button();
            this.WM25 = new System.Windows.Forms.Button();
            this.WM23 = new System.Windows.Forms.Button();
            this.WM18 = new System.Windows.Forms.Button();
            this.panel5.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.teller)).BeginInit();
            this.panel4.SuspendLayout();
            this.panel3.SuspendLayout();
            this.panel2.SuspendLayout();
            this.panel1.SuspendLayout();
            this.WPMpanel.SuspendLayout();
            this.SuspendLayout();
            // 
            // panel5
            // 
            this.panel5.BackColor = System.Drawing.SystemColors.Info;
            this.panel5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel5.Controls.Add(this.Nummer);
            this.panel5.Controls.Add(this.teller);
            this.panel5.Controls.Add(this.stopKnop);
            this.panel5.Controls.Add(this.sendExch);
            this.panel5.Controls.Add(this.tellerUp);
            this.panel5.Location = new System.Drawing.Point(12, 250);
            this.panel5.Name = "panel5";
            this.panel5.Size = new System.Drawing.Size(531, 61);
            this.panel5.TabIndex = 90;
            // 
            // Nummer
            // 
            this.Nummer.Location = new System.Drawing.Point(344, 19);
            this.Nummer.Name = "Nummer";
            this.Nummer.Size = new System.Drawing.Size(75, 23);
            this.Nummer.TabIndex = 84;
            this.Nummer.Text = "NR";
            this.Nummer.UseVisualStyleBackColor = true;
            this.Nummer.Click += new System.EventHandler(this.sendNr);
            // 
            // teller
            // 
            this.teller.BackColor = System.Drawing.SystemColors.Info;
            this.teller.Location = new System.Drawing.Point(12, 19);
            this.teller.Maximum = new decimal(new int[] {
            9999,
            0,
            0,
            0});
            this.teller.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.teller.Name = "teller";
            this.teller.Size = new System.Drawing.Size(79, 20);
            this.teller.TabIndex = 81;
            this.teller.TabStop = false;
            this.teller.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
            this.teller.ValueChanged += new System.EventHandler(this.werkBij);
            this.teller.Click += new System.EventHandler(this.werkBij);
            // 
            // stopKnop
            // 
            this.stopKnop.Location = new System.Drawing.Point(445, 19);
            this.stopKnop.Name = "stopKnop";
            this.stopKnop.Size = new System.Drawing.Size(75, 23);
            this.stopKnop.TabIndex = 25;
            this.stopKnop.Text = "Stop";
            this.stopKnop.UseVisualStyleBackColor = true;
            this.stopKnop.Click += new System.EventHandler(this.stopCwx);
            // 
            // sendExch
            // 
            this.sendExch.Location = new System.Drawing.Point(243, 19);
            this.sendExch.Name = "sendExch";
            this.sendExch.Size = new System.Drawing.Size(75, 23);
            this.sendExch.TabIndex = 83;
            this.sendExch.Text = "Exh";
            this.sendExch.UseVisualStyleBackColor = true;
            this.sendExch.Click += new System.EventHandler(this.sendExchf);
            // 
            // tellerUp
            // 
            this.tellerUp.Location = new System.Drawing.Point(98, 19);
            this.tellerUp.Name = "tellerUp";
            this.tellerUp.Size = new System.Drawing.Size(75, 23);
            this.tellerUp.TabIndex = 82;
            this.tellerUp.Text = "Counter+";
            this.tellerUp.UseVisualStyleBackColor = true;
            this.tellerUp.Click += new System.EventHandler(this.hoogTeller);
            // 
            // panel4
            // 
            this.panel4.BackColor = System.Drawing.SystemColors.Info;
            this.panel4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel4.Controls.Add(this.C6);
            this.panel4.Controls.Add(this.C5);
            this.panel4.Controls.Add(this.C4);
            this.panel4.Controls.Add(this.C3);
            this.panel4.Controls.Add(this.C1);
            this.panel4.Controls.Add(this.C2);
            this.panel4.Location = new System.Drawing.Point(12, 190);
            this.panel4.Name = "panel4";
            this.panel4.Size = new System.Drawing.Size(531, 54);
            this.panel4.TabIndex = 89;
            // 
            // C6
            // 
            this.C6.Location = new System.Drawing.Point(444, 14);
            this.C6.Name = "C6";
            this.C6.Size = new System.Drawing.Size(75, 23);
            this.C6.TabIndex = 26;
            this.C6.Text = "C6";
            this.C6.UseVisualStyleBackColor = true;
            this.C6.Click += new System.EventHandler(this.c_click);
            // 
            // C5
            // 
            this.C5.Location = new System.Drawing.Point(363, 14);
            this.C5.Name = "C5";
            this.C5.Size = new System.Drawing.Size(75, 23);
            this.C5.TabIndex = 25;
            this.C5.Text = "C5";
            this.C5.UseVisualStyleBackColor = true;
            this.C5.Click += new System.EventHandler(this.c_click);
            // 
            // C4
            // 
            this.C4.Location = new System.Drawing.Point(282, 14);
            this.C4.Name = "C4";
            this.C4.Size = new System.Drawing.Size(75, 23);
            this.C4.TabIndex = 24;
            this.C4.Text = "C4";
            this.C4.UseVisualStyleBackColor = true;
            this.C4.Click += new System.EventHandler(this.c_click);
            // 
            // C3
            // 
            this.C3.Location = new System.Drawing.Point(178, 14);
            this.C3.Name = "C3";
            this.C3.Size = new System.Drawing.Size(75, 23);
            this.C3.TabIndex = 23;
            this.C3.Text = "C3";
            this.C3.UseVisualStyleBackColor = true;
            this.C3.Click += new System.EventHandler(this.c_click);
            // 
            // C1
            // 
            this.C1.Location = new System.Drawing.Point(16, 14);
            this.C1.Name = "C1";
            this.C1.Size = new System.Drawing.Size(75, 23);
            this.C1.TabIndex = 21;
            this.C1.Text = "C1";
            this.C1.UseVisualStyleBackColor = true;
            this.C1.Click += new System.EventHandler(this.c_click);
            // 
            // C2
            // 
            this.C2.Location = new System.Drawing.Point(97, 14);
            this.C2.Name = "C2";
            this.C2.Size = new System.Drawing.Size(75, 23);
            this.C2.TabIndex = 22;
            this.C2.Text = "C2";
            this.C2.UseVisualStyleBackColor = true;
            this.C2.Click += new System.EventHandler(this.c_click);
            // 
            // panel3
            // 
            this.panel3.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.panel3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel3.Controls.Add(this.F75);
            this.panel3.Controls.Add(this.F50);
            this.panel3.Controls.Add(this.F100);
            this.panel3.Location = new System.Drawing.Point(282, 12);
            this.panel3.Name = "panel3";
            this.panel3.Size = new System.Drawing.Size(261, 54);
            this.panel3.TabIndex = 88;
            // 
            // F75
            // 
            this.F75.Location = new System.Drawing.Point(96, 17);
            this.F75.Name = "F75";
            this.F75.Size = new System.Drawing.Size(75, 23);
            this.F75.TabIndex = 15;
            this.F75.Text = "75 Hz";
            this.F75.UseVisualStyleBackColor = true;
            this.F75.Click += new System.EventHandler(this.fklik);
            // 
            // F50
            // 
            this.F50.Location = new System.Drawing.Point(15, 17);
            this.F50.Name = "F50";
            this.F50.Size = new System.Drawing.Size(75, 23);
            this.F50.TabIndex = 14;
            this.F50.Text = "50 Hz";
            this.F50.UseVisualStyleBackColor = true;
            this.F50.Click += new System.EventHandler(this.fklik);
            // 
            // F100
            // 
            this.F100.Location = new System.Drawing.Point(177, 16);
            this.F100.Name = "F100";
            this.F100.Size = new System.Drawing.Size(75, 23);
            this.F100.TabIndex = 17;
            this.F100.Text = "100 Hz";
            this.F100.UseVisualStyleBackColor = true;
            this.F100.Click += new System.EventHandler(this.fklik);
            // 
            // panel2
            // 
            this.panel2.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.panel2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel2.Controls.Add(this.Pw40);
            this.panel2.Controls.Add(this.Pw20);
            this.panel2.Controls.Add(this.Pw80);
            this.panel2.Location = new System.Drawing.Point(12, 12);
            this.panel2.Name = "panel2";
            this.panel2.Size = new System.Drawing.Size(264, 54);
            this.panel2.TabIndex = 87;
            // 
            // Pw40
            // 
            this.Pw40.Location = new System.Drawing.Point(96, 17);
            this.Pw40.Name = "Pw40";
            this.Pw40.Size = new System.Drawing.Size(75, 23);
            this.Pw40.TabIndex = 15;
            this.Pw40.Text = "40 W";
            this.Pw40.UseVisualStyleBackColor = true;
            this.Pw40.Click += new System.EventHandler(this.pklik);
            // 
            // Pw20
            // 
            this.Pw20.Location = new System.Drawing.Point(15, 17);
            this.Pw20.Name = "Pw20";
            this.Pw20.Size = new System.Drawing.Size(75, 23);
            this.Pw20.TabIndex = 14;
            this.Pw20.Text = "20 W";
            this.Pw20.UseVisualStyleBackColor = true;
            this.Pw20.Click += new System.EventHandler(this.pklik);
            // 
            // Pw80
            // 
            this.Pw80.Location = new System.Drawing.Point(177, 16);
            this.Pw80.Name = "Pw80";
            this.Pw80.Size = new System.Drawing.Size(75, 23);
            this.Pw80.TabIndex = 17;
            this.Pw80.Text = "80 W";
            this.Pw80.UseVisualStyleBackColor = true;
            this.Pw80.Click += new System.EventHandler(this.pklik);
            // 
            // panel1
            // 
            this.panel1.BackColor = System.Drawing.SystemColors.GradientInactiveCaption;
            this.panel1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.panel1.Controls.Add(this.B3);
            this.panel1.Controls.Add(this.B28);
            this.panel1.Controls.Add(this.B21);
            this.panel1.Controls.Add(this.B14);
            this.panel1.Controls.Add(this.B7);
            this.panel1.Controls.Add(this.B1);
            this.panel1.Location = new System.Drawing.Point(12, 132);
            this.panel1.Name = "panel1";
            this.panel1.Size = new System.Drawing.Size(531, 52);
            this.panel1.TabIndex = 86;
            // 
            // B3
            // 
            this.B3.Location = new System.Drawing.Point(96, 16);
            this.B3.Name = "B3";
            this.B3.Size = new System.Drawing.Size(75, 23);
            this.B3.TabIndex = 2;
            this.B3.Text = "3,5 MHz";
            this.B3.UseVisualStyleBackColor = true;
            this.B3.Click += new System.EventHandler(this.bklik);
            // 
            // B28
            // 
            this.B28.Location = new System.Drawing.Point(444, 16);
            this.B28.Name = "B28";
            this.B28.Size = new System.Drawing.Size(75, 23);
            this.B28.TabIndex = 6;
            this.B28.Text = "28 MHz";
            this.B28.UseVisualStyleBackColor = true;
            this.B28.Click += new System.EventHandler(this.bklik);
            // 
            // B21
            // 
            this.B21.Location = new System.Drawing.Point(363, 16);
            this.B21.Name = "B21";
            this.B21.Size = new System.Drawing.Size(75, 23);
            this.B21.TabIndex = 5;
            this.B21.Text = "21 MHz";
            this.B21.UseVisualStyleBackColor = true;
            this.B21.Click += new System.EventHandler(this.bklik);
            // 
            // B14
            // 
            this.B14.Location = new System.Drawing.Point(282, 16);
            this.B14.Name = "B14";
            this.B14.Size = new System.Drawing.Size(75, 23);
            this.B14.TabIndex = 4;
            this.B14.Text = "14 MHz";
            this.B14.UseVisualStyleBackColor = true;
            this.B14.Click += new System.EventHandler(this.bklik);
            // 
            // B7
            // 
            this.B7.Location = new System.Drawing.Point(177, 16);
            this.B7.Name = "B7";
            this.B7.Size = new System.Drawing.Size(75, 23);
            this.B7.TabIndex = 3;
            this.B7.Text = "7 MHz";
            this.B7.UseVisualStyleBackColor = true;
            this.B7.Click += new System.EventHandler(this.bklik);
            // 
            // B1
            // 
            this.B1.Location = new System.Drawing.Point(15, 16);
            this.B1.Name = "B1";
            this.B1.Size = new System.Drawing.Size(75, 23);
            this.B1.TabIndex = 1;
            this.B1.Text = "1,8 MHz";
            this.B1.UseVisualStyleBackColor = true;
            this.B1.Click += new System.EventHandler(this.bklik);
            // 
            // WPMpanel
            // 
            this.WPMpanel.BackColor = System.Drawing.SystemColors.GradientActiveCaption;
            this.WPMpanel.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
            this.WPMpanel.Controls.Add(this.WM20);
            this.WPMpanel.Controls.Add(this.WM30);
            this.WPMpanel.Controls.Add(this.WM27);
            this.WPMpanel.Controls.Add(this.WM25);
            this.WPMpanel.Controls.Add(this.WM23);
            this.WPMpanel.Controls.Add(this.WM18);
            this.WPMpanel.Location = new System.Drawing.Point(12, 74);
            this.WPMpanel.Name = "WPMpanel";
            this.WPMpanel.Size = new System.Drawing.Size(531, 52);
            this.WPMpanel.TabIndex = 85;
            // 
            // WM20
            // 
            this.WM20.Location = new System.Drawing.Point(96, 16);
            this.WM20.Name = "WM20";
            this.WM20.Size = new System.Drawing.Size(75, 23);
            this.WM20.TabIndex = 2;
            this.WM20.Text = "20 WPM";
            this.WM20.UseVisualStyleBackColor = true;
            this.WM20.Click += new System.EventHandler(this.zetwpm);
            // 
            // WM30
            // 
            this.WM30.Location = new System.Drawing.Point(444, 16);
            this.WM30.Name = "WM30";
            this.WM30.Size = new System.Drawing.Size(75, 23);
            this.WM30.TabIndex = 6;
            this.WM30.Text = "30 WPM";
            this.WM30.UseVisualStyleBackColor = true;
            this.WM30.Click += new System.EventHandler(this.zetwpm);
            // 
            // WM27
            // 
            this.WM27.Location = new System.Drawing.Point(363, 16);
            this.WM27.Name = "WM27";
            this.WM27.Size = new System.Drawing.Size(75, 23);
            this.WM27.TabIndex = 5;
            this.WM27.Text = "27 WPM";
            this.WM27.UseVisualStyleBackColor = true;
            this.WM27.Click += new System.EventHandler(this.zetwpm);
            // 
            // WM25
            // 
            this.WM25.Location = new System.Drawing.Point(282, 16);
            this.WM25.Name = "WM25";
            this.WM25.Size = new System.Drawing.Size(75, 23);
            this.WM25.TabIndex = 4;
            this.WM25.Text = "25 WPM";
            this.WM25.UseVisualStyleBackColor = true;
            this.WM25.Click += new System.EventHandler(this.zetwpm);
            // 
            // WM23
            // 
            this.WM23.Location = new System.Drawing.Point(177, 16);
            this.WM23.Name = "WM23";
            this.WM23.Size = new System.Drawing.Size(75, 23);
            this.WM23.TabIndex = 3;
            this.WM23.Text = "23 WPM";
            this.WM23.UseVisualStyleBackColor = true;
            this.WM23.Click += new System.EventHandler(this.zetwpm);
            // 
            // WM18
            // 
            this.WM18.Location = new System.Drawing.Point(15, 16);
            this.WM18.Name = "WM18";
            this.WM18.Size = new System.Drawing.Size(75, 23);
            this.WM18.TabIndex = 1;
            this.WM18.Text = "18 WPM";
            this.WM18.UseVisualStyleBackColor = true;
            this.WM18.Click += new System.EventHandler(this.zetwpm);
            // 
            // Contest
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(560, 327);
            this.Controls.Add(this.panel5);
            this.Controls.Add(this.panel4);
            this.Controls.Add(this.panel3);
            this.Controls.Add(this.panel2);
            this.Controls.Add(this.panel1);
            this.Controls.Add(this.WPMpanel);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "Contest";
            this.Text = "CW Contestscreen ";
            this.panel5.ResumeLayout(false);
            ((System.ComponentModel.ISupportInitialize)(this.teller)).EndInit();
            this.panel4.ResumeLayout(false);
            this.panel3.ResumeLayout(false);
            this.panel2.ResumeLayout(false);
            this.panel1.ResumeLayout(false);
            this.WPMpanel.ResumeLayout(false);
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Panel panel5;
        private System.Windows.Forms.Button Nummer;
        private System.Windows.Forms.NumericUpDown teller;
        private System.Windows.Forms.Button stopKnop;
        private System.Windows.Forms.Button sendExch;
        private System.Windows.Forms.Button tellerUp;
        private System.Windows.Forms.Panel panel4;
        private System.Windows.Forms.Button C6;
        private System.Windows.Forms.Button C5;
        private System.Windows.Forms.Button C4;
        private System.Windows.Forms.Button C3;
        private System.Windows.Forms.Button C1;
        private System.Windows.Forms.Button C2;
        private System.Windows.Forms.Panel panel3;
        private System.Windows.Forms.Button F75;
        private System.Windows.Forms.Button F50;
        private System.Windows.Forms.Button F100;
        private System.Windows.Forms.Panel panel2;
        private System.Windows.Forms.Button Pw40;
        private System.Windows.Forms.Button Pw20;
        private System.Windows.Forms.Button Pw80;
        private System.Windows.Forms.Panel panel1;
        private System.Windows.Forms.Button B3;
        private System.Windows.Forms.Button B28;
        private System.Windows.Forms.Button B21;
        private System.Windows.Forms.Button B14;
        private System.Windows.Forms.Button B7;
        private System.Windows.Forms.Button B1;
        private System.Windows.Forms.Panel WPMpanel;
        private System.Windows.Forms.Button WM20;
        private System.Windows.Forms.Button WM30;
        private System.Windows.Forms.Button WM27;
        private System.Windows.Forms.Button WM25;
        private System.Windows.Forms.Button WM23;
        private System.Windows.Forms.Button WM18;
    }
}