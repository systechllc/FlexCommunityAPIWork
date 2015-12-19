using System;
using System.IO;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using System.Text;

namespace AsyncSocketsV2
{
    public class client
    {
        public Socket ClientSocket;
        public AsyncCallback ClientCallback;
        private byte[] db = new byte[10];
        private IAsyncResult async;
        public delegate void DataReceived(string args);
        public event DataReceived DataRx;
        public delegate void SocketErrorEventHandler(object src, SocketException se);
        public event SocketErrorEventHandler ErrorEvent;


        private string txtmsg;
        private bool isconnected = false;

        public bool IsConnected
        {
            get { return isconnected; }
        }

        public client(int port)
        {
            //IPHostEntry host = Dns.GetHostEntry("localhost");
            //IPAddress ipaddr = host.AddressList[0];
            IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
            ClientSocket = new Socket(ipep.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
			try
			{
				ClientSocket.Connect(ipep);
			}
			catch (SocketException se)
			{
				Debug.WriteLine(se.Message);
			}
            if (ClientSocket.Connected)
            {
                isconnected = true;
                WaitForData();
            }

        }

        public class SocketData
        {
            public Socket thisSocket;
            public byte[] db = new byte[1024];
        }

        private void WaitForData()
        {
            if (ClientCallback == null)
                ClientCallback = new AsyncCallback(OnDataReceived);

            SocketData csocket = new SocketData();
            csocket.thisSocket = ClientSocket;
            async = ClientSocket.BeginReceive(csocket.db,
                0,
                csocket.db.Length,
                SocketFlags.None,
                ClientCallback,
                csocket);
        }

        public void OnDataReceived(IAsyncResult ar)
        {
            try
            {
                SocketData thisID = (SocketData)ar.AsyncState;
                int charsrx = thisID.thisSocket.EndReceive(ar);
                char[] msg = new char[charsrx];
                Decoder dd = Encoding.UTF8.GetDecoder();
                int len = dd.GetChars(thisID.db, 0, charsrx, msg, 0);
                txtmsg = new String(msg);
				Debug.WriteLine("Message received:  " + txtmsg);
                OnDataRx(txtmsg);
                //Some code goes here
                WaitForData();
            }
            catch (SocketException se)
            {
                OnErrorEvent(this, se);
            }
        }

        protected virtual void OnDataRx(String args)
        {
            DataReceived dr = DataRx;
            if (dr != null)
                dr(args);
        }

        protected virtual void OnErrorEvent(object src, SocketException se)
        {
            SocketErrorEventHandler seeh = ErrorEvent;
            if (seeh != null)
                seeh(this, se);
        }

        public void Send(String msg)
        {
            byte[] bmsg = Encoding.ASCII.GetBytes(msg);
            int len = bmsg.Length;
            SocketData msgdata = new SocketData();
            msgdata.thisSocket = ClientSocket;
            ClientSocket.BeginSend(bmsg, 0, len, SocketFlags.None, SendCallback, msgdata);

        }

        private void SendCallback(IAsyncResult ar)
        {
            SocketData thisID = (SocketData)ar.AsyncState;
            int bytestx = thisID.thisSocket.EndSend(ar);
        }

        public void CleanUp()
        {
            if (ClientSocket.Connected)
                ClientSocket.Close();
        }

    }
}
