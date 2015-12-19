using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Cat.Cat.Interfaces;

namespace Cat.Cat.Ports
{
	class DefaultSerialComSpec : ISerialComSpec
	{
		private int _port_nmbr = 255;
		private int _baud_rate = 9600;
		private int _data_bits = 8;
		private StopBits _stop_bits = StopBits.One;
		private Parity _parity = Parity.None;

		public int portNumber
		{
			get { return _port_nmbr; }
			set { _port_nmbr = value; }
		}

		public int baudRate
		{
			get { return _baud_rate; }
			set { _baud_rate = value; }
		}

		public int dataBits
		{
			get { return _data_bits; }
			set { _data_bits = value; }
		}

		public StopBits stopBits
		{
			get { return _stop_bits; }
			set
			{ _stop_bits = value; }
		}

		public Parity parity
		{
			get { return _parity; }
			set { _parity = value; }
		}

		#region Documentation

		/*! \class Cat.Cat.Ports.DefaultSerialComSpec
		 * \brief       Default parameters if no ComSpec is specified in the XML file.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \var    portNumber
		 *  \brief      The default port number.
		 */

		/*! \var    baudRate
		 *  \brief      The default baud rate.
		 */

		/*! \var    dataBits
		 *  \brief      The default number of data bits.
		 */

		/*! \var    stopBits
		 *  \brief      The default number of stop bits.
		 */

		/*! \var    parity
		 *  \brief      The default parity.
		 */

		#endregion Documentation
	}
}
