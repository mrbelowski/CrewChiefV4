using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.Events;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4
{
    public enum FragmentType
    {
        Text, Time, Opponent, Integer, Delayed
    }

    public class DelayedMessageEvent
    {
        public String methodName;
        public Object[] methodParams;
        public AbstractEvent abstractEvent;
        public DelayedMessageEvent(String methodName, Object[] methodParams, AbstractEvent abstractEvent)
        {
            this.methodName = methodName;
            this.methodParams = methodParams;
            this.abstractEvent = abstractEvent;
        }
    }

    public class MessageFragment
    {        
        public String text;
        public TimeSpanWrapper timeSpan;
        public OpponentData opponent;
        public int integer;
        public FragmentType type;

        private MessageFragment(String text)
        {
            this.text = text;
            this.type = FragmentType.Text;
        }

        private MessageFragment(TimeSpanWrapper timeSpan)
        {
            this.timeSpan = timeSpan;
            this.type = FragmentType.Time;
        }

        private MessageFragment(OpponentData opponent)
        {
            this.opponent = opponent;
            this.type = FragmentType.Opponent;
        }

        private MessageFragment(int integer)
        {
            this.integer = integer;
            this.type = FragmentType.Integer;
        }

        public static MessageFragment Text(String text)
        {
            return new MessageFragment(text);
        }
        public static MessageFragment Time(TimeSpanWrapper timeSpan)
        {
            return new MessageFragment(timeSpan);
        }

        public static MessageFragment Opponent(OpponentData opponent)
        {
            return new MessageFragment(opponent);
        }

        public static MessageFragment Integer(int integer)
        {
            return new MessageFragment(integer);
        }

        public override String ToString()
        {
            if (type == FragmentType.Text)
            {
                return text;
            }
            if (type == FragmentType.Integer)
            {
                return integer.ToString();
            }
            if (type == FragmentType.Opponent && opponent != null)
            {
                return opponent.DriverRawName;
            }
            if (type == FragmentType.Time && timeSpan != null)
            {
                return timeSpan.ToString();
            }
            return "";
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
        
        public int maxPermittedQueueLengthForMessage = 0;         // 0 => don't check queue length
        public long dueTime;
        public AbstractEvent abstractEvent;
        public String messageName;
        public List<String> messageFolders;
        public Boolean playEvenWhenSilenced;

        private DelayedMessageEvent delayedMessageEvent;
        public Boolean delayMessageResolution = false;

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

        private NumberReader numberReader = NumberReaderFactory.GetNumberReader();

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
            this.messageFolders = getMessageFolders(messageFragments, false);
            this.dueTime = secondsDelay == 0 ? 0 : (GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
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
            this.messageFolders = getMessageFolders(messageFragments, true);
            if (!canBePlayed)
            {
                Console.WriteLine("Using secondary messages for event " + messageName);
                canBePlayed = true;
                this.messageFolders = getMessageFolders(alternateMessageFragments, false);
                if (!canBePlayed)
                {
                    Console.WriteLine("Primary and secondary messages for event " +
                        messageName + " can't be played");
                }
            }
            this.dueTime = secondsDelay == 0 ? 0 : (GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
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
            this.messageFolders = getMessageFolders(messageFragments, false);
            this.dueTime = secondsDelay == 0 ? 0 : (GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.abstractEvent = abstractEvent;
        }

        public QueuedMessage(String messageName, DelayedMessageEvent delayedMessageEvent, int secondsDelay, AbstractEvent abstractEvent)
        {
            this.messageName = compoundMessageIdentifier + messageName;
            this.delayedMessageEvent = delayedMessageEvent;
            this.delayMessageResolution = true;
            this.dueTime = secondsDelay == 0 ? 0 : (GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond) + (secondsDelay * 1000) + updateInterval;
            this.delayMessageResolution = true;
            this.abstractEvent = abstractEvent;
        }

        public Boolean isMessageStillValid(String eventSubType, GameStateData currentGameState)
        {
            return this.abstractEvent == null || 
                this.abstractEvent.isMessageStillValid(eventSubType, currentGameState, validationData);
        }

        public void resolveDelayedContents()
        {
            this.messageFolders = getMessageFolders((List<MessageFragment>) delayedMessageEvent.abstractEvent.GetType().GetMethod(delayedMessageEvent.methodName).
                Invoke(delayedMessageEvent.abstractEvent, delayedMessageEvent.methodParams), false);
        }

        private List<String> getMessageFolders(List<MessageFragment> messageFragments, Boolean hasAlternative)
        {
            List<String> messages = new List<String>();
            for (int i=0; i< messageFragments.Count; i++) 
            {
                MessageFragment messageFragment = messageFragments[i];
                if (messageFragment == null)
                {
                    canBePlayed = false;
                    break;
                }
                switch (messageFragment.type)
                {
                    case FragmentType.Text:
                        if (messageFragment.text.StartsWith(AudioPlayer.PAUSE_ID) || SoundCache.sortedAvailableSounds.BinarySearch(messageFragment.text) >= 0 ||
                            SoundCache.sortedAvailableDriverNames.BinarySearch(messageFragment.text) >= 0)
                        {
                            messages.Add(messageFragment.text);
                        }
                        else
                        {
                            canBePlayed = false;
                        }                     
                        break;
                    case FragmentType.Time:
                        // if this time fragment is not the last message fragment, then some languages (Italian only at the time of writing)
                        // require a different inflection to their tenths sounds
                        Boolean useMoreInflection = i < messageFragments.Count - 1;
                        if (numberReader != null)
                        {
                            List<String> timeFolders = numberReader.ConvertTimeToSounds(messageFragment.timeSpan, useMoreInflection);
                            if (timeFolders.Count == 0)
                            {
                                canBePlayed = false;
                            }
                            else
                            {
                                foreach (String timeFolder in timeFolders)
                                {
                                    if (!timeFolder.StartsWith(AudioPlayer.PAUSE_ID) && SoundCache.sortedAvailableSounds.BinarySearch(timeFolder) < 0)
                                    {
                                        canBePlayed = false;
                                        break;
                                    }
                                }
                                messages.AddRange(timeFolders);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Number reader is not available");
                            canBePlayed = false;
                        }
                        break;                    
                    case FragmentType.Opponent:
                        canBePlayed = false;
                        if (messageFragment.opponent != null)
                        {
                            String usableName = DriverNameHelper.getUsableDriverName(messageFragment.opponent.DriverRawName);
                            if (SoundCache.sortedAvailableDriverNames.BinarySearch(usableName) >= 0)
                            {
                                messages.Add(usableName);
                                canBePlayed = true;
                            }
                            else if (usableName != null && usableName.Count() > 0 && SoundCache.useTTS && !hasAlternative)
                            {
                                messages.Add(SoundCache.TTS_IDENTIFIER + usableName);
                                canBePlayed = true;
                            }
                        }                        
                        break;
                    case FragmentType.Integer:                        
                        if (numberReader != null)
                        {
                            List<String> integerFolders = numberReader.GetIntegerSounds(messageFragment.integer);
                            if (integerFolders.Count() == 0)
                            {
                                canBePlayed = false;
                                break;
                            }
                            else
                            {
                                foreach (String integerFolder in integerFolders)
                                {
                                    if (!integerFolder.StartsWith(AudioPlayer.PAUSE_ID) && SoundCache.sortedAvailableSounds.BinarySearch(integerFolder) < 0)
                                    {
                                        canBePlayed = false;
                                        break;
                                    }
                                }
                            }
                            messages.AddRange(integerFolders);
                        }
                        else
                        {
                            Console.WriteLine("Number reader is not available");
                            canBePlayed = false;
                        }
                        break;
                }
                if (!canBePlayed)
                {
                    break;
                }
            }
            return messages;
        }
    }
}
