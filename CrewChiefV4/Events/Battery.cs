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
        private readonly bool enableBatteryMessages = UserSettings.GetUserSettings().getBoolean("enable_battery_messages");
        private readonly bool delayResponses = UserSettings.GetUserSettings().getBoolean("enable_delayed_responses");

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

        private const float averagedChargeWindowTime = 15.0f;
        private LinkedList<BatteryWindowedStatsEntry> windowedBatteryStats = new LinkedList<BatteryWindowedStatsEntry>();

        bool batteryUseActive = false;
        private float gameTimeWhenInitialized = -1.0f;
        private bool sessionHasFixedNumberOfLaps = false;
        private int halfDistance = -1;
        private int halfTime = -1;
        private int currLapBatteryUseSectorCheck = -1;
        private float initialBatteryChargePercentage = -1.0f;
        private bool playedPitForBatteryNow = false;

        // Checking if we need to read battery messages involves a bit of arithmetic and stuff, so only do this every few seconds
        private DateTime nextBatteryStatusCheck = DateTime.MinValue;
        private readonly TimeSpan batteryStatusCheckInterval = TimeSpan.FromSeconds(5);

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
        // It should be ok that they aren't from the same update, otherwise we'll have to lock.
        private float averageUsagePerLap = -1.0f;
        private float averageUsagePerMinute = -1.0f;
        private float windowedAverageChargeLeft = -1.0f;

        public Battery(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
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
            this.gameTimeWhenInitialized = -1.0f;
            this.sessionHasFixedNumberOfLaps = false;
            this.halfDistance = -1;
            this.halfTime = -1;
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
            this.windowedAverageChargeLeft = -1.0f;
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

            // Only track battery data after the session has settled down
            if (this.batteryUseActive && currentGameState.SessionData.SessionRunningTime > 15 &&
                ((currentGameState.SessionData.SessionType == SessionType.Race &&
                    (currentGameState.SessionData.SessionPhase == SessionPhase.Green ||
                     currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow ||
                     currentGameState.SessionData.SessionPhase == SessionPhase.Checkered)) ||
                 ((currentGameState.SessionData.SessionType == SessionType.Qualify ||
                   currentGameState.SessionData.SessionType == SessionType.Practice ||
                   currentGameState.SessionData.SessionType == SessionType.HotLap) &&
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
                    this.windowedAverageChargeLeft = -1.0f;
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

                    this.gameTimeWhenInitialized = currentGameState.SessionData.SessionRunningTime;
                    this.initialBatteryChargePercentage = currBattLeftPct;

                    if (!this.initialized)
                    {
                        if (currentGameState.SessionData.SessionNumberOfLaps > 1)
                        {
                            this.sessionHasFixedNumberOfLaps = true;
                            this.halfDistance = (int)Math.Ceiling(currentGameState.SessionData.SessionNumberOfLaps / 2.0f);
                        }
                        else if (currentGameState.SessionData.SessionTotalRunTime > 0.0f)
                        {
                            this.sessionHasFixedNumberOfLaps = false;
                            this.halfTime = (int)Math.Ceiling(currentGameState.SessionData.SessionTotalRunTime / 2.0f);
                        }

                        Console.WriteLine(string.Format("Battery use tracking initilized: initialChargePercentage = {0}%    halfDistance = {1} laps    halfTime = {2} minutes",
                            this.initialBatteryChargePercentage.ToString("0.000"),
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
                        if ((currentGameState.SessionData.SessionRunningTime - entry.Value.SessionRunningTime) > Battery.averagedChargeWindowTime)
                            this.windowedBatteryStats.Remove(entry);
                        else
                            break;  // We're done.

                        entry = next;
                    }

                    // Calculate windowed average charge level:
                    this.windowedAverageChargeLeft = this.windowedBatteryStats.Average(e => e.BatteryPercentageLeft);
                }

                if (currentGameState.PitData.OnOutLap  // Don't track out laps.
                    || (previousGameState != null && previousGameState.PitData.OnOutLap)
                    || currentGameState.PitData.InPitlane)  // or in pit lane.
                    return;

                if (currentGameState.SessionData.IsNewLap
                    && this.currLapNumBatteryMeasurements > 0)
                {
                    this.batteryStats.Add(new BatteryStatsEntry()
                    {
                        AverageBatteryPercentageLeft = this.currLapBatteryPercentageLeftAccumulator / this.currLapNumBatteryMeasurements,
                        MinimumBatteryPercentageLeft = this.currLapMinBatteryLeft,
                        SessionRunningTime = currentGameState.SessionData.SessionRunningTime
                    });

                    Console.WriteLine(string.Format("Last lap average battery left percentage: {0}%  min percentage: {1}%  windowed avg: {2}%,  curr percentage {3}%",
                        this.batteryStats.Last().AverageBatteryPercentageLeft.ToString("0.000"),
                        this.batteryStats.Last().MinimumBatteryPercentageLeft.ToString("0.000"),
                        this.windowedBatteryStats.Average(e => e.BatteryPercentageLeft).ToString("0.000"),
                        currBattLeftPct.ToString("0.000")));

                    this.currLapBatteryPercentageLeftAccumulator = 0.0f;
                    this.currLapNumBatteryMeasurements = 0;
                    this.currLapMinBatteryLeft = float.MaxValue;

                    this.currLapBatteryUseSectorCheck = Utilities.random.Next(1, 3);

                    // Update cached stats:
                    var prevLapStats = this.batteryStats.Last();

                    // Get battery use per lap:
                    this.averageUsagePerLap = (this.initialBatteryChargePercentage - prevLapStats.AverageBatteryPercentageLeft) / this.batteryStats.Count;

                    // Calculate per minute usage:
                    var batteryDrainSinceMonitoringStart = this.initialBatteryChargePercentage - prevLapStats.AverageBatteryPercentageLeft;
                    this.averageUsagePerMinute = (batteryDrainSinceMonitoringStart / prevLapStats.SessionRunningTime) * 60.0f;
                }

                // Update this lap stats.
                this.currLapBatteryPercentageLeftAccumulator += currBattLeftPct;
                ++this.currLapNumBatteryMeasurements;

                this.currLapMinBatteryLeft = Math.Min(this.currLapMinBatteryLeft, currBattLeftPct);

                // NOTE: unlike fuel messages, here we process data on new sector and randomly in cetain sector.  This is to reduce message overload on the new lap.
                // Warnings for particular battery levels
                if (this.enableBatteryMessages
                    && currentGameState.SessionData.IsNewSector
                    && this.currLapBatteryUseSectorCheck == currentGameState.SessionData.SectorNumber
                    && this.batteryStats.Count > 0)
                {
                    Debug.Assert(this.windowedAverageChargeLeft >= 0.0f);

                    var prevLapStats = this.batteryStats.Last();

                    // For now assume 10% is low, below 5% is critical.  Alternatively, this could be tied to avg per lap consumption.
                    if (this.windowedAverageChargeLeft <= 10.0f && !this.playedBatteryLowWarning)
                    {
                        this.playedBatteryLowWarning = true;
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderLowBattery), 0, this));
                    }
                    else if (this.windowedAverageChargeLeft <= 5.0f && !this.playedBatteryCriticalWarning)
                    {
                        this.playedBatteryCriticalWarning = true;
                        this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderCriticalBattery), 0, this));
                    }

                    // Warnings for fixed lap sessions.
                    if (this.averageUsagePerLap > 0.0f
                        && (currentGameState.SessionData.SessionNumberOfLaps > 0 || currentGameState.SessionData.SessionType == SessionType.HotLap))
                    {
                        var battStatusMsg = string.Format("starting battery = {0}%,  windowed avg charge = {1}%  previous lap avg charge = {2}%,  previous lap min charge = {3}%, current battery level = {4}%, usage per lap = {5}%",
                            this.initialBatteryChargePercentage.ToString("0.000"),
                            this.windowedAverageChargeLeft.ToString("0.000"),
                            prevLapStats.AverageBatteryPercentageLeft.ToString("0.000"),
                            prevLapStats.MinimumBatteryPercentageLeft.ToString("0.000"),
                            currentGameState.BatteryData.BatteryPercentageLeft.ToString("0.000"),
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
                                    this.audioPlayer.playMessage(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                    this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", MessageContents(Battery.folderWeEstimate, estBattLapsLeft, Battery.folderLapsRemaining), 0, this));
                                }
                                else
                                    this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceLowBattery, 0, this));
                            }
                            else
                                this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceGoodBattery, 0, this));
                        }
                        else if (estBattLapsLeft == 4 && !this.playedFourLapsRemaining)
                        {
                            this.playedFourLapsRemaining = true;
                            Console.WriteLine("4 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderFourLapsEstimate, 0, this));
                        }
                        else if (estBattLapsLeft == 3 && !this.playedThreeLapsRemaining)
                        {
                            this.playedThreeLapsRemaining = true;
                            Console.WriteLine("3 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderThreeLapsEstimate, 0, this));
                        }
                        else if (estBattLapsLeft == 2 && !this.playedTwoLapsRemaining)
                        {
                            this.playedTwoLapsRemaining = true;
                            Console.WriteLine("2 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderTwoLapsEstimate, 0, this));
                        }
                        else if (estBattLapsLeft == 1)
                        {
                            Console.WriteLine("1 lap of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderOneLapEstimate, 0, this));

                            // If we've not played the pit-now message, play it with a bit of a delay - should probably wait for sector3 here
                            // but i'd have to move some stuff around and I'm an idle fucker
                            if (!this.playedPitForBatteryNow)
                            {
                                this.playedPitForBatteryNow = true;
                                this.audioPlayer.playMessage(new QueuedMessage(PitStops.folderMandatoryPitStopsPitThisLap, 10, this));
                            }
                        }
                    }

                    // warnings for fixed time sessions - check every 5 seconds
                    else if (currentGameState.Now > this.nextBatteryStatusCheck
                        && currentGameState.SessionData.SessionNumberOfLaps <= 0
                        && currentGameState.SessionData.SessionTotalRunTime > 0.0f
                        && this.averageUsagePerMinute > 0.0f)
                    {
                        var battStatusMsg = string.Format("starting battery = {0}%,  windowed avg charge = {1}%,  previous lap avg charge = {2}%,  previous lap min charge = {3}%, current battery level = {4}%, usage per minute = {5}%",
                            this.initialBatteryChargePercentage.ToString("0.000"),
                            this.windowedAverageChargeLeft.ToString("0.000"),
                            prevLapStats.AverageBatteryPercentageLeft.ToString("0.000"),
                            prevLapStats.MinimumBatteryPercentageLeft.ToString("0.000"),
                            currentGameState.BatteryData.BatteryPercentageLeft.ToString("0.000"),
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
                                        this.audioPlayer.playMessage(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                        this.audioPlayer.playMessage(new QueuedMessage("Battery/estimate", MessageContents(
                                            Battery.folderWeEstimate, minutesLeft, Battery.folderMinutesRemaining), 0, this));
                                    }
                                    else
                                        this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceLowBattery, 0, this));
                                }
                                else
                                    this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfDistanceGoodBattery, 0, this));
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
                            this.audioPlayer.playMessage(new QueuedMessage("pit_for_vehicle_swap_now",
                                MessageContents(Battery.folderAboutToRunOut, PitStops.folderMandatoryPitStopsPitThisLap), 0, this));
                        }
                        if (estBattMinsLeft <= 2.0f && estBattMinsLeft > 1.8f && !this.playedTwoMinutesRemaining)
                        {
                            Console.WriteLine("Less than 2 mins of battery charge left, " + battStatusMsg);

                            this.playedTwoMinutesRemaining = true;
                            this.playedFiveMinutesRemaining = true;
                            this.playedTenMinutesRemaining = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderTwoMinutesBattery, 0, this));
                        }
                        else if (estBattMinsLeft <= 5.0f && estBattMinsLeft > 4.8f && !this.playedFiveMinutesRemaining)
                        {
                            Console.WriteLine("Less than 5 mins of battery charge left, " + battStatusMsg);

                            this.playedFiveMinutesRemaining = true;
                            this.playedTenMinutesRemaining = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderFiveMinutesBattery, 0, this));
                        }
                        else if (estBattMinsLeft <= 10.0f && estBattMinsLeft > 9.8f && !this.playedTenMinutesRemaining)
                        {
                            Console.WriteLine("Less than 10 mins of battery charge left, " + battStatusMsg);

                            this.playedTenMinutesRemaining = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderTenMinutesBattery, 0, this));
                        }
                        else if (!this.playedHalfBatteryChargeWarning && this.windowedAverageChargeLeft / this.initialBatteryChargePercentage <= 0.55f &&
                            this.windowedAverageChargeLeft / this.initialBatteryChargePercentage >= 0.45f)
                        {
                            Console.WriteLine("Less than 50% of battery charge left, " + battStatusMsg);

                            // warning message for battery left - these play as soon previous lap average charge drops below 1/2.
                            this.playedHalfBatteryChargeWarning = true;
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderHalfChargeWarning, 0, this));
                        }
                    }
                }
            }
        }

        public void reportBatteryStatus(Boolean allowNoDataMessage)
        {
            // TODO: report actual charge level first, always.
            var reportedRemaining = this.reportBatteryRemaining(allowNoDataMessage);

            // TODO: don't report avg use if we're running low already.
            // TODO: instead of reporting avg battery use, report battery usage for the last lap.  This is much more informative, because user can sense how settings/driving impact the drain.
            var reportedUse = this.reportBatteryUse();
            if (!reportedUse && !reportedRemaining && allowNoDataMessage)
                this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                this.reportBatteryStatus(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY));
            }
        }

        private bool reportBatteryUse()
        {
            var haveData = false;
            if (!this.initialized || this.averageUsagePerLap < 0.0f)
                return haveData;

            if (this.batteryUseActive && this.averageUsagePerLap > 0.0f)
            {
                // round to 1dp
                var meanUsePerLap = ((float)Math.Round(this.averageUsagePerLap * 10.0f)) / 10.0f;
                if (meanUsePerLap == 0.0f)
                {
                    // rounded battery use is < 0.1 litres per lap - can't really do anything with this.
                    return false;
                }

                // get the whole and fractional part (yeah, I know this is shit)
                var str = meanUsePerLap.ToString();
                var pointPosition = str.IndexOf('.');
                var wholePart = 0;
                var fractionalPart = 0;
                if (pointPosition > 0)
                {
                    wholePart = int.Parse(str.Substring(0, pointPosition));
                    fractionalPart = int.Parse(str[pointPosition + 1].ToString());
                }
                else
                    wholePart = (int)meanUsePerLap;

                if (meanUsePerLap > 0.0f)
                {
                    haveData = true;
                    if (fractionalPart > 0)
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/mean_use_per_lap",
                                MessageContents(wholePart, NumberReader.folderPoint, fractionalPart, Battery.folderPercentagePerLap), 0, null));
                    else
                        this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/mean_use_per_lap",
                                MessageContents(wholePart, Battery.folderPercentagePerLap), 0, null));
                }
            }

            return haveData;
        }

        private bool reportBatteryRemaining(bool allowNowDataMessage)
        {
            var haveData = false;
            if (this.windowedAverageChargeLeft < 0.0f)
                return haveData;  // Nothing we can do.

            if (!this.initialized  // Never initialized
                || (this.averageUsagePerLap < 0.0f && this.averageUsagePerMinute < 0.0f))  // or usage stats not available yet
            {
                // Handle no rich data available cases.
                if (!this.batteryUseActive && allowNowDataMessage)
                {
                    haveData = true;
                    this.audioPlayer.playMessageImmediately(new QueuedMessage(Battery.folderPlentyOfBattery, 0, null));
                }
                else if (this.windowedAverageChargeLeft >= 10.0f)
                {
                    haveData = true;
                    var messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Integer((int)windowedAverageChargeLeft, false));
                    messageFragments.Add(MessageFragment.Text(Battery.folderPercentRemaining));
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/level", messageFragments, 0, null));
                }
                else if (this.windowedAverageChargeLeft > 5.0f)
                {
                    haveData = true;
                    this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderLowBattery), 0, this));
                }
                else if (this.windowedAverageChargeLeft <= 2.0f)
                {
                    haveData = true;
                    this.audioPlayer.playMessage(new QueuedMessage("Battery/level", MessageContents(Battery.folderCriticalBattery), 0, this));
                }
                else if (this.windowedAverageChargeLeft > 0)
                {
                    haveData = true;
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/level",
                            MessageContents(Battery.folderAboutToRunOut), 0, null));
                }

                return haveData;
            }

            if (this.sessionHasFixedNumberOfLaps && this.averageUsagePerLap > 0.0f)
            {
                haveData = true;
                var lapsOfBatteryChargeLeft = (int)Math.Floor(this.windowedAverageChargeLeft / this.averageUsagePerLap);
                if (lapsOfBatteryChargeLeft < 0)
                {
                    // nothing to report (pit stop reset on a separate thread)
                    haveData = false;
                }
                else if (lapsOfBatteryChargeLeft <= 1)
                {
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate",
                        MessageContents(Battery.folderAboutToRunOut), 0, null));
                }
                else
                {
                    var messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(Battery.folderWeEstimate));
                    messageFragments.Add(MessageFragment.Integer(lapsOfBatteryChargeLeft, false));
                    messageFragments.Add(MessageFragment.Text(Battery.folderLapsRemaining));
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", messageFragments, 0, null));
                }
            }
            else if (this.averageUsagePerMinute > 0.0f) // Timed race.
            {
                haveData = true;
                var minutesOfBatteryChargeLeft = (int)Math.Floor(windowedAverageChargeLeft / this.averageUsagePerMinute);
                if (minutesOfBatteryChargeLeft < 0)
                {
                    // nothing to report (pit stop reset on a separate thread)
                    haveData = false;
                }
                else if (minutesOfBatteryChargeLeft <= 1)
                {
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate",
                        MessageContents(Battery.folderAboutToRunOut), 0, null));
                }
                else
                {
                    var messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(Battery.folderWeEstimate));
                    messageFragments.Add(MessageFragment.Integer(minutesOfBatteryChargeLeft, false));
                    messageFragments.Add(MessageFragment.Text(Battery.folderMinutesRemaining));
                    this.audioPlayer.playMessageImmediately(new QueuedMessage("Battery/estimate", messageFragments, 0, null));
                }
            }

            return haveData;
        }
    }
}
