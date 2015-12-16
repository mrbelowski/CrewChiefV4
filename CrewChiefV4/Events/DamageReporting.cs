using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;

namespace CrewChiefV4.Events
{
    class DamageReporting : AbstractEvent
    {
        private Boolean enableDamageMessages = UserSettings.GetUserSettings().getBoolean("enable_damage_messages");
        private Boolean enableBrakeDamageMessages = UserSettings.GetUserSettings().getBoolean("enable_brake_damage_messages");
        private Boolean enableSuspensionDamageMessages = UserSettings.GetUserSettings().getBoolean("enable_suspension_damage_messages");

        private String folderMinorTransmissionDamage = "damage_reporting/minor_transmission_damage";
        private String folderMinorEngineDamage = "damage_reporting/minor_engine_damage";
        private String folderMinorAeroDamage = "damage_reporting/minor_aero_damage";
        private String folderMinorSuspensionDamage = "damage_reporting/minor_suspension_damage";
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

        private String folderMissingWheel = "damage_reporting/missing_wheel";

        private DamageLevel engineDamage;
        private DamageLevel trannyDamage;
        private DamageLevel aeroDamage;
        private DamageLevel maxSuspensionDamage;
        private DamageLevel maxBrakeDamage;
        
        private Boolean isMissingWheel = false;

        private TimeSpan timeToWaitForDamageToSettle = TimeSpan.FromSeconds(3);

        private DateTime timeWhenDamageLastChanged = DateTime.MinValue;

        private Tuple<Component, DamageLevel> damageToReportNext = null;

        private Dictionary<Component, DamageLevel> reportedDamagesLevels = new Dictionary<Component, DamageLevel>();

        private DamageLevel minDamageToReport = DamageLevel.TRIVIAL;

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

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (currentGameState.CarDamageData.DamageEnabled)
            {
                aeroDamage = currentGameState.CarDamageData.OverallAeroDamage;
                trannyDamage = currentGameState.CarDamageData.OverallTransmissionDamage;
                engineDamage = currentGameState.CarDamageData.OverallEngineDamage;
                if (enableBrakeDamageMessages)
                {
                    if (currentGameState.CarDamageData.BrakeDamageStatus.hasValueAtLevel(DamageLevel.DESTROYED))
                    {
                        maxBrakeDamage = DamageLevel.DESTROYED;
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
                }

                if (enableSuspensionDamageMessages)
                {
                    if (currentGameState.CarDamageData.SuspensionDamageStatus.hasValueAtLevel(DamageLevel.DESTROYED))
                    {
                        maxSuspensionDamage = DamageLevel.DESTROYED;
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
                }

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
                        if (reportedDamagesLevels.ContainsKey(damageToReportNext.Item1))
                        {
                            reportedDamagesLevels[damageToReportNext.Item1] = damageToReportNext.Item2;
                        }
                        else
                        {
                            reportedDamagesLevels.Add(damageToReportNext.Item1, damageToReportNext.Item2);
                        }
                        if (enableDamageMessages)
                        {
                            playDamageToReport();
                        }
                    }
                }
            }
        }

