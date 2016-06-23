using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class MandatoryPitStops : AbstractEvent
    {
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

        // for voice responses
        public static String folderMandatoryPitStopsYesStopOnLap = "mandatory_pit_stops/yes_stop_on_lap";
        public static String folderMandatoryPitStopsYesStopAfter = "mandatory_pit_stops/yes_stop_after";
        public static String folderMandatoryPitStopsMissedStop = "mandatory_pit_stops/missed_stop";


        private int pitWindowOpenLap;

        private int pitWindowClosedLap;

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

        private Random random = new Random();
        
        public MandatoryPitStops(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public override void clearState()
        {
            pitWindowOpenLap = 0;
            pitWindowClosedLap = 0;
            pitWindowOpenTime = 0;
            pitWindowClosedTime = 0;
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
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.PitData.HasMandatoryPitStop && currentGameState.SessionData.SessionPhase == SessionPhase.Green)
            {                
                if (!pitDataInitialised)
                {
                    mandatoryStopCompleted = false;
                    mandatoryStopBoxThisLap = false;
                    mandatoryStopMissed = false;
                    Console.WriteLine("pit start = " + currentGameState.PitData.PitWindowStart + ", pit end = " + currentGameState.PitData.PitWindowEnd);

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
                else if (currentGameState.PitData.IsMakingMandatoryPitStop)
                {
                    playPitThisLap = false;
                    playBoxNowMessage = false;
                    mandatoryStopCompleted = true;
                    mandatoryStopBoxThisLap = false;
                    mandatoryStopMissed = false;
                }
                else
                {
                    if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps > 0)
                    {
                        if (currentGameState.PitData.PitWindow != PitWindow.StopInProgress && currentGameState.PitData.PitWindow != PitWindow.Completed) 
                        {
                            if (maxDistanceOnCurrentTyre > 0 && currentGameState.SessionData.CompletedLaps == maxDistanceOnCurrentTyre && playPitThisLap)
                            {
                                playBoxNowMessage = true;
                                playPitThisLap = false;
                                mandatoryStopBoxThisLap = true;
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitPrimesThisLap, random.Next(0, 10), this));
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitOptionsThisLap, random.Next(0, 20), this));
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLap, random.Next(0, 20), this));
                                }
                            }
                            else if (minDistanceOnCurrentTyre > 0 && currentGameState.SessionData.CompletedLaps == minDistanceOnCurrentTyre)
                            {
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsCanNowFitPrimes, random.Next(0, 20), this));
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsCanNowFitOptions, random.Next(0, 20), this));
                                }
                            }
                        }

                        if (pitWindowOpenLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowOpenLap - 1)
                        {
                            // note this is a 'pit window opens at the end of this lap' message, 
                            // so we play it 1 lap before the window opens
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpening, random.Next(0, 20), this));
                        }
                        else if (pitWindowOpenLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowOpenLap)
                        {
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowOpenBackground);
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen, 0, this));
                        }
                        else if (pitWindowClosedLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowClosedLap - 1)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowClosing, random.Next(0, 20), this));
                            if (currentGameState.PitData.PitWindow != PitWindow.Completed &&
                                currentGameState.PitData.PitWindow != PitWindow.StopInProgress)
                            {
                                playBoxNowMessage = true;
                                if (mandatoryTyreChangeTyreType == TyreType.Prime)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitPrimesThisLap, random.Next(0, 10), this));
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitOptionsThisLap, random.Next(0, 10), this));
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLap, random.Next(0, 10), this));
                                }
                            }
                        }
                        else if (pitWindowClosedLap > 0 && currentGameState.SessionData.CompletedLaps == pitWindowClosedLap)
                        {
                            mandatoryStopBoxThisLap = false;
                            if (currentGameState.PitData.PitWindow != PitWindow.Completed)
                            {
                                mandatoryStopMissed = true;
                            }
                            audioPlayer.setBackgroundSound(AudioPlayer.dtmPitWindowClosedBackground);
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowClosed, 0, this));                            
                        }
                    }
                    else if (currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0 && currentGameState.SessionData.SessionTimeRemaining > 0)
                    {
                        if (pitWindowClosedTime > 0 && currentGameState.PitData.PitWindow != PitWindow.StopInProgress &&
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
                                audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLapTooLate, 0, this));
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
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitPrimesThisLap, random.Next(0, 20), this));
                                }
                                else if (mandatoryTyreChangeTyreType == TyreType.Option)
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsFitOptionsThisLap, random.Next(0, 20), this));
                                }
                                else
                                {
                                    audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitThisLap, random.Next(0, 20), this));
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
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen, 0, this));
                    }
                    else if (play1minOpenWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowOpenTime - 1) * 60))
                    {
                        play1minOpenWarning = false;
                        play2minOpenWarning = false;
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen1Min, 0, this));
                    }
                    else if (play2minOpenWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowOpenTime - 2) * 60))
                    {
                        play2minOpenWarning = false;
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowOpen2Min, 0, this));
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
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowClosed, 0, this));
                    }
                    else if (pitWindowClosedTime > 0 && play1minCloseWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowClosedTime - 1) * 60))
                    {
                        play1minCloseWarning = false;
                        play2minCloseWarning = false;
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowCloses1min, 0, this));
                    }
                    else if (pitWindowClosedTime > 0 && play2minCloseWarning && currentGameState.SessionData.SessionTimeRemaining > 0 &&
                        currentGameState.SessionData.SessionTotalRunTime - currentGameState.SessionData.SessionTimeRemaining > ((pitWindowClosedTime - 2) * 60))
                    {
                        play2minCloseWarning = false;
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitWindowCloses2min, 0, this));
                    }

                    // for Automobilista, sector update lag time means sometimes we miss the pit entrance before this message plays
                    if (playBoxNowMessage && currentGameState.SessionData.SectorNumber == 2 && currentGameState.carClass.rF1ClassName.Length > 0)
                    {
                        playBoxNowMessage = false;
                        audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitNow, 15, this));
                    }

                    if (playBoxNowMessage && currentGameState.SessionData.SectorNumber == 3)
                    {
                        playBoxNowMessage = false;
                        if (mandatoryTyreChangeTyreType == TyreType.Prime)
                        {
                            audioPlayer.playMessage(new QueuedMessage("box_now_for_primes", MessageContents(folderMandatoryPitStopsPitNow, folderMandatoryPitStopsPrimeTyres), 3, this));
                        }
                        else if (mandatoryTyreChangeTyreType == TyreType.Option)
                        {
                            audioPlayer.playMessage(new QueuedMessage("box_now_for_options", MessageContents(folderMandatoryPitStopsPitNow, folderMandatoryPitStopsOptionTyres), 3, this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderMandatoryPitStopsPitNow, 3, this));
                        }
                    }
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (!hasMandatoryPitStop || mandatoryStopCompleted)
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
                    MessageContents(folderMandatoryPitStopsYesStopAfter, TimeSpan.FromMinutes(pitWindowOpenTime)), 0, null));
                
            }            
            else
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                
            }
        }
    }
}
