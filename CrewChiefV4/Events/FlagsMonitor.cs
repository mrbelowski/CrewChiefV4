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
    class FlagsMonitor : AbstractEvent
    {
        private String folderBlueFlag = "flags/blue_flag";
        private String folderYellowFlag = "flags/yellow_flag";
        private String folderDoubleYellowFlag = "flags/double_yellow_flag";
        private String folderWhiteFlagEU = "flags/white_flag";
        private String folderBlackFlag = "flags/black_flag";

        private DateTime disableYellowFlagUntil = DateTime.MinValue;
        private DateTime disableBlackFlagUntil = DateTime.MinValue;
        private DateTime disableWhiteFlagUntil = DateTime.MinValue;
        private DateTime disableBlueFlagUntil = DateTime.MinValue;

        private TimeSpan timeBetweenYellowFlagMessages = TimeSpan.FromSeconds(30);
        private TimeSpan timeBetweenBlueFlagMessages = TimeSpan.FromSeconds(20);
        private TimeSpan timeBetweenBlackFlagMessages = TimeSpan.FromSeconds(20);
        private TimeSpan timeBetweenWhiteFlagMessages = TimeSpan.FromSeconds(20);

        private String folderFCYellowStartEU = "flags/fc_yellow_start_eu";
        private String folderFCYellowPitsClosedEU = "flags/fc_yellow_pits_closed_eu";
        private String folderFCYellowPitsOpenLeadLapCarsEU = "flags/fc_yellow_pits_open_lead_lap_cars_eu";
        private String folderFCYellowPitsOpenEU = "flags/fc_yellow_pits_open_eu";
        private String folderFCYellowLastLapNextEU = "flags/fc_yellow_last_lap_next_eu";
        private String folderFCYellowLastLapCurrentEU = "flags/fc_yellow_last_lap_current_eu";
        private String folderFCYellowPrepareForGreenEU = "flags/fc_yellow_prepare_for_green_eu";
        private String folderFCYellowInProgressEU = "flags/fc_yellow_in_progress_eu";
        private String folderFCYellowStartUS = "flags/fc_yellow_start_usa";
        private String folderFCYellowPitsClosedUS = "flags/fc_yellow_pits_closed_usa";
        private String folderFCYellowPitsOpenLeadLapCarsUS = "flags/fc_yellow_pits_open_lead_lap_cars_usa";
        private String folderFCYellowPitsOpenUS = "flags/fc_yellow_pits_open_usa";
        private String folderFCYellowLastLapNextUS = "flags/fc_yellow_last_lap_next_usa";
        private String folderFCYellowLastLapCurrentUS = "flags/fc_yellow_last_lap_current_usa";
        private String folderFCYellowPrepareForGreenUS = "flags/fc_yellow_prepare_for_green_usa";
        private String folderFCYellowInProgressUS = "flags/fc_yellow_in_progress_usa";
        private String folderFCYellowGreenFlag = "flags/fc_yellow_green_flag";

        private String[] folderYellowFlagSectors = new String[] { "flags/yellow_flag_sector_1", "flags/yellow_flag_sector_2", "flags/yellow_flag_sector_3" };
        private String[] folderDoubleYellowFlagSectors = new String[] { "flags/double_yellow_flag_sector_1", "flags/double_yellow_flag_sector_2", "flags/double_yellow_flag_sector_3" };
        private String[] folderGreenFlagSectors = new String[] { "flags/green_flag_sector_1", "flags/green_flag_sector_2", "flags/green_flag_sector_3" };

        private String folderLocalYellow = "flags/local_yellow_flag";
        private String folderLocalYellowClear = "flags/local_yellow_clear";
        private String folderLocalYellowAhead = "flags/local_yellow_ahead";

        public static String[] folderPositionHasGoneOff = new String[] { "flags/position1_has_gone_off", "flags/position2_has_gone_off", "flags/position3_has_gone_off", 
                                                                   "flags/position4_has_gone_off", "flags/position5_has_gone_off", "flags/position6_has_gone_off", };
        private String[] folderPositionHasGoneOffIn = new String[] { "flags/position1_has_gone_off_in", "flags/position2_has_gone_off_in", "flags/position3_has_gone_off_in", 
                                                                   "flags/position4_has_gone_off_in", "flags/position5_has_gone_off_in", "flags/position6_has_gone_off_in", };
        private String folderNameHasGoneOffIntro = "flags/name_has_gone_off_intro";
        private String folderNameHasGoneOffOutro = "flags/name_has_gone_off_outro";
        //  used when we know the corner name:
        private String folderNameHasGoneOffInOutro = "flags/name_has_gone_off_in_outro";

        private String folderNamesHaveGoneOffOutro = "flags/names_have_gone_off_outro";
        private String folderNamesHaveGoneOffInOutro = "flags/names_have_gone_off_in_outro";
        private String folderAnd = "flags/and";

        private String folderPileupInCornerIntro = "flags/pileup_in_corner_intro";

        private String folderIncidentInCornerIntro = "flags/incident_in_corner_intro";
        private String folderIncidentInCornerDriverIntro = "flags/incident_in_corner_with_driver_intro";

        private String folderGivePositionsBackFirstWarningIntro = "flags/give_positions_back_first_warning_intro";
        private String folderGivePositionsBackNextWarningIntro = "flags/give_positions_back_next_warning_intro";
        private String folderGiveOnePositionBackFirstWarning = "flags/give_one_position_back_first_warning";
        private String folderGiveOnePositionsBackNextWarning = "flags/give_one_position_back_next_warning";
        private String folderGivePositionsBackFirstWarningOutro = "flags/give_positions_back_first_warning_outro";
        private String folderGivePositionsBackNextWarningOutro = "flags/give_positions_back_next_warning_outro";
        private String folderGivePositionsBackCompleted = "flags/give_positions_back_completed";

        private String folderNoOvertaking = "flags/no_overtaking";
        private String folderClearToOvertake = "flags/clear_to_overtake";

        // StockCarRulesData states
        private String folderLeaderChooseLane = "flags/choose_a_lane_by_staying_left_or_right";
        private String folderOpponentIsLuckyDog = "flags/is_the_lucky_dog";
        private String folderAllowLuckyDogPass = "flags/let_the_lucky_dog_pass_on_left";
        private String folderWeAreLuckyDog = "flags/we_are_the_lucky_dog";
        private String folderEOLLPenalty = "flags/move_to_end_of_longest_line_for_penalty";
        private String folderWeHaveBeenWavedAround = "flags/we_have_been_waved_around";
        private String folderPassDriverForLuckyDogPositionIntro = "flags/pass_drivername_for_lucky_dog_position_intro";
        private String folderPassDriverForLuckyDogPositionOutro = "flags/pass_drivername_for_lucky_dog_position_outro";
        private String folderPassCarAheadForLuckyPositionDog = "flags/pass_this_guy_to_get_lucky_dog_position";
        private String folderWeAreInLuckyDogPosition = "flags/we_are_in_lucky_dog_position";

        // green flag lucky-dog tracking
        private enum GreenFlagLuckyDogStatus { NONE, PASS_FOR_LUCKY_DOG, WE_ARE_IN_LUCKY_DOG }
        private TimeSpan minTimeBetweenGreenFlagLuckyDogMessages = TimeSpan.FromMinutes(2);
        private TimeSpan greenFlagLuckyDogCheckInterval = TimeSpan.FromSeconds(15);
        private int maxGreenFlagLuckyDogMessagesPerSession = 2;
        private GreenFlagLuckyDogStatus lastGreenFlagLuckyDogStatusAnnounced = GreenFlagLuckyDogStatus.NONE;
        private DateTime nextGreenFlagLuckyDogCheckDue = DateTime.MinValue;
        private int greenFlagLuckyDogMessageCountInSession = 0;
        //

        private int maxDistanceMovedForYellowAnnouncement = UserSettings.GetUserSettings().getInt("max_distance_moved_for_yellow_announcement");
        private Boolean reportAllowedOvertakesUnderYellow = UserSettings.GetUserSettings().getBoolean("report_allowed_overtakes_under_yellow");
        
        // for new (RF2 and R3E) impl
        private FlagEnum[] lastSectorFlags = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
        private FlagEnum[] lastSectorFlagsReported = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
        private DateTime[] lastSectorFlagsReportedTime = new DateTime[] { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        private FullCourseYellowPhase lastFCYAnnounced = FullCourseYellowPhase.RACING;
        private DateTime lastFCYAccountedTime = DateTime.MinValue;
        private TimeSpan timeBetweenYellowAndClearFlagMessages = TimeSpan.FromSeconds(3);
        private TimeSpan minTimeBetweenNewYellowFlagMessages = TimeSpan.FromSeconds(10);

        // do we need this?
        private DateTime lastLocalYellowAnnouncedTime = DateTime.MinValue;
        private DateTime lastLocalYellowClearAnnouncedTime = DateTime.MinValue;
        private DateTime lastOvertakeAllowedReportTime = DateTime.MinValue;
        private Boolean isUnderLocalYellow = false;
        private Boolean hasReportedIsUnderLocalYellow = false;
        private Boolean hasWarnedOfUpcomingIncident = false;

        private DateTime nextIncidentDriversCheck = DateTime.MaxValue;

        private TimeSpan fcyStatusReminderMinTime = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("time_between_caution_period_status_reminders"));

        private Boolean reportYellowsInAllSectors = UserSettings.GetUserSettings().getBoolean("report_yellows_in_all_sectors");
        private Boolean enableSimpleIncidentDetection = UserSettings.GetUserSettings().getBoolean("enable_simple_incident_detection");
        private Boolean enableOpponentCrashMessages = UserSettings.GetUserSettings().getBoolean("enable_opponent_crash_messages");
        
        private float maxDistanceToWarnOfLocalYellow = 300;    // metres - externalise? Is this sufficient? Make it speed-dependent?
        private float minDistanceToWarnOfLocalYellow = 50;    // metres - externalise? Is this sufficient? Make it speed-dependent?

        List<IncidentCandidate> incidentCandidates = new List<IncidentCandidate>();

        private int playerClassPositionAtStartOfIncident = int.MaxValue;

        private Dictionary<string, DateTime> incidentWarnings = new Dictionary<string, DateTime>();

        private TimeSpan incidentRepeatFrequency = TimeSpan.FromSeconds(30);

        private int getInvolvedInIncidentAttempts = 0;

        private int maxFCYGetInvolvedInIncidentAttempts = 5;

        private TimeSpan incidentDriversCheckInterval = TimeSpan.FromSeconds(1.5);

        private int maxLocalYellowGetInvolvedInIncidentAttempts = 2;

        private int maxDriversToReportAsInvolvedInIncident = 3;

        private int pileupDriverCount = 4;

        int waitingForCrashedDriverInSector = -1;

        private Dictionary<string, float> lastIncidentPositionForOpponents = new Dictionary<string, float>();

        private List<NamePositionPair> driversInvolvedInCurrentIncident = new List<NamePositionPair>();

        private String waitingForCrashedDriverInCorner = null;
        private List<String> driversCrashedInCorner = new List<String>();
        private DateTime waitingForCrashedDriverInCornerFinishTime = DateTime.MaxValue;

        // this will be initialised to something sensible once a yellow has been shown - if no yellow is ever 
        // shown we never want to check for illegal passes
        private DateTime nextIllegalPassWarning = DateTime.MaxValue;
        private TimeSpan illegalPassRepeatInterval = TimeSpan.FromSeconds(7);
        private int illegalPassCarsCountAtLastAnnouncement = 0;
        private Boolean hasAlreadyWarnedAboutIllegalPass = false;

        private TimeSpan incidentAheadSettlingTime = TimeSpan.FromSeconds(2);
        private DateTime incidentAheadSettledTime = DateTime.MinValue;
        private Boolean waitingToWarnOfIncident = false;

        private static String validationSectorNumberKey = "sectorNumber"; 
        private static String validationSectorFlagKey = "sectorFlag";
        private static String validationIsLocalYellowKey = "isLocalYellow";
        private static String sectorFlagChangeMessageKeyStart = "sectorFlagChange_Sector";
        private static String localFlagChangeMessageKey = "localFlagChange";
        private static String isValidatingSectorMessage = "isValidatingSectorMessage";
        
        private PassAllowedUnderYellow lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;

        private int blueFlagWarningCountForSingleDriver = 0;
        private String opponentWhoTriggeredLastBlueFlag = null;

        public FlagsMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Checkered, SessionPhase.FullCourseYellow }; }
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
                {
                    return false;
                }
                if (eventSubType == folderFCYellowPitsClosedEU || eventSubType == folderFCYellowPitsClosedUS)
                {
                    return currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.PITS_CLOSED;
                }
                else if (eventSubType == folderFCYellowPitsOpenEU || eventSubType == folderFCYellowPitsOpenUS)
                {
                    return currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.PITS_OPEN;
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
            }
            return false;           
        }

        public override void clearState()
        {
            disableYellowFlagUntil = DateTime.MinValue;
            disableWhiteFlagUntil = DateTime.MinValue;
            disableBlackFlagUntil = DateTime.MinValue;
            disableBlueFlagUntil = DateTime.MinValue;

            lastSectorFlags = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
            lastSectorFlagsReported = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
            lastSectorFlagsReportedTime = new DateTime[] { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
            nextIncidentDriversCheck = DateTime.MaxValue;
            lastFCYAnnounced = FullCourseYellowPhase.RACING;
            lastFCYAccountedTime = DateTime.MinValue;
            lastLocalYellowAnnouncedTime = DateTime.MinValue;
            lastLocalYellowClearAnnouncedTime = DateTime.MinValue;
            lastOvertakeAllowedReportTime = DateTime.MinValue;
            isUnderLocalYellow = false;
            hasWarnedOfUpcomingIncident = false;
            playerClassPositionAtStartOfIncident = int.MaxValue;
            incidentCandidates.Clear();
            incidentWarnings.Clear();
            getInvolvedInIncidentAttempts = 0;
            driversInvolvedInCurrentIncident.Clear();
            waitingForCrashedDriverInSector = -1;

            waitingForCrashedDriverInCorner = null;
            driversCrashedInCorner.Clear();
            waitingForCrashedDriverInCornerFinishTime = DateTime.MaxValue;

            nextIllegalPassWarning = DateTime.MaxValue;
            illegalPassCarsCountAtLastAnnouncement = 0;
            hasAlreadyWarnedAboutIllegalPass = false;

            lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;

            incidentAheadSettledTime = DateTime.MinValue;
            waitingToWarnOfIncident = false;
            hasReportedIsUnderLocalYellow = false;

            lastGreenFlagLuckyDogStatusAnnounced = GreenFlagLuckyDogStatus.NONE;
            greenFlagLuckyDogMessageCountInSession = 0;
            nextGreenFlagLuckyDogCheckDue = DateTime.MinValue;

            lastIncidentPositionForOpponents.Clear();

            opponentWhoTriggeredLastBlueFlag = null;
            blueFlagWarningCountForSingleDriver = 0;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.PitData.InPitlane || currentGameState.SessionData.SessionRunningTime < 10 || GameStateData.onManualFormationLap)
            {
                // don't process if we're in the pits or just started a session
                return;
            }
            if (currentGameState.SessionData.JustGoneGreen)
            {
                // ensure blue & white flags aren't enabled immediately:
                disableBlueFlagUntil = currentGameState.Now.Add(timeBetweenYellowFlagMessages);
            }
            if (currentGameState.FlagData.useImprovisedIncidentCalling)
            {
                improvisedYellowFlagImplementation(previousGameState, currentGameState);
            }
            else
            {
                gameDataYellowFlagImplementation(previousGameState, currentGameState);
            }
            //  now other flags
            if (currentGameState.PositionAndMotionData.CarSpeed < 1)
            {
                return;
            }
            if (currentGameState.SessionData.Flag == FlagEnum.BLACK)
            {
                if (currentGameState.Now > disableBlackFlagUntil)
                {
                    disableBlackFlagUntil = currentGameState.Now.Add(timeBetweenBlackFlagMessages);
                    audioPlayer.playMessage(new QueuedMessage(folderBlackFlag, 0, this), 10);
                }
            }
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.BLUE)
            {
                if (currentGameState.Now > disableBlueFlagUntil)
                {
                    disableBlueFlagUntil = currentGameState.Now.Add(timeBetweenBlueFlagMessages);
                    String opponentKeyBehind = currentGameState.getOpponentKeyBehindOnTrack(true);
                    // if the last 3 warnings are for this same driver, don't call the blue flag. Note that it's unsafe to 
                    // assume opponentKeyBehind is never null
                    if (opponentWhoTriggeredLastBlueFlag == null || !opponentWhoTriggeredLastBlueFlag.Equals(opponentKeyBehind) || blueFlagWarningCountForSingleDriver < 3)
                    {
                        // only update this stuff if we were able to derive the opponent behind on track
                        if (opponentKeyBehind != null)
                        {
                            opponentWhoTriggeredLastBlueFlag = opponentKeyBehind;
                            blueFlagWarningCountForSingleDriver++;
                        }
                        // immediate to prevent it being delayed by the hard-parts logic
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderBlueFlag, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                    }
                }
            }
            // In iRacing White flag is set on last lap if "useAmericanTerms" is enabled, this causes white flag to be announched every 40 sec on last lap.
            else if (currentGameState.SessionData.Flag == FlagEnum.WHITE && !(GlobalBehaviourSettings.useAmericanTerms && CrewChief.gameDefinition.gameEnum == GameEnum.IRACING))
            {
                if (currentGameState.Now > disableWhiteFlagUntil)
                {
                    disableWhiteFlagUntil = currentGameState.Now.Add(timeBetweenWhiteFlagMessages);
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderWhiteFlagEU, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                }
            }
            if (currentGameState.FlagData.numCarsPassedIllegally >= 0
                && currentGameState.Now > nextIllegalPassWarning && currentGameState.FlagData.numCarsPassedIllegally != illegalPassCarsCountAtLastAnnouncement)
            {
                processIllegalOvertakes(previousGameState, currentGameState);
                illegalPassCarsCountAtLastAnnouncement = currentGameState.FlagData.numCarsPassedIllegally;
            }

            processsStockCarRules(previousGameState, currentGameState);
        }

        private void processsStockCarRules(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!currentGameState.StockCarRulesData.stockCarRulesEnabled)
                return;

            if (GlobalBehaviourSettings.useAmericanTerms
                && currentGameState.SessionData.SessionType == SessionType.Race
                && previousGameState != null)
            {
                if (currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow
                    && currentGameState.StockCarRulesData.stockCarRuleApplicable != StockCarRule.NONE)
                {
                    if (!string.IsNullOrWhiteSpace(currentGameState.StockCarRulesData.luckyDogNameRaw)
                        && previousGameState.StockCarRulesData.luckyDogNameRaw != currentGameState.StockCarRulesData.luckyDogNameRaw
                        && currentGameState.StockCarRulesData.stockCarRuleApplicable != StockCarRule.LUCKY_DOG_PASS_ON_LEFT)  // Make sure that's not player.
                    {
                        if (currentGameState.StockCarRulesData.luckyDogNameRaw == currentGameState.SessionData.DriverRawName)
                        {
                            Console.WriteLine("Won't announce current lucky dog because it appears to be player");
                        }
                        else if (AudioPlayer.canReadName(currentGameState.StockCarRulesData.luckyDogNameRaw))
                        {
                            var usableDriverName = DriverNameHelper.getUsableDriverName(currentGameState.StockCarRulesData.luckyDogNameRaw);
                            Console.WriteLine("Stock Car Rule triggered: Lucky Dog is - " + usableDriverName);
                            audioPlayer.playMessageImmediately(new QueuedMessage("flags/lucky_dog_is",
                                MessageContents(folderOpponentIsLuckyDog, usableDriverName), 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }

                    // See if rule has changed.
                    if (previousGameState.StockCarRulesData.stockCarRuleApplicable != currentGameState.StockCarRulesData.stockCarRuleApplicable)
                    {
                        Console.WriteLine("Stock Car Rule triggered: " + currentGameState.StockCarRulesData.stockCarRuleApplicable);
                        if (currentGameState.StockCarRulesData.stockCarRuleApplicable == StockCarRule.LUCKY_DOG_PASS_ON_LEFT)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderWeAreLuckyDog, Utilities.random.Next(3, 7), this), 10);
                        }
                        else if (currentGameState.StockCarRulesData.stockCarRuleApplicable == StockCarRule.LUCKY_DOG_ALLOW_TO_PASS_ON_LEFT)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderAllowLuckyDogPass, Utilities.random.Next(3, 7), this), 10);
                        }
                        else if (currentGameState.StockCarRulesData.stockCarRuleApplicable == StockCarRule.LEADER_CHOOSE_LANE)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderLeaderChooseLane, Utilities.random.Next(3, 7), this), 10);
                        }
                        else if (currentGameState.StockCarRulesData.stockCarRuleApplicable == StockCarRule.WAVE_AROUND_PASS_ON_RIGHT)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderWeHaveBeenWavedAround, Utilities.random.Next(3, 7), this), 10);
                        }
                        else if (currentGameState.StockCarRulesData.stockCarRuleApplicable == StockCarRule.MOVE_TO_EOLL)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderEOLLPenalty, Utilities.random.Next(3, 7), this), 10);
                        }
                    }
                }
                else if (greenFlagLuckyDogMessageCountInSession < maxGreenFlagLuckyDogMessagesPerSession
                    && currentGameState.Now > nextGreenFlagLuckyDogCheckDue
                    && currentGameState.SessionData.SessionPhase == SessionPhase.Green
                    && currentGameState.StockCarRulesData.stockCarRuleApplicable == StockCarRule.NONE)
                {
                    GreenFlagLuckyDogStatus currentGreenFlagLuckyDogStatus = getGreenFlagLuckyDogPosition(currentGameState);
                    if (currentGreenFlagLuckyDogStatus != GreenFlagLuckyDogStatus.NONE
                        && currentGreenFlagLuckyDogStatus != lastGreenFlagLuckyDogStatusAnnounced)
                    {
                        if (currentGreenFlagLuckyDogStatus == GreenFlagLuckyDogStatus.WE_ARE_IN_LUCKY_DOG)
                        {
                            Console.WriteLine("Stock Car Rule triggered: " + currentGreenFlagLuckyDogStatus);
                            audioPlayer.playMessage(new QueuedMessage(folderWeAreInLuckyDogPosition, 0, this), 10);
                        }
                        else if (currentGreenFlagLuckyDogStatus == GreenFlagLuckyDogStatus.PASS_FOR_LUCKY_DOG)
                        {
                            OpponentData carAhead = currentGameState.getOpponentAtOverallPosition(currentGameState.SessionData.OverallPosition - 1);
                            Console.WriteLine("Stock Car Rule triggered: " + currentGreenFlagLuckyDogStatus + " driver to pass: " + (carAhead != null ? carAhead.DriverRawName : "not found"));
                            if (carAhead != null && AudioPlayer.canReadName(carAhead.DriverRawName))
                            {
                                audioPlayer.playMessage(new QueuedMessage("push_to_pass_lucky_dog",
                                    MessageContents(folderPassDriverForLuckyDogPositionIntro, carAhead, folderPassDriverForLuckyDogPositionOutro), 0, this), 10);
                            }
                            else
                            {
                                audioPlayer.playMessage(new QueuedMessage(folderPassCarAheadForLuckyPositionDog, 0, this), 10);
                            }
                        }
                        greenFlagLuckyDogMessageCountInSession++;
                        lastGreenFlagLuckyDogStatusAnnounced = currentGreenFlagLuckyDogStatus;
                        nextGreenFlagLuckyDogCheckDue = currentGameState.Now.Add(minTimeBetweenGreenFlagLuckyDogMessages);
                    }
                    else
                    {
                        nextGreenFlagLuckyDogCheckDue = currentGameState.Now.Add(greenFlagLuckyDogCheckInterval);
                    }
                }
            }
        }

        private GreenFlagLuckyDogStatus getGreenFlagLuckyDogPosition(GameStateData currentGameState)
        {
            if (currentGameState.SessionData.OverallPosition > 1 && !currentGameState.PitData.OnOutLap && !currentGameState.PitData.InPitlane &&
                currentGameState.SessionData.TrackDefinition != null && currentGameState.SessionData.TrackDefinition.trackLength > 0)
            {
                float trackLength = currentGameState.SessionData.TrackDefinition.trackLength;
                OpponentData leader = currentGameState.getOpponentAtOverallPosition(1);
                OpponentData carAhead = currentGameState.getOpponentAtOverallPosition(currentGameState.SessionData.OverallPosition - 1);
                if (carAhead != null && leader != null && leader.CompletedLaps > 0 && !leader.InPits && !leader.isExitingPits())
                {
                    float leaderRaceDistance = (trackLength * leader.CompletedLaps) + leader.DistanceRoundTrack;
                    float carAheadRaceDistance = (trackLength * carAhead.CompletedLaps) + carAhead.DistanceRoundTrack;
                    float myRaceDistance = (trackLength * currentGameState.SessionData.CompletedLaps) + currentGameState.PositionAndMotionData.DistanceRoundTrack;
                    // this will be zero if the car ahead is the leader:
                    float carAheadDistanceToLeader = leaderRaceDistance - carAheadRaceDistance;
                    float myDistanceToLeader = leaderRaceDistance - myRaceDistance;

                    // sanity checks - car ahead distance to leader can be 0 (if he's the leader). Our distance must be positive.
                    if (carAheadDistanceToLeader < 0 || myDistanceToLeader <= 0)
                    {
                        return GreenFlagLuckyDogStatus.NONE;
                    }

                    // see if we're the lucky dog:
                    if (carAheadDistanceToLeader < trackLength && myDistanceToLeader > trackLength)
                    {
                        // we're the first lapped car. Only allow a lucky dog call if he's not in the pit or on an out lap
                        return carAhead.InPits || carAhead.isExitingPits() ? GreenFlagLuckyDogStatus.NONE : GreenFlagLuckyDogStatus.WE_ARE_IN_LUCKY_DOG;
                    }

                    // if we're in p3 or higher, see if the guy in front is the lucky dog:
                    if (currentGameState.SessionData.OverallPosition > 2)
                    {
                        // check if car ahead is lucky dog - need to get the car ahead of him
                        OpponentData car2PlacesAhead = currentGameState.getOpponentAtOverallPosition(currentGameState.SessionData.OverallPosition - 2);
                        if (car2PlacesAhead != null)
                        {
                            float car2PlacesAheadRaceDistance = (trackLength * car2PlacesAhead.CompletedLaps) + car2PlacesAhead.DistanceRoundTrack;
                            float car2PlacesAheadDistanceToLeader = leaderRaceDistance - car2PlacesAheadRaceDistance;
                            // sanity check - this quantity should always be positive. If it's not, the race position is out of sync with the
                            // lap count and / or distance round the track
                            if (car2PlacesAheadDistanceToLeader < 0)
                            {
                                return GreenFlagLuckyDogStatus.NONE;
                            }

                            if (car2PlacesAheadDistanceToLeader < trackLength && carAheadDistanceToLeader > trackLength)
                            {
                                // the car 2 places ahead is on the lead lap, the car ahead is lapped, so he's the lucky dog.
                                // Only allow a lucky dog call if he's not in the pit or on an out lap
                                return car2PlacesAhead.InPits || car2PlacesAhead.isExitingPits() ? GreenFlagLuckyDogStatus.NONE : GreenFlagLuckyDogStatus.PASS_FOR_LUCKY_DOG;                                
                            }
                        }
                    }
                }
            }
            return GreenFlagLuckyDogStatus.NONE;
        }

        // note that these messages still play even if the yellow flag messages are disabled - I suppose they're penalty related
        private void processIllegalOvertakes(GameStateData previousGameState, GameStateData currentGameState)
        {
            // some uncertainty here - once a penalty has been applied, does the numCarsPassedIllegally reset or remain non-zero?
            if (illegalPassCarsCountAtLastAnnouncement > 0 && previousGameState.PenaltiesData.NumPenalties > currentGameState.PenaltiesData.NumPenalties)
            {
                Console.WriteLine("numCarsPassedIllegally has changed from " + illegalPassCarsCountAtLastAnnouncement +
                    " to  " + currentGameState.FlagData.numCarsPassedIllegally + " and penalty count has increased");
                hasAlreadyWarnedAboutIllegalPass = false;
                // If we have a new penalty delay the next check for a while
                nextIllegalPassWarning = currentGameState.Now + TimeSpan.FromSeconds(30);
            }
            else
            {
                nextIllegalPassWarning = currentGameState.Now + illegalPassRepeatInterval;
                if (currentGameState.FlagData.numCarsPassedIllegally > 0)
                {
                    if (hasAlreadyWarnedAboutIllegalPass)
                    {
                        if (currentGameState.FlagData.numCarsPassedIllegally == 1)
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_position_back_repeat", MessageContents(folderGiveOnePositionsBackNextWarning), 0,
                                null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                        else
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_positions_back_repeat", MessageContents(folderGivePositionsBackNextWarningIntro,
                                currentGameState.FlagData.numCarsPassedIllegally, folderGivePositionsBackNextWarningOutro), 0,
                                null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                    else
                    {
                        hasAlreadyWarnedAboutIllegalPass = true;
                        if (currentGameState.FlagData.numCarsPassedIllegally == 1)
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_position_back", MessageContents(folderGiveOnePositionBackFirstWarning), 0,
                                null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                        else
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_positions_back", MessageContents(folderGivePositionsBackFirstWarningIntro,
                                currentGameState.FlagData.numCarsPassedIllegally, folderGivePositionsBackFirstWarningOutro), 0,
                                null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                }
                else
                {
                    // more guesswork :(
                    if (currentGameState.PenaltiesData.NumPenalties == 0)
                    {
                        // don't allow any other message to override this one:
                        audioPlayer.playMessageImmediately(new QueuedMessage("give_positions_back_completed", MessageContents(folderGivePositionsBackCompleted), 0,
                            null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                    }
                    hasAlreadyWarnedAboutIllegalPass = false;
                }
            }
        }

        private void gameDataYellowFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (previousGameState != null)
            {
                Boolean playerStartedSector3 = previousGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.SectorNumber == 3;
                Boolean leaderStartedSector3 = previousGameState.SessionData.LeaderSectorNumber == 2 && currentGameState.SessionData.LeaderSectorNumber == 3;
                if (announceFCYPhase(previousGameState.FlagData.fcyPhase, currentGameState.FlagData.fcyPhase, currentGameState.Now, playerStartedSector3))
                {
                    lastFCYAnnounced = currentGameState.FlagData.fcyPhase;
                    lastFCYAccountedTime = currentGameState.Now;
                    switch (currentGameState.FlagData.fcyPhase)
                    {
                        case FullCourseYellowPhase.PENDING:
                            // don't allow any other message to override this one:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowStartUS : folderFCYellowStartEU, 0,
                                    null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                            }
                            // start working out who's gone off
                            if (enableOpponentCrashMessages)
                            {
                                findInitialIncidentCandidateKeys(-1, currentGameState.OpponentData);
                                playerClassPositionAtStartOfIncident = currentGameState.SessionData.ClassPosition;
                                nextIncidentDriversCheck = currentGameState.Now + incidentDriversCheckInterval;
                                getInvolvedInIncidentAttempts = 0;
                                driversInvolvedInCurrentIncident.Clear();
                            }
                            break;
                        case FullCourseYellowPhase.PITS_CLOSED:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowPitsClosedUS : folderFCYellowPitsClosedEU, Utilities.random.Next(1, 4), this), 10);
                            }
                            break;
                        case FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowPitsOpenLeadLapCarsUS : folderFCYellowPitsOpenLeadLapCarsEU, Utilities.random.Next(1, 4), this), 10);
                            }
                            break;
                        case FullCourseYellowPhase.PITS_OPEN:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowPitsOpenUS : folderFCYellowPitsOpenEU, Utilities.random.Next(1, 4), this), 10);
                            }
                            break;
                        case FullCourseYellowPhase.IN_PROGRESS:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowInProgressUS : folderFCYellowInProgressEU, Utilities.random.Next(1, 4), this), 10);
                            }
                            break;
                        case FullCourseYellowPhase.LAST_LAP_NEXT:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowLastLapNextUS : folderFCYellowLastLapNextEU, Utilities.random.Next(1, 4), this), 10);
                            }
                            break;
                        case FullCourseYellowPhase.LAST_LAP_CURRENT:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowLastLapCurrentUS : folderFCYellowLastLapCurrentEU, Utilities.random.Next(1, 4), this), 10);
                            }
                            break;
                        case FullCourseYellowPhase.RACING:
                            // don't allow any other message to override this one:
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderFCYellowGreenFlag, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                            }
                            break;
                        default:
                            break;
                    }
                }
                else if (currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.LAST_LAP_CURRENT && leaderStartedSector3)
                {
                    // last sector, safety car coming in
                    // don't allow any other message to override this one:
                    if (CrewChief.yellowFlagMessagesEnabled)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderFCYellowPrepareForGreenUS : folderFCYellowPrepareForGreenEU, 0,
                            null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                    }
                }
                else if ((currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.PENDING ||
                              currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.PITS_CLOSED) && 
                          currentGameState.Now > nextIncidentDriversCheck)
                {
                    if (enableOpponentCrashMessages)
                    {
                        if (getInvolvedInIncidentAttempts >= maxFCYGetInvolvedInIncidentAttempts)
                        {
                            // we've collected as many involved drivers as we're going to get, so report them
                            reportYellowFlagDrivers(currentGameState.OpponentData, currentGameState.SessionData.TrackDefinition);
                            nextIncidentDriversCheck = DateTime.MaxValue;
                            playerClassPositionAtStartOfIncident = int.MaxValue;
                            incidentCandidates.Clear();
                            getInvolvedInIncidentAttempts = 0;
                            driversInvolvedInCurrentIncident.Clear();
                        }
                        else
                        {
                            // get more involved drivers and schedule the next check
                            nextIncidentDriversCheck = currentGameState.Now + incidentDriversCheckInterval;
                            driversInvolvedInCurrentIncident.AddRange(getInvolvedIncidentCandidates(-1, currentGameState.OpponentData));
                        }
                        getInvolvedInIncidentAttempts++;
                    }
                }
                else
                {
                    // Console.WriteLine("Track lap distance: "  + currentGameState.PositionAndMotionData.DistanceRoundTrack + 
                    //    " distanceToNearestIncident: " + currentGameState.FlagData.distanceToNearestIncident);
                    // local yellows
                    // note the 'allSectorsAreGreen' check - we can be under local yellow with no yellow sectors in the hairpin at Macau
                    if (!isUnderLocalYellow && currentGameState.FlagData.isLocalYellow && !allSectorsAreGreen(currentGameState.FlagData))
                    {
                        //Console.WriteLine("FLAG_DEBUG: local yellow at " + currentGameState.Now.ToString("HH:mm:ss"));
                        isUnderLocalYellow = true;
                        // ensure the last state is updated, even if we don't actually read the transition
                        lastSectorFlags[currentGameState.SessionData.SectorNumber - 1] = FlagEnum.YELLOW;

                        nextIllegalPassWarning = currentGameState.Now;
                        // we might not have warned of an incident ahead - no point in warning about it now we've actually reached it
                        hasWarnedOfUpcomingIncident = true;
                        waitingToWarnOfIncident = false;
                        Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                        validationData.Add(validationIsLocalYellowKey, true);
                        validationData.Add(isValidatingSectorMessage, false);
                        if (CrewChief.yellowFlagMessagesEnabled && !currentGameState.PitData.InPitlane && !hasReportedIsUnderLocalYellow)
                        {
                            //Console.WriteLine("FLAG_DEBUG: queuing local yellow " + " at " + currentGameState.Now.ToString("HH:mm:ss"));
                            // immediate to prevent it being delayed by the hard-parts logic
                            audioPlayer.playMessageImmediately(new QueuedMessage(localFlagChangeMessageKey + "_yellow", MessageContents(folderLocalYellow), 1, this,
                                validationData) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                    else if (isUnderLocalYellow && !currentGameState.FlagData.isLocalYellow)
                    {
                        //Console.WriteLine("FLAG_DEBUG: local green at " + currentGameState.Now.ToString("HH:mm:ss"));
                        isUnderLocalYellow = false;
                        // we've passed the incident so allow warnings of other incidents approaching
                        hasWarnedOfUpcomingIncident = false;
                        waitingToWarnOfIncident = false;
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;
                        Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                        validationData.Add(validationIsLocalYellowKey, false);
                        validationData.Add(isValidatingSectorMessage, false);
                        if (CrewChief.yellowFlagMessagesEnabled && !currentGameState.PitData.InPitlane && hasReportedIsUnderLocalYellow)
                        {
                            //Console.WriteLine("FLAG_DEBUG: queuing local green " + " at " + currentGameState.Now.ToString("HH:mm:ss"));
                            audioPlayer.playMessageImmediately(
                                new QueuedMessage(localFlagChangeMessageKey + "_clear", MessageContents(folderLocalYellowClear), 1, this, validationData) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                    else if (allSectorsAreGreen(currentGameState.FlagData))
                    {
                        // if all the sectors are clear the local and warning booleans. This ensures we don't sit waiting for a 'clear' that never comes.
                        // Console.WriteLine("FLAG_DEBUG: all sectors green at " + currentGameState.Now.ToString("HH:mm:ss"));
                        isUnderLocalYellow = false;
                        hasWarnedOfUpcomingIncident = false;
                        waitingToWarnOfIncident = false;
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;
                    }
                    // This produces false-positives. Not sure why - TODO: work out what the issue is here - perhaps
                    // the data doesn't contain what I think it contains
                    /*else if (!isUnderLocalYellow && !hasWarnedOfUpcomingIncident)
                    {
                        if (waitingToWarnOfIncident)
                        {
                            if (shouldWarnOfUpComingYellow(currentGameState))
                            {
                                if (currentGameState.Now > incidentAheadSettledTime)
                                {
                                    waitingToWarnOfIncident = false;
                                    hasWarnedOfUpcomingIncident = true;
                                    if (CrewChief.yellowFlagMessagesEnabled && !currentGameState.PitData.InPitlane)
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellowAhead, 0, null));
                                    }
                                }
                            }
                            else
                            {
                                waitingToWarnOfIncident = false;
                            }
                        }
                        else if (shouldWarnOfUpComingYellow(currentGameState))
                        {
                            waitingToWarnOfIncident = true;
                            incidentAheadSettledTime = currentGameState.Now + incidentAheadSettlingTime;
                        }
                    }*/
                    // sector yellows
                    for (int i = 0; i < 3; i++)
                    {
                        //  Yellow Flags:
                        // * Only announce Yellow if
                        //      - Not in pits
                        //      - Enough time ellapsed since last announcement
                        //      - Yellow is in current or next sector (relative to player's sector).
                        //      - For current sector, sometimes announce yellow without sector number.
                        // * Only announce Clear message if
                        //      - Not in pits
                        //      - Enough time passed since Yellow was announced
                        //      - Yellow went away in or next sector (relative to player's sector).
                        //      - Announce delayed message and drop it if sector or sector flag changes

                        // we've announced this, so see if we can add more information
                        if (enableOpponentCrashMessages && i == waitingForCrashedDriverInSector && 
                            currentGameState.Now > nextIncidentDriversCheck)
                        {
                            if (getInvolvedInIncidentAttempts >= maxLocalYellowGetInvolvedInIncidentAttempts)
                            {
                                // we've collected as many involved drivers as we're going to get, so report them
                                reportYellowFlagDrivers(currentGameState.OpponentData, currentGameState.SessionData.TrackDefinition);
                                nextIncidentDriversCheck = DateTime.MaxValue;
                                playerClassPositionAtStartOfIncident = int.MaxValue;
                                incidentCandidates.Clear();
                                getInvolvedInIncidentAttempts = 0;
                                driversInvolvedInCurrentIncident.Clear();
                                waitingForCrashedDriverInSector = -1;
                            }
                            else
                            {
                                // get more involved drivers and schedule the next check
                                nextIncidentDriversCheck = currentGameState.Now + incidentDriversCheckInterval;
                                driversInvolvedInCurrentIncident.AddRange(getInvolvedIncidentCandidates(waitingForCrashedDriverInSector + 1, currentGameState.OpponentData));
                            }
                            getInvolvedInIncidentAttempts++;
                        }

                        FlagEnum sectorFlag = currentGameState.FlagData.sectorFlags[i];
                        if (sectorFlag != lastSectorFlags[i])
                        {
                            // Console.WriteLine("FLAG_DEBUG: sector " + (i + 1) + " " + sectorFlag + " at " + currentGameState.Now.ToString("HH:mm:ss"));
                            lastSectorFlags[i] = sectorFlag;
                            Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                            validationData.Add(validationSectorNumberKey, i);
                            validationData.Add(validationSectorFlagKey, sectorFlag);
                            validationData.Add(isValidatingSectorMessage, true);
                            if ((sectorFlag == FlagEnum.YELLOW || sectorFlag == FlagEnum.DOUBLE_YELLOW) && 
                                currentGameState.Now > lastSectorFlagsReportedTime[i].Add(minTimeBetweenNewYellowFlagMessages) &&
                                        (reportYellowsInAllSectors || isCurrentSector(currentGameState, i) || isNextSector(currentGameState, i)))
                            {
                                // Sector i changed to yellow - don't announce this if we're in a local yellow
                                if (!currentGameState.FlagData.isLocalYellow && lastSectorFlagsReported[i] != sectorFlag && 
                                    !incidentIsInCurrentSectorButBehind(i + 1, currentGameState))
                                {                                            
                                    hasAlreadyWarnedAboutIllegalPass = false;
                                    
                                    // don't call sector yellow if we've in a local yellow
                                    if (CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM && isCurrentSector(currentGameState, i) && 4 > Utilities.random.NextDouble() * 10)
                                    {
                                        // If in current, sometimes announce without sector number.
                                        if (CrewChief.yellowFlagMessagesEnabled && !currentGameState.PitData.InPitlane)
                                        {
                                            //Console.WriteLine("FLAG_DEBUG: queuing sector " + (i + 1) + " " + sectorFlag + " at " + currentGameState.Now.ToString("HH:mm:ss"));
                                            // immediate to prevent it being delayed by the hard-parts logic
                                            audioPlayer.playMessageImmediately(new QueuedMessage(sectorFlagChangeMessageKeyStart + (i + 1), MessageContents(sectorFlag == FlagEnum.YELLOW ?
                                                folderYellowFlag : folderDoubleYellowFlag), 3, this, validationData) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                        }
                                    }
                                    else if (CrewChief.yellowFlagMessagesEnabled && !currentGameState.PitData.InPitlane)
                                    {
                                        //Console.WriteLine("FLAG_DEBUG: queuing sector " + (i + 1) + " " + sectorFlag + " at " + currentGameState.Now.ToString("HH:mm:ss"));
                                        // immediate to prevent it being delayed by the hard-parts logic
                                        audioPlayer.playMessageImmediately(new QueuedMessage(sectorFlagChangeMessageKeyStart + (i + 1), MessageContents(sectorFlag == FlagEnum.YELLOW ?
                                            folderYellowFlagSectors[i] : folderDoubleYellowFlagSectors[i]), 3, this, validationData) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                    }
                                }
                                if (enableOpponentCrashMessages)
                                {
                                    // start working out who's gone off
                                    findInitialIncidentCandidateKeys(i + 1, currentGameState.OpponentData);
                                    playerClassPositionAtStartOfIncident = currentGameState.SessionData.ClassPosition;
                                    nextIncidentDriversCheck = currentGameState.Now + incidentDriversCheckInterval;
                                    getInvolvedInIncidentAttempts = 0;
                                    driversInvolvedInCurrentIncident.Clear();
                                    waitingForCrashedDriverInSector = i;
                                }
                            }
                            else if (sectorFlag == FlagEnum.GREEN)
                            {
                                // Sector i changed to green.  Check time since last announcement.
                                if (lastSectorFlagsReported[i] != sectorFlag &&
                                    currentGameState.Now > lastSectorFlagsReportedTime[i].Add(timeBetweenYellowAndClearFlagMessages))
                                {
                                    if (CrewChief.yellowFlagMessagesEnabled && !currentGameState.PitData.InPitlane)
                                    {
                                        // hack the message key if we're reporting green in the current sector. This prevents
                                        // a duplicate clear for local sectors
                                        //Console.WriteLine("FLAG_DEBUG: queuing sector " + (i + 1) + " " + sectorFlag + " at " + currentGameState.Now.ToString("HH:mm:ss"));
                                        String messageKey = i == currentGameState.SessionData.SectorNumber - 1 ? localFlagChangeMessageKey+ "_clear" : sectorFlagChangeMessageKeyStart + (i + 1);
                                        audioPlayer.playMessageImmediately(new QueuedMessage(messageKey,
                                            MessageContents(folderGreenFlagSectors[i]), 3, this, validationData) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                    }
                                }
                            }
                        }
                    }
                }                

                if (isUnderLocalYellow && reportAllowedOvertakesUnderYellow)
                {
                    if (currentGameState.FlagData.canOvertakeCarInFront == PassAllowedUnderYellow.YES
                        && lastReportedOvertakeAllowed == PassAllowedUnderYellow.NO && lastOvertakeAllowedReportTime.Add(TimeSpan.FromSeconds(3)) < currentGameState.Now)
                    {
                        if (CrewChief.yellowFlagMessagesEnabled)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderClearToOvertake, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.YES;
                        lastOvertakeAllowedReportTime = currentGameState.Now;
                    }
                    else if (currentGameState.FlagData.canOvertakeCarInFront == PassAllowedUnderYellow.NO
                        && lastReportedOvertakeAllowed == PassAllowedUnderYellow.YES && lastOvertakeAllowedReportTime.Add(TimeSpan.FromSeconds(3)) < currentGameState.Now)
                    {
                        if (CrewChief.yellowFlagMessagesEnabled)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderNoOvertaking, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO;
                        lastOvertakeAllowedReportTime = currentGameState.Now;                                               
                    }
                }
            }
        }

        private Boolean incidentIsInCurrentSectorButBehind(int sector, GameStateData currentGameState) 
        {
            return currentGameState.SessionData.SectorNumber == sector && 
                currentGameState.FlagData.distanceToNearestIncident > currentGameState.SessionData.TrackDefinition.trackLength - currentGameState.PositionAndMotionData.DistanceRoundTrack;
        }

        private Boolean allSectorsAreGreen(FlagData flagData)
        {
            return flagData.sectorFlags[0] == FlagEnum.GREEN && flagData.sectorFlags[1] == FlagEnum.GREEN &&
                        flagData.sectorFlags[2] == FlagEnum.GREEN;
        }

        private Boolean shouldWarnOfUpComingYellow(GameStateData gameState)
        {
            return gameState != null && gameState.FlagData.distanceToNearestIncident > 0 &&
                gameState.FlagData.distanceToNearestIncident > minDistanceToWarnOfLocalYellow && 
                    gameState.FlagData.distanceToNearestIncident < maxDistanceToWarnOfLocalYellow;
        }

        private bool isCurrentSector(GameStateData currentGameState, int sectorIndex)
        {
            return currentGameState.SessionData.SectorNumber == sectorIndex + 1;
        }

        private bool isNextSector(GameStateData currentGameState, int sectorIndex)
        {
            int nextSector = currentGameState.SessionData.SectorNumber == 3 ? 1 : currentGameState.SessionData.SectorNumber + 1;
            return nextSector == sectorIndex + 1;
        }

        private Boolean announceFCYPhase(FullCourseYellowPhase previousPhase, FullCourseYellowPhase currentPhase, DateTime now, Boolean startedSector3)
        {
            // reminder announcements for pit status at the start of sector 3, if we've not announce it for a while
            return (previousPhase != currentPhase && currentPhase != lastFCYAnnounced) ||
                ((currentPhase == FullCourseYellowPhase.PITS_CLOSED ||
                 currentPhase == FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES ||
                 currentPhase == FullCourseYellowPhase.PITS_OPEN ||
                 currentPhase == FullCourseYellowPhase.IN_PROGRESS) && now > lastFCYAccountedTime + fcyStatusReminderMinTime && startedSector3);
        }

        /**
         * Used by all other games, legacy code.
         */
        private void improvisedYellowFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.PositionAndMotionData.CarSpeed < 1)
            {
                return;
            }
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.YELLOW)
            {
                if (currentGameState.Now > disableYellowFlagUntil)
                {
                    disableYellowFlagUntil = currentGameState.Now.Add(timeBetweenYellowFlagMessages);
                    if (CrewChief.yellowFlagMessagesEnabled)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderYellowFlag, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 0) });
                    }
                }
            }
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.DOUBLE_YELLOW)
            {
                if (currentGameState.Now > disableYellowFlagUntil &&
                    // AMS specific hack until RF2 FCY stuff is ported - don't spam the double yellow during caution periods, just report it once per lap
                    (CrewChief.gameDefinition.gameEnum != GameEnum.RF1 || currentGameState.SessionData.IsNewLap))
                {
                    disableYellowFlagUntil = currentGameState.Now.Add(timeBetweenYellowFlagMessages);
                    if (CrewChief.yellowFlagMessagesEnabled)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderDoubleYellowFlag, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 0) });
                    }
                }
            }
            // now check for stopped cars
            if (currentGameState.SessionData.SessionType == SessionType.Race && enableSimpleIncidentDetection)
            {
                if (waitingForCrashedDriverInCorner == null)
                {
                    // get the first stopped car and his corner
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        String opponentId = entry.Key;
                        OpponentData opponent = entry.Value;
                        String landmark = opponent.stoppedInLandmark;
                        if (landmark != null && !landmark.Equals(currentGameState.SessionData.stoppedInLandmark))
                        {
                            // is this car in an interesting part of the track?
                            Boolean isApproaching;
                            if (opponent.DistanceRoundTrack > currentGameState.PositionAndMotionData.DistanceRoundTrack)
                            {
                                isApproaching = opponent.DistanceRoundTrack - currentGameState.PositionAndMotionData.DistanceRoundTrack < 1000;
                            }
                            else
                            {
                                isApproaching = opponent.DistanceRoundTrack -
                                    (currentGameState.PositionAndMotionData.DistanceRoundTrack - currentGameState.SessionData.TrackDefinition.trackLength) < 1000;
                            }
                            // are we fighting with him and can we call him by name?
                            Boolean isInteresting = Math.Abs(currentGameState.SessionData.ClassPosition - opponent.ClassPosition) <= 2 &&
                                (AudioPlayer.canReadName(opponent.DriverRawName) || opponent.ClassPosition <= folderPositionHasGoneOff.Length);
                            DateTime incidentWarningTime = DateTime.MinValue;
                            if ((isApproaching || isInteresting) &&
                                (!incidentWarnings.TryGetValue(landmark, out incidentWarningTime) || incidentWarningTime + incidentRepeatFrequency < currentGameState.Now))
                            {
                                waitingForCrashedDriverInCorner = landmark;
                                driversCrashedInCorner.Add(opponentId);
                                waitingForCrashedDriverInCornerFinishTime = currentGameState.Now + TimeSpan.FromSeconds(4);
                                break;
                            }
                        }
                    }
                }
                else if (currentGameState.Now < waitingForCrashedDriverInCornerFinishTime)
                {
                    // get more stopped cars
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        OpponentData opponent = entry.Value;
                        String landmark = opponent.stoppedInLandmark;
                        if (landmark == waitingForCrashedDriverInCorner && !driversCrashedInCorner.Contains(entry.Key))
                        {
                            driversCrashedInCorner.Add(entry.Key);
                        }
                    }
                }
                else
                {
                    // finished waiting, get the results and play 'em
                    List<OpponentData> crashedOpponents = new List<OpponentData>();
                    foreach (String opponentKey in driversCrashedInCorner)
                    {
                        if (currentGameState.OpponentData.ContainsKey(opponentKey) && !currentGameState.OpponentData[opponentKey].InPits)
                        {
                            crashedOpponents.Add(currentGameState.OpponentData[opponentKey]);
                        }
                    }
                    incidentWarnings[waitingForCrashedDriverInCorner] = currentGameState.Now;
                    if (crashedOpponents.Count >= pileupDriverCount)
                    {
                        // report pileup
                        if (CrewChief.yellowFlagMessagesEnabled)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("pileup_in_corner", MessageContents(folderPileupInCornerIntro, "corners/" +
                                waitingForCrashedDriverInCorner), 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                    else
                    {
                        List<OpponentData> opponentNamesToRead = new List<OpponentData>();
                        int positionToRead = -1;
                        if (enableOpponentCrashMessages)
                        {
                            foreach (OpponentData opponent in crashedOpponents)
                            {
                                if (AudioPlayer.canReadName(opponent.DriverRawName))
                                {
                                    opponentNamesToRead.Add(opponent);
                                }
                                else if (opponent.ClassPosition <= folderPositionHasGoneOff.Length && positionToRead == -1)
                                {
                                    positionToRead = opponent.ClassPosition;
                                }
                            }
                        }
                        if (opponentNamesToRead.Count > 0)
                        {
                            // one or more names to read
                            List<MessageFragment> messageContents = new List<MessageFragment>();
                            messageContents.AddRange(MessageContents(folderIncidentInCornerIntro));
                            messageContents.AddRange(MessageContents("corners/" + waitingForCrashedDriverInCorner));
                            messageContents.AddRange(MessageContents(folderIncidentInCornerDriverIntro));
                            int andIndex = opponentNamesToRead.Count > 1 ? opponentNamesToRead.Count - 1 : -1;
                            List<String> namesToDebug = new List<string>();
                            for (int i = 0; i < opponentNamesToRead.Count; i++)
                            {
                                if (i == andIndex)
                                {
                                    messageContents.AddRange(MessageContents(folderAnd));
                                }
                                namesToDebug.Add(opponentNamesToRead[i].DriverRawName);
                                messageContents.AddRange(MessageContents(opponentNamesToRead[i]));
                            }
                            Console.WriteLine("Incident in " + waitingForCrashedDriverInCorner + " for drivers " + String.Join(",", namesToDebug));
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("incident_corner_with_driver", 
                                    messageContents, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                            }
                        }
                        else if (positionToRead != -1)
                        {
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("incident_corner_with_driver", MessageContents(
                                    folderPositionHasGoneOffIn[positionToRead - 1], "corners/" + waitingForCrashedDriverInCorner), 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                            }
                        }
                        else
                        {
                            Console.WriteLine("Incident in " + waitingForCrashedDriverInCorner);
                            if (CrewChief.yellowFlagMessagesEnabled)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("incident_corner", 
                                    MessageContents(folderIncidentInCornerIntro, "corners/" + waitingForCrashedDriverInCorner), 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                            }
                        }
                    }
                    waitingForCrashedDriverInCorner = null;
                    driversCrashedInCorner.Clear();
                }
            }
        }

        void findInitialIncidentCandidateKeys(int flagSector, Dictionary<string, OpponentData> opponents)
        {
            incidentCandidates.Clear();
            foreach (KeyValuePair<string, OpponentData> entry in opponents)
            {
                string opponentKey = entry.Key;
                OpponentData opponentData = entry.Value;
                if (opponentData.DistanceRoundTrack == 0)
                {
                    // fuck's sake... pCARS2's data is so shit. This value will be 0 when a car enters the pitlane.
                    continue;
                }
                if ((flagSector == -1 || opponentData.CurrentSectorNumber == flagSector) && !opponentData.InPits)
                {
                    LapData lapData = opponentData.getCurrentLapData();
                    incidentCandidates.Add(new IncidentCandidate(opponentKey, opponentData.DistanceRoundTrack, opponentData.ClassPosition,
                        opponentData.OverallPosition, lapData == null || lapData.IsValid));
                }
            }
        }

        // get incident candidates who are involved - they've lost lots of position or haven't really moved since the last check
        List<NamePositionPair> getInvolvedIncidentCandidates(int flagSector, Dictionary<string, OpponentData> opponents)
        {
            List<NamePositionPair> involvedDrivers = new List<NamePositionPair>();
            List<IncidentCandidate> remainingIncidentCandidates = new List<IncidentCandidate>();
            foreach (IncidentCandidate incidentCandidate in incidentCandidates)
            {
                OpponentData opponent = null;
                if (opponents.TryGetValue(incidentCandidate.opponentDataKey, out opponent))
                {
                    if (opponent.DistanceRoundTrack == 0)
                    {
                        // fuck's sake... pCARS2's data is so shit. This value will be 0 when a car enters the pitlane.
                        continue;
                    }
                    if (flagSector == -1 || opponent.CurrentSectorNumber == flagSector)
                    {
                        if ((Math.Abs(opponent.DistanceRoundTrack - incidentCandidate.distanceRoundTrackAtStartOfIncident) < maxDistanceMovedForYellowAnnouncement) ||
                                opponent.OverallPosition > incidentCandidate.overallPositionAtStartOfIncident + 3)
                        {
                            // this guy is in the same sector as the yellow but has only travelled 10m in 2 seconds or has lost a load of places so he's probably involved.
                            // Only add him if we've not reported him already in this spot on the track
                            Boolean canAdd = true;
                            float distanceRoundTrackAtIncident = -1.0f;
                            if (lastIncidentPositionForOpponents.TryGetValue(opponent.DriverRawName, out distanceRoundTrackAtIncident))
                            {
                                // we've already reported on this guy in this session - if he's not really moved since this report, don't report him again

                                // this check doesn't make sense when the incident is within 20m of the start line, but it's an edge case and this check isn't
                                // really essential anyway so let's not worry about it
                                if (Math.Abs(opponent.DistanceRoundTrack - distanceRoundTrackAtIncident) < 20)
                                {
                                    canAdd = false;
                                }
                            }
                            if (canAdd)
                            {
                                lastIncidentPositionForOpponents[opponent.DriverRawName] = opponent.DistanceRoundTrack;
                                involvedDrivers.Add(new NamePositionPair(opponent.DriverRawName, incidentCandidate.classPositionAtStartOfIncident,
                                    incidentCandidate.overallPositionAtStartOfIncident, opponent.DistanceRoundTrack, AudioPlayer.canReadName(opponent.DriverRawName), 
                                    incidentCandidate.opponentDataKey));
                            }
                        }
                        else
                        {
                            // update incident candidate element to reflect the current state ready for the next check
                            incidentCandidate.overallPositionAtLastCheck = opponent.OverallPosition;
                            incidentCandidate.distanceRoundTrackAtLastCheck = opponent.DistanceRoundTrack;
                            remainingIncidentCandidates.Add(incidentCandidate);
                        }
                    }
                }
            }
            incidentCandidates = remainingIncidentCandidates;
            return involvedDrivers;
        }

        void reportYellowFlagDrivers(Dictionary<string, OpponentData> opponents, TrackDefinition currentTrack)
        {
            // remove driver who are no longer in the opponentdata or who have pitted
            List<NamePositionPair> driversInvolvedAndConnected = new List<NamePositionPair>();
            foreach (NamePositionPair driverInvolved in driversInvolvedInCurrentIncident)
            {
                if (opponents.ContainsKey(driverInvolved.opponentKey) && !opponents[driverInvolved.opponentKey].InPits)
                {
                    driversInvolvedAndConnected.Add(driverInvolved);
                }
            }
            driversInvolvedInCurrentIncident = driversInvolvedAndConnected;
            if (driversInvolvedInCurrentIncident.Count == 0 || checkForAndReportPileup(currentTrack))
            {
                return;
            }
            
            // no pileup so read name / positions / corners as appropriate
            // there may be many of these, so we need to sort the list then pick the top few
            driversInvolvedInCurrentIncident.Sort(new NamePositionPairComparer(playerClassPositionAtStartOfIncident));
            // get the landmark and other data off the first item
            String landmark = TrackData.getLandmarkForLapDistance(currentTrack, driversInvolvedInCurrentIncident[0].distanceRoundTrack);
            float distanceRoundTrackOfFirstDriver = driversInvolvedInCurrentIncident[0].distanceRoundTrack;
            List<OpponentData> opponentsToRead = new List<OpponentData>();
            opponentsToRead.Add(opponents[driversInvolvedInCurrentIncident[0].opponentKey]);
            // if the first item is a position (we have no name for him), don't process the others
            Boolean namesMode = driversInvolvedInCurrentIncident[0].canReadName;
            int position = driversInvolvedInCurrentIncident[0].classPosition;
            if (namesMode)
            {
                for (int i = 1; i < driversInvolvedInCurrentIncident.Count; i++)
                {
                    if (opponentsToRead.Count == maxDriversToReportAsInvolvedInIncident)
                    {
                        break;
                    }
                    if (driversInvolvedInCurrentIncident[i].canReadName)
                    {                        
                        String thisLandmark = TrackData.getLandmarkForLapDistance(currentTrack, driversInvolvedInCurrentIncident[i].distanceRoundTrack);
                        if (landmark == thisLandmark || Math.Abs(distanceRoundTrackOfFirstDriver - driversInvolvedInCurrentIncident[i].distanceRoundTrack) < 300)
                        {
                            opponentsToRead.Add(opponents[driversInvolvedInCurrentIncident[i].opponentKey]);
                            if (landmark == null)
                            {
                                landmark = thisLandmark;
                            }
                        }
                    }
                }
            }
            // now we have the opponents to read and the landmark - all the drivers in the list are in or close to the landmark
            List<MessageFragment> messageContents = new List<MessageFragment>();
            if (namesMode)
            {
                messageContents.AddRange(MessageContents(folderNameHasGoneOffIntro));
                int andIndex = opponentsToRead.Count > 1 ? opponentsToRead.Count - 1 : -1;
                for (int i = 0; i < opponentsToRead.Count; i++)
                {
                    if (i == andIndex)
                    {
                        messageContents.AddRange(MessageContents(folderAnd));
                    }
                    messageContents.AddRange(MessageContents(opponentsToRead[i]));
                }
                if (andIndex != -1)
                {
                    if (landmark != null)
                    {
                        messageContents.AddRange(MessageContents(folderNamesHaveGoneOffInOutro));
                        messageContents.AddRange(MessageContents("corners/" + landmark));
                    }
                    else
                    {
                        messageContents.AddRange(MessageContents(folderNamesHaveGoneOffOutro));
                    }
                }
                else
                {
                    if (landmark != null)
                    {
                        messageContents.AddRange(MessageContents(folderNameHasGoneOffInOutro));
                        messageContents.AddRange(MessageContents("corners/" + landmark));
                    }
                    else
                    {
                        messageContents.AddRange(MessageContents(folderNameHasGoneOffOutro));
                    }
                }
            }
            else if (landmark != null && position <= folderPositionHasGoneOffIn.Length)
            {
                messageContents.AddRange(MessageContents(folderPositionHasGoneOffIn[position - 1]));
                messageContents.AddRange(MessageContents("corners/" + landmark));
            }
            else if (position <= folderPositionHasGoneOffIn.Length)
            {
                messageContents.AddRange(MessageContents(folderPositionHasGoneOff[position - 1]));
            }
            if (messageContents.Count > 0 && CrewChief.yellowFlagMessagesEnabled)
            {
                // immediate to prevent it being delayed by the hard-parts logic
                audioPlayer.playMessageImmediately(new QueuedMessage("incident_drivers", messageContents, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
            }
        }

        Boolean checkForAndReportPileup(TrackDefinition currentTrack)
        {
            if (driversInvolvedInCurrentIncident.Count >= pileupDriverCount)
            {
                Dictionary<String, int> crashedInLandmarkCounts = new Dictionary<string, int>();
                foreach (NamePositionPair driver in driversInvolvedInCurrentIncident)
                {
                    String crashLandmark = TrackData.getLandmarkForLapDistance(currentTrack, driver.distanceRoundTrack);
                    if (crashLandmark != null)
                    {
                        int numCrashesInLandmark = -1;
                        if (crashedInLandmarkCounts.TryGetValue(crashLandmark, out numCrashesInLandmark))
                        {
                            crashedInLandmarkCounts[crashLandmark] = ++numCrashesInLandmark;
                        }
                        else
                        {
                            crashedInLandmarkCounts.Add(crashLandmark, 1);
                        }
                    }
                }
                // now see if any exceed the limit
                foreach (String crashedInLandmarkKey in crashedInLandmarkCounts.Keys)
                {
                    if (crashedInLandmarkCounts[crashedInLandmarkKey] >= pileupDriverCount)
                    {
                        // report the pileup
                        if (CrewChief.yellowFlagMessagesEnabled)
                        {
                            // immediate to prevent it being delayed by the hard-parts logic
                            audioPlayer.playMessageImmediately(new QueuedMessage("pileup_in_corner", MessageContents(folderPileupInCornerIntro, "corners/" + crashedInLandmarkKey),
                                0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                        return true;
                    }
                }
            }
            return false;
        }
    }

    class NamePositionPair
    {
        public String name;
        public int classPosition;
        public int overallPosition;
        public float distanceRoundTrack;
        public Boolean canReadName;
        public string opponentKey;

        public NamePositionPair(String name, int classPosition, int overallPosition, float distanceRoundTrack, Boolean canReadName, string opponentKey)
        {
            this.name = name;
            this.classPosition = classPosition;
            this.overallPosition = overallPosition;
            this.distanceRoundTrack = distanceRoundTrack;
            this.canReadName = canReadName;
            this.opponentKey = opponentKey;
        }
    }

    class NamePositionPairComparer : IComparer<NamePositionPair>
    {
        private int playerClassPosition;

        public NamePositionPairComparer(int playerClassPosition)
        {
            this.playerClassPosition = playerClassPosition;
        }
        public int Compare(NamePositionPair a, NamePositionPair b)
        {
            if (a.canReadName == b.canReadName)
            {
                // can (or can't) read both names, return the one closest but preferably in front
                if (a.classPosition > playerClassPosition && b.classPosition > playerClassPosition)
                {
                    return a.classPosition > b.classPosition ? 1 : -1;
                }
                else if (a.classPosition < playerClassPosition && b.classPosition < playerClassPosition)
                {
                    return a.classPosition < b.classPosition ? 1 : -1;
                }
                else if (a.classPosition < playerClassPosition && b.classPosition > playerClassPosition)
                {
                    return 1;
                }
                else if (a.classPosition > playerClassPosition && b.classPosition < playerClassPosition)
                {
                    return -1;
                }
                else
                {
                    return Math.Abs(a.classPosition - playerClassPosition) < Math.Abs(b.classPosition - playerClassPosition) ? 1 : -1;
                }
            }
            else
            {
                // we have one name
                if (a.canReadName)
                {
                    // can't read b's name but he still might be in a more interesting position
                    return b.classPosition <= FlagsMonitor.folderPositionHasGoneOff.Length && b.classPosition < playerClassPosition && a.classPosition > playerClassPosition ? -1 : 1;
                }
                else
                {
                    // can't read a's name but he still might be in a more interesting position
                    return a.classPosition <= FlagsMonitor.folderPositionHasGoneOff.Length && a.classPosition < playerClassPosition && b.classPosition > playerClassPosition ? 1 : -1;
                }
            }
        }
    }

    class IncidentCandidate
    {
        public string opponentDataKey;
        public float distanceRoundTrackAtStartOfIncident;
        public float distanceRoundTrackAtLastCheck;
        public Boolean lapValidAtStartOfIncident;
        public int overallPositionAtStartOfIncident;
        public int classPositionAtStartOfIncident;
        public int overallPositionAtLastCheck;
        public IncidentCandidate(string opponentDataKey, float distanceRoundTrackAtStartOfIncident, int classPositionAtStartOfIncident,
            int overallPositionAtStartOfIncident, Boolean lapValidAtStartOfIncident)
        {
            this.opponentDataKey = opponentDataKey;
            this.distanceRoundTrackAtStartOfIncident = distanceRoundTrackAtStartOfIncident;
            this.classPositionAtStartOfIncident = classPositionAtStartOfIncident;
            this.overallPositionAtStartOfIncident = overallPositionAtStartOfIncident;
            this.lapValidAtStartOfIncident = lapValidAtStartOfIncident;
            this.overallPositionAtLastCheck = overallPositionAtStartOfIncident;
            this.distanceRoundTrackAtLastCheck = distanceRoundTrackAtStartOfIncident;
        }
    }
}
