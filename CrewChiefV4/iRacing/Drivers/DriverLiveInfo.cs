using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverLiveInfo
    {
        private const float SPEED_CALC_INTERVAL = 0.1f;
        private static TimeSpan MaxWaitForNewLapData = TimeSpan.FromSeconds(3);
        public static DateTime CurrentTime = DateTime.Now;

        private DateTime NewLapDataTimerExpiry = DateTime.MaxValue;
        private DateTime Now = new DateTime(DateTime.Now.Ticks);


        public DriverLiveInfo(Driver driver)
        {
            _driver = driver;
            LastLaptimeFromGame = new Laptime(0);
            PreviousLapWasValid = false;
            HasCrossedSFLine = false;
            FirstTime = false;
            IsNewLap = false;
        }

        private readonly Driver _driver;

        public Driver Driver
        {
            get { return _driver; }
        }
        
        public float SessionTime { get; set; }
        public int Position { get; set; }
        public int TrackPosition { get; set; }
        public int ClassPosition { get; set; }
        public int Lap { get; private set; }
        public float LapDistance { get; private set; }
        public float CorrectedLapDistance { get; private set; }
        public float TotalLapDistance
        {
            get { return Lap + LapDistance; }
        }

        public TrackSurfaces TrackSurface { get; private set; }
        
        public int Gear { get; private set; }
        public float Rpm { get; private set; }

        public double Speed { get; private set; }
        public double SpeedKph { get; private set; }
        
        public int CurrentSector { get; set; }
        public int CurrentFakeSector  { get; set; }
        public float LastLaptime { get; set; }
        public Laptime LastLaptimeFromGame { get; set; }
        public int LapsCompleted { get; set; }
        public bool IsNewLap { get; set; }
        public bool PreviousLapWasValid { get; set; }
        public float LapTimePrevious { get; set; }
        public bool HasCrossedSFLine { get; set; }
        public bool FirstTime { get; set; }
        // this may be true a short time *after* IsNewLap is true
        public Boolean LastLapTimeUpdated = false;

        private Boolean WaitingForNewLapData = false;

        private double _prevSpeedUpdateTime;
        private double _prevSpeedUpdateDist;
        private int _prevLap;
        private int _prevSector;
        public float CurrentLapTime 
        { 
            get
            {
                return SessionTime - (float)this.Driver.CurrentResults.FakeSector1.EnterSessionTime;
            }
        }
        public void checkForNewLapData( float gameProvidedLastLapTime)
        {

                if (this.HasCrossedSFLine)
                {
                    // reset the timer and start waiting for an updated laptime...
                    this.WaitingForNewLapData = true;
                    this.NewLapDataTimerExpiry = this.Now.Add(MaxWaitForNewLapData);
                }
                // if we're waiting, see if the timer has expired or we have a change in the previous laptime value
                if (this.WaitingForNewLapData && (this.LapTimePrevious != gameProvidedLastLapTime || this.Now > this.NewLapDataTimerExpiry))
                {
                    // the timer has expired or we have new data
                    this.WaitingForNewLapData = false;
                    this.LastLapTimeUpdated = true;
                    this.LapTimePrevious = gameProvidedLastLapTime;
                    this.PreviousLapWasValid = gameProvidedLastLapTime > 1;
                    this.LastLaptimeFromGame = new Laptime(gameProvidedLastLapTime);
                    this.IsNewLap = true;
                }
                else
                {
                    LastLapTimeUpdated = false;
                    this.IsNewLap = false;
                }
        }
        public void ParseTelemetry(iRacingData e)
        {   
                     
            this.LapDistance = e.CarIdxLapDistPct[this.Driver.Id];
            this.Lap = e.CarIdxLap[this.Driver.Id];

            if (this._prevSector == 3 && (this.CurrentFakeSector == 1))
            {
                HasCrossedSFLine = true;
            }
            else
            {
                HasCrossedSFLine = false;
            }
            if (this._prevSector != this.CurrentFakeSector)
            {
                this._prevSector = this.CurrentFakeSector;
            }


            this.CorrectedLapDistance = FixPercentagesOnLapChange(e.CarIdxLapDistPct[this.Driver.Id]);
            this.LapsCompleted = e.CarIdxLapCompleted[this.Driver.Id];
            if (this.LapsCompleted < 0)
                this.LapsCompleted = 0;
            this.TrackSurface = e.CarIdxTrackSurface[this.Driver.Id];            
            this.Gear = e.CarIdxGear[this.Driver.Id];
            this.Rpm = e.CarIdxRPM[this.Driver.Id];
            this.SessionTime = (float)e.SessionTime;


            this.Driver.PitInfo.CalculatePitInfo(e.SessionTime);
            //for local player we use data from telemetry as its updated faster the session info,
            //we do not have lastlaptime from opponents available in telemetry so we use data from sessioninfo.
            if(Driver.Id == e.PlayerCarIdx)
            {
                if (!FirstTime)
                {
                    this.LapTimePrevious = e.LapLastLapTime;
                    this.LastLaptimeFromGame = new Laptime(LapTimePrevious);
                    FirstTime = true;
                }
                checkForNewLapData(e.LapLastLapTime);                
            }
            else if (!this._driver.CurrentResults.IsEmpty)
            {
                if (!FirstTime)
                {
                    this.LapTimePrevious = (float)this._driver.CurrentResults.LastTime.Time.TotalSeconds;
                    this.LastLaptimeFromGame = new Laptime(LapTimePrevious);
                    FirstTime = true;
                }
                checkForNewLapData((float)this._driver.CurrentResults.LastTime.Time.TotalSeconds);
            }
                 
            /*else if (this._driver.CurrentResults.Laps.Count > this.LapsCompleted)
            {
                if (this._driver.CurrentResults.Laps.Count < 0)
                {
                    this.LapsCompleted = 0;
                }
                else
                {
                    this.LapsCompleted = this._driver.CurrentResults.Laps.Count;
                    //this.DelayedLap = this._driver.CurrentResults.Lap;
                }
                //Console.WriteLine(this.Driver.Name + " Has just started lap nr " + this._driver.CurrentResults.LapsComplete + " Last Time " + this._driver.CurrentResults.LastTime.DisplayShort);
                this.LastLaptimeFromGame = this._driver.CurrentResults.LastTime;                
                this.PreviousLapWasValid = LastLaptimeFromGame.Value > 1;
                this.IsNewLap = true;
            } 
            else
            {
                this.IsNewLap = false;
            }*/
                
        }


        private float FixPercentagesOnLapChange(float carIdxLapDistPct)
        {
            if (this.Lap > _prevLap && carIdxLapDistPct > 0.80f)
                return 0;
            else
                _prevLap = this.Lap;

            return carIdxLapDistPct;
        }
        public void CalculateSpeed(iRacingData current, double? trackLengthKm)
        {
            if (current == null) return;
            if (trackLengthKm == null) return;

            try
            {
                var t1 = current.SessionTime;
                var t0 = _prevSpeedUpdateTime;
                var time = t1 - t0;

                if (time < SPEED_CALC_INTERVAL)
                {
                    // Ignore
                    return;
                }

                var p1 = current.CarIdxLapDistPct[this.Driver.Id];
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

                var distance = distancePct*trackLengthKm.GetValueOrDefault()*1000; //meters


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
