using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

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

        private const float averagedChargeWindowTime = 30.0f;
        private List<BatteryWindowedStatsEntry> windowedBatteryStats = new List<BatteryWindowedStatsEntry>();

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
        private bool playedHalfBatteryChargeWarning = false;

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
            this.playedHalfBatteryChargeWarning = false;
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
                    // Not sure if stats should be cleared or not here.  Keep it around for now.
                    // this.batteryStats.Clear();
                    this.windowedBatteryStats.Clear();
                    this.currLapNumBatteryMeasurements = 0;
                    this.currLapBatteryPercentageLeftAccumulator = 0.0f;
                    this.currLapMinBatteryLeft = float.MaxValue;
                    this.playedPitForBatteryNow = false;
                    this.playedTwoMinutesRemaining = false;
                    this.playedFiveMinutesRemaining = false;
                    this.playedTenMinutesRemaining = false;

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

                    Console.WriteLine(string.Format("Last lap average battery left percentage: {0}%  min percentage: {1}%  curr percentage {2}%",
                        this.batteryStats.Last().AverageBatteryPercentageLeft.ToString("0.000"),
                        this.batteryStats.Last().MinimumBatteryPercentageLeft.ToString("0.000"),
                        currBattLeftPct.ToString("0.000")));

                    this.currLapBatteryPercentageLeftAccumulator = 0.0f;
                    this.currLapNumBatteryMeasurements = 0;
                    this.currLapMinBatteryLeft = float.MaxValue;

                    this.currLapBatteryUseSectorCheck = Utilities.random.Next(1, 3);
                }

                // Update this lap stats.
                this.currLapBatteryPercentageLeftAccumulator += currBattLeftPct;
                ++this.currLapNumBatteryMeasurements;

                this.currLapMinBatteryLeft = Math.Min(this.currLapMinBatteryLeft, currBattLeftPct);

                // Track windowed average charge level.
                this.windowedBatteryStats.Add(new BatteryWindowedStatsEntry()
                {
                    BatteryPercentageLeft = currBattLeftPct,
                    SessionRunningTime = currentGameState.SessionData.SessionRunningTime
                });

                // Remove records older than Battery.averagedChargeWindowTime.
                this.windowedBatteryStats.RemoveAll(e => (currentGameState.SessionData.SessionRunningTime - e.SessionRunningTime) > Battery.averagedChargeWindowTime);

                // NOTE: unlike fuel messages, here we process data on new sector and randomly in cetain sector.  This is to reduce message overload on the new lap.
                // Warnings for particular battery levels
                if (this.enableBatteryMessages
                    && currentGameState.SessionData.IsNewSector
                    && this.currLapBatteryUseSectorCheck == currentGameState.SessionData.SectorNumber
                    && this.batteryStats.Count > 0)
                {
                    var prevLapStats = this.batteryStats.Last();

                    // Get battery use per lap:
                    var averageUsagePerLap = (this.initialBatteryChargePercentage - prevLapStats.AverageBatteryPercentageLeft) / this.batteryStats.Count;

                    // Calculate per minute usage:
                    var batteryDrainSinceMonitoringStart = this.initialBatteryChargePercentage - prevLapStats.AverageBatteryPercentageLeft;
                    var averageUsagePerMinute = (batteryDrainSinceMonitoringStart / prevLapStats.SessionRunningTime) * 60.0f;

                    // Calculate windowed average charge level:
                    var windowedAverageChargeLeft = this.windowedBatteryStats.Average(e => e.BatteryPercentageLeft);

                    // Not sure about per level warnings, what is user supposed to do if his battery is at 5%?  Is such warning valuable?
                    // or your battery is running low?
                    /*if (currentFuel <= 2 && !played2LitreWarning)
                    {
                        played2LitreWarning = true;
                        audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(2, folderLitresRemaining), 0, this));
                    }
                    else if (currentFuel <= 1 && !played1LitreWarning)
                    {
                        played1LitreWarning = true;
                        audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(folderOneLitreRemaining), 0, this));
                    }*/

                    // Warnings for fixed lap sessions.
                    if (averageUsagePerLap > 0.0f
                        && (currentGameState.SessionData.SessionNumberOfLaps > 0 || currentGameState.SessionData.SessionType == SessionType.HotLap))
                    {
                        var battStatusMsg = string.Format("starting battery = {0}%,  windowed avg charge = {1}%  previous lap avg charge = {2}%,  previous lap min charge = {3}%, current battery level = {4}%, usage per lap = {5}%",
                            this.initialBatteryChargePercentage.ToString("0.000"),
                            windowedAverageChargeLeft.ToString("0.000"),
                            prevLapStats.AverageBatteryPercentageLeft.ToString("0.000"),
                            prevLapStats.MinimumBatteryPercentageLeft.ToString("0.000"),
                            currentGameState.BatteryData.BatteryPercentageLeft.ToString("0.000"),
                            averageUsagePerLap.ToString("0.000"));

                        var estBattLapsLeft = (int)Math.Floor(windowedAverageChargeLeft / averageUsagePerLap);
                        if (this.halfDistance != -1
                            && !this.playedHalfDistanceBatteryEstimate
                            && currentGameState.SessionData.SessionType == SessionType.Race
                            && currentGameState.SessionData.CompletedLaps == this.halfDistance)
                        {
                            Console.WriteLine("Half race distance, " + battStatusMsg);

                            this.playedHalfDistanceBatteryEstimate = true;

                            if (estBattLapsLeft < this.halfDistance
                                && windowedAverageChargeLeft / this.initialBatteryChargePercentage < 0.6f)
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
                        else if (estBattLapsLeft == 4)
                        {
                            Console.WriteLine("4 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderFourLapsEstimate, 0, this));
                        }
                        else if (estBattLapsLeft == 3)
                        {
                            Console.WriteLine("3 laps of battery charge left, " + battStatusMsg);
                            this.audioPlayer.playMessage(new QueuedMessage(Battery.folderThreeLapsEstimate, 0, this));
                        }
                        else if (estBattLapsLeft == 2)
                        {
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
                        && averageUsagePerMinute > 0.0f)
                    {
                        var battStatusMsg = string.Format("starting battery = {0}%,  windowed avg charge = {1}%,  previous lap avg charge = {2}%,  previous lap min charge = {3}%, current battery level = {4}%, usage per minute = {5}%",
                            this.initialBatteryChargePercentage.ToString("0.000"),
                            windowedAverageChargeLeft.ToString("0.000"),
                            prevLapStats.AverageBatteryPercentageLeft.ToString("0.000"),
                            prevLapStats.MinimumBatteryPercentageLeft.ToString("0.000"),
                            currentGameState.BatteryData.BatteryPercentageLeft.ToString("0.000"),
                            averageUsagePerMinute.ToString("0.000"));

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
                                if (averageUsagePerMinute * this.halfTime / 60.0f > windowedAverageChargeLeft
                                    && windowedAverageChargeLeft / this.initialBatteryChargePercentage < 0.6)
                                {
                                    if (currentGameState.PitData.IsElectricVehicleSwapAllowed)
                                    {
                                        var minutesLeft = (int)Math.Floor(prevLapStats.AverageBatteryPercentageLeft / averageUsagePerMinute);
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

                        var estBattMinsLeft = windowedAverageChargeLeft / averageUsagePerMinute;
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
                        else if (!this.playedHalfBatteryChargeWarning && windowedAverageChargeLeft / this.initialBatteryChargePercentage <= 0.55f &&
                            windowedAverageChargeLeft / this.initialBatteryChargePercentage >= 0.45f)
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
        // TODO: I think interesting stats would be:
        // all the same things as fuel +
        // for the last lap, we could report average charge level, and minimal charge level, which actually represents "worst case"

        public void reportBatteryStatus(Boolean allowNoDataMessage)
        {
            // TODO: Implement me :)
            audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
        }
        
        public override void respond(String voiceMessage)
        {
            // TODO: implement me :)
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                reportBatteryStatus(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BATTERY));
            }
        }


        /*
        private Boolean reportFuelConsumption()
        {
            Boolean haveData = false;
            if (batteryUseActive && averageUsagePerLap > 0)
            {
                // round to 1dp
                float meanUsePerLap = ((float)Math.Round(averageUsagePerLap * 10f)) / 10f;
                if (meanUsePerLap == 0)
                {
                    // rounded fuel use is < 0.1 litres per lap - can't really do anything with this.
                    return false;
                }
                // get the whole and fractional part (yeah, I know this is shit)
                String str = meanUsePerLap.ToString();
                int pointPosition = str.IndexOf('.');
                int wholePart = 0;
                int fractionalPart = 0;
                if (pointPosition > 0)
                {
                    wholePart = int.Parse(str.Substring(0, pointPosition));
                    fractionalPart = int.Parse(str[pointPosition + 1].ToString());
                }
                else
                {
                    wholePart = (int)meanUsePerLap;
                }
                if (meanUsePerLap > 0)
                {
                    haveData = true;
                    if (fractionalPart > 0)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/mean_use_per_lap",
                                MessageContents(wholePart, NumberReader.folderPoint, fractionalPart, folderLitresPerLap), 0, null));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/mean_use_per_lap",
                                MessageContents(wholePart, folderLitresPerLap), 0, null));
                    }
                }
            }
            return haveData;
        }

        private Boolean reportFuelConsumptionForLaps(int numberOfLaps)
        {
            Boolean haveData = false;
            if (batteryUseActive && averageUsagePerLap > 0)
            {
                // round up
                float totalUsage = (float)Math.Ceiling(averageUsagePerLap * numberOfLaps);
                if (totalUsage > 0)
                {
                    haveData = true;
                    // build up the message fragments the verbose way, so we can prevent the number reader from shortening hundreds to
                    // stuff like "one thirty two" - we always want "one hundred and thirty two"
                    List<MessageFragment> messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(folderWeEstimate));
                    messageFragments.Add(MessageFragment.Integer(Convert.ToInt32(totalUsage), false));
                    messageFragments.Add(MessageFragment.Text(folderLitres));
                    QueuedMessage fuelEstimateMessage = new QueuedMessage("Fuel/estimate",
                            messageFragments, 0, null);
                    // play this immediately or play "stand by", and queue it to be played in a few seconds
                    if (delayResponses && Utilities.random.Next(10) >= 2 && SoundCache.availableSounds.Contains(AudioPlayer.folderStandBy))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderStandBy, 0, null));
                        int secondsDelay = Math.Max(5, Utilities.random.Next(8));
                        audioPlayer.pauseQueue(secondsDelay);
                        fuelEstimateMessage.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (1000 * secondsDelay);
                        audioPlayer.playDelayedImmediateMessage(fuelEstimateMessage);
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(fuelEstimateMessage);
                    }
                }
            }
            return haveData;
        }
        private Boolean reportFuelConsumptionForTime(int hours, int minutes)
        {
            Boolean haveData = false;
            if (batteryUseActive && averageUsagePerMinute > 0)
            {
                int  timeToUse = (hours * 60) + minutes;
                // round up
                float totalUsage = ((float)Math.Ceiling(averageUsagePerMinute * timeToUse));
                if (totalUsage > 0)
                {
                    haveData = true;
                    // build up the message fragments the verbose way, so we can prevent the number reader from shortening hundreds to
                    // stuff like "one thirty two" - we always want "one hundred and thirty two"
                    List<MessageFragment> messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(folderWeEstimate));
                    messageFragments.Add(MessageFragment.Integer(Convert.ToInt32(totalUsage), false));
                    messageFragments.Add(MessageFragment.Text(folderLitres));
                    QueuedMessage fuelEstimateMessage = new QueuedMessage("Fuel/estimate",
                            messageFragments, 0, null);
                    // play this immediately or play "stand by", and queue it to be played in a few seconds
                    if (delayResponses && Utilities.random.Next(10) >= 2 && SoundCache.availableSounds.Contains(AudioPlayer.folderStandBy))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderStandBy, 0, null));
                        int secondsDelay = Math.Max(5, Utilities.random.Next(8));
                        audioPlayer.pauseQueue(secondsDelay);
                        fuelEstimateMessage.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (1000 * secondsDelay);
                        audioPlayer.playDelayedImmediateMessage(fuelEstimateMessage);
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(fuelEstimateMessage);
                    }
                }
            }
            return haveData;
        }
        private Boolean reportFuelRemaining(Boolean allowNowDataMessage)
        {
            Boolean haveData = false;
            if (initialised && currentBatteryPercentage > -1)
            {
                if (sessionHasFixedNumberOfLaps && averageUsagePerLap > 0)
                {
                    haveData = true;
                    int lapsOfFuelLeft = (int)Math.Floor(currentBatteryPercentage / averageUsagePerLap);
                    if (lapsOfFuelLeft <= 1)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/estimate",
                            MessageContents(folderAboutToRunOut), 0, null));
                    }
                    else
                    {
                        List<MessageFragment> messageFragments = new List<MessageFragment>();
                        messageFragments.Add(MessageFragment.Text(folderWeEstimate));
                        messageFragments.Add(MessageFragment.Integer(lapsOfFuelLeft, false));
                        messageFragments.Add(MessageFragment.Text(folderLapsRemaining));
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/estimate", messageFragments, 0, null));
                    }                    
                }
                else if (averageUsagePerMinute > 0)
                {
                    haveData = true;
                    int minutesOfFuelLeft = (int)Math.Floor(currentBatteryPercentage / averageUsagePerMinute);
                    if (minutesOfFuelLeft <= 1)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/estimate",
                            MessageContents(folderAboutToRunOut), 0, null));
                    }
                    else
                    {
                        List<MessageFragment> messageFragments = new List<MessageFragment>();
                        messageFragments.Add(MessageFragment.Text(folderWeEstimate));
                        messageFragments.Add(MessageFragment.Integer(minutesOfFuelLeft, false));
                        messageFragments.Add(MessageFragment.Text(folderMinutesRemaining));
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/estimate", messageFragments, 0, null));
                    }                    
                }
            }
            if (!haveData)
            {
                if (!batteryUseActive && allowNowDataMessage)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null));
                }
                else if (currentBatteryPercentage >= 2)
                {
                    haveData = true;
                    List<MessageFragment> messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Integer((int)currentBatteryPercentage, false));
                    messageFragments.Add(MessageFragment.Text(folderLitresRemaining));
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level", messageFragments, 0, null));
                }
                else if (currentBatteryPercentage >= 1)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents(folderOneLitreRemaining), 0, null));
                }
                else if (currentBatteryPercentage > 0)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                            MessageContents(folderAboutToRunOut), 0, null));
                }
            }
            return haveData;
        }

        public void reportFuelStatus(Boolean allowNoDataMessage)
        {            
            Boolean reportedRemaining = reportFuelRemaining(allowNoDataMessage);
            Boolean reportedConsumption = reportFuelConsumption();
            if (!reportedConsumption && !reportedRemaining && allowNoDataMessage)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_FUEL_USAGE))
            {
                if (!reportFuelConsumption())
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));                    
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_FUEL_LEVEL))
            {
                if (!batteryUseActive)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                else if (currentBatteryPercentage >= 2)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents((int)currentBatteryPercentage, folderLitresRemaining), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderAboutToRunOut, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_FUEL) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                reportFuelStatus(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_FUEL));
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CALCULATE_FUEL_FOR))
            {
                int unit = 0;
                foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.numberToNumber)
                {
                    foreach (String numberStr in entry.Key)
                    {
                        if (voiceMessage.Contains(" " + numberStr + " "))
                        {
                            unit = entry.Value;
                            break;
                        }
                    }
                }
                if (unit == 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDidntUnderstand, 0, null));
                    return;
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.LAP) || SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.LAPS))
                {
                    if (!reportFuelConsumptionForLaps(unit))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    } 
                }
                else if(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.MINUTE) || SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.MINUTES))
                {
                    if (!reportFuelConsumptionForTime(0, unit))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    } 
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOUR) || SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOURS))
                {
                    if (!reportFuelConsumptionForTime(unit, 0))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    }
                }
            }
        }*/
    }
}
