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

            // quick n dirty hard-coded macros to test R3E integration. Assume R3E pit menu assignments are:
            // Q: open / close menu
            // WASD: up / down / left / right
            // E: select
            MacroItem[] r3emakeOrCancelPitRequest = new MacroItem[] { 
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_W),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_E),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q)
            };

            MacroItem[] r3eSelectNextPitPreset = new MacroItem[] { 
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_D),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q)
            };

            MacroItem[] r3eSelectPreviousPitPreset = new MacroItem[] { 
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_A),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q)
            };

            MacroItem[] r3eConfirmPitActions = new MacroItem[] { 
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_Q),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_S),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_S),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_S),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_S),
                new MacroItem(100),
                new MacroItem(CrewChiefV4.commands.KeyPresser.KeyCode.KEY_E)
            };

            voiceTriggeredMacros.Add("PIT_REQUEST", new CommandMacro(audioPlayer, AudioPlayer.folderAcknowlegeOK, r3emakeOrCancelPitRequest));
            voiceTriggeredMacros.Add("NEXT_PIT_PRESET", new CommandMacro(audioPlayer, AudioPlayer.folderAcknowlegeOK, r3eSelectNextPitPreset));
            voiceTriggeredMacros.Add("PREVIOUS_PIT_PRESET", new CommandMacro(audioPlayer, AudioPlayer.folderAcknowlegeOK, r3eSelectPreviousPitPreset));
            voiceTriggeredMacros.Add("CONFIRM_PIT_ACTIONS", new CommandMacro(audioPlayer, AudioPlayer.folderAcknowlegeOK, r3eConfirmPitActions));
            speechRecogniser.loadMacroVoiceTriggers(voiceTriggeredMacros);
        }
    }
}
