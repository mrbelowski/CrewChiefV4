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
        private const float DIST_TO_START_TO_ANNOUNCE_POS_REMINDER = 300.0f;

        // Number of updates FO Action and Driver name were the same.
        private int numUpdatesActionSame = 0;

        // Last FO Action and Driver name announced.
        private FrozenOrderAction currFrozenOrderAction = FrozenOrderAction.None;
        private string currDriverToFollow = null;

        // Next FO Action and Driver to be announced if it stays stable for a ACTION_STABLE_THRESHOLD times.
        private FrozenOrderAction newFrozenOrderAction = FrozenOrderAction.None;
        private string newDriverToFollow = null;

        private bool formationStandingStartAnnounced = false;
        private bool formationStandingPreStartReminderAnnounced = false;

        // sounds...
        public static String folderFollow = "frozen_order/follow";
        public static String folderInTheLeftColumn = "frozen_order/in_the_left_column";
        public static String folderInTheRightColumn = "frozen_order/in_the_right_column";
        public static String folderInTheInsideColumn = "frozen_order/in_the_inside_column";
        public static String folderInTheOutsideColumn = "frozen_order/in_the_outside_column";

        // for cases where we have no driver name:
        private String folderLineUpInLeftColumn = "frozen_order/line_up_in_the_left_column";
        private String folderLineUpInRightColumn = "frozen_order/line_up_in_the_right_column";
        private String folderLineUpInInsideColumn = "frozen_order/line_up_in_the_inside_column";
        private String folderLineUpInOutsideColumn = "frozen_order/line_up_in_the_outside_column";

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

        // Validation stuff:
        private const string validateMessageTypeKey = "validateMessageTypeKey";
        private const string validateMessageTypeAction = "validateMessageTypeAction";
        private const string validationActionKey = "validationActionKey";
        private const string validationAssignedPositionKey = "validationAssignedPositionKey";
        private const string validationDriverToFollowKey = "validationDriverToFollowKey";

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
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if (currentGameState.PitData.InPitlane)
                    return false;
               
                if (validationData == null)
                    return true;
               
                if ((string)validationData[FrozenOrderMonitor.validateMessageTypeKey] == FrozenOrderMonitor.validateMessageTypeAction)
                {
                    var queuedAction = (FrozenOrderAction)validationData[FrozenOrderMonitor.validationActionKey];
                    var queuedAssignedPosition = (int)validationData[FrozenOrderMonitor.validationAssignedPositionKey];
                    var queuedDriverToFollow = (string)validationData[FrozenOrderMonitor.validationDriverToFollowKey];
                    if (queuedAction == currentGameState.FrozenOrderData.Action
                        && queuedAssignedPosition == currentGameState.FrozenOrderData.AssignedPosition
                        && queuedDriverToFollow == currentGameState.FrozenOrderData.DriverToFollowRaw)
                    {
                        return true;
                    }
                    else
                    {
                        Console.WriteLine(string.Format("Frozen Order: message invalidated.  Was {0} {1} {2} is {3} {4} {5}", queuedAction, queuedAssignedPosition, queuedDriverToFollow,
                            currentGameState.FrozenOrderData.Action, currentGameState.FrozenOrderData.AssignedPosition, currentGameState.FrozenOrderData.DriverToFollowRaw));
                        return false;
                    }
                }
            }
            return true;
        }

        public override void clearState()
        {
            this.formationStandingStartAnnounced = false;
            this.formationStandingPreStartReminderAnnounced = false;
            this.numUpdatesActionSame = 0;
            this.newFrozenOrderAction = FrozenOrderAction.None;
            this.newDriverToFollow = null;
            this.currFrozenOrderAction = FrozenOrderAction.None;
            this.currDriverToFollow = null;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
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
                Console.WriteLine("Frozen Order: New Phase detected: " + cfod.Phase);

                if (cfod.Phase == FrozenOrderPhase.Rolling)
                    audioPlayer.playMessage(new QueuedMessage(folderRollingStartReminder, Utilities.random.Next(0, 3), this));
                else if (cfod.Phase == FrozenOrderPhase.FormationStanding)
                    audioPlayer.playMessage(new QueuedMessage(folderStandingStartReminder, Utilities.random.Next(0, 3), this));

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

                    // canReadDriverToFollow will be true if we're behind the safety car or we can read the driver's name:
                    Boolean canReadDriverToFollow = shouldFollowSafetyCar || AudioPlayer.canReadName(driverToFollow);

                    var usableDriverNameToFollow = shouldFollowSafetyCar ? driverToFollow : DriverNameHelper.getUsableDriverName(driverToFollow);

                    var validationData = new Dictionary<string, object>();
                    validationData.Add(FrozenOrderMonitor.validateMessageTypeKey, FrozenOrderMonitor.validationActionKey);
                    validationData.Add(FrozenOrderMonitor.validationAssignedPositionKey, cfod.AssignedPosition);
                    validationData.Add(FrozenOrderMonitor.validationDriverToFollowKey, cfod.DriverToFollowRaw);

                    if (this.newFrozenOrderAction == FrozenOrderAction.Follow
                        && prevDriverToFollow != this.currDriverToFollow)  // Don't announce Follow messages for the driver that we caught up to or allowed to pass.
                    {
                        if (canReadDriverToFollow)
                        { 
                            // Follow messages are only meaningful if there's name to announce.
                            if (cfod.AssignedColumn == FrozenOrderColumn.None
                                || Utilities.random.Next(1, 11) > 8)  // Randomly, announce message without coulmn info.
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, usableDriverNameToFollow), Utilities.random.Next(0, 3), this, validationData));
                            else
                            {
                                string columnName;
                                if (useOvalLogic)
                                    columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? folderInTheInsideColumn : folderInTheOutsideColumn;
                                else
                                    columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? folderInTheLeftColumn : folderInTheRightColumn;
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, usableDriverNameToFollow, columnName), Utilities.random.Next(0, 3), this, validationData));
                            }
                        }
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.AllowToPass)
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (canReadDriverToFollow && Utilities.random.Next(1, 11) > 2)  // Randomly, announce message without name.
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/allow_driver_to_pass", 
                                MessageContents(folderAllow, usableDriverNameToFollow, folderToPass), Utilities.random.Next(1, 4), this, validationData));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYoureAheadOfAGuyYouShouldBeFollowing, Utilities.random.Next(1, 4), this, validationData));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.CatchUp)
                    {
                        if (canReadDriverToFollow && Utilities.random.Next(1, 11) > 2)  // Randomly, announce message without name.
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/allow_driver_to_pass", 
                                MessageContents(folderCatchUpTo, usableDriverNameToFollow), Utilities.random.Next(1, 4), this, validationData));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYouNeedToCatchUpToTheGuyAhead, Utilities.random.Next(1, 4), this, validationData));
                    }
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

                    // canReadDriverToFollow will be true if we're behind the safety car or we can read the driver's name:
                    Boolean canReadDriverToFollow = shouldFollowSafetyCar || AudioPlayer.canReadName(driverToFollow);

                    var usableDriverNameToFollow = shouldFollowSafetyCar ? driverToFollow : DriverNameHelper.getUsableDriverName(driverToFollow);

                    var validationData = new Dictionary<string, object>();
                    validationData.Add(FrozenOrderMonitor.validateMessageTypeKey, FrozenOrderMonitor.validationActionKey);
                    validationData.Add(FrozenOrderMonitor.validationAssignedPositionKey, cfod.AssignedPosition);
                    validationData.Add(FrozenOrderMonitor.validationDriverToFollowKey, cfod.DriverToFollowRaw);

                    if (this.newFrozenOrderAction == FrozenOrderAction.Follow
                        && prevDriverToFollow != this.currDriverToFollow)  // Don't announce Follow messages for the driver that we caught up to or allowed to pass.
                    {
                        Boolean announceLane = currentGameState.StockCarRulesData.stockCarRulesEnabled && currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.LAST_LAP_NEXT;
                        if (canReadDriverToFollow)
                        {                            
                            if (announceLane && cfod.AssignedColumn == FrozenOrderColumn.Left)
                            {
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, usableDriverNameToFollow, 
                                        useOvalLogic ? folderInTheInsideColumn : folderInTheLeftColumn), Utilities.random.Next(0, 3), this, validationData));
                            }
                            else if (announceLane && cfod.AssignedColumn == FrozenOrderColumn.Right)
                            {
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver",
                                    MessageContents(folderFollow, usableDriverNameToFollow, 
                                        useOvalLogic ? folderInTheOutsideColumn : folderInTheRightColumn), Utilities.random.Next(0, 3), this, validationData));
                            }
                            else
                            {
                                audioPlayer.playMessage(new QueuedMessage("frozen_order/follow_driver", MessageContents(folderFollow, usableDriverNameToFollow), Utilities.random.Next(0, 3), this, validationData));
                            }
                        }
                        else if (announceLane && cfod.AssignedColumn == FrozenOrderColumn.Left)
                        {
                            audioPlayer.playMessage(new QueuedMessage(useOvalLogic ? folderLineUpInInsideColumn : folderLineUpInLeftColumn, 
                                Utilities.random.Next(0, 3), this, validationData));
                        }
                        else if (announceLane && cfod.AssignedColumn == FrozenOrderColumn.Right)
                        {
                            audioPlayer.playMessage(new QueuedMessage(useOvalLogic ? folderLineUpInOutsideColumn : folderLineUpInRightColumn,
                                Utilities.random.Next(0, 3), this, validationData));
                        }
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.AllowToPass)
                    {
                        if (canReadDriverToFollow && Utilities.random.Next(1, 11) > 2)  // Randomly, announce message without name.
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/allow_driver_to_pass", 
                                MessageContents(folderAllow, usableDriverNameToFollow, folderToPass), Utilities.random.Next(1, 4), this, validationData));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYoureAheadOfAGuyYouShouldBeFollowing, Utilities.random.Next(1, 4), this, validationData));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.CatchUp)
                    {
                        if (canReadDriverToFollow && Utilities.random.Next(1, 11) > 2)  // Randomly, announce message without name.
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/catch_up_to_driver", 
                                MessageContents(folderCatchUpTo, usableDriverNameToFollow), Utilities.random.Next(1, 4), this, validationData));
                        else
                            audioPlayer.playMessage(new QueuedMessage(folderYouNeedToCatchUpToTheGuyAhead, Utilities.random.Next(1, 4), this, validationData));
                    }
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
                            audioPlayer.playMessage(new QueuedMessage(folderWereStartingFromPole, 0, this));
                        else
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/youre_starting_from_pole_in_column",
                                    MessageContents(folderWereStartingFromPole, columnName), Utilities.random.Next(0, 3), this));
                    }
                    else
                    {
                        if (columnName == null)
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/youre_starting_from_pos",
                                    MessageContents(folderWeStartingFromPosition, cfod.AssignedPosition), Utilities.random.Next(0, 3), this));
                        else
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/youre_starting_from_pos_row_in_column",
                                    MessageContents(folderWeStartingFromPosition, cfod.AssignedPosition, folderRow, cfod.AssignedGridPosition, columnName), Utilities.random.Next(0, 3), this));
                    }
                }

                if (!this.formationStandingPreStartReminderAnnounced
                    && cgs.SessionData.SectorNumber == 3
                    && cgs.PositionAndMotionData.DistanceRoundTrack > (cgs.SessionData.TrackDefinition.trackLength - FrozenOrderMonitor.DIST_TO_START_TO_ANNOUNCE_POS_REMINDER))
                {
                    this.formationStandingPreStartReminderAnnounced = true;
                    var isStartingFromPole = cfod.AssignedPosition == 1;
                    if (isStartingFromPole)
                    {
                        if (columnName == null)
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_starting_from_pole",
                                    MessageContents(LapCounter.folderGetReady, folderWereStartingFromPole), Utilities.random.Next(0, 3), this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_starting_from_pole_in_column",
                                    MessageContents(LapCounter.folderGetReady, folderWereStartingFromPole, columnName), Utilities.random.Next(0, 3), this));
                        }
                    }
                    else
                    {
                        if (columnName == null)
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_youre_starting_from_pos",
                                    MessageContents(LapCounter.folderGetReady, folderWeStartingFromPosition, cfod.AssignedPosition), Utilities.random.Next(0, 3), this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("frozen_order/get_ready_youre_starting_from_pos_row_in_column",
                                    MessageContents(LapCounter.folderGetReady, folderWeStartingFromPosition, cfod.AssignedPosition, folderRow, cfod.AssignedGridPosition, columnName), Utilities.random.Next(0, 3), this));
                        }
                    }
                }
            }

            // Announce SC speed.
            if (pfod.SafetyCarSpeed == -1.0f && cfod.SafetyCarSpeed != -1.0f)
            {
                // TODO: may also need to announce basic "SC in" message.
                var kmPerHour = cfod.SafetyCarSpeed * 3.6f;
                var messageFragments = new List<MessageFragment>();
                if (useAmericanTerms)
                {
                    messageFragments.Add(MessageFragment.Text(FrozenOrderMonitor.folderPaceCarSpeedIs));
                    var milesPerHour = kmPerHour * 0.621371f;
                    messageFragments.Add(MessageFragment.Integer((int)Math.Round(milesPerHour), false));
                    messageFragments.Add(MessageFragment.Text(FrozenOrderMonitor.folderMilesPerHour));
                }
                else
                {
                    messageFragments.Add(MessageFragment.Text(FrozenOrderMonitor.folderSafetyCarSpeedIs));
                    messageFragments.Add(MessageFragment.Integer((int)Math.Round(kmPerHour), false));
                    messageFragments.Add(MessageFragment.Text(FrozenOrderMonitor.folderKilometresPerHour));
                }
                audioPlayer.playMessage(new QueuedMessage("frozen_order/pace_car_speed", messageFragments, Utilities.random.Next(10, 16), this));
            }

            // Announce SC left.
            if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
            {
                if (useAmericanTerms)
                    audioPlayer.playMessage(new QueuedMessage(folderPaceCarJustLeft, 0, this));
                else
                    audioPlayer.playMessage(new QueuedMessage(folderSafetyCarJustLeft, 0, this));
            }

            // For fast rolling, do nothing for now.
        }
    }
}
