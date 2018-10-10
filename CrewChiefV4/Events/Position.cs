using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class Position : AbstractEvent
    {
        private String positionValidationKey = "CURRENT_POSITION";

        public static String folderLeading = "position/leading";
        public static String folderPole = "position/pole";
        public static String folderStub = "position/p";
        public static String folderLast = "position/last";
        public static String folderAhead = "position/ahead";
        public static String folderBehind = "position/behind";
        public static String folderLapsAhead = "position/laps_ahead";
        public static String folderLapsBehind = "position/laps_behind"; 
        public static String folderOneLapAhead = "position/one_lap_ahead";
        public static String folderOneLapBehind = "position/one_lap_down";
        public static String folderOvertaking = "position/overtaking";
        public static String folderBeingOvertaken = "position/being_overtaken";

        private TimeSpan minTimeToWaitBeforeReportingPass = TimeSpan.FromSeconds(3);
        public static int maxSecondsToWaitBeforeReportingPass = 6;
        private TimeSpan maxTimeToWaitBeforeReportingPass = TimeSpan.FromSeconds(maxSecondsToWaitBeforeReportingPass);

        private String folderConsistentlyLast = "position/consistently_last";
        private String folderGoodStart = "position/good_start";
        private String folderOKStart = "position/ok_start";
        private String folderBadStart = "position/bad_start";
        private String folderTerribleStart = "position/terrible_start";

        // optional intro for driver position message (not used in English)
        public static String folderDriverPositionIntro = "position/driver_position_intro";

        private int currentPosition;

        private int previousPosition;

        private SessionType sessionType;

        private int lapNumberAtLastMessage;

        private int numberOfLapsInLastPlace;

        private Boolean playedRaceStartMessage;

        private Boolean enableRaceStartMessages = UserSettings.GetUserSettings().getBoolean("enable_race_start_messages");

        private Boolean enablePositionMessages = UserSettings.GetUserSettings().getBoolean("enable_position_messages");

        private int frequencyOfOvertakingMessages = UserSettings.GetUserSettings().getInt("frequency_of_overtaking_messages");
        private int frequencyOfBeingOvertakenMessages = UserSettings.GetUserSettings().getInt("frequency_of_being_overtaken_messages");

        private int startMessageTime;

        private Boolean isLast;
        
        private List<float> gapsAhead = new List<float>();
        private List<float> gapsBehind = new List<float>();
        private TimeSpan passCheckInterval = TimeSpan.FromSeconds(1);

        private float minAverageGapForPassMessage;
        private float minAverageGapForBeingPassedMessage;
        private int passCheckSamplesToCheck = 100;
        private int beingPassedCheckSamplesToCheck = 100;
        private float maxSpeedDifferenceForReportablePass = 0;
        private float maxSpeedDifferenceForReportableBeingPassed = 0;
        private float minTimeDeltaForPassToBeCompleted = 0.15f;
        private TimeSpan minTimeBetweenOvertakeMessages;

        private DateTime lastPassCheck;

        private DateTime lastOvertakeMessageTime;

        private DateTime timeWhenWeMadeAPass;
        private DateTime timeWhenWeWerePassed;

        private const int secondsToCheckForDamageOrOfftrackOnPass = 10;
        private const int secondsToCheckForYellowOnPass = 3;
        private float lastOffTrackSessionTime = -1.0f;
        private float lastYellowFlagTime = -1.0f;
        private bool lastOvertakeWasClean = true;

        private string opponentAheadKey = null;
        private string opponentBehindKey = null;

        private string opponentKeyForCarWeJustPassed;

        private string opponentKeyForCarThatJustPassedUs;

        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            // frequency of 5 means you need to be < 2.5 seconds apart for at least 20 seconds
            // 9 means you need to be < 4.5 seconds apart for at least 11 seconds
            minAverageGapForPassMessage = 0.5f * (float)frequencyOfOvertakingMessages;
            minAverageGapForBeingPassedMessage = 0.5f * (float)frequencyOfBeingOvertakenMessages;
            if (frequencyOfOvertakingMessages > 0)
            {
                passCheckSamplesToCheck = (int)(100 / frequencyOfOvertakingMessages);
                maxSpeedDifferenceForReportablePass = frequencyOfOvertakingMessages + 15;
            }
            if (frequencyOfBeingOvertakenMessages > 0)
            {
                beingPassedCheckSamplesToCheck = (int)(100 / frequencyOfBeingOvertakenMessages);
                maxSpeedDifferenceForReportableBeingPassed = frequencyOfBeingOvertakenMessages + 15;
            }

            minTimeBetweenOvertakeMessages = TimeSpan.FromSeconds(20);
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        public override void clearState()
        {
            currentPosition = 0;
            sessionType = SessionType.Unavailable;
            previousPosition = 0;
            lapNumberAtLastMessage = 0;
            numberOfLapsInLastPlace = 0;
            playedRaceStartMessage = false;
            startMessageTime = Utilities.random.Next(30, 50);
            isLast = false;
            lastPassCheck = DateTime.MinValue;
            gapsAhead.Clear();
            gapsBehind.Clear();
            opponentKeyForCarWeJustPassed = null;
            opponentKeyForCarThatJustPassedUs = null;
            timeWhenWeWerePassed = DateTime.MinValue;
            timeWhenWeMadeAPass = DateTime.MinValue;
            lastOvertakeMessageTime = DateTime.MinValue;
            lastOffTrackSessionTime = -1.0f;
            lastYellowFlagTime = -1.0f;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                Boolean isStillInThisPosition = true;
                if (validationData != null)
                {
                    object positionValidationData = null;
                    if (validationData.TryGetValue(positionValidationKey, out positionValidationData) && (int)positionValidationData != currentGameState.SessionData.ClassPosition)
                    {
                        isStillInThisPosition = false;
                    }
                }
                return !currentGameState.PitData.InPitlane && isStillInThisPosition;
            }
            else
            {
                return false;
            }
        }

        private Boolean isPassMessageCandidate(List<float> gapsList, int samplesToCheck, float minAverageGap)
        {
            return gapsList.Count >= samplesToCheck &&
                    gapsList.GetRange(gapsList.Count - samplesToCheck, samplesToCheck).Average() < minAverageGap;
        }

        private void checkForNewOvertakes(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.PenaltiesData.IsOffRacingSurface)
            {
                lastOffTrackSessionTime = currentGameState.SessionData.SessionRunningTime;
            }

            Boolean currentSectorIsYellow = currentGameState.SessionData.SectorNumber > 0 && currentGameState.SessionData.SectorNumber < 4 &&
                currentGameState.FlagData.sectorFlags[currentGameState.SessionData.SectorNumber - 1] == FlagEnum.YELLOW;
            if (currentGameState.FlagData.isLocalYellow || currentSectorIsYellow)
            {
                lastYellowFlagTime = currentGameState.SessionData.SessionRunningTime;
            }
            if (currentGameState.SessionData.SessionPhase == SessionPhase.Green &&
                currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.CompletedLaps > 0)
            {                
                if (currentGameState.Now > lastPassCheck.Add(passCheckInterval))
                {
                    lastPassCheck = currentGameState.Now;
                    if (currentGameState.SessionData.TimeDeltaFront > 0)
                    {
                        gapsAhead.Add(currentGameState.SessionData.TimeDeltaFront);
                    }
                    if (currentGameState.SessionData.TimeDeltaBehind > 0) 
                    {
                        gapsBehind.Add(currentGameState.SessionData.TimeDeltaBehind);
                    }
                    string currentOpponentAheadKey = currentGameState.getOpponentKeyInFront(currentGameState.carClass);
                    string currentOpponentBehindKey = currentGameState.getOpponentKeyBehind(currentGameState.carClass);
                    // seems like belt and braces, but as Raceroom names aren't unique we need to double check a pass actually happened here:
                    if (frequencyOfOvertakingMessages > 0 && currentOpponentAheadKey != opponentAheadKey)
                    {
                        if (currentOpponentBehindKey != null &&
                            currentGameState.SessionData.CurrentLapIsValid && !currentGameState.PitData.InPitlane &&
                            currentOpponentBehindKey == opponentAheadKey && isPassMessageCandidate(gapsAhead, passCheckSamplesToCheck, minAverageGapForPassMessage))
                        {
                            OpponentData carWeJustPassed = currentGameState.OpponentData[currentOpponentBehindKey];
                            if (carWeJustPassed.CompletedLaps == currentGameState.SessionData.CompletedLaps &&
                                CarData.IsCarClassEqual(carWeJustPassed.CarClass, currentGameState.carClass))
                            {
                                timeWhenWeMadeAPass = currentGameState.Now;
                                opponentKeyForCarWeJustPassed = currentOpponentBehindKey;
                                lastOvertakeWasClean = true;
                                if (currentGameState.CarDamageData.LastImpactTime > 0.0f && (currentGameState.SessionData.SessionRunningTime - currentGameState.CarDamageData.LastImpactTime) < secondsToCheckForDamageOrOfftrackOnPass)
                                {
                                    lastOvertakeWasClean = false;
                                    Console.WriteLine("Overtake considered not clean due to vehicle damage.");
                                }
                                else if (lastOffTrackSessionTime > 0.0f && (currentGameState.SessionData.SessionRunningTime - lastOffTrackSessionTime) < secondsToCheckForDamageOrOfftrackOnPass)
                                {
                                    lastOvertakeWasClean = false;
                                    Console.WriteLine("Overtake considered not clean due to vehicle off track.");
                                }
                                else if (lastYellowFlagTime > 0.0f && (currentGameState.SessionData.SessionRunningTime - lastYellowFlagTime) < secondsToCheckForYellowOnPass)
                                {
                                    lastOvertakeWasClean = false;
                                    Console.WriteLine("Overtake considered not clean due to yellow flag.");
                                }
                            }
                        }
                        gapsAhead.Clear();
                    }
                    if (frequencyOfBeingOvertakenMessages > 0 && opponentBehindKey != currentOpponentBehindKey)
                    {
                        if (currentOpponentAheadKey != null &&
                            !currentGameState.PitData.InPitlane && currentOpponentAheadKey == opponentBehindKey && 
                            isPassMessageCandidate(gapsBehind, beingPassedCheckSamplesToCheck, minAverageGapForBeingPassedMessage))
                        {
                            OpponentData carThatJustPassedUs = currentGameState.OpponentData[currentOpponentAheadKey];
                            if (carThatJustPassedUs.CompletedLaps == currentGameState.SessionData.CompletedLaps &&
                                CarData.IsCarClassEqual(carThatJustPassedUs.CarClass, currentGameState.carClass))
                            {
                                timeWhenWeWerePassed = currentGameState.Now;
                                opponentKeyForCarThatJustPassedUs = currentOpponentAheadKey;
                            }                            
                        }
                        gapsBehind.Clear();
                    }
                    opponentAheadKey = currentOpponentAheadKey;
                    opponentBehindKey = currentOpponentBehindKey;
                }
            }
        }

        private void checkCompletedOvertake(GameStateData currentGameState)
        {
            if (opponentKeyForCarWeJustPassed != null)
            {
                OpponentData carWeJustPassed = null;
                if (currentGameState.OpponentData.TryGetValue(opponentKeyForCarWeJustPassed, out carWeJustPassed) &&
                    currentGameState.Now < timeWhenWeMadeAPass.Add(maxTimeToWaitBeforeReportingPass) && lastOvertakeWasClean)
                {
                    Boolean reported = false;
                    if (currentGameState.Now > timeWhenWeMadeAPass.Add(minTimeToWaitBeforeReportingPass))
                    {                                 
                        if (currentGameState.Now > lastOvertakeMessageTime.Add(minTimeBetweenOvertakeMessages) &&
                            carWeJustPassed.ClassPosition > currentGameState.SessionData.ClassPosition && currentGameState.SessionData.TimeDeltaBehind > minTimeDeltaForPassToBeCompleted)
                        {
                            lastOvertakeMessageTime = currentGameState.Now;
                            Console.WriteLine("Reporting overtake on car " + opponentKeyForCarWeJustPassed);
                            opponentKeyForCarWeJustPassed = null;
                            gapsAhead.Clear();
                            // adding a 'good' pearl with 0 probability of playing seems odd, but this forces the app to only
                            // allow an existing queued pearl to be played if it's type is 'good'
                            Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                            validationData.Add(positionValidationKey, currentGameState.SessionData.ClassPosition);
                            QueuedMessage overtakingMessage = new QueuedMessage(folderOvertaking, 0, this, validationData);
                            audioPlayer.playMessage(overtakingMessage, PearlsOfWisdom.PearlType.GOOD, 0, 10);
                            reported = true;
                        }
                    }
                    if (!reported)
                    {
                        // check the pass is still valid
                        if (!currentGameState.SessionData.CurrentLapIsValid || carWeJustPassed.isEnteringPits() || currentGameState.PitData.InPitlane ||
                                currentGameState.PositionAndMotionData.CarSpeed - carWeJustPassed.Speed > maxSpeedDifferenceForReportablePass)
                        {
                            opponentKeyForCarWeJustPassed = null;
                            gapsAhead.Clear();
                        }
                    }
                }
                else
                {
                    opponentKeyForCarWeJustPassed = null;
                    gapsAhead.Clear();
                }
            }
            if (opponentKeyForCarThatJustPassedUs != null)
            {
                OpponentData carThatJustPassedUs = null;
                if (currentGameState.OpponentData.TryGetValue(opponentKeyForCarThatJustPassedUs, out carThatJustPassedUs) && 
                    currentGameState.Now < timeWhenWeWerePassed.Add(maxTimeToWaitBeforeReportingPass))
                {
                    Boolean reported = false;
                    if (currentGameState.Now > timeWhenWeWerePassed.Add(minTimeToWaitBeforeReportingPass))
                    {
                        if (currentGameState.Now > lastOvertakeMessageTime.Add(minTimeBetweenOvertakeMessages) &&
                            carThatJustPassedUs.ClassPosition < currentGameState.SessionData.ClassPosition && currentGameState.SessionData.TimeDeltaFront > minTimeDeltaForPassToBeCompleted)
                        {
                            lastOvertakeMessageTime = currentGameState.Now;
                            Console.WriteLine("Reporting being overtaken by car " + opponentKeyForCarThatJustPassedUs);
                            opponentKeyForCarThatJustPassedUs = null;
                            gapsBehind.Clear();
                            // adding a 'bad' pearl with 0 probability of playing seems odd, but this forces the app to only
                            // allow an existing queued pearl to be played if it's type is 'bad'
                            Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                            validationData.Add(positionValidationKey, currentGameState.SessionData.ClassPosition);
                            QueuedMessage beingOvertakenMessage = new QueuedMessage(folderBeingOvertaken, 0, this, validationData);
                            audioPlayer.playMessage(new QueuedMessage(folderBeingOvertaken, 0, this, validationData), PearlsOfWisdom.PearlType.BAD, 0, 10);
                            reported = true;
                        }
                    }
                    if (!reported)
                    {
                        // check the pass is still valid - no lap validity check here because we're being passed
                        if (carThatJustPassedUs.isEnteringPits() ||
                                carThatJustPassedUs.Speed - currentGameState.PositionAndMotionData.CarSpeed > maxSpeedDifferenceForReportableBeingPassed)
                        {
                            opponentKeyForCarThatJustPassedUs = null;
                            gapsBehind.Clear();
                        }
                    }
                }
                else
                {
                    opponentKeyForCarThatJustPassedUs = null;
                    gapsBehind.Clear();
                }
            }
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (GameStateData.onManualFormationLap)
            {
                return;
            }
            // edge case here - if we're the only can in the session, don't bother with this event
            if (currentGameState.OpponentData.Count == 0)
            {
                currentPosition = 1;
                return;
            }

            if (!GlobalBehaviourSettings.useOvalLogic && opponentKeyForCarThatJustPassedUs == null && opponentKeyForCarWeJustPassed == null)
            {
                checkForNewOvertakes(currentGameState, previousGameState);
            }
            checkCompletedOvertake(currentGameState);
            currentPosition = currentGameState.SessionData.ClassPosition;
            sessionType = currentGameState.SessionData.SessionType;
            isLast = currentGameState.isLast();
            if (previousPosition == 0)
            {
                previousPosition = currentPosition;
            }
            if (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race &&
                    enableRaceStartMessages && !playedRaceStartMessage &&
                    currentGameState.SessionData.CompletedLaps == 0 &&
                    (currentGameState.SessionData.JustGoneGreenTime != DateTime.MaxValue && currentGameState.Now > currentGameState.SessionData.JustGoneGreenTime.AddSeconds(startMessageTime)) &&
                    !currentGameState.FlagData.isLocalYellow)
                {
                    playedRaceStartMessage = true;
                    Console.WriteLine("Race start message... isLast = " + isLast +
                        " session start pos = " + currentGameState.SessionData.SessionStartClassPosition + " current pos = " + currentGameState.SessionData.ClassPosition);
                    bool hasrFactorPenaltyPending = (CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT) && currentGameState.PenaltiesData.NumPenalties > 0;
                    if (currentGameState.SessionData.SessionStartClassPosition > 0 &&
                            !currentGameState.PenaltiesData.HasDriveThrough && !currentGameState.PenaltiesData.HasStopAndGo &&
                            !hasrFactorPenaltyPending)
                    {
                        if (currentGameState.SessionData.ClassPosition > currentGameState.SessionData.SessionStartClassPosition + 4)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderTerribleStart, 0, this), 5);
                        }
                        else if (currentGameState.SessionData.ClassPosition > currentGameState.SessionData.SessionStartClassPosition + 1)
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderBadStart, 0, this), 5);
                        }
                        else if (!isLast && (currentGameState.SessionData.ClassPosition == 1 || currentGameState.SessionData.ClassPosition < currentGameState.SessionData.SessionStartClassPosition - 1))
                        {
                            audioPlayer.playMessage(new QueuedMessage(folderGoodStart, 0, this), 5);
                        }
                        else if (!isLast && Utilities.random.NextDouble() > 0.6)
                        {
                            // only play the OK start message sometimes
                            audioPlayer.playMessage(new QueuedMessage(folderOKStart, 0, this), 5);
                        }
                    }
                }
            }
            if (enablePositionMessages && currentGameState.SessionData.IsNewLap && 
                currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && !currentGameState.PitData.InPitlane)
            {
                if (currentGameState.SessionData.CompletedLaps > 0)
                {
                    playedRaceStartMessage = true;
                }
                if (isLast)
                {
                    numberOfLapsInLastPlace++;
                }
                else
                {
                    numberOfLapsInLastPlace = 0;
                }
                if (previousPosition == 0 && currentGameState.SessionData.ClassPosition > 0)
                {
                    previousPosition = currentGameState.SessionData.ClassPosition;
                }
                else
                {
                    if (currentGameState.SessionData.CompletedLaps > lapNumberAtLastMessage + 3
                            || previousPosition != currentGameState.SessionData.ClassPosition)
                    {
                        PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                        float pearlLikelihood = 0.2f;
                        if (currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.ClassPosition > 0)
                        {
                            if (!isLast && (previousPosition > currentGameState.SessionData.ClassPosition + 5 ||
                                (previousPosition > currentGameState.SessionData.ClassPosition && currentGameState.SessionData.ClassPosition <= 5)))
                            {
                                pearlType = PearlsOfWisdom.PearlType.GOOD;
                                pearlLikelihood = 0.8f;
                            }
                            else if (!isLast && previousPosition < currentGameState.SessionData.ClassPosition &&
                                currentGameState.SessionData.ClassPosition > 5 && !previousGameState.PitData.OnOutLap &&
                                !currentGameState.PitData.OnOutLap && !currentGameState.PitData.InPitlane && 
                                currentGameState.SessionData.LapTimePrevious > currentGameState.SessionData.PlayerLapTimeSessionBest)
                            {
                                // don't play bad-pearl if the lap just completed was an out lap or are in the pit

                                // note that we don't play a pearl for being last - there's a special set of 
                                // insults reserved for this
                                pearlType = PearlsOfWisdom.PearlType.BAD;
                                pearlLikelihood = 0.5f;
                            }
                            else if (!isLast)
                            {
                                pearlType = PearlsOfWisdom.PearlType.NEUTRAL;
                            }
                        }
                        // read the position message. This is may be part of a long message queue so it can be a few seconds before it triggers.
                        // Because of this, we use a delayed message event - when the message reaches the top of the queue it uses the latest 
                        // position, rather than the position when it was inserted into the queue.

                        // For RF2 use a non-zero delay here because the position data isn't always updated in a timely fashion at the start of a new lap.
                        int delaySeconds = CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT ||
                            CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT ||
                            CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT ? 1 : 0;
                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("getPositionMessages", new Object[] { 
                            currentPosition }, this);
                        audioPlayer.playMessage(new QueuedMessage("position", delayedMessageEvent, delaySeconds, null), pearlType, pearlLikelihood, 10);
                        lapNumberAtLastMessage = currentGameState.SessionData.CompletedLaps;
                    }
                }
            }
        }

        public Tuple<List<MessageFragment>, List<MessageFragment>> getPositionMessages(int positionWhenQueued)
        {
            // the position might have changed between queueing this messasge and processing it, so update the
            // previousPosition here. We should probably do the same with the lapNumberAtLastMessage, but this won't
            // change quickly enough for it to be a problem
            previousPosition = currentPosition;
            // if the position has changed since we queued this message, prevent the pearls playing as they may be out of date
            // We also don't berate the player for being crap in message *and* any associated pearl
            if (isLast || positionWhenQueued != this.currentPosition)
            {
                audioPlayer.suspendPearlsOfWisdom();
            }
            if (this.currentPosition == 1 && this.sessionType == SessionType.Race)
            {
                return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(folderLeading), null);
            }
            else if (this.isLast)
            {
                if (this.numberOfLapsInLastPlace > 5 &&
                    CrewChief.currentGameState.SessionData.LapTimePrevious > CrewChief.currentGameState.SessionData.PlayerLapTimeSessionBest &&
                    CrewChief.currentGameState.SessionData.ClassPosition > 3)
                {
                    return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(folderConsistentlyLast), null);
                }
                else
                {
                    return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(folderLast), null);
                }
            }
            else if (SoundCache.availableSounds.Contains(folderDriverPositionIntro))
            {
                return new Tuple<List<MessageFragment>,List<MessageFragment>>(MessageContents(folderDriverPositionIntro, folderStub + this.currentPosition), null);
            }
            else
            {
                return new Tuple<List<MessageFragment>, List<MessageFragment>>(MessageContents(folderStub + this.currentPosition), null);
            }
        }

        public override void respond(String voiceMessage)
        {            
            if (isLast)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(folderLast, 0, null));
            }
            else if (currentPosition == 1)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(folderLeading, 0, null));                    
            }
            else if (currentPosition > 0)
            {
                if (SoundCache.availableSounds.Contains(folderDriverPositionIntro))
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("position", MessageContents(folderDriverPositionIntro, folderStub + currentPosition), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderStub + currentPosition, 0, null));
                }    
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_POSITION))
            {
                // only play 'no data' if we asked for position directly
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));                    
            }
        }
    }
}
