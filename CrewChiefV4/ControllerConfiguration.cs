using CrewChiefV4.PCars;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CrewChiefV4
{
    class ControllerConfiguration : IDisposable
    {
        private static Guid NETWORK_CONSOLE_CONTROLLER_GUID = new Guid("2bbfed03-a04f-4408-91cf-e0aa6b20b8ff");

        public Boolean listenForAssignment = false;
        DirectInput directInput = new DirectInput();
        DeviceType[] supportedDeviceTypes = new DeviceType[] {DeviceType.Driving, DeviceType.Joystick, DeviceType.Gamepad, 
            DeviceType.Keyboard, DeviceType.ControlDevice, DeviceType.FirstPerson, DeviceType.Flight, 
            DeviceType.Supplemental, DeviceType.Remote};
        public List<ButtonAssignment> buttonAssignments = new List<ButtonAssignment>();
        public List<ControllerData> controllers;

        public static String CHANNEL_OPEN_FUNCTION = "Talk to crew chief";
        public static String TOGGLE_RACE_UPDATES_FUNCTION = "Toggle race updates on/off"; 
        public static String TOGGLE_SPOTTER_FUNCTION = "Toggle spotter on/off";
        public static String TOGGLE_READ_OPPONENT_DELTAS = "Toggle opponent deltas on/off for each lap";
        public static String REPEAT_LAST_MESSAGE_BUTTON = "Press to replay the last message";

        private ControllerData networkGamePad = new ControllerData("Console controller", DeviceType.Gamepad, NETWORK_CONSOLE_CONTROLLER_GUID);
        
        // yuk...
        public Dictionary<String, int> buttonAssignmentIndexes = new Dictionary<String, int>();

        public void Dispose()
        {
            foreach (ButtonAssignment ba in buttonAssignments)
            {
                if (ba.joystick != null)
                {
                    try
                    {
                        ba.joystick.Unacquire();
                        ba.joystick.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            directInput.Dispose();
        }

        public ControllerConfiguration()
        {
            addButtonAssignment(CHANNEL_OPEN_FUNCTION);
            addButtonAssignment(TOGGLE_RACE_UPDATES_FUNCTION);
            addButtonAssignment(TOGGLE_SPOTTER_FUNCTION);
            addButtonAssignment(TOGGLE_READ_OPPONENT_DELTAS);
            addButtonAssignment(REPEAT_LAST_MESSAGE_BUTTON);
            controllers = getControllers();
        }

        public void addCustomController(Guid guid)
        {
            var joystick = new Joystick(directInput, guid);
            String productName = " custom device";
            try
            {
                productName = ": " + joystick.Properties.ProductName;
            }
            catch (Exception)
            {
            }
            controllers.Add(new ControllerData(productName, DeviceType.Joystick, guid));
        }

        public void pollForButtonClicks(Boolean channelOpenIsToggle)
        {
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_RACE_UPDATES_FUNCTION]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_SPOTTER_FUNCTION]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_READ_OPPONENT_DELTAS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[REPEAT_LAST_MESSAGE_BUTTON]]);
            if (channelOpenIsToggle) 
            {
                pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[CHANNEL_OPEN_FUNCTION]]);
            }
        }

        private void pollForButtonClicks(ButtonAssignment ba)
        {
            if (ba != null && ba.buttonIndex != -1)
            {
                if (ba.joystick != null)
                {
                    try
                    {
                        if (ba.joystick != null)
                        {
                            JoystickState state = ba.joystick.GetCurrentState();
                            if (state != null)
                            {
                                Boolean click = state.Buttons[ba.buttonIndex];
                                if (click)
                                {
                                    ba.hasUnprocessedClick = true;
                                }
                            }
                        }
                    }
                    catch (Exception)
                    {

                    }
                }
                else if (ba.controller.guid == NETWORK_CONSOLE_CONTROLLER_GUID)
                {
                    if (PCarsUDPreader.getButtonState(ba.buttonIndex))
                    {
                        ba.hasUnprocessedClick = true;
                    }
                }
            }
        }

        public Boolean hasOutstandingClick(String action)
        {
            ButtonAssignment ba = buttonAssignments[buttonAssignmentIndexes[action]];
            if (ba.hasUnprocessedClick)
            {
                ba.hasUnprocessedClick = false;
                return true;
            }
            return false;
        }
        
        public Boolean listenForChannelOpen()
        {
            foreach (ButtonAssignment buttonAssignment in buttonAssignments)
            {
                if (buttonAssignment.action == CHANNEL_OPEN_FUNCTION && buttonAssignment.buttonIndex != -1 
                    && (buttonAssignment.joystick != null || (buttonAssignment.controller != null && buttonAssignment.controller.guid == NETWORK_CONSOLE_CONTROLLER_GUID)))
                {
                    return true;
                }
            }
            return false;
        }

        public Boolean listenForButtons(Boolean channelOpenIsToggle)
        {
            foreach (ButtonAssignment buttonAssignment in buttonAssignments)
            {
                if ((channelOpenIsToggle || buttonAssignment.action != CHANNEL_OPEN_FUNCTION) &&
                    (buttonAssignment.joystick != null || (buttonAssignment.controller != null && buttonAssignment.controller.guid == NETWORK_CONSOLE_CONTROLLER_GUID)) 
                    && buttonAssignment.buttonIndex != -1)
                {
                    return true;
                }
            }
            return false;     
        }
        
        public void saveSettings()
        {
            foreach (ButtonAssignment buttonAssignment in buttonAssignments)
            {
                String actionId = "";
                if (buttonAssignment.action == CHANNEL_OPEN_FUNCTION)
                {
                    actionId = "CHANNEL_OPEN_FUNCTION";
                }
                else if (buttonAssignment.action == TOGGLE_RACE_UPDATES_FUNCTION)
                {
                    actionId = "TOGGLE_RACE_UPDATES_FUNCTION";
                }
                else if (buttonAssignment.action == TOGGLE_SPOTTER_FUNCTION)
                {
                    actionId = "TOGGLE_SPOTTER_FUNCTION";
                }
                else if (buttonAssignment.action == TOGGLE_READ_OPPONENT_DELTAS)
                {
                    actionId = "TOGGLE_READ_OPPONENT_DELTAS";
                }
                else if (buttonAssignment.action == REPEAT_LAST_MESSAGE_BUTTON)
                {
                    actionId = "REPEAT_LAST_MESSAGE_BUTTON";
                }

                if (buttonAssignment.controller != null && (buttonAssignment.joystick != null || buttonAssignment.controller.guid == NETWORK_CONSOLE_CONTROLLER_GUID) && buttonAssignment.buttonIndex != -1)
                {
                    UserSettings.GetUserSettings().setProperty(actionId + "_button_index", buttonAssignment.buttonIndex);
                    UserSettings.GetUserSettings().setProperty(actionId + "_device_guid", buttonAssignment.controller.guid.ToString());
                }
                else
                {
                    UserSettings.GetUserSettings().setProperty(actionId + "_button_index", -1);
                    UserSettings.GetUserSettings().setProperty(actionId + "_device_guid", "");
                }
            }
            UserSettings.GetUserSettings().saveUserSettings();
        }

        public void loadSettings(System.Windows.Forms.Form parent)
        {
            int channelOpenButtonIndex = UserSettings.GetUserSettings().getInt("CHANNEL_OPEN_FUNCTION_button_index");
            String channelOpenButtonDeviceGuid = UserSettings.GetUserSettings().getString("CHANNEL_OPEN_FUNCTION_device_guid");
            if (channelOpenButtonIndex != -1 && channelOpenButtonDeviceGuid.Length > 0)
            {
                loadAssignment(parent, CHANNEL_OPEN_FUNCTION, channelOpenButtonIndex, channelOpenButtonDeviceGuid);
            }

            int toggleRaceUpdatesButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_RACE_UPDATES_FUNCTION_button_index");
            String toggleRaceUpdatesButtonDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_RACE_UPDATES_FUNCTION_device_guid");
            if (toggleRaceUpdatesButtonIndex != -1 && toggleRaceUpdatesButtonDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_RACE_UPDATES_FUNCTION, toggleRaceUpdatesButtonIndex, toggleRaceUpdatesButtonDeviceGuid);
            }

            int toggleSpotterFunctionButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_SPOTTER_FUNCTION_button_index");
            String toggleSpotterFunctionDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_SPOTTER_FUNCTION_device_guid");
            if (toggleSpotterFunctionButtonIndex != -1 && toggleSpotterFunctionDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_SPOTTER_FUNCTION, toggleSpotterFunctionButtonIndex, toggleSpotterFunctionDeviceGuid);
            }

            int toggleReadOpponentDeltasButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_READ_OPPONENT_DELTAS_button_index");
            String toggleReadOpponentDeltasDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_READ_OPPONENT_DELTAS_device_guid");
            if (toggleReadOpponentDeltasButtonIndex != -1 && toggleReadOpponentDeltasDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_READ_OPPONENT_DELTAS, toggleReadOpponentDeltasButtonIndex, toggleReadOpponentDeltasDeviceGuid);
            }

            int repeatLastMessageButtonIndex = UserSettings.GetUserSettings().getInt("REPEAT_LAST_MESSAGE_BUTTON_button_index");
            String repeatLastMessageDeviceGuid = UserSettings.GetUserSettings().getString("REPEAT_LAST_MESSAGE_BUTTON_device_guid");
            if (repeatLastMessageButtonIndex != -1 && repeatLastMessageDeviceGuid.Length > 0)
            {
                loadAssignment(parent, REPEAT_LAST_MESSAGE_BUTTON, repeatLastMessageButtonIndex, repeatLastMessageDeviceGuid);
            }
        }

        private void loadAssignment(System.Windows.Forms.Form parent, String functionName, int buttonIndex, String deviceGuid)
        {
            if (deviceGuid == NETWORK_CONSOLE_CONTROLLER_GUID.ToString())
            {
                addNetworkControllerToList();
            }
            foreach (ControllerData controller in this.controllers)
            {                
                if (controller.guid.ToString() == deviceGuid)
                {
                    buttonAssignments[buttonAssignmentIndexes[functionName]].controller = controller;
                    buttonAssignments[buttonAssignmentIndexes[functionName]].buttonIndex = buttonIndex;
                    if (controller.guid != NETWORK_CONSOLE_CONTROLLER_GUID)
                    {
                        var joystick = new Joystick(directInput, controller.guid);
                        // Acquire the joystick
                        joystick.SetCooperativeLevel(parent, (CooperativeLevel.NonExclusive | CooperativeLevel.Background));
                        joystick.Properties.BufferSize = 128;
                        joystick.Acquire();
                        buttonAssignments[buttonAssignmentIndexes[functionName]].joystick = joystick;
                    }
                }
            }
        }

        private void addButtonAssignment(String action)
        {
            buttonAssignmentIndexes.Add(action, buttonAssignmentIndexes.Count());
            buttonAssignments.Add(new ButtonAssignment(action));
        }

        public Boolean isChannelOpen()
        {
            ButtonAssignment ba = buttonAssignments[buttonAssignmentIndexes[CHANNEL_OPEN_FUNCTION]];
            if (ba != null && ba.buttonIndex != -1 && ba.joystick != null)
            {
                try
                {
                    return ba.joystick.GetCurrentState().Buttons[ba.buttonIndex];
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to get button state for index " + ba.buttonIndex + " message: " + e.Message);
                }
            }
            return false;
        }
        
        private List<ControllerData> getControllers()
        {
            List<ControllerData> controllers = new List<ControllerData>();
            foreach (DeviceType deviceType in supportedDeviceTypes)
            {
                foreach (var deviceInstance in directInput.GetDevices(deviceType, DeviceEnumerationFlags.AllDevices))
                {
                    Guid joystickGuid = deviceInstance.InstanceGuid;
                    if (joystickGuid != Guid.Empty) 
                    {
                        var joystick = new Joystick(directInput, joystickGuid);
                        String productName = "";
                        try
                        {
                            productName = ": " + joystick.Properties.ProductName;
                        }
                        catch (Exception)
                        {

                        }
                        controllers.Add(new ControllerData(productName, deviceType, joystickGuid));
                    }
                }
            }
            return controllers;
        }

        public void addNetworkControllerToList()
        {
            if (!controllers.Contains(networkGamePad))
            {
                controllers.Add(networkGamePad);
            }
        }

        public void removeNetworkControllerFromList()
        {
            if (controllers.Contains(networkGamePad))
            {
                controllers.Remove(networkGamePad);
            }
        }

        public Boolean assignButton(System.Windows.Forms.Form parent, int controllerIndex, int actionIndex)
        {
            return getFirstPressedButton(parent, controllers[controllerIndex], buttonAssignments[actionIndex]);
        }

        private Boolean getFirstPressedButton(System.Windows.Forms.Form parent, ControllerData controllerData, ButtonAssignment buttonAssignment)
        {
            Boolean gotAssignment = false;
            if (controllerData.guid == NETWORK_CONSOLE_CONTROLLER_GUID)
            {
                PCarsUDPreader gameDataReader = (PCarsUDPreader)GameStateReaderFactory.getInstance().getGameStateReader(GameDefinition.pCarsNetwork);
                int assignedButton = gameDataReader.getButtonIndexForAssignment();
                if (assignedButton != -1)
                {
                    removeAssignmentsForControllerAndButton(controllerData.guid, assignedButton);
                    buttonAssignment.controller = controllerData;
                    buttonAssignment.buttonIndex = assignedButton;
                    listenForAssignment = false;
                    gotAssignment = true;
                }
            }
            else
            {
                listenForAssignment = true;
                // Instantiate the joystick
                var joystick = new Joystick(directInput, controllerData.guid);
                // Acquire the joystick
                joystick.SetCooperativeLevel(parent, (CooperativeLevel.NonExclusive | CooperativeLevel.Background));
                joystick.Properties.BufferSize = 128;
                joystick.Acquire();                
                while (listenForAssignment)
                {
                    Boolean[] buttons = joystick.GetCurrentState().Buttons;
                    for (int i = 0; i < buttons.Count(); i++)
                    {
                        if (buttons[i])
                        {
                            Console.WriteLine("Got button at index " + i);
                            removeAssignmentsForControllerAndButton(controllerData.guid, i);
                            buttonAssignment.controller = controllerData;
                            buttonAssignment.joystick = joystick;
                            buttonAssignment.buttonIndex = i;
                            listenForAssignment = false;
                            gotAssignment = true;
                        }
                    }
                }
                if (!gotAssignment)
                {
                    joystick.Unacquire();
                }
            }
            return gotAssignment;
        }

        private void removeAssignmentsForControllerAndButton(Guid controllerGuid, int buttonIndex)
        {
            foreach (ButtonAssignment ba in buttonAssignments)
            {
                if (ba.controller != null && ba.controller.guid == controllerGuid && ba.buttonIndex == buttonIndex)
                {
                    ba.controller = null;
                    ba.joystick = null; // unacquire here?
                    ba.buttonIndex = -1;
                }
            }
        }

        public class ControllerData
        {
            public String deviceName;
            public DeviceType deviceType;
            public Guid guid;

            public ControllerData(String deviceName, DeviceType deviceType, Guid guid)
            {
                this.deviceName = deviceName;
                this.deviceType = deviceType;
                this.guid = guid;
            }
        }

        public class ButtonAssignment
        {
            public String action;
            public ControllerData controller;
            public int buttonIndex = -1;
            public Joystick joystick;
            public Boolean hasUnprocessedClick = false;
            public ButtonAssignment(String action)
            {
                this.action = action;
            }
            
            public String getInfo()
            {
                if (controller != null && buttonIndex > -1)
                {
                    return action + " (assigned to " + controller.deviceName + ", button: " + buttonIndex + ")";
                }
                else
                {
                    return action + " (not assigned)";
                }
            }
            
            public void unassign()
            {
                this.controller = null;
                this.buttonIndex = -1;
                if (this.joystick != null)
                {
                    this.joystick.Unacquire();
                    this.joystick.SetNotification(null);
                }
                this.joystick = null;
            }
        }
    }
}
