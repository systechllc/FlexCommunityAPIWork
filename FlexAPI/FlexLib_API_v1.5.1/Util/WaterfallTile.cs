// ****************************************************************************
///*!	\file WaterfallTile.cs
// *	\brief Represents a single Waterfall Tile object
// *
// *	\copyright	Copyright 2012-2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2014-03-11
// *	\author Eric Wachsmann, KE5DTO
// */
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;


namespace Flex.Util
{
    public class WaterfallTile
    {
        /// <summary>
        /// The frequency represented by the first bin in the object
        /// </summary>
        public VitaFrequency FirstPixelFreq { get; set; }
        public VitaFrequency BinBandwidth { get; set; }

        /// <summary>
        /// The length of time in milliseconds
        /// </summary>
        public uint LineDurationMS { get; set; }

        /// <summary>
        /// The width in pixels of the data provided in this tile
        /// </summary>
        private ushort _width;
        public ushort Width 
        {
            get { return _width; }
            set
            {
                _width = value;
                UpdateDataLength();
            }
        }

        /// <summary>
        /// The height in pixels of the data provided in this tile
        /// </summary>
        private ushort _height;
        public ushort Height
        {
            get { return _height; }
            set
            {
                _height = value;
                UpdateDataLength();
            }
        }

        /// <summary>
        /// An index relating the Tile to a relative time base
        /// </summary>
        public uint Timecode { get; set; }
        public uint AutoBlackLevel { get; set; }
        public ushort[] Data;

        private void UpdateDataLength()
        {
            if(_width == 0 || _height == 0) return;

            Data = new ushort[_width*_height];
        }

        public DateTime DateTime { get; set; }

        public override string ToString()
        {
            return Timecode + ": " + ((double)FirstPixelFreq).ToString("f6") + "  " + _width + "x" + _height + " " + LineDurationMS + "ms " + DateTime.ToString("hh:mm:ss.fff");
        }
    }
}
