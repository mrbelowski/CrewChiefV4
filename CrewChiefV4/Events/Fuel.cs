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

        private int fuelUseWindowLength = 3;

        private List<float> fuelUseWindow = new List<float>();

        private float gameTimeAtLastFuelWindowUpdate;

        private Boolean playedPitForFuelNow;

        private Boolean playedTwoMinutesRemaining;

        private Boolean playedFiveMinutesRemaining;

        private Boolean playedTenMinutesRemaining;

        private Boolean fuelUseActive;

        // check fuel use every 90 seconds
        private int fuelUseSampleTime = 90;

        private float currentFuel = -1;

        private float gameTimeWhenFuelWasReset = 0;

        private int lapsCompletedWhenFuelWasReset = 0;

        private Boolean enableFuelMessages = UserSettings.GetUserSettings().getBoolean("enable_fuel_messages");

        private Boolean hasBeenRefuelled = false;

        private List<float> usagePerLap = new List<float>();

        private float fuelAtStartOfLastLap = 0;

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
            fuelUseWindow = new List<float>();
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
            lapsCompletedWhenFuelWasReset = 0;
            hasBeenRefuelled = false;
            usagePerLap.Clear();
            fuelAtStartOfLastLap = 0;
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
            // if the fuel level has increased, don't trigger
            if (currentFuel > -1 && currentFuel < currentGameState.FuelData.FuelLeft)
            {
                currentFuel = currentGameState.FuelData.FuelLeft;
                return;
            }

            fuelUseActive = currentGameState.FuelData.FuelUseActive;
            currentFuel = currentGameState.FuelData.FuelLeft;
            if (fuelUseActive && ((currentGameState.SessionData.SessionType == SessionType.Race &&
                ((currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow) || 
                    currentGameState.SessionData.SessionPhase == SessionPhase.Checkered)) ||
                 ((currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice || 
                    currentGameState.SessionData.SessionType == SessionType.HotLap) &&
                    ((currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow) || 
                        currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) &&
                    currentGameState.SessionData.LapTimeCurrent > 0)))
            {               
                // To get the initial fuel, wait for 15 seconds
                if (currentGameState.SessionData.SessionRunningTime > 15)
                {
                    if (!initialised || (fuelUseWindow.Count() > 0 && fuelUseWindow[0] > 0 && fuelUseWindow[0] < currentGameState.FuelData.FuelLeft))
                    {
                        fuelUseWindow = new List<float>();
                        fuelUseWindow.Add(currentGameState.FuelData.FuelLeft);
                        initialFuelLevel = currentGameState.FuelData.FuelLeft;
                        lapsCompletedWhenFuelWasReset = currentGameState.SessionData.CompletedLaps;
                        gameTimeWhenFuelWasReset = currentGameState.SessionData.SessionRunningTime;
                        gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                        playedPitForFuelNow = false;
                        playedFiveMinutesRemaining = false;
                        playedTenMinutesRemaining = false;
                        playedTwoMinutesRemaining = false;
                        played1LitreWarning = false;
                        played2LitreWarning = false;
                        Console.WriteLine("Initial fuel level = " + initialFuelLevel);
                        if (!initialised)
                        {
                            initialised = true;
                            if (currentGameState.SessionData.SessionNumberOfLaps > 0)
                            {
                                if (halfDistance == -1)
                                {
                                    halfDistance = currentGameState.SessionData.SessionNumberOfLaps / 2;
                                }
                            }
                            else if (currentGameState.SessionData.SessionTotalRunTime > 0)
                            {
                                if (halfTime == -1)
                                {
                                    halfTime = currentGameState.SessionData.SessionTotalRunTime / 2;
                                    Console.WriteLine("Half time = " + halfTime);
                                }
                            }
                        }
                    }
                }
                if (initialised)
                {
                    if (currentGameState.SessionData.IsNewLap)
                    {
                        if (fuelAtStartOfLastLap > currentFuel)
                        {
                            usagePerLap.Add(fuelAtStartOfLastLap - currentFuel);                            
                        }
                        fuelAtStartOfLastLap = currentFuel;                        
                    }
                    if (enableFuelMessages && currentFuel <= 2 && !played2LitreWarning)
                    {
                        played2LitreWarning = true;
                        audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(2, folderLitresRemaining), 0, this));
                    }
                    else if (enableFuelMessages && currentFuel <= 1 && !played1LitreWarning)
                    {
                        played1LitreWarning = true;
                        audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(folderOneLitreRemaining), 0, this));
                    }
                }
                if (currentGameState.SessionData.IsNewLap && initialised && currentGameState.SessionData.CompletedLaps > lapsCompletedWhenFuelWasReset 
                    && (currentGameState.SessionData.SessionNumberOfLaps > 0 || currentGameState.SessionData.SessionType == SessionType.HotLap))
                {
                    // completed a lap, so store the fuel left at this point:
                    fuelUseWindow.Insert(0, currentGameState.FuelData.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with initialFuelLevel so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerLap = 0;
                        for (int i = 0; i < fuelUseWindowLength; i++)
                        {
                            averageUsagePerLap += (fuelUseWindow[i + 1] - fuelUseWindow[i]);
                        }
                        averageUsagePerLap = averageUsagePerLap / fuelUseWindowLength;
                    }
                    else
                    {
                        averageUsagePerLap = (initialFuelLevel - currentGameState.FuelData.FuelLeft) / (currentGameState.SessionData.CompletedLaps - lapsCompletedWhenFuelWasReset);
                    }
                    int estimatedFuelLapsLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerLap);
                    if (halfDistance != -1 && currentGameState.SessionData.SessionType == SessionType.Race && enableFuelMessages && currentGameState.SessionData.CompletedLaps == halfDistance)
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
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 4)
                    {
                        Console.WriteLine("4 laps fuel left, starting fuel = " + initialFuelLevel +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.playMessage(new QueuedMessage(folderFourLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 3)
                    {
                        Console.WriteLine("3 laps fuel left, starting fuel = " + initialFuelLevel +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.playMessage(new QueuedMessage(folderThreeLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 2)
                    {
                        Console.WriteLine("2 laps fuel left, starting fuel = " + initialFuelLevel +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 1)
                    {
                        Console.WriteLine("1 lap fuel left, starting fuel = " + initialFuelLevel +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.playMessage(new QueuedMessage(folderOneLapEstimate, 0, this));
                    }                    
                }
                else if (initialised && halfTime != -1 && currentGameState.SessionData.SessionNumberOfLaps <= 0 && !playedHalfTimeFuelEstimate &&
                    currentGameState.SessionData.SessionTimeRemaining <= halfTime && currentGameState.SessionData.SessionTimeRemaining > halfTime - 30 && averageUsagePerMinute > 0)
                {
                    Console.WriteLine("Half race distance. Fuel in tank = " + currentGameState.FuelData.FuelLeft + ", average usage per minute = " + averageUsagePerMinute);
                    playedHalfTimeFuelEstimate = true;
                    if (currentGameState.SessionData.SessionType == SessionType.Race && enableFuelMessages)
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
                else if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && currentGameState.SessionData.SessionTotalRunTime > 0 &&
                    currentGameState.SessionData.SessionRunningTime > gameTimeAtLastFuelWindowUpdate + fuelUseSampleTime)
                {
                    // it's x minutes since the last fuel window check
                    gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                    fuelUseWindow.Insert(0, currentGameState.FuelData.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerMinute = 0;
                        for (int i = 0; i < fuelUseWindowLength; i++)
                        {
                            averageUsagePerMinute += (fuelUseWindow[i + 1] - fuelUseWindow[i]);
                        }
                        averageUsagePerMinute = 60 * averageUsagePerMinute / (fuelUseWindowLength * fuelUseSampleTime);
                        Console.WriteLine("fuel use per minute (windowed calc) = " + averageUsagePerMinute + " fuel left = " + currentGameState.FuelData.FuelLeft);
                    }
                    else
                    {
                        averageUsagePerMinute = 60 * (initialFuelLevel - currentGameState.FuelData.FuelLeft) / (gameTimeAtLastFuelWindowUpdate - gameTimeWhenFuelWasReset);
                        Console.WriteLine("fuel use per minute (basic calc) = " + averageUsagePerMinute + " fuel left = " + currentGameState.FuelData.FuelLeft);
                    }
                }
                if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && currentGameState.SessionData.SessionTotalRunTime > 0 && averageUsagePerMinute > 0)
                {
                    float estimatedFuelMinutesLeft = currentGameState.FuelData.FuelLeft / averageUsagePerMinute;
                    if (enableFuelMessages && estimatedFuelMinutesLeft < 1.5 && !playedPitForFuelNow)
                    {
                        playedPitForFuelNow = true;
                        playedTwoMinutesRemaining = true;
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.playMessage(new QueuedMessage("pit_for_fuel_now", 
                            MessageContents(folderAboutToRunOut, MandatoryPitStops.folderMandatoryPitStopsPitThisLap), 0, this));
                    } if (enableFuelMessages && estimatedFuelMinutesLeft <= 2 && estimatedFuelMinutesLeft > 1.8 && !playedTwoMinutesRemaining)
                    {
                        playedTwoMinutesRemaining = true;
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.playMessage(new QueuedMessage(folderTwoMinutesFuel, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelMinutesLeft <= 5 && estimatedFuelMinutesLeft > 4.8 && !playedFiveMinutesRemaining)
                    {
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.playMessage(new QueuedMessage(folderFiveMinutesFuel, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelMinutesLeft <= 10 && estimatedFuelMinutesLeft > 9.8 && !playedTenMinutesRemaining)
                    {
                        playedTenMinutesRemaining = true;
                        audioPlayer.playMessage(new QueuedMessage(folderTenMinutesFuel, 0, this));

                    }
                    else if (enableFuelMessages && initialised && !playedHalfTankWarning && currentGameState.FuelData.FuelLeft / initialFuelLevel <= 0.50 && !hasBeenRefuelled)
                    {
                        // warning message for fuel left - these play as soon as the fuel reaches 1/2 tank left
                        playedHalfTankWarning = true;
                        audioPlayer.playMessage(new QueuedMessage(folderHalfTankWarning, 0, this));
                    }
                }
            }
        }

        private Boolean reportFuelConsumption()
        {
            Boolean haveData = false;
            if (fuelUseActive && usagePerLap.Count > 0)
            {
                // round to 1dp
                float meanUsePerLap = ((float)Math.Round(usagePerLap.Average() * 10f)) / 10f;
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

        private Boolean reportFuelRemaining()
        {
            Boolean haveData = false;
            if (initialised && currentFuel > -1)
            {
                if (averageUsagePerLap > 0)
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
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/estimate",
                            MessageContents(folderWeEstimate, lapsOfFuelLeft, folderLapsRemaining), 0, null));
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
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/estimate",
                            MessageContents(folderWeEstimate, minutesOfFuelLeft, folderMinutesRemaining), 0, null));
                    }                    
                }
            }
            if (!haveData)
            {
                if (!fuelUseActive)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null));
                }
                else if (currentFuel >= 2)
                {
                    haveData = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents((int)currentFuel, folderLitresRemaining), 0, null));
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

        public void reportFuelStatus()
        {            
            Boolean reportedRemaining = reportFuelRemaining();
            Boolean reportedConsumption = reportFuelConsumption();
            if (!reportedConsumption && !reportedRemaining)
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
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_FUEL))
            {
                reportFuelStatus();
            }
        }
    }
}
