using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.iRacing
{
    public class DriverQualyResults
    {
        public DriverQualyResults(Driver driver)
        {
            _driver = driver;
        }

        private readonly Driver _driver;
        /// <summary>
        /// Gets the driver object.
        /// </summary>
        public Driver Driver { get { return _driver; } }

        public int Position { get; set; }
        public int ClassPosition { get; set; }
        public Laptime Lap { get; set; }

        internal void ParseYaml(YamlQuery query, int position)
        {
            this.Position = position + 1;
            this.ClassPosition = Parser.ParseInt(query["ClassPosition"].GetValue()) + 1;
            this.Lap = new Laptime(Parser.ParseFloat(query["FastestTime"].GetValue()));
            this.Lap.LapNumber = Parser.ParseInt(query["FastestLap"].GetValue());
        }
    }
}
