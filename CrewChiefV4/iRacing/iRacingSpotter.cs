using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using iRacingSDK;
namespace CrewChiefV4.iRacing
{
    class iRacingSpotter : Spotter
    {

        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("r3e_spotter_car_length");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enabled;

        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private float carWidth = 1.8f;

        private DateTime previousTime = DateTime.Now;


        public iRacingSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
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
            DataSample lastState = ((CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)lastStateObj).data;
            DataSample currentState = ((CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)currentStateObj).data;
            if(!currentState.IsConnected)
            {
                return;
            }
            this.internalSpotter.setCarDimensions(GlobalBehaviourSettings.spotterVehicleLength, GlobalBehaviourSettings.spotterVehicleWidth);
            internalSpotter.triggerInternal((int)currentState.Telemetry.CarLeftRight);
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
