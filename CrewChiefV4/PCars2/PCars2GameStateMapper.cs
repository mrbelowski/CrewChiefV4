﻿using System;
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
namespace CrewChiefV4.PCars2
{
    class PCars2GameStateMapper : GameStateMapper
    {
        public static String NULL_CHAR_STAND_IN = "?";

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

        // TODO: have separate thresholds for tin-tops and open wheelers with wings here
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

        private SpeechRecogniser speechRecogniser;

        private Dictionary<string, float> waitingForCarsToFinish = new Dictionary<string, float>();
        private DateTime nextDebugCheckeredToFinishMessageTime = DateTime.MinValue;

        private static int lastGuessAtPlayerIndex = -1;

        Dictionary<string, DateTime> lastActiveTimeForOpponents = new Dictionary<string, DateTime>();
        DateTime nextOpponentCleanupTime = DateTime.MinValue;
        TimeSpan opponentCleanupInterval = TimeSpan.FromSeconds(4);
        HashSet<uint> positionsFilledForThisTick = new HashSet<uint>();
        List<String> opponentDriverNamesProcessedForThisTick = new List<String>();

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

        public void versionCheck(Object memoryMappedFileStruct)
        {
            // no-op
        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
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
            if (gameState.OpponentData.ContainsKey(name))
            {
                gameState.OpponentData.Remove(name);
            }
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

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
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
            
            NameValidator.validateName(playerName);
            currentGameState.SessionData.CompletedLaps = (int)playerData.mLapsCompleted;
            currentGameState.SessionData.SectorNumber = (int)playerData.mCurrentSector + 1; // zero indexed
            currentGameState.SessionData.Position = (int)playerData.mRacePosition;
            if (currentGameState.SessionData.Position == 1)
            {
                currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
            }
            currentGameState.SessionData.UnFilteredPosition = (int)playerData.mRacePosition;
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
                if (!String.Equals(newClass.getClassIdentifier(), currentGameState.carClass.getClassIdentifier()))
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
                lastSessionRunningTime, shared.mPitModes[playerIndex], previousGameState == null ? null : previousGameState.OpponentData,
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
            Boolean justGoneGreen = false;
            if (sessionOfSameTypeRestarted ||
                (currentGameState.SessionData.SessionType != SessionType.Unavailable && 
                    (lastSessionType != currentGameState.SessionData.SessionType ||                
                        lastSessionTrack == null || lastSessionTrack.name != currentGameState.SessionData.TrackDefinition.name ||
                            (currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTimeRemaining > lastSessionTimeRemaining + 1))))
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
                currentGameState.SessionData.SessionStartPosition = (int) playerData.mRacePosition;
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

                opponentDriverNamesProcessedForThisTick.Clear();
                opponentDriverNamesProcessedForThisTick.Add(playerName);
                positionsFilledForThisTick.Clear();
                positionsFilledForThisTick.Add((uint)currentGameState.SessionData.Position);
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

                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
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
                            justGoneGreen = true;
                            // ensure that we track the car we're in at the point when the lights change
                            if (currentGameState.SessionData.SessionHasFixedTime)
                            {
                                currentGameState.SessionData.SessionTotalRunTime = shared.mEventTimeRemaining;
                                currentGameState.SessionData.SessionTimeRemaining = shared.mEventTimeRemaining;
                                currentGameState.SessionData.SessionRunningTime = 0;
                            }
                            currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                            currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                            currentGameState.SessionData.SessionStartPosition = (int)playerData.mRacePosition;
                        }          
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.mNumParticipants;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(StructHelper.getNameFromBytes(shared.mTrackLocation) + ":" +
                            StructHelper.getNameFromBytes(shared.mTrackVariation), -1, shared.mTrackLength);
                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(currentGameState.SessionData.TrackDefinition.name, shared.mTrackLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                        currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);

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
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                        Console.WriteLine("SessionStartPosition " + currentGameState.SessionData.SessionStartPosition);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName);
                    }
                }
                // copy persistent data from the previous game state
                //

                // TODO: this is just retarded. Clone the previousGameState and update it as required...
                if (!justGoneGreen && previousGameState != null)
                {
                    //Console.WriteLine("regular update, session type = " + currentGameState.SessionData.SessionType + " phase = " + currentGameState.SessionData.SessionPhase);
                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                    currentGameState.SessionData.NumCarsAtStartOfSession = previousGameState.SessionData.NumCarsAtStartOfSession;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = previousGameState.SessionData.PositionAtStartOfCurrentLap;
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
                    currentGameState.Conditions = previousGameState.Conditions;
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;
                    currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
                    currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;

                    currentGameState.SessionData.DeltaTime.deltaPoints = previousGameState.SessionData.DeltaTime.deltaPoints;
                    currentGameState.SessionData.DeltaTime.currentDeltaPoint = previousGameState.SessionData.DeltaTime.currentDeltaPoint;
                    currentGameState.SessionData.DeltaTime.nextDeltaPoint = previousGameState.SessionData.DeltaTime.nextDeltaPoint;
                    currentGameState.SessionData.DeltaTime.lapsCompleted = previousGameState.SessionData.DeltaTime.lapsCompleted;
                    currentGameState.SessionData.DeltaTime.totalDistanceTravelled = previousGameState.SessionData.DeltaTime.totalDistanceTravelled;
                    currentGameState.SessionData.DeltaTime.trackLength = previousGameState.SessionData.DeltaTime.trackLength;

                    currentGameState.disqualifiedDriverNames = previousGameState.disqualifiedDriverNames;
                    currentGameState.retriedDriverNames = previousGameState.retriedDriverNames;
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
            if (currentGameState.SessionData.IsNewSector)
            {
                if (currentGameState.SessionData.SectorNumber == 1)
                {
                    currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;

                    if (shared.mLastLapTimes[playerIndex] > 0 && currentGameState.SessionData.LastSector1Time > 0 && currentGameState.SessionData.LastSector2Time > 0)
                    {
                        currentGameState.SessionData.LastSector3Time = (shared.mLastLapTimes[playerIndex] - currentGameState.SessionData.LastSector1Time) - currentGameState.SessionData.LastSector2Time;
                    }
                    else
                    {
                        currentGameState.SessionData.LastSector3Time = -1;
                    }
                    if (currentGameState.SessionData.LastSector3Time > 0 && 
                        (currentGameState.SessionData.PlayerBestSector3Time == -1 || currentGameState.SessionData.LastSector3Time < currentGameState.SessionData.PlayerBestSector3Time))
                    {
                        currentGameState.SessionData.PlayerBestSector3Time = currentGameState.SessionData.LastSector3Time;
                    }
                    if (shared.mLastLapTimes[playerIndex] > 0 &&
                        (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 || shared.mLastLapTimes[playerIndex] <= currentGameState.SessionData.PlayerLapTimeSessionBest))
                    {
                        currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.LastSector1Time;
                        currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.LastSector2Time;
                        currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.LastSector3Time;
                    }
                }
                else if (currentGameState.SessionData.SectorNumber == 2)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;

                    currentGameState.SessionData.LastSector1Time = shared.mCurrentSector1Times[playerIndex];
                    if (currentGameState.SessionData.LastSector1Time > 0 && !shared.mLapInvalidated &&
                        (currentGameState.SessionData.PlayerBestSector1Time == -1 || currentGameState.SessionData.LastSector1Time < currentGameState.SessionData.PlayerBestSector1Time))
                    {
                        currentGameState.SessionData.PlayerBestSector1Time = currentGameState.SessionData.LastSector1Time;
                    }
                    currentGameState.SessionData.playerAddCumulativeSectorData(1, currentGameState.SessionData.Position, shared.mCurrentSector1Times[playerIndex],
                        currentGameState.SessionData.SessionRunningTime, !shared.mLapInvalidated, shared.mRainDensity > 0, shared.mTrackTemperature, shared.mAmbientTemperature);
                }
                if (currentGameState.SessionData.SectorNumber == 3)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                    currentGameState.SessionData.LastSector2Time = shared.mCurrentSector2Times[playerIndex];
                    if (currentGameState.SessionData.LastSector2Time > 0 && !shared.mLapInvalidated &&
                        (currentGameState.SessionData.PlayerBestSector2Time == -1 || currentGameState.SessionData.LastSector2Time < currentGameState.SessionData.PlayerBestSector2Time))
                    {
                        currentGameState.SessionData.PlayerBestSector2Time = currentGameState.SessionData.LastSector2Time;
                    }
                    currentGameState.SessionData.playerAddCumulativeSectorData(2, currentGameState.SessionData.Position, shared.mCurrentSector2Times[playerIndex] + shared.mCurrentSector1Times[playerIndex],
                        currentGameState.SessionData.SessionRunningTime, !shared.mLapInvalidated, shared.mRainDensity > 0, shared.mTrackTemperature, shared.mAmbientTemperature);
                }
            }

            currentGameState.SessionData.Flag = mapToFlagEnum(shared.mHighestFlagColour);
            currentGameState.SessionData.NumCars = shared.mNumParticipants;
            currentGameState.SessionData.CurrentLapIsValid = !shared.mLapInvalidated;                
            currentGameState.SessionData.IsNewLap = previousGameState == null || currentGameState.SessionData.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.readLandmarksForThisLap = false;
            }
            currentGameState.PitData.InPitlane = shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_DRIVING_INTO_PITS ||
                shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_IN_PIT ||
               shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS ||
                shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_IN_GARAGE;

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

            currentGameState.SessionData.IsRacingSameCarBehind = previousGameState != null && previousGameState.getOpponentKeyBehind(false) == currentGameState.getOpponentKeyBehind(false);
            currentGameState.SessionData.IsRacingSameCarInFront = previousGameState != null && previousGameState.getOpponentKeyInFront(false) == currentGameState.getOpponentKeyInFront(false);
            if (!currentGameState.SessionData.IsRacingSameCarInFront)
            {
                currentGameState.SessionData.GameTimeAtLastPositionFrontChange = currentGameState.SessionData.SessionRunningTime;
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                currentGameState.SessionData.GameTimeAtLastPositionBehindChange = currentGameState.SessionData.SessionRunningTime;
            }

            // TODO: assume non-cumulative sectors....
            currentGameState.SessionData.LapTimeCurrent = shared.mCurrentSector1Times[playerIndex] +
                shared.mCurrentSector3Times[playerIndex] + shared.mCurrentSector3Times[playerIndex];
            currentGameState.SessionData.TimeDeltaBehind = shared.mSplitTimeBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.mSplitTimeAhead;

            opponentDriverNamesProcessedForThisTick.Clear();
            opponentDriverNamesProcessedForThisTick.Add(playerName);
            positionsFilledForThisTick.Clear();
            positionsFilledForThisTick.Add((uint)currentGameState.SessionData.Position);
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
                            if (currentGameState.OpponentData.ContainsKey(participantName))
                            {
                                currentGameState.OpponentData.Remove(participantName);
                            }
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
                            if (currentGameState.OpponentData.ContainsKey(participantName))
                            {
                                currentGameState.OpponentData.Remove(participantName);
                            }
                            continue;
                        }
                        CarData.CarClass opponentCarClass = CarData.getCarClassForClassName(StructHelper.getCarClassName(shared, i));
                        
                        if (currentGameState.OpponentData.ContainsKey(participantName))
                        {
                            OpponentData currentOpponentData = currentGameState.OpponentData[participantName];
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
                                    if (previousGameState.OpponentData.ContainsKey(participantName))
                                    {
                                        previousOpponentData = currentGameState.OpponentData[participantName];                                    
                                        previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                        previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                        previousOpponentPosition = previousOpponentData.Position;
                                        previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                        previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                        previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                        previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
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
                                    if (currentOpponentRacePosition == 1)
                                    {
                                        currentGameState.SessionData.LeaderSectorNumber = currentOpponentSector;
                                    }
                                    if (currentOpponentRacePosition == 1 && previousOpponentPosition > 1)
                                    {
                                        currentGameState.SessionData.HasLeadChanged = true;
                                    }
                                    Boolean isEnteringPits = shared.mPitModes[i] == (uint) ePitMode.PIT_MODE_DRIVING_INTO_PITS;
                                    Boolean isLeavingPits = shared.mPitModes[i] == (uint)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS ||
                                        shared.mPitModes[i] == (uint)ePitMode.PIT_MODE_DRIVING_OUT_OF_GARAGE;
                                    Boolean isInPits = shared.mPitModes[i] == (uint)ePitMode.PIT_MODE_IN_PIT ||
                                        shared.mPitModes[i] == (uint)ePitMode.PIT_MODE_IN_GARAGE;

                                    if (isEnteringPits && !previousOpponentIsEnteringPits)
                                    {
                                        if (currentOpponentData.PositionOnApproachToPitEntry == 1)
                                        {
                                            currentGameState.PitData.LeaderIsPitting = true;
                                            currentGameState.PitData.OpponentForLeaderPitting = currentOpponentData;
                                        }
                                        if (currentGameState.SessionData.Position > 2 && currentOpponentData.PositionOnApproachToPitEntry == currentGameState.SessionData.Position - 1)
                                        {
                                            currentGameState.PitData.CarInFrontIsPitting = true;
                                            currentGameState.PitData.OpponentForCarAheadPitting = currentOpponentData;
                                        }
                                        if (!currentGameState.isLast() && currentOpponentData.PositionOnApproachToPitEntry == currentGameState.SessionData.Position + 1)
                                        {
                                            currentGameState.PitData.CarBehindIsPitting = true;
                                            currentGameState.PitData.OpponentForCarBehindPitting = currentOpponentData;
                                        }
                                    }
                                    
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
                                    updateOpponentData(currentOpponentData, participantName, currentOpponentRacePosition, currentOpponentLapsCompleted,
                                            currentOpponentSector, isEnteringPits, isInPits, isLeavingPits, currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                            new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] }, previousOpponentWorldPosition,
                                            shared.mSpeeds[i], shared.mWorldFastestLapTime, shared.mWorldFastestSector1Time, shared.mWorldFastestSector2Time, shared.mWorldFastestSector3Time, 
                                            participantStruct.mCurrentLapDistance, shared.mRainDensity == 1,
                                            shared.mAmbientTemperature, shared.mTrackTemperature, opponentCarClass,
                                            currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                            lastSectorTime, shared.mLapsInvalidated[i] == 1, currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks);

                                    if (previousOpponentData != null)
                                    {
                                        currentOpponentData.trackLandmarksTiming = previousOpponentData.trackLandmarksTiming;
                                        String stoppedInLandmark = currentOpponentData.trackLandmarksTiming.updateLandmarkTiming(
                                            currentGameState.SessionData.TrackDefinition, currentGameState.SessionData.SessionRunningTime,
                                            previousDistanceRoundTrack, currentOpponentData.DistanceRoundTrack, currentOpponentData.Speed);
                                        currentOpponentData.stoppedInLandmark = currentOpponentData.InPits ? null : stoppedInLandmark;
                                    }
                                    if (justGoneGreen)
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
                                currentGameState.OpponentData.Remove(participantName);
                            }
                        }
                        else
                        {
                            if (participantStruct.mIsActive && participantName != null && participantName.Length > 0)
                            {
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
                List<string> inactiveOpponents = new List<string>();
                foreach (string opponentName in currentGameState.OpponentData.Keys)
                {
                    if (!lastActiveTimeForOpponents.ContainsKey(opponentName) || lastActiveTimeForOpponents[opponentName] < oldestAllowedUpdate)
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
            
            currentGameState.SessionData.LapTimePrevious = shared.mLastLapTime;
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.PreviousLapWasValid = previousGameState != null && previousGameState.SessionData.CurrentLapIsValid;
                currentGameState.SessionData.CurrentLapIsValid = true;
                currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(shared.mLastLapTime).ToString(@"mm\:ss\.fff"));
                currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.Position;
                currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.Position, currentGameState.SessionData.SessionRunningTime,
                        shared.mLastLapTime, currentGameState.SessionData.PreviousLapWasValid, false, shared.mTrackTemperature, shared.mAmbientTemperature, 
                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3);
                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                    currentGameState.SessionData.Position, currentGameState.PitData.InPitlane, currentGameState.SessionData.SessionRunningTime, false, 
                    shared.mTrackTemperature, shared.mAmbientTemperature);
            }
            else if (previousGameState != null)
            {
                currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;
            }

            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.PreviousLapWasValid &&
                currentGameState.SessionData.LapTimePrevious > 0)
            {
                if (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 ||
                     currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.PlayerLapTimeSessionBest)
                {
                    currentGameState.SessionData.PlayerLapTimeSessionBest = currentGameState.SessionData.LapTimePrevious;
                    if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                        currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.OverallSessionBestLapTime)
                    {
                        currentGameState.SessionData.OverallSessionBestLapTime = currentGameState.SessionData.LapTimePrevious;
                    }
                    if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                        currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.PlayerClassSessionBestLapTime)
                    {
                        currentGameState.SessionData.PlayerClassSessionBestLapTime = currentGameState.SessionData.LapTimePrevious;
                    }
                }
            }

            if (currentGameState.PitData.InPitlane)
            {
                // should we just use the sector number to check this?
                if (shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_DRIVING_INTO_PITS)
                {
                    currentGameState.PitData.OnInLap = true;
                    currentGameState.PitData.OnOutLap = false;
                }
                else if (shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_DRIVING_OUT_OF_PITS || shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_IN_GARAGE
                    || shared.mPitModes[playerIndex] == (int)ePitMode.PIT_MODE_IN_PIT)
                {
                    currentGameState.PitData.OnInLap = false;
                    currentGameState.PitData.OnOutLap = true;
                }
            }
            else if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = false;
            }

            currentGameState.PitData.IsAtPitExit = previousGameState != null && currentGameState.PitData.OnOutLap && 
                previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane;
            // looks like this is fucked up in the data - UDP has pitModeSchedule for each participant, MMF has pitMode. We need both.
            currentGameState.PitData.HasRequestedPitStop = false;
            currentGameState.PitData.PitWindow = PitWindow.Unavailable;
            if (currentGameState.SessionData.SessionType == SessionType.Race && shared.mEnforcedPitStopLap > 0)
            {
                currentGameState.PitData.HasMandatoryPitStop = true;
                currentGameState.PitData.PitWindowStart = (int) shared.mEnforcedPitStopLap;
                // estimate the pit window close lap / time
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.PitData.PitWindowEnd = (int)((currentGameState.SessionData.SessionTotalRunTime - 120f) / 60f);
                    // TODO: fixme, there's no pit schedule data in shared memory because that would be too 'working'.
                    currentGameState.PitData.PitWindow = currentGameState.SessionData.SessionRunningTime >= currentGameState.PitData.PitWindowStart &&
                        currentGameState.SessionData.SessionRunningTime <= currentGameState.PitData.PitWindowEnd ?
                            currentGameState.PitData.PitWindow = PitWindow.Open : currentGameState.PitData.PitWindow = PitWindow.Closed;
                }
                else
                {
                    currentGameState.PitData.PitWindowEnd = currentGameState.SessionData.SessionNumberOfLaps - 1;
                    // TODO: fixme, there's no pit schedule data in shared memory because that would be too 'working'.
                    currentGameState.PitData.PitWindow = currentGameState.SessionData.CompletedLaps >= currentGameState.PitData.PitWindowStart &&
                        currentGameState.SessionData.CompletedLaps <= currentGameState.PitData.PitWindowEnd ?
                            currentGameState.PitData.PitWindow = PitWindow.Open : currentGameState.PitData.PitWindow = PitWindow.Closed;
                }
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

            currentGameState.EngineData.EngineOilPressure = shared.mOilPressureKPa; // todo: units conversion
            currentGameState.EngineData.EngineOilTemp = shared.mOilTempCelsius;
            currentGameState.EngineData.EngineWaterTemp = shared.mWaterTempCelsius;
            currentGameState.EngineData.MaxEngineRpm = shared.mMaxRPM;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 2;

            currentGameState.FuelData.FuelCapacity = shared.mFuelCapacity;
            currentGameState.FuelData.FuelLeft = currentGameState.FuelData.FuelCapacity * shared.mFuelLevel;
            currentGameState.FuelData.FuelPressure = shared.mFuelPressureKPa;
            currentGameState.FuelData.FuelUseActive = true;         // no way to tell if it's disabled

            // TODO: broken pit schedule data
            /*
            currentGameState.PenaltiesData.HasDriveThrough = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_DRIVE_THROUGH;
            currentGameState.PenaltiesData.HasStopAndGo = shared.mPitSchedule == (int)ePitSchedule.PIT_SCHEDULE_STOP_GO;
            */
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
                // TODO: fix this properly - decrease the minRotatingSpeed from 2*pi to pi just to hide the problem
                float minRotatingSpeed = (float)Math.PI * shared.mSpeed / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.mTyreRPS[0]) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.mTyreRPS[1]) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.mTyreRPS[2]) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.mTyreRPS[3]) < minRotatingSpeed;

                // TODO: fix this properly - increase the maxRotatingSpeed from 2*pi to 3*pi just to hide the problem
                float maxRotatingSpeed = 3 * (float)Math.PI * shared.mSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.mTyreRPS[0]) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.mTyreRPS[1]) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.mTyreRPS[2]) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.mTyreRPS[3]) > maxRotatingSpeed;
            }

            if (currentGameState.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < currentGameState.Now)
            {
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.mAmbientTemperature, shared.mTrackTemperature, shared.mRainDensity, shared.mWindSpeed, shared.mWindDirectionX, shared.mWindDirectionY, shared.mCloudBrightness);
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

        private void updateOpponentData(OpponentData opponentData, String name, int racePosition, int completedLaps, int sector, Boolean isEnteringPits,
            Boolean isInPits, Boolean isLeavingPits,
            float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition, float[] previousWorldPosition,
            float speed, float worldRecordLapTime, float worldRecordS1Time, float worldRecordS2Time, float worldRecordS3Time, 
            float distanceRoundTrack, Boolean isRaining, float trackTemp, float airTemp, CarData.CarClass carClass,
            Boolean sessionLengthIsTime, float sessionTimeRemaining, float lastSectorTime, Boolean lapInvalidated, float nearPitEntryPointDistance)
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
            opponentData.Speed = speed;
            opponentData.Position = racePosition;
            opponentData.UnFilteredPosition = racePosition;
            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.Position;
            }
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            opponentData.CarClass = carClass;
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
                            !lapInvalidated, isRaining, trackTemp, airTemp, sessionLengthIsTime, sessionTimeRemaining, 3);
                        
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
            if (participantName != null && participantName.Length > 0 && !participantName.StartsWith(NULL_CHAR_STAND_IN) && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            } 
            opponentData.Position = (int)participantStruct.mRacePosition;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = (int)participantStruct.mLapsCompleted;
            opponentData.CurrentSectorNumber = (int)participantStruct.mCurrentSector + 1;   // zero indexed
            opponentData.WorldPosition = new float[] { participantStruct.mWorldPosition[0], participantStruct.mWorldPosition[2] };
            opponentData.DistanceRoundTrack = participantStruct.mCurrentLapDistance;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.Now);
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
                    if (sessionState == (uint)eSessionState.SESSION_FORMATION_LAP)
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
                                if (!waitingForCarsToFinish.ContainsKey(opponent.DriverRawName)) {
                                    waitingForCarsToFinish.Add(opponent.DriverRawName, opponent.DistanceRoundTrack);
                                    running = true;
                                }
                                else if (waitingForCarsToFinish[opponent.DriverRawName] < opponent.DistanceRoundTrack)
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
                            Console.WriteLine("looks like session is finished - no activity in checkered phase");
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
                    if ((pitSchedule == (uint)ePitSchedule.PIT_SCHEDULE_PLAYER_REQUESTED || pitSchedule == (uint)ePitSchedule.PIT_SCHEDULE_ENGINEER_REQUESTED) &&
                        (pitMode == (uint)ePitMode.PIT_MODE_DRIVING_INTO_PITS || pitMode == (uint)ePitMode.PIT_MODE_IN_PIT))
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
    }
}
