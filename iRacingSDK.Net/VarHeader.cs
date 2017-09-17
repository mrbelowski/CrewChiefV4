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
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using System.Collections.Generic;
using System.Threading;
using System.Linq;

namespace iRacingSDK
{

	[StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
	internal struct VarHeader
	{
		//16 bytes: offset = 0
		public VarType type;
		//offset = 4
		public int offset;
		//offset = 8
		public int count;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 1)]
		public int[] pad;
		//32 bytes: offset = 16
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Defines.MaxString)]
		public string name;
		//64 bytes: offset = 48
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Defines.MaxDesc)]
		public string desc;
		//32 bytes: offset = 112
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = Defines.MaxString)]
		public string unit;
	}
	
}
