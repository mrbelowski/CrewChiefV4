using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.Events;
using System.Threading;
using CrewChiefV4.Audio;

namespace CrewChiefV4
{
    public class SpeechRecogniser : IDisposable
    {
        private SpeechRecognitionEngine sre;

        private String location = UserSettings.GetUserSettings().getString("speech_recognition_location");

        private float minimum_name_voice_recognition_confidence = UserSettings.GetUserSettings().getFloat("minimum_name_voice_recognition_confidence");
        private float minimum_voice_recognition_confidence = UserSettings.GetUserSettings().getFloat("minimum_voice_recognition_confidence");

        private static String defaultLocale = Configuration.getSpeechRecognitionConfigOption("defaultLocale");

        public static String[] HOWS_MY_TYRE_WEAR = Configuration.getSpeechRecognitionPhrases("HOWS_MY_TYRE_WEAR");
        public static String[] HOWS_MY_TRANSMISSION = Configuration.getSpeechRecognitionPhrases("HOWS_MY_TRANSMISSION");
        public static String[] HOWS_MY_AERO = Configuration.getSpeechRecognitionPhrases("HOWS_MY_AERO");
        public static String[] HOWS_MY_ENGINE = Configuration.getSpeechRecognitionPhrases("HOWS_MY_ENGINE");
        public static String[] HOWS_MY_SUSPENSION = Configuration.getSpeechRecognitionPhrases("HOWS_MY_SUSPENSION");
        public static String[] HOWS_MY_BRAKES = Configuration.getSpeechRecognitionPhrases("HOWS_MY_BRAKES");
        public static String[] HOWS_MY_FUEL = Configuration.getSpeechRecognitionPhrases("HOWS_MY_FUEL");
        public static String[] HOWS_MY_PACE = Configuration.getSpeechRecognitionPhrases("HOWS_MY_PACE");
        public static String[] HOW_ARE_MY_TYRE_TEMPS = Configuration.getSpeechRecognitionPhrases("HOW_ARE_MY_TYRE_TEMPS");
        public static String[] WHAT_ARE_MY_TYRE_TEMPS = Configuration.getSpeechRecognitionPhrases("WHAT_ARE_MY_TYRE_TEMPS");
        public static String[] HOW_ARE_MY_BRAKE_TEMPS = Configuration.getSpeechRecognitionPhrases("HOW_ARE_MY_BRAKE_TEMPS");
        public static String[] WHAT_ARE_MY_BRAKE_TEMPS = Configuration.getSpeechRecognitionPhrases("WHAT_ARE_MY_BRAKE_TEMPS");
        public static String[] HOW_ARE_MY_ENGINE_TEMPS = Configuration.getSpeechRecognitionPhrases("HOW_ARE_MY_ENGINE_TEMPS");
        public static String[] WHATS_MY_GAP_IN_FRONT = Configuration.getSpeechRecognitionPhrases("WHATS_MY_GAP_IN_FRONT");
        public static String[] WHATS_MY_GAP_BEHIND = Configuration.getSpeechRecognitionPhrases("WHATS_MY_GAP_BEHIND");
        public static String[] WHAT_WAS_MY_LAST_LAP_TIME = Configuration.getSpeechRecognitionPhrases("WHAT_WAS_MY_LAST_LAP_TIME");
        public static String[] WHATS_MY_BEST_LAP_TIME = Configuration.getSpeechRecognitionPhrases("WHATS_MY_BEST_LAP_TIME");
        public static String[] WHATS_MY_POSITION = Configuration.getSpeechRecognitionPhrases("WHATS_MY_POSITION");
        public static String[] WHATS_MY_FUEL_LEVEL = Configuration.getSpeechRecognitionPhrases("WHATS_MY_FUEL_LEVEL");

        public static String[] KEEP_QUIET = Configuration.getSpeechRecognitionPhrases("KEEP_QUIET");
        public static String[] KEEP_ME_INFORMED = Configuration.getSpeechRecognitionPhrases("KEEP_ME_INFORMED");
        public static String[] TELL_ME_THE_GAPS = Configuration.getSpeechRecognitionPhrases("TELL_ME_THE_GAPS");
        public static String[] DONT_TELL_ME_THE_GAPS = Configuration.getSpeechRecognitionPhrases("DONT_TELL_ME_THE_GAPS");

