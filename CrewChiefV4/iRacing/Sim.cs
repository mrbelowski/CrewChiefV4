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
        private static Sim _instance;
        /*public static Sim Instance
        {
            get { return _instance ?? (_instance = new Sim()); }
        }*/

        private TelemetryInfo _telemetry;
        private SessionInfo _sessionInfo;

        private bool _mustUpdateSessionData, _mustReloadDrivers;
        private TimeDelta _timeDelta;
        private int _DriverId;
        public int DriverId { get { return _DriverId; } }
        public Sim()
        {
            _drivers = new List<Driver>();

            _sessionData = new SessionData();
            _mustUpdateSessionData = true;

            // Attach events
        }

        #region Properties


        private int? _currentSessionNumber;
        public int? CurrentSessionNumber { get { return _currentSessionNumber; } }

        public TelemetryInfo Telemetry { get { return _telemetry; } }
        public SessionInfo SessionInfo { get { return _sessionInfo; } }

        private SessionData _sessionData;
        public SessionData SessionData { get { return _sessionData; } }

        private Driver _driver;
        public Driver Driver { get { return _driver; } }

        private Driver _leader;
        public Driver Leader{ get { return _leader; } }

        private bool _isReplay;
        public bool IsReplay { get { return _isReplay; } }

        #endregion

        #region Methods
        
        private void Reset()
        {
            _mustUpdateSessionData = true;
            _mustReloadDrivers = true;
            _currentSessionNumber = null;
            _driver = null;
            _leader = null;
            _drivers.Clear();
            _timeDelta = null;
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
            Debug.WriteLine("UpdateDriverList");
            _isUpdatingDrivers = true;
            this.GetDrivers(info);
            _isUpdatingDrivers = false;

            this.GetResults(info);
        }

        private void GetDrivers(SessionInfo info)
        {
            Debug.WriteLine("GetDrivers");
            if (_mustReloadDrivers)
            {
                Debug.WriteLine("MustReloadDrivers: true");
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
                    if (driver == null) break;

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

                    if (oldId != driver.CustId)
                    {
                        var e = new DriverSwapRaceEvent();
                        e.Driver = driver;
                        e.PreviousDriverId = oldId;
                        e.PreviousDriverName = oldName;
                        e.CurrentDriverId = driver.Id;
                        e.CurrentDriverName = driver.Name;
                        e.SessionTime = _telemetry.SessionTime.Value;
                        e.Lap = driver.Live.Lap;

                        this.OnRaceEvent(e);
                    }
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
            if (_isUpdatingDrivers) return;
            if (_currentSessionNumber == null) return;

            this.GetQualyResults(info);
            this.GetRaceResults(info);
        }

        private void GetQualyResults(SessionInfo info)
        {
            // TODO: stop if qualy is finished
            var query =
                info["QualifyResultsInfo"]["Results"];

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
                    driver.UpdateQualyResultsInfo(positionQuery, position);
                }
            }
        }

        private void GetRaceResults(SessionInfo info)
        {
            var query =
                info["SessionInfo"]["Sessions"]["SessionNum", _currentSessionNumber]["ResultsPositions"];

            for (int position = 1; position <= _drivers.Count; position++)
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
                    var previousPosition = driver.Results.Current.ClassPosition;

                    driver.UpdateResultsInfo(_currentSessionNumber.Value, positionQuery, position);

                    if (_telemetry != null)
                    {
                        // Check for new leader
                        if (previousPosition > 1 && driver.Results.Current.ClassPosition == 1)
                        {
                            var e = new NewLeaderRaceEvent();
                            e.Driver = driver;
                            e.SessionTime = _telemetry.SessionTime.Value;
                            e.Lap = driver.Live.Lap;

                            this.OnRaceEvent(e);
                        }

                        // Check for new best lap
                        var bestlap = _sessionData.UpdateFastestLap(driver.CurrentResults.FastestTime, driver);
                        if (bestlap != null)
                        {
                            var e = new BestLapRaceEvent();
                            e.Driver = driver;
                            e.BestLap = bestlap;
                            e.SessionTime = _telemetry.SessionTime.Value;
                            e.Lap = driver.Live.Lap;

                            this.OnRaceEvent(e);
                        }
                    }
                }
            }
        }

        private void ResetSession()
        {
            // Need to re-load all drivers when session info updates
            _mustReloadDrivers = true;
        }

        private void UpdateDriverTelemetry(TelemetryInfo info)
        {
            // If currently updating list, no need to update telemetry info 
            if (_isUpdatingDrivers) return;

            if (_driver != null) _driver.UpdatePrivateInfo(info);
            foreach (var driver in _drivers)
            {
                driver.Live.CalculateSpeed(info, _sessionData.Track.Length);
                driver.UpdateLiveInfo(info);
                driver.UpdateSectorTimes(_sessionData.Track, info);
            }
            
            this.CalculateLivePositions();
            this.UpdateTimeDelta();
        }

        private void CalculateLivePositions()
        {
            // In a race that is not yet in checkered flag mode,
            // Live positions are determined from track position (total lap distance)
            // Any other conditions (race finished, P, Q, etc), positions are ordered as result positions

            if (this.SessionData.EventType == "Race" && !this.SessionData.IsCheckered)
            {
                // Determine live position from lapdistance
                int pos = 1;
                foreach (var driver in _drivers.OrderByDescending(d => d.Live.TotalLapDistance))
                {
                    if (pos == 1) _leader = driver;
                    driver.Live.Position = pos;
                    pos++;
                }
            }
            else
            {
                // In P or Q, set live position from result position (== best lap according to iRacing)
                foreach (var driver in _drivers.OrderBy(d => d.Results.Current.Position))
                {
                    if (this.Leader == null) _leader = driver;
                    driver.Live.Position = driver.Results.Current.Position;
                }
            }

            // Determine live class position from live positions and class
            // Group drivers in dictionary with key = classid and value = list of all drivers in that class
            var dict = (from driver in _drivers
                        group driver by driver.Car.CarClassId)
                .ToDictionary(d => d.Key, d => d.ToList());

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

            if (this.Leader != null && this.Leader.CurrentResults != null)
                _sessionData.LeaderLap = this.Leader.CurrentResults.LapsComplete + 1;
        }
        
        private void UpdateTimeDelta()
        {
            if (_timeDelta == null) return;

            // Update the positions of all cars
            _timeDelta.Update(_telemetry.SessionTime.Value, _telemetry.CarIdxLapDistPct.Value);

            // Order drivers by live position
            var drivers = _drivers.OrderBy(d => d.Live.Position).ToList();
            if (drivers.Count > 0)
            {
                // Get leader
                //var leader = drivers[0];
                this.Leader.Live.DeltaToLeader = "-";
                this.Leader.Live.DeltaToNext = "-";

                // Loop through drivers
                for (int i = 1; i < drivers.Count; i++)
                {
                    var behind = drivers[i];
                    var ahead = drivers[i - 1];

                    // Lapped?
                    var leaderLapDiff = Math.Abs(this.Leader.Live.TotalLapDistance - behind.Live.TotalLapDistance);
                    var nextLapDiff = Math.Abs(ahead.Live.TotalLapDistance - behind.Live.TotalLapDistance);

                    if (leaderLapDiff < 1)
                    {
                        var leaderDelta = _timeDelta.GetDelta(behind.Id, this.Leader.Id);
                        behind.Live.DeltaToLeader = TimeDelta.DeltaToString(leaderDelta);
                    }
                    else
                    {
                        behind.Live.DeltaToLeader = Math.Floor(leaderLapDiff) + " L";
                    }

                    if (nextLapDiff < 1)
                    {
                        var nextDelta = _timeDelta.GetDelta(behind.Id, ahead.Id);
                        behind.Live.DeltaToNext = TimeDelta.DeltaToString(nextDelta);
                    }
                    else
                    {
                        behind.Live.DeltaToNext = Math.Floor(nextLapDiff) + " L";
                    }
                }
            }
        }

        private void CheckSessionFlagUpdates(SessionFlag prevFlags, SessionFlag curFlags)
        {
            if (prevFlags == null || curFlags == null) return;

            var go = SessionFlags.StartGo;
            var green = SessionFlags.Green;
            var yellow = SessionFlags.Caution;

            bool isGreen = !prevFlags.Contains(go) && curFlags.Contains(go) 
                || !prevFlags.Contains(green) && curFlags.Contains(green);

            if (isGreen)
            {
                var e=  new GreenFlagRaceEvent();
                e.SessionTime = _telemetry.SessionTime.Value;
                e.Lap = Leader == null ? 0 : Leader.Live.Lap;
                this.OnRaceEvent(e);
            }

            if (!prevFlags.Contains(yellow) && curFlags.Contains(yellow))
            {
                var e = new YellowFlagRaceEvent();
                e.SessionTime = _telemetry.SessionTime.Value;
                e.Lap = Leader == null ? 0 : Leader.Live.Lap;
                this.OnRaceEvent(e);
            }
        }

        public void NotifyPitstop(RaceEvent.EventTypes type, Driver driver)
        {
            /*
            DriverRaceEvent e;
            if (type.HasFlag(EventTypes.PitEntry) == RaceEvent.EventTypes.PitEntry)
                e = new PitEntryRaceEvent();
            else
                e = new PitExitRaceEvent();

            e.Driver = driver;
            e.SessionTime = _telemetry.SessionTime.Value;
            e.Lap = driver.Live.Lap;
            this.OnRaceEvent(e);
             * */
        }

        #endregion

        #region Events

        public void SdkOnSessionInfoUpdated(SessionInfo sessionInfo, int sessionNumber)
        {         
            // Cache info
            _sessionInfo = sessionInfo;
            _currentSessionNumber = sessionNumber;
            // Stop if we don't have a session number yet
            if (_currentSessionNumber == null) 
                return;

            if (_mustUpdateSessionData)
            {
                _sessionData.Update(sessionInfo, sessionNumber);
                _timeDelta = new TimeDelta((float)_sessionData.Track.Length * 1000f, 20, 64);
                _mustUpdateSessionData = false;

                this.OnStaticInfoChanged();
            }

            // Update drivers
            this.UpdateDriverList(sessionInfo);
        }

        public void SdkOnTelemetryUpdated(TelemetryInfo telemetry)
        {
            // Cache info
            
            _telemetry = telemetry;

            _isReplay = telemetry.IsReplayPlaying.Value;

            // Check if session changed
            if (_currentSessionNumber == null || (_currentSessionNumber.Value != telemetry.SessionNum.Value))
            {
                _mustUpdateSessionData = true;

                // Session changed, reset session info
                this.ResetSession();
            }

            // Store current session number
            _currentSessionNumber = telemetry.SessionNum.Value;

            // Get previous state
            var sessionWasFinished = this.SessionData.IsFinished;
            var prevFlags = this.SessionData.Flags;

            // Update session state
            _sessionData.UpdateState(telemetry.SessionState.Value);

            // Update drivers telemetry
            this.UpdateDriverTelemetry(telemetry);

            // Update session data
            this.SessionData.Update(telemetry);

            // Check if flags updated
            this.CheckSessionFlagUpdates(prevFlags, this.SessionData.Flags);

            if (!sessionWasFinished && this.SessionData.IsFinished)
            {
                // If session just finished, get winners
                // Use result position (not live position)
                var winners =
                    Drivers.Where(d => d.CurrentResults != null && d.CurrentResults.ClassPosition == 1).OrderBy(d => d.CurrentResults.Position);
                foreach (var winner in winners)
                {
                    var ev = new WinnerRaceEvent();
                    ev.Driver = winner;
                    ev.SessionTime = _telemetry.SessionTime.Value;
                    ev.Lap = winner.Live.Lap;
                    this.OnRaceEvent(ev);
                }
            }
            //Console.WriteLine("SdkOnTelemetryUpdated");
        }

        private void SdkOnDisconnected(object sender, EventArgs e)
        {
            this.Reset();
            this.OnDisconnected();
        }

        private void SdkOnConnected(object sender, EventArgs e)
        {
            this.OnConnected();
        }

        public event EventHandler Connected;
        public event EventHandler Disconnected;
        public event EventHandler StaticInfoChanged;
        public event EventHandler SimulationUpdated;
        public event EventHandler<RaceEventArgs> RaceEvent;

        protected virtual void OnConnected()
        {
            if (this.Connected != null) this.Connected(this, EventArgs.Empty);
        }

        protected virtual void OnDisconnected()
        {
            if (this.Disconnected != null) this.Disconnected(this, EventArgs.Empty);
        }

        protected virtual void OnStaticInfoChanged()
        {
            if (this.StaticInfoChanged != null) this.StaticInfoChanged(this, EventArgs.Empty);
        }

        protected virtual void OnSimulationUpdated()
        {
            if (this.SimulationUpdated != null) this.SimulationUpdated(this, EventArgs.Empty);
        }

        protected virtual void OnRaceEvent(RaceEvent @event)
        {
            if (this.RaceEvent != null) this.RaceEvent(this, new RaceEventArgs(@event));
        }

        public class RaceEventArgs : EventArgs
        {
            public RaceEventArgs(RaceEvent @event)
            {
                _event = @event;
            }

            private readonly RaceEvent _event;
            public RaceEvent Event { get { return _event; } }
        }
        
        #endregion

        #endregion

    }
}
