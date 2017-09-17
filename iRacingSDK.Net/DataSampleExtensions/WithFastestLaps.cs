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

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {
        public static IEnumerable<DataSample> WithFastestLaps(this IEnumerable<DataSample> samples)
        {
            FastLap lastFastLap = null;
            var lastDriverLaps = new int[64];
            var driverLapStartTime = new double[64];
            var fastestLapTime = double.MaxValue;

			foreach (var data in samples.ForwardOnly())
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

                    if (lap.Lap == lastDriverLaps[lap.CarIdx] + 1)
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

                yield return data;
            }
        }
    }
}
