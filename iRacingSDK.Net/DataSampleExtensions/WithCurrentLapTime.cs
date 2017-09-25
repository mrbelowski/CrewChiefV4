using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {

        public static IEnumerable<DataSample> WithCurrentLapTime(this IEnumerable<DataSample> samples)
        {
            int[] lastDriverLaps = new int[64];
            double[] driverLapStartTime = new double[64];
            double[] lapTime = new double[64];
            foreach (var data in samples.ForwardOnly())
            {
                if (data.IsConnected && data.Telemetry!=null)
                {
                    var carsAndLaps = data.Telemetry
                        .CarIdxLap
                        .Select((l, i) => new { CarIdx = i, Lap = l })
                        .Skip(1)
                        .Take(data.SessionData.DriverInfo.CompetingDrivers.Length - 1);

                    foreach (var lap in carsAndLaps)
                    {
                        if (lap.Lap == -1)
                            continue;
                        lapTime[lap.CarIdx] = data.Telemetry.SessionTime - driverLapStartTime[lap.CarIdx];

                        if (lap.Lap > data.Telemetry.CarIdxLapCompleted[lap.CarIdx]
                            && lap.Lap > lastDriverLaps[lap.CarIdx]
                            && data.Telemetry.CarIdxTrackSurface[lap.CarIdx] == TrackLocation.OnTrack)
                        {
                            //Console.WriteLine("NewLap started by " + data.Telemetry.CarDetails[lap.CarIdx].UserName + " laptime:" + TimeSpan.FromSeconds(lapTime[lap.CarIdx]).ToString());
                            driverLapStartTime[lap.CarIdx] = data.Telemetry.SessionTime;
                            lastDriverLaps[lap.CarIdx] = lap.Lap;
                        }
                    }
                }
                data.Telemetry.CarIdxLapTime = lapTime;

                yield return data;
            }
        }
    }
}
