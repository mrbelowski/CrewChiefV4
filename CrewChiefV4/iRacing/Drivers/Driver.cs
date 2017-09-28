using System;
using System.Diagnostics;
using System.Linq;


namespace CrewChiefV4.iRacing
{
    [Serializable]
    public partial class Driver
    {
        private const string PACECAR_NAME = "safety pcfr500s";

        public Driver()
        {
            this.Car = new DriverCarInfo();
            this.PitInfo = new DriverPitInfo(this);
            this.Results = new DriverResults(this);
            this.QualyResults = new DriverQualyResults(this);
            this.Live = new DriverLiveInfo(this);
            this.Championship = new DriverChampInfo(this);
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
        public License License { get; set; }

        public bool IsSpectator { get; set; }
        public bool IsPacecar { get; set; }

        public string HelmetDesign { get; set; }
        public string CarDesign { get; set; }
        public string SuitDesign { get; set; }
        public string CarNumberDesign { get; set; }
        public string CarSponsor1 { get; set; }
        public string CarSponsor2 { get; set; }

        public string ClubName { get; set; }
        public string DivisionName { get; set; }

        public DriverCarInfo Car { get; set; }
        public DriverPitInfo PitInfo { get; set; }
        public DriverResults Results { get; private set; }
        public DriverSessionResults CurrentResults { get; set; }
        public DriverQualyResults QualyResults { get; set; }
        public DriverLiveInfo Live { get; private set; }
        public DriverChampInfo Championship { get; private set; }
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
            var licenseLevel = Parser.ParseInt(query["LicLevel"].GetValue());
            var licenseSublevel = Parser.ParseInt(query["LicSubLevel"].GetValue());
            var licenseColor = Parser.ParseColor(query["LicColor"].GetValue());
            this.License = new License(licenseLevel, licenseSublevel, licenseColor);

            this.IsSpectator = Parser.ParseInt(query["IsSpectator"].GetValue()) == 1;

            this.HelmetDesign = query["HelmetDesignStr"].GetValue();
            this.CarDesign = query["CarDesignStr"].GetValue();
            this.SuitDesign = query["SuitDesignStr"].GetValue();
            this.CarNumberDesign = query["CarNumberDesignStr"].GetValue();
            this.CarSponsor1 = query["CarSponsor_1"].GetValue();
            this.CarSponsor2 = query["CarSponsor_2"].GetValue();
            this.ClubName = query["ClubName"].GetValue();
            this.DivisionName = query["DivisionName"].GetValue();
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
            this.Car.CarClassRelSpeed = Parser.ParseInt(query["CarClassRelSpeed"].GetValue());
            this.Car.CarClassColor = Parser.ParseColor(query["CarClassColor"].GetValue());
            this.Car.CarClassShortName = query["CarClassShortName"].GetValue();
            this.Car.CarShortName = query["CarScreenNameShort"].GetValue();
            this.Car.CarPath = query["CarPath"].GetValue();

            this.IsPacecar = this.CustId == -1 || this.Car.CarName == PACECAR_NAME;
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
            Console.WriteLine(driver.Name);
            return driver;
        }

        internal void UpdateResultsInfo(int sessionNumber, YamlQuery query, int position)
        {
            this.Results.SetResults(sessionNumber, query, position);
            this.CurrentResults = this.Results.Current;
        }

        internal void UpdateQualyResultsInfo(YamlQuery query, int position)
        {
            this.QualyResults.ParseYaml(query, position);
        }

        internal void UpdateLiveInfo(TelemetryInfo e)
        {
            this.Live.ParseTelemetry(e);
        }

        internal void UpdatePrivateInfo(TelemetryInfo e)
        {
            this.Private.ParseTelemetry(e);
        }

        private double _prevPos;

        public void UpdateSectorTimes(Track track, TelemetryInfo telemetry)
        {
            if (track == null) return;
            if (track.Sectors.Count == 0) return;

            var results = this.CurrentResults;
            if (results != null)
            {
                var sectorcount = track.Sectors.Count;

                // Set arrays
                if (results.SectorTimes == null || results.SectorTimes.Length == 0)
                {
                    results.SectorTimes = track.Sectors.Select(s => s.Copy()).ToArray();
                }

                var p0 = _prevPos;
                var p1 = telemetry.CarIdxLapDistPct.Value[this.Id];
                var dp = p1 - p0;

                if (p1 < -0.5)
                {
                    // Not in world?
                    return;
                }

                var t = telemetry.SessionTime.Value;
                
                // Check lap crossing
                if (p0 - p1 > 0.5) // more than 50% jump in track distance == lap crossing occurred from 0.99xx -> 0.00x
                {
                    this.Live.CurrentSector = 0;
                    this.Live.CurrentFakeSector = 0;
                    p0 -= 1;
                }
                    
                // Check all real sectors
                foreach (var s in results.SectorTimes)
                {
                    if (p1 > s.StartPercentage && p0 <= s.StartPercentage)
                    {
                        // Crossed into new sector
                        var crossTime = (float)(t - (p1 - s.StartPercentage) * dp);

                        // Finish previous
                        var prevNum = s.Number <= 0 ? sectorcount - 1 : s.Number - 1;
                        var sector = results.SectorTimes[prevNum];
                        if (sector != null && sector.EnterSessionTime > 0)
                        {
                            sector.SectorTime = new Laptime((float)(crossTime - sector.EnterSessionTime));
                        }

                        // Begin next sector
                        s.EnterSessionTime = crossTime;

                        this.Live.CurrentSector = s.Number;

                        break;
                    }
                }

                // Check 'fake' sectors (divide track into thirds)
                sectorcount = 3;
                foreach (var s in results.FakeSectorTimes)
                {
                    if (p1 > s.StartPercentage && p0 <= s.StartPercentage)
                    {
                        // Crossed into new sector
                        var crossTime = (float)(t - (p1 - s.StartPercentage) * dp);

                        // Finish previous
                        
                        var prevNum = s.Number <= 0 ? sectorcount - 1 : s.Number - 1;
                        var sector = results.FakeSectorTimes[prevNum];
                        if (s.Number == 0 && sector != null && sector.EnterSessionTime > 0)
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
