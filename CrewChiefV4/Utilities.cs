using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;

namespace CrewChiefV4
{
    public static class Utilities
    {
        public static Random random = new Random();

        // if sound pack version >= 146 use update3, >= 122 use update2, >= 0 use update, else use base
        // To add a new pack, add to the front of the appropriate list
        //
        // Note that the first entry in each of these 3 lists doesn't (yet) exist in the autoupdate data,
        // so the XML element from the parser will be null, which is interpreted as 'no update available'.
        public static SoundPackData[] soundPacks = { new SoundPackData(146, "update3soundpackurl"),
                                                     new SoundPackData(122, "update2soundpackurl"),
                                                     new SoundPackData(0, "updatesoundpackurl"),
                                                     new SoundPackData(-1, "basesoundpackurl")
                                                   };

        public static SoundPackData[] personalisationPacks = { new SoundPackData(129, "update3personalisationsurl"),
                                                               new SoundPackData(121, "update2personalisationsurl"),
                                                               new SoundPackData(0, "updatepersonalisationsurl"),
                                                               new SoundPackData(-1, "basepersonalisationsurl")
                                                             };

        public static SoundPackData[] drivernamesPacks = { new SoundPackData(130, "update2drivernamesurl"),
                                                           new SoundPackData(0, "updatedrivernamesurl"),
                                                           new SoundPackData(-1, "basedrivernamesurl")
                                                         };
        public class SoundPackData
        {
            public int upgradeFromVersion;
            public String elementName;
            public String downloadLocation = null;
            public SoundPackData(int upgradeFromVersion, String elementName)
            {
                this.upgradeFromVersion = upgradeFromVersion;
                this.elementName = elementName;
            }

            // use the first decendant or null if we can't get it
            public void setDownloadLocation(System.Xml.Linq.XElement parent)
            {
                try
                {
                    this.downloadLocation = parent.Descendants(elementName).First().Value;
                }
                catch (Exception)
                {
                }
            }

            // use the first decendant or null if we can't get it
            public void setDownloadLocation(System.Xml.Linq.XDocument parent)
            {
                try
                {
                    this.downloadLocation = parent.Descendants(elementName).First().Value;
                }
                catch (Exception)
                {
                }
            }
        }

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
    }
}