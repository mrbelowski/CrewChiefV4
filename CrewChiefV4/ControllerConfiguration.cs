using CrewChiefV4.PCars;
using CrewChiefV4.PCars2;
using SharpDX.DirectInput;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;

namespace CrewChiefV4
{
    public class ControllerConfiguration : IDisposable
    {
        private static Guid UDP_NETWORK_CONTROLLER_GUID = new Guid("2bbfed03-a04f-4408-91cf-e0aa6b20b8ff");

        private MainWindow mainWindow;
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
        public static String PRINT_TRACK_DATA = Configuration.getUIString("print_track_data");
        public static String TOGGLE_YELLOW_FLAG_MESSAGES = Configuration.getUIString("toggle_yellow_flag_messages");
        public static String GET_FUEL_STATUS = Configuration.getUIString("get_fuel_status");
        public static String TOGGLE_MANUAL_FORMATION_LAP = Configuration.getUIString("toggle_manual_formation_lap");
        public static String READ_CORNER_NAMES_FOR_LAP = Configuration.getUIString("read_corner_names_for_lap");

        public static String GET_CAR_STATUS = Configuration.getUIString("get_car_status");
        public static String GET_STATUS = Configuration.getUIString("get_status");
        public static String GET_SESSION_STATUS = Configuration.getUIString("get_session_status");
        public static String GET_DAMAGE_REPORT = Configuration.getUIString("get_damage_report");
                
        public static String TOGGLE_PACE_NOTES_RECORDING = Configuration.getUIString("toggle_pace_notes_recording");
        public static String TOGGLE_PACE_NOTES_PLAYBACK = Configuration.getUIString("toggle_pace_notes_playback");

        public static String TOGGLE_TRACK_LANDMARKS_RECORDING = Configuration.getUIString("toggle_track_landmarks_recording");
        public static String ADD_TRACK_LANDMARK = Configuration.getUIString("add_track_landmark");

        public static String PIT_PREDICTION = Configuration.getUIString("activate_pit_prediction");

        public static String TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS = Configuration.getUIString("toggle_delay_messages_in_hard_parts");

        private ControllerData networkGamePad = new ControllerData(Configuration.getUIString("udp_network_data_buttons"), DeviceType.Gamepad, UDP_NETWORK_CONTROLLER_GUID);
        
        // yuk...
        public Dictionary<String, int> buttonAssignmentIndexes = new Dictionary<String, int>();
        private Thread asyncDisposeThread = null;

