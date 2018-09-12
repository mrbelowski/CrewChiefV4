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

        private Boolean brakeTempWarningOnPitExit = UserSettings.GetUserSettings().getBoolean("enable_pit_exit_brake_temp_warning");
        private Boolean tyreTempWarningOnPitExit = UserSettings.GetUserSettings().getBoolean("enable_pit_exit_tyre_temp_warning");

        private String folderPushToImprove = "push_now/push_to_improve";
        private String folderPushToGetWin = "push_now/push_to_get_win";
        private String folderPushToGetSecond = "push_now/push_to_get_second";
        private String folderPushToGetThird = "push_now/push_to_get_third";
        private String folderPushToHoldPosition = "push_now/push_to_hold_position";

        private String folderPushExitingPits = "push_now/pits_exit_clear";
        private String folderTrafficBehindExitingPits = "push_now/pits_exit_traffic_behind";
        private String folderOpponentExitingPits = "push_now/opponent_exiting_pits";

        public static String folderQualExitIntro = "push_now/we_have";
        public static String folderQualExitOutroMinutes = "push_now/minutes_to_set_a_lap";
        public static String folderQualExitOutroLaps = "push_now/laps_to_get_the_job_done";

        private Boolean playedNearEndTimePush;
        private int lapsToCountBackForOpponentBest = 4;
        private Boolean playedNearEndLapsPush;

        private Boolean playedQualExitMessage = false;
        private float minTimeToBeInThisPosition = 60;

        private float distanceBeforeStartLineToWarnOfPitExit = 200;
        private float maxSpeedWhenCrossingLine = 0;

        public PushNow(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            playedNearEndTimePush = false;
            playedNearEndLapsPush = false;
            playedQualExitMessage = false;
            maxSpeedWhenCrossingLine = 0;
            distanceBeforeStartLineToWarnOfPitExit = 100;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }
        
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (GameStateData.onManualFormationLap)
            {
                return;
            }
            if (currentGameState.SessionData.IsNewLap && currentGameState.PositionAndMotionData.CarSpeed > maxSpeedWhenCrossingLine)
            {
                maxSpeedWhenCrossingLine = currentGameState.PositionAndMotionData.CarSpeed;
                // the distance at which we check if there's a car exiting the pits will be speed dependent. 
                // The faster we are over the line, the more notice we'll need. * 3 is a number I pulled out of my arse,
                // it may be shit.
                distanceBeforeStartLineToWarnOfPitExit = maxSpeedWhenCrossingLine * 3;
            }
            Boolean checkPushToGain = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.GameTimeAtLastPositionFrontChange < minTimeToBeInThisPosition;
            Boolean checkPushToHold = currentGameState.SessionData.SessionRunningTime - currentGameState.SessionData.GameTimeAtLastPositionBehindChange < minTimeToBeInThisPosition;
            if (currentGameState.SessionData.SessionType == SessionType.Race && !currentGameState.PitData.InPitlane)
            {
                if ((checkPushToGain || checkPushToHold) && !playedNearEndTimePush && currentGameState.SessionData.SessionHasFixedTime &&
                        currentGameState.SessionData.SessionTimeRemaining < 4 * 60 && currentGameState.SessionData.SessionTimeRemaining > 2 * 60)
                {
                    // estimate the number of remaining laps - be optimistic...
                    int numLapsLeft = (int)Math.Ceiling((double)currentGameState.SessionData.SessionTimeRemaining / (double)currentGameState.SessionData.PlayerLapTimeSessionBest);
                    if (currentGameState.SessionData.HasExtraLap)
                    {
                        numLapsLeft = numLapsLeft + 1;
                    }
                    playedNearEndTimePush = checkGaps(currentGameState, numLapsLeft, checkPushToGain, checkPushToHold);
                }
                else if ((checkPushToGain || checkPushToHold) && !playedNearEndLapsPush && !currentGameState.SessionData.SessionHasFixedTime &&
                    ((currentGameState.SessionData.SessionLapsRemaining <= 4 && currentGameState.SessionData.TrackDefinition.trackLengthClass <= TrackData.TrackLengthClass.MEDIUM) ||
                     (currentGameState.SessionData.SessionLapsRemaining <= 2 && currentGameState.SessionData.TrackDefinition.trackLengthClass <= TrackData.TrackLengthClass.LONG) ||
                     (currentGameState.SessionData.SessionLapsRemaining == 1 && currentGameState.SessionData.TrackDefinition.trackLengthClass <= TrackData.TrackLengthClass.VERY_LONG)))
                {
                    playedNearEndLapsPush = checkGaps(currentGameState, currentGameState.SessionData.SessionLapsRemaining, checkPushToGain, checkPushToHold);
                }
            }
            if (currentGameState.PitData.IsAtPitExit && currentGameState.PositionAndMotionData.CarSpeed > 5)
            {
                // we've just been handed control back after a pitstop
                if (currentGameState.SessionData.SessionRunningTime > 30 && isOpponentApproachingPitExit(currentGameState))
                {
                    // we've exited into clean air
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderTrafficBehindExitingPits, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPushExitingPits, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                }
                // now try and report the current brake and tyre temp status
                try
                {
                    if (brakeTempWarningOnPitExit)
                    {
                        ((TyreMonitor)CrewChief.getEvent("TyreMonitor")).reportBrakeTempStatus(false, false);
                    }
                    if (tyreTempWarningOnPitExit)
                    {
                        ((TyreMonitor)CrewChief.getEvent("TyreMonitor")).reportCurrentTyreTempStatus(false);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Failed to report brake temp status on pit exit");
                }
                if (!playedQualExitMessage && currentGameState.SessionData.SessionType == SessionType.Qualify)
                {
                    playedQualExitMessage = true;
                    if (currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        // special case for iracing - AFAIK no other games have number-of-laps in qual sessions
                        audioPlayer.playMessageImmediately(new QueuedMessage("qual_pit_exit", MessageContents(folderQualExitIntro,
                            currentGameState.SessionData.SessionNumberOfLaps, folderQualExitOutroLaps), 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                    }
                    else if (currentGameState.SessionData.SessionHasFixedTime)
                    {
                        int minutesLeft = (int)Math.Floor(currentGameState.SessionData.SessionTimeRemaining / 60f);
                        if (minutesLeft > 1)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("qual_pit_exit", MessageContents(folderQualExitIntro, minutesLeft, folderQualExitOutroMinutes), 0,
                                this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                }
            }
            if (previousGameState != null &&
                currentGameState.SessionData.SectorNumber == 3 &&
                currentGameState.PositionAndMotionData.CarSpeed > 5 && 
                !currentGameState.PitData.InPitlane && 
                isApproachingStartLine(currentGameState.SessionData.TrackDefinition, previousGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.PositionAndMotionData.DistanceRoundTrack) &&
                isOpponentLeavingPits(currentGameState))
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(folderOpponentExitingPits, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
            }
        }

        private Boolean isApproachingStartLine(TrackDefinition track, float previousLapDistance, float currentLapDistance)
        {
            if (track == null) {
                return false;
            }
            float checkPoint = track.trackLength - distanceBeforeStartLineToWarnOfPitExit;
            return previousLapDistance < checkPoint && currentLapDistance > checkPoint; 
        }

        private Boolean isOpponentApproachingPitExit(GameStateData currentGameState)
        {
            // Hooray for PCars and its broken data
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
            foreach (KeyValuePair<string, OpponentData> opponent in currentGameState.OpponentData)
            {
                if ((opponent.Value.OpponentLapData.Count > 0 || !startCheckPointIsInSector1) && opponent.Value.Speed > 0 &&
                    !opponent.Value.isEnteringPits() && !opponent.Value.isExitingPits() && !opponent.Value.InPits &&
                    ((startCheckPointIsInSector1 && opponent.Value.DistanceRoundTrack > distanceStartCheckPoint && opponent.Value.DistanceRoundTrack < distanceEndCheckPoint) ||
                        (!startCheckPointIsInSector1 && (opponent.Value.DistanceRoundTrack > distanceStartCheckPoint || opponent.Value.DistanceRoundTrack < distanceEndCheckPoint))))
                {
                    return true;
                }
                if (opponent.Value.Speed > 0 &&
                    !opponent.Value.isEnteringPits() && !opponent.Value.isExitingPits() && !opponent.Value.InPits)
                {
                    float signedDelta = opponent.Value.DeltaTime.GetSignedDeltaTimeOnly(currentGameState.SessionData.DeltaTime);
                    //add a little to gap as 0 is right next to us when leaving the pits
                    if (signedDelta < 0.2 && signedDelta < -7)
                    {
                        // more than 0 but less than 7 seconds behind, so warn about an approaching car
                        return true;
                    }
                }
            }
            return false;
        }           
        
        private Boolean isOpponentLeavingPits(GameStateData currentGameState)
        {
            // games with sane lap distance data when pitting
            foreach (KeyValuePair<string, OpponentData> opponent in currentGameState.OpponentData)
            {
                // if the opponent car is moving at approximately the pit speed limit, is exiting the pits, 
                // has passed the start line, warn about him. Note that this method is expected to be called
                // when the player is approaching the start line
                if (opponent.Value.Speed > 10 && opponent.Value.Speed < 40 && opponent.Value.InPits &&
                    opponent.Value.isExitingPits() && opponent.Value.DistanceRoundTrack > 0 && opponent.Value.DistanceRoundTrack < 300)
                {
                    return true;
                }
            }
            return false;           
        }

        private Boolean checkGaps(GameStateData currentGameState, int numLapsLeft, Boolean checkPushToGain, Boolean checkPushToHold)
        {
            if (checkPushToGain && currentGameState.SessionData.ClassPosition > 1)
            {
                float opponentInFrontBestLap = getOpponentBestLap(currentGameState.SessionData.ClassPosition - 1, lapsToCountBackForOpponentBest, currentGameState);
                if (opponentInFrontBestLap > 0 &&
                    (opponentInFrontBestLap - currentGameState.SessionData.PlayerLapTimeSessionBest) * numLapsLeft > currentGameState.SessionData.TimeDeltaFront)
                {
                    // going flat out, we're going to catch the guy ahead us before the end
                    if (currentGameState.SessionData.ClassPosition == 2)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToGetWin, 0, this), 5);
                    }
                    else if (currentGameState.SessionData.ClassPosition == 3)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToGetSecond, 0, this), 5);
                    }
                    else if (currentGameState.SessionData.ClassPosition == 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToGetThird, 0, this), 5);
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPushToImprove, 0, this), 5);
                    }
                    return true;
                }
            }
            if (checkPushToHold && !currentGameState.isLast())
            {
                float opponentBehindBestLap = getOpponentBestLap(currentGameState.SessionData.ClassPosition + 1, lapsToCountBackForOpponentBest, currentGameState);
                if (opponentBehindBestLap > 0 &&
                    (currentGameState.SessionData.PlayerLapTimeSessionBest - opponentBehindBestLap) * numLapsLeft > currentGameState.SessionData.TimeDeltaBehind)
                {
                    // even with us going flat out, the guy behind is going to catch us before the end
                    Console.WriteLine("Might lose this position. Player best lap = " + currentGameState.SessionData.PlayerLapTimeSessionBest.ToString("0.000") + " laps left = " + numLapsLeft +
                        " opponent best lap = " + opponentBehindBestLap.ToString("0.000") + " time delta = " + currentGameState.SessionData.TimeDeltaBehind.ToString("0.000"));
                    audioPlayer.playMessage(new QueuedMessage(folderPushToHoldPosition, 0, this), 3);
                    return true;
                }
            }
            return false;
        }

        private float getOpponentBestLap(int opponentPosition, int lapsToCheck, GameStateData gameState)
        {
            OpponentData opponent = gameState.getOpponentAtClassPosition(opponentPosition, gameState.carClass);
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
