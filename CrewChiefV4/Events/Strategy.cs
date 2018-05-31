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
        // may be timed during practice.
        // Need to be careful here to ensure this is applicable to the session we've actually entered
        private float playerTimeLostForStop = -1;

        private Dictionary<String, float> opponentsTimeLostForStop = new Dictionary<string, float>();
        private Boolean isTimingPracticeStop = false;
        private float gameTimeAtPracticeStopTimerStart = -1;
        private float s3AndS1TimeOnStop = -1;

        // when making 'box this lap' request or from an explict command, we may report post pit estimates - set this flag
        private Boolean gavePostPitDataThisLap = false;

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
                }
                // nothing else to do unless we're in race mode
            }
            else if (currentGameState.SessionData.SessionType == SessionType.Race)
            {
                if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.IsNewLap)
                {
                    gavePostPitDataThisLap = false;
                }
                if (currentGameState.PitData.InPitlane && !previousGameState.PitData.InPitlane)
                {
                    // we've entered the pit. If we've not already given a call, provide the post-pit estimate here
                    Strategy.PostPitRacePosition postRacePositions = getPostPitPositionData(false, currentGameState.SessionData.ClassPosition,
                        currentGameState.carClass, currentGameState.OpponentData, currentGameState.SessionData.DeltaTime, currentGameState.Now);
                    if (postRacePositions != null && postRacePositions.expectedRacePosition != -1)
                    {
                        // we have some estimated data about where we'll be after our stop, so report it
                    }
                }
                if (nextPitTimingCheckDue > currentGameState.Now)
                {
                    nextPitTimingCheckDue = currentGameState.Now.AddSeconds(5);
                    // update opponent time lost
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
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

                List<Tuple<float, OpponentData>> opponentsAhead = new List<Tuple<float, OpponentData>>();
                List<Tuple<float, OpponentData>> opponentsBehind = new List<Tuple<float, OpponentData>>();

                String playerClassId = playerClass.getClassIdentifier();
                foreach (OpponentData opponent in opponents.Values)
                {
                    if (opponent.CarClass.getClassIdentifier() == playerClassId)
                    {
                        float opponentPositionDelta = opponent.DeltaTime.currentDeltaPoint - closestDeltapointPosition;
                        float absDelta = Math.Abs(opponentPositionDelta);
                        if (opponentPositionDelta > 0)
                        {
                            // he'll be ahead
                            opponentsAhead.Add(new Tuple<float, OpponentData>(absDelta, opponent));
                        }
                        else
                        {
                            // he'll be behind (TODO: work out which way the delta-points lag will bias this)
                            opponentsBehind.Add(new Tuple<float, OpponentData>(absDelta, opponent));
                        }
                    }
                }
                // sort each of these by the delta, smallest first
                opponentsAhead.Sort(delegate(Tuple<float, OpponentData> t1, Tuple<float, OpponentData> t2)
                {
                    return t1.Item1.CompareTo(t2.Item2);
                });
                opponentsBehind.Sort(delegate(Tuple<float, OpponentData> t1, Tuple<float, OpponentData> t2)
                {
                    return t1.Item1.CompareTo(t2.Item2);
                });

                // phew... now we know who will be in front and who will be behind when we emerge from the pitlane. We also
                // now the expected distance between us and them (in metres) when we emerge.
                return new Strategy.PostPitRacePosition(opponentsAhead, opponentsBehind);
            }
            // oh dear
            return null;
        }

        public class PostPitRacePosition
        {
            public List<Tuple<float, OpponentData>> opponentsFrontAfterStop;
            public List<Tuple<float, OpponentData>> opponentsBehindAfterStop;
            public int expectedRacePosition = -1;

            public PostPitRacePosition(List<Tuple<float, OpponentData>> opponentsFrontAfterStop, List<Tuple<float, OpponentData>> opponentsBehindAfterStop)
            {
                this.opponentsBehindAfterStop = opponentsBehindAfterStop;
                this.opponentsFrontAfterStop = opponentsFrontAfterStop;
                if (opponentsBehindAfterStop != null && opponentsBehindAfterStop.Count > 0)
                {
                    // we'll be in position - 1
                    expectedRacePosition = opponentsBehindAfterStop[0].Item2.ClassPosition - 1;
                }
                else if (opponentsFrontAfterStop != null && opponentsFrontAfterStop.Count > 0)
                {
                    // we'll be in position + 1
                    expectedRacePosition = opponentsFrontAfterStop[0].Item2.ClassPosition + 1;
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            // if voice message is 'practice pitstop' or something, set the boolean
        }
    }
}
