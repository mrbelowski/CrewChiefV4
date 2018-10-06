using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using System.Diagnostics;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV4.PCars2
{
    class PCars2GameStateMapper : GameStateMapper
    {
        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;
        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(new CarData.CarClass());

        // if all 4 wheels are off the racing surface, increment the number of cut track incedents
        private Boolean incrementCutTrackCountWhenLeavingRacingSurface = false;

        private List<uint> racingSurfaces = new List<uint>() { (uint)eTerrainMaterials.TERRAIN_BUMPY_DIRT_ROAD, 
            (uint)eTerrainMaterials.TERRAIN_BUMPY_ROAD1, (uint)eTerrainMaterials.TERRAIN_BUMPY_ROAD2, (uint)eTerrainMaterials.TERRAIN_BUMPY_ROAD3, 
            (uint)eTerrainMaterials.TERRAIN_COBBLES, (uint)eTerrainMaterials.TERRAIN_DRAINS, (uint)eTerrainMaterials.TERRAIN_EXIT_RUMBLE_STRIPS,
            (uint)eTerrainMaterials.TERRAIN_LOW_GRIP_ROAD, (uint)eTerrainMaterials.TERRAIN_MARBLES,(uint)eTerrainMaterials.TERRAIN_PAVEMENT,
            (uint)eTerrainMaterials.TERRAIN_ROAD, (uint)eTerrainMaterials.TERRAIN_RUMBLE_STRIPS, (uint)eTerrainMaterials.TERRAIN_SAND_ROAD};

        // 3 or 4 wheels on any of these terrains triggers a possible cut warning
        private HashSet<eTerrainMaterials> illegalSurfaces = new HashSet<eTerrainMaterials>(
            new eTerrainMaterials[]{ eTerrainMaterials.TERRAIN_GRASSY_BERMS, eTerrainMaterials.TERRAIN_GRASS, eTerrainMaterials.TERRAIN_LONG_GRASS,
                                     eTerrainMaterials.TERRAIN_SLOPE_GRASS, eTerrainMaterials.TERRAIN_RUNOFF_ROAD, eTerrainMaterials.TERRAIN_ILLEGAL_STRIP,
                                     eTerrainMaterials.TERRAIN_PAINT_CONCRETE_ILLEGAL, eTerrainMaterials.TERRAIN_DRY_VERGE, eTerrainMaterials.TERRAIN_GRASSCRETE });

        // 2 wheels on one of these terrains, while 2 wheels are on an illegal material, will log a warning to the console
        private HashSet<eTerrainMaterials> marginalSurfaces = new HashSet<eTerrainMaterials>(
            new eTerrainMaterials[] { eTerrainMaterials.TERRAIN_RUMBLE_STRIPS, eTerrainMaterials.TERRAIN_PAINT_CONCRETE });

        private Boolean loggedPossibleTrackLimitViolationOnThisLap = false;
        private Boolean loggedTrackLimitViolationOnThisLap = false;

        private float trivialEngineDamageThreshold = 0.05f;
        private float minorEngineDamageThreshold = 0.20f;
        private float severeEngineDamageThreshold = 0.45f;
        private float destroyedEngineDamageThreshold = 0.90f;

        private float trivialSuspensionDamageThreshold = 0.01f;
        private float minorSuspensionDamageThreshold = 0.05f;
        private float severeSuspensionDamageThreshold = 0.15f;
        private float destroyedSuspensionDamageThreshold = 0.60f;

        private float trivialBrakeDamageThreshold = 0.15f;
        private float minorBrakeDamageThreshold = 0.3f;
        private float severeBrakeDamageThreshold = 0.6f;
        private float destroyedBrakeDamageThreshold = 0.90f;

        private float trivialAeroDamageThreshold = 0.1f;
        private float minorAeroDamageThreshold = 0.25f;
        private float severeAeroDamageThreshold = 0.6f;
        private float destroyedAeroDamageThreshold = 0.90f;

        // tyres in PCars are worn out when the wear level is > ?
        private float wornOutTyreWearLevel = 0.50f;

        private float scrubbedTyreWearPercent = 1f;
        private float minorTyreWearPercent = 20f;
        private float majorTyreWearPercent = 40f;
        private float wornOutTyreWearPercent = 80f;
        
        private Dictionary<string, float> waitingForCarsToFinish = new Dictionary<string, float>();
        private DateTime nextDebugCheckeredToFinishMessageTime = DateTime.MinValue;

        private static int lastGuessAtPlayerIndex = -1;

        Dictionary<string, DateTime> lastActiveTimeForOpponents = new Dictionary<string, DateTime>();
        DateTime nextOpponentCleanupTime = DateTime.MinValue;
        TimeSpan opponentCleanupInterval = TimeSpan.FromSeconds(4);
        HashSet<uint> positionsFilledForThisTick = new HashSet<uint>();
        List<String> opponentDriverNamesProcessedForThisTick = new List<String>();

        private float lastCollisionMagnitude = 0;
        private Boolean collisionOnThisLap = false;
        private float collisionMagnitudeThreshold = 0.02f;

        // next track conditions sample due after:
        private DateTime nextConditionsSampleDue = DateTime.MinValue;

        private DateTime lastTimeEngineWasRunning = DateTime.MaxValue;
        
        public PCars2GameStateMapper()
        {
            CornerData.EnumWithThresholds suspensionDamageNone = new CornerData.EnumWithThresholds(DamageLevel.NONE, -10000, trivialSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageTrivial = new CornerData.EnumWithThresholds(DamageLevel.TRIVIAL, trivialSuspensionDamageThreshold, minorSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageMinor = new CornerData.EnumWithThresholds(DamageLevel.MINOR, trivialSuspensionDamageThreshold, severeSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageMajor = new CornerData.EnumWithThresholds(DamageLevel.MAJOR, severeSuspensionDamageThreshold, destroyedSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageDestroyed = new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, destroyedSuspensionDamageThreshold, 10000);
            suspensionDamageThresholds.Add(suspensionDamageNone);
            suspensionDamageThresholds.Add(suspensionDamageTrivial);
            suspensionDamageThresholds.Add(suspensionDamageMinor);
            suspensionDamageThresholds.Add(suspensionDamageMajor);
            suspensionDamageThresholds.Add(suspensionDamageDestroyed);

            CornerData.EnumWithThresholds brakeDamageNone = new CornerData.EnumWithThresholds(DamageLevel.NONE, -10000, trivialBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageTrivial = new CornerData.EnumWithThresholds(DamageLevel.TRIVIAL, trivialBrakeDamageThreshold, minorBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageMinor = new CornerData.EnumWithThresholds(DamageLevel.MINOR, trivialBrakeDamageThreshold, severeBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageMajor = new CornerData.EnumWithThresholds(DamageLevel.MAJOR, severeBrakeDamageThreshold, destroyedBrakeDamageThreshold);
            CornerData.EnumWithThresholds brakeDamageDestroyed = new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, destroyedBrakeDamageThreshold, 10000);
            brakeDamageThresholds.Add(brakeDamageNone);
            brakeDamageThresholds.Add(brakeDamageTrivial);
            brakeDamageThresholds.Add(brakeDamageMinor);
            brakeDamageThresholds.Add(brakeDamageMajor);
            brakeDamageThresholds.Add(brakeDamageDestroyed);

            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));
        }

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no-op
        }

        public static void addOpponentForName(String name, OpponentData opponentData, GameStateData gameState)
        {
            if (name == null || name.Length == 0)
            {
                return;
            }
            if (gameState.OpponentData == null)
            {
                gameState.OpponentData = new Dictionary<string, OpponentData>();
            }
            gameState.OpponentData.Remove(name);
            gameState.OpponentData.Add(name, opponentData);
        }

        public static int getPlayerIndex(pCars2APIStruct pCars2APIStruct)
        {
            // we have no idea where in the participants array the local player is. No idea at all. And there's no way to work it out.
            // Offline it's generally 0. Online it can be anything. We can't use mViewedParticipantIndex here because monitoring other
            // cars will fck up the internal state. mViewedParticipantIndex is random and wrong when you first enter a session so we 
            // can't grab and reuse the first value we get.
            //
            // If we're 'playing' assume we're the player:
            if (pCars2APIStruct.mGameState == (uint) eGameState.GAME_INGAME_PLAYING)
            {
                lastGuessAtPlayerIndex = pCars2APIStruct.mViewedParticipantIndex;
                //Console.WriteLine("Playing, updating player index guess to " + pCars2APIStruct.mViewedParticipantIndex + 
                //    " player name " + StructHelper.getNameFromBytes(pCars2APIStruct.mParticipantData[lastGuessAtPlayerIndex].mName));                
            }
            // if we've never played in this session, return what the game tells us:
            if (lastGuessAtPlayerIndex == -1)
            {
                // all we can do here is report what the game tells us, even though it'll be wrong
                return pCars2APIStruct.mViewedParticipantIndex;
            }
            return lastGuessAtPlayerIndex;
        }

        public override GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            pCars2APIStruct shared = ((CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper)memoryMappedFileStruct).data;
            long ticks = ((CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper)memoryMappedFileStruct).ticksWhenRead;
            eGameState gameState = (eGameState)shared.mGameState;

            if (gameState == eGameState.GAME_INGAME_REPLAY || gameState == eGameState.GAME_FRONT_END_REPLAY)
            {
                CrewChief.trackName = StructHelper.getNameFromBytes(shared.mTrackLocation) + ":" + StructHelper.getNameFromBytes(shared.mTrackVariation);
                CrewChief.carClass = CarData.getCarClassForClassName(StructHelper.getNameFromBytes(shared.mCarClassName)).carClassEnum;
                CrewChief.viewingReplay = true;
                CrewChief.distanceRoundTrack = shared.mParticipantData[shared.mViewedParticipantIndex].mCurrentLapDistance;
            }

            if (gameState == eGameState.GAME_FRONT_END ||
                gameState == eGameState.GAME_INGAME_PAUSED ||
                gameState == eGameState.GAME_INGAME_REPLAY ||
                gameState == eGameState.GAME_FRONT_END_REPLAY ||
                gameState == eGameState.GAME_EXITED ||
                shared.mNumParticipants < 1 || shared.mTrackLength <= 0)
            {
                if (shared.mGameState == (uint)eGameState.GAME_FRONT_END && previousGameState != null)
                {
                    previousGameState.SessionData.SessionType = SessionType.Unavailable;
                    previousGameState.SessionData.SessionPhase = SessionPhase.Unavailable;
                }
                return previousGameState;
            }

            int playerIndex = getPlayerIndex(shared);
            pCars2APIParticipantStruct playerData = shared.mParticipantData[playerIndex];

            String playerName = StructHelper.getNameFromBytes(shared.mParticipantData[playerIndex].mName);

            GameStateData currentGameState = new GameStateData(ticks);
            
            /*Console.WriteLine("SessionState: " + (eSessionState)shared.mSessionState + " RaceState: " + (eRaceState)shared.mRaceState + 
                " GameState: " + (eGameState) shared.mGameState + " PitMode: " + (ePitMode) shared.mPitMode +
                " EventTimeRemaining: " + shared.mEventTimeRemaining + " LapsInEvent: " + 
                shared.mLapsInEvent + " SequenceNumber: " + shared.mSequenceNumber);*/
            
            Validator.validate(playerName);
            currentGameState.SessionData.CompletedLaps = (int)playerData.mLapsCompleted;
            currentGameState.SessionData.SectorNumber = (int)playerData.mCurrentSector + 1; // zero indexed
            currentGameState.SessionData.OverallPosition = (int)playerData.mRacePosition;
            if (currentGameState.SessionData.OverallPosition == 1)
            {
                currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
            }
            currentGameState.SessionData.IsNewSector = previousGameState == null || playerData.mCurrentSector + 1 != previousGameState.SessionData.SectorNumber;
            // When in the pit lane, mCurrentLapDistance gets set to 0 when crossing the start line and *remains at 0* until some distance into the lap (about 300 metres)
            currentGameState.PositionAndMotionData.DistanceRoundTrack = playerData.mCurrentLapDistance;
                        
            // previous session data to check if we've started an new session
            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;
            float lastSessionRunningTime = 0;
            int lastSessionLapsCompleted = 0;
            TrackDefinition lastSessionTrack = null;
            Boolean lastSessionHasFixedTime = false;
            int lastSessionNumberOfLaps = 0;
            float lastSessionTotalRunTime = 0;
            float lastSessionTimeRemaining = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionType = previousGameState.SessionData.SessionType;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
                lastSessionTrack = previousGameState.SessionData.TrackDefinition;
                lastSessionLapsCompleted = previousGameState.SessionData.CompletedLaps;
                lastSessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                lastSessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                lastSessionTimeRemaining = previousGameState.SessionData.SessionTimeRemaining;
                currentGameState.carClass = previousGameState.carClass;

                currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                currentGameState.SessionData.PlayerLapTimeSessionBestPrevious = previousGameState.SessionData.PlayerLapTimeSessionBestPrevious;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                currentGameState.SessionData.OverallSessionBestLapTime = previousGameState.SessionData.OverallSessionBestLapTime;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = previousGameState.SessionData.PlayerClassSessionBestLapTime;
                currentGameState.readLandmarksForThisLap = previousGameState.readLandmarksForThisLap;
            }

            if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
            {
                String carClassId = StructHelper.getNameFromBytes(shared.mCarClassName);
                CarData.CarClass newClass = CarData.getCarClassForClassName(carClassId);
                CarData.CLASS_ID = carClassId;
                if (!CarData.IsCarClassEqual(newClass, currentGameState.carClass))
                {
                    currentGameState.carClass = newClass;
                    GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                    Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                    brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                    // no tyre data in the block so get the default tyre types for this car
                    defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                    Utilities.TraceEventClass(currentGameState);
                }
            }

            // current session data
            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.LeaderHasFinishedRace = leaderHasFinished;
            currentGameState.SessionData.IsDisqualified = shared.mRaceState == (int)eRaceState.RACESTATE_DISQUALIFIED;

            int numberOfLapsInSession = (int)shared.mLapsInEvent;

            if (numberOfLapsInSession  <= 0)
            {
                currentGameState.SessionData.SessionHasFixedTime = true;
                if (lastSessionRunningTime == 0)
                {
                    currentGameState.SessionData.SessionTotalRunTime = shared.mEventTimeRemaining;
                }
                currentGameState.SessionData.SessionTimeRemaining = shared.mEventTimeRemaining;
            }

            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType,
                shared.mSessionState, shared.mRaceState, shared.mNumParticipants, leaderHasFinished, lastSessionPhase, lastSessionTimeRemaining,
                lastSessionRunningTime, shared.mPitModes[playerIndex], (eGameState)shared.mGameState, previousGameState == null ? null : previousGameState.OpponentData,
                shared.mSpeed, currentGameState.Now);
                        
            currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(StructHelper.getNameFromBytes(shared.mTrackLocation)
                + ":" + StructHelper.getNameFromBytes(shared.mTrackVariation), -1, shared.mTrackLength);
            // now check if this is a new session...

            Boolean sessionOfSameTypeRestarted = ((currentGameState.SessionData.SessionType == SessionType.Race && lastSessionType == SessionType.Race) ||
                (currentGameState.SessionData.SessionType == SessionType.Practice && lastSessionType == SessionType.Practice) ||
                (currentGameState.SessionData.SessionType == SessionType.Qualify && lastSessionType == SessionType.Qualify)) &&
                (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.Checkered || lastSessionPhase == SessionPhase.FullCourseYellow || 
                    lastSessionPhase == SessionPhase.Finished) &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Countdown &&
                (currentGameState.SessionData.SessionType == SessionType.Race ||
                    currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTimeRemaining > lastSessionTimeRemaining + 1);


            // pcars will update the session type before updating the race state, so it goes [qual / finished] -> [race / finished] -> [race / not started]
            // so don't count this as a valid transition
            eSessionState rawSessionState = (eSessionState) shared.mSessionState;
            eRaceState rawRaceState = (eRaceState) shared.mRaceState;
            Boolean ignoreFinishedStatus = previousGameState != null && 
                ((previousGameState.SessionData.SessionType == SessionType.Practice && rawSessionState == eSessionState.SESSION_QUALIFY) ||
                 (previousGameState.SessionData.SessionType == SessionType.Qualify && rawSessionState == eSessionState.SESSION_RACE))
                 && rawRaceState == eRaceState.RACESTATE_FINISHED;
            if (ignoreFinishedStatus)
            {
                // don't allow the session type to be updated here
                currentGameState.SessionData.SessionType = previousGameState.SessionData.SessionType;
            }
            if (!ignoreFinishedStatus && 
                (sessionOfSameTypeRestarted ||
                 (currentGameState.SessionData.SessionType != SessionType.Unavailable && 
                     (lastSessionType != currentGameState.SessionData.SessionType ||                
                         lastSessionTrack == null || lastSessionTrack.name != currentGameState.SessionData.TrackDefinition.name ||
                             (currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTimeRemaining > lastSessionTimeRemaining + 1))))
                )
            {
                lastGuessAtPlayerIndex = -1;
                lastActiveTimeForOpponents.Clear();
                nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
                Console.WriteLine("New session, trigger...");
                if (sessionOfSameTypeRestarted)
                {
                    Console.WriteLine("Session of same type (" + lastSessionType + ") restarted (green / finished -> countdown)");
                }
                if (lastSessionType != currentGameState.SessionData.SessionType) 
                {
                    Console.WriteLine("lastSessionType = " + lastSessionType + " currentGameState.SessionData.SessionType = " + currentGameState.SessionData.SessionType);
                }
                else if (lastSessionTrack != currentGameState.SessionData.TrackDefinition)
                {
                    String lastTrackName = lastSessionTrack == null ? "unknown" : lastSessionTrack.name;
                    String currentTrackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                    Console.WriteLine("lastSessionTrack = " + lastTrackName + " currentGameState.SessionData.Track = " + currentTrackName);
                }
                else if (currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTimeRemaining > lastSessionTimeRemaining + 1)
                {
                    Console.WriteLine("sessionTimeRemaining = " + currentGameState.SessionData.SessionTimeRemaining + " lastSessionTimeRemaining = " + lastSessionTimeRemaining);
                }
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionTotalRunTime = shared.mEventTimeRemaining;
                    currentGameState.SessionData.SessionTimeRemaining = shared.mEventTimeRemaining;
                    currentGameState.SessionData.SessionRunningTime = 0;
                    Console.WriteLine("Time in this new session = " + currentGameState.SessionData.SessionTimeRemaining);
                }
                currentGameState.SessionData.DriverRawName = playerName;
                currentGameState.PitData.IsRefuellingAllowed = true;

                String carClassId = StructHelper.getNameFromBytes(shared.mCarClassName);
                currentGameState.carClass = CarData.getCarClassForClassName(carClassId);
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                CarData.CLASS_ID = carClassId;

                Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                // no tyre data in the block so get the default tyre types for this car
                defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);

                lastTimeEngineWasRunning = DateTime.MaxValue;

                opponentDriverNamesProcessedForThisTick.Clear();
                opponentDriverNamesProcessedForThisTick.Add(playerName);
                positionsFilledForThisTick.Clear();
                positionsFilledForThisTick.Add((uint)currentGameState.SessionData.OverallPosition);
                for (int i = 0; i < shared.mParticipantData.Length; i++)
                {
                    pCars2APIParticipantStruct participantStruct = shared.mParticipantData[i];
                    String participantName = StructHelper.getNameFromBytes(participantStruct.mName).ToLower();
                    if (i != playerIndex && participantStruct.mIsActive && participantName != null && participantName.Length > 0
                        && !opponentDriverNamesProcessedForThisTick.Contains(participantName) && !positionsFilledForThisTick.Contains(participantStruct.mRacePosition))
                    {
                        CarData.CarClass opponentCarClass = CarData.getCarClassForClassName(StructHelper.getCarClassName(shared, i));
                        addOpponentForName(participantName, createOpponentData(participantStruct, false, opponentCarClass,
                            participantStruct.mName != null && participantStruct.mName[0] != 0, currentGameState.SessionData.TrackDefinition.trackLength), currentGameState);
                        opponentDriverNamesProcessedForThisTick.Add(participantName);
                        positionsFilledForThisTick.Add(participantStruct.mRacePosition);
                    }
                }

                currentGameState.SessionData.PlayerLapTimeSessionBest = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = -1;
                currentGameState.SessionData.OverallSessionBestLapTime = -1;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = -1;
                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(currentGameState.SessionData.TrackDefinition.name, shared.mTrackLength);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                {
                    if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                    {
                        if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                        {
                            currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                        }
                    }
                }
                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);

                currentGameState.PitData.MandatoryPitStopCompleted = false;
                currentGameState.PitData.PitWindow = shared.mEnforcedPitStopLap > 0 ? PitWindow.Closed : PitWindow.Unavailable;
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        // just gone green, so get the session data.
                        lastActiveTimeForOpponents.Clear();
                        nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            currentGameState.SessionData.JustGoneGreen = true;
                            // ensure that we track the car we're in at the point when the lights change
                            if (currentGameState.SessionData.SessionHasFixedTime)
                            {
                                currentGameState.SessionData.SessionTotalRunTime = shared.mEventTimeRemaining;
                                currentGameState.SessionData.SessionTimeRemaining = shared.mEventTimeRemaining;
                                currentGameState.SessionData.SessionRunningTime = 0;
                            }
                            currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                            currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                        }          
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsOverallAtStartOfSession = shared.mNumParticipants;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(StructHelper.getNameFromBytes(shared.mTrackLocation) + ":" +
                            StructHelper.getNameFromBytes(shared.mTrackVariation), -1, shared.mTrackLength);
                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(currentGameState.SessionData.TrackDefinition.name, shared.mTrackLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                        currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                        if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                        {
                            if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                            {
                                if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                                {
                                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                                }
                            }
                        }
                        String carClassId = StructHelper.getNameFromBytes(shared.mCarClassName);
                        currentGameState.carClass = CarData.getCarClassForClassName(carClassId);
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        CarData.CLASS_ID = carClassId;

                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        // no tyre data in the block so get the default tyre types for this car
                        defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                        if (previousGameState != null)
                        {
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            if (currentGameState.SessionData.SessionType != SessionType.Race)
                            {
                                currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                                currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                                currentGameState.SessionData.SessionTimeRemaining = previousGameState.SessionData.SessionTimeRemaining;
                                currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                            }
                        }

                        currentGameState.PitData.MandatoryPitStopCompleted = false;
                        currentGameState.PitData.PitWindow = shared.mEnforcedPitStopLap > 0 ? PitWindow.Closed : PitWindow.Unavailable;

                        currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);

                        Console.WriteLine("Just gone green, session details...");
                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        if (previousGameState != null)
                        {
                            Console.WriteLine("previous SessionPhase " + previousGameState.SessionData.SessionPhase);
                        }
                        Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
                        Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("PitWindowStart " + currentGameState.PitData.PitWindowStart);
                        Console.WriteLine("PitWindowEnd " + currentGameState.PitData.PitWindowEnd);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName);
                    }
                }
                // copy persistent data from the previous game state
                //

                if (!currentGameState.SessionData.JustGoneGreen && previousGameState != null)
                {
                    //Console.WriteLine("regular update, session type = " + currentGameState.SessionData.SessionType + " phase = " + currentGameState.SessionData.SessionPhase);
                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.NumCarsOverallAtStartOfSession = previousGameState.SessionData.NumCarsOverallAtStartOfSession;
                    currentGameState.SessionData.NumCarsInPlayerClassAtStartOfSession = previousGameState.SessionData.NumCarsInPlayerClassAtStartOfSession;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = previousGameState.SessionData.PositionAtStartOfCurrentLap;
                    currentGameState.SessionData.SessionStartClassPosition = previousGameState.SessionData.SessionStartClassPosition;
                    currentGameState.SessionData.ClassPositionAtStartOfCurrentLap = previousGameState.SessionData.ClassPositionAtStartOfCurrentLap;
                    currentGameState.SessionData.LapTimePrevious = previousGameState.SessionData.LapTimePrevious;
                    currentGameState.OpponentData = previousGameState.OpponentData;

                    currentGameState.PitData.PitWindowStart = previousGameState.PitData.PitWindowStart;
                    currentGameState.PitData.PitWindowEnd = previousGameState.PitData.PitWindowEnd;
                    currentGameState.PitData.PitWindow = previousGameState.PitData.PitWindow;
                    currentGameState.PitData.HasMandatoryPitStop = previousGameState.PitData.HasMandatoryPitStop;
                    currentGameState.PitData.HasMandatoryTyreChange = previousGameState.PitData.HasMandatoryTyreChange;
                    currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = previousGameState.PitData.MandatoryTyreChangeRequiredTyreType;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                    currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = previousGameState.PitData.MaxPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.MinPermittedDistanceOnCurrentTyre = previousGameState.PitData.MinPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.OnInLap = previousGameState.PitData.OnInLap;
                    currentGameState.PitData.OnOutLap = previousGameState.PitData.OnOutLap;
                    currentGameState.PitData.HasRequestedPitStop = previousGameState.PitData.HasRequestedPitStop;
                    currentGameState.PitData.PitStallOccupied = previousGameState.PitData.PitStallOccupied;
                    currentGameState.PitData.IsPitCrewReady = previousGameState.PitData.IsPitCrewReady;

                    currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState.SessionData.SessionTimesAtEndOfSectors;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;                    
                    currentGameState.SessionData.GameTimeAtLastPositionFrontChange = previousGameState.SessionData.GameTimeAtLastPositionFrontChange;
                    currentGameState.SessionData.GameTimeAtLastPositionBehindChange = previousGameState.SessionData.GameTimeAtLastPositionBehindChange;
                    currentGameState.SessionData.LastSector1Time = previousGameState.SessionData.LastSector1Time;
                    currentGameState.SessionData.LastSector2Time = previousGameState.SessionData.LastSector2Time;
                    currentGameState.SessionData.LastSector3Time = previousGameState.SessionData.LastSector3Time;
                    currentGameState.SessionData.PlayerBestSector1Time = previousGameState.SessionData.PlayerBestSector1Time;
                    currentGameState.SessionData.PlayerBestSector2Time = previousGameState.SessionData.PlayerBestSector2Time;
                    currentGameState.SessionData.PlayerBestSector3Time = previousGameState.SessionData.PlayerBestSector3Time;
                    currentGameState.SessionData.PlayerBestLapSector1Time = previousGameState.SessionData.PlayerBestLapSector1Time;
                    currentGameState.SessionData.PlayerBestLapSector2Time = previousGameState.SessionData.PlayerBestLapSector2Time;
                    currentGameState.SessionData.PlayerBestLapSector3Time = previousGameState.SessionData.PlayerBestLapSector3Time;
                    currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;
                    currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
                    currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;

                    currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;

                    currentGameState.disqualifiedDriverNames = previousGameState.disqualifiedDriverNames;
                    currentGameState.retriedDriverNames = previousGameState.retriedDriverNames;

                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;

                    currentGameState.TimingData = previousGameState.TimingData;

                    currentGameState.SessionData.JustGoneGreenTime = previousGameState.SessionData.JustGoneGreenTime;
                }
            }

            currentGameState.ControlData.ThrottlePedal = shared.mThrottle;
            currentGameState.ControlData.ClutchPedal = shared.mClutch;
            currentGameState.TransmissionData.Gear = shared.mGear;
            currentGameState.ControlData.BrakeBias = shared.mBrake;

            //------------------- Variable session data ---------------------------
            if (currentGameState.SessionData.SessionHasFixedTime)
            {
                //Console.WriteLine("Setting session running time 1, total run time = " + currentGameState.SessionData.SessionTotalRunTime + " event time remaining  " + shared.mEventTimeRemaining);
                currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionTotalRunTime - shared.mEventTimeRemaining;
                currentGameState.SessionData.SessionTimeRemaining = shared.mEventTimeRemaining;
            }
            else
            {
                currentGameState.SessionData.SessionRunningTime = (float)(currentGameState.Now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
            }

            if (shared.mLastOpponentCollisionMagnitude != lastCollisionMagnitude)
            {
                // oops, we've hit someone...
                lastCollisionMagnitude = shared.mLastOpponentCollisionMagnitude;
                if (lastCollisionMagnitude > collisionMagnitudeThreshold)
                {
                    currentGameState.CarDamageData.LastImpactTime = currentGameState.SessionData.SessionRunningTime;
                    if (!collisionOnThisLap && shared.mLastOpponentCollisionIndex > -1 && shared.mLastOpponentCollisionIndex < shared.mParticipantData.Length)
                    {
                        Console.WriteLine("Collision on lap " + (playerData.mLapsCompleted + 1) + " strength: " + shared.mLastOpponentCollisionMagnitude +
                            " with opponent name = " + StructHelper.getNameFromBytes(shared.mParticipantData[shared.mLastOpponentCollisionIndex].mName) +
                            " in position " + shared.mParticipantData[shared.mLastOpponentCollisionIndex].mRacePosition);
                    }
                    collisionOnThisLap = true;
                }
            }

            ePitMode pitMode = (ePitMode)shared.mPitMode;
            currentGameState.PitData.InPitlane =
                pitMode == ePitMode.PIT_MODE_DRIVING_INTO_PITS ||
                pitMode == ePitMode.PIT_MODE_IN_PIT ||
                pitMode == ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS ||
                pitMode == ePitMode.PIT_MODE_IN_GARAGE ||
                pitMode == ePitMode.PIT_MODE_DRIVING_OUT_OF_GARAGE;
            if (shared.mLapInvalidated)
            {
                if (currentGameState.SessionData.CurrentLapIsValid && currentGameState.SessionData.CompletedLaps > 0)
                {
                    // log lap invalidation if we're not in the pits
                    Console.WriteLine("Invalidating lap " + (currentGameState.SessionData.CompletedLaps + 1) + " in sector " + currentGameState.SessionData.SectorNumber +
                        " at distance " + playerData.mCurrentLapDistance + " lap time " + shared.mmfOnly_mCurrentTime + " collision on this lap = " + collisionOnThisLap);
                }
                currentGameState.SessionData.CurrentLapIsValid = false;
            }

            currentGameState.SessionData.Flag = mapToFlagEnum(shared.mHighestFlagColour);
            currentGameState.SessionData.NumCarsOverall = shared.mNumParticipants;
            currentGameState.SessionData.IsNewLap = previousGameState != null &&
                (currentGameState.SessionData.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                  (currentGameState.SessionData.SessionType == SessionType.Practice &&
                      currentGameState.SessionData.SectorNumber == 1 && previousGameState.SessionData.SectorNumber == 3));

            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.readLandmarksForThisLap = false;
                loggedPossibleTrackLimitViolationOnThisLap = false;
                loggedTrackLimitViolationOnThisLap = false;
                collisionOnThisLap = false;

                currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                        shared.mLastLapTime, currentGameState.SessionData.CurrentLapIsValid, currentGameState.PitData.InPitlane, shared.mRainDensity > 0, 
                        shared.mTrackTemperature, shared.mAmbientTemperature, currentGameState.SessionData.SessionHasFixedTime,
                        currentGameState.SessionData.SessionTimeRemaining, 3, currentGameState.TimingData);
                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                    currentGameState.SessionData.OverallPosition, currentGameState.PitData.InPitlane, currentGameState.SessionData.SessionRunningTime);
            }
            else if (currentGameState.SessionData.IsNewSector)
            {
                if (currentGameState.SessionData.SectorNumber == 2)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(1, currentGameState.SessionData.OverallPosition, shared.mCurrentSector1Times[playerIndex],
                        currentGameState.SessionData.SessionRunningTime, currentGameState.SessionData.CurrentLapIsValid, shared.mRainDensity > 0,
                        shared.mTrackTemperature, shared.mAmbientTemperature);
                }
                else if (currentGameState.SessionData.SectorNumber == 3)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(2, currentGameState.SessionData.OverallPosition, shared.mCurrentSector2Times[playerIndex] + shared.mCurrentSector1Times[playerIndex],
                        currentGameState.SessionData.SessionRunningTime, currentGameState.SessionData.CurrentLapIsValid, shared.mRainDensity > 0,
                        shared.mTrackTemperature, shared.mAmbientTemperature);
                }
            }

            if (previousGameState != null)
            {
                String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                    currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, shared.mSpeed);
                currentGameState.SessionData.stoppedInLandmark = currentGameState.PitData.InPitlane ? null : stoppedInLandmark;
                if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                }
            }

            currentGameState.SessionData.LapTimeCurrent = shared.mCurrentSector1Times[playerIndex] +
                shared.mCurrentSector3Times[playerIndex] + shared.mCurrentSector3Times[playerIndex];

            currentGameState.SessionData.TimeDeltaBehind = shared.mSplitTimeBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.mSplitTimeAhead;

            opponentDriverNamesProcessedForThisTick.Clear();
            opponentDriverNamesProcessedForThisTick.Add(playerName);
            positionsFilledForThisTick.Clear();
            positionsFilledForThisTick.Add((uint)currentGameState.SessionData.OverallPosition);
            // the player can appear many times in the participant data array. We have no sane way of knowing which is the 'correct' player.
            // So all we can do is discard all of them as duplicates. Where an opponent appears multiple times, only use the first one. All 
            // these stupid bugs have existed since PCars1
            for (int i = 0; i < shared.mParticipantData.Length; i++)
            {
                if (i != playerIndex)
                {
                    pCars2APIParticipantStruct participantStruct = shared.mParticipantData[i];
                    if (participantStruct.mName == null || participantStruct.mName[0] == 0)
                    {
                        // first character of name is null - this means the game regards this driver as inactive or missing for this update
                        continue;
                    }
                    if (positionsFilledForThisTick.Contains(participantStruct.mRacePosition))
                    {
                        // discard this participant element because the race position is already occupied
                        continue;
                    }
                    String participantName = StructHelper.getNameFromBytes(participantStruct.mName).ToLower();
                    if (participantName != null && participantName.Length > 0 && !opponentDriverNamesProcessedForThisTick.Contains(participantName))
                    {
                        opponentDriverNamesProcessedForThisTick.Add(participantName);
                        positionsFilledForThisTick.Add(participantStruct.mRacePosition);
                        if (shared.mRaceStates[i] == (uint)eRaceState.RACESTATE_DNF || shared.mRaceStates[i] == (uint)eRaceState.RACESTATE_RETIRED)
                        {
                            if (!currentGameState.retriedDriverNames.Contains(participantName))
                            {
                                Console.WriteLine("Opponent " + participantName + " has retired");
                                currentGameState.retriedDriverNames.Add(participantName);
                            }
                            // remove this driver from the set immediately
                            currentGameState.OpponentData.Remove(participantName);
                            continue;
                        }
                        if (shared.mRaceStates[i] == (uint)eRaceState.RACESTATE_DISQUALIFIED)
                        {
                            if (!currentGameState.disqualifiedDriverNames.Contains(participantName))
                            {
                                Console.WriteLine("Opponent " + participantName + " has been disqualified");
                                currentGameState.disqualifiedDriverNames.Add(participantName);
                            }
                            // remove this driver from the set immediately
                            currentGameState.OpponentData.Remove(participantName);
                            continue;
                        }

                        OpponentData currentOpponentData = null;
                        if (currentGameState.OpponentData.TryGetValue(participantName, out currentOpponentData))
                        {
                            if (participantStruct.mIsActive)
                            {
                                lastActiveTimeForOpponents[participantName] = currentGameState.Now;
                                if (previousGameState != null)
                                {
                                    int previousOpponentSectorNumber = 1;
                                    int previousOpponentCompletedLaps = 0;
                                    int previousOpponentPosition = 0;
                                    Boolean previousOpponentIsEnteringPits = false;
                                    Boolean previousOpponentIsExitingPits = false;

                                    float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                                    float previousDistanceRoundTrack = 0;
                                    OpponentData previousOpponentData = null;
                                    if (previousGameState.OpponentData.TryGetValue(participantName, out previousOpponentData))
                                    {
                                        previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                        previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                        previousOpponentPosition = previousOpponentData.OverallPosition;
                                        previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                        previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                        previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                        previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                                        currentOpponentData.ClassPositionAtPreviousTick = previousOpponentData.ClassPosition;
                                        currentOpponentData.OverallPositionAtPreviousTick = previousOpponentData.OverallPosition;
                                    }

                                    int currentOpponentRacePosition = (int)participantStruct.mRacePosition;
                                    int currentOpponentLapsCompleted = (int)participantStruct.mLapsCompleted;
                                    int currentOpponentSector = (int)participantStruct.mCurrentSector + 1;  // zero-indexed
                                    if (currentOpponentSector == 0)
                                    {
                                        currentOpponentSector = previousOpponentSectorNumber;
                                    }
                                    float currentOpponentLapDistance = participantStruct.mCurrentLapDistance;

                                    if (currentOpponentRacePosition == 1 && (currentGameState.SessionData.SessionNumberOfLaps > 0 &&
                                            currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted) ||
                                            (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining < 1 &&
                                            previousOpponentCompletedLaps < currentOpponentLapsCompleted))
                                    {
                                        currentGameState.SessionData.LeaderHasFinishedRace = true;
                                    }
                                    ePitMode opponentPitMode = (ePitMode)shared.mPitModes[i];
                                    Boolean isEnteringPits = opponentPitMode == ePitMode.PIT_MODE_DRIVING_INTO_PITS;
                                    Boolean isLeavingPits = opponentPitMode == ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS || opponentPitMode == ePitMode.PIT_MODE_DRIVING_OUT_OF_GARAGE;
                                    Boolean isInPits = opponentPitMode == ePitMode.PIT_MODE_IN_PIT || opponentPitMode == ePitMode.PIT_MODE_IN_GARAGE;
                                                                        
                                    float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;
                                    float lastSectorTime = -1;
                                    if (currentOpponentSector == 1)
                                    {
                                        lastSectorTime = shared.mCurrentSector3Times[i];
                                    }
                                    else if (currentOpponentSector == 2)
                                    {
                                        lastSectorTime = shared.mCurrentSector1Times[i];
                                    }
                                    else if (currentOpponentSector == 3)
                                    {
                                        lastSectorTime = shared.mCurrentSector2Times[i];
                                    }
                                    updateOpponentData(currentOpponentData, currentOpponentRacePosition, currentOpponentLapsCompleted,
                                            currentOpponentSector, isEnteringPits, isInPits, isLeavingPits, currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                            new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] }, previousOpponentWorldPosition,
                                            shared.mSpeeds[i], shared.mWorldFastestLapTime, shared.mWorldFastestSector1Time, shared.mWorldFastestSector2Time, shared.mWorldFastestSector3Time, 
                                            participantStruct.mCurrentLapDistance, shared.mRainDensity == 1,
                                            shared.mAmbientTemperature, shared.mTrackTemperature,
                                            currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                            lastSectorTime, shared.mLapsInvalidated[i] == 1, currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks,
                                            currentGameState.TimingData, currentGameState.carClass);

                                    if (previousOpponentData != null)
                                    {
                                        currentOpponentData.trackLandmarksTiming = previousOpponentData.trackLandmarksTiming;
                                        String stoppedInLandmark = currentOpponentData.trackLandmarksTiming.updateLandmarkTiming(
                                            currentGameState.SessionData.TrackDefinition, currentGameState.SessionData.SessionRunningTime,
                                            previousDistanceRoundTrack, currentOpponentData.DistanceRoundTrack, currentOpponentData.Speed);
                                        currentOpponentData.stoppedInLandmark = currentOpponentData.InPits ? null : stoppedInLandmark;
                                    }
                                    if (currentGameState.SessionData.JustGoneGreen)
                                    {
                                        currentOpponentData.trackLandmarksTiming = new TrackLandmarksTiming();
                                    }
                                    if (currentOpponentData.IsNewLap)
                                    {
                                        currentOpponentData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                                        currentOpponentData.CarClass = CarData.getCarClassForClassName(StructHelper.getCarClassName(shared, i));
                                    }
                                    if (currentOpponentData.IsNewLap && currentOpponentData.CurrentBestLapTime > 0)
                                    {
                                        if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall == -1 ||
                                            currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestOverall)
                                        {
                                            currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = currentOpponentData.CurrentBestLapTime;
                                            if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                                                currentGameState.SessionData.OverallSessionBestLapTime > currentOpponentData.CurrentBestLapTime)
                                            {
                                                currentGameState.SessionData.OverallSessionBestLapTime = currentOpponentData.CurrentBestLapTime;
                                            }
                                        }
                                        if (CarData.IsCarClassEqual(currentOpponentData.CarClass, currentGameState.carClass))
                                        {
                                            if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass == -1 ||
                                                currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass)
                                            {
                                                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = currentOpponentData.CurrentBestLapTime;
                                                if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                                                    currentGameState.SessionData.PlayerClassSessionBestLapTime > currentOpponentData.CurrentBestLapTime)
                                                {
                                                    currentGameState.SessionData.PlayerClassSessionBestLapTime = currentOpponentData.CurrentBestLapTime;
                                                }
                                            }
                                        }
                                    }

                                    currentOpponentData.DeltaTime.SetNextDeltaPoint(currentOpponentLapDistance, currentOpponentData.CompletedLaps,
                                        currentOpponentData.Speed, currentGameState.Now);
                                }
                            }
                            else
                            {
                                currentGameState.OpponentData.Remove(participantName);
                            }
                        }
                        else
                        {
                            if (participantStruct.mIsActive && participantName != null && participantName.Length > 0)
                            {
                                addOpponentForName(participantName, createOpponentData(participantStruct, true, CarData.getCarClassForClassName(StructHelper.getCarClassName(shared, i)),
                                    participantStruct.mName != null && participantStruct.mName[0] != 0, currentGameState.SessionData.TrackDefinition.trackLength), currentGameState);
                            }
                        }
                    }
                }
            }

            if (currentGameState.Now > nextOpponentCleanupTime)
            {
                nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
                DateTime oldestAllowedUpdate = currentGameState.Now - opponentCleanupInterval;
                List<string> inactiveOpponents = new List<string>();
                foreach (string opponentName in currentGameState.OpponentData.Keys)
                {
                    DateTime lastTimeActive = DateTime.MinValue;
                    if (!lastActiveTimeForOpponents.TryGetValue(opponentName, out lastTimeActive) || lastTimeActive < oldestAllowedUpdate)
                    {
                        inactiveOpponents.Add(opponentName);
                        Console.WriteLine("Opponent " + opponentName + " has been inactive for " + opponentCleanupInterval + ", removing him");
                    }
                }
                foreach (String inactiveOpponent in inactiveOpponents)
                {
                    currentGameState.OpponentData.Remove(inactiveOpponent);
                }
            }

            currentGameState.sortClassPositions();
            currentGameState.setPracOrQualiDeltas();

            if (currentGameState.PitData.InPitlane)
            {
                if (previousGameState != null && !previousGameState.PitData.InPitlane)
                {
                    if (currentGameState.SessionData.SessionRunningTime > 30 && currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        currentGameState.PitData.NumPitStops++;
                    }
                    currentGameState.PitData.OnInLap = true;
                    currentGameState.PitData.OnOutLap = false;
                }
                else if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.PitData.OnInLap = false;
                    currentGameState.PitData.OnOutLap = true;
                }
            }
            else if (previousGameState != null && previousGameState.PitData.InPitlane)
            {
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = true;
                currentGameState.PitData.IsAtPitExit = true;
            }
            else if (currentGameState.SessionData.IsNewLap)
            {
                // starting a new lap while not in the pitlane so clear the in / out lap flags
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = false;
            }

            ePitSchedule pitShedule = (ePitSchedule)shared.mPitSchedule;
            if (pitShedule == ePitSchedule.PIT_SCHEDULE_NONE)
            {
                currentGameState.PitData.HasRequestedPitStop = false;
                currentGameState.PitData.PitStallOccupied = false;
                currentGameState.PitData.IsPitCrewReady = false;
            }
            else if (pitShedule == ePitSchedule.PIT_SCHEDULE_PLAYER_REQUESTED)
            {
                currentGameState.PitData.HasRequestedPitStop = true;
                currentGameState.PitData.IsPitCrewReady = true;
                currentGameState.PitData.PitStallOccupied = false;
            }
            else if (pitShedule == ePitSchedule.PIT_SCHEDULE_PITSPOT_OCCUPIED)
            {
                currentGameState.PitData.PitStallOccupied = true;
                currentGameState.PitData.IsPitCrewReady = false;
            }
            
            currentGameState.PitData.IsPitCrewDone = currentGameState.SessionData.SessionType == SessionType.Race && 
                shared.mPitMode == (uint)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS &&
                previousGameState != null && previousGameState.PositionAndMotionData.CarSpeed < 1;    // don't allow the 'go go go' message unless we were actually stopped
            if (currentGameState.PitData.IsPitCrewDone)
            {
                currentGameState.PitData.IsPitCrewReady = false;
            }
            
            currentGameState.PitData.IsApproachingPitlane = pitMode == ePitMode.PIT_MODE_DRIVING_INTO_PITS;

            if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                if (shared.mEnforcedPitStopLap > 0)
                {
                    currentGameState.PitData.HasMandatoryPitStop = true;
                    currentGameState.PitData.PitWindowStart = (int)shared.mEnforcedPitStopLap;

                    // estimate the pit window close lap / time
                    if (currentGameState.SessionData.SessionHasFixedTime)
                    {
                        currentGameState.PitData.PitWindowEnd = (int)((currentGameState.SessionData.SessionTotalRunTime - 60f) / 60f);
                    }
                    else
                    {
                        currentGameState.PitData.PitWindowEnd = currentGameState.SessionData.SessionNumberOfLaps - 1;
                    }
                    currentGameState.PitData.PitWindow = mapToPitWindow(currentGameState, pitShedule, pitMode);
                    currentGameState.PitData.IsMakingMandatoryPitStop = (currentGameState.PitData.PitWindow == PitWindow.Open || currentGameState.PitData.PitWindow == PitWindow.StopInProgress) &&
                            (currentGameState.PitData.OnInLap || currentGameState.PitData.OnOutLap);
                    // do we need to move this out of the shared.mEnforcedPitStopLap > 0 check, as the lap may be reset to 0 after the stop is completed?
                    if (previousGameState != null)
                    {
                        currentGameState.PitData.MandatoryPitStopCompleted = previousGameState.PitData.MandatoryPitStopCompleted || currentGameState.PitData.IsMakingMandatoryPitStop;
                    }
                }
                else
                {
                    // if the enforcedPitStopLap is < 0, assume it's completed - no way of knowing whether this means the stop is complete, or there was no stop in the first place
                    currentGameState.PitData.MandatoryPitStopCompleted = true;
                    currentGameState.PitData.PitWindow = PitWindow.Completed;
                }
            }

            currentGameState.CarDamageData.DamageEnabled = true;    // no way to tell if it's disabled from the shared memory
            currentGameState.CarDamageData.OverallAeroDamage = mapToAeroDamageLevel(shared.mAeroDamage);
            currentGameState.CarDamageData.OverallEngineDamage = mapToEngineDamageLevel(shared.mEngineDamage);
            currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.NONE;
            currentGameState.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(suspensionDamageThresholds,
                shared.mSuspensionDamage[0], shared.mSuspensionDamage[1], shared.mSuspensionDamage[2], shared.mSuspensionDamage[3]);
            currentGameState.CarDamageData.BrakeDamageStatus = CornerData.getCornerData(brakeDamageThresholds,
                shared.mBrakeDamage[0], shared.mBrakeDamage[1], shared.mBrakeDamage[2], shared.mBrakeDamage[3]);

            currentGameState.EngineData.EngineOilPressure = shared.mOilPressureKPa;
            currentGameState.EngineData.EngineOilTemp = shared.mOilTempCelsius;
            currentGameState.EngineData.EngineWaterTemp = shared.mWaterTempCelsius;
            currentGameState.EngineData.MaxEngineRpm = shared.mMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 2;

            currentGameState.FuelData.FuelCapacity = shared.mFuelCapacity;
            currentGameState.FuelData.FuelLeft = currentGameState.FuelData.FuelCapacity * shared.mFuelLevel;
            currentGameState.FuelData.FuelPressure = shared.mFuelPressureKPa;
            currentGameState.FuelData.FuelUseActive = true;         // no way to tell if it's disabled

            currentGameState.PenaltiesData.HasDriveThrough = pitShedule == ePitSchedule.PIT_SCHEDULE_DRIVE_THROUGH;
            currentGameState.PenaltiesData.HasStopAndGo = pitShedule == ePitSchedule.PIT_SCHEDULE_STOP_GO;
            
            currentGameState.PositionAndMotionData.CarSpeed = shared.mSpeed;

            currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, 
                currentGameState.SessionData.CompletedLaps, shared.mSpeed, currentGameState.Now);

            //------------------------ Tyre data -----------------------          
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            // TODO: unmatched tyres
            currentGameState.TyreData.TyreWearActive = true;

            // only map to tyre type every sector or on pit exit
            TyreType tyreType;
            if (previousGameState == null || currentGameState.SessionData.IsNewSector || currentGameState.PitData.IsAtPitExit || currentGameState.SessionData.JustGoneGreen)
            {
                tyreType = mapToTyreType(shared.mLFTyreCompoundName);
            }
            else
            {
                tyreType = previousGameState.TyreData.FrontLeftTyreType;
            }

            currentGameState.TyreData.LeftFrontAttached = (shared.mTyreFlags[0] & 1) == 1;
            currentGameState.TyreData.RightFrontAttached = (shared.mTyreFlags[1] & 1) == 1;
            currentGameState.TyreData.LeftRearAttached = (shared.mTyreFlags[2] & 1) == 1;
            currentGameState.TyreData.RightRearAttached = (shared.mTyreFlags[3] & 1) == 1;

            currentGameState.TyreData.FrontLeft_CenterTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeftTyreType = tyreType;
            currentGameState.TyreData.FrontLeftPressure = -1; // not in the block
            currentGameState.TyreData.FrontLeftPercentWear = Math.Min(100, shared.mTyreWear[0] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontLeftTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = currentGameState.TyreData.FrontLeft_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.FrontLeft_CenterTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = currentGameState.TyreData.FrontLeft_CenterTemp;
            }

            currentGameState.TyreData.FrontRight_CenterTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRight_RightTemp = shared.mTyreTreadTemp[1] - 273;
            currentGameState.TyreData.FrontRightTyreType = tyreType;
            currentGameState.TyreData.FrontRightPressure = -1; // not in the block
            currentGameState.TyreData.FrontRightPercentWear = Math.Min(100, shared.mTyreWear[1] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontRightTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = currentGameState.TyreData.FrontRight_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.FrontRight_CenterTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = currentGameState.TyreData.FrontRight_CenterTemp;
            }

            currentGameState.TyreData.RearLeft_CenterTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeft_RightTemp = shared.mTyreTreadTemp[2] - 273;
            currentGameState.TyreData.RearLeftTyreType = tyreType;
            currentGameState.TyreData.RearLeftPressure = -1; // not in the block
            currentGameState.TyreData.RearLeftPercentWear = Math.Min(100, shared.mTyreWear[2] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearLeftTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = currentGameState.TyreData.RearLeft_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.RearLeft_CenterTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = currentGameState.TyreData.RearLeft_CenterTemp;
            }

            currentGameState.TyreData.RearRight_CenterTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_LeftTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRight_RightTemp = shared.mTyreTreadTemp[3] - 273;
            currentGameState.TyreData.RearRightTyreType = tyreType;
            currentGameState.TyreData.RearRightPressure = -1; // not in the block
            currentGameState.TyreData.RearRightPercentWear = Math.Min(100, shared.mTyreWear[3] * 100 / wornOutTyreWearLevel);
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearRightTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = currentGameState.TyreData.RearRight_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.RearRight_CenterTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = currentGameState.TyreData.RearRight_CenterTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear, 
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);


            var tyreTempThresholds = CarData.getTyreTempThresholds(currentGameState.carClass, tyreType);
            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(tyreTempThresholds,
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap, currentGameState.TyreData.PeakFrontRightTemperatureForLap,
                currentGameState.TyreData.PeakRearLeftTemperatureForLap, currentGameState.TyreData.PeakRearRightTemperatureForLap);

            currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar, 
                shared.mBrakeTempCelsius[0], shared.mBrakeTempCelsius[1], shared.mBrakeTempCelsius[2], shared.mBrakeTempCelsius[3]);
            currentGameState.TyreData.LeftFrontBrakeTemp = shared.mBrakeTempCelsius[0];
            currentGameState.TyreData.RightFrontBrakeTemp = shared.mBrakeTempCelsius[1];
            currentGameState.TyreData.LeftRearBrakeTemp = shared.mBrakeTempCelsius[2];
            currentGameState.TyreData.RightRearBrakeTemp = shared.mBrakeTempCelsius[3];

            // improvised cut track warnings...
            if (incrementCutTrackCountWhenLeavingRacingSurface)
            {
                currentGameState.PenaltiesData.IsOffRacingSurface = !racingSurfaces.Contains(shared.mTerrain[0]) &&
               !racingSurfaces.Contains(shared.mTerrain[1]) && !racingSurfaces.Contains(shared.mTerrain[2]) &&
               !racingSurfaces.Contains(shared.mTerrain[3]);
                if (previousGameState != null && previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface)
                {
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
                }
            }
            if (!currentGameState.PitData.OnOutLap && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid && !currentGameState.SessionData.CurrentLapIsValid &&
                !(shared.mSessionState == (int)eSessionState.SESSION_RACE && shared.mRaceState == (int)eRaceState.RACESTATE_NOT_STARTED))
            {
                currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
            }
            // Tyre slip speed seems to peak at about 30 with big lock or wheelspin (in Sauber Merc). It's noisy as hell and is frequently bouncing around
            // in single figures, with the noise varying between cars.
            // tyreRPS is much cleaner but we don't know the diameter of the tyre so can't compare it (accurately) to the car's speed

            // disabled for go-karts
            if (shared.mSpeed > 7 && currentGameState.carClass != null &&
                currentGameState.carClass.carClassEnum != CarData.CarClassEnum.Kart_1 && currentGameState.carClass.carClassEnum != CarData.CarClassEnum.Kart_2)
            {
                float minRotatingSpeed = (float)Math.PI * shared.mSpeed / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.mTyreRPS[0]) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.mTyreRPS[1]) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.mTyreRPS[2]) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.mTyreRPS[3]) < minRotatingSpeed;

                float maxRotatingSpeed = 3 * (float)Math.PI * shared.mSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.mTyreRPS[0]) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.mTyreRPS[1]) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.mTyreRPS[2]) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.mTyreRPS[3]) > maxRotatingSpeed;
            }

            if (currentGameState.Now > nextConditionsSampleDue)
            {
                nextConditionsSampleDue = currentGameState.Now.Add(ConditionsMonitor.ConditionsSampleFrequency);
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.mAmbientTemperature, shared.mTrackTemperature, shared.mRainDensity, shared.mWindSpeed, shared.mWindDirectionX, shared.mWindDirectionY, shared.mCloudBrightness,
                    currentGameState.SessionData.IsNewLap);
            }
            currentGameState.CloudBrightness = shared.mCloudBrightness;
            currentGameState.RainDensity = shared.mRainDensity;

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

            if (currentGameState.PositionAndMotionData.DistanceRoundTrack > 0 && currentGameState.PositionAndMotionData.CarSpeed > 0 
                && !currentGameState.PitData.InPitlane && currentGameState.SessionData.CurrentLapIsValid)
            {

                eTerrainMaterials[] terrainMaterials = new eTerrainMaterials[] {(eTerrainMaterials)shared.mTerrain[0], 
                    (eTerrainMaterials)shared.mTerrain[1], (eTerrainMaterials)shared.mTerrain[2], (eTerrainMaterials)shared.mTerrain[3] };
                int illegalSurfacesCount = 0;
                foreach (eTerrainMaterials material in terrainMaterials)
                {
                    if (illegalSurfaces.Contains(material))
                    {
                        illegalSurfacesCount++;
                        if (illegalSurfacesCount > 2)
                        {
                            if (!loggedTrackLimitViolationOnThisLap)
                            {
                                Console.WriteLine("Track limit violation, lap " + (currentGameState.SessionData.CompletedLaps + 1) + " distance " +
                                    currentGameState.PositionAndMotionData.DistanceRoundTrack + " terrain " + String.Join(", ", terrainMaterials));
                                loggedTrackLimitViolationOnThisLap = true;
                            }
                            currentGameState.PenaltiesData.PossibleTrackLimitsViolation = true;
                            break;
                        }
                    }
                }
                if (illegalSurfacesCount == 2)
                {
                    int marginalSurfacesCount = 0;
                    foreach (eTerrainMaterials material in terrainMaterials)
                    {
                        if (marginalSurfaces.Contains(material))
                        {
                            marginalSurfacesCount++;
                        }
                    }
                    if (marginalSurfacesCount > 1)
                    {
                        if (!loggedPossibleTrackLimitViolationOnThisLap)
                        {
                            Console.WriteLine("Possible track limit violation, lap " + (currentGameState.SessionData.CompletedLaps + 1) + 
                                " sector " + currentGameState.SessionData.SectorNumber + " distance " +
                                currentGameState.PositionAndMotionData.DistanceRoundTrack + 
                                " laptime " + shared.mmfOnly_mCurrentTime + " terrain " + String.Join(", ", terrainMaterials));
                            loggedPossibleTrackLimitViolationOnThisLap = true;
                        }
                        // still not sure if these 'possible' violations (2 wheels out of track limits, 2 wheels on a rumble strip) will invalidate a lap
                        // currentGameState.PenaltiesData.PossibleTrackLimitsViolation = true;
                    }
                }
            }
            currentGameState.EngineData.EngineRpm = shared.mRpm;
            if (shared.mRpm > 5)
            {
                lastTimeEngineWasRunning = currentGameState.Now;
            }
            if (!currentGameState.PitData.InPitlane &&
                previousGameState != null && !previousGameState.EngineData.EngineStalledWarning &&
                currentGameState.SessionData.SessionRunningTime > 60 && currentGameState.EngineData.EngineRpm < 5 &&
                lastTimeEngineWasRunning < currentGameState.Now.Subtract(TimeSpan.FromSeconds(2)))
            {
                currentGameState.EngineData.EngineStalledWarning = true;
                lastTimeEngineWasRunning = DateTime.MaxValue;
            }
            
            currentGameState.ControlData.BrakePedal = shared.mBrake;
            currentGameState.ControlData.ThrottlePedal = shared.mThrottle;
            currentGameState.ControlData.ClutchPedal = shared.mClutch;

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

            return currentGameState;
        }

        private DamageLevel mapToAeroDamageLevel(float aeroDamage)
        {
            if (aeroDamage >= destroyedAeroDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            }
            else if (aeroDamage >= severeAeroDamageThreshold)
            {
                return DamageLevel.MAJOR;
            }
            else if (aeroDamage >= minorAeroDamageThreshold)
            {
                return DamageLevel.MINOR;
            }
            else if (aeroDamage >= trivialAeroDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            } 
            else
            {
                return DamageLevel.NONE;
            }
        }
        private DamageLevel mapToEngineDamageLevel(float engineDamage)
        {
            if (engineDamage >= destroyedEngineDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            }
            else if (engineDamage >= severeEngineDamageThreshold)
            {
                return DamageLevel.MAJOR;
            }
            else if (engineDamage >= minorEngineDamageThreshold)
            {
                return DamageLevel.MINOR;
            }
            else if (engineDamage >= trivialEngineDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            }
            else
            {
                return DamageLevel.NONE;
            }
        }

        private void updateOpponentData(OpponentData opponentData, int racePosition, int completedLaps, int sector, Boolean isEnteringPits,
            Boolean isInPits, Boolean isLeavingPits,
            float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition, float[] previousWorldPosition,
            float speed, float worldRecordLapTime, float worldRecordS1Time, float worldRecordS2Time, float worldRecordS3Time, 
            float distanceRoundTrack, Boolean isRaining, float trackTemp, float airTemp, 
            Boolean sessionLengthIsTime, float sessionTimeRemaining, float lastSectorTime, Boolean lapInvalidated, float nearPitEntryPointDistance,
            TimingData timingData, CarData.CarClass playerCarClass)
        {
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            opponentData.Speed = speed;
            opponentData.OverallPosition = racePosition;
            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.OverallPosition;
            }
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            opponentData.JustEnteredPits = !opponentData.InPits && (isInPits || isEnteringPits);
            if (opponentData.JustEnteredPits)
            {
                opponentData.NumPitStops++;
            }
            opponentData.InPits = isEnteringPits || isInPits || isLeavingPits;
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        if (lastSectorTime <= 0)
                        {
                            lastSectorTime = -1;
                            lapInvalidated = true;
                        }
                        opponentData.CompleteLapWithLastSectorTime(racePosition, lastSectorTime, sessionRunningTime, 
                            !lapInvalidated, isRaining, trackTemp, airTemp, sessionLengthIsTime, sessionTimeRemaining, 3, timingData, 
                            CarData.IsCarClassEqual(opponentData.CarClass, playerCarClass));
                        
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits || isLeavingPits, sessionRunningTime, isRaining, trackTemp, airTemp);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    if (lastSectorTime <= 0)
                    {
                        lastSectorTime = -1;
                        lapInvalidated = true;
                    }
                    opponentData.AddSectorData(opponentData.CurrentSectorNumber, racePosition, lastSectorTime, sessionRunningTime, !lapInvalidated, isRaining, trackTemp, airTemp);             
                }
                opponentData.CurrentSectorNumber = sector;
            }
            if (sector == 3 && isInPits || isEnteringPits) 
            {
                opponentData.setInLap();
            }
            opponentData.CompletedLaps = completedLaps;
        }

        private OpponentData createOpponentData(pCars2APIParticipantStruct participantStruct, Boolean loadDriverName, CarData.CarClass carClass, Boolean canUseName, float trackLength)
        {            
            OpponentData opponentData = new OpponentData();
            String participantName = StructHelper.getNameFromBytes(participantStruct.mName).ToLower();
            opponentData.DriverRawName = participantName;
            opponentData.DriverNameSet = true;
            if (participantName != null && participantName.Length > 0 && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }
            opponentData.OverallPosition = (int)participantStruct.mRacePosition;
            opponentData.CompletedLaps = (int)participantStruct.mLapsCompleted;
            opponentData.CurrentSectorNumber = (int)participantStruct.mCurrentSector + 1;   // zero indexed
            opponentData.WorldPosition = new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] };
            opponentData.DistanceRoundTrack = participantStruct.mCurrentLapDistance;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.UtcNow);
            opponentData.CarClass = carClass;
            opponentData.IsActive = true;
            String nameToLog = opponentData.DriverRawName == null ? "unknown" : opponentData.DriverRawName;
            Console.WriteLine("New driver " + nameToLog + " is using car class " + opponentData.CarClass.carClassEnum);
            opponentData.CanUseName = canUseName;

            return opponentData;
        }
        
        /*
         * Race state changes - start race, skip practice to end of session, then into race:
         * 
         * pre race practice initial - sessionState = SESSION_TEST, raceState = not started 
         * pre race practice after pit exit - sessionState = SESSION_TEST, raceState = racing
         * skip to end - sessionState = SESSION_TEST, raceState = not started 
         * load race - sessionState = NO_SESSION, raceState = not started 
         * grid walk - sessionState = SESSION_RACE, raceState = racing
         * 
         * TODO: other session types. The "SESSION_TEST" above is actually the warmup. Presumably
         * an event with prac -> qual -> warmup -> race would use SESSION_PRACTICE
         * */
        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            pCars2APIStruct shared = (pCars2APIStruct)memoryMappedFileStruct;
            uint sessionState = shared.mSessionState;
            if (sessionState == (uint)eSessionState.SESSION_RACE || sessionState == (uint)eSessionState.SESSION_FORMATION_LAP)
            {
                return SessionType.Race;
            }
            else if (sessionState == (uint)eSessionState.SESSION_PRACTICE || sessionState == (uint)eSessionState.SESSION_TEST)
            {
                return SessionType.Practice;
            } 
            else if (sessionState == (uint)eSessionState.SESSION_QUALIFY)
            {
                return SessionType.Qualify;
            }
            else if (sessionState == (uint)eSessionState.SESSION_TIME_ATTACK)
            {
                return SessionType.HotLap;
            }
            else
            {
                return SessionType.Unavailable;
            }
        }

        /*
         * When a practice session ends (while driving around) the mSessionState goes from 2 (racing) to
         * 1 (race not started) to 0 (invalid) over a few seconds. It never goes to any of the finished
         * states
         * 
         * 
         * 27-02-16
         * For timed sessions using SharedMemory, seesionTimeRemaining is Float.MaxValue in the pre-race phase, until the actual count down (lights cycle)
         * starts. Then it is set to the correct time.
         * 
         * For UDP it's similar, but it appears that the initial 'sessionTimeRemaining' gets set to half the correct value, then Float.MaxValue
         * 
         * When we retire to the pit box, the raceState is set to RaceNotStarted
         */
        private SessionPhase mapToSessionPhase(SessionType sessionType, uint sessionState, uint raceState, int numParticipants, Boolean leaderHasFinishedRace, 
            SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime, uint pitMode, eGameState gameState,
            Dictionary<string, OpponentData> opponentData, float playerSpeed, DateTime now)
        {
            if (numParticipants < 1)
            {
                return SessionPhase.Unavailable;
            }
            if (sessionType == SessionType.Race)
            {
                if (raceState == (uint)eRaceState.RACESTATE_NOT_STARTED)
                {
                    if (sessionState == (uint)eSessionState.SESSION_FORMATION_LAP)
                    {
                        return SessionPhase.Formation;
                    }
                    else if (gameState == eGameState.GAME_INGAME_INMENU_TIME_TICKING)
                    {
                        return SessionPhase.Gridwalk;
                    }
                    else if (pitMode != (uint) ePitMode.PIT_MODE_IN_GARAGE)
                    {
                        return SessionPhase.Countdown;
                    }
                    else
                    {
                        return previousSessionPhase;
                    }
                }
                else if (raceState == (uint)eRaceState.RACESTATE_RACING)
                {
                    if (leaderHasFinishedRace)
                    {
                        return SessionPhase.Checkered;
                    }
                    else
                    {
                        return SessionPhase.Green;
                    }
                }
                else if (raceState == (uint)eRaceState.RACESTATE_FINISHED ||
                    raceState == (uint)eRaceState.RACESTATE_DNF ||
                    raceState == (uint)eRaceState.RACESTATE_DISQUALIFIED ||
                    raceState == (uint)eRaceState.RACESTATE_RETIRED ||
                    raceState == (uint)eRaceState.RACESTATE_INVALID ||
                    raceState == (uint)eRaceState.RACESTATE_MAX)
                {
                    return SessionPhase.Finished;
                }
            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.Qualify || sessionType == SessionType.HotLap)
            {
                if (sessionRunTime > 0 && sessionTimeRemaining <= 0.2)
                {
                    if (previousSessionPhase == SessionPhase.Finished)
                    {
                        return SessionPhase.Finished;
                    }
                    SessionPhase currentPhase = SessionPhase.Checkered;
                    if (previousSessionPhase == SessionPhase.Green)
                    {
                        waitingForCarsToFinish.Clear();
                        nextDebugCheckeredToFinishMessageTime = DateTime.MinValue;
                    }
                    if (playerSpeed < 1)
                    {
                        // the player isn't driving, so check the opponents
                        int waitingForCount = 0;
                        if (opponentData != null)
                        {
                            foreach (OpponentData opponent in opponentData.Values)
                            {
                                Boolean running = false;
                                float distRoundLap = -1.0f;
                                if (!waitingForCarsToFinish.TryGetValue(opponent.DriverRawName, out distRoundLap)) {
                                    waitingForCarsToFinish.Add(opponent.DriverRawName, opponent.DistanceRoundTrack);
                                    running = true;
                                }
                                else if (distRoundLap < opponent.DistanceRoundTrack)
                                {
                                    waitingForCarsToFinish[opponent.DriverRawName] = opponent.DistanceRoundTrack;
                                    running = true;
                                }
                                else
                                {
                                    waitingForCarsToFinish[opponent.DriverRawName] = float.MaxValue;
                                }
                                if (running) 
                                {
                                    waitingForCount++;
                                }
                            }
                        }
                        if (waitingForCount == 0)
                        {
                            Console.WriteLine("Looks like session is finished - no activity in checkered phase");
                            currentPhase = SessionPhase.Finished;
                        }
                        else if (now > nextDebugCheckeredToFinishMessageTime)
                        {
                            Console.WriteLine("Session has finished but there are " + waitingForCount + " cars still out on track");
                            nextDebugCheckeredToFinishMessageTime = now.Add(TimeSpan.FromSeconds(10));
                        }
                    }
                    return currentPhase;
                }
                else if (previousSessionPhase != SessionPhase.Checkered && previousSessionPhase != SessionPhase.Finished &&
                    (raceState == (uint)eRaceState.RACESTATE_RACING || raceState == (uint)eRaceState.RACESTATE_NOT_STARTED))
                {
                    // session state is 'countdown' until the player has started his first flying lap
                    return SessionPhase.Green;
                }
            }
            Console.WriteLine("Unavailable");
            return SessionPhase.Unavailable;
        }

        private TyreCondition getTyreCondition(float percentWear)
        {
            if (percentWear <= -1)
            {
                return TyreCondition.UNKNOWN;
            }
            if (percentWear >= wornOutTyreWearPercent)
            {
                return TyreCondition.WORN_OUT;
            }
            else if (percentWear >= majorTyreWearPercent)
            {
                return TyreCondition.MAJOR_WEAR;
            }
            if (percentWear >= minorTyreWearPercent)
            {
                return TyreCondition.MINOR_WEAR;
            }
            if (percentWear >= scrubbedTyreWearPercent)
            {
                return TyreCondition.SCRUBBED;
            }
            else
            {
                return TyreCondition.NEW;
            }
        }

        private FlagEnum mapToFlagEnum(uint highestFlagColour)
        {
            if (highestFlagColour == (uint) eFlagColour.FLAG_COLOUR_CHEQUERED)
            {
                return FlagEnum.CHEQUERED;
            }
            else if (highestFlagColour == (uint)eFlagColour.FLAG_COLOUR_BLACK) 
            {
                return FlagEnum.BLACK;
            }
            else if (highestFlagColour == (uint)eFlagColour.FLAG_COLOUR_DOUBLE_YELLOW) 
            {
                return FlagEnum.DOUBLE_YELLOW;
            }
            else if (highestFlagColour == (uint)eFlagColour.FLAG_COLOUR_YELLOW) 
            {
                return FlagEnum.YELLOW;
            }
            else if (highestFlagColour == (uint)eFlagColour.FLAG_COLOUR_WHITE_SLOW_CAR) 
            {
                return FlagEnum.WHITE;
            }
            else if (highestFlagColour == (uint)eFlagColour.FLAG_COLOUR_BLUE) 
            {
                return FlagEnum.BLUE;
            }
            else if (highestFlagColour == (uint)eFlagColour.FLAG_COLOUR_GREEN) 
            {
                return FlagEnum.GREEN;
            }
            return FlagEnum.UNKNOWN;
        }

        private PitWindow mapToPitWindow(GameStateData currentGameState, ePitSchedule pitSchedule, ePitMode pitMode)
        {
            // if we've already completed our stop, just return completed here
            if (currentGameState.PitData.PitWindow == PitWindow.Completed)
            {
                return PitWindow.Completed;
            }
            if (currentGameState.PitData.PitWindowStart > 0)
            {
                if ((currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.CompletedLaps < currentGameState.PitData.PitWindowStart) ||
                    (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionRunningTime < currentGameState.PitData.PitWindowStart * 60) ||
                    (currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.CompletedLaps > currentGameState.PitData.PitWindowEnd) ||
                    (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionRunningTime > currentGameState.PitData.PitWindowEnd * 60))
                {
                    return PitWindow.Closed;
                }
                else if ((currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.CompletedLaps >= currentGameState.PitData.PitWindowStart) ||
                    (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionRunningTime >= currentGameState.PitData.PitWindowStart * 60))
                {
                    if (currentGameState.PitData.PitWindow == PitWindow.StopInProgress && 
                        (pitMode == ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS || pitMode == ePitMode.PIT_MODE_DRIVING_OUT_OF_GARAGE))
                    {
                        return PitWindow.Completed;
                    }
                    else if (pitMode == ePitMode.PIT_MODE_DRIVING_INTO_PITS || pitMode == ePitMode.PIT_MODE_IN_PIT || pitMode ==  ePitMode.PIT_MODE_IN_GARAGE)
                    {
                        return PitWindow.StopInProgress;
                    }
                    else
                    {
                        return PitWindow.Open;
                    }
                }
            }
            return PitWindow.Unavailable;
        }

        private float getMean(List<float> data)
        {
            if (data.Count == 0)
            {
                return 0;
            }
            float mean = 0;
            int count = 0;
            foreach (float d in data)
            {
                count++;
                mean += d;
            }
            return mean / count;
        }

        private TyreType mapToTyreType(byte[] lfTyreName)
        {
            if (lfTyreName != null && lfTyreName.Length > 0 && lfTyreName[0] != (byte)0)
            {
                String tyreName = StructHelper.getNameFromBytes(lfTyreName).ToLower();
                if (tyreName.Contains("wet"))
                {
                    return TyreType.Wet;
                }
                else if (tyreName.Contains("hard"))
                {
                    return TyreType.Hard;
                }
                else if (tyreName.Contains("medium"))
                {
                    return TyreType.Medium;
                }
                else if (tyreName.Contains("soft"))
                {
                    return TyreType.Soft;
                }
                else if (tyreName.Contains("inter"))
                {
                    return TyreType.Intermediate;
                }
                else if (tyreName.Contains("road") || tyreName.Contains("street"))
                {
                    return TyreType.Road;
                }
                else if (tyreName.Contains("ice"))
                {
                    return TyreType.Ice;
                }
                else if (tyreName.Contains("snow"))
                {
                    return TyreType.Snow;
                }
                else if (tyreName.Contains("terrain"))
                {
                    return TyreType.AllTerrain;
                }
                else if (tyreName.Contains("bias") || tyreName.Contains("vintage")) 
                {
                    return TyreType.Bias_Ply;
                }
                else if (tyreName.Contains("dry") || tyreName.Contains("track"))
                {
                    // no idea what these are - they're fitted to older cars, so lets assume they're bias ply
                    return TyreType.Bias_Ply;
                }
            }
            return defaultTyreTypeForPlayersCar;            
        }
    }
}
