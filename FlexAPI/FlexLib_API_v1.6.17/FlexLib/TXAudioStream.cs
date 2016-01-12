// ****************************************************************************
///*!	\file TXAudioStream.cs
// *	\brief Represents a single TX Audio Stream (narrow, mono)
// *
// *	\copyright	Copyright 2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2013-11-18
// *	\author Eric Wachsmann, KE5DTO
// */
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using System.Globalization;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Flex.UiWpfFramework.Mvvm;
using Flex.Util;
using Flex.Smoothlake.Vita;
using System.Threading;

namespace Flex.Smoothlake.FlexLib
{
    public class TXAudioStream : ObservableObject
    {
        private Radio _radio;
        private bool _closing = false;
        private Socket _txSocket;
        private EndPoint _txDataEndPoint;

        internal bool Closing
        {
            set { _closing = value; }
        }

        public TXAudioStream(Radio radio)
        {
            this._radio = radio;

            _txSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _txDataEndPoint = new IPEndPoint(_radio.IP, 4991);
        }

        private uint _txStreamID;
        public uint TXStreamID
        {
            get { return _txStreamID; }
            internal set
            {
                if (_txStreamID != value)
                {
                    _txStreamID = value;
                    RaisePropertyChanged("TXStreamID");
                }
            }
        }

        private bool _radioAck = false;
        public bool RadioAck
        {
            get { return _radioAck; }
            internal set
            {
                if (_radioAck != value)
                {
                    _radioAck = value;
                    RaisePropertyChanged("RadioAck");
                }
            }
        }
    
        private bool _transmit;
        public bool Transmit
        {
            get { return _transmit; }
            set
            {
                if (_transmit != value)
                {
                    _transmit = value;
                    _radio.SendCommand("dax tx " + Convert.ToByte(_transmit));
                    RaisePropertyChanged("Transmit");
                }
            }
        }

        private int _txGain;
        public int TXGain
        {
            get { return _txGain; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain > 100) new_gain = 100;
                if (new_gain < 0) new_gain = 0;

                if (_txGain != new_gain)
                {
                    _txGain = value;
                    RaisePropertyChanged("TXGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("TXGain");
                }
            }
        }


        private IPAddress _ip;
        public IPAddress IP
        {
            get { return _ip; }
        }

        private int _port;
        public int Port
        {
            get { return _port; }
        }

        public bool RequestTXAudioStreamFromRadio()
        {
            // check to see if this object has already been activated
            if (_radioAck) return false;

            // check to ensure this object is tied to a radio object
            if (_radio == null) return false;

            // check to make sure the radio is connected
            if (!_radio.Connected) return false;

            // send the command to the radio to create the object
            _radio.SendReplyCommand(new ReplyHandler(UpdateStreamID), "stream create daxtx");

            return true;
        }

        private void UpdateStreamID(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            bool b = uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _txStreamID);

            if (!b)
            {
                Debug.WriteLine("TXAudioStream::UpdateStreamID-Error parsing Stream ID (" + s + ")");
                return;
            }

            _mine = true;

            _radio.AddTXAudioStream(this);
        }

        private bool _mine = false;
        public bool Mine
        {
            get { return _mine; }
        }

        public void Close()
        {
            Debug.WriteLine("TXAudioStream::Close (0x" + _txStreamID.ToString("X") + ")");
            _closing = true;
            _radio.SendCommand("stream remove 0x" + _txStreamID.ToString("X"));
            _radio.RemoveTXAudioStream(_txStreamID);
        }
        
        private VitaIFDataPacket _txPacket;
        public void AddTXData(float[] tx_data)
        {
            // skip this if we are not the DAX TX Client
            if (!_transmit) return;

            if (_txPacket == null)
            {
                _txPacket = new VitaIFDataPacket();
                _txPacket.header.pkt_type = VitaPacketType.IFDataWithStream;
                _txPacket.header.c = true;
                _txPacket.header.t = false;
                _txPacket.header.tsi = VitaTimeStampIntegerType.Other;
                _txPacket.header.tsf = VitaTimeStampFractionalType.SampleCount;

                _txPacket.stream_id = _txStreamID;
                _txPacket.class_id.OUI = 0x001C2D;
                _txPacket.class_id.InformationClassCode = 0x543C;
                _txPacket.class_id.PacketClassCode = 0x03E3;

            }

            int samples_sent = 0;

            //Debug.WriteLine("<" + tx_data.Length+">");
            while (samples_sent < tx_data.Length)
            {
                // how many samples should we send?
                int num_samples_to_send = Math.Min(256, tx_data.Length - samples_sent);

                _txPacket.payload = new float[num_samples_to_send];
                // copy the incoming data into the packet payload
                Array.Copy(tx_data, samples_sent, _txPacket.payload, 0, num_samples_to_send);

                // set the length of the packet
                _txPacket.header.packet_size = (ushort)(num_samples_to_send + 7); // 7*4=28 bytes of Vita overhead

                // send the packet to the radio
                //Debug.WriteLine("sending from channel " + _daxChannel);

                try
                {

                    _txSocket.SendTo(_txPacket.ToBytes(), _txDataEndPoint);
                }
                catch (Exception e)
                {
                    Debug.WriteLine("TXAudioStream: AddTXData Exception (" + e.ToString() + ")");
                }
                    //Debug.Write("("+num_samples_to_send+")");

                // bump the packet count
                _txPacket.header.packet_count = (byte)((_txPacket.header.packet_count + 1) % 16);

                // adjust the samples sent
                samples_sent += num_samples_to_send;
            }
        }

        
        public void StatusUpdate(string s)
        {
            // ignore status messages for audiostreams that are not mine
            if (!_mine) return;

            bool set_radio_ack = false;
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("TXAudioStream::StatusUpdate: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "ip":
                        {
                            IPAddress temp = null;
                            bool b = IPAddress.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("AudioStream::StatusUpdate: Invalid ip address (" + kv + ")");
                                continue;
                            }

                            _ip = temp;
                            RaisePropertyChanged("IP");

                            if (!_radioAck)
                                set_radio_ack = true;
                        }
                        break;

                    case "port":
                        {
                            ushort temp;
                            bool b = ushort.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("AudioStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _port = (int)temp;
                            RaisePropertyChanged("Port");
                        }
                        break;

                    case "dax_tx":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("AudioStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _transmit = Convert.ToBoolean(temp);
                            RaisePropertyChanged("Transmit");
                        }
                        break;
                    default:
                        Debug.WriteLine("AudioStream::StatusUpdate: Key not parsed (" + kv + ")");
                        break;
                }
            }

            if (set_radio_ack)
            {
                set_radio_ack = false;
                RadioAck = true;
                _radio.OnTXAudioStreamAdded(this);                
            }
        }
    }
}
