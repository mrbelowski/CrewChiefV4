using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using System.Diagnostics;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 * 
 * 
 * Weather...
 * 
 * cloud brightness varies a *lot*. Perhaps it's for that section of track?
 */
namespace CrewChiefV4.PCars
{
    class PCarsGameStateMapper : GameStateMapper
    {
        public static String NULL_CHAR_STAND_IN = "?";


        public static int FIRST_VIEWED_PARTICIPANT_INDEX = -1;
        public static String FIRST_VIEWED_PARTICIPANT_NAME = null;
        public static Boolean WARNED_ABOUT_MISSING_STEAM_ID = false;

        private Boolean attemptPitDetection = UserSettings.GetUserSettings().getBoolean("attempt_pcars_opponent_pit_detection");
        private Boolean enablePCarsPitWindowStuff = UserSettings.GetUserSettings().getBoolean("enable_pcars_pit_window_messages");
        private static String userSpecifiedSteamId = UserSettings.GetUserSettings().getString("pcars_steam_id");
        private static String playerSteamId = null;
        private static Boolean getPlayerByName = true;

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;
        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(new CarData.CarClass());

        // if all 4 wheels are off the racing surface, increment the number of cut track incedents
        private Boolean incrementCutTrackCountWhenLeavingRacingSurface = false;

        private static uint expectedVersion = 5;

        private List<uint> racingSurfaces = new List<uint>() { (uint)eTerrain.TERRAIN_BUMPY_DIRT_ROAD, 
            (uint)eTerrain.TERRAIN_BUMPY_ROAD1, (uint)eTerrain.TERRAIN_BUMPY_ROAD2, (uint)eTerrain.TERRAIN_BUMPY_ROAD3, 
            (uint)eTerrain.TERRAIN_COBBLES, (uint)eTerrain.TERRAIN_DRAINS, (uint)eTerrain.TERRAIN_EXIT_RUMBLE_STRIPS,
            (uint)eTerrain.TERRAIN_LOW_GRIP_ROAD, (uint)eTerrain.TERRAIN_MARBLES,(uint)eTerrain.TERRAIN_PAVEMENT,
            (uint)eTerrain.TERRAIN_ROAD, (uint)eTerrain.TERRAIN_RUMBLE_STRIPS, (uint)eTerrain.TERRAIN_SAND_ROAD};

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

        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private Dictionary<string, List<float>> opponentSpeedsWindow = new Dictionary<string, List<float>>();

        private int opponentSpeedsToAverage = 20;

        private Dictionary<string, float> waitingForCarsToFinish = new Dictionary<string, float>();
        private DateTime nextDebugCheckeredToFinishMessageTime = DateTime.MinValue;

        Dictionary<string, DateTime> lastActiveTimeForOpponents = new Dictionary<string, DateTime>();
        DateTime nextOpponentCleanupTime = DateTime.MinValue;
        TimeSpan opponentCleanupInterval = TimeSpan.FromSeconds(2);

        // next track conditions sample due after:
        private DateTime nextConditionsSampleDue = DateTime.MinValue;

        public PCarsGameStateMapper()
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
            pCarsAPIStruct shared = ((CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).data;
            uint currentVersion = shared.mVersion;
            if (currentVersion != expectedVersion)
            {
                throw new GameDataReadException("Expected shared data version " + expectedVersion + " but got version " + currentVersion);
            }
        }

        /**
         * When a participant is no longer part of a session sometimes he's removed from the array. Sometimes the first char of his name
         * is nullified. When a participant has a name with one or more chars not in the default codepage we also get a null first char, and
         * any chars including and after the offending char will be nonsense (whatever as in the array before this guy joined). This method
         * attempts to make sense of the *complete fucking clusterfuck*.
         * @param participantData
         * @param numParticipants
        */
        private void checkForPossiblyInactiveOpponents(pCarsAPIParticipantStruct[] participantData, int numParticipants) {
            ISet<int> positions = new HashSet<int>();
            // get a set of all the race positions which we're confident are occupied by actual opponents
            for (int i=0; i<numParticipants; i++) {
                pCarsAPIParticipantStruct participant = participantData[i];
                if (participant.mIsActive && participant.mName != null && participant.mName[0] != 0) {
                    // there can be duplicates here when the session first starts up
                    positions.Add((int)participant.mRacePosition);
                }
            }
            // now get the hooky opponents and set any which are in the same position as a proper opponent to inactive
            for (int i=0; i<numParticipants; i++) {
                pCarsAPIParticipantStruct participant = participantData[i];
                if (participant.mIsActive && participant.mName != null && participant.mName[0] == 0)
                {
                    if (positions.Contains((int)participant.mRacePosition)) {
                        participant.mIsActive = false;
                    } else {
                        // his name is potentially full of shit - possibly a pronouncable name left over from the previous session
                        positions.Add((int)participant.mRacePosition);
                    }
                }
            }
        }

