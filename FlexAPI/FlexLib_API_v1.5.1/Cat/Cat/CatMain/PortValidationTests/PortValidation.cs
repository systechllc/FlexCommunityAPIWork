using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Cat.Cat.PortValidationTests
{
	class PortValidation
	{
		static int[] rates = { 1200, 2400, 4800, 9600, 19200, 38400, 
							   57600, 115200, 230400, 460800, 921600, 1382400 };
		static string[] parity = { "Even", "Odd", "None" };
		static string[] stop_bits = {"None", "One", "OnePointFive", "Two"};

		 //Validation of serial port number
		public static bool ValidPortRange(int port, int min, int max)
		{
			return (port >= min && port <= max);
		}

		//validation of baud rate
		public static bool ValidBaudRate(int baud_rate)
		{
			return rates.Contains(baud_rate);       
		}  

		//Validation of data bits
		public static bool ValidDataBits(int d_bits)
		{
			return (d_bits >= 5 || d_bits <= 8);
		}

		//Validation of parity
		public static bool ValidParity(string p)
		{
			return parity.Contains(p);
		}

		//Validation of stop bits
		public static bool ValidStopBits(string bits)
		{
			return stop_bits.Contains(bits);
		}

		//Validation of baud rate vs stop bits
		public static bool ValidCombo(int databits, string stopbits)
		{
			if ((databits == 5 && stopbits == "Two") || (stopbits == "OnePointFive" && databits > 5))
				return false;
			else return true;
		}

		#region Documentation

		/*! \class Cat.Cat.PortValidationTests.PortValidation
		 * \brief       Validation tests for the CAT serial ports.
		 * \author      Bob Tracy, K5KDN
		 * \version     0.1Alpha
		 * \date        01/2012
		 * \copyright   FlexRadio Systems
		 */

		#endregion Documentation

	}
}
