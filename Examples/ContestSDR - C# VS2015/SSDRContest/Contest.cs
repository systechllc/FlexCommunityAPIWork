using System;
using System.Drawing;
using System.Linq;
using System.Text.RegularExpressions;
using System.IO;
using System.Windows.Forms;
using System.Globalization;
using Flex.Smoothlake.FlexLib;

namespace Sssdrcontest
{
    public partial class Contest : Form
    {
        Radio rd;
        CWX cwx;
        public Contest(Radio rd1, CWX cwx1)
        {
            InitializeComponent();
            rd = rd1;
            cwx = cwx1;
            WM18.BackColor = WM20.BackColor = WM23.BackColor = WM25.BackColor = WM27.BackColor = WM30.BackColor = Color.Gray;
            Pw20.BackColor = Pw40.BackColor = Pw80.BackColor = Color.Gray;
            B1.BackColor = B3.BackColor = B7.BackColor = B14.BackColor = B21.BackColor = B28.BackColor = Color.Gray;
            F100.BackColor = F50.BackColor = F75.BackColor = Color.Gray;
            c_lader();  // load text from cx.txt files
            werkBij(this, new EventArgs()); // set the buttons on initial value
        }

        void c_lader() // load text from cx.txt files
        {
            string[] lijn = new string[255];
            bool vind = false;
            vind = cx_lader("c1.txt", lijn);
            if (vind)
                C1.Text = lijn[0];
            Array.Clear(lijn, 0, lijn.Length);
            vind = cx_lader("c2.txt", lijn); ;
            if (vind)
                C2.Text = lijn[0];
            vind = cx_lader("c3.txt", lijn); ;
            if (vind)
                C3.Text = lijn[0];
            vind = cx_lader("c4.txt", lijn);
            if (vind)
                C4.Text = lijn[0];
            vind = cx_lader("c5.txt", lijn);
            if (vind)
                C5.Text = lijn[0];
            vind = cx_lader("c6.txt", lijn);
            if (vind)
                C6.Text = lijn[0];
            vind = cx_lader("c7.txt", lijn);
        }


        void zetwpm(object sender, EventArgs e) // setting the CWX speed
        {
            WM18.BackColor = WM20.BackColor = WM23.BackColor = WM25.BackColor = WM27.BackColor = WM30.BackColor = Color.Gray;
            var knop = sender as Button;
            knop.BackColor = Color.Azure;
            int numWaarde;
            bool isgetal = Int32.TryParse(knop.Name.Substring(2), out numWaarde);
            if (isgetal) cwx.Speed = numWaarde;
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

        void fklik(object sender, EventArgs e) // set CW filter
        {
            var knop = sender as Button;
            int numWaarde;
            bool isgetal = Int32.TryParse(knop.Name.Substring(1), out numWaarde);
            F100.BackColor = F50.BackColor = F75.BackColor = Color.Gray;
            knop.BackColor = Color.Azure;
            if (isgetal)
            {
                int hoog = (numWaarde / 2);
                int laag = hoog * -1;
                rd.ActiveSlice.UpdateFilter(laag, hoog);
            }
        }

        void bklik(object sender, EventArgs e) // set a new band
        {
            var knop = sender as Button;
              string[] band = { "B1", "B3", "B7", "B14", "B21", "B28" };
            double[] qrg = { 1.815, 3.510, 7.010, 14.010, 21.010, 28.010 };
 
            B1.BackColor = B3.BackColor = B7.BackColor = B14.BackColor = B21.BackColor = B28.BackColor = Color.Gray;
            knop.BackColor = Color.Azure;
            Pw20.BackColor = Pw40.BackColor = Pw80.BackColor = Color.Gray;

            int nr = Array.IndexOf(band, knop.Name);
            if (nr >= 0)
            {
                rd.ActiveSlice.Freq = qrg[nr];
            }
        }


        void c_click(object sender, EventArgs e) // start the message from the button (cx.txt files)
        {
            var knop = sender as Button;
            string[] lijn = new string[25];
            bool vond = false;
            vond = cx_lader(knop.Name + ".txt", lijn);
            if (vond && rd.ActiveSlice.DemodMode == "CW")
            {
                if (lijn.Count() > 2)
                {
                    if (lijn[1].Trim().Length > 2) // there is a bug if second line in file is empty!!!!
                    {
                        cwx.Send(Regex.Replace(lijn[1], @"\s+", " "));
                    }
                }
            }
        }


        static bool cx_lader(string fnaam, string[] lijn) // check of file exits
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

        private void stopCwx(object sender, EventArgs e) // clear the cwx buffer (also stops send)
        {
            cwx.ClearBuffer();
        }

        private void hoogTeller(object sender, EventArgs e) 
        {
            teller.Value = teller.Value + 1;
        }

        private void sendExchf(object sender, EventArgs e)  // send the exchange 
        {
            string cnt = teller.Value.ToString(CultureInfo.CurrentCulture);
            while (cnt.Length < 3)
            {
                cnt = "T" + cnt;
            }
            string uit = "R 5NN " + cnt;
            cwx.Send(uit);
        }

        private void sendNr(object sender, EventArgs e)  // send only the number
        {
            string cnt = teller.Value.ToString(CultureInfo.CurrentCulture);
            while (cnt.Length < 3)
            {
                cnt = "T" + cnt;
            }
            cwx.Send(cnt);
        }

        private void werkBij(object sender, EventArgs e) // counter was (re)set, update the text on the buttons
        {
            string cnt = teller.Value.ToString(CultureInfo.CurrentCulture);
            while (cnt.Length < 3)
            {
                cnt = "T" + cnt;
            }
            Nummer.Text = cnt;
            sendExch.Text = "R 5nn " + cnt;
        }

    }
}
