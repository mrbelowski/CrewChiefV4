using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.rFactor2.rFactor2Data;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;

namespace CrewChiefV4.rFactor2
{
    class RF2Spotter : Spotter
    {
        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;
        
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("rf2_spotter_car_length");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enabled;

        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private float carWidth = 1.8f;

        private DateTime previousTime = DateTime.Now;

        public RF2Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
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

        private rf2VehicleInfo getVehicleInfo(rf2Shared shared)
        {
            foreach (rf2VehicleInfo vehicle in shared.vehicle)
            {
                if (vehicle.isPlayer == 1)
                {
                    return vehicle;
                }
            }
            throw new Exception("no vehicle for player!");
        }

        public void trigger(Object lastStateObj, Object currentStateObj)
        {
            if (paused)
            {
                return;
            }

            rf2Shared lastState = ((CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)lastStateObj).data;
            rf2Shared currentState = ((CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)currentStateObj).data;
            
            if (!enabled || currentState.currentET < timeAfterRaceStartToActivate || currentState.inRealtime == 0 || 
                (currentState.numVehicles <= 2))
            {
                return;
            }

            DateTime now = DateTime.Now;
            rf2VehicleInfo currentPlayerData;
            rf2VehicleInfo previousPlayerData;
            float timeDiffSeconds;
            try
            {
                currentPlayerData = getVehicleInfo(currentState);
                previousPlayerData = getVehicleInfo(lastState);
                timeDiffSeconds = ((float)(now - previousTime).TotalMilliseconds) / 1000f;
                previousTime = now;
                if (timeDiffSeconds <= 0)
                {
                    // WTF?
                    return;
                }
            }
            catch (Exception)
            {
                return;
            }
            float[] currentPlayerPosition = new float[] { currentPlayerData.pos.x, currentPlayerData.pos.z };

            if (currentPlayerData.inPits == 0 && currentPlayerData.control == (int)rFactor2Constant.rf2Control.player && 
                // turn off spotter for formation lap before going green
                !(currentState.gamePhase == (int)rFactor2Constant.rf2GamePhase.formation))
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = currentState.speed;
                playerVelocityData[1] = (currentPlayerData.pos.x - previousPlayerData.pos.x) / timeDiffSeconds;
                playerVelocityData[2] = (currentPlayerData.pos.z - previousPlayerData.pos.z) / timeDiffSeconds;

                for (int i = 0; i < currentState.numVehicles; i++)
                {
                    rf2VehicleInfo vehicle = currentState.vehicle[i];
                    if (vehicle.isPlayer == 1 || vehicle.inPits == 1 || vehicle.lapDist < 0)
                    {
                        continue;
                    }
                    currentOpponentPositions.Add(new float[] { vehicle.pos.x, vehicle.pos.z });
                }
                float playerRotation = (float)(Math.Atan2((double)(currentState.oriZ.x), (double)(currentState.oriZ.z)));                
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
