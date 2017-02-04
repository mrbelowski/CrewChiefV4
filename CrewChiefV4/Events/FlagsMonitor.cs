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

        // for new (RF2 and R3E) impl
        private FlagEnum[] lastSectorFlagsAnnounced = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
        private DateTime[] lastSectorFlagsAnnouncedTime = new DateTime[] { DateTime.MinValue, DateTime.MinValue, DateTime.MinValue };
        private FullCourseYellowPhase lastFCYAnnounced = FullCourseYellowPhase.RACING;
        private DateTime lastFCYAccounedTime = DateTime.MinValue;

        private TimeSpan fcyPitStatusReminderMinTime = TimeSpan.FromSeconds(30);

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
            lastFCYAnnounced = FullCourseYellowPhase.RACING;
            DateTime lastFCYAccounedTime = DateTime.MinValue;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT)
            {
                newFlagImplementation(previousGameState, currentGameState);
            }
            else
            {
                oldFlagImplementation(previousGameState, currentGameState);
            }            
        }

        private void newFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
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
                    for (int i = 0; i < 3; i++)
                    {
                        if (currentGameState.FlagData.sectorFlags[i] != lastSectorFlagsAnnounced[i])
                        {
                            // change of flag status in sector i
                            lastSectorFlagsAnnounced[i] = currentGameState.FlagData.sectorFlags[i];
                            lastSectorFlagsAnnouncedTime[i] = DateTime.Now;
                            
                            if (currentGameState.FlagData.sectorFlags[i] == FlagEnum.YELLOW)
                            {
                                // don't allow any other message to override this one:
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderYellowFlagSectors[i], 0, null));
                            } 
                            else if (currentGameState.FlagData.sectorFlags[i] == FlagEnum.DOUBLE_YELLOW)
                            {
                                // don't allow any other message to override this one:
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderDoubleYellowFlagSectors[i], 0, null));
                            }
                            else if (currentGameState.FlagData.sectorFlags[i] == FlagEnum.GREEN)
                            {
                                // don't allow any other message to override this one:
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenFlagSectors[i], 0, null));
                            }
                        }
                    }                        
                } 
            }
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
        private void oldFlagImplementation(GameStateData previousGameState, GameStateData currentGameState)
        {            
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
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.YELLOW)
            {
                if (currentGameState.Now > lastYellowFlagTime.Add(timeBetweenYellowFlagMessages))
                {
                    lastYellowFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderYellowFlag, 0, this));
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
            else if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.Flag == FlagEnum.DOUBLE_YELLOW)
            {
                if (currentGameState.Now > lastYellowFlagTime.Add(timeBetweenYellowFlagMessages))
                {
                    lastYellowFlagTime = currentGameState.Now;
                    audioPlayer.playMessage(new QueuedMessage(folderDoubleYellowFlag, 0, this));
                }
            }
        }
    }
}
