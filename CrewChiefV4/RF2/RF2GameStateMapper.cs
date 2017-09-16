using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.rFactor2.rFactor2Data;
using System.Diagnostics;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV4.rFactor2
{
    public class RF2GameStateMapper : GameStateMapper
    {
        private SpeechRecogniser speechRecogniser;

        public static string playerName = null;

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();

        private float scrubbedTyreWearPercent = 5.0f;
        private float minorTyreWearPercent = 30.0f;
        private float majorTyreWearPercent = 60.0f;
        private float wornOutTyreWearPercent = 85.0f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;

        // At which point we consider that it is raining
        private const double minRainThreshold = 0.1;

        // Pit stop prediction constants.
        private const int minMinutesBetweenPredictedStops = 10;
        private const int minLapsBetweenPredictedStops = 5;

        // If we're running only against AI, force the pit window to open
        private bool isOfflineSession = true;

        // Keep track of opponents processed this time
        private List<string> opponentKeysProcessed = new List<string>();

        // Detect when approaching racing surface after being off track
        private float distanceOffTrack = 0.0f;
        private bool isApproachingTrack = false;

        // User preferences.
        private readonly bool enablePitStopPrediction = UserSettings.GetUserSettings().getBoolean("enable_rf2_pit_stop_prediction");
        private readonly bool enableBlueOnSlower = UserSettings.GetUserSettings().getBoolean("enable_rf2_blue_on_slower");

        // Detect if there any changes in the the game data since the last update.
        private double lastPlayerTelemetryET = -1.0;
        private double lastScoringET = -1.0;

        // State tracking for hacks around model.
        bool lastInRealTimeState = false;

        public RF2GameStateMapper()
        {
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000.0f, this.scrubbedTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, this.scrubbedTyreWearPercent, this.minorTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, this.minorTyreWearPercent, this.majorTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, this.majorTyreWearPercent, this.wornOutTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, this.wornOutTyreWearPercent, 10000.0f));

            this.suspensionDamageThresholds.Add(new CornerData.EnumWithThresholds(DamageLevel.NONE, 0.0f, 1.0f));
            this.suspensionDamageThresholds.Add(new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, 1.0f, 2.0f));
        }

        private int[] minimumSupportedVersionParts = new int[] { 2, 0, 0, 0 };
        public static bool pluginVerified = false;
        public void versionCheck(Object memoryMappedFileStruct)
        {
            if (RF2GameStateMapper.pluginVerified)
                return;

            var shared = memoryMappedFileStruct as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;
            var versionStr = getStringFromBytes(shared.extended.mVersion);

            var versionParts = versionStr.Split('.');
            if (versionParts.Length != 4)
            {
                var msg = "Corrupt rFactor 2 Shared Memory version string: " + versionStr;
                Console.WriteLine(msg);
                return;
            }

            int smVer = 0;
            int minVer = 0;
            int partFactor = 1;
            for (int i = 3; i >= 0; --i)
            {
                int versionPart = 0;
                if (!int.TryParse(versionParts[i], out versionPart))
                {
                    var msg = "Corrupt rFactor 2 Shared Memory version string: " + versionStr;
                    Console.WriteLine(msg);
                    return;
                }

                smVer += (versionPart * partFactor);
                minVer += (this.minimumSupportedVersionParts[i] * partFactor);
                partFactor *= 100;
            }

            if (shared.extended.is64bit == 0)
            {
                Console.WriteLine("Only 64bit version of rFactor 2 is supported.");
            }
            else if (smVer < minVer)
            {
                var minVerStr = string.Join(".", this.minimumSupportedVersionParts);
                var msg = "Unsupported rFactor 2 Shared Memory version: "
                    + versionStr
                    + "  Minimum supported version is: "
                    + minVerStr
                    + "  Please update rFactor2SharedMemoryMapPlugin64.dll";
                Console.WriteLine(msg);
            }
            else
            {
                RF2GameStateMapper.pluginVerified = true;

                var msg = "rFactor 2 Shared Memory version: " + versionStr + " 64bit";
                Console.WriteLine(msg);
            }
        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            var pgs = previousGameState;
            var shared = memoryMappedFileStruct as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;
            var cgs = new GameStateData(shared.ticksWhenRead);

            // No session data
            if (shared.scoring.mScoringInfo.mNumVehicles == 0)
            {
                // If user clicks "Next Session" the session phase goes to "Finished" only if session time actually ran out.
                // Otherwise, game does not update Session Phase.  We do, however, see the numVehicles  drop to zero.
                // If we have a previous game state and it's in a valid phase here, update it to "Finished" and return it,
                // unless it looks like user clicked "Restart" button during the race.
                // Additionally, if user made no valid laps in a session, mark it as DNF, because position does not matter in that case
                // (and it isn't reported by the game, so whatever we announce is wrong).
                //
                // It is well known that a lot of comments is an indicator of trouble.  I suspect we might have to remove or hide 
                // all this crap behind the setting, because there will be cases when incorrect position is reported.  This, however,
                // works most of the time.
                // All of this might need revisiting on exposure of MultiSessionRulesV01.  Another thing to try is to capture last
                // reported position in the plugin.  It is possible we are simply missing it.
                if (pgs != null
                    && pgs.SessionData.SessionType != SessionType.Unavailable
                    && pgs.SessionData.SessionPhase != SessionPhase.Finished
                    && pgs.SessionData.SessionPhase != SessionPhase.Unavailable)
                {
                    if (this.lastInRealTimeState && pgs.SessionData.SessionType == SessionType.Race)
                    {
                        // Looks like race restart without exiting to monitor.  We can't reliably detect session end
                        // here, because it is timing affected (we might miss this between updates).  So better not do it.
                        Console.WriteLine("Abrupt Session End: suppressed due to restart during real time.");
                    }
                    else
                    {
                        if (pgs.SessionData.PlayerLapTimeSessionBest < 0.0f && !pgs.SessionData.IsDisqualified)
                        {
                            // If user has not set any lap time during the session, mark it as DNF.
                            pgs.SessionData.IsDNF = true;

                            Console.WriteLine("Abrupt Session End: mark session as DNF due to no valid laps made.");
                        }
                        // While this detects the "Next Session" this still sounds a bit weird if user clicks
                        // "Leave Session" and goes to main menu.  60 sec delay (minSessionRunTimeForEndMessages) helps, but not entirely.
                        pgs.SessionData.SessionPhase = SessionPhase.Finished;
                        pgs.SessionData.AbruptSessionEndDetected = true;
                        Console.WriteLine("Abrupt Session End: SessionType: " + pgs.SessionData.SessionType);

                        return pgs;
                    }
                }

                this.isOfflineSession = true;
                this.distanceOffTrack = 0;
                this.isApproachingTrack = false;

                if (pgs != null)
                {
                    // In rF2 user can quit practice session and we will never know
                    // about it.  Mark previous game state with Unavailable flags.
                    pgs.SessionData.SessionType = SessionType.Unavailable;
                    pgs.SessionData.SessionPhase = SessionPhase.Unavailable;
                }

                return pgs;
            }

            this.lastInRealTimeState = shared.extended.mInRealtimeFC == 1 || shared.scoring.mScoringInfo.mInRealtime == 1;

            // --------------------------------
            // session data
            // get player scoring info (usually index 0)
            // get session leader scoring info (usually index 1 if not player)
            var playerScoring = new rF2VehicleScoring();
            var leaderScoring = new rF2VehicleScoring();
            for (int i = 0; i < shared.scoring.mScoringInfo.mNumVehicles; ++i)
            {
                var vehicle = shared.scoring.mVehicles[i];
                switch (mapToControlType((rFactor2Constants.rF2Control)vehicle.mControl))
                {
                    case ControlType.AI:
                    case ControlType.Player:
                    case ControlType.Remote:
                        if (vehicle.mIsPlayer == 1)
                            playerScoring = vehicle;

                        if (vehicle.mPlace == 1)
                            leaderScoring = vehicle;
                        break;
                    default:
                        continue;
                }

                if (playerScoring.mIsPlayer == 1 && leaderScoring.mPlace == 1)
                    break;
            }

            // can't find the player or session leader vehicle info (replay)
            if (playerScoring.mIsPlayer != 1 || leaderScoring.mPlace != 1)
                return pgs;

            // Get player and leader telemetry objects.
            // NOTE: Those are not available on first entry to the garage and likely in rare
            // cases during online races.  But using just zeroed structs are mostly ok.
            var playerTelemetry = new rF2VehicleTelemetry();

            // This is shaky part of the mapping, but here goes:
            // Telemetry and Scoring are updated separately by the game.  Therefore, one can be 
            // ahead of another, sometimes in a significant way.  Particularly, this is possible with
            // online races, where people quit/join the game.
            //
            // For Crew Chief in rF2, our primary data structure is _Scoring_ (because it contains timings).
            // However, since Telemetry is updated more frequently (up to 90FPS vs 5FPS for Scoring), we
            // try to use Telemetry values whenever possible (position, speed, elapsed time, orientation).
            // In those rare cases where Scoring contains vehicle that is not in Telemetry set, use Scoring as a
            // fallback where possible.  For the rest of values, use zeroed out Telemetry object (playerTelemetry).
            bool playerTelemetryAvailable = true;

            var idsToTelIndicesMap = RF2GameStateMapper.getIdsToTelIndicesMap(ref shared.telemetry);
            int playerTelIdx = -1;
            if (idsToTelIndicesMap.TryGetValue(playerScoring.mID, out playerTelIdx))
                playerTelemetry = shared.telemetry.mVehicles[playerTelIdx];
            else
            {
                playerTelemetryAvailable = false;
                RF2GameStateMapper.initEmptyVehicleTelemetry(ref playerTelemetry);

                // Exclude known situations when telemetry is not available, but log otherwise to get more
                // insights.
                if (shared.extended.mInRealtimeFC == 1 
                    && shared.scoring.mScoringInfo.mInRealtime == 1
                    && shared.scoring.mScoringInfo.mGamePhase != (byte)rFactor2Constants.rF2GamePhase.GridWalk)
                {
                    Console.WriteLine("Failed to obtain player telemetry, falling back to scoring.");
                }
            }

            // See if there are meaningful updates to the data.
            var currPlayerTelET = playerTelemetry.mElapsedTime;
            var currScoringET = shared.scoring.mScoringInfo.mCurrentET;

            if (currPlayerTelET == this.lastPlayerTelemetryET 
                && currScoringET == this.lastScoringET)
                return pgs;  // Skip this update.

            this.lastPlayerTelemetryET = currPlayerTelET;
            this.lastScoringET = currScoringET;

            if (RF2GameStateMapper.playerName == null)
            {
                var driverName = getStringFromBytes(playerScoring.mDriverName).ToLower();
                NameValidator.validateName(driverName);
                RF2GameStateMapper.playerName = driverName;
            }

            // these things should remain constant during a session
            var csd = cgs.SessionData;
            var psd = pgs != null ? pgs.SessionData : null;
            csd.EventIndex = shared.scoring.mScoringInfo.mSession;

            csd.SessionIteration
                = shared.scoring.mScoringInfo.mSession >= 1 && shared.scoring.mScoringInfo.mSession <= 4 ? shared.scoring.mScoringInfo.mSession - 1 :
                shared.scoring.mScoringInfo.mSession >= 5 && shared.scoring.mScoringInfo.mSession <= 8 ? shared.scoring.mScoringInfo.mSession - 5 :
                shared.scoring.mScoringInfo.mSession >= 10 && shared.scoring.mScoringInfo.mSession <= 13 ? shared.scoring.mScoringInfo.mSession - 10 : 0;

            csd.SessionType = mapToSessionType(shared);
            csd.SessionPhase = mapToSessionPhase((rFactor2Constants.rF2GamePhase)shared.scoring.mScoringInfo.mGamePhase, csd.SessionType, ref playerScoring);

            var carClassId = getStringFromBytes(playerScoring.mVehicleClass);
            cgs.carClass = CarData.getCarClassForClassName(carClassId);
            CarData.CLASS_ID = carClassId;
            this.brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(cgs.carClass);
            csd.DriverRawName = getStringFromBytes(playerScoring.mDriverName).ToLower();
            csd.TrackDefinition = new TrackDefinition(getStringFromBytes(shared.scoring.mScoringInfo.mTrackName), (float)shared.scoring.mScoringInfo.mLapDist);

            if (pgs == null || psd.TrackDefinition.name != csd.TrackDefinition.name)
            {
                // New game or new track
                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(csd.TrackDefinition.name, (float)shared.scoring.mScoringInfo.mLapDist);
                csd.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                csd.TrackDefinition.isOval = tdc.isOval;
                csd.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(csd.TrackDefinition);
            }
            else if (pgs != null)
            {
                // Copy from previous gamestate
                csd.TrackDefinition.trackLandmarks = psd.TrackDefinition.trackLandmarks;
                csd.TrackDefinition.gapPoints = psd.TrackDefinition.gapPoints;
            }

            csd.SessionNumberOfLaps = shared.scoring.mScoringInfo.mMaxLaps > 0 && shared.scoring.mScoringInfo.mMaxLaps < 1000 ? shared.scoring.mScoringInfo.mMaxLaps : 0;

            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630.0f;
            csd.SessionTotalRunTime
                = (float)shared.scoring.mScoringInfo.mEndET > 0.0f
                    ? (float)shared.scoring.mScoringInfo.mEndET
                    : csd.SessionNumberOfLaps > 0 ? 0.0f : defaultSessionTotalRunTime;

            // If any difference between current and previous states suggests it is a new session
            if (pgs == null
                || csd.SessionType != psd.SessionType
                || !String.Equals(cgs.carClass.getClassIdentifier(), pgs.carClass.getClassIdentifier())
                || csd.DriverRawName != psd.DriverRawName
                || csd.TrackDefinition.name != psd.TrackDefinition.name  // TODO: this is empty sometimes, investigate 
                || csd.TrackDefinition.trackLength != psd.TrackDefinition.trackLength
                || csd.EventIndex != psd.EventIndex
                || csd.SessionIteration != psd.SessionIteration)
            {
                csd.IsNewSession = true;
            }
            // Else, if any difference between current and previous phases suggests it is a new session
            else if ((psd.SessionPhase == SessionPhase.Checkered
                        || psd.SessionPhase == SessionPhase.Finished
                        || psd.SessionPhase == SessionPhase.Green
                        || psd.SessionPhase == SessionPhase.FullCourseYellow)
                    && (csd.SessionPhase == SessionPhase.Garage
                        || csd.SessionPhase == SessionPhase.Gridwalk
                        || csd.SessionPhase == SessionPhase.Formation
                        || csd.SessionPhase == SessionPhase.Countdown))
            {
                csd.IsNewSession = true;
            }

            // Do not use previous game state if this is the new session.
            if (csd.IsNewSession)
            {
                pgs = null;
                GlobalBehaviourSettings.UpdateFromCarClass(cgs.carClass);
            }

            // Restore cumulative data.
            if (psd != null && !csd.IsNewSession)
            {
                cgs.PitData.NumPitStops = pgs.PitData.NumPitStops;
            }

            csd.SessionStartTime = csd.IsNewSession ? cgs.Now : psd.SessionStartTime;
            csd.SessionHasFixedTime = csd.SessionTotalRunTime > 0.0f;

            csd.SessionRunningTime = (float)(playerTelemetryAvailable ? playerTelemetry.mElapsedTime : shared.scoring.mScoringInfo.mCurrentET);
            csd.SessionTimeRemaining = csd.SessionHasFixedTime ? csd.SessionTotalRunTime - csd.SessionRunningTime : 0.0f;

            // hack for test day sessions running longer than allotted time
            csd.SessionTimeRemaining = csd.SessionTimeRemaining < 0.0f && shared.scoring.mScoringInfo.mSession == 0 ? defaultSessionTotalRunTime : csd.SessionTimeRemaining;

            csd.NumCars = shared.scoring.mScoringInfo.mNumVehicles;
            csd.NumCarsAtStartOfSession = csd.IsNewSession ? csd.NumCars : psd.NumCarsAtStartOfSession;
            csd.Position = playerScoring.mPlace;
            csd.UnFilteredPosition = csd.Position;
            csd.SessionStartPosition = csd.IsNewSession ? csd.Position : psd.SessionStartPosition;
            csd.SectorNumber = playerScoring.mSector == 0 ? 3 : playerScoring.mSector;
            csd.IsNewSector = csd.IsNewSession || csd.SectorNumber != psd.SectorNumber;
            csd.IsNewLap = csd.IsNewSession || (csd.IsNewSector && csd.SectorNumber == 1);
            csd.PositionAtStartOfCurrentLap = csd.IsNewLap ? csd.Position : psd.PositionAtStartOfCurrentLap;
            // TODO: See if Black Flag handling needed here.
            csd.IsDisqualified = (rFactor2Constants.rF2FinishStatus)playerScoring.mFinishStatus == rFactor2Constants.rF2FinishStatus.Dq;
            csd.IsDNF = (rFactor2Constants.rF2FinishStatus)playerScoring.mFinishStatus == rFactor2Constants.rF2FinishStatus.Dnf;

            // NOTE: Telemetry contains mLapNumber, which might be ahead of Scoring due to higher refresh rate.  However,
            // since we use Scoring fields for timing calculations, stick to Scoring here as well.
            csd.CompletedLaps = playerScoring.mTotalLaps;

            ////////////////////////////////////
            // motion data
            if (playerTelemetryAvailable)
            {
                cgs.PositionAndMotionData.CarSpeed = (float)RF2GameStateMapper.getVehicleSpeed(ref playerTelemetry);
                cgs.PositionAndMotionData.DistanceRoundTrack = (float)getEstimatedLapDist(shared, ref playerScoring, ref playerTelemetry);
            }
            else
            {
                cgs.PositionAndMotionData.CarSpeed = (float)RF2GameStateMapper.getVehicleSpeed(ref playerScoring);
                cgs.PositionAndMotionData.DistanceRoundTrack = (float)playerScoring.mLapDist;
            }

            // Is online session?
            this.isOfflineSession = true;
            for (int i = 0; i < shared.scoring.mScoringInfo.mNumVehicles; ++i)
            {
                if ((rFactor2Constants.rF2Control)shared.scoring.mVehicles[i].mControl == rFactor2Constants.rF2Control.Remote)
                    this.isOfflineSession = false;
            }

            ///////////////////////////////////
            // Pit Data
            cgs.PitData.IsRefuellingAllowed = true;

            if (this.enablePitStopPrediction)
            {
                cgs.PitData.HasMandatoryPitStop = this.isOfflineSession
                    && playerTelemetry.mScheduledStops > 0
                    && playerScoring.mNumPitstops < playerTelemetry.mScheduledStops
                    && csd.SessionType == SessionType.Race;

                cgs.PitData.PitWindowStart = this.isOfflineSession && cgs.PitData.HasMandatoryPitStop ? 1 : 0;

                var pitWindowEndLapOrTime = 0;
                if (cgs.PitData.HasMandatoryPitStop)
                {
                    if (csd.SessionHasFixedTime)
                    {
                        var minutesBetweenStops = (int)(csd.SessionTotalRunTime / 60 / (playerTelemetry.mScheduledStops + 1));
                        if (minutesBetweenStops > RF2GameStateMapper.minMinutesBetweenPredictedStops)
                            pitWindowEndLapOrTime = minutesBetweenStops * (playerScoring.mNumPitstops + 1) + 1;
                    }
                    else
                    {
                        var lapsBetweenStops = (int)(csd.SessionNumberOfLaps / (playerTelemetry.mScheduledStops + 1));
                        if (lapsBetweenStops > RF2GameStateMapper.minLapsBetweenPredictedStops)
                            pitWindowEndLapOrTime = lapsBetweenStops * (playerScoring.mNumPitstops + 1) + 1;
                    }

                    // Force the MandatoryPit event to be re-initialsed if the window end has been recalculated.
                    cgs.PitData.ResetEvents = pgs != null && pitWindowEndLapOrTime > pgs.PitData.PitWindowEnd;
                }

                cgs.PitData.PitWindowEnd = pitWindowEndLapOrTime;
            }

            // mInGarageStall also means retired or before race start, but for now use it here.
            cgs.PitData.InPitlane = playerScoring.mInPits == 1 || playerScoring.mInGarageStall == 1;

            if (cgs.PitData.InPitlane && pgs != null && !pgs.PitData.InPitlane)
                cgs.PitData.NumPitStops++;

            cgs.PitData.IsAtPitExit = pgs != null && pgs.PitData.InPitlane && !cgs.PitData.InPitlane;
            cgs.PitData.OnOutLap = cgs.PitData.InPitlane && csd.SectorNumber == 1;

            if (shared.extended.mInRealtimeFC == 0  // Mark pit limiter as unavailable if in Monitor (not real time).
                || shared.scoring.mScoringInfo.mInRealtime == 0
                || playerTelemetry.mSpeedLimiterAvailable == 0)
                cgs.PitData.limiterStatus = -1;
            else
                cgs.PitData.limiterStatus = playerTelemetry.mSpeedLimiter > 0 ? 1 : 0;

            if (pgs != null
                && csd.CompletedLaps == psd.CompletedLaps
                && pgs.PitData.OnOutLap)
            {
                // If current lap is pit out lap, keep it that way till lap completes.
                cgs.PitData.OnOutLap = true;
            }

            cgs.PitData.OnInLap = cgs.PitData.InPitlane && csd.SectorNumber == 3;

            cgs.PitData.IsMakingMandatoryPitStop = cgs.PitData.HasMandatoryPitStop
                && cgs.PitData.OnInLap
                && csd.CompletedLaps > cgs.PitData.PitWindowStart;

            cgs.PitData.PitWindow = cgs.PitData.IsMakingMandatoryPitStop
                ? PitWindow.StopInProgress : mapToPitWindow((rFactor2Constants.rF2YellowFlagState)shared.scoring.mScoringInfo.mYellowFlagState);

            if (pgs != null)
            {
                cgs.PitData.MandatoryPitStopCompleted = pgs.PitData.MandatoryPitStopCompleted || cgs.PitData.IsMakingMandatoryPitStop;
            }

            ////////////////////////////////////
            // Timings
            if (psd != null && !csd.IsNewSession)
            {
                // Preserve current values.
                // Those values change on sector/lap change, otherwise stay the same between updates.
                psd.restorePlayerTimings(csd);
            }

            if (cgs.PitData.InPitlane && pgs != null && !pgs.PitData.InPitlane)
                cgs.PitData.NumPitStops++;

            this.processPlayerTimingData(ref shared.scoring, cgs, pgs, ref playerScoring);

            csd.SessionTimesAtEndOfSectors = pgs != null ? psd.SessionTimesAtEndOfSectors : new SessionData().SessionTimesAtEndOfSectors;

            if (csd.IsNewSector && !csd.IsNewSession)
            {
                // There's a slight delay due to scoring updating every 200 ms, so we can't use SessionRunningTime here.
                // NOTE: Telemetry contains mLapStartET as well, which is out of sync with Scoring mLapStartET (might be ahead
                // due to higher refresh rate).  However, since we're using Scoring for timings elsewhere, use Scoring here, for now at least.
                switch (csd.SectorNumber)
                {
                    case 1:
                        csd.SessionTimesAtEndOfSectors[3]
                            = playerScoring.mLapStartET > 0.0f ? (float)playerScoring.mLapStartET : -1.0f;
                        break;
                    case 2:
                        csd.SessionTimesAtEndOfSectors[1]
                            = playerScoring.mLapStartET > 0.0f && playerScoring.mCurSector1 > 0.0f
                                ? (float)(playerScoring.mLapStartET + playerScoring.mCurSector1)
                                : -1.0f;
                        break;
                    case 3:
                        csd.SessionTimesAtEndOfSectors[2]
                            = playerScoring.mLapStartET > 0 && playerScoring.mCurSector2 > 0.0f
                                ? (float)(playerScoring.mLapStartET + playerScoring.mCurSector2)
                                : -1.0f;
                        break;
                    default:
                        break;
                }
            }

            if (pgs != null)
            {
                foreach (var lt in psd.formattedPlayerLapTimes)
                    csd.formattedPlayerLapTimes.Add(lt);
            }

            if (csd.IsNewLap && csd.LapTimePrevious > 0)
                csd.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(csd.LapTimePrevious).ToString(@"mm\:ss\.fff"));

            csd.LeaderHasFinishedRace = leaderScoring.mFinishStatus == (int)rFactor2Constants.rF2FinishStatus.Finished;
            csd.LeaderSectorNumber = leaderScoring.mSector == 0 ? 3 : leaderScoring.mSector;
            csd.TimeDeltaFront = (float)Math.Abs(playerScoring.mTimeBehindNext);

            // --------------------------------
            // engine data
            cgs.EngineData.EngineRpm = (float)playerTelemetry.mEngineRPM;
            cgs.EngineData.MaxEngineRpm = (float)playerTelemetry.mEngineMaxRPM;
            cgs.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            cgs.EngineData.EngineOilTemp = (float)playerTelemetry.mEngineOilTemp;
            cgs.EngineData.EngineWaterTemp = (float)playerTelemetry.mEngineWaterTemp;
            //HACK: there's probably a cleaner way to do this...
            if (playerTelemetry.mOverheating == 1)
            {
                cgs.EngineData.EngineWaterTemp += 50;
                cgs.EngineData.EngineOilTemp += 50;
            }

            // --------------------------------
            // transmission data
            cgs.TransmissionData.Gear = playerTelemetry.mGear;

            // controls
            cgs.ControlData.BrakePedal = (float)playerTelemetry.mUnfilteredBrake;
            cgs.ControlData.ThrottlePedal = (float)playerTelemetry.mUnfilteredThrottle;
            cgs.ControlData.ClutchPedal = (float)playerTelemetry.mUnfilteredClutch;

            // --------------------------------
            // damage
            cgs.CarDamageData.DamageEnabled = true;
            cgs.CarDamageData.LastImpactTime = (float)playerTelemetry.mLastImpactET;

            var playerDamageInfo = shared.extended.mTrackedDamages[playerScoring.mID % rFactor2Constants.MAX_MAPPED_IDS];

            if (shared.extended.mPhysics.mInvulnerable == 0)
            {
                const double MINOR_DAMAGE_THRESHOLD = 1500.0;
                const double MAJOR_DAMAGE_THRESHOLD = 4000.0;
                const double ACCUMULATED_THRESHOLD_FACTOR = 4.0;

                bool anyWheelDetached = false;
                foreach (var wheel in playerTelemetry.mWheels)
                    anyWheelDetached |= wheel.mDetached == 1;

                if (playerTelemetry.mDetached == 1
                    && anyWheelDetached)  // Wheel is not really aero damage, but it is bad situation.
                {
                    // Things are sad if we have both part and wheel detached.
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.DESTROYED;
                }
                else if (playerTelemetry.mDetached == 1  // If there are parts detached, consider damage major, and pit stop is necessary.
                    || playerDamageInfo.mMaxImpactMagnitude > MAJOR_DAMAGE_THRESHOLD)  // Also take max impact magnitude into consideration.
                {

                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.MAJOR;
                }
                else if (playerDamageInfo.mMaxImpactMagnitude > MINOR_DAMAGE_THRESHOLD
                    || playerDamageInfo.mAccumulatedImpactMagnitude > MINOR_DAMAGE_THRESHOLD * ACCUMULATED_THRESHOLD_FACTOR)  // Also consider accumulated damage, if user grinds car against the wall, max won't be high, but car is still damaged.
                {
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.MINOR;
                }
                else if (playerDamageInfo.mMaxImpactMagnitude > 0.0)
                {
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.NONE;
                }
            }
            else  // shared.extended.mPhysics.mInvulnerable != 0
            {
                // roll over all you want - it's just a scratch.
                cgs.CarDamageData.OverallAeroDamage = playerDamageInfo.mMaxImpactMagnitude > 0.0 ? DamageLevel.TRIVIAL : DamageLevel.NONE;
            }

            // --------------------------------
            // control data
            cgs.ControlData.ControlType = mapToControlType((rFactor2Constants.rF2Control)playerScoring.mControl);

            // --------------------------------
            // tire data
            // rF2 reports in Kelvin
            cgs.TyreData.TireWearActive = true;

            // For now, all tyres will be reported as front compund.
            var tt = this.mapToTyreType(ref playerTelemetry);

            var wheelFrontLeft = playerTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft];
            cgs.TyreData.FrontLeftTyreType = tt;
            cgs.TyreData.LeftFrontAttached = wheelFrontLeft.mDetached == 0;
            cgs.TyreData.FrontLeft_LeftTemp = (float)wheelFrontLeft.mTemperature[0] - 273.15f;
            cgs.TyreData.FrontLeft_CenterTemp = (float)wheelFrontLeft.mTemperature[1] - 273.15f;
            cgs.TyreData.FrontLeft_RightTemp = (float)wheelFrontLeft.mTemperature[2] - 273.15f;

            var frontLeftTemp = (cgs.TyreData.FrontLeft_CenterTemp + cgs.TyreData.FrontLeft_LeftTemp + cgs.TyreData.FrontLeft_RightTemp) / 3.0f;
            cgs.TyreData.FrontLeftPressure = wheelFrontLeft.mFlat == 0 ? (float)wheelFrontLeft.mPressure : 0.0f;
            cgs.TyreData.FrontLeftPercentWear = (float)(1.0f - wheelFrontLeft.mWear) * 100.0f;
            if (csd.IsNewLap)
            {
                cgs.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }
            else if (pgs == null || frontLeftTemp > pgs.TyreData.PeakFrontLeftTemperatureForLap)
            {
                cgs.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }

            var wheelFrontRight = playerTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight];
            cgs.TyreData.FrontRightTyreType = tt;
            cgs.TyreData.RightFrontAttached = wheelFrontRight.mDetached == 0;
            cgs.TyreData.FrontRight_LeftTemp = (float)wheelFrontRight.mTemperature[0] - 273.15f;
            cgs.TyreData.FrontRight_CenterTemp = (float)wheelFrontRight.mTemperature[1] - 273.15f;
            cgs.TyreData.FrontRight_RightTemp = (float)wheelFrontRight.mTemperature[2] - 273.15f;

            var frontRightTemp = (cgs.TyreData.FrontRight_CenterTemp + cgs.TyreData.FrontRight_LeftTemp + cgs.TyreData.FrontRight_RightTemp) / 3.0f;
            cgs.TyreData.FrontRightPressure = wheelFrontRight.mFlat == 0 ? (float)wheelFrontRight.mPressure : 0.0f;
            cgs.TyreData.FrontRightPercentWear = (float)(1.0f - wheelFrontRight.mWear) * 100.0f;

            if (csd.IsNewLap)
                cgs.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            else if (pgs == null || frontRightTemp > pgs.TyreData.PeakFrontRightTemperatureForLap)
                cgs.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;

            var wheelRearLeft = playerTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft];
            cgs.TyreData.RearLeftTyreType = tt;
            cgs.TyreData.LeftRearAttached = wheelRearLeft.mDetached == 0;
            cgs.TyreData.RearLeft_LeftTemp = (float)wheelRearLeft.mTemperature[0] - 273.15f;
            cgs.TyreData.RearLeft_CenterTemp = (float)wheelRearLeft.mTemperature[1] - 273.15f;
            cgs.TyreData.RearLeft_RightTemp = (float)wheelRearLeft.mTemperature[2] - 273.15f;

            var rearLeftTemp = (cgs.TyreData.RearLeft_CenterTemp + cgs.TyreData.RearLeft_LeftTemp + cgs.TyreData.RearLeft_RightTemp) / 3.0f;
            cgs.TyreData.RearLeftPressure = wheelRearLeft.mFlat == 0 ? (float)wheelRearLeft.mPressure : 0.0f;
            cgs.TyreData.RearLeftPercentWear = (float)(1.0f - wheelRearLeft.mWear) * 100.0f;

            if (csd.IsNewLap)
                cgs.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            else if (pgs == null || rearLeftTemp > pgs.TyreData.PeakRearLeftTemperatureForLap)
                cgs.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;

            var wheelRearRight = playerTelemetry.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight];
            cgs.TyreData.RearRightTyreType = tt;
            cgs.TyreData.RightRearAttached = wheelRearRight.mDetached == 0;
            cgs.TyreData.RearRight_LeftTemp = (float)wheelRearRight.mTemperature[0] - 273.15f;
            cgs.TyreData.RearRight_CenterTemp = (float)wheelRearRight.mTemperature[1] - 273.15f;
            cgs.TyreData.RearRight_RightTemp = (float)wheelRearRight.mTemperature[2] - 273.15f;

            var rearRightTemp = (cgs.TyreData.RearRight_CenterTemp + cgs.TyreData.RearRight_LeftTemp + cgs.TyreData.RearRight_RightTemp) / 3.0f;
            cgs.TyreData.RearRightPressure = wheelRearRight.mFlat == 0 ? (float)wheelRearRight.mPressure : 0.0f;
            cgs.TyreData.RearRightPercentWear = (float)(1.0f - wheelRearRight.mWear) * 100.0f;

            if (csd.IsNewLap)
                cgs.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            else if (pgs == null || rearRightTemp > pgs.TyreData.PeakRearRightTemperatureForLap)
                cgs.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;

            cgs.TyreData.TyreConditionStatus = CornerData.getCornerData(this.tyreWearThresholds, cgs.TyreData.FrontLeftPercentWear,
                cgs.TyreData.FrontRightPercentWear, cgs.TyreData.RearLeftPercentWear, cgs.TyreData.RearRightPercentWear);

            var tyreTempThresholds = CarData.getTyreTempThresholds(cgs.carClass);
            cgs.TyreData.TyreTempStatus = CornerData.getCornerData(tyreTempThresholds,
                cgs.TyreData.PeakFrontLeftTemperatureForLap, cgs.TyreData.PeakFrontRightTemperatureForLap,
                cgs.TyreData.PeakRearLeftTemperatureForLap, cgs.TyreData.PeakRearRightTemperatureForLap);

            // some simple locking / spinning checks
            if (cgs.PositionAndMotionData.CarSpeed > 7.0f)
            {
                // TODO: fix this properly - decrease the minRotatingSpeed from 2*pi to pi just to hide the problem
                float minRotatingSpeed = (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / cgs.carClass.maxTyreCircumference;
                cgs.TyreData.LeftFrontIsLocked = Math.Abs(wheelFrontLeft.mRotation) < minRotatingSpeed;
                cgs.TyreData.RightFrontIsLocked = Math.Abs(wheelFrontRight.mRotation) < minRotatingSpeed;
                cgs.TyreData.LeftRearIsLocked = Math.Abs(wheelRearLeft.mRotation) < minRotatingSpeed;
                cgs.TyreData.RightRearIsLocked = Math.Abs(wheelRearRight.mRotation) < minRotatingSpeed;

                // TODO: fix this properly - increase the maxRotatingSpeed from 2*pi to 3*pi just to hide the problem
                float maxRotatingSpeed = 3 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / cgs.carClass.minTyreCircumference;
                cgs.TyreData.LeftFrontIsSpinning = Math.Abs(wheelFrontLeft.mRotation) > maxRotatingSpeed;
                cgs.TyreData.RightFrontIsSpinning = Math.Abs(wheelFrontRight.mRotation) > maxRotatingSpeed;
                cgs.TyreData.LeftRearIsSpinning = Math.Abs(wheelRearLeft.mRotation) > maxRotatingSpeed;
                cgs.TyreData.RightRearIsSpinning = Math.Abs(wheelRearRight.mRotation) > maxRotatingSpeed;
#if DEBUG
                RF2GameStateMapper.writeSpinningLockingDebugMsg(cgs, wheelFrontLeft.mRotation, wheelFrontRight.mRotation,
                    wheelRearLeft.mRotation, wheelRearRight.mRotation, minRotatingSpeed, maxRotatingSpeed);
#endif
            }

            // use detached wheel status for suspension damage
            cgs.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(this.suspensionDamageThresholds,
                !cgs.TyreData.LeftFrontAttached ? 1 : 0,
                !cgs.TyreData.RightFrontAttached ? 1 : 0,
                !cgs.TyreData.LeftRearAttached ? 1 : 0,
                !cgs.TyreData.RightRearAttached ? 1 : 0);

            // --------------------------------
            // brake data
            // rF2 reports in Kelvin
            cgs.TyreData.LeftFrontBrakeTemp = (float)wheelFrontLeft.mBrakeTemp - 273.15f;
            cgs.TyreData.RightFrontBrakeTemp = (float)wheelFrontRight.mBrakeTemp - 273.15f;
            cgs.TyreData.LeftRearBrakeTemp = (float)wheelRearLeft.mBrakeTemp - 273.15f;
            cgs.TyreData.RightRearBrakeTemp = (float)wheelRearRight.mBrakeTemp - 273.15f;

            if (this.brakeTempThresholdsForPlayersCar != null)
            {
                cgs.TyreData.BrakeTempStatus = CornerData.getCornerData(this.brakeTempThresholdsForPlayersCar,
                    cgs.TyreData.LeftFrontBrakeTemp, cgs.TyreData.RightFrontBrakeTemp,
                    cgs.TyreData.LeftRearBrakeTemp, cgs.TyreData.RightRearBrakeTemp);
            }

            // --------------------------------
            // track conditions
            if (cgs.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < cgs.Now)
            {
                cgs.Conditions.addSample(cgs.Now, csd.CompletedLaps, csd.SectorNumber,
                    (float)shared.scoring.mScoringInfo.mAmbientTemp, (float)shared.scoring.mScoringInfo.mTrackTemp, (float)shared.scoring.mScoringInfo.mRaining,
                    (float)Math.Sqrt((double)(shared.scoring.mScoringInfo.mWind.x * shared.scoring.mScoringInfo.mWind.x + shared.scoring.mScoringInfo.mWind.y * shared.scoring.mScoringInfo.mWind.y + shared.scoring.mScoringInfo.mWind.z * shared.scoring.mScoringInfo.mWind.z)), 0, 0, 0);
            }

            // --------------------------------
            // opponent data
            this.opponentKeysProcessed.Clear();

            // first check for duplicates:
            Dictionary<string, int> driverNameCounts = new Dictionary<string, int>();
            Dictionary<string, int> duplicatesCreated = new Dictionary<string, int>();
            for (int i = 0; i < shared.scoring.mScoringInfo.mNumVehicles; ++i)
            {
                var vehicleScoring = shared.scoring.mVehicles[i];
                var driverName = getStringFromBytes(vehicleScoring.mDriverName).ToLower();

                if (driverNameCounts.ContainsKey(driverName))
                    driverNameCounts[driverName] += 1;
                else
                    driverNameCounts.Add(driverName, 1);
            }

            for (int i = 0; i < shared.scoring.mScoringInfo.mNumVehicles; ++i)
            {
                var vehicleScoring = shared.scoring.mVehicles[i];
                if (vehicleScoring.mIsPlayer == 1)
                {
                    csd.OverallSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0.0f ?
                        csd.PlayerLapTimeSessionBest : -1.0f;

                    csd.PlayerClassSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0.0f ?
                        csd.PlayerLapTimeSessionBest : -1.0f;

                    if (csd.IsNewLap && psd != null && !psd.IsNewLap)
                    {
                        if (!csd.PlayerClassSessionBestLapTimeByTyre.ContainsKey(cgs.TyreData.FrontLeftTyreType)
                            || csd.PlayerClassSessionBestLapTimeByTyre[cgs.TyreData.FrontLeftTyreType] > csd.LapTimePrevious)
                        {
                            csd.PlayerClassSessionBestLapTimeByTyre[cgs.TyreData.FrontLeftTyreType] = csd.LapTimePrevious;
                        }

                        if (!csd.PlayerBestLapTimeByTyre.ContainsKey(cgs.TyreData.FrontLeftTyreType)
                            || csd.PlayerBestLapTimeByTyre[cgs.TyreData.FrontLeftTyreType] > csd.LapTimePrevious)
                        {
                            csd.PlayerBestLapTimeByTyre[cgs.TyreData.FrontLeftTyreType] = csd.LapTimePrevious;
                        }
                    }

                    continue;
                }

                var ct = this.mapToControlType((rFactor2Constants.rF2Control)vehicleScoring.mControl);
                if (ct == ControlType.Player || ct == ControlType.Replay || ct == ControlType.Unavailable)
                    continue;

                // Get telemetry for this vehicle.
                var vehicleTelemetry = new rF2VehicleTelemetry();
                bool vehicleTelemetryAvailable = true;
                int vehicleTelIdx = -1;
                if (idsToTelIndicesMap.TryGetValue(vehicleScoring.mID, out vehicleTelIdx))
                    vehicleTelemetry = shared.telemetry.mVehicles[vehicleTelIdx];
                else
                {
                    vehicleTelemetryAvailable = false;
                    RF2GameStateMapper.initEmptyVehicleTelemetry(ref vehicleTelemetry);

                    // Exclude known situations when telemetry is not available, but log otherwise to get more
                    // insights.
                    if (shared.extended.mInRealtimeFC == 1
                        && shared.scoring.mScoringInfo.mInRealtime == 1
                        && shared.scoring.mScoringInfo.mGamePhase != (byte)rFactor2Constants.rF2GamePhase.GridWalk)
                    {
                        Console.WriteLine("Failed to obtain opponent telemetry, falling back to scoring.");
                    }
                }

                var driverName = getStringFromBytes(vehicleScoring.mDriverName).ToLower();
                OpponentData opponentPrevious;
                int duplicatesCount = driverNameCounts[driverName];
                string opponentKey;
                if (duplicatesCount > 1)
                {
                    if (!isOfflineSession)
                    {
                        // there shouldn't be duplicate driver names in online sessions. This is probably a temporary glitch in the shared memory data - 
                        // don't panic and drop the existing opponentData for this key - just copy it across to the current state. This prevents us losing
                        // the historical data and repeatedly re-adding this name to the SpeechRecogniser (which is expensive)
                        if (pgs != null && pgs.OpponentData.ContainsKey(driverName)
                            && !cgs.OpponentData.ContainsKey(driverName))
                        {
                            cgs.OpponentData.Add(driverName, pgs.OpponentData[driverName]);
                        }
                        opponentKeysProcessed.Add(driverName);
                        continue;
                    }
                    else
                    {
                        // offline we can have any number of duplicates :(
                        opponentKey = this.getOpponentKeyForVehicleInfo(ref vehicleScoring, ref vehicleTelemetry, pgs, csd.SessionRunningTime, driverName, duplicatesCount, vehicleTelemetryAvailable);

                        if (opponentKey == null)
                        {
                            // there's no previous opponent data record for this driver so create one
                            if (duplicatesCreated.ContainsKey(driverName))
                                duplicatesCreated[driverName] += 1;
                            else
                                duplicatesCreated.Add(driverName, 1);

                            opponentKey = driverName + "_duplicate_" + duplicatesCreated[driverName];
                        }
                    }
                }
                else
                {
                    opponentKey = driverName;
                }

                opponentPrevious = pgs == null || opponentKey == null || !pgs.OpponentData.ContainsKey(opponentKey) ? null : previousGameState.OpponentData[opponentKey];
                var opponent = new OpponentData();
                opponent.DriverRawName = driverName;
                opponent.CarClass = CarData.getCarClassForClassName(getStringFromBytes(vehicleScoring.mVehicleClass));
                opponent.CurrentTyres = this.mapToTyreType(ref vehicleTelemetry);
                opponent.DriverRawName = driverName;
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.Position = vehicleScoring.mPlace;

                if (opponentPrevious == null)
                    opponent.TyreChangesByLap[0] = opponent.CurrentTyres;

                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    this.speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName +
                        " is using car class " + opponent.CarClass.getClassIdentifier() +
                        " at position " + opponent.Position.ToString());
                }
                
                // Carry over state
                if (opponentPrevious != null)
                {
                    // Copy so that we can safely use previous state.
                    foreach (var old in opponentPrevious.OpponentLapData)
                        opponent.OpponentLapData.Add(old);

                    foreach (var old in opponentPrevious.TyreChangesByLap)
                        opponent.TyreChangesByLap.Add(old.Key, old.Value);

                    opponent.NumPitStops = opponentPrevious.NumPitStops;
                }

                opponent.UnFilteredPosition = opponent.Position;
                opponent.SessionTimeAtLastPositionChange
                    = opponentPrevious != null && opponentPrevious.Position != opponent.Position
                            ? csd.SessionRunningTime : -1.0f;

                opponent.CompletedLaps = vehicleScoring.mTotalLaps;
                opponent.CurrentSectorNumber = vehicleScoring.mSector == 0 ? 3 : vehicleScoring.mSector;
                var isNewSector = csd.IsNewSession || (opponentPrevious != null && opponentPrevious.CurrentSectorNumber != opponent.CurrentSectorNumber);
                opponent.IsNewLap = csd.IsNewSession || (isNewSector && opponent.CurrentSectorNumber == 1 && opponent.CompletedLaps > 0);

                if (vehicleTelemetryAvailable)
                {
                    opponent.Speed = (float)RF2GameStateMapper.getVehicleSpeed(ref vehicleTelemetry);
                    opponent.WorldPosition = new float[] { (float)vehicleTelemetry.mPos.x, (float)vehicleTelemetry.mPos.z };
                    opponent.DistanceRoundTrack = (float)RF2GameStateMapper.getEstimatedLapDist(shared, ref vehicleScoring, ref vehicleTelemetry);
                }
                else
                {
                    opponent.Speed = (float)RF2GameStateMapper.getVehicleSpeed(ref vehicleScoring);
                    opponent.WorldPosition = new float[] { (float)vehicleScoring.mPos.x, (float)vehicleScoring.mPos.z };
                    opponent.DistanceRoundTrack = (float)vehicleScoring.mLapDist;
                }

                opponent.CurrentBestLapTime = vehicleScoring.mBestLapTime > 0.0f ? (float)vehicleScoring.mBestLapTime : -1.0f;
                opponent.PreviousBestLapTime = opponentPrevious != null && opponentPrevious.CurrentBestLapTime > 0.0f &&
                    opponentPrevious.CurrentBestLapTime > opponent.CurrentBestLapTime ? opponentPrevious.CurrentBestLapTime : -1.0f;
                float previousDistanceRoundTrack = opponentPrevious != null ? opponentPrevious.DistanceRoundTrack : 0;
                opponent.bestSector1Time = vehicleScoring.mBestSector1 > 0 ? (float)vehicleScoring.mBestSector1 : -1.0f;
                opponent.bestSector2Time = vehicleScoring.mBestSector2 > 0 && vehicleScoring.mBestSector1 > 0.0f ? (float)(vehicleScoring.mBestSector2 - vehicleScoring.mBestSector1) : -1.0f;
                opponent.bestSector3Time = vehicleScoring.mBestLapTime > 0 && vehicleScoring.mBestSector2 > 0.0f ? (float)(vehicleScoring.mBestLapTime - vehicleScoring.mBestSector2) : -1.0f;
                opponent.LastLapTime = vehicleScoring.mLastLapTime > 0 ? (float)vehicleScoring.mLastLapTime : -1.0f;

                var isInPits = vehicleScoring.mInPits == 1;
                if (!opponent.InPits && isInPits)
                    opponent.NumPitStops++;

                opponent.InPits = isInPits;

                opponent.hasJustChangedToDifferentTyreType = false;
                if (opponent.InPits)
                {
                    if (opponentPrevious == null 
                        || opponent.CurrentTyres != opponentPrevious.CurrentTyres)
                    {
                        opponent.TyreChangesByLap[opponent.OpponentLapData.Count] = opponent.CurrentTyres;
                        opponent.hasJustChangedToDifferentTyreType = true;
                    }
                }

                var lastSectorTime = this.getLastSectorTime(ref vehicleScoring, opponent.CurrentSectorNumber);

                bool lapValid = true;
                if (vehicleScoring.mCountLapFlag != 2)
                    lapValid = false;

                if (opponent.IsNewLap)
                {
                    if (lastSectorTime > 0.0f)
                    {
                        opponent.CompleteLapWithProvidedLapTime(
                            opponent.Position,
                            csd.SessionRunningTime,
                            opponent.LastLapTime,
                            lapValid,  // TODO: revisit
                            shared.scoring.mScoringInfo.mRaining > minRainThreshold,
                            (float)shared.scoring.mScoringInfo.mTrackTemp,
                            (float)shared.scoring.mScoringInfo.mAmbientTemp,
                            csd.SessionHasFixedTime,
                            csd.SessionTimeRemaining,
                            3);
                    }
                    opponent.StartNewLap(
                        opponent.CompletedLaps + 1,
                        opponent.Position,
                        vehicleScoring.mInPits == 1 || opponent.DistanceRoundTrack < 0.0f,
                        csd.SessionRunningTime,
                        shared.scoring.mScoringInfo.mRaining > minRainThreshold,
                        (float)shared.scoring.mScoringInfo.mTrackTemp,
                        (float)shared.scoring.mScoringInfo.mAmbientTemp);
                }
                else if (isNewSector && lastSectorTime > 0.0f)
                {
                    opponent.AddCumulativeSectorData(
                        opponentPrevious.CurrentSectorNumber,
                        opponent.Position,
                        lastSectorTime,
                        csd.SessionRunningTime,
                        lapValid,
                        shared.scoring.mScoringInfo.mRaining > minRainThreshold,
                        (float)shared.scoring.mScoringInfo.mTrackTemp,
                        (float)shared.scoring.mScoringInfo.mAmbientTemp);
                }

                if (vehicleScoring.mInPits == 1
                    && opponent.CurrentSectorNumber == 3
                    && opponentPrevious != null
                    && !opponentPrevious.isEnteringPits())
                {
                    opponent.setInLap();
                    var currentLapData = opponent.getCurrentLapData();
                    int sector3Position = currentLapData != null && currentLapData.SectorPositions[2] > 0
                                            ? currentLapData.SectorPositions[2]
                                            : opponent.Position;

                    if (sector3Position == 1)
                    {
                        cgs.PitData.LeaderIsPitting = true;
                        cgs.PitData.OpponentForLeaderPitting = opponent;
                    }

                    if (sector3Position == csd.Position - 1 && csd.Position > 2)
                    {
                        cgs.PitData.CarInFrontIsPitting = true;
                        cgs.PitData.OpponentForCarAheadPitting = opponent;
                    }

                    if (sector3Position == csd.Position + 1 && !cgs.isLast())
                    {
                        cgs.PitData.CarBehindIsPitting = true;
                        cgs.PitData.OpponentForCarBehindPitting = opponent;
                    }
                }

                if (opponentPrevious != null
                    && opponentPrevious.Position > 1
                    && opponent.Position == 1)
                {
                    csd.HasLeadChanged = true;
                }

                // session best lap times
                if (opponent.Position == csd.Position + 1)
                    csd.TimeDeltaBehind = (float)Math.Abs(vehicleScoring.mTimeBehindNext);

                if (opponent.CurrentBestLapTime > 0.0f
                    && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestOverall
                        || csd.OpponentsLapTimeSessionBestOverall < 0.0f))
                {
                    csd.OpponentsLapTimeSessionBestOverall = opponent.CurrentBestLapTime;
                }

                if (opponent.CurrentBestLapTime > 0.0f
                    && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestPlayerClass
                        || csd.OpponentsLapTimeSessionBestPlayerClass < 0.0f)
                    && CarData.IsCarClassEqual(opponent.CarClass, cgs.carClass))
                {
                    csd.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;

                    if (csd.OpponentsLapTimeSessionBestPlayerClass < csd.PlayerClassSessionBestLapTime)
                        csd.PlayerClassSessionBestLapTime = csd.OpponentsLapTimeSessionBestPlayerClass;

                    if (opponent.IsNewLap && opponentPrevious != null && !opponentPrevious.IsNewLap)
                    {
                        if (opponent.LastLapTime > 0.0
                            && opponent.LastLapValid
                            && (!csd.PlayerClassSessionBestLapTimeByTyre.ContainsKey(opponent.CurrentTyres)
                                || csd.PlayerClassSessionBestLapTimeByTyre[opponent.CurrentTyres] > opponent.LastLapTime))
                        {
                            csd.PlayerClassSessionBestLapTimeByTyre[opponent.CurrentTyres] = opponent.LastLapTime;
                        }
                    }
                }

                if (opponent.CurrentBestLapTime > 0.0f
                    && (opponent.CurrentBestLapTime < csd.OverallSessionBestLapTime
                        || csd.OverallSessionBestLapTime < 0.0f))
                {
                    csd.OverallSessionBestLapTime = opponent.CurrentBestLapTime;
                }

                if (opponentPrevious != null)
                {
                    opponent.trackLandmarksTiming = opponentPrevious.trackLandmarksTiming;
                    String stoppedInLandmark = opponent.trackLandmarksTiming.updateLandmarkTiming(csd.TrackDefinition,
                        csd.SessionRunningTime, previousDistanceRoundTrack, opponent.DistanceRoundTrack, opponent.Speed);
                    opponent.stoppedInLandmark = opponent.InPits ? null : stoppedInLandmark;
                }

                if (opponent.IsNewLap)
                    opponent.trackLandmarksTiming.cancelWaitingForLandmarkEnd();

                // shouldn't have duplicates, but just in case
                if (!cgs.OpponentData.ContainsKey(opponentKey))
                    cgs.OpponentData.Add(opponentKey, opponent);
            }

            if (pgs != null)
            {
                csd.HasLeadChanged = !csd.HasLeadChanged && psd.Position > 1 && csd.Position == 1 ? true : csd.HasLeadChanged;
                csd.IsRacingSameCarInFront = String.Equals(pgs.getOpponentKeyInFront(false), cgs.getOpponentKeyInFront(false));
                csd.IsRacingSameCarBehind = String.Equals(pgs.getOpponentKeyBehind(false), cgs.getOpponentKeyBehind(false));
                csd.GameTimeAtLastPositionFrontChange = !csd.IsRacingSameCarInFront ? csd.SessionRunningTime : psd.GameTimeAtLastPositionFrontChange;
                csd.GameTimeAtLastPositionBehindChange = !csd.IsRacingSameCarBehind ? csd.SessionRunningTime : psd.GameTimeAtLastPositionBehindChange;

                csd.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                String stoppedInLandmark = csd.trackLandmarksTiming.updateLandmarkTiming(csd.TrackDefinition,
                    csd.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack, cgs.PositionAndMotionData.DistanceRoundTrack,
                    cgs.PositionAndMotionData.CarSpeed);
                cgs.SessionData.stoppedInLandmark = cgs.PitData.InPitlane ? null : stoppedInLandmark;
                if (csd.IsNewLap)
                    csd.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
            }

            // --------------------------------
            // fuel data
            cgs.FuelData.FuelUseActive = shared.extended.mPhysics.mFuelMult > 0;
            cgs.FuelData.FuelLeft = (float)playerTelemetry.mFuel;

            // --------------------------------
            // flags data
            // TODO: should RF2 ever drop back to the improvised incident calling?
            cgs.FlagData.useImprovisedIncidentCalling = false;

            cgs.FlagData.isFullCourseYellow = csd.SessionPhase == SessionPhase.FullCourseYellow
                || shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.Resume;

            if (shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.Resume)
            {
                // Special case for resume after FCY.  rF2 no longer has FCY set, but still has Resume sub phase set.
                cgs.FlagData.fcyPhase = FullCourseYellowPhase.RACING;
                cgs.FlagData.lapCountWhenLastWentGreen = cgs.SessionData.CompletedLaps;
            }
            else if (cgs.FlagData.isFullCourseYellow)
            {
                if (shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.Pending)
                    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PENDING;
                //else if (shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitClosed)
                //    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PITS_CLOSED;
                // At default ruleset, both open and close sub states result in "Pits open" visible in the UI.
                else if (shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitOpen
                    || shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitClosed)
                    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PITS_OPEN;  // TODO: use mAllowedToPit from vehicle rules to distinguish here.  Seems like 2 is Closed 3 is Open.
                else if (shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitLeadLap)
                    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES;
                else if (shared.scoring.mScoringInfo.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.LastLap)
                {
                    if (pgs != null)
                    {
                        if (pgs.FlagData.fcyPhase != FullCourseYellowPhase.LAST_LAP_NEXT && pgs.FlagData.fcyPhase != FullCourseYellowPhase.LAST_LAP_CURRENT)
                            // Initial last lap phase
                            cgs.FlagData.fcyPhase = FullCourseYellowPhase.LAST_LAP_NEXT;
                        else if (csd.CompletedLaps != psd.CompletedLaps && pgs.FlagData.fcyPhase == FullCourseYellowPhase.LAST_LAP_NEXT)
                            // Once we reach the end of current lap, and this lap is next last lap, switch to last lap current phase.
                            cgs.FlagData.fcyPhase = FullCourseYellowPhase.LAST_LAP_CURRENT;
                        else
                            // Keep previous FCY last lap phase.
                            cgs.FlagData.fcyPhase = pgs.FlagData.fcyPhase;

                    }
                }
            }

            if (csd.SessionPhase == SessionPhase.Green)
            {
                for (int i = 0; i < 3; ++i)
                {
                    // Mark Yellow sectors.
                    if (shared.scoring.mScoringInfo.mSectorFlag[i] == (int)rFactor2Constants.rF2YellowFlagState.Pending)
                        cgs.FlagData.sectorFlags[i] = FlagEnum.YELLOW;
                }
            }

            var currFlag = FlagEnum.UNKNOWN;

            if (GlobalBehaviourSettings.useAmericanTerms
                && !cgs.FlagData.isFullCourseYellow)  // Don't announce White flag under FCY.
            {
                // Only works correctly if race is not timed.
                if ((csd.SessionType == SessionType.Race || csd.SessionType == SessionType.Qualify)
                    && csd.SessionPhase == SessionPhase.Green
                    && (playerScoring.mTotalLaps == csd.SessionNumberOfLaps - 1) || csd.LeaderHasFinishedRace)
                {
                    currFlag = FlagEnum.WHITE;
                }
            }

            if (playerScoring.mFlag == (byte)rFactor2Constants.rF2PrimaryFlag.Blue)
                currFlag = FlagEnum.BLUE;
            else if (this.enableBlueOnSlower
                && !cgs.FlagData.isFullCourseYellow)  // Don't announce blue on slower under FCY.
            {
                foreach (var opponent in cgs.OpponentData.Values)
                {
                    if (csd.SessionType != SessionType.Race
                        || csd.CompletedLaps < 1
                        || cgs.PositionAndMotionData.DistanceRoundTrack < 0.0f)
                    {
                        break;
                    }

                    if (opponent.getCurrentLapData().InLap
                        || opponent.getCurrentLapData().OutLap
                        || opponent.Position > csd.Position)
                    {
                        continue;
                    }

                    if (isBehindWithinDistance(csd.TrackDefinition.trackLength, 8.0f, 40.0f,
                            cgs.PositionAndMotionData.DistanceRoundTrack, opponent.DistanceRoundTrack)
                        && opponent.Speed >= cgs.PositionAndMotionData.CarSpeed)
                    {
                        currFlag = FlagEnum.BLUE;
                        break;
                    }
                }
            }

            if (csd.IsDisqualified
                && pgs != null
                && !psd.IsDisqualified)
            {
                currFlag = FlagEnum.BLACK;
            }

            csd.Flag = currFlag;

            // --------------------------------
            // penalties data
            cgs.PenaltiesData.NumPenalties = playerScoring.mNumPenalties;
            float lateralDistDiff = (float)(Math.Abs(playerScoring.mPathLateral) - Math.Abs(playerScoring.mTrackEdge));
            cgs.PenaltiesData.IsOffRacingSurface = !cgs.PitData.InPitlane && lateralDistDiff >= 2;
            float offTrackDistanceDelta = lateralDistDiff - this.distanceOffTrack;
            this.distanceOffTrack = cgs.PenaltiesData.IsOffRacingSurface ? lateralDistDiff : 0;
            this.isApproachingTrack = offTrackDistanceDelta < 0 && cgs.PenaltiesData.IsOffRacingSurface && lateralDistDiff < 3;

            // --------------------------------
            // console output
            if (csd.IsNewSession)
            {
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("EventIndex " + csd.EventIndex);
                Console.WriteLine("SessionType " + csd.SessionType);
                Console.WriteLine("SessionPhase " + csd.SessionPhase);
                Console.WriteLine("SessionIteration " + csd.SessionIteration);
                Console.WriteLine("HasMandatoryPitStop " + cgs.PitData.HasMandatoryPitStop);
                Console.WriteLine("PitWindowStart " + cgs.PitData.PitWindowStart);
                Console.WriteLine("PitWindowEnd " + cgs.PitData.PitWindowEnd);
                Console.WriteLine("NumCarsAtStartOfSession " + csd.NumCarsAtStartOfSession);
                Console.WriteLine("SessionNumberOfLaps " + csd.SessionNumberOfLaps);
                Console.WriteLine("SessionRunTime " + csd.SessionTotalRunTime);
                Console.WriteLine("SessionStartPosition " + csd.SessionStartPosition);
                Console.WriteLine("SessionStartTime " + csd.SessionStartTime);
                Console.WriteLine("TrackName " + csd.TrackDefinition.name);
                Console.WriteLine("Player is using car class " + cgs.carClass.getClassIdentifier() +
                    " at position " + csd.Position.ToString());

                Utilities.TraceEventClass(cgs);
            }
            if (pgs != null && psd.SessionPhase != csd.SessionPhase)
            {
                Console.WriteLine("SessionPhase changed from " + psd.SessionPhase +
                    " to " + csd.SessionPhase);
                if (csd.SessionPhase == SessionPhase.Checkered ||
                    csd.SessionPhase == SessionPhase.Finished)
                {
                    Console.WriteLine("Checkered - completed " + csd.CompletedLaps +
                        " laps, session running time = " + csd.SessionRunningTime);
                }
            }
            if (pgs != null && !psd.LeaderHasFinishedRace && csd.LeaderHasFinishedRace)
            {
                Console.WriteLine("Leader has finished race, player has done " + csd.CompletedLaps +
                    " laps, session time = " + csd.SessionRunningTime);
            }
            return cgs;
        }

        private static double getVehicleSpeed(ref rF2VehicleTelemetry vehicleTelemetry)
        {
            return Math.Sqrt((vehicleTelemetry.mLocalVel.x * vehicleTelemetry.mLocalVel.x)
                + (vehicleTelemetry.mLocalVel.y * vehicleTelemetry.mLocalVel.y)
                + (vehicleTelemetry.mLocalVel.z * vehicleTelemetry.mLocalVel.z));
        }

        private static double getVehicleSpeed(ref rF2VehicleScoring vehicleScoring)
        {
            return Math.Sqrt((vehicleScoring.mLocalVel.x * vehicleScoring.mLocalVel.x)
                + (vehicleScoring.mLocalVel.y * vehicleScoring.mLocalVel.y)
                + (vehicleScoring.mLocalVel.z * vehicleScoring.mLocalVel.z));
        }

        private static double getEstimatedLapDist(RF2SharedMemoryReader.RF2StructWrapper shared, ref rF2VehicleScoring vehicleScoring, ref rF2VehicleTelemetry vehicleTelemetry)
        {
            // Estimate lapdist
            // See how much ahead telemetry is ahead of scoring update
            // TODO: experiment with pick speed from telemetry.
            var delta = vehicleTelemetry.mElapsedTime - shared.scoring.mScoringInfo.mCurrentET;
            var lapDistEstimated = vehicleScoring.mLapDist;
            if (delta > 0.0)
            {
                var localZAccelEstimated = vehicleScoring.mLocalAccel.z * delta;
                var localZVelEstimated = vehicleScoring.mLocalVel.z + localZAccelEstimated;

                lapDistEstimated = vehicleScoring.mLapDist - localZVelEstimated * delta;
            }

            return lapDistEstimated;
        }

        private void processPlayerTimingData(
            ref rF2Scoring scoring,
            GameStateData currentGameState,
            GameStateData previousGameState,
            ref rF2VehicleScoring playerScoring)
        {
            var cgs = currentGameState;
            var csd = cgs.SessionData;
            var psd = previousGameState != null ? previousGameState.SessionData : null;

            // Clear all the timings one new session.
            if (csd.IsNewSession)
                return;

            Debug.Assert(psd != null);

            /////////////////////////////////////
            // Current lap timings
            csd.LapTimeCurrent = csd.SessionRunningTime - (float)playerScoring.mLapStartET;
            csd.LapTimePrevious = playerScoring.mLastLapTime > 0.0f ? (float)playerScoring.mLastLapTime : -1.0f;

            // Last (most current) per-sector times:
            // NOTE: this logic still misses invalid sector handling.
            var lastS1Time = playerScoring.mLastSector1 > 0.0 ? playerScoring.mLastSector1 : -1.0;
            var lastS2Time = playerScoring.mLastSector1 > 0.0 && playerScoring.mLastSector2 > 0.0
                ? playerScoring.mLastSector2 - playerScoring.mLastSector1 : -1.0;
            var lastS3Time = playerScoring.mLastSector2 > 0.0 && playerScoring.mLastLapTime > 0.0
                ? playerScoring.mLastLapTime - playerScoring.mLastSector2 : -1.0;

            csd.LastSector1Time = (float)lastS1Time;
            csd.LastSector2Time = (float)lastS2Time;
            csd.LastSector3Time = (float)lastS3Time;

            // Check if we have more current values for S1 and S2.
            // S3 always equals to lastS3Time.
            if (playerScoring.mCurSector1 > 0.0)
                csd.LastSector1Time = (float)playerScoring.mCurSector1;

            if (playerScoring.mCurSector1 > 0.0 && playerScoring.mCurSector2 > 0.0)
                csd.LastSector2Time = (float)(playerScoring.mCurSector2 - playerScoring.mCurSector1);

            // Verify lap is valid
            // TODO: Apply something similar to opponents.
            // First, verify if previous sector has invalid time.
            if (((csd.SectorNumber == 2 && csd.LastSector1Time < 0.0f
                    || csd.SectorNumber == 3 && csd.LastSector2Time < 0.0f)
                // And, this is not an out/in lap
                && !cgs.PitData.OnOutLap && !cgs.PitData.OnInLap
                // And it's Race or Qualification
                && (csd.SessionType == SessionType.Race || csd.SessionType == SessionType.Qualify)))
            {
                csd.CurrentLapIsValid = false;
            }
            // If current lap was marked as invalid, keep it that way.
            else if (psd.CompletedLaps == csd.CompletedLaps  // Same lap
                     && !psd.CurrentLapIsValid)
            {
                csd.CurrentLapIsValid = false;
            }
            // rF2 lap time or whole lap won't count
            else if (playerScoring.mCountLapFlag != (byte)rFactor2Constants.rF2CountLapFlag.CountLapAndTime
                // And, this is not an out/in lap
                && !cgs.PitData.OnOutLap && !cgs.PitData.OnInLap)
            {
                csd.CurrentLapIsValid = false;
            }

            // Check if timing update is needed.
            if (!csd.IsNewLap && !csd.IsNewSector)
                return;

            /////////////////////////////////////////
            // Update Sector/Lap timings.
            var lastSectorTime = this.getLastSectorTime(ref playerScoring, csd.SectorNumber);

            if (csd.IsNewLap)
            {
                if (lastSectorTime > 0.0f)
                {
                    csd.playerCompleteLapWithProvidedLapTime(
                        csd.Position,
                        csd.SessionRunningTime,
                        csd.LapTimePrevious,
                        csd.CurrentLapIsValid,
                        scoring.mScoringInfo.mRaining > minRainThreshold,
                        (float)scoring.mScoringInfo.mTrackTemp,
                        (float)scoring.mScoringInfo.mAmbientTemp,
                        csd.SessionHasFixedTime,
                        csd.SessionTimeRemaining,
                        3);
                }

                csd.playerStartNewLap(
                    csd.CompletedLaps + 1,
                    csd.Position,
                    playerScoring.mInPits == 1 || currentGameState.PositionAndMotionData.DistanceRoundTrack < 0.0f,
                    csd.SessionRunningTime,
                    scoring.mScoringInfo.mRaining > minRainThreshold,
                    (float)scoring.mScoringInfo.mTrackTemp,
                    (float)scoring.mScoringInfo.mAmbientTemp);
            }
            else if (csd.IsNewSector && lastSectorTime > 0.0f)
            {
                csd.playerAddCumulativeSectorData(
                    psd.SectorNumber,
                    csd.Position,
                    lastSectorTime,
                    csd.SessionRunningTime,
                    csd.CurrentLapIsValid,
                    scoring.mScoringInfo.mRaining > minRainThreshold,
                    (float)scoring.mScoringInfo.mTrackTemp,
                    (float)scoring.mScoringInfo.mAmbientTemp);
            }
        }

        private float getLastSectorTime(ref rF2VehicleScoring vehicle, int currSector)
        {
            var lastSectorTime = -1.0f;
            if (currSector == 1)
                lastSectorTime = vehicle.mLastLapTime > 0.0f ? (float)vehicle.mLastLapTime : -1.0f;
            else if (currSector == 2)
            {
                lastSectorTime = vehicle.mLastSector1 > 0.0f ? (float)vehicle.mLastSector1 : -1.0f;

                if (vehicle.mCurSector1 > 0.0)
                    lastSectorTime = (float)vehicle.mCurSector1;
            }
            else if (currSector == 3)
            {
                lastSectorTime = vehicle.mLastSector2 > 0.0f ? (float)vehicle.mLastSector2 : -1.0f;

                if (vehicle.mCurSector2 > 0.0)
                    lastSectorTime = (float)vehicle.mCurSector2;
            }

            return lastSectorTime;
        }

