using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    public class TimeSpanWrapper
    {
        public TimeSpan timeSpan;

        public Boolean readSeconds;

        public TimeSpanWrapper(TimeSpan timeSpan)
        {
            this.timeSpan = timeSpan;
            this.readSeconds = false;
        }

        public TimeSpanWrapper(TimeSpan timeSpan, Boolean readSeconds)
        {
            this.timeSpan = timeSpan;
            this.readSeconds = readSeconds;
        }

        public static TimeSpanWrapper FromSeconds(float seconds, Boolean readSeconds)
        {
            return new TimeSpanWrapper(TimeSpan.FromSeconds(seconds), readSeconds);
        }
        public static TimeSpanWrapper FromMilliseconds(float milliseconds, Boolean readSeconds)
        {
            return new TimeSpanWrapper(TimeSpan.FromMilliseconds(milliseconds), readSeconds);
        }
    }
}
