using Microsoft.Speech.Recognition;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.Events;
using System.Threading;
using CrewChiefV4.Audio;
using CrewChiefV4.commands;
using System.Windows.Forms;
using System.Diagnostics;

namespace CrewChiefV4
{
    public class SpeechRecogniser : IDisposable
    {
        private SpeechRecognitionEngine sre;

        // used in nAudio mode:
        public static Dictionary<string, Tuple<string, int>> speechRecognitionDevices = new Dictionary<string, Tuple<string, int>>();
        public static int initialSpeechInputDeviceIndex = 0;
        private Boolean useNAudio = UserSettings.GetUserSettings().getBoolean("use_naudio_for_speech_recognition");
        private RingBufferStream.RingBufferStream buffer;
        private NAudio.Wave.WaveInEvent waveIn;
        private bool keepRecording = true;
        //

        private String location = UserSettings.GetUserSettings().getString("speech_recognition_location");

        private float minimum_name_voice_recognition_confidence = UserSettings.GetUserSettings().getFloat("minimum_name_voice_recognition_confidence");
        private float minimum_voice_recognition_confidence = UserSettings.GetUserSettings().getFloat("minimum_voice_recognition_confidence");
        private Boolean disable_alternative_voice_commands = UserSettings.GetUserSettings().getBoolean("disable_alternative_voice_commands");
        private Boolean enable_iracing_pit_stop_commands = UserSettings.GetUserSettings().getBoolean("enable_iracing_pit_stop_commands");
        private static Boolean use_verbose_responses = UserSettings.GetUserSettings().getBoolean("use_verbose_responses");

        private static String defaultLocale = Configuration.getSpeechRecognitionConfigOption("defaultLocale");

        public static String[] HOWS_MY_TYRE_WEAR = Configuration.getSpeechRecognitionPhrases("HOWS_MY_TYRE_WEAR");
        public static String[] HOWS_MY_TRANSMISSION = Configuration.getSpeechRecognitionPhrases("HOWS_MY_TRANSMISSION");
        public static String[] HOWS_MY_AERO = Configuration.getSpeechRecognitionPhrases("HOWS_MY_AERO");
        public static String[] HOWS_MY_ENGINE = Configuration.getSpeechRecognitionPhrases("HOWS_MY_ENGINE");
        public static String[] HOWS_MY_SUSPENSION = Configuration.getSpeechRecognitionPhrases("HOWS_MY_SUSPENSION");
        public static String[] HOWS_MY_BRAKES = Configuration.getSpeechRecognitionPhrases("HOWS_MY_BRAKES");
        public static String[] HOWS_MY_FUEL = Configuration.getSpeechRecognitionPhrases("HOWS_MY_FUEL");
        public static String[] HOWS_MY_BATTERY = Configuration.getSpeechRecognitionPhrases("HOWS_MY_BATTERY");
        public static String[] HOWS_MY_PACE = Configuration.getSpeechRecognitionPhrases("HOWS_MY_PACE");
        public static String[] HOWS_MY_SELF_PACE = Configuration.getSpeechRecognitionPhrases("HOWS_MY_SELF_PACE");
        public static String[] HOW_ARE_MY_TYRE_TEMPS = Configuration.getSpeechRecognitionPhrases("HOW_ARE_MY_TYRE_TEMPS");
        public static String[] WHAT_ARE_MY_TYRE_TEMPS = Configuration.getSpeechRecognitionPhrases("WHAT_ARE_MY_TYRE_TEMPS");
        public static String[] HOW_ARE_MY_BRAKE_TEMPS = Configuration.getSpeechRecognitionPhrases("HOW_ARE_MY_BRAKE_TEMPS");
        public static String[] WHAT_ARE_MY_BRAKE_TEMPS = Configuration.getSpeechRecognitionPhrases("WHAT_ARE_MY_BRAKE_TEMPS");
        public static String[] HOW_ARE_MY_ENGINE_TEMPS = Configuration.getSpeechRecognitionPhrases("HOW_ARE_MY_ENGINE_TEMPS");
        public static String[] WHATS_MY_GAP_IN_FRONT = Configuration.getSpeechRecognitionPhrases("WHATS_MY_GAP_IN_FRONT");
        public static String[] WHATS_MY_GAP_BEHIND = Configuration.getSpeechRecognitionPhrases("WHATS_MY_GAP_BEHIND");
        public static String[] WHAT_WAS_MY_LAST_LAP_TIME = Configuration.getSpeechRecognitionPhrases("WHAT_WAS_MY_LAST_LAP_TIME");
        public static String[] WHATS_MY_BEST_LAP_TIME = Configuration.getSpeechRecognitionPhrases("WHATS_MY_BEST_LAP_TIME");
        public static String[] WHATS_THE_FASTEST_LAP_TIME = Configuration.getSpeechRecognitionPhrases("WHATS_THE_FASTEST_LAP_TIME");
        public static String[] WHATS_MY_POSITION = Configuration.getSpeechRecognitionPhrases("WHATS_MY_POSITION");
        public static String[] WHATS_MY_FUEL_LEVEL = Configuration.getSpeechRecognitionPhrases("WHATS_MY_FUEL_LEVEL");
        public static String[] WHATS_MY_FUEL_USAGE = Configuration.getSpeechRecognitionPhrases("WHATS_MY_FUEL_USAGE");
        public static String[] WHATS_MY_IRATING = Configuration.getSpeechRecognitionPhrases("WHATS_MY_IRATING");
        public static String[] WHATS_MY_LICENSE_CLASS = Configuration.getSpeechRecognitionPhrases("WHATS_MY_LICENSE_CLASS");
        public static String[] WHAT_TYRES_AM_I_ON = Configuration.getSpeechRecognitionPhrases("WHAT_TYRES_AM_I_ON");
        public static String[] WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES = Configuration.getSpeechRecognitionPhrases("WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES");
        public static String[] HOW_LONG_WILL_THESE_TYRES_LAST = Configuration.getSpeechRecognitionPhrases("HOW_LONG_WILL_THESE_TYRES_LAST");

        public static String[] HOW_MUCH_FUEL_TO_END_OF_RACE = Configuration.getSpeechRecognitionPhrases("HOW_MUCH_FUEL_TO_END_OF_RACE");
        public static String[] CALCULATE_FUEL_FOR = Configuration.getSpeechRecognitionPhrases("CALCULATE_FUEL_FOR");
        public static String[] LAP = Configuration.getSpeechRecognitionPhrases("LAP");
        public static String[] LAPS = Configuration.getSpeechRecognitionPhrases("LAPS");
        public static String[] MINUTE = Configuration.getSpeechRecognitionPhrases("MINUTE");
        public static String[] MINUTES = Configuration.getSpeechRecognitionPhrases("MINUTES");
        public static String[] HOUR = Configuration.getSpeechRecognitionPhrases("HOUR");
        public static String[] HOURS = Configuration.getSpeechRecognitionPhrases("HOURS");

