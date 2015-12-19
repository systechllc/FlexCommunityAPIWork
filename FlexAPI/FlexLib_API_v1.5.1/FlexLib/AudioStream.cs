// ****************************************************************************
///*!	\file AudioStream.cs
// *	\brief Represents a single Audio Stream (narrow, mono)
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
    public class AudioStream : ObservableObject
    {
        private Radio _radio;
        private bool _closing = false;
        private Socket _txSocket;
        private EndPoint _txDataEndPoint;

        internal bool Closing
        {
            set { _closing = value; }
        }

        public AudioStream(Radio radio, int dax_channel)
        {
            this._radio = radio;
            this._daxChannel = dax_channel;

            _slice = _radio.FindSliceByDAXChannel(dax_channel);

            _txSocket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            _txDataEndPoint = new IPEndPoint(_radio.IP, 4991);
        }

        private uint _rxStreamId;
        public uint RXStreamID
        {
            get { return _rxStreamId; }
            internal set { _rxStreamId = value; }
        }

        private uint _txStreamID;
        public uint TXStreamID
        {
            get { return _txStreamID; }
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

        private int _daxChannel;
        public int DAXChannel
        {
            get { return _daxChannel; }
            internal set
            {
                if (_daxChannel != value)
                {
                    _daxChannel = value;
                    RaisePropertyChanged("DAXChannel");

                    if(_radio != null)
                        Slice = _radio.FindSliceByDAXChannel(_daxChannel);
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
                    _radio.SendCommand("dax audio set " + _daxChannel + " tx=" + Convert.ToByte(_transmit));
                    RaisePropertyChanged("Transmit");
                }
            }
        }

        private Slice _slice;
        public Slice Slice
        {
            get { return _slice; }
            internal set
            {
                if (_slice != value)
                {
                    _slice = value;
                    RaisePropertyChanged("Slice");

                    // resend the RX Gain for the new slice
                    if (_slice != null)
                    {
                        int gain = _rxGain;
                        _rxGain = 0;
                        RXGain = gain;
                    }
                }
            }
        }

        private int _rxGain = 50;
        public int RXGain
        {
            get { return _rxGain; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain > 100) new_gain = 100;
                if (new_gain < 0) new_gain = 0;

                if (_rxGain != new_gain)
                {
                    _rxGain = new_gain;
                    if(_slice != null)
                        _radio.SendCommand("audio stream 0x" + _rxStreamId.ToString("X") + " slice " + _slice.Index + " gain " + value);
                    RaisePropertyChanged("RXGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("RXGain");
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
                    // TODO: Add command here
                    RaisePropertyChanged("TXGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("TXGain");
                }
            }
        }

        private double _byteSum = 0;
        private double _bytesPerSecFromRadio;
        public double BytesPerSecFromRadio
        {
            get { return _bytesPerSecFromRadio; }
            set
            {
                if (_bytesPerSecFromRadio != value)
                {
                    _bytesPerSecFromRadio = value;
                    RaisePropertyChanged("BytesPerSecFromRadio");
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

        public bool RequestAudioStreamFromRadio()
        {
            // check to see if this object has already been activated
            if (_radioAck) return false;

            // check to ensure this object is tied to a radio object
            if (_radio == null) return false;

            // check to make sure the radio is connected
            if (!_radio.Connected) return false;

            // send the command to the radio to create the object
            _radio.SendReplyCommand(new ReplyHandler(UpdateStreamID), "stream create dax=" + _daxChannel);

            return true;
        }

        private void UpdateStreamID(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            bool b = uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _rxStreamId);

            if (!b)
            {
                Debug.WriteLine("AudioStream::UpdateStreamID-Error parsing Stream ID (" + s + ")");
                return;
            }

            _mine = true;

            _radio.AddAudioStream(this);
        }

        private bool _mine = false;
        public bool Mine
        {
            get { return _mine; }
        }

        public void Close()
        {
            Debug.WriteLine("AudioStream::Close (0x" + _rxStreamId.ToString("X") + ")");
            _closing = true;
            _radio.SendCommand("stream remove 0x" + _rxStreamId.ToString("X"));
            _radio.RemoveAudioStream(_rxStreamId);
        }

        private int error_count_out_of_order = 0;
        private int error_count_total = 0;
        public int ErrorCount
        {
            get { return error_count_total; }
            set
            {
                if (error_count_total != value)
                {
                    error_count_total = value;
                    RaisePropertyChanged("ErrorCount");
                }
            }
        }

        private int total_count = 0;
        public int TotalCount
        {
            get { return total_count; }
            set
            {
                if (total_count != value)
                {
                    total_count = value;
                    // only raise the property change every 100 packets (performance)
                    if (total_count % 100 == 0) RaisePropertyChanged("TotalCount");
                }
            }
        }


        // note that the "Lost" indicator assumes we only lost a single packet -- this is probably reasonable for "decent" networks
        private void PrintStats()
        {
            int lost = error_count_total - error_count_out_of_order;
            Debug.WriteLine("Audio Stream 0x" + _rxStreamId.ToString("X").PadLeft(8, '0') +
                "-Reversed: " + error_count_out_of_order + " (" + (error_count_out_of_order * 100.0 / total_count).ToString("f2") + ")" +
                "  Lost: " + lost + " (" + (lost * 100.0 / total_count).ToString("f2") + ")" +
                "  Total: " + total_count);
        }

        private const int NOT_INITIALIZED = 99;
        private int last_packet_count = NOT_INITIALIZED;
        private float[] old_data;
        private int old_packet_count = NOT_INITIALIZED;
        internal void AddRXData(VitaIFDataPacket packet)
        {
            TotalCount++;
            if (total_count % 1000 == 0) PrintStats();

            lock (this)
            {
                _byteSum += packet.Length;
                //Debug.WriteLine("packet.Length: " + packet.Length);
            }

            int packet_count = packet.header.packet_count;

            // normal case -- this is the next packet we are looking for, or it is the first one
            if (packet_count == (last_packet_count + 1) % 16 || last_packet_count == NOT_INITIALIZED)
            {
                OnRXDataReady(this, packet.payload);
                last_packet_count = packet_count;

                // check to see whether we were storing an out of order packet
                if (old_data != null && old_packet_count == (last_packet_count + 1) % 16)
                {
                    // yes, we were -- lets send this data out now that it is in the right order
                    error_count_out_of_order++;
                    OnRXDataReady(this, old_data);
                    last_packet_count = old_packet_count;

                    // now lets clean up
                    old_data = null; // is this a problem following the event trigger?
                    old_packet_count = NOT_INITIALIZED;
                }
                return;
            }

            // handle the exception case when packets arrive out of order or we lose packets
            // if this is the first issue, lets just store the data and packet count
            if (old_data == null)
            {
                old_data = packet.payload;
                old_packet_count = packet_count;
                ErrorCount++;
                return;
            }

            // if we made it this far, then we have already had at least one problem and are already storing old data
            // if it was just a problem arriving out of order, it will have already been taken care of by now
            // so this is a worse problem.  Let's just start over with this packet.
            OnRXDataReady(this, packet.payload);
            last_packet_count = packet_count;
            ErrorCount++;

            // now lets clean up
            old_data = null; // is this a problem following the event trigger?
            old_packet_count = NOT_INITIALIZED;
        }

        public delegate void RXDataReadyEventHandler(AudioStream audio_stream, float[] rx_data);
        public event RXDataReadyEventHandler RXDataReady;
        private void OnRXDataReady(AudioStream audio_stream, float[] rx_data)
        {
            if (RXDataReady != null)
                RXDataReady(audio_stream, rx_data);
        }

        private void UpdateRXRate()
        {
            while (!_closing)
            {
                lock (this)
                {
                    _bytesPerSecFromRadio = _byteSum;
                    _byteSum = 0;
                }
                RaisePropertyChanged("BytesPerSecFromRadio");
                //Debug.WriteLine("AudioStream-" + _daxChannel + " Rate: " + (_bytesPerSecFromRadio * 8.0 / 1000.0 / 1000.0).ToString("f3")+ " Mbps");
                Thread.Sleep(1000);
            }
        }

        private VitaIFDataPacket _txPacket;
        public void AddTXData(float[] tx_data)
        {
            Slice slc = _radio.FindSliceByDAXChannel(_daxChannel);
            if (slc == null) return;

            // skip this if the TX slice is not associated
            if (!slc.Transmit) return;

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

                _txSocket.SendTo(_txPacket.ToBytes(), _txDataEndPoint);
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
                    Debug.WriteLine("AudioStream::StatusUpdate: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "dax":
                        {
                            uint temp;
                            bool b = uint.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("AudioStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            DAXChannel = (int)temp;
                        }
                        break;

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

                    case "tx_stream":
                        {
                            uint temp;
                            bool b = uint.TryParse(value.Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("AudioStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txStreamID = temp;
                            RaisePropertyChanged("TXStreamID");
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

                    case "slice":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("AudioStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _slice = _radio.FindSliceByIndex(temp);
                            RaisePropertyChanged("Slice");
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
                _radio.OnAudioStreamAdded(this);                

                Thread t = new Thread(new ThreadStart(UpdateRXRate));
                t.Name = "AudioStream UpdateRXRate Thread";
                t.IsBackground = true;
                t.Priority = ThreadPriority.Normal;
                t.Start();
            }
        }
    }
}
