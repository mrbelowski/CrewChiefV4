﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class OvertakingAidsMonitor : AbstractEvent
    {        
        public static String folderAFewTenthsOffDRSRange = "overtaking_aids/a_few_tenths_off_drs_range";
        public static String folderASecondOffDRSRange = "overtaking_aids/a_second_off_drs_range";
        public static String folderActivationsRemaining = "overtaking_aids/activations_remaining";
        public static String folderNoActivationsRemaining = "overtaking_aids/no_activations_remaining";
        public static String folderOneActivationRemaining = "overtaking_aids/one_activation_remaining";
        public static String folderDontForgetDRS = "overtaking_aids/dont_forget_drs"; 
        public static String folderGuyBehindHasDRS = "overtaking_aids/guy_behind_has_drs";
        public static String folderPushToPassNowAvailable = "overtaking_aids/push_to_pass_now_available";

        private Random random = new Random();

        private Boolean hasUsedDrsOnThisLap = false;    // Note that DTM 2015 experience has 3 DRS activations per lap - only moans if we've used none of them
        private Boolean drsAvailableOnThisLap = false;
        private Boolean drsAvailableInAnyZoneOnThisLap = false;  // In rF2, there are multiple DRS zones allowed.  Track if there was any moment user could've engaged DRS.
        private float trackDistanceToCheckDRSGapFrontAt = -1;

        private Boolean playedGetCloserForDRSOnThisLap = false;
        private Boolean playedOpponentHasDRSOnThisLap = false;

        private int pushToPassActivationsRemaining = 0;

        private Boolean drsMessagesEnabled = UserSettings.GetUserSettings().getBoolean("enable_drs_messages");
        private Boolean ptpMessagesEnabled = UserSettings.GetUserSettings().getBoolean("enable_push_to_pass_messages");

        public OvertakingAidsMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            this.hasUsedDrsOnThisLap = false;
            this.drsAvailableOnThisLap = false;
            this.drsAvailableInAnyZoneOnThisLap = false;
            this.trackDistanceToCheckDRSGapFrontAt = -1;
            this.playedOpponentHasDRSOnThisLap = false;
            this.playedGetCloserForDRSOnThisLap = false;
            this.pushToPassActivationsRemaining = 0;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (GameStateData.onManualFormationLap)
            {
                return;
            }
            // DRS:
            if (drsMessagesEnabled && currentGameState.OvertakingAids.DrsEnabled)
            {
                if (trackDistanceToCheckDRSGapFrontAt == -1 && currentGameState.SessionData.TrackDefinition != null)
                {
                    trackDistanceToCheckDRSGapFrontAt = currentGameState.SessionData.TrackDefinition.trackLength / 2;
                }
                if (currentGameState.SessionData.IsNewLap)
                {
                    if ((drsAvailableOnThisLap && !hasUsedDrsOnThisLap)
                        || (CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT && drsAvailableInAnyZoneOnThisLap && !hasUsedDrsOnThisLap))
                    {
                        audioPlayer.playMessage(new QueuedMessage("missed_available_drs", MessageContents(folderDontForgetDRS), 0, this));
                    }
                    drsAvailableOnThisLap = currentGameState.OvertakingAids.DrsAvailable;
                    drsAvailableInAnyZoneOnThisLap = currentGameState.OvertakingAids.DrsAvailable;
                    hasUsedDrsOnThisLap = false;
                    playedGetCloserForDRSOnThisLap = false;
                    playedOpponentHasDRSOnThisLap = false;
                }
                if (currentGameState.OvertakingAids.DrsAvailable)
                {
                    drsAvailableInAnyZoneOnThisLap = true;
                }
                if (currentGameState.OvertakingAids.DrsEngaged)
                {
                    hasUsedDrsOnThisLap = true;
                }
                bool hadChanceToUseDrs = CrewChief.gameDefinition.gameEnum != GameEnum.RF2_64BIT
                    ? drsAvailableOnThisLap : drsAvailableInAnyZoneOnThisLap;
                if (!hasUsedDrsOnThisLap && !hadChanceToUseDrs && !playedGetCloserForDRSOnThisLap &&
                    currentGameState.PositionAndMotionData.DistanceRoundTrack > trackDistanceToCheckDRSGapFrontAt)
                {
                    if (currentGameState.SessionData.TimeDeltaFront < 1.3 + currentGameState.OvertakingAids.DrsRange &&
                        currentGameState.SessionData.TimeDeltaFront >= 0.6 + currentGameState.OvertakingAids.DrsRange)
                    {
                        audioPlayer.playMessage(new QueuedMessage("drs_a_second_out_of_range", MessageContents(folderASecondOffDRSRange), 0, this));
                        playedGetCloserForDRSOnThisLap = true;
                    }
                    else if (currentGameState.SessionData.TimeDeltaFront < 0.6 + currentGameState.OvertakingAids.DrsRange &&
                        currentGameState.SessionData.TimeDeltaFront >= 0.1 + currentGameState.OvertakingAids.DrsRange)
                    {
                        audioPlayer.playMessage(new QueuedMessage("drs_a_few_tenths_out_of_range", MessageContents(folderAFewTenthsOffDRSRange), 0, this));
                        playedGetCloserForDRSOnThisLap = true;
                    }
                }
                if (!playedOpponentHasDRSOnThisLap && currentGameState.SessionData.TimeDeltaBehind <= currentGameState.OvertakingAids.DrsRange && 
                    currentGameState.SessionData.LapTimeCurrent > currentGameState.SessionData.TimeDeltaBehind &&
                    currentGameState.SessionData.LapTimeCurrent < currentGameState.SessionData.TimeDeltaBehind + 1 &&
                    currentGameState.OvertakingAids.DrsAvailable)
                {
                    string opponentBehindKey = currentGameState.getOpponentKeyBehind(false);
                    playedOpponentHasDRSOnThisLap = true;
                    if (random.NextDouble() >= 0.4 && opponentBehindKey != null && !currentGameState.OpponentData[opponentBehindKey].isEnteringPits() &&
                        !currentGameState.OpponentData[opponentBehindKey].isExitingPits()) { 
                        audioPlayer.playMessage(new QueuedMessage("opponent_has_drs", MessageContents(folderGuyBehindHasDRS), 0, this));                        
                    }
                }
            }

            // push to pass
            if (ptpMessagesEnabled && previousGameState != null)
            {
                if (previousGameState.OvertakingAids.PushToPassEngaged && !currentGameState.OvertakingAids.PushToPassEngaged &&
                    currentGameState.OvertakingAids.PushToPassActivationsRemaining == 0)
                {
                    audioPlayer.playMessage(new QueuedMessage("no_push_to_pass_remaining", MessageContents(folderNoActivationsRemaining), 0, this));
                    pushToPassActivationsRemaining = 0;
                }
                else if (previousGameState.OvertakingAids.PushToPassWaitTimeLeft > 0 && currentGameState.OvertakingAids.PushToPassWaitTimeLeft == 0)
                {
                    if (currentGameState.OvertakingAids.PushToPassActivationsRemaining == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage("one_push_to_pass_remaining", MessageContents(
                            folderPushToPassNowAvailable, folderOneActivationRemaining), 0, this));
                        pushToPassActivationsRemaining = 1;
                    }
                    else if (currentGameState.OvertakingAids.PushToPassActivationsRemaining > 0)
                    {
                        audioPlayer.playMessage(new QueuedMessage("push_to_pass_remaining", MessageContents(folderPushToPassNowAvailable,
                            currentGameState.OvertakingAids.PushToPassActivationsRemaining, folderActivationsRemaining), 0, this));
                        pushToPassActivationsRemaining = currentGameState.OvertakingAids.PushToPassActivationsRemaining;
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage("no_push_to_pass_remaining", MessageContents(folderNoActivationsRemaining), 0, this));
                        pushToPassActivationsRemaining = 0;
                    }
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            // TODO - "how many activations left?" response? Is this needed?
        }
    }
}
