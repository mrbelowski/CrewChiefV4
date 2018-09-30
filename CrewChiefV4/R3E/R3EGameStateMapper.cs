using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Diagnostics;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */
namespace CrewChiefV4.RaceRoom
{
    public class R3EGameStateMapper : GameStateMapper
    {
        // this is set when we first join a practice or qual session (or when the session first starts). All participants
        // car classes are recalculated every tick for 5 seconds
        private DateTime recheckCarClassesUntil = DateTime.MinValue;
        private TimeSpan recheckCarClassesForSeconds = TimeSpan.FromSeconds(5);

        private TimeSpan minimumSessionParticipationTime = TimeSpan.FromSeconds(6);

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> tyreWearThresholds = new List<CornerData.EnumWithThresholds>();
        private List<CornerData.EnumWithThresholds> brakeDamageThresholds = new List<CornerData.EnumWithThresholds>();

        // recent r3e changes to tyre wear levels / rates - the data in the block appear to 
        // have changed recently, with about 0.94 representing 'worn out' - these start at (or close to) 1
        // and drop, so 0.06 worth of wear means "worn out". 
        private float wornOutTyreWearLevel = 0f;

        private float scrubbedTyreWearPercent = 2f;
        private float minorTyreWearPercent = 20f;
        private float majorTyreWearPercent = 45f;
        private float wornOutTyreWearPercent = 70f;        

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

        private float targetEngineWaterTemp = 88;
        private float targetEngineOilTemp = 105;
        private float baselineEngineDataOilTemp = 88;
        private float baselineEngineDataWaterTemp = 105;        

        // blue flag zone for improvised blues when the 'full flag rules' are disabled
        // note that this will be set to true at the start of a session and change to false as soon as the game sends a blue flag
        private Boolean useImprovisedBlueFlagDetection = true;
        private int blueFlagDetectionDistance = UserSettings.GetUserSettings().getInt("r3e_blue_flag_detection_distance");

        Dictionary<string, DateTime> lastActiveTimeForOpponents = new Dictionary<string, DateTime>();
        DateTime nextOpponentCleanupTime = DateTime.MinValue;
        TimeSpan opponentCleanupInterval = TimeSpan.FromSeconds(2);

        HashSet<int> positionsFilledForThisTick = new HashSet<int>();
        List<String> opponentDriverNamesProcessedForThisTick = new List<String>();

        private DateTime lastTimeEngineWasRunning = DateTime.MaxValue;
        private HashSet<string> ghostOpponents = new HashSet<string>();

        // True while on HotLap/Qualifying flying start lap.
        private bool approachingFirstFlyingLap = false;

        private bool chequeredFlagShownInThisSession = false;

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

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in r3e shared data so this is a no-op
        }

        public override GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper wrapper = (CrewChiefV4.RaceRoom.R3ESharedMemoryReader.R3EStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            RaceRoomData.RaceRoomShared shared = wrapper.data;

            if (shared.ControlType == (int)RaceRoomConstant.Control.Replay)
            {
                CrewChief.trackName = getNameFromBytes(shared.TrackName);
                CrewChief.carClass = CarData.getCarClassForRaceRoomId(shared.VehicleInfo.ClassId).carClassEnum;
                CrewChief.viewingReplay = true;
                CrewChief.distanceRoundTrack = shared.LapDistance;
                CrewChief.raceroomTrackId = shared.LayoutId;
            }

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

                // belt n braces checks to ensure we have our static data loaded
                String trackNameFromGame = getNameFromBytes(shared.TrackName);
                if (trackNameFromGame != null && trackNameFromGame.Length > 0 && (previousGameState.SessionData.TrackDefinition == null ||
                    previousGameState.SessionData.TrackDefinition.name == null || previousGameState.SessionData.TrackDefinition.name.Length == 0 ||
                    !previousGameState.SessionData.TrackDefinition.name.Equals(trackNameFromGame)))
                {
                    currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(getNameFromBytes(shared.TrackName), shared.LayoutId, shared.LayoutLength);
                    TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackLandmarksForTrackLayoutId(shared.LayoutId);
                    currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                    currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                    currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance = tdc.raceroomRollingStartLapDistance;
                    currentGameState.SessionData.TrackDefinition.setGapPoints();
                    GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                }
                currentGameState.readLandmarksForThisLap = previousGameState.readLandmarksForThisLap;

                currentGameState.SessionData.NumCarsOverallAtStartOfSession = previousGameState.SessionData.NumCarsOverallAtStartOfSession;
            }
            else
            {
                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(getNameFromBytes(shared.TrackName), shared.LayoutId, shared.LayoutLength);
                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackLandmarksForTrackLayoutId(shared.LayoutId);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance = tdc.raceroomRollingStartLapDistance;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);

