using System;
using System.Collections.Generic;

namespace CrewChiefV4.NumberProcessing
{
    /**
     * This is the English number reader implementation. To create another implementation you must create a file called
     * NumberReaderImpl.cs, using this as a template. The class name must be NumberReaderImpl and it must override NumberReader
     * like this one does (the public class NumberReaderImpl : NumberReader line).
     * 
     * The number folders used in your class can be anything you want - these are the folders in the sound pack where you 
     * place your number related sound files.
     * 
     * This class must provide implementations of the GetHoursSound, GetMinutesSound, GetSecondsSound, GetTenthsSound and GetIntegerSounds.
     * These implementations must return a List<String> which contains the sound folders you want for the given number. This 
     * can be an empty list if no sounds are to be read for that number (e.g. zero hours).
     * 
     */
    public class NumberReaderEn : NumberReader
    {
        private static Boolean prefix_hundred_and_thousand_with_one = true;
        private static Boolean say_and_between_hundred_and_units = true;
        private static Boolean say_and_between_thousand_and_units = true;
        private static Boolean global_allow_short_hundreds = true;   // allow "one oh four", instead of "one hundred and four"
        private static Boolean always_use_thousands = false;   // don't allow "thirteen hundred" etc

        // this folder contains lots of subfolders, one for each number from 0 to 99, so we can add a folder to the 
        // list to play called "numbers/[number]" - i.e. numbers/45 or numbers/1. This is used a lot in the implementations below.
        private static String folderNumbersStub = "numbers/";

        private static String folderMinutes = "numbers/minutes";
        private static String folderThousand = "numbers/thousand";
        private static String folderThousandAnd = "numbers/thousand_and";
        private static String folderHundred = "numbers/hundred";
        private static String folderHundredAnd = "numbers/hundred_and";
        private static String folderZeroZero = "numbers/zerozero";
        private static String folderTenths = "numbers/tenths";
        private static String folderTenth = "numbers/tenth";
        private static String folderSeconds = "numbers/seconds";
        private static String folderSecond = "numbers/second";
        private static String folderHours = "numbers/hours";
        private static String folderHour = "numbers/hour";
        private static String folderMinus = "numbers/minus";

        static NumberReaderEn()
        {
            try
            {
                prefix_hundred_and_thousand_with_one = Boolean.Parse(Configuration.getSoundConfigOption("prefix_hundred_and_thousand_with_one"));
                say_and_between_hundred_and_units = Boolean.Parse(Configuration.getSoundConfigOption("say_and_between_hundred_and_units"));
                say_and_between_thousand_and_units = Boolean.Parse(Configuration.getSoundConfigOption("say_and_between_thousand_and_units"));
                global_allow_short_hundreds = Boolean.Parse(Configuration.getSoundConfigOption("allow_short_hundreds"));   // allow "one oh four", instead of "one hundred and four"
                always_use_thousands = Boolean.Parse(Configuration.getSoundConfigOption("always_use_thousands"));   // don't allow "thirteen hundred" etc
            }
            catch (Exception)
            {
                Console.WriteLine("Unable to load number reader config");
            }
        }

        protected override String getLocale()
        {
            return "en";
        }

        /**
         * Get an English sound for a whole number of hours.
         */
        protected override List<String> GetHoursSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            if (hours > 0)
            {
                messages.Add(folderNumbersStub + hours);
                if (hours == 1)
                {
                    messages.Add(folderHour);
                }
                else
                {
                    messages.Add(folderHours);
                }
            }
            /*if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            }*/
            return messages;
        }