#if DEBUG
        // NOTE: This can be made generic for all sims, but I am not sure if anyone needs this but me
        private static void writeDebugMsg(string msg)
        {
            Console.WriteLine("DEBUG_MSG: " +  msg);
        }

        private static void writeSpinningLockingDebugMsg(GameStateData cgs, double frontLeftRotation, double frontRightRotation, 
            double rearLeftRotation, double rearRightRotation, float minRotatingSpeed, float maxRotatingSpeed)
        {
            if (cgs.TyreData.LeftFrontIsLocked)
                RF2GameStateMapper.writeDebugMsg($"Left Front is locked.  minRotatingSpeed: {minRotatingSpeed:N3}  mRotation: {frontLeftRotation:N3}");
            if (cgs.TyreData.RightFrontIsLocked)
                RF2GameStateMapper.writeDebugMsg($"Right Front is locked.  minRotatingSpeed: {minRotatingSpeed:N3}  mRotation: {frontRightRotation:N3}");
            if (cgs.TyreData.LeftRearIsLocked)
                RF2GameStateMapper.writeDebugMsg($"Left Rear is locked.  minRotatingSpeed: {minRotatingSpeed:N3}  mRotation: {rearLeftRotation:N3}");
            if (cgs.TyreData.RightRearIsLocked)
                RF2GameStateMapper.writeDebugMsg($"Right Rear is locked.  minRotatingSpeed: {minRotatingSpeed:N3}  mRotation: {rearRightRotation:N3}");
            if (cgs.TyreData.LeftFrontIsSpinning)
                RF2GameStateMapper.writeDebugMsg($"Left Front is spinning.  maxRotatingSpeed: {maxRotatingSpeed:N3}  mRotation: {frontLeftRotation:N3}");
            if (cgs.TyreData.RightFrontIsSpinning)
                RF2GameStateMapper.writeDebugMsg($"Right Front is spinning.  maxRotatingSpeed: {maxRotatingSpeed:N3}  mRotation: {frontRightRotation:N3}");
            if (cgs.TyreData.LeftRearIsSpinning)
                RF2GameStateMapper.writeDebugMsg($"Left Rear is spinning.  maxRotatingSpeed: {maxRotatingSpeed:N3}  mRotation: {rearLeftRotation:N3}");
            if (cgs.TyreData.RightRearIsSpinning)
                RF2GameStateMapper.writeDebugMsg($"Right Rear is spinning.  maxRotatingSpeed: {maxRotatingSpeed:N3}  mRotation: {rearRightRotation:N3}");
        }