        public static Boolean namesMatch(String name1, String name2)
        {
            if (name1 != null && name1.Length > 0 && name2 != null && name2.Length > 0)
            {
                if (name1.StartsWith(NULL_CHAR_STAND_IN) && name2.StartsWith((NULL_CHAR_STAND_IN)))
                {
                    return name1.Equals(name2);
                }
                else if (name1.StartsWith(NULL_CHAR_STAND_IN) || name2.StartsWith(NULL_CHAR_STAND_IN))
                {
                    return name1.Length > 1 && name2.Length > 1 && name1.Substring(1).Equals(name2.Substring(1));
                }
                else
                {
                    return name1.Equals(name2);
                }
            }
            return false;
        }

        public static Tuple<int, pCarsAPIParticipantStruct> getPlayerDataStruct(pCarsAPIParticipantStruct[] pCarsAPIParticipantStructArray, int viewedParticipantIndex)
        {
            // sanity check
            if (viewedParticipantIndex >= pCarsAPIParticipantStructArray.Length)
            {
                viewedParticipantIndex = 0;
            }
            Boolean haveDriverNames = StructHelper.getNameFromBytes(pCarsAPIParticipantStructArray[0].mName).Length > 0;
            pCarsAPIParticipantStruct viewedParticipant = pCarsAPIParticipantStructArray[viewedParticipantIndex];

            if (haveDriverNames && userSpecifiedSteamId != null && userSpecifiedSteamId.Length > 0)
            {
                if (namesMatch(StructHelper.getNameFromBytes(viewedParticipant.mName), userSpecifiedSteamId))
                {
                    return new Tuple<int, pCarsAPIParticipantStruct>(viewedParticipantIndex, viewedParticipant);
                }
                else
                {
                    for (int i = 0; i < pCarsAPIParticipantStructArray.Length; i++)
                    {
                        pCarsAPIParticipantStruct participant = pCarsAPIParticipantStructArray[i];
                        if (participant.mIsActive && namesMatch(StructHelper.getNameFromBytes(participant.mName), userSpecifiedSteamId))
                        {
                            return new Tuple<int, pCarsAPIParticipantStruct>(i, participant);
                        }
                    }
                    // this will spam so only send it once per session
                    if (!WARNED_ABOUT_MISSING_STEAM_ID)
                    {
                        Console.WriteLine("User specified steam ID " +
                                userSpecifiedSteamId + " not found in active participant data, getting player data based on provided index");
                        WARNED_ABOUT_MISSING_STEAM_ID = true;
                    }
                }
            }

            // FIRST_VIEWED_PARTICIPANT_NAME will be null in many cases, FIRST_VIEWED_PARTICIPANT_INDEX will generally be set
            if (FIRST_VIEWED_PARTICIPANT_INDEX >= 0 && FIRST_VIEWED_PARTICIPANT_INDEX < pCarsAPIParticipantStructArray.Length)
            {
                pCarsAPIParticipantStruct guess1 = pCarsAPIParticipantStructArray[FIRST_VIEWED_PARTICIPANT_INDEX];
                // only interested in 'guess1' if he's active
                Boolean guess1IsActive = guess1.mIsActive;
                String guess1Name = StructHelper.getNameFromBytes(guess1.mName);
                if (haveDriverNames)
                {
                    if (FIRST_VIEWED_PARTICIPANT_NAME != null)
                    {
                        if (namesMatch(guess1Name, FIRST_VIEWED_PARTICIPANT_NAME) && guess1IsActive)
                        {
                            return new Tuple<int, pCarsAPIParticipantStruct>(FIRST_VIEWED_PARTICIPANT_INDEX, guess1);
                        }
                        else
                        {
                            for (int i = 0; i < pCarsAPIParticipantStructArray.Length; i++)
                            {
                                pCarsAPIParticipantStruct participant = pCarsAPIParticipantStructArray[i];
                                if (participant.mIsActive && namesMatch(StructHelper.getNameFromBytes(participant.mName), FIRST_VIEWED_PARTICIPANT_NAME))
                                {
                                    return new Tuple<int, pCarsAPIParticipantStruct>(i, participant);
                                }
                            }
                        }
                    }
                    else if (guess1IsActive)
                    {
                        // set the first viewed name
                        FIRST_VIEWED_PARTICIPANT_NAME = guess1Name;
                    }
                }
                if (guess1IsActive)
                {
                    return new Tuple<int, pCarsAPIParticipantStruct>(FIRST_VIEWED_PARTICIPANT_INDEX, guess1);
                }
            }
            return new Tuple<int, pCarsAPIParticipantStruct>(viewedParticipantIndex, pCarsAPIParticipantStructArray[viewedParticipantIndex]);
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

        public static OpponentData getOpponentForName(GameStateData gameState, String nameToFind)
        {
            if (gameState.OpponentData == null || gameState.OpponentData.Count == 0 || nameToFind == null || nameToFind.Length == 0)
            {
                return null;
            }
            String nameToFindWithNoFirstChar = nameToFind.Substring(1);
            if (nameToFind.StartsWith(NULL_CHAR_STAND_IN))
            {
                // oh dear, the game has decided not to send us the first character of the name                
                foreach (String name in gameState.OpponentData.Keys)
                {
                    if (name.Substring(1).Equals(nameToFindWithNoFirstChar))
                    {
                        return gameState.OpponentData[name];
                    }
                }
            }
            else
            {
                OpponentData opponent = null;
                if (gameState.OpponentData.TryGetValue(nameToFind, out opponent))
                {
                    return opponent;
                }
                else
                {
                    foreach (String name in gameState.OpponentData.Keys)
                    {
                        if (name.StartsWith(NULL_CHAR_STAND_IN) && name.Substring(1).Equals(nameToFindWithNoFirstChar))
                        {
                            return gameState.OpponentData[name];
                        }
                    }
                }
            }
            return null;
        }

        private void setCurrentParticipant(pCarsAPIStruct shared)
        {
            if (FIRST_VIEWED_PARTICIPANT_INDEX == -1)
            {
                FIRST_VIEWED_PARTICIPANT_INDEX = shared.mViewedParticipantIndex;
                String thisName = StructHelper.getNameFromBytes(shared.mParticipantData[shared.mViewedParticipantIndex].mName);
                if (thisName != null && thisName.Length > 0)
                {
                    FIRST_VIEWED_PARTICIPANT_NAME = thisName;
                }
            }
        }

        public override GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            pCarsAPIStruct shared = ((CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).data;
            long ticks = ((CrewChiefV4.PCars.PCarsSharedMemoryReader.PCarsStructWrapper)memoryMappedFileStruct).ticksWhenRead;

            if (shared.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY)
            {
                CrewChief.trackName = StructHelper.getNameFromBytes(shared.mTrackLocation) + ":" + StructHelper.getNameFromBytes(shared.mTrackVariation);
                CrewChief.carClass = CarData.getCarClassForClassName(StructHelper.getNameFromBytes(shared.mCarClassName)).carClassEnum;
                CrewChief.viewingReplay = true;
                CrewChief.distanceRoundTrack = shared.mParticipantData[shared.mViewedParticipantIndex].mCurrentLapDistance;
            }

            // game state is 3 for paused, 5 for replay. No idea what 4 is...
            if (shared.mGameState == (uint)eGameState.GAME_FRONT_END ||
                shared.mGameState == (uint)eGameState.GAME_INGAME_PAUSED || 
                shared.mGameState == (uint)eGameState.GAME_VIEWING_REPLAY || 
                shared.mGameState == (uint)eGameState.GAME_EXITED ||
                shared.mNumParticipants < 1 || shared.mTrackLength <= 0)
            {
                if (shared.mGameState == (uint)eGameState.GAME_FRONT_END && previousGameState != null)
                {
                    previousGameState.SessionData.SessionType = SessionType.Unavailable;
                    previousGameState.SessionData.SessionPhase = SessionPhase.Unavailable;
                }
                return previousGameState;
            }
            checkForPossiblyInactiveOpponents(shared.mParticipantData, shared.mNumParticipants);
            setCurrentParticipant(shared);

            Tuple<int, pCarsAPIParticipantStruct> playerData = getPlayerDataStruct(shared.mParticipantData, shared.mViewedParticipantIndex);
            String playerName = StructHelper.getNameFromBytes(shared.mParticipantData[shared.mViewedParticipantIndex].mName);
            if (getPlayerByName && playerName != null && playerSteamId != null && !namesMatch(playerName, playerSteamId))
            {
                return previousGameState;
            }

            GameStateData currentGameState = new GameStateData(ticks);          
            
            int playerDataIndex = playerData.Item1;
            pCarsAPIParticipantStruct viewedParticipant = playerData.Item2;
            Validator.validate(playerName);
            currentGameState.SessionData.CompletedLaps = (int)viewedParticipant.mLapsCompleted;
            currentGameState.SessionData.SectorNumber = (int)viewedParticipant.mCurrentSector;
            currentGameState.SessionData.OverallPosition = (int)viewedParticipant.mRacePosition;
            if (currentGameState.SessionData.OverallPosition == 1)
            {
                currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
            }
            currentGameState.SessionData.IsNewSector = previousGameState == null || viewedParticipant.mCurrentSector != previousGameState.SessionData.SectorNumber;
            // When in the pit lane, mCurrentLapDistance gets set to 0 when crossing the start line and *remains at 0* until some distance into the lap (about 300 metres)
            currentGameState.PositionAndMotionData.DistanceRoundTrack = viewedParticipant.mCurrentLapDistance;
                        
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
                lastSessionRunningTime, shared.mPitMode, previousGameState == null ? null : previousGameState.OpponentData, shared.mSpeed, currentGameState.Now);
                        
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

            if (sessionOfSameTypeRestarted ||
                (currentGameState.SessionData.SessionType != SessionType.Unavailable && 
                    (lastSessionType != currentGameState.SessionData.SessionType ||                
                        lastSessionTrack == null || lastSessionTrack.name != currentGameState.SessionData.TrackDefinition.name ||
                            (currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTimeRemaining > lastSessionTimeRemaining + 1))))
            {
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
                    Console.WriteLine("SessionTimeRemaining = " + currentGameState.SessionData.SessionTimeRemaining + " lastSessionTimeRemaining = " + lastSessionTimeRemaining);
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
                for (int i = 0; i < shared.mParticipantData.Length; i++)
                {
                    pCarsAPIParticipantStruct participantStruct = shared.mParticipantData[i];
                    String participantName = StructHelper.getNameFromBytes(participantStruct.mName).ToLower();
                    if (i != playerDataIndex && participantStruct.mIsActive && participantName != null && participantName.Length > 0)
                    {
                        CarData.CarClass opponentCarClass = !shared.hasOpponentClassData || shared.isSameClassAsPlayer[i] ? currentGameState.carClass : CarData.DEFAULT_PCARS_OPPONENT_CLASS;
                        addOpponentForName(participantName, createOpponentData(participantStruct, false, opponentCarClass,
                            participantStruct.mName != null && participantStruct.mName[0] != 0, currentGameState.SessionData.TrackDefinition.trackLength), currentGameState);
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

                lastActiveTimeForOpponents.Clear();
                nextOpponentCleanupTime = nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        // just gone green, so get the session data.
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            currentGameState.SessionData.JustGoneGreen = true;
                            // ensure that we track the car we're in at the point when the lights change
                            setCurrentParticipant(shared);
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
                        lastActiveTimeForOpponents.Clear();
                        nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;

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
                    currentGameState.OpponentData = previousGameState.OpponentData;
                    currentGameState.PitData.PitWindowStart = previousGameState.PitData.PitWindowStart;
                    currentGameState.PitData.PitWindowEnd = previousGameState.PitData.PitWindowEnd;
                    currentGameState.PitData.HasMandatoryPitStop = previousGameState.PitData.HasMandatoryPitStop;
                    currentGameState.PitData.HasMandatoryTyreChange = previousGameState.PitData.HasMandatoryTyreChange;
                    currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = previousGameState.PitData.MandatoryTyreChangeRequiredTyreType;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                    currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = previousGameState.PitData.MaxPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.MinPermittedDistanceOnCurrentTyre = previousGameState.PitData.MinPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.OnInLap = previousGameState.PitData.OnInLap;
                    currentGameState.PitData.OnOutLap = previousGameState.PitData.OnOutLap;
                    // the other properties of PitData are updated each tick, and shouldn't be copied over here. Nasty...
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
                    currentGameState.SessionData.LapTimePrevious = previousGameState.SessionData.LapTimePrevious;
                    currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;
                    currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
                    currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;

                    currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;

                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;

                    currentGameState.TimingData = previousGameState.TimingData;
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
            if (shared.mLapInvalidated)
            {
                currentGameState.SessionData.CurrentLapIsValid = false;
            }
            
            currentGameState.SessionData.Flag = mapToFlagEnum(shared.mHighestFlagColour);
            currentGameState.SessionData.NumCarsOverall = shared.mNumParticipants;            
            currentGameState.SessionData.IsNewLap = previousGameState == null || currentGameState.SessionData.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                ((shared.mSessionState == (int)eSessionState.SESSION_PRACTICE || shared.mSessionState == (int)eSessionState.SESSION_QUALIFY || 
                shared.mSessionState == (int)eSessionState.SESSION_TEST || shared.mSessionState == (int)eSessionState.SESSION_TIME_ATTACK) 
                && previousGameState.SessionData.LapTimeCurrent == -1 && shared.mCurrentTime > 0);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.readLandmarksForThisLap = false;
            }

            currentGameState.PitData.InPitlane = shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_INTO_PITS ||
                shared.mPitMode == (int)ePitMode.PIT_MODE_IN_PIT ||
                shared.mPitMode == (int)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS ||
                shared.mPitMode == (int)ePitMode.PIT_MODE_IN_GARAGE;

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
            currentGameState.SessionData.LapTimeCurrent = shared.mCurrentTime;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                        shared.mLastLapTime, currentGameState.SessionData.CurrentLapIsValid, currentGameState.PitData.InPitlane,
                        shared.mRainDensity == 1, shared.mTrackTemperature, shared.mAmbientTemperature,
                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3, currentGameState.TimingData);
                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                    currentGameState.SessionData.OverallPosition, currentGameState.PitData.InPitlane, currentGameState.SessionData.SessionRunningTime);
            }
            else if (currentGameState.SessionData.IsNewSector)
            {
                if (currentGameState.SessionData.SectorNumber == 2)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(1, currentGameState.SessionData.OverallPosition, shared.mCurrentSector1Time,
                        currentGameState.SessionData.SessionRunningTime, currentGameState.SessionData.CurrentLapIsValid, shared.mRainDensity > 0, shared.mTrackTemperature, shared.mAmbientTemperature);
                }
                else if (currentGameState.SessionData.SectorNumber == 3)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(2, currentGameState.SessionData.OverallPosition, shared.mCurrentSector2Time + shared.mCurrentSector1Time,
                        currentGameState.SessionData.SessionRunningTime, currentGameState.SessionData.CurrentLapIsValid, shared.mRainDensity > 0, shared.mTrackTemperature, shared.mAmbientTemperature);
                }
            }

            // NOTE: the shared.mSessionFastestLapTime is JUST FOR THE PLAYER so the code below is not going to work:
            // currentGameState.SessionData.SessionFastestLapTimeFromGame = shared.mSessionFastestLapTime;
            // currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = shared.mSessionFastestLapTime;
            List<String> namesInRawData = new List<String>();
            for (int i = 0; i < shared.mParticipantData.Length; i++)
            {
                if (i != playerDataIndex)
                {
                    pCarsAPIParticipantStruct participantStruct = shared.mParticipantData[i];
                    if (participantStruct.mName == null || participantStruct.mName[0] == 0)
                    {
                        // first character of name is null - this means the game regards this driver as inactive or missing for this update
                        continue;
                    }
                    CarData.CarClass opponentCarClass = !shared.hasOpponentClassData || shared.isSameClassAsPlayer[i] ? currentGameState.carClass : CarData.DEFAULT_PCARS_OPPONENT_CLASS;
                    String participantName = StructHelper.getNameFromBytes(participantStruct.mName).ToLower();
                    // awesomely, the driver can appear twice (or more?) in the array. Can't be sure which of the copies is the dead one so assume the first is the one to use.
                    // This may be wrong but the PCars data is such a fucking shambles I honestly can't be arsed with it any more.
                    if (participantName != null && participantName.Length > 0 && !namesInRawData.Contains(participantName))
                    {
                        namesInRawData.Add(participantName);
                        OpponentData currentOpponentData = getOpponentForName(currentGameState, participantName);
                        if (currentOpponentData != null)
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
                                    float previousOpponentSpeed = 0;
                                    float previousDistanceRoundTrack = 0;

                                    OpponentData previousOpponentData = getOpponentForName(previousGameState, participantName);
                                    if (previousOpponentData != null)
                                    {
                                        previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                        previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                        previousOpponentPosition = previousOpponentData.OverallPosition;
                                        previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                        previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                        previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                        previousOpponentSpeed = previousOpponentData.Speed;
                                        previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                                        currentOpponentData.ClassPositionAtPreviousTick = previousOpponentData.ClassPosition;
                                        currentOpponentData.OverallPositionAtPreviousTick = previousOpponentData.OverallPosition;
                                    }

                                    int currentOpponentRacePosition = (int)participantStruct.mRacePosition;
                                    int currentOpponentLapsCompleted = (int)participantStruct.mLapsCompleted;
                                    int currentOpponentSector = (int)participantStruct.mCurrentSector;
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
                                    Boolean isEnteringPits = false;
                                    Boolean isLeavingPits = false;
                                    if (attemptPitDetection)
                                    {
                                        if (previousOpponentData != null && currentGameState.SessionData.SessionRunningTime > 30)
                                        {
                                            if (currentOpponentSector == 3)
                                            {
                                                if (!previousOpponentIsEnteringPits)
                                                {
                                                    isEnteringPits = currentGameState.SessionData.TrackDefinition != null &&
                                                        currentGameState.SessionData.TrackDefinition.isAtPitEntry(participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2]);
                                                }
                                                else
                                                {
                                                    isEnteringPits = previousOpponentIsEnteringPits;
                                                }
                                            }
                                            else if (currentOpponentSector == 1 && !previousOpponentIsExitingPits)
                                            {
                                                isLeavingPits = currentGameState.SessionData.TrackDefinition != null &&
                                                        currentGameState.SessionData.TrackDefinition.isAtPitExit(participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2]);
                                            }
                                        }
                                    }
                                    float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;
                                    upateOpponentData(currentOpponentData, participantName, currentOpponentRacePosition, currentOpponentLapsCompleted,
                                            currentOpponentSector, isEnteringPits || isLeavingPits, currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                            new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] }, previousOpponentWorldPosition,
                                            previousOpponentSpeed, shared.mWorldFastestLapTime, shared.mWorldFastestSector1Time, shared.mWorldFastestSector2Time, shared.mWorldFastestSector3Time, 
                                            participantStruct.mCurrentLapDistance, shared.mRainDensity == 1,
                                            shared.mAmbientTemperature, shared.mTrackTemperature, opponentCarClass,
                                            currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                            shared.mLastSectorData == null ? -1 : shared.mLastSectorData[i], shared.mLapInvalidatedData == null ? false : shared.mLapInvalidatedData[i],
                                            currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks,
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
                                currentOpponentData.IsActive = false;
                            }
                        }
                        else
                        {
                            if (participantStruct.mIsActive && participantName != null && participantName.Length > 0)
                            {
                                lastActiveTimeForOpponents[participantName] = currentGameState.Now;
                                addOpponentForName(participantName, createOpponentData(participantStruct, true, opponentCarClass,
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
                List<String> inactiveOpponents = new List<string>();
                foreach (string opponentName in currentGameState.OpponentData.Keys)
                {
                    DateTime lastActiveTime = DateTime.MinValue;
                    if (!lastActiveTimeForOpponents.TryGetValue(opponentName, out lastActiveTime) || lastActiveTime < oldestAllowedUpdate)
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

            currentGameState.PitData.IsAtPitExit = previousGameState != null && currentGameState.PitData.OnOutLap && 
                previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane;

            if (CrewChief.gameDefinition.gameEnum != GameEnum.PCARS_NETWORK)
            {
                // broken pit schedule data in pcars1 UDP
                currentGameState.PitData.HasRequestedPitStop = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_STANDARD;
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && shared.mEnforcedPitStopLap > 0 && enablePCarsPitWindowStuff)
            {
                currentGameState.PitData.HasMandatoryPitStop = true;
                currentGameState.PitData.PitWindowStart = (int) shared.mEnforcedPitStopLap;
                // estimate the pit window close lap / time
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.PitData.PitWindowEnd = (int)((currentGameState.SessionData.SessionTotalRunTime - 120f) / 60f);
                }
                else
                {
                    currentGameState.PitData.PitWindowEnd = currentGameState.SessionData.SessionNumberOfLaps - 1;
                }
                currentGameState.PitData.PitWindow = mapToPitWindow(currentGameState, shared.mPitSchedule, shared.mPitMode);
                currentGameState.PitData.IsMakingMandatoryPitStop = (currentGameState.PitData.PitWindow == PitWindow.Open || currentGameState.PitData.PitWindow == PitWindow.StopInProgress) &&
                                                                    (currentGameState.PitData.OnInLap || currentGameState.PitData.OnOutLap);
                if (previousGameState != null)
                {
                    currentGameState.PitData.MandatoryPitStopCompleted = previousGameState.PitData.MandatoryPitStopCompleted || currentGameState.PitData.IsMakingMandatoryPitStop;
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
            currentGameState.EngineData.EngineRpm = shared.mRPM;
            currentGameState.EngineData.MaxEngineRpm = shared.mMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 2;

            currentGameState.FuelData.FuelCapacity = shared.mFuelCapacity;
            currentGameState.FuelData.FuelLeft = currentGameState.FuelData.FuelCapacity * shared.mFuelLevel;
            currentGameState.FuelData.FuelPressure = shared.mFuelPressureKPa;
            currentGameState.FuelData.FuelUseActive = true;         // no way to tell if it's disabled

            if (CrewChief.gameDefinition.gameEnum != GameEnum.PCARS_NETWORK)
            {
                // broken pitschedule data in pcars1 UDP
                currentGameState.PenaltiesData.HasDriveThrough = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_DRIVE_THROUGH;
                currentGameState.PenaltiesData.HasStopAndGo = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_STOP_GO;
            }


            currentGameState.PositionAndMotionData.CarSpeed = shared.mSpeed;

            currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, 
                currentGameState.SessionData.CompletedLaps, shared.mSpeed, currentGameState.Now);

            //------------------------ Tyre data -----------------------          
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TyreWearActive = true;

            currentGameState.TyreData.LeftFrontAttached = (shared.mTyreFlags[0] & 1) == 1;
            currentGameState.TyreData.RightFrontAttached = (shared.mTyreFlags[1] & 1) == 1;
            currentGameState.TyreData.LeftRearAttached = (shared.mTyreFlags[2] & 1) == 1;
            currentGameState.TyreData.RightRearAttached = (shared.mTyreFlags[3] & 1) == 1;

            currentGameState.TyreData.FrontLeft_CenterTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.mTyreTreadTemp[0] - 273;
            currentGameState.TyreData.FrontLeftTyreType = defaultTyreTypeForPlayersCar;
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
            currentGameState.TyreData.FrontRightTyreType = defaultTyreTypeForPlayersCar;
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
            currentGameState.TyreData.RearLeftTyreType = defaultTyreTypeForPlayersCar;
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
            currentGameState.TyreData.RearRightTyreType = defaultTyreTypeForPlayersCar;
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

            var tyreTempThresholds = CarData.getTyreTempThresholds(currentGameState.carClass);
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
            
            if (currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.hardPartsOnTrackData.updateHardPartsForNewLap(currentGameState.SessionData.LapTimePrevious))
                {
                    currentGameState.SessionData.TrackDefinition.adjustGapPoints(currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap);
                }
            }
            else if (!currentGameState.PitData.OnOutLap &&
                !(currentGameState.SessionData.SessionType == SessionType.Race &&
                   (currentGameState.SessionData.CompletedLaps < 1 || (GameStateData.useManualFormationLap && currentGameState.SessionData.CompletedLaps < 2))))
            {
                currentGameState.hardPartsOnTrackData.mapHardPartsOnTrack(currentGameState.ControlData.BrakePedal, currentGameState.ControlData.ThrottlePedal,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CurrentLapIsValid, currentGameState.SessionData.TrackDefinition.trackLength);
            }

            currentGameState.ControlData.BrakePedal = shared.mBrake;
            currentGameState.ControlData.ThrottlePedal = shared.mThrottle;
            currentGameState.ControlData.ClutchPedal = shared.mClutch;

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

        private void upateOpponentData(OpponentData opponentData, String name, int racePosition, int completedLaps, int sector, Boolean isInPits,
            float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition, float[] previousWorldPosition,
            float previousSpeed, float worldRecordLapTime, float worldRecordS1Time, float worldRecordS2Time, float worldRecordS3Time, 
            float distanceRoundTrack, Boolean isRaining, float trackTemp, float airTemp, CarData.CarClass carClass,
            Boolean sessionLengthIsTime, float sessionTimeRemaining, float lastSectorTime, Boolean lapInvalidated, float nearPitEntryPointDistance,
            TimingData timingData, CarData.CarClass playerCarClass)
        {
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            if (opponentData.DriverRawName.StartsWith(NULL_CHAR_STAND_IN) && name != null && name.Trim().Length > 0 && !name.StartsWith(NULL_CHAR_STAND_IN))
            {
                opponentData.DriverRawName = name;
                if (CrewChief.enableDriverNames)
                {
                    speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
                }
            }
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            float speed;
            if (secondsSinceLastUpdate == 0)
            {
                speed = opponentData.Speed;
            }
            else
            {
                speed = (float)Math.Sqrt(Math.Pow(currentWorldPosition[0] - previousWorldPosition[0], 2) + Math.Pow(currentWorldPosition[1] - previousWorldPosition[1], 2)) / secondsSinceLastUpdate;
            }
            if (speed > 500)
            {
                // faster than 500m/s (1000+mph) suggests the player has quit to the pit. Might need to reassess this as the data are quite noisy
                if (CrewChief.gameDefinition.gameEnum != GameEnum.PCARS_NETWORK)
                {
                    lapInvalidated = true;
                }
                speed = opponentData.Speed;
            }
            List<float> speedsExisting = null;
            if (opponentSpeedsWindow.TryGetValue(opponentData.DriverRawName, out speedsExisting)) {
                if (speedsExisting.Count() == opponentSpeedsToAverage) {
                    speedsExisting.RemoveAt(opponentSpeedsToAverage - 1);
                }
                speedsExisting.Insert(0, speed);
                float sum = 0f;
                foreach (float item in speedsExisting) {
                    sum += item;
                }
                opponentData.Speed = sum / speedsExisting.Count();
            } else {
                List<float> speeds = new List<float>();
                speeds.Add(speed);
                opponentSpeedsWindow.Add(opponentData.DriverRawName, speeds);
                opponentData.Speed = speed;
            }
            opponentData.OverallPosition = racePosition;
            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.OverallPosition;
            }
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            opponentData.CarClass = carClass;
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        // lastSectorTime values are -123 at the start of the session
                        if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK) 
                        {
                            // use the last sector time, or -1 if it's that magic -123 number (and mark the lap as invalid)
                            if (lastSectorTime <= 0)
                            {
                                lastSectorTime = -1;
                                lapInvalidated = true;
                            }
                            opponentData.CompleteLapWithLastSectorTime(racePosition, lastSectorTime, sessionRunningTime,
                                !lapInvalidated, isRaining, trackTemp, airTemp, sessionLengthIsTime, sessionTimeRemaining, 3, timingData,
                                CarData.IsCarClassEqual(opponentData.CarClass, playerCarClass));
                        }
                        else
                        {
                            // use the inbuilt timing
                            opponentData.CompleteLapWithEstimatedLapTime(racePosition, sessionRunningTime, worldRecordLapTime, worldRecordS1Time, worldRecordS2Time, worldRecordS3Time,
                                !lapInvalidated, isRaining, trackTemp, airTemp, sessionLengthIsTime, sessionTimeRemaining, timingData,
                                CarData.IsCarClassEqual(opponentData.CarClass, playerCarClass));
                        }
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, isRaining, trackTemp, airTemp);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    // lastSectorTime values are -123 at the start of the session
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK) 
                    {
                        // use the last sector time, or -1 if it's that magic -123 number (and mark the lap as invalid)
                        if (lastSectorTime <= 0)
                        {
                            lastSectorTime = -1;
                            lapInvalidated = true;
                        }
                        opponentData.AddSectorData(opponentData.CurrentSectorNumber, racePosition, lastSectorTime, sessionRunningTime, !lapInvalidated, isRaining, trackTemp, airTemp);
                    }
                    else
                    {
                        // use the inbuilt timing
                        opponentData.AddCumulativeSectorData(opponentData.CurrentSectorNumber, racePosition, -1, sessionRunningTime, !lapInvalidated, isRaining, trackTemp, airTemp);
                    }
                }
                opponentData.CurrentSectorNumber = sector;
            }
            if (sector == 3 && isInPits) 
            {
                opponentData.setInLap();
            }
            opponentData.JustEnteredPits = !opponentData.InPits && isInPits;
            if (opponentData.JustEnteredPits)
            {
                opponentData.NumPitStops++;
            }
            opponentData.InPits = isInPits;
            opponentData.CompletedLaps = completedLaps;
        }

        private OpponentData createOpponentData(pCarsAPIParticipantStruct participantStruct, Boolean loadDriverName, CarData.CarClass carClass, Boolean canUseName, float trackLength)
        {            
            OpponentData opponentData = new OpponentData();
            String participantName = StructHelper.getNameFromBytes(participantStruct.mName).ToLower();
            opponentData.DriverRawName = participantName;
            opponentData.DriverNameSet = true;
            if (participantName != null && participantName.Length > 0 && !participantName.StartsWith(NULL_CHAR_STAND_IN) && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }
            opponentData.OverallPosition = (int)participantStruct.mRacePosition;
            opponentData.CompletedLaps = (int)participantStruct.mLapsCompleted;
            opponentData.CurrentSectorNumber = (int)participantStruct.mCurrentSector;
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
            pCarsAPIStruct shared = (pCarsAPIStruct)memoryMappedFileStruct;
            uint sessionState = shared.mSessionState;
            if (sessionState == (uint)eSessionState.SESSION_RACE || sessionState == (uint)eSessionState.SESSION_FORMATIONLAP)
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
            SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime, uint pitMode, Dictionary<string, OpponentData> opponentData, float playerSpeed, DateTime now)
        {
            if (numParticipants < 1)
            {
                return SessionPhase.Unavailable;
            }
            if (sessionType == SessionType.Race)
            {
                if (raceState == (uint)eRaceState.RACESTATE_NOT_STARTED)
                {
                    if (sessionState == (uint)eSessionState.SESSION_FORMATIONLAP)
                    {
                        return SessionPhase.Formation;
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
                                float distRoundTrack = -1.0f;
                                if (!waitingForCarsToFinish.TryGetValue(opponent.DriverRawName, out distRoundTrack)) {
                                    waitingForCarsToFinish.Add(opponent.DriverRawName, opponent.DistanceRoundTrack);
                                    running = true;
                                }
                                else if (distRoundTrack < opponent.DistanceRoundTrack)
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
            if (highestFlagColour == (uint) eFlagColors.FLAG_COLOUR_CHEQUERED)
            {
                return FlagEnum.CHEQUERED;
            }
            else if (highestFlagColour == (uint) eFlagColors.FLAG_COLOUR_BLACK) 
            {
                return FlagEnum.BLACK;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_DOUBLE_YELLOW) 
            {
                return FlagEnum.DOUBLE_YELLOW;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_YELLOW) 
            {
                return FlagEnum.YELLOW;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_WHITE) 
            {
                return FlagEnum.WHITE;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_BLUE) 
            {
                return FlagEnum.BLUE;
            }
            else if (highestFlagColour == (uint)eFlagColors.FLAG_COLOUR_GREEN) 
            {
                return FlagEnum.GREEN;
            }
            return FlagEnum.UNKNOWN;
        }

        private PitWindow mapToPitWindow(GameStateData currentGameState, uint pitSchedule, uint pitMode)
        {
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
                    if (currentGameState.PitData.PitWindow == PitWindow.Completed ||
                        (currentGameState.PitData.PitWindow == PitWindow.StopInProgress && pitMode == (uint)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS))
                    {
                        return PitWindow.Completed;
                    }
                    else 
                    if (pitSchedule == (uint)ePitSchedule.PIT_SCHEDULE_STANDARD &&
                        (pitMode == (uint)ePitMode.PIT_MODE_DRIVING_INTO_PITS || pitMode == (uint)ePitMode.PIT_MODE_IN_PIT))
                    {
                        // pcars1 UDP pit schedule is broken - this may be unsafe:
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
    }
}
