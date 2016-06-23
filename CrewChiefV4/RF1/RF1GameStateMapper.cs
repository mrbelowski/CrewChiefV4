using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.rFactor1.rFactor1Data;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV4.rFactor1
{
    public class RF1GameStateMapper : GameStateMapper
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

        public RF1GameStateMapper()
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
            CrewChiefV4.rFactor1.RF1SharedMemoryReader.RF1StructWrapper wrapper = (CrewChiefV4.rFactor1.RF1SharedMemoryReader.RF1StructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            rFactor1Data.rfShared shared = wrapper.data;

            // no session data
            if (shared.numVehicles == 0)
            {
                return null;
            }
            // game is paused or other window has take focus
            if (shared.inRealtime == 0 || shared.deltaTime >= 0.56)
            {
                return previousGameState;
            }

            // --------------------------------
            // session data
            // get player scoring info (usually index 0)
            // get session leader scoring info (usually index 1 if not player)
            rFactor1Data.rfVehicleInfo player = new rfVehicleInfo();
            rFactor1Data.rfVehicleInfo leader = new rfVehicleInfo();
            for (int i = 0; i < shared.vehicle.Length; i++)
            {
                rFactor1Data.rfVehicleInfo vehicle = shared.vehicle[i];
                switch (mapToControlType((rFactor1Constant.rfControl)vehicle.control))
                { 
                    case ControlType.AI:
                    case ControlType.Player:
                    case ControlType.Remote:
                        if (vehicle.isPlayer == 1)
                        {
                            player = vehicle;
                        }
                        if (vehicle.place == 1)
                        {
                            leader = vehicle;
                        }
                        break;
                    default:
                        continue;
                }
                if (player.isPlayer == 1 && leader.place == 1)
                {
                    break;
                }
            }
            // can't find the player or session leader vehicle info (replay)
            if (player.isPlayer != 1 || leader.place != 1)
            {
                return previousGameState;
            }
            if (playerName == null)
            {
                String driverName = getNameFromBytes(player.driverName).ToLower();
                NameValidator.validateName(driverName);
                playerName = driverName;
            }
            // these things should remain constant during a session
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            currentGameState.carClass = CarData.getCarClassForRF1ClassName(getNameFromBytes(player.vehicleClass));
            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
            currentGameState.SessionData.DriverRawName = getNameFromBytes(player.driverName).ToLower();
            currentGameState.SessionData.TrackDefinition = new TrackDefinition(getNameFromBytes(shared.trackName), shared.lapDist);
            currentGameState.SessionData.TrackDefinition.setGapPoints();
            currentGameState.SessionData.SessionNumberOfLaps = shared.maxLaps > 0 && shared.maxLaps < 1000 ? shared.maxLaps : 0;
            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630;
            currentGameState.SessionData.SessionTotalRunTime = shared.endET > 0 ? shared.endET : currentGameState.SessionData.SessionNumberOfLaps > 0 ? 0 : defaultSessionTotalRunTime;
            // if previous state is null or any of the above change, this is a new session
            currentGameState.SessionData.IsNewSession = previousGameState == null ||
                currentGameState.SessionData.SessionType != previousGameState.SessionData.SessionType ||
                currentGameState.carClass.rF1ClassName != previousGameState.carClass.rF1ClassName ||
                currentGameState.SessionData.DriverRawName != previousGameState.SessionData.DriverRawName || 
                currentGameState.SessionData.TrackDefinition.name != previousGameState.SessionData.TrackDefinition.name ||
                currentGameState.SessionData.TrackDefinition.trackLength != previousGameState.SessionData.TrackDefinition.trackLength ||
                // these sometimes change in the beginning or end of session!
                //currentGameState.SessionData.SessionNumberOfLaps != previousGameState.SessionData.SessionNumberOfLaps ||
                //currentGameState.SessionData.SessionTotalRunTime != previousGameState.SessionData.SessionTotalRunTime || 
                ((previousGameState.SessionData.SessionPhase == SessionPhase.Checkered || 
                previousGameState.SessionData.SessionPhase == SessionPhase.Finished || 
                previousGameState.SessionData.SessionPhase == SessionPhase.Green) && 
                (currentGameState.SessionData.SessionPhase == SessionPhase.Garage || 
                currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Countdown)); 
            currentGameState.SessionData.SessionPhase = mapToSessionPhase((rFactor1Constant.rfGamePhase)shared.gamePhase);
            currentGameState.SessionData.SessionStartTime = currentGameState.SessionData.IsNewSession ? currentGameState.Now : previousGameState.SessionData.SessionStartTime;
            currentGameState.SessionData.SessionHasFixedTime = currentGameState.SessionData.SessionTotalRunTime > 0;
            currentGameState.SessionData.SessionRunningTime = shared.currentET;
            currentGameState.SessionData.SessionTimeRemaining = currentGameState.SessionData.SessionHasFixedTime ? currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionRunningTime : 0;
            // hack for test day sessions running longer than allotted time
            currentGameState.SessionData.SessionTimeRemaining = currentGameState.SessionData.SessionTimeRemaining < 0 && shared.session == 0 ? defaultSessionTotalRunTime : currentGameState.SessionData.SessionTimeRemaining;
            currentGameState.SessionData.NumCars = shared.numVehicles;
            currentGameState.SessionData.NumCarsAtStartOfSession = currentGameState.SessionData.IsNewSession ? currentGameState.SessionData.NumCars : previousGameState.SessionData.NumCarsAtStartOfSession;
            currentGameState.SessionData.Position = player.place;
            currentGameState.SessionData.UnFilteredPosition = currentGameState.SessionData.Position;
            currentGameState.SessionData.SessionStartPosition = currentGameState.SessionData.IsNewSession ? currentGameState.SessionData.Position : previousGameState.SessionData.SessionStartPosition;
            currentGameState.SessionData.SectorNumber = player.sector == 0 ? 3 : player.sector;
            currentGameState.SessionData.IsNewSector = currentGameState.SessionData.IsNewSession || currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;
            currentGameState.SessionData.IsNewLap = currentGameState.SessionData.IsNewSession || (currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == 1);
            currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.IsNewLap ? currentGameState.SessionData.Position : previousGameState.SessionData.PositionAtStartOfCurrentLap;
            currentGameState.SessionData.IsDisqualified = (rFactor1Constant.rfFinishStatus)player.finishStatus == rFactor1Constant.rfFinishStatus.dq;
            currentGameState.SessionData.CompletedLaps = player.totalLaps;
            currentGameState.SessionData.LapTimeCurrent = currentGameState.SessionData.SessionRunningTime - player.lapStartET;
            currentGameState.SessionData.LapTimePrevious = player.lastLapTime > 0 ? player.lastLapTime : -1;
            currentGameState.SessionData.LastSector1Time = player.lastSector1 > 0 ? player.lastSector1 : -1;
            currentGameState.SessionData.LastSector2Time = player.lastSector2 > 0 && player.lastSector1 > 0 ? player.lastSector2 - player.lastSector1 : -1;
            currentGameState.SessionData.LastSector3Time = player.lastLapTime > 0 && player.lastSector2 > 0 ? player.lastLapTime - player.lastSector2 : -1;
            currentGameState.SessionData.PlayerBestSector1Time = player.bestSector1 > 0 ? player.bestSector1 : -1;
            currentGameState.SessionData.PlayerBestSector2Time = player.bestSector2 > 0 && player.bestSector1 > 0 ? player.bestSector2 - player.bestSector1 : -1;
            currentGameState.SessionData.PlayerBestSector3Time = player.bestLapTime > 0 && player.bestSector2 > 0 ? player.bestLapTime - player.bestSector2 : -1;
            currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.PlayerBestSector1Time;
            currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.PlayerBestSector2Time;
            currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.PlayerBestSector3Time;
            currentGameState.SessionData.PlayerLapTimeSessionBest = player.bestLapTime > 0 ? player.bestLapTime : -1;
            currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState != null ? previousGameState.SessionData.SessionTimesAtEndOfSectors : new SessionData().SessionTimesAtEndOfSectors;
            if (currentGameState.SessionData.IsNewSector && !currentGameState.SessionData.IsNewSession)
            {
                // there's a slight delay due to scoring updating every 500 ms, so we can't use SessionRunningTime here
                switch(currentGameState.SessionData.SectorNumber)
                {
                    case 1:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = player.lapStartET > 0 ? player.lapStartET : -1;
                        break;
                    case 2:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = player.lapStartET + player.lastSector1 > 0 ? player.lapStartET + player.lastSector1 : -1;
                        break;
                    case 3:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = player.lapStartET + player.lastSector2 > 0 ? player.lapStartET + player.lastSector2 : -1;
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
            currentGameState.SessionData.LeaderHasFinishedRace = leader.finishStatus == (int)rFactor1Constant.rfFinishStatus.finished;
            currentGameState.SessionData.TimeDeltaFront = player.timeBehindNext;

            // --------------------------------
            // engine data
            currentGameState.EngineData.EngineRpm = shared.engineRPM;
            currentGameState.EngineData.MaxEngineRpm = shared.engineMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            currentGameState.EngineData.EngineOilTemp = shared.engineOilTemp;
            currentGameState.EngineData.EngineWaterTemp = shared.engineWaterTemp;
            //HACK: there's probably a cleaner way to do this...
            if (shared.overheating == 1)
            {
                currentGameState.EngineData.EngineWaterTemp += 50;
                currentGameState.EngineData.EngineOilTemp += 50;
            }

            // --------------------------------
            // transmission data
            currentGameState.TransmissionData.Gear = shared.gear;

            // --------------------------------
            // fuel data
            currentGameState.FuelData.FuelUseActive = true;
            currentGameState.FuelData.FuelLeft = shared.fuel;

            // --------------------------------
            // damage
            // not 100% certain on this mapping but it should be reasonably close
            currentGameState.CarDamageData.DamageEnabled = true;
            int bodyDamage = 0;
            int engineDamage = 0;
            int transmissionDamage = 0;
            for (int i = 0; i < shared.dentSeverity.Length; i++)
            {
                int dent = shared.dentSeverity[i];
                switch (i)
                {
                    case 3:
                        transmissionDamage = dent;
                        break;
                    case 4:
                        engineDamage = dent;
                        break;
                    default:
                        bodyDamage += dent;
                        break;
                }
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
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MINOR;
                    break;
                case 3:
                case 4:
                case 5:
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MAJOR;
                    break;
                default:
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.DESTROYED;
                    break;
            }
            switch (engineDamage)
            {
                // there is no "TRIVIAL" engine damage as even at the first level there's a chance of the engine seizing
                case 1:
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.MAJOR;
                    break;
                case 2:
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.DESTROYED;
                    break;
                default:
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.NONE;
                    break;
            }
            switch (transmissionDamage)
            {
                // it seems that even at the first level the transmission is already toast
                case 1:
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.MAJOR;
                    break;
                case 2:
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.DESTROYED;
                    break;
                default:
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.NONE;
                    break;
            }

            // --------------------------------
            // control data
            currentGameState.ControlData.ControlType = mapToControlType((rFactor1Constant.rfControl)player.control);

            // --------------------------------
            // motion data
            currentGameState.PositionAndMotionData.CarSpeed = shared.speed;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = player.lapDist;

            // --------------------------------
            // tire data
            currentGameState.TyreData.TireWearActive = true;
            currentGameState.TyreData.LeftFrontAttached = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].detached == 0;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[0];
            currentGameState.TyreData.FrontLeft_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[1];
            currentGameState.TyreData.FrontLeft_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[2];
            //expected Celsius but Automobilista reports in Kelvin
            if (currentGameState.TyreData.FrontLeft_LeftTemp > 273)
            {
                currentGameState.TyreData.FrontLeft_LeftTemp -= 273;
            }
            if (currentGameState.TyreData.FrontLeft_CenterTemp > 273)
            {
                currentGameState.TyreData.FrontLeft_CenterTemp -= 273;
            }
            if (currentGameState.TyreData.FrontLeft_RightTemp > 273)
            {
                currentGameState.TyreData.FrontLeft_RightTemp -= 273;
            }
            float frontLeftTemp = (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3;
            currentGameState.TyreData.FrontLeftPressure = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].pressure;
            currentGameState.TyreData.FrontLeftPercentWear = (1 - shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].wear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }
            else if (previousGameState == null || frontLeftTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }

            currentGameState.TyreData.RightFrontAttached = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].detached == 0;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].temperature[0];
            currentGameState.TyreData.FrontRight_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].temperature[1];
            currentGameState.TyreData.FrontRight_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].temperature[2];
            //expected Celsius but Automobilista reports in Kelvin
            if (currentGameState.TyreData.FrontRight_LeftTemp > 273)
            {
                currentGameState.TyreData.FrontRight_LeftTemp -= 273;
            }
            if (currentGameState.TyreData.FrontRight_CenterTemp > 273)
            {
                currentGameState.TyreData.FrontRight_CenterTemp -= 273;
            }
            if (currentGameState.TyreData.FrontRight_RightTemp > 273)
            {
                currentGameState.TyreData.FrontRight_RightTemp -= 273;
            }
            float frontRightTemp = (currentGameState.TyreData.FrontRight_CenterTemp + currentGameState.TyreData.FrontRight_LeftTemp + currentGameState.TyreData.FrontRight_RightTemp) / 3;
            currentGameState.TyreData.FrontRightPressure = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].pressure;
            currentGameState.TyreData.FrontRightPercentWear = (1 - shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].wear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }
            else if (previousGameState == null || frontRightTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }

            currentGameState.TyreData.LeftRearAttached = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].detached == 0;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].temperature[0];
            currentGameState.TyreData.RearLeft_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].temperature[1];
            currentGameState.TyreData.RearLeft_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].temperature[2];
            //expected Celsius but Automobilista reports in Kelvin
            if (currentGameState.TyreData.RearLeft_LeftTemp > 273)
            {
                currentGameState.TyreData.RearLeft_LeftTemp -= 273;
            }
            if (currentGameState.TyreData.RearLeft_CenterTemp > 273)
            {
                currentGameState.TyreData.RearLeft_CenterTemp -= 273;
            }
            if (currentGameState.TyreData.RearLeft_RightTemp > 273)
            {
                currentGameState.TyreData.RearLeft_RightTemp -= 273;
            }
            float rearLeftTemp = (currentGameState.TyreData.RearLeft_CenterTemp + currentGameState.TyreData.RearLeft_LeftTemp + currentGameState.TyreData.RearLeft_RightTemp) / 3;
            currentGameState.TyreData.RearLeftPressure = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].pressure;
            currentGameState.TyreData.RearLeftPercentWear = (1 - shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].wear) * 100;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }
            else if (previousGameState == null || rearLeftTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }

            currentGameState.TyreData.RightRearAttached = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].detached == 0;
            currentGameState.TyreData.RearRight_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].temperature[0];
            currentGameState.TyreData.RearRight_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].temperature[1];
            currentGameState.TyreData.RearRight_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].temperature[2];
            //expected Celsius but Automobilista reports in Kelvin
            if (currentGameState.TyreData.RearRight_LeftTemp > 273)
            {
                currentGameState.TyreData.RearRight_LeftTemp -= 273;
            }
            if (currentGameState.TyreData.RearRight_CenterTemp > 273)
            {
                currentGameState.TyreData.RearRight_CenterTemp -= 273;
            }
            if (currentGameState.TyreData.RearRight_RightTemp > 273)
            {
                currentGameState.TyreData.RearRight_RightTemp -= 273;
            }
            float rearRightTemp = (currentGameState.TyreData.RearRight_CenterTemp + currentGameState.TyreData.RearRight_LeftTemp + currentGameState.TyreData.RearRight_RightTemp) / 3;
            currentGameState.TyreData.RearRightPressure = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].pressure;
            currentGameState.TyreData.RearRightPercentWear = (1 - shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].wear) * 100;
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

            if (brakeTempThresholdsForPlayersCar != null)
            {
                currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar, shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].brakeTemp,
                    shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].brakeTemp, shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].brakeTemp, shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].brakeTemp);
            }

            // --------------------------------
            // brake data
            currentGameState.TyreData.LeftFrontBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].brakeTemp;
            currentGameState.TyreData.RightFrontBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].brakeTemp;
            currentGameState.TyreData.LeftRearBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].brakeTemp;
            currentGameState.TyreData.RightRearBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].brakeTemp;
            // some simple locking / spinning checks
            if (currentGameState.PositionAndMotionData.CarSpeed > 7)
            {
                float minRotatingSpeed = 2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].rotation) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].rotation) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].rotation) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].rotation) < minRotatingSpeed;

                float maxRotatingSpeed = 2 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].rotation) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].rotation) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].rotation) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].rotation) > maxRotatingSpeed;
            }
            currentGameState.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(suspensionDamageThresholds,
                !currentGameState.TyreData.LeftFrontAttached ? 1 : 0,
                !currentGameState.TyreData.RightFrontAttached ? 1 : 0,
                !currentGameState.TyreData.LeftRearAttached ? 1 : 0,
                !currentGameState.TyreData.RightRearAttached ? 1 : 0);

            // --------------------------------
            // track conditions
            if (currentGameState.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < currentGameState.Now)
            {
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.ambientTemp, shared.trackTemp, 0, (float)Math.Sqrt((double)(shared.wind.x * shared.wind.x + shared.wind.y * shared.wind.y + shared.wind.z * shared.wind.z)), 0, 0, 0);
            }

            // --------------------------------
            // opponent data
            isOfflineSession = true;
            opponentKeysProcessed.Clear();
            for (int i = 0; i < shared.vehicle.Length; i++)
            {
                rFactor1Data.rfVehicleInfo vehicle = shared.vehicle[i];
                if (vehicle.isPlayer == 1)
                {
                    currentGameState.SessionData.OverallSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest > 0 ?
                        currentGameState.SessionData.PlayerLapTimeSessionBest : -1;
                    currentGameState.SessionData.PlayerClassSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest > 0 ?
                        currentGameState.SessionData.PlayerLapTimeSessionBest : -1;
                    continue;
                }
                switch (mapToControlType((rFactor1Constant.rfControl)vehicle.control))
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
                String opponentKey = getNameFromBytes(vehicle.vehicleClass) + vehicle.place.ToString();
                OpponentData opponentPrevious = getOpponentDataForVehicleInfo(vehicle, previousGameState, currentGameState.SessionData.SessionRunningTime);
                OpponentData opponent = new OpponentData();
                opponent.DriverRawName = getNameFromBytes(vehicle.driverName).ToLower();
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.CarClass = CarData.getCarClassForRF1ClassName(getNameFromBytes(vehicle.vehicleClass));
                opponent.Position = vehicle.place;
                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName + 
                        " is using car class " + opponent.CarClass.carClassEnum + 
                        " (class ID " + opponent.CarClass.rF1ClassName + ")" + 
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
                opponent.CompletedLaps = vehicle.totalLaps;
                opponent.CurrentSectorNumber = vehicle.sector == 0 ? 3 : vehicle.sector;
                Boolean isNewSector = currentGameState.SessionData.IsNewSession || (opponentPrevious != null && opponentPrevious.CurrentSectorNumber != opponent.CurrentSectorNumber);
                opponent.IsNewLap = currentGameState.SessionData.IsNewSession || (isNewSector && opponent.CurrentSectorNumber == 1);
                opponent.Speed = vehicle.speed;
                opponent.DistanceRoundTrack = vehicle.lapDist;
                opponent.WorldPosition = new float[] { vehicle.pos.x, vehicle.pos.z };
                opponent.CurrentBestLapTime = vehicle.bestLapTime > 0 ? vehicle.bestLapTime : -1;
                opponent.PreviousBestLapTime = opponentPrevious != null && opponentPrevious.CurrentBestLapTime > 0 && 
                    opponentPrevious.CurrentBestLapTime > opponent.CurrentBestLapTime ? opponentPrevious.CurrentBestLapTime : -1;
                opponent.bestSector1Time = vehicle.bestSector1 > 0 ? vehicle.bestSector1 : -1;
                opponent.bestSector2Time = vehicle.bestSector2 > 0 && vehicle.bestSector1 > 0 ? vehicle.bestSector2 - vehicle.bestSector1 : -1;
                opponent.bestSector3Time = vehicle.bestLapTime > 0 && vehicle.bestSector2 > 0 ? vehicle.bestLapTime - vehicle.bestSector2 : -1;
                opponent.LastLapTime = vehicle.lastLapTime > 0 ? vehicle.lastLapTime : -1;
                float lastSectorTime = -1;
                switch (opponent.CurrentSectorNumber)
                {
                    case 1:
                        lastSectorTime = vehicle.lastLapTime > 0 ? vehicle.lastLapTime : -1;
                        break;
                    case 2:
                        lastSectorTime = vehicle.lastSector1 > 0 ? vehicle.lastSector1 : -1;
                        break;
                    case 3:
                        lastSectorTime = vehicle.lastSector2 > 0 ? vehicle.lastSector2 : -1;
                        break;
                    default:
                        break;
                }
                if (opponent.IsNewLap)
                {
                    if (lastSectorTime > 0)
                    {
                        opponent.CompleteLapWithProvidedLapTime(opponent.Position, vehicle.lapStartET, lastSectorTime, true, false, shared.trackTemp, shared.ambientTemp, currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining);
                    }
                    opponent.StartNewLap(opponent.CompletedLaps + 1, opponent.Position, vehicle.inPits == 1 || opponent.DistanceRoundTrack < 0, vehicle.lapStartET, false, shared.trackTemp, shared.ambientTemp);
                }
                else if (isNewSector && lastSectorTime > 0)
                {
                    opponent.AddCumulativeSectorData(opponent.Position, lastSectorTime, vehicle.lapStartET + lastSectorTime, true, false, shared.trackTemp, shared.ambientTemp);
                    if (vehicle.inPits == 1)
                    {
                        opponent.setInLap();
                        if (opponent.Position == 1)
                        {
                            currentGameState.PitData.LeaderIsPitting = true;
                            currentGameState.PitData.OpponentForLeaderPitting = opponent;
                        }
                        if (opponent.Position == currentGameState.SessionData.Position - 1)
                        {
                            currentGameState.PitData.CarInFrontIsPitting = true;
                            currentGameState.PitData.OpponentForCarAheadPitting = opponent;
                        }
                        if (opponent.Position == currentGameState.SessionData.Position + 1)
                        {
                            currentGameState.PitData.CarBehindIsPitting = true;
                            currentGameState.PitData.OpponentForCarBehindPitting = opponent;
                        }
                    }
                }
                if (opponentPrevious != null && opponentPrevious.Position > 1 && opponent.Position == 1)
                {
                    currentGameState.SessionData.HasLeadChanged = true;
                }
                // session best lap times
                if (opponent.Position == currentGameState.SessionData.Position + 1)
                {
                    currentGameState.SessionData.TimeDeltaBehind = vehicle.timeBehindNext;
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
                if (opponent.CurrentBestLapTime > 0 && (opponent.CurrentBestLapTime < currentGameState.SessionData.PlayerClassSessionBestLapTime ||
                    currentGameState.SessionData.PlayerClassSessionBestLapTime < 0) && 
                    opponent.CarClass.rF1ClassName == currentGameState.carClass.rF1ClassName)
                {
                    currentGameState.SessionData.PlayerClassSessionBestLapTime = opponent.CurrentBestLapTime;
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
                OpponentData oPrev = null;
                OpponentData oCurr = null;
                oPrev = previousGameState.getOpponentAtPosition(previousGameState.SessionData.Position - 1, true);
                oCurr = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, true);
                currentGameState.SessionData.IsRacingSameCarInFront = !((oPrev == null && oCurr != null) || (oPrev != null && oCurr == null) || (oPrev != null && oCurr != null && oPrev.DriverRawName != oCurr.DriverRawName));
                oPrev = previousGameState.getOpponentAtPosition(previousGameState.SessionData.Position + 1, true);
                oCurr = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1, true);
                currentGameState.SessionData.IsRacingSameCarBehind = !((oPrev == null && oCurr != null) || (oPrev != null && oCurr == null) || (oPrev != null && oCurr != null && oPrev.DriverRawName != oCurr.DriverRawName));
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = !currentGameState.SessionData.IsRacingSameCarInFront ? 
                    currentGameState.SessionData.SessionRunningTime : previousGameState.SessionData.GameTimeAtLastPositionFrontChange;
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = !currentGameState.SessionData.IsRacingSameCarBehind ? 
                    currentGameState.SessionData.SessionRunningTime : previousGameState.SessionData.GameTimeAtLastPositionBehindChange;
            }

            // --------------------------------
            // pit data
            currentGameState.PitData.IsRefuellingAllowed = true;
            currentGameState.PitData.HasMandatoryPitStop = shared.scheduledStops > 0 && player.numPitstops < shared.scheduledStops && currentGameState.SessionData.SessionType == SessionType.Race;
            currentGameState.PitData.PitWindowStart = currentGameState.PitData.HasMandatoryPitStop ? 1 : 0;
            currentGameState.PitData.PitWindowEnd = !currentGameState.PitData.HasMandatoryPitStop ? 0 :
                currentGameState.SessionData.SessionHasFixedTime ? (int)(currentGameState.SessionData.SessionTotalRunTime / 60 / (shared.scheduledStops + 1)) * (player.numPitstops + 1) + 1 :
                (int)(currentGameState.SessionData.SessionNumberOfLaps / (shared.scheduledStops + 1)) * (player.numPitstops + 1) + 1;
            currentGameState.PitData.InPitlane = player.inPits == 1;
            currentGameState.PitData.IsAtPitExit = previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane;
            currentGameState.PitData.OnOutLap = (previousGameState == null && currentGameState.PitData.InPitlane) ||
                (previousGameState != null && previousGameState.PitData.OnOutLap && currentGameState.SessionData.SectorNumber == 1);
            currentGameState.PitData.OnInLap = previousGameState != null && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane;
            currentGameState.PitData.IsMakingMandatoryPitStop = currentGameState.PitData.HasMandatoryPitStop && currentGameState.PitData.OnInLap && currentGameState.SessionData.CompletedLaps > 0;
            currentGameState.PitData.PitWindow = currentGameState.PitData.IsMakingMandatoryPitStop ? PitWindow.StopInProgress : mapToPitWindow((rFactor1Constant.rfYellowFlagState)shared.yellowFlagState);

            // --------------------------------
            // flags data
            FlagEnum Flag = FlagEnum.UNKNOWN;
            if (currentGameState.SessionData.IsDisqualified && previousGameState != null && !previousGameState.SessionData.IsDisqualified)
            {
                Flag = FlagEnum.BLACK;
            }
            else if (shared.sectorFlag[player.sector] > (int)rFactor1Constant.rfYellowFlagState.noFlag)
            {
                Flag = FlagEnum.YELLOW;
            }
            else if (shared.gamePhase == (int)rFactor1Constant.rfGamePhase.fullCourseYellow)
            {
                Flag = FlagEnum.DOUBLE_YELLOW;
            }
            else if (shared.yellowFlagState == (int)rFactor1Constant.rfYellowFlagState.lastLap || currentGameState.SessionData.LeaderHasFinishedRace)
            {
                Flag = FlagEnum.WHITE;
            }
            else if (shared.gamePhase == (int)rFactor1Constant.rfYellowFlagState.noFlag && previousGameState != null && previousGameState.SessionData.Flag == FlagEnum.DOUBLE_YELLOW)
            {
                Flag = FlagEnum.GREEN;
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
            currentGameState.PenaltiesData.NumPenalties = player.numPenalties;
            float lateralDistDiff = (float)(Math.Abs(player.pathLateral) - Math.Abs(player.trackEdge));
            currentGameState.PenaltiesData.IsOffRacingSurface = !currentGameState.PitData.InPitlane && lateralDistDiff >= 2;
            float offTrackDistanceDelta = lateralDistDiff - distanceOffTrack;
            distanceOffTrack = currentGameState.PenaltiesData.IsOffRacingSurface ? lateralDistDiff : 0;
            isApproachingTrack = offTrackDistanceDelta < 0 && currentGameState.PenaltiesData.IsOffRacingSurface && lateralDistDiff < 3;

            // --------------------------------
            // console output
            if (currentGameState.SessionData.IsNewSession)
            {
                Console.WriteLine("New session, trigger data:");
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
                Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum + 
                    " (class ID " + currentGameState.carClass.rF1ClassName + ")" + 
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
        
        private PitWindow mapToPitWindow(rFactor1Constant.rfYellowFlagState pitWindow)
        {
            // it seems that the pit window is only truly open on multiplayer races?
            if (isOfflineSession)
            {
                return PitWindow.Open;
            }
            switch (pitWindow)
            {
                case rFactor1Constant.rfYellowFlagState.pitClosed:
                    return PitWindow.Closed;
                case rFactor1Constant.rfYellowFlagState.pitOpen:
                case rFactor1Constant.rfYellowFlagState.pitLeadLap:
                    return PitWindow.Open;
                default:
                    return PitWindow.Unavailable;
            }
        }

        private SessionPhase mapToSessionPhase(rFactor1Constant.rfGamePhase sessionPhase)
        {
            switch (sessionPhase)
            {
                case rFactor1Constant.rfGamePhase.countdown:
                    return SessionPhase.Countdown;
                // warmUp never happens, but just in case
                case rFactor1Constant.rfGamePhase.warmUp:
                case rFactor1Constant.rfGamePhase.formation:
                    return SessionPhase.Formation;
                case rFactor1Constant.rfGamePhase.garage:
                    return SessionPhase.Garage;
                case rFactor1Constant.rfGamePhase.gridWalk:
                    return SessionPhase.Gridwalk;
                // sessions never go to sessionStopped, they always go straight from greenFlag to sessionOver
                case rFactor1Constant.rfGamePhase.sessionStopped:
                case rFactor1Constant.rfGamePhase.sessionOver:
                    return SessionPhase.Finished;
                // fullCourseYellow will count as greenFlag since we'll call it out in the Flags separately anyway
                case rFactor1Constant.rfGamePhase.fullCourseYellow:
                case rFactor1Constant.rfGamePhase.greenFlag:
                    return SessionPhase.Green;
                default:
                    return SessionPhase.Unavailable;
            }
        }

        // finds OpponentData for given vehicle based on driver name, vehicle class, and world position
        private OpponentData getOpponentDataForVehicleInfo(rfVehicleInfo vehicle, GameStateData previousGameState, float sessionRunningTime)
        {
            OpponentData opponentPrevious = null;
            float timeDelta = previousGameState != null ? sessionRunningTime - previousGameState.SessionData.SessionRunningTime : -1;
            if (previousGameState != null && timeDelta >= 0)
            {
                float[] worldPos = { vehicle.pos.x, vehicle.pos.z };
                float minDistDiff = -1;
                String opponentKey = null;
                foreach (OpponentData o in previousGameState.OpponentData.Values)
                {
                    opponentKey = o.CarClass.rF1ClassName + o.Position.ToString();
                    if (o.DriverRawName != getNameFromBytes(vehicle.driverName).ToLower() || 
                        o.CarClass.rF1ClassName != getNameFromBytes(vehicle.vehicleClass) || 
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
            rFactor1Data.rfShared shared = (rFactor1Data.rfShared)memoryMappedFileStruct;
            switch (shared.session)
            {
                // up to four possible practice sessions
                // test day and pre-race warm-up sessions are 'Practice' as well since 'HotLap' seems to suppress flag info
                case 1:
                case 2:
                case 3:
                case 4:
                case 0:
                case 9:
                    return SessionType.Practice;
                // up to four possible qualifying sessions
                case 5:
                case 6:
                case 7:
                case 8:
                    return SessionType.Qualify;
                // only one race session
                case 10:
                    return SessionType.Race;
                default:
                    return SessionType.Unavailable;
            }
        }

        private ControlType mapToControlType(rFactor1Constant.rfControl controlType)
        {
            switch (controlType)
            {
                case rFactor1Constant.rfControl.ai:
                    return ControlType.AI;
                case rFactor1Constant.rfControl.player:
                    return ControlType.Player;
                case rFactor1Constant.rfControl.remote:
                    return ControlType.Remote;
                case rFactor1Constant.rfControl.replay:
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