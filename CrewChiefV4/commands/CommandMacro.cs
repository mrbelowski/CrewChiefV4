﻿using CrewChiefV4.Audio;
using CrewChiefV4.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;

namespace CrewChiefV4.commands
{
    // wrapper that actually runs the macro
    public class ExecutableCommandMacro
    {
        private static Object mutex = new Object();

        AudioPlayer audioPlayer;
        public Macro macro;
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

        public void execute(String recognitionResult)
        {           
            execute(recognitionResult, false);
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

        public void execute(String recognitionResult, Boolean supressConfirmationMessage)
        {
            // blocking...
            Boolean isPCars2 = CrewChief.gameDefinition == GameDefinition.pCars2;
            Boolean isR3e = CrewChief.gameDefinition == GameDefinition.raceRoom;
            int multiplePressCountFromVoiceCommand = 0;
            if (macro.integerVariableVoiceTrigger != null && macro.integerVariableVoiceTrigger.Length > 0)
            {
                multiplePressCountFromVoiceCommand = macro.extractInt(recognitionResult, macro.startPhrase, macro.endPhrase);
            }
            foreach (CommandSet commandSet in macro.commandSets)
            {
                // only execute for the requested game - is this check sensible?
                if (CrewChief.gameDefinition.gameEnum.ToString().Equals(commandSet.gameDefinition) &&
                    assignmentsByGame.ContainsKey(commandSet.gameDefinition))
                {
                    Boolean isValid = checkValidAndPlayConfirmation(commandSet, supressConfirmationMessage);
                    if (isValid)
                    {
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
                                        if (MacroManager.MULTIPLE_PRESS_IDENTIFIER.Equals(actionItem.extendedType))
                                        {
                                            int count = 0;
                                            if (actionItem.resolvedByEvent != null)
                                            {
                                                if (MacroManager.MULTIPLE_PRESS_FROM_VOICE_TRIGGER_IDENTIFIER.Equals(actionItem.resolvedByEvent))
                                                {
                                                    count = multiplePressCountFromVoiceCommand;
                                                }
                                                else
                                                {
                                                    count = CrewChief.getEvent(actionItem.resolvedByEvent).resolveMacroKeyPressCount(macro.name);
                                                }
                                            }
                                            else
                                            {
                                                count = actionItem.pressCount;
                                            }
                                            // completely arbitrary sanity check on resolved count. We don't want the app trying to press 'right' MaxInt times
                                            if (count > 0 && count < 300)
                                            {
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

        private String _integerVariableVoiceTrigger;
        public String integerVariableVoiceTrigger
        {
            get { return _integerVariableVoiceTrigger; }
            set
            {
                this._integerVariableVoiceTrigger = value;
                parseIntRangeAndPhrase();
            }
        }
        public Tuple<int, int> intRange;
        public String startPhrase;
        public String endPhrase;

        public String getIntegerVariableVoiceTrigger()
        {
            return this._integerVariableVoiceTrigger;
        }

        public int extractInt(String recognisedVoiceCommand, String start, String end)
        {
            foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.numberToNumber)
            {
                foreach (String numberStr in entry.Key)
                {
                    if (recognisedVoiceCommand.Contains(start + numberStr + end))
                    {
                        return entry.Value;
                    }
                }
            }
            return 0;
        }

        private void parseIntRangeAndPhrase()
        {
            try
            {
                Boolean success = false;
                int start = this._integerVariableVoiceTrigger.IndexOf("{") + 1;
                int end = this._integerVariableVoiceTrigger.IndexOf("}", start);
                if (start != -1 && end > -1)
                {
                    String[] range = this._integerVariableVoiceTrigger.Substring(start, end - start).Split(',');
                    if (range.Length == 2)
                    {
                        this.startPhrase = this._integerVariableVoiceTrigger.Substring(0, this._integerVariableVoiceTrigger.IndexOf("{"));
                        this.endPhrase = this._integerVariableVoiceTrigger.Substring(this._integerVariableVoiceTrigger.IndexOf("}") + 1);
                        this.intRange = new Tuple<int, int>(int.Parse(range[0]), int.Parse(range[1]));
                        success = true;
                    }
                }
                if (!success)
                {
                    Console.WriteLine("Failed to parse range and phrase from voice trigger " + this._integerVariableVoiceTrigger + " in macro " + this.name);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing range and phrase from voice trigger " + this._integerVariableVoiceTrigger + " in macro " + this.name + ", " + e.StackTrace);
            }
        }
    }

    public class CommandSet
    {
        public String description { get; set; }
        public String gameDefinition { get; set; }
		public String[] actionSequence { get; set; }
		public int keyPressTime { get; set; }
        public int waitBetweenEachCommand { get; set; }
        public Boolean allowAutomaticTriggering { get; set; }
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
        public String extendedType;
        public String resolvedByEvent;
        public int pressCount;
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
                if (action.StartsWith("{"))
                {
                    int start = action.IndexOf("{") + 1;
                    int end = action.IndexOf("}", start);
                    if (start != -1 && end > -1)
                    {
                        String[] typeAndParam = action.Substring(start, end - start).Split(',');
                        if (typeAndParam.Length == 2)
                        {
                            extendedType = typeAndParam[0];
                            if (typeAndParam[1].All(char.IsDigit))
                            {
                                pressCount = int.Parse(typeAndParam[1]);
                            }
                            else
                            {
                                resolvedByEvent = typeAndParam[1];
                            }
                        }
                    }
                    action = action.Substring(action.IndexOf("}") + 1);
                }
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
