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
using System.Runtime.InteropServices;

namespace iRacingSDK
{
	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	public class iRSDKHeader
	{
		//12 bytes: offset = 0
		public int ver;
		public int status;
		public int tickRate;
		//12 bytes: offset = 12
		public int sessionInfoUpdate;
		public int sessionInfoLen;
		public int sessionInfoOffset;
		//8 bytes: offset = 24
		public int numVars;
		public int varHeaderOffset;
		//16 bytes: offset = 32
		public int numBuf;
		public int bufLen;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
		public int[] pad1;
		//128 bytes: offset = 48
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
		public VarBuf[] varBuf;
	}
	
}
