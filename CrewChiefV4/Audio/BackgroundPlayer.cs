using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    abstract class BackgroundPlayer
    {
        protected Boolean muted = false;
        protected String backgroundFilesPath;
        protected String defaultBackgroundSound;
        protected Boolean initialised = false;
        protected int backgroundLeadout = 30;

        public abstract void mute(bool doMute);

        public abstract void play();

        public abstract void stop();

        public abstract void initialise(String initialBackgroundSound);

        public virtual void dispose()
        {

        }

        public abstract void setBackgroundSound(String backgroundSoundName);

        protected float getBackgroundVolume()
        {
            float volume = UserSettings.GetUserSettings().getFloat("background_volume");
            if (volume > 1)
            {
                volume = 1;
            }
            if (volume < 0)
            {
                volume = 0;
            }
            return volume;
        }
    }
}
