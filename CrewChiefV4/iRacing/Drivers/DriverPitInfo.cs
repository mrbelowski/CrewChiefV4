using System;
using System.Diagnostics;

namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class DriverPitInfo
    {
        private const float PIT_MINSPEED = 0.01f;

        public DriverPitInfo(Driver driver)
        {
            _driver = driver;
            this.IsAtPitEntry = false;
            this.IsAtPitExit = false;
        }

        private readonly Driver _driver;
        private bool _hasIncrementedCounter;

        public int Pitstops { get; set; }

        public bool IsAtPitEntry { get; set; }
        public bool IsAtPitExit { get; set; }

        public bool InPitLane { get; set; }
        public bool InPitStall { get; set; }

        public double? PitLaneEntryTime { get; set; }
        public double? PitLaneExitTime { get; set; }

        public double? PitStallEntryTime { get; set; }
        public double? PitStallExitTime { get; set; }

        public double LastPitLaneTimeSeconds { get; set; }
        public double LastPitStallTimeSeconds { get; set; }

        public double CurrentPitLaneTimeSeconds { get; set; }
        public double CurrentPitStallTimeSeconds { get; set; }

        public int LastPitLap { get; set; }
        public int CurrentStint { get; set; }

        public void CalculatePitInfo(double time)
        {
            // If we are not in the world (blinking?), stop checking
            if (_driver.Live.TrackSurface == TrackSurfaces.NotInWorld)
            {
                return;
            }

            // Are we NOW in pit lane (pitstall includes pitlane)
            this.InPitLane = _driver.Live.TrackSurface == TrackSurfaces.AproachingPits ||
                        _driver.Live.TrackSurface == TrackSurfaces.InPitStall;
            if(InPitLane)
            {
                IsAtPitEntry = false;
                IsAtPitExit = false;
            }

            // Are we NOW in pit stall?
            this.InPitStall = _driver.Live.TrackSurface == TrackSurfaces.InPitStall;


            this.CurrentStint = _driver.Results.Current.LapsComplete - this.LastPitLap;

            // Were we already in pitlane previously?
            if (this.PitLaneEntryTime == null)
            {
                // We were not previously in pitlane
                if (this.InPitLane)
                {
                    // We have only just now entered pitlane
                    this.PitLaneEntryTime = time;
                    this.CurrentPitLaneTimeSeconds = 0;
                    this.IsAtPitEntry = true;
                }
            }
            else
            {
                // We were already in pitlane but have not exited yet
                this.CurrentPitLaneTimeSeconds = time - this.PitLaneEntryTime.Value;
                
                // Were we already in pit stall?
                if (this.PitStallEntryTime == null)
                {
                    // We were not previously in our pit stall yet
                    if (this.InPitStall)
                    {
                        IsAtPitEntry = false;
                        IsAtPitExit = false;
                        if (Math.Abs(_driver.Live.Speed) > PIT_MINSPEED)
                        {
                            Debug.WriteLine("PIT: did not stop in pit stall, ignored.");
                        }
                        else
                        {
                            // We have only just now entered our pit stall

                            this.PitStallEntryTime = time;
                            this.CurrentPitStallTimeSeconds = 0;
                        }
                    }
                }
                else
                {
                    // We already were in our pit stall
                    this.CurrentPitStallTimeSeconds = time - this.PitStallEntryTime.Value;
                    
                    if (!this.InPitStall)
                    {
                        // We have now left our pit stall

                        this.LastPitStallTimeSeconds = time - this.PitStallEntryTime.Value;

                        this.CurrentPitStallTimeSeconds = 0;

                        if (this.PitStallExitTime != null)
                        {
                            var diff = this.PitStallExitTime.Value - time;
                            if (Math.Abs(diff) < 5)
                            {
                                // Sim detected pit stall exit again less than 5 seconds after previous exit.
                                // This is not possible?
                                return;
                            }
                        }

                        // Did we already count this stop?
                        if (!_hasIncrementedCounter)
                        {
                            // Now increment pitstop count
                            this.Pitstops += 1;
                            _hasIncrementedCounter = true;
                        }
                        
                        this.LastPitLap = _driver.Results.Current.LapsComplete;
                        this.CurrentStint = 0;

                        // Reset
                        this.PitStallEntryTime = null;
                        this.PitStallExitTime = time;
                    }
                }
                
                if (!this.InPitLane)
                {
                    // We have now left pitlane
                    this.PitLaneExitTime = time;
                    _hasIncrementedCounter = false;

                    this.LastPitLaneTimeSeconds = this.PitLaneExitTime.Value - this.PitLaneEntryTime.Value;
                    this.CurrentPitLaneTimeSeconds = 0;
                    IsAtPitExit = true;
                    //Sim.Instance.NotifyPitstop(RaceEvent.EventTypes.PitExit, _driver);

                    // Reset
                    this.PitLaneEntryTime = null;
                }
            }
        }
    }
}
