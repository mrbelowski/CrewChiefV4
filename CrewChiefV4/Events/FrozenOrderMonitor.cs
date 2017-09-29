/*
 * This monitor announces order information during Full Course Yellow/Yellow (in NASCAR), Rolling start and Formation/standing starts.
 * 
 * Official website: thecrewchief.org 
 * License: MIT
 */
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.RaceRoom.RaceRoomData;
using System.Threading;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class FrozenOrderMonitor : AbstractEvent
    {
        private const int ACTION_STABLE_THRESHOLD = 3;

        // Number of updates FO Action and Driver name were the same.
        private int numUpdatesActionSame = 0;

        // Last FO Action and Driver name announced.
        private FrozenOrderAction currFrozenOrderAction = FrozenOrderAction.None;
        private string currDriverToFollow = "";

        // Next FO Action and Driver to be announced if it stays stable for a ACTION_STABLE_THRESHOLD times.
        private FrozenOrderAction newFrozenOrderAction = FrozenOrderAction.None;
        private string newDriverToFollow = "";

        private bool formationStandingStartAnnounced = false;
        private bool formationStandingPreStartReminderAnnounced = false;


        public FrozenOrderMonitor(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override List<SessionType> applicableSessionTypes
        {
            get { return new List<SessionType> { SessionType.Race }; }
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Formation, SessionPhase.FullCourseYellow }; }
        }

        /*
         * IMPORTANT: This method is called twice - when the message becomes due, and immediately before playing it (which may have a 
         * delay caused by the length of the queue at the time). So be *very* careful when checking and updating local state in here.
         */
        public override bool isMessageStillValid(string eventSubType, GameStateData currentGameState, Dictionary<String, Object> validationData)
        {
            /*if (base.isMessageStillValid(eventSubType, currentGameState, validationData))
            {
                if (currentGameState.PitData.InPitlane)
                {
                    return false;
                }
                if (validationData == null) {
                    return true;
                }
                if ((Boolean) validationData[isValidatingSectorMessage])
                {                    
                    int sectorIndex = (int)validationData[validationSectorNumberKey];
                    FlagEnum sectorFlagWhenQueued = (FlagEnum)validationData[validationSectorFlagKey];
                    if (lastSectorFlags[sectorIndex] == sectorFlagWhenQueued)
                    {
                        lastSectorFlagsReported[sectorIndex] = sectorFlagWhenQueued;
                        lastSectorFlagsReportedTime[sectorIndex] = currentGameState.Now;
                        //Console.WriteLine("FLAG_DEBUG: transition to sector " + (sectorIndex + 1) + " " + sectorFlagWhenQueued + " is valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return true;
                    }
                    else
                    {
                        // reset the last reported flag and the time so we can report this flag transition when it's actually valid:
                        lastSectorFlagsReported[sectorIndex] = sectorFlagWhenQueued == FlagEnum.YELLOW ? FlagEnum.GREEN : FlagEnum.YELLOW;
                        lastSectorFlagsReportedTime[sectorIndex] = DateTime.MinValue;
                        //Console.WriteLine("FLAG_DEBUG: transition to sector " + (sectorIndex + 1) + " " + sectorFlagWhenQueued + " is NOT valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return false;
                    }
                }
                else
                {
                    Boolean wasLocalYellow = (Boolean)validationData[validationIsLocalYellowKey];
                    if (currentGameState.FlagData.isLocalYellow && wasLocalYellow)
                    {
                        hasReportedIsUnderLocalYellow = true;
                        lastSectorFlagsReported[currentGameState.SessionData.SectorNumber - 1] = FlagEnum.YELLOW;
                        lastSectorFlagsReportedTime[currentGameState.SessionData.SectorNumber - 1] = currentGameState.Now;
                        //Console.WriteLine("FLAG_DEBUG: transition to local YELLOW is valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return true;
                    }
                    else if (!currentGameState.FlagData.isLocalYellow && !wasLocalYellow && !currentGameState.PitData.InPitlane)
                    {
                        hasReportedIsUnderLocalYellow = false;
                        // don't change the local sector state to green here - it might remain yellow after we pass the incident
                        // lastSectorFlagsReported[currentGameState.SessionData.SectorNumber - 1] = FlagEnum.GREEN;
                        lastSectorFlagsReportedTime[currentGameState.SessionData.SectorNumber - 1] = currentGameState.Now;
                        //Console.WriteLine("FLAG_DEBUG: transition to local GREEN is valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                        return true;
                    }
                    else
                    {
                       // Console.WriteLine("FLAG_DEBUG: transition to local " + (wasLocalYellow ? "YELLOW" : "GREEN") + " is NOT valid at " + currentGameState.Now.ToString("HH:mm:ss"));
                    }
                }
            }*/
            return false;
        }

        public override void clearState()
        {
            this.formationStandingStartAnnounced = false;
            this.formationStandingPreStartReminderAnnounced = false;
            this.numUpdatesActionSame = 0;
            this.newFrozenOrderAction = FrozenOrderAction.None;
            this.newDriverToFollow = "";
            this.currFrozenOrderAction = FrozenOrderAction.None;
            this.currDriverToFollow = "";
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            /*
            rolling\recording_2017-22-9--09-45-31.xml
            */
            var cgs = currentGameState;
            var pgs = previousGameState;
            if (cgs.PitData.InPitlane /*|| cgs.SessionData.SessionRunningTime < 10 */
                || GameStateData.onManualFormationLap  // We may want manual formation to phase of FrozenOrder.
                || pgs == null)
                return; // don't process if we're in the pits or just started a session

            var cfod = cgs.FrozenOrderData;
            var pfod = pgs.FrozenOrderData;
            var cfodp = cgs.FrozenOrderData.Phase;
            if (cfodp == FrozenOrderPhase.None)
                return;  // Nothing to do.

            var useAmericanTerms = GlobalBehaviourSettings.useAmericanTerms || GlobalBehaviourSettings.useOvalLogic;
            var useOvalLogic = GlobalBehaviourSettings.useOvalLogic;

            if (pfod.Phase == FrozenOrderPhase.None)
            {
                Console.WriteLine("FO: NEW PHASE DETECTED: " + cfod.Phase);

                if (cfod.Phase == FrozenOrderPhase.Rolling)
                    Console.WriteLine("FO: OK, THAT'S A ROLLING START.");
                else if (cfod.Phase == FrozenOrderPhase.FormationStanding)
                    Console.WriteLine("FO: OK, THAT'S FORMATION/STANDING START.");

                // Clear previous state.
                this.clearState();
            }

            // Because FO Action is distance dependent, it tends to fluctuate.
            // We need to detect when it stabilizes (values stay identical for ACTION_STABLE_THRESHOLD times).
            if (cfod.Action == pfod.Action
                && cfod.DriverToFollowRaw == pfod.DriverToFollowRaw)
                ++this.numUpdatesActionSame;
            else
            {
                this.newFrozenOrderAction = cfod.Action;
                this.newDriverToFollow = cfod.DriverToFollowRaw;

                this.numUpdatesActionSame = 0;
            }

            var isActionUpdateStable = this.numUpdatesActionSame >= FrozenOrderMonitor.ACTION_STABLE_THRESHOLD;

            if (cfodp == FrozenOrderPhase.Rolling)
            {
                var shouldFollowSafetyCar = cfod.AssignedGridPosition == 1;
                var driverToFollow = shouldFollowSafetyCar ? (useAmericanTerms ? "Pace car" : "Safety car") : cfod.DriverToFollowRaw;

                if (isActionUpdateStable
                    && (this.currFrozenOrderAction != this.newFrozenOrderAction
                        || this.currDriverToFollow != this.newDriverToFollow))
                {
                    this.currFrozenOrderAction = this.newFrozenOrderAction;
                    this.currDriverToFollow = this.newDriverToFollow;

                    if (this.newFrozenOrderAction == FrozenOrderAction.Follow)
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToFollow)))
                        {
                            string columnName;
                            if (useOvalLogic)
                                columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? "inside" : "outside";
                            else
                                columnName = cfod.AssignedColumn.ToString();

                            Console.WriteLine(string.Format("FO: FOLLOW DIRVER: {0} IN COLUMN {1}", driverToFollow, columnName));
                        }
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.AllowToPass)
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToFollow)))
                            Console.WriteLine(string.Format("FO: ALLOW DIRVER: {0} TO PASS", driverToFollow));
                        else
                            Console.WriteLine(string.Format("FO: YOU ARE AHEAD OF A GUY YOU SHOULD BE FOLLOWING, LET HIM PASS"));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.CatchUp)
                    {
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToFollow)))
                            Console.WriteLine(string.Format("FO: CATCH UP TO DIRVER: {0}", driverToFollow)); 
                        else
                            Console.WriteLine(string.Format("FO: YOU NEED TO CATCH UP TO THE GUY YOU SHOULD BE FOLLOWING"));

                    }
                }

                if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
                {
                    if (useAmericanTerms)
                        Console.WriteLine("FO: PACE CAR JUST LEFT.");
                    else
                        Console.WriteLine("FO: SAFETY CAR JUST LEFT.");
                }
            }
            else if (cfodp == FrozenOrderPhase.FullCourseYellow)
            {
                var shouldFollowSafetyCar = cfod.AssignedPosition == 1;
                var driverToFollow = shouldFollowSafetyCar ? (useAmericanTerms ? "Pace car" : "Safety car") : cfod.DriverToFollowRaw;

                if (isActionUpdateStable
                    && (this.currFrozenOrderAction != this.newFrozenOrderAction
                        || this.currDriverToFollow != this.newDriverToFollow))
                {
                    this.currFrozenOrderAction = this.newFrozenOrderAction;
                    this.currDriverToFollow = this.newDriverToFollow;

                    if (this.newFrozenOrderAction == FrozenOrderAction.Follow)
                    {
                        // Follow messages are only meaningful if there's name to announce.
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToFollow)))
                            Console.WriteLine(string.Format("FO: FOLLOW DIRVER: {0}", driverToFollow));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.AllowToPass)
                    {
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToFollow)))
                            Console.WriteLine(string.Format("FO: ALLOW DIRVER: {0} TO PASS", driverToFollow));
                        else
                            Console.WriteLine(string.Format("FO: YOU ARE AHEAD OF A GUY YOU SHOULD BE FOLLOWING, LET HIM PASS"));
                    }
                    else if (this.newFrozenOrderAction == FrozenOrderAction.CatchUp)
                    {
                        if (SoundCache.hasSuitableTTSVoice || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(driverToFollow)))
                            Console.WriteLine(string.Format("FO: CATCH UP TO DIRVER: {0}", driverToFollow));
                        else
                            Console.WriteLine(string.Format("FO: YOU NEED TO CATCH UP TO THE GUY YOU SHOULD BE FOLLOWING"));
                    }
                }

                if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
                {
                    if (useAmericanTerms)
                        Console.WriteLine("FO: PACE CAR JUST LEFT.");
                    else
                        Console.WriteLine("FO: SAFETY CAR JUST LEFT.");
                }
            }
            else if (cfodp == FrozenOrderPhase.FormationStanding)
            {
                string columnName;
                if (useOvalLogic)
                    columnName = cfod.AssignedColumn == FrozenOrderColumn.Left ? "inside" : "outside";
                else
                    columnName = cfod.AssignedColumn.ToString();

                if (!this.formationStandingStartAnnounced && cgs.SessionData.SessionRunningTime > 10)
                {
                    this.formationStandingStartAnnounced = true;
                    var isStartingFromPole = cfod.AssignedPosition == 1;
                    if (isStartingFromPole)
                    {
                        Console.WriteLine(string.Format("FO: YOU'RE STARTING FROM THE POLE IN THE {0} COLUMN", columnName));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("FO: YOU'RE STARTING FROM POSITION {0} ROW {1} IN THE {2} COLUMN", cfod.AssignedPosition, cfod.AssignedGridPosition, columnName));
                    }
                }

                // TODO: Not sure how will this play with formation lap message in S3.
                if (!this.formationStandingPreStartReminderAnnounced
                    && cgs.SessionData.SectorNumber == 3
                    && cgs.PositionAndMotionData.DistanceRoundTrack > cgs.SessionData.TrackDefinition.distanceForNearPitEntryChecks)
                {
                    this.formationStandingPreStartReminderAnnounced = true;
                    var isStartingFromPole = cfod.AssignedPosition == 1;
                    if (isStartingFromPole)
                    {
                        Console.WriteLine(string.Format("FO: GET READY, YOU'RE STARTING FROM THE POLE IN THE {0} COLUMN", columnName));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("FO: GET READY, YOU'RE STARTING FROM POSITION {0} ROW {1} IN THE {2} COLUMN", cfod.AssignedPosition, cfod.AssignedGridPosition, columnName));
                    }
                }
            }

            // For fast rolling, do nothing for now.
        }
    }
}
