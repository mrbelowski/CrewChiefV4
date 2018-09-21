using CrewChiefV4.Audio;
using CrewChiefV4.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;

namespace CrewChiefV4.commands
{
    // wrapper that actually runs the macro
    public class ExecutableCommandMacro
    {
        // a magic string...
        private static readonly String MULTIPLE_IDENTIFIER = "Multiple";

        // another magic string...
        private static readonly String REQUEST_PIT_IDENTIFIER = "request pit";

        private Boolean enablePitExitPositionEstimates = UserSettings.GetUserSettings().getBoolean("enable_pit_exit_position_estimates");

        private static Object mutex = new Object();

        Boolean bringGameWindowToFrontForMacros = UserSettings.GetUserSettings().getBoolean("bring_game_window_to_front_for_macros");
        Boolean enableAutoTriggering = UserSettings.GetUserSettings().getBoolean("allow_macros_to_trigger_automatically");

        AudioPlayer audioPlayer;
        Macro macro;
        Dictionary<String, KeyBinding[]> assignmentsByGame;
        public Boolean allowAutomaticTriggering;
        private Thread executableCommandMacroThread = null;

        public ExecutableCommandMacro(AudioPlayer audioPlayer, Macro macro, Dictionary<String, KeyBinding[]> assignmentsByGame, Boolean allowAutomaticTriggering)
        {
            this.audioPlayer = audioPlayer;
            this.macro = macro;
            this.assignmentsByGame = assignmentsByGame;
            this.allowAutomaticTriggering = allowAutomaticTriggering && enableAutoTriggering;
            if (allowAutomaticTriggering)
            {
                Console.WriteLine("Macro \"" + macro.name + "\" can be triggered automatically");
            }
        }
        [DllImport("user32.dll")]
        static extern bool SetForegroundWindow(IntPtr hWnd);
        [DllImport("user32.dll")]
        static extern IntPtr GetForegroundWindow();

        bool BringGameWindowToFront(String processName, String[] alternateProcessNames, IntPtr currentForgroundWindow)
        {
            if (!bringGameWindowToFrontForMacros)
            {
                return false;
            }
            Process[] p = Process.GetProcessesByName(processName);
            if (p.Count() > 0)
            {
                if (p[0].MainWindowHandle != currentForgroundWindow)
                {
                    SetForegroundWindow(p[0].MainWindowHandle);
                    return true;
                }               
            }                
            else if (alternateProcessNames != null && alternateProcessNames.Length > 0)
            {
                foreach (String alternateProcessName in alternateProcessNames)
                {
                    p = Process.GetProcessesByName(processName);
                    if (p.Count() > 0)
                    {
                        if (p[0].MainWindowHandle != currentForgroundWindow)
                        {
                            SetForegroundWindow(p[0].MainWindowHandle);
                            return true;
                        }                       
                    } 
                }
            }
            return false;
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
                    else if (commandSet.confirmationMessage != null && commandSet.confirmationMessage.Length > 0 && !supressConfirmationMessage)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(commandSet.confirmationMessage, 0, null));
                    }
                    // special case for 'request pit' macro - we might want to play the pitstop strategy estimate
                    if (macro.name == REQUEST_PIT_IDENTIFIER && enablePitExitPositionEstimates)
                    {
                        Strategy.playPitPositionEstimates = true;
                    }
                    ThreadManager.UnregisterTemporaryThread(executableCommandMacroThread);
                    executableCommandMacroThread = new Thread(() =>
                    {
                        // only allow macros to excute one at a time
                        lock (ExecutableCommandMacro.mutex)
                        {
                            IntPtr currentForgroundWindow = GetForegroundWindow();
                            bool hasChangedForgroundWindow = BringGameWindowToFront(CrewChief.gameDefinition.processName, CrewChief.gameDefinition.alternativeProcessNames, currentForgroundWindow);

                            foreach (ActionItem actionItem in commandSet.getActionItems(true, assignmentsByGame[commandSet.gameDefinition]))
                            {
                                if (MacroManager.stopped)
                                {
                                    break;
                                }
                                if (actionItem.pauseMillis > 0)
                                {                                   
                                    Thread.Sleep(actionItem.pauseMillis);
                                }
                                else
                                {
                                    if (actionItem.actionText.StartsWith(MULTIPLE_IDENTIFIER))
                                    {
                                        AbstractEvent eventToCall = CrewChief.getEvent(commandSet.resolveMultipleCountWithEvent);
                                        if (eventToCall != null)
                                        {
                                            int count = eventToCall.resolveMacroKeyPressCount(macro.name);
                                            for (int i = 0; i < count; i++)
                                            {
                                                if (MacroManager.stopped)
                                                {
                                                    break;
                                                }
                                                KeyPresser.SendScanCodeKeyPress(actionItem.keyCode, commandSet.keyPressTime);
                                                Thread.Sleep(commandSet.waitBetweenEachCommand);
                                            }
                                        }
                                    }
                                    else
                                    {
                                        KeyPresser.SendScanCodeKeyPress(actionItem.keyCode, commandSet.keyPressTime);
                                        Thread.Sleep(commandSet.waitBetweenEachCommand);
                                    }
                                }
                            }
                            if (hasChangedForgroundWindow)
                            {
                                SetForegroundWindow(currentForgroundWindow);
                            }
                        }
                    });
                    executableCommandMacroThread.Name = "CommandMacro.executableCommandMacroThread";
                    ThreadManager.RegisterTemporaryThread(executableCommandMacroThread);
                    executableCommandMacroThread.Start();
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
        public String resolveMultipleCountWithEvent { get; set; }
        public String confirmationMessage { get; set; }

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
