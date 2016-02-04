using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class PushNow : AbstractEvent
    {
        private float maxSeparationForPitExitWarning = 300;   // metres
        private float minSeparationForPitExitWarning = 10;   // metres

        // TODO: use driver names here?
        private String folderPushToImprove = "push_now/push_to_improve";
        private String folderPushToGetWin = "push_now/push_to_get_win";
        private String folderPushToGetSecond = "push_now/push_to_get_second";
        private String folderPushToGetThird = "push_now/push_to_get_third";
        private String folderPushToHoldPosition = "push_now/push_to_hold_position";

        private String folderPushExitingPits = "push_now/pits_exit_clear";
        private String folderTrafficBehindExitingPits = "push_now/pits_exit_traffic_behind";

        private Boolean playedNearEndTimePush;
        private int lapsToCountBackForOpponentBest = 4;
        private Boolean playedNearEndLapsPush;

        private float minTimeToBeInThisPosition = 60;

        public PushNow(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            playedNearEndTimePush = false;
            playedNearEndLapsPush = false;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }
        
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            Boolean checkPushToGain = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.GameTimeAtLastPositionFrontChange < minTimeToBeInThisPosition;
            Boolean checkPushToHold = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.GameTimeAtLastPositionBehindChange < minTimeToBeInThisPosition;
            if (currentGameState.SessionData.SessionType == SessionType.Race && !currentGameState.PitData.InPitlane)
            {
                if ((checkPushToGain || checkPushToHold) && !playedNearEndTimePush && currentGameState.SessionData.SessionNumberOfLaps <= 0 &&
                        currentGameState.SessionData.SessionTimeRemaining < 4 * 60 && currentGameState.SessionData.SessionTimeRemaining > 2 * 60)
                {
                    // estimate the number of remaining laps - be optimistic...
                    int numLapsLeft = (int)Math.Ceiling((double)currentGameState.SessionData.SessionTimeRemaining / (double)currentGameState.SessionData.PlayerLapTimeSessionBest);
                    if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.DTM_2015)
                    {
                        numLapsLeft = numLapsLeft + 1;
                    }
                    playedNearEndTimePush = checkGaps(currentGameState, numLapsLeft, checkPushToGain, checkPushToHold);
                }
                else if ((checkPushToGain || checkPushToHold) && !playedNearEndLapsPush && currentGameState.SessionData.SessionNumberOfLaps > 0 &&
                    currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps <= 4)
                {
                    playedNearEndLapsPush = checkGaps(currentGameState, currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps, checkPushToGain, checkPushToHold);
                }
            }
            if (currentGameState.PitData.IsAtPitExit && currentGameState.PositionAndMotionData.CarSpeed > 5)
            {
                // we've just been handed control back after a pitstop
                if (isOpponentApproachingPitExit(currentGameState))
                {
                    // we've exited into clean air
                    audioPlayer.playMessage(new QueuedMessage(folderTrafficBehindExitingPits, 0, this));
                }
                else
                {
                    audioPlayer.playMessage(new QueuedMessage(folderPushExitingPits, 0, this));
                }
            }
        }

        private Boolean isOpponentApproachingPitExit(GameStateData currentGameState)
        {
            float distanceStartCheckPoint;
            float distanceEndCheckPoint;

            // hack for PCars - the distanceRoundTrack will be zero until we enter turn one after leaving the pits. Stupid...
            if (currentGameState.PositionAndMotionData.DistanceRoundTrack == 0)
            {
                distanceStartCheckPoint = 0;
                distanceEndCheckPoint = maxSeparationForPitExitWarning - minSeparationForPitExitWarning;
            }
            else
            {
                distanceStartCheckPoint = currentGameState.PositionAndMotionData.DistanceRoundTrack - maxSeparationForPitExitWarning;
                distanceEndCheckPoint = currentGameState.PositionAndMotionData.DistanceRoundTrack - minSeparationForPitExitWarning;
            }
            Boolean startCheckPointIsInSector1 = true;
            // here we assume the end check point will be in sector 1 (after the s/f line)
            if (distanceStartCheckPoint < 0) 
            {
                startCheckPointIsInSector1 = false;
                distanceStartCheckPoint = currentGameState.SessionData.TrackDefinition.trackLength + distanceStartCheckPoint;
            }
            foreach (KeyValuePair<Object, OpponentData> opponent in currentGameState.OpponentData)
            {
                if ((opponent.Value.OpponentLapData.Count > 0 || !startCheckPointIsInSector1) && opponent.Value.Speed > 0 && 
                    !opponent.Value.isEnteringPits() && !opponent.Value.isExitingPits() &&
                    ((startCheckPointIsInSector1 && opponent.Value.DistanceRoundTrack > distanceStartCheckPoint && opponent.Value.DistanceRoundTrack < distanceEndCheckPoint) ||
                     (!startCheckPointIsInSector1 && (opponent.Value.DistanceRoundTrack > distanceStartCheckPoint || opponent.Value.DistanceRoundTrack < distanceEndCheckPoint))))
                {
                    return true;
                }
            }
            return false;
        }

        private Boolean checkGaps(GameStateData currentGameState, int numLapsLeft, Boolean checkPushToGain, Boolean checkPushToHold)
        {
            if (checkPushToGain && currentGameState.SessionData.Position > 1)
            {
                float opponentInFrontBestLap = getOpponentBestLap(currentGameState.SessionData.Position - 1, lapsToCountBackForOpponentBest, currentGameState);
                if (opponentInFrontBestLap > 0 &&
                    (opponentInFrontBestLap - currentGameState.SessionData.PlayerLapTimeSessionBest) * numLapsLeft > currentGameState.SessionData.TimeDeltaFront)
                {
                    // going flat out, we're going to catch the guy ahead us before the end
                    if (currentGameState.SessionData.Position == 2)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToGetWin, 0, this));
                    }
                    else if (currentGameState.SessionData.Position == 3)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToGetSecond, 0, this));
                    }
                    else if (currentGameState.SessionData.Position == 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToGetThird, 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToImprove, 0, this));
                    }
                    return true;
                }
            }
            if (checkPushToHold && !currentGameState.isLast())
            {
                float opponentBehindBestLap = getOpponentBestLap(currentGameState.SessionData.Position + 1, lapsToCountBackForOpponentBest, currentGameState);
                if (opponentBehindBestLap > 0 &&
                    (currentGameState.SessionData.PlayerLapTimeSessionBest - opponentBehindBestLap) * numLapsLeft > currentGameState.SessionData.TimeDeltaBehind)
                {
                    // even with us going flat out, the guy behind is going to catch us before the end
                    Console.WriteLine("might lose this position. Player best lap = " + currentGameState.SessionData.PlayerLapTimeSessionBest + " laps left = " + numLapsLeft +
                        " opponent best lap = " + opponentBehindBestLap + " time delta = " + currentGameState.SessionData.TimeDeltaBehind);
                    audioPlayer.playMessage(new QueuedMessage(folderPushToHoldPosition, 0, this));
                    return true;
                }
            }
            return false;
        }

        private float getOpponentBestLap(int opponentPosition, int lapsToCheck, GameStateData gameState)
        {
            OpponentData opponent = gameState.getOpponentAtPosition(opponentPosition, false);
            if (opponent == null || opponent.OpponentLapData.Count < lapsToCheck)
            {
                return -1;
            }
            float bestLap = -1;
            for (int i = opponent.OpponentLapData.Count - 1; i >= opponent.OpponentLapData.Count - lapsToCheck; i--)
            {
                if (bestLap == -1 || bestLap > opponent.OpponentLapData[i].LapTime)
                {
                    bestLap = opponent.OpponentLapData[i].LapTime;
                }
            }
            return bestLap;
        }
    }
}