        public static String[] KEEP_QUIET = Configuration.getSpeechRecognitionPhrases("KEEP_QUIET");
        public static String[] KEEP_ME_INFORMED = Configuration.getSpeechRecognitionPhrases("KEEP_ME_INFORMED");
        public static String[] TELL_ME_THE_GAPS = Configuration.getSpeechRecognitionPhrases("TELL_ME_THE_GAPS");
        public static String[] DONT_TELL_ME_THE_GAPS = Configuration.getSpeechRecognitionPhrases("DONT_TELL_ME_THE_GAPS");
        public static String[] TALK_TO_ME_ANYWHERE = Configuration.getSpeechRecognitionPhrases("TALK_TO_ME_ANYWHERE");
        public static String[] DONT_TALK_IN_THE_CORNERS = Configuration.getSpeechRecognitionPhrases("DONT_TALK_IN_THE_CORNERS");
        public static String[] WHATS_THE_TIME = Configuration.getSpeechRecognitionPhrases("WHATS_THE_TIME");
        public static String[] ENABLE_YELLOW_FLAG_MESSAGES = Configuration.getSpeechRecognitionPhrases("ENABLE_YELLOW_FLAG_MESSAGES");
        public static String[] DISABLE_YELLOW_FLAG_MESSAGES = Configuration.getSpeechRecognitionPhrases("DISABLE_YELLOW_FLAG_MESSAGES");
        public static String[] ENABLE_MANUAL_FORMATION_LAP = Configuration.getSpeechRecognitionPhrases("ENABLE_MANUAL_FORMATION_LAP");
        public static String[] DISABLE_MANUAL_FORMATION_LAP = Configuration.getSpeechRecognitionPhrases("DISABLE_MANUAL_FORMATION_LAP");

        public static String[] WHOS_IN_FRONT_IN_THE_RACE = Configuration.getSpeechRecognitionPhrases("WHOS_IN_FRONT_IN_THE_RACE");
        public static String[] WHOS_BEHIND_IN_THE_RACE = Configuration.getSpeechRecognitionPhrases("WHOS_BEHIND_IN_THE_RACE");
        public static String[] WHOS_IN_FRONT_ON_TRACK = Configuration.getSpeechRecognitionPhrases("WHOS_IN_FRONT_ON_TRACK");
        public static String[] WHOS_BEHIND_ON_TRACK = Configuration.getSpeechRecognitionPhrases("WHOS_BEHIND_ON_TRACK");
        public static String[] WHOS_LEADING = Configuration.getSpeechRecognitionPhrases("WHOS_LEADING");

        public static String[] WHERE_AM_I_FASTER = Configuration.getSpeechRecognitionPhrases("WHERE_AM_I_FASTER");
        public static String[] WHERE_AM_I_SLOWER = Configuration.getSpeechRecognitionPhrases("WHERE_AM_I_SLOWER");

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

        public static String[] IS_MY_PIT_BOX_OCCUPIED = Configuration.getSpeechRecognitionPhrases("IS_MY_PIT_BOX_OCCUPIED");
        public static String[] PLAY_POST_PIT_POSITION_ESTIMATE = Configuration.getSpeechRecognitionPhrases("PLAY_POST_PIT_POSITION_ESTIMATE");
        public static String[] PRACTICE_PIT_STOP = Configuration.getSpeechRecognitionPhrases("PRACTICE_PIT_STOP");


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

        public static String IRATING = Configuration.getSpeechRecognitionConfigOption("IRATING");
        public static String LICENSE_CLASS = Configuration.getSpeechRecognitionConfigOption("LICENSE_CLASS");

        public static String[] PLAY_CORNER_NAMES = Configuration.getSpeechRecognitionPhrases("PLAY_CORNER_NAMES");

        public static String[] DAMAGE_REPORT = Configuration.getSpeechRecognitionPhrases("DAMAGE_REPORT");
        public static String[] CAR_STATUS = Configuration.getSpeechRecognitionPhrases("CAR_STATUS");
        public static String[] SESSION_STATUS = Configuration.getSpeechRecognitionPhrases("SESSION_STATUS");
        public static String[] STATUS = Configuration.getSpeechRecognitionPhrases("STATUS");

        public static String[] START_PACE_NOTES_PLAYBACK = Configuration.getSpeechRecognitionPhrases("START_PACE_NOTES_PLAYBACK");
        public static String[] STOP_PACE_NOTES_PLAYBACK = Configuration.getSpeechRecognitionPhrases("STOP_PACE_NOTES_PLAYBACK");

        // pitstop commands specific to iRacing:
        public static String[] PIT_STOP = Configuration.getSpeechRecognitionPhrases("PIT_STOP");
        public static String[] PIT_STOP_ADD = Configuration.getSpeechRecognitionPhrases("PIT_STOP_ADD");
        public static String[] LITERS = Configuration.getSpeechRecognitionPhrases("LITERS");
        public static String[] GALLONS = Configuration.getSpeechRecognitionPhrases("GALLONS");
        public static String[] PIT_STOP_TEAROFF = Configuration.getSpeechRecognitionPhrases("PIT_STOP_TEAROFF");
        public static String[] PIT_STOP_FAST_REPAIR = Configuration.getSpeechRecognitionPhrases("PIT_STOP_FAST_REPAIR");
        public static String[] PIT_STOP_CLEAR_ALL = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CLEAR_ALL");
        public static String[] PIT_STOP_CLEAR_TYRES = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CLEAR_TYRES");
        public static String[] PIT_STOP_CLEAR_WIND_SCREEN = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CLEAR_WIND_SCREEN");
        public static String[] PIT_STOP_CLEAR_FAST_REPAIR = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CLEAR_FAST_REPAIR");
        public static String[] PIT_STOP_CLEAR_FUEL = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CLEAR_FUEL");

        public static String[] PIT_STOP_CHANGE_ALL_TYRES = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_ALL_TYRES");
        public static String[] PIT_STOP_CHANGE_FRONT_LEFT_TYRE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_FRONT_LEFT_TYRE");
        public static String[] PIT_STOP_CHANGE_FRONT_RIGHT_TYRE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_FRONT_RIGHT_TYRE");
        public static String[] PIT_STOP_CHANGE_REAR_LEFT_TYRE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_REAR_LEFT_TYRE");
        public static String[] PIT_STOP_CHANGE_REAR_RIGHT_TYRE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_REAR_RIGHT_TYRE");

        public static String[] PIT_STOP_CHANGE_TYRE_PRESSURE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_TYRE_PRESSURE");
        public static String[] PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE");
        public static String[] PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE");
        public static String[] PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE");
        public static String[] PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE");

        public static String[] PIT_STOP_CHANGE_LEFT_SIDE_TYRES = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_LEFT_SIDE_TYRES");
        public static String[] PIT_STOP_CHANGE_RIGHT_SIDE_TYRES = Configuration.getSpeechRecognitionPhrases("PIT_STOP_CHANGE_RIGHT_SIDE_TYRES");

        public static String[] HOW_MANY_INCIDENT_POINTS = Configuration.getSpeechRecognitionPhrases("HOW_MANY_INCIDENT_POINTS");
        public static String[] WHATS_THE_INCIDENT_LIMIT = Configuration.getSpeechRecognitionPhrases("WHATS_THE_INCIDENT_LIMIT");
        public static String[] WHATS_THE_SOF = Configuration.getSpeechRecognitionPhrases("WHATS_THE_SOF");
        
        public static String[] PIT_STOP_FUEL_TO_THE_END = Configuration.getSpeechRecognitionPhrases("PIT_STOP_FUEL_TO_THE_END");

        public static String[] MORE_INFO = Configuration.getSpeechRecognitionPhrases("MORE_INFO");

