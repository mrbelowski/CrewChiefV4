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
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));

            suspensionDamageThresholds.Add(new CornerData.EnumWithThresholds(DamageLevel.NONE, 0, 1));
            suspensionDamageThresholds.Add(new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, 1, 2));
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
            CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper wrapper = (CrewChiefV4.rFactor2.RF2SharedMemoryReader.RF2StructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            rFactor2Data.rF2State shared = wrapper.state;

            // no session data
            if (shared.mNumVehicles == 0)
            {
                isOfflineSession = true;
                distanceOffTrack = 0;
                isApproachingTrack = false;
                wheelCircumference = new float[] { 0, 0 };
                previousGameState = null;
                return null;
            }
            // game is paused or other window has taken focus
            if (shared.mDeltaTime >= 0.56)
            {
                return previousGameState;
            }

            // --------------------------------
            // session data
            // get player scoring info (usually index 0)
            // get session leader scoring info (usually index 1 if not player)
            rFactor2Data.rF2VehScoringInfo player = new rF2VehScoringInfo();
            rFactor2Data.rF2VehScoringInfo leader = new rF2VehScoringInfo();
            for (int i = 0; i < shared.mNumVehicles; i++)
            {
                rFactor2Data.rF2VehScoringInfo vehicle = shared.mVehicles[i];
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
                return previousGameState;
            }
            if (playerName == null)
            {

                // TODO:Don't think rF2 is UTF 8
                var driverName = getNameFromBytes(player.mDriverName).ToLower();
                NameValidator.validateName(driverName);
                playerName = driverName;
            }
            // these things should remain constant during a session
            currentGameState.SessionData.EventIndex = shared.mSession;
            currentGameState.SessionData.SessionIteration = 
                shared.mSession >= 1 && shared.mSession <= 4 ? shared.mSession - 1 :
                shared.mSession >= 5 && shared.mSession <= 8 ? shared.mSession - 5 :
                shared.mSession >= 10 && shared.mSession <= 13 ? shared.mSession - 10 : 0;
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            currentGameState.SessionData.SessionPhase = mapToSessionPhase((rFactor2Constants.rF2GamePhase)shared.mGamePhase);
            currentGameState.carClass = CarData.getCarClassForRF1ClassName(getNameFromBytes(player.mVehicleClass));
            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
            currentGameState.SessionData.DriverRawName = getNameFromBytes(player.mDriverName).ToLower();
            currentGameState.SessionData.TrackDefinition = new TrackDefinition(getNameFromBytes(shared.mTrackName), (float)shared.mLapDist);
            currentGameState.SessionData.TrackDefinition.setGapPoints();
            currentGameState.SessionData.SessionNumberOfLaps = shared.mMaxLaps > 0 && shared.mMaxLaps < 1000 ? shared.mMaxLaps : 0;
            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630;
            currentGameState.SessionData.SessionTotalRunTime = (float)shared.mEndET > 0 ? (float)shared.mEndET : currentGameState.SessionData.SessionNumberOfLaps > 0 ? 0 : defaultSessionTotalRunTime;
            // if previous state is null or any of the above change, this is a new session
            currentGameState.SessionData.IsNewSession = previousGameState == null ||
                currentGameState.SessionData.SessionType != previousGameState.SessionData.SessionType ||
                currentGameState.carClass != previousGameState.carClass ||
                currentGameState.SessionData.DriverRawName != previousGameState.SessionData.DriverRawName || 
                currentGameState.SessionData.TrackDefinition.name != previousGameState.SessionData.TrackDefinition.name ||
                currentGameState.SessionData.TrackDefinition.trackLength != previousGameState.SessionData.TrackDefinition.trackLength ||
                // these sometimes change in the beginning or end of session!
                //currentGameState.SessionData.SessionNumberOfLaps != previousGameState.SessionData.SessionNumberOfLaps ||
                //currentGameState.SessionData.SessionTotalRunTime != previousGameState.SessionData.SessionTotalRunTime || 
                currentGameState.SessionData.EventIndex != previousGameState.SessionData.EventIndex || 
                currentGameState.SessionData.SessionIteration != previousGameState.SessionData.SessionIteration || 
                ((previousGameState.SessionData.SessionPhase == SessionPhase.Checkered || 
                previousGameState.SessionData.SessionPhase == SessionPhase.Finished || 
                previousGameState.SessionData.SessionPhase == SessionPhase.Green) && 
                (currentGameState.SessionData.SessionPhase == SessionPhase.Garage || 
                currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Countdown)); 
            currentGameState.SessionData.SessionStartTime = currentGameState.SessionData.IsNewSession ? currentGameState.Now : previousGameState.SessionData.SessionStartTime;
            currentGameState.SessionData.SessionHasFixedTime = currentGameState.SessionData.SessionTotalRunTime > 0;
            currentGameState.SessionData.SessionRunningTime = (float)shared.mCurrentET;
            currentGameState.SessionData.SessionTimeRemaining = currentGameState.SessionData.SessionHasFixedTime ? currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionRunningTime : 0;
            // hack for test day sessions running longer than allotted time
            currentGameState.SessionData.SessionTimeRemaining = currentGameState.SessionData.SessionTimeRemaining < 0 && shared.mSession == 0 ? defaultSessionTotalRunTime : currentGameState.SessionData.SessionTimeRemaining;
            currentGameState.SessionData.NumCars = shared.mNumVehicles;
            currentGameState.SessionData.NumCarsAtStartOfSession = currentGameState.SessionData.IsNewSession ? currentGameState.SessionData.NumCars : previousGameState.SessionData.NumCarsAtStartOfSession;
            currentGameState.SessionData.Position = player.mPlace;
            currentGameState.SessionData.UnFilteredPosition = currentGameState.SessionData.Position;
            currentGameState.SessionData.SessionStartPosition = currentGameState.SessionData.IsNewSession ? currentGameState.SessionData.Position : previousGameState.SessionData.SessionStartPosition;
            currentGameState.SessionData.SectorNumber = player.mSector == 0 ? 3 : player.mSector;
            currentGameState.SessionData.IsNewSector = currentGameState.SessionData.IsNewSession || currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;
            currentGameState.SessionData.IsNewLap = currentGameState.SessionData.IsNewSession || (currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == 1);
            currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.IsNewLap ? currentGameState.SessionData.Position : previousGameState.SessionData.PositionAtStartOfCurrentLap;
            currentGameState.SessionData.IsDisqualified = (rFactor2Constants.rF2FinishStatus)player.mFinishStatus == rFactor2Constants.rF2FinishStatus.Dq;
            currentGameState.SessionData.CompletedLaps = player.mTotalLaps;
            currentGameState.SessionData.LapTimeCurrent = currentGameState.SessionData.SessionRunningTime - (float)player.mLapStartET;
            currentGameState.SessionData.LapTimePrevious = player.mLastLapTime > 0 ? (float)player.mLastLapTime : -1;
            currentGameState.SessionData.LastSector1Time = player.mCurSector1 > 0 ? (float)player.mCurSector1 : -1;
            currentGameState.SessionData.LastSector2Time = player.mCurSector2 > 0 && player.mCurSector1 > 0 ? (float)(player.mCurSector2 - player.mCurSector1) : -1;
            currentGameState.SessionData.LastSector3Time = player.mLastLapTime > 0 && player.mCurSector2 > 0 ? (float)(player.mLastLapTime - player.mCurSector2) : -1;
            currentGameState.SessionData.PlayerBestSector1Time = player.mBestSector1 > 0 ? (float)player.mBestSector1 : -1;
            currentGameState.SessionData.PlayerBestSector2Time = player.mBestSector2 > 0 && player.mBestSector1 > 0 ? (float)(player.mBestSector2 - player.mBestSector1) : -1;
            currentGameState.SessionData.PlayerBestSector3Time = player.mBestLapTime > 0 && player.mBestSector2 > 0 ? (float)(player.mBestLapTime - player.mBestSector2) : -1;
            currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.PlayerBestSector1Time;
            currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.PlayerBestSector2Time;
            currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.PlayerBestSector3Time;
            currentGameState.SessionData.PlayerLapTimeSessionBest = player.mBestLapTime > 0 ? (float)player.mBestLapTime : -1;
            currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState != null ? previousGameState.SessionData.SessionTimesAtEndOfSectors : new SessionData().SessionTimesAtEndOfSectors;
            if (currentGameState.SessionData.IsNewSector && !currentGameState.SessionData.IsNewSession)
            {
                // there's a slight delay due to scoring updating every 200 ms, so we can't use SessionRunningTime here
                switch(currentGameState.SessionData.SectorNumber)
                {
                    case 1:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = player.mLapStartET > 0 ? (float)player.mLapStartET : -1;
                        break;
                    case 2:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = player.mLapStartET > 0 && player.mCurSector1 > 0 ? (float)(player.mLapStartET + player.mCurSector1) : -1;
                        break;
                    case 3:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = player.mLapStartET > 0 && player.mCurSector2 > 0 ? (float)(player.mLapStartET + player.mCurSector2) : -1;
                        break;
                    default:
                        break;
                }
            }
            if (previousGameState != null)
            {
                foreach (String lt in previousGameState.SessionData.formattedPlayerLapTimes)
                {
                    currentGameState.SessionData.formattedPlayerLapTimes.Add(lt);
                }
            }
            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.LapTimePrevious > 0)
            {
                currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious).ToString(@"mm\:ss\.fff"));
            }
            currentGameState.SessionData.LeaderHasFinishedRace = leader.mFinishStatus == (int)rFactor2Constants.rF2FinishStatus.Finished;
            currentGameState.SessionData.TimeDeltaFront = (float)player.mTimeBehindNext;

            // --------------------------------
            // engine data
            currentGameState.EngineData.EngineRpm = (float)shared.mEngineRPM;
            currentGameState.EngineData.MaxEngineRpm = (float)shared.mEngineMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            currentGameState.EngineData.EngineOilTemp = (float)shared.mEngineOilTemp;
            currentGameState.EngineData.EngineWaterTemp = (float)shared.mEngineWaterTemp;
            //HACK: there's probably a cleaner way to do this...
            if (shared.mOverheating == 1)
            {
                currentGameState.EngineData.EngineWaterTemp += 50;
                currentGameState.EngineData.EngineOilTemp += 50;
            }

            // --------------------------------
            // transmission data
            currentGameState.TransmissionData.Gear = shared.mGear;

            // controls
            currentGameState.ControlData.BrakePedal = (float)shared.mUnfilteredBrake;
            currentGameState.ControlData.ThrottlePedal = (float)shared.mUnfilteredThrottle;
            currentGameState.ControlData.ClutchPedal = (float)shared.mUnfilteredClutch;

            // --------------------------------
            // damage
            // not 100% certain on this mapping but it should be reasonably close
            if (currentGameState.SessionData.SessionType != SessionType.HotLap)
            {
                currentGameState.CarDamageData.DamageEnabled = true;
                int bodyDamage = 0;
                foreach (int dent in shared.mDentSeverity)
                {
                    bodyDamage += dent;
                }
                switch (bodyDamage)
                {
                    // there's suspension damage included in these bytes but I'm not sure which ones
                    case 0:
                        currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.NONE;
                        break;
                    case 1:
                        currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.TRIVIAL;
                        break;
                    case 2:
                    case 3:
                        currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MINOR;
                        break;
                    case 4:
                    case 5:
                        currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MAJOR;
                        break;
                    default:
                        currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.DESTROYED;
                        break;
                }
            }

            // --------------------------------
            // control data
            currentGameState.ControlData.ControlType = mapToControlType((rFactor2Constants.rF2Control)player.mControl);

            // --------------------------------
            // motion data
            currentGameState.PositionAndMotionData.CarSpeed = (float)shared.mSpeed;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = (float)player.mLapDist;

            // --------------------------------
            // tire data
            // Automobilista reports in Kelvin
            currentGameState.TyreData.TireWearActive = true;
            currentGameState.TyreData.LeftFrontAttached = shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mDetached == 0;
            currentGameState.TyreData.FrontLeft_LeftTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTemperature[0] - 273;
            currentGameState.TyreData.FrontLeft_CenterTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTemperature[1] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mTemperature[2] - 273;

            float frontLeftTemp = (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3;
            currentGameState.TyreData.FrontLeftPressure = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mPressure;
            currentGameState.TyreData.FrontLeftPercentWear = (float)(1 - shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mWear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }
            else if (previousGameState == null || frontLeftTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }

            currentGameState.TyreData.RightFrontAttached = shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mDetached == 0;
            currentGameState.TyreData.FrontRight_LeftTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTemperature[0] - 273;
            currentGameState.TyreData.FrontRight_CenterTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTemperature[1] - 273;
            currentGameState.TyreData.FrontRight_RightTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mTemperature[2] - 273;

            float frontRightTemp = (currentGameState.TyreData.FrontRight_CenterTemp + currentGameState.TyreData.FrontRight_LeftTemp + currentGameState.TyreData.FrontRight_RightTemp) / 3;
            currentGameState.TyreData.FrontRightPressure = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mPressure;
            currentGameState.TyreData.FrontRightPercentWear = (float)(1 - shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mWear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }
            else if (previousGameState == null || frontRightTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }

            currentGameState.TyreData.LeftRearAttached = shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mDetached == 0;
            currentGameState.TyreData.RearLeft_LeftTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTemperature[0] - 273;
            currentGameState.TyreData.RearLeft_CenterTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTemperature[1] - 273;
            currentGameState.TyreData.RearLeft_RightTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mTemperature[2] - 273;

            float rearLeftTemp = (currentGameState.TyreData.RearLeft_CenterTemp + currentGameState.TyreData.RearLeft_LeftTemp + currentGameState.TyreData.RearLeft_RightTemp) / 3;
            currentGameState.TyreData.RearLeftPressure = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mPressure;
            currentGameState.TyreData.RearLeftPercentWear = (float)(1 - shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mWear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }
            else if (previousGameState == null || rearLeftTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }

            currentGameState.TyreData.RightRearAttached = shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mDetached == 0;
            currentGameState.TyreData.RearRight_LeftTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTemperature[0] - 273;
            currentGameState.TyreData.RearRight_CenterTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTemperature[1] - 273;
            currentGameState.TyreData.RearRight_RightTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mTemperature[2] - 273;

            float rearRightTemp = (currentGameState.TyreData.RearRight_CenterTemp + currentGameState.TyreData.RearRight_LeftTemp + currentGameState.TyreData.RearRight_RightTemp) / 3;
            currentGameState.TyreData.RearRightPressure = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mPressure;
            currentGameState.TyreData.RearRightPercentWear = (float)(1 - shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mWear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (previousGameState == null || rearRightTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear,
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[currentGameState.carClass.defaultTyreType],
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap, currentGameState.TyreData.PeakFrontRightTemperatureForLap,
                currentGameState.TyreData.PeakRearLeftTemperatureForLap, currentGameState.TyreData.PeakRearRightTemperatureForLap);
            // some simple locking / spinning checks
            if ((currentGameState.SessionData.IsNewSession || 
                wheelCircumference[0] == 0 || wheelCircumference[1] == 0) && 
                currentGameState.PositionAndMotionData.CarSpeed > 14 && 
                Math.Abs(shared.mUnfilteredSteering) <= 0.05)
            {
                // calculate wheel circumference (assume left/right symmetry) at 50+ km/h with (mostly) straight steering
                // front
                wheelCircumference[0] = (float)(2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / Math.Abs(shared.mWheels[0].mRotation) + 
                    2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / Math.Abs(shared.mWheels[1].mRotation)) / 2;
                // rear
                wheelCircumference[1] = (float)(2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / Math.Abs(shared.mWheels[2].mRotation) + 
                    2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / Math.Abs(shared.mWheels[3].mRotation)) / 2;
            }
            if (currentGameState.PositionAndMotionData.CarSpeed > 7 && 
                wheelCircumference[0] > 0 && wheelCircumference[1] > 0)
            {
                float[] rotatingSpeed = new float[] { 
                    2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / wheelCircumference[0], 
                    2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / wheelCircumference[1] };
                float minRotFactor = 0.5f;
                float maxRotFactor = 1.3f;

                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mRotation) < minRotFactor * rotatingSpeed[0];
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mRotation) < minRotFactor * rotatingSpeed[0];
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mRotation) < minRotFactor * rotatingSpeed[1];
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mRotation) < minRotFactor * rotatingSpeed[1];

                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mRotation) > maxRotFactor * rotatingSpeed[0];
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mRotation) > maxRotFactor * rotatingSpeed[0];
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mRotation) > maxRotFactor * rotatingSpeed[1];
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mRotation) > maxRotFactor * rotatingSpeed[1];
            }
            // use detached wheel status for suspension damage
            currentGameState.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(suspensionDamageThresholds,
                !currentGameState.TyreData.LeftFrontAttached ? 1 : 0,
                !currentGameState.TyreData.RightFrontAttached ? 1 : 0,
                !currentGameState.TyreData.LeftRearAttached ? 1 : 0,
                !currentGameState.TyreData.RightRearAttached ? 1 : 0);

            // --------------------------------
            // brake data
            // Automobilista reports in Kelvin
            currentGameState.TyreData.LeftFrontBrakeTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontLeft].mBrakeTemp - 273;
            currentGameState.TyreData.RightFrontBrakeTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.FrontRight].mBrakeTemp - 273;
            currentGameState.TyreData.LeftRearBrakeTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearLeft].mBrakeTemp - 273;
            currentGameState.TyreData.RightRearBrakeTemp = (float)shared.mWheels[(int)rFactor2Constants.rF2WheelIndex.RearRight].mBrakeTemp - 273;

            if (brakeTempThresholdsForPlayersCar != null)
            {
                currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar,
                    currentGameState.TyreData.LeftFrontBrakeTemp, currentGameState.TyreData.RightFrontBrakeTemp,
                    currentGameState.TyreData.LeftRearBrakeTemp, currentGameState.TyreData.RightRearBrakeTemp);
            }

            // --------------------------------
            // track conditions
            if (currentGameState.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < currentGameState.Now)
            {
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    (float)shared.mAmbientTemp, (float)shared.mTrackTemp, 0, (float)Math.Sqrt((double)(shared.mWind.x * shared.mWind.x + shared.mWind.y * shared.mWind.y + shared.mWind.z * shared.mWind.z)), 0, 0, 0);
            }

            // --------------------------------
            // opponent data
            isOfflineSession = true;
            opponentKeysProcessed.Clear();
            for (int i = 0; i < shared.mVehicles.Length; i++)
            {
                rFactor2Data.rF2VehScoringInfo vehicle = shared.mVehicles[i];
                if (vehicle.mIsPlayer == 1)
                {
                    currentGameState.SessionData.OverallSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest > 0 ?
                        currentGameState.SessionData.PlayerLapTimeSessionBest : -1;
                    currentGameState.SessionData.PlayerClassSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest > 0 ?
                        currentGameState.SessionData.PlayerLapTimeSessionBest : -1;
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
                OpponentData opponentPrevious = getOpponentDataForVehicleInfo(vehicle, previousGameState, currentGameState.SessionData.SessionRunningTime);
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
                opponent.SessionTimeAtLastPositionChange = opponentPrevious != null && opponentPrevious.Position != opponent.Position ? currentGameState.SessionData.SessionRunningTime : -1;
                opponent.CompletedLaps = vehicle.mTotalLaps;
                opponent.CurrentSectorNumber = vehicle.mSector == 0 ? 3 : vehicle.mSector;
                Boolean isNewSector = currentGameState.SessionData.IsNewSession || (opponentPrevious != null && opponentPrevious.CurrentSectorNumber != opponent.CurrentSectorNumber);
                opponent.IsNewLap = currentGameState.SessionData.IsNewSession || (isNewSector && opponent.CurrentSectorNumber == 1);
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
                        opponent.CompleteLapWithProvidedLapTime(opponent.Position, (float)vehicle.mLapStartET, lastSectorTime, true, false, (float)shared.mTrackTemp, (float)shared.mAmbientTemp, currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining);
                    }
                    opponent.StartNewLap(opponent.CompletedLaps + 1, opponent.Position, vehicle.mInPits == 1 || opponent.DistanceRoundTrack < 0, (float)vehicle.mLapStartET, false, (float)shared.mTrackTemp, (float)shared.mAmbientTemp);
                }
                else if (isNewSector && lastSectorTime > 0)
                {
                    opponent.AddCumulativeSectorData(opponent.Position, lastSectorTime, (float)vehicle.mLapStartET + lastSectorTime, true, false, (float)shared.mTrackTemp, (float)shared.mAmbientTemp);
                }
                if (vehicle.mInPits == 1 && opponent.CurrentSectorNumber == 3 && opponentPrevious != null && !opponentPrevious.isEnteringPits())
                {
                    opponent.setInLap();
                    LapData currentLapData = opponent.getCurrentLapData();
                    int sector3Position = currentLapData != null && currentLapData.SectorPositions.Count > 2 ? currentLapData.SectorPositions[2] : opponent.Position;
                    if (sector3Position == 1)
                    {
                        currentGameState.PitData.LeaderIsPitting = true;
                        currentGameState.PitData.OpponentForLeaderPitting = opponent;
                    }
                    if (sector3Position == currentGameState.SessionData.Position - 1 && currentGameState.SessionData.Position > 2)
                    {
                        currentGameState.PitData.CarInFrontIsPitting = true;
                        currentGameState.PitData.OpponentForCarAheadPitting = opponent;
                    }
                    if (sector3Position == currentGameState.SessionData.Position + 1 && !currentGameState.isLast())
                    {
                        currentGameState.PitData.CarBehindIsPitting = true;
                        currentGameState.PitData.OpponentForCarBehindPitting = opponent;
                    }
                }
                if (opponentPrevious != null && opponentPrevious.Position > 1 && opponent.Position == 1)
                {
                    currentGameState.SessionData.HasLeadChanged = true;
                }
                // session best lap times
                if (opponent.Position == currentGameState.SessionData.Position + 1)
                {
                    currentGameState.SessionData.TimeDeltaBehind = (float)vehicle.mTimeBehindNext;
                }
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestOverall || 
                    currentGameState.SessionData.OpponentsLapTimeSessionBestOverall < 0))
                {
                    currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = opponent.CurrentBestLapTime;
                }
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < currentGameState.SessionData.OverallSessionBestLapTime ||
                    currentGameState.SessionData.OverallSessionBestLapTime < 0))
                {
                    currentGameState.SessionData.OverallSessionBestLapTime = opponent.CurrentBestLapTime;
                }
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass ||
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass < 0) && 
                    opponent.CarClass == currentGameState.carClass)
                {
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;
                }
                // shouldn't have duplicates, but just in case
                if (!currentGameState.OpponentData.ContainsKey(opponentKey))
                {
                    currentGameState.OpponentData.Add(opponentKey, opponent);
                }
            }
            if (previousGameState != null)
            {
                currentGameState.SessionData.HasLeadChanged = !currentGameState.SessionData.HasLeadChanged && previousGameState.SessionData.Position > 1 && currentGameState.SessionData.Position == 1 ?
                    true : currentGameState.SessionData.HasLeadChanged;
                String oPrevKey = null;
                String oCurrKey = null;
                OpponentData oPrev = null;
                OpponentData oCurr = null;
                oPrevKey = (String)previousGameState.getOpponentKeyInFrontOnTrack();
                oCurrKey = (String)currentGameState.getOpponentKeyInFrontOnTrack();
                oPrev = oPrevKey != null ? previousGameState.OpponentData[oPrevKey] : null;
                oCurr = oCurrKey != null ? currentGameState.OpponentData[oCurrKey] : null;
                currentGameState.SessionData.IsRacingSameCarInFront = !((oPrev == null && oCurr != null) || (oPrev != null && oCurr == null) || (oPrev != null && oCurr != null && oPrev.DriverRawName != oCurr.DriverRawName));
                oPrevKey = (String)previousGameState.getOpponentKeyBehindOnTrack();
                oCurrKey = (String)currentGameState.getOpponentKeyBehindOnTrack();
                oPrev = oPrevKey != null ? previousGameState.OpponentData[oPrevKey] : null;
                oCurr = oCurrKey != null ? currentGameState.OpponentData[oCurrKey] : null;
                currentGameState.SessionData.IsRacingSameCarBehind = !((oPrev == null && oCurr != null) || (oPrev != null && oCurr == null) || (oPrev != null && oCurr != null && oPrev.DriverRawName != oCurr.DriverRawName));
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = !currentGameState.SessionData.IsRacingSameCarInFront ? 
                    currentGameState.SessionData.SessionRunningTime : previousGameState.SessionData.GameTimeAtLastPositionFrontChange;
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = !currentGameState.SessionData.IsRacingSameCarBehind ? 
                    currentGameState.SessionData.SessionRunningTime : previousGameState.SessionData.GameTimeAtLastPositionBehindChange;
            }

            // --------------------------------
            // pit data
            currentGameState.PitData.IsRefuellingAllowed = true;
            currentGameState.PitData.HasMandatoryPitStop = isOfflineSession && shared.mScheduledStops > 0 && player.mNumPitstops < shared.mScheduledStops && currentGameState.SessionData.SessionType == SessionType.Race;
            currentGameState.PitData.PitWindowStart = isOfflineSession && currentGameState.PitData.HasMandatoryPitStop ? 1 : 0;
            currentGameState.PitData.PitWindowEnd = !currentGameState.PitData.HasMandatoryPitStop ? 0 :
                currentGameState.SessionData.SessionHasFixedTime ? (int)(currentGameState.SessionData.SessionTotalRunTime / 60 / (shared.mScheduledStops + 1)) * (player.mNumPitstops + 1) + 1 :
                (int)(currentGameState.SessionData.SessionNumberOfLaps / (shared.mScheduledStops + 1)) * (player.mNumPitstops + 1) + 1;
            currentGameState.PitData.InPitlane = player.mInPits == 1;
            currentGameState.PitData.IsAtPitExit = previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane;
            currentGameState.PitData.OnOutLap = currentGameState.PitData.InPitlane && currentGameState.SessionData.SectorNumber == 1;
            currentGameState.PitData.OnInLap = currentGameState.PitData.InPitlane && currentGameState.SessionData.SectorNumber == 3;
            currentGameState.PitData.IsMakingMandatoryPitStop = currentGameState.PitData.HasMandatoryPitStop && currentGameState.PitData.OnInLap && currentGameState.SessionData.CompletedLaps > currentGameState.PitData.PitWindowStart;
            currentGameState.PitData.PitWindow = currentGameState.PitData.IsMakingMandatoryPitStop ? PitWindow.StopInProgress : mapToPitWindow((rFactor2Constants.rF2YellowFlagState)shared.mYellowFlagState);

            // --------------------------------
            // fuel data
            // don't read fuel data until race session is green
            // don't read fuel data for non-race session until out of pit lane and more than one lap completed
            if ((currentGameState.SessionData.SessionType == SessionType.Race &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Finished ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Checkered)) ||
                (!currentGameState.PitData.InPitlane && currentGameState.SessionData.CompletedLaps > 1))
            {
                currentGameState.FuelData.FuelUseActive = true;
                currentGameState.FuelData.FuelLeft = (float)shared.mFuel;
            }

            // --------------------------------
            // flags data
            FlagEnum Flag = FlagEnum.UNKNOWN;
            if (currentGameState.SessionData.IsDisqualified && previousGameState != null && !previousGameState.SessionData.IsDisqualified)
            {
                Flag = FlagEnum.BLACK;
            }
            else if (shared.mSectorFlag[player.mSector] > (int)rFactor2Constants.rF2YellowFlagState.NoFlag)
            {
                Flag = FlagEnum.YELLOW;
            }
            else if (currentGameState.SessionData.SessionType == SessionType.Race ||
                currentGameState.SessionData.SessionType == SessionType.Qualify)
            {
                if (shared.mGamePhase == (int)rFactor2Constants.rF2GamePhase.FullCourseYellow)
                {
                    // TODO:Revisit
                    Flag = FlagEnum.DOUBLE_YELLOW;
                }
                else if (shared.mYellowFlagState == (int)rFactor2Constants.rF2YellowFlagState.LastLap || currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    Flag = FlagEnum.WHITE;
                }
                else if (shared.mGamePhase == (int)rFactor2Constants.rF2YellowFlagState.NoFlag && previousGameState != null && previousGameState.SessionData.Flag == FlagEnum.DOUBLE_YELLOW)
                {
                    Flag = FlagEnum.GREEN;
                }
            }
            foreach (OpponentData opponent in currentGameState.OpponentData.Values)
            {
                if (currentGameState.SessionData.SessionType != SessionType.Race || 
                    currentGameState.SessionData.CompletedLaps < 1 || 
                    currentGameState.PositionAndMotionData.DistanceRoundTrack < 0)
                {
                    break;
                }
                if (opponent.getCurrentLapData().InLap || 
                    opponent.getCurrentLapData().OutLap || 
                    opponent.Position > currentGameState.SessionData.Position)
                {
                    continue;
                }
                if (isBehindWithinDistance(currentGameState.SessionData.TrackDefinition.trackLength, 8, 40, 
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, opponent.DistanceRoundTrack) && 
                    opponent.Speed >= currentGameState.PositionAndMotionData.CarSpeed)
                {
                    Flag = FlagEnum.BLUE;
                    break;
                }
            }
            currentGameState.SessionData.Flag = Flag;

            // --------------------------------
            // penalties data
            currentGameState.PenaltiesData.NumPenalties = player.mNumPenalties;
            float lateralDistDiff = (float)(Math.Abs(player.mPathLateral) - Math.Abs(player.mTrackEdge));
            currentGameState.PenaltiesData.IsOffRacingSurface = !currentGameState.PitData.InPitlane && lateralDistDiff >= 2;
            float offTrackDistanceDelta = lateralDistDiff - distanceOffTrack;
            distanceOffTrack = currentGameState.PenaltiesData.IsOffRacingSurface ? lateralDistDiff : 0;
            isApproachingTrack = offTrackDistanceDelta < 0 && currentGameState.PenaltiesData.IsOffRacingSurface && lateralDistDiff < 3;
            // primitive cut track detection for Reiza Time Trial Mode
            if (currentGameState.SessionData.SessionType == SessionType.HotLap)
            {
                if ((previousGameState != null && !previousGameState.SessionData.CurrentLapIsValid &&
                    previousGameState.SessionData.CompletedLaps == currentGameState.SessionData.CompletedLaps) || 
                    currentGameState.PenaltiesData.IsOffRacingSurface)
                {
                    currentGameState.SessionData.CurrentLapIsValid = false;
                }
            }
            if ((((currentGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.LastSector1Time < 0) || 
                (currentGameState.SessionData.SectorNumber == 3 && currentGameState.SessionData.LastSector2Time < 0)) && 
                !currentGameState.PitData.OnOutLap && !currentGameState.PitData.OnInLap &&
                (currentGameState.SessionData.SessionType == SessionType.Race || currentGameState.SessionData.SessionType == SessionType.Qualify)) || 
                (previousGameState != null && previousGameState.SessionData.CompletedLaps == currentGameState.SessionData.CompletedLaps && 
                !previousGameState.SessionData.CurrentLapIsValid))
            {
                currentGameState.SessionData.CurrentLapIsValid = false;
            }

            // --------------------------------
            // console output
            if (currentGameState.SessionData.IsNewSession)
            {
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
                Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
                Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                Console.WriteLine("PitWindowStart " + currentGameState.PitData.PitWindowStart);
                Console.WriteLine("PitWindowEnd " + currentGameState.PitData.PitWindowEnd);
                Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsAtStartOfSession);
                Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                Console.WriteLine("SessionStartPosition " + currentGameState.SessionData.SessionStartPosition);
                Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                Console.WriteLine("TrackName " + currentGameState.SessionData.TrackDefinition.name);
                Console.WriteLine("Player is using car class " + currentGameState.carClass.rF1ClassName + 
                    " at position " + currentGameState.SessionData.Position.ToString());
            }
            if (previousGameState != null && previousGameState.SessionData.SessionPhase != currentGameState.SessionData.SessionPhase)
            {
                Console.WriteLine("SessionPhase changed from " + previousGameState.SessionData.SessionPhase + 
                    " to " + currentGameState.SessionData.SessionPhase);
                if (currentGameState.SessionData.SessionPhase == SessionPhase.Checkered || 
                    currentGameState.SessionData.SessionPhase == SessionPhase.Finished)
                {
                    Console.WriteLine("Checkered - completed " + currentGameState.SessionData.CompletedLaps + 
                        " laps, session running time = " + currentGameState.SessionData.SessionRunningTime);
                }
            }
            if (previousGameState != null && !previousGameState.SessionData.LeaderHasFinishedRace && currentGameState.SessionData.LeaderHasFinishedRace)
            {
                Console.WriteLine("Leader has finished race, player has done " + currentGameState.SessionData.CompletedLaps + 
                    " laps, session time = " + currentGameState.SessionData.SessionRunningTime);
            }

            return currentGameState;
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
            float timeDelta = previousGameState != null ? sessionRunningTime - previousGameState.SessionData.SessionRunningTime : -1;
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