        public static String[] WHOS_IN_FRONT_IN_THE_RACE = Configuration.getSpeechRecognitionPhrases("WHOS_IN_FRONT_IN_THE_RACE");
        public static String[] WHOS_BEHIND_IN_THE_RACE = Configuration.getSpeechRecognitionPhrases("WHOS_BEHIND_IN_THE_RACE");
        public static String[] WHOS_IN_FRONT_ON_TRACK = Configuration.getSpeechRecognitionPhrases("WHOS_IN_FRONT_ON_TRACK");
        public static String[] WHOS_BEHIND_ON_TRACK = Configuration.getSpeechRecognitionPhrases("WHOS_BEHIND_ON_TRACK");
        public static String[] WHOS_LEADING = Configuration.getSpeechRecognitionPhrases("WHOS_LEADING");

        public static String[] HOW_LONGS_LEFT = Configuration.getSpeechRecognitionPhrases("HOW_LONGS_LEFT");
        public static String[] SPOT = Configuration.getSpeechRecognitionPhrases("SPOT");
        public static String[] DONT_SPOT = Configuration.getSpeechRecognitionPhrases("DONT_SPOT");
        public static String[] REPEAT_LAST_MESSAGE = Configuration.getSpeechRecognitionPhrases("REPEAT_LAST_MESSAGE");
        public static String[] HAVE_I_SERVED_MY_PENALTY = Configuration.getSpeechRecognitionPhrases("HAVE_I_SERVED_MY_PENALTY");
        public static String[] DO_I_HAVE_A_PENALTY = Configuration.getSpeechRecognitionPhrases("DO_I_HAVE_A_PENALTY");
        public static String[] DO_I_STILL_HAVE_A_PENALTY = Configuration.getSpeechRecognitionPhrases("DO_I_STILL_HAVE_A_PENALTY");
        public static String[] DO_I_HAVE_A_MANDATORY_PIT_STOP = Configuration.getSpeechRecognitionPhrases("DO_I_HAVE_A_MANDATORY_PIT_STOP");
        public static String[] WHAT_ARE_MY_SECTOR_TIMES = Configuration.getSpeechRecognitionPhrases("WHAT_ARE_MY_SECTOR_TIMES");
        public static String[] WHATS_MY_LAST_SECTOR_TIME = Configuration.getSpeechRecognitionPhrases("WHATS_MY_LAST_SECTOR_TIME");
        public static String[] WHATS_THE_AIR_TEMP = Configuration.getSpeechRecognitionPhrases("WHATS_THE_AIR_TEMP");
        public static String[] WHATS_THE_TRACK_TEMP = Configuration.getSpeechRecognitionPhrases("WHATS_THE_TRACK_TEMP");
        public static String[] RADIO_CHECK = Configuration.getSpeechRecognitionPhrases("RADIO_CHECK");


        public static String ON = Configuration.getSpeechRecognitionConfigOption("ON");
        public static String POSSESSIVE = Configuration.getSpeechRecognitionConfigOption("POSSESSIVE");
        public static String WHERE_IS = Configuration.getSpeechRecognitionConfigOption("WHERE_IS");
        public static String WHERES = Configuration.getSpeechRecognitionConfigOption("WHERES");
        public static String POSITION_LONG = Configuration.getSpeechRecognitionConfigOption("POSITION_LONG");
        public static String POSITION_SHORT = Configuration.getSpeechRecognitionConfigOption("POSITION_SHORT");

        public static String WHOS_IN = Configuration.getSpeechRecognitionConfigOption("WHOS_IN");
        public static String WHATS = Configuration.getSpeechRecognitionConfigOption("WHATS");
        public static String BEST_LAP = Configuration.getSpeechRecognitionConfigOption("BEST_LAP");
        public static String BEST_LAP_TIME = Configuration.getSpeechRecognitionConfigOption("BEST_LAP_TIME"); 
        public static String LAST_LAP = Configuration.getSpeechRecognitionConfigOption("LAST_LAP");
        public static String LAST_LAP_TIME = Configuration.getSpeechRecognitionConfigOption("LAST_LAP_TIME");
        public static String THE_LEADER = Configuration.getSpeechRecognitionConfigOption("THE_LEADER");
        public static String THE_CAR_AHEAD = Configuration.getSpeechRecognitionConfigOption("THE_CAR_AHEAD");
        public static String THE_CAR_IN_FRONT = Configuration.getSpeechRecognitionConfigOption("THE_CAR_IN_FRONT");
        public static String THE_GUY_AHEAD = Configuration.getSpeechRecognitionConfigOption("THE_GUY_AHEAD");
        public static String THE_GUY_IN_FRONT = Configuration.getSpeechRecognitionConfigOption("THE_GUY_IN_FRONT");
        public static String THE_CAR_BEHIND = Configuration.getSpeechRecognitionConfigOption("THE_CAR_BEHIND");
        public static String THE_GUY_BEHIND = Configuration.getSpeechRecognitionConfigOption("THE_GUY_BEHIND");

