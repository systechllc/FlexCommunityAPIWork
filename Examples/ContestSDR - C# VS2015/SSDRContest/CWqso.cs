using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.Text.RegularExpressions;

using System.IO;
using System.Windows.Forms;
using Flex.Smoothlake.FlexLib;

namespace Sssdrcontest
{
    public partial class Cqso : Form
    {
        Radio rd;
        CWX cwxx;
        bool cwx_buf;

        ToolTip toolCW1 = new ToolTip();
        ToolTip toolCW2 = new ToolTip();
        ToolTip toolCW3 = new ToolTip();
        ToolTip toolCW4 = new ToolTip();
        ToolTip toolCW5 = new ToolTip();
        ToolTip toolCW6 = new ToolTip();
        ToolTip toolCW7 = new ToolTip();
        ToolTip toolCW8 = new ToolTip();
        ToolTip toolCW9 = new ToolTip();
        ToolTip toolCW10 = new ToolTip();
        ToolTip toolCW11 = new ToolTip();
        ToolTip toolCW12 = new ToolTip();

        ToolTip toolM1 = new ToolTip();
        ToolTip toolM2 = new ToolTip();
        ToolTip toolM3 = new ToolTip();
        ToolTip toolM4 = new ToolTip();
        ToolTip toolM5 = new ToolTip();
        ToolTip toolM6 = new ToolTip();
        ToolTip toolM7 = new ToolTip();
        ToolTip toolM8 = new ToolTip();
        ToolTip toolM9 = new ToolTip();
        ToolTip toolM10 = new ToolTip();
        ToolTip toolM11 = new ToolTip();
        ToolTip toolM12 = new ToolTip();

    //    ToolTip toolBT = new ToolTip();
        ToolTip toolAR = new ToolTip();
        ToolTip toolKN = new ToolTip();
      //  ToolTip toolBK = new ToolTip();
     //   ToolTip toolSK = new ToolTip();
        ToolTip toolReset = new ToolTip();

        public Cqso(Radio rd1, CWX cwx1)
        {
            InitializeComponent();
            rd = rd1;
            cwxx = cwx1;
            m_lader();
            rd.PropertyChanged += new PropertyChangedEventHandler(this.radioveranderd);
            cwxx.PropertyChanged += new PropertyChangedEventHandler(this.cwxveranderd);
            cwxx.CharSent += new CWX.CharSentEventHandler(cwxverzend);

            CenterGroupBoxTitle(cwFilbox);
            CenterGroupBoxTitle(cwxSpeedbox);
            CenterGroupBoxTitle(powerGroupbox);
            CenterGroupBoxTitle(rstGroupbox);
            Swaarde.Text = "599";
            Output.Text = rd.RFPower.ToString(CultureInfo.CurrentCulture);
            laad_tooltip_cw();

            toolM1.SetToolTip(M1, tooltip_lader("M1"));
            toolM2.SetToolTip(M2, tooltip_lader("M2"));
            toolM3.SetToolTip(M3, tooltip_lader("M3"));
            toolM4.SetToolTip(M4, tooltip_lader("M4"));
            toolM5.SetToolTip(M5, tooltip_lader("M5"));
            toolM6.SetToolTip(M6, tooltip_lader("M6"));
            toolM7.SetToolTip(M7, tooltip_lader("M7"));
            toolM8.SetToolTip(M8, tooltip_lader("M8"));
            toolM9.SetToolTip(M9, tooltip_lader("M9"));
            toolM10.SetToolTip(M10, tooltip_lader("M10"));
            toolM11.SetToolTip(M11, tooltip_lader("M11"));
            toolM12.SetToolTip(M12, tooltip_lader("M12"));

       //     toolAR.SetToolTip(BT, "=");
            toolAR.SetToolTip(AR, "+");
            toolKN.SetToolTip(KN, "(");
          //  toolAR.SetToolTip(BK, "&");
        //    toolAR.SetToolTip(SK, "$");
            toolReset.SetToolTip(ResetCWX, "Restart CWX when 'hanging': set flag to send"); 
            m_lader();
            //  MessageBox.Show("Nep");
        }

