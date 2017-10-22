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
        // This is called immediately after initialising the speech recogniser in MainWindow
        public static void initialise(AudioPlayer audioPlayer, SpeechRecogniser speechRecogniser)
        {
            if (UserSettings.GetUserSettings().getBoolean("enable_command_macros"))
            {
                // load the json:
                MacroContainer macroContainer = loadCommands(getUserMacrosFileLocation());

                // get the assignments by game:
                Dictionary<String, KeyBinding[]> assignmentsByGame = new Dictionary<String, KeyBinding[]>();
                foreach (Assignment assignment in macroContainer.assignments)
                {
                    if (!assignmentsByGame.ContainsKey(assignment.gameDefinition))
                    {
                        assignmentsByGame.Add(assignment.gameDefinition, assignment.keyBindings);
                    }
                }

                // now load them into the speech recogniser
                Dictionary<string, ExecutableCommandMacro> voiceTriggeredMacros = new Dictionary<string, ExecutableCommandMacro>();
                foreach (Macro macro in macroContainer.macros)
                {
                    if (macro.voiceTriggers != null && macro.voiceTriggers.Length > 0)
                    {
                        ExecutableCommandMacro commandMacro = new ExecutableCommandMacro(audioPlayer, macro, assignmentsByGame);
                        foreach (String voiceTrigger in macro.voiceTriggers)
                        {
                            voiceTriggeredMacros.Add(voiceTrigger, commandMacro);
                        }
                    }
                    // now eagerly load the key bindings for each macro:
                    foreach (CommandSet commandSet in macro.commandSets)
                    {
                        // this does the conversion from key characters to key enums and stores the result to save us doing it every time
                        commandSet.getActionItems(false, assignmentsByGame[commandSet.gameDefinition]);
                    }
                }
                speechRecogniser.loadMacroVoiceTriggers(voiceTriggeredMacros);
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
