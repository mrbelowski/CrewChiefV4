using System;
using System.Collections.Generic;

namespace CrewChiefV4.NumberProcessing
{
    /**
     * This is the Italian number reader implementation. 
     *  
     */
    public class NumberReaderIt : NumberReader
    {        
        // this folder contains lots of subfolders, one for each number from 0 to 99, so we can add a folder to the 
        // list to play called "numbers/[number]" - i.e. numbers/45 or numbers/1. This is used a lot in the implementations below.
        private static String folderNumbersStub = "numbers_it/";

        // the number sounds for tenths sometimes need a 'hanging inflection' - add this suffix when they do
        private static String moreInflectionSuffix = "_more";

        // these are used to build up the hundreds folder names, combining with folderNumbersStub - i.e.
        // "numbers_it/1_hundreds","numbers_it/2_hundreds", etc
        private static String folderHundredSuffix = "_hundreds";

        private static String folderThousand = "numbers_it/thousand";
        private static String folderThousands = "numbers_it/thousands";


        // folders for reading out times.

        // This is combined with folderNumbersStub to produce tenths sounds for tenths > 1 - numbers_it/2_tenths -> numbers_it/9_tenths
        private static String folderTenthsSuffix = "_tenths";
        private static String folderATenth = "numbers_it/a_tenth";

        // this is used for reading out the number of tenths - "and 1, "and 2", etc.
        // The name is built up with the number of tenths, so we need folders called
        // "numbers_it/and_0", "numbers_it/and_1", etc
        private static String folderAndPrefix = "numbers_it/and_";

        // this is a separate set of number recordings for numbers which are to be read as seconds, but only where we'll read some tenths afterwards.
        // These have a special inflection specific to numbers read as, e.g. "one *zero six* and 4" or "3 *twenty* and zero"
        private static String folderSecondsNumbersPrefix = "numbers_it/numbers_seconds_";
        private static String folderSeconds = "numbers_it/seconds";
        private static String folderASecond = "numbers_it/a_second";

        private static String folderMinutes = "numbers_it/minutes";
        private static String folderAMinute = "numbers_it/a_minute";

        private static String folderAnHourAnd = "numbers_it/an_hour_and";
        private static String folderAnHour = "numbers_it/an_hour";
        private static String folderHoursAnd = "numbers_it/hours_and";
        private static String folderHours = "numbers_it/hours";

        protected override String getLocale()
        {
            return "it";
        }

