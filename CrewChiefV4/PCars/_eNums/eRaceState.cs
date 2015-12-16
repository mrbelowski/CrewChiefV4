using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    public enum eRaceState
    {
        [Description("Invalid")]
        RACESTATE_INVALID = 0,
        [Description("Not started")]
        RACESTATE_NOT_STARTED,
        [Description("Racing")]
        RACESTATE_RACING,
        [Description("Finished")]
        RACESTATE_FINISHED,
        [Description("Disqualified")]
        RACESTATE_DISQUALIFIED,
        [Description("Retired")]
        RACESTATE_RETIRED,
        [Description("DNF")]
        RACESTATE_DNF,
        //-------------
        RACESTATE_MAX
    }
}
