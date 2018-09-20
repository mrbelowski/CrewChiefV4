using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.rFactor1.rFactor1Data;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.rFactor1
{
    class RF1Spotter : Spotter
    {        
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("rf1_spotter_car_length");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");
        
        private float carWidth = 1.8f;

        private DateTime previousTime = DateTime.UtcNow;

        private string currentPlayerCarClassID = "#not_set#";

        public RF1Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
        }

        public override void clearState()
        {
            previousTime = DateTime.UtcNow;
            internalSpotter.clearState();
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

        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
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

            DateTime now = DateTime.UtcNow;
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

            if (currentGameState != null)
            {
                var carClass = currentGameState.carClass;
                if (carClass != null && !String.Equals(currentPlayerCarClassID, carClass.getClassIdentifier()))
                {
                    // Retrieve and use user overridable spotter car length/width.
                    this.internalSpotter.setCarDimensions(GlobalBehaviourSettings.spotterVehicleLength, GlobalBehaviourSettings.spotterVehicleWidth);
                    this.currentPlayerCarClassID = carClass.getClassIdentifier();
                }
            }
            float[] currentPlayerPosition = new float[] { currentPlayerData.pos.x, currentPlayerData.pos.z };

            if (currentPlayerData.inPits == 0 && currentPlayerData.control == (int)rFactor1Constant.rfControl.player && 
                // turn off spotter for formation lap before going green
                !(currentState.gamePhase == (int)rFactor1Constant.rfGamePhase.formation))
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
    }
}
