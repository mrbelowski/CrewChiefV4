using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class Penalties : AbstractEvent
    {
        // time (in seconds) to delay messages about penalty laps to go - 
        // we need this because the play might cross the start line while serving 
        // a penalty, so we should wait before telling them how many laps they have to serve it
        private int pitstopDelay = 20;

        private String folderNewPenaltyStopGo = "penalties/new_penalty_stopgo";

        private String folderNewPenaltyDriveThrough = "penalties/new_penalty_drivethrough";

        private String folderThreeLapsToServe = "penalties/penalty_three_laps_left";

        private String folderTwoLapsToServe = "penalties/penalty_two_laps_left";

        private String folderOneLapToServeStopGo = "penalties/penalty_one_lap_left_stopgo";

        private String folderOneLapToServeDriveThrough = "penalties/penalty_one_lap_left_drivethrough";

        public static String folderDisqualified = "penalties/penalty_disqualified";

        private String folderPitNowStopGo = "penalties/pit_now_stop_go";

        private String folderPitNowDriveThrough = "penalties/pit_now_drive_through";

        private String folderTimePenalty = "penalties/time_penalty";

        public static String folderCutTrackInRace = "penalties/cut_track_in_race";

        private String folderPossibleTrackLimitsViolation = "penalties/possible_track_limits_warning";

        public static String folderLapDeleted = "penalties/lap_deleted";

        public static String folderCutTrackPracticeOrQual = "penalties/cut_track_in_prac_or_qual";

        public static String folderCutTrackPracticeOrQualNextLapInvalid = "penalties/cut_track_in_prac_or_qual_next_invalid";

        private String folderPenaltyNotServed = "penalties/penalty_not_served";

        // for voice requests
        private String folderYouStillHavePenalty = "penalties/you_still_have_a_penalty";

        private String folderYouHavePenalty = "penalties/you_have_a_penalty";

        private String folderPenaltyServed = "penalties/penalty_served";

        private String folderYouDontHaveAPenalty = "penalties/you_dont_have_a_penalty";


        private Boolean hasHadAPenalty;

        private int penaltyLap;

        private int lapsCompleted;

        private Boolean playedPitNow;

        private Boolean hasOutstandingPenalty = false;

        private Boolean playedTimePenaltyMessage;

        private int cutTrackWarningsCount;

        private TimeSpan cutTrackWarningFrequency = TimeSpan.FromSeconds(30);

        private Boolean playCutTrackWarnings = UserSettings.GetUserSettings().getBoolean("play_cut_track_warnings");

        private Boolean playedTrackCutWarningInPracticeOrQualOnThisLap = false;

        private DateTime lastCutTrackWarningTime;

        private Boolean playedNotServedPenalty;

        private Boolean warnedOfPossibleTrackLimitsViolationOnThisLap = false;

        public Penalties(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            clearPenaltyState();
            lastCutTrackWarningTime = DateTime.MinValue;
            cutTrackWarningsCount = 0;
            hasHadAPenalty = false;
            warnedOfPossibleTrackLimitsViolationOnThisLap = false;
            playedTrackCutWarningInPracticeOrQualOnThisLap = false;
        }

        private void clearPenaltyState()
        {
            penaltyLap = -1;
            lapsCompleted = -1;
            hasOutstandingPenalty = false;
            // edge case here: if a penalty is given and immediately served (slow down penalty), then
            // the player gets another within the next 20 seconds, the 'you have 3 laps to come in to serve'
            // message would be in the queue and would be made valid again, so would play. So we explicity 
            // remove this message from the queue
            audioPlayer.removeQueuedMessage(folderThreeLapsToServe);
            playedPitNow = false;
            playedTimePenaltyMessage = false;
            playedNotServedPenalty = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if (eventSubType == folderPossibleTrackLimitsViolation)
                {
                    return true;
                }
                // When a new penalty is given we queue a 'three laps left to serve' delayed message.
                // If, the moment message is about to play, the player has started a new lap, this message is no longer valid so shouldn't be played
                if (eventSubType == folderThreeLapsToServe)
                {
                    Console.WriteLine("Checking penalty validity, pen lap = " + penaltyLap + ", completed =" + lapsCompleted);
                    return hasOutstandingPenalty && lapsCompleted == penaltyLap && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
                }
                else if (eventSubType == folderCutTrackInRace)
                {
                    return !hasOutstandingPenalty && currentGameState.SessionData.SessionPhase != SessionPhase.Finished && !currentGameState.PitData.InPitlane;
                }
                else if (eventSubType == folderCutTrackPracticeOrQual || eventSubType == folderCutTrackPracticeOrQualNextLapInvalid || eventSubType == folderLapDeleted)
                {
                    return currentGameState.SessionData.SessionPhase != SessionPhase.Finished && !currentGameState.PitData.InPitlane;
                }
                else
                {
                    return hasOutstandingPenalty && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
                }
            }
            else
            {
                return false;
            }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.IsNewLap)
            {
                warnedOfPossibleTrackLimitsViolationOnThisLap = false;
                playedTrackCutWarningInPracticeOrQualOnThisLap = false;
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null && 
                (currentGameState.PenaltiesData.HasDriveThrough || currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasTimeDeduction))
            {
                if (currentGameState.PenaltiesData.HasDriveThrough && !previousGameState.PenaltiesData.HasDriveThrough)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.playMessage(new QueuedMessage(folderNewPenaltyDriveThrough, 0, this), 10);
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.playMessage(new QueuedMessage(folderThreeLapsToServe, 20, this), 10);
                    // we don't already have a penalty
                    if (penaltyLap == -1 || !hasOutstandingPenalty)
                    {
                        penaltyLap = currentGameState.SessionData.CompletedLaps;
                    }
                    hasOutstandingPenalty = true;
                    hasHadAPenalty = true;
                }
                else if (currentGameState.PenaltiesData.HasStopAndGo && !previousGameState.PenaltiesData.HasStopAndGo)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.playMessage(new QueuedMessage(folderNewPenaltyStopGo, 0, this), 10);
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.playMessage(new QueuedMessage(folderThreeLapsToServe, 20, this), 10);
                    // we don't already have a penalty
                    if (penaltyLap == -1 || !hasOutstandingPenalty)
                    {
                        penaltyLap = currentGameState.SessionData.CompletedLaps;
                    }
                    hasOutstandingPenalty = true;
                    hasHadAPenalty = true;
                }
                else if (currentGameState.PitData.InPitlane && currentGameState.PitData.OnOutLap && !playedNotServedPenalty &&
                    (currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasDriveThrough))
                {
                    // we've exited the pits but there's still an outstanding penalty
                    audioPlayer.playMessage(new QueuedMessage(folderPenaltyNotServed, 3, this), 10);
                    playedNotServedPenalty = true;
                } 
                else if (currentGameState.SessionData.IsNewLap && (currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasDriveThrough))
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    if (lapsCompleted - penaltyLap == 3 && !currentGameState.PitData.InPitlane)
                    {
                        // run out of laps, and not in the pitlane
                        if (!audioPlayer.playRant("disqualified_rant", MessageContents(folderDisqualified)))
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderDisqualified, 5, this), 10);
                        }
                    }
                    else if (lapsCompleted - penaltyLap == 2 && currentGameState.PenaltiesData.HasDriveThrough)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderOneLapToServeDriveThrough, pitstopDelay, this), 10);
                    }
                    else if (lapsCompleted - penaltyLap == 2 && currentGameState.PenaltiesData.HasStopAndGo)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderOneLapToServeStopGo, pitstopDelay, this), 10);
                    }
                    else if (lapsCompleted - penaltyLap == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLapsToServe, pitstopDelay, this), 10);
                    }
                }
                else if (!playedPitNow && currentGameState.SessionData.SectorNumber == 3 && currentGameState.PenaltiesData.HasStopAndGo && lapsCompleted - penaltyLap == 2)
                {
                    playedPitNow = true;
                    audioPlayer.playMessage(new QueuedMessage(folderPitNowStopGo, 6, this), 10);
                }
                else if (!playedPitNow && currentGameState.SessionData.SectorNumber == 3 && currentGameState.PenaltiesData.HasDriveThrough && lapsCompleted - penaltyLap == 2)
                {
                    playedPitNow = true;
                    audioPlayer.playMessage(new QueuedMessage(folderPitNowDriveThrough, 6, this), 10);
                }
                else if (!playedTimePenaltyMessage && currentGameState.PenaltiesData.HasTimeDeduction)
                {
                    playedTimePenaltyMessage = true;
                    audioPlayer.playMessage(new QueuedMessage(folderTimePenalty, 0, this), 10);
                }
            }
            else if (currentGameState.PositionAndMotionData.CarSpeed > 1 && playCutTrackWarnings && 
                !currentGameState.PitData.OnOutLap &&
                currentGameState.PenaltiesData.CutTrackWarnings > cutTrackWarningsCount &&
                currentGameState.PenaltiesData.NumPenalties == previousGameState.PenaltiesData.NumPenalties)  // Make sure we've no new penalty for this cut.
            {
                cutTrackWarningsCount = currentGameState.PenaltiesData.CutTrackWarnings;
                if (currentGameState.ControlData.ControlType != ControlType.AI &&
                    lastCutTrackWarningTime.Add(cutTrackWarningFrequency) < currentGameState.Now)
                {
                    lastCutTrackWarningTime = currentGameState.Now;
                    // don't warn on the first lap of the session
                    if (currentGameState.SessionData.CompletedLaps > 0)
                    {
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderCutTrackInRace, 2, this), 3);
                        }
                        else if (!playedTrackCutWarningInPracticeOrQualOnThisLap)
                        {
                            // cut track in prac / qual is the same as lap deleted. Rather than dick about with the sound files, just allow either here
                            if (CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM
                                && currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance != -1.0f
                                && currentGameState.PositionAndMotionData.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance)
                            {
                                audioPlayer.playMessage(new QueuedMessage(Utilities.random.NextDouble() < 0.3 ? folderLapDeleted : folderCutTrackPracticeOrQualNextLapInvalid, 2, this), 10);
                            }
                            else
                            {
                                audioPlayer.playMessage(new QueuedMessage(Utilities.random.NextDouble() < 0.3 ? folderLapDeleted : folderCutTrackPracticeOrQual, 2, this), 10);
                            }
                            playedTrackCutWarningInPracticeOrQualOnThisLap = true;
                        }
                    }
                    clearPenaltyState();
                }
            }
            else if (currentGameState.PositionAndMotionData.CarSpeed > 1 && playCutTrackWarnings && currentGameState.SessionData.SessionType != SessionType.Race &&
              !currentGameState.SessionData.CurrentLapIsValid && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid &&
                CrewChief.gameDefinition.gameEnum != GameEnum.IRACING && !currentGameState.PitData.OnOutLap)
            {
                // JB: don't think we need this block - the previous block should always trigger in preference to this, but we'll leave it here just in case
                cutTrackWarningsCount = currentGameState.PenaltiesData.CutTrackWarnings;
                // don't warn about cut track if the AI is driving
                if (currentGameState.ControlData.ControlType != ControlType.AI &&
                    lastCutTrackWarningTime.Add(cutTrackWarningFrequency) < currentGameState.Now)
                {
                    lastCutTrackWarningTime = currentGameState.Now;
                    // cut track in prac / qual is the same as lap deleted. Rather than dick about with the sound files, just allow either here
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM
                        && currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance != -1.0f
                        && currentGameState.PositionAndMotionData.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.raceroomRollingStartLapDistance)
                    {
                        audioPlayer.playMessage(new QueuedMessage(Utilities.random.NextDouble() < 0.3 ? folderLapDeleted : folderCutTrackPracticeOrQualNextLapInvalid, 2, this), 10);
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage(Utilities.random.NextDouble() < 0.3 ? folderLapDeleted : folderCutTrackPracticeOrQual, 2, this), 10);
                    }
                    clearPenaltyState();
                }
            }
            
            // can't read penalty type in Automobilista
            // Assume this applies to rF2 as well for now
            else if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null &&
                currentGameState.PenaltiesData.NumPenalties > 0 && (CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT))
            {
                if (currentGameState.PenaltiesData.NumPenalties > previousGameState.PenaltiesData.NumPenalties)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.playMessage(new QueuedMessage(folderYouHavePenalty, Utilities.random.Next(3, 7), this), 10);
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.playMessage(new QueuedMessage(folderThreeLapsToServe, Utilities.random.Next(10, 20), this), 10);
                    // we don't already have a penalty
                    if (penaltyLap == -1 || !hasOutstandingPenalty)
                    {
                        penaltyLap = currentGameState.SessionData.CompletedLaps;
                    }
                    hasOutstandingPenalty = true;
                    hasHadAPenalty = true;
                }
                else if (currentGameState.PitData.InPitlane && currentGameState.PitData.OnOutLap && !playedNotServedPenalty &&
                    currentGameState.PenaltiesData.NumPenalties > 0)
                {
                    // we've exited the pits but there's still an outstanding penalty
                    audioPlayer.playMessage(new QueuedMessage(folderPenaltyNotServed, 3, this), 10);
                    playedNotServedPenalty = true;
                }
                else if (currentGameState.SessionData.IsNewLap && currentGameState.PenaltiesData.NumPenalties > 0)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    if (lapsCompleted - penaltyLap >= 2 && !currentGameState.PitData.InPitlane)
                    {
                        // run out of laps, an not in the pitlane
                        audioPlayer.playMessage(new QueuedMessage(folderYouStillHavePenalty, 5, this), 10);
                    }
                    else if (lapsCompleted - penaltyLap == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLapsToServe, pitstopDelay, this), 10);
                    }
                }
            }
            else if (currentGameState.PenaltiesData.PossibleTrackLimitsViolation && playCutTrackWarnings && !warnedOfPossibleTrackLimitsViolationOnThisLap)
            {
                warnedOfPossibleTrackLimitsViolationOnThisLap = true;
                audioPlayer.playMessage(new QueuedMessage(folderPossibleTrackLimitsViolation, 2 + Utilities.random.Next(3), this), 0);
            }
            else
            {
                clearPenaltyState();
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null && 
                ((previousGameState.PenaltiesData.HasStopAndGo && !currentGameState.PenaltiesData.HasStopAndGo) ||
                (previousGameState.PenaltiesData.HasDriveThrough && !currentGameState.PenaltiesData.HasDriveThrough) ||
                // can't read penalty type in Automobilista (and presumably in rF2).
                (previousGameState.PenaltiesData.NumPenalties > currentGameState.PenaltiesData.NumPenalties &&
                (CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT))))
            {
                audioPlayer.playMessage(new QueuedMessage(folderPenaltyServed, 0, this), 10);
            }            
        }

        public override void respond(string voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SESSION_STATUS) ||
                     SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                if (hasOutstandingPenalty)
                {
                    if (lapsCompleted - penaltyLap == 2)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("youHaveAPenaltyBoxThisLap",
                            MessageContents(folderYouHavePenalty, PitStops.folderMandatoryPitStopsPitThisLap), 0, null));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderYouHavePenalty, 0, null));
                    }
                }
            }
            else
            {
                if (!hasHadAPenalty)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderYouDontHaveAPenalty, 0, null));
                    return;
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.DO_I_HAVE_A_PENALTY))
                {
                    if (hasOutstandingPenalty)
                    {
                        if (lapsCompleted - penaltyLap == 2)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("youHaveAPenaltyBoxThisLap",
                                MessageContents(folderYouHavePenalty, PitStops.folderMandatoryPitStopsPitThisLap), 0, null));
                        }
                        else
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderYouHavePenalty, 0, null));
                        }
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderYouDontHaveAPenalty, 0, null));
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HAVE_I_SERVED_MY_PENALTY))
                {
                    if (hasOutstandingPenalty)
                    {
                        List<MessageFragment> messages = new List<MessageFragment>();
                        messages.Add(MessageFragment.Text(AudioPlayer.folderNo));
                        messages.Add(MessageFragment.Text(folderYouStillHavePenalty));
                        if (lapsCompleted - penaltyLap == 2)
                        {
                            messages.Add(MessageFragment.Text(PitStops.folderMandatoryPitStopsPitThisLap));
                        }
                        audioPlayer.playMessageImmediately(new QueuedMessage("noYouStillHaveAPenalty", messages, 0, null));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("yesYouServedYourPenalty",
                            MessageContents(AudioPlayer.folderYes, folderPenaltyServed), 0, null));
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.DO_I_STILL_HAVE_A_PENALTY))
                {
                    if (hasOutstandingPenalty)
                    {
                        List<MessageFragment> messages = new List<MessageFragment>();
                        messages.Add(MessageFragment.Text(AudioPlayer.folderYes));
                        messages.Add(MessageFragment.Text(folderYouStillHavePenalty));
                        if (lapsCompleted - penaltyLap == 2)
                        {
                            messages.Add(MessageFragment.Text(PitStops.folderMandatoryPitStopsPitThisLap));
                        }
                        audioPlayer.playMessageImmediately(new QueuedMessage("yesYouStillHaveAPenalty", messages, 0, null));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("noYouServedYourPenalty",
                            MessageContents(AudioPlayer.folderNo, folderPenaltyServed), 0, null));
                    }
                }
            }
        }
    }
}
