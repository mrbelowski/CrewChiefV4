/*
 * This monitor announces order information during Full Course Yellow/Yellow (in NASCAR), Rolling start and Formation/standing starts.
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class FrozenOrderMonitor : AbstractEvent
    {
        private const int ACTION_STABLE_THRESHOLD = 3;

        // Number of updates FO Action and Driver name were the same.
        private int numUpdatesActionSame = 0;

        // Last FO Action and Driver name announced.
        private FrozenOrderAction currFrozenOrderAction = FrozenOrderAction.None;
        private string currDriverToFollow = "";

        // Next FO Action and Driver to be announced if it stays stable for a ACTION_STABLE_THRESHOLD times.
        private FrozenOrderAction newFrozenOrderAction = FrozenOrderAction.None;
        private string newDriverToFollow = "";

        private bool formationStandingStartAnnounced = false;
        private bool formationStandingPreStartReminderAnnounced = false;

        // sounds...
        public static String folderFollow = "frozen_order/follow";
        public static String folderInTheLeftColumn = "frozen_order/in_the_left_column";
        public static String folderInTheRightColumn = "frozen_order/in_the_right_column";
        public static String folderInTheInsideColumn = "frozen_order/in_the_inside_column";
        public static String folderInTheOutsideColumn = "frozen_order/in_the_outside_column";
        public static String folderCatchUpTo = "frozen_order/catch_up_to";    // can we have multiple phrasings of this without needing different structure?
        public static String folderAllow = "frozen_order/allow";
        public static String folderToPass = "frozen_order/to_pass";
        public static String folderTheSafetyCar = "frozen_order/the_safety_car";
        public static String folderThePaceCar = "frozen_order/the_pace_car";
        public static String folderYoureAheadOfAGuyYouShouldBeFollowing = "frozen_order/youre_ahead_of_guy_you_should_follow";
        public static String folderYouNeedToCatchUpToTheGuyAhead = "frozen_order/you_need_to_catch_up_to_the_guy_ahead";
        public static String folderAllowGuyBehindToPass = "frozen_order/allow_guy_behind_to_pass";

        public static String folderWeStartingFromPosition = "frozen_order/were_starting_from_position";
        public static String folderRow = "frozen_order/row";    // "starting from position 4, row 2 in the outside column" - uses column stuff above
        // we'll use the get-ready sound from the LapCounter event here
        public static String folderWereStartingFromPole = "frozen_order/were_starting_from_pole";
        public static String folderSafetyCarSpeedIs = "frozen_order/safety_car_speed_is";
        public static String folderPaceCarSpeedIs = "frozen_order/pace_car_speed_is";
        public static String folderMilesPerHour = "frozen_order/miles_per_hour";
        public static String folderKilometresPerHour = "frozen_order/kilometres_per_hour";
        public static String folderSafetyCarJustLeft = "frozen_order/safety_car_just_left"; // left the pits?
        public static String folderPaceCarJustLeft = "frozen_order/pace_car_just_left"; // left the pits?
        public static String folderRollingStartReminder = "frozen_order/thats_a_rolling_start";
        public static String folderStandingStartReminder = "frozen_order/thats_a_standing_start";

        public FrozenOrderMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Race }; }
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Formation, SessionPhase.FullCourseYellow }; }
        }

        /*
         * IMPORTANT: This method is called twice - when the message becomes due, and immediately before playing it (which may have a 
         * delay caused by the length of the queue at the time). So be *very* careful when checking and updating local state in here.
         */
        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            /*if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if (currentGameState.PitData.InPitlane)
                {
                    return false;
                }
                if (validationData == null) {
                    return true;
                }
                if ((Boolean) validationData[isValidatingSectorMessage])
                {                    
                    int sectorIndex = (int)validationData[validationSectorNumberKey];
                    FlagEnum sectorFlagWhenQueued = (FlagEnum)validationData[validationSectorFlagKey];
                    if (lastSectorFlags[sectorIndex] == sectorFlagWhenQueued)
                    {
                        lastSectorFlagsReported[sectorIndex] = sectorFlagWhenQueued;
                        lastSectorFlagsReportedTime[sectorIndex] = currentGameState.Now;
                        //Console.WriteLine("FLAG_DEBUG: transition to sector " + (sectorIndex + 1) + " " + sectorFlagWhenQueued + " is valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return true;
                    }
                    else
                    {
                        // reset the last reported flag and the time so we can report this flag transition when it's actually valid:
                        lastSectorFlagsReported[sectorIndex] = sectorFlagWhenQueued == FlagEnum.YELLOW ? FlagEnum.GREEN : FlagEnum.YELLOW;
                        lastSectorFlagsReportedTime[sectorIndex] = DateTime.MinValue;
                        //Console.WriteLine("FLAG_DEBUG: transition to sector " + (sectorIndex + 1) + " " + sectorFlagWhenQueued + " is NOT valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return false;
                    }
                }
                else
                {
                    Boolean wasLocalYellow = (Boolean)validationData[validationIsLocalYellowKey];
                    if (currentGameState.FlagData.isLocalYellow && wasLocalYellow)
                    {
                        hasReportedIsUnderLocalYellow = true;
                        lastSectorFlagsReported[currentGameState.SessionData.SectorNumber - 1] = FlagEnum.YELLOW;
                        lastSectorFlagsReportedTime[currentGameState.SessionData.SectorNumber - 1] = currentGameState.Now;
                        //Console.WriteLine("FLAG_DEBUG: transition to local YELLOW is valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return true;
                    }
                    else if (!currentGameState.FlagData.isLocalYellow && !wasLocalYellow && !currentGameState.PitData.InPitlane)
                    {
                        hasReportedIsUnderLocalYellow = false;
                        // don't change the local sector state to green here - it might remain yellow after we pass the incident
                        // lastSectorFlagsReported[currentGameState.SessionData.SectorNumber - 1] = FlagEnum.GREEN;
                        lastSectorFlagsReportedTime[currentGameState.SessionData.SectorNumber - 1] = currentGameState.Now;
                        //Console.WriteLine("FLAG_DEBUG: transition to local GREEN is valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return true;
                    }
                    else
                    {
                       // Console.WriteLine("FLAG_DEBUG: transition to local " + (wasLocalYellow ? "YELLOW" : "GREEN") + " is NOT valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                    }
                }
            }*/
            return true;
        }

        public override void clearState()
        {
            this.formationStandingStartAnnounced = false;
            this.formationStandingPreStartReminderAnnounced = false;
            this.numUpdatesActionSame = 0;
            this.newFrozenOrderAction = FrozenOrderAction.None;
            this.newDriverToFollow = "";
            this.currFrozenOrderAction = FrozenOrderAction.None;
            this.currDriverToFollow = "";
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            /*
            rolling\recording_2017-22-9--09-45-31.xml
            */
            var cgs = currentGameState;
            var pgs = previousGameState;
            if (cgs.PitData.InPitlane /*|| cgs.SessionData.SessionRunningTime < 10 */
                || GameStateData.onManualFormationLap  // We may want manual formation to phase of FrozenOrder.
                || pgs == null)
                return; // don't process if we're in the pits or just started a session

            var cfod = cgs.FrozenOrderData;
            var pfod = pgs.FrozenOrderData;
            var cfodp = cgs.FrozenOrderData.Phase;
            if (cfodp == FrozenOrderPhase.None)
                return;  // Nothing to do.

            var useAmericanTerms = GlobalBehaviourSettings.useAmericanTerms || GlobalBehaviourSettings.useOvalLogic;
            var useOvalLogic = GlobalBehaviourSettings.useOvalLogic;

            if (pfod.Phase == FrozenOrderPhase.None)
            {
                Console.WriteLine("FO: NEW PHASE DETECTED: " + cfod.Phase);

                if (cfod.Phase == FrozenOrderPhase.Rolling)
                    audioPlayer.playMessage(new QueuedMessage(folderRollingStartReminder, 0, this));
                else if (cfod.Phase == FrozenOrderPhase.FormationStanding)
                    audioPlayer.playMessage(new QueuedMessage(folderStandingStartReminder, 0, this));

                // Clear previous state.
                this.clearState();
            }

            // Because FO Action is distance dependent, it tends to fluctuate.
            // We need to detect when it stabilizes (values stay identical for ACTION_STABLE_THRESHOLD times).
            if (cfod.Action == pfod.Action
                && cfod.DriverToFollowRaw == pfod.DriverToFollowRaw)
                ++this.numUpdatesActionSame;
            else
            {
                this.newFrozenOrderAction = cfod.Action;
                this.newDriverToFollow = cfod.DriverToFollowRaw;

                this.numUpdatesActionSame = 0;
            }

            
            var isActionUpdateStable = this.numUpdatesActionSame >= FrozenOrderMonitor.ACTION_STABLE_THRESHOLD;

            if (cfodp == FrozenOrderPhase.Rolling)
            {
                var shouldFollowSafetyCar = cfod.AssignedGridPosition == 1;
                var driverToFollow = shouldFollowSafetyCar ? (useAmericanTerms ? folderThePaceCar : folderTheSafetyCar) : cfod.DriverToFollowRaw;

                var prevDriverToFollow = this.currDriverToFollow;

                if (isActionUpdateStable
                    && (this.currFrozenOrderAction != this.newFrozenOrderAction
                        || this.currDriverToFollow != this.newDriverToFollow))
                {
                    this.currFrozenOrderAction = this.newFrozenOrderAction;
                    this.currDriverToFollow = this.newDriverToFollow;

                    var usableDriverNameToFollow = DriverNameHelper.getUsableDriverName(driverToFollow);
                    if (this.newFrozenOrderAction == FrozenOrderAction.Follow
                        && prevDriverToFollow != this.currDriverToFollow)  // Don't announce Follow messages for the driver that we caught up to or allowed to pass.
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(usableDriverNameToFollow))
                        {
                            if (cfod.AssignedColumn == FrozenOrderColumn.None)
                            {
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, driverToFollow), 0, this));
                            }
                            else
                            {
                                string columnName;
                                if (useOvalLogic)
                                    columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? folderInTheInsideColumn : folderInTheOutsideColumn;
                                else
                                    columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? folderInTheLeftColumn : folderInTheRightColumn;
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, usableDriverNameToFollow, columnName), 0, this));
                            }
                        }
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.AllowToPass)
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(usableDriverNameToFollow))
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/allow_driver_to_pass", 
                                MessageContents(folderAllow, usableDriverNameToFollow, folderToPass), 0, this));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYoureAheadOfAGuyYouShouldBeFollowing, 0, this));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.CatchUp)
                    {
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(usableDriverNameToFollow))
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/allow_driver_to_pass", 
                                MessageContents(folderCatchUpTo, usableDriverNameToFollow), 0, this));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYouNeedToCatchUpToTheGuyAhead, 0, this));
                    }
                }

                if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
                {
                    // TODO: pace car speed (if we want it) - need to get the units right for this
                    if (useAmericanTerms)
                        audioPlayer.playMessage(new QueuedMessage(folderPaceCarJustLeft, 0, this));
                    else
                        audioPlayer.playMessage(new QueuedMessage(folderSafetyCarJustLeft, 0, this));
                }
            }
            else if (cfodp == FrozenOrderPhase.FullCourseYellow)
            {
                var shouldFollowSafetyCar = cfod.AssignedPosition == 1;
                var driverToFollow = shouldFollowSafetyCar ? (useAmericanTerms ? folderThePaceCar : folderTheSafetyCar) : cfod.DriverToFollowRaw;

                var prevDriverToFollow = this.currDriverToFollow;

                if (isActionUpdateStable
                    && (this.currFrozenOrderAction != this.newFrozenOrderAction
                        || this.currDriverToFollow != this.newDriverToFollow))
                {
                    this.currFrozenOrderAction = this.newFrozenOrderAction;
                    this.currDriverToFollow = this.newDriverToFollow;

                    var usableDriverNameToFollow = DriverNameHelper.getUsableDriverName(driverToFollow);
                    if (this.newFrozenOrderAction == FrozenOrderAction.Follow
                        && prevDriverToFollow != this.currDriverToFollow)  // Don't announce Follow messages for the driver that we caught up to or allowed to pass.)
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(usableDriverNameToFollow))
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, usableDriverNameToFollow), 0, this));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.AllowToPass)
                    {
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(usableDriverNameToFollow))
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/allow_driver_to_pass", 
                                MessageContents(folderAllow, usableDriverNameToFollow, folderToPass), 0, this));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYoureAheadOfAGuyYouShouldBeFollowing, 0, this));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.CatchUp)
                    {
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(usableDriverNameToFollow))
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/catch_up_to_driver", 
                                MessageContents(folderCatchUpTo, usableDriverNameToFollow), 0, this));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYouNeedToCatchUpToTheGuyAhead, 0, this));
                    }
                }

                if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
                {
                    // TODO: pace car speed (if we want it) - need to get the units right for this
                    if (useAmericanTerms)
                        audioPlayer.playMessage(new QueuedMessage(folderPaceCarJustLeft, 0, this));
                    else
                        audioPlayer.playMessage(new QueuedMessage(folderSafetyCarJustLeft, 0, this));
                }
            }
            else if (cfodp == FrozenOrderPhase.FormationStanding)
            {
                string columnName = null;
                if (cfod.AssignedColumn != FrozenOrderColumn.None)
                {
                    if (useOvalLogic)
                        columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? folderInTheInsideColumn : folderInTheOutsideColumn;
                    else
                        columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? folderInTheLeftColumn : folderInTheRightColumn;
                }
                if (!this.formationStandingStartAnnounced && cgs.SessionData.SessionRunningTime > 10)
                {
                    this.formationStandingStartAnnounced = true;
                    var isStartingFromPole = cfod.AssignedPosition == 1;
                    if (isStartingFromPole)
                    {
                        if (columnName == null)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderWereStartingFromPole, 0, this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/youre_starting_from_pole_in_column",
                                    MessageContents(folderWereStartingFromPole, columnName), 0, this));
                        }
                    }
                    else
                    {
                        if (columnName == null)
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/youre_starting_from_pos",
                                    MessageContents(folderWeStartingFromPosition, cfod.AssignedPosition), 0, this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/youre_starting_from_pos_row_in_column",
                                    MessageContents(folderWeStartingFromPosition, cfod.AssignedPosition, folderRow, cfod.AssignedGridPosition, columnName), 0, this));
                        }
                    }
                }

                // TODO: Not sure how will this play with formation lap message in S3.
                if (!this.formationStandingPreStartReminderAnnounced
                    && cgs.SessionData.SectorNumber == 3
                    && cgs.PositionAndMotionData.DistanceRoundTrack > cgs.SessionData.TrackDefinition.distanceForNearPitEntryChecks)
                {
                    this.formationStandingPreStartReminderAnnounced = true;
                    var isStartingFromPole = cfod.AssignedPosition == 1;
                    if (isStartingFromPole)
                    {
                        if (columnName == null)
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_starting_from_pole",
                                    MessageContents(LapCounter.folderGetReady, folderWeStartingFromPosition), 0, this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_starting_from_pole_in_column",
                                    MessageContents(LapCounter.folderGetReady, folderWeStartingFromPosition, columnName), 0, this));
                        }
                    }
                    else
                    {
                        if (columnName == null)
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_youre_starting_from_pos",
                                    MessageContents(LapCounter.folderGetReady, folderWeStartingFromPosition, cfod.AssignedPosition), 0, this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_youre_starting_from_pos_row_in_column",
                                    MessageContents(LapCounter.folderGetReady, folderWeStartingFromPosition, cfod.AssignedPosition, folderRow, cfod.AssignedGridPosition, columnName), 0, this));
                        }
                    }
                }
            }

            // For fast rolling, do nothing for now.
        }
    }
}
