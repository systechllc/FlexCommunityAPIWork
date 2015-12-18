using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Net;
using System.Net.Sockets;

namespace NoAPIDiscoveryApplication
{
    public partial class Form1 : Form
    {
        private UdpClient udpDiscoveryClient;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            try
            {
                udpDiscoveryClient = new UdpClient();
                udpDiscoveryClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                udpDiscoveryClient.Client.Bind(new IPEndPoint(IPAddress.Any, 4992));
                timer1.Enabled = true;
            }
            catch (SocketException ex)
            {
                throw new SocketException(ex.ErrorCode);
            }
        }

        private void Form1_FormClosed(object sender, FormClosedEventArgs e)
        {
            udpDiscoveryClient.Close();
        }

        private void timer1_Tick(object sender, EventArgs e)
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            byte[] data = udpDiscoveryClient.Receive(ref ep);
            if( data.Length >= 16 )
            {
                //
                // I don't work with byte[] much and somewhere I've seen a really
                // cool way to manipulate byte[].  For this simple example I'm just
                // moving them into a string.  String class in C# handles just about anything
                //

                String datastr = System.Text.Encoding.Default.GetString(data);

                String header = datastr.Substring(0, 28);
                byte[] headerba = Encoding.Default.GetBytes(header);
                String radiodata = datastr.Substring(28, datastr.Length-28);

                String[] radiolist = radiodata.Split(' ');

                listBox1.Items.Add("------------ data gram ------------");
                listBox1.Items.Add("header=" + BitConverter.ToString(headerba));
                foreach (string s in radiolist)
                    listBox1.Items.Add(s);
            }
        }
    }
}
