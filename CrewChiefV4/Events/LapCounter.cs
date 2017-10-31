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

        public static String folderGreenGreenGreen = "lap_counter/green_green_green";

        // used in manual rolling starts (hack...)
        public static String folderLeaderHasCrossedStartLine = "lap_counter/leader_has_crossed_start_line";
        // used in manual rolling starts - if we pass someone play a message. We don't know if we've passed someone because of
        // a disconnection or because we've actually overtaken, so err on the side of caution here
        public static String folderHoldYourPosition = "lap_counter/hold_your_position";
        public static String folderGivePositionBack = "lap_counter/give_that_position_back";
        public static String folderManualStartInitialIntro = "lap_counter/ok_youre_in";
        public static String folderManualStartInitialOutroNoDriverName = "lap_counter/hold_this_position_until_start_line";
        public static String folderManualStartInitialOutroWithDriverName1 = "lap_counter/hold_position_behind";
        public static String folderManualStartInitialOutroWithDriverName2 = "lap_counter/until_start_line";

        // toggle / request acknowledgements when enabling / disabling manual formation lap mode
        public static String folderManualFormationLapModeEnabled = "lap_counter/manual_formation_lap_mode_enabled";
        public static String folderManualFormationLapModeDisabled = "lap_counter/manual_formation_lap_mode_disabled";

        // some folks might want to start racing when the leader crosses the line, others might not be allowed to overtake
        // until their car crosses the line
        private Boolean manualFormationGoWhenLeaderCrossesLine = UserSettings.GetUserSettings().getBoolean("manual_formation_go_with_leader");

        private Boolean playedManualStartGetReady = false;
        private Boolean playedManualStartLeaderHasCrossedLine = false;
        private Boolean playedManualStartPlayedGoGoGo = false;
        private Boolean playedManualStartInitialMessage = false;
        private OpponentData manualStartOpponentAhead = null;

        // used by the FrozenOrder event
        public static String folderGetReady = "lap_counter/get_ready";

        private String folderLastLapEU = "lap_counter/last_lap";

        private String folderLastLapUS = "lap_counter/white_flag_last_lap";

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

        private Boolean playPreRaceMessagesUntilCancelled = UserSettings.GetUserSettings().getBoolean("play_pre_lights_messages_until_cancelled");

        private Boolean enableGreenLightMessages = UserSettings.GetUserSettings().getBoolean("enable_green_light_messages");

        private static Boolean useFahrenheit = UserSettings.GetUserSettings().getBoolean("use_fahrenheit");

        private DateTime nextManualFormationOvertakeWarning = DateTime.MinValue;

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

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            // this validates that the 'give position back' messasge only so we don't care what's in the validationData. only that it is not null
            if (validationData != null && manualStartOpponentAhead != null)
            {
                OpponentData currentCarAhead = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, true);
                if (currentCarAhead != null && currentCarAhead.DriverRawName.Equals(manualStartOpponentAhead.DriverRawName))
                {
                    // the opponent in front is who should be in front, so the message is invalid
                    return false;
                }
            }
            return true;
        }

        public override void clearState()
        {
            playedGetReady = false;
            playedFinished = false;
            playedPreLightsMessage = false;
            purgePreLightsMessages = false;
            // assume we start on the formation lap if we're using a manual formation lap
            GameStateData.onManualFormationLap = GameStateData.useManualFormationLap;
            playedManualStartGetReady = false;
            playedManualStartLeaderHasCrossedLine = false;
            playedManualStartInitialMessage = false;
            playedManualStartPlayedGoGoGo = false;
            nextManualFormationOvertakeWarning = DateTime.MinValue;
            manualStartOpponentAhead = null;
        }

        private void playPreLightsMessage(GameStateData currentGameState, int maxNumberToPlay)
        {
            playedPreLightsMessage = true;
           /* if ((CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT || 
                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT) && currentGameState.SessionData.SessionNumberOfLaps <= 0 && 
                !playPreRaceMessagesUntilCancelled) 
            {
                // don't play pre-lights messages in PCars if the race is a fixed time, rather than number of laps
                return;
            }*/
            CrewChiefV4.GameState.Conditions.ConditionsSample currentConditions = currentGameState.Conditions.getMostRecentConditions();
            List<QueuedMessage> possibleMessages = new List<QueuedMessage>();
            if (currentConditions != null)
            {
                Console.WriteLine("pre-start message for track temp");
                possibleMessages.Add(new QueuedMessage("trackTemp", MessageContents(ConditionsMonitor.folderTrackTempIs,
                    convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                Console.WriteLine("pre-start message for air temp");
                possibleMessages.Add(new QueuedMessage("air_temp", MessageContents(ConditionsMonitor.folderAirTempIs,
                    convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this));
            }
            if (currentGameState.PitData.HasMandatoryPitStop && CrewChief.gameDefinition.gameEnum != GameEnum.RF1 && CrewChief.gameDefinition.gameEnum != GameEnum.RF2_64BIT)
            {
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    Console.WriteLine("pre-start message for mandatory stop time");
                    possibleMessages.Add(new QueuedMessage("pit_window_time", MessageContents(MandatoryPitStops.folderMandatoryPitStopsPitWindowOpensAfter,
                        TimeSpan.FromMinutes(currentGameState.PitData.PitWindowStart)), 0, this));
                } 
                else
                {
                    Console.WriteLine("pre-start message for mandatory stop lap");
                    possibleMessages.Add(new QueuedMessage("pit_window_lap", MessageContents(MandatoryPitStops.folderMandatoryPitStopsPitWindowOpensOnLap,
                        currentGameState.PitData.PitWindowStart), 0, this));
                }
            }
            if (currentGameState.SessionData.Position == 1)
            {
                Console.WriteLine("pre-start message for pole");
                if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                {
                    possibleMessages.Add(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro, Position.folderPole), 0, this));                    
                }
                else
                {
                    possibleMessages.Add(new QueuedMessage("position", MessageContents(Pause(200), Position.folderPole), 0, this));
                }                
            }
            else
            {
                Console.WriteLine("pre-start message for P " + currentGameState.SessionData.Position);
                if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                {
                    possibleMessages.Add(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro, 
                        Position.folderStub + currentGameState.SessionData.Position), 0, this));
                }
                else 
                {
                    possibleMessages.Add(new QueuedMessage("position", MessageContents(Pause(200), Position.folderStub + currentGameState.SessionData.Position), 0, this));
                }
            }
            if (currentGameState.SessionData.SessionNumberOfLaps > 0) {
                // check how long the race is
                if (currentGameState.SessionData.Position > 3 && currentGameState.SessionData.TrackDefinition.trackLength * currentGameState.SessionData.SessionNumberOfLaps < 20000)
                {
                    // anything less than 20000 metres worth of racing (e.g. 10 laps of Brands Indy) should have a 'make them count'
                    Console.WriteLine("pre-start message for race laps + get on with it");
                    possibleMessages.Add(new QueuedMessage("race_distance", MessageContents(currentGameState.SessionData.SessionNumberOfLaps, folderLapsMakeThemCount), 0, this));
                }
                else
                {
                    Console.WriteLine("pre-start message for race laps");

                    // TODO: need to add a "laps..." message here otherwise it just plays the number. Add the "make them count" message until this is available
                    possibleMessages.Add(new QueuedMessage("race_distance", MessageContents(currentGameState.SessionData.SessionNumberOfLaps, folderLapsMakeThemCount), 0, this));
                }
            } else if (currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTotalRunTime < 1800) 
            {
                int minutes = (int)currentGameState.SessionData.SessionTotalRunTime / 60;
                if (currentGameState.SessionData.Position > 3 && minutes < 20)
                {
                    Console.WriteLine("pre-start message for race time + get on with it");                   
                    possibleMessages.Add(new QueuedMessage("race_time", MessageContents(minutes, folderMinutesYouNeedToGetOnWithIt), 0, this));
                }
                else
                {
                    Console.WriteLine("pre-start message for race time");
                    possibleMessages.Add(new QueuedMessage("race_time", MessageContents(minutes), 0, this));
                }
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
            if (GameStateData.useManualFormationLap)
            {
                // when the session is first cleared, this will be true if we're using manual formation laps:
                if (GameStateData.onManualFormationLap) 
                {
                    // when the lights change to green, give some info:
                    if (!playedManualStartInitialMessage && previousGameState != null &&
                        currentGameState.SessionData.SessionType == SessionType.Race &&
                        currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                        (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                         previousGameState.SessionData.SessionPhase == SessionPhase.Countdown))
                    {
                        playManualStartInitialMessage(currentGameState);
                    }
                    // don't bother with any other messages until things have had a few seconds to settle down:
                    else if (currentGameState.SessionData.SessionRunningTime > 10)
                    {
                        checkForIllegalPassesOnFormationLap(currentGameState);
                        checkForManualFormationRaceStart(currentGameState, currentGameState.SessionData.Position == 1);
                    }
                }
                // now check if we really are on a manual formation lap. We have to do this *after* checking for the race start (above) because
                // this will switch manual formation lap stuff off as soon as we cross the line (so would suppress the 'green green green' message).
                // We want to ensure it's switched off if we're not in a race session, for obvious reasons.
                GameStateData.onManualFormationLap = currentGameState.SessionData.SessionType == SessionType.Race && !playedManualStartPlayedGoGoGo;
            }
            else
            {
                // nasty... 2 separate code paths here - one for the existing pre-lights logic which is a ball of spaghetti I don't fancy unpicking, 
                // one for the 'cancel on control input' version
                if (playPreRaceMessagesUntilCancelled)
                {
                    if (!playedGetReady)
                    {
                        // in the pre-lights phase
                        if (purgePreLightsMessages)
                        {
                            // empty the queue and play 'get ready'
                            int purgedCount = audioPlayer.purgeQueues();
                            Console.WriteLine("Purging pre-lights messages, removed = " + purgedCount + " messages");
                            audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this));
                            playedGetReady = true;
                        }
                        else
                        {
                            if (playedPreLightsMessage && !purgePreLightsMessages)
                            {
                                // we've started playing the pre-lights messages. As soon as the play makes a throttle input purge this queue.
                                // Some games hold the brake at '1' automatically on the grid, so this can't be used
                                purgePreLightsMessages = previousGameState != null &&
                                    currentGameState.ControlData.ThrottlePedal > 0.2 && previousGameState.ControlData.ThrottlePedal > 0.2;
                            }
                            else
                            {
                                // Play these only for race sessions. Some game-specific rules here:
                                //      Allow messages for countdown phase for any game
                                //      Allow messages for gridwalk phase for any game *except* Raceroom (which treats gridwalk as its own session with different data to the race session)
                                //      Allow messages for formation phase for Raceroom
                                //      Allow messages for formation phase for RF1 (AMS) and rF2 only when we enter sector 3 of the formation lap.
                                if (currentGameState.SessionData.SessionType == SessionType.Race &&
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown ||
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk && CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM) ||
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM) ||
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && (CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT) &&
                                        currentGameState.SessionData.SectorNumber == 3)))
                                {
                                    Console.WriteLine("Queuing pre-lights messages");
                                    playPreLightsMessage(currentGameState, 10); // queue as many messages as we have here, in any order
                                }
                            }
                        }
                    }
                }
                else
                {
                    int preLightsMessageCount = CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT ||
                                                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                                                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 ||
                                                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK ||
                                                CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK ? 1 : 2;
                    if (!playedPreLightsMessage && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk &&
                        (playPreLightsInRaceroom || CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM))
                    {
                        playPreLightsMessage(currentGameState, preLightsMessageCount);
                        purgePreLightsMessages = true;
                    }
                    // TODO: in R3E online there's a GridWalk phase before the Countdown. In PCars they're combined. Add some messages to this phase.

                    // R3E's gridWalk phase isn't useable here - the data during this phase are bollocks
                    if (!playedGetReady && currentGameState.SessionData.SessionType == SessionType.Race && (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown ||
                        (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM) ||
                        // play 'get ready' message when entering sector 3 of formation lap in Automobilista and RF2
                        (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && (CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT) &&
                        currentGameState.SessionData.SectorNumber == 3)))
                    {
                        // If we've not yet played the pre-lights messages, just play one of them here, but not for RaceRoom as the lights will already have started
                        if (!playedPreLightsMessage && CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM)
                        {
                            playPreLightsMessage(currentGameState, preLightsMessageCount);
                            purgePreLightsMessages = false;
                        }
                        if (purgePreLightsMessages)
                        {
                            audioPlayer.purgeQueues();
                        }
                        audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this));
                        playedGetReady = true;
                    }
                }
                if (previousGameState != null && enableGreenLightMessages &&
                    currentGameState.SessionData.SessionType == SessionType.Race &&
                    currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                    (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                     previousGameState.SessionData.SessionPhase == SessionPhase.Countdown))
                {
                    // ensure we don't play 'get ready' or any pre-lights messages again
                    playedPreLightsMessage = true;
                    playedGetReady = true;
                    int purgeCount = audioPlayer.purgeQueues();
                    if (purgeCount > 0)
                    {
                        Console.WriteLine("Purged " + purgeCount + " outstanding messages at green light");
                    }
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenGreenGreen, 0, this));
                    audioPlayer.disablePearlsOfWisdom = false;
                }
            }
            // end of start race stuff

            // looks like belt n braces but there's a bug in R3E DTM 2015 race 1 which has a number of laps and a time remaining
            if (!currentGameState.SessionData.SessionHasFixedTime &&
                currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.IsNewLap && currentGameState.SessionData.CompletedLaps > 0)
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
                    Console.WriteLine("1 lap remaining, SessionHasFixedTime = " + currentGameState.SessionData.SessionHasFixedTime);
                    if (position == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLapLeading, 0, this));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLapTopThree, 0, this));
                    }
                    else if (position > 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderLastLapUS : folderLastLapEU, 0, this));
                    }
                    else
                    {
                        Console.WriteLine("1 lap left but position is < 1");
                    }
                }
                else if (currentGameState.SessionData.CompletedLaps == currentGameState.SessionData.SessionNumberOfLaps - 2)
                {
                    Console.WriteLine("2 laps remaining, SessionHasFixedTime = " + currentGameState.SessionData.SessionHasFixedTime);
                    if (position == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeftLeading, 0, this));
                    }
                    else if (position < 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeftTopThree, 0, this));
                    }
                    else if (position >= currentGameState.SessionData.SessionStartPosition + 5)
                    {
                        // yuk... don't yell at the player for being shit if he's play Assetto. Because assetto drivers *are* shit, and also the SessionStartPosition
                        // might be invalid so perhaps they're really not being shit. At the moment.
                        if (CrewChief.gameDefinition.gameEnum != GameEnum.ASSETTO_32BIT && CrewChief.gameDefinition.gameEnum != GameEnum.ASSETTO_64BIT)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.BAD, 0.5);
                        }
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, 0.5);
                    }
                    else
                    {
                        Console.WriteLine("2 laps left but position is < 1");
                    }
                    // 2 laps left, so prevent any further pearls of wisdom being added
                }
                
            }
        }

        private void playManualStartGreenFlag()
        {
            if (!playedManualStartPlayedGoGoGo)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenGreenGreen, 0, this));
            }
            GameStateData.onManualFormationLap = false;
            // switch off the other updates
            playedManualStartPlayedGoGoGo = true;
            playedManualStartLeaderHasCrossedLine = true;
            playedManualStartGetReady = true;
            playedManualStartInitialMessage = true;
        }

        private void playManualStartGetReady()
        {
            audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this));
            playedManualStartGetReady = true;
        }

        private void playManualStartInitialMessage(GameStateData currentGameState)
        {
            manualStartOpponentAhead = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, true);
            // use the driver name in front if we have it - if we're starting on pole the manualStartOpponentAhead var will be null,
            // which will force the audio player to use the secondary message

            if (manualFormationGoWhenLeaderCrossesLine)
            {
                // go when leader crosses line, so make sure we don't say "hold position until the start line"
                audioPlayer.playMessage(new QueuedMessage("manual_start_intro",
                    MessageContents(folderManualStartInitialIntro,
                        Position.folderStub + currentGameState.SessionData.Position, folderManualStartInitialOutroWithDriverName1,
                        manualStartOpponentAhead),
                    MessageContents(folderManualStartInitialIntro,
                        Position.folderStub + currentGameState.SessionData.Position, folderHoldYourPosition), 0, this));
            }
            else
            {
                audioPlayer.playMessage(new QueuedMessage("manual_start_intro",
                    MessageContents(folderManualStartInitialIntro,
                        Position.folderStub + currentGameState.SessionData.Position, folderManualStartInitialOutroWithDriverName1,
                        manualStartOpponentAhead, folderManualStartInitialOutroWithDriverName2),
                    MessageContents(folderManualStartInitialIntro,
                        Position.folderStub + currentGameState.SessionData.Position, folderManualStartInitialOutroNoDriverName), 0, this));
            }
            playedManualStartInitialMessage = true;
        }

        private void checkForIllegalPassesOnFormationLap(GameStateData currentGameState)
        {
            if (GameStateData.onManualFormationLap && currentGameState.SessionData.SessionStartPosition > currentGameState.SessionData.Position &&
                            nextManualFormationOvertakeWarning < currentGameState.Now)
            {
                // we've overtaken someone
                nextManualFormationOvertakeWarning = currentGameState.Now.AddSeconds(30);
                // if the number of cars in the session has reduced, just play a 'hold your position' message - 
                // perhaps someone disconnected in front.
                if (currentGameState.SessionData.NumCarsAtStartOfSession > currentGameState.SessionData.NumCars)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderHoldYourPosition, 0, this));
                }
                else
                {
                    // check if the car in front has changed
                    OpponentData currentOpponentInFront = currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, true);
                    if (manualStartOpponentAhead != null &&
                        (currentOpponentInFront == null || !manualStartOpponentAhead.DriverRawName.Equals(currentOpponentInFront.DriverRawName)))
                    {
                        // delay and validate this message so we don't grumble about a different car in front if a couple of
                        // cars fall back through the field for whatever reason
                        audioPlayer.playMessage(new QueuedMessage("give_position_back",
                            MessageContents(folderGivePositionBack, folderManualStartInitialOutroWithDriverName1, manualStartOpponentAhead),
                            MessageContents(folderGivePositionBack), 5, this, new Dictionary<String, Object>()));
                    }
                }
            }
        }

        private void checkForManualFormationRaceStart(GameStateData currentGameState, Boolean isLeader)
        {
            if (isLeader)
            {
                // we're the leader, so play 'go' when we cross the line and get ready when we're near the line
                if (currentGameState.SessionData.CompletedLaps == 1)
                {
                    playManualStartGreenFlag();
                }
                else if (!playedManualStartGetReady && currentGameState.SessionData.SectorNumber == 3 &&
                    currentGameState.PositionAndMotionData.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.trackLength - 200)
                {
                    playManualStartGetReady();
                }
            }
            else
            {
                if (manualFormationGoWhenLeaderCrossesLine)
                {
                    // here we're only interested in what the leader is up to
                    OpponentData leader = currentGameState.getOpponentAtPosition(1, false);
                    if (leader != null)
                    {
                        if (!playedManualStartGetReady && leader.CurrentSectorNumber == 3 &&
                            leader.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.trackLength - 200)
                        {
                            playManualStartGetReady();
                        }
                        else if (leader.CompletedLaps == 1)
                        {
                            playManualStartGreenFlag();
                        }
                    }
                }
                else
                {
                    // give leader updates then green when we cross the line
                    if (currentGameState.SessionData.CompletedLaps == 1)
                    {
                        playManualStartGreenFlag();
                    }
                    else if (!playedManualStartGetReady && currentGameState.SessionData.SectorNumber == 3 &&
                            currentGameState.PositionAndMotionData.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.trackLength - 300)
                    {
                        playManualStartGetReady();
                    }
                    else if (currentGameState.SessionData.Position > 3)
                    {
                        // don't say "leader has crossed the line" if we're right behind him - this would delay the 'go go go' call
                        OpponentData leader = currentGameState.getOpponentAtPosition(1, false);
                        if (leader != null && !playedManualStartLeaderHasCrossedLine && leader.CompletedLaps == 1)
                        {
                            playedManualStartLeaderHasCrossedLine = true;
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderLeaderHasCrossedStartLine, 0, this));
                        }
                    }
                }
            }
        }
    }
}
