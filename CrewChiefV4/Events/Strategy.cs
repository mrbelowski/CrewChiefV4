using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class Strategy : AbstractEvent
    {
        private Boolean enablePitExitPositionEstimates = UserSettings.GetUserSettings().getBoolean("enable_pit_exit_position_estimates");
        private Boolean warnAboutOpponentsExitingCloseToPlayer = UserSettings.GetUserSettings().getBoolean("enable_opponent_pit_exit_estimates");

        // if this is enabled, don't play the pit position estimates on pit entry. This is only a fallback in case
        // we haven't made a pit request
        private Boolean pitBoxPositionCountdown = UserSettings.GetUserSettings().getBoolean("pit_box_position_countdown");

        // less than 70m => 'just ahead' or 'just behind'
        private static float distanceBehindToBeConsideredVeryClose = 70;
        private static float distanceAheadToBeConsideredVeryClose = 70;

        // assume speed at pit exit is 70m/s (mad, I know), so any distance > 200 and < 500 is a few seconds.
        // < 200 means we'll give a simple non-commital ahead / behind
        private static float minDistanceBehindToBeConsideredAFewSeconds= 200;
        private static float minDistanceAheadToBeConsideredAFewSeconds = 200;
        private static float maxDistanceBehindToBeConsideredAFewSeconds = 500;
        private static float maxDistanceAheadToBeConsideredAFewSeconds = 500;

        public static String folderExpectTrafficOnPitExit = "strategy/expect_traffic_on_pit_exit";
        public static String folderClearTrackOnPitExit = "strategy/expect_clear_track_on_pit_exit";
        public static String folderWeShouldEmergeInPosition = "strategy/we_should_emerge_in_position";
        public static String folderCloseBetween = "strategy/close_between";    // stuff like "right with" and "really close to..."
        public static String folderBetween = "strategy/between";
        public static String folderAnd = "strategy/and";
        public static String folderJustAheadOf = "strategy/just_ahead_of";
        public static String folderJustBehind = "strategy/just_behind";
        public static String folderAheadOf = "strategy/ahead_of";
        public static String folderBehind = "strategy/behind";

        // these are a bit tricky as we only know the separation distance, not time
        public static String folderAFewSecondsAheadOf = "strategy/a_few_seconds_ahead_of";
        public static String folderAFewSecondsBehind = "strategy/a_few_seconds_behind";

        public static String folderPitStopCostsUsAbout = "strategy/a_pitstop_costs_us_about";
        // stuff like: "ok, we'll time this stop", "understood, we'll use this stop as a benchmark, push until sector2" or something
        public static String folderTimePitstopAcknowledge = "strategy/acknowledge_time_pitstop";
        // used when we request a benchmark pitstop timing in practice, but we have no best lap data
        public static String folderNeedMoreLapData = "strategy/set_benchmark_laptime_first";
        public static String folderNeedFlyingLap = "strategy/will_calculate_time_loss_from_next_lap";

        public static String folderIsPittingFromPosition = "strategy/is_pitting_from_position";
        public static String folderHeWillComeOutJustInFront = "strategy/he_will_come_out_just_in_front";
        public static String folderHeWillComeOutJustBehind = "strategy/he_will_come_out_just_behind";


        // may be timed during practice.
        // Need to be careful here to ensure this is applicable to the session we've actually entered
        private float playerTimeLostForStop = -1;
        private float gameTimeOnPitEntry = -1;
        private float playerTimeSpentInPitLane = -1;
        private CarData.CarClass carClassForLastPitstopTiming;
        private String trackNameForLastPitstopTiming;
        private Dictionary<String, float> opponentsTimeLostForStop = new Dictionary<string, float>();
        private Dictionary<String, float> opponentsTimeSpentInPitlane = new Dictionary<string, float>();

        public Boolean isTimingPracticeStop = false;
        private Boolean hasPittedDuringPracticeStopProcess = false;
        private float gameTimeWhenEnteringLastSectorInPractice = -1;
        private float lastAndFirstSectorTimesOnStop = -1;

        // an extra check for practice sessions. We only allow the pit benchmark calculation if we've completed
        // a valid lap that's not our first lap and is not a lap where we visited the pits. This prevents the app
        // using the line lap or outlap for its comparison data
        private Boolean hasPracticeLapForComparison = false;

        // this is set by the box-this-lap macro (eeewwww), and primes this event to play the position
        // estimates when we hit the final sector
        public static Boolean playPitPositionEstimates = false;
        private Boolean pitPositionEstimatesRequested = false;

        public Boolean playedPitPositionEstimatesForThisLap = false;

        // for timing opponent stops
        private Boolean timeOpponentStops = true;
        private HashSet<String> opponentsInPitCycle = new HashSet<string>();
        private DateTime nextPitTimingCheckDue = DateTime.MinValue;

        // for tracking opponent pitting
        private DateTime nextOpponentFinalSectorTimingCheckDue = DateTime.MinValue;
        // just used to track when an opponent transitions from to final sector
        private HashSet<String> opponentsInPenultimateSector = new HashSet<string>();
        // these are static because the opponents event needs to check them:
        public static HashSet<String> opponentsWhoWillExitCloseInFront = new HashSet<string>();
        public static HashSet<String> opponentsWhoWillExitCloseBehind = new HashSet<string>();

        private int sectorCount = 3;

        public static String opponentFrontToWatchForPitting = null;
        public static String opponentBehindToWatchForPitting = null;

        private DateTime nextOpponentPitExitWarningDue = DateTime.MinValue;

        private Boolean printS1Positions = false;

        private Dictionary<String, float> opponentsInPitLane = new Dictionary<String, float>();

        private Boolean waitingForValidDataForBenchmark = false;

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.FullCourseYellow }; }
        }

        public Strategy(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            // don't wipe the time-lost-on-stop that may have been derived in a previous session
            opponentsTimeLostForStop.Clear();
            opponentsTimeSpentInPitlane.Clear();
            isTimingPracticeStop = false;
            gameTimeWhenEnteringLastSectorInPractice = -1;
            nextPitTimingCheckDue = DateTime.MinValue;
            nextOpponentFinalSectorTimingCheckDue = DateTime.MinValue;
            opponentsInPitCycle.Clear();
            Strategy.playPitPositionEstimates = false;
            playedPitPositionEstimatesForThisLap = false;
            timeOpponentStops = true;
            opponentsInPenultimateSector.Clear();
            Strategy.opponentsWhoWillExitCloseBehind.Clear();
            hasPittedDuringPracticeStopProcess = false;
            Strategy.opponentsWhoWillExitCloseInFront.Clear();
            pitPositionEstimatesRequested = false;
            Strategy.opponentFrontToWatchForPitting = null;
            Strategy.opponentBehindToWatchForPitting = null;
            printS1Positions = false;
            opponentsInPitLane.Clear();
            hasPracticeLapForComparison = false;
        }

        private void setTimeLossFromBenchmark(GameStateData currentGameState)
        {
            if (sectorCount == 2)
            {
                playerTimeLostForStop = lastAndFirstSectorTimesOnStop - (currentGameState.SessionData.PlayerBestLapSector2Time + currentGameState.SessionData.PlayerBestLapSector1Time);
            }
            else
            {
                playerTimeLostForStop = lastAndFirstSectorTimesOnStop - (currentGameState.SessionData.PlayerBestLapSector3Time + currentGameState.SessionData.PlayerBestLapSector1Time);
            }
        }

        private Boolean hasValidComparisonForBenchmark(GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionType == SessionType.Qualify)
            {
                return false;
            }
            if (currentGameState.SessionData.SessionType == SessionType.Practice)
            {
                return hasPracticeLapForComparison &&
                    (sectorCount == 2 && currentGameState.SessionData.PlayerBestLapSector1Time > 0 && currentGameState.SessionData.PlayerBestLapSector2Time > 0) ||
                    (sectorCount == 3 && currentGameState.SessionData.PlayerBestLapSector1Time > 0 && currentGameState.SessionData.PlayerBestLapSector3Time > 0);
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                return currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 1 && currentGameState.SessionData.PreviousLapWasValid;
            }
            return false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState) 
        {
            // can't be arsed to keep checking this:
            if (previousGameState == null || currentGameState.SessionData.TrackDefinition == null)
            {
                // no track data
                return;
            }
            if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT)
            {
                // tracks may not have 2 or 3 sectors
                sectorCount = CrewChiefV4.assetto.ACSGameStateMapper.numberOfSectorsOnTrack;
                if (sectorCount < 2 || sectorCount > 3)
                {
                    return;
                }
            }

            if (waitingForValidDataForBenchmark && (currentGameState.SessionData.IsNewLap ||
                (currentGameState.SessionData.IsNewSector && previousGameState.SessionData.SectorNumber == 1)))
            {
                if (currentGameState.SessionData.TrackDefinition.name != trackNameForLastPitstopTiming ||
                    !CarData.IsCarClassEqual(currentGameState.carClass, carClassForLastPitstopTiming))
                {
                    // wrong car class or track, cancel waiting
                    waitingForValidDataForBenchmark = false;
                }
                else if (hasValidComparisonForBenchmark(currentGameState))
                {
                    setTimeLossFromBenchmark(currentGameState);
                    Console.WriteLine("Completing pit benchmark in " + currentGameState.SessionData.SessionType +
                        " session, practice pitstop has cost us " + playerTimeLostForStop + " seconds");
                    // only notify about this if we're in a practice session
                    if (currentGameState.SessionData.SessionType == SessionType.Practice)
                    {
                        audioPlayer.playMessage(new QueuedMessage("pit_stop_cost_estimate",
                            MessageContents(folderPitStopCostsUsAbout,
                            TimeSpanWrapper.FromSeconds(playerTimeLostForStop, Precision.SECONDS)),
                            0, this), 10);
                    }
                    waitingForValidDataForBenchmark = false;
                }
            }
            if (currentGameState.SessionData.SessionType == SessionType.Practice)
            {
                if (currentGameState.SessionData.IsNewLap && !currentGameState.PitData.InPitlane && 
                    !currentGameState.PitData.OnOutLap &&
                    currentGameState.SessionData.PreviousLapWasValid)
                {
                    hasPracticeLapForComparison = true;
                }
                // always log the game time at start of final sector, in case we make a late decision to time a stop
                if (enteredLastSector(previousGameState, currentGameState))
                {
                    gameTimeWhenEnteringLastSectorInPractice = currentGameState.SessionData.SessionRunningTime;
                    hasPittedDuringPracticeStopProcess = false;
                }
                if (isTimingPracticeStop)
                {
                    if (!previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
                    {
                        gameTimeOnPitEntry = currentGameState.SessionData.SessionRunningTime;
                        playerTimeSpentInPitLane = -1;
                        // sanity check
                        if (currentGameState.PositionAndMotionData.CarSpeed > 1)
                        {
                            Console.WriteLine("Pitting - this stop will be used as a benchmark. Game time = " + gameTimeWhenEnteringLastSectorInPractice);
                            hasPittedDuringPracticeStopProcess = true;
                        }
                        else
                        {
                            Console.WriteLine("Pitted during practice pitstop window but we appear to have quit-to-pit, cancelling");
                            gameTimeWhenEnteringLastSectorInPractice = -1;
                            isTimingPracticeStop = false;
                            hasPittedDuringPracticeStopProcess = false;
                            gameTimeOnPitEntry = -1;
                        }
                    }
                    else if (previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane && gameTimeOnPitEntry != -1)
                    {
                        playerTimeSpentInPitLane = currentGameState.SessionData.SessionRunningTime - gameTimeOnPitEntry;
                        gameTimeOnPitEntry = -1;
                    }
                    else if (currentGameState.SessionData.SectorNumber == 2 && previousGameState.SessionData.SectorNumber == 1)
                    {
                        if (hasPittedDuringPracticeStopProcess && gameTimeWhenEnteringLastSectorInPractice != -1)
                        {
                            lastAndFirstSectorTimesOnStop = currentGameState.SessionData.SessionRunningTime - gameTimeWhenEnteringLastSectorInPractice;
                            gameTimeWhenEnteringLastSectorInPractice = -1;
                            isTimingPracticeStop = false;
                            hasPittedDuringPracticeStopProcess = false;
                            carClassForLastPitstopTiming = currentGameState.carClass;
                            trackNameForLastPitstopTiming = currentGameState.SessionData.TrackDefinition.name;

                            if (hasValidComparisonForBenchmark(currentGameState))
                            {
                                waitingForValidDataForBenchmark = false;
                                setTimeLossFromBenchmark(currentGameState);
                                Console.WriteLine("Practice pitstop has cost us " + playerTimeLostForStop + " seconds");
                                audioPlayer.playMessage(new QueuedMessage("pit_stop_cost_estimate",
                                    MessageContents(folderPitStopCostsUsAbout,
                                    TimeSpanWrapper.FromSeconds(playerTimeLostForStop, Precision.SECONDS)),
                                    0, this), 10);
                            }
                            else
                            {
                                waitingForValidDataForBenchmark = true;
                            }
                        }
                    }
                }
                // nothing else to do unless we're in race mode
            }
            else if (currentGameState.SessionData.SessionType == SessionType.Race && !currentGameState.PitData.IsTeamRacing)
            {
                // record the time each opponent entered the pitlane
                foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                {
                    // if this guy hasn't completed a lap, ignore him
                    if (entry.Value.CompletedLaps < 1)
                    {
                        continue;
                    }
                    if (entry.Value.IsNewLap)
                    {
                        Strategy.opponentsWhoWillExitCloseBehind.Remove(entry.Value.DriverRawName);
                        Strategy.opponentsWhoWillExitCloseInFront.Remove(entry.Value.DriverRawName);
                    }
                    if (entry.Value.InPits)
                    {
                        if (!opponentsInPitLane.ContainsKey(entry.Value.DriverRawName))
                        {
                            opponentsInPitLane.Add(entry.Value.DriverRawName, currentGameState.SessionData.SessionRunningTime);
                            if (warnAboutOpponentsExitingCloseToPlayer && currentGameState.Now > nextOpponentPitExitWarningDue)
                            {
                                if (Strategy.opponentsWhoWillExitCloseInFront.Contains(entry.Value.DriverRawName))
                                {
                                    // this guy has just entered the pit and we predict he'll exit just in front of us
                                    Console.WriteLine("Opponent " + entry.Value.DriverRawName + " will exit the pit close in front of us");
                                    audioPlayer.playMessage(new QueuedMessage("opponent_exiting_in_front", MessageContents(entry.Value,
                                        folderIsPittingFromPosition, entry.Value.ClassPosition, folderHeWillComeOutJustInFront), 0, this), 10);

                                    // only allow one of these every 10 seconds. When an opponent crosses the start line he's 
                                    // removed from this set anyway
                                    nextOpponentPitExitWarningDue = currentGameState.Now.AddSeconds(10);
                                    Strategy.opponentsWhoWillExitCloseInFront.Remove(entry.Value.DriverRawName);
                                }
                                else if (Strategy.opponentsWhoWillExitCloseBehind.Contains(entry.Value.DriverRawName))
                                {
                                    // this guy has just entered the pit and we predict he'll exit just behind us
                                    Console.WriteLine("Opponent " + entry.Value.DriverRawName + " will exit the pit close behind us");
                                    audioPlayer.playMessage(new QueuedMessage("opponent_exiting_behind", MessageContents(entry.Value,
                                        folderIsPittingFromPosition, entry.Value.ClassPosition, folderHeWillComeOutJustBehind), 0, this), 10);
                                    // only allow one of these every 10 seconds. When an opponent crosses the start line he's 
                                    // removed from this set anyway
                                    nextOpponentPitExitWarningDue = currentGameState.Now.AddSeconds(10);
                                    Strategy.opponentsWhoWillExitCloseBehind.Remove(entry.Value.DriverRawName);
                                }                                
                            }
                        }
                    }
                    else
                    {
                        if (opponentsInPitLane.ContainsKey(entry.Key))
                        {
                            // he's just left the pit lane
                            opponentsTimeSpentInPitlane[entry.Key] = currentGameState.SessionData.SessionRunningTime - opponentsInPitLane[entry.Key];
                        }
                        opponentsInPitLane.Remove(entry.Value.DriverRawName);
                    }
                }

                if (printS1Positions && previousGameState.SessionData.SectorNumber == 1 && currentGameState.SessionData.SectorNumber == 2)
                {
                    printS1Positions = false;
                    Console.WriteLine("After exiting pit, we're in P " + currentGameState.SessionData.ClassPosition);
                }
                if (previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane)
                {
                    // just left the pits, clear our hack
                    Strategy.opponentBehindToWatchForPitting = null;
                    Strategy.opponentFrontToWatchForPitting = null;
                }

                // if we've timed our pitstop in practice, don't search for opponent stop times
                if (currentGameState.SessionData.JustGoneGreen)
                {
                    // don't time opponent stops for iracing because they are so variable - only make estimates for the player,
                    // and when he's done a benchmark stop.
                    timeOpponentStops = CrewChief.gameDefinition.gameEnum != GameEnum.IRACING &&
                        getExpectedPlayerTimeLoss(currentGameState.carClass, currentGameState.SessionData.TrackDefinition.name) == -1;
                }
                if (currentGameState.SessionData.IsNewLap)
                {
                    playedPitPositionEstimatesForThisLap = false;
                    pitPositionEstimatesRequested = false;
                    Strategy.playPitPositionEstimates = false;
                }
                // if we've just requested a pit stop (and the game has this data), trigger the strategy data when we next hit the final sector
                else if (!previousGameState.PitData.HasRequestedPitStop && currentGameState.PitData.HasRequestedPitStop && !playedPitPositionEstimatesForThisLap)
                {
                    Strategy.playPitPositionEstimates = true;
                    pitPositionEstimatesRequested = false;
                }
                // if we've just entered the pitlane and the pit countdown is disabled, and we don't have a penalty, trigger
                // the strategy stuff
                else if (!pitBoxPositionCountdown && !currentGameState.PenaltiesData.HasDriveThrough && !currentGameState.PenaltiesData.HasStopAndGo &&
                    !playedPitPositionEstimatesForThisLap && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
                {
                    Strategy.playPitPositionEstimates = true;
                    pitPositionEstimatesRequested = false;
                }
                if (Strategy.playPitPositionEstimates &&
                    (pitPositionEstimatesRequested || (inFinalSector(currentGameState) && !playedPitPositionEstimatesForThisLap)))
                {
                    // we requested a stop and we're in the final sector, or we requested data, so gather up the data we'll need and report it
                    //
                    // Note that we need to derive the position estimates here before we start slowing for pit entry
                    float bestLapTime;
                    if (sectorCount == 2)
                    {
                        bestLapTime = currentGameState.SessionData.PlayerBestLapSector1Time + currentGameState.SessionData.PlayerBestLapSector2Time;
                    }
                    else
                    {
                        bestLapTime = currentGameState.SessionData.PlayerBestLapSector1Time + currentGameState.SessionData.PlayerBestLapSector2Time + currentGameState.SessionData.PlayerBestLapSector3Time;
                    } 
                    Strategy.PostPitRacePosition postRacePositions = getPostPitPositionData(false, currentGameState.SessionData.ClassPosition, currentGameState.SessionData.CompletedLaps,
                            currentGameState.carClass, currentGameState.OpponentData, currentGameState.SessionData.DeltaTime, currentGameState.Now,
                            currentGameState.SessionData.TrackDefinition.name, currentGameState.SessionData.TrackDefinition.trackLength,
                            currentGameState.PositionAndMotionData.DistanceRoundTrack, bestLapTime, currentGameState.SessionData.SessionRunningTime,
                            currentGameState.PitData.HasMandatoryPitStop, currentGameState.PitData.PitWindowEnd, currentGameState.SessionData.SessionHasFixedTime);
                    reportPostPitData(postRacePositions);
                    playedPitPositionEstimatesForThisLap = true;
                    pitPositionEstimatesRequested = false;
                    Strategy.playPitPositionEstimates = false;
                }



                //--------------------------------------
                // opponent pit exit position estimation
                if (warnAboutOpponentsExitingCloseToPlayer && currentGameState.Now > nextOpponentFinalSectorTimingCheckDue)
                {
                    float expectedPlayerTimeLoss = -1;
                    DateTime nowMinusExpectedLoss = DateTime.MinValue;
                    Boolean doneTimeLossCalc = false;
                    nextOpponentFinalSectorTimingCheckDue = currentGameState.Now.AddSeconds(5);
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        if (entry.Value.CompletedLaps < 1)
                        {
                            continue;
                        }
                        if (((sectorCount == 3 && entry.Value.CurrentSectorNumber == 2) || (sectorCount == 2 && entry.Value.CurrentSectorNumber == 1)) &&
                            !opponentsInPenultimateSector.Contains(entry.Key))
                        {
                            opponentsInPenultimateSector.Add(entry.Key);
                        }
                        else
                        {
                            if (opponentsInPenultimateSector.Contains(entry.Key) && 
                                ((sectorCount == 3 && entry.Value.CurrentSectorNumber == 3) || (sectorCount == 2 && entry.Value.CurrentSectorNumber == 2)))
                            {
                                opponentsInPenultimateSector.Remove(entry.Key);
                                // this guy has just hit final sector, so do the count-back

                                // lazily obtain the expected time loss
                                if (!doneTimeLossCalc)
                                {
                                    expectedPlayerTimeLoss = getTimeLossEstimate(currentGameState.carClass, currentGameState.SessionData.TrackDefinition.name,
                                        currentGameState.OpponentData, currentGameState.SessionData.ClassPosition);

                                    if (expectedPlayerTimeLoss < 5) // 5 is a number i pulled out my arse, less than 5 seconds lost for a stop means something's wrong
                                    {
                                        // no loss data, can't continue
                                        break;
                                    }
                                    // get his track distanceRoundTrack at this point in history
                                    int fullLapsLost = (int)(expectedPlayerTimeLoss / currentGameState.SessionData.PlayerLapTimeSessionBest);

                                    // need to allow for losing 1 or more complete laps. This adjustment is required because the DeltaTimes stuff only
                                    // holds the last lap's worth of data
                                    nowMinusExpectedLoss = currentGameState.Now.AddSeconds((fullLapsLost * currentGameState.SessionData.PlayerLapTimeSessionBest) - expectedPlayerTimeLoss);
                                    doneTimeLossCalc = true;
                                }                                
                                
                                TimeSpan closestDeltapointTimeDelta = TimeSpan.MaxValue;
                                float closestDeltapointPosition = -1;
                                foreach (KeyValuePair<float, DateTime> deltaPointEntry in entry.Value.DeltaTime.deltaPoints)
                                {
                                    TimeSpan timeDelta = (nowMinusExpectedLoss - deltaPointEntry.Value).Duration();
                                    if (timeDelta < closestDeltapointTimeDelta)
                                    {
                                        closestDeltapointTimeDelta = timeDelta;
                                        closestDeltapointPosition = deltaPointEntry.Key;
                                    }
                                }
                                // this is the gap we expect to this guy when he leaves the pits. Negative gap means he'll be in front
                                float expectedDistanceToPlayerOnPitExit = currentGameState.PositionAndMotionData.DistanceRoundTrack - closestDeltapointPosition;
                                float absGap = Math.Abs(expectedDistanceToPlayerOnPitExit);

                                if (expectedDistanceToPlayerOnPitExit < 0 && absGap < minDistanceAheadToBeConsideredAFewSeconds && AudioPlayer.canReadName(entry.Value.DriverRawName))
                                {
                                    // he'll come out of the pits right in front of us if he pits
                                    Strategy.opponentsWhoWillExitCloseInFront.Add(entry.Value.DriverRawName);
                                }
                                else if (expectedDistanceToPlayerOnPitExit > 0 && absGap < minDistanceBehindToBeConsideredAFewSeconds && AudioPlayer.canReadName(entry.Value.DriverRawName))
                                {
                                    // he'll come out right behind us if he pits
                                    Strategy.opponentsWhoWillExitCloseBehind.Add(entry.Value.DriverRawName);
                                }
                            }
                        }
                    }
                }
                //------------------------------


                if (timeOpponentStops && currentGameState.Now > nextPitTimingCheckDue)
                {
                    // check for opponent pit timings every 10 seconds if we don't have our own
                    nextPitTimingCheckDue = currentGameState.Now.AddSeconds(10);
                    // update opponent time lost
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        if (entry.Value.CompletedLaps < 1)
                        {
                            continue;
                        }
                        // only interested in opponent pit times for our class
                        if (CarData.IsCarClassEqual(entry.Value.CarClass, currentGameState.carClass) && entry.Value.CompletedLaps > 2)
                        {
                            if (opponentsInPitCycle.Contains(entry.Key) && entry.Value.CurrentSectorNumber == 2)
                            {
                                // he's just pitted and has entered the finished the first sector since we last checked, so calculate how much time he's lost pitting
                                float bestLastAndFirstSectorTime;
                                if (sectorCount == 3)
                                {
                                    bestLastAndFirstSectorTime = entry.Value.bestSector3Time + entry.Value.bestSector1Time;
                                }
                                else
                                {
                                    bestLastAndFirstSectorTime = entry.Value.bestSector2Time + entry.Value.bestSector1Time;
                                }
                                
                                // note that these are zero-indexed - we want the game time at the end of sector2 on the previous lap and s1 on this lap
                                float lastLapPenultimateSectorEndTime = entry.Value.getLastLapData().GameTimeAtSectorEnd[sectorCount == 3 ? 1 : 0];
                                float thisLapS1EndTime = entry.Value.getCurrentLapData().GameTimeAtSectorEnd[0];
                                // only insert data here if we have sane times
                                if (lastLapPenultimateSectorEndTime > 0 && thisLapS1EndTime > 0)
                                {
                                    float lastPenultimateSectorAndS1Time = thisLapS1EndTime - lastLapPenultimateSectorEndTime;
                                    float timeLost = lastPenultimateSectorAndS1Time - bestLastAndFirstSectorTime;
                                    // if this opponent has lost less than 5 seconds due to pitting, something has gone wrong in the calculations
                                    if (timeLost > 5)
                                    {
                                        opponentsTimeLostForStop[entry.Key] = timeLost;
                                    }
                                }
                                opponentsInPitCycle.Remove(entry.Key);
                            }
                            else if (!opponentsInPitCycle.Contains(entry.Key) && entry.Value.InPits)
                            {
                                opponentsInPitCycle.Add(entry.Key);
                            }
                        }
                    }
                }
            }
        }
        
        private float getExpectedPlayerTimeLoss(CarData.CarClass carClass, String trackName)
        {
            if (CarData.IsCarClassEqual(carClass, carClassForLastPitstopTiming) && trackName == trackNameForLastPitstopTiming)
            {
                return playerTimeLostForStop;
            }
            return -1;
        }

        private float getTimeLossEstimate(CarData.CarClass carClass, String trackName, Dictionary<String, OpponentData> opponents, int currentRacePosition)
        {
            float timeLossEstimate = getExpectedPlayerTimeLoss(carClass, trackName);
            if (timeLossEstimate == -1)
            {
                if (opponentsTimeLostForStop.Count != 0)
                {                    
                    int pittedOpponentPositionDiff = int.MaxValue;
                    // select the best opponent to compare with
                    foreach (KeyValuePair<String, float> entry in opponentsTimeLostForStop)
                    {
                        OpponentData opponentData;
                        if (opponents.TryGetValue(entry.Key, out opponentData))
                        {
                            int positionDiff = Math.Abs(opponentData.ClassPosition - currentRacePosition);
                            if (positionDiff < pittedOpponentPositionDiff)
                            {
                                timeLossEstimate = entry.Value;
                                pittedOpponentPositionDiff = positionDiff;
                                float opponentTimeSpentInPitlane;
                                if (opponentsTimeSpentInPitlane.TryGetValue(entry.Key, out opponentTimeSpentInPitlane))
                                {
                                    playerTimeSpentInPitLane = opponentTimeSpentInPitlane;
                                }
                            }
                        }
                    }
                }
            }
            return timeLossEstimate;
        }

        /**
         * Get the track position (distance round lap) and the total distance covered. Allow for whole laps difference
         * by assuming we'd have been travelling at bestLap pace if we were not stopping
         */
        private Tuple<float, float> getPositionAndTotalDistanceForTimeLoss(float expectedPlayerTimeLoss, float trackLength,
            float currentDistanceRoundTrack, float bestLapTime, int currentLapsCompleted, DateTime now, DeltaTime playerDeltaTime)
        {
            int fullLapsLost = (int) (expectedPlayerTimeLoss / bestLapTime);

            // need to allow for losing 1 or more complete laps. This adjustment is required because the DeltaTimes stuff only
            // holds the last lap's worth of data
            DateTime nowMinusExpectedLoss = now.AddSeconds((fullLapsLost * bestLapTime) - expectedPlayerTimeLoss);
            // get the track distanceRoundTrack at this point in history
            TimeSpan closestDeltapointTimeDelta = TimeSpan.MaxValue;
            float closestDeltapointPosition = float.MaxValue;
            foreach (KeyValuePair<float, DateTime> entry in playerDeltaTime.deltaPoints)
            {
                TimeSpan timeDelta = (nowMinusExpectedLoss - entry.Value).Duration();
                if (timeDelta < closestDeltapointTimeDelta)
                {
                    closestDeltapointTimeDelta = timeDelta;
                    closestDeltapointPosition = entry.Key;
                }
            }

            // work out how far we'd have travelled if we were expectedPlayerTimeLoss seconds behind where we are now
            float totalRaceDistanceAtExpectedLoss;
            // also need to allow for this deltapoint position being in front of us
            if (closestDeltapointPosition > currentDistanceRoundTrack)
            {
                totalRaceDistanceAtExpectedLoss = ((currentLapsCompleted - fullLapsLost - 1) * trackLength) + closestDeltapointPosition;
            }
            else
            {
                totalRaceDistanceAtExpectedLoss = ((currentLapsCompleted - fullLapsLost) * trackLength) + closestDeltapointPosition;
            }
            return new Tuple<float, float>(closestDeltapointPosition, totalRaceDistanceAtExpectedLoss);
        }

        // all the nasty logic is in this method - refactor?
        private Strategy.PostPitRacePosition getPostPitPositionData(Boolean fromVoiceCommand, int currentRacePosition, int lapsCompleted,
            CarData.CarClass playerClass, Dictionary<String, OpponentData> opponents, DeltaTime playerDeltaTime, DateTime now,
            String trackName, float trackLength, float currentDistanceRoundTrack, float bestLapTime, float sessionRunningTime,
            Boolean hasMandatoryPitStop, int pitWindowEnd, Boolean fixedTimeSession)
        {
            float halfTrackLength = trackLength / 2;
            // check we have deltapoints first
            if (playerDeltaTime == null || playerDeltaTime.deltaPoints == null || playerDeltaTime.deltaPoints.Count == 0)
            {
                Console.WriteLine("No usable deltapoints object, can't derive post-pit positions");
                return null;
            }

            float expectedPlayerTimeLoss = getTimeLossEstimate(playerClass, trackName, opponents, currentRacePosition);
            if (expectedPlayerTimeLoss >= 5) // 5 is a number i pulled out my arse - less than 5 and surely something's gone wrong in the calculations
            {
                // now we have a sensible value for the time lost due to the stop, estimate where we'll emerge
                // in order to do this we need to know the total race distance at the point where we'd have been
                // expectedPlayerTimeLoss seconds ago
                Tuple<float, float> positionAndTotalDistanceForTimeLoss = getPositionAndTotalDistanceForTimeLoss(
                    expectedPlayerTimeLoss, trackLength, currentDistanceRoundTrack, bestLapTime, lapsCompleted, now, playerDeltaTime);

                float closestDeltapointPosition = positionAndTotalDistanceForTimeLoss.Item1;

                // this will be scaled
                float baseDistanceCorrectionForPittingOpponents = (((trackLength * lapsCompleted) + currentDistanceRoundTrack) - positionAndTotalDistanceForTimeLoss.Item2);

                List<OpponentPositionAtPlayerPitExit> opponentsAhead = new List<OpponentPositionAtPlayerPitExit>();
                List<OpponentPositionAtPlayerPitExit> opponentsBehind = new List<OpponentPositionAtPlayerPitExit>();
                List<Tuple<OpponentData, float>> totalRaceDistances = new List<Tuple<OpponentData, float>>();

                // for fixed time races, see if we'd woudl expect this guy to stop on this lap:
                Boolean expectMandatoryStopRaceTime = hasMandatoryPitStop && fixedTimeSession && pitWindowEnd > 1 && pitWindowEnd * 60 < sessionRunningTime + bestLapTime;

                foreach (OpponentData opponent in opponents.Values)
                {
                    String opponentCarClassId = opponent.CarClass.getClassIdentifier();
                    Boolean isPlayerClass = CarData.IsCarClassEqual(opponent.CarClass, playerClass);

                    float correction = 0;

                    Boolean willNeedToStop = opponent.NumPitStops == 0 &&
                        (expectMandatoryStopRaceTime || (!fixedTimeSession && hasMandatoryPitStop && pitWindowEnd > 1 && opponent.CompletedLaps == pitWindowEnd - 1));

                    if (opponent.InPits || willNeedToStop)
                    {
                        // this makes things awkward. He'll be some distance behind where we predicted him to be.
                        // The best we can do here is move him back some proportion of the distance we'd expect to lose by pitting.
                        // The exact amount is based on how far we think his is through his pit stop process.
                        float timeOpponentEnteredPitlane;
                        if (opponentsInPitLane.TryGetValue(opponent.DriverRawName, out timeOpponentEnteredPitlane))
                        {
                            // proportion of stop completed is the proportion of the amount of in-pitlane time this guy has already spent, minus a small correction for the time 
                            // he'll lose on exit (the part of the pit process he's not completed yet), plus a small correction for the time he's already spent on entry (the part
                            // of the pit process he *as* completed). Because these two corrections are the same (we assume entry loss and exit loss are identical),
                            // these will cancel each other out, so we're only interested in the proportion of in-pitlane time he's spent
                            float proportionOfPitstopCompleted = (sessionRunningTime - timeOpponentEnteredPitlane) / playerTimeSpentInPitLane;
                            correction = baseDistanceCorrectionForPittingOpponents * (1 - (Math.Min(1, Math.Max(0, proportionOfPitstopCompleted))));
                        }
                        else if (willNeedToStop)
                        {
                            // he's not in the pits but probably will be
                            correction = baseDistanceCorrectionForPittingOpponents;
                        }
                        else
                        {
                            // errr...
                            correction = baseDistanceCorrectionForPittingOpponents * 0.5f;
                        }
                    }

                    if (isPlayerClass)
                    {
                        // get this guy's predicted total distance travelled.
                        //
                        // If this car is in the pits, he will be further back due to this stop. Move him back by distance we'd expect him to lose
                        float opponentTotalRaceDistance = ((opponent.CompletedLaps) * trackLength) + opponent.DistanceRoundTrack - correction;
                        totalRaceDistances.Add(new Tuple<OpponentData, float>(opponent, opponentTotalRaceDistance));                        
                    }
                    
                    // want to know how far the opponent is from this closestDeltapointPosition right now
                    // fuck me this is a retarded way to do this, but it's late and my brain has given up.

                    // additional hack here - if he's in the pit we need to correct this position
                    float opponentPositionDelta = opponent.DistanceRoundTrack - closestDeltapointPosition - correction;
                    if (opponentPositionDelta < halfTrackLength * -1)
                    {
                        opponentPositionDelta = (trackLength - closestDeltapointPosition) + opponent.DistanceRoundTrack;
                    }
                    else if (opponentPositionDelta > halfTrackLength)
                    {
                        opponentPositionDelta = -1 * ((trackLength - opponent.DistanceRoundTrack) + closestDeltapointPosition);
                    }

                    float absDelta = Math.Abs(opponentPositionDelta);

                    if (opponentPositionDelta > 0)
                    {
                        // he'll be ahead
                        opponentsAhead.Add(new OpponentPositionAtPlayerPitExit(absDelta, isPlayerClass, opponentCarClassId, opponent));
                    }
                    else
                    {
                        opponentsBehind.Add(new OpponentPositionAtPlayerPitExit(absDelta, isPlayerClass, opponentCarClassId, opponent));
                    }                    
                }

                // now work out the race position of the player, from the expected distance covered of all the drivers
                totalRaceDistances.Sort(delegate(Tuple<OpponentData, float> t1, Tuple<OpponentData, float> t2)
                {
                    return t2.Item2.CompareTo(t1.Item2);
                });
                int expectedPlayerRacePosition = 1;
                OpponentData opponentAheadExpected = null;
                foreach (Tuple<OpponentData, float> opponentAndDistance in totalRaceDistances)
                {                    
                    if (opponentAndDistance.Item2 < positionAndTotalDistanceForTimeLoss.Item2)
                    {
                        // we'll be ahead of this car, and he's the car who'll have travelled the furthest
                        // of all the cars we'll be ahead of
                        opponentAheadExpected = opponentAndDistance.Item1;
                        break;
                    }
                    expectedPlayerRacePosition++;
                }
                if (opponentAheadExpected == null)
                {
                    // we're going to be last
                    opponentAheadExpected = totalRaceDistances.Last().Item1;
                }

                if (opponentAheadExpected != null)
                {
                    Console.WriteLine("Derived expected race position P" + expectedPlayerRacePosition + " from opponent " +
                        opponentAheadExpected.DriverRawName + " who's currently in P" + opponentAheadExpected.ClassPosition + 
                        ". Completed laps player: " + lapsCompleted + " opponent: " +
                        opponentAheadExpected.CompletedLaps + " track position player: " + currentDistanceRoundTrack + " opponent: " +
                        opponentAheadExpected.DistanceRoundTrack);
                }

                printS1Positions = true;

                // sort each of these by the delta, smallest first
                opponentsAhead.Sort(delegate(OpponentPositionAtPlayerPitExit d1, OpponentPositionAtPlayerPitExit d2)
                {
                    return d1.predictedDistanceGap.CompareTo(d2.predictedDistanceGap);
                });
                opponentsBehind.Sort(delegate(OpponentPositionAtPlayerPitExit d1, OpponentPositionAtPlayerPitExit d2)
                {
                    return d1.predictedDistanceGap.CompareTo(d2.predictedDistanceGap);
                });

                // phew... now we know who will be in front and who will be behind when we emerge from the pitlane. We also
                // now the expected distance between us and them (in metres) when we emerge.
                return new Strategy.PostPitRacePosition(opponentsAhead, opponentsBehind, expectedPlayerRacePosition);
            }
            else
            {
                Console.WriteLine("Unable to get pitstop prediction - no pitstop time loss data available");
            }
            // oh dear
            return null;
        }

        /**
         * value object containing car class data, predicted post-pit distance gap, and opponent
         */
        public class OpponentPositionAtPlayerPitExit
        {
            // always positive:
            public float predictedDistanceGap;
            public Boolean isPlayerClass;
            public String carClassId;
            public OpponentData opponentData;
            public OpponentPositionAtPlayerPitExit(float predictedDistanceGap, Boolean isPlayerClass, String carClassId, OpponentData opponentData)
            {
                this.predictedDistanceGap = predictedDistanceGap;
                this.isPlayerClass = isPlayerClass;
                this.carClassId = carClassId;
                this.opponentData = opponentData;
            }
        }

        /**
         * Helpfully organised post-pit position data.
         */
        public class PostPitRacePosition
        {
            public List<OpponentPositionAtPlayerPitExit> opponentsFrontAfterStop;
            public List<OpponentPositionAtPlayerPitExit> opponentsBehindAfterStop;
            public int expectedRacePosition;

            public OpponentPositionAtPlayerPitExit opponentClosestAheadAfterStop = null;
            public OpponentPositionAtPlayerPitExit opponentClosestBehindAfterStop = null;

            // this is based on any class
            public float numCarsVeryCloseBehindAfterStop = 0;
            public float numCarsVeryCloseAheadAfterStop = 0;

            public float numCarsCloseBehindAfterStop = 0;
            public float numCarsCloseAheadAfterStop = 0;

            public void print()
            {
                Console.WriteLine("Pistop predition: position " + expectedRacePosition + ", " + numCarsVeryCloseAheadAfterStop + " cars very close ahead, " + 
                    numCarsVeryCloseBehindAfterStop + " cars very close behind, " + numCarsCloseAheadAfterStop + " cars close ahead, " + 
                    numCarsCloseBehindAfterStop + " cars close behind");
                if (opponentClosestAheadAfterStop != null)
                {
                    Console.WriteLine("opponent " + opponentClosestAheadAfterStop.opponentData.DriverRawName + " will be " + opponentClosestAheadAfterStop.predictedDistanceGap + " metres ahead");
                }
                if (opponentClosestBehindAfterStop != null)
                {
                    Console.WriteLine("opponent " + opponentClosestBehindAfterStop.opponentData.DriverRawName + " will be " + opponentClosestBehindAfterStop.predictedDistanceGap + " metres behind");
                }
            }

            public PostPitRacePosition(List<OpponentPositionAtPlayerPitExit> opponentsFrontAfterStop, List<OpponentPositionAtPlayerPitExit> opponentsBehindAfterStop,
                int expectedRacePosition)
            {
                this.expectedRacePosition = expectedRacePosition;
                this.opponentsBehindAfterStop = opponentsBehindAfterStop;
                this.opponentsFrontAfterStop = opponentsFrontAfterStop;

                if (opponentsFrontAfterStop != null && opponentsFrontAfterStop.Count > 0)
                {
                    // note these are sorted by distance (closest first)
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsFrontAfterStop)
                    {
                        if (opponent.predictedDistanceGap < Strategy.distanceAheadToBeConsideredVeryClose)
                        {
                            numCarsVeryCloseAheadAfterStop++;
                        }
                        if (opponent.predictedDistanceGap < Strategy.minDistanceAheadToBeConsideredAFewSeconds)
                        {
                            numCarsCloseAheadAfterStop++;
                        }
                        if (opponent.isPlayerClass) 
                        {
                            if (opponentClosestAheadAfterStop == null)
                            {
                                opponentClosestAheadAfterStop = opponent;
                            }
                        }
                    }
                }
                if (opponentsBehindAfterStop != null && opponentsBehindAfterStop.Count > 0)
                {
                    // note these are sorted by distance (closest first)
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsBehindAfterStop)
                    {
                        if (opponent.predictedDistanceGap < Strategy.distanceBehindToBeConsideredVeryClose)
                        {
                            numCarsVeryCloseBehindAfterStop++;                            
                        }
                        if (opponent.predictedDistanceGap < Strategy.minDistanceBehindToBeConsideredAFewSeconds)
                        {
                            numCarsCloseBehindAfterStop++;
                        }
                        if (opponent.isPlayerClass) 
                        {
                            if (opponentClosestBehindAfterStop == null)
                            {
                                opponentClosestBehindAfterStop = opponent;
                            }
                        }
                    }
                }
            }

            public Boolean willBeInTraffic()
            {
                return numCarsCloseAheadAfterStop > 0 && numCarsCloseBehindAfterStop > 0;
            }
        }

        private void reportPostPitData(Strategy.PostPitRacePosition postPitData)
        {
            List<MessageFragment> fragments = new List<MessageFragment>();
            if (postPitData != null)
            {
                postPitData.print();
                if (postPitData.willBeInTraffic())
                {
                    fragments.Add(MessageFragment.Text(folderExpectTrafficOnPitExit));
                }
                else if (postPitData.numCarsCloseAheadAfterStop == 0 && postPitData.numCarsVeryCloseBehindAfterStop == 0)
                {
                    fragments.Add(MessageFragment.Text(folderClearTrackOnPitExit));
                }
                if (postPitData.expectedRacePosition != -1)
                {
                    fragments.Add(MessageFragment.Text(folderWeShouldEmergeInPosition));
                    fragments.Add(MessageFragment.Integer(postPitData.expectedRacePosition));
                }
                if (postPitData.opponentClosestAheadAfterStop != null && !postPitData.opponentClosestAheadAfterStop.opponentData.InPits)
                {
                    Strategy.opponentFrontToWatchForPitting = postPitData.opponentClosestAheadAfterStop.opponentData.DriverRawName;
                }
                if (postPitData.opponentClosestBehindAfterStop != null && !postPitData.opponentClosestBehindAfterStop.opponentData.InPits)
                {
                    Strategy.opponentBehindToWatchForPitting = postPitData.opponentClosestBehindAfterStop.opponentData.DriverRawName;
                }
                // figure out what to read here
                if (postPitData.opponentClosestAheadAfterStop != null)
                {
                    Boolean canReadOpponentAhead = AudioPlayer.canReadName(postPitData.opponentClosestAheadAfterStop.opponentData.DriverRawName);
                    float gapFront = postPitData.opponentClosestAheadAfterStop.predictedDistanceGap;
                    if (postPitData.opponentClosestBehindAfterStop != null)
                    {
                        Boolean canReadOpponentBehind = AudioPlayer.canReadName(postPitData.opponentClosestBehindAfterStop.opponentData.DriverRawName);
                        // can read 2 driver names, so decide which to read (or both)
                        float gapBehind = postPitData.opponentClosestBehindAfterStop.predictedDistanceGap;
                        if (gapFront < distanceAheadToBeConsideredVeryClose)
                        {
                            if (gapBehind < distanceBehindToBeConsideredVeryClose)
                            {
                                // both cars very close
                                fragments.Add(MessageFragment.Text(folderCloseBetween));
                                fragments.Add(canReadOpponentBehind ? MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData) :
                                    MessageFragment.Text(Position.folderStub + postPitData.opponentClosestBehindAfterStop.opponentData.ClassPosition));
                                fragments.Add(MessageFragment.Text(folderAnd));
                                fragments.Add(canReadOpponentAhead ? MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData) :
                                    MessageFragment.Text(Position.folderStub + postPitData.opponentClosestAheadAfterStop.opponentData.ClassPosition));
                            }
                            else
                            {
                                // car in front very close
                                fragments.Add(MessageFragment.Text(folderJustBehind));
                                fragments.Add(canReadOpponentAhead ? MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData) :
                                     MessageFragment.Text(Position.folderStub + postPitData.opponentClosestAheadAfterStop.opponentData.ClassPosition));
                            }
                        }
                        else if (gapBehind < distanceBehindToBeConsideredVeryClose)
                        {
                            // car behind very close
                            fragments.Add(MessageFragment.Text(folderJustAheadOf));
                            fragments.Add(canReadOpponentBehind ? MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData) :
                                    MessageFragment.Text(Position.folderStub + postPitData.opponentClosestBehindAfterStop.opponentData.ClassPosition));
                        }
                        else if (gapFront < minDistanceAheadToBeConsideredAFewSeconds)
                        {
                            if (gapBehind < minDistanceBehindToBeConsideredAFewSeconds)
                            {
                                // both cars quite close. 
                                // Additional check here. "you'll emerge in P4 between P5 and P3" is going to sound particularly shit, so if we have neither name,
                                // don't announce this
                                if (canReadOpponentAhead || canReadOpponentBehind)
                                {
                                    fragments.Add(MessageFragment.Text(folderBetween));
                                    fragments.Add(canReadOpponentBehind ? MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData) :
                                        MessageFragment.Text(Position.folderStub + postPitData.opponentClosestBehindAfterStop.opponentData.ClassPosition));
                                    fragments.Add(MessageFragment.Text(folderAnd));
                                    fragments.Add(canReadOpponentAhead ? MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData) :
                                        MessageFragment.Text(Position.folderStub + postPitData.opponentClosestAheadAfterStop.opponentData.ClassPosition));
                                }
                            }
                            else
                            {
                                // car in front quite close
                                // Only announce this if we can use the name
                                if (canReadOpponentAhead)
                                {
                                    fragments.Add(MessageFragment.Text(folderBehind));
                                    fragments.Add(MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData));
                                }
                            }
                        }
                        else if (gapBehind < minDistanceBehindToBeConsideredAFewSeconds)
                        {
                            // car behind quite close
                            // Only announce this if we can use the name
                            if (canReadOpponentBehind)
                            {
                                fragments.Add(MessageFragment.Text(folderAheadOf));
                                fragments.Add(MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData));
                            }
                        }
                        else if (gapFront < maxDistanceAheadToBeConsideredAFewSeconds)
                        {
                            // car in front a few seconds away
                            fragments.Add(MessageFragment.Text(folderAFewSecondsBehind));
                            fragments.Add(canReadOpponentAhead ? MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData) :
                                    MessageFragment.Text(Position.folderStub + postPitData.opponentClosestAheadAfterStop.opponentData.ClassPosition));
                        }
                        else if (gapBehind < maxDistanceBehindToBeConsideredAFewSeconds)
                        {
                            // car behind a few seconds away
                            fragments.Add(MessageFragment.Text(folderAFewSecondsAheadOf));
                            fragments.Add(canReadOpponentBehind ? MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData) :
                                    MessageFragment.Text(Position.folderStub + postPitData.opponentClosestBehindAfterStop.opponentData.ClassPosition));
                        }
                    }
                    else
                    {
                        // only have a car in front here
                        if (gapFront < distanceAheadToBeConsideredVeryClose)
                        {
                            // car in front very close
                            fragments.Add(MessageFragment.Text(folderJustBehind));
                            fragments.Add(canReadOpponentAhead ? MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData) :
                                 MessageFragment.Text(Position.folderStub + postPitData.opponentClosestAheadAfterStop.opponentData.ClassPosition));
                        }
                        else if (gapFront < minDistanceAheadToBeConsideredAFewSeconds)
                        {
                            // car in front quite close
                            // Only announce this if we have a name
                            if (canReadOpponentAhead)
                            {
                                fragments.Add(MessageFragment.Text(folderBehind));
                                fragments.Add(MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData));
                            }
                        }
                        else if (gapFront < maxDistanceAheadToBeConsideredAFewSeconds)
                        {
                            // car in front a few seconds away
                            fragments.Add(MessageFragment.Text(folderAFewSecondsBehind));
                            fragments.Add(canReadOpponentAhead ? MessageFragment.Opponent(postPitData.opponentClosestAheadAfterStop.opponentData) :
                                    MessageFragment.Text(Position.folderStub + postPitData.opponentClosestAheadAfterStop.opponentData.ClassPosition));
                        }
                    }
                }
                else if (postPitData.opponentClosestBehindAfterStop != null)
                {
                    // only have a car behind here
                    Boolean canReadOpponentBehind = AudioPlayer.canReadName(postPitData.opponentClosestBehindAfterStop.opponentData.DriverRawName);
                    // can read 2 driver names, so decide which to read (or both)
                    float gapBehind = postPitData.opponentClosestBehindAfterStop.predictedDistanceGap;
                    if (gapBehind < distanceBehindToBeConsideredVeryClose)
                    {
                        // car behind very close
                        fragments.Add(MessageFragment.Text(folderJustAheadOf));
                        fragments.Add(canReadOpponentBehind ? MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData) :
                                MessageFragment.Text(Position.folderStub + postPitData.opponentClosestBehindAfterStop.opponentData.ClassPosition));
                    }
                    else if (gapBehind < minDistanceBehindToBeConsideredAFewSeconds)
                    {
                        // car behind quite close
                        // Only announce this if we have a name
                        if (canReadOpponentBehind)
                        {
                            fragments.Add(MessageFragment.Text(folderAheadOf));
                            fragments.Add(MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData));
                        }
                    }
                    else if (gapBehind < maxDistanceBehindToBeConsideredAFewSeconds)
                    {
                        // car behind a few seconds away
                        fragments.Add(MessageFragment.Text(folderAFewSecondsAheadOf));
                        fragments.Add(canReadOpponentBehind ? MessageFragment.Opponent(postPitData.opponentClosestBehindAfterStop.opponentData) :
                                MessageFragment.Text(Position.folderStub + postPitData.opponentClosestBehindAfterStop.opponentData.ClassPosition));
                    }
                }
            }
            if (fragments.Count > 0)
            {
                if (pitPositionEstimatesRequested)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("pit_stop_position_prediction", fragments, 0, null));
                }
                else
                {
                    audioPlayer.playMessage(new QueuedMessage("pit_stop_position_prediction", fragments, 0, this), 10);
                }
            }
            else if (pitPositionEstimatesRequested)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
        }

        private Boolean enteredLastSector(GameStateData previousGameState, GameStateData currentGameState)
        {
            return currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == sectorCount;
        }

        private Boolean inFinalSector(GameStateData currentGameState)
        {
            if (sectorCount == 2)
            {
                return currentGameState.SessionData.SectorNumber == 2;
            }
            else
            {
                return currentGameState.SessionData.SectorNumber == 3;
            }
        }

        public void respondPracticeStop()
        {
            isTimingPracticeStop = true;
            audioPlayer.playMessageImmediately(new QueuedMessage(folderTimePitstopAcknowledge, 0, null));
        }

        public void respondRace()
        {            
            if (CrewChief.currentGameState == null || CrewChief.currentGameState.SessionData.TrackDefinition == null)
            {
                Console.WriteLine("No data for pit estimate");
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
            else
            {
                Strategy.playPitPositionEstimates = true;
                pitPositionEstimatesRequested = true;
            }
        }

        public override void respond(string voiceMessage)
        {
            // if voice message is 'practice pitstop' or something, set the boolean flag that makes the
            // trigger-loop calculate the time loss
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PRACTICE_PIT_STOP))
            {
                respondPracticeStop();
            }

            // if the voice message is 'where will I emerge' or something, get the PostPitRacePosition object
            // and report some data from it, then set the playedPitPositionEstimatesForThisLap to true
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PLAY_POST_PIT_POSITION_ESTIMATE))
            {
                respondRace();
            }                
        }
    }
}
