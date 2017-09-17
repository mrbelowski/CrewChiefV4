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

namespace iRacingSDK
{
	public partial class Telemetry : Dictionary<string, object>
    {
        float? raceDistance;

        public float RaceDistance
        {
            get
            {
                if (raceDistance != null)
                    return raceDistance.Value;

                raceDistance = this.CarIdxLap
                    .Select((lap, idx) => new { Lap = lap, Distance = lap + this.CarIdxLapDistPct[idx] })
                    .Max(l => l.Distance);

                if (raceDistance.Value < this.RaceLaps)
                {
                    Trace.WriteLine("WARNING! No cars on current RaceLaps", "DEBUG");
                    return this.RaceLaps;
                }

                return raceDistance.Value;
            }
        }
	}
}
