﻿using CrewChiefV4.GameState;
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

        int maxQueueLengthForRaceSectorDeltaReports = 0;
        int maxQueueLengthForRaceLapTimeReports = 0;

        // for qualifying:
        // "that was a 1:34.2, you're now 0.4 seconds off the pace"
        public static String folderLapTimeIntro = "lap_times/time_intro";
        public static String folderGapIntro = "lap_times/gap_intro";

        public static String folderGapOutroOffPace = "lap_times/gap_outro_off_pace";
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
        
        private TimeSpan deltaPlayerLastToSessionBestInClass;

        private Boolean deltaPlayerLastToSessionBestInClassSet = false;

        private float lastLapTime;

        private float bestLapTime;
        
        private int currentPosition;

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
            conditionsWindow = new List<Conditions.ConditionsSample>();
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

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<string, object> validationData)
        {
            // TODO: unfuck me
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if ((eventSubType == folderImprovingTimes || eventSubType == folderConsistentTimes || eventSubType == folderWorseningTimes) &&
                    currentGameState.SessionData.SectorNumber != 1)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
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
            if (previousGameState != null && previousGameState.SessionData.CompletedLaps <= currentGameState.FlagData.lapCountWhenLastWentGreen)
            {
                return;
            }
            float[] lapAndSectorsComparisonData = new float[] { -1, -1, -1, -1 };
            if (currentGameState.SessionData.IsNewSector)
            {
                isHotLapping = currentGameState.SessionData.SessionType == SessionType.HotLap || (currentGameState.OpponentData.Count == 0 || (
                    currentGameState.OpponentData.Count == 1 && currentGameState.OpponentData.First().Value.DriverRawName == currentGameState.SessionData.DriverRawName));
                if (isHotLapping)
                {
                    lapAndSectorsComparisonData[0] = currentGameState.SessionData.PlayerLapTimeSessionBest;
                    lapAndSectorsComparisonData[1] = currentGameState.SessionData.PlayerBestSector1Time;
                    lapAndSectorsComparisonData[2] = currentGameState.SessionData.PlayerBestSector2Time;
                    lapAndSectorsComparisonData[3] = currentGameState.SessionData.PlayerBestSector3Time;
                }
                else
                {
                    if (currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRace, currentGameState.carClass);
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice)
                    {
                        lapAndSectorsComparisonData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass);
                        float[] playerBestLapAndSectors = new float[] { -1, -1, -1, -1 };
                        if (currentGameState.SessionData.IsNewLap)
                        {
                            // If this is a new lap, then the just completed lap became last lap.  We do not want to use it as a comparison,
                            // we need previous player best time.
                            playerBestLapAndSectors = currentGameState.SessionData.getPlayerTimeAndSectorsForBestLap(true /*ignoreLast*/);
                        }
                        else
                        {
                            playerBestLapAndSectors = currentGameState.SessionData.getPlayerTimeAndSectorsForBestLap(false /*ignoreLast*/);
                        }
                        if (playerBestLapAndSectors[0] > 0.0 && playerBestLapAndSectors[0] < lapAndSectorsComparisonData[0])
                        {
                            // Use player's best lap as comparison data.
                            lapAndSectorsComparisonData = playerBestLapAndSectors;
                        }
                    }
                }
            }
            // TODO: in R3E this previousGameState OnOutLap doesn't appear to true when we start our flying lap
            if (!currentGameState.PitData.OnInLap && previousGameState != null && !previousGameState.PitData.OnOutLap 
                && !currentGameState.PitData.InPitlane)   // as this is a new lap, check whether the *previous* state was an outlap
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
                        Conditions.ConditionsSample conditionsSample = currentGameState.Conditions.getMostRecentConditions();
                        if (conditionsSample != null)
                        {
                            conditionsWindow.Insert(0, conditionsSample);
                        }
                        if (lapIsValid)
                        {
                            Boolean playedLapTime = false;
                            if (isHotLapping)
                            {
                                // always play the laptime in hotlap mode
                                audioPlayer.playMessage(new QueuedMessage("laptime",
                                        MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LapTimePrevious, Precision.AUTO_LAPTIMES)), 0, this));
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
                                            MessageContents(folderGapIntro, new TimeSpanWrapper(deltaPlayerLastToSessionBestInClass, Precision.AUTO_GAPS), folderGapOutroOffPace), 0, this));
                                    }
                                    if (practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > Utilities.random.NextDouble() * 10)
                                    {
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(SectorReportOption.ALL, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
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
                                            if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                                            {
                                                audioPlayer.playMessage(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro, Position.folderStub + 1), 0, this));
                                            }
                                            else
                                            {
                                                audioPlayer.playMessage(new QueuedMessage(Position.folderStub + 1, 0, this));
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
                                        if ((!disablePCarspracAndQualPoleDeltaReports || 
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.raceRoom.gameEnum ||
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.iracing.gameEnum ||
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.assetto32Bit.gameEnum ||
                                            CrewChief.gameDefinition.gameEnum == GameDefinition.assetto64Bit.gameEnum) &&
                                            (gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50))
                                        {
                                            // delay this a bit...
                                            audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                                MessageContents(folderGapIntro, new TimeSpanWrapper(gapBehind, Precision.AUTO_GAPS), folderQuickerThanSecondPlace), Utilities.random.Next(0, 8), this));
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
                                    if ((!disablePCarspracAndQualPoleDeltaReports || CrewChief.gameDefinition.gameEnum == GameDefinition.raceRoom.gameEnum || 
                                        CrewChief.gameDefinition.gameEnum == GameDefinition.iracing.gameEnum ||
                                        CrewChief.gameDefinition.gameEnum == GameDefinition.assetto32Bit.gameEnum ||
                                        CrewChief.gameDefinition.gameEnum == GameDefinition.assetto64Bit.gameEnum) &&
                                        (deltaPlayerLastToSessionBestInClass.Seconds > 0 || deltaPlayerLastToSessionBestInClass.Milliseconds > 50) &&
                                        deltaPlayerLastToSessionBestInClass.Seconds < 60)
                                    {
                                        // delay this a bit...
                                        audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
                                            MessageContents(folderGapIntro, new TimeSpanWrapper(deltaPlayerLastToSessionBestInClass, Precision.AUTO_GAPS), folderGapOutroOffPace), Utilities.random.Next(0, 8), this));
                                    }
                                    if (practiceAndQualSectorReportsLapEnd && frequencyOfPracticeAndQualSectorDeltaReports > Utilities.random.NextDouble() * 10)
                                    {
                                        List<MessageFragment> sectorMessageFragments = getSectorDeltaMessages(SectorReportOption.ALL, currentGameState.SessionData.LastSector1Time, lapAndSectorsComparisonData[1],
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], true);
                                        if (sectorMessageFragments.Count > 0)
                                        {
                                            audioPlayer.playMessage(new QueuedMessage("sectorDeltas", sectorMessageFragments, 0, this));
                                            sectorsReportedForLap = true;
                                        }
                                    }
                                }
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                Boolean playedLapMessage = false;
                                if (frequencyOfPlayerRaceLapTimeReports > Utilities.random.NextDouble() * 10)
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
                                            if (Utilities.random.NextDouble() < 0.5)
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
                                            if (Utilities.random.NextDouble() < 0.2)
                                            {
                                                playedLapMessage = true;
                                                audioPlayer.playMessage(new QueuedMessage(folderGoodLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, pearlLikelihood);
                                            }
                                            break;
                                        default:
                                            break;
                                    }
                                }

                                if (raceSectorReportsAtLapEnd && frequencyOfRaceSectorDeltaReports > Utilities.random.NextDouble() * 10)
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
                                            currentGameState.SessionData.LastSector2Time, lapAndSectorsComparisonData[2], currentGameState.SessionData.LastSector3Time, lapAndSectorsComparisonData[3], false);
                                    if (sectorMessageFragments.Count > 0)
                                    {
                                        QueuedMessage message = new QueuedMessage("sectorDeltas", sectorMessageFragments, 0, this);
                                        message.maxPermittedQueueLengthForMessage = maxQueueLengthForRaceSectorDeltaReports;
                                        audioPlayer.playMessage(message);
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
                                        audioPlayer.playMessage(new QueuedMessage(folderConsistentTimes, Utilities.random.Next(0, 8), this));
                                    }
                                    else if (consistency == ConsistencyResult.IMPROVING)
                                    {
                                        lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;
                                        audioPlayer.playMessage(new QueuedMessage(folderImprovingTimes, Utilities.random.Next(0, 8), this));
                                    }
                                    else if (consistency == ConsistencyResult.WORSENING)
                                    {
                                        // don't play the worsening message if the lap rating is good
                                        if (lastLapRating == LastLapRating.BEST_IN_CLASS || lastLapRating == LastLapRating.BEST_OVERALL ||
                                            lastLapRating == LastLapRating.SETTING_CURRENT_PACE || lastLapRating == LastLapRating.CLOSE_TO_CURRENT_PACE)
                                        {
                                            Console.WriteLine("Skipping 'worsening' laptimes message - inconsistent with lap rating");
                                        }
                                        else if (currentGameState.SessionData.Position >= currentGameState.SessionData.PositionAtStartOfCurrentLap)
                                        {
                                            // only complain about worsening laptimes if we've not overtaken anyone on this lap
                                            lastConsistencyUpdate = currentGameState.SessionData.CompletedLaps;

                                            audioPlayer.playMessage(new QueuedMessage(folderWorseningTimes, Utilities.random.Next(0, 8), this, new Dictionary<String, Object>()));
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
                    double r = Utilities.random.NextDouble() * 10;
                    Boolean canPlayForRace = frequencyOfRaceSectorDeltaReports > r;
                    Boolean canPlayForPracAndQual = frequencyOfPracticeAndQualSectorDeltaReports > r;
                    
                    if ((currentGameState.SessionData.SessionType == SessionType.Race && canPlayForRace) ||
                        (((currentGameState.SessionData.SessionType == SessionType.Practice && (currentGameState.OpponentData.Count > 0 || currentGameState.SessionData.CompletedLaps > 1))
                        || currentGameState.SessionData.SessionType == SessionType.Qualify ||
                        (currentGameState.SessionData.SessionType == SessionType.HotLap && currentGameState.SessionData.CompletedLaps > 1)) && canPlayForPracAndQual))
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
                        List<MessageFragment> messageFragments = getSingleSectorDeltaMessages(sectorEnum, playerSector, comparisonSector);
                        if (messageFragments.Count() > 0)
                        {
                            audioPlayer.playMessage(new QueuedMessage("singleSectorDelta", messageFragments, Utilities.random.Next(2, 4), this));
                        }
                    }
                }
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
                if (currentGameState != null && 
                    currentGameState.SessionData.LastSector1Time > -1 && currentGameState.SessionData.LastSector2Time > -1 && currentGameState.SessionData.LastSector3Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector1Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector1Time, Precision.AUTO_LAPTIMES)), 0, this));
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector2Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector2Time, Precision.AUTO_LAPTIMES)), 0, this));
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector3Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector3Time, Precision.AUTO_LAPTIMES)), 0, this));
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
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector3Time, Precision.AUTO_LAPTIMES)), 0, this));
                }
                else if (currentGameState != null && currentGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.LastSector1Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector1Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector1Time, Precision.AUTO_LAPTIMES)), 0, this));
                }
                else if (currentGameState != null && currentGameState.SessionData.SectorNumber == 3 && currentGameState.SessionData.LastSector2Time > -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sector2Time",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.LastSector2Time, Precision.AUTO_LAPTIMES)), 0, this));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_BEST_LAP_TIME))
            {
                if (bestLapTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("bestLapTime",
                        MessageContents(TimeSpan.FromSeconds(bestLapTime)), 0, this));

                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_THE_FASTEST_LAP_TIME))
            {
                if (currentGameState.SessionData.PlayerClassSessionBestLapTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("sessionFastestLaptime",
                        MessageContents(TimeSpanWrapper.FromSeconds(currentGameState.SessionData.PlayerClassSessionBestLapTime, Precision.AUTO_LAPTIMES)), 0, this));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHAT_WAS_MY_LAST_LAP_TIME))
            {
                if (lastLapTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("laptime",
                        MessageContents(folderLapTimeIntro, TimeSpanWrapper.FromSeconds(lastLapTime, Precision.AUTO_LAPTIMES)), 0, this));
                    
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_PACE))
            {
                if (sessionType == SessionType.Race)
                {
                    if (currentGameState == null)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    }
                    else
                    {
                        float[] bestOpponentLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(paceCheckLapsWindowForRace, currentGameState.carClass);

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
                            SectorReportOption reportOption = SectorReportOption.ALL;
                            double r = Utilities.random.NextDouble();
                            // usually report the combined sectors
                            if (r > 0.33)
                            {
                                reportOption = SectorReportOption.WORST_ONLY;
                            }
                            List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(reportOption, currentGameState.SessionData.LastSector1Time, bestOpponentLapData[1],
                                currentGameState.SessionData.LastSector2Time, bestOpponentLapData[2], currentGameState.SessionData.LastSector3Time, bestOpponentLapData[3], false);
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
                                audioPlayer.playMessage(new QueuedMessage("lapTimeNotRaceGap",
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

                        // TODO: wrap this in a try-catch until I work out why the array indices are being screwed up in online races (yuk...)
                        try
                        {

                            float[] bestOpponentLapData = currentGameState.getTimeAndSectorsForBestOpponentLapInWindow(-1, currentGameState.carClass);
                            List<MessageFragment> sectorDeltaMessages = getSectorDeltaMessages(SectorReportOption.ALL, currentGameState.SessionData.LastSector1Time, bestOpponentLapData[1],
                                currentGameState.SessionData.LastSector2Time, bestOpponentLapData[2], currentGameState.SessionData.LastSector3Time, bestOpponentLapData[3], true);
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
                    else {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                        
                    }
                }
            }
        }

        private enum LastLapRating
        {
            BEST_OVERALL, BEST_IN_CLASS, SETTING_CURRENT_PACE, CLOSE_TO_CURRENT_PACE, PERSONAL_BEST_CLOSE_TO_OVERALL_LEADER, PERSONAL_BEST_CLOSE_TO_CLASS_LEADER,
            PERSONAL_BEST_STILL_SLOW, CLOSE_TO_OVERALL_LEADER, CLOSE_TO_CLASS_LEADER, CLOSE_TO_PERSONAL_BEST, MEH, NO_DATA
        }

        public enum SectorSet
        {
            ONE, TWO, THREE, NONE
        }

        public static List<MessageFragment> getSingleSectorDeltaMessages(SectorSet sector, float playerTime, float comparisonTime)
        {
            List<MessageFragment> messages = new List<MessageFragment>();
            // the sector times must be > 5 seconds to be considered valid
            if (playerTime > 5 && comparisonTime > 5)
            {
                float delta = ((float)Math.Round((playerTime - comparisonTime) * 10)) / 10f;
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
                        messages.Add(MessageFragment.Text(folderSector1ATenthOffThePace));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(folderSector2ATenthOffThePace));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(folderSector3ATenthOffThePace));
                    }
                }
                else if (nearlyEqual(delta, 0.2f))
                {
                    if (sector == SectorSet.ONE)
                    {
                        messages.Add(MessageFragment.Text(folderSector1TwoTenthsOffThePace));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(folderSector2TwoTenthsOffThePace));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(folderSector3TwoTenthsOffThePace));
                    }
                }
                else if (nearlyEqual(delta, 1))
                {
                    if (sector == SectorSet.ONE)
                    {
                        messages.Add(MessageFragment.Text(folderSector1ASecondOffThePace));
                    }
                    else if (sector == SectorSet.TWO)
                    {
                        messages.Add(MessageFragment.Text(folderSector2ASecondOffThePace));
                    }
                    else
                    {
                        messages.Add(MessageFragment.Text(folderSector3ASecondOffThePace));
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
                    messages.Add(MessageFragment.Text(folderOffThePace));
                }
            }
            return messages;
        }
        
        public static List<MessageFragment> getSectorDeltaMessages(SectorReportOption reportOption, float playerSector1, float comparisonSector1, float playerSector2,
            float comparisonSector2, float playerSector3, float comparisonSector3, Boolean comparisonIncludesAllLaps)
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
                    delta1 = ((float)Math.Round((playerSector1 - comparisonSector1) * 10)) / 10f;
                }
            } if (playerSector2 > 5 && comparisonSector2 > 5)
            {
                if (playerSector2 < comparisonSector2)
                {
                    delta2 = -1;
                }
                else
                {
                    delta2 = ((float)Math.Round((playerSector2 - comparisonSector2) * 10)) / 10f;
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
                    delta3 = ((float)Math.Round((playerSector3 - comparisonSector3) * 10)) / 10f;
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
                    if (delta1 < 0.04)
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllSectorsFast));
                    }
                    else if (nearlyEqual(delta1, 0.1f))
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllSectorsATenthOffThePace));
                    }
                    else if (nearlyEqual(delta1, 0.2f))
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllSectorsTwoTenthsOffThePace));
                    }
                    else if (nearlyEqual(delta1, 1))
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllSectorsASecondOffThePace));
                    }
                    else if (delta1 < 10 && delta1 > 0)
                    {
                        messageFragments.Add(MessageFragment.Text(folderAllThreeSectorsAre));
                        messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                        messageFragments.Add(MessageFragment.Text(folderOffThePace));
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
                        messageFragments.Add(MessageFragment.Text(folderSector1and2ATenthOffThePace));
                    }
                    else if (nearlyEqual(delta1, 0.2f))
                    {
                        messageFragments.Add(MessageFragment.Text(folderSector1and2TwoTenthsOffThePace));
                    }
                    else if (nearlyEqual(delta1,  1f))
                    {
                        messageFragments.Add(MessageFragment.Text(folderSector1and2ASecondOffThePace));
                    }
                    else if (delta1 < 10 && delta1 > 0)
                    {
                        messageFragments.Add(MessageFragment.Text(folderSectors1And2Are));
                        messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                        messageFragments.Add(MessageFragment.Text(folderOffThePace));
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
                    messageFragments.Add(MessageFragment.Text(folderSector2and3ATenthOffThePace));
                }
                else if (nearlyEqual(delta2, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2and3TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta2, 1))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2and3ASecondOffThePace));
                }
                else if (delta2 < 10 && delta2 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSectors2And3Are));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(folderOffThePace));
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
                    messageFragments.Add(MessageFragment.Text(folderSector1and3ATenthOffThePace));
                }
                else if (nearlyEqual(delta1, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1and3TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta1, 1))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1and3ASecondOffThePace));
                }
                else if (delta1 < 10 && delta1 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSectors1And3Are));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(folderOffThePace));
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
                    messageFragments.Add(MessageFragment.Text(folderSector1ATenthOffThePace));
                }
                else if (nearlyEqual(delta1, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta1, 1))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1ASecondOffThePace));
                }
                else if (delta1 < 10 && delta1 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector1Is));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta1, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(folderOffThePace));
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
                    messageFragments.Add(MessageFragment.Text(folderSector2ATenthOffThePace));
                }
                else if (nearlyEqual(delta2, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta2, 1))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2ASecondOffThePace));
                }
                else if (delta2 < 10 && delta2 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector2Is));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta2, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(folderOffThePace));
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
                    messageFragments.Add(MessageFragment.Text(folderSector3ATenthOffThePace));
                }
                else if (nearlyEqual(delta3, 0.2f))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector3TwoTenthsOffThePace));
                }
                else if (nearlyEqual(delta3, 1))
                {
                    messageFragments.Add(MessageFragment.Text(folderSector3ASecondOffThePace));
                }
                else if (delta3 < 10 && delta3 > 0)
                {
                    messageFragments.Add(MessageFragment.Text(folderSector3Is));
                    messageFragments.Add(MessageFragment.Time(TimeSpanWrapper.FromSeconds(delta3, Precision.AUTO_GAPS)));
                    messageFragments.Add(MessageFragment.Text(folderOffThePace));
                }
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
            return Math.Abs(sample1.RainDensity - sample2.RainDensity) > 0.02 || Math.Abs(sample1.TrackTemperature - sample2.TrackTemperature) > 4;
        }

        public static Boolean nearlyEqual(float a, float b)
        {
            return nearlyEqual(a, b, 0.01f);
        }

        public static Boolean nearlyEqual(float a, float b, float epsilon) {
            if (a == b)
            {
                return true;
            }
            float absA = Math.Abs(a);
            float absB = Math.Abs(b);
            float diff = Math.Abs(a - b);

            if (a == 0 || b == 0 || diff < float.Epsilon) {
                // a or b is zero or both are extremely close to it
                // relative error is less meaningful here
                return diff < (epsilon * float.Epsilon);
            } else { // use relative error
                return diff / (absA + absB) < epsilon;
            }
        }
    }
}
