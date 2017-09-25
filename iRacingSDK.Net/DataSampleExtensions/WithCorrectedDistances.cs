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
        /// <summary>
        /// filter the DataSamples, and correct for when a car blips off due to data loss - and reports laps/distances as -1
        /// Ensures the lap/distances measures are only progressing upwards
        /// Does not support streaming across sessions, where laps/distrance will naturally go down
        /// Also does not support is gaming is playing replay in reverse.
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public static IEnumerable<DataSample> WithCorrectedDistances(this IEnumerable<DataSample> samples)
        {
            var maxDistance = new float[64];
            var lastAdjustment = new int[64];

            foreach (var data in samples.ForwardOnly())
            {
                if (data.IsConnected)
                {
                    for (int i = 0; i < data.SessionData.DriverInfo.CompetingDrivers.Length; i++)
                        CorrectDistance(data.SessionData.DriverInfo.CompetingDrivers[i].UserName,
                            ref data.Telemetry.CarIdxLap[i],
                            ref data.Telemetry.CarIdxLapDistPct[i],
                            ref maxDistance[i],
                            ref lastAdjustment[i]);
                }
                yield return data;
            }
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
    }
}
