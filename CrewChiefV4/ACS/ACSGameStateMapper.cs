using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.assetto.assettoData;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */

namespace CrewChiefV4.assetto
{
    public class ACSGameStateMapper : GameStateMapper
    {

        public static String NULL_CHAR_STAND_IN = "?";
        public static String playerName = null;
        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);
        public static Boolean versionChecked = false;

        private static Boolean getPlayerByName = true;

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;

        private float scrubbedTyreWearPercent = 5f;
        private float minorTyreWearPercent = 30f;
        private float majorTyreWearPercent = 60f;
        private float wornOutTyreWearPercent = 85f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;
        private Dictionary<String, List<float>> opponentSpeedsWindow = new Dictionary<string, List<float>>();
        private int opponentSpeedsToAverage = 20;

        private static string expectedVersion = "1.7";
        private static bool gotSectorGaps = false;
        static float sector1Start = 0.0f;
        static float sector2Start = 0.0f;
        static float sector3Start = 0.0f;
        private SpeechRecogniser speechRecogniser;



        
        public ACSGameStateMapper()
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
            
            AssettoCorsaShared shared = ((ACSSharedMemoryReader.ACSStructWrapper)memoryMappedFileStruct).data;
            String currentVersion = shared.acsStatic.smVersion;
            if(currentVersion.Length != 0 && versionChecked == false)
            {
                Console.WriteLine(shared.acsStatic.smVersion);
                if (!currentVersion.Equals(expectedVersion, StringComparison.Ordinal))
                {
                    throw new GameDataReadException("Expected shared data version " + expectedVersion + " but got version " + currentVersion);
                }
                versionChecked = true;
            }

        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        public static OpponentData getOpponentForName(GameStateData gameState, String nameToFind)
        {
            if (gameState.OpponentData == null || gameState.OpponentData.Count == 0 || nameToFind == null || nameToFind.Length == 0)
            {
                return null;
            }

            if (gameState.OpponentData.ContainsKey(nameToFind))
            {
                return gameState.OpponentData[nameToFind];
            }
            return null;
        }

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            ACSSharedMemoryReader.ACSStructWrapper wrapper = (ACSSharedMemoryReader.ACSStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            AssettoCorsaShared shared = wrapper.data;

            if (shared.acsChief.numVehicles <= 0)
            {
                return previousGameState;
            }
            AC_STATUS status = shared.acsGraphic.status;
            if (status == AC_STATUS.AC_OFF || status == AC_STATUS.AC_REPLAY || status == AC_STATUS.AC_PAUSE )
                return previousGameState;

            acsVehicleInfo playerVehicle = shared.acsChief.vehicle[0];

            playerName = shared.acsStatic.playerName;
            NameValidator.validateName(playerName);
            currentGameState.SessionData.CompletedLaps = (int)shared.acsGraphic.completedLaps;
            currentGameState.SessionData.SectorNumber = (int)shared.acsGraphic.currentSectorIndex+1;
            currentGameState.SessionData.Position = (int)playerVehicle.carLeaderboardPosition;
            currentGameState.SessionData.UnFilteredPosition = (int)playerVehicle.carRealTimeLeaderboardPosition;
            currentGameState.SessionData.IsNewSector = previousGameState == null || currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;


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
            }
            if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
            {
                CarData.CarClass newClass = CarData.getCarClassForPCarsClassName(shared.acsStatic.carModel);
                if (newClass.carClassEnum != currentGameState.carClass.carClassEnum)
                {
                    currentGameState.carClass = newClass;
                    Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
                    brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                    // no tyre data in the block so get the default tyre types for this car
                    defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                }
            }

