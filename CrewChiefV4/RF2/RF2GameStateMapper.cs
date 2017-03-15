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

        public static String playerName = null;

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();

        private float scrubbedTyreWearPercent = 5.0f;
        private float minorTyreWearPercent = 30.0f;
        private float majorTyreWearPercent = 60.0f;
        private float wornOutTyreWearPercent = 85.0f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;

        // At which point we consider that it is raining
        private const double minRainThreshold = 0.1;

        // if we're running only against AI, force the pit window to open
        private Boolean isOfflineSession = true;

        // keep track of opponents processed this time
        private List<String> opponentKeysProcessed = new List<String>();

        // detect when approaching racing surface after being off track
        private float distanceOffTrack = 0.0f;
        private Boolean isApproachingTrack = false;

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

        private int[] minimumSupportedVersionParts = new int[] { 1, 1, 0, 1 };
        private bool pluginSupported = false;
        public void versionCheck(Object memoryMappedFileStruct)
        {
            if (this.pluginSupported)
                return;

            var wrapper = (CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)memoryMappedFileStruct;
            var versionStr = getStringFromBytes(wrapper.state.mVersion);

            var versionParts = versionStr.Split('.');
            if (versionParts.Length != 4)
            {
                var msg = "Corrupt rFactor 2 Shared Memory version string: " + versionStr;
                Console.WriteLine(msg);
                return;
                //throw new GameDataReadException(msg);
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
                    //throw new GameDataReadException(msg);
                }

                smVer += (versionPart * partFactor);
                minVer += (this.minimumSupportedVersionParts[i] * partFactor);
                partFactor *= 100;

            }

            if (smVer < minVer)
            {
                var minVerStr = string.Join(".", this.minimumSupportedVersionParts);
                var msg = "Unsupported rFactor 2 Shared Memory version: "
                    + versionStr
                    + "  Minimum supported version is: "
                    + minVerStr
                    + "  Please update rFactor2SharedMemoryMapPlugin64.dll";
                Console.WriteLine(msg);
                // throw new GameDataReadException("Unsupported rF2 Shared Memory version: " + versionStr + "  Minimum supported version is: " + minVerStr);
            }
            else
            {
                this.pluginSupported = true;

                var msg = "rFactor 2 Shared Memory version: " + versionStr;
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
            var wrapper = (CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)memoryMappedFileStruct;
            var cgs = new GameStateData(wrapper.ticksWhenRead);
            var rf2state = wrapper.state;

            // no session data
            if (rf2state.mNumVehicles == 0)
            {
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

            // game is paused or other window has taken focus
            if (rf2state.mDeltaTime > 0.22)
                return pgs;

            // --------------------------------
            // session data
            // get player scoring info (usually index 0)
            // get session leader scoring info (usually index 1 if not player)
            var player = new rF2VehScoringInfo();
            var leader = new rF2VehScoringInfo();
            for (int i = 0; i < rf2state.mNumVehicles; ++i)
            {
                var vehicle = rf2state.mVehicles[i];
                switch (mapToControlType((rFactor2Constants.rF2Control)vehicle.mControl))
                {
                    case ControlType.AI:
                    case ControlType.Player:
                    case ControlType.Remote:
                        if (vehicle.mIsPlayer == 1)
                            player = vehicle;

                        if (vehicle.mPlace == 1)
                            leader = vehicle;
                        break;
                    default:
                        continue;
                }

                if (player.mIsPlayer == 1 && leader.mPlace == 1)
                    break;
            }

            // can't find the player or session leader vehicle info (replay)
            if (player.mIsPlayer != 1 || leader.mPlace != 1)
                return pgs;

            if (RF2GameStateMapper.playerName == null)
            {
                var driverName = getStringFromBytes(player.mDriverName).ToLower();
                NameValidator.validateName(driverName);
                RF2GameStateMapper.playerName = driverName;
            }

            // these things should remain constant during a session
            var csd = cgs.SessionData;
            var psd = pgs != null ? pgs.SessionData : null;
            csd.EventIndex = rf2state.mSession;

            csd.SessionIteration
                = rf2state.mSession >= 1 && rf2state.mSession <= 4 ? rf2state.mSession - 1 :
                rf2state.mSession >= 5 && rf2state.mSession <= 8 ? rf2state.mSession - 5 :
                rf2state.mSession >= 10 && rf2state.mSession <= 13 ? rf2state.mSession - 10 : 0;

            csd.SessionType = mapToSessionType(rf2state);
            csd.SessionPhase = mapToSessionPhase((rFactor2Constants.rF2GamePhase)rf2state.mGamePhase, csd.SessionType, ref player);
            var carClassId = getStringFromBytes(player.mVehicleClass);
            cgs.carClass = CarData.getCarClassForClassName(carClassId);
            CarData.CLASS_ID = carClassId;
            this.brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(cgs.carClass);
            csd.DriverRawName = getStringFromBytes(player.mDriverName).ToLower();
            csd.TrackDefinition = new TrackDefinition(getStringFromBytes(rf2state.mTrackName), (float)rf2state.mLapDist);
            csd.TrackDefinition.setGapPoints();
            csd.TrackDefinition.trackLandmarks = TrackData.TRACK_LANDMARKS_DATA.getTrackLandmarksForTrackName(csd.TrackDefinition.name);

            csd.SessionNumberOfLaps = rf2state.mMaxLaps > 0 && rf2state.mMaxLaps < 1000 ? rf2state.mMaxLaps : 0;

            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630.0f;
            csd.SessionTotalRunTime
                = (float)rf2state.mEndET > 0.0f
                    ? (float)rf2state.mEndET
                    : csd.SessionNumberOfLaps > 0 ? 0.0f : defaultSessionTotalRunTime;

            // If any difference between current and previous states suggests it is a new session
            if (pgs == null
                || csd.SessionType != psd.SessionType
                || cgs.carClass.getClassIdentifier() != pgs.carClass.getClassIdentifier()
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
                pgs = null;

            csd.SessionStartTime = csd.IsNewSession ? cgs.Now : psd.SessionStartTime;
            csd.SessionHasFixedTime = csd.SessionTotalRunTime > 0.0f;

            csd.SessionRunningTime = (float)rf2state.mElapsedTime;
            csd.SessionTimeRemaining = csd.SessionHasFixedTime ? csd.SessionTotalRunTime - csd.SessionRunningTime : 0.0f;

            // hack for test day sessions running longer than allotted time
            csd.SessionTimeRemaining = csd.SessionTimeRemaining < 0.0f && rf2state.mSession == 0 ? defaultSessionTotalRunTime : csd.SessionTimeRemaining;

            csd.NumCars = rf2state.mNumVehicles;
            csd.NumCarsAtStartOfSession = csd.IsNewSession ? csd.NumCars : psd.NumCarsAtStartOfSession;
            csd.Position = player.mPlace;
            csd.UnFilteredPosition = csd.Position;
            csd.SessionStartPosition = csd.IsNewSession ? csd.Position : psd.SessionStartPosition;
            csd.SectorNumber = player.mSector == 0 ? 3 : player.mSector;
            csd.IsNewSector = csd.IsNewSession || csd.SectorNumber != psd.SectorNumber;
            csd.IsNewLap = csd.IsNewSession || (csd.IsNewSector && csd.SectorNumber == 1);
            csd.PositionAtStartOfCurrentLap = csd.IsNewLap ? csd.Position : psd.PositionAtStartOfCurrentLap;
            // TODO: See if Black Flag handling needed here.
            csd.IsDisqualified = (rFactor2Constants.rF2FinishStatus)player.mFinishStatus == rFactor2Constants.rF2FinishStatus.Dq;
            /*csd.PlayerBestSector1Time = player.mBestSector1 > 0.0f ? (float)player.mBestSector1 : -1.0f;
            csd.PlayerBestSector2Time = player.mBestSector2 > 0.0f && player.mBestSector1 > 0.0f ? (float)(player.mBestSector2 - player.mBestSector1) : -1.0f;
            // TODO: This is incorrect.  We need to store player's lap data to figure this out.
            csd.PlayerBestSector3Time = player.mBestLapTime > 0.0f && player.mBestSector2 > 0.0f ? (float)(player.mBestLapTime - player.mBestSector2) : -1.0f;*/

            // TODO: make suse this is reasonably close to what we calculate during lap tracking
            // csd.PlayerLapTimeSessionBest = player.mBestLapTime > 0.0f ? (float)player.mBestLapTime : -1.0f;
            csd.CompletedLaps = player.mTotalLaps;

            ////////////////////////////////////
            // motion data
            cgs.PositionAndMotionData.CarSpeed = (float)rf2state.mSpeed;
            cgs.PositionAndMotionData.DistanceRoundTrack = (float)player.mLapDist;

            // Is online session?
            this.isOfflineSession = true;
            for (int i = 0; i < rf2state.mNumVehicles; ++i)
            {
                if ((rFactor2Constants.rF2Control)rf2state.mVehicles[i].mControl == rFactor2Constants.rF2Control.Remote)
                    this.isOfflineSession = false;
            }

            ///////////////////////////////////
            // Pit Data
            cgs.PitData.IsRefuellingAllowed = true;

            cgs.PitData.HasMandatoryPitStop = this.isOfflineSession
                && rf2state.mScheduledStops > 0
                && player.mNumPitstops < rf2state.mScheduledStops
                && csd.SessionType == SessionType.Race;

            cgs.PitData.PitWindowStart = this.isOfflineSession && cgs.PitData.HasMandatoryPitStop ? 1 : 0;
            cgs.PitData.PitWindowEnd = !cgs.PitData.HasMandatoryPitStop ? 0 :
                csd.SessionHasFixedTime ? (int)(csd.SessionTotalRunTime / 60 / (rf2state.mScheduledStops + 1)) * (player.mNumPitstops + 1) + 1 :
                (int)(csd.SessionNumberOfLaps / (rf2state.mScheduledStops + 1)) * (player.mNumPitstops + 1) + 1;

            // mInGarageStall also means retired or before race start, but for now use it here.
            cgs.PitData.InPitlane = player.mInPits == 1 || player.mInGarageStall == 1;
            cgs.PitData.IsAtPitExit = pgs != null && pgs.PitData.InPitlane && !cgs.PitData.InPitlane;
            cgs.PitData.OnOutLap = cgs.PitData.InPitlane && csd.SectorNumber == 1;

            if (rf2state.mInRealtimeFC == 0  // Mark pit limiter as unavailable if in Monitor (not real time).
                || rf2state.mSpeedLimiterAvailable == 0)
                cgs.PitData.limiterStatus = -1;
            else
                cgs.PitData.limiterStatus = rf2state.mSpeedLimiter > 0 ? 1 : 0;

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
                ? PitWindow.StopInProgress : mapToPitWindow((rFactor2Constants.rF2YellowFlagState)rf2state.mYellowFlagState);

            ////////////////////////////////////
            // Timings
            this.processPlayerTimingData(ref rf2state, cgs, csd, psd, ref player);

            csd.SessionTimesAtEndOfSectors = pgs != null ? psd.SessionTimesAtEndOfSectors : new SessionData().SessionTimesAtEndOfSectors;

            if (csd.IsNewSector && !csd.IsNewSession)
            {
                // there's a slight delay due to scoring updating every 200 ms, so we can't use SessionRunningTime here
                // TODO: validate if time changes a bit after IsNewSector.
                switch (csd.SectorNumber)
                {
                    case 1:
                        csd.SessionTimesAtEndOfSectors[3]
                            = player.mLapStartET > 0.0f ? (float)player.mLapStartET : -1.0f;
                        break;
                    case 2:
                        csd.SessionTimesAtEndOfSectors[1]
                            = player.mLapStartET > 0.0f && player.mCurSector1 > 0.0f
                                ? (float)(player.mLapStartET + player.mCurSector1)
                                : -1.0f;
                        break;
                    case 3:
                        csd.SessionTimesAtEndOfSectors[2]
                            = player.mLapStartET > 0 && player.mCurSector2 > 0.0f
                                ? (float)(player.mLapStartET + player.mCurSector2)
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

            csd.LeaderHasFinishedRace = leader.mFinishStatus == (int)rFactor2Constants.rF2FinishStatus.Finished;
            csd.TimeDeltaFront = (float)player.mTimeBehindNext;

            // --------------------------------
            // engine data
            cgs.EngineData.EngineRpm = (float)rf2state.mEngineRPM;
            cgs.EngineData.MaxEngineRpm = (float)rf2state.mEngineMaxRPM;
            cgs.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            cgs.EngineData.EngineOilTemp = (float)rf2state.mEngineOilTemp;
            cgs.EngineData.EngineWaterTemp = (float)rf2state.mEngineWaterTemp;
            //HACK: there's probably a cleaner way to do this...
            if (rf2state.mOverheating == 1)
            {
                cgs.EngineData.EngineWaterTemp += 50;
                cgs.EngineData.EngineOilTemp += 50;
            }

            // --------------------------------
            // transmission data
            cgs.TransmissionData.Gear = rf2state.mGear;

            // controls
            cgs.ControlData.BrakePedal = (float)rf2state.mUnfilteredBrake;
            cgs.ControlData.ThrottlePedal = (float)rf2state.mUnfilteredThrottle;
            cgs.ControlData.ClutchPedal = (float)rf2state.mUnfilteredClutch;

            // --------------------------------
            // damage
            cgs.CarDamageData.DamageEnabled = true;

            if (rf2state.mInvulnerable == 0)
            {
                const double MINOR_DAMAGE_THRESHOLD = 1500.0;
                const double MAJOR_DAMAGE_THRESHOLD = 4000.0;
                const double ACCUMULATED_THRESHOLD_FACTOR = 4.0;

                bool anyWheelDetached = false;
                foreach (var wheel in rf2state.mWheels)
                {
                    anyWheelDetached |= wheel.mDetached == 1;
                }

                if (rf2state.mDetached == 1
                    && anyWheelDetached)  // Wheel is not really aero damage, but it is bad situation.
                {
                    // Things are sad if we have both part and wheel detached.
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.DESTROYED;
                }
                else if (rf2state.mDetached == 1  // If there are parts detached, consider damage major, and pit stop is necessary.
                    || rf2state.mMaxImpactMagnitude > MAJOR_DAMAGE_THRESHOLD)  // Also take max impact magnitude into consideration.
                {

                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.MAJOR;
                }
                else if (rf2state.mMaxImpactMagnitude > MINOR_DAMAGE_THRESHOLD
                    || rf2state.mAccumulatedImpactMagnitude > MINOR_DAMAGE_THRESHOLD * ACCUMULATED_THRESHOLD_FACTOR)  // Also consider accumulated damage, if user grinds car against the wall, max won't be high, but car is still damaged.
                {
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.MINOR;
                }
                else if (rf2state.mMaxImpactMagnitude > 0.0)
                {
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    cgs.CarDamageData.OverallAeroDamage = DamageLevel.NONE;
                }
            }
            else  // rf2state.mInvulnerable != 0
            {
                // roll over all you want - it's just a scratch.
                cgs.CarDamageData.OverallAeroDamage = rf2state.mMaxImpactMagnitude > 0.0 ? DamageLevel.TRIVIAL : DamageLevel.NONE;
            }

            // --------------------------------
            // control data
            cgs.ControlData.ControlType = mapToControlType((rFactor2Constants.rF2Control)player.mControl);

            // --------------------------------
            // tire data
            // rF2 reports in Kelvin
            cgs.TyreData.TireWearActive = true;
            var wheelFrontLeft = rf2state.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft];
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

            var wheelFrontRight = rf2state.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight];
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

            var wheelRearLeft = rf2state.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft];
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

            var wheelRearRight = rf2state.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight];
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

            cgs.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[cgs.carClass.defaultTyreType],
                cgs.TyreData.PeakFrontLeftTemperatureForLap, cgs.TyreData.PeakFrontRightTemperatureForLap,
                cgs.TyreData.PeakRearLeftTemperatureForLap, cgs.TyreData.PeakRearRightTemperatureForLap);

            // some simple locking / spinning checks
            if (cgs.PositionAndMotionData.CarSpeed > 7.0f)
            {
                float minRotatingSpeed = 2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / cgs.carClass.maxTyreCircumference;
                cgs.TyreData.LeftFrontIsLocked = Math.Abs(wheelFrontLeft.mRotation) < minRotatingSpeed;
                cgs.TyreData.RightFrontIsLocked = Math.Abs(wheelFrontRight.mRotation) < minRotatingSpeed;
                cgs.TyreData.LeftRearIsLocked = Math.Abs(wheelRearLeft.mRotation) < minRotatingSpeed;
                cgs.TyreData.RightRearIsLocked = Math.Abs(wheelRearRight.mRotation) < minRotatingSpeed;

                float maxRotatingSpeed = 2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / cgs.carClass.minTyreCircumference;
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
                    (float)rf2state.mAmbientTemp, (float)rf2state.mTrackTemp, (float)rf2state.mRaining,
                    (float)Math.Sqrt((double)(rf2state.mWind.x * rf2state.mWind.x + rf2state.mWind.y * rf2state.mWind.y + rf2state.mWind.z * rf2state.mWind.z)), 0, 0, 0);
            }

            // --------------------------------
            // opponent data
            this.opponentKeysProcessed.Clear();

            // first check for duplicates:
            Dictionary<string, int> driverNameCounts = new Dictionary<string, int>();
            Dictionary<string, int> duplicatesCreated = new Dictionary<string, int>();
            for (int i = 0; i < rf2state.mNumVehicles; ++i)
            {
                var vehicle = rf2state.mVehicles[i];
                var driverName = getStringFromBytes(vehicle.mDriverName).ToLower();

                if (driverNameCounts.ContainsKey(driverName))
                    driverNameCounts[driverName] += 1;
                else
                    driverNameCounts.Add(driverName, 1);
            }

            for (int i = 0; i < rf2state.mNumVehicles; ++i)
            {
                var vehicle = rf2state.mVehicles[i];
                if (vehicle.mIsPlayer == 1)
                {
                    csd.OverallSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0.0f ?
                        csd.PlayerLapTimeSessionBest : -1.0f;

                    csd.PlayerClassSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0.0f ?
                        csd.PlayerLapTimeSessionBest : -1.0f;

                    continue;
                }

                var ct = this.mapToControlType((rFactor2Constants.rF2Control)vehicle.mControl);
                if (ct == ControlType.Player || ct == ControlType.Replay || ct == ControlType.Unavailable)
                    continue;

                var driverName = getStringFromBytes(vehicle.mDriverName).ToLower();
                OpponentData opponentPrevious;
                int duplicatesCount = driverNameCounts[driverName];
                String opponentKey;
                if (duplicatesCount > 1)
                {
                    if (!isOfflineSession)
                    {
                        // there shouldn't be duplicate driver names in online sessions. This is probably a temporary glitch in the shared memory data - 
                        // don't panic and drop the existing opponentData for this key - just copy it across to the current state. This prevents us losing
                        // the historical data and repeatedly re-adding this name to the SpeechRecogniser (which is expensive)
                        if (pgs != null && pgs.OpponentData.ContainsKey(driverName) && 
                            !cgs.OpponentData.ContainsKey(driverName))
                        {
                            cgs.OpponentData.Add(driverName, pgs.OpponentData[driverName]);
                        }
                        opponentKeysProcessed.Add(driverName);
                        continue;
                    }
                    else
                    {
                        // offline we can have any number of duplicates :(
                        opponentKey = getOpponentKeyForVehicleInfo(vehicle, pgs, csd.SessionRunningTime, driverName, duplicatesCount);
                        if (opponentKey == null)
                        {
                            // there's no previous opponent data record for this driver so create one
                            if (duplicatesCreated.ContainsKey(driverName))
                            {
                                duplicatesCreated[driverName] += 1;
                            } else {
                                duplicatesCreated.Add(driverName, 1);
                            }
                            opponentKey = driverName + "_duplicate_" + duplicatesCreated[driverName];
                        }
                    }
                }
                else
                {
                    opponentKey = driverName;
                }
                opponentPrevious = pgs == null || opponentKey == null || !pgs.OpponentData.ContainsKey(opponentKey) ? null : previousGameState.OpponentData[opponentKey];
                OpponentData opponent = new OpponentData();
                opponent.DriverRawName = driverName;
                opponent.CarClass = CarData.getCarClassForClassName(getStringFromBytes(vehicle.mVehicleClass));                

                opponent.DriverRawName = driverName;
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.Position = vehicle.mPlace;

                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    this.speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName +
                        " is using car class " + opponent.CarClass.getClassIdentifier() +
                        " at position " + opponent.Position.ToString());
                }

                if (opponentPrevious != null)
                {
                    foreach (var old in opponentPrevious.OpponentLapData)
                        opponent.OpponentLapData.Add(old);
                }

                opponent.UnFilteredPosition = opponent.Position;
                opponent.SessionTimeAtLastPositionChange
                    = opponentPrevious != null && opponentPrevious.Position != opponent.Position
                            ? csd.SessionRunningTime : -1.0f;

                opponent.CompletedLaps = vehicle.mTotalLaps;
                opponent.CurrentSectorNumber = vehicle.mSector == 0 ? 3 : vehicle.mSector;

                var isNewSector = csd.IsNewSession || (opponentPrevious != null && opponentPrevious.CurrentSectorNumber != opponent.CurrentSectorNumber);
                opponent.IsNewLap = csd.IsNewSession || (isNewSector && opponent.CurrentSectorNumber == 1);
                opponent.Speed = (float)vehicle.mSpeed;
                opponent.DistanceRoundTrack = (float)vehicle.mLapDist;
                opponent.WorldPosition = new float[] { (float)vehicle.mPos.x, (float)vehicle.mPos.z };
                opponent.CurrentBestLapTime = vehicle.mBestLapTime > 0.0f ? (float)vehicle.mBestLapTime : -1.0f;
                opponent.PreviousBestLapTime = opponentPrevious != null && opponentPrevious.CurrentBestLapTime > 0.0f &&
                    opponentPrevious.CurrentBestLapTime > opponent.CurrentBestLapTime ? opponentPrevious.CurrentBestLapTime : -1.0f;
                float previousDistanceRoundTrack = opponentPrevious != null ? opponentPrevious.DistanceRoundTrack : 0;
                opponent.bestSector1Time = vehicle.mBestSector1 > 0 ? (float)vehicle.mBestSector1 : -1.0f;
                opponent.bestSector2Time = vehicle.mBestSector2 > 0 && vehicle.mBestSector1 > 0.0f ? (float)(vehicle.mBestSector2 - vehicle.mBestSector1) : -1.0f;
                opponent.bestSector3Time = vehicle.mBestLapTime > 0 && vehicle.mBestSector2 > 0.0f ? (float)(vehicle.mBestLapTime - vehicle.mBestSector2) : -1.0f;
                opponent.LastLapTime = vehicle.mLastLapTime > 0 ? (float)vehicle.mLastLapTime : -1.0f;
                opponent.InPits = vehicle.mInPits == 1;

                float lastSectorTime = -1.0f;
                if (opponent.CurrentSectorNumber == 1)
                    lastSectorTime = vehicle.mLastLapTime > 0.0f ? (float)vehicle.mLastLapTime : -1.0f;
                else if (opponent.CurrentSectorNumber == 2)
                {
                    lastSectorTime = vehicle.mLastSector1 > 0.0f ? (float)vehicle.mLastSector1 : -1.0f;

                    if (vehicle.mCurSector1 > 0.0)
                        lastSectorTime = (float)vehicle.mCurSector1;
                }
                else if (opponent.CurrentSectorNumber == 3)
                {
                    lastSectorTime = vehicle.mLastSector2 > 0.0f ? (float)vehicle.mLastSector2 : -1.0f;

                    if (vehicle.mCurSector2 > 0.0)
                        lastSectorTime = (float)vehicle.mCurSector2;
                }

                bool lapValid = true;
                if (vehicle.mCountLapFlag != 2)
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
                            rf2state.mRaining > minRainThreshold,
                            (float)rf2state.mTrackTemp,
                            (float)rf2state.mAmbientTemp,
                            csd.SessionHasFixedTime,
                            csd.SessionTimeRemaining);
                    }
                    opponent.StartNewLap(
                        opponent.CompletedLaps + 1,
                        opponent.Position,
                        vehicle.mInPits == 1 || opponent.DistanceRoundTrack < 0.0f,
                        csd.SessionRunningTime,
                        rf2state.mRaining > minRainThreshold,
                        (float)rf2state.mTrackTemp,
                        (float)rf2state.mAmbientTemp);
                }
                else if (isNewSector && lastSectorTime > 0.0f)
                {
                    opponent.AddCumulativeSectorData(
                        opponent.Position,
                        lastSectorTime,
                        csd.SessionRunningTime,
                        lapValid,
                        rf2state.mRaining > minRainThreshold,
                        (float)rf2state.mTrackTemp,
                        (float)rf2state.mAmbientTemp);
                }

                if (vehicle.mInPits == 1
                    && opponent.CurrentSectorNumber == 3
                    && opponentPrevious != null
                    && !opponentPrevious.isEnteringPits())
                {
                    opponent.setInLap();
                    var currentLapData = opponent.getCurrentLapData();
                    int sector3Position = currentLapData != null && currentLapData.SectorPositions.Count > 2
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
                {
                    csd.TimeDeltaBehind = (float)vehicle.mTimeBehindNext;
                }

                if (opponent.CurrentBestLapTime > 0.0f
                    && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestOverall
                        || csd.OpponentsLapTimeSessionBestOverall < 0.0f))
                {
                    csd.OpponentsLapTimeSessionBestOverall = opponent.CurrentBestLapTime;
                }

                if (opponent.CurrentBestLapTime > 0.0f
                    && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestPlayerClass
                        || csd.OpponentsLapTimeSessionBestPlayerClass < 0.0f)
                    && opponent.CarClass.getClassIdentifier() == cgs.carClass.getClassIdentifier())
                {
                    csd.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;

                    if (csd.OpponentsLapTimeSessionBestPlayerClass < csd.PlayerClassSessionBestLapTime)
                        csd.PlayerClassSessionBestLapTime = csd.OpponentsLapTimeSessionBestPlayerClass;
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
                    opponent.trackLandmarksTiming.updateLandmarkTiming(csd.TrackDefinition.trackLandmarks,
                        csd.SessionRunningTime, previousDistanceRoundTrack, opponent.DistanceRoundTrack, opponent.Speed);
                }
                if (opponent.IsNewLap)
                {
                    opponent.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                }

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
                csd.trackLandmarksTiming.updateLandmarkTiming(csd.TrackDefinition.trackLandmarks,
                                    csd.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack, cgs.PositionAndMotionData.DistanceRoundTrack, (float) rf2state.mSpeed);
                if (csd.IsNewLap)
                {
                    csd.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                }
            }

            // --------------------------------
            // fuel data
            // don't read fuel data until race session is green
            // don't read fuel data for non-race session until out of pit lane and more than one lap completed
            if ((csd.SessionType == SessionType.Race
                 && (csd.SessionPhase == SessionPhase.Green || csd.SessionPhase == SessionPhase.FullCourseYellow
                     || csd.SessionPhase == SessionPhase.Finished
                     || csd.SessionPhase == SessionPhase.Checkered))
                 || (!cgs.PitData.InPitlane && csd.CompletedLaps > 1))
            {
                cgs.FuelData.FuelUseActive = true;
                cgs.FuelData.FuelLeft = (float)rf2state.mFuel;
            }

            // --------------------------------
            // flags data
            cgs.FlagData.isFullCourseYellow = csd.SessionPhase == SessionPhase.FullCourseYellow
                || rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.Resume;

            if (rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.Resume)
            {
                // Special case for resume after FCY.  rF2 no longer has FCY set, but still has Resume sub phase set.
                cgs.FlagData.fcyPhase = FullCourseYellowPhase.RACING;
            }
            else if (cgs.FlagData.isFullCourseYellow)
            {
                if (rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.Pending)
                    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PENDING;
                //else if (rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitClosed)
                //    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PITS_CLOSED;
                // At default ruleset, both open and close sub states result in "Pits open" visible in the UI.
                else if (rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitOpen
                    || rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitClosed)
                    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PITS_OPEN;
                else if (rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.PitLeadLap)
                    cgs.FlagData.fcyPhase = FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES;
                else if (rf2state.mYellowFlagState == (sbyte)rFactor2Constants.rF2YellowFlagState.LastLap)
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
                    if (rf2state.mSectorFlag[i] == (int)rFactor2Constants.rF2YellowFlagState.Pending)
                        cgs.FlagData.sectorFlags[i] = FlagEnum.YELLOW;
                }
            }

            var currFlag = FlagEnum.UNKNOWN;

            if (UserSettings.GetUserSettings().getBoolean("enable_rf2_white_on_last_lap"))
            {
                // TODO: Re-work when NASCAR rules are implemented.
                if ((csd.SessionType == SessionType.Race || csd.SessionType == SessionType.Qualify)
                    && csd.SessionPhase == SessionPhase.Green
                    && csd.LeaderHasFinishedRace)
                {
                    currFlag = FlagEnum.WHITE;
                }
            }

            if (player.mFlag == (byte)rFactor2Constants.rF2PrimaryFlag.Blue)
            {
                currFlag = FlagEnum.BLUE;
            }
            else if (UserSettings.GetUserSettings().getBoolean("enable_rf2_blue_on_slower"))
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
            cgs.PenaltiesData.NumPenalties = player.mNumPenalties;
            float lateralDistDiff = (float)(Math.Abs(player.mPathLateral) - Math.Abs(player.mTrackEdge));
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

        private void processPlayerTimingData(
            ref rF2State rf2state,
            GameStateData currentGameState,
            SessionData currentSessionData,
            SessionData previousSessionData,
            ref rF2VehScoringInfo player)
        {
            var csd = currentSessionData;
            var cgs = currentGameState;
            var psd = previousSessionData;

            // Clear all the timings one new session.
            if (csd.IsNewSession)
                return;

            Debug.Assert(psd != null);

            csd.LapTimeCurrent = csd.SessionRunningTime - (float)player.mLapStartET;
            csd.LapTimePrevious = player.mLastLapTime > 0.0f ? (float)player.mLastLapTime : -1.0f;

            // Last (most current) per-sector times:
            // Note: this logic still misses invalid sector handling.
            var lastS1Time = player.mLastSector1 > 0.0 ? player.mLastSector1 : -1.0;
            var lastS2Time = player.mLastSector1 > 0.0 && player.mLastSector2 > 0.0
                ? player.mLastSector2 - player.mLastSector1 : -1.0;
            var lastS3Time = player.mLastSector2 > 0.0 && player.mLastLapTime > 0.0
                ? player.mLastLapTime - player.mLastSector2 : -1.0;

            csd.LastSector1Time = (float)lastS1Time;
            csd.LastSector2Time = (float)lastS2Time;
            csd.LastSector3Time = (float)lastS3Time;

            // Check if we have more current values for S1 and S2.
            // S3 always equals to lastS3Time.
            if (player.mCurSector1 > 0.0)
                csd.LastSector1Time = (float)player.mCurSector1;

            if (player.mCurSector1 > 0.0 && player.mCurSector2 > 0.0)
                csd.LastSector2Time = (float)(player.mCurSector2 - player.mCurSector1);

            // Below values change on sector/lap change, otherwise stay the same between updates.
            // Preserve current values.
            csd.PlayerBestSector1Time = psd.PlayerBestSector1Time;
            csd.PlayerBestSector2Time = psd.PlayerBestSector2Time;
            csd.PlayerBestSector3Time = psd.PlayerBestSector3Time;

            csd.PlayerBestLapSector1Time = psd.PlayerBestLapSector1Time;
            csd.PlayerBestLapSector2Time = psd.PlayerBestLapSector2Time;
            csd.PlayerBestLapSector3Time = psd.PlayerBestLapSector3Time;

            csd.PlayerLapTimeSessionBest = psd.PlayerLapTimeSessionBest;
            csd.PlayerLapTimeSessionBestPrevious = psd.PlayerLapTimeSessionBestPrevious;

            foreach (var ld in psd.PlayerLapData)
                csd.PlayerLapData.Add(ld);

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
            else if (player.mCountLapFlag != (byte)rFactor2Constants.rF2CountLapFlag.CountLapAndTime
                // And, this is not an out/in lap
                && !cgs.PitData.OnOutLap && !cgs.PitData.OnInLap)
            {
                csd.CurrentLapIsValid = false;
            }

            // Check if timing update is needed.
            if (!csd.IsNewLap && !csd.IsNewSector)
                return;

            float lastSectorTime = -1.0f;
            if (csd.SectorNumber == 1)
                lastSectorTime = player.mLastLapTime > 0.0f ? (float)player.mLastLapTime : -1.0f;
            else if (csd.SectorNumber == 2)
            {
                lastSectorTime = player.mLastSector1 > 0.0f ? (float)player.mLastSector1 : -1.0f;

                if (player.mCurSector1 > 0.0)
                    lastSectorTime = (float)player.mCurSector1;
            }
            else if (csd.SectorNumber == 3)
            { 
                lastSectorTime = player.mLastSector2 > 0.0f ? (float)player.mLastSector2 : -1.0f;

                if (player.mCurSector2 > 0.0)
                    lastSectorTime = (float)player.mCurSector2;
            }

            if (csd.IsNewLap)
            {
                if (lastSectorTime > 0.0f)
                {
                    csd.playerCompleteLapWithProvidedLapTime(
                        csd.Position,
                        csd.SessionRunningTime,
                        csd.LapTimePrevious,
                        csd.CurrentLapIsValid,
                        rf2state.mRaining > minRainThreshold,
                        (float)rf2state.mTrackTemp,
                        (float)rf2state.mAmbientTemp,
                        csd.SessionHasFixedTime,
                        csd.SessionTimeRemaining);
                }

                csd.playerStartNewLap(
                    csd.CompletedLaps + 1,
                    csd.Position,
                    player.mInPits == 1 || currentGameState.PositionAndMotionData.DistanceRoundTrack < 0.0f,
                    csd.SessionRunningTime,
                    rf2state.mRaining > minRainThreshold,
                    (float)rf2state.mTrackTemp,
                    (float)rf2state.mAmbientTemp);
            }
            else if (csd.IsNewSector && lastSectorTime > 0.0f)
            {
                csd.playerAddCumulativeSectorData(
                    csd.Position,
                    lastSectorTime,
                    csd.SessionRunningTime,
                    csd.CurrentLapIsValid,
                    rf2state.mRaining > minRainThreshold,
                    (float)rf2state.mTrackTemp,
                    (float)rf2state.mAmbientTemp);
            }
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
            ref rF2VehScoringInfo player)
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
        private String getOpponentKeyForVehicleInfo(rF2VehScoringInfo vehicle, GameStateData previousGameState, float sessionRunningTime, String driverName, int duplicatesCount)
        {
            if (previousGameState == null)
            {
                return null;
            }
            List<string> possibleKeys = new List<string>();
            for (int i = 1; i <= duplicatesCount; i++)
            {
                possibleKeys.Add(driverName + "_duplicate_ " + i);
            }
            float[] worldPos = { (float)vehicle.mPos.x, (float)vehicle.mPos.z };
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
                        if (o.DriverRawName != getStringFromBytes(vehicle.mDriverName).ToLower() ||
                            o.CarClass != CarData.getCarClassForClassName(getStringFromBytes(vehicle.mVehicleClass)) ||
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
            {
                opponentKeysProcessed.Add(bestKey);
            }
            return bestKey;
        }

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            rFactor2Data.rF2State shared = (rFactor2Data.rF2State)memoryMappedFileStruct;
            switch (shared.mSession)
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

        public static String getStringFromBytes(byte[] name)
        {
            var str = Encoding.Default.GetString(name);
            var eosChar = str.IndexOf('\0');
            if (eosChar != -1)
              str = str.Substring(0, eosChar);

            return str;
        }
    }
}
