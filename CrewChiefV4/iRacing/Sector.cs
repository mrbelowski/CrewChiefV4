using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.iRacing
{
    public class Sector
    {
        public Sector()
        {
            SectorTime = new Laptime(int.MaxValue);
        }
        public int Number { get; set; }
        public float StartPercentage { get; set; }

        public double EnterSessionTime { get; set; }
        public Laptime SectorTime { get; set; }

        public Sector Copy()
        {
            var s = new Sector();
            s.Number = this.Number;
            s.StartPercentage = this.StartPercentage;
            return s;
        }
    }
}
