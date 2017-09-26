using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRacingSDK;
using iRacingSDK.Support;
namespace CrewChiefV4.iRacing
{
    class PostProcessData
    {
        static int[] lastDriverLaps = new int[64];
        static double[] driverLapStartTime = new double[64];
        static double[] lapTime = new double[64];
        static double[] lastLapTime = new double[64];
        public static DataSample ProcessLapTimes(DataSample data)
        {


            var carsAndLaps = data.Telemetry
                .CarIdxLap
                .Select((l, i) => new { CarIdx = i, Lap = l })
                .Skip(1)
                .Take(data.SessionData.DriverInfo.CompetingDrivers.Length);

            foreach (var lap in carsAndLaps)
            {
                
                if (lap.Lap == -1)
                    continue;

                lapTime[lap.CarIdx] = data.Telemetry.SessionTime - driverLapStartTime[lap.CarIdx];

                if (lap.Lap > data.Telemetry.CarIdxLapCompleted[lap.CarIdx]
                    && lap.Lap > lastDriverLaps[lap.CarIdx]
                    && data.Telemetry.CarIdxTrackSurface[lap.CarIdx] == TrackLocation.OnTrack)
                {

                    driverLapStartTime[lap.CarIdx] = data.Telemetry.SessionTime;
                    lastDriverLaps[lap.CarIdx] = lap.Lap;
                    lastLapTime[lap.CarIdx] = lapTime[lap.CarIdx];
                    Console.WriteLine("NewLap started by " + data.Telemetry.CarDetails[lap.CarIdx].UserName + " laptime:" + TimeSpan.FromSeconds(lapTime[lap.CarIdx]).ToString());
 
                }
            }
            data.Telemetry.CarIdxLastLapTime = lastLapTime;
            data.Telemetry.CarIdxLapTime = lapTime;
            
            return data;
        }
        public static void ProcessCalculateSpeed(DataSample data)
        {
            foreach(Car car in data.Telemetry.RaceCars)
            {
                car.CalculateSpeed(data.Telemetry, iRacingHelpers.ParseTrackLength(data.SessionData.WeekendInfo.TrackLength));
                car.CalculatePitInfo(data.Telemetry.SessionTime);                
            }
        }

        public static DataSample ProcessCorrectedDistances(DataSample data)
        {
            var maxDistance = new float[64];
            var lastAdjustment = new int[64];

            for (int i = 0; i < data.SessionData.DriverInfo.CompetingDrivers.Length; i++)
                CorrectDistance(data.SessionData.DriverInfo.CompetingDrivers[i].UserName,
                    ref data.Telemetry.CarIdxLap[i],
                    ref data.Telemetry.CarIdxLapDistPct[i],
                    ref maxDistance[i],
                    ref lastAdjustment[i]);
            
            return data;            
        }

        static void CorrectDistance(string driverName, ref int lap, ref float distance, ref float maxDistance, ref int lastAdjustment)
        {
            var totalDistance = lap + distance;
            var roundedDistance = (int)(totalDistance * 1000.0);
            var roundedMaxDistance = (int)(maxDistance * 1000.0);

            if (roundedDistance > roundedMaxDistance && roundedDistance > 0)
                maxDistance = totalDistance;

            if (roundedDistance < roundedMaxDistance)
            {
                lastAdjustment = roundedDistance;
                lap = (int)maxDistance;
                distance = maxDistance - (int)maxDistance;
            }
        }
        public static DataSample ProcessCorrectedPercentages(DataSample data)
        {
            int[] lastLaps = null;
            if (lastLaps == null)
                lastLaps = (int[])data.Telemetry.CarIdxLap.Clone();

            for (int i = 0; i < data.SessionData.DriverInfo.CompetingDrivers.Length; i++)
            {
                if (data.Telemetry.HasData(i))
                {
                    FixPercentagesOnLapChange(
                        ref lastLaps[i],
                        ref data.Telemetry.CarIdxLapDistPct[i],
                        data.Telemetry.CarIdxLap[i]);
                }   
            }
         
            return data;
        }

