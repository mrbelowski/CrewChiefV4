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
using System.Linq;
using System.Threading;

namespace iRacingSDK
{
    public static class IncidentsSupport
    {
        /// <summary>
        /// Pauses the game and then uses the 'toNext' Action to send a message to the game to advance through all incidents
        /// Returns a list of DataSamples for each frame of each incident
        /// </summary>
        /// <param name="samples"></param>
        /// <param name="toNext">A function to that sends ither move to next incident or move to previous incident</param>
        /// <param name="sampleScanSettle">The number of samples to take, which must all have the same frame number - to identify that iRacing has paused.</param>
        /// <param name="maxTotalIncidents">an optional hard limit on the total number of incidents that can be returned.</param>
        /// <returns>DataSamples of each incident</returns>
        public static List<DataSample> FindIncidents(IEnumerable<DataSample> samples, int sampleScanSettle, int maxTotalIncidents = int.MaxValue)
        {
            if (maxTotalIncidents <= 0)
                return new List<DataSample>();

            iRacing.Replay.SetSpeed(0);
            iRacing.Replay.Wait();

            return samples.PositionsOf(sampleScanSettle).Take(maxTotalIncidents).ToList();
        }

        enum states {Continue, RequestPaceCar, RequestNextIncident, Yield, Break };

    /// <summary>
    /// Assumes the game is in paused mode
    /// Filters the samples to just single frame numbers, that the game moves after invoking the moveReplay action (eg: which sends message to the game - MoveToNextIncident)
    /// If the game does not advance to another frame within about 1second, it will stop enumerating
    /// </summary>
    /// <param name="samples"></param>
    /// <param name="moveReplay"></param>
    /// <param name="sampleScanSettle">The number of samples to take, which must all have the same frame number - to identify that iRacing has paused.</param>
    /// <returns></returns>
    static IEnumerable<DataSample> PositionsOf(this IEnumerable<DataSample> samples, int sampleScanSettle)
        {
            var lastSamples = new List<int>();
            var lastFrameNumber = -2;

            Func<DataSample, states> waitForMessage = ignored => states.RequestPaceCar;
            foreach (var dx in samples)
            {
                switch(waitForMessage(dx) )
                {
                    case states.Continue:
                        break;

                    case states.RequestPaceCar:
                        iRacing.Replay.NoWait.CameraOnDriver(0, 0);
                        waitForMessage = d => d.Telemetry.CamCarIdx == 0 ? states.RequestNextIncident : states.Continue;
                        break;

                    case states.RequestNextIncident:
                        iRacing.Replay.NoWait.MoveToNextIncident();
                        waitForMessage = d =>
                        {
                            lastSamples.Add(d.Telemetry.ReplayFrameNum);

                            if (lastSamples.Count == sampleScanSettle)
                                lastSamples.RemoveAt(0);

                            if (lastSamples.Count == (sampleScanSettle - 1) && lastSamples.All(f => f == d.Telemetry.ReplayFrameNum))
                            {
                                if (d.Telemetry.ReplayFrameNum == lastFrameNumber)
                                {
                                    TraceDebug.WriteLine("Incidents: Frame number did not change - asuming no more incidents.  Current Frame: {0}", d.Telemetry.ReplayFrameNum);
                                    return states.Break;
                                }

                                if (d.Telemetry.CamCarIdx == 0) //Pace Car
                                {
                                    TraceWarning.WriteLine("Incident scan aborted - iRacing has not progressed to incident.  Frame Number: {0}", d.Telemetry.ReplayFrameNum);
                                    return states.Break;
                                }

                                lastFrameNumber = d.Telemetry.ReplayFrameNum;
                                TraceDebug.WriteLine("Incidents: last {0} samples have settled on frame number {1}", sampleScanSettle, d.Telemetry.ReplayFrameNum);

                                lastSamples.Add(-1);
                                lastSamples.RemoveAt(0);
                                return states.Yield;
                            };

                            return states.Continue;
                        };
                        break;

                    case states.Yield:
                        yield return dx;
                        waitForMessage = ignored => states.RequestPaceCar;
                        break;

                    case states.Break:
                        yield break;

                }
            }
        }
    }
}
