using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.iRacing
{
    public class TrackConditions
    {
        public const float Tolerance = 0.1f;
        public static readonly int DefaultWeatherType = 0;
        public static readonly float DefaultAirDensity = 1.207f;
        public static readonly float DefaultAirPressure = 29.34f; 
        public static readonly float DefaultAirTemp = 25.556f; // C
        public static readonly int DefaultFogLevel = 0;
        public static readonly float DefaultHumidity = 0.55f;
        public static readonly int DefaultSkies = 1;
        public static readonly float DefaultTrackTemp = 39.8f; // C
        public static readonly float DefaultWindDir = 0;
        public static readonly float DefaultWindVel = 0.894f; // m/s
        public static readonly int DefaultTrackUsage = (int)TrackUsageTypes.ModeratelyLow;
        public static readonly bool DefaultMarbleCleanup = true;

        public static readonly int MinTemperatureF = 65; // F
        public static readonly int MaxTemperatureF = 90; // F
        public static readonly int MaxWindSpeedMph = 30; // mph
        public static readonly int MinTemperatureC = (int) TempToC(MinTemperatureF);
        public static readonly int MaxTemperatureC = (int) TempToC(MaxTemperatureF);
        public static readonly int MaxWindSpeedKph = (int) WindToKph(MaxWindSpeedMph);

        /// <summary>
        /// Converts temperature from degrees Celsius to Fahrenheit
        /// </summary>
        /// <param name="tempC">Temperature in degrees Celsius</param>
        /// <returns></returns>
        public static float TempToF(float tempC)
        {
            return tempC * 1.8f + 32f;
        }

        /// <summary>
        /// Converts temperature from Fahrenheit to degrees Celsius
        /// </summary>
        /// <param name="tempF">Temperature in Fahrenheit</param>
        /// <returns></returns>
        public static float TempToC(float tempF)
        {
            return (tempF - 32)/1.8f;
        }

        /// <summary>
        /// Converts wind speed from meters/second to miles/hour.
        /// </summary>
        /// <param name="wind">Wind speed in meters/second</param>
        /// <returns></returns>
        public static float WindToMph(float windMps)
        {
            return WindToMphFromKph(windMps * 3.6f);
        }

        /// <summary>
        /// Converts wind speed from kilometers/hour to miles/hour.
        /// </summary>
        /// <param name="wind">Wind speed in kilometers/hour</param>
        /// <returns></returns>
        public static float WindToMphFromKph(float windKph)
        {
            return windKph* 0.6213712f;
        }

        /// <summary>
        /// Converts wind speed from miles/hour to kilometers/hour.
        /// </summary>
        /// <param name="windMph">Wind speed in miles/hour</param>
        /// <returns></returns>
        public static float WindToKph(float windMph)
        {
            return windMph / 0.6213712f;
        }

        private static double WrapWindDir(double degrees)
        {
            while (degrees < 0) degrees += 360;
            while (degrees > 360) degrees -= 360;
            return degrees;
        }

        /// <summary>
        /// Converts wind direction from radians to degrees, optionally rounding to nearest "45-degree" cardinal direction (e.g. N, NE, E, SE, etc).
        /// </summary>
        /// <param name="dirRadians">Direction in radians</param>
        /// <param name="rounded">If true, rounds to nearest "45-degree" cardinal direction</param>
        /// <returns></returns>
        public static float WindDirToDegrees(float dirRadians, bool rounded = false)
        {
            var degrees = WrapWindDir(dirRadians*180/Math.PI);
            if (rounded)
                return (int) Math.Round(degrees/45f)*45f;
            return (float)degrees;
        }

        /// <summary>
        /// Converts wind direction from degrees to radians.
        /// </summary>
        /// <param name="dirDegrees">Direction in degrees (0 - 360)</param>
        /// <returns></returns>
        public static float WindDirToRadians(float dirDegrees)
        {
            var degrees = WrapWindDir(dirDegrees);
            return (float) (degrees*Math.PI/180f);
        }

        /// <summary>
        /// Converts wind direction in radians to the name of the corresponding cardinal direction (e.g. N, NE, E, SE, etc).
        /// </summary>
        /// <param name="dirRadians">Direction in radians</param>
        /// <returns></returns>
        public static string WindDirToCardinal(float dirRadians)
        {
            var degrees = WindDirToDegrees(dirRadians);

            var step = 45/2f;

            if (degrees > 360 - step || degrees < step)
                return "N";
            if (degrees < 45+step) 
                return "NE";
            if (degrees < 90+step)
                return "E";
            if (degrees < 135 + step)
                return "SE";
            if (degrees < 180 + step)
                return "S";
            if (degrees < 225 + step)
                return "SW";
            if (degrees < 270 + step)
                return "W";
            return "NW";
        }
        
        /// <summary>
        /// Converts wind direction in a string containing both the cardinal direction name (e.g. N, NE, E) and the actual direction in degrees.
        /// </summary>
        /// <param name="dirRadians">Direction in radians</param>
        /// <returns></returns>
        public static object WindDirToCardinalAndDegrees(float dirRadians)
        {
            var cardinal = WindDirToCardinal(dirRadians);
            var degrees = WindDirToDegrees(dirRadians, false);
            return string.Format("{0} ({1:0}°)", cardinal, degrees);
        }

        /// <summary>
        /// Converts sky type parameter (0-3) to corresponding sky type text (Clear, Partly Cloudy, etc).
        /// </summary>
        /// <param name="value">Sky type parameter (0 - 3)</param>
        /// <returns></returns>
        public static string SkiesFromValue(int value)
        {
            switch (value)
            {
                case 0:
                    return "Clear";
                case 1:
                    return "Partly cloudy";
                case 2:
                    return "Mostly cloudy";
                case 3:
                    return "Overcast";
            }
            return "(Unknown)";
        }

        /// <summary>
        /// Converts track usage session info string to TrackUsageTypes enum value.
        /// </summary>
        /// <param name="usage">Track usage string from session info (e.g. "low usage")</param>
        /// <returns></returns>
        public static TrackUsageTypes TrackUsageFromString(string usage)
        {
            switch (usage.ToLower().Trim())
            {
                case "clean": return TrackUsageTypes.Clean;
                case "low usage": return TrackUsageTypes.Low;
                case "slight usage": return TrackUsageTypes.Slight;
                case "moderately low usage": return TrackUsageTypes.ModeratelyLow;
                case "moderate usage": return TrackUsageTypes.Moderate;
                case "moderately high usage": return TrackUsageTypes.ModeratelyHigh;
                case "high usage": return TrackUsageTypes.High;
                case "extensive usage": return TrackUsageTypes.Extensive;
                case "maximum usage": return TrackUsageTypes.Maximum;
            }
            return TrackUsageTypes.Unknown;
        }

        /// <summary>
        /// Converts track usage value (from TrackUsageTypes enum) to display string (e.g. "low usage").
        /// </summary>
        /// <param name="usage">Track usage value</param>
        /// <returns></returns>
        public static string TrackUsageToString(TrackUsageTypes usage)
        {
            switch (usage)
            {
                case TrackUsageTypes.Clean:
                    return "clean";
                case TrackUsageTypes.Low:
                    return "low usage";
                case TrackUsageTypes.Slight:
                    return "slight usage";
                case TrackUsageTypes.ModeratelyLow:
                    return "moderately low usage";
                case TrackUsageTypes.Moderate:
                    return "moderate usage";
                case TrackUsageTypes.ModeratelyHigh:
                    return "moderately high usage";
                case TrackUsageTypes.High:
                    return "high usage";
                case TrackUsageTypes.Extensive:
                    return "extensive usage";
                case TrackUsageTypes.Maximum:
                    return "maximum usage";
            }
            return "(unknown setting)";
        }

        public enum TrackUsageTypes
        {
            Unknown = -1,
            Clean,
            Slight,
            Low,
            ModeratelyLow,
            Moderate,
            ModeratelyHigh,
            High,
            Extensive,
            Maximum
        }

    }
}
