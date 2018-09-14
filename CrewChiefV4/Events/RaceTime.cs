using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.Events;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class RaceTime : AbstractEvent
    {
        private String folder5mins = "race_time/five_minutes_left";
        private String folder5minsLeading = "race_time/five_minutes_left_leading";
        private String folder5minsPodium = "race_time/five_minutes_left_podium";
        private String folder2mins = "race_time/two_minutes_left";
        private String folder0mins = "race_time/zero_minutes_left";

        private String folder10mins = "race_time/ten_minutes_left";
        private String folder15mins = "race_time/fifteen_minutes_left";
        private String folder20mins = "race_time/twenty_minutes_left";
        public static String folderHalfWayHome = "race_time/half_way";
        private String folderLastLap = "race_time/last_lap";
        private String folderLastLapLeading = "race_time/last_lap_leading";
        private String folderLastLapPodium = "race_time/last_lap_top_three";

        public static String folderRemaining = "race_time/remaining";
        private String folderLapsLeft = "race_time/laps_remaining";

        private String folderLessThanOneMinute = "race_time/less_than_one_minute";

        private String folderThisIsTheLastLap = "race_time/this_is_the_last_lap";

        private String folderOneMinuteRemaining = "race_time/one_minute_remaining";

        private String folderOneLapAfterThisOne = "race_time/one_more_lap_after_this_one";

        private Boolean played0mins, played2mins, played5mins, played10mins, played15mins, played20mins, playedHalfWayHome, playedLastLap;

        private float halfTime;

        private Boolean gotHalfTime;

        private int lapsLeft;
        private float timeLeft;

        private Boolean addExtraLap;

        private Boolean startedExtraLap;

        private Boolean leaderHasFinishedRace;

        private Boolean sessionLengthIsTime;

        // allow condition messages during caution periods
        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Checkered, SessionPhase.FullCourseYellow }; }
        }

        public RaceTime(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            played0mins = false; played2mins = false; played5mins = false; played10mins = false; played15mins = false;
            played20mins = false; playedHalfWayHome = false; playedLastLap = false;
            halfTime = 0;
            gotHalfTime = false;
            lapsLeft = -1;
            timeLeft = 0;
            sessionLengthIsTime = false;
            leaderHasFinishedRace = false;
            addExtraLap = false;
            startedExtraLap = false;
        }
        
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            // store this in a local var so it's available for vocie command responses
            addExtraLap = currentGameState.SessionData.HasExtraLap;
            leaderHasFinishedRace = currentGameState.SessionData.LeaderHasFinishedRace;
            timeLeft = currentGameState.SessionData.SessionTimeRemaining;
            if (!currentGameState.SessionData.SessionHasFixedTime)
            {
                lapsLeft = currentGameState.SessionData.SessionLapsRemaining;
                sessionLengthIsTime = false;
            }
            else
            {
                sessionLengthIsTime = true;
            }
            if (sessionLengthIsTime)
            {
                if (addExtraLap && gotHalfTime && timeLeft <= 0 && currentGameState.SessionData.IsNewLap)
                {
                    startedExtraLap = true;
                }
                if (!gotHalfTime)
                {
                    Console.WriteLine("Session time remaining = " + timeLeft);
                    halfTime = timeLeft / 2;
                    gotHalfTime = true;
                    if (currentGameState.FuelData.FuelUseActive)
                    {
                        // don't allow the half way message to play if fuel use is active - there's already one in there
                        playedHalfWayHome = true;
                    }
                }
                PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps > 1)
                {
                    pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                    if (currentGameState.SessionData.ClassPosition < 4)
                    {
                        pearlType = PearlsOfWisdom.PearlType.GOOD;
                    }
                    else if (currentGameState.SessionData.ClassPosition > currentGameState.SessionData.SessionStartClassPosition + 5 &&
                        !currentGameState.PitData.OnOutLap && !currentGameState.PitData.InPitlane &&
                        currentGameState.SessionData.LapTimePrevious > currentGameState.TimingData.getPlayerBestLapTime() &&
                        // yuk... AC SessionStartPosition is suspect so don't allow "you're shit" messages based on it.
                        CrewChief.gameDefinition.gameEnum != GameEnum.ASSETTO_32BIT && CrewChief.gameDefinition.gameEnum != GameEnum.ASSETTO_64BIT)
                    {
                        // don't play bad-pearl if we're on an out lap or are pitting
                        pearlType = PearlsOfWisdom.PearlType.BAD;
                    }
                }

                if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.IsNewLap &&
                    currentGameState.SessionData.SessionRunningTime > 60 && !playedLastLap)
                {
                    Boolean timeWillBeZeroAtEndOfLeadersLap = false;
                    if (currentGameState.SessionData.OverallPosition == 1)
                    {
                        float playerBest = currentGameState.TimingData.getPlayerBestLapTime();
                        timeWillBeZeroAtEndOfLeadersLap = timeLeft > 0 && playerBest > 0 &&
                            timeLeft < playerBest - 5;
                    }
                    else
                    {
                        OpponentData leader = currentGameState.getOpponentAtClassPosition(1, currentGameState.carClass);
                        timeWillBeZeroAtEndOfLeadersLap = leader != null && leader.isProbablyLastLap;
                    }
                    if ((addExtraLap && timeLeft <= 0) ||
                        (!addExtraLap && timeWillBeZeroAtEndOfLeadersLap)) {
                        playedLastLap = true;
                        played2mins = true;
                        played5mins = true;
                        played10mins = true;
                        played15mins = true;
                        played20mins = true;
                        playedHalfWayHome = true;
                        if (currentGameState.SessionData.ClassPosition == 1)
                        {
                            // don't add a pearl here - the audio clip already contains encouragement
                            audioPlayer.playMessage(new QueuedMessage(folderLastLapLeading, 0, this), pearlType, 0, 5);
                        }
                        else if (currentGameState.SessionData.ClassPosition < 4)
                        {
                            // don't add a pearl here - the audio clip already contains encouragement
                            audioPlayer.playMessage(new QueuedMessage(folderLastLapPodium, 0, this), pearlType, 0, 5);
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderLastLap, 0, this), 5);
                        }
                    }
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && timeLeft / 60 < 3 && timeLeft / 60 > 2.9)
                {
                    // disable pearls for the last part of the race
                    audioPlayer.disablePearlsOfWisdom = true;
                }
                // Console.WriteLine("Session time left = " + timeLeft + " SessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                if (!currentGameState.SessionData.HasExtraLap && 
                    currentGameState.SessionData.SessionRunningTime > 0 && !played0mins && timeLeft <= 0.2)
                {
                    played0mins = true;
                    played2mins = true;
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    audioPlayer.suspendPearlsOfWisdom();
                    // PCars hack - don't play this if it's an unlimited session - no lap limit and no time limit
                    if (!currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionNumberOfLaps <= 0)
                    {
                        Console.WriteLine("Skipping session end messages for unlimited session");
                    }
                    else if (currentGameState.SessionData.SessionType != SessionType.Race) 
                    {
                        // don't play the chequered flag message in race sessions
                        audioPlayer.playMessage(new QueuedMessage("session_complete",
                            MessageContents(folder0mins, Position.folderStub + currentGameState.SessionData.ClassPosition), 0, this), 10);
                    }
                } 
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played2mins && timeLeft / 60 < 2 && timeLeft / 60 > 1.9)
                {
                    played2mins = true;
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    audioPlayer.suspendPearlsOfWisdom();
                    audioPlayer.playMessage(new QueuedMessage(folder2mins, 0, this), 10);
                } if (currentGameState.SessionData.SessionRunningTime > 60 && !played5mins && timeLeft / 60 < 5 && timeLeft / 60 > 4.9)
                {
                    played5mins = true;
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    playedHalfWayHome = true;
                    if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.ClassPosition == 1)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.playMessage(new QueuedMessage(folder5minsLeading, 0, this), pearlType, 0, 5);
                    }
                    else if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.ClassPosition < 4)
                    {
                        // don't add a pearl here - the audio clip already contains encouragement
                        audioPlayer.playMessage(new QueuedMessage(folder5minsPodium, 0, this), pearlType, 0, 5);
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage(folder5mins, 0, this), pearlType, 0.7, 5);
                    }
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played10mins && timeLeft / 60 < 10 && timeLeft / 60 > 9.9)
                {
                    played10mins = true;
                    played15mins = true;
                    played20mins = true;
                    audioPlayer.playMessage(new QueuedMessage(folder10mins, 0, this), pearlType, 0.7, 3);
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played15mins && timeLeft / 60 < 15 && timeLeft / 60 > 14.9)
                {
                    played15mins = true;
                    played20mins = true;
                    audioPlayer.playMessage(new QueuedMessage(folder15mins, 0, this), pearlType, 0.7, 3);
                }
                if (currentGameState.SessionData.SessionRunningTime > 60 && !played20mins && timeLeft / 60 < 20 && timeLeft / 60 > 19.9)
                {
                    played20mins = true;
                    audioPlayer.playMessage(new QueuedMessage(folder20mins, 0, this), pearlType, 0.7, 3);
                }
                else if (currentGameState.SessionData.SessionType == SessionType.Race &&
                    currentGameState.SessionData.SessionRunningTime > 60 && !playedHalfWayHome && timeLeft > 0 && timeLeft < halfTime)
                {
                    // this one sounds weird in practice and qual sessions, so skip it
                    playedHalfWayHome = true;
                    audioPlayer.playMessage(new QueuedMessage(folderHalfWayHome, 0, this), pearlType, 0.7, 3);
                }
            }
        }

        public override void respond(string voiceMessage)
        {
            if (sessionLengthIsTime)
            {
                if (leaderHasFinishedRace)
                {
                    Console.WriteLine("Playing last lap message, timeleft = " + timeLeft);
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderThisIsTheLastLap, 0, null));                    
                }
                if (timeLeft >= 120)
                {
                    int minutesLeft = (int)Math.Round(timeLeft / 60f);
                    audioPlayer.playMessageImmediately(new QueuedMessage("RaceTime/time_remaining",
                        MessageContents(TimeSpanWrapper.FromMinutes(minutesLeft, Precision.MINUTES), folderRemaining), 0, null));                    
                }
                else if (timeLeft >= 60)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderOneMinuteRemaining, 0, null));                    
                }
                else if (timeLeft <= 0)
                {
                    if (addExtraLap && !startedExtraLap)
                    {
                        Console.WriteLine("Playing extra lap one more lap message, timeleft = " + timeLeft);
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderOneLapAfterThisOne, 0, null));                        
                    }
                    else 
                    {
                        Console.WriteLine("Playing last lap message, timeleft = " + timeLeft);
                        audioPlayer.playMessageImmediately(new QueuedMessage(folderThisIsTheLastLap, 0, null));                        
                    }                   
                }
                else if (timeLeft < 60)
                {
                    Console.WriteLine("Playing less than a minute message, timeleft = " + timeLeft);
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderLessThanOneMinute, 0, null));                    
                }
            }
            else
            {
                if (lapsLeft > 2)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("RaceTime/laps_remaining",
                        MessageContents(lapsLeft, folderLapsLeft), 0, null));
                }
                else if (lapsLeft == 2)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderOneLapAfterThisOne, 0, null));
                }
                else if (lapsLeft == 1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderThisIsTheLastLap, 0, null));                    
                }
            }     
        }
    }
}
