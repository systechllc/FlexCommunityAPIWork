// ****************************************************************************
///*!	\file Radio.cs
// *	\brief Represents a single radio
// *
// *	\copyright	Copyright 2012-2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2012-03-05
// *	\author Eric Wachsmann, KE5DTO
// */
// ****************************************************************************

//#define TIMING

using System;
using System.Collections;   // for Hashtable class
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel; // for ObservableCollection
using System.Diagnostics;   // for Debug.WriteLine
using System.Globalization; // for NumberStyles.HexNumber
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading;     // for AutoResetEvent
// for Size
using Flex.Smoothlake.Vita;
using Flex.UiWpfFramework.Mvvm;
using Flex.Util;
using Ionic.Zip;

namespace Flex.Smoothlake.FlexLib
{
    #region Enums

    public enum MessageResponse
    {
        Success = 0,
    }

    public enum MessageSeverity
    {
        Info = 0,
        Warning = 1,
        Error = 2,
        Fatal = 3
    }

    /// <summary>
    /// The interlock state of transmitter
    /// </summary>
    public enum InterlockState
    {
        None,
        Receive,
        Ready,
        NotReady,
        PTTRequested,
        Transmitting,
        TXFault,
        Timeout,
        StuckInput
    }

    /// <summary>
    /// The push-to-talk input source
    /// </summary>
    public enum PTTSource
    {
        None,
        SW, // SmartSDR, CAT, etc
        Mic,
        ACC,
        RCA
    }

    /// <summary>
    /// The reason that the InterlockState is
    /// in the state that it is in
    /// </summary>
    public enum InterlockReason
    {
        None,
        RCA_TXREQ,
        ACC_TXREQ,
        BAD_MODE,
        TUNED_TOO_FAR,
        OUT_OF_BAND,
        PA_RANGE,
        CLIENT_TX_INHIBIT,
        XVTR_RX_ONLY
    }

    /// <summary>
    /// Display options for the front panel of the radio
    /// </summary>
    public enum ScreensaverMode
    {
        None,
        Model,
        Name,
        Callsign        
    }

    public enum SourceInput
    {
        None = -1,
        SignalGenerator = 0,
        Microphone,
        Balanced,
        LineIn,
        ACC,
        DAX
    }

    /// <summary>
    /// The state of the automatic antenna tuning unit (ATU)
    /// </summary>
    public enum ATUTuneStatus
    {
        None = -1,
        NotStarted = 0,
        InProgress,
        Bypass,
        Successful,
        OK,
        FailBypass,
        Fail,
        Aborted,
        ManualBypass
    }

    public enum NetworkQuality
    {
        OFF,
        EXCELLENT,
        VERYGOOD,
        GOOD,
        FAIR,
        POOR
    }

    #endregion

    public delegate void ReplyHandler(int seq, uint resp_val, string s);
    public class Radio : ObservableObject
    {
        #region Variables

        private Hashtable _replyTable;
        private const int NETWORK_PING_FAIR_THRESHOLD_MS = 50;
        private const int NETWORK_PING_POOR_THRESHOLD_MS = 100;
        private const int LAST_PACKET_COUNT_UNINITIALIZED = -1;

        private Thread _meterProcessThread = null;
        private Thread _fftProcessThread = null;
        //private Thread _parseReadThread = null;

        private ConcurrentQueue<VitaMeterPacket> meterQueue = new ConcurrentQueue<VitaMeterPacket>();
        private AutoResetEvent _semNewMeterPacket = new AutoResetEvent(false);
        private AutoResetEvent _semNewFFTPacket = new AutoResetEvent(false);
        //private AutoResetEvent _semNewReadBuffer = new AutoResetEvent(false);

        private List<Slice> _slices;
        /// <summary>
        /// A List of Slices present in this Radio instance
        /// </summary>
        public List<Slice> SliceList
        {
            get { return _slices; }
        }

        private List<Panadapter> _panadapters;
        /// <summary>
        /// A List of Panadapters present in this Radio instance
        /// </summary>
        public List<Panadapter> PanadapterList
        {
            get { return _panadapters; }
        }

        private List<Memory> _memoryList;
        public List<Memory> MemoryList
        {
            get { return _memoryList; }
        }

        private List<Waterfall> _waterfalls;
        private List<Meter> _meters;
        private List<Equalizer> _equalizers;
        private List<AudioStream> _audioStreams;
        private List<TXAudioStream> _txAudioStreams;
        private List<MICAudioStream> _micAudioStreams;
        private List<OpusStream> _opusStreams;  // currently there is only one opus stream
        private List<IQStream> _iqStreams;
        private List<TNF> _tnfs;
        public List<TNF> TNFList
        {
            get { return _tnfs; }
        }
        private List<Xvtr> _xvtrs;
        private CWX _cwx;

#if TIMING
        private Hashtable cmd_time_table;

        private class CmdTime
        {
            public uint Sequence { get; set; }
            public string Command { get; set; }
            public string Reply { get; set; }
            public double Start { get; set; }
            public double Stop { get; set; }

            public double RoundTrip()
            {
                return Stop - Start;
            }

            public override string ToString()
            {
                return Sequence + ": " + Command + " | " + Reply + " | " + RoundTrip().ToString("f8");
            }
        }
#endif

        private HiPerfTimer t1;

        #endregion

        #region Properties
        private UInt64 _min_protocol_version = 0x0001000000000000;    //used to be 0x01000000 but changed to UInt64 version format
        private UInt64 _max_protocol_version = 0x0001020000000000;
        private UInt64 _protocol_version;
        /// <summary>
        /// The Protocol Version is a 64 bit number with the format
        /// "maj.min.v_a.v_b" where maj, min, and v_a are each 1 byte and
        /// v_b is 4 bytes. The most significant byte is not used and 
        /// must always be 0x00.
        /// Example: v1.1.0.4 would be 0x0001010000000004 (0x00 01 01 00 00000004)
        /// </summary>
        public UInt64 ProtocolVersion
        {
            get { return _protocol_version; }
        }

        private UInt64 _req_version = FirmwareRequiredVersion.RequiredVersion; //0x00000F0100000073;   //0.15.1.115
        public UInt64 ReqVersion
        {
            get { return _req_version; }
        }

        private string _branch_name = FirmwareRequiredVersion.BranchName;
        public string BranchName
        {
            get { return _branch_name; }
        }

        private ulong _version;
        public ulong Version
        {
            get { return _version; }
            set
            {
                if (_version != value)
                {
                    // NOTE: this gets called when a Discovery packet is found and the resulting Radio object is created
                    _version = value;
                    RaisePropertyChanged("Version");                    

                    // figure out whether the system has the smoothlake_dev file in the right place
                    bool dev_file_exists = false;
                    // make sure that an access exception when trying to get information about this file doesn't take down
                    // the whole application
                    try
                    {
                        string dev_file = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData) + "\\FlexRadio Systems\\smoothlake_dev";
                        dev_file_exists = File.Exists(dev_file);
                    }
                    catch (Exception)
                    {
                        // do nothing, just assume the file doesn't exist
                    }
                    
                    // was the file there?
                    if (dev_file_exists)
                    {
                        // yes, assume the developer knows what they are doing and will ensure
                        // the right version of firmware is running on the radio
                        _updateRequired = false;
                    }
                    else
                    {
                        // no, does the firmware version match exactly what we are looking for?
                        if (_version != _req_version)
                        {
                            // no -- prompt to run the update process
                            _updateRequired = true;
                        }
                        else
                        {
                            // yes -- we're good to connect
                            _updateRequired = false;
                        }
                    }

                    UpdateConnectedState();                    
                }
            }
        }

        private void UpdateConnectedState()
        {
            // first, check to see if we are in the middle of an update.  Don't go showing it as inuse and changing the display around if so.
            if (_updating || (_updateRequired && _connected))
            {
                ConnectedState = "Update";
            }
            // if we aren't in the middle of an update, show whether the unit is in use (this will not allow connection in SmartSDR)
            else if (_status == "In_Use")
            {
                ConnectedState = "In Use";
            }
            // if the radio is not in use, decide whether it requires and update or not
            else if (_status == "Available")
            {
                if (_updateRequired)
                    ConnectedState = "Update";
                else ConnectedState = "Available";
            }
        }

        private bool _updateRequired = false;

        private ulong _discoveryProtocolVersion;
        public ulong DiscoveryProtocolVersion
        {
            get { return _discoveryProtocolVersion; }
            set
            {
                if (_discoveryProtocolVersion != value)
                {
                    _discoveryProtocolVersion = value;
                    RaisePropertyChanged("DiscoveryProtocolVersion");
                }
            }
        }

        private string _versions;
        public string Versions
        {
            get { return _versions; }
        }

        private string _client_id;
        public string ClientID
        {
            get { return _client_id; }
        }        

        private string _model;
        /// <summary>
        /// The model name of the radio, i.e. "FLEX-6500" or "FLEX-6700"
        /// </summary>
        public string Model
        {
            get { return _model; }
            internal set
            {
                if (_model != value)
                {
                    _model = value;
                    RaisePropertyChanged("Model");
                }
            }
        }

        private string _serial;
        /// <summary>
        /// The serial number of the radio, including dashes
        /// </summary>
        public string Serial
        {
            get { return _serial; }
            internal set
            {
                if(_serial != value)
                {
                    _serial = value;
                    RaisePropertyChanged("Serial");
                }
            }
        }

        private IPAddress _ip;
        /// <summary>
        /// The TCP IP address of the radio
        /// </summary>
        public IPAddress IP
        {
            get { return _ip; }
            internal set
            {
                if (_ip != value)
                {
                    _ip = value;
                    RaisePropertyChanged("IP");
                }
            }
        }

        private string _inUseIP;
        public string InUseIP
        {
            get { return _inUseIP; }
            set
            {
                if (_inUseIP != value)
                {
                    _inUseIP = value;
                    RaisePropertyChanged("InUseIP");
                }
            }
        }

        private string _inUseHost;
        public string InUseHost
        {
            get { return _inUseHost; }
            set
            {
                if (_inUseHost != value)
                {
                    _inUseHost = value;
                    RaisePropertyChanged("InUseHost");
                }
            }
        }

        private int _commandPort = 4992;
        /// <summary>
        /// The TCP Port number of the radio, used for commands and status messages
        /// </summary>
        public int CommandPort
        {
            get { return _commandPort; }
            internal set
            {
                if (_commandPort != value)
                {
                    _commandPort = value;
                    RaisePropertyChanged("CommandPort");
                }
            }
        }

        private IPAddress _subnetMask;
        public IPAddress SubnetMask
        {
            get { return _subnetMask; }
            internal set
            {
                if (_subnetMask != value)
                {
                    _subnetMask = value;
                    RaisePropertyChanged("SubnetMask");
                }
            }
        }

        private bool _verbose = false;
        public bool Verbose
        {
            get { return _verbose; }
        }

        private string _connectedState = "Available";
        /// <summary>
        /// The state of the radio connection, i.e. "Update", "Available"
        /// </summary>
        public string ConnectedState
        {
            get { return _connectedState; }
            set
            {
                if (_connectedState != value)
                {
                    _connectedState = value;
                    RaisePropertyChanged("ConnectedState");
                }
            }
        }

        private bool _connected = false;
        /// <summary>
        /// The status of the connection.  True when the radio
        /// is connected, false when the radio is disconnected
        /// </summary>
        public bool Connected
        {
            get { return _connected; }
            internal set // not intended to be used externally -- for messaging
            {
                if (_connected != value)
                {
                    _connected = value;
                    RaisePropertyChanged("Connected");
                }
            }
        }

        private string _status;
        public string Status
        {
            get { return _status; }
            set
            {
                if (_status != value)
                {
                    _status = value;
                    UpdateConnectedState();
                    RaisePropertyChanged("Status");
                }
            }
        }

        private string[] _rx_ant_list;
        /// <summary>
        /// A list of the available RX Antenna ports on 
        /// the radio, i.e. "ANT1", "ANT2", "RX_A", 
        /// "RX_B", "XVTR"
        /// </summary>
        public string[] RXAntList
        {
            get { return _rx_ant_list; }
        }

        private string _radioOptions;
        public string RadioOptions
        {
            get { return _radioOptions; }
            internal set
            {
                if (_radioOptions != value)
                {
                    _radioOptions = value;
                    RaisePropertyChanged("RadioOptions");
                }
            }
        }

        private bool _showTxInWaterfall = true;
        public bool ShowTxInWaterfall
        {
            get { return _showTxInWaterfall; }
            set
            {
                if (_showTxInWaterfall != value)
                {
                    _showTxInWaterfall = value;
                    SendCommand("transmit set show_tx_in_waterfall=" + Convert.ToByte(_showTxInWaterfall));
                    RaisePropertyChanged("ShowTxInWaterfall");
                }
            }
        }

        private bool _binauralRX = false;
        public bool BinauralRX
        {
            get { return _binauralRX; }
            set
            {
                if (_binauralRX != value)
                {
                    _binauralRX = value;
                    SendCommand("radio set binaural_rx=" + Convert.ToByte(_binauralRX));
                    RaisePropertyChanged("BinauralRX");
                }
            }
        }

        private bool _snapTune = true;
        public bool SnapTune
        {
            get { return _snapTune; }
            set
            {
                if (_snapTune != value)
                {
                    _snapTune = value;
                    SendCommand("radio set snap_tune_enabled=" + Convert.ToByte(_snapTune));
                    RaisePropertyChanged("SnapTune");
                }
            }
        }

        private int _meterPacketTotalCount = 0;
        public int MeterPacketTotalCount
        {
            get { return _meterPacketTotalCount; }
            set
            {
                if (_meterPacketTotalCount != value)
                {
                    _meterPacketTotalCount = value;
                    // only raise the property change every 100 packets (performance)
                    if (_meterPacketTotalCount % 100 == 0) RaisePropertyChanged("MeterPacketTotalCount");
                }
            }
        }

        private int _meterPacketErrorCount = 0;
        public int MeterPacketErrorCount
        {
            get { return _meterPacketErrorCount; }
            set
            {
                if (_meterPacketErrorCount != value)
                {
                    _meterPacketErrorCount = value;
                    RaisePropertyChanged("MeterPacketErrorCount");
                }
            }
        }



        #endregion

        #region Constructor

        private int _unique_id;

        internal Radio()
        {
            _unique_id = new Random().Next();

            InitLists();
        }

        internal Radio(string model, string serial, string name, IPAddress ip, string version)
        {
            _unique_id = new Random().Next();

            this._model = model;
            this._serial = serial;
            this._nickname = name;
            this._ip = ip;
            

            UInt64 ver;
            bool b = FlexVersion.TryParse(version, out ver);
            if (!b)
            {
                Debug.WriteLine("Radio::Constructor: Error converting version string (" + version + ")");
            }
            else Version = ver;

            InitLists();

#if TIMING
            cmd_time_table = new Hashtable();
#endif

            t1 = new HiPerfTimer();
            t1.Start();
        }

        private void InitLists()
        {
            _replyTable = new Hashtable();
            _slices = new List<Slice>();
            _panadapters = new List<Panadapter>();
            _waterfalls = new List<Waterfall>();
            _meters = new List<Meter>();
            _equalizers = new List<Equalizer>();
            _audioStreams = new List<AudioStream>();
            _micAudioStreams = new List<MICAudioStream>();
            _txAudioStreams = new List<TXAudioStream>();
            _opusStreams = new List<OpusStream>();
            _iqStreams = new List<IQStream>();
            _micInputList = new List<string>();
            _tnfs = new List<TNF>();
            _xvtrs = new List<Xvtr>();
            _memoryList = new List<Memory>();
        }

        private const int TCP_READ_BUFFER_SIZE = 1024;
        private TcpClient _tcp_client = null;
        private NetworkStream _tcp_stream = null;

        /// <summary>
        /// The local client IP address
        /// </summary>
        public IPAddress LocalIP
        {
            get
            {
                if (!_connected || _tcp_client == null || 
                    _tcp_client.Client == null || _tcp_client.Client.LocalEndPoint == null) return null;
                return ((IPEndPoint)_tcp_client.Client.LocalEndPoint).Address;
            }
        }

        private object _connectSyncObj = new Object();

        /// <summary>
        /// Creates a TCP client and connects to the radio
        /// </summary>
        /// <returns>Connection status of the radio</returns>
        public bool Connect()
        {
            lock (_connectSyncObj)
            {
                if (_tcp_client != null) return true;

                int count = 0;
                while (count++ <= 10)
                {
                    try
                    {
                        // create tcp client object and connect to the radio
                        _tcp_client = new TcpClient();
                        //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
                        _tcp_client.Connect(new IPEndPoint(IP, _commandPort));
                        _tcp_stream = _tcp_client.GetStream();
                        count = 20;
                    }
                    catch (Exception ex)
                    {
                        string s = "Radio::Connect() -- Error creating TCP client\n";
                        s += ex.Message;
                        if (ex.InnerException != null)
                            s += "\n\n" + ex.InnerException.Message;
                        Debug.WriteLine(s);

                        // clean up tcp client object
                        if (_tcp_client != null)
                            _tcp_client = null;

                        // this is likely due to trying to reconnect too quickly -- lets try again after waiting
                        Thread.Sleep(1000);
                    }
                }

                if (count < 20)
                    return false;

                Connected = true;
                //StartParseReadThread();

                // setup for reading messages coming from the radio
                _tcp_stream.BeginRead(_tcp_read_buf, 0, TCP_READ_BUFFER_SIZE, new AsyncCallback(TCPReadCallback), null);

                // send client program to radio
                if (API.ProgramName != null && API.ProgramName != "")
                    SendCommand("client program " + API.ProgramName);

                if (API.IsGUI) SendCommand("client gui");

                // subscribe for status updates
                SendCommand("sub tx all");
                SendCommand("sub atu all");
                SendCommand("sub meter all");
                SendCommand("sub pan all");
                SendCommand("sub slice all");
                SendCommand("sub gps all");
                SendCommand("sub audio_stream all");
                SendCommand("sub cwx all");
                SendCommand("sub xvtr all");
                SendCommand("sub memories all");
                SendCommand("sub daxiq all");
                SendCommand("sub dax all");

                // get info (name, etc)
                GetInfo();

                // get version info from radio
                GetVersions();

                // get the list of antennas from the radio
                GetRXAntennaList();

                // get list of Input sources
                GetMicList();

                // get list of Profiles
                GetProfileLists();

                // turn off persistence if about to do an update
                if (ConnectedState == "Update")
                    SendCommand("client start_persistence off");

                // set the streaming UDP port for this client
                SendCommand("client udpport " + API.UDPPort);


                StartFFTProcessThread();
                StartMeterProcessThread();

                StartKeepAlive();

                return true;
            }
        }

        /// <summary>
        /// Closes the TCP client and disconnects the radio
        /// </summary>
        public void Disconnect()
        {
            //Console.WriteLine("FlexLib::Disconnect()");
            Connected = false;

            if (_tcp_client != null && _tcp_client.Connected)
            {
                _tcp_client.Close();
                _tcp_client = null;
            }

            lock (_xvtrs)
            {
                for (int i = 0; i < _xvtrs.Count; i++)
                {
                    Xvtr xvtr = _xvtrs[i];
                    RemoveXvtr(xvtr);
                    i--;
                }
            }

            lock(_equalizers)
                _equalizers.Clear();

            lock(_meters)
                _meters.Clear();

            RemoveAllSlices();

            RemoveAllPanadapters();

            RemoveAllWaterfalls();

            lock(_audioStreams)
                _audioStreams.Clear();

            lock(_txAudioStreams)
                _txAudioStreams.Clear();

            lock (_micAudioStreams)
                _micAudioStreams.Clear();

            lock(_opusStreams)
                _opusStreams.Clear();
            lock(_iqStreams)
                _iqStreams.Clear();
            lock(_replyTable)
                _replyTable.Clear();

            _nickname = null;
            _trxPsocVersion = 0;
            _paPsocVersion = 0;
            _fpgaVersion = 0;

            _rx_ant_list = null;

            _semNewFFTPacket.Set();
            _semNewMeterPacket.Set();

            RemoveOpusStream();

            API.RemoveRadio(this);
        }

        private void StartKeepAlive()
        {
            if (!_connected) return;

#if(!DEBUG)
            Thread t = new Thread(new ThreadStart(KeepAlive));
            t.Name = "KeepAlive Thread";
            t.IsBackground = true;
            t.Priority = ThreadPriority.Normal;
            t.Start();
#endif
        }

        private HiPerfTimer _keepAliveTimer = new HiPerfTimer();
        private void KeepAlive()
        {
            // set the time of the last reply to right now as a good starting place
            _lastPingReplyTime = 0;

            _keepAliveTimer.Start();

            // tell the radio to watch for pings
            SendCommand("keepalive enable");
            
            while (_connected)
            {
                // send a ping
                SendReplyCommand(new ReplyHandler(GetPingReply), "ping");

                // check to see how long it has been since we got a reply
                _keepAliveTimer.Stop();
                double delta_sec = _keepAliveTimer.Duration - _lastPingReplyTime;               

                // did we exceed the TIMEOUT?
                if (delta_sec > API.RADIOLIST_TIMEOUT_SECONDS)
                {
                    // yes -- we should disconnect
                    Disconnect();
                    API.LogDisconnect("Radio::KeepAlive()--" + this.ToString() + " ping timeout");
                }

                // wait for a second before sending again
                Thread.Sleep(1000); // just ping once a second
            }
        }

        double _lastPingReplyTime;
        private void GetPingReply(int seq, uint resp_val, string reply)
        {
            if (resp_val != 0) return;

            _keepAliveTimer.Stop();
            _lastPingReplyTime = _keepAliveTimer.Duration;
        }

        #endregion

        #region Reply/Status Processing Routines