        public override void respond(String voiceMessage)
        {
            if (voiceMessage.Contains(SpeechRecogniser.AERO) || voiceMessage.Contains(SpeechRecogniser.BODY_WORK))
            {
                if (aeroDamage == DamageLevel.NONE)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoAeroDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (aeroDamage == DamageLevel.MAJOR || aeroDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereAeroDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (aeroDamage == DamageLevel.MINOR)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorAeroDamage, 0, null), false);
                }
                else if (aeroDamage == DamageLevel.TRIVIAL)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderJustAScratch, 0, null), false);
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.TRANSMISSION))
            {
                if (trannyDamage == DamageLevel.NONE || trannyDamage == DamageLevel.TRIVIAL)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoTransmissionDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderBustedTransmission, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereTransmissionDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (trannyDamage == DamageLevel.MINOR)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorTransmissionDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.ENGINE))
            {
                if (engineDamage == DamageLevel.NONE || engineDamage == DamageLevel.TRIVIAL)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderNoEngineDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.DESTROYED)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderBustedEngine, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.MAJOR)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderSevereEngineDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else if (engineDamage == DamageLevel.MINOR)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(folderMinorEngineDamage, 0, null), false);
                    audioPlayer.closeChannel();
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.SUSPENSION))
            {
                if (!enableSuspensionDamageMessages)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    if (isMissingWheel)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderMissingWheel, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    if ((maxSuspensionDamage == DamageLevel.NONE || maxSuspensionDamage == DamageLevel.TRIVIAL) && !isMissingWheel)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderNoSuspensionDamage, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else if (maxSuspensionDamage == DamageLevel.DESTROYED)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderBustedSuspension, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else if (maxSuspensionDamage == DamageLevel.MAJOR)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderSevereSuspensionDamage, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else if (maxSuspensionDamage == DamageLevel.MINOR && !isMissingWheel)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderMinorSuspensionDamage, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                }
            }
            if (voiceMessage.Contains(SpeechRecogniser.BRAKES))
            {
                if (!enableBrakeDamageMessages)
                {
                    audioPlayer.playClipImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null), false);
                    audioPlayer.closeChannel();
                }
                else
                {
                    if (maxBrakeDamage == DamageLevel.NONE || maxBrakeDamage == DamageLevel.TRIVIAL)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderNoBrakeDamage, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else if (maxBrakeDamage == DamageLevel.DESTROYED)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderBustedBrakes, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else if (maxBrakeDamage == DamageLevel.MAJOR)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderSevereBrakeDamage, 0, null), false);
                        audioPlayer.closeChannel();
                    }
                    else if (maxBrakeDamage == DamageLevel.MINOR)
                    {
                        audioPlayer.playClipImmediately(new QueuedMessage(folderMinorBrakeDamage, 0, null), false);
                        audioPlayer.closeChannel();
                    }
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
            if (enableBrakeDamageMessages && maxBrakeDamage > getLastReportedDamageLevel(Component.BRAKES))
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
            Boolean playMissingWheel = isMissingWheel;
            if (playMissingWheel || damageToReportNext.Item2 > DamageLevel.MINOR)
            {
                // missing wheel or major damage, so don't play any cut track warnings that might be queued
                audioPlayer.removeQueuedClip(Penalties.folderCutTrackInRace);
                audioPlayer.removeQueuedClip(Penalties.folderCutTrackPracticeOrQual);
                audioPlayer.removeQueuedClip(Penalties.folderLapDeleted);
            }
            if (damageToReportNext.Item1 == Component.ENGINE)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedEngine, 0, this));
                    audioPlayer.disablePearlsOfWisdom = true;
                    playMissingWheel = false;
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereEngineDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorEngineDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.TRANNY)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedTransmission, 0, this));
                    audioPlayer.disablePearlsOfWisdom = true;
                    playMissingWheel = false;
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereTransmissionDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorTransmissionDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.SUSPENSION)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedSuspension, 0, this));
                    audioPlayer.disablePearlsOfWisdom = true;
                    playMissingWheel = false;
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereSuspensionDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR && !isMissingWheel)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorSuspensionDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.BRAKES)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderBustedBrakes, 0, this));
                    audioPlayer.disablePearlsOfWisdom = true;
                    playMissingWheel = false;
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereBrakeDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorBrakeDamage, 0, this));
                }
            }
            else if (damageToReportNext.Item1 == Component.AERO)
            {
                if (damageToReportNext.Item2 == DamageLevel.DESTROYED)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereAeroDamage, 0, this));
                    audioPlayer.disablePearlsOfWisdom = true;
                }
                else if (damageToReportNext.Item2 == DamageLevel.MAJOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderSevereAeroDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.MINOR)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderMinorAeroDamage, 0, this));
                }
                else if (damageToReportNext.Item2 == DamageLevel.TRIVIAL)
                {
                    audioPlayer.queueClip(new QueuedMessage(folderJustAScratch, 0, this));
                }
            }
            if (playMissingWheel)
            {
                audioPlayer.queueClip(new QueuedMessage(folderMissingWheel, 0, this));
            }
        }
    }
}
