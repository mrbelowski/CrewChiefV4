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


        // may be timed during practice.
        // Need to be careful here to ensure this is applicable to the session we've actually entered
        private static float playerTimeLostForStop = -1;
        private static CarData.CarClass carClassForLastPitstopTiming;
        private static String trackNameForLastPitstopTiming;
        private static Dictionary<String, float> opponentsTimeLostForStop = new Dictionary<string, float>();

        public static Boolean isTimingPracticeStop = false;
        private Boolean hasPittedDuringPracticeStopProcess = false;
        private float gameTimeWhenEnteringLastSectorInPractice = -1;
        private float lastAndFirstSectorTimesOnStop = -1;

        // this is set by the box-this-lap macro (eeewwww), and primes this event to play the position
        // estimates when we hit the final sector
        public static Boolean playPitPositionEstimates = false;
        private static Boolean pitPositionEstimatesRequested = false;

        public static Boolean playedPitPositionEstimatesForThisLap = false;

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

        // this is disabled for now - it's unfinished and will probably spam messages
        private Boolean warnAboutOpponentsExitingCloseToPlayer = false;

        private int sectorCount = 3;

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
            Strategy.opponentsTimeLostForStop.Clear();
            isTimingPracticeStop = false;
            gameTimeWhenEnteringLastSectorInPractice = -1;
            nextPitTimingCheckDue = DateTime.MinValue;
            nextOpponentFinalSectorTimingCheckDue = DateTime.MinValue;
            opponentsInPitCycle.Clear();
            Strategy.playPitPositionEstimates = false;
            Strategy.playedPitPositionEstimatesForThisLap = false;
            timeOpponentStops = true;
            opponentsInPenultimateSector.Clear();
            Strategy.opponentsWhoWillExitCloseBehind.Clear();
            hasPittedDuringPracticeStopProcess = false;
            Strategy.opponentsWhoWillExitCloseInFront.Clear();
            Strategy.pitPositionEstimatesRequested = false;
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
            if (currentGameState.SessionData.SessionType == SessionType.Practice)
            {
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
                        }
                    }
                    else if (currentGameState.SessionData.SectorNumber == 2 && previousGameState.SessionData.SectorNumber == 1 &&
                        hasPittedDuringPracticeStopProcess && gameTimeWhenEnteringLastSectorInPractice != -1)
                    {
                        lastAndFirstSectorTimesOnStop = currentGameState.SessionData.SessionRunningTime - gameTimeWhenEnteringLastSectorInPractice;

                        if (sectorCount == 2)
                        {
                            Strategy.playerTimeLostForStop = lastAndFirstSectorTimesOnStop - (currentGameState.SessionData.PlayerBestLapSector2Time + currentGameState.SessionData.PlayerBestLapSector1Time);
                        }
                        else
                        {
                            Strategy.playerTimeLostForStop = lastAndFirstSectorTimesOnStop - (currentGameState.SessionData.PlayerBestLapSector3Time + currentGameState.SessionData.PlayerBestLapSector1Time);
                        }
                        gameTimeWhenEnteringLastSectorInPractice = -1;
                        isTimingPracticeStop = false;
                        hasPittedDuringPracticeStopProcess = false;
                        Strategy.carClassForLastPitstopTiming = currentGameState.carClass;
                        Strategy.trackNameForLastPitstopTiming = currentGameState.SessionData.TrackDefinition.name;

                        Console.WriteLine("Practice pitstop has cost us " + Strategy.playerTimeLostForStop + " seconds");
                        audioPlayer.playMessage(new QueuedMessage("pit_stop_cost_estimate",
                            MessageContents(folderPitStopCostsUsAbout,
                            TimeSpanWrapper.FromSeconds(Strategy.playerTimeLostForStop, Precision.SECONDS)),
                            0, this));
                    }
                }
                // nothing else to do unless we're in race mode
            }
            else if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                // if we've timed our pitstop in practice, don't search for opponent stop times
                if (currentGameState.SessionData.JustGoneGreen)
                {
                    timeOpponentStops = getExpectedPlayerTimeLoss(currentGameState.carClass, currentGameState.SessionData.TrackDefinition.name) == -1;
                }
                if (currentGameState.SessionData.IsNewLap)
                {
                    Strategy.playedPitPositionEstimatesForThisLap = false;
                }
                // if we've just requested a pit stop (and the game has this data), trigger the strategy data when we next hit the final sector
                else if (playPitPositionEstimates &&
                    !previousGameState.PitData.HasRequestedPitStop && currentGameState.PitData.HasRequestedPitStop)
                {
                    Strategy.playPitPositionEstimates = true;
                    Strategy.pitPositionEstimatesRequested = false;
                }
                // if we've just entered the pitlane and the pit countdown is disabled, and we don't have a penalty, trigger
                // the strategy stuff
                else if (!pitBoxPositionCountdown && !currentGameState.PenaltiesData.HasDriveThrough && !currentGameState.PenaltiesData.HasStopAndGo &&
                    !Strategy.playedPitPositionEstimatesForThisLap && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
                {
                    Strategy.playPitPositionEstimates = true;
                    Strategy.pitPositionEstimatesRequested = false;
                }
                if (Strategy.playPitPositionEstimates &&
                    (Strategy.pitPositionEstimatesRequested || (inFinalSector(currentGameState) && !Strategy.playedPitPositionEstimatesForThisLap)))
                {
                    Strategy.playPitPositionEstimates = false;
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
                            currentGameState.PositionAndMotionData.DistanceRoundTrack, bestLapTime);
                    reportPostPitData(postRacePositions);
                    playedPitPositionEstimatesForThisLap = true;
                }



                //-------------------------------
                // this block is currently unused
                if (warnAboutOpponentsExitingCloseToPlayer && currentGameState.Now > nextOpponentFinalSectorTimingCheckDue)
                {
                    float expectedPlayerTimeLoss = -1;
                    DateTime nowMinusExpectedLoss = DateTime.MinValue;
                    Boolean doneTimeLossCalc = false;
                    nextOpponentFinalSectorTimingCheckDue = currentGameState.Now.AddSeconds(5);
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        if (entry.Value.IsNewLap)
                        {
                            opponentsWhoWillExitCloseBehind.Remove(entry.Key);
                            opponentsWhoWillExitCloseInFront.Remove(entry.Key);
                        }
                        else if (entry.Value.JustEnteredPits)
                        {
                            if (opponentsWhoWillExitCloseInFront.Contains(entry.Key))
                            {
                                // this guy has just entered the pit and we predict he'll exit just in front of us
                                // TODO: report this, ensure we don't report more than 1 in a short time
                            }
                            opponentsWhoWillExitCloseInFront.Remove(entry.Key);
                            if (opponentsWhoWillExitCloseBehind.Contains(entry.Key))
                            {
                                // this guy has just entered the pit and we predict he'll exit just behind us
                                // TODO: report this, ensure we don't report more than 1 in a short time
                            }
                            opponentsWhoWillExitCloseBehind.Remove(entry.Key);
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
                                // this guy has just hit final sector, so do the count-back

                                // lazily obtain the expected time loss
                                if (!doneTimeLossCalc)
                                {
                                    expectedPlayerTimeLoss = getTimeLossEstimate(currentGameState.carClass, currentGameState.SessionData.TrackDefinition.name,
                                        currentGameState.OpponentData, currentGameState.SessionData.ClassPosition);
                                    nowMinusExpectedLoss = currentGameState.Now.AddSeconds(expectedPlayerTimeLoss * -1);
                                    doneTimeLossCalc = true;
                                }
                                if (expectedPlayerTimeLoss == -1)
                                {
                                    // no loss data, can't continue
                                    break;
                                }
                                // get his track distanceRoundTrack at this point in history
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
                                if (expectedDistanceToPlayerOnPitExit > 0 && absGap < distanceAheadToBeConsideredVeryClose)
                                {
                                    // he'll come out of the pits right in front of us
                                    opponentsWhoWillExitCloseInFront.Add(entry.Key);
                                }
                                else if (expectedDistanceToPlayerOnPitExit < 0 && absGap < distanceBehindToBeConsideredVeryClose)
                                {
                                    // he'll come out right behind us
                                    opponentsWhoWillExitCloseBehind.Add(entry.Key);
                                }
                            }
                            opponentsInPenultimateSector.Remove(entry.Key);
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
                                
                                // TODO: these game-time values aren't always set - each mapper will need to be updated to ensure they're set
                                // Also note that these are zero-indexed - we want the game time at the end of sector2 on the previous lap and s1 on this lap
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
                                        Strategy.opponentsTimeLostForStop[entry.Key] = timeLost;
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
            if (CarData.IsCarClassEqual(carClass, Strategy.carClassForLastPitstopTiming) && trackName == Strategy.trackNameForLastPitstopTiming)
            {
                return Strategy.playerTimeLostForStop;
            }
            return -1;
        }

        private float getTimeLossEstimate(CarData.CarClass carClass, String trackName, Dictionary<String, OpponentData> opponents, int currentRacePosition)
        {
            float timeLossEstimate = getExpectedPlayerTimeLoss(carClass, trackName);
            if (timeLossEstimate == -1)
            {
                if (Strategy.opponentsTimeLostForStop.Count != 0)
                {                    
                    int pittedOpponentPositionDiff = int.MaxValue;
                    // select the best opponent to compare with
                    foreach (KeyValuePair<String, float> entry in Strategy.opponentsTimeLostForStop)
                    {
                        int positionDiff = Math.Abs(opponents[entry.Key].ClassPosition - currentRacePosition);
                        if (positionDiff < pittedOpponentPositionDiff)
                        {
                            timeLossEstimate = entry.Value;
                            pittedOpponentPositionDiff = positionDiff;
                        }
                    }
                    Console.WriteLine("Got pitstop time loss estimate from opponent stop times - expect to lose " + timeLossEstimate + " seconds");
                }
            }
            else
            {
                Console.WriteLine("Got pitstop time loss estimate from practice stop - expect to lose " + timeLossEstimate + " seconds");
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
            String trackName, float trackLength, float currentDistanceRoundTrack, float bestLapTime)
        {
            float halfTrackLength = trackLength / 2;
            // check we have deltapoints first
            if (playerDeltaTime == null || playerDeltaTime.deltaPoints == null || playerDeltaTime.deltaPoints.Count == 0)
            {
                Console.WriteLine("No usable deltapoints object, can't derive post-pit positions");
                return null;
            }

            float expectedPlayerTimeLoss = getTimeLossEstimate(playerClass, trackName, opponents, currentRacePosition);
            if (expectedPlayerTimeLoss != -1)
            {
                // now we have a sensible value for the time lost due to the stop, estimate where we'll emerge
                // in order to do this we need to know the total race distance at the point where we'd have been
                // expectedPlayerTimeLoss seconds ago
                Tuple<float, float> positionAndTotalDistanceForTimeLoss = getPositionAndTotalDistanceForTimeLoss(
                    expectedPlayerTimeLoss, trackLength, currentDistanceRoundTrack, bestLapTime, lapsCompleted, now, playerDeltaTime);

                float closestDeltapointPosition = positionAndTotalDistanceForTimeLoss.Item1;

                List<OpponentPositionAtPlayerPitExit> opponentsAhead = new List<OpponentPositionAtPlayerPitExit>();
                List<OpponentPositionAtPlayerPitExit> opponentsBehind = new List<OpponentPositionAtPlayerPitExit>();

                int expectedPlayerRacePosition = 1;
                float closestTotalRaceDistance = float.MaxValue;

                foreach (OpponentData opponent in opponents.Values)
                {
                    String opponentCarClassId = opponent.CarClass.getClassIdentifier();
                    Boolean isPlayerClass = CarData.IsCarClassEqual(opponent.CarClass, playerClass);

                    if (isPlayerClass)
                    {
                        // work out where we'll be by inspecting the predicted total race distance of the nearest opponent car
                        float opponentTotalRaceDistance = (opponent.CompletedLaps) * trackLength + opponent.DistanceRoundTrack;
                        float raceDistanceDiff = positionAndTotalDistanceForTimeLoss.Item2 - opponentTotalRaceDistance;
                        float absRaceDistanceDiff = Math.Abs(raceDistanceDiff);
                        if (absRaceDistanceDiff < closestTotalRaceDistance)
                        {
                            closestTotalRaceDistance = absRaceDistanceDiff;
                            if (raceDistanceDiff < 0)
                            {
                                // this guy will be in front of us. If he was behind us before our stop, we'll have exchanged positions
                                if (opponent.ClassPosition > currentRacePosition)
                                {
                                    expectedPlayerRacePosition = opponent.ClassPosition;
                                }
                                else
                                {
                                    expectedPlayerRacePosition = opponent.ClassPosition + 1;
                                }
                            }
                            else
                            {
                                // this guy will be in behind us.
                                expectedPlayerRacePosition = opponent.ClassPosition - 1;
                            }
                        }
                    }
                    // want to know how far the opponent is from this closestDeltapointPosition right now
                    // fuck me this is a retarded way to do this, but it's late and my brain has given up
                    float opponentPositionDelta = opponent.DistanceRoundTrack - closestDeltapointPosition;
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
                        // he'll be behind (TODO: work out which way the delta-points lag will bias this)
                        opponentsBehind.Add(new OpponentPositionAtPlayerPitExit(absDelta, isPlayerClass, opponentCarClassId, opponent));
                    }                    
                }
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
                if (Strategy.pitPositionEstimatesRequested)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("pit_stop_position_prediction", fragments, 0, null));
                }
                else
                {
                    audioPlayer.playMessage(new QueuedMessage("pit_stop_position_prediction", fragments, 0, this));
                }
            }
            else if (Strategy.pitPositionEstimatesRequested)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
        }

        private Boolean enteredLastSector(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (sectorCount == 2)
            {
                return currentGameState.SessionData.SectorNumber == 2 && previousGameState.SessionData.SectorNumber == 1;
            }
            else
            {
                return currentGameState.SessionData.SectorNumber == 3 && previousGameState.SessionData.SectorNumber == 2;
            }
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
            if (CrewChief.currentGameState.SessionData.PlayerBestLapSector1Time > 0 && CrewChief.currentGameState.SessionData.PlayerBestLapSector3Time > 0)
            {
                isTimingPracticeStop = true;
                audioPlayer.playMessageImmediately(new QueuedMessage(folderTimePitstopAcknowledge, 0, null));
            }
            else
            {
                // can't get a benchmark as we have no best lap data in the session
                audioPlayer.playMessageImmediately(new QueuedMessage(folderNeedMoreLapData, 0, null));
            }
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
                Strategy.pitPositionEstimatesRequested = true;
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
