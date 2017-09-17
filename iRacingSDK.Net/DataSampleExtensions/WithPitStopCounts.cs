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

using System.Collections.Generic;
using System.Linq;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {
        /// <summary>
        /// Set the CarIdxPitStopCount field for each enumerted datasample's telemetry
        /// </summary>
        public static IEnumerable<DataSample> WithPitStopCounts(this IEnumerable<DataSample> samples)
        {
            var lastTrackLocation = Enumerable.Repeat(TrackLocation.NotInWorld, 64).ToArray();
            var carIdxPitStopCount = new int[64];

            foreach (var data in samples.ForwardOnly())
            {
                CapturePitStopCounts(lastTrackLocation, carIdxPitStopCount, data);

                data.Telemetry.CarIdxPitStopCount = (int[])carIdxPitStopCount.Clone();

                yield return data;
            }
        }

        static void CapturePitStopCounts(TrackLocation[] lastTrackLocation, int[] carIdxPitStopCount, DataSample data)
        {
            if (data.LastSample == null)
                return;

            CaptureLastTrackLocations(lastTrackLocation, data);
            IncrementPitStopCounts(lastTrackLocation, carIdxPitStopCount, data);
        }

        static void CaptureLastTrackLocations(TrackLocation[] lastTrackLocation, DataSample data)
        {
            var last = data.LastSample.Telemetry.CarIdxTrackSurface;
            for (var i = 0; i < last.Length; i++)
                if (last[i] != TrackLocation.NotInWorld)
                    lastTrackLocation[i] = last[i];
        }

        static void IncrementPitStopCounts(TrackLocation[] lastTrackLocation, int[] carIdxPitStopCount, DataSample data)
        {
            var current = data.Telemetry.CarIdxTrackSurface;
            for (var i = 0; i < current.Length; i++)
                if (lastTrackLocation[i] != TrackLocation.InPitStall && current[i] == TrackLocation.InPitStall)
                    carIdxPitStopCount[i] += 1;
        }
    }
}
