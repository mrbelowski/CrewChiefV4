using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.rFactor2.rFactor2Data;
using System.Threading;
using CrewChiefV4.Events;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;

// When team moves to VS2015 or newer.
//using static CrewChiefV4.rFactor2.rFactor2Constants;

namespace CrewChiefV4.rFactor2
{
    class RF2Spotter : Spotter
    {
        // how long is a car? we use 3.5 meters by default here. Too long and we'll get 'hold your line' messages
        // when we're clearly directly behind the car
        // Note: both below variables can be overrided in car class.
        private readonly float carLength = UserSettings.GetUserSettings().getFloat("rf2_spotter_car_length");
        private readonly float carWidth = 1.8f;

        // don't activate the spotter unless this many seconds have elapsed (race starts are messy)
        private readonly int timeAfterRaceStartToActivate = UserSettings.GetUserSettings().getInt("time_after_race_start_for_spotter");

        private NoisyCartesianCoordinateSpotter internalSpotter;

        private Boolean paused = false;
        private Boolean enabled;
        private Boolean initialEnabledState;

        private AudioPlayer audioPlayer;

        private DateTime previousTime = DateTime.Now;
        private string currentPlayerCarClassID = "#not_set#";

        public RF2Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(
                audioPlayer, initialEnabledState, carLength, carWidth);
        }

        public void clearState()
        {            
            this.previousTime = DateTime.Now;
        }

        public void pause()
        {
            this.paused = true;
        }

        public void unpause()
        {
            this.paused = false;
        }

        private rF2VehScoringInfo getVehicleInfo(rF2State shared)
        {
            for (int i = 0; i < shared.mNumVehicles; ++i)
            {
                var vehicle = shared.mVehicles[i];
                if (vehicle.mIsPlayer == 1)
                {
                    return vehicle;
                }
            }
            throw new Exception("no vehicle for player!");
        }

        public void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if (this.paused)
            {
                return;
            }

            var lastState = ((CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)lastStateObj).state;
            var currentState = ((CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)currentStateObj).state;
            
            if (!this.enabled 
                || currentState.mCurrentET < this.timeAfterRaceStartToActivate
                || currentState.mInRealtimeFC == 0
                || lastState.mInRealtimeFC == 0
                || currentState.mNumVehicles <= 2)
                return;

            var now = DateTime.Now;
            rF2VehScoringInfo currentPlayerData;
            rF2VehScoringInfo previousPlayerData;
            float timeDiffSeconds;
            try
            {
                currentPlayerData = getVehicleInfo(currentState);
                previousPlayerData = getVehicleInfo(lastState);
                timeDiffSeconds = ((float)(now - this.previousTime).TotalMilliseconds) / 1000.0f;
                this.previousTime = now;

                if (timeDiffSeconds <= 0.0f)
                {
                    // In pits probably.
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
                if (carClass != null && currentPlayerCarClassID != carClass.getClassIdentifier())
                {
                    // Retrieve and use user overridable spotter car length/width.
                    currentPlayerCarClassID = carClass.getClassIdentifier();
                    var preferences = carClass.getPreferences();
                    this.internalSpotter.setCarDimensions(preferences.spotterVehicleLength, preferences.spotterVehicleWidth);
                }
            }

            var currentPlayerPosition = new float[] { (float) currentPlayerData.mPos.x, (float) currentPlayerData.mPos.z };

            if (currentPlayerData.mInPits == 0
                && currentPlayerData.mControl == (int)rFactor2Constants.rF2Control.Player
                && !(currentState.mGamePhase == (int)rFactor2Constants.rF2GamePhase.Formation))  // turn off spotter for formation lap before going green
            {
                var currentOpponentPositions = new List<float[]>();
                var playerVelocityData = new float[3];
                playerVelocityData[0] = (float)currentState.mSpeed;
                playerVelocityData[1] = ((float)currentPlayerData.mPos.x - (float)previousPlayerData.mPos.x) / timeDiffSeconds;
                playerVelocityData[2] = ((float)currentPlayerData.mPos.z - (float)previousPlayerData.mPos.z) / timeDiffSeconds;

                for (int i = 0; i < currentState.mNumVehicles; ++i)
                {
                    var vehicle = currentState.mVehicles[i];
                    if (vehicle.mIsPlayer == 1 || vehicle.mInPits == 1 || vehicle.mLapDist < 0.0f)
                        continue;

                    currentOpponentPositions.Add(new float[] { (float)vehicle.mPos.x, (float)vehicle.mPos.z });
                }

                float playerRotation = (float)(Math.Atan2((double)(currentState.mOri[rFactor2Constants.RowZ].x), (double)(currentState.mOri[rFactor2Constants.RowZ].z)));
                if (playerRotation < 0)
                    playerRotation = (float)(2 * Math.PI) + playerRotation;

                this.internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
            }
        }

        public void enableSpotter()
        {
            this.enabled = true;
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null));
            
        }

        public void disableSpotter()
        {
            this.enabled = false;
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null));
        }
    }
}
