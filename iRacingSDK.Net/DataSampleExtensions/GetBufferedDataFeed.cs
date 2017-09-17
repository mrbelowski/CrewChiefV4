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

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Threading.Tasks;

namespace iRacingSDK
{
    public static partial class DataSampleExtensions
    {
        /// <summary>
        /// Similiar to GetDataFeed, except DataSample can be buffered upto maxBufferLength to asist in reducing loss of data packets
        /// Therefore, DataSamples yield from this enumeration may have a higher latency of values.
        /// </summary>
        /// <param name="iRacingConnection"></param>
        /// <param name="maxBufferLength"></param>
        /// <returns></returns>
        public static IEnumerable<DataSample> GetBufferedDataFeed(this iRacingConnection iRacingConnection, int maxBufferLength = 10)
        {
            return _GetBufferedDataFeed(iRacingConnection, maxBufferLength).WithLastSample();
        }

        static IEnumerable<DataSample> _GetBufferedDataFeed(iRacingConnection iRacingConnection, int maxBufferLength)
        {
            var que = new ConcurrentQueue<DataSample>();
            bool cancelRequest = false;

            var t = new Task(() => EnqueueSamples(que, iRacingConnection, maxBufferLength, ref cancelRequest));
            t.Start();

            try
            {
                DataSample data;

                while (true)
                {
                    if (que.TryDequeue(out data))
                        yield return data;
                }
            }
            finally
            {
                cancelRequest = true;
                t.Wait(200);
                t.Dispose();
            }
        }

        static void EnqueueSamples(ConcurrentQueue<DataSample> que, iRacingConnection samples, int maxBufferLength, ref bool cancelRequest)
        {
            foreach (var data in samples.GetRawDataFeed())
            {
                if (cancelRequest)
                    return;

                if (que.Count < maxBufferLength)
                    que.Enqueue(data);
                else
                    Debug.WriteLine(string.Format("Dropped DataSample {0}.", data.Telemetry.TickCount));
            }
        }
    }
}
