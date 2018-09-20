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
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength =  UserSettings.GetUserSettings().getFloat("acs_spotter_car_length");

        private float carWidth = 1.8f;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private DateTime previousTime = DateTime.UtcNow;

        private string currentPlayerCarClassID = "#not_set#";

        public ACSSpotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
            Console.WriteLine("ACSSpotter enable");
        }

        public override void clearState()
        {
            previousTime = DateTime.UtcNow;
            internalSpotter.clearState();
        }

        // For double-file manual rolling starts. Will only work when the cars are all nicely settled on the grid - preferably 
        // when the game thinks the race has just started
        public override Tuple<GridSide, Dictionary<int, GridSide>> getGridSide(Object currentStateObj)
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
        }

        protected override float[] getWorldPositionOfDriverAtPosition(Object currentStateObj, int position)
        {
            AssettoCorsaShared latestRawData = (AssettoCorsaShared)currentStateObj;
            foreach (acsVehicleInfo vehicleInfo in latestRawData.acsChief.vehicle)
            {
                if (vehicleInfo.carLeaderboardPosition == position)
                {
                    return new float[] { vehicleInfo.worldPosition.x, vehicleInfo.worldPosition.y };
                }
            }
            return new float[] { 0, 0 };
        }

        public float mapToFloatTime(int time)
        {
            TimeSpan ts = TimeSpan.FromTicks(time);
            return (float)ts.TotalMilliseconds * 10;
        }
        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if (paused)
            {
                return;
            }
            AssettoCorsaShared currentState = ((ACSSharedMemoryReader.ACSStructWrapper)currentStateObj).data;
            AssettoCorsaShared lastState = ((ACSSharedMemoryReader.ACSStructWrapper)lastStateObj).data;

            if (!enabled || currentState.acsChief.numVehicles <= 1 ||
                currentState.acsGraphic.status == AC_STATUS.AC_REPLAY || currentState.acsGraphic.status == AC_STATUS.AC_OFF ||
                (mapToFloatTime(currentState.acsChief.vehicle[0].currentLapTimeMS) < timeAfterRaceStartToActivate &&
                currentState.acsChief.vehicle[0].lapCount <= 0))
            { 
               return;  
            }
            DateTime now = DateTime.UtcNow;
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
                if (carClass != null && !String.Equals(currentPlayerCarClassID, carClass.getClassIdentifier()))
                {
                    // Retrieve and use user overridable spotter car length/width.
                    this.internalSpotter.setCarDimensions(GlobalBehaviourSettings.spotterVehicleLength, GlobalBehaviourSettings.spotterVehicleWidth);
                    this.currentPlayerCarClassID = carClass.getClassIdentifier();
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
    }
}