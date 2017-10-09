using CrewChiefV4.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.commands
{
    class MacroManager
    {
        // initialise some static test hack to see if this shit hangs together. This is called immediately after initialising 
        // the speech recogniser in MainWindow.
        public static void initialise(AudioPlayer audioPlayer, SpeechRecogniser speechRecogniser)
        {
            Dictionary<string, CommandMacro> voiceTriggeredMacros = new Dictionary<string, CommandMacro>();

            // the test command at the bottom of the speech recogniser config file
            String testCommandSpeechRecogniserPhrase = "MACRO_TEST";    

            // quick n dirty smoke test - press some keys with pauses - these could be assigned to open pit menu, go right, confirm, whatever
            MacroItem[] r3eSelectNextPitItem = new MacroItem[] { 
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_D),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_E)
            };

            voiceTriggeredMacros.Add(testCommandSpeechRecogniserPhrase, new CommandMacro(audioPlayer, AudioPlayer.folderAcknowlegeOK, r3eSelectNextPitItem));
            speechRecogniser.loadMacroVoiceTriggers(voiceTriggeredMacros);
        }
    }
}
