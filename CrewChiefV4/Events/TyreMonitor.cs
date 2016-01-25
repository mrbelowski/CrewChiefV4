using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class TyreMonitor : AbstractEvent
    {
        // tyre temp messages...
        private String folderColdFrontTyres = "tyre_monitor/cold_front_tyres";
        private String folderColdRearTyres = "tyre_monitor/cold_rear_tyres";
        private String folderColdLeftTyres = "tyre_monitor/cold_left_tyres";
        private String folderColdRightTyres = "tyre_monitor/cold_right_tyres";
        private String folderColdTyresAllRound = "tyre_monitor/cold_tyres_all_round";

        private String folderHotLeftFrontTyre = "tyre_monitor/hot_left_front_tyre";
        private String folderHotLeftRearTyre = "tyre_monitor/hot_left_rear_tyre";
        private String folderHotRightFrontTyre = "tyre_monitor/hot_right_front_tyre";
        private String folderHotRightRearTyre = "tyre_monitor/hot_right_rear_tyre";
        private String folderHotFrontTyres = "tyre_monitor/hot_front_tyres";
        private String folderHotRearTyres = "tyre_monitor/hot_rear_tyres";
        private String folderHotLeftTyres = "tyre_monitor/hot_left_tyres";
        private String folderHotRightTyres = "tyre_monitor/hot_right_tyres";
        private String folderHotTyresAllRound = "tyre_monitor/hot_tyres_all_round";

        public static String folderLeftFront = "tyre_monitor/left_front";
        public static String folderRightFront = "tyre_monitor/right_front";
        public static String folderLeftRear = "tyre_monitor/left_rear";
        public static String folderRightRear = "tyre_monitor/right_rear";

        private String folderCookingLeftFrontTyre = "tyre_monitor/cooking_left_front_tyre";
        private String folderCookingLeftRearTyre = "tyre_monitor/cooking_left_rear_tyre";
        private String folderCookingRightFrontTyre = "tyre_monitor/cooking_right_front_tyre";
        private String folderCookingRightRearTyre = "tyre_monitor/cooking_right_rear_tyre";
        private String folderCookingFrontTyres = "tyre_monitor/cooking_front_tyres";
        private String folderCookingRearTyres = "tyre_monitor/cooking_rear_tyres";
        private String folderCookingLeftTyres = "tyre_monitor/cooking_left_tyres";
        private String folderCookingRightTyres = "tyre_monitor/cooking_right_tyres";
        private String folderCookingTyresAllRound = "tyre_monitor/cooking_tyres_all_round";

        private String folderGoodTyreTemps = "tyre_monitor/good_tyre_temps";


        // brake temp messages...
        private String folderColdFrontBrakes = "tyre_monitor/cold_front_brakes";
        private String folderColdRearBrakes = "tyre_monitor/cold_rear_brakes";
        private String folderHotFrontBrakes = "tyre_monitor/hot_front_brakes";
        private String folderHotRearBrakes = "tyre_monitor/hot_rear_brakes";
        private String folderCookingFrontBrakes = "tyre_monitor/hot_front_brakes";
        private String folderCookingRearBrakes = "tyre_monitor/hot_rear_brakes";

        private String folderColdBrakesAllRound = "tyre_monitor/cold_brakes_all_round";
        private String folderGoodBrakeTemps = "tyre_monitor/good_brake_temps";
        private String folderHotBrakesAllRound = "tyre_monitor/hot_brakes_all_round";
        private String folderCookingBrakesAllRound = "tyre_monitor/cooking_brakes_all_round";      


        // tyre condition messages...
        private String folderKnackeredLeftFront = "tyre_monitor/knackered_left_front";
        private String folderKnackeredLeftRear = "tyre_monitor/knackered_left_rear";
        private String folderKnackeredRightFront = "tyre_monitor/knackered_right_front";
        private String folderKnackeredRightRear = "tyre_monitor/knackered_right_rear";
        private String folderKnackeredFronts = "tyre_monitor/knackered_fronts";
        private String folderKnackeredRears = "tyre_monitor/knackered_rears";
        private String folderKnackeredLefts = "tyre_monitor/knackered_lefts";
        private String folderKnackeredRights = "tyre_monitor/knackered_rights";
        private String folderKnackeredAllRound = "tyre_monitor/knackered_all_round";

        private String folderGoodWear = "tyre_monitor/good_wear";

        private String folderWornLeftFront = "tyre_monitor/worn_left_front";
        private String folderWornLeftRear = "tyre_monitor/worn_left_rear";
        private String folderWornRightFront = "tyre_monitor/worn_right_front";
        private String folderWornRightRear = "tyre_monitor/worn_right_rear";
        private String folderWornFronts = "tyre_monitor/worn_fronts";
        private String folderWornRears = "tyre_monitor/worn_rears";
        private String folderWornLefts = "tyre_monitor/worn_lefts";
        private String folderWornRights = "tyre_monitor/worn_rights";
        private String folderWornAllRound = "tyre_monitor/worn_all_round";

        public static String folderLapsOnCurrentTyresIntro = "tyre_monitor/laps_on_current_tyres_intro";
        public static String folderLapsOnCurrentTyresOutro = "tyre_monitor/laps_on_current_tyres_outro";
        public static String folderMinutesOnCurrentTyresIntro = "tyre_monitor/minutes_on_current_tyres_intro";
        public static String folderMinutesOnCurrentTyresOutro = "tyre_monitor/minutes_on_current_tyres_outro";

        public static String folderLockingFrontsForLapWarning = "tyre_monitor/locking_fronts_lap_warning";
        public static String folderLockingRearsForLapWarning = "tyre_monitor/locking_rears_lap_warning";
        public static String folderLockingLeftFrontForLapWarning = "tyre_monitor/locking_left_front_lap_warning";
        public static String folderLockingRightFrontForLapWarning = "tyre_monitor/locking_right_front_lap_warning";
        public static String folderLockingLeftRearForLapWarning = "tyre_monitor/locking_left_rear_lap_warning";
        public static String folderLockingRightRearForLapWarning = "tyre_monitor/locking_right_rear_lap_warning";

        public static String folderSpinningFrontsForLapWarning = "tyre_monitor/spinning_fronts_lap_warning";
        public static String folderSpinningRearsForLapWarning = "tyre_monitor/spinning_rears_lap_warning";
        public static String folderSpinningLeftFrontForLapWarning = "tyre_monitor/spinning_left_front_lap_warning";
        public static String folderSpinningRightFrontForLapWarning = "tyre_monitor/spinning_right_front_lap_warning";
        public static String folderSpinningLeftRearForLapWarning = "tyre_monitor/spinning_left_rear_lap_warning";
        public static String folderSpinningRightRearForLapWarning = "tyre_monitor/spinning_right_rear_lap_warning";

        private static Boolean enableTyreTempWarnings = UserSettings.GetUserSettings().getBoolean("enable_tyre_temp_warnings");
        private static Boolean enableBrakeTempWarnings = UserSettings.GetUserSettings().getBoolean("enable_brake_temp_warnings");
        private static Boolean enableTyreWearWarnings = UserSettings.GetUserSettings().getBoolean("enable_tyre_wear_warnings");

        private static Boolean useFahrenheit = UserSettings.GetUserSettings().getBoolean("use_fahrenheit");

        private static Boolean enableWheelSpinWarnings = UserSettings.GetUserSettings().getBoolean("enable_wheel_spin_warnings");
        private static Boolean enableBrakeLockWarnings = UserSettings.GetUserSettings().getBoolean("enable_brake_lock_warnings");

        private static float initialTotalLapLockupThreshold = UserSettings.GetUserSettings().getFloat("cumulative_lap_lockup_warning_threshold");
        private static float initialTotalWheelspinThreshold = UserSettings.GetUserSettings().getFloat("cumulative_lap_wheelspin_warning_threshold");

        // todo: warn on single lockups

        private int lapsIntoSessionBeforeTempMessage = 2;        

        // check at start of which sector (1=s/f line)
        private int checkBrakesAtSector = 3;

        private Boolean reportedTyreWearForCurrentPitEntry;

        private Boolean reportedEstimatedTimeLeft;
        
        private float leftFrontWearPercent;
        private float rightFrontWearPercent;
        private float leftRearWearPercent;
        private float rightRearWearPercent;

        private int completedLaps;
        private int lapsInSession;
        private float timeInSession;
        private float timeElapsed;

        private CornerData currentTyreConditionStatus;

        private CornerData currentTyreTempStatus;

        private CornerData currentBrakeTempStatus;

        private List<MessageFragment> lastTyreTempMessage = null;

        private List<MessageFragment> lastBrakeTempMessage = null;
        
        private List<MessageFragment> lastTyreConditionMessage = null;

        private float peakBrakeTempForLap = 0;

        private float timeLeftFrontIsLockedForLap = 0;
        private float timeRightFrontIsLockedForLap = 0; 
        private float timeLeftRearIsLockedForLap = 0; 
        private float timeRightRearIsLockedForLap = 0;
        private float timeLeftFrontIsSpinningForLap = 0;
        private float timeRightFrontIsSpinningForLap = 0;
        private float timeLeftRearIsSpinningForLap = 0;
        private float timeRightRearIsSpinningForLap = 0;

        private float leftFrontTyreTemp = 0;
        private float rightFrontTyreTemp = 0;
        private float leftRearTyreTemp = 0;
        private float rightRearTyreTemp = 0;

        private float leftFrontBrakeTemp = 0;
        private float rightFrontBrakeTemp = 0;
        private float leftRearBrakeTemp = 0;
        private float rightRearBrakeTemp = 0;

        private float totalLockupThresholdForNextLap = initialTotalLapLockupThreshold;
        private float totalWheelspinThresholdForNextLap = initialTotalWheelspinThreshold;
        
        private Boolean warnedOnLockingForLap = false;
        private Boolean warnedOnWheelspinForLap = false;

        private DateTime nextLockingAndSpinningCheck = DateTime.MinValue;

        private TimeSpan lockingAndSpinningCheckInterval = TimeSpan.FromSeconds(3);

        private Random random = new Random();
        
        public TyreMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            reportedTyreWearForCurrentPitEntry = false;
            reportedEstimatedTimeLeft = false;
            leftFrontWearPercent = 0;
            leftRearWearPercent = 0;
            rightFrontWearPercent = 0;
            rightRearWearPercent = 0;
            completedLaps = 0;
            lapsInSession = 0;
            timeInSession = 0;
            timeElapsed = 0;
            currentTyreConditionStatus = new CornerData();
            currentTyreTempStatus = new CornerData();
            currentBrakeTempStatus = new CornerData();
            lastTyreTempMessage = null;
            lastBrakeTempMessage = null;
            lastTyreConditionMessage = null;
            peakBrakeTempForLap = 0;
            timeLeftFrontIsLockedForLap = 0;
            timeRightFrontIsLockedForLap = 0; 
            timeLeftRearIsLockedForLap = 0; 
            timeRightRearIsLockedForLap = 0;
            timeLeftFrontIsSpinningForLap = 0;
            timeRightFrontIsSpinningForLap = 0;
            timeLeftRearIsSpinningForLap = 0;
            timeRightRearIsSpinningForLap = 0;
            totalLockupThresholdForNextLap = initialTotalLapLockupThreshold;
            totalWheelspinThresholdForNextLap = initialTotalWheelspinThreshold;
            warnedOnLockingForLap = false;
            warnedOnWheelspinForLap = false;
            nextLockingAndSpinningCheck = DateTime.MinValue;

            leftFrontTyreTemp = 0;
            rightFrontTyreTemp = 0;
            leftRearTyreTemp = 0;
            rightRearTyreTemp = 0;

            leftFrontBrakeTemp = 0;
            rightFrontBrakeTemp = 0;
            leftRearBrakeTemp = 0;
            rightRearBrakeTemp = 0;
        }

        private Boolean isBrakeTempPeakForLap(float leftFront, float rightFront, float leftRear, float rightRear) 
        {
            if (leftFront > peakBrakeTempForLap || rightFront > peakBrakeTempForLap || 
                leftRear > peakBrakeTempForLap || rightRear > peakBrakeTempForLap) {
                peakBrakeTempForLap = Math.Max(leftFront, Math.Max(rightFront, Math.Max(leftRear, rightRear)));
                return true;
            }
            return false;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.SessionData.IsNewLap)
            {
                timeLeftFrontIsLockedForLap = 0;
                timeRightFrontIsLockedForLap = 0;
                timeLeftRearIsLockedForLap = 0;
                timeRightRearIsLockedForLap = 0;
                timeLeftFrontIsSpinningForLap = 0;
                timeRightFrontIsSpinningForLap = 0;
                timeLeftRearIsSpinningForLap = 0;
                timeRightRearIsSpinningForLap = 0;
                if (warnedOnLockingForLap)
                {
                    totalLockupThresholdForNextLap = totalLockupThresholdForNextLap + 1;
                }
                else
                {
                    totalLockupThresholdForNextLap = initialTotalLapLockupThreshold;
                }
                if (warnedOnWheelspinForLap)
                {
                    totalWheelspinThresholdForNextLap = totalWheelspinThresholdForNextLap + 1;
                }
                else
                {
                    totalWheelspinThresholdForNextLap = initialTotalWheelspinThreshold;
                }
                warnedOnLockingForLap = false;
                warnedOnWheelspinForLap = false;
            }
            if (previousGameState != null && currentGameState.Ticks > previousGameState.Ticks) 
            {
                addLockingAndSpinningData(currentGameState.TyreData, previousGameState.Ticks, currentGameState.Ticks);
            }

            if (currentGameState.Now > nextLockingAndSpinningCheck)
            {
                if (enableBrakeLockWarnings)
                {
                    checkLocking();
                }
                if (enableWheelSpinWarnings)
                {
                    checkWheelSpinning();
                }
                nextLockingAndSpinningCheck = currentGameState.Now.Add(lockingAndSpinningCheckInterval);
            }

            leftFrontBrakeTemp = currentGameState.TyreData.LeftFrontBrakeTemp;
            rightFrontBrakeTemp = currentGameState.TyreData.RightFrontBrakeTemp;
            leftRearBrakeTemp = currentGameState.TyreData.LeftRearBrakeTemp;
            rightRearBrakeTemp = currentGameState.TyreData.RightRearBrakeTemp;
            leftFrontTyreTemp = currentGameState.TyreData.FrontLeft_CenterTemp;
            rightFrontTyreTemp = currentGameState.TyreData.FrontRight_CenterTemp;
            leftRearTyreTemp = currentGameState.TyreData.RearLeft_CenterTemp;
            rightRearTyreTemp = currentGameState.TyreData.RearRight_CenterTemp;

            if (currentGameState.TyreData.TireWearActive)
            {
                leftFrontWearPercent = currentGameState.TyreData.FrontLeftPercentWear;
                leftRearWearPercent = currentGameState.TyreData.RearLeftPercentWear;
                rightFrontWearPercent = currentGameState.TyreData.FrontRightPercentWear;
                rightRearWearPercent = currentGameState.TyreData.RearRightPercentWear;

                currentTyreConditionStatus = currentGameState.TyreData.TyreConditionStatus;
                currentTyreTempStatus = currentGameState.TyreData.TyreTempStatus;
                if (isBrakeTempPeakForLap(currentGameState.TyreData.LeftFrontBrakeTemp, 
                    currentGameState.TyreData.RightFrontBrakeTemp, currentGameState.TyreData.LeftRearBrakeTemp,
                    currentGameState.TyreData.RightRearBrakeTemp))
                {
                    currentBrakeTempStatus = currentGameState.TyreData.BrakeTempStatus;
                }

                completedLaps = currentGameState.SessionData.CompletedLaps;
                lapsInSession = currentGameState.SessionData.SessionNumberOfLaps;
                timeInSession = currentGameState.SessionData.SessionRunTime;
                timeElapsed = currentGameState.SessionData.SessionRunningTime;
                    
                if (currentGameState.PitData.InPitlane && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    if (currentGameState.SessionData.SessionType == SessionType.Race && enableTyreWearWarnings && !reportedTyreWearForCurrentPitEntry)
                    {
                        //reportCurrentTyreConditionStatus(false, true);
                        // sounds shit...
                        reportedTyreWearForCurrentPitEntry = true;
                    }
                }
                else
                {
                    reportedTyreWearForCurrentPitEntry = false;
                }
                if (currentGameState.SessionData.IsNewLap && !currentGameState.PitData.InPitlane && enableTyreWearWarnings && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    reportCurrentTyreConditionStatus(false, false);
                }
                if (!currentGameState.PitData.InPitlane && !reportedEstimatedTimeLeft && enableTyreWearWarnings && !currentGameState.SessionData.LeaderHasFinishedRace)
                {
                    reportEstimatedTyreLife(33, false);
                }
                // if the tyre wear has actually decreased, reset the 'reportdEstimatedTyreWear flag - assume this means the tyres have been changed
                if (previousGameState != null && (currentGameState.TyreData.FrontLeftPercentWear < previousGameState.TyreData.FrontLeftPercentWear ||
                    currentGameState.TyreData.FrontRightPercentWear < previousGameState.TyreData.FrontRightPercentWear ||
                    currentGameState.TyreData.RearRightPercentWear < previousGameState.TyreData.RearRightPercentWear ||
                    currentGameState.TyreData.RearLeftPercentWear < previousGameState.TyreData.RearLeftPercentWear))
                {
                    reportedEstimatedTimeLeft = true;
                }

                if (enableTyreTempWarnings && !currentGameState.SessionData.LeaderHasFinishedRace &&
                    !currentGameState.PitData.InPitlane &&
                    currentGameState.SessionData.CompletedLaps >= lapsIntoSessionBeforeTempMessage && currentGameState.SessionData.IsNewLap)
                {
                    reportCurrentTyreTempStatus(false);
                }
                if (!currentGameState.SessionData.LeaderHasFinishedRace &&
                     ((checkBrakesAtSector == 1 && currentGameState.SessionData.IsNewLap) ||
                     ((currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == checkBrakesAtSector))))
                {
                    if (!currentGameState.PitData.InPitlane && currentGameState.SessionData.CompletedLaps >= lapsIntoSessionBeforeTempMessage)
                    {
                        if (enableBrakeTempWarnings)
                        {
                            reportCurrentBrakeTempStatus(false);
                        }
                    }
                    peakBrakeTempForLap = 0;
                }
            }
        }

        private void reportCurrentTyreTempStatus(Boolean playImmediately)
        {
            List<MessageFragment> messageContents = new List<MessageFragment>();
            addTyreTempWarningMessages(currentTyreTempStatus.getCornersForStatus(TyreTemp.COLD), TyreTemp.COLD, messageContents);
            addTyreTempWarningMessages(currentTyreTempStatus.getCornersForStatus(TyreTemp.HOT), TyreTemp.HOT, messageContents);
            addTyreTempWarningMessages(currentTyreTempStatus.getCornersForStatus(TyreTemp.COOKING), TyreTemp.COOKING, messageContents);
            if (messageContents.Count == 0)
            {
                messageContents.Add(MessageFragment.Text(folderGoodTyreTemps));
            }            

            if (playImmediately)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("tyre_temps", messageContents, 0, null), false);
                
            }
            else if (lastTyreTempMessage == null || !messagesHaveSameContent(lastTyreTempMessage, messageContents))
            {
                audioPlayer.playMessage(new QueuedMessage("tyre_temps", messageContents, random.Next(0, 10), this));
            }
            lastTyreTempMessage = messageContents;
        }

        private void reportCurrentBrakeTempStatus(Boolean playImmediately)
        {
            List<MessageFragment> messageContents = new List<MessageFragment>();
            addBrakeTempWarningMessages(currentBrakeTempStatus.getCornersForStatus(BrakeTemp.COLD), BrakeTemp.COLD, messageContents);
            addBrakeTempWarningMessages(currentBrakeTempStatus.getCornersForStatus(BrakeTemp.HOT), BrakeTemp.HOT, messageContents);
            addBrakeTempWarningMessages(currentBrakeTempStatus.getCornersForStatus(BrakeTemp.COOKING), BrakeTemp.COOKING, messageContents);
            if (messageContents.Count == 0)
            {
                messageContents.Add(MessageFragment.Text(folderGoodBrakeTemps));
            }

            if (playImmediately)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("brake_temps", messageContents, 0, null), false);
                
            }
            else if (lastBrakeTempMessage == null || !messagesHaveSameContent(lastBrakeTempMessage, messageContents))
            {
                audioPlayer.playMessage(new QueuedMessage("brake_temps", messageContents, random.Next(0, 10), this));
            }
            lastBrakeTempMessage = messageContents;
        }

        private void reportCurrentTyreConditionStatus(Boolean playImmediately, Boolean playEvenIfUnchanged)
        {
            List<MessageFragment> messageContents = new List<MessageFragment>();
            addTyreConditionWarningMessages(currentTyreConditionStatus.getCornersForStatus(TyreCondition.MINOR_WEAR), TyreCondition.MINOR_WEAR, messageContents);
            addTyreConditionWarningMessages(currentTyreConditionStatus.getCornersForStatus(TyreCondition.MAJOR_WEAR), TyreCondition.MAJOR_WEAR, messageContents);
            addTyreConditionWarningMessages(currentTyreConditionStatus.getCornersForStatus(TyreCondition.WORN_OUT), TyreCondition.WORN_OUT, messageContents);
            if (messageContents.Count == 0)
            {
                messageContents.Add(MessageFragment.Text(folderGoodWear));
            }

            if (playImmediately)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("tyre_condition", messageContents, 0, null), false);
                
            }
            else if (playEvenIfUnchanged || (lastTyreConditionMessage != null && !messagesHaveSameContent(lastTyreConditionMessage, messageContents)))
            {
                audioPlayer.playMessage(new QueuedMessage("tyre_condition", messageContents, random.Next(0, 10), this));
            }
            lastTyreConditionMessage = messageContents;
        }

        private void playEstimatedTypeLifeMinutes(int minutesRemainingOnTheseTyres, Boolean immediate)
        {
            if (immediate)
            {
                if (minutesRemainingOnTheseTyres > (timeInSession - timeElapsed) / 60)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderGoodWear, 0, null), false);
                    
                    return;
                }
                else if (minutesRemainingOnTheseTyres < 1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderKnackeredAllRound, 0, null), false);
                    
                    return;
                }
            }
            if (minutesRemainingOnTheseTyres > 1 &&
                        minutesRemainingOnTheseTyres <= (timeInSession - timeElapsed) / 60)
            {
                QueuedMessage queuedMessage = new QueuedMessage("minutes_on_current_tyres",
                    MessageContents(folderMinutesOnCurrentTyresIntro, minutesRemainingOnTheseTyres, folderMinutesOnCurrentTyresOutro), 0, null);
                if (immediate)
                {
                    audioPlayer.playMessageImmediately(queuedMessage, false);
                    
                }
                else
                {
                    audioPlayer.playMessage(queuedMessage);
                }
            }
            else if (immediate)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                
            }
        }

        private void playEstimatedTypeLifeLaps(int lapsRemainingOnTheseTyres, Boolean immediate)
        {
            if (immediate)
            {
                if (lapsRemainingOnTheseTyres > lapsInSession - completedLaps)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderGoodWear, 0, null), false);
                    
                    return;
                }
                else if (lapsRemainingOnTheseTyres < 1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(folderKnackeredAllRound, 0, null), false);
                    
                    return;
                }
            }
            if (lapsRemainingOnTheseTyres > 1 &&
                        lapsRemainingOnTheseTyres <= lapsInSession - completedLaps)
            {
                QueuedMessage queuedMessage = new QueuedMessage("laps_on_current_tyres",
                    MessageContents(folderLapsOnCurrentTyresIntro, lapsRemainingOnTheseTyres, folderLapsOnCurrentTyresOutro), 0, null);
                if (immediate)
                {
                    audioPlayer.playMessageImmediately(queuedMessage, false);
                    
                }
                else
                {
                    audioPlayer.playMessage(queuedMessage);
                }                
            }
            else if (immediate)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                
            }
        }

        private void reportEstimatedTyreLife(float maxWearThreshold, Boolean immediate)
        {
            float maxWearPercent = Math.Max(leftFrontWearPercent, Math.Max(rightFrontWearPercent, Math.Max(leftRearWearPercent, rightRearWearPercent)));
            if (maxWearPercent >= maxWearThreshold)
            {
                // 1/3 through the tyre's life
                reportedEstimatedTimeLeft = true;
                if (lapsInSession > 0 || timeInSession == 0)
                {
                    int lapsRemainingOnTheseTyres = (int)(completedLaps / (maxWearPercent / 100)) - completedLaps - 1;
                    playEstimatedTypeLifeLaps(lapsRemainingOnTheseTyres, immediate);
                }
                else
                {
                    int minutesRemainingOnTheseTyres = (int)Math.Round((timeElapsed / (maxWearPercent / 100)) - timeElapsed - 1);
                    playEstimatedTypeLifeMinutes(minutesRemainingOnTheseTyres, immediate);
                }
            }
        }

        private void reportCurrentTyreTemps()
        {
            if (leftFrontTyreTemp == 0 && rightFrontTyreTemp == 0 && leftRearTyreTemp == 0 && rightRearTyreTemp == 0)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
            }
            else
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("tyre_temps", MessageContents(folderLeftFront, convertTemp(leftFrontTyreTemp), 
                    folderRightFront, convertTemp(rightFrontTyreTemp), folderLeftRear, convertTemp(leftRearTyreTemp), folderRightRear, convertTemp(rightRearTyreTemp), getTempUnit()), 0, this), false);
            }
            
        }

        private void reportCurrentBrakeTemps()
        {
            if (leftFrontBrakeTemp == 0 && rightFrontBrakeTemp == 0 && leftRearBrakeTemp == 0 && rightRearBrakeTemp == 0)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
            }
            else
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("brake_temps", MessageContents(folderLeftFront, convertTemp(leftFrontBrakeTemp, 10), 
                    folderRightFront, convertTemp(rightFrontBrakeTemp, 10), folderLeftRear, convertTemp(leftRearBrakeTemp, 10), folderRightRear, convertTemp(rightRearBrakeTemp, 10), getTempUnit()), 0, this), false);
            }
            
        }

        public override void respond(string voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.TYRE_TEMPS) || voiceMessage.Contains(SpeechRecogniser.TYRE_TEMPERATURES))
            {
                if (voiceMessage.Contains(SpeechRecogniser.WHAT_ARE_MY))
                {
                    reportCurrentTyreTemps();
                }
                else
                {
                    if (currentTyreTempStatus != null)
                    {
                        reportCurrentTyreTempStatus(true);
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                        
                    }
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.BRAKE_TEMPS) || voiceMessage.Contains(SpeechRecogniser.BRAKE_TEMPERATURES))
            {
                if (voiceMessage.Contains(SpeechRecogniser.WHAT_ARE_MY))
                {
                    reportCurrentBrakeTemps();
                }
                else
                {
                    if (currentBrakeTempStatus != null)
                    {
                        reportCurrentBrakeTempStatus(true);
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                        
                    }
                }
            }
            else if (voiceMessage.Contains(SpeechRecogniser.TYRE_WEAR))
            {
                if (currentTyreConditionStatus != null)
                {
                    reportCurrentTyreConditionStatus(true, true);
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                    
                }
            }
        }

        private void addTyreTempWarningMessages(CornerData.Corners corners, TyreTemp tyreTemp, List<MessageFragment> messageContents)
        {
            switch (corners)
            {
                case CornerData.Corners.ALL:
                    switch (tyreTemp)
                    {
                        case TyreTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdTyresAllRound));
                            break;
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotTyresAllRound));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingTyresAllRound));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONTS:
                    switch (tyreTemp)
                    {
                        case TyreTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdFrontTyres));
                            break;
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotFrontTyres));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingFrontTyres));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REARS:
                    switch (tyreTemp)
                    {
                        case TyreTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdRearTyres));
                            break;
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotRearTyres));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingRearTyres));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.LEFTS:
                    switch (tyreTemp)
                    {
                        case TyreTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdLeftTyres));
                            break;
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotLeftTyres));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingLeftTyres));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.RIGHTS:
                    switch (tyreTemp)
                    {
                        case TyreTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdRightTyres));
                            break;
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotRightTyres));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingRightTyres));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONT_LEFT:
                    switch (tyreTemp)
                    {
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotLeftFrontTyre));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingLeftFrontTyre));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONT_RIGHT:
                    switch (tyreTemp)
                    {
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotRightFrontTyre));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingRightFrontTyre));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REAR_LEFT:
                    switch (tyreTemp)
                    {
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotLeftRearTyre));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingLeftRearTyre));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REAR_RIGHT:
                    switch (tyreTemp)
                    {
                        case TyreTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotRightRearTyre));
                            break;
                        case TyreTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingRightRearTyre));
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }

        private void addTyreConditionWarningMessages(CornerData.Corners corners, TyreCondition tyreCondition, List<MessageFragment> messageContents)
        {
            switch (corners)
            {
                case CornerData.Corners.ALL:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornAllRound));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredAllRound));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONTS:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornFronts));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredFronts));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REARS:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornRears));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredRears));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.LEFTS:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornLefts));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredLefts));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.RIGHTS:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornRights));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredRights));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONT_LEFT:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornLeftFront));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredLeftFront));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONT_RIGHT:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornRightFront));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredRightFront));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REAR_LEFT:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornLeftRear));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredLeftRear));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REAR_RIGHT:
                    switch (tyreCondition)
                    {
                        case TyreCondition.MAJOR_WEAR:
                            messageContents.Add(MessageFragment.Text(folderWornRightRear));
                            break;
                        case TyreCondition.WORN_OUT:
                            messageContents.Add(MessageFragment.Text(folderKnackeredRightRear));
                            break;
                        default:
                            break;
                    }
                    break;
            }
        }

        private void addBrakeTempWarningMessages(CornerData.Corners corners, BrakeTemp brakeTemp, List<MessageFragment> messageContents)
        {
            switch (corners)
            {
                case CornerData.Corners.ALL:
                    switch (brakeTemp)
                    {
                        case BrakeTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdBrakesAllRound));
                            break;
                        case BrakeTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotBrakesAllRound));
                            break;
                        case BrakeTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingBrakesAllRound));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.FRONTS:
                    switch (brakeTemp)
                    {
                        case BrakeTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdFrontBrakes));
                            break;
                        case BrakeTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotFrontBrakes));
                            break;
                        case BrakeTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingFrontBrakes));
                            break;
                        default:
                            break;
                    }
                    break;
                case CornerData.Corners.REARS:
                    switch (brakeTemp)
                    {
                        case BrakeTemp.COLD:
                            messageContents.Add(MessageFragment.Text(folderColdRearBrakes));
                            break;
                        case BrakeTemp.HOT:
                            messageContents.Add(MessageFragment.Text(folderHotRearBrakes));
                            break;
                        case BrakeTemp.COOKING:
                            messageContents.Add(MessageFragment.Text(folderCookingRearBrakes));
                            break;
                        default:
                            break;
                    }
                    break;
                default:
                    break;
            }
        }
        private void addLockingAndSpinningData(TyreData tyreData, long previousTicks, long currentTicks)
        {
            if (enableBrakeLockWarnings)
            {
                if (tyreData.LeftFrontIsLocked)
                {
                    timeLeftFrontIsLockedForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
                if (tyreData.RightFrontIsLocked)
                {
                    timeRightFrontIsLockedForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
                if (tyreData.LeftRearIsLocked)
                {
                    timeLeftRearIsLockedForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
                if (tyreData.RightRearIsLocked)
                {
                    timeRightRearIsLockedForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
            }
            if (enableWheelSpinWarnings)
            {
                if (tyreData.LeftFrontIsSpinning)
                {
                    timeLeftFrontIsSpinningForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
                if (tyreData.RightFrontIsSpinning)
                {
                    timeRightFrontIsSpinningForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
                if (tyreData.LeftRearIsSpinning)
                {
                    timeLeftRearIsSpinningForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
                if (tyreData.RightRearIsSpinning)
                {
                    timeRightRearIsSpinningForLap += (float)(currentTicks - previousTicks) / (float)TimeSpan.TicksPerSecond;
                }
            }
        }

        private void checkLocking()
        {
            int messageDelay = random.Next(0, 5);
            if (!warnedOnLockingForLap)
            {
                if (timeLeftFrontIsLockedForLap > totalLockupThresholdForNextLap)
                {
                    warnedOnLockingForLap = true;
                    if (timeRightFrontIsLockedForLap > totalLockupThresholdForNextLap / 2)
                    {
                        // lots of left front locking, some right front locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingFrontsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just left front locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingLeftFrontForLapWarning, messageDelay, this));
                    }
                }
                else if (timeRightFrontIsLockedForLap > totalLockupThresholdForNextLap)
                {
                    warnedOnLockingForLap = true;
                    if (timeLeftFrontIsLockedForLap > totalLockupThresholdForNextLap / 2)
                    {
                        // lots of right front locking, some left front locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingFrontsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just right front locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingRightFrontForLapWarning, messageDelay, this));
                    }
                }
                else if (timeLeftRearIsLockedForLap > totalLockupThresholdForNextLap)
                {
                    warnedOnLockingForLap = true;
                    if (timeRightRearIsLockedForLap > totalLockupThresholdForNextLap / 2)
                    {
                        // lots of left rear locking, some right rear locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingRearsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just left rear locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingLeftRearForLapWarning, messageDelay, this));
                    }
                }
                else if (timeRightRearIsLockedForLap > totalLockupThresholdForNextLap)
                {
                    warnedOnLockingForLap = true;
                    if (timeLeftRearIsLockedForLap > totalLockupThresholdForNextLap / 2)
                    {
                        // lots of right rear locking, some left rear locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingRearsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just right rear locking
                        audioPlayer.playMessage(new QueuedMessage(folderLockingRightRearForLapWarning, messageDelay, this));
                    }
                }
            }
        }

        private void checkWheelSpinning()
        {
            int messageDelay = random.Next(0, 5);
            if (!warnedOnWheelspinForLap)
            {
                if (timeLeftFrontIsSpinningForLap > totalWheelspinThresholdForNextLap)
                {
                    warnedOnWheelspinForLap = true;
                    if (timeRightFrontIsSpinningForLap > totalWheelspinThresholdForNextLap / 2)
                    {
                        // lots of left front spinning, some right front spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningFrontsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just left front spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningLeftFrontForLapWarning, messageDelay, this));
                    }
                }
                else if (timeRightFrontIsSpinningForLap > totalWheelspinThresholdForNextLap)
                {
                    warnedOnWheelspinForLap = true;
                    if (timeLeftFrontIsSpinningForLap > totalWheelspinThresholdForNextLap / 2)
                    {
                        // lots of right front spinning, some left front spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningFrontsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just right front spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningRightFrontForLapWarning, messageDelay, this));
                    }
                }
                else if (timeLeftRearIsSpinningForLap > totalWheelspinThresholdForNextLap)
                {
                    warnedOnWheelspinForLap = true;
                    if (timeRightRearIsSpinningForLap > totalWheelspinThresholdForNextLap / 2)
                    {
                        // lots of left rear spinning, some right rear spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningRearsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just left rear spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningLeftRearForLapWarning, messageDelay, this));
                    }
                }
                else if (timeRightRearIsSpinningForLap > totalWheelspinThresholdForNextLap)
                {
                    warnedOnWheelspinForLap = true;
                    if (timeLeftRearIsSpinningForLap > totalWheelspinThresholdForNextLap / 2)
                    {
                        // lots of right rear spinning, some left rear spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningRearsForLapWarning, messageDelay, this));
                    }
                    else
                    {
                        // just right rear spinning
                        audioPlayer.playMessage(new QueuedMessage(folderSpinningRightRearForLapWarning, messageDelay, this));
                    }
                }
            }
        }
    }
}
