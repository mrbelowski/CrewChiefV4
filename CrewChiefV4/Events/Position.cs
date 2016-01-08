using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;

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

        private int currentPosition;

        private int previousPosition;

        private int lapNumberAtLastMessage;

        private Random rand = new Random();

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

        private Object opponentAheadKey = null;
        private Object opponentBehindKey = null;

        private Object opponentKeyForCarWeJustPassed;

        private Object opponentKeyForCarThatJustPassedUs;
                
        public Position(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            // frequency of 5 means you need to be < 2.5 seconds apart for at least 20 seconds
            // 9 means you need to be < 4.5 seconds apart for at least 11 seconds
            minAverageGapForPassMessage = 0.2f * (float)frequencyOfOvertakingMessages;
            minAverageGapForBeingPassedMessage = 0.4f * (float)frequencyOfBeingOvertakenMessages;
            if (frequencyOfOvertakingMessages > 0)
            {
                passCheckSamplesToCheck = (int)(100 / frequencyOfOvertakingMessages);
                maxSpeedDifferenceForReportablePass = frequencyOfOvertakingMessages + 2;
            }
            if (frequencyOfBeingOvertakenMessages > 0)
            {
                beingPassedCheckSamplesToCheck = (int)(100 / frequencyOfBeingOvertakenMessages);
                maxSpeedDifferenceForReportableBeingPassed = frequencyOfBeingOvertakenMessages + 2;
            }

            minTimeBetweenOvertakeMessages = TimeSpan.FromSeconds(20);
        }

        public override void clearState()
        {
            previousPosition = 0;
            lapNumberAtLastMessage = 0;
            numberOfLapsInLastPlace = 0;
            playedRaceStartMessage = false;
            startMessageTime = rand.Next(30, 50);
            isLast = false;
            lastPassCheck = DateTime.MinValue;
            gapsAhead.Clear();
            gapsBehind.Clear();
            opponentKeyForCarWeJustPassed = null;
            opponentKeyForCarThatJustPassedUs = null;
            timeWhenWeWerePassed = DateTime.MinValue;
            timeWhenWeMadeAPass = DateTime.MinValue;
            lastOvertakeMessageTime = DateTime.MinValue;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            Boolean isStillInThisPosition = true;
            if (validationData != null && validationData.ContainsKey(positionValidationKey) &&
                (int) validationData[positionValidationKey] != currentGameState.SessionData.Position)
            {
                isStillInThisPosition = false;
            }
            return isApplicableForCurrentSessionAndPhase(currentGameState.SessionData.SessionType, currentGameState.SessionData.SessionPhase) &&
                !currentGameState.PitData.InPitlane && isStillInThisPosition;
        }

        private Boolean isPassMessageCandidate(List<float> gapsList, int samplesToCheck, float minAverageGap)
        {
            return gapsList.Count >= samplesToCheck &&
                    gapsList.GetRange(gapsList.Count - samplesToCheck, samplesToCheck).Average() < minAverageGap;
        }

        private void checkForNewOvertakes(GameStateData previousGameState, GameStateData currentGameState)
        {
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
                    Object currentOpponentAheadKey = currentGameState.getOpponentKeyInFront(true);
                    Object currentOpponentBehindKey = currentGameState.getOpponentKeyBehind(true);
                    // seems like belt and braces, but as Raceroom names aren't unique we need to double check a pass actually happened here:
                    if (frequencyOfOvertakingMessages > 0 && currentOpponentAheadKey != opponentAheadKey)
                    {
                        if (currentGameState.SessionData.CurrentLapIsValid && !currentGameState.PitData.InPitlane &&
                            currentOpponentBehindKey == opponentAheadKey && isPassMessageCandidate(gapsAhead, passCheckSamplesToCheck, minAverageGapForPassMessage))
                        {
                            // TODO: check if we need to do a pit check here - don't think so
                            OpponentData carWeJustPassed = currentGameState.OpponentData[currentOpponentBehindKey];
                            if (carWeJustPassed.CompletedLaps == currentGameState.SessionData.CompletedLaps && carWeJustPassed.CarClass == currentGameState.carClass)
                            {
                                timeWhenWeMadeAPass = currentGameState.Now;
                                opponentKeyForCarWeJustPassed = currentOpponentBehindKey;
                            }
                        }
                        gapsAhead.Clear();
                    }
                    if (frequencyOfBeingOvertakenMessages > 0 && opponentBehindKey != currentOpponentBehindKey)
                    {
                        if (!currentGameState.PitData.InPitlane && currentOpponentAheadKey == opponentBehindKey && isPassMessageCandidate(gapsBehind, beingPassedCheckSamplesToCheck, minAverageGapForBeingPassedMessage))
                        {
                            // TODO: check if we need to do a pit check here - don't think so
                            OpponentData carThatJustPassedUs = currentGameState.OpponentData[currentOpponentAheadKey];
                            if (carThatJustPassedUs.CompletedLaps == currentGameState.SessionData.CompletedLaps && carThatJustPassedUs.CarClass == currentGameState.carClass)
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
                if (currentGameState.Now < timeWhenWeMadeAPass.Add(maxTimeToWaitBeforeReportingPass))
                {
                    Boolean reported = false;
                    OpponentData carWeJustPassed = currentGameState.OpponentData[opponentKeyForCarWeJustPassed];               
                    if (currentGameState.Now > timeWhenWeMadeAPass.Add(minTimeToWaitBeforeReportingPass))
                    {                                 
                        if (currentGameState.Now > lastOvertakeMessageTime.Add(minTimeBetweenOvertakeMessages) && 
                            carWeJustPassed.Position > currentGameState.SessionData.Position && currentGameState.SessionData.TimeDeltaBehind > minTimeDeltaForPassToBeCompleted)
                        {
                            lastOvertakeMessageTime = currentGameState.Now;
                            Console.WriteLine("Reporting overtake on car " + opponentKeyForCarWeJustPassed);
                            opponentKeyForCarWeJustPassed = null;
                            gapsAhead.Clear();
                            // adding a 'good' pearl with 0 probability of playing seems odd, but this forces the app to only
                            // allow an existing queued pearl to be played if it's type is 'good'
                            audioPlayer.queueClip(new QueuedMessage(folderOvertaking, 0, this), PearlsOfWisdom.PearlType.GOOD, 0);
                            reported = true;
                        }
                    }
                    if (!reported)
                    {
                        // check the pass is still valid
                        if (!currentGameState.SessionData.CurrentLapIsValid || carWeJustPassed.isEnteringPits() ||
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
                if (currentGameState.Now < timeWhenWeWerePassed.Add(maxTimeToWaitBeforeReportingPass))
                {
                    Boolean reported = false;
                    OpponentData carThatJustPassedUs = currentGameState.OpponentData[opponentKeyForCarThatJustPassedUs];
                    if (currentGameState.Now > timeWhenWeWerePassed.Add(minTimeToWaitBeforeReportingPass))
                    {
                        if (currentGameState.Now > lastOvertakeMessageTime.Add(minTimeBetweenOvertakeMessages) &&
                            carThatJustPassedUs.Position < currentGameState.SessionData.Position && currentGameState.SessionData.TimeDeltaFront > minTimeDeltaForPassToBeCompleted)
                        {
                            lastOvertakeMessageTime = currentGameState.Now;
                            Console.WriteLine("Reporting being overtaken by car " + opponentKeyForCarThatJustPassedUs);
                            opponentKeyForCarThatJustPassedUs = null;
                            gapsBehind.Clear();
                            // adding a 'bad' pearl with 0 probability of playing seems odd, but this forces the app to only
                            // allow an existing queued pearl to be played if it's type is 'bad'
                            audioPlayer.queueClip(new QueuedMessage(folderBeingOvertaken, 0, this), PearlsOfWisdom.PearlType.BAD, 0);
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
            if (opponentKeyForCarThatJustPassedUs == null && opponentKeyForCarWeJustPassed == null)
            {
                checkForNewOvertakes(currentGameState, previousGameState);
            }
            checkCompletedOvertake(currentGameState);
            currentPosition = currentGameState.SessionData.Position;
            isLast = currentGameState.isLast();
            if (previousPosition == 0)
            {
                previousPosition = currentPosition;
            }            
            if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
            {
                if (currentGameState.SessionData.SessionType == SessionType.Race &&
                    enableRaceStartMessages && !playedRaceStartMessage &&
                    currentGameState.SessionData.CompletedLaps == 0 && currentGameState.SessionData.LapTimeCurrent > startMessageTime)
                {
                    playedRaceStartMessage = true;
                    Console.WriteLine("Race start message... isLast = " + isLast +
                        " session start pos = " + currentGameState.SessionData.SessionStartPosition + " current pos = " + currentGameState.SessionData.Position);
                    if (currentGameState.SessionData.SessionStartPosition > 0)
                    {
                        if (currentGameState.SessionData.SessionStartPosition + 1 < currentGameState.SessionData.Position)
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderBadStart, 0, this));
                        }
                        else if (!isLast && (currentGameState.SessionData.Position == 1 || currentGameState.SessionData.SessionStartPosition > currentGameState.SessionData.Position + 2))
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderGoodStart, 0, this));
                        }
                        else if (currentGameState.SessionData.SessionStartPosition + 5 < currentGameState.SessionData.Position)
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderTerribleStart, 0, this));
                        }
                        else if (!isLast && rand.NextDouble() > 0.6)
                        {
                            // only play the OK start message sometimes
                            audioPlayer.queueClip(new QueuedMessage(folderOKStart, 0, this));
                        }
                    }
                }
            }
            if (enablePositionMessages && currentGameState.SessionData.IsNewLap)
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
                if (previousPosition == 0 && currentGameState.SessionData.Position > 0)
                {
                    previousPosition = currentGameState.SessionData.Position;
                }
                else
                {
                    if (currentGameState.SessionData.CompletedLaps > lapNumberAtLastMessage + 3
                            || previousPosition != currentGameState.SessionData.Position)
                    {
                        Dictionary<String, Object> validationData = new Dictionary<String, Object>();
                        validationData.Add(positionValidationKey, currentGameState.SessionData.Position);
                        PearlsOfWisdom.PearlType pearlType = PearlsOfWisdom.PearlType.NONE;
                        float pearlLikelihood = 0.2f;
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            if (!isLast && (previousPosition > currentGameState.SessionData.Position + 5 ||
                                (previousPosition > currentGameState.SessionData.Position && currentGameState.SessionData.Position <= 5)))
                            {
                                pearlType = PearlsOfWisdom.PearlType.GOOD;
                                pearlLikelihood = 0.8f;
                            }
                            else if (!isLast && previousPosition < currentGameState.SessionData.Position && currentGameState.SessionData.Position > 5)
                            {
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
                        if (currentGameState.SessionData.Position == 1)
                        {
                            if (currentGameState.SessionData.SessionType == SessionType.Race)
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderLeading, 0, this, validationData), pearlType, pearlLikelihood);
                            }
                            else if (currentGameState.SessionData.SessionType == SessionType.Practice)
                            {
                                audioPlayer.queueClip(new QueuedMessage(folderStub + 1, 0, this, validationData), pearlType, pearlLikelihood);
                            }
                            // no p1 for pole - this is in the laptime tracker (yuk)
                        }
                        else if (!isLast)
                        {
                            audioPlayer.queueClip(new QueuedMessage(folderStub + currentGameState.SessionData.Position, 0, this), pearlType, pearlLikelihood);
                        }
                        else if (isLast)
                        {
                            if (numberOfLapsInLastPlace > 3)
                            {
                                audioPlayer.suspendPearlsOfWisdom();
                                audioPlayer.queueClip(new QueuedMessage(folderConsistentlyLast, 0, this, validationData));
                            }
                            else
                            {
                                audioPlayer.suspendPearlsOfWisdom();
                                audioPlayer.queueClip(new QueuedMessage(folderLast, 0, this, validationData));
                            }
                        }
                        previousPosition = currentGameState.SessionData.Position;
                        lapNumberAtLastMessage = currentGameState.SessionData.CompletedLaps;
                    }
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.POSITION))
            {
                if (isLast)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderLast, 0, this), false);
                    audioPlayer.closeChannel();
                }
                else if (currentPosition == 1)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderLeading, 0, this), false);
                    audioPlayer.closeChannel();
                }
                else if (currentPosition > 0) {
                    if (currentPosition < 25) {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderStub + currentPosition, 0, this), false);
                        audioPlayer.closeChannel();
                    } else {
                        audioPlayer.playClipImmediately(new QueuedMessage("position", MessageContents(currentPosition), 0, this), false);
                        audioPlayer.closeChannel();
                    }
                }
                else {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                    audioPlayer.closeChannel();
                }
            }
        }
    }
}
