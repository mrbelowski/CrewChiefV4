using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;
using CrewChiefV4.NumberProcessing;

namespace CrewChiefV4.Events
{
    class Timings : AbstractEvent
    {
        public static String folderGapInFrontIncreasing = "timings/gap_in_front_increasing";
        public static String folderGapInFrontDecreasing = "timings/gap_in_front_decreasing";
        public static String folderGapInFrontIsNow = "timings/gap_in_front_is_now";

        public static String folderHeIsSlowerThroughCorner = "timings/he_is_slower_through_corner";
        public static String folderHeIsFasterThroughCorner = "timings/he_is_faster_through_corner";
        public static String folderHeIsSlowerEnteringCorner = "timings/he_is_slower_entering_corner";
        public static String folderHeIsFasterEnteringCorner = "timings/he_is_faster_entering_corner";

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

        private class Gap
        {
            public float timeDelta = -1.0f;
            public int lapDelta = -1;
        }

        private List<Gap> gapsInFront;

        private List<Gap> gapsBehind;

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

        private Random random = new Random();

        private float currentGapInFront;
        private float currentGapBehind;
        private int currentLapsDeltaBehind;
        private int currentLapsDeltaInFront;

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

        // don't play the same warning for the same guy more than once
        private Dictionary<String, DateTime> trackLandmarkAttackDriverNamesUsed = new Dictionary<String, DateTime>();
        private Dictionary<String, DateTime> trackLandmarkDefendDriverNamesUsed = new Dictionary<String, DateTime>();

        // don't repeat a message about where to attack or defend from the same opponent within 3 minutes - 
        // really important that these messages don't 'spam'
        private TimeSpan minTimeBetweenAttackOrDefendByDriver = TimeSpan.FromMinutes(3);

        public Timings(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            closeAheadMinSectorWait = 13 - gapAheadReportFrequency;
            closeAheadMaxSectorWait = closeAheadMinSectorWait + 5;
            gapAheadMinSectorWait = 13 - gapAheadReportFrequency;
            gapAheadMaxSectorWait = gapAheadMinSectorWait + 5;

            closeBehindMinSectorWait = 13 - gapBehindReportFrequency;
            closeBehindMaxSectorWait = closeBehindMinSectorWait + 5;
            gapBehindMinSectorWait = 13 - gapBehindReportFrequency;
            gapBehindMaxSectorWait = gapBehindMinSectorWait + 5;
        }