        private void laad_tooltip_cw()
        {
            toolCW1.SetToolTip(XF1, cwxx.Macros.GetValue(0).ToString());
            toolCW2.SetToolTip(XF2, cwxx.Macros.GetValue(1).ToString());
            toolCW3.SetToolTip(XF3, cwxx.Macros.GetValue(2).ToString());
            toolCW4.SetToolTip(XF4, cwxx.Macros.GetValue(3).ToString());
            toolCW5.SetToolTip(XF5, cwxx.Macros.GetValue(4).ToString());
            toolCW6.SetToolTip(XF6, cwxx.Macros.GetValue(5).ToString());
            toolCW7.SetToolTip(XF7, cwxx.Macros.GetValue(6).ToString());
            toolCW8.SetToolTip(XF8, cwxx.Macros.GetValue(7).ToString());
            toolCW9.SetToolTip(XF9, cwxx.Macros.GetValue(8).ToString());
            toolCW10.SetToolTip(XF10, cwxx.Macros.GetValue(9).ToString());
            toolCW11.SetToolTip(XF11, cwxx.Macros.GetValue(10).ToString());
            toolCW12.SetToolTip(XF12, cwxx.Macros.GetValue(11).ToString());
        }

        private static void CenterGroupBoxTitle(GroupBox groupbox)
        {
            Label label = new Label();
            label.Text = groupbox.Text;
            label.TextAlign = ContentAlignment.MiddleCenter;
            groupbox.Text = "";
            label.Left = groupbox.Left + (groupbox.Width - label.Width) / 2;
            label.Top = groupbox.Top; // is an example : adjust the constant
            label.Height = label.Font.Height + 2;
            label.Parent = groupbox.Parent;
            label.BringToFront();
            label.BackColor = groupbox.BackColor;
        }

        void m_lader()
        {
            //string[] macro = { "M1", "M2", "M3" };
            string[] lijn = new string[255];
            bool vind = false;
            vind = mx_lader("m1.txt", lijn);
            if (vind)
                M1.Text = lijn[0];
            Array.Clear(lijn, 0, lijn.Length);
            vind = mx_lader("m2.txt", lijn); ;
            if (vind)
                M2.Text = lijn[0];
            vind = mx_lader("m3.txt", lijn); ;
            if (vind)
                M3.Text = lijn[0];
            vind = mx_lader("m4.txt", lijn);
            if (vind)
                M4.Text = lijn[0];
            vind = mx_lader("m5.txt", lijn);
            if (vind)
                M5.Text = lijn[0];
            vind = mx_lader("m6.txt", lijn);
            if (vind)
                M6.Text = lijn[0];
            vind = mx_lader("m7.txt", lijn);
            if (vind)
                M7.Text = lijn[0];
            vind = mx_lader("m8.txt", lijn);
            if (vind)
                M8.Text = lijn[0];
            vind = mx_lader("m9.txt", lijn);
            if (vind)
                M9.Text = lijn[0];
            vind = mx_lader("m10.txt", lijn);
            if (vind)
                M10.Text = lijn[0];
            vind = mx_lader("m11.txt", lijn);
            if (vind)
                M11.Text = lijn[0];
            vind = mx_lader("m12.txt", lijn);
            //  if (lijn.Length > 1)
            if (vind)
                M12.Text = lijn[0];
        }

        void Prosigns(object sender, EventArgs e)
        {
            var knop = sender as Button;
            string[] prosigns = { "BT", "AR", "KN", "BK", "SK" };
            string[] sett = { "=", "+", "(", "&", "$" };

            int nr = Array.IndexOf(prosigns, knop.Name);
            if (nr >= 0)
            {
                // weg = sett[nr];
                inputscreen.Text = inputscreen.Text + " " + sett[nr];
            }
        }

        void m_click(object sender, EventArgs e)
        {
            var knop = sender as Button;
            string[] lijn = new string[25];
            bool vond = false;
            vond = mx_lader(knop.Name + ".txt", lijn);
         //   string mode = rd.ActiveSlice.DemodMode;          
            if (vond)
            {              
                    if (lijn[1].Trim().Length > 1)
                    {
                        string reg = macro_omzet(lijn[1]);
                        inputscreen.Text = inputscreen.Text + " " + reg;
                   }                
            }
        }

