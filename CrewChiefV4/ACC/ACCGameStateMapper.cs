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
            TimeSpan ts = TimeSpan.FromTicks(time);
            return (float)ts.TotalMilliseconds * 10;
        }
        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            ACCSharedMemoryReader.ACCStructWrapper wrapper = (ACCSharedMemoryReader.ACCStructWrapper)structWrapper;            
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            ACCSharedMemoryData data = wrapper.data;
            
            if(data.isReady != 1)
            {
                return previousGameState;
            }
            if (!previousRaceSessionType.Equals(data.sessionData.currentSessionType))
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
            }
            
            //Console.WriteLine("tyre temp" + wrapper.physicsData.tyreTempM[0]);
            SessionType previousSessionType = SessionType.Unavailable;
            SessionPhase previousSessionPhase = SessionPhase.Unavailable;
            float previousSessionRunningTime = -1;
            if(previousGameState != null)
            {
                previousSessionType = previousGameState.SessionData.SessionType;
                previousSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                previousSessionPhase = previousGameState.SessionData.SessionPhase;
            }
            //test commit
            SessionType currentSessionType = mapToSessionType(data.sessionData.currentSessionType);
            currentGameState.SessionData.SessionType = currentSessionType;
            SessionPhase currentSessioPhase = mapToSessionPhase(data.sessionData.currentSessionPhase, currentSessionType);
            currentGameState.SessionData.SessionPhase = currentSessioPhase;

            currentGameState.SessionData.SessionRunningTime = (float)TimeSpan.FromMilliseconds((data.sessionData.physicsTime - data.sessionData.sessionStartTimeStamp)).TotalSeconds;           
            currentGameState.SessionData.SessionTimeRemaining = (float)TimeSpan.FromMilliseconds((data.sessionData.sessionEndTime - data.sessionData.physicsTime)).TotalSeconds;
            currentGameState.SessionData.SessionTotalRunTime = (float)data.sessionData.sessionDuration;
            currentGameState.SessionData.SessionHasFixedTime = true;
            
            Driver playerDriver = data.playerDriver;
            currentGameState.SessionData.NumCarsOverall = data.driverCount;
            //this still needs fixing
            if (currentSessionType != SessionType.Unavailable && 
                (previousRaceSessionPhase == RaceSessionPhase.StartingUI && data.sessionData.currentSessionPhase == RaceSessionPhase.PreFormationTime && currentSessionType == SessionType.Race))
            {
                currentGameState.SessionData.IsNewSession = true;
                PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
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

                currentGameState.SessionData.SessionTotalRunTime = (float)data.sessionData.sessionDuration;


                currentGameState.PitData.InPitlane = playerDriver.trackLocation != CarLocation.ECarLocation__Track;
                currentGameState.PositionAndMotionData.DistanceRoundTrack = Math.Abs(playerDriver.distanceRoundTrack * currentGameState.SessionData.TrackDefinition.trackLength);

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
            else if(currentSessionType != SessionType.Unavailable)
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
                        currentGameState.SessionData.SessionTotalRunTime = data.sessionData.sessionDuration;

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

                    currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
                    currentGameState.retriedDriverNames = previousGameState.retriedDriverNames;
                    currentGameState.disqualifiedDriverNames = previousGameState.disqualifiedDriverNames;
                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;

                    currentGameState.TimingData = previousGameState.TimingData;
                }
            }
            if(playerDriver.isCarOutOfTrack == 1 && playerDriver.trackLocation != CarLocation.ECarLocation__PitLane && 
                playerDriver.trackLocation != CarLocation.ECarLocation__PitEntry && playerDriver.trackLocation != CarLocation.ECarLocation__PitLane)
            {
                Console.WriteLine("player car out of track");
            }
            currentGameState.SessionData.OverallPosition = playerDriver.realTimePosition;
            previousRaceSessionPhase = data.sessionData.currentSessionPhase;
            //currentGameState.SessionData.SessionPhase = SessionPhase.Green;
            return currentGameState;
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
            opponentData.DistanceRoundTrack = driver.distanceRoundTrack * trackLength;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.UtcNow);
            opponentData.CarClass = CarData.getCarClassFromEnum(CarData.CarClassEnum.GT3);
            opponentData.CurrentSectorNumber = (int)driver.currentSector + 1;
            return opponentData;
        }
        private SessionType mapToSessionType(RaceSessionType sessionType)
        {
            switch (sessionType)
            {
                case RaceSessionType.FreePractice1:
                case RaceSessionType.FreePractice2:
                    return SessionType.Practice;
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
                case SessionType.HotLap:
                case SessionType.Qualify:
                    return SessionPhase.Green;
                case SessionType.Race:
                    {
                        switch(currentRaceSessionPhase)
                        {
                            case RaceSessionPhase.StartingUI:
                                return SessionPhase.Garage;
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
