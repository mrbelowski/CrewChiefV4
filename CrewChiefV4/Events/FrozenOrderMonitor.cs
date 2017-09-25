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

            if (pfod.Phase == FrozenOrderPhase.None)
            {
                Console.WriteLine("FO: NEW PHASE DETECTED: " + cfod.Phase);

                // Clear previous state.
                this.clearState();
            }

            if (cfodp == FrozenOrderPhase.Rolling)
            {
                var shouldFollowSafetyCar = cfod.AssignedGridPosition == 1;
                var driverToFollow = shouldFollowSafetyCar ? "Safety/Pace car" : cfod.DriverToFollow;

                // TODO: needs to allow announce on start .
                if (cfod.Action == FrozenOrderAction.Follow
                    && pfod.Action != FrozenOrderAction.Follow)
                {
                    Console.WriteLine(string.Format("FO: FOLLOW DIRVER: {0} IN COLUMN {1}", driverToFollow, cfod.AssignedColumn));
                }
                else if (cfod.Action == FrozenOrderAction.AllowToPass
                    && pfod.Action != FrozenOrderAction.AllowToPass)
                {
                    Console.WriteLine(string.Format("FO: ALLOW DIRVER: {0} TO PASS", driverToFollow));
                }
                else if (cfod.Action == FrozenOrderAction.CatchUp
                    && pfod.Action != FrozenOrderAction.CatchUp)
                {
                    Console.WriteLine(string.Format("FO: CATCH UP TO DIRVER: {0}", driverToFollow));
                }

                if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
                {
                    Console.WriteLine("FO: SAFETY CAR JUST LEFT.");
                }
            }
            else if (cfodp == FrozenOrderPhase.FullCourseYellow)
            {
                var shouldFollowSafetyCar = cfod.AssignedPosition == 1;
                var driverToFollow = shouldFollowSafetyCar ? "Safety/Pace car" : cfod.DriverToFollow;
                if (cfod.Action == FrozenOrderAction.Follow
                    && pfod.Action != FrozenOrderAction.Follow)
                {
                    Console.WriteLine(string.Format("FO: FOLLOW DIRVER: {0}", driverToFollow));
                }
                else if (cfod.Action == FrozenOrderAction.AllowToPass
                    && pfod.Action != FrozenOrderAction.AllowToPass)
                {
                    Console.WriteLine(string.Format("FO: ALLOW DIRVER: {0} TO PASS", driverToFollow));
                }
                else if (cfod.Action == FrozenOrderAction.CatchUp
                    && pfod.Action != FrozenOrderAction.CatchUp)
                {
                    Console.WriteLine(string.Format("FO: CATCH UP TO DIRVER: {0}", driverToFollow));
                }

                if (pfod.SafetyCarSpeed != -1.0f && cfod.SafetyCarSpeed == -1.0f)
                {
                    Console.WriteLine("FO: SAFETY CAR JUST LEFT.");
                }
            }
            else if (cfodp == FrozenOrderPhase.FormationStanding)
            {
                if (!this.formationStandingStartAnnounced && cgs.SessionData.SessionRunningTime > 10)
                {
                    this.formationStandingStartAnnounced = true;
                    var isStartingFromPole = cfod.AssignedPosition == 1;
                    if (isStartingFromPole)
                    {
                        Console.WriteLine(string.Format("FO: YOU'RE STARTING FROM THE POLE IN THE {0} COLUMN", cfod.AssignedColumn));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("FO: YOU'RE STARTING FROM POSITION {0} ROW {1} IN THE {2} COLUMN", cfod.AssignedPosition, cfod.AssignedGridPosition, cfod.AssignedColumn));
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
                        Console.WriteLine(string.Format("FO: GET READY, YOU'RE STARTING FROM THE POLE IN THE {0} COLUMN", cfod.AssignedColumn));
                    }
                    else
                    {
                        Console.WriteLine(string.Format("FO: GET READY, YOU'RE STARTING FROM POSITION {0} ROW {1} IN THE {2} COLUMN", cfod.AssignedPosition, cfod.AssignedGridPosition, cfod.AssignedColumn));
                    }
                }
            }

            // For fast rolling, do nothing for now.
        }
    }
}
