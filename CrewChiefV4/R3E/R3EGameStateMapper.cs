﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.RaceRoom.RaceRoomData;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV4.RaceRoom
{
    public class R3EGameStateMapper : GameStateMapper
    {
        public static String playerName = null;
        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // recent r3e changes to tyre wear levels / rates - the data in the block appear to 
        // have changed recently, with about 0.6 representing 'worn out'.
        private float wornOutTyreWearLevel = 0.05f;

        private float scrubbedTyreWearPercent = 2f;
        private float minorTyreWearPercent = 10f;
        private float majorTyreWearPercent = 37f;
        private float wornOutTyreWearPercent = 65f;        

        private float trivialAeroDamageThreshold = 0.99995f;
        private float trivialEngineDamageThreshold = 0.995f;
        private float trivialTransmissionDamageThreshold = 0.99f;

        private float minorTransmissionDamageThreshold = 0.97f;
        private float minorEngineDamageThreshold = 0.99f;
        private float minorAeroDamageThreshold = 0.995f;

        private float severeTransmissionDamageThreshold = 0.4f;
        private float severeEngineDamageThreshold = 0.6f;
        private float severeAeroDamageThreshold = 0.95f;

        private float destroyedTransmissionThreshold = 0.1f;
        private float destroyedEngineThreshold = 0.1f;
        private float destroyedAeroThreshold = 0.8f;

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;

        // Oil temps are typically 1 or 2 units (I'm assuming celcius) higher than water temps. Typical temps while racing tend to be
        // mid - high 50s, with some in-traffic running this creeps up to the mid 60s. To get it into the 
        // 70s you have to really try. Any higher requires you to sit by the side of the road bouncing off the
        // rev limiter. Doing this I've been able to get to 110 without blowing up (I got bored). With temps in the
        // 80s, by the end of a single lap at racing speed they're back into the 60s.
        //
        // I think the cool down effect of the radiator is the underlying issue here - it's far too strong. 
        // The oil temp does lag behind the water temp, which is correct, but I think it should lag 
        // more (i.e. it should take longer for the oil to cool) and the oil should heat up more relative to the water. 
        // 
        // I'd expect to be seeing water temperatures in the 80s for 'normal' running, with this getting well into the 
        // 90s or 100s in traffic. The oil temps should be 100+, maybe hitting 125 or more when the water's also hot.
        // 
        // To work around this I take a 'baseline' temp for oil and water - this is the average temperature between 3
        // and 5 minutes of the session. I then look at differences between this baseline and the current temperature, allowing
        // a configurable 'max above baseline' for each. Assuming the base line temps are sensible (say, 85 for water 105 for oil), 
        // then anthing over 95 for water and 120 for oil is 'bad' - the numbers in the config reflect this

        private Boolean gotBaselineEngineData = false;
        private int baselineEngineDataSamples = 0;
        // record the average temperature between minutes 3 and 5 of driving
        private int baselineEngineDataSamplesStart = (int)(3d * 60d / CrewChief._timeInterval.TotalSeconds);
        private int baselineEngineDataSamplesEnd = (int)(5d * 60d / CrewChief._timeInterval.TotalSeconds);

        private float targetEngineWaterTemp = 88;
        private float targetEngineOilTemp = 105;
        private float baselineEngineDataOilTemp = 88;
        private float baselineEngineDataWaterTemp = 105;

        private SpeechRecogniser speechRecogniser;

        // blue flag zone
        private int blueFlagDetectionDistance = UserSettings.GetUserSettings().getInt("r3e_blue_flag_detection_distance");


        // now we're much stricter with the bollocks opponents data (duplicates, missing entries, stuff randomly being given the wrong
        // slot_id), can we remove this grotty delayed-position hack and all the associated crap it creates? Turns out that no, we can't. 
        // The data are broken and unreliable in multiple ways - the opponent data get jumbled up, and the data *within each opponent slot*
        // get jumbled up too. Can't criticise too strongly though, there's no shortage of shit code right here...
        private Dictionary<String, PendingRacePositionChange> PendingRacePositionChanges = new Dictionary<String, PendingRacePositionChange>();
        private TimeSpan PositionChangeLag = TimeSpan.FromMilliseconds(1000);
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

        public R3EGameStateMapper()
        {
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000, scrubbedTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, scrubbedTyreWearPercent, minorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, minorTyreWearPercent, majorTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, majorTyreWearPercent, wornOutTyreWearPercent));
            tyreWearThresholds.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, wornOutTyreWearPercent, 10000));
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
            CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper wrapper = (CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            RaceRoomData.RaceRoomShared shared = wrapper.data;

            if (shared.Player.GameSimulationTime <= 0 || shared.VehicleInfo.SlotId < 0 ||
                shared.ControlType == (int)RaceRoomConstant.Control.Remote || shared.ControlType == (int)RaceRoomConstant.Control.Replay)
            {
                return previousGameState;
            }

            Boolean isCarRunning = CheckIsCarRunning(shared);

            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                //Console.WriteLine("Raw: " + shared.CarDamage.TireRearLeft + ", calc:" + previousGameState.TyreData.RearLeftPercentWear);
            }

            currentGameState.SessionData.SessionType = mapToSessionType(shared);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Player.GameSimulationTime;
            currentGameState.ControlData.ControlType = mapToControlType(shared.ControlType); // TODO: the rest of the control data
            currentGameState.SessionData.NumCarsAtStartOfSession = shared.NumCars;
            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase, currentGameState.SessionData.SessionType, lastSessionRunningTime,
                currentGameState.SessionData.SessionRunningTime, shared.SessionPhase, currentGameState.ControlData.ControlType,
                previousLapsCompleted, shared.CompletedLaps, isCarRunning);

            List<String> opponentDriverNamesProcessedThisUpdate = new List<String>();

            if ((lastSessionPhase != currentGameState.SessionData.SessionPhase && (lastSessionPhase == SessionPhase.Unavailable || lastSessionPhase == SessionPhase.Finished)) ||
                ((lastSessionPhase == SessionPhase.Checkered || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow) && 
                    currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) ||
                lastSessionRunningTime > currentGameState.SessionData.SessionRunningTime)
            {                
                currentGameState.SessionData.IsNewSession = true;
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("lastSessionPhase = " + lastSessionPhase);
                Console.WriteLine("lastSessionRunningTime = " + lastSessionRunningTime);
                Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("rawSessionPhase = " + shared.SessionPhase);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);

                currentGameState.SessionData.EventIndex = shared.EventIndex;
                currentGameState.SessionData.SessionIteration = shared.SessionIteration;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(null, shared.LayoutLength);
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                currentGameState.PitData.IsRefuellingAllowed = true;

                if (shared.SessionTimeRemaining > 0)
                {
                    currentGameState.SessionData.SessionTotalRunTime = shared.SessionTimeRemaining;
                    currentGameState.SessionData.SessionHasFixedTime = true;
                }

                // reset the engine temp monitor stuff

                gotBaselineEngineData = false;
                baselineEngineDataSamples = 0;
                baselineEngineDataOilTemp = targetEngineOilTemp;
                baselineEngineDataWaterTemp = targetEngineWaterTemp;
                for (int i = 0; i < shared.DriverData.Length; i++)
                {
                    DriverData participantStruct = shared.DriverData[i];
                    String driverName = getNameFromBytes(participantStruct.DriverInfo.Name).ToLower();
                    if (participantStruct.DriverInfo.SlotId == shared.VehicleInfo.SlotId)
                    {
                        currentGameState.SessionData.IsNewSector = previousGameState == null || participantStruct.TrackSector != previousGameState.SessionData.SectorNumber;
                        
                        currentGameState.SessionData.SectorNumber = participantStruct.TrackSector;
                        currentGameState.SessionData.DriverRawName = driverName;
                        if (playerName == null)
                        {
                            NameValidator.validateName(driverName);
                            playerName = driverName;
                        }
                        currentGameState.PitData.InPitlane = participantStruct.InPitlane == 1;
                        currentGameState.PositionAndMotionData.DistanceRoundTrack = participantStruct.LapDistance;
                        currentGameState.carClass = CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum + " (class ID " + participantStruct.DriverInfo.ClassId + ")");
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                    }
                    else
                    {
                        if (driverName.Length > 0 && currentGameState.SessionData.DriverRawName != driverName)
                        {
                            if (opponentDriverNamesProcessedThisUpdate.Contains(driverName))
                            {
                                // would be nice to warn here, but this happens a lot :(
                            }
                            else
                            {
                                opponentDriverNamesProcessedThisUpdate.Add(driverName);
                                currentGameState.OpponentData.Add(driverName, createOpponentData(participantStruct, driverName,
                                    false, currentGameState.carClass.carClassEnum));
                            }
                        }
                    }
                }
            }
            else
            {
                Boolean justGoneGreen = false;
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + lastSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        justGoneGreen = true;
                        // just gone green, so get the session data
                        if (shared.SessionTimeRemaining > 0)
                        {
                            currentGameState.SessionData.SessionTotalRunTime = shared.SessionTimeRemaining;
                            // TODO: confirm that this is enough to catch cases where we have a fixed time + extra lap
                            if (shared.NumberOfLaps > 0)
                            {
                                currentGameState.SessionData.HasExtraLap = true;
                            }
                            currentGameState.SessionData.SessionHasFixedTime = true;
                        }
                        else if (shared.NumberOfLaps > 0)
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = shared.NumberOfLaps;
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        currentGameState.SessionData.SessionStartPosition = shared.Position;
                        currentGameState.SessionData.NumCarsAtStartOfSession = shared.NumCars;
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                        currentGameState.carClass = CarData.getCarClassForRaceRoomId(shared.VehicleInfo.ClassId);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum);
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                        }
                        currentGameState.PitData.PitWindowStart = shared.PitWindowStart;
                        currentGameState.PitData.PitWindowEnd = shared.PitWindowEnd;
                        currentGameState.PitData.HasMandatoryPitStop = currentGameState.PitData.PitWindowStart > 0 && currentGameState.PitData.PitWindowEnd > 0;                         
                        if (currentGameState.PitData.HasMandatoryPitStop)
                        {
                            // TODO: mandatory pitstop for DTM stuff has changed since the removal of the Experiences
                            if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2014 ||
                                currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2015 || currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2016)
                            {
                                // iteration 1 of the DTM 2015 doesn't have a mandatory tyre change, but this means the pit window stuff won't be set, so we're (kind of) OK here...
                                currentGameState.PitData.HasMandatoryTyreChange = true;
                            }
                            if (currentGameState.PitData.HasMandatoryTyreChange && currentGameState.PitData.MandatoryTyreChangeRequiredTyreType == TyreType.Unknown_Race)
                            {
                                if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2014)
                                {
                                    double halfRaceDistance = currentGameState.SessionData.SessionNumberOfLaps / 2d;
                                    if (mapToTyreType(shared.TireType, currentGameState.carClass.carClassEnum) == TyreType.Option)
                                    {
                                        currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = TyreType.Prime;
                                        currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = ((int)Math.Floor(halfRaceDistance)) - 1;
                                    }
                                    else
                                    {
                                        currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = TyreType.Option;
                                        currentGameState.PitData.MinPermittedDistanceOnCurrentTyre = (int)Math.Ceiling(halfRaceDistance);
                                    }
                                }
                                else if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2015 || currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2016)
                                {
                                    currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = TyreType.R3E_NEW_Prime;
                                    // the mandatory change must be completed by the end of the pit window
                                    currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = currentGameState.PitData.PitWindowEnd;
                                }
                            }
                        }
                        Console.WriteLine("Just gone green, session details...");

                        // reset the engine temp monitor stuff
                        gotBaselineEngineData = false;
                        baselineEngineDataSamples = 0;
                        baselineEngineDataOilTemp = targetEngineOilTemp;
                        baselineEngineDataWaterTemp = targetEngineWaterTemp;

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
                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
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
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
                    currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                    currentGameState.carClass = previousGameState.carClass;
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
                }
            }

            currentGameState.ControlData.ThrottlePedal = shared.ThrottlePedal;
            currentGameState.ControlData.ClutchPedal = shared.ClutchPedal;
            currentGameState.ControlData.BrakePedal = shared.BrakePedal;
            currentGameState.TransmissionData.Gear = shared.Gear;

            //------------------------ Session data -----------------------
            currentGameState.SessionData.Flag = FlagEnum.UNKNOWN;
            currentGameState.SessionData.SessionTimeRemaining = shared.SessionTimeRemaining;
            currentGameState.SessionData.CompletedLaps = shared.CompletedLaps;     
            
            currentGameState.SessionData.LapTimeCurrent = shared.LapTimeCurrentSelf;
            currentGameState.SessionData.CurrentLapIsValid = currentGameState.SessionData.LapTimeCurrent != -1;
            currentGameState.SessionData.LapTimePrevious = shared.LapTimePreviousSelf;
            currentGameState.SessionData.PreviousLapWasValid = shared.LapTimePreviousSelf > 0;
            currentGameState.SessionData.NumCars = shared.NumCars;
            
            currentGameState.SessionData.Position = getRacePosition(currentGameState.SessionData.DriverRawName, currentGameState.SessionData.Position, shared.Position, currentGameState.Now);
            // currentGameState.SessionData.Position = shared.Position;
            currentGameState.SessionData.UnFilteredPosition = shared.Position;
            currentGameState.SessionData.TimeDeltaBehind = shared.TimeDeltaBehind;
            currentGameState.SessionData.TimeDeltaFront = shared.TimeDeltaFront;

            currentGameState.SessionData.SessionFastestLapTimeFromGame = shared.LapTimeBestLeader;
            currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = shared.LapTimeBestLeaderClass;
            if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                currentGameState.SessionData.OverallSessionBestLapTime > shared.LapTimeBestLeader)
            {
                currentGameState.SessionData.OverallSessionBestLapTime = shared.LapTimeBestLeader;
            }
            if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                currentGameState.SessionData.PlayerClassSessionBestLapTime > shared.LapTimeBestLeaderClass)
            {
                currentGameState.SessionData.PlayerClassSessionBestLapTime = shared.LapTimeBestLeaderClass;
            }
            // TODO: calculate the actual session best sector times from the bollocks in the block (cumulative deltas between the last player sector time and the session best)

            currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                (shared.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                ((lastSessionPhase == SessionPhase.Countdown || lastSessionPhase == SessionPhase.Formation || lastSessionPhase == SessionPhase.Garage)
                && (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)));
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.SessionData.PositionAtStartOfCurrentLap = currentGameState.SessionData.Position;
                currentGameState.SessionData.formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(shared.LapTimePreviousSelf).ToString(@"mm\:ss\.fff"));
                // quick n dirty hack here - if the current car class is unknown, try and get it again
                if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                {
                    currentGameState.carClass = CarData.getCarClassForRaceRoomId(shared.VehicleInfo.ClassId);
                    brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);                    
                }
            }
            if (previousGameState != null && !currentGameState.SessionData.IsNewSession)
            {
                currentGameState.OpponentData = previousGameState.OpponentData;
                currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
            }

            foreach (DriverData participantStruct in shared.DriverData)
            {
                if (participantStruct.DriverInfo.SlotId == shared.VehicleInfo.SlotId)
                {
                    if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                    {
                        CarData.CarClass newClass = CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId);
                        if (newClass.carClassEnum != currentGameState.carClass.carClassEnum)
                        {
                            currentGameState.carClass = newClass;
                            Console.WriteLine("Player is using car class " + currentGameState.carClass.carClassEnum + " (class ID " + participantStruct.DriverInfo.ClassId + ")");
                            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        }
                    }
                    currentGameState.SessionData.IsNewSector = participantStruct.TrackSector != 0 && currentGameState.SessionData.SectorNumber != participantStruct.TrackSector;
                    
                    if (currentGameState.SessionData.CurrentLapIsValid && participantStruct.CurrentLapValid != 1) {
                        currentGameState.SessionData.CurrentLapIsValid = false;
                    }
                    if (currentGameState.SessionData.IsNewSector)
                    {
                        if (participantStruct.TrackSector == 1)
                        {
                            if (currentGameState.SessionData.SessionTimesAtEndOfSectors[3] != -1)
                            {
                                currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.SessionTimesAtEndOfSectors[3];
                            }
                            currentGameState.SessionData.SessionTimesAtEndOfSectors[3] = currentGameState.SessionData.SessionRunningTime;
                            if (participantStruct.SectorTimePreviousSelf.Sector3 > 0 && participantStruct.SectorTimeCurrentSelf.Sector2 > 0 &&
                                previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
                            {
                                float sectorTime = participantStruct.SectorTimePreviousSelf.Sector3 - participantStruct.SectorTimeCurrentSelf.Sector2;
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
                        else if (participantStruct.TrackSector == 2)
                        {
                            currentGameState.SessionData.SessionTimesAtEndOfSectors[1] = currentGameState.SessionData.SessionRunningTime;
                            if (participantStruct.SectorTimeCurrentSelf.Sector1 > 0 && currentGameState.SessionData.CurrentLapIsValid)
                            {
                                currentGameState.SessionData.LastSector1Time = participantStruct.SectorTimeCurrentSelf.Sector1;
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
                        else if (participantStruct.TrackSector == 3)
                        {
                            currentGameState.SessionData.SessionTimesAtEndOfSectors[2] = currentGameState.SessionData.SessionRunningTime;
                            if (participantStruct.SectorTimeCurrentSelf.Sector2 > 0 && participantStruct.SectorTimeCurrentSelf.Sector1 > 0 &&
                                 currentGameState.SessionData.CurrentLapIsValid)
                            {
                                float sectorTime = participantStruct.SectorTimeCurrentSelf.Sector2 - participantStruct.SectorTimeCurrentSelf.Sector1;
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
                    currentGameState.SessionData.SectorNumber = participantStruct.TrackSector;
                    currentGameState.PitData.InPitlane = participantStruct.InPitlane == 1;
                    currentGameState.PositionAndMotionData.DistanceRoundTrack = participantStruct.LapDistance;
                    if (currentGameState.PitData.InPitlane)
                    {
                        if (participantStruct.TrackSector == 3)
                        {
                            currentGameState.PitData.OnInLap = true;
                            currentGameState.PitData.OnOutLap = false;
                        }
                        else if (participantStruct.TrackSector == 1)
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
                    break;
                }
            }

            foreach (DriverData participantStruct in shared.DriverData)
            {
                if (participantStruct.DriverInfo.SlotId != -1 && participantStruct.DriverInfo.SlotId != shared.VehicleInfo.SlotId)
                {
                    String driverName = getNameFromBytes(participantStruct.DriverInfo.Name).ToLower();
                    if (driverName.Length == 0 || driverName == currentGameState.SessionData.DriverRawName || opponentDriverNamesProcessedThisUpdate.Contains(driverName) || participantStruct.Place < 1) 
                    {
                        continue;
                    }
                    if (currentGameState.OpponentData.ContainsKey(driverName))
                    {
                        opponentDriverNamesProcessedThisUpdate.Add(driverName);
                        if (previousGameState != null)
                        {
                            OpponentData previousOpponentData = null;
                            Boolean newOpponentLap = false;
                            int previousOpponentSectorNumber = 1;
                            int previousOpponentCompletedLaps = 0;
                            int previousOpponentPosition = 0;
                            Boolean previousOpponentIsEnteringPits = false;
                            Boolean previousOpponentIsExitingPits = false;
                            float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                            float previousOpponentSpeed = 0;
                            if (previousGameState.OpponentData.ContainsKey(driverName))
                            {
                                previousOpponentData = previousGameState.OpponentData[driverName];
                                previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                previousOpponentPosition = previousOpponentData.Position;
                                previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                previousOpponentSpeed = previousOpponentData.Speed;
                                newOpponentLap = previousOpponentData.CurrentSectorNumber == 3 && participantStruct.TrackSector == 1;
                            }

                            OpponentData currentOpponentData = currentGameState.OpponentData[driverName];
                            
                            float sectorTime = -1;
                            if (participantStruct.TrackSector == 1)
                            {
                                sectorTime = participantStruct.SectorTimeCurrentSelf.Sector3;
                            }
                            else if (participantStruct.TrackSector == 2)
                            {
                                sectorTime = participantStruct.SectorTimeCurrentSelf.Sector1;
                            }
                            else if (participantStruct.TrackSector == 3)
                            {
                                sectorTime = participantStruct.SectorTimeCurrentSelf.Sector2;
                            }

                            int currentOpponentRacePosition = getRacePosition(driverName, previousOpponentPosition, participantStruct.Place, currentGameState.Now);
                            //int currentOpponentRacePosition = participantStruct.place;
                            int currentOpponentLapsCompleted = participantStruct.CompletedLaps;
                            int currentOpponentSector = participantStruct.TrackSector;
                            if (currentOpponentSector == 0)
                            {
                                currentOpponentSector = previousOpponentSectorNumber;
                            }
                            float currentOpponentLapDistance = participantStruct.LapDistance;

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
                            if (currentOpponentRacePosition == 1 && previousOpponentPosition > 1)
                            {
                                currentGameState.SessionData.HasLeadChanged = true;
                            }
                            Boolean isEnteringPits = participantStruct.InPitlane == 1 && currentOpponentSector == 3;
                            Boolean isLeavingPits = participantStruct.InPitlane == 1 && currentOpponentSector == 1;

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
                                    participantStruct.Place, currentOpponentLapsCompleted,
                                    currentOpponentSector, sectorTime, participantStruct.SectorTimePreviousSelf.Sector3,
                                    isEnteringPits || isLeavingPits, participantStruct.CurrentLapValid == 1,
                                    currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                    new float[] { participantStruct.Position.X, participantStruct.Position.Z }, previousOpponentWorldPosition,
                                    participantStruct.LapDistance, participantStruct.TireType, participantStruct.DriverInfo.ClassId,
                                    currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 
                                    currentGameState.carClass.carClassEnum);
                            if (newOpponentLap)
                            {
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
                            // TODO: fix this properly - hack to work around issue with lagging position updates - 
                            // only allow a blue flag if the 'settled' position and the latest position agree

                            Boolean isInSector1OnOutlap = currentOpponentData.CurrentSectorNumber == 1 &&
                                (currentOpponentData.getCurrentLapData() != null && currentOpponentData.getCurrentLapData().OutLap);
                            if (currentGameState.SessionData.SessionType == SessionType.Race && currentOpponentData.Position == participantStruct.Place &&
                                !isEnteringPits && !isLeavingPits && currentGameState.PositionAndMotionData.DistanceRoundTrack != 0 &&
                                currentOpponentData.Position + 1 < shared.Position && !isInSector1OnOutlap && 
                                isBehindWithinDistance(shared.LayoutLength, 8, blueFlagDetectionDistance, currentGameState.PositionAndMotionData.DistanceRoundTrack, 
                                participantStruct.LapDistance))
                            {
                                currentGameState.SessionData.Flag = FlagEnum.BLUE;
                            }
                        }
                    }
                    else
                    {
                        opponentDriverNamesProcessedThisUpdate.Add(driverName);
                        currentGameState.OpponentData.Add(driverName, createOpponentData(participantStruct, driverName, true, currentGameState.carClass.carClassEnum));
                    }
                }
            }

            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.PreviousLapWasValid &&
                currentGameState.SessionData.LapTimePrevious > 0)
            {
                if ((currentGameState.SessionData.PlayerLapTimeSessionBest == -1 ||
                     currentGameState.SessionData.LapTimePrevious < currentGameState.SessionData.PlayerLapTimeSessionBest))
                {
                    currentGameState.SessionData.PlayerLapTimeSessionBest = currentGameState.SessionData.LapTimePrevious;
                    if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                        currentGameState.SessionData.OverallSessionBestLapTime > currentGameState.SessionData.PlayerLapTimeSessionBest)
                    {
                        currentGameState.SessionData.OverallSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest;
                    }
                    if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                        currentGameState.SessionData.PlayerClassSessionBestLapTime > currentGameState.SessionData.PlayerLapTimeSessionBest)
                    {
                        currentGameState.SessionData.PlayerClassSessionBestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest;
                    }
                }
            }

            // TODO: lap time previous for invalid laps (is this still needed?)

            if (shared.SessionType == (int)RaceRoomConstant.Session.Race && shared.SessionPhase == (int)RaceRoomConstant.SessionPhase.Checkered &&
                previousGameState != null && (previousGameState.SessionData.SessionPhase == SessionPhase.Green || previousGameState.SessionData.SessionPhase == SessionPhase.Green))
            {
                Console.WriteLine("Leader has finished race, player has done "+ shared.CompletedLaps + " laps, session time = " + shared.Player.GameSimulationTime);
                currentGameState.SessionData.LeaderHasFinishedRace = true;
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
                        
            //------------------------ Car damage data -----------------------
            currentGameState.CarDamageData.DamageEnabled = shared.CarDamage.Aerodynamics != -1 &&
                shared.CarDamage.Transmission != -1 && shared.CarDamage.Engine != -1;
            if (currentGameState.CarDamageData.DamageEnabled)
            {
                if (shared.CarDamage.Aerodynamics < destroyedAeroThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.DESTROYED;
                }
                else if (shared.CarDamage.Aerodynamics < severeAeroDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MAJOR;
                }
                else if (shared.CarDamage.Aerodynamics < minorAeroDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.MINOR;
                }
                else if (shared.CarDamage.Aerodynamics < trivialAeroDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    currentGameState.CarDamageData.OverallAeroDamage = DamageLevel.NONE;
                }
                if (shared.CarDamage.Engine < destroyedEngineThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.DESTROYED;
                }
                else if (shared.CarDamage.Engine < severeEngineDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.MAJOR;
                }
                else if (shared.CarDamage.Engine < minorEngineDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.MINOR;
                }
                else if (shared.CarDamage.Engine < trivialEngineDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    currentGameState.CarDamageData.OverallEngineDamage = DamageLevel.NONE;
                }
                if (shared.CarDamage.Transmission < destroyedTransmissionThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.DESTROYED;
                }
                else if (shared.CarDamage.Transmission < severeTransmissionDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.MAJOR;
                }
                else if (shared.CarDamage.Transmission < minorTransmissionDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.MINOR;
                }
                else if (shared.CarDamage.Transmission < trivialTransmissionDamageThreshold)
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.TRIVIAL;
                }
                else
                {
                    currentGameState.CarDamageData.OverallTransmissionDamage = DamageLevel.NONE;
                }
            }
            
            //------------------------ Engine data -----------------------            
            currentGameState.EngineData.EngineOilPressure = shared.EngineOilPressure;
            currentGameState.EngineData.EngineRpm = Utilities.RpsToRpm(shared.EngineRps);
            currentGameState.EngineData.MaxEngineRpm = Utilities.RpsToRpm(shared.MaxEngineRps);
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            
            // all this 'baseline' engine temp logic was only ever a hack and is now disabled
            /*
            if (!gotBaselineEngineData)
            {
                currentGameState.EngineData.EngineOilTemp = shared.EngineOilTemp;
                currentGameState.EngineData.EngineWaterTemp = shared.EngineWaterTemp;
                if (isCarRunning)
                {
                    baselineEngineDataSamples++;
                    if (baselineEngineDataSamples > baselineEngineDataSamplesStart)
                    {
                        if (baselineEngineDataSamples < baselineEngineDataSamplesEnd)
                        {
                            baselineEngineDataWaterTemp += shared.EngineWaterTemp;
                            baselineEngineDataOilTemp += shared.EngineOilTemp;
                        }
                        else
                        {
                            gotBaselineEngineData = true;
                            baselineEngineDataOilTemp = baselineEngineDataOilTemp / (baselineEngineDataSamples - baselineEngineDataSamplesStart);
                            baselineEngineDataWaterTemp = baselineEngineDataWaterTemp / (baselineEngineDataSamples - baselineEngineDataSamplesStart);
                            Console.WriteLine("Got baseline engine temps, water = " + baselineEngineDataWaterTemp + ", oil = " + baselineEngineDataOilTemp);
                        }
                    }
                }
            }
            else
            {
                currentGameState.EngineData.EngineOilTemp = shared.EngineOilTemp * targetEngineOilTemp / baselineEngineDataOilTemp;
                currentGameState.EngineData.EngineWaterTemp = shared.EngineWaterTemp * targetEngineWaterTemp / baselineEngineDataWaterTemp;
            }
            */
            currentGameState.EngineData.EngineOilTemp = shared.EngineOilTemp;
            currentGameState.EngineData.EngineWaterTemp = shared.EngineWaterTemp;

            //------------------------ Fuel data -----------------------
            currentGameState.FuelData.FuelUseActive = shared.FuelUseActive == 1;
            currentGameState.FuelData.FuelPressure = shared.FuelPressure;
            currentGameState.FuelData.FuelCapacity = shared.FuelCapacity;
            currentGameState.FuelData.FuelLeft = shared.FuelLeft;


            //------------------------ Penalties data -----------------------
            currentGameState.PenaltiesData.CutTrackWarnings = shared.CutTrackWarnings;
            currentGameState.PenaltiesData.HasDriveThrough = shared.Penalties.DriveThrough > 0;
            currentGameState.PenaltiesData.HasSlowDown = shared.Penalties.SlowDown > 0;
            currentGameState.PenaltiesData.HasPitStop = shared.Penalties.PitStop > 0;
            currentGameState.PenaltiesData.HasStopAndGo = shared.Penalties.StopAndGo > 0;
            currentGameState.PenaltiesData.HasTimeDeduction = shared.Penalties.TimeDeduction > 0; ;
            currentGameState.PenaltiesData.NumPenalties = shared.NumPenalties;


            //------------------------ Pit stop data -----------------------            
            currentGameState.PitData.PitWindow = mapToPitWindow(shared.PitWindowStatus);
            currentGameState.PitData.IsMakingMandatoryPitStop = (currentGameState.PitData.PitWindow == PitWindow.Open || currentGameState.PitData.PitWindow == PitWindow.StopInProgress) &&
               (currentGameState.PitData.OnInLap || currentGameState.PitData.OnOutLap);

            //------------------------ Car position / motion data -----------------------
            currentGameState.PositionAndMotionData.CarSpeed = shared.CarSpeed;


            //------------------------ Transmission data -----------------------
            currentGameState.TransmissionData.Gear = shared.Gear;


            //------------------------ Tyre data -----------------------
            // no way to have unmatched tyre types in R3E
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TireWearActive = shared.TireWearActive == 1;
            TyreType tyreType = mapToTyreType(shared.TireType, currentGameState.carClass.carClassEnum);            
            currentGameState.TyreData.FrontLeft_CenterTemp = shared.TireTemp.FrontLeft_Center;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.TireTemp.FrontLeft_Left;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.TireTemp.FrontLeft_Right;
            float frontLeftTemp = (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3;
            currentGameState.TyreData.FrontLeftTyreType = tyreType;
            currentGameState.TyreData.FrontLeftPressure = shared.TirePressure.FrontLeft;
            currentGameState.TyreData.FrontLeftPercentWear = getTyreWearPercentage(shared.TireWear.FrontLeft);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }
            else if (previousGameState == null || frontLeftTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = frontLeftTemp;
            }

            currentGameState.TyreData.FrontRight_CenterTemp = shared.TireTemp.FrontRight_Center;
            currentGameState.TyreData.FrontRight_LeftTemp = shared.TireTemp.FrontRight_Left;
            currentGameState.TyreData.FrontRight_RightTemp = shared.TireTemp.FrontRight_Right;
            float frontRightTemp = (currentGameState.TyreData.FrontRight_CenterTemp + currentGameState.TyreData.FrontRight_LeftTemp + currentGameState.TyreData.FrontRight_RightTemp) / 3;
            currentGameState.TyreData.FrontRightTyreType = tyreType;
            currentGameState.TyreData.FrontRightPressure = shared.TirePressure.FrontRight;
            currentGameState.TyreData.FrontRightPercentWear = getTyreWearPercentage(shared.TireWear.FrontRight);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }
            else if (previousGameState == null || frontRightTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = frontRightTemp;
            }

            currentGameState.TyreData.RearLeft_CenterTemp = shared.TireTemp.RearLeft_Center;
            currentGameState.TyreData.RearLeft_LeftTemp = shared.TireTemp.RearLeft_Left;
            currentGameState.TyreData.RearLeft_RightTemp = shared.TireTemp.RearLeft_Right;
            float rearLeftTemp = (currentGameState.TyreData.RearLeft_CenterTemp + currentGameState.TyreData.RearLeft_LeftTemp + currentGameState.TyreData.RearLeft_RightTemp) / 3;
            currentGameState.TyreData.RearLeftTyreType = tyreType;
            currentGameState.TyreData.RearLeftPressure = shared.TirePressure.RearLeft;
            currentGameState.TyreData.RearLeftPercentWear = getTyreWearPercentage(shared.TireWear.RearLeft);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }
            else if (previousGameState == null || rearLeftTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = rearLeftTemp;
            }

            currentGameState.TyreData.RearRight_CenterTemp = shared.TireTemp.RearRight_Center;
            currentGameState.TyreData.RearRight_LeftTemp = shared.TireTemp.RearRight_Left;
            currentGameState.TyreData.RearRight_RightTemp = shared.TireTemp.RearRight_Right;
            float rearRightTemp = (currentGameState.TyreData.RearRight_CenterTemp + currentGameState.TyreData.RearRight_LeftTemp + currentGameState.TyreData.RearRight_RightTemp) / 3;
            currentGameState.TyreData.RearRightTyreType = tyreType;
            currentGameState.TyreData.RearRightPressure = shared.TirePressure.RearRight;
            currentGameState.TyreData.RearRightPercentWear = getTyreWearPercentage(shared.TireWear.RearRight);
            if (currentGameState.SessionData.IsNewLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (previousGameState == null || rearRightTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear,
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(CarData.tyreTempThresholds[tyreType],
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap, currentGameState.TyreData.PeakFrontRightTemperatureForLap,
                currentGameState.TyreData.PeakRearLeftTemperatureForLap, currentGameState.TyreData.PeakRearRightTemperatureForLap);

            if (brakeTempThresholdsForPlayersCar != null)
            {
                currentGameState.TyreData.BrakeTempStatus = CornerData.getCornerData(brakeTempThresholdsForPlayersCar, shared.BrakeTemp.FrontLeft,
                    shared.BrakeTemp.FrontRight, shared.BrakeTemp.RearLeft, shared.BrakeTemp.RearRight);
            }

            currentGameState.TyreData.LeftFrontBrakeTemp = shared.BrakeTemp.FrontLeft;
            currentGameState.TyreData.RightFrontBrakeTemp = shared.BrakeTemp.FrontRight;
            currentGameState.TyreData.LeftRearBrakeTemp = shared.BrakeTemp.RearLeft;
            currentGameState.TyreData.RightRearBrakeTemp = shared.BrakeTemp.RearRight;

            // some simple locking / spinning checks
            if (shared.CarSpeed > 7)
            {
                float minRotatingSpeed = 2 * (float)Math.PI * shared.CarSpeed / currentGameState.carClass.maxTyreCircumference;
                // I think the tyreRPS is actually radians per second...
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.TireRps.FrontLeft) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.TireRps.FrontRight) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.TireRps.RearLeft) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.TireRps.RearRight) < minRotatingSpeed;

                float maxRotatingSpeed = 2 * (float)Math.PI * shared.CarSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.TireRps.FrontLeft) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.TireRps.FrontRight) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.TireRps.RearLeft) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.TireRps.RearRight) > maxRotatingSpeed;
            }
            currentGameState.OvertakingAids = getOvertakingAids(shared, currentGameState.carClass.carClassEnum, currentGameState.SessionData.CompletedLaps,
                currentGameState.SessionData.SessionNumberOfLaps, currentGameState.SessionData.SessionTimeRemaining,
                currentGameState.SessionData.SessionType);
            return currentGameState;
        }

        private int getRacePosition(String driverName, int oldPosition, int newPosition, DateTime now)
        {
            if (oldPosition < 1)
            {
                return newPosition;
            }
            if (newPosition < 1)
            {
                Console.WriteLine("Can't update position to " + newPosition);
                return oldPosition;
            }
            if (oldPosition == newPosition)
            {
                // clear any pending position change
                if (PendingRacePositionChanges.ContainsKey(driverName))
                {
                    PendingRacePositionChanges.Remove(driverName);
                }
                return oldPosition;
            }
            else if (PendingRacePositionChanges.ContainsKey(driverName))
            {
                PendingRacePositionChange pendingRacePositionChange = PendingRacePositionChanges[driverName];
                if (newPosition == pendingRacePositionChange.newPosition)
                {
                    // R3E is still reporting this driver is in the same race position, see if it's been long enough...
                    if (now > pendingRacePositionChange.positionChangeTime + PositionChangeLag)
                    {
                        int positionToReturn = newPosition;
                        PendingRacePositionChanges.Remove(driverName);
                        return positionToReturn;
                    }
                    else
                    {
                        return oldPosition;
                    }
                }
                else
                {
                    // the new position is not consistent with the pending position change, bit of an edge case here
                    pendingRacePositionChange.newPosition = newPosition;
                    pendingRacePositionChange.positionChangeTime = now;
                    return oldPosition;
                }
            }
            else
            {
                PendingRacePositionChanges.Add(driverName, new PendingRacePositionChange(newPosition, now));
                return oldPosition;
            }
        }
        
        private TyreType mapToTyreType(int r3eTyreType, CarData.CarClassEnum carClass)
        {
            // only DTM 2014 (old physics) use Option tyres
            if ((int)RaceRoomConstant.TireType.DTM_Option == r3eTyreType)
            {
                return TyreType.Option;
            }
            else if ((int)RaceRoomConstant.TireType.Prime == r3eTyreType)
            {
                if (carClass == CarData.CarClassEnum.DTM_2015)
                {
                    return TyreType.R3E_NEW_Prime;
                }
                else
                {
                    return TyreType.Prime;
                }
            }
            else if (CarData.r3eNewTyreModelClasses.Contains(carClass))
            {
                return TyreType.R3E_NEW;
            }
            else
            {
                return TyreType.Unknown_Race;
            }
        }

        private PitWindow mapToPitWindow(int r3ePitWindow)
        {
            if ((int)RaceRoomConstant.PitWindow.Closed == r3ePitWindow)
            {
                return PitWindow.Closed;
            }
            if ((int)RaceRoomConstant.PitWindow.Completed == r3ePitWindow)
            {
                return PitWindow.Completed;
            }
            else if ((int)RaceRoomConstant.PitWindow.Disabled == r3ePitWindow)
            {
                return PitWindow.Disabled;
            }
            else if ((int)RaceRoomConstant.PitWindow.Open == r3ePitWindow)
            {
                return PitWindow.Open;
            }
            else if ((int)RaceRoomConstant.PitWindow.StopInProgress == r3ePitWindow)
            {
                return PitWindow.StopInProgress;
            }
            else
            {
                return PitWindow.Unavailable;
            }
        }

        /**
         * Gets the current session phase. If the transition is valid this is returned, otherwise the
         * previous phase is returned
         */
        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionType currentSessionType, float lastSessionRunningTime, float thisSessionRunningTime, 
            int r3eSessionPhase, ControlType controlType, int previousLapsCompleted, int currentLapsCompleted, Boolean isCarRunning)
        {

            /* prac and qual sessions go chequered after the allotted time. They never go 'finished'. If we complete a lap
             * during this period we can detect the session end and trigger the finish message. Otherwise we just can't detect
             * this period end - hence the 'isCarRunning' hack...
            */
            if ((int)RaceRoomConstant.SessionPhase.Checkered == r3eSessionPhase)
            {
                if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow)
                {
                    // only allow a transition to checkered if the last state was green
                    Console.WriteLine("checkered - completed " + currentLapsCompleted + " laps, session running time = " + thisSessionRunningTime);
                    return SessionPhase.Checkered;
                }
                else if (SessionPhase.Checkered == lastSessionPhase)
                {
                    if (previousLapsCompleted != currentLapsCompleted || controlType == ControlType.AI ||
                        ((currentSessionType == SessionType.Qualify || currentSessionType == SessionType.Practice) && !isCarRunning))
                    {
                        Console.WriteLine("finished - completed " + currentLapsCompleted + " laps (was " + previousLapsCompleted + "), session running time = " +
                            thisSessionRunningTime + " control type = " + controlType);
                        return SessionPhase.Finished;
                    }
                }
            }
            else if ((int)RaceRoomConstant.SessionPhase.Countdown == r3eSessionPhase)
            {
                // don't allow a transition to Countdown if the game time has increased
                if (lastSessionRunningTime < thisSessionRunningTime)
                {
                    return SessionPhase.Countdown;
                }
            }
            else if ((int)RaceRoomConstant.SessionPhase.Formation == r3eSessionPhase)
            {
                return SessionPhase.Formation;
            }
            else if ((int)RaceRoomConstant.SessionPhase.Garage == r3eSessionPhase)
            {
                return SessionPhase.Garage;
            }
            else if ((int)RaceRoomConstant.SessionPhase.Green == r3eSessionPhase)
            {
                if (controlType == ControlType.AI && thisSessionRunningTime < 30)
                {
                    return SessionPhase.Formation;
                }
                else
                {
                    return SessionPhase.Green;
                }
            }
            else if ((int)RaceRoomConstant.SessionPhase.Gridwalk == r3eSessionPhase)
            {
                return SessionPhase.Gridwalk;
            }
            else if ((int)RaceRoomConstant.SessionPhase.Terminated == r3eSessionPhase)
            {
                return SessionPhase.Finished;
            }
            return lastSessionPhase;
        }

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            RaceRoomData.RaceRoomShared shared = (RaceRoomData.RaceRoomShared)memoryMappedFileStruct;
            int r3eSessionType = shared.SessionType;
            int numCars = shared.NumCars;
            if ((int)RaceRoomConstant.Session.Practice == r3eSessionType)
            {
                return SessionType.Practice;
            }
            else if ((int)RaceRoomConstant.Session.Qualify == r3eSessionType && (numCars == 1 || numCars == 2))
            {
                // hotlap sessions are not explicity declared in R3E - have to check if it's qual and there are 1 or 2 cars
                return SessionType.HotLap;
            }
            else if ((int)RaceRoomConstant.Session.Qualify == r3eSessionType)
            {
                return SessionType.Qualify;
            }
            else if ((int)RaceRoomConstant.Session.Race == r3eSessionType)
            {
                return SessionType.Race;
            }
            else
            {
                return SessionType.Unavailable;
            }
        }

        private ControlType mapToControlType(int r3eControlType)
        {
            if ((int)RaceRoomConstant.Control.AI == r3eControlType)
            {
                return ControlType.AI;
            }
            else if ((int)RaceRoomConstant.Control.Player == r3eControlType)
            {
                return ControlType.Player;
            }
            else if ((int)RaceRoomConstant.Control.Remote == r3eControlType)
            {
                return ControlType.Remote;
            }
            else if ((int)RaceRoomConstant.Control.Replay == r3eControlType)
            {
                return ControlType.Replay;
            }
            else
            {
                return ControlType.Unavailable;
            }
        }

        private float getTyreWearPercentage(float wearLevel)
        {
            if (wearLevel == -1)
            {
                return -1;
            }
            return Math.Min(100, ((1 - wearLevel) / wornOutTyreWearLevel) * 100);
        }

        private Boolean CheckIsCarRunning(RaceRoomData.RaceRoomShared shared)
        {
            return shared.Gear > 0 || shared.CarSpeed > 0.001;
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

        private OvertakingAids getOvertakingAids(RaceRoomShared shared, CarData.CarClassEnum carClassEnum, 
            int lapsCompleted, int lapsInSession, float sessionTimeRemaining, SessionType sessionType)
        {
            OvertakingAids overtakingAids = new OvertakingAids();
            overtakingAids.DrsAvailable = shared.Drs.Available == 1;
            overtakingAids.DrsEngaged = shared.Drs.Engaged == 1;
            if (carClassEnum == CarData.CarClassEnum.DTM_2014)
            {
                // is the race-end check correct here? I assume DRS is disabled for the last 2 laps, but I really am just guessing...
                overtakingAids.DrsEnabled = sessionType == SessionType.Race && lapsCompleted > 2/* && (lapsInSession < 1 || lapsInSession > lapsCompleted + 2)*/;
                overtakingAids.DrsRange = 2;
            }
            else if (carClassEnum == CarData.CarClassEnum.DTM_2015 || carClassEnum == CarData.CarClassEnum.DTM_2016)
            {
                // is the race-end check correct here? I assume DRS is disabled for the last 3 minutes, but I really am just guessing...
                overtakingAids.DrsEnabled = sessionType == SessionType.Race && lapsCompleted > 3/* && (sessionTimeRemaining < 0 || sessionTimeRemaining > 180)*/;
                overtakingAids.DrsRange = 1;
            }
            overtakingAids.PushToPassActivationsRemaining = shared.PushToPass.AmountLeft;
            overtakingAids.PushToPassAvailable = shared.PushToPass.Available == 1;
            overtakingAids.PushToPassEngaged = shared.PushToPass.Engaged == 1;
            overtakingAids.PushToPassEngagedTimeLeft = shared.PushToPass.EngagedTimeLeft;
            overtakingAids.PushToPassWaitTimeLeft = shared.PushToPass.WaitTimeLeft;
            return overtakingAids;
        }

        private void upateOpponentData(OpponentData opponentData, int racePosition, int unfilteredRacePosition, int completedLaps, int sector, float sectorTime, 
            float completedLapTime, Boolean isInPits, Boolean lapIsValid, float sessionRunningTime, float secondsSinceLastUpdate, float[] currentWorldPosition,
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
                opponentData.CarClass = CarData.getCarClassForRaceRoomId(carClassId);
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, completedLapTime,
                            lapIsValid && validSpeed, false, 20, 20, sessionLengthIsTime, sessionTimeRemaining);
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, false, 20, 20);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddCumulativeSectorData(racePosition, sectorTime, sessionRunningTime, lapIsValid && validSpeed, false, 20, 20);
                    if (sector == 2)
                    {
                        // crappy but necessary assumption - assume single class here. It only really matters for DTM races, which will be a single class.
                        // The alternative is to check the opponent car class each tick (too expensive)
                        opponentData.CurrentTyres = mapToTyreType(tire_type, playerCarClass);
                    }
                }
                opponentData.CurrentSectorNumber = sector;
            }
            opponentData.CompletedLaps = completedLaps;
            if (sector == 3 && isInPits)
            {
                opponentData.setInLap();
            }
        }

        private OpponentData createOpponentData(DriverData participantStruct, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass)
        {
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            } 
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = driverName;
            opponentData.Position = participantStruct.Place;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = participantStruct.CompletedLaps;
            opponentData.CurrentSectorNumber = participantStruct.TrackSector;
            opponentData.WorldPosition = new float[] { participantStruct.Position.X, participantStruct.Position.Z };
            opponentData.DistanceRoundTrack = participantStruct.LapDistance;
            opponentData.CarClass = CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId);
            opponentData.CurrentTyres = mapToTyreType(participantStruct.TireType, playerCarClass);
            Console.WriteLine("New driver " + driverName + " is using car class " +
                opponentData.CarClass.carClassEnum + " (class ID " + participantStruct.DriverInfo.ClassId + ")");

            return opponentData;
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
