using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.iRacing
{
    [Flags]
    public enum PitServiceFlags : uint
    {
        LFTireChange = 0x0001,
        RFTireChange = 0x0002,
        LRTireChange = 0x0004,
        RRTireChange = 0x0008,
        FuelFill = 0x0010,
        WindshieldTearoff = 0x0020,
        FastRepair = 0x0040
    }
}
