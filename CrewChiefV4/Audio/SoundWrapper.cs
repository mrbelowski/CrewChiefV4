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
        public int priority = 5;  // 0 = lowest, 5 = default, 10 = spotter

        // this has no messageId or priority because they're only ever single sounds
        public static SoundMetadata beep = new SoundMetadata(SoundType.BEEP);
        
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
