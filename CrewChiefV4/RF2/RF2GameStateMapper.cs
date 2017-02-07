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

        // if we're running only against AI, force the pit window to open
        private Boolean isOfflineSession = true;
        
        // keep track of opponents processed this time
        private List<String> opponentKeysProcessed = new List<String>();
        
        // detect when approaching racing surface after being off track
        private float distanceOffTrack = 0.0f;
        private Boolean isApproachingTrack = false;

        // dynamically calculated wheel circumferences
        private float[] wheelCircumference = new float[] { 0.0f, 0.0f };

        // Session classes tracing.
        private Dictionary<string, string> carClassMap = new Dictionary<string, string>();
        bool isMultiClassSession = false;

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

        private int[] minimumSupportedVersionParts = new int[] { 1, 0, 0, 0 };
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
                this.wheelCircumference = new float[] { 0, 0 };
                pgs = null;

                return null;
            }
            
            // game is paused or other window has taken focus
            if (rf2state.mDeltaTime > 0.23)
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
            csd.SessionPhase = mapToSessionPhase((rFactor2Constants.rF2GamePhase)rf2state.mGamePhase);
            cgs.FlagData.isFullCourseYellow = csd.SessionPhase == SessionPhase.FullCourseYellow;
            cgs.carClass = CarData.getCarClassForRF2ClassName(getSafeCarClassName(getStringFromBytes(player.mVehicleClass)));
            this.brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(cgs.carClass);
            csd.DriverRawName = getStringFromBytes(player.mDriverName).ToLower();
            csd.TrackDefinition = new TrackDefinition(getStringFromBytes(rf2state.mTrackName), (float)rf2state.mLapDist);

            if (cgs.FlagData.isFullCourseYellow && pgs != null && !pgs.FlagData.isFullCourseYellow)
            {
                // transitioned from racing to yellow, so set the FCY status to pending
                cgs.FlagData.fcyPhase = FullCourseYellowPhase.PENDING;
            }
            else if (pgs != null && pgs.FlagData.isFullCourseYellow && !cgs.FlagData.isFullCourseYellow)
            {
                // transitioned from yellow to racing, so set the FCY status to racing
                cgs.FlagData.fcyPhase = FullCourseYellowPhase.RACING;
            }

            csd.TrackDefinition.setGapPoints();
            csd.SessionNumberOfLaps = rf2state.mMaxLaps > 0 && rf2state.mMaxLaps < 1000 ? rf2state.mMaxLaps : 0;
            
            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630.0f;
            csd.SessionTotalRunTime
                = (float)rf2state.mEndET > 0.0f
                    ? (float)rf2state.mEndET
                    : csd.SessionNumberOfLaps > 0.0f ? 0.0f : defaultSessionTotalRunTime;

            // if previous state is null or any of the above change, this is a new session
            csd.IsNewSession = pgs == null ||
                csd.SessionType != psd.SessionType ||
                cgs.carClass != pgs.carClass ||
                csd.DriverRawName != psd.DriverRawName || 
                csd.TrackDefinition.name != psd.TrackDefinition.name ||
                csd.TrackDefinition.trackLength != psd.TrackDefinition.trackLength ||
                // these sometimes change in the beginning or end of session!
                //currSessionData.SessionNumberOfLaps != prevSessionData.SessionNumberOfLaps ||
                //currSessionData.SessionTotalRunTime != prevSessionData.SessionTotalRunTime || 
                csd.EventIndex != psd.EventIndex || 
                csd.SessionIteration != psd.SessionIteration || 
                ((psd.SessionPhase == SessionPhase.Checkered || 
                psd.SessionPhase == SessionPhase.Finished ||
                psd.SessionPhase == SessionPhase.Green || 
                psd.SessionPhase == SessionPhase.FullCourseYellow) && 
                (csd.SessionPhase == SessionPhase.Garage || 
                csd.SessionPhase == SessionPhase.Gridwalk ||
                csd.SessionPhase == SessionPhase.Formation ||
                csd.SessionPhase == SessionPhase.Countdown));

            if (csd.IsNewSession)
            {
                this.repopulateClassMap(ref rf2state);
                var classesStr = this.getCarClassesString();

                var msg = this.getIsMultiClassSession()
                    ? "New Mutli-Class Session: " + classesStr.ToString()
                    : "New Single-Class Session: " + classesStr.ToString();

                this.isMultiClassSession = this.getIsMultiClassSession();
                Console.WriteLine(msg);
            }
            else if (this.isMultiClassSession != this.getIsMultiClassSession())
            {
                // Consider: There might be an earlier spot to check for this (multiplayer
                // new veh joined or something).
                var classesStr = this.getCarClassesString();

                // Consider: might cause bugs if all cars retired besides one.
                var msg = this.getIsMultiClassSession()
                    ? "Session changed to Mutli-Class: " + classesStr.ToString()
                    : "Session to Single-Class: " + classesStr.ToString();

                this.isMultiClassSession = this.getIsMultiClassSession();
                Console.WriteLine(msg);
            }

            csd.SessionStartTime = csd.IsNewSession ? cgs.Now : psd.SessionStartTime;
            csd.SessionHasFixedTime = csd.SessionTotalRunTime > 0.0f;
            csd.SessionRunningTime = (float)rf2state.mCurrentET;
            csd.SessionTimeRemaining = csd.SessionHasFixedTime ? csd.SessionTotalRunTime - csd.SessionRunningTime : 0.0f;
            
            // hack for test day sessions running longer than allotted time
            csd.SessionTimeRemaining = csd.SessionTimeRemaining < 0.0f && rf2state.mSession == 0.0f ? defaultSessionTotalRunTime : csd.SessionTimeRemaining;

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
            csd.CompletedLaps = player.mTotalLaps;
            csd.LapTimeCurrent = csd.SessionRunningTime - (float)player.mLapStartET;
            csd.LapTimePrevious = player.mLastLapTime > 0.0f ? (float)player.mLastLapTime : -1.0f;
            csd.LastSector1Time = player.mCurSector1 > 0.0f ? (float)player.mCurSector1 : -1.0f;
            csd.LastSector2Time = player.mCurSector2 > 0.0f && player.mCurSector1 > 0.0f ? (float)(player.mCurSector2 - player.mCurSector1) : -1.0f;
            csd.LastSector3Time = player.mLastLapTime > 0.0f && player.mCurSector2 > 0.0f ? (float)(player.mLastLapTime - player.mCurSector2) : -1.0f;
            csd.PlayerBestSector1Time = player.mBestSector1 > 0.0f ? (float)player.mBestSector1 : -1.0f;
            csd.PlayerBestSector2Time = player.mBestSector2 > 0.0f && player.mBestSector1 > 0.0f ? (float)(player.mBestSector2 - player.mBestSector1) : -1.0f;
            csd.PlayerBestSector3Time = player.mBestLapTime > 0.0f && player.mBestSector2 > 0.0f ? (float)(player.mBestLapTime - player.mBestSector2) : -1.0f;
            csd.PlayerBestLapSector1Time = csd.PlayerBestSector1Time;
            csd.PlayerBestLapSector2Time = csd.PlayerBestSector2Time;
            csd.PlayerBestLapSector3Time = csd.PlayerBestSector3Time;
            csd.PlayerLapTimeSessionBest = player.mBestLapTime > 0.0f ? (float)player.mBestLapTime : -1.0f;
            csd.SessionTimesAtEndOfSectors = pgs != null ? psd.SessionTimesAtEndOfSectors : new SessionData().SessionTimesAtEndOfSectors;
            
            if (csd.IsNewSector && !csd.IsNewSession)
            {
                // there's a slight delay due to scoring updating every 200 ms, so we can't use SessionRunningTime here
                switch(csd.SectorNumber)
                {
                    case 1:
                        csd.SessionTimesAtEndOfSectors[3]
                            = player.mLapStartET > 0 ? (float)player.mLapStartET : -1;
                        break;
                    case 2:
                        csd.SessionTimesAtEndOfSectors[1]
                            = player.mLapStartET > 0 && player.mCurSector1 > 0 
                                ? (float)(player.mLapStartET + player.mCurSector1)
                                : -1;
                        break;
                    case 3:
                        csd.SessionTimesAtEndOfSectors[2]
                            = player.mLapStartET > 0 && player.mCurSector2 > 0 
                                ? (float)(player.mLapStartET + player.mCurSector2)
                                : -1;
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
            // motion data
            cgs.PositionAndMotionData.CarSpeed = (float)rf2state.mSpeed;
            cgs.PositionAndMotionData.DistanceRoundTrack = (float)player.mLapDist;

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
            {
                cgs.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }
            else if (pgs == null || frontRightTemp > pgs.TyreData.PeakFrontRightTemperatureForLap)
            {
                cgs.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }

            var wheelRearLeft = rf2state.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft];
            cgs.TyreData.LeftRearAttached = wheelRearLeft.mDetached == 0;
            cgs.TyreData.RearLeft_LeftTemp = (float)wheelRearLeft.mTemperature[0] - 273.15f;
            cgs.TyreData.RearLeft_CenterTemp = (float)wheelRearLeft.mTemperature[1] - 273.15f;
            cgs.TyreData.RearLeft_RightTemp = (float)wheelRearLeft.mTemperature[2] - 273.15f;

            var rearLeftTemp = (cgs.TyreData.RearLeft_CenterTemp + cgs.TyreData.RearLeft_LeftTemp + cgs.TyreData.RearLeft_RightTemp) / 3.0f;
            cgs.TyreData.RearLeftPressure = wheelRearLeft.mFlat == 0 ? (float)wheelRearLeft.mPressure : 0.0f;
            cgs.TyreData.RearLeftPercentWear = (float)(1.0f - wheelRearLeft.mWear) * 100.0f;
            if (csd.IsNewLap)
            {
                cgs.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }
            else if (pgs == null || rearLeftTemp > pgs.TyreData.PeakRearLeftTemperatureForLap)
            {
                cgs.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }

            var wheelRearRight = rf2state.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight];
            cgs.TyreData.RightRearAttached = wheelRearRight.mDetached == 0;
            cgs.TyreData.RearRight_LeftTemp = (float)wheelRearRight.mTemperature[0] - 273.15f;
            cgs.TyreData.RearRight_CenterTemp = (float)wheelRearRight.mTemperature[1] - 273.15f;
            cgs.TyreData.RearRight_RightTemp = (float)wheelRearRight.mTemperature[2] - 273.15f;

            var rearRightTemp = (cgs.TyreData.RearRight_CenterTemp + cgs.TyreData.RearRight_LeftTemp + cgs.TyreData.RearRight_RightTemp) / 3.0f;
            cgs.TyreData.RearRightPressure = wheelRearRight.mFlat == 0 ? (float)wheelRearRight.mPressure : 0.0f;
            cgs.TyreData.RearRightPercentWear = (float)(1.0f - wheelRearRight.mWear) * 100.0f;
            if (csd.IsNewLap)
            {
                cgs.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (pgs == null || rearRightTemp > pgs.TyreData.PeakRearRightTemperatureForLap)
            {
                cgs.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            cgs.TyreData.TyreConditionStatus = CornerData.getCornerData(this.tyreWearThresholds, cgs.TyreData.FrontLeftPercentWear,
                cgs.TyreData.FrontRightPercentWear, cgs.TyreData.RearLeftPercentWear, cgs.TyreData.RearRightPercentWear);

            cgs.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[cgs.carClass.defaultTyreType],
                cgs.TyreData.PeakFrontLeftTemperatureForLap, cgs.TyreData.PeakFrontRightTemperatureForLap,
                cgs.TyreData.PeakRearLeftTemperatureForLap, cgs.TyreData.PeakRearRightTemperatureForLap);

            // some simple locking / spinning checks
            if ((csd.IsNewSession
                    || this.wheelCircumference[0] == 0 
                    || this.wheelCircumference[1] == 0)
                && cgs.PositionAndMotionData.CarSpeed > 14.0f
                && Math.Abs(rf2state.mUnfilteredSteering) <= 0.05)
            {
                // calculate wheel circumference (assume left/right symmetry) at 50+ km/h with (mostly) straight steering
                // front
                this.wheelCircumference[0] = (float)(2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[0].mRotation) + 
                    2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[1].mRotation)) / 2.0f;
                // rear
                this.wheelCircumference[1] = (float)(2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[2].mRotation) + 
                    2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[3].mRotation)) / 2.0f;
            }
            
            if (cgs.PositionAndMotionData.CarSpeed > 7.0f &&
                this.wheelCircumference[0] > 0.0f && this.wheelCircumference[1] > 0.0f)
            {
                float[] rotatingSpeed = new float[] { 
                    2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / this.wheelCircumference[0], 
                    2.0f * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / this.wheelCircumference[1] };
                float minRotFactor = 0.5f;
                float maxRotFactor = 1.3f;

                cgs.TyreData.LeftFrontIsLocked = Math.Abs(wheelFrontLeft.mRotation) < minRotFactor * rotatingSpeed[0];
                cgs.TyreData.RightFrontIsLocked = Math.Abs(wheelFrontRight.mRotation) < minRotFactor * rotatingSpeed[0];
                cgs.TyreData.LeftRearIsLocked = Math.Abs(wheelRearLeft.mRotation) < minRotFactor * rotatingSpeed[1];
                cgs.TyreData.RightRearIsLocked = Math.Abs(wheelRearRight.mRotation) < minRotFactor * rotatingSpeed[1];

                cgs.TyreData.LeftFrontIsSpinning = Math.Abs(wheelFrontLeft.mRotation) > maxRotFactor * rotatingSpeed[0];
                cgs.TyreData.RightFrontIsSpinning = Math.Abs(wheelFrontRight.mRotation) > maxRotFactor * rotatingSpeed[0];
                cgs.TyreData.LeftRearIsSpinning = Math.Abs(wheelRearLeft.mRotation) > maxRotFactor * rotatingSpeed[1];
                cgs.TyreData.RightRearIsSpinning = Math.Abs(wheelRearRight.mRotation) > maxRotFactor * rotatingSpeed[1];
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
                    (float)rf2state.mAmbientTemp, (float)rf2state.mTrackTemp, 0, (float)Math.Sqrt((double)(rf2state.mWind.x * rf2state.mWind.x + rf2state.mWind.y * rf2state.mWind.y + rf2state.mWind.z * rf2state.mWind.z)), 0, 0, 0);
            }

            // --------------------------------
            // opponent data
            this.isOfflineSession = true;
            this.opponentKeysProcessed.Clear();

            // NOTE: AMS/rF1 implementation scanned all vehicles, we only scan active (beyound comes trash).
            // See if it causes problems.
            for (int i = 0; i < rf2state.mNumVehicles; ++i)
            {
                var vehicle = rf2state.mVehicles[i];
                if (vehicle.mIsPlayer == 1)
                {
                    if (this.isMultiClassSession)
                    {
                        csd.PlayerClassSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0.0f ?
                            csd.PlayerLapTimeSessionBest : -1.0f;

                        csd.OverallSessionBestLapTime = -1.0f;
                    }
                    else
                    {
                        csd.OverallSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0.0f ?
                            csd.PlayerLapTimeSessionBest : -1.0f;

                        csd.PlayerClassSessionBestLapTime = -1.0f;
                    }
                    continue;
                }

                switch (mapToControlType((rFactor2Constants.rF2Control)vehicle.mControl))
                {
                    case ControlType.Player:
                    case ControlType.Replay:
                    case ControlType.Unavailable:
                        continue;
                    case ControlType.Remote:
                        this.isOfflineSession = false;
                        break;
                    default:
                        break;
                }

                var opponentKey = getSafeCarClassName(getStringFromBytes(vehicle.mVehicleClass)) + vehicle.mPlace.ToString();
                var opponentPrevious = getOpponentDataForVehicleInfo(vehicle, pgs, csd.SessionRunningTime);
                var opponent = new OpponentData();
                opponent.DriverRawName = getStringFromBytes(vehicle.mDriverName).ToLower();
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.CarClass = CarData.getCarClassForRF2ClassName(getSafeCarClassName(getStringFromBytes(vehicle.mVehicleClass)));
                opponent.Position = vehicle.mPlace;
                
                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    this.speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName + 
                        " is using car class " + opponent.CarClass.rFClassName + 
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
                opponent.bestSector1Time = vehicle.mBestSector1 > 0 ? (float)vehicle.mBestSector1 : -1.0f;
                opponent.bestSector2Time = vehicle.mBestSector2 > 0 && vehicle.mBestSector1 > 0.0f ? (float)(vehicle.mBestSector2 - vehicle.mBestSector1) : -1.0f;
                opponent.bestSector3Time = vehicle.mBestLapTime > 0 && vehicle.mBestSector2 > 0.0f ?  (float)(vehicle.mBestLapTime - vehicle.mBestSector2) : -1.0f;
                opponent.LastLapTime = vehicle.mLastLapTime > 0 ? (float)vehicle.mLastLapTime : -1.0f;
                
                float lastSectorTime = -1.0f;
                switch (opponent.CurrentSectorNumber)
                {
                    case 1:
                        lastSectorTime = vehicle.mLastLapTime > 0.0f ? (float)vehicle.mLastLapTime : -1.0f;
                        break;
                    case 2:
                        lastSectorTime = vehicle.mLastSector1 > 0.0f ? (float)vehicle.mLastSector1 : -1.0f;
                        break;
                    case 3:
                        lastSectorTime = vehicle.mLastSector2 > 0.0f ? (float)vehicle.mLastSector2 : -1.0f;
                        break;
                    default:
                        break;
                }

                if (opponent.IsNewLap)
                {
                    if (lastSectorTime > 0.0f)
                    {
                        opponent.CompleteLapWithProvidedLapTime(
                            opponent.Position,
                            (float)vehicle.mLapStartET,
                            lastSectorTime,
                            true,
                            false,
                            (float)rf2state.mTrackTemp,
                            (float)rf2state.mAmbientTemp,
                            csd.SessionHasFixedTime,
                            csd.SessionTimeRemaining);
                    }
                    opponent.StartNewLap(
                        opponent.CompletedLaps + 1,
                        opponent.Position,
                        vehicle.mInPits == 1 || opponent.DistanceRoundTrack < 0,
                        (float)vehicle.mLapStartET, 
                        false, 
                        (float)rf2state.mTrackTemp,
                        (float)rf2state.mAmbientTemp);
                }
                else if (isNewSector && lastSectorTime > 0.0f)
                {
                    opponent.AddCumulativeSectorData(
                        opponent.Position,
                        lastSectorTime,
                        (float)vehicle.mLapStartET + lastSectorTime,
                        true,
                        false,
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

                if (this.isMultiClassSession)
                {
                    if (opponent.CurrentBestLapTime > 0.0f
                        && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestPlayerClass
                            || csd.OpponentsLapTimeSessionBestPlayerClass < 0.0f)
                        && opponent.CarClass == cgs.carClass)
                    {
                        csd.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;
                    }

                    csd.OverallSessionBestLapTime = -1.0f;
                }
                else
                {
                    if (opponent.CurrentBestLapTime > 0.0f
                        && (opponent.CurrentBestLapTime < csd.OverallSessionBestLapTime
                            || csd.OverallSessionBestLapTime < 0.0f))
                    {
                        csd.OverallSessionBestLapTime = opponent.CurrentBestLapTime;
                    }

                    csd.OpponentsLapTimeSessionBestPlayerClass = -1.0f;
                }

                // shouldn't have duplicates, but just in case
                if (!cgs.OpponentData.ContainsKey(opponentKey))
                    cgs.OpponentData.Add(opponentKey, opponent);
            }

            if (pgs != null)
            {
                csd.HasLeadChanged = !csd.HasLeadChanged && psd.Position > 1 && csd.Position == 1 ?
                    true : csd.HasLeadChanged;
                String oPrevKey = null;
                String oCurrKey = null;
                OpponentData oPrev = null;
                OpponentData oCurr = null;
                oPrevKey = (String)pgs.getOpponentKeyInFrontOnTrack();
                oCurrKey = (String)cgs.getOpponentKeyInFrontOnTrack();
                oPrev = oPrevKey != null ? pgs.OpponentData[oPrevKey] : null;
                oCurr = oCurrKey != null ? cgs.OpponentData[oCurrKey] : null;

                csd.IsRacingSameCarInFront = !((oPrev == null && oCurr != null) 
                                                || (oPrev != null && oCurr == null) 
                                                || (oPrev != null && oCurr != null 
                                                    && oPrev.DriverRawName != oCurr.DriverRawName));

                oPrevKey = (String)pgs.getOpponentKeyBehindOnTrack();
                oCurrKey = (String)cgs.getOpponentKeyBehindOnTrack();
                oPrev = oPrevKey != null ? pgs.OpponentData[oPrevKey] : null;
                oCurr = oCurrKey != null ? cgs.OpponentData[oCurrKey] : null;

                csd.IsRacingSameCarBehind = !((oPrev == null && oCurr != null)
                                               || (oPrev != null && oCurr == null)
                                               || (oPrev != null && oCurr != null 
                                                   && oPrev.DriverRawName != oCurr.DriverRawName));

                csd.GameTimeAtLastPositionFrontChange = !csd.IsRacingSameCarInFront ? 
                    csd.SessionRunningTime : psd.GameTimeAtLastPositionFrontChange;
                csd.GameTimeAtLastPositionBehindChange = !csd.IsRacingSameCarBehind ? 
                    csd.SessionRunningTime : psd.GameTimeAtLastPositionBehindChange;
            }

            // --------------------------------
            // pit data
            cgs.PitData.IsRefuellingAllowed = true;

            cgs.PitData.HasMandatoryPitStop = this.isOfflineSession 
                && rf2state.mScheduledStops > 0 
                && player.mNumPitstops < rf2state.mScheduledStops 
                && csd.SessionType == SessionType.Race;

            cgs.PitData.PitWindowStart = this.isOfflineSession && cgs.PitData.HasMandatoryPitStop ? 1 : 0;
            cgs.PitData.PitWindowEnd = !cgs.PitData.HasMandatoryPitStop ? 0 :
                csd.SessionHasFixedTime ? (int)(csd.SessionTotalRunTime / 60 / (rf2state.mScheduledStops + 1)) * (player.mNumPitstops + 1) + 1 :
                (int)(csd.SessionNumberOfLaps / (rf2state.mScheduledStops + 1)) * (player.mNumPitstops + 1) + 1;
            cgs.PitData.InPitlane = player.mInPits == 1;
            cgs.PitData.IsAtPitExit = pgs != null && pgs.PitData.InPitlane && !cgs.PitData.InPitlane;
            cgs.PitData.OnOutLap = cgs.PitData.InPitlane && csd.SectorNumber == 1;
            cgs.PitData.OnInLap = cgs.PitData.InPitlane && csd.SectorNumber == 3;
            cgs.PitData.IsMakingMandatoryPitStop = cgs.PitData.HasMandatoryPitStop && cgs.PitData.OnInLap && csd.CompletedLaps > cgs.PitData.PitWindowStart;
            cgs.PitData.PitWindow = cgs.PitData.IsMakingMandatoryPitStop ? PitWindow.StopInProgress : mapToPitWindow((rFactor2Constants.rF2YellowFlagState)rf2state.mYellowFlagState);

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

            var currSectorIdx = (player.mSector == 0 ? 3 : player.mSector) - 1;
            //var nextSectorIdx = currSectorIdx == 2 ? 0 : currSectorIdx + 1;
            Debug.Assert(currSectorIdx >= 0 && currSectorIdx <= 2);
            //Debug.Assert(nextSectorIdx >= 0 && nextSectorIdx <= 2);
        
            // TODO: this whole code is messed up for rF2, rework
            // --------------------------------
            // flags data
            var currFlag = FlagEnum.UNKNOWN;
            if (csd.IsDisqualified
                && pgs != null 
                && !psd.IsDisqualified)
            {
                currFlag = FlagEnum.BLACK;
            }
            else if (rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.GreenFlag 
                && rf2state.mSectorFlag[currSectorIdx] == (int)rFactor2Constants.rF2YellowFlagState.Pending
                    /*|| rf2state.mSectorFlag[nextSectorIdx] == (int)rFactor2Constants.rF2YellowFlagState.Pending)*/)  // TODO: announce in next sector once event is available.
            {
                // TODO: we need message per sector as well.
                // We could announce sector number if flag is in the next sector.
                currFlag = FlagEnum.YELLOW;
            }
            else if (csd.SessionType == SessionType.Race ||
                csd.SessionType == SessionType.Qualify)
            {
                if (rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.FullCourseYellow
                    && rf2state.mYellowFlagState != (int)rFactor2Constants.rF2YellowFlagState.LastLap)
                {
                    // TODO: Play various SC phase events.
                    currFlag = FlagEnum.DOUBLE_YELLOW;
                }
                else if ((rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.FullCourseYellow
                    && rf2state.mYellowFlagState == (int)rFactor2Constants.rF2YellowFlagState.LastLap)
                    || csd.LeaderHasFinishedRace)
                {
                    currFlag = FlagEnum.WHITE;
                }
                else if (rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.GreenFlag
                    && pgs != null
                    && (psd.Flag == FlagEnum.DOUBLE_YELLOW || psd.Flag == FlagEnum.WHITE))
                {
                    currFlag = FlagEnum.GREEN;
                }
            }

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
                    cgs.PositionAndMotionData.DistanceRoundTrack, opponent.DistanceRoundTrack) && 
                    opponent.Speed >= cgs.PositionAndMotionData.CarSpeed)
                {
                    currFlag = FlagEnum.BLUE;
                    break;
                }
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

            if ((((csd.SectorNumber == 2 && csd.LastSector1Time < 0) || 
                (csd.SectorNumber == 3 && csd.LastSector2Time < 0)) && 
                !cgs.PitData.OnOutLap && !cgs.PitData.OnInLap &&
                (csd.SessionType == SessionType.Race || csd.SessionType == SessionType.Qualify)) || 
                (pgs != null && psd.CompletedLaps == csd.CompletedLaps && 
                !psd.CurrentLapIsValid))
            {
                // TODO: rF2 has direct flag for this, use it.
                csd.CurrentLapIsValid = false;
            }

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
                Console.WriteLine("Player is using car class " + cgs.carClass.rFClassName + 
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

        private SessionPhase mapToSessionPhase(rFactor2Constants.rF2GamePhase sessionPhase)
        {
            // TODO: FullCourseYellow is a separate session phase and is needed to suppress some messages during caution periods
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
                    return SessionPhase.Finished;
                    // TODO: revisit.
                // fullCourseYellow will count as greenFlag since we'll call it out in the Flags separately anyway
                case rFactor2Constants.rF2GamePhase.FullCourseYellow:
                    // TODO: CHECK ME!!
                    return SessionPhase.FullCourseYellow;
                case rFactor2Constants.rF2GamePhase.GreenFlag:
                    return SessionPhase.Green;
                default:
                    return SessionPhase.Unavailable;
            }
        }

        // finds OpponentData for given vehicle based on driver name, vehicle class, and world position
        private OpponentData getOpponentDataForVehicleInfo(rF2VehScoringInfo vehicle, GameStateData previousGameState, float sessionRunningTime)
        {
            OpponentData opponentPrevious = null;
            var prevSessionData = previousGameState != null ? previousGameState.SessionData : null;
            float timeDelta = previousGameState != null ? sessionRunningTime - prevSessionData.SessionRunningTime : -1.0f;
            if (previousGameState != null && timeDelta >= 0.0f)
            {
                float[] worldPos = { (float)vehicle.mPos.x, (float)vehicle.mPos.z };
                float minDistDiff = -1.0f;
                foreach (var o in previousGameState.OpponentData.Values)
                {
                    var opponentKey = o.CarClass.rFClassName + o.Position.ToString();
                    if (o.DriverRawName != getStringFromBytes(vehicle.mDriverName).ToLower() || 
                        o.CarClass != CarData.getCarClassForRF2ClassName(getSafeCarClassName(getStringFromBytes(vehicle.mVehicleClass))) || 
                        this.opponentKeysProcessed.Contains(opponentKey))
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
                        opponentPrevious = o;
                    }
                }

                if (opponentPrevious != null)
                {
                    this.opponentKeysProcessed.Add(opponentPrevious.CarClass.rFClassName + opponentPrevious.Position.ToString());
                }
            }
            return opponentPrevious;
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
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        }


        List<string> mappedSeries = new List<string>()
        {
            "GT1",
            "GT2",
            "GT3",
            "GT4",
            "GTE",
            "GTC",
            "GTLM",
            "GTC",
            "DTM",
        };

        //
        // Since class name in rF2 often, but not always, hehe, includes maker name, I need to try to guess
        // what series this class belongs, so that time comparison is not stuck within one brand, but rather
        // is done within class, as intended.
        //
        private string getSafeCarClassName(string rf2ClassName)
        {
            string safeClassName = null;
            if (this.carClassMap.TryGetValue(rf2ClassName, out safeClassName))
                return safeClassName;

            foreach (var series in this.mappedSeries)
            {
                if (rf2ClassName.Contains(series))
                {
                    this.carClassMap.Add(rf2ClassName, series);
                    return series;
                }
            }

            // If not mapped, just add itself.
            this.carClassMap.Add(rf2ClassName, rf2ClassName);
            return rf2ClassName;
        }

        private void repopulateClassMap(ref rF2State state)
        {
            this.carClassMap.Clear();
            for (int i = 0; i < state.mNumVehicles; ++i)
                this.getSafeCarClassName(RF2GameStateMapper.getStringFromBytes(state.mVehicles[i].mVehicleClass));
        }

        private bool getIsMultiClassSession()
        {
            return this.carClassMap.Values.Distinct().Count() > 1;
        }

        private string getCarClassesString()
        {
            var sb = new StringBuilder();
            foreach (var cls in this.carClassMap.Values.Distinct().ToList())
                sb.Append(cls + " ");

            return sb.ToString();
        }
    }
}