        public static String[] I_AM_OK = Configuration.getSpeechRecognitionPhrases("I_AM_OK");

        public static String[] IS_CAR_AHEAD_MY_CLASS = Configuration.getSpeechRecognitionPhrases("IS_CAR_AHEAD_MY_CLASS");
        public static String[] IS_CAR_BEHIND_MY_CLASS = Configuration.getSpeechRecognitionPhrases("IS_CAR_BEHIND_MY_CLASS");
        public static String[] WHAT_CLASS_IS_CAR_AHEAD = Configuration.getSpeechRecognitionPhrases("WHAT_CLASS_IS_CAR_AHEAD");
        public static String[] WHAT_CLASS_IS_CAR_BEHIND = Configuration.getSpeechRecognitionPhrases("WHAT_CLASS_IS_CAR_BEHIND");

        private String lastRecognisedText = null;

        private CrewChief crewChief;

        public Boolean initialised = false;

        //this is needed if the user for some odd reason have decided to uninstall SpeechPlatformRuntime.msi after having activated speech recognition, in this case the app trys to init twise.
        public Boolean failedSpeechEngineOnce = false;

        public MainWindow.VoiceOptionEnum voiceOptionEnum;

        private List<String> driverNamesInUse = new List<string>();

        private List<Grammar> opponentGrammarList = new List<Grammar>();
        private List<Grammar> iracingPitstopGrammarList = new List<Grammar>();

        private Grammar macroGrammar = null;

        private Dictionary<String, ExecutableCommandMacro> macroLookup = new Dictionary<string, ExecutableCommandMacro>();

        private System.Globalization.CultureInfo cultureInfo;

        public static Dictionary<String[], int> numberToNumber = getNumberMappings(1, 199);

        public static Dictionary<String[], int> racePositionNumberToNumber = getNumberMappings(1, 64);

        private Choices digitsChoices;

        public static Boolean waitingForSpeech = false;

        public static Boolean gotRecognitionResult = false;

        // guard against race condition between closing channel and sre_SpeechRecognised event completing
        public static Boolean keepRecognisingInHoldMode = false;

        static SpeechRecogniser () 
        {
            if (UserSettings.GetUserSettings().getBoolean("use_naudio_for_speech_recognition"))
            {
                speechRecognitionDevices.Clear();
                for (int deviceId = 0; deviceId < NAudio.Wave.WaveIn.DeviceCount; deviceId++)
                {
                    // the audio device stuff makes no guarantee as to the presence of sensible device and product guids,
                    // so we have to do the best we can here
                    NAudio.Wave.WaveInCapabilities capabilities = NAudio.Wave.WaveIn.GetCapabilities(deviceId);
                    Boolean hasNameGuid = capabilities.NameGuid != null && !capabilities.NameGuid.Equals(Guid.Empty);
                    Boolean hasProductGuid = capabilities.ProductGuid != null && !capabilities.ProductGuid.Equals(Guid.Empty);
                    String rawName = capabilities.ProductName;
                    String name = rawName;
                    int nameAddition = 0;
                    while (speechRecognitionDevices.Keys.Contains(name))
                    {
                        nameAddition++;
                        name = rawName += "(" + nameAddition + ")";
                    }
                    String guidToUse;
                    if (hasNameGuid)
                    {
                        guidToUse = capabilities.NameGuid.ToString();
                    }
                    else if (hasProductGuid)
                    {
                        guidToUse = capabilities.ProductGuid.ToString() + "_" + name;
                    }
                    else
                    {
                        guidToUse = name;
                    }
                    speechRecognitionDevices.Add(name, new Tuple<string, int>(guidToUse, deviceId));
                }
            }
        }

        // load voice commands for triggering keyboard macros. The String key of the input Dictionary is the
        // command list key in speech_recognition_config.txt. When one of these phrases is heard the map value
        // CommandMacro is executed.
        public void loadMacroVoiceTriggers(Dictionary<string, ExecutableCommandMacro> voiceTriggeredMacros)
        {
            if (!initialised)
            {
                return;
            }
            macroLookup.Clear();
            if (macroGrammar != null && macroGrammar.Loaded)
            {
                sre.UnloadGrammar(macroGrammar);
            }
            if (voiceTriggeredMacros.Count == 0)
            {
                Console.WriteLine("No macro voice triggers defined for the current game.");
                return;
            }
            Choices macroChoices = new Choices();
            foreach (String triggerPhrase in voiceTriggeredMacros.Keys)
            {
                // validate?
                if (!macroLookup.ContainsKey(triggerPhrase))
                {
                    macroLookup.Add(triggerPhrase, voiceTriggeredMacros[triggerPhrase]);
                }
                macroChoices.Add(triggerPhrase);

            }
            GrammarBuilder macroGrammarBuilder = new GrammarBuilder();
            macroGrammarBuilder.Culture = cultureInfo;
            macroGrammarBuilder.Append(macroChoices);
            macroGrammar = new Grammar(macroGrammarBuilder);
            sre.LoadGrammar(macroGrammar);
            Console.WriteLine("Loaded " + voiceTriggeredMacros.Count + " macro voice triggers into the speech recogniser");
        }

        private static Dictionary<String[], int> getNumberMappings(int start, int end)
        {
            Dictionary<String[], int> dict = new Dictionary<string[], int>();
            for (int i = start; i <= end; i++)
            {
                dict.Add(Configuration.getSpeechRecognitionPhrases(i.ToString()), i);
            }
            return dict;
        }

        // if alwaysUseAllPhrases is true, we add all the phrase options to the recogniser even if the disable_alternative_voice_commands option is true.
        // If alwaysUseAllAppends is true we do the same thing with the append options.
        //
        // The generatedGrammars are loaded by this method call, they're only returned to allow us to detect which grammar has been triggered - the
        // opponent grammar processing stuff needs this.
        private List<Grammar> addCompoundChoices(String[] phrases, Boolean alwaysUseAllPhrases, Choices choices, String[] append, Boolean alwaysUseAllAppends)
        {
            List<Grammar> generatedGrammars = new List<Grammar>();
            foreach (string s in phrases)
            {
                if (s == null || s.Trim().Count() == 0)
                {
                    continue;
                }
                GrammarBuilder gb = new GrammarBuilder();
                gb.Culture = cultureInfo;
                gb.Append(s);
                gb.Append(choices);
                Boolean addAppendChoices = false;
                if (append != null && append.Length > 0)
                {
                    Choices appendChoices = new Choices();
                    foreach (string sa in append)
                    {
                        if (sa == null || sa.Trim().Count() == 0)
                        {
                            continue;
                        }
                        addAppendChoices = true;
                        appendChoices.Add(sa.Trim().Trim());
                        if (disable_alternative_voice_commands && !alwaysUseAllAppends)
                        {
                            break;
                        }
                    }
                    if (addAppendChoices)
                    {
                        gb.Append(appendChoices);
                    }
                }
                if (disable_alternative_voice_commands && !alwaysUseAllPhrases)
                {
                    break;
                }
                Grammar grammar = new Grammar(gb);
                sre.LoadGrammar(grammar);
                generatedGrammars.Add(grammar);
            }
            return generatedGrammars;
        }

