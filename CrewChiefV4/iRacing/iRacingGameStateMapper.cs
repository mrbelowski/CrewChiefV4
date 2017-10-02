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
        class splitTimes
        {
            private float currentSplitPoint = 0;
            private float nextSplitPoint = 0;
            private const float splitSpacing = 50;
            private int previousLap = 0;
            private Dictionary<float, DateTime> splitPoints = new Dictionary<float, DateTime>();

            /*private float FixPercentagesOnLapChange(float distanceRoundTrack, int currentLap)
            {
                if (currentLap > previousLap && distanceRoundTrack > 0.80f)
                    return 0;
                else
                    previousLap = currentLap;

                return distanceRoundTrack;
            }*/

            public void setSplitPoints(float trackLength, DateTime now)
            {
                splitPoints.Clear();
                float totalGaps = 0;
                while (totalGaps < trackLength)
                {
                    totalGaps += splitSpacing;

                    if (totalGaps < trackLength - splitSpacing)
                    {
                        splitPoints.Add(totalGaps, now);
                    }
                    else
                    {
                        break;
                    }

                }
                splitPoints.Add(trackLength - 50, now);
            }

            public void setNextSplitPoint(float distanceRoundTrack, float speed, DateTime now, int currentLap)
            {
                //float distance = FixPercentagesOnLapChange(distanceRoundTrack, currentLap);
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
                    // Console.WriteLine("setting split:" + nextSplitPoint);
                    splitPoints[nextSplitPoint] = now;
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

        private Dictionary<int, splitTimes> opponentsSplits = new Dictionary<int, splitTimes>();

        private static splitTimes playerSplits = new splitTimes();


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

                //playerLapData.Clear();
                opponentsSplits.Clear(); 
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
                currentGameState.PositionAndMotionData.DistanceRoundTrack = playerCar.Live.CorrectedLapDistance * currentGameState.SessionData.TrackDefinition.trackLength;
                //TODO update car classes
                currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.UNKNOWN_RACE);
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);

                playerSplits.setSplitPoints(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.Now);
                playerSplits.setNextSplitPoint(0, 100, currentGameState.Now,shared.Telemetry.LapCompleted.Value);

                foreach (Driver car in shared.Drivers)
                {
                    driverName = car.Name.ToLower();
                    if (car.Id == PlayerCarIdx)
                    {
                        continue;
                    }
                    else
                    {
                        if (!car.CurrentResults.IsEmpty && !car.CurrentResults.IsOut)
                        {
                            currentGameState.OpponentData.Add(driverName, createOpponentData(car, driverName,
                            false, CarData.CarClassEnum.UNKNOWN_RACE, currentGameState.SessionData.TrackDefinition.trackLength));
                            if (!opponentsSplits.ContainsKey(car.Id))
                            {
                                opponentsSplits.Add(car.Id, new splitTimes());
                                opponentsSplits[car.Id].setSplitPoints(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.Now);
                                opponentsSplits[car.Id].setNextSplitPoint(0, 100, currentGameState.Now,car.Live.Lap);
                            }
                        }

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
                            if(currentGameState.SessionData.SessionType.HasFlag(SessionType.Race))
                            {
                                currentGameState.SessionData.HasExtraLap = true;
                            }

                        }
                        else
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName,
                                (float)shared.SessionData.Track.Length * 1000, 3);

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
            if ((float)playerCar.Live.LastLaptime <= 1)
            {
                currentGameState.SessionData.PreviousLapWasValid = false;
            }
            else
            {
                currentGameState.SessionData.PreviousLapWasValid = true;
            }
            

            currentGameState.SessionData.CompletedLaps = shared.Telemetry.LapCompleted.Value;

            //currentGameState.SessionData.TimeDeltaBehind = shared.TimeDeltaBehind;
            //currentGameState.SessionData.TimeDeltaFront = (float)shared.Driver.Live.DeltaToNext;


            //TODO validate laptimes
            currentGameState.SessionData.LapTimeCurrent = shared.Telemetry.LapCurrentLapTime.Value;
            currentGameState.SessionData.CurrentLapIsValid = true;

            currentGameState.SessionData.NumCars = shared.Drivers.Count;

            currentGameState.SessionData.Position = shared.Telemetry.PlayerCarPosition.Value;
            currentGameState.SessionData.UnFilteredPosition = shared.Telemetry.PlayerCarPosition.Value;
            
            currentGameState.SessionData.SessionFastestLapTimeFromGame = shared.SessionData.OverallBestLap.Laptime.Time.Seconds;

            if (shared.SessionData.Flags.Contains(SessionFlags.Black) && !shared.SessionData.Flags.Contains(SessionFlags.Furled))
            {
                currentGameState.PenaltiesData.HasStopAndGo = true;  
            }
            if (shared.SessionData.Flags.Contains(SessionFlags.Black) && shared.SessionData.Flags.Contains(SessionFlags.Furled))
            {
                currentGameState.PenaltiesData.HasSlowDown = true;                 
            }
            if(shared.SessionData.Flags.Contains(SessionFlags.Yellow))
            {
                currentGameState.FlagData.isLocalYellow = true;
            }
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
                currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                Console.WriteLine("LapLength: " + previousGameState.PositionAndMotionData.DistanceRoundTrack);
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
            int currentSector = playerCar.Live.CurrentFakeSector + 1;
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
            currentGameState.PositionAndMotionData.DistanceRoundTrack = shared.Telemetry.LapDist.Value;//spLineLengthToDistanceRoundTrack(currentGameState.SessionData.TrackDefinition.trackLength, playerCar.Live.CorrectedLapDistance);
            currentGameState.PositionAndMotionData.CarSpeed = (float)playerCar.Live.Speed;
            
            
            //Console.WriteLine("distance round track " + currentGameState.PositionAndMotionData.DistanceRoundTrack);
            if (previousGameState != null)
            {
                String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                    currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.PositionAndMotionData.CarSpeed);
                currentGameState.SessionData.stoppedInLandmark = playerCar.PitInfo.InPitLane? null : stoppedInLandmark;
            }
            
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
            foreach(Driver driver in shared.Drivers)
            {
                if(driver.Id == PlayerCarIdx)
                { 
                    continue;
                }
                String driverName = driver.Name.ToLower();
                if (currentGameState.OpponentData.ContainsKey(driverName))
                {
                    if (driver.CurrentResults.IsOut)
                    {
                        currentGameState.OpponentData.Remove(driverName);
                        Console.WriteLine(driverName + " Has disconnected so removing him/her");
                        continue;
                    }
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
                        if (previousGameState.OpponentData.ContainsKey(driverName))
                        {
                            previousOpponentData = previousGameState.OpponentData[driverName];
                            previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                            previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                            previousOpponentPosition = previousOpponentData.Position;
                            previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                            previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                            previousOpponentSpeed = previousOpponentData.Speed;
                            newOpponentLap = previousOpponentData.CurrentSectorNumber == 3 && driver.Live.CurrentFakeSector + 1 == 1;
                            previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                        }

                        float currentOpponentLapDistance = spLineLengthToDistanceRoundTrack(currentGameState.SessionData.TrackDefinition.trackLength, driver.Live.CorrectedLapDistance);
                        int currentOpponentSector = getCurrentSector(currentGameState.SessionData.TrackDefinition, currentOpponentLapDistance); 

                        //int currentOpponentSector = driver.Live.CurrentFakeSector + 1;
                        OpponentData currentOpponentData = currentGameState.OpponentData[driverName];


                        float sectorTime = -1;
                        if (currentOpponentSector == 1)
                        {
                            sectorTime = (float)driver.CurrentResults.FakeSector3.SectorTime.Time.TotalSeconds;
                        }
                        else if (currentOpponentSector == 2)
                        {
                            sectorTime = (float)driver.CurrentResults.FakeSector1.SectorTime.Time.TotalSeconds;
                        }
                        else if (currentOpponentSector == 3)
                        {
                            sectorTime = (float)driver.CurrentResults.FakeSector2.SectorTime.Time.TotalSeconds;
                        }
                        int currentOpponentRacePosition = driver.CurrentResults.Position;
                        int currentOpponentLapsCompleted = shared.Telemetry.CarIdxLapCompleted.Value[driver.Id];

                        if (currentOpponentSector == 0)
                        {
                            currentOpponentSector = previousOpponentSectorNumber;
                        }
                        //float currentOpponentLapDistance = driver.Live.LapDistance * currentGameState.SessionData.TrackDefinition.trackLength;
                        //Console.WriteLine("lapdistance:" + currentOpponentLapDistance);

                        Boolean finishedAllottedRaceLaps = currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted;
                        Boolean finishedAllottedRaceTime = false;
                        
                        if (currentGameState.SessionData.HasExtraLap &&
                            currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            if (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
                                previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                            {
                                if (!currentOpponentData.HasStartedExtraLap)
                                {
                                    currentOpponentData.HasStartedExtraLap = true;
                                }
                                else
                                {
                                    finishedAllottedRaceTime = true;
                                }
                            }
                        }
                        else if (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
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

                        Boolean isEnteringPits = driver.PitInfo.IsAtPitEntry;
                        Boolean isLeavingPits = driver.PitInfo.IsAtPitExit;
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
                            if (opponentsSplits.ContainsKey(driver.Id))
                            {
                                float timeDeltaBehind = currentGameState.SessionData.TimeDeltaBehind = opponentsSplits[driver.Id].getSplitTime(playerSplits, true);
                            }
                        }

                        if (currentOpponentRacePosition == currentGameState.SessionData.Position - 1 && currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            if (opponentsSplits.ContainsKey(driver.Id))
                            {
                                currentGameState.SessionData.TimeDeltaFront = opponentsSplits[driver.Id].getSplitTime(playerSplits, false);
                            }
                        }
                        if (opponentsSplits.ContainsKey(driver.Id))
                        {
                            opponentsSplits[driver.Id].setNextSplitPoint(currentOpponentLapDistance, (float)driver.Live.Speed, currentGameState.Now, currentOpponentLapsCompleted);
                        }

                        upateOpponentData(currentOpponentData, currentOpponentRacePosition,
                                 currentOpponentRacePosition, currentOpponentLapsCompleted,
                                 currentOpponentSector, sectorTime, driver.Live.LastLaptime,
                                 driver.PitInfo.InPitLane, true, currentGameState.SessionData.SessionRunningTime, currentOpponentLapDistance,
                                 currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                 currentGameState.SessionData.SessionType == SessionType.Race, shared.Telemetry.TrackTemp.Value,
                                 shared.Telemetry.AirTemp.Value, currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks, (float)driver.Live.Speed);
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
                        if (newOpponentLap)
                        {
                            currentOpponentData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
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
                                if (CarData.IsCarClassEqual(currentOpponentData.CarClass, currentGameState.carClass))
                                {
                                    if (currentOpponentData.LastLapTime > 0 && currentOpponentData.LastLapValid &&
                                        (!currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre.ContainsKey(currentOpponentData.CurrentTyres) ||
                                        currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre[currentOpponentData.CurrentTyres] > currentOpponentData.LastLapTime))
                                    {
                                        currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre[currentOpponentData.CurrentTyres] = currentOpponentData.LastLapTime;
                                    }
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

                    if (!driver.CurrentResults.IsEmpty && !driver.CurrentResults.IsOut)
                    {
                        currentGameState.OpponentData.Add(driverName, createOpponentData(driver, driverName,
                                  false, CarData.CarClassEnum.UNKNOWN_RACE, currentGameState.SessionData.TrackDefinition.trackLength));
                        
                        if (!opponentsSplits.ContainsKey(driver.Id))
                        {
                            opponentsSplits.Add(driver.Id, new splitTimes());
                            opponentsSplits[driver.Id].setSplitPoints(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.Now);
                            opponentsSplits[driver.Id].setNextSplitPoint(0, 100, currentGameState.Now,shared.Telemetry.CarIdxLapCompleted.Value[driver.Id]);
                        }
                    }
  
                }
            }

            playerSplits.setNextSplitPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, (float)playerCar.Live.Speed, currentGameState.Now,currentGameState.SessionData.CompletedLaps);

            currentGameState.FuelData.FuelUseActive = true;    
            currentGameState.FuelData.FuelPressure = shared.Telemetry.FuelPress.Value;
            currentGameState.FuelData.FuelLeft = shared.Telemetry.FuelLevel.Value;
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
            //Console.WriteLine("Speed:" + playerCar.SpeedKph);
            return currentGameState;


        }

        private void upateOpponentData(OpponentData opponentData, int racePosition, int unfilteredRacePosition, int completedLaps,
            int sector, float sectorTime,float completedLapTime, Boolean isInPits, Boolean lapIsValid, float sessionRunningTime, 
            float distanceRoundTrack, Boolean sessionLengthIsTime, float sessionTimeRemaining, 
            Boolean isRace, float airTemperature, float trackTempreture, float nearPitEntryPointDistance, float speed)
        {
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            Boolean validSpeed = true;
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
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, completedLapTime,
                            lapIsValid && validSpeed, false, 20, 20, sessionLengthIsTime, sessionTimeRemaining, 3);
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, false, 20, 20);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddSectorData(opponentData.CurrentSectorNumber, racePosition, sectorTime, sessionRunningTime, lapIsValid && validSpeed, false, trackTempreture, airTemperature);
                    //opponentData.AddCumulativeSectorData(opponentData.CurrentSectorNumber, racePosition, sectorTime, sessionRunningTime, lapIsValid && validSpeed, false, trackTempreture, airTemperature);
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
            //Console.WriteLine(sessionString);
            if (sessionTypeMap.ContainsKey(sessionString))
            {
                return sessionTypeMap[sessionString];
            }
            return SessionType.Unavailable;
        }

        string prevSessionFlags = "";
        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionStates sessionState, 
            SessionType currentSessionType, float lastSessionRunningTime, float thisSessionRunningTime,
            int previousLapsCompleted, int laps, SessionFlag sessionFlags, bool isInPit)
        {
            if (!prevSessionFlags.Equals(sessionFlags.ToString()))
            {
                Console.WriteLine(sessionFlags.ToString());
                prevSessionFlags = sessionFlags.ToString();
            }
            //      
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
                return SessionPhase.Unavailable;
            }
            return lastSessionPhase;
        }

        private OpponentData createOpponentData(Driver opponentCar, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass,float trackLength)
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

            opponentData.CurrentSectorNumber = 0;
            
            Console.WriteLine("New driver " + driverName);
            return opponentData;
        }
        private float spLineLengthToDistanceRoundTrack(float trackLength, float spLine)
        {
            if (spLine < 0.0f)
            {
                spLine -= 1f;
            }
            return spLine * trackLength;
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
            return ret;
        }
    }
}
