// ****************************************************************************
///*!	\file TNF.cs
// *	\brief Represents a single Tracking Notch Filter (TNF)
// *
// *	\copyright	Copyright 2012-2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2014-03-25
// *	\author Ed Gonzalez
// */
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

using System.Diagnostics;
using System.ComponentModel;
using Flex.UiWpfFramework.Mvvm;
using System.Globalization;
using Flex.Util;


namespace Flex.Smoothlake.FlexLib
{
    public class TNF : ObservableObject
    {

        private Radio _radio;

        public TNF(Radio radio, uint tnf_id)
        {
            this._radio = radio;
            this._id = tnf_id;
        }

        public TNF(Radio radio, uint tnf_id, double freq)
        {
            this._radio = radio;
            this._id = tnf_id;
            this._frequency = freq;
        }

        public TNF(Radio radio, uint tnf_id, double freq, uint depth, double width, bool permanent)
        {
            this._radio = radio;
            this._id = tnf_id;
            this._frequency = freq;
            this._depth = depth;
            this._bandwidth = width;
            this._permanent = permanent;
        }

        private uint _id;
        public uint ID
        {
            get { return _id; }
        }

        private double _frequency;
        public double Frequency
        {
            get { return _frequency; }
            set
            {
                if (_frequency != value)
                {
                    _frequency = value;
                    _radio.SendCommand("tnf set " + _id + " freq=" + StringHelper.DoubleToString(_frequency, "f6"));
                    RaisePropertyChanged("Frequency");
                }
            }
        }

        private uint _depth = 1;
        public uint Depth
        {
            get { return _depth; }
            set
            {
                if (_depth != value)
                {
                    if (value > 3 || value < 1)
                    {
                        Debug.WriteLine("TNF " + _id + " Depth out of range " + value + " Ignoring");
                    }
                    else
                    {
                        _depth = value;
                        _radio.SendCommand("tnf set " + _id + " depth=" + _depth);
                        RaisePropertyChanged("Depth");
                    }
                }
            }
        }

        private bool _permanent = false;
        public bool Permanent
        {
            get { return _permanent; }
            set
            {
                if (_permanent != value)
                {
                    _permanent = value;
                    _radio.SendCommand("tnf set " + _id + " permanent=" + _permanent);
                    RaisePropertyChanged("Permanent");
                }
            }
        }


        private bool _radio_ack = false;
        public bool RadioAck
        {
            get { return _radio_ack; }
            internal set
            {
                if (_radio_ack != value)
                {
                    _radio_ack = value;
                    RaisePropertyChanged("RadioAck");
                }
            }
        }

        private double _bandwidth;
        public double Bandwidth
        {
            get { return _bandwidth; }
            set
            {
                if (value > 6000 * 1e-6 || value < 5 * 1e-6) //TODO: Revisit limits
                {
                    Debug.WriteLine("TNF " + _id + " Bandwidth out of range " + value + " Ignoring");
                }
                else
                {
                    _bandwidth = value;
                    _radio.SendCommand("tnf set " + _id + " width=" + StringHelper.DoubleToString(_bandwidth, "f6"));
                    RaisePropertyChanged("Bandwidth");
                }
            }

        }

        public override string ToString()
        {
            return "TNF: " + _id + " Freq:" + _frequency + " Depth: " + _depth + " Bandwidth" + _bandwidth;
        }

        public void StatusUpdate(string s)
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
                    case "freq":
                        {
                            double temp;
                            bool b = StringHelper.DoubleTryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTNFStatus - freq: Invalid value (" + kv + ")");
                                continue;
                            }

                            _frequency = temp;
                            RaisePropertyChanged("Frequency");

                            break;
                        }
                    case "depth":
                        {
                            uint temp;
                            bool b = uint.TryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTNFStatus - depth: Invalid value (" + kv + ")");
                                continue;
                            }

                            _depth = temp;
                            RaisePropertyChanged("Depth");

                            break;
                        }
                    case "width":
                        {
                            double temp;
                            bool b = StringHelper.DoubleTryParse(value, out temp);
                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTNFStatus - width: Invalid value (" + kv + ")");
                                continue;
                            }

                            _bandwidth = temp;
                            RaisePropertyChanged("Bandwidth");
                            break;
                        }
                    case "permanent":
                        {
                            byte temp;
                            bool b = byte.TryParse(value, out temp);

                            if (!b)
                            {
                                Debug.WriteLine("Radio::ParseTNFStatus - permanent: Invalid value (" + kv + ")");
                                continue;
                            }

                            _permanent = Convert.ToBoolean(temp);
                            RaisePropertyChanged("Permanent");
                        }
                        break;
                }
            }
            if (_id != 0 && _bandwidth != 0 && _frequency != 0 && Depth != 0)
            {
                RadioAck = true;
                _radio.OnTNFAdded(this);
            }
        }


        public bool RequestTNFFromRadio()
        {
            // check to see if this object has already been activated
            if (_radio_ack) return false;

            // check to ensure this object is tied to a radio object
            if (_radio == null) return false;

            // check to make sure the radio is connected
            if (!_radio.Connected) return false;

            // Send command
           // _radio.SendReplyCommand(new ReplyHandler(UpdateId), "tnf create " + _id + " freq=" + _frequency.ToString("f6"));
            //_radio.SendCommand("tnf create " + _id + " freq=" + _frequency.ToString("f6"));
            return true;
        }

        public void Remove(bool sendCommands)
        {
            if (sendCommands)
            {
                _radio.SendCommand("tnf remove " + _id);
                _radio.RemoveTNF(this);
            }
        }

        private void UpdateId(uint resp_val, string s)
        {
            if (resp_val != 0) return;

            bool b = uint.TryParse(s, out _id);

            if (!b)
            {
                Debug.WriteLine("TNF::UpdateId - Error parsing ID (" + s + ")");
                return;
            }

            _radio.AddTNF(this);
        }
    }

}

