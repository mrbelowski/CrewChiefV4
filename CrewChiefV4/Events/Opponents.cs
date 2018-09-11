using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class Opponents : AbstractEvent
    {
        private static String validationDriverAheadKey = "validationDriverAheadKey";
        private static String validationNewLeaderKey = "validationNewLeaderKey";

        public static String folderLeaderIsPitting = "opponents/the_leader_is_pitting";
        public static String folderCarAheadIsPitting = "opponents/the_car_ahead_is_pitting";
        public static String folderCarBehindIsPitting = "opponents/the_car_behind_is_pitting";

        public static String folderTheLeader = "opponents/the_leader";
        public static String folderIsPitting = "opponents/is_pitting";
        public static String folderAheadIsPitting = "opponents/ahead_is_pitting";
        public static String folderBehindIsPitting = "opponents/behind_is_pitting";

        public static String folderTheLeaderIsNowOn = "opponents/the_leader_is_now_on";
        public static String folderTheCarAheadIsNowOn = "opponents/the_car_ahead_is_now_on";
        public static String folderTheCarBehindIsNowOn = "opponents/the_car_behind_is_now_on";
        public static String folderIsNowOn = "opponents/is_now_on";

        public static String folderLeaderHasJustDoneA = "opponents/the_leader_has_just_done_a";
        public static String folderTheCarAheadHasJustDoneA = "opponents/the_car_ahead_has_just_done_a";
        public static String folderTheCarBehindHasJustDoneA = "opponents/the_car_behind_has_just_done_a";
        public static String folderNewFastestLapFor = "opponents/new_fastest_lap_for";

        public static String folderOneLapBehind = "opponents/one_lap_behind";
        public static String folderOneLapAhead = "opponents/one_lap_ahead";

        public static String folderIsNowLeading = "opponents/is_now_leading";
        public static String folderNextCarIs = "opponents/next_car_is";

        public static String folderCantPronounceName = "opponents/cant_pronounce_name";

        public static String folderWeAre = "opponents/we_are";

        // optional intro for opponent position (not used in English)
        public static String folderOpponentPositionIntro = "position/opponent_position_intro";

        public static String folderHasJustRetired = "opponents/has_just_retired";
        public static String folderHasJustBeenDisqualified = "opponents/has_just_been_disqualified";

        public static String folderLicenseA = "licence/a_licence";
        public static String folderLicenseB = "licence/b_licence";
        public static String folderLicenseC = "licence/c_licence";
        public static String folderLicenseD = "licence/d_licence";
        public static String folderLicenseR = "licence/r_licence";
        public static String folderLicensePro = "licence/pro_licence";
        
        private int frequencyOfOpponentRaceLapTimes = UserSettings.GetUserSettings().getInt("frequency_of_opponent_race_lap_times");
        private int frequencyOfOpponentPracticeAndQualLapTimes = UserSettings.GetUserSettings().getInt("frequency_of_opponent_practice_and_qual_lap_times");

        private float minImprovementBeforeReadingOpponentRaceTime;
        private float maxOffPaceBeforeReadingOpponentRaceTime;

        private GameStateData currentGameState;

        private DateTime nextLeadChangeMessage = DateTime.MinValue;

        private DateTime nextCarAheadChangeMessage = DateTime.MinValue;

        private string positionIsPlayerKey = "";

        // single set here because we never want to announce a DQ and a retirement for the same guy
        private HashSet<String> announcedRetirementsAndDQs = new HashSet<String>();

        // this prevents us from bouncing between 'next car is...' messages:
        private Dictionary<string, DateTime> onlyAnnounceOpponentAfter = new Dictionary<string, DateTime>();
        private TimeSpan waitBeforeAnnouncingSameOpponentAhead = TimeSpan.FromMinutes(3);
        private String lastNextCarAheadOpponentName = null;

        private String lastLeaderAnnounced = null;
        
        public Opponents(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            maxOffPaceBeforeReadingOpponentRaceTime = (float)frequencyOfOpponentRaceLapTimes / 10f;
            minImprovementBeforeReadingOpponentRaceTime = (1f - maxOffPaceBeforeReadingOpponentRaceTime) / 5f;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Practice, SessionType.Qualify, SessionType.Race }; }
        }

        // allow this event to trigger for FCY, but only the retired and DQ'ed checks:
        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Countdown, SessionPhase.FullCourseYellow }; }
        }

        public override void clearState()
        {
            currentGameState = null;
            nextLeadChangeMessage = DateTime.MinValue;
            nextCarAheadChangeMessage = DateTime.MinValue;
            announcedRetirementsAndDQs.Clear();
            onlyAnnounceOpponentAfter.Clear();
            lastNextCarAheadOpponentName = null;
            lastLeaderAnnounced = null;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if (validationData != null)
                {
                    object validationValue = null;
                    if (validationData.TryGetValue(validationDriverAheadKey, out validationValue))
                    {
                        String expectedOpponentName = (String)validationValue;
                        OpponentData opponentInFront = currentGameState.SessionData.ClassPosition > 1 ?
                            currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass) : null;
                        String actualOpponentName = opponentInFront == null ? null : opponentInFront.DriverRawName;
                        if (actualOpponentName != expectedOpponentName)
                        {
                            if (actualOpponentName != null && expectedOpponentName != null)
                            {
                                Console.WriteLine("New car in front message for opponent " + expectedOpponentName +
                                    " no longer valid - driver in front is now " + actualOpponentName);
                            }
                            return false;
                        }
                        else if (opponentInFront != null && (opponentInFront.InPits || opponentInFront.isEnteringPits()))
                        {
                            Console.WriteLine("New car in front message for opponent " + expectedOpponentName +
                                " no longer valid - driver is " + (opponentInFront.InPits ? "in pits" : "is entering the pits"));
                        }
                    }
                    else if (validationData.TryGetValue(validationNewLeaderKey, out validationValue))
                    {
                        String expectedLeaderName = (String)validationValue;
                        if (currentGameState.SessionData.ClassPosition == 1)
                        {
                            Console.WriteLine("New leader message for opponent " + expectedLeaderName +
                                    " no longer valid - player is now leader");
                            return false;
                        }
                        OpponentData actualLeader = currentGameState.getOpponentAtClassPosition(1, currentGameState.carClass);
                        String actualLeaderName = actualLeader == null ? null : actualLeader.DriverRawName;
                        if (actualLeaderName != expectedLeaderName)
                        {
                            if (actualLeaderName != null && expectedLeaderName != null)
                            {
                                Console.WriteLine("New leader message for opponent " + expectedLeaderName +
                                    " no longer valid - leader is now " + actualLeaderName);
                            }
                            return false;
                        }
                    }
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private Object getOpponentIdentifierForTyreChange(OpponentData opponentData, int playerRacePosition)
        {
            // leader
            int positionToCheck;
            if (opponentData.PositionOnApproachToPitEntry > 0)
            {
                positionToCheck = opponentData.PositionOnApproachToPitEntry;
            }
            else
            {
                // fallback if the PositionOnApproachToPitEntry isn't set - shouldn't really happen
                positionToCheck = opponentData.ClassPosition;
            }
            if (positionToCheck == 1)
            {
                return folderTheLeader;
            }
            // 2nd, 3rd, or within 2 positions of the player
            if ((positionToCheck > 1 && positionToCheck <= 3) ||
                (playerRacePosition - 2 <= positionToCheck && playerRacePosition + 2 >= positionToCheck))
            {
                if (opponentData.CanUseName && AudioPlayer.canReadName(opponentData.DriverRawName))
                {
                    return opponentData;
                }
                else
                {
                    return Position.folderStub + positionToCheck;
                }
            }
            return null;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (GameStateData.onManualFormationLap)
            {
                return;
            }
            this.currentGameState = currentGameState;
            // skip the lap time checks and stuff under yellow:
            if (currentGameState.SessionData.SessionPhase != SessionPhase.FullCourseYellow)
            {
                if (nextCarAheadChangeMessage == DateTime.MinValue)
                {
                    nextCarAheadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                }
                if (nextLeadChangeMessage == DateTime.MinValue)
                {
                    nextLeadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                }
                if (currentGameState.SessionData.SessionType != SessionType.Race || frequencyOfOpponentRaceLapTimes > 0)
                {
                    foreach (KeyValuePair<string, OpponentData> entry in currentGameState.OpponentData)
                    {
                        string opponentKey = entry.Key;
                        OpponentData opponentData = entry.Value;
                        if (!CarData.IsCarClassEqual(opponentData.CarClass, currentGameState.carClass))
                        {
                            // not interested in opponents from other classes
                            continue;
                        }

                        // in race sessions, announce tyre type changes once the session is underway
                        if (currentGameState.SessionData.SessionType == SessionType.Race &&
                            currentGameState.SessionData.SessionRunningTime > 30 && opponentData.hasJustChangedToDifferentTyreType)
                        {
                            // this may be a race position or an OpponentData object
                            Object opponentIdentifier = getOpponentIdentifierForTyreChange(opponentData, currentGameState.SessionData.ClassPosition);
                            if (opponentIdentifier != null)
                            {
                                audioPlayer.playMessage(new QueuedMessage("opponent_tyre_change_" + opponentIdentifier.ToString(), MessageContents(opponentIdentifier,
                                    folderIsNowOn, TyreMonitor.getFolderForTyreType(opponentData.CurrentTyres)), 0, this), 5);
                            }
                        }

                        if (opponentData.IsNewLap && opponentData.LastLapTime > 0 && opponentData.OpponentLapData.Count > 1 &&
                            opponentData.LastLapValid && opponentData.CurrentBestLapTime > 0)
                        {
                            float currentFastestLap;
                            if (currentGameState.SessionData.PlayerLapTimeSessionBest == -1)
                            {
                                currentFastestLap = currentGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                            }
                            else if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall == -1)
                            {
                                currentFastestLap = currentGameState.SessionData.PlayerLapTimeSessionBest;
                            }
                            else
                            {
                                currentFastestLap = Math.Min(currentGameState.SessionData.PlayerLapTimeSessionBest, currentGameState.SessionData.OpponentsLapTimeSessionBestOverall);
                            }

                            // this opponent has just completed a lap - do we need to report it? if it's fast overall and more than
                            // a tenth quicker then his previous best we do...
                            if (((currentGameState.SessionData.SessionType == SessionType.Race && opponentData.CompletedLaps > 2) ||
                                (currentGameState.SessionData.SessionType != SessionType.Race && opponentData.CompletedLaps > 1)) && opponentData.LastLapTime <= currentFastestLap &&
                                (opponentData.CanUseName && AudioPlayer.canReadName(opponentData.DriverRawName)))
                            {
                                if ((currentGameState.SessionData.SessionType == SessionType.Race && frequencyOfOpponentRaceLapTimes > 0) ||
                                    (currentGameState.SessionData.SessionType != SessionType.Race && frequencyOfOpponentPracticeAndQualLapTimes > 0))
                                {
                                    audioPlayer.playMessage(new QueuedMessage("new_fastest_lap", MessageContents(folderNewFastestLapFor, opponentData,
                                                TimeSpanWrapper.FromSeconds(opponentData.LastLapTime, Precision.AUTO_LAPTIMES)), 0, this), 3);
                                }
                            }
                            else if ((currentGameState.SessionData.SessionType == SessionType.Race &&
                                    (opponentData.LastLapTime <= opponentData.CurrentBestLapTime &&
                                     opponentData.LastLapTime < opponentData.PreviousBestLapTime - minImprovementBeforeReadingOpponentRaceTime &&
                                     opponentData.LastLapTime < currentFastestLap + maxOffPaceBeforeReadingOpponentRaceTime)) ||
                               ((currentGameState.SessionData.SessionType == SessionType.Practice || currentGameState.SessionData.SessionType == SessionType.Qualify) &&
                                     opponentData.LastLapTime <= opponentData.CurrentBestLapTime))
                            {
                                if (currentGameState.SessionData.ClassPosition > 1 && opponentData.ClassPosition == 1 &&
                                    (currentGameState.SessionData.SessionType == SessionType.Race || frequencyOfOpponentPracticeAndQualLapTimes > 0))
                                {
                                    // he's leading, and has recorded 3 or more laps, and this one's his fastest
                                    Console.WriteLine("Leader fast lap - this lap time = " + opponentData.LastLapTime + " session best = " + currentFastestLap);
                                    audioPlayer.playMessage(new QueuedMessage("leader_good_laptime", MessageContents(folderLeaderHasJustDoneA,
                                            TimeSpanWrapper.FromSeconds(opponentData.LastLapTime, Precision.AUTO_LAPTIMES)), 0, this), 3);
                                }
                                else if (currentGameState.SessionData.ClassPosition > 1 && opponentData.ClassPosition == currentGameState.SessionData.ClassPosition - 1 &&
                                    (currentGameState.SessionData.SessionType == SessionType.Race || Utilities.random.Next(10) < frequencyOfOpponentPracticeAndQualLapTimes))
                                {
                                    // he's ahead of us, and has recorded 3 or more laps, and this one's his fastest
                                    Console.WriteLine("Car ahead fast lap - this lap time = " + opponentData.LastLapTime + " session best = " + currentFastestLap);
                                    audioPlayer.playMessage(new QueuedMessage("car_ahead_good_laptime", MessageContents(folderTheCarAheadHasJustDoneA,
                                           TimeSpanWrapper.FromSeconds(opponentData.LastLapTime, Precision.AUTO_LAPTIMES)), 0, this), 0);
                                }
                                else if (!currentGameState.isLast() && opponentData.ClassPosition == currentGameState.SessionData.ClassPosition + 1 &&
                                    (currentGameState.SessionData.SessionType == SessionType.Race || Utilities.random.Next(10) < frequencyOfOpponentPracticeAndQualLapTimes))
                                {
                                    // he's behind us, and has recorded 3 or more laps, and this one's his fastest
                                    Console.WriteLine("Car behind fast lap - this lap time = " + opponentData.LastLapTime + " session best = " + currentFastestLap);
                                    audioPlayer.playMessage(new QueuedMessage("car_behind_good_laptime", MessageContents(folderTheCarBehindHasJustDoneA,
                                            TimeSpanWrapper.FromSeconds(opponentData.LastLapTime, Precision.AUTO_LAPTIMES)), 0, this), 0);
                                }
                            }
                        }
                    }
                }
            }

            // allow the retired and DQ checks under yellow:
            if (currentGameState.SessionData.SessionType == SessionType.Race &&
                ((currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionTimeRemaining > 0) ||
                 (!currentGameState.SessionData.SessionHasFixedTime && currentGameState.SessionData.SessionLapsRemaining > 0)))
            {
                // don't bother processing retired and DQ'ed drivers and position changes if we're not allowed to use the names:
                if (CrewChief.enableDriverNames)
                {
                    foreach (String retiredDriver in currentGameState.retriedDriverNames)
                    {
                        if (!announcedRetirementsAndDQs.Contains(retiredDriver))
                        {
                            announcedRetirementsAndDQs.Add(retiredDriver);
                            if ((CrewChief.gameDefinition.gameEnum == GameEnum.RF1 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT)
                                && currentGameState.SessionData.SessionPhase != SessionPhase.Green
                                && currentGameState.SessionData.SessionPhase != SessionPhase.FullCourseYellow
                                && currentGameState.SessionData.SessionPhase != SessionPhase.Checkered)
                            {
                                // In an offline session of the ISI games it is possible to select more AI drivers than a track can handle.
                                // The ones that don't fit on a track are marked as DNF before session goes Green.  Don't announce those.
                                continue;
                            }
                            if (AudioPlayer.canReadName(retiredDriver))
                            {
                                audioPlayer.playMessage(new QueuedMessage("retirement", MessageContents(DriverNameHelper.getUsableDriverName(retiredDriver), folderHasJustRetired), 0, this), 0);
                            }
                        }
                    }
                    foreach (String dqDriver in currentGameState.disqualifiedDriverNames)
                    {
                        if (!announcedRetirementsAndDQs.Contains(dqDriver))
                        {
                            announcedRetirementsAndDQs.Add(dqDriver);
                            if (AudioPlayer.canReadName(dqDriver))
                            {
                                audioPlayer.playMessage(new QueuedMessage("retirement", MessageContents(DriverNameHelper.getUsableDriverName(dqDriver), folderHasJustBeenDisqualified), 0, this), 0);
                            }
                        }
                    }
                    // skip the position change checks under yellow:
                    if (currentGameState.SessionData.SessionPhase != SessionPhase.FullCourseYellow)
                    {
                        if (!currentGameState.SessionData.IsRacingSameCarInFront)
                        {
                            if (currentGameState.SessionData.ClassPosition > 2 && currentGameState.Now > nextCarAheadChangeMessage && !currentGameState.PitData.InPitlane
                                && currentGameState.SessionData.CompletedLaps > 0)
                            {
                                OpponentData opponentData = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                                if (opponentData != null)
                                {
                                    String opponentName = opponentData.DriverRawName;
                                    DateTime announceAfterTime = DateTime.MinValue;
                                    if (!opponentData.isEnteringPits() && !opponentData.InPits && (lastNextCarAheadOpponentName == null || !lastNextCarAheadOpponentName.Equals(opponentName)) &&
                                        opponentData.CanUseName && AudioPlayer.canReadName(opponentName) &&
                                        (!onlyAnnounceOpponentAfter.TryGetValue(opponentName, out announceAfterTime) || currentGameState.Now > announceAfterTime))
                                    {
                                        Console.WriteLine("New car ahead: " + opponentName);
                                        audioPlayer.playMessage(new QueuedMessage("new_car_ahead", MessageContents(folderNextCarIs, opponentData),
                                            Utilities.random.Next(Position.maxSecondsToWaitBeforeReportingPass + 1, Position.maxSecondsToWaitBeforeReportingPass + 3), this,
                                            new Dictionary<string, object> { { validationDriverAheadKey, opponentData.DriverRawName } }), 7);
                                        nextCarAheadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(30));
                                        onlyAnnounceOpponentAfter[opponentName] = currentGameState.Now.Add(waitBeforeAnnouncingSameOpponentAhead);
                                        lastNextCarAheadOpponentName = opponentName;
                                    }
                                }
                            }
                        }
                        if (currentGameState.SessionData.HasLeadChanged)
                        {
                            OpponentData leader = currentGameState.getOpponentAtClassPosition(1, currentGameState.carClass);
                            if (leader != null)
                            {
                                String name = leader.DriverRawName;
                                if (currentGameState.SessionData.ClassPosition > 1 && previousGameState.SessionData.ClassPosition > 1 &&
                                    !name.Equals(lastLeaderAnnounced) &&
                                    currentGameState.Now > nextLeadChangeMessage && leader.CanUseName && AudioPlayer.canReadName(name))
                                {
                                    Console.WriteLine("Lead change, current leader is " + name + " laps completed = " + currentGameState.SessionData.CompletedLaps);
                                    audioPlayer.playMessage(new QueuedMessage("new_leader", MessageContents(leader, folderIsNowLeading), 2, this,
                                        new Dictionary<string, object> { { validationNewLeaderKey, name } }), 3);
                                    nextLeadChangeMessage = currentGameState.Now.Add(TimeSpan.FromSeconds(60));
                                    lastLeaderAnnounced = name;
                                }
                            }
                        }
                    }
                }

                HashSet<String> announcedPitters = new HashSet<string>();
                if (currentGameState.PitData.LeaderIsPitting &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation &&
                    !Strategy.opponentsWhoWillExitCloseInFront.Contains(currentGameState.PitData.OpponentForLeaderPitting.DriverRawName))
                {
                    audioPlayer.playMessage(new QueuedMessage("leader_is_pitting", MessageContents(folderTheLeader, currentGameState.PitData.OpponentForLeaderPitting,
                        folderIsPitting), MessageContents(folderLeaderIsPitting), 0, this), 3);
                    announcedPitters.Add(currentGameState.PitData.OpponentForLeaderPitting.DriverRawName);
                }

                if (currentGameState.PitData.CarInFrontIsPitting && currentGameState.SessionData.TimeDeltaFront > 3 &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation &&
                    !Strategy.opponentsWhoWillExitCloseInFront.Contains(currentGameState.PitData.OpponentForCarAheadPitting.DriverRawName))
                {
                    audioPlayer.playMessage(new QueuedMessage("car_in_front_is_pitting", MessageContents(currentGameState.PitData.OpponentForCarAheadPitting,
                        folderAheadIsPitting), MessageContents(folderCarAheadIsPitting), 0, this), 3);
                    announcedPitters.Add(currentGameState.PitData.OpponentForCarAheadPitting.DriverRawName);
                }

                if (currentGameState.PitData.CarBehindIsPitting && currentGameState.SessionData.TimeDeltaBehind > 3 &&
                    currentGameState.SessionData.SessionPhase != SessionPhase.Countdown && currentGameState.SessionData.SessionPhase != SessionPhase.Formation &&
                    !Strategy.opponentsWhoWillExitCloseBehind.Contains(currentGameState.PitData.OpponentForCarBehindPitting.DriverRawName))
                {
                    audioPlayer.playMessage(new QueuedMessage("car_behind_is_pitting", MessageContents(currentGameState.PitData.OpponentForCarBehindPitting,
                        folderBehindIsPitting), MessageContents(folderCarBehindIsPitting), 0, this), 3);
                    announcedPitters.Add(currentGameState.PitData.OpponentForCarBehindPitting.DriverRawName);
                }
                if (Strategy.opponentFrontToWatchForPitting != null && !announcedPitters.Contains(Strategy.opponentFrontToWatchForPitting))
                {
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        if (entry.Value.DriverRawName == Strategy.opponentFrontToWatchForPitting)
                        {
                            if (entry.Value.InPits)
                            {
                                audioPlayer.playMessage(new QueuedMessage("car_is_pitting", MessageContents(entry.Value,
                                    currentGameState.SessionData.ClassPosition > entry.Value.ClassPosition ? folderAheadIsPitting : folderBehindIsPitting), 0, this), 3);
                                Strategy.opponentFrontToWatchForPitting = null;
                                break;
                            }
                        }
                    }
                }
                if (Strategy.opponentBehindToWatchForPitting != null && !announcedPitters.Contains(Strategy.opponentBehindToWatchForPitting))
                {
                    foreach (KeyValuePair<String, OpponentData> entry in currentGameState.OpponentData)
                    {
                        if (entry.Value.DriverRawName == Strategy.opponentBehindToWatchForPitting)
                        {
                            if (entry.Value.InPits)
                            {
                                audioPlayer.playMessage(new QueuedMessage("car_is_pitting", MessageContents(entry.Value,
                                    currentGameState.SessionData.ClassPosition > entry.Value.ClassPosition ? folderAheadIsPitting : folderBehindIsPitting), 0, this), 3);
                                Strategy.opponentBehindToWatchForPitting = null;
                                break;
                            }
                        }
                    }
                }
            }
        }

        private Tuple<string, Boolean> getOpponentKey(String voiceMessage, String expectedNumberSuffix)
        {
            string opponentKey = null;
            Boolean gotByPositionNumber = false;
            if (voiceMessage.Contains(SpeechRecogniser.THE_LEADER))
            {
                if (currentGameState.SessionData.ClassPosition > 1)
                {
                    opponentKey = currentGameState.getOpponentKeyAtClassPosition(1, currentGameState.carClass);
                }
                else if (currentGameState.SessionData.ClassPosition == 1)
                {
                    opponentKey = positionIsPlayerKey;
                }
            }
            if ((voiceMessage.Contains(SpeechRecogniser.THE_CAR_AHEAD) || voiceMessage.Contains(SpeechRecogniser.THE_GUY_AHEAD) ||
                voiceMessage.Contains(SpeechRecogniser.THE_GUY_IN_FRONT) || voiceMessage.Contains(SpeechRecogniser.THE_CAR_IN_FRONT)) && currentGameState.SessionData.ClassPosition > 1)
            {
                opponentKey = currentGameState.getOpponentKeyInFront(currentGameState.carClass);
            }
            else if ((voiceMessage.Contains(SpeechRecogniser.THE_CAR_BEHIND) || voiceMessage.Contains(SpeechRecogniser.THE_GUY_BEHIND)) &&
                            !currentGameState.isLast())
            {
                opponentKey = currentGameState.getOpponentKeyBehind(currentGameState.carClass);
            }
            else if (voiceMessage.Contains(SpeechRecogniser.POSITION_LONG) || voiceMessage.Contains(SpeechRecogniser.POSITION_SHORT))
            {
                int position = 0;
                foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.racePositionNumberToNumber)
                {
                    foreach (String numberStr in entry.Key)
                    {
                        if (expectedNumberSuffix.Length > 0)
                        {
                            if (voiceMessage.Contains(" " + numberStr + expectedNumberSuffix))
                            {
                                position = entry.Value;
                                break;
                            }
                        }
                        else
                        {
                            if (voiceMessage.EndsWith(" " + numberStr))
                            {
                                position = entry.Value;
                                break;
                            }
                        }
                    }
                }
                if (position != currentGameState.SessionData.ClassPosition)
                {
                    opponentKey = currentGameState.getOpponentKeyAtClassPosition(position, currentGameState.carClass);
                }
                else
                {
                    opponentKey = positionIsPlayerKey;
                }
                gotByPositionNumber = true;
            }
            else
            {
                foreach (KeyValuePair<string, OpponentData> entry in currentGameState.OpponentData)
                {
                    String usableDriverName = DriverNameHelper.getUsableDriverName(entry.Value.DriverRawName);
                    if (voiceMessage.Contains(usableDriverName))
                    {
                        opponentKey = entry.Key;
                        break;
                    }
                }
            }
            return new Tuple<string, bool>(opponentKey, gotByPositionNumber);
        }

        private float getOpponentLastLap(string opponentKey)
        {
            OpponentData opponentData = null;
            if (opponentKey != null && currentGameState.OpponentData.TryGetValue(opponentKey, out opponentData))
            {
                return opponentData.LastLapTime;
            }
            return -1;
        }

        private float getOpponentBestLap(string opponentKey)
        {
            OpponentData opponentData = null;
            if (opponentKey != null && currentGameState.OpponentData.TryGetValue(opponentKey, out opponentData))
            {
                return opponentData.CurrentBestLapTime;
            }
            return -1;
        }

        private Tuple<String, float> getOpponentLicensLevel(string opponentKey)
        {
            OpponentData opponentData = null;
            if (opponentKey != null && currentGameState.OpponentData.TryGetValue(opponentKey, out opponentData))
            {
                return opponentData.LicensLevel;
            }
            return new Tuple<String, float>("invalid", -1);
        }
        private int getOpponentIRating(string opponentKey)
        {
            OpponentData opponentData = null;
            if (opponentKey != null && currentGameState.OpponentData.TryGetValue(opponentKey, out opponentData))
            {
                return opponentData.iRating;
            }
            return -1;
        }
        public override void respond(String voiceMessage)
        {
            Boolean gotData = false;
            if (currentGameState != null)
            {
                if (SpeechRecogniser.WHAT_TYRES_AM_I_ON.Contains(voiceMessage))
                {
                    gotData = true;
                    // TODO: mismatched tyre types...
                    audioPlayer.playMessageImmediately(new QueuedMessage(TyreMonitor.getFolderForTyreType(currentGameState.TyreData.FrontLeftTyreType), 0, null));
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHAT_TYRE_IS) || voiceMessage.StartsWith(SpeechRecogniser.WHAT_TYRES_IS))
                {
                    // only have data here for r3e and rf2, other games don't expose opponent tyre types
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)
                    {
                        string opponentKey = getOpponentKey(voiceMessage, " " + SpeechRecogniser.ON).Item1;
                        if (opponentKey != null)
                        {
                            OpponentData opponentData = currentGameState.OpponentData[opponentKey];
                            if (opponentData != null)
                            {
                                gotData = true;
                                audioPlayer.playMessageImmediately(new QueuedMessage(TyreMonitor.getFolderForTyreType(opponentData.CurrentTyres), 0, null));
                            }
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHATS) &&
                    (voiceMessage.EndsWith(SpeechRecogniser.LAST_LAP) || voiceMessage.EndsWith(SpeechRecogniser.BEST_LAP) ||
                    voiceMessage.EndsWith(SpeechRecogniser.LICENSE_CLASS) ||
                    voiceMessage.EndsWith(SpeechRecogniser.IRATING)))
                {
                    if (voiceMessage.EndsWith(SpeechRecogniser.LAST_LAP))
                    {
                        float lastLap = getOpponentLastLap(getOpponentKey(voiceMessage, SpeechRecogniser.POSSESSIVE + " ").Item1);
                        if (lastLap != -1)
                        {
                            gotData = true;
                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentLastLap", MessageContents(
                                TimeSpanWrapper.FromSeconds(lastLap, Precision.AUTO_LAPTIMES)), 0, null));

                        }
                    }
                    else if (voiceMessage.EndsWith(SpeechRecogniser.BEST_LAP))
                    {
                        float bestLap = getOpponentBestLap(getOpponentKey(voiceMessage, SpeechRecogniser.POSSESSIVE + " ").Item1);
                        if (bestLap != -1)
                        {
                            gotData = true;
                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentBestLap", MessageContents(
                                TimeSpanWrapper.FromSeconds(bestLap, Precision.AUTO_LAPTIMES)), 0, null));

                        }
                    }
                    else if (voiceMessage.EndsWith(SpeechRecogniser.LICENSE_CLASS))
                    {
                        Tuple<string, float> licenseLevel = getOpponentLicensLevel(getOpponentKey(voiceMessage, SpeechRecogniser.POSSESSIVE + " ").Item1);
                        if (licenseLevel.Item2 != -1)
                        {
                            gotData = true;
                            Tuple<int, int> wholeandfractional = Utilities.WholeAndFractionalPart(licenseLevel.Item2, 2);
                            List<MessageFragment> messageFragments = new List<MessageFragment>();

                            if (licenseLevel.Item1.ToLower() == "a")
                            {
                                messageFragments.Add(MessageFragment.Text(folderLicenseA));
                            }
                            else if (licenseLevel.Item1.ToLower() == "b")
                            {
                                messageFragments.Add(MessageFragment.Text(folderLicenseB));
                            }
                            else if (licenseLevel.Item1.ToLower() == "c")
                            {
                                messageFragments.Add(MessageFragment.Text(folderLicenseC));
                            }
                            else if (licenseLevel.Item1.ToLower() == "d")
                            {
                                messageFragments.Add(MessageFragment.Text(folderLicenseD));
                            }
                            else if (licenseLevel.Item1.ToLower() == "r")
                            {
                                messageFragments.Add(MessageFragment.Text(folderLicenseR));
                            }
                            else if (licenseLevel.Item1.ToLower() == "wc")
                            {
                                messageFragments.Add(MessageFragment.Text(folderLicensePro));
                            }
                            else
                            {
                                gotData = false;
                            }
                            if (gotData)
                            {
                                messageFragments.AddRange(MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2));
                                QueuedMessage licenceLevelMessage = new QueuedMessage("License/license", messageFragments, 0, null);
                                audioPlayer.playDelayedImmediateMessage(licenceLevelMessage);
                            }
                        }
                    }
                    else
                    {
                        int rating = getOpponentIRating(getOpponentKey(voiceMessage, SpeechRecogniser.POSSESSIVE + " ").Item1);
                        if (rating != -1)
                        {
                            gotData = true;
                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentiRating", MessageContents(rating), 0, null));
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHERE_IS) || voiceMessage.StartsWith(SpeechRecogniser.WHERES))
                {
                    Tuple<string, Boolean> response = getOpponentKey(voiceMessage, "");
                    string opponentKey = response.Item1;
                    Boolean gotByPositionNumber = response.Item2;
                    OpponentData opponent = null;
                    if (opponentKey != null && currentGameState.OpponentData.TryGetValue(opponentKey, out opponent))
                    {
                        if (opponent.IsActive)
                        {
                            int position = opponent.ClassPosition;
                            Tuple<int, float> deltas = currentGameState.SessionData.DeltaTime.GetSignedDeltaTimeWithLapDifference(opponent.DeltaTime);
                            int lapDifference = deltas.Item1;
                            float timeDelta = deltas.Item2;
                            if (currentGameState.SessionData.SessionType != SessionType.Race || timeDelta == 0 || (lapDifference == 0 && Math.Abs(timeDelta) < 0.05))
                            {
                                // the delta is not usable - say the position if we didn't directly ask by position

                                // TODO: we need a "right infront" or "right behind" type response here for when the delta is < 0.05 (< 1 tenth rounded)
                                if (!gotByPositionNumber)
                                {
                                    if (SoundCache.availableSounds.Contains(folderOpponentPositionIntro))
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentPosition", MessageContents(folderOpponentPositionIntro, Position.folderStub + position), 0, null));
                                    }
                                    else
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentPosition", MessageContents(Position.folderStub + position), 0, null));
                                    }
                                    gotData = true;
                                }
                            }
                            else
                            {
                                gotData = true;
                                if (lapDifference == 1)
                                {
                                    if (!gotByPositionNumber)
                                    {
                                        if (SoundCache.availableSounds.Contains(folderOpponentPositionIntro))
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(folderOpponentPositionIntro, Position.folderStub + position, Pause(200), folderOneLapBehind), 0, null));
                                        }
                                        else
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                               MessageContents(Position.folderStub + position, Pause(200), folderOneLapBehind), 0, null));
                                        }
                                    }
                                    else
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta", MessageContents(folderOneLapBehind), 0, null));
                                    }
                                }
                                else if (lapDifference > 1)
                                {
                                    if (!gotByPositionNumber)
                                    {
                                        if (SoundCache.availableSounds.Contains(folderOpponentPositionIntro))
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(folderOpponentPositionIntro, Position.folderStub + position, Pause(200), lapDifference, Position.folderLapsBehind), 0, null));
                                        }
                                        else
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                               MessageContents(Position.folderStub + position, Pause(200), lapDifference, Position.folderLapsBehind), 0, null));
                                        }
                                    }
                                    else
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(lapDifference, Position.folderLapsBehind), 0, null));
                                    }
                                }
                                else if (lapDifference == -1)
                                {
                                    if (!gotByPositionNumber)
                                    {
                                        if (SoundCache.availableSounds.Contains(folderOpponentPositionIntro))
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(folderOpponentPositionIntro, Position.folderStub + position, Pause(200), folderOneLapAhead), 0, null));
                                        }
                                        else
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(Position.folderStub + position, Pause(200), folderOneLapAhead), 0, null));
                                        }
                                    }
                                    else
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta", MessageContents(folderOneLapAhead), 0, null));
                                    }
                                }
                                else if (lapDifference < -1)
                                {
                                    if (!gotByPositionNumber)
                                    {
                                        if (SoundCache.availableSounds.Contains(folderOpponentPositionIntro))
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(folderOpponentPositionIntro, Position.folderStub + position, Pause(200), Math.Abs(lapDifference), Position.folderLapsAhead), 0, null));
                                        }
                                        else
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(Position.folderStub + position, Pause(200), Math.Abs(lapDifference), Position.folderLapsAhead), 0, null));
                                        }
                                    }
                                    else
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(Math.Abs(lapDifference), Position.folderLapsAhead), 0, null));
                                    }
                                }
                                else
                                {
                                    TimeSpanWrapper delta = TimeSpanWrapper.FromSeconds(Math.Abs(timeDelta), Precision.AUTO_GAPS);
                                    String aheadOrBehind = Position.folderAhead;
                                    if (timeDelta >= 0)
                                    {
                                        aheadOrBehind = Position.folderBehind;
                                    }
                                    if (!gotByPositionNumber)
                                    {
                                        if (SoundCache.availableSounds.Contains(folderOpponentPositionIntro))
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                                MessageContents(folderOpponentPositionIntro, Position.folderStub + position, Pause(200), delta, aheadOrBehind), 0, null));
                                        }
                                        else
                                        {
                                            audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                               MessageContents(Position.folderStub + position, Pause(200), delta, aheadOrBehind), 0, null));
                                        }
                                    }
                                    else
                                    {
                                        audioPlayer.playMessageImmediately(new QueuedMessage("opponentTimeDelta",
                                            MessageContents(delta, aheadOrBehind), 0, null));
                                    }
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Driver " + opponent.DriverRawName + " is no longer active in this session");
                        }
                    }
                }

                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHOS_BEHIND_ON_TRACK))
                {
                    string opponentKey = currentGameState.getOpponentKeyBehindOnTrack();
                    if (opponentKey != null)
                    {
                        OpponentData opponent = currentGameState.OpponentData[opponentKey];
                        QueuedMessage queuedMessage;
                        if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
                        {
                            queuedMessage = new QueuedMessage("opponentNameAndPosition", MessageContents(opponent,
                                    Position.folderStub + opponent.ClassPosition), 0, null);
                        }
                        else
                        {
                            queuedMessage = new QueuedMessage("opponentNameAndPosition", MessageContents(opponent,
                                    Position.folderStub + opponent.ClassPosition),
                                    MessageContents(Position.folderStub + opponent.ClassPosition, folderCantPronounceName), 0, null);
                        }
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playMessageImmediately(queuedMessage);
                            gotData = true;
                        }
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHOS_IN_FRONT_ON_TRACK))
                {
                    string opponentKey = currentGameState.getOpponentKeyInFrontOnTrack();
                    if (opponentKey != null)
                    {
                        OpponentData opponent = currentGameState.OpponentData[opponentKey];
                        QueuedMessage queuedMessage;
                        if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
                        {
                            queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent,
                                    Position.folderStub + opponent.ClassPosition), 0, null);
                        }
                        else
                        {
                            queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent,
                                    Position.folderStub + opponent.ClassPosition),
                                    MessageContents(Position.folderStub + opponent.ClassPosition, folderCantPronounceName), 0, null);
                        }

                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playMessageImmediately(queuedMessage);

                            gotData = true;
                        }
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHOS_BEHIND_IN_THE_RACE))
                {
                    if (currentGameState.isLast())
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderLast, 0, null));

                        gotData = true;
                    }
                    else
                    {
                        OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                        if (opponent != null)
                        {
                            QueuedMessage queuedMessage;
                            if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
                            {
                                queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                            }
                            else
                            {
                                queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                            }

                            if (queuedMessage.canBePlayed)
                            {
                                audioPlayer.playMessageImmediately(queuedMessage);

                                gotData = true;
                            }
                        }
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHOS_IN_FRONT_IN_THE_RACE))
                {
                    if (currentGameState.SessionData.ClassPosition == 1)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderLeading, 0, null));

                        gotData = true;
                    }
                    else
                    {
                        OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                        if (opponent != null)
                        {
                            QueuedMessage queuedMessage;
                            if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
                            {
                                queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                            }
                            else
                            {
                                queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                            }

                            if (queuedMessage.canBePlayed)
                            {
                                audioPlayer.playMessageImmediately(queuedMessage);

                                gotData = true;
                            }
                        }
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHOS_LEADING) && currentGameState.SessionData.ClassPosition > 1)
                {
                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(1, currentGameState.carClass);
                    if (opponent != null)
                    {
                        QueuedMessage queuedMessage;
                        if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
                        {
                            queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                        }
                        else
                        {
                            queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                        }
                        if (queuedMessage.canBePlayed)
                        {
                            audioPlayer.playMessageImmediately(queuedMessage);

                            gotData = true;
                        }
                    }
                }
                else if (voiceMessage.StartsWith(SpeechRecogniser.WHOS_IN))
                {
                    string opponentKey = getOpponentKey(voiceMessage, "").Item1;
                    if (opponentKey != null)
                    {
                        OpponentData opponent = null;
                        if (opponentKey == positionIsPlayerKey)
                        {
                            audioPlayer.playMessageImmediately(new QueuedMessage(folderWeAre, 0, null));

                            gotData = true;
                        }
                        else if (currentGameState.OpponentData.TryGetValue(opponentKey, out opponent))
                        {
                            QueuedMessage queuedMessage;
                            if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
                            {
                                queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), 0, null);
                            }
                            else
                            {
                                queuedMessage = new QueuedMessage("opponentName", MessageContents(opponent), MessageContents(folderCantPronounceName), 0, null);
                            }
                            if (queuedMessage.canBePlayed)
                            {
                                audioPlayer.playMessageImmediately(queuedMessage);

                                gotData = true;
                            }
                        }
                    }
                }
            }
            if (!gotData)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));

            }
        }

        private float getOpponentBestLap(List<float> opponentLapTimes, int lapsToCheck)
        {
            if (opponentLapTimes == null && opponentLapTimes.Count == 0)
            {
                return -1;
            }
            float bestLap = opponentLapTimes[opponentLapTimes.Count - 1];
            int minIndex = opponentLapTimes.Count - lapsToCheck;
            for (int i = opponentLapTimes.Count - 1; i >= minIndex; i--)
            {
                if (opponentLapTimes[i] > 0 && opponentLapTimes[i] < bestLap)
                {
                    bestLap = opponentLapTimes[i];
                }
            }
            return bestLap;
        }
    }
}
