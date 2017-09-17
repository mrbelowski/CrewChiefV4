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
using System.Linq;
using System.Collections.Generic;
using iRacingSDK.Support;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {
        /// <summary>
        /// Assignes the telemetry fields HasSeenCheckeredFlag, IsFinalLap, LeaderHasFinished, HasRetired
        /// </summary>
        /// <param name="samples"></param>
        /// <returns></returns>
        public static IEnumerable<DataSample> WithFinishingStatus(this IEnumerable<DataSample> samples)
        {
            var hasSeenCheckeredFlag = new bool[64];
            var lastTimeForData = new TimeSpan[64];

            foreach (var data in samples)
            {
                ApplyIsFinalLap(data);

                ApplyLeaderHasFinished(data);

                ApplyHasSeenCheckeredFlag(data, hasSeenCheckeredFlag);

                ApplyHasRetired(data, lastTimeForData);

                yield return data;
            }
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

                if (lastTimeOfData[i] + 30.Seconds() < data.Telemetry.SessionTimeSpan)
                    data.Telemetry.HasRetired[i] = true;
            }
        }
    }
}
