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
        Dictionary<String, KeyBinding[]> assignmentsByGame;
        public Boolean allowAutomaticTriggering;
        public ExecutableCommandMacro(AudioPlayer audioPlayer, Macro macro, Dictionary<String, KeyBinding[]> assignmentsByGame, Boolean allowAutomaticTriggering)
        {
            this.audioPlayer = audioPlayer;
            this.macro = macro;
            this.assignmentsByGame = assignmentsByGame;
            this.allowAutomaticTriggering = allowAutomaticTriggering;
            if (allowAutomaticTriggering)
            {
                Console.WriteLine("Macro \"" + macro.name + "\" can be triggered automatically");
            }
        }
        public void execute()
        {
            execute(false);
        }

        public void execute(Boolean supressConfirmationMessage)
        {
            // blocking...
            foreach (CommandSet commandSet in macro.commandSets)
            {
                // only execute for the requested game - is this check sensible?
                if (CrewChief.gameDefinition.gameEnum.ToString().Equals(commandSet.gameDefinition) &&
                    assignmentsByGame.ContainsKey(commandSet.gameDefinition))
                {
                    if (macro.confirmationMessage != null && macro.confirmationMessage.Length > 0 && !supressConfirmationMessage)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(macro.confirmationMessage, 0, null));
                    }
                    new Thread(() =>
                    {
                        foreach (ActionItem actionItem in commandSet.getActionItems(true, assignmentsByGame[commandSet.gameDefinition]))
                        {
                            if (actionItem.pauseMillis > 0)
                            {
                                Thread.Sleep(actionItem.pauseMillis);
                            }
                            else
                            {
                                KeyPresser.SendScanCodeKeyPress(actionItem.keyCode, commandSet.keyPressTime);
                            }
                            Thread.Sleep(commandSet.waitBetweenEachCommand);
                        } 
                    }).Start();                                  
                    break;
                }
            }
        }
    }

    // JSON objects
    public class MacroContainer
    {
        public Assignment[] assignments { get; set; }
        public Macro[] macros { get; set; }
    }

    public class Assignment
    {
        public String description { get; set; }
        public String gameDefinition { get; set; }
        public KeyBinding[] keyBindings { get; set; }
    }

    public class KeyBinding
    {
        public String description { get; set; }
        public String action { get; set; }
        public String key { get; set; }
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
		public String[] actionSequence { get; set; }
		public int keyPressTime { get; set; }
        public int waitBetweenEachCommand { get; set; }
        public Boolean allowAutomaticTriggering { get; set; }

        private List<ActionItem> actionItems = null;

        public List<ActionItem> getActionItems(Boolean writeToConsole, KeyBinding[] keyBindings)
        {
            if (this.actionItems == null)
            {
                this.actionItems = new List<ActionItem>();
                foreach (String action in actionSequence)
                {
                    ActionItem actionItem = new ActionItem(action, keyBindings);
                    if (actionItem.parsedSuccessfully)
                    {
                        this.actionItems.Add(actionItem);
                    }
                }
            }
            if (writeToConsole)
            {
                Console.WriteLine("Sending actions " + String.Join(", ", actionSequence));
                Console.WriteLine("Pressing keys " + String.Join(", ", actionItems));
            }
            return actionItems;
        }
    }

    public class ActionItem
    {
        public Boolean parsedSuccessfully = false;
        public int pauseMillis = -1;
        public KeyPresser.KeyCode keyCode;
        public String actionText;
        public ActionItem(String action, KeyBinding[] keyBindings)
        {
            this.actionText = action;
            if (action.StartsWith("WAIT_"))
            {
                pauseMillis = int.Parse(action.Substring(5));
                parsedSuccessfully = pauseMillis > 0;
            }
            else
            {
                try
                {
                    foreach (KeyBinding keyBinding in keyBindings)
                    {
                        if (String.Equals(keyBinding.action, action, StringComparison.InvariantCultureIgnoreCase))
                        {
                            keyCode = (KeyPresser.KeyCode)Enum.Parse(typeof(KeyPresser.KeyCode), keyBinding.key, true);
                            parsedSuccessfully = true;
                            break;
                        }
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Action " + action + " not recognised");
                }
            }
        }

        public override String ToString()
        {
            if (parsedSuccessfully)
            {
                if (pauseMillis > 0)
                {
                    return "Pause " + pauseMillis + " milliseconds";
                }
                else
                {
                    return keyCode.ToString();
                }
            }
            else
            {
                return "unable to parse action " + actionText;
            }
        }
    }

    public class ButtonTrigger
    {
        public String description { get; set; }
        public String deviceId { get; set; }
        public int buttonIndex { get; set; }
    }
}
