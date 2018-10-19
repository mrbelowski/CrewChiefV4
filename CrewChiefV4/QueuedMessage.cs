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

    // Delayed message event allows us to insert a message into the queue who's content will be altered in some way immediately before it's played.
    // The method 'methodName' is invoked in the specified AbstractEvent as late as possible int he workflow. 
    // The elements in the methodParams Object[] are passed to the named method. They must match in type and ordering.
    // 
    // This method MUST return Tuple<List<MessageFragment>, List<MessageFragment>>. Item1 is the 'primary' message contents and Item2 is the (optional) 
    // alternate message contents after the method has done its work to resolve them. Item2 is optional and can be null.
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
        public Boolean allowShortHundreds = true;   // allow a number like 160 to be read as "one sixty" instead of "one hundred and sixty"

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
            this.allowShortHundreds = true;
        }

        private MessageFragment(int integer, Boolean allowShortHundreds)
        {
            this.integer = integer;
            this.type = FragmentType.Integer;
            this.allowShortHundreds = allowShortHundreds;
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
            return MessageFragment.Integer(integer, true);
        }

        public static MessageFragment Integer(int integer, Boolean allowShortHundreds)
        {
            return new MessageFragment(integer, allowShortHundreds);
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
        // Note that even when queuing a message with 0 delay, we always wait 1 complete update interval. This is to 
        // (hopefully...) address issues where some data in the block get updated (like the lap count), but other data haven't 
        // get been updated (like the session phase)
        private static readonly int updateInterval = UserSettings.GetUserSettings().getInt("update_interval");

        private static readonly NumberReader numberReader = NumberReaderFactory.GetNumberReader();

        private static readonly String compoundMessageIdentifier = "COMPOUND_";
        private static readonly String delayedMessageIdentifier = "DELAYED_";

        public SoundMetadata metadata = null;  // null => a generic 'regular message' meta data object will be created automatically
                                               // for regular queue messages, and a 'high importance' metadata object create for immediate-queue messages
        
        public int maxPermittedQueueLengthForMessage = 0;         // 0 => don't check queue length
        public long dueTime;
        public AbstractEvent abstractEvent;
        public String messageName;
        public List<String> messageFolders;
        public Boolean playEvenWhenSilenced;

        // for delayed messages, sounds are resolved just-in-time
        private DelayedMessageEvent delayedMessageEvent;
        public Boolean delayMessageResolution = false;

        public int secondsDelay;

        private long creationTime;

        // some snapshot of pertentent data at the point of creation, 
        // which can be validated before it actually gets played. E.g.
        // e.g. {SessionData.Position = 1}
        public Dictionary<String, Object> validationData = null;

        public long expiryTime = 0;

        // if any of the sound clips in this message are missing, this will be set to false when the constructors
        // get the message folders to use
        public Boolean canBePlayed = true;

        public Boolean isRant = false;

        public int messageId = 0;

        private static int messageIdCounter = 0;

        private int getMessageId()
        {
            lock (this)
            {
                QueuedMessage.messageIdCounter++;
                return QueuedMessage.messageIdCounter;
            }
        }

        public QueuedMessage(String messageName, int expiresAfter, List<MessageFragment> messageFragments = null, 
            List<MessageFragment> alternateMessageFragments = null, DelayedMessageEvent delayedMessageEvent = null,
            int secondsDelay = 0, AbstractEvent abstractEvent = null, Dictionary<String, Object> validationData = null,
            int priority = SoundMetadata.DEFAULT_PRIORITY, SoundType type = SoundType.AUTO)
        {
            this.messageId = getMessageId();
            this.validationData = validationData;
            this.creationTime = GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond;
            this.dueTime = secondsDelay == 0 ? 0 : this.creationTime + (secondsDelay * 1000) + updateInterval;
            this.expiryTime = expiresAfter == 0 ? 0 : this.creationTime + (expiresAfter * 1000);
            this.secondsDelay = secondsDelay;
            this.abstractEvent = abstractEvent;
            this.metadata = new SoundMetadata(type, priority);
            this.delayedMessageEvent = delayedMessageEvent;
            this.delayMessageResolution = delayedMessageEvent != null;

            // for delayed message events, we collect up the message folder when the message when the message is about to be played, not here
            if (delayedMessageEvent != null)
            {
                this.messageName = delayedMessageIdentifier + messageName;
            }
            else if (messageFragments == null)
            {
                this.messageName = messageName;
                List<MessageFragment> singleMessageFragement = new List<MessageFragment>();
                singleMessageFragement.Add(MessageFragment.Text(messageName));
                this.messageFolders = getMessageFolders(singleMessageFragement, false);
            }
            else
            {
                this.messageName = compoundMessageIdentifier + messageName;
                Boolean hasAlternative = alternateMessageFragments != null;
                this.messageFolders = getMessageFolders(messageFragments, hasAlternative);
                if (!canBePlayed && hasAlternative)
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
            }
        }

        // used for creating a pearl of wisdom message where we need to copy the dueTime from the original
        public QueuedMessage(AbstractEvent abstractEvent)
        {
            this.abstractEvent = abstractEvent;
        }

        public long getAge()
        {
            return (GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond) - this.creationTime;
        }

        // called when we repeat this message - clears all the validation and sets the type to voice-command
        public void prepareToBeRepeated()
        {
            if (metadata == null)
            {
                metadata = new SoundMetadata();
            }
            messageName = "REPEAT_" + messageName;
            metadata.messageId = getMessageId();
            metadata.priority = 5;
            metadata.type = SoundType.VOICE_COMMAND_RESPONSE;
            dueTime = 0;
            expiryTime = 0;
            abstractEvent = null;
            validationData = null;
            secondsDelay = 0;
        }

        public override string ToString()
        {
            if (messageFolders != null)
            {
                return "(" + String.Join(", ", messageFolders) + ")";
            }
            else
            {
                return "";
            }
        }

        public Boolean isMessageStillValid(String eventSubType, GameStateData currentGameState)
        {
            return this.abstractEvent == null || 
                this.abstractEvent.isMessageStillValid(eventSubType, currentGameState, validationData);
        }

        public void resolveDelayedContents()
        {
            // the delayed resolution gets primary and alternate message fragments:
            Tuple<List<MessageFragment>, List<MessageFragment>> primaryAndAlternateMessages =
                (Tuple<List<MessageFragment>, List<MessageFragment>>) delayedMessageEvent.abstractEvent.GetType().GetMethod(delayedMessageEvent.methodName).
                Invoke(delayedMessageEvent.abstractEvent, delayedMessageEvent.methodParams);

            Boolean hasAlternate = primaryAndAlternateMessages.Item2 != null && primaryAndAlternateMessages.Item2.Count > 0;
            this.messageFolders = getMessageFolders(primaryAndAlternateMessages.Item1, hasAlternate);
            if (!canBePlayed && hasAlternate)
            {
                Console.WriteLine("Using secondary messages for delayed message resolution event " + messageName);
                canBePlayed = true;
                this.messageFolders = getMessageFolders(primaryAndAlternateMessages.Item2, false);
                if (!canBePlayed)
                {
                    Console.WriteLine("Primary and secondary messages for delayed resolution event " +
                        messageName + " can't be played");
                }
            }
        }

        private List<String> getMessageFolders(List<MessageFragment> messageFragments, Boolean hasAlternative)
        {
            List<String> messages = new List<String>();
            for (int i=0; i< messageFragments.Count; i++) 
            {
                MessageFragment messageFragment = messageFragments[i];
                if (messageFragment == null)
                {
                    Console.WriteLine("Message " + this.messageName + " can't be played because it has no contents");
                    canBePlayed = false;
                    break;
                }
                // if this fragment is not the last message fragment, then some languages (Italian only at the time of writing)
                // require a different inflection to the final part of a time / number sound.
                Boolean useMoreInflection = i < messageFragments.Count - 1;
                switch (messageFragment.type)
                {
                    case FragmentType.Text:
                        if (messageFragment.text.StartsWith(AudioPlayer.PAUSE_ID) || SoundCache.availableSounds.Contains(messageFragment.text) ||
                            SoundCache.hasSingleSound(messageFragment.text))
                        {
                            messages.Add(messageFragment.text);
                        }
                        else
                        {
                            Console.WriteLine("Message " + this.messageName + " can't be played because there is no sound for text fragment " + messageFragment.text);
                            canBePlayed = false;
                        }
                        break;
                    case FragmentType.Time:                        
                        if (numberReader != null)
                        {
                            List<String> timeFolders = numberReader.ConvertTimeToSounds(messageFragment.timeSpan, useMoreInflection);
                            if (timeFolders.Count == 0)
                            {
                                Console.WriteLine("Message " + this.messageName + " can't be played because the number reader found no sounds for timespan " 
                                    + messageFragment.timeSpan.timeSpan.ToString() + " precision " + messageFragment.timeSpan.getPrecision());
                                canBePlayed = false;
                            }
                            else
                            {
                                foreach (String timeFolder in timeFolders)
                                {
                                    if (!timeFolder.StartsWith(AudioPlayer.PAUSE_ID) && !SoundCache.availableSounds.Contains(timeFolder))
                                    {
                                        Console.WriteLine("Message " + this.messageName + " can't be played because there is no sound for time fragment " + timeFolder);
                                        canBePlayed = false;
                                        break;
                                    }
                                }
                                messages.AddRange(timeFolders);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Message " + this.messageName + " can't be played because the number reader is not available");
                            canBePlayed = false;
                        }
                        break;                    
                    case FragmentType.Opponent:
                        canBePlayed = false;
                        if (messageFragment.opponent != null && messageFragment.opponent.CanUseName)
                        {
                            String usableName = DriverNameHelper.getUsableDriverName(messageFragment.opponent.DriverRawName);
                            if (SoundCache.availableDriverNames.Contains(usableName))
                            {
                                messages.Add(usableName);
                                canBePlayed = true;
                            }
                            else if (usableName != null && usableName.Count() > 0 && AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER 
                                && (!hasAlternative || AudioPlayer.ttsOption == AudioPlayer.TTS_OPTION.ANY_TIME))
                            {
                                messages.Add(SoundCache.TTS_IDENTIFIER + usableName);
                                canBePlayed = true;
                            }
                            else
                            {
                                Console.WriteLine("Message " + this.messageName + " can't be played because there is no sound for opponent name " + usableName);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Message " + this.messageName + " can't be played because the opponent is null or unusable");
                        }
                        break;
                    case FragmentType.Integer:                        
                        if (numberReader != null)
                        {
                            List<String> integerFolders = numberReader.GetIntegerSounds(messageFragment.integer, messageFragment.allowShortHundreds, useMoreInflection);
                            if (integerFolders.Count() == 0)
                            {
                                Console.WriteLine("Message " + this.messageName + " can't be played because the number reader found no sounds for number " + messageFragment.integer);
                                canBePlayed = false;
                                break;
                            }
                            else
                            {
                                foreach (String integerFolder in integerFolders)
                                {
                                    if (!integerFolder.StartsWith(AudioPlayer.PAUSE_ID) && !SoundCache.availableSounds.Contains(integerFolder))
                                    {
                                        Console.WriteLine("Message " + this.messageName + " can't be played because there is no sound for number fragment " + integerFolder);
                                        canBePlayed = false;
                                        break;
                                    }
                                }
                            }
                            messages.AddRange(integerFolders);
                        }
                        else
                        {
                            Console.WriteLine("Message " + this.messageName + " can't be played because the number reader is not available"); 
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
