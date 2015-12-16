using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    public enum eCrashDamageState
    {
        CRASH_DAMAGE_NONE = 0,
        CRASH_DAMAGE_OFFTRACK,
        CRASH_DAMAGE_LARGE_PROP,
        CRASH_DAMAGE_SPINNING,
        CRASH_DAMAGE_ROLLING,
        //-------------
        CRASH_MAX
    }
}
