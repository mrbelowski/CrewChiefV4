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
                    reportPostPitEstimates(false, currentGameState.SessionData.ClassPosition, currentGameState.OpponentData, 
                        currentGameState.SessionData.DeltaTime, currentGameState.Now);
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

        private void reportPostPitEstimates(Boolean fromVoiceCommand, int currentRacePosition,
            Dictionary<String, OpponentData> opponents, DeltaTime playerDeltaTime, DateTime now)
        {
            float expectedPlayerTimeLoss = -1;
            if (playerTimeLostForStop == -1)
            {
                // we have no idea how much we'll lose in a stop, so derive this from the closet opponent
                if (opponentsTimeLostForStop.Count == 0)
                {
                    // oh dear
                    if (fromVoiceCommand)
                    {
                        audioPlayer.playMessage(new QueuedMessage(AudioPlayer.folderNoData, 0, this));
                    }
                }
                else
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
                
                foreach (OpponentData opponent in opponents.Values)
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
                // TODO: sort each of these by the delta, smallest first

                // phew... now we know who will be in front and who will be behind when we emerge from the pitlane. We also
                // now the expected distance between us and them (in metres) when we emerge.
            }
        }

        private static class PostPitRacePosition
        {
            // these may have zero or more entries - these are the opponents who'll
            // be 'interesting' (close, racing for position, etc)
            List<Tuple<String, float>> opponentsFrontAfterStop = new List<Tuple<string,float>>();
            List<Tuple<String, float>> opponentsBehindAfterStop = new List<Tuple<string, float>>();
            int expectedRacePosition = -1;

            public PostPitRacePosition()
            {

            }
        }

        public override void respond(string voiceMessage)
        {
            // if voice message is 'practice pitstop' or something, set the boolean
        }
    }
}
