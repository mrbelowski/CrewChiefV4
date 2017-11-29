using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class Fuel : AbstractEvent
    {
        public static String folderOneLapEstimate = "fuel/one_lap_fuel";

        public static String folderTwoLapsEstimate = "fuel/two_laps_fuel";

        public static String folderThreeLapsEstimate = "fuel/three_laps_fuel";

        public static String folderFourLapsEstimate = "fuel/four_laps_fuel";

        public static String folderHalfDistanceGoodFuel = "fuel/half_distance_good_fuel";

        public static String folderHalfDistanceLowFuel = "fuel/half_distance_low_fuel";

        public static String folderHalfTankWarning = "fuel/half_tank_warning";

        public static String folderTenMinutesFuel = "fuel/ten_minutes_fuel";

        public static String folderTwoMinutesFuel = "fuel/two_minutes_fuel";

        public static String folderFiveMinutesFuel = "fuel/five_minutes_fuel";

        public static String folderMinutesRemaining = "fuel/minutes_remaining";

        public static String folderLapsRemaining = "fuel/laps_remaining";

        public static String folderWeEstimate = "fuel/we_estimate";

        public static String folderPlentyOfFuel = "fuel/plenty_of_fuel";

        public static String folderLitresRemaining = "fuel/litres_remaining";

        public static String folderOneLitreRemaining = "fuel/one_litre_remaining";

        public static String folderAboutToRunOut = "fuel/about_to_run_out";

        public static String folderLitresPerLap = "fuel/litres_per_lap";

        public static String folderLitres = "fuel/litres";

        private float averageUsagePerLap;

        private float averageUsagePerMinute;

        // fuel in tank 15 seconds after game start
        private float initialFuelLevel;

        private int halfDistance;

        private float halfTime;

        private Boolean playedHalfTankWarning;

        private Boolean initialised;

        private Boolean playedHalfTimeFuelEstimate;

        private Boolean played1LitreWarning;

        private Boolean played2LitreWarning;
        
        // base fuel use by lap estimates on the last 3 laps
        private int fuelUseByLapsWindowLength = 3;
        
        // base fuel use by time estimates on the last 6 samples (6 minutes)
        private int fuelUseByTimeWindowLength = 6;

        private List<float> fuelLevelWindowByLap = new List<float>();

        private List<float> fuelLevelWindowByTime = new List<float>();

        private float gameTimeAtLastFuelWindowUpdate;

        private Boolean playedPitForFuelNow;

        private Boolean playedTwoMinutesRemaining;

        private Boolean playedFiveMinutesRemaining;

        private Boolean playedTenMinutesRemaining;

        private Boolean fuelUseActive;

        // check fuel use every 60 seconds
        private int fuelUseSampleTime = 60;

        private float currentFuel = -1;

        private float gameTimeWhenFuelWasReset = 0;

        private Boolean enableFuelMessages = UserSettings.GetUserSettings().getBoolean("enable_fuel_messages");

        private Boolean delayResponses = UserSettings.GetUserSettings().getBoolean("enable_delayed_responses");

        private Boolean hasBeenRefuelled = false;

        // checking if we need to read fuel messages involves a bit of arithmetic and stuff, so only do this every few seconds
        private DateTime nextFuelStatusCheck = DateTime.MinValue;

        private TimeSpan fuelStatusCheckInterval = TimeSpan.FromSeconds(5);

        private Boolean sessionHasFixedNumberOfLaps = false;

        // count laps separately for fuel so we always count incomplete and invalid laps
        private int lapsCompletedSinceFuelReset = 0;

        public Fuel(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            initialFuelLevel = 0;
            averageUsagePerLap = 0;
            halfDistance = -1;
            playedHalfTankWarning = false;
            initialised = false;
            halfTime = -1;
            playedHalfTimeFuelEstimate = false;
            fuelLevelWindowByLap = new List<float>();
            fuelLevelWindowByTime = new List<float>();
            gameTimeAtLastFuelWindowUpdate = 0;
            averageUsagePerMinute = 0;
            playedPitForFuelNow = false;
            playedFiveMinutesRemaining = false;
            playedTenMinutesRemaining = false;
            playedTwoMinutesRemaining = false;
            played1LitreWarning = false;
            played2LitreWarning = false;
            currentFuel = 0;
            fuelUseActive = false;
            gameTimeWhenFuelWasReset = 0;
            hasBeenRefuelled = false;
            nextFuelStatusCheck = DateTime.MinValue;
            sessionHasFixedNumberOfLaps = false;
            lapsCompletedSinceFuelReset = 0;
        }

        // fuel not implemented for HotLap modes
        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.FUEL))
            {
                return;
            }
            fuelUseActive = currentGameState.FuelData.FuelUseActive;
            // if the fuel level has increased, don't trigger
            if (currentFuel > -1 && currentFuel < currentGameState.FuelData.FuelLeft)
            {
                currentFuel = currentGameState.FuelData.FuelLeft;
                return;
            }
            currentFuel = currentGameState.FuelData.FuelLeft;
            // only track fuel data after the session has settled down
            if (fuelUseActive && currentGameState.SessionData.SessionRunningTime > 15 &&
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
                    // don't process fuel data in prac and qual until we're actually moving:
                    currentGameState.PositionAndMotionData.CarSpeed > 10)))
            {    
                if (!initialised ||
                    // fuel has increased by at least 1 litre - we only check against the time window here
                    (fuelLevelWindowByTime.Count() > 0 && fuelLevelWindowByTime[0] > 0 && currentGameState.FuelData.FuelLeft > fuelLevelWindowByTime[0] + 1) ||
                    (previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane))
                {
                    // first time in, fuel has increased, or pit exit so initialise our internal state. Note we don't blat the average use data - 
                    // this will be replaced when we get our first data point but it's still valid until we do.
                    fuelLevelWindowByTime = new List<float>();
                    fuelLevelWindowByLap = new List<float>();
                    fuelLevelWindowByTime.Add(currentGameState.FuelData.FuelLeft);
                    fuelLevelWindowByLap.Add(currentGameState.FuelData.FuelLeft);
                    initialFuelLevel = currentGameState.FuelData.FuelLeft;
                    gameTimeWhenFuelWasReset = currentGameState.SessionData.SessionRunningTime;
                    gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                    playedPitForFuelNow = false;
                    playedFiveMinutesRemaining = false;
                    playedTenMinutesRemaining = false;
                    playedTwoMinutesRemaining = false;
                    played1LitreWarning = false;
                    played2LitreWarning = false;
                    lapsCompletedSinceFuelReset = 0;
                    // if this is the first time we've initialised the fuel stats (start of session), get the half way point of this session
                    if (!initialised)
                    {
                        if (currentGameState.SessionData.SessionNumberOfLaps > 1)
                        {
                            sessionHasFixedNumberOfLaps = true;
                            if (halfDistance == -1)
                            {
                                halfDistance = (int) Math.Ceiling(currentGameState.SessionData.SessionNumberOfLaps / 2f);
                            }
                        }
                        else if (currentGameState.SessionData.SessionTotalRunTime > 0)
                        {
                            sessionHasFixedNumberOfLaps = false;
                            if (halfTime == -1)
                            {
                                halfTime = (int) Math.Ceiling(currentGameState.SessionData.SessionTotalRunTime / 2f);
                            }
                        }
                    }
                    Console.WriteLine("Fuel level initialised, initialFuelLevel = " + initialFuelLevel + ", halfDistance = " + halfDistance + " halfTime = " + halfTime);
                    initialised = true;                    
                }
                if (initialised)
                {
                    if (currentGameState.SessionData.IsNewLap)
                    {
                        lapsCompletedSinceFuelReset++;
                        // completed a lap, so store the fuel left at this point:
                        fuelLevelWindowByLap.Insert(0, currentGameState.FuelData.FuelLeft);
                        // if we've got fuelUseByLapsWindowLength + 1 samples (note we initialise the window data with initialFuelLevel so we always
                        // have one extra), get the average difference between each pair of values

                        // only do this if we have a full window of data + one extra start point
                        if (fuelLevelWindowByLap.Count > fuelUseByLapsWindowLength)
                        {
                            averageUsagePerLap = 0;
                            for (int i = 0; i < fuelUseByLapsWindowLength; i++)
                            {
                                averageUsagePerLap += (fuelLevelWindowByLap[i + 1] - fuelLevelWindowByLap[i]);
                            }
                            averageUsagePerLap = averageUsagePerLap / fuelUseByLapsWindowLength;
                            Console.WriteLine("fuel use per lap (windowed calc) = " + averageUsagePerLap + " fuel left = " + currentGameState.FuelData.FuelLeft);
                        }
                        else
                        {
                            averageUsagePerLap = (initialFuelLevel - currentGameState.FuelData.FuelLeft) / lapsCompletedSinceFuelReset;
                            Console.WriteLine("fuel use per lap (basic calc) = " + averageUsagePerLap + " fuel left = " + currentGameState.FuelData.FuelLeft);
                        }
                    }
                    if (currentGameState.SessionData.SessionRunningTime > gameTimeAtLastFuelWindowUpdate + fuelUseSampleTime)
                    {
                        // it's x minutes since the last fuel window check
                        gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                        fuelLevelWindowByTime.Insert(0, currentGameState.FuelData.FuelLeft);
                        // if we've got fuelUseByTimeWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                        // have one extra), get the average difference between each pair of values

                        // only do this if we have a full window of data + one extra start point
                        if (fuelLevelWindowByTime.Count > fuelUseByTimeWindowLength)
                        {
                            averageUsagePerMinute = 0;
                            for (int i = 0; i < fuelUseByTimeWindowLength; i++)
                            {
                                averageUsagePerMinute += (fuelLevelWindowByTime[i + 1] - fuelLevelWindowByTime[i]);
                            }
                            averageUsagePerMinute = 60 * averageUsagePerMinute / (fuelUseByTimeWindowLength * fuelUseSampleTime);
                            Console.WriteLine("fuel use per minute (windowed calc) = " + averageUsagePerMinute + " fuel left = " + currentGameState.FuelData.FuelLeft);
                        }
                        else
                        {
                            averageUsagePerMinute = 60 * (initialFuelLevel - currentGameState.FuelData.FuelLeft) / (gameTimeAtLastFuelWindowUpdate - gameTimeWhenFuelWasReset);
                            Console.WriteLine("fuel use per minute (basic calc) = " + averageUsagePerMinute + " fuel left = " + currentGameState.FuelData.FuelLeft);
                        }
                    }

                    // warnings for particular fuel levels
                    if (enableFuelMessages)
                    {
                        if (currentFuel <= 2 && !played2LitreWarning)
                        {
                            played2LitreWarning = true;
                            audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(2, folderLitresRemaining), 0, this));
                        }
                        else if (currentFuel <= 1 && !played1LitreWarning)
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
                    }
                }
            }
        }

        private Boolean reportFuelConsumption()
        {
            Boolean haveData = false;
            if (fuelUseActive && averageUsagePerLap > 0)
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
            if (fuelUseActive && averageUsagePerLap > 0)
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
            if (fuelUseActive && averageUsagePerMinute > 0)
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
            if (initialised && currentFuel > -1)
            {
                if (sessionHasFixedNumberOfLaps && averageUsagePerLap > 0)
                {
                    haveData = true;
                    int lapsOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerLap);
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
                    int minutesOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerMinute);
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
                if (!fuelUseActive && allowNowDataMessage)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null));
                }
                else if (currentFuel >= 2)
                {
                    haveData = true;
                    List<MessageFragment> messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Integer((int)currentFuel, false));
                    messageFragments.Add(MessageFragment.Text(folderLitresRemaining));
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level", messageFragments, 0, null));
                }
                else if (currentFuel >= 1)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents(folderOneLitreRemaining), 0, null));
                }
                else if (currentFuel > 0)
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
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.FUEL))
            {
                if (allowNoDataMessage)
                {
                    this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }

                return;
            }

            Boolean reportedRemaining = reportFuelRemaining(allowNoDataMessage);
            Boolean reportedConsumption = reportFuelConsumption();
            if (!reportedConsumption && !reportedRemaining && allowNoDataMessage)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
            }
        }

        public override void respond(String voiceMessage)
        {
            if (!GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.FUEL))
            {
                this.audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));

                return;
            }

            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_FUEL_USAGE))
            {
                if (!reportFuelConsumption())
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));                    
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_FUEL_LEVEL))
            {
                if (!fuelUseActive)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                else if (currentFuel >= 2)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents((int)currentFuel, folderLitresRemaining), 0, null));
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
        }
    }
}
