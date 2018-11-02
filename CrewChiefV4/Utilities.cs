using CrewChiefV4.GameState;
using MathNet.Numerics;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading;

namespace CrewChiefV4
{
    public static class Utilities
    {
        public static Random random = new Random();

        public static bool IsGameRunning(String processName, String[] alternateProcessNames)
        {
            if (Process.GetProcessesByName(processName).Length > 0)
            {
                return true;
            }
            else if (alternateProcessNames != null && alternateProcessNames.Length > 0)
            {
                foreach (String alternateProcessName in alternateProcessNames)
                {
                    if (Process.GetProcessesByName(alternateProcessName).Length > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        public static void runGame(String launchExe, String launchParams)
        {
            try
            {
                Console.WriteLine("Attempting to run game using " + launchExe + " " + launchParams);
                if (launchExe.Contains(" "))
                {
                    if (!launchExe.StartsWith("\""))
                    {
                        launchExe = "\"" + launchExe;
                    }
                    if (!launchExe.EndsWith("\""))
                    {
                        launchExe = launchExe + "\"";
                    }
                }
                using (Process process = new Process())
                {
                    ProcessStartInfo startInfo = new ProcessStartInfo(launchExe);
                    startInfo.Arguments = launchParams;
                    process.StartInfo = startInfo;
                    process.Start();
                }
            }
            catch (InvalidOperationException e)
            {
                Console.WriteLine("InvalidOperationException starting game: " + e.Message);
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception starting game: " + e.Message);
            }
        }

        /*
         * For tyre life estimates we want to know how long the tyres will last, so we're asking for a time prediction
         * given a wear amount (100% wear). So y_data is the y-axis which may be time points (session running time) or
         * number of sectors since session start incrementing +1 for each sector. When we change tyres we clear these
         * data sets but the y-axis time / sector counts will start at however long into the session (time or total 
         * sectors) we are.
         * x_data is the tyre wear at that y point (a percentage).
         * the x_point is the point you want to predict the life - wear amount. So we pass 100% in here to give us
         * a time / sector count estimate.
         * order is the polynomial fit order - 1 for linear, 2 for quadratic etc. > 3 does not give a suitable
         * curve and will produce nonsense. Use 2 for tyre wear.
         */
        public static double getYEstimate(double[] x_data, double[] y_data, double x_point, int order)
        {
            // get the polynomial from the Numerics library:
            double[] curveParams = Fit.Polynomial(x_data, y_data, order);

            // solve for x_point:
            double y_point = 0;
            for (int power = 0; power < curveParams.Length; power++)
            {
                if (power == 0)
                {
                    y_point = y_point + curveParams[power];
                }
                else if (power == 1)
                {
                    y_point = y_point + curveParams[power] * x_point;
                }
                else
                {
                    y_point = y_point + curveParams[power] * Math.Pow(x_point, power);
                }
            }
            return y_point;
        }

        public static void TraceEventClass(GameStateData gsd)
        {
            if (gsd == null || gsd.carClass == null)
                return;

            var eventCarClasses = new Dictionary<string, CarData.CarClassEnum>();
            eventCarClasses.Add(gsd.carClass.getClassIdentifier(), gsd.carClass.carClassEnum);

            if (gsd.OpponentData != null)
            {
                foreach (var opponent in gsd.OpponentData)
                {
                    if (opponent.Value.CarClass != null
                        && !eventCarClasses.ContainsKey(opponent.Value.CarClass.getClassIdentifier()))
                    {
                        eventCarClasses.Add(opponent.Value.CarClass.getClassIdentifier(), opponent.Value.CarClass.carClassEnum);
                    }
                }
            }

            if (eventCarClasses.Count == 1)
                Console.WriteLine("Single-Class event:\"" + eventCarClasses.Keys.First() + "\" "
                    + Utilities.GetCarClassMappingHint(eventCarClasses.Values.First()));
            else
            {
                Console.WriteLine("Multi-Class event:");
                foreach (var carClass in eventCarClasses)
                {
                    Console.WriteLine("\t\"" + carClass.Key + "\" "
                        + Utilities.GetCarClassMappingHint(carClass.Value));
                }
            }
        }

        private static string GetCarClassMappingHint(CarData.CarClassEnum cce)
        {
            if (cce == CarData.CarClassEnum.UNKNOWN_RACE)
                return "(unmapped)";
            else if (cce == CarData.CarClassEnum.USER_CREATED)
                return "(user defined)";

            return "(built-in)";
        }

        public static string ResolveDataFile(string dataFilesPath, string fileNameToResolve)
        {
            // Search in dataFiles:
            var resolvedFilePaths = Directory.GetFiles(dataFilesPath, fileNameToResolve, SearchOption.AllDirectories);
            if (resolvedFilePaths.Length > 0)
                return resolvedFilePaths[0];

            // Search documents debugLogs:
            resolvedFilePaths = Directory.GetFiles(System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), @"CrewChiefV4\debugLogs"), fileNameToResolve, SearchOption.AllDirectories);

            if (resolvedFilePaths.Length > 0)
                return resolvedFilePaths[0];

            Console.WriteLine("Failed to resolve trace file full path: " + fileNameToResolve);
            return null;
        }


        public static Tuple<int, int> WholeAndFractionalPart(float realNumber, int fractions = 1)
        {
            // get the whole and fractional part (yeah, I know this is shit)
            var str = realNumber.ToString();
            int pointPosition = str.IndexOf('.');
            int wholePart = 0;
            int fractionalPart = 0;
            if (pointPosition > 0)
            {
                wholePart = int.Parse(str.Substring(0, pointPosition));
                fractionalPart = int.Parse(str.Substring(pointPosition + 1, fractions).ToString());
            }
            else
            {
                wholePart = (int)realNumber;
            }

            return new Tuple<int, int>(wholePart, fractionalPart);
        }

        internal static void ReportException(Exception e, string msg, bool needReport)
        {
            Console.WriteLine(
                Environment.NewLine + "==================================================================" + Environment.NewLine
                + (needReport ? ("PLEASE REPORT THIS ERROR TO CC DEV TEAM." + Environment.NewLine) : "")
                + "Error message: " + msg + Environment.NewLine
                + e.ToString() + Environment.NewLine
                + e.Message + Environment.NewLine
                + e.StackTrace + Environment.NewLine
            );

            if (e.InnerException != null)
            {
                Console.WriteLine(
                    "Inner exception: " + e.InnerException.ToString() + Environment.NewLine
                    + e.InnerException.Message + Environment.NewLine
                    + e.InnerException.StackTrace + Environment.NewLine
                );
            }

            Console.WriteLine(
                "==================================================================" + Environment.NewLine
            );
        }

        internal static bool InterruptedSleep(int totalWaitMillis, int waitWindowMillis, Func<bool> keepWaitingPredicate)
        {
            Debug.Assert(totalWaitMillis > 0 && waitWindowMillis > 0);
            var waitSoFar = 0;
            while (waitSoFar < totalWaitMillis)
            {
                if (!keepWaitingPredicate())
                    return false;

                Thread.Sleep(waitWindowMillis);
                waitSoFar += waitWindowMillis;
            }

            return true;
        }
    }
}