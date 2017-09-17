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
using System.Runtime.Serialization;

namespace iRacingSDK
{
	[Serializable]
	public partial class Telemetry : Dictionary<string, object>
	{
		public Telemetry()
		{
		}

		public Telemetry(SerializationInfo info, StreamingContext context) : base(info, context) {

		}
	}

	[Serializable]
	public partial class SessionData
	{
		[Serializable]
		public partial class _WeekendInfo
		{
			[Serializable]
			public partial class _WeekendOptions
			{
			}
				
			[Serializable]
			public partial class _TelemetryOptions
			{
			}
		}
			
		[Serializable]
		public partial class _SessionInfo
		{
			[Serializable]
			public partial class _Sessions
			{
				[Serializable]
				public partial class _ResultsPositions
				{
				}

				[Serializable]
				public partial class _ResultsFastestLap
				{
				}
			}
		}

		[Serializable]
		public partial class _CameraInfo
		{
			[Serializable]
			public partial class _Groups
			{
				[Serializable]
				public partial class _Cameras
				{
				}
			}
		}

		[Serializable]
		public partial class _RadioInfo
		{
			[Serializable]
			public partial class _Radios
			{
			}
		}

		[Serializable]
		public partial class _DriverInfo
		{
			[Serializable]
			public partial class _Drivers
			{
			}
		}

		[Serializable]
		public partial class _SplitTimeInfo
		{
			[Serializable]
			public partial class _Sectors
			{
			}
		}
	}
}
