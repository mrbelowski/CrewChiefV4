using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class ConditionsMonitor : AbstractEvent
    {
        private Boolean enableTrackAndAirTempReports = UserSettings.GetUserSettings().getBoolean("enable_track_and_air_temp_reports");

        public static TimeSpan ConditionsSampleFrequency = TimeSpan.FromSeconds(10);
        private TimeSpan AirTemperatureReportMaxFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("ambient_temp_check_interval_seconds"));
        private TimeSpan TrackTemperatureReportMaxFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("track_temp_check_interval_seconds"));

        // is this acceptable? It means we report rain changes as quickly as possible but it might be noisy...
        private TimeSpan RainReportMaxFrequency = ConditionsSampleFrequency;

        private float minTrackTempDeltaToReport = UserSettings.GetUserSettings().getFloat("report_ambient_temp_changes_greater_than");
        private float minAirTempDeltaToReport = UserSettings.GetUserSettings().getFloat("report_track_temp_changes_greater_than");

        private DateTime lastAirTempReport;
        private DateTime lastTrackTempReport;
        private DateTime lastRainReport;

        private float airTempAtLastReport;
        private float trackTempAtLastReport;
        private float rainAtLastReport;

        public static String folderAirAndTrackTempIncreasing = "conditions/air_and_track_temp_increasing";
        public static String folderAirAndTrackTempDecreasing = "conditions/air_and_track_temp_decreasing";
        public static String folderTrackTempIsNow = "conditions/track_temp_is_now";
        public static String folderAirTempIsNow = "conditions/air_temp_is_now"; 
        public static String folderTrackTempIs = "conditions/track_temp_is";
        public static String folderAirTempIs = "conditions/air_temp_is";
        public static String folderAirTempIncreasing = "conditions/air_temp_increasing_its_now";
        public static String folderAirTempDecreasing = "conditions/air_temp_decreasing_its_now";
        public static String folderTrackTempIncreasing = "conditions/track_temp_increasing_its_now";
        public static String folderTrackTempDecreasing = "conditions/track_temp_decreasing_its_now";
        public static String folderCelsius = "conditions/celsius";
        public static String folderFahrenheit = "conditions/fahrenheit";
        public static String folderSeeingSomeRain = "conditions/seeing_some_rain";
        public static String folderStoppedRaining = "conditions/stopped_raining";

        private static Boolean useFahrenheit = UserSettings.GetUserSettings().getBoolean("use_fahrenheit");

        private Conditions.ConditionsSample currentConditions;

        public ConditionsMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            lastRainReport = DateTime.MinValue;
            lastAirTempReport = DateTime.MaxValue;
            lastTrackTempReport = DateTime.MaxValue;
            airTempAtLastReport = float.MinValue;
            trackTempAtLastReport = float.MinValue;
            rainAtLastReport = float.MinValue;
            currentConditions = null;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            currentConditions = currentGameState.Conditions.getMostRecentConditions();
            if (currentConditions != null && enableTrackAndAirTempReports) 
            {
                if (airTempAtLastReport == float.MinValue)
                {
                    airTempAtLastReport = currentConditions.AmbientTemperature;
                    trackTempAtLastReport = currentConditions.TrackTemperature;
                    rainAtLastReport = currentConditions.RainDensity;
                    lastRainReport = currentGameState.Now;
                    lastTrackTempReport = currentGameState.Now;
                    lastAirTempReport = currentGameState.Now;
                }
                else
                {
                    Boolean canReportAirChange = currentGameState.Now > lastAirTempReport.Add(AirTemperatureReportMaxFrequency);
                    Boolean canReportTrackChange = currentGameState.Now > lastTrackTempReport.Add(TrackTemperatureReportMaxFrequency);
                    Boolean reportedCombinedTemps = false;
                    if (canReportAirChange || canReportTrackChange)
                    {
                        if (currentConditions.TrackTemperature > trackTempAtLastReport + minTrackTempDeltaToReport && currentConditions.AmbientTemperature > airTempAtLastReport + minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            trackTempAtLastReport = currentConditions.TrackTemperature;
                            lastAirTempReport = currentGameState.Now;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("conditionsAirAndTrackIncreasing1", MessageContents
                                (folderAirAndTrackTempIncreasing, folderAirTempIsNow, convertTemp(currentConditions.AmbientTemperature),
                                folderTrackTempIsNow, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                            reportedCombinedTemps = true;
                        }
                        else if (currentConditions.TrackTemperature < trackTempAtLastReport - minTrackTempDeltaToReport && currentConditions.AmbientTemperature < airTempAtLastReport - minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            trackTempAtLastReport = currentConditions.TrackTemperature;
                            lastAirTempReport = currentGameState.Now;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("conditionsAirAndTrackDecreasing1", MessageContents
                                (folderAirAndTrackTempDecreasing, folderAirTempIsNow, convertTemp(currentConditions.AmbientTemperature),
                                folderTrackTempIsNow, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                            reportedCombinedTemps = true;
                        }
                    }
                    if (!reportedCombinedTemps && canReportAirChange)
                    {
                        if (currentConditions.AmbientTemperature > airTempAtLastReport + minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            lastAirTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("conditionsAirIncreasing", MessageContents
                                (folderAirTempIncreasing, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this));
                        }
                        else if (currentConditions.AmbientTemperature < airTempAtLastReport - minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            lastAirTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("conditionsAirDecreasing", MessageContents
                                (folderAirTempDecreasing, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this));
                        }
                    }
                    if (!reportedCombinedTemps && canReportTrackChange)
                    {
                        if (currentConditions.TrackTemperature > trackTempAtLastReport + minTrackTempDeltaToReport)
                        {
                            trackTempAtLastReport = currentConditions.TrackTemperature;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("conditionsTrackIncreasing", MessageContents
                                (folderTrackTempIncreasing, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                        }
                        else if (currentConditions.TrackTemperature < trackTempAtLastReport - minTrackTempDeltaToReport)
                        {
                            trackTempAtLastReport = currentConditions.TrackTemperature;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("conditionsTrackDecreasing", MessageContents
                                (folderTrackTempDecreasing, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                        }
                    }
                    if (currentGameState.Now > lastRainReport.Add(RainReportMaxFrequency))
                    {
                        // TODO: the implementation is coupled to the PCars mRainDensity value, which is 0 or 1
                        if (currentConditions.RainDensity == 0 && rainAtLastReport == 1)
                        {
                            rainAtLastReport = currentConditions.RainDensity;
                            lastRainReport = currentGameState.Now;
                            audioPlayer.playMessage(new QueuedMessage(folderStoppedRaining, 0, this));
                        }
                        else if (currentConditions.RainDensity == 1 && rainAtLastReport == 0)
                        {
                            rainAtLastReport = currentConditions.RainDensity;
                            lastRainReport = currentGameState.Now;
                            audioPlayer.playMessage(new QueuedMessage(folderSeeingSomeRain, 0, this));
                        }
                    }
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            if (currentConditions == null)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
            }
            else
            {
                if (voiceMessage.Contains(SpeechRecogniser.WHATS_THE_AIR_TEMP) || voiceMessage.Contains(SpeechRecogniser.WHATS_THE_AIR_TEMP))
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("airTempResponse",
                        MessageContents(folderAirTempIsNow, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, null), false);
                }
                if (voiceMessage.Contains(SpeechRecogniser.WHATS_THE_TRACK_TEMP) || voiceMessage.Contains(SpeechRecogniser.WHATS_THE_TRACK_TEMPERATURE))
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("trackTempResponse",
                        MessageContents(folderTrackTempIsNow, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, null), false);
                }
            }
        }
    }
}
