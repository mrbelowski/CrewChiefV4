using System;
using System.Diagnostics;
using System.Linq;


namespace CrewChiefV4.iRacing
{
    [Serializable]
    public partial class Driver
    {
        public Driver()
        {
            this.Car = new DriverCarInfo();
            this.Live = new DriverLiveInfo(this);
            this.CurrentResults = new DriverSessionResults();
            this.licensLevel = new Tuple<string, float>("invalid", -1);
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
        public string LicensLevelString { get; set; }
        public Tuple<String, float> licensLevel { get; set; }

        public bool IsSpectator { get; set; }
        public bool IsPacecar { get; set; }

        public string ClubName { get; set; }
        public string DivisionName { get; set; }

        public DriverCarInfo Car { get; set; }
        public DriverSessionResults CurrentResults { get; set; }
        public DriverLiveInfo Live { get; private set; }

        
        private double _prevPos;
        private double _prevTime;
        private double _prevSpeed;
        private double _prevDistance;

        public void ParseDynamicSessionInfo(SessionInfo info)
        {
            // Parse only session info that could have changed (driver dependent)
            var query = info["DriverInfo"]["Drivers"]["CarIdx", this.Id];

            this.Name = query["UserName"].GetValue("");
            this.CustId = Parser.ParseInt(query["UserID"].GetValue("0"));
            this.ShortName = query["AbbrevName"].GetValue();

            this.IRating = Parser.ParseInt(query["IRating"].GetValue());
            this.LicensLevelString = query["LicString"].GetValue();

            this.licensLevel = Parser.ParseLicens(LicensLevelString);

            this.IsSpectator = Parser.ParseInt(query["IsSpectator"].GetValue()) == 1;

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
            this.Car.CarClassShortName = query["CarClassShortName"].GetValue();
            this.Car.CarShortName = query["CarScreenNameShort"].GetValue();
            this.Car.CarClassRelSpeed = Parser.ParseInt(query["CarClassRelSpeed"].GetValue());            
            bool isPaceCar = Parser.ParseInt(query["CarIsPaceCar"].GetValue()) == 1;
            this.IsPacecar = this.CustId == -1 || isPaceCar;
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
            this.CurrentResults.ParseYaml( query, position);
        }

        internal void UpdateLiveInfo(iRacingData e)
        {
            this.Live.ParseTelemetry(e);
        }

        
        /*
        public double InterpolateTimeExact(TelemetryUpdate prev, TelemetryUpdate cur, double targetPos)
        {
            var t0 = _prevTime;
            var t1 = cur.Time;
            var p0 = _prevDistance;
            var p1 = cur.LapDistance;
            var v0 = _prevSpeed;
            var v1 = cur.Speed;
            var p = targetPos;

            var dv = v0 - v1;
            var dt = t0 - t1;

            var term1 = t0 * v0 - 2 * t0 * v1 + t1 * v0;
            var term2 = Math.Sqrt(dt * (4 * dv * (p + p0) + v0 * v0 * dt));
            var term3 = 2 * (v0 - v1);

            // Two possible answers
            var timePos = (term1 + term2) / term3;
            var timeNeg = (term1 - term2) / term3;

            // Take the one that is in between t0 and t1
            if (timePos > t1 || timePos < t0)
                return timeNeg;
            return timePos;
        }
        */
        public void UpdateSector(Track track, iRacingData telemetry)
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
                    this.Live.CurrentSector = 1;
                    p0 -= 1;
                }
                    
                // Check 'fake' sectors (divide track into thirds)
                foreach (var s in results.Sectors)
                {
                    if (p1 > s.StartPercentage && p0 <= s.StartPercentage)
                    {                    
                        this.Live.CurrentSector = s.Number + 1;
                        break;
                    }
                }                
                _prevPos = p1;
            }
        }
    }
}