                // Used by mapToSessionType.
                currentGameState.SessionData.NumCarsOverallAtStartOfSession = shared.NumCars;
                chequeredFlagShownInThisSession = false;
            }

            currentGameState.SessionData.SessionType = mapToSessionType(shared, previousGameState);

            if (previousGameState != null && currentGameState.SessionData.SessionType != previousGameState.SessionData.SessionType)
            {
                Console.WriteLine("Session type changed from: " + previousGameState.SessionData.SessionType + "  to: " + currentGameState.SessionData.SessionType
                    + "  Last session phase: " + previousGameState.SessionData.SessionPhase + "  New session phase: " + currentGameState.SessionData.SessionPhase);
            }

            currentGameState.SessionData.SessionRunningTime = (float)shared.Player.GameSimulationTime;
            currentGameState.ControlData.ControlType = mapToControlType(shared.ControlType);

            // in some cases, the session start trigger gets missed and we don't have a driver name
            currentGameState.SessionData.DriverRawName = getNameFromBytes(shared.PlayerName).ToLower();

            DriverData playerDriverData = new DriverData();
            int playerDriverDataIndex = 0;
            // In hotlap/leaderboard challenge, this contains duplicate entries.  Do we want this?
            string[] driverNames = new string[shared.DriverData.Length];
            for (int i = 0; i < shared.DriverData.Length; i++)
            {
                DriverData participantStruct = shared.DriverData[i];
                String driverName = getNameFromBytes(participantStruct.DriverInfo.Name).ToLower();
                driverNames[i] = driverName;
                if (driverName.Equals(currentGameState.SessionData.DriverRawName))
                {
                    playerDriverData = participantStruct;
                    playerDriverDataIndex = i;
                }
            }

            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase, currentGameState.SessionData.SessionType, lastSessionRunningTime,
                currentGameState.SessionData.SessionRunningTime, shared.SessionPhase, currentGameState.ControlData.ControlType,
                previousLapsCompleted, shared.CompletedLaps, isCarRunning, chequeredFlagShownInThisSession);

            if ((lastSessionPhase != currentGameState.SessionData.SessionPhase && (lastSessionPhase == SessionPhase.Unavailable || lastSessionPhase == SessionPhase.Finished)) ||
                ((lastSessionPhase == SessionPhase.Checkered || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow) && 
                    currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) ||
                lastSessionRunningTime > currentGameState.SessionData.SessionRunningTime)
            {
                currentGameState.SessionData.IsNewSession = true;
                chequeredFlagShownInThisSession = false;
                // if this is a new prac / qual session, we might have just joined a multiclass session so we need to keep
                // updating the car class until it settles.
                if (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice)
                {
                    recheckCarClassesUntil = currentGameState.Now.Add(recheckCarClassesForSeconds);
                }
                else
                {
                    // probably don't need to reset this but it does no harm
                    recheckCarClassesUntil = DateTime.MinValue;
                }
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("lastSessionPhase = " + lastSessionPhase);
                Console.WriteLine("lastSessionRunningTime = " + lastSessionRunningTime);
                Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("rawSessionPhase = " + shared.SessionPhase);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);

                if ((currentGameState.SessionData.SessionType == SessionType.Practice || currentGameState.SessionData.SessionType == SessionType.Qualify)
                    && shared.InPitlane == 1)
                {
                    currentGameState.PitData.PitBoxPositionEstimate = shared.LapDistance;
                    Console.WriteLine("Pit box position = " + currentGameState.PitData.PitBoxPositionEstimate.ToString("0.000"));
                }
                else if (previousGameState != null)
                {
                    // if we're entering a race session or rolling qually, copy the value from the previous field
                    currentGameState.PitData.PitBoxPositionEstimate = previousGameState.PitData.PitBoxPositionEstimate;
                }

                // reset the flag to allow the improvised blue flag calling
                useImprovisedBlueFlagDetection = true;

                currentGameState.SessionData.NumCarsOverallAtStartOfSession = shared.NumCars;
                currentGameState.SessionData.EventIndex = shared.EventIndex;
                currentGameState.SessionData.SessionIteration = shared.SessionIteration;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                currentGameState.OpponentData.Clear();
                ghostOpponents.Clear();
                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(getNameFromBytes(shared.TrackName), shared.LayoutId, shared.LayoutLength);
                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackLandmarksForTrackLayoutId(shared.LayoutId);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance = tdc.raceroomRollingStartLapDistance;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                {
                    if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                    {
                        if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                        {
                            currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                        }
                    }
                }
                currentGameState.PitData.IsRefuellingAllowed = true;

                lastActiveTimeForOpponents.Clear();
                nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;

                if (shared.sessionLengthFormat == 0 || shared.sessionLengthFormat == 2 || shared.SessionTimeRemaining > 0)
                {
                    currentGameState.SessionData.SessionTotalRunTime = shared.SessionTimeRemaining;
                    currentGameState.SessionData.SessionHasFixedTime = true;
                }

                // reset the engine temp monitor stuff

                baselineEngineDataOilTemp = targetEngineOilTemp;
                baselineEngineDataWaterTemp = targetEngineWaterTemp;
                lastTimeEngineWasRunning = DateTime.MaxValue;
                opponentDriverNamesProcessedForThisTick.Clear();
                for (int i = 0; i < shared.DriverData.Length; i++)
                {
                    DriverData participantStruct = shared.DriverData[i];
                    String driverName = driverNames[i];
                    if (i == playerDriverDataIndex)
                    {
                        currentGameState.SessionData.IsNewSector = previousGameState == null || participantStruct.TrackSector != previousGameState.SessionData.SectorNumber;
                        currentGameState.SessionData.SectorNumber = participantStruct.TrackSector;
                        Validator.validate(driverName);

                        currentGameState.PitData.InPitlane = participantStruct.InPitlane == 1;
                        currentGameState.PositionAndMotionData.DistanceRoundTrack = participantStruct.LapDistance;
                        currentGameState.carClass = CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId);
                        CarData.RACEROOM_CLASS_ID = participantStruct.DriverInfo.ClassId;
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier() + " (class ID " + participantStruct.DriverInfo.ClassId + ")");
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        Utilities.TraceEventClass(currentGameState);

                        if ((currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.HotLap) &&
                            currentGameState.SessionData.SectorNumber != 1 && !currentGameState.PitData.InPitlane)
                        {
                            // Assume that Qualify/HotLap starts with flying lap.  This is cleared out when we begin the new lap.
                            approachingFirstFlyingLap = true;
                        }
                        else
                        {
                            approachingFirstFlyingLap = false;
                        }
                    }
                    else
                    {
                        if (driverName.Length > 0 && currentGameState.SessionData.DriverRawName != driverName)
                        {
                            if (!opponentDriverNamesProcessedForThisTick.Contains(driverName))
                            {
                                opponentDriverNamesProcessedForThisTick.Add(driverName);
                                currentGameState.OpponentData.Add(driverName, createOpponentData(participantStruct, driverName,
                                    false, CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId).carClassEnum, currentGameState.SessionData.TrackDefinition.trackLength));
                            }
                        }
                    }
                }
                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);
            }
            else
            {                
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + lastSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        currentGameState.SessionData.JustGoneGreen = true;
                        chequeredFlagShownInThisSession = false;
                        // just gone green, so get the session data
                        if (shared.sessionLengthFormat == 0 || shared.sessionLengthFormat == 2 || shared.SessionTimeRemaining > 0)
                        {
                            currentGameState.SessionData.SessionTotalRunTime = shared.SessionTimeRemaining;
                            if (shared.sessionLengthFormat == 2) 
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

                        lastActiveTimeForOpponents.Clear();
                        nextOpponentCleanupTime = currentGameState.Now + opponentCleanupInterval;

                        currentGameState.SessionData.NumCarsOverallAtStartOfSession = shared.NumCars;
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                        currentGameState.carClass = CarData.getCarClassForRaceRoomId(shared.VehicleInfo.ClassId);
                        CarData.RACEROOM_CLASS_ID = shared.VehicleInfo.ClassId;
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                            currentGameState.PositionAndMotionData.DistanceRoundTrack = playerDriverData.LapDistance;
                            currentGameState.PitData.PitBoxPositionEstimate = previousGameState.PitData.PitBoxPositionEstimate;
                        }
                        currentGameState.PitData.PitWindowStart = shared.PitWindowStart;
                        currentGameState.PitData.PitWindowEnd = shared.PitWindowEnd;
                        currentGameState.PitData.HasMandatoryPitStop = currentGameState.PitData.PitWindowStart > 0 && currentGameState.PitData.PitWindowEnd > 0;                         
                        if (currentGameState.PitData.HasMandatoryPitStop)
                        {
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
                                    if (mapToTyreType(shared.TireTypeFront, shared.TireSubTypeFront, shared.TireTypeRear, shared.TireSubTypeFront, 
                                        currentGameState.carClass.carClassEnum) == TyreType.Option)
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
                                    currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = TyreType.Prime;
                                    // the mandatory change must be completed by the end of the pit window
                                    currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = currentGameState.PitData.PitWindowEnd;
                                }
                            }
                        }

                        currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.Now);

                        Console.WriteLine("Just gone green, session details...");

                        // reset the engine temp monitor stuff
                        baselineEngineDataOilTemp = targetEngineOilTemp;
                        baselineEngineDataWaterTemp = targetEngineWaterTemp;

                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
                        Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("PitWindowStart " + currentGameState.PitData.PitWindowStart);
                        Console.WriteLine("PitWindowEnd " + currentGameState.PitData.PitWindowEnd);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                        {
                            if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                            {
                                if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                                {
                                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                                }
                            }
                        }
                        Console.WriteLine("TrackName " + trackName);                        
                    }
                }
                if (!currentGameState.SessionData.JustGoneGreen && previousGameState != null)
                {
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
                    currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
                    currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;
                    currentGameState.SessionData.LapTimePrevious = previousGameState.SessionData.LapTimePrevious;
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

                    currentGameState.FlagData.useImprovisedIncidentCalling = previousGameState.FlagData.useImprovisedIncidentCalling;

                    currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;

                    currentGameState.retriedDriverNames = previousGameState.retriedDriverNames;
                    currentGameState.disqualifiedDriverNames = previousGameState.disqualifiedDriverNames;

                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;

                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;

                    currentGameState.TimingData = previousGameState.TimingData;
                }
            }

            currentGameState.ControlData.ThrottlePedal = shared.ThrottlePedal;
            currentGameState.ControlData.ClutchPedal = shared.ClutchPedal;
            currentGameState.ControlData.BrakePedal = shared.BrakePedal;
            currentGameState.ControlData.BrakeBias = shared.BrakeBias;
            currentGameState.TransmissionData.Gear = shared.Gear;

            //------------------------ Session data -----------------------
            currentGameState.SessionData.Flag = FlagEnum.UNKNOWN;
            // Mark Yellow sectors.
            // note that any yellow flag info will switch off improvised incident calling for the remainder of the session
            if (shared.sector1Yellow == 1) {
                currentGameState.FlagData.sectorFlags[0] = FlagEnum.YELLOW;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            } else if (shared.sector1Yellow == 2) {
                currentGameState.FlagData.sectorFlags[0] = FlagEnum.DOUBLE_YELLOW;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            }
            if (shared.sector2Yellow == 1) {
                currentGameState.FlagData.sectorFlags[1] = FlagEnum.YELLOW;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            } else if (shared.sector2Yellow == 2) {
                currentGameState.FlagData.sectorFlags[1] = FlagEnum.DOUBLE_YELLOW;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            }
            if (shared.sector3Yellow == 1) {
                currentGameState.FlagData.sectorFlags[2] = FlagEnum.YELLOW;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            } else if (shared.sector3Yellow == 2) {
                currentGameState.FlagData.sectorFlags[2] = FlagEnum.DOUBLE_YELLOW;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            }
            currentGameState.FlagData.isLocalYellow = shared.Flags.Yellow == 1;
            if (currentGameState.FlagData.isLocalYellow)
            {
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            }
            // the flag rules cover blue and yellow with a single in-game option.
            // So as soon as we see any yellow flag data we know that the game is also handling blues, so we turn this off too.
            if (!currentGameState.FlagData.useImprovisedIncidentCalling)
            {
                useImprovisedBlueFlagDetection = false;
            }

            if (shared.FlagsExtended2.white == 1 && !currentGameState.FlagData.isLocalYellow)
            {
                currentGameState.SessionData.Flag = FlagEnum.WHITE;
            }
            else if (shared.Flags.Black == 1)
            {
                currentGameState.SessionData.Flag = FlagEnum.BLACK;
            }

            if (shared.FlagsExtended.checkered == 1 && currentGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                chequeredFlagShownInThisSession = true;
            }

            currentGameState.FlagData.numCarsPassedIllegally = shared.FlagsExtended2.yellowPositionsGained;
            if (shared.FlagsExtended2.yellowOvertake == 1)
            {
                currentGameState.FlagData.canOvertakeCarInFront = PassAllowedUnderYellow.YES;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            }
            else if (shared.FlagsExtended2.yellowOvertake == 0)
            {
                currentGameState.FlagData.canOvertakeCarInFront = PassAllowedUnderYellow.NO;
            }

            // closestYellowLapDistance is the distance roundn the lap from the player to the incident
            if (shared.closestYellowLapDistance > 0)
            {
                currentGameState.FlagData.distanceToNearestIncident = shared.closestYellowLapDistance;
                currentGameState.FlagData.useImprovisedIncidentCalling = false;
            }

            if (shared.Flags.Blue == 1)
            {
                currentGameState.SessionData.Flag = FlagEnum.BLUE;
                useImprovisedBlueFlagDetection = false;
            }

            currentGameState.SessionData.SessionTimeRemaining = shared.SessionTimeRemaining;
            currentGameState.SessionData.CompletedLaps = shared.CompletedLaps;

            currentGameState.SessionData.LapTimeCurrent = shared.LapTimeCurrentSelf;
            currentGameState.SessionData.NumCarsOverall = shared.NumCars;

            currentGameState.SessionData.OverallPosition = currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null ?
                getRacePosition(currentGameState.SessionData.DriverRawName, previousGameState.SessionData.OverallPosition, shared.Position, currentGameState.Now)
                : shared.Position;
            // currentGameState.SessionData.Position = shared.Position;
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

            if (previousGameState != null && !currentGameState.SessionData.IsNewSession)
            {
                currentGameState.OpponentData = previousGameState.OpponentData;
                currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
            }
            if (currentGameState.SessionData.OverallPosition == 1)
            {
                currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
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
                            Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier() + " (class ID " + participantStruct.DriverInfo.ClassId + ")");
                            brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        }
                    }
                    if (currentGameState.SessionData.CurrentLapIsValid && (participantStruct.CurrentLapValid != 1 || participantStruct.LapTimeCurrentSelf == -1) && !approachingFirstFlyingLap)
                    {
                        currentGameState.SessionData.CurrentLapIsValid = false;
                    }

                    // Note that the participantStruct.TrackSector does NOT get updated if the participant exits to the pits. If he does this,
                    // participantStruct.TrackSector will remain at whatever it was when he exited until he starts he next flying lap
                    currentGameState.SessionData.IsNewSector = participantStruct.TrackSector != 0 && currentGameState.SessionData.SectorNumber != participantStruct.TrackSector;
                    currentGameState.PitData.InPitlane = participantStruct.InPitlane == 1;
                    currentGameState.SessionData.IsNewLap = previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                        (shared.CompletedLaps == previousGameState.SessionData.CompletedLaps + 1 ||
                         (participantStruct.TrackSector == 1 && currentGameState.SessionData.IsNewSector) ||
                        ((lastSessionPhase == SessionPhase.Countdown || lastSessionPhase == SessionPhase.Formation || lastSessionPhase == SessionPhase.Garage)
                        && (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)));
                    if (currentGameState.SessionData.IsNewLap)
                    {
                        currentGameState.readLandmarksForThisLap = false;
                        currentGameState.SessionData.PreviousLapWasValid = currentGameState.SessionData.CurrentLapIsValid && !approachingFirstFlyingLap;
                        currentGameState.SessionData.CurrentLapIsValid = true;

                        currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                            shared.LapTimePreviousSelf, currentGameState.SessionData.CurrentLapIsValid, currentGameState.PitData.InPitlane, false,
                            30, 25, currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining, 3, currentGameState.TimingData);
                        currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                            currentGameState.SessionData.OverallPosition, currentGameState.PitData.InPitlane, currentGameState.SessionData.SessionRunningTime);
                    }
                    else if (currentGameState.SessionData.IsNewSector)
                    {
                        // at this point in the mapper, the sector number is the sector we just left
                        float sectorTime = currentGameState.SessionData.SectorNumber == 1 ?
                            participantStruct.SectorTimeCurrentSelf.Sector1 : participantStruct.SectorTimeCurrentSelf.Sector2;
                        currentGameState.SessionData.playerAddCumulativeSectorData(currentGameState.SessionData.SectorNumber, currentGameState.SessionData.OverallPosition, sectorTime,
                            currentGameState.SessionData.SessionRunningTime, currentGameState.SessionData.CurrentLapIsValid, false, 30, 25);
                    }

                    if (currentGameState.SessionData.SectorNumber == 1)
                    {
                        // Don't consider this lap as flying as soon as we reach sector 1.  Ideally this should be done on new lap, but it seems that we disregard sector number in check above.
                        approachingFirstFlyingLap = false;
                    }

                    currentGameState.SessionData.SectorNumber = participantStruct.TrackSector;
                    
                    currentGameState.PositionAndMotionData.DistanceRoundTrack = participantStruct.LapDistance;
                    currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(currentGameState.PositionAndMotionData.DistanceRoundTrack, 
                        currentGameState.SessionData.CompletedLaps, shared.CarSpeed, currentGameState.Now);

                    if (previousGameState != null)
                    {
                        String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                            currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack,
                            participantStruct.LapDistance, shared.CarSpeed);
                        currentGameState.SessionData.stoppedInLandmark = participantStruct.InPitlane == 1 ? null : stoppedInLandmark;
                    }

                    if (currentGameState.PitData.InPitlane)
                    {
                        if (previousGameState != null && !previousGameState.PitData.InPitlane)
                        {
                            if (currentGameState.SessionData.SessionRunningTime > 30 && currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                currentGameState.PitData.NumPitStops++;
                            }
                            currentGameState.PitData.OnInLap = true;
                            currentGameState.PitData.OnOutLap = false;
                        }
                        else if (currentGameState.SessionData.IsNewLap)
                        {
                            currentGameState.PitData.OnInLap = false;
                            currentGameState.PitData.OnOutLap = true;
                        }
                    }
                    else if (previousGameState != null && previousGameState.PitData.InPitlane)
                    {
                        currentGameState.PitData.OnInLap = false;
                        currentGameState.PitData.OnOutLap = true;
                        currentGameState.PitData.IsAtPitExit = true;
                    }
                    else if (currentGameState.SessionData.IsNewLap)
                    {
                        // starting a new lap while not in the pitlane so clear the in / out lap flags
                        currentGameState.PitData.OnInLap = false;
                        currentGameState.PitData.OnOutLap = false;
                    }
                    break;
                }
            }

            if (currentGameState.SessionData.IsNewLap)
            {
                // quick n dirty hack here - if the current car class is unknown, try and get it again
                if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                {
                    currentGameState.carClass = CarData.getCarClassForRaceRoomId(shared.VehicleInfo.ClassId);
                    brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                }
                currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
            }

            opponentDriverNamesProcessedForThisTick.Clear();
            opponentDriverNamesProcessedForThisTick.Add(currentGameState.SessionData.DriverRawName);
            positionsFilledForThisTick.Clear();
            positionsFilledForThisTick.Add(currentGameState.SessionData.OverallPosition);
            for (int i = 0; i < shared.DriverData.Length; i++)
            {
                if (i == playerDriverDataIndex)
                {
                    continue;
                }
                DriverData participantStruct = shared.DriverData[i];
                /*
                // don't discard opponents with duplicate positions
                if (positionsFilledForThisTick.Contains(participantStruct.Place))
                {
                    // discard this participant element because the race position is already occupied
                    continue;
                }*/
                String driverName = driverNames[i];
                if (driverName.Length == 0 || driverName == currentGameState.SessionData.DriverRawName || opponentDriverNamesProcessedForThisTick.Contains(driverName) ||
                    participantStruct.Place < 1 || participantStruct.FinishStatus == (int)CrewChiefV4.RaceRoom.RaceRoomConstant.FinishStatus.R3E_FINISH_STATUS_DNS)
                {
                    // allow these drivers be pruned from the set if we continue to receive no data for them
                    continue;
                }
                else if (participantStruct.FinishStatus == (int)CrewChiefV4.RaceRoom.RaceRoomConstant.FinishStatus.R3E_FINISH_STATUS_DNF)
                {
                    // remove this driver from the set immediately
                    if (!currentGameState.retriedDriverNames.Contains(driverName))
                    {
                        Console.WriteLine("Opponent " + driverName + " has retired");
                        currentGameState.retriedDriverNames.Add(driverName);
                    }
                    currentGameState.OpponentData.Remove(driverName);
                    continue;
                }
                else if (participantStruct.FinishStatus == (int)CrewChiefV4.RaceRoom.RaceRoomConstant.FinishStatus.R3E_FINISH_STATUS_DQ)
                {
                    // remove this driver from the set immediately
                    if (!currentGameState.disqualifiedDriverNames.Contains(driverName))
                    {
                        Console.WriteLine("Opponent " + driverName + " has been disqualified");
                        currentGameState.disqualifiedDriverNames.Add(driverName);
                    }
                    currentGameState.OpponentData.Remove(driverName);
                    continue;
                }
                lastActiveTimeForOpponents[driverName] = currentGameState.Now;
                positionsFilledForThisTick.Add(participantStruct.Place);
                opponentDriverNamesProcessedForThisTick.Add(driverName);
                OpponentData currentOpponentData = null;
                if (currentGameState.OpponentData.TryGetValue(driverName, out currentOpponentData))
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
                        float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                        float previousOpponentSpeed = 0;
                        float previousDistanceRoundTrack = 0;
                        Boolean previousOpponentInPits = false;
                        if (previousGameState.OpponentData.TryGetValue(driverName, out previousOpponentData))
                        {
                            previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                            previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                            previousOpponentPosition = previousOpponentData.OverallPosition;
                            previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                            previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                            previousOpponentInPits = previousOpponentData.InPits;
                            previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                            previousOpponentSpeed = previousOpponentData.Speed;
                            newOpponentLap = previousOpponentData.CurrentSectorNumber == 3 && participantStruct.TrackSector == 1;
                            previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;
                            currentOpponentData.ClassPositionAtPreviousTick = previousOpponentData.ClassPosition;
                            currentOpponentData.OverallPositionAtPreviousTick = previousOpponentData.OverallPosition;
                        }

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

                        int currentOpponentRacePosition = currentGameState.SessionData.SessionType == SessionType.Race && previousOpponentPosition > 0 ?
                            getRacePosition(driverName, previousOpponentPosition, participantStruct.Place, currentGameState.Now)
                            : participantStruct.Place;
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
                        Boolean isEnteringPits = participantStruct.InPitlane == 1 && !previousOpponentInPits;
                        Boolean isLeavingPits = previousOpponentInPits && participantStruct.InPitlane != 1;

                        float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;

                        // lap invalidation: at the start of the lap the laptime will be 0, so only check if the time is zero if
                        // we've actually start this lap. For this we use LapDistance > 1 metre. Bit of an abitrary choice but
                        // that's we roll at the CCMC
                        Boolean lapInvalidated = participantStruct.CurrentLapValid != 1 || participantStruct.LapTimeCurrentSelf == -1 ||
                            (participantStruct.LapDistance > 1 && participantStruct.LapTimeCurrentSelf == 0);

                        upateOpponentData(currentOpponentData, currentOpponentRacePosition,
                                participantStruct.Place, currentOpponentLapsCompleted,
                                currentOpponentSector, sectorTime, participantStruct.SectorTimePreviousSelf.Sector3,
                                participantStruct.InPitlane == 1, !lapInvalidated,
                                currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                new float[] { participantStruct.Position.X, participantStruct.Position.Z }, previousOpponentWorldPosition,
                                participantStruct.LapDistance, participantStruct.TireTypeFront, participantStruct.TireSubTypeFront,
                                participantStruct.TireTypeRear, participantStruct.TireSubTypeRear,
                                currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                currentGameState.SessionData.SessionType == SessionType.Race,
                                currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks,
                                participantStruct.CarSpeed,
                                participantStruct.LapTimeCurrentSelf,
                                participantStruct.DriverInfo.ClassId, currentGameState.Now,
                                currentGameState.TimingData,
                                currentGameState.carClass);

                        currentOpponentData.DeltaTime.SetNextDeltaPoint(currentOpponentLapDistance, currentOpponentData.CompletedLaps, currentOpponentData.Speed, currentGameState.Now);

                        if (previousOpponentData != null)
                        {
                            currentOpponentData.trackLandmarksTiming = previousOpponentData.trackLandmarksTiming;
                            String stoppedInLandmark = currentOpponentData.trackLandmarksTiming.updateLandmarkTiming(
                                currentGameState.SessionData.TrackDefinition, currentGameState.SessionData.SessionRunningTime,
                                previousDistanceRoundTrack, currentOpponentData.DistanceRoundTrack, currentOpponentData.Speed);
                            currentOpponentData.stoppedInLandmark = currentOpponentData.InPits ? null : stoppedInLandmark;
                        }
                        if (currentGameState.SessionData.JustGoneGreen)
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
                                    float playerClassSessionBestByTyre = -1.0f;
                                    if (currentOpponentData.LastLapTime > 0 && currentOpponentData.LastLapValid &&
                                        (!currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre.TryGetValue(currentOpponentData.CurrentTyres, out playerClassSessionBestByTyre) ||
                                        playerClassSessionBestByTyre > currentOpponentData.LastLapTime))
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

                        // improvised blue flag calculation when we have full flag rules disabled
                        if (useImprovisedBlueFlagDetection)
                        {
                            Boolean isInSector1OnOutlap = currentOpponentData.CurrentSectorNumber == 1 &&
                                (currentOpponentData.getCurrentLapData() != null && currentOpponentData.getCurrentLapData().OutLap);
                            if (currentGameState.SessionData.SessionType == SessionType.Race && currentOpponentData.OverallPosition == participantStruct.Place &&
                                !isEnteringPits && !isLeavingPits && currentGameState.PositionAndMotionData.DistanceRoundTrack != 0 &&
                                currentOpponentData.OverallPosition + 1 < shared.Position && !isInSector1OnOutlap &&
                                isBehindWithinDistance(shared.LayoutLength, 8, blueFlagDetectionDistance, currentGameState.PositionAndMotionData.DistanceRoundTrack,
                                participantStruct.LapDistance))
                            {
                                currentGameState.SessionData.Flag = FlagEnum.BLUE;
                            }
                        }
                    }
                }
                else
                {
                    Boolean skipGhost = false;
                    if (currentGameState.OpponentData.Count() == 0 
                        &&  (currentGameState.SessionData.SessionType == SessionType.HotLap
                             || currentGameState.SessionData.SessionType == SessionType.LonePractice))
                    {
                        if (!ghostOpponents.Contains(driverName))
                        {
                            // Not sure if this is valid check.  We need a trace of the online session with folks joining in.
                            if (participantStruct.InPitlane == 0
                                && participantStruct.CarSpeed == 0.0f
                                && Math.Abs(participantStruct.LapDistance) < 50.0)
                            {
                                ghostOpponents.Add(driverName);
                                Console.WriteLine("Added opponent: " + driverName + " into the set of ghost vehicles.  At lapdist: " + participantStruct.LapDistance.ToString("0.000"));
                                skipGhost = true;
                            }
                        }
                        else
                        {
                            skipGhost = true;
                        }
                    }

                    if (!skipGhost)
                    {
                        currentGameState.OpponentData.Add(driverName, createOpponentData(participantStruct, driverName, true,
                            CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId).carClassEnum, currentGameState.SessionData.TrackDefinition.trackLength));
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
                    DateTime lastActiveTime = DateTime.MinValue;
                    if (!lastActiveTimeForOpponents.TryGetValue(opponentName, out lastActiveTime) || lastActiveTime < oldestAllowedUpdate)
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
            //Sort class positions
            currentGameState.sortClassPositions();
            currentGameState.setPracOrQualiDeltas();

            if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.PreviousLapWasValid &&
                currentGameState.SessionData.LapTimePrevious > 0)
            {
                // TODO: different tyre types on the same car
                float playerClassBestTimeByTyre = -1.0f;
                if (!currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre.TryGetValue(currentGameState.TyreData.FrontLeftTyreType, out playerClassBestTimeByTyre) ||
                    playerClassBestTimeByTyre > currentGameState.SessionData.LapTimePrevious)
                {
                    currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre[currentGameState.TyreData.FrontLeftTyreType] = currentGameState.SessionData.LapTimePrevious;
                }
                float playerBestLapTimeByTyre = -1.0f;
                if (!currentGameState.SessionData.PlayerBestLapTimeByTyre.TryGetValue(currentGameState.TyreData.FrontLeftTyreType, out playerBestLapTimeByTyre) ||
                    playerBestLapTimeByTyre > currentGameState.SessionData.LapTimePrevious)
                {
                    currentGameState.SessionData.PlayerBestLapTimeByTyre[currentGameState.TyreData.FrontLeftTyreType] = currentGameState.SessionData.LapTimePrevious;
                }
            }

            if (shared.SessionType == (int)RaceRoomConstant.Session.Race && shared.SessionPhase == (int)RaceRoomConstant.SessionPhase.Checkered &&
                previousGameState != null && (previousGameState.SessionData.SessionPhase == SessionPhase.Green || previousGameState.SessionData.SessionPhase == SessionPhase.Green))
            {
                Console.WriteLine("Leader has finished race, player has done "+ shared.CompletedLaps + " laps, session time = " + shared.Player.GameSimulationTime);
                currentGameState.SessionData.LeaderHasFinishedRace = true;
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
            currentGameState.EngineData.EngineRpm = shared.EngineRps * (60 / (2 * (Single)Math.PI));
            currentGameState.EngineData.MaxEngineRpm = shared.MaxEngineRps * (60 / (2 * (Single)Math.PI));
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 5;
            
            currentGameState.EngineData.EngineOilTemp = shared.EngineOilTemp;
            currentGameState.EngineData.EngineWaterTemp = shared.EngineWaterTemp;

            //------------------------ Fuel data -----------------------
            currentGameState.FuelData.FuelUseActive = shared.FuelUseActive != 0;    // there seems to be some issue with this...
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
            if (previousGameState != null)
            {
                currentGameState.PitData.MandatoryPitStopCompleted = previousGameState.PitData.MandatoryPitStopCompleted || shared.PitWindowStatus == (int)PitWindow.Completed;
            }
            currentGameState.PitData.limiterStatus = shared.PitLimiter;

            //------------------------ Car position / motion data -----------------------
            currentGameState.PositionAndMotionData.CarSpeed = shared.CarSpeed;


            //------------------------ Transmission data -----------------------
            currentGameState.TransmissionData.Gear = shared.Gear;


            //------------------------ Tyre data -----------------------
            // no way to have unmatched tyre types in R3E
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TyreWearActive = shared.TireWearActive == 1;
            TyreType tyreType = mapToTyreType(shared.TireTypeFront, shared.TireSubTypeFront, shared.TireTypeRear, shared.TireSubTypeFront, currentGameState.carClass.carClassEnum);
            currentGameState.TyreData.FrontLeft_CenterTemp = shared.TireTemp.FrontLeft_Center;
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.TireTemp.FrontLeft_Left;
            currentGameState.TyreData.FrontLeft_RightTemp = shared.TireTemp.FrontLeft_Right;
            float frontLeftTemp = (currentGameState.TyreData.FrontLeft_CenterTemp + currentGameState.TyreData.FrontLeft_LeftTemp + currentGameState.TyreData.FrontLeft_RightTemp) / 3;
            currentGameState.TyreData.FrontLeftTyreType = tyreType;
            currentGameState.TyreData.FrontLeftPressure = shared.TirePressure.FrontLeft;
            currentGameState.TyreData.FrontLeftPercentWear = getTyreWearPercentage(shared.TireWear.FrontLeft);
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontLeftTemperatureForLap == 0)
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
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontRightTemperatureForLap == 0)
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
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearLeftTemperatureForLap == 0)
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
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearRightTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }
            else if (previousGameState == null || rearRightTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = rearRightTemp;
            }

            currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(tyreWearThresholds, currentGameState.TyreData.FrontLeftPercentWear,
                currentGameState.TyreData.FrontRightPercentWear, currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);

            var tyreTempThresholds = CarData.getTyreTempThresholds(currentGameState.carClass, tyreType);
            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(tyreTempThresholds,
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
                float minRotatingSpeed = (float)Math.PI * shared.CarSpeed / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.TireRps.FrontLeft) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.TireRps.FrontRight) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.TireRps.RearLeft) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.TireRps.RearRight) < minRotatingSpeed;

                float maxRotatingSpeed = 3 * (float)Math.PI * shared.CarSpeed / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.TireRps.FrontLeft) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.TireRps.FrontRight) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.TireRps.RearLeft) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.TireRps.RearRight) > maxRotatingSpeed;
            }
            currentGameState.OvertakingAids = getOvertakingAids(shared, currentGameState.carClass.carClassEnum, currentGameState.SessionData.CompletedLaps,
                currentGameState.SessionData.SessionNumberOfLaps, currentGameState.SessionData.SessionTimeRemaining,
                currentGameState.SessionData.SessionType);

            if (currentGameState.SessionData.TrackDefinition != null)
            {
                CrewChief.trackName = currentGameState.SessionData.TrackDefinition.name;
            }
            if (currentGameState.carClass != null)
            {
                CrewChief.carClass = currentGameState.carClass.carClassEnum;
            }
            CrewChief.distanceRoundTrack = currentGameState.PositionAndMotionData.DistanceRoundTrack;
            CrewChief.raceroomTrackId = shared.LayoutId;
            CrewChief.viewingReplay = false;

            currentGameState.PositionAndMotionData.Orientation.Pitch = shared.CarOrientation.Pitch;
            currentGameState.PositionAndMotionData.Orientation.Roll = shared.CarOrientation.Roll;
            currentGameState.PositionAndMotionData.Orientation.Yaw = shared.CarOrientation.Yaw;

            if (currentGameState.EngineData.EngineRpm > 5)
            {
                lastTimeEngineWasRunning = currentGameState.Now;
            }
            // don't check for stalled engine in qualify because this will be triggered when starting a lap in rolling-start qual
            if (!currentGameState.PitData.InPitlane && 
                currentGameState.SessionData.SessionType != SessionType.Qualify &&
                previousGameState != null && !previousGameState.EngineData.EngineStalledWarning &&
                currentGameState.SessionData.SessionRunningTime > 60 && currentGameState.EngineData.EngineRpm < 5 &&
                lastTimeEngineWasRunning < currentGameState.Now.Subtract(TimeSpan.FromSeconds(2)) &&
                shared.GameInMenus == 0)  // Don't play engine stall message in menu.
            {
                currentGameState.EngineData.EngineStalledWarning = true;
                lastTimeEngineWasRunning = DateTime.MaxValue;
            }

            // hack to work around delayed car class data joining online multiclass sessions
            if (currentGameState.Now < recheckCarClassesUntil)
            {
                CarData.CarClass correctedCarClass = CarData.getCarClassForRaceRoomId(playerDriverData.DriverInfo.ClassId);
                if (!CarData.IsCarClassEqual(correctedCarClass, currentGameState.carClass))
                {
                    Console.WriteLine("Player car class in game data has changed. Updating to " + correctedCarClass.getClassIdentifier());
                    currentGameState.carClass = correctedCarClass;
                    CarData.RACEROOM_CLASS_ID = playerDriverData.DriverInfo.ClassId;
                    GlobalBehaviourSettings.UpdateFromCarClass(correctedCarClass);
                }
            }

             if (currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.hardPartsOnTrackData.updateHardPartsForNewLap(currentGameState.SessionData.LapTimePrevious))
                {
                    currentGameState.SessionData.TrackDefinition.adjustGapPoints(currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap);
                }
            }
             else if (!currentGameState.PitData.OnOutLap &&
                 !(currentGameState.SessionData.SessionType == SessionType.Race &&
                   (currentGameState.SessionData.CompletedLaps < 1 || (GameStateData.useManualFormationLap && currentGameState.SessionData.CompletedLaps < 2))))
            {
                currentGameState.hardPartsOnTrackData.mapHardPartsOnTrack(currentGameState.ControlData.BrakePedal, currentGameState.ControlData.ThrottlePedal,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CurrentLapIsValid, currentGameState.SessionData.TrackDefinition.trackLength);
            }
            return currentGameState;
        }

        private TyreType mapToTyreType(int tire_type_front, int tire_sub_type_front, int tire_type_rear, int tire_sub_type_rear, CarData.CarClassEnum carClass)
        {
            // TODO: handle cases where the front and rears are different types or subtypes. Here we assume the rears are always the same as the fronts
            if (CarData.r3e2017TyreModelClasses.Contains(carClass))
            {
                return TyreType.R3E_2017;
            } 
            else if (carClass == CarData.CarClassEnum. F1 && (int)RaceRoomConstant.TireSubType.Hard == tire_sub_type_front)
            {
                return TyreType.R3E_2016_HARD;
            }
            else if (carClass == CarData.CarClassEnum.F1 && (int)RaceRoomConstant.TireSubType.Medium == tire_sub_type_front)
            {
                return TyreType.R3E_2016_MEDIUM;
            }
            else if (carClass == CarData.CarClassEnum.F1 && (int)RaceRoomConstant.TireSubType.Soft == tire_sub_type_front)
            {
                return TyreType.R3E_2016_SOFT;
            }
            else if ((int)RaceRoomConstant.TireSubType.Alternate == tire_sub_type_front)
            {
                return TyreType.Alternate;
            }
            else if ((int)RaceRoomConstant.TireSubType.Primary == tire_sub_type_front)
            {
                return TyreType.Primary;
            }
            else if ((carClass == CarData.CarClassEnum.DTM_2014 || carClass == CarData.CarClassEnum.DTM_2015 || carClass == CarData.CarClassEnum.DTM_2016) &&
                (int)RaceRoomConstant.TireType.DTM_Option == tire_type_front)
            {
                return TyreType.Option;
            }
            else if ((carClass == CarData.CarClassEnum.DTM_2014 || carClass == CarData.CarClassEnum.DTM_2015 || carClass == CarData.CarClassEnum.DTM_2016) &&
                (int)RaceRoomConstant.TireType.Prime == tire_type_front)
            {
                return TyreType.Prime;
            }
            else if (CarData.r3e2016TyreModelClasses.Contains(carClass))
            {
                return TyreType.R3E_2016;
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
            int r3eSessionPhase, ControlType controlType, int previousLapsCompleted, int currentLapsCompleted, Boolean isCarRunning, Boolean chequeredFlagShownInThisSession)
        {
            /* prac and qual sessions go chequered after the allotted time. They never go 'finished'. If we complete a lap
             * during this period we can detect the session end and trigger the finish message. Otherwise we just can't detect
             * this period end - hence the 'isCarRunning' hack...
            */
            if ((int)RaceRoomConstant.SessionPhase.Checkered == r3eSessionPhase || chequeredFlagShownInThisSession)
            {
                if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow)
                {
                    // only allow a transition to checkered if the last state was green
                    Console.WriteLine("Checkered - completed " + currentLapsCompleted + " laps, session running time = " + thisSessionRunningTime);
                    return SessionPhase.Checkered;
                }
                else if (SessionPhase.Checkered == lastSessionPhase)
                {
                    if (previousLapsCompleted != currentLapsCompleted || controlType == ControlType.AI ||
                        ((currentSessionType == SessionType.Qualify || currentSessionType == SessionType.Practice) && !isCarRunning))
                    {
                        Console.WriteLine("Finished - completed " + currentLapsCompleted + " laps (was " + previousLapsCompleted + "), session running time = " +
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

        // we use the previous gamestate here as we call this method before mapping the opponent data.
        // If the previousGameState opponentData is empty, but the NumCars is > 1, then all driverData 
        // sent by the game have the same name. For practice and qual this is a hint that we're looking
        // at ghost lap data
        public SessionType mapToSessionType(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            RaceRoomData.RaceRoomShared shared = (RaceRoomData.RaceRoomShared)memoryMappedFileStruct;
            RaceRoomConstant.Session r3eSessionType = (RaceRoomConstant.Session) shared.SessionType;

            if (RaceRoomConstant.Session.Practice == r3eSessionType)
            {
                if (previousGameState != null && previousGameState.OpponentData.Count == 0)
                {
                    return SessionType.LonePractice;
                }
                else
                {
                    return SessionType.Practice;
                }
            }
            else if (RaceRoomConstant.Session.Warmup == r3eSessionType)
            {
                return SessionType.Practice;
            }
            else if (RaceRoomConstant.Session.Qualify == r3eSessionType)
            {
                if (previousGameState != null && previousGameState.OpponentData.Count == 0)
                {
                    return SessionType.HotLap;
                }
                return SessionType.Qualify;
            }
            else if (RaceRoomConstant.Session.Race == r3eSessionType)
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
            return Math.Min(100, ((1 - wearLevel) / (1 - wornOutTyreWearLevel)) * 100);
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
            float[] previousWorldPosition, float distanceRoundTrack, int tire_type_front, int tyre_sub_type_front, int tire_type_rear, int tyre_sub_type_rear,  
            Boolean sessionLengthIsTime, float sessionTimeRemaining, Boolean isRace, float nearPitEntryPointDistance, float speed,
            /* currentLapTime is used only to correct the game time at lap start */float currentLapTime,
            /* may need to recalculate car classes for a short time at session start */int carClassId, DateTime now, TimingData timingData, CarData.CarClass playerCarClass)
        {
            // hack to work around delayed car class data in online sessions
            if (now < recheckCarClassesUntil)
            {
                CarData.CarClass correctedCarClass = CarData.getCarClassForRaceRoomId(carClassId);
                if (!CarData.IsCarClassEqual(opponentData.CarClass, correctedCarClass)) 
                {
                    // note we're not correcting the tyre types here but this shouldn't matter as we don't announce them in 
                    // practice and qually anyway
                    Console.WriteLine("Correcting driver " + opponentData.DriverRawName + "'s car class to " + correctedCarClass.getClassIdentifier());
                    opponentData.CarClass = correctedCarClass;
                }
            }

            Boolean isPlayerCarClass = CarData.IsCarClassEqual(opponentData.CarClass, playerCarClass);
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            opponentData.Speed = speed;

            if (opponentData.OverallPosition != racePosition) 
            {
                opponentData.SessionTimeAtLastPositionChange = sessionRunningTime;
            }
            opponentData.OverallPosition = racePosition;
            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.OverallPosition;
            }
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            Boolean wasInPits = opponentData.InPits;
            if (sessionRunningTime > 30 && !wasInPits && isInPits)
            {
                opponentData.InvalidateCurrentLap();
                opponentData.setInLap();
                if (isRace)
                {
                    opponentData.NumPitStops++;
                }
            }
            opponentData.JustEnteredPits = !wasInPits && isInPits;
            opponentData.InPits = isInPits;
            TyreType previousTyreType = opponentData.CurrentTyres;
            opponentData.hasJustChangedToDifferentTyreType = false;
            if (opponentData.InPits)
            {
                opponentData.CurrentTyres = mapToTyreType(tire_type_front, tyre_sub_type_front, tire_type_rear, tyre_sub_type_rear, opponentData.CarClass.carClassEnum);
                if (opponentData.CurrentTyres != previousTyreType)
                {
                    opponentData.TyreChangesByLap[opponentData.OpponentLapData.Count] = opponentData.CurrentTyres;
                    opponentData.hasJustChangedToDifferentTyreType = true;
                }
            }

            // special case for S3 - invalidate immediately because we won't get a chance to invalidate once the lap is finished
            LapData currentLapData = opponentData.getCurrentLapData();
            if (opponentData.CurrentSectorNumber == 3 && sector == 3 && currentLapData != null && currentLapData.IsValid && !lapIsValid)
            {
                opponentData.InvalidateCurrentLap();
            }
            if (opponentData.CurrentSectorNumber != sector)
            {
                if (opponentData.CurrentSectorNumber == 3 && sector == 1)
                {
                    // correct the game time at lap start - if we're 0.1 into the current lap, the game time when we started
                    // this lap is sessionRunningTime - 0.1
                    if (currentLapTime > 0)
                    {
                        sessionRunningTime = sessionRunningTime - currentLapTime;
                    }
                    if (currentLapData != null)
                    {
                        // the game-provided sector3 times appear to be nonsense in the participant data array, so for sector3 we use
                        // the built-in timer (based on the GameTime reported by the game).
                        if (currentLapData.GameTimeAtLapStart > 0)
                        {
                            completedLapTime = sessionRunningTime - currentLapData.GameTimeAtLapStart;
                        }
                        opponentData.CompleteLapWithProvidedLapTime(racePosition, sessionRunningTime, currentLapData.IsValid ? completedLapTime : -1, 
                            isInPits, false, 20, 20, sessionLengthIsTime, sessionTimeRemaining, 3, timingData, isPlayerCarClass);
                    }
                    opponentData.StartNewLap(completedLaps + 1, racePosition, isInPits, sessionRunningTime, false, 20, 20);
                    opponentData.IsNewLap = true;
                }
                else if (opponentData.CurrentSectorNumber == 1 && sector == 2 || opponentData.CurrentSectorNumber == 2 && sector == 3)
                {
                    opponentData.AddCumulativeSectorData(opponentData.CurrentSectorNumber, racePosition, sectorTime, sessionRunningTime, lapIsValid, false, 20, 20);
                    if (sector == 2)
                    {
                        opponentData.CurrentTyres = mapToTyreType(tire_type_front, tyre_sub_type_front, tire_type_rear, tyre_sub_type_rear, opponentData.CarClass.carClassEnum);                        
                    }
                }
                opponentData.CurrentSectorNumber = sector;
            }
            opponentData.CompletedLaps = completedLaps;
            if (wasInPits && !isInPits)
            {
                opponentData.InvalidateCurrentLap();
            }
        }

        private OpponentData createOpponentData(DriverData participantStruct, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass, float trackLength)
        {
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            } 
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = driverName;
            opponentData.OverallPosition = participantStruct.Place;
            opponentData.CompletedLaps = participantStruct.CompletedLaps;
            opponentData.CurrentSectorNumber = participantStruct.TrackSector;
            opponentData.WorldPosition = new float[] { participantStruct.Position.X, participantStruct.Position.Z };
            opponentData.DistanceRoundTrack = participantStruct.LapDistance;
            opponentData.DeltaTime = new DeltaTime(trackLength, opponentData.DistanceRoundTrack, DateTime.UtcNow);
            opponentData.CarClass = CarData.getCarClassForRaceRoomId(participantStruct.DriverInfo.ClassId);
            opponentData.CurrentTyres = mapToTyreType(participantStruct.TireTypeFront, participantStruct.TireSubTypeFront,
                participantStruct.TireTypeRear, participantStruct.TireSubTypeRear, playerCarClass);
            Console.WriteLine("New driver " + driverName + " is using car class " +
                opponentData.CarClass.getClassIdentifier() + " (class ID " + participantStruct.DriverInfo.ClassId + ") with tyres " + opponentData.CurrentTyres);
            opponentData.TyreChangesByLap[0] = opponentData.CurrentTyres;
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
            int count = Array.IndexOf(name, (byte) 0);
            return Encoding.UTF8.GetString(name, 0, count);
        }
    }
}
