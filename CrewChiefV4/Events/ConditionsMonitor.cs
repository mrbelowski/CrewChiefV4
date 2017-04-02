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
        // allow condition messages during caution periods
        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Checkered, SessionPhase.FullCourseYellow }; }
        }

        private static float drizzleMin = 0.02f;
        private static float drizzleMax = 0.1f;
        private static float lightRainMax = 0.25f;
        private static float midRainMax = 0.5f;
        private static float heavyRainMax = 0.75f;

        private enum RainLevel
        {
            NONE, LIGHT, DRIZZLE, MID, HEAVY, STORM
        }

        private Boolean enableTrackAndAirTempReports = UserSettings.GetUserSettings().getBoolean("enable_track_and_air_temp_reports");

        public static TimeSpan ConditionsSampleFrequency = TimeSpan.FromSeconds(10);
        private TimeSpan AirTemperatureReportMaxFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("ambient_temp_check_interval_seconds"));
        private TimeSpan TrackTemperatureReportMaxFrequency = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("track_temp_check_interval_seconds"));

        // don't report rain changes more that 2 minutes apart for RF2
        private TimeSpan RainReportMaxFrequencyRF2 = TimeSpan.FromSeconds(120);
        private TimeSpan RainReportMaxFrequencyPCars = TimeSpan.FromSeconds(10);

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

        // this is for PCars, where the 'rain' flag is boolean
        public static String folderSeeingSomeRain = "conditions/seeing_some_rain";
        
        // these are for RF2 where the rain varies from 0 (dry), 0.5 (rain) to 1.0 (storm).
        public static String folderDrizzleIncreasing = "conditions/drizzle_increasing";
        public static String folderRainLightIncreasing = "conditions/light_rain_increasing";
        public static String folderRainMidIncreasing = "conditions/mid_rain_increasing";
        public static String folderRainHeavyIncreasing = "conditions/heavy_rain_increasing";
        public static String folderRainMax = "conditions/maximum_rain"; // "completely pissing it down"
        public static String folderRainHeavyDecreasing = "conditions/heavy_rain_decreasing";
        public static String folderRainMidDecreasing = "conditions/mid_rain_decreasing";
        public static String folderRainLightDecreasing = "conditions/light_rain_decreasing";
        public static String folderDrizzleDecreasing = "conditions/drizzle_decreasing";

        // this is used for RF2 and PCars
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
            if (currentConditions != null) 
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
                    Boolean canReportAirChange = enableTrackAndAirTempReports &&
                        currentGameState.Now > lastAirTempReport.Add(AirTemperatureReportMaxFrequency);
                    Boolean canReportTrackChange = enableTrackAndAirTempReports &&
                        currentGameState.Now > lastTrackTempReport.Add(TrackTemperatureReportMaxFrequency);
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
                            audioPlayer.playMessage(new QueuedMessage("airAndTrackTemp", MessageContents
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
                            audioPlayer.playMessage(new QueuedMessage("airAndTrackTemp", MessageContents
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
                            audioPlayer.playMessage(new QueuedMessage("airTemp", MessageContents
                                (folderAirTempIncreasing, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this));
                        }
                        else if (currentConditions.AmbientTemperature < airTempAtLastReport - minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            lastAirTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("airTemp", MessageContents
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
                            audioPlayer.playMessage(new QueuedMessage("trackTemp", MessageContents
                                (folderTrackTempIncreasing, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                        }
                        else if (currentConditions.TrackTemperature < trackTempAtLastReport - minTrackTempDeltaToReport)
                        {
                            trackTempAtLastReport = currentConditions.TrackTemperature;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("trackTemp", MessageContents
                                (folderTrackTempDecreasing, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                        }
                    }
                    if (currentGameState.Now > lastRainReport.Add(CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT ? RainReportMaxFrequencyRF2 : RainReportMaxFrequencyPCars))
                    {
                        // for PCars mRainDensity value is 0 or 1
                        if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT ||
                            CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                            CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
                        {
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
                        else if (CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT)
                        {
                            RainLevel currentRainLevel = getRainLevel(currentConditions.RainDensity);
                            RainLevel lastReportedRainLevel = getRainLevel(rainAtLastReport);                            
                            if (currentRainLevel != lastReportedRainLevel)
                            {
                                Boolean increasing = currentConditions.RainDensity > rainAtLastReport;
                                switch (currentRainLevel)
                                {
                                    case RainLevel.DRIZZLE:
                                        audioPlayer.playMessage(new QueuedMessage(folderDrizzleIncreasing, 0, this));
                                        break;
                                    case RainLevel.LIGHT:
                                        audioPlayer.playMessage(new QueuedMessage(increasing ? folderRainLightIncreasing : folderRainLightDecreasing, 0, this));
                                        break;
                                    case RainLevel.MID:
                                        audioPlayer.playMessage(new QueuedMessage(increasing ? folderRainMidIncreasing : folderRainMidDecreasing, 0, this));
                                        break;
                                    case RainLevel.HEAVY:
                                        audioPlayer.playMessage(new QueuedMessage(increasing ? folderRainHeavyIncreasing : folderRainHeavyDecreasing, 0, this));
                                        break;
                                    case RainLevel.STORM:
                                        audioPlayer.playMessage(new QueuedMessage(folderRainMax, 0, this));
                                        break;
                                    case RainLevel.NONE:
                                        audioPlayer.playMessage(new QueuedMessage(folderStoppedRaining, 0, this));
                                        break;
                                }
                                lastRainReport = currentGameState.Now;
                                rainAtLastReport = currentConditions.RainDensity;
                            }
                        }
                    }
                }
            }
        }

        private RainLevel getRainLevel(float amount)
        {
            if (amount > drizzleMin && amount <= drizzleMax)
            {
                return RainLevel.DRIZZLE;
            }
            else if (amount > drizzleMax && amount <= lightRainMax)
            {
                return RainLevel.LIGHT;
            }
            else if (amount > lightRainMax && amount <= midRainMax)
            {
                return RainLevel.MID;
            }
            else if (amount > midRainMax && amount <= heavyRainMax)
            {
                return RainLevel.HEAVY;
            }
            else if (amount > heavyRainMax)
            {
                return RainLevel.STORM;
            }
            else
            {
                return RainLevel.NONE;
            }
        }

        public override void respond(string voiceMessage)
        {
            if (currentConditions == null)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
            else
            {
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_THE_AIR_TEMP))
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("airTemp",
                        MessageContents(folderAirTempIsNow, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, null));
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_THE_TRACK_TEMP))
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("trackTemp",
                        MessageContents(folderTrackTempIsNow, convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, null));
                }
            }
        }
    }
}
