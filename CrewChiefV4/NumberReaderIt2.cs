using CrewChiefV4.Audio;
using System;
using System.Collections.Generic;

namespace CrewChiefV4.NumberProcessing
{
    /**
     * This is the Italian number reader implementation. 
     *  
     */
    public class NumberReaderIt2 : NumberReader
    {
        // this folder contains lots of subfolders, one for each number from 0 to 99, so we can add a folder to the 
        // list to play called "numbers/[number]" - i.e. numbers/45 or numbers/1. This is used a lot in the implementations below.
        private static String folderNumbersStub = "numbers/";
        private static String folderAndPrefix = "and_";
        // the number sounds for tenths sometimes need a 'hanging inflection' - add this suffix when they do
        private static String moreInflectionSuffix = "_more";

        private static String folderMinutes = folderNumbersStub + "minutes";
        private static String folderAMinute = folderNumbersStub + "a_minute";
        private static String folderHours = folderNumbersStub + "hours";
        private static String folderASecond = folderNumbersStub + "a_second";
        private static String folderSeconds = folderNumbersStub + "seconds";
        public static String folderAnd = folderNumbersStub + "and";


        private static String folderNetto = folderNumbersStub + "netto";
        private static String folderNetti = folderNumbersStub + "netti";


        // these are used to build up the hundreds folder names, combining with folderNumbersStub - i.e.
        // "numbers_it/1_hundreds","numbers_it/2_hundreds", etc
        private static String folderHundredSuffix = "_hundreds";

        private static String folderThousand = folderNumbersStub + "thousand";
        private static String folderThousands = folderNumbersStub + "thousands";

        private enum Unit { HOUR, MINUTE, SECOND, AND_TENTH, JUST_TENTH, AND_HUNDREDTH, JUST_HUNDREDTH }

        // folders for reading out times.

        // This is combined with folderNumbersStub to produce tenths sounds for tenths > 1 - numbers_it/2_tenths -> numbers_it/9_tenths
        // private static String folderTenthsSuffix = "_tenths";
        private static String folderATenth = folderNumbersStub + "a_tenth";

        protected override String getLocale()
        {
            return "it";
        }


