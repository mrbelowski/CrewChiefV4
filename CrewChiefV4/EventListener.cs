using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    public class EventListener
    {
        // reads data from server or other remote location. This is separate from the game state,
        // but is available to the mappers
        protected Boolean enabled = false;
        protected Boolean active = false;

        private AudioPlayer audioPlayer;

        // some events will trigger messages:
        public void setAudioPlayer(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public virtual Boolean autoStart()
        {
            return false;
        }

        public void enable()
        {
            this.enabled = true;
        }
        public void disable()
        {
            this.enabled = false;
        }

        public virtual void activate(Object activationData)
        {
            this.active = true;
        }

        // note that calling this method should put the listener in a state where it can be GC'ed
        public virtual void deactivate()
        {
            this.active = false;
        }
    }
}
