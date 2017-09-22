using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;
using System.IO.Compression;
using iRacingSDK;
namespace CrewChiefV4.iRacing
{

    public class DriverTimings
    {
        public List<Car> drivers = new List<Car>();
        public DriverTimings(List<Car> cars)
        {
            drivers = cars.OrderBy(d => d.Position).ToList();
        }
            
    }
    public class Laptime
    {
        public Laptime()
            : this(0)
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
                    return string.Format("{0}{1:0}:{2:00}.{3:000}", isNeg ? "-" : "", time.Minutes, time.Seconds, time.Milliseconds);
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
                int factor = (int)Math.Pow(10, (TIMESPAN_SIZE - precision));
                var rounded = new TimeSpan(((long)Math.Round((1.0 * this.Time.Ticks / factor)) * factor));

                if (rounded.Minutes > 0)
                {
                    var min = rounded.Minutes;
                    var sec = rounded.TotalSeconds - 60 * min;
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
            if (validLaps.Count == 0) 
                return Laptime.Empty;

            var averageMs = (int)validLaps.Average(l => l.Value);
            return new Laptime(averageMs);
        }
    }

    public class TimeDelta
    {

        private Single splitdistance = 10;

        private Int32 maxcars = 64;
        private Double[][] splits = new Double[0][];
        private Int32[] splitPointer = new Int32[0];
        private Single splitLength;
        private Double prevTimestamp;
        private Int32 followed;
        private Double[] bestlap;
        private Double[] currentlap;
        private Boolean validbestlap;
        private Double lapstarttime;
        private Int32 arraySize;
        public Double[] currentlapTime = new Double[64];

        public TimeDelta(Single length, Single splitdist, Int32 drivers)
        {
            // save split distance
            this.splitdistance = splitdist;

            // save car count
            maxcars = drivers;
            // split times every 10 meters
            arraySize = (Int32)Math.Round(length / splitdistance);

            // set split length
            splitLength = (Single)(1.0 / (Double)arraySize);

            // init best lap
            followed = -1;
            bestlap = new Double[arraySize];
            currentlap = new Double[arraySize];
            validbestlap = false;

            // initialize array
            splits = new Double[maxcars][];
            splitPointer = new Int32[maxcars];
            for (Int32 i = 0; i < maxcars; i++)
                splits[i] = new Double[arraySize];
        }

        public void SaveBestLap(Int32 caridx)
        {
            followed = caridx;
        }

        public TimeSpan BestLap { get { if (validbestlap) return new TimeSpan(0, 0, 0, (Int32)bestlap[bestlap.Length - 1], (Int32)((bestlap[bestlap.Length - 1] % 1) * 1000)); else return new TimeSpan(); } set { } }

        public void Update(Double timestamp, Single[] trackPosition)
        {
            Double[] temp = Array.ConvertAll(trackPosition, item => (Double)item);
            Update(timestamp, temp);
        }

