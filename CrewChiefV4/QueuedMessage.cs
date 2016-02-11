using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.Events;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4
{
    public class MessageFragment
    {
        public enum FragmentType
        {
            Text, Time, Opponent, Integer
        }

        public String text;
        public TimeSpanWrapper timeSpanWrapper;
        public OpponentData opponent;
        public int integer;
        public FragmentType type;

        private MessageFragment(String text, TimeSpanWrapper timeSpanWrapper, OpponentData opponent, FragmentType type)
        {
            this.text = text;
            this.timeSpanWrapper = timeSpanWrapper;
            this.opponent = opponent;
            this.type = type;
        }

        private MessageFragment(int integer, FragmentType type)
        {
            this.integer = integer;
            this.type = type;
        }

        public static MessageFragment Text(String text)
        {
            return new MessageFragment(text, null, null, FragmentType.Text);
        }
        public static MessageFragment Time(TimeSpanWrapper timeSpanWrapper)
        {
            return new MessageFragment(null, timeSpanWrapper, null, FragmentType.Time);
        }

        public static MessageFragment Opponent(OpponentData opponent)
        {
            return new MessageFragment(null, null, opponent, FragmentType.Opponent);
        }

        public static MessageFragment Integer(int integer)
        {
            return new MessageFragment(integer, FragmentType.Integer);
        }
    }

    public class QueuedMessage
    {

        private Boolean prefix_hundred_and_thousand_with_one = Boolean.Parse(Configuration.getSoundConfigOption("prefix_hundred_and_thousand_with_one"));
        private Boolean say_and_between_hundred_and_units = Boolean.Parse(Configuration.getSoundConfigOption("say_and_between_hundred_and_units"));
        private Boolean say_and_between_thousand_and_units = Boolean.Parse(Configuration.getSoundConfigOption("say_and_between_thousand_and_units"));
        private Boolean allow_short_hundreds = Boolean.Parse(Configuration.getSoundConfigOption("allow_short_hundreds"));   // allow "one oh four", instead of "one hundred and four"
        private Boolean always_use_thousands = Boolean.Parse(Configuration.getSoundConfigOption("always_use_thousands"));   // don't allow "thirteen hundred" etc

        private static String compoundMessageIdentifier = "COMPOUND_";

        public static String folderNameOh = "numbers/oh";
        public static String folderNamePoint = "numbers/point";
        private static String folderNameNumbersStub = "numbers/";
        private static String folderNameThousand = "numbers/thousand";
        private static String folderNameThousandAnd = "numbers/thousand_and";
        private static String folderNameHundred = "numbers/hundred";
        private static String folderNameHundredAnd = "numbers/hundred_and";
        public static String folderZeroZero = "numbers/zerozero";
        public static String folderSeconds = "numbers/seconds";
        public static String folderSecond = "numbers/second";
        
        public int maxPermittedQueueLengthForMessage = 0;         // 0 => don't check queue length
        public long dueTime;
        public AbstractEvent abstractEvent;
        public String messageName;
        public List<String> messageFolders;
        public Boolean playEvenWhenSilenced;

        private Random rand = new Random();

        // some snapshot of pertentent data at the point of creation, 
        // which can be validated before it actually gets played. E.g.
        // e.g. {SessionData.Position = 1}
        public Dictionary<String, Object> validationData = null;

        public long expiryTime = 0;

        // Note that even when queuing a message with 0 delay, we always wait 1 complete update interval. This is to 
        // (hopefully...) address issues where some data in the block get updated (like the lap count), but other data haven't 
        // get been updated (like the session phase)
        private int updateInterval = UserSettings.GetUserSettings().getInt("update_interval");

        // if any of the sound clips in this message are missing, this will be set to false when the constructors
        // get the message folders to use
        public Boolean canBePlayed = true;

        // used for creating a pearl of wisdom message where we need to copy the dueTime from the original
        public QueuedMessage(AbstractEvent abstractEvent)
        {
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(String messageName, List<MessageFragment> messageFragments, int secondsDelay, AbstractEvent abstractEvent,
            Dictionary<String, Object> validationData) : this(messageName, messageFragments, secondsDelay, abstractEvent)
        {
            this.validationData = validationData;
        }

        public QueuedMessage(String messageName, List<MessageFragment> messageFragments, int secondsDelay, AbstractEvent abstractEvent)
        {
            this.messageName = compoundMessageIdentifier + messageName;
            this.messageFolders = getMessageFolders(messageFragments);
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(String messageName, List<MessageFragment> messageFragments, List<MessageFragment> alternateMessageFragments,
            int secondsDelay, AbstractEvent abstractEvent, Dictionary<String, Object> validationData) : 
            this(messageName, messageFragments, alternateMessageFragments, secondsDelay, abstractEvent)
        {
            this.validationData = validationData;
        }
        /**
         * Queues a message with multiple fragments, with an alternate version if the first version can't be played.
         * Use this when a compound message includes a driver name which may or may not be in the set that are have associated
         * sound files. If there's no sound file for this driver name, the alternate message will be played
         */
        public QueuedMessage(String messageName, List<MessageFragment> messageFragments, List<MessageFragment> alternateMessageFragments, 
            int secondsDelay, AbstractEvent abstractEvent)
        {
            this.messageName = compoundMessageIdentifier + messageName;
            this.messageFolders = getMessageFolders(messageFragments);
            if (!canBePlayed)
            {
                Console.WriteLine("Using secondary messages for event " + messageName);
                canBePlayed = true;
                this.messageFolders = getMessageFolders(alternateMessageFragments);
                if (!canBePlayed)
                {
                    Console.WriteLine("Primary and secondary messages for event " +
                        messageName + " can't be played");
                }
            }
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(String message, int secondsDelay, AbstractEvent abstractEvent, 
            Dictionary<String, Object> validationData) : this (message, secondsDelay, abstractEvent)
        {
            this.validationData = validationData;
        }

        public QueuedMessage(String message, int secondsDelay, AbstractEvent abstractEvent)
        {
            this.messageName = message;
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            messageFragments.Add(MessageFragment.Text(message));
            this.messageFolders = getMessageFolders(messageFragments);
            this.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public Boolean isMessageStillValid(String eventSubType, GameStateData currentGameState)
        {
            return this.abstractEvent == null || 
                this.abstractEvent.isMessageStillValid(eventSubType, currentGameState, validationData);
        }

        private List<String> getMessageFolders(List<MessageFragment> messageFragments)
        {
            List<String> messages = new List<String>();
            foreach (MessageFragment messageFragment in messageFragments) 
            {
                if (messageFragment == null)
                {
                    canBePlayed = false;
                    break;
                }
                switch (messageFragment.type)
                {
                    case MessageFragment.FragmentType.Text:
                        if (messageFragment.text.StartsWith(AudioPlayer.PAUSE_ID) || SoundCache.availableSounds.Contains(messageFragment.text) ||
                            SoundCache.availableDriverNames.Contains(messageFragment.text))
                        {
                            messages.Add(messageFragment.text);
                        }
                        else
                        {
                            canBePlayed = false;
                        }                     
                        break;
                    case MessageFragment.FragmentType.Time:
                        List<String> timeFolders = getTimeMessageFolders(messageFragment.timeSpanWrapper.timeSpan, messageFragment.timeSpanWrapper.readSeconds);
                        if (timeFolders.Count == 0)
                        {
                            canBePlayed = false;
                        }
                        else { 
                            foreach (String timeFolder in timeFolders)
                            {
                                if (!SoundCache.availableSounds.Contains(timeFolder))
                                {
                                    canBePlayed = false;
                                    break;
                                }
                            }
                             messages.AddRange(timeFolders);
                        }
                        break;                    
                    case MessageFragment.FragmentType.Opponent:
                        canBePlayed = false;
                        if (messageFragment.opponent != null)
                        {
                            String usableName = DriverNameHelper.getUsableNameForRawName(messageFragment.opponent.DriverRawName);
                            if (SoundCache.availableDriverNames.Contains(usableName))
                            {
                                messages.Add(usableName);
                                canBePlayed = true;
                            }
                        }                        
                        break;
                    case MessageFragment.FragmentType.Integer:
                        List<String> integerFolders = getIntegerMessageFolders(messageFragment.integer);
                        if (integerFolders.Count() == 0) {
                            canBePlayed = false;
                            break;
                        } else {
                            foreach (String integerFolder in integerFolders) {
                                if (!SoundCache.availableSounds.Contains(integerFolder))
                                {
                                    canBePlayed = false;
                                    break;
                                }
                            }
                        }
                        messages.AddRange(integerFolders);
                        break;
                }
                if (!canBePlayed)
                {
                    break;
                }
            }
            return messages;
        }

        private List<String> getIntegerMessageFolders(int integer) {
        List<String> messages = new List<String>();
        if (integer == 0) {
            messages.Add(folderNameNumbersStub + 0);
        } else if (integer < 100000 && integer > 0){
            char[] digits = integer.ToString().ToCharArray();
            String tensAndUnits = null;
            String hundreds = null;
            String thousands = null;

            if (digits.Count() == 1 || (digits[digits.Count() - 2] == '0' && digits[digits.Count() - 1] != '0'))
            {
                tensAndUnits = digits[digits.Count() - 1].ToString();
            }
            else if (digits[digits.Count() - 2] != '0' || digits[digits.Count() - 1] != '0')
            {
                tensAndUnits = digits[digits.Count() - 2].ToString() + digits[digits.Count() - 1].ToString();
            }
            if (digits.Count() == 4 && digits[0] == '1' && digits[1] != '0' && !always_use_thousands) {
                // don't allow "thirteen hundred" type messages if always_use_thousands is true
                hundreds = digits[0].ToString() + digits[1].ToString();
            } else {
                if (digits.Count() >= 3) {
                    if (digits[digits.Count() - 3] != '0') {
                        hundreds = digits[digits.Count() - 3].ToString();
                    }
                    if (digits.Count() == 4) {
                        thousands = digits[0].ToString();
                    } else if (digits.Count() == 5) {
                        thousands = digits[0].ToString() + digits[1].ToString();
                    }
                }
            }
            if (thousands != null) {
                if (thousands != "1" || prefix_hundred_and_thousand_with_one)
                {
                    messages.Add(folderNameNumbersStub + thousands);
                }
                if (hundreds == null && tensAndUnits != null && say_and_between_thousand_and_units) {
                    messages.Add(folderNameThousandAnd);
                } else {
                    messages.Add(folderNameThousand);
                }
            }
            if (hundreds != null) {
                Boolean saidNumberOfHundreds = false;
                if (hundreds != "1" || prefix_hundred_and_thousand_with_one)
                {
                    saidNumberOfHundreds = true;
                    messages.Add(folderNameNumbersStub + hundreds);
                }
                // don't always use "hundred and"
                Boolean addedHundreds = false;
                if (tensAndUnits != null) {
                    // if there's a thousand, or we're saying something like "13 hundred", then always use the long version
                    if (!allow_short_hundreds || hundreds.Count() == 2 || thousands != null || rand.NextDouble() > 0.6)
                    {
                        if (say_and_between_hundred_and_units)
                        {
                            messages.Add(folderNameHundredAnd);
                        }
                        else
                        {
                            messages.Add(folderNameHundred);
                        }
                        addedHundreds = true;
                    }
                } else {
                    messages.Add(folderNameHundred);
                    addedHundreds = true;
                }
                if (!addedHundreds)
                {
                    if (!saidNumberOfHundreds)
                    {
                        messages.Add(folderNameNumbersStub + hundreds);
                    }
                    if (tensAndUnits != null && tensAndUnits.Count() == 1)
                    {
                        // need to modify the tensAndUnits here - we've skipped "hundreds" even though the number is > 99.
                        // This is fine if the tensAndUnits > 9 (it'll be read as "One twenty five"), but if the tensAndUnits < 10
                        // this will be read as "One two" instead of "One oh two".
                        tensAndUnits = "0" + tensAndUnits;
                    }
                }
            }
            if (tensAndUnits != null) {
                messages.Add(folderNameNumbersStub + tensAndUnits);                
            }
        }
        return messages;
    }

        private List<String> getTimeMessageFolders(TimeSpan timeSpan, Boolean includeSeconds)
        {
            List<String> messages = new List<String>();
            if (timeSpan != null)
            {
                // if the milliseconds would is > 949, when we turn this into tenths it'll get rounded up to 
                // ten tenths, which we can't have. So move the timespan on so this rounding doesn't happen
                if (timeSpan.Milliseconds > 949)
                {
                    timeSpan = timeSpan.Add(TimeSpan.FromMilliseconds(1000 - timeSpan.Milliseconds));
                }
                if (timeSpan.Minutes > 0)
                {
                    messages.AddRange(getFolderNames(timeSpan.Minutes, ZeroType.NONE));
                    if (timeSpan.Seconds == 0)
                    {
                        // add "zero-zero" for messages with minutes in them
                        messages.Add(folderZeroZero);
                    }
                    else
                    {
                        messages.AddRange(getFolderNames(timeSpan.Seconds, ZeroType.OH));
                    }
                }
                else
                {
                    messages.AddRange(getFolderNames(timeSpan.Seconds, ZeroType.NONE));
                }
                int tenths = (int)Math.Round(((double)timeSpan.Milliseconds / 100));
                if (includeSeconds)
                {
                    if (tenths == 0)
                    {
                        if (timeSpan.Minutes == 0)
                        {
                            if (timeSpan.Seconds == 1)
                            {
                                messages.Add(folderSecond);
                            }
                            else
                            {
                                messages.Add(folderSeconds);
                            }
                        }
                        else
                        {
                            messages.Add(folderNamePoint);
                            messages.Add(folderNameOh);
                            messages.Add(folderSeconds);
                        }
                    }
                    else
                    {
                        messages.Add(folderNameNumbersStub + "point" + tenths + "seconds");
                    }
                }
                else
                {
                    messages.Add(folderNamePoint);
                    if (tenths == 0)
                    {
                        messages.Add(folderNameNumbersStub + 0);
                    }
                    else
                    {
                        messages.AddRange(getFolderNames(tenths, ZeroType.NONE));
                    }
                }                
            }
            return messages;
        }

        private List<String> getFolderNames(int number, ZeroType zeroType)
        {
            List<String> names = new List<String>();
            if (number < 60)
            {
                if (number < 10)
                {
                    // if the number is < 10, use the "oh two" files if we've asked for "oh" instead of "zero"
                    if (zeroType == ZeroType.OH)
                    {
                        if (number == 0)
                        {
                            // will this block ever be reached?
                            names.Add(folderNameOh);
                        }
                        else
                        {
                            names.Add(folderNameNumbersStub + "0" + number);
                        }
                    }
                    else if (zeroType == ZeroType.ZERO)
                    {
                        names.Add(folderNameNumbersStub + 0);
                        if (number > 0)
                        {
                            names.Add(folderNameNumbersStub + number);
                        }
                    }
                    else if (zeroType != ZeroType.NONE || number > 0)
                    {
                        names.Add(folderNameNumbersStub + number);
                    }
                }
                else
                {
                    // > 10 so use the actual number
                    names.Add(folderNameNumbersStub + number);
                }
            }
            return names;
        }

        private enum ZeroType
        {
            NONE, OH, ZERO
        }
    }
}
