using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.assetto.assettoData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.assetto
{
    class ACSSpotter : Spotter
    {
        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength =  UserSettings.GetUserSettings().getFloat("acs_spotter_car_length");

        private float carWidth = 1.8f;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enabled;

        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private DateTime previousTime = DateTime.Now;

        private string currentPlayerCarClassID = "#not_set#";

        public ACSSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
            Console.WriteLine("ACSSpotter enable");
        }

        public void clearState()
        {

            previousTime = DateTime.Now;
        }

        public void pause()
        {
            paused = true;
        }

        public void unpause()
        {
            paused = false;
        }
        public float mapToFloatTime(int time)
        {
            TimeSpan ts = TimeSpan.FromTicks(time);
            return (float)ts.TotalMilliseconds * 10;
        }
        public void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {

            if (paused)
            {
                return;
            }
            AssettoCorsaShared lastState = ((ACSSharedMemoryReader.ACSStructWrapper)lastStateObj).data;
            AssettoCorsaShared currentState = ((ACSSharedMemoryReader.ACSStructWrapper)currentStateObj).data;

            if (!enabled || currentState.acsChief.numVehicles <= 1 || 
                (mapToFloatTime(currentState.acsChief.vehicle[0].currentLapTimeMS) < timeAfterRaceStartToActivate &&
                currentState.acsChief.vehicle[0].lapCount <= 0))
            { 
               return;  
            }
            DateTime now = DateTime.Now;
            acsVehicleInfo currentPlayerData;
            acsVehicleInfo previousPlayerData;
            float timeDiffSeconds;
            try
            {
                currentPlayerData = currentState.acsChief.vehicle[0];
                previousPlayerData = lastState.acsChief.vehicle[0];
                timeDiffSeconds = ((float)(now - previousTime).TotalMilliseconds) / 1000f;
                previousTime = now;
                if (timeDiffSeconds <= 0)
                {
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
            if (currentGameState != null)
            {
                var carClass = currentGameState.carClass;
                if (carClass != null && currentPlayerCarClassID != carClass.getClassIdentifier())
                {
                    // Retrieve and use user overridable spotter car length/width.
                    currentPlayerCarClassID = carClass.getClassIdentifier();
                    var preferences = carClass.getPreferences();
                    this.internalSpotter.setCarDimensions(preferences.spotterVehicleLength, preferences.spotterVehicleWidth);
                }
            }
            float[] currentPlayerPosition = new float[] { currentPlayerData.worldPosition.x, currentPlayerData.worldPosition.z };

            if (currentPlayerData.isCarInPitline == 0 || currentPlayerData.isCarInPit == 0)
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = currentPlayerData.speedMS;
                playerVelocityData[1] = (currentPlayerData.worldPosition.x - previousPlayerData.worldPosition.x) / timeDiffSeconds;
                playerVelocityData[2] = (currentPlayerData.worldPosition.z - previousPlayerData.worldPosition.z) / timeDiffSeconds;

                for (int i = 0; i < currentState.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo vehicle = currentState.acsChief.vehicle[i];
                    if (vehicle.carId == 0 || vehicle.isCarInPit == 1 || vehicle.isCarInPitline == 1 || vehicle.isConnected != 1)
                    {
                        continue;
                    }
                    currentOpponentPositions.Add(new float[] { vehicle.worldPosition.x, vehicle.worldPosition.z });
                }
                float playerRotation = currentState.acsPhysics.heading;
                
                if (playerRotation < 0)
                {
                    playerRotation = (float)(2 * Math.PI) + playerRotation;
                }
                internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
            }
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null));
        }
        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null));

        }
    }
}