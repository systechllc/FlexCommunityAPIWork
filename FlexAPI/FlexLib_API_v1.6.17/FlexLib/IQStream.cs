// ****************************************************************************
///*!	\file IQStream.cs
// *	\brief Represents a single IQ Stream
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

using System.Diagnostics;
using System.Globalization;
using System.Net;
using System.Threading;
using Flex.UiWpfFramework.Mvvm;
using Flex.Smoothlake.Vita;
using Flex.Util;

namespace Flex.Smoothlake.FlexLib
{
    public class IQStream : ObservableObject
    {
        private Radio _radio;
        private bool _closing = false;

        public IQStream(Radio radio, int daxIQChannel)
        {
            this._radio = radio;
            this._daxIQChannel = daxIQChannel;

            _pan = radio.FindPanByDAXIQChannel(daxIQChannel);
        }

        private uint _streamId;
        public uint StreamID
        {
            get { return _streamId; }
            internal set { _streamId = value; }
        }

        private bool _radioAck = false;
        public bool RadioAck
        {
            get { return _radioAck; }
            private set
            {
                if (_radioAck != value)
                {
                    _radioAck = value;
                    RaisePropertyChanged("RadioAck");
                }
            }
        }

        private int _daxIQChannel;
        public int DAXIQChannel
        {
            get { return _daxIQChannel; }
            internal set
            {
                if (_daxIQChannel != value)
                {
                    _daxIQChannel = value;
                    RaisePropertyChanged("DAXIQChannel");

                    if (_radio != null)
                        Pan = _radio.FindPanByDAXIQChannel(_daxIQChannel);
                }
            }
        }

        private Panadapter _pan;
        public Panadapter Pan
        {
            get { return _pan; }
            set
            {
                if (_pan != value)
                {
                    _pan = value;
                    RaisePropertyChanged("Pan");
                }
            }
        }

        private int _sampleRate;
        public int SampleRate
        {
            get { return _sampleRate; }
            set
            {
                if (_sampleRate != value)
                {
                    _sampleRate = value;
                    if (_radio != null)
                        _radio.SendCommand("dax iq set " + _daxIQChannel + " rate=" + _sampleRate);
                    RaisePropertyChanged("SampleRate");
                }
            }
        }

        private int _totalCapacity;
        public int TotalCapacity
        {
            get { return _totalCapacity; }
            set
            {
                if (_totalCapacity != value)
                {
                    _totalCapacity = value;
                    RaisePropertyChanged("TotalCapacity");
                }
            }
        }

        private int _availableCapacity;
        public int AvailableCapacity
        {
            get { return _availableCapacity; }
            set
            {
                if (_availableCapacity != value)
                {
                    _availableCapacity = value;
                    RaisePropertyChanged("AvailableCapacity");
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

        private bool _streaming;
        public bool Streaming
        {
            get { return _streaming; }
        }

        public bool RequestIQStreamFromRadio()
        {
            // check to see if this object has already been activated
            if (_radioAck) return false;

            // check to ensure this object is tied to a radio object
            if (_radio == null) return false;

            // check to make sure the radio is connected
            if (!_radio.Connected) return false;

            // send the command to the radio to create the object
            _radio.SendReplyCommand(new ReplyHandler(UpdateStreamID), "stream create daxiq=" + _daxIQChannel);

            return true;
        }

        public bool RequestIQStreamFromRadio(IPAddress ip, ushort port)
        {
            // check to see if this object has already been activated
            if (_radioAck) return false;

            // check to ensure this object is tied to a radio object
            if (_radio == null) return false;

            // check to make sure the radio is connected
            if (!_radio.Connected) return false;

            // send the command to the radio to create the object
            _radio.SendReplyCommand(new ReplyHandler(UpdateStreamID), "stream create daxiq=" + _daxIQChannel + "ip="+ip.ToString()+" port="+port);

            return true;
        }

        private void UpdateStreamID(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            bool b = uint.TryParse(s, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out _streamId);

            if (!b)
            {
                Debug.WriteLine("IQStream::UpdateStreamID-Error parsing Stream ID (" + s + ")");
                return;
            }

            _mine = true;

            _radio.AddIQStream(this);
        }

        private bool _mine = false;
        public bool Mine
        {
            get { return _mine; }
        }

        public void Close()
        {
            _closing = true;
            Debug.WriteLine("IQStream::Close (0x" + _streamId.ToString("X") + ")");
            _radio.SendCommand("stream remove 0x" + _streamId.ToString("X"));
            _radio.RemoveIQStream(_streamId);
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
            Debug.WriteLine("IQ Stream 0x"+_streamId.ToString("X").PadLeft(8, '0') + 
                "-Reversed: " + error_count_out_of_order + " (" + (error_count_out_of_order * 100.0 / total_count).ToString("f2") + ")" +
                "  Lost: " + lost + " (" + (lost * 100.0 / total_count).ToString("f2") + ")" +
                "  Total: " + total_count);
        }

        private const int NOT_INITIALIZED = 99;
        private int last_packet_count = NOT_INITIALIZED;
        internal void AddData(VitaIFDataPacket packet)
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
            OnDataReady(this, packet.payload);

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

        public delegate void DataReadyEventHandler(IQStream iq_stream, float[] data);
        public event DataReadyEventHandler DataReady;
        private void OnDataReady(IQStream iq_stream, float[] data)
        {
            if (DataReady != null)
                DataReady(iq_stream, data);
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
            // ignore messages for other clients
            if (!_mine) return;

            bool set_radio_ack = false;
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("IQStream::StatusUpdate: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "daxiq":
                        {
                            uint temp;
                            bool b = uint.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            DAXIQChannel = (int)temp;
                        }
                        break;

                    case "ip":
                        {
                            IPAddress temp = null;
                            bool b = IPAddress.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid ip address (" + kv + ")");
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
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _port = (int)temp;
                            RaisePropertyChanged("Port");
                        }
                        break;

                    case "rate":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _sampleRate = temp;
                            RaisePropertyChanged("SampleRate");
                        }
                        break;

                    case "streaming":
                        {
                            ushort temp;
                            bool b = ushort.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _streaming = (temp == 1) ? true : false;
                            RaisePropertyChanged("Streaming");
                        }
                        break;

                    case "capacity":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _totalCapacity = temp;
                            RaisePropertyChanged("TotalCapacity");
                        }
                        break;

                    case "available":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("IQStream::StatusUpdate: Invalid value (" + kv + ")");
                                continue;
                            }

                            _availableCapacity = temp;
                            RaisePropertyChanged("AvailableCapacity");
                        }
                        break;

                    default:
                        Debug.WriteLine("IQStream::StatusUpdate: Key not parsed (" + kv + ")");
                        break;
                }
            }

            if (set_radio_ack)
            {
                set_radio_ack = false;
                RadioAck = true;
                _radio.OnIQStreamAdded(this);

                Thread t = new Thread(new ThreadStart(UpdateRXRate));
                t.Name = "IQStream UpdateRXRate Thread";
                t.IsBackground = true;
                t.Priority = ThreadPriority.Normal;
                t.Start();
            }
        }
    }
}