        public void Update(Double timestamp, Double[] trackPosition)
        {
            // sanity check
            if (timestamp > prevTimestamp)
            {
                Int32 currentSplitPointer;

                for (Int32 i = 0; i < trackPosition.Length; i++)
                {
                    if (trackPosition[i] > 0)
                    {
                        // interpolate split border crossing
                        currentSplitPointer = (Int32)Math.Floor((trackPosition[i] % 1) / splitLength);

                        if (currentSplitPointer != splitPointer[i])
                        {
                            // interpolate
                            Double distance = trackPosition[i] - (currentSplitPointer * splitLength);
                            Double correction = distance / splitLength;
                            Double currentSplitTime = timestamp - ((timestamp - prevTimestamp) * correction);
                            currentlapTime[i] = currentSplitTime;
                            Boolean newlap = false;

                            if (currentSplitPointer < (100 / splitdistance) && splitPointer[i] > arraySize - (100 / splitdistance))
                                newlap = true;

                            // check if we need interpolation over zero values (splithop > 1)
                            Int32 splithop = currentSplitPointer - splitPointer[i];
                            Double splitcumulator = (currentSplitTime - prevTimestamp) / splithop;
                            Int32 k = 1;

                            // check if we crossed the s/f-line (2*10 split threshold, otherwise we miss it)
                            if (splithop < 0 && newlap)
                            {
                                splithop = arraySize - splitPointer[i] + currentSplitPointer;

                                // in case it new best lap precalculate rest of the lap
                                if (followed >= 0 && i == followed)
                                {
                                    for (Int32 j = splitPointer[i] + 1; j < arraySize; j++)
                                    {
                                        splits[i][j % arraySize] = splits[i][splitPointer[i]] + k++ * splitcumulator;
                                    }
                                }
                            }

                            // save in case of new lap record
                            if (followed >= 0 && i == followed)
                            {
                                // check new lap
                                if (newlap)
                                {
                                    if ((currentSplitTime - splits[i][0]) < bestlap[bestlap.Length - 1] || bestlap[bestlap.Length - 1] == 0)
                                    {
                                        validbestlap = true;
                                        // save lap and substract session time offset
                                        for (Int32 j = 0; j < bestlap.Length - 1; j++)
                                        {
                                            bestlap[j] = splits[i][j + 1] - splits[i][0];
                                            if (splits[i][j + 1] == 0.0)
                                                validbestlap = false;
                                        }

                                        bestlap[bestlap.Length - 1] = currentSplitTime - splits[i][0];
                                    }
                                }

                                lapstarttime = currentlap[currentSplitPointer];
                                currentlap[currentSplitPointer] = currentSplitTime;
                            }

                            // fill hopped sectors if necessary
                            if (splithop > 1)
                            {
                                k = 1;
                                for (Int32 j = splitPointer[i] + 1; j % arraySize != currentSplitPointer; j++)
                                {
                                    splits[i][j % arraySize] = splits[i][splitPointer[i]] + (k++ * splitcumulator);
                                }
                            }

                            // save
                            splits[i][currentSplitPointer] = currentSplitTime;
                            splitPointer[i] = currentSplitPointer;
                        }
                    }
                }
                prevTimestamp = timestamp;
            }
        }

        public TimeSpan GetBestLapDelta(Single trackPosition)
        {
            return GetBestLapDelta((Double)trackPosition);
        }

        public TimeSpan GetBestLapDelta(Double trackPosition)
        {
            if (validbestlap)
            {
                Int32 currentSplitPointer = (Int32)Math.Floor((Math.Abs(trackPosition) % 1) / splitLength);
                Double delta;

                if (currentSplitPointer == 0)
                    delta = (splits[followed][0] - lapstarttime) - bestlap[bestlap.Length - 1];
                else if (currentSplitPointer == (bestlap.Length - 1))
                    delta = (splits[followed][currentSplitPointer] - lapstarttime) - bestlap[bestlap.Length - 1];
                else
                    delta = (splits[followed][currentSplitPointer] - splits[followed][bestlap.Length - 1]) - bestlap[currentSplitPointer - 1];

                return new TimeSpan(0, 0, 0, (Int32)Math.Floor(delta), (Int32)Math.Abs((delta % 1) * 1000));
            }
            else
            {
                return new TimeSpan();
            }
        }

        public TimeSpan GetDelta(Int32 caridx1, Int32 caridx2)
        {
            // validate
            if (caridx1 < maxcars && caridx2 < maxcars && caridx1 >= 0 && caridx2 >= 0)
            {
                // comparing latest finished split
                Int32 comparedSplit = splitPointer[caridx1];

                // catch negative index and loop it to last index
                if (comparedSplit < 0)
                    comparedSplit = splits[caridx1].Length - 1;

                Double delta = splits[caridx1][comparedSplit] - splits[caridx2][comparedSplit];

                //Console.WriteLine(prevTimestamp + " " + splits[caridx1][comparedSplit] + " " + splits[caridx2][comparedSplit]);

                if (splits[caridx1][comparedSplit] == 0 || splits[caridx2][comparedSplit] == 0)
                    return new TimeSpan();
                //else if (delta < 0)
                //    return new TimeSpan();
                else
                    return new TimeSpan(0, 0, 0, (Int32)Math.Floor(delta), (Int32)Math.Abs((delta % 1) * 1000));
            }
            else
            {
                return new TimeSpan();
            }
        }

        public static string DeltaToString(TimeSpan delta)
        {
            var seconds = delta.TotalSeconds;
            var laptime = new Laptime((float)seconds);
            return laptime.DisplayShort;
        }
    }
    
}
