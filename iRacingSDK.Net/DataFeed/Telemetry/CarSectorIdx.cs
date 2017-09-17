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
		LapSector[] carSectorIdx;
        public LapSector[] CarSectorIdx //0 -> Start/Finish, 1 -> 33%, 2-> 66%
        {
            get
            {
                if (carSectorIdx != null)
                    return carSectorIdx;

                carSectorIdx = new LapSector[64];
                for(int i = 0; i < 64; i++)
					carSectorIdx[i] = new LapSector(this.CarIdxLap[i], ToSectorFromPercentage(CarIdxLapDistPct[i]));

                return carSectorIdx;
            }
        }

		static int ToSectorFromPercentage(float percentage)
		{
			if (percentage > 0.66)
				return 2;

			else if (percentage > 0.33)
				return 1;

			return 0;
		}
    }
}
