using System;
using System.Diagnostics;
using System.Linq;


namespace CrewChiefV4.iRacing
{
    [Serializable]
    public partial class Driver
    {
        private const string PACECAR_NAME = "safety pcfr500s";
        private const string PACECAR_NAME2 = "pace car";

        public Driver()
        {
            this.Car = new DriverCarInfo();
            this.PitInfo = new DriverPitInfo(this);
            this.Results = new DriverResults(this);
            this.Live = new DriverLiveInfo(this);
            this.Private = new DriverPrivateInfo(this);
            this.CurrentResults = new DriverSessionResults(this,0);
        }

        /// <summary>
        /// If true, this is your driver on track.
        /// </summary>
        public bool IsCurrentDriver { get; set; }

        public int Id { get; set; }
        public int CustId { get; set; }
        public string Name { get; set; }
        public string ShortName { get; set; }
        public string CarNumber { get { return this.Car.CarNumber; } }

        public int TeamId { get; set; }
        public string TeamName { get; set; }

        public int IRating { get; set; }

        public bool IsSpectator { get; set; }
        public bool IsPacecar { get; set; }

        public string ClubName { get; set; }
        public string DivisionName { get; set; }

        public DriverCarInfo Car { get; set; }
        public DriverPitInfo PitInfo { get; set; }
        public DriverResults Results { get; private set; }
        public DriverSessionResults CurrentResults { get; set; }
        public DriverLiveInfo Live { get; private set; }
        public DriverPrivateInfo Private { get; private set; }

        public string LongDisplay
        {
            get { return string.Format("#{0} {1}{2}",
                this.Car.CarNumber,
                this.Name,
                this.TeamId > 0 ? " (" + this.TeamName + ")" : ""); }
        }

        public void ParseDynamicSessionInfo(SessionInfo info)
        {
            // Parse only session info that could have changed (driver dependent)
            var query = info["DriverInfo"]["Drivers"]["CarIdx", this.Id];

            this.Name = query["UserName"].GetValue("");
            this.CustId = Parser.ParseInt(query["UserID"].GetValue("0"));
            this.ShortName = query["AbbrevName"].GetValue();

            this.IRating = Parser.ParseInt(query["IRating"].GetValue());
            this.IsSpectator = Parser.ParseInt(query["IsSpectator"].GetValue()) == 1;

            this.ClubName = query["ClubName"].GetValue();
            this.DivisionName = query["DivisionName"].GetValue();
            this.IsPacecar = this.Name.ToLower().Equals(PACECAR_NAME2);
        }

        public void ParseStaticSessionInfo(SessionInfo info)
        {
            // Parse only static session info that never changes (car dependent)
            var query = info["DriverInfo"]["Drivers"]["CarIdx", this.Id];
            
            this.TeamId = Parser.ParseInt(query["TeamID"].GetValue());
            this.TeamName = query["TeamName"].GetValue();

            this.Car.CarId = Parser.ParseInt(query["CarID"].GetValue());
            this.Car.CarNumber = query["CarNumber"].GetValue();
            this.Car.CarNumberRaw = Parser.ParseInt(query["CarNumberRaw"].GetValue());
            this.Car.CarName = query["CarScreenName"].GetValue();
            this.Car.CarClassId = Parser.ParseInt(query["CarClassID"].GetValue());
            this.Car.CarClassShortName = query["CarClassShortName"].GetValue();
            this.Car.CarShortName = query["CarScreenNameShort"].GetValue();

            this.IsPacecar = this.CustId == -1 || this.Car.CarName.ToLower().Equals(PACECAR_NAME);
        }

        public static Driver FromSessionInfo(SessionInfo info, int carIdx)
        {
            var query = info["DriverInfo"]["Drivers"]["CarIdx", carIdx];

            string name;
            if (!query["UserName"].TryGetValue(out name))
            {
                // Driver not found
                return null;
            }

            var driver = new Driver();
            driver.Id = carIdx;
            driver.ParseStaticSessionInfo(info);
            driver.ParseDynamicSessionInfo(info);
            return driver;
        }

        internal void UpdateResultsInfo(int sessionNumber, YamlQuery query, int position)
        {
            this.Results.SetResults(sessionNumber, query, position);
            this.CurrentResults = this.Results.Current;
        }

        internal void UpdateLiveInfo(iRacingData e)
        {
            this.Live.ParseTelemetry(e);
        }

        internal void UpdatePrivateInfo(iRacingData e)
        {
            this.Private.ParseTelemetry(e);
        }

        private double _prevPos;

        public void UpdateSectorTimes(Track track, iRacingData telemetry)
        {
            if (track == null) 
                return;

            var results = this.CurrentResults;
            if (results != null)
            {

                var p0 = _prevPos;
                var p1 = telemetry.CarIdxLapDistPct[this.Id];
                var dp = p1 - p0;

                if (p1 < -0.5)
                {
                    // Not in world?
                    return;
                }

                var t = telemetry.SessionTime;
                
                // Check lap crossing
                if (p0 - p1 > 0.5) // more than 50% jump in track distance == lap crossing occurred from 0.99xx -> 0.00x
                {
                    this.Live.CurrentSector = 0;
                    this.Live.CurrentFakeSector = 0;
                    p0 -= 1;
                }
                    
                // Check 'fake' sectors (divide track into thirds)
                int sectorcount = 3;
                foreach (var s in results.FakeSectorTimes)
                {
                    if (p1 > s.StartPercentage && p0 <= s.StartPercentage)
                    {
                        // Crossed into new sector
                        var crossTime = (float)(t - (p1 - s.StartPercentage) * dp);

                        // Finish previous
                        
                        var prevNum = s.Number <= 0 ? sectorcount - 1 : s.Number - 1;
                        var sector = results.FakeSectorTimes[prevNum];
                        if (s.Number == 0 && sector != null && sector.EnterSessionTime > 0 && results.FakeSector1.EnterSessionTime > 0)
                        {

                            this.Live.LastLaptime = (float)(crossTime - results.FakeSector1.EnterSessionTime);

                        }
                        if (sector != null && sector.EnterSessionTime > 0)
                        {
                            sector.SectorTime = new Laptime((float)(crossTime - sector.EnterSessionTime));
                        }

                        // Begin next sector
                        s.EnterSessionTime = crossTime;                        
                        this.Live.CurrentFakeSector = s.Number;

                        break;
                    }
                }
                
                _prevPos = p1;
            }
        }
    }
}
