using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;


namespace CrewChiefV4.iRacing
{
    class iRacingGameStateMapper : GameStateMapper
    {
        public static String playerName = null;
        Driver playerCar = null;
        private SpeechRecogniser speechRecogniser;
        public iRacingGameStateMapper()
        {

        }

        public void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in r3e shared data so this is a no-op
        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        Dictionary<string, DateTime> lastActiveTimeForOpponents = new Dictionary<string, DateTime>();
        DateTime nextOpponentCleanupTime = DateTime.MinValue;
        TimeSpan opponentCleanupInterval = TimeSpan.FromSeconds(2);
        string prevTrackSurface = "";
        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper wrapper = (CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            Sim shared = wrapper.data;

            if (memoryMappedFileStruct == null)
            {
                return null;
            }

            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;

            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionType = previousGameState.SessionData.SessionType;
                currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                currentGameState.SessionData.OverallSessionBestLapTime = previousGameState.SessionData.OverallSessionBestLapTime;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = previousGameState.SessionData.PlayerClassSessionBestLapTime;
                currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
                currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                currentGameState.readLandmarksForThisLap = previousGameState.readLandmarksForThisLap;
            }
            currentGameState.SessionData.SessionType = mapToSessionType(shared.SessionData.SessionType);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Telemetry.SessionTime;
            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;
            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;

            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase, shared.Telemetry.SessionState, currentGameState.SessionData.SessionType, shared.Telemetry.IsReplayPlaying,
                (float)shared.Telemetry.SessionTime, previousLapsCompleted, shared.Telemetry.Lap, shared.Telemetry.SessionFlags, shared.Telemetry.IsInGarage,
                shared.SessionData.IsLimitedTime, shared.Telemetry.SessionTimeRemain, shared.Telemetry.SessionTime);

            currentGameState.SessionData.NumCarsAtStartOfSession = shared.Drivers.Count;

            int sessionNumber = shared.Telemetry.SessionNum;
            int PlayerCarIdx = shared.Telemetry.PlayerCarIdx;

