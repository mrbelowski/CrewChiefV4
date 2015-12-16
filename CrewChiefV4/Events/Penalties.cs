using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;

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

        public static String folderLapDeleted = "penalties/lap_deleted";

        public static String folderCutTrackPracticeOrQual = "penalties/cut_track_in_prac_or_qual";

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

        private DateTime lastCutTrackWarningTime;

        private Boolean playedNotServedPenalty;

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
            audioPlayer.removeQueuedClip(folderThreeLapsToServe);
            playedPitNow = false;
            playedTimePenaltyMessage = false;
            playedNotServedPenalty = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            // when a new penalty is given we queue a 'three laps left to serve' message for 20 seconds in the future.
            // If, 20 seconds later, the player has started a new lap, this message is no longer valid so shouldn't be played
            if (eventSubType == folderThreeLapsToServe)
            {
                Console.WriteLine("checking penalty validity, pen lap = " + penaltyLap + ", completed =" + lapsCompleted);
                return hasOutstandingPenalty && lapsCompleted == penaltyLap && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
            else if (eventSubType == folderCutTrackInRace) 
            {
                return !hasOutstandingPenalty && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
            else if(eventSubType == folderCutTrackPracticeOrQual || eventSubType == folderLapDeleted)
            {
                return currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
            else
            {
                return hasOutstandingPenalty && currentGameState.SessionData.SessionPhase != SessionPhase.Finished;
            }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null && 
                currentGameState.PenaltiesData.HasDriveThrough || currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasTimeDeduction)
            {
                if (currentGameState.PenaltiesData.HasDriveThrough && !previousGameState.PenaltiesData.HasDriveThrough)
                {
                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    // this is a new penalty
                    audioPlayer.queueClip(new QueuedMessage(folderNewPenaltyDriveThrough, 0, this));
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.queueClip(new QueuedMessage(folderThreeLapsToServe, 20, this));
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
                    audioPlayer.queueClip(new QueuedMessage(folderNewPenaltyStopGo, 0, this));
                    // queue a '3 laps to serve penalty' message - this might not get played
                    audioPlayer.queueClip(new QueuedMessage(folderThreeLapsToServe, 20, this));
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
                    audioPlayer.queueClip(new QueuedMessage(folderPenaltyNotServed, 3, this));
                    playedNotServedPenalty = true;
                } 
                else if (currentGameState.SessionData.IsNewLap && (currentGameState.PenaltiesData.HasStopAndGo || currentGameState.PenaltiesData.HasDriveThrough))
                {
                    // TODO: variable number of laps to serve penalty...

                    lapsCompleted = currentGameState.SessionData.CompletedLaps;
                    if (lapsCompleted - penaltyLap == 3 && !currentGameState.PitData.InPitlane)
                    {
                        // run out of laps, an not in the pitlane
                        audioPlayer.queueClip(new QueuedMessage(folderDisqualified, 5, this));
                    }
                    else if (lapsCompleted - penaltyLap == 2 && currentGameState.PenaltiesData.HasDriveThrough)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderOneLapToServeDriveThrough, pitstopDelay, this));
                    }
                    else if (lapsCompleted - penaltyLap == 2 && currentGameState.PenaltiesData.HasStopAndGo)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderOneLapToServeStopGo, pitstopDelay, this));
                    }
                    else if (lapsCompleted - penaltyLap == 1)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLapsToServe, pitstopDelay, this));
                    }
                }
                else if (!playedPitNow && currentGameState.SessionData.SectorNumber == 3 && currentGameState.PenaltiesData.HasStopAndGo && lapsCompleted - penaltyLap == 2)
                {
                    playedPitNow = true;
                    audioPlayer.queueClip(new QueuedMessage(folderPitNowStopGo, 6, this));
                }
                else if (!playedPitNow && currentGameState.SessionData.SectorNumber == 3 && currentGameState.PenaltiesData.HasDriveThrough && lapsCompleted - penaltyLap == 2)
                {
                    playedPitNow = true;
                    audioPlayer.queueClip(new QueuedMessage(folderPitNowDriveThrough, 6, this));
                }
                else if (!playedTimePenaltyMessage && currentGameState.PenaltiesData.HasTimeDeduction)
                {
                    playedTimePenaltyMessage = true;
                    audioPlayer.queueClip(new QueuedMessage(folderTimePenalty, 0, this));
                }
            }
            else if (currentGameState.PositionAndMotionData.CarSpeed > 1 && playCutTrackWarnings && currentGameState.SessionData.SessionType != SessionType.Race &&
              !currentGameState.SessionData.CurrentLapIsValid && previousGameState != null && previousGameState.SessionData.CurrentLapIsValid)
            {
                cutTrackWarningsCount = currentGameState.PenaltiesData.CutTrackWarnings;
                // don't warn about cut track if the AI is driving
                if (currentGameState.ControlData.ControlType != ControlType.AI &&
                    lastCutTrackWarningTime.Add(cutTrackWarningFrequency) < currentGameState.Now)
                {
                    lastCutTrackWarningTime = currentGameState.Now;
                    audioPlayer.queueClip(new QueuedMessage(folderLapDeleted, 2, this));
                    clearPenaltyState();
                }
            }
            else if (currentGameState.PositionAndMotionData.CarSpeed > 1 && playCutTrackWarnings &&
                currentGameState.PenaltiesData.CutTrackWarnings > cutTrackWarningsCount)
            {
                cutTrackWarningsCount = currentGameState.PenaltiesData.CutTrackWarnings;
                if (currentGameState.ControlData.ControlType != ControlType.AI &&
                    lastCutTrackWarningTime.Add(cutTrackWarningFrequency) < currentGameState.Now)
                {
                    lastCutTrackWarningTime = currentGameState.Now;
                    if (currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderCutTrackInRace, 2, this));
                    }
                    else
                    {
                        audioPlayer.queueClip(new QueuedMessage(folderCutTrackPracticeOrQual, 2, this));
                    }
                    clearPenaltyState();
                }
            }                     
            else
            {
                clearPenaltyState();
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && previousGameState != null && 
                ((previousGameState.PenaltiesData.HasStopAndGo && !currentGameState.PenaltiesData.HasStopAndGo) ||
                (previousGameState.PenaltiesData.HasDriveThrough && !currentGameState.PenaltiesData.HasDriveThrough)))
            {
                audioPlayer.queueClip(new QueuedMessage(folderPenaltyServed, 0, null));
            }            
        }

        public override void respond(string voiceMessage)
        {
            if (!hasHadAPenalty)
            {
                audioPlayer.playClipImmediately(new QueuedMessage(folderYouDontHaveAPenalty, 0, null), false);
                audioPlayer.closeChannel();
                return;
            }
            if (voiceMessage.Contains(SpeechRecogniser.DO_I_HAVE_A_PENALTY))
            {
                if (hasOutstandingPenalty) {
                    if (lapsCompleted - penaltyLap == 2) {
                        audioPlayer.playClipImmediately(new QueuedMessage("youHaveAPenaltyBoxThisLap",
                            MessageContents(folderYouHavePenalty, MandatoryPitStops.folderMandatoryPitStopsPitThisLap), 0, null), false);
                        audioPlayer.closeChannel();
                    } else
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderYouHavePenalty, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderYouDontHaveAPenalty, 0, null), false);
                    audioPlayer.closeChannel();
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.HAVE_I_SERVED_MY_PENALTY))
            {
                if (hasOutstandingPenalty)
                {
                    List<MessageFragment> messages = new List<MessageFragment>();
                    messages.Add(MessageFragment.Text(AudioPlayer.folderNo));
                    messages.Add(MessageFragment.Text(folderYouStillHavePenalty));
                    if (lapsCompleted - penaltyLap == 2)
                    {
                        messages.Add(MessageFragment.Text(MandatoryPitStops.folderMandatoryPitStopsPitThisLap));
                    }
                    audioPlayer.playClipImmediately(new QueuedMessage("noYouStillHaveAPenalty", messages, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("yesYouServedYourPenalty",
                        MessageContents(AudioPlayer.folderYes, folderPenaltyServed), 0, null), false);
                    audioPlayer.closeChannel();
                }
            } else if (voiceMessage.Contains(SpeechRecogniser.DO_I_STILL_HAVE_A_PENALTY))
            {
                if (hasOutstandingPenalty)
                {
                    List<MessageFragment> messages = new List<MessageFragment>();
                    messages.Add(MessageFragment.Text(AudioPlayer.folderYes));
                    messages.Add(MessageFragment.Text(folderYouStillHavePenalty));
                    if (lapsCompleted - penaltyLap == 2)
                    {
                        messages.Add(MessageFragment.Text(MandatoryPitStops.folderMandatoryPitStopsPitThisLap));
                    }
                    audioPlayer.playClipImmediately(new QueuedMessage("yesYouStillHaveAPenalty", messages, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("noYouServedYourPenalty",
                        MessageContents(AudioPlayer.folderNo, folderPenaltyServed), 0, null), false);
                    audioPlayer.closeChannel();
                }                
            }
        }
    }
}
