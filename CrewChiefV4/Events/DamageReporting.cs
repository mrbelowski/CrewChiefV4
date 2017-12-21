using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{
    class DamageReporting : AbstractEvent
    {
        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Checkered, SessionPhase.FullCourseYellow, SessionPhase.Formation }; }
        }

        private Boolean delayResponses = UserSettings.GetUserSettings().getBoolean("enable_delayed_responses");

        private Boolean enableDamageMessages = UserSettings.GetUserSettings().getBoolean("enable_damage_messages");
        private Boolean enableBrakeDamageMessages = UserSettings.GetUserSettings().getBoolean("enable_brake_damage_messages");
        private Boolean enableSuspensionDamageMessages = UserSettings.GetUserSettings().getBoolean("enable_suspension_damage_messages");

        private String folderMinorTransmissionDamage = "damage_reporting/minor_transmission_damage";
        private String folderMinorEngineDamage = "damage_reporting/minor_engine_damage";
        private String folderMinorAeroDamage = "damage_reporting/minor_aero_damage";
        // same as above but filtered to remove sounds that only work when used in a voice command response
        private String folderMinorAeroDamageGeneral = "damage_reporting/minor_aero_damage_general";
        private String folderMinorSuspensionDamage = "damage_reporting/minor_suspension_damage";
        // same as above but filtered to remove sounds that only work when used in a voice command response
        private String folderMinorSuspensionDamageGeneral = "damage_reporting/minor_suspension_damage_general";
        private String folderMinorBrakeDamage = "damage_reporting/minor_brake_damage";

        private String folderSevereTransmissionDamage = "damage_reporting/severe_transmission_damage";
        private String folderSevereEngineDamage = "damage_reporting/severe_engine_damage";
        private String folderSevereAeroDamage = "damage_reporting/severe_aero_damage";
        private String folderSevereBrakeDamage = "damage_reporting/severe_brake_damage";
        private String folderSevereSuspensionDamage = "damage_reporting/severe_suspension_damage";

        private String folderBustedTransmission = "damage_reporting/busted_transmission";
        private String folderBustedEngine = "damage_reporting/busted_engine";
        private String folderBustedSuspension = "damage_reporting/busted_suspension";
        private String folderBustedBrakes = "damage_reporting/busted_brakes";

        private String folderNoTransmissionDamage = "damage_reporting/no_transmission_damage";
        private String folderNoEngineDamage = "damage_reporting/no_engine_damage";
        private String folderNoAeroDamage = "damage_reporting/no_aero_damage"; 
        private String folderNoSuspensionDamage = "damage_reporting/no_suspension_damage"; 
        private String folderNoBrakeDamage = "damage_reporting/no_brake_damage";
        private String folderJustAScratch = "damage_reporting/trivial_aero_damage";
        // same as above but filtered to remove sounds that only work when used in a voice command response
        private String folderJustAScratchGeneral = "damage_reporting/trivial_aero_damage_general";

        private String folderMissingWheel = "damage_reporting/missing_wheel";

        private String folderLeftFrontPuncture = "damage_reporting/left_front_puncture";
        private String folderRightFrontPuncture = "damage_reporting/right_front_puncture";
        private String folderLeftRearPuncture = "damage_reporting/left_rear_puncture";
        private String folderRightRearPuncture = "damage_reporting/right_rear_puncture";

        // "the car's in good shape" / "we have no significant damage" etc
        private String folderNoDamageOnAnyComponent = "damage_reporting/no_damage";

        private DamageLevel engineDamage;
        private DamageLevel trannyDamage;
        private DamageLevel aeroDamage;
        private DamageLevel maxSuspensionDamage;
        private DamageLevel maxBrakeDamage;
        
        private Boolean isMissingWheel = false;

        private TimeSpan timeToWaitForDamageToSettle = TimeSpan.FromSeconds(3);

        private DateTime timeWhenDamageLastChanged = DateTime.MinValue;

        private DateTime nextPunctureCheck = DateTime.MinValue;

        private CornerData.Corners lastReportedPunctureCorner = CornerData.Corners.NONE;

        private Tuple<Component, DamageLevel> damageToReportNext = null;

        private Dictionary<Component, DamageLevel> reportedDamagesLevels = new Dictionary<Component, DamageLevel>();

        private DamageLevel minDamageToReport = DamageLevel.TRIVIAL;

        private static float punctureThreshold = 30f; // about 5psi

        private Component componentDestroyed = Component.NONE;

        private enum Component
        {
            ENGINE, TRANNY, AERO, SUSPENSION, BRAKES, NONE
        }
        
        public DamageReporting(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }

        public override void clearState()
        {
            engineDamage = DamageLevel.NONE;
            trannyDamage = DamageLevel.NONE;
            aeroDamage = DamageLevel.NONE;
            maxSuspensionDamage = DamageLevel.NONE;
            maxBrakeDamage = DamageLevel.NONE;
            timeWhenDamageLastChanged = DateTime.MinValue;
            isMissingWheel = false;
            damageToReportNext = null;
            reportedDamagesLevels.Clear();
            minDamageToReport = DamageLevel.TRIVIAL;
            nextPunctureCheck = DateTime.Now + timeToWaitForDamageToSettle;
            componentDestroyed = Component.NONE;
        }

        private Boolean hasBeenReported(Component component, DamageLevel damageLevel)
        {
            foreach (KeyValuePair<Component, DamageLevel> componentAndDamageAlreadyReported in reportedDamagesLevels)
            {
                if (component == componentAndDamageAlreadyReported.Key && componentAndDamageAlreadyReported.Value == damageLevel)
                {
                    return true;
                }
            }
            return false;
        }

        // used when damage level decreases
        private void resetReportedDamage(Component component, DamageLevel newDamageLevel)
        {
            if (reportedDamagesLevels.ContainsKey(component))
            {
                reportedDamagesLevels[component] = newDamageLevel;
            }
        }

        private DamageLevel getLastReportedDamageLevel(Component component)
        {
            if (reportedDamagesLevels.ContainsKey(component))
            {
                return reportedDamagesLevels[component];
            }
            else
            {
                return DamageLevel.NONE;
            }
        }

        public static CornerData.Corners getPuncture(TyreData tyreData)
        {
            // quick sanity check on the data - if all the tyres are the same pressure we have no puncture
            if (tyreData.FrontLeftPressure == tyreData.FrontRightPressure &&
                    tyreData.FrontLeftPressure == tyreData.RearLeftPressure &&
                    tyreData.FrontLeftPressure == tyreData.RearRightPressure)
            {
                return CornerData.Corners.NONE;
            }
            else if (tyreData.FrontLeftPressure < punctureThreshold)
            {
                return CornerData.Corners.FRONT_LEFT;
            }
            else if (tyreData.FrontRightPressure < punctureThreshold)
            {
                return CornerData.Corners.FRONT_RIGHT;
            }
            else if (tyreData.RearLeftPressure < punctureThreshold)
            {
                return CornerData.Corners.REAR_LEFT;
            }
            else if (tyreData.RearRightPressure < punctureThreshold)
            {
                return CornerData.Corners.REAR_RIGHT;
            }
            return CornerData.Corners.NONE;
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.CarDamageData.DamageEnabled && currentGameState.SessionData.SessionRunningTime > 10 && currentGameState.Now > nextPunctureCheck)
            {
                nextPunctureCheck = currentGameState.Now + timeToWaitForDamageToSettle;
                CornerData.Corners puncture = getPuncture(currentGameState.TyreData);
                if (puncture != lastReportedPunctureCorner)
                {
                    lastReportedPunctureCorner = puncture;
                    switch (puncture)
                    {
                        case CornerData.Corners.FRONT_LEFT:
                            audioPlayer.playMessage(new QueuedMessage(folderLeftFrontPuncture, 0, this));
                            break;
                        case CornerData.Corners.FRONT_RIGHT:
                            audioPlayer.playMessage(new QueuedMessage(folderRightFrontPuncture, 0, this));
                            break;
                        case CornerData.Corners.REAR_LEFT:
                            audioPlayer.playMessage(new QueuedMessage(folderLeftRearPuncture, 0, this));
                            break;
                        case CornerData.Corners.REAR_RIGHT:
                            audioPlayer.playMessage(new QueuedMessage(folderRightRearPuncture, 0, this));
                            break;
                    }
                }
            }
            if (currentGameState.CarDamageData.DamageEnabled)
            {
                aeroDamage = currentGameState.CarDamageData.OverallAeroDamage;
                trannyDamage = currentGameState.CarDamageData.OverallTransmissionDamage;
                engineDamage = currentGameState.CarDamageData.OverallEngineDamage;
                if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.DESTROYED))
                {
                    maxBrakeDamage = DamageLevel.DESTROYED;
                    componentDestroyed = Component.BRAKES;
                }
                else if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.MAJOR))
                {
                    maxBrakeDamage = DamageLevel.MAJOR;
                }
                else if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.MINOR))
                {
                    maxBrakeDamage = DamageLevel.MINOR;
                }
                else if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.TRIVIAL))
                {
                    maxBrakeDamage = DamageLevel.TRIVIAL;
                }
            
                if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.DESTROYED))
                {
                    maxSuspensionDamage = DamageLevel.DESTROYED;
                    componentDestroyed = Component.SUSPENSION;
                }
                else if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.MAJOR))
                {
                    maxSuspensionDamage = DamageLevel.MAJOR;
                }
                else if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.MINOR))
                {
                    maxSuspensionDamage = DamageLevel.MINOR;
                }
                else if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.TRIVIAL))
                {
                    maxSuspensionDamage = DamageLevel.TRIVIAL;
                }
                isMissingWheel = !currentGameState.PitData.InPitlane && (!currentGameState.TyreData.LeftFrontAttached || !currentGameState.TyreData.RightFrontAttached ||
                        !currentGameState.TyreData.LeftRearAttached || !currentGameState.TyreData.RightRearAttached);
            
                if (engineDamage < getLastReportedDamageLevel(Component.ENGINE))
                {
                    resetReportedDamage(Component.ENGINE, engineDamage);
                } 
                if (trannyDamage < getLastReportedDamageLevel(Component.TRANNY))
                {
                    resetReportedDamage(Component.TRANNY, trannyDamage);
                } 
                if (maxSuspensionDamage < getLastReportedDamageLevel(Component.SUSPENSION))
                {
                    resetReportedDamage(Component.SUSPENSION, maxSuspensionDamage);
                } 
                if (maxBrakeDamage < getLastReportedDamageLevel(Component.BRAKES))
                {
                    resetReportedDamage(Component.BRAKES, maxBrakeDamage);
                } 
                if (aeroDamage < getLastReportedDamageLevel(Component.AERO))
                {
                    resetReportedDamage(Component.AERO, aeroDamage);
                }

                minDamageToReport = (DamageLevel)Math.Max((int)engineDamage, Math.Max((int)trannyDamage, Math.Max((int)maxSuspensionDamage, Math.Max((int)maxBrakeDamage, (int) aeroDamage))));

                Tuple<Component, DamageLevel> worstUnreportedDamage = getWorstUnreportedDamage();
                if (worstUnreportedDamage != null && worstUnreportedDamage.Item2 >= minDamageToReport)
                {
                    if (damageToReportNext == null || worstUnreportedDamage.Item1 != damageToReportNext.Item1 || worstUnreportedDamage.Item2 != damageToReportNext.Item2)
                    {
                        timeWhenDamageLastChanged = currentGameState.Now;
                        damageToReportNext = worstUnreportedDamage;
                    }
                    else if (timeWhenDamageLastChanged.Add(timeToWaitForDamageToSettle) < currentGameState.Now)
                    {
                        Console.WriteLine("reporting ...");
                        Console.WriteLine(damageToReportNext.Item1 + ", " + damageToReportNext.Item2);

                        // put *all* the damage levels in the 'reported' set, even though we haven't actually reported them.
                        // This ensure we only ever play the worst damage on the car when damage has just increased
                        // Only do this if the component damage is *less* than the one we just reported
                        if (Component.AERO == damageToReportNext.Item1 || aeroDamage < damageToReportNext.Item2)
                        {
                            addReportedDamage(Component.AERO, aeroDamage);
                        }
                        if (Component.BRAKES == damageToReportNext.Item1 || maxBrakeDamage < damageToReportNext.Item2)
                        {
                            addReportedDamage(Component.BRAKES, maxBrakeDamage);
                        }
                        if (Component.ENGINE == damageToReportNext.Item1 || engineDamage < damageToReportNext.Item2)
                        {
                            addReportedDamage(Component.ENGINE, engineDamage);
                        }
                        if (Component.SUSPENSION == damageToReportNext.Item1 || maxSuspensionDamage < damageToReportNext.Item2)
                        {
                            addReportedDamage(Component.SUSPENSION, maxSuspensionDamage);
                        }
                        if (Component.TRANNY == damageToReportNext.Item1 || trannyDamage < damageToReportNext.Item2)
                        {
                            addReportedDamage(Component.TRANNY, trannyDamage);
                        }
                        if (enableDamageMessages)
                        {
                            playDamageToReport();
                        }
                    }
                }
            }
        }

        private void addReportedDamage(Component component, DamageLevel damageLevel)
        {
            if (reportedDamagesLevels.ContainsKey(component))
            {
                reportedDamagesLevels[component] = damageLevel;
            }
            else
            {
                reportedDamagesLevels.Add(component, damageLevel);
            }
        }

        private QueuedMessage getDamageMessage(Component component, Boolean includeNoDamage)
        {
            QueuedMessage damageMessage = null;
            switch (component)
            {
                case Component.AERO:
                    if (aeroDamage == DamageLevel.NONE)
                    {
                        if (includeNoDamage)
                        {
                            damageMessage = new QueuedMessage(folderNoAeroDamage, 0, null);
                        }
                    }
                    else if (aeroDamage == DamageLevel.MAJOR || aeroDamage == DamageLevel.DESTROYED)
                    {
                        damageMessage = new QueuedMessage(folderSevereAeroDamage, 0, null);
                    }
                    else if (aeroDamage == DamageLevel.MINOR)
                    {
                        damageMessage = new QueuedMessage(includeNoDamage ? folderMinorAeroDamage : folderMinorAeroDamageGeneral, 0, null);
                    }
                    else if (aeroDamage == DamageLevel.TRIVIAL)
                    {
                        damageMessage = new QueuedMessage(includeNoDamage ? folderJustAScratch : folderJustAScratchGeneral, 0, null);
                    }
                    break;
                case Component.BRAKES:
                    if (maxBrakeDamage == DamageLevel.NONE || maxBrakeDamage == DamageLevel.TRIVIAL)
                    {
                        if (includeNoDamage)
                        {
                            damageMessage = new QueuedMessage(folderNoBrakeDamage, 0, null);
                        }
                    }
                    else if (maxBrakeDamage == DamageLevel.DESTROYED)
                    {
                        damageMessage = new QueuedMessage(folderBustedBrakes, 0, null);
                    }
                    else if (maxBrakeDamage == DamageLevel.MAJOR)
                    {
                        damageMessage = new QueuedMessage(folderSevereBrakeDamage, 0, null);
                    }
                    else if (maxBrakeDamage == DamageLevel.MINOR)
                    {
                        damageMessage = new QueuedMessage(folderMinorBrakeDamage, 0, null);
                    }
                    break;
                case Component.ENGINE:
                    if (engineDamage == DamageLevel.NONE || engineDamage == DamageLevel.TRIVIAL)
                    {
                        if (includeNoDamage)
                        {
                            damageMessage = new QueuedMessage(folderNoEngineDamage, 0, null);
                        }
                    }
                    else if (engineDamage == DamageLevel.DESTROYED)
                    {
                        damageMessage = new QueuedMessage(folderBustedEngine, 0, null);
                    }
                    else if (engineDamage == DamageLevel.MAJOR)
                    {
                        damageMessage = new QueuedMessage(folderSevereEngineDamage, 0, null);
                    }
                    else if (engineDamage == DamageLevel.MINOR)
                    {
                        damageMessage = new QueuedMessage(folderMinorEngineDamage, 0, null);
                    }
                    break;
                case Component.SUSPENSION:
                    if (isMissingWheel)
                    {
                        damageMessage = new QueuedMessage(folderMissingWheel, 0, null);                        
                    }
                    if ((maxSuspensionDamage == DamageLevel.NONE || maxSuspensionDamage == DamageLevel.TRIVIAL) && !isMissingWheel)
                    {
                        if (includeNoDamage)
                        {
                            damageMessage = new QueuedMessage(folderNoSuspensionDamage, 0, null);
                        }
                    }
                    else if (maxSuspensionDamage == DamageLevel.DESTROYED)
                    {
                        damageMessage = new QueuedMessage(folderBustedSuspension, 0, null);                        
                    }
                    else if (maxSuspensionDamage == DamageLevel.MAJOR)
                    {
                        damageMessage = new QueuedMessage(folderSevereSuspensionDamage, 0, null);                        
                    }
                    else if (maxSuspensionDamage == DamageLevel.MINOR && !isMissingWheel)
                    {
                        damageMessage = new QueuedMessage(includeNoDamage ? folderMinorSuspensionDamage : folderMinorSuspensionDamageGeneral, 0, null);                        
                    }
                    break;
                case Component.TRANNY:
                    if (trannyDamage == DamageLevel.NONE || trannyDamage == DamageLevel.TRIVIAL)
                    {
                        if (includeNoDamage)
                        {
                            damageMessage = new QueuedMessage(folderNoTransmissionDamage, 0, null);
                        }
                    }
                    else if (trannyDamage == DamageLevel.DESTROYED)
                    {
                        damageMessage = new QueuedMessage(folderBustedTransmission, 0, null);
                    }
                    else if (trannyDamage == DamageLevel.MAJOR)
                    {
                        damageMessage = new QueuedMessage(folderSevereTransmissionDamage, 0, null);
                    }
                    else if (trannyDamage == DamageLevel.MINOR)
                    {
                        damageMessage = new QueuedMessage(folderMinorTransmissionDamage, 0, null);
                    }
                    break;
                default:
                    break;
            }
            return damageMessage;
        }

        private void readStatus()
        {
            List<QueuedMessage> damageMessages = new List<QueuedMessage>();
            switch (lastReportedPunctureCorner)
            {
                case CornerData.Corners.FRONT_LEFT:
                    damageMessages.Add(new QueuedMessage(folderLeftFrontPuncture, 0, this));
                    break;
                case CornerData.Corners.FRONT_RIGHT:
                    damageMessages.Add(new QueuedMessage(folderRightFrontPuncture, 0, this));
                    break;
                case CornerData.Corners.REAR_LEFT:
                    damageMessages.Add(new QueuedMessage(folderLeftRearPuncture, 0, this));
                    break;
                case CornerData.Corners.REAR_RIGHT:
                    damageMessages.Add(new QueuedMessage(folderRightRearPuncture, 0, this));
                    break;
            }
            QueuedMessage aero = getDamageMessage(Component.AERO, false);
            if (aero != null)
            {
                damageMessages.Add(aero);
            }
            QueuedMessage tranny = getDamageMessage(Component.TRANNY, false);
            if (tranny != null)
            {
                damageMessages.Add(tranny);
            }
            QueuedMessage engine = getDamageMessage(Component.ENGINE, false);
            if (engine != null)
            {
                damageMessages.Add(engine);
            }
            QueuedMessage sus = getDamageMessage(Component.SUSPENSION, false);
            if (sus != null)
            {
                damageMessages.Add(sus);
            }
            QueuedMessage brakes = getDamageMessage(Component.BRAKES, false);
            if (brakes != null)
            {
                damageMessages.Add(brakes);
            }           
            if (damageMessages.Count == 0)
            {
                // no damage
                damageMessages.Add(new QueuedMessage(folderNoDamageOnAnyComponent, 0, this));
            }
            foreach (QueuedMessage message in damageMessages)
            {
                audioPlayer.playMessageImmediately(message);
            }
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CAR_STATUS) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.DAMAGE_REPORT) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.STATUS))
            {
                readStatus();
            }
            else
            {
                QueuedMessage damageMessage = null;
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_AERO))
                {
                    damageMessage = getDamageMessage(Component.AERO, true);
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_TRANSMISSION))
                {
                    damageMessage = getDamageMessage(Component.TRANNY, true);
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_ENGINE))
                {
                    damageMessage = getDamageMessage(Component.ENGINE, true);
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_SUSPENSION))
                {
                    damageMessage = getDamageMessage(Component.SUSPENSION, true);
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOWS_MY_BRAKES))
                {
                    damageMessage = getDamageMessage(Component.BRAKES, true);
                }
                if (damageMessage != null)
                {
                    // play this immediately or play "stand by", and queue it to be played in a few seconds
                    if (delayResponses && Utilities.random.Next(10) >= 2 && SoundCache.availableSounds.Contains(AudioPlayer.folderStandBy))
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderStandBy, 0, null));
                        int secondsDelay = Math.Max(5, Utilities.random.Next(11));
                        audioPlayer.pauseQueue(secondsDelay);
                        damageMessage.dueTime = (DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond) + (1000 * secondsDelay);
                        audioPlayer.playDelayedImmediateMessage(damageMessage);
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(damageMessage);
                    }
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
            }
        }

        private Tuple<Component, DamageLevel> getWorstUnreportedDamage()
        {
            List<Tuple<Component, DamageLevel>> componentsWithMoreDamage = new List<Tuple<Component, DamageLevel>>();
            if (engineDamage > getLastReportedDamageLevel(Component.ENGINE))
            {
                componentsWithMoreDamage.Add(new Tuple<Component, DamageLevel> (Component.ENGINE, engineDamage));
            }
            if (trannyDamage > getLastReportedDamageLevel(Component.TRANNY))
            {
                componentsWithMoreDamage.Add(new Tuple<Component, DamageLevel>(Component.TRANNY, trannyDamage));
            }
            if (enableSuspensionDamageMessages && maxSuspensionDamage > getLastReportedDamageLevel(Component.SUSPENSION))
            {
                componentsWithMoreDamage.Add(new Tuple<Component, DamageLevel>(Component.SUSPENSION, maxSuspensionDamage));
            }
            if (enableBrakeDamageMessages && GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.BRAKE_DAMAGE) &&
                maxBrakeDamage > getLastReportedDamageLevel(Component.BRAKES))
            {
                componentsWithMoreDamage.Add(new Tuple<Component, DamageLevel>(Component.BRAKES, maxBrakeDamage));
            }
            if (aeroDamage > getLastReportedDamageLevel(Component.AERO))
            {
                componentsWithMoreDamage.Add(new Tuple<Component, DamageLevel>(Component.AERO, aeroDamage));
            }
            if (componentsWithMoreDamage.Count == 0)
            {
                return null;
            }
            else if (componentsWithMoreDamage.Count == 1)
            {
                return componentsWithMoreDamage[0];
            }
            else
            {
                Tuple<Component, DamageLevel> worstUnreported = componentsWithMoreDamage[0];
                for (int i = 1; i < componentsWithMoreDamage.Count; i++)
                {
                    if (componentsWithMoreDamage[i].Item2 > worstUnreported.Item2)
                    {
                        worstUnreported = componentsWithMoreDamage[i];
                    }
                }
                return worstUnreported;
            }
        }

        private void playDamageToReport()
        {
            if (isMissingWheel || damageToReportNext.Item2 > DamageLevel.MINOR)
            {
                // missing wheel or major damage, so don't play other messages that might be queued - note this won't interrupt an
                // already playing message
                audioPlayer.purgeQueues();
                // if the damage is race-ending switch off pearls-of-wisdom for the remainder of the session
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.disablePearlsOfWisdom = true;
                }
            }
            if (componentDestroyed != Component.NONE  // If there is any component already destroyed
                && damageToReportNext.Item1 != componentDestroyed)  // And it is not the current component
            {
                // Do not play any message, because it does not matter if Aero is minor after suspension is damaged.
                Console.WriteLine(string.Format("Not reporting damage {0} for {1} because {2} is already destroyed", damageToReportNext.Item2, damageToReportNext.Item1, componentDestroyed));
                return;
            }
            if (damageToReportNext.Item1 == Component.ENGINE)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderBustedEngine, 0, this));
                    audioPlayer.playRant("damage_rant", null);
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderSevereEngineDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderMinorEngineDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.TRANNY)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderBustedTransmission, 0, this));
                    audioPlayer.playRant("damage_rant", null);
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderSevereTransmissionDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderMinorTransmissionDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.SUSPENSION)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderBustedSuspension, 0, this));
                    audioPlayer.playRant("damage_rant", null);
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR || isMissingWheel)
                {
                    if (isMissingWheel)
                    {
                        audioPlayer.playMessage(new QueuedMessage(folderMissingWheel, 0, this));
                    }
                    audioPlayer.playMessage(new QueuedMessage(folderSevereSuspensionDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR && !isMissingWheel)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderMinorSuspensionDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.BRAKES)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderBustedBrakes, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderSevereBrakeDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderMinorBrakeDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.AERO)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderSevereAeroDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderSevereAeroDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderMinorAeroDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.TRIVIAL)
                {
                    audioPlayer.playMessage(new QueuedMessage(folderJustAScratch, 0, this));
                }
            }
        }
    }
}
