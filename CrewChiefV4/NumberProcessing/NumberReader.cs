using CrewChiefV4.Audio;
using CrewChiefV4.Events;
using CrewChiefV4.NumberProcessing;
using System;
using System.Collections.Generic;
namespace CrewChiefV4
{
    public abstract class NumberReader
    {
        /**
         * Language specific implementation to speak an integer, using whatever rules and words this language requires.
         * Note this char array may contain only '0'. This will typically include words for "seconds", "tenths", "hundreds", etc
         * as well as the number sounds.
         */
        protected abstract List<String> GetIntegerSounds(char[] digits);

        /**
         * Language specific implementation to speak a number of hours, using whatever rules and words this language requires.
         * This might need to take the numbers of minutes, seconds and tenths into consideration.
         */
        protected abstract List<String> GetHoursSounds(int hours, int minutes, int seconds, int tenths);

        /**
         * Language specific implementation to speak a number of minutes, using whatever rules and words this language requires.
         * This might need to take the numbers of hours, seconds and tenths into consideration.
         */
        protected abstract List<String> GetMinutesSounds(int hours, int minutes, int seconds, int tenths);

        /**
         * Language specific implementation to speak a number of seconds, using whatever rules and words this language requires.
         * This might need to take the numbers of hours, minutes and tenths into consideration.
         */
        protected abstract List<String> GetSecondsSounds(int hours, int minutes, int seconds, int tenths);

        /**
         * Language specific implementation to speak a number of tenths, using whatever rules and words this language requires.
         * This might need to take the numbers of hours, minutes and seconds into consideration.
         * The useMoreInflection tells the implementation to select a different tenths sound with a rising (or hanging) inflection. This
         * is needed for Italian numbers.
         */
        protected abstract List<String> GetTenthsSounds(int hours, int minutes, int seconds, int tenths, Boolean useMoreInflection);

        /**
         * Separate recordings for when we just want a number of seconds with tenths. This is only used when we have no minutes part,
         * or we have a minutes part *and* the number of seconds is 10 or more (because these sounds have no "zero.." or "oh.." part.
         * This is (currently) only applicable to English numbers.
         *
         */
        protected abstract String GetSecondsWithTenths(int seconds, int tenths);

        /**
         * Separate recordings for when we just want a number of seconds with hundreths. This is only used when we have no minutes part,
         * or we have a minutes part *and* the number of seconds is 10 or more (because these sounds have no "zero.." or "oh.." part.
         * This is (currently) only applicable to English numbers.
         *
         */
        protected abstract List<String> GetSecondsWithHundreths(int seconds, int hundreths);

        /**
         * Separate recordings for when we just want a number of seconds with tenths with 1 or 2 minutes. 
         * This is (currently) only applicable to English numbers.
         *
         */
        protected abstract List<String> GetMinutesAndSecondsWithFraction(int minutes, int seconds, int fraction);

        protected abstract String getLocale();

        protected Random random = new Random();

        /**
         * Convert a timeSpan to some sound files, using the current language's implementation.
         */
        public List<String> ConvertTimeToSounds(TimeSpanWrapper timeSpanWrapper, Boolean useMoreInflection)
        {
            // Console.WriteLine(new DateTime(timeSpan.Ticks).ToString("HH:mm:ss.F"));
            List<String> messageFolders = new List<String>();
            if (timeSpanWrapper != null)
            {
                // if the milliseconds in this timeSpan is > 949, when we turn this into tenths it'll get rounded up to 
                // ten tenths, which we can't have. So move the timespan on so this rounding doesn't happen
                if (timeSpanWrapper.getPrecision() == Precision.TENTHS && timeSpanWrapper.timeSpan.Milliseconds > 949)
                {
                    timeSpanWrapper.timeSpan = timeSpanWrapper.timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpanWrapper.timeSpan.Milliseconds));
                }
                else if (timeSpanWrapper.getPrecision() == Precision.HUNDREDTHS && timeSpanWrapper.timeSpan.Milliseconds > 995)
                {
                    timeSpanWrapper.timeSpan = timeSpanWrapper.timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpanWrapper.timeSpan.Milliseconds));
                }
                int tenths = (int)Math.Round((float)timeSpanWrapper.timeSpan.Milliseconds / 100f);
                int hundreths = (int)Math.Round((float)timeSpanWrapper.timeSpan.Milliseconds / 10f);

                // now call the language-specific implementations
                Boolean useNewENMinutes = AudioPlayer.soundPackVersion > 106 && getLocale() == "en" && timeSpanWrapper.timeSpan.Hours == 0 &&
                    timeSpanWrapper.timeSpan.Minutes > 0 && timeSpanWrapper.timeSpan.Minutes < 3 && timeSpanWrapper.timeSpan.Seconds > 0 && timeSpanWrapper.timeSpan.Seconds < 60;

                Boolean useNewENSeconds = AudioPlayer.soundPackVersion > 106 && getLocale() == "en" && timeSpanWrapper.timeSpan.Hours == 0 &&
                    timeSpanWrapper.timeSpan.Minutes == 0 && (timeSpanWrapper.timeSpan.Seconds > 0 || tenths > 0 ||
                    (timeSpanWrapper.getPrecision() == Precision.HUNDREDTHS && hundreths > 0)) && timeSpanWrapper.timeSpan.Seconds < 60;

                if (useNewENSeconds)
                {
                    messageFolders.Add(AbstractEvent.Pause(50));
                    if (timeSpanWrapper.getPrecision() == Precision.HUNDREDTHS)
                    {
                        messageFolders.AddRange(GetSecondsWithHundreths(timeSpanWrapper.timeSpan.Seconds, hundreths));
                    } 
                    else 
                    {
                        messageFolders.Add(GetSecondsWithTenths(timeSpanWrapper.timeSpan.Seconds, tenths));
                    }
                    // TODO: seconds and lower
                }
                else if (useNewENMinutes)
                {
                    messageFolders.Add(AbstractEvent.Pause(50));
                    messageFolders.AddRange(GetMinutesAndSecondsWithFraction(timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds,
                        timeSpanWrapper.getPrecision() == Precision.HUNDREDTHS ? hundreths : tenths));
                }
                else
                {
                    messageFolders.AddRange(GetHoursSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths));
                    messageFolders.AddRange(GetMinutesSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths));
                    messageFolders.AddRange(GetSecondsSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths));
                    messageFolders.AddRange(GetTenthsSounds(timeSpanWrapper.timeSpan.Hours, timeSpanWrapper.timeSpan.Minutes, timeSpanWrapper.timeSpan.Seconds, tenths, useMoreInflection));
                }

                /*if (messageFolders.Count > 0)
                {
                    Console.WriteLine(String.Join(", ", messageFolders));
                }*/
            }
            return messageFolders;
        }

        /**
         * Convert an integer to some sound files, using the current language's implementation.
         */
        public List<String> GetIntegerSounds(int integer)
        {
            if (integer >= 0 && integer <= 99999)
            {
                return GetIntegerSounds(integer.ToString().ToCharArray());
            }
            else
            {
                Console.WriteLine("Cannot convert integer " + integer + " valid range is 0 - 99999");
                return new List<String>();
            }
        }
    }
}