        /**
         * Get an English sound for a whole number of minutes.
         */
        protected override List<String> GetMinutesSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            if (minutes > 0)
            {
                messages.Add(folderNumbersStub + minutes);
                // only say the "minutes" sound if there are no seconds or tenths, or if we've read some hours
                if ((seconds == 0 && tenths == 0) || hours > 0)
                {
                    if (minutes == 1)
                    {
                        messages.Add(folderMinute);
                    }
                    else
                    {
                        messages.Add(folderMinutes);
                    }
                }
            }
            /*if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            } */
            return messages;
        }

        /**
         * Get an English sound for a whole number of seconds.
         */
        protected override List<String> GetSecondsSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            // special case here - if we're reading a time which has hours, the seconds aren't significant so ignore them
            if (hours == 0)
            {
                // if we've read some minutes, the zeros in the seconds must be read.
                // if we're going to read some tenths as well and the number of seconds is 0, say "zero-zero".
                if (minutes > 0 && seconds == 0 && tenths > 0)
                {
                    messages.Add(folderZeroZero);
                }
                else if (seconds > 0)
                {
                    // again, if we've read some minutes, the zeros in the seconds must be read.
                    // There are some specific sounds for this - 01 to 09 which combine the "oh" sound with the number
                    if (minutes > 0 && seconds < 10)
                    {
                        messages.Add(folderNumbersStub + "0" + seconds);
                    }
                    else
                    {
                        messages.Add(folderNumbersStub + seconds);
                    }
                    // we only add "seconds" here if we've not read "minutes", and there are no tenths
                    // Note that we don't have to check the hours here because we don't read seconds if there are hours
                    if (minutes == 0 && tenths == 0)
                    {
                        if (seconds == 1)
                        {
                            messages.Add(folderSecond);
                        }
                        else
                        {
                            messages.Add(folderSeconds);
                        }
                    }
                }
            }
            /*if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            } */
            return messages;
        }

        /**
         * Get an English sound for a whole number of tenths of a second.
         */
        protected override List<String> GetTenthsSounds(int hours, int minutes, int seconds, int tenths, Boolean useMoreInflection, Precision precision)
        {
            // hanging inflection isn't used for English tenths sounds - it's not needed
            List<String> messages = new List<String>();
            // special case here - if we're reading a time which has hours, the tenths aren't significant so ignore them. 
            // Still read the tenths if we have > 0 minutes, because this is common for laptimes
            if (hours == 0 && tenths < 10)
            {
                // if there are no minutes or seconds, just say "1 tenth" or "2 tenths" etc
                if (minutes == 0 && seconds == 0)
                {
                    messages.Add(folderNumbersStub + tenths);
                    if (tenths == 1)
                    {
                        messages.Add(folderTenth);
                    }
                    else
                    {
                        messages.Add(folderTenths);
                    }
                }
                else
                {
                    // there are some more compact number sounds for tenths which include point and seconds - e.g. "point 3 seconds".
                    // We can use them or not, it makes sense either way, so we can mix it up a little here and sometimes include 
                    // the "seconds", sometimes not
                    if (tenths > 0)
                    {
                        if (Utilities.random.NextDouble() > 0.5)
                        {
                            messages.Add(folderPoint + tenths + "seconds");
                        }
                        else
                        {
                            messages.Add(folderPoint);
                            messages.Add(folderNumbersStub + tenths);
                        }
                    }
                }
            }
            /*if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            } */
            return messages;
        }

        /**
         * 
         */
        protected override String GetSecondsWithTenths(int seconds, int tenths)
        {
            // sometimes include "seconds" if it's less than 10
            if (seconds > 0 && seconds < 10 && Utilities.random.NextDouble() <= 0.5)
            {
                return folderNumbersStub + seconds + "point" + tenths + "seconds";
            }
            else
            {
                return folderNumbersStub + seconds + "point" + tenths;
            }
        }
        
        /**
         * 
         */
        protected override List<String> GetSeconds(int seconds)
        {
            List<String> messages = new List<String>();
            messages.Add(folderNumbersStub + seconds);
            messages.Add(seconds > 1 ? folderSeconds : folderSecond);
            return messages;
        }

        /**
         * 
         */
        protected override List<String> GetSecondsWithHundredths(int seconds, int hundredths)
        {
            String leadingZero = hundredths < 10 ? "0" : "";
            List<String> messages = new List<String>();
            messages.Add(folderNumbersStub + seconds);
            messages.Add(folderPoint + leadingZero + hundredths);
            return messages;
        }

        /**
         * fraction is String so we can pass "01" etc - we don't know if it's tenths or hundredths so it may need zero padding.
         */
        protected override List<String> GetMinutesAndSecondsWithFraction(int minutes, int seconds, String fraction, Boolean messageHasContentAfterTime)
        {
            List<String> messages = new List<String>();
			// assume minutes is always 1 or 2
			if (minutes < 1 || minutes > 2) 
            {
				Console.WriteLine("Invalid minutes arg " + minutes + " to GetMinutesAndSecondsWithTenths");
			} 
            else 
            {
				if (seconds < 1 || seconds > 59)
                {
					Console.WriteLine("Invalid seconds arg " + minutes + " to GetMinutesAndSecondsWithTenths");
				} else 
				{
                    String paddedSeconds = seconds < 10 ? "_0" + seconds : "_" + seconds;
					messages.Add(folderNumbersStub + minutes + paddedSeconds);					
					messages.Add(folderPoint + fraction);
				}
			}
            return messages;
        }

        /**
         * Get an English sound for an Integer from 0 to 99999.
         */
        protected override List<String> GetIntegerSounds(char[] rawDigits, Boolean allowShortHundredsForThisNumber, Boolean messageHasContentAfterNumber)
        {
            List<String> messages = new List<String>();
            char[] digits;
            if (rawDigits.Length >= 2 && rawDigits[0] == '-')
            {
                digits = new char[rawDigits.Length - 1];
                Array.Copy(rawDigits, 1, digits, 0, digits.Length);
                messages.Add(folderMinus);
            }
            else
            {
                digits = rawDigits;
            }            
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
                if (digits.Length == 4 && digits[0] == '1' && digits[1] != '0' && !always_use_thousands)
                {
                    // the number is 1100 -> 1999. In English we say "eleven hundred", not "one thousand one hundred"
                    // So the number of hundreds is the first and second digit

                    // don't allow "thirteen hundred" type messages if always_use_thousands is true
                    hundreds = digits[0].ToString() + digits[1].ToString();
                }
                else
                {
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
                }
                if (thousands != null)
                {
                    if (thousands != "1" || prefix_hundred_and_thousand_with_one)
                    {
                         messages.Add(folderNumbersStub + thousands);
                    }
                    if (hundreds == null && tensAndUnits != null && say_and_between_thousand_and_units) 
                    {
                        // if we're going to also read out a number of hundreds or tensAndUnits, we say "thousand and..."
                         messages.Add(folderThousandAnd);
                    } else 
                    {
                         messages.Add(folderThousand);
                    }
                }
                if (hundreds != null)
                {
                    Boolean saidNumberOfHundreds = false;
                    if (hundreds != "1" || prefix_hundred_and_thousand_with_one)
                    {
                        saidNumberOfHundreds = true;
                        messages.Add(folderNumbersStub + hundreds);
                    }
                    // don't always use "hundred and" - it's valid to say "one hundred and twenty" or "one twenty". This implementation
                    // will choose semi-randomly whether to use the long or short form.
                    Boolean addedHundreds = false;
                    if (tensAndUnits != null)
                    {
                        // if there's a thousand, or we're saying something like "13 hundred", then always use the long version
                        if (!global_allow_short_hundreds || hundreds.Length == 2 || thousands != null || !allowShortHundredsForThisNumber || Utilities.random.NextDouble() > 0.6)
                        {
                            if (say_and_between_hundred_and_units)
                            {
                                messages.Add(folderHundredAnd);
                            }
                            else
                            {
                                messages.Add(folderHundred);
                            }
                            addedHundreds = true;
                        }
                    }
                    else
                    {
                        messages.Add(folderHundred);
                        addedHundreds = true;
                    }
                    if (!addedHundreds)
                    {
                        if (!saidNumberOfHundreds)
                        {
                            messages.Add(folderNumbersStub + hundreds);
                        }
                        if (tensAndUnits != null && tensAndUnits.Length == 1)
                        {
                            // need to modify the tensAndUnits here - we've skipped "hundreds" even though the number is > 99.
                            // This is fine if the tensAndUnits > 9 (it'll be read as "One twenty five"), but if the tensAndUnits < 10
                            // this will be read as "One two" instead of "One oh two".
                            tensAndUnits = "0" + tensAndUnits;
                        }
                    }
                }
                if (tensAndUnits != null)
                {
                    messages.Add(folderNumbersStub + tensAndUnits);
                }
            }
            /*if (messages.Count > 0)
            {
                Console.WriteLine(String.Join(", ", messages));
            } */
            return messages;
        }
    }
}
