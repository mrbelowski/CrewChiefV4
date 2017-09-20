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
using System.Threading;
using Win32.Synchronization;
using System.Runtime.InteropServices;
using System.IO.MemoryMappedFiles;
using System.Diagnostics;
using iRacingSDK;
using iRacingSDK.Support;

namespace iRacingSDK
{
    public class iRacingConnection
    {
        readonly CrossThreadEvents connected = new CrossThreadEvents();
        readonly CrossThreadEvents disconnected = new CrossThreadEvents();
        readonly CrossThreadEvents<DataSample> newSessionData = new CrossThreadEvents<DataSample>();
        
        DataFeed dataFeed = null;
        bool isRunning = false;
        iRacingMemory iRacingMemory;
        internal bool IsRunning { get { return isRunning; } }

        long processingTime;
        public long ProcessingTime { get { return processingTime * 1000000L / Stopwatch.Frequency; } }

        long waitingTime;
        public long WaitingTime { get { return waitingTime * 1000000L / Stopwatch.Frequency; } }

        long yieldTime;
        public long YieldTime { get { return (yieldTime * 1000000L / Stopwatch.Frequency) ; } }

        public readonly Replay Replay;
        public readonly PitCommand PitCommand;

        public bool IsConnected { get; private set; }

        public event Action Connected
        {
            add { connected.Event += value; }
            remove { connected.Event -= value; }
        }

        public event Action Disconnected
        {
            add { disconnected.Event += value; }
            remove { disconnected.Event -= value; }
        }

        public event Action<DataSample> NewSessionData
        {
            add { newSessionData.Event += value; }
            remove { newSessionData.Event -= value; }
        }

        public iRacingConnection()
        {
            this.Replay = new Replay(this);
            this.PitCommand = new PitCommand();
            this.iRacingMemory = new iRacingMemory();
        }

        public IEnumerable<DataSample> GetDataFeed(bool logging = true)
        {
            return GetRawDataFeed(logging).WithLastSample().WithEvents(connected, disconnected, newSessionData);
        }

        internal IEnumerable<DataSample> GetRawDataFeed(bool logging = true)
        {
            if (isRunning)
                throw new Exception("Can not call GetDataFeed concurrently.");

            isRunning = true;
            try
            {
                foreach (var notConnectedSample in WaitForInitialConnection())
                {
                    IsConnected = false;
                    yield return notConnectedSample;
                }

                foreach (var sample in AllSamples(logging))
                {
                    IsConnected = sample.IsConnected;
                    yield return sample;
                }
            }
            finally
            {
                isRunning = false;
            }
        }

        IEnumerable<DataSample> WaitForInitialConnection()
        {
            bool wasConnected = iRacingMemory.Accessor != null;
            TraceInfo.WriteLineIf(!wasConnected, "Waiting to connect to iRacing application");

            while (!iRacingMemory.IsConnected())
            {
                yield return DataSample.YetToConnected;
                Thread.Sleep(10);
            }

            TraceInfo.WriteLineIf(!wasConnected, "Connected to iRacing application");
        }

        IEnumerable<DataSample> AllSamples(bool logging)
        {
            if (dataFeed == null)
                dataFeed = new DataFeed(iRacingMemory.Accessor);

            var nextTickCount = 0;
            var lastTickTime = DateTime.Now;

            var watchProcessingTime = new Stopwatch();
            var watchWaitingTime = new Stopwatch();

            while (true)
            {
                watchWaitingTime.Restart();
                iRacingMemory.WaitForData();
                waitingTime = watchWaitingTime.ElapsedTicks;

                watchProcessingTime.Restart();

                var data = dataFeed.GetNextDataSample(nextTickCount, logging);
                if (data != null)
                {
                    if (data.IsConnected)
                    {
                        if (data.Telemetry.TickCount == nextTickCount - 1)
                            continue; //Got the same sample - try again.

                        if (logging && data.Telemetry.TickCount != nextTickCount && nextTickCount != 0)
                            Console.WriteLine("Dropped DataSample from {0} to {1}. Over time of {2}",
                                nextTickCount, data.Telemetry.TickCount - 1, (DateTime.Now - lastTickTime).ToString(@"s\.fff"), "WARN");

                        nextTickCount = data.Telemetry.TickCount + 1;
                        lastTickTime = DateTime.Now;
                    }
                    processingTime = watchProcessingTime.ElapsedTicks;

                    watchProcessingTime.Restart();
                    yield return data;
                    yieldTime = watchProcessingTime.ElapsedTicks;
                }
            }
        }
    }
}
