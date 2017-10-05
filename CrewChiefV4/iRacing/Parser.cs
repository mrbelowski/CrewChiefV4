﻿using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CrewChiefV4.iRacing
{
    public static class Parser
    {
        public static double ParseTrackLength(string value)
        {
            // value = "6.93 km"
            double length = 0;

            var indexOfKm = value.IndexOf("km");
            if (indexOfKm > 0) value = value.Substring(0, indexOfKm);

            if (double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out length))
            {
                return length;
            }
            return 0;
        }

        public static int ParseInt(string value, int @default = 0)
        {
            int val;
            if (int.TryParse(value, out val)) return val;
            return @default;
        }

        public static float ParseFloat(string value, float @default = 0f)
        {
            float val;
            if (float.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture, out val)) return val;
            return @default;
        }

        public static double ParseSec(string value)
        {
            // value = "600.00 sec"
            double length = 0;

            var indexOfSec = value.IndexOf(" sec");
            if (indexOfSec > 0) value = value.Substring(0, indexOfSec);

            if (double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out length))
            {
                return length;
            }
            return 0;
        }

        public static Color ParseColor(string value)
        {
            if (!string.IsNullOrWhiteSpace(value) && value.StartsWith("0x"))
            {
                try
                {
                    var hex = value.Replace("0x", "#");
                    //int argb = Int32.Parse(hex.Replace("#", ""), NumberStyles.HexNumber);
                    return (Color)ColorConverter.ConvertFromString(hex);
                }
                catch (Exception)
                {
                }
            }

            return Colors.White;
        }
    }
}
