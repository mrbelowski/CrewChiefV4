using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class LapTimes : AbstractEvent
    {        
        int frequencyOfRaceSectorDeltaReports = UserSettings.GetUserSettings().getInt("frequency_of_race_sector_delta_reports");
        int frequencyOfPracticeAndQualSectorDeltaReports = UserSettings.GetUserSettings().getInt("frequency_of_prac_and_qual_sector_delta_reports");
        int frequencyOfPlayerRaceLapTimeReports = UserSettings.GetUserSettings().getInt("frequency_of_player_race_lap_time_reports");
        int frequencyOfPlayerQualAndPracLapTimeReports = UserSettings.GetUserSettings().getInt("frequency_of_player_prac_and_qual_lap_time_reports");
        Boolean raceSectorReportsAtEachSector = UserSettings.GetUserSettings().getBoolean("race_sector_reports_at_each_sector");
        Boolean practiceAndQualSectorReportsAtEachSector = UserSettings.GetUserSettings().getBoolean("practice_and_qual_sector_reports_at_each_sector"); 
        Boolean raceSectorReportsAtLapEnd = UserSettings.GetUserSettings().getBoolean("race_sector_reports_at_lap_end");
        Boolean practiceAndQualSectorReportsLapEnd = UserSettings.GetUserSettings().getBoolean("practice_and_qual_sector_reports_at_lap_end");
        Boolean disablePCarspracAndQualPoleDeltaReports = UserSettings.GetUserSettings().getBoolean("disable_pcars_prac_and_qual_pole_deltas");

        int maxQueueLengthForRaceSectorDeltaReports = 0;
        int maxQueueLengthForRaceLapTimeReports = 0;

        // for qualifying:
        // "that was a 1:34.2, you're now 0.4 seconds off the pace"
        public static String folderLapTimeIntro = "lap_times/time_intro";
        private String folderGapIntro = "lap_times/gap_intro";
        private String folderGapOutroOffPace = "lap_times/gap_outro_off_pace";
        // "that was a 1:34.2, you're fastest in your class"
        private String folderFastestInClass = "lap_times/fastest_in_your_class";

        private String folderLessThanATenthOffThePace = "lap_times/less_than_a_tenth_off_the_pace";

        private String folderQuickerThanSecondPlace = "lap_times/quicker_than_second_place";

        private String folderQuickestOverall = "lap_times/quickest_overall";

        private String folderPaceOK = "lap_times/pace_ok";
        private String folderPaceBad = "lap_times/pace_bad";
        private String folderNeedToFindOneMoreTenth = "lap_times/need_to_find_one_more_tenth";
        private String folderNeedToFindASecond = "lap_times/need_to_find_a_second";
        private String folderNeedToFindMoreThanASecond = "lap_times/need_to_find_more_than_a_second";
        private String folderNeedToFindAFewMoreTenths = "lap_times/need_to_find_a_few_more_tenths";

        // for race:
        private String folderBestLapInRace = "lap_times/best_lap_in_race";
        private String folderBestLapInRaceForClass = "lap_times/best_lap_in_race_for_class";

        private String folderGoodLap = "lap_times/good_lap";

        private String folderConsistentTimes = "lap_times/consistent";

        private String folderImprovingTimes = "lap_times/improving";

        private String folderWorseningTimes = "lap_times/worsening";

        private String folderPersonalBest = "lap_times/personal_best";
        private String folderSettingCurrentRacePace = "lap_times/setting_current_race_pace";
        private String folderMatchingCurrentRacePace = "lap_times/matching_race_pace";

        private String folderSector1Fastest = "lap_times/sector1_fastest";
        private String folderSector2Fastest = "lap_times/sector2_fastest";
        private String folderSector3Fastest = "lap_times/sector3_fastest";
        private String folderSector1and2Fastest = "lap_times/sector1_and_2_fastest";
        private String folderSector2and3Fastest = "lap_times/sector2_and_3_fastest";
        private String folderSector1and3Fastest = "lap_times/sector1_and_3_fastest";
        private String folderAllSectorsFastest = "lap_times/sector_all_fastest";

        private String folderSector1Fast = "lap_times/sector1_fast";
        private String folderSector2Fast = "lap_times/sector2_fast";
        private String folderSector3Fast = "lap_times/sector3_fast";
        private String folderSector1and2Fast = "lap_times/sector1_and_2_fast";
        private String folderSector2and3Fast = "lap_times/sector2_and_3_fast";
        private String folderSector1and3Fast = "lap_times/sector1_and_3_fast";
        private String folderAllSectorsFast = "lap_times/sector_all_fast";

        private String folderSector1ATenthOffThePace = "lap_times/sector1_a_tenth_off_pace";
        private String folderSector2ATenthOffThePace = "lap_times/sector2_a_tenth_off_pace";
        private String folderSector3ATenthOffThePace = "lap_times/sector3_a_tenth_off_pace";
        private String folderSector1and2ATenthOffThePace = "lap_times/sector1_and_2_a_tenth_off_pace";
        private String folderSector2and3ATenthOffThePace = "lap_times/sector2_and_3_a_tenth_off_pace";
        private String folderSector1and3ATenthOffThePace = "lap_times/sector1_and_3_a_tenth_off_pace";
        private String folderAllSectorsATenthOffThePace = "lap_times/sector_all_a_tenth_off_pace";

        private String folderSector1TwoTenthsOffThePace = "lap_times/sector1_two_tenths_off_pace";
        private String folderSector2TwoTenthsOffThePace = "lap_times/sector2_two_tenths_off_pace";
        private String folderSector3TwoTenthsOffThePace = "lap_times/sector3_two_tenths_off_pace";
        private String folderSector1and2TwoTenthsOffThePace = "lap_times/sector1_and_2_two_tenths_off_pace";
        private String folderSector2and3TwoTenthsOffThePace = "lap_times/sector2_and_3_two_tenths_off_pace";
        private String folderSector1and3TwoTenthsOffThePace = "lap_times/sector1_and_3_two_tenths_off_pace";
        private String folderAllSectorsTwoTenthsOffThePace = "lap_times/sector_all_two_tenths_off_pace";

        private String folderSector1AFewTenthsOffThePace = "lap_times/sector1_a_few_tenths_off_pace";
        private String folderSector2AFewTenthsOffThePace = "lap_times/sector2_a_few_tenths_off_pace";
        private String folderSector3AFewTenthsOffThePace = "lap_times/sector3_a_few_tenths_off_pace";
        private String folderSector1and2AFewTenthsOffThePace = "lap_times/sector1_and_2_a_few_tenths_off_pace";
        private String folderSector2and3AFewTenthsOffThePace = "lap_times/sector2_and_3_a_few_tenths_off_pace";
        private String folderSector1and3AFewTenthsOffThePace = "lap_times/sector1_and_3_a_few_tenths_off_pace";
        private String folderAllSectorsAFewTenthsOffThePace = "lap_times/sector_all_a_few_tenths_off_pace";

        private String folderSector1ASecondOffThePace = "lap_times/sector1_a_second_off_pace";
        private String folderSector2ASecondOffThePace = "lap_times/sector2_a_second_off_pace";
        private String folderSector3ASecondOffThePace = "lap_times/sector3_a_second_off_pace";
        private String folderSector1and2ASecondOffThePace = "lap_times/sector1_and_2_a_second_off_pace";
        private String folderSector2and3ASecondOffThePace = "lap_times/sector2_and_3_a_second_off_pace";
        private String folderSector1and3ASecondOffThePace = "lap_times/sector1_and_3_a_second_off_pace";
        private String folderAllSectorsASecondOffThePace = "lap_times/sector_all_a_second_off_pace";

        private String folderSector1MoreThanASecondOffThePace = "lap_times/sector1_more_than_a_second_off_pace";
        private String folderSector2MoreThanASecondOffThePace = "lap_times/sector2_more_than_a_second_off_pace";
        private String folderSector3MoreThanASecondOffThePace = "lap_times/sector3_more_than_a_second_off_pace";
        private String folderSector1and2MoreThanASecondOffThePace = "lap_times/sector1_and_2_more_than_a_second_off_pace";
        private String folderSector2and3MoreThanASecondOffThePace = "lap_times/sector2_and_3_more_than_a_second_off_pace";
        private String folderSector1and3MoreThanASecondOffThePace = "lap_times/sector1_and_3_more_than_a_second_off_pace";
        private String folderAllSectorsMoreThanASecondOffThePace = "lap_times/sector_all_more_than_a_second_off_pace";


        // if the lap is within 0.3% of the best lap time play a message
        private Single goodLapPercent = 0.3f;

        private Single matchingRacePacePercent = 0.1f;

        // if the lap is within 0.5% of the previous lap it's considered consistent
        private Single consistencyLimit = 0.5f;

        private List<float> lapTimesWindow;

        private int lapTimesWindowSize = 3;

        private ConsistencyResult lastConsistencyMessage;

        // lap number when the last consistency update was made
        private int lastConsistencyUpdate;

        private Boolean lapIsValid;

        private LastLapRating lastLapRating;
        
        private TimeSpan deltaPlayerLastToSessionBestInClass;

        private Boolean deltaPlayerLastToSessionBestInClassSet = false;

        private float lastLapTime;

        private float bestLapTime;
        
        private int currentPosition;

        private Random random = new Random();

        private SessionType sessionType;

        private GameStateData currentGameState;

        private int paceCheckLapsWindowForRace = 3;

        private Boolean isHotLapping;

        private TimeSpan lastGapToSecondWhenLeadingPracOrQual;
        
        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            if (frequencyOfRaceSectorDeltaReports > 7)
            {
                maxQueueLengthForRaceSectorDeltaReports = 7;
            } 
            else if (frequencyOfRaceSectorDeltaReports > 5)
            {
                maxQueueLengthForRaceSectorDeltaReports = 5;
            }
            else
            {
                maxQueueLengthForRaceSectorDeltaReports = 4;
            }
            if (frequencyOfPlayerRaceLapTimeReports > 7)
            {
                maxQueueLengthForRaceLapTimeReports = 7;
            }
            else if (frequencyOfPlayerRaceLapTimeReports > 5)
            {
                maxQueueLengthForRaceLapTimeReports = 5;
            }
            else
            {
                maxQueueLengthForRaceLapTimeReports = 4;
            }
        }

        public override void clearState()
        {
            lapTimesWindow = new List<float>(lapTimesWindowSize);
            lastConsistencyUpdate = 0;
            lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
            lapIsValid = true;
            lastLapRating = LastLapRating.NO_DATA;
            deltaPlayerLastToSessionBestInClass = TimeSpan.MaxValue;
            deltaPlayerLastToSessionBestInClassSet = false;
            lastLapTime = 0;
            bestLapTime = 0;
            currentPosition = -1;
            currentGameState = null;
            isHotLapping = false;
            lastGapToSecondWhenLeadingPracOrQual = TimeSpan.Zero;
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            sessionType = currentGameState.SessionData.SessionType;
            this.currentGameState = currentGameState;
            if (currentGameState.SessionData.IsNewLap)
            {
                deltaPlayerLastToSessionBestInClassSet = false;
                if (currentGameState.SessionData.LapTimePrevious > 0)
                {
                    if (currentGameState.OpponentData.Count > 0)
                    {
                        if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass > 0)
                        {
                            deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(
                                currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass);
                            deltaPlayerLastToSessionBestInClassSet = true;
                        }
                    }
                    else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0)
                    {
                        deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.PlayerLapTimeSessionBest);
                        deltaPlayerLastToSessionBestInClassSet = true;
                    }
                }
            }
            currentPosition = currentGameState.SessionData.Position;

            // check the current lap is still valid
            if (lapIsValid && currentGameState.SessionData.CompletedLaps > 0 &&
                !currentGameState.SessionData.IsNewLap && !currentGameState.SessionData.CurrentLapIsValid)
            {
                lapIsValid = false;
            }
            if (currentGameState.SessionData.IsNewLap)
            {
                lastLapTime = currentGameState.SessionData.LapTimePrevious;
                if (lastLapTime > 0 && lapIsValid) {
                    if (bestLapTime == 0 || lastLapTime < bestLapTime) {
                        bestLapTime = lastLapTime;
                    }
                }
            }

            float[] lapAndSectorsComparisonData = new float[] { -1, -1, -1, -1 };
            if (currentGameState.SessionData.IsNewSector)
            {
                isHotLapping = currentGameState.SessionData.SessionType == SessionType.HotLap || (currentGameState.OpponentData.Count == 0 || (
                    currentGameState.OpponentData.Count == 1 && currentGameState.OpponentData.First().Value.DriverRawName == currentGameState.SessionData.DriverRawName));
                if (isHotLapping)
                {
                    lapAndSectorsComparisonData[1] = currentGameState.SessionData.PlayerBestLapSector1Time;
                    lapAndSectorsComparisonData[2] = currentGameState.SessionData.PlayerBestLapSector2Time;
                    lapAndSectorsComparisonData[3] = currentGameState.SessionData.PlayerBestLapSector3Time;
                }
                else
                {
                    if (currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRace, currentGameState.carClass.carClassEnum);
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice)
                    {
                        lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass.carClassEnum);
                    }
                }
            }

            if (!currentGameState.PitData.OnInLap && !previousGameState.PitData.OnOutLap)   // as this is a new lap, check whether the *previous* state was an outlap
            {
                Boolean sectorsReportedForLap = false;                
                if (currentGameState.SessionData.IsNewLap && 
                    ((currentGameState.SessionData.SessionType == SessionType.HotLap && currentGameState.SessionData.CompletedLaps > 0) || currentGameState.SessionData.CompletedLaps > 1))
                {
                    if (lapTimesWindow == null)
                    {
                        lapTimesWindow = new List<float>(lapTimesWindowSize);
                    }                    
                    lastLapRating = getLastLapRating(currentGameState, lapAndSectorsComparisonData);

                    if (currentGameState.SessionData.PreviousLapWasValid)
                    {
                        lapTimesWindow.Insert(0, currentGameState.SessionData.LapTimePrevious);
                        if (lapIsValid)
                        {
                            Boolean playedLapTime = false;
                            if (isHotLapping)
                            {
                                // always play the laptime in hotlap mode
                                audioPlayer.playMessage(new QueuedMessage("laptime",
                                        MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this));
                                playedLapTime = true;
                            }
                            else if (((currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice) && frequencyOfPlayerQualAndPracLapTimeReports > random.NextDouble() * 10) 
                                || (currentGameState.SessionData.SessionType == SessionType.Race && frequencyOfPlayerRaceLapTimeReports > random.NextDouble() * 10))
                            {
                                // usually play it in practice / qual mode, occasionally play it in race mode
                                QueuedMessage gapFillerLapTime = new QueuedMessage("laptime",
                                    MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious)), 0, this);
                                if (currentGameState.SessionData.SessionType == SessionType.Race)
                                {
                                    gapFillerLapTime.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceLapTimeReports;
                                }
                                audioPlayer.playMessage(gapFillerLapTime);
                                playedLapTime = true;
                            }

                            if (deltaPlayerLastToSessionBestInClassSet && 
                                (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice ||
                                currentGameState.SessionData.SessionType == SessionType.HotLap))
                            {
                                if (currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.OpponentData.Count == 0)
                                {
                                    if (lastLapRating == LastLapRating.BEST_IN_CLASS || (deltaPlayerLastToSessionBestInClass <= TimeSpan.Zero))
                                    {
                                        audioPlayer.playMessage(new QueuedMessage(folderPersonalBest, 0, this));
                                    }
                                    else if (deltaPlayerLastToSessionBestInClass < TimeSpan.FromMilliseconds(50))
                                    {
                                        audioPlayer.playMessage(new QueuedMessage(folderLessThanATenthOffThePace, 0, this));
                                    }
                                    else if (deltaPlayerLastToSessionBestInClass < TimeSpan.MaxValue)
                                    {
                                        audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, deltaPlayerLastToSessionBestInClass, folderGapOutroOffPace), 0, this));
                                    }
                                    if (practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > random.NextDouble() * 10)
                                    {
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(SectorReportOption.COMBINED, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], true);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage("sectorsHotLap", sectorMessageFragments, 0, this));
                                            sectorsReportedForLap = true;
                                        }
                                    }
                                }
                                    // need to be careful with the rating here as it's based on the known opponent laps, and we may have joined the session part way through
                                else if (currentGameState.SessionData.Position == 1) 
                                {
                                    // TODO: rework this grotty logic...
                                    Boolean newGapToSecond = false;
                                    if (previousGameState != null && previousGameState.SessionData.Position > 1)
                                    {                                        
                                        newGapToSecond = true;
                                        if (currentGameState.SessionData.SessionType == SessionType.Qualify)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage(Position.folderPole, 0, this));
                                        }
                                        else if (currentGameState.SessionData.SessionType == SessionType.Practice)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage(Position.folderStub + 1, 0, this));
                                        }
                                    }
                                    if (deltaPlayerLastToSessionBestInClass < lastGapToSecondWhenLeadingPracOrQual)
                                    {
                                        newGapToSecond = true;
                                        lastGapToSecondWhenLeadingPracOrQual = deltaPlayerLastToSessionBestInClass;
                                    }
                                    if (newGapToSecond)
                                    {
                                        TimeSpan gapBehind = deltaPlayerLastToSessionBestInClass.Negate();
                                        // only play qual / prac deltas for Raceroom as the PCars data is inaccurate for sessions joined part way through
                                        if ((!disablePCarspracAndQualPoleDeltaReports || 
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.raceRoom.gameEnum) &&
                                            (gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50))
                                        {
                                            // delay this a bit...
                                            audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                                MessageContents(folderGapIntro, gapBehind, folderQuickerThanSecondPlace), random.Next(0, 20), this));
                                        }
                                    }
                                }
                                else
                                {
                                    if (lastLapRating == LastLapRating.PERSONAL_BEST_STILL_SLOW || lastLapRating == LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER ||
                                        lastLapRating == LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER)
                                    {
                                        audioPlayer.playMessage(new QueuedMessage(folderPersonalBest, 0, this));
                                    }
                                    // don't read this message if the rounded time gap is 0.0 seconds or it's more than 59 seconds
                                    // only play qual / prac deltas for Raceroom as the PCars data is inaccurate for sessions joined part way through
                                    if ((!disablePCarspracAndQualPoleDeltaReports || CrewChief.gameDefinition.gameEnum == GameDefinition.raceRoom.gameEnum) &&
                                        (deltaPlayerLastToSessionBestInClass.Seconds > 0 || deltaPlayerLastToSessionBestInClass.Milliseconds > 50) &&
                                        deltaPlayerLastToSessionBestInClass.Seconds < 60)
                                    {
                                        // delay this a bit...
                                        audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, deltaPlayerLastToSessionBestInClass, folderGapOutroOffPace), random.Next(0, 20), this));
                                    }
                                    if (practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > random.NextDouble() * 10)
                                    {
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(SectorReportOption.COMBINED, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], true);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage("sectorsQual", sectorMessageFragments, 0, this));
                                            sectorsReportedForLap = true;
                                        }
                                    }
                                }
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                Boolean playedLapMessage = false;
                                if (frequencyOfPlayerRaceLapTimeReports > random.NextDouble() * 10)
                                {
                                    float pearlLikelihood = 0.8f;
                                    switch (lastLapRating)
                                    {
                                        case LastLapRating.BEST_OVERALL:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderBestLapInRace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.BEST_IN_CLASS:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderBestLapInRaceForClass, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.SETTING_CURRENT_PACE:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderSettingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.CLOSE_TO_CURRENT_PACE:
                                            // don't keep playing this one
                                            if (random.NextDouble() < 0.5)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.playMessage(new QueuedMessage(folderMatchingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            }
                                            break;
                                        case LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER:
                                        case LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood);
                                            break;
                                        case LastLapRating.PERSONAL_BEST_STILL_SLOW:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderPersonalBest, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood);
                                            break;
                                        case LastLapRating.CLOSE_TO_OVERALL_LEADER:
                                        case LastLapRating.CLOSE_TO_CLASS_LEADER:
                                            // this is an OK lap but not a PB. We only want to say "decent lap" occasionally here
                                            if (random.NextDouble() < 0.2)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.playMessage(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                if (raceSectorReportsAtLapEnd && frequencyOfRaceSectorDeltaReports > random.NextDouble() * 10)
                                {
                                    double r = random.NextDouble();
                                    SectorReportOption reportOption = SectorReportOption.COMBINED;
                                    if (playedLapTime && playedLapMessage)
                                    {
                                        // if we've already played a laptime and lap rating, use the short sector message.
                                        reportOption = SectorReportOption.WORST_ONLY;
                                    }
                                    else if (r > 0.6 || ((playedLapTime || playedLapMessage) && r > 0.3))
                                    {
                                        // if we've played one of these, usually use the abbrieviated version. If we've played neither, sometimes use the abbrieviated version
                                        reportOption = SectorReportOption.BEST_AND_WORST;
                                    }
                                    
                                    List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], false);
                                    if (sectorMessageFragments.Count > 0)
                                    {
                                        QueuedMessage message = new QueuedMessage("sectorsRace", sectorMessageFragments, 0, this);
                                        message.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceSectorDeltaReports;
                                        audioPlayer.playMessage(message);
                                        sectorsReportedForLap = true;
                                    }
                                }

                                // play the consistency message if we've not played the good lap message, or sometimes
                                // play them both
                                Boolean playConsistencyMessage = !playedLapMessage || random.NextDouble() < 0.25;
                                if (playConsistencyMessage && currentGameState.SessionData.CompletedLaps >= lastConsistencyUpdate + lapTimesWindowSize &&
                                    lapTimesWindow.Count >= lapTimesWindowSize)
                                {
                                    ConsistencyResult consistency = checkAgainstPreviousLaps();
                                    if (consistency == ConsistencyResult.CONSISTENT)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.playMessage(new QueuedMessage(folderConsistentTimes, random.Next(0, 20), this));
                                    }
                                    else if (consistency == ConsistencyResult.IMPROVING)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.playMessage(new QueuedMessage(folderImprovingTimes, random.Next(0, 20), this));
                                    }
                                    else if (consistency == ConsistencyResult.WORSENING)
                                    {
                                        // don't play the worsening message if the lap rating is good
                                        if (lastLapRating == LastLapRating.BEST_IN_CLASS || lastLapRating == LastLapRating.BEST_OVERALL ||
                                            lastLapRating == LastLapRating.SETTING_CURRENT_PACE || lastLapRating == LastLapRating.CLOSE_TO_CURRENT_PACE)
                                        {
                                            Console.WriteLine("Skipping 'worsening' laptimes message - inconsistent with lap rating");
                                        }
                                        else
                                        {
                                            lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                            audioPlayer.playMessage(new QueuedMessage(folderWorseningTimes, random.Next(0, 20), this));
                                        }
                                    }
                                }
                            }
                        }
                    }
                    lapIsValid = true;
                }
                // report sector delta at the completion of a sector?
                if (!sectorsReportedForLap && currentGameState.SessionData.IsNewSector && 
                    ((currentGameState.SessionData.SessionType == SessionType.Race && raceSectorReportsAtEachSector) ||
                     (currentGameState.SessionData.SessionType != SessionType.Race && practiceAndQualSectorReportsAtEachSector))) 
                {
                    double r = random.NextDouble() * 10;
                    Boolean canPlayForRace = frequencyOfRaceSectorDeltaReports > r;
                    Boolean canPlayForPracAndQual = frequencyOfPracticeAndQualSectorDeltaReports > r;
                    if ((currentGameState.SessionData.SessionType == SessionType.Race && canPlayForRace) ||
                        ((currentGameState.SessionData.SessionType == SessionType.Practice || currentGameState.SessionData.SessionType == SessionType.Qualify ||
                        currentGameState.SessionData.SessionType == SessionType.HotLap) && canPlayForPracAndQual))
                    {
                        float playerSector = -1;
                        float comparisonSector = -1;
                        SectorSet sectorEnum = SectorSet.NONE;
                        switch (currentGameState.SessionData.SectorNumber)
                        {
                            case 1:
                                playerSector = currentGameState.SessionData.LastSector3Time;
                                comparisonSector = lapAndSectorsComparisonData[3];
                                sectorEnum = SectorSet.THREE;
                                break;
                            case 2:
                                playerSector = currentGameState.SessionData.LastSector1Time;
                                comparisonSector = lapAndSectorsComparisonData[1];
                                sectorEnum = SectorSet.ONE;
                                break;
                            case 3:
                                playerSector = currentGameState.SessionData.LastSector2Time;
                                comparisonSector = lapAndSectorsComparisonData[2];
                                sectorEnum = SectorSet.TWO;
                                break;
                        }
                        if (playerSector > 0 && comparisonSector > 0)
                        {
                            String folder = getFolderForSectorCombination(getEnumForSectorDelta(playerSector - comparisonSector, currentGameState.SessionData.SessionType != SessionType.Race), sectorEnum);
                            if (folder != null)
                            {
                                audioPlayer.playMessage(new QueuedMessage(folder, random.Next(2, 4), this));
                            }
                        }
                    }
                }
            }
        }
               
        private ConsistencyResult checkAgainstPreviousLaps()
        {
            Boolean isImproving = true;
            Boolean isWorsening = true;
            Boolean isConsistent = true;

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                // check the lap time was recorded
                if (lapTimesWindow[index] <= 0)
                {
                    Console.WriteLine("no data for consistency check");
                    lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
                    return ConsistencyResult.NOT_APPLICABLE;
                }
                if (lapTimesWindow[index] >= lapTimesWindow[index + 1])
                {
                    isImproving = false;
                    break;
                }
            }

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                if (lapTimesWindow[index] <= lapTimesWindow[index + 1])
                {
                    isWorsening = false;
                }
            }

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                float lastLap = lapTimesWindow[index];
                float lastButOneLap = lapTimesWindow[index + 1];
                float consistencyRange = (lastButOneLap * consistencyLimit) / 100;
                if (lastLap > lastButOneLap + consistencyRange || lastLap < lastButOneLap - consistencyRange)
                {
                    isConsistent = false;
                }
            }

            // todo: untangle this mess....
            if (isImproving)
            {
                if (lastConsistencyMessage == ConsistencyResult.IMPROVING)
                {
                    // don't play the same improving message - see if the consistent message might apply
                    if (isConsistent)
                    {
                        lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                        return ConsistencyResult.CONSISTENT;
                    }
                }
                else
                {
                    lastConsistencyMessage = ConsistencyResult.IMPROVING;
                    return ConsistencyResult.IMPROVING;
                }
            }
            if (isWorsening)
            {
                if (lastConsistencyMessage == ConsistencyResult.WORSENING)
                {
                    // don't play the same worsening message - see if the consistent message might apply
                    if (isConsistent)
                    {
                        lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                        return ConsistencyResult.CONSISTENT;
                    }
                }
                else
                {
                    lastConsistencyMessage = ConsistencyResult.WORSENING;
                    return ConsistencyResult.WORSENING;
                }
            }
            if (isConsistent)
            {
                lastConsistencyMessage = ConsistencyResult.CONSISTENT;
                return ConsistencyResult.CONSISTENT;
            }
            return ConsistencyResult.NOT_APPLICABLE;
        }

        private enum ConsistencyResult
        {
            NOT_APPLICABLE, CONSISTENT, IMPROVING, WORSENING
        }

        private LastLapRating getLastLapRating(GameStateData currentGameState, float[] bestLapDataForOpponents)
        {
            if (currentGameState.SessionData.PreviousLapWasValid && currentGameState.SessionData.LapTimePrevious > 0)
            {
                float closeThreshold = currentGameState.SessionData.LapTimePrevious * goodLapPercent / 100;
                float matchingRacePaceThreshold = currentGameState.SessionData.LapTimePrevious * matchingRacePacePercent / 100;
                if (currentGameState.SessionData.OverallSessionBestLapTime == currentGameState.SessionData.LapTimePrevious)
                {
                    return LastLapRating.BEST_OVERALL;
                }
                else if (currentGameState.SessionData.PlayerClassSessionBestLapTime == currentGameState.SessionData.LapTimePrevious)
                {
                    return LastLapRating.BEST_IN_CLASS;
                }
                else if (currentGameState.SessionData.SessionType == SessionType.Race && bestLapDataForOpponents[0] > 0 && bestLapDataForOpponents[0] >= currentGameState.SessionData.LapTimePrevious) 
                {
                    return LastLapRating.SETTING_CURRENT_PACE;                
                }
                else if (currentGameState.SessionData.SessionType == SessionType.Race && bestLapDataForOpponents[0] > 0 && bestLapDataForOpponents[0] > currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_CURRENT_PACE;
                }
                else if (currentGameState.SessionData.LapTimePrevious == currentGameState.SessionData.PlayerLapTimeSessionBest)
                {
                    if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall > currentGameState.SessionData.LapTimePrevious - closeThreshold)
                    {
                        return LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER;
                    }
                    else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass > currentGameState.SessionData.LapTimePrevious - closeThreshold)
                    {
                        return LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER;
                    }
                    else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass > 0 || currentGameState.SessionData.OpponentsLapTimeSessionBestOverall > 0)
                    {
                        return LastLapRating.PERSONAL_BEST_STILL_SLOW;
                    }
                }
                else if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall >= currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_OVERALL_LEADER;
                }
                else if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass >= currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_CLASS_LEADER;
                }
                else if (currentGameState.SessionData.PlayerLapTimeSessionBest >= currentGameState.SessionData.LapTimePrevious - closeThreshold)
                {
                    return LastLapRating.CLOSE_TO_PERSONAL_BEST;
                }
                else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0)
                {
                    return LastLapRating.MEH;
                }
            }
            return LastLapRating.NO_DATA;
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHAT_ARE_MY_SECTOR_TIMES))
            {
                if (currentGameState.SessionData.LastSector1Time > -1 && currentGameState.SessionData.LastSector2Time > -1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sectorTimes",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector1Time), 
                        TimeSpan.FromSeconds(currentGameState.SessionData.LastSector2Time), TimeSpan.FromSeconds(currentGameState.SessionData.LastSector3Time)), 0, this), false);
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                }
                
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_LAST_SECTOR_TIME))
            {
                if (currentGameState.SessionData.SectorNumber == 1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector3Time",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector3Time)), 0, this), false);
                }
                else if (currentGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.LastSector1Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector1Time",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector1Time)), 0, this), false);
                }
                else if (currentGameState.SessionData.SectorNumber == 3 && currentGameState.SessionData.LastSector2Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector2Time",
                        MessageContents(TimeSpan.FromSeconds(currentGameState.SessionData.LastSector2Time)), 0, this), false);
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                }
                
            }
            else if (voiceMessage.Contains(SpeechRecogniser.BEST_LAP) ||
                voiceMessage.Contains(SpeechRecogniser.BEST_LAP_TIME))
            {
                if (bestLapTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("bestLapTime",
                        MessageContents(TimeSpan.FromSeconds(bestLapTime)), 0, this), false);
                    
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                    
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHAT_WAS_MY_LAST_LAP_TIME))
            {
                if (lastLapTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("lastLapTime",
                        MessageContents(folderLapTimeIntro, TimeSpan.FromSeconds(lastLapTime)), 0, this), false);
                    
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this), false);
                    
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_PACE))
            {
                if (sessionType == SessionType.Race)
                {
                    float[] bestOpponentLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRace, currentGameState.carClass.carClassEnum);

                    if (bestOpponentLapData[0] > -1 && lastLapRating != LastLapRating.NO_DATA)
                    {
                        TimeSpan lapToCompare = TimeSpan.FromSeconds(lastLapTime - bestOpponentLapData[0]);
                        String timeToFindFolder = null;
                        if (lapToCompare.Seconds == 0 && lapToCompare.Milliseconds < 200)
                        {
                            timeToFindFolder = folderNeedToFindOneMoreTenth;
                        }
                        else if (lapToCompare.Seconds == 0 && lapToCompare.Milliseconds < 600)
                        {
                            timeToFindFolder = folderNeedToFindAFewMoreTenths;
                        }
                        else if ((lapToCompare.Seconds == 1 && lapToCompare.Milliseconds < 500) ||
                            (lapToCompare.Seconds == 0 && lapToCompare.Milliseconds >= 600))
                        {
                            timeToFindFolder = folderNeedToFindASecond;
                        }
                        else if ((lapToCompare.Seconds == 1 && lapToCompare.Milliseconds >= 500) ||
                            lapToCompare.Seconds > 1)
                        {
                            timeToFindFolder = folderNeedToFindMoreThanASecond;
                        }
                        List<MessageFragment> messages = new List<MessageFragment>();
                        switch (lastLapRating)
                        {
                            case LastLapRating.BEST_OVERALL:
                            case LastLapRating.BEST_IN_CLASS:
                            case LastLapRating.SETTING_CURRENT_PACE:
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderSettingCurrentRacePace, 0, null), false);
                                break;
                            case LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER:
                            case LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER:
                            case LastLapRating.CLOSE_TO_OVERALL_LEADER:
                            case LastLapRating.CLOSE_TO_CLASS_LEADER:
                            case LastLapRating.PERSONAL_BEST_STILL_SLOW:
                            case LastLapRating.CLOSE_TO_PERSONAL_BEST:
                            case LastLapRating.CLOSE_TO_CURRENT_PACE:
                                if (timeToFindFolder == null || timeToFindFolder != folderNeedToFindMoreThanASecond)
                                {
                                    if (lastLapRating == LastLapRating.CLOSE_TO_CURRENT_PACE)
                                    {
                                        messages.Add(MessageFragment.Text(folderMatchingCurrentRacePace));
                                    }
                                    else
                                    {
                                        messages.Add(MessageFragment.Text(folderPaceOK));
                                    }
                                }
                                if (timeToFindFolder != null)
                                {
                                    messages.Add(MessageFragment.Text(timeToFindFolder));
                                }
                                if (messages.Count > 0)
                                {
                                    audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null), false);
                                }                                    
                                break;
                            case LastLapRating.MEH:
                                messages.Add(MessageFragment.Text(folderPaceBad));
                                if (timeToFindFolder != null)
                                {
                                    messages.Add(MessageFragment.Text(timeToFindFolder));
                                }
                                audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null), false);
                                    break;
                            default:
                                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                                break;                     
                        }
                        SectorReportOption reportOption = SectorReportOption.COMBINED;
                        double r = random.NextDouble();
                        // usually report the combined sectors
                        if (r > 0.33)
                        {
                            reportOption = SectorReportOption.WORST_ONLY;
                        }
                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, bestOpponentLapData[1],
                            currentGameState.SessionData.LastSector2Time, bestOpponentLapData[2], currentGameState.SessionData.LastSector3Time, bestOpponentLapData[3], false);
                        if (sectorDeltaMessages.Count > 0)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("race_sector_times_report", sectorDeltaMessages, 0, null), false);
                        }
                        
                    }
                    else {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                        
                    }
                }
                else
                {
                    if (deltaPlayerLastToSessionBestInClassSet)
                    {
                        if (deltaPlayerLastToSessionBestInClass <= TimeSpan.Zero)
                        {   
                            if (sessionType == SessionType.Qualify && currentPosition == 1)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderPole, 0, null), false);
                            }
                            else
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderQuickestOverall, 0, null), false);
                            }
                            TimeSpan gapBehind = deltaPlayerLastToSessionBestInClass.Negate();
                            if (gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50)
                            {
                                // delay this a bit...
                                audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                    MessageContents(folderGapIntro, gapBehind, folderQuickerThanSecondPlace), 0, this));
                            }
                        }
                        else if (deltaPlayerLastToSessionBestInClass.Seconds == 0 && deltaPlayerLastToSessionBestInClass.Milliseconds < 50)
                        {
                            if (currentPosition > 1)
                            {
                                // should always trigger
                                audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderStub + currentPosition, 0, null), false);
                            }
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderLessThanATenthOffThePace, 0, null), false);
                        }
                        else
                        {
                            if (currentPosition > 1)
                            {
                                // should always trigger
                                audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderStub + currentPosition, 0, null), false);
                            }
                            audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeNotRaceGap",
                                MessageContents(deltaPlayerLastToSessionBestInClass, folderGapOutroOffPace), 0, null), false);
                        }

                        // TODO: wrap this in a try-catch until I work out why the array indices are being screwed up in online races (yuk...)
                        try
                        {

                            float[] bestOpponentLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass.carClassEnum);
                            SectorReportOption reportOption = SectorReportOption.COMBINED;
                            double r = random.NextDouble();
                            // usually report the combined sectors, occasionally report all
                            if (r > 0.33)
                            {
                                reportOption = SectorReportOption.ALL_SECTORS;
                            }
                            List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, bestOpponentLapData[1],
                                currentGameState.SessionData.LastSector2Time, bestOpponentLapData[2], currentGameState.SessionData.LastSector3Time, bestOpponentLapData[3], true);
                            if (sectorDeltaMessages.Count > 0)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("non-race_sector_times_report", sectorDeltaMessages, 0, null), false);
                            }
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unable to get sector deltas: " + e.Message);
                        }
                        
                    }
                    else {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                        
                    }
                }
            }
        }

        private enum LastLapRating
        {
            BEST_OVERALL, BEST_IN_CLASS, SETTING_CURRENT_PACE, CLOSE_TO_CURRENT_PACE, PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER, PERSONAL_BEST_CLOSE_TO_CLASS_LEADER,
            PERSONAL_BEST_STILL_SLOW, CLOSE_TO_OVERALL_LEADER, CLOSE_TO_CLASS_LEADER, CLOSE_TO_PERSONAL_BEST, MEH, NO_DATA
        }


        // ----------------- 'mode 1' - will attempt to piece together sector pace reports from fragments ---------------
        private enum SectorDeltaEnum
        {
            FASTEST, FAST, TENTH_OFF_PACE, TWO_TENTHS_OFF_PACE, A_FEW_TENTHS_OFF_PACE, A_SECOND_OFF_PACE, MORE_THAN_A_SECOND_OFF_PACE, UNKNOWN
        }

        private enum SectorSet
        {
            ONE, TWO, THREE, ONE_AND_TWO, ONE_AND_THREE, TWO_AND_THREE, ALL, NONE
        }

        private SectorDeltaEnum getEnumForSectorDelta(float delta, Boolean comparisonIncludesAllLaps)
        {
            if (delta == float.MaxValue)
            {
                return SectorDeltaEnum.UNKNOWN;
            }
            if (delta <= 0.0 && comparisonIncludesAllLaps)
            {
                return SectorDeltaEnum.FASTEST;
            }
            else if ((delta <= 0.0 && !comparisonIncludesAllLaps) || delta > 0.0 && delta < 0.05)
            {
                return SectorDeltaEnum.FAST;
            }
            else if (delta >= 0.05 && delta < 0.15)
            {
                return SectorDeltaEnum.TENTH_OFF_PACE;
            }
            else if (delta >= 0.15 && delta < 0.3)
            {
                return SectorDeltaEnum.TWO_TENTHS_OFF_PACE;
            }
            else if (delta >= 0.3 && delta < 0.7)
            {
                return SectorDeltaEnum.A_FEW_TENTHS_OFF_PACE;
            }
            else if (delta >= 0.7 && delta < 1.2)
            {
                return SectorDeltaEnum.A_SECOND_OFF_PACE;
            }
            else if (delta >= 1.2)
            {
                return SectorDeltaEnum.MORE_THAN_A_SECOND_OFF_PACE;
            }
            return SectorDeltaEnum.UNKNOWN;
        }

        private String getFolderForSectorCombination(SectorDeltaEnum delta, SectorSet sectorSet)
        {
            switch (delta)
            {
                case SectorDeltaEnum.FASTEST:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsFastest;
                        case SectorSet.ONE:
                            return folderSector1Fastest;
                        case SectorSet.TWO:
                            return folderSector2Fastest;
                        case SectorSet.THREE:
                            return folderSector3Fastest;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2Fastest;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3Fastest;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3Fastest;
                    }
                    break;
                case SectorDeltaEnum.FAST:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsFast;
                        case SectorSet.ONE:
                            return folderSector1Fast;
                        case SectorSet.TWO:
                            return folderSector2Fast;
                        case SectorSet.THREE:
                            return folderSector3Fast;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2Fast;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3Fast;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3Fast;
                    }
                    break;
                case SectorDeltaEnum.TENTH_OFF_PACE:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsATenthOffThePace;
                        case SectorSet.ONE:
                            return folderSector1ATenthOffThePace;
                        case SectorSet.TWO:
                            return folderSector2ATenthOffThePace;
                        case SectorSet.THREE:
                            return folderSector3ATenthOffThePace;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2ATenthOffThePace;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3ATenthOffThePace;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3ATenthOffThePace;
                    }
                    break;
                case SectorDeltaEnum.TWO_TENTHS_OFF_PACE:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsTwoTenthsOffThePace;
                        case SectorSet.ONE:
                            return folderSector1TwoTenthsOffThePace;
                        case SectorSet.TWO:
                            return folderSector2TwoTenthsOffThePace;
                        case SectorSet.THREE:
                            return folderSector3TwoTenthsOffThePace;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2TwoTenthsOffThePace;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3TwoTenthsOffThePace;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3TwoTenthsOffThePace;
                    }
                    break;
                case SectorDeltaEnum.A_FEW_TENTHS_OFF_PACE:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsAFewTenthsOffThePace;
                        case SectorSet.ONE:
                            return folderSector1AFewTenthsOffThePace;
                        case SectorSet.TWO:
                            return folderSector2AFewTenthsOffThePace;
                        case SectorSet.THREE:
                            return folderSector3AFewTenthsOffThePace;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2AFewTenthsOffThePace;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3AFewTenthsOffThePace;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3AFewTenthsOffThePace;
                    }
                    break;
                case SectorDeltaEnum.A_SECOND_OFF_PACE:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsASecondOffThePace;
                        case SectorSet.ONE:
                            return folderSector1ASecondOffThePace;
                        case SectorSet.TWO:
                            return folderSector2ASecondOffThePace;
                        case SectorSet.THREE:
                            return folderSector3ASecondOffThePace;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2ASecondOffThePace;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3ASecondOffThePace;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3ASecondOffThePace;
                    }
                    break;
                case SectorDeltaEnum.MORE_THAN_A_SECOND_OFF_PACE:
                    switch (sectorSet)
                    {
                        case SectorSet.ALL:
                            return folderAllSectorsMoreThanASecondOffThePace;
                        case SectorSet.ONE:
                            return folderSector1MoreThanASecondOffThePace;
                        case SectorSet.TWO:
                            return folderSector2MoreThanASecondOffThePace;
                        case SectorSet.THREE:
                            return folderSector3MoreThanASecondOffThePace;
                        case SectorSet.ONE_AND_TWO:
                            return folderSector1and2MoreThanASecondOffThePace;
                        case SectorSet.ONE_AND_THREE:
                            return folderSector1and3MoreThanASecondOffThePace;
                        case SectorSet.TWO_AND_THREE:
                            return folderSector2and3MoreThanASecondOffThePace;
                    }
                    break;
            }
            return null;
        }

        private List<String> getFoldersForSectorsAndDeltas(float sector1delta, float sector2delta, float sector3delta, Boolean comparisonIncludesAllLaps)
        {
            List<String> folders = new List<string>();
            SectorDeltaEnum s1 = getEnumForSectorDelta(sector1delta, comparisonIncludesAllLaps);
            SectorDeltaEnum s2 = getEnumForSectorDelta(sector2delta, comparisonIncludesAllLaps);
            SectorDeltaEnum s3 = getEnumForSectorDelta(sector3delta, comparisonIncludesAllLaps);

            if (s1 != SectorDeltaEnum.UNKNOWN)
            {
                if (s2 == s1 && s3 == s1)
                {
                    // all three sectors
                    folders.Add(getFolderForSectorCombination(s1, SectorSet.ALL));
                    return folders;
                }
                else if (s2 == s1)
                {
                    folders.Add(getFolderForSectorCombination(s1, SectorSet.ONE_AND_TWO));
                    folders.Add(getFolderForSectorCombination(s3, SectorSet.THREE));
                    return folders;
                }
                else if (s3 == s1)
                {
                    folders.Add(getFolderForSectorCombination(s1, SectorSet.ONE_AND_THREE));
                    folders.Add(getFolderForSectorCombination(s2, SectorSet.TWO));
                    return folders;
                }
                else
                {
                    folders.Add(getFolderForSectorCombination(s1, SectorSet.ONE));
                }
            }
            if (s2 != SectorDeltaEnum.UNKNOWN)
            {
                if (s2 == s3)
                {
                    folders.Add(getFolderForSectorCombination(s2, SectorSet.TWO_AND_THREE));
                    return folders;
                }
                else
                {
                    folders.Add(getFolderForSectorCombination(s2, SectorSet.TWO));
                }
            }
            if (s3 != SectorDeltaEnum.UNKNOWN)
            {
                folders.Add(getFolderForSectorCombination(s3, SectorSet.THREE));
            }
            return folders;
        }
        
        private List<MessageFragment> getSectorDeltaMessagesCombined(float playerSector1, float comparisonSector1, float playerSector2,
            float comparisonSector2, float playerSector3, float comparisonSector3, Boolean comparisonIncludesAllLaps)
        {
            float sector1delta = float.MaxValue;
            float sector2delta = float.MaxValue;
            float sector3delta = float.MaxValue;
            if (playerSector1 > 0 && comparisonSector1 > 0)
            {
                sector1delta = playerSector1 - comparisonSector1;
            }
            if (playerSector2 > 0 && comparisonSector2 > 0)
            {
                sector2delta = playerSector2 - comparisonSector2;
            }
            if (playerSector3 > 0 && comparisonSector3 > 0)
            {
                sector3delta = playerSector3 - comparisonSector3;
            }

            List<String> folders = getFoldersForSectorsAndDeltas(sector1delta, sector2delta, sector3delta, comparisonIncludesAllLaps);
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            foreach (String folder in folders)
            {
                if (folder != null)
                {
                    messageFragments.Add(MessageFragment.Text(folder));
                }
            }
            return messageFragments;
        }


        private List<MessageFragment> getSectorDeltaMessages(SectorReportOption reportOption, float playerSector1, float comparisonSector1, float playerSector2,
            float comparisonSector2, float playerSector3, float comparisonSector3, Boolean comparisonIncludesAllLaps)
        {
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            if (reportOption == SectorReportOption.ALL_SECTORS)
            {
                if (playerSector1 > 0 && comparisonSector1 > 0)
                {
                    String folder = getFolderForSectorCombination(getEnumForSectorDelta(playerSector1 - comparisonSector1, comparisonIncludesAllLaps), SectorSet.ONE);
                    if (folder != null)
                    {
                        messageFragments.Add(MessageFragment.Text(folder));
                    }
                }
                if (playerSector2 > 0 && comparisonSector2 > 0)
                {
                    String folder = getFolderForSectorCombination(getEnumForSectorDelta(playerSector2 - comparisonSector2, comparisonIncludesAllLaps), SectorSet.TWO);
                    if (folder != null)
                    {
                        messageFragments.Add(MessageFragment.Text(folder));
                    }
                }
                if (playerSector3 > 0 && comparisonSector3 > 0)
                {
                    String folder = getFolderForSectorCombination(getEnumForSectorDelta(playerSector3 - comparisonSector3, comparisonIncludesAllLaps), SectorSet.THREE);
                    if (folder != null)
                    {
                        messageFragments.Add(MessageFragment.Text(folder));
                    }
                }
            }
            else if (reportOption == SectorReportOption.COMBINED)
            {
                return getSectorDeltaMessagesCombined(playerSector1, comparisonSector1, playerSector2, comparisonSector2, playerSector3, comparisonSector3, comparisonIncludesAllLaps);
            }
            else
            {
                float maxDelta = float.MinValue;
                SectorSet maxDeltaSector = SectorSet.NONE;
                float minDelta = float.MaxValue;
                SectorSet minDeltaSector = SectorSet.NONE;

                if (playerSector1 > 0 && comparisonSector1 > 0)
                {
                    float delta = playerSector1 - comparisonSector1;
                    if (delta > maxDelta)
                    {
                        maxDelta = delta;
                        maxDeltaSector = SectorSet.ONE;
                    }
                    else if (delta < minDelta)
                    {
                        minDelta = delta;
                        minDeltaSector = SectorSet.ONE;
                    }
                }
                if (playerSector2 > 0 && comparisonSector2 > 0)
                {
                    float delta = playerSector2 - comparisonSector2;
                    if (delta > maxDelta)
                    {
                        if (maxDeltaSector != SectorSet.NONE)
                        {
                            minDelta = maxDelta;
                            minDeltaSector = maxDeltaSector;
                        }
                        maxDelta = playerSector2 - comparisonSector2;
                        maxDeltaSector = SectorSet.TWO;
                    }
                    else if (delta < minDelta)
                    {
                        if (minDeltaSector != SectorSet.NONE)
                        {
                            maxDelta = minDelta;
                            maxDeltaSector = minDeltaSector;
                        }
                        minDelta = playerSector2 - comparisonSector2;
                        minDeltaSector = SectorSet.TWO;
                    }
                }
                if (playerSector3 > 0 && comparisonSector3 > 0)
                {
                    float delta = playerSector3 - comparisonSector3;
                    if (delta > maxDelta)
                    {
                        if (maxDeltaSector != SectorSet.NONE && minDeltaSector == SectorSet.NONE)
                        {
                            minDelta = maxDelta;
                            minDeltaSector = maxDeltaSector;
                        }
                        maxDelta = playerSector3 - comparisonSector3;
                        maxDeltaSector = SectorSet.THREE;
                    }
                    else if (delta < minDelta)
                    {
                        if (minDeltaSector != SectorSet.NONE && maxDeltaSector == SectorSet.NONE)
                        {
                            maxDelta = minDelta;
                            maxDeltaSector = minDeltaSector;
                        }
                        minDelta = playerSector3 - comparisonSector3;
                        minDeltaSector = SectorSet.THREE;
                    }
                }
                if (minDeltaSector != SectorSet.NONE && reportOption != SectorReportOption.WORST_ONLY)
                {
                    String folder = getFolderForSectorCombination(getEnumForSectorDelta(minDelta, comparisonIncludesAllLaps), minDeltaSector);
                    if (folder != null)
                    {
                        messageFragments.Add(MessageFragment.Text(folder));
                    }
                }
                if (maxDeltaSector != SectorSet.NONE)
                {
                    String folder = getFolderForSectorCombination(getEnumForSectorDelta(maxDelta, comparisonIncludesAllLaps), maxDeltaSector);
                    if (folder != null)
                    {
                        messageFragments.Add(MessageFragment.Text(folder));
                    }
                }
            }
            return messageFragments;
        }

        private enum SectorReportOption
        {
            ALL_SECTORS, BEST_AND_WORST, WORST_ONLY, COMBINED
        }
    }
}
