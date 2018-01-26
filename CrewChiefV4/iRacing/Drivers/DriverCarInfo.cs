using System;
using System.Windows.Media;

namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverCarInfo
    {
        public string CarNumber { get; set; }
        public int CarNumberRaw { get; set; }
        public int CarId { get; set; }
        public string CarName { get; set; }
        public int CarClassId { get; set; }
        public string CarClassShortName { get; set; }
        public string CarShortName { get; set; }
        public int CarClassRelSpeed { get; set; }
    }
}
