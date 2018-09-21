using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.RaceRoom
{
    class R3ESpotterv2 : Spotter
    {
        private float twoPi = (float)(2 * Math.PI);

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("r3e_spotter_car_length");

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");
        
        private float carWidth = 1.8f;

        private DateTime previousTime = DateTime.UtcNow;

        private string currentPlayerCarClassID = "#not_set#";

        private HashSet<int> positionsFilledForThisTick = new HashSet<int>();

        public R3ESpotterv2(AudioPlayer audioPlayer, Boolean initialEnabledState)
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

        // For double-file manual rolling starts. Will only work when the cars are all nicely settled on the grid - preferably 
        // when the game thinks the race has just started
        public override Tuple<GridSide, Dictionary<int, GridSide>> getGridSide(Object currentStateObj)
        {
            RaceRoomShared latestRawData = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)currentStateObj).data;
            DriverData playerData = getDriverData(latestRawData, latestRawData.VehicleInfo.SlotId);
            float playerRotation = latestRawData.CarOrientation.Yaw;                
            if (playerRotation < 0)
            {
                playerRotation = (float)(2 * Math.PI) + playerRotation;
            }
            playerRotation = (float)(2 * Math.PI) - playerRotation;
            float playerXPosition = playerData.Position.X;
            float playerZPosition = playerData.Position.Z;
            int playerStartingPosition = latestRawData.Position;
            int numCars = latestRawData.NumCars;
            return getGridSideInternal(latestRawData, playerRotation, playerXPosition, playerZPosition, playerStartingPosition, numCars);
        }

        protected override float[] getWorldPositionOfDriverAtPosition(Object currentStateObj, int position)
        {
            RaceRoomShared latestRawData = (RaceRoomShared)currentStateObj;
            foreach (DriverData driverData in latestRawData.DriverData)
            {
                if (driverData.Place == position)
                {
                    return new float[] {driverData.Position.X, driverData.Position.Z};
                }
            }
            return new float[]{0, 0};
        }
        
        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if (paused)
            {
                return;
            }
            RaceRoomShared currentState = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)currentStateObj).data;
            RaceRoomShared lastState = ((CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)lastStateObj).data;

            if (!enabled || currentState.Player.GameSimulationTime < timeAfterRaceStartToActivate ||
                currentState.ControlType != (int)RaceRoomConstant.Control.Player || 
                currentGameState.SessionData.SessionType == SessionType.HotLap || 
                currentGameState.SessionData.SessionType == SessionType.LonePractice)
            {
                return;
            }

            DateTime now = DateTime.UtcNow;
            DriverData currentPlayerData;
            DriverData previousPlayerData;
            float timeDiffSeconds;
            try
            {
                currentPlayerData = getDriverData(currentState, currentState.VehicleInfo.SlotId);
                previousPlayerData = getDriverData(lastState, currentState.VehicleInfo.SlotId);
                timeDiffSeconds = (float)(now - previousTime).TotalSeconds;
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
            float[] currentPlayerPosition = new float[] { currentPlayerData.Position.X, currentPlayerData.Position.Z };

            if (currentPlayerData.InPitlane == 0)
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = currentState.CarSpeed;
                playerVelocityData[1] = (currentPlayerData.Position.X - previousPlayerData.Position.X) / timeDiffSeconds;
                playerVelocityData[2] = (currentPlayerData.Position.Z - previousPlayerData.Position.Z) / timeDiffSeconds;

                positionsFilledForThisTick.Clear();
                positionsFilledForThisTick.Add(currentPlayerData.Place);
                foreach (DriverData driverData in currentState.DriverData)
                {
                    if (driverData.DriverInfo.SlotId == currentState.VehicleInfo.SlotId || driverData.DriverInfo.SlotId == -1 || driverData.InPitlane == 1 ||
                        positionsFilledForThisTick.Contains(driverData.Place))
                    {
                        continue;
                    }
                    positionsFilledForThisTick.Add(driverData.Place);
                    currentOpponentPositions.Add(new float[] { driverData.Position.X, driverData.Position.Z });
                }
                float playerRotation = currentState.CarOrientation.Yaw;
                if (playerRotation < 0)
                {
                    playerRotation = playerRotation * -1;
                }
                else
                {
                    playerRotation = twoPi - playerRotation;
                }
                internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
            }
        }
    }
}
