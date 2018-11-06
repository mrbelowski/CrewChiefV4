using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.GameState;
using CrewChiefV4.ACC;
using CrewChiefV4.ACC.Data;
using System.Runtime.InteropServices;
namespace CrewChiefV4.ACC
{
    class ACCGameStateMapper : GameStateMapper
    {

        RaceSessionType previousRaceSessionType = RaceSessionType.FreePractice1;
        RaceSessionPhase previousRaceSessionPhase = RaceSessionPhase.RaceSessionPhase_Max;
           
        // next track conditions sample due after:
        private DateTime nextConditionsSampleDue = DateTime.MinValue;

        private void PrintProperties<T>(T myObj)
        {
            /*foreach (var prop in myObj.GetType().GetProperties())
            {
                Console.WriteLine(Marshal.OffsetOf(typeof(T), prop.Name).ToString("d") + " prop " + prop.Name + ": " + prop.GetValue(myObj, null));
            }*/
            Console.WriteLine("sizeOf: " + myObj.ToString() + " " + Marshal.SizeOf(typeof(T)).ToString("X"));
            foreach (var field in myObj.GetType().GetFields())
            {
                Console.WriteLine("0x" + Marshal.OffsetOf(typeof(T), field.Name).ToString("X") + " " + field.Name + ": " + field.GetValue(myObj));
            }
        }
        public ACCGameStateMapper()
        {

        }

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no version data in the stream so this is a no-op

        }
        public override void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            speechRecogniser.addiRacingSpeechRecogniser();
            this.speechRecogniser = speechRecogniser;
        }
        public float mapToFloatTime(int time)
        {
            return (float)TimeSpan.FromMilliseconds(time).TotalSeconds;
        }
        public float mapToFloatTime(float time)
        {
            return (float)TimeSpan.FromMilliseconds(time).TotalSeconds;
        }
        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            ACCSharedMemoryReader.ACCStructWrapper wrapper = (ACCSharedMemoryReader.ACCStructWrapper)structWrapper;            
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            ACCSharedMemoryData data = wrapper.data;
            
            if(data.isReady != 1 || data.sessionData.areCarsInitializated != 1)
            {
                return previousGameState;
            }
           /* if (!previousRaceSessionType.Equals(data.sessionData.currentSessionType))
            {   
                PrintProperties<CrewChiefV4.ACC.Data.Track>(data.track);
                PrintProperties<CrewChiefV4.ACC.Data.WeatherStatus>(data.track.weatherState);
                previousRaceSessionType = data.sessionData.currentSessionType;
                Console.WriteLine("currentSessionType " + data.sessionData.currentSessionType);
            }
            if (!previousRaceSessionPhase.Equals(data.sessionData.currentSessionPhase))            
            {
                PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
                PrintProperties<CrewChiefV4.ACC.Data.ACCSharedMemoryData>(data);
                Console.WriteLine("previousRaceSessionPhase " + previousRaceSessionPhase );
                Console.WriteLine("currentRaceSessionPhase " + data.sessionData.currentSessionPhase);
            }*/
            
            //Console.WriteLine("tyre temp" + wrapper.physicsData.tyreTempM[0]);
            SessionType previousSessionType = SessionType.Unavailable;
            SessionPhase previousSessionPhase = SessionPhase.Unavailable;
            float previousSessionRunningTime = -1;
            int previousSessionId = -1;
            if(previousGameState != null)
            {
                previousSessionType = previousGameState.SessionData.SessionType;
                previousSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                previousSessionPhase = previousGameState.SessionData.SessionPhase;
                previousSessionId = previousGameState.SessionData.SessionId;

            }
            //test commit
            SessionType currentSessionType = mapToSessionType(data.sessionData.currentSessionType, data.driverCount == 1 && data.sessionData.isOnline == 0);
            currentGameState.SessionData.SessionType = currentSessionType;
            SessionPhase currentSessioPhase = mapToSessionPhase(data.sessionData.currentSessionPhase, currentSessionType);
            currentGameState.SessionData.SessionPhase = currentSessioPhase;

            currentGameState.SessionData.SessionRunningTime = mapToFloatTime(data.sessionData.physicsTime - data.sessionData.sessionStartTimeStamp);
            currentGameState.SessionData.SessionTimeRemaining = mapToFloatTime(data.sessionData.sessionEndTime - data.sessionData.physicsTime);
            float defaultSessionTotalRunTime = 3630.0f;
            if (currentSessionType == SessionType.LonePractice)
            {                
                currentGameState.SessionData.SessionTotalRunTime = defaultSessionTotalRunTime;
                currentGameState.SessionData.SessionTimeRemaining = defaultSessionTotalRunTime - currentGameState.SessionData.SessionRunningTime;
            }                      
            Driver playerDriver = data.playerDriver;
            currentGameState.SessionData.NumCarsOverall = data.driverCount;

            //this still needs fixing
            if (currentSessionType != SessionType.Unavailable &&
                ((previousRaceSessionPhase == RaceSessionPhase.StartingUI && data.sessionData.currentSessionPhase == RaceSessionPhase.PreFormationTime && currentSessionType == SessionType.Race)
                || (previousSessionId != data.sessionData.currentSessionIndex)))
            {
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.SessionId = data.sessionData.currentSessionIndex;
                //PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
                //Console.WriteLine("New session, trigger data:");
                Console.WriteLine("SessionTimeRemaining = " + currentGameState.SessionData.SessionTimeRemaining);
                Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                Console.WriteLine("SessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                currentGameState.SessionData.NumCarsOverallAtStartOfSession = data.driverCount;
                currentGameState.SessionData.OverallPosition = playerDriver.position;
                //currentGameState.SessionData.SessionStartClassPosition;
                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(data.track.name, 0, data.track.length);
                if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                {
                    if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                    {
                        if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                        {
                            currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap = previousGameState.hardPartsOnTrackData.processedHardPartsForBestLap;
                            currentGameState.hardPartsOnTrackData.isAlreadyBraking = previousGameState.hardPartsOnTrackData.isAlreadyBraking;
                            currentGameState.hardPartsOnTrackData.hardPartStart = previousGameState.hardPartsOnTrackData.hardPartStart;
                            currentGameState.hardPartsOnTrackData.hardPartsMapped = previousGameState.hardPartsOnTrackData.hardPartsMapped;
                        }
                    }
                }
                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(data.track.name, currentGameState.SessionData.TrackDefinition.trackLength);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = false;

                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.PlayerLapData.Clear();
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;

                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.PitData.IsRefuellingAllowed = true;
                currentGameState.SessionData.SessionHasFixedTime = true;

                
                if (currentSessionType != SessionType.LonePractice)
                {
                    currentGameState.SessionData.SessionTotalRunTime = data.sessionData.sessionDuration;
                }

                currentGameState.PitData.InPitlane = playerDriver.trackLocation != CarLocation.ECarLocation__Track;
                currentGameState.PositionAndMotionData.DistanceRoundTrack = Math.Abs(playerDriver.distanceRoundTrackNormalizes * currentGameState.SessionData.TrackDefinition.trackLength);

                //TODO update car classes shuold be easy as they will all be GT3 :D
                currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.GT3);
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());


                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
                currentGameState.SessionData.SectorNumber = (int)playerDriver.currentSector + 1; // this is incorrect doing formation lap!

                for(int i = 1; i < data.driverCount; i++)
                {
                    Driver driver = data.drivers[i];
                        currentGameState.OpponentData.Add(driver.name, createOpponentData(driver, true,
                            currentGameState.SessionData.TrackDefinition.trackLength));
                }
                // add a conditions sample when we first start a session so we're not using stale or default data in the pre-lights phase
                currentGameState.Conditions.addSample(currentGameState.Now, 0, 1, data.track.weatherState.ambientTemperature, data.track.weatherState.roadTemperature, 
                    data.track.weatherState.rainLevel, data.track.weatherState.windSpeed, 0, 0, 0, true);
            }
            else
            {
                if (previousSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + previousSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (previousGameState != null && previousGameState.SessionData.TrackDefinition == null)
                    {
                        Console.WriteLine("New session phase without new session initialized previously.");
                    }

                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green && previousSessionPhase != SessionPhase.Finished)
                    {
                        currentGameState.SessionData.JustGoneGreen = true;
                        // just gone green, so get the session data
                        currentGameState.SessionData.SessionHasFixedTime = true;
                        if (currentSessionType != SessionType.LonePractice)
                        {
                            currentGameState.SessionData.SessionTotalRunTime = data.sessionData.sessionDuration;
                        }

                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(data.track.name, 0, data.track.length);
                        if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                        {
                            if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                            {
                                if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                                {
                                    currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap = previousGameState.hardPartsOnTrackData.processedHardPartsForBestLap;
                                    currentGameState.hardPartsOnTrackData.isAlreadyBraking = previousGameState.hardPartsOnTrackData.isAlreadyBraking;
                                    currentGameState.hardPartsOnTrackData.hardPartStart = previousGameState.hardPartsOnTrackData.hardPartStart;
                                    currentGameState.hardPartsOnTrackData.hardPartsMapped = previousGameState.hardPartsOnTrackData.hardPartsMapped;
                                }
                            }
                        }
                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(data.track.name, currentGameState.SessionData.TrackDefinition.trackLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                          currentGameState.SessionData.TrackDefinition.isOval = false;                          
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);

                        currentGameState.carClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.GT3);
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());

                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                            currentGameState.PitData.OnInLap = previousGameState.PitData.OnInLap;
                            currentGameState.PitData.OnOutLap = previousGameState.PitData.OnOutLap;
                        }
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                        currentGameState.SessionData.SessionId = data.sessionData.currentSessionIndex;
                        //currentGameState.SessionData.CompletedLaps = shared.Driver.CurrentResults.LapsComplete;
                        Console.WriteLine("Just gone green, session details...");

                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName + " Track Reported Length " + currentGameState.SessionData.TrackDefinition.trackLength);

                    }
                }
                if (!currentGameState.SessionData.JustGoneGreen && previousGameState != null)
                {
                    //Console.WriteLine("regular update, session type = " + currentGameState.SessionData.SessionType + " phase = " + currentGameState.SessionData.SessionPhase);

                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
                    currentGameState.SessionData.HasExtraLap = previousGameState.SessionData.HasExtraLap;
                    currentGameState.SessionData.NumCarsOverallAtStartOfSession = previousGameState.SessionData.NumCarsOverallAtStartOfSession;
                    currentGameState.SessionData.NumCarsInPlayerClassAtStartOfSession = previousGameState.SessionData.NumCarsInPlayerClassAtStartOfSession;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = previousGameState.SessionData.PositionAtStartOfCurrentLap;
                    currentGameState.SessionData.SessionStartClassPosition = previousGameState.SessionData.SessionStartClassPosition;
                    currentGameState.SessionData.ClassPositionAtStartOfCurrentLap = previousGameState.SessionData.ClassPositionAtStartOfCurrentLap;

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
                    currentGameState.PitData.PitBoxPositionEstimate = previousGameState.PitData.PitBoxPositionEstimate;
                    currentGameState.PitData.IsTeamRacing = previousGameState.PitData.IsTeamRacing;
                    currentGameState.PitData.IsApproachingPitlane = previousGameState.PitData.IsApproachingPitlane;

                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
                    currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                    currentGameState.SessionData.PlayerLapTimeSessionBestPrevious = previousGameState.SessionData.PlayerLapTimeSessionBestPrevious;
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
                    currentGameState.SessionData.LapTimePrevious = previousGameState.SessionData.LapTimePrevious;
                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.SessionData.CompletedLaps = previousGameState.SessionData.CompletedLaps;
                    currentGameState.FlagData.useImprovisedIncidentCalling = previousGameState.FlagData.useImprovisedIncidentCalling;
                    currentGameState.OpponentData = previousGameState.OpponentData;
                    currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
                    currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;

                    currentGameState.SessionData.SessionId = previousGameState.SessionData.SessionId;
                    currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
                    currentGameState.retriedDriverNames = previousGameState.retriedDriverNames;
                    currentGameState.disqualifiedDriverNames = previousGameState.disqualifiedDriverNames;
                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                    currentGameState.TimingData = previousGameState.TimingData;
                    currentGameState.SessionData.JustGoneGreenTime = previousGameState.SessionData.JustGoneGreenTime;
                }
            }


            /*if(playerDriver.isCarOutOfTrack == 1 && playerDriver.trackLocation == CarLocation.ECarLocation__Track)
            {
                Console.WriteLine("player car out of track");
            }*/
            currentGameState.PositionAndMotionData.DistanceRoundTrack = Math.Abs(playerDriver.distanceRoundTrackNormalizes * currentGameState.SessionData.TrackDefinition.trackLength);
            currentGameState.PositionAndMotionData.CarSpeed = playerDriver.speedMS;

            currentGameState.SessionData.OverallPosition = playerDriver.realTimePosition;
            previousRaceSessionPhase = data.sessionData.currentSessionPhase;

            currentGameState.SessionData.CompletedLaps = playerDriver.lapCount;
            currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.CompletedLaps < currentGameState.SessionData.CompletedLaps;           
            // on a instalattion/formation lap the sector does not change before we have crossed the s/f line first time
            // so get it from distance driven round track.
            if (playerDriver.currentSector == data.track.sectors)
            {
                currentGameState.SessionData.SectorNumber = getSectorFromDistanceRoundTrack(playerDriver.distanceRoundTrackNormalizes, data.track.normalizesSectorLimits);
            }
            else
            {
                currentGameState.SessionData.SectorNumber = playerDriver.currentSector + 1;
            }
            currentGameState.SessionData.IsNewSector = previousGameState != null && previousGameState.SessionData.SectorNumber != currentGameState.SessionData.SectorNumber;
            currentGameState.SessionData.LapTimeCurrent = mapToFloatTime(playerDriver.currentlaptime);
            if (currentGameState.SessionData.IsNewLap || currentGameState.SessionData.IsNewSector)
            {               
                if(currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                        mapToFloatTime(playerDriver.lastLap.lapTime), true, playerDriver.trackLocation != CarLocation.ECarLocation__Track, false, data.track.weatherState.roadTemperature, data.track.weatherState.ambientTemperature,
                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3, currentGameState.TimingData);
                }
                else if (currentGameState.SessionData.SectorNumber == 2 || currentGameState.SessionData.SectorNumber == 3)
                {
                    float lastSectorTime = mapToFloatTime(playerDriver.currentLap.sectorTimes[0]);
                    if(currentGameState.SessionData.SectorNumber == 3)
                    {
                        lastSectorTime += mapToFloatTime(playerDriver.currentLap.sectorTimes[1]);
                    }
                    currentGameState.SessionData.playerAddCumulativeSectorData(currentGameState.SessionData.SectorNumber - 1, currentGameState.SessionData.OverallPosition, lastSectorTime,
                        currentGameState.SessionData.SessionRunningTime, true, false, data.track.weatherState.roadTemperature, data.track.weatherState.ambientTemperature);
                }
            }
            
            if ((previousGameState != null && currentGameState.SessionData.SectorNumber == 1 && currentGameState.SessionData.IsNewSector && currentGameState.SessionData.CompletedLaps == 0) || currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                    currentGameState.SessionData.OverallPosition, playerDriver.trackLocation != CarLocation.ECarLocation__Track, currentGameState.SessionData.SessionRunningTime);
                currentGameState.SessionData.IsNewLap = true;
            }
                                   
            currentGameState.PitData.InPitlane = playerDriver.trackLocation.HasFlag(CarLocation.ECarLocation__PitLane);
            currentGameState.PitData.IsApproachingPitlane =  playerDriver.trackLocation.HasFlag(CarLocation.ECarLocation__PitEntry);
            currentGameState.PitData.limiterStatus = playerDriver.pitLimiterOn;            
            if (currentGameState.PitData.InPitlane)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionRunningTime > 10 &&
                    previousGameState != null && !previousGameState.PitData.InPitlane)
                {
                    currentGameState.PitData.NumPitStops++;
                }
                if (previousGameState != null && previousGameState.PitData.IsApproachingPitlane)
                {
                    currentGameState.PitData.OnInLap = true;
                    currentGameState.PitData.OnOutLap = false;
                }
            }           
            if ((previousGameState != null && previousGameState.PitData.InPitlane && playerDriver.trackLocation == CarLocation.ECarLocation__PitExit))
            {
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = true;
            }
            if (currentGameState.SessionData.IsNewLap && playerDriver.trackLocation.HasFlag(CarLocation.ECarLocation__Track))
            {
                // starting a new lap while not in the pitlane so clear the in / out lap flags
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = false;
            }
            if (previousGameState != null && currentGameState.PitData.OnOutLap && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane)
            {
                currentGameState.PitData.IsAtPitExit = true;
            }

            for (int i = 1; i < data.driverCount; i++)
            {
                Driver driver = data.drivers[i];
                String driverName = driver.name.ToLower();
                if (currentGameState.disqualifiedDriverNames.Contains(driver.name) || currentGameState.retriedDriverNames.Contains(driver.name))
                {
                    continue;
                }
                if (driver.isDisqualified == 1)
                {
                    // remove this driver from the set immediately
                    if (!currentGameState.disqualifiedDriverNames.Contains(driver.name))
                    {
                        Console.WriteLine("Opponent " + driver.name + " has been disqualified");
                        currentGameState.disqualifiedDriverNames.Add(driver.name);
                    }
                    currentGameState.OpponentData.Remove(driverName);
                    continue;
                }

                if (driver.isRetired == 1) 
                {
                    // remove this driver from the set immediately
                    if (!currentGameState.retriedDriverNames.Contains(driver.name))
                    {
                        Console.WriteLine("Opponent " + driver.name + " has retired");
                        currentGameState.retriedDriverNames.Add(driver.name);
                    }
                    currentGameState.OpponentData.Remove(driverName);
                    continue;
                }
                OpponentData currentOpponentData = null;
                if (currentGameState.OpponentData.TryGetValue(driverName, out currentOpponentData))
                {
                    if (previousGameState != null)
                    {
                        OpponentData previousOpponentData = null;
                        int previousOpponentSectorNumber = 0;
                        int previousOpponentCompletedLaps = 0;
                        int previousOpponentOverallPosition = 0;
                        Boolean previousOpponentIsEnteringPits = false;
                        Boolean previousOpponentIsExitingPits = false;
                        float previousOpponentSpeed = 0;
                        float previousDistanceRoundTrack = 0;
                        if (previousGameState.OpponentData.TryGetValue(driverName, out previousOpponentData))
                        {
                            previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                            previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                            previousOpponentOverallPosition = previousOpponentData.OverallPosition;
                            previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                            previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                            previousOpponentSpeed = previousOpponentData.Speed;
                            previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                        }
                        int currentOpponentSector = -1;
                        if (driver.currentSector == data.track.sectors)
                        {
                            currentOpponentSector = getSectorFromDistanceRoundTrack(driver.distanceRoundTrackNormalizes, data.track.normalizesSectorLimits);
                        }
                        else
                        {
                            currentOpponentSector = driver.currentSector + 1;
                        }
                        float currentOpponentLapDistance = currentGameState.SessionData.TrackDefinition != null ? currentGameState.SessionData.TrackDefinition.trackLength * driver.distanceRoundTrackNormalizes : 0;
                        int currentOpponentLapsCompleted = driver.lapCount;
                        Boolean finishedAllottedRaceLaps = currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted;
                        Boolean finishedAllottedRaceTime = false;

                        if (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
                            previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                        {
                            finishedAllottedRaceTime = true;
                        }
                        int currentOpponentOverallPosition = getRacePosition(driverName, previousOpponentOverallPosition, driver.realTimePosition, currentGameState.Now);
                        if (currentOpponentOverallPosition == 1 && (finishedAllottedRaceTime || finishedAllottedRaceLaps))
                        {
                            currentGameState.SessionData.LeaderHasFinishedRace = true;
                        }
                    }

                }
                else
                {
                    currentGameState.OpponentData.Add(driver.name, createOpponentData(driver, true,
                        currentGameState.SessionData.TrackDefinition.trackLength));
                }

            }

            currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CompletedLaps, playerDriver.speedMS, currentGameState.Now);

            currentGameState.sortClassPositions();
            currentGameState.setPracOrQualiDeltas();

            currentGameState.ControlData.BrakePedal = playerDriver.brake;
            currentGameState.ControlData.ThrottlePedal = playerDriver.trottle;

            if (currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.hardPartsOnTrackData.updateHardPartsForNewLap(currentGameState.SessionData.LapTimePrevious))
                {
                    currentGameState.SessionData.TrackDefinition.adjustGapPoints(currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap);
                }
            }
            else if (!currentGameState.PitData.OnOutLap && !currentGameState.SessionData.TrackDefinition.isOval &&
                !(currentGameState.SessionData.SessionType == SessionType.Race && (currentGameState.SessionData.CompletedLaps < 1 )))
            {
                currentGameState.hardPartsOnTrackData.mapHardPartsOnTrack(currentGameState.ControlData.BrakePedal, currentGameState.ControlData.ThrottlePedal,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CurrentLapIsValid, currentGameState.SessionData.TrackDefinition.trackLength);
            }

            return currentGameState;
        }
        private void updateOpponentData(OpponentData opponentData, int racePosition, int completedLaps)
        {

        }
        private int getSectorFromDistanceRoundTrack(float distance, float[]sectors)
        {
            int ret = 3;
            if (distance >= 0 && distance < sectors[0])
            {
                ret = 1;
            }
            if (distance >= sectors[0] && distance < sectors[1])
            {
                ret = 2;
            }
            return ret;
        }
        private OpponentData createOpponentData(Driver driver, Boolean loadDriverName, float trackLength)
        {
            String driverName = driver.name.ToLower();
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            }
            OpponentData opponentData = new OpponentData();
            opponentData.IsActive = true;
            opponentData.DriverRawName = driverName;
            opponentData.OverallPosition = driver.position;
            opponentData.CompletedLaps = driver.lapCount;
            opponentData.DistanceRoundTrack = driver.distanceRoundTrackNormalizes * trackLength;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.UtcNow);
            opponentData.CarClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.GT3);
            opponentData.CurrentSectorNumber = (int)driver.currentSector + 1;
            return opponentData;
        }
        private SessionType mapToSessionType(RaceSessionType sessionType, Boolean isLonePractice)
        {
            switch (sessionType)
            {
                case RaceSessionType.FreePractice1:
                case RaceSessionType.FreePractice2:
                    if (isLonePractice)
                    {
                        return SessionType.LonePractice;
                    }
                    else
                    {
                        return SessionType.Practice;
                    }
                case RaceSessionType.Hotstint:
                case RaceSessionType.Hotlap:
                    return SessionType.HotLap;
                case RaceSessionType.PreQualifying:
                case RaceSessionType.Qualifying:
                case RaceSessionType.Qualifying1:
                case RaceSessionType.Qualifying3:
                case RaceSessionType.Qualifying4:
                case RaceSessionType.Superpole:
                case RaceSessionType.HotlapSuperpole:
                    return SessionType.Qualify;
                case RaceSessionType.Race:
                    return SessionType.Race;        
            }
            return SessionType.Unavailable;
        }
        private SessionPhase mapToSessionPhase(RaceSessionPhase currentRaceSessionPhase, SessionType currentSessionType)
        {
            switch(currentSessionType)
            {
                case SessionType.Practice:
                case SessionType.LonePractice:
                    {
                        switch (currentRaceSessionPhase)
                        {
                            case RaceSessionPhase.StartingUI:                                
                                return SessionPhase.Garage;

                        }
                        return SessionPhase.Green;
                    }
                case SessionType.HotLap:
                case SessionType.Qualify:
                    return SessionPhase.Green;
                case SessionType.Race:
                    {
                        switch(currentRaceSessionPhase)
                        {
                            case RaceSessionPhase.StartingUI:
                                return SessionPhase.Unavailable;
                            case RaceSessionPhase.PreFormationTime:
                            case RaceSessionPhase.FormationTime: //here we are in our car on the grid waition to roll
                                return SessionPhase.Gridwalk;
                            case RaceSessionPhase.PreSessionTime:                            
                                return SessionPhase.Formation;
                            case RaceSessionPhase.SessionTime:
                                return SessionPhase.Green;
                            case RaceSessionPhase.SessionOverTime:
                                return SessionPhase.Checkered;
                            case RaceSessionPhase.ResultUI:
                            case RaceSessionPhase.PostSessionTime:
                                return SessionPhase.Finished;
                            default:
                                return SessionPhase.Unavailable;
                        }
                    }
                default:
                    return SessionPhase.Unavailable;
            }
        }
    }
}
