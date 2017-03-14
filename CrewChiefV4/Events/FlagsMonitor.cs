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
        private TimeSpan timeBetweenBlueFlagMessages = TimeSpan.FromSeconds(10);
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

        private String[] folderPositionHasGoneOff = new String[] { "flags/position1_has_gone_off", "flags/position2_has_gone_off", "flags/position3_has_gone_off", 
                                                                   "flags/position4_has_gone_off", "flags/position5_has_gone_off", "flags/position6_has_gone_off", };
        private String[] folderPositionHasGoneOffIn = new String[] { "flags/position1_has_gone_off_in", "flags/position2_has_gone_off_in", "flags/position3_has_gone_off_in", 
                                                                   "flags/position4_has_gone_off_in", "flags/position5_has_gone_off_in", "flags/position6_has_gone_off_in", };
        private String folderNameHasGoneOffIntro = "flags/name_has_gone_off_intro";
        private String folderNameHasGoneOffOutro = "flags/name_has_gone_off_outro";
        //  used when we know the corner name:
        private String folderNameHasGoneOffInOutro = "flags/name_has_gone_off_in_outro";

        private int maxDistanceMovedForYellowAnnouncement = UserSettings.GetUserSettings().getInt("max_distance_moved_for_yellow_announcement");

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
        private Boolean isUnderLocalYellow = false;
        private Boolean hasWarnedOfUpcomingIncident = false;

        private DateTime nextIncidentDriversCheck = DateTime.MaxValue;

        private TimeSpan fcyPitStatusReminderMinTime = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("time_between_caution_period_status_reminders"));

        private Boolean reportYellowsInAllSectors = UserSettings.GetUserSettings().getBoolean("report_yellows_in_all_sectors");

        private float distanceToWarnOfLocalYellow = 500;    // metres - externalise? Is this sufficient? Make it speed-dependent?

        List<IncidentCandidate> incidentCandidates = new List<IncidentCandidate>();

        private int positionAtStartOfIncident = int.MaxValue;
        
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
            isUnderLocalYellow = false;
            hasWarnedOfUpcomingIncident = false;
            positionAtStartOfIncident = int.MaxValue;
            incidentCandidates.Clear();
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT ||
                (CrewChief.gameDefinition.gameEnum == GameEnum.RF1))
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
        }

        private void newYellowFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (previousGameState != null)
            {
                Boolean startedSector3 = previousGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.SectorNumber == 3;
                if (announceFCYPhase(previousGameState.FlagData.fcyPhase, currentGameState.FlagData.fcyPhase, startedSector3))
                {
                    lastFCYAnnounced = currentGameState.FlagData.fcyPhase;
                    lastFCYAccounedTime = DateTime.Now;
                    switch (currentGameState.FlagData.fcyPhase)
                    {
                        case FullCourseYellowPhase.PENDING:
                            // don't allow any other message to override this one:
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderFCYellowStart, 0, null));
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
                else if (currentGameState.FlagData.fcyPhase == FullCourseYellowPhase.LAST_LAP_CURRENT && startedSector3)
                {
                    // last sector, safety car coming in
                    // don't allow any other message to override this one:
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderFCYellowPrepareForGreen, 0, null));
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
                                        lastSectorFlagsAnnounced[i] = sectorFlag;
                                        lastSectorFlagsAnnouncedTime[i] = DateTime.Now;

                                        if (isCurrentSector(currentGameState, i) && 4 > random.NextDouble() * 10)
                                        {
                                            // If in current, sometimes announce without sector number.
                                            audioPlayer.playMessage(new QueuedMessage(sectorFlag == FlagEnum.YELLOW ? 
                                                folderYellowFlag : folderDoubleYellowFlag, 0, this));
                                        }
                                        else
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage(sectorFlag == FlagEnum.YELLOW ?
                                                folderYellowFlagSectors[i] : folderDoubleYellowFlagSectors[i], 0, null));
                                        }

                                        // start working out who's gone off
                                        findInitialIncidentCandidateKeys(i + 1, currentGameState.OpponentData);
                                        positionAtStartOfIncident = currentGameState.SessionData.Position;
                                        nextIncidentDriversCheck = DateTime.Now + TimeSpan.FromSeconds(3);
                                    }
                                }
                                else if (sectorFlag == FlagEnum.GREEN)
                                {
                                    // Sector i changed to green.  Check time since last announcement.
                                    if (currentGameState.Now > lastSectorFlagsAnnouncedTime[i].Add(timeBetweenYellowAndClearFlagMessages))
                                    {
                                        lastSectorFlagsAnnounced[i] = sectorFlag;
                                        lastSectorFlagsAnnouncedTime[i] = DateTime.Now;

                                        // Queue delayed message for flag is clear.
                                        audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenFlagSectors[i], secondsToPreValidateYellowClearMessages, this));
                                    }
                                }
                            }
                            else
                            {
                                // we've announced this, so see if we can add more information
                                if (DateTime.Now > nextIncidentDriversCheck)
                                {
                                    if (incidentCandidates.Count > 0)
                                    {
                                        reportYellowFlagDriver(i + 1, currentGameState.OpponentData, currentGameState.SessionData.TrackDefinition);
                                    }
                                    nextIncidentDriversCheck = DateTime.MaxValue;
                                    positionAtStartOfIncident = int.MaxValue;
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
                if (!isUnderLocalYellow && currentGameState.FlagData.isLocalYellow)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellow, 0, null));
                    isUnderLocalYellow = true;
                    lastLocalYellowAnnouncedTime = DateTime.Now;
                    // we might not have warned of an incident ahead - no point in warning about it now we've actually reached it
                    hasWarnedOfUpcomingIncident = true;
                }
                else if (isUnderLocalYellow && !currentGameState.FlagData.isLocalYellow)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellowClear, 0, null));
                    isUnderLocalYellow = false;
                    lastLocalYellowAnnouncedTime = DateTime.Now;
                    // we've passed the incident so allow warnings of other incidents approaching
                    hasWarnedOfUpcomingIncident = false;
                } else if (!isUnderLocalYellow && !hasWarnedOfUpcomingIncident &&
                    previousGameState.FlagData.distanceToNearestIncident > distanceToWarnOfLocalYellow && currentGameState.FlagData.distanceToNearestIncident < distanceToWarnOfLocalYellow)
                {
                    hasWarnedOfUpcomingIncident = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderLocalYellowAhead, 0, null));
                }
                else if (currentGameState.FlagData.sectorFlags[0] == FlagEnum.GREEN && currentGameState.FlagData.sectorFlags[1] == FlagEnum.GREEN &&
                        currentGameState.FlagData.sectorFlags[1] == FlagEnum.GREEN)
                {
                    // if all the sectors are clear the local and warning booleans. This ensures we don't sit waiting for a 'clear' that never comes.
                    isUnderLocalYellow = false;
                    hasWarnedOfUpcomingIncident = false;
                }
                
            }
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

        private Boolean announceFCYPhase(FullCourseYellowPhase previousPhase, FullCourseYellowPhase currentPhase, Boolean startedSector3)
        {
            // reminder announcements for pit status at the start of sector 3, if we've not announce it for a while
            return (previousPhase != currentPhase && currentPhase != lastFCYAnnounced) ||
                ((currentPhase == FullCourseYellowPhase.PITS_CLOSED ||
                 currentPhase == FullCourseYellowPhase.PITS_OPEN_LEAD_LAP_VEHICLES || 
                 currentPhase == FullCourseYellowPhase.PITS_OPEN) && DateTime.Now > lastFCYAccounedTime + fcyPitStatusReminderMinTime && startedSector3);            
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
        }

        void findInitialIncidentCandidateKeys(int flagSector, Dictionary<Object, OpponentData> opponents)
        {
            incidentCandidates.Clear();
            foreach (KeyValuePair<Object, OpponentData> entry in opponents)
            {
                Object opponentKey = entry.Key;
                OpponentData opponentData = entry.Value;                
                if (opponentData.CurrentSectorNumber == flagSector && !opponentData.InPits)
                {
                    LapData lapData = opponentData.getCurrentLapData();
                    incidentCandidates.Add(new IncidentCandidate(opponentKey, opponentData.DistanceRoundTrack, opponentData.Position,
                        lapData == null || lapData.IsValid));
                }
            }
        }

        void reportYellowFlagDriver(int flagSector, Dictionary<Object, OpponentData> opponents, TrackDefinition currentTrack)
        {
            List<NamePositionPair> driversToReport = new List<NamePositionPair>();
            foreach (IncidentCandidate incidentCandidate in incidentCandidates)
            {
                if (opponents.ContainsKey(incidentCandidate.opponentDataKey))
                {
                    OpponentData opponent = opponents[incidentCandidate.opponentDataKey];
                    if (opponent.CurrentSectorNumber == flagSector &&
                        (Math.Abs(opponent.DistanceRoundTrack - incidentCandidate.distanceRoundTrackAtStartOfIncident) < maxDistanceMovedForYellowAnnouncement) ||
                        opponent.Position > incidentCandidate.positionAtStartOfIncident + 5)
                    {
                        // this guy is in the same sector as the yellow but has only travelled 10m in 5 seconds or has lost a load of places,
                        // so he's probably involved - add him to the list if we have sound files for him:
                        NamePositionPair namePositionPair = new NamePositionPair(opponent.DriverRawName, incidentCandidate.positionAtStartOfIncident, opponent.DistanceRoundTrack,
                            canReadName(opponent.DriverRawName), incidentCandidate.opponentDataKey);
                        if (namePositionPair.canReadName || namePositionPair.position <= folderPositionHasGoneOff.Length)
                        {
                            driversToReport.Add(namePositionPair);
                        }
                    }
                }
            }
            incidentCandidates.Clear();
            // now we have a list of possible drivers who we think are involved in the incident and we can read out, so select one to read
            foreach (NamePositionPair namePositionPair in driversToReport)
            {
                if (Math.Abs(positionAtStartOfIncident - namePositionPair.position) < 4 && namePositionPair.canReadName)
                {
                    // best match - he's within 3 places and we have his name 

                    // TODO: refactor this copy-paste horseshit:
                    String landmark = TrackData.getLandmarkForLapDistance(currentTrack, namePositionPair.distanceRoundTrack);
                    if (landmark != null)
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderNameHasGoneOffIntro, opponents[namePositionPair.opponentKey], folderNameHasGoneOffInOutro, "corners/" + landmark), 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderNameHasGoneOffIntro, opponents[namePositionPair.opponentKey], folderNameHasGoneOffOutro), 0, this));
                    }
                    return;
                }
            }
            foreach (NamePositionPair namePositionPair in driversToReport)
            {
                if (namePositionPair.position < positionAtStartOfIncident && namePositionPair.canReadName)
                {
                    // decent match - he's ahead and we have a name

                    // TODO: refactor this copy-paste horseshit:
                    String landmark = TrackData.getLandmarkForLapDistance(currentTrack, namePositionPair.distanceRoundTrack);
                    if (landmark != null)
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderNameHasGoneOffIntro, opponents[namePositionPair.opponentKey], folderNameHasGoneOffInOutro, "corners/" + landmark), 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderNameHasGoneOffIntro, opponents[namePositionPair.opponentKey], folderNameHasGoneOffOutro), 0, this));
                    }
                    return;
                }
            }
            foreach (NamePositionPair namePositionPair in driversToReport)
            {
                if (namePositionPair.position < positionAtStartOfIncident && positionAtStartOfIncident - namePositionPair.position < 6)
                {
                    // hmm... no name, but he's close in front

                    // TODO: refactor this copy-paste horseshit:
                    String landmark = TrackData.getLandmarkForLapDistance(currentTrack, namePositionPair.distanceRoundTrack);
                    if (landmark != null)
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderPositionHasGoneOffIn[namePositionPair.position - 1], "corners/" + landmark), 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderPositionHasGoneOff[namePositionPair.position - 1]), 0, this));
                    }
                    return;
                }
            }
            foreach (NamePositionPair namePositionPair in driversToReport)
            {
                if (namePositionPair.canReadName)
                {
                    // meh, at least we have a name

                    // TODO: refactor this copy-paste horseshit:
                    String landmark = TrackData.getLandmarkForLapDistance(currentTrack, namePositionPair.distanceRoundTrack);
                    if (landmark != null)
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderNameHasGoneOffIntro, opponents[namePositionPair.opponentKey], folderNameHasGoneOffInOutro, "corners/" + landmark), 0, this));
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage("incident_driver", MessageContents(
                            folderNameHasGoneOffIntro, opponents[namePositionPair.opponentKey], folderNameHasGoneOffOutro), 0, this));
                    }
                    return;
                }
            }
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
        public Object opponentKey;

        public NamePositionPair(String name, int position, float distanceRoundTrack, Boolean canReadName, Object opponentKey)
        {
            this.name = name;
            this.position = position;
            this.distanceRoundTrack = distanceRoundTrack;
            this.canReadName = canReadName;
            this.opponentKey = opponentKey;
        }
    }

    class IncidentCandidate
    {
        public Object opponentDataKey;
        public float distanceRoundTrackAtStartOfIncident;
        public Boolean lapValidAtStartOfIncident;
        public int positionAtStartOfIncident;
        public IncidentCandidate(Object opponentDataKey, float distanceRoundTrackAtStartOfIncident, int positionAtStartOfIncident, Boolean lapValidAtStartOfIncident)
        {
            this.opponentDataKey = opponentDataKey;
            this.distanceRoundTrackAtStartOfIncident = distanceRoundTrackAtStartOfIncident;
            this.positionAtStartOfIncident = positionAtStartOfIncident;
            this.lapValidAtStartOfIncident = lapValidAtStartOfIncident;
        }
    } 
}
