using System;
using System.Collections.Generic;
using System.Linq;


namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverSessionResults
    {
        public DriverSessionResults()
        {
            this.IsEmpty = true;
            this.FastestLap = -1;
            this.Time = -1;
            this.LastTime = -1;
            this.FastestTime = -1;
            this.QualifyingPosition = -1;
            this.Sectors = new[]
                    {
                        new Sector() {Number = 0, StartPercentage = 0f},
                        new Sector() {Number = 1, StartPercentage = 0.333f},
                        new Sector() {Number = 2, StartPercentage = 0.666f}
                    };
        }
        public bool IsEmpty { get; set; }

        public int Position { get; set; }
        public int ClassPosition { get; set; }
        public int QualifyingPosition { get; set; }

        public int Lap { get; set; }
        public float Time { get; set; }

        public int FastestLap { get; set; }
        public float FastestTime { get; set; }
        public float  LastTime { get; set; }
        public int LapsLed { get; set; }
        public int LapsComplete { get; set; }
        public int LapsDriven { get; set; }
        

        public Sector[] Sectors { get; set; }

        public int Incidents { get; set; }
        public string OutReason { get; set; }
        public int OutReasonId { get; set; }
        public bool IsOut { get { return this.OutReasonId != 0; } }

        internal void ParseYaml(YamlQuery query, int position)
        {
            this.IsEmpty = false;

            this.Position = position;
            this.ClassPosition = Parser.ParseInt(query["ClassPosition"].GetValue()) + 1;

            this.Lap = Parser.ParseInt(query["Lap"].GetValue());
            this.Time = Parser.ParseFloat(query["Time"].GetValue());
            this.FastestLap = Parser.ParseInt(query["FastestLap"].GetValue());
            this.FastestTime = Parser.ParseFloat(query["FastestTime"].GetValue());
            this.LastTime = Parser.ParseFloat(query["LastTime"].GetValue());
            this.LapsLed = Parser.ParseInt(query["LapsLed"].GetValue());

            this.LapsComplete = Parser.ParseInt(query["LapsComplete"].GetValue());
            this.LapsDriven = Parser.ParseInt(query["LapsDriven"].GetValue());

            this.Incidents = Parser.ParseInt(query["Incidents"].GetValue());;
            this.OutReasonId = Parser.ParseInt(query["ReasonOutId"].GetValue());
            this.OutReason = query["ReasonOutStr"].GetValue();
        }
    }
}