#endif

        private PitWindow mapToPitWindow(rFactor2Constants.rF2YellowFlagState pitWindow)
        {
            // it seems that the pit window is only truly open on multiplayer races?
            if (this.isOfflineSession)
            {
                return PitWindow.Open;
            }
            switch (pitWindow)
            {
                case rFactor2Constants.rF2YellowFlagState.PitClosed:
                    return PitWindow.Closed;
                case rFactor2Constants.rF2YellowFlagState.PitOpen:
                case rFactor2Constants.rF2YellowFlagState.PitLeadLap:
                    return PitWindow.Open;
                default:
                    return PitWindow.Unavailable;
            }
        }


        private SessionPhase mapToSessionPhase(
            rFactor2Constants.rF2GamePhase sessionPhase,
            SessionType sessionType,
            ref rF2VehicleScoring player)
        {
            switch (sessionPhase)
            {
                case rFactor2Constants.rF2GamePhase.Countdown:
                    return SessionPhase.Countdown;
                // warmUp never happens, but just in case
                case rFactor2Constants.rF2GamePhase.WarmUp:
                case rFactor2Constants.rF2GamePhase.Formation:
                    return SessionPhase.Formation;
                case rFactor2Constants.rF2GamePhase.Garage:
                    return SessionPhase.Garage;
                case rFactor2Constants.rF2GamePhase.GridWalk:
                    return SessionPhase.Gridwalk;
                // sessions never go to sessionStopped, they always go straight from greenFlag to sessionOver
                case rFactor2Constants.rF2GamePhase.SessionStopped:
                case rFactor2Constants.rF2GamePhase.SessionOver:
                    if (sessionType == SessionType.Race
                        && player.mFinishStatus == (sbyte)rFactor2Constants.rF2FinishStatus.None)
                    {
                        return SessionPhase.Checkered;
                    }
                    else
                    {
                        return SessionPhase.Finished;
                    }
                // fullCourseYellow will count as greenFlag since we'll call it out in the Flags separately anyway
                case rFactor2Constants.rF2GamePhase.FullCourseYellow:
                    return SessionPhase.FullCourseYellow;
                case rFactor2Constants.rF2GamePhase.GreenFlag:
                    return SessionPhase.Green;
                default:
                    return SessionPhase.Unavailable;
            }
        }

        // finds OpponentData key for given vehicle based on driver name, vehicle class, and world position
        private String getOpponentKeyForVehicleInfo(ref rF2VehicleScoring vehicleScoring, ref rF2VehicleTelemetry vehicleTelemetry, GameStateData previousGameState, float sessionRunningTime, String driverName, int duplicatesCount, bool vehicleTelemetryAvailable)
        {
            if (previousGameState == null)
                return null;

            List<string> possibleKeys = new List<string>();
            for (int i = 1; i <= duplicatesCount; i++)
                possibleKeys.Add(driverName + "_duplicate_ " + i);

            float[] worldPos = null;
            if (vehicleTelemetryAvailable)
                worldPos = new float[] { (float)vehicleTelemetry.mPos.x, (float)vehicleTelemetry.mPos.z };
            else
                worldPos = new float[] { (float)vehicleScoring.mPos.x, (float)vehicleScoring.mPos.z };

            float minDistDiff = -1.0f;
            float timeDelta = sessionRunningTime - previousGameState.SessionData.SessionRunningTime;
            String bestKey = null;
            if (timeDelta >= 0.0f)
            {
                foreach (String possibleKey in possibleKeys)
                {
                    if (previousGameState.OpponentData.ContainsKey(possibleKey))
                    {
                        OpponentData o = previousGameState.OpponentData[possibleKey];
                        if (o.DriverRawName != getStringFromBytes(vehicleScoring.mDriverName).ToLower() ||
                            o.CarClass != CarData.getCarClassForClassName(getStringFromBytes(vehicleScoring.mVehicleClass)) ||
                            opponentKeysProcessed.Contains(possibleKey))
                        {
                            continue;
                        }
                        // distance from predicted position
                        float targetDist = o.Speed * timeDelta;
                        float dist = (float)Math.Abs(Math.Sqrt((double)((o.WorldPosition[0] - worldPos[0]) * (o.WorldPosition[0] - worldPos[0]) +
                            (o.WorldPosition[1] - worldPos[1]) * (o.WorldPosition[1] - worldPos[1]))) - targetDist);

                        if (minDistDiff < 0.0f || dist < minDistDiff)
                        {
                            minDistDiff = dist;
                            bestKey = possibleKey;
                        }
                    }
                }
            }

            if (bestKey != null)
                opponentKeysProcessed.Add(bestKey);

            return bestKey;
        }

        public SessionType mapToSessionType(Object wrapper)
        {
            var shared = wrapper as CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper;
            switch (shared.scoring.mScoringInfo.mSession)
            {
                // up to four possible practice sessions
                case 1:
                case 2:
                case 3:
                case 4:
                // test day and pre-race warm-up sessions are 'Practice' as well
                case 0:
                case 9:
                    return SessionType.Practice;
                // up to four possible qualifying sessions
                case 5:
                case 6:
                case 7:
                case 8:
                    return SessionType.Qualify;
                // up to four possible race sessions
                case 10:
                case 11:
                case 12:
                case 13:
                    return SessionType.Race;
                default:
                    return SessionType.Unavailable;
            }
        }

        private TyreType mapToTyreType(ref rF2VehicleTelemetry vehicleTelemetry)
        {
            // For now, use fronts.
            var frontCompound = RF2GameStateMapper.getStringFromBytes(vehicleTelemetry.mFrontTireCompoundName).ToUpperInvariant();

            if (string.IsNullOrWhiteSpace(frontCompound))
                return TyreType.Unknown_Race;
            else if (frontCompound.Contains("HARD"))
                return TyreType.Hard;
            else if (frontCompound.Contains("MEDIUM"))
                return TyreType.Medium;
            else if (frontCompound.Contains("SOFT"))
            {
                if (frontCompound.Contains("SUPER"))
                    return TyreType.Super_Soft;
                else if (frontCompound.Contains("ULTRA"))
                    return TyreType.Ultra_Soft;

                return TyreType.Soft;
            }
            else if (frontCompound.Contains("WET"))
                return TyreType.Wet;
            else if (frontCompound.Contains("INTERMEDIATE"))
                return TyreType.Intermediate;
            else if (frontCompound.Contains("BIAS") && frontCompound.Contains("PLY"))
                return TyreType.Bias_Ply;
            else if (frontCompound.Contains("PRIME"))
                return TyreType.Prime;
            else if (frontCompound.Contains("OPTION"))
                return TyreType.Option;
            else if (frontCompound.Contains("ALTERNATE"))
                return TyreType.Alternate;
            else if (frontCompound.Contains("PRIMARY"))
                return TyreType.Primary;

            return TyreType.Unknown_Race;
        }

        private ControlType mapToControlType(rFactor2Constants.rF2Control controlType)
        {
            switch (controlType)
            {
                case rFactor2Constants.rF2Control.AI:
                    return ControlType.AI;
                case rFactor2Constants.rF2Control.Player:
                    return ControlType.Player;
                case rFactor2Constants.rF2Control.Remote:
                    return ControlType.Remote;
                case rFactor2Constants.rF2Control.Replay:
                    return ControlType.Replay;
                default:
                    return ControlType.Unavailable;
            }
        }

        public Boolean isBehindWithinDistance(float trackLength, float minDistance, float maxDistance, float playerTrackDistance, float opponentTrackDistance)
        {
            float difference = playerTrackDistance - opponentTrackDistance;
            if (difference > 0)
            {
                return difference < maxDistance && difference > minDistance;
            }
            else
            {
                difference = (playerTrackDistance + trackLength) - opponentTrackDistance;
                return difference < maxDistance && difference > minDistance;
            }
        }

        public static String getStringFromBytes(byte[] bytes)
        {
            var nullIdx = Array.IndexOf(bytes, (byte)0);

            return nullIdx >= 0
              ? Encoding.Default.GetString(bytes, 0, nullIdx)
              : Encoding.Default.GetString(bytes);
        }

        // Since it is not clear if there's any guarantee around vehicle telemetry update order in rF2
        // I don't want to assume mID == i in telemetry.mVehicles.  Also, telemetry is updated separately from scoring,
        // so it is possible order changes in between updates.  Lastly, it is possible scoring to contain mID, but not
        // telemetry.  So, build a lookup map of mID -> i into telemetry mVehicles, for lookup between scoring.mVehicles[].mID
        // into telemetry.
        public static Dictionary<long, int> getIdsToTelIndicesMap(ref rF2Telemetry telemetry)
        {
            var idsToTelIndices = new Dictionary<long, int>();
            for (int i = 0; i < telemetry.mNumVehicles; ++i)
            {
                if (!idsToTelIndices.ContainsKey(telemetry.mVehicles[i].mID))
                    idsToTelIndices.Add(telemetry.mVehicles[i].mID, i);
            }

            return idsToTelIndices;
        }

        // Vehicle telemetry is not always available (before sesssion start).  Instead of
        // hardening code against this case, create and zero intialize arrays within passed in object.
        // This is equivalent of how V1 and rF1 works.
        // NOTE: not a complete initialization, just parts that were cause NRE.
        public static void initEmptyVehicleTelemetry(ref rF2VehicleTelemetry vehicleTelemetry)
        {
            Debug.Assert(vehicleTelemetry.mWheels == null);

            vehicleTelemetry.mWheels = new rF2Wheel[4];
            for (int i = 0; i < 4; ++i)
                vehicleTelemetry.mWheels[i].mTemperature = new double[3];
        }
    }
}
