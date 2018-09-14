using CrewChiefV4.GameState;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

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

        Boolean reportAllLaptimesInHotlapMode = UserSettings.GetUserSettings().getBoolean("report_all_laps_in_hotlap_mode");

        int maxQueueLengthForRaceSectorDeltaReports = 0;
        int maxQueueLengthForRaceLapTimeReports = 0;

        // for qualifying:
        // "that was a 1:34.2, you're now 0.4 seconds off the pace"
        public static String folderLapTimeIntro = "lap_times/time_intro";
        public static String folderGapIntro = "lap_times/gap_intro";

        public static String folderGapOutroOffPace = "lap_times/gap_outro_off_pace";

        public static String folderSelfGapOutroOffPace = "lap_times/off_the_self_pace";

        private String folderLessThanATenthOffThePace = "lap_times/less_than_a_tenth_off_the_pace";

        private String folderSelfLessThanATenthOffThePace = "lap_times/less_than_a_tenth_off_self_pace";

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

        public static String folderSector1Fastest = "lap_times/sector1_fastest";
        public static String folderSector2Fastest = "lap_times/sector2_fastest";
        public static String folderSector3Fastest = "lap_times/sector3_fastest";
        public static String folderSector1and2Fastest = "lap_times/sector1_and_2_fastest";
        public static String folderSector2and3Fastest = "lap_times/sector2_and_3_fastest";
        public static String folderSector1and3Fastest = "lap_times/sector1_and_3_fastest";
        public static String folderAllSectorsFastest = "lap_times/sector_all_fastest";

        public static String folderSector1Fast = "lap_times/sector1_fast";
        public static String folderSector2Fast = "lap_times/sector2_fast";
        public static String folderSector3Fast = "lap_times/sector3_fast";
        public static String folderSector1and2Fast = "lap_times/sector1_and_2_fast";
        public static String folderSector2and3Fast = "lap_times/sector2_and_3_fast";
        public static String folderSector1and3Fast = "lap_times/sector1_and_3_fast";
        public static String folderAllSectorsFast = "lap_times/sector_all_fast";

        public static String folderSector1ATenthOffThePace = "lap_times/sector1_a_tenth_off_pace";
        public static String folderSector2ATenthOffThePace = "lap_times/sector2_a_tenth_off_pace";
        public static String folderSector3ATenthOffThePace = "lap_times/sector3_a_tenth_off_pace";
        public static String folderSector1and2ATenthOffThePace = "lap_times/sector1_and_2_a_tenth_off_pace";
        public static String folderSector2and3ATenthOffThePace = "lap_times/sector2_and_3_a_tenth_off_pace";
        public static String folderSector1and3ATenthOffThePace = "lap_times/sector1_and_3_a_tenth_off_pace";
        public static String folderAllSectorsATenthOffThePace = "lap_times/sector_all_a_tenth_off_pace";

        public static String folderSector1TwoTenthsOffThePace = "lap_times/sector1_two_tenths_off_pace";
        public static String folderSector2TwoTenthsOffThePace = "lap_times/sector2_two_tenths_off_pace";
        public static String folderSector3TwoTenthsOffThePace = "lap_times/sector3_two_tenths_off_pace";
        public static String folderSector1and2TwoTenthsOffThePace = "lap_times/sector1_and_2_two_tenths_off_pace";
        public static String folderSector2and3TwoTenthsOffThePace = "lap_times/sector2_and_3_two_tenths_off_pace";
        public static String folderSector1and3TwoTenthsOffThePace = "lap_times/sector1_and_3_two_tenths_off_pace";
        public static String folderAllSectorsTwoTenthsOffThePace = "lap_times/sector_all_two_tenths_off_pace";

        public static String folderSector1ASecondOffThePace = "lap_times/sector1_a_second_off_pace";
        public static String folderSector2ASecondOffThePace = "lap_times/sector2_a_second_off_pace";
        public static String folderSector3ASecondOffThePace = "lap_times/sector3_a_second_off_pace";
        public static String folderSector1and2ASecondOffThePace = "lap_times/sector1_and_2_a_second_off_pace";
        public static String folderSector2and3ASecondOffThePace = "lap_times/sector2_and_3_a_second_off_pace";
        public static String folderSector1and3ASecondOffThePace = "lap_times/sector1_and_3_a_second_off_pace";
        public static String folderAllSectorsASecondOffThePace = "lap_times/sector_all_a_second_off_pace";

        public static String folderSector1Is = "lap_times/sector1_is";
        public static String folderSector2Is = "lap_times/sector2_is";
        public static String folderSector3Is = "lap_times/sector3_is";
        public static String folderSectors1And2Are = "lap_times/sector1_and_2_are";
        public static String folderSectors2And3Are = "lap_times/sector2_and_3_are";
        public static String folderSectors1And3Are = "lap_times/sector1_and_3_are";
        public static String folderAllThreeSectorsAre = "lap_times/sector_all_are";
        public static String folderOffThePace = "lap_times/off_the_pace";

        public static String folderSelfSector1ATenthOffThePace = "lap_times/sector1_a_tenth_off_self_pace";
        public static String folderSelfSector2ATenthOffThePace = "lap_times/sector2_a_tenth_off_self_pace";
        public static String folderSelfSector3ATenthOffThePace = "lap_times/sector3_a_tenth_off_self_pace";
        public static String folderSelfSector1and2ATenthOffThePace = "lap_times/sector1_and_2_a_tenth_off_self_pace";
        public static String folderSelfSector2and3ATenthOffThePace = "lap_times/sector2_and_3_a_tenth_off_self_pace";
        public static String folderSelfSector1and3ATenthOffThePace = "lap_times/sector1_and_3_a_tenth_off_self_pace";
        public static String folderSelfAllSectorsATenthOffThePace = "lap_times/sector_all_a_tenth_off_self_pace";

        public static String folderSelfSector1TwoTenthsOffThePace = "lap_times/sector1_two_tenths_off_self_pace";
        public static String folderSelfSector2TwoTenthsOffThePace = "lap_times/sector2_two_tenths_off_self_pace";
        public static String folderSelfSector3TwoTenthsOffThePace = "lap_times/sector3_two_tenths_off_self_pace";
        public static String folderSelfSector1and2TwoTenthsOffThePace = "lap_times/sector1_and_2_two_tenths_off_self_pace";
        public static String folderSelfSector2and3TwoTenthsOffThePace = "lap_times/sector2_and_3_two_tenths_off_self_pace";
        public static String folderSelfSector1and3TwoTenthsOffThePace = "lap_times/sector1_and_3_two_tenths_off_self_pace";
        public static String folderSelfAllSectorsTwoTenthsOffThePace = "lap_times/sector_all_two_tenths_off_self_pace";

        public static String folderSelfSector1ASecondOffThePace = "lap_times/sector1_a_second_off_self_pace";
        public static String folderSelfSector2ASecondOffThePace = "lap_times/sector2_a_second_off_self_pace";
        public static String folderSelfSector3ASecondOffThePace = "lap_times/sector3_a_second_off_self_pace";
        public static String folderSelfSector1and2ASecondOffThePace = "lap_times/sector1_and_2_a_second_off_self_pace";
        public static String folderSelfSector2and3ASecondOffThePace = "lap_times/sector2_and_3_a_second_off_self_pace";
        public static String folderSelfSector1and3ASecondOffThePace = "lap_times/sector1_and_3_a_second_off_self_pace";
        public static String folderSelfAllSectorsASecondOffThePace = "lap_times/sector_all_a_second_off_self_pace";

        public static String folderSelfOffThePace = "lap_times/off_the_self_pace";

        // if the lap is within 0.3% of the best lap time play a message
        private Single goodLapPercent = 0.3f;

        private Single matchingRacePacePercent = 0.1f;

        // if the lap is within 0.5% of the previous lap it's considered consistent
        private Single consistencyLimit = 0.5f;

        private List<float> lapTimesWindow = new List<float>();
        private List<Conditions.ConditionsSample> conditionsWindow = new List<Conditions.ConditionsSample>();

        private int lapTimesWindowSize = 5;

        private ConsistencyResult lastConsistencyMessage;

        // lap number when the last consistency update was made
        private int lastConsistencyUpdate;

        private Boolean lapIsValid;

        private LastLapRating lastLapRating;

        private LastLapRating lastLapSelfRating;

        private TimeSpan deltaPlayerLastToSessionBestInClass;

        private Boolean deltaPlayerLastToSessionBestInClassSet = false;
                
        private int currentPosition;

        private SessionType sessionType;

        private GameStateData currentGameState;

        private int paceCheckLapsWindowForRaceToUse = 3;
        private int paceCheckLapsWindowForRaceVeryShort = 6;
        private int paceCheckLapsWindowForRaceShort = 5;
        private int paceCheckLapsWindowForRaceMedium = 4;
        private int paceCheckLapsWindowForRaceLong = 3;
        private int paceCheckLapsWindowForRaceVeryLong = 2;

        private Boolean isHotLappingOrLonePractice;

        private TimeSpan lastGapToSecondWhenLeadingPracOrQual;

        private int ClassPositionAtStartOfCurrentLap = -1;

        public LapTimes(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            if (frequencyOfRaceSectorDeltaReports > 7)
            {
                maxQueueLengthForRaceSectorDeltaReports = 7;
            } 
            else if (frequencyOfRaceSectorDeltaReports > 5)
            {
                maxQueueLengthForRaceSectorDeltaReports = 4;
            }
            else
            {
                maxQueueLengthForRaceSectorDeltaReports = 3;
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
            conditionsWindow = new List<Conditions.ConditionsSample>();
            lastConsistencyUpdate = 0;
            lastConsistencyMessage = ConsistencyResult.NOT_APPLICABLE;
            lapIsValid = true;
            lastLapRating = LastLapRating.NO_DATA;
            lastLapSelfRating = LastLapRating.NO_DATA;
            deltaPlayerLastToSessionBestInClass = TimeSpan.MaxValue;
            deltaPlayerLastToSessionBestInClassSet = false;
            currentPosition = -1;
            currentGameState = null;
            isHotLappingOrLonePractice = false;
            lastGapToSecondWhenLeadingPracOrQual = TimeSpan.Zero;
            ClassPositionAtStartOfCurrentLap = -1;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<string, object> validationData)
        {
            // not sure if we need this - validate that we're not in sector 2 by the time the lap consistency message is played
            if ((eventSubType == folderImprovingTimes || eventSubType == folderConsistentTimes || eventSubType == folderWorseningTimes) &&
                    currentGameState.SessionData.SectorNumber != 1)
            {
                return false;
            }
            else
            {
                return base.isMessageStillValid(eventSubType, currentGameState, validationData);
            }
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (GameStateData.onManualFormationLap)
            {
                return;
            }
            sessionType = currentGameState.SessionData.SessionType;
            this.currentGameState = currentGameState;
            if (currentGameState.SessionData.IsNewLap)
            {
                ClassPositionAtStartOfCurrentLap = currentGameState.SessionData.ClassPosition;
                if (currentGameState.SessionData.CompletedLaps > 0)
                {
                    if (currentGameState.SessionData.LapTimePrevious > 0.0f)
                    {
                        Console.WriteLine("Laptime: " + TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious).ToString(@"mm\:ss\.fff") + ",  Valid = " + currentGameState.SessionData.PreviousLapWasValid);
                    }
                    else
                    {
                        Console.WriteLine("Laptime: " + currentGameState.SessionData.LapTimePrevious.ToString("0.000") + ",  Valid = " + currentGameState.SessionData.PreviousLapWasValid);
                    }
                }
                if (currentGameState.SessionData.TrackDefinition != null)
                {
                    switch (currentGameState.SessionData.TrackDefinition.trackLengthClass)
                    {
                        case TrackData.TrackLengthClass.VERY_SHORT:
                            paceCheckLapsWindowForRaceToUse = paceCheckLapsWindowForRaceVeryShort;
                            break;
                        case TrackData.TrackLengthClass.SHORT:
                            paceCheckLapsWindowForRaceToUse = paceCheckLapsWindowForRaceShort;
                            break;
                        case TrackData.TrackLengthClass.MEDIUM:
                            paceCheckLapsWindowForRaceToUse = paceCheckLapsWindowForRaceMedium;
                            break;
                        case TrackData.TrackLengthClass.LONG:
                            paceCheckLapsWindowForRaceToUse = paceCheckLapsWindowForRaceLong;
                            break;
                        case TrackData.TrackLengthClass.VERY_LONG:
                            paceCheckLapsWindowForRaceToUse = paceCheckLapsWindowForRaceVeryLong;
                            break;
                    }
                }
                deltaPlayerLastToSessionBestInClassSet = false;
                if (currentGameState.SessionData.LapTimePrevious > 0)
                {
                    if (currentGameState.OpponentData.Count > 0
                        && currentGameState.SessionData.SessionType != SessionType.LonePractice && currentGameState.SessionData.SessionType != SessionType.HotLap)
                    {
                        if (currentGameState.SessionData.SessionType == SessionType.Qualify)
                        {
                            // always want the overall delta in qually
                            float opponentOverallBest = currentGameState.TimingData.getPlayerClassOpponentBestLapTime(TimingData.ConditionsEnum.ANY);
                            if (opponentOverallBest > 0)
                            {
                                deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious - opponentOverallBest);
                                deltaPlayerLastToSessionBestInClassSet = true;
                            }
                        }
                        else
                        {
                            // get the delta for the current conditions
                            float opponentBestInCurrentConditions = currentGameState.TimingData.getPlayerClassOpponentBestLapTime(TimingData.ConditionsEnum.CURRENT);
                            if (opponentBestInCurrentConditions > 0)
                            {
                                deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious - opponentBestInCurrentConditions);
                                deltaPlayerLastToSessionBestInClassSet = true;
                            }
                        }
                    }
                    else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0 && currentGameState.SessionData.CompletedLaps > 1)
                    {
                        deltaPlayerLastToSessionBestInClass = TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious - currentGameState.SessionData.PlayerLapTimeSessionBest);
                        deltaPlayerLastToSessionBestInClassSet = true;
                    }
                }
            }
            currentPosition = currentGameState.SessionData.ClassPosition;

            // check the current lap is still valid
            if (lapIsValid && currentGameState.SessionData.CompletedLaps > 0 &&
                !currentGameState.SessionData.IsNewLap && !currentGameState.SessionData.CurrentLapIsValid)
            {
                lapIsValid = false;
            }
            if (previousGameState != null && previousGameState.SessionData.CompletedLaps <= currentGameState.FlagData.lapCountWhenLastWentGreen)
            {
                return;
            }
            float[] lapAndSectorsComparisonData = new float[] { -1, -1, -1, -1 };
            float[] lapAndSectorsSelfComparisonData = new float[] { -1, -1, -1, -1 };
            if (currentGameState.SessionData.IsNewLap)
            {
                // If this is a new lap, then the just completed lap became last lap.  We do not want to use it as a 
                // Qualification/Practice and self pace comparison, we need the previous player best time.
                lapAndSectorsSelfComparisonData = currentGameState.SessionData.getPlayerTimeAndSectorsForBestLap(true /*ignoreLast*/);
            }
            else
            {
                lapAndSectorsSelfComparisonData = currentGameState.SessionData.getPlayerTimeAndSectorsForBestLap(false /*ignoreLast*/);
            }

            if (currentGameState.SessionData.IsNewSector)
            {
                isHotLappingOrLonePractice = currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.SessionData.SessionType == SessionType.LonePractice ||
                    (currentGameState.OpponentData.Count == 0 || (currentGameState.OpponentData.Count == 1 && currentGameState.OpponentData.First().Value.DriverRawName == currentGameState.SessionData.DriverRawName));
                if (isHotLappingOrLonePractice)
                {
                    // note that lone practice in changing conditions doesn't take conditions into account. This is a bit of an edge case
                    lapAndSectorsComparisonData[0] = lapAndSectorsSelfComparisonData[0];
                    lapAndSectorsComparisonData[1] = lapAndSectorsSelfComparisonData[1];
                    lapAndSectorsComparisonData[2] = lapAndSectorsSelfComparisonData[2];
                    lapAndSectorsComparisonData[3] = lapAndSectorsSelfComparisonData[3];
                }
                else
                {
                    // in qual sessions we want absolute timings. We can also use absolute timings if the conditions are static.
                    // If the conditions are changing we want timings relative to the prevailing conditions for non-qual sessions.
                    // For race sessions we want the recent pace
                    if (currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        if (!currentGameState.TimingData.conditionsHaveChanged)
                        {
                            // no changing conditions, get the 'pace' from the most recent laps
                            lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRaceToUse, currentGameState.carClass);
                        }
                        else
                        {
                            // use data relevant to current conditions
                            lapAndSectorsComparisonData = new float[] { 
                                currentGameState.TimingData.getPlayerClassOpponentBestLapTime(),
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector1Time(),
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector2Time(),
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector3Time()
                            };
                        }
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Practice)
                    {
                        if (!currentGameState.TimingData.conditionsHaveChanged)
                        {
                            // no changing conditions, get the 'pace' from the all the recorded laps
                            lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass);
                        }
                        else
                        {
                            // use data relevant to current conditions
                            lapAndSectorsComparisonData = new float[] { 
                                currentGameState.TimingData.getPlayerClassOpponentBestLapTime(),
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector1Time(),
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector2Time(),
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector3Time()
                            };
                        }
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Qualify)
                    {
                        // not interested in the conditions, just want best laps from all the data we have
                        lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass);
                    }
                }
            }

            if (!currentGameState.PitData.OnInLap && previousGameState != null && !previousGameState.PitData.OnOutLap 
                && !currentGameState.PitData.InPitlane   // as this is a new lap, check whether the *previous* state was an outlap
                && !currentGameState.FlagData.previousLapWasFCY)    // don't announce lap times if we've just gone green after FCY
            {
                Boolean sectorsReportedForLap = false;                
                if (currentGameState.SessionData.IsNewLap && 
                    (((currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.SessionData.SessionType == SessionType.LonePractice
                        || currentGameState.SessionData.SessionType == SessionType.Qualify) 
                            && currentGameState.SessionData.CompletedLaps > 0) ||
                      currentGameState.SessionData.CompletedLaps > 1))
                {
                    if (lapTimesWindow == null)
                    {
                        lapTimesWindow = new List<float>(lapTimesWindowSize);
                    }
                    lastLapRating = getLastLapRating(currentGameState, lapAndSectorsComparisonData, false /*selfPace*/);
                    lastLapSelfRating = getLastLapRating(currentGameState, lapAndSectorsSelfComparisonData, true /*selfPace*/);

                    if (currentGameState.SessionData.PreviousLapWasValid)
                    {
                        lapTimesWindow.Insert(0, currentGameState.SessionData.LapTimePrevious);
                        Conditions.ConditionsSample conditionsSample = currentGameState.Conditions.getMostRecentConditions();
                        if (conditionsSample != null)
                        {
                            conditionsWindow.Insert(0, conditionsSample);
                        }
                        if (lapIsValid && !currentGameState.PitData.InPitlane)
                        {
                            Boolean playedLapTime = false;
                            if (isHotLappingOrLonePractice && reportAllLaptimesInHotlapMode)
                            {
                                // If requested, always play the laptime in hotlap/lone practice mode
                                audioPlayer.playMessage(new QueuedMessage("laptime",
                                        MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LapTimePrevious, Precision.AUTO_LAPTIMES)), 0, this), 10);
                                playedLapTime = true;
                            }
                            else if (((currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice) && frequencyOfPlayerQualAndPracLapTimeReports > Utilities.random.NextDouble() * 10)
                                || (currentGameState.SessionData.SessionType == SessionType.Race && frequencyOfPlayerRaceLapTimeReports > Utilities.random.NextDouble() * 10))
                            {
                                // usually play it in practice / qual mode, occasionally play it in race mode
                                QueuedMessage gapFillerLapTime = new QueuedMessage("laptime",
                                    MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LapTimePrevious, Precision.AUTO_LAPTIMES)), 0, this);
                                if (currentGameState.SessionData.SessionType == SessionType.Race)
                                {
                                    gapFillerLapTime.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceLapTimeReports;
                                }
                                audioPlayer.playMessage(gapFillerLapTime, 0);
                                playedLapTime = true;
                            }

                            if (deltaPlayerLastToSessionBestInClassSet &&
                                (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice ||
                                currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.SessionData.SessionType == SessionType.LonePractice))
                            {
                                if (currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.SessionData.SessionType == SessionType.LonePractice ||
                                    currentGameState.OpponentData.Count == 0)
                                {
                                    if (currentGameState.SessionData.CompletedLaps > 1 && 
                                        (isHotLappingOrLonePractice ? lastLapRating == LastLapRating.BEST_OVERALL : lastLapRating == LastLapRating.BEST_IN_CLASS 
                                            || deltaPlayerLastToSessionBestInClass <= TimeSpan.Zero))
                                    {
                                        audioPlayer.playMessage(new QueuedMessage(folderPersonalBest, 0, this), 3);
                                    }
                                    else if (deltaPlayerLastToSessionBestInClass > TimeSpan.Zero  // Guard against first lap time set.
                                        && deltaPlayerLastToSessionBestInClass < TimeSpan.FromMilliseconds(50))
                                    {
                                        audioPlayer.playMessage(new QueuedMessage((isHotLappingOrLonePractice ? folderSelfLessThanATenthOffThePace : folderLessThanATenthOffThePace), 0, this), 3);
                                    }
                                    else if (deltaPlayerLastToSessionBestInClass > TimeSpan.Zero  // Guard against first lap time set.
                                        && deltaPlayerLastToSessionBestInClass < TimeSpan.MaxValue)
                                    {
                                        audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, new TimeSpanWrapper(deltaPlayerLastToSessionBestInClass, Precision.AUTO_GAPS),
                                            isHotLappingOrLonePractice ? folderSelfGapOutroOffPace : folderGapOutroOffPace), 0, this), 3);
                                    }
                                    if (!GlobalBehaviourSettings.useOvalLogic &&
                                        practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > Utilities.random.NextDouble() * 10)
                                    {
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(SectorReportOption.ALL, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], true, isHotLappingOrLonePractice /*selfPace*/);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage("sectorsHotLap", sectorMessageFragments, 0, this), 3);
                                            sectorsReportedForLap = true;
                                        }
                                    }
                                }
                                // need to be careful with the rating here as it's based on the known opponent laps, and we may have joined the session part way through
                                else if (currentGameState.SessionData.ClassPosition == 1) 
                                {
                                    Boolean newGapToSecond = false;
                                    if (previousGameState != null && previousGameState.SessionData.ClassPosition > 1)
                                    {
                                        newGapToSecond = true;
                                        if (currentGameState.SessionData.SessionType == SessionType.Qualify)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage(Position.folderPole, 0, this), 10);
                                        }
                                        else if (currentGameState.SessionData.SessionType == SessionType.Practice)
                                        {
                                            if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                                            {
                                                audioPlayer.playMessage(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro, Position.folderStub + 1), 0, this), 5);
                                            }
                                            else
                                            {
                                                audioPlayer.playMessage(new QueuedMessage(Position.folderStub + 1, 0, this), 5);
                                            }
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
                                        if (frequencyOfPlayerQualAndPracLapTimeReports > Utilities.random.NextDouble() * 10 &&
                                            (!disablePCarspracAndQualPoleDeltaReports || 
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.raceRoom.gameEnum ||
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.iracing.gameEnum ||
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.assetto32Bit.gameEnum ||
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.assetto64Bit.gameEnum) &&
                                            (gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50))
                                        {
                                            // delay this a bit...
                                            audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                                MessageContents(folderGapIntro, new TimeSpanWrapper(gapBehind, Precision.AUTO_GAPS), folderQuickerThanSecondPlace), Utilities.random.Next(0, 8), this), 5);
                                        }
                                    }
                                }
                                else
                                {
                                    if (currentGameState.SessionData.CompletedLaps > 1 && 
                                        (lastLapRating == LastLapRating.PERSONAL_BEST_STILL_SLOW || lastLapRating == LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER ||
                                         lastLapRating == LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER))
                                    {
                                        audioPlayer.playMessage(new QueuedMessage(folderPersonalBest, 0, this), 7);
                                    }
                                    // don't read this message if the rounded time gap is 0.0 seconds or it's more than 59 seconds
                                    // only play qual / prac deltas for Raceroom as the PCars data is inaccurate for sessions joined part way through
                                    if (frequencyOfPlayerQualAndPracLapTimeReports > Utilities.random.NextDouble() * 10 &&
                                        (!disablePCarspracAndQualPoleDeltaReports || CrewChief.gameDefinition.gameEnum == GameDefinition.raceRoom.gameEnum || 
                                        CrewChief.gameDefinition.gameEnum == GameDefinition.iracing.gameEnum ||
                                        CrewChief.gameDefinition.gameEnum == GameDefinition.assetto32Bit.gameEnum ||
                                        CrewChief.gameDefinition.gameEnum == GameDefinition.assetto64Bit.gameEnum) &&
                                        (deltaPlayerLastToSessionBestInClass.Seconds > 0 || deltaPlayerLastToSessionBestInClass.Milliseconds > 50) &&
                                        deltaPlayerLastToSessionBestInClass.Seconds < 60)
                                    {
                                        // delay this a bit...
                                        audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, new TimeSpanWrapper(deltaPlayerLastToSessionBestInClass, Precision.AUTO_GAPS), folderGapOutroOffPace), Utilities.random.Next(0, 8), this), 5);
                                    }
                                    if (!GlobalBehaviourSettings.useOvalLogic && 
                                        practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > Utilities.random.NextDouble() * 10)
                                    {
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(SectorReportOption.ALL, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], true, false /*selfPace*/);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage("sectorDeltas", sectorMessageFragments, 0, this), 5);
                                            sectorsReportedForLap = true;
                                        }
                                    }
                                }
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Race && !currentGameState.PitData.InPitlane)
                            {
                                Boolean playedLapMessage = false;
                                if (frequencyOfPlayerRaceLapTimeReports > Utilities.random.NextDouble() * 10)
                                {
                                    float pearlLikelihood = 0.8f;
                                    switch (lastLapRating)
                                    {
                                        case LastLapRating.BEST_OVERALL:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderBestLapInRace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood, 3);
                                            break;
                                        case LastLapRating.BEST_IN_CLASS:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderBestLapInRaceForClass, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood, 3);
                                            break;
                                        case LastLapRating.SETTING_CURRENT_PACE:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderSettingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood, 3);
                                            break;
                                        case LastLapRating.CLOSE_TO_CURRENT_PACE:
                                            // don't keep playing this one
                                            if (Utilities.random.NextDouble() < 0.5)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.playMessage(new QueuedMessage(folderMatchingCurrentRacePace, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood, 0);
                                            }
                                            break;
                                        case LastLapRating.PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER:
                                        case LastLapRating.PERSONAL_BEST_CLOSE_TO_CLASS_LEADER:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.GOOD, pearlLikelihood, 0);
                                            break;
                                        case LastLapRating.PERSONAL_BEST_STILL_SLOW:
                                            playedLapMessage = true;
                                            audioPlayer.playMessage(new QueuedMessage(folderPersonalBest, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood, 0);
                                            break;
                                        case LastLapRating.CLOSE_TO_OVERALL_LEADER:
                                        case LastLapRating.CLOSE_TO_CLASS_LEADER:
                                            // this is an OK lap but not a PB. We only want to say "decent lap" occasionally here
                                            if (Utilities.random.NextDouble() < 0.2)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.playMessage(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood, 0);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                if (!GlobalBehaviourSettings.useOvalLogic && currentGameState.SessionData.ClassPosition == ClassPositionAtStartOfCurrentLap &&
                                    raceSectorReportsAtLapEnd && frequencyOfRaceSectorDeltaReports > Utilities.random.NextDouble() * 10)
                                {
                                    double r = Utilities.random.NextDouble();
                                    SectorReportOption reportOption = SectorReportOption.ALL;
                                    if (playedLapTime && playedLapMessage)
                                    {
                                        // if we've already played a laptime and lap rating, use the short sector message.
                                        reportOption = SectorReportOption.WORST_ONLY;
                                    }
                                    else if (r > 0.5 || ((playedLapTime || playedLapMessage) && r > 0.2))
                                    {
                                        // if we've played one of these, usually use the abbrieviated version. If we've played neither, sometimes use the abbrieviated version
                                        reportOption = SectorReportOption.WORST_ONLY;
                                    }
                                    
                                    List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], false, false /*selfPace*/);
                                    if (sectorMessageFragments.Count > 0)
                                    {
                                        QueuedMessage message = new QueuedMessage("sectorDeltas", sectorMessageFragments, 0, this);
                                        message.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceSectorDeltaReports;
                                        audioPlayer.playMessage(message, 0);
                                        sectorsReportedForLap = true;
                                    }
                                }

                                // play the consistency message if we've not played the good lap message, or sometimes
                                // play them both
                                Boolean playConsistencyMessage = !playedLapMessage || Utilities.random.NextDouble() < 0.25;
                                if (playConsistencyMessage && currentGameState.SessionData.CompletedLaps >= lastConsistencyUpdate + lapTimesWindowSize &&
                                    lapTimesWindow.Count >= lapTimesWindowSize)
                                {
                                    ConsistencyResult consistency = checkAgainstPreviousLaps();
                                    if (consistency == ConsistencyResult.CONSISTENT)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.playMessage(new QueuedMessage(folderConsistentTimes, Utilities.random.Next(0, 8), this), 0);
                                    }
                                    else if (consistency == ConsistencyResult.IMPROVING)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.playMessage(new QueuedMessage(folderImprovingTimes, Utilities.random.Next(0, 8), this), 5);
                                    }
                                    else if (consistency == ConsistencyResult.WORSENING)
                                    {
                                        // don't play the worsening message if the lap rating is good
                                        if (lastLapRating == LastLapRating.BEST_IN_CLASS || lastLapRating == LastLapRating.BEST_OVERALL ||
                                            lastLapRating == LastLapRating.SETTING_CURRENT_PACE || lastLapRating == LastLapRating.CLOSE_TO_CURRENT_PACE)
                                        {
                                            Console.WriteLine("Skipping 'worsening' laptimes message - inconsistent with lap rating");
                                        }
                                        else if (currentGameState.SessionData.ClassPosition >= currentGameState.SessionData.ClassPositionAtStartOfCurrentLap)
                                        {
                                            // only complain about worsening laptimes if we've not overtaken anyone on this lap
                                            lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;

                                            audioPlayer.playMessage(new QueuedMessage(folderWorseningTimes, Utilities.random.Next(0, 8), this, new Dictionary<String, Object>()), 3);
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                // report sector delta at the completion of a sector?
                if (!sectorsReportedForLap && currentGameState.SessionData.IsNewSector && 
                    ((currentGameState.SessionData.SessionType == SessionType.Race && raceSectorReportsAtEachSector) ||
                     (currentGameState.SessionData.SessionType != SessionType.Race && practiceAndQualSectorReportsAtEachSector)))
                {
                    double r = Utilities.random.NextDouble() * 10;
                    Boolean canPlayForRace = frequencyOfRaceSectorDeltaReports > r;
                    Boolean canPlayForPracAndQual = frequencyOfPracticeAndQualSectorDeltaReports > r;

                    // only report sector time if this is a valid lap
                    Boolean sectorWasOnValidLap;
                    if (currentGameState.SessionData.IsNewLap)
                    {
                        sectorWasOnValidLap = currentGameState.SessionData.PreviousLapWasValid;
                    }
                    else
                    {
                        sectorWasOnValidLap = currentGameState.SessionData.CurrentLapIsValid;
                    }
                    
                    if (sectorWasOnValidLap &&
                        ((currentGameState.SessionData.SessionType == SessionType.Race && canPlayForRace) ||
                        (((currentGameState.SessionData.SessionType == SessionType.Practice && (currentGameState.OpponentData.Count > 0 || currentGameState.SessionData.CompletedLaps > 1))
                        || currentGameState.SessionData.SessionType == SessionType.Qualify ||
                        ((currentGameState.SessionData.SessionType == SessionType.HotLap || currentGameState.SessionData.SessionType == SessionType.LonePractice)
                            && currentGameState.SessionData.CompletedLaps > 1)) && canPlayForPracAndQual)))
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
                        List<MessageFragment> messageFragments = getSingleSectorDeltaMessages(sectorEnum, playerSector, comparisonSector, isHotLappingOrLonePractice /*selfPace*/);
                        if (!GlobalBehaviourSettings.useOvalLogic && 
                            messageFragments.Count() > 0)
                        {
                            audioPlayer.playMessage(new QueuedMessage("singleSectorDelta", messageFragments, Utilities.random.Next(2, 4), this), 5);
                        }
                    }
                }
            }
            if (currentGameState.SessionData.IsNewLap && !currentGameState.PitData.OnOutLap)
            {
                // lapIsValid has mixed use.  It is used to track if current lap is valid, but is also used
                // to decide if previous lap was valid when the new lap begins.  So reset it here.
                lapIsValid = true;
            }
        }

        private ConsistencyResult checkAgainstPreviousLaps()
        {
            if (conditionsWindow.Count() >= lapTimesWindowSize && ConditionsHaveChanged(conditionsWindow[0], conditionsWindow[lapTimesWindowSize - 1]))
            {
                return ConsistencyResult.NOT_APPLICABLE;
            }

            Boolean isImproving = true;
            Boolean isWorsening = true;
            Boolean isConsistent = true;

            for (int index = 0; index < lapTimesWindowSize - 1; index++)
            {
                // check the lap time was recorded
                if (lapTimesWindow[index] <= 0)
                {
                    Console.WriteLine("No data for consistency check");
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

        private LastLapRating getLastLapRating(GameStateData currentGameState, float[] bestLapComparisonData, Boolean selfPace)
        {
            // if we've only completed a couple of laps, make this 'no data'
            if (currentGameState.SessionData.CompletedLaps < 3)
            {
                return LastLapRating.NO_DATA;
            }
            if (currentGameState.SessionData.PreviousLapWasValid && currentGameState.SessionData.LapTimePrevious > 0)
            {
                float closeThreshold = currentGameState.SessionData.LapTimePrevious * goodLapPercent / 100;
                float matchingRacePaceThreshold = currentGameState.SessionData.LapTimePrevious * matchingRacePacePercent / 100;

                // no point in reporting lap awesomeness if we have no comparison data:
                Boolean hasPlayerLapComparisonData = currentGameState.SessionData.CompletedLaps > 1
                    && currentGameState.SessionData.LapTimePrevious > 0
                    && currentGameState.SessionData.PreviousLapWasValid;

                Boolean sessionHasOpponents = currentGameState.SessionData.SessionType != SessionType.HotLap
					&& currentGameState.SessionData.SessionType != SessionType.LonePractice
					&& currentGameState.OpponentData.Count > 0;
                Boolean hasComparisonData = (sessionHasOpponents || selfPace) && bestLapComparisonData[0] > 0;

                if (!hasPlayerLapComparisonData && !hasComparisonData)
                {
                    return LastLapRating.NO_DATA;
                }

                if (!selfPace)
                {
                    if (currentGameState.SessionData.OverallSessionBestLapTime == currentGameState.SessionData.LapTimePrevious)
                    {
                        return LastLapRating.BEST_OVERALL;
                    }
                    else if (GameStateData.Multiclass && currentGameState.SessionData.PlayerClassSessionBestLapTime == currentGameState.SessionData.LapTimePrevious)
                    {
                        return LastLapRating.BEST_IN_CLASS;
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Race && bestLapComparisonData[0] > 0 && bestLapComparisonData[0] >= currentGameState.SessionData.LapTimePrevious)
                    {
                        return LastLapRating.SETTING_CURRENT_PACE;
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Race && bestLapComparisonData[0] > 0 && bestLapComparisonData[0] > currentGameState.SessionData.LapTimePrevious - closeThreshold)
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
                    else if (currentGameState.SessionData.PlayerLapTimeSessionBest >= currentGameState.SessionData.LapTimePrevious - closeThreshold
                        && currentGameState.SessionData.CompletedLaps > 1)
                    {
                        return LastLapRating.CLOSE_TO_PERSONAL_BEST;
                    }
                    else if (bestLapComparisonData[0] > 0 && bestLapComparisonData[0] < currentGameState.SessionData.LapTimePrevious - 3)
                    {
                        // 3 seconds off the pace
                        return LastLapRating.BAD;
                    }
                    else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0)
                    {
                        return LastLapRating.MEH;
                    }
                }
                else
                {
                    if (bestLapComparisonData[0] > 0 && currentGameState.SessionData.LapTimePrevious == bestLapComparisonData[0])
                    {
                        return LastLapRating.PERSONAL_BEST;
                    }
                    else if (bestLapComparisonData[0] > 0 && bestLapComparisonData[0] >= currentGameState.SessionData.LapTimePrevious - closeThreshold
                        && currentGameState.SessionData.CompletedLaps > 1)
                    {
                        return LastLapRating.CLOSE_TO_PERSONAL_BEST;
                    }
                    else if (bestLapComparisonData[0] > 0 && bestLapComparisonData[0] < currentGameState.SessionData.LapTimePrevious - 3)
                    {
                        // 3 seconds off the pace
                        return LastLapRating.BAD;
                    }
                    else if (currentGameState.SessionData.PlayerLapTimeSessionBest > 0)
                    {
                        return LastLapRating.MEH;
                    }
                }
            }
            return LastLapRating.NO_DATA;
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHAT_ARE_MY_SECTOR_TIMES))
            {
                if (currentGameState != null && 
                    currentGameState.SessionData.LastSector1Time > -1 && currentGameState.SessionData.LastSector2Time > -1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector1Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector1Time, Precision.AUTO_LAPTIMES)), 0, null));
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector2Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector2Time, Precision.AUTO_LAPTIMES)), 0, null));
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector3Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector3Time, Precision.AUTO_LAPTIMES)), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_LAST_SECTOR_TIME))
            {
                if (currentGameState != null && currentGameState.SessionData.SectorNumber == 1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector3Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector3Time, Precision.AUTO_LAPTIMES)), 0, null));
                }
                else if (currentGameState != null && currentGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.LastSector1Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector1Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector1Time, Precision.AUTO_LAPTIMES)), 0, null));
                }
                else if (currentGameState != null && currentGameState.SessionData.SectorNumber == 3 && currentGameState.SessionData.LastSector2Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector2Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector2Time, Precision.AUTO_LAPTIMES)), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_BEST_LAP_TIME))
            {
                Boolean gotData = false;
                if (CrewChief.currentGameState != null)
                {
                    float bestLap = CrewChief.currentGameState.TimingData.getPlayerBestLapTime(TimingData.ConditionsEnum.ANY);
                    if (bestLap > 0)
                    {
                        gotData = true;
                        audioPlayer.playMessageImmediately(new QueuedMessage("bestLapTime",
                            MessageContents(TimeSpanWrapper.FromSeconds(bestLap, Precision.AUTO_LAPTIMES)), 0, this));
                    }
                }
                if (!gotData)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_THE_FASTEST_LAP_TIME))
            {
                if (currentGameState.SessionData.PlayerClassSessionBestLapTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sessionFastestLaptime",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.PlayerClassSessionBestLapTime, Precision.AUTO_LAPTIMES)), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHAT_WAS_MY_LAST_LAP_TIME))
            {
                if (CrewChief.currentGameState != null && CrewChief.currentGameState.SessionData.LapTimePrevious > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("laptime",
                        MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(
                        CrewChief.currentGameState.SessionData.LapTimePrevious, Precision.AUTO_LAPTIMES)), 0, null));
                    
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_PACE))
            {
                reportPace(false /*selfPace*/);
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_SELF_PACE))
            {
                reportPace(true /*selfPace*/);
            }
        }

        public void reportPace(bool selfPace)
        {
            if (sessionType == SessionType.Race)
            {
                if (currentGameState == null)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                else
                {
                    float[] bestComparisonLapData;
                    if (selfPace)
                    {
                        bestComparisonLapData = currentGameState.SessionData.getPlayerTimeAndSectorsForBestLap(false /*ignoreLast*/);  // Currently, use sectors of the best valid lap for self pace comparison.
                                                                                                                // Later down the road, we might want use best sector times out of all the valid laps,
                                                                                                                // or report them as a response to some separate voice command.
                    }
                    else if (currentGameState.TimingData.conditionsHaveChanged)
                    {
                        bestComparisonLapData = new float[] {
                            currentGameState.TimingData.getPlayerClassOpponentBestLapTime(),
                            currentGameState.TimingData.getPlayerClassOpponentBestLapSector1Time(),
                            currentGameState.TimingData.getPlayerClassOpponentBestLapSector2Time(),
                            currentGameState.TimingData.getPlayerClassOpponentBestLapSector3Time()
                        };
                    }
                    else
                    {
                        bestComparisonLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRaceToUse, currentGameState.carClass);
                    }
                    if (bestComparisonLapData[0] > -1 && lastLapRating != LastLapRating.NO_DATA)
                    {
                        TimeSpan lapToCompare = TimeSpan.FromSeconds(currentGameState.SessionData.LapTimePrevious - bestComparisonLapData[0]);
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
                        if (!selfPace)
                        {
                            switch (lastLapRating)
                            {
                                case LastLapRating.BEST_OVERALL:
                                case LastLapRating.BEST_IN_CLASS:
                                case LastLapRating.SETTING_CURRENT_PACE:
                                    audioPlayer.playMessageImmediately(new QueuedMessage(folderSettingCurrentRacePace, 0, null));
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
                                        audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    }
                                    break;
                                case LastLapRating.MEH:
                                    if (timeToFindFolder != null)
                                    {
                                        messages.Add(MessageFragment.Text(timeToFindFolder));
                                    }
                                    audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    break;
                                case LastLapRating.BAD:
                                    messages.Add(MessageFragment.Text(folderPaceBad));
                                    if (timeToFindFolder != null)
                                    {
                                        messages.Add(MessageFragment.Text(timeToFindFolder));
                                    }
                                    audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    break;
                                default:
                                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                                    break;
                            }
                        }
                        else
                        {
                            // Fors self pace case, announce last lap time.
                            if (currentGameState.SessionData.LapTimePrevious > 0)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("laptime",
                                    MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(
                                    currentGameState.SessionData.LapTimePrevious, Precision.AUTO_LAPTIMES)), 0, null));
                            }

                            switch (lastLapSelfRating)
                            {
                                case LastLapRating.PERSONAL_BEST:
                                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPersonalBest, 0, null));
                                    break;
                                case LastLapRating.CLOSE_TO_PERSONAL_BEST:
                                    if (timeToFindFolder == null || timeToFindFolder != folderNeedToFindMoreThanASecond)
                                    {
                                        messages.Add(MessageFragment.Text(folderPaceOK));
                                    }
                                    if (timeToFindFolder != null)
                                    {
                                        messages.Add(MessageFragment.Text(timeToFindFolder));
                                    }
                                    if (messages.Count > 0)
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    }
                                    break;
                                case LastLapRating.BAD:
                                    messages.Add(MessageFragment.Text(folderPaceBad));
                                    if (timeToFindFolder != null)
                                    {
                                        messages.Add(MessageFragment.Text(timeToFindFolder));
                                    }
                                    audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    break;
                                case LastLapRating.MEH:
                                    if (timeToFindFolder != null)
                                    {
                                        messages.Add(MessageFragment.Text(timeToFindFolder));
                                    }
                                    audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    break;
                                default:
                                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                                    break;
                            }
                        }
                        SectorReportOption reportOption = SectorReportOption.ALL;
                        double r = Utilities.random.NextDouble();
                        // usually report the combined sectors
                        if (r > 0.33)
                        {
                            reportOption = SectorReportOption.WORST_ONLY;
                        }
                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, bestComparisonLapData[1],
                            currentGameState.SessionData.LastSector2Time, bestComparisonLapData[2], currentGameState.SessionData.LastSector3Time, bestComparisonLapData[3], false, selfPace);
                        if (sectorDeltaMessages.Count > 0)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("sectorDeltas", sectorDeltaMessages, 0, null));
                        }
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    }
                }
            }
            else
            {
                if (deltaPlayerLastToSessionBestInClassSet)
                {
                    if (!selfPace)
                    {
                        if (deltaPlayerLastToSessionBestInClass <= TimeSpan.Zero)
                        {
                            if (sessionType == SessionType.Qualify && currentPosition == 1)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderPole, 0, null));
                            }
                            else
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage(folderQuickestOverall, 0, null));
                            }
                            TimeSpan gapBehind = deltaPlayerLastToSessionBestInClass.Negate();
                            if (gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50)
                            {
                                // delay this a bit...
                                audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeNotRaceGap",
                                    MessageContents(folderGapIntro, new TimeSpanWrapper(gapBehind, Precision.AUTO_GAPS), folderQuickerThanSecondPlace), 0, this));
                            }
                        }
                        else if (deltaPlayerLastToSessionBestInClass.Seconds == 0 && deltaPlayerLastToSessionBestInClass.Milliseconds < 50)
                        {
                            if (currentPosition > 1)
                            {
                                // should always trigger
                                if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                                {
                                    audioPlayer.playMessageImmediately(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro, Position.folderStub + currentPosition), 0, null));
                                }
                                else
                                {
                                    audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderStub + currentPosition, 0, null));
                                }
                            }
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderLessThanATenthOffThePace, 0, null));
                        }
                        else
                        {
                            if (currentPosition > 1)
                            {
                                // should always trigger
                                if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                                {
                                    audioPlayer.playMessageImmediately(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro, Position.folderStub + currentPosition), 0, null));
                                }
                                else
                                {
                                    audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderStub + currentPosition, 0, null));
                                }
                            }
                            audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeNotRaceGap",
                                MessageContents(new TimeSpanWrapper(deltaPlayerLastToSessionBestInClass, Precision.AUTO_GAPS), folderGapOutroOffPace), 0, null));
                        }
                    }
                    else
                    {
                        // Fors self pace case, announce last lap time.
                        if (currentGameState.SessionData.LapTimePrevious > 0)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("laptime",
                                MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(
                                currentGameState.SessionData.LapTimePrevious, Precision.AUTO_LAPTIMES)), 0, null));

                            // We also neeed to announce how good it is.
                            List<MessageFragment> messages = new List<MessageFragment>();
                            switch (lastLapSelfRating)
                            {
                                case LastLapRating.PERSONAL_BEST:
                                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPersonalBest, 0, null));
                                    break;
                                case LastLapRating.CLOSE_TO_PERSONAL_BEST:
                                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPaceOK, 0, null));
                                    break;
                                case LastLapRating.MEH:
                                case LastLapRating.BAD:
                                    messages.Add(MessageFragment.Text(folderPaceBad));
                                    audioPlayer.playMessageImmediately(new QueuedMessage("lapTimeRacePaceReport", messages, 0, null));
                                    break;
                                default:
                                    break;
                            }
                        }
                    }

                    // TODO: wrap this in a try-catch until I work out why the array indices are being screwed up in online races (yuk...)
                    try
                    {
                        float[] bestComparisonLapData = selfPace
                            ? currentGameState.SessionData.getPlayerTimeAndSectorsForBestLap(false /*ignoreLast*/)  // Currently, use sectors of the best valid lap for self pace comparison.
                            // Later down the road, we might want use best sector times out of all the valid laps,
                            // or report them as a response to some separate voice command.
                            : new float[] {
                                currentGameState.TimingData.getPlayerClassOpponentBestLapTime(), 
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector1Time(), 
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector2Time(), 
                                currentGameState.TimingData.getPlayerClassOpponentBestLapSector3Time(), 
                            };

                        List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(SectorReportOption.ALL, currentGameState.SessionData.LastSector1Time, bestComparisonLapData[1],
                            currentGameState.SessionData.LastSector2Time, bestComparisonLapData[2], currentGameState.SessionData.LastSector3Time, bestComparisonLapData[3], true, selfPace);
                        if (sectorDeltaMessages.Count > 0)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("sectorDeltas", sectorDeltaMessages, 0, null));
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to get sector deltas: " + e.Message);
                    }
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
        }

        private enum LastLapRating
        {
            BEST_OVERALL, BEST_IN_CLASS, SETTING_CURRENT_PACE, CLOSE_TO_CURRENT_PACE, PERSONAL_BEST, PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER,
            PERSONAL_BEST_CLOSE_TO_CLASS_LEADER, PERSONAL_BEST_STILL_SLOW, CLOSE_TO_OVERALL_LEADER, CLOSE_TO_CLASS_LEADER,
            CLOSE_TO_PERSONAL_BEST, MEH, BAD, NO_DATA
        }

        public enum SectorSet
        {
            ONE, TWO, THREE, NONE
        }

        public static List<MessageFragment> getSingleSectorDeltaMessages(SectorSet sector, float playerTime, float comparisonTime, bool selfPace)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            // the sector times must be > 5 seconds to be considered valid
            if (playerTime > 5 && comparisonTime > 5)
            {
                float delta = getAutoRoundedDelta(playerTime, comparisonTime);
                if (delta < 0.05)
                {
                    if (sector == SectorSet.ONE) {
                        messages.Add(MessageFragment.Text(folderSector1Fast));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(folderSector2Fast));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(folderSector3Fast));
                    }
                }
                else if (nearlyEqual(delta, 0.1f))
                {
                    if (sector == SectorSet.ONE)
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector1ATenthOffThePace : folderSector1ATenthOffThePace));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector2ATenthOffThePace : folderSector2ATenthOffThePace));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector3ATenthOffThePace : folderSector3ATenthOffThePace));
                    }
                }
                else if (nearlyEqual(delta, 0.2f))
                {
                    if (sector == SectorSet.ONE)
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector1TwoTenthsOffThePace : folderSector1TwoTenthsOffThePace));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector2TwoTenthsOffThePace : folderSector2TwoTenthsOffThePace));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector3TwoTenthsOffThePace : folderSector3TwoTenthsOffThePace));
                    }
                }
                else if (nearlyEqual(delta, 1))
                {
                    if (sector == SectorSet.ONE)
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector1ASecondOffThePace : folderSector1ASecondOffThePace));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector2ASecondOffThePace : folderSector2ASecondOffThePace));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(selfPace ? folderSelfSector3ASecondOffThePace : folderSector3ASecondOffThePace));
                    }
                }
                else
                {
                    if (sector == SectorSet.ONE)
                    {
                        messages.Add(MessageFragment.Text(folderSector1Is));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(folderSector2Is));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(folderSector3Is));
                    }
                    messages.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta, Precision.AUTO_GAPS)));
                    messages.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                }
            }
            if (messages.Count > 0)
            {
                Console.WriteLine("Sector = " + sector + " delta (-ve = player faster) = " + (playerTime - comparisonTime).ToString("0.000"));
                Console.WriteLine("Resolved delta message: " + String.Join(", ", messages));
            }
            return messages;
        }

        private static float getAutoRoundedDelta(float time1, float time2)
        {
            float unroundedDelta = time1 - time2;
            float delta;
            if (Math.Abs(unroundedDelta) < 0.5)
            {
                delta = ((float)Math.Round(unroundedDelta * 100)) / 100f;
            }
            else
            {
                delta = ((float)Math.Round(unroundedDelta * 10)) / 10f;
            }
            return delta;
        }
        
        public static List<MessageFragment> getSectorDeltaMessages(SectorReportOption reportOption, float playerSector1, float comparisonSector1, float playerSector2,
            float comparisonSector2, float playerSector3, float comparisonSector3, Boolean comparisonIncludesAllLaps, bool selfPace)
        {
            List<MessageFragment> messageFragments = new List<MessageFragment>();
            float delta1 = float.MaxValue;
            float delta2 = float.MaxValue;
            float delta3 = float.MaxValue;
            // the sector times must be > 5 seconds to be considered valid
            if (playerSector1 > 5 && comparisonSector1 > 5)
            {
                if (playerSector1 < comparisonSector1)
                {
                    delta1 = -1;
                }
                else
                {
                    delta1 = getAutoRoundedDelta(playerSector1, comparisonSector1);
                }
            } if (playerSector2 > 5 && comparisonSector2 > 5)
            {
                if (playerSector2 < comparisonSector2)
                {
                    delta2 = -1;
                }
                else
                {
                    delta2 = getAutoRoundedDelta(playerSector2, comparisonSector2);
                }
            }
            if (playerSector3 > 5 && comparisonSector3 > 5)
            {
                if (playerSector3 < comparisonSector3)
                {
                    delta3 = -1;
                }
                else
                {
                    delta3 = getAutoRoundedDelta(playerSector3, comparisonSector3);
                }
            }

            if (reportOption == SectorReportOption.WORST_ONLY)
            {
                // remove the 2 best sector deltas so we only report on the worst one
                if (delta1 < float.MaxValue && delta1 > delta2 && delta1 > delta3)
                {
                    // worst is delta1
                    delta2 = float.MaxValue;
                    delta3 = float.MaxValue;
                }
                else if (delta2 < float.MaxValue && delta2 > delta1 && delta2 > delta3)
                {
                    // worst is delta2
                    delta1 = float.MaxValue;
                    delta3 = float.MaxValue;
                }
                else if (delta3 < float.MaxValue && delta3 > delta1 && delta3 > delta2)
                {
                    // worst is delta3
                    delta1 = float.MaxValue;
                    delta2 = float.MaxValue;
                }
            }
            // now report the deltas
            Boolean reportedDelta1 = false;
            Boolean reportedDelta2 = false;
            Boolean reportedDelta3 = false;
            if (nearlyEqual(delta1, delta2))
            {
                if (nearlyEqual(delta3, delta1))
                {
                    reportedDelta1 = true;
                    reportedDelta2 = true;
                    reportedDelta3 = true;
                    if (delta1 < 0.05)
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllSectorsFast));
                    }
                    else if (nearlyEqual(delta1, 0.1f))
                    {
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfAllSectorsATenthOffThePace : folderAllSectorsATenthOffThePace));
                    }
                    else if (nearlyEqual(delta1, 0.2f))
                    {
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfAllSectorsTwoTenthsOffThePace : folderAllSectorsTwoTenthsOffThePace));
                    }
                    else if (nearlyEqual(delta1, 1))
                    {
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfAllSectorsASecondOffThePace : folderAllSectorsASecondOffThePace));
                    }
                    else if (delta1 < 10 && delta1 > 0)
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllThreeSectorsAre));
                        messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                    }
                }
                else
                {
                    reportedDelta1 = true;
                    reportedDelta2 = true;
                    if (delta1 < 0.05)
                    {
                        messageFragments.Add(MessageFragment.Text(folderSector1and2Fast));
                    }
                    else if (nearlyEqual(delta1, 0.1f))
                    {
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1and2ATenthOffThePace : folderSector1and2ATenthOffThePace));
                    }
                    else if (nearlyEqual(delta1, 0.2f))
                    {
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1and2TwoTenthsOffThePace : folderSector1and2TwoTenthsOffThePace));
                    }
                    else if (nearlyEqual(delta1, 1f))
                    {
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1and2ASecondOffThePace : folderSector1and2ASecondOffThePace));
                    }
                    else if (delta1 < 10 && delta1 > 0)
                    {
                        messageFragments.Add(MessageFragment.Text(folderSectors1And2Are));
                        messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                        messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                    }
                }
            }
            else if (nearlyEqual(delta2, delta3))
            {
                reportedDelta2 = true;
                reportedDelta3 = true;
                if (delta2 < 0.05)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2and3Fast));
                }
                else if (nearlyEqual(delta2, 0.1f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector2and3ATenthOffThePace : folderSector2and3ATenthOffThePace));
                }
                else if (nearlyEqual(delta2, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector2and3TwoTenthsOffThePace : folderSector2and3TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta2, 1))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector2and3ASecondOffThePace : folderSector2and3ASecondOffThePace));
                }
                else if (delta2 < 10 && delta2 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSectors2And3Are));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                }
            }
            else if (nearlyEqual(delta1, delta3))
            {
                reportedDelta1 = true;
                reportedDelta3 = true;
                if (delta1 < 0.05)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1and3Fast));
                }
                else if (nearlyEqual(delta1, 0.1f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1and3ATenthOffThePace : folderSector1and3ATenthOffThePace));
                }
                else if (nearlyEqual(delta1, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1and3TwoTenthsOffThePace : folderSector1and3TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta1, 1))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1and3ASecondOffThePace : folderSector1and3ASecondOffThePace));
                }
                else if (delta1 < 10 && delta1 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSectors1And3Are));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                }
            }

            if (!reportedDelta1)
            {
                if (delta1 < 0.05)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1Fast));
                }
                else if (nearlyEqual(delta1, 0.1f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1ATenthOffThePace : folderSector1ATenthOffThePace));
                }
                else if (nearlyEqual(delta1, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1TwoTenthsOffThePace : folderSector1TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta1, 1))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector1ASecondOffThePace : folderSector1ASecondOffThePace));
                }
                else if (delta1 < 10 && delta1 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1Is));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                }
            }
            if (!reportedDelta2)
            {
                if (delta2 < 0.05)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2Fast));
                }
                else if (nearlyEqual(delta2, 0.1f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector2ATenthOffThePace : folderSector2ATenthOffThePace));
                }
                else if (nearlyEqual(delta2, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector2TwoTenthsOffThePace : folderSector2TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta2, 1))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector2ASecondOffThePace : folderSector2ASecondOffThePace));
                }
                else if (delta2 < 10 && delta2 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2Is));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta2, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                }
            }
            if (!reportedDelta3)
            {
                if (delta3 < 0.05)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector3Fast));
                }
                else if (nearlyEqual(delta3, 0.1f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector3ATenthOffThePace : folderSector3ATenthOffThePace));
                }
                else if (nearlyEqual(delta3, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector3TwoTenthsOffThePace : folderSector3TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta3, 1))
                {
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfSector3ASecondOffThePace : folderSector3ASecondOffThePace));
                }
                else if (delta3 < 10 && delta3 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector3Is));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta3, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(selfPace ? folderSelfOffThePace : folderOffThePace));
                }
            }
            if (messageFragments.Count > 0)
            {
                Console.WriteLine("Player best sectors " + playerSector1.ToString("0.000") + ",    " + playerSector2.ToString("0.000") + ",    " + playerSector3.ToString("0.000"));
                Console.WriteLine("Opponent best sectors " + comparisonSector1.ToString("0.000") + ",    " + comparisonSector2.ToString("0.000") + ",    " + comparisonSector3.ToString("0.000"));
                Console.WriteLine("S1 delta (-ve = player faster) = " + (playerSector1 - comparisonSector1).ToString("0.000") +
                    "    S2 delta  = " + (playerSector2 - comparisonSector2).ToString("0.000") +
                    "    S3 delta  = " + (playerSector3 - comparisonSector3).ToString("0.000"));
                Console.WriteLine("Resolved delta message: " + String.Join(", ", messageFragments));
            }
            return messageFragments;
        }

        public enum SectorReportOption
        {
            WORST_ONLY, ALL
        }

        private Boolean ConditionsHaveChanged(Conditions.ConditionsSample sample1, Conditions.ConditionsSample sample2)
        {
            if (sample1 == null || sample2 == null)
            {
                // hmm....
                return false;
            }
            return ConditionsMonitor.getRainLevel(sample1.RainDensity) != ConditionsMonitor.getRainLevel(sample2.RainDensity) || 
                Math.Abs(sample1.TrackTemperature - sample2.TrackTemperature) > 4;
        }

        public static Boolean nearlyEqual(float a, float b)
        {
            if (a == b)
            {
                return true;
            }
            // calculate a suitable epsilon
            float absA = Math.Abs(a);
            float absB = Math.Abs(b);
            float diff = Math.Abs(absA - absB);
            float epsilon;
            if (diff <= 0.1f)
            {
                epsilon = 0.04f;
            }
            else if (diff <= 0.5f)
            {
                epsilon = 0.1f;
            }
            else
            {
                epsilon = 0.15f;
            }
            
            return diff < epsilon;
        }
    }
}
