using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;

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

        public static String folderAboutToRunOut = "fuel/about_to_run_out";

        private float averageUsagePerLap;

        private float averageUsagePerMinute;

        // fuel in tank 15 seconds after game start
        private float initialFuelLevel;

        private int halfDistance;

        private float halfTime;

        private Boolean playedHalfTankWarning;

        private Boolean initialised;

        private Boolean playedHalfTimeFuelEstimate;

        private int fuelUseWindowLength = 3;

        private List<float> fuelUseWindow;

        private float gameTimeAtLastFuelWindowUpdate;

        private Boolean playedPitForFuelNow;

        private Boolean playedTwoMinutesRemaining;

        private Boolean playedFiveMinutesRemaining;

        private Boolean playedTenMinutesRemaining;

        private Boolean fuelUseActive;

        // check fuel use every 2 minutes
        private int fuelUseSampleTime = 2;

        private float currentFuel;

        private float gameTimeWhenFuelWasReset = 0;

        private int lapsCompletedWhenFuelWasReset = 0;

        private Boolean enableFuelMessages = UserSettings.GetUserSettings().getBoolean("enable_fuel_messages");

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
            currentFuel = -1;
            fuelUseActive = false;
            gameTimeWhenFuelWasReset = 0;
            lapsCompletedWhenFuelWasReset = 0;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            fuelUseActive = currentGameState.FuelData.FuelUseActive;
            currentFuel = currentGameState.FuelData.FuelLeft;
            if (fuelUseActive && ((currentGameState.SessionData.SessionType == SessionType.Race &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.Checkered)) ||
                 ((currentGameState.SessionData.SessionType == SessionType.Qualify || currentGameState.SessionData.SessionType == SessionType.Practice || 
                    currentGameState.SessionData.SessionType == SessionType.HotLap) && 
                    (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) &&
                    currentGameState.SessionData.LapTimeCurrent > 0)))
            {               
                // To get the initial fuel, wait for 15 seconds
                if (currentGameState.SessionData.SessionRunningTime > 15 && (!initialised || 
                    (previousGameState != null && currentGameState.FuelData.FuelLeft > previousGameState.FuelData.FuelLeft + 1)))
                {
                    fuelUseWindow = new List<float>();
                    initialFuelLevel = currentGameState.FuelData.FuelLeft;
                    lapsCompletedWhenFuelWasReset = currentGameState.SessionData.CompletedLaps;
                    gameTimeWhenFuelWasReset = currentGameState.SessionData.SessionRunningTime;
                    fuelUseWindow.Add(initialFuelLevel);
                    gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                    Console.WriteLine("Initial fuel level = " + initialFuelLevel);
                    initialised = true;
                    if (currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        if (halfDistance == -1)
                        {
                            halfDistance = currentGameState.SessionData.SessionNumberOfLaps / 2;
                        }
                    }
                    else if (currentGameState.SessionData.SessionRunTime > 0)
                    {
                        if (halfTime == -1)
                        {
                            halfTime = currentGameState.SessionData.SessionRunTime / 2;
                            Console.WriteLine("Half time = " + halfTime);
                        }
                    }                    
                    playedPitForFuelNow = false;
                    playedFiveMinutesRemaining = false;
                    playedTenMinutesRemaining = false;
                    playedTwoMinutesRemaining = false;
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
                        for (int i = 0; i < fuelUseWindowLength - 1; i++)
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
                                audioPlayer.queueClip(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                if (estimatedFuelLapsLeft < 60)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Fuel/estimate", MessageContents(folderWeEstimate, 
                                        QueuedMessage.folderNameNumbersStub + estimatedFuelLapsLeft, folderLapsRemaining), 0, this));
                                }
                            }
                            else
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceLowFuel, 0, this));
                            }
                        }
                        else
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceGoodFuel, 0, this));
                        }
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 4)
                    {
                        Console.WriteLine("4 laps fuel left, starting fuel = " + initialFuelLevel +
                                ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderFourLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 3)
                    {
                        Console.WriteLine("3 laps fuel left, starting fuel = " + initialFuelLevel +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderThreeLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 2)
                    {
                        Console.WriteLine("2 laps fuel left, starting fuel = " + initialFuelLevel +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderTwoLapsEstimate, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelLapsLeft == 1)
                    {
                        Console.WriteLine("1 lap fuel left, starting fuel = " + initialFuelLevel +
                            ", current fuel = " + currentGameState.FuelData.FuelLeft + ", usage per lap = " + averageUsagePerLap);
                        audioPlayer.queueClip(new QueuedMessage(folderOneLapEstimate, 0, this));
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
                                audioPlayer.queueClip(new QueuedMessage(RaceTime.folderHalfWayHome, 0, this));
                                if (minutesLeft < 60)
                                {
                                    audioPlayer.queueClip(new QueuedMessage("Fuel/estimate", MessageContents(
                                        folderWeEstimate, QueuedMessage.folderNameNumbersStub + minutesLeft, folderMinutesRemaining), 0, this));
                                }
                            }
                            else
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceLowFuel, 0, this));
                            }
                        }
                        else
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderHalfDistanceGoodFuel, 0, this));
                        }
                    }
                }
                else if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && currentGameState.SessionData.SessionRunTime > 0 &&
                    currentGameState.SessionData.SessionRunningTime > gameTimeAtLastFuelWindowUpdate + (60 * fuelUseSampleTime))
                {
                    // it's 2 minutes since the last fuel window check
                    gameTimeAtLastFuelWindowUpdate = currentGameState.SessionData.SessionRunningTime;
                    fuelUseWindow.Insert(0, currentGameState.FuelData.FuelLeft);
                    // if we've got fuelUseWindowLength + 1 samples (note we initialise the window data with fuelAt15Seconds so we always
                    // have one extra), get the average difference between each pair of values

                    // only do this if we have a full window of data + one extra start point
                    if (fuelUseWindow.Count > fuelUseWindowLength)
                    {
                        averageUsagePerMinute = 0;
                        for (int i = 0; i < fuelUseWindowLength - 1; i++)
                        {
                            averageUsagePerMinute += (fuelUseWindow[i + 1] - fuelUseWindow[i]);
                        }
                        averageUsagePerMinute = averageUsagePerMinute / (fuelUseWindowLength * fuelUseSampleTime);
                    }
                    else
                    {
                        averageUsagePerMinute = 60 * (initialFuelLevel - currentGameState.FuelData.FuelLeft) / (gameTimeAtLastFuelWindowUpdate - gameTimeWhenFuelWasReset);
                    }
                }
                if (initialised && currentGameState.SessionData.SessionNumberOfLaps <= 0 && currentGameState.SessionData.SessionRunTime > 0 && averageUsagePerMinute > 0)
                {
                    float estimatedFuelMinutesLeft = currentGameState.FuelData.FuelLeft / averageUsagePerMinute;
                    if (enableFuelMessages && estimatedFuelMinutesLeft <1.5 && !playedPitForFuelNow)
                    {
                        playedPitForFuelNow = true;
                        playedTwoMinutesRemaining = true;
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(new QueuedMessage(folderAboutToRunOut, 0, this));
                    } if (enableFuelMessages && estimatedFuelMinutesLeft <= 2 && estimatedFuelMinutesLeft > 1.8 && !playedTwoMinutesRemaining)
                    {
                        playedTwoMinutesRemaining = true;
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(new QueuedMessage(folderTwoMinutesFuel, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelMinutesLeft <= 5 && estimatedFuelMinutesLeft > 4.8 && !playedFiveMinutesRemaining)
                    {
                        playedFiveMinutesRemaining = true;
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(new QueuedMessage(folderFiveMinutesFuel, 0, this));
                    }
                    else if (enableFuelMessages && estimatedFuelMinutesLeft <= 10 && estimatedFuelMinutesLeft > 9.8 && !playedTenMinutesRemaining)
                    {
                        playedTenMinutesRemaining = true;
                        audioPlayer.queueClip(new QueuedMessage(folderTenMinutesFuel, 0, this));
                    }
                }
                else if (enableFuelMessages && initialised && !playedHalfTankWarning && currentGameState.FuelData.FuelLeft / initialFuelLevel <= 0.50)
                {
                    // warning message for fuel left - these play as soon as the fuel reaches 1/2 tank left
                    playedHalfTankWarning = true;
                    audioPlayer.queueClip(new QueuedMessage(folderHalfTankWarning, 0, this));
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.FUEL_LEVEL))
            {
                if (!fuelUseActive)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (currentFuel < 60 && currentFuel >= 2)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage("Fuel/level",
                                MessageContents(QueuedMessage.folderNameNumbersStub + (int)currentFuel, folderLitresRemaining), 0, null), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null), false);
                    audioPlayer.closeChannel();
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.FUEL)) 
            {
                Boolean haveData = false;
                if (initialised && currentFuel > -1)
                {
                    if (averageUsagePerLap > 0)
                    {
                        int lapsOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerLap);
                        if (lapsOfFuelLeft > 60)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        else if (lapsOfFuelLeft <= 1)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage("Fuel/estimate",
                                MessageContents(folderAboutToRunOut), 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        else
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage("Fuel/estimate",
                                MessageContents(folderWeEstimate, QueuedMessage.folderNameNumbersStub + lapsOfFuelLeft, folderLapsRemaining), 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        haveData = true;
                    }
                    else if (averageUsagePerMinute > 0)
                    {
                        int minutesOfFuelLeft = (int)Math.Floor(currentFuel / averageUsagePerMinute);
                        if (minutesOfFuelLeft > 60)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        else if (minutesOfFuelLeft <= 1)
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage("Fuel/estimate",
                                MessageContents(folderAboutToRunOut), 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        else
                        {
                            audioPlayer.playClipImmediately(new QueuedMessage("Fuel/estimate",
                                MessageContents(folderWeEstimate, QueuedMessage.folderNameNumbersStub + minutesOfFuelLeft, folderMinutesRemaining), 0, null), false);
                            audioPlayer.closeChannel();
                        }
                        haveData = true;
                    }
                }
                if (!haveData)
                {
                    if (!fuelUseActive)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null), false);
                    }
                    else if (currentFuel < 60 && currentFuel >= 2)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage("Fuel/level",
                                    MessageContents(QueuedMessage.folderNameNumbersStub + (int)currentFuel, folderLitresRemaining), 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                    }
                    audioPlayer.closeChannel();
                }
            }            
        }
    }
}
