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

        class BatteryStatsEntry
        {
            internal float AverageBatteryPercentageLeft = -1.0f;
            internal float MinimumBatteryPercentageLeft = float.MaxValue;
        };

        bool sessionInitialized = false;

        // Per lap battery stats.  Might not be necessary in a long run, but I'd like to have this in order to get better understanding.
        List<BatteryStatsEntry> batteryStats = new List<BatteryStatsEntry>();

        private int currLapNumBatteryMeasurements = 0;
        private float currLapBatteryPercentageLeftAccumulator = 0.0f;
        private float currLapMinBatteryLeft = float.MaxValue;

        bool batteryUseActive = false;
        private float gameTimeWhenSessionInitialized = float.MinValue;
        private bool sessionHasFixedNumberOfLaps = false;
        private int halfDistance = -1;
        private int halfTime = -1;

        public Battery(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
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

            float currBattLeftPct = currentGameState.BatteryData.BatteryPercentageLeft;

            // Only track fuel data after the session has settled down
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
                    // Don't process fuel data in prac and qual until we're actually moving:
                    currentGameState.PositionAndMotionData.CarSpeed > 10)))
            {    
                if (!this.sessionInitialized)  // NOTE: Or, if vehicle swap ever allowed, reinitilize.
                {
                    this.clearState();
                    this.gameTimeWhenSessionInitialized = currentGameState.SessionData.SessionRunningTime;

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

                    Console.WriteLine("Battery use tracking initilized: halfDistance = " + this.halfDistance + " halfTime = " + this.halfTime);
                    this.sessionInitialized = true;
                }

                if (currentGameState.SessionData.IsNewLap
                    && this.currLapNumBatteryMeasurements > 0)
                {
                    this.batteryStats.Add(new BatteryStatsEntry()
                    {
                        AverageBatteryPercentageLeft = this.currLapBatteryPercentageLeftAccumulator / this.currLapNumBatteryMeasurements,
                        MinimumBatteryPercentageLeft = this.currLapMinBatteryLeft
                    });

                    Console.WriteLine("Last lap average battery left percentage: "
                        + (this.currLapBatteryPercentageLeftAccumulator / this.currLapNumBatteryMeasurements).ToString("0.000")
                        + "%.  Min percentage: " + this.currLapMinBatteryLeft.ToString("0.000"));

                    this.currLapBatteryPercentageLeftAccumulator = 0.0f;
                    this.currLapNumBatteryMeasurements = 0;
                    this.currLapMinBatteryLeft = float.MaxValue;
                }

                this.currLapBatteryPercentageLeftAccumulator += currBattLeftPct;
                ++this.currLapNumBatteryMeasurements;

                if (this.currLapMinBatteryLeft > currBattLeftPct)
                    this.currLapMinBatteryLeft = currBattLeftPct;

                // Warnings for particular fuel levels
                if (this.enableBatteryMessages)
                {
                    /*if (currentBatteryPercentage <= 2 && !played2LitreWarning)
                    {
                        played2LitreWarning = true;
                        audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(2, folderLitresRemaining), 0, this));
                    }
                    else if (currentBatteryPercentage <= 1 && !played1LitreWarning)
                    {
                        played1LitreWarning = true;
                        audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(folderOneLitreRemaining), 0, this));
                    }
                    
                    // warnings for fixed lap sessions
                    if (currentGameState.SessionData.IsNewLap && averageUsagePerLap > 0 &&
                        (currentGameState.SessionData.SessionNumberOfLaps > 0 || currentGameState.SessionData.SessionType == SessionType.HotLap) &&
                        lapsCompletedSinceFuelReset > 0)
                    {
                        int estimatedFuelLapsLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerLap);
                        if (halfDistance != -1 && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps == halfDistance)
                        {
                            if (estimatedFuelLapsLeft < halfDistance && currentGameState.FuelData.FuelLeft / initialFuelLevel < 0.6)
                            {
                                if (currentGameState.PitData.IsRefuellingAllowed)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                    audioPlayer.playMessage(new QueuedMessage("Fuel/estimate", MessageContents(folderWeEstimate, estimatedFuelLapsLeft, folderLapsRemaining), 0, this));
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderHalfDistanceLowFuel, 0, this));
                                }
                            }
                            else
                            {
                                audioPlayer.playMessage(new QueuedMessage(folderHalfDistanceGoodFuel, 0, this));
                            }
                        }
                        else if (estimatedFuelLapsLeft == 4)
                        {
                            Console.WriteLine("4 laps fuel left, starting fuel = " + initialFuelLevel +
                                    ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                            audioPlayer.playMessage(new QueuedMessage(folderFourLapsEstimate, 0, this));
                        }
                        else if (estimatedFuelLapsLeft == 3)
                        {
                            Console.WriteLine("3 laps fuel left, starting fuel = " + initialFuelLevel +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                            audioPlayer.playMessage(new QueuedMessage(folderThreeLapsEstimate, 0, this));
                        }
                        else if (estimatedFuelLapsLeft == 2)
                        {
                            Console.WriteLine("2 laps fuel left, starting fuel = " + initialFuelLevel +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                            audioPlayer.playMessage(new QueuedMessage(folderTwoLapsEstimate, 0, this));
                        }
                        else if (estimatedFuelLapsLeft == 1)
                        {
                            Console.WriteLine("1 lap fuel left, starting fuel = " + initialFuelLevel +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                            audioPlayer.playMessage(new QueuedMessage(folderOneLapEstimate, 0, this));
                            // if we've not played the pit-now message, play it with a bit of a delay - should probably wait for sector3 here
                            // but i'd have to move some stuff around and I'm an idle fucker
                            if (!playedPitForFuelNow)
                            {
                                playedPitForFuelNow = true;
                                audioPlayer.playMessage(new QueuedMessage(PitStops.folderMandatoryPitStopsPitThisLap, 10, this));
                            }
                        }
                    }

                    // warnings for fixed time sessions - check every 5 seconds
                    else if (currentGameState.Now > nextFuelStatusCheck &&
                        currentGameState.SessionData.SessionNumberOfLaps <= 0 && currentGameState.SessionData.SessionTotalRunTime > 0 && averageUsagePerMinute > 0)
                    {
                        nextFuelStatusCheck = currentGameState.Now.Add(fuelStatusCheckInterval);
                        if (halfTime != -1 && !playedHalfTimeFuelEstimate && currentGameState.SessionData.SessionTimeRemaining <= halfTime &&
                            currentGameState.SessionData.SessionTimeRemaining > halfTime - 30)
                        {
                            Console.WriteLine("Half race distance. Fuel in tank = " + currentGameState.FuelData.FuelLeft + ", average usage per minute = " + averageUsagePerMinute);
                            playedHalfTimeFuelEstimate = true;
                            if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                if (averageUsagePerMinute * halfTime / 60 > currentGameState.FuelData.FuelLeft && currentGameState.FuelData.FuelLeft / initialFuelLevel < 0.6)
                                {
                                    if (currentGameState.PitData.IsRefuellingAllowed)
                                    {
                                        int minutesLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerMinute);
                                        audioPlayer.playMessage(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                        audioPlayer.playMessage(new QueuedMessage("Fuel/estimate", MessageContents(
                                            folderWeEstimate, minutesLeft, folderMinutesRemaining), 0, this));
                                    }
                                    else
                                    {
                                        audioPlayer.playMessage(new QueuedMessage(folderHalfDistanceLowFuel, 0, this));
                                    }
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderHalfDistanceGoodFuel, 0, this));
                                }
                            }
                        }

                        float estimatedFuelMinutesLeft = currentGameState.FuelData.FuelLeft / averageUsagePerMinute;
                        if (estimatedFuelMinutesLeft < 1.5 && !playedPitForFuelNow)
                        {
                            playedPitForFuelNow = true;
                            playedTwoMinutesRemaining = true;
                            playedFiveMinutesRemaining = true;
                            playedTenMinutesRemaining = true;
                            audioPlayer.playMessage(new QueuedMessage("pit_for_fuel_now",
                                MessageContents(folderAboutToRunOut, PitStops.folderMandatoryPitStopsPitThisLap), 0, this));
                        }
                        if (estimatedFuelMinutesLeft <= 2 && estimatedFuelMinutesLeft > 1.8 && !playedTwoMinutesRemaining)
                        {
                            playedTwoMinutesRemaining = true;
                            playedFiveMinutesRemaining = true;
                            playedTenMinutesRemaining = true;
                            audioPlayer.playMessage(new QueuedMessage(folderTwoMinutesFuel, 0, this));
                        }
                        else if (estimatedFuelMinutesLeft <= 5 && estimatedFuelMinutesLeft > 4.8 && !playedFiveMinutesRemaining)
                        {
                            playedFiveMinutesRemaining = true;
                            playedTenMinutesRemaining = true;
                            audioPlayer.playMessage(new QueuedMessage(folderFiveMinutesFuel, 0, this));
                        }
                        else if (estimatedFuelMinutesLeft <= 10 && estimatedFuelMinutesLeft > 9.8 && !playedTenMinutesRemaining)
                        {
                            playedTenMinutesRemaining = true;
                            audioPlayer.playMessage(new QueuedMessage(folderTenMinutesFuel, 0, this));

                        }
                        else if (!playedHalfTankWarning && currentGameState.FuelData.FuelLeft / initialFuelLevel <= 0.55 && 
                            currentGameState.FuelData.FuelLeft / initialFuelLevel >= 0.45 && !hasBeenRefuelled)
                        {
                            // warning message for fuel left - these play as soon as the fuel reaches 1/2 tank left
                            playedHalfTankWarning = true;
                            audioPlayer.playMessage(new QueuedMessage(folderHalfTankWarning, 0, this));
                        }
                    }
                    */
                }
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
