using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.rFactor2.rFactor2Data;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
//using static CrewChiefV4.rFactor2.rFactor2Constants;

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

        private rF2VehScoringInfo getVehicleInfo(rF2State shared)
        {
            foreach (var vehicle in shared.mVehicles)
            {
                // TOOD: CHECK this out
                if (vehicle.mIsPlayer == 1)
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

            rF2State lastState = ((CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)lastStateObj).state;
            rF2State currentState = ((CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)currentStateObj).state;
            
            if (!enabled || currentState.mCurrentET < timeAfterRaceStartToActivate || currentState.mInRealtime == 0 || 
                (currentState.mNumVehicles <= 2))
            {
                return;
            }

            DateTime now = DateTime.Now;
            rF2VehScoringInfo currentPlayerData;
            rF2VehScoringInfo previousPlayerData;
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
            // TODO: rf2 is in doubles.
            float[] currentPlayerPosition = new float[] { (float) currentPlayerData.mPos.x, (float) currentPlayerData.mPos.z };

            if (currentPlayerData.mInPits == 0 && currentPlayerData.mControl == (int)rFactor2Constants.rF2Control.Player && 
                // turn off spotter for formation lap before going green
                !(currentState.mGamePhase == (int)rFactor2Constants.rF2GamePhase.Formation))
            {
                List<float[]> currentOpponentPositions = new List<float[]>();
                float[] playerVelocityData = new float[3];
                playerVelocityData[0] = (float)currentState.mSpeed;
                playerVelocityData[1] = ((float)currentPlayerData.mPos.x - (float)previousPlayerData.mPos.x) / timeDiffSeconds;
                playerVelocityData[2] = ((float)currentPlayerData.mPos.z - (float)previousPlayerData.mPos.z) / timeDiffSeconds;

                for (int i = 0; i < currentState.mNumVehicles; ++i)
                {
                    var vehicle = currentState.mVehicles[i];
                    if (vehicle.mIsPlayer == 1 || vehicle.mInPits == 1 || vehicle.mLapDist < 0)
                    {
                        continue;
                    }
                    currentOpponentPositions.Add(new float[] { (float)vehicle.mPos.x, (float)vehicle.mPos.z });
                }
                float playerRotation = (float)(Math.Atan2((double)(currentState.mOri[rFactor2Constants.RowZ].x), (double)(currentState.mOri[rFactor2Constants.RowZ].z)));                
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
