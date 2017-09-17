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

namespace iRacingSDK
{
    public partial class Telemetry : Dictionary<string, object>
    {
        LapSector? _raceLapSector;
        public LapSector RaceLapSector
        {
            get
            {
                if (_raceLapSector != null)
                    return _raceLapSector.Value;

                var firstSector = this.CarIdxLap
                    .Select((lap, idx) => new { Lap = lap, Idx = idx, Pct = this.CarIdxLapDistPct[idx] })
                    .Where(l => l.Lap == this.RaceLaps)
                    .OrderByDescending(l => l.Pct)
                    .FirstOrDefault();

                if (firstSector == null)
                    return (_raceLapSector = new LapSector(this.RaceLaps, 2)).Value;

                return (_raceLapSector = new LapSector(this.RaceLaps, ToSectorFromPercentage(firstSector.Pct))).Value;
            }
        }
    }
}