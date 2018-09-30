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
        private readonly bool enableBlueOnSlower = UserSettings.GetUserSettings().getBoolean("enable_ams_blue_on_slower");
        private readonly bool enableFCYPitStateMessages = UserSettings.GetUserSettings().getBoolean("enable_ams_pit_state_during_fcy");

        private bool incrementCutTrackCountWhenLeavingRacingSurface = true;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;
        
        // if we're running only against AI, force the pit window to open
        private Boolean isOfflineSession = true;
        // keep track of opponents processed this time
        private List<String> opponentKeysProcessed = new List<String>();
        // detect when approaching racing surface after being off track
        private float distanceOffTrack = 0;
        private Boolean isApproachingTrack = false;
        // dynamically calculated wheel circumferences
        private float[] wheelCircumference = new float[] { 0, 0 };

        private SessionPhase lastSessionPhase = SessionPhase.Unavailable;

        // Track landmarks cache.
        private string lastSessionTrackName = null;
        private TrackDataContainer lastSessionTrackDataContainer = null;
        private HardPartsOnTrackData lastSessionHardPartsOnTrackData = null;
        private float lastSessionTrackLength = -1.0f;

        // next track conditions sample due after:
        private DateTime nextConditionsSampleDue = DateTime.MinValue;

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

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in rFactor shared data so this is a no-op
        }

        public override GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            CrewChiefV4.rFactor1.RF1SharedMemoryReader.RF1StructWrapper wrapper = (CrewChiefV4.rFactor1.RF1SharedMemoryReader.RF1StructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            rFactor1Data.rfShared shared = wrapper.data;

            // no session data
            if (shared.numVehicles == 0)
            {
                // if we skip to next session the session phase never goes to 'finished'. We do, however, see the numVehicles drop to zero.
                // If we have a previous game state and it's in a valid phase here, update it to Finished and return it.
                if (previousGameState != null && previousGameState.SessionData.SessionType != SessionType.Unavailable &&
                    previousGameState.SessionData.SessionPhase != SessionPhase.Finished &&
                    previousGameState.SessionData.SessionPhase != SessionPhase.Unavailable &&
                    lastSessionPhase != SessionPhase.Unavailable &&
                    lastSessionPhase != SessionPhase.Finished)
                {
                    previousGameState.SessionData.SessionPhase = SessionPhase.Finished;
                    lastSessionPhase = previousGameState.SessionData.SessionPhase;
                    previousGameState.SessionData.AbruptSessionEndDetected = true;
                    return previousGameState;
                }
                else
                {
                    isOfflineSession = true;
                    distanceOffTrack = 0;
                    isApproachingTrack = false;
                    wheelCircumference = new float[] { 0, 0 };
                    previousGameState = null;
                    lastSessionPhase = SessionPhase.Unavailable;
                    return null;
                }
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
                String driverName = getStringFromBytes(player.driverName).ToLower();
                Validator.validate(driverName);
                playerName = driverName;
            }
            // these things should remain constant during a session
            currentGameState.SessionData.EventIndex = shared.session;
            currentGameState.SessionData.SessionIteration = 
                shared.session >= 1 && shared.session <= 4 ? shared.session - 1 :
                shared.session >= 5 && shared.session <= 8 ? shared.session - 5 :
                shared.session >= 10 && shared.session <= 13 ? shared.session - 10 : 0;
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            currentGameState.SessionData.SessionPhase = mapToSessionPhase((rFactor1Constant.rfGamePhase)shared.gamePhase,
                currentGameState.SessionData.SessionType, ref player);
            lastSessionPhase = currentGameState.SessionData.SessionPhase;

            // --------------------------------
            // flags data
            currentGameState.FlagData.useImprovisedIncidentCalling = false;

            currentGameState.FlagData.isFullCourseYellow = currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow
                || shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.resume;

            if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.resume)
            {
                // Special case for resume after FCY.  rF2 no longer has FCY set, but still has Resume sub phase set.
                currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.RACING;
                currentGameState.FlagData.lapCountWhenLastWentGreen = shared.lapNumber;
            }
            else if (currentGameState.FlagData.isFullCourseYellow)
            {
                if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pending)
                    currentGameState.FlagData.fcyPhase = FullCourseYellowPhase.PENDING;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pitClosed)
                    currentGameState.FlagData.fcyPhase = this.enableFCYPitStateMessages ? FullCourseYellowPhase.PITS_CLOSED : FullCourseYellowPhase.IN_PROGRESS;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pitOpen)
                    currentGameState.FlagData.fcyPhase = this.enableFCYPitStateMessages ? FullCourseYellowPhase.PITS_OPEN : FullCourseYellowPhase.IN_PROGRESS;
                else if (shared.yellowFlagState == (sbyte)rFactor1Constant.rfYellowFlagState.pitLeadLap)
                    currentGameState.FlagData.fcyPhase = this.enableFCYPitStateMessages ? FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES : FullCourseYellowPhase.IN_PROGRESS;
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

            currentGameState.carClass = getCarClass(getStringFromBytes(shared.vehicleName), true);
            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
            currentGameState.SessionData.DriverRawName = getStringFromBytes(player.driverName).ToLower();
            currentGameState.SessionData.TrackDefinition = new TrackDefinition(getStringFromBytes(shared.trackName), shared.lapDist);
            if (previousGameState != null)
            {
                // copy from previous gamestate
                currentGameState.SessionData.TrackDefinition.trackLandmarks = previousGameState.SessionData.TrackDefinition.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.gapPoints = previousGameState.SessionData.TrackDefinition.gapPoints;
                currentGameState.readLandmarksForThisLap = previousGameState.readLandmarksForThisLap;
                currentGameState.retriedDriverNames = previousGameState.retriedDriverNames;
                currentGameState.disqualifiedDriverNames = previousGameState.disqualifiedDriverNames;
                currentGameState.FlagData.currentLapIsFCY = previousGameState.FlagData.currentLapIsFCY;
                currentGameState.FlagData.previousLapWasFCY = previousGameState.FlagData.previousLapWasFCY;
                currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
            }
            if (currentGameState.FlagData.isFullCourseYellow)
            {
                currentGameState.FlagData.currentLapIsFCY = true;
            }

            // don't do this on every tick - assume the previous gamestate's data we copied earlier are still valid:
            // currentGameState.SessionData.TrackDefinition.setGapPoints();

            currentGameState.SessionData.SessionNumberOfLaps = shared.maxLaps > 0 && shared.maxLaps < 1000 ? shared.maxLaps : 0;
            // default to 60:30 if both session time and number of laps undefined (test day)
            float defaultSessionTotalRunTime = 3630;
            currentGameState.SessionData.SessionTotalRunTime = shared.endET > 0 ? shared.endET : currentGameState.SessionData.SessionNumberOfLaps > 0 ? 0 : defaultSessionTotalRunTime;
            // if previous state is null or any of the above change, this is a new session
            currentGameState.SessionData.IsNewSession = previousGameState == null ||
                currentGameState.SessionData.SessionType != previousGameState.SessionData.SessionType ||
                !CarData.IsCarClassEqual(currentGameState.carClass, previousGameState.carClass) ||
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
            {
                previousGameState = null; 
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);

                // Initialize track landmarks for this session.
                TrackDataContainer tdc = null;
                if (this.lastSessionTrackDataContainer != null
                    && this.lastSessionTrackName == currentGameState.SessionData.TrackDefinition.name
                    && this.lastSessionTrackLength == shared.lapDist)
                {
                    tdc = this.lastSessionTrackDataContainer;
                    if (tdc.trackLandmarks.Count > 0)
                        Console.WriteLine(tdc.trackLandmarks.Count + " landmarks defined for this track");

                    if (this.lastSessionHardPartsOnTrackData != null
                        && this.lastSessionHardPartsOnTrackData.hardPartsMapped)
                        currentGameState.hardPartsOnTrackData = this.lastSessionHardPartsOnTrackData;
                }
                else
                {
                    tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(currentGameState.SessionData.TrackDefinition.name, shared.lapDist);
                    this.lastSessionTrackDataContainer = tdc;
                    this.lastSessionHardPartsOnTrackData = null;

                    this.lastSessionTrackName = currentGameState.SessionData.TrackDefinition.name;
                    this.lastSessionTrackLength = shared.lapDist;
                }
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
            }

            currentGameState.SessionData.SessionStartTime = currentGameState.SessionData.IsNewSession ? currentGameState.Now : previousGameState.SessionData.SessionStartTime;
            currentGameState.SessionData.SessionHasFixedTime = currentGameState.SessionData.SessionTotalRunTime > 0;
            currentGameState.SessionData.SessionRunningTime = shared.currentET;
            currentGameState.SessionData.SessionTimeRemaining = currentGameState.SessionData.SessionHasFixedTime ? currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionRunningTime : 0;
            // hack for test day sessions running longer than allotted time
            currentGameState.SessionData.SessionTimeRemaining = currentGameState.SessionData.SessionTimeRemaining < 0 && shared.session == 0 ? defaultSessionTotalRunTime : currentGameState.SessionData.SessionTimeRemaining;
            currentGameState.SessionData.NumCarsOverall = shared.numVehicles;
            currentGameState.SessionData.NumCarsOverallAtStartOfSession = currentGameState.SessionData.IsNewSession ? currentGameState.SessionData.NumCarsOverall : previousGameState.SessionData.NumCarsOverallAtStartOfSession;

            currentGameState.SessionData.OverallPosition = player.place;
            currentGameState.SessionData.SectorNumber = player.sector == 0 ? 3 : player.sector;
            currentGameState.SessionData.IsNewSector = currentGameState.SessionData.IsNewSession || currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;
            currentGameState.SessionData.IsNewLap = currentGameState.SessionData.IsNewSession || (currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == 1);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.readLandmarksForThisLap = false;
                currentGameState.FlagData.previousLapWasFCY = previousGameState != null && previousGameState.FlagData.currentLapIsFCY;
                currentGameState.FlagData.currentLapIsFCY = currentGameState.FlagData.isFullCourseYellow;
            }
            currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.IsNewLap ? currentGameState.SessionData.OverallPosition : previousGameState.SessionData.PositionAtStartOfCurrentLap;
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

                currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;
                currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                currentGameState.TimingData = previousGameState.TimingData;
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
                currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                        lastSectorTime, lastSectorTime > 0, player.inPits == 1, false, shared.trackTemp, shared.ambientTemp, 
                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3, currentGameState.TimingData);
                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1, currentGameState.SessionData.OverallPosition, player.inPits == 1 || player.lapDist < 0, currentGameState.SessionData.SessionRunningTime);
            }
            else if (currentGameState.SessionData.IsNewSector)
            {
                currentGameState.SessionData.playerAddCumulativeSectorData(previousGameState.SessionData.SectorNumber, currentGameState.SessionData.OverallPosition, lastSectorTime,
                    currentGameState.SessionData.SessionRunningTime,  lastSectorTime > 0 || (currentGameState.SessionData.SectorNumber >= 2 && player.totalLaps == 1), 
                    false, shared.trackTemp, shared.ambientTemp);
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

            currentGameState.SessionData.LeaderHasFinishedRace = leader.finishStatus == (int)rFactor1Constant.rfFinishStatus.finished;
            currentGameState.SessionData.LeaderSectorNumber = leader.sector == 0 ? 3 : leader.sector;
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
                currentGameState.CarDamageData.LastImpactTime = shared.lastImpactET;
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

            var yaw = Math.Atan2(player.oriZ.x, player.oriZ.z);

            var pitch = Math.Atan2(-player.oriY.z,
              Math.Sqrt(player.oriX.z * player.oriX.z + player.oriZ.z * player.oriZ.z));

            var roll = Math.Atan2(player.oriY.x,
              Math.Sqrt(player.oriX.x * player.oriX.x + player.oriZ.x * player.oriZ.x));

            currentGameState.PositionAndMotionData.Orientation.Pitch = (float)pitch;
            currentGameState.PositionAndMotionData.Orientation.Roll = (float)roll;
            currentGameState.PositionAndMotionData.Orientation.Yaw = (float)yaw;

            // Initialize DeltaTime.
            if (currentGameState.SessionData.IsNewSession)
            {
                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
            }
            currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack,
                currentGameState.SessionData.CompletedLaps, currentGameState.PositionAndMotionData.CarSpeed, currentGameState.Now);

            // --------------------------------
            // tire data
            // Automobilista reports in Kelvin
            currentGameState.TyreData.TyreWearActive = true;
            currentGameState.TyreData.LeftFrontAttached = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].detached == 0;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[0] - 273;
            currentGameState.TyreData.FrontLeft_CenterTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[1] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].temperature[2] - 273;

            float frontLeftTemp = (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3;
            currentGameState.TyreData.FrontLeftPressure = shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].pressure;
            currentGameState.TyreData.FrontLeftPercentWear = (1 - shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].wear) * 100;
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontLeftTemperatureForLap == 0)
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
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontRightTemperatureForLap == 0)
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
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearLeftTemperatureForLap == 0)
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
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearRightTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (previousGameState == null || rearRightTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear,
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            var tyreTempThresholds = CarData.getTyreTempThresholds(currentGameState.carClass);
            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(tyreTempThresholds,
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap, currentGameState.TyreData.PeakFrontRightTemperatureForLap,
                currentGameState.TyreData.PeakRearLeftTemperatureForLap, currentGameState.TyreData.PeakRearRightTemperatureForLap);
            // some simple locking / spinning checks
            if (currentGameState.PositionAndMotionData.CarSpeed > 7.0f)
            {
                float minRotatingSpeed = (float)Math.PI * currentGameState.PositionAndMotionData.CarSpeed / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.wheel[0].rotation) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.wheel[1].rotation) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.wheel[2].rotation) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.wheel[3].rotation) < minRotatingSpeed;

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
            if (currentGameState.Now > nextConditionsSampleDue)
            {
                nextConditionsSampleDue = currentGameState.Now.Add(ConditionsMonitor.ConditionsSampleFrequency);
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.ambientTemp, shared.trackTemp, 0, (float)Math.Sqrt((double)(shared.wind.x * shared.wind.x + shared.wind.y * shared.wind.y + shared.wind.z * shared.wind.z)),
                    0, 0, 0, currentGameState.SessionData.IsNewLap);
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
                String driverName = getStringFromBytes(vehicle.driverName).ToLower();
                if (isOfflineSession && (rFactor1Constant.rfControl)vehicle.control == rFactor1Constant.rfControl.remote)
                {
                    isOfflineSession = false;
                }
                var numNames = -1;
                if (driverNameCounts.TryGetValue(driverName, out numNames))
                {
                    driverNameCounts[driverName] = ++numNames;
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
                String driverName = getStringFromBytes(vehicle.driverName).ToLower();
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
                        OpponentData opp = null;
                        if (previousGameState != null && previousGameState.OpponentData.TryGetValue(driverName, out opp) &&
                            !currentGameState.OpponentData.ContainsKey(driverName))
                        {
                            currentGameState.OpponentData.Add(driverName, opp);
                        }
                        opponentKeysProcessed.Add(driverName);
                        continue;
                    }
                    else
                    {
                        // offline we can have any number of duplicates :(
                        opponentKey = getOpponentKeyForVehicleInfo(vehicle, previousGameState, currentGameState.SessionData.SessionRunningTime, driverName, duplicatesCount);
                        // there's no previous opponent data record for this driver so create one
                        int numDuplicates = -1;
                        if (duplicatesCreated.TryGetValue(driverName, out numDuplicates))
                        {
                            duplicatesCreated[driverName] = ++numDuplicates;
                        }
                        else
                        {
                            numDuplicates = 1;
                            duplicatesCreated.Add(driverName, 1);
                        }
                        opponentKey = driverName + "_duplicate_" + numDuplicates;
                    }
                }
                else
                {
                    opponentKey = driverName;
                }

                var ofs = (rFactor1Constant.rfFinishStatus)vehicle.finishStatus;
                if (ofs == rFactor1Constant.rfFinishStatus.dnf)
                {
                    // Note driver DNF and don't tack him anymore.
                    if (!currentGameState.retriedDriverNames.Contains(driverName))
                    {
                        Console.WriteLine("Opponent " + driverName + " has retired");
                        currentGameState.retriedDriverNames.Add(driverName);
                    }
                    continue;
                }
                else if (ofs == rFactor1Constant.rfFinishStatus.dq)
                {
                    // Note driver DQ and don't tack him anymore.
                    if (!currentGameState.disqualifiedDriverNames.Contains(driverName))
                    {
                        Console.WriteLine("Opponent " + driverName + " has been disqualified");
                        currentGameState.disqualifiedDriverNames.Add(driverName);
                    }
                    continue;
                }

                OpponentData opponentPrev = null;
                opponentPrevious = previousGameState == null || opponentKey == null || !previousGameState.OpponentData.TryGetValue(opponentKey, out opponentPrev) ? null : opponentPrev;
                OpponentData opponent = new OpponentData();
                if (opponentPrevious != null)
                {
                    opponent.OverallPositionAtPreviousTick = opponentPrevious.OverallPosition;
                    opponent.ClassPositionAtPreviousTick = opponentPrevious.ClassPosition;
                }
                opponent.DriverRawName = driverName;
                opponent.DriverNameSet = opponent.DriverRawName.Length > 0;
                opponent.CarClass = getCarClass(getStringFromBytes(vehicle.vehicleName), false);
                opponent.OverallPosition = vehicle.place;
                if (opponent.DriverNameSet && opponentPrevious == null && CrewChief.enableDriverNames)
                {
                    speechRecogniser.addNewOpponentName(opponent.DriverRawName);
                    Console.WriteLine("New driver " + opponent.DriverRawName + 
                        " is using car class " + opponent.CarClass.getClassIdentifier() +
                        " at position " + opponent.OverallPosition.ToString());
                }
                if (opponentPrevious != null)
                {
                    foreach (LapData old in opponentPrevious.OpponentLapData)
                    {
                        opponent.OpponentLapData.Add(old);
                    }
                    opponent.NumPitStops = opponentPrevious.NumPitStops;                    
                }
                opponent.SessionTimeAtLastPositionChange = opponentPrevious != null && opponentPrevious.OverallPosition != opponent.OverallPosition ? currentGameState.SessionData.SessionRunningTime : -1;
                opponent.CompletedLaps = vehicle.totalLaps;
                opponent.CurrentSectorNumber = vehicle.sector == 0 ? 3 : vehicle.sector;
                Boolean isNewSector = currentGameState.SessionData.IsNewSession || (opponentPrevious != null && opponentPrevious.CurrentSectorNumber != opponent.CurrentSectorNumber);
                opponent.IsNewLap = currentGameState.SessionData.IsNewSession || (isNewSector && opponent.CurrentSectorNumber == 1 && opponent.CompletedLaps > 0);
                opponent.Speed = vehicle.speed;
                opponent.DistanceRoundTrack = vehicle.lapDist;
                opponent.WorldPosition = new float[] { vehicle.pos.x, vehicle.pos.z };
                opponent.CurrentBestLapTime = vehicle.bestLapTime > 0 ? vehicle.bestLapTime : -1;
                opponent.PreviousBestLapTime = opponentPrevious != null && opponentPrevious.CurrentBestLapTime > 0 && 
                    opponentPrevious.CurrentBestLapTime > opponent.CurrentBestLapTime ? opponentPrevious.CurrentBestLapTime : -1;
                float previousDistanceRoundTrack = opponentPrevious != null ? opponentPrevious.DistanceRoundTrack : 0;

                if (previousDistanceRoundTrack > 0)
                {
                    // if we've just crossed the 'near to pit entry' mark, update our near-pit-entry position. Otherwise copy it from the previous state
                    if (previousDistanceRoundTrack < currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks
                        && opponent.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks)
                    {
                        opponent.PositionOnApproachToPitEntry = opponent.OverallPosition;
                    }
                    else
                    {
                        opponent.PositionOnApproachToPitEntry = opponentPrevious.PositionOnApproachToPitEntry;
                    }
                    // carry over the delta time - do this here so if we have to initalise it we have the correct distance data
                    opponent.DeltaTime = opponentPrevious.DeltaTime;
                }
                else
                {
                    opponent.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, opponent.DistanceRoundTrack, DateTime.UtcNow);
                }
                opponent.DeltaTime.SetNextDeltaPoint(opponent.DistanceRoundTrack, opponent.CompletedLaps, opponent.Speed, currentGameState.Now);

                opponent.bestSector1Time = vehicle.bestSector1 > 0 ? vehicle.bestSector1 : -1;
                opponent.bestSector2Time = vehicle.bestSector2 > 0 && vehicle.bestSector1 > 0 ? vehicle.bestSector2 - vehicle.bestSector1 : -1;
                opponent.bestSector3Time = vehicle.bestLapTime > 0 && vehicle.bestSector2 > 0 ? vehicle.bestLapTime - vehicle.bestSector2 : -1;
                opponent.LastLapTime = vehicle.lastLapTime > 0 ? vehicle.lastLapTime : -1;                
                opponent.InPits = vehicle.inPits == 1;
                opponent.JustEnteredPits = opponentPrevious != null && !opponentPrevious.InPits && opponent.InPits;

                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionRunningTime > 10
                    && opponentPrevious != null && !opponentPrevious.InPits && opponent.InPits)
                {
                    opponent.NumPitStops++;
                }

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
                    opponent.CompleteLapWithProvidedLapTime(opponent.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                            lastSectorTime, lastSectorTime > 0, vehicle.inPits == 1, false, shared.trackTemp, shared.ambientTemp, 
                            currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3, currentGameState.TimingData, 
                            CarData.IsCarClassEqual(opponent.CarClass, currentGameState.carClass));
                    opponent.StartNewLap(opponent.CompletedLaps + 1, opponent.OverallPosition, vehicle.inPits == 1 || opponent.DistanceRoundTrack < 0, currentGameState.SessionData.SessionRunningTime, false, shared.trackTemp, shared.ambientTemp);
                }
                else if (isNewSector)
                {
                    opponent.AddCumulativeSectorData(opponentPrevious.CurrentSectorNumber, opponent.OverallPosition, lastSectorTime, currentGameState.SessionData.SessionRunningTime,
                        lastSectorTime > 0 || (opponent.CurrentSectorNumber >= 2 && vehicle.totalLaps == 1), false, shared.trackTemp, shared.ambientTemp);
                }
                if (vehicle.inPits == 1 && opponent.CurrentSectorNumber == 3 && opponentPrevious != null && !opponentPrevious.isEnteringPits())
                {
                    opponent.setInLap();  
                }

                // session best lap times
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
                    CarData.IsCarClassEqual(opponent.CarClass, currentGameState.carClass))
                {
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = opponent.CurrentBestLapTime;
                    if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                        currentGameState.SessionData.PlayerClassSessionBestLapTime > opponent.CurrentBestLapTime)
                    {
                        currentGameState.SessionData.PlayerClassSessionBestLapTime = opponent.CurrentBestLapTime;
                    }
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

            currentGameState.sortClassPositions();
            currentGameState.setPracOrQualiDeltas();
            
            currentGameState.PitData.InPitlane = player.inPits == 1;
            if (previousGameState != null)
            {
                currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                                    currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                                    currentGameState.PositionAndMotionData.DistanceRoundTrack, shared.speed);
                currentGameState.SessionData.stoppedInLandmark = currentGameState.PitData.InPitlane ? null : stoppedInLandmark;
                if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                }
                currentGameState.SessionData.SessionStartClassPosition = previousGameState.SessionData.SessionStartClassPosition;
                currentGameState.SessionData.ClassPositionAtStartOfCurrentLap = previousGameState.SessionData.ClassPositionAtStartOfCurrentLap;
                currentGameState.SessionData.NumCarsInPlayerClassAtStartOfSession = previousGameState.SessionData.NumCarsInPlayerClassAtStartOfSession;
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

            if (previousGameState != null)
            {
                currentGameState.PitData.MandatoryPitStopCompleted = previousGameState.PitData.MandatoryPitStopCompleted || currentGameState.PitData.IsMakingMandatoryPitStop;
            }

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
            if (this.enableBlueOnSlower
                && !currentGameState.FlagData.isFullCourseYellow)  // Don't announce blue on slower under FCY.
            {
                foreach (var opponent in currentGameState.OpponentData.Values)
                {
                    if (currentGameState.SessionData.SessionType != SessionType.Race
                        || currentGameState.SessionData.CompletedLaps < 1
                        || currentGameState.PositionAndMotionData.DistanceRoundTrack < 0.0f)
                    {
                        break;
                    }

                    if (opponent.getCurrentLapData().InLap
                        || opponent.getCurrentLapData().OutLap
                        || opponent.OverallPosition + 2 > currentGameState.SessionData.OverallPosition)   // ignore blue if this opponent is directly ahead of us in the race
                    {
                        continue;
                    }

                    if (isBehindWithinDistance(currentGameState.SessionData.TrackDefinition.trackLength, 8.0f, 40.0f,
                            currentGameState.PositionAndMotionData.DistanceRoundTrack, opponent.DistanceRoundTrack)
                        && opponent.Speed >= currentGameState.PositionAndMotionData.CarSpeed)
                    {
                        Flag = FlagEnum.BLUE;
                        break;
                    }
                }
            }
            if (currentGameState.SessionData.IsDisqualified && previousGameState != null && !previousGameState.SessionData.IsDisqualified)
            {
                Flag = FlagEnum.BLACK;
            }
            currentGameState.SessionData.Flag = Flag;

            // --------------------------------
            // penalties data
            currentGameState.PenaltiesData.NumPenalties = player.numPenalties;

            if (previousGameState != null)
            {
                currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
            }

            // Improvised cut track warnings based on surface type.
            if (!currentGameState.PitData.OnOutLap && !currentGameState.PitData.InPitlane && incrementCutTrackCountWhenLeavingRacingSurface)
            {
                rFactor1Constant.rfSurfaceType fl_surface = (rFactor1Constant.rfSurfaceType)shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontLeft].surfaceType;
                rFactor1Constant.rfSurfaceType fr_surface = (rFactor1Constant.rfSurfaceType)shared.wheel[(int)rFactor1Constant.rfWheelIndex.frontRight].surfaceType;
                rFactor1Constant.rfSurfaceType rl_surface = (rFactor1Constant.rfSurfaceType)shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearLeft].surfaceType;
                rFactor1Constant.rfSurfaceType rr_surface = (rFactor1Constant.rfSurfaceType)shared.wheel[(int)rFactor1Constant.rfWheelIndex.rearRight].surfaceType;

                // assume kerb is racing surface here - any wheel on a racing surface is OK
                currentGameState.PenaltiesData.IsOffRacingSurface =
                    fl_surface != rFactor1Constant.rfSurfaceType.dry && fl_surface != rFactor1Constant.rfSurfaceType.wet && fl_surface != rFactor1Constant.rfSurfaceType.kerb &&
                    fr_surface != rFactor1Constant.rfSurfaceType.dry && fr_surface != rFactor1Constant.rfSurfaceType.wet && fr_surface != rFactor1Constant.rfSurfaceType.kerb &&
                    rl_surface != rFactor1Constant.rfSurfaceType.dry && rl_surface != rFactor1Constant.rfSurfaceType.wet && rl_surface != rFactor1Constant.rfSurfaceType.kerb &&
                    rr_surface != rFactor1Constant.rfSurfaceType.dry && rr_surface != rFactor1Constant.rfSurfaceType.wet && rr_surface != rFactor1Constant.rfSurfaceType.kerb;

                if (previousGameState != null && !previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface)
                {
                    Console.WriteLine("Player off track: by surface type.");
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
                }
            }

            if (!currentGameState.PenaltiesData.IsOffRacingSurface)
            {
                float lateralDistDiff = (float)(Math.Abs(player.pathLateral) - Math.Abs(player.trackEdge));
                currentGameState.PenaltiesData.IsOffRacingSurface = !currentGameState.PitData.InPitlane && lateralDistDiff >= 2;
                float offTrackDistanceDelta = lateralDistDiff - distanceOffTrack;
                distanceOffTrack = currentGameState.PenaltiesData.IsOffRacingSurface ? lateralDistDiff : 0;
                isApproachingTrack = offTrackDistanceDelta < 0 && currentGameState.PenaltiesData.IsOffRacingSurface && lateralDistDiff < 3;

                if (!currentGameState.PitData.OnOutLap && previousGameState != null
                && !previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface
                && !(currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Countdown))
                {
                    Console.WriteLine("Player off track: by distance.");
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
                }
            }

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
                Console.WriteLine("EventIndex: " + currentGameState.SessionData.EventIndex);
                Console.WriteLine("SessionType: " + currentGameState.SessionData.SessionType);
                Console.WriteLine("SessionPhase: " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("SessionIteration: " + currentGameState.SessionData.SessionIteration);
                Console.WriteLine("HasMandatoryPitStop: " + currentGameState.PitData.HasMandatoryPitStop);
                Console.WriteLine("PitWindowStart: " + currentGameState.PitData.PitWindowStart);
                Console.WriteLine("PitWindowEnd: " + currentGameState.PitData.PitWindowEnd);
                Console.WriteLine("NumCarsAtStartOfSession: " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);
                Console.WriteLine("SessionNumberOfLaps: " + currentGameState.SessionData.SessionNumberOfLaps);
                Console.WriteLine("SessionRunTime: " + currentGameState.SessionData.SessionTotalRunTime);
                Console.WriteLine("SessionStartTime: " + currentGameState.SessionData.SessionStartTime);
                Console.WriteLine("Player is using car class: \"" + currentGameState.carClass.getClassIdentifier() +
                    "\" at position: " + currentGameState.SessionData.OverallPosition.ToString());
                Utilities.TraceEventClass(currentGameState);
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

            if (currentGameState.SessionData.TrackDefinition != null)
            {
                CrewChief.trackName = currentGameState.SessionData.TrackDefinition.name;
            }
            if (currentGameState.carClass != null)
            {
                CrewChief.carClass = currentGameState.carClass.carClassEnum;
            }
            CrewChief.distanceRoundTrack = currentGameState.PositionAndMotionData.DistanceRoundTrack;
            CrewChief.viewingReplay = false;

            if (previousGameState != null &&
                currentGameState.SessionData.SessionType == SessionType.Race &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                    (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                     previousGameState.SessionData.SessionPhase == SessionPhase.Countdown))
                currentGameState.SessionData.JustGoneGreen = true;

            if (currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.hardPartsOnTrackData.updateHardPartsForNewLap(currentGameState.SessionData.LapTimePrevious))
                {
                    currentGameState.SessionData.TrackDefinition.adjustGapPoints(currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap);
                }
            }
            else if (!currentGameState.PitData.OnOutLap && !currentGameState.SessionData.TrackDefinition.isOval &&
                !(currentGameState.SessionData.SessionType == SessionType.Race &&
                   (currentGameState.SessionData.CompletedLaps < 1 || (GameStateData.useManualFormationLap && currentGameState.SessionData.CompletedLaps < 2))))
            {
                currentGameState.hardPartsOnTrackData.mapHardPartsOnTrack(currentGameState.ControlData.BrakePedal, currentGameState.ControlData.ThrottlePedal,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CurrentLapIsValid, currentGameState.SessionData.TrackDefinition.trackLength);
            }
            this.lastSessionHardPartsOnTrackData = currentGameState.hardPartsOnTrackData;

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

        private SessionPhase mapToSessionPhase(
            rFactor1Constant.rfGamePhase sessionPhase,
            SessionType sessionType,
            ref rfVehicleInfo player)
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
                    if (sessionType == SessionType.Race
                        && player.finishStatus == (sbyte)rFactor1Constant.rfFinishStatus.none)
                    {
                        return SessionPhase.Checkered;
                    }
                    else
                    {
                        return SessionPhase.Finished;
                    }
                // fullCourseYellow will count as greenFlag since we'll call it out in the Flags separately anyway
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
            if (CrewChief.gameDefinition == GameDefinition.rFactor1)
            {
                // I don't have RF1 so am guessing at these
                switch (shared.session)
                {
                    // up to three possible practice sessions?
                    case 1:
                    case 2:
                    case 3:
                    // test day and pre-race warm-up sessions are 'Practice' as well since 'HotLap' seems to suppress flag info?
                    case 0:
                    case 6:
                        return SessionType.Practice;
                    // one qualifying session?
                    case 5:
                        return SessionType.Qualify;
                    // no idea how many race sessions are available and if there's a session type > 7
                    case 7:
                    case 8:
                        return SessionType.Race;
                    default:
                        return SessionType.Unavailable;
                }
            }
            else
            {
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
                OpponentData o = null;
                if (previousGameState.OpponentData.TryGetValue(possibleKey, out o))
                {
                    if (o.DriverRawName != getStringFromBytes(vehicle.driverName).ToLower() ||
                        !CarData.IsCarClassEqual(o.CarClass, getCarClass(getStringFromBytes(vehicle.vehicleName), false)) ||
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

        public static String getStringFromBytes(byte[] bytes)
        {
            var nullIdx = Array.IndexOf(bytes, (byte)0);

            return nullIdx >= 0
              ? Encoding.Default.GetString(bytes, 0, nullIdx)
              : Encoding.Default.GetString(bytes);
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
