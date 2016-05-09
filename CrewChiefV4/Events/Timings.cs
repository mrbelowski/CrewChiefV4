using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class Timings : AbstractEvent
    {
        public static String folderGapInFrontIncreasing = "timings/gap_in_front_increasing";
        public static String folderGapInFrontDecreasing = "timings/gap_in_front_decreasing";
        public static String folderGapInFrontIsNow = "timings/gap_in_front_is_now";

        public static String folderGapBehindIncreasing = "timings/gap_behind_increasing";
        public static String folderGapBehindDecreasing = "timings/gap_behind_decreasing";
        public static String folderGapBehindIsNow = "timings/gap_behind_is_now";

        // for when we have a driver name...
        public static String folderTheGapTo = "timings/the_gap_to";   // "the gap to..."
        public static String folderAheadIsIncreasing = "timings/ahead_is_increasing"; // [bob] "ahead is increasing, it's now..."
        public static String folderBehindIsIncreasing = "timings/behind_is_increasing"; // [bob] "behind is increasing, it's now..."
        
        public static String folderAheadIsNow = "timings/ahead_is_now"; // [bob] "ahead is increasing, it's now..."
        public static String folderBehindIsNow = "timings/behind_is_now"; // [bob] "behind is increasing, it's now..."

        public static String folderYoureReeling = "timings/youre_reeling";    // "you're reeling..."
        public static String folderInTheGapIsNow = "timings/in_the_gap_is_now";  // [bob] "in, the gap is now..."

        public static String folderIsReelingYouIn = "timings/is_reeling_you_in";    // [bob] "is reeling you in, the gap is now...."

        private String folderBeingHeldUp = "timings/being_held_up";
        private String folderBeingPressured = "timings/being_pressured";

        private int gapAheadReportFrequency = UserSettings.GetUserSettings().getInt("frequency_of_gap_ahead_reports");
        private int gapBehindReportFrequency = UserSettings.GetUserSettings().getInt("frequency_of_gap_behind_reports");
        private int carCloseAheadReportFrequency = UserSettings.GetUserSettings().getInt("frequency_of_car_close_ahead_reports");
        private int carCloseBehindReportFrequency = UserSettings.GetUserSettings().getInt("frequency_of_car_close_behind_reports");

        // if true, don't give as many gap reports at the start/finish - stops the lap start getting too crowded with messages
        private Boolean preferGapReportsMidLap = true;

        private List<float> gapsInFront;

        private List<float> gapsBehind;

        private float gapBehindAtLastReport;

        private float gapInFrontAtLastReport;

        private int sectorsSinceLastGapAheadReport;

        private int sectorsSinceLastGapBehindReport;

        private int sectorsSinceLastCloseCarAheadReport;

        private int sectorsSinceLastCloseCarBehindReport;

        private int sectorsUntilNextGapAheadReport;

        private int sectorsUntilNextGapBehindReport;

        private int sectorsUntilNextCloseCarAheadReport;

        private int sectorsUntilNextCloseCarBehindReport;

        private Random rand = new Random();

        private float currentGapInFront;

        private float currentGapBehind;

        private Boolean enableGapMessages = UserSettings.GetUserSettings().getBoolean("enable_gap_messages");

        private Boolean isLeading;

        private Boolean isLast;

        private Boolean isRace;
        
        private Boolean playedGapBehindForThisLap;

        private int closeAheadMinSectorWait;
        private int closeAheadMaxSectorWait;
        private int gapAheadMinSectorWait;
        private int gapAheadMaxSectorWait;
        private int closeBehindMinSectorWait;
        private int closeBehindMaxSectorWait;
        private int gapBehindMinSectorWait;
        private int gapBehindMaxSectorWait;

        public Timings(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            closeAheadMinSectorWait = 10 - gapAheadReportFrequency;
            closeAheadMaxSectorWait = closeAheadMinSectorWait + 2;
            gapAheadMinSectorWait = 10 - gapAheadReportFrequency;
            gapAheadMaxSectorWait = gapAheadMinSectorWait + 2;

            closeBehindMinSectorWait = 10 - gapBehindReportFrequency;
            closeBehindMaxSectorWait = closeBehindMinSectorWait + 2;
            gapBehindMinSectorWait = 10 - gapBehindReportFrequency;
            gapBehindMaxSectorWait = gapBehindMinSectorWait + 2;
        }

        public override void clearState()
        {
            gapsInFront = new List<float>();
            gapsBehind = new List<float>();            
            gapBehindAtLastReport = -1;
            gapInFrontAtLastReport = -1;
            sectorsSinceLastGapAheadReport = 0;
            sectorsSinceLastGapBehindReport = 0;
            sectorsSinceLastCloseCarAheadReport = 0;
            sectorsSinceLastCloseCarBehindReport = 0;
            if (gapAheadReportFrequency == 0)
            {
                sectorsUntilNextGapAheadReport = int.MaxValue;
            }
            else
            {
                sectorsUntilNextGapAheadReport = 0;
            }
            if (gapBehindReportFrequency == 0)
            {
                sectorsUntilNextGapBehindReport = int.MaxValue;
            }
            else
            {
                sectorsUntilNextGapBehindReport = 0;
            }
            if (carCloseBehindReportFrequency == 0)
            {
                sectorsUntilNextGapBehindReport = int.MaxValue;
            }
            else
            {
                sectorsUntilNextGapBehindReport = 0;
            }
            if (carCloseAheadReportFrequency == 0)
            {
                sectorsUntilNextCloseCarAheadReport = int.MaxValue;
            }
            else
            {
                sectorsUntilNextCloseCarAheadReport = 0;
            }
            currentGapBehind = -1;
            currentGapInFront = -1;
            isLast = false;
            isLeading = false;
            isRace = false;
            playedGapBehindForThisLap = false;
        }

        // adds 0, 1, or 2 to the sectors to wait. This means there's a 2 in 3 chance that a gap report
        // scheduled for the lap end will be moved to a sector
        private int adjustForMidLapPreference(int currentSector, int sectorsTillNextReport)
        {
            if (preferGapReportsMidLap && (currentSector + sectorsTillNextReport - 1) % 3 == 0)
            {
                // note the 0 here - we allow *some* gap reports to remain at the lap end
                int adjustment = rand.Next(0, 3);
                Console.WriteLine("Adjusting gap report wait from " + sectorsTillNextReport + " to " + (sectorsTillNextReport + adjustment));
                return sectorsTillNextReport + adjustment;
            }
            return sectorsTillNextReport;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<string, object> validationData)
        {
            if (validationData != null && validationData.ContainsKey("position") && (int)validationData["position"] != currentGameState.SessionData.Position)
            {
                return false;
            }
            return true;
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            isLeading = currentGameState.SessionData.Position == 1;
            isLast = currentGameState.isLast();
            isRace = currentGameState.SessionData.SessionType == SessionType.Race;
            currentGapInFront = currentGameState.SessionData.TimeDeltaFront;
            currentGapBehind = currentGameState.SessionData.TimeDeltaBehind;

            if (currentGameState.SessionData.IsNewLap)
            {
                playedGapBehindForThisLap = false;
            }
            
            if (gapsInFront == null || gapsBehind == null)
            {
                clearState();
            }
            if (!currentGameState.SessionData.IsRacingSameCarInFront)
            {
                gapsInFront.Clear();
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                gapsBehind.Clear();
            }
            if (!currentGameState.PitData.InPitlane && enableGapMessages)
            {
                if (isRace && !CrewChief.readOpponentDeltasForEveryLap &&
                    IsNewSectorOrGapPoint(previousGameState, currentGameState))
                {
                    sectorsSinceLastGapAheadReport++;
                    sectorsSinceLastGapBehindReport++;
                    sectorsSinceLastCloseCarAheadReport++;
                    sectorsSinceLastCloseCarBehindReport++;
                    GapStatus gapInFrontStatus = GapStatus.NONE;
                    GapStatus gapBehindStatus = GapStatus.NONE;
                    if (currentGameState.SessionData.Position != 1)
                    {
                        gapsInFront.Insert(0, currentGameState.SessionData.TimeDeltaFront);
                        gapInFrontStatus = getGapStatus(gapsInFront, gapInFrontAtLastReport);
                    }
                    if (!isLast)
                    {
                        gapsBehind.Insert(0, currentGameState.SessionData.TimeDeltaBehind);
                        gapBehindStatus = getGapStatus(gapsBehind, gapBehindAtLastReport);
                    }

                    // Play which ever is the smaller gap, but we're not interested if the gap is < 0.5 or > 20 seconds or hasn't changed:
                    Boolean playGapInFront = gapInFrontStatus != GapStatus.NONE &&
                        (gapBehindStatus == GapStatus.NONE || (gapsInFront.Count() > 0 && gapsBehind.Count() > 0 && gapsInFront[0] < gapsBehind[0]));

                    Boolean playGapBehind = !playGapInFront && gapBehindStatus != GapStatus.NONE;
                    if (playGapInFront)
                    {
                        if (gapInFrontStatus == GapStatus.CLOSE)
                        {
                            if (sectorsSinceLastCloseCarAheadReport >= sectorsUntilNextCloseCarAheadReport)
                            {
                                sectorsSinceLastCloseCarAheadReport = 0;
                                // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                                if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                                {
                                    rand.Next(closeAheadMinSectorWait, closeAheadMaxSectorWait);
                                } 
                                else 
                                {
                                    sectorsUntilNextCloseCarAheadReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                        rand.Next(closeAheadMinSectorWait, closeAheadMaxSectorWait));
                                }
                                audioPlayer.playMessage(new QueuedMessage(folderBeingHeldUp, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                gapInFrontAtLastReport = gapsInFront[0];
                            }
                        }
                        else if (gapInFrontStatus != GapStatus.NONE && sectorsSinceLastGapAheadReport >= sectorsUntilNextGapAheadReport)
                        {
                            sectorsSinceLastGapAheadReport = 0;
                            // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                            if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                            {
                                sectorsUntilNextGapAheadReport = rand.Next(gapAheadMinSectorWait, gapAheadMaxSectorWait);
                            }
                            else
                            {
                                sectorsUntilNextGapAheadReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                    rand.Next(gapAheadMinSectorWait, gapAheadMaxSectorWait));
                            }
                            TimeSpan gapInFront = TimeSpan.FromMilliseconds(gapsInFront[0] * 1000);
                            Boolean readGap = gapInFront.Seconds > 0 || gapInFront.Milliseconds > 50;
                            if (readGap)
                            {
                                if (gapInFrontStatus == GapStatus.INCREASING)
                                {
                                    audioPlayer.playMessage(new QueuedMessage("Timings/gap_in_front",
                                        MessageContents(folderTheGapTo, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false), folderAheadIsIncreasing,
                                        gapInFront), MessageContents(folderGapInFrontIncreasing, gapInFront), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                }
                                else if (gapInFrontStatus == GapStatus.DECREASING)
                                {
                                    audioPlayer.playMessage(new QueuedMessage("Timings/gap_in_front",
                                        MessageContents(folderYoureReeling, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false),
                                        folderInTheGapIsNow, gapInFront), MessageContents(folderGapInFrontDecreasing, gapInFront), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                }
                                else if (gapInFrontStatus == GapStatus.OTHER)
                                {
                                    audioPlayer.playMessage(new QueuedMessage("Timings/gap_in_front",
                                        MessageContents(folderTheGapTo, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false),
                                        folderAheadIsNow, gapInFront), MessageContents(folderGapInFrontIsNow, gapInFront), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                }
                            }
                            gapInFrontAtLastReport = gapsInFront[0];
                        }
                    }
                    else if (playGapBehind)
                    {
                        if (gapBehindStatus == GapStatus.CLOSE)
                        {
                            if (sectorsSinceLastCloseCarBehindReport >= sectorsUntilNextCloseCarBehindReport)
                            {
                                sectorsSinceLastCloseCarBehindReport = 0;
                                // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                                if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                                {
                                    sectorsUntilNextCloseCarBehindReport = rand.Next(closeBehindMinSectorWait, closeBehindMaxSectorWait);
                                } 
                                else 
                                {
                                    sectorsUntilNextCloseCarBehindReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                        rand.Next(closeBehindMinSectorWait, closeBehindMaxSectorWait));
                                }
                                audioPlayer.playMessage(new QueuedMessage(folderBeingPressured, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                gapBehindAtLastReport = gapsBehind[0];
                            }
                        }
                        else if (gapBehindStatus != GapStatus.NONE && sectorsSinceLastGapBehindReport >= sectorsUntilNextGapBehindReport)
                        {
                            sectorsSinceLastGapBehindReport = 0;
                            // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                            if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                            {
                                sectorsUntilNextGapBehindReport = rand.Next(gapBehindMinSectorWait, gapBehindMaxSectorWait);
                            }
                            else
                            {
                                sectorsUntilNextGapBehindReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                    rand.Next(gapBehindMinSectorWait, gapBehindMaxSectorWait));
                            }
                            TimeSpan gapBehind = TimeSpan.FromMilliseconds(gapsBehind[0] * 1000);
                            Boolean readGap = gapBehind.Seconds > 0 || gapBehind.Milliseconds > 50;
                            if (readGap)
                            {
                                if (gapBehindStatus == GapStatus.INCREASING)
                                {
                                    audioPlayer.playMessage(new QueuedMessage("Timings/gap_behind",
                                        MessageContents(folderTheGapTo, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1, false),
                                        folderBehindIsIncreasing, gapBehind), MessageContents(folderGapBehindIncreasing, gapBehind), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                }
                                else if (gapBehindStatus == GapStatus.DECREASING)
                                {
                                    audioPlayer.playMessage(new QueuedMessage("Timings/gap_behind",
                                        MessageContents(currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1, false), folderIsReelingYouIn, gapBehind),
                                        MessageContents(folderGapBehindDecreasing, gapBehind), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                }
                                else if (gapBehindStatus == GapStatus.OTHER)
                                {
                                    audioPlayer.playMessage(new QueuedMessage("Timings/gap_behind",
                                        MessageContents(folderTheGapTo, currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1, false),
                                        folderBehindIsNow, gapBehind), MessageContents(folderGapBehindIsNow, gapBehind), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } }));
                                }
                            }
                            gapBehindAtLastReport = gapsBehind[0];
                        }
                    }
                }
                if (isRace && CrewChief.readOpponentDeltasForEveryLap && currentGameState.SessionData.CompletedLaps > 0)
                {
                    if (currentGameState.SessionData.Position > 1 && currentGameState.SessionData.IsNewLap) 
                    {                            
                        if (currentGapInFront > 0.05)
                        {
                            TimeSpan gap = TimeSpan.FromSeconds(currentGapInFront);
                            QueuedMessage message = new QueuedMessage("Timings/gap_ahead", MessageContents(folderTheGapTo,
                                currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position - 1, false), folderAheadIsNow, gap),
                                MessageContents(folderGapInFrontIsNow, gap), 0, this, new Dictionary<string,object>{ {"position", currentGameState.SessionData.Position} });
                            message.playEvenWhenSilenced = true;
                            audioPlayer.playMessage(message);
                        }
                    }
                    if (!currentGameState.isLast())
                    {
                        if (!playedGapBehindForThisLap && currentGapBehind > 0.05 && currentGameState.SessionData.LapTimeCurrent > 0 &&
                            currentGameState.SessionData.LapTimeCurrent >= currentGapBehind && 
                            currentGameState.SessionData.LapTimeCurrent <= currentGapBehind + CrewChief._timeInterval.TotalSeconds)
                        {
                            playedGapBehindForThisLap = true;
                            TimeSpan gap = TimeSpan.FromSeconds(currentGapBehind);
                            QueuedMessage message = new QueuedMessage("Timings/gap_behind", MessageContents(folderTheGapTo,
                                currentGameState.getOpponentAtPosition(currentGameState.SessionData.Position + 1, false), folderBehindIsNow, gap),
                                MessageContents(folderGapBehindIsNow, gap), 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.Position } });
                            message.playEvenWhenSilenced = true;
                            audioPlayer.playMessage(message);
                        }
                    }
                }
            }
        }

        private GapStatus getGapStatus(List<float> gaps, float lastReportedGap)
        {
            // if we have less than 3 gaps in the list, or the last gap is too big, or the change in the gap is too big,
            // we don't want to report anything

            // when comparing gaps round to 1 decimal place
            if (gaps.Count < 3 || gaps[0] <= 0 || gaps[1] <= 0 || gaps[2] <= 0 || gaps[0] > 20 || Math.Abs(gaps[0] - gaps[1]) > 5)
            {
                return GapStatus.NONE;
            }
            else if (gaps[0] < 0.5 && gaps[1] < 0.5)
            {
                // this car has been close for 2 sectors
                return GapStatus.CLOSE;
            }
            else if ((lastReportedGap == -1 || Math.Round(gaps[0], 1) > Math.Round(lastReportedGap)) &&
                Math.Round(gaps[0], 1) > Math.Round(gaps[1], 1) && Math.Round(gaps[1], 1) > Math.Round(gaps[2], 1))
            {
                return GapStatus.INCREASING;
            }
            else if ((lastReportedGap == -1 || Math.Round(gaps[0], 1) < Math.Round(lastReportedGap)) &&
                Math.Round(gaps[0], 1) < Math.Round(gaps[1], 1) && Math.Round(gaps[1], 1) < Math.Round(gaps[2], 1))
            {
                return GapStatus.DECREASING;
            }
            else if (Math.Abs(gaps[0] - gaps[1]) < 1 && Math.Abs(gaps[0] - gaps[1]) < 1 && Math.Abs(gaps[0] - gaps[2]) < 1)
            {
                // If the gap hasn't changed by more than a second we can report it with no 'increasing' or 'decreasing' prefix
                return GapStatus.OTHER;
            }            
            else
            {
                return GapStatus.NONE;
            }
        }

        public override void respond(String voiceMessage)
        {
            Boolean haveData = false;
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_GAP_IN_FRONT) && currentGapInFront != -1)
            {
                if (isLeading && isRace)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderLeading, 0, this));
                    
                    haveData = true;
                }
                else 
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("Timings/gap_in_front",
                        MessageContents(TimeSpan.FromMilliseconds(currentGapInFront * 1000)), 0, this));
                    
                    haveData = true;
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_GAP_BEHIND) &&
                currentGapBehind != -1)
            {
                if (isLast && isRace)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderLast, 0, this));
                    
                    haveData = true;
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("Timings/gap_behind",
                        MessageContents(TimeSpan.FromMilliseconds(currentGapBehind * 1000)), 0, this));
                    
                    haveData = true;
                }
            }
            if (!haveData)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, this));
                
            }
        }

        private enum GapStatus
        {
            CLOSE, INCREASING, DECREASING, OTHER, NONE
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

        private Boolean IsNewSectorOrGapPoint(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0) {
                // the current track definition has 'gapPoints', so use them
                if (currentGameState.PositionAndMotionData.DistanceRoundTrack > 0 &&
                    currentGameState.PositionAndMotionData.DistanceRoundTrack > previousGameState.PositionAndMotionData.DistanceRoundTrack)
                {                    
                    foreach (float gapPoint in currentGameState.SessionData.TrackDefinition.gapPoints)
                    {
                        if (currentGameState.PositionAndMotionData.DistanceRoundTrack >= gapPoint &&
                            previousGameState.PositionAndMotionData.DistanceRoundTrack < gapPoint)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
            else
            {
                return currentGameState.SessionData.IsNewSector;
            }
        }
    }
}