        /*
    * %m My call sign. 
%n The other operator's name from the Name window. 
%q The other operator's QTH. 
%r The other station's RST from the RST window (it may include a contest number). 

%p = max power;

    * */
        string macro_omzet(string lijn)
        {
            string uit = "Empty";
            string naam2 = Naam1.Text;
            string rst2 = RSTuit.Text;
            string vermogen = "100";
            if (rst2.Length < 2 && Swaarde.Text.Length > 1)
            {
                rst2 = Swaarde.Text;
            }

            if (rst2 == "599")
                rst2 = "5nn";

            if (Output.Text.Length > 1)
                vermogen = Output.Text;

            if (naam2.Length < 2) naam2 = "op";
            if (rst2.Length < 2) rst2 = "5nn";
            if (lijn.Length > 1)
            {
                uit = lijn;
                uit = lijn.Replace("%r", rst2);
                uit = uit.Replace("%n", naam2);
                uit = uit.Replace("%p", vermogen);
            }
            return uit;
        }


        static string tooltip_lader(string knop)
        {
            string uit;
            string[] lijn = new string[25];
            string nm = knop + ".txt";
            bool vind = false;
            vind = mx_lader(nm, lijn);
            if (vind)
                uit = lijn[1];
            else uit = "Empty";
            return uit;
        }

        static bool mx_lader(string fnaam, string[] lijn)
        {
            int counter = 0;
            string line;

            lijn[0] = " ";
            // Read the file and display it line by line.
            bool waar = false;
            if (File.Exists(fnaam))
                waar = true;

            if (waar)
            {
                StreamReader file = new System.IO.StreamReader(fnaam);
                while ((line = file.ReadLine()) != null)
                {
                    lijn[counter] = line;
                    counter++;
                }
                file.Close();
            }
            return waar;
        }

        private void zetfilter(object sender, EventArgs e)
        {
            var knop = sender as Button;
            int numWaarde;

            bool isgetal = Int32.TryParse(knop.Name.Substring(6), out numWaarde);
            if (isgetal)
            {
                rd.ActiveSlice.FilterLow = (numWaarde * -1) / 2;
                rd.ActiveSlice.FilterHigh = numWaarde / 2;
            }
        }


        void pklik(object sender, EventArgs e) // set the output power 
        {
            var knop = sender as Button;
            int numWaarde;
            bool isgetal = Int32.TryParse(knop.Name.Substring(2), out numWaarde);
            Pw20.BackColor = Pw40.BackColor = Pw80.BackColor = Color.Gray;
            //    knop.BackColor = Color.Azure;

            if (isgetal)
            {
                rd.RFPower = numWaarde;
                knop.BackColor = Color.Azure;
            }
        }

        void zetwpm(object sender, EventArgs e)
        {
            WPM18.BackColor = WPM20.BackColor = WPM23.BackColor = WPM25.BackColor = WPM27.BackColor = WPM30.BackColor = Color.Gray;
            var knop = sender as Button;
            knop.BackColor = Color.Azure;
            int numWaarde;
            bool isgetal = Int32.TryParse(knop.Name.Substring(3), out numWaarde);
            if (isgetal) cwxx.Speed = numWaarde;
        }

        void load_start()
        {
            Pw20.BackColor = Pw40.BackColor = Pw80.BackColor = WPM18.BackColor = WPM20.BackColor = WPM23.BackColor = WPM25.BackColor = WPM27.BackColor = WPM30.BackColor = Color.Gray;
            int pw = rd.RFPower;
            int speed = cwxx.Speed;
            switch (pw)
            {
                case 20:
                    Pw20.BackColor = Color.Azure;
                    break;
                case 40:
                    Pw40.BackColor = Color.Azure;
                    break;
                case 80:
                    Pw80.BackColor = Color.Azure;
                    break;
                default:
                    break;
            }

            switch (speed)
            {
                case 18:
                    WPM18.BackColor = Color.Azure;
                    break;
                case 20:
                    WPM20.BackColor = Color.Azure;
                    break;
                case 23:
                    WPM23.BackColor = Color.Azure;
                    break;
                case 25:
                    WPM25.BackColor = Color.Azure;
                    break;
                case 27:
                    WPM27.BackColor = Color.Azure;
                    break;
                case 30:
                    WPM30.BackColor = Color.Azure;
                    break;
                default:
                    break;
            }
        }


