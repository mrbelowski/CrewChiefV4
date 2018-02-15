﻿using System;
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
        public iRacingGameStateMapper()
        {

        }

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in r3e shared data so this is a no-op
        }

        public override void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            speechRecogniser.addiRacingSpeechRecogniser();
            this.speechRecogniser = speechRecogniser;
        }

        Dictionary<string, DateTime> lastActiveTimeForOpponents = new Dictionary<string, DateTime>();
        DateTime nextOpponentCleanupTime = DateTime.MinValue;
        TimeSpan opponentCleanupInterval = TimeSpan.FromSeconds(3);
        string prevTrackSurface = "";

        // next track conditions sample due after:
        private DateTime nextConditionsSampleDue = DateTime.MinValue;
        private DateTime lastTimeEngineWasRunning = DateTime.MaxValue;
        private DateTime lastTimeEngineWaterTempWarning = DateTime.MaxValue;
        private DateTime lastTimeEngineOilPressureWarning = DateTime.MaxValue;
        private DateTime lastTimeEngineFuelPressureWarning = DateTime.MaxValue;
        class PendingRacePositionChange
        {
            public int newPosition;
            public DateTime positionChangeTime;
            public PendingRacePositionChange(int newPosition, DateTime positionChangeTime)
            {
                this.newPosition = newPosition;
                this.positionChangeTime = positionChangeTime;
            }
        }
        public override GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            if (memoryMappedFileStruct == null)
            {
                return null;
            }
            CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper wrapper = (CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            Sim shared = wrapper.data;

            if (shared.Telemetry.IsReplayPlaying)
            {
                CrewChief.trackName = shared.SessionData.Track.CodeName;
                CrewChief.carClass = CarData.getCarClassForIRacingId(shared.Driver.Car.CarClassId, shared.Driver.Car.CarId).carClassEnum;
                CrewChief.viewingReplay = true;
                CrewChief.distanceRoundTrack = shared.Driver.Live.CorrectedLapDistance * ((float)shared.SessionData.Track.Length * 1000);
            }

            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;
            int? previousSessionNumber = -1;
            int previousSessionId = -1;
            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionType = previousGameState.SessionData.SessionType;
                currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                currentGameState.readLandmarksForThisLap = previousGameState.readLandmarksForThisLap;
                previousSessionNumber = previousGameState.SessionData.SessionIteration;
                previousSessionId = previousGameState.SessionData.SessionId;
            }
            currentGameState.SessionData.SessionType = mapToSessionType(shared.SessionData.SessionType);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Telemetry.SessionTime;
            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;

            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            int sessionNumberOfLaps = previousGameState == null ? 0 : previousGameState.SessionData.SessionNumberOfLaps;

            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase, shared.Telemetry.SessionState, currentGameState.SessionData.SessionType, shared.Telemetry.IsReplayPlaying,
                (float)shared.Telemetry.SessionTime, previousLapsCompleted, shared.Telemetry.LapCompleted, (SessionFlags)shared.Telemetry.SessionFlags, shared.Telemetry.IsInGarage,
                shared.SessionData.IsLimitedTime, shared.Telemetry.SessionTimeRemain, shared.Telemetry.SessionTime, sessionNumberOfLaps);

            currentGameState.SessionData.NumCarsOverallAtStartOfSession = shared.Drivers.Count;

            int sessionNumber = shared.Telemetry.SessionNum;


            currentGameState.SessionData.SessionIteration = sessionNumber;

            currentGameState.SessionData.SessionId = shared.SessionData.SessionId;

            int PlayerCarIdx = shared.Telemetry.PlayerCarIdx;

            if (shared.Driver != null)
            {
                playerCar = shared.Driver;
                playerName = playerCar.Name.ToLower();
            }

            Validator.validate(playerName);

            /*
            if (!prevTrackSurface.Equals(playerCar.Live.TrackSurface.ToString()))
            {
                Console.WriteLine(playerCar.Live.TrackSurface.ToString());
                prevTrackSurface = playerCar.Live.TrackSurface.ToString();
            }*/

            if (currentGameState.SessionData.SessionType != SessionType.Unavailable && shared.Telemetry.IsNewSession
                && (lastSessionType != currentGameState.SessionData.SessionType || sessionNumber != previousSessionNumber || previousSessionId != currentGameState.SessionData.SessionId))
            {
                currentGameState.SessionData.IsNewSession = true;
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("sessionType = " + currentGameState.SessionData.SessionType);
                Console.WriteLine("lastSessionPhase = " + lastSessionPhase);
                Console.WriteLine("lastSessionRunningTime = " + lastSessionRunningTime);
                Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("rawSessionPhase = " + shared.Telemetry.SessionState);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                Console.WriteLine("NumCarsAtStartOfSession = " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);

                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.PlayerLapData.Clear();
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;

                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName, 0, (float)shared.SessionData.Track.Length * 1000);

                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(shared.SessionData.Track.CodeName, currentGameState.SessionData.TrackDefinition.trackLength);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval || shared.SessionData.Track.IsOval;
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

                if (!shared.SessionData.IsLimitedSessionLaps)
                {
                    currentGameState.SessionData.SessionHasFixedTime = true;
                    currentGameState.SessionData.SessionTotalRunTime = (float)shared.SessionData.RaceTime;
                    Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                }

                currentGameState.SessionData.MaxIncidentCount = shared.SessionData.IncidentLimit;
                currentGameState.SessionData.CurrentIncidentCount = shared.Telemetry.PlayerCarMyIncidentCount;
                currentGameState.SessionData.CurrentDriverIncidentCount = shared.Telemetry.PlayerCarDriverIncidentCount;
                currentGameState.SessionData.CurrentTeamIncidentCount = shared.Telemetry.PlayerCarTeamIncidentCount;

                lastActiveTimeForOpponents.Clear();
                nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
                 
                currentGameState.PitData.InPitlane = shared.Telemetry.OnPitRoad;
                currentGameState.PositionAndMotionData.DistanceRoundTrack = Math.Abs(playerCar.Live.CorrectedLapDistance * currentGameState.SessionData.TrackDefinition.trackLength);

                if ((currentGameState.SessionData.SessionType == SessionType.Practice || currentGameState.SessionData.SessionType == SessionType.Qualify)
                     && playerCar.Live.TrackSurface == TrackSurfaces.InPitStall && currentGameState.PositionAndMotionData.DistanceRoundTrack != 0)
                {
                    currentGameState.PitData.PitBoxPositionEstimate = currentGameState.PositionAndMotionData.DistanceRoundTrack;
                    Console.WriteLine("pit box position = " + currentGameState.PitData.PitBoxPositionEstimate);
                }
                else if (previousGameState != null)
                {
                    // if we're entering a race session or rolling qually, copy the value from the previous field
                    currentGameState.PitData.PitBoxPositionEstimate = previousGameState.PitData.PitBoxPositionEstimate;
                }

                //TODO update car classes
                currentGameState.carClass = CarData.getCarClassForIRacingId(playerCar.Car.CarClassId, playerCar.Car.CarId);
                CarData.IRACING_CLASS_ID = playerCar.Car.CarClassId;
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier() + " (car ID " + playerCar.Car.CarId + ")");
                currentGameState.SessionData.PlayerCarNr = Parser.ParseInt(playerCar.CarNumber);

                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
                currentGameState.SessionData.SectorNumber = playerCar.Live.CurrentSector;

                foreach (Driver driver in shared.Drivers)
                {
                    if (driver.IsCurrentDriver || driver.CurrentResults.IsOut || driver.IsPacecar || driver.Live.TrackSurface.HasFlag(TrackSurfaces.NotInWorld) || driver.IsSpectator)
                    {
                        continue;
                    }
                    else
                    {
                        currentGameState.OpponentData.Add(driver.Id.ToString(), createOpponentData(driver, true, 
                            CarData.getCarClassForIRacingId(driver.Car.CarClassId, driver.Car.CarId).carClassEnum, currentGameState.SessionData.TrackDefinition.trackLength));
                    }
                }
                // add a conditions sample when we first start a session so we're not using stale or default data in the pre-lights phase
                currentGameState.Conditions.addSample(currentGameState.Now, 0, 1, shared.Telemetry.AirTemp, shared.Telemetry.TrackTemp, 0, shared.Telemetry.WindVel, 0, 0, 0, true);

                //need to call this after adding opponents else we have nothing to compare against 
                Utilities.TraceEventClass(currentGameState);
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + lastSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        currentGameState.SessionData.JustGoneGreen = true;
                        // just gone green, so get the session data

                        if (shared.SessionData.IsLimitedSessionLaps)
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        else
                        {
                            currentGameState.SessionData.SessionHasFixedTime = true;
                            currentGameState.SessionData.SessionTotalRunTime = (float)shared.SessionData.RaceTime;
                        }
                        currentGameState.SessionData.SessionNumberOfLaps = Parser.ParseInt(shared.SessionData.RaceLaps);


                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.Track.CodeName, 0, (float)shared.SessionData.Track.Length * 1000);

                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(shared.SessionData.Track.CodeName, currentGameState.SessionData.TrackDefinition.trackLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                        currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval || shared.SessionData.Track.IsOval;
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);

                        currentGameState.carClass = CarData.getCarClassForIRacingId(playerCar.Car.CarClassId, playerCar.Car.CarId);
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier() + " (car ID " + playerCar.Car.CarId + ")");
                        currentGameState.SessionData.PlayerCarNr = Parser.ParseInt(playerCar.CarNumber);

                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                            currentGameState.PitData.PitBoxPositionEstimate = previousGameState.PitData.PitBoxPositionEstimate;
                        }
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;


                        lastActiveTimeForOpponents.Clear();
                        nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
                        lastTimeEngineWasRunning = DateTime.MaxValue;
                        lastTimeEngineWaterTempWarning = DateTime.MaxValue;
                        lastTimeEngineOilPressureWarning = DateTime.MaxValue;
                        lastTimeEngineFuelPressureWarning = DateTime.MaxValue;
                        //currentGameState.SessionData.CompletedLaps = shared.Driver.CurrentResults.LapsComplete;

                        Console.WriteLine("Just gone green, session details...");

                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                        Console.WriteLine("SessionStartPosition " + currentGameState.SessionData.SessionStartPosition);
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
                    currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
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
                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.SessionData.CompletedLaps = previousGameState.SessionData.CompletedLaps;
                    currentGameState.FlagData.useImprovisedIncidentCalling = previousGameState.FlagData.useImprovisedIncidentCalling;
                    currentGameState.OpponentData = previousGameState.OpponentData;
                    currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
                    currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;
                    currentGameState.SessionData.MaxIncidentCount = previousGameState.SessionData.MaxIncidentCount;
                    currentGameState.SessionData.CurrentIncidentCount = previousGameState.SessionData.CurrentIncidentCount;
                    currentGameState.SessionData.CurrentDriverIncidentCount = previousGameState.SessionData.CurrentDriverIncidentCount;
                    currentGameState.SessionData.CurrentTeamIncidentCount = previousGameState.SessionData.CurrentTeamIncidentCount;
                    currentGameState.SessionData.HasLimitedIncidents = previousGameState.SessionData.HasLimitedIncidents;
                    currentGameState.SessionData.StrengthOfField = previousGameState.SessionData.StrengthOfField;
                    currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
                }
            }

            currentGameState.ControlData.ThrottlePedal = shared.Telemetry.Throttle;
            currentGameState.ControlData.ClutchPedal = shared.Telemetry.Clutch;
            currentGameState.ControlData.BrakePedal = shared.Telemetry.Brake;
            currentGameState.TransmissionData.Gear = shared.Telemetry.Gear;

            currentGameState.SessionData.CurrentIncidentCount = shared.Telemetry.PlayerCarMyIncidentCount;
            currentGameState.SessionData.CurrentDriverIncidentCount = shared.Telemetry.PlayerCarDriverIncidentCount;
            currentGameState.SessionData.CurrentTeamIncidentCount = shared.Telemetry.PlayerCarTeamIncidentCount;
            currentGameState.SessionData.HasLimitedIncidents = shared.SessionData.IsLimitedIncidents;
            currentGameState.SessionData.MaxIncidentCount = shared.SessionData.IncidentLimit;
            currentGameState.SessionData.LicenseLevel = playerCar.licensLevel;
            currentGameState.SessionData.iRating = playerCar.IRating;

            currentGameState.EngineData.EngineOilTemp = shared.Telemetry.OilTemp;
            currentGameState.EngineData.EngineWaterTemp = shared.Telemetry.WaterTemp;

            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 0;

            bool additionalEngineCheckFlags = shared.Telemetry.IsOnTrack && !shared.Telemetry.OnPitRoad && shared.Telemetry.Voltage > 0f;

            if (!shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.EngineStalled)) 
            { 
                lastTimeEngineWasRunning = currentGameState.Now; 
            } 
            if (previousGameState != null && !previousGameState.EngineData.EngineStalledWarning &&
                currentGameState.SessionData.SessionRunningTime > 60 && shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.EngineStalled) &&
                lastTimeEngineWasRunning < currentGameState.Now.Subtract(TimeSpan.FromSeconds(2)) && additionalEngineCheckFlags)
            {
                currentGameState.EngineData.EngineStalledWarning = true;
                lastTimeEngineWasRunning = DateTime.MaxValue;
                
            }

            if (!shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.WaterTemperatureWarning) )
            {
                lastTimeEngineWaterTempWarning = currentGameState.Now;
            }
            
            if (previousGameState != null && !previousGameState.EngineData.EngineWaterTempWarning && !shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.EngineStalled) &&
                currentGameState.SessionData.SessionRunningTime > 60 && shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.WaterTemperatureWarning) &&
                lastTimeEngineWaterTempWarning < currentGameState.Now.Subtract(TimeSpan.FromSeconds(2)) && additionalEngineCheckFlags)
            {
                currentGameState.EngineData.EngineWaterTempWarning = true;
                lastTimeEngineWaterTempWarning = DateTime.MaxValue;
            }

            if (!shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.OilPressureWarning))
            {
                lastTimeEngineOilPressureWarning = currentGameState.Now;
            }
            if (previousGameState != null && !previousGameState.EngineData.EngineWaterTempWarning && !shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.EngineStalled) &&
                currentGameState.SessionData.SessionRunningTime > 60 && shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.OilPressureWarning) &&
                lastTimeEngineOilPressureWarning < currentGameState.Now.Subtract(TimeSpan.FromSeconds(2)) && additionalEngineCheckFlags)
            {
                currentGameState.EngineData.EngineWaterTempWarning = true;
                lastTimeEngineOilPressureWarning = DateTime.MaxValue;
            }
            if (!shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.FuelPressureWarning))
            {
                lastTimeEngineFuelPressureWarning = currentGameState.Now;
            }
            if (previousGameState != null && !previousGameState.EngineData.EngineWaterTempWarning && !shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.EngineStalled) &&
                currentGameState.SessionData.SessionRunningTime > 60 && shared.Telemetry.EngineWarnings.HasFlag(EngineWarnings.FuelPressureWarning) &&
                lastTimeEngineFuelPressureWarning < currentGameState.Now.Subtract(TimeSpan.FromSeconds(2)) && additionalEngineCheckFlags)
            {
                currentGameState.EngineData.EngineWaterTempWarning = true;
                lastTimeEngineFuelPressureWarning = DateTime.MaxValue;
            }


            //Console.WriteLine("Voltage: " + shared.Telemetry.Voltage);
            //TODO add yellow 
            SessionFlags flag = (SessionFlags)shared.Telemetry.SessionFlags;
            if (flag.HasFlag(SessionFlags.Black) && !flag.HasFlag(SessionFlags.Furled))
            {
                currentGameState.PenaltiesData.HasStopAndGo = true;
            }
            if (flag.HasFlag(SessionFlags.Black) && flag.HasFlag(SessionFlags.Furled))
            {
                currentGameState.PenaltiesData.HasSlowDown = true;
            }
            if (flag.HasFlag(SessionFlags.Yellow))
            {
                currentGameState.FlagData.isLocalYellow = true;
            }
            if (previousGameState != null && !previousGameState.SessionData.Flag.HasFlag(FlagEnum.BLUE) && flag.HasFlag(SessionFlags.Blue))
            {
                currentGameState.SessionData.Flag = FlagEnum.BLUE;
            }
            currentGameState.SessionData.CompletedLaps = playerCar.Live.LapsCompleted;
            //TODO validate laptimes
            currentGameState.SessionData.LapTimeCurrent = shared.Telemetry.LapCurrentLapTime;
            if (playerCar.Live.Lap <= 0)
            {
                currentGameState.SessionData.CurrentLapIsValid = false;
            }
            else
            {
                currentGameState.SessionData.CurrentLapIsValid = true;
            }

            currentGameState.SessionData.NumCarsOverall = shared.Drivers.Count;

            if (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown && currentGameState.SessionData.SessionType == SessionType.Race)
            {
                currentGameState.SessionData.OverallPosition = playerCar.CurrentResults.QualifyingPosition;
            }
            else
            {
                currentGameState.SessionData.OverallPosition = currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null ?
                    getRacePosition(currentGameState.SessionData.DriverRawName, previousGameState.SessionData.OverallPosition, playerCar.Live.Position, currentGameState.Now)
                    : playerCar.Live.Position;
            }

            if (currentGameState.SessionData.SessionType != SessionType.Race)
            {
                currentGameState.SessionData.ClassPosition = playerCar.Live.ClassPosition;
            }
            /*Driver fastestPlayerClassDriver = shared.Drivers.OrderBy(d => d.CurrentResults.FastestTime).Where(e => e.Car.CarClassId == playerCar.Car.CarClassId && 
                e.CurrentResults.FastestTime > 1 && !e.IsPacecar && !shared.Telemetry.CarIdxTrackSurface[e.Id].HasFlag(TrackSurfaces.NotInWorld)).FirstOrDefault();
            if (fastestPlayerClassDriver != null && !shared.Telemetry.IsReplayPlaying)
            {
                currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = fastestPlayerClassDriver.CurrentResults.FastestTime;
            }*/

            //Console.WriteLine("fastest " + currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass);

            if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                currentGameState.SessionData.OverallSessionBestLapTime > shared.Telemetry.LapBestLapTime)
            {
                currentGameState.SessionData.OverallSessionBestLapTime = shared.Telemetry.LapBestLapTime;
            }

            if (currentGameState.SessionData.OverallPosition == 1)
            {
                currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
            }

            int currentSector = playerCar.Live.CurrentSector;

            currentGameState.SessionData.IsNewSector = currentGameState.SessionData.SectorNumber != currentSector;
            currentGameState.SessionData.IsNewLap = currentGameState.HasNewLapData(previousGameState, playerCar.Live.LapTimePrevious, playerCar.Live.HasCrossedSFLine) && playerCar.Live.Lap >=1;

            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.readLandmarksForThisLap = false;
            }

            currentGameState.SessionData.SectorNumber = currentSector;
            currentGameState.PitData.InPitlane = shared.Telemetry.CarIdxOnPitRoad[PlayerCarIdx] || playerCar.Live.TrackSurface == TrackSurfaces.InPitStall;

            if (currentGameState.SessionData.IsNewSector || currentGameState.SessionData.IsNewLap)
            {
                Boolean validSpeed = true;
                if (playerCar.Live.Speed > 500)
                {
                    validSpeed = false;
                }
                if (currentSector == 1 && currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.OverallPosition;
                    currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(playerCar.Live.LapTimePrevious).ToString(@"mm\:ss\.fff"));
                    Console.WriteLine(TimeSpan.FromSeconds(playerCar.Live.LapTimePrevious).ToString(@"mm\:ss\.fff"));

                    currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                       playerCar.Live.LapTimePrevious, validSpeed && currentGameState.SessionData.PreviousLapWasValid, false, shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp,
                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3);

                    currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                        currentGameState.SessionData.OverallPosition, currentGameState.PitData.InPitlane, currentGameState.GameTimeWhenLastCrossedStartFinishLine, false,
                        shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp);

                    if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                    {
                        currentGameState.carClass = CarData.getCarClassForIRacingId(playerCar.Car.CarClassId, playerCar.Car.CarId);
                    }
                }
                else if (currentSector == 2)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(1, currentGameState.SessionData.OverallPosition, shared.Telemetry.LapCurrentLapTime,
                        currentGameState.SessionData.SessionRunningTime, validSpeed && currentGameState.SessionData.CurrentLapIsValid, false, shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp);

                }
                else if (currentSector == 3)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(2, currentGameState.SessionData.OverallPosition, shared.Telemetry.LapCurrentLapTime,
                        currentGameState.SessionData.SessionRunningTime, validSpeed && currentGameState.SessionData.CurrentLapIsValid, false, shared.Telemetry.TrackTemp, shared.Telemetry.AirTemp);
                }
            }

            currentGameState.PositionAndMotionData.DistanceRoundTrack = Math.Abs(currentGameState.SessionData.TrackDefinition.trackLength * playerCar.Live.CorrectedLapDistance);
            currentGameState.PositionAndMotionData.CarSpeed = (float)shared.Telemetry.Speed;

            currentGameState.PositionAndMotionData.Orientation.Pitch = shared.Telemetry.Pitch;
            currentGameState.PositionAndMotionData.Orientation.Roll = shared.Telemetry.Roll;
            currentGameState.PositionAndMotionData.Orientation.Yaw = shared.Telemetry.Yaw;

            //experimantal
            if((playerCar.Live.TrackSurface == TrackSurfaces.InPitStall && previousGameState != null && previousGameState.PitData.PitBoxPositionEstimate == -1) ||
                (playerCar.Live.TrackSurface == TrackSurfaces.InPitStall && previousGameState != null &&
                Math.Abs(previousGameState.PitData.PitBoxPositionEstimate - currentGameState.PositionAndMotionData.DistanceRoundTrack) > 5 && currentGameState.PositionAndMotionData.DistanceRoundTrack != 0))
            {
                currentGameState.PitData.PitBoxPositionEstimate = currentGameState.PositionAndMotionData.DistanceRoundTrack;
                Console.WriteLine("pit box position = " + currentGameState.PitData.PitBoxPositionEstimate);
            }

            currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CompletedLaps,
                (float)playerCar.Live.Speed, currentGameState.Now);

            if (previousGameState != null)
            {
                String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                    currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.PositionAndMotionData.CarSpeed);
                currentGameState.SessionData.stoppedInLandmark = currentGameState.PitData.InPitlane ? null : stoppedInLandmark;
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

            GameStateData.Multiclass = false;
            foreach (Driver driver in shared.Drivers)
            {
                String opponentDataKey = driver.Id.ToString();
                List<int> combinedStrengthOfField = new List<int>();
                if(driver.IsPacecar || driver.IsSpectator)
                {
                    continue;
                }                
                combinedStrengthOfField.Add(driver.IRating);
                                
                if (driver.IsCurrentDriver || driver.CurrentResults.IsOut || driver.Live.TrackSurface.HasFlag(TrackSurfaces.NotInWorld))
                {
                    continue;
                }

                String driverName = driver.Name.ToLower();
                lastActiveTimeForOpponents[opponentDataKey] = currentGameState.Now;
                Boolean createNewDriver = true;
                OpponentData currentOpponentData = null;
                if (currentGameState.OpponentData.TryGetValue(opponentDataKey, out currentOpponentData))
                {
                    if (shared.SessionData.IsTeamRacing || driver.CustId == currentOpponentData.CostId)
                    {
                        createNewDriver = false;
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
                            bool previousIsInPits = false;
                            bool hasCrossedSFLine = false;
                            bool previousIsApporchingPits = false;
                            Boolean previousOpponentDataWaitingForNewLapData = false;
                            DateTime previousOpponentNewLapDataTimerExpiry = DateTime.MaxValue;
                            int previousCompleatedLapsWhenHasNewLapDataWasLastTrue = -2;
                            float previousOpponentLastLapTime = -1;
                            Boolean previousOpponentLastLapValid = false;
                            float previousOpponentGameTimeWhenLastCrossedStartFinishLine = -1;
                            OpponentData opponentPrevious = null;
                            if (previousGameState.OpponentData.TryGetValue(opponentDataKey, out opponentPrevious))
                            {
                                previousOpponentData = opponentPrevious;
                                previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                previousOpponentOverallPosition = previousOpponentData.OverallPosition;
                                previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                previousOpponentSpeed = previousOpponentData.Speed;
                                previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                                previousIsInPits = previousOpponentData.InPits;

                                previousOpponentDataWaitingForNewLapData = previousOpponentData.WaitingForNewLapData;
                                previousOpponentNewLapDataTimerExpiry = previousOpponentData.NewLapDataTimerExpiry;
                                previousCompleatedLapsWhenHasNewLapDataWasLastTrue = previousOpponentData.CompleatedLapsWhenHasNewLapDataWasLastTrue;
                                previousOpponentGameTimeWhenLastCrossedStartFinishLine = previousOpponentData.GameTimeWhenLastCrossedStartFinishLine;
                                previousOpponentLastLapTime = previousOpponentData.LastLapTime;
                                previousOpponentLastLapValid = previousOpponentData.LastLapValid;
                                previousIsApporchingPits = previousOpponentData.isApporchingPits;
                                currentOpponentData.ClassPositionAtPreviousTick = previousOpponentData.ClassPosition;
                                currentOpponentData.OverallPositionAtPreviousTick = previousOpponentData.OverallPosition;
                            }

                            hasCrossedSFLine = driver.Live.HasCrossedSFLine;
                            int currentOpponentSector = driver.Live.CurrentSector;

                            currentOpponentData.IsActive = true;
                            bool previousOpponentLapValid = driver.Live.PreviousLapWasValid;

                            currentOpponentData.isApporchingPits = driver.Live.TrackSurface == TrackSurfaces.AproachingPits && !shared.Telemetry.CarIdxOnPitRoad[driver.Id];
                            // TODO:
                            // reset to to pitstall
                            bool currentOpponentLapValid = true;
                            if ((previousOpponentSectorNumber == 1 || previousOpponentSectorNumber == 2) && !previousIsInPits && shared.Telemetry.CarIdxOnPitRoad[driver.Id])
                            {
                                currentOpponentLapValid = false;
                            }
                            Boolean carIsSameAsPlayer = CarData.IsCarClassEqual(currentOpponentData.CarClass, currentGameState.carClass);
                            if (!carIsSameAsPlayer)
                            {
                                GameStateData.Multiclass = true;
                            }
                            int currentOpponentOverallPosition = currentGameState.SessionData.SessionType == SessionType.Race && previousOpponentOverallPosition > 0 ?
                                getRacePosition(opponentDataKey, previousOpponentOverallPosition, driver.Live.Position, currentGameState.Now)
                                : driver.Live.Position;

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
                            if (currentOpponentOverallPosition == 1 && (finishedAllottedRaceTime || finishedAllottedRaceLaps))
                            {
                                currentGameState.SessionData.LeaderHasFinishedRace = true;
                            }

                            currentOpponentData.LicensLevel = driver.licensLevel;
                            currentOpponentData.iRating = driver.IRating;

                            updateOpponentData(currentOpponentData, driverName, driver.CustId, currentOpponentOverallPosition, currentOpponentLapsCompleted,
                                        currentOpponentSector, (float)driver.Live.LapTimePrevious, hasCrossedSFLine,
                                        shared.Telemetry.CarIdxOnPitRoad[driver.Id] || driver.Live.TrackSurface == TrackSurfaces.InPitStall, previousIsApporchingPits, previousOpponentLapValid, currentOpponentLapValid, currentGameState.SessionData.SessionRunningTime, currentOpponentLapDistance,
                                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                        currentGameState.SessionData.SessionType == SessionType.Race, shared.Telemetry.TrackTemp,
                                        shared.Telemetry.AirTemp, currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks, (float)driver.Live.Speed,
                                        previousOpponentDataWaitingForNewLapData, previousOpponentNewLapDataTimerExpiry,
                                        previousOpponentLastLapTime, previousOpponentLastLapValid, previousCompleatedLapsWhenHasNewLapDataWasLastTrue, previousOpponentGameTimeWhenLastCrossedStartFinishLine);

                            if (currentGameState.SessionData.SessionType != SessionType.Race)
                            {
                                currentOpponentData.ClassPosition = driver.Live.ClassPosition;
                            }
                            //allow gaps in qual and prac, delta here is not on track delta but diff on fastest time 
                            else if (currentGameState.SessionData.SessionType != SessionType.Race)
                            {
                                if (currentOpponentOverallPosition == currentGameState.SessionData.ClassPosition + 1)
                                {
                                    currentGameState.SessionData.TimeDeltaBehind = Math.Abs(currentOpponentData.CurrentBestLapTime - currentGameState.SessionData.PlayerLapTimeSessionBest);
                                }
                                if (currentOpponentOverallPosition == currentGameState.SessionData.ClassPosition - 1)
                                {
                                    currentGameState.SessionData.TimeDeltaFront = Math.Abs(currentGameState.SessionData.PlayerLapTimeSessionBest - currentOpponentData.CurrentBestLapTime);
                                }
                            }

                            if (previousOpponentData != null)
                            {
                                currentOpponentData.trackLandmarksTiming = previousOpponentData.trackLandmarksTiming;
                                String stoppedInLandmark = currentOpponentData.trackLandmarksTiming.updateLandmarkTiming(
                                    currentGameState.SessionData.TrackDefinition, currentGameState.SessionData.SessionRunningTime,
                                    previousDistanceRoundTrack, currentOpponentData.DistanceRoundTrack, currentOpponentData.Speed);
                                currentOpponentData.stoppedInLandmark = shared.Telemetry.CarIdxOnPitRoad[driver.Id] ? null : stoppedInLandmark;
                            }
                            if (currentGameState.SessionData.JustGoneGreen)
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
                        Console.WriteLine("Removing driver " + currentOpponentData.DriverRawName + " and replacing with " + driverName);
                        currentGameState.OpponentData.Remove(opponentDataKey);
                    }
                }
                if (createNewDriver)
                {
                    if (!driver.CurrentResults.IsOut || !driver.IsPacecar || !driver.Live.TrackSurface.HasFlag(TrackSurfaces.NotInWorld) || !driver.IsSpectator)
                    {
                        currentGameState.OpponentData.Add(opponentDataKey, createOpponentData(driver,
                            false, CarData.getCarClassForIRacingId(driver.Car.CarClassId, driver.Car.CarId).carClassEnum, currentGameState.SessionData.TrackDefinition.trackLength));
                    }
                }
                currentGameState.SessionData.StrengthOfField = (int)combinedStrengthOfField.Average();
            }
            if (currentGameState.Now > nextOpponentCleanupTime)
            {
                nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;
                DateTime oldestAllowedUpdate = currentGameState.Now - opponentCleanupInterval;
                foreach (KeyValuePair<string, OpponentData> entry in currentGameState.OpponentData)
                {
                    DateTime lastTimeForOpponent = DateTime.MinValue;
                    if (!lastActiveTimeForOpponents.TryGetValue(entry.Key, out lastTimeForOpponent) || (lastTimeForOpponent < oldestAllowedUpdate && entry.Value.IsActive))
                    {
                        entry.Value.IsActive = false;
                        entry.Value.InPits = true;
                        entry.Value.Speed = 0;
                        entry.Value.stoppedInLandmark = null;
                        entry.Value.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                        entry.Value.DeltaTime.SetNextDeltaPoint(0, entry.Value.CompletedLaps, 0, currentGameState.Now);
                        Console.WriteLine("Opponent " + entry.Value.DriverRawName + "(index " + entry.Key + ") has been inactive for " + opponentCleanupInterval + ", sending him back to pits");
                    }
                }
            }
            //Sort class positions
            if(currentGameState.SessionData.SessionType == SessionType.Race)
            {
                currentGameState.sortClassPositions();
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

            //conditions
            if (currentGameState.Now > nextConditionsSampleDue)
            {
                nextConditionsSampleDue = currentGameState.Now.Add(ConditionsMonitor.ConditionsSampleFrequency);
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.Telemetry.AirTemp, shared.Telemetry.TrackTemp, 0, shared.Telemetry.WindVel, 0, 0, 0, currentGameState.SessionData.IsNewLap);
            }



            currentGameState.PenaltiesData.IsOffRacingSurface = shared.Telemetry.PlayerTrackSurface == TrackSurfaces.OffTrack;
            if (!currentGameState.PitData.OnOutLap && previousGameState != null && !previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface
            && !(currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Countdown)
            && previousGameState.PenaltiesData.NumPenalties < shared.Telemetry.PlayerCarMyIncidentCount)
            {
                Console.WriteLine("Player off track");
                currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
            }
            currentGameState.PenaltiesData.NumPenalties = shared.Telemetry.PlayerCarMyIncidentCount;

            currentGameState.TyreData.FrontLeftPressure = shared.Telemetry.LFcoldPressure;
            currentGameState.TyreData.FrontRightPressure = shared.Telemetry.RFcoldPressure;
            currentGameState.TyreData.RearLeftPressure = shared.Telemetry.LRcoldPressure;
            currentGameState.TyreData.RearRightPressure = shared.Telemetry.RRcoldPressure;
            //Console.WriteLine("Speed:" + playerCar.SpeedKph);


            // Console.WriteLine("Session running time = " + currentGameState.SessionData.SessionRunningTime + " type = " + currentGameState.SessionData.SessionType + " phase " + currentGameState.SessionData.SessionPhase + " run time = " + currentGameState.SessionData.SessionTotalRunTime);

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

        private void updateOpponentData(OpponentData opponentData, String driverName, int CostId, int racePosition, int completedLaps,
            int sector, float completedLapTime, Boolean hasCrossedSFLine, Boolean isInPits, bool previousIsApporchingPits,
            Boolean previousLapWasValid, Boolean currentLapValid, float sessionRunningTime,
            float distanceRoundTrack, Boolean sessionLengthIsTime, float sessionTimeRemaining,
            Boolean isRace, float airTemperature, float trackTempreture, float nearPitEntryPointDistance, float speed,
            /* previous tick data for hasNewLapData check*/
            Boolean previousOpponentDataWaitingForNewLapData,
            DateTime previousOpponentNewLapDataTimerExpiry, float previousOpponentLastLapTime, Boolean previousOpponentLastLapValid,
            int previousCompleatedLapsWhenHasNewLapDataWasLastTrue, float previousOpponentGameTimeWhenLastCrossedStartFinishLine)
        {
            if (opponentData.CostId != CostId)
            {
                Console.WriteLine("Driver " + opponentData.DriverRawName + " has been swapped for " + driverName);
                opponentData.DriverRawName = driverName;
                opponentData.CostId = CostId;
                speechRecogniser.addNewOpponentName(driverName);
            }
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            Boolean validSpeed = true;
            if (speed > 500)
            {
                // faster than 500m/s (1000+mph) suggests the player has quit to the pit. Might need to reassess this as the data are quite noisy
                validSpeed = false;
                opponentData.Speed = 0;
                Console.WriteLine(opponentData.DriverRawName + " invalidating lap based of car speed = " + speed + "m/s");
            }
            else
            {
                opponentData.Speed = speed;
            }
            if (opponentData.OverallPosition != racePosition)
            {
                opponentData.SessionTimeAtLastPositionChange = sessionRunningTime;
            }

            opponentData.OverallPosition = racePosition;
            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.ClassPosition;
            }

            opponentData.IsNewLap = false;

            if (sessionRunningTime > 10 && isRace && !opponentData.InPits && isInPits)
            {
                opponentData.NumPitStops++;
            }
            //Check that previous state was IsApporchingPits, this includes the zone befor the pitlane(striped lines on track)
            opponentData.JustEnteredPits = previousIsApporchingPits && isInPits && !opponentData.InPits;
            if (opponentData.JustEnteredPits)
            {
                Console.WriteLine(opponentData.DriverRawName + " has entered the pitlane with sane flags");
            }
            opponentData.InPits = isInPits;
            bool hasNewLapData = opponentData.HasNewLapData(completedLapTime, hasCrossedSFLine, completedLaps, isRace, sessionRunningTime, previousOpponentDataWaitingForNewLapData,
                previousOpponentNewLapDataTimerExpiry, previousOpponentLastLapTime, previousOpponentLastLapValid, previousCompleatedLapsWhenHasNewLapDataWasLastTrue, previousOpponentGameTimeWhenLastCrossedStartFinishLine);

            if (hasNewLapData)
            {
                if (opponentData.OpponentLapData.Count > 0)
                {
                    opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, completedLapTime, opponentData.LastLapValid && validSpeed,
                        false, trackTempreture, airTemperature, sessionLengthIsTime, sessionTimeRemaining, 3);
                    //Console.WriteLine(opponentData.DriverRawName + " Compleated laps: " + completedLaps + " time: " + TimeSpan.FromSeconds(completedLapTime).ToString(@"mm\:ss\.fff") + " lap valid: " + (opponentData.LastLapValid && validSpeed));
                }
                opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, opponentData.GameTimeWhenLastCrossedStartFinishLine, false, trackTempreture, airTemperature);
                opponentData.IsNewLap = true;
            }
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddCumulativeSectorData(opponentData.CurrentSectorNumber, racePosition, -1, sessionRunningTime, currentLapValid && validSpeed, false, trackTempreture, airTemperature);
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
            {"Lone Practice", SessionType.Practice},
            {"Open Qualify", SessionType.Qualify},
            {"Lone Qualify", SessionType.Qualify},
            {"Race", SessionType.Race}
        };

        public override SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            String sessionString = (String)memoryMappedFileStruct;
            SessionType st = SessionType.Unavailable;
            if (sessionTypeMap.TryGetValue(sessionString, out st))
            {
                return st;
            }
            return SessionType.Unavailable;
        }

        string prevSessionFlags = "";
        string prevSessionStates = "";

        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionStates sessionState,
            SessionType currentSessionType, bool isReplay, float thisSessionRunningTime,
            int previousLapsCompleted, int laps, SessionFlags sessionFlags, bool isInPit, bool fixedTimeSession,
            double sessionTimeRemaining, double sessionRunningTime, int sessionNumberOfLaps)
        {
            /*
            if (!prevSessionFlags.Equals(sessionFlags.ToString()))
            {
                Console.WriteLine(sessionFlags.ToString());
                prevSessionFlags = sessionFlags.ToString();
            }
            if (!prevSessionStates.Equals(sessionState.ToString()))
            {
                Console.WriteLine(sessionState.ToString());
                prevSessionStates = sessionState.ToString();
            }
            */
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
                if (sessionState == SessionStates.GetInCar)
                {
                    return SessionPhase.Unavailable;
                }
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
                        // for fixed number of laps, as soon as we've completed the required number end the session
                        if ((!fixedTimeSession && sessionNumberOfLaps > 0 && laps == sessionNumberOfLaps) || (fixedTimeSession && previousLapsCompleted != laps))
                        {
                            Console.WriteLine("finished - completed " + laps + " laps (was " + previousLapsCompleted + "), session running time = " +
                                thisSessionRunningTime);
                            return SessionPhase.Finished;
                        }
                    }
                }
                else if (sessionState == SessionStates.GetInCar)
                {
                    return SessionPhase.Unavailable;
                }
                else if (sessionState.HasFlag(SessionStates.ParadeLaps) && !sessionFlags.HasFlag(SessionFlags.StartGo))
                {
                    return SessionPhase.Formation;
                }
                else if (sessionState == SessionStates.Warmup && !sessionFlags.HasFlag(SessionFlags.StartGo))
                {
                    // don't allow a transition to Countdown if the game time has increased
                    //if (lastSessionRunningTime < thisSessionRunningTime)
                    //{
                    return SessionPhase.Countdown;
                    //}
                }

                else if ((SessionStates.Racing == sessionState && isReplay) || sessionFlags.HasFlag(SessionFlags.Green) || sessionFlags.HasFlag(SessionFlags.StartGo) || SessionPhase.Unavailable == lastSessionPhase)
                {
                    return SessionPhase.Green;
                }
            }
            return lastSessionPhase;
        }

        private OpponentData createOpponentData(Driver driver, Boolean loadDriverName, CarData.CarClassEnum playerCarClass, float trackLength)
        {
            String driverName = driver.Name.ToLower();
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            }
            OpponentData opponentData = new OpponentData();
            opponentData.IsActive = true;
            opponentData.DriverRawName = driverName;
            opponentData.CostId = driver.CustId;
            opponentData.OverallPosition = driver.Live.Position;
            opponentData.CompletedLaps = driver.Live.LapsCompleted;
            opponentData.DistanceRoundTrack = driver.Live.CorrectedLapDistance * trackLength;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.Now);
            opponentData.CarClass = CarData.getCarClassForIRacingId(driver.Car.CarClassId, driver.Car.CarId);
            opponentData.CurrentSectorNumber = driver.Live.CurrentSector;
            opponentData.CarNr = Parser.ParseInt(driver.CarNumber);
            Console.WriteLine("New driver " + driverName + " is using car class " +
                opponentData.CarClass.getClassIdentifier() + " (car ID " + driver.Car.CarId + ")");

            return opponentData;
        }
    }
}