            Boolean justGoneGreen = false;
            if (shared.Driver != null)
            {
                playerCar = shared.Driver;
            }
            /*
            if (!prevTrackSurface.Equals(playerCar.Live.TrackSurface.ToString()))
            {
                Console.WriteLine(playerCar.Live.TrackSurface.ToString());
                prevTrackSurface = playerCar.Live.TrackSurface.ToString();
            }*/
            Boolean sessionOfSameTypeRestarted = ((currentGameState.SessionData.SessionType == SessionType.Race && lastSessionType == SessionType.Race) ||
                (currentGameState.SessionData.SessionType == SessionType.Practice && lastSessionType == SessionType.Practice) ||
                (currentGameState.SessionData.SessionType == SessionType.Qualify && lastSessionType == SessionType.Qualify)) &&
                ((lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow) || lastSessionPhase == SessionPhase.Finished) &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown);

            if (sessionOfSameTypeRestarted || currentGameState.SessionData.SessionType != SessionType.Unavailable
                && lastSessionPhase != SessionPhase.Countdown
                && lastSessionType != currentGameState.SessionData.SessionType)
            {
                currentGameState.SessionData.IsNewSession = true;
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("sessionType = " + currentGameState.SessionData.SessionType);
                Console.WriteLine("lastSessionPhase = " + lastSessionPhase);
                Console.WriteLine("lastSessionRunningTime = " + lastSessionRunningTime);
                Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("rawSessionPhase = " + shared.Telemetry.SessionState);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                Console.WriteLine("NumCarsAtStartOfSession = " + currentGameState.SessionData.NumCarsAtStartOfSession);

                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;

                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName, 0, (float)shared.SessionData.Track.Length * 1000);

                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(shared.SessionData.Track.CodeName, currentGameState.SessionData.TrackDefinition.trackLength);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);

                currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                if (currentGameState.SessionData.SessionType == SessionType.Race)
                {
                    currentGameState.SessionData.SessionStartPosition = playerCar.CurrentResults.QualifyingPosition;
                }
                else
                {
                    currentGameState.SessionData.SessionStartPosition = playerCar.Live.Position;
                }
                Console.WriteLine("SessionStartPosition = " + currentGameState.SessionData.SessionStartPosition);
                currentGameState.PitData.IsRefuellingAllowed = true;
                currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;
                
                if (shared.SessionData.IsLimitedTime)
                {
                    currentGameState.SessionData.SessionHasFixedTime = true;
                    currentGameState.SessionData.SessionTotalRunTime = (float)shared.Telemetry.SessionTimeRemain;
                    Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                }

                lastActiveTimeForOpponents.Clear();
                nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;

                String driverName = playerCar.Name.ToLower();
                if (playerName == null)
                {
                    NameValidator.validateName(driverName);
                    playerName = driverName;
                }

                currentGameState.PitData.InPitlane = shared.Telemetry.OnPitRoad;
                currentGameState.PositionAndMotionData.DistanceRoundTrack = playerCar.Live.CorrectedLapDistance * currentGameState.SessionData.TrackDefinition.trackLength;
                //TODO update car classes
                currentGameState.carClass = CarData.getCarClassForIRacingId(playerCar.Car.CarClassId);
                CarData.IRACING_CLASS_ID = playerCar.Car.CarClassId;
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier() + " (class ID " + playerCar.Car.CarClassId + ")");

                Utilities.TraceEventClass(currentGameState);
                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
                currentGameState.SessionData.SectorNumber = playerCar.Live.CurrentFakeSector;
                foreach (Driver driver in shared.Drivers)
                {
                    driverName = driver.Name.ToLower();
                    if (driver.Id == PlayerCarIdx || driver.CurrentResults.IsOut || driver.IsPacecar || driver.Live.TrackSurface.HasFlag(TrackSurfaces.NotInWorld))
                    {
                        continue;
                    }
                    else
                    {
                        currentGameState.OpponentData.Add(driverName, createOpponentData(driver, driverName,
                        false, CarData.getCarClassForIRacingId(driver.Car.CarClassId).carClassEnum, currentGameState.SessionData.TrackDefinition.trackLength));
                    }

                }
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + lastSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        justGoneGreen = true;
                        // just gone green, so get the session data
                        
                        if (shared.SessionData.IsLimitedTime)
                        {                            
                            currentGameState.SessionData.SessionHasFixedTime = true;
                            currentGameState.SessionData.SessionTotalRunTime = (float)shared.SessionData.RaceTime;
                        }
                        else
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);


                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName, 0, (float)shared.SessionData.Track.Length * 1000);

                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(shared.SessionData.Track.CodeName, currentGameState.SessionData.TrackDefinition.trackLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                        currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);

                        currentGameState.carClass = CarData.getCarClassForIRacingId(playerCar.Car.CarClassId);
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);

                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());

                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                        }
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;


                        lastActiveTimeForOpponents.Clear();
                        nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;


                        //currentGameState.SessionData.CompletedLaps = shared.Driver.CurrentResults.LapsComplete;

                        Console.WriteLine("Just gone green, session details...");

                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
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
                        Console.WriteLine("TrackName " + trackName + " Track Reported Length " + currentGameState.SessionData.TrackDefinition.trackLength);

                    }
                }
                if (!justGoneGreen && previousGameState != null)
                {
                    //Console.WriteLine("regular update, session type = " + currentGameState.SessionData.SessionType + " phase = " + currentGameState.SessionData.SessionPhase);

                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
                    currentGameState.SessionData.HasExtraLap = previousGameState.SessionData.HasExtraLap;
                    currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                    currentGameState.SessionData.NumCarsAtStartOfSession = previousGameState.SessionData.NumCarsAtStartOfSession;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = previousGameState.SessionData.PositionAtStartOfCurrentLap;
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
                    currentGameState.PitData.NumPitStops = previousGameState.PitData.NumPitStops;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
                    currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                    currentGameState.carClass = previousGameState.carClass;
                    currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre = previousGameState.SessionData.PlayerClassSessionBestLapTimeByTyre;
                    currentGameState.SessionData.PlayerBestLapTimeByTyre = previousGameState.SessionData.PlayerBestLapTimeByTyre;
                    currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                    currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState.SessionData.SessionTimesAtEndOfSectors;
                    currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = previousGameState.SessionData.LapTimePreviousEstimateForInvalidLap;
                    currentGameState.SessionData.OverallSessionBestLapTime = previousGameState.SessionData.OverallSessionBestLapTime;
                    currentGameState.SessionData.PlayerClassSessionBestLapTime = previousGameState.SessionData.PlayerClassSessionBestLapTime;
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
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.SessionData.CompletedLaps = previousGameState.SessionData.CompletedLaps;
                    currentGameState.FlagData.useImprovisedIncidentCalling = previousGameState.FlagData.useImprovisedIncidentCalling;
                    currentGameState.SessionData.DeltaTime.deltaPoints = previousGameState.SessionData.DeltaTime.deltaPoints;
                    currentGameState.SessionData.DeltaTime.currentDeltaPoint = previousGameState.SessionData.DeltaTime.currentDeltaPoint;
                    currentGameState.SessionData.DeltaTime.nextDeltaPoint = previousGameState.SessionData.DeltaTime.currentDeltaPoint;
                    currentGameState.SessionData.DeltaTime.lapsCompleted = previousGameState.SessionData.DeltaTime.lapsCompleted;
                    currentGameState.SessionData.DeltaTime.totalDistanceTravelled = previousGameState.SessionData.DeltaTime.totalDistanceTravelled;
                    currentGameState.SessionData.DeltaTime.trackLength = previousGameState.SessionData.DeltaTime.trackLength;
                }
            }

            currentGameState.ControlData.ThrottlePedal = shared.Telemetry.Throttle;
            currentGameState.ControlData.ClutchPedal = shared.Telemetry.Clutch;
            currentGameState.ControlData.BrakePedal = shared.Telemetry.Brake;
            currentGameState.TransmissionData.Gear = shared.Telemetry.Gear;

            //TODO add yellow 
            if (shared.Telemetry.SessionFlags.HasFlag(SessionFlags.Black) && !shared.Telemetry.SessionFlags.HasFlag(SessionFlags.Furled))
            {
                currentGameState.PenaltiesData.HasStopAndGo = true;
            }
            if (shared.Telemetry.SessionFlags.HasFlag(SessionFlags.Black) && shared.Telemetry.SessionFlags.HasFlag(SessionFlags.Furled))
            {
                currentGameState.PenaltiesData.HasSlowDown = true;
            }
            if (shared.Telemetry.SessionFlags.HasFlag(SessionFlags.Yellow))
            {
                currentGameState.FlagData.isLocalYellow = true;
            }

            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;

            currentGameState.SessionData.LapTimePrevious = (float)playerCar.Live.LapTimePrevious;

            currentGameState.SessionData.PreviousLapWasValid = playerCar.Live.PreviousLapWasValid;


            currentGameState.SessionData.CompletedLaps = playerCar.Live.LapsCompleted;
            //TODO validate laptimes
            currentGameState.SessionData.LapTimeCurrent = shared.Telemetry.LapCurrentLapTime;
            currentGameState.SessionData.CurrentLapIsValid = true;

            currentGameState.SessionData.NumCars = shared.Drivers.Count;

            
            if(currentGameState.SessionData.SessionPhase == SessionPhase.Countdown && currentGameState.SessionData.SessionType == SessionType.Race)
            {
                currentGameState.SessionData.Position = playerCar.CurrentResults.QualifyingPosition;
            }
            else
            {
                currentGameState.SessionData.Position = playerCar.Live.Position;
            }

            currentGameState.SessionData.UnFilteredPosition = playerCar.CurrentResults.Position;

            Driver fastestPlayerClassDriver = shared.Drivers.OrderBy(d => d.CurrentResults.FastestTime).Where(e => e.Car.CarClassId == playerCar.Car.CarClassId && e.CurrentResults.FastestTime > 1).FirstOrDefault();
            if (fastestPlayerClassDriver != null)
            {
                currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = fastestPlayerClassDriver.CurrentResults.FastestTime;
            }
                


            //Console.WriteLine("fastest " + currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass);
            
            if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                currentGameState.SessionData.OverallSessionBestLapTime > shared.Telemetry.LapBestLapTime)
            {
                currentGameState.SessionData.OverallSessionBestLapTime = shared.Telemetry.LapBestLapTime;
            }

            if (previousGameState != null && !currentGameState.SessionData.IsNewSession)
            {
                currentGameState.OpponentData = previousGameState.OpponentData;
                currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
            }
            if (currentGameState.SessionData.Position == 1)
            {
                currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
            }

            int currentSector = playerCar.Live.CurrentFakeSector;
            
            currentGameState.SessionData.IsNewSector = currentGameState.SessionData.SectorNumber != currentSector;
            currentGameState.SessionData.IsNewLap = currentGameState.HasNewLapData(previousGameState, playerCar.Live.LapTimePrevious, currentSector) ||
                ((lastSessionPhase == SessionPhase.Countdown) && (currentGameState.SessionData.SessionPhase == SessionPhase.Green ||
                currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow));

            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.readLandmarksForThisLap = false;
            }

            currentGameState.SessionData.SectorNumber = currentSector;

            if (currentGameState.SessionData.IsNewSector || currentGameState.SessionData.IsNewLap)
            {
                if (currentSector == 1 && currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;
                    float sectorTime = (float)playerCar.CurrentResults.FakeSector3.SectorTime;
                    if (sectorTime > 0 && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                    {
                        Console.WriteLine("sector 3 time: " + TimeSpan.FromSeconds(sectorTime).ToString(@"mm\:ss\.fff"));
                        currentGameState.SessionData.LastSector3Time = sectorTime;
                        if (currentGameState.SessionData.PlayerBestSector3Time == -1 || currentGameState.SessionData.LastSector3Time < currentGameState.SessionData.PlayerBestSector3Time)
                        {
                            currentGameState.SessionData.PlayerBestSector3Time = currentGameState.SessionData.LastSector3Time;
                        }
                        if (currentGameState.SessionData.LapTimePrevious > 1 &&
                            (currentGameState.SessionData.PlayerLapTimeSessionBest == -1 || currentGameState.SessionData.LapTimePrevious <= currentGameState.SessionData.PlayerLapTimeSessionBest))
                        {
                            currentGameState.SessionData.PlayerBestLapSector1Time = currentGameState.SessionData.LastSector1Time;
                            currentGameState.SessionData.PlayerBestLapSector2Time = currentGameState.SessionData.LastSector2Time;
                            currentGameState.SessionData.PlayerBestLapSector3Time = currentGameState.SessionData.LastSector3Time;
                        }
                    }
                    else
                    {
                        currentGameState.SessionData.LastSector3Time = -1;
                    }
                }
                else if (currentSector == 2)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;
                    float sectorTime = (float)playerCar.CurrentResults.FakeSector1.SectorTime;

                    if (sectorTime > 0 && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                    {
                        Console.WriteLine("sector 1 time: " + TimeSpan.FromSeconds(sectorTime).ToString(@"mm\:ss\.fff"));
                        currentGameState.SessionData.LastSector1Time = sectorTime;
                        if (currentGameState.SessionData.PlayerBestSector1Time == -1 || currentGameState.SessionData.LastSector1Time < currentGameState.SessionData.PlayerBestSector1Time)
                        {
                            currentGameState.SessionData.PlayerBestSector1Time = currentGameState.SessionData.LastSector1Time;
                        }
                    }
                    else
                    {
                        currentGameState.SessionData.LastSector1Time = -1;
                    }
                    currentGameState.SessionData.playerAddCumulativeSectorData(1, currentGameState.SessionData.Position, shared.Telemetry.LapCurrentLapTime,
                        currentGameState.SessionData.SessionRunningTime, true, false, shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp);

                }
                else if (currentSector == 3)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                    float sectorTime = (float)playerCar.CurrentResults.FakeSector2.SectorTime;

                    if (sectorTime > 0 && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                    {
                        Console.WriteLine("sector 2 time: " + TimeSpan.FromSeconds(sectorTime).ToString(@"mm\:ss\.fff"));
                        currentGameState.SessionData.LastSector2Time = sectorTime;
                        if (currentGameState.SessionData.PlayerBestSector2Time == -1 || currentGameState.SessionData.LastSector2Time < currentGameState.SessionData.PlayerBestSector2Time)
                        {
                            currentGameState.SessionData.PlayerBestSector2Time = currentGameState.SessionData.LastSector2Time;
                        }
                    }
                    else
                    {
                        currentGameState.SessionData.LastSector2Time = -1;
                    }
                    currentGameState.SessionData.playerAddCumulativeSectorData(2, currentGameState.SessionData.Position, shared.Telemetry.LapCurrentLapTime,
                        currentGameState.SessionData.SessionRunningTime, true, false, shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp);
                }

            }
            
            currentGameState.PitData.InPitlane = shared.Telemetry.CarIdxOnPitRoad[PlayerCarIdx];
            currentGameState.PositionAndMotionData.DistanceRoundTrack = currentGameState.SessionData.TrackDefinition.trackLength * playerCar.Live.CorrectedLapDistance;
            currentGameState.PositionAndMotionData.CarSpeed = (float)playerCar.Live.Speed;

            currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CompletedLaps,
                (float)playerCar.Live.Speed, currentGameState.Now);

            if (previousGameState != null)
            {
                String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                    currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.PositionAndMotionData.CarSpeed);
                currentGameState.SessionData.stoppedInLandmark = shared.Telemetry.OnPitRoad ? null : stoppedInLandmark;
            }

            if (currentGameState.PitData.InPitlane)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionRunningTime > 10 &&
                    previousGameState != null && !previousGameState.PitData.InPitlane)
                {
                    currentGameState.PitData.NumPitStops++;
                }
                if (currentSector == 3)
                {
                    currentGameState.PitData.OnInLap = true;
                    currentGameState.PitData.OnOutLap = false;
                }
                else if (currentSector == 1)
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
            if (playerCar.Live.HasCrossedSFLine)
            {
                currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
            }

            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.Position;
                currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(playerCar.Live.LapTimePrevious).ToString(@"mm\:ss\.fff"));
                Console.WriteLine(TimeSpan.FromSeconds(playerCar.Live.LapTimePrevious).ToString(@"mm\:ss\.fff"));


                currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.Position, currentGameState.SessionData.SessionRunningTime,
                    playerCar.Live.LapTimePrevious, currentGameState.SessionData.PreviousLapWasValid, false, shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp,
                    currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3);

                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                    currentGameState.SessionData.Position, currentGameState.PitData.InPitlane, currentGameState.SessionData.SessionRunningTime, false,
                    shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp);

                if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                {
                    currentGameState.carClass = CarData.getCarClassForIRacingId(playerCar.Car.CarClassId);
                }

            }

            foreach (Driver driver in shared.Drivers)
            {
                if (driver.Id == PlayerCarIdx || driver.CurrentResults.IsOut || driver.Live.TrackSurface.HasFlag(TrackSurfaces.NotInWorld) || driver.IsPacecar)
                {
                    continue;
                }
                String driverName = driver.Name.ToLower();
                lastActiveTimeForOpponents[driverName] = currentGameState.Now;
                if (currentGameState.OpponentData.ContainsKey(driverName))
                {
                    if (previousGameState != null)
                    {
                        OpponentData previousOpponentData = null;
                        Boolean newOpponentLap = false;
                        int previousOpponentSectorNumber = 1;
                        int previousOpponentCompletedLaps = 0;
                        int previousOpponentPosition = 0;
                        Boolean previousOpponentIsEnteringPits = false;
                        Boolean previousOpponentIsExitingPits = false;
                        float previousOpponentSpeed = 0;
                        float previousDistanceRoundTrack = 0;
                        bool previousIsInPits = false;
                        bool hasCrossedSFLine = false;
                        if (previousGameState.OpponentData.ContainsKey(driverName))
                        {
                            previousOpponentData = previousGameState.OpponentData[driverName];
                            previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                            previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                            previousOpponentPosition = previousOpponentData.Position;
                            previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                            previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                            previousOpponentSpeed = previousOpponentData.Speed;

                            previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                            previousIsInPits = previousOpponentData.InPits;
                        }

                        hasCrossedSFLine = driver.Live.HasCrossedSFLine;
                        int currentOpponentSector = driver.Live.CurrentFakeSector;
                        OpponentData currentOpponentData = currentGameState.OpponentData[driverName];

                        //reset to to pitstall
                        bool currentOpponentLapValid = true;

                        /*if (driver.PitInfo.InPitStall && !previousOpponentIsEnteringPits)
                        {
                            currentOpponentLapValid = false;
                        }*/
                        currentOpponentLapValid = driver.Live.PreviousLapWasValid;

                        float sectorTime = -1;
                        if (currentOpponentSector == 1)
                        {
                            sectorTime = (float)driver.CurrentResults.FakeSector3.SectorTime;
                        }
                        else if (currentOpponentSector == 2)
                        {
                            sectorTime = (float)driver.CurrentResults.FakeSector1.SectorTime;
                        }
                        else if (currentOpponentSector == 3)
                        {
                            sectorTime = (float)driver.CurrentResults.FakeSector2.SectorTime;
                        }
                        int currentOpponentRacePosition = driver.Live.Position;
                        int currentOpponentLapsCompleted = driver.Live.LapsCompleted;

                        if (currentOpponentSector == 0)
                        {
                            currentOpponentSector = previousOpponentSectorNumber;
                        }
                        float currentOpponentLapDistance = currentGameState.SessionData.TrackDefinition.trackLength * driver.Live.CorrectedLapDistance;
                        //Console.WriteLine("lapdistance:" + currentOpponentLapDistance);
                        currentOpponentData.DeltaTime.SetNextDeltaPoint(currentOpponentLapDistance, currentOpponentLapsCompleted, (float)driver.Live.Speed, currentGameState.Now);

                        Boolean finishedAllottedRaceLaps = currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted;
                        Boolean finishedAllottedRaceTime = false;

                        if (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
                            previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                        {
                            finishedAllottedRaceTime = true;
                        }
                        if (currentOpponentRacePosition == 1 && (finishedAllottedRaceTime || finishedAllottedRaceLaps))
                        {
                            currentGameState.SessionData.LeaderHasFinishedRace = true;
                        }
                        if (currentGameState.SessionData.Position == 1)
                        {
                            currentGameState.SessionData.LeaderSectorNumber = currentOpponentSector;
                        }
                        if (currentOpponentRacePosition == 1 && previousOpponentPosition > 1)
                        {
                            currentGameState.SessionData.HasLeadChanged = true;
                        }

                        Boolean isEnteringPits = shared.Telemetry.CarIdxOnPitRoad[driver.Id] && currentOpponentSector == 3;
                        Boolean isLeavingPits = shared.Telemetry.CarIdxOnPitRoad[driver.Id] && currentOpponentSector == 1;

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

                        if (currentOpponentRacePosition == currentGameState.SessionData.Position + 1 && currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            currentGameState.SessionData.TimeDeltaBehind = currentOpponentData.DeltaTime.GetAbsoluteTimeDeltaAllowingForLapDifferences(currentGameState.SessionData.DeltaTime);
                        }
                        if (currentOpponentRacePosition == currentGameState.SessionData.Position - 1 && currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            currentGameState.SessionData.TimeDeltaFront = currentOpponentData.DeltaTime.GetAbsoluteTimeDeltaAllowingForLapDifferences(currentGameState.SessionData.DeltaTime);
                        }

                        upateOpponentData(currentOpponentData,previousOpponentData, currentOpponentRacePosition,
                                 currentOpponentRacePosition, currentOpponentLapsCompleted,
                                 currentOpponentSector, sectorTime, driver.Live.CurrentLapTime, (float)driver.Live.LapTimePrevious, newOpponentLap,
                                 shared.Telemetry.CarIdxOnPitRoad[driver.Id], previousIsInPits, currentOpponentLapValid, currentGameState.SessionData.SessionRunningTime, currentOpponentLapDistance,
                                 currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                 currentGameState.SessionData.SessionType == SessionType.Race, shared.Telemetry.TrackTemp,
                                 shared.Telemetry.AirTemp, currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks, (float)driver.Live.Speed);

                        /*if (driver.PitInfo.IsAtPitExit)
                        {
                            Console.WriteLine(currentOpponentData.DriverRawName + " Is At Pit Exit");
                            currentOpponentData.IsAtPitExit = true;
                        }
                        else
                        {
                            currentOpponentData.IsAtPitExit = false;
                        }*/

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
                        if (hasCrossedSFLine)
                        {
                            currentOpponentData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                        }
                        if (currentOpponentData.CurrentBestLapTime > 1)
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
                    }
                }
                else
                {
                    if (!driver.CurrentResults.IsOut || !driver.IsPacecar || !driver.Live.TrackSurface.HasFlag(TrackSurfaces.NotInWorld))
                    {
                        currentGameState.OpponentData.Add(driverName, createOpponentData(driver, driverName,
                            false, CarData.getCarClassForIRacingId(driver.Car.CarClassId).carClassEnum, currentGameState.SessionData.TrackDefinition.trackLength));
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

            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.PreviousLapWasValid &&
                currentGameState.SessionData.LapTimePrevious > 1)
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

            currentGameState.FuelData.FuelUseActive = true;
            currentGameState.FuelData.FuelPressure = shared.Telemetry.FuelPress;
            currentGameState.FuelData.FuelLeft = shared.Telemetry.FuelLevel;
            
            currentGameState.PitData.limiterStatus = shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.PitSpeedLimiter) == true ? 1 : 0;


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
            //conditions
            if (currentGameState.Conditions.timeOfMostRecentSample.Add(ConditionsMonitor.ConditionsSampleFrequency) < currentGameState.Now)
            {
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.Telemetry.AirTemp, shared.Telemetry.TrackTemp, 0, shared.Telemetry.WindVel, 0, 0, 0);
            }
            //Console.WriteLine("Speed:" + playerCar.SpeedKph);


            // Console.WriteLine("Session running time = " + currentGameState.SessionData.SessionRunningTime + " type = " + currentGameState.SessionData.SessionType + " phase " + currentGameState.SessionData.SessionPhase + " run time = " + currentGameState.SessionData.SessionTotalRunTime);
            return currentGameState;
        }

        private void upateOpponentData(OpponentData opponentData, OpponentData previousOpponentData, int racePosition, int unfilteredRacePosition, int completedLaps,
            int sector, float sectorTime, float currentLaptime, float completedLapTime, Boolean isNewLap, Boolean isInPits, bool previousIsInPits, Boolean lapIsValid, float sessionRunningTime,
            float distanceRoundTrack, Boolean sessionLengthIsTime, float sessionTimeRemaining,
            Boolean isRace, float airTemperature, float trackTempreture, float nearPitEntryPointDistance, float speed)
        {
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            Boolean validSpeed = true;
            if (speed > 500 || speed < 0)
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

            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.Position;
            }

            opponentData.IsNewLap = false;

            if (sessionRunningTime > 10 && isRace && !opponentData.InPits && isInPits)
            {
                opponentData.NumPitStops++;
            }
            opponentData.InPits = isInPits;
            bool hasNewLapData = opponentData.HasNewLapData(previousOpponentData, sector, completedLapTime, 3) || opponentData.OpponentLapData.Count <=0;
            if (hasNewLapData)
            {
                if (opponentData.OpponentLapData.Count > 0)
                {
                    opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, completedLapTime,
                        /*lapIsValid && validSpeed*/ true, false, trackTempreture, airTemperature, sessionLengthIsTime, sessionTimeRemaining, 3);
                }
                opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, false, trackTempreture, airTemperature);
                opponentData.IsNewLap = true;
            }
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    //opponentData.AddSectorData(opponentData.CurrentSectorNumber, racePosition, sectorTime, sessionRunningTime, true, false, trackTempreture, airTemperature);
                    opponentData.AddCumulativeSectorData(opponentData.CurrentSectorNumber, racePosition, currentLaptime, sessionRunningTime, true, false, trackTempreture, airTemperature);
                    
                }
                opponentData.CurrentSectorNumber = sector;
            }
            opponentData.CompletedLaps = completedLaps;
            if (sector == 3 && isInPits)
            {
                opponentData.setInLap();
            }
        }

        private static Dictionary<String, SessionType> sessionTypeMap = new Dictionary<String, SessionType>()
        {
            {"Offline Testing", SessionType.Practice},
            {"Practice", SessionType.Practice},
            {"Open Qualify", SessionType.Qualify},
            {"Lone Qualify", SessionType.Qualify},
            {"Race", SessionType.Race}
        };

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            String sessionString = (String)memoryMappedFileStruct;
            if (sessionTypeMap.ContainsKey(sessionString))
            {
                return sessionTypeMap[sessionString];
            }
            return SessionType.Unavailable;
        }

        string prevSessionFlags = "";

        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionStates sessionState,
            SessionType currentSessionType, bool isReplay, float thisSessionRunningTime,
            int previousLapsCompleted, int laps, SessionFlags sessionFlags, bool isInPit, bool fixedTimeSession,
            double sessionTimeRemaining, double sessionRunningTime)
        {
            // here we assume a timed session must be at least 1 minute - if the time remaining < 0 and we've been running for < 60 seconds, it's bollocks
            //if (fixedTimeSession && sessionTimeRemaining < 0 && sessionRunningTime < 60)
            //{
                // Console.WriteLine("assuming unavailable - fixed time with " + sessionTimeRemaining + " left and has run for " + sessionRunningTime);
            //    return SessionPhase.Unavailable;
            //}
            if (!prevSessionFlags.Equals(sessionFlags.ToString()))
            {
                Console.WriteLine(sessionFlags.ToString());
                prevSessionFlags = sessionFlags.ToString();
            }   
            if (currentSessionType == SessionType.Practice)
            {
                if (sessionState.HasFlag(SessionStates.CoolDown))
                {
                    return SessionPhase.Finished;
                }
                else if (sessionState.HasFlag(SessionStates.Checkered))
                {
                    return SessionPhase.Checkered;
                }
                else if (/*isInPit || laps <= 0 && */lastSessionPhase == SessionPhase.Unavailable)
                {
                    return SessionPhase.Countdown;
                }

                return SessionPhase.Green;
            }
            else if (currentSessionType == SessionType.Qualify)
            {
                /*if(lastSessionPhase.HasFlag(SessionPhase.Finished) || lastSessionPhase.HasFlag(SessionPhase.Checkered))
                {
                    return SessionPhase.Unavailable;
                }*/
                if (sessionState.HasFlag(SessionStates.CoolDown))
                {
                    return SessionPhase.Finished;
                }
                else if (sessionState.HasFlag(SessionStates.Checkered))
                {
                    return SessionPhase.Checkered;
                }
                else if ( /*isInPit && laps <= 0*/ !sessionFlags.HasFlag(SessionFlags.Green) && sessionFlags.HasFlag(SessionFlags.OneLapToGreen))
                {
                    return SessionPhase.Countdown;
                }
                else if (sessionFlags.HasFlag(SessionFlags.Green))
                {
                    return SessionPhase.Green;
                }

                return lastSessionPhase;
            }
            else if (currentSessionType.HasFlag(SessionType.Race))
            {

                if (sessionState.HasFlag(SessionStates.Checkered) || sessionState.HasFlag(SessionStates.CoolDown))
                {
                    if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow)
                    {
                        if (previousLapsCompleted != laps || sessionState.HasFlag(SessionStates.CoolDown))
                        {
                            Console.WriteLine("finished - completed " + laps + " laps (was " + previousLapsCompleted + "), session running time = " +
                                thisSessionRunningTime);
                            return SessionPhase.Finished;
                        }
                    }
                }
                else if (sessionFlags.HasFlag(SessionFlags.StartReady) || sessionFlags.HasFlag(SessionFlags.StartSet) || SessionStates.Racing != sessionState && isReplay)
                {
                    // don't allow a transition to Countdown if the game time has increased
                    //if (lastSessionRunningTime < thisSessionRunningTime)
                    //{
                    return SessionPhase.Countdown;
                    //}
                }
                else if (sessionState.HasFlag(SessionStates.ParadeLaps))
                {
                    return SessionPhase.Formation;
                }
                else if ((SessionStates.Racing == sessionState && isReplay) || sessionFlags.HasFlag(SessionFlags.Green) || sessionFlags.HasFlag(SessionFlags.StartGo))
                {
                    return SessionPhase.Green;
                }
            }
            return lastSessionPhase;
        }

        private OpponentData createOpponentData(Driver opponentCar, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass, float trackLength)
        {
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            }
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = driverName;
            opponentData.Position = opponentCar.Live.Position;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = opponentCar.CurrentResults.LapsComplete;
            opponentData.DistanceRoundTrack = opponentCar.Live.CorrectedLapDistance * trackLength;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.Now);
            opponentData.CarClass = CarData.getCarClassForIRacingId(opponentCar.Car.CarClassId);
            opponentData.CurrentSectorNumber = opponentCar.Live.CurrentFakeSector;
            
            Console.WriteLine("New driver " + driverName + " is using car class " +
                opponentData.CarClass.getClassIdentifier() + " (class ID " + opponentCar.Car.CarClassId + ")");

            return opponentData;
        }
    }
}
