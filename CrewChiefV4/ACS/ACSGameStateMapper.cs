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
        private static bool setonce = false;

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
            if (status == AC_STATUS.AC_OFF || status == AC_STATUS.AC_PAUSE || status == AC_STATUS.AC_REPLAY)
                return previousGameState;

            
            playerName = shared.acsStatic.playerName;
            NameValidator.validateName(playerName);
            currentGameState.SessionData.CompletedLaps = (int)shared.acsGraphic.completedLaps;
            currentGameState.SessionData.SectorNumber = (int)shared.acsGraphic.currentSectorIndex;
            currentGameState.SessionData.Position = (int)shared.acsChief.vehicle[0].carLeaderboardPosition;
            currentGameState.SessionData.UnFilteredPosition = (int)shared.acsChief.vehicle[0].carRealTimeLeaderboardPosition;
            currentGameState.SessionData.IsNewSector = previousGameState == null || shared.acsGraphic.currentSectorIndex != previousGameState.SessionData.SectorNumber;


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

           /* if (setonce == false)
            {
                currentGameState.SessionData.IsNewSession = true;
                setonce = true;
            }*/

            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.LeaderHasFinishedRace = leaderHasFinished;
            AC_FLAG_TYPE currentFlag = shared.acsGraphic.flag;
            currentGameState.SessionData.IsDisqualified = currentFlag == AC_FLAG_TYPE.AC_BLACK_FLAG;
            
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType, currentFlag, shared.acsChief.isCountdown, lastSessionPhase, shared.acsGraphic.sessionTimeLeft , lastSessionTotalRunTime);
            float sessionTimeRemaining = -1;
            int numberOfLapsInSession = (int)shared.acsGraphic.numberOfLaps;
            if (numberOfLapsInSession == 0)
            {
                currentGameState.SessionData.SessionHasFixedTime = true;
                sessionTimeRemaining = shared.acsGraphic.sessionTimeLeft;
            }

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
                            (currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1))))
            
            
            
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

                currentGameState.carClass = CarData.getCarClassForPCarsClassName(shared.acsStatic.carModel);

                Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
                brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                // no tyre data in the block so get the default tyre types for this car
                defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                for (int i = 0; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];
                    String participantName = participantStruct.driverName;
                    if (i != 0 && participantName != null && participantName.Length > 0)
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
                            currentGameState.SessionData.SessionStartPosition = (int)shared.acsChief.vehicle[0].carRealTimeLeaderboardPosition;
                        }
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.acsChief.numVehicles;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength);
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        currentGameState.carClass = CarData.getCarClassForPCarsClassName(shared.acsStatic.carModel);

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
                
                }
                //------------------- Variable session data ---------------------------
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionTotalRunTime - shared.acsGraphic.sessionTimeLeft;
                    currentGameState.SessionData.SessionTimeRemaining = shared.acsGraphic.sessionTimeLeft;
                }
                else
                {
                    currentGameState.SessionData.SessionRunningTime = (float)(currentGameState.Now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
                }
            }
            return currentGameState;
        }

        private SessionPhase mapToSessionPhase(SessionType sessionType,  AC_FLAG_TYPE flag,int isCountdown, 
            SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime)
        {
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
                // yeah yeah....
                if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG || sessionTimeRemaining < 1)
                {
                    return SessionPhase.Finished;
                }
                return SessionPhase.Green;

            }
            return SessionPhase.Unavailable;
            
        }

        private PitWindow mapToPitWindow(int pitWindow)
        {
            return PitWindow.Unavailable;  
        }
        private OpponentData createOpponentData(acsVehicleInfo participantStruct, Boolean loadDriverName, CarData.CarClass carClass)
        {
            OpponentData opponentData = new OpponentData();
            String participantName = participantStruct.driverName.ToLower();
            opponentData.DriverRawName = participantName;
            opponentData.DriverNameSet = true;
            if (participantName != null && participantName.Length > 0 && !participantName.StartsWith(NULL_CHAR_STAND_IN) && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }
            opponentData.Position = (int)participantStruct.carRealTimeLeaderboardPosition;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = (int)participantStruct.lapCount;
            //opponentData.CurrentSectorNumber = (int)participantStruct.;
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

        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        }


    }

   


}