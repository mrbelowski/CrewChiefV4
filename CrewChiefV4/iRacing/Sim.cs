using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace CrewChiefV4.iRacing
{
    public class Sim
    {

        private iRacingData _telemetry;
        private SessionInfo _sessionInfo;

        private bool _mustUpdateSessionData, _mustReloadDrivers;
        private int _DriverId;
        public int DriverId { get { return _DriverId; } }
        public Sim()
        {
            _drivers = new List<Driver>();
            _sessionData = new SessionData();
            _mustUpdateSessionData = true;
        }

        #region Properties

        private int? _currentSessionNumber;
        public int? CurrentSessionNumber { get { return _currentSessionNumber; } }

        public iRacingData Telemetry { get { return _telemetry; } }
        public SessionInfo SessionInfo { get { return _sessionInfo; } }

        private SessionData _sessionData;
        public SessionData SessionData { get { return _sessionData; } }

        private Driver _driver;
        public Driver Driver { get { return _driver; } }

        #endregion

        #region Methods
        
        public void Reset()
        {
            _mustUpdateSessionData = true;
            _mustReloadDrivers = true;
            _currentSessionNumber = null;
            _driver = null;
            _drivers.Clear();
            _telemetry = null;
            _sessionInfo = null;
            _isUpdatingDrivers = false;
        }

        #region Drivers

        private readonly List<Driver> _drivers;
        public List<Driver> Drivers { get { return _drivers; } }

        private bool _isUpdatingDrivers;

        private void UpdateDriverList(SessionInfo info)
        {
            //Console.WriteLine("UpdateDriverList");
            _isUpdatingDrivers = true;
            this.GetDrivers(info);
            _isUpdatingDrivers = false;
            this.GetResults(info);
        }

        private void GetDrivers(SessionInfo info)
        {
            //Console.WriteLine("GetDrivers");
            if (_mustReloadDrivers)
            {
                Console.WriteLine("MustReloadDrivers: true");
                _drivers.Clear();
                _mustReloadDrivers = false;
            }

            // Assume max 70 drivers
            for (int id = 0; id < 70; id++)
            {
                // Find existing driver in list
                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver == null)
                {
                    driver = Driver.FromSessionInfo(info, id);

                    // If no driver found, end of list reached
                    if (driver == null)
                    {
                        continue;
                    }

                    driver.IsCurrentDriver = false;

                    // Add to list
                    _drivers.Add(driver);
                }
                else
                {
                    // Update and check if driver swap occurred
                    var oldId = driver.CustId;
                    var oldName = driver.Name;
                    driver.ParseDynamicSessionInfo(info);

                    /*
                    if (oldId != driver.CustId)
                    {
                        var e = new DriverSwapRaceEvent();
                        e.Driver = driver;
                        e.PreviousDriverId = oldId;
                        e.PreviousDriverName = oldName;
                        e.CurrentDriverId = driver.Id;
                        e.CurrentDriverName = driver.Name;
                        e.SessionTime = _telemetry.SessionTime;
                        e.Lap = driver.Live.Lap;

                        this.OnRaceEvent(e);
                    }
                     * */
                }
                
                if (DriverId == driver.Id)
                {
                    _driver = driver;
                    _driver.IsCurrentDriver = true;
                }                
            }
        }
        
        private void GetResults(SessionInfo info)
        {
            // If currently updating list, or no session yet, then no need to update result info 
            if (_isUpdatingDrivers) 
                return;
            if (_currentSessionNumber == null) 
                return;

            this.GetRaceResults(info);
            this.GetQualyResults(info);
        }

        private void GetQualyResults(SessionInfo info)
        {
            // TODO: stop if qualy is finished
            var query = info["QualifyResultsInfo"]["Results"];
            if(_driver.CurrentResults.QualifyingPosition != -1)
            {
                return;
            }

            for (int position = 0; position < _drivers.Count; position++)
            {
                var positionQuery = query["Position", position];

                string idValue;
                if (!positionQuery["CarIdx"].TryGetValue(out idValue))
                {
                    // Driver not found
                    continue;
                }

                // Find driver and update results
                int id = int.Parse(idValue);

                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver != null)
                {
                    driver.CurrentResults.QualifyingPosition = position + 1; 
                }
            }
        }
        private void GetRaceResults(SessionInfo info)
        {
            var query =
                info["SessionInfo"]["Sessions"]["SessionNum", _currentSessionNumber]["ResultsPositions"];
            //Console.WriteLine(info.Yaml);
            for (int position = 1; position <= _drivers.Count; position++)
            {
                var positionQuery = query["Position", position];

                //string idValue;
                string idValue = positionQuery["CarIdx"].GetValue("0");
                string reasonOut;
                if(!positionQuery["ReasonOutId"].TryGetValue(out reasonOut))
                    continue;
                
                if (int.Parse(reasonOut) != 0)
                    continue;
                // Driver not found

                // Find driver and update results
                int id = int.Parse(idValue);

                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver != null)
                {
                    driver.UpdateResultsInfo(_currentSessionNumber.Value, positionQuery, position);
                }
            }
        }

        private void ResetSession()
        {
            // Need to re-load all drivers when session info updates
            _mustReloadDrivers = true;
        }

        private void UpdateDriverTelemetry(iRacingData info)
        {
            // If currently updating list, no need to update telemetry info 
            if (_isUpdatingDrivers) return;

            foreach (var driver in _drivers)
            {
                driver.Live.CalculateSpeed(info, _sessionData.Track.Length);
                driver.UpdateSectorTimes(_sessionData.Track, info);
                driver.UpdateLiveInfo(info);
                
                //_sessionData.UpdateFastestLap(driver);
            }
            this.CalculateLivePositions(info);
        }

        private void CalculateLivePositions(iRacingData info)
        {
            // In a race that is not yet in checkered flag mode,
            // Live positions are determined from track position (total lap distance)
            // Any other conditions (race finished, P, Q, etc), positions are ordered as result positions

            if (this.SessionData.EventType == "Race" && !info.SessionFlags.HasFlag(SessionFlags.Checkered))
            {
                // Determine live position from lapdistance
                int pos = 1;
                foreach (var driver in _drivers.OrderByDescending(d => d.Live.TotalLapDistance))                
                {
                    if(driver.IsPacecar)
                    {
                        continue;
                    }
                    driver.Live.Position = pos;
                    pos++;
                }
            }
            else
            {
                // In P or Q, set live position from result position (== best lap according to iRacing)
                // In P or Q, set live position from result position (== best lap according to iRacing)
                foreach (var driver in _drivers.OrderBy(d => d.CurrentResults.Position))
                {
                    driver.Live.Position = driver.CurrentResults.Position;
                }

                //foreach (var driver in _drivers)
                //{
                //    driver.Live.Position = info.CarIdxPosition[driver.Id];
                //}
            }

            // Determine live class position from live positions and class
            // Group drivers in dictionary with key = classid and value = list of all drivers in that class
            var dict = (from driver in _drivers group driver by driver.Car.CarClassId).ToDictionary(d => d.Key, d => d.ToList());

            // Set class position
            foreach (var drivers in dict.Values)
            {
                var pos = 1;
                foreach (var driver in drivers.OrderBy(d => d.Live.Position))
                {
                    driver.Live.ClassPosition = pos;
                    pos++;
                }
            }

        }
        
        #endregion

        #region Events

        public void SdkOnSessionInfoUpdated(SessionInfo sessionInfo, int sessionNumber,int driverId)
        {
           
            _DriverId = driverId;

            // Stop if we don't have a session number yet


            if (_currentSessionNumber == null || (_currentSessionNumber.Value != sessionNumber))
            {
                _mustUpdateSessionData = true;

                // Session changed, reset session info
                this.ResetSession();
            }
            _currentSessionNumber = sessionNumber;
            if (_mustUpdateSessionData)
            {
                _sessionData.Update(sessionInfo, sessionNumber);
                _mustUpdateSessionData = false;
            }
            // Update drivers
            this.UpdateDriverList(sessionInfo);
            
        }

        public void SdkOnTelemetryUpdated(iRacingData telemetry)
        {
            // Cache info            
            _telemetry = telemetry;
            // Update drivers telemetry
            this.UpdateDriverTelemetry(telemetry);
        }
      
        #endregion

        #endregion

    }
}
