using System;
using System.Diagnostics;
using System.Linq;
using iRSDKSharp;

namespace CrewChiefV4.iRacing
{
    [Serializable]
    public partial class Driver
    {
        const string driverYamlPath = "DriverInfo:Drivers:CarIdx:{{{0}}}{1}:";

        public Driver()
        {
            this.Car = new DriverCarInfo();
            this.Live = new DriverLiveInfo(this);
            this.CurrentResults = new DriverSessionResults();
            this.licensLevel = new Tuple<string, float>("invalid", -1);
            this.FinishStatus = FinishState.Unknown;
        }

        /// <summary>
        /// If true, this is your driver on track.
        /// </summary>
        public bool IsCurrentDriver { get; set; }
        public int Id { get; set; }
        public int CustId { get; set; }
        public string Name { get; set; }
        public string CarNumber { get { return this.Car.CarNumber; } }

        public int TeamId { get; set; }
        public string TeamName { get; set; }

        public int IRating { get; set; }
        public string LicensLevelString { get; set; }
        public Tuple<String, float> licensLevel { get; set; }

        public bool IsSpectator { get; set; }
        public bool IsPaceCar { get; set; }

        public DriverCarInfo Car { get; set; }
        public DriverSessionResults CurrentResults { get; set; }
        public DriverLiveInfo Live { get; private set; }

        public enum FinishState
        {
            Unknown,
            Retired,
            Finished
        }
        // Heuristically derived during Race position ordering, not coming from game data.
        public FinishState FinishStatus { get; internal set; }

        private string ParseDriverYaml(string sessionInfo, string Node)
        {
            return YamlParser.Parse(sessionInfo, string.Format(driverYamlPath, this.Id, Node));
        }
        
        public void ParseDynamicSessionInfo(string sessionInfo, bool isPlayerCar)
        {
            // Parse only session info that could have changed (driver dependent)
            this.Name = ParseDriverYaml(sessionInfo, "UserName");
            this.CustId = Parser.ParseInt(ParseDriverYaml(sessionInfo, "UserID"));
            this.IRating = Parser.ParseInt(ParseDriverYaml(sessionInfo, "IRating"));
            this.LicensLevelString = ParseDriverYaml(sessionInfo, "LicString");
            this.licensLevel = Parser.ParseLicens(LicensLevelString);
            this.IsSpectator = Parser.ParseInt(ParseDriverYaml(sessionInfo, "IsSpectator")) == 1;
            if(isPlayerCar && (this.Car.DriverPitTrkPct <= 0 || this.Car.DriverCarMaxFuelPct <= 0 || this.Car.DriverCarFuelMaxLtr <= 0))
            {
                this.Car.DriverCarFuelMaxLtr = Parser.ParseFloat(YamlParser.Parse(sessionInfo, "DriverInfo:DriverCarFuelMaxLtr:"), -1.0f);
                this.Car.DriverCarMaxFuelPct = Parser.ParseFloat(YamlParser.Parse(sessionInfo, "DriverInfo:DriverCarMaxFuelPct:"), -1.0f);
                this.Car.DriverPitTrkPct = Parser.ParseFloat(YamlParser.Parse(sessionInfo, "DriverInfo:DriverPitTrkPct:"), -1.0f);
            }
        }

        public void ParseStaticSessionInfo(string sessionInfo, bool isPlayerCar)
        {
            this.TeamId = Parser.ParseInt(ParseDriverYaml(sessionInfo, "TeamID"));
            this.TeamName = ParseDriverYaml(sessionInfo, "TeamName");
            this.Car.CarId = Parser.ParseInt(ParseDriverYaml(sessionInfo, "CarID"));
            this.Car.CarNumber = ParseDriverYaml(sessionInfo, "CarNumberRaw");
            this.Car.CarClassId = Parser.ParseInt(ParseDriverYaml(sessionInfo, "CarClassID"));
            this.Car.CarClassRelSpeed = Parser.ParseInt(ParseDriverYaml(sessionInfo, "CarClassRelSpeed"));
            if (isPlayerCar)
            {
                this.Car.DriverCarFuelMaxLtr = Parser.ParseFloat(YamlParser.Parse(sessionInfo, "DriverInfo:DriverCarFuelMaxLtr:"), -1.0f);
                this.Car.DriverCarMaxFuelPct = Parser.ParseFloat(YamlParser.Parse(sessionInfo, "DriverInfo:DriverCarMaxFuelPct:"), -1.0f);
                this.Car.DriverPitTrkPct = Parser.ParseFloat(YamlParser.Parse(sessionInfo, "DriverInfo:DriverPitTrkPct:"), -1.0f);
            }
            bool isPaceCar = Parser.ParseInt(ParseDriverYaml(sessionInfo, "CarIsPaceCar")) == 1;
            this.IsPaceCar = this.CustId == -1 || isPaceCar;
        }
        public static Driver FromSessionInfo(string sessionInfo, int carIdx, int playerCarIdx)
        {            
            string name;
            if (!YamlParser.TryGetValue(sessionInfo, string.Format(driverYamlPath, carIdx, "UserName"), out name))
            {
                // Driver not found
                return null;
            }
            var driver = new Driver();
            driver.Id = carIdx;
            driver.ParseStaticSessionInfo(sessionInfo, playerCarIdx == carIdx);
            driver.ParseDynamicSessionInfo(sessionInfo, playerCarIdx == carIdx);
            return driver;
        }

        internal void UpdateResultsInfo(string sessionInfo, int sessionNumber, int position)
        {
            this.CurrentResults.ParseYaml(sessionInfo, sessionNumber, position);
        }

        internal void UpdateLiveInfo(iRacingData e, Boolean isRaceOrQualifying)
        {
            this.Live.ParseTelemetry(e, isRaceOrQualifying);
        }
    }
}
