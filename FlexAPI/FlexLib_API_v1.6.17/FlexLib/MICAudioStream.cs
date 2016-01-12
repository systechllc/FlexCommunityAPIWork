// ****************************************************************************
///*!	\file MICAudioStream.cs
// *	\brief Represents a single MICAudio Stream (narrow, mono)
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
    public class MICAudioStream : ObservableObject
    {
        private Radio _radio;
        private bool _closing = false;
    
        internal bool Closing
        {
            set { _closing = value; }
        }

        public MICAudioStream(Radio radio)
        {
            this._radio = radio;
        }

        private uint _rxStreamId;
        public uint RXStreamID
        {
            get { return _rxStreamId; }
            internal set { _rxStreamId = value; }
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

        private float _rxGainScalar = 1.0f;
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
                    RaisePropertyChanged("RXGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("RXGain");
                }

                if (_rxGain == 0)
                {
                    _rxGainScalar = 0.0f;
                    return;
                }
                double db_min = -10.0;
                double db_max = +10.0;
                double db = db_min + (_rxGain / 100.0) * (db_max - db_min);
                _rxGainScalar = (float)Math.Pow(10.0, db / 20.0);
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

        public bool RequestMICAudioStreamFromRadio()
        {
            // check to see if this object has already been activated
            if (_radioAck) return false;

            // check to ensure this object is tied to a radio object
            if (_radio == null) return false;

            // check to make sure the radio is connected
            if (!_radio.Connected) return false;

            // send the command to the radio to create the object
            _radio.SendReplyCommand(new ReplyHandler(UpdateStreamID), "stream create daxmic");

            return true;
        }

        private void UpdateStreamID(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            bool b = uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _rxStreamId);

            if (!b)
            {
                Debug.WriteLine("MICAudioStream::UpdateStreamID-Error parsing Stream ID (" + s + ")");
                return;
            }

            _mine = true;

            _radio.AddMICAudioStream(this);
        }

        private bool _mine = false;
        public bool Mine
        {
            get { return _mine; }
        }

        public void Close()
        {
            Debug.WriteLine("MICAudioStream::Close (0x" + _rxStreamId.ToString("X") + ")");
            _closing = true;
            _radio.SendCommand("stream remove 0x" + _rxStreamId.ToString("X"));
            _radio.RemoveMICAudioStream(_rxStreamId);
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
            Debug.WriteLine("MIC Audio Stream 0x" + _rxStreamId.ToString("X").PadLeft(8, '0') +
                "-Reversed: " + error_count_out_of_order + " (" + (error_count_out_of_order * 100.0 / total_count).ToString("f2") + ")" +
                "  Lost: " + lost + " (" + (lost * 100.0 / total_count).ToString("f2") + ")" +
                "  Total: " + total_count);
        }

        private const int NOT_INITIALIZED = 99;
        private int last_packet_count = NOT_INITIALIZED;
        internal void AddRXData(VitaIFDataPacket packet)
        {
            TotalCount++;

#if DEBUG_STATS
            if (total_count % 1000 == 0) PrintStats();
#endif
            lock (this)
            {
                _byteSum += packet.Length;
                //Debug.WriteLine("packet.Length: " + packet.Length);
            }

            int packet_count = packet.header.packet_count;
            OnRXDataReady(this, packet.payload);

            // normal case -- this is the next packet we are looking for, or it is the first one
            if (packet_count == (last_packet_count + 1) % 16 || last_packet_count == NOT_INITIALIZED)
            {
                last_packet_count = packet_count;
            }
            else
            {
                error_count_out_of_order++;
                ErrorCount++;
                last_packet_count = packet_count;
            }
        }

        public delegate void RXDataReadyEventHandler(MICAudioStream mic_audio_stream, float[] rx_data);
        public event RXDataReadyEventHandler RXDataReady;
        private void OnRXDataReady(MICAudioStream mic_audio_stream, float[] rx_data)
        {
            if (RXDataReady != null)
            {
                for (int i = 0; i < rx_data.Length; i++)
                {
                    rx_data[i] = rx_data[i] * _rxGainScalar;
                }
                RXDataReady(mic_audio_stream, rx_data);
            }
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
                    Debug.WriteLine("MICAudioStream::StatusUpdate: Invalid key/value pair (" + kv + ")");
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
                                Debug.WriteLine("MICAudioStream::StatusUpdate: Invalid ip address (" + kv + ")");
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
                                Debug.WriteLine("MICAudioStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _port = (int)temp;
                            RaisePropertyChanged("Port");
                        }
                        break;
                    default:
                        Debug.WriteLine("MICAudioStream::StatusUpdate: Key not parsed (" + kv + ")");
                        break;
                }
            }

            if (set_radio_ack)
            {
                set_radio_ack = false;
                RadioAck = true;
                _radio.OnMICAudioStreamAdded(this);                

                Thread t = new Thread(new ThreadStart(UpdateRXRate));
                t.Name = "MICAudioStream UpdateRXRate Thread";
                t.IsBackground = true;
                t.Priority = ThreadPriority.Normal;
                t.Start();
            }
        }
    }
}
