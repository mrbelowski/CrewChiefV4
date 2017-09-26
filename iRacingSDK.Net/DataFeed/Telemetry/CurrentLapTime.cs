using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iRacingSDK
{
    public partial class Telemetry : Dictionary<string, object>
    {
        double[] _carIdxLapTime;
        public double[] CarIdxLapTime
        {
            get
            {
                return _carIdxLapTime;
            }
            set
            {
                _carIdxLapTime = value;
            }
        }
        double[] _carIdxLastLapTime;
        public double[] CarIdxLastLapTime
        {
            get
            {
                return _carIdxLastLapTime;
            }
            set
            {
                _carIdxLastLapTime = value;
            }
        }




    }
}
