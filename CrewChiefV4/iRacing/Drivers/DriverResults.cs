using System;
using System.Collections.Generic;
using iRSDKSharp;


namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverSessionResults
    {
        public DriverSessionResults()
        {
            this.IsEmpty = true;
            this.LastTime = -1;
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
        public float  LastTime { get; set; }        
        public Sector[] Sectors { get; set; }
        public int Incidents { get; set; }
        public string OutReason { get; set; }
        public ReasonOutId OutReasonId { get; set; }
        public bool IsOut { get { return this.OutReasonId != ReasonOutId.IDS_REASON_OUT_NOT_OUT; } }

        const string driverResultYamlPath = "SessionInfo:Sessions:SessionNum:{{{0}}}ResultsPositions:Position:{{{1}}}{2}:";
        private string ParseRaceResultsYaml(string sessionInfo, int sessionnumber, int position, string node)
        {
            return YamlParser.Parse(sessionInfo, string.Format(driverResultYamlPath, sessionnumber, position, node));
        }

        internal void ParseYaml(string sessionInfo, int sessionnumber, int position)
        {
            this.IsEmpty = false;
            this.Position = position;
            this.ClassPosition = Parser.ParseInt(ParseRaceResultsYaml(sessionInfo, sessionnumber, position, "ClassPosition")) + 1;
            this.LastTime = Parser.ParseFloat(ParseRaceResultsYaml(sessionInfo, sessionnumber, position, "LastTime"));
            this.Incidents = Parser.ParseInt(ParseRaceResultsYaml(sessionInfo, sessionnumber, position, "Incidents"));
            this.OutReasonId = (ReasonOutId)Parser.ParseInt(ParseRaceResultsYaml(sessionInfo, sessionnumber, position, "ReasonOutId"));
            this.OutReason = ParseRaceResultsYaml(sessionInfo, sessionnumber, position, "ReasonOutStr");
        }
    }
}
