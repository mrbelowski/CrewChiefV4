using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    public class SoundWrapper
    {
        public SingleSound singleSound;
        public SoundMetadata soundMetadata;
        public SoundWrapper(SingleSound singleSound, SoundMetadata soundMetadata)
        {
            this.singleSound = singleSound;
            this.soundMetadata = soundMetadata;
        }
    }

    public enum SoundType {
        BEEP,
        SPOTTER,
        VOICE_COMMAND_RESPONSE, 
        CRITICAL_MESSAGE,
        IMPORTANT_MESSAGE,
        REGULAR_MESSAGE
    }

    public class SoundMetadata
    {
        public int messageId = -1;  // -1 => unset
        public SoundType type;
        public int priority = 0;  // 0 => lowest priority

        // these have no messageId
        public static SoundMetadata beep()
        {
            return new SoundMetadata(SoundType.BEEP, 100);
        }

        public SoundMetadata()
        {
            this.messageId = -1;
            this.type = SoundType.REGULAR_MESSAGE;
        }

        public SoundMetadata(int messageId, SoundType type)
        {
            this.type = type;
            this.messageId = messageId;
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