        private void radioveranderd(object sender, PropertyChangedEventArgs e)
        {
            int watt1 = rd.RFPower;
            int watt = (watt1 / 10) * 10;
            if (watt1 < 20)
                watt = (watt1 / 2) * 2;
            Output.Text = watt.ToString(CultureInfo.CurrentCulture);
            load_start();
        }

        private void cwxveranderd(object sender, EventArgs e)
        {
            load_start();
        }

        private void invoerveranderd(object sender, EventArgs e)
        {
            if (cwx_buf == false && breakin1.Checked == true) cwxverzend(0);
        }

        private void cwxverzend(int c)  // reageert op een cwx karakter verzonden, zend dan het volgende karakter
        {
            if (inputscreen.Text.Length > 0) inputscreen.Text = Regex.Replace(inputscreen.Text, @"\s+", " ");  // remove double spaces! (don't works at CWX)

            if (inputscreen.Text.Length < 1)
                cwx_buf = false;  // geef controle aan invoerveranded (invoer vanaf invoerscherm)
            else
            {
                cwx_buf = true; // neem nu de controle over (Take the control over CWX)
       
                string weg = inputscreen.Text.Substring(0, 1);  // is 1 voldoende?? i.v.m. spaties!!!
                verstuur();
                try
                {
                    if (weg == " " && inputscreen.Text.Length > 0) verstuur();
                }
                catch
                {
                    MessageBox.Show(this,"Error on module cwxverzend.","Error", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
                }
            }
        }

        private void verstuur()
        {
            int lang = inputscreen.Text.Length;
            if (lang > 0)
            {
                string weg = inputscreen.Text.Substring(0, 1);  // is 1 voldoende?? i.v.m. spaties!!!
                cwxx.Send(weg);
                if (weg.Length > 1)
                {
                    //hier een eventlogpunt voor analyse te veel verzonden
                    //  Log_evt("Weg: " + weg, "CWX.txt");
                }
                inputscreen.Text = inputscreen.Text.Substring(1, lang - 1);
                outputscreen.Text = outputscreen.Text + weg;
                inputscreen.Select(inputscreen.Text.Length, 0);
                outputscreen.ScrollToCaret();
            }
        }

        void stopcwx(object sender, EventArgs e)
        {
            cwxx.ClearBuffer();
            cwx_buf = false;
            inputscreen.Clear();
            inputscreen.Focus();
        }

        void resetcwx(object sender, EventArgs e)
        {
            cwx_buf = false;
            cwxverzend(0);
        }

        void opschonen(object sender, EventArgs e)
        {
            outputscreen.Clear();
        }

        void opschoon_qso(object sender, EventArgs e)
        {
            Call1.Text = RSTuit.Text = RSTin.Text = Naam1.Text = QTH1.Text = "";
            Call1.BackColor = Color.White;
        }

        void cwx_call(object sender, EventArgs e)
        {
            // zet call tegenstation naar CWX scherm
            if (Call1.Text.Length > 2)
            {
                inputscreen.Text = inputscreen.Text + " " + Call1.Text + " ";
            }
        }

        void Sklik(object sender, EventArgs e)
        {
            var knop = sender as Button;
            int numWaarde;
            bool isgetal = Int32.TryParse(knop.Name.Substring(1), out numWaarde);
            S559.BackColor = Color.Gray;
            S579.BackColor = Color.Gray;
            S599.BackColor = Color.Gray;

            Swaarde.Text = numWaarde.ToString(CultureInfo.CurrentCulture);
            if (isgetal)
            {
                switch (numWaarde)
                {
                    case 559:
                        S559.BackColor = Color.Azure;
                        RSTuit.Text = "559";
                        break;
                    case 579:
                        S579.BackColor = Color.Azure;
                        RSTuit.Text = "579";
                        break;
                    case 599:
                        S599.BackColor = Color.Azure;
                        RSTuit.Text = "599";
                        break;
                    default:
                        break;
                }
            }
        }

        void cwf_macro(object sender, EventArgs e)
        {
            var knop = sender as Button;
            int numValue;
            bool isgetal = Int32.TryParse(knop.Name.Substring(2), out numValue);
            if (isgetal)
            {
                cwfxx(numValue - 1);
            }
        }

        void cwfxx(int mc)
        {
            String uit;
            if (cwxx.GetMacro(mc, out uit))
                inputscreen.Text = inputscreen.Text + " " + uit;
            inputscreen.Focus();
        }

    }
}
