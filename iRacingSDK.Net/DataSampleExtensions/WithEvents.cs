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
        /// Internal use in sdk only.
        /// Raise the connection and disconnection events as iRacing is started, stopped.
        /// </summary>
        internal static IEnumerable<DataSample> WithEvents(this IEnumerable<DataSample> samples, CrossThreadEvents connectionEvent, CrossThreadEvents disconnectionEvent, CrossThreadEvents<DataSample> newSessionData)
        {
            var isConnected = false;
            var isDisconnected = true;
            var lastSessionInfoUpdate = -1;

            foreach (var data in samples)
            {
                if (!isConnected && data.IsConnected)
                {
                    isConnected = true;
                    isDisconnected = false;
                    connectionEvent.Invoke();
                }

                if (!isDisconnected && !data.IsConnected)
                {
                    isConnected = false;
                    isDisconnected = true;
                    disconnectionEvent.Invoke();
                }

                if(data.IsConnected && data.SessionData.InfoUpdate != lastSessionInfoUpdate)
                {
                    lastSessionInfoUpdate = data.SessionData.InfoUpdate;
                    newSessionData.Invoke(data);
                }

                yield return data;
            }
        }
    }
}
