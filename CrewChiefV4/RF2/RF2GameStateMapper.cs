using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.rFactor2.rFactor2Data;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV4.rFactor2
{
    public class RF2GameStateMapper : GameStateMapper
    {
        public static String playerName = null;
        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        private float scrubbedTyreWearPercent = 5f;
        private float minorTyreWearPercent = 30f;
        private float majorTyreWearPercent = 60f;
        private float wornOutTyreWearPercent = 85f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;

        private SpeechRecogniser speechRecogniser;

        // if we're running only against AI, force the pit window to open
        private Boolean isOfflineSession = true;
        
        // keep track of opponents processed this time
        private List<String> opponentKeysProcessed = new List<String>();
        
        // detect when approaching racing surface after being off track
        private float distanceOffTrack = 0;
        
        private Boolean isApproachingTrack = false;
        
        // dynamically calculated wheel circumferences
        private float[] wheelCircumference = new float[] { 0, 0 };

        public RF2GameStateMapper()
        {
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            this.tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));

            this.suspensionDamageThresholds.Add(new CornerData.EnumWithThresholds(DamageLevel.NONE, 0, 1));
            this.suspensionDamageThresholds.Add(new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, 1, 2));
        }

        public void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in rFactor shared data so this is a no-op
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
            {
                return pgs;
            }

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
                        {
                            player = vehicle;
                        }
                        if (vehicle.mPlace == 1)
                        {
                            leader = vehicle;
                        }
                        break;
                    default:
                        continue;
                }
                if (player.mIsPlayer == 1 && leader.mPlace == 1)
                {
                    break;
                }
            }
            
            // can't find the player or session leader vehicle info (replay)
            if (player.mIsPlayer != 1 || leader.mPlace != 1)
            {
                return pgs;
            }
            
            if (RF2GameStateMapper.playerName == null)
            {
                var driverName = getNameFromBytes(player.mDriverName).ToLower();
                NameValidator.validateName(driverName);
                RF2GameStateMapper.playerName = driverName;
            }
            
            // these things should remain constant during a session
            var csd = cgs.SessionData;
            var psd = pgs != null ? pgs.SessionData : null;
            csd.EventIndex = rf2state.mSession;
            csd.SessionIteration = 
                rf2state.mSession >= 1 && rf2state.mSession <= 4 ? rf2state.mSession - 1 :
                rf2state.mSession >= 5 && rf2state.mSession <= 8 ? rf2state.mSession - 5 :
                rf2state.mSession >= 10 && rf2state.mSession <= 13 ? rf2state.mSession - 10 : 0;
            csd.SessionType = mapToSessionType(rf2state);
            csd.SessionPhase = mapToSessionPhase((rFactor2Constants.rF2GamePhase)rf2state.mGamePhase);
            cgs.carClass = CarData.getCarClassForRF1ClassName(getNameFromBytes(player.mVehicleClass));
            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(cgs.carClass);
            csd.DriverRawName = getNameFromBytes(player.mDriverName).ToLower();
            csd.TrackDefinition = new TrackDefinition(getNameFromBytes(rf2state.mTrackName), (float)rf2state.mLapDist);
            csd.TrackDefinition.setGapPoints();
            csd.SessionNumberOfLaps = rf2state.mMaxLaps > 0 && rf2state.mMaxLaps < 1000 ? rf2state.mMaxLaps : 0;
            
            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630;
            csd.SessionTotalRunTime = (float)rf2state.mEndET > 0 ? (float)rf2state.mEndET : csd.SessionNumberOfLaps > 0 ? 0 : defaultSessionTotalRunTime;

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
                psd.SessionPhase == SessionPhase.Green) && 
                (csd.SessionPhase == SessionPhase.Garage || 
                csd.SessionPhase == SessionPhase.Gridwalk ||
                csd.SessionPhase == SessionPhase.Formation ||
                csd.SessionPhase == SessionPhase.Countdown)); 
            
            csd.SessionStartTime = csd.IsNewSession ? cgs.Now : psd.SessionStartTime;
            csd.SessionHasFixedTime = csd.SessionTotalRunTime > 0;
            csd.SessionRunningTime = (float)rf2state.mCurrentET;
            csd.SessionTimeRemaining = csd.SessionHasFixedTime ? csd.SessionTotalRunTime - csd.SessionRunningTime : 0;
            
            // hack for test day sessions running longer than allotted time
            csd.SessionTimeRemaining = csd.SessionTimeRemaining < 0 && rf2state.mSession == 0 ? defaultSessionTotalRunTime : csd.SessionTimeRemaining;

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
            csd.LapTimePrevious = player.mLastLapTime > 0 ? (float)player.mLastLapTime : -1;
            csd.LastSector1Time = player.mCurSector1 > 0 ? (float)player.mCurSector1 : -1;
            csd.LastSector2Time = player.mCurSector2 > 0 && player.mCurSector1 > 0 ? (float)(player.mCurSector2 - player.mCurSector1) : -1;
            csd.LastSector3Time = player.mLastLapTime > 0 && player.mCurSector2 > 0 ? (float)(player.mLastLapTime - player.mCurSector2) : -1;
            csd.PlayerBestSector1Time = player.mBestSector1 > 0 ? (float)player.mBestSector1 : -1;
            csd.PlayerBestSector2Time = player.mBestSector2 > 0 && player.mBestSector1 > 0 ? (float)(player.mBestSector2 - player.mBestSector1) : -1;
            csd.PlayerBestSector3Time = player.mBestLapTime > 0 && player.mBestSector2 > 0 ? (float)(player.mBestLapTime - player.mBestSector2) : -1;
            csd.PlayerBestLapSector1Time = csd.PlayerBestSector1Time;
            csd.PlayerBestLapSector2Time = csd.PlayerBestSector2Time;
            csd.PlayerBestLapSector3Time = csd.PlayerBestSector3Time;
            csd.PlayerLapTimeSessionBest = player.mBestLapTime > 0 ? (float)player.mBestLapTime : -1;
            csd.SessionTimesAtEndOfSectors = pgs != null ? psd.SessionTimesAtEndOfSectors : new SessionData().SessionTimesAtEndOfSectors;
            
            if (csd.IsNewSector && !csd.IsNewSession)
            {
                // there's a slight delay due to scoring updating every 200 ms, so we can't use SessionRunningTime here
                switch(csd.SectorNumber)
                {
                    case 1:
                        csd.SessionTimesAtEndOfSectors[3] = player.mLapStartET > 0 ? (float)player.mLapStartET : -1;
                        break;
                    case 2:
                        csd.SessionTimesAtEndOfSectors[1] = player.mLapStartET > 0 && player.mCurSector1 > 0 ? (float)(player.mLapStartET + player.mCurSector1) : -1;
                        break;
                    case 3:
                        csd.SessionTimesAtEndOfSectors[2] = player.mLapStartET > 0 && player.mCurSector2 > 0 ? (float)(player.mLapStartET + player.mCurSector2) : -1;
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
            if (csd.SessionType != SessionType.HotLap)
            {    
                cgs.CarDamageData.DamageEnabled = true;

                const double MINOR_DAMAGE_THRESHOLD = 1500.0;
                const double MAJOR_DAMAGE_THRESHOLD = 4000.0;
                const double ACCUMULATED_THRESHOLD_FACTOR = 2.5;

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
                    || rf2state.mAccumulatedImpactMagnitude > MINOR_DAMAGE_THRESHOLD * ACCUMULATED_THRESHOLD_FACTOR)
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

            float frontLeftTemp = (cgs.TyreData.FrontLeft_CenterTemp + cgs.TyreData.FrontLeft_LeftTemp + cgs.TyreData.FrontLeft_RightTemp) / 3;
            cgs.TyreData.FrontLeftPressure = wheelFrontLeft.mFlat == 0 ? (float)wheelFrontLeft.mPressure : 0.0f;
            cgs.TyreData.FrontLeftPercentWear = (float)(1 - wheelFrontLeft.mWear) * 100;
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

            float frontRightTemp = (cgs.TyreData.FrontRight_CenterTemp + cgs.TyreData.FrontRight_LeftTemp + cgs.TyreData.FrontRight_RightTemp) / 3;
            cgs.TyreData.FrontRightPressure = wheelFrontRight.mFlat == 0 ? (float)wheelFrontRight.mPressure : 0.0f;
            cgs.TyreData.FrontRightPercentWear = (float)(1 - wheelFrontRight.mWear) * 100;
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

            float rearLeftTemp = (cgs.TyreData.RearLeft_CenterTemp + cgs.TyreData.RearLeft_LeftTemp + cgs.TyreData.RearLeft_RightTemp) / 3;
            cgs.TyreData.RearLeftPressure = wheelRearLeft.mFlat == 0 ? (float)wheelRearLeft.mPressure : 0.0f;
            cgs.TyreData.RearLeftPercentWear = (float)(1 - wheelRearLeft.mWear) * 100;
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

            float rearRightTemp = (cgs.TyreData.RearRight_CenterTemp + cgs.TyreData.RearRight_LeftTemp + cgs.TyreData.RearRight_RightTemp) / 3;
            cgs.TyreData.RearRightPressure = wheelRearRight.mFlat == 0 ? (float)wheelRearRight.mPressure : 0.0f;
            cgs.TyreData.RearRightPercentWear = (float)(1 - wheelRearRight.mWear) * 100;
            if (csd.IsNewLap)
            {
                cgs.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (pgs == null || rearRightTemp > pgs.TyreData.PeakRearRightTemperatureForLap)
            {
                cgs.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            cgs.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, cgs.TyreData.FrontLeftPercentWear,
                cgs.TyreData.FrontRightPercentWear, cgs.TyreData.RearLeftPercentWear, cgs.TyreData.RearRightPercentWear);

            cgs.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[cgs.carClass.defaultTyreType],
                cgs.TyreData.PeakFrontLeftTemperatureForLap, cgs.TyreData.PeakFrontRightTemperatureForLap,
                cgs.TyreData.PeakRearLeftTemperatureForLap, cgs.TyreData.PeakRearRightTemperatureForLap);
            // some simple locking / spinning checks
            if ((csd.IsNewSession || 
                wheelCircumference[0] == 0 || wheelCircumference[1] == 0) && 
                cgs.PositionAndMotionData.CarSpeed > 14 && 
                Math.Abs(rf2state.mUnfilteredSteering) <= 0.05)
            {
                // calculate wheel circumference (assume left/right symmetry) at 50+ km/h with (mostly) straight steering
                // front
                wheelCircumference[0] = (float)(2 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[0].mRotation) + 
                    2 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[1].mRotation)) / 2;
                // rear
                wheelCircumference[1] = (float)(2 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[2].mRotation) + 
                    2 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / Math.Abs(rf2state.mWheels[3].mRotation)) / 2;
            }
            if (cgs.PositionAndMotionData.CarSpeed > 7 && 
                wheelCircumference[0] > 0 && wheelCircumference[1] > 0)
            {
                float[] rotatingSpeed = new float[] { 
                    2 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / wheelCircumference[0], 
                    2 * (float)Math.PI * cgs.PositionAndMotionData.CarSpeed / wheelCircumference[1] };
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
            cgs.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(suspensionDamageThresholds,
                !cgs.TyreData.LeftFrontAttached ? 1 : 0,
                !cgs.TyreData.RightFrontAttached ? 1 : 0,
                !cgs.TyreData.LeftRearAttached ? 1 : 0,
                !cgs.TyreData.RightRearAttached ? 1 : 0);

            // --------------------------------
            // brake data
            // TODO: Verify, comment says it is Celsius!
            // rF2 reports in Kelvin
            cgs.TyreData.LeftFrontBrakeTemp = (float)wheelFrontLeft.mBrakeTemp - 273.15f;
            cgs.TyreData.RightFrontBrakeTemp = (float)wheelFrontRight.mBrakeTemp - 273.15f;
            cgs.TyreData.LeftRearBrakeTemp = (float)wheelRearLeft.mBrakeTemp - 273.15f;
            cgs.TyreData.RightRearBrakeTemp = (float)wheelRearRight.mBrakeTemp - 273.15f;

            if (brakeTempThresholdsForPlayersCar != null)
            {
                cgs.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar,
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
            isOfflineSession = true;
            opponentKeysProcessed.Clear();
            for (int i = 0; i < rf2state.mVehicles.Length; ++i)
            {
                var vehicle = rf2state.mVehicles[i];
                if (vehicle.mIsPlayer == 1)
                {
                    csd.OverallSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0 ?
                        csd.PlayerLapTimeSessionBest : -1;
                    csd.PlayerClassSessionBestLapTime = csd.PlayerLapTimeSessionBest > 0 ?
                        csd.PlayerLapTimeSessionBest : -1;
                    continue;
                }
                switch (mapToControlType((rFactor2Constants.rF2Control)vehicle.mControl))
                {
                    case ControlType.Player:
                    case ControlType.Replay:
                    case ControlType.Unavailable:
                        continue;
                    case ControlType.Remote:
                        isOfflineSession = false;
                        break;
                    default:
                        break;
                }
                String opponentKey = getNameFromBytes(vehicle.mVehicleClass) + vehicle.mPlace.ToString();
                OpponentData opponentPrevious = getOpponentDataForVehicleInfo(vehicle, pgs, csd.SessionRunningTime);
                OpponentData opponent = new OpponentData();
                opponent.DriverRawName = getNameFromBytes(vehicle.mDriverName).ToLower();
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.CarClass = CarData.getCarClassForRF1ClassName(getNameFromBytes(vehicle.mVehicleClass));
                opponent.Position = vehicle.mPlace;
                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName + 
                        " is using car class " + opponent.CarClass.rF1ClassName + 
                        " at position " + opponent.Position.ToString());
                }
                if (opponentPrevious != null)
                {
                    foreach (LapData old in opponentPrevious.OpponentLapData)
                    {
                        opponent.OpponentLapData.Add(old);
                    }
                }
                opponent.UnFilteredPosition = opponent.Position;
                opponent.SessionTimeAtLastPositionChange = opponentPrevious != null && opponentPrevious.Position != opponent.Position ? csd.SessionRunningTime : -1;
                opponent.CompletedLaps = vehicle.mTotalLaps;
                opponent.CurrentSectorNumber = vehicle.mSector == 0 ? 3 : vehicle.mSector;
                Boolean isNewSector = csd.IsNewSession || (opponentPrevious != null && opponentPrevious.CurrentSectorNumber != opponent.CurrentSectorNumber);
                opponent.IsNewLap = csd.IsNewSession || (isNewSector && opponent.CurrentSectorNumber == 1);
                opponent.Speed = (float)vehicle.mSpeed;
                opponent.DistanceRoundTrack = (float)vehicle.mLapDist;
                opponent.WorldPosition = new float[] { (float)vehicle.mPos.x, (float)vehicle.mPos.z };
                opponent.CurrentBestLapTime = vehicle.mBestLapTime > 0 ? (float)vehicle.mBestLapTime : -1;
                opponent.PreviousBestLapTime = opponentPrevious != null && opponentPrevious.CurrentBestLapTime > 0 && 
                    opponentPrevious.CurrentBestLapTime > opponent.CurrentBestLapTime ? opponentPrevious.CurrentBestLapTime : -1;
                opponent.bestSector1Time = vehicle.mBestSector1 > 0 ? (float)vehicle.mBestSector1 : -1;
                opponent.bestSector2Time = vehicle.mBestSector2 > 0 && vehicle.mBestSector1 > 0 ? (float)(vehicle.mBestSector2 - vehicle.mBestSector1) : -1;
                opponent.bestSector3Time = vehicle.mBestLapTime > 0 && vehicle.mBestSector2 > 0 ?  (float)(vehicle.mBestLapTime - vehicle.mBestSector2) : -1;
                opponent.LastLapTime = vehicle.mLastLapTime > 0 ? (float)vehicle.mLastLapTime : -1;
                float lastSectorTime = -1;
                switch (opponent.CurrentSectorNumber)
                {
                    case 1:
                        lastSectorTime = vehicle.mLastLapTime > 0 ? (float)vehicle.mLastLapTime : -1;
                        break;
                    case 2:
                        lastSectorTime = vehicle.mLastSector1 > 0 ? (float)vehicle.mLastSector1 : -1;
                        break;
                    case 3:
                        lastSectorTime = vehicle.mLastSector2 > 0 ? (float)vehicle.mLastSector2 : -1;
                        break;
                    default:
                        break;
                }
                if (opponent.IsNewLap)
                {
                    if (lastSectorTime > 0)
                    {
                        opponent.CompleteLapWithProvidedLapTime(opponent.Position, (float)vehicle.mLapStartET, lastSectorTime, true, false, (float)rf2state.mTrackTemp, (float)rf2state.mAmbientTemp, csd.SessionHasFixedTime, csd.SessionTimeRemaining);
                    }
                    opponent.StartNewLap(opponent.CompletedLaps + 1, opponent.Position, vehicle.mInPits == 1 || opponent.DistanceRoundTrack < 0, (float)vehicle.mLapStartET, false, (float)rf2state.mTrackTemp, (float)rf2state.mAmbientTemp);
                }
                else if (isNewSector && lastSectorTime > 0)
                {
                    opponent.AddCumulativeSectorData(opponent.Position, lastSectorTime, (float)vehicle.mLapStartET + lastSectorTime, true, false, (float)rf2state.mTrackTemp, (float)rf2state.mAmbientTemp);
                }
                if (vehicle.mInPits == 1 && opponent.CurrentSectorNumber == 3 && opponentPrevious != null && !opponentPrevious.isEnteringPits())
                {
                    opponent.setInLap();
                    LapData currentLapData = opponent.getCurrentLapData();
                    int sector3Position = currentLapData != null && currentLapData.SectorPositions.Count > 2 ? currentLapData.SectorPositions[2] : opponent.Position;
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
                if (opponentPrevious != null && opponentPrevious.Position > 1 && opponent.Position == 1)
                {
                    csd.HasLeadChanged = true;
                }
                // session best lap times
                if (opponent.Position == csd.Position + 1)
                {
                    csd.TimeDeltaBehind = (float)vehicle.mTimeBehindNext;
                }
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestOverall || 
                    csd.OpponentsLapTimeSessionBestOverall < 0))
                {
                    csd.OpponentsLapTimeSessionBestOverall = opponent.CurrentBestLapTime;
                }
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < csd.OverallSessionBestLapTime ||
                    csd.OverallSessionBestLapTime < 0))
                {
                    csd.OverallSessionBestLapTime = opponent.CurrentBestLapTime;
                }
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < csd.OpponentsLapTimeSessionBestPlayerClass ||
                    csd.OpponentsLapTimeSessionBestPlayerClass < 0) && 
                    opponent.CarClass == cgs.carClass)
                {
                    csd.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;
                }
                // shouldn't have duplicates, but just in case
                if (!cgs.OpponentData.ContainsKey(opponentKey))
                {
                    cgs.OpponentData.Add(opponentKey, opponent);
                }
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
                csd.IsRacingSameCarInFront = !((oPrev == null && oCurr != null) || (oPrev != null && oCurr == null) || (oPrev != null && oCurr != null && oPrev.DriverRawName != oCurr.DriverRawName));
                oPrevKey = (String)pgs.getOpponentKeyBehindOnTrack();
                oCurrKey = (String)cgs.getOpponentKeyBehindOnTrack();
                oPrev = oPrevKey != null ? pgs.OpponentData[oPrevKey] : null;
                oCurr = oCurrKey != null ? cgs.OpponentData[oCurrKey] : null;
                csd.IsRacingSameCarBehind = !((oPrev == null && oCurr != null) || (oPrev != null && oCurr == null) || (oPrev != null && oCurr != null && oPrev.DriverRawName != oCurr.DriverRawName));
                csd.GameTimeAtLastPositionFrontChange = !csd.IsRacingSameCarInFront ? 
                    csd.SessionRunningTime : psd.GameTimeAtLastPositionFrontChange;
                csd.GameTimeAtLastPositionBehindChange = !csd.IsRacingSameCarBehind ? 
                    csd.SessionRunningTime : psd.GameTimeAtLastPositionBehindChange;
            }

            // --------------------------------
            // pit data
            cgs.PitData.IsRefuellingAllowed = true;
            cgs.PitData.HasMandatoryPitStop = isOfflineSession && rf2state.mScheduledStops > 0 && player.mNumPitstops < rf2state.mScheduledStops && csd.SessionType == SessionType.Race;
            cgs.PitData.PitWindowStart = isOfflineSession && cgs.PitData.HasMandatoryPitStop ? 1 : 0;
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
            if ((csd.SessionType == SessionType.Race &&
                (csd.SessionPhase == SessionPhase.Green ||
                csd.SessionPhase == SessionPhase.Finished ||
                csd.SessionPhase == SessionPhase.Checkered)) ||
                (!cgs.PitData.InPitlane && csd.CompletedLaps > 1))
            {
                cgs.FuelData.FuelUseActive = true;
                cgs.FuelData.FuelLeft = (float)rf2state.mFuel;
            }

            var sectorIndex = (player.mSector == 0 ? 3 : player.mSector) - 1;
        
            // TODO: this whole code is messed up for rF2, rework
            // --------------------------------
            // flags data
            FlagEnum Flag = FlagEnum.UNKNOWN;
            if (csd.IsDisqualified
                && pgs != null 
                && !psd.IsDisqualified)
            {
                Flag = FlagEnum.BLACK;
            }
            else if (rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.GreenFlag 
                && rf2state.mSectorFlag[sectorIndex] == (int)rFactor2Constants.rF2YellowFlagState.Pending)
            {
                // TODO: we need message per sector as well.
                Flag = FlagEnum.YELLOW;
            }
            else if (csd.SessionType == SessionType.Race ||
                csd.SessionType == SessionType.Qualify)
            {
                if (rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.FullCourseYellow
                    && rf2state.mYellowFlagState != (int)rFactor2Constants.rF2YellowFlagState.LastLap)
                {
                    // TODO: Play various SC phase events.
                    Flag = FlagEnum.DOUBLE_YELLOW;
                }
                else if ((rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.FullCourseYellow
                    && rf2state.mYellowFlagState == (int)rFactor2Constants.rF2YellowFlagState.LastLap)
                    || csd.LeaderHasFinishedRace)
                {
                    Flag = FlagEnum.WHITE;
                }
                else if (rf2state.mGamePhase == (int)rFactor2Constants.rF2GamePhase.GreenFlag
                    && pgs != null
                    && (psd.Flag == FlagEnum.DOUBLE_YELLOW || psd.Flag == FlagEnum.WHITE))
                {
                    Flag = FlagEnum.GREEN;
                }
            }
            foreach (OpponentData opponent in cgs.OpponentData.Values)
            {
                if (csd.SessionType != SessionType.Race || 
                    csd.CompletedLaps < 1 || 
                    cgs.PositionAndMotionData.DistanceRoundTrack < 0)
                {
                    break;
                }
                if (opponent.getCurrentLapData().InLap || 
                    opponent.getCurrentLapData().OutLap || 
                    opponent.Position > csd.Position)
                {
                    continue;
                }
                if (isBehindWithinDistance(csd.TrackDefinition.trackLength, 8, 40, 
                    cgs.PositionAndMotionData.DistanceRoundTrack, opponent.DistanceRoundTrack) && 
                    opponent.Speed >= cgs.PositionAndMotionData.CarSpeed)
                {
                    Flag = FlagEnum.BLUE;
                    break;
                }
            }
            csd.Flag = Flag;

            // --------------------------------
            // penalties data
            cgs.PenaltiesData.NumPenalties = player.mNumPenalties;
            float lateralDistDiff = (float)(Math.Abs(player.mPathLateral) - Math.Abs(player.mTrackEdge));
            cgs.PenaltiesData.IsOffRacingSurface = !cgs.PitData.InPitlane && lateralDistDiff >= 2;
            float offTrackDistanceDelta = lateralDistDiff - distanceOffTrack;
            distanceOffTrack = cgs.PenaltiesData.IsOffRacingSurface ? lateralDistDiff : 0;
            isApproachingTrack = offTrackDistanceDelta < 0 && cgs.PenaltiesData.IsOffRacingSurface && lateralDistDiff < 3;
            // primitive cut track detection for Reiza Time Trial Mode
            if (csd.SessionType == SessionType.HotLap)
            {
                if ((pgs != null && !psd.CurrentLapIsValid &&
                    psd.CompletedLaps == csd.CompletedLaps) || 
                    cgs.PenaltiesData.IsOffRacingSurface)
                {
                    csd.CurrentLapIsValid = false;
                }
            }
            if ((((csd.SectorNumber == 2 && csd.LastSector1Time < 0) || 
                (csd.SectorNumber == 3 && csd.LastSector2Time < 0)) && 
                !cgs.PitData.OnOutLap && !cgs.PitData.OnInLap &&
                (csd.SessionType == SessionType.Race || csd.SessionType == SessionType.Qualify)) || 
                (pgs != null && psd.CompletedLaps == csd.CompletedLaps && 
                !psd.CurrentLapIsValid))
            {
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
                Console.WriteLine("Player is using car class " + cgs.carClass.rF1ClassName + 
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
            if (isOfflineSession)
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
            float timeDelta = previousGameState != null ? sessionRunningTime - prevSessionData.SessionRunningTime : -1;
            if (previousGameState != null && timeDelta >= 0)
            {
                float[] worldPos = { (float)vehicle.mPos.x, (float)vehicle.mPos.z };
                float minDistDiff = -1;
                foreach (OpponentData o in previousGameState.OpponentData.Values)
                {
                    String opponentKey = o.CarClass.rF1ClassName + o.Position.ToString();
                    if (o.DriverRawName != getNameFromBytes(vehicle.mDriverName).ToLower() || 
                        o.CarClass != CarData.getCarClassForRF1ClassName(getNameFromBytes(vehicle.mVehicleClass)) || 
                        opponentKeysProcessed.Contains(opponentKey))
                    {
                        continue;
                    }
                    // distance from predicted position
                    float targetDist = o.Speed * timeDelta;
                    float dist = (float)Math.Abs(Math.Sqrt((double)((o.WorldPosition[0] - worldPos[0]) * (o.WorldPosition[0] - worldPos[0]) + 
                        (o.WorldPosition[1] - worldPos[1]) * (o.WorldPosition[1] - worldPos[1]))) - targetDist);
                    if (minDistDiff < 0 || dist < minDistDiff)
                    {
                        minDistDiff = dist;
                        opponentPrevious = o;
                    }
                }
                if (opponentPrevious != null)
                {
                    opponentKeysProcessed.Add(opponentPrevious.CarClass.rF1ClassName + opponentPrevious.Position.ToString());
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
                // test day and pre-race warm-up sessions are 'Practice' as well since 'HotLap' seems to suppress flag info
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
                // Reiza Time Trial Mode
                // TODO: verify
                case 14:
                    return SessionType.HotLap;
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

        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        } 
    }
}