        public void Dispose()
        {
            mainWindow = null;
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

        public ControllerConfiguration(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            addButtonAssignment(CHANNEL_OPEN_FUNCTION);
            addButtonAssignment(TOGGLE_RACE_UPDATES_FUNCTION);
            addButtonAssignment(TOGGLE_SPOTTER_FUNCTION);
            addButtonAssignment(TOGGLE_READ_OPPONENT_DELTAS);
            addButtonAssignment(TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS);
            addButtonAssignment(REPEAT_LAST_MESSAGE_BUTTON);
            addButtonAssignment(VOLUME_UP);
            addButtonAssignment(VOLUME_DOWN);
            addButtonAssignment(TOGGLE_YELLOW_FLAG_MESSAGES);
            addButtonAssignment(GET_FUEL_STATUS);
            addButtonAssignment(TOGGLE_MANUAL_FORMATION_LAP);
            addButtonAssignment(PRINT_TRACK_DATA);
            addButtonAssignment(READ_CORNER_NAMES_FOR_LAP);
            addButtonAssignment(GET_CAR_STATUS);
            addButtonAssignment(GET_DAMAGE_REPORT);
            addButtonAssignment(GET_SESSION_STATUS);
            addButtonAssignment(GET_STATUS);
            addButtonAssignment(TOGGLE_PACE_NOTES_PLAYBACK);
            addButtonAssignment(TOGGLE_PACE_NOTES_RECORDING);
            addButtonAssignment(TOGGLE_TRACK_LANDMARKS_RECORDING);
            addButtonAssignment(ADD_TRACK_LANDMARK);
            addButtonAssignment(PIT_PREDICTION);
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
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[REPEAT_LAST_MESSAGE_BUTTON]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[VOLUME_UP]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[VOLUME_DOWN]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_YELLOW_FLAG_MESSAGES]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[GET_FUEL_STATUS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_MANUAL_FORMATION_LAP]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[READ_CORNER_NAMES_FOR_LAP]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[PRINT_TRACK_DATA]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[CHANNEL_OPEN_FUNCTION]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[GET_STATUS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[GET_SESSION_STATUS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[GET_DAMAGE_REPORT]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[GET_CAR_STATUS]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_PACE_NOTES_PLAYBACK]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_PACE_NOTES_RECORDING]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[TOGGLE_TRACK_LANDMARKS_RECORDING]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[ADD_TRACK_LANDMARK]]);
            pollForButtonClicks(buttonAssignments[buttonAssignmentIndexes[PIT_PREDICTION]]);
            
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
                    if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
                    {
                        if (PCarsUDPreader.getButtonState(ba.buttonIndex))
                        {
                            ba.hasUnprocessedClick = true;
                        }
                    }
                    else if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK)
                    {
                        if (PCars2UDPreader.getButtonState(ba.buttonIndex))
                        {
                            ba.hasUnprocessedClick = true;
                        }
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
                else if (buttonAssignment.action == TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS)
                {
                    actionId = "TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS";
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
                else if (buttonAssignment.action == PRINT_TRACK_DATA)
                {
                    actionId = "PRINT_TRACK_DATA";
                }
                else if (buttonAssignment.action == TOGGLE_YELLOW_FLAG_MESSAGES)
                {
                    actionId = "TOGGLE_YELLOW_FLAG_MESSAGES";
                }
                else if (buttonAssignment.action == GET_FUEL_STATUS)
                {
                    actionId = "GET_FUEL_STATUS";
                }
                else if (buttonAssignment.action == TOGGLE_MANUAL_FORMATION_LAP)
                {
                    actionId = "TOGGLE_MANUAL_FORMATION_LAP";
                }
                else if (buttonAssignment.action == READ_CORNER_NAMES_FOR_LAP)
                {
                    actionId = "READ_CORNER_NAMES_FOR_LAP";
                }
                else if (buttonAssignment.action == GET_CAR_STATUS)
                {
                    actionId = "GET_CAR_STATUS";
                }
                else if (buttonAssignment.action == GET_DAMAGE_REPORT)
                {
                    actionId = "GET_DAMAGE_REPORT";
                }
                else if (buttonAssignment.action == GET_SESSION_STATUS)
                {
                    actionId = "GET_SESSION_STATUS";
                }
                else if (buttonAssignment.action == GET_STATUS)
                {
                    actionId = "GET_STATUS";
                }
                else if (buttonAssignment.action == TOGGLE_PACE_NOTES_PLAYBACK)
                {
                    actionId = "TOGGLE_PACE_NOTES_PLAYBACK";
                }
                else if (buttonAssignment.action == TOGGLE_PACE_NOTES_RECORDING)
                {
                    actionId = "TOGGLE_PACE_NOTES_RECORDING";
                }
                else if (buttonAssignment.action == TOGGLE_TRACK_LANDMARKS_RECORDING)
                {
                    actionId = "TOGGLE_TRACK_LANDMARKS_RECORDING";
                }
                else if (buttonAssignment.action == ADD_TRACK_LANDMARK)
                {
                    actionId = "ADD_TRACK_LANDMARK";
                }
                else if (buttonAssignment.action == PIT_PREDICTION)
                {
                    actionId = "PIT_PREDICTION";
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

            int toggleBlockMessagesInHardPartsButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS_button_index");
            String toggleBlockMessagesInHardPartsDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS_device_guid");
            if (toggleBlockMessagesInHardPartsButtonIndex != -1 && toggleBlockMessagesInHardPartsDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_BLOCK_MESSAGES_IN_HARD_PARTS, toggleBlockMessagesInHardPartsButtonIndex, toggleBlockMessagesInHardPartsDeviceGuid);
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

            int printTrackDataButtonIndex = UserSettings.GetUserSettings().getInt("PRINT_TRACK_DATA_button_index");
            String printTrackDataDeviceGuid = UserSettings.GetUserSettings().getString("PRINT_TRACK_DATA_device_guid");
            if (printTrackDataButtonIndex != -1 && printTrackDataDeviceGuid.Length > 0)
            {
                loadAssignment(parent, PRINT_TRACK_DATA, printTrackDataButtonIndex, printTrackDataDeviceGuid);
            }

            int toggleYellowFlagMessagesButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_YELLOW_FLAG_MESSAGES_button_index");
            String toggleYellowFlagMessagesDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_YELLOW_FLAG_MESSAGES_device_guid");
            if (toggleYellowFlagMessagesButtonIndex != -1 && toggleYellowFlagMessagesDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_YELLOW_FLAG_MESSAGES, toggleYellowFlagMessagesButtonIndex, toggleYellowFlagMessagesDeviceGuid);
            }

            int getFuelStatusButtonIndex = UserSettings.GetUserSettings().getInt("GET_FUEL_STATUS_button_index");
            String getFuelStatusDeviceGuid = UserSettings.GetUserSettings().getString("GET_FUEL_STATUS_device_guid");
            if (getFuelStatusButtonIndex != -1 && getFuelStatusDeviceGuid.Length > 0)
            {
                loadAssignment(parent, GET_FUEL_STATUS, getFuelStatusButtonIndex, getFuelStatusDeviceGuid);
            }

            int toggleManualFormationLapButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_MANUAL_FORMATION_LAP_button_index");
            String toggleManualFormationLapDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_MANUAL_FORMATION_LAP_device_guid");
            if (toggleManualFormationLapButtonIndex != -1 && toggleManualFormationLapDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_MANUAL_FORMATION_LAP, toggleManualFormationLapButtonIndex, toggleManualFormationLapDeviceGuid);
            }

            int readCornerNamesForLapButtonIndex = UserSettings.GetUserSettings().getInt("READ_CORNER_NAMES_FOR_LAP_button_index");
            String readCornerNamesForLapDeviceGuid = UserSettings.GetUserSettings().getString("READ_CORNER_NAMES_FOR_LAP_device_guid");
            if (readCornerNamesForLapButtonIndex != -1 && readCornerNamesForLapDeviceGuid.Length > 0)
            {
                loadAssignment(parent, READ_CORNER_NAMES_FOR_LAP, readCornerNamesForLapButtonIndex, readCornerNamesForLapDeviceGuid);
            }

            int getStatusButtonIndex = UserSettings.GetUserSettings().getInt("GET_STATUS_button_index");
            String getStatusDeviceGuid = UserSettings.GetUserSettings().getString("GET_STATUS_device_guid");
            if (getStatusButtonIndex != -1 && getStatusDeviceGuid.Length > 0)
            {
                loadAssignment(parent, GET_STATUS, getStatusButtonIndex, getStatusDeviceGuid);
            }

            int getDamageReportButtonIndex = UserSettings.GetUserSettings().getInt("GET_DAMAGE_REPORT_button_index");
            String getDamageReportDeviceGuid = UserSettings.GetUserSettings().getString("GET_DAMAGE_REPORT_device_guid");
            if (getDamageReportButtonIndex != -1 && getDamageReportDeviceGuid.Length > 0)
            {
                loadAssignment(parent, GET_DAMAGE_REPORT, getDamageReportButtonIndex, getDamageReportDeviceGuid);
            }

            int getCarStatusButtonIndex = UserSettings.GetUserSettings().getInt("GET_CAR_STATUS_button_index");
            String getCarStatusDeviceGuid = UserSettings.GetUserSettings().getString("GET_CAR_STATUS_device_guid");
            if (getCarStatusButtonIndex != -1 && getCarStatusDeviceGuid.Length > 0)
            {
                loadAssignment(parent, GET_CAR_STATUS, getCarStatusButtonIndex, getCarStatusDeviceGuid);
            }

            int getSessionStatusButtonIndex = UserSettings.GetUserSettings().getInt("GET_SESSION_STATUS_button_index");
            String getSessionStatusDeviceGuid = UserSettings.GetUserSettings().getString("GET_SESSION_STATUS_device_guid");
            if (getSessionStatusButtonIndex != -1 && getSessionStatusDeviceGuid.Length > 0)
            {
                loadAssignment(parent, GET_SESSION_STATUS, getSessionStatusButtonIndex, getSessionStatusDeviceGuid);
            }

            int togglePaceNotesPlaybackButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_PACE_NOTES_PLAYBACK_button_index");
            String togglePaceNotesPlaybackDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_PACE_NOTES_PLAYBACK_device_guid");
            if (togglePaceNotesPlaybackButtonIndex != -1 && togglePaceNotesPlaybackDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_PACE_NOTES_PLAYBACK, togglePaceNotesPlaybackButtonIndex, togglePaceNotesPlaybackDeviceGuid);
            }

            int togglePaceNotesRecordingButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_PACE_NOTES_RECORDING_button_index");
            String togglePaceNotesRecordingDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_PACE_NOTES_RECORDING_device_guid");
            if (togglePaceNotesRecordingButtonIndex != -1 && togglePaceNotesRecordingDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_PACE_NOTES_RECORDING, togglePaceNotesRecordingButtonIndex, togglePaceNotesRecordingDeviceGuid);
            }
            int toggleTrackLandmarkButtonIndex = UserSettings.GetUserSettings().getInt("TOGGLE_TRACK_LANDMARKS_RECORDING_button_index");
            String toggleTrackLandmarkRecordingDeviceGuid = UserSettings.GetUserSettings().getString("TOGGLE_TRACK_LANDMARKS_RECORDING_device_guid");
            if (toggleTrackLandmarkButtonIndex != -1 && toggleTrackLandmarkRecordingDeviceGuid.Length > 0)
            {
                loadAssignment(parent, TOGGLE_TRACK_LANDMARKS_RECORDING, toggleTrackLandmarkButtonIndex, toggleTrackLandmarkRecordingDeviceGuid);
            }
            int addTracklandmarkButtonIndex = UserSettings.GetUserSettings().getInt("ADD_TRACK_LANDMARK_button_index");
            String addTracklandmarkDeviceGuid = UserSettings.GetUserSettings().getString("ADD_TRACK_LANDMARK_device_guid");
            if (addTracklandmarkButtonIndex != -1 && addTracklandmarkDeviceGuid.Length > 0)
            {
                loadAssignment(parent, ADD_TRACK_LANDMARK, addTracklandmarkButtonIndex, addTracklandmarkDeviceGuid);
            }

            int pitPredictionButtonIndex = UserSettings.GetUserSettings().getInt("PIT_PREDICTION_button_index");
            String pitPredictionDeviceGuid = UserSettings.GetUserSettings().getString("PIT_PREDICTION_device_guid");
            if (pitPredictionButtonIndex != -1 && pitPredictionDeviceGuid.Length > 0)
            {
                loadAssignment(parent, PIT_PREDICTION, pitPredictionButtonIndex, pitPredictionDeviceGuid);
            }
        }

        private void loadAssignment(System.Windows.Forms.Form parent, String functionName, int buttonIndex, String deviceGuid)
        {
            if (deviceGuid == UDP_NETWORK_CONTROLLER_GUID.ToString())
            {
                addNetworkControllerToList();
            }
            List<ControllerData> missingControllers = new List<ControllerData>();
            foreach (ControllerData controller in this.controllers)
            {                
                if (controller.guid.ToString() == deviceGuid)
                {
                    buttonAssignments[buttonAssignmentIndexes[functionName]].controller = controller;
                    buttonAssignments[buttonAssignmentIndexes[functionName]].buttonIndex = buttonIndex;
                    if (controller.guid != UDP_NETWORK_CONTROLLER_GUID)
                    {
                        try
                        {
                            var joystick = new Joystick(directInput, controller.guid);
                            // Acquire the joystick
                            joystick.SetCooperativeLevel(parent, (CooperativeLevel.NonExclusive | CooperativeLevel.Background));
                            joystick.Properties.BufferSize = 128;
                            joystick.Acquire();
                            buttonAssignments[buttonAssignmentIndexes[functionName]].joystick = joystick;
                        }
                        catch (Exception e)
                        {
                            missingControllers.Add(controller);
                            Console.WriteLine("Controller " + controller.deviceName + " is not available: " + e.Message);
                        }
                    }
                }
            }
            Boolean removedMissingController = false;
            foreach (ControllerData controllerData in missingControllers) {
                if (missingControllers.Contains(controllerData))
                {
                    removedMissingController = true;
                    this.controllers.Remove(controllerData);
                }
            }
            if (removedMissingController)
            {
                this.mainWindow.getControllers();
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
                    catch (Exception)
                    {
                        // ignore this exception
                    }
                }
                else if (ba.controller.guid == UDP_NETWORK_CONTROLLER_GUID && CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
                {
                    return PCarsUDPreader.getButtonState(ba.buttonIndex);
                }
                else if (ba.controller.guid == UDP_NETWORK_CONTROLLER_GUID && CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2_NETWORK)
                {
                    return PCars2UDPreader.getButtonState(ba.buttonIndex);
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
                            Console.WriteLine("Failed to get device info: " + e.Message);
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
            controllers.Remove(networkGamePad);
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
                int assignedButton;
                if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK)
                {
                    PCarsUDPreader gameDataReader = (PCarsUDPreader)GameStateReaderFactory.getInstance().getGameStateReader(GameDefinition.pCarsNetwork);
                    assignedButton = gameDataReader.getButtonIndexForAssignment();
                }
                else
                {
                    PCars2UDPreader gameDataReader = (PCars2UDPreader)GameStateReaderFactory.getInstance().getGameStateReader(GameDefinition.pCars2Network);
                    assignedButton = gameDataReader.getButtonIndexForAssignment();
                }
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
                try
                {
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
                        if (!gotAssignment)
                        {
                            Thread.Sleep(20);
                        }
                    }
                    if (!gotAssignment)
                    {
                        joystick.Unacquire();
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to acquire device " + controllerData.deviceName + " error: " + e.Message);
                    listenForAssignment = false;
                    gotAssignment = false;
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
            ThreadManager.UnregisterTemporaryThread(asyncDisposeThread);
            asyncDisposeThread = new Thread(() =>
            {
                DateTime now = DateTime.UtcNow;
                Thread.CurrentThread.IsBackground = true;
                String name = joystick.Information.InstanceName;
                try
                {                    
                    joystick.Dispose();
                    //Console.WriteLine("Disposed of temporary " + deviceType + " object " + name + " after " + (DateTime.UtcNow - now).TotalSeconds + " seconds");
                }
                catch (Exception e) { 
                    //log and swallow 
                    Console.WriteLine("Failed to dispose of temporary " + deviceType + " object " + name + "after " + (DateTime.UtcNow - now).TotalSeconds + " seconds: " + e.Message);
                }
            });
            asyncDisposeThread.Name = "ControllerConfiguration.asyncDisposeThread";
            ThreadManager.RegisterTemporaryThread(asyncDisposeThread);
            asyncDisposeThread.Start();
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
