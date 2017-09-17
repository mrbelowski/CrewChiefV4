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
using System.Collections.Generic;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {
        /// <summary>
        /// Logs an error is frame numbers goes down - indicating the game is replaying in reverse.
        /// Sometimes stream may glitch and the FrameNum decements
        /// </summary>
        public static IEnumerable<DataSample> ForwardOnly(this IEnumerable<DataSample> samples)
        {
            foreach (var data in samples)
            {
                if (data.LastSample != null && data.LastSample.Telemetry.ReplayFrameNum > data.Telemetry.ReplayFrameNum)
                    TraceInfo.WriteLine(
                        "WARNING! Replay data reversed.  Current enumeration only support iRacing in forward mode. Received sample {0} after sample {1}",
                        data.Telemetry.ReplayFrameNum, data.LastSample.Telemetry.ReplayFrameNum);
                else
                    yield return data;
            }
        }
    }
}
