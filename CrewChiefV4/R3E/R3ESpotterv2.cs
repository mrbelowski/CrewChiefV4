using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;

namespace CrewChiefV4.RaceRoom
{
    class R3ESpotterv2 : Spotter
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

        public R3ESpotterv2(AudioPlayer audioPlayer, Boolean initialEnabledState)
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

        private DriverData getDriverData(RaceRoomShared shared, int slot_id)
        {
            foreach (DriverData driverData in shared.DriverData)
            {
                if (driverData.DriverInfo.SlotId == slot_id)
                {
                    return driverData;
                }
            }
            throw new Exception("no driver data for slotID " + slot_id);
        }

        public void trigger(Object lastStateObj, Object currentStateObj)
        {
            if (paused)
            {
                return;
            }

            RaceRoomShared lastState = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)lastStateObj).data;
            RaceRoomShared currentState = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)currentStateObj).data;
            
            if (!enabled || currentState.Player.GameSimulationTime < timeAfterRaceStartToActivate ||
                currentState.ControlType != (int)RaceRoomConstant.Control.Player || 
                ((int)RaceRoomConstant.Session.Qualify == currentState.SessionType && (currentState.NumCars == 1 || currentState.NumCars == 2)))
            {
                return;
            }

            DateTime now = DateTime.Now;
            DriverData currentPlayerData;
            DriverData previousPlayerData;
            float timeDiffSeconds;
            try
            {
                currentPlayerData = getDriverData(currentState, currentState.VehicleInfo.SlotId);
                previousPlayerData = getDriverData(lastState, currentState.VehicleInfo.SlotId);
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
            float[] currentPlayerPosition = new float[] { currentPlayerData.Position.X, currentPlayerData.Position.Z };

            if (currentPlayerData.InPitlane == 0)
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = currentState.CarSpeed;
                playerVelocityData[1] = (currentPlayerData.Position.X - previousPlayerData.Position.X) / timeDiffSeconds;
                playerVelocityData[2] = (currentPlayerData.Position.Z - previousPlayerData.Position.Z) / timeDiffSeconds;

                foreach (DriverData driverData in currentState.DriverData)
                {
                    if (driverData.DriverInfo.SlotId == currentState.VehicleInfo.SlotId || driverData.DriverInfo.SlotId == -1 || driverData.InPitlane == 1)
                    {
                        continue;
                    }
                    currentOpponentPositions.Add(new float[] { driverData.Position.X, driverData.Position.Z });
                }
                float playerRotation = currentState.CarOrientation.Yaw;                
                if (playerRotation < 0)
                {
                    playerRotation = (float)(2 * Math.PI) + playerRotation;
                }
                playerRotation = (float)(2 * Math.PI) - playerRotation;
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
