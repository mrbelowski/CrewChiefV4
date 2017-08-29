using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CrewChiefV4
{
    public class Utilities
    {
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
    }
}