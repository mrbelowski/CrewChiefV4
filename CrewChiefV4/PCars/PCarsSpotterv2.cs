using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

namespace CrewChiefV4.PCars
{
    class PCarsSpotterv2 : Spotter
    {

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private Boolean enableSpotterInTimetrial = UserSettings.GetUserSettings().getBoolean("enable_spotter_in_timetrial");

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("pcars_spotter_car_length");

        private DateTime timeToStartSpotting = DateTime.UtcNow;

        private Dictionary<String, List<float>> previousOpponentSpeeds = new Dictionary<String, List<float>>();

        private float carWidth = 1.8f;

        // User set class preferences will override this, might need special handling.
        private float udpCarWidth = 1.5f;

        private string currentPlayerCarClassID = "#not_set#";

        public PCarsSpotterv2(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
            {
                this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, udpCarWidth);
            }
            else
            {
                this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth);
            }
        }

        public override void clearState()
        {
            timeToStartSpotting = DateTime.UtcNow;
            internalSpotter.clearState();
        }

        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if (paused)
            {
                return;
            }
            CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper currentWrapper = (CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)currentStateObj;
            pCarsAPIStruct currentState = currentWrapper.data;

            // game state is 3 for paused, 5 for replay. No idea what 4 is...
            if (currentState.mGameState == (uint)eGameState.GAME_FRONT_END ||
                (currentState.mGameState == (uint)eGameState.GAME_INGAME_PAUSED && !CrewChief.Debugging) ||
                currentState.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY || currentState.mGameState == (uint)eGameState.GAME_EXITED)
            {
                // don't ignore the paused game updates if we're in debug mode                
                return;
            }
            CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper previousWrapper = (CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)lastStateObj;
            pCarsAPIStruct lastState = previousWrapper.data;

            DateTime now = new DateTime(currentWrapper.ticksWhenRead);
            float interval = (float)(((double)currentWrapper.ticksWhenRead - (double)previousWrapper.ticksWhenRead) / (double)TimeSpan.TicksPerSecond);
            if (currentState.mRaceState == (int)eRaceState.RACESTATE_RACING &&
                lastState.mRaceState != (int)eRaceState.RACESTATE_RACING)
            {
                timeToStartSpotting = now.Add(TimeSpan.FromSeconds(timeAfterRaceStartToActivate));
            }
            // this check looks a bit funky... whe we start a practice session, the raceState is not_started
            // until we cross the line for the first time. Which is retarded really.
            if (currentState.mRaceState == (int)eRaceState.RACESTATE_INVALID || now < timeToStartSpotting ||
                (currentState.mSessionState == (int)eSessionState.SESSION_RACE && currentState.mRaceState == (int) eRaceState.RACESTATE_NOT_STARTED))
            {
                return;
            }

            if (enabled && currentState.mNumParticipants > 1 && 
                (enableSpotterInTimetrial || currentState.mSessionState != (uint)eSessionState.SESSION_TIME_ATTACK))
            {
                Tuple<int, pCarsAPIParticipantStruct> playerDataWithIndex = PCarsGameStateMapper.getPlayerDataStruct(currentState.mParticipantData, currentState.mViewedParticipantIndex);
                int playerIndex = playerDataWithIndex.Item1;
                pCarsAPIParticipantStruct playerData = playerDataWithIndex.Item2;
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
                float[] currentPlayerPosition = new float[] { playerData.mWorldPosition[0], playerData.mWorldPosition[2] };

                if (currentState.mPitMode == (uint)ePitMode.PIT_MODE_NONE)
                {
                    List<float[]> currentOpponentPositions = new List<float[]>();
                    float[] playerVelocityData = new float[3];
                    playerVelocityData[0] = currentState.mSpeed;
                    playerVelocityData[1] = currentState.mWorldVelocity[0];
                    playerVelocityData[2] = currentState.mWorldVelocity[2];

                    for (int i = 0; i < currentState.mParticipantData.Count(); i++)
                    {
                        if (i == playerIndex)
                        {
                            continue;
                        }
                        pCarsAPIParticipantStruct opponentData = currentState.mParticipantData[i];
                        if (opponentData.mIsActive)
                        {
                            float[] currentPositions = new float[] { opponentData.mWorldPosition[0], opponentData.mWorldPosition[2] };
                            currentOpponentPositions.Add(currentPositions);
                        }
                    }
                    if (currentOpponentPositions.Count() > 0)
                    {
                        float playerRotation = currentState.mOrientation[1];
                        if (playerRotation < 0)
                        {
                            playerRotation = (float)(2 * Math.PI) + playerRotation;
                        }
                        playerRotation = (float)(2 * Math.PI) - playerRotation;
                        internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
                    }
                }
            }
        }

        private float getSpeed(float[] current, float[] previous, float timeInterval)
        {
            return (float)(Math.Sqrt(Math.Pow(current[0] - previous[0], 2) + Math.Pow(current[1] - previous[1], 2))) / timeInterval;
        }
    }
}
