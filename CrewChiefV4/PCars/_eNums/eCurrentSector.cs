using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;
using System.Xml;
using System.Xml.Linq;

namespace CrewChiefV4.PCars
{
    public enum eCurrentSector
    {
        [Description("Invalid Sector")]
        SECTOR_INVALID = 0,
        [Description("Sector Start")]
        SECTOR_START,
        [Description("Sector 1")]
        SECTOR_SECTOR1,
        [Description("Sector 2")]
        SECTOR_SECTOR2,
        [Description("Sector 3")]
        SECTOR_FINISH,
        [Description("Sector Stop??")]
        SECTOR_STOP,
        //-------------
        SECTOR_MAX
    }
}