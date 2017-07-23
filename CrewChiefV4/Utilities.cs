using System;
using System.Diagnostics;

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
    }
}