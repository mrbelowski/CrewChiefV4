using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class Strategy : AbstractEvent
    {
        // less than 70m => 'just ahead' or 'just behind'
        private static float distanceBehindToBeConsideredClose = 70;
        private static float distanceAheadToBeConsideredClose = 70;

        // assume speed at pit exit is 70m/s (mad, I know), so any distance > 200 and < 500 is a few seconds.
        // < 200 means we'll give a simple non-commital ahead / behind
        private static float minDistanceBehindToBeConsideredAFewSeconds= 200;
        private static float minDistanceAheadToBeConsideredAFewSeconds = 200;
        private static float maxDistanceBehindToBeConsideredAFewSeconds = 500;
        private static float maxDistanceAheadToBeConsideredAFewSeconds = 500;

        private static String folderExpectTrafficOnPitExit = "strategy/expect_traffic_on_pit_exit";
        private static String folderClearTrackOnPitExit = "strategy/expect_clear_track_on_pit_exit";
        private static String folderWeShouldEmergeInPosition = "strategy/we_should_emerge_in_position";
        private static String folderBetween = "strategy/between";
        private static String folderAnd = "strategy/and";
        private static String folderJustAheadOf = "strategy/just_ahead_of";
        private static String folderJustBehind = "strategy/just_behind";
        private static String folderAheadOf = "strategy/ahead_of";
        private static String folderBehind = "strategy/behind";

        // these are a bit tricky as we only know the separation distance, not time
        private static String folderAFewSecondsAheadOf = "strategy/a_few_seconds_ahead_of";
        private static String folderAFewSecondsBehind = "strategy/a_few_seconds_behind";


        // may be timed during practice.
        // Need to be careful here to ensure this is applicable to the session we've actually entered
        private float playerTimeLostForStop = -1;

        private Dictionary<String, float> opponentsTimeLostForStop = new Dictionary<string, float>();
        private Boolean isTimingPracticeStop = false;
        private float gameTimeAtPracticeStopTimerStart = -1;
        private float s3AndS1TimeOnStop = -1;

        // this is set by the box-this-lap macro (eeewwww), and primes this event to play the position
        // estimates when we hit S3
        public static Boolean playPitPositionEstimates = false;

        public static Boolean playedPitPositionEstimatesForThisLap = false;

        private HashSet<String> opponentsInPitCycle = new HashSet<string>();

        private DateTime nextPitTimingCheckDue = DateTime.MinValue;

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
            isTimingPracticeStop = false;
            gameTimeAtPracticeStopTimerStart = -1;
            nextPitTimingCheckDue = DateTime.MinValue;
            opponentsInPitCycle.Clear();
            playPitPositionEstimates = false;
            playedPitPositionEstimatesForThisLap = false;
        }
                
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState) 
        {
            // can't be arsed to keep checking this:
            if (previousGameState == null)
            {
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
                    gameTimeAtPracticeStopTimerStart = -1;
                    isTimingPracticeStop = false;

                    // TODO: if we have enough laps, we can do the calculation here and announce the result ("we think a pitstop
                    // costs about X seconds overall"), but this is not so important
                }
                // nothing else to do unless we're in race mode
            }
            else if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                if (currentGameState.SessionData.IsNewLap)
                {
                    playedPitPositionEstimatesForThisLap = false;
                }
                if (playPitPositionEstimates && currentGameState.SessionData.SectorNumber == 3 && !playedPitPositionEstimatesForThisLap)
                {
                    playPitPositionEstimates = false;
                    // we requested a stop and we're in S3, so gather up the data we'll need and report it
                    //
                    // Note that we need to derive the position estimates here before we start slowing for pit entry
                    Strategy.PostPitRacePosition postRacePositions = getPostPitPositionData(false, currentGameState.SessionData.ClassPosition,
                            currentGameState.carClass, currentGameState.OpponentData, currentGameState.SessionData.DeltaTime, currentGameState.Now);
                    if (postRacePositions != null && postRacePositions.expectedRacePosition != -1)
                    {
                        // we have some estimated data about where we'll be after our stop, so report it
                        reportPostPitData(postRacePositions);
                    }
                }
                if (nextPitTimingCheckDue > currentGameState.Now)
                {
                    nextPitTimingCheckDue = currentGameState.Now.AddSeconds(5);
                    // update opponent time lost
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        // only interested in opponent pit times for our class
                        if (CarData.IsCarClassEqual(entry.Value.CarClass, currentGameState.carClass))
                        {
                            if (opponentsInPitCycle.Contains(entry.Key) && entry.Value.CurrentSectorNumber == 2)
                            {
                                // he's entered s2 since we last checked, so calculate how much time he's lost pitting
                                float bestS3AndS1Time = entry.Value.bestSector3Time + entry.Value.bestSector1Time;
                                // TODO: these game-time values aren't always set - each mapper will need to be updated to ensure they're set
                                float lastS3AndS1Time = entry.Value.getCurrentLapData().GameTimeAtSectorEnd[1] - entry.Value.getLastLapData().GameTimeAtSectorEnd[2];
                                opponentsTimeLostForStop[entry.Key] = lastS3AndS1Time - bestS3AndS1Time;
                                opponentsInPitCycle.Remove(entry.Key);
                            }
                            else if (entry.Value.InPits)
                            {
                                opponentsInPitCycle.Add(entry.Key);
                            }
                        }
                    }
                }
            }
        }

        // all the nasty logic is in this method - refactor?
        private Strategy.PostPitRacePosition getPostPitPositionData(Boolean fromVoiceCommand, int currentRacePosition,
            CarData.CarClass playerClass, Dictionary<String, OpponentData> opponents, DeltaTime playerDeltaTime, DateTime now)
        {
            float expectedPlayerTimeLoss = -1;
            if (playerTimeLostForStop == -1)
            {
                if (opponentsTimeLostForStop.Count != 0) 
                {
                    // select the best opponent to compare with
                    foreach (KeyValuePair<String, float> entry in opponentsTimeLostForStop)
                    {
                        if (Math.Abs(opponents[entry.Key].ClassPosition - currentRacePosition) < 2)
                        {
                            // he'll do - probably need a better way to decide this
                            expectedPlayerTimeLoss = entry.Value;
                        }
                    }
                }
            }
            else
            {
                expectedPlayerTimeLoss = playerTimeLostForStop;
            }
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
                    TimeSpan timeDelta = nowMinusExpectedLoss - entry.Value;
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

                // TODO: as we'll be notifying about clear-air or coming out into traffic, we'll need all the opponents
                // here, maybe as a Dictionary keyed on car class ID. This allows us to report info for same-class stuff
                // as well as general traffic warnings

                foreach (OpponentData opponent in opponents.Values)
                {
                    String opponentCarClassId = opponent.CarClass.getClassIdentifier();
                    
                    float opponentPositionDelta = opponent.DeltaTime.currentDeltaPoint - closestDeltapointPosition;
                    float absDelta = Math.Abs(opponentPositionDelta);
                    if (opponentPositionDelta > 0)
                    {
                        // he'll be ahead
                        opponentsAhead.Add(new OpponentPositionAtPlayerPitExit(absDelta, true, CarData.IsCarClassEqual(opponent.CarClass, playerClass),
                            opponentCarClassId, opponent));
                    }
                    else
                    {
                        // he'll be behind (TODO: work out which way the delta-points lag will bias this)
                        opponentsAhead.Add(new OpponentPositionAtPlayerPitExit(absDelta, false, CarData.IsCarClassEqual(opponent.CarClass, playerClass),
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
                return new Strategy.PostPitRacePosition(opponentsAhead, opponentsBehind);
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
            // probably don't need this:
            public Boolean isAheadOfPlayer;
            public Boolean isPlayerClass;
            public String carClassId;
            public OpponentData opponentData;
            public OpponentPositionAtPlayerPitExit(float predictedDistanceGap, Boolean isAheadOfPlayer, Boolean isPlayerClass,
                String carClassId, OpponentData opponentData)
            {
                this.predictedDistanceGap = predictedDistanceGap;
                this.isAheadOfPlayer = isAheadOfPlayer;
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

            // these are player class only
            public List<OpponentData> opponentsCloseBehindAfterStop = new List<OpponentData>();
            public List<OpponentData> opponentsCloseAheadAfterStop = new List<OpponentData>();

            // this is based on any class
            public float numCarsCloseBehindAfterStop = 0;
            public float numCarsCloseAheadAfterStop = 0;

            public float numCarsFairlyCloseAheadAfterStop = 0;

            public PostPitRacePosition(List<OpponentPositionAtPlayerPitExit> opponentsFrontAfterStop, List<OpponentPositionAtPlayerPitExit> opponentsBehindAfterStop)
            {
                this.opponentsBehindAfterStop = opponentsBehindAfterStop;
                this.opponentsFrontAfterStop = opponentsFrontAfterStop;

                // derive the expected race position:
                if (opponentsBehindAfterStop != null && opponentsBehindAfterStop.Count > 0)
                {                    
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsBehindAfterStop)
                    {
                        // we'll be in position - 1 from the closest opponent behind in our class - set this if we haven't already
                        if (expectedRacePosition == -1 && opponent.isPlayerClass)
                        {
                            expectedRacePosition = opponent.opponentData.ClassPosition - 1;
                        }
                        if (opponent.predictedDistanceGap < Strategy.distanceBehindToBeConsideredClose)
                        {
                            numCarsCloseBehindAfterStop++;
                            if (opponent.isPlayerClass)
                            {
                                opponentsCloseBehindAfterStop.Add(opponent.opponentData);
                            }
                        }
                    }
                }
                // if there's noone in our class behind us, get it from the cars expected to be in front
                if (opponentsFrontAfterStop != null && opponentsFrontAfterStop.Count > 0)
                {
                    foreach (OpponentPositionAtPlayerPitExit opponent in opponentsFrontAfterStop)
                    {
                        // we'll be in position + 1 from the closest opponent behind in our class - set this if we haven't already
                        if (expectedRacePosition == -1 && opponent.isPlayerClass)
                        {
                            expectedRacePosition = opponent.opponentData.ClassPosition + 1;
                        }
                        if (opponent.predictedDistanceGap < Strategy.distanceAheadToBeConsideredClose)
                        {
                            numCarsCloseAheadAfterStop++;
                            if (opponent.isPlayerClass)
                            {
                                opponentsCloseAheadAfterStop.Add(opponent.opponentData);
                            }
                        }
                        else if (opponent.predictedDistanceGap < Strategy.minDistanceAheadToBeConsideredAFewSeconds)
                        {
                            numCarsFairlyCloseAheadAfterStop++;
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
            if (postPitData.willBeInTraffic())
            {
                fragments.Add(MessageFragment.Text(folderExpectTrafficOnPitExit));
            }
            else if (postPitData.numCarsCloseAheadAfterStop == 0 && postPitData.numCarsFairlyCloseAheadAfterStop == 0 &&
                postPitData.numCarsCloseBehindAfterStop == 0)
            {
                fragments.Add(MessageFragment.Text(folderClearTrackOnPitExit));
            }
            if (postPitData.expectedRacePosition != -1)
            {
                fragments.Add(MessageFragment.Text(folderWeShouldEmergeInPosition));
                fragments.Add(MessageFragment.Integer(postPitData.expectedRacePosition));
                
            }
        }

        public override void respond(string voiceMessage)
        {
            // if voice message is 'practice pitstop' or something, set the boolean flag that makes the
            // trigger-loop calculate the time loss
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PRACTICE_PIT_STOP))
            {
                isTimingPracticeStop = true;
            }

            // if the voice message is 'where will I emerge' or something, get the PostPitRacePosition object
            // and report some data from it, then set the playedPitPositionEstimatesForThisLap to true
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PLAY_POST_PIT_POSITION_ESTIMATE))
            {
                Strategy.PostPitRacePosition postPitPosition = getPostPitPositionData(true, CrewChief.currentGameState.SessionData.ClassPosition,
                    CrewChief.currentGameState.carClass, CrewChief.currentGameState.OpponentData, CrewChief.currentGameState.SessionData.DeltaTime,
                    CrewChief.currentGameState.Now);
                // do some reporting
                if (postPitPosition != null && postPitPosition.expectedRacePosition != -1)
                {
                    reportPostPitData(postPitPosition);
                    playedPitPositionEstimatesForThisLap = true;
                }
            }                
        }
    }
}
