using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    public enum eSessionState
    {
        [Description("No Session")]
        SESSION_INVALID = 0,
        [Description("Practise")]
        SESSION_PRACTICE,
        [Description("Testing")]
        SESSION_TEST,
        [Description("Qualifying")]
        SESSION_QUALIFY,
        [Description("Formation Lap")]
        SESSION_FORMATIONLAP,
        [Description("Racing")]
        SESSION_RACE,
        [Description("Time Trial")]
        SESSION_TIME_ATTACK,
        //-------------
        SESSION_MAX
    }
}
