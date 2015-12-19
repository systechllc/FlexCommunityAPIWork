using System;
using System.ComponentModel;
using System.Windows.Forms;
using Flex.Smoothlake.FlexLib;
[assembly: CLSCompliant(false)]
namespace Sssdrcontest
{
    public partial class MainForm : Form
    {
        private Radio MijnRadio;
        private CWX _CWX;
        public MainForm()
        {
                   InitializeComponent();
            try
            {
                API.RadioAdded += API_RadioAdded;
                API.RadioRemoved += API_RadioRemoved;

                API.ProgramName = "Sssdrcontest";
                API.Init();
            }
            catch (Exception f)
            {
                MessageBox.Show(this, "Connection check", "Connection to radio failed"+ f.ToString(), MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
            }
           System.Threading.Thread.Sleep(3000);  // Necessary time out for connection (Why ????)
            MessageBox.Show(this, "Start" + Environment.NewLine + MijnRadio.Model + " " + MijnRadio.Nickname, "Start connection to radio", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
        }

        private void API_RadioAdded(Radio radio)
        {
            MijnRadio = radio;
            _CWX = MijnRadio.GetCWX();
            MijnRadio.SliceAdded += radio_SliceAdded;
            MijnRadio.SliceRemoved += radio_SliceRemoved;
       //     radio.PanadapterAdded += radio_PanadapterAdded;
          //   System.Threading.Thread.Sleep(600); // some time to make contact with the radio (why??)

            if (radio.Connect() == false)
            {
                MessageBox.Show(this, "Connection to Radio failed.", "Radio Connection", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
            }
            if (MijnRadio.Connect() == false)
            {
                MessageBox.Show(this, "Connection to radio failed", "Radioconnection", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
            }
        }

        private void API_RadioRemoved(Radio radio)
        {
            //  Console.WriteLine("API_RadioRemovfed Fired: " + radio);
            MijnRadio = null;
            MessageBox.Show(this, "Radio is removed!", "Radio", MessageBoxButtons.OK, MessageBoxIcon.Information, MessageBoxDefaultButton.Button1, MessageBoxOptions.RtlReading);
        }

        private void radio_SliceRemoved(Slice slc)
        {
          
        }

        private void radio_SliceAdded(Slice slc)
        {

            try
            {
        
            }
            catch
            {
                MessageBox.Show(this,"Error in function radio_SliceAdded(Slice slc) ","Error", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
            }
           
        }
/*
        private void radio_PanadapterRemoved(Panadapter pan)
        {
            Console.WriteLine("radio_PanadapterRemoved fired\n");
        }*/
/*
        private void radio_PanadapterAdded(Panadapter pan, Waterfall fall)
        {
            pan.PropertyChanged += panadapter_PropertyChanged;
        }
*/
        private void panadapter_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            Console.WriteLine("panadapter_PropertyChanged fired\n");
        }

        private void form_close(object sender, FormClosedEventArgs e)
        {
            Application.Exit();
        }

        private void openContest(object sender, EventArgs e)
        {
            if (MijnRadio.ActiveSlice.DemodMode == "CW")
            {
                Contest cont = new Contest(MijnRadio, _CWX);
                cont.Show();
            }
            else
                MessageBox.Show(this, "The active slice is not in CW mode!", "TRX not in correct mode", MessageBoxButtons.OK, MessageBoxIcon.Stop, MessageBoxDefaultButton.Button1, (MessageBoxOptions)0);
        }

        private void cwscreen(object sender, EventArgs e)
        {
            Cqso Cqso = new Cqso(MijnRadio, _CWX);
            Cqso.Show();
        }
    }
}
