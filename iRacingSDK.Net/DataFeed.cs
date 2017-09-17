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

using iRacingSDK.Support;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace iRacingSDK
{
    public class DataFeed
    {
        MemoryMappedViewAccessor accessor;

        public DataFeed(MemoryMappedViewAccessor accessor)
        {
            this.accessor = accessor;
        }

        public unsafe DataSample GetNextDataSample(int requestedTickCount, bool logging)
        {
            var headers = accessor.AcquirePointer(ptr =>
            {
                var a = ReadHeader(ptr);
                var b = ReadVariableHeaders(a, ptr);
                return new { Header = a, VarHeaders = b };
            });

            if ((headers.Header.status & 1) == 0)
                return DisconnectedSample();

            var sessionData = ReadSessionInfo(headers.Header);
            var variables = ReadVariables(headers.Header, headers.VarHeaders, requestedTickCount, logging);
            var variableDescriptions = headers.VarHeaders.ToDictionary(vh => vh.name, vh => vh.desc);
            if(variables != null)
                variables.Descriptions = variableDescriptions;

            if (sessionData == null)
                return DisconnectedSample();

            if (variables == null)
                return null;

            variables.SessionData = sessionData;
            return new DataSample { Telemetry = variables, SessionData = sessionData, TelemetryDescription = variableDescriptions, IsConnected = true };
        }

        unsafe iRSDKHeader ReadHeader(byte* ptr)
        {
            return (iRSDKHeader)System.Runtime.InteropServices.Marshal.PtrToStructure(new IntPtr(ptr), typeof(iRSDKHeader));
        }

        static readonly int size = System.Runtime.InteropServices.Marshal.SizeOf(typeof(VarHeader));

        unsafe VarHeader[] ReadVariableHeaders(iRSDKHeader header, byte* ptr)
        {
            var varHeaders = new VarHeader[header.numVars];
            ptr += header.varHeaderOffset;

            for (var i = 0; i < header.numVars; i++)
            {
                varHeaders[i] = (VarHeader)Marshal.PtrToStructure(new IntPtr(ptr), typeof(VarHeader));
                ptr += size;
            }

            return varHeaders;
        }

        int sessionLastInfoUpdate = -2;
        SessionData lastSessionInfo;

        DataSample DisconnectedSample()
        {
            lastSessionInfo = null;
            sessionLastInfoUpdate = -2;
            return DataSample.YetToConnected;
        }

        SessionData ReadSessionInfo(iRSDKHeader header)
        {
            if (header.sessionInfoUpdate == sessionLastInfoUpdate)
                return lastSessionInfo;

            sessionLastInfoUpdate = header.sessionInfoUpdate;
            Trace.WriteLine("New Session data retrieved from iRacing. {0}".F(sessionLastInfoUpdate), "DEBUG");

            var t = Task.Factory.StartNew(() => {

                try {
                    var sessionInfoData = new byte[header.sessionInfoLen];
                    accessor.ReadArray<byte>(header.sessionInfoOffset, sessionInfoData, 0, header.sessionInfoLen);
                    var sessionInfoString = System.Text.Encoding.Default.GetString(sessionInfoData);

                    var length = sessionInfoString.IndexOf('\0');
                    if (length == -1)
                    {
                        lastSessionInfo = null;
                        return;
                    }

                    sessionInfoString = sessionInfoString.Substring(0, sessionInfoString.IndexOf('\0'));
                    Trace.WriteLine(sessionInfoString, "DEBUG");
                    lastSessionInfo = DeserialiseSessionInfo(sessionInfoString, header.sessionInfoUpdate);
                }
                catch(Exception e)
                {
                    TraceError.WriteLine(e.Message);
                    TraceError.WriteLine(e.StackTrace);
                }
            });

            if (lastSessionInfo == null)
                t.Wait();

            return lastSessionInfo;
        }

        static SessionData DeserialiseSessionInfo(string sessionInfoString, int sessionInfoUpdate)
        {
            if (sessionInfoString.Length == 0)
                return null;

            try
            {
                sessionInfoString.Replace(": *", ": ");
                var input = new StringReader(sessionInfoString);

                var deserializer = new Deserializer(ignoreUnmatched: true);

                var result = (SessionData)deserializer.Deserialize(input, typeof(SessionData));
                result.Raw = sessionInfoString.Replace("\n", "\r\n");
                result.InfoUpdate = sessionInfoUpdate;
                return result;
            }
            catch (Exception e)
            {
                Trace.WriteLine(string.Format("Error decoding session yml data {0}", e.Message), "DEBUG");

                return null;
            }
        }

        unsafe Telemetry ReadVariables(iRSDKHeader header, VarHeader[] varHeaders, int requestedTickCount, bool logging)
        {
            var buf = header.FindLatestBuf(requestedTickCount);

            if (buf.index == -1)
            {
                Trace.WriteLine("No data in iRacing buffers", "DEBUG");
                return null;
            }

            var values = ReadAllValues(accessor, buf.bufOffset, varHeaders);
            var latestHeader = accessor.AcquirePointer(ptr => ReadHeader(ptr));

            if (latestHeader.HasChangedSinceReading(buf))
            {
                Trace.WriteLineIf(logging, "Failed to read data before iRacing overwrote new sample!", "DEBUG");
                return null;
            }

            values.Add("TickCount", buf.tickCount);
            return values;
        }

        static Telemetry ReadAllValues(MemoryMappedViewAccessor accessor, int buffOffset, VarHeader[] varHeaders)
        {
            var result = new Telemetry();

            var maps = new Dictionary<VarType, Func<int, object>>() {
                { VarType.irInt, (offset) => accessor.ReadInt32(offset) },
                { VarType.irBitField, (offset) => accessor.ReadInt32(offset) },
                { VarType.irDouble, (offset) => accessor.ReadDouble(offset) },
                { VarType.irBool, (offset) => accessor.ReadBoolean(offset) },
                { VarType.irFloat, (offset) => accessor.ReadSingle(offset) }
            };

            var arryMaps = new Dictionary<VarType, Func<int, int, object>>() {
                { VarType.irInt, (size, offset) => GetArrayData<int>(accessor, size, offset) },
                { VarType.irBitField, (size, offset) => GetArrayData<int>(accessor, size, offset) },
                { VarType.irDouble, (size, offset) => GetArrayData<double>(accessor, size, offset) },
                { VarType.irFloat, (size, offset) => GetArrayData<float>(accessor, size, offset) },
                { VarType.irBool, (size, offset) => GetArrayData<bool>(accessor, size, offset) }
            };

            for (var i = 0; i < varHeaders.Length; i++)
            {
                var varHeader = varHeaders[i];
                var offset = buffOffset + varHeader.offset;

                if (varHeader.type == VarType.irChar)
                    throw new NotSupportedException();

                object value;
                if (varHeader.count != 1)
                    value = arryMaps[varHeader.type](varHeader.count, offset);
                else
                    value = maps[varHeader.type](offset);

                result.Add(varHeader.name, value);
            }

            return result;
        }

        static T[] GetArrayData<T>(MemoryMappedViewAccessor accessor, int size, int offset) where T : struct
        {
            var data = new T[size];
            accessor.ReadArray<T>(offset, data, 0, size);
            return data;
        }
    }
}
