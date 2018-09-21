using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class PitStops : AbstractEvent
    {
        private Boolean pitBoxPositionCountdown = UserSettings.GetUserSettings().getBoolean("pit_box_position_countdown");

        public static String folderMandatoryPitStopsPitWindowOpensOnLap = "mandatory_pit_stops/pit_window_opens_on_lap";
        public static String folderMandatoryPitStopsPitWindowOpensAfter = "mandatory_pit_stops/pit_window_opens_after";

        public static String folderMandatoryPitStopsFitPrimesThisLap = "mandatory_pit_stops/box_to_fit_primes_now";

        public static String folderMandatoryPitStopsFitOptionsThisLap = "mandatory_pit_stops/box_to_fit_options_now";

        public static String folderMandatoryPitStopsPrimeTyres = "mandatory_pit_stops/prime_tyres";

        public static String folderMandatoryPitStopsOptionTyres = "mandatory_pit_stops/option_tyres";

        private String folderMandatoryPitStopsCanNowFitPrimes = "mandatory_pit_stops/can_fit_primes";

        private String folderMandatoryPitStopsCanNowFitOptions = "mandatory_pit_stops/can_fit_options";

        private String folderMandatoryPitStopsPitWindowOpening = "mandatory_pit_stops/pit_window_opening";

        private String folderMandatoryPitStopsPitWindowOpen1Min = "mandatory_pit_stops/pit_window_opens_1_min";

        private String folderMandatoryPitStopsPitWindowOpen2Min = "mandatory_pit_stops/pit_window_opens_2_min";

        private String folderMandatoryPitStopsPitWindowOpen = "mandatory_pit_stops/pit_window_open";

        private String folderMandatoryPitStopsPitWindowCloses1min = "mandatory_pit_stops/pit_window_closes_1_min";

        private String folderMandatoryPitStopsPitWindowCloses2min = "mandatory_pit_stops/pit_window_closes_2_min";

        private String folderMandatoryPitStopsPitWindowClosing = "mandatory_pit_stops/pit_window_closing";

        private String folderMandatoryPitStopsPitWindowClosed = "mandatory_pit_stops/pit_window_closed";

        public static String folderMandatoryPitStopsPitThisLap = "mandatory_pit_stops/pit_this_lap";

        private String folderMandatoryPitStopsPitThisLapTooLate = "mandatory_pit_stops/pit_this_lap_too_late";

        private String folderMandatoryPitStopsPitNow = "mandatory_pit_stops/pit_now";

        private String folderEngageLimiter = "mandatory_pit_stops/engage_limiter";
        private String folderDisengageLimiter = "mandatory_pit_stops/disengage_limiter";

        // for voice responses
        public static String folderMandatoryPitStopsYesStopOnLap = "mandatory_pit_stops/yes_stop_on_lap";
        public static String folderMandatoryPitStopsYesStopAfter = "mandatory_pit_stops/yes_stop_after";
        public static String folderMandatoryPitStopsMissedStop = "mandatory_pit_stops/missed_stop";

        // pit stop messages
        private String folderWatchYourPitSpeed = "mandatory_pit_stops/watch_your_pit_speed";
        private String folderPitCrewReady = "mandatory_pit_stops/pit_crew_ready";
        private String folderPitStallOccupied = "mandatory_pit_stops/pit_stall_occupied";
        private String folderStopCompleteGo = "mandatory_pit_stops/stop_complete_go";
        private String folderPitStopRequestReceived = "mandatory_pit_stops/pit_stop_requested";
        private String folderPitStopRequestCancelled = "mandatory_pit_stops/pit_request_cancelled";

        private String folderMetres = "mandatory_pit_stops/metres";
        private String folderBoxPositionIntro = "mandatory_pit_stops/box_in";

        // separate sounds for "100 metres" and "50 metres" for a nicer pit countdown
        private String folderOneHundredMetreWarning = "mandatory_pit_stops/one_hundred_metres";
        private String folderFiftyMetreWarning = "mandatory_pit_stops/fifty_metres";

        private int pitWindowOpenLap;

        private int pitWindowClosedLap;

        private Boolean inPitWindow;

        private int pitWindowOpenTime;

        private int pitWindowClosedTime;

        private Boolean pitDataInitialised;
        
        private Boolean playBoxNowMessage;

        private Boolean playOpenNow;

        private Boolean play1minOpenWarning;

        private Boolean play2minOpenWarning;

        private Boolean playClosedNow;

        private Boolean play1minCloseWarning;

        private Boolean play2minCloseWarning;

        private Boolean playPitThisLap;

        private Boolean mandatoryStopCompleted;

        private Boolean mandatoryStopBoxThisLap;

        private Boolean mandatoryStopMissed;

        private TyreType mandatoryTyreChangeTyreType = TyreType.Unknown_Race;

        private Boolean hasMandatoryTyreChange;

        private Boolean hasMandatoryPitStop;

        private float minDistanceOnCurrentTyre;

        private float maxDistanceOnCurrentTyre;

        private DateTime timeOfLastLimiterWarning = DateTime.MinValue;

        private DateTime timeOfDisengageCheck = DateTime.MaxValue;

        private DateTime timeOfPitRequestOrCancel = DateTime.MinValue;

        private DateTime timeSpeedInPitsWarning = DateTime.MinValue;

        private const int minSecondsBetweenPitRequestCancel = 5;

        private Boolean enableWindowWarnings = true;

        private Boolean pitStallOccupied = false;

        private Boolean warnedAboutOccupiedPitOnThisLap = false;

        private float previousDistanceToBox = -1;
        private Boolean playedLimiterLineToPitBoxDistanceWarning = false;
        private Boolean played100MetreWarning = false;
        private Boolean played50MetreWarning = false;

        private DateTime timeStartedAppoachingPitsCheck = DateTime.MaxValue;

        public PitStops(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race, SessionType.LonePractice }; }
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Countdown, SessionPhase.Finished, SessionPhase.Checkered, SessionPhase.FullCourseYellow }; }
        }

        public override void clearState()
        {
            timeOfLastLimiterWarning = DateTime.MinValue;
            timeOfDisengageCheck = DateTime.MaxValue;
            timeOfPitRequestOrCancel = DateTime.MinValue;
            timeStartedAppoachingPitsCheck = DateTime.MaxValue;
            timeSpeedInPitsWarning = DateTime.MinValue;
            pitWindowOpenLap = 0;
            pitWindowClosedLap = 0;
            pitWindowOpenTime = 0;
            pitWindowClosedTime = 0;
            inPitWindow = false;
            pitDataInitialised = false;
            playBoxNowMessage = false;
            play2minOpenWarning = false;
            play2minCloseWarning = false;
            play1minOpenWarning = false;
            play1minCloseWarning = false;
            playClosedNow = false;
            playOpenNow = false;
            playPitThisLap = false;
            mandatoryStopCompleted = false;
            mandatoryStopBoxThisLap = false;
            mandatoryStopMissed = false;
            mandatoryTyreChangeTyreType = TyreType.Unknown_Race;
            hasMandatoryPitStop = false;
            hasMandatoryTyreChange = false;
            minDistanceOnCurrentTyre = -1;
            maxDistanceOnCurrentTyre = -1;
            enableWindowWarnings = true;
            pitStallOccupied = false;
            warnedAboutOccupiedPitOnThisLap = false;
            previousDistanceToBox = -1;
            played100MetreWarning = false;
            played50MetreWarning = false;
            playedLimiterLineToPitBoxDistanceWarning = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if (eventSubType == folderPitStopRequestReceived)
                {
                    return currentGameState.PitData.HasRequestedPitStop;
                }
                else if (eventSubType == folderPitStopRequestCancelled)
                {
                    return !currentGameState.PitData.HasRequestedPitStop;
                }
            }
            else
            {
                return false;
            }

            return true;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionPhase == SessionPhase.Finished
                && currentGameState.ControlData.ControlType == ControlType.AI)
            {
                return;
            }

            this.pitStallOccupied = currentGameState.PitData.PitStallOccupied;
            if (currentGameState.SessionData.IsNewLap)
            {
                warnedAboutOccupiedPitOnThisLap = false;
            }
            // AMS (RF1) uses the pit window calculations to make 'box now' calls for scheduled stops, but we don't want 
            // the pit window opening / closing warnings.
            // Try also applying the same approach to rF2.
            if (CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT)
            {
                enableWindowWarnings = false;
            }

            if (previousGameState != null && pitBoxPositionCountdown && 
                currentGameState.PositionAndMotionData.CarSpeed > 2 &&
                currentGameState.PitData.PitBoxPositionEstimate > 0 && 
                !currentGameState.PenaltiesData.HasDriveThrough)
            {
                if (previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane)
                {
                    playedLimiterLineToPitBoxDistanceWarning = false;
                }
                float distanceToBox;
                if (currentGameState.PitData.PitBoxPositionEstimate > currentGameState.PositionAndMotionData.DistanceRoundTrack)
                {
                    distanceToBox = currentGameState.PitData.PitBoxPositionEstimate - currentGameState.PositionAndMotionData.DistanceRoundTrack;
                }
                else
                {
                    distanceToBox = currentGameState.SessionData.TrackDefinition.trackLength - currentGameState.PositionAndMotionData.DistanceRoundTrack + currentGameState.PitData.PitBoxPositionEstimate;
                }
                if (!previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
                {
                    // just entered the pitlane
                    previousDistanceToBox = 0;
                    played100MetreWarning = false;
                    played50MetreWarning = false;
                    if (distanceToBox > 150 && !playedLimiterLineToPitBoxDistanceWarning)
                    {
                        int distanceToBoxInt = (int)distanceToBox;
                        int distanceToBoxRounded;
                        if (distanceToBoxInt % 10 == 0)
                            distanceToBoxRounded = distanceToBoxInt;
                        else
                            distanceToBoxRounded = (10 - distanceToBoxInt % 10) + distanceToBoxInt;

                        List<MessageFragment> messageContents = new List<MessageFragment>();
                        messageContents.Add(MessageFragment.Text(folderBoxPositionIntro));
                        messageContents.Add(MessageFragment.Integer(distanceToBoxRounded, false));   // explicity disable short hundreds here, forcing the full "one hundred" sound
                        messageContents.Add(MessageFragment.Text(folderMetres));
                        QueuedMessage firstPitCountdown = new QueuedMessage("pit_entry_to_box_distance_warning", messageContents, 0, this);
                        firstPitCountdown.expiryTime = (DateTime.UtcNow.Ticks / TimeSpan.TicksPerMillisecond) + 2000;
                        audioPlayer.playMessage(firstPitCountdown, 10);

                    }
                    playedLimiterLineToPitBoxDistanceWarning = true;
                }
                else if (previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane && previousDistanceToBox > -1)
                {
                    if (!played100MetreWarning && distanceToBox < 100 && previousDistanceToBox > 95)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderOneHundredMetreWarning, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        previousDistanceToBox = distanceToBox;
                        played100MetreWarning = true;
                    }
                    // VL: I see some tracks with pit stall as close as 35 meters to the entrance.  Shall we add "less than 30 meters" message if nothing played before?
                    else if (!played50MetreWarning && distanceToBox < 50 && previousDistanceToBox > 45)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderFiftyMetreWarning, 0, null) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        previousDistanceToBox = distanceToBox;
                        played50MetreWarning = true;
                    }
                    else if (previousDistanceToBox > -1)
                    {
                        previousDistanceToBox = distanceToBox;
                    }
                }
            }

            if (!mandatoryStopCompleted && currentGameState.PitData.MandatoryPitStopCompleted)
            {
                mandatoryStopCompleted = true;
                mandatoryStopMissed = false;
                playPitThisLap = false;
                playBoxNowMessage = false;
                mandatoryStopBoxThisLap = false;
            }
            if (currentGameState.PitData.limiterStatus != -1 && currentGameState.Now > timeOfLastLimiterWarning + TimeSpan.FromSeconds(30))
            {
                if (currentGameState.SessionData.SectorNumber == 1 && 
                    currentGameState.Now > timeOfDisengageCheck && !currentGameState.PitData.InPitlane && currentGameState.PitData.limiterStatus == 1)
                {
                    // in S1 but have exited pits, and we're expecting the limit to have been turned off
                    timeOfDisengageCheck = DateTime.MaxValue;
                    timeOfLastLimiterWarning = currentGameState.Now;
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderDisengageLimiter, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                }
                else if (previousGameState != null)
                {
                    if (!previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane)
                    {
                        if (currentGameState.PitData.limiterStatus == 0 && currentGameState.PositionAndMotionData.CarSpeed > 1)
                        {
                            // just entered the pit lane with no limiter active
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderEngageLimiter, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
                            timeOfLastLimiterWarning = currentGameState.Now;
                        }
                    }
                    else if (currentGameState.SessionData.SectorNumber == 1 &&
                        previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane && currentGameState.PitData.limiterStatus == 1 && CrewChief.gameDefinition.gameEnum != GameEnum.IRACING)
                    {
                        // just left the pitlane with the limiter active - wait 2 seconds then warn
                        timeOfDisengageCheck = currentGameState.Now + TimeSpan.FromSeconds(2);
                    }
                    else if (currentGameState.PitData.IsAtPitExit && currentGameState.PitData.limiterStatus == 1 && CrewChief.gameDefinition.gameEnum == GameEnum.IRACING)
                    {
                        // TODO: this needs a bit more investigation. We have 2 separate blocks here because the time delay may need to be different for iRacing. 
                        // I know this looks like a fucking retarded if-else statement but I don't care. It's all Morten's fault anyway. Just like the AccessViolationErrors
                        timeOfDisengageCheck = currentGameState.Now + TimeSpan.FromSeconds(2);
                    }
                }
            }
            else if (previousGameState != null 
                && currentGameState.PitData.limiterStatus == -1  // If limiter is not available
                && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane  // Just entered the pits
                && currentGameState.Now > timeSpeedInPitsWarning + TimeSpan.FromSeconds(120)  // We did not play this on pit approach
                && previousGameState.PositionAndMotionData.CarSpeed > 2.0f && currentGameState.PositionAndMotionData.CarSpeed > 2.0f  // Guard against tow, teleport, returning to ISI game's Monitor and other bullshit
                && currentGameState.SessionData.SessionRunningTime > 30.0f)  // Sanity check !inPts -> inPits flip on session start.
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(folderWatchYourPitSpeed, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.PitData.HasMandatoryPitStop &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow))
            {                
                // allow this data to be reinitialised during a race (hack for AMS)
                if (!pitDataInitialised || currentGameState.PitData.ResetEvents)
                {
                    mandatoryStopCompleted = false;
                    mandatoryStopBoxThisLap = false;
                    mandatoryStopMissed = false;
                    Console.WriteLine("Pit start = " + currentGameState.PitData.PitWindowStart + ", pit end = " + currentGameState.PitData.PitWindowEnd);

                    hasMandatoryPitStop = currentGameState.PitData.HasMandatoryPitStop;
                    hasMandatoryTyreChange = currentGameState.PitData.HasMandatoryTyreChange;
                    mandatoryTyreChangeTyreType = currentGameState.PitData.MandatoryTyreChangeRequiredTyreType;
                    maxDistanceOnCurrentTyre = currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre;
                    minDistanceOnCurrentTyre = currentGameState.PitData.MinPermittedDistanceOnCurrentTyre;

                    if (currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        pitWindowOpenLap = currentGameState.PitData.PitWindowStart;
                        pitWindowClosedLap = currentGameState.PitData.PitWindowEnd;
                        playPitThisLap = true;
                    }
                    else if (currentGameState.SessionData.SessionTimeRemaining > 0)
                    {
                        pitWindowOpenTime = currentGameState.PitData.PitWindowStart;
                        pitWindowClosedTime = currentGameState.PitData.PitWindowEnd;
                        if (pitWindowOpenTime > 0)
                        {
                            play2minOpenWarning = pitWindowOpenTime > 2;
                            play1minOpenWarning = pitWindowOpenTime > 1;
                            playOpenNow = true;
                        }
                        if (pitWindowClosedTime > 0)
                        {
                            play2minCloseWarning = pitWindowClosedTime > 2;
                            play1minCloseWarning = pitWindowClosedTime > 1;
                            playClosedNow = true;
                            playPitThisLap = true;
                        }
                    }
                    else
                    {
                        Console.WriteLine("Error getting pit data");
                    }
                    pitDataInitialised = true;
                }
                else
                {
                    if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        if (currentGameState.PitData.PitWindow != PitWindow.StopInProgress && 
                            currentGameState.PitData.PitWindow != PitWindow.Completed && !currentGameState.PitData.InPitlane) 
                        {
                            if (maxDistanceOnCurrentTyre > 0 && currentGameState.SessionData.CompletedLaps == maxDistanceOnCurrentTyre && playPitThisLap)
                            {
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                                mandatoryStopBoxThisLap = true;
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitPrimesThisLap, Utilities.random.Next(0, 10), this), 10);
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitOptionsThisLap, Utilities.random.Next(0, 20), this), 10);
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLap, Utilities.random.Next(0, 20), this), 10);
                                }
                            }
                            else if (minDistanceOnCurrentTyre > 0 && currentGameState.SessionData.CompletedLaps == minDistanceOnCurrentTyre)
                            {
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsCanNowFitPrimes, Utilities.random.Next(0, 20), this), 5);
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsCanNowFitOptions, Utilities.random.Next(0, 20), this), 5);
                                }
                            }
                        }

                        if (pitWindowOpenLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowOpenLap - 1)
                        {
                            // note this is a 'pit window opens at the end of this lap' message, 
                            // so we play it 1 lap before the window opens
                            if (enableWindowWarnings)
                            {
                                audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpening, Utilities.random.Next(0, 20), this), 10);
                            }
                        }
                        else if (pitWindowOpenLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowOpenLap)
                        {
                            inPitWindow = true;
                            if (enableWindowWarnings)
                            {
                                audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowOpenBackground);
                                audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen, 0, this), 10);
                            }
                        }
                        else if (pitWindowClosedLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowClosedLap - 1)
                        {
                            if (enableWindowWarnings)
                            {
                                audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowClosing, Utilities.random.Next(0, 20), this), 10);
                            }
                            if (currentGameState.PitData.PitWindow != PitWindow.Completed && !currentGameState.PitData.InPitlane &&
                                currentGameState.PitData.PitWindow != PitWindow.StopInProgress)
                            {
                                playBoxNowMessage = true;
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitPrimesThisLap, Utilities.random.Next(0, 10), this), 10);
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitOptionsThisLap, Utilities.random.Next(0, 10), this), 10);
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLap, Utilities.random.Next(0, 10), this), 10);
                                }
                            }
                        }
                        else if (pitWindowClosedLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowClosedLap)
                        {
                            mandatoryStopBoxThisLap = false;
                            inPitWindow = false;
                            if (currentGameState.PitData.PitWindow != PitWindow.Completed)
                            {
                                mandatoryStopMissed = true;
                            }
                            if (enableWindowWarnings)
                            {
                                audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowClosedBackground);
                                audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowClosed, 0, this), 10);
                            }
                        }
                    }
                    else if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionTimeRemaining > 0)
                    {
                        if (pitWindowClosedTime > 0 && currentGameState.PitData.PitWindow != PitWindow.StopInProgress &&
                            !currentGameState.PitData.InPitlane &&
                            currentGameState.PitData.PitWindow != PitWindow.Completed &&
                            currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > pitWindowOpenTime * 60 &&
                            currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining < pitWindowClosedTime * 60)
                        {
                            double timeLeftToPit = pitWindowClosedTime * 60 - (currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining);
                            if (playPitThisLap && currentGameState.SessionData.PlayerLapTimeSessionBest + 10 > timeLeftToPit)
                            {
                                // oh dear, we might have missed the pit window.
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                                mandatoryStopBoxThisLap = true;
                                if (enableWindowWarnings)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLapTooLate, 0, this), 10);
                                }
                            }
                            else if (playPitThisLap && currentGameState.SessionData.PlayerLapTimeSessionBest + 10 < timeLeftToPit &&
                                (currentGameState.SessionData.PlayerLapTimeSessionBest * 2) + 10 > timeLeftToPit)
                            {
                                // we probably won't make it round twice - pit at the end of this lap
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                                mandatoryStopBoxThisLap = true;
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitPrimesThisLap, Utilities.random.Next(0, 20), this), 10);
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitOptionsThisLap, Utilities.random.Next(0, 20), this), 10);
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLap, Utilities.random.Next(0, 20), this), 10);
                                }
                            }
                        }
                    }
                    if (playOpenNow && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        (currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > (pitWindowOpenTime * 60) ||
                        currentGameState.PitData.PitWindow == PitWindow.Open))
                    {
                        playOpenNow = false;
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        if (enableWindowWarnings)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen, 0, this), 10);
                        }
                    }
                    else if (play1minOpenWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowOpenTime - 1) * 60))
                    {
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        if (enableWindowWarnings)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen1Min, 0, this), 3);
                        }
                    }
                    else if (play2minOpenWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowOpenTime - 2) * 60))
                    {
                        play2minOpenWarning = false;
                        if (enableWindowWarnings)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen2Min, 0, this), 10);
                        }
                    }
                    else if (pitWindowClosedTime > 0 && playClosedNow && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > (pitWindowClosedTime * 60))
                    {
                        playClosedNow = false;
                        playBoxNowMessage = false;
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        playPitThisLap = false;
                        mandatoryStopBoxThisLap = false;
                        if (currentGameState.PitData.PitWindow != PitWindow.Completed)
                        {
                            mandatoryStopMissed = true;
                        }
                        if (enableWindowWarnings)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowClosed, 0, this), 10);
                        }
                    }
                    else if (pitWindowClosedTime > 0 && play1minCloseWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowClosedTime - 1) * 60))
                    {
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        if (enableWindowWarnings)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowCloses1min, 0, this), 10);
                        }
                    }
                    else if (pitWindowClosedTime > 0 && play2minCloseWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowClosedTime - 2) * 60))
                    {
                        play2minCloseWarning = false;
                        if (enableWindowWarnings)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowCloses2min, 0, this), 10);
                        }
                    }

                    // for Automobilista, sector update lag time means sometimes we miss the pit entrance before this message plays
                    if (playBoxNowMessage && currentGameState.SessionData.SectorNumber == 2 && 
                        CrewChief.gameDefinition.gameEnum == GameEnum.RF1)
                    {
                        playBoxNowMessage = false;
                        // pit entry is right at sector 3 timing line, play message part way through sector 2 to give us time to pit
                        int messageDelay = currentGameState.SessionData.PlayerBestSector2Time > 0 ? (int)(currentGameState.SessionData.PlayerBestSector2Time * 0.7) : 15;
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitNow, messageDelay, this), 10);
                    }

                    if (playBoxNowMessage && currentGameState.SessionData.SectorNumber == 3)
                    {
                        playBoxNowMessage = false;
                        if (!currentGameState.PitData.InPitlane && currentGameState.PitData.PitWindow != PitWindow.StopInProgress && 
                            currentGameState.PitData.PitWindow != PitWindow.Completed)
                        {                            
                            if (mandatoryTyreChangeTyreType == TyreType.Prime)
                            {
                                audioPlayer.playMessage(new QueuedMessage("box_now_for_primes", MessageContents(folderMandatoryPitStopsPitNow, folderMandatoryPitStopsPrimeTyres), 3, this), 10);
                            }
                            else if (mandatoryTyreChangeTyreType == TyreType.Option)
                            {
                                audioPlayer.playMessage(new QueuedMessage("box_now_for_options", MessageContents(folderMandatoryPitStopsPitNow, folderMandatoryPitStopsOptionTyres), 3, this), 10);
                            }
                            else
                            {
                                audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitNow, 3, this), 10);
                            }
                        }
                    }
                }
            }
            if (previousGameState != null)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race
                    || currentGameState.SessionData.SessionType == SessionType.Qualify
                    || currentGameState.SessionData.SessionType == SessionType.Practice)
                {
                    if ((!previousGameState.PitData.IsApproachingPitlane
                        && currentGameState.PitData.IsApproachingPitlane && CrewChief.gameDefinition.gameEnum != GameEnum.IRACING)
                        // Here we need to make sure that the player has intended to go into the pit's sometimes this trows if we are getting in this zone while overtaking or just defending the line  
                        || currentGameState.PitData.IsApproachingPitlane && CrewChief.gameDefinition.gameEnum == GameEnum.IRACING
                        && currentGameState.Now > timeStartedAppoachingPitsCheck && currentGameState.ControlData.BrakePedal <= 0 && !currentGameState.PitData.OnOutLap)
                    {
                        timeStartedAppoachingPitsCheck = DateTime.MaxValue;
                        timeSpeedInPitsWarning = currentGameState.Now;

                        audioPlayer.playMessageImmediately(new QueuedMessage(folderWatchYourPitSpeed, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
                    }
                    if (!previousGameState.PitData.IsApproachingPitlane
                        && currentGameState.PitData.IsApproachingPitlane && timeStartedAppoachingPitsCheck == DateTime.MaxValue)
                    {
                        timeStartedAppoachingPitsCheck = currentGameState.Now + TimeSpan.FromSeconds(1);
                    }
                    // different logic for PCars2 pit-crew-ready checks
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK)
                    {
                        if (!previousGameState.PitData.PitStallOccupied && currentGameState.PitData.PitStallOccupied)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderPitStallOccupied, Utilities.random.Next(1, 3), this), 10);
                            warnedAboutOccupiedPitOnThisLap = true;
                        }
                        if (currentGameState.SessionData.SectorNumber == 3 &&
                            previousGameState.SessionData.SectorNumber == 2 &&
                            currentGameState.PitData.HasRequestedPitStop)
                        {
                            if (currentGameState.PitData.PitStallOccupied)
                            {
                                if (!warnedAboutOccupiedPitOnThisLap)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderPitStallOccupied, Utilities.random.Next(1, 3), this), 10);
                                    warnedAboutOccupiedPitOnThisLap = true;
                                }
                            }
                            else
                            {
                                audioPlayer.playMessage(new QueuedMessage(folderPitCrewReady, Utilities.random.Next(1, 3), this), 10);
                            }
                        }
                    }
                    else if (!previousGameState.PitData.IsPitCrewReady
                        && currentGameState.PitData.IsPitCrewReady)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderPitCrewReady, Utilities.random.Next(1, 3), this), 10);
                    }
                    if (!previousGameState.PitData.IsPitCrewDone
                        && currentGameState.PitData.IsPitCrewDone)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderStopCompleteGo, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
                    }
                    if (!previousGameState.PitData.HasRequestedPitStop
                        && currentGameState.PitData.HasRequestedPitStop
                        && (currentGameState.Now - timeOfPitRequestOrCancel).TotalSeconds > minSecondsBetweenPitRequestCancel)
                    {
                        timeOfPitRequestOrCancel = currentGameState.Now;
                        // respond immediately to this request
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderPitStopRequestReceived, 0, this));
                    }
                    // don't play pit request cancelled in pCars2 because the request often cancels itself for no reason at all (other than pcars2 being a mess)
                    // - the pit crew may or may not be ready for you when this happens. It's just one of the many mysteries of pCars2.
                    if (CrewChief.gameDefinition.gameEnum != GameEnum.PCARS2 && CrewChief.gameDefinition.gameEnum != GameEnum.PCARS2_NETWORK
                        && !currentGameState.PitData.InPitlane && !previousGameState.PitData.InPitlane  // Make sure we're not in pits.  More checks might be needed.
                        && previousGameState.PitData.HasRequestedPitStop
                        && !currentGameState.PitData.HasRequestedPitStop
                        && (currentGameState.Now - timeOfPitRequestOrCancel).TotalSeconds > minSecondsBetweenPitRequestCancel)
                    {
                        timeOfPitRequestOrCancel = currentGameState.Now;
                        audioPlayer.playMessage(new QueuedMessage(folderPitStopRequestCancelled, Utilities.random.Next(1, 3), this), 10);
                    }
                }
                else if ((CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK) &&
                    !previousGameState.PitData.HasRequestedPitStop && currentGameState.PitData.HasRequestedPitStop && 
                      (currentGameState.Now - timeOfPitRequestOrCancel).TotalSeconds > minSecondsBetweenPitRequestCancel)
                {
                    timeOfPitRequestOrCancel = currentGameState.Now;
                    // respond immediately to this request
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPitStopRequestReceived, 0, this));
                }
            }            
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.IS_MY_PIT_BOX_OCCUPIED))
            {
                if (this.pitStallOccupied)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderPitStallOccupied, 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNo, 0, null));
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SESSION_STATUS) ||
                 SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                if (enableWindowWarnings && pitDataInitialised)
                {
                    if (mandatoryStopMissed)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderMandatoryPitStopsMissedStop, 0, null));
                    }
                    else if (hasMandatoryPitStop && !mandatoryStopCompleted)
                    {
                        if (!inPitWindow)
                        {
                            if (pitWindowOpenLap > 0)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("pit_window_open_lap",
                                    MessageContents(folderMandatoryPitStopsPitWindowOpensOnLap, pitWindowOpenLap), 0, null));
                            }
                            else if (pitWindowOpenTime > 0)
                            {
                                audioPlayer.playMessageImmediately(new QueuedMessage("pit_window_open_time",
                                    MessageContents(folderMandatoryPitStopsPitWindowOpensAfter, TimeSpanWrapper.FromMinutes(pitWindowOpenTime, Precision.MINUTES)), 0, null));
                            }
                        }
                        else
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage("pit_window_open", 
                                MessageContents(folderMandatoryPitStopsPitWindowOpen, pitWindowOpenLap), 0, null));
                        }
                    }
                }
            }
            else
            {
                if (!hasMandatoryPitStop || mandatoryStopCompleted || !enableWindowWarnings)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNo, 0, null));
                }
                else if (mandatoryStopMissed)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderMandatoryPitStopsMissedStop, 0, null));
                }
                else if (mandatoryStopBoxThisLap)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("yesBoxThisLap",
                        MessageContents(AudioPlayer.folderYes, folderMandatoryPitStopsPitThisLap), 0, null));
                }
                else if (pitWindowOpenLap > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("yesBoxOnLap",
                        MessageContents(folderMandatoryPitStopsYesStopOnLap, pitWindowOpenLap), 0, null));
                }
                else if (pitWindowOpenTime > 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("yesBoxAfter",
                        MessageContents(folderMandatoryPitStopsYesStopAfter, TimeSpanWrapper.FromMinutes(pitWindowOpenTime, Precision.MINUTES)), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
        }
    }
}