        /**
         * Get an Italian sound for a whole number of hours. Long form.
         */
        protected override List<String> GetHoursSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            if (hours > 0)
            {
                messages.AddRange(resolveNumberSounds(false, hours, Unit.HOUR, !messageHasContentAfterTime));
                if (minutes == 0)
                {
                    if (hours > 1)
                    {
                        if (messageHasContentAfterTime)
                        {
                            messages.Add(getSoundWithMoreInflection(folderNetti));
                        }
                        else
                        {
                            messages.Add(folderNetti);
                        }
                    }
                    else
                    {
                        if (messageHasContentAfterTime)
                        {
                            messages.Add(getSoundWithMoreInflection(folderNetto));
                        }
                        else
                        {
                            messages.Add(folderNetto);
                        }
                    }
                }
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of minutes. Long form.
         */
        protected override List<String> GetMinutesSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            if (minutes > 0)
            {
                if (hours > 0)
                {
                    // skip seconds and tenths, use 'and' if we can
                    messages.AddRange(resolveNumberSounds(true, minutes, Unit.MINUTE, messageHasContentAfterTime));
                }
                else
                {
                    // no hours, so we may be reading seconds / tenths as well.
                    if (seconds == 0 && tenths == 0)
                    {
                        messages.AddRange(resolveNumberSounds(false, minutes, Unit.MINUTE, !messageHasContentAfterTime));
                        if (precision != Precision.MINUTES)
                        {
                            if (minutes > 1)
                            {
                                if (messageHasContentAfterTime)
                                {
                                    messages.Add(getSoundWithMoreInflection(folderNetti));
                                }
                                else
                                {
                                    messages.Add(folderNetti);
                                }
                            }
                            else
                            {
                                if (messageHasContentAfterTime)
                                {
                                    messages.Add(getSoundWithMoreInflection(folderNetto));
                                }
                                else
                                {
                                    messages.Add(folderNetto);
                                }
                            }
                        }
                    }
                    else
                    {
                        // when we're here, we know that there are seconds or tenths to come. We'll either add a 'netti' for 
                        // 0 tenths, or an 'and 8' for, e.g., 8 tenths. This means we only have 1 case where the rising inflection
                        // is needed - some minutes, 0 seconds and some tenths with no content after the time (1 minute! and 8)
                        messages.AddRange(resolveNumberSounds(false, minutes, Unit.MINUTE, !messageHasContentAfterTime && seconds == 0));
                    }
                }
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of seconds. Long form.
         */
        protected override List<String> GetSecondsSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            // special case here - if we're reading a time which has hours, the seconds aren't significant so ignore them
            if (hours == 0)
            {
                if (seconds > 0)
                {
                    if (tenths == 0)
                    {
                        messages.AddRange(resolveNumberSounds(minutes > 0, seconds, Unit.SECOND, !messageHasContentAfterTime));
                        if (precision != Precision.SECONDS && precision != Precision.MINUTES)
                        {
                            if (seconds > 1)
                            {
                                if (messageHasContentAfterTime)
                                {
                                    messages.Add(getSoundWithMoreInflection(folderNetti));
                                }
                                else
                                {
                                    messages.Add(folderNetti);
                                }
                            }
                            else
                            {
                                if (messageHasContentAfterTime)
                                {
                                    messages.Add(getSoundWithMoreInflection(folderNetto));
                                }
                                else
                                {
                                    messages.Add(folderNetto);
                                }
                            }
                        }
                    }
                    else
                    {
                        messages.AddRange(resolveNumberSounds(false, seconds, Unit.SECOND, !messageHasContentAfterTime));
                    }
                }
            }
            return messages;
        }

        /**
         * Get an Italian sound for a whole number of tenths of a second.
         */
        protected override List<String> GetTenthsSounds(int hours, int minutes, int seconds, int tenths, Boolean messageHasContentAfterTime, Precision precision)
        {
            List<String> messages = new List<String>();
            if (tenths > 0)
            {
                Boolean haveMinutesOrSeconds = minutes > 0 || seconds > 0;
                messages.AddRange(resolveNumberSounds(haveMinutesOrSeconds, tenths, 
                    haveMinutesOrSeconds ? Unit.AND_TENTH : Unit.JUST_TENTH, messageHasContentAfterTime));
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
         * fraction is String so we can pass "01" etc - we don't know if it's tenths or hundredths so it may need zero padding.
         */
        protected override List<String> GetMinutesAndSecondsWithFraction(int minutes, int seconds, String fraction, Boolean messageHasContentAfterTime)
        {
            // there will always be some seconds here
            String combinedMinutesAndSecondsSoundFolder;
            List<String> separateMinutesAndSecondsSoundFolders = new List<string>();
            String fractionsFolder;
            Boolean usePoint = false;

            // we check for the existence of a '1_23_more' type sound, so don't need to check this twice:
            Boolean alreadyCheckedCombinedSound = false;
            if (minutes > 0)
            {
                separateMinutesAndSecondsSoundFolders.Add(folderNumbersStub + minutes.ToString());
                // TODO: are we using padded seconds here?
                String paddedSeconds = seconds < 10 ? "_0" + seconds : "_" + seconds;
                combinedMinutesAndSecondsSoundFolder = folderNumbersStub + minutes + paddedSeconds;
                if (!messageHasContentAfterTime)
                {
                    String separateSound = folderNumbersStub + seconds.ToString();
                    String separateSoundWithMore = separateSound + moreInflectionSuffix;
                    String combinedSoundWithMore = combinedMinutesAndSecondsSoundFolder + moreInflectionSuffix;
                    if (SoundCache.availableSounds.Contains(separateSoundWithMore))
                    {
                        separateMinutesAndSecondsSoundFolders.Add(separateSoundWithMore);
                    }
                    else
                    {
                        separateMinutesAndSecondsSoundFolders.Add(separateSound);
                    }
                    if (SoundCache.availableSounds.Contains(combinedSoundWithMore))
                    {
                        alreadyCheckedCombinedSound = true;
                        combinedMinutesAndSecondsSoundFolder = combinedSoundWithMore;
                    }
                    else
                    {
                        Console.WriteLine("Unable to find number sound: " + combinedSoundWithMore);
                    }
                }
                else
                {
                    separateMinutesAndSecondsSoundFolders.Add(folderNumbersStub + seconds.ToString());
                }
            }
            else
            {
                combinedMinutesAndSecondsSoundFolder = folderNumbersStub + seconds.ToString();
                if (!messageHasContentAfterTime)
                {
                    String combinedSoundWithMore = combinedMinutesAndSecondsSoundFolder + moreInflectionSuffix;
                    if (SoundCache.availableSounds.Contains(combinedSoundWithMore))
                    {
                        alreadyCheckedCombinedSound = true;
                        combinedMinutesAndSecondsSoundFolder = combinedSoundWithMore;
                    }
                    else
                    {
                        Console.WriteLine("Unable to find number sound: " + combinedSoundWithMore);
                    }
                }
            }
            if (fraction == "0" || fraction == "00")
            {
                fractionsFolder = folderNetti;
                if (messageHasContentAfterTime)
                {
                    fractionsFolder += moreInflectionSuffix;
                }
            }
            else if (fraction.Length == 1)
            {
                fractionsFolder = folderNumbersStub + folderAndPrefix + fraction;
                if (messageHasContentAfterTime)
                {
                    String fractionsFolderWithMore = fractionsFolder + moreInflectionSuffix;
                    if (SoundCache.availableSounds.Contains(fractionsFolderWithMore))
                    {
                        fractionsFolder = fractionsFolderWithMore;
                    }
                    else
                    {
                        Console.WriteLine("Unable to find number sound: " + fractionsFolderWithMore);
                    }
                }
            }
            else
            {
                fractionsFolder = folderNumbersStub + fraction;
                if (messageHasContentAfterTime)
                {
                    String fractionsFolderWithMore = fractionsFolder + moreInflectionSuffix;
                    if (SoundCache.availableSounds.Contains(fractionsFolderWithMore))
                    {
                        fractionsFolder = fractionsFolderWithMore;
                    }
                    else
                    {
                        Console.WriteLine("Unable to find number sound: " + fractionsFolderWithMore);
                    }
                }
                usePoint = true;
            }
            List<String> messages = new List<String>();

            Boolean addCombined = true;
            if (!alreadyCheckedCombinedSound && !SoundCache.availableSounds.Contains(combinedMinutesAndSecondsSoundFolder))
            {
                addCombined = false;
            }
            if (addCombined) 
            {
                messages.Add(combinedMinutesAndSecondsSoundFolder);
            }
            else
            {
                Console.WriteLine("Unable to find number sound: " + combinedMinutesAndSecondsSoundFolder);
                messages.AddRange(separateMinutesAndSecondsSoundFolders);
            }
            if (usePoint)
            {
                messages.Add(folderPoint);
            }
            messages.Add(fractionsFolder);
            Console.WriteLine("Reading short form with sounds " + String.Join(", ", messages));
            return messages;
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
            // now add a _more inflection to the last number *or* the second to last
            int messageCount = messages.Count;
            int indexToAddInflection = messageHasContentAfterNumber ? messageCount - 1 : messageCount - 2;
            if (indexToAddInflection >= 0)
            {
                messages[indexToAddInflection] = getSoundWithMoreInflection(messages[indexToAddInflection]);
            }
            return messages;
        }

        private String getSoundWithMoreInflection(String folder)
        {
            if (SoundCache.availableSounds.Contains(folder + moreInflectionSuffix))
            {
                return folder + moreInflectionSuffix;
            }
            return folder;
        }

        private String getSuffixForUnit(Unit unit)
        {
            switch (unit)
            {
                case Unit.HOUR:
                    return "_hours";
                case Unit.MINUTE:
                    return "_minutes";
                case Unit.SECOND:
                    return "_seconds";
                case Unit.JUST_TENTH:
                     return "_tenths";
                default:
                    // used for 'and_X' sounds
                    return "";
            }
        }

        private String getFolderForUnit(Unit unit, int number)
        {
            switch (unit)
            {
                case Unit.HOUR:
                    return NumberReaderIt2.folderNumbersStub + (number == 1 ? "hour" : "hours");
                case Unit.MINUTE:
                    return NumberReaderIt2.folderNumbersStub + (number == 1 ? "minute" : "minutes");
                case Unit.SECOND:
                    return NumberReaderIt2.folderNumbersStub + (number == 1 ? "second" : "seconds");
                case Unit.JUST_TENTH:
                    return NumberReaderIt2.folderNumbersStub + (number == 1 ? "tenth" : "tenths");
                default:
                    return "";
            }
        }

        private List<String> resolveNumberSounds(Boolean startWithAnd, int number, Unit unitEnum, Boolean useMoreInflection)
        {
            String unitSuffix = getSuffixForUnit(unitEnum);
            String unitFolder = getFolderForUnit(unitEnum, number);
            List<String> sounds = new List<string>();
            if (startWithAnd)
            {
                if (useMoreInflection)
                {
                    String sound = folderNumbersStub + folderAndPrefix + number + unitSuffix + moreInflectionSuffix;
                    Console.WriteLine("looking for sound " + sound);
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        // this is the best case - the full sound is available
                        Console.WriteLine("Got sound for all parameters");
                        sounds.Add(sound);
                        return sounds;
                    }
                    // oh dear, we don't have this sound.
                    // assume the inflection is a requirement and try without the units
                    sound = folderNumbersStub + folderAndPrefix + number;
                    // this should never get any hits:
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        // we have the 'and' version of the sound but are missing the requested inflection on the unit.
                        // So we return the sound without the unit, and the unit with inflection separately.
                        // Here we assume the single unit recordings have _more versions. This is a requirement.                        
                        sounds.Add(sound);
                        sounds.Add(getSoundWithMoreInflection(unitFolder));
                        Console.WriteLine("Got sound for parameters without unit: " + String.Join(", ", sounds));
                        return sounds;
                    }
                    sound = folderNumbersStub + number + unitSuffix + moreInflectionSuffix;
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        sounds.Add(folderAnd);
                        sounds.Add(sound);
                        Console.WriteLine("Got sound for parameters without 'and':" + String.Join(", ", sounds));
                        return sounds;
                    }
                    sounds.Add(folderAnd);
                    sounds.Add(folderNumbersStub + number);
                    sounds.Add(getSoundWithMoreInflection(unitFolder));
                    Console.WriteLine("Returning individual sounds: " + String.Join(", ", sounds));
                    return sounds;
                }
                else
                {
                    String sound = folderNumbersStub + folderAndPrefix + number + unitSuffix;
                    Console.WriteLine("looking for sound " + sound);
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        // this is the best case - the full sound is available
                        Console.WriteLine("Got sound for all parameters: " + sound);
                        sounds.Add(sound);
                        return sounds;
                    }
                    sound = folderNumbersStub + number + unitSuffix;
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        sounds.Add(folderAnd);
                        sounds.Add(sound);
                        Console.WriteLine("Got sound for parameters without 'and': " + String.Join(", ", sounds));
                        return sounds;
                    }
                    sounds.Add(folderAnd);
                    sounds.Add(folderNumbersStub + number);
                    sounds.Add(unitFolder);
                    Console.WriteLine("Returning individual sounds: " + String.Join(", ", sounds));
                    return sounds;
                }
            }
            else
            {
                if (useMoreInflection)
                {
                    String sound = folderNumbersStub + number + unitSuffix + moreInflectionSuffix;
                    Console.WriteLine("looking for sound " + sound);
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        // this is the best case - the full sound is available
                        Console.WriteLine("Got sound for all parameters: " + sound);
                        sounds.Add(sound);
                        return sounds;
                    }
                    sounds.Add(folderNumbersStub + number);
                    sounds.Add(getSoundWithMoreInflection(unitFolder));
                    Console.WriteLine("Returning individual sounds: " + String.Join(", ", sounds));
                    return sounds;
                }
                else
                {
                    String sound = folderNumbersStub + number + unitSuffix;
                    Console.WriteLine("looking for sound " + sound);
                    if (SoundCache.availableSounds.Contains(sound))
                    {
                        // this is the best case - the full sound is available
                        Console.WriteLine("Got sound for all parameters: " + sound);
                        sounds.Add(sound);
                        return sounds;
                    }
                    sounds.Add(folderNumbersStub + number);
                    sounds.Add(unitFolder);
                    Console.WriteLine("Returning individual sounds: " + String.Join(", ", sounds));
                    return sounds;
                }
            }
        }
    }
}
