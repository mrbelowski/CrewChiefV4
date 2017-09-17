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
using System.Diagnostics;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;
using Win32.Synchronization;

namespace iRacingSDK
{
    class iRacingMemory
    {
        public MemoryMappedViewAccessor Accessor { get; private set; }
        IntPtr dataValidEvent;
        MemoryMappedFile irsdkMappedMemory;

        public bool IsConnected()
        {
            if (Accessor != null)
                return true;

            var dataValidEvent = Event.OpenEvent(Event.EVENT_ALL_ACCESS | Event.EVENT_MODIFY_STATE, false, "Local\\IRSDKDataValidEvent");
            if (dataValidEvent == IntPtr.Zero)
            {
                var lastError = Marshal.GetLastWin32Error();
                if (lastError == Event.ERROR_FILE_NOT_FOUND)
                    return false;

                Trace.WriteLine(string.Format("Unable to open event Local\\IRSDKDataValidEvent - Error Code {0}", lastError), "DEBUG");
                return false;
            }

            MemoryMappedFile irsdkMappedMemory = null;
            try
            {
                irsdkMappedMemory = MemoryMappedFile.OpenExisting("Local\\IRSDKMemMapFileName");
            }
            catch (Exception e)
            {
                Trace.WriteLine("Error accessing shared memory", "DEBUG");
                Trace.WriteLine(e.Message, "DEBUG");
            }

            if (irsdkMappedMemory == null)
                return false;

            var accessor = irsdkMappedMemory.CreateViewAccessor();
            if (accessor == null)
            {
                irsdkMappedMemory.Dispose();
                Trace.WriteLine("Unable to Create View into shared memory", "DEBUG");
                return false;
            }

            this.irsdkMappedMemory = irsdkMappedMemory;
            this.dataValidEvent = dataValidEvent;
            Accessor = accessor;
            return true;
        }

        public bool WaitForData()
        {
            return Event.WaitForSingleObject(dataValidEvent, 17) == 0;
        }
    }
}
