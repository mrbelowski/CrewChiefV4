using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using iRSDKSharp;

namespace CrewChiefV4.iRacing
{

    class iRacingSpotter : Spotter
    {

        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enabled;

        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private DateTime previousTime = DateTime.Now;


        public iRacingSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, 0, 0);
        }

        public void clearState()
        {
            previousTime = DateTime.Now;
            internalSpotter.clearState();
        }

        public void pause()
        {
            paused = true;
        }

        public void unpause()
        {
            paused = false;
        }

        public void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if(enabled)
            {
                int currentState = (int)currentStateObj;
                internalSpotter.triggerInternal((int)currentState);
            }
            return;
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderEnableSpotter, 0, null));

        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playMessageImmediately(new QueuedMessage(NoisyCartesianCoordinateSpotter.folderDisableSpotter, 0, null));

        }
    }

}