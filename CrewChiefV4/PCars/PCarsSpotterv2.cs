using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.Events;

namespace CrewChiefV4.PCars
{
    class PCarsSpotterv2 : Spotter
    {
        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;

        private Boolean checkCarSpeedsForNetworkSpotter = true;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        private float carLength = UserSettings.GetUserSettings().getFloat("pcars_spotter_car_length");
        
        private Boolean enabled;

        private Boolean initialEnabledState;
        
        private AudioPlayer audioPlayer;

        private DateTime timeToStartSpotting = DateTime.Now;

        private Dictionary<String, List<float>> previousOpponentSpeeds = new Dictionary<String, List<float>>();

        private float maxClosingSpeed = 30;

        private float trackWidth = 10f;

        private float carWidth = 1.8f;

        private float udpMaxClosingSpeed = 60;

        private float udpTrackWidth = 7f;

        private float udpCarWidth = 1.5f;

        public PCarsSpotterv2(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
            {
                this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, udpCarWidth, udpTrackWidth, udpMaxClosingSpeed);
            }
            else
            {
                this.internalSpotter = new NoisyCartesianCoordinateSpotter(audioPlayer, initialEnabledState, carLength, carWidth, trackWidth, maxClosingSpeed);
            }
        }

        public void clearState()
        {
            timeToStartSpotting = DateTime.Now;
            audioPlayer.closeChannel();
        }

        public void pause()
        {
            paused = true;
        }

        public void unpause()
        {
            paused = false;
        }

        public void trigger(Object lastStateObj, Object currentStateObj)
        {
            if (paused)
            {
                audioPlayer.closeChannel();
                return;
            }
            CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper currentWrapper = (CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)currentStateObj;
            pCarsAPIStruct currentState = currentWrapper.data;

            // game state is 3 for paused, 5 for replay. No idea what 4 is...
            if (currentState.mGameState == (uint)eGameState.GAME_FRONT_END ||
                (currentState.mGameState == (uint)eGameState.GAME_INGAME_PAUSED && !System.Diagnostics.Debugger.IsAttached) ||
                currentState.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY || currentState.mGameState == (uint)eGameState.GAME_EXITED)
            {
                // don't ignore the paused game updates if we're in debug mode
                audioPlayer.closeChannel();
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

            if (enabled && currentState.mParticipantData.Count() > 1)
            {
                Tuple<int, pCarsAPIParticipantStruct> playerDataWithIndex = PCarsGameStateMapper.getPlayerDataStruct(currentState.mParticipantData, currentState.mViewedParticipantIndex);
                int playerIndex = playerDataWithIndex.Item1;
                pCarsAPIParticipantStruct playerData = playerDataWithIndex.Item2;
                float[] currentPlayerPosition = new float[] { playerData.mWorldPosition[0], playerData.mWorldPosition[2] };

                if (currentState.mPitMode == (uint)ePitMode.PIT_MODE_NONE)
                {
                    List<float[]> currentOpponentPositions = new List<float[]>();
                    List<float> opponentSpeeds = new List<float>();
                    float playerSpeed = currentState.mSpeed;

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
                            try
                            {
                                pCarsAPIParticipantStruct previousOpponentData = PCarsGameStateMapper.getParticipantDataForName(lastState.mParticipantData, opponentData.mName, i);
                                float[] previousPositions = new float[] { previousOpponentData.mWorldPosition[0], previousOpponentData.mWorldPosition[2] };
                                float opponentSpeed;

                                // TODO: check that the network data are up to the task of calculating opponent speeds
                                if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK && !checkCarSpeedsForNetworkSpotter) 
                                {
                                    opponentSpeed = playerSpeed;
                                }
                                else 
                                {
                                    opponentSpeed = getSpeed(currentPositions, previousPositions, interval);
                                }
                                if (previousOpponentSpeeds.ContainsKey(opponentData.mName))
                                {
                                    List<float> speeds = previousOpponentSpeeds[opponentData.mName];
                                    speeds.Add(opponentSpeed);
                                    if (speeds.Count == 5)
                                    {
                                        speeds.RemoveAt(0);
                                        opponentSpeed = speeds.Average();
                                    }
                                }
                                else
                                {
                                    List<float> speeds = new List<float>();
                                    speeds.Add(opponentSpeed);
                                    previousOpponentSpeeds.Add(opponentData.mName, speeds);
                                }
                                opponentSpeeds.Add(opponentSpeed);
                            }
                            catch (Exception)
                            {
                                opponentSpeeds.Add(playerSpeed);
                            }
                        }
                    }
                    float playerRotation = currentState.mOrientation[1];
                    if (playerRotation < 0)
                    {
                        playerRotation = (float)(2 * Math.PI) + playerRotation;
                    }
                    playerRotation = (float)(2 * Math.PI) - playerRotation;
                    internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerSpeed, opponentSpeeds, currentOpponentPositions);
                }
            }
        }

        public void enableSpotter()
        {
            enabled = true;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null), false);
            audioPlayer.closeChannel();
        }

        public void disableSpotter()
        {
            enabled = false;
            audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null), false);
            audioPlayer.closeChannel();
        }

        private float getSpeed(float[] current, float[] previous, float timeInterval)
        {
            return (float)(Math.Sqrt(Math.Pow(current[0] - previous[0], 2) + Math.Pow(current[1] - previous[1], 2))) / timeInterval;
        }
    }
}
