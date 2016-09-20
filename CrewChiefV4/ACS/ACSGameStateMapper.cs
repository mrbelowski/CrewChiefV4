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
        public static String playerName = null;
        public static Boolean versionChecked = false;

        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();

        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;

        private float wornOutTyreWearLevel = 88f;

        // tyrewear values still needs a bit of fine tuning!
        private float scrubbedTyreWearPercent = 4f;
        private float minorTyreWearPercent = 20f;
        private float majorTyreWearPercent = 40f;
        private float wornOutTyreWearPercent = 80f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;
        private static string expectedVersion = "1.7";


        class splitTimes
        {
            private float currentSplitPoint = 0;
            private float nextSplitPoint = 0;
            private const float splitSpacing = 150;
            private Dictionary<float, DateTime> splitPoints = new Dictionary<float, DateTime>();
            public void setSplitPoints(float trackLength)
            {
                splitPoints.Clear();
                float totalGaps = 0;
                while (totalGaps < trackLength)
                {
                    totalGaps += splitSpacing;

                    if (totalGaps < trackLength - splitSpacing)
                    {
                        splitPoints.Add(totalGaps, DateTime.Now);
                    }
                    else
                    {
                        break;
                    }

                }
                splitPoints.Add(trackLength - 50, DateTime.Now);
            }

            public void setNextSplitPoint(float distanceRoundTrack, float speed)
            {
                foreach (KeyValuePair<float, DateTime> gap in splitPoints)
                {
                    if (gap.Key >= distanceRoundTrack)
                    {
                        if (currentSplitPoint != gap.Key)
                        {
                            nextSplitPoint = gap.Key;
                        }
                        break;
                    }
                }
                if (currentSplitPoint != nextSplitPoint || speed < 5)
                {
                    splitPoints[nextSplitPoint] = DateTime.Now;
                    currentSplitPoint = nextSplitPoint;
                }
            }

            public float getSplitTime(splitTimes playerGaps, Boolean behind)
            {
                TimeSpan splitTime = new TimeSpan(0);
                if (playerGaps.splitPoints.Count > 0 && splitPoints.Count > 0)
                {
                    if (behind)
                    {
                        splitTime = playerGaps.splitPoints[currentSplitPoint] - splitPoints[currentSplitPoint];
                    }
                    else
                    {
                        splitTime = playerGaps.splitPoints[playerGaps.currentSplitPoint] - splitPoints[playerGaps.currentSplitPoint];
                    }
                }
                return Math.Abs((float)splitTime.TotalSeconds);
            }
        }

        private Dictionary<String, splitTimes> opponentsSplits = new Dictionary<String, splitTimes>();

        private static splitTimes playerSplits = new splitTimes();

        private SpeechRecogniser speechRecogniser;

        public List<LapData> playerLapData = new List<LapData>();

        public void StartNewLap(int lapNumber, float gameTimeAtStart)
        {
            LapData thisLapData = new LapData();
            thisLapData.GameTimeAtLapStart = gameTimeAtStart;
            thisLapData.LapNumber = lapNumber;
            playerLapData.Add(thisLapData);
        }

        public float addPlayerLapdata(float cumulativeSectorTime, float gameTimeAtSectorEnd)
        {
            LapData lapData = playerLapData[playerLapData.Count - 1];
            if (cumulativeSectorTime <= 0)
            {
                cumulativeSectorTime = gameTimeAtSectorEnd - lapData.GameTimeAtLapStart;
            }
            float thisSectorTime = cumulativeSectorTime;
            int sectorNumber = 1;
            foreach (float sectorTime in lapData.SectorTimes)
            {
                sectorNumber++;
                thisSectorTime = thisSectorTime - sectorTime;
            }
            lapData.SectorTimes.Add(thisSectorTime);
            lapData.GameTimeAtSectorEnd.Add(gameTimeAtSectorEnd);

            return thisSectorTime;
        }

        public ACSGameStateMapper()
        {
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));

        }

        public void versionCheck(Object memoryMappedFileStruct)
        {

            AssettoCorsaShared shared = ((ACSSharedMemoryReader.ACSStructWrapper)memoryMappedFileStruct).data;
            String currentVersion = shared.acsStatic.smVersion;
            if (currentVersion.Length != 0 && versionChecked == false)
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
            AC_STATUS status = shared.acsGraphic.status;
            if (status == AC_STATUS.AC_OFF || status == AC_STATUS.AC_REPLAY || shared.acsChief.numVehicles <= 0)
            {
                return previousGameState;
            }
            acsVehicleInfo playerVehicle = shared.acsChief.vehicle[0];

            Boolean isOnline = getNameFromBytes(shared.acsChief.serverName).Length > 0;
            Boolean isSinglePlayerPracticeSession = shared.acsChief.numVehicles == 1 && !isOnline && shared.acsGraphic.session == AC_SESSION_TYPE.AC_PRACTICE;
            float distanceRoundTrack = spLineLengthToDistanceRoundTrack(playerVehicle.spLineLength, shared.acsStatic.trackSPlineLength);


            playerName = getNameFromBytes(playerVehicle.driverName);
            NameValidator.validateName(playerName);

            currentGameState.SessionData.CompletedLaps = (int)shared.acsGraphic.completedLaps;
            AC_SESSION_TYPE sessionType = shared.acsGraphic.session;

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
                currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
            }


            if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
            {
                CarData.CarClass newClass = CarData.getDefaultCarClass();
                if (newClass.carClassEnum != currentGameState.carClass.carClassEnum)
                {
                    currentGameState.carClass = newClass;
                    Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
                    brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                    // no tyre data in the block so get the default tyre types for this car
                    //defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                }
            }

            currentGameState.SessionData.SessionType = mapToSessionState(sessionType);

            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.LeaderHasFinishedRace = leaderHasFinished;


            int realTimeLeaderBoardValid = isCarRealTimeLeaderBoardValid(shared.acsChief.vehicle, shared.acsChief.numVehicles);
            AC_FLAG_TYPE currentFlag = shared.acsGraphic.flag;
            if (sessionType == AC_SESSION_TYPE.AC_PRACTICE || sessionType == AC_SESSION_TYPE.AC_QUALIFY)
            {
                currentGameState.SessionData.Position = playerVehicle.carLeaderboardPosition;
                currentGameState.SessionData.UnFilteredPosition = currentGameState.SessionData.Position;
            }
            else
            {
                // Prevent positsion change first 10 sec of race else we get invalid position report when crossing the start/finish line when the light turns green.
                // carRealTimeLeaderboardPosition does not allways provide valid data doing this fase.
                if ((previousGameState != null && previousGameState.SessionData.SessionRunningTime < 10 && shared.acsGraphic.session == AC_SESSION_TYPE.AC_RACE) ||
                    currentFlag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
                {
                    currentGameState.SessionData.Position = playerVehicle.carLeaderboardPosition;
                    currentGameState.SessionData.UnFilteredPosition = currentGameState.SessionData.Position;
                }
                else
                {
                    currentGameState.SessionData.Position = playerVehicle.carRealTimeLeaderboardPosition + realTimeLeaderBoardValid;
                    currentGameState.SessionData.UnFilteredPosition = currentGameState.SessionData.Position;
                }
            }

            currentGameState.SessionData.IsDisqualified = currentFlag == AC_FLAG_TYPE.AC_BLACK_FLAG;
            bool isInPits = shared.acsGraphic.isInPit == 1;
            int lapsCompleated = shared.acsGraphic.completedLaps;
            int numberOfSectorsOnTrack = shared.acsStatic.sectorCount;

            int numberOfLapsInSession = (int)shared.acsGraphic.numberOfLaps;

            float gameSessionTimeLeft = 0.0f;
            if (!Double.IsInfinity(shared.acsGraphic.sessionTimeLeft))
            {
                gameSessionTimeLeft = shared.acsGraphic.sessionTimeLeft / 1000f;
            }
            float sessionTimeRemaining = -1;
            if (numberOfLapsInSession == 0)
            {
                currentGameState.SessionData.SessionHasFixedTime = true;
                sessionTimeRemaining = isSinglePlayerPracticeSession ? (float)TimeSpan.FromHours(1).TotalSeconds - lastSessionRunningTime : gameSessionTimeLeft;
            }

            Boolean isCountDown = false;
            TimeSpan countDown = TimeSpan.FromSeconds(gameSessionTimeLeft);
            if (isOnline && (sessionType == AC_SESSION_TYPE.AC_RACE || sessionType == AC_SESSION_TYPE.AC_DRIFT || sessionType == AC_SESSION_TYPE.AC_DRAG))
            {
                isCountDown = countDown.TotalMilliseconds >= 0.01;
            }
            else
            {
                isCountDown = Math.Round(countDown.TotalMinutes) == 30 && countDown.Seconds < 10;
            }

            Boolean raceFinished = lapsCompleated == numberOfLapsInSession || (previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace && previousGameState.SessionData.IsNewLap);
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType, currentFlag, status, isCountDown, lastSessionPhase, sessionTimeRemaining, lastSessionTotalRunTime, isInPits, lapsCompleated, raceFinished);

            currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track
                + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength, shared.acsStatic.sectorCount);




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
                playerLapData.Clear();

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

                    if (currentGameState.SessionData.TrackDefinition.unknownTrack)
                    {
                        currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();
                    }

                }
                else if (currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1)
                {
                    Console.WriteLine("sessionTimeRemaining = " + sessionTimeRemaining + " lastSessionTimeRemaining = " + lastSessionTimeRemaining);
                }
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.SessionData.SessionStartPosition = (int)playerVehicle.carLeaderboardPosition;
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
                //defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);

                for (int i = 1; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];
                    if (participantStruct.isConnected == 1)
                    {
                        String participantName = getNameFromBytes(participantStruct.driverName).ToLower();
                        if (participantName != null && participantName.Length > 0)
                        {
                            CarData.CarClass opponentCarClass = CarData.getDefaultCarClass();
                            addOpponentForName(participantName, createOpponentData(participantStruct, false, opponentCarClass, shared.acsStatic.trackSPlineLength), currentGameState);
                            splitTimes opg = new splitTimes();
                            if (!opponentsSplits.ContainsKey(participantName))
                            {
                                opponentsSplits.Add(participantName, opg);
                                opponentsSplits[participantName].setSplitPoints(shared.acsStatic.trackSPlineLength);
                                opponentsSplits[participantName].setNextSplitPoint(0, 100);
                            }
                        }
                    }
                }

                currentGameState.SessionData.PlayerLapTimeSessionBest = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = -1;
                currentGameState.SessionData.OverallSessionBestLapTime = -1;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = -1;

                currentGameState.SessionData.TrackDefinition.setGapPoints();
                playerSplits.setSplitPoints(shared.acsStatic.trackSPlineLength);
                playerSplits.setNextSplitPoint(0, 100);
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
                            currentGameState.SessionData.SessionStartPosition = playerVehicle.carLeaderboardPosition;
                        }
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.acsChief.numVehicles;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength, shared.acsStatic.sectorCount);
                        if (currentGameState.SessionData.TrackDefinition.unknownTrack)
                        {
                            currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();
                        }
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        playerSplits.setSplitPoints(shared.acsStatic.trackSPlineLength);


                        currentGameState.carClass = CarData.getDefaultCarClass();
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        // no tyre data in the block so get the default tyre types for this car
                        //defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
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
                    currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionTotalRunTime - sessionTimeRemaining;
                    currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                }
                else
                {
                    currentGameState.SessionData.SessionRunningTime = (float)(currentGameState.Now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
                }

                currentGameState.SessionData.SectorNumber = getCurrentSector(currentGameState.SessionData.TrackDefinition, distanceRoundTrack);
                currentGameState.SessionData.IsNewSector = previousGameState == null || currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;
                currentGameState.SessionData.LapTimeCurrent = mapToFloatTime(shared.acsGraphic.iCurrentTime);

                if (currentGameState.SessionData.IsNewSector && playerLapData.Count > 0)
                {

                    float sectorTimeToUse = -1;
                    float lastLapTime = mapToFloatTime(shared.acsGraphic.iLastTime);
                    if (currentGameState.SessionData.SectorNumber == 1)
                    {
                        sectorTimeToUse = lastLapTime;
                    }
                    else
                    {
                        sectorTimeToUse = currentGameState.SessionData.LapTimeCurrent;
                    }

                    sectorTimeToUse = addPlayerLapdata(sectorTimeToUse, currentGameState.SessionData.SessionRunningTime);

                    if (currentGameState.SessionData.SectorNumber == 1 && shared.acsGraphic.numberOfLaps > 0)
                    {
                        currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[numberOfSectorsOnTrack];
                        currentGameState.SessionData.SessionTimesAtEndOfSectors[numberOfSectorsOnTrack] = currentGameState.SessionData.SessionRunningTime;
                        if (numberOfSectorsOnTrack == 3)
                        {
                            currentGameState.SessionData.LastSector3Time = sectorTimeToUse;

                            if (currentGameState.SessionData.LastSector3Time > 0 &&
                                (currentGameState.SessionData.PlayerBestSector3Time == -1 || currentGameState.SessionData.LastSector3Time < currentGameState.SessionData.PlayerBestSector3Time))
                            {
                                currentGameState.SessionData.PlayerBestSector3Time = currentGameState.SessionData.LastSector3Time;
                            }

                            if (lastLapTime > 0 &&
                                (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 || lastLapTime <= currentGameState.SessionData.PlayerLapTimeSessionBest))
                            {
                                currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.LastSector1Time;
                                currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.LastSector2Time;
                                currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.LastSector3Time;
                            }

                        }
                        else if (numberOfSectorsOnTrack == 2)
                        {

                            currentGameState.SessionData.LastSector2Time = sectorTimeToUse;

                            if (currentGameState.SessionData.LastSector2Time > 0 &&
                                (currentGameState.SessionData.PlayerBestSector2Time == -1 || currentGameState.SessionData.LastSector2Time < currentGameState.SessionData.PlayerBestSector2Time))
                            {
                                currentGameState.SessionData.PlayerBestSector2Time = currentGameState.SessionData.LastSector2Time;
                            }

                            if (lastLapTime > 0 &&
                                (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 || lastLapTime <= currentGameState.SessionData.PlayerLapTimeSessionBest))
                            {
                                currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.LastSector1Time;
                                currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.LastSector2Time;
                                currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.LastSector3Time;
                            }

                        }

                    }
                    else if (currentGameState.SessionData.SectorNumber == 2)
                    {

                        currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;
                        currentGameState.SessionData.LastSector1Time = sectorTimeToUse;

                        if (currentGameState.SessionData.LastSector1Time > 0 &&
                            (currentGameState.SessionData.PlayerBestSector1Time == -1 || currentGameState.SessionData.LastSector1Time < currentGameState.SessionData.PlayerBestSector1Time))
                        {
                            currentGameState.SessionData.PlayerBestSector1Time = currentGameState.SessionData.LastSector1Time;
                        }
                    }
                    if (currentGameState.SessionData.SectorNumber == 3)
                    {


                        currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                        currentGameState.SessionData.LastSector2Time = sectorTimeToUse;

                        if (currentGameState.SessionData.LastSector2Time > 0 &&
                            (currentGameState.SessionData.PlayerBestSector2Time == -1 || currentGameState.SessionData.LastSector2Time < currentGameState.SessionData.PlayerBestSector2Time))
                        {
                            currentGameState.SessionData.PlayerBestSector2Time = currentGameState.SessionData.LastSector2Time;
                        }
                    }
                }

                currentGameState.SessionData.Flag = mapToFlagEnum(currentFlag);
                currentGameState.SessionData.NumCars = shared.acsChief.numVehicles;

                currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                    (shared.acsGraphic.completedLaps == previousGameState.SessionData.CompletedLaps + 1 || ((lastSessionPhase == SessionPhase.Countdown)
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

                List<String> namesInRawData = new List<String>();
                for (int i = 0; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];

                    CarData.CarClass opponentCarClass = CarData.getDefaultCarClass();
                    String participantName = getNameFromBytes(participantStruct.driverName).ToLower();
                    namesInRawData.Add(participantName);
                    OpponentData currentOpponentData = getOpponentForName(currentGameState, participantName);

                    if (i != 0 && participantName != null && participantName.Length > 0)
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
                                    int currentOpponentSector = 0;
                                    float previousTimeDeltaBehind = 0.0f;
                                    Boolean previousOpponentIsEnteringPits = false;
                                    Boolean previousOpponentIsExitingPits = false;

                                    float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                                    float previousOpponentSpeed = 0;
                                    int currentOpponentRacePosition = 0;
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


                                    }
                                    float currentOpponentLapDistance = spLineLengthToDistanceRoundTrack(shared.acsStatic.trackSPlineLength, participantStruct.spLineLength);
                                    currentOpponentSector = getCurrentSector(currentGameState.SessionData.TrackDefinition, currentOpponentLapDistance);


                                    Boolean useCarLeaderBoardPosition = previousOpponentSectorNumber != currentOpponentSector && currentOpponentSector == 1;

                                    if (shared.acsGraphic.session == AC_SESSION_TYPE.AC_PRACTICE || shared.acsGraphic.session == AC_SESSION_TYPE.AC_QUALIFY
                                        || currentFlag == AC_FLAG_TYPE.AC_CHECKERED_FLAG || useCarLeaderBoardPosition)
                                    {
                                        currentOpponentRacePosition = participantStruct.carLeaderboardPosition;
                                    }
                                    else
                                    {
                                        currentOpponentRacePosition = participantStruct.carRealTimeLeaderboardPosition + realTimeLeaderBoardValid;
                                    }
                                    int currentOpponentLapsCompleted = participantStruct.lapCount;

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

                                    Boolean isEnteringPits = participantStruct.isCarInPitline == 1 && currentOpponentSector == numberOfSectorsOnTrack;
                                    Boolean isLeavingPits = participantStruct.isCarInPitline == 1 && currentOpponentSector == 1;

                                    if (isEnteringPits && !previousOpponentIsEnteringPits)
                                    {
                                        int opponentPositionAtLastSector = currentOpponentData.Position;
                                        LapData currentLapData = currentOpponentData.getCurrentLapData();

                                        if (currentLapData != null && currentLapData.SectorPositions.Count > numberOfSectorsOnTrack - 1)
                                        {
                                            opponentPositionAtLastSector = currentLapData.SectorPositions[numberOfSectorsOnTrack];
                                        }
                                        if (opponentPositionAtLastSector == 1)
                                        {
                                            currentGameState.PitData.LeaderIsPitting = true;
                                            currentGameState.PitData.OpponentForLeaderPitting = currentOpponentData;
                                        }
                                        if (currentGameState.SessionData.Position > 2 && opponentPositionAtLastSector == currentGameState.SessionData.Position - 1)
                                        {
                                            currentGameState.PitData.CarInFrontIsPitting = true;
                                            currentGameState.PitData.OpponentForCarAheadPitting = currentOpponentData;
                                        }
                                        if (!currentGameState.isLast() && opponentPositionAtLastSector == currentGameState.SessionData.Position + 1)
                                        {
                                            currentGameState.PitData.CarBehindIsPitting = true;
                                            currentGameState.PitData.OpponentForCarBehindPitting = currentOpponentData;
                                        }
                                    }

                                    if (currentOpponentRacePosition == currentGameState.SessionData.Position + 1 && !useCarLeaderBoardPosition)
                                    {
                                        if (opponentsSplits.ContainsKey(participantName))
                                        {
                                            float timeDeltaBehind =
                                            currentGameState.SessionData.TimeDeltaBehind = opponentsSplits[participantName].getSplitTime(playerSplits, true);
                                        }



                                    }
                                    if (currentOpponentRacePosition == currentGameState.SessionData.Position - 1 && !useCarLeaderBoardPosition)
                                    {
                                        if (opponentsSplits.ContainsKey(participantName))
                                        {
                                            currentGameState.SessionData.TimeDeltaFront = opponentsSplits[participantName].getSplitTime(playerSplits, false);
                                        }

                                    }
                                    if (opponentsSplits.ContainsKey(participantName))
                                    {
                                        opponentsSplits[participantName].setNextSplitPoint(currentOpponentLapDistance, participantStruct.speedMS);
                                    }



                                    float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;

                                    upateOpponentData(currentOpponentData, currentOpponentRacePosition, participantStruct.carLeaderboardPosition, currentOpponentLapsCompleted,
                                        currentOpponentSector, mapToFloatTime(participantStruct.currentLapTimeMS), mapToFloatTime(participantStruct.lastLapTimeMS),
                                        isEnteringPits || isLeavingPits, participantStruct.currentLapInvalid == 0,
                                        currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                        new float[] { participantStruct.worldPosition.x, participantStruct.worldPosition.z }, previousOpponentWorldPosition,
                                        currentOpponentLapDistance, 0, 0,
                                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                        currentGameState.carClass.carClassEnum, numberOfSectorsOnTrack, shared.acsPhysics.airTemp, shared.acsPhysics.roadTemp
                                        );


                                    if (currentOpponentData.IsNewLap)
                                    {
                                        //
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
                            if (participantName != null && participantName.Length > 0)
                            {
                                addOpponentForName(participantName, createOpponentData(participantStruct, true, opponentCarClass, shared.acsStatic.trackSPlineLength), currentGameState);
                                splitTimes opg = new splitTimes();
                                if (!opponentsSplits.ContainsKey(participantName))
                                {
                                    opponentsSplits.Add(participantName, opg);
                                    opponentsSplits[participantName].setSplitPoints(shared.acsStatic.trackSPlineLength);
                                    opg.setNextSplitPoint(0, 100);
                                }
                                //setOpponentGapPoints(participantName, currentGameState.SessionData.TrackDefinition.trackLength);
                            }
                        }
                    }

                }

                if (namesInRawData.Count() != shared.acsChief.numVehicles)
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
                                Console.WriteLine(opponentName + "->" + nameInRawData);
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
                playerSplits.setNextSplitPoint(distanceRoundTrack, playerVehicle.speedMS);
                // more to come here 
                currentGameState.SessionData.LapTimePrevious = mapToFloatTime(shared.acsGraphic.iLastTime);

                if (previousGameState != null && previousGameState.SessionData.CurrentLapIsValid && shared.acsStatic.penaltiesEnabled == 1)
                {
                    currentGameState.SessionData.CurrentLapIsValid = shared.acsPhysics.numberOfTyresOut < 3;
                }

                if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.PreviousLapWasValid = previousGameState != null && previousGameState.SessionData.CurrentLapIsValid;
                    currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious).ToString(@"mm\:ss\.fff"));
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.Position;
                    currentGameState.SessionData.CurrentLapIsValid = true;
                    StartNewLap(currentGameState.SessionData.CompletedLaps + 1, currentGameState.SessionData.SessionRunningTime);
                    currentGameState.SessionData.Position = playerVehicle.carLeaderboardPosition;
                    currentGameState.SessionData.UnFilteredPosition = playerVehicle.carLeaderboardPosition;
                    //currentGameState.displayOpponentData();
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

            // engine/transmission data
            currentGameState.EngineData.EngineRpm = shared.acsPhysics.rpms;
            currentGameState.EngineData.MaxEngineRpm = shared.acsStatic.maxRpm;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 2;

            currentGameState.FuelData.FuelCapacity = shared.acsStatic.maxFuel;
            currentGameState.FuelData.FuelLeft = shared.acsPhysics.fuel;
            currentGameState.FuelData.FuelUseActive = shared.acsStatic.aidFuelRate > 0;

            currentGameState.TransmissionData.Gear = shared.acsPhysics.gear - 1;

            currentGameState.ControlData.BrakePedal = shared.acsPhysics.brake;
            currentGameState.ControlData.ThrottlePedal = shared.acsPhysics.gas;
            currentGameState.ControlData.ClutchPedal = shared.acsPhysics.clutch;

            // penalty data
            currentGameState.PenaltiesData.HasDriveThrough = currentFlag == AC_FLAG_TYPE.AC_PENALTY_FLAG && shared.acsGraphic.penaltyTime <= 0;
            currentGameState.PenaltiesData.HasSlowDown = currentFlag == AC_FLAG_TYPE.AC_PENALTY_FLAG && shared.acsGraphic.penaltyTime > 0;

            // motion data
            currentGameState.PositionAndMotionData.CarSpeed = playerVehicle.speedMS;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = distanceRoundTrack;

            //pit data
            currentGameState.PitData.InPitlane = shared.acsGraphic.isInPitLane == 1;

            if (currentGameState.PitData.InPitlane)
            {
                if (currentGameState.SessionData.SectorNumber == numberOfSectorsOnTrack)
                {
                    currentGameState.PitData.OnInLap = true;
                    currentGameState.PitData.OnOutLap = false;
                }
                else if (currentGameState.SessionData.SectorNumber == 1)
                {

                    currentGameState.PitData.OnInLap = false;
                    currentGameState.PitData.OnOutLap = true;
                }
            }
            else if (currentGameState.SessionData.IsNewLap)
            {
                // starting a new lap while not in the pitlane so clear the in / out lap flags
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = false;
            }
            if (previousGameState != null && currentGameState.PitData.OnOutLap && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane)
            {
                currentGameState.PitData.IsAtPitExit = true;
            }

            //tyre data
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TireWearActive = shared.acsStatic.aidTireRate > 0;

            currentGameState.TyreData.FrontLeftTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.FrontLeftPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[0]);

            currentGameState.TyreData.FrontRightTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.FrontRightPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[1]);

            currentGameState.TyreData.RearLeftTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.RearLeftPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[2]);

            currentGameState.TyreData.RearRightTyreType = defaultTyreTypeForPlayersCar;
            currentGameState.TyreData.RearRightPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[3]);

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear,
            currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            //penalty data
            if (shared.acsStatic.penaltiesEnabled == 1)
            {
                currentGameState.PenaltiesData.IsOffRacingSurface = shared.acsPhysics.numberOfTyresOut > 2;
                if (!currentGameState.PitData.OnOutLap && previousGameState != null && !previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface &&
                    !(shared.acsGraphic.session == AC_SESSION_TYPE.AC_RACE && isCountDown))
                {
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
                }

            }

            if (playerVehicle.speedMS > 7 && currentGameState.carClass != null)
            {
                float minRotatingSpeed = 2 * (float)Math.PI * playerVehicle.speedMS / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[0]) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[1]) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[2]) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[3]) < minRotatingSpeed;

                float maxRotatingSpeed = 2 * (float)Math.PI * playerVehicle.speedMS / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[0]) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[1]) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[2]) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[3]) > maxRotatingSpeed;
            }


            //conditions
            if (currentGameState.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < currentGameState.Now)
            {
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.acsPhysics.airTemp, shared.acsPhysics.roadTemp, 0, 0, 0, 0, 0);
            }

            return currentGameState;
        }

        private SessionPhase mapToSessionPhase(SessionType sessionType, AC_FLAG_TYPE flag, AC_STATUS status, Boolean isCountdown,
            SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime, bool isInPitLane, int lapCount, Boolean raceIsFinished)
        {
            if (status == AC_STATUS.AC_PAUSE)
                return previousSessionPhase;

            if (sessionType == SessionType.Race)
            {
                if (isCountdown)
                {
                    return SessionPhase.Countdown;
                }
                else if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG && raceIsFinished)
                {
                    return SessionPhase.Finished;
                }
                else if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
                {
                    return SessionPhase.Checkered;
                }
                else if (!isCountdown)
                {
                    return SessionPhase.Green;
                }

                return previousSessionPhase;

            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.HotLap)
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
            else if (sessionType == SessionType.Qualify)
            {
                if (isInPitLane && lapCount == 0)
                {
                    return SessionPhase.Countdown;
                }
                else if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG && isInPitLane)
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

        private void upateOpponentData(OpponentData opponentData, int racePosition, int leaderBoardPosition, int completedLaps, int sector,
            float completedLapTime, float lastLapTime, Boolean isInPits, Boolean lapIsValid, float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition,
            float[] previousWorldPosition, float distanceRoundTrack, int tire_type, int carClassId, Boolean sessionLengthIsTime, float sessionTimeRemaining,
        CarData.CarClassEnum playerCarClass, int trackNumberOfSectors, float airTemperature, float trackTempreture)
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
            opponentData.UnFilteredPosition = racePosition;
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;

            if (opponentData.CurrentSectorNumber != sector)
            {

                opponentData.CarClass = CarData.getDefaultCarClass();
                if (opponentData.CurrentSectorNumber == trackNumberOfSectors && sector == 1)
                {
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        opponentData.CompleteLapWithProvidedLapTime(leaderBoardPosition, sessionRunningTime, lastLapTime,
                            lapIsValid && validSpeed, false, trackTempreture, airTemperature, sessionLengthIsTime, sessionTimeRemaining);
                    }

                    opponentData.StartNewLap(completedLaps + 1, leaderBoardPosition, isInPits, sessionRunningTime, false, trackTempreture, airTemperature);
                    opponentData.IsNewLap = true;

                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddCumulativeSectorData(racePosition, completedLapTime, sessionRunningTime, lapIsValid && validSpeed, false, trackTempreture, airTemperature);
                }
                opponentData.CurrentSectorNumber = sector;
            }
            opponentData.CompletedLaps = completedLaps;
            if (sector == trackNumberOfSectors && isInPits)
            {
                opponentData.setInLap();
            }
        }

        private OpponentData createOpponentData(acsVehicleInfo participantStruct, Boolean loadDriverName, CarData.CarClass carClass, float trackSplineLength)
        {
            OpponentData opponentData = new OpponentData();
            String participantName = getNameFromBytes(participantStruct.driverName).ToLower();
            opponentData.DriverRawName = participantName;
            opponentData.DriverNameSet = true;
            if (participantName != null && participantName.Length > 0 && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }

            opponentData.Position = (int)participantStruct.carLeaderboardPosition;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = (int)participantStruct.lapCount;
            opponentData.CurrentSectorNumber = 0;
            opponentData.WorldPosition = new float[] { participantStruct.worldPosition.x, participantStruct.worldPosition.z };
            opponentData.DistanceRoundTrack = spLineLengthToDistanceRoundTrack(trackSplineLength, participantStruct.spLineLength);
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
        //AC provides tyrewear data in the range from 100-88, start value is 99.5, from where it starts by going up to 100, and then it
        //drop to 88 over time/wear. Qual hint maby :)
        private float mapToPercentage(float level, float minimumIn, float maximumIn, float minimumOut, float maximumOut)
        {
            return (level - minimumIn) * (maximumOut - minimumOut) / (maximumIn - minimumIn) + minimumOut;
        }

        private float getTyreWearPercentage(float wearLevel)
        {
            if (wearLevel == -1)
            {
                return -1;
            }
            return Math.Min(100, mapToPercentage((wornOutTyreWearLevel / wearLevel) * 100, wornOutTyreWearLevel, 100, 0, 100));
        }

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            return SessionType.Unavailable;
        }

        private SessionType mapToSessionState(AC_SESSION_TYPE sessionState)
        {
            if (sessionState == AC_SESSION_TYPE.AC_RACE || sessionState == AC_SESSION_TYPE.AC_DRIFT || sessionState == AC_SESSION_TYPE.AC_DRAG)
            {
                return SessionType.Race;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_PRACTICE)
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

        private int getCurrentSector(TrackDefinition trackDef, float distanceRoundtrack)
        {

            int ret = 3;
            if (distanceRoundtrack >= 0 && distanceRoundtrack < trackDef.sectorPoints[0])
            {
                ret = 1;
            }
            if (distanceRoundtrack >= trackDef.sectorPoints[0] && distanceRoundtrack < trackDef.sectorPoints[1])
            {
                ret = 2;
            }
            //Console.WriteLine("CurrentSector:" + ret);
            return ret;
        }

        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
        }

        private float spLineLengthToDistanceRoundTrack(float trackLength, float spLine)
        {
            if (spLine < 0.0f)
            {
                spLine -= 1f;
                Console.WriteLine("Hmmmm");
            }


            return spLine * trackLength;
        }

        private int isCarRealTimeLeaderBoardValid(acsVehicleInfo[] vehicles, int numVehicles)
        {
            for (int i = 0; i < numVehicles; i++)
            {
                if (vehicles[i].carRealTimeLeaderboardPosition == 0)
                {
                    return 1;
                }
            }
            return 0;
        }

    }
}