        public static String WHAT_TYRES_IS = Configuration.getSpeechRecognitionConfigOption("WHAT_TYRES_IS");
        public static String WHAT_TYRE_IS = Configuration.getSpeechRecognitionConfigOption("WHAT_TYRE_IS");
        
        
        private CrewChief crewChief;

        public Boolean initialised = false;

        public MainWindow.VoiceOptionEnum voiceOptionEnum;

        private List<String> driverNamesInUse = new List<string>();

        private List<Grammar> opponentGrammarList = new List<Grammar>();
        
        private System.Globalization.CultureInfo cultureInfo;

        public static Dictionary<String, int> numberToNumber = getNumberMappings();

        private static Dictionary<String, int> getNumberMappings()
        {
            Dictionary<String, int> dict = new Dictionary<string, int>();
            for (int i = 1; i <= 50; i++)
            {
                dict.Add(Configuration.getSpeechRecognitionConfigOption(i.ToString()), i);
            }
            return dict;
        }

        public void Dispose()
        {
            if (sre != null)
            {
                sre.Dispose();
                sre = null;
            }
            initialised = false;
        }

        public SpeechRecogniser(CrewChief crewChief)
        {
            this.crewChief = crewChief;
            if (minimum_name_voice_recognition_confidence < 0 || minimum_name_voice_recognition_confidence > 1)
            {
                minimum_name_voice_recognition_confidence = 0.4f;
            }
            if (minimum_voice_recognition_confidence < 0 || minimum_voice_recognition_confidence > 1)
            {
                minimum_voice_recognition_confidence = 0.5f;
            }
        }

        private void initWithLocale(String locale)
        {
            cultureInfo = new System.Globalization.CultureInfo(locale);
            this.sre = new SpeechRecognitionEngine(cultureInfo);
        }

        private void validateAndAdd(String[] speechPhrases, Choices choices)
        {
            if (speechPhrases != null && speechPhrases.Count() > 0)
            {
                Boolean valid = true;
                foreach (String s in speechPhrases)
                {
                    if (s == null || s.Trim().Count() == 0)
                    {
                        valid = false;
                        break;
                    }
                }
                if (valid)
                {
                    choices.Add(speechPhrases);
                }
            }
        }

