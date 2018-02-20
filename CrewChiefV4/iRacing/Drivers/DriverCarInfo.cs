using System;
using System.Windows.Media;

namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverCarInfo
    {
        public string CarNumber { get; set; }
        public int CarId { get; set; }
        public int CarClassId { get; set; }
        public int CarClassRelSpeed { get; set; }
        public float DriverCarFuelMaxLtr { get; set; }
        public float DriverCarMaxFuelPct { get; set; }
        public float DriverPitTrkPct { get; set; }
    }
}
