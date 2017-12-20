﻿using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.NumberProcessing
{
    public class TimeSpanWrapper
    {
        public TimeSpan timeSpan;
        private Precision precision;
        private static TimeSpan gapsInHundredthsThreshold = TimeSpan.FromMilliseconds(500);
        private static TimeSpan ovalGapsInHundredthsThreshold = TimeSpan.FromMilliseconds(2000);
        private static TimeSpan gapsSecondsThreshold = TimeSpan.FromSeconds(10);

        public TimeSpanWrapper(TimeSpan timeSpan, Precision precision)
        {
            this.timeSpan = timeSpan;
            this.precision = precision;
        }

        public Precision getPrecision()
        {
            if (precision == Precision.AUTO_GAPS) 
            {
                if (timeSpan > gapsSecondsThreshold)
                {
                    return Precision.SECONDS;
                }
                else if (GlobalBehaviourSettings.useHundredths && 
                    (timeSpan < gapsInHundredthsThreshold ||
                    (GlobalBehaviourSettings.useOvalLogic && timeSpan < ovalGapsInHundredthsThreshold)))
                {
                    return Precision.HUNDREDTHS;
                }
                else
                {
                    return Precision.TENTHS;
                }
            }
            else if (precision == Precision.AUTO_LAPTIMES)
            {
                if (GlobalBehaviourSettings.useHundredths)
                {
                    return Precision.HUNDREDTHS;
                }
                else
                {
                    return Precision.TENTHS;
                }
            }
            else
            {
                return precision;
            }
        }

        public static TimeSpanWrapper FromMinutes(int minutes, Precision precision)
        {
            return new TimeSpanWrapper(TimeSpan.FromMinutes(minutes), precision);
        }

        public static TimeSpanWrapper FromSeconds(int seconds, Precision precision)
        {
            return new TimeSpanWrapper(TimeSpan.FromSeconds(seconds), precision);
        }

        public static TimeSpanWrapper FromMilliseconds(int milliseconds, Precision precision)
        {
            return new TimeSpanWrapper(TimeSpan.FromMilliseconds(milliseconds), precision);
        }

        public static TimeSpanWrapper FromMinutes(float minutes, Precision precision)
        {
            return new TimeSpanWrapper(TimeSpan.FromMinutes(minutes), precision);
        }

        public static TimeSpanWrapper FromSeconds(float seconds, Precision precision)
        {
            return new TimeSpanWrapper(TimeSpan.FromSeconds(seconds), precision);
        }

        public static TimeSpanWrapper FromMilliseconds(float milliseconds, Precision precision)
        {
            return new TimeSpanWrapper(TimeSpan.FromMilliseconds(milliseconds), precision);
        }
    }

    public enum Precision {
        AUTO_GAPS /* used for gaps - will report hundredths for gaps in oval races, if the 'prefer hundredths' is set, or if gap < 0.2, otherwise tenths. */,
        AUTO_LAPTIMES /* used for laptimes - will report hundredthds for gaps in oval races, if the 'prefer hundredths' is set, otherwise tenths. */, 
        HUNDREDTHS, 
        TENTHS, 
        SECONDS
    }
}
