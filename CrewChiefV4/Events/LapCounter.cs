using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class LapCounter : AbstractEvent
    {
        // can't play these messages as the GridWalk phase has bollocks data :(
        private Boolean playPreLightsInRaceroom = false;

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
        public static String folderManualStartRejoinAtBack = "lap_counter/rejoin_at_back";
        public static String folderManualStartFormUpBehind = "lap_counter/form_up_behind";
        public static String folderManualStartStartingOnLeftBehind = "lap_counter/starting_in_left_lane_behind";
        public static String folderManualStartStartingOnRightBehind = "lap_counter/starting_in_right_lane_behind";
        public static String folderManualStartLeaderHasGone = "lap_counter/leader_has_gone";

        // toggle / request acknowledgements when enabling / disabling manual formation lap mode
        public static String folderManualFormationLapModeEnabled = "lap_counter/manual_formation_lap_mode_enabled";
        public static String folderManualFormationLapModeDisabled = "lap_counter/manual_formation_lap_mode_disabled";

        // some folks might want to start racing when the leader crosses the line, others might not be allowed to overtake
        // until their car crosses the line
        private Boolean manualFormationGoWhenLeaderCrossesLine = UserSettings.GetUserSettings().getBoolean("manual_formation_go_with_leader");
        private Boolean manualFormationDoubleFile = UserSettings.GetUserSettings().getBoolean("manual_formation_double_file");

        private Boolean playedManualStartGetReady = false;
        private Boolean playedManualStartLeaderHasCrossedLine = false;
        private Boolean playedManualStartPlayedGoGoGo = false;
        private Boolean playedManualStartInitialMessage = false;

        private float distanceBeforeLineToStartLeaderAccelerationCheck = 200f;
        private float leaderSpeedAtAccelerationCheckStart = -1;
        private OpponentData poleSitter = null;
        private Boolean leaderHasGone = false;

        private String manualStartOpponentToFollow = null;

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
        private int currentPosition = -1;
        private float currentSessionTotalRunTime = -1.0f;

        // special case for LapCounter - needs access to the CrewChief class to interrogate the spotter
        private CrewChief crewChief;

        private GridSide gridSide = GridSide.UNKNOWN;

        private Dictionary<String, GridSide> opponentsInFrontGridSides = new Dictionary<string, GridSide>();

        private int manualFormationStartingPosition = 0;

        private Boolean playedRejoinAtBackMessage = false;

        private Boolean playedPreLightsRollingStartWarning = false;

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Countdown, SessionPhase.Formation, SessionPhase.Gridwalk, SessionPhase.Green, SessionPhase.Checkered, SessionPhase.Finished }; }
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public LapCounter(AudioPlayer audioPlayer, CrewChief crewChief)
        {
            this.audioPlayer = audioPlayer;
            this.crewChief = crewChief;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            // this validates that the 'give position back' message
            if (validationData != null) 
            { 
                OpponentData opponentWeOvertook = getOpponent(currentGameState, (String) validationData["OpponentToFollowName"]);
                if (opponentWeOvertook != null && currentGameState.SessionData.ClassPosition > opponentWeOvertook.ClassPosition
                    && !hasOpponentDroppedBack((int) validationData["PlayerPosition"], opponentWeOvertook))
                {
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
            manualStartOpponentToFollow = null;
            gridSide = GridSide.UNKNOWN;
            manualFormationStartingPosition = 0;
            playedRejoinAtBackMessage = false;
            playedPreLightsRollingStartWarning = false;
            opponentsInFrontGridSides = new Dictionary<string, GridSide>();
            leaderSpeedAtAccelerationCheckStart = -1;
            poleSitter = null;
            leaderHasGone = false;
        }

        private OpponentData getOpponent(GameStateData currentGameState, String opponentName)
        {
            OpponentData opponent = null;
            if (opponentName != null && currentGameState.OpponentData.TryGetValue(opponentName, out opponent))
            {
                return opponent;
            }
            return null;
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
                Console.WriteLine("Pre-start message for track temp");
                possibleMessages.Add(new QueuedMessage("trackTemp", MessageContents(ConditionsMonitor.folderTrackTempIs,
                    convertTemp(currentConditions.TrackTemperature), getTempUnit()), 0, this));
                Console.WriteLine("Pre-start message for air temp");
                possibleMessages.Add(new QueuedMessage("air_temp", MessageContents(ConditionsMonitor.folderAirTempIs,
                    convertTemp(currentConditions.AmbientTemperature), getTempUnit()), 0, this));
            }
            if (currentGameState.PitData.HasMandatoryPitStop && CrewChief.gameDefinition.gameEnum != GameEnum.RF1 && CrewChief.gameDefinition.gameEnum != GameEnum.RF2_64BIT)
            {
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    Console.WriteLine("Pre-start message for mandatory stop time");
                    possibleMessages.Add(new QueuedMessage("pit_window_time", MessageContents(PitStops.folderMandatoryPitStopsPitWindowOpensAfter,
                        TimeSpanWrapper.FromMinutes(currentGameState.PitData.PitWindowStart, Precision.MINUTES)), 0, this));
                } 
                else
                {
                    Console.WriteLine("Pre-start message for mandatory stop lap");
                    possibleMessages.Add(new QueuedMessage("pit_window_lap", MessageContents(PitStops.folderMandatoryPitStopsPitWindowOpensOnLap,
                        currentGameState.PitData.PitWindowStart), 0, this));
                }
            }
            if (CrewChief.gameDefinition.gameEnum != GameEnum.RF2_64BIT)
            {
                if (currentGameState.SessionData.ClassPosition == 1)
                {
                    Console.WriteLine("Pre-start message for pole");
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
                    Console.WriteLine("Pre-start message for P " + currentGameState.SessionData.ClassPosition);
                    if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                    {
                        possibleMessages.Add(new QueuedMessage("position", MessageContents(Position.folderDriverPositionIntro,
                            Position.folderStub + currentGameState.SessionData.ClassPosition), 0, this));
                    }
                    else
                    {
                        possibleMessages.Add(new QueuedMessage("position", MessageContents(Pause(200), Position.folderStub + currentGameState.SessionData.ClassPosition), 0, this));
                    }
                }
            }
            else
            {
                // In rF2 there's a delay for position to get updated (usually by the end of a gridwalk).
                Console.WriteLine("Pre-start message for position (delay evaluated)");

                DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("getPositionMessages", new Object[] {
                    currentGameState.SessionData.ClassPosition }, this);
                possibleMessages.Add(new QueuedMessage("position", delayedMessageEvent, 1 /*secondsDelay*/, null));
            }
            if (currentGameState.SessionData.SessionNumberOfLaps > 0) {
                // check how long the race is
                if (currentGameState.SessionData.ClassPosition > 3 && currentGameState.SessionData.TrackDefinition != null && 
                    currentGameState.SessionData.TrackDefinition.trackLength > 0 && 
                    currentGameState.SessionData.TrackDefinition.trackLength * currentGameState.SessionData.SessionNumberOfLaps < 30000)
                {
                    // anything less than 20000 metres worth of racing (e.g. 10 laps of Brands Indy) should have a 'make them count'
                    Console.WriteLine("Pre-start message for race laps + get on with it");
                    possibleMessages.Add(new QueuedMessage("race_distance", MessageContents(currentGameState.SessionData.SessionNumberOfLaps, folderLapsMakeThemCount), 0, this));
                }
                else
                {
                    Console.WriteLine("Pre-start message for race laps");
                    // use the 'Laps' sound from Battery here
                    possibleMessages.Add(new QueuedMessage("race_distance", MessageContents(currentGameState.SessionData.SessionNumberOfLaps, Battery.folderLaps), 0, this));
                }
            }
            else if (CrewChief.gameDefinition.gameEnum != GameEnum.RF1 && /* session time in gridwalk phase isn't usable in rf1 */
                currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTotalRunTime < 1800) 
            {
                if (CrewChief.gameDefinition.gameEnum != GameEnum.RF2_64BIT)
                {
                    int minutes = (int)currentGameState.SessionData.SessionTotalRunTime / 60;
                    if (currentGameState.SessionData.ClassPosition > 3 && minutes < 20 && minutes > 1)
                    {
                        Console.WriteLine("Pre-start message for race time + get on with it");
                        possibleMessages.Add(new QueuedMessage("race_time", MessageContents(minutes, folderMinutesYouNeedToGetOnWithIt), 0, this));
                    }
                    //dont play pre-start message for race time unless its more then 2 minuts
                    else
                    {
                        Console.WriteLine("Pre-start message for race time");
                        possibleMessages.Add(new QueuedMessage("race_time", MessageContents(minutes, Battery.folderMinutes), 0, this));
                    }
                }
                else
                {
                    // In rF2 there's a delay for timed session total running time to get updated (usually by the end of a gridwalk).
                    Console.WriteLine("Pre-start message for race time (delay evaluated)");

                    DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("getSessionTotalRunningTimeTimeMessages", new Object[] {
                        currentGameState.SessionData.SessionTotalRunTime }, this);
                    possibleMessages.Add(new QueuedMessage("race_time", delayedMessageEvent, 1 /*secondsDelay*/, null));
                }

            }
            // now pick a random selection
            if (possibleMessages.Count > 0)
            {
                int played = 0;
                var shuffled = possibleMessages.OrderBy(item => Utilities.random.Next());
                foreach (var message in shuffled)
                {
                    played++;
                    if (played > maxNumberToPlay)
                    {
                        break;
                    }
                    audioPlayer.playMessage(message, 10);
                }
            }
        }

        public Tuple<List<MessageFragment>, List<MessageFragment>> getPositionMessages(int positionWhenQueued)
        {
            if (this.currentPosition != positionWhenQueued)
            {
                Console.WriteLine("Pre-start delay-evaluated pos updated from: " + positionWhenQueued + " to: " + this.currentPosition);
            }

            if (this.currentPosition == 1)
            {
                if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                {
                    return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(Position.folderDriverPositionIntro, Position.folderPole), null);;
                }
                else
                {
                    return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(Pause(200), Position.folderPole), null);;
                }
            }
            else
            {
                if (SoundCache.availableSounds.Contains(Position.folderDriverPositionIntro))
                {
                    return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(Position.folderDriverPositionIntro, Position.folderStub + this.currentPosition), null);;
                }
                else
                {
                    return new Tuple<List<MessageFragment>, List<MessageFragment>>(MessageContents(Position.folderStub + this.currentPosition), null); ;
                }
            }
        }

        public List<MessageFragment> getSessionTotalRunningTimeTimeMessages(float sessionTotalRunningTimeWnenQueued)
        {
            if (this.currentSessionTotalRunTime != sessionTotalRunningTimeWnenQueued)
            {
                Console.WriteLine("Pre-start delay-evaluated session running time updated from: " + sessionTotalRunningTimeWnenQueued.ToString("0.00") + " to: " + this.currentSessionTotalRunTime.ToString("0.00"));
            }

            int minutes = (int)this.currentSessionTotalRunTime / 60;
            if (this.currentPosition > 3 && minutes < 20 && minutes > 1)
            {
                Console.WriteLine("Pre-start message for race time + get on with it");
                return MessageContents(minutes, folderMinutesYouNeedToGetOnWithIt);
            }
            //dont play pre-start message for race time unless its more then 2 minuts
            else
            {
                Console.WriteLine("Pre-start message for race time");
                return MessageContents(minutes, Battery.folderMinutes);
            }
        }


        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            // Hack for delayed variables during rF2 session startup.
            this.currentPosition = currentGameState.SessionData.ClassPosition;
            this.currentSessionTotalRunTime = currentGameState.SessionData.SessionTotalRunTime;

            if (GameStateData.useManualFormationLap)
            {
                // when the session is first cleared, this will be true if we're using manual formation laps:
                if (GameStateData.onManualFormationLap)
                {
                    if (!playedPreLightsRollingStartWarning &&
                        !currentGameState.PitData.InPitlane &&
                        currentGameState.SessionData.SessionType == SessionType.Race &&
                        (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown ||
                         currentGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                         currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk))
                    {
                        playedPreLightsRollingStartWarning = true;
                        audioPlayer.playMessage(new QueuedMessage(FrozenOrderMonitor.folderRollingStartReminder, 0, this), 10);
                    }

                    // when the lights change to green, give some info:
                    if (!playedManualStartInitialMessage && previousGameState != null &&
                        !currentGameState.PitData.InPitlane &&
                        currentGameState.SessionData.SessionType == SessionType.Race &&
                        currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                        (previousGameState.SessionData.SessionPhase == SessionPhase.Formation ||
                         previousGameState.SessionData.SessionPhase == SessionPhase.Countdown))
                    {
                        playManualStartInitialMessage(currentGameState);
                    }
                    // don't bother with any other messages until things have had a few seconds to settle down:
                    else if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionRunningTime > 10
                        && currentGameState.SessionData.SessionPhase == SessionPhase.Green && !currentGameState.PitData.InPitlane)
                    {
                        checkForIllegalPassesOnFormationLap(currentGameState);
                        checkForManualFormationRaceStart(currentGameState, currentGameState.SessionData.OverallPosition == 1);
                        checkForManualDoubleFileReminder(previousGameState, currentGameState);
                        checkForLeaderHasAccelerated(previousGameState, currentGameState);
                    }
                }
                // now check if we really are on a manual formation lap. We have to do this *after* checking for the race start (above) because
                // this will switch manual formation lap stuff off as soon as we cross the line (so would suppress the 'green green green' message).
                // We want to ensure it's switched off if we're not in a race session, for obvious reasons.
                GameStateData.onManualFormationLap = currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps < 1;
            }
            /*else if (currentGameState.SessionData.StartType == StartType.Rolling)
            {

            }*/
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
                            audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this), 10);
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
                                //      Allow messages for formation phase for iRacing
                                //      Allow messages for formation phase for RF1 (AMS) and rF2 only when we enter sector 3 of the formation lap.
                                if (currentGameState.SessionData.SessionType == SessionType.Race &&
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Countdown ||
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk && CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM) ||
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM) ||
                                        (currentGameState.SessionData.SessionPhase == SessionPhase.Formation && CrewChief.gameDefinition.gameEnum == GameEnum.IRACING) ||
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
                    int preLightsMessageCount = 2;
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT ||
                        CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                        CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK ||
                        CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK)
                    {
                        preLightsMessageCount = 1;
                    }
                    else if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2)
                    {
                        preLightsMessageCount = 2 + Utilities.random.Next(6);
                    }
                    if (!playedPreLightsMessage && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionPhase == SessionPhase.Gridwalk &&
                        (playPreLightsInRaceroom || CrewChief.gameDefinition.gameEnum != GameEnum.RACE_ROOM))
                    {
                        playPreLightsMessage(currentGameState, preLightsMessageCount);
                        if (CrewChief.gameDefinition.gameEnum != GameEnum.RF2_64BIT || CrewChief.gameDefinition.gameEnum != GameEnum.IRACING)  // In rF2, Gridwalk/Countown phase is long enough to not purge messages.
                        {
                            purgePreLightsMessages = true;
                        }
                    }

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
                            playPreLightsMessage(currentGameState, 1);
                            purgePreLightsMessages = false;
                        }
                        if (purgePreLightsMessages)
                        {
                            audioPlayer.purgeQueues();
                        }
                        audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this), 10);
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
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenGreenGreen, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
                    audioPlayer.disablePearlsOfWisdom = false;
                }
            }
            // end of start race stuff

            // looks like belt n braces but there's a bug in R3E DTM 2015 race 1 which has a number of laps and a time remaining
            if (!currentGameState.SessionData.SessionHasFixedTime &&
                currentGameState.SessionData.SessionType == SessionType.Race &&
                currentGameState.SessionData.IsNewLap &&
                currentGameState.SessionData.CompletedLaps > 0 &&
                currentGameState.SessionData.SessionPhase != SessionPhase.Finished)
            {
                // a new lap has been started in race mode
                if (currentGameState.SessionData.SessionLapsRemaining == 2)
                {
                    // disable pearls for the last part of the race
                    audioPlayer.disablePearlsOfWisdom = true;
                }
                int position = currentGameState.SessionData.ClassPosition;
                if (currentGameState.SessionData.SessionLapsRemaining == 1 || currentGameState.SessionData.IsLastLap)  // Note: this might trigger in timed race.  Is this desired?
                {
                    Console.WriteLine("1 lap remaining");
                    if (position == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLapLeading, 0, this), 10);
                    }
                    else if (position < 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderLastLapTopThree, 0, this), 10);
                    }
                    else if (position > 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(GlobalBehaviourSettings.useAmericanTerms ? folderLastLapUS : folderLastLapEU, 0, this), 10);
                    }
                    else
                    {
                        Console.WriteLine("1 lap left but position is < 1");
                    }
                }
                else if (currentGameState.SessionData.SessionLapsRemaining == 2)
                {
                    Console.WriteLine("2 laps remaining");
                    if (position == 1)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeftLeading, 0, this), 10);
                    }
                    else if (position < 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeftTopThree, 0, this), 10);
                    }
                    else if (position >= currentGameState.SessionData.SessionStartClassPosition + 5 &&
                        currentGameState.SessionData.LapTimePrevious > currentGameState.TimingData.getPlayerBestLapTime())
                    {
                        // yuk... don't yell at the player for being shit if he's playing Assetto. Because assetto drivers *are* shit, and also the SessionStartClassPosition
                        // might be invalid so perhaps they're really not being shit. At the moment.
                        if (CrewChief.gameDefinition.gameEnum != GameEnum.ASSETTO_32BIT && CrewChief.gameDefinition.gameEnum != GameEnum.ASSETTO_64BIT)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.BAD, 0.5, 10);
                        }
                    }
                    else if (position >= 4)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderTwoLeft, 0, this), PearlsOfWisdom.PearlType.NEUTRAL, 0.5, 10);
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
                audioPlayer.playMessageImmediately(new QueuedMessage(folderGreenGreenGreen, 0, this) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
            }
            GameStateData.onManualFormationLap = false;
            // reset
            playedManualStartPlayedGoGoGo = false;
            playedManualStartLeaderHasCrossedLine = false;
            playedManualStartGetReady = false;
            playedManualStartInitialMessage = false;
            leaderSpeedAtAccelerationCheckStart = -1;
            poleSitter = null;
            leaderHasGone = false;
            manualStartOpponentToFollow = null;
            manualFormationStartingPosition = 0;
        }

        private void playManualStartGetReady()
        {
            audioPlayer.playMessage(new QueuedMessage(folderGetReady, 0, this), 10);
            playedManualStartGetReady = true;
        }

        private void setOpponentToFollowAndStartPosition(GameStateData currentGameState, Boolean playerHasCrashed, Boolean getGridSide)
        {
            this.manualFormationStartingPosition = playerHasCrashed ? currentGameState.SessionData.NumCarsOverall : currentGameState.SessionData.OverallPosition;
            if (manualFormationDoubleFile)
            {
                if (getGridSide)
                {
                    Tuple<GridSide, Dictionary<int, GridSide>> playerAndOpponentsInFrontGridSides = this.crewChief.getGridSide();
                    gridSide = playerAndOpponentsInFrontGridSides.Item1;
                    // match up the opponent grid sides to their names
                    foreach (int opponentPosition in playerAndOpponentsInFrontGridSides.Item2.Keys)
                    {
                        foreach (OpponentData opponent in currentGameState.OpponentData.Values)
                        {
                            if (!opponentsInFrontGridSides.ContainsKey(opponent.DriverRawName) && opponent.OverallPosition == opponentPosition)
                            {
                                opponentsInFrontGridSides.Add(opponent.DriverRawName, playerAndOpponentsInFrontGridSides.Item2[opponentPosition]);
                                break;
                            }
                        }
                    }
                }
                if (playerHasCrashed)
                {
                    gridSide = GridSide.UNKNOWN;
                    manualStartOpponentToFollow = null;
                }
                else
                {
                    // get the next opponent in front who started on the same grid side as the player
                    int closestSameSidePosition = 0;
                    String nameOfDriverInFront = null;
                    foreach (String opponentNameInFront in opponentsInFrontGridSides.Keys)
                    {
                        if (opponentsInFrontGridSides[opponentNameInFront] == gridSide &&
                            currentGameState.OpponentData[opponentNameInFront].OverallPosition < manualFormationStartingPosition &&
                            currentGameState.OpponentData[opponentNameInFront].OverallPosition > closestSameSidePosition)
                        {
                            closestSameSidePosition = currentGameState.OpponentData[opponentNameInFront].OverallPosition;
                            nameOfDriverInFront = opponentNameInFront;
                        }
                    }
                    manualStartOpponentToFollow = nameOfDriverInFront;                    
                }
            }
            else
            {
                if (playerHasCrashed)
                {
                    manualStartOpponentToFollow = null;
                }
                else
                {
                    manualStartOpponentToFollow = currentGameState.getOpponentAtOverallPosition(currentGameState.SessionData.OverallPosition - 1).DriverRawName;
                }
            }
        }
        
        private void playManualStartInitialMessage(GameStateData currentGameState)
        {
            setOpponentToFollowAndStartPosition(currentGameState, false, true);
            poleSitter = currentGameState.getOpponentAtOverallPosition(1);

            // use the driver name in front if we have it - if we're starting on pole the manualStartOpponentAhead var will be null,
            // which will force the audio player to use the secondary message
            List<MessageFragment> messageContentsWithName;
            List<MessageFragment> messageContentsNoName;

            OpponentData opponentToLineUpBehind = getOpponent(currentGameState, manualStartOpponentToFollow);
            if (manualFormationGoWhenLeaderCrossesLine)
            {
                // go when leader crosses line, so make sure we don't say "hold position until the start line"
                messageContentsWithName = MessageContents(folderManualStartInitialIntro,
                        Position.folderStub + currentGameState.SessionData.OverallPosition, folderManualStartInitialOutroWithDriverName1,
                        opponentToLineUpBehind);
                messageContentsNoName = MessageContents(folderManualStartInitialIntro,
                        Position.folderStub + currentGameState.SessionData.OverallPosition, folderHoldYourPosition);
                if (manualFormationDoubleFile && gridSide == GridSide.LEFT)
                {
                    messageContentsWithName.Add(MessageFragment.Text(FrozenOrderMonitor.folderInTheLeftColumn));
                }
                else if (manualFormationDoubleFile && gridSide == GridSide.RIGHT)
                {
                    messageContentsWithName.Add(MessageFragment.Text(FrozenOrderMonitor.folderInTheRightColumn));
                }
            }
            else
            {
                if (manualFormationDoubleFile && gridSide == GridSide.LEFT)
                {
                    messageContentsWithName = MessageContents(folderManualStartInitialIntro,
                            Position.folderStub + currentGameState.SessionData.OverallPosition, folderManualStartStartingOnLeftBehind, opponentToLineUpBehind);
                    messageContentsNoName = MessageContents(folderManualStartInitialIntro,
                            Position.folderStub + currentGameState.SessionData.OverallPosition, FrozenOrderMonitor.folderInTheLeftColumn, folderManualStartInitialOutroNoDriverName);
                }
                else if (manualFormationDoubleFile && gridSide == GridSide.RIGHT)
                {
                    messageContentsWithName = MessageContents(folderManualStartInitialIntro,
                            Position.folderStub + currentGameState.SessionData.OverallPosition, folderManualStartStartingOnRightBehind, opponentToLineUpBehind);
                    messageContentsNoName = MessageContents(folderManualStartInitialIntro,
                            Position.folderStub + currentGameState.SessionData.OverallPosition, FrozenOrderMonitor.folderInTheRightColumn, folderManualStartInitialOutroNoDriverName);
                }
                else
                {
                    messageContentsWithName = MessageContents(folderManualStartInitialIntro,
                            Position.folderStub + currentGameState.SessionData.OverallPosition, folderManualStartInitialOutroWithDriverName1,
                            opponentToLineUpBehind, folderManualStartInitialOutroWithDriverName2);
                    messageContentsNoName = MessageContents(folderManualStartInitialIntro,
                            Position.folderStub + currentGameState.SessionData.OverallPosition, folderManualStartInitialOutroNoDriverName);
                }
            }
            if (opponentToLineUpBehind == null)
            {
                audioPlayer.playMessage(new QueuedMessage("manual_start_intro", messageContentsNoName, 0, this), 10);
            }
            else
            {
                audioPlayer.playMessage(new QueuedMessage("manual_start_intro", messageContentsWithName, messageContentsNoName, 0, this), 10);
            }
            playedManualStartInitialMessage = true;
        }

        private void checkForIllegalPassesOnFormationLap(GameStateData currentGameState)
        {
            if (this.manualStartOpponentToFollow == null || playedManualStartPlayedGoGoGo)
            {
                return;
            }
            OpponentData opponentToFollow = getOpponent(currentGameState, this.manualStartOpponentToFollow);
            if (opponentToFollow == null)
            {
                return;
            }
            if (hasOpponentDroppedBack(currentGameState.SessionData.OverallPosition, opponentToFollow))
            {
                setOpponentToFollowAndStartPosition(currentGameState, false, false);
                OpponentData newOpponentToFollow = getOpponent(currentGameState, manualStartOpponentToFollow);
                if (AudioPlayer.canReadName(newOpponentToFollow.DriverRawName))
                {
                    if (manualFormationDoubleFile)
                    {
                        if (gridSide == GridSide.LEFT)
                        {
                            audioPlayer.playMessage(new QueuedMessage("new_car_to_follow", MessageContents(folderManualStartInitialOutroWithDriverName1,
                                    newOpponentToFollow, FrozenOrderMonitor.folderInTheLeftColumn), 0, this), 10);
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage("new_car_to_follow", MessageContents(folderManualStartInitialOutroWithDriverName1,
                                    newOpponentToFollow, FrozenOrderMonitor.folderInTheRightColumn), 0, this), 10);
                        }
                    }
                    else
                    {
                        audioPlayer.playMessage(new QueuedMessage("new_car_to_follow", MessageContents(folderManualStartInitialOutroWithDriverName1,
                                    newOpponentToFollow), 0, this), 10);
                    }
                }
            }
            else if (haveWeDroppedBack(currentGameState))
            {
                if (!playedRejoinAtBackMessage)
                {
                    playedRejoinAtBackMessage = true;
                    audioPlayer.playMessage(new QueuedMessage("rejoin_at_back", MessageContents(folderManualStartRejoinAtBack), 0, this), 10);
                }
                setOpponentToFollowAndStartPosition(currentGameState, true, false);
            }
            else if (GameStateData.onManualFormationLap && nextManualFormationOvertakeWarning < currentGameState.Now &&
                opponentToFollow != null && opponentToFollow.OverallPosition > currentGameState.SessionData.OverallPosition)
            {
                if (manualFormationDoubleFile)
                {
                    // we've overtaken the guy in we're supposed to be following
                    nextManualFormationOvertakeWarning = currentGameState.Now.AddSeconds(30);
                    Dictionary<String, Object> validationData = new Dictionary<string, object>();
                    validationData.Add("PlayerPosition", currentGameState.SessionData.OverallPosition);
                    validationData.Add("OpponentToFollowName", this.manualStartOpponentToFollow);

                    audioPlayer.playMessage(new QueuedMessage("give_position_back",
                        MessageContents(folderGivePositionBack, folderManualStartInitialOutroWithDriverName1, getOpponent(currentGameState, manualStartOpponentToFollow)),
                        MessageContents(folderGivePositionBack), 5, this, validationData), 10);                    
                }
            }
        }

        private Boolean hasOpponentDroppedBack(int playerPosition, OpponentData opponent)
        {
            return opponent != null && opponent.OverallPosition > playerPosition + 3;
        }

        private Boolean haveWeDroppedBack(GameStateData currentGameState)
        {
            return currentGameState.SessionData.OverallPosition > manualFormationStartingPosition + 3;
        }

        private void checkForLeaderHasAccelerated(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (leaderHasGone || previousGameState == null || currentGameState.SessionData.TrackDefinition == null ||
                currentGameState.PositionAndMotionData.DistanceRoundTrack == 0 || poleSitter == null || playedManualStartPlayedGoGoGo)
            {
                return;
            }
            float currentDistanceToStartLine = currentGameState.SessionData.TrackDefinition.trackLength - currentGameState.PositionAndMotionData.DistanceRoundTrack;
            if (currentDistanceToStartLine < distanceBeforeLineToStartLeaderAccelerationCheck)
            {
                float previousDistanceToStartLine = currentGameState.SessionData.TrackDefinition.trackLength - previousGameState.PositionAndMotionData.DistanceRoundTrack;
                if (previousDistanceToStartLine > distanceBeforeLineToStartLeaderAccelerationCheck)
                {
                    leaderSpeedAtAccelerationCheckStart = poleSitter.Speed;
                }
                else if (leaderSpeedAtAccelerationCheckStart > 0)
                {
                    if (poleSitter.Speed - leaderSpeedAtAccelerationCheckStart > 5)
                    {
                        Console.WriteLine("Looks like the leader has 'gone'");
                        audioPlayer.playMessage(new QueuedMessage(folderManualStartLeaderHasGone, 0, this), 10);
                        leaderHasGone = true;
                    }
                }
            }
        }

        private void checkForManualDoubleFileReminder(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (manualFormationDoubleFile && gridSide != GridSide.UNKNOWN && previousGameState != null &&
                previousGameState.SessionData.SectorNumber == 2 && currentGameState.SessionData.SectorNumber == 3)
            {
                // use the driver name in front if we have it - if we're starting on pole the manualStartOpponentAhead var will be null,
                // which will force the audio player to use the secondary message
                List<MessageFragment> messageContentsWithName = null;
                List<MessageFragment> messageContentsNoName = null;
                OpponentData opponentToLineUpBehind = getOpponent(currentGameState, manualStartOpponentToFollow);
                if (manualFormationGoWhenLeaderCrossesLine)
                {
                    if (gridSide == GridSide.LEFT)
                    {
                        messageContentsWithName = MessageContents(folderManualStartFormUpBehind, opponentToLineUpBehind, FrozenOrderMonitor.folderInTheLeftColumn);
                        messageContentsNoName = MessageContents(folderHoldYourPosition, FrozenOrderMonitor.folderInTheLeftColumn);
                    }
                    else
                    {
                        messageContentsWithName = MessageContents(folderManualStartFormUpBehind, opponentToLineUpBehind, FrozenOrderMonitor.folderInTheRightColumn);
                        messageContentsNoName = MessageContents(folderHoldYourPosition, FrozenOrderMonitor.folderInTheRightColumn);
                    }
                }
                else
                {
                    if (gridSide == GridSide.LEFT)
                    {
                        messageContentsWithName = MessageContents(folderManualStartFormUpBehind, opponentToLineUpBehind,
                            FrozenOrderMonitor.folderInTheLeftColumn);
                        messageContentsNoName = MessageContents(folderManualStartInitialOutroNoDriverName, FrozenOrderMonitor.folderInTheLeftColumn);
                    }
                    else
                    {
                        messageContentsWithName = MessageContents(folderManualStartFormUpBehind, opponentToLineUpBehind,
                            FrozenOrderMonitor.folderInTheRightColumn);
                        messageContentsNoName = MessageContents(folderManualStartInitialOutroNoDriverName, FrozenOrderMonitor.folderInTheRightColumn);
                    }
                }
                if (opponentToLineUpBehind == null)
                {
                    audioPlayer.playMessage(new QueuedMessage("manual_start_double_file_reminder", messageContentsNoName, 0, this), 10);
                }
                else
                {
                    audioPlayer.playMessage(new QueuedMessage("manual_start_double_file_reminder", messageContentsWithName, messageContentsNoName, 0, this), 10);
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
                    // here we're only interested in what the pole sitter is up to
                    if (poleSitter != null)
                    {
                        if (!playedManualStartGetReady && poleSitter.CurrentSectorNumber == 3 &&
                            poleSitter.DistanceRoundTrack > currentGameState.SessionData.TrackDefinition.trackLength - 200)
                        {
                            playManualStartGetReady();
                        }
                        else if (poleSitter.CompletedLaps == 1)
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
                    else if (currentGameState.SessionData.OverallPosition > 3)
                    {
                        // don't say "leader has crossed the line" if we're right behind him - this would delay the 'go go go' call
                        if (poleSitter != null && !playedManualStartLeaderHasCrossedLine && poleSitter.CompletedLaps == 1)
                        {
                            playedManualStartLeaderHasCrossedLine = true;
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderLeaderHasCrossedStartLine, 0, this) { metadata = new SoundMetadata(SoundType.IMPORTANT_MESSAGE, 0) });
                        }
                    }
                }
            }
        }
    }
}
