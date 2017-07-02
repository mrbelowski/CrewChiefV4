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

        private rF2VehicleScoring getVehicleInfo(CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper shared)
        {
            for (int i = 0; i < shared.scoring.mScoringInfo.mNumVehicles; ++i)
            {
                var vehicle = shared.scoring.mVehicles[i];
                if (vehicle.mIsPlayer == 1)
                    return vehicle;
            }
            throw new Exception("no vehicle for player!");
        }

        public void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
            if (this.paused)
                return;

            var lastState = lastStateObj as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;
            var currentState = currentStateObj as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;

            if (!this.enabled 
                || currentState.scoring.mScoringInfo.mCurrentET < this.timeAfterRaceStartToActivate
                || currentState.extended.mInRealtimeFC == 0
                || currentState.scoring.mScoringInfo.mInRealtime == 0
                || lastState.extended.mInRealtimeFC == 0
                || lastState.scoring.mScoringInfo.mInRealtime == 0
                || currentState.scoring.mScoringInfo.mNumVehicles <= 2)
                return;

            var now = DateTime.Now;
            rF2VehicleScoring currentPlayerScoring;
            rF2VehicleScoring previousPlayerScoring;
            float timeDiffSeconds;
            try
            {
                currentPlayerScoring = getVehicleInfo(currentState);
                previousPlayerScoring = getVehicleInfo(lastState);
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
                if (carClass != null && !String.Equals(currentPlayerCarClassID, carClass.getClassIdentifier()))
                {
                    // Retrieve and use user overridable spotter car length/width.
                    this.internalSpotter.setCarDimensions(GlobalBehaviourSettings.spotterVehicleLength, GlobalBehaviourSettings.spotterVehicleWidth);
                }
            }

            // Find telemetry data for current player vehicle.
            var idsToTelIndicesMap = RF2GameStateMapper.getIdsToTelIndicesMap(ref currentState.telemetry);
            int playerTelIdx = -1;
            if (!idsToTelIndicesMap.TryGetValue(currentPlayerScoring.mID, out playerTelIdx))
            {
                // TODO: remove.
                Console.WriteLine("Couldn't find player telemetry entry for spotter");
                return;
            }

            var currentPlayerTelemetry = currentState.telemetry.mVehicles[playerTelIdx];

            // Find telemetry data for previous player vehicle.
            int previousPlayerTelIdx = -1;
            for (int i = 0; i < lastState.telemetry.mNumVehicles; ++i)
            {
                if (previousPlayerScoring.mID == lastState.telemetry.mVehicles[i].mID)
                {
                    previousPlayerTelIdx = i;
                    break;
                }
            }

            var previousPlayerTelemetry = lastState.telemetry.mVehicles[previousPlayerTelIdx];

            var currentPlayerPosition = new float[] { (float) currentPlayerTelemetry.mPos.x, (float) currentPlayerTelemetry.mPos.z };

            if (currentPlayerScoring.mInPits == 0
                && currentPlayerScoring.mControl == (int)rFactor2Constants.rF2Control.Player
                && !(currentState.scoring.mScoringInfo.mGamePhase == (int)rFactor2Constants.rF2GamePhase.Formation))  // turn off spotter for formation lap before going green
            {
                var currentOpponentPositions = new List<float[]>();
                var playerVelocityData = new float[3];

                playerVelocityData[0] = (float)Math.Sqrt((currentPlayerTelemetry.mLocalVel.x * currentPlayerTelemetry.mLocalVel.x)
                    + (currentPlayerTelemetry.mLocalVel.y * currentPlayerTelemetry.mLocalVel.y)
                    + (currentPlayerTelemetry.mLocalVel.z * currentPlayerTelemetry.mLocalVel.z));

                playerVelocityData[1] = ((float)currentPlayerTelemetry.mPos.x - (float)previousPlayerTelemetry.mPos.x) / timeDiffSeconds;
                playerVelocityData[2] = ((float)currentPlayerTelemetry.mPos.z - (float)previousPlayerTelemetry.mPos.z) / timeDiffSeconds;

                for (int i = 0; i < currentState.scoring.mScoringInfo.mNumVehicles; ++i)
                {
                    var vehicle = currentState.scoring.mVehicles[i];
                    if (vehicle.mIsPlayer == 1 || vehicle.mInPits == 1 || vehicle.mLapDist < 0.0f)
                        continue;

                    int opponentTelIdx = -1;
                    if (!idsToTelIndicesMap.TryGetValue(vehicle.mID, out opponentTelIdx))
                    {
                        // TODO: remove.
                        Console.WriteLine("Couldn't find opponent telemetry entry for spotter");
                        return;
                    }

                    var opponentTelemetry = currentState.telemetry.mVehicles[opponentTelIdx];

                    currentOpponentPositions.Add(new float[] { (float)opponentTelemetry.mPos.x, (float)opponentTelemetry.mPos.z });
                }

                float playerRotation = (float)(Math.Atan2(currentPlayerTelemetry.mOri[rFactor2Constants.RowZ].x, currentPlayerTelemetry.mOri[rFactor2Constants.RowZ].z));
                if (playerRotation < 0)
                    playerRotation = (float)(2 * Math.PI) + playerRotation;

                this.internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);
            }
        }

        public void enableSpotter()
        {
            this.enabled = true;
            this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderEnableSpotter, 0, null));
        }

        public void disableSpotter()
        {
            this.enabled = false;
            this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDisableSpotter, 0, null));
        }
    }
}