        public override void clearState()
        {
            gapsInFront = new List<Gap>();
            gapsBehind = new List<Gap>();
            gapBehindAtLastReport = -1;
            gapInFrontAtLastReport = -1;
            sectorsSinceLastGapAheadReport = 0;
            sectorsSinceLastGapBehindReport = 0;
            sectorsSinceLastCloseCarAheadReport = 0;
            sectorsSinceLastCloseCarBehindReport = 0;
            trackLandmarkAttackDriverNamesUsed.Clear();
            trackLandmarkDefendDriverNamesUsed.Clear();
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
            currentLapsDeltaBehind = -1;
            currentLapsDeltaInFront = -1;
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
                int adjustment = Utilities.random.Next(0, 3);
                Console.WriteLine("Adjusting gap report wait from " + sectorsTillNextReport + " to " + (sectorsTillNextReport + adjustment));
                return sectorsTillNextReport + adjustment;
            }
            return sectorsTillNextReport;
        }

        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<string, object> validationData)
        {
            if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                object timingValidationDataValue = null;
                if (validationData != null && validationData.TryGetValue("position", out timingValidationDataValue) && (int)timingValidationDataValue != currentGameState.SessionData.ClassPosition)
                {
                    return false;
                }
                return true;
            }
            else
            {
                return false;
            }
        }

        private Boolean isNearRaceEnd(GameStateData currentGameState)
        {
            if (!currentGameState.SessionData.SessionHasFixedTime)
            {
                if (currentGameState.SessionData.SessionLapsRemaining == 0)
                {
                    // on last lap - check track length 
                    if (currentGameState.SessionData.TrackDefinition.trackLengthClass == TrackData.TrackLengthClass.LONG)
                    {
                        return currentGameState.SessionData.SectorNumber > 1;
                    }
                    if (currentGameState.SessionData.TrackDefinition.trackLengthClass == TrackData.TrackLengthClass.VERY_LONG)
                    {
                        return currentGameState.SessionData.SectorNumber == 3;
                    }
                    return true;
                }
            }
            else if (currentGameState.SessionData.SessionTimeRemaining < 120)
            {
                return true;
            }
            return false;
        }

        protected override void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            // do the corner names stuff first, if it's enabled
            if (currentGameState.readLandmarksForThisLap && currentGameState.SessionData.trackLandmarksTiming != null &&
                currentGameState.SessionData.trackLandmarksTiming.atMidPointOfLandmark != null)
            {
                // mid-point it true for only 1 tick so this should be safe
                audioPlayer.playMessage(new QueuedMessage("corners/" + currentGameState.SessionData.trackLandmarksTiming.atMidPointOfLandmark, 0, this), 10);
            }
            if (GameStateData.onManualFormationLap)
            {
                return;
            }
            isLeading = currentGameState.SessionData.ClassPosition == 1;
            isLast = currentGameState.isLast();
            isRace = currentGameState.SessionData.SessionType == SessionType.Race;
            currentGapInFront = currentGameState.SessionData.TimeDeltaFront;
            currentGapBehind = currentGameState.SessionData.TimeDeltaBehind;
            currentLapsDeltaBehind = currentGameState.SessionData.LapsDeltaBehind;
            currentLapsDeltaInFront = currentGameState.SessionData.LapsDeltaFront;

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
                gapInFrontAtLastReport = -1;
            }
            if (!currentGameState.SessionData.IsRacingSameCarBehind)
            {
                gapsBehind.Clear();
                gapBehindAtLastReport = -1;
            }
            if (previousGameState != null && previousGameState.SessionData.CompletedLaps <= currentGameState.FlagData.lapCountWhenLastWentGreen)
            {
                return;
            }
            if (!currentGameState.PitData.InPitlane && enableGapMessages && !currentGameState.FlagData.currentLapIsFCY && !isNearRaceEnd(currentGameState))
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
                    if (currentGameState.SessionData.ClassPosition != 1)
                    {
                        // AMS / RF1 hack - sometimes the gap data is stale, so don't put the exact same gap in the list
                        if (gapsInFront.Count == 0 || gapsInFront[0].timeDelta != currentGameState.SessionData.TimeDeltaFront)
                        {
                            gapsInFront.Insert(0, new Gap()
                            {
                                timeDelta = currentGameState.SessionData.TimeDeltaFront,
                                lapDelta = currentGameState.SessionData.LapsDeltaFront
                            });
                            gapInFrontStatus = getGapStatus(gapsInFront, gapInFrontAtLastReport);
                        }
                    }
                    if (!isLast)
                    {
                        // AMS / RF1 hack - sometimes the gap data is stale, so don't put the exact same gap in the list
                        if (gapsBehind.Count == 0 || gapsBehind[0].timeDelta != currentGameState.SessionData.TimeDeltaBehind)
                        {
                            gapsBehind.Insert(0, new Gap()
                            {
                                timeDelta = currentGameState.SessionData.TimeDeltaBehind,
                                lapDelta = currentGameState.SessionData.LapsDeltaBehind
                            });
                            gapBehindStatus = getGapStatus(gapsBehind, gapBehindAtLastReport);
                        }
                    }

                    // Play which ever is the smaller gap, but we're not interested if the gap is < 0.5 or > 20 seconds or hasn't changed:
                    Boolean playGapInFront = gapInFrontStatus != GapStatus.NONE &&
                        (gapBehindStatus == GapStatus.NONE || (gapsInFront.Count() > 0 && gapsBehind.Count() > 0 && gapsInFront[0].timeDelta < gapsBehind[0].timeDelta && gapsInFront[0].lapDelta == 0));

                    Boolean playGapBehind = !playGapInFront && gapBehindStatus != GapStatus.NONE;
                    if (playGapInFront)
                    {
                        if (gapInFrontStatus == GapStatus.CLOSE)
                        {
                            if (!GlobalBehaviourSettings.useOvalLogic && sectorsSinceLastCloseCarAheadReport >= sectorsUntilNextCloseCarAheadReport && !currentGameState.FlagData.isLocalYellow)
                            {
                                sectorsSinceLastCloseCarAheadReport = 0;
                                // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                                if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                                {
                                    sectorsUntilNextCloseCarAheadReport = Utilities.random.Next(closeAheadMinSectorWait, closeAheadMaxSectorWait);
                                }
                                else
                                {
                                    sectorsUntilNextCloseCarAheadReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                        Utilities.random.Next(closeAheadMinSectorWait, closeAheadMaxSectorWait));
                                }
                                audioPlayer.playMessage(new QueuedMessage(folderBeingHeldUp, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 10);
                                OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                                if (opponent != null)
                                {
                                    DateTime lastTimeDriverNameUsed = DateTime.MinValue;
                                    if (!trackLandmarkAttackDriverNamesUsed.TryGetValue(opponent.DriverRawName, out lastTimeDriverNameUsed) ||
                                        lastTimeDriverNameUsed + minTimeBetweenAttackOrDefendByDriver < currentGameState.Now)
                                    {
                                        CrewChiefV4.GameState.TrackLandmarksTiming.LandmarkAndDeltaType landmarkAndDeltaType =
                                                    currentGameState.SessionData.trackLandmarksTiming.getLandmarkWhereIAmFaster(opponent.trackLandmarksTiming, true, false);
                                        if (landmarkAndDeltaType.landmarkName != null)
                                        {
                                            // either we're faster on entry or faster through
                                            String attackFolder = landmarkAndDeltaType.deltaType == TrackLandmarksTiming.DeltaType.Time ? folderHeIsSlowerThroughCorner : folderHeIsSlowerEnteringCorner;
                                            audioPlayer.playMessage(new QueuedMessage("Timings/corner_to_attack_in", MessageContents(Pause(200), attackFolder, "corners/" + landmarkAndDeltaType.landmarkName), 0, this), 5);
                                            trackLandmarkAttackDriverNamesUsed[opponent.DriverRawName] = currentGameState.Now;
                                        }
                                    }
                                }
                                gapInFrontAtLastReport = gapsInFront[0].timeDelta;
                            }
                        }
                        else if (gapInFrontStatus != GapStatus.NONE && sectorsSinceLastGapAheadReport >= sectorsUntilNextGapAheadReport)
                        {
                            sectorsSinceLastGapAheadReport = 0;
                            // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                            if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                            {
                                sectorsUntilNextGapAheadReport = Utilities.random.Next(gapAheadMinSectorWait, gapAheadMaxSectorWait);
                            }
                            else
                            {
                                sectorsUntilNextGapAheadReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                    Utilities.random.Next(gapAheadMinSectorWait, gapAheadMaxSectorWait));
                            }
                            TimeSpanWrapper gapInFront = TimeSpanWrapper.FromMilliseconds(gapsInFront[0].timeDelta * 1000, Precision.AUTO_GAPS);
                            Boolean readGap = gapInFront.timeSpan.Seconds > 0 || gapInFront.timeSpan.Milliseconds > 50;
                            if (readGap)
                            {
                                if (gapInFrontStatus == GapStatus.INCREASING)
                                {
                                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                                    if (opponent != null)
                                    {
                                        List<MessageFragment> primaryPartialMessageContents = MessageContents(folderTheGapTo, opponent, folderAheadIsIncreasing);
                                        List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapInFrontIncreasing);
                                        int primaryGapIndex = 3;
                                        int alternateGapIndex = 1;
                                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            true, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                        audioPlayer.playMessage(new QueuedMessage("Timings/gap_in_front", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 5);
                                    }
                                }
                                else if (gapInFrontStatus == GapStatus.DECREASING)
                                {
                                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                                    if (opponent != null)
                                    {
                                        List<MessageFragment> primaryPartialMessageContents = MessageContents(folderYoureReeling, opponent, folderInTheGapIsNow);
                                        List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapInFrontDecreasing);
                                        int primaryGapIndex = 3;
                                        int alternateGapIndex = 1;
                                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            true, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                        audioPlayer.playMessage(new QueuedMessage("Timings/gap_in_front", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 5);

                                        DateTime lastTimeDriverNameUsed = DateTime.MinValue;
                                        if (!trackLandmarkAttackDriverNamesUsed.TryGetValue(opponent.DriverRawName, out lastTimeDriverNameUsed) ||
                                            lastTimeDriverNameUsed + minTimeBetweenAttackOrDefendByDriver < currentGameState.Now)
                                        {
                                            CrewChiefV4.GameState.TrackLandmarksTiming.LandmarkAndDeltaType landmarkAndDeltaType =
                                                currentGameState.SessionData.trackLandmarksTiming.getLandmarkWhereIAmFaster(opponent.trackLandmarksTiming, true, false);
                                            if (landmarkAndDeltaType.landmarkName != null)
                                            {
                                                // either we're faster on entry or faster through
                                                String attackFolder = landmarkAndDeltaType.deltaType == TrackLandmarksTiming.DeltaType.Time ? folderHeIsSlowerThroughCorner : folderHeIsSlowerEnteringCorner;
                                                audioPlayer.playMessage(new QueuedMessage("Timings/corner_to_attack_in", MessageContents(Pause(200), attackFolder, "corners/" + landmarkAndDeltaType.landmarkName), 0, this), 5);
                                                trackLandmarkAttackDriverNamesUsed[opponent.DriverRawName] = currentGameState.Now;
                                            }
                                        }
                                    }
                                }
                                else if (gapInFrontStatus == GapStatus.OTHER)
                                {
                                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                                    if (opponent != null)
                                    {
                                        List<MessageFragment> primaryPartialMessageContents = MessageContents(folderTheGapTo, opponent, folderAheadIsNow);
                                        List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapInFrontIsNow);
                                        int primaryGapIndex = 3;
                                        int alternateGapIndex = 1;
                                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            true, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                        audioPlayer.playMessage(new QueuedMessage("Timings/gap_in_front", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 5);
                                    }
                                }
                            }
                            gapInFrontAtLastReport = gapsInFront[0].timeDelta;
                        }
                    }
                    else if (playGapBehind)
                    {
                        if (gapBehindStatus == GapStatus.CLOSE)
                        {
                            if (!GlobalBehaviourSettings.useOvalLogic && sectorsSinceLastCloseCarBehindReport >= sectorsUntilNextCloseCarBehindReport && !currentGameState.FlagData.isLocalYellow)
                            {
                                sectorsSinceLastCloseCarBehindReport = 0;
                                // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                                if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                                {
                                    sectorsUntilNextCloseCarBehindReport = Utilities.random.Next(closeBehindMinSectorWait, closeBehindMaxSectorWait);
                                }
                                else
                                {
                                    sectorsUntilNextCloseCarBehindReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                        Utilities.random.Next(closeBehindMinSectorWait, closeBehindMaxSectorWait));
                                }
                                audioPlayer.playMessage(new QueuedMessage(folderBeingPressured, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 10);
                                OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                                if (opponent != null)
                                {
                                    DateTime lastTimeDriverNameUsed = DateTime.MinValue;
                                    if (!trackLandmarkDefendDriverNamesUsed.TryGetValue(opponent.DriverRawName, out lastTimeDriverNameUsed) ||
                                        lastTimeDriverNameUsed + minTimeBetweenAttackOrDefendByDriver < currentGameState.Now)
                                    {
                                        CrewChiefV4.GameState.TrackLandmarksTiming.LandmarkAndDeltaType landmarkAndDeltaType =
                                                currentGameState.SessionData.trackLandmarksTiming.getLandmarkWhereIAmSlower(opponent.trackLandmarksTiming, true, false);
                                        if (landmarkAndDeltaType.landmarkName != null)
                                        {
                                            // either we're slower on entry or slower through
                                            String defendFolder = landmarkAndDeltaType.deltaType == TrackLandmarksTiming.DeltaType.Time ? folderHeIsFasterThroughCorner : folderHeIsFasterEnteringCorner;
                                            audioPlayer.playMessage(new QueuedMessage("Timings/corner_to_defend_in", MessageContents(Pause(200), defendFolder, "corners/" + landmarkAndDeltaType.landmarkName), 0, this), 5);
                                            trackLandmarkDefendDriverNamesUsed[opponent.DriverRawName] = currentGameState.Now;
                                        }
                                    }
                                }
                                gapBehindAtLastReport = gapsBehind[0].timeDelta;
                            }
                        }
                        else if (gapBehindStatus != GapStatus.NONE && sectorsSinceLastGapBehindReport >= sectorsUntilNextGapBehindReport)
                        {
                            sectorsSinceLastGapBehindReport = 0;
                            // only prefer mid-lap gap reports if we're on a track with no ad-hoc gapPoints
                            if (currentGameState.SessionData.TrackDefinition.gapPoints.Count() > 0)
                            {
                                sectorsUntilNextGapBehindReport = Utilities.random.Next(gapBehindMinSectorWait, gapBehindMaxSectorWait);
                            }
                            else
                            {
                                sectorsUntilNextGapBehindReport = adjustForMidLapPreference(currentGameState.SessionData.SectorNumber,
                                    Utilities.random.Next(gapBehindMinSectorWait, gapBehindMaxSectorWait));
                            }
                            TimeSpanWrapper gapBehind = TimeSpanWrapper.FromMilliseconds(gapsBehind[0].timeDelta * 1000, Precision.AUTO_GAPS);
                            Boolean readGap = gapBehind.timeSpan.Seconds > 0 || gapBehind.timeSpan.Milliseconds > 50;
                            if (readGap)
                            {
                                if (gapBehindStatus == GapStatus.INCREASING)
                                {
                                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                                    if (opponent != null)
                                    {
                                        List<MessageFragment> primaryPartialMessageContents = MessageContents(folderTheGapTo, opponent, folderBehindIsIncreasing);
                                        List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapBehindIncreasing);
                                        int primaryGapIndex = 3;
                                        int alternateGapIndex = 1;
                                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            false, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                        audioPlayer.playMessage(new QueuedMessage("Timings/gap_behind", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 5);
                                    }
                                }
                                else if (gapBehindStatus == GapStatus.DECREASING)
                                {
                                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                                    if (opponent != null)
                                    {
                                        List<MessageFragment> primaryPartialMessageContents = MessageContents(opponent, folderIsReelingYouIn);
                                        List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapBehindDecreasing);
                                        int primaryGapIndex = 2;
                                        int alternateGapIndex = 1;
                                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            false, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                        audioPlayer.playMessage(new QueuedMessage("Timings/gap_behind", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 10);

                                        DateTime lastTimeDriverNameUsed = DateTime.MinValue;
                                        if (!trackLandmarkDefendDriverNamesUsed.TryGetValue(opponent.DriverRawName, out lastTimeDriverNameUsed) ||
                                            lastTimeDriverNameUsed + minTimeBetweenAttackOrDefendByDriver < currentGameState.Now)
                                        {
                                            CrewChiefV4.GameState.TrackLandmarksTiming.LandmarkAndDeltaType landmarkAndDeltaType =
                                                currentGameState.SessionData.trackLandmarksTiming.getLandmarkWhereIAmSlower(opponent.trackLandmarksTiming, true, false);
                                            if (landmarkAndDeltaType.landmarkName != null)
                                            {
                                                // either we're slower on entry or slower through
                                                String defendFolder = landmarkAndDeltaType.deltaType == TrackLandmarksTiming.DeltaType.Time ? folderHeIsFasterThroughCorner : folderHeIsFasterEnteringCorner;
                                                audioPlayer.playMessage(new QueuedMessage("Timings/corner_to_defend_in", MessageContents(Pause(200), defendFolder, "corners/" + landmarkAndDeltaType.landmarkName), 0, this), 5);
                                                trackLandmarkDefendDriverNamesUsed[opponent.DriverRawName] = currentGameState.Now;
                                            }
                                        }
                                    }
                                }
                                else if (gapBehindStatus == GapStatus.OTHER)
                                {
                                    OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                                    if (opponent != null)
                                    {
                                        List<MessageFragment> primaryPartialMessageContents = MessageContents(folderTheGapTo, opponent, folderBehindIsNow);
                                        List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapBehindIsNow);
                                        int primaryGapIndex = 3;
                                        int alternateGapIndex = 1;
                                        DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            false, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                        audioPlayer.playMessage(new QueuedMessage("Timings/gap_behind", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } }), 5);
                                    }
                                }
                            }
                            gapBehindAtLastReport = gapsBehind[0].timeDelta;
                        }
                    }
                }
                if (isRace && CrewChief.readOpponentDeltasForEveryLap && currentGameState.SessionData.CompletedLaps > 0)
                {
                    if (currentGameState.SessionData.ClassPosition > 1 && currentGameState.SessionData.IsNewLap)
                    {
                        if (currentGapInFront > 0.05)
                        {
                            OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                            if (opponent != null)
                            {
                                List<MessageFragment> primaryPartialMessageContents = MessageContents(folderTheGapTo, opponent, folderAheadIsNow);
                                List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapInFrontIsNow);
                                int primaryGapIndex = 3;
                                int alternateGapIndex = 1;
                                DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            true, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                QueuedMessage message = new QueuedMessage("Timings/gap_ahead", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } });
                                message.playEvenWhenSilenced = true;
                                audioPlayer.playMessage(message, 10);
                            }
                        }
                    }
                    if (!currentGameState.isLast())
                    {
                        // play the gap behind at the point when the opponent crosses the line, so the gap is about the same as our current laptime
                        if (!playedGapBehindForThisLap && currentGapBehind > 0.05 && currentGameState.SessionData.LapTimeCurrent > 0 &&
                            currentGameState.SessionData.LapTimeCurrent >= currentGapBehind &&
                            currentGameState.SessionData.LapTimeCurrent <= currentGapBehind + 1)
                        {
                            playedGapBehindForThisLap = true;
                            OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                            if (opponent != null)
                            {
                                List<MessageFragment> primaryPartialMessageContents = MessageContents(folderTheGapTo, opponent, folderBehindIsNow);
                                List<MessageFragment> alternatePartialMessageContents = MessageContents(folderGapBehindIsNow);
                                int primaryGapIndex = 3;
                                int alternateGapIndex = 1;
                                DelayedMessageEvent delayedMessageEvent = new DelayedMessageEvent("resolveGapAmount", new Object[] {
                                            false, primaryPartialMessageContents, primaryGapIndex, alternatePartialMessageContents, alternateGapIndex }, this);

                                QueuedMessage message = new QueuedMessage("Timings/gap_behind", delayedMessageEvent, 0, this, new Dictionary<string, object> { { "position", currentGameState.SessionData.ClassPosition } });
                                message.playEvenWhenSilenced = true;
                                audioPlayer.playMessage(message, 10);
                            }
                        }
                    }
                }
            }
        }

        private GapStatus getGapStatus(List<Gap> gaps, float lastReportedGap)
        {
            // when comparing gaps round to 1 decimal place
            if (gaps.Count < 3  // if we have less than 3 gaps in the list
                || gaps[0].timeDelta <= 0 || gaps[1].timeDelta <= 0 || gaps[2].timeDelta <= 0 || gaps[0].lapDelta < 0  // or not fully initialized
                || gaps[0].lapDelta > 0  // or at the last gap cars weren't on a same lap
                || gaps[0].timeDelta > 20  // or the last gap is too big
                || Math.Abs(gaps[0].timeDelta - gaps[1].timeDelta) > 5)  // or the change in the gap is too big, we don't want to report anything
            {
                return GapStatus.NONE;
            }
            else if (gaps[0].timeDelta < 0.5 && gaps[1].timeDelta < 0.5)
            {
                // this car has been close for 2 sectors
                return GapStatus.CLOSE;
            }
            else if ((lastReportedGap == -1 || Math.Round(gaps[0].timeDelta, 1) > Math.Round(lastReportedGap)) &&
                Math.Round(gaps[0].timeDelta, 1) > Math.Round(gaps[1].timeDelta, 1) && Math.Round(gaps[1].timeDelta, 1) > Math.Round(gaps[2].timeDelta, 1))
            {
                return GapStatus.INCREASING;
            }
            else if ((lastReportedGap == -1 || Math.Round(gaps[0].timeDelta, 1) < Math.Round(lastReportedGap)) &&
                Math.Round(gaps[0].timeDelta, 1) < Math.Round(gaps[1].timeDelta, 1) && Math.Round(gaps[1].timeDelta, 1) < Math.Round(gaps[2].timeDelta, 1))
            {
                return GapStatus.DECREASING;
            }
            else if (Math.Abs(gaps[0].timeDelta - gaps[1].timeDelta) < 1 && Math.Abs(gaps[0].timeDelta - gaps[1].timeDelta) < 1 && Math.Abs(gaps[0].timeDelta - gaps[2].timeDelta) < 1)
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
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SESSION_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                if (isLeading)
                {
                    if (currentGapBehind > 2)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Timings/gap_behind",
                                MessageContents(folderGapBehindIsNow, TimeSpanWrapper.FromMilliseconds(currentGapBehind * 1000, Precision.AUTO_GAPS)), 0, null));
                    }
                }
                else
                {
                    if (currentGapInFront > 2)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Timings/gap_in_front",
                            MessageContents(folderGapInFrontIsNow, TimeSpanWrapper.FromMilliseconds(currentGapInFront * 1000, Precision.AUTO_GAPS)), 0, null));
                    }
                }
            }
            else
            {
                Boolean haveData = false;
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHERE_AM_I_FASTER))
                {
                    GameStateData currentGameState = CrewChief.currentGameState;
                    if (currentGameState != null)
                    {
                        OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition - 1, currentGameState.carClass);
                        if (opponent != null)
                        {
                            CrewChiefV4.GameState.TrackLandmarksTiming.LandmarkAndDeltaType landmarkAndDeltaType =
                                currentGameState.SessionData.trackLandmarksTiming.getLandmarkWhereIAmFaster(opponent.trackLandmarksTiming, true, true);
                            if (landmarkAndDeltaType != null && landmarkAndDeltaType.landmarkName != null)
                            {
                                haveData = true;
                                // either we're faster on entry or faster through
                                String attackFolder = landmarkAndDeltaType.deltaType == TrackLandmarksTiming.DeltaType.Time ? folderHeIsSlowerThroughCorner : folderHeIsSlowerEnteringCorner;
                                audioPlayer.playMessageImmediately(new QueuedMessage("Timings/corner_to_attack_in",
                                    MessageContents(Pause(200), attackFolder, "corners/" + landmarkAndDeltaType.landmarkName), 0, null));
                                trackLandmarkAttackDriverNamesUsed[opponent.DriverRawName] = currentGameState.Now;
                            }
                        }
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHERE_AM_I_SLOWER))
                {
                    GameStateData currentGameState = CrewChief.currentGameState;
                    if (currentGameState != null)
                    {
                        OpponentData opponent = currentGameState.getOpponentAtClassPosition(currentGameState.SessionData.ClassPosition + 1, currentGameState.carClass);
                        if (opponent != null)
                        {
                            CrewChiefV4.GameState.TrackLandmarksTiming.LandmarkAndDeltaType landmarkAndDeltaType =
                                        currentGameState.SessionData.trackLandmarksTiming.getLandmarkWhereIAmSlower(opponent.trackLandmarksTiming, true, true);
                            if (landmarkAndDeltaType != null && landmarkAndDeltaType.landmarkName != null)
                            {
                                haveData = true;
                                // either we're slower on entry or slower through
                                String defendFolder = landmarkAndDeltaType.deltaType == TrackLandmarksTiming.DeltaType.Time ? folderHeIsFasterThroughCorner : folderHeIsFasterEnteringCorner;
                                audioPlayer.playMessageImmediately(new QueuedMessage("Timings/corner_to_defend_in",
                                    MessageContents(Pause(200), defendFolder, "corners/" + landmarkAndDeltaType.landmarkName), 0, null));
                                trackLandmarkDefendDriverNamesUsed[opponent.DriverRawName] = currentGameState.Now;
                            }
                        }
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_GAP_IN_FRONT) && currentGapInFront != -1)
                {
                    if (isLeading && isRace)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderLeading, 0, null));
                        haveData = true;
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Timings/gap_in_front",
                            MessageContents(TimeSpanWrapper.FromMilliseconds(currentGapInFront * 1000, Precision.AUTO_GAPS)), 0, null));
                        haveData = true;
                    }
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_GAP_BEHIND) &&
                    currentGapBehind != -1)
                {
                    if (isLast && isRace)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(Position.folderLast, 0, null));
                        haveData = true;
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("Timings/gap_behind",
                            MessageContents(TimeSpanWrapper.FromMilliseconds(currentGapBehind * 1000, Precision.AUTO_GAPS)), 0, null));
                        haveData = true;
                    }
                }
                if (!haveData)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
        }

        private enum GapStatus
        {
            CLOSE, INCREASING, DECREASING, OTHER, NONE
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

        // this call assumes the gap amount is NOT present in the list of primaryCurrentGapMessageContents (and alternateCurrentGapMessageContents 
        // if it exists)
        //
        // we insert the most recent gap (if isGapInFront -> ahead, else behind) into the primary and (if it's non-null) secondary message content
        // lists at the specified indexes
        public Tuple<List<MessageFragment>, List<MessageFragment>> resolveGapAmount(Boolean isGapInFront,
            List<MessageFragment> primaryCurrentGapMessageContents, int primaryGapAmountMessageIndex, 
            List<MessageFragment> alternateCurrentGapMessageContents, int alternateGapAmountMessageIndex)
        {
            float gapAmount = isGapInFront ? currentGapInFront : currentGapBehind;
            MessageFragment gapFragment = MessageFragment.Time(TimeSpanWrapper.FromSeconds(gapAmount, Precision.AUTO_GAPS));
            primaryCurrentGapMessageContents.Insert(primaryGapAmountMessageIndex, gapFragment);
            if (alternateGapAmountMessageIndex != -1 && alternateCurrentGapMessageContents != null)
            {
                alternateCurrentGapMessageContents.Insert(alternateGapAmountMessageIndex, gapFragment);
                return new Tuple<List<MessageFragment>, List<MessageFragment>>(primaryCurrentGapMessageContents, alternateCurrentGapMessageContents);
            }
            else
            {
                return new Tuple<List<MessageFragment>, List<MessageFragment>>(primaryCurrentGapMessageContents, null);
            }
        }
    }
}