        public void initialiseSpeechEngine()
        {
            initialised = false;
            if (location != null && location.Length > 0)
            {
                try
                {
                    Console.WriteLine("Attempting to initialise speech recognition for user specified location " + location);
                    initWithLocale(location);
                    Console.WriteLine("Success");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to initialise speech engine with voice recognition pack for location " + location +
                        ". Check that SpeechPlatformRuntime.msi and MSSpeech_SR_" + location + "_TELE.msi are installed.");
                    Console.WriteLine("Exception message: " + e.Message);
                    return;
                }
            }
            else
            {
                try
                {
                    Console.WriteLine("Attempting to initialise speech recognition for any English locale");
                    initWithLocale(defaultLocale);
                    Console.WriteLine("Success");
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to initialise speech engine with the OS's default English voice recognition pack (location name " + defaultLocale + "). " +
                        "Check that SpeechPlatformRuntime.msi and at least one of MSSpeech_SR_en-GB_TELE.msi, MSSpeech_SR_en-US_TELE.msi, " + 
                        "MSSpeech_SR_en-AU_TELE.msi, MSSpeech_SR_en-CA_TELE.msi or MSSpeech_SR_en-IN_TELE.msi are installed.");
                    Console.WriteLine("Exception message: " + e.Message);
                    return;
                }
            }
            try
            {
                sre.SetInputToDefaultAudioDevice();
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to set default audio device");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            try
            {
                Choices staticSpeechChoices = new Choices();
                validateAndAdd(HOWS_MY_TYRE_WEAR, staticSpeechChoices);
                validateAndAdd(HOWS_MY_TRANSMISSION, staticSpeechChoices);
                validateAndAdd(HOWS_MY_AERO, staticSpeechChoices);
                validateAndAdd(HOWS_MY_ENGINE, staticSpeechChoices);
                validateAndAdd(HOWS_MY_SUSPENSION, staticSpeechChoices);
                validateAndAdd(HOWS_MY_BRAKES, staticSpeechChoices);
                validateAndAdd(HOWS_MY_FUEL, staticSpeechChoices);
                validateAndAdd(HOWS_MY_PACE, staticSpeechChoices);
                validateAndAdd(HOW_ARE_MY_TYRE_TEMPS, staticSpeechChoices);
                validateAndAdd(WHAT_ARE_MY_TYRE_TEMPS, staticSpeechChoices);
                validateAndAdd(HOW_ARE_MY_BRAKE_TEMPS, staticSpeechChoices);
                validateAndAdd(WHAT_ARE_MY_BRAKE_TEMPS, staticSpeechChoices);
                validateAndAdd(HOW_ARE_MY_ENGINE_TEMPS, staticSpeechChoices);
                validateAndAdd(WHATS_MY_GAP_IN_FRONT, staticSpeechChoices);
                validateAndAdd(WHATS_MY_GAP_BEHIND, staticSpeechChoices);
                validateAndAdd(WHAT_WAS_MY_LAST_LAP_TIME, staticSpeechChoices);
                validateAndAdd(WHATS_MY_BEST_LAP_TIME, staticSpeechChoices);
                validateAndAdd(WHATS_MY_POSITION, staticSpeechChoices);
                validateAndAdd(WHATS_MY_FUEL_LEVEL, staticSpeechChoices);

                validateAndAdd(KEEP_QUIET, staticSpeechChoices);
                validateAndAdd(KEEP_ME_INFORMED, staticSpeechChoices);
                validateAndAdd(TELL_ME_THE_GAPS, staticSpeechChoices);
                validateAndAdd(DONT_TELL_ME_THE_GAPS, staticSpeechChoices);

                validateAndAdd(HOW_LONGS_LEFT, staticSpeechChoices);
                validateAndAdd(SPOT, staticSpeechChoices);
                validateAndAdd(DONT_SPOT, staticSpeechChoices);
                validateAndAdd(REPEAT_LAST_MESSAGE, staticSpeechChoices);
                validateAndAdd(HAVE_I_SERVED_MY_PENALTY, staticSpeechChoices);
                validateAndAdd(DO_I_HAVE_A_PENALTY, staticSpeechChoices);
                validateAndAdd(DO_I_STILL_HAVE_A_PENALTY, staticSpeechChoices);
                validateAndAdd(DO_I_HAVE_A_MANDATORY_PIT_STOP, staticSpeechChoices);
                validateAndAdd(WHAT_ARE_MY_SECTOR_TIMES, staticSpeechChoices);
                validateAndAdd(WHATS_MY_LAST_SECTOR_TIME, staticSpeechChoices);
                validateAndAdd(WHATS_THE_AIR_TEMP, staticSpeechChoices);
                validateAndAdd(WHATS_THE_TRACK_TEMP, staticSpeechChoices);
                validateAndAdd(RADIO_CHECK, staticSpeechChoices);

                GrammarBuilder staticGrammarBuilder = new GrammarBuilder();
                staticGrammarBuilder.Culture = cultureInfo;
                staticGrammarBuilder.Append(staticSpeechChoices);
                Grammar staticGrammar = new Grammar(staticGrammarBuilder);
                sre.LoadGrammar(staticGrammar);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to configure speech engine grammar");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            sre.InitialSilenceTimeout = TimeSpan.Zero;
            try
            {
                sre.SpeechRecognized += new EventHandler<SpeechRecognizedEventArgs>(sre_SpeechRecognized);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to add event handler to speech engine");
                Console.WriteLine("Exception message: " + e.Message);
                return;
            }
            initialised = true;
        }

        public void addNewOpponentName(String rawDriverName)
        {
            try
            {                
                String usableName = DriverNameHelper.getUsableDriverName(rawDriverName, AudioPlayer.soundFilesPath);
                if (usableName != null && usableName.Length > 0)
                {
                    if (driverNamesInUse.Contains(rawDriverName))
                    {
                        return;
                    }
                    if (initialised)
                    {
                        Console.WriteLine("Adding " + usableName + " to speech recogniser");
                        Choices opponentChoices = new Choices();
                        opponentChoices.Add(WHERE_IS + " " + usableName);
                        opponentChoices.Add(WHATS + " " + usableName + POSSESSIVE + " " + LAST_LAP);
                        opponentChoices.Add(WHATS + " " + usableName + POSSESSIVE + " " + BEST_LAP);
                        opponentChoices.Add(WHAT_TYRES_IS + " " + usableName + " " + ON);
                        opponentChoices.Add(WHAT_TYRE_IS + " " + usableName + " " + ON);

                        GrammarBuilder opponentGrammarBuilder = new GrammarBuilder();
                        opponentGrammarBuilder.Culture = cultureInfo;
                        opponentGrammarBuilder.Append(opponentChoices);
                        Grammar newOpponentGrammar = new Grammar(opponentGrammarBuilder);
                        sre.LoadGrammar(newOpponentGrammar);
                        opponentGrammarList.Add(newOpponentGrammar);
                    }
                    driverNamesInUse.Add(rawDriverName);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to add new driver to speech recognition engine - " + e.Message);
            }
        }

        public void addOpponentSpeechRecognition(List<String> names, Boolean useNames)
        {
            driverNamesInUse.Clear();
            Console.WriteLine("adding opponent names to speech recogniser");
            foreach (Grammar opponentGrammar in opponentGrammarList)
            {
                sre.UnloadGrammar(opponentGrammar);
            }
            opponentGrammarList.Clear();
            Choices opponentChoices = new Choices();
            if (useNames)
            {
                foreach (String name in names)
                {
                    opponentChoices.Add(WHERE_IS + " " + name);
                    opponentChoices.Add(WHATS + " " + name + POSSESSIVE + " " + LAST_LAP);
                    opponentChoices.Add(WHATS + " " + name + POSSESSIVE + " " + BEST_LAP);
                    opponentChoices.Add(WHAT_TYRES_IS + " " + name + " " + ON);
                    opponentChoices.Add(WHAT_TYRE_IS + " " + name + " " + ON);
                }
            }
            foreach (KeyValuePair<String, int> entry in numberToNumber)
            {
                opponentChoices.Add(WHATS + " " + POSITION_LONG + " " + entry.Key + POSSESSIVE + " " + LAST_LAP);
                opponentChoices.Add(WHATS + " " + POSITION_LONG + " " + entry.Key + POSSESSIVE + " " + BEST_LAP);
                opponentChoices.Add(WHAT_TYRE_IS + " " + POSITION_LONG + " " + entry.Key + " " + ON);
                opponentChoices.Add(WHAT_TYRES_IS + " " + POSITION_LONG + " " + entry.Key + " " + ON);
                opponentChoices.Add(WHATS + " " + POSITION_SHORT + " " + entry.Key + POSSESSIVE + " " + LAST_LAP);
                opponentChoices.Add(WHATS + " " + POSITION_SHORT + " " + entry.Key + POSSESSIVE + " " + BEST_LAP);
                opponentChoices.Add(WHOS_IN + " " + POSITION_SHORT + " " + entry.Key);
                opponentChoices.Add(WHOS_IN + " " + POSITION_LONG + " " + entry.Key);
                opponentChoices.Add(WHAT_TYRE_IS + " " + POSITION_SHORT + " " + entry.Key + " " + ON);
                opponentChoices.Add(WHAT_TYRES_IS + " " + POSITION_SHORT + " " + entry.Key + " " + ON);
                opponentChoices.Add(WHERE_IS + " " + POSITION_SHORT + " " + entry.Key);
                opponentChoices.Add(WHERE_IS + " " + POSITION_LONG + " " + entry.Key);
            }
            opponentChoices.Add(WHATS + " " + THE_LEADER + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_LEADER + POSSESSIVE + " " + LAST_LAP);

            opponentChoices.Add(WHATS + " " + THE_GUY_IN_FRONT + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_IN_FRONT + POSSESSIVE + " " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_IN_FRONT + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_IN_FRONT + POSSESSIVE + " " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_AHEAD + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_AHEAD + POSSESSIVE + " " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_AHEAD + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_AHEAD + POSSESSIVE + " " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_BEHIND + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_BEHIND + POSSESSIVE + " " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_BEHIND + POSSESSIVE + " " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_BEHIND + POSSESSIVE + " " + LAST_LAP);

            opponentChoices.Add(WHAT_TYRE_IS + " " + THE_GUY_IN_FRONT + " " + ON);
            opponentChoices.Add(WHAT_TYRES_IS + " " + THE_GUY_IN_FRONT + " " + ON);
            opponentChoices.Add(WHAT_TYRE_IS + " " + THE_GUY_AHEAD + " " + ON);
            opponentChoices.Add(WHAT_TYRES_IS + " " + THE_GUY_AHEAD + " " + ON);
            opponentChoices.Add(WHAT_TYRE_IS + " " + THE_GUY_BEHIND + " " + ON);
            opponentChoices.Add(WHAT_TYRES_IS + " " + THE_GUY_BEHIND + " " + ON);

            validateAndAdd(WHOS_IN_FRONT_IN_THE_RACE, opponentChoices);
            validateAndAdd(WHOS_BEHIND_IN_THE_RACE, opponentChoices);
            validateAndAdd(WHOS_IN_FRONT_ON_TRACK, opponentChoices);
            validateAndAdd(WHOS_BEHIND_ON_TRACK, opponentChoices);
            validateAndAdd(WHOS_LEADING, opponentChoices);

            GrammarBuilder opponentGrammarBuilder = new GrammarBuilder();
            opponentGrammarBuilder.Culture = cultureInfo;
            opponentGrammarBuilder.Append(opponentChoices);
            Grammar newOpponentGrammar = new Grammar(opponentGrammarBuilder);
            sre.LoadGrammar(newOpponentGrammar);
            opponentGrammarList.Add(newOpponentGrammar);
            driverNamesInUse.AddRange(names);
        }

        public static Boolean ResultContains(String result, String[] alternatives)
        {
            foreach (String alternative in alternatives)
            {
                if (result.ToLower() == alternative.ToLower())
                {
                    return true;
                }
            }
            // no result with == so try contains
            foreach (String alternative in alternatives)
            {
                if (result.ToLower().Contains(alternative.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }
        
        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            Console.WriteLine("recognised : " + e.Result.Text + " confidence = " + e.Result.Confidence);
            try
            {
                if (opponentGrammarList.Contains(e.Result.Grammar))
                {
                    if (e.Result.Confidence > minimum_name_voice_recognition_confidence)
                    {
                        CrewChief.getEvent("Opponents").respond(e.Result.Text);
                    }
                    else
                    {
                        crewChief.youWot();
                    }
                }
                else if (e.Result.Confidence > minimum_voice_recognition_confidence)
                {
                    if (ResultContains(e.Result.Text, REPEAT_LAST_MESSAGE))
                    {
                        crewChief.audioPlayer.repeatLastMessage();
                    }
                    else
                    {
                        AbstractEvent abstractEvent = getEventForSpeech(e.Result.Text);
                        if (abstractEvent != null)
                        {
                            abstractEvent.respond(e.Result.Text);
                        }
                    }
                }
                else 
                {
                    crewChief.youWot();
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Unable to respond - error message: " + exception.Message);
            }         

            recognizeAsyncStop();
            Thread.Sleep(500);
            if (voiceOptionEnum == MainWindow.VoiceOptionEnum.ALWAYS_ON ||
                voiceOptionEnum == MainWindow.VoiceOptionEnum.TOGGLE)
            {
                Console.WriteLine("restarting speech recognition");
                recognizeAsync();
            }
        }

        public void recognizeAsync()
        {
            sre.RecognizeAsync(RecognizeMode.Multiple);
        }

        public void recognizeAsyncStop()
        {
            sre.RecognizeAsyncStop();
        }

        public void recognizeAsyncCancel()
        {
            sre.RecognizeAsyncCancel();
        }

        private AbstractEvent getEventForSpeech(String recognisedSpeech)
        {
            if (ResultContains(recognisedSpeech, RADIO_CHECK))
            {
                crewChief.respondToRadioCheck();
            }
            else if (ResultContains(recognisedSpeech, DONT_SPOT))
            {
                crewChief.disableSpotter();
            }
            else if (ResultContains(recognisedSpeech, SPOT))
            {
                crewChief.enableSpotter();
            }
            else if (ResultContains(recognisedSpeech, KEEP_QUIET))
            {
                crewChief.enableKeepQuietMode();
            }
            else if (ResultContains(recognisedSpeech, DONT_TELL_ME_THE_GAPS))
            {
                crewChief.disableDeltasMode();
            }
            else if (ResultContains(recognisedSpeech, TELL_ME_THE_GAPS))
            {
                crewChief.enableDeltasMode();
            }
            else if (ResultContains(recognisedSpeech, HOWS_MY_AERO) ||
               ResultContains(recognisedSpeech, HOWS_MY_TRANSMISSION) ||
               ResultContains(recognisedSpeech, HOWS_MY_ENGINE) ||
               ResultContains(recognisedSpeech, HOWS_MY_SUSPENSION) ||
               ResultContains(recognisedSpeech, HOWS_MY_BRAKES))
            {
                return CrewChief.getEvent("DamageReporting");
            }
            else if (ResultContains(recognisedSpeech, KEEP_ME_INFORMED))
            {
                crewChief.disableKeepQuietMode();
            }
            else if (ResultContains(recognisedSpeech, WHATS_MY_FUEL_LEVEL) || ResultContains(recognisedSpeech, HOWS_MY_FUEL))
            {
                return CrewChief.getEvent("Fuel");
            }
            else if (ResultContains(recognisedSpeech, WHATS_MY_GAP_IN_FRONT) ||
                ResultContains(recognisedSpeech, WHATS_MY_GAP_BEHIND))
            {
                return CrewChief.getEvent("Timings");
            }
            else if (ResultContains(recognisedSpeech, WHATS_MY_POSITION))
            {
                return CrewChief.getEvent("Position");
            }
            else if (ResultContains(recognisedSpeech, WHAT_WAS_MY_LAST_LAP_TIME) ||
                ResultContains(recognisedSpeech, WHATS_MY_BEST_LAP_TIME) ||
                ResultContains(recognisedSpeech, HOWS_MY_PACE) ||
                ResultContains(recognisedSpeech, WHAT_ARE_MY_SECTOR_TIMES) ||
                ResultContains(recognisedSpeech, WHATS_MY_LAST_SECTOR_TIME))
            {
                return CrewChief.getEvent("LapTimes");
            }
            else if (ResultContains(recognisedSpeech, WHAT_ARE_MY_TYRE_TEMPS) ||
                ResultContains(recognisedSpeech, HOW_ARE_MY_TYRE_TEMPS) ||
                ResultContains(recognisedSpeech, HOWS_MY_TYRE_WEAR) ||
                ResultContains(recognisedSpeech, HOW_ARE_MY_BRAKE_TEMPS) ||
                ResultContains(recognisedSpeech, WHAT_ARE_MY_BRAKE_TEMPS))
            {
                return CrewChief.getEvent("TyreMonitor");
            }
            else if (ResultContains(recognisedSpeech, HOW_LONGS_LEFT))
            {
                return CrewChief.getEvent("RaceTime");
            }
            else if (ResultContains(recognisedSpeech, DO_I_STILL_HAVE_A_PENALTY) ||
                ResultContains(recognisedSpeech, DO_I_HAVE_A_PENALTY) ||
                ResultContains(recognisedSpeech, HAVE_I_SERVED_MY_PENALTY))
            {
                return CrewChief.getEvent("Penalties");
            }
            else if (ResultContains(recognisedSpeech, DO_I_HAVE_A_MANDATORY_PIT_STOP))
            {
                return CrewChief.getEvent("MandatoryPitStops");
            }
            else if (ResultContains(recognisedSpeech, HOW_ARE_MY_ENGINE_TEMPS))
            {
                return CrewChief.getEvent("EngineMonitor");
            }
            else if (ResultContains(recognisedSpeech, WHATS_THE_AIR_TEMP) ||
               ResultContains(recognisedSpeech, WHATS_THE_TRACK_TEMP))
            {
                return CrewChief.getEvent("ConditionsMonitor");
            }
            return null;
        }
    }
}
