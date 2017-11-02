using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    public class PearlsOfWisdom
    {
        public static int pearlsFrequency = UserSettings.GetUserSettings().getInt("frequency_of_pearls_of_wisdom");
        public static String folderMustDoBetter = "pearls_of_wisdom/must_do_better";
        public static String folderKeepItUp = "pearls_of_wisdom/keep_it_up";
        public static String folderNeutral = "pearls_of_wisdom/neutral";

        public enum PearlType
        {
            GOOD, BAD, NEUTRAL, NONE
        }

        public enum PearlMessagePosition
        {
            BEFORE, AFTER, NONE
        }

        public PearlMessagePosition getMessagePosition(double messageProbability)
        {
            if (messageProbability * pearlsFrequency > Utilities.random.NextDouble() * 10)
            {
                if (Utilities.random.NextDouble() < 0.33)
                {
                    return PearlMessagePosition.BEFORE;
                }
                else
                {
                    return PearlMessagePosition.AFTER;
                }
            }
            return PearlMessagePosition.NONE;
        }

        public static String getMessageFolder(PearlType pearlType)
        {
            switch (pearlType)
            {
                case PearlType.GOOD:
                    return folderKeepItUp;
                case PearlType.BAD:
                    return folderMustDoBetter;
                case PearlType.NEUTRAL:
                    return folderNeutral;
                default:
                    Console.WriteLine("Error getting pearl type for type " + pearlType);
                    return "";
            }
        }
    }
}
