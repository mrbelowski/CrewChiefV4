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

namespace iRacingSDK
{
    public static class iRSDKHeaderExtensions
    {
        public static bool HasChangedSinceReading(this iRSDKHeader header, VarBufWithIndex buf)
        {
            return header.varBuf[buf.index].tickCount != buf.tickCount;
        }

        public static VarBufWithIndex FindLatestBuf(this iRSDKHeader header, int requestedTickCount)
        {
            VarBuf maxBuf = new VarBuf();
            int maxIndex = -1;

            for (var i = 0; i < header.numBuf; i++)
            {
                var b = header.varBuf[i];

                if (b.tickCount == requestedTickCount)
                    return new VarBufWithIndex() { tickCount = requestedTickCount, bufOffset = b.bufOffset, index = i };

                if (b.tickCount > maxBuf.tickCount)
                {
                    maxBuf = b;
                    maxIndex = i;
                }
            }

            return new VarBufWithIndex() { tickCount = maxBuf.tickCount, bufOffset = maxBuf.bufOffset, index = maxIndex };
        }
    }
}
