using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverLiveInfo
    {
        private const float SPEED_CALC_INTERVAL = 0.1f;  
        public DriverLiveInfo(Driver driver)
        {
            _driver = driver;
            PreviousLapWasValid = false;
            HasCrossedSFLine = false;
            LapTimePrevious = -1;
            _prevSector = -1;
        }

        private readonly Driver _driver;

        public Driver Driver
        {
            get { return _driver; }
        }
        
        public float SessionTime { get; set; }
        public int Position { get; set; }
        public int ClassPosition { get; set; }
        public int Lap { get; private set; }
        public float LapDistance { get; private set; }
        public float CorrectedLapDistance { get; private set; }
        public float TotalLapDistance
        {
            get { return this.LapsCompleted + CorrectedLapDistance; }
        }

        public TrackSurfaces TrackSurface { get; private set; }
        
        public int Gear { get; private set; }
        public float Rpm { get; private set; }

        public double Speed { get; private set; }
        public double SpeedKph { get; private set; }
        
        public int CurrentSector  { get; set; }
        public int LapsCompleted { get; set; }
        public bool IsNewLap { get; set; }
        public bool PreviousLapWasValid { get; set; }
        public float LapTimePrevious { get; set; }
        public bool HasCrossedSFLine { get; set; }

        private double _prevSpeedUpdateTime;
        private double _prevSpeedUpdateDist;
        private int _prevLap;
        private int _prevSector;
        public void ParseTelemetry(iRacingData e)
        {   
                     
            this.LapDistance = e.CarIdxLapDistPct[this.Driver.Id];
            this.Lap = e.CarIdxLap[this.Driver.Id];

            if (this._prevSector == 3 && (this.CurrentSector == 1))
            {
                HasCrossedSFLine = true;
            }
            else
            {
                HasCrossedSFLine = false;
            }
            if (this._prevSector != this.CurrentSector)
            {
                this._prevSector = this.CurrentSector;
            }

            this.CorrectedLapDistance = FixPercentagesOnLapChange(e.CarIdxLapDistPct[this.Driver.Id]);
            this.LapsCompleted = e.CarIdxLapCompleted[this.Driver.Id];
            if (this.LapsCompleted < 0)
            {
                this.LapsCompleted = 0;
            }
            this.TrackSurface = e.CarIdxTrackSurface[this.Driver.Id];            
            this.Gear = e.CarIdxGear[this.Driver.Id];
            this.Rpm = e.CarIdxRPM[this.Driver.Id];
            this.SessionTime = (float)e.SessionTime;

            //for local player we use data from telemetry as its updated faster then session info,
            //we do not have lastlaptime from opponents available in telemetry so we use data from sessioninfo.
            if(Driver.Id == e.PlayerCarIdx)
            {
                this.LapTimePrevious = this._driver.CurrentResults.LastTime;                            
            }
            else
            {
                this.LapTimePrevious = this._driver.CurrentResults.LastTime;                
            }                
        }

        private float FixPercentagesOnLapChange(float carIdxLapDistPct)
        {
            if (this.Lap > _prevLap && carIdxLapDistPct > 0.90f)
                return 0;
            else
                _prevLap = this.Lap;

            return carIdxLapDistPct;
        }

        public void CalculateSpeed(iRacingData telemetry, double? trackLengthKm)
        {
            if (telemetry == null) return;
            if (trackLengthKm == null) return;

            try
            {
                var t1 = telemetry.SessionTime;
                var t0 = _prevSpeedUpdateTime;
                var time = t1 - t0;

                if (time < SPEED_CALC_INTERVAL)
                {
                    // Ignore
                    return;
                }

                var p1 = telemetry.CarIdxLapDistPct[this.Driver.Id];
                var p0 = _prevSpeedUpdateDist;

                if (p1 < -0.5 || _driver.Live.TrackSurface == TrackSurfaces.NotInWorld)
                {
                    // Not in world?
                    return;
                }

                if (p0 - p1 > 0.5)
                {
                    // Lap crossing
                    p1 += 1;
                }
                var distancePct = p1 - p0;

                var distance = distancePct * trackLengthKm.GetValueOrDefault() * 1000; //meters


                if (time >= Double.Epsilon)
                {
                    this.Speed = distance/(time); // m/s
                }
                else
                {
                    if (distance < 0)
                        this.Speed = Double.NegativeInfinity;
                    else
                        this.Speed = Double.PositiveInfinity;
                }
                this.SpeedKph = this.Speed * 3.6;

                _prevSpeedUpdateTime = t1;
                _prevSpeedUpdateDist = p1;
            }
            catch (Exception ex)
            {
                //Log.Instance.LogError("Calculating speed of car " + this.Driver.Id, ex);
                this.Speed = 0;
            }
        }
    }
}
