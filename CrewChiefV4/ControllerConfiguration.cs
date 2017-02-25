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
        private static Guid UDP_NETWORK_CONTROLLER_GUID = new Guid("2bbfed03-a04f-4408-91cf-e0aa6b20b8ff");

        public Boolean listenForAssignment = false;
        DirectInput directInput = new DirectInput();
        DeviceType[] supportedDeviceTypes = new DeviceType[] {DeviceType.Driving, DeviceType.Joystick, DeviceType.Gamepad, 
            DeviceType.Keyboard, DeviceType.ControlDevice, DeviceType.FirstPerson, DeviceType.Flight, 
            DeviceType.Supplemental, DeviceType.Remote};
        public List<ButtonAssignment> buttonAssignments = new List<ButtonAssignment>();
        public List<ControllerData> controllers;

        public static String CHANNEL_OPEN_FUNCTION = Configuration.getUIString("talk_to_crew_chief");
        public static String TOGGLE_RACE_UPDATES_FUNCTION = Configuration.getUIString("toggle_race_updates_on/off");
        public static String TOGGLE_SPOTTER_FUNCTION = Configuration.getUIString("toggle_spotter_on/off");
        public static String TOGGLE_READ_OPPONENT_DELTAS = Configuration.getUIString("toggle_opponent_deltas_on/off_for_each_lap");
        public static String REPEAT_LAST_MESSAGE_BUTTON = Configuration.getUIString("press_to_replay_the_last_message");
        public static String VOLUME_UP = Configuration.getUIString("volume_up");
        public static String VOLUME_DOWN = Configuration.getUIString("volume_down");        

        private ControllerData networkGamePad = new ControllerData(Configuration.getUIString("udp_network_data_buttons"), DeviceType.Gamepad, UDP_NETWORK_CONTROLLER_GUID);
        
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
            try
            {
                directInput.Dispose();
            }
            catch (Exception) { }
        }

        public ControllerConfiguration()
        {
            addButtonAssignment(CHANNEL_OPEN_FUNCTION);
            addButtonAssignment(TOGGLE_RACE_UPDATES_FUNCTION);
            addButtonAssignment(TOGGLE_SPOTTER_FUNCTION);
            addButtonAssignment(TOGGLE_READ_OPPONENT_DELTAS);
            addButtonAssignment(REPEAT_LAST_MESSAGE_BUTTON);
            addButtonAssignment(VOLUME_UP);
            addButtonAssignment(VOLUME_DOWN);
            controllers = loadControllers();
        }

        public void addCustomController(Guid guid)
        {
            var joystick = new Joystick(directInput, guid);
            String productName = " " + Configuration.getUIString("custom_device");
            try
            {
                productName = ": " + joystick.Properties.ProductName;
            }
            catch (Exception)
            {
            }
            asyncDispose(DeviceType.ControlDevice, joystick);
            controllers.Add(new ControllerData(productName, DeviceType.Joystick, guid));
        }

        public void pollForButtonClicks(Boolean channelOpenIsToggle)
        {
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_RACE_UPDATES_FUNCTION]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_SPOTTER_FUNCTION]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_READ_OPPONENT_DELTAS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[REPEAT_LAST_MESSAGE_BUTTON]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[VOLUME_UP]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[VOLUME_DOWN]]);
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
                else if (ba.controller.guid == UDP_NETWORK_CONTROLLER_GUID)
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
                    && (buttonAssignment.joystick != null || (buttonAssignment.controller != null && buttonAssignment.controller.guid == UDP_NETWORK_CONTROLLER_GUID)))
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
                    (buttonAssignment.joystick != null || (buttonAssignment.controller != null && buttonAssignment.controller.guid == UDP_NETWORK_CONTROLLER_GUID)) 
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
                else if (buttonAssignment.action == VOLUME_UP)
                {
                    actionId = "VOLUME_UP";
                }
                else if (buttonAssignment.action == VOLUME_DOWN)
                {
                    actionId = "VOLUME_DOWN";
                }

                if (buttonAssignment.controller != null && (buttonAssignment.joystick != null || buttonAssignment.controller.guid == UDP_NETWORK_CONTROLLER_GUID) && buttonAssignment.buttonIndex != -1)
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

            int volumeUpButtonIndex = UserSettings.GetUserSettings().getInt("VOLUME_UP_button_index");
            String volumeUpDeviceGuid = UserSettings.GetUserSettings().getString("VOLUME_UP_device_guid");
            if (volumeUpButtonIndex != -1 && volumeUpDeviceGuid.Length > 0)
            {
                loadAssignment(parent, VOLUME_UP, volumeUpButtonIndex, volumeUpDeviceGuid);
            }

            int volumeDownButtonIndex = UserSettings.GetUserSettings().getInt("VOLUME_DOWN_button_index");
            String volumeDownDeviceGuid = UserSettings.GetUserSettings().getString("VOLUME_DOWN_device_guid");
            if (volumeDownButtonIndex != -1 && volumeDownDeviceGuid.Length > 0)
            {
                loadAssignment(parent, VOLUME_DOWN, volumeDownButtonIndex, volumeDownDeviceGuid);
            }
        }

        private void loadAssignment(System.Windows.Forms.Form parent, String functionName, int buttonIndex, String deviceGuid)
        {
            if (deviceGuid == UDP_NETWORK_CONTROLLER_GUID.ToString())
            {
                addNetworkControllerToList();
            }
            foreach (ControllerData controller in this.controllers)
            {                
                if (controller.guid.ToString() == deviceGuid)
                {
                    buttonAssignments[buttonAssignmentIndexes[functionName]].controller = controller;
                    buttonAssignments[buttonAssignmentIndexes[functionName]].buttonIndex = buttonIndex;
                    if (controller.guid != UDP_NETWORK_CONTROLLER_GUID)
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
            if (ba != null && ba.buttonIndex != -1)
            {
                if (ba.joystick != null)
                {
                    try
                    {
                        return ba.joystick.GetCurrentState().Buttons[ba.buttonIndex];
                    }
                    catch (Exception e)
                    {
                        // ignore this exception
                    }
                } else if (ba.controller.guid == UDP_NETWORK_CONTROLLER_GUID)
                {
                    return PCarsUDPreader.getButtonState(ba.buttonIndex);
                }
            }
            return false;
        }

        public List<ControllerData> loadControllers()
        {
            return ControllerData.parse(UserSettings.GetUserSettings().getString(ControllerData.PROPERTY_CONTAINER));
        }
        
        public List<ControllerData> scanControllers()
        {
            List<ControllerData> controllers = new List<ControllerData>(); 
            foreach (DeviceType deviceType in supportedDeviceTypes)
            {
                foreach (var deviceInstance in directInput.GetDevices(deviceType, DeviceEnumerationFlags.AllDevices))
                {
                    Guid joystickGuid = deviceInstance.InstanceGuid;
                    if (joystickGuid != Guid.Empty) 
                    {
                        try
                        {
                            var joystick = new Joystick(directInput, joystickGuid);
                            String productName = "";
                            try
                            {
                                productName = ": " + joystick.Properties.ProductName;
                            }
                            catch (Exception)
                            {
                                // ignore - some devices don't have a product name
                            }
                            asyncDispose(deviceType, joystick);
                            controllers.Add(new ControllerData(productName, deviceType, joystickGuid));
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("failed to get device info: " + e.Message);
                        }                        
                    }
                }
            }
            String propVal = ControllerData.createPropValue(controllers);
            UserSettings.GetUserSettings().setProperty(ControllerData.PROPERTY_CONTAINER, propVal);
            UserSettings.GetUserSettings().saveUserSettings();
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
            if (controllerData.guid == UDP_NETWORK_CONTROLLER_GUID)
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

        private void asyncDispose(DeviceType deviceType, Joystick joystick)
        {
            new Thread(() =>
            {                
                DateTime now = DateTime.Now;
                Thread.CurrentThread.IsBackground = true;
                String name = joystick.Information.InstanceName;
                try
                {                    
                    joystick.Dispose();
                    //Console.WriteLine("Disposed of temporary " + deviceType + " object " + name + " after " + (DateTime.Now - now).TotalSeconds + " seconds");
                }
                catch (Exception e) { 
                    //log and swallow 
                    Console.WriteLine("Failed to dispose of temporary " + deviceType + " object " + name + "after " + (DateTime.Now - now).TotalSeconds + " seconds: " + e.Message);
                }
            }).Start();
        }

        public class ControllerData
        {
            public static String PROPERTY_CONTAINER = "CONTROLLER_DATA";

            public static String definitionSeparator = "CC_CD_SEPARATOR";
            public static String elementSeparator = "CC_CE_SEPARATOR";

            public String deviceName;
            public DeviceType deviceType;
            public Guid guid;

            public static List<ControllerData> parse(String propValue)
            {
                List<ControllerData> definitionsList = new List<ControllerData>();

                if (propValue != null && propValue.Length > 0)
                {
                    String[] definitions = propValue.Split(new string[] { definitionSeparator }, StringSplitOptions.None);
                    foreach (String definition in definitions)
                    {
                        if (definition != null && definition.Length > 0)
                        {
                            try
                            {
                                String[] elements = definition.Split(new string[] { elementSeparator }, StringSplitOptions.None);
                                if (elements.Length == 3)
                                {
                                    definitionsList.Add(new ControllerData(elements[0], (DeviceType)System.Enum.Parse(typeof(DeviceType), elements[1]), new Guid(elements[2])));
                                }
                            }
                            catch (Exception)
                            {

                            }
                        }
                    }
                }
                return definitionsList;
            }

            public static String createPropValue(List<ControllerData> definitions)
            {
                StringBuilder propVal = new StringBuilder();
                foreach (ControllerData def in definitions)
                {
                    propVal.Append(def.deviceName).Append(elementSeparator).Append(def.deviceType.ToString()).Append(elementSeparator).
                            Append(def.guid.ToString()).Append(definitionSeparator);
                }
                return propVal.ToString();
            }

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
                    String name = controller.deviceName == null || controller.deviceName.Length == 0 ? controller.deviceType.ToString() : controller.deviceName;
                    return action + " " + Configuration.getUIString("assigned_to") + " " + name + ", " + Configuration.getUIString("button") + ": " + buttonIndex;
                }
                else
                {
                    return action + " " + Configuration.getUIString("not_assigned");
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
