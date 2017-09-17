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
using Win32.Synchronization;

namespace Win32
{
    namespace Synchronization
    {
        internal static class Event
        {
            public const uint STANDARD_RIGHTS_REQUIRED = 0x000F0000;
            public const uint SYNCHRONIZE = 0x00100000;
            public const uint EVENT_ALL_ACCESS = (STANDARD_RIGHTS_REQUIRED | SYNCHRONIZE | 0x3);
            public const uint EVENT_MODIFY_STATE = 0x0002;
            public const long ERROR_FILE_NOT_FOUND = 2L;

            [DllImport("Kernel32.dll", SetLastError = true)]
            public static extern IntPtr OpenEvent(uint dwDesiredAccess, bool bInheritHandle, string lpName);

            [DllImport("kernel32", CharSet = CharSet.Ansi, SetLastError = true, ExactSpelling = true)]
            public static extern Int32 WaitForSingleObject(IntPtr Handle, Int32 Wait);
        }
    }
}