using CrewChiefV4.Audio;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CrewChiefV4.commands
{
    class MacroManager
    {
        // messages used when a pit request or cancel pit request isn't relevant (pcars2 and rf2 only):
        public static String folderPitAlreadyRequested = "mandatory_pitstops/pitstop_already_requested";
        public static String folderPitNotRequested = "mandatory_pitstops/pitstop_not_requested";

        // these are the macro names used to idenify certain macros which have special hard-coded behaviours. Not idea...
        public static readonly String MULTIPLE_IDENTIFIER = "Multiple";
        public static readonly String REQUEST_PIT_IDENTIFIER = "request pit";
        public static readonly String CANCEL_REQUEST_PIT_IDENTIFIER = "cancel pit request";
        public static readonly String AUTO_FUEL_IDENTIFIER = "auto fuel";
        public static readonly String MULTIPLE_DECREASE_IDENTIFIER = "Decrease";
        public static readonly String MULTIPLE_LEFT_IDENTIFIER = "Left";
        // another magic case - in R3E we can't track which strategy we're on because the menu wraps and we don't 
        // know how many there are anyway. So as soon as we change to a different strat, reset the fuel-added counter.
        public static readonly String R3E_STRAT_IDENTIFIER = "pit preset";

        public static Boolean enablePitExitPositionEstimates = UserSettings.GetUserSettings().getBoolean("enable_pit_exit_position_estimates");

        public static Boolean bringGameWindowToFrontForMacros = UserSettings.GetUserSettings().getBoolean("bring_game_window_to_front_for_macros");
        public static Boolean enableAutoTriggering = UserSettings.GetUserSettings().getBoolean("allow_macros_to_trigger_automatically");

        public static int lastFuelAmountAddedToThisStrat = 0;

        public static Boolean stopped = false;

        // make all the macros available so the events can press buttons as they see fit:
        public static Dictionary<string, ExecutableCommandMacro> macros = new Dictionary<string, ExecutableCommandMacro>();

        public static void stop()
        {
            stopped = true;
            KeyPresser.releasePressedKey();
        }

        public static void clearState()
        {
            MacroManager.lastFuelAmountAddedToThisStrat = 0;
        }

        // This is called immediately after initialising the speech recogniser in MainWindow
        public static void initialise(AudioPlayer audioPlayer, SpeechRecogniser speechRecogniser)
        {
            stopped = false;
            macros.Clear();
            if (UserSettings.GetUserSettings().getBoolean("enable_command_macros"))
            {
                // load the json:
                MacroContainer macroContainer = loadCommands(getUserMacrosFileLocation());
                // if it's valid, load the command sets:
                if (macroContainer.assignments != null && macroContainer.assignments.Length > 0 && macroContainer.macros != null)
                {
                    // get the assignments by game:
                    Dictionary<String, KeyBinding[]> assignmentsByGame = new Dictionary<String, KeyBinding[]>();
                    foreach (Assignment assignment in macroContainer.assignments)
                    {
                        if (!assignmentsByGame.ContainsKey(assignment.gameDefinition))
                        {
                            assignmentsByGame.Add(assignment.gameDefinition, assignment.keyBindings);
                        }
                    }

                    Dictionary<string, ExecutableCommandMacro> voiceTriggeredMacros = new Dictionary<string, ExecutableCommandMacro>();
                    foreach (Macro macro in macroContainer.macros)
                    {
                        Boolean hasCommandForCurrentGame = false;
                        Boolean allowAutomaticTriggering = false;
                        // eagerly load the key bindings for each macro:
                        foreach (CommandSet commandSet in macro.commandSets)
                        {
                            if (commandSet.gameDefinition.Equals(CrewChief.gameDefinition.gameEnum.ToString(), StringComparison.InvariantCultureIgnoreCase))
                            {
                                hasCommandForCurrentGame = true;
                                allowAutomaticTriggering = commandSet.allowAutomaticTriggering;
                                // this does the conversion from key characters to key enums and stores the result to save us doing it every time
                                commandSet.getActionItems(false, assignmentsByGame[commandSet.gameDefinition]);
                                break;
                            }
                        }
                        if (hasCommandForCurrentGame)
                        {
                            // make this macro globally visible:
                            ExecutableCommandMacro commandMacro = new ExecutableCommandMacro(audioPlayer, macro, assignmentsByGame, allowAutomaticTriggering);
                            macros.Add(macro.name, commandMacro);
                            // if there's a voice command, load it into the recogniser:
                            if (macro.voiceTriggers != null && macro.voiceTriggers.Length > 0)
                            {
                                foreach (String voiceTrigger in macro.voiceTriggers)
                                {
                                    if (voiceTriggeredMacros.ContainsKey(voiceTrigger))
                                    {
                                        Console.WriteLine("Voice trigger " + voiceTrigger + " has already been allocated to a different command");
                                    }
                                    else
                                    {
                                        voiceTriggeredMacros.Add(voiceTrigger, commandMacro);
                                    }
                                }
                            }
                        }
                    }
                    try
                    {
                        speechRecogniser.loadMacroVoiceTriggers(voiceTriggeredMacros);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to load command macros into speech recogniser: " + e.Message);
                    }
                }
            }
            else
            {
                Console.WriteLine("Command macros are disabled");
            }
        }

        // file loading boilerplate - needs refactoring
        private static MacroContainer loadCommands(String filename)
        {
            if (filename != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<MacroContainer>(getFileContents(filename));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error pasing " + filename + ": " + e.Message);
                }
            }
            return new MacroContainer();
        }

        private static String getFileContents(String fullFilePath)
        {
            StringBuilder jsonString = new StringBuilder();
            StreamReader file = null;
            try
            {
                file = new StreamReader(fullFilePath);
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    if (!line.Trim().StartsWith("#"))
                    {
                        jsonString.AppendLine(line);
                    }
                }
                return jsonString.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading file " + fullFilePath + ": " + e.Message);
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
            return null;
        }

        private static String getUserMacrosFileLocation()
        {
            String path = System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "saved_command_macros.json");

            if (File.Exists(path))
            {
                Console.WriteLine("Loading user-configured command macros from Documents/CrewChiefV4/ folder");
                return path;
            }
            else
            {
                Console.WriteLine("Loading default command macros from installation folder");
                return Configuration.getDefaultFileLocation("saved_command_macros.json");
            }
        }
    }
}