        public void Dispose()
        {
            if (waveIn != null)
            {
                try
                {
                    waveIn.Dispose();
                }
                catch (Exception) { }
            }
            if (sre != null)
            {
                try
                {
                    sre.Dispose();
                }
                catch (Exception) { }
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

        private Boolean initWithLocale(String locale)
        {
            RecognizerInfo info = null;
            foreach (RecognizerInfo ri in SpeechRecognitionEngine.InstalledRecognizers())
            {
                if (ri.Culture.TwoLetterISOLanguageName.Equals("en") && locale.Length == 2)
                {
                    info = ri;
                    cultureInfo = ri.Culture;
                    break;
                }
                else if (ri.Culture.Name.Equals(locale))
                {
                    info = ri;
                    cultureInfo = ri.Culture;
                    break;
                }
            }
            if(info != null)
            {
                Console.WriteLine(info.Culture.EnglishName + " - (" + info.Culture.Name + ")");
                this.sre = new SpeechRecognitionEngine(info);
                return this.sre != null;
            }
            if(locale.Length == 2)
            {

                if (MessageBox.Show(Configuration.getUIString("install_any_speechlanguage_popup_text"), Configuration.getUIString("install_speechplatform_popup_title"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=27225");
                }
                Console.WriteLine("Unable to initialise speech engine with the OS's default English voice recognition pack (location name " + locale + "). " +
                    "Check that at least one of MSSpeech_SR_en-GB_TELE.msi, MSSpeech_SR_en-US_TELE.msi, " +
                    "MSSpeech_SR_en-AU_TELE.msi, MSSpeech_SR_en-CA_TELE.msi or MSSpeech_SR_en-IN_TELE.msi are installed." +
                    " It can be downloaded from https://www.microsoft.com/en-us/download/details.aspx?id=27225");
                
                return false;
            }
            else
            {
                if (MessageBox.Show(Configuration.getUIString("install_single_speechlanguage_popup_text_start") + locale + Configuration.getUIString("install_single_speechlanguage_popup_text_end"),
                    Configuration.getUIString("install_speechplatform_popup_title"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=27225");
                }
                Console.WriteLine("Unable to initialise speech engine with voice recognition pack for location " + locale +
                    ". Check MSSpeech_SR_" + locale + "_TELE.msi is installed." +
                    " It can be downloaded from https://www.microsoft.com/en-us/download/details.aspx?id=27225");
                
                return false;
            }            
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
                    if (disable_alternative_voice_commands)
                    {
                        choices.Add(speechPhrases[0]);
                    }
                    else
                    {
                        choices.Add(speechPhrases);
                    }
                }
            }
        }

        public void initialiseSpeechEngine()
        {
            initialised = false;
            if (failedSpeechEngineOnce)
            {
                return;
            }
            try
            {                
                new SpeechRecognitionEngine();                    
            }
            catch (Exception e)
            {
                if (MessageBox.Show(Configuration.getUIString("install_speechplatform_popup_text"), Configuration.getUIString("install_speechplatform_popup_title"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information) == DialogResult.OK)
                {
                    Process.Start("https://www.microsoft.com/en-us/download/details.aspx?id=27225");
                }
                Console.WriteLine("Unable to initialise speech engine. Check that SpeechPlatformRuntime.msi is installed.");
                Console.WriteLine("Exception message: " + e.Message);
                //failedSpeechEngineOnce = true;
                return;
            }
            if (useNAudio) 
            {
                buffer = new RingBufferStream.RingBufferStream(48000);
                waveIn = new NAudio.Wave.WaveInEvent();
                waveIn.DeviceNumber = SpeechRecogniser.initialSpeechInputDeviceIndex;
            }
            String locationToUse = defaultLocale;
            if (location != null && location.Length > 0)
            {
                Console.WriteLine("Attempting to initialise speech recognition for user specified location " + location);
                if (location.Length == 2 && !location.Equals("en"))
                {
                    locationToUse = "en-" + location;
                }
            }
            else
            {
                Console.WriteLine("Attempting to initialise speech recognition for any English locale");
            }
            try
            {
                if (!initWithLocale(locationToUse))
                {
                    return;
                }
                Console.WriteLine("Success");
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to initialise speech engine.");
                Console.WriteLine("Exception message: " + e.Message);
            }
           
            try
            {
                if (useNAudio)
                {
                    waveIn.WaveFormat = new NAudio.Wave.WaveFormat(8000, 1);
                    waveIn.DataAvailable += new EventHandler<NAudio.Wave.WaveInEventArgs>(waveIn_DataAvailable);
                    waveIn.NumberOfBuffers = 3;
                }
                else
                {
                    sre.SetInputToDefaultAudioDevice();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to set default audio device, speech recognition may not function and may crash the app");
                Console.WriteLine("Exception message: " + e.Message);
            }
            try
            {
                if (disable_alternative_voice_commands)
                {
                    Console.WriteLine("*Alternative voice commands are disabled, only the first command from each line in speech_recognition_config.txt will be available*");
                }
                else
                {
                    Console.WriteLine("Loading all voice command alternatives from speech_recognition_config.txt");
                }
                this.digitsChoices = new Choices();
                foreach (KeyValuePair<String[], int> entry in numberToNumber)
                {
                    foreach (String numberStr in entry.Key)
                    {
                        digitsChoices.Add(numberStr);
                    }
                }
                Choices staticSpeechChoices = new Choices();
                validateAndAdd(HOWS_MY_TYRE_WEAR, staticSpeechChoices);
                validateAndAdd(HOWS_MY_TRANSMISSION, staticSpeechChoices);
                validateAndAdd(HOWS_MY_AERO, staticSpeechChoices);
                validateAndAdd(HOWS_MY_ENGINE, staticSpeechChoices);
                validateAndAdd(HOWS_MY_SUSPENSION, staticSpeechChoices);
                validateAndAdd(HOWS_MY_BRAKES, staticSpeechChoices);
                validateAndAdd(HOWS_MY_FUEL, staticSpeechChoices);
                validateAndAdd(HOWS_MY_BATTERY, staticSpeechChoices);
                validateAndAdd(HOWS_MY_PACE, staticSpeechChoices);
                validateAndAdd(HOWS_MY_SELF_PACE, staticSpeechChoices);
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
                validateAndAdd(WHATS_MY_FUEL_USAGE, staticSpeechChoices);
                validateAndAdd(HOW_MUCH_FUEL_TO_END_OF_RACE, staticSpeechChoices);
                validateAndAdd(WHAT_TYRES_AM_I_ON, staticSpeechChoices);
                validateAndAdd(WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES, staticSpeechChoices);
                validateAndAdd(PLAY_CORNER_NAMES, staticSpeechChoices);
                validateAndAdd(HOW_LONG_WILL_THESE_TYRES_LAST, staticSpeechChoices);

                validateAndAdd(DAMAGE_REPORT, staticSpeechChoices);
                validateAndAdd(CAR_STATUS, staticSpeechChoices);
                validateAndAdd(SESSION_STATUS, staticSpeechChoices);
                validateAndAdd(STATUS, staticSpeechChoices);

                validateAndAdd(START_PACE_NOTES_PLAYBACK, staticSpeechChoices);
                validateAndAdd(STOP_PACE_NOTES_PLAYBACK, staticSpeechChoices);

                validateAndAdd(KEEP_QUIET, staticSpeechChoices);
                validateAndAdd(KEEP_ME_INFORMED, staticSpeechChoices);
                validateAndAdd(TELL_ME_THE_GAPS, staticSpeechChoices);
                validateAndAdd(DONT_TELL_ME_THE_GAPS, staticSpeechChoices);
                validateAndAdd(TALK_TO_ME_ANYWHERE, staticSpeechChoices);
                validateAndAdd(DONT_TALK_IN_THE_CORNERS, staticSpeechChoices);
                validateAndAdd(WHATS_THE_FASTEST_LAP_TIME, staticSpeechChoices);
                validateAndAdd(ENABLE_YELLOW_FLAG_MESSAGES, staticSpeechChoices);
                validateAndAdd(DISABLE_YELLOW_FLAG_MESSAGES, staticSpeechChoices);
                validateAndAdd(ENABLE_MANUAL_FORMATION_LAP, staticSpeechChoices);
                validateAndAdd(DISABLE_MANUAL_FORMATION_LAP, staticSpeechChoices);

                validateAndAdd(WHERE_AM_I_FASTER, staticSpeechChoices);
                validateAndAdd(WHERE_AM_I_SLOWER, staticSpeechChoices);

                validateAndAdd(HOW_LONGS_LEFT, staticSpeechChoices);
                validateAndAdd(WHATS_THE_TIME, staticSpeechChoices);
                validateAndAdd(SPOT, staticSpeechChoices);
                validateAndAdd(DONT_SPOT, staticSpeechChoices);
                validateAndAdd(REPEAT_LAST_MESSAGE, staticSpeechChoices);
                validateAndAdd(HAVE_I_SERVED_MY_PENALTY, staticSpeechChoices);
                validateAndAdd(DO_I_HAVE_A_PENALTY, staticSpeechChoices);
                validateAndAdd(DO_I_STILL_HAVE_A_PENALTY, staticSpeechChoices);
                validateAndAdd(IS_MY_PIT_BOX_OCCUPIED, staticSpeechChoices);
                validateAndAdd(PLAY_POST_PIT_POSITION_ESTIMATE, staticSpeechChoices);
                validateAndAdd(PRACTICE_PIT_STOP, staticSpeechChoices);
                validateAndAdd(DO_I_HAVE_A_MANDATORY_PIT_STOP, staticSpeechChoices);
                validateAndAdd(WHAT_ARE_MY_SECTOR_TIMES, staticSpeechChoices);
                validateAndAdd(WHATS_MY_LAST_SECTOR_TIME, staticSpeechChoices);
                validateAndAdd(WHATS_THE_AIR_TEMP, staticSpeechChoices);
                validateAndAdd(WHATS_THE_TRACK_TEMP, staticSpeechChoices);
                validateAndAdd(RADIO_CHECK, staticSpeechChoices);

                validateAndAdd(WHOS_IN_FRONT_IN_THE_RACE, staticSpeechChoices);
                validateAndAdd(WHOS_BEHIND_IN_THE_RACE, staticSpeechChoices);
                validateAndAdd(WHOS_IN_FRONT_ON_TRACK, staticSpeechChoices);
                validateAndAdd(WHOS_BEHIND_ON_TRACK, staticSpeechChoices);
                validateAndAdd(WHOS_LEADING, staticSpeechChoices);

                validateAndAdd(WHAT_CLASS_IS_CAR_AHEAD, staticSpeechChoices);
                validateAndAdd(WHAT_CLASS_IS_CAR_BEHIND, staticSpeechChoices);
                validateAndAdd(IS_CAR_AHEAD_MY_CLASS, staticSpeechChoices);
                validateAndAdd(IS_CAR_BEHIND_MY_CLASS, staticSpeechChoices);

                validateAndAdd(MORE_INFO, staticSpeechChoices);

                validateAndAdd(I_AM_OK, staticSpeechChoices);

                GrammarBuilder staticGrammarBuilder = new GrammarBuilder();
                staticGrammarBuilder.Culture = cultureInfo;
                staticGrammarBuilder.Append(staticSpeechChoices);
                Grammar staticGrammar = new Grammar(staticGrammarBuilder);
                sre.LoadGrammar(staticGrammar);

                // now the fuel choices
                List<string> fuelTimeChoices = new List<string>();
                if (disable_alternative_voice_commands)
                {
                    fuelTimeChoices.Add(LAPS[0]);
                    fuelTimeChoices.Add(MINUTES[0]);
                    fuelTimeChoices.Add(HOURS[0]);
                }
                else
                {
                    fuelTimeChoices.AddRange(LAPS);
                    fuelTimeChoices.AddRange(MINUTES);
                    fuelTimeChoices.AddRange(HOURS);
                }
                addCompoundChoices(CALCULATE_FUEL_FOR, false, this.digitsChoices, fuelTimeChoices.ToArray(), true);
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
            if (!initialised)
            {
                return;
            }
            try
            {
                String usableName = DriverNameHelper.getUsableDriverName(rawDriverName);
                if (usableName != null && usableName.Length > 0)
                {
                    if (driverNamesInUse.Contains(rawDriverName))
                    {
                        return;
                    }
                    if (initialised)
                    {
                        Console.WriteLine("Adding new (mid-session joined) opponent name to speech recogniser: " + Environment.NewLine + usableName);
                        Choices opponentNameChoices = new Choices(usableName);
                        Choices opponentNamePossessiveChoices = new Choices(usableName + POSSESSIVE);

                        opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHERE_IS, WHERES }, false, opponentNameChoices, null, true));
                        opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHAT_TYRE_IS, WHAT_TYRES_IS }, false, opponentNameChoices, new String[] { ON }, true));
                        opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHATS }, true, opponentNamePossessiveChoices, new String[] { LAST_LAP, BEST_LAP, IRATING, LICENSE_CLASS }, true));
                    }
                    // This method is called when a new driver appears mid-session. We need to load the sound file for this new driver
                    // so do it here - nasty nasty hack, need to refactor this. The alternative is to call
                    // SoundCache.loadDriverNameSound in each of mappers when a new driver is added.
                    SoundCache.loadDriverNameSound(usableName);
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
            if (!initialised)
            {
                return;
            }
            driverNamesInUse.Clear();
            foreach (Grammar opponentGrammar in opponentGrammarList)
            {
                sre.UnloadGrammar(opponentGrammar);
            }
            opponentGrammarList.Clear();
            Choices opponentChoices = new Choices();

            // need choice sets for names, possessive names, positions, possessive positions, and combined:
            Choices opponentNameOrPositionChoices = new Choices();
            Choices opponentPositionChoices = new Choices();
            Choices opponentNameOrPositionPossessiveChoices = new Choices();

            if (useNames)
            {
                Console.WriteLine("Adding " + names.Count + " new session opponent names to speech recogniser");
                foreach (String name in names)
                {
                    opponentNameOrPositionChoices.Add(name);
                    opponentNameOrPositionPossessiveChoices.Add(name + POSSESSIVE);
                }
            }

            foreach (KeyValuePair<String[], int> entry in racePositionNumberToNumber)
            {
                foreach (String numberStr in entry.Key)
                {
                    opponentNameOrPositionChoices.Add(POSITION_LONG + " " + numberStr);
                    opponentNameOrPositionChoices.Add(POSITION_SHORT + " " + numberStr);
                    opponentPositionChoices.Add(POSITION_LONG + " " + numberStr);
                    opponentPositionChoices.Add(POSITION_SHORT + " " + numberStr);
                    opponentNameOrPositionPossessiveChoices.Add(POSITION_LONG + " " + numberStr + POSSESSIVE);
                    opponentNameOrPositionPossessiveChoices.Add(POSITION_SHORT + " " + numberStr + POSSESSIVE);
                }
            }
            opponentNameOrPositionChoices.Add(THE_GUY_AHEAD);
            opponentNameOrPositionChoices.Add(THE_CAR_AHEAD);
            opponentNameOrPositionChoices.Add(THE_GUY_IN_FRONT);
            opponentNameOrPositionChoices.Add(THE_GUY_BEHIND);
            opponentNameOrPositionChoices.Add(THE_CAR_BEHIND);
            opponentNameOrPositionChoices.Add(THE_LEADER);
            opponentNameOrPositionPossessiveChoices.Add(THE_GUY_AHEAD);
            opponentNameOrPositionPossessiveChoices.Add(THE_CAR_AHEAD);
            opponentNameOrPositionPossessiveChoices.Add(THE_GUY_IN_FRONT);
            opponentNameOrPositionPossessiveChoices.Add(THE_GUY_BEHIND);
            opponentNameOrPositionPossessiveChoices.Add(THE_CAR_BEHIND);
            opponentNameOrPositionPossessiveChoices.Add(THE_LEADER);

            opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHERE_IS, WHERES }, false, opponentNameOrPositionChoices, null, true));
            opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHOS_IN }, false, opponentPositionChoices, null, true));
            opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHAT_TYRE_IS, WHAT_TYRES_IS }, false, opponentNameOrPositionChoices, new String[] { ON }, true));
            opponentGrammarList.AddRange(addCompoundChoices(new String[] { WHATS }, false, opponentNameOrPositionPossessiveChoices, new String[] { LAST_LAP, BEST_LAP, IRATING, LICENSE_CLASS }, true));

            driverNamesInUse.AddRange(names);
        }

        public void addiRacingSpeechRecogniser()
        {
            if (!initialised)
            {
                return;
            }
            foreach (Grammar iracingGrammar in iracingPitstopGrammarList)
            {
                try
                {
                    sre.UnloadGrammar(iracingGrammar);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to unload iRacing grammar: " + e.Message);
                }
            }
            try
            {
                iracingPitstopGrammarList.Clear();
                Choices iRacingChoices = new Choices();
                if (enable_iracing_pit_stop_commands)
                {
                    List<string> tyrePressureChangePhrases = new List<string>();
                    if (disable_alternative_voice_commands)
                    {
                        tyrePressureChangePhrases.Add(PIT_STOP_CHANGE_TYRE_PRESSURE[0]);
                        tyrePressureChangePhrases.Add(PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE[0]);
                        tyrePressureChangePhrases.Add(PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE[0]);
                        tyrePressureChangePhrases.Add(PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE[0]);
                        tyrePressureChangePhrases.Add(PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE[0]);
                    }
                    else
                    {
                        tyrePressureChangePhrases.AddRange(PIT_STOP_CHANGE_TYRE_PRESSURE);
                        tyrePressureChangePhrases.AddRange(PIT_STOP_CHANGE_FRONT_LEFT_TYRE_PRESSURE);
                        tyrePressureChangePhrases.AddRange(PIT_STOP_CHANGE_FRONT_RIGHT_TYRE_PRESSURE);
                        tyrePressureChangePhrases.AddRange(PIT_STOP_CHANGE_REAR_LEFT_TYRE_PRESSURE);
                        tyrePressureChangePhrases.AddRange(PIT_STOP_CHANGE_REAR_RIGHT_TYRE_PRESSURE);
                    }

                    iracingPitstopGrammarList.AddRange(addCompoundChoices(tyrePressureChangePhrases.ToArray(), true, this.digitsChoices, null, true));
                    List<string> litresAndGallons = new List<string>();
                    litresAndGallons.AddRange(LITERS);
                    litresAndGallons.AddRange(GALLONS);
                    iracingPitstopGrammarList.AddRange(addCompoundChoices(PIT_STOP_ADD, false, this.digitsChoices, litresAndGallons.ToArray(), true));

                    validateAndAdd(PIT_STOP_TEAROFF, iRacingChoices);
                    validateAndAdd(PIT_STOP_FAST_REPAIR, iRacingChoices);
                    validateAndAdd(PIT_STOP_CLEAR_ALL, iRacingChoices);
                    validateAndAdd(PIT_STOP_CLEAR_TYRES, iRacingChoices);
                    validateAndAdd(PIT_STOP_CLEAR_WIND_SCREEN, iRacingChoices);
                    validateAndAdd(PIT_STOP_CLEAR_FAST_REPAIR, iRacingChoices);
                    validateAndAdd(PIT_STOP_CLEAR_FUEL, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_ALL_TYRES, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_FRONT_LEFT_TYRE, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_FRONT_RIGHT_TYRE, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_REAR_LEFT_TYRE, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_REAR_RIGHT_TYRE, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_LEFT_SIDE_TYRES, iRacingChoices);
                    validateAndAdd(PIT_STOP_CHANGE_RIGHT_SIDE_TYRES, iRacingChoices);
                }

                validateAndAdd(HOW_MANY_INCIDENT_POINTS, iRacingChoices);
                validateAndAdd(WHATS_THE_INCIDENT_LIMIT, iRacingChoices);
                validateAndAdd(WHATS_MY_IRATING, iRacingChoices);
                validateAndAdd(WHATS_MY_LICENSE_CLASS, iRacingChoices);
                validateAndAdd(PIT_STOP_FUEL_TO_THE_END, iRacingChoices);
                validateAndAdd(WHATS_THE_SOF, iRacingChoices);

                GrammarBuilder iRacingGrammarBuilder = new GrammarBuilder(iRacingChoices);
                iRacingGrammarBuilder.Culture = cultureInfo;
                Grammar iRacingGrammar = new Grammar(iRacingGrammarBuilder);
                iracingPitstopGrammarList.Add(iRacingGrammar);
                sre.LoadGrammar(iRacingGrammar);
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to add iRacing pit stop commands to speech recognition engine - " + e.Message);
            }
        }
        public static Boolean ResultContains(String result, String[] alternatives)
        {
            result = result.ToLower();
            foreach (String alternative in alternatives)
            {
                if (result == alternative.ToLower())
                {
                    return true;
                }
            }
            // no result with == so try contains
            foreach (String alternative in alternatives)
            {
                if (result.Contains(alternative.ToLower()))
                {
                    return true;
                }
            }
            return false;
        }

        void sre_SpeechRecognized(object sender, SpeechRecognizedEventArgs e)
        {
            SpeechRecogniser.waitingForSpeech = false;
            SpeechRecogniser.gotRecognitionResult = true;
            Console.WriteLine("Recognised : " + e.Result.Text + " confidence = " + e.Result.Confidence.ToString("0.000"));
            try
            {
                // special case when we're waiting for a message after a heavy crash:
                if (DamageReporting.waitingForDriverIsOKResponse)
                {
                    DamageReporting damageReportingEvent = (DamageReporting)CrewChief.getEvent("DamageReporting");
                    if (e.Result.Confidence > minimum_voice_recognition_confidence && ResultContains(e.Result.Text, I_AM_OK))
                    {
                        damageReportingEvent.cancelWaitingForDriverIsOK(DamageReporting.DriverOKResponseType.CLEARLY_OK);
                    }
                    else
                    {
                        damageReportingEvent.cancelWaitingForDriverIsOK(DamageReporting.DriverOKResponseType.NOT_UNDERSTOOD);
                    }
                }
                else
                {
                    if (opponentGrammarList.Contains(e.Result.Grammar))
                    {
                        if (e.Result.Confidence > minimum_name_voice_recognition_confidence)
                        {
                            this.lastRecognisedText = e.Result.Text;
                            CrewChief.getEvent("Opponents").respond(e.Result.Text);
                        }
                        else
                        {
                            crewChief.youWot(true);
                        }
                    }
                    else if (e.Result.Confidence > minimum_voice_recognition_confidence)
                    {
                        if (macroGrammar == e.Result.Grammar && macroLookup.ContainsKey(e.Result.Text))
                        {
                            this.lastRecognisedText = e.Result.Text;
                            macroLookup[e.Result.Text].execute();
                        }
                        else if (iracingPitstopGrammarList.Contains(e.Result.Grammar))
                        {
                            this.lastRecognisedText = e.Result.Text;
                            CrewChief.getEvent("IRacingBroadcastMessageEvent").respond(e.Result.Text);
                        }
                        else if (ResultContains(e.Result.Text, REPEAT_LAST_MESSAGE))
                        {
                            crewChief.audioPlayer.repeatLastMessage();
                        }
                        else if (ResultContains(e.Result.Text, MORE_INFO) && this.lastRecognisedText != null && !use_verbose_responses)
                        {
                            AbstractEvent abstractEvent = getEventForSpeech(this.lastRecognisedText);
                            if (abstractEvent != null)
                            {
                                abstractEvent.respondMoreInformation(this.lastRecognisedText, true);
                            }
                        }
                        else
                        {
                            this.lastRecognisedText = e.Result.Text;
                            AbstractEvent abstractEvent = getEventForSpeech(e.Result.Text);
                            if (abstractEvent != null)
                            {
                                abstractEvent.respond(e.Result.Text);

                                if (use_verbose_responses)
                                {
                                    // In verbose mode, always respond with more info.
                                    abstractEvent.respondMoreInformation(this.lastRecognisedText, false);
                                }
                            }
                        }
                    }
                    else
                    {
                        crewChief.youWot(true);
                    }
                }
            }
            catch (Exception exception)
            {
                Console.WriteLine("Unable to respond - error message: " + exception.Message);
            }

            // 'stop' the recogniser if we're ALWAYS_ON (because we restart it below) or TOGGLE
            // (because the user might have forgotten to press the button to close the channel).
            // For HOLD mode, let the recogniser continue listening and executing commands (invoking this 
            // callback again from another thread) until the button is released, which will call
            // RecogniseAsyncCancel
            if (voiceOptionEnum == MainWindow.VoiceOptionEnum.TOGGLE)
            {
                sre.RecognizeAsyncStop();
                Thread.Sleep(500);
                Console.WriteLine("Stopping speech recognition");
            }
            else if (voiceOptionEnum == MainWindow.VoiceOptionEnum.ALWAYS_ON)
            {
                if (!useNAudio)
                {
                    sre.RecognizeAsyncStop();
                    Thread.Sleep(500);
                    Console.WriteLine("Restarting speech recognition");
                    recognizeAsync();
                }
                // in always-on mode, we're now waiting-for-speech until we get another result
                waitingForSpeech = true;
            }
            else
            {
                // in toggle mode, we're now waiting-for-speech until we get another result or the button is released
                if (SpeechRecogniser.keepRecognisingInHoldMode)
                {
                    Console.WriteLine("Waiting for more speech");
                    waitingForSpeech = true;
                }
            }
        }

        public void recognizeAsync()
        {
            Console.WriteLine("Opened channel - waiting for speech");
            SpeechRecogniser.waitingForSpeech = true;
            SpeechRecogniser.gotRecognitionResult = false;
            SpeechRecogniser.keepRecognisingInHoldMode = true;
            try
            {
                if (useNAudio)
                {
                    Console.WriteLine("Getting audio from nAudio input stream");
                    if (MainWindow.voiceOption == MainWindow.VoiceOptionEnum.HOLD)
                    {
                         waveIn.StartRecording();
                    }
                    else if (MainWindow.voiceOption == MainWindow.VoiceOptionEnum.ALWAYS_ON)
                    {
                        keepRecording = true;
                        (new Thread(() =>
                        {
                            waveIn.StartRecording();
                            while (keepRecording)
                            {
                                Thread.Sleep(5000); // fill the 5s ringbuffer, then process
                                Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo safi =
                                    new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(
                                        waveIn.WaveFormat.SampleRate, Microsoft.Speech.AudioFormat.AudioBitsPerSample.Sixteen, Microsoft.Speech.AudioFormat.AudioChannel.Mono);
                                sre.SetInputToAudioStream(buffer, safi); // otherwise input gets unset
                                try
                                {
                                    sre.RecognizeAsync(RecognizeMode.Multiple); // before this call
                                }
                                catch (Exception e)
                                {
                                    Utilities.ReportException(e, "Exception in SpeechRecognitionEngine.RecognizeAsync.", true /*needReport*/);
                                }
                            }
                            waveIn.StopRecording();
                        })).Start();
                    }
                }
                else
                {
                    Console.WriteLine("Getting audio from default device");
                    try
                    {
                        sre.RecognizeAsync(RecognizeMode.Multiple);
                    }
                    catch (Exception e)
                    {
                        Utilities.ReportException(e, "Exception in SpeechRecognitionEngine.RecognizeAsync.", false /*needReport*/);
                        throw e;
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to start speech recognition " + e.Message);
            }
        }

        public void recognizeAsyncCancel()
        {
            Console.WriteLine("Cancelling wait for speech");
            SpeechRecogniser.waitingForSpeech = false;
            if (useNAudio)
            {
                if (MainWindow.voiceOption == MainWindow.VoiceOptionEnum.HOLD)
                {
                    SpeechRecogniser.keepRecognisingInHoldMode = false;
                    waveIn.StopRecording();
                    Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo safi = new Microsoft.Speech.AudioFormat.SpeechAudioFormatInfo(
                        waveIn.WaveFormat.SampleRate, Microsoft.Speech.AudioFormat.AudioBitsPerSample.Sixteen, Microsoft.Speech.AudioFormat.AudioChannel.Mono);
                    sre.SetInputToAudioStream(buffer, safi); // otherwise input gets unset
                    try
                    {
                        sre.RecognizeAsync(RecognizeMode.Multiple); // before this call
                    }
                    catch (Exception e)
                    {
                        Utilities.ReportException(e, "Exception in SpeechRecognitionEngine.RecognizeAsync.", true /*needReport*/);
                    }
                }
                else if (MainWindow.voiceOption == MainWindow.VoiceOptionEnum.ALWAYS_ON)
                {
                    keepRecording = false;
                    sre.RecognizeAsyncCancel();
                }
            }
            else
            {
                SpeechRecogniser.keepRecognisingInHoldMode = false;
                sre.RecognizeAsyncCancel();
            }
        }

        public void changeInputDevice(int dev)
        {
            if (initialised)
            {
                waveIn.DeviceNumber = dev;
            }
        }

        private void waveIn_DataAvailable(object sender, NAudio.Wave.WaveInEventArgs e)
        {
            lock (buffer)
            {
                buffer.Write(e.Buffer, (int)buffer.Position, e.BytesRecorded);
            }
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
            else if (ResultContains(recognisedSpeech, PLAY_CORNER_NAMES))
            {
                crewChief.playCornerNamesForCurrentLap();
            }
            else if (ResultContains(recognisedSpeech, DONT_TELL_ME_THE_GAPS))
            {
                crewChief.disableDeltasMode();
            }
            else if (ResultContains(recognisedSpeech, TELL_ME_THE_GAPS))
            {
                crewChief.enableDeltasMode();
            }
            else if (ResultContains(recognisedSpeech, ENABLE_YELLOW_FLAG_MESSAGES))
            {
                crewChief.enableYellowFlagMessages();
            }
            else if (ResultContains(recognisedSpeech, DISABLE_YELLOW_FLAG_MESSAGES))
            {
                crewChief.disableYellowFlagMessages();
            }
            else if (ResultContains(recognisedSpeech, ENABLE_MANUAL_FORMATION_LAP))
            {
                crewChief.enableManualFormationLapMode();
            }
            else if (ResultContains(recognisedSpeech, DISABLE_MANUAL_FORMATION_LAP))
            {
                crewChief.disableManualFormationLapMode();
            }
            else if (ResultContains(recognisedSpeech, WHATS_THE_TIME))
            {
                crewChief.reportCurrentTime();
            }
            else if (ResultContains(recognisedSpeech, TALK_TO_ME_ANYWHERE))
            {
                crewChief.disableDelayMessagesInHardParts();
            }
            else if (ResultContains(recognisedSpeech, DONT_TALK_IN_THE_CORNERS))
            {
                crewChief.enableDelayMessagesInHardParts();
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
            else if (ResultContains(recognisedSpeech, WHATS_MY_FUEL_LEVEL)
                || ResultContains(recognisedSpeech, HOWS_MY_FUEL)
                || ResultContains(recognisedSpeech, WHATS_MY_FUEL_USAGE)
                || ResultContains(recognisedSpeech, CALCULATE_FUEL_FOR)
                || ResultContains(recognisedSpeech, HOW_MUCH_FUEL_TO_END_OF_RACE))
            {
                return CrewChief.getEvent("Fuel");
            }
            else if (// TODO: other battery queries
                ResultContains(recognisedSpeech, HOWS_MY_BATTERY))
            {
                return CrewChief.getEvent("Battery");
            }
            else if (ResultContains(recognisedSpeech, WHATS_MY_GAP_IN_FRONT) ||
                ResultContains(recognisedSpeech, WHATS_MY_GAP_BEHIND) ||
                ResultContains(recognisedSpeech, WHERE_AM_I_FASTER) ||
                ResultContains(recognisedSpeech, WHERE_AM_I_SLOWER))
            {
                return CrewChief.getEvent("Timings");
            }
            else if (ResultContains(recognisedSpeech, WHATS_MY_POSITION))
            {
                return CrewChief.getEvent("Position");
            }
            else if (ResultContains(recognisedSpeech, WHAT_WAS_MY_LAST_LAP_TIME) ||
                ResultContains(recognisedSpeech, WHATS_MY_BEST_LAP_TIME) ||
                ResultContains(recognisedSpeech, WHATS_THE_FASTEST_LAP_TIME) ||
                ResultContains(recognisedSpeech, HOWS_MY_PACE) ||
                ResultContains(recognisedSpeech, HOWS_MY_SELF_PACE) ||
                ResultContains(recognisedSpeech, WHAT_ARE_MY_SECTOR_TIMES) ||
                ResultContains(recognisedSpeech, WHATS_MY_LAST_SECTOR_TIME))
            {
                return CrewChief.getEvent("LapTimes");
            }
            else if (ResultContains(recognisedSpeech, WHAT_ARE_MY_TYRE_TEMPS) ||
                ResultContains(recognisedSpeech, HOW_ARE_MY_TYRE_TEMPS) ||
                ResultContains(recognisedSpeech, HOWS_MY_TYRE_WEAR) ||
                ResultContains(recognisedSpeech, HOW_ARE_MY_BRAKE_TEMPS) ||
                ResultContains(recognisedSpeech, WHAT_ARE_MY_BRAKE_TEMPS) ||
                ResultContains(recognisedSpeech, WHAT_ARE_THE_RELATIVE_TYRE_PERFORMANCES) ||
                ResultContains(recognisedSpeech, HOW_LONG_WILL_THESE_TYRES_LAST))
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
            else if (ResultContains(recognisedSpeech, DO_I_HAVE_A_MANDATORY_PIT_STOP) ||
                ResultContains(recognisedSpeech, IS_MY_PIT_BOX_OCCUPIED))
            {
                return CrewChief.getEvent("PitStops");
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
            else if (ResultContains(recognisedSpeech, WHAT_TYRES_AM_I_ON) ||
                ResultContains(recognisedSpeech, WHOS_IN_FRONT_ON_TRACK) ||
                ResultContains(recognisedSpeech, WHOS_IN_FRONT_IN_THE_RACE) ||
                ResultContains(recognisedSpeech, WHOS_BEHIND_ON_TRACK) ||
                ResultContains(recognisedSpeech, WHOS_BEHIND_IN_THE_RACE) ||
                ResultContains(recognisedSpeech, WHOS_LEADING))
            {
                return CrewChief.getEvent("Opponents");
            }
            // multiple events for status reporting:
            else if (ResultContains(recognisedSpeech, DAMAGE_REPORT))
            {
                CrewChief.getDamageReport();
            }
            else if (ResultContains(recognisedSpeech, CAR_STATUS))
            {
                CrewChief.getCarStatus();
            }
            else if (ResultContains(recognisedSpeech, STATUS))
            {
                CrewChief.getStatus();
            }
            else if (ResultContains(recognisedSpeech, SESSION_STATUS))
            {
                CrewChief.getSessionStatus();
            }
            else if (ResultContains(recognisedSpeech, START_PACE_NOTES_PLAYBACK))
            {
                if (!DriverTrainingService.isPlayingPaceNotes)
                {
                    crewChief.togglePaceNotesPlayback();
                }
            }
            else if (ResultContains(recognisedSpeech, STOP_PACE_NOTES_PLAYBACK))
            {
                if (DriverTrainingService.isPlayingPaceNotes)
                {
                    crewChief.togglePaceNotesPlayback();
                }
            }
            else if (ResultContains(recognisedSpeech, IS_CAR_AHEAD_MY_CLASS) ||
                ResultContains(recognisedSpeech, IS_CAR_BEHIND_MY_CLASS) ||
                ResultContains(recognisedSpeech, WHAT_CLASS_IS_CAR_AHEAD) ||
                ResultContains(recognisedSpeech, WHAT_CLASS_IS_CAR_BEHIND))
            {
                return CrewChief.getEvent("MulticlassWarnings");
            }
            else if (ResultContains(recognisedSpeech, PRACTICE_PIT_STOP) ||
                ResultContains(recognisedSpeech, PLAY_POST_PIT_POSITION_ESTIMATE))
            {
                return CrewChief.getEvent("Strategy");
            }
            return null;
        }
    }
}
