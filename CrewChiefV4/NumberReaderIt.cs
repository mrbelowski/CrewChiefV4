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

        // these are used to build up the hundreds folder names, combining with folderNumbersStub - i.e.
        // "numbers_it/1_hundreds","numbers_it/2_hundreds", etc
        private static String folderNumbersItPrefix = "numbers_it/";
        private static String folderHundredSuffix = "_hundreds";

        private static String folderThousand = "numbers_it/thousand";
        private static String folderThousands = "numbers_it/thousands";


        // folders for reading out times
        private static String folderTenths = "numbers_it/tenths";
        private static String folderATenth = "numbers_it/a_tenth";

        // this is used for reading out the number of tenths - "and 1, "and 2", etc.
        // The name is built up with the number of tenths, so we need folders called
        // ""numbers_it/and_1", "numbers_it/and_2", etc
        private static String folderAndTenthsPrefix = "numbers_it/and_";

        private static String folderASecond = "numbers_it/a_second";
        private static String folderSeconds = "numbers_it/seconds";

        private static String folderMinutesAnd = "numbers_it/minutes_and";
        private static String folderAMinuteAnd = "numbers_it/a_minute_and"; 
        private static String folderMinutes = "numbers_it/minutes";
        private static String folderAMinute = "numbers_it/a_minute";

        private static String folderZeroZero = "numbers_it/zerozero";
        private static String folderZero = "numbers_it/zero";

        private static String folderAnHourAnd = "numbers_it/an_hour_and";
        private static String folderAnHour = "numbers_it/an_hour";
        private static String folderHoursAnd = "numbers_it/hours_and";
        private static String folderHours = "numbers_it/hours";

        private Random random = new Random();

        /**
         * Get an Italian sound for a whole number of hours.
         */
        protected override List<String> GetHoursSounds(int hours, int minutes, int seconds, int tenths)
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
            if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of minutes.
         */
        protected override List<String> GetMinutesSounds(int hours, int minutes, int seconds, int tenths)
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
                if (tenths > 0)
                {
                    messages.Add(folderNumbersStub + minutes);
                }
                else
                {
                    // no tenths, so we'll have the "and" here if there are some seconds
                    if (minutes == 1)
                    {
                        if (seconds > 0)
                        {
                            messages.Add(folderAMinuteAnd);
                        }
                        else
                        {
                            messages.Add(folderAMinute);
                        }
                    }
                    else
                    {
                        messages.Add(folderNumbersStub + minutes);
                        if (seconds > 0)
                        {
                            messages.Add(folderMinutesAnd);
                        }
                        else
                        {
                            messages.Add(folderMinutes);
                        }
                    }
                }
            }
            if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of seconds.
         */
        protected override List<String> GetSecondsSounds(int hours, int minutes, int seconds, int tenths)
        {
            List<String> messages = new List<String>();
            // special case here - if we're reading a time which has hours, the seconds aren't significant so ignore them
            if (hours == 0)
            {
                if (minutes == 0 || tenths == 0) 
                {
                    // if there are no minutes we'll always read the "seconds"
                    if (seconds == 1)
                    {
                        messages.Add(folderASecond);
                    }
                    else if (seconds > 0)
                    {
                        messages.Add(folderNumbersStub + seconds);
                        messages.Add(folderSeconds);
                    }
                }
                else if (tenths > 0)
                {
                    // if we have some minutes the seconds won't include the "seconds" sound
                    if (seconds == 0)
                    {
                        messages.Add(folderZeroZero);
                    }
                    else
                    {
                        if (seconds < 10)
                        {
                            messages.Add(folderZero);
                        }
                        messages.Add(folderNumbersStub + seconds);
                    }
                }
                else if (seconds > 0)
                {
                    messages.Add(folderNumbersStub + seconds);
                }
            }
            if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of tenths of a second.
         */
        protected override List<String> GetTenthsSounds(int hours, int minutes, int seconds, int tenths)
        {
            List<String> messages = new List<String>();
            // special case here - if we're reading a time which has hours, the tenths aren't significant so ignore them. 
            // Still read the tenths if we have > 0 minutes, because this is common for laptimes
            if (hours == 0 && tenths < 10)
            {
                // if there are no minutes or seconds, just say "a tenth" or "2 tenths" etc
                if (minutes == 0 && seconds == 0)
                {                    
                    if (tenths == 1)
                    {
                        messages.Add(folderATenth);
                    }
                    else
                    {
                        messages.Add(folderNumbersStub + tenths);
                        messages.Add(folderTenths);
                    }
                }
                else
                {
                    if (tenths > 0)
                    {
                        // we need to add the "and... " here
                        messages.Add(folderAndTenthsPrefix + tenths);
                    }
                }
            }
            if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            }
            return messages;
        }

        /**
         * Get an Italian sound for an Integer from 0 to 99999.
         */
        protected override List<String> GetIntegerSounds(char[] digits)
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
                    messages.Add(folderNumbersItPrefix + hundreds + folderHundredSuffix);
                }
                if (tensAndUnits != null)
                {
                    messages.Add(folderNumbersStub + tensAndUnits);
                }
            }
            if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            }
            return messages;
        }
    }
}