            currentGameState.SessionData.SessionType = mapToSessionType(shared);

            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.LeaderHasFinishedRace = leaderHasFinished;
            AC_FLAG_TYPE currentFlag = shared.acsGraphic.flag;
            currentGameState.SessionData.IsDisqualified = currentFlag == AC_FLAG_TYPE.AC_BLACK_FLAG;
            bool isInPits = shared.acsGraphic.isInPit == 1;
            int lapsCompleated = shared.acsGraphic.completedLaps;


            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType, currentFlag, status, shared.acsChief.isCountdown, lastSessionPhase, shared.acsGraphic.sessionTimeLeft, lastSessionTotalRunTime, isInPits, lapsCompleated);
            float sessionTimeRemaining = -1;
            int numberOfLapsInSession = (int)shared.acsGraphic.numberOfLaps;
            if (numberOfLapsInSession == 0)
            {
                currentGameState.SessionData.SessionHasFixedTime = true;
                sessionTimeRemaining = shared.acsGraphic.sessionTimeLeft / 1000;
            }
            List<String> opponentDriverNamesProcessedThisUpdate = new List<String>();
            currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track
                + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength);

            Boolean sessionOfSameTypeRestarted = ((currentGameState.SessionData.SessionType == SessionType.Race && lastSessionType == SessionType.Race) ||
                (currentGameState.SessionData.SessionType == SessionType.Practice && lastSessionType == SessionType.Practice) ||
                (currentGameState.SessionData.SessionType == SessionType.Qualify && lastSessionType == SessionType.Qualify)) &&
                (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.Finished) &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Countdown &&
                (currentGameState.SessionData.SessionType == SessionType.Race ||
                    currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1);

            if (sessionOfSameTypeRestarted ||
                (currentGameState.SessionData.SessionType != SessionType.Unavailable &&
                 currentGameState.SessionData.SessionPhase != SessionPhase.Finished &&
                    (lastSessionType != currentGameState.SessionData.SessionType ||
                        lastSessionTrack == null || lastSessionTrack.name != currentGameState.SessionData.TrackDefinition.name ||
                            (currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1))) && status != AC_STATUS.AC_PAUSE)
            
            
            
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
                    gotSectorGaps = false;
                }
                else if (currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1)
                {
                    Console.WriteLine("sessionTimeRemaining = " + sessionTimeRemaining + " lastSessionTimeRemaining = " + lastSessionTimeRemaining);
                }
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.SessionData.SessionStartPosition = (int)shared.acsGraphic.position;
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionTotalRunTime = sessionTimeRemaining;
                    currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                    if (currentGameState.SessionData.SessionTotalRunTime == 0)
                    {
                        Console.WriteLine("Setting session run time to 0");
                    }
                    Console.WriteLine("Time in this new session = " + sessionTimeRemaining);
                }
                currentGameState.SessionData.DriverRawName = playerName;
                currentGameState.PitData.IsRefuellingAllowed = true;


                //add carclasses for assetto corsa.
                currentGameState.carClass = CarData.getDefaultCarClass();

                Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
                brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                // no tyre data in the block so get the default tyre types for this car
                defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                for (int i = 1; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];
                    String participantName = participantStruct.driverName;
                    if (participantName != null && participantName.Length > 0)
                    {
                        CarData.CarClass opponentCarClass = CarData.getDefaultCarClass();
                        addOpponentForName(participantName, createOpponentData(participantStruct, false, opponentCarClass), currentGameState);
                    }
                }

                currentGameState.SessionData.PlayerLapTimeSessionBest = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = -1;
                currentGameState.SessionData.OverallSessionBestLapTime = -1;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = -1;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
            }
            else
            {
                Boolean justGoneGreen = false;
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        // just gone green, so get the session data.
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            justGoneGreen = true;
                            if (currentGameState.SessionData.SessionHasFixedTime)
                            {
                                currentGameState.SessionData.SessionTotalRunTime = sessionTimeRemaining;
                                currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                                if (currentGameState.SessionData.SessionTotalRunTime == 0)
                                {
                                    Console.WriteLine("Setting session run time to 0");
                                }
                            }
                            currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                            currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                            currentGameState.SessionData.SessionStartPosition = shared.acsGraphic.position;
                        }
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.acsChief.numVehicles;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength);
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        currentGameState.carClass = CarData.getDefaultCarClass();

                        Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
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
                if (!justGoneGreen && previousGameState != null)
                {
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
                } 
                //------------------- Variable session data ---------------------------
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionTotalRunTime - shared.acsGraphic.sessionTimeLeft / 1000;
                    currentGameState.SessionData.SessionTimeRemaining = shared.acsGraphic.sessionTimeLeft / 1000;
                }
                else
                {
                    currentGameState.SessionData.SessionRunningTime = (float)(currentGameState.Now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
                }
                if (currentGameState.SessionData.IsNewSector)
                {

                    if (!gotSectorGaps)
                        recordSectorGaps(playerVehicle.distanceRoundTrack, currentGameState.SessionData.SectorNumber, shared.acsStatic.sectorCount);
                    
                    float sectortime = mapToFloatTime(shared.acsGraphic.lastSectorTime);

                    if (currentGameState.SessionData.SectorNumber == 1)
                    {
                        currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;


                        currentGameState.SessionData.LastSector3Time = sectortime;
                        
                        if (currentGameState.SessionData.LastSector3Time > 0 &&
                            (currentGameState.SessionData.PlayerBestSector3Time == -1 || currentGameState.SessionData.LastSector3Time < currentGameState.SessionData.PlayerBestSector3Time))
                        {
                            currentGameState.SessionData.PlayerBestSector3Time = currentGameState.SessionData.LastSector3Time;
                        }
                        if (mapToFloatTime(shared.acsGraphic.iLastTime) > 0 &&
                            (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 || mapToFloatTime(shared.acsGraphic.iLastTime) <= currentGameState.SessionData.PlayerLapTimeSessionBest))
                        {
                            currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.LastSector1Time;
                            currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.LastSector2Time;
                            currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.LastSector3Time;
                        }
                    }
                    else if (currentGameState.SessionData.SectorNumber == 2)
                    {
                        
                        //Console.WriteLine("sector1 time:{0:D2}m:{1:D2}s:{2:D3}ms", ts.Minutes, ts.Seconds, ts.Milliseconds);
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;
                        // TODO: confirm that an invalid sector will put -1 in here...
                        currentGameState.SessionData.LastSector1Time = sectortime;
                        if (currentGameState.SessionData.LastSector1Time > 0 &&
                            (currentGameState.SessionData.PlayerBestSector1Time == -1 || currentGameState.SessionData.LastSector1Time < currentGameState.SessionData.PlayerBestSector1Time))
                        {
                            currentGameState.SessionData.PlayerBestSector1Time = currentGameState.SessionData.LastSector1Time;
                        }
                    }
                    if (currentGameState.SessionData.SectorNumber == 3)
                    {
                        
                        //Console.WriteLine("sector2 time:{0:D2}m:{1:D2}s:{2:D3}ms", ts.Minutes, ts.Seconds, ts.Milliseconds);
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;

                        currentGameState.SessionData.LastSector2Time = sectortime;
                        if (currentGameState.SessionData.LastSector2Time > 0 &&
                            (currentGameState.SessionData.PlayerBestSector2Time == -1 || currentGameState.SessionData.LastSector2Time < currentGameState.SessionData.PlayerBestSector2Time))
                        {
                            currentGameState.SessionData.PlayerBestSector2Time = currentGameState.SessionData.LastSector2Time;
                        }
                    }
                }
                currentGameState.SessionData.Flag = mapToFlagEnum(currentFlag);
                currentGameState.SessionData.NumCars = shared.acsChief.numVehicles;
                currentGameState.SessionData.CurrentLapIsValid = playerVehicle.currentLapInvalid != 1;

                currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                    (shared.acsGraphic.completedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                    ((lastSessionPhase == SessionPhase.Countdown || lastSessionPhase == SessionPhase.Formation || lastSessionPhase == SessionPhase.Garage)
                    && currentGameState.SessionData.SessionPhase == SessionPhase.Green));

      
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


                currentGameState.SessionData.LapTimeCurrent = mapToFloatTime(shared.acsGraphic.iCurrentTime);

                List<String> namesInRawData = new List<String>();
                for (int i = 1; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];
                    CarData.CarClass opponentCarClass = CarData.getDefaultCarClass();
                    String participantName = participantStruct.driverName.ToLower();
                    namesInRawData.Add(participantName);
                    OpponentData currentOpponentData = getOpponentForName(currentGameState, participantName);
                    if (participantName != null && participantName.Length > 0)
                    {
                        if (currentOpponentData != null)
                        {
                            if (participantStruct.isConnected == 1)
                            {
                                if (previousGameState != null)
                                {
                                    int previousOpponentSectorNumber = 1;
                                    int previousOpponentCompletedLaps = 0;
                                    int previousOpponentPosition = 0;
                                    float previousOpponentSector1Time = 0.0f;
                                    float previousOpponentSector2Time = 0.0f;
                                    float sectorTime = 0.0f;
                                    int currentOpponentSector = 0;
                                    Boolean previousOpponentIsEnteringPits = false;
                                    Boolean previousOpponentIsExitingPits = false;

                                    float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                                    float previousOpponentSpeed = 0;

                                    OpponentData previousOpponentData = getOpponentForName(previousGameState, participantName);
                                    
                                    if (previousOpponentData != null)
                                    {
                                        previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                        previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                        previousOpponentPosition = previousOpponentData.Position;
                                        previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                        previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                        previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                        previousOpponentSpeed = previousOpponentData.Speed;
                                        currentOpponentSector = getOpponantSector(participantStruct.distanceRoundTrack);
                                    }

                                    int currentOpponentRacePosition = (int)participantStruct.carRealTimeLeaderboardPosition+1;
                                    int currentOpponentLapsCompleted = (int)participantStruct.lapCount;

                                    if (currentOpponentSector == 0)
                                    {
                                        currentOpponentSector = previousOpponentSectorNumber;
                                    }

                                    float currentOpponentLapDistance = participantStruct.distanceRoundTrack;

                                    if (currentOpponentRacePosition == 1 && (currentGameState.SessionData.SessionNumberOfLaps > 0 &&
                                            currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted) ||
                                            (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining < 1 &&
                                            previousOpponentCompletedLaps < currentOpponentLapsCompleted))
                                    {
                                        currentGameState.SessionData.LeaderHasFinishedRace = true;
                                    }
                                    if (currentOpponentRacePosition == 1 && previousOpponentPosition > 1)
                                    {
                                        currentGameState.SessionData.HasLeadChanged = true;
                                    }
                                    
                                    Boolean isEnteringPits = participantStruct.isCarInPitline == 1 && currentOpponentSector == 3;
                                    Boolean isLeavingPits = participantStruct.isCarInPitline == 1 && currentOpponentSector == 1;

                                    if (isEnteringPits && !previousOpponentIsEnteringPits)
                                    {
                                        int opponentPositionAtSector3 = currentOpponentData.Position;
                                        LapData currentLapData = currentOpponentData.getCurrentLapData();
                                        if (currentLapData != null && currentLapData.SectorPositions.Count > 2)
                                        {
                                            opponentPositionAtSector3 = currentLapData.SectorPositions[2];
                                        }
                                        if (opponentPositionAtSector3 == 1)
                                        {
                                            currentGameState.PitData.LeaderIsPitting = true;
                                            currentGameState.PitData.OpponentForLeaderPitting = currentOpponentData;
                                        }
                                        if (currentGameState.SessionData.Position > 2 && opponentPositionAtSector3 == currentGameState.SessionData.Position - 1)
                                        {
                                            currentGameState.PitData.CarInFrontIsPitting = true;
                                            currentGameState.PitData.OpponentForCarAheadPitting = currentOpponentData;
                                        }
                                        if (!currentGameState.isLast() && opponentPositionAtSector3 == currentGameState.SessionData.Position + 1)
                                        {
                                            currentGameState.PitData.CarBehindIsPitting = true;
                                            currentGameState.PitData.OpponentForCarBehindPitting = currentOpponentData;
                                        }
                                    }

                                        

                                    float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;

                                    upateOpponentData(currentOpponentData, currentOpponentRacePosition,
                                        participantStruct.carRealTimeLeaderboardPosition+1, currentOpponentLapsCompleted,
                                        currentOpponentSector, sectorTime, mapToFloatTime(participantStruct.currentLapTimeMS), mapToFloatTime(participantStruct.lastLapTimeMS),
                                        isEnteringPits || isLeavingPits, participantStruct.currentLapInvalid == 0,
                                        currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                        new float[] { participantStruct.worldPosition.x, participantStruct.worldPosition.z }, previousOpponentWorldPosition,
                                        participantStruct.distanceRoundTrack, 0, 0,
                                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                        currentGameState.carClass.carClassEnum);

                                    if (currentOpponentData.IsNewLap)
                                    {
                                        currentGameState.displayOpponentData();
                                        if (currentOpponentData.CurrentBestLapTime > 0)
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
                                            if (currentOpponentData.CarClass.carClassEnum == currentGameState.carClass.carClassEnum)
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
                                    }
                                
                                }
                            }
                            else
                            {
                                currentOpponentData.IsActive = false;
                            }
                        }
                        else
                        {
                            if ( participantName != null && participantName.Length > 0)
                            {
                                addOpponentForName(participantName, createOpponentData(participantStruct, true, opponentCarClass), currentGameState);
                            }
                        }
                    }

                }

                if (namesInRawData.Count() == shared.acsChief.numVehicles)
                {
                    List<String> keysToRemove = new List<String>();
                    // purge any opponents that aren't in the current data
                    foreach (String opponentName in currentGameState.OpponentData.Keys)
                    {
                        Boolean matched = false;
                        foreach (String nameInRawData in namesInRawData)
                        {
                            if (nameInRawData.CompareTo(opponentName) == 0)
                            {
                                matched = true;
                                break;
                            }
                        }
                        if (!matched || !currentGameState.OpponentData[opponentName].IsActive)
                        {
                            keysToRemove.Add(opponentName);
                        }
                    }
                    foreach (String keyToRemove in keysToRemove)
                    {
                        currentGameState.OpponentData.Remove(keyToRemove);
                    }
                }


                //more to come here 
                currentGameState.SessionData.LapTimePrevious = mapToFloatTime(shared.acsGraphic.iLastTime);
                if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.PreviousLapWasValid = previousGameState != null && previousGameState.SessionData.CurrentLapIsValid;
                    currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(mapToFloatTime(shared.acsGraphic.iLastTime)).ToString(@"mm\:ss\.fff"));
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.Position;
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

            }
            return currentGameState;
        }

        private SessionPhase mapToSessionPhase(SessionType sessionType,  AC_FLAG_TYPE flag, AC_STATUS status, int isCountdown, 
            SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime, bool isInPitLane, int lapCount)
        {
            if(status == AC_STATUS.AC_PAUSE)
                return previousSessionPhase;
            if (sessionType == SessionType.Race)
            {
                if ( isCountdown == 1)
                {
                    return SessionPhase.Countdown;
                }
                else if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
                {
                    return SessionPhase.Checkered;
                }
                else if (flag == AC_FLAG_TYPE.AC_NO_FLAG && isCountdown == 0)
                {
                    return SessionPhase.Green;
                }
                return previousSessionPhase;

            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.Qualify || sessionType == SessionType.HotLap)
            {
                if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG || sessionTimeRemaining < 1)
                {
                    return SessionPhase.Finished;
                }
                if (isInPitLane && lapCount == 0)
                {
                    return SessionPhase.Countdown;
                }
                return SessionPhase.Green;

            }
            return SessionPhase.Unavailable;
            
        }

        private PitWindow mapToPitWindow(int pitWindow)
        {
            return PitWindow.Unavailable;  
        }

        private void upateOpponentData(OpponentData opponentData, int racePosition, int unfilteredRacePosition, int completedLaps, int sector, float sectorTime,
            float completedLapTime, float lastLapTime, Boolean isInPits, Boolean lapIsValid, float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition,
            float[] previousWorldPosition, float distanceRoundTrack, int tire_type, int carClassId, Boolean sessionLengthIsTime, float sessionTimeRemaining,
        CarData.CarClassEnum playerCarClass)
        {
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            float speed;
            Boolean validSpeed = true;
            speed = (float)Math.Sqrt(Math.Pow(currentWorldPosition[0] - previousWorldPosition[0], 2) + Math.Pow(currentWorldPosition[1] - previousWorldPosition[1], 2)) / secondsSinceLastUpdate;
            if (speed > 500)
            {
                // faster than 500m/s (1000+mph) suggests the player has quit to the pit. Might need to reassess this as the data are quite noisy
                validSpeed = false;
                opponentData.Speed = 0;
            }
            opponentData.Speed = speed;
            if (opponentData.Position != racePosition)
            {
                opponentData.SessionTimeAtLastPositionChange = sessionRunningTime;
            }
            opponentData.Position = racePosition;
            opponentData.UnFilteredPosition = unfilteredRacePosition;
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            
            if (opponentData.CurrentSectorNumber != sector)
            {
                
                opponentData.CarClass = CarData.getDefaultCarClass();
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {

                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, lastLapTime,
                            lapIsValid && validSpeed, false, 20, 20, sessionLengthIsTime, sessionTimeRemaining);
                    }

                    opponentData.StartNewLap(completedLaps+1, racePosition, isInPits, sessionRunningTime, false, 20, 20);
                    opponentData.IsNewLap = true;

                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddCumulativeSectorData(racePosition, completedLapTime, sessionRunningTime, lapIsValid && validSpeed, false, 20, 20);
                }
                opponentData.CurrentSectorNumber = sector;
            }
            opponentData.CompletedLaps = completedLaps;
            if (sector == 3 && isInPits)
            {
                opponentData.setInLap();
            }
        }

        private OpponentData createOpponentData(acsVehicleInfo participantStruct, Boolean loadDriverName, CarData.CarClass carClass)
        {
            OpponentData opponentData = new OpponentData();
            String participantName = participantStruct.driverName.ToLower();
            opponentData.DriverRawName = participantName;
            opponentData.DriverNameSet = true;
            if (participantName != null && participantName.Length > 0 && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }
            opponentData.Position = (int)participantStruct.carRealTimeLeaderboardPosition+1;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = (int)participantStruct.lapCount;
            opponentData.CurrentSectorNumber = 0;
            opponentData.WorldPosition = new float[] { participantStruct.worldPosition.x, participantStruct.worldPosition.z };
            opponentData.DistanceRoundTrack = participantStruct.distanceRoundTrack;
            opponentData.CarClass = carClass;
            opponentData.IsActive = true;
            String nameToLog = opponentData.DriverRawName == null ? "unknown" : opponentData.DriverRawName;
            Console.WriteLine("New driver " + nameToLog + " is using car class " + opponentData.CarClass.carClassEnum);
            return opponentData;
        }
        public static void addOpponentForName(String name, OpponentData opponentData, GameStateData gameState)
        {
            if (name == null || name.Length == 0)
            {
                return;
            }
            if (gameState.OpponentData == null)
            {
                gameState.OpponentData = new Dictionary<Object, OpponentData>();
            }
            if (gameState.OpponentData.ContainsKey(name))
            {
                gameState.OpponentData.Remove(name);
            }
            gameState.OpponentData.Add(name, opponentData);
        }
        public float mapToFloatTime(int time)
        {
            TimeSpan ts = TimeSpan.FromTicks(time);
            return (float)ts.TotalMilliseconds * 10;
        }
        public float mapOpponentFloatTime(int time)
        {
            TimeSpan ts = TimeSpan.FromMilliseconds(time);
            return (float)ts.TotalMilliseconds;
        }

       
        private FlagEnum mapToFlagEnum(AC_FLAG_TYPE flag)
        {
            if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
            {
                return FlagEnum.CHEQUERED;
            }
            else if (flag == AC_FLAG_TYPE.AC_BLACK_FLAG)
            {
                return FlagEnum.BLACK;
            }
            else if (flag == AC_FLAG_TYPE.AC_YELLOW_FLAG)
            {
                return FlagEnum.YELLOW;
            }
            else if (flag == AC_FLAG_TYPE.AC_WHITE_FLAG)
            {
                return FlagEnum.WHITE;
            }
            else if (flag == AC_FLAG_TYPE.AC_BLUE_FLAG)
            {
                return FlagEnum.BLUE;
            }
            else if (flag == AC_FLAG_TYPE.AC_NO_FLAG)
            {
                return FlagEnum.GREEN;
            }
            return FlagEnum.UNKNOWN;
        }
        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            AssettoCorsaShared shared = (AssettoCorsaShared)memoryMappedFileStruct;
            AC_SESSION_TYPE sessionState = shared.acsGraphic.session;
            if (sessionState == AC_SESSION_TYPE.AC_RACE || sessionState == AC_SESSION_TYPE.AC_DRIFT || sessionState == AC_SESSION_TYPE.AC_DRAG)
            {
                return SessionType.Race;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_PRACTICE )
            {
                return SessionType.Practice;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_QUALIFY)
            {
                return SessionType.Qualify;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_TIME_ATTACK || sessionState == AC_SESSION_TYPE.AC_HOTLAP)
            {
                return SessionType.HotLap;
            }
            else
            {
                return SessionType.Unavailable;   
            }
            
        }
        private TyreType mapToTyreType(int r3eTyreType, CarData.CarClassEnum carClass)
        {
            return TyreType.Unknown_Race;
        }
        private ControlType mapToControlType(int controlType)
        {
            return ControlType.Player;   
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
        //Assetto Corsa does not supply current sector information for other then the local player
        //So record them based on how far around track players lapdistans is when a sector changes.
        //its not ideal as sectors dont get updated before player has compleated sector 2 on first lap
        private void recordSectorGaps(float sectorStart, int sector, int numberOfTrackSectors)
        {
            if (sector == 2)
                sector2Start = sectorStart;
            if (sector == 3)
                sector3Start = sectorStart;

            if (numberOfTrackSectors == 2 && sector2Start != 0.0f)
                gotSectorGaps = true;
            if (numberOfTrackSectors == 3 && sector2Start != 0.0f && sector3Start != 0.0f)
                gotSectorGaps = true;

            Console.WriteLine("sector1:{0:F2} sector2:{1:F2} sector3:{2:F3} gotSectorGaps:{3:D}", sector1Start, sector2Start, sector3Start, gotSectorGaps);

        }
        private int getOpponantSector(float distanceRoundtrack)
        {
            if (!gotSectorGaps)
                return 0;

            if (distanceRoundtrack >= sector1Start && distanceRoundtrack < sector2Start)
                return 1;

            if (distanceRoundtrack >= sector2Start && distanceRoundtrack < sector3Start)
                return 2;

            return 3;

        }
        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        }


    }

   


}