        /**
         * Get an Italian sound for a whole number of hours.
         */
        protected override List<String> GetHoursSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime)
        {
            List<String> messages = new List<String>();            
            if (hours == 1)
            {
                if (minutes > 0)
                {
                    messages.Add(folderAnHourAnd);
                }
                else
                {
                    messages.Add(folderAnHour);
                }
            }
            else if (hours > 0)
            {
                if (minutes > 0)
                {
                    messages.Add(folderNumbersStub + hours);
                    messages.Add(folderHoursAnd);
                }
                else
                {
                    messages.Add(folderNumbersStub + hours);
                    messages.Add(folderHours);
                }
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of minutes.
         */
        protected override List<String> GetMinutesSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime)
        {
            List<String> messages = new List<String>();

            if (hours > 0)
            {
                // if we've read some hours, never bother with the seconds or tenths
                if (minutes == 1)
                {
                    messages.Add(folderAMinute);
                }
                else if (minutes > 0)
                {
                    messages.Add(folderNumbersStub + minutes);
                    messages.Add(folderMinutes);
                }
            }
            else if (minutes > 0)
            {
                // if we have only 1 minute and no tenths or seconds, we say "a minute"
                if (minutes == 1 && seconds == 0 && tenths == 0)
                {
                    messages.Add(folderAMinute);
                }
                else if (minutes > 0)
                {
                    messages.Add(folderNumbersStub + minutes);
                    // add "minutes" if there's nothing else to read
                    if (seconds == 0 && tenths == 0)
                    {
                        messages.Add(folderMinutes);
                    }
                }
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of seconds.
         */
        protected override List<String> GetSecondsSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime)
        {
            List<String> messages = new List<String>();
            // special case here - if we're reading a time which has hours, the seconds aren't significant so ignore them
            if (hours == 0)
            {
                if (minutes != 0)
                {
                    if (seconds == 0 && tenths != 0)
                    {
                        // say "zero zero" so we can add the non-zero tenths
                        messages.Add(folderSecondsNumbersPrefix + "00");
                    }
                    else
                    {
                        String secondsFolderSuffix;
                        if (seconds < 10)
                        {
                            secondsFolderSuffix = "0" + seconds;
                        }
                        else
                        {
                            secondsFolderSuffix = seconds.ToString();
                        }
                        messages.Add(folderSecondsNumbersPrefix + secondsFolderSuffix);
                    }
                }
                else
                {
                    // if there are no minutes we'll always read the "seconds"
                    if (seconds == 1)
                    {
                        messages.Add(folderASecond);
                    }
                    else if (seconds > 1)
                    {
                        messages.Add(folderNumbersStub + seconds);
                        messages.Add(folderSeconds);
                    }
                }
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of tenths of a second.
         */
        protected override List<String> GetTenthsSounds(int hours, int minutes, int seconds, int tenths, Boolean useMoreInflection)
        {
            List<String> messages = new List<String>();
            String addonForMoreInflection = useMoreInflection ? moreInflectionSuffix : "";
            // special case here - if we're reading a time which has hours, the tenths aren't significant so ignore them. 
            // Still read the tenths if we have > 0 minutes, because this is common for laptimes
            if (hours == 0 && tenths < 10)
            {
                // if there are no minutes or seconds, just say "a tenth" or "2 tenths" etc
                if (minutes == 0 && seconds == 0)
                {                    
                    if (tenths == 1)
                    {
                        messages.Add(folderATenth + addonForMoreInflection);
                    }
                    else
                    {
                        messages.Add(folderNumbersStub + tenths + folderTenthsSuffix + addonForMoreInflection);
                    }
                }
                else if (seconds > 0 || tenths > 0)
                {
                    // we need to add the "and... " here
                    messages.Add(folderAndPrefix + tenths + addonForMoreInflection);
                }
            }
            return messages;
        }

        /**
         * Not implemented for Italian number reader.
         * */
        protected override String GetSecondsWithTenths(int seconds, int tenths)
        {
            return null;
        }

        /**
         * Not implemented for Italian number reader.
         * */
        protected override List<String> GetMinutesAndSecondsWithFraction(int minutes, int seconds, String fraction)
        {
            return null;
        }

        /**
         * Not implemented for Italian number reader.
         * */
        protected override List<String> GetSecondsWithHundredths(int seconds, int hundredths)
        {
            return null;
        }

        /**
         * Not implemented for Italian number reader.
         * */
        protected override List<String> GetSeconds(int seconds)
        {
            return null;
        }

        /**
         * Get an Italian sound for an Integer from 0 to 99999.
         */
        protected override List<String> GetIntegerSounds(char[] digits, Boolean allowShortHundredsForThisNumber, Boolean messageHasContentAfterNumber)
        {
            List<String> messages = new List<String>();
            // if this is just zero, return a list with just "zero"
            if (digits.Length == 0 || (digits.Length == 1 && digits[0] == '0'))
            {
                messages.Add(folderNumbersStub + 0);
            }
            else
            {
                // work out what to say for the thousands, hundreds, and tens / units
                String tensAndUnits = null;
                String hundreds = null;
                String thousands = null;
                
                if (digits.Length == 1 || (digits[digits.Length - 2] == '0' && digits[digits.Length - 1] != '0'))
                {
                    // if we have just 1 digit, or we have a number that ends with 01, 02, 03, etc, then the 
                    // number of tensAndUnits is the final character
                    tensAndUnits = digits[digits.Length - 1].ToString();
                }
                else if (digits[digits.Length - 2] != '0' || digits[digits.Length - 1] != '0')
                {
                    // if we have just multiple digits, and one or both of the last 2 are non-zero
                    tensAndUnits = digits[digits.Length - 2].ToString() + digits[digits.Length - 1].ToString();
                }
                if (digits.Length >= 3)
                {
                    if (digits[digits.Length - 3] != '0')
                    {
                        // there's a non-zero number of hundreds
                        hundreds = digits[digits.Length - 3].ToString();
                    }
                    if (digits.Length == 4)
                    {
                        // there's a non-zero number of thousands
                        thousands = digits[0].ToString();
                    }
                    else if (digits.Length == 5)
                    {
                        // there's a non-zero number of thousands - 10 or more
                        thousands = digits[0].ToString() + digits[1].ToString();
                    }                    
                }
                if (thousands != null)
                {
                    if (thousands == "1")
                    {
                        messages.Add(folderThousand);
                    }
                    else
                    {
                        messages.Add(folderNumbersStub + thousands);
                        messages.Add(folderThousands);
                    }
                }
                if (hundreds != null)
                {
                    messages.Add(folderNumbersStub + hundreds + folderHundredSuffix);
                }
                if (tensAndUnits != null)
                {
                    messages.Add(folderNumbersStub + tensAndUnits);
                }
            }
            return messages;
        }
    }
}
