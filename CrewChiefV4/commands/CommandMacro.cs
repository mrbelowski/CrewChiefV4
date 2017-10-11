using CrewChiefV4.Audio;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CrewChiefV4.commands
{
    // wrapper that actually runs the macro
    public class ExecutableCommandMacro
    {
        AudioPlayer audioPlayer;
        Macro macro;
        public ExecutableCommandMacro(AudioPlayer audioPlayer, Macro macro)
        {
            this.audioPlayer = audioPlayer;
            this.macro = macro;
        }
        public void execute()
        {
            // blocking...
            foreach (CommandSet commandSet in macro.commandSets)
            {
                // only execute for the requested game - is this check sensible?
                if (CrewChief.gameDefinition.gameEnum.ToString().Equals(commandSet.gameDefinition))
                {
                    foreach (KeyPresser.KeyCode keyCode in commandSet.getKeyCodes())
                    {
                        KeyPresser.SendScanCodeKeyPress(keyCode, commandSet.keyPressTime);
                        Thread.Sleep(commandSet.waitBetweenEachCommand);
                    }
                    if (macro.confirmationMessage != null && macro.confirmationMessage.Length > 0)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(macro.confirmationMessage, 0, null));
                    }
                    break;
                }
            }
        }
    }

    // JSON objects
    public class MacroContainer
    {
        public Macro[] macros { get; set; }
    }

    public class Macro
    {
        public String name { get; set; }
		public String description { get; set; }
        public String confirmationMessage { get; set; }
		public String[] voiceTriggers { get; set; }
        public ButtonTrigger[] buttonTriggers { get; set; }
        public CommandSet[] commandSets { get; set; }
    }

    public class CommandSet
    {
        public String description { get; set; }
        public String gameDefinition { get; set; }
		public String[] keyPressSequence { get; set; }
		public int keyPressTime { get; set; }
        public int waitBetweenEachCommand { get; set; }

        private List<KeyPresser.KeyCode> codes = null;

        public List<KeyPresser.KeyCode> getKeyCodes()
        {
            if (this.codes == null)
            {
                this.codes = new List<KeyPresser.KeyCode>();
                foreach (String key in keyPressSequence)
                {
                    try
                    {
                        codes.Add((KeyPresser.KeyCode)Enum.Parse(typeof(KeyPresser.KeyCode), key, true));
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Key " + key + " not recognised");
                    }
                }
            }
            return codes;
        }
    }

    public class ButtonTrigger
    {
        public String description { get; set; }
        public String deviceId { get; set; }
        public int buttonIndex { get; set; }
    }
}
