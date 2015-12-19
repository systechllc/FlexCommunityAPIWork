using System;
using System.Collections.Generic;
using System.IO.Ports;
using System.Linq;
using System.Text;
using Cat.Cat.Interfaces;

namespace Cat.Cat.Ports
{
	public class SerialComSpec : ISerialComSpec
	{
		private int _baud_rate;
		private int _data_bits;
		private StopBits _stop_bits;
		private Parity _parity;


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
			set { _stop_bits = value; }
		}

		public Parity parity
		{
			get{return _parity; }
			set
			{ _parity = value; }
		}

		#region Documentation

		/*! \class Cat.Cat.Ports.SerialComSpec
		 * \brief       Holds Com parameters for serial ports. 
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		/*! \var    portNumber
		 *  \brief      The port number.
		 */

		/*! \var    baudRate
		 *  \brief      The baud rate.
		 */

		/*! \var    dataBits
		 *  \brief      The number of data bits.
		 */

		/*! \var    stopBits
		 *  \brief      The number of stop bits.
		 */

		/*! \var    parity
		 *  \brief      The parity.
		 */
		#endregion Documentation
	}
}
