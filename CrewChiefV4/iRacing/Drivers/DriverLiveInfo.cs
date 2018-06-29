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
            LiveLapsCompleted = -1;
            LapsCompleted = -1;
            insertDummyLap = false;
            dummyInserted = false;
            this.Sectors = new[]
                    {
                        new Sector() {Number = 0, StartPercentage = 0f},
                        new Sector() {Number = 1, StartPercentage = 0.333f},
                        new Sector() {Number = 2, StartPercentage = 0.666f}
                    };
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
            get { return this.LiveLapsCompleted + CorrectedLapDistance; }
        }

        public float TotalLapDistanceCorrected { get; set; }

        public TrackSurfaces TrackSurface { get; private set; }
        
        public int Gear { get; private set; }
        public float Rpm { get; private set; }

        public double Speed { get; private set; }
        public double SpeedKph { get; private set; }
        public float GameTimeWhenLastCrossedSFLine { get; private set; }
        public int CurrentSector  { get; set; }
        public int LiveLapsCompleted { get; set; }
        public int LapsCompleted { get; set; }
        public bool IsNewLap { get; set; }
        public bool PreviousLapWasValid { get; set; }
        public float LapTimePrevious { get; set; }
        public bool HasCrossedSFLine { get; set; }
        public Sector[] Sectors { get; set; }
        public int PlayerCarPosition { get; set; }

        private double _prevSpeedUpdateTime;
        private double _prevSpeedUpdateDist;
        private int _prevLap = 0;
        private int _prevSector;
        private Boolean insertDummyLap;
        private Boolean dummyInserted;
        public void ParseTelemetry(iRacingData e)
        {   
            this.LapDistance = Math.Abs(e.CarIdxLapDistPct[this.Driver.Id]);
            insertDummyLap = this.Lap < e.CarIdxLap[this.Driver.Id] && e.CarIdxLap[this.Driver.Id] > 0 && dummyInserted == false;
            this.Lap = e.CarIdxLap[this.Driver.Id];
            
            this.TrackSurface = e.CarIdxTrackSurface[this.Driver.Id];     
            if (this._prevSector == 3 && (this.CurrentSector == 1) || 
                (LapsCompleted < _driver.CurrentResults.LapsComplete && !_driver.IsCurrentDriver && TrackSurface == TrackSurfaces.NotInWorld))
            {                
                HasCrossedSFLine = true;
                // This is not accurate for playes that are not in live telemetry but its not used in any calculations in this case.
                GameTimeWhenLastCrossedSFLine = (float)e.SessionTime;
            }
            else
            {
                HasCrossedSFLine = false;
            }
            if (this._prevSector != this.CurrentSector)
            {
                this._prevSector = this.CurrentSector;
            }
            if (_driver.CurrentResults.LapsComplete > this.LapsCompleted || insertDummyLap)
            {
                if(insertDummyLap)
                {
                    dummyInserted = true;
                }
                this.IsNewLap = true;
            }
            else
            {
                this.IsNewLap = false;
            }
            this.LapsCompleted = _driver.CurrentResults.LapsComplete;

            this.CorrectedLapDistance = FixPercentagesOnLapChange(this.LapDistance);
            this.LiveLapsCompleted = e.CarIdxLapCompleted[this.Driver.Id] < this.LapsCompleted ? this.LapsCompleted : e.CarIdxLapCompleted[this.Driver.Id];
                  
            this.Gear = e.CarIdxGear[this.Driver.Id];
            this.Rpm = e.CarIdxRPM[this.Driver.Id];

            //for local player we use data from telemetry as its updated faster then session info,
            //we do not have lastlaptime from opponents available in telemetry so we use data from sessioninfo.
            if(Driver.Id == e.PlayerCarIdx)
            {
                this.LapTimePrevious = e.LapLastLapTime;                           
            }
            else
            {
                this.LapTimePrevious = this._driver.CurrentResults.LastTime;                
            }
            this.PreviousLapWasValid = this.LapTimePrevious > 1;
            this.PlayerCarPosition = e.PlayerCarPosition;
            this.TotalLapDistanceCorrected = this.TotalLapDistance;
        }

        private float FixPercentagesOnLapChange(float carIdxLapDistPct)
        {
            if (this.Lap > _prevLap && carIdxLapDistPct > 0.90f)
                return 0;
            else
                _prevLap = this.Lap;

            return Math.Abs(carIdxLapDistPct);
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
            catch (Exception)
            {
                //Log.Instance.LogError("Calculating speed of car " + this.Driver.Id, ex);
                this.Speed = 0;
            }
        }
    }
}
