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
        private static Object mutex = new Object();

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
            this.allowAutomaticTriggering = allowAutomaticTriggering && MacroManager.enableAutoTriggering;
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
            if (!MacroManager.bringGameWindowToFrontForMacros)
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

        private Boolean checkValidAndPlayConfirmation(CommandSet commandSet, Boolean supressConfirmationMessage)
        {
            Boolean isValid = true;
            String macroConfirmationMessage = macro.confirmationMessage != null && macro.confirmationMessage.Length > 0 && !supressConfirmationMessage ? 
                macro.confirmationMessage : null;
            String commandConfirmationMessage = commandSet.confirmationMessage != null && commandSet.confirmationMessage.Length > 0 && !supressConfirmationMessage ? 
                commandSet.confirmationMessage : null;

            // special case for 'request pit' macro - check we've not already requested a stop, and we might want to play the pitstop strategy estimate
            if (macro.name == MacroManager.REQUEST_PIT_IDENTIFIER)
            {
                // if there's a confirmation message set up here, suppress the PitStops event from triggering the same message when the pit request changes in the gamestate
                PitStops.playedRequestPitOnThisLap = macroConfirmationMessage != null || commandConfirmationMessage != null;
                if ((CrewChief.gameDefinition == GameDefinition.pCars2 || CrewChief.gameDefinition == GameDefinition.rfactor2_64bit) &&
                     CrewChief.currentGameState != null && CrewChief.currentGameState.PitData.HasRequestedPitStop)
                {
                    // we've already requested a stop, so change the confirm message to 'yeah yeah, we know'
                    if (macroConfirmationMessage != null)
                    {
                        macroConfirmationMessage = PitStops.folderPitAlreadyRequested;
                    }
                    else if (commandConfirmationMessage != null)
                    {
                        commandConfirmationMessage = PitStops.folderPitAlreadyRequested;
                    }
                    isValid = false;
                }
                if (isValid && MacroManager.enablePitExitPositionEstimates)
                {
                    Strategy.playPitPositionEstimates = true;
                }
            }
            // special case for 'cancel pit request' macro - check we've actually requested a stop
            else if (macro.name == MacroManager.CANCEL_REQUEST_PIT_IDENTIFIER)
            {
                // if there's a confirmation message set up here, suppress the PitStops event from triggering the same message when the pit request changes in the gamestate
                PitStops.playedPitRequestCancelledOnThisLap = macroConfirmationMessage != null || commandConfirmationMessage != null;
                if ((CrewChief.gameDefinition == GameDefinition.pCars2 || CrewChief.gameDefinition == GameDefinition.rfactor2_64bit) &&
                     CrewChief.currentGameState != null && !CrewChief.currentGameState.PitData.HasRequestedPitStop)
                {
                    // we don't have a stop requested, so change the confirm message to 'what? we weren't waiting anyway'
                    if (macroConfirmationMessage != null)
                    {
                        macroConfirmationMessage = PitStops.folderPitNotRequested;
                    }
                    else if (commandConfirmationMessage != null)
                    {
                        commandConfirmationMessage = PitStops.folderPitNotRequested;
                    }
                    isValid = false;
                } 
            }
            if (macroConfirmationMessage != null)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(macroConfirmationMessage, 0, null));
            }
            else if (commandConfirmationMessage != null)
            {
                audioPlayer.playMessageImmediately(new QueuedMessage(commandConfirmationMessage, 0, null));
            }
            return isValid;
        }

        public void execute(Boolean supressConfirmationMessage)
        {
            // blocking...
            Boolean isPCars2 = CrewChief.gameDefinition == GameDefinition.pCars2;
            Boolean isR3e = CrewChief.gameDefinition == GameDefinition.raceRoom;
            foreach (CommandSet commandSet in macro.commandSets)
            {
                // only execute for the requested game - is this check sensible?
                if (CrewChief.gameDefinition.gameEnum.ToString().Equals(commandSet.gameDefinition) &&
                    assignmentsByGame.ContainsKey(commandSet.gameDefinition))
                {
                    Boolean isValid = checkValidAndPlayConfirmation(commandSet, supressConfirmationMessage);
                    if (isValid)
                    {
                        if (isR3e && macro.name.EndsWith(MacroManager.R3E_STRAT_IDENTIFIER))
                        {
                            // we've changed strategy, so nuke the last-added-fuel amount
                            MacroManager.clearState();
                        }
                        else if (isPCars2 && macro.name.StartsWith(MacroManager.PCARS2_STRAT_IDENTIFIER))
                        {
                            MacroManager.activePCars2Strategy = macro.name;
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
                                        if (actionItem.actionText.StartsWith(MacroManager.MULTIPLE_IDENTIFIER))
                                        {
                                            AbstractEvent eventToCall = CrewChief.getEvent(commandSet.resolveMultipleCountWithEvent);
                                            if (eventToCall != null)
                                            {
                                                int count = 0;
                                                if (macro.name == MacroManager.AUTO_FUEL_IDENTIFIER)
                                                {
                                                    // special case for fuelling. There are 2 multiple presses - decrease, to get the menu to the start,
                                                    // and increase to add the fuel

                                                    // first reset the fuelling
                                                    if (actionItem.actionText.EndsWith(MacroManager.MULTIPLE_LEFT_IDENTIFIER) || actionItem.actionText.EndsWith(MacroManager.MULTIPLE_DECREASE_IDENTIFIER))
                                                    {
                                                        int resetCount = 0;
                                                        if (isPCars2)
                                                        {
                                                            if (MacroManager.pcars2LastFuelAmountAddedByStrat.ContainsKey(MacroManager.activePCars2Strategy))
                                                            {
                                                                resetCount = MacroManager.pcars2LastFuelAmountAddedByStrat[MacroManager.activePCars2Strategy];
                                                            }
                                                        }
                                                        else if (isR3e)
                                                        {
                                                            resetCount = MacroManager.r3eLastFuelAmountAddedToThisStrat + 3;
                                                        }
                                                        for (int i = 0; i < resetCount; i++)
                                                        {
                                                            if (MacroManager.stopped)
                                                            {
                                                                break;
                                                            }
                                                            // play these quickly
                                                            KeyPresser.SendScanCodeKeyPress(actionItem.keyCode, 10);
                                                            Thread.Sleep(40);
                                                        }
                                                    }
                                                    else
                                                    {
                                                        count = eventToCall.resolveMacroKeyPressCount(macro.name);
                                                        if (isPCars2)
                                                        {
                                                            MacroManager.pcars2LastFuelAmountAddedByStrat[MacroManager.activePCars2Strategy] = count;
                                                        }
                                                        else if (isR3e)
                                                        {
                                                            MacroManager.r3eLastFuelAmountAddedToThisStrat = count;
                                                            count = count + 3;
                                                        }
                                                    }
                                                }
                                                else
                                                {
                                                    count = eventToCall.resolveMacroKeyPressCount(macro.name);
                                                }
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
                    }
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
