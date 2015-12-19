// ****************************************************************************
///*!	\file StringHelper.cs
// *	\brief Helps handle strings in various localizations
// *
// *	\copyright	Copyright 2012-2015 FlexRadio Systems.  All Rights Reserved.
// *				Unauthorized use, duplication or distribution of this software is
// *				strictly prohibited by law.
// *
// *	\date 2013-06-18
// *	\author Eric Wachsmann, KE5DTO
// */
// ****************************************************************************

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace Flex.Util
{
    public class StringHelper
    {
        #region Double

        public static bool DoubleTryParse(string s, out double result)
        {
            return double.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static string DoubleToString(double val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        public static string DoubleToString(double val, string format)
        {
            return val.ToString(format, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Float

        public static bool TryParseFloat(string s, out float result)
        {
            return float.TryParse(s, NumberStyles.Any, CultureInfo.InvariantCulture, out result);
        }

        public static string FloatToString(float val)
        {
            return val.ToString(CultureInfo.InvariantCulture);
        }

        public static string FloatToString(float val, string format)
        {
            return val.ToString(format, CultureInfo.InvariantCulture);
        }

        #endregion

        #region Misc

        public static void RemoveHexPrefix(string hexString, out string cleanString)
        {

            cleanString = String.Copy(hexString);

            int index = hexString.IndexOf("0x");
            cleanString = (index < 0) ? hexString : hexString.Remove(index, 2);

            index = hexString.IndexOf("0X");
            cleanString = (index < 0) ? hexString : hexString.Remove(index, 2);
        }
        #endregion

        public static string Sanitize(string input)
        {
            // ^ matches characters that are NOT in the set
            // alphanumeric, periods, commas, forwards shashes, dash
            return Regex.Replace(input, @"[^a-zA-Z0-9\.,/-]", string.Empty);
        }
    }
}
