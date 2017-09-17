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

namespace iRacingSDK
{
    public partial class Telemetry : Dictionary<string, object>
    {
        public bool[] HasSeenCheckeredFlag;
        public bool IsFinalLap;
        public bool LeaderHasFinished;
        public bool[] HasRetired;
            
        public bool HasData(int carIdx)
        {
            return this.CarIdxTrackSurface[carIdx] != TrackLocation.NotInWorld;
        }
    }
}
