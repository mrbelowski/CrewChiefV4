using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class LapCounter : AbstractEvent
    {
        // can't play these messages as the GridWalk phase has bollocks data :(
        private Boolean playPreLightsInRaceroom = false;

        private Random rand = new Random();

        private String folderGreenGreenGreen = "lap_counter/green_green_green";

        public static String folderGetReady = "lap_counter/get_ready";

        private String folderLastLap = "lap_counter/last_lap";

        private String folderTwoLeft = "lap_counter/two_to_go";

        private String folderLastLapLeading = "lap_counter/last_lap_leading";

        private String folderLastLapTopThree = "lap_counter/last_lap_top_three";

        private String folderTwoLeftLeading = "lap_counter/two_to_go_leading";

        private String folderTwoLeftTopThree = "lap_counter/two_to_go_top_three";

        private String folderLapsMakeThemCount = "lap_counter/laps_make_them_count";
        private String folderMinutesYouNeedToGetOnWithIt = "lap_counter/minutes_you_need_to_get_on_with_it";
        
        private Boolean playedGetReady;

        private Boolean playedPreLightsMessage;

        private Boolean purgePreLightsMessages;

        public Boolean playedFinished;

        private DateTime lastFinishMessageTime = DateTime.MinValue;

        private Boolean enableGreenLightMessages = UserSettings.GetUserSettings().getBoolean("enable_green_light_messages");

        private static Boolean useFahrenheit = UserSettings.GetUserSettings().getBoolean("use_fahrenheit");

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Countdown, SessionPhase.Formation, SessionPhase.Gridwalk, SessionPhase.Green, SessionPhase.Checkered, SessionPhase.Finished }; }
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public LapCounter(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            playedGetReady = false;
            playedFinished = false;
            playedPreLightsMessage = false;
            purgePreLightsMessages = false;
        }

        public override bool isMessageStillValid(String eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            return applicableSessionPhases.Contains(currentGameState.SessionData.SessionPhase);
        }

        private void playPreLightsMessage(GameStateData currentGameState, int maxNumberToPlay)
        {
            playedPreLightsMessage = true;
            if ((CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT || 
                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT) && currentGameState.SessionData.SessionNumberOfLaps <= 0) 
            {
                // don't play pre-lights messages in PCars if the race is a fixed time, rather than number of laps
                return;
            }
            CrewChiefV4.GameState.Conditions.ConditionsSample currentConditions = currentGameState.Conditions.getMostRecentConditions();
            List<QueuedMessage> possibleMessages = new List<QueuedMessage>();
            if (currentConditions != null)
            {
                possibleMessages.Add(new QueuedMessage("trackTemp", MessageContents(ConditionsMonitor.folderTrackTempIs,
                    convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, null));
                possibleMessages.Add(new QueuedMessage("air_temp", MessageContents(ConditionsMonitor.folderAirTempIs,
                    convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, null));
            }
            if (currentGameState.PitData.HasMandatoryPitStop)
            {
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    possibleMessages.Add(new QueuedMessage("pit_window_time", MessageContents(MandatoryPitStops.folderMandatoryPitStopsPitWindowOpensAfter,
                        TimeSpan.FromMinutes(currentGameState.PitData.PitWindowStart)), 0, this));
                } 
                else
                {
                    possibleMessages.Add(new QueuedMessage("pit_window_lap", MessageContents(MandatoryPitStops.folderMandatoryPitStopsPitWindowOpensOnLap,
                        currentGameState.PitData.PitWindowStart), 0, this));
                }
            }
            if (currentGameState.SessionData.Position == 1)
            {
                possibleMessages.Add(new QueuedMessage(Position.folderPole, 0, this));
            }
            else
            {
                Console.WriteLine("pre-start message for P " + currentGameState.SessionData.Position);
                possibleMessages.Add(new QueuedMessage(Position.folderStub + currentGameState.SessionData.Position, 0, this));
            }
            if (currentGameState.SessionData.SessionNumberOfLaps > 0) {
                possibleMessages.Add(new QueuedMessage("race_distance", MessageContents(
                        currentGameState.SessionData.SessionNumberOfLaps, folderLapsMakeThemCount), 0, this));
            } else if (currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionRunTime > 0 && currentGameState.SessionData.SessionRunTime < 1800 &&
                    currentGameState.SessionData.Position > 3) {
                int minutes = (int)currentGameState.SessionData.SessionRunTime / 60;
                possibleMessages.Add(new QueuedMessage("race_time", MessageContents(
                        minutes, folderMinutesYouNeedToGetOnWithIt), 0, this));
            }
            // now pick a random selection
            if (possibleMessages.Count > 0)
            {
                int played = 0;
                var shuffled = possibleMessages.OrderBy(item => rand.Next());
                foreach (var message in shuffled)
                {
                    played++;
                    if (played > maxNumberToPlay)
                    {
                        break;
                    }
                    audioPlayer.playMessage(message);
                }            
            }
            // TODO: in the countdown / pre-lights phase, we don't know how long the race is going to be so we can't use the 'get on with it' messages :(
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!playedPreLightsMessage && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk && 
                (playPreLightsInRaceroom || CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM))
            {
                playPreLightsMessage(currentGameState, 3);
                playedPreLightsMessage = true;
                purgePreLightsMessages = true;
            }
            // TODO: in R3E online there's a GridWalk phase before the Countdown. In PCars they're combined. Add some messages to this phase.

            // R3E's gridWalk phase isn't useable here - the data during this phase are bollocks
            if (!playedGetReady && currentGameState.SessionData.SessionType == SessionType.Race && (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown ||
                (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)))
            {
                // If we've not yet played the pre-lights messages, just play one of them here, but not for RaceRoom as the lights will already have started
                if (!playedPreLightsMessage && CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM)
                {
                    playedPreLightsMessage = true;
                    playPreLightsMessage(currentGameState, 2);
                    purgePreLightsMessages = false;
                }
                if (purgePreLightsMessages)
                {
                    audioPlayer.purgeQueues();
                }
                audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this));
                playedGetReady = true;
            }
            /*if (!playedGreenGreenGreen && previousGameState != null && currentGameState.SessionData.SessionType == SessionType.Race &&
                (currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                    (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                     previousGameState.SessionData.SessionPhase == SessionPhase.Countdown)))
            {*/
            if (previousGameState != null && enableGreenLightMessages && 
                currentGameState.SessionData.SessionType == SessionType.Race &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Green && 
                (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                 previousGameState.SessionData.SessionPhase == SessionPhase.Countdown))
            {
                audioPlayer.removeQueuedMessage(folderGetReady);
                audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenGreenGreen, 0, this));
                
                audioPlayer.disablePearlsOfWisdom = false;
            }
            if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0)
            {
                // a new lap has been started in race mode
                if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 2)
                {
                    // disable pearls for the last part of the race
                    audioPlayer.disablePearlsOfWisdom = true;
                }
                int position = currentGameState.SessionData.Position;
                if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 1)
                {
                    if (position == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLapLeading, 0, this));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLapTopThree, 0, this));
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLap, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (position >= 10)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLap, 0, this), PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("1 lap left but position is < 1");
                    }
                }
                else if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 2)
                {
                    if (position == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeftLeading, 0, this));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeftTopThree, 0, this));
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else if (position >= 10)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.BAD, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("2 laps left but position is < 1");
                    }
                    // 2 laps left, so prevent any further pearls of wisdom being added

                }
            }
        }
    }
}
