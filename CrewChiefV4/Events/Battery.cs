using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using System.Diagnostics;

namespace CrewChiefV4.Events
{
    class Battery : AbstractEvent
    {
        private readonly bool EnableBatteryMessages = UserSettings.GetUserSettings().getBoolean("enable_battery_messages");

        private const string folderOneLapEstimate = "battery/one_lap_battery";
        private const string folderTwoLapsEstimate = "battery/two_laps_battery";
        private const string folderThreeLapsEstimate = "battery/three_laps_battery";
        private const string folderFourLapsEstimate = "battery/four_laps_battery";
        private const string folderHalfDistanceGoodBattery = "battery/half_distance_good_battery";
        private const string folderHalfDistanceLowBattery = "battery/half_distance_low_battery";
        private const string folderHalfChargeWarning = "battery/half_charge_warning";
        private const string folderTenMinutesBattery = "battery/ten_minutes_battery";
        private const string folderTwoMinutesBattery = "battery/two_minutes_battery";
        private const string folderFiveMinutesBattery = "battery/five_minutes_battery";
        private const string folderLowBattery = "battery/low_battery";
        private const string folderCriticalBattery = "battery/critical_battery";
        private const string folderMinutesRemaining = "battery/minutes_remaining";
        private const string folderLapsRemaining = "battery/laps_remaining";
        private const string folderWeEstimate = "battery/we_estimate";
        private const string folderPlentyOfBattery = "battery/plenty_of_battery";
        private const string folderPercentRemaining = "battery/percent_remaining";
        private const string folderAboutToRunOut = "battery/about_to_run_out";
        private const string folderPercentagePerLap = "battery/percent_per_lap";
        private const string folderPercent = "battery/percent";
        private const string folderOnLastLapYouUsed = "battery/on_last_lap_you_used";
        private const string folderPercentOfYourBattery = "battery/percent_of_your_battery";
        private const string folderUseIncreasing = "battery/battery_use_increasing";
        private const string folderUseDecreasing = "battery/battery_use_reducing";
        private const string folderUseStable = "battery/battery_use_stable";
        private const string folderShouldMakeEnd = "battery/current_charge_should_make_end";
        private const string folderShouldMakeHalfDistance = "battery/current_charge_should_make_half_distance";
        private const string folderReduceUseToMakeEnd = "battery/reduce_battery_use_to_make_end";
        private const string folderReduceUseHalfDistance = "battery/reduce_battery_use_to_make_half_distance";
        private const string folderIncreaseUseEasilyMakeEnd = "battery/increase_battery_use_easily_make_end";
        private const string folderIncreaseUseEasilyMakeHalfDistance = "battery/increase_battery_use_easily_make_half_distance";
        private const string folderWontMakeEndWoPit = "battery/wont_make_end_without_pitstop";
        private const string folderWontMakeHalfDistanceWoPit = "battery/wont_make_half_distance_without_pitstop";
        private const string folderWeWillGetAnother = "battery/we_will_get_another";
        public static readonly string folderLaps = "battery/laps";
        public static readonly string folderMinutes = "battery/minutes";

        class BatteryStatsEntry
        {
            internal float AverageBatteryPercentageLeft = -1.0f;
            internal float MinimumBatteryPercentageLeft = float.MaxValue;
            internal float SessionRunningTime = -1.0f;
        };

        bool initialized = false;

        // Per lap battery stats.  Might not be necessary in a long run, but I'd like to have this in order to get better understanding.
        List<BatteryStatsEntry> batteryStats = new List<BatteryStatsEntry>();

        private int currLapNumBatteryMeasurements = 0;
        private float currLapBatteryPercentageLeftAccumulator = 0.0f;
        private float currLapMinBatteryLeft = float.MaxValue;

        class BatteryWindowedStatsEntry
        {
            internal float BatteryPercentageLeft = -1.0f;
            internal float SessionRunningTime = -1.0f;
        };

        private const int NumLapsAverageWindow = 5;
        private const float BatteryLowThreshold = 10.0f;
        private const float BatteryLowLapsFactor = 1.8f;
        private const float BatteryCriticalThreshold = 5.0f;
        private const float BatteryCriticaLapsFactor = 0.8f;

        private const float AveragedChargeWindowTime = 15.0f;
        private LinkedList<BatteryWindowedStatsEntry> windowedBatteryStats = new LinkedList<BatteryWindowedStatsEntry>();

        bool batteryUseActive = false;

        private bool sessionHasFixedNumberOfLaps = false;
        private int halfDistance = -1;
        private int halfTime = -1;
        private int sessionNumberOfLaps = -1;
        private float sessionTotalRunTime = -1.0f;
        private float sessionRunningTime = -1.0f;
        private int completedLaps = -1;
        private int currLapBatteryUseSectorCheck = -1;
        private float initialBatteryChargePercentage = -1.0f;
        private float initialBatteryGameTime = -1.0f;

        // Checking if we need to read battery messages involves a bit of arithmetic and stuff, so only do this every few seconds
        private DateTime nextBatteryStatusCheck = DateTime.MinValue;
        private readonly TimeSpan batteryStatusCheckInterval = TimeSpan.FromSeconds(5);

        private bool playedPitForBatteryNow = false;
        private bool playedHalfDistanceBatteryEstimate = false;
        private bool playedHalfTimeBatteryEstimate = false;
        private bool playedTwoMinutesRemaining = false;
        private bool playedFiveMinutesRemaining = false;
        private bool playedTenMinutesRemaining = false;
        private bool playedFourLapsRemaining = false;
        private bool playedThreeLapsRemaining = false;
        private bool playedTwoLapsRemaining = false;
        private bool playedBatteryLowWarning = false;
        private bool playedBatteryCriticalWarning = false;
        private bool playedHalfBatteryChargeWarning = false;

        // Cache variables to be used in command responses (separate thread, can't access collections).
        // It is not critical for all values to be consistent.  But if that changes we'll have to lock.
        private float averageUsagePerLap = -1.0f;
        private float averageUsagePerMinute = -1.0f;
        private float prevLapBatteryUse = -1.0f;
        private float windowedAverageChargeLeft = -1.0f;
        private int numBatteryStatsEntries = -1;