        public void UDPDataReceivedCallback(VitaPacketPreamble vita_preamble, byte[] data, int bytes)
        {
            try
            {
                // ensure that the packet is at least long enough to inspect for VITA info
                //if (data.Length < 28) // this is now done at the level above
                //    return;

                //VitaPacketPreamble vita = new VitaPacketPreamble(data);

                // ensure the packet has our OUI in it -- looks like it came from us
                //if (vita.class_id.OUI != FLEX_OUI)
                //    return;

                // check to make sure the packet length matches the VITA packet
                //if (vita.header.packet_size * 4 != data.Length)
                //    return;

                switch (vita_preamble.header.pkt_type)
                {
                    //case VitaPacketType.IFDataWithStream:
                    //    if (vita.class_id.PacketClassCode == 0x03E4)
                    //    {
                    //        ProcessIFDataPacket(new VitaDataPacket(data));
                    //    }
                    //    break;
                    case VitaPacketType.ExtDataWithStream:
                        switch (vita_preamble.class_id.PacketClassCode)
                        {
                            case VitaFlex.SL_VITA_FFT_CLASS:
                                ProcessFFTDataPacket(new VitaFFTPacket(data));
                                break;
                            case VitaFlex.SL_VITA_OPUS_CLASS:   // Opus Encoded Audio
                                ProcessOpusDataPacket(new VitaOpusDataPacket(data, bytes));
                                break;
                            case VitaFlex.SL_VITA_IF_NARROW_CLASS: // DAX Audio
                                ProcessIFDataPacket(new VitaIFDataPacket(data, bytes));
                                break;                               
                            case VitaFlex.SL_VITA_METER_CLASS:
                                ProcessMeterDataPacket(new VitaMeterPacket(data));
                                break;
                            case VitaFlex.SL_VITA_WATERFALL_CLASS:
                                ProcessWaterfallDataPacket(new VitaWaterfallPacket(data));
                                break;
                            default:
                                //Debug.WriteLine("Unprocessed UDP packet");
                                break;
                        }
                        break;

                    case VitaPacketType.IFDataWithStream:
                        switch (vita_preamble.class_id.PacketClassCode)
                        {
                            case VitaFlex.SL_VITA_IF_WIDE_CLASS_24kHz: // DAX IQ
                            case VitaFlex.SL_VITA_IF_WIDE_CLASS_48kHz:
                            case VitaFlex.SL_VITA_IF_WIDE_CLASS_96kHz:
                            case VitaFlex.SL_VITA_IF_WIDE_CLASS_192kHz:
                                ProcessIFDataPacket(new VitaIFDataPacket(data, bytes));
                                break;
                        }
                        break;

                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine(ex.Message + "\n\n" + ex.StackTrace);
            }
        }

        private ConcurrentQueue<VitaFFTPacket> FFTPacketQueue = new ConcurrentQueue<VitaFFTPacket>();
        private void ProcessFFTDataPacket(VitaFFTPacket packet)
        {
            FFTPacketQueue.Enqueue(packet);
            _semNewFFTPacket.Set();

            //Panadapter pan = FindPanadapterByStreamID(packet.stream_id);
            //if (pan == null) return;

            //pan.AddData(packet.payload, packet.start_bin_index, packet.frame_index);
        }

        

        private void ProcessFFTDataPacket_ThreadFunction()
        {
            VitaFFTPacket packet = null;
            while (_connected)
            {
                bool try_dequeue_result = false;
                _semNewFFTPacket.WaitOne();
                if (!_connected) break;
                while (try_dequeue_result = FFTPacketQueue.TryDequeue(out packet))
                {
                    Panadapter pan = FindPanadapterByStreamID(packet.stream_id);
                    if (pan == null) continue;

                    pan.AddData(packet.payload, packet.start_bin_index, packet.frame_index, packet.header.packet_count);
                }
            }
        }

        private void ProcessWaterfallDataPacket(VitaWaterfallPacket packet)
        {
            Waterfall fall = FindWaterfallByStreamID(packet.stream_id);
            if (fall == null) return;

            fall.AddData(packet.tile, packet.header.packet_count);
        }

        private int last_packet_count = LAST_PACKET_COUNT_UNINITIALIZED;
        private void ProcessMeterDataPacket(VitaMeterPacket packet)
        {

            // queue and get out so we don't hold up the network thread
            meterQueue.Enqueue(packet);

            if (meterQueue.Count > 1000)
            {
                Debug.WriteLine("meterQueue.Count =  " + meterQueue.Count + ". This should not happen, please investigate. Flushing queue.");
                VitaMeterPacket trash = null;
                while (meterQueue.Count > 10)
                {
                    meterQueue.TryDequeue(out trash);
                }
            }

            _semNewMeterPacket.Set();

            // lost packet diagnostics
            int packet_count = packet.header.packet_count;
            MeterPacketTotalCount++;
            //normal case -- this is the next packet we are looking for, or it is the first one
            if (packet_count == (last_packet_count + 1) % 16 || last_packet_count == LAST_PACKET_COUNT_UNINITIALIZED)
            {
                // do nothing
            }
            else
            {
                Debug.WriteLine("Meter Packet: Expected " + ((last_packet_count + 1) % 16) + "  got " + packet_count);
                MeterPacketErrorCount++;
            }

            last_packet_count = packet_count;


            //for (int i = 0; i < packet.NumMeters; i++)
            //{
            //    int id = (int)packet.GetMeterID(i);
            //    Meter m = FindMeterByIndex(id);
            //    if (m != null)
            //        m.UpdateValue(packet.GetMeterValue(i));
            //}
        }

        public void ResetMeterPacketStatistics()
        {
            _meterPacketErrorCount = 0;
            _meterPacketTotalCount = 0;
            last_packet_count = LAST_PACKET_COUNT_UNINITIALIZED;
        }

        private void ProcessMeterDataPacket_ThreadFunction()
        {
            VitaMeterPacket packet = null;

            while (_connected)
            {
                _semNewMeterPacket.WaitOne();
                if (!_connected) break;
                // Contains the meter's ID and raw value
                Dictionary<int, short> meterUpdateDictionary = new Dictionary<int, short>();

                while (meterQueue.TryDequeue(out packet))
                {
                    for (int i = 0; i < packet.NumMeters; i++)
                    {
                        // Add the meter index to the update list if it doesn't already exist in the list.
                        // If it alraedy exists, update the value.
                        int id = (int)packet.GetMeterID(i);

                        if (!meterUpdateDictionary.ContainsKey(id))
                            meterUpdateDictionary.Add(id, packet.GetMeterValue(i));
                        else
                            meterUpdateDictionary[id] = packet.GetMeterValue(i);

                    }
                }

                // Update the meters to the GUI
                foreach (int meter_id in meterUpdateDictionary.Keys)
                {
                    Meter m = FindMeterByIndex(meter_id);
                    if (m != null)
                        m.UpdateValue(meterUpdateDictionary[meter_id]);
                }
            }
        }

        private void ProcessOpusDataPacket(VitaOpusDataPacket packet)
        {
            OpusStream opus_stream = FindOpusStreamByStreamID(packet.stream_id);

            if (opus_stream != null)
            {
                opus_stream.AddRXData2(packet);
                return;
            }
        }

        private void ProcessIFDataPacket(VitaIFDataPacket packet)
        {
            AudioStream audio_stream = FindAudioStreamByStreamID(packet.stream_id);
            if (audio_stream != null)
            {
                audio_stream.AddRXData(packet);
                return;
            }

            MICAudioStream mic_audio_stream = FindMICAudioStreamByStreamID(packet.stream_id);
            if (mic_audio_stream != null)
            {
                mic_audio_stream.AddRXData(packet);
                return;
            }

            IQStream iq_stream = FindIQStreamByStreamID(packet.stream_id);
            if (iq_stream == null) return;

            iq_stream.AddData(packet);
        }

        private byte[] _tcp_read_buf = new byte[TCP_READ_BUFFER_SIZE];
        private string _tcp_read_string_buffer = "";
        private void TCPReadCallback(IAsyncResult ar)
        {
            // Retrieve Read Bytes
            int num_bytes;
            try
            {
                num_bytes = _tcp_stream.EndRead(ar);
            }
            catch (Exception)
            {
                // if the stream is somehow closed, we should exit gracefully
                _tcp_read_string_buffer = "";
                Disconnect();
                API.LogDisconnect("Radio::TCPReadCallback: Exception in _tcp_stream.EndRead(ar)");
                return;
            }

            if (num_bytes == 0) // stream closed? -- need to handle disconnect
            {
                Disconnect();
                API.LogDisconnect("Radio::TCPReadCallback: 0 bytes read from _tcp_stream.EndRead(ar)");
                return;
            }

            // Convert byte array to a string
            string s = Encoding.UTF8.GetString(_tcp_read_buf, 0, num_bytes);

            // Add string to end of the reply string buffer -- ensure changes to string are safe
            lock (this)
            {
                _tcp_read_string_buffer += s;
            }

            // Process the reply string buffer
            ProcessReadBuffer();

            // Setup next Read
            try
            {
                _tcp_stream.BeginRead(_tcp_read_buf, 0, TCP_READ_BUFFER_SIZE, new AsyncCallback(TCPReadCallback), null);
            }
            catch (Exception)
            {
                Disconnect();
                API.LogDisconnect("Radio::TCPReadCallback: Exception in _tcp_stream.BeginRead()");
                return;
            }
        }

        private ConcurrentQueue<string> _readBufferQueue = new ConcurrentQueue<string>();

        private void ProcessReadBuffer()
        {
            bool processing = true;

            while (processing)
            {
                // look for end of message token
                int eom = _tcp_read_string_buffer.IndexOf('\n');

                // handle end of message token not found
                if (eom < 0) processing = false;
                else // process this message
                {
                    // strip message out of larger buffer
                    string s = _tcp_read_string_buffer.Substring(0, eom).Trim('\0');

                    // parse the message
                    ParseRead(s);

                    // add message to queue to be parsed
                    //_readBufferQueue.Enqueue(s);
                    //_semNewReadBuffer.Set();

#if DEBUG_IO
                    Debug.WriteLine(s);
#endif
                    // remove the processed message from the buffer -- ensure modification of string is safe
                    lock (this)
                    {
                        _tcp_read_string_buffer = _tcp_read_string_buffer.Substring(eom + 1);
                    }
                }
            }
        }

        #endregion

        #region Parse Routines

        //private void ParseRead_ThreadFunction()
        //{
        //    string message = null;
        //    bool try_dequeue_result = false;
        //    while (_connected)
        //    {
        //        _semNewReadBuffer.WaitOne();
        //        while (try_dequeue_result = _readBufferQueue.TryDequeue(out message))
        //        {                    
        //            ParseRead(message);
        //        }
        //    }
        //}

        private void ParseRead(string s)
        {
            // bump the ping reply timer so that EXTREMELY slow computers do not timeout
            _keepAliveTimer.Stop();
            _lastPingReplyTime = _keepAliveTimer.Duration;

            // handle empty string
            if (s.Length == 0) return;

            // decide what kind of message this is based on the first character
            switch (s[0]) // first character of message
            {
                case 'R': // reply
                    ParseReply(s);
                    break;
                case 'S': // status
                    ParseStatus(s);
                    break;
                case 'H': // handle
                    ParseHandle(s);
                    break;
                case 'V': // version
                    ParseProtocolVersion(s);
                    break;
                case 'M': // message
                    ParseMessage(s);
                    break;
            }
        }

        private void ParseReply(string s)
        {
            string[] tokens = s.Split('|');

            // handle incomplete reply -- must have at least 3 tokens
            if (tokens.Length < 3)
            {
                Debug.WriteLine("FlexLib::Radio::ParseReply: Incomplete reply -- must have at least 3 tokens (" + s + ")");
                return;
            }

            // handle first token shorter than minimum (2 characters)
            if (tokens[0].Length < 2)
            {
                Debug.WriteLine("FlexLib::Radio::ParseReply: First reply token invalid -- min 2 chars (" + s + ")");
                return;
            }

            // parse the sequence number
            int seq;
            bool b = int.TryParse(tokens[0].Substring(1), out seq);
            if (!b) // handle sequence number formatted improperly
            {
                Debug.WriteLine("FlexLib::Radio::ParseReply: Reply sequence invalid (" + s + ")");
                return;
            }

            // parse the hex response number
            uint resp;
            b = uint.TryParse(tokens[1], NumberStyles.HexNumber, CultureInfo.InvariantCulture, out resp);
            if (!b) // handle response number formatted improperly
            {
                Debug.WriteLine("FlexLib::Radio::ParseReply: Reply response number invalid (" + s + ")");
                return;
            }

            // parse the message
            string msg = tokens[2];

            // parse optional debug if present
            string debug = "";
            if (tokens.Length == 4)
                debug = tokens[3];

#if TIMING
            // add timing info
            if (cmd_time_table.Contains(seq))
            {
                CmdTime cmd_time = (CmdTime)cmd_time_table[seq];
                cmd_time.Reply = msg;
                t1.Stop();
                cmd_time.Stop = t1.Duration;

                Debug.WriteLine(cmd_time.ToString());
            }
#endif

            //Debug.WriteLine("ParseReply: " + seq + ": " + s);

            ReplyHandler handler = null;

            // is there an entry in the reply table looking for this reply
            lock (_replyTable)
            {
                if (_replyTable.ContainsKey(seq))
                {
                    // yes -- pull the handler out of the reply table
                    handler = (ReplyHandler)_replyTable[seq];

                    // remove the handler from the table as there will only be one response from any one command
                    _replyTable.Remove(seq);
                }
            }

            // call the method to handle the reply on the object from the table
            if(handler != null)                
                handler(seq, resp, msg);
        }

        private void ParseStatus(string s)
        {
            string[] tokens = s.Split('|');
            // handle minimum status tokens
            if (tokens.Length < 2)
            {
                Debug.WriteLine("ParseStatus: Invalid status -- min 2 tokens (" + s + ")");
                return;
            }

            string[] words = tokens[1].Split(' ');

            switch (words[0])
            {
                case "mic_audio_stream":
                    { 
                        if (words.Length < 3)
                        {
                            Debug.WriteLine("ParseStatus: Too few words for mic_audio_stream status -- min 3(" + s + ")");
                            return;
                        }

                        uint stream_id;
                        bool b = uint.TryParse(words[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid mic_audio_stream stream_id (" + s + ")");
                            return;
                        }

                        bool add_new = false;
                        MICAudioStream mic_audio_stream = FindMICAudioStreamByStreamID(stream_id);
                        if (mic_audio_stream == null)
                        {
                            if (s.Contains("in_use=0")) return;

                            add_new = true;
                            mic_audio_stream = new MICAudioStream(this);
                            mic_audio_stream.RXStreamID = stream_id;
                        }

                        if (s.Contains("in_use=0"))
                        {
                            lock (_micAudioStreams)
                            {
                                mic_audio_stream.Closing = true;
                            }
                            RemoveMICAudioStream(mic_audio_stream.RXStreamID);
                        }
                        else
                        {
                            string update = tokens[1].Substring(17 + words[1].Length + 1); // mic_audio_stream <client_id>
                            mic_audio_stream.StatusUpdate(update);
                        }

                        if (add_new)
                            AddMICAudioStream(mic_audio_stream);
                    }
                    break;
                case "tx_audio_stream":
                    {
                        if (words.Length < 3)
                        {
                            Debug.WriteLine("ParseStatus: Too few words for tx_audio_stream status -- min 3(" + s + ")");
                            return;
                        }

                        uint stream_id;
                        bool b = uint.TryParse(words[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid tx_audio_stream stream_id (" + s + ")");
                            return;
                        }

                        bool add_new = false;
                        TXAudioStream tx_audio_stream = FindTXAudioStreamByStreamID(stream_id);
                        if (tx_audio_stream == null)
                        {
                            if (s.Contains("in_use=0")) return;

                            add_new = true;
                            tx_audio_stream = new TXAudioStream(this);
                            tx_audio_stream.TXStreamID = stream_id;
                        }

                        if (s.Contains("in_use=0"))
                        {
                            lock (_txAudioStreams)
                            {
                                tx_audio_stream.Closing = true;
                            }
                            RemoveTXAudioStream(tx_audio_stream.TXStreamID);
                        }
                        else
                        {
                            string update = tokens[1].Substring(16 + words[1].Length + 1); // tx_audio_stream <client_id>
                            tx_audio_stream.StatusUpdate(update);
                        }

                        if (add_new)
                            AddTXAudioStream(tx_audio_stream);
                    }
                    break;
                case "audio_stream":
                    {
                        if (words.Length < 4)
                        {
                            Debug.WriteLine("ParseStatus: Too few words for audio_stream status -- min 3(" + s + ")");
                            return;
                        }

                        uint stream_id;
                        bool b = uint.TryParse(words[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid audio_stream stream_id (" + s + ")");
                            return;
                        }

                        bool add_new = false;
                        AudioStream audio_stream = FindAudioStreamByStreamID(stream_id);
                        if (audio_stream == null)
                        {
                            if (s.Contains("in_use=0")) return;

                            uint daxChannel;
                            b = uint.TryParse(words[2].Substring(4), out daxChannel); // cut out dax= 
                            if (!b)
                            {
                                Debug.WriteLine("ParseStatus: Invalid audio_stream daxChannel (" + s + ")");
                                return;
                            }

                            add_new = true;
                            audio_stream = new AudioStream(this, (int)daxChannel);
                            audio_stream.RXStreamID = stream_id;
                        }

                        if (s.Contains("in_use=0"))
                        {
                            lock (_audioStreams)
                            {
                                audio_stream.Closing = true;
                            }
                            RemoveAudioStream(audio_stream.RXStreamID);
                        }
                        else
                        {
                            string update = tokens[1].Substring(13 + words[1].Length + 1); // audio_stream <client_id>
                            audio_stream.StatusUpdate(update);
                        }

                        if (add_new)
                            AddAudioStream(audio_stream);
                    }
                    break;                

                case "atu":
                    ParseATUStatus(tokens[1].Substring(4)); // "atu "
                    break;

                case "cwx":
                    if (_cwx == null)
                        _cwx = new CWX(this);
                    _cwx.StatusUpdate(tokens[1].Substring(4)); // "cwx "
                    break;

                case "display":
                    {
                        if (words.Length < 4)
                        {
                            Debug.WriteLine("ParseStatus: Too few words for display status -- min 3(" + s + ")");
                            return;
                        }

                        switch (words[1])
                        {
                            case "pan":
                                uint stream_id;
                                bool b = uint.TryParse(words[2].Substring("0x".Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                                if (!b)
                                {
                                    Debug.WriteLine("ParseStatus: Invalid display pan stream_id (" + s + ")");
                                    return;
                                }

                                //Debug.WriteLine("Stream Id Parsed From Status = 0x" + words[2].Substring(2) + " (" + stream_id.ToString()+")");

                                bool add_new_pan = false;
                                Panadapter pan = FindPanadapterByStreamID(stream_id);
                                if (pan == null)
                                {
                                    if (s.Contains("removed")) return;
                                    pan = new Panadapter(this, 0, 0);
                                    pan.StreamID = stream_id;
                                    add_new_pan = true;
                                }

                                if (s.Contains("removed"))
                                {
                                    lock (_panadapters)
                                    {
                                        //pan.Remove();
                                        _panadapters.Remove(pan);
                                        OnPanadapterRemoved(pan);
                                        //Debug.WriteLine("Display removed from "+ip);
                                    }
                                }
                                else
                                {

                                    if (add_new_pan)
                                    {
                                        AddPanadapter(pan);
                                    }

                                    string update = tokens[1].Substring("display pan ".Length + words[2].Length + 1); // display pan <client_id> -- +1 for the trailing space
                                    pan.StatusUpdate(update);
                                }

                                break;

                            case "waterfall":
                                b = uint.TryParse(words[2].Substring("0x".Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                                if (!b)
                                {
                                    Debug.WriteLine("ParseStatus: Invalid waterfall pan stream_id (" + s + ")");
                                    return;
                                }

                                //Debug.WriteLine("Stream Id Parsed From Status = 0x" + words[2].Substring(2) + " (" + stream_id.ToString()+")");

                                bool add_new_fall = false;
                                Waterfall fall = FindWaterfallByStreamID(stream_id);
                                if (fall == null)
                                {
                                    if (s.Contains("removed")) return;
                                    fall = new Waterfall(this, 0, 0);
                                    fall.StreamID = stream_id;
                                    add_new_fall = true;
                                }

                                if (s.Contains("removed"))
                                {
                                    lock (_waterfalls)
                                    {
                                        //pan.Remove();
                                        _waterfalls.Remove(fall);
                                        OnWaterfallRemoved(fall);
                                        //Debug.WriteLine("Display removed from "+ip);
                                    }
                                }
                                else

                                {
                                    if (add_new_fall)
                                    {
                                        AddWaterfall(fall);
                                    }
                                    string update = tokens[1].Substring("display waterfall ".Length + words[2].Length + 1); // display fall <client_id> -- +1 for trailing space
                                    fall.StatusUpdate(update);
                                }

                                break;

                            case "panafall":
                                break;
                        }
                        break;
                    }

                case "eq":
                    if (s.Contains("txsc"))
                    {
                        Equalizer eq = FindEqualizerByEQSelect(EqualizerSelect.TX);
                        if (eq == null)
                            return;
                        string update = tokens[1].Substring("eq_tx".Length); // "eq tx " 
                        eq.StatusUpdate(update);
                    }
                    else if (s.Contains("rxsc"))
                    {
                        Equalizer eq = FindEqualizerByEQSelect(EqualizerSelect.RX);
                        if (eq == null)
                            return;
                        string update = tokens[1].Substring("eq rx".Length); // "eq rx "
                        eq.StatusUpdate(update);
                    }
                    else if (s.Contains("apf"))
                    {
                        ParseAPFStatus(tokens[1].Substring("eq apf ".Length)); // "eq apf "
                    }
                    break;                

                case "file":
                    {
                        ParseUpdateStatus(tokens[1].Substring("file update ".Length)); // "file update "
                    }
                    break;

                case "gps":
                    {
                        ParseGPSStatus(tokens[1].Substring("gps ".Length)); // "gps "
                    }
                    break;

                case "interlock":
                    ParseInterlockStatus(tokens[1].Substring("interlock ".Length)); // "interlock "
                    break;

                case "memory":
                    {
                        // handle minimum words
                        if (words.Length < 3 || words[1] == "")
                        {
                            Debug.WriteLine("ParseStatus: Too few words for Memory status -- min 3 (" + s + ")");
                            return;
                        }

                        uint index;
                        bool b = uint.TryParse(words[1], out index);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid memory index (" + s + ")");
                            return;
                        }

                        bool add_memory = false;
                        Memory mem = FindMemoryByIndex((int)index);
                        if (mem == null)
                        {
                            if (s.Contains("removed"))
                                return;

                            mem = new Memory(this);
                            mem.Index = (int)index;
                            add_memory = true;
                        }

                        if (s.Contains("removed"))
                        {
                            mem.RadioAck = false;
                            RemoveMemory(mem);
                            return;
                        }

                        string update = tokens[1].Substring("memory ".Length + words[1].Length + 1); // "slice <num> "


                        if (add_memory)
                            AddMemory(mem);

                        mem.StatusUpdate(update);
                    }
                    break;

                case "meter":
                    ParseMeterStatus(tokens[1].Substring("meter ".Length)); // 6 to skip the leading 'meter '
                    break;

                case "opus_stream":
                    {
                        if (words.Length < 2)
                        {

                            Debug.WriteLine("ParseStatus: Too few words for opus_stream status -- min 2(" + s + ")");
                            return;
                        }

                        uint stream_id;
                        bool b = uint.TryParse(words[1].Substring(2), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid opus_stream stream_id (" + s + ")");
                            return;
                        }

                        bool add_new = false;
                        OpusStream opus_stream = FindOpusStreamByStreamID(stream_id);

                        if (opus_stream == null)
                        {
                            //create an opus_stream if one has not yet been created
                            add_new = true;
                            opus_stream = new OpusStream(this);
                            opus_stream.RXStreamID = stream_id;                            
                        }

                        // there is no need to remove an opusStream...so let's leave it there
                        //if (s.Contains("rx_on=0") && s.Contains("tx_on=0"))
                        //{
                        //    // a valid opus stream already exists, but both rx and tx are turned off,
                        //    // so remove this opus stream
                        //    lock (_opusStreams)
                        //    {
                        //        opus_stream.Remove();
                        //        _opusStreams.Remove(opus_stream);
                        //    }

                        //    if (opus_stream != null)
                        //        OnOpusStreamRemoved(opus_stream);
                        //}
                        //else
                        //{
                        //    // a valid opus stream already exists, and either or both rx and tx are turned on,
                        //    // update this opus stream with this data
                        //    string update = tokens[1].Substring(12 + words[1].Length + 1); // opus_stream <stream_id> <rx_on>
                        //    opus_stream.StatusUpdate(update);
                        //}

                        string update = tokens[1].Substring(12 + words[1].Length + 1); // opus_stream <stream_id> <rx_on>
                        opus_stream.StatusUpdate(update);


                        if (add_new)
                        {
                            // We have added a brand new opus stream, so add it to our collection.
                            // Today, there will only be 0 or 1 items in this collection.
                            AddOpusStream(opus_stream);
                        }

                        // we fire the OnOpusStreamAdded event in the OpusStream class so that we can ensure that good
                        // staus info is present before notifying the client
                    }
                    break;  
                case "profile":
                    ParseProfilesStatus(tokens[1].Substring("profile ".Length)); // "profile "
                    break;

                case "radio":
                    ParseRadioStatus(tokens[1].Substring("radio ".Length)); // radio 
                    break;

                case "slice":
                    {
                        // handle minimum words
                        if (words.Length < 3 || words[1] == "")
                        {
                            Debug.WriteLine("ParseStatus: Too few words for slice status -- min 3 (" + s + ")");
                            return;
                        }

                        uint index;
                        bool b = uint.TryParse(words[1], out index);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid slice index (" + s + ")");
                            return;
                        }

                        bool add_slice = false;
                        Slice slc = FindSliceByIndex((int)index);
                        if (slc == null)
                        {
                            if (s.Contains("in_use=0"))
                                return;

                            slc = new Slice(this);
                            slc.Index = (int)index;

                            lock (_meters)
                            {
                                for (int i = 0; i < _meters.Count; i++)
                                {
                                    if (_meters[i].Source == "SLC" && _meters[i].SourceIndex == index)
                                        slc.AddMeter(_meters[i]);
                                }
                            }

                            add_slice = true;
                        }

                        if (s.Contains("in_use=0"))
                        {
                            slc.RadioAck = false;
                            RemoveSlice(slc);
                            return;
                        }

                        if (s.Contains("in_use=1")) // EW 2014-11-03: this is happening much more than I would expect
                        {
                            lock (_meters)
                            {
                                for (int i = 0; i < _meters.Count; i++)
                                {
                                    if (_meters[i].Source == "SLC" && _meters[i].SourceIndex == index)
                                        slc.AddMeter(_meters[i]);
                                }
                            }
                        }

                        string update = tokens[1].Substring(7 + words[1].Length); // "slice <num> "


                        if (add_slice)
                            AddSlice(slc);

                        slc.StatusUpdate(update);

                        //OnSliceAdded(slc);
                        break;
                    }

                case "stream":
                    {
                        if (words.Length < 4)
                        {
                            Debug.WriteLine("ParseStatus: Too few words for iq_stream status -- min 3(" + s + ")");
                            return;
                        }

                        uint stream_id;
                        bool b = uint.TryParse(words[1].Substring("0x".Length), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out stream_id);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid iq_stream stream_id (" + s + ")");
                            return;
                        }

                        bool add_new = false;
                        IQStream iq_stream = FindIQStreamByStreamID(stream_id);
                        if (iq_stream == null)
                        {
                            if (s.Contains("in_use=0")) return;

                            uint daxIQChannel;
                            int index = words[2].IndexOf("=") + 1;
                            b = uint.TryParse(words[2].Substring(index), out daxIQChannel);
                            if (!b)
                            {
                                Debug.WriteLine("ParseStatus: Invalid iq_stream daxIQChannel (" + s + ")");
                                return;
                            }

                            add_new = true;
                            iq_stream = new IQStream(this, (int)daxIQChannel);
                            iq_stream.StreamID = stream_id;
                        }

                        if (s.Contains("in_use=0"))
                        {
                            RemoveIQStream(iq_stream.StreamID);
                        }
                        else
                        {
                            string update = tokens[1].Substring("stream ".Length + words[1].Length + 1); // stream <stream_id>
                            iq_stream.StatusUpdate(update);
                        }

                        if (add_new)
                            AddIQStream(iq_stream);
                    }
                    break;

                case "tnf":
                    {
                        uint tnf_id;
                        bool b = uint.TryParse(words[1], NumberStyles.Any, CultureInfo.InvariantCulture, out tnf_id);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid TNF ID (" + s + ")");
                            return;
                        }

                        bool add_new_tnf = false;

                        TNF tnf = FindTNFById(tnf_id);
                        if (tnf == null)
                        {
                            if (s.Contains("removed")) return;
                            tnf = new TNF(this, tnf_id);
                            add_new_tnf = true;
                        }

                        if (s.Contains("removed"))
                        {
                            lock (_tnfs)
                            {
                                _tnfs.Remove(tnf);
                            }

                            if (tnf != null)
                                OnTNFRemoved(tnf);
                        }
                        else
                        {
                            string update = tokens[1].Substring("tnf ".Length + words[1].Length + 1); // tnf <tnf_id>
                            tnf.StatusUpdate(update);
                        }

                        if (add_new_tnf)
                        {
                            AddTNF(tnf);
                        }
                    }
                    break;

                case "transmit":
                    ParseTransmitStatus(tokens[1].Substring("transmit ".Length));
                    break;                

                case "turf":
                    {
                        ParseTurfStatus(tokens[1].Substring("turf ".Length)); // "turf "
                    }
                    break;

                case "xvtr":
                    {
                        // handle minimum words
                        if (words.Length < 3 || words[1] == "")
                        {
                            Debug.WriteLine("ParseStatus: Too few words for xvtr status -- min 3 (" + s + ")");
                            return;
                        }

                        uint index;
                        bool b = uint.TryParse(words[1], out index);
                        if (!b)
                        {
                            Debug.WriteLine("ParseStatus: Invalid xvtr index (" + s + ")");
                            return;
                        }

                        bool add_xvtr = false;
                        Xvtr xvtr = FindXvtrByIndex((int)index);
                        if (xvtr == null)
                        {
                            if (s.Contains("in_use=0"))
                                return;

                            xvtr = new Xvtr(this);
                            xvtr.Index = (int)index;

                            add_xvtr = true;
                        }

                        if (s.Contains("in_use=0"))
                        {
                            xvtr.RadioAck = false;
                            RemoveXvtr(xvtr);
                            return;
                        }

                        string update = tokens[1].Substring("xvtr ".Length + words[1].Length + 1); // "xvtr <num> "
                        
                        xvtr.StatusUpdate(update);

                        if (add_xvtr)
                            AddXvtr(xvtr);
                    }
                    break;
                case "waveform":
                    {
                        ParseWaveformStatus(tokens[1].Substring("waveform ".Length));
                    }
                    break;

                default:
                    Debug.WriteLine("Radio::ParseStatus: Unparsed status (" + s + ")");
                    break;
            }
        }

        private void ParseRadioStatus(string s)
        {
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::ParseRadioStatus: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "callsign":
                        {
                            _callsign = value;
                            RaisePropertyChanged("Callsign");
                        }
                        break;

                    case "full_duplex_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - Speech Full Duplex Enable: Invalid value (" + kv + ")");
                                continue;
                            }

                            _fullDuplexEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("FullDuplexEnabled");
                            break;
                        }

                    case "headphone_gain":
                        {
                            bool b = int.TryParse(value, out _headphoneGain);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus: Invalid headphone_gain value (" + kv + ")");
                                continue;
                            }

                            RaisePropertyChanged("HeadphoneGain");
                        }
                        break;
                    case "headphone_mute":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - headphone_mute: Invalid value (" + kv + ")");
                                continue;
                            }

