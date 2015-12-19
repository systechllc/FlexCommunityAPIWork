namespace Sssdrcontest
{
    partial class MainForm
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
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainForm));
            this.contestButton = new System.Windows.Forms.Button();
            this.ragCW = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // contestButton
            // 
            this.contestButton.Location = new System.Drawing.Point(37, 43);
            this.contestButton.Name = "contestButton";
            this.contestButton.Size = new System.Drawing.Size(116, 23);
            this.contestButton.TabIndex = 0;
            this.contestButton.Text = "CW Contest Screen";
            this.contestButton.UseVisualStyleBackColor = true;
            this.contestButton.Click += new System.EventHandler(this.openContest);
            // 
            // ragCW
            // 
            this.ragCW.Location = new System.Drawing.Point(201, 43);
            this.ragCW.Name = "ragCW";
            this.ragCW.Size = new System.Drawing.Size(109, 23);
            this.ragCW.TabIndex = 1;
            this.ragCW.Text = "CW QSO Screen";
            this.ragCW.UseVisualStyleBackColor = true;
            this.ragCW.Click += new System.EventHandler(this.cwscreen);
            // 
            // MainForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(366, 118);
            this.Controls.Add(this.ragCW);
            this.Controls.Add(this.contestButton);
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.MaximizeBox = false;
            this.Name = "MainForm";
            this.Text = "Launch screens";
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.Button contestButton;
        private System.Windows.Forms.Button ragCW;
    }
}

