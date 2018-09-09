using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using CrewChiefV4.iRacing;
using iRSDKSharp;

namespace CrewChiefV4.Events
{
    enum PressureUnit
    {
        PSI, KPA
    }

    class IRacingBroadcastMessageEvent : AbstractEvent
    {
        private static float kpaPerPsi = 6.89476f;
        private static float litresPerGallon = 3.78541f;
        private PressureUnit pressureUnit = UserSettings.GetUserSettings().getBoolean("iracing_pit_tyre_pressure_in_psi") ?
            PressureUnit.PSI : PressureUnit.KPA;

        private Boolean autoFuelToEnd = UserSettings.GetUserSettings().getBoolean("iracing_enable_auto_fuel_to_end_of_race");

        public static String folderYouHave = "incidents/you_have";

        public static String folderincidents = "incidents/incidents";
        public static String folderincidentlimit = "incidents/the_incident_limit_is";
        public static String folderUnlimited = "incidents/no_incident_limit";

        public static String folderincidentPoints = "incidents/incident_points";
        public static String folderincidentPointslimit = "incidents/the_incident_points_limit_is";
        public static String folderUnlimitedPoints = "incidents/no_incident_points_limit";

        public static String folderLicenseA = "licence/a_licence";
        public static String folderLicenseB = "licence/b_licence";
        public static String folderLicenseC = "licence/c_licence";
        public static String folderLicenseD = "licence/d_licence";
        public static String folderLicenseR = "licence/r_licence";
        public static String folderLicensePro = "licence/pro_licence";

        private int lastColdFLPressure = -1;
        private int lastColdFRPressure = -1;
        private int lastColdRLPressure = -1;
        private int lastColdRRPressure = -1;

        private int maxIncidentCount = -1;
        private int incidentsCount = -1;
        private int iRating = -1;
        private int strenghtOfField = -1; 
        private Boolean hasLimitedIncidents = false;
        private float fuelCapacity = -1;
        private float currentFuel = -1;
        private Tuple<String, float> licenseLevel = new Tuple<string, float>("invalid", -1);

        public IRacingBroadcastMessageEvent(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            this.lastColdFLPressure = -1;
            this.lastColdFRPressure = -1;
            this.lastColdRLPressure = -1;
            this.lastColdRRPressure = -1;
            this.incidentsCount = -1;
            this.maxIncidentCount = -1;
            this.iRating = -1;
            this.hasLimitedIncidents = false;
            this.licenseLevel = new Tuple<string, float>("invalid", -1);
            this.fuelCapacity = -1;
            this.currentFuel = -1;
        }

        public override void clearState()
        {
            this.lastColdFLPressure = -1;
            this.lastColdFRPressure = -1;
            this.lastColdRLPressure = -1;
            this.lastColdRRPressure = -1;
            this.incidentsCount = -1;
            this.maxIncidentCount = -1;
            this.iRating = -1;
            this.hasLimitedIncidents = false;
            this.licenseLevel = new Tuple<string, float>("invalid", -1);
            this.fuelCapacity = -1;
            this.currentFuel = -1;
        }

        public override List<SessionPhase> applicableSessionPhases
        {
            get { return new List<SessionPhase> { SessionPhase.Green, SessionPhase.Countdown, SessionPhase.FullCourseYellow }; }
        }

        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (CrewChief.gameDefinition.gameEnum != GameEnum.IRACING)
            {
                return;
            }
            lastColdFLPressure = (int)currentGameState.TyreData.FrontLeftPressure;
            lastColdFRPressure = (int)currentGameState.TyreData.FrontRightPressure;
            lastColdRLPressure = (int)currentGameState.TyreData.RearLeftPressure;
            lastColdRRPressure = (int)currentGameState.TyreData.RearRightPressure;

