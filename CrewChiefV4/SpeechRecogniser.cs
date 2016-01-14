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

        private static String defaultLocale = "en";

        // externalise these?
        public static String FUEL = "fuel";
        public static String FUEL_LEVEL = "fuel level";
        public static String TYRE_WEAR = "tyre wear";
        public static String TYRE_TEMPS = "tyre temps";
        public static String TYRE_TEMPERATURES = "tyre temperatures";
        public static String BRAKE_TEMPS = "brake temps";
        public static String BRAKE_TEMPERATURES = "brake temperatures";
        public static String AERO = "aero";
        public static String BODY_WORK = "body work";
        public static String TRANSMISSION = "transmission";
        public static String ENGINE = "engine";
        public static String SUSPENSION = "suspension";
        public static String BRAKES = "brakes";
        public static String ENGINE_TEMPS = "engine temps";
        public static String ENGINE_TEMPERATURES = "engine temperatures";
        public static String GAP_IN_FRONT = "gap in front";
        public static String GAP_AHEAD = "gap ahead";
        public static String GAP_BEHIND = "gap behind";
        public static String LAST_LAP_TIME = "last lap time";
        public static String LAP_TIME = "lap time";
        public static String LAST_LAP = "last lap";
        public static String POSITION = "position";
        public static String PEA = "pea";
        public static String PACE = "pace";

        public static String WHAT_ARE_MY = "what are my";

        private static String KEEP_QUIET = "keep quiet";
        private static String SHUT_UP = "shut up";
        private static String I_KNOW_WHAT_IM_DOING = "I know what I'm doing";
        private static String LEAVE_ME_ALONE = "leave me alone";
        private static String DONT_TELL_ME_THE_GAPS = "don't tell me the gaps";
        private static String DONT_TELL_ME_THE_DELTAS = "don't tell me the deltas";
        private static String DONT_GIVE_ME_THE_DELTAS = "don't give me the deltas"; 
        private static String NO_MORE_DELTAS = "no more deltas";
        private static String NO_MORE_GAPS = "no more gaps";

        private static String KEEP_ME_UPDATED = "keep me updated";
        private static String KEEP_ME_INFORMED = "keep me informed";
        private static String KEEP_ME_POSTED = "keep me posted";
        private static String TELL_ME_THE_GAPS = "tell me the gaps";
        private static String TELL_ME_THE_DELTAS = "tell me the deltas";
        private static String GIVE_ME_THE_DELTAS = "give me the deltas";

        private static String HOW_LONGS_LEFT = "how long's left";
        private static String HOW_MANY_LAPS_LEFT = "how many laps left";
        private static String HOW_MANY_LAPS_TO_GO = "how many laps to go";

        private static String SPOT = "spot";
        private static String DONT_SPOT = "don't spot";

        public static String DO_I_STILL_HAVE_A_PENALTY = "do I still have a penalty";
        public static String DO_I_HAVE_A_PENALTY = "do I have a penalty";
        public static String HAVE_I_SERVED_MY_PENALTY = "have I served my penalty";

        public static String DO_I_HAVE_TO_PIT = "do I have to pit";
        public static String DO_I_NEED_TO_PIT = "do I need to pit";
        public static String DO_I_HAVE_A_MANDATORY_PIT_STOP = "do I have a mandatory pit stop";
        public static String DO_I_HAVE_A_MANDATORY_STOP = "do I have a mandatory stop";
        public static String DO_I_HAVE_TO_MAKE_A_PIT_STOP = "do I have to make a pit stop";

        public static String WHERE_IS = "where's";
        public static String WHOS_IN_FRONT_IN_THE_RACE = "who's in front in the race";
        public static String WHOS_AHEAD_IN_THE_RACE = "who's ahead in the race";
        public static String WHOS_BEHIND_IN_THE_RACE = "who's behind in the race"; 
        public static String WHOS_IN_FRONT = "who's in front";
        public static String WHOS_AHEAD = "who's ahead";
        public static String WHOS_BEHIND = "who's behind"; 
        public static String WHOS_IN_FRONT_ON_TRACK = "who's in front on track";
        public static String WHOS_AHEAD_ON_TRACK = "who's ahead on track";
        public static String WHOS_BEHIND_ON_TRACK = "who's behind on track";
        public static String WHOS_IN = "who's in";
        public static String WHOS_LEADING = "who's leading";
        public static String WHATS = "what's";
        public static String BEST_LAP = "best lap";
        public static String BEST_LAP_TIME = "best lap time";
        public static String THE_LEADER = "the leader";
        public static String THE_CAR_AHEAD = "the car ahead";
        public static String THE_CAR_IN_FRONT = "the car in front";
        public static String THE_GUY_AHEAD = "the guy ahead";
        public static String THE_GUY_IN_FRONT = "the guy in front";
        public static String THE_CAR_BEHIND = "the car behind";
        public static String THE_GUY_BEHIND = "the guy behind";

        public static String WHAT_TYRES_IS = "what tyres is";
        public static String WHAT_TYRE_IS = "what tyre is";

        public static String REPEAT_LAST_MESSAGE = "repeat last message";
        public static String SAY_AGAIN = "say again";

        public static String WHAT_ARE_MY_SECTOR_TIMES = "what are my sector times";
        public static String WHATS_MY_LAST_SECTOR_TIME = "what's my last sector time";

        public static String WHATS_THE_AIR_TEMP = "what's the air temp"; 
        public static String WHATS_THE_AIR_TEMPERATURE = "what's the air temperature"; 
        public static String WHATS_THE_TRACK_TEMP = "what's the track temp"; 
        public static String WHATS_THE_TRACK_TEMPERATURE = "what's the track temperature";
        
        private CrewChief crewChief;

        public Boolean initialised = false;

        public MainWindow.VoiceOptionEnum voiceOptionEnum;

        private List<String> driverNamesInUse = new List<string>();

        private List<Grammar> opponentGrammarList = new List<Grammar>();
        
        private System.Globalization.CultureInfo cultureInfo;

        public static Dictionary<String, int> numberToNumber = new Dictionary<String, int>(){
            {"one", 1}, {"two", 2}, {"three", 3}, {"four", 4}, {"five", 5}, {"six", 6}, {"seven", 7}, {"eight", 8}, 
            {"nine", 9}, {"ten", 10}, {"eleven", 11}, {"twelve", 12},  {"thirteen", 13}, {"fourteen", 14}, {"fifteen", 15}, {"sixteen", 16}, 
            {"seventeen", 17}, {"eighteen", 18}, {"nineteen", 19}, {"twenty", 20}, {"twenty-one", 21}, 
            {"twenty-two", 22}, {"twenty-three", 23}, {"twenty-four", 24}, {"twenty-five", 25}, {"twenty-six", 26}, 
            {"twenty-seven", 27}, {"twenty-eight", 28}, {"twenty-nine", 29}, {"thirty", 30}, {"thirty-one", 31}, 
            {"thirty-two", 32}, {"thirty-three", 33}, {"thirty-four", 34}, {"thirty-five", 35}, {"thirty-six", 36}, 
            {"thirty-seven", 37}, {"thirty-eight", 38}, {"thirty-nine", 39}, {"fourty", 40}, {"fourty-one", 41}, 
            {"fourty-two", 42}, {"fourty-three", 43}, {"fourty-four", 44}, {"fourty-five", 45}, {"fourty-six", 46}, 
            {"fourty-seven", 47}, {"fourty-eight", 48}, {"fourty-nine", 49}, {"fifty", 50}
        };

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


                Choices info0 = new Choices();
                info0.Add(new string[] { "how's my", "how is my" });
                Choices info1 = new Choices();
                info1.Add(new string[] { FUEL, TYRE_WEAR, AERO, BODY_WORK, TRANSMISSION, ENGINE, SUSPENSION, PACE, 
                    TYRE_TEMPS, TYRE_TEMPERATURES, BRAKE_TEMPS, BRAKE_TEMPERATURES, BRAKES, ENGINE_TEMPS, ENGINE_TEMPERATURES });
                GrammarBuilder gb1 = new GrammarBuilder();
                gb1.Culture = cultureInfo;
                gb1.Append(info0);
                gb1.Append(info1);
                Grammar g1 = new Grammar(gb1);

                Choices info2 = new Choices();
                info2.Add(new string[] { GAP_IN_FRONT, GAP_AHEAD, GAP_BEHIND, LAST_LAP, LAP_TIME, LAST_LAP_TIME, BEST_LAP, BEST_LAP_TIME, POSITION, FUEL_LEVEL });
                GrammarBuilder gb2 = new GrammarBuilder();
                gb2.Culture = cultureInfo;
                gb2.Append("what's my");
                gb2.Append(info2);
                Grammar g2 = new Grammar(gb2);

                Choices info3 = new Choices();
                info3.Add(new string[] { TYRE_TEMPS, TYRE_TEMPERATURES, BRAKE_TEMPS, BRAKE_TEMPERATURES, BRAKES, ENGINE_TEMPS, ENGINE_TEMPERATURES });
                GrammarBuilder gb3 = new GrammarBuilder();
                gb3.Culture = cultureInfo;
                gb3.Append("how are my");
                gb3.Append(info3);
                Grammar g3 = new Grammar(gb3);

                Choices info4 = new Choices();
                info4.Add(new string[] { KEEP_QUIET, SHUT_UP, I_KNOW_WHAT_IM_DOING, LEAVE_ME_ALONE, DONT_TELL_ME_THE_GAPS, DONT_GIVE_ME_THE_DELTAS, DONT_TELL_ME_THE_GAPS,
                    NO_MORE_DELTAS, NO_MORE_GAPS, KEEP_ME_INFORMED, KEEP_ME_POSTED, KEEP_ME_UPDATED, TELL_ME_THE_GAPS, GIVE_ME_THE_DELTAS, TELL_ME_THE_DELTAS,
                    HOW_LONGS_LEFT, HOW_MANY_LAPS_LEFT, HOW_MANY_LAPS_TO_GO, SPOT, DONT_SPOT, REPEAT_LAST_MESSAGE, SAY_AGAIN,HAVE_I_SERVED_MY_PENALTY, DO_I_HAVE_A_PENALTY, DO_I_STILL_HAVE_A_PENALTY,
                    DO_I_HAVE_A_MANDATORY_PIT_STOP, DO_I_NEED_TO_PIT, DO_I_HAVE_A_MANDATORY_STOP, DO_I_HAVE_TO_MAKE_A_PIT_STOP, DO_I_HAVE_TO_PIT, WHAT_ARE_MY_SECTOR_TIMES, WHATS_MY_LAST_SECTOR_TIME,
                    WHATS_THE_AIR_TEMP, WHATS_THE_AIR_TEMPERATURE, WHATS_THE_TRACK_TEMP, WHATS_THE_TRACK_TEMPERATURE});
                GrammarBuilder gb4 = new GrammarBuilder();
                gb4.Culture = cultureInfo;
                gb4.Append(info4);
                Grammar g4 = new Grammar(gb4);

                Choices info5 = new Choices();
                info5.Add(new string[] { TYRE_TEMPS, TYRE_TEMPERATURES, BRAKE_TEMPS, BRAKE_TEMPERATURES });
                GrammarBuilder gb5 = new GrammarBuilder();
                gb5.Culture = cultureInfo;
                gb5.Append(WHAT_ARE_MY);
                gb5.Append(info5);
                Grammar g5 = new Grammar(gb5);

                sre.LoadGrammar(g1);
                sre.LoadGrammar(g2);
                sre.LoadGrammar(g3);
                sre.LoadGrammar(g4);
                sre.LoadGrammar(g5);
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
                        Choices opponentChoices = new Choices();
                        opponentChoices.Add(WHERE_IS + " " + usableName);
                        opponentChoices.Add(WHATS + " " + usableName + "'s " + LAST_LAP);
                        opponentChoices.Add(WHATS + " " + usableName + "'s " + BEST_LAP);
                        opponentChoices.Add(WHAT_TYRES_IS + " " + usableName + " on");
                        opponentChoices.Add(WHAT_TYRE_IS + " " + usableName + " on");

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
                    opponentChoices.Add(WHATS + " " + name + "'s " + LAST_LAP);
                    opponentChoices.Add(WHATS + " " + name + "'s " + BEST_LAP);
                    opponentChoices.Add(WHAT_TYRES_IS + " " + name + " on");
                    opponentChoices.Add(WHAT_TYRE_IS + " " + name + " on");
                }
            }
            foreach (KeyValuePair<String, int> entry in numberToNumber)
            {
                opponentChoices.Add(WHATS + " " + POSITION + " " + entry.Key + "'s " + LAST_LAP);
                opponentChoices.Add(WHATS + " " + POSITION + " " + entry.Key + "'s " + BEST_LAP);
                opponentChoices.Add(WHAT_TYRE_IS + " " + POSITION + " " + entry.Key + " on");
                opponentChoices.Add(WHAT_TYRES_IS + " " + POSITION + " " + entry.Key + " on");
                opponentChoices.Add(WHATS + " " + PEA + " " + entry.Key + "'s " + LAST_LAP);
                opponentChoices.Add(WHATS + " " + PEA + " " + entry.Key + "'s " + BEST_LAP);
                opponentChoices.Add(WHOS_IN + " " + PEA + " " + entry.Key);
                opponentChoices.Add(WHOS_IN + " " + POSITION + " " + entry.Key);
                opponentChoices.Add(WHAT_TYRE_IS + " " + PEA + " " + entry.Key + " on");
                opponentChoices.Add(WHAT_TYRES_IS + " " + PEA + " " + entry.Key + " on");
                opponentChoices.Add(WHERE_IS + " " + PEA + " " + entry.Key);
                opponentChoices.Add(WHERE_IS + " " + POSITION + " " + entry.Key);
            }
            opponentChoices.Add(WHATS + " " + THE_LEADER +"'s " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_LEADER + "'s " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_IN_FRONT + "'s " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_IN_FRONT + "'s " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_AHEAD + "'s " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_AHEAD + "'s " + LAST_LAP);
            opponentChoices.Add(WHATS + " " + THE_CAR_BEHIND + "'s " + BEST_LAP);
            opponentChoices.Add(WHATS + " " + THE_GUY_BEHIND + "'s " + LAST_LAP);
            opponentChoices.Add(WHAT_TYRE_IS + " " + THE_GUY_IN_FRONT + " on");
            opponentChoices.Add(WHAT_TYRES_IS + " " + THE_GUY_IN_FRONT + " on");
            opponentChoices.Add(WHAT_TYRE_IS + " " + THE_GUY_AHEAD + " on");
            opponentChoices.Add(WHAT_TYRES_IS + " " + THE_GUY_AHEAD + " on");
            opponentChoices.Add(WHAT_TYRE_IS + " " + THE_GUY_BEHIND + " on");
            opponentChoices.Add(WHAT_TYRES_IS + " " + THE_GUY_BEHIND + " on");

            opponentChoices.Add(WHOS_BEHIND_IN_THE_RACE);
            opponentChoices.Add(WHOS_BEHIND);
            opponentChoices.Add(WHOS_IN_FRONT_IN_THE_RACE);
            opponentChoices.Add(WHOS_IN_FRONT);
            opponentChoices.Add(WHOS_AHEAD_IN_THE_RACE);
            opponentChoices.Add(WHOS_AHEAD);
            opponentChoices.Add(WHOS_BEHIND_ON_TRACK);
            opponentChoices.Add(WHOS_IN_FRONT_ON_TRACK);
            opponentChoices.Add(WHOS_AHEAD_ON_TRACK);
            opponentChoices.Add(WHOS_LEADING);
            GrammarBuilder opponentGrammarBuilder = new GrammarBuilder();
            opponentGrammarBuilder.Culture = cultureInfo;
            opponentGrammarBuilder.Append(opponentChoices);
            Grammar newOpponentGrammar = new Grammar(opponentGrammarBuilder);
            sre.LoadGrammar(newOpponentGrammar);
            opponentGrammarList.Add(newOpponentGrammar);
            driverNamesInUse.AddRange(names);
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
                    if (e.Result.Text.Contains(REPEAT_LAST_MESSAGE) || e.Result.Text.Contains(SAY_AGAIN))
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
            if (recognisedSpeech.Contains(DONT_SPOT))
            {
                crewChief.disableSpotter();
            }
            else if (recognisedSpeech.Contains(SPOT))
            {
                crewChief.enableSpotter();
            }
            else if (recognisedSpeech.Contains(KEEP_QUIET) ||
                recognisedSpeech.Contains(SHUT_UP) ||
                recognisedSpeech.Contains(I_KNOW_WHAT_IM_DOING) ||
                recognisedSpeech.Contains(LEAVE_ME_ALONE))
            {
                crewChief.enableKeepQuietMode();
            }
            else if (recognisedSpeech.Contains(DONT_TELL_ME_THE_GAPS) || recognisedSpeech.Contains(DONT_TELL_ME_THE_DELTAS) ||
                recognisedSpeech.Contains(DONT_GIVE_ME_THE_DELTAS) || recognisedSpeech.Contains(NO_MORE_DELTAS) ||
                recognisedSpeech.Contains(NO_MORE_GAPS))
            {
                crewChief.disableDeltasMode();
            }
            else if (recognisedSpeech.Contains(TELL_ME_THE_GAPS) || recognisedSpeech.Contains(GIVE_ME_THE_DELTAS) ||
                recognisedSpeech.Contains(TELL_ME_THE_DELTAS))
            {
                crewChief.enableDeltasMode();
            }
            else if (recognisedSpeech.Contains(AERO) ||
               recognisedSpeech.Contains(BODY_WORK) ||
               recognisedSpeech.Contains(TRANSMISSION) ||
               recognisedSpeech.Contains(ENGINE) ||
               recognisedSpeech.Contains(SUSPENSION) ||
               recognisedSpeech.Contains(BRAKES))
            {
                return CrewChief.getEvent("DamageReporting");
            }
            else if (recognisedSpeech.Contains(KEEP_ME_UPDATED) ||
                recognisedSpeech.Contains(KEEP_ME_POSTED) ||
                recognisedSpeech.Contains(KEEP_ME_INFORMED))
            {
                crewChief.disableKeepQuietMode();
            }
            else if (recognisedSpeech.Contains(FUEL) || recognisedSpeech.Contains(FUEL_LEVEL))
            {
                return CrewChief.getEvent("Fuel");
            }
            else if (recognisedSpeech.Contains(GAP_IN_FRONT) ||
                recognisedSpeech.Contains(GAP_AHEAD) ||
                recognisedSpeech.Contains(GAP_BEHIND))
            {
                return CrewChief.getEvent("Timings");
            }
            else if (recognisedSpeech.Contains(POSITION))
            {
                return CrewChief.getEvent("Position");
            }
            else if (recognisedSpeech.Contains(LAST_LAP_TIME) ||
                recognisedSpeech.Contains(LAP_TIME) ||
                recognisedSpeech.Contains(LAST_LAP) || 
                recognisedSpeech.Contains(BEST_LAP_TIME) ||
                recognisedSpeech.Contains(BEST_LAP) ||
                recognisedSpeech.Contains(PACE) ||
                recognisedSpeech.Contains(WHAT_ARE_MY_SECTOR_TIMES) || 
                recognisedSpeech.Contains(WHATS_MY_LAST_SECTOR_TIME))
            {
                return CrewChief.getEvent("LapTimes");
            }
            else if (recognisedSpeech.Contains(TYRE_TEMPS) ||
                recognisedSpeech.Contains(TYRE_TEMPERATURES) || 
                recognisedSpeech.Contains(TYRE_WEAR) ||
                recognisedSpeech.Contains(BRAKE_TEMPS) ||
                recognisedSpeech.Contains(BRAKE_TEMPERATURES))
            {
                return CrewChief.getEvent("TyreMonitor");
            }
            else if (recognisedSpeech.Contains(HOW_LONGS_LEFT) || 
                recognisedSpeech.Contains(HOW_MANY_LAPS_TO_GO) ||
                recognisedSpeech.Contains(HOW_MANY_LAPS_LEFT))
            {
                return CrewChief.getEvent("RaceTime");
            }
            else if (recognisedSpeech.Contains(DO_I_STILL_HAVE_A_PENALTY) ||
                recognisedSpeech.Contains(DO_I_HAVE_A_PENALTY) ||
                recognisedSpeech.Contains(HAVE_I_SERVED_MY_PENALTY))
            {
                return CrewChief.getEvent("Penalties");
            }
            else if (recognisedSpeech.Contains(DO_I_HAVE_TO_PIT) ||
               recognisedSpeech.Contains(DO_I_HAVE_A_MANDATORY_PIT_STOP) ||
               recognisedSpeech.Contains(DO_I_HAVE_A_MANDATORY_STOP) ||
               recognisedSpeech.Contains(DO_I_NEED_TO_PIT) ||
                recognisedSpeech.Contains(DO_I_HAVE_TO_MAKE_A_PIT_STOP))
            {
                return CrewChief.getEvent("MandatoryPitStops");
            }
            else if (recognisedSpeech.Contains(ENGINE_TEMPS) || recognisedSpeech.Contains(ENGINE_TEMPERATURES))
            {
                return CrewChief.getEvent("EngineMonitor");
            }
            else if (recognisedSpeech.Contains(WHATS_THE_AIR_TEMP) ||
               recognisedSpeech.Contains(WHATS_THE_AIR_TEMPERATURE) ||
               recognisedSpeech.Contains(WHATS_THE_TRACK_TEMP) ||
               recognisedSpeech.Contains(WHATS_THE_TRACK_TEMPERATURE))
            {
                return CrewChief.getEvent("ConditionsMonitor");
            }
            return null;
        }
    }
}
