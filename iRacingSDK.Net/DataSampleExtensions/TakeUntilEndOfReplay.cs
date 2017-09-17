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
	public static partial class DataSampleExtensions
	{
		/// <summary>
		/// Assume replay is playing forward
        /// If Frame numbers do not advance after 100 identical framenumbers, the enumerator is stopped
		/// </summary>
		public static IEnumerable<DataSample> TakeUntilEndOfReplay(this IEnumerable<DataSample> samples)
		{
            const int MaxRetryCount = 100;
            var retryCount = MaxRetryCount;
            var lastFrameNumber = -1;

			foreach (var data in samples)
			{
                if (lastFrameNumber == data.Telemetry.ReplayFrameNum)
                {
                    if (retryCount-- <= 0)
                        break;
                }
                else
                {
                    retryCount = MaxRetryCount;
                    lastFrameNumber = data.Telemetry.ReplayFrameNum;
                }

				yield return data;
			}
		}
	}
}

