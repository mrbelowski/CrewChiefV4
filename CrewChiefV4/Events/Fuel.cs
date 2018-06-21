﻿using System;
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

        public static String folderGallonsRemaining = "fuel/gallons_remaining";

        public static String folderOneLitreRemaining = "fuel/one_litre_remaining";

        public static String folderOneGallonRemaining = "fuel/one_gallon_remaining";

        public static String folderHalfAGallonRemaining = "fuel/half_a_gallon_remaining";

        public static String folderAboutToRunOut = "fuel/about_to_run_out";

        public static String folderLitresPerLap = "fuel/litres_per_lap";

        public static String folderGallonsPerLap = "fuel/gallons_per_lap";

        public static String folderLitres = "fuel/litres";

        public static String folderLitre = "fuel/litre";

        public static String folderGallons = "fuel/gallons";

        public static String folderGallon = "fuel/gallon";

        public static String folderWillNeedToStopAgain = "fuel/will_need_to_stop_again";

        public static String folderWillNeedToAdd = "fuel/we_will_need_to_add";

        public static String folderLitresToGetToTheEnd = "fuel/litres_to_get_to_the_end";

        public static String folderGallonsToGetToTheEnd = "fuel/gallons_to_get_to_the_end";

        // no 1 litres equivalent
        public static String folderWillNeedToAddOneGallonToGetToTheEnd = "fuel/need_to_add_one_gallon_to_get_to_the_end";

        public static String folderFuelWillBeTight = "fuel/fuel_will_be_tight";

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
        private int fuelUseByLapsWindowLengthToUse = 3;
        private int fuelUseByLapsWindowLengthVeryShort = 5;
        private int fuelUseByLapsWindowLengthShort = 4;
        private int fuelUseByLapsWindowLengthMedium = 3;
        private int fuelUseByLapsWindowLengthLong = 2;
        private int fuelUseByLapsWindowLengthVeryLong = 1;
        
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

        private float fuelCapacity = 0;

        private float gameTimeWhenFuelWasReset = 0;

        private Boolean enableFuelMessages = UserSettings.GetUserSettings().getBoolean("enable_fuel_messages");

        private Boolean delayResponses = UserSettings.GetUserSettings().getBoolean("enable_delayed_responses");

        private Boolean fuelReportsInGallon = UserSettings.GetUserSettings().getBoolean("report_fuel_in_gallons");

        private Boolean useTightFuelCalc = UserSettings.GetUserSettings().getBoolean("enable_tight_fuel_calc");

        private float addAdditionalFuel = UserSettings.GetUserSettings().getFloat("add_additional_fuel");

        private Boolean hasBeenRefuelled = false;

        // checking if we need to read fuel messages involves a bit of arithmetic and stuff, so only do this every few seconds
        private DateTime nextFuelStatusCheck = DateTime.MinValue;

        private TimeSpan fuelStatusCheckInterval = TimeSpan.FromSeconds(5);

        private Boolean sessionHasFixedNumberOfLaps = false;

        // count laps separately for fuel so we always count incomplete and invalid laps
        private int lapsCompletedSinceFuelReset = 0;

        private int lapsRemaining = -1;
        private float secondsRemaining = -1;
        private float bestLapTime = -1;

        private static float litresPerGallon = 3.78541f;

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

            lapsRemaining = -1;
            secondsRemaining = -1;
            bestLapTime = -1;
            fuelCapacity = 0;
        }

        // fuel not implemented for HotLap/LonePractice modes
        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Countdown, SessionPhase.FullCourseYellow }; }
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
            if (currentGameState.SessionData.SessionHasFixedTime)
            {
                secondsRemaining = currentGameState.SessionData.SessionTimeRemaining;
                bestLapTime = currentGameState.SessionData.PlayerLapTimeSessionBest;
            }
            else
            {
                lapsRemaining = currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps;
            }
            currentFuel = currentGameState.FuelData.FuelLeft;
            fuelCapacity = currentGameState.FuelData.FuelCapacity;
            // only track fuel data after the session has settled down
            if (fuelUseActive && !GameStateData.onManualFormationLap &&
                currentGameState.SessionData.SessionRunningTime > 15 &&
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
                    // don't process fuel data in prac and qual until we're actually moving:
                    currentGameState.PositionAndMotionData.CarSpeed > 10)))
            {    
                if (!initialised ||
                    // fuel has increased by at least 1 litre - we only check against the time window here
                    (fuelLevelWindowByTime.Count() > 0 && fuelLevelWindowByTime[0] > 0 && currentGameState.FuelData.FuelLeft > fuelLevelWindowByTime[0] + 1) ||
                    (currentGameState.SessionData.SessionType != SessionType.Race && previousGameState != null && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane))
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
                        if (currentGameState.SessionData.TrackDefinition != null)
                        {
                            switch (currentGameState.SessionData.TrackDefinition.trackLengthClass)
                            {
                                case TrackData.TrackLengthClass.VERY_SHORT:
                                    fuelUseByLapsWindowLengthToUse = fuelUseByLapsWindowLengthVeryShort;
                                    break;
                                case TrackData.TrackLengthClass.SHORT:
                                    fuelUseByLapsWindowLengthToUse = fuelUseByLapsWindowLengthShort;
                                    break;
                                case TrackData.TrackLengthClass.MEDIUM:
                                    fuelUseByLapsWindowLengthToUse = fuelUseByLapsWindowLengthMedium;
                                    break;
                                case TrackData.TrackLengthClass.LONG:
                                    fuelUseByLapsWindowLengthToUse = fuelUseByLapsWindowLengthLong;
                                    break;
                                case TrackData.TrackLengthClass.VERY_LONG:
                                    fuelUseByLapsWindowLengthToUse = fuelUseByLapsWindowLengthVeryLong;
                                    break;
                            }
                        }
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
                    if (fuelReportsInGallon)
                    {
                        Console.WriteLine("Fuel level initialised, initialFuelLevel = " + convertLitersToGallons(initialFuelLevel).ToString("0.000") + " gallons, halfDistance = " + halfDistance + " halfTime = " + halfTime.ToString("0.00"));
                    }
                    else
                    {
                        Console.WriteLine("Fuel level initialised, initialFuelLevel = " + initialFuelLevel.ToString("0.000") + " liters, halfDistance = " + halfDistance + " halfTime = " + halfTime.ToString("0.00"));
                    }

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
                        if (fuelLevelWindowByLap.Count > fuelUseByLapsWindowLengthToUse)
                        {
                            averageUsagePerLap = 0;
                            for (int i = 0; i < fuelUseByLapsWindowLengthToUse; i++)
                            {
                                averageUsagePerLap += (fuelLevelWindowByLap[i + 1] - fuelLevelWindowByLap[i]);
                            }
                            averageUsagePerLap = averageUsagePerLap / fuelUseByLapsWindowLengthToUse;
                            if(fuelReportsInGallon)
                            {
                                Console.WriteLine("Fuel use per lap (windowed calc) = " + convertLitersToGallons(averageUsagePerLap).ToString("0.000") + " fuel(gallons) left = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000"));
                            }
                            else
                            {
                                Console.WriteLine("Fuel use per lap (windowed calc) = " + averageUsagePerLap.ToString("0.000") + " fuel(liters) left = " + currentGameState.FuelData.FuelLeft.ToString("0.000"));
                            }
                        }
                        else
                        {
                            averageUsagePerLap = (initialFuelLevel - currentGameState.FuelData.FuelLeft) / lapsCompletedSinceFuelReset;
                            if (fuelReportsInGallon)
                            {
                                Console.WriteLine("Fuel use per lap (basic calc) = " + convertLitersToGallons(averageUsagePerLap).ToString("0.000") + " fuel left(gallons) = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000"));
                            }
                            else
                            {
                                Console.WriteLine("Fuel use per lap (basic calc) = " + averageUsagePerLap.ToString("0.000") + " fuel(liters) left = " + currentGameState.FuelData.FuelLeft.ToString("0.000"));
                            }
                        }
                    }
                    if (!currentGameState.PitData.InPitlane
                        && currentGameState.SessionData.SessionRunningTime > gameTimeAtLastFuelWindowUpdate + fuelUseSampleTime)
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

                            if (fuelReportsInGallon)
                            {
                                Console.WriteLine("Fuel use per minute (windowed calc) = " + convertLitersToGallons(averageUsagePerMinute).ToString("0.000") + " fuel(gallons) left = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000"));
                            }
                            else
                            {
                                Console.WriteLine("Fuel use per minute (windowed calc) = " + averageUsagePerMinute.ToString("0.000") + " fuel left(liters) = " + currentGameState.FuelData.FuelLeft.ToString("0.000"));
                            }
                        }
                        else
                        {
                            averageUsagePerMinute = 60 * (initialFuelLevel - currentGameState.FuelData.FuelLeft) / (gameTimeAtLastFuelWindowUpdate - gameTimeWhenFuelWasReset);
                            
                            if (fuelReportsInGallon)
                            {
                                Console.WriteLine("Fuel use per minute (basic calc) = " + convertLitersToGallons(averageUsagePerMinute).ToString("0.000") + " fuel(gallons) left = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000"));
                            }
                            else
                            {
                                Console.WriteLine("Fuel use per minute (basic calc) = " + averageUsagePerMinute.ToString("0.000") + " fuel(liters) left = " + currentGameState.FuelData.FuelLeft.ToString("0.000"));
                            }
                        }
                    }

                    // warnings for particular fuel levels
                    if (enableFuelMessages)
                    {
                        if(fuelReportsInGallon)
                        {
                            if (convertLitersToGallons(currentFuel) <= 1 && !played2LitreWarning)
                            {
                                // yes i know its not 2 liters but who really cares.
                                played2LitreWarning = true;
                                audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(folderOneGallonRemaining), 0, this));
                            }
                            else if (convertLitersToGallons(currentFuel) <= 0.5f && !played1LitreWarning)
                            {
                                //^^
                                played1LitreWarning = true;
                                audioPlayer.playMessage(new QueuedMessage("Fuel/level", MessageContents(folderHalfAGallonRemaining), 0, this));
                            }
                        }
                        else
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
                        }


                        // warnings for fixed lap sessions
                        if (currentGameState.SessionData.IsNewLap && averageUsagePerLap > 0 &&
                            (currentGameState.SessionData.SessionNumberOfLaps > 0 ||
                                currentGameState.SessionData.SessionType == SessionType.HotLap ||
                                currentGameState.SessionData.SessionType == SessionType.LonePractice) &&
                            lapsCompletedSinceFuelReset > 0)
                        {
                            int estimatedFuelLapsLeft = (int)Math.Floor(currentGameState.FuelData.FuelLeft / averageUsagePerLap);
                            int raceLapsRemaining = currentGameState.SessionData.SessionNumberOfLaps - currentGameState.SessionData.CompletedLaps;
                            if (halfDistance != -1 && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps == halfDistance)
                            {
                                if (estimatedFuelLapsLeft <= halfDistance)
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
                            else if (raceLapsRemaining > 3 && estimatedFuelLapsLeft == 4)
                            {
                                if(fuelReportsInGallon)
                                {
                                    Console.WriteLine("4 laps fuel left, starting fuel = " + convertLitersToGallons(initialFuelLevel).ToString("0.000") +
                                            ", current fuel = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000") + ", usage per lap = " + convertLitersToGallons(averageUsagePerLap).ToString("0.000"));
                                }
                                else
                                {
                                    Console.WriteLine("4 laps fuel left, starting fuel = " + initialFuelLevel.ToString("0.000") +
                                            ", current fuel = " + currentGameState.FuelData.FuelLeft.ToString("0.000") + ", usage per lap = " + averageUsagePerLap.ToString("0.000"));  
                                }
                                audioPlayer.playMessage(new QueuedMessage(folderFourLapsEstimate, 0, this));
                            }
                            else if (raceLapsRemaining > 2 && estimatedFuelLapsLeft == 3)
                            {
                                if (fuelReportsInGallon)
                                {
                                    Console.WriteLine("3 laps fuel left, starting fuel = " + convertLitersToGallons(initialFuelLevel).ToString("0.000") +
                                            ", current fuel = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000") + ", usage per lap = " + convertLitersToGallons(averageUsagePerLap).ToString("0.000"));
                                }
                                else
                                {
                                    Console.WriteLine("3 laps fuel left, starting fuel = " + initialFuelLevel.ToString("0.000") +
                                            ", current fuel = " + currentGameState.FuelData.FuelLeft.ToString("0.000") + ", usage per lap = " + averageUsagePerLap.ToString("0.000"));
                                }
                            }
                            else if (raceLapsRemaining > 1 && estimatedFuelLapsLeft == 2)
                            {
                                if (fuelReportsInGallon)
                                {
                                    Console.WriteLine("2 laps fuel left, starting fuel = " + convertLitersToGallons(initialFuelLevel).ToString("0.000") +
                                            ", current fuel = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000") + ", usage per lap = " + convertLitersToGallons(averageUsagePerLap).ToString("0.000"));
                                }
                                else
                                {
                                    Console.WriteLine("2 laps fuel left, starting fuel = " + initialFuelLevel.ToString("0.000") +
                                            ", current fuel = " + currentGameState.FuelData.FuelLeft.ToString("0.000") + ", usage per lap = " + averageUsagePerLap.ToString("0.000"));
                                }
                            }
                            else if (raceLapsRemaining > 0 && estimatedFuelLapsLeft == 1)
                            {
                                if (fuelReportsInGallon)
                                {
                                    Console.WriteLine("1 laps fuel left, starting fuel = " + convertLitersToGallons(initialFuelLevel).ToString("0.000") +
                                            ", current fuel = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000") + ", usage per lap = " + convertLitersToGallons(averageUsagePerLap).ToString("0.000"));
                                }
                                else
                                {
                                    Console.WriteLine("1 laps fuel left, starting fuel = " + initialFuelLevel.ToString("0.000") +
                                            ", current fuel = " + currentGameState.FuelData.FuelLeft.ToString("0.000") + ", usage per lap = " + averageUsagePerLap.ToString("0.000"));
                                }                                
                                audioPlayer.playMessage(new QueuedMessage(folderOneLapEstimate, 0, this));
                                // if we've not played the pit-now message, play it with a bit of a delay - should probably wait for sector3 here
                                // but i'd have to move some stuff around and I'm an idle fucker
                                if (!playedPitForFuelNow && raceLapsRemaining > 1)
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
                                if (fuelReportsInGallon)
                                {
                                    Console.WriteLine("Half race distance. Fuel(gallons) in tank = " + convertLitersToGallons(currentGameState.FuelData.FuelLeft).ToString("0.000") +
                                        ", average usage per minute = " + convertLitersToGallons(averageUsagePerMinute).ToString("0.000"));
                                }
                                else
                                {
                                    Console.WriteLine("Half race distance. Fuel(liters) in tank = " + currentGameState.FuelData.FuelLeft.ToString("0.000") + ", average usage per minute = " + averageUsagePerMinute.ToString("0.000"));
                                }
                                playedHalfTimeFuelEstimate = true;
                                if (currentGameState.SessionData.SessionType == SessionType.Race)
                                {
                                    // need a bit of slack in this estimate:
                                    if (averageUsagePerMinute * (halfTime + currentGameState.SessionData.PlayerLapTimeSessionBest) / 60 > currentGameState.FuelData.FuelLeft)
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
                                float cutoffForRefuelCall = currentGameState.SessionData.PlayerLapTimeSessionBest * 2;
                                if (cutoffForRefuelCall == 0)
                                {
                                    // shouldn't need this, but just in case...
                                    cutoffForRefuelCall = 120;
                                }
                                if (!currentGameState.PitData.InPitlane)
                                {
                                    if (currentGameState.SessionData.SessionTimeRemaining > cutoffForRefuelCall)
                                    {
                                        audioPlayer.playMessage(new QueuedMessage("pit_for_fuel_now",
                                            MessageContents(folderAboutToRunOut, PitStops.folderMandatoryPitStopsPitThisLap), 0, this));
                                    }
                                    else
                                    {
                                        // going to run out, but don't call the player into the pits - it's up to him
                                        audioPlayer.playMessage(new QueuedMessage("about_to_run_out_of_fuel", MessageContents(folderAboutToRunOut), 0, this));
                                    }
                                }
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
                            else if (!playedHalfTankWarning && currentGameState.FuelData.FuelLeft / initialFuelLevel <= 0.50 && 
                                currentGameState.FuelData.FuelLeft / initialFuelLevel >= 0.47 && !hasBeenRefuelled)
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
                if(fuelReportsInGallon)
                {
                    meanUsePerLap = convertLitersToGallons(averageUsagePerLap, true);
                }
                Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(meanUsePerLap);
                if (meanUsePerLap > 0)
                {
                    haveData = true;

                    if (wholeandfractional.Item2 > 0)
                    {
                        if (fuelReportsInGallon)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/mean_use_per_lap",
                                    MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderGallonsPerLap), 0, null));
                        }
                        else
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/mean_use_per_lap",
                                    MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderLitresPerLap), 0, null));
                        }
                    }
                    else
                    {
                        if (fuelReportsInGallon)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/mean_use_per_lap",
                                    MessageContents(wholeandfractional.Item1, folderGallonsPerLap), 0, null));
                        }
                        else
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/mean_use_per_lap",
                                    MessageContents(wholeandfractional.Item1, folderLitresPerLap), 0, null));
                        }
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
                float totalUsage = 0f;
                if(fuelReportsInGallon)
                {
                    totalUsage = convertLitersToGallons(averageUsagePerLap * numberOfLaps, true);
                }
                else
                {
                    totalUsage = (float)Math.Ceiling(averageUsagePerLap * numberOfLaps);
                }
                if (totalUsage > 0)
                {
                    haveData = true;
                    // build up the message fragments the verbose way, so we can prevent the number reader from shortening hundreds to
                    // stuff like "one thirty two" - we always want "one hundred and thirty two"
                    List<MessageFragment> messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(folderWeEstimate));
                    if(fuelReportsInGallon)
                    {
                        // for gallons we want both whole and fractional part cause its a stupid unit.
                        Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(totalUsage);
                        if (wholeandfractional.Item2 > 0)
                        {                            
                            messageFragments.AddRange(MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderGallons));
                        }
                        else
                        {
                            int usage = Convert.ToInt32(wholeandfractional.Item1);
                            messageFragments.Add(MessageFragment.Integer(usage, false));
                            messageFragments.Add(MessageFragment.Text(usage == 1 ? folderGallon : folderGallons));
                        }
                    }
                    else
                    {
                        int usage = Convert.ToInt32(totalUsage);
                        messageFragments.Add(MessageFragment.Integer(usage, false));
                        messageFragments.Add(MessageFragment.Text(usage == 1 ? folderLitre : folderLitres));
                    }
                    QueuedMessage fuelEstimateMessage = new QueuedMessage("Fuel/estimate", messageFragments, 0, null);

                    // play this immediately or play "stand by", and queue it to be played in a few seconds
                    if (delayResponses && Utilities.random.Next(10) >= 2 && SoundCache.availableSounds.Contains(AudioPlayer.folderStandBy))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderStandBy, 0, null));
                        int secondsDelay = Math.Max(5, Utilities.random.Next(7));
                        audioPlayer.pauseQueue(secondsDelay);
                        fuelEstimateMessage.secondsDelay = secondsDelay;
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
                int timeToUse = (hours * 60) + minutes;
                // round up
                float totalUsage = 0;
                if(fuelReportsInGallon)
                {
                    totalUsage = convertLitersToGallons(averageUsagePerMinute * timeToUse, true);
                }
                else
                {
                    totalUsage = ((float)Math.Ceiling(averageUsagePerMinute * timeToUse));
                }                              
                if (totalUsage > 0)
                {
                    haveData = true;
                    // build up the message fragments the verbose way, so we can prevent the number reader from shortening hundreds to
                    // stuff like "one thirty two" - we always want "one hundred and thirty two"    
                    List<MessageFragment> messageFragments = new List<MessageFragment>();
                    messageFragments.Add(MessageFragment.Text(folderWeEstimate));
                    if (fuelReportsInGallon)
                    {
                        // for gallons we want both whole and fractional part cause its a stupid unit.
                        Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(totalUsage);
                        if (wholeandfractional.Item2 > 0)
                        {
                            messageFragments.AddRange(MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderGallons));
                        }
                        else
                        {
                            int usage = Convert.ToInt32(wholeandfractional.Item1);
                            messageFragments.Add(MessageFragment.Integer(usage, false));
                            messageFragments.Add(MessageFragment.Text(usage == 1 ? folderGallon : folderGallons));
                        }
                    }
                    else
                    {
                        int usage = Convert.ToInt32(totalUsage);
                        messageFragments.Add(MessageFragment.Integer(usage, false));
                        messageFragments.Add(MessageFragment.Text(usage == 1 ? folderLitre : folderLitres));
                    }

                    QueuedMessage fuelEstimateMessage = new QueuedMessage("Fuel/estimate",
                            messageFragments, 0, null);
                    // play this immediately or play "stand by", and queue it to be played in a few seconds
                    if (delayResponses && Utilities.random.Next(10) >= 2 && SoundCache.availableSounds.Contains(AudioPlayer.folderStandBy))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderStandBy, 0, null));
                        int secondsDelay = Math.Max(5, Utilities.random.Next(7));
                        audioPlayer.pauseQueue(secondsDelay);
                        fuelEstimateMessage.secondsDelay = secondsDelay;
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
                    return haveData;
                }
                if(fuelReportsInGallon)
                {
                    if (convertLitersToGallons(currentFuel) >= 2)
                    {
                        haveData = true;
                        List<MessageFragment> messageFragments = new List<MessageFragment>();

                        // for gallons we want both whole and fractional part cause its a stupid unit.
                        Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(convertLitersToGallons(currentFuel, true));
                        if (wholeandfractional.Item2 > 0)
                        {
                            messageFragments.AddRange(MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderGallonsRemaining));
                        }
                        else
                        {
                            messageFragments.Add(MessageFragment.Integer(Convert.ToInt32(wholeandfractional.Item1), false));
                            messageFragments.Add(MessageFragment.Text(folderGallonsRemaining));
                        }
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level", messageFragments, 0, null));
                    }
                    else if (convertLitersToGallons(currentFuel) >= 1)
                    {
                        haveData = true;
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                    MessageContents(folderOneGallonRemaining), 0, null));
                    }
                    else if (convertLitersToGallons(currentFuel) > 0.5f)
                    {
                        haveData = true;
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents(folderHalfAGallonRemaining), 0, null));
                    }
                    else if (convertLitersToGallons(currentFuel) > 0)
                    {
                        haveData = true;
                        audioPlayer.playMessageImmediately(new QueuedMessage("Fuel/level",
                                MessageContents(folderAboutToRunOut), 0, null));
                    }
                }
                else
                {
                    if (currentFuel >= 2)
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
            Boolean reportedLitresNeeded = false;
            int litresToEnd = getLitresToEndOfRace();
            if (litresToEnd > 0)
            {
                int litresRemaining = (int)Math.Floor(CrewChief.currentGameState.FuelData.FuelLeft);
                int litresNeeded = litresToEnd - litresRemaining;
                // -2 means we expect to have 2 litres left at the end of the race
                if (litresNeeded >= -2)
                {
                    QueuedMessage fuelMessage;
                    if (litresNeeded < 3)
                    {
                        // between 2 litres short, and 2 litres excess fuel
                        fuelMessage = new QueuedMessage(folderFuelWillBeTight, 0, null);
                    }
                    else
                    {
                        if (fuelReportsInGallon)
                        {
                            // for gallons we want both whole and fractional part cause its a stupid unit.
                            float gallonsNeeded = convertLitersToGallons(litresNeeded, true);
                            Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(gallonsNeeded);
                            if (wholeandfractional.Item2 > 0)
                            {
                                fuelMessage = new QueuedMessage("fuel_estimate_to_end", MessageContents(folderWillNeedToAdd,
                                    wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderGallonsToGetToTheEnd), 0, null);
                            }
                            else
                            {
                                int wholeGallons = Convert.ToInt32(wholeandfractional.Item1);
                                fuelMessage = new QueuedMessage("fuel_estimate_to_end", MessageContents(wholeGallons, wholeGallons == 1 ?
                                    folderWillNeedToAddOneGallonToGetToTheEnd : folderWillNeedToAdd, wholeGallons, folderGallonsToGetToTheEnd), 0, null);
                            }
                        }
                        else
                        {
                            fuelMessage = new QueuedMessage("fuel_estimate_to_end", MessageContents(folderWillNeedToAdd,
                                litresNeeded, folderLitresToGetToTheEnd), 0, null);
                        }
                    }
                    audioPlayer.playMessageImmediately(fuelMessage);
                    reportedLitresNeeded = true;
                }
            }
            if (!reportedConsumption && !reportedRemaining && !reportedLitresNeeded && allowNoDataMessage)
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
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOW_MUCH_FUEL_TO_END_OF_RACE))
            {
                int litresNeeded = getLitresToEndOfRace();
                if (!fuelUseActive || litresNeeded == -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                } 
                else if (litresNeeded == 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPlentyOfFuel, 0, null));
                }
                else
                {
                    QueuedMessage fuelMessage;
                    if (fuelReportsInGallon)
                    {
                        // for gallons we want both whole and fractional part cause its a stupid unit.
                        float gallonsNeeded = convertLitersToGallons(litresNeeded, true);
                        Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(gallonsNeeded);
                        if (wholeandfractional.Item2 > 0)
                        {
                            fuelMessage = new QueuedMessage("fuel_estimate_to_end", MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2, folderGallons), 0, null);
                        }
                        else
                        {
                            int wholeGallons = Convert.ToInt32(wholeandfractional.Item1);
                            fuelMessage = new QueuedMessage("fuel_estimate_to_end", MessageContents(wholeGallons, wholeGallons == 1 ? folderGallon : folderGallons), 0, null);
                        }
                    }
                    else
                    {
                        fuelMessage = new QueuedMessage("fuel_estimate_to_end", MessageContents(litresNeeded, litresNeeded == 1 ? folderLitre : folderLitres), 0, null);
                    }
                    if (delayResponses && Utilities.random.Next(10) >= 2 && SoundCache.availableSounds.Contains(AudioPlayer.folderStandBy))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderStandBy, 0, null));
                        int secondsDelay = Math.Max(5, Utilities.random.Next(7));
                        audioPlayer.pauseQueue(secondsDelay);
                        fuelMessage.secondsDelay = secondsDelay;
                        audioPlayer.playDelayedImmediateMessage(fuelMessage);
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(fuelMessage);
                    }
                }
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

        // -1 means no data
        public int getLitresToEndOfRace()
        {
            int additionalLitresNeeded = -1;
            if (fuelUseActive)
            {
                int additionalFuelLiters = fuelReportsInGallon ? convertGallonsToLitres(addAdditionalFuel) : (int)addAdditionalFuel;
                int reserve = !useTightFuelCalc ? 2 : additionalFuelLiters;
                if (sessionHasFixedNumberOfLaps && averageUsagePerLap > 0)
                {
                    float totalLitresNeededToEnd = (averageUsagePerLap * lapsRemaining) + reserve;
                    additionalLitresNeeded = (int) Math.Max(0, totalLitresNeededToEnd - currentFuel);
                    Console.WriteLine("Use per lap = " + averageUsagePerLap + " laps to go = " + lapsRemaining + " current fuel = " +
                        currentFuel + " additional fuel needed = " + additionalLitresNeeded);
                }
                else if (averageUsagePerMinute > 0)
                {
                    if (CrewChief.currentGameState != null && CrewChief.currentGameState.SessionData.TrackDefinition != null && !useTightFuelCalc)
                    {
                        TrackData.TrackLengthClass trackLengthClass = CrewChief.currentGameState.SessionData.TrackDefinition.trackLengthClass;
                        if (trackLengthClass < TrackData.TrackLengthClass.MEDIUM)
                        {
                            reserve = 1;
                        }
                        else if (trackLengthClass == TrackData.TrackLengthClass.LONG)
                        {
                            reserve = 3;
                        }
                        else if (trackLengthClass == TrackData.TrackLengthClass.VERY_LONG)
                        {
                            reserve = 4;
                        }
                    }
                    float maxMinutesRemaining = (secondsRemaining + bestLapTime) / 60f;
                    float totalLitresNeededToEnd = (float)Math.Ceiling(averageUsagePerMinute * maxMinutesRemaining) + reserve;
                    additionalLitresNeeded = (int) Math.Max(0, totalLitresNeededToEnd - currentFuel);
                    Console.WriteLine("Use per minute = " + averageUsagePerMinute + " estimated minutes to go (including final lap) = " +
                        maxMinutesRemaining + " current fuel = " + currentFuel + " additional fuel needed = " + additionalLitresNeeded);
                }
            }
            return additionalLitresNeeded;
        }

        public override int resolveMacroKeyPressCount(String macroName)
        {
            // only used for r3e auto-fuel amount selection at present
            Console.WriteLine("Getting fuel requirement keypress count");
            int litresToEnd = getLitresToEndOfRace();

            // limit the number of key presses to 200 here, or fuelCapacity
            int fuelCapacityInt = (int)fuelCapacity;
            if (fuelCapacityInt > 0 && fuelCapacityInt < litresToEnd)
            {
                // if we have a known fuel capacity and this is less than the calculated amount of fuel we need, warn about it.
                audioPlayer.playMessage(new QueuedMessage(folderWillNeedToStopAgain, 4, this));
            }
            int maxPresses = fuelCapacityInt > 0 ? fuelCapacityInt : 200;
            return litresToEnd == -1 ? 0 : litresToEnd > maxPresses ? maxPresses : litresToEnd;
        }

        private float convertLitersToGallons(float liters, Boolean roundTo1dp = false)
        {
            if (liters <= 0)
            {
                return 0f;
            }
            float gallons = liters / litresPerGallon;
            if(roundTo1dp)
            {
                return ((float)Math.Round(gallons * 10f)) / 10f;
            }
            return gallons;
        }

        private int convertGallonsToLitres(float gallons)
        {
            return (int)Math.Ceiling(gallons * litresPerGallon);
        }
    }
}
