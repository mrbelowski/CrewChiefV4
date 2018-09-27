using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.ACC.Data;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.ACC
{
    class ACCSpotter : Spotter
    {
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("acc_spotter_car_length");

        private float carWidth = 1.8f;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private DateTime previousTime = DateTime.UtcNow;

        private string currentPlayerCarClassID = "#not_set#";

        public ACCSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
            Console.WriteLine("ACCSpotter enable");
        }

        public override void clearState()
        {
            previousTime = DateTime.UtcNow;
            internalSpotter.clearState();
        }

        // For double-file manual rolling starts. Will only work when the cars are all nicely settled on the grid - preferably 
        // when the game thinks the race has just started
        /*public override Tuple<GridSide, Dictionary<int, GridSide>> getGridSide(Object currentStateObj)
        {
            AssettoCorsaShared latestRawData = ((ACSSharedMemoryReader.ACSStructWrapper)currentStateObj).data;
            acsVehicleInfo playerData = latestRawData.acsChief.vehicle[0];
            float playerRotation = latestRawData.acsPhysics.heading;
            if (playerRotation < 0)
            {
                playerRotation = (float)(2 * Math.PI) + playerRotation;
            }
            playerRotation = (float)(2 * Math.PI) - playerRotation;
            float playerXPosition = playerData.worldPosition.x;
            float playerZPosition = playerData.worldPosition.y;
            int playerStartingPosition = playerData.carLeaderboardPosition;
            int numCars = latestRawData.acsChief.numVehicles;
            return getGridSideInternal(latestRawData, playerRotation, playerXPosition, playerZPosition, playerStartingPosition, numCars);
        }*/

        protected override float[] getWorldPositionOfDriverAtPosition(Object currentStateObj, int position)
        {
            ACCSharedMemoryData latestRawData = (ACCSharedMemoryData)currentStateObj;
            foreach (Driver vehicleInfo in latestRawData.opponentDrivers)
            {
                if (vehicleInfo.realTimePosition == position)
                {
                    return new float[] { vehicleInfo.location.x, vehicleInfo.location.y };
                }
            }
            return new float[] { 0, 0 };
        }
        public double ConvertToRadians(double angle)
        {
            return (Math.PI / 180) * angle;
        }
        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if (paused)
            {
                return;
            }
            ACCSharedMemoryData currentState = ((ACCSharedMemoryReader.ACCStructWrapper)currentStateObj).data;
            ACCSharedMemoryData lastState = ((ACCSharedMemoryReader.ACCStructWrapper)lastStateObj).data;

            if (currentState.isReady != 1|| !enabled || !(currentState.sessionData.currentSessionPhase == RaceSessionPhase.SessionTime ||
                currentState.sessionData.currentSessionPhase == RaceSessionPhase.SessionOverTime) ||
                (currentState.sessionData.currentSessionType == RaceSessionType.Hotlap ||
                currentState.sessionData.currentSessionType == RaceSessionType.Hotstint))
            {
                return;
            }
            DateTime now = DateTime.UtcNow;
            Driver currentPlayerData;
            Driver previousPlayerData;
            float timeDiffSeconds;
            try
            {
                currentPlayerData = currentState.playerDriver;
                previousPlayerData = lastState.playerDriver;
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
                if (carClass != null && !String.Equals(currentPlayerCarClassID, carClass.getClassIdentifier()))
                {
                    // Retrieve and use user overridable spotter car length/width.
                    this.internalSpotter.setCarDimensions(GlobalBehaviourSettings.spotterVehicleLength, GlobalBehaviourSettings.spotterVehicleWidth);
                    this.currentPlayerCarClassID = carClass.getClassIdentifier();
                }
            }
            float[] currentPlayerPosition = new float[] { currentPlayerData.location.x, currentPlayerData.location.y };

            List<float[]> currentOpponentPositions = new List<float[]>();
            float[] playerVelocityData = new float[3];
            playerVelocityData[0] = currentPlayerData.speedMS;
            playerVelocityData[1] = (currentPlayerData.location.x - previousPlayerData.location.x) / timeDiffSeconds;
            playerVelocityData[2] = (currentPlayerData.location.y - previousPlayerData.location.y) / timeDiffSeconds;
                                             
            for (int i = 1; i < currentState.opponentDriverCount; i++)
            {
                Driver vehicle = currentState.opponentDrivers[i];
                currentOpponentPositions.Add(new float[] { vehicle.location.x, vehicle.location.y });
            }
            float playerRadRotation = (float)ConvertToRadians(currentState.playerDriver.rotation.yaw);
            internalSpotter.triggerInternal(playerRadRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
            
        }
    }
}