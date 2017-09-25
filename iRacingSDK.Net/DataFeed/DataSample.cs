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
	[Serializable]
    public class DataSample
    {
        private Telemetry telemetry;
        private SessionData sessionData;

        public static readonly DataSample YetToConnected = new DataSample { IsConnected = false };
        public DataSample LastSample { get; set; }

        public bool IsConnected { get; internal set; }
        public SessionData SessionData
        {
            get
            {
                //if (!IsConnected)
                    //throw new Exception("Attempt to read session data before connection to iRacing");

                //if (sessionData == null)
                    //throw new Exception("SessionData is null");

                return sessionData;
            }
            set 
            {
                sessionData = value;
            }
        }

        public Telemetry Telemetry
        {
            get
            {
                //if (!IsConnected)
                    //throw new Exception("Attempt to read telemetry data before connection to iRacing");

                //if (telemetry == null)
                    //throw new Exception("Telemetry is null");

                return telemetry;
            }
            set
            {
                telemetry = value;
            }
        }
        
        public Dictionary<string, string> TelemetryDescription { get; internal set; }
    }
}
