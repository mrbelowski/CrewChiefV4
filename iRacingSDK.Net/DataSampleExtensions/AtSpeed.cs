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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {
        public static IEnumerable<DataSample> AtSpeed(this IEnumerable<DataSample> samples, int replaySpeed, Func<DataSample, bool> fn)
        {
            var speedBeenSet = false;

            foreach (var data in samples)
            {
                if (fn(data) && !speedBeenSet && data.Telemetry.ReplayPlaySpeed != replaySpeed)
                {
                    iRacing.Replay.SetSpeed(replaySpeed);
                    speedBeenSet = true;
                }

                if (speedBeenSet)
                    if (data.Telemetry.ReplayPlaySpeed == replaySpeed)
                        speedBeenSet = false;

                yield return data;
            }
        }

        public static IEnumerable<DataSample> AtSpeed(this IEnumerable<DataSample> samples, int replaySpeed)
        {
            return AtSpeed(samples, replaySpeed, (data) => true);
        }
    }
}
