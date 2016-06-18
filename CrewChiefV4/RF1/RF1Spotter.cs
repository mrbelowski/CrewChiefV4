using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.rFactor1.rFactor1Data;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;

namespace CrewChiefV4.rFactor1
{
    class RF1Spotter : Spotter
    {
        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;
        
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("rf1_spotter_car_length");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enabled;

        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private float carWidth = 1.8f;

        private DateTime previousTime = DateTime.Now;

        public RF1Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
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

        private rfVehicleInfo getVehicleInfo(rfShared shared)
        {
            foreach (rfVehicleInfo vehicle in shared.vehicle)
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

            rfShared lastState = ((CrewChiefV4.rFactor1.RF1SharedMemoryReader.RF1StructWrapper)lastStateObj).data;
            rfShared currentState = ((CrewChiefV4.rFactor1.RF1SharedMemoryReader.RF1StructWrapper)currentStateObj).data;
            
            if (!enabled || currentState.currentET < timeAfterRaceStartToActivate || currentState.inRealtime == 0 || 
                (currentState.numVehicles <= 2))
            {
                return;
            }

            DateTime now = DateTime.Now;
            rfVehicleInfo currentPlayerData;
            rfVehicleInfo previousPlayerData;
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

            if (currentPlayerData.inPits == 0 && currentPlayerData.control == (int)rFactor1Constant.rfControl.player)
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = currentState.speed;
                playerVelocityData[1] = (currentPlayerData.pos.x - previousPlayerData.pos.x) / timeDiffSeconds;
                playerVelocityData[2] = (currentPlayerData.pos.z - previousPlayerData.pos.z) / timeDiffSeconds;

                for (int i = 0; i < currentState.numVehicles; i++)
                {
                    rfVehicleInfo vehicle = currentState.vehicle[i];
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