        // We don't calculate averaged stats for the OutLap (because they largely vary on how the pit lane is).
        // Instead, just capture battery level at a lap start (windowed avg).  That way we can still announce last lap use.
        private float firstFullLapInitialChargeLeft = -1.0f;
        private float firstFullLapGameTime = -1.0f;
        private BatteryUseTrend lastReportedTrend = BatteryUseTrend.Unknown;

        public Battery(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Countdown, SessionPhase.FullCourseYellow }; }
        }

        public override void clearState()
        {
            this.initialized = false;

            this.batteryStats.Clear();
            this.windowedBatteryStats.Clear();
            this.currLapNumBatteryMeasurements = 0;
            this.currLapBatteryPercentageLeftAccumulator = 0.0f;
            this.currLapMinBatteryLeft = float.MaxValue;

            this.batteryUseActive = false;
            this.initialBatteryGameTime = -1.0f;
            this.sessionHasFixedNumberOfLaps = false;
            this.halfDistance = -1;
            this.halfTime = -1;
            this.sessionNumberOfLaps = -1;
            this.sessionTotalRunTime = -1.0f;
            this.sessionRunningTime = -1.0f;
            this.completedLaps = -1;
            this.currLapBatteryUseSectorCheck = -1;
            this.initialBatteryChargePercentage = -1.0f;

            this.nextBatteryStatusCheck = DateTime.MinValue;

            this.playedPitForBatteryNow = false;
            this.playedHalfDistanceBatteryEstimate = false;
            this.playedHalfTimeBatteryEstimate = false;
            this.playedTwoMinutesRemaining = false;
            this.playedFiveMinutesRemaining = false;
            this.playedTenMinutesRemaining = false;
            this.playedFourLapsRemaining = false;
            this.playedThreeLapsRemaining = false;
            this.playedTwoLapsRemaining = false;

            this.playedHalfBatteryChargeWarning = false;
            this.playedBatteryLowWarning = false;
            this.playedBatteryCriticalWarning = false;

            this.averageUsagePerLap = -1.0f;
            this.averageUsagePerMinute = -1.0f;
            this.prevLapBatteryUse = -1.0f;
            this.windowedAverageChargeLeft = -1.0f;
            this.numBatteryStatsEntries = -1;

            this.firstFullLapInitialChargeLeft = -1.0f;
            this.firstFullLapGameTime = -1.0f;

            this.lastReportedTrend = BatteryUseTrend.Unknown;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
                return;

            this.batteryUseActive = currentGameState.BatteryData.BatteryUseActive;
            var currBattLeftPct = currentGameState.BatteryData.BatteryPercentageLeft;

            this.sessionRunningTime = currentGameState.SessionData.SessionRunningTime;
            this.completedLaps = currentGameState.SessionData.CompletedLaps;

            // Only track battery data after the session has settled down
            if (this.batteryUseActive && currentGameState.SessionData.SessionRunningTime > 15 &&
                ((currentGameState.SessionData.SessionType == SessionType.Race &&
                    (currentGameState.SessionData.SessionPhase == SessionPhase.Green ||
                     currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow ||
                     currentGameState.SessionData.SessionPhase == SessionPhase.Checkered)) ||
                 ((currentGameState.SessionData.SessionType == SessionType.Qualify ||
                   currentGameState.SessionData.SessionType == SessionType.Practice ||
                   currentGameState.SessionData.SessionType == SessionType.HotLap ||
                   currentGameState.SessionData.SessionType == SessionType.LonePractice) &&
                    (currentGameState.SessionData.SessionPhase == SessionPhase.Green ||
                     currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow ||
                     currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) &&
                    // Don't process battery data in prac and qual until we're actually moving:
                    currentGameState.PositionAndMotionData.CarSpeed > 10)))
            {
                if (!this.initialized
                    || (previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane))  // Vehicle swap or some magical recharge ?
                {
                    this.batteryStats.Clear();
                    this.windowedBatteryStats.Clear();
                    this.currLapNumBatteryMeasurements = 0;
                    this.currLapBatteryPercentageLeftAccumulator = 0.0f;
                    this.currLapMinBatteryLeft = float.MaxValue;
                    this.playedPitForBatteryNow = false;
                    this.playedTwoMinutesRemaining = false;
                    this.playedFiveMinutesRemaining = false;
                    this.playedTenMinutesRemaining = false;
                    this.playedFourLapsRemaining = false;
                    this.playedThreeLapsRemaining = false;
                    this.playedTwoLapsRemaining = false;
                    this.playedBatteryLowWarning = false;
                    this.playedBatteryCriticalWarning = false;

                    // Clear usage stats on pit in.
                    this.averageUsagePerLap = -1.0f;
                    this.averageUsagePerMinute = -1.0f;
                    this.prevLapBatteryUse = -1.0f;
                    this.windowedAverageChargeLeft = -1.0f;
                    this.numBatteryStatsEntries = -1;
                    this.firstFullLapInitialChargeLeft = -1.0f;
                    this.firstFullLapGameTime = -1.0f;

                    this.lastReportedTrend = BatteryUseTrend.Unknown;

                    this.initialBatteryGameTime = currentGameState.SessionData.SessionRunningTime;
                    this.initialBatteryChargePercentage = currBattLeftPct;

                    if (!this.initialized)
                    {
                        if (currentGameState.SessionData.SessionNumberOfLaps > 1)
                        {
                            this.sessionHasFixedNumberOfLaps = true;
                            this.halfDistance = (int)Math.Ceiling(currentGameState.SessionData.SessionNumberOfLaps / 2.0f);
                            this.sessionNumberOfLaps = currentGameState.SessionData.SessionNumberOfLaps;
                        }
                        else if (currentGameState.SessionData.SessionTotalRunTime > 0.0f)
                        {
                            this.sessionHasFixedNumberOfLaps = false;
                            this.halfTime = (int)Math.Ceiling(currentGameState.SessionData.SessionTotalRunTime / 2.0f);
                            this.sessionTotalRunTime = currentGameState.SessionData.SessionTotalRunTime;
                        }

                        Console.WriteLine(string.Format("Battery use tracking initilized: halfDistance = {0} laps    halfTime = {1} minutes",
                            this.halfDistance,
                            this.halfTime));

                        this.initialized = true;
                    }
                }

                if (!currentGameState.PitData.InPitlane)
                {
                    // Track windowed average charge level.
                    this.windowedBatteryStats.AddLast(new BatteryWindowedStatsEntry()
                    {
                        BatteryPercentageLeft = currBattLeftPct,
                        SessionRunningTime = currentGameState.SessionData.SessionRunningTime
                    });

                    // Remove records older than Battery.averagedChargeWindowTime.
                    var entry = this.windowedBatteryStats.First;
                    while (entry != null)
                    {
                        var next = entry.Next;
                        if ((currentGameState.SessionData.SessionRunningTime - entry.Value.SessionRunningTime) > Battery.AveragedChargeWindowTime)
                            this.windowedBatteryStats.Remove(entry);
                        else
                            break;  // We're done.

                        entry = next;
                    }

                    // Calculate windowed average charge level:
                    this.windowedAverageChargeLeft = this.windowedBatteryStats.Average(e => e.BatteryPercentageLeft);
                }

                if (currentGameState.SessionData.IsNewLap
                    && this.firstFullLapInitialChargeLeft == -1.0f)
                {
                    this.firstFullLapInitialChargeLeft = this.windowedAverageChargeLeft;
                    this.firstFullLapGameTime = currentGameState.SessionData.SessionRunningTime;
                }

                if (currentGameState.PitData.OnOutLap  // Don't track out laps.
                    || (previousGameState != null && previousGameState.PitData.OnOutLap)
                    || currentGameState.PitData.InPitlane)  // or in pit lane.
                    return;

                if (currentGameState.SessionData.IsNewLap
                    && this.currLapNumBatteryMeasurements > 0
                    && currentGameState.SessionData.SessionRunningTime > 5.0f)  // Guard against getting called too early.
                {
                    this.batteryStats.Add(new BatteryStatsEntry()
                    {
                        AverageBatteryPercentageLeft = this.currLapBatteryPercentageLeftAccumulator / this.currLapNumBatteryMeasurements,
                        MinimumBatteryPercentageLeft = this.currLapMinBatteryLeft,
                        SessionRunningTime = currentGameState.SessionData.SessionRunningTime
                    });

                    this.numBatteryStatsEntries = this.batteryStats.Count;
                    this.currLapBatteryPercentageLeftAccumulator = 0.0f;
                    this.currLapNumBatteryMeasurements = 0;
                    this.currLapMinBatteryLeft = float.MaxValue;

                    this.currLapBatteryUseSectorCheck = Utilities.random.Next(1, 4);

                    // Update cached stats:
                    var prevLapStats = this.batteryStats.Last();

                    Debug.Assert(this.firstFullLapInitialChargeLeft != -1.0f);
                    var batteryDrainSinceMonitoringStart = this.firstFullLapInitialChargeLeft - this.windowedAverageChargeLeft;

                    if (this.batteryStats.Count > 1)
                    {
                        // Get battery use per lap:
                        var startIdx = Math.Max(0, this.batteryStats.Count - Battery.NumLapsAverageWindow - 1);
                        var numDataPoints = Math.Min(this.batteryStats.Count - 1, Battery.NumLapsAverageWindow);

                        var acc = 0.0f;
                        for (var i = startIdx; i < this.batteryStats.Count - 1; ++i)
                            acc += this.batteryStats[i].AverageBatteryPercentageLeft - this.batteryStats[i + 1].AverageBatteryPercentageLeft;

                        this.averageUsagePerLap = acc / numDataPoints;

                        // Calculate per minute usage:
                        this.averageUsagePerMinute = ((this.batteryStats[startIdx].AverageBatteryPercentageLeft - prevLapStats.AverageBatteryPercentageLeft)
                            / (prevLapStats.SessionRunningTime - this.batteryStats[startIdx].SessionRunningTime)) * 60.0f;

                        // Save previous lap consumption:
                        this.prevLapBatteryUse = this.batteryStats[this.batteryStats.Count - 2].AverageBatteryPercentageLeft - prevLapStats.AverageBatteryPercentageLeft;
                    }
                    else
                    {
                        // If this is first lap completed, just use its consumption.
                        this.averageUsagePerLap = this.prevLapBatteryUse = batteryDrainSinceMonitoringStart > 0.0f ? batteryDrainSinceMonitoringStart : -1.0f;

                        this.averageUsagePerMinute = batteryDrainSinceMonitoringStart > 0.0f
                            ? (batteryDrainSinceMonitoringStart / (prevLapStats.SessionRunningTime - this.firstFullLapGameTime)) * 60.0f : -1.0f;
                    }

                    Console.WriteLine(string.Format("Last lap battery use: {0}%  avg battery left: {1}%  min: {2}%  windowed charge: {3}%,  curr charge: {4}%  windowed avg use per lap: {5}%  avg use per minute: {6}%",
                        this.prevLapBatteryUse.ToString("0.000"),
                        this.batteryStats.Last().AverageBatteryPercentageLeft.ToString("0.000"),
                        this.batteryStats.Last().MinimumBatteryPercentageLeft.ToString("0.000"),
                        this.windowedAverageChargeLeft,
                        currBattLeftPct.ToString("0.000"),
                        this.averageUsagePerLap.ToString("0.000"),
                        this.averageUsagePerMinute.ToString("0.000")));

                    // Play low/critical on beginning of a new lap.
                    if (((this.averageUsagePerLap > 0.0f  // If avg usage per lap available, calculate threshold dynamically.
                            && this.windowedAverageChargeLeft < (this.averageUsagePerLap * Battery.BatteryLowLapsFactor))
                        || (this.averageUsagePerLap < 0.0f && this.windowedAverageChargeLeft <= Battery.BatteryLowThreshold))  // In a corner case of no avg use available, just use fixed threshold.
                        && !this.playedBatteryLowWarning)
                    {
                        this.playedBatteryLowWarning = true;
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderLowBattery), 0, this), 6);
                    }
                    else if (((this.averageUsagePerLap > 0.0f  // If avg usage per lap available, calculate threshold dynamically.
                            && this.windowedAverageChargeLeft < (this.averageUsagePerLap * Battery.BatteryCriticaLapsFactor))
                        || (this.averageUsagePerLap < 0.0f && this.windowedAverageChargeLeft <= Battery.BatteryCriticalThreshold))  // In a corner case of no avg use available, just use fixed threshold.
                        && !this.playedBatteryCriticalWarning)
                    {
                        this.playedBatteryCriticalWarning = true;
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderCriticalBattery), 0, this), 10);
                    }
                }

                // Update this lap stats.
                this.currLapBatteryPercentageLeftAccumulator += currBattLeftPct;
                ++this.currLapNumBatteryMeasurements;

                this.currLapMinBatteryLeft = Math.Min(this.currLapMinBatteryLeft, currBattLeftPct);

                // NOTE: unlike fuel messages, here we process data on new sector and randomly in cetain sector.  This is to reduce message overload on the new lap.
                // Warnings for particular battery levels
                if (this.EnableBatteryMessages
                    && currentGameState.SessionData.IsNewSector
                    && this.currLapBatteryUseSectorCheck == currentGameState.SessionData.SectorNumber
                    && this.batteryStats.Count > 0)
                {
                    Debug.Assert(this.windowedAverageChargeLeft >= 0.0f);

                    var prevLapStats = this.batteryStats.Last();

                    // Warnings for fixed lap sessions.
                    if (this.averageUsagePerLap > 0.0f
                        && (currentGameState.SessionData.SessionNumberOfLaps > 0
                            || currentGameState.SessionData.SessionType == SessionType.HotLap
                            || currentGameState.SessionData.SessionType == SessionType.LonePractice))
                    {
                        var battStatusMsg = string.Format("windowed avg charge = {0}%  previous lap avg charge = {1}%,  previous lap min charge = {2}%, current battery level = {3}%,  prev lap usage = {4}%,  usage per lap = {5}%",
                            this.windowedAverageChargeLeft.ToString("0.000"),
                            prevLapStats.AverageBatteryPercentageLeft.ToString("0.000"),
                            prevLapStats.MinimumBatteryPercentageLeft.ToString("0.000"),
                            currentGameState.BatteryData.BatteryPercentageLeft.ToString("0.000"),
                            this.prevLapBatteryUse.ToString("0.000"),
                            this.averageUsagePerLap.ToString("0.000"));

                        var estBattLapsLeft = (int)Math.Floor(this.windowedAverageChargeLeft / this.averageUsagePerLap);

                        if (this.halfDistance != -1
                            && !this.playedHalfDistanceBatteryEstimate
                            && currentGameState.SessionData.SessionType == SessionType.Race
                            && currentGameState.SessionData.CompletedLaps == this.halfDistance)
                        {
                            Console.WriteLine("Half race distance, " + battStatusMsg);

                            this.playedHalfDistanceBatteryEstimate = true;

                            if (estBattLapsLeft < this.halfDistance
                                && this.windowedAverageChargeLeft / this.initialBatteryChargePercentage < 0.6f)
                            {
                                if (currentGameState.PitData.IsElectricVehicleSwapAllowed)
                                {
                                    this.audioPlayer.playMessage(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this), 3);
                                    this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", MessageContents(Battery.folderWeEstimate, estBattLapsLeft, Battery.folderLapsRemaining), 0, this), 3);
                                }
                                else
                                    this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceLowBattery, 0, this), 8);
                            }
                            else
                                this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceGoodBattery, 0, this), 3);
                        }
                        else if (currentGameState.SessionData.SessionLapsRemaining > 3 && estBattLapsLeft == 4 && !this.playedFourLapsRemaining)
                        {
                            this.playedFourLapsRemaining = true;
                            Console.WriteLine("4 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderFourLapsEstimate, 0, this), 5);
                        }
                        else if (currentGameState.SessionData.SessionLapsRemaining > 2 && estBattLapsLeft == 3 && !this.playedThreeLapsRemaining)
                        {
                            this.playedThreeLapsRemaining = true;
                            Console.WriteLine("3 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderThreeLapsEstimate, 0, this), 7);
                        }
                        else if (currentGameState.SessionData.SessionLapsRemaining > 1 && estBattLapsLeft == 2 && !this.playedTwoLapsRemaining)
                        {
                            this.playedTwoLapsRemaining = true;
                            Console.WriteLine("2 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderTwoLapsEstimate, 0, this), 10);
                        }
                        else if (currentGameState.SessionData.SessionLapsRemaining > 0 && estBattLapsLeft == 1)
                        {
                            Console.WriteLine("1 lap of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderOneLapEstimate, 0, this), 10);

                            // If we've not played the pit-now message, play it with a bit of a delay - should probably wait for sector3 here
                            // but i'd have to move some stuff around and I'm an idle fucker
                            if (!this.playedPitForBatteryNow && currentGameState.SessionData.SessionLapsRemaining > 1)
                            {
                                this.playedPitForBatteryNow = true;
                                this.audioPlayer.playMessage(new QueuedMessage(PitStops.folderMandatoryPitStopsPitThisLap, 10, this), 10);
                            }
                        }
                    }
                    // warnings for fixed time sessions - check every 5 seconds
                    else if (currentGameState.Now > this.nextBatteryStatusCheck
                        && currentGameState.SessionData.SessionNumberOfLaps <= 0
                        && currentGameState.SessionData.SessionTotalRunTime > 0.0f
                        && this.averageUsagePerMinute > 0.0f)
                    {
                        var battStatusMsg = string.Format("windowed avg charge = {0}%,  previous lap avg charge = {1}%,  previous lap min charge = {2}%, current battery level = {3}%,  prev lap usage = {4}%,  usage per minute = {5}%",
                            this.windowedAverageChargeLeft.ToString("0.000"),
                            prevLapStats.AverageBatteryPercentageLeft.ToString("0.000"),
                            prevLapStats.MinimumBatteryPercentageLeft.ToString("0.000"),
                            currentGameState.BatteryData.BatteryPercentageLeft.ToString("0.000"),
                            this.prevLapBatteryUse.ToString("0.000"),
                            this.averageUsagePerMinute.ToString("0.000"));

                        this.nextBatteryStatusCheck = currentGameState.Now.Add(this.batteryStatusCheckInterval);
                        if (halfTime != -1
                            && !this.playedHalfTimeBatteryEstimate
                            && currentGameState.SessionData.SessionTimeRemaining <= halfTime
                            && currentGameState.SessionData.SessionTimeRemaining > halfTime - 30)
                        {
                            Console.WriteLine("Half race time, " + battStatusMsg);
                            this.playedHalfTimeBatteryEstimate = true;

                            if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                if (this.averageUsagePerMinute * this.halfTime / 60.0f > this.windowedAverageChargeLeft
                                    && this.windowedAverageChargeLeft / this.initialBatteryChargePercentage < 0.6)
                                {
                                    if (currentGameState.PitData.IsElectricVehicleSwapAllowed)
                                    {
                                        var minutesLeft = (int)Math.Floor(prevLapStats.AverageBatteryPercentageLeft / this.averageUsagePerMinute);
                                        this.audioPlayer.playMessage(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this), 3);
                                        this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", MessageContents(
                                            Battery.folderWeEstimate, minutesLeft, Battery.folderMinutesRemaining), 0, this), 3);
                                    }
                                    else
                                        this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceLowBattery, 0, this), 8);
                                }
                                else
                                    this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceGoodBattery, 0, this), 3);
                            }
                        }

                        var estBattMinsLeft = this.windowedAverageChargeLeft / this.averageUsagePerMinute;
                        if (estBattMinsLeft < 1.5f && !this.playedPitForBatteryNow)
                        {
                            Console.WriteLine("Less than 1.5 mins of battery charge left, " + battStatusMsg);

                            this.playedPitForBatteryNow = true;
                            this.playedTwoMinutesRemaining = true;
                            this.playedFiveMinutesRemaining = true;
                            this.playedTenMinutesRemaining = true;
                            float cutoffForVehicleSwapCall = currentGameState.SessionData.PlayerLapTimeSessionBest * 2;
                            if (cutoffForVehicleSwapCall == 0)
                            {
                                // shouldn't need this, but just in case...
                                cutoffForVehicleSwapCall = 120;
                            }
                            if (currentGameState.SessionData.SessionTimeRemaining > cutoffForVehicleSwapCall)
                            {
                                this.audioPlayer.playMessage(new QueuedMessage("pit_for_vehicle_swap_now",
                                    MessageContents(Battery.folderAboutToRunOut, PitStops.folderMandatoryPitStopsPitThisLap), 0, this), 10);
                            }
                            else
                            {
                                this.audioPlayer.playMessage(new QueuedMessage("about_to_run_out_of_battery", MessageContents(Battery.folderAboutToRunOut), 0, this), 10);
                            }
                        }
                        if (estBattMinsLeft <= 2.0f && estBattMinsLeft > 1.8f && !this.playedTwoMinutesRemaining)
                        {
                            Console.WriteLine("Less than 2 mins of battery charge left, " + battStatusMsg);

                            this.playedTwoMinutesRemaining = true;
                            this.playedFiveMinutesRemaining = true;
                            this.playedTenMinutesRemaining = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderTwoMinutesBattery, 0, this), 10);
                        }
                        else if (estBattMinsLeft <= 5.0f && estBattMinsLeft > 4.8f && !this.playedFiveMinutesRemaining)
                        {
                            Console.WriteLine("Less than 5 mins of battery charge left, " + battStatusMsg);

                            this.playedFiveMinutesRemaining = true;
                            this.playedTenMinutesRemaining = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderFiveMinutesBattery, 0, this), 8);
                        }
                        else if (estBattMinsLeft <= 10.0f && estBattMinsLeft > 9.8f && !this.playedTenMinutesRemaining)
                        {
                            Console.WriteLine("Less than 10 mins of battery charge left, " + battStatusMsg);

                            this.playedTenMinutesRemaining = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderTenMinutesBattery, 0, this), 5);
                        }
                        else if (!this.playedHalfBatteryChargeWarning && this.windowedAverageChargeLeft / this.initialBatteryChargePercentage <= 0.55f &&
                            this.windowedAverageChargeLeft / this.initialBatteryChargePercentage >= 0.45f)
                        {
                            Console.WriteLine("Less than 50% of battery charge left, " + battStatusMsg);

                            // warning message for battery left - these play as soon previous lap average charge drops below 1/2.
                            this.playedHalfBatteryChargeWarning = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfChargeWarning, 0, this), 3);
                        }
                    }  // if Timed or fixed lap race

                    if (!this.playedPitForBatteryNow
                        && !this.playedTwoMinutesRemaining
                        && !this.playedTwoLapsRemaining
                        && !this.playedBatteryLowWarning
                        && !this.playedBatteryCriticalWarning)
                    {
                        var bu = this.EvaluateBatteryUse();
                        if (bu == Battery.BatteryUseTrend.Increasing
                            && this.lastReportedTrend != Battery.BatteryUseTrend.Increasing)
                        {
                            this.audioPlayer.playMessage(new QueuedMessage("Battery/trend", MessageContents(Battery.folderUseIncreasing), Utilities.random.Next(0, 11), this), 3);
                            this.lastReportedTrend = Battery.BatteryUseTrend.Increasing;
                        }
                        else if (bu == Battery.BatteryUseTrend.Decreasing
                            && this.lastReportedTrend != Battery.BatteryUseTrend.Decreasing)
                        {
                            this.audioPlayer.playMessage(new QueuedMessage("Battery/trend", MessageContents(Battery.folderUseDecreasing), Utilities.random.Next(0, 11), this), 3);
                            this.lastReportedTrend = Battery.BatteryUseTrend.Decreasing;
                        }
                        else if (bu == Battery.BatteryUseTrend.Stable)
                            this.lastReportedTrend = Battery.BatteryUseTrend.Stable;  // Maybe, we need "Stabilized" message.
                    }
                }
            }
        }

        private enum BatteryUseTrend
        {
            Unknown,
            Stable,
            Increasing,
            Decreasing
        }

        private BatteryUseTrend EvaluateBatteryUse()
        {
            const int lapsToAverage = 3;
            const float statbilityThresholdFactor = 0.03f;  // 3% threshold

            // Need at least 5 data points.
            if (this.batteryStats.Count < (lapsToAverage + 2))
            {
                Console.WriteLine("Battery use trend: Unknown");
                return Battery.BatteryUseTrend.Unknown;
            }

            // Calculate 3 lap average consumption excluding last lap.
            var acc = 0.0f;
            var startIdx = this.batteryStats.Count - lapsToAverage - 2;  // Last lap excluded from the average.
            for (var i = startIdx; i < this.batteryStats.Count - 2; ++i)
                acc += (this.batteryStats[i].AverageBatteryPercentageLeft - this.batteryStats[i + 1].AverageBatteryPercentageLeft);

            var avgUse = acc / lapsToAverage;

            var testLap1Use = this.batteryStats[this.batteryStats.Count - 3].AverageBatteryPercentageLeft 
                - this.batteryStats[this.batteryStats.Count - 2].AverageBatteryPercentageLeft;

            var testLap2Use = this.batteryStats[this.batteryStats.Count - 2].AverageBatteryPercentageLeft
                - this.batteryStats.Last().AverageBatteryPercentageLeft;

            var stableThreshold = avgUse * statbilityThresholdFactor;
            var lap1Delta = testLap1Use - avgUse;
            var lap2Delta = testLap2Use - avgUse;

            var statsMsg = string.Format(".  Change from avg: {0}% and {1}%.  {2}% of avg and {3}% of avg",
                lap1Delta.ToString("0.000"),
                lap2Delta.ToString("0.000"),
                (lap1Delta / (avgUse / 100.0f)).ToString("0.000"),
                (lap2Delta / (avgUse / 100.0f)).ToString("0.000"));

            // See if use delta is significant to test.
            if (Math.Abs(lap1Delta) > stableThreshold
                && Math.Abs(lap2Delta) > stableThreshold)
            {
                // If both most recent laps are lower than calculated average, and decrease is above stable threshold consider use decreasing.
                if (testLap1Use < avgUse && testLap2Use < avgUse)
                {
                    Console.WriteLine("Battery use trend: Decreasing" + statsMsg);
                    return Battery.BatteryUseTrend.Decreasing;
                }
                // Else, both most recent laps are higher than calculated average, and decrease is above stable threshold consider use increasing.
                else if (testLap1Use > avgUse && testLap2Use > avgUse)
                {
                    Console.WriteLine("Battery use trend: Increasing" + statsMsg);
                    return Battery.BatteryUseTrend.Increasing;
                }

                // Otherwise we're stable.
            }

            // If neither is true, consumption is stable
            Console.WriteLine("Battery use trend: Stable" + statsMsg);
            return Battery.BatteryUseTrend.Stable;
        }


        public void reportBatteryStatus(Boolean allowNoDataMessage)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                if (allowNoDataMessage)
                    this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));

                return;
            }

            var batteryRunningLow = false;
            var reportedRemaining = this.reportBatteryRemaining(allowNoDataMessage, out batteryRunningLow);

            var reportedUse = false;

            // Don't report usage stats if we're running low already, it sounds a bit weird.
            if (!batteryRunningLow)
                reportedUse = this.reportBatteryUse(allowNoDataMessage);

            if (!reportedUse && !reportedRemaining && allowNoDataMessage)
                this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
        }

        public override void respond(String voiceMessage)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));

                return;
            }

            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                this.reportBatteryStatus(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY));
            }
        }

        public override void respondMoreInformation(String voiceMessage, Boolean requestedExplicitly)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));

                return;
            }

            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                this.reportExtendedBatteryStatus(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY), requestedExplicitly);
            }
        }

        private bool reportBatteryUse(Boolean useImmediateQueue)
        {
            var haveData = false;
            if (!this.initialized || this.prevLapBatteryUse < 0.0f)
                return haveData;

            if (this.batteryUseActive && this.prevLapBatteryUse > 0.0f)
            {
                // round to 1dp
                var prevLapUse = ((float)Math.Round(this.prevLapBatteryUse * 10.0f)) / 10.0f;
                if (prevLapUse == 0.0f)
                {
                    // rounded battery use is < 0.1 litres per lap - can't really do anything with this.
                    return false;
                }

                var wholeAndFractional = Utilities.WholeAndFractionalPart(prevLapUse);

                if (prevLapUse > 0.0f)
                {
                    haveData = true;

                    var wholePart = wholeAndFractional.Item1;
                    var fractionalPart = wholeAndFractional.Item2;

                    if (fractionalPart > 0)
                    {
                        var messageFragments = new List<MessageFragment>();
                        messageFragments.Add(MessageFragment.Text(Battery.folderOnLastLapYouUsed));
                        messageFragments.Add(MessageFragment.Integer(wholePart, false));
                        messageFragments.Add(MessageFragment.Text(NumberReader.folderPoint));
                        messageFragments.Add(MessageFragment.Integer(fractionalPart, false));
                        messageFragments.Add(MessageFragment.Text(Battery.folderPercentOfYourBattery));

                        if (useImmediateQueue)
                            this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/prev_lap_use", messageFragments, 0, null));
                        else
                            this.audioPlayer.playMessage(new QueuedMessage("Battery/prev_lap_use", messageFragments, 0, this), 1);
                    }
                    else
                    {
                        var messageFragments = new List<MessageFragment>();
                        messageFragments.Add(MessageFragment.Text(Battery.folderOnLastLapYouUsed));
                        messageFragments.Add(MessageFragment.Integer(wholePart, false));
                        messageFragments.Add(MessageFragment.Text(Battery.folderPercentOfYourBattery));

                        if (useImmediateQueue)
                            this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/prev_lap_use", messageFragments, 0, null));
                        else
                            this.audioPlayer.playMessage(new QueuedMessage("Battery/prev_lap_use", messageFragments, 0, this), 1);

                    }
                }
            }

            return haveData;
        }

        private bool reportBatteryRemaining(bool useImmediateQueue, out bool batteryRunningLow)
        {
            var haveData = false;
            batteryRunningLow = false;

            if (this.windowedAverageChargeLeft < 0.0f)
                return haveData;  // Nothing we can do.

            // Assume battery is running low.
            batteryRunningLow = true;

            // Handle no rich data available cases.
            if (!this.batteryUseActive && useImmediateQueue)
            {
                haveData = true;
                batteryRunningLow = false;
                this.audioPlayer.playMessageImmediately(new QueuedMessage(Battery.folderPlentyOfBattery, 0, null));
            }
            else if ((this.averageUsagePerLap > 0.0f  // If avg usage per lap available, calculate threshold dynamically.
                    && this.windowedAverageChargeLeft > (this.averageUsagePerLap * Battery.BatteryLowLapsFactor))
                || (this.averageUsagePerLap < 0.0f && this.windowedAverageChargeLeft > Battery.BatteryLowThreshold))  // In a corner case of no avg use available, just use fixed threshold.
            {
                haveData = true;
                batteryRunningLow = false;
                var messageFragments = new List<MessageFragment>();
                messageFragments.Add(MessageFragment.Integer((int)windowedAverageChargeLeft, false));
                messageFragments.Add(MessageFragment.Text(Battery.folderPercentRemaining));

                if (useImmediateQueue)
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/level", messageFragments, 0, null));
                else
                    this.audioPlayer.playMessage(new QueuedMessage("Battery/level", messageFragments, 0, null), 5);
            }
            else if ((this.averageUsagePerLap > 0.0f  // If avg usage per lap available, calculate threshold dynamically.
                    && this.windowedAverageChargeLeft > (this.averageUsagePerLap * Battery.BatteryCriticaLapsFactor))
                || (this.averageUsagePerLap < 0.0f && this.windowedAverageChargeLeft > Battery.BatteryCriticalThreshold))  // In a corner case of no avg use available, just use fixed threshold.
            {
                haveData = true;
                if (useImmediateQueue)
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/level", MessageContents(Battery.folderLowBattery), 0, null));
                else
                    this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderLowBattery), 0, this), 5);
            }
            else if (this.windowedAverageChargeLeft > 0)
            {
                haveData = true;
                var messageFragments = new List<MessageFragment>();
                messageFragments.Add(MessageFragment.Text(Battery.folderCriticalBattery));
                messageFragments.Add(MessageFragment.Text(Battery.folderAboutToRunOut));
                
                if (useImmediateQueue)
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/level", messageFragments, 0, null));
                else
                    this.audioPlayer.playMessage(new QueuedMessage("Battery/level", messageFragments, 0, null), 5);
            }

            if (batteryRunningLow || !this.batteryUseActive)
                return haveData;

            if (this.sessionHasFixedNumberOfLaps && this.averageUsagePerLap > 0.0f)
            {
                // if we've already read something about the battery, use a different phrasing here
                var introSound = haveData ? Battery.folderWeWillGetAnother : Battery.folderWeEstimate;
                var outroSound = haveData ? Battery.folderLaps : Battery.folderLapsRemaining;

                haveData = true;

                var lapsOfBatteryChargeLeft = (int)Math.Floor(this.windowedAverageChargeLeft / this.averageUsagePerLap);
                if (lapsOfBatteryChargeLeft < 0)
                {
                    // nothing to report (pit stop reset on a separate thread)
                    haveData = false;
                }
                else if (lapsOfBatteryChargeLeft <= 1)
                    if (useImmediateQueue)
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", MessageContents(Battery.folderAboutToRunOut), 0, null));
                    else
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", MessageContents(Battery.folderAboutToRunOut), 0, null), 5);
                else
                {
                    var messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(introSound));
                    messageFragments.Add(MessageFragment.Integer(lapsOfBatteryChargeLeft, false));
                    messageFragments.Add(MessageFragment.Text(outroSound));
                    if (useImmediateQueue)
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", messageFragments, 0, null));
                    else
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", messageFragments, 0, null), 5);
                }
            }
            else if (this.averageUsagePerMinute > 0.0f) // Timed race.
            {
                // if we've already read something about the battery, use a different phrasing here
                var introSound = haveData ? Battery.folderWeWillGetAnother : Battery.folderWeEstimate;
                var outroSound = haveData ? Battery.folderMinutes : Battery.folderMinutesRemaining;

                haveData = true;

                var minutesOfBatteryChargeLeft = (int)Math.Floor(windowedAverageChargeLeft / this.averageUsagePerMinute);
                if (minutesOfBatteryChargeLeft < 0)
                {
                    // nothing to report (pit stop reset on a separate thread)
                    haveData = false;
                }
                else if (minutesOfBatteryChargeLeft <= 1)
                    if (useImmediateQueue)
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", MessageContents(Battery.folderAboutToRunOut), 0, null));
                    else
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", MessageContents(Battery.folderAboutToRunOut), 0, null), 10);
                else
                {
                    var messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(introSound));
                    messageFragments.Add(MessageFragment.Integer(minutesOfBatteryChargeLeft, false));
                    messageFragments.Add(MessageFragment.Text(outroSound));
                    if (useImmediateQueue)
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", messageFragments, 0, null));
                    else
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", messageFragments, 0, null));
                }
            }

            return haveData;
        }

        private enum BatteryAdvice
        {
            Unknown,
            WontMakeItWithoutPitting,
            ReduceBatteryUse,
            BatteryUseSpotOn,
            IncreaseBatteryUse,
        }

        public void reportExtendedBatteryStatus(Boolean allowNoDataMessage, Boolean requestedExplicitly)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BATTERY))
            {
                if (allowNoDataMessage && requestedExplicitly)
                    this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));

                return;
            }

            // No point in advising without data.
            if (!this.initialized
                || this.windowedAverageChargeLeft < 0.0f
                || this.numBatteryStatsEntries < 3)
            {
                // play the more-information equivalent of 'no data'
                base.respondMoreInformation("", requestedExplicitly);
                return;
            }

            // Report usage trend:
            var bu = this.EvaluateBatteryUse();
            if (bu == BatteryUseTrend.Decreasing)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/trend", MessageContents(Battery.folderUseDecreasing), 0, null));
            else if (bu == BatteryUseTrend.Increasing)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/trend", MessageContents(Battery.folderUseIncreasing), 0, null));
            else if (bu == BatteryUseTrend.Stable)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/trend", MessageContents(Battery.folderUseStable), 0, null));

            var midRaceReached = false;
            var batteryAdvice = BatteryAdvice.Unknown;

            // Predict if user will be able to make it to half distance (for a pit in) or to finish.
            // Intention is to help user to adjust driving to more/less aggressive.
            // Note: all this was done with FE bias, where you pit once.  If we need to support more than 1 pit nicely more work is necessary.
            if (this.sessionHasFixedNumberOfLaps && this.averageUsagePerLap > 0.0f)
            {
                var lapsOfBatteryChargeLeft = (int)Math.Floor(this.windowedAverageChargeLeft / this.averageUsagePerLap);
                if (lapsOfBatteryChargeLeft < 0)
                {
                    // nothing to report (pit stop reset on a separate thread)
                    return;
                }

                midRaceReached = this.completedLaps >= this.halfDistance;
                if (!midRaceReached)
                {
                    // If we're close the mid race, see if it makes sense to consider end of race messages.
                    midRaceReached = this.completedLaps >= this.halfDistance * 0.66f  // we're within 33% of completing half race.
                        && this.windowedAverageChargeLeft > 70.0f;  // 70% or above charge.
                }

                var lapsToGo = midRaceReached
                    ? this.sessionNumberOfLaps - this.completedLaps
                    : this.halfDistance - this.completedLaps;

                Debug.Assert(lapsToGo >= 0);

                var lapsBalance = lapsOfBatteryChargeLeft - lapsToGo;
                if (lapsBalance == 0 || lapsBalance == 1)
                    batteryAdvice = BatteryAdvice.BatteryUseSpotOn;
                else if (lapsBalance > 1)
                    batteryAdvice = BatteryAdvice.IncreaseBatteryUse;
                else if (lapsBalance < 0  // We're below target
                    && Math.Abs(lapsBalance) <= (lapsToGo * 0.25f))  // but still within 25% of remaining distance.
                    batteryAdvice = BatteryAdvice.ReduceBatteryUse;
                else if (lapsBalance < 0  // We're below target
                    && Math.Abs(lapsBalance) > (lapsToGo * 0.25f))  // And it's hopeless.
                    batteryAdvice = BatteryAdvice.WontMakeItWithoutPitting;

                Console.WriteLine(string.Format("Battery Advice:{0}  Reached mid:{1}  Laps to go:{2}  Laps balance:{3}",
                    batteryAdvice,
                    midRaceReached,
                    lapsToGo,
                    lapsBalance));
            }
            else if (this.averageUsagePerMinute > 0.0f) // Timed race.
            {
                var minutesOfBatteryChargeLeft = (int)Math.Floor(windowedAverageChargeLeft / this.averageUsagePerMinute);
                if (minutesOfBatteryChargeLeft < 0)
                {
                    // nothing to report (pit stop reset on a separate thread)
                    return;
                }

                midRaceReached = this.sessionRunningTime >= this.halfTime;
                if (!midRaceReached)
                {
                    // If we're close the mid race, see if it makes sense to consider end of race messages.
                    midRaceReached = this.sessionRunningTime >= this.halfTime * 0.66f  // we're within 33% of completing half race.
                        && this.windowedAverageChargeLeft > 70.0f;  // 70% or above charge.
                }

                var minsToGo = (midRaceReached
                    ? this.sessionTotalRunTime - this.sessionRunningTime
                    : this.halfTime - this.sessionRunningTime) / 60.0f;

                Debug.Assert(minsToGo >= 0);

                var minsBalance = minutesOfBatteryChargeLeft - minsToGo;
                if (minsBalance >= 0 && minsBalance <= 2)
                    batteryAdvice = BatteryAdvice.BatteryUseSpotOn;
                else if (minsBalance > 2)
                    batteryAdvice = BatteryAdvice.IncreaseBatteryUse;
                else if (minsBalance < 0  // We're below target
                    && Math.Abs(minsBalance) <= (minsToGo * 0.25f))  // but still within 25% of remaining distance.
                    batteryAdvice = BatteryAdvice.ReduceBatteryUse;
                else if (minsBalance < 0  // We're below target
                    && Math.Abs(minsBalance) > (minsToGo * 0.25f))  // And it's hopeless.
                    batteryAdvice = BatteryAdvice.WontMakeItWithoutPitting;

                Console.WriteLine(string.Format("Battery Advice:{0}  Reached mid:{1}  Mins to go:{2}  Mins balance:{3}",
                    batteryAdvice,
                    midRaceReached,
                    minsToGo,
                    minsBalance));
            }

            if (batteryAdvice == BatteryAdvice.BatteryUseSpotOn)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/advice", MessageContents(midRaceReached ? Battery.folderShouldMakeEnd : Battery.folderShouldMakeHalfDistance), 0, null));
            else if (batteryAdvice == BatteryAdvice.IncreaseBatteryUse)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/advice", MessageContents(midRaceReached ? Battery.folderIncreaseUseEasilyMakeEnd : Battery.folderIncreaseUseEasilyMakeHalfDistance), 0, null));
            else if (batteryAdvice == BatteryAdvice.ReduceBatteryUse)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/advice", MessageContents(midRaceReached ? Battery.folderReduceUseToMakeEnd : Battery.folderReduceUseHalfDistance), 0, null));
            else if (batteryAdvice == BatteryAdvice.WontMakeItWithoutPitting)
                this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/advice", MessageContents(midRaceReached ? Battery.folderWontMakeEndWoPit : Battery.folderWontMakeHalfDistanceWoPit), 0, null));
        }
    }
}