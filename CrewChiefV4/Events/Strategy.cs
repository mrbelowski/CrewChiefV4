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

        private Boolean isTimingPracticeStop = false;
        private float gameTimeAtPracticeStopTimerStart = -1;
        private float s3AndS1TimeOnStop = -1;

        // this is set by the box-this-lap macro (eeewwww), and primes this event to play the position
        // estimates when we hit S3
        public static Boolean playPitPositionEstimates = false;

        public static Boolean playedPitPositionEstimatesForThisLap = false;

        // for timing opponent stops
        private Boolean timeOpponentStops = true;
        private HashSet<String> opponentsInPitCycle = new HashSet<string>();
        private DateTime nextPitTimingCheckDue = DateTime.MinValue;

        // for tracking opponent pitting
        private DateTime nextOpponentS3TimingCheckDue = DateTime.MinValue;
        // just used to track when an opponent transitions from S2 to S3
        private HashSet<String> opponentsInS2 = new HashSet<string>();
        // these are static because the opponents event needs to check them:
        public static HashSet<String> opponentsWhoWillExitCloseInFront = new HashSet<string>();
        public static HashSet<String> opponentsWhoWillExitCloseBehind = new HashSet<string>();

        // this is disabled for now - it's unfinished and will probably spam messages
        private Boolean warnAboutOpponentsExitingCloseToPlayer = false;

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
            gameTimeAtPracticeStopTimerStart = -1;
            nextPitTimingCheckDue = DateTime.MinValue;
            nextOpponentS3TimingCheckDue = DateTime.MinValue;
            opponentsInPitCycle.Clear();
            Strategy.playPitPositionEstimates = false;
            Strategy.playedPitPositionEstimatesForThisLap = false;
            timeOpponentStops = true;
            opponentsInS2.Clear();
            Strategy.opponentsWhoWillExitCloseBehind.Clear();
            Strategy.opponentsWhoWillExitCloseInFront.Clear();
        }
                
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState) 
        {
            // can't be arsed to keep checking this:
            if (previousGameState == null)
            {
                return;
            }
            if (currentGameState.SessionData.TrackDefinition == null || 
                ((CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT) &&
                    CrewChiefV4.assetto.ACSGameStateMapper.numberOfSectorsOnTrack != 3))
            {
                // no track data, or track that doesn't have 3 sectors
                return;
            }
            if (currentGameState.SessionData.SessionType == SessionType.Practice && isTimingPracticeStop)
            {
                if (currentGameState.SessionData.SectorNumber == 3 && previousGameState.SessionData.SectorNumber == 2)
                {
                    gameTimeAtPracticeStopTimerStart = currentGameState.SessionData.SessionRunningTime;                    
                }
                else if (currentGameState.SessionData.SectorNumber == 2 && previousGameState.SessionData.SectorNumber == 1)
                {
                    s3AndS1TimeOnStop = currentGameState.SessionData.SessionRunningTime - gameTimeAtPracticeStopTimerStart;
                    Strategy.playerTimeLostForStop = s3AndS1TimeOnStop - (currentGameState.SessionData.PlayerBestLapSector3Time + currentGameState.SessionData.PlayerBestLapSector1Time);
                    gameTimeAtPracticeStopTimerStart = -1;
                    isTimingPracticeStop = false;
                    Strategy.carClassForLastPitstopTiming = currentGameState.carClass;
                    Strategy.trackNameForLastPitstopTiming = currentGameState.SessionData.TrackDefinition.name;

                    audioPlayer.playMessage(new QueuedMessage("pit_stop_cost_estimate", 
                        MessageContents(folderPitStopCostsUsAbout,
                        TimeSpanWrapper.FromSeconds(Strategy.playerTimeLostForStop, Precision.SECONDS)),
                        0, this));
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
                // if we've just requested a pit stop (and the game has this data), trigger the strategy data when we next hit sector3
                else if (playPitPositionEstimates &&
                    !previousGameState.PitData.HasRequestedPitStop && currentGameState.PitData.HasRequestedPitStop)
                {
                    Strategy.playPitPositionEstimates = true;
                }
                else if (!pitBoxPositionCountdown && playPitPositionEstimates &&
                    !Strategy.playedPitPositionEstimatesForThisLap && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
                {
                    Strategy.playPitPositionEstimates = true;
                }
                if (Strategy.playPitPositionEstimates && currentGameState.SessionData.SectorNumber == 3 && !Strategy.playedPitPositionEstimatesForThisLap)
                {
                    Strategy.playPitPositionEstimates = false;
                    // we requested a stop and we're in S3, so gather up the data we'll need and report it
                    //
                    // Note that we need to derive the position estimates here before we start slowing for pit entry
                    Strategy.PostPitRacePosition postRacePositions = getPostPitPositionData(false, currentGameState.SessionData.ClassPosition, currentGameState.SessionData.CompletedLaps,
                            currentGameState.carClass, currentGameState.OpponentData, currentGameState.SessionData.DeltaTime, currentGameState.Now,
                            currentGameState.SessionData.TrackDefinition.name);
                    reportPostPitData(postRacePositions, false);
                }
                if (warnAboutOpponentsExitingCloseToPlayer && currentGameState.Now > nextOpponentS3TimingCheckDue)
                {
                    float expectedPlayerTimeLoss = -1;
                    DateTime nowMinusExpectedLoss = DateTime.MinValue;
                    Boolean doneTimeLossCalc = false;
                    nextOpponentS3TimingCheckDue = currentGameState.Now.AddSeconds(5);
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
                        if (entry.Value.CurrentSectorNumber == 2 && !opponentsInS2.Contains(entry.Key))
                        {
                            opponentsInS2.Add(entry.Key);
                        }
                        else
                        {
                            if (opponentsInS2.Contains(entry.Key) && entry.Value.CurrentSectorNumber == 3)
                            {
                                // this guy has just hit S3, so do the count-back

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
                            opponentsInS2.Remove(entry.Key);
                        }
                    }
                }
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
                                // he's entered s2 since we last checked, so calculate how much time he's lost pitting
                                float bestS3AndS1Time = entry.Value.bestSector3Time + entry.Value.bestSector1Time;
                                // TODO: these game-time values aren't always set - each mapper will need to be updated to ensure they're set
                                // Also note that these are zero-indexed - we want the game time at the end of sector2 on the previous lap and s1 on this lap
                                float lastLapS2EndTime = entry.Value.getLastLapData().GameTimeAtSectorEnd[1];
                                float thisLapS1EndTime = entry.Value.getCurrentLapData().GameTimeAtSectorEnd[0];
                                // only insert data here if we have sane times
                                if (lastLapS2EndTime > 0 && thisLapS1EndTime > 0)
                                {
                                    float lastS3AndS1Time = thisLapS1EndTime - lastLapS2EndTime;
                                    Strategy.opponentsTimeLostForStop[entry.Key] = lastS3AndS1Time - bestS3AndS1Time;
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
        
        private static float getExpectedPlayerTimeLoss(CarData.CarClass carClass, String trackName)
        {
            if (CarData.IsCarClassEqual(carClass, Strategy.carClassForLastPitstopTiming) && trackName == Strategy.trackNameForLastPitstopTiming)
            {
                return Strategy.playerTimeLostForStop;
            }
            return -1;
        }

        private static float getTimeLossEstimate(CarData.CarClass carClass, String trackName, Dictionary<String, OpponentData> opponents, int currentRacePosition)
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
                }
            }
            return timeLossEstimate;
        }

        // all the nasty logic is in this method - refactor?
        private static Strategy.PostPitRacePosition getPostPitPositionData(Boolean fromVoiceCommand, int currentRacePosition, int lapsCompleted,
            CarData.CarClass playerClass, Dictionary<String, OpponentData> opponents, DeltaTime playerDeltaTime, DateTime now,
            String trackName)
        {
            float expectedPlayerTimeLoss = getTimeLossEstimate(playerClass, trackName, opponents, currentRacePosition);
            if (expectedPlayerTimeLoss != -1)
            {
                // now we have a sensible value for the time lost due to the stop, estimate where we'll emerge
                // in order to do this we need to know the real-time time gap to each opponent behind us. 
                DateTime nowMinusExpectedLoss = now.AddSeconds(expectedPlayerTimeLoss * -1);
                // get the track distanceRoundTrack at this point in history
                TimeSpan closestDeltapointTimeDelta = TimeSpan.MaxValue;
                float closestDeltapointPosition = -1;
                foreach (KeyValuePair<float, DateTime> entry in playerDeltaTime.deltaPoints)
                {
                    TimeSpan timeDelta = (nowMinusExpectedLoss - entry.Value).Duration();
                    if (timeDelta < closestDeltapointTimeDelta)
                    {
                        closestDeltapointTimeDelta = timeDelta;
                        closestDeltapointPosition = entry.Key;
                    }
                }
                // this needs to be bounds-checked

                // now we have an estimate of where we were on track this many seconds ago. Get the closest opponents
                // to this position on track.

                List<OpponentPositionAtPlayerPitExit> opponentsAhead = new List<OpponentPositionAtPlayerPitExit>();
                List<OpponentPositionAtPlayerPitExit> opponentsBehind = new List<OpponentPositionAtPlayerPitExit>();

                int playerLapsCompletedAfterStop = 1 + lapsCompleted;

                foreach (OpponentData opponent in opponents.Values)
                {
                    // TODO: major issue here when a car laps us or nearly laps us when we're in the pits - the position estimate
                    // is nonsense. If we emerge just in front of the leader our position will be reported as p0.
                    // Need to find some way to account for lapping or nearly lapping, or else block the position estimate
                    // if we're unsure.
                    String opponentCarClassId = opponent.CarClass.getClassIdentifier();
                    Boolean isPlayerClass = CarData.IsCarClassEqual(opponent.CarClass, playerClass);

                    int opponentLapsCompletedAfterStop = 1 + opponent.CompletedLaps;

                    // if there's any possibility of us being lapped during or shortly after the stop, or the cars around
                    // us might not be on the same lap, don't derive position data
                    Boolean positionDataCanBeUsed = opponent.ClassPosition > currentRacePosition && Math.Abs(opponent.CompletedLaps - lapsCompleted) < 2;

                    float opponentPositionDelta = opponent.DeltaTime.currentDeltaPoint - closestDeltapointPosition;
                    float absDelta = Math.Abs(opponentPositionDelta);
                    if (opponentPositionDelta > 0)
                    {
                        // he'll be ahead
                        opponentsAhead.Add(new OpponentPositionAtPlayerPitExit(absDelta, opponentLapsCompletedAfterStop, isPlayerClass,
                            opponentCarClassId, opponent));
                    }
                    else
                    {
                        // he'll be behind (TODO: work out which way the delta-points lag will bias this)
                        opponentsBehind.Add(new OpponentPositionAtPlayerPitExit(absDelta, opponentLapsCompletedAfterStop, isPlayerClass,
                            opponentCarClassId, opponent));
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
                return new Strategy.PostPitRacePosition(opponentsAhead, opponentsBehind, playerLapsCompletedAfterStop);
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
            public int opponentLapsCompletedAfterStop;
            public Boolean isPlayerClass;
            public String carClassId;
            public OpponentData opponentData;
            public OpponentPositionAtPlayerPitExit(float predictedDistanceGap, int opponentLapsCompletedAfterStop, Boolean isPlayerClass,
                String carClassId, OpponentData opponentData)
            {
                this.predictedDistanceGap = predictedDistanceGap;
                this.opponentLapsCompletedAfterStop = opponentLapsCompletedAfterStop;
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
            public int expectedRacePosition = -1;

            public OpponentPositionAtPlayerPitExit opponentClosestAheadAfterStop = null;
            public OpponentPositionAtPlayerPitExit opponentClosestBehindAfterStop = null;

            // this is based on any class
            public float numCarsVeryCloseBehindAfterStop = 0;
            public float numCarsVeryCloseAheadAfterStop = 0;

            public float numCarsCloseBehindAfterStop = 0;
            public float numCarsCloseAheadAfterStop = 0;

            public PostPitRacePosition(List<OpponentPositionAtPlayerPitExit> opponentsFrontAfterStop, List<OpponentPositionAtPlayerPitExit> opponentsBehindAfterStop,
                int playerLapsCompletedAfterStop)
            {
                this.opponentsBehindAfterStop = opponentsBehindAfterStop;
                this.opponentsFrontAfterStop = opponentsFrontAfterStop;

                // work out which opponents we can use to derive player position. These are drivers who've completed
                // the same (or closest) number of laps as us
                int minLapsDiff = int.MaxValue;
                int closestOpponentLapsCompleted = -1;
                if (opponentsBehindAfterStop != null && opponentsBehindAfterStop.Count > 0)
                {
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsBehindAfterStop)
                    {
                        int lapDiff = Math.Abs(playerLapsCompletedAfterStop - opponent.opponentLapsCompletedAfterStop);
                        if (lapDiff < minLapsDiff)
                        {
                            minLapsDiff = lapDiff;
                            closestOpponentLapsCompleted = opponent.opponentLapsCompletedAfterStop;
                        }
                    }
                }
                if (opponentsFrontAfterStop != null && opponentsFrontAfterStop.Count > 0)
                {
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsFrontAfterStop)
                    {
                        int lapDiff = Math.Abs(playerLapsCompletedAfterStop - opponent.opponentLapsCompletedAfterStop);
                        if (lapDiff < minLapsDiff)
                        {
                            minLapsDiff = lapDiff;
                            closestOpponentLapsCompleted = opponent.opponentLapsCompletedAfterStop;
                        }
                    }
                }

                // derive the expected race position:
                if (opponentsBehindAfterStop != null && opponentsBehindAfterStop.Count > 0)
                {
                    // note these are sorted by distance (closest first)
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsBehindAfterStop)
                    {
                        // we'll be in position - 1 from the closest opponent behind in our class - set this if we haven't already
                        if (closestOpponentLapsCompleted == opponent.opponentLapsCompletedAfterStop && opponent.isPlayerClass && opponentClosestBehindAfterStop == null)
                        {
                            expectedRacePosition = opponent.opponentData.ClassPosition - 1;
                            opponentClosestBehindAfterStop = opponent;
                        }
                        if (opponent.predictedDistanceGap < Strategy.distanceBehindToBeConsideredVeryClose)
                        {
                            numCarsVeryCloseBehindAfterStop++;                            
                        }
                        if (opponent.predictedDistanceGap < Strategy.minDistanceAheadToBeConsideredAFewSeconds)
                        {
                            numCarsCloseBehindAfterStop++;
                        }
                    }
                }
                // if there's noone in our class behind us, get it from the cars expected to be in front
                if (opponentsFrontAfterStop != null && opponentsFrontAfterStop.Count > 0)
                {
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsFrontAfterStop)
                    {
                        // we'll be in position + 1 from the closest opponent behind in our class - set this if we haven't already
                        if (opponent.isPlayerClass && opponentClosestAheadAfterStop == null)
                        {
                            opponentClosestAheadAfterStop = opponent;
                            // do we need this?
                            if (closestOpponentLapsCompleted == opponent.opponentLapsCompletedAfterStop && expectedRacePosition == -1)
                            {
                                expectedRacePosition = opponent.opponentData.ClassPosition + 1;
                            }
                        }
                        if (opponent.predictedDistanceGap < Strategy.distanceAheadToBeConsideredVeryClose)
                        {
                            numCarsVeryCloseAheadAfterStop++;
                        }
                        if (opponent.predictedDistanceGap < Strategy.minDistanceAheadToBeConsideredAFewSeconds)
                        {
                            numCarsCloseAheadAfterStop++;
                        }
                    }
                }
            }

            public Boolean willBeInTraffic()
            {
                return numCarsCloseAheadAfterStop > 0 && numCarsCloseBehindAfterStop > 0;
            }
        }

        private void reportPostPitData(Strategy.PostPitRacePosition postPitData, Boolean immediate)
        {
            List<MessageFragment> fragments = new List<MessageFragment>();
            if (postPitData != null)
            {
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
                if (immediate)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("pit_stop_position_prediction", fragments, 0, null));
                }
                else
                {
                    audioPlayer.playMessage(new QueuedMessage("pit_stop_position_prediction", fragments, 0, this));
                }
            }
            else if (immediate)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
        }

        public override void respond(string voiceMessage)
        {
            // if voice message is 'practice pitstop' or something, set the boolean flag that makes the
            // trigger-loop calculate the time loss
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PRACTICE_PIT_STOP))
            {
                if (CrewChief.currentGameState.SessionData.PlayerBestLapSector1Time > 0 && CrewChief.currentGameState.SessionData.PlayerBestLapSector3Time > 0)
                {
                    isTimingPracticeStop = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderTimePitstopAcknowledge, 0, null));
                }
                else if ((CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT) &&
                    CrewChiefV4.assetto.ACSGameStateMapper.numberOfSectorsOnTrack != 3)
                {
                    // unable to use this track for pit benchmarks as it doesn't have 3 sectors
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNo, 0, null));
                }
                else
                {
                    // can't get a benchmark as we have no best lap data in the session
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderNeedMoreLapData, 0, null));
                }
            }

            // if the voice message is 'where will I emerge' or something, get the PostPitRacePosition object
            // and report some data from it, then set the playedPitPositionEstimatesForThisLap to true
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PLAY_POST_PIT_POSITION_ESTIMATE))
            {
                Strategy.PostPitRacePosition postPitPosition = Strategy.getPostPitPositionData(true, CrewChief.currentGameState.SessionData.ClassPosition,
                    CrewChief.currentGameState.SessionData.CompletedLaps, CrewChief.currentGameState.carClass, CrewChief.currentGameState.OpponentData, 
                CrewChief.currentGameState.SessionData.DeltaTime, CrewChief.currentGameState.Now, CrewChief.currentGameState.SessionData.TrackDefinition.name);
                // do some reporting
                reportPostPitData(postPitPosition, true);
                playedPitPositionEstimatesForThisLap = true;
           }                
        }
    }
}
