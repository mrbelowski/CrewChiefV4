// This file is part of iRacingSDK.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingSDK.Net
//
// iRacingSDK is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingSDK is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingSDK.  If not, see <http://www.gnu.org/licenses/>.

using iRacingSDK.Support;
using System;
using System.Diagnostics;
using System.Linq;

namespace iRacingSDK
{
    public class CarDetails
    {
        readonly int carIdx;
        readonly Telemetry telemetry;
        readonly SessionData._DriverInfo._Drivers driver;

        public CarDetails(Telemetry telemetry, int carIdx)
        {
            this.telemetry = telemetry;
            this.carIdx = carIdx;
            this.driver = telemetry.SessionData.DriverInfo.CompetingDrivers[carIdx];
        }

        public int Index { get { return carIdx; } }
        public int CarIdx { get { return carIdx; } }
        public SessionData._DriverInfo._Drivers Driver { get { return driver; } }
        public string CarNumberDisplay { get { return driver == null ? "" : driver.CarNumber; } }
        public short CarNumberRaw { get { return driver == null ? (short)-1 : (short)driver.CarNumberRaw; } }
        public string UserName { get { return driver == null ? "Unknown" : driver.UserName; } }
        public bool IsPaceCar { get { return carIdx == 0; } }

        public Car Car(DataSample data)
        {
            return data.Telemetry.Cars[this.carIdx];
        }
    }

    public class Car
    {
        readonly int carIdx;
        readonly Telemetry telemetry;
        readonly SessionData._DriverInfo._Drivers driver;
        public readonly CarDetails Details;
        private const float SPEED_CALC_INTERVAL = 0.5f;
        private const float PIT_MINSPEED = 0.01f;

        public Car(Telemetry telemetry, int carIdx)
        {
            this.telemetry = telemetry;
            this.carIdx = carIdx;
            this.driver = telemetry.SessionData.DriverInfo.CompetingDrivers[carIdx];
            this.Details = new CarDetails(telemetry, carIdx);
        }

        public int Index { get { return carIdx; } }
        public int CarIdx { get { return carIdx; } }

        public int Lap { get { return telemetry.CarIdxLap[carIdx]; } }
        public float DistancePercentage { get { return telemetry.CarIdxLapDistPct[carIdx]; } }
        public float DistanceRoundTrack { get { return telemetry.CarIdxDistance[carIdx]; } }
        public float TotalDistance { get { return this.Lap + this.DistancePercentage; } }
        public LapSector LapSector { get { return telemetry.CarSectorIdx[carIdx]; } }
        public int Position { get { return telemetry.Positions[carIdx]; } }
        public int OfficialPostion { get { return telemetry.CarIdxPosition[carIdx]; } }
        public bool HasSeenCheckeredFlag { get { return telemetry.HasSeenCheckeredFlag[carIdx]; } }
        public bool HasData { get { return telemetry.HasData(carIdx); } }
        public bool HasRetired { get { return telemetry.HasRetired[carIdx]; } }
        public TrackLocation TrackSurface { get { return telemetry.CarIdxTrackSurface[carIdx]; } }
        public int PitStopCount { get { return telemetry.CarIdxPitStopCount[carIdx]; } }
        public int LapCompleated { get { return telemetry.CarIdxLapCompleted[carIdx]; } }
        public double LapTime { get { return telemetry.CarIdxLapTime[carIdx]; } }
        
        public int Gear { get; private set; }
        public float Rpm { get; private set; }
        public double SteeringAngle { get; private set; }

        public double Speed { get; private set; }
        public double SpeedKph { get; private set; }

        public string DeltaToLeader { get; set; }
        public string DeltaToNext { get; set; }

        public int CurrentSector { get; set; }
        public int CurrentFakeSector { get; set; }


        private bool _hasIncrementedCounter;

        public int Pitstops { get; set; }

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


        private double _prevSpeedUpdateTime;
        private double _prevSpeedUpdateDist;

        public void CalculateSpeed(Telemetry current, double? trackLengthKm)
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

                var p1 = current.CarIdxLapDistPct[CarIdx];
                var p0 = _prevSpeedUpdateDist;

                if (p1 < -0.5 || TrackSurface == TrackLocation.NotInWorld)
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
                    this.Speed = distance / (time); // m/s
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
        public void CalculatePitInfo(double time)
        {
            // If we are not in the world (blinking?), stop checking
            if (TrackSurface == TrackLocation.NotInWorld)
            {
                return;
            }

            // Are we NOW in pit lane (pitstall includes pitlane)
            this.InPitLane = TrackSurface == TrackLocation.AproachingPits ||
                        TrackSurface == TrackLocation.InPitStall;

            // Are we NOW in pit stall?
            this.InPitStall = TrackSurface == TrackLocation.InPitStall;


            this.CurrentStint = LapCompleated - this.LastPitLap;

            // Were we already in pitlane previously?
            if (this.PitLaneEntryTime == null)
            {
                // We were not previously in pitlane
                if (this.InPitLane)
                {
                    // We have only just now entered pitlane
                    this.PitLaneEntryTime = time;
                    this.CurrentPitLaneTimeSeconds = 0;

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
                        if (Math.Abs(Speed) > PIT_MINSPEED)
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

                        this.LastPitLap = LapCompleated;
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


                    // Reset
                    this.PitLaneEntryTime = null;
                }
            }
        }
        public SessionData._SessionInfo._Sessions._ResultsPositions ResultPosition
        {
            get
            {
                if (telemetry.Session.ResultsPositions == null)
                    return null;

                return telemetry.Session.ResultsPositions.FirstOrDefault(rp => rp.CarIdx == carIdx);
            }
        }

        public TimeSpan LastTimeSpan
        {
            get { return LastTime.Seconds(); }
        }

        public double LastTime
        {
            get
            {
                var rp = ResultPosition;
                if (rp == null)
                    return 0f;

                if( rp.LapsComplete != (Lap-1))
                {
                    TraceInfo.WriteLine("Attempt to get LastTime from session data, with mismatch Lap counters.  Telemerty Lap: {0}.  Session LapComplete: {1}", Lap-1, rp.LapsComplete);
                    return 0f;
                }

                return rp.LastTime;
            }
        }
    }
}