                            _headphoneMute = Convert.ToBoolean(temp);
                            RaisePropertyChanged("HeadphoneMute");
                        }
                        break;
                    case "lineout_gain":
                        {
                            bool b = int.TryParse(value, out _lineoutGain);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus: Invalid lineout_gain value (" + kv + ")");
                                continue;
                            }

                            RaisePropertyChanged("LineoutGain");
                        }
                        break;
                    case "lineout_mute":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - lineout_mute: Invalid value (" + kv + ")");
                                continue;
                            }

                            _lineoutMute = Convert.ToBoolean(temp);
                            RaisePropertyChanged("LineoutMute");
                        }
                        break;
                    case "nickname":
                        {
                            _nickname = value;
                            RaisePropertyChanged("Nickname");
                        }
                        break;
                    case "panadapters":
                        {
                            bool b = int.TryParse(value, out _panadaptersRemaining);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus: Invalid panadapter value (" + kv + ")");
                                continue;
                            }

                            RaisePropertyChanged("PanadaptersRemaining");
                        }
                        break;
                    case "pll_done":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - pll_done: Invalid value (" + kv + ")");
                                continue;
                            }

                            // enable the PLL Start button again once the pll is done
                            if (Convert.ToBoolean(temp))
                            {
                                _startOffsetEnabled = Convert.ToBoolean(temp);
                                RaisePropertyChanged("StartOffsetEnabled");
                            }
                        }
                        break;
                    case "remote_on_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - remote_on_enabled: Invalid value (" + kv + ")");
                                continue;
                            }

                            _remoteOnEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("RemoteOnEnabled");
                        }
                        break;
                    case "slices":
                        {
                            bool b = int.TryParse(value, out _slicesRemaining);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus: Invalid slice value (" + kv + ")");
                                continue;
                            }

                            RaisePropertyChanged("SlicesRemaining");
                        }
                        break;

                    
                    case "cal_freq":
                        {
                            bool b = StringHelper.DoubleTryParse(value, out _calFreq);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus: Invalid calFreq value (" + kv + ")");
                                continue;
                            }

                            RaisePropertyChanged("CalFreq");
                        }
                        break;
                    case "freq_error_ppb":
                        {
                            bool b = int.TryParse(value, out _freqErrorPPB);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus: Invalid freqErrorPPB value (" + kv + ")");
                                continue;
                            }

                            RaisePropertyChanged("FreqErrorPPB");
                        }
                        break;
                    case "tnf_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - tnf_enabled: Invalid value (" + kv + ")");
                                continue;
                            }

                            _tnfEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TNFEnabled");
                        }
                        break;
                    case "snap_tune_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - snap_tune_enabled: Invalid value (" + kv + ")");
                                continue;
                            }

                            _snapTune = Convert.ToBoolean(temp);
                            RaisePropertyChanged("SnapTune");
                        }
                        break;
                    case "binaural_rx":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - binaural_rx: Invalid value (" + kv + ")");
                                continue;
                            }

                            _binauralRX = Convert.ToBoolean(temp);
                            RaisePropertyChanged("BinauralRX");
                        }
                        break;
                    case "importing":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseRadioStatus - importing: Invalid value (" + kv + ")");
                                continue;
                            }

                            _databaseImportComplete = !Convert.ToBoolean(temp);
                            RaisePropertyChanged("DatabaseImportComplete");
                        }
                        break;

                    case "unity_tests_complete":
                        UnityResultsImportComplete = true;
                        break;
                }
            }
        }

        private void ParseMessage(string s)
        {
            string[] tokens = s.Split('|');
            if (tokens.Length != 2)
            {
                Debug.WriteLine("ParseMessage: Invalid message -- min 2 tokens (" + s + ")");
                return;
            }

            uint num;
            bool b = uint.TryParse(tokens[0].Substring(1), NumberStyles.HexNumber, CultureInfo.InvariantCulture, out num);
            if (!b)
            {
                Debug.WriteLine("ParseMessage: Invalid message number (" + s + ")");
                return;
            }

            MessageSeverity severity = (MessageSeverity)((num >> 24) & 0x3);
            OnMessageReceived(severity, tokens[1]);
        }

        /// <summary>
        /// Delegate event handler for the MessageReceived event
        /// </summary>
        /// <param name="severity">The message severity </param>
        /// <param name="msg">The message being received</param>
        public delegate void MessageReceivedEventHandler(MessageSeverity severity, string msg);
        /// <summary>
        /// This event is raised when the radio receives a message from the client
        /// </summary>
        public event MessageReceivedEventHandler MessageReceived;

        private void OnMessageReceived(MessageSeverity severity, string msg)
        {
            if (MessageReceived != null)
                MessageReceived(severity, msg);
        }

        private void ParseHandle(string s)
        {
            if (s.Length <= 1) return;
            _client_id = s.Substring(1);
            RaisePropertyChanged("ClientID");
        }

        private void ParseProtocolVersion(string s)
        {
            if (s.Length <= 1) return;
            FlexVersion.TryParse(s.Substring(1), out _protocol_version);
            if (_protocol_version < _min_protocol_version || _protocol_version > _max_protocol_version)           
            {
                // OnMessageReceived probably was not the right thing to do here?  Need to revist this
                Debug.WriteLine("*****Protocol not supported!  _protocol_version = 0x" + _protocol_version.ToString("X") + ", _min_protocol_version = " + _min_protocol_version + ", _max_protocol_version 0x = " + _max_protocol_version.ToString("X"));
                OnMessageReceived(MessageSeverity.Fatal, "Protocol Not Supported ("+Util.FlexVersion.ToString(_protocol_version)+")");
            }
        }

        #endregion

        #region Command Routines

        private int _cmdSequenceNumber = 0;
        private int GetNextSeqNum()
        {
            return Interlocked.Increment(ref _cmdSequenceNumber);
        }

        internal int SendCommand(int seq_num, string s)
        {
            if (_tcp_client == null || !_tcp_client.Connected)
            {
                // TODO: handle disconnect
                return 0;
            }

            //Debug.WriteLine("SendCommand: " + seq_num + ": " + s);

            /*if (!connected)
            {
                bool b = Connect();
                if (!b) return 0;
            }*/

            string msg_type = "C";
            if (Verbose) msg_type = "CD";

            string seq = seq_num.ToString();

            if (!s.EndsWith("\n") && !s.EndsWith("\r"))
                s = s + "\n";

            string msg = msg_type + seq + "|" + s;

            byte[] buf = Encoding.UTF8.GetBytes(msg);

#if TIMING
            CmdTime cmd_time = new CmdTime();
            cmd_time.Command = s;
            cmd_time.Sequence = seq_num;
            t1.Stop();
            cmd_time.Start = t1.Duration;

            cmd_time_table.Add(seq_num, cmd_time);
#endif

            try
            {
                _tcp_stream.Write(buf, 0, buf.Length);
            }
            catch (Exception)
            {
                // TODO: handle disconnect
                return 0;
            }

            return seq_num;
        }

        internal int SendCommand(string s)
        {
            return SendCommand(GetNextSeqNum(), s);
        }

        internal int SendReplyCommand(int seq_num, ReplyHandler handler, string s)
        {
            if (_tcp_client == null || !_tcp_client.Connected)
            {
                // TODO: handle disconnect
                return 0;
            }

            // add the sender to the reply queue with the sequence number
            lock(_replyTable)
                _replyTable.Add(seq_num, handler);

            return SendCommand(seq_num, s);
        }

        internal int SendReplyCommand(ReplyHandler handler, string s)
        {
            return SendReplyCommand(GetNextSeqNum(), handler, s);
        }

        #endregion

        #region TNF Routines

        private bool _tnfEnabled;
        public bool TNFEnabled
        {
            get { return _tnfEnabled; }
            set
            {
                if (_tnfEnabled != value)
                {
                    _tnfEnabled = value;
                    SendCommand("radio set tnf_enabled=" + _tnfEnabled);
                    RaisePropertyChanged("TNFEnabled");
                }
            }
        }

        public TNF CreateTNF(uint tnf_id)
        {
            return new TNF(this, tnf_id);
        }

        public TNF CreateTNF()
        {
            return new TNF(this, (uint)(_tnfs.Count + 1));
        }

        public TNF CreateTNF(double freq)
        {
            return new TNF(this, (uint)(_tnfs.Count + 1), freq);
        }

        private TNF FindTNFById(uint id)
        {
            lock (_tnfs)
            {
                foreach (TNF t in _tnfs)
                {
                    if (t.ID == id)
                    {
                        return t;
                    }
                }
            }
            return null;
        }

        internal void AddTNF(TNF tnf)
        {
            lock (_tnfs)
            {
                if (_tnfs.Contains(tnf)) return;
                _tnfs.Add(tnf);
            }
        }

        internal void RemoveTNF(TNF tnf)
        {
            lock (_tnfs)
            {
                if (!_tnfs.Contains(tnf)) return;
                _tnfs.Remove(tnf);
                OnTNFRemoved(tnf);
            }
        }

        public delegate void TNFRemovedEventHandler(TNF tnf);
        public event TNFRemovedEventHandler TNFRemoved;
        private void OnTNFRemoved(TNF tnf)
        {
            if (TNFRemoved != null)
                TNFRemoved(tnf);
        }

        public delegate void TNFAddedEventHandler(TNF tnf);
        public event TNFAddedEventHandler TNFAdded;

        internal void OnTNFAdded(TNF tnf)
        {
            if (TNFAdded != null)
                TNFAdded(tnf);
        }

        public void UpdateTNFFrequency(uint tnf_id, double freq)
        {
            TNF tnf = FindTNFById(tnf_id);
            if (tnf != null)
                tnf.Frequency = freq;
        }

        public void UpdateTNFBandwidth(uint tnf_id, double bw)
        {
            TNF tnf = FindTNFById(tnf_id);
            if (tnf != null)
                tnf.Bandwidth = bw;
        }

        public void UpdateTNFDepth(uint tnf_id, uint depth)
        {
            TNF tnf = FindTNFById(tnf_id);
            if (tnf != null)
                tnf.Depth = depth;
        }

        public void UpdateTNFPemanent(uint tnf_id, bool permanent)
        {
            TNF tnf = FindTNFById(tnf_id);
            if (tnf != null)
                tnf.Permanent = permanent;
        }

        public void RequestTNF(double freq, uint panID)
        {
            if (freq == 0)
            {
                Panadapter pan = FindPanadapterByStreamID(panID);
                if (pan == null)
                    return;

                Slice target_slice = null;
                double freq_diff = 1000;

                lock (_slices)
                {
                    foreach (Slice s in _slices)
                    {
                        if (s.PanadapterStreamID == panID)
                        {
                            double diff = Math.Abs(s.Freq - pan.CenterFreq);
                            if (diff < freq_diff)
                            {
                                freq_diff = diff;
                                target_slice = s;
                            }
                        }
                    }
                }

                if (target_slice == null)
                {
                    freq = pan.CenterFreq;
                }
                else
                {
                    switch(target_slice.DemodMode)
                    {
                        case "LSB":
                        case "DIGL":
                            {
                                freq = target_slice.Freq + (((target_slice.FilterLow - target_slice.FilterHigh) / 2.0) * 1e-6);
                            }
                            break;
                        case "RTTY":
                            {
                                freq = target_slice.Freq - ( target_slice.RTTYShift/2 * 1e-6) ;
                            }
                            break;
                        case "CW":
                        case "AM":
                        case "SAM":
                            {
                                freq = target_slice.Freq + (((target_slice.FilterHigh / 2.0) * 1e-6));
                            } 
                            break;

                        case "USB":
                        case "DIGU":
                        case "FDV":
                        default:
                            {
                                freq = target_slice.Freq + (((target_slice.FilterHigh - target_slice.FilterLow) / 2.0) * 1e-6);
                            }
                            break;
                    }
                }
            }

            SendCommand("tnf create freq=" + StringHelper.DoubleToString(freq, "f6"));
        }

        #endregion

        public void RequestPanafall()
        {
            SendCommand("display panafall create x=100 y=100");
        }

        #region Slice Routines

        /// <summary>
        /// Creates a new slice on the radio
        /// </summary>
        /// <param name="pan">The Panadapter object on which to add the slice</param>
        /// <param name="mode">The demodulation mode of this slice: "USB", "DIGU",
        /// "LSB", "DIGL", "CW", "DSB", "AM", "SAM", "FM"</param>
        /// <returns>The Slice object</returns>
        public Slice CreateSlice(Panadapter pan, string mode)
        {
            return new Slice(this, pan, mode);
        }

        /// <summary>
        /// Creates a new slice on the radio
        /// </summary>
        /// <param name="pan">The Panadapter object on which to add the slice</param>
        /// <param name="mode">The demodulation mode of this slice: "USB", "DIGU",
        /// "LSB", "DIGL", "CW", "DSB", "AM", "SAM", "FM"</param>
        /// <param name="freq">The starging frequency of the slice, in MHz</param>
        /// <returns></returns>
        public Slice CreateSlice(Panadapter pan, string mode, double freq)
        {
            return new Slice(this, pan, mode, freq);
        }

        /// <summary>
        /// Creates a new slice on the radio
        /// </summary>
        /// <param name="freq">The starting frequency of the slice, in MHz</param>
        /// <param name="rx_ant">The starting RX Antenna of the slice.  Us RXAnt
        /// to get list of available antenna ports. "ANT1", "ANT2", "RX_A", 
        /// "RX_B", "XVTR"</param>
        /// <param name="mode">The demodulation mode of this slice: "USB", "DIGU",
        /// "LSB", "DIGL", "CW", "DSB", "AM", "SAM", "FM"</param>
        /// <returns>The Slice object</returns>
        public Slice CreateSlice(double freq, string rx_ant, string mode)
        {
            return new Slice(this, freq, rx_ant, mode);
        }

        /// <summary>
        /// Find a Slice object by index number
        /// </summary>
        /// <param name="index">The index number for the Slice</param>
        /// <returns>The Slice object</returns>
        public Slice FindSliceByIndex(int index)
        {
            lock (_slices)
            {
                foreach (Slice s in _slices)
                {
                    if (s.Index == index)
                        return s;
                }
            }

            return null;
        }

        /// <summary>
        /// Find a Slice by the DAX Channel number
        /// </summary>
        /// <param name="dax_channel">The DAX Channel number of the slice</param>
        /// <returns>The Slice object</returns>
        public Slice FindSliceByDAXChannel(int dax_channel)
        {
            lock (_slices)
            {
                foreach (Slice s in _slices)
                {
                    if (s.DAXChannel == dax_channel)
                        return s;
                }
            }

            return null;
        }

        internal void AddSlice(Slice slc)
        {
            lock (_slices)
            {
                if (_slices.Contains(slc)) return;
                _slices.Add(slc);
                //OnSliceAdded(slc); -- this is now done in the Slice class to ensure that good status info is present before notifying the client
            }
            
            RaisePropertyChanged("SliceList");
        }

        internal void RemoveSlice(Slice slc)
        {
            lock (_slices)
            {
                if (!_slices.Contains(slc)) return;
                _slices.Remove(slc);
                OnSliceRemoved(slc);
            }
            
            RaisePropertyChanged("SliceList");
        }

        internal void RemoveAllSlices()
        {
            lock (_slices)
            {
                while (_slices.Count > 0)
                {
                    Slice slc = _slices[0];
                    _slices.Remove(slc);
                    OnSliceRemoved(slc);
                }
            }
        }

        /// <summary>
        /// Delegate event handler for the SliceRemoved event
        /// </summary>
        /// <param name="slc"></param>
        public delegate void SliceRemovedEventHandler(Slice slc);
        /// <summary>
        /// This event is raised when a Slice is removed from the radio
        /// </summary>
        public event SliceRemovedEventHandler SliceRemoved;

        private void OnSliceRemoved(Slice slc)
        {
            if (SliceRemoved != null)
                SliceRemoved(slc);
        }

        /// <summary>
        /// Delegate event handler for the SlicePanReferenceChange event
        /// </summary>
        /// <param name="slc"></param>
        public delegate void SlicePanReferenceChangeEventHandler(Slice slc);
        /// <summary>
        /// This event is raised when a new Slice has been added to the radio
        /// </summary>
        public event SlicePanReferenceChangeEventHandler SlicePanReferenceChange;

        internal void OnSlicePanReferenceChange(Slice slc)
        {
            if (SlicePanReferenceChange != null)
                SlicePanReferenceChange(slc);
        }

        /// <summary>
        /// Delegate event handler for the SliceAdded event
        /// </summary>
        /// <param name="slc"></param>
        public delegate void SliceAddedEventHandler(Slice slc);
        /// <summary>
        /// This event is raised when a new Slice has been added to the radio
        /// </summary>
        public event SliceAddedEventHandler SliceAdded;

        internal void OnSliceAdded(Slice slc)
        {
            if (SliceAdded != null)
                SliceAdded(slc);
        }

        private Slice _activeSlice;
        public Slice ActiveSlice
        {
            get { return _activeSlice; }
            set
            {
                if (_activeSlice != value)
                {
                    _activeSlice = value;

                    if (_activeSlice != null)
                    {
                        if (!_activeSlice.Active)
                            _activeSlice.Active = true;
                    }

                    RaisePropertyChanged("ActiveSlice");
                }
            }
        }

        private int _slicesRemaining;
        /// <summary>
        /// Gets the number of remaining Slice resources available
        /// </summary>
        public int SlicesRemaining
        {
            get { return _slicesRemaining; }
            
            // It looks like we are not using the setter--
            // the _sliceRemaining gets changed only by 
            // a radio command message.  I am leaving it 
            // but will make it internal. --Abed
            internal set
            {
                if (_slicesRemaining != value)
                {
                    _slicesRemaining = value;
                    RaisePropertyChanged("SlicesRemaining");
                }
            }
        }

        #endregion

        #region Panadapter Routines

        /// <summary>
        /// Creates a new Panadapter
        /// </summary>
        /// <param name="width">The width of the Panadapter in pixels</param>
        /// <param name="height">The height of the Panadapter in pixels</param>
        /// <returns>The Panadapter object</returns>
        public Panadapter CreatePanadapter(int width, int height)
        {
            return new Panadapter(this, width, height);
        }

        internal void RemovePanadapter(uint stream_id, bool sendCommands)
        {
            Panadapter pan = FindPanadapterByStreamID(stream_id);
            if (pan == null) return;
            lock (_panadapters)
            {
                if (_panadapters.Contains(pan))
                {
                    pan.Remove(sendCommands);

                    _panadapters.Remove(pan);
                    OnPanadapterRemoved(pan);
                }
            }
        }

        private void RemovePanadapter(Panadapter pan, bool sendCommands)
        {
            if (pan == null) return;

            lock (_panadapters)
            {
                if (_panadapters.Contains(pan))
                {
                    pan.Remove(sendCommands);

                    _panadapters.Remove(pan);
                    OnPanadapterRemoved(pan);
                }
            }
            RaisePropertyChanged("PanadapterList");
        }

        private void RemoveAllPanadapters()
        {
            lock (_panadapters)
            {
                while (_panadapters.Count > 0)
                {
                    Panadapter pan = _panadapters[0];
                    _panadapters.Remove(pan);
                    OnPanadapterRemoved(pan);
                }
            }
        }

        public delegate void PanadapterAddedEventHandler(Panadapter pan, Waterfall fall);
        public event PanadapterAddedEventHandler PanadapterAdded;

        internal void OnPanadapterAdded(Panadapter pan, Waterfall fall)
        {
            if (PanadapterAdded != null)
                PanadapterAdded(pan, fall);

            lock (_tnfs)
            {
                foreach (TNF t in _tnfs)
                    OnTNFAdded(t);
            }
        }

        /// <summary>
        /// The delegate event handler for the PanadapterRemoved event
        /// </summary>
        /// <param name="pan">The Panadapter object</param>
        public delegate void PanadapterRemovedEventHandler(Panadapter pan);
        /// <summary>
        /// This event is raised when a Panadapter is closed
        /// </summary>
        public event PanadapterRemovedEventHandler PanadapterRemoved;

        private void OnPanadapterRemoved(Panadapter pan)
        {
            if (PanadapterRemoved != null)
                PanadapterRemoved(pan);
        }

        internal void AddPanadapter(Panadapter new_pan)
        {
            Panadapter pan = FindPanadapterByStreamID(new_pan.StreamID);
            if (pan != null)
            {
                Debug.WriteLine("Attempted to Add Panadapter already in Radio _panadapters List");
                return; // already in the list
            }

            lock(_panadapters)
                _panadapters.Add(new_pan);

            lock (_slices)
            {
                foreach (Slice slc in _slices)
                {
                    if (slc.PanadapterStreamID == new_pan.StreamID)
                    {
                        slc.Panadapter = new_pan;
                        if ( slc.RadioAck )
                            OnSlicePanReferenceChange(slc);
                    }
                }
            }

            //OnPanadapterAdded(new_pan); -- this is now done in the Panadapter class after receiving the necessary status info
            RaisePropertyChanged("PanadapterList");
        }

        internal Panadapter FindPanadapterByStreamID(uint stream_id)
        {
            lock (_panadapters)
            {
                foreach (Panadapter pan in _panadapters)
                {
                    if (pan.StreamID == stream_id)
                        return pan;
                }
            }

            return null;
        }

        internal Waterfall FindWaterfallByParentStreamID(uint stream_id)
        {
            lock (_waterfalls)
            {
                foreach (Waterfall fall in _waterfalls)
                {
                    if (fall._parentPanadapterStreamID == stream_id)
                        return fall;
                }
            }

            return null;
        }

        /// <summary>
        /// Finds a Panadapter given its DAX IQ Channel
        /// </summary>
        /// <param name="daxIQChannel">The DAX IQ Channel number</param>
        /// <returns>The Panadapter object </returns>
        public Panadapter FindPanByDAXIQChannel(int daxIQChannel)
        {
            lock (_panadapters)
            {
                foreach (Panadapter pan in _panadapters)
                {
                    if (pan.DAXIQChannel == daxIQChannel)
                        return pan;
                }
            }

            return null;
        }

        private int _panadaptersRemaining;
        /// <summary>
        /// The number of available Panadapter resources remaining
        /// </summary>
        public int PanadaptersRemaining
        {
            get { return _panadaptersRemaining; }
            
            // This is currently only set by a radio command.
            internal set
            {
                if (_panadaptersRemaining != value)
                {
                    _panadaptersRemaining = value;
                    RaisePropertyChanged("PanadaptersRemaining");
                }
            }
        }

        #endregion        

        #region Waterfall Routines

        public Waterfall CreateWaterfall(int width, int height)
        {
            return new Waterfall(this, width, height);
        }

        internal void RemoveWaterfall(uint stream_id, bool sendCommands)
        {
            Waterfall fall = FindWaterfallByStreamID(stream_id);
            if (fall == null) return;
            lock (_waterfalls)
            {
                if (_waterfalls.Contains(fall))
                    fall.Remove(sendCommands);
                _waterfalls.Remove(fall);
            }
        }

        private void RemoveWaterfall(Waterfall fall, bool sendCommands)
        {
            if (fall == null) return;
            lock (_waterfalls)
            {
                if (_waterfalls.Contains(fall))
                    fall.Remove(sendCommands);
                _waterfalls.Remove(fall);
            }
        }

        private void RemoveAllWaterfalls()
        {
            lock (_waterfalls)
            {
                while (_waterfalls.Count > 0)
                {
                    Waterfall fall = _waterfalls[0];
                    _waterfalls.Remove(fall);
                }
            }
        }

        public delegate void WaterfallAddedEventHandler(Waterfall wf);
        public event WaterfallAddedEventHandler WaterfallAdded;

        internal void OnWaterfallAdded(Waterfall fall)
        {
            if (WaterfallAdded != null)
                WaterfallAdded(fall);
        }

        public delegate void WaterfallRemovedEventHandler(Waterfall fall);
        public event WaterfallRemovedEventHandler WaterfallRemoved;

        private void OnWaterfallRemoved(Waterfall fall)
        {
            if (WaterfallRemoved != null)
                WaterfallRemoved(fall);
        }

        internal void AddWaterfall(Waterfall new_fall)
        {
            Waterfall fall = FindWaterfallByStreamID(new_fall.StreamID);
            if (fall != null)
            {
                Debug.WriteLine("Attempted to Add Waterfall already in Radio _waterfalls List");
                return; // already in the list
            }

            lock (_waterfalls)
            {
                _waterfalls.Add(new_fall);
            }

            /*foreach (Slice slc in _slices)
            {
                if (slc.PanadapterStreamID == new_pan._stream_id)
                    slc.Panadapter = new_pan;
            }*/

            //OnWaterfallAdded(new_wf); -- this is now done in the Waterfall class after receiving the necessary status info
        }

        internal Waterfall FindWaterfallByStreamID(uint stream_id)
        {
            lock (_waterfalls)
            {
                foreach (Waterfall fall in _waterfalls)
                {
                    if (fall.StreamID == stream_id)
                        return fall;
                }
            }

            return null;
        }

        public Waterfall FindWaterfallByDAXIQChannel(int daxIQChannel)
        {
            lock (_waterfalls)
            {
                foreach (Waterfall fall in _waterfalls)
                {
                    if (fall.DAXIQChannel == daxIQChannel)
                        return fall;
                }
            }

            return null;
        }

        private int _waterfallsRemaining;
        public int WaterfallsRemaining
        {
            get { return _waterfallsRemaining; }
            set
            {
                if (_waterfallsRemaining != value)
                {
                    _waterfallsRemaining = value;
                    RaisePropertyChanged("WaterfallsRemaining");
                }
            }
        }

        #endregion

        #region MICAudioStream Routines

        public delegate void MICAudioStreamRemovedEventHandler(MICAudioStream mic_audio_stream);
        public event MICAudioStreamRemovedEventHandler MICAudioStreamRemoved;

        private void OnMICAudioStreamRemoved(MICAudioStream mic_audio_stream)
        {
            if (MICAudioStreamRemoved != null)
                MICAudioStreamRemoved(mic_audio_stream);
        }

        public delegate void MICAudioStreamAddedEventHandler(MICAudioStream mic_audio_stream);
        public event MICAudioStreamAddedEventHandler MICAudioStreamAdded;
        internal void OnMICAudioStreamAdded(MICAudioStream mic_audio_stream)
        {
            if (MICAudioStreamAdded != null)
                MICAudioStreamAdded(mic_audio_stream);
        }

        internal MICAudioStream FindMICAudioStreamByStreamID(uint stream_id)
        {
            lock (_micAudioStreams)
            {
                foreach (MICAudioStream mic_audio_stream in _micAudioStreams)
                {
                    if (mic_audio_stream.RXStreamID == stream_id)
                        return mic_audio_stream;
                }
            }

            return null;
        }

        public MICAudioStream CreateMICAudioStream()
        {
            return new MICAudioStream(this);
        }

        internal void AddMICAudioStream(MICAudioStream new_mic_audio_stream)
        {

            MICAudioStream mic_audio_stream = FindMICAudioStreamByStreamID(new_mic_audio_stream.RXStreamID);
            if (mic_audio_stream != null)
            {
                Debug.WriteLine("Attempted to Add MICAudioStream already in Radio _micAudioStreams List");
                return; // already in the list
            }

            lock (_micAudioStreams)
                _micAudioStreams.Add(new_mic_audio_stream);
        }

        public void RemoveMICAudioStream(uint stream_id)
        {
            MICAudioStream mic_audio_stream = FindMICAudioStreamByStreamID(stream_id);
            if (mic_audio_stream == null) return;

            lock (_micAudioStreams)
                _micAudioStreams.Remove(mic_audio_stream);

            OnMICAudioStreamRemoved(mic_audio_stream);
        }

        #endregion

        #region TXAudioStream Routines
        
        public delegate void TXAudioStreamRemovedEventHandler(TXAudioStream tx_audio_stream);
        public event TXAudioStreamRemovedEventHandler TXAudioStreamRemoved;

        private void OnTXAudioStreamRemoved(TXAudioStream tx_audio_stream)
        {
            if (TXAudioStreamRemoved != null)
                TXAudioStreamRemoved(tx_audio_stream);
        }

        public delegate void TXAudioStreamAddedEventHandler(TXAudioStream tx_audio_stream);
        public event TXAudioStreamAddedEventHandler TXAudioStreamAdded;
        internal void OnTXAudioStreamAdded(TXAudioStream tx_audio_stream)
        {
            if (TXAudioStreamAdded != null)
                TXAudioStreamAdded(tx_audio_stream);
        }

         internal TXAudioStream FindTXAudioStreamByStreamID(uint stream_id)
        {
            lock (_txAudioStreams)
            {
                foreach (TXAudioStream tx_audio_stream in _txAudioStreams)
                {
                    if (tx_audio_stream.TXStreamID == stream_id)
                        return tx_audio_stream;
                }
            }

            return null;
        }

        public TXAudioStream CreateTXAudioStream()
        {
            return new TXAudioStream(this);
        }

        internal void AddTXAudioStream(TXAudioStream new_tx_audio_stream)
        {
            
           TXAudioStream tx_audio_stream = FindTXAudioStreamByStreamID(new_tx_audio_stream.TXStreamID);
            if (tx_audio_stream != null)
            {
                Debug.WriteLine("Attempted to Add TXAudioStream already in Radio _txAudioStreams List");
                return; // already in the list
            }

            lock (_txAudioStreams)
                _txAudioStreams.Add(new_tx_audio_stream);           
       }

        public void RemoveTXAudioStream(uint stream_id)
        {
            TXAudioStream tx_audio_stream = FindTXAudioStreamByStreamID(stream_id);
            if (tx_audio_stream == null) return;
                        
            lock (_txAudioStreams)
                _txAudioStreams.Remove(tx_audio_stream);

            OnTXAudioStreamRemoved(tx_audio_stream);
        }

        #endregion

        #region AudioStream Routines

        /// <summary>
        /// Creates a new DAX Audio Stream
        /// </summary>
        /// <param name="daxChannel">The DAX Channel number</param>
        /// <returns>The DAX AudioStream object</returns>
        public AudioStream CreateAudioStream(int daxChannel)
        {
            return new AudioStream(this, daxChannel);
        }

        /// <summary>
        /// Removes a DAX Audio Stream
        /// </summary>
        /// <param name="daxChannel">The DAX Channel number</param>
        public void RemoveAudioStream(uint stream_id)
        {
            AudioStream audio_stream = FindAudioStreamByStreamID(stream_id);
            if (audio_stream == null) return;
                        
            lock (_audioStreams)
                _audioStreams.Remove(audio_stream);

            OnAudioStreamRemoved(audio_stream);
        }

        /// <summary>
        /// The delegate event handler for the AudioStreamAdded event
        /// </summary>
        /// <param name="audio_stream">The DAX AudioStream object</param>
        public delegate void AudioStreamAddedEventHandler(AudioStream audio_stream);

        /// <summary>
        /// This event is reaised when a new DAX Audio Stream is added
        /// </summary>
        public event AudioStreamAddedEventHandler AudioStreamAdded;

        internal void OnAudioStreamAdded(AudioStream audio_stream)
        {
            if (AudioStreamAdded != null)
                AudioStreamAdded(audio_stream);
        }

        /// <summary>
        /// The delegate event handler for the AudioStreamRemoved event
        /// </summary>
        /// <param name="audio_stream">The DAX AudioStream object</param>
        public delegate void AudioStreamRemovedEventHandler(AudioStream audio_stream);
        /// <summary>
        /// This event is raised when a DAX Audio Stream has been removed
        /// </summary>
        public event AudioStreamRemovedEventHandler AudioStreamRemoved;

        private void OnAudioStreamRemoved(AudioStream audio_stream)
        {
            if (AudioStreamRemoved != null)
                AudioStreamRemoved(audio_stream);
        }

        internal void AddAudioStream(AudioStream new_audio_stream)
        {
            
            AudioStream audio_stream = FindAudioStreamByStreamID(new_audio_stream.RXStreamID);
            if (audio_stream != null)
            {
                Debug.WriteLine("Attempted to Add AudioStream already in Radio _audioStreams List");
                return; // already in the list
            }

            lock (_audioStreams)
                _audioStreams.Add(new_audio_stream);           

            //foreach (Slice slc in _slices)
            //{
            //    if (slc.PanadapterStreamID == new_pan._stream_id)
            //        slc.Panadapter = new_pan;
            //}

            //OnAudioStreamAdded(new_audio_stream); -- this is now done in the AudioStream class to ensure good status info is present before notifying the client
        }

        internal void SetAudioStreamSlice(int daxChannel, Slice slc)
        {
            AudioStream audio_stream = FindAudioStreamByDAXChannel(daxChannel);
            if (audio_stream == null) return;

            audio_stream.Slice = slc;
        }

        internal void ClearAudioStreamSlice(int daxChannel, Slice slc)
        {
            AudioStream audio_stream = FindAudioStreamByDAXChannel(daxChannel);
            if (audio_stream == null) return;

            if (audio_stream.Slice == null) return;

            if (audio_stream.Slice == slc)
                audio_stream.Slice = null;
        }

        internal AudioStream FindAudioStreamByStreamID(uint stream_id)
        {
            lock (_audioStreams)
            {
                foreach (AudioStream audio_stream in _audioStreams)
                {
                    if (audio_stream.RXStreamID == stream_id)
                        return audio_stream;
                }
            }

            return null;
        }

        internal AudioStream FindAudioStreamByDAXChannel(int daxChannel)
        {
            lock (_audioStreams)
            {
                foreach (AudioStream audio_stream in _audioStreams)
                {
                    if (audio_stream.DAXChannel == daxChannel && audio_stream.Mine)
                        return audio_stream;
                }
            }

            return null;
        }        

        #endregion

        #region OpusStream Routines

        /// <summary>
        /// Creates a new Opus Audio Stream
        /// </summary>
        /// <returns>The OpusStream object</returns>
        public OpusStream CreateOpusStream()
        {
            return new OpusStream(this);
        }

        /// <summary>
        /// Removes an Opus Audio Stream
        /// </summary>
        public void RemoveOpusStream()
        {
            OpusStream opus_stream = FindOpusStream();
            if (opus_stream == null) return;

            opus_stream.Remove();
            lock(_opusStreams)
                _opusStreams.Remove(opus_stream);

            OnOpusStreamRemoved(opus_stream);
        }

        /// <summary>
        /// The delegate event handler for the OpusStreamAdded event
        /// </summary>
        /// <param name="audio_stream">The OpusStream object</param>
        public delegate void OpusStreamAddedEventHandler(OpusStream opus_stream);

        /// <summary>
        /// This event is reaised when a new Opus Audio Stream is added
        /// </summary>
        public event OpusStreamAddedEventHandler OpusStreamAdded;

        internal void OnOpusStreamAdded(OpusStream opus_stream)
        {
            if (OpusStreamAdded != null)
                OpusStreamAdded(opus_stream);
        }

        /// <summary>
        /// The delegate event handler for the OpusStreamRemoved event
        /// </summary>
        /// <param name="audio_stream">The OpusStream object</param>
        public delegate void OpusStreamRemovedEventHandler(OpusStream opus_stream);
        /// <summary>
        /// This event is raised when an Opus Stream has been removed
        /// </summary>
        public event OpusStreamRemovedEventHandler OpusStreamRemoved;

        private void OnOpusStreamRemoved(OpusStream opus_stream)
        {
            if (OpusStreamRemoved != null)
                OpusStreamRemoved(opus_stream);
        }


        public void AddOpusStream(OpusStream new_opus_stream)
        {
            OpusStream opus_stream = FindOpusStreamByStreamID(new_opus_stream.RXStreamID);
            if (opus_stream != null)
            {
                Debug.WriteLine("Attempted to Add OpusStream already in Radio _audioStreams List");
                return; // already in the list
            }

            lock(_opusStreams)
                _opusStreams.Add(new_opus_stream);
        }

        internal OpusStream FindOpusStreamByStreamID(uint stream_id)
        {
            lock (_opusStreams)
            {
                foreach (OpusStream opus_stream in _opusStreams)
                {
                    if (opus_stream.RXStreamID == stream_id)
                        return opus_stream;
                }
            }

            return null;
        }

        internal OpusStream FindOpusStream()
        {
            lock (_opusStreams)
            {
                if (_opusStreams != null && _opusStreams.Count > 0 && _opusStreams[0] != null)
                {
                    return _opusStreams[0];
                }
                else
                {
                    Debug.WriteLine("FindOpusStream():: Opus Stream not found");
                    return null;
                }
            }
        }

        #endregion

        #region IQStream Routines

        /// <summary>
        /// Creates a new DAX IQ stream
        /// </summary>
        /// <param name="daxIQChannel">The DAX IQ Channel number</param>
        /// <returns>The DAX IQStream object</returns>
        public IQStream CreateIQStream(int daxIQChannel)
        {
            return new IQStream(this, daxIQChannel);
        }

        /// <summary>
        /// Finds a DAX IQ Stream by its Stream ID
        /// </summary>
        /// <param name="stream_id">The StreamID of the DAX IQ Stream</param>
        public void RemoveIQStream(uint stream_id)
        {
            IQStream iq_stream = FindIQStreamByStreamID(stream_id);
            if (iq_stream == null) return;

            lock(_iqStreams)
                _iqStreams.Remove(iq_stream);

            OnIQStreamRemoved(iq_stream); // good find EHR
        }

        /// <summary>
        /// The delegate event handler for the IQStreamAdded event
        /// </summary>
        /// <param name="iq_stream"></param>
        public delegate void IQStreamAddedEventHandler(IQStream iq_stream);
        /// <summary>
        /// This event is raised when a new DAX IQ Stream is added
        /// </summary>
        public event IQStreamAddedEventHandler IQStreamAdded;

        internal void OnIQStreamAdded(IQStream iq_stream)
        {
            if (IQStreamAdded != null)
                IQStreamAdded(iq_stream);
        }

        /// <summary>
        /// The delegate event handler for the IQStreamRemoved event
        /// </summary>
        /// <param name="iq_stream">The DAX IQStream object</param>
        public delegate void IQStreamRemovedEventHandler(IQStream iq_stream);
        /// <summary>
        ///  This event is raised when a DAX IQ Stream is removed
        /// </summary>
        public event IQStreamRemovedEventHandler IQStreamRemoved;

        private void OnIQStreamRemoved(IQStream iq_stream)
        {
            if (IQStreamRemoved != null)
                IQStreamRemoved(iq_stream);
        }

        internal void AddIQStream(IQStream new_iq_stream)
        {
            IQStream iq_stream = FindIQStreamByStreamID(new_iq_stream.StreamID);
            if (iq_stream != null)
            {
                Debug.WriteLine("Attempted to Add IQStream already in Radio _iqStreams List");
                return; // already in the list
            }
           
            // Add the new stream to the IQ Streams list
            lock(_iqStreams)
                _iqStreams.Add(new_iq_stream);

            // Update the Pandapter link within the stream based on the IQ Channel
            new_iq_stream.Pan = FindPanByDAXIQChannel(new_iq_stream.DAXIQChannel);
           
            //OnIQStreamAdded(new_iq_stream); -- this is now done in the IQStream class to ensure full info before notifying the clients
        }

        internal void SetIQStreamPan(int daxIQChannel, Panadapter pan)
        {
            IQStream iq_stream = FindIQStreamByDAXIQChannel(daxIQChannel);
            if (iq_stream == null) return;

            iq_stream.Pan = pan;
        }

        internal void ClearIQStreamPan(int daxIQChannel, Panadapter pan)
        {
            IQStream iq_stream = FindIQStreamByDAXIQChannel(daxIQChannel);
            if (iq_stream == null) return;

            if (iq_stream.Pan == null) return;

            if (iq_stream.Pan == pan)
                iq_stream.Pan = null;
        }

        internal IQStream FindIQStreamByStreamID(uint stream_id)
        {
            lock (_iqStreams)
            {
                foreach (IQStream iq_stream in _iqStreams)
                {
                    if (iq_stream.StreamID == stream_id)
                        return iq_stream;
                }
            }

            return null;
        }

        public IQStream FindIQStreamByDAXIQChannel(int daxIQChannel)
        {
            lock (_iqStreams)
            {
                foreach (IQStream iq_stream in _iqStreams)
                {
                    if (iq_stream.DAXIQChannel == daxIQChannel && iq_stream.Mine)
                        return iq_stream;
                }
            }

            return null;
        }

        #endregion

        #region Antenna Routines

        private void GetRXAntennaList()
        {
            SendReplyCommand(new ReplyHandler(GetRXAntennaListReply), "ant list");
        }

        private void GetRXAntennaListReply(int seq, uint resp_val, string reply)
        {
            if (resp_val != 0) return;

            _rx_ant_list = reply.Split(',');
            RaisePropertyChanged("RXAntList");
        }

        #endregion

        #region Meter Routines

        private void GetMeterList()
        {
            SendReplyCommand(new ReplyHandler(GetMeterListReply), "meter list");
        }

        private void GetMeterListReply(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;
            ParseMeterStatus(s);
        }

        private void ParseMeterStatus(string status)
        {
            if (status == "") return;

            Meter m = null;

            // check for removal first
            if (status.Contains("removed"))
            {
                string[] words = status.Split(' ');
                if (words.Length < 2 || words[0] == "")
                {
                    Debug.WriteLine("ParseMeterStatus: Invalid removal status -- min 2 tokens (" + status + ")");
                    return;
                }

                int meter_index;
                bool b = int.TryParse(words[0], out meter_index);

                if (!b)
                {
                    Debug.WriteLine("ParseMeterStatus: Error parsing meter index in removal status (" + status + ")");
                    return;
                }

                m = FindMeterByIndex(meter_index);
                if (m == null) return;

                if (m.Source == "SLC")
                {
                    // get a hold of the slice in order to remove the meter from its meter list
                    Slice slc = FindSliceByIndex(m.SourceIndex);
                    if (slc != null)
                        slc.RemoveMeter(m);
                }

                RemoveMeter(m);
                return;
            }

            bool new_meter = false;

            // not a removal, do normal parsing
            string[] reply_tokens = status.Split('#');

            foreach (string s in reply_tokens)
            {
                if (s == "") break;

                // break down the message into thae 3 components: index, key, and value
                // message is typically in the format index.key=value
                // we can use the '.' and '=' to find the edges of each token
                int meter_index;
                int start = 0;
                int len = s.IndexOf(".");
                if (len < 0)
                {
                    Debug.WriteLine("Error in Meter List Reply: Expected '.', but found none (" + s + ")");
                    continue;
                }

                bool b = int.TryParse(s.Substring(start, len), out meter_index);

                if (!b)
                {
                    Debug.WriteLine("Error in Meter List Reply: Invalid Index (" + s.Substring(start, len) + ")");
                    continue;
                }

                // parse the key from the string
                start = len + 1;
                int eq_index = s.IndexOf("=");
                if (eq_index < 0)
                {
                    Debug.WriteLine("Error in Meter List Reply: Expected '=', but found none (" + s + ")");
                    continue;
                }

                len = eq_index - len - 1;
                string key = s.Substring(start, len);

                // parse the value from the string
                string value = s.Substring(eq_index + 1); // everything after the '='


                // check to see whether we have a meter object for this index
                m = FindMeterByIndex(meter_index);
                if (m == null) // if not, create one
                {
                    m = new Meter(this, meter_index);
                    lock (_meters)
                    {
                        _meters.Add(m);
                    }
                    new_meter = true;
                }

                // depending on what the key is, parse the next value appropriately
                switch (key)
                {
                    case "src":
                        {
                            if (m.Source != value)
                                m.Source = value;
                        }
                        break;
                    case "num":
                        {
                            int source_index;
                            b = int.TryParse(value, out source_index);
                            if (!b)
                            {
                                Debug.WriteLine("Error in Meter List Reply: Invalid Source Index (" + value + ")");
                                continue;
                            }

                            if (m.SourceIndex != source_index)
                                m.SourceIndex = source_index;
                        }
                        break;
                    case "nam":
                        {
                            if (m.Name != value)
                                m.Name = value;
                        }
                        break;
                    case "low":
                        {
                            double low;
                            b = StringHelper.DoubleTryParse(value, out low);
                            if (!b)
                            {
                                Debug.WriteLine("Error in Meter List Reply: Invalid Low (" + value + ")");
                                continue;
                            }

                            if (m.Low != low)
                                m.Low = low;
                        }
                        break;
                    case "hi":
                        {
                            double high;
                            b = StringHelper.DoubleTryParse(value, out high);
                            if (!b)
                            {
                                Debug.WriteLine("Error in Meter List Reply: Invalid High (" + value + ")");
                                continue;
                            }

                            if (m.High != high)
                                m.High = high;
                        }
                        break;
                    case "desc":
                        {
                            if (m.Description != value)
                                m.Description = value;
                        }
                        break;
                    case "unit":
                        {
                            MeterUnits units = MeterUnits.None;
                            switch (value)
                            {
                                case "dBm": units = MeterUnits.Dbm; break;
                                case "dBFS": units = MeterUnits.Dbfs; break;
                                case "Volts": units = MeterUnits.Volts; break;
                                case "amps": units = MeterUnits.Amps; break;
                                case "degC": units = MeterUnits.Degrees; break;
                                case "SWR": units = MeterUnits.SWR; break;
                            }

                            if (m.Units != units)
                                m.Units = units;
                        }
                        break;
                }
            }

            if (m != null && m.Source == "SLC")
            {
                Slice slc = FindSliceByIndex(m.SourceIndex);
                if (slc != null)
                {
                    if (slc.FindMeterByIndex(m.Index) == null)
                        slc.AddMeter(m);
                }
            }

            if (m != null && new_meter)
                AddMeter(m);
        }

        private Meter FindMeterByIndex(int index)
        {
            lock (_meters)
            {
                foreach (Meter m in _meters)
                {
                    if (m.Index == index)
                        return m;
                }
            }

            return null;
        }

        /// <summary>
        /// Gets a Meter by its name
        /// </summary>
        /// <param name="s">The meter name</param>
        /// <returns>The found Meter object</returns>
        public Meter FindMeterByName(string s)
        {
            lock (_meters)
            {
                foreach (Meter m in _meters)
                {
                    if (m.Name == s)
                        return m;
                }
            }

            return null;
        }

        private void AddMeter(Meter m)
        {
            lock (_meters)
            {
                if (!_meters.Contains(m))
                {
                    _meters.Add(m);
                }
            }

            if (m.Name == "FWDPWR")
                m.DataReady += new Meter.DataReadyEventHandler(FWDPW_DataReady);
            else if (m.Name == "REFPWR")
                m.DataReady += new Meter.DataReadyEventHandler(REFPW_DataReady);
            else if (m.Name == "SWR")
                m.DataReady += new Meter.DataReadyEventHandler(SWR_DataReady);
            else if (m.Name == "PATEMP")
                m.DataReady += new Meter.DataReadyEventHandler(PATEMP_DataReady);
            else if (m.Name == "MIC")
                m.DataReady += new Meter.DataReadyEventHandler(MIC_DataReady);
            else if (m.Name == "MICPEAK")
                m.DataReady += new Meter.DataReadyEventHandler(MICPeak_DataReady);
            else if (m.Name == "COMPPEAK")
                m.DataReady += new Meter.DataReadyEventHandler(COMPPeak_DataReady);
            else if (m.Name == "HWALC")
                m.DataReady += new Meter.DataReadyEventHandler(HWAlc_DataReady);
            else if (m.Name == "+13.8A") // A: before the fuse
                m.DataReady += new Meter.DataReadyEventHandler(Volts_DataReady);
        }

        private void RemoveMeter(Meter m)
        {
            lock (_meters)
            {
                if (_meters.Contains(m))
                {
                    _meters.Remove(m);

                    if (m.Name == "FWDPWR")
                        m.DataReady -= FWDPW_DataReady;
                    else if (m.Name == "REFPWR")
                        m.DataReady -= REFPW_DataReady;
                    else if (m.Name == "SWR")
                        m.DataReady -= SWR_DataReady;
                    else if (m.Name == "PATEMP")
                        m.DataReady -= PATEMP_DataReady;
                    else if (m.Name == "MIC")
                        m.DataReady -= MIC_DataReady;
                    else if (m.Name == "MICPEAK")
                        m.DataReady -= MICPeak_DataReady;
                    else if (m.Name == "COMPPEAK")
                        m.DataReady -= Volts_DataReady;
                    else if (m.Name == "HWALC")
                        m.DataReady -= HWAlc_DataReady;
                    else if (m.Name == "+13.8A") // A: Before the fuse
                        m.DataReady -= Volts_DataReady;
                }
            }
        }

        private void FWDPW_DataReady(Meter meter, float data)
        {
            OnForwardPowerDataReady(data);
        }

        private void REFPW_DataReady(Meter meter, float data)
        {
            OnReflectedPowerDataReady(data);
        }

        void SWR_DataReady(Meter meter, float data)
        {
            OnSWRDataReady(data);
        }

        void PATEMP_DataReady(Meter meter, float data)
        {
            OnPATempDataReady(data);
        }

        void MIC_DataReady(Meter meter, float data)
        {
            OnMicDataReady(data);
        }

        void MICPeak_DataReady(Meter meter, float data)
        {
            OnMicPeakDataReady(data);
        }

        void COMPPeak_DataReady(Meter meter, float data)
        {
            OnCompPeakDataReady(data);
        }

        void HWAlc_DataReady(Meter meter, float data)
        {
            OnHWAlcDataReady(data);  //abed change
        }

        void Volts_DataReady(Meter meter, float data)
        {
            OnVoltsDataReady(data);
        }

        /// <summary>
        /// The delegate event handler for meter data events.
        /// Used with the events: ForwardPowerDataReady, ReflectedPowerDataReady,
        /// SWRDataReady, PATempDataReady, VoltsDataReady, MicDataReady, MicPeakDataReady,
        /// CompPeakDataReady, HWAlcDataReady, etc.
        /// </summary>
        /// <param name="data">The forward RF power meter value</param>
        public delegate void MeterDataReadyEventHandler(float data);
        /// <summary>
        /// This event is raised when there is new meter data for the forward RF power.
        /// Data units are in dBm.
        /// </summary>
        public event MeterDataReadyEventHandler ForwardPowerDataReady;
        private void OnForwardPowerDataReady(float data)
        {
            if (ForwardPowerDataReady != null)
                ForwardPowerDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for the reflected RF power.
        /// Data units are in dBm.
        /// </summary>
        public event MeterDataReadyEventHandler ReflectedPowerDataReady;
        private void OnReflectedPowerDataReady(float data)
        {
            if (ReflectedPowerDataReady != null)
                ReflectedPowerDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for the SWR.
        /// Data units are in VSWR.
        /// </summary>
        public event MeterDataReadyEventHandler SWRDataReady;
        private void OnSWRDataReady(float data)
        {
            //Debug.WriteLine("SWR: " + data.ToString("f2"));
            if (SWRDataReady != null)
                SWRDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for the PA
        /// Temperature.  Data units are in degrees Celsius.
        /// </summary>
        public event MeterDataReadyEventHandler PATempDataReady;
        private void OnPATempDataReady(float data)
        {
            if (PATempDataReady != null)
                PATempDataReady(data);
        }

        public event MeterDataReadyEventHandler VoltsDataReady;
        private void OnVoltsDataReady(float data)
        {
            if (VoltsDataReady != null)
                VoltsDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for
        /// the Mic input level (use MicPeakDataReady for peak levels).
        /// </summary>
        public event MeterDataReadyEventHandler MicDataReady;
        private void OnMicDataReady(float data)
        {
            if (MicDataReady != null)
                MicDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for
        /// the Mic peak level.
        /// </summary>
        public event MeterDataReadyEventHandler MicPeakDataReady;
        private void OnMicPeakDataReady(float data)
        {
            if (MicPeakDataReady != null)
                MicPeakDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for
        /// the input Compression.  Data is in units of reduction
        /// in dB.
        /// </summary>
        public event MeterDataReadyEventHandler CompPeakDataReady;
        private void OnCompPeakDataReady(float data)
        {
            if (CompPeakDataReady != null)
                CompPeakDataReady(data);
        }

        /// <summary>
        /// This event is raised when there is new meter data for the Hardware ALC
        /// input to the radio.  The data is in units of Volts.
        /// </summary>
        public event MeterDataReadyEventHandler HWAlcDataReady;
        private void OnHWAlcDataReady(float data)
        {
            if (HWAlcDataReady != null)
                HWAlcDataReady(data);
        }

        #endregion

        #region Version Routines

        private void GetVersions()
        {
            SendReplyCommand(new ReplyHandler(UpdateVersions), "version");
        }

        private void UpdateVersions(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            _versions = s;
            string[] vers = s.Split('#');
            UInt64 temp;
            bool b;


            foreach (string kv in vers)
            {
                string key, value;
                string[] tokens = kv.Split('=');

                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::UpdateVersions - Invalid token (" + kv + ")");
                    continue;
                }

                key = tokens[0];
                value = tokens[1];

                b = FlexVersion.TryParse(value, out temp);
                if (!b)
                {
                    Debug.WriteLine("Radio::UpdateVersions -- Invalid value (" + value + ")");
                    continue;
                }

                switch (key)
                {
                    case "PSoC-MBTRX":
                        {
                            _trxPsocVersion = temp;
                            RaisePropertyChanged("TRXPsocVersion");
                        }
                        break;
                    case "PSoC-MBPA100":
                        {
                            _paPsocVersion = temp;
                            RaisePropertyChanged("PAPsocVersion");
                        }
                        break;
                    case "FPGA-MB":
                        {
                            _fpgaVersion = temp;
                            RaisePropertyChanged("FPGAVersion");
                        }
                        break;
                }
            }

            RaisePropertyChanged("Versions");
        }

        private UInt64 _fpgaVersion;
        public UInt64 FPGAVersion
        {
            get { return _fpgaVersion; }
        }

        private UInt64 _paPsocVersion;
        public UInt64 PAPsocVersion
        {
            get { return _paPsocVersion; }
        }

        private UInt64 _trxPsocVersion;
        public UInt64 TRXPsocVersion
        {
            get { return _trxPsocVersion; }
        }

        #endregion

        #region Info Routines

        private void GetInfo()
        {
            SendReplyCommand(new ReplyHandler(UpdateInfo), "info");
        }

        private void UpdateInfo(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            string[] vers = s.Split(',');

            foreach (string kv in vers)
            {
                string key, value;
                string[] tokens = kv.Split('=');

                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::UpdateInfo - Invalid token (" + kv + ")");
                    continue;
                }

                key = tokens[0];
                value = tokens[1].Trim('\\', '"');

                switch (key)
                {
                    case "atu_present":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::UpdateInfo - invalid value (" + key + "=" + value + ")");
                                continue;
                            }

                            _atuPresent = Convert.ToBoolean(temp);
                            RaisePropertyChanged("ATUPresent");
                        }
                        break;

                    case "callsign":
                        {
                            _callsign = value;
                            RaisePropertyChanged("Callsign");
                        }
                        break;

                    case "gps":
                        {
                            GPSInstalled = (value != "Not Present");
                        }
                        break;

                    case "name":
                        {
                            _nickname = value;
                            RaisePropertyChanged("Nickname");
                        }
                        break;

                    case "num_tx":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::UpdateInfo - tx: Error - invalid value (" + value + ")");
                                continue;
                            }

                            _num_tx = temp;
                            RaisePropertyChanged("NumTX");
                        }
                        break;

                    case "options":
                        _radioOptions = value;
                        RaisePropertyChanged("RadioOptions");
                        break;
                    
                    case "region":
                        {
                            RegionCode = value;
                        }
                        break;

                    case "screensaver":
                        {
                            ScreensaverMode mode = ParseScreensaverMode(value);
                            if (mode == ScreensaverMode.None) continue;
                            
                            _screensaver = mode;
                            RaisePropertyChanged("Screensaver");
                        }
                        break;

                    case "netmask":
                        {
                            IPAddress temp;
                            bool b = IPAddress.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::UpdateInfo - subnet: Error - invalid value (" + value + ")");
                                continue;
                            }

                            _subnetMask = temp;
                            RaisePropertyChanged("SubnetMask");
                        }
                        break;
                }
            }
        }

        private ScreensaverMode ParseScreensaverMode(string s)
        {
            ScreensaverMode mode = ScreensaverMode.None;

            switch (s)
            {
                case "model": mode = ScreensaverMode.Model; break;
                case "name": mode = ScreensaverMode.Name; break;
                case "callsign": mode = ScreensaverMode.Callsign; break;
            }

            return mode;
        }

        private string ScreensaverModeToString(ScreensaverMode mode)
        {
            string s = "";

            switch (mode)
            {
                case ScreensaverMode.Model: s = "model"; break;
                case ScreensaverMode.Name: s = "name"; break;
                case ScreensaverMode.Callsign: s = "callsign"; break;
            }

            return s;
        }

        private ScreensaverMode _screensaver;
        /// <summary>
        /// Sets the screensaver mode to be shown on the front display of
        /// the radio (Model, Name, Callsign, None).
        /// </summary>
        public ScreensaverMode Screensaver
        {
            get { return _screensaver; }
            set
            {
                if (_screensaver != value)
                {
                    _screensaver = value;
                    SendCommand("radio screensaver " + ScreensaverModeToString(_screensaver));
                    RaisePropertyChanged("Screensaver");
                }
            }
        }

        private string _callsign;
        /// <summary>
        /// The Callsign string to be stored in the radio to be shown
        /// on the front display if the Callsign ScreensaverMode is
        /// selected with the Screensaver property.
        /// </summary>
        public string Callsign
        {
            get { return _callsign; }
            set
            {
                if (_callsign != value.ToUpper())
                {
                    _callsign = value.ToUpper();
                    SendCommand("radio callsign " + _callsign);
                    RaisePropertyChanged("Callsign");
                }
            }
        }

        private string _nickname;
        /// <summary>
        /// The Nickname string to be stored in the radio to be shown
        /// on the front display if the Name ScreensaverMode is
        /// selected with the Screensaver property.
        /// </summary>
        public string Nickname
        {
            get { return _nickname; }
            set
            {
                if (_nickname != value)
                {
                    _nickname = value;
                    SendCommand("radio name " + _nickname);
                    RaisePropertyChanged("Nickname");
                }
            }
        }

        private int _num_tx = 0;
        /// <summary>
        /// The number of transmitters that are currently available and 
        /// enabled on the radio.  Typically NumTX will be 0 if there are
        /// no transmitters enabled and will be 1 if there is a slice that is
        /// set to TX.
        /// </summary>
        public int NumTX
        {
            get { return _num_tx; }
        }

        #endregion

        #region Interlock Routines

        private int _interlockTimeout; // in ms
        /// <summary>
        /// The timeout period for the transmitter to be keyed continuously, in milliseconds.
        /// If set to 120000, the transmitter can be keyed for 2 
        /// minutes continuously before begin unkeyed automatically.
        /// </summary>
        public int InterlockTimeout
        {
            get { return _interlockTimeout; }
            set
            {
                if (_interlockTimeout != value)
                {
                    _interlockTimeout = value;
                    SendCommand("interlock timeout=" + _interlockTimeout);
                    RaisePropertyChanged("InterlockTimeout");
                }
            }
        }

        private bool _txreqRCAEnabled;
        /// <summary>
        /// Enables or disables the Transmit Request functionality
        /// via the Accessory input on the back panel of the radio.
        /// </summary>
        public bool TXReqRCAEnabled
        {
            get { return _txreqRCAEnabled; }
            set
            {
                if (_txreqRCAEnabled != value)
                {
                    _txreqRCAEnabled = value;
                    SendCommand("interlock rca_txreq_enable=" + Convert.ToByte(_txreqRCAEnabled));
                    //RaisePropertyChanged("TXReqRCAEnabled");
                }
            }
        }

        private bool _txreqACCEnabled;
        /// <summary>
        /// Enables or disables the Transmit Request (TX REQ) RCA 
        /// input on the back panel of the radio.
        /// </summary>
        public bool TXReqACCEnabled
        {
            get { return _txreqACCEnabled; }
            set
            {
                if (_txreqACCEnabled != value)
                {
                    _txreqACCEnabled = value;
                    SendCommand("interlock acc_txreq_enable=" + Convert.ToByte(_txreqACCEnabled));
                    RaisePropertyChanged("TXReqACCEnabled");
                }
            }
        }

        private bool _txreqRCAPolarity;
        /// <summary>
        /// The polartiy of the Transmit Request (TX REQ) RCA input on the back panel
        /// of the radio.  When true, TX REQ is active high.
        /// When false, TX REQ is active low. The RCA port must be enabled by 
        /// the TXReqRCAEnabled property.
        /// </summary>
        public bool TXReqRCAPolarity
        {
            get { return _txreqRCAPolarity; }
            set
            {
                if (_txreqRCAPolarity != value)
                {
                    _txreqRCAPolarity = value;
                    SendCommand("interlock rca_txreq_polarity=" + Convert.ToByte(_txreqRCAPolarity));
                    //RaisePropertyChanged("TXReqRCAPolarity");
                }
            }
        }

        private bool _txreqACCPolarity;
        /// <summary>
        /// The polartiy of the Transmit Request input via the Accessory port 
        /// on the back panel of the radio.  When true, TX REQ is active high.
        /// When false, TX REQ is active low. TX REQ functionality via the
        /// Accessory port must be enabled by the TXReqACCEnabled property.
        /// </summary>
        public bool TXReqACCPolarity
        {
            get { return _txreqACCPolarity; }
            set
            {
                if (_txreqACCPolarity != value)
                {
                    _txreqACCPolarity = value;
                    SendCommand("interlock acc_txreq_polarity=" + Convert.ToByte(_txreqACCPolarity));
                    //RaisePropertyChanged("TXReqACCPolarity");
                }
            }
        }

        private InterlockState _interlockState = InterlockState.Ready;
        /// <summary>
        /// Gets the Interlock State of the transmitter: None, Receive,
        /// NotReady, PTTRequested, Transmitting, TXFault, Timeout,
        /// StuckInput
        /// </summary>
        public InterlockState InterlockState
        {
            get { return _interlockState; }
            internal set
            {
                if (_interlockState != value)
                {
                    _interlockState = value;
                    RaisePropertyChanged("InterlockState");

                    // do we need to update the Mox property??
                    if (_interlockState == InterlockState.Transmitting)
                    {
                        if (_mox != true)
                        {
                            _mox = true;
                            RaisePropertyChanged("Mox");
                        }
                    }
                    else if(_interlockState != InterlockState.PTTRequested) // don't undo MOX if we are waiting on TX Req
                    {
                        if (_mox != false)
                        {
                            _mox = false;
                            RaisePropertyChanged("Mox");
                        }
                    }
                }
            }
        }

        private PTTSource _pttSource;
        /// <summary>
        /// Gets the current push to talk (PTT) source of the radio:
        /// SW, Mic, ACC, RCA.
        /// </summary>
        public PTTSource PTTSource
        {
            get { return _pttSource; }
            internal set
            {
                if (_pttSource != value)
                {
                    _pttSource = value;
                    RaisePropertyChanged("PTTSource");
                }
            }
        }

        private InterlockReason _interlockReason;
        /// <summary>
        /// Gets the radio's reasoning for the current InterlockState
        /// </summary>
        public InterlockReason InterlockReason
        {
            get { return _interlockReason; }
            internal set
            {
                if (_interlockReason != value)
                {
                    _interlockReason = value;
                    RaisePropertyChanged("InterlockReason");

                    if (_interlockReason == InterlockReason.CLIENT_TX_INHIBIT)
                    {
                        _txInhibit = true;
                        RaisePropertyChanged("TXInhibit");
                    }
                }
            }
        }

        private int _delayTX;
        /// <summary>
        /// The delay duration between keying the radio and transmit in milliseconds
        /// </summary>
        public int DelayTX
        {
            get { return _delayTX; }
            set
            {
                if (_delayTX != value)
                {
                    _delayTX = value;
                    SendCommand("interlock tx_delay=" + _delayTX);
                    RaisePropertyChanged("DelayTX");
                }
            }
        }

        private bool _tx1Enabled;
        /// <summary>
        /// Enables the TX1 Transmit Relay RCA output port on the back panel of the radio
        /// </summary>
        public bool TX1Enabled
        {
            get { return _tx1Enabled; }
            set
            {
                if (_tx1Enabled != value)
                {
                    _tx1Enabled = value;
                    SendCommand("interlock tx1_enabled=" + Convert.ToByte(_tx1Enabled));
                    RaisePropertyChanged("TX1Enabled");
                }
            }
        }

        private bool _tx2Enabled;
        /// <summary>
        /// Enables the TX2 Transmit Relay RCA output port on the back panel of the radio
        /// </summary>
        public bool TX2Enabled
        {
            get { return _tx2Enabled; }
            set
            {
                if (_tx2Enabled != value)
                {
                    _tx2Enabled = value;
                    SendCommand("interlock tx2_enabled=" + Convert.ToByte(_tx2Enabled));
                    RaisePropertyChanged("TX2Enabled");
                }
            }
        }

        private bool _tx3Enabled;
        /// <summary>
        /// Enables the TX3 Transmit Relay RCA output port on the back panel of the radio
        /// </summary>
        public bool TX3Enabled
        {
            get { return _tx3Enabled; }
            set
            {
                if (_tx3Enabled != value)
                {
                    _tx3Enabled = value;
                    SendCommand("interlock tx3_enabled=" + Convert.ToByte(_tx3Enabled));
                    RaisePropertyChanged("TX3Enabled");
                }
            }
        }

        private bool _txACCEnabled;
        /// <summary>
        /// Enables the Transmit Relay output via the Accessory port
        /// on the back panel of the radio
        /// </summary>
        public bool TXACCEnabled
        {
            get { return _txACCEnabled; }
            set
            {
                if (_txACCEnabled != value)
                {
                    _txACCEnabled = value;
                    SendCommand("interlock acc_tx_enabled=" + Convert.ToByte(_txACCEnabled));
                    RaisePropertyChanged("TXACCEnabled");
                }
            }
        }

        private int _tx1Delay;
        /// <summary>
        /// The delay in milliseconds (ms) for the TX1 RCA output relay.  This
        /// port must be enabled by setting the TX1Enabled property
        /// </summary>
        public int TX1Delay
        {
            get { return _tx1Delay; }
            set
            {
                if (_tx1Delay != value)
                {
                    _tx1Delay = value;
                    SendCommand("interlock tx1_delay=" + _tx1Delay);
                    RaisePropertyChanged("TX1Delay");
                }
            }
        }

        private int _tx2Delay;
        /// <summary>
        /// The delay in milliseconds (ms) for the TX2 RCA output relay.  This
        /// port must be enabled by setting the TX2Enabled property
        /// </summary>
        public int TX2Delay
        {
            get { return _tx2Delay; }
            set
            {
                if (_tx2Delay != value)
                {
                    _tx2Delay = value;
                    SendCommand("interlock tx2_delay=" + _tx2Delay);
                    RaisePropertyChanged("TX2Delay");
                }
            }
        }

        private int _tx3Delay;
        /// <summary>
        /// The delay in milliseconds (ms) for the TX3 RCA output relay.  This
        /// port must be enabled by setting the TX3Enabled property
        /// </summary>
        public int TX3Delay
        {
            get { return _tx3Delay; }
            set
            {
                if (_tx3Delay != value)
                {
                    _tx3Delay = value;
                    SendCommand("interlock tx3_delay=" + _tx3Delay);
                    RaisePropertyChanged("TX3Delay");
                }
            }
        }

        private int _txACCDelay;
        /// <summary>
        /// The delay in milliseconds (ms) for the Tranmist Relay output pin via the
        /// Accessory port on the back panel of the radio.  This
        /// pin must be enabled by setting the TXACCEnabled property
        /// </summary>
        public int TXACCDelay
        {
            get { return _txACCDelay; }
            set
            {
                if (_txACCDelay != value)
                {
                    _txACCDelay = value;
                    SendCommand("interlock acc_tx_delay=" + _txACCDelay);
                    RaisePropertyChanged("TXACCDelay");
                }
            }
        }

        private bool _remoteOnEnabled;
        /// <summary>
        /// Enables the remote on "REM ON" RCA input port on the back 
        /// panel of the radio.
        /// </summary>
        public bool RemoteOnEnabled
        {
            get { return _remoteOnEnabled; }
            set
            {
                if (_remoteOnEnabled != value)
                {
                    _remoteOnEnabled = value;
                    SendCommand("radio set remote_on_enabled=" + Convert.ToByte(_remoteOnEnabled));
                    RaisePropertyChanged("RemoteOnEnabled");
                }
            }
        }

        private InterlockReason ParseInterlockReason(string s)
        {
            InterlockReason reason = InterlockReason.None;

            switch (s)
            {
                case "RCA_TXREQ": reason = InterlockReason.RCA_TXREQ; break;
                case "ACC_TXREQ": reason = InterlockReason.ACC_TXREQ; break;
                case "BAD_MODE": reason = InterlockReason.BAD_MODE; break;
                case "TUNED_TOO_FAR": reason = InterlockReason.TUNED_TOO_FAR; break;
                case "OUT_OF_BAND": reason = InterlockReason.OUT_OF_BAND; break;
                case "PA_RANGE": reason = InterlockReason.PA_RANGE; break;
                case "CLIENT_TX_INHIBIT": reason = InterlockReason.CLIENT_TX_INHIBIT; break;
                case "XVTR_RX_ONLY": reason = InterlockReason.XVTR_RX_ONLY; break;
            }

            return reason;
        }

        private InterlockState ParseInterlockState(string s)
        {
            InterlockState state = InterlockState.None;
            switch (s)
            {
                case "RECEIVE": state = InterlockState.Receive; break;
                case "READY": state = InterlockState.Ready; break;
                case "NOT_READY": state = InterlockState.NotReady; break;
                case "PTT_REQUESTED": state = InterlockState.PTTRequested; break;
                case "TRANSMITTING": state = InterlockState.Transmitting; break;
                case "TX_FAULT": state = InterlockState.TXFault; break;
                case "TIMEOUT": state = InterlockState.Timeout; break;
                case "STUCK_INPUT": state = InterlockState.StuckInput; break;
            }

            return state;
        }

        private ATUTuneStatus ParseATUTuneStatus(string s)
        {
            ATUTuneStatus status = ATUTuneStatus.None;
            switch (s)
            {
                case "NONE": status = ATUTuneStatus.None; break;
                case "TUNE_NOT_STARTED": status = ATUTuneStatus.NotStarted; break;
                case "TUNE_IN_PROGRESS": status = ATUTuneStatus.InProgress; break;
                case "TUNE_BYPASS": status = ATUTuneStatus.Bypass; break;
                case "TUNE_SUCCESSFUL": status = ATUTuneStatus.Successful; break;
                case "TUNE_OK": status = ATUTuneStatus.OK; break;
                case "TUNE_FAIL_BYPASS": status = ATUTuneStatus.FailBypass; break;
                case "TUNE_FAIL": status = ATUTuneStatus.Fail; break;
                case "TUNE_ABORTED": status = ATUTuneStatus.Aborted; break;
                case "TUNE_MANUAL_BYPASS": status = ATUTuneStatus.ManualBypass; break;
            }

            return status;
        }

        private PTTSource ParsePTTSource(string s)
        {
            PTTSource source = PTTSource.None;

            switch (s)
            {
                case "SW": source = PTTSource.SW; break;
                case "MIC": source = PTTSource.Mic; break;
                case "ACC": source = PTTSource.ACC; break;
                case "RCA": source = PTTSource.RCA; break;
            }

            return source;
        }

        private void ParseInterlockStatus(string s)
        {
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::ParseInterlockStatus: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "tx_allowed":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - tx_allowed: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txAallowed = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXAllowed");
                            break;
                        }

                    case "state":
                        {
                            InterlockState state = ParseInterlockState(value);
                            if (state == InterlockState.None)
                            {
                                Debug.WriteLine("ParseInterlockStatus: Error - Invalid state (" + value + ")");
                                continue;
                            }

                            InterlockState = state;
                        }
                        break;

                    case "source":
                        {
                            PTTSource source = ParsePTTSource(value);
                            if (source == PTTSource.None)
                            {
                                Debug.WriteLine("ParseInterlockStatus: Error - Invalid PTT Source (" + value + ")");
                                continue;
                            }

                            PTTSource = source;
                        }
                        break;

                    case "reason":
                        {
                            InterlockReason reason = ParseInterlockReason(value);
                            if (reason == InterlockReason.None)
                            {
                                Debug.WriteLine("ParseInterlockStatus: Error - Invalid reason (" + value + ")");
                                continue;
                            }

                            InterlockReason = reason;
                        }
                        break;

                    case "timeout":
                        {
                            uint timeout;
                            bool b = uint.TryParse(value, out timeout);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus: Inavlid timeout value (" + value + ")");
                                continue;
                            }

                            _interlockTimeout = (int)timeout;
                            RaisePropertyChanged("InterlockTimeout");
                        }
                        break;

                    case "acc_txreq_enable":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - acc_txreq_enable: Invalid value (" + value + ")");
                                continue;
                            }

                            _txreqACCEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXReqACCEnabled");
                        }
                        break;

                    case "rca_txreq_enable":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - rca_txreq_enable: Invalid value (" + value + ")");
                                continue;
                            }

                            _txreqRCAEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXReqRCAEnabled");
                        }
                        break;

                    case "acc_txreq_polarity":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - acc_txreq_polarity: Invalid value (" + value + ")");
                                continue;
                            }

                            _txreqACCPolarity = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXReqACCPolarity");
                        }
                        break;

                    case "rca_txreq_polarity":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - rca_txreq_polarity: Invalid value (" + value + ")");
                                continue;
                            }

                            _txreqRCAPolarity = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXReqRCAPolarity");
                        }
                        break;

                    case "tx1_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b || temp > 1)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx1_enabled: Invalid value (" + value + ")");
                                continue;
                            }

                            _tx1Enabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TX1Enabled");
                        }
                        break;

                    case "tx2_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b || temp > 1)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx2_enabled: Invalid value (" + value + ")");
                                continue;
                            }

                            _tx2Enabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TX2Enabled");
                        }
                        break;

                    case "tx3_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b || temp > 1)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx3_enabled: Invalid value (" + value + ")");
                                continue;
                            }

                            _tx3Enabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TX3Enabled");
                        }
                        break;

                    case "acc_tx_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);
                            if (!b || temp > 1)
                            {
                                Debug.WriteLine("ParseInterlockStatus - acc_tx_enabled: Invalid value (" + value + ")");
                                continue;
                            }

                            _txACCEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXACCEnabled");
                        }
                        break;

                    case "tx1_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx1_delay: Invalid value (" + value + ")");
                                continue;
                            }

                            _tx1Delay = temp;
                            RaisePropertyChanged("TX1Delay");
                        }
                        break;

                    case "tx2_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx2_delay: Invalid value (" + value + ")");
                                continue;
                            }

                            _tx2Delay = temp;
                            RaisePropertyChanged("TX2Delay");
                        }
                        break;

                    case "tx3_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx3_delay: Invalid value (" + value + ")");
                                continue;
                            }

                            _tx3Delay = temp;
                            RaisePropertyChanged("TX3Delay");
                        }
                        break;

                    case "acc_tx_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - acc_tx_delay: Invalid value (" + value + ")");
                                continue;
                            }

                            _txACCDelay = temp;
                            RaisePropertyChanged("TXACCDelay");
                        }
                        break;

                    case "tx_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("ParseInterlockStatus - tx_delay: Invalid value (" + value + ")");
                                continue;
                            }

                            _delayTX = temp;
                            RaisePropertyChanged("DelayTX");
                        }
                        break;
                }
            }
        }

        #endregion

        #region Transmit Routines

        private int _maxPowerLevel;
        /// <summary>
        /// The maximum power level (in Watts) that the radio will transmit when using the PA
        /// </summary>
        public int MaxPowerLevel
        {
            get { return _maxPowerLevel; }
            set
            {
                int new_power = value;

                // check limits
                if (new_power < 0) new_power = 0;
                if (new_power > 100) new_power = 100;

                if (_maxPowerLevel != new_power)
                {
                    _maxPowerLevel = new_power;
                    SendCommand("transmit set max_power_level=" + _maxPowerLevel);
                    RaisePropertyChanged("MaxPowerLevel");
                }
                else if (new_power != value)
                {
                    RaisePropertyChanged("MaxPowerLevel");
                }
            }
        }


        private int _rfPower;
        /// <summary>
        /// The transmit RF power level in Watts, from 0 to 100.
        /// </summary>
        public int RFPower
        {
            get { return _rfPower; }
            set
            {
                int new_power = value;

                // check limits
                if (new_power < 0) new_power = 0;
                if (new_power > 100) new_power = 100;

                if (_rfPower != new_power)
                {
                    _rfPower = new_power;
                    SendCommand("transmit set rfpower=" + _rfPower);
                    RaisePropertyChanged("RFPower");
                }
                else if (new_power != value)
                {
                    RaisePropertyChanged("RFPower");
                }
            }
        }

        private int _tunePower;
        /// <summary>
        /// The transmit RF power level for Tune in Watts, from 0 to 100
        /// </summary>
        public int TunePower
        {
            get { return _tunePower; }
            set
            {
                int new_power = value;

                // check limits
                if (new_power < 0) new_power = 0;
                if (new_power > 100) new_power = 100;
               
                if (_tunePower != new_power)
                {
                    _tunePower = new_power;
                    SendCommand("transmit set tunepower=" + _tunePower);
                    RaisePropertyChanged("TunePower");
                }
                else if (new_power != value)
                {
                    RaisePropertyChanged("TunePower");
                }
            }
        }

        private int _amCarrierLevel;
        /// <summary>
        /// The AM Carrier level in Watts, from 0 to 100
        /// </summary>
        public int AMCarrierLevel
        {
            get { return _amCarrierLevel; }
            set
            {
                int new_level = value;

                // check limits
                if (new_level < 0) new_level = 0;
                if (new_level > 100) new_level = 100;

                if (_amCarrierLevel != new_level)
                {
                    _amCarrierLevel = new_level;
                    SendCommand("transmit set am_carrier=" + _amCarrierLevel);
                    RaisePropertyChanged("AMCarrierLevel");
                }
                else if (new_level != value)
                {
                    RaisePropertyChanged("AMCarrierLevel");
                }
            }
        }


        public void SaveTXProfile(string profile_name)
        {
            if (profile_name != null && profile_name != "")
            {
                SendCommand("profile transmit save \"" + profile_name.Replace("*","") + "\"");
            }
        }

        public void DeleteMICProfile(string profile_name)
        {
            if (profile_name != null && profile_name != "")
            {
                SendCommand("profile mic delete \"" + profile_name.Replace("*","") + "\"");
            }
        }

        public void SaveMICProfile(string profile_name)
        {
            if (profile_name != null && profile_name != "")
            {
                SendCommand("profile mic save \"" + profile_name.Replace("*", "") + "\"");
            }
        }

        public void DeleteTXProfile(string profile_name)
        {
            if (profile_name != null && profile_name != "")
            {
                SendCommand("profile transmit delete \"" + profile_name.Replace("*", "") + "\"");
            }
        }

        public void SaveGlobalProfile(string profile_name)
        {
            if (profile_name != null && profile_name != "")
            {
                SendCommand("profile global save \"" + profile_name + "\"");
            }
        }

        public void DeleteGlobalProfile(string profile_name)
        {
            if (profile_name != null && profile_name != "")
            {
                SendCommand("profile global delete \"" + profile_name + "\"");
            }
        }

        public void UninstallWaveform(string waveform_name)
        {
            if (waveform_name != null && waveform_name != "")
            {
                SendCommand("waveform uninstall " + waveform_name);
            }
        }


        private ObservableCollection<string> _profileMICList;
        public ObservableCollection<string> ProfileMICList
        {
            get { return _profileMICList; }

        }

        private string _profileMICSelection;
        public string ProfileMICSelection
        {
            get { return _profileMICSelection; }
            set
            {
                if (_profileMICSelection != value)
                {
                    _profileMICSelection = value;
                    if (_profileMICSelection != null &&
                        _profileMICSelection != "")
                    {
                        SendCommand("profile mic load \"" + _profileMICSelection + "\"");
                        RaisePropertyChanged("ProfileMICSelection");
                    }
                }
            }
        }

        private ObservableCollection<string> _profileTXList;
        public ObservableCollection<string> ProfileTXList
        {
            get { return _profileTXList; }

        }

        private string _profileTXSelection;
        public string ProfileTXSelection
        {
            get { return _profileTXSelection; }
            set
            {
                _profileTXSelection = value;
                if (_profileTXSelection != null &&
                    _profileTXSelection != "")
                {
                    _profileTXSelection = _profileTXSelection.Replace("*","");
                    SendCommand("profile tx load \"" + _profileTXSelection + "\"");
                    RaisePropertyChanged("ProfileTXSelection");
                }
            }
        }

        private ObservableCollection<string> _profileDisplayList;
        public ObservableCollection<string> ProfileDisplayList
        {
            get { return _profileDisplayList; }

        }

        private string _profileDisplaySelection;
        public string ProfileDisplaySelection
        {
            get { return _profileDisplaySelection; }
            set
            {
                if (_profileDisplaySelection != value)
                {
                    _profileDisplaySelection = value;
                    if (_profileDisplaySelection != null &&
                        _profileDisplaySelection != "")
                    {
                        SendCommand("profile display load \"" + _profileDisplaySelection + "\"");
                        RaisePropertyChanged("ProfileDisplaySelection");
                    }
                }
            }
        }


        private ObservableCollection<string> _profileGlobalList;
        public ObservableCollection<string> ProfileGlobalList
        {
            get { return _profileGlobalList; }
        }

        private string _profileGlobalSelection;
        public string ProfileGlobalSelection
        {
            get { return _profileGlobalSelection; }
            set
            {
                _profileGlobalSelection = value;
                if (_profileGlobalSelection != null &&
                    _profileGlobalSelection != "")
                {
                    SendCommand("profile global load \"" + _profileGlobalSelection + "\"");
                    RaisePropertyChanged("ProfileGlobalSelection");
                }
            }
        }

        private void UpdateProfileMicList(string s)
        {
            string[] inputs = s.Split('^');

            _profileMICList = new ObservableCollection<string>();
            foreach (string profile in inputs)
            {
                if (profile != "")
                {
                    _profileMICList.Add(profile);
                }
            }
            RaisePropertyChanged("ProfileMICList");
        }

        private void UpdateProfileTxList(string s)
        {
            string[] inputs = s.Split('^');

            _profileTXList = new ObservableCollection<string>();
            foreach (string profile in inputs)
            {
                if (profile != "")
                {
                    _profileTXList.Add(profile);
                }
            }
            RaisePropertyChanged("ProfileTXList");
        }

        private void UpdateProfileDisplayList(string s)
        {
            string[] inputs = s.Split('^');

            _profileDisplayList = new ObservableCollection<string>();
            foreach (string profile in inputs)
            {
                if (profile != "")
                {
                    _profileDisplayList.Add(profile);
                }
            }
            RaisePropertyChanged("ProfileDisplayList");
        }

        private void UpdateProfileGlobalList(string s)
        {
            string[] inputs = s.Split('^');

            _profileGlobalList = new ObservableCollection<string>();
            foreach (string profile in inputs)
            {
                if (profile != "")
                {
                    _profileGlobalList.Add(profile);
                }
            }

            RaisePropertyChanged("ProfileGlobalList");
        }

        private void GetProfileLists()
        {
            SendCommand("profile global info");
            SendCommand("profile tx info");
            SendCommand("profile mic info");
            SendCommand("profile display info");
        }

        private void GetMicList()
        {
            SendReplyCommand(new ReplyHandler(UpdateMicList), "mic list");
        }

        private void UpdateMicList(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;
            if (_micInputList == null) return;

            string[] inputs = s.Split(',');

            _micInputList = new List<string>();
            foreach (string mic in inputs)
                _micInputList.Add(mic);

            RaisePropertyChanged("MicInputList");
        }

        private List<string> _micInputList;
        /// <summary>
        /// A list of the available mic inputs
        /// </summary>
        public List<string> MicInputList
        {
            get { return _micInputList; }

            /*set
            internal set
            {
                _micInputList = value;
                RaisePropertyChanged("MicInputList");
            }*/
        }

        private string _micInput;
        /// <summary>
        /// The currently selected mic input
        /// </summary>
        public string MicInput
        {
            get { return _micInput; }
            set
            {
                if (_micInput != value)
                {
                    _micInput = value;
                    SendCommand("mic input " + _micInput);
                    RaisePropertyChanged("MicInput");
                }
            }
        }

        public void MonitorNetworkQuality()
        {
            Thread t = new Thread(new ParameterizedThreadStart(Private_MonitorNetworkQuality));
            t.Name = "Monitor Network Quality Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;
            t.Start();
        }

        private enum NetworkIndicatorState
        {
            STATE_OFF,
            STATE_EXCELLENT,
            STATE_VERY_GOOD,
            STATE_GOOD,
            STATE_FAIR,
            STATE_POOR
        }

        private void Private_MonitorNetworkQuality(object obj)
        {           
            Ping pingSender = new Ping();
            int packetErrorCount = 0;
            int lastPacketErrorCount = 0;
            IPAddress addressToPing = _ip; // IPAddress.Parse("8.8.8.8");
            PingReply reply = null;
            bool packet_lost = false;
            NetworkIndicatorState currentState = NetworkIndicatorState.STATE_EXCELLENT;
            NetworkIndicatorState nextState = NetworkIndicatorState.STATE_EXCELLENT;
            int state_countdown = 0;


            while (true)
            {
                reply = null;

                try
                {
                    reply = pingSender.Send(addressToPing);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine("Private_MonitorNetworkQuality :: " + ex.ToString());
                }

                if (reply != null && reply.Status == IPStatus.Success)
                {
                    //Console.WriteLine("Address: {0}", reply.Address.ToString());
                    //Console.WriteLine("RoundTrip time: {0}", reply.RoundtripTime);
                    //Console.WriteLine("Time to live: {0}", reply.Options.Ttl);
                    //Console.WriteLine("Don't fragment: {0}", reply.Options.DontFragment);
                    //Console.WriteLine("Buffer size: {0}", reply.Buffer.Length);
                    //Console.WriteLine("---");

                    NetworkPing = (int)reply.RoundtripTime;

                    

                    lastPacketErrorCount = packetErrorCount;

                    packetErrorCount = _meterPacketErrorCount;
                    if (_opusStreams.Count > 0)
                    {
                        packetErrorCount += _opusStreams[0].ErrorCount;
                    }

                    lock (_panadapters)
                    {
                        foreach (Panadapter p in _panadapters)
                        {
                            packetErrorCount += p.FFTPacketErrorCount;
                        }
                    }

                    lock (_waterfalls)
                    {
                        foreach (Waterfall w in _waterfalls)
                        {
                            packetErrorCount += w.FallPacketErrorCount;
                        }
                    }

                    if (packetErrorCount > lastPacketErrorCount)
                    {
                        packet_lost = true;
                    }
                    else
                    {
                        packet_lost = false;
                    }

                    // order of operations is:
                    // 1. Check to see if we need to move down in state
                    // 2. Check to see if we can move up (countdown == 0).
                    // 3. If yes, set the countdown and the state
                    // 4. If not, decrement state_countdown

                    switch (currentState)
                    {
                        case NetworkIndicatorState.STATE_EXCELLENT:
                            // down
                            if (_networkPing >= NETWORK_PING_POOR_THRESHOLD_MS)
                                nextState = NetworkIndicatorState.STATE_POOR;
                            else if (_networkPing >= NETWORK_PING_FAIR_THRESHOLD_MS &&
                                _networkPing < NETWORK_PING_POOR_THRESHOLD_MS)
                                nextState = NetworkIndicatorState.STATE_GOOD;
                            else if (packet_lost)
                                nextState = NetworkIndicatorState.STATE_VERY_GOOD;
                            break;
                        
                        case NetworkIndicatorState.STATE_VERY_GOOD:
                            // down
                            if (_networkPing >= NETWORK_PING_POOR_THRESHOLD_MS)
                            {
                                nextState = NetworkIndicatorState.STATE_POOR;
                                state_countdown = 5;
                            }
                            else if (_networkPing >= NETWORK_PING_FAIR_THRESHOLD_MS &&
                                _networkPing < NETWORK_PING_POOR_THRESHOLD_MS ||
                                packet_lost)
                            {
                                nextState = NetworkIndicatorState.STATE_GOOD;
                                state_countdown = 5;
                            }
                            else // up
                            {
                                if (state_countdown-- == 0)
                                {
                                    nextState = NetworkIndicatorState.STATE_EXCELLENT;
                                    state_countdown = 5;
                                }
                            }
                            break;

                        case NetworkIndicatorState.STATE_GOOD:
                            // down
                            if (_networkPing >= NETWORK_PING_POOR_THRESHOLD_MS)
                            {
                                nextState = NetworkIndicatorState.STATE_POOR;
                                state_countdown = 5;
                            }
                            else if (packet_lost)
                            {
                                nextState = NetworkIndicatorState.STATE_FAIR;
                                state_countdown = 5;
                            }
                            else if (_networkPing < NETWORK_PING_FAIR_THRESHOLD_MS)
                            {
                                if (state_countdown-- == 0)
                                {
                                    nextState = NetworkIndicatorState.STATE_VERY_GOOD;
                                    state_countdown = 5;
                                }
                            }
                            break;                        

                        case NetworkIndicatorState.STATE_FAIR:
                            if (_networkPing >= NETWORK_PING_POOR_THRESHOLD_MS ||
                                packet_lost)
                            {
                                nextState = NetworkIndicatorState.STATE_POOR;
                                state_countdown = 5;
                            }
                            else
                            {
                                if (state_countdown-- == 0)
                                {
                                    nextState = NetworkIndicatorState.STATE_GOOD;
                                    state_countdown = 5;
                                }
                            }
                            break;

                        case NetworkIndicatorState.STATE_POOR:
                            if (_networkPing < NETWORK_PING_POOR_THRESHOLD_MS)
                            {
                                if (state_countdown-- == 0)
                                {
                                    nextState = NetworkIndicatorState.STATE_FAIR;
                                    state_countdown = 5;
                                }
                            }
                            break;
                        
                        case NetworkIndicatorState.STATE_OFF:
                            nextState = NetworkIndicatorState.STATE_POOR;
                            state_countdown = 5;
                            break;
                    }

                    //Debug.WriteLine("Network Indicator State: " + currentState.ToString() + " --> " + nextState.ToString());

                    switch (nextState)
                    {
                        case NetworkIndicatorState.STATE_EXCELLENT:
                            _remoteNetworkQuality = NetworkQuality.EXCELLENT;
                            break;
                        case NetworkIndicatorState.STATE_VERY_GOOD:
                            _remoteNetworkQuality = NetworkQuality.VERYGOOD;
                            break;
                        case NetworkIndicatorState.STATE_GOOD:
                            _remoteNetworkQuality = NetworkQuality.GOOD;
                            break;
                        case NetworkIndicatorState.STATE_FAIR:
                            _remoteNetworkQuality = NetworkQuality.FAIR;
                            break;
                        case NetworkIndicatorState.STATE_POOR:
                            _remoteNetworkQuality = NetworkQuality.POOR;
                            break;
                    }

                    currentState = nextState;
                }
                else
                {
                    if (reply != null)
                    {
                        Debug.WriteLine(reply.Status);
                    }
                    _remoteNetworkQuality = NetworkQuality.OFF;
                    currentState = NetworkIndicatorState.STATE_OFF;
                    NetworkPing = -1;
                }                

                RaisePropertyChanged("RemoteNetworkQuality");
                Thread.Sleep(1000);
            }
        }

        private NetworkQuality _remoteNetworkQuality = NetworkQuality.OFF;
        /// <summary>
        /// Gets quality of the network between the client and the radio
        /// </summary>
        public NetworkQuality RemoteNetworkQuality
        {
            get { return _remoteNetworkQuality; }
            internal set
            {
                if (_remoteNetworkQuality == value)
                    return;

                _remoteNetworkQuality = value;
                RaisePropertyChanged("RemoteNetworkQuality");
            }
        }

        private int _networkPing = -1;
        /// <summary>
        /// Gets the round-trip time (ping time) between the client and
        /// radio in milliseconds.
        /// </summary>
        public int NetworkPing
        {
            get { return _networkPing; }
            internal set
            {
                if (_networkPing == value)
                    return;

                _networkPing = value;
                RaisePropertyChanged("NetworkPing");
            }
        }

        private bool _remoteTxOn;
        public bool RemoteTxOn
        {
            get { return _remoteTxOn; }
            internal set
            {
                if (_remoteTxOn != value)
                {
                    _remoteTxOn = value;


                    // No need to send a command to the radio.  The radio knows when to turn
                    // on remote transmit based on the selected mic

                    //_radio.SendCommand("remote_audio tx_on " + Convert.ToByte(_remoteTxOn));

                    RaisePropertyChanged("RemoteTxOn");
                }
            }
        }

        private int _micLevel;
        /// <summary>
        /// The currently selected mic level from 0 to 100
        /// </summary>
        public int MicLevel
        {
            get { return _micLevel; }
            set
            {
                int new_level = value;

                // check limits
                if (new_level < 0) new_level = 0;
                if (new_level > 100) new_level = 100;                

                if (_micLevel != new_level)
                {
                    _micLevel = new_level;
                    SendCommand("transmit set miclevel=" + _micLevel);
                    RaisePropertyChanged("MicLevel");
                }
                else if (new_level != value)
                {
                    RaisePropertyChanged("MicLevel");
                }
            }
        }

        private bool _micBias;
        /// <summary>
        /// Enables (true) or disables (false) the mic bias
        /// </summary>
        public bool MicBias
        {
            get { return _micBias; }
            set
            {
                if (_micBias != value)
                {
                    _micBias = value;
                    SendCommand("mic bias " + Convert.ToByte(_micBias));
                    RaisePropertyChanged("MicBias");
                }
            }
        }

        private bool _micBoost;
        /// <summary>
        /// Enables (true) or disables (false) the +20 dB mic boost
        /// </summary>
        public bool MicBoost
        {
            get { return _micBoost; }
            set
            {
                if (_micBoost != value)
                {
                    _micBoost = value;
                    SendCommand("mic boost " + Convert.ToByte(_micBoost));
                    RaisePropertyChanged("MicBoost");
                }
            }
        }

        private bool _txFilterChangesAllowed;
        /// <summary>
        /// Gets whether transmit filter widths are allowed to be changed in
        /// for the current transmit mode
        /// </summary>
        public bool TXFilterChangesAllowed
        {
            get { return _txFilterChangesAllowed; }
            internal set
            {
                if (_txFilterChangesAllowed != value)
                {
                    _txFilterChangesAllowed = value;
                    RaisePropertyChanged("TXFilterChangesAllowed");
                }
            }
        }

        private bool _txRFPowerChangesAllowed;
        /// <summary>
        /// Gets whether the RF Power is allowed to be changed in
        /// for the current radio state
        /// </summary>
        public bool TXRFPowerChangesAllowed
        {
            get { return _txRFPowerChangesAllowed; }
            internal set
            {
                if (_txRFPowerChangesAllowed != value)
                {
                    _txRFPowerChangesAllowed = value;
                    RaisePropertyChanged("TXRFPowerChangesAllowed");
                }
            }
        }

        private bool _hwalcEnabled;
        /// <summary>
        /// Enables or disables the ALC RCA input on the back panel of the radio
        /// </summary>
        public bool HWAlcEnabled
        {
            get { return _hwalcEnabled; }
            set
            {
                if (_hwalcEnabled != value)
                {
                    _hwalcEnabled = value;
                    SendCommand("transmit set hwalc_enabled=" + Convert.ToByte(_hwalcEnabled));
                    RaisePropertyChanged("HWAlcEnabled");
                }
            }
        }

        private void _SetTXFilter(int low, int high)
        {
            if (low >= high) return;

            if (_txFilterLow != low || _txFilterHigh != high)
            {
                if (high > 10000)   // max 10 kHz
                    high = 10000;

                if (low < 0)        // min 0 Hz
                    low = 0;

                _txFilterLow = low;
                _txFilterHigh = high;
                SendCommand("transmit set filter_low=" + _txFilterLow + " filter_high=" + _txFilterHigh);
            }
        }

        private int _txFilterLow;
        /// <summary>
        /// The low cut frequency of the transmit filter in Hz (0 to TXFilterHigh Hz - 50 Hz)
        /// </summary>
        public int TXFilterLow
        {
            get { return _txFilterLow; }
            set
            {
                int new_cut = value;

                if (new_cut > _txFilterHigh - 50)
                    new_cut = _txFilterHigh - 50;

                if (new_cut < 0) new_cut = 0;

                if (_txFilterLow != new_cut)
                {
                    _SetTXFilter(value, _txFilterHigh);
                    RaisePropertyChanged("TXFilterLow");
                }
                else if (new_cut != value)
                {
                    RaisePropertyChanged("TXFilterLow");
                }
            }
        }

        private int _txFilterHigh;
        /// <summary>
        /// The high cut frequency of the transmit filter in Hz (TXFilterLow + 50 Hz to 10000 Hz)
        /// </summary>
        public int TXFilterHigh
        {
            get { return _txFilterHigh; }
            set
            {
                int new_cut = value;

                if (new_cut < _txFilterLow + 50)
                    new_cut = _txFilterLow + 50;

                if (new_cut > 10000) new_cut = 10000;

                if (_txFilterHigh != new_cut)
                {
                    _SetTXFilter(_txFilterLow, value);
                    RaisePropertyChanged("TXFilterHigh");
                }
                else if (new_cut != value)
                {
                    RaisePropertyChanged("TXFilterHigh");
                }
            }
        }

        private bool _txTune;
        /// <summary>
        /// Keys the transmitter with Tune
        /// </summary>
        public bool TXTune
        {
            get { return _txTune; }
            set
            {
                if (_txTune != value)
                {
                    _txTune = value;
                    SendCommand("transmit tune " + Convert.ToByte(_txTune));
                    RaisePropertyChanged("TXTune");
                }
            }
        }

        private bool _txMonitor;
        /// <summary>
        /// Enables the transmit monitor
        /// </summary>
        public bool TXMonitor
        {
            get { return _txMonitor; }
            set
            {
                if (_txMonitor != value)
                {
                    _txMonitor = value;
                    SendCommand("transmit set mon=" + Convert.ToByte(_txMonitor));
                    RaisePropertyChanged("TXMonitor");
                }
            }
        }

        private int _txCWMonitorGain;
        /// <summary>
        /// The transmit monitor gain from 0 to 100
        /// </summary>
        public int TXCWMonitorGain
        {
            get { return _txCWMonitorGain; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain < 0) new_gain = 0;
                if (new_gain > 100) new_gain = 100;

                if (_txCWMonitorGain != new_gain)
                {
                    _txCWMonitorGain = new_gain;
                    SendCommand("transmit set mon_gain_cw=" + _txCWMonitorGain);
                    RaisePropertyChanged("TXCWMonitorGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("TXCWMonitorGain");
                }
            }
        }

        private int _txSBMonitorGain;
        /// <summary>
        /// The transmit monitor gain from 0 to 100
        /// </summary>
        public int TXSBMonitorGain
        {
            get { return _txSBMonitorGain; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain < 0) new_gain = 0;
                if (new_gain > 100) new_gain = 100;

                if (_txSBMonitorGain != new_gain)
                {
                    _txSBMonitorGain = new_gain;
                    SendCommand("transmit set mon_gain_sb=" + _txSBMonitorGain);
                    RaisePropertyChanged("TXSBMonitorGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("TXSBMonitorGain");
                }
            }
        }

        private int _txCWMonitorPan;
        /// <summary>
        /// Gets or sets the left-right pan for the CW monitor (sidetone) from 0 to 100.  
        /// A value of 50 pans evenly between left and right.
        /// </summary>
        public int TXCWMonitorPan
        {
            get { return _txCWMonitorPan; }
            set
            {
                int new_pan = value;

                // check limits
                if (new_pan < 0) new_pan = 0;
                if (new_pan > 100) new_pan = 100;

                if (_txCWMonitorPan != new_pan)
                {
                    _txCWMonitorPan = new_pan;
                    SendCommand("transmit set mon_pan_cw=" + _txCWMonitorPan);
                    RaisePropertyChanged("TXCWMonitorPan");
                }
                else if (new_pan != value)
                {
                    RaisePropertyChanged("TXCWMonitorPan");
                }
            }
        }

        private int _txSBMonitorPan;
        /// <summary>
        /// The transmit monitor gain from 0 to 100
        /// </summary>
        public int TXSBMonitorPan
        {
            get { return _txSBMonitorPan; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain < 0) new_gain = 0;
                if (new_gain > 100) new_gain = 100;

                if (_txSBMonitorPan != new_gain)
                {
                    _txSBMonitorPan = new_gain;
                    SendCommand("transmit set mon_pan_sb=" + _txSBMonitorPan);
                    RaisePropertyChanged("TXSBMonitorPan");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("TXSBMonitorPan");
                }
            }
        }

        private bool _mox;
        /// <summary>
        /// Enables mox
        /// </summary>
        public bool Mox
        {
            get { return _mox; }
            set
            {
                if (_mox != value)
                {
                    _mox = value;
                    SendCommand("xmit " + Convert.ToByte(_mox));
                    RaisePropertyChanged("Mox");
                }
            }
        }

        private bool _txMonAvailable;
        /// <summary>
        /// True when MOX is avaialble to be used
        /// </summary>
        public bool TxMonAvailable
        {
            get { return _txMonAvailable; }
        }

        private bool _txInhibit;
        /// <summary>
        /// Enables or disables the transmit inhibit
        /// </summary>
        public bool TXInhibit
        {
            get { return _txInhibit; }
            set
            {
                if (_txInhibit != value)
                {
                    _txInhibit = value;
                    SendCommand("transmit set inhibit=" + Convert.ToByte(_txInhibit));
                    RaisePropertyChanged("TXInhibit");
                }
            }
        }

        private bool _txAallowed;
        public bool TXAllowed
        {
            get { return _txAallowed; }
            internal set
            {
                if (_txAallowed == value)
                    return;

                _txAallowed = value;
                RaisePropertyChanged("TXAllowed");
            }
        }

        private bool _met_in_rx;
        /// <summary>
        /// Enables or disables the level meter during receive
        /// </summary>
        public bool MetInRX
        {
            get { return _met_in_rx; }
            set
            {
                if (_met_in_rx != value)
                {
                    _met_in_rx = value;
                    SendCommand("transmit set met_in_rx=" + Convert.ToByte(_met_in_rx));
                    RaisePropertyChanged("MetInRX");
                }
            }
        }

        private int _cwPitch;
        /// <summary>
        /// The CW pitch from 100 Hz to 6000 Hz
        /// </summary>
        public int CWPitch
        {
            get { return _cwPitch; }
            set
            {
                int new_pitch = value;

                if (new_pitch < 100) new_pitch = 100;
                if (new_pitch > 6000) new_pitch = 6000;

                if (_cwPitch != new_pitch)
                {
                    _cwPitch = new_pitch;
                    SendCommand("cw pitch " + _cwPitch);
                    RaisePropertyChanged("CWPitch");
                }
                else if (new_pitch != value)
                {
                    RaisePropertyChanged("CWPitch");
                }
            }
        }

        private bool _apfMode;
        /// <summary>
        /// Enables or disables the auto-peaking filter (APF)
        /// </summary>
        public bool APFMode
        {
            get { return _apfMode; }
            set
            {
                if (_apfMode != value)
                {
                    _apfMode = value;
                    SendCommand("eq apf mode=" + _apfMode);
                    RaisePropertyChanged("APFMode");
                }
            }
        }

        private double _apfQFactor;
        /// <summary>
        /// The Q factor for the auto-peaking filter (APF) from 0 to 33
        /// </summary>
        public double APFQFactor
        {
            get { return _apfQFactor; }
            set
            {
                if (_apfQFactor != value)
                {
                    _apfQFactor = value;
                    SendCommand("eq apf qfactor=" + StringHelper.DoubleToString(_apfQFactor, "f6"));
                    RaisePropertyChanged("APFQFactor");
                }
            }
        }

        private double _apfGain;
        /// <summary>
        /// The gain of the auto-peaking filter (APF) from 0 to 100, mapped
        /// linearly from 0 dB to 14 dB
        /// </summary>
        public double APFGain
        {
            get { return _apfGain; }
            set
            {
                // TODO: Need a bounds check here.  Are the bounds really 0-100?
                // Should this property be a double or an int?
                if (_apfGain != value)
                {
                    _apfGain = value;
                    SendCommand("eq apf gain=" + StringHelper.DoubleToString(_apfGain, "f6"));
                    RaisePropertyChanged("APFGain");
                }
            }
        }

        private int _cwSpeed;
        /// <summary>
        /// The CW speed in words per minute (wpm) from 5 to 100
        /// </summary>
        public int CWSpeed
        {
            get { return _cwSpeed; }
            set
            {
                int new_speed = value;

                if (new_speed < 5) new_speed = 5;
                if (new_speed > 100) new_speed = 100;

                if (_cwSpeed != new_speed)
                {
                    _cwSpeed = new_speed;
                    SendCommand("cw wpm " + _cwSpeed);
                    RaisePropertyChanged("CWSpeed");
                }
                else if (new_speed != value)
                {
                    RaisePropertyChanged("CWSpeed");
                }
            }
        }

        private int _cwDelay;
        /// <summary>
        /// The CW breakin delay in milliseconds (ms) from 0 ms to 2000 ms
        /// </summary>
        public int CWDelay
        {
            get { return _cwDelay; }
            set
            {
                int new_delay = value;

                if (new_delay < 0) new_delay = 0;
                if (new_delay > 2000) new_delay = 2000;

                if (_cwDelay != new_delay)
                {
                    _cwDelay = new_delay;
                    SendCommand("cw break_in_delay " + _cwDelay);
                    RaisePropertyChanged("CWDelay");
                }
                else if (new_delay != value)
                {
                    RaisePropertyChanged("CWDelay");
                }
            }
        }


        private bool _cwBreakIn;
        /// <summary>
        /// Enables or disables CW breakin mode, which turns on the
        /// transmitter by a key or paddle closure rather than using PTT
        /// </summary>
        public bool CWBreakIn
        {
            get { return _cwBreakIn; }
            set
            {
                if (_cwBreakIn != value)
                {
                    _cwBreakIn = value;
                    SendCommand("cw break_in " + Convert.ToByte(_cwBreakIn));
                    RaisePropertyChanged("CWBreakIn");
                }
            }
        }

        private bool _cwSidetone;
        /// <summary>
        /// Enables or disables the CW Sidetone
        /// </summary>
        public bool CWSidetone
        {
            get { return _cwSidetone; }
            set
            {
                if (_cwSidetone != value)
                {
                    _cwSidetone = value;
                    SendCommand("cw sidetone " + Convert.ToByte(_cwSidetone));
                    RaisePropertyChanged("CWSidetone");
                }
            }
        }

        private bool _cwIambic;
        /// <summary>
        /// Enables or disables the Iambic keyer for CW
        /// </summary>
        public bool CWIambic
        {
            get { return _cwIambic; }
            set
            {
                if (_cwIambic != value)
                {
                    _cwIambic = value;
                    SendCommand("cw iambic " + Convert.ToByte(_cwIambic));
                    RaisePropertyChanged("CWIambic");
                }
            }
        }

        private bool _cwIambicModeA;
        /// <summary>
        /// Enables or disables CW Iambic Mode A
        /// </summary>
        public bool CWIambicModeA
        {
            get { return _cwIambicModeA; }
            set
            {
                if (_cwIambicModeA != value)
                {
                    _cwIambicModeA = value;
                    if (_cwIambicModeA)
                    {
                        SendCommand("cw mode 0");
                    }

                    RaisePropertyChanged("CWIambicModeA");
                }
            }
        }

        private bool _cwIambicModeB;
        /// <summary>
        /// Enables or disables CW Iambic Mode B
        /// </summary>
        public bool CWIambicModeB
        {
            get { return _cwIambicModeB; }
            set
            {
                if (_cwIambicModeB != value)
                {
                    _cwIambicModeB = value;
                    if (_cwIambicModeB)
                    {
                        SendCommand("cw mode 1");
                    }

                    RaisePropertyChanged("CWIambicModeB");
                }
            }
        }


        private bool _cwl_enabled;
        /// <summary>
        /// Enables or disables CWL. CWU (default) active when disabled.
        /// </summary>
        public bool CWL_Enabled
        {
            get { return _cwl_enabled; }
            set
            {
                if (_cwl_enabled != value)
                {
                    _cwl_enabled = value;
                    SendCommand("cw cwl_enabled " + Convert.ToByte(_cwl_enabled));

                    RaisePropertyChanged("CWL_Enabled");
                }
            }
        }

        private bool _cwSwapPaddles;
        /// <summary>
        /// Swaps the CW dot-dash paddles when true
        /// </summary>
        public bool CWSwapPaddles
        {
            get { return _cwSwapPaddles; }
            set
            {
                if (_cwSwapPaddles != value)
                {
                    _cwSwapPaddles = value;
                    SendCommand("cw swap " + Convert.ToByte(_cwSwapPaddles));
                    RaisePropertyChanged("CWSwapPaddles");
                }
            }
        }

        private bool _companderOn;
        /// <summary>
        /// Enables or disables the Compander
        /// </summary>
        public bool CompanderOn
        {
            get { return _companderOn; }
            set
            {
                if (_companderOn != value)
                {
                    _companderOn = value;
                    SendCommand("transmit set compander=" + Convert.ToByte(_companderOn));
                    RaisePropertyChanged("CompanderOn");
                }
            }
        }

        private int _companderLevel;
        /// <summary>
        /// The compander level from 0 to 100
        /// </summary>
        public int CompanderLevel
        {
            get { return _companderLevel; }
            set
            {
                int new_val = value;

                if (new_val < 0) new_val = 0;
                if (new_val > 100) new_val = 100;
                

                if (new_val != _companderLevel)
                {
                    _companderLevel = new_val;
                    SendCommand("transmit set compander_level=" + _companderLevel);
                    RaisePropertyChanged("CompanderLevel");
                }
                else if (new_val != value)
                {
                    RaisePropertyChanged("CompanderLevel");
                }
            }
        }

        private bool _accOn;
        /// <summary>
        /// Enables or disables mixing of an input via the accessory port on the back panel 
        /// of the radio with the currently selected Mic input
        /// </summary>
        public bool ACCOn
        {
            get { return _accOn; }
            set
            {
                if (_accOn != value)
                {
                    _accOn = value;
                    SendCommand("mic acc " + Convert.ToByte(_accOn));
                    RaisePropertyChanged("ACCOn");
                }
            }
        }


        private bool _daxOn;
        /// <summary>
        /// Enables or disables Digital Audio eXchange (DAX)
        /// </summary>
        public bool DAXOn
        {
            get { return _daxOn; }
            set
            {
                if (_daxOn != value)
                {
                    _daxOn = value;
                    SendCommand("transmit set dax=" + Convert.ToByte(_daxOn));
                    RaisePropertyChanged("DAXOn");
                }
            }
        }

        private bool _simpleVOXEnable;
        /// <summary>
        /// Enables or disables VOX
        /// </summary>
        public bool SimpleVOXEnable
        {
            get { return _simpleVOXEnable; }
            set
            {
                if (_simpleVOXEnable != value)
                {
                    _simpleVOXEnable = value;
                    SendCommand("transmit set vox_enable=" + Convert.ToByte(_simpleVOXEnable));
                    RaisePropertyChanged("SimpleVOXEnable");
                }
            }
        }

        private bool _ssbPeakControlEnable;
        /// <summary>
        /// Enables or disables Peak Control on TX for improved power output
        /// </summary>
        public bool SSBPeakControlEnable
        {
            get { return _ssbPeakControlEnable; }
            set
            {
                if (_ssbPeakControlEnable != value)
                {
                    _ssbPeakControlEnable = value;
                    SendCommand("transmit set ssb_peak_control=" + Convert.ToByte(_ssbPeakControlEnable));
                    RaisePropertyChanged("SSBPeakControlEnable");
                }
            }
        }

        private int _simpleVOXLevel;
        /// <summary>
        /// The vox level from 0 to 100
        /// </summary>
        public int SimpleVOXLevel
        {
            get { return _simpleVOXLevel; }
            set
            {
                int new_val = value;

                // check limits
                if (new_val < 0) new_val = 0;
                if (new_val > 100) new_val = 100;

                if (_simpleVOXLevel != new_val)
                {
                    _simpleVOXLevel = new_val;
                    SendCommand("transmit set vox_level=" + _simpleVOXLevel);
                    RaisePropertyChanged("SimpleVOXLevel");
                }
                else if (new_val != value)
                    RaisePropertyChanged("SimpleVOXLevel");
            }
        }

        private int _simpleVOXDelay;
        /// <summary>
        /// Sets the VOX delay from 0 to 100.  The delay will
        /// be (value * 20) milliseconds.  Setting this value to 
        /// 50 will result in a delay of 1000 ms.
        /// </summary>
        public int SimpleVOXDelay
        {
            get { return _simpleVOXDelay; }
            set
            {
                int new_val = value;

                // check limits
                if (new_val < 0) new_val = 0;
                if (new_val > 100) new_val = 100;

                if (_simpleVOXDelay != new_val)
                {
                    _simpleVOXDelay = new_val;
                    // _simpleVOXDelay is multiplied by 20 to set the hang time in milliseconds
                    SendCommand("transmit set vox_delay=" + _simpleVOXDelay);
                    RaisePropertyChanged("SimpleVOXDelay");
                }
                else if (new_val != value)
                    RaisePropertyChanged("SimpleVOXDelay");
            }
        }


        private bool _speechProcessorEnable;
        public bool SpeechProcessorEnable
        {
            get { return _speechProcessorEnable; }
            set
            {
                if (_speechProcessorEnable != value)
                {
                    _speechProcessorEnable = value;
                    SendCommand("transmit set speech_processor_enable=" + Convert.ToByte(_speechProcessorEnable));
                    RaisePropertyChanged("SpeechProcessorEnable");
                }
            }
        }

        private uint _speechProcessorLevel;
        public uint SpeechProcessorLevel
        {
            get { return _speechProcessorLevel; }
            set
            {
                if (_speechProcessorLevel != value)
                {
                    _speechProcessorLevel = value;
                    SendCommand("transmit set speech_processor_level=" + Convert.ToByte(_speechProcessorLevel));
                    RaisePropertyChanged("SpeechProcessorLevel");
                }
            }
        }

        private bool _fullDuplexEnabled;
        public bool FullDuplexEnabled
        {
            get { return _fullDuplexEnabled; }
            set
            {
                if (_fullDuplexEnabled != value)
                {
                    _fullDuplexEnabled = value;
                    SendCommand("radio set full_duplex_enabled=" + Convert.ToByte(_fullDuplexEnabled));
                    RaisePropertyChanged("FullDuplexEnabled");
                }
            }
        }

        private void ParseTransmitStatus(string s)
        {
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::ParseTransmitStatus: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "max_power_level":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - max_power_level: Invalid value (" + kv + ")");
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;
                            _maxPowerLevel = temp;
                            RaisePropertyChanged("MaxPowerLevel");
                            break;
                        }
                    case "rfpower":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - rfpower: Invalid value (" + kv + ")");
                                continue;
                            }

                            // check limits
                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _rfPower = temp;
                            RaisePropertyChanged("RFPower");
                            break;
                        }

                    case "tunepower":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - tunepower: Invalid value (" + kv + ")");
                                continue;
                            }

                            // check limits
                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _tunePower = temp;
                            RaisePropertyChanged("TunePower");
                            break;
                        }

                    case "lo":
                        {
                            int temp; // in Hz
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txFilterLow = temp;
                            RaisePropertyChanged("TXFilterLow");
                            break;
                        }

                    case "hi":
                        {
                            int temp; // in Hz
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txFilterHigh = temp;
                            RaisePropertyChanged("TXFilterHigh");
                            break;
                        }

                    case "tx_filter_changes_allowed":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - tx_filter_changes_allowed: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txFilterChangesAllowed = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXFilterChangesAllowed");
                            break;
                        }

                    case "tx_rf_power_changes_allowed":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - tx_rf_power_changes_allowed: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txRFPowerChangesAllowed = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXRFPowerChangesAllowed");
                            break;
                        }

                    case "am_carrier_level":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - am_carrier_level: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _amCarrierLevel = temp;
                            RaisePropertyChanged("AMCarrierLevel");
                            break;
                        }

                    case "mic_level":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mic_level: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _micLevel = temp;
                            RaisePropertyChanged("MicLevel");
                            break;
                        }

                    case "mic_selection":
                        {
                            _micInput = value;
                            if (_micInput == "PC")
                                RemoteTxOn = true;
                            else
                                RemoteTxOn = false;

                            RaisePropertyChanged("MicInput");
                            break;
                        }

                    case "mic_boost":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mic_boost: Invalid value (" + kv + ")");
                                continue;
                            }

                            _micBoost = Convert.ToBoolean(temp);
                            RaisePropertyChanged("MicBoost");
                            break;
                        }

                    case "mon_available":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mon_available: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txMonAvailable = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TxMonAvailable");
                            break;
                        }
                    case "hwalc_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - hwalc_enabled: Invalid value (" + kv + ")");
                                continue;
                            }

                            _hwalcEnabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("HWAlcEnabled");
                            break;
                        }

                    case "inhibit":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - inhibit: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txInhibit = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXInhibit");
                            break;
                        }

                    case "mic_bias":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mic_bias: Invalid value (" + kv + ")");
                                continue;
                            }

                            _micBias = Convert.ToBoolean(temp);
                            RaisePropertyChanged("MicBias");
                            break;
                        }

                    case "mic_acc":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mic_acc: Invalid value (" + kv + ")");
                                continue;
                            }

                            _accOn = Convert.ToBoolean(temp);
                            RaisePropertyChanged("ACCOn");
                            break;
                        }

                    case "dax":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - dax: Invalid value (" + kv + ")");
                                continue;
                            }

                            _daxOn = Convert.ToBoolean(temp);
                            RaisePropertyChanged("DAXOn");
                            break;
                        }

                    case "compander":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - compander: Invalid value (" + kv + ")");
                                continue;
                            }

                            _companderOn = Convert.ToBoolean(temp);
                            RaisePropertyChanged("CompanderOn");
                            break;
                        }

                    case "compander_level":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - compander_level: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _companderLevel = temp;
                            RaisePropertyChanged("CompanderLevel");
                            break;
                        }

                    /*case "noise_gate_level":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - noise_gate_level: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _noiseGateLevel = temp;
                            RaisePropertyChanged("NoiseGateLevel");
                            break;
                        }*/

                    case "pitch":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - pitch: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 100) temp = 100;
                            if (temp > 6000) temp = 6000;

                            _cwPitch = temp;
                            RaisePropertyChanged("CWPitch");
                            break;
                        }

                    case "speed":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - pitch: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 1) temp = 1;
                            if (temp > 100) temp = 100;

                            _cwSpeed = temp;
                            RaisePropertyChanged("CWSpeed");
                            break;
                        }

                    case "synccwx":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b || temp < 0 || temp > 1)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - synccwx: Invalid value (" + kv + ")");
                                continue;
                            }

                            _syncCWX = Convert.ToBoolean(temp);
                            RaisePropertyChanged("SyncCWX");
                            break;
                        }

                    case "iambic":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - iambic: Invalid value (" + kv + ")");
                                continue;
                            }

                            _cwIambic = Convert.ToBoolean(temp);
                            RaisePropertyChanged("CWIambic");
                            break;
                        }

                    case "iambic_mode":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - iambic_mode: Invalid value (" + kv + ")");
                                continue;
                            }

                            // Currently only Iambic Mode A and B are available in the client
                            // 0: Iambic Mode A
                            // 1: Iambic Mode B
                            // 2: Iambic Mode B Strict
                            // 3: Iambic Mode Bug

                            switch (temp)
                            {
                                case 0:
                                    _cwIambicModeA = true;
                                    RaisePropertyChanged("CWIambicModeA");
                                    break;
                                case 1:
                                    _cwIambicModeB = true;
                                    RaisePropertyChanged("CWIambicModeB");
                                    break;
                                default:
                                    _cwIambicModeA = true;
                                    RaisePropertyChanged("CWIambicModeA");
                                    break;
                            }

                            break;
                        }

                    case "swap_paddles":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - swap_paddles: Invalid value (" + kv + ")");
                                continue;
                            }

                            _cwSwapPaddles = Convert.ToBoolean(temp);
                            RaisePropertyChanged("CWSwapPaddles");
                            break;
                        }

                    case "break_in":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - break_in: Invalid value (" + kv + ")");
                                continue;
                            }

                            _cwBreakIn = Convert.ToBoolean(temp);
                            RaisePropertyChanged("CWBreakIn");
                            break;
                        }

                    case "sidetone":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - sidetone: Invalid value (" + kv + ")");
                                continue;
                            }

                            _cwSidetone = Convert.ToBoolean(temp);
                            RaisePropertyChanged("CWSidetone");
                            break;
                        }
                    case "cwl_enabled":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - cwl_enabled: Invalid value (" + kv + ")");
                                continue;
                            }

                            _cwl_enabled = Convert.ToBoolean(temp);
                            RaisePropertyChanged("CWL_Enabled");
                            break;
                        }

                    case "break_in_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - break_in_delay: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 2000) temp = 2000;

                            _cwDelay = temp;

                            RaisePropertyChanged("CWDelay");
                            break;
                        }

                    case "sb_monitor":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - sb_monitor: Invalid value (" + kv + ")");
                                continue;
                            }
                            _txMonitor = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXMonitor");
                            break;
                        }
                    case "mon_gain_cw":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mon_gain_cw: invalid value (" + kv + ")");
                                continue;
                            }

                            _txCWMonitorGain = temp;
                            RaisePropertyChanged("TXCWMonitorGain");
                            break;
                        }
                    case "mon_gain_sb":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mon_gain_sb: invalid value (" + kv + ")");
                                continue;
                            }

                            _txSBMonitorGain = temp;
                            RaisePropertyChanged("TXSBMonitorGain");
                            break;
                        }
                    case "mon_pan_cw":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mon_pan_cw: invalid value (" + kv + ")");
                                continue;
                            }

                            _txCWMonitorPan = temp;
                            RaisePropertyChanged("TXCWMonitorPan");
                            break;
                        }
                    case "mon_pan_sb":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - mon_pan_sb: invalid value (" + kv + ")");
                                continue;
                            }

                            _txSBMonitorPan = temp;
                            RaisePropertyChanged("TXSBMonitorPan");
                            break;
                        }
                    case "speech_processor_enable":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - Speech Processor Enable: Invalid value (" + kv + ")");
                                continue;
                            }

                            _speechProcessorEnable = Convert.ToBoolean(temp);
                            RaisePropertyChanged("SpeechProcessorEnable");
                            break;
                        }
                    case "speech_processor_level":
                        {
                            uint temp;
                            bool b = uint.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - Speech Processor Level: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _speechProcessorLevel = temp;
                            RaisePropertyChanged("SpeechProcessorLevel");
                            break;
                        }

                    case "vox_enable":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - vox_enable: Invalid value (" + kv + ")");
                                continue;
                            }

                            _simpleVOXEnable = Convert.ToBoolean(temp);
                            RaisePropertyChanged("SimpleVOXEnable");
                            break;
                        }

                    case "vox_level":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - vox_level: Invalid value (" + kv + ")");
                                continue;
                            }

                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _simpleVOXLevel = temp;
                            RaisePropertyChanged("SimpleVOXLevel");
                            break;
                        }
                    case "vox_delay":
                        {
                            int temp;
                            bool b = int.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - vox_delay: Invalid value (" + kv + ")");
                                continue;
                            }
                            if (temp < 0) temp = 0;
                            if (temp > 100) temp = 100;

                            _simpleVOXDelay = temp;
                            RaisePropertyChanged("SimpleVOXDelay");
                            break;
                        }
                    case "tune":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - tune: Invalid value (" + kv + ")");
                                continue;
                            }

                            _txTune = Convert.ToBoolean(temp);
                            RaisePropertyChanged("TXTune");
                            break;
                        }
                    case "met_in_rx":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - met_in_rx: Invalid value (" + kv + ")");
                                continue;
                            }

                            _met_in_rx = Convert.ToBoolean(temp);
                            RaisePropertyChanged("MetInRX");
                            break;
                        }
                    case "show_tx_in_waterfall":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTransmitStatus - show_tx_in_waterfall: Invalid value (" + kv + ")");
                                continue;
                            }

                            _showTxInWaterfall = Convert.ToBoolean(temp);
                            RaisePropertyChanged("ShowTxInWaterfall");
                            break;
                        }
                }
            }
        }



        #endregion

        #region receive Routines

        private bool _startOffsetEnabled = true;
        /// <summary>
        /// Allows or prevents the ability to start an automatic 
        /// frequency offset calibration routine.  This is used 
        /// to prevent the user from starting a routine while
        /// one is already in progress
        /// </summary>
        public bool StartOffsetEnabled
        {
            get { return _startOffsetEnabled; }
            set
            {
                if (_startOffsetEnabled != value)
                {
                    _startOffsetEnabled = value;

                    //when 0 start pll (disabled)
                    //when 1 pll is over (enabled, default state)
                    if (!_startOffsetEnabled)
                    {
                        SendCommand("radio pll_start");
                    }
                    RaisePropertyChanged("StartOffsetEnabled");
                }
            }
        }


        private int _freqErrorPPB;
        /// <summary>
        /// The frequency error correction value for the internal clock of
        /// radio in parts per billion
        /// </summary>
        public int FreqErrorPPB
        {
            get { return _freqErrorPPB; }
            set
            {
                if (_freqErrorPPB != value)
                {
                    _freqErrorPPB = value;
                    SendCommand("radio set freq_error_ppb=" + _freqErrorPPB);
                    RaisePropertyChanged("FreqErrorPPB");
                }
            }
        }

        private double _calFreq;
        /// <summary>
        /// The frequency, in MHz, that the automatic frequency error correction
        /// routine will use to listen for a reference tone
        /// </summary>
        public double CalFreq
        {
            get { return _calFreq; }
            set
            {
                if (_calFreq != value)
                {
                    _calFreq = value;
                    SendCommand("radio set cal_freq=" + StringHelper.DoubleToString(_calFreq, "f6"));
                    RaisePropertyChanged("CalFreq");
                }
            }
        }

        /// <summary>
        /// Returns true if Diversity is allowed on the radio model.
        /// </summary>
        public bool DiversityIsAllowed
        {
            get { return (this.Model=="FLEX-6700" || this.Model=="FLEX-6700R"); }
        }

        #endregion

        #region ATU Routines

        private bool _atuPresent;
        /// <summary>
        /// Returns true if an automatic antenna tuning unit (ATU) is present
        /// </summary>
        public bool ATUPresent
        {
            get { return _atuPresent; }
        }

        private bool _atuEnabled;
        /// <summary>
        /// Returns true if the ATU is allowed to be used.
        /// </summary>
        public bool ATUEnabled
        {
            get { return _atuEnabled; }
            internal set
            {
                _atuEnabled = value;
                RaisePropertyChanged("ATUEnabled");
            }
        }

        private bool _atuMemoriesEnabled;
        /// <summary>
        /// Gets or sets whether ATU Memories are enabled
        /// </summary>
        public bool ATUMemoriesEnabled
        {
            get { return _atuMemoriesEnabled; }
            set
            {
                if (_atuMemoriesEnabled == value)
                    return;

                _atuMemoriesEnabled = value;
                SendCommand("atu set memories_enabled=" + Convert.ToByte(_atuMemoriesEnabled));
                RaisePropertyChanged("ATUMemoriesEnabled");
            }
        }

        private bool _atuUsingMemory;
        /// <summary>
        /// Gets whether an ATU Memory is currently being used
        /// </summary>
        public bool ATUUsingMemory
        {
            get { return _atuUsingMemory; }
        }

        /// <summary>
        /// Starts an automatic tune on the automatic antenna tuning unit (ATU)
        /// </summary>
        public void ATUTuneStart()
        {
            SendCommand("atu start");
        }

        /// <summary>
        /// Sets the automatic antenna tuning unit (ATU) to be in bypass mode
        /// </summary>
        public void ATUTuneBypass()
        {
            SendCommand("atu bypass");
        }

        /// <summary>
        /// Clears all ATU memories
        /// </summary>
        public void ATUClearMemories()
        {
            SendCommand("atu clear");
        }

        private ATUTuneStatus _atuTuneStatus = ATUTuneStatus.None;
        /// <summary>
        /// Gets the current status of the automatic antenna tuning unit (ATU)
        /// </summary>
        public ATUTuneStatus ATUTuneStatus
        {
            get { return _atuTuneStatus; }
            internal set
            {
                if (_atuTuneStatus != value)
                {
                    _atuTuneStatus = value;
                    RaisePropertyChanged("ATUTuneStatus");
                }
            }
        }

        private void ParseATUStatus(string s)
        {
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::ParseATUStatus: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "status":
                        {
                            ATUTuneStatus status = ParseATUTuneStatus(value);
                            if (status == ATUTuneStatus.None)
                            {
                                Debug.WriteLine("Radio::ParseATUStatus: Error - Invalid status (" + value + ")");
                                continue;
                            }

                            ATUTuneStatus = status;
                            break;
                        }

                    case "atu_enabled":
                        {
                            byte is_enabled = 0;
                            bool b = byte.TryParse(value, out is_enabled);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseATUStatus: Invalid value for atu_enabled(" + kv + ")");
                            }

                            ATUEnabled = Convert.ToBoolean(is_enabled);
                            break;
                        }

                    case "memories_enabled":
                        {
                            byte memeories_enabled = 0;
                            bool b = byte.TryParse(value, out memeories_enabled);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseATUStatus: Invalid value for memeories_enabled(" + kv + ")");
                            }

                            _atuMemoriesEnabled = Convert.ToBoolean(memeories_enabled);
                            RaisePropertyChanged("ATUMemoriesEnabled");
                            break;
                        }

                    case "using_mem":
                        {
                            byte using_memory = 0;
                            bool b = byte.TryParse(value, out using_memory);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseATUStatus: Invalid value for using_mem(" + kv + ")");
                            }

                            _atuUsingMemory = Convert.ToBoolean(using_memory);
                            RaisePropertyChanged("ATUUsingMemory");
                            break;
                        }                        
                }
            }
        }
        
        private ObservableCollection<string> _waveformsInstalledList;
        public ObservableCollection<string> WaveformsInstalledList
        {
            get { return _waveformsInstalledList; }
        }

        private void UpdateWaveformsInstalledList(string s)
        {
            string[] inputs = s.Split(',');

            _waveformsInstalledList = new ObservableCollection<string>();
            foreach (string wave in inputs)
            {
                if (wave != "")
                {
                    _waveformsInstalledList.Add(wave.Replace('\u007f', ' '));
                }
            }

            RaisePropertyChanged("WaveformsInstalledList");
        }

        private void ParseWaveformStatus(string s)
        {
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::ParseWaveformStatus: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "installed_list":
                        {
                            UpdateWaveformsInstalledList(value);
                            break;
                        }
                }
            }

        }


        private void ParseProfilesStatus(string s)
        {
            char[] separators = new char[] { ' ' };
            Int32 count = 2;
            string[] words = s.Split(separators, count);
            string profile_type = words[0]; // global | tx | mic | displays
            //uint i;
            /* We only allow one single status key=token pair in profiles since 
             * profile names can have spaces 
             */
            string kv = words[1];
            string [] tokens = kv.Split('=');
            if(tokens.Length != 2)
            {
                Debug.WriteLine("Radio::ParseProfilesStatus: Invalid key/value pair (" + kv + ")");
            }

            string key = tokens[0];
            string value = tokens[1];

            switch(key.ToLower())
            {
                case "list":
                    {
                        UpdateProfileList(profile_type, value);
                        break;
                    }
                case "current":
                    {
                        UpdateProfileListSelection(profile_type, value);
                        break;
                    }
                case "importing":
                    {
                        byte is_importing = 0;

                        bool b = byte.TryParse(value, out is_importing);
                        if (!b)
                        {
                            Debug.WriteLine("Radio::ParseProfilesStatus: Invalid value for importing(" + kv + ")");
                        }

                        DatabaseImportComplete = !Convert.ToBoolean(is_importing);
                        break;
                    }
                case "exporting":
                    {
                        byte is_exporting = 0;
                        bool b = byte.TryParse(value, out is_exporting);
                        if (!b)
                        {
                            Debug.WriteLine("Radio::ParseProfilesStatus: Invalid value for exporting(" + kv + ")");
                        }

                        DatabaseExportComplete = !Convert.ToBoolean(is_exporting);
                        break;
                    }
            }
            
        }

        private void UpdateProfileListSelection(string profile_type, string profile_name)
        {
            switch (profile_type)
            {
                case "global":
                    if (_profileGlobalList != null && _profileGlobalList.Contains(profile_name))
                    {
                        if (_profileGlobalSelection != profile_name)
                        {
                            _profileGlobalSelection = profile_name;
                            RaisePropertyChanged("ProfileGlobalSelection");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Profile List Problem!");
                    }
                    break;
                case "tx":
                    if (_profileTXList != null && _profileTXList.Contains(profile_name))
                    {
                        if (_profileTXSelection != profile_name)
                        {
                            _profileTXSelection = profile_name;
                            RaisePropertyChanged("ProfileTXSelection");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Profile List Problem!");
                    }
                    break;
                case "mic":
                    if ( _profileMICList != null && _profileMICList.Contains(profile_name))
                    {
                        if (_profileMICSelection != profile_name)
                        {
                            _profileMICSelection = profile_name;
                            RaisePropertyChanged("ProfileMICSelection");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Profile List Problem!");
                    }
                    break;
                case "displays":
                    if ( _profileDisplayList != null && _profileDisplayList.Contains(profile_name))
                    {
                        if (_profileDisplaySelection != profile_name)
                        {
                            _profileDisplaySelection = profile_name;
                            RaisePropertyChanged("ProfileDisplaySelection");
                        }
                    }
                    else
                    {
                        Debug.WriteLine("Profile List Problem!");
                    }
                    break;
            }
        }

       private void UpdateProfileList(string profile_type, string list)
       {
           switch(profile_type)
           {
               case "global":
                   UpdateProfileGlobalList(list);
                   break;
               case "tx":
                   UpdateProfileTxList(list);
                   break;
               case "mic":
                   UpdateProfileMicList(list);
                   break;
               case "displays":
                   UpdateProfileDisplayList(list);
                   break;
           }
       }         

        #endregion

        #region GPS Routines

        private bool _gpsInstalled;
        /// <summary>
        /// True if a GPS unit is installed in the radio
        /// </summary>
        public bool GPSInstalled
        {
            get { return _gpsInstalled; }
            internal set
            {
                if (_gpsInstalled != value)
                {
                    _gpsInstalled = value;
                    RaisePropertyChanged("GPSInstalled");
                }
            }
        }

        /// <summary>
        /// Installs the GPS unit on the radio.  Check if a GPS is
        /// installed using the property GPSInstalled
        /// </summary>
        public void GPSInstall()
        {
            SendCommand("radio gps install");
        }

        /// <summary>
        /// Uninstalls the GPS unit on the radio.  Check if a GPS is 
        /// installed using the property GPSInstalled
        /// </summary>
        public void GPSUninstall()
        {
            SendCommand("radio gps uninstall");
        }

        private string _gpsLatitude;
        /// <summary>
        /// Gets the GPS Latitude as a string
        /// </summary>
        public string GPSLatitude
        {
            get { return _gpsLatitude; }
            internal set
            {
                if (_gpsLatitude != value)
                {
                    _gpsLatitude = value;
                    RaisePropertyChanged("GPSLatitude");
                }
            }
        }

        private string _gpsLongitude;
        /// <summary>
        /// Gets the GPS Longitude as a string
        /// </summary>
        public string GPSLongitude
        {
            get { return _gpsLongitude; }
            internal set
            {
                if (_gpsLongitude != value)
                {
                    _gpsLongitude = value;
                    RaisePropertyChanged("GPSLongitude");
                }
            }
        }

        private string _gpsGrid;
        /// <summary>
        /// Gets the GPS Grid as a string
        /// </summary>
        public string GPSGrid
        {
            get { return _gpsGrid; }
            internal set
            {
                if (_gpsGrid != value)
                {
                    _gpsGrid = value;
                    RaisePropertyChanged("GPSGrid");
                }
            }
        }

        private string _gpsAltitude;
        /// <summary>
        /// Gets the GPS Altitude as a string
        /// </summary>
        public string GPSAltitude
        {
            get { return _gpsAltitude; }
            set
            {
                if (_gpsAltitude != value)
                {
                    _gpsAltitude = value;
                    RaisePropertyChanged("GPSAltitude");
                }
            }
        }

        private string _gpsSatellitesTracked;
        /// <summary>
        /// Gets the GPS satellites tracked as a string
        /// </summary>
        public string GPSSatellitesTracked
        {
            get { return _gpsSatellitesTracked; }
            internal set
            {
                if (_gpsSatellitesTracked != value)
                {
                    _gpsSatellitesTracked = value;
                    RaisePropertyChanged("GPSSatellitesTracked");
                }
            }
        }

        private string _gpsSatellitesVisible;
        /// <summary>
        /// Gets the GPS satellites visible as a string
        /// </summary>
        public string GPSSatellitesVisible
        {
            get { return _gpsSatellitesVisible; }
            internal set
            {
                if (_gpsSatellitesVisible != value)
                {
                    _gpsSatellitesVisible = value;
                    RaisePropertyChanged("GPSSatellitesVisible");
                }
            }
        }

        private string _gpsSpeed;
        /// <summary>
        /// Gets the GPS speed as a string
        /// </summary>
        public string GPSSpeed
        {
            get { return _gpsSpeed; }
            internal set
            {
                if (_gpsSpeed != value)
                {
                    _gpsSpeed = value;
                    RaisePropertyChanged("GPSSpeed");
                }
            }
        }

        private string _gpsFreqError;
        /// <summary>
        /// Gets the GPS frequency error as a string
        /// </summary>
        public string GPSFreqError
        {
            get { return _gpsFreqError; }
            internal set
            {
                if (_gpsFreqError != value)
                {
                    _gpsFreqError = value;
                    RaisePropertyChanged("GPSFreqError");
                }
            }
        }

        private string _gpsStatus;
        /// <summary>
        /// Gets the GPS status as a string
        /// </summary>
        public string GPSStatus
        {
            get { return _gpsStatus; }
            internal set
            {
                if (_gpsStatus != value)
                {
                    _gpsStatus = value;
                    RaisePropertyChanged("GPSStatus");
                }
            }
        }

        private string _gpsUtcTime;
        /// <summary>
        /// Gets the GPS UTC time as a string
        /// </summary>
        public string GPSUtcTime
        {
            get { return _gpsUtcTime; }
            internal set
            {
                if (_gpsUtcTime != value)
                {
                    _gpsUtcTime = value;
                    RaisePropertyChanged("GPSUtcTime");
                }
            }
        }

        private void ParseGPSStatus(string s)
        {
            string[] words = s.Split('#');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Radio::ParseGPSStatus: Invalid key/value pair (" + kv + ")");
                    continue;
                }

                string key = tokens[0];
                string value = tokens[1];

                switch (key.ToLower())
                {
                    case "lat":
                        {
                            GPSLatitude = value;
                            break;
                        }

                    case "lon":
                        {
                            GPSLongitude = value;
                            break;
                        }

                    case "grid":
                        {
                            GPSGrid = value;
                            break;
                        }

                    case "altitude":
                        {
                            GPSAltitude = value;
                            break;
                        }

                    case "tracked":
                        {
                            GPSSatellitesTracked = value;
                            break;
                        }

                    case "visible":
                        {
                            GPSSatellitesVisible = value;
                            break;
                        }

                    case "speed":
                        {
                            GPSSpeed = value;
                            break;
                        }

                    case "freq_error":
                        {
                            GPSFreqError = value;
                            break;
                        }

                    case "status":
                        {
                            GPSStatus = value;
                            break;
                        }

                    case "time":
                        {
                            GPSUtcTime = value;
                            break;
                        }
                }
            }
        }

        #endregion

        #region Mixer Routines

        private int _lineoutGain;
        /// <summary>
        /// The line out gain value from 0 to 100
        /// </summary>
        public int LineoutGain
        {
            get { return _lineoutGain; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain < 0) new_gain = 0;
                if (new_gain > 100) new_gain = 100;

                if (_lineoutGain != new_gain)
                {
                    _lineoutGain = new_gain;
                    SendCommand("mixer lineout gain " + _lineoutGain);
                    RaisePropertyChanged("LineoutGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("LineoutGain");
                }
            }
        }

        private bool _lineoutMute;
        /// <summary>
        /// Mutes or unmutes the lineout output
        /// </summary>
        public bool LineoutMute
        {
            get { return _lineoutMute; }
            set
            {
                if (_lineoutMute != value)
                {
                    _lineoutMute = value;
                    SendCommand("mixer lineout mute " + Convert.ToByte(_lineoutMute));
                    RaisePropertyChanged("LineoutMute");
                }
            }
        }

        private int _headphoneGain;
        /// <summary>
        /// The headphone gain value from 0 to 100
        /// </summary>
        public int HeadphoneGain
        {
            get { return _headphoneGain; }
            set
            {
                int new_gain = value;

                // check limits
                if (new_gain < 0) new_gain = 0;
                if (new_gain > 100) new_gain = 100;

                if (_headphoneGain != new_gain)
                {
                    _headphoneGain = new_gain;
                    SendCommand("mixer headphone gain " + _headphoneGain);
                    RaisePropertyChanged("HeadphoneGain");
                }
                else if (new_gain != value)
                {
                    RaisePropertyChanged("HeadphoneGain");
                }
            }
        }

        private bool _headphoneMute;
        /// <summary>
        /// Mutes or unmutes the headphone output
        /// </summary>
        public bool HeadphoneMute
        {
            get { return _headphoneMute; }
            set
            {
                if (_headphoneMute != value)
                {
                    _headphoneMute = value;
                    SendCommand("mixer headphone mute " + Convert.ToByte(_headphoneMute));
                    RaisePropertyChanged("HeadphoneMute");
                }
            }
        }

        #endregion

        #region Update Routines

        private bool _updateFailed = false;
        public bool UpdateFailed
        {
            get { return _updateFailed; }
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="update_filename"></param>

        public void SendUpdateFile(string update_filename)
        {
            Thread t = new Thread(new ParameterizedThreadStart(Private_SendUpdateFile));
            t.Name = "Update File Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.Start(update_filename);
        }

        public void SendSSDRWaveformFile(string wave_filename)
        {
            Thread t = new Thread(new ParameterizedThreadStart(Private_SendSSDRWaveformFile));
            t.Name = "Waveform File Thread";
            t.Priority = ThreadPriority.Normal;
            t.Start(wave_filename);
        }

        public void SendDBImportFile(string database_filename)
        {
            Thread t = new Thread(new ParameterizedThreadStart(Private_SendDatabaseFile));
            t.Name = "Database Import File Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;
            t.Start(database_filename);
        }

        public void SendMemoryImportFile(string memory_filename)
        {
            Thread t = new Thread(new ParameterizedThreadStart(Private_SendMemoryFile));
            t.Name = "Memory Import File Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;
            t.Start(memory_filename);
        }


        private void StartMeterProcessThread()
        {
            _meterProcessThread = new Thread(new ThreadStart(ProcessMeterDataPacket_ThreadFunction));
            _meterProcessThread.Name = "Meter Packet Processing Thread";
            _meterProcessThread.Priority = ThreadPriority.Normal;
            _meterProcessThread.IsBackground = true;
            _meterProcessThread.Start();
        }

        private void StartFFTProcessThread()
        {
            _fftProcessThread = new Thread(new ThreadStart(ProcessFFTDataPacket_ThreadFunction));
            _fftProcessThread.Name = "FFT Packet Processing Thread";
            _fftProcessThread.Priority = ThreadPriority.Normal;
            _fftProcessThread.IsBackground = true;
            _fftProcessThread.Start();
        }

        //private void StartParseReadThread()
        //{
        //    // ensure this thread only gets started once
        //    if (_parseReadThread != null)
        //        return;

        //    _parseReadThread = new Thread(new ThreadStart(ParseRead_ThreadFunction));
        //    _parseReadThread.Name = "Status Message Processing Thread";
        //    _parseReadThread.Priority = ThreadPriority.Normal;
        //    _parseReadThread.IsBackground = true;
        //    _parseReadThread.Start();
        //}

        // this function no longer is used.  We will get the profile list from the client's copy of the list.
        //public void ReceiveDBMetaFile(string file_name)
        //{
        //    Thread t = new Thread(new ParameterizedThreadStart(Private_GetDBMetaFile));
        //    t.Name = "Database Meta Data File Thread";
        //    t.Priority = ThreadPriority.BelowNormal;
        //    t.IsBackground = true;
        //    t.Start(file_name);
        //}

        public void ReceiveSSDRDatabaseFile(string meta_subset_path, string destination_path, bool memories_export_checked)
        {
            
            Thread t = new Thread(new ParameterizedThreadStart(Private_GetSSDRDatabaseFile));
            t.Name = "Database Database File Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.IsBackground = true;

            List<string> path_list = new List<string>();
            path_list.Add(meta_subset_path);
            path_list.Add(destination_path);
            if (memories_export_checked)
                path_list.Add("CSV");

            t.Start(path_list);
        }

        private string _databaseExportException;
        public string DatabaseExportException
        {
            get { return _databaseExportException; }
            set
            {
                _databaseExportException = value;
                RaisePropertyChanged("DatabaseExportException");
            }
        }

        private bool _databaseExportComplete = true;
        public bool DatabaseExportComplete
        {
            get { return _databaseExportComplete; }
            set
            {
                if (_databaseExportComplete != value)
                {
                    _databaseExportComplete = value;
                    RaisePropertyChanged("DatabaseExportComplete");
                }
            }
        }

        private bool _databaseImportComplete = true;
        public bool DatabaseImportComplete
        {
            get { return _databaseImportComplete; }
            set
            {
                _databaseImportComplete = value;
                RaisePropertyChanged("DatabaseImportComplete");
            }
        }

        private bool _unityResultsImportComplete = true;
        public bool UnityResultsImportComplete
        {
            get { return _unityResultsImportComplete; }
            set
            {
                _unityResultsImportComplete = value;
            }
        }

        private void Private_GetSSDRDatabaseFile(object obj)
        {
            if (obj == null)
            {
                Debug.WriteLine("Null object passed into GetSSDRDatabaseFile");
                return;
            }

            List<string> path_list = (List<string>)obj;
            DatabaseExportComplete = false;
            _metaSubsetTransferComplete = false;
            /* Index 0 contains the meta_subset path */
            Private_SendMetaSubsetFile(path_list[0]);

            int timeout = 0;
            while (_metaSubsetTransferComplete == false && timeout < 50)
            {
                Thread.Sleep(100);
                timeout++;
            }

            if (timeout >= 50)
            {
                Debug.WriteLine("Export SSDR Database File: Could not send meta_subset file");
                DatabaseExportComplete = true;
                return;
            }

            SendReplyCommand(new ReplyHandler(UpdateReceivePort), "file download db_package");

            timeout = 0;
            while (_receive_port == -1 && timeout++ < 100)
                Thread.Sleep(100);

            if (_receive_port == -1)
                _receive_port = 42607;

            string timestamp_string = "";
            string config_file = "";
            string memories_file = "";

            TcpClient client = null;
            FileStream file_stream = null;
            FileStream memories_stream = null;
            TcpListener server = null;

            try
            {
                /* Open meta_data file tinto a file stream */
                /* path_list[1] is the destination directory */
                DateTime timestamp = DateTime.UtcNow;

                //Filename format: SSDR_Config_08-04-14_3.16_PM.ssdr_cfg
                timestamp_string = timestamp.ToLocalTime().ToString("MM-dd-yy_") + timestamp.ToLocalTime().ToShortTimeString().Replace(":", ".").Replace(" ", "_");

                config_file = path_list[1] + "\\SSDR_Config_" + timestamp_string + ".ssdr_cfg";
                file_stream = File.Create(config_file);


                IPAddress ip = IPAddress.Any;

                server = new TcpListener(ip, _receive_port);

                /* Start Listening */
                server.Start();

                Byte[] bytes = new Byte[1500];

                Debug.WriteLine("Listening for SSDR_Database file");

                /* Blocking call to accept requests */
                client = server.AcceptTcpClient();
                Debug.WriteLine("Connected to client! ");

                /* Get stream object */
                NetworkStream stream = client.GetStream();

                /* Loop to receive all the data sent by the client */
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    /* Translate bytes to ascii string */
                    //data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    //Debug.WriteLine("Received : " + data);
                    file_stream.Write(bytes, 0, i);
                }
                file_stream.Close();
                /* Extract memories file */
                if (path_list.Contains("CSV"))
                {
                    /* Kind of a hacky way of doing this but I didn't feel like making a helper class just to be 
                     * able to pass in whether the Memories export is passed */

                    memories_file = path_list[1] + "\\SSDR_Memories_" + timestamp_string + ".csv";
                    memories_stream = File.Create(memories_file);

                    using (ZipFile zip = ZipFile.Read(config_file))
                    {
                        ZipEntry entry = zip["memories.csv"];
                        if (entry != null)
                            entry.Extract(memories_stream);
                    }

                    memories_stream.Close();
                }

                Debug.WriteLine("Finished getting SSDR_Database file");
            }
            catch (SocketException e)
            {
                Debug.WriteLine("SocketException: {0}", e);
                DatabaseExportException = "Network connection error.  Please check your network settings and try again.";
            }
            catch (UnauthorizedAccessException e)
            {
                Debug.WriteLine("UnauthorizedAccessException: {0}", e);
                DatabaseExportException = "Unauthorized access to export location \n\n" + path_list[1] +
                    "\n\nPlease check permissions or select a different folder.";
            }
            catch (DirectoryNotFoundException e)
            {
                Debug.WriteLine("DirectoryNotFoundException: {0}", e);
                DatabaseExportException = "Directory \n\n" + path_list[1] + "\n\n not found.  Please select a valid directory.";
            }
            catch (Exception e)
            {
                Debug.WriteLine("Caught exception: {0}", e);
                DatabaseExportException = "Configuration export failed.\n\n" + e.ToString();
            }
            finally
            {
                if ( client != null ) 
                    client.Close();
                if ( server != null )
                    server.Stop();

                if (memories_stream != null)
                    memories_stream.Close();

                DatabaseExportComplete = true;
            }
        }

        public bool ReceiveTestingResultsFile(object obj)
        {
            string dest_file_full_path = (string)obj;

            SendReplyCommand(new ReplyHandler(UpdateReceivePort), "file download unity_test");

            int timeout = 0;
            while (_receive_port == -1 && timeout++ < 100)
                Thread.Sleep(100);

            if (_receive_port == -1)
            {
                Console.WriteLine("Was not able to Update Recieve Port, setting port to 42607.");
                _receive_port = 42607;
            }

            string unity_file = "";
            TcpClient client = null;
            FileStream file_stream = null;
            TcpListener server = null;

            try
            {
                unity_file = dest_file_full_path;
                file_stream = File.Create(unity_file);
                IPAddress ip = IPAddress.Any;
                server = new TcpListener(ip, _receive_port);

                /* Start Listening */
                server.Start();

                Byte[] bytes = new Byte[1500];

                Console.WriteLine("Waiting to accept TCP Client on port " + _receive_port);

                /* Blocking call to accept requests */

                timeout = 0;
                while (timeout++ < 100 && !server.Pending())
                {
                    Thread.Sleep(100);
                }

                if (!server.Pending())
                {
                    Console.WriteLine("Error, there were no pending TCP client requests. Exiting.");
                    return false;
                }
                client = server.AcceptTcpClient();
                Console.WriteLine("Connected to client! Getting Unity Output file");

                /* Get stream object */
                NetworkStream stream = client.GetStream();

                /* Loop to receive all the data sent by the client */
                int i;
                while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
                {
                    /* Translate bytes to ascii string */
                    //data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
                    //Debug.WriteLine("Received : " + data);
                    file_stream.Write(bytes, 0, i);
                }

                file_stream.Close();
                Console.WriteLine("Finished getting Unity Output file");
                UnityResultsImportComplete = true;
                return true;
            }
            catch (SocketException e)
            {
                Console.WriteLine("SocketException: {0}", e);
                DatabaseExportException = "Network connection error.  Please check your network settings and try again.";
            }
            catch (UnauthorizedAccessException e)
            {
                Console.WriteLine("UnauthorizedAccessException: {0}", e);
                DatabaseExportException = "Unauthorized access to export location \n\n" + dest_file_full_path +
                    "\n\nPlease check permissions or select a different folder.";
            }
            catch (DirectoryNotFoundException e)
            {
                Console.WriteLine("DirectoryNotFoundException: {0}", e);
                DatabaseExportException = "Directory \n\n" + dest_file_full_path + "\n\n not found.  Please select a valid directory.";
            }
            catch (Exception e)
            {
                Console.WriteLine("Caught exception: {0}", e);
                DatabaseExportException = "Configuration export failed.\n\n" + e.ToString();
            }
            finally
            {
                if (file_stream != null)
                    file_stream.Close();

                if (client != null)
                    client.Close();

                if (server != null)
                    server.Stop();                
            }

            return false;
        }

       // We no longer use this function.  We get the current list
       // of profiles from the client, not from the radio
       
       //private void Private_GetDBMetaFile(object obj)
       //{
       //    _exportMetaData_Received = false;

       //    string file_name = (string)obj;

       //    SendReplyCommand(new ReplyHandler(UpdateReceivePort), "file download db_meta_data");

       //    int timeout = 0;
       //    while (_receive_port == -1 && timeout++ < 100)
       //        Thread.Sleep(100);

       //    if (_receive_port == -1)
       //        _receive_port = 42607;

       //    try
       //    {
       //        /* Open meta_data file tinto a file stream */
       //        FileStream file_stream = File.Create("meta_data");
       //    }
       //    catch
       //    {
       //        Debug.WriteLine("Database Meta Data Download: Error opening meta_data file for writing");
       //    }

       //    try
       //    {
       //        IPAddress ip = IPAddress.Any;

       //        TcpListener server = new TcpListener(ip, _receive_port);

       //        /* Start Listening */
       //        server.Start();

       //        Byte[] bytes = new Byte[1500];
       //        String data = null;


       //        Debug.WriteLine("Listening for meta data file");

       //        /* Blockign call to accept requests */
       //        TcpClient client = server.AcceptTcpClient();
       //        Debug.WriteLine("Connected to client! ");

       //        data = null;

       //        /* Get stream object */
       //        NetworkStream stream = client.GetStream();

       //        using (StreamWriter sw = File.CreateText(file_name))
       //        {

       //            /* Loop to receive all the data sent by the client */
       //            int i;
       //            while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
       //            {
       //                /* Translate bytes to ascii string */
       //                data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
       //                sw.WriteLine(data);
       //                Debug.WriteLine("Received : " + data);
       //            }

       //            sw.Close();
       //        }
       //        client.Close();
       //        server.Stop();
       //        ExportMetaData_Received = true;
       //    }
       //    catch (SocketException e)
       //    {
       //        Debug.WriteLine("SocketException: {0}", e);
       //    }
       //}

       private void Private_SendMetaSubsetFile(object obj)
       {
           string meta_subset_filename = (string)obj;

           // check to make sure the file exists
           if (!File.Exists(meta_subset_filename))
           {
               Debug.WriteLine("Database Import: Database file does not exist (" + meta_subset_filename + ")");
               return;
           }

           // read the file contents into a byte buffer to be sent via TCP
           byte[] file_buffer;
           FileStream stream = null;
           try
           {
               // open the file into a file stream
               stream = File.OpenRead(meta_subset_filename);

               // allocate a buffer large enough for the file
               file_buffer = new byte[stream.Length];

               // read the entire contents of the file into the buffer
               stream.Read(file_buffer, 0, (int)stream.Length);
           }
           catch (Exception)
           {
               Debug.WriteLine("Database Export: Error reading the meta_subset file");
               return;
           }
           finally
           {
               // cleanup -- close the stream
               stream.Close();
           }

           // create a TCP client to send the data to the radio
           TcpClient tcp_client = null;
           NetworkStream tcp_stream = null;

           string filename = meta_subset_filename.Substring(meta_subset_filename.LastIndexOf("\\") + 1);

           SendReplyCommand(new ReplyHandler(UpdateUpgradePort), "file upload " + file_buffer.Length + " db_meta_subset");

           int timeout = 0;
           while (_upgrade_port == -1 && timeout++ < 100)
               Thread.Sleep(100);

           if (_upgrade_port == -1)
               _upgrade_port = 4995;

           if (timeout < 2)
               Thread.Sleep(200); // wait for the server to get setup and be ready to accept the connection

           // connect to the radio's upgrade port
           try
           {
               // create tcp client object and connect to the radio
               tcp_client = new TcpClient();
               Debug.WriteLine("Opening TCP Database Export port " + _upgrade_port.ToString());
               //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
               tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
               tcp_stream = tcp_client.GetStream();
           }
           catch (Exception)
           {
               // lets try again on the new known update port if radio does not reply with proper response
               _upgrade_port = 42607;
               tcp_client.Close(); // ensure the previous object is closed to prevent orphaning the resource

               tcp_client = new TcpClient();
              
               try
               {
                   Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                   tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                   tcp_stream = tcp_client.GetStream();
               }
               catch (Exception)
               {
                   Debug.WriteLine("Update: Error opening the update TCP client");
                   tcp_client.Close();
                   return;
               }
           }

           // send the data over TCP
           try
           {
               tcp_stream.Write(file_buffer, 0, file_buffer.Length);
           }
           catch (Exception)
           {
               Debug.WriteLine("Update: Error sending the update buffer over TCP");
               return;
           }
           finally
           {
               // clean up the upgrade TCP connection
               tcp_client.Close();
           }           

           // note: removing this delay causes both the radio and client to crash on the next line
           Thread.Sleep(5000); // wait 5 seconds, then disconnect

           _metaSubsetTransferComplete = true;

           // close main command channel too since the radio will reboot
           //if (_tcp_client != null)
           //{
           //    _tcp_client.Close();
           //    _tcp_client = null;
           //}

           // if we get this far, the file contents have been sent
       }

        //private void Private_GetDBMetaFile(object obj)
        //{
        //    SendReplyCommand(new ReplyHandler(UpdateReceivePort), "file download db_meta_data");

        //    int timeout = 0;
        //    while (_receive_port == -1 && timeout++ < 100)
        //        Thread.Sleep(100);

        //    if (_receive_port == -1)
        //        _receive_port = 42607;

        //    try
        //    {
        //        /* Open meta_data file tinto a file stream */
        //        FileStream file_stream = File.Create("meta_data");
        //    }
        //    catch
        //    {
        //        Debug.WriteLine("Database Meta Data Download: Error opening meta_data file for writing");
        //    }

        //    try
        //    {
        //        IPAddress ip = IPAddress.Any;

        //        TcpListener server = new TcpListener(ip, _receive_port);

        //        /* Start Listening */
        //        server.Start();

        //        Byte[] bytes = new Byte[1500];
        //        String data = null;


        //        Debug.WriteLine("Listening for meta data file");

        //        /* Blockign call to accept requests */
        //        TcpClient client = server.AcceptTcpClient();
        //        Debug.WriteLine("Connected to client! ");

        //        data = null;

        //        /* Get stream object */
        //        NetworkStream stream = client.GetStream();

        //        /* Loop to receive all the data sent by the client */
        //        int i;
        //        while ((i = stream.Read(bytes, 0, bytes.Length)) != 0)
        //        {
        //            /* Translate bytes to ascii string */
        //            data = System.Text.Encoding.ASCII.GetString(bytes, 0, i);
        //            Debug.WriteLine("Received : " + data);
        //        }
        //        client.Close();
        //        server.Stop();
        //    }
        //    catch (SocketException e)
        //    {
        //        Debug.WriteLine("SocketException: {0}", e);
        //    }
           
        //}

        private void Private_SendDatabaseFile(object obj)
        {
            string database_filename = (string)obj;
            //DatabaseImportComplete = false;

            // check to make sure the file exists
            if (!File.Exists(database_filename))
            {
                Debug.WriteLine("Database Import: Database file does not exist (" + database_filename + ")");
                return;
            }

            // read the file contents into a byte buffer to be sent via TCP
            byte[] update_file_buffer;
            FileStream stream = null;
            try
            {
                // open the file into a file stream
                stream = File.OpenRead(database_filename);

                // allocate a buffer large enough for the file
                update_file_buffer = new byte[stream.Length];

                // read the entire contents of the file into the buffer
                stream.Read(update_file_buffer, 0, (int)stream.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Database Import: Error reading the database file");
                return;
            }
            finally
            {
                // cleanup -- close the stream
                stream.Close();
            }

            // create a TCP client to send the data to the radio
            TcpClient tcp_client = null;
            NetworkStream tcp_stream = null;

            string filename = database_filename.Substring(database_filename.LastIndexOf("\\") + 1);
           
            SendReplyCommand(new ReplyHandler(UpdateUpgradePort), "file upload " + update_file_buffer.Length + " db_import");

            int timeout = 0;
            while (_upgrade_port == -1 && timeout++ < 100)
                Thread.Sleep(100);

            if (_upgrade_port == -1)
                _upgrade_port = 4995;

            if (timeout < 2)
                Thread.Sleep(200); // wait for the server to get setup and be ready to accept the connection

            // connect to the radio's upgrade port
            try
            {
                // create tcp client object and connect to the radio
                tcp_client = new TcpClient();
                Debug.WriteLine("Opening TCP Database Import port " + _upgrade_port.ToString());
                //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
                tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                tcp_stream = tcp_client.GetStream();
            }
            catch (Exception)
            {
                // lets try again on the new known update port if radio does not reply with proper response
                _upgrade_port = 42607;
                tcp_client.Close(); // close the old object so we don't orphan the resource
                tcp_client = new TcpClient();
                
                try
                {
                    Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                    tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                    tcp_stream = tcp_client.GetStream();
                }
                catch (Exception)
                {
                    tcp_client.Close();
                    Debug.WriteLine("Update: Error opening the update TCP client");
                    return;
                }
            }

            // send the data over TCP
            try
            {
                tcp_stream.Write(update_file_buffer, 0, update_file_buffer.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error sending the update buffer over TCP");
                tcp_client.Close();
                return;
            }
                      

            // clean up the upgrade TCP connection
            tcp_client.Close();

            // note: removing this delay causes both the radio and client to crash on the next line
            Thread.Sleep(5000); // wait 5 seconds, then disconnect

            //DatabaseImportComplete = true;

            // close main command channel too since the radio will reboot
            //if (_tcp_client != null)
            //{
            //    _tcp_client.Close();
            //    _tcp_client = null;
            //}

            // if we get this far, the file contents have been sent

            
        }

        private void Private_SendMemoryFile(object obj)
        {
            string memory_filename = (string)obj;
            //DatabaseImportComplete = false;

            // check to make sure the file exists
            if (!File.Exists(memory_filename))
            {
                Debug.WriteLine("Memory Import: Memory file does not exist (" + memory_filename + ")");
                return;
            }

            // read the file contents into a byte buffer to be sent via TCP
            byte[] update_file_buffer;
            FileStream stream = null;
            try
            {
                // open the file into a file stream
                stream = File.OpenRead(memory_filename);

                // allocate a buffer large enough for the file
                update_file_buffer = new byte[stream.Length];

                // read the entire contents of the file into the buffer
                stream.Read(update_file_buffer, 0, (int)stream.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Memory Import: Error reading the memory file");
                return;
            }
            finally
            {
                // cleanup -- close the stream
                stream.Close();
            }

            // create a TCP client to send the data to the radio
            TcpClient tcp_client = null;
            NetworkStream tcp_stream = null;

            string filename = memory_filename.Substring(memory_filename.LastIndexOf("\\") + 1);

            SendReplyCommand(new ReplyHandler(UpdateUpgradePort), "file upload " + update_file_buffer.Length + " memories_csv_file");

            int timeout = 0;
            while (_upgrade_port == -1 && timeout++ < 100)
                Thread.Sleep(100);

            if (_upgrade_port == -1)
                _upgrade_port = 4995;

            if (timeout < 2)
                Thread.Sleep(200); // wait for the server to get setup and be ready to accept the connection

            // connect to the radio's upgrade port
            try
            {
                // create tcp client object and connect to the radio
                tcp_client = new TcpClient();
                Debug.WriteLine("Opening TCP Database Import port " + _upgrade_port.ToString());
                //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
                tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                tcp_stream = tcp_client.GetStream();
            }
            catch (Exception)
            {
                // lets try again on the new known update port if radio does not reply with proper response
                _upgrade_port = 42607;
                tcp_client.Close(); // close the old object so we don't orphan the resource
                tcp_client = new TcpClient();

                try
                {
                    Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                    tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                    tcp_stream = tcp_client.GetStream();
                }
                catch (Exception)
                {
                    tcp_client.Close();
                    Debug.WriteLine("Update: Error opening the update TCP client");
                    return;
                }
            }

            // send the data over TCP
            try
            {
                tcp_stream.Write(update_file_buffer, 0, update_file_buffer.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error sending the update buffer over TCP");
                tcp_client.Close();
                return;
            }


            // clean up the upgrade TCP connection
            tcp_client.Close();

            // note: removing this delay causes both the radio and client to crash on the next line
            Thread.Sleep(5000); // wait 5 seconds, then disconnect

            //DatabaseImportComplete = true;

            // close main command channel too since the radio will reboot
            //if (_tcp_client != null)
            //{
            //    _tcp_client.Close();
            //    _tcp_client = null;
            //}

            // if we get this far, the file contents have been sent


        }

        private void Private_SendSSDRWaveformFile(object obj)
        {
            string waveform_filename = (string)obj;

            // check to make sure the file exists
            if (!File.Exists(waveform_filename))
            {
                Debug.WriteLine("Update: Update file does not exist (" + waveform_filename + ")");
                return;
            }

            // TODO: verify file integrity

            // read the file contents into a byte buffer to be sent via TCP
            byte[] update_file_buffer;
            FileStream stream = null;
            try
            {
                // open the file into a file stream
                stream = File.OpenRead(waveform_filename);

                // allocate a buffer large enough for the file
                update_file_buffer = new byte[stream.Length];

                // read the entire contents of the file into the buffer
                stream.Read(update_file_buffer, 0, (int)stream.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error reading the upgrade file");
                return;
            }
            finally
            {
                // cleanup -- close the stream
                stream.Close();
            }


            // create a TCP client to send the data to the radio
            TcpClient tcp_client = null;
            NetworkStream tcp_stream = null;

            string filename = waveform_filename.Substring(waveform_filename.LastIndexOf("\\") + 1);
            SendCommand("file filename " + filename);
            SendReplyCommand(new ReplyHandler(UpdateUpgradePort), "file upload " + update_file_buffer.Length + " new_waveform");

            int timeout = 0;
            while (_upgrade_port == -1 && timeout++ < 100)
                Thread.Sleep(100);

            if (_upgrade_port == -1)
                _upgrade_port = 4995;

            if (timeout < 2)
                Thread.Sleep(200); // wait for the server to get setup and be ready to accept the connection

            // connect to the radio's upgrade port
            try
            {
                // create tcp client object and connect to the radio
                tcp_client = new TcpClient();
                Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
                tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                tcp_stream = tcp_client.GetStream();
            }
            catch (Exception)
            {
                // lets try again on the new known update port if radio does not reply with proper response
                _upgrade_port = 42607;
                tcp_client.Close(); // ensure the old object is disposed so we don't orphan it
                tcp_client = new TcpClient();

                try
                {
                    Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                    tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                    tcp_stream = tcp_client.GetStream();
                }
                catch (Exception)
                {
                    Debug.WriteLine("Update: Error opening the update TCP client");
                    tcp_client.Close();
                    return;
                }
            }

            // send the data over TCP
            try
            {
                tcp_stream.Write(update_file_buffer, 0, update_file_buffer.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error sending the update buffer over TCP");
                tcp_stream.Close();
                return;
            }

            // clean up the upgrade TCP connection
            tcp_client.Close();

            // note: removing this delay causes both the radio and client to crash on the next line
            Thread.Sleep(5000); // wait 5 seconds, then disconnect

            // close main command channel too since the radio will reboot
            //if (_tcp_client != null)
            //{
            //    _tcp_client.Close();
            //    _tcp_client = null;
            //}

            // if we get this far, the file contents have been sent
        }

        private void Private_SendUpdateFile(object obj)
        {
            string update_filename = (string)obj;

            // check to make sure the file exists
            if (!File.Exists(update_filename))
            {
                Debug.WriteLine("Update: Update file does not exist (" + update_filename + ")");
                return;
            }

            // TODO: verify file integrity

            // read the file contents into a byte buffer to be sent via TCP
            byte[] update_file_buffer;
            FileStream stream = null;
            try
            {
                // open the file into a file stream
                stream = File.OpenRead(update_filename);

                // allocate a buffer large enough for the file
                update_file_buffer = new byte[stream.Length];

                // read the entire contents of the file into the buffer
                stream.Read(update_file_buffer, 0, (int)stream.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error reading the upgrade file");
                return;
            }
            finally
            {
                // cleanup -- close the stream
                stream.Close();
            }


            // create a TCP client to send the data to the radio
            TcpClient tcp_client = null;
            NetworkStream tcp_stream = null;

            string filename = update_filename.Substring(update_filename.LastIndexOf("\\") + 1);
            SendCommand("file filename " + filename);
            SendReplyCommand(new ReplyHandler(UpdateUpgradePort), "file upload " + update_file_buffer.Length + " update");

            int timeout = 0;
            while (_upgrade_port == -1 && timeout++ < 100)
                Thread.Sleep(100);

            if (_upgrade_port == -1)
                _upgrade_port = 4995;

            if (timeout < 2)
                Thread.Sleep(200); // wait for the server to get setup and be ready to accept the connection

            // connect to the radio's upgrade port
            try
            {
                // create tcp client object and connect to the radio
                tcp_client = new TcpClient();
                Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
                tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                tcp_stream = tcp_client.GetStream();
            }
            catch (Exception)
            {
                // lets try again on the new known update port if radio does not reply with proper response
                _upgrade_port = 42607;
                tcp_client.Close(); // ensure the old object is disposed so we don't orphan it
                tcp_client = new TcpClient();

                try
                {
                    Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                    tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                    tcp_stream = tcp_client.GetStream();
                }
                catch (Exception)
                {
                    Debug.WriteLine("Update: Error opening the update TCP client");
                    tcp_client.Close();
                    return;
                }
            }

            _updating = true;

            // send the data over TCP
            try
            {
                tcp_stream.Write(update_file_buffer, 0, update_file_buffer.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error sending the update buffer over TCP");
                tcp_stream.Close();
                return;
            }

            // clean up the upgrade TCP connection
            tcp_client.Close();

            // note: removing this delay causes both the radio and client to crash on the next line
            Thread.Sleep(5000); // wait 5 seconds, then disconnect

            // close main command channel too since the radio will reboot
            //if (_tcp_client != null)
            //{
            //    _tcp_client.Close();
            //    _tcp_client = null;
            //}

            // if we get this far, the file contents have been sent
        }

        private bool _updating;
        internal bool Updating
        {
            get { return _updating; }
            set
            {
                _updating = value;
                UpdateConnectedState();
            }
        }

        private int _receive_port = -1;
        private void UpdateReceivePort(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            int temp;
            bool b = int.TryParse(s, out temp);

            if (!b)
            {
                Debug.WriteLine("Radio::UpgradeReceivePort-Error parsing Receive Port (" + s + ")");
                return;
            }
            else
            {
                _receive_port = temp;
                Debug.WriteLine("Receive Port updated to: " + s);
            }
        }

        private int _upgrade_port = -1;
        private void UpdateUpgradePort(int seq, uint resp_val, string s)
        {
            if (resp_val != 0) return;

            int temp;
            bool b = int.TryParse(s, out temp);

            if (!b)
            {
                Debug.WriteLine("Radio::UpdateUpgradePort-Error parsing Upgrade Port (" + s + ")");
                return;
            }
            else
            {
                _upgrade_port = temp;
                Debug.WriteLine("Upgrade Port updated to: " + s);
            }
        }

        private string _regionCode;
        /// <summary>
        /// Gets the region code of the radio as a string.
        /// </summary>
        public string RegionCode
        {
            get { return _regionCode; }
            internal set
            {
                if (_regionCode != value)
                {
                    _regionCode = value;
                    RaisePropertyChanged("RegionCode");
                }
            }
        }

        /// <summary>
        /// For internal use only.
        /// </summary>
        /// <param name="update_filename"></param>
        public void SendTurfFile(string update_filename)
        {
            Thread t = new Thread(new ParameterizedThreadStart(Private_SendTurfFile));
            t.Name = "Update File Thread";
            t.Priority = ThreadPriority.BelowNormal;
            t.Start(update_filename);
        }

        private void Private_SendTurfFile(object obj)
        {
            string turf_filename = (string)obj;

            // check to make sure the file exists
            if (!File.Exists(turf_filename))
            {
                Debug.WriteLine("Update: Update file does not exist (" + turf_filename + ")");
                return;
            }

            // TODO: verify file integrity

            // read the file contents into a byte buffer to be sent via TCP
            byte[] turf_file_buffer;
            FileStream stream = null;
            try
            {
                // open the file into a file stream
                stream = File.OpenRead(turf_filename);

                // allocate a buffer large enough for the file
                turf_file_buffer = new byte[stream.Length];

                // read the entire contents of the file into the buffer
                stream.Read(turf_file_buffer, 0, (int)stream.Length);
            }
            catch (Exception)
            {
                Debug.WriteLine("Update: Error reading the turf file");
                return;
            }
            finally
            {
                // cleanup -- close the stream
                stream.Close();
            }

            // create a TCP client to send the data to the radio
            TcpClient tcp_client = null;
            NetworkStream tcp_stream = null;

            SendReplyCommand(new ReplyHandler(UpdateUpgradePort), "file upload " + turf_file_buffer.Length + " turf");

            int timeout = 0;
            while (_upgrade_port == -1 && timeout++ < 10)
                Thread.Sleep(100);

            if (_upgrade_port == -1)
                _upgrade_port = 4995;

            if (timeout < 2)
                Thread.Sleep(200); // wait for the server to get setup and be ready to accept the connection

            // connect to the radio's upgrade port
            try
            {
                // create tcp client object and connect to the radio
                tcp_client = new TcpClient();
                //_tcp_client.NoDelay = true; // hopefully minimize round trip command latency
                tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                tcp_stream = tcp_client.GetStream();
            }
            catch (Exception)
            {
                // lets try again on the new known update port if radio does not reply with proper response
                _upgrade_port = 42607;
                tcp_client.Close(); // ensure the old object is disposed so it will not be orphaned
                tcp_client = new TcpClient();
                
                try
                {
                    Debug.WriteLine("Opening TCP upgrade port " + _upgrade_port.ToString());
                    tcp_client.Connect(new IPEndPoint(IP, _upgrade_port));
                    tcp_stream = tcp_client.GetStream();
                }
                catch (Exception)
                {
                    Debug.WriteLine("Update: Error opening the update TCP client");
                    tcp_client.Close();
                    return;
                }
            }

            // send the data over TCP
            try
            {
                tcp_stream.Write(turf_file_buffer, 0, turf_file_buffer.Length);
            }
            catch (Exception)
            {
                tcp_stream.Close();
                Debug.WriteLine("Update: Error sending the turf buffer over TCP");
                return;
            }

            // clean up the turf TCP connection
            tcp_client.Close();

            // if we get this far, the file contents have been sent
        }

        private void ParseUpdateStatus(string s)
        {
            string[] words = s.Split(' ');

            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("Update::StatusUpdate: Invalid key/value pair (" + kv + ")");
                    continue;
                }
                string key = tokens[0];
                string value = tokens[1];

                switch (key)
                {
                    case "failed":
                        {
                            byte fail;
                            bool b = byte.TryParse(value, out fail);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseUpdateStatus: Invalid value for failed(" + kv + ")");
                                continue;
                            }

                            _updateFailed = Convert.ToBoolean(fail);

                                // close main command channel too since the radio will reboot
                                if (_tcp_client != null)
                                {
                                    _tcp_client.Close();
                                    _tcp_client = null;
                                }

                            RaisePropertyChanged("UpdateFailed");
                            break;
                        }

                    case "reason":
                        {
                            Debug.WriteLine("Update faild for reason: " + value);
                            break;
                        }
                    case "transfer":
                        {
                            double temp;
                            bool b = StringHelper.DoubleTryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseUpdateStatus - transfer: Invalid value (" + kv + ")");
                                continue;
                            }

                            UploadStatus = temp;
                            break;
                        }
                }
            }
        }

        private void ParseTurfStatus(string s)
        {
            string[] words = s.Split(' ');

            switch (words[0].ToLower())
            {
                case "success":
                    {
                        GetInfo(); // update the region code since the turf upload succeeded
                        OnMessageReceived(MessageSeverity.Info, "Region change succeeded!");
                    }
                    break;

                case "fail":
                    {
                        string failure_mode = "";
                        if (words.Length > 1)
                            failure_mode = words[1];

                        OnMessageReceived(MessageSeverity.Error, "Region change failed: " + failure_mode);
                    }
                    break;
            }
        }

        private void ParseAPFStatus(string s)
        {
            string[] words = s.Split(' ');


            foreach (string kv in words)
            {
                string[] tokens = kv.Split('=');
                if (tokens.Length != 2)
                {
                    Debug.WriteLine("APF::StatusUpdate: Invalid key/value pair (" + kv + ")");
                    continue;
                }
                string key = tokens[0];
                string value = tokens[1];

                switch (key)
                {
                    case "mode":
                        int mode;
                        bool b = int.TryParse(value, out mode);
                        if (!b)
                        {
                            Debug.WriteLine("Radio::ParseAPFStatus: Invalid APF Mode (" + kv + ")");
                            continue;
                        }
                        if (mode == 0)
                            _apfMode = false;
                        else if (mode == 1)
                            _apfMode = true;
                        else
                        {
                            Debug.WriteLine("Radio::ParseAPFStatus: Invalid APF Mode Number (" + mode + ")");
                            continue;
                        }
                       
                        RaisePropertyChanged("APFMode");
                        break;
                    case "gain":
                        double temp;
                        b = StringHelper.DoubleTryParse(value, out temp);

                        if (!b)
                        {
                            Debug.WriteLine("Radio::ParseAPFStatus:: Invalid APFGain value (" + kv + ")");
                            continue;
                        }
                        _apfGain = temp;
                        RaisePropertyChanged("APFGain");
                        break;
                    case "qfactor":
                        b = StringHelper.DoubleTryParse(value, out temp);

                        if (!b)
                        {
                            Debug.WriteLine("Radio::ParseAPFStatus:: Invalid APFGain value (" + kv + ")");
                            continue;
                        }

                        _apfQFactor = temp;
                        RaisePropertyChanged("APFQFactor");
                        break;
                }
            }
        }


        private double _uploadStatus = 0.0;
        /// <summary>
        /// For internal use only.
        /// </summary>
        public double UploadStatus
        {
            get { return _uploadStatus; }
            internal set
            {
                _uploadStatus = value;
                RaisePropertyChanged("UploadStatus");
            }
        }

        #endregion

        #region Equalizer Routines
        /// <summary>
        /// Docs not available
        /// </summary>
        /// <param name="eq_select"></param>
        /// <returns></returns>
        public Equalizer CreateEqualizer(EqualizerSelect eq_select)
        {
            if (_equalizers.Count >= 2)
            {
                Equalizer eq = FindEqualizerByEQSelect(eq_select);
                if (eq == null) return null;
                eq.RequestEqualizerInfo();
                return eq;
            }
            else
            {
                return new Equalizer(this, eq_select);
            }
        }

        /// <summary>
        /// Docs not available
        /// </summary>
        /// <param name="eq_select"></param>
        public void RemoveEqualizer(EqualizerSelect eq_select)
        {
            lock (_equalizers)
            {
                Equalizer eq = FindEqualizerByEQSelect(eq_select);
                if (eq == null) return;

                eq.Remove();
                _equalizers.Remove(eq);
            }
        }

        public delegate void EqualizerAddedEventHandler(Equalizer eq);
        /// <summary>
        /// Docs not available
        /// </summary>
        public event EqualizerAddedEventHandler EqualizerAdded;

        private void OnEqualizerAdded(Equalizer eq)
        {
            if (EqualizerAdded != null)
                EqualizerAdded(eq);
        }

        public delegate void EqualizerRemovedEventHandler(Equalizer eq);
        /// <summary>
        /// Docs not available
        /// </summary>
        public event EqualizerRemovedEventHandler EqualizerRemoved;
        private bool _metaSubsetTransferComplete = false;

        private void OnEqualizerRemoved(Equalizer eq)
        {
            if (EqualizerRemoved != null)
                EqualizerRemoved(eq);
        }

        internal void AddEqualizer(Equalizer new_eq)
        {
            lock (_equalizers)
            {
                Equalizer eq = FindEqualizerByEQSelect(new_eq.EQ_select);
                if (eq != null) return; // already in the list

                _equalizers.Add(new_eq);
                OnEqualizerAdded(eq);
            }
        }

        /// <summary>
        /// Docs not available
        /// </summary>
        /// <param name="eq_select"></param>
        /// <returns></returns>
        public Equalizer FindEqualizerByEQSelect(EqualizerSelect eq_select)
        {
            lock (_equalizers)
            {
                foreach (Equalizer eq in _equalizers)
                {
                    if (eq.EQ_select == eq_select)
                    {
                        return eq;
                    }
                }
            }

            Debug.WriteLine("Radio: FindEqualizerByEQSelect() returned null");
            return null;
        }
        #endregion
        
        #region Xvtr Routines

        /// <summary>
        /// Create a new XVTR object
        /// </summary>
        /// <returns>A reference to the new XVTR object</returns>
        public Xvtr CreateXvtr()
        {
            return new Xvtr(this);
        }

        /// <summary>
        /// Find a Xvtr object by index number
        /// </summary>
        /// <param name="index">The index number for the XVTR</param>
        /// <returns></returns>
        public Xvtr FindXvtrByIndex(int index)
        {
            lock (_xvtrs)
            {
                foreach (Xvtr xvtr in _xvtrs)
                {
                    if (xvtr.Index == index)
                        return xvtr;
                }
            }

            return null;
        }

        internal void AddXvtr(Xvtr xvtr)
        {
            lock (_xvtrs)
            {
                if (_xvtrs.Contains(xvtr)) return;
                _xvtrs.Add(xvtr);
            }
            OnXvtrAdded(xvtr);
        }

        internal void RemoveXvtr(Xvtr xvtr)
        {
            lock (_xvtrs)
            {
                if (!_xvtrs.Contains(xvtr)) return;
                _xvtrs.Remove(xvtr);
            }
            OnXvtrRemoved(xvtr);
        }

        /// <summary>
        /// Delegate event handler for the XvtrRemoved event
        /// </summary>
        /// <param name="xvtr">The XVTR to be removed</param>
        public delegate void XvtrRemovedEventHandler(Xvtr xvtr);
        /// <summary>
        /// This event is raised when a XVTR is removed from the radio
        /// </summary>
        public event XvtrRemovedEventHandler XvtrRemoved;

        private void OnXvtrRemoved(Xvtr xvtr)
        {
            if (XvtrRemoved != null)
                XvtrRemoved(xvtr);
        }

        /// <summary>
        /// Delegate event handler for the XVTRAdded event
        /// </summary>
        /// <param name="xvtr">The XVTR object being added</param>
        public delegate void XvtrAddedEventHandler(Xvtr xvtr);
        /// <summary>
        /// This event is raised when a new XVTR has been added to the radio
        /// </summary>
        public event XvtrAddedEventHandler XvtrAdded;

        internal void OnXvtrAdded(Xvtr xvtr)
        {
            if (XvtrAdded != null)
                XvtrAdded(xvtr);
        }

        #endregion

        #region Memory Routines

        internal void AddMemory(Memory mem)
        {
            lock (_memoryList)
            {
                if (_memoryList.Contains(mem)) return;
                _memoryList.Add(mem);
            }
            RaisePropertyChanged("MemoryList");
        }

        internal void RemoveMemory(Memory mem)
        {
            lock (_memoryList)
            {
                if (!_memoryList.Contains(mem)) return;
                _memoryList.Remove(mem);            
                OnMemoryRemoved(mem);
            }
            RaisePropertyChanged("MemoryList");
        }

        /// <summary>
        /// Delegate event handler for the MemoryRemoved event
        /// </summary>
        /// <param name="mem">The Memory object being removed</param>
        public delegate void MemoryRemovedEventHandler(Memory mem);
        /// <summary>
        /// This event is raised when a Memory is removed from the radio
        /// </summary>
        public event MemoryRemovedEventHandler MemoryRemoved;

        private void OnMemoryRemoved(Memory mem)
        {
            if (MemoryRemoved != null)
                MemoryRemoved(mem);
        }

        /// <summary>
        /// Delegate event handler for the MemoryAdded event
        /// </summary>
        /// <param name="mem">The Memory object being added</param>
        public delegate void MemoryAddedEventHandler(Memory mem);
        /// <summary>
        /// This event is raised when a new Memory has been added to the radio
        /// </summary>
        public event MemoryAddedEventHandler MemoryAdded;

        internal void OnMemoryAdded(Memory mem)
        {
            if (MemoryAdded != null)
                MemoryAdded(mem);
        }

        /// <summary>
        /// Find a MEmory object by index number
        /// </summary>
        /// <param name="index">The index number for the Memory</param>
        /// <returns>The Memory object</returns>
        public Memory FindMemoryByIndex(int index)
        {
            lock (_memoryList)
            {
                foreach (Memory mem in _memoryList)
                {
                    if (mem.Index == index)
                        return mem;
                }
            }

            return null;
        }

        #endregion
        
        /// <summary>
        /// Get a reference to the CWX object
        /// </summary>
        /// <returns>CWX object</returns>
        public CWX GetCWX()
        {
            if (_cwx == null)
                _cwx = new CWX(this);

            return _cwx;
        }

        private bool _syncCWX = true;
        public bool SyncCWX
        {
            get { return _syncCWX; }
            set
            {
                if (_syncCWX != value)
                {
                    _syncCWX = value;
                    SendCommand("cw synccwx " + Convert.ToByte(_syncCWX));
                    RaisePropertyChanged("SyncCWX");
                }
            }
        }
        
        public bool GetTXFreq(out double freq_mhz)
        {
            freq_mhz = 0.0;

            // are there any slices?
            if (_slices.Count == 0) return false;

            // for each slice...
            foreach (Slice s in _slices)
            {
                // is this slice the Transmit slice?
                if (s.Transmit)
                {
                    freq_mhz = s.Freq;
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Overridden ToString() method for the Radio.cs class
        /// </summary>
        /// <returns>A string description of the radio object in the form of "{IP_Address} {Radio_model}: {Serial_number} {ID_string}"</returns>
        public override string ToString()
        {
            // to enable easy binding to ListBoxes, etc
            return _ip.ToString() + " " + _model + ": " + _serial + " ("+_nickname +" 0x" + _unique_id.ToString("X").PadLeft(8, '0') + ")";
        }
    }
}
