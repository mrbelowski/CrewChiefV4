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

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper wrapper = (CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            Sim shared = wrapper.data;
    
            if(memoryMappedFileStruct == null)
            {
                return previousGameState;
            }

            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;

            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionType = previousGameState.SessionData.SessionType;
            }

            currentGameState.SessionData.SessionType = mapToSessionType(shared.SessionData.SessionType);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Telemetry.SessionTime.Value;
            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain.Value;
            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase,
                shared.Telemetry.SessionState.Value,
                currentGameState.SessionData.SessionType,
                lastSessionRunningTime,
                (float)shared.Telemetry.SessionTime.Value,
                previousLapsCompleted, shared.Telemetry.Lap.Value,
                shared.Telemetry.SessionFlags.Value, shared.Telemetry.IsInGarage.Value);
            currentGameState.SessionData.NumCarsAtStartOfSession = shared.Drivers.Count;
            int sessionNumber = shared.Telemetry.SessionNum.Value;
            int PlayerCarIdx = shared.Telemetry.PlayerCarIdx.Value;
            Boolean justGoneGreen = false;
            if(shared.Driver != null)
            {
                playerCar = shared.Driver;
            }
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
                Console.WriteLine("rawSessionPhase = " + shared.Telemetry.SessionState.Value);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                Console.WriteLine("NumCarsAtStartOfSession = " + currentGameState.SessionData.NumCarsAtStartOfSession);

                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;

                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName,
                    (float)shared.SessionData.Track.Length * 1000, 3);
                currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();

                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(shared.SessionData.Track.CodeName, currentGameState.SessionData.TrackDefinition.trackLength);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);



                currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartPosition = shared.Telemetry.PlayerCarPosition.Value;
                Console.WriteLine("SessionStartPosition = " + currentGameState.SessionData.SessionStartPosition);
                currentGameState.PitData.IsRefuellingAllowed = true;
                if (shared.SessionData.IsLimitedTime)
                {
                    currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain.Value;
                    currentGameState.SessionData.SessionHasFixedTime = true;
                    Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                }
                String driverName = playerCar.Name.ToLower();
                if (playerName == null)
                {
                    NameValidator.validateName(driverName);
                    playerName = driverName;
                }
                TrackSurfaces[] surfaces = shared.Telemetry.CarIdxTrackSurface.Value;
                currentGameState.PitData.InPitlane = playerCar.PitInfo.InPitLane;
                currentGameState.PositionAndMotionData.DistanceRoundTrack = playerCar.Live.LapDistance * 1000;
                //TODO update car classes
                currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.UNKNOWN_RACE);
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                foreach (Driver car in shared.Drivers)
                {

                    driverName = car.Name.ToLower();
                    if (car.Id == PlayerCarIdx)
                    {
                        continue;
                    }
                    else
                    {
                        currentGameState.OpponentData.Add(driverName, createOpponentData(car, driverName,
                            false, CarData.CarClassEnum.UNKNOWN_RACE));
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
                        if (shared.SessionData.IsLimitedSessionLaps)
                        {
                            currentGameState.SessionData.SessionTotalRunTime = (float)shared.Telemetry.SessionTimeRemain.Value;
                            currentGameState.SessionData.SessionHasFixedTime = true;
                        }
                        else
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName,
                                (float)shared.SessionData.Track.Length * 1000, 3);
                        currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();

                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(shared.SessionData.Track.CodeName, currentGameState.SessionData.TrackDefinition.trackLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                        currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                        
                        currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.UNKNOWN_RACE);
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());

                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                        }
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;
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
                        Console.WriteLine("TrackName " + trackName);
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
                    //currentGameState.SessionData.CompletedLaps = previousGameState.SessionData.CompletedLaps;
                    currentGameState.FlagData.useImprovisedIncidentCalling = previousGameState.FlagData.useImprovisedIncidentCalling;
                }
            }
            currentGameState.ControlData.ThrottlePedal = shared.Telemetry.Throttle.Value;
            currentGameState.ControlData.ClutchPedal = shared.Telemetry.Clutch.Value;
            currentGameState.ControlData.BrakePedal = shared.Telemetry.Brake.Value;
            currentGameState.TransmissionData.Gear = shared.Telemetry.Gear.Value;

            //TODO add yellow 


            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain.Value;
            
            currentGameState.SessionData.LapTimePrevious = (float)playerCar.Live.LastLaptime;

            //currentGameState.SessionData.LapTimePrevious = (float)shared.Telemetry.LapLastLapTime.Value;

            currentGameState.SessionData.PreviousLapWasValid = true;

            currentGameState.SessionData.CompletedLaps = shared.Telemetry.LapCompleted.Value;

            //currentGameState.SessionData.TimeDeltaBehind = shared.TimeDeltaBehind;
            currentGameState.SessionData.TimeDeltaFront = (float)shared.Driver.Live.DeltaToNext;


            //TODO validate laptimes
            currentGameState.SessionData.LapTimeCurrent = shared.Telemetry.LapCurrentLapTime.Value;
            currentGameState.SessionData.CurrentLapIsValid = true;

            currentGameState.SessionData.NumCars = shared.Drivers.Count;

            currentGameState.SessionData.Position = shared.Telemetry.PlayerCarPosition.Value;
            currentGameState.SessionData.UnFilteredPosition = shared.Telemetry.PlayerCarPosition.Value;
            
            currentGameState.SessionData.SessionFastestLapTimeFromGame = shared.SessionData.OverallBestLap.Laptime.Time.Seconds;
            //float playerClassBestLap = (float)shared.SessionData.ClassBestLaps[playerCar.Car.CarClassId].Laptime.Time.TotalSeconds;
            //currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = playerClassBestLap;

            if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                currentGameState.SessionData.OverallSessionBestLapTime > shared.SessionData.OverallBestLap.Laptime.Time.Seconds)
            {
                currentGameState.SessionData.OverallSessionBestLapTime = shared.SessionData.OverallBestLap.Laptime.Time.Seconds;
            }
            //if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
            //    currentGameState.SessionData.PlayerClassSessionBestLapTime > playerClassBestLap)
            //{
            //    currentGameState.SessionData.PlayerClassSessionBestLapTime = playerClassBestLap;
            //}
            currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                (shared.Telemetry.LapCompleted.Value == previousGameState.SessionData.CompletedLaps + 1 ||
                ((lastSessionPhase == SessionPhase.Countdown || lastSessionPhase == SessionPhase.Formation || lastSessionPhase == SessionPhase.Garage)
                && (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)));

            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.Position;
                //currentGameState.SessionData.formattedPlayerLapTimes.Add(shared.Driver.CurrentResults.LastTime.Time.ToString(@"mm\:ss\.fff"));
                currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(playerCar.Live.LastLaptime).ToString(@"mm\:ss\.fff"));
                //Console.WriteLine(shared.Driver.CurrentResults.LastTime.Time.ToString(@"mm\:ss\.fff"));
                Console.WriteLine(TimeSpan.FromSeconds(playerCar.Live.LastLaptime).ToString(@"mm\:ss\.fff"));
                //currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
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
            int currentSector = playerCar.Live.CurrentFakeSector+1;
            currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.UNKNOWN_RACE);
            currentGameState.SessionData.IsNewSector = currentGameState.SessionData.SectorNumber != currentSector;

            if (currentGameState.SessionData.IsNewSector)
            {
                if (currentSector == 1)
                {
                    if (currentGameState.SessionData.SessionTimesAtEndOfSectors[3] != -1)
                    {
                        currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                    }
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;
                    float sectorTime = (float)playerCar.CurrentResults.FakeSector3.SectorTime.Time.TotalSeconds;
                    if (sectorTime > 0 && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                    {
                        currentGameState.SessionData.LastSector3Time = sectorTime;
                        if (currentGameState.SessionData.PlayerBestSector3Time == -1 || currentGameState.SessionData.LastSector3Time < currentGameState.SessionData.PlayerBestSector3Time)
                        {
                            currentGameState.SessionData.PlayerBestSector3Time = currentGameState.SessionData.LastSector3Time;
                        }
                        if (currentGameState.SessionData.LapTimePrevious > 0 &&
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
                    float sectorTime = (float)playerCar.CurrentResults.FakeSector1.SectorTime.Time.TotalSeconds;
                    if (sectorTime > 0 && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                    {
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
                }
                else if (currentSector == 3)
                {
                    currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                    float sectorTime = (float)playerCar.CurrentResults.FakeSector2.SectorTime.Time.TotalSeconds;
                    if (sectorTime > 0 && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                    {
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
                }

            }
            currentGameState.SessionData.SectorNumber = currentSector;
            currentGameState.PitData.InPitlane = playerCar.PitInfo.InPitLane;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = playerCar.Live.LapDistance * 1000;

            if (currentGameState.PitData.InPitlane)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionRunningTime > 10 &&
                    previousGameState != null && !previousGameState.PitData.InPitlane)
                {
                    currentGameState.PitData.NumPitStops = playerCar.PitInfo.Pitstops;
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
            //Console.WriteLine("Speed:" + playerCar.SpeedKph);
            return currentGameState;


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
            //Console.WriteLine(sessionString);
            if (sessionTypeMap.ContainsKey(sessionString))
            {
                return sessionTypeMap[sessionString];
            }
            return SessionType.Unavailable;
        }

        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionStates sessionState, 
            SessionType currentSessionType, float lastSessionRunningTime, float thisSessionRunningTime,
            int previousLapsCompleted, int laps, SessionFlag sessionFlags, bool isInPit)
        {
                       
            if (currentSessionType == SessionType.Practice)
            {
                //Console.WriteLine("Practice");
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
                else if (sessionFlags.Contains(SessionFlags.StartReady) || sessionFlags.Contains(SessionFlags.StartSet))
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
                else if (sessionState.HasFlag(SessionStates.Racing) || sessionFlags.Contains(SessionFlags.Green))
                {
                    return SessionPhase.Green;
                }
            }
            return lastSessionPhase;
        }

        private OpponentData createOpponentData(Driver opponentCar, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass)
        {
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            }
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = driverName;
            opponentData.Position = opponentCar.Live.Position;
            opponentData.UnFilteredPosition = opponentData.Position;
            //opponentData.CompletedLaps = opponentCar.CurrentResults.LapsComplete;
            opponentData.CurrentSectorNumber = opponentCar.Live.CurrentSector;
            opponentData.DistanceRoundTrack = opponentCar.Live.LapDistance * 1000;
            return opponentData;
        }
    }
}
