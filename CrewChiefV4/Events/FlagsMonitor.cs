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
        private String folderWhiteFlag = "flags/white_flag";
        private String folderBlackFlag = "flags/black_flag";

        private DateTime lastYellowFlagTime = DateTime.MinValue;
        private DateTime lastBlackFlagTime = DateTime.MinValue;
        private DateTime lastWhiteFlagTime = DateTime.MinValue;
        private DateTime lastBlueFlagTime = DateTime.MinValue;

        private TimeSpan timeBetweenYellowFlagMessages = TimeSpan.FromSeconds(30);
        private TimeSpan timeBetweenBlueFlagMessages = TimeSpan.FromSeconds(20);
        private TimeSpan timeBetweenBlackFlagMessages = TimeSpan.FromSeconds(20);
        private TimeSpan timeBetweenWhiteFlagMessages = TimeSpan.FromSeconds(20);

        private String folderFCYellowStart = "flags/fc_yellow_start";
        private String folderFCYellowPitsClosed = "flags/fc_yellow_pits_closed";
        private String folderFCYellowPitsOpenLeadLapCars = "flags/fc_yellow_pits_open_lead_lap_cars";
        private String folderFCYellowPitsOpen = "flags/fc_yellow_pits_open";
        private String folderFCYellowLastLapNext = "flags/fc_yellow_last_lap_next";
        private String folderFCYellowLastLapCurrent = "flags/fc_yellow_last_lap_current";
        private String folderFCYellowPrepareForGreen = "flags/fc_yellow_prepare_for_green";
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
        // TODO: Record sweary versions of this when the kids are out
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

        private int maxDistanceMovedForYellowAnnouncement = UserSettings.GetUserSettings().getInt("max_distance_moved_for_yellow_announcement");

        private Boolean reportAllowedOvertakesUnderYellow = UserSettings.GetUserSettings().getBoolean("report_allowed_overtakes_under_yellow");

        // for new (RF2 and R3E) impl
        private FlagEnum[] lastSectorFlagsAnnounced = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
        private DateTime[] lastSectorFlagsAnnouncedTime = new DateTime[] { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        private FullCourseYellowPhase lastFCYAnnounced = FullCourseYellowPhase.RACING;
        private DateTime lastFCYAccounedTime = DateTime.MinValue;
        private TimeSpan timeBetweenYellowAndClearFlagMessages = TimeSpan.FromSeconds(5);
        private int secondsToPreValidateYellowClearMessages = 8;
        private TimeSpan timeBetweenNewYellowFlagMessages = TimeSpan.FromSeconds(5);
        private Random random = new Random();

        // do we need this?
        private DateTime lastLocalYellowAnnouncedTime = DateTime.MinValue;
        private DateTime lastLocalYellowClearAnnouncedTime = DateTime.MinValue;
        private DateTime lastOvertakeAllowedReportTime = DateTime.MinValue;
        private Boolean isUnderLocalYellow = false;
        private Boolean hasWarnedOfUpcomingIncident = false;

        private DateTime nextIncidentDriversCheck = DateTime.MaxValue;

        private TimeSpan fcyPitStatusReminderMinTime = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("time_between_caution_period_status_reminders"));

        private Boolean reportYellowsInAllSectors = UserSettings.GetUserSettings().getBoolean("report_yellows_in_all_sectors");
        private Boolean enableSimpleIncidentDetection = UserSettings.GetUserSettings().getBoolean("enable_simple_incident_detection");

        private float maxDistanceToWarnOfLocalYellow = 300;    // metres - externalise? Is this sufficient? Make it speed-dependent?
        private float minDistanceToWarnOfLocalYellow = 50;    // metres - externalise? Is this sufficient? Make it speed-dependent?

        List<IncidentCandidate> incidentCandidates = new List<IncidentCandidate>();

        private int positionAtStartOfIncident = int.MaxValue;

        private Dictionary<string, DateTime> incidentWarnings = new Dictionary<string, DateTime>();

        private TimeSpan incidentRepeatFrequency = TimeSpan.FromSeconds(30);

        private int getInvolvedInIncidentAttempts = 0;

        private int maxFCYGetInvolvedInIncidentAttempts = 5;

        private TimeSpan incidentDriversCheckInterval = TimeSpan.FromSeconds(1.5);

        private int maxLocalYellowGetInvolvedInIncidentAttempts = 2;

        private int maxDriversToReportAsInvolvedInIncident = 3;

        private int pileupDriverCount = 4;

        int waitingForCrashedDriverInSector = -1;

        private List<NamePositionPair> driversInvolvedInCurrentIncident = new List<NamePositionPair>();

        private String waitingForCrashedDriverInCorner = null;
        private List<OpponentData> driversCrashedInCorner = new List<OpponentData>();
        private DateTime waitingForCrashedDriverInCornerFinishTime = DateTime.MaxValue;

        // this will be initialised to something sensible once a yellow has been shown - if no yellow is ever 
        // shown we never want to check for illegal passes
        private DateTime nextIllegalPassWarning = DateTime.MaxValue;
        private TimeSpan illegalPassRepeatInterval = TimeSpan.FromSeconds(7);
        private int illegalPassCarsCountAtLastAnnouncement = 0;
        private Boolean hasAlreadyWarnedAboutIllegalPass = false;

        private DateTime localYellowStartSettledTime = DateTime.MinValue;
        private DateTime localYellowEndSettledTime = DateTime.MinValue;
        private TimeSpan localYellowChangeSettlingTime = TimeSpan.FromSeconds(2);
        private Boolean waitingForNewLocalYellowFlagToSettle = false;
        private Boolean waitingForNewLocalGreenFlagToSettle = false;
        private DateTime incidentAheadSettledTime = DateTime.MinValue;
        private Boolean waitingToWarnOfIncident = false;
        
        private PassAllowedUnderYellow lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;

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

        public override void clearState()
        {
            lastYellowFlagTime = DateTime.MinValue;
            lastBlackFlagTime = DateTime.MinValue;
            lastWhiteFlagTime = DateTime.MinValue;
            lastBlueFlagTime = DateTime.MinValue;

            lastSectorFlagsAnnounced = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
            lastSectorFlagsAnnouncedTime = new DateTime[] { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
            nextIncidentDriversCheck = DateTime.MaxValue;
            lastFCYAnnounced = FullCourseYellowPhase.RACING;
            lastFCYAccounedTime = DateTime.MinValue;
            lastLocalYellowAnnouncedTime = DateTime.MinValue;
            lastLocalYellowClearAnnouncedTime = DateTime.MinValue;
            lastOvertakeAllowedReportTime = DateTime.MinValue;
            isUnderLocalYellow = false;
            hasWarnedOfUpcomingIncident = false;
            positionAtStartOfIncident = int.MaxValue;
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

            localYellowStartSettledTime = DateTime.MinValue;
            localYellowEndSettledTime = DateTime.MinValue;
            incidentAheadSettledTime = DateTime.MinValue;
            waitingToWarnOfIncident = false;
            waitingForNewLocalYellowFlagToSettle = false;
            waitingForNewLocalGreenFlagToSettle = false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.PitData.InPitlane || currentGameState.SessionData.SessionRunningTime < 10)
            {
                // don't process if we're in the pits or just started a session
                return;
            }

            if (CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)
            {
                newYellowFlagImplementation(previousGameState, currentGameState);
            }
            else
            {
                oldYellowFlagImplementation(previousGameState, currentGameState);
            }
            //  now other flags
            if (currentGameState.PositionAndMotionData.CarSpeed < 1)
            {
                return;
            }
            if (currentGameState.SessionData.Flag == FlagEnum.BLACK)
            {
                if (currentGameState.Now > lastBlackFlagTime.Add(timeBetweenBlackFlagMessages))
                {
                    lastBlackFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderBlackFlag, 0, this));
                }
            }
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.BLUE)
            {
                if (currentGameState.Now > lastBlueFlagTime.Add(timeBetweenBlueFlagMessages))
                {
                    lastBlueFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderBlueFlag, 0, this));
                }
            }
            else if (currentGameState.SessionData.Flag == FlagEnum.WHITE)
            {
                if (currentGameState.Now > lastWhiteFlagTime.Add(timeBetweenWhiteFlagMessages))
                {
                    lastWhiteFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderWhiteFlag, 0, this));
                }
            }
            if (currentGameState.FlagData.numCarsPassedIllegally >= 0 
                && currentGameState.Now > nextIllegalPassWarning && currentGameState.FlagData.numCarsPassedIllegally != illegalPassCarsCountAtLastAnnouncement)
            {                
                processIllegalOvertakes(previousGameState, currentGameState);
                illegalPassCarsCountAtLastAnnouncement = currentGameState.FlagData.numCarsPassedIllegally;
            }
        }

        private void processIllegalOvertakes(GameStateData previousGameState, GameStateData currentGameState)
        {
            // some uncertainty here - once a penalty has been applied, does the numCarsPassedIllegally reset or remain non-zero?
            if (illegalPassCarsCountAtLastAnnouncement > 0 && previousGameState.PenaltiesData.NumPenalties > currentGameState.PenaltiesData.NumPenalties)
            {
                Console.WriteLine("numCarsPassedIllegally has changed from " + illegalPassCarsCountAtLastAnnouncement +
                    " to  " + currentGameState.FlagData.numCarsPassedIllegally + " and penalty count has increased");
                hasAlreadyWarnedAboutIllegalPass = false;
                // TODO: really struggling to actually get penalised for passing under yellow, so I'm guessing here. 
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
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_position_back_repeat", MessageContents(folderGiveOnePositionsBackNextWarning), 0, null));
                        }
                        else
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_positions_back_repeat", MessageContents(folderGivePositionsBackNextWarningIntro,
                                currentGameState.FlagData.numCarsPassedIllegally, folderGivePositionsBackNextWarningOutro), 0, null));
                        }
                    }
                    else
                    {
                        hasAlreadyWarnedAboutIllegalPass = true;
                        if (currentGameState.FlagData.numCarsPassedIllegally == 1)
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_position_back", MessageContents(folderGiveOnePositionBackFirstWarning), 0, null));
                        }
                        else
                        {
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage("give_positions_back", MessageContents(folderGivePositionsBackFirstWarningIntro,
                                currentGameState.FlagData.numCarsPassedIllegally, folderGivePositionsBackFirstWarningOutro), 0, null));
                        }
                    }
                }
                else
                {
                    // more guesswork :(
                    if (currentGameState.PenaltiesData.NumPenalties == 0)
                    {
                        // don't allow any other message to override this one:
                        audioPlayer.playMessageImmediately(new QueuedMessage("give_positions_back_completed", MessageContents(folderGivePositionsBackCompleted), 0, null));
                    }
                    hasAlreadyWarnedAboutIllegalPass = false;
                }
            }
        }

        private void newYellowFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (previousGameState != null)
            {
                Boolean playerStartedSector3 = previousGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.SectorNumber == 3;
                Boolean leaderStartedSector3 = previousGameState.SessionData.LeaderSectorNumber == 2 && currentGameState.SessionData.LeaderSectorNumber == 3;
                if (announceFCYPhase(previousGameState.FlagData.fcyPhase, currentGameState.FlagData.fcyPhase, currentGameState.Now, playerStartedSector3))
                {
                    lastFCYAnnounced = currentGameState.FlagData.fcyPhase;
                    lastFCYAccounedTime = currentGameState.Now;
                    switch (currentGameState.FlagData.fcyPhase)
                    {
                        case FullCourseYellowPhase.PENDING:
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderFCYellowStart, 0, null));
                            // start working out who's gone off
                            findInitialIncidentCandidateKeys(-1, currentGameState.OpponentData);
                            positionAtStartOfIncident = currentGameState.SessionData.Position;
                            nextIncidentDriversCheck = currentGameState.Now + incidentDriversCheckInterval;
                            getInvolvedInIncidentAttempts = 0;
                            driversInvolvedInCurrentIncident.Clear();
                            break;
                        case FullCourseYellowPhase.PITS_CLOSED:
                            audioPlayer.playMessage(new QueuedMessage(folderFCYellowPitsClosed, 0, this));
                            break;
                        case FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES:
                            audioPlayer.playMessage(new QueuedMessage(folderFCYellowPitsOpenLeadLapCars, 0, this));
                            break;
                        case FullCourseYellowPhase.PITS_OPEN:
                            audioPlayer.playMessage(new QueuedMessage(folderFCYellowPitsOpen, 0, this));
                            break;
                        case FullCourseYellowPhase.LAST_LAP_NEXT:
                            audioPlayer.playMessage(new QueuedMessage(folderFCYellowLastLapNext, 0, this));
                            break;
                        case FullCourseYellowPhase.LAST_LAP_CURRENT:
                            audioPlayer.playMessage(new QueuedMessage(folderFCYellowLastLapCurrent, 0, this));
                            break;
                        case FullCourseYellowPhase.RACING:
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderFCYellowGreenFlag, 0, null));
                            break;
                        default:
                            break;
                    }
                }
                else if (currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.LAST_LAP_CURRENT && leaderStartedSector3)
                {
                    // last sector, safety car coming in
                    // don't allow any other message to override this one:
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderFCYellowPrepareForGreen, 0, null));
                }
                else if ((currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.PENDING ||
                              currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.PITS_CLOSED) && 
                          currentGameState.Now > nextIncidentDriversCheck)
                {
                    if (getInvolvedInIncidentAttempts >= maxFCYGetInvolvedInIncidentAttempts)
                    {
                        // we've collected as many involved drivers as we're going to get, so report them
                        reportYellowFlagDrivers(currentGameState.OpponentData, currentGameState.SessionData.TrackDefinition);
                        nextIncidentDriversCheck = DateTime.MaxValue;
                        positionAtStartOfIncident = int.MaxValue;
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
                else
                {
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
                        if (i == waitingForCrashedDriverInSector && currentGameState.Now > nextIncidentDriversCheck)
                        {
                            if (getInvolvedInIncidentAttempts >= maxLocalYellowGetInvolvedInIncidentAttempts)
                            {
                                // we've collected as many involved drivers as we're going to get, so report them
                                reportYellowFlagDrivers(currentGameState.OpponentData, currentGameState.SessionData.TrackDefinition);
                                nextIncidentDriversCheck = DateTime.MaxValue;
                                positionAtStartOfIncident = int.MaxValue;
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

                        if (!currentGameState.PitData.InPitlane &&
                            (reportYellowsInAllSectors || isCurrentSector(currentGameState, i) || isNextSector(currentGameState, i)))
                        {
                            FlagEnum sectorFlag = currentGameState.FlagData.sectorFlags[i];
                            if (sectorFlag != lastSectorFlagsAnnounced[i])
                            {
                                if (sectorFlag == FlagEnum.YELLOW || sectorFlag == FlagEnum.DOUBLE_YELLOW)
                                {
                                    // Sector i changed to yellow
                                    if (currentGameState.Now > lastSectorFlagsAnnouncedTime[i].Add(timeBetweenNewYellowFlagMessages))
                                    {
                                        hasAlreadyWarnedAboutIllegalPass = false;
                                        lastSectorFlagsAnnounced[i] = sectorFlag;
                                        lastSectorFlagsAnnouncedTime[i] = currentGameState.Now;

                                        // don't call sector yellow if we've in a local yellow
                                        if (isCurrentSector(currentGameState, i) && 4 > random.NextDouble() * 10 && !currentGameState.FlagData.isLocalYellow)
                                        {
                                            // If in current, sometimes announce without sector number.
                                            audioPlayer.playMessage(new QueuedMessage(sectorFlag == FlagEnum.YELLOW ?
                                                folderYellowFlag : folderDoubleYellowFlag, 0, this));
                                        }
                                        else if (!currentGameState.FlagData.isLocalYellow)
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage(sectorFlag == FlagEnum.YELLOW ?
                                                folderYellowFlagSectors[i] : folderDoubleYellowFlagSectors[i], 0, null));
                                        }

                                        // start working out who's gone off
                                        findInitialIncidentCandidateKeys(i + 1, currentGameState.OpponentData);
                                        positionAtStartOfIncident = currentGameState.SessionData.Position;
                                        nextIncidentDriversCheck = currentGameState.Now + incidentDriversCheckInterval;
                                        getInvolvedInIncidentAttempts = 0;
                                        driversInvolvedInCurrentIncident.Clear();
                                        waitingForCrashedDriverInSector = i;
                                    }
                                }
                                else if (sectorFlag == FlagEnum.GREEN)
                                {
                                    // Sector i changed to green.  Check time since last announcement.
                                    if (currentGameState.Now > lastSectorFlagsAnnouncedTime[i].Add(timeBetweenYellowAndClearFlagMessages))
                                    {
                                        lastSectorFlagsAnnounced[i] = sectorFlag;
                                        lastSectorFlagsAnnouncedTime[i] = currentGameState.Now;

                                        // Queue delayed message for flag is clear.
                                        if (lastLocalYellowClearAnnouncedTime.Add(localYellowChangeSettlingTime) < currentGameState.Now)
                                        {
                                            // if the previousGameState was local yellow we'll have already called 'clear' - don't also call the sector clear
                                            audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenFlagSectors[i], secondsToPreValidateYellowClearMessages, this));
                                        }
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Clear previous sector state
                            lastSectorFlagsAnnounced[i] = FlagEnum.GREEN;
                            lastSectorFlagsAnnouncedTime[i] = DateTime.MinValue;
                        }
                    }
                }

                // local yellows (planned R3E implementation)
                // note the 'allSectorsAreGreen' check - we can be under local yellow with no yellow sectors in the hairpin at Macau
                if (!isUnderLocalYellow && currentGameState.FlagData.isLocalYellow && !allSectorsAreGreen(currentGameState.FlagData))
                {
                    // transition from green to local yellow - stop waiting for green to settle:
                    waitingForNewLocalGreenFlagToSettle = false;
                    if (!waitingForNewLocalYellowFlagToSettle)
                    {
                        waitingForNewLocalYellowFlagToSettle = true;
                        localYellowStartSettledTime = currentGameState.Now + localYellowChangeSettlingTime;
                    }
                    else if (currentGameState.Now > localYellowStartSettledTime)
                    {
                        // been yellow for a while, so call it
                        if (lastLocalYellowAnnouncedTime.Add(TimeSpan.FromSeconds(6)) < currentGameState.Now)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellow, 0, null));
                            lastLocalYellowAnnouncedTime = currentGameState.Now;
                        }
                        isUnderLocalYellow = true;
                        nextIllegalPassWarning = currentGameState.Now;
                        // we might not have warned of an incident ahead - no point in warning about it now we've actually reached it
                        hasWarnedOfUpcomingIncident = true;
                        waitingToWarnOfIncident = false;
                    }
                }
                else if (isUnderLocalYellow && !currentGameState.FlagData.isLocalYellow)
                {
                    // transition from local yellow to green - stop waiting for yellow to settle:
                    waitingForNewLocalYellowFlagToSettle = false;
                    if (!waitingForNewLocalGreenFlagToSettle)
                    {
                        waitingForNewLocalGreenFlagToSettle = true;
                        localYellowEndSettledTime = currentGameState.Now + localYellowChangeSettlingTime;
                    }
                    else if (currentGameState.Now > localYellowEndSettledTime) 
                    {
                        // has been green long enough to announce
                        if (lastLocalYellowClearAnnouncedTime.Add(TimeSpan.FromSeconds(6)) < currentGameState.Now)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellowClear, 0, null));
                            lastLocalYellowClearAnnouncedTime = currentGameState.Now;
                        }
                        isUnderLocalYellow = false;
                        // we've passed the incident so allow warnings of other incidents approaching
                        hasWarnedOfUpcomingIncident = false;
                        waitingToWarnOfIncident = false;
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;
                    }
                }
                else if (!isUnderLocalYellow && !hasWarnedOfUpcomingIncident) 
                {
                    if (waitingToWarnOfIncident)
                    {
                        if (shouldWarnOfUpComingYellow(currentGameState))
                        {
                            if (currentGameState.Now > incidentAheadSettledTime)
                            {
                                waitingToWarnOfIncident = false;
                                hasWarnedOfUpcomingIncident = true;
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellowAhead, 0, null));
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
                        incidentAheadSettledTime = currentGameState.Now + localYellowChangeSettlingTime;
                    }
                }
                else if (allSectorsAreGreen(currentGameState.FlagData))
                {
                    // if all the sectors are clear the local and warning booleans. This ensures we don't sit waiting for a 'clear' that never comes.
                    isUnderLocalYellow = false;
                    hasWarnedOfUpcomingIncident = false;
                    waitingToWarnOfIncident = false;
                    lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO_DATA;
                }

                if (isUnderLocalYellow && reportAllowedOvertakesUnderYellow)
                {
                    if (currentGameState.FlagData.canOvertakeCarInFront == PassAllowedUnderYellow.YES
                        && lastReportedOvertakeAllowed == PassAllowedUnderYellow.NO && lastOvertakeAllowedReportTime.Add(TimeSpan.FromSeconds(3)) < currentGameState.Now)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderClearToOvertake, 0, this));
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.YES;
                        lastOvertakeAllowedReportTime = currentGameState.Now;
                    }
                    else if (currentGameState.FlagData.canOvertakeCarInFront == PassAllowedUnderYellow.NO
                        && lastReportedOvertakeAllowed == PassAllowedUnderYellow.YES && lastOvertakeAllowedReportTime.Add(TimeSpan.FromSeconds(3)) < currentGameState.Now)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderNoOvertaking, 0, this));
                        lastReportedOvertakeAllowed = PassAllowedUnderYellow.NO;
                        lastOvertakeAllowedReportTime = currentGameState.Now;                                               
                    }
                }
            }
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

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<string, object> validationData)
        {
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                for (int i = 0; i < 3; ++i)
                {
                    // If i'th sector has Clear message pending
                    if (eventSubType == folderGreenFlagSectors[i])
                    {
                        // If in pits or FCY, drop this message.
                        if (currentGameState.PitData.InPitlane || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)
                        {
                            return false;
                        }

                        if (currentGameState.FlagData.sectorFlags[i] != FlagEnum.GREEN ||  // If flag is no longer Green
                            (!isCurrentSector(currentGameState, i) && !isNextSector(currentGameState, i)))  // Or sector is nor current nor next
                        {
                            return false;
                        }
                    }
                }

                // Still valid
                return true;
            }

            return false;
        }

        private Boolean announceFCYPhase(FullCourseYellowPhase previousPhase, FullCourseYellowPhase currentPhase, DateTime now, Boolean startedSector3)
        {
            // reminder announcements for pit status at the start of sector 3, if we've not announce it for a while
            return (previousPhase != currentPhase && currentPhase != lastFCYAnnounced) ||
                ((currentPhase == FullCourseYellowPhase.PITS_CLOSED ||
                 currentPhase == FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES ||
                 currentPhase == FullCourseYellowPhase.PITS_OPEN) && now > lastFCYAccounedTime + fcyPitStatusReminderMinTime && startedSector3);
        }

        /**
         * Used by all other games, legacy code.
         */
        private void oldYellowFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.PositionAndMotionData.CarSpeed < 1)
            {
                return;
            }
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.YELLOW)
            {
                if (currentGameState.Now > lastYellowFlagTime.Add(timeBetweenYellowFlagMessages))
                {
                    lastYellowFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderYellowFlag, 0, this));
                }
            }
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.DOUBLE_YELLOW)
            {
                if (currentGameState.Now > lastYellowFlagTime.Add(timeBetweenYellowFlagMessages) &&
                    // AMS specific hack until RF2 FCY stuff is ported - don't spam the double yellow during caution periods, just report it once per lap
                    (CrewChief.gameDefinition.gameEnum != GameEnum.RF1 || currentGameState.SessionData.IsNewLap))
                {
                    lastYellowFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderDoubleYellowFlag, 0, this));
                }
            }
            // now check for stopped cars
            // TODO: I think it should also be possible to dynamically disable and enable this through a button assignment and a voice command
            if (currentGameState.SessionData.SessionType == SessionType.Race && enableSimpleIncidentDetection)
            {
                if (waitingForCrashedDriverInCorner == null)
                {
                    // get the first stopped car and his corner
                    foreach (OpponentData opponent in currentGameState.OpponentData.Values)
                    {
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
                            Boolean isInteresting = Math.Abs(currentGameState.SessionData.Position - opponent.Position) <= 2 &&
                                (canReadName(opponent.DriverRawName) || opponent.Position <= folderPositionHasGoneOff.Length);
                            if ((isApproaching || isInteresting) &&
                                (!incidentWarnings.ContainsKey(landmark) || incidentWarnings[landmark] + incidentRepeatFrequency < currentGameState.Now))
                            {
                                waitingForCrashedDriverInCorner = landmark;
                                driversCrashedInCorner.Add(opponent);
                                waitingForCrashedDriverInCornerFinishTime = currentGameState.Now + TimeSpan.FromSeconds(4);
                                break;
                            }
                        }
                    }
                }
                else if (currentGameState.Now < waitingForCrashedDriverInCornerFinishTime)
                {
                    // get more stopped cars
                    foreach (OpponentData opponent in currentGameState.OpponentData.Values)
                    {
                        String landmark = opponent.stoppedInLandmark;
                        if (landmark == waitingForCrashedDriverInCorner && !driversCrashedInCorner.Contains(opponent))
                        {
                            driversCrashedInCorner.Add(opponent);
                        }
                    }
                }
                else
                {
                    // finished waiting, get the results and play 'em
                    incidentWarnings[waitingForCrashedDriverInCorner] = currentGameState.Now;
                    if (driversCrashedInCorner.Count >= pileupDriverCount)
                    {
                        // report pileup
                        audioPlayer.playMessage(new QueuedMessage("pileup_in_corner", MessageContents(folderPileupInCornerIntro, "corners/" +
                            waitingForCrashedDriverInCorner), 0, this));
                    }
                    else
                    {
                        List<OpponentData> opponentNamesToRead = new List<OpponentData>();
                        int positionToRead = -1;
                        foreach (OpponentData opponent in driversCrashedInCorner)
                        {
                            if (canReadName(opponent.DriverRawName))
                            {
                                opponentNamesToRead.Add(opponent);
                            }
                            else if (opponent.Position <= folderPositionHasGoneOff.Length && positionToRead == -1)
                            {
                                positionToRead = opponent.Position;
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
                            Console.WriteLine("incident in " + waitingForCrashedDriverInCorner + " for drivers " + String.Join(",", namesToDebug));
                            audioPlayer.playMessage(new QueuedMessage("incident_corner_with_driver", messageContents, 0, this));
                        }
                        else if (positionToRead != -1)
                        {
                            audioPlayer.playMessage(new QueuedMessage("incident_corner_with_driver", MessageContents(
                                        folderPositionHasGoneOffIn[positionToRead - 1], "corners/" + waitingForCrashedDriverInCorner), 0, this));
                        }
                        else
                        {
                            Console.WriteLine("incident in " + waitingForCrashedDriverInCorner);
                            audioPlayer.playMessage(new QueuedMessage("incident_corner", MessageContents(folderIncidentInCornerIntro, "corners/" + waitingForCrashedDriverInCorner), 0, this));
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
                if ((flagSector == -1 || opponentData.CurrentSectorNumber == flagSector) && !opponentData.InPits)
                {
                    LapData lapData = opponentData.getCurrentLapData();
                    incidentCandidates.Add(new IncidentCandidate(opponentKey, opponentData.DistanceRoundTrack, opponentData.Position,
                        lapData == null || lapData.IsValid));
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
                if (opponents.ContainsKey(incidentCandidate.opponentDataKey))
                {
                    OpponentData opponent = opponents[incidentCandidate.opponentDataKey];
                    if (flagSector == -1 || opponent.CurrentSectorNumber == flagSector)
                    {
                        if ((Math.Abs(opponent.DistanceRoundTrack - incidentCandidate.distanceRoundTrackAtStartOfIncident) < maxDistanceMovedForYellowAnnouncement) ||
                                opponent.Position > incidentCandidate.positionAtStartOfIncident + 3)
                        {
                            // this guy is in the same sector as the yellow but has only travelled 10m in 2 seconds or has lost a load of places so he's probably involved
                            involvedDrivers.Add(new NamePositionPair(opponent.DriverRawName, incidentCandidate.positionAtStartOfIncident, opponent.DistanceRoundTrack,
                                canReadName(opponent.DriverRawName), incidentCandidate.opponentDataKey));
                        }
                        else
                        {
                            // update incident candidate element to reflect the current state ready for the next check
                            incidentCandidate.positionAtLastCheck = opponent.Position;
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
            if (driversInvolvedInCurrentIncident.Count == 0 || checkForAndReportPileup(currentTrack))
            {
                return;
            }

            // no pileup so read name / positions / corners as appropriate
            // there may be many of these, so we need to sort the list then pick the top few
            driversInvolvedInCurrentIncident.Sort(new NamePositionPairComparer(positionAtStartOfIncident));
            // get the landmark and other data off the first item
            String landmark = TrackData.getLandmarkForLapDistance(currentTrack, driversInvolvedInCurrentIncident[0].distanceRoundTrack);
            float distanceRoundTrackOfFirstDriver = driversInvolvedInCurrentIncident[0].distanceRoundTrack;
            List<OpponentData> opponentsToRead = new List<OpponentData>();
            opponentsToRead.Add(opponents[driversInvolvedInCurrentIncident[0].opponentKey]);
            // if the first item is a position (we have no name for him), don't process the others
            Boolean namesMode = driversInvolvedInCurrentIncident[0].canReadName;
            int position = driversInvolvedInCurrentIncident[0].position;
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
            if (messageContents.Count > 0)
            {
                audioPlayer.playMessage(new QueuedMessage("incident_drivers", messageContents, 0, this));
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
                        if (crashedInLandmarkCounts.ContainsKey(crashLandmark))
                        {
                            crashedInLandmarkCounts[crashLandmark]++;
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
                        audioPlayer.playMessage(new QueuedMessage("pileup_in_corner", MessageContents(folderPileupInCornerIntro, "corners/" + crashedInLandmarkKey), 0, this));
                        return true;
                    }
                }
            }
            return false;
        }


        private Boolean canReadName(String rawName)
        {
            return SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(rawName));
        }
    }

    class NamePositionPair
    {
        public String name;
        public int position;
        public float distanceRoundTrack;
        public Boolean canReadName;
        public string opponentKey;

        public NamePositionPair(String name, int position, float distanceRoundTrack, Boolean canReadName, string opponentKey)
        {
            this.name = name;
            this.position = position;
            this.distanceRoundTrack = distanceRoundTrack;
            this.canReadName = canReadName;
            this.opponentKey = opponentKey;
        }
    }

    class NamePositionPairComparer : IComparer<NamePositionPair>
    {
        private int playerPosition;

        public NamePositionPairComparer(int playerPosition)
        {
            this.playerPosition = playerPosition;
        }
        public int Compare(NamePositionPair a, NamePositionPair b)
        {
            if (a.canReadName == b.canReadName)
            {
                // can (or can't) read both names, return the one closest but preferably in front
                if (a.position > playerPosition && b.position > playerPosition)
                {
                    return a.position > b.position ? 1 : -1;
                }
                else if (a.position < playerPosition && b.position < playerPosition)
                {
                    return a.position < b.position ? 1 : -1;
                }
                else if (a.position < playerPosition && b.position > playerPosition)
                {
                    return 1;
                }
                else if (a.position > playerPosition && b.position < playerPosition)
                {
                    return -1;
                }
                else
                {
                    return Math.Abs(a.position - playerPosition) < Math.Abs(b.position - playerPosition) ? 1 : -1;
                }
            }
            else
            {
                // we have one name
                if (a.canReadName)
                {
                    // can't read b's name but he still might be in a more interesting position
                    return b.position <= FlagsMonitor.folderPositionHasGoneOff.Length && b.position < playerPosition && a.position > playerPosition ? -1 : 1;
                }
                else
                {
                    // can't read a's name but he still might be in a more interesting position
                    return a.position <= FlagsMonitor.folderPositionHasGoneOff.Length && a.position < playerPosition && b.position > playerPosition ? 1 : -1;
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
        public int positionAtStartOfIncident;
        public int positionAtLastCheck;
        public IncidentCandidate(string opponentDataKey, float distanceRoundTrackAtStartOfIncident, int positionAtStartOfIncident, Boolean lapValidAtStartOfIncident)
        {
            this.opponentDataKey = opponentDataKey;
            this.distanceRoundTrackAtStartOfIncident = distanceRoundTrackAtStartOfIncident;
            this.positionAtStartOfIncident = positionAtStartOfIncident;
            this.lapValidAtStartOfIncident = lapValidAtStartOfIncident;
            this.positionAtLastCheck = positionAtStartOfIncident;
            this.distanceRoundTrackAtLastCheck = distanceRoundTrackAtStartOfIncident;
        }
    }
}
