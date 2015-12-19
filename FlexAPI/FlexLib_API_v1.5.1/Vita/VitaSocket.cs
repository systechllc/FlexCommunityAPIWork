// ****************************************************************************
///*!	\file VitaSocket.cs
// *	\brief A Socket for use in communicating with the Vita protocol
// *
// *	\copyright	Copyright 2012-2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2012-03-05
// *	\author Eric Wachsmann, KE5DTO
// */
// ****************************************************************************

using System;
using System.Net;
using System.Net.Sockets;
using System.Diagnostics;


namespace Flex.Smoothlake.Vita
{
    public class VitaSocket
    {
        private VitaDataReceivedCallback callback;
        private Socket socket;

        private int port;
        public int Port
        {
            get { return port; }
        }

        //private IPAddress ip;
        public IPAddress IP
        {
            get
            {
                if (socket == null || socket.LocalEndPoint == null) return null;
                return ((IPEndPoint)(socket.LocalEndPoint)).Address;
            }
        }

        public VitaSocket(int _port, VitaDataReceivedCallback _callback)
        {
            bool done = false;
            port = _port;
            callback = _callback;

            while (!done)
            {
                try
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
                    //socket.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                    socket.ReceiveBufferSize = 150000 * 5;
                    socket.Bind(new IPEndPoint(IPAddress.Any, port));
                    done = true;
                }
                catch (SocketException ex) // if we get here, it is likely because the port is already open
                {
                    port++; // lets increment the port and try again
                    if (port > 5010)
                        throw new SocketException(ex.ErrorCode);
                }
            }
            
            // beging looking for UDP packets immediately
            StartReceive();
        }

        /// <summary>
        /// Begin an asynchronous receive
        /// </summary>
        private void StartReceive()
        {
            //Console.WriteLine("VitaSocket::StartReceive()");
            try
            {
                byte[] buf = new byte[VitaFlex.MAX_VITA_PACKET_SIZE];
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                socket.BeginReceiveFrom(buf, 0, VitaFlex.MAX_VITA_PACKET_SIZE, SocketFlags.None, ref remoteEndPoint,
                    new AsyncCallback(DataReceived), buf);      
            }
            catch (SocketException ex)
            {
                HandleException(ex, "VitaSocket::StartReceive");
            }           
        }

        private void DataReceived(IAsyncResult ar)
        {
            //Console.WriteLine("VitaSocket::DataReceived()");
            try
            {
                EndPoint remoteEndPoint = new IPEndPoint(IPAddress.Any, 0);
                int num_bytes = socket.EndReceiveFrom(ar, ref remoteEndPoint);
                byte[] buf = (byte[])ar.AsyncState;                

                if (callback != null)
                    callback((IPEndPoint)remoteEndPoint, buf, num_bytes);

                StartReceive();                
            }
            catch (SocketException ex)
            {
                HandleException(ex, "VitaSocket::DataReceived");
            }
        }

        private void HandleException(Exception ex, string function_path)
        {
            string s = function_path+" Exception: " + ex.Message + "\n\n";
            if (ex.InnerException != null)
                s += ex.InnerException.Message + "\n\n" + ex.InnerException.StackTrace;
            else s += ex.StackTrace;

            Debug.WriteLine(s);

            if (socket != null)
            {
                if (socket.Connected)
                    socket.Close();
            }
        }
    }
}
