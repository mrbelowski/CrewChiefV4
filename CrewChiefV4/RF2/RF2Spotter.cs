//#define TRACE_SPOTTER_ELAPSED_TIME

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

        private DateTime previousTime = DateTime.UtcNow;
        private string currentPlayerCarClassID = "#not_set#";

        public RF2Spotter(AudioPlayer audioPlayer, Boolean initialEnabledState)
        {
            this.audioPlayer = audioPlayer;
            this.enabled = initialEnabledState;
            this.initialEnabledState = initialEnabledState;
            this.internalSpotter = new NoisyCartesianCoordinateSpotter(
                audioPlayer, initialEnabledState, carLength, carWidth);
        }

        public override void clearState()
        {
            this.previousTime = DateTime.UtcNow;
            this.internalSpotter.clearState();
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

        public override void trigger(Object lastStateObj, Object currentStateObj, GameStateData currentGameState)
        {
#if TRACE_SPOTTER_ELAPSED_TIME
            var watch = System.Diagnostics.Stopwatch.StartNew();
#endif
            if (this.paused)
                return;

            var lastState = lastStateObj as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;
            var currentState = currentStateObj as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;

            if (!this.enabled 
                || currentState.scoring.mScoringInfo.mCurrentET < this.timeAfterRaceStartToActivate
                || currentState.extended.mInRealtimeFC == 0
                || currentState.scoring.mScoringInfo.mInRealtime == 0
                || currentGameState.OpponentData.Count == 0
                || lastState.extended.mInRealtimeFC == 0
                || lastState.scoring.mScoringInfo.mInRealtime == 0)
                return;

            // turn off spotter for formation lap before going green
            if (currentState.scoring.mScoringInfo.mGamePhase == (int)rFactor2Constants.rF2GamePhase.Formation)
                return;

            var now = DateTime.UtcNow;
            rF2VehicleScoring currentPlayerScoring;
            rF2VehicleScoring previousPlayerScoring;
            float timeDiffSeconds;
            try
            {
                currentPlayerScoring = this.getVehicleInfo(currentState);
                previousPlayerScoring = this.getVehicleInfo(lastState);
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

            if (currentPlayerScoring.mInPits != 0)  // No spotter in pits.
                return;

            if (currentGameState != null)
            {
                var carClass = currentGameState.carClass;
                if (carClass != null && !string.Equals(this.currentPlayerCarClassID, carClass.getClassIdentifier()))
                {
                    // Retrieve and use user overridable spotter car length/width.
                    this.internalSpotter.setCarDimensions(GlobalBehaviourSettings.spotterVehicleLength, GlobalBehaviourSettings.spotterVehicleWidth);
                    this.currentPlayerCarClassID = carClass.getClassIdentifier();
                }
            }

            var idsToTelIndicesMap = RF2GameStateMapper.getIdsToTelIndicesMap(ref currentState.telemetry);

            // Initialize current player information.
            float[] currentPlayerPosition = null;
            float currentPlayerSpeed = -1.0f;
            float playerRotation = 0.0f;

            int playerTelIdx = -1;
            if (idsToTelIndicesMap.TryGetValue(currentPlayerScoring.mID, out playerTelIdx))
            {
                var currentPlayerTelemetry = currentState.telemetry.mVehicles[playerTelIdx];

                currentPlayerPosition = new float[] { (float)currentPlayerTelemetry.mPos.x, (float)currentPlayerTelemetry.mPos.z };
                currentPlayerSpeed = (float)Math.Sqrt((currentPlayerTelemetry.mLocalVel.x * currentPlayerTelemetry.mLocalVel.x)
                    + (currentPlayerTelemetry.mLocalVel.y * currentPlayerTelemetry.mLocalVel.y)
                    + (currentPlayerTelemetry.mLocalVel.z * currentPlayerTelemetry.mLocalVel.z));

                playerRotation = (float)(Math.Atan2(currentPlayerTelemetry.mOri[rFactor2Constants.RowZ].x, currentPlayerTelemetry.mOri[rFactor2Constants.RowZ].z));
            }
            else
            {
                // Telemetry is not available, fall back to scoring info.  This is corner case, should not happen often.
                currentPlayerPosition = new float[] { (float)currentPlayerScoring.mPos.x, (float)currentPlayerScoring.mPos.z };
                currentPlayerSpeed = (float)Math.Sqrt((currentPlayerScoring.mLocalVel.x * currentPlayerScoring.mLocalVel.x)
                    + (currentPlayerScoring.mLocalVel.y * currentPlayerScoring.mLocalVel.y)
                    + (currentPlayerScoring.mLocalVel.z * currentPlayerScoring.mLocalVel.z));

                playerRotation = (float)(Math.Atan2(currentPlayerScoring.mOri[rFactor2Constants.RowZ].x, currentPlayerScoring.mOri[rFactor2Constants.RowZ].z));
            }

            if (playerRotation < 0.0f)
                playerRotation = (float)(2.0f * Math.PI) + playerRotation;

            // Find position data for previous player vehicle.  Default to scoring pos, but use telemetry if available (corner case, should not happen often).
            var previousPlayerPosition = new float[] { (float)previousPlayerScoring.mPos.x, (float)previousPlayerScoring.mPos.z };
            for (int i = 0; i < lastState.telemetry.mNumVehicles; ++i)
            {
                if (previousPlayerScoring.mID == lastState.telemetry.mVehicles[i].mID)
                {
                    previousPlayerPosition = new float[] { (float)lastState.telemetry.mVehicles[i].mPos.x, (float)lastState.telemetry.mVehicles[i].mPos.z };
                    break;
                }
            }

            var playerVelocityData = new float[] {
                currentPlayerSpeed,
                (currentPlayerPosition[0] - previousPlayerPosition[0]) / timeDiffSeconds,
                (currentPlayerPosition[1] - previousPlayerPosition[1]) / timeDiffSeconds };

            var currentOpponentPositions = new List<float[]>();
            for (int i = 0; i < currentState.scoring.mScoringInfo.mNumVehicles; ++i)
            {
                var vehicle = currentState.scoring.mVehicles[i];
                if (vehicle.mIsPlayer == 1 || vehicle.mInPits == 1 || vehicle.mLapDist < 0.0f)
                    continue;

                int opponentTelIdx = -1;
                if (idsToTelIndicesMap.TryGetValue(vehicle.mID, out opponentTelIdx))
                {
                    var opponentTelemetry = currentState.telemetry.mVehicles[opponentTelIdx];
                    currentOpponentPositions.Add(new float[] { (float)opponentTelemetry.mPos.x, (float)opponentTelemetry.mPos.z });
                }
                else
                    currentOpponentPositions.Add(new float[] { (float)vehicle.mPos.x, (float)vehicle.mPos.z });  // Use scoring if telemetry isn't available.
            }

            this.internalSpotter.triggerInternal(playerRotation, currentPlayerPosition, playerVelocityData, currentOpponentPositions);

#if TRACE_SPOTTER_ELAPSED_TIME
            watch.Stop();
            var microseconds = watch.ElapsedTicks * 1000000 / System.Diagnostics.Stopwatch.Frequency;
            System.Console.WriteLine("Spotter microseconds: " + microseconds);
#endif
        }
    }
}