        static void FixPercentagesOnLapChange(ref int lastLap, ref float carIdxLapDistPct, int carIdxLap)
        {
            if (carIdxLap > lastLap && carIdxLapDistPct > 0.80f)
                carIdxLapDistPct = 0;
            else
                lastLap = carIdxLap;
        }
        public static DataSample ProcessFinishingStatus(DataSample data)
        {
            var hasSeenCheckeredFlag = new bool[64];
            var lastTimeForData = new TimeSpan[64];

            ApplyIsFinalLap(data);

            ApplyLeaderHasFinished(data);

            ApplyHasSeenCheckeredFlag(data, hasSeenCheckeredFlag);

            ApplyHasRetired(data, lastTimeForData);
            
            return data;

        }

        static void ApplyIsFinalLap(DataSample data)
        {
            data.Telemetry.IsFinalLap = data.Telemetry.RaceLaps >= data.SessionData.SessionInfo.Sessions[data.Telemetry.SessionNum].ResultsLapsComplete;
        }

        static void ApplyLeaderHasFinished(DataSample data)
        {
            if (data.Telemetry.RaceLaps > data.SessionData.SessionInfo.Sessions[data.Telemetry.SessionNum].ResultsLapsComplete)
                data.Telemetry.LeaderHasFinished = true;
        }

        static void ApplyHasSeenCheckeredFlag(DataSample data, bool[] hasSeenCheckeredFlag)
        {
            if (data.LastSample != null && data.Telemetry.LeaderHasFinished)
                for (int i = 1; i < data.SessionData.DriverInfo.CompetingDrivers.Length; i++)
                    if (data.LastSample.Telemetry.CarIdxLapDistPct[i] > 0.90 && data.Telemetry.CarIdxLapDistPct[i] < 0.10)
                        hasSeenCheckeredFlag[i] = true;

            data.Telemetry.HasSeenCheckeredFlag = hasSeenCheckeredFlag;
        }

        static void ApplyHasRetired(DataSample data, TimeSpan[] lastTimeOfData)
        {
            data.Telemetry.HasRetired = new bool[64];

            if (!(new[] { SessionState.Racing, SessionState.Checkered, SessionState.CoolDown }).Contains(data.Telemetry.SessionState))
                return;

            for (int i = 1; i < data.SessionData.DriverInfo.CompetingDrivers.Length; i++)
            {
                if (data.Telemetry.HasSeenCheckeredFlag[i])
                    continue;

                if (data.Telemetry.HasData(i))
                {
                    lastTimeOfData[i] = data.Telemetry.SessionTimeSpan;
                    continue;
                }

                if (lastTimeOfData[i] + TimeSpan.FromSeconds(30) < data.Telemetry.SessionTimeSpan)
                    data.Telemetry.HasRetired[i] = true;
            }
        }
        public static DataSample ProcessFastestLaps(DataSample data)
        {
            FastLap lastFastLap = new FastLap();
            var lastDriverLaps = new int[64];
            var driverLapStartTime = new double[64];
            var fastestLapTime = double.MaxValue;


            var carsAndLaps = data.Telemetry
                .CarIdxLap
                .Select((l, i) => new { CarIdx = i, Lap = l })
                .Skip(1)
                .Take(data.SessionData.DriverInfo.CompetingDrivers.Length);

            foreach (var lap in carsAndLaps)
            {
                if (lap.Lap == -1)
                    continue;

                if (lap.Lap > data.Telemetry.CarIdxLapCompleted[lap.CarIdx]
                    && lap.Lap > lastDriverLaps[lap.CarIdx])
                {
                    var lapTime = data.Telemetry.SessionTime - driverLapStartTime[lap.CarIdx];

                    driverLapStartTime[lap.CarIdx] = data.Telemetry.SessionTime;
                    lastDriverLaps[lap.CarIdx] = lap.Lap;

                    if (lap.Lap > 1 && lapTime < fastestLapTime)
                    {
                        fastestLapTime = lapTime;

                        lastFastLap = new FastLap
                        {
                            Time = TimeSpan.FromSeconds(lapTime),
                            Driver = data.SessionData.DriverInfo.CompetingDrivers[lap.CarIdx]
                        };
                    }
                }
            }
            data.Telemetry.FastestLap = lastFastLap;
            return data;
        }
    }
}
