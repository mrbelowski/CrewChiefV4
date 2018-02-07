﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRSDKSharp;

namespace CrewChiefV4.iRacing
{
    public class Sim
    {
        public Sim()
        {
            _drivers = new List<Driver>();
            _sessionData = new SessionData();
            _infoUpdate = -1;
            _sessionId = -1;
            _driver = null;
            _paceCar = null;
        }

        private iRacingData _telemetry;

        private int _infoUpdate, _sessionId;

        private int? _currentSessionNumber;
        public int? CurrentSessionNumber { get { return _currentSessionNumber; } }

        public iRacingData Telemetry { get { return _telemetry; } }

        private SessionData _sessionData;
        public SessionData SessionData { get { return _sessionData; } }

        private int _DriverId;
        public int DriverId { get { return _DriverId; } }

        private Driver _driver;
        public Driver Driver { get { return _driver; } }

        private Driver _paceCar;
        public Driver PaceCar { get { return _paceCar; } }


        private readonly List<Driver> _drivers;
        public List<Driver> Drivers { get { return _drivers; } }

        private void UpdateDriverList(string sessionInfo, bool reloadDrivers)
        {
            this.GetDrivers(sessionInfo, reloadDrivers);
            this.GetResults(sessionInfo);
        }

        private void GetDrivers(string sessionInfo, bool reloadDrivers)
        {
            if (reloadDrivers)
            {
                Console.WriteLine("Reloading Drivers");
                _drivers.Clear();
                _driver = null;
            }

            // Assume max 70 drivers
            for (int id = 0; id < 70; id++)
            {
                // Find existing driver in list
                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver == null)
                {
                    driver = Driver.FromSessionInfo(sessionInfo, id);

                    if (driver == null || driver.IsPacecar)
                    {
                        continue;
                    }

                    driver.IsCurrentDriver = false;
                    _drivers.Add(driver);
                }
                else
                {
                    driver.ParseDynamicSessionInfo(sessionInfo);
                }
                
                if (DriverId == driver.Id)
                {
                    _driver = driver;
                    _driver.IsCurrentDriver = true;
                }                
            }
        }

        private void GetResults(string sessionInfo)
        {
            if (_currentSessionNumber == null) 
                return;
            this.GetRaceResults(sessionInfo);

            if (this.SessionData.EventType == "Race" || this.SessionData.EventType == "Open Qualify" || this.SessionData.EventType == "Lone Qualify")
            {
                this.GetQualyResults(sessionInfo);
            }
            
        }

        private void GetQualyResults(string sessionInfo)
        {
            // TODO: stop if qualy is finished
            for (int position = 0; position < _drivers.Count; position++)
            {

                string idValue = "0";
                if (!YamlParser.TryGetValue(sessionInfo, string.Format("QualifyResultsInfo:Results:Position:{{{0}}}CarIdx:", position ), out idValue))
                {
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

        private void GetRaceResults(string sessionInfo)
        {

            for (int position = 1; position <= _drivers.Count; position++)
            {
                
                string reasonOut;
                if (!YamlParser.TryGetValue(sessionInfo, string.Format("SessionInfo:Sessions:SessionNum:{{{0}}}ResultsPositions:Position:{{{1}}}ReasonOutId:",
                    _currentSessionNumber, position), out reasonOut))
                {
                    continue;
                }                    
                if (int.Parse(reasonOut) != 0)
                {
                    continue;
                }
                string idValue = "0";
                if (!YamlParser.TryGetValue(sessionInfo, string.Format("SessionInfo:Sessions:SessionNum:{{{0}}}ResultsPositions:Position:{{{1}}}CarIdx:",
                    _currentSessionNumber, position), out idValue))
                {
                    continue;
                }
                // Driver not found

                // Find driver and update results
                int id = int.Parse(idValue);

                var driver = _drivers.SingleOrDefault(d => d.Id == id);
                if (driver != null)
                {
                    driver.UpdateResultsInfo(sessionInfo, _currentSessionNumber.Value, position);
                }
            }
        }
        private void UpdateDriverTelemetry(iRacingData info)
        {
            foreach (var driver in _drivers)
            {
                driver.Live.CalculateSpeed(info, _sessionData.Track.Length);
                driver.UpdateSector(_sessionData.Track, info);
                driver.UpdateLiveInfo(info);                
            }
            this.CalculateLivePositions(info);
        }

        private void CalculateLivePositions(iRacingData telemetry)
        {
            // In a race that is not yet in checkered flag mode,
            // Live positions are determined from track position (total lap distance)
            // Any other conditions (race finished, P, Q, etc), positions are ordered as result positions
            SessionFlags flag = (SessionFlags)telemetry.SessionFlags;
            if (this.SessionData.EventType == "Race" && !flag.HasFlag(SessionFlags.Checkered))
            {
                // Determine live position from lapdistance
                int pos = 1;
                foreach (var driver in _drivers.OrderByDescending(d => d.Live.TotalLapDistance))                
                {
                    if(driver.IsSpectator || driver.IsPacecar)
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
                foreach (var driver in _drivers)
                {
                    if (driver.IsSpectator || driver.IsPacecar)
                    {
                        continue;
                    }
                    if (telemetry.CarIdxPosition[driver.Id] > 0)
                    {
                        driver.Live.Position = telemetry.CarIdxPosition[driver.Id];
                    }
                    else
                    {
                        driver.Live.Position = driver.CurrentResults.Position;
                    }

                    if (telemetry.CarIdxClassPosition[driver.Id] > 0)
                    {
                        driver.Live.ClassPosition = telemetry.CarIdxClassPosition[driver.Id];
                    }
                    else
                    {
                        driver.Live.ClassPosition = driver.CurrentResults.ClassPosition;
                    }
                }
            }
        }

        public bool SdkOnSessionInfoUpdated(string sessionInfo, int sessionNumber, int driverId)
        {           
            _DriverId = driverId;
            bool reloadDrivers = false;
            
            if (_currentSessionNumber == null || (_currentSessionNumber != sessionNumber))
            {
                // Session changed, reset session info
                reloadDrivers = true;
                _sessionData.Update(sessionInfo, sessionNumber);
            }
            _currentSessionNumber = sessionNumber;
            // Update drivers
            this.UpdateDriverList(sessionInfo, reloadDrivers);
            
            return reloadDrivers;
         
        }

        public void SdkOnTelemetryUpdated(iRacingData telemetry)
        {
            // Cache info            
            _telemetry = telemetry;
            // Update drivers telemetry
            this.UpdateDriverTelemetry(telemetry);
        }
    }
}
