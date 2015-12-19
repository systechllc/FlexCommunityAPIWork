using System;
using System.Collections;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Diagnostics;
using System.Threading;

namespace AsyncSocketsV2
{
    public class server
    {
        public AsyncCallback ClientCallback;
        private Socket ServerSocket;
        private ArrayList ClientSocketList = ArrayList.Synchronized(new ArrayList());
        private int ClientCount = 0;
        public delegate void ServerRxDataAvailable(string args, int id);
        public event ServerRxDataAvailable RxDataAvailable;

        public server(int port)
        {
            ServerSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
			//IPEndPoint ipep = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);
			IPEndPoint ipep = new IPEndPoint(IPAddress.Any, port);

            ServerSocket.Bind(ipep);
            ServerSocket.Listen(5);
            ServerSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }

        public int Clients
        {
            get { return ClientCount; }
        }

        public class ClientSocketID
        {
            public ClientSocketID(Socket socket, int n)
            {
                ClientSocket = socket;
                ClientID = n;
            }

            public Socket ClientSocket;
            public int ClientID;
            public byte[] db = new byte[1024];
        }

        private void OnClientConnect(IAsyncResult asyn)
        {
            Socket ClientSocket = ServerSocket.EndAccept(asyn);
            Interlocked.Increment(ref ClientCount);
            ClientSocketList.Add(ClientSocket);
            WaitForData(ClientSocket, ClientCount);
            ServerSocket.BeginAccept(new AsyncCallback(OnClientConnect), null);
        }


        private void OnDataReceived(IAsyncResult asyn)
        {
            ClientSocketID sdata = (ClientSocketID)asyn.AsyncState;
            try
            {
                int charsent = sdata.ClientSocket.EndReceive(asyn);
                string tmsg = ASCIIEncoding.ASCII.GetString(sdata.db, 0, charsent);
                RxData(tmsg, sdata.ClientID);
                WaitForData(sdata.ClientSocket, sdata.ClientID);
            }
            catch (SocketException se)
            {
                const int WSAECONNRESET = 10054;
                const int WSAECONNABORTED = 10053;
                if (se.ErrorCode == WSAECONNRESET || se.ErrorCode == WSAECONNABORTED)
                {
                    ClientSocketList[sdata.ClientID - 1] = null;
                    ClientCount = ClientCount - 1;
                    if (ClientCount == 0)
                        ClientSocketList.Clear();
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message);
            }
        }

        protected virtual void RxData(string args, int n)
        {
            ServerRxDataAvailable srxda = RxDataAvailable;
            if (srxda != null && n > 0)
                srxda(args, n);
        }

        private void WaitForData(Socket soc, int ClientNumber)
        {
            if (ClientCallback == null)
                ClientCallback = new AsyncCallback(OnDataReceived);
            
            ClientSocketID thisID = new ClientSocketID(soc, ClientNumber);

            soc.BeginReceive(
                thisID.db, 
                0, 
                thisID.db.Length, 
                SocketFlags.None, 
                ClientCallback, 
                thisID);
        }

        public void Send(String msg, int who)
        {
			try
			{
				byte[] bmsg = Encoding.ASCII.GetBytes(msg);
				int len = bmsg.Length;
				Socket msgSocket = (Socket)ClientSocketList[who - 1];
				ClientSocketID thisID = new ClientSocketID(msgSocket, who - 1);
				msgSocket.BeginSend(bmsg, 0, len, SocketFlags.None, SendCallback, thisID);
			}
			catch 
			{
			}
        }

        private void SendCallback(IAsyncResult ar)
        {
            ClientSocketID thisID = (ClientSocketID)ar.AsyncState;
            int bytestx = thisID.ClientSocket.EndSend(ar);
        }


        private void Cleanup()
        {
            if (ServerSocket != null)
                ServerSocket.Close();

            Socket thisSocket = null;
			for (int i = 0; i < ClientSocketList.Count; i++)
			{
				thisSocket = (Socket)ClientSocketList[i];
				if (thisSocket != null)
				{
					thisSocket.Close();
					thisSocket = null;
				}
			}
      }

        public void Close()
        {
            Cleanup();
        }

    }
}
