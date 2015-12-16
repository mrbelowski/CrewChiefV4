using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    [Flags]
    public enum eCarFlags
    {
        [Description("None")]
        NONE = 0,
        [Description("Headlight")]
        CAR_HEADLIGHT = 1,
        [Description("Engine Active")]
        CAR_ENGINE_ACTIVE = 2,
        [Description("Engine Warning")]
        CAR_ENGINE_WARNING = 4,
        [Description("Speed Limiter")]
        CAR_SPEED_LIMITER = 8,
        [Description("ABS")]
        CAR_ABS = 16,
        [Description("Handbrake")]
        CAR_HANDBRAKE = 32
    }

}
