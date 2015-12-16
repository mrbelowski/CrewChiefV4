using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    public enum ePitSchedule
    {
        [Description("None")]
        PIT_SCHEDULE_NONE = 0,        // Nothing scheduled
        [Description("Standard")]
        PIT_SCHEDULE_STANDARD,        // Used for standard pit sequence
        [Description("Drive Through")]
        PIT_SCHEDULE_DRIVE_THROUGH,   // Used for drive-through penalty
        [Description("Stop Go")]
        PIT_SCHEDULE_STOP_GO,         // Used for stop-go penalty
        //-------------
        PIT_SCHEDULE_MAX
    }
}
