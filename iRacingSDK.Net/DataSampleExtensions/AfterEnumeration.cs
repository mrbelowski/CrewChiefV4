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

namespace iRacingSDK
{
    public class AfterEnumeration
    {
        readonly IEnumerable<DataSample> samples;
        readonly TimeSpan period;

        public AfterEnumeration(IEnumerable<DataSample> samples, TimeSpan period)
        {
            this.samples = samples;
            this.period = period;
        }

        /// <summary>
        /// Once supplied function returns true, iteration stops after the specified period
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IEnumerable<DataSample> After(Func<DataSample, bool> condition)
        {
            bool conditionMet = false;
            TimeSpan conditionMetAt = new TimeSpan();

            foreach( var data in samples)
            {
                if(!conditionMet && condition(data))
                {
                    conditionMet = true;
                    conditionMetAt = data.Telemetry.SessionTimeSpan;
                }

                if (conditionMet && conditionMetAt + period < data.Telemetry.SessionTimeSpan)
                    break;

                yield return data;
            }
        }

        /// <summary>
        /// If the supplied function returns true for the period, then iteration stops
        /// </summary>
        /// <param name="condition"></param>
        /// <returns></returns>
        public IEnumerable<DataSample> Of(Func<DataSample, bool> condition)
        {
            bool conditionMet = false;
            TimeSpan conditionMetAt = new TimeSpan();

            foreach (var data in samples)
            {
                if (condition(data))
                {
                    if (!conditionMet)
                    {
                        TraceDebug.WriteLine("{0}: Condition met".F(data.Telemetry.SessionTimeSpan));
                        conditionMet = true;
                        conditionMetAt = data.Telemetry.SessionTimeSpan;
                    }
                }
                else
                {
                    if(conditionMet)
                        TraceDebug.WriteLine("{0}: Condition unmet".F(data.Telemetry.SessionTimeSpan));
                    conditionMet = false;
                }


                if (conditionMet && conditionMetAt + period < data.Telemetry.SessionTimeSpan)
                    break;

                yield return data;
            }
        }

        public IEnumerable<DataSample> AfterReplayPaused()
        {
            var timeoutAt = DateTime.Now + period;
            var lastFrameNumber = -1;

            foreach (var data in samples)
            {
                if (lastFrameNumber == data.Telemetry.ReplayFrameNum)
                {
                    if (timeoutAt < DateTime.Now)
                    {
                        TraceInfo.WriteLine("{0} Replay paused for {1}.  Assuming end of replay", data.Telemetry.SessionTimeSpan, period);
                        break;
                    }
                }
                else
                {
                    timeoutAt = DateTime.Now + period;
                    lastFrameNumber = data.Telemetry.ReplayFrameNum;
                }

                yield return data;
            }
        }
    }
}
