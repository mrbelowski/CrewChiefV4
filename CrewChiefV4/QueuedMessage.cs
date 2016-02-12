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
    public class MessageFragment
    {
        public enum FragmentType
        {
            Text, Time, Opponent, Integer
        }

        public String text;
        public TimeSpan timeSpan;
        public OpponentData opponent;
        public int integer;
        public FragmentType type;

        private MessageFragment(String text)
        {
            this.text = text;
            this.type = FragmentType.Text;
        }

        private MessageFragment(TimeSpan timeSpan)
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
        public static MessageFragment Time(TimeSpan timeSpan)
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
                        List<String> timeFolders = NumberReaderFactory.GetNumberReader().ConvertTimeToSounds(messageFragment.timeSpan);
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
                        List<String> integerFolders = NumberReaderFactory.GetNumberReader().GetIntegerSounds(messageFragment.integer);
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
    }
}