            maxIncidentCount = currentGameState.SessionData.MaxIncidentCount;
            incidentsCount = currentGameState.SessionData.CurrentIncidentCount;
            hasLimitedIncidents = currentGameState.SessionData.HasLimitedIncidents;
            licenseLevel = currentGameState.SessionData.LicenseLevel;
            iRating = currentGameState.SessionData.iRating;
            strenghtOfField = currentGameState.SessionData.StrengthOfField;
            fuelCapacity = currentGameState.FuelData.FuelCapacity;
            currentFuel = currentGameState.FuelData.FuelLeft;
            if(autoFuelToEnd)
            {
                if(previousGameState != null && !previousGameState.PitData.InPitlane && currentGameState.PitData.InPitlane
                    && currentGameState.SessionData.SessionType == SessionType.Race && currentGameState.SessionData.SessionRunningTime > 15 
                    && !previousGameState.PitData.IsInGarage && !currentGameState.PitData.JumpedToPits)
                {
                    Fuel fuelEvent = (Fuel)CrewChief.getEvent("Fuel");
                    float litresNeeded = fuelEvent.getLitresToEndOfRace(true);

                    if (litresNeeded == float.MaxValue)
                    {
                        audioPlayer.playMessage(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    }
                    else if (litresNeeded <= 0)
                    {
                        audioPlayer.playMessage(new QueuedMessage(Fuel.folderPlentyOfFuel, 0, null));
                    }
                    else if (litresNeeded > 0)
                    {
                        int roundedLitresNeeded = (int)Math.Ceiling(litresNeeded);
                        AddFuel(roundedLitresNeeded);
                        Console.WriteLine("Auto refuel to the end of the race, adding " + roundedLitresNeeded + " liters of fuel");
                        if (roundedLitresNeeded > fuelCapacity - currentFuel)
                        {
                            // if we have a known fuel capacity and this is less than the calculated amount of fuel we need, warn about it.
                            audioPlayer.playMessage(new QueuedMessage(Fuel.folderWillNeedToStopAgain, 4, this));
                        }
                        else
                        {
                            audioPlayer.playMessage(new QueuedMessage(AudioPlayer.folderFuelToEnd, 0, null));
                        }
                    }
                }
            }
/*
            if (hasLimitedIncidents)
            {
                //play < 5 incident left warning.
                if (incidentsCount >= maxIncidentCount - 5 && !playedIncidentsWarning)
                {
                    playedIncidentsWarning = true;
                    audioPlayer.playMessageImmediately(new QueuedMessage("Incidents/limit", MessageContents(folderYouHave, incidentsCount, folderincidentPoints,
                        Pause(200), folderincidentPointslimit, maxIncidentCount), 0, null));
                    
                }
                else if (incidentsCount >= maxIncidentCount - 1 && !playedLastIncidentsLeftWarning)
                {
                    playedLastIncidentsLeftWarning = true;
                    //play 1 incident left warning.
                }
            }
 */
        }

        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_ADD))
            {
                int amount = 0;
                foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.numberToNumber)
                {
                    foreach (String numberStr in entry.Key)
                    {
                        if (voiceMessage.Contains(" " + numberStr + " "))
                        {
                            amount = entry.Value;
                            break;
                        }
                    }
                }
                if (amount == 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDidntUnderstand, 0, null));
                    return;
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.LITERS))
                {
                    AddFuel(amount);
                    audioPlayer.playMessageImmediately(new QueuedMessage("iracing_add_fuel",
                        MessageContents(AudioPlayer.folderAcknowlegeOK, amount, amount == 1 ? Fuel.folderLitre : Fuel.folderLitres), 0, null));
                }
                else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.GALLONS))
                {
                    AddFuel(convertGallonsToLitres(amount));
                    audioPlayer.playMessageImmediately(new QueuedMessage("iracing_add_fuel",
                        MessageContents(AudioPlayer.folderAcknowlegeOK, amount, amount == 1 ? Fuel.folderGallon : Fuel.folderGallons), 0, null));
                }
                else
                {
                    Console.WriteLine("Got fuel request with no unit, assuming " + (Fuel.fuelReportsInGallon ? " gallons" : "litres"));
                    if (!Fuel.fuelReportsInGallon)
                    {
                        AddFuel(amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage("iracing_add_fuel",
                            MessageContents(AudioPlayer.folderAcknowlegeOK, amount, amount == 1 ? Fuel.folderLitre : Fuel.folderLitres), 0, null));
                    }
                    else
                    {
                        AddFuel(convertGallonsToLitres(amount));
                        audioPlayer.playMessageImmediately(new QueuedMessage("iracing_add_fuel",
                            MessageContents(AudioPlayer.folderAcknowlegeOK, amount, amount == 1 ? Fuel.folderGallon : Fuel.folderGallons), 0, null));
                    }
                }
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_FUEL_TO_THE_END))
            {
                Fuel fuelEvent = (Fuel)CrewChief.getEvent("Fuel");
                float litresNeeded = fuelEvent.getLitresToEndOfRace(true);

                if (litresNeeded == float.MaxValue)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                }
                else if (litresNeeded <= 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(Fuel.folderPlentyOfFuel, 0, null));
                }
                else if(litresNeeded > 0)
                {
                    int roundedLitresNeeded = (int)Math.Ceiling(litresNeeded);
                    AddFuel(roundedLitresNeeded);
                    if (roundedLitresNeeded > fuelCapacity - currentFuel) 
                    {
                        // if we have a known fuel capacity and this is less than the calculated amount of fuel we need, warn about it.
                        audioPlayer.playMessage(new QueuedMessage(Fuel.folderWillNeedToStopAgain, 4, this));
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderFuelToEnd, 0, null));
                    }                    
                    return;
                }

            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_TEAROFF))
            {
                Tearoff();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_FAST_REPAIR))
            {
                FastRepair();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_ALL))
            {
                ClearAll();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_TYRES))
            {
                ClearTires();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_WIND_SCREEN))
            {
                ClearTearoff();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_FAST_REPAIR))
            {
                ClearFastRepair();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_FUEL))
            {
                ClearFuel();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_TYRE_PRESSURE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE))
            {
                int amount = 0;
                foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.numberToNumber)
                {
                    foreach (String numberStr in entry.Key)
                    {
                        if (voiceMessage.Contains(" " + numberStr))
                        {
                            amount = entry.Value;
                            break;
                        }
                    }
                }
                if (amount == 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDidntUnderstand, 0, null));
                    return;
                }
                else
                {
                    if (pressureUnit == PressureUnit.PSI)
                    {
                        amount = convertPSItoKPA(amount);
                    }
                    if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_TYRE_PRESSURE))
                    {
                        ChangeTire(PitCommandModeTypes.LF, amount);
                        ChangeTire(PitCommandModeTypes.RF, amount);
                        ChangeTire(PitCommandModeTypes.LR, amount);
                        ChangeTire(PitCommandModeTypes.RR, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE))
                    {
                        ChangeTire(PitCommandModeTypes.LF, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE))
                    {
                        ChangeTire(PitCommandModeTypes.RF, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE))
                    {
                        ChangeTire(PitCommandModeTypes.LR, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE))
                    {
                        ChangeTire(PitCommandModeTypes.RR, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_ALL_TYRES))
            {
                ChangeTire(PitCommandModeTypes.LF, lastColdFLPressure);
                ChangeTire(PitCommandModeTypes.RF, lastColdFRPressure);
                ChangeTire(PitCommandModeTypes.LR, lastColdRLPressure);
                ChangeTire(PitCommandModeTypes.RR, lastColdRRPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_LEFT_TYRE))
            {
                ChangeTire(PitCommandModeTypes.LF, lastColdFLPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_RIGHT_TYRE))
            {
                ChangeTire(PitCommandModeTypes.RF, lastColdFRPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_LEFT_TYRE))
            {
                ChangeTire(PitCommandModeTypes.LR, lastColdRLPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_RIGHT_TYRE))
            {
                ChangeTire(PitCommandModeTypes.RR, lastColdRRPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_LEFT_SIDE_TYRES))
            {
                ClearTires();
                ChangeTire(PitCommandModeTypes.LF, lastColdFLPressure);
                ChangeTire(PitCommandModeTypes.LR, lastColdRLPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_RIGHT_SIDE_TYRES))
            {
                ClearTires();
                ChangeTire(PitCommandModeTypes.RF, lastColdFRPressure);
                ChangeTire(PitCommandModeTypes.RR, lastColdRRPressure);
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }

            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.HOW_MANY_INCIDENT_POINTS))
            {
                audioPlayer.playMessageImmediately(new QueuedMessage("Incidents/incidents", MessageContents(folderYouHave, incidentsCount, folderincidents), 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_THE_INCIDENT_LIMIT))
            {
                if (hasLimitedIncidents)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("Incidents/limit", MessageContents(folderincidentlimit, maxIncidentCount), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("Incidents/limit", MessageContents(folderUnlimited), 0, null));
                }
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_IRATING))
            {
                if(iRating != -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("license/irating", MessageContents(iRating), 0, null));
                    return;
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    return;
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_MY_LICENSE_CLASS))
            {
                if (licenseLevel.Item2 == -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    return;
                }
                if (licenseLevel.Item2 != -1)
                {
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
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                        return;
                    }
                    messageFragments.AddRange(MessageContents(wholeandfractional.Item1, NumberReader.folderPoint, wholeandfractional.Item2));
                    QueuedMessage licenceLevelMessage = new QueuedMessage("License/license", messageFragments, 0, null);
                    audioPlayer.playDelayedImmediateMessage(licenceLevelMessage);
                }
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.WHATS_THE_SOF))
            {
                if (strenghtOfField != -1)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("license/irating", MessageContents(strenghtOfField), 0, null));
                    return;
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderNoData, 0, null));
                    return;
                }
            }
        }



        /// <summary>
        /// Schedule to add the specified amount of fuel (in liters) in the next pitstop.
        /// </summary>
        /// <param name="amount">The amount of fuel (in liters) to add. Use 0 to leave at current value.</param>
        public void AddFuel(int amount)
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.Fuel, amount, 0);
        }

        private void ChangeTire(PitCommandModeTypes type, int pressure)
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)type, pressure);
        }

        /// <summary>
        /// Schedule to use a windshield tear-off in the next pitstop.
        /// </summary>
        public void Tearoff()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.WS, 0);
        }

        /// <summary>
        /// Schedule to use a fast repair in the next pitstop.
        /// </summary>
        public void FastRepair()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.FastRepair, 0);
        }

        /// <summary>
        /// Clear all pit commands.
        /// </summary>
        public static void ClearAll()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.Clear, 0);
        }

        /// <summary>
        /// Clear all tire changes.
        /// </summary>
        public void ClearTires()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearTires, 0);
        }

        /// <summary>
        /// Clear tearoff.
        /// </summary>
        public void ClearTearoff()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearWS, 0);
        }

        /// <summary>
        /// Clear fast repair.
        /// </summary>
        public void ClearFastRepair()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearFR, 0);
        }

        /// <summary>
        /// Clear clear fuel.
        /// </summary>
        public void ClearFuel()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearFuel, 0);
        }

        private int convertPSItoKPA(int psi)
        {
            return (int)Math.Round(psi * kpaPerPsi);
        }

        private int convertGallonsToLitres(int gallons)
        {
            return (int)Math.Ceiling(gallons * litresPerGallon);
        }
    }
}
