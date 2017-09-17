using System;
using System.Collections.Generic;

namespace iRacingSDK
{
    public class FastLap
    {
        public SessionData._DriverInfo._Drivers Driver;
        public TimeSpan Time;

        public static bool operator ==(FastLap a, FastLap b)
        {
            if ((object)a == null && (object)b == null)
                return true;

            if ((object)a == null || (object)b == null)
                return false;
            
            return a.Driver == b.Driver && a.Time == b.Time;
        }

        public static bool operator !=(FastLap a, FastLap b)
        {
            return !(a == b);
        }

        public override bool Equals(object obj)
        {
            return obj is FastLap && this == (FastLap)obj;
        }

        public override int GetHashCode()
        {
            return Driver.GetHashCode();
        }
    }

    public partial class Telemetry : Dictionary<string, object>
    {
        public FastLap FastestLap { get; set; }
    }
}
