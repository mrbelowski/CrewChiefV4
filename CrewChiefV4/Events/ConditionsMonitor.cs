using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class ConditionsMonitor : AbstractEvent
    {
        // allow condition messages during caution periods
        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Checkered, SessionPhase.FullCourseYellow }; }
        }

        private static float drizzleMin = 0.01f;
        private static float drizzleMax = 0.15f;
        private static float lightRainMax = 0.3f;
        private static float midRainMax = 0.6f;
        private static float heavyRainMax = 0.75f;

        public enum RainLevel
        {
            NONE, DRIZZLE, LIGHT, MID, HEAVY, STORM
        }

        private Boolean enableTrackAndAirTempReports = UserSettings.GetUserSettings().getBoolean("enable_track_and_air_temp_reports");
        private Boolean enablePCarsRainPrediction = UserSettings.GetUserSettings().getBoolean("pcars_enable_rain_prediction");

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
        // this is for PCars2, where we try to interpret a drop in CloudDensity value to mean "rain approaching"
        public static String folderExpectRain = "conditions/we_expect_rain_in_the_next";
        
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
        private Conditions.ConditionsSample conditionsAtStartOfThisLap;

        private float rainDensityAtLastCheck = -1;

        // PCars2 only
        private DateTime timeWhenCloudIncreased = DateTime.MinValue;
        private DateTime timeWhenRainExpected = DateTime.MinValue;
        private Boolean waitingForRainEstimate = false;

        // units here are 'rain quantity per second', where rain quantity is 0 -> 1, direct from the pcars2 or rf2 game data.
        // The number is always positive (it's the absolute change)
        private static float maxRainChangeRate = -1;
        
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
            conditionsAtStartOfThisLap = null;
            timeWhenCloudIncreased = DateTime.MinValue;
            timeWhenRainExpected = DateTime.MinValue;
            waitingForRainEstimate = false;
            rainDensityAtLastCheck = -1;
            maxRainChangeRate = -1;
        }
                
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            currentConditions = currentGameState.Conditions.getMostRecentConditions();
            if (currentGameState.SessionData.IsNewLap)
            {
                conditionsAtStartOfThisLap = currentConditions;
            }
            
            if (currentConditions != null) 
            {
                // for pcars track temp, we're only interested in changes at the start line (a single point on the track) because the track
                // temp is localised. The air temp is (probably) localised too, but will be less variable
                float trackTempToUse = CrewChief.isPCars() && conditionsAtStartOfThisLap != null ? conditionsAtStartOfThisLap.TrackTemperature : currentConditions.TrackTemperature;
                if (airTempAtLastReport == float.MinValue)
                {
                    airTempAtLastReport = currentConditions.AmbientTemperature;
                    trackTempAtLastReport = trackTempToUse;
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
                    TimeSpan rainReportFrequency = CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT ? RainReportMaxFrequencyRF2 : RainReportMaxFrequencyPCars;
                    if (canReportAirChange || canReportTrackChange)
                    {
                        if (trackTempToUse > trackTempAtLastReport + minTrackTempDeltaToReport && currentConditions.AmbientTemperature > airTempAtLastReport + minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            trackTempAtLastReport = trackTempToUse;
                            lastAirTempReport = currentGameState.Now;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("airAndTrackTemp", MessageContents
                                (folderAirAndTrackTempIncreasing, folderAirTempIsNow, convertTemp(currentConditions.AmbientTemperature),
                                folderTrackTempIsNow, convertTemp(trackTempToUse), getTempUnit()), 0, this), 0);
                            reportedCombinedTemps = true;
                        }
                        else if (trackTempToUse < trackTempAtLastReport - minTrackTempDeltaToReport && currentConditions.AmbientTemperature < airTempAtLastReport - minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            trackTempAtLastReport = trackTempToUse;
                            lastAirTempReport = currentGameState.Now;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("airAndTrackTemp", MessageContents
                                (folderAirAndTrackTempDecreasing, folderAirTempIsNow, convertTemp(currentConditions.AmbientTemperature),
                                folderTrackTempIsNow, convertTemp(trackTempToUse), getTempUnit()), 0, this), 0);
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
                                (folderAirTempIncreasing, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this), 0);
                        }
                        else if (currentConditions.AmbientTemperature < airTempAtLastReport - minAirTempDeltaToReport)
                        {
                            airTempAtLastReport = currentConditions.AmbientTemperature;
                            lastAirTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("airTemp", MessageContents
                                (folderAirTempDecreasing, convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this), 0);
                        }
                    }
                    if (!reportedCombinedTemps && canReportTrackChange)
                    {
                        if (trackTempToUse > trackTempAtLastReport + minTrackTempDeltaToReport)
                        {
                            trackTempAtLastReport = trackTempToUse;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("trackTemp", MessageContents
                                (folderTrackTempIncreasing, convertTemp(trackTempToUse), getTempUnit()), 0, this), 0);
                        }
                        else if (trackTempToUse < trackTempAtLastReport - minTrackTempDeltaToReport)
                        {
                            trackTempAtLastReport = trackTempToUse;
                            lastTrackTempReport = currentGameState.Now;
                            // do the reporting
                            audioPlayer.playMessage(new QueuedMessage("trackTemp", MessageContents
                                (folderTrackTempDecreasing, convertTemp(trackTempToUse), getTempUnit()), 0, this), 0);
                        }
                    }
                    //pcars2 test warning
                    if (enablePCarsRainPrediction && CrewChief.isPCars())
                    {
                        if (previousGameState != null && currentGameState.SessionData.SessionRunningTime > 10)
                        {
                            if (currentGameState.RainDensity == 0)
                            {
                                // not raining so see if we can guess when it might start
                                if (!waitingForRainEstimate)
                                {
                                    if (previousGameState.CloudBrightness == 2 && currentGameState.CloudBrightness < 2)
                                    {
                                        timeWhenCloudIncreased = previousGameState.Now;
                                        waitingForRainEstimate = true;
                                    }
                                }
                                else if (currentGameState.CloudBrightness < 1.98)
                                {
                                    // big enough change to calculate expected rain time
                                    TimeSpan timeDelta = currentGameState.Now - timeWhenCloudIncreased;
                                    // assume rain just after it hits 1.9
                                    float millisTillRain = (float)timeDelta.TotalMilliseconds * 6f;
                                    // this is usually really inaccurate and can go either way
                                    timeWhenRainExpected = timeWhenCloudIncreased.AddMilliseconds(millisTillRain);
                                    waitingForRainEstimate = false;
                                    timeWhenCloudIncreased = DateTime.MinValue;
                                    DateTime when = currentGameState.Now.AddMilliseconds(millisTillRain);
                                    Console.WriteLine("It is now " + currentGameState.Now + ", we expect rain at game time " + when);
                                    int minutes = (int)Math.Round(millisTillRain / 60000);

                                    if (minutes > 2)
                                    {
                                        audioPlayer.playMessage(new QueuedMessage("expecting_rain", MessageContents(folderExpectRain,
                                            new TimeSpanWrapper(TimeSpan.FromMinutes(minutes), Precision.MINUTES)), 0, this), 5);
                                    }
                                }
                            }
                            else
                            {
                                // cancel waiting for rain
                                waitingForRainEstimate = false;
                                timeWhenCloudIncreased = DateTime.MinValue;
                            }
                        }
                    }
                    if (currentGameState.Now > lastRainReport.Add(rainReportFrequency))
                    {
                        // for PCars mRainDensity value is 0 or 1
                        if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT ||
                            CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                            CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
                        {
                            if (currentGameState.RainDensity == 0 && rainAtLastReport == 1)
                            {
                                rainAtLastReport = currentGameState.RainDensity;
                                lastRainReport = currentGameState.Now;
                                audioPlayer.playMessage(new QueuedMessage(folderStoppedRaining, 0, this), 2);
                            }
                            else if (currentConditions.RainDensity == 1 && rainAtLastReport == 0)
                            {
                                rainAtLastReport = currentGameState.RainDensity;
                                lastRainReport = currentGameState.Now;
                                audioPlayer.playMessage(new QueuedMessage(folderSeeingSomeRain, 0, this), 5);
                            }
                        }
                        else if (CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2)
                        {
                            if (rainDensityAtLastCheck != -1 && rainDensityAtLastCheck != currentConditions.RainDensity)
                            {
                                float rainChangeRate = (float) (Math.Abs(rainDensityAtLastCheck - currentConditions.RainDensity) / rainReportFrequency.TotalSeconds);
                                if (rainChangeRate > ConditionsMonitor.maxRainChangeRate)
                                {
                                    ConditionsMonitor.maxRainChangeRate = rainChangeRate;
                                }
                            }
                            rainDensityAtLastCheck = currentConditions.RainDensity;
                            RainLevel currentRainLevel = getRainLevel(currentConditions.RainDensity);
                            RainLevel lastReportedRainLevel = getRainLevel(rainAtLastReport);                            
                            if (currentRainLevel != lastReportedRainLevel)
                            {
                                Boolean increasing = currentConditions.RainDensity > rainAtLastReport;
                                switch (currentRainLevel)
                                {
                                    case RainLevel.DRIZZLE:
                                        audioPlayer.playMessageImmediately(new QueuedMessage(folderDrizzleIncreasing, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                        break;
                                    case RainLevel.LIGHT:
                                        audioPlayer.playMessageImmediately(new QueuedMessage(increasing ? folderRainLightIncreasing : folderRainLightDecreasing, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                        break;
                                    case RainLevel.MID:
                                        audioPlayer.playMessageImmediately(new QueuedMessage(increasing ? folderRainMidIncreasing : folderRainMidDecreasing, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                        break;
                                    case RainLevel.HEAVY:
                                        audioPlayer.playMessageImmediately(new QueuedMessage(increasing ? folderRainHeavyIncreasing : folderRainHeavyDecreasing, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                        break;
                                    case RainLevel.STORM:
                                        audioPlayer.playMessageImmediately(new QueuedMessage(folderRainMax, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                                        break;
                                    case RainLevel.NONE:
                                        audioPlayer.playMessage(new QueuedMessage(folderStoppedRaining, 0, this), 3);
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

        public static RainLevel getRainLevel(float amount)
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

        public static TimeSpan getTrackConditionsChangeDelay()
        {
            // maxRainChangeRate is in rain-points-per-second, so *60 gives us rain-points-per-minute.
            if (ConditionsMonitor.maxRainChangeRate == -1)
            {
                return TimeSpan.FromMinutes(2); // complete guesswork - this applies to pcars1 rain changes, and any other changes that
                                                // don't involve rain (i.e. the delay between ambient temp changes and track condition changes)
            }
            // numbers i pulled out of my botty.
            return TimeSpan.FromMinutes(0.3 / (ConditionsMonitor.maxRainChangeRate * 60));
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
