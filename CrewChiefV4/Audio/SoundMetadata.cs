using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    // type of sound, in order of importance. OTHER is used for beeps. The ordering here
    // determines whether the presence of a sound can (if it's in the immediate queue) prevent
    // regular queued messages from playing.
    public enum SoundType {
        SPOTTER = 0,    // most important
        CRITICAL_MESSAGE,
        VOICE_COMMAND_RESPONSE, 
        IMPORTANT_MESSAGE,
        REGULAR_MESSAGE,
        AUTO,        // allow the context (spotter, immediate, regular) to determine the type
        OTHER           // used only for beeps (do we need this?)
    }

    public enum MinPriorityForInterrupt
    {
        NEVER = 0,
        SPOTTER_MESSAGES,
        CRITICAL_MESSAGES,
        IMPORTANT_MESSAGES
    }

    public class SoundMetadata
    {
        public const int DEFAULT_PRIORITY = 5;
        public int messageId = 0;  // 0 => unset
        public SoundType type;

        // this affects the queue insertion order. Higher priority items are inserted at the head of the queue
        public int priority = DEFAULT_PRIORITY;  // 0 = lowest, 5 = default, 10 = spotter

        // this has no messageId or priority because they're only ever single sounds
        public static SoundMetadata beep = new SoundMetadata(SoundType.OTHER);

        public SoundMetadata()
        {

        }

        public SoundMetadata(SoundType type)
        {
            this.type = type;
        }

        public SoundMetadata(SoundType type, int priority)
        {
            this.type = type;
            this.priority = priority;
        }
    }
}
