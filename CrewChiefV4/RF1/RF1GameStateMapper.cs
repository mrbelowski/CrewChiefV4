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

        private Boolean enablePitWindowHack = UserSettings.GetUserSettings().getBoolean("enable_ams_pit_schedule_messages");

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
                isOfflineSession = true;
                distanceOffTrack = 0;
                isApproachingTrack = false;
                wheelCircumference = new float[] { 0, 0 };
                previousGameState = null;
                return null;
            }
            // game is paused or other window has taken focus
            if (shared.deltaTime >= 0.56)
            {
                return previousGameState;
            }

            // --------------------------------
            // session data
            // get player scoring info (usually index 0)
            // get session leader scoring info (usually index 1 if not player)
            rFactor1Data.rfVehicleInfo player = new rfVehicleInfo();
            rFactor1Data.rfVehicleInfo leader = new rfVehicleInfo();            
            for (int i = 0; i < shared.numVehicles; i++)
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
            currentGameState.SessionData.EventIndex = shared.session;
            currentGameState.SessionData.SessionIteration = 
                shared.session >= 1 && shared.session <= 4 ? shared.session - 1 :
                shared.session >= 5 && shared.session <= 8 ? shared.session - 5 :
                shared.session >= 10 && shared.session <= 13 ? shared.session - 10 : 0;
            currentGameState.SessionData.SessionType = mapToSessionType(shared);

            Boolean startedNewLap = false;
            Boolean finishedLap = false;
            SessionPhase previousSessionPhase = SessionPhase.Unavailable;
            if (previousGameState != null) 
            {
                // player.sectorNumber might go to 0 at session-end
                finishedLap = previousGameState.SessionData.SectorNumber == 0 ||
                              (previousGameState.SessionData.SectorNumber == 3 && player.sector <= 1);
                startedNewLap = shared.lapNumber > previousGameState.SessionData.CompletedLaps;
                previousSessionPhase = previousGameState.SessionData.SessionPhase;
            }            
            Boolean isInPits = player.inPits == 1;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase((rFactor1Constant.rfGamePhase)shared.gamePhase,
                    previousSessionPhase, /*finishedLap ||*/ startedNewLap, isInPits);

            // --------------------------------
            // flags data
            currentGameState.FlagData.isFullCourseYellow = currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow
                || shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.resume;

            if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.resume)
            {
                // Special case for resume after FCY.  rF2 no longer has FCY set, but still has Resume sub phase set.
                currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.RACING;
            }
            else if (currentGameState.FlagData.isFullCourseYellow)
            {
                if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pending)
                    currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.PENDING;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pitClosed)
                    currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.PITS_CLOSED;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pitOpen)
                    currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.PITS_OPEN;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pitLeadLap)
                    currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.lastLap)
                {
                    if (previousGameState != null)
                    {
                        if (previousGameState.FlagData.fcyPhase != FullCourseYellowPhase.LAST_LAP_NEXT && previousGameState.FlagData.fcyPhase != FullCourseYellowPhase.LAST_LAP_CURRENT)
                            // Initial last lap phase
                            currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.LAST_LAP_NEXT;
                        else if (currentGameState.SessionData.CompletedLaps != previousGameState.SessionData.CompletedLaps && 
                            previousGameState.FlagData.fcyPhase == FullCourseYellowPhase.LAST_LAP_NEXT)
                            // Once we reach the end of current lap, and this lap is next last lap, switch to last lap current phase.
                            currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.LAST_LAP_CURRENT;
                        else
                            // Keep previous FCY last lap phase.
                            currentGameState.FlagData.fcyPhase = previousGameState.FlagData.fcyPhase;
                    }
                }
            }
            if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                //Console.WriteLine(shared.sectorFlag[0] + " : " + shared.sectorFlag[1] + " : " + shared.sectorFlag[2]);
                // Mark Yellow sectors.
                // RF1 uses 2 as the yellow sector indicator, but the plugin sends the sector *after* the sector
                // where in incident actually is
                if (shared.sectorFlag[0] == (sbyte)rFactor1Constant.rfYellowFlagState.pitClosed)
                {
                    currentGameState.FlagData.sectorFlags[2] = FlagEnum.YELLOW;
                }
                if (shared.sectorFlag[1] == (sbyte)rFactor1Constant.rfYellowFlagState.pitClosed)
                {
                    currentGameState.FlagData.sectorFlags[0] = FlagEnum.YELLOW;
                }
                if (shared.sectorFlag[2] == (sbyte)rFactor1Constant.rfYellowFlagState.pitClosed)
                {
                    currentGameState.FlagData.sectorFlags[1] = FlagEnum.YELLOW;
                }
            }

            currentGameState.carClass = getCarClass(getNameFromBytes(shared.vehicleName), true);
            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
            currentGameState.SessionData.DriverRawName = getNameFromBytes(player.driverName).ToLower();
            currentGameState.SessionData.TrackDefinition = new TrackDefinition(getNameFromBytes(shared.trackName), shared.lapDist);
            if (previousGameState == null || previousGameState.SessionData.TrackDefinition.name != currentGameState.SessionData.TrackDefinition.name)
            {
                // new game or new track
                currentGameState.SessionData.TrackDefinition.trackLandmarks = TrackData.TRACK_LANDMARKS_DATA.getTrackLandmarksForTrackName(currentGameState.SessionData.TrackDefinition.name);
                currentGameState.SessionData.TrackDefinition.setGapPoints();
            }
            else if (previousGameState != null)
            {
                // copy from previous gamestate
                currentGameState.SessionData.TrackDefinition.trackLandmarks = previousGameState.SessionData.TrackDefinition.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.gapPoints = previousGameState.SessionData.TrackDefinition.gapPoints;
            }
            
            currentGameState.SessionData.TrackDefinition.setGapPoints();
            currentGameState.SessionData.SessionNumberOfLaps = shared.maxLaps > 0 && shared.maxLaps < 1000 ? shared.maxLaps : 0;
            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630;
            currentGameState.SessionData.SessionTotalRunTime = shared.endET > 0 ? shared.endET : currentGameState.SessionData.SessionNumberOfLaps > 0 ? 0 : defaultSessionTotalRunTime;
            // if previous state is null or any of the above change, this is a new session
            currentGameState.SessionData.IsNewSession = previousGameState == null ||
                currentGameState.SessionData.SessionType != previousGameState.SessionData.SessionType ||
                currentGameState.carClass.getClassIdentifier() != previousGameState.carClass.getClassIdentifier() ||
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
                previousGameState.SessionData.SessionPhase == SessionPhase.Green ||
                previousGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow) && 
                (currentGameState.SessionData.SessionPhase == SessionPhase.Garage || 
                currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Countdown));

            // Do not use previous game state if this is the new session.
            if (currentGameState.SessionData.IsNewSession)
                previousGameState = null;

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
            currentGameState.SessionData.CompletedLaps = shared.lapNumber < 0 ? 0 : shared.lapNumber;
            currentGameState.SessionData.LapTimeCurrent = currentGameState.SessionData.SessionRunningTime - player.lapStartET;
            currentGameState.SessionData.LapTimePrevious = player.lastLapTime > 0 ? player.lastLapTime : -1;

            // Last (most current) per-sector times:
            // Note: this logic still misses invalid sector handling.
            var lastS1Time = player.lastSector1 > 0.0 ? player.lastSector1 : -1.0;
            var lastS2Time = player.lastSector1 > 0.0 && player.lastSector2 > 0.0
                ? player.lastSector2 - player.lastSector1 : -1.0;
            var lastS3Time = player.lastSector2 > 0.0 && player.lastLapTime > 0.0
                ? player.lastLapTime - player.lastSector2 : -1.0;

            currentGameState.SessionData.LastSector1Time = (float)lastS1Time;
            currentGameState.SessionData.LastSector2Time = (float)lastS2Time;
            currentGameState.SessionData.LastSector3Time = (float)lastS3Time;

            // Check if we have more current values for S1 and S2.
            // S3 always equals to lastS3Time.
            if (player.curSector1 > 0.0)
                currentGameState.SessionData.LastSector1Time = (float)player.curSector1;

            if (player.curSector1 > 0.0 && player.curSector2 > 0.0)
                currentGameState.SessionData.LastSector2Time = (float)(player.curSector2 - player.curSector1);

            if (previousGameState != null && !currentGameState.SessionData.IsNewSession)
            {
                // Preserve current timing values.
                // Those values change on sector/lap change, otherwise stay the same between updates.
                previousGameState.SessionData.restorePlayerTimings(currentGameState.SessionData);
            }
            float lastSectorTime = -1;
            switch (currentGameState.SessionData.SectorNumber)
            {
                case 1:
                    lastSectorTime = player.lastLapTime > 0 ? player.lastLapTime : -1;
                    break;
                case 2:
                    lastSectorTime = player.lastSector1 > 0 ? player.lastSector1 : -1;

                    if (player.curSector1 > 0.0)
                        lastSectorTime = (float)player.curSector1;

                    break;
                case 3:
                    lastSectorTime = player.lastSector2 > 0 ? player.lastSector2 : -1;

                    if (player.curSector2 > 0.0)
                        lastSectorTime = (float)player.curSector2;

                    break;
                default:
                    break;
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.Position, currentGameState.SessionData.SessionRunningTime,
                        lastSectorTime, lastSectorTime > 0, false, shared.trackTemp, shared.ambientTemp, currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining);
                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1, currentGameState.SessionData.Position, player.inPits == 1 || player.lapDist < 0, currentGameState.SessionData.SessionRunningTime, false, shared.trackTemp, shared.ambientTemp);
            }
            else if (currentGameState.SessionData.IsNewSector)
            {
                currentGameState.SessionData.playerAddCumulativeSectorData(currentGameState.SessionData.Position, lastSectorTime, currentGameState.SessionData.SessionRunningTime,
                    lastSectorTime > 0 || (currentGameState.SessionData.SectorNumber >= 2 && player.totalLaps == 1), false, shared.trackTemp, shared.ambientTemp);
            }
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
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = player.lapStartET > 0 && player.curSector1 > 0 ? player.lapStartET + player.curSector1 : -1;
                        break;
                    case 3:
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = player.lapStartET > 0 && player.curSector2 > 0 ? player.lapStartET + player.curSector2 : -1;
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
            currentGameState.SessionData.TimeDeltaFront = Math.Abs(player.timeBehindNext);

            // --------------------------------
            // engine data
            currentGameState.EngineData.EngineRpm = shared.engineRPM;
            currentGameState.EngineData.MaxEngineRpm = shared.engineMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            currentGameState.EngineData.EngineOilTemp = shared.engineOilTemp;
            currentGameState.EngineData.EngineWaterTemp = shared.engineWaterTemp;
            //HACK: there's probably a cleaner way to do this...            
            // JB: apparently CC is too sensitive to engine temperatures, so disable this for now.
            // if (shared.overheating == 1)
            // {
            //     currentGameState.EngineData.EngineWaterTemp += 50;
            //     currentGameState.EngineData.EngineOilTemp += 50;
            // }

            // --------------------------------
            // transmission data
            currentGameState.TransmissionData.Gear = shared.gear;

            // controls
            currentGameState.ControlData.BrakePedal = shared.unfilteredBrake;
            currentGameState.ControlData.ThrottlePedal = shared.unfilteredThrottle;
            currentGameState.ControlData.ClutchPedal = shared.unfilteredClutch;

            // --------------------------------
            // damage
            // not 100% certain on this mapping but it should be reasonably close
            if (currentGameState.SessionData.SessionType != SessionType.HotLap)
            {
                currentGameState.CarDamageData.DamageEnabled = true;
                int bodyDamage = 0;
                foreach (int dent in shared.dentSeverity)
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
            currentGameState.ControlData.ControlType = mapToControlType((rFactor1Constant.rfControl)player.control);

            // --------------------------------
            // motion data
            currentGameState.PositionAndMotionData.CarSpeed = shared.speed;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = player.lapDist;

            // --------------------------------
            // tire data
            // Automobilista reports in Kelvin
            currentGameState.TyreData.TireWearActive = true;
            currentGameState.TyreData.LeftFrontAttached = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].detached == 0;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[0] - 273;
            currentGameState.TyreData.FrontLeft_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[1] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[2] - 273;

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
            currentGameState.TyreData.FrontRight_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].temperature[0] - 273;
            currentGameState.TyreData.FrontRight_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].temperature[1] - 273;
            currentGameState.TyreData.FrontRight_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].temperature[2] - 273;

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
            currentGameState.TyreData.RearLeft_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].temperature[0] - 273;
            currentGameState.TyreData.RearLeft_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].temperature[1] - 273;
            currentGameState.TyreData.RearLeft_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].temperature[2] - 273;

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
            currentGameState.TyreData.RearRight_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].temperature[0] - 273;
            currentGameState.TyreData.RearRight_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].temperature[1] - 273;
            currentGameState.TyreData.RearRight_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].temperature[2] - 273;

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
            // some simple locking / spinning checks
            if (currentGameState.PositionAndMotionData.CarSpeed > 7.0f)
            {
                // TODO: fix this properly - decrease the minRotatingSpeed from 2*pi to pi just to hide the problem
                float minRotatingSpeed = (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.wheel[0].rotation) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.wheel[1].rotation) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.wheel[2].rotation) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.wheel[3].rotation) < minRotatingSpeed;

                // TODO: fix this properly - increase the maxRotatingSpeed from 2*pi to 3*pi just to hide the problem
                float maxRotatingSpeed = 3 * (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.wheel[0].rotation) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.wheel[1].rotation) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.wheel[2].rotation) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.wheel[3].rotation) > maxRotatingSpeed;
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
            currentGameState.TyreData.LeftFrontBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].brakeTemp - 273;
            currentGameState.TyreData.RightFrontBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].brakeTemp - 273;
            currentGameState.TyreData.LeftRearBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].brakeTemp - 273;
            currentGameState.TyreData.RightRearBrakeTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].brakeTemp - 273;

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
                    shared.ambientTemp, shared.trackTemp, 0, (float)Math.Sqrt((double)(shared.wind.x * shared.wind.x + shared.wind.y * shared.wind.y + shared.wind.z * shared.wind.z)), 0, 0, 0);
            }

            // --------------------------------
            // opponent data
            isOfflineSession = true;
            opponentKeysProcessed.Clear();

            // first check for duplicates and online session:
            Dictionary<string, int> driverNameCounts = new Dictionary<string, int>();
            Dictionary<string, int> duplicatesCreated = new Dictionary<string, int>();
            for (int i = 0; i < shared.numVehicles; ++i)
            {
                var vehicle = shared.vehicle[i];
                String driverName = getNameFromBytes(vehicle.driverName).ToLower();
                if (isOfflineSession && (rFactor1Constant.rfControl)vehicle.control == rFactor1Constant.rfControl.remote)
                {
                    isOfflineSession = false;
                }
                if (driverNameCounts.ContainsKey(driverName))
                {
                    driverNameCounts[driverName] += 1;
                }
                else
                {
                    driverNameCounts.Add(driverName, 1);
                }
            }

            for (int i = 0; i < shared.numVehicles; i++)
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
                String driverName = getNameFromBytes(vehicle.driverName).ToLower();
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
                        if (previousGameState != null && previousGameState.OpponentData.ContainsKey(driverName) &&
                            !currentGameState.OpponentData.ContainsKey(driverName))
                        {
                            currentGameState.OpponentData.Add(driverName, previousGameState.OpponentData[driverName]);
                        }
                        opponentKeysProcessed.Add(driverName);
                        continue;
                    }
                    else
                    {
                        // offline we can have any number of duplicates :(
                        opponentKey = getOpponentKeyForVehicleInfo(vehicle, previousGameState, currentGameState.SessionData.SessionRunningTime, driverName, duplicatesCount);
                        // there's no previous opponent data record for this driver so create one
                        if (duplicatesCreated.ContainsKey(driverName))
                        {
                            duplicatesCreated[driverName] += 1;
                        }
                        else
                        {
                            duplicatesCreated.Add(driverName, 1);
                        }
                        opponentKey = driverName + "_duplicate_" + duplicatesCreated[driverName];
                    }
                }
                else
                {
                    opponentKey = driverName;                    
                }
                opponentPrevious = previousGameState == null || opponentKey == null || !previousGameState.OpponentData.ContainsKey(opponentKey) ? null : previousGameState.OpponentData[opponentKey];
                OpponentData opponent = new OpponentData();
                opponent.DriverRawName = driverName;
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.CarClass = getCarClass(getNameFromBytes(vehicle.vehicleName), false);
                opponent.Position = vehicle.place;
                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName + 
                        " is using car class " + opponent.CarClass.getClassIdentifier() + 
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
                float previousDistanceRoundTrack = opponentPrevious != null ? opponentPrevious.DistanceRoundTrack : 0;
                opponent.bestSector1Time = vehicle.bestSector1 > 0 ? vehicle.bestSector1 : -1;
                opponent.bestSector2Time = vehicle.bestSector2 > 0 && vehicle.bestSector1 > 0 ? vehicle.bestSector2 - vehicle.bestSector1 : -1;
                opponent.bestSector3Time = vehicle.bestLapTime > 0 && vehicle.bestSector2 > 0 ? vehicle.bestLapTime - vehicle.bestSector2 : -1;
                opponent.LastLapTime = vehicle.lastLapTime > 0 ? vehicle.lastLapTime : -1;
                opponent.InPits = vehicle.inPits == 1;
                lastSectorTime = -1;
                switch (opponent.CurrentSectorNumber)
                {
                    case 1:
                        lastSectorTime = vehicle.lastLapTime > 0 ? vehicle.lastLapTime : -1;
                        break;
                    case 2:
                        lastSectorTime = vehicle.lastSector1 > 0 ? vehicle.lastSector1 : -1;

                        if (vehicle.curSector1 > 0.0)
                            lastSectorTime = (float)vehicle.curSector1;

                        break;
                    case 3:
                        lastSectorTime = vehicle.lastSector2 > 0 ? vehicle.lastSector2 : -1;

                        if (vehicle.curSector2 > 0.0)
                            lastSectorTime = (float)vehicle.curSector2;

                        break;
                    default:
                        break;
                }
                // on the first flying lap the lastSectorTime values for sectors 1 and 2 will all be zero, so we miss the first laptime unless we assume the first flying lap
                // is valid and use the game timer to derive the lap time :(
                if (opponent.IsNewLap)
                {
                    opponent.CompleteLapWithProvidedLapTime(opponent.Position, currentGameState.SessionData.SessionRunningTime,
                            lastSectorTime, lastSectorTime > 0, false, shared.trackTemp, shared.ambientTemp, currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining);
                    opponent.StartNewLap(opponent.CompletedLaps + 1, opponent.Position, vehicle.inPits == 1 || opponent.DistanceRoundTrack < 0, currentGameState.SessionData.SessionRunningTime, false, shared.trackTemp, shared.ambientTemp);
                }
                else if (isNewSector)
                {
                    opponent.AddCumulativeSectorData(opponent.Position, lastSectorTime, currentGameState.SessionData.SessionRunningTime,
                        lastSectorTime > 0 || (opponent.CurrentSectorNumber >= 2 && vehicle.totalLaps == 1), false, shared.trackTemp, shared.ambientTemp);
                }
                if (vehicle.inPits == 1 && opponent.CurrentSectorNumber == 3 && opponentPrevious != null && !opponentPrevious.isEnteringPits())
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
                    currentGameState.SessionData.TimeDeltaBehind = Math.Abs(vehicle.timeBehindNext);
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
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass < 0) && opponent.CarClass == currentGameState.carClass)
                {
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;
                }

                if (opponentPrevious != null)
                {
                    opponent.trackLandmarksTiming = opponentPrevious.trackLandmarksTiming;
                    String stoppedInLandmark = opponent.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                        currentGameState.SessionData.SessionRunningTime, previousDistanceRoundTrack, opponent.DistanceRoundTrack, opponent.Speed);
                    opponent.stoppedInLandmark = opponent.InPits ? null : stoppedInLandmark;
                }
                if (opponent.IsNewLap)
                {
                    opponent.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                }

                // shouldn't have duplicates, but just in case
                if (!currentGameState.OpponentData.ContainsKey(opponentKey))
                {
                    currentGameState.OpponentData.Add(opponentKey, opponent);
                }
            }
            currentGameState.PitData.InPitlane = player.inPits == 1;
            if (previousGameState != null)
            {
                currentGameState.SessionData.HasLeadChanged = !currentGameState.SessionData.HasLeadChanged && previousGameState.SessionData.Position > 1 && currentGameState.SessionData.Position == 1 ?
                    true : currentGameState.SessionData.HasLeadChanged;
                currentGameState.SessionData.IsRacingSameCarInFront = String.Equals(previousGameState.getOpponentKeyInFront(false), currentGameState.getOpponentKeyInFront(false));
                currentGameState.SessionData.IsRacingSameCarBehind = String.Equals(previousGameState.getOpponentKeyBehind(false), currentGameState.getOpponentKeyBehind(false));
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = !currentGameState.SessionData.IsRacingSameCarInFront ? 
                    currentGameState.SessionData.SessionRunningTime : previousGameState.SessionData.GameTimeAtLastPositionFrontChange;
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = !currentGameState.SessionData.IsRacingSameCarBehind ? 
                    currentGameState.SessionData.SessionRunningTime : previousGameState.SessionData.GameTimeAtLastPositionBehindChange;
                currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                                    currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                                    currentGameState.PositionAndMotionData.DistanceRoundTrack, shared.speed);
                currentGameState.SessionData.stoppedInLandmark = currentGameState.PitData.InPitlane ? null : stoppedInLandmark;
                if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                }
            }

            // --------------------------------
            // pit data
            currentGameState.PitData.IsRefuellingAllowed = true;

            // JB: this code estimates pit calls based on the number of scheduled stops in offline races, splitting the session into equal stints.
            // In order for this to work correctly, the MandatoryPitStop event needs to be structured differently - for multiple scheduled stops
            // the window end lap / time changes throughout the race, but the MandatoryPitStop event won't see these changes so will only call the 1st 'box now'

            if (enablePitWindowHack)
            {
                currentGameState.PitData.HasMandatoryPitStop = isOfflineSession && shared.scheduledStops > 0 && player.numPitstops < shared.scheduledStops &&
                    currentGameState.SessionData.SessionType == SessionType.Race;
                currentGameState.PitData.PitWindowStart = isOfflineSession && currentGameState.PitData.HasMandatoryPitStop ? 1 : 0;
                currentGameState.PitData.PitWindowEnd = !currentGameState.PitData.HasMandatoryPitStop ? 0 :
                    currentGameState.SessionData.SessionHasFixedTime ? (int)(((currentGameState.SessionData.SessionTotalRunTime / 60) / (shared.scheduledStops + 1)) * (player.numPitstops + 1)) + 1 :
                    (int)((currentGameState.SessionData.SessionNumberOfLaps / (shared.scheduledStops + 1)) * (player.numPitstops + 1)) + 1;

                // force the MandatoryPit event to be re-initialsed if the window end has been recalculated.
                currentGameState.PitData.ResetEvents = currentGameState.PitData.HasMandatoryPitStop && 
                    previousGameState != null && currentGameState.PitData.PitWindowEnd > previousGameState.PitData.PitWindowEnd;
            }

             
            currentGameState.PitData.IsAtPitExit = previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane;
            currentGameState.PitData.OnOutLap = (currentGameState.PitData.InPitlane && currentGameState.SessionData.SectorNumber == 1) ||
                (previousGameState != null && previousGameState.PitData.OnOutLap && !currentGameState.SessionData.IsNewLap) || 
                (currentGameState.SessionData.SessionType != SessionType.Race && currentGameState.SessionData.CompletedLaps == 0);
            currentGameState.PitData.OnInLap = currentGameState.PitData.InPitlane && currentGameState.SessionData.SectorNumber == 3;
            currentGameState.PitData.IsMakingMandatoryPitStop = currentGameState.PitData.HasMandatoryPitStop && currentGameState.PitData.OnInLap && currentGameState.SessionData.CompletedLaps > currentGameState.PitData.PitWindowStart;
            currentGameState.PitData.PitWindow = currentGameState.PitData.IsMakingMandatoryPitStop ? PitWindow.StopInProgress : 
                mapToPitWindow((rFactor1Constant.rfYellowFlagState)shared.yellowFlagState);

            // --------------------------------
            // fuel data
            // don't read fuel data until race session is green
            // don't read fuel data for non-race session until out of pit lane and more than one lap completed
            if ((currentGameState.SessionData.SessionType == SessionType.Race &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green ||
                currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow || 
                currentGameState.SessionData.SessionPhase == SessionPhase.Finished ||
                currentGameState.SessionData.SessionPhase == SessionPhase.Checkered)) ||
                (!currentGameState.PitData.InPitlane && currentGameState.SessionData.CompletedLaps > 1))
            {
                currentGameState.FuelData.FuelUseActive = true;
                currentGameState.FuelData.FuelLeft = shared.fuel;
            }

            // --------------------------------
            // flags data
            FlagEnum Flag = FlagEnum.UNKNOWN;
            if (currentGameState.SessionData.IsDisqualified && previousGameState != null && !previousGameState.SessionData.IsDisqualified)
            {
                Flag = FlagEnum.BLACK;
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
                Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier() + 
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

        private SessionPhase mapToSessionPhase(rFactor1Constant.rfGamePhase sessionPhase, SessionPhase previousSessionPhase, 
            Boolean finishedLap, Boolean isInPit)
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
                    if (isInPit || finishedLap || previousSessionPhase == SessionPhase.Finished)
                    {
                        return SessionPhase.Finished;
                    }
                    else
                    {
                        return SessionPhase.Checkered;
                    }
                // fullCourseYellow will count as greenFlag since we'll call it out in the Flags separately anyway

                    // TODO: can we map to FullCourseYellow here?
                case rFactor1Constant.rfGamePhase.fullCourseYellow:
                    return SessionPhase.FullCourseYellow;
                case rFactor1Constant.rfGamePhase.greenFlag:
                    return SessionPhase.Green;
                default:
                    return SessionPhase.Unavailable;
            }
        }

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            rFactor1Data.rfShared shared = (rFactor1Data.rfShared)memoryMappedFileStruct;
            switch (shared.session)
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

        // finds OpponentData key for given vehicle based on driver name, vehicle class, and world position
        private String getOpponentKeyForVehicleInfo(rfVehicleInfo vehicle, GameStateData previousGameState, float sessionRunningTime, String driverName, int duplicatesCount)
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
            float[] worldPos = { vehicle.pos.x, vehicle.pos.z };
            float minDistDiff = -1;
            float timeDelta = sessionRunningTime - previousGameState.SessionData.SessionRunningTime;
            String bestKey = null;
            foreach (String possibleKey in possibleKeys)
            {
                if (previousGameState.OpponentData.ContainsKey(possibleKey))
                {
                    OpponentData o = previousGameState.OpponentData[possibleKey];
                    if (o.DriverRawName != getNameFromBytes(vehicle.driverName).ToLower() ||
                        o.CarClass != getCarClass(getNameFromBytes(vehicle.vehicleName), false) ||
                        opponentKeysProcessed.Contains(possibleKey))
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
                        bestKey = possibleKey;
                    }
                }
            }
            if (bestKey != null)
            {
                opponentKeysProcessed.Add(bestKey);
            }
            return bestKey;
        }

        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        }

        /**
         * For AMS, vehicleName has the form classname: driver name #number
         */
        public CarData.CarClass getCarClass(String vehicleName, Boolean forPlayer)
        {
            if (vehicleName.Length > 0)
            {
                int splitChar = vehicleName.IndexOf(':');
                if (splitChar > 0)
                {
                    vehicleName = vehicleName.Substring(0, splitChar);
                    if (forPlayer)
                    {
                        CarData.CLASS_ID = vehicleName;
                    }
                    return CarData.getCarClassForClassName(vehicleName);
                }
                else
                {
                    if (forPlayer)
                    {
                        CarData.CLASS_ID = vehicleName;
                    }
                    return CarData.getCarClassForClassName(vehicleName);
                }
            }
            return CarData.getCarClassForClassName(vehicleName);
        }
    }
}
