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

    public enum SoundType {BEEP, SPOTTER, VOICE_COMMAND_RESPONSE, REGULAR_MESSAGE, IMPORTANT_MESSAGE, CRITICAL_MESSAGE}

    public class SoundMetadata
    {
        public int messageId = -1;
        public SoundType type;

        public static SoundMetadata beep = new SoundMetadata(SoundType.BEEP);
        public static SoundMetadata spotter = new SoundMetadata(SoundType.SPOTTER);

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
    }
}
