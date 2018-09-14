using CrewChiefV4.Audio;
using CrewChiefV4.Events;
using CrewChiefV4.NumberProcessing;
using System;
using System.Collections.Generic;
namespace CrewChiefV4
{
    public abstract class NumberReader
    {
        public static String folderPoint = "numbers/point";
        public static String folderMinute = "numbers/minute";
        public static String folderOh = "numbers/oh";

        /**
         * Language specific implementation to speak an integer, using whatever rules and words this language requires.
         * Note this char array may contain only '0'. This will typically include words for "seconds", "tenths", "hundreds", etc
         * as well as the number sounds.
         */
        protected abstract List<String> GetIntegerSounds(char[] digits, Boolean allowShortHundredsForThisNumber, Boolean messageHasContentAfterNumber);

        /**
         * Language specific implementation to speak a number of hours, using whatever rules and words this language requires.
         * This might need to take the numbers of minutes, seconds and tenths into consideration.
         */
        protected abstract List<String> GetHoursSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision);

        /**
         * Language specific implementation to speak a number of minutes, using whatever rules and words this language requires.
         * This might need to take the numbers of hours, seconds and tenths into consideration.
         */
        protected abstract List<String> GetMinutesSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision);

        /**
         * Language specific implementation to speak a number of seconds, using whatever rules and words this language requires.
         * This might need to take the numbers of hours, minutes and tenths into consideration.
         */
        protected abstract List<String> GetSecondsSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision);

        /**
         * Language specific implementation to speak a number of tenths, using whatever rules and words this language requires.
         * This might need to take the numbers of hours, minutes and seconds into consideration.
         * The useMoreInflection tells the implementation to select a different tenths sound with a rising (or hanging) inflection. This
         * is needed for Italian numbers.
         */
        protected abstract List<String> GetTenthsSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision);

        /**
         * Separate recordings for when we just want a number of seconds with tenths. This is only used when we have no minutes part,
         * or we have a minutes part *and* the number of seconds is 10 or more (because these sounds have no "zero.." or "oh.." part.
         * This is (currently) only applicable to English numbers.
         *
         */
        protected abstract String GetSecondsWithTenths(int seconds, int tenths);

        /**
         *
         */
        protected abstract List<String> GetSeconds(int seconds);

        /**
         * Separate recordings for when we just want a number of seconds with hundredths. This is only used when we have no minutes part,
         * or we have a minutes part *and* the number of seconds is 10 or more (because these sounds have no "zero.." or "oh.." part.
         * This is (currently) only applicable to English numbers.
         *
         */
        protected abstract List<String> GetSecondsWithHundredths(int seconds, int hundredths);

        /**
         * Separate recordings for when we just want a number of seconds with tenths with 1 or 2 minutes. 
         * This is (currently) only applicable to English numbers.
         * fraction is String so we can pass "01" etc - we don't know if it's tenths or hundredths so it may need zero padding.
         * 
         */
        protected abstract List<String> GetMinutesAndSecondsWithFraction(int minutes, int seconds, String fraction, Boolean messageHasContentAfterTime);
        
        protected abstract String getLocale();

        /**
         * Convert a timeSpan to some sound files, using the current language's implementation.
         */
        public List<String> ConvertTimeToSounds(TimeSpanWrapper timeSpanWrapper, Boolean useMoreInflection)
        {
            // Console.WriteLine(new DateTime(timeSpan.Ticks).ToString("HH:mm:ss.F"));
            List<String> messageFolders = new List<String>();
            if (timeSpanWrapper != null)
            {
                Precision precision = timeSpanWrapper.getPrecision();
                // Rounding hacks. Because we treat the tenths or hundredths as separate numbers, they may get
                // rounded - .950 will be rounded to 'point 10' and .995 will be rounded to 'point 100', so we 
                // move the time on a bit to ensure this doesn't happen:
                if (precision == Precision.HUNDREDTHS && timeSpanWrapper.timeSpan.Milliseconds > 995)
                {
                    timeSpanWrapper.timeSpan = timeSpanWrapper.timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpanWrapper.timeSpan.Milliseconds));
                }
                else if (timeSpanWrapper.timeSpan.Milliseconds > 949)
                {
                    // move the time on even if we're not asking for TENTHS in our precision argument
                    timeSpanWrapper.timeSpan = timeSpanWrapper.timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpanWrapper.timeSpan.Milliseconds));
                }
                // so now these tenths and hundredths can never be 10 or 100 respectively:
                int tenths = (int)Math.Round((float)timeSpanWrapper.timeSpan.Milliseconds / 100f);
                int hundredths = (int)Math.Round((float)timeSpanWrapper.timeSpan.Milliseconds / 10f);

                // now call the language-specific implementations
                Boolean useNewENMinutes = SoundPackVersionsHelper.currentSoundPackVersion > 106 && getLocale() == "en" && timeSpanWrapper.timeSpan.Hours == 0 &&
                    timeSpanWrapper.timeSpan.Minutes > 0 && timeSpanWrapper.timeSpan.Minutes < 3 && timeSpanWrapper.timeSpan.Seconds > 0 && timeSpanWrapper.timeSpan.Seconds < 60;

                Boolean useNewENSeconds = precision != Precision.MINUTES &&
                    SoundPackVersionsHelper.currentSoundPackVersion > 106 && getLocale() == "en" && timeSpanWrapper.timeSpan.Hours == 0 &&
                    timeSpanWrapper.timeSpan.Minutes == 0 && (timeSpanWrapper.timeSpan.Seconds > 0 || tenths > 0 ||
                    (precision == Precision.HUNDREDTHS && hundredths > 0)) && timeSpanWrapper.timeSpan.Seconds < 60;

                Boolean useItalianShortForm = precision != Precision.MINUTES && precision != Precision.SECONDS && !timeSpanWrapper.precisionIsAutoGaps &&
                    SoundPackVersionsHelper.currentSoundPackVersion > 150 && getLocale() == "it" &&
                    timeSpanWrapper.timeSpan.Hours == 0 && 
                        (timeSpanWrapper.timeSpan.Seconds > 0 && (timeSpanWrapper.timeSpan.Minutes > 0 || tenths > 0 || hundredths > 0));    // more checks on numbers?

                if (useNewENSeconds)
                {
                    messageFolders.Add(AbstractEvent.Pause(50));
                    if (precision == Precision.HUNDREDTHS)
                    {
                        messageFolders.AddRange(GetSecondsWithHundredths(timeSpanWrapper.timeSpan.Seconds, hundredths));
                    } 
                    else if (precision == Precision.TENTHS)
                    {
                        messageFolders.Add(GetSecondsWithTenths(timeSpanWrapper.timeSpan.Seconds, tenths));
                    }
                    else if (precision == Precision.SECONDS)
                    {
                        messageFolders.AddRange(GetSeconds(timeSpanWrapper.timeSpan.Seconds));
                    }
                }
                else if (useNewENMinutes || useItalianShortForm)
                {
                    messageFolders.Add(AbstractEvent.Pause(50));
                    if (precision == Precision.HUNDREDTHS)
                    {
                        String leadingZero = hundredths < 10 ? "0" : "";
                        messageFolders.AddRange(GetMinutesAndSecondsWithFraction(timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, leadingZero + hundredths, useMoreInflection));
                    }
                    else if (precision == Precision.TENTHS)
                    {
                        messageFolders.AddRange(GetMinutesAndSecondsWithFraction(timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths.ToString(), useMoreInflection));
                    }
                    else if (precision == Precision.SECONDS || precision == Precision.MINUTES)
                    {
                        messageFolders.AddRange(GetMinutesSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection, precision));
                        messageFolders.AddRange(GetSecondsSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection, precision));
                    }
                }
                else
                {
                    messageFolders.AddRange(GetHoursSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection, precision));
                    messageFolders.AddRange(GetMinutesSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection, precision));
                    messageFolders.AddRange(GetSecondsSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection, precision));
                    messageFolders.AddRange(GetTenthsSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection, precision));
                }

                if (getLocale() == "it" && messageFolders.Count > 0)
                {
                    Console.WriteLine(String.Join(", ", messageFolders));
                }
            }
            return messageFolders;
        }

        /**
         * Convert an integer to some sound files, using the current language's implementation.
         */
        public List<String> GetIntegerSounds(int integer, Boolean allowShortHundredsForThisNumber, Boolean useMoreInflection)
        {
            if (integer >= -99999 && integer <= 99999)
            {
                return GetIntegerSounds(integer.ToString().ToCharArray(), allowShortHundredsForThisNumber, useMoreInflection);
            }
            else
            {
                Console.WriteLine("Cannot convert integer " + integer + " valid range is 0 - 99999");
                return new List<String>();
            }
        }
    }
}
