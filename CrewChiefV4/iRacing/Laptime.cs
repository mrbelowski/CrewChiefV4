using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;


namespace CrewChiefV4.iRacing
{
    public class Laptime
    {
        public Laptime() : this(0)
        {
            this.Time = TimeSpan.MaxValue;
        }

        public Laptime(int value)
        {
            this.Value = value;
            this.Time = TimeSpan.FromMilliseconds(value);
        }
        public Laptime(float seconds)
            : this((int)(seconds * 1000f))
        {
        }

        public int Value { get; set; }
        public TimeSpan Time { get; set; }
        public int LapNumber { get; set; }

        /// <summary>
        /// Formats a positive laptime in mm:sss.fff format. Use 'DiffDisplay' for displaying negative (differences in) laptimes.
        /// </summary>
        public string Display
        {
            get
            {
                if (this.Value <= 0 || this.Time == TimeSpan.MaxValue) return "-:--";
                return DiffDisplay;
            }
        }

        /// <summary>
        /// Formats a (difference in) laptimes in mm:sss.fff format. Works for negative laptimes too.
        /// </summary>
        public string DiffDisplay
        {
            get
            {
                bool isNeg = this.Value < 0;
                var time = this.Time;
                if (isNeg) time = this.Time.Negate();

                if (this.Time.Minutes > 0)
                    return string.Format("{0}{1:0}:{2:00}.{3:000}", isNeg ? "-": "", time.Minutes, time.Seconds, time.Milliseconds);
                return string.Format("{0}{1:00}.{2:000}", isNeg ? "-" : "", time.Seconds, time.Milliseconds);
            }
        }

        public string DisplayShort
        {
            get
            {
                if (this.Value <= 0) return "-:--";

                int precision = 1;
                const int TIMESPAN_SIZE = 7;
                int factor = (int) Math.Pow(10, (TIMESPAN_SIZE - precision));
                var rounded = new TimeSpan(((long)Math.Round((1.0 * this.Time.Ticks / factor)) * factor));

                if (rounded.Minutes > 0)
                {
                    var min = rounded.Minutes;
                    var sec = rounded.TotalSeconds - 60*min;
                    return string.Format("{0}:{1:00.0}", min, sec);
                }
                else
                {
                    var sec = rounded.TotalSeconds;
                    return string.Format("{0:0.0}", sec);
                }
            }
        }

        public static Laptime Empty
        {
            get
            {
                return new Laptime(0);
            }
        }
    }

    public class LaptimeCollection : List<Laptime>
    {
        public Laptime Average()
        {
            var validLaps = this.Where(l => l.Value > 0).ToList();
            if (validLaps.Count == 0) return Laptime.Empty;
            var averageMs = (int) validLaps.Average(l => l.Value);
            return new Laptime(averageMs);
        }
    }

    public class BestLap
    {
        public BestLap(Laptime lap, Driver driver)
        {
            this.Laptime = lap;

            this.DriverId = driver.CustId;
            this.DriverName = driver.Name;
            this.DriverNumber = driver.CarNumber;
            this.DriverTeamId = driver.TeamId;
            this.DriverTeamName = driver.TeamName;
        }

        public Laptime Laptime { get; private set; }

        public int DriverId { get; set; }
        public string DriverName { get; set; }
        public string DriverNumber { get; set; }
        public int DriverTeamId { get; set; }
        public string DriverTeamName { get; set; }

        public static BestLap Default
        {
            get
            {
                var lap = new Laptime(int.MaxValue);
                lap.LapNumber = 0;
                return new BestLap(lap, new Driver());
            }
        }
    }
}
