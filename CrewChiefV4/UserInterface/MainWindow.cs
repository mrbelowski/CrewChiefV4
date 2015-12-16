using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using CrewChiefV4;
using System.Threading;
using System.IO;
using SharpDX.DirectInput;
using System.Runtime.InteropServices;

namespace CrewChiefV4
{
    public partial class MainWindow : Form
    {
        private ControllerConfiguration controllerConfiguration;
        
        private CrewChief crewChief;

        private Boolean isAssigningButton = false;

        private bool _IsAppRunning;

        private Boolean runListenForChannelOpenThread = false;

        private Boolean runListenForButtonPressesThread = false;

        private TimeSpan buttonCheckInterval = TimeSpan.FromMilliseconds(100);

        private VoiceOptionEnum voiceOption;
        
        private void messagesVolumeSlider_Scroll(object sender, EventArgs e)
        {
            float volFloat = (float) messagesVolumeSlider.Value / 10;
            setMessagesVolume(volFloat);
            UserSettings.GetUserSettings().setProperty("messages_volume", volFloat);
            UserSettings.GetUserSettings().saveUserSettings();
        }

        private void setMessagesVolume(float vol)
        {
            int NewVolume = (int) (((float)ushort.MaxValue) * vol);
            // Set the same volume for both the left and the right channels
            uint NewVolumeAllChannels = (((uint)NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
            // Set the volume
            NativeMethods.waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
        }

        private void backgroundVolumeSlider_Scroll(object sender, EventArgs e)
        {
            float volFloat = (float)backgroundVolumeSlider.Value / 10;
            UserSettings.GetUserSettings().setProperty("background_volume", volFloat);
            UserSettings.GetUserSettings().saveUserSettings();
        }
        
        public bool IsAppRunning
        {
            get
            {
                return _IsAppRunning;
            }
            set
            {
                _IsAppRunning = value;
                startApplicationButton.Text = _IsAppRunning ? "Stop" : "Start Application";
            }
        }

        private void setSelectedGameType()
        {
            String[] commandLineArgs = Environment.GetCommandLineArgs();
            Boolean setFromCommandLine = false;
            if (commandLineArgs != null)
            {
                foreach (String arg in commandLineArgs)
                {
                    if (arg.Equals(GameDefinition.raceRoom.gameEnum.ToString()))
                    {
                        this.gameDefinitionList.Text = GameDefinition.raceRoom.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.pCars32Bit.gameEnum.ToString()))
                    {
                        this.gameDefinitionList.Text = GameDefinition.pCars32Bit.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.pCars64Bit.gameEnum.ToString()))
                    {
                        this.gameDefinitionList.Text = GameDefinition.pCars64Bit.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.pCarsNetwork.gameEnum.ToString()))
                    {
                        this.gameDefinitionList.Text = GameDefinition.pCarsNetwork.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                }
            }
            if (!setFromCommandLine)
            {
                String lastDef = UserSettings.GetUserSettings().getString("last_game_definition");
                if (lastDef != null && lastDef.Length > 0)
                {
                    GameDefinition gameDefinition = GameDefinition.getGameDefinitionForEnumName(lastDef);
                    if (gameDefinition != null)
                    {
                        this.gameDefinitionList.Text = gameDefinition.friendlyName;
                    }
                }
            }
        }

        private void updateSelectedGame()
        {

        }

        public MainWindow()
        {
            controllerConfiguration = new ControllerConfiguration();
            InitializeComponent();
            setSelectedGameType();
            this.app_version.Text = CrewChief.Version;
            this.filenameLabel.Visible = System.Diagnostics.Debugger.IsAttached;
            this.filenameTextbox.Visible = System.Diagnostics.Debugger.IsAttached;
            this.recordSession.Visible = System.Diagnostics.Debugger.IsAttached;
            this.playbackInterval.Visible = System.Diagnostics.Debugger.IsAttached;
            CheckForIllegalCrossThreadCalls = false;
            Console.SetOut(new ControlWriter(textBox1));
            crewChief = new CrewChief();
            float messagesVolume = UserSettings.GetUserSettings().getFloat("messages_volume");
            float backgroundVolume = UserSettings.GetUserSettings().getFloat("background_volume");
            setMessagesVolume(messagesVolume);
            messagesVolumeSlider.Value = (int)(messagesVolume * 10f);
            backgroundVolumeSlider.Value = (int) (backgroundVolume * 10f);

            getControllers();
            controllerConfiguration.loadSettings(this);
            String customDeviceGuid = UserSettings.GetUserSettings().getString("custom_device_guid");
            if (customDeviceGuid != null && customDeviceGuid.Length > 0)
            {
                try
                {
                    Guid guid;
                    if (Guid.TryParse(customDeviceGuid, out guid)) {
                        controllerConfiguration.addCustomController(guid);
                    }
                    else
                    {
                        Console.WriteLine("Failed to add custom device, unable to process GUID");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to add custom device, message: " + e.Message);
                }
            }
            voiceOption = getVoiceOptionEnum(UserSettings.GetUserSettings().getString("VOICE_OPTION"));
            if (voiceOption == VoiceOptionEnum.DISABLED)
            {
                this.voiceDisableButton.Checked = true;
            }
            else if (voiceOption == VoiceOptionEnum.ALWAYS_ON)
            {
                this.alwaysOnButton.Checked = true;
            } else if (voiceOption == VoiceOptionEnum.HOLD)
            {
                this.holdButton.Checked = true;
            }
            else if (voiceOption == VoiceOptionEnum.TOGGLE)
            {
                this.toggleButton.Checked = true;
            }
            if (voiceOption != VoiceOptionEnum.DISABLED)
            {
                initialiseSpeechEngine();
            }
            runListenForButtonPressesThread = controllerConfiguration.listenForButtons(voiceOption == VoiceOptionEnum.TOGGLE);
            updateActions();
            this.assignButtonToAction.Enabled = false;
            this.deleteAssigmentButton.Enabled = false;

            if (UserSettings.GetUserSettings().getBoolean("run_immediately") &&
                GameDefinition.getGameDefinitionForFriendlyName(gameDefinitionList.Text) != null)
            {
                doStartAppStuff();
            }
        }

        private void listenForChannelOpen()
        {
            Boolean channelOpen = false;
            if (crewChief.speechRecogniser.initialised && voiceOption == VoiceOptionEnum.HOLD)
            {
                Console.WriteLine("Running speech recognition in 'hold button' mode");
                crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.HOLD;
                while (runListenForChannelOpenThread)
                {
                    Thread.Sleep(100);
                    if (!channelOpen && controllerConfiguration.isChannelOpen())
                    {
                        channelOpen = true;
                        crewChief.audioPlayer.playStartListeningBeep();
                        crewChief.speechRecogniser.recognizeAsync();
                        Console.WriteLine("Listening...");
                    }
                    else if (channelOpen && !controllerConfiguration.isChannelOpen())
                    {
                        Console.WriteLine("Stopping listening...");
                        crewChief.speechRecogniser.recognizeAsyncCancel();
                        channelOpen = false;
                    }
                }        
            }            
        }

        private void listenForButtons()
        {
            DateTime lastButtoncheck = DateTime.Now;
            Boolean channelOpen = false;
            if (crewChief.speechRecogniser.initialised && voiceOption == VoiceOptionEnum.TOGGLE) 
            {
                Console.WriteLine("Running speech recognition in 'toggle button' mode");
            }
            while (runListenForButtonPressesThread)
            {
                Thread.Sleep(100);
                DateTime now = DateTime.Now;
                controllerConfiguration.pollForButtonClicks(voiceOption == VoiceOptionEnum.TOGGLE);
                int nextPollWait = 0;
                if (now > lastButtoncheck.Add(buttonCheckInterval))
                {
                    lastButtoncheck = now;
                    if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_RACE_UPDATES_FUNCTION))
                    {
                        Console.WriteLine("Toggling keep quiet mode");
                        crewChief.toggleKeepQuietMode();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_SPOTTER_FUNCTION))
                    {
                        Console.WriteLine("Toggling spotter mode");
                        crewChief.toggleSpotterMode();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_READ_OPPONENT_DELTAS))
                    {
                        Console.WriteLine("Toggling read opponent deltas mode");
                        crewChief.toggleReadOpponentDeltasMode();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.REPEAT_LAST_MESSAGE_BUTTON))
                    {
                        Console.WriteLine("Repeating last message");
                        crewChief.audioPlayer.repeatLastMessage();
                        nextPollWait = 1000;
                    }
                    else if (crewChief.speechRecogniser.initialised && voiceOption == VoiceOptionEnum.TOGGLE && 
                        controllerConfiguration.hasOutstandingClick(ControllerConfiguration.CHANNEL_OPEN_FUNCTION))
                    {
                        crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.TOGGLE;
                        if (!channelOpen)
                        {
                            Console.WriteLine("Listening...");
                            channelOpen = true;
                            crewChief.speechRecogniser.recognizeAsync();
                        }
                        else
                        {
                            Console.WriteLine("Finished listening...");
                            channelOpen = false;
                            crewChief.speechRecogniser.recognizeAsyncCancel();
                        }
                        nextPollWait = 1000;
                    }
                }
                Thread.Sleep(nextPollWait);
            }
        }
        
        private void startApplicationButton_Click(object sender, EventArgs e)
        {
            doStartAppStuff();
        }

        private void doStartAppStuff()
        {
            IsAppRunning = !IsAppRunning;
            if (_IsAppRunning)
            {
                GameDefinition gameDefinition = GameDefinition.getGameDefinitionForFriendlyName(gameDefinitionList.Text);
                if (gameDefinition != null)
                {
                    crewChief.setGameDefinition(gameDefinition);
                }
                else
                {
                    MessageBox.Show("Please choose a game option", "No game selected",
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                this.assignButtonToAction.Enabled = false;
                this.deleteAssigmentButton.Enabled = false;
                this.groupBox1.Enabled = false;
                this.button1.Enabled = false;
                ThreadStart crewChiefWork = runApp;
                Thread crewChiefThread = new Thread(crewChiefWork);
                crewChiefThread.Start();
                runListenForChannelOpenThread = controllerConfiguration.listenForChannelOpen()
                    && voiceOption == VoiceOptionEnum.HOLD && crewChief.speechRecogniser.initialised;
                if (runListenForChannelOpenThread && voiceOption == VoiceOptionEnum.HOLD && crewChief.speechRecogniser.initialised)
                {
                    Console.WriteLine("Listening on default audio input device");
                    ThreadStart channelOpenButtonListenerWork = listenForChannelOpen;
                    Thread channelOpenButtonListenerThread = new Thread(channelOpenButtonListenerWork);
                    channelOpenButtonListenerThread.Start();
                }
                else if (voiceOption == VoiceOptionEnum.ALWAYS_ON && crewChief.speechRecogniser.initialised)
                {
                    Console.WriteLine("Running speech recognition in 'always on' mode");
                    crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.ALWAYS_ON;
                    crewChief.speechRecogniser.recognizeAsync();
                }
                if (runListenForButtonPressesThread)
                {
                    Console.WriteLine("Listening for buttons");
                    ThreadStart buttonPressesListenerWork = listenForButtons;
                    Thread buttonPressesListenerThread = new Thread(buttonPressesListenerWork);
                    buttonPressesListenerThread.Start();
                }
            }
            else
            {
                if (voiceOption == VoiceOptionEnum.ALWAYS_ON && crewChief.speechRecogniser.initialised)
                {
                    Console.WriteLine("Stopping listening...");
                    crewChief.speechRecogniser.recognizeAsyncCancel();
                }
                this.deleteAssigmentButton.Enabled = this.buttonActionSelect.SelectedIndex > -1 &&
                    this.controllerConfiguration.buttonAssignments[this.buttonActionSelect.SelectedIndex].joystick != null;
                this.assignButtonToAction.Enabled = this.buttonActionSelect.SelectedIndex > -1 && this.controllersList.SelectedIndex > -1;
                stopApp();
                Console.WriteLine("Application stopped");
                this.button1.Enabled = true;
                this.groupBox1.Enabled = true;
            }
        }

        private void stopApp(object sender, FormClosedEventArgs e)
        {
            stopApp();
        }

        private void runApp()
        {
            String filenameToRun = null;
            int interval = 0;
            Boolean record = false;
            if (System.Diagnostics.Debugger.IsAttached && filenameTextbox.Text != null && filenameTextbox.Text.Count() > 0)
            {
                filenameToRun = filenameTextbox.Text;
                if (playbackInterval.Text.Length > 0)
                {
                    interval = int.Parse(playbackInterval.Text);
                }
            }
            if (System.Diagnostics.Debugger.IsAttached && recordSession.Checked) {
                record = true;
            }
            if (!crewChief.Run(filenameToRun, interval, record))
            {
                this.deleteAssigmentButton.Enabled = this.buttonActionSelect.SelectedIndex > -1 &&
                    this.controllerConfiguration.buttonAssignments[this.buttonActionSelect.SelectedIndex].joystick != null;
                this.assignButtonToAction.Enabled = this.buttonActionSelect.SelectedIndex > -1 && this.controllersList.SelectedIndex > -1;
                stopApp();
                this.button1.Enabled = true;
                IsAppRunning = false;
            }
        }
        
        private void stopApp()
        {
            runListenForChannelOpenThread = false;
            runListenForButtonPressesThread = false;
            crewChief.stop();
        }

        private void buttonActionSelect_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.deleteAssigmentButton.Enabled = this.buttonActionSelect.SelectedIndex > -1 && !crewChief.running;
            this.assignButtonToAction.Enabled = this.buttonActionSelect.SelectedIndex > -1 && this.controllersList.SelectedIndex > -1 && !crewChief.running;
        }

        private void controllersList_SelectedIndexChanged(object sender, EventArgs e)
        {
            this.deleteAssigmentButton.Enabled = this.buttonActionSelect.SelectedIndex > -1 && !crewChief.running;
            this.assignButtonToAction.Enabled = this.buttonActionSelect.SelectedIndex > -1 && this.controllersList.SelectedIndex > -1 && !crewChief.running;
        }

        private void getControllers() {
            this.controllersList.Items.Clear();
            foreach (ControllerConfiguration.ControllerData configData in controllerConfiguration.controllers)
            {
                this.controllersList.Items.Add(configData.deviceType.ToString() + " " + configData.deviceName);
            }
        }

        private void updateActions()
        {
            this.buttonActionSelect.Items.Clear();
            foreach (ControllerConfiguration.ButtonAssignment assignment in controllerConfiguration.buttonAssignments)
            {
                this.buttonActionSelect.Items.Add(assignment.getInfo());
            }
        }

        private void assignButtonToActionClick(object sender, EventArgs e)
        {
            if (!isAssigningButton)
            {
                if (this.controllersList.SelectedIndex >= 0 && this.buttonActionSelect.SelectedIndex >= 0)
                {
                    isAssigningButton = true;
                    this.assignButtonToAction.Text = "Waiting for button, click to cancel";
                    ThreadStart assignButtonWork = assignButton;
                    Thread assignButtonThread = new Thread(assignButtonWork);
                    assignButtonThread.Start();
                }                
            }
            else
            {
                isAssigningButton = false;
                controllerConfiguration.listenForAssignment = false;
                this.assignButtonToAction.Text = "Assign";
            }
        }

        private void initialiseSpeechEngine()
        {
            try
            {
                if (!crewChief.speechRecogniser.initialised)
                {
                    crewChief.speechRecogniser.initialiseSpeechEngine();
                    Console.WriteLine("Attempted to initialise speech engine - success = " + crewChief.speechRecogniser.initialised);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Unable to create speech engine, error message: " + e.Message);
                runListenForChannelOpenThread = false;
            }
        }

        private void assignButton()
        {
            if (controllerConfiguration.assignButton(this, this.controllersList.SelectedIndex, this.buttonActionSelect.SelectedIndex))
            {
                updateActions();
                isAssigningButton = false;
                controllerConfiguration.saveSettings();
                runListenForChannelOpenThread = controllerConfiguration.listenForChannelOpen() && voiceOption != VoiceOptionEnum.DISABLED;
                if (runListenForChannelOpenThread)
                {
                    initialiseSpeechEngine();
                }
                runListenForButtonPressesThread = controllerConfiguration.listenForButtons(voiceOption == VoiceOptionEnum.TOGGLE);
            }
            this.assignButtonToAction.Text = "Assign";
            controllerConfiguration.saveSettings();
        }

        private void deleteAssignmentButtonClicked(object sender, EventArgs e)
        {
            if (this.buttonActionSelect.SelectedIndex >= 0)
            {
                this.controllerConfiguration.buttonAssignments[this.buttonActionSelect.SelectedIndex].unassign();                
                updateActions();
                runListenForChannelOpenThread = controllerConfiguration.listenForChannelOpen();
                runListenForButtonPressesThread = controllerConfiguration.listenForButtons(voiceOption == VoiceOptionEnum.TOGGLE);
            }
            controllerConfiguration.saveSettings();
        }

        private void editPropertiesButtonClicked(object sender, EventArgs e)
        {
            var form = new PropertiesForm(this);
            form.ShowDialog(this);
        }

        private void voiceDisableButton_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                runListenForChannelOpenThread = false;
                runListenForButtonPressesThread = controllerConfiguration.listenForButtons(false);
                voiceOption = VoiceOptionEnum.DISABLED;
                UserSettings.GetUserSettings().setProperty("VOICE_OPTION", getVoiceOptionString());
                UserSettings.GetUserSettings().saveUserSettings();
            }            
        }
        private void holdButton_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                runListenForButtonPressesThread = controllerConfiguration.listenForButtons(false);
                try
                {
                    initialiseSpeechEngine();
                    crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.HOLD;
                    voiceOption = VoiceOptionEnum.HOLD;
                    runListenForChannelOpenThread = true;
                    UserSettings.GetUserSettings().setProperty("VOICE_OPTION", getVoiceOptionString());
                    UserSettings.GetUserSettings().saveUserSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to initialise speech engine, message = " + ex.Message);
                }  
            }            
        }
        private void toggleButton_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                runListenForButtonPressesThread = true;
                runListenForChannelOpenThread = false;
                try
                {
                    initialiseSpeechEngine();
                    crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.TOGGLE;
                    voiceOption = VoiceOptionEnum.TOGGLE;
                    UserSettings.GetUserSettings().setProperty("VOICE_OPTION", getVoiceOptionString());
                    UserSettings.GetUserSettings().saveUserSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to initialise speech engine, message = " + ex.Message);
                }  
            }
        }
        private void alwaysOnButton_CheckedChanged(object sender, EventArgs e)
        {
            if (((RadioButton)sender).Checked)
            {
                runListenForChannelOpenThread = false;
                runListenForButtonPressesThread = controllerConfiguration.listenForButtons(false);
                try
                {
                    initialiseSpeechEngine();
                    crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.ALWAYS_ON;
                    voiceOption = VoiceOptionEnum.ALWAYS_ON;
                    UserSettings.GetUserSettings().setProperty("VOICE_OPTION", getVoiceOptionString());
                    UserSettings.GetUserSettings().saveUserSettings();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Unable to initialise speech engine, message = " + ex.Message);
                }                
            }
        }
            
        private VoiceOptionEnum getVoiceOptionEnum(String enumStr)
        {
            VoiceOptionEnum enumVal = VoiceOptionEnum.DISABLED;
            if (enumStr != null && enumStr.Length > 0) {
                 enumVal = (VoiceOptionEnum)VoiceOptionEnum.Parse(typeof(VoiceOptionEnum), enumStr, true);
            }
            return enumVal;
        }

        private String getVoiceOptionString()
        {
            return voiceOption.ToString();
        }

        public enum VoiceOptionEnum
        {
            DISABLED, HOLD, TOGGLE, ALWAYS_ON
        }
        
        private void clearConsole(object sender, EventArgs e)
        {
            if (!textBox1.IsDisposed)
            {
                try
                {
                    textBox1.Text = "";
                }
                catch (Exception)
                {
                    // swallow - nothing to log it to
                }
            }
        }

        private void updateSelectedGameDefinition(object sender, EventArgs e)
        {
            if (this.gameDefinitionList.Text.Equals(GameDefinition.pCarsNetwork.friendlyName))
            {
                controllerConfiguration.addNetworkControllerToList();
            }
            else
            {
                controllerConfiguration.removeNetworkControllerFromList();                
            }
            getControllers();
        }  
    }

    public class ControlWriter : TextWriter
    {
        private TextBox textbox;
        public ControlWriter(TextBox textbox)
        {
            this.textbox = textbox;
        }

        public override void Write(char value)
        {
            if (!textbox.IsDisposed)
            {
                textbox.AppendText(value.ToString());
            }
        }

        public override void Write(string value)
        {
            if (!textbox.IsDisposed)
            {
                textbox.AppendText(value);
            }
        }

        public override void WriteLine(string value)
        {
            if (!textbox.IsDisposed)
            {
                try
                {
                    textbox.AppendText(DateTime.Now.ToString("HH:mm:ss.fff"));
                    textbox.AppendText(" : ");
                    textbox.AppendText(value + "\n");
                }
                catch (Exception)
                {
                    // swallow - nothing to log it to
                }
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.ASCII; }
        }
    }

    static class NativeMethods
    {
        [DllImport("winmm.dll")]
        public static extern int waveOutGetVolume(IntPtr hwo, out uint dwVolume);

        [DllImport("winmm.dll")]
        public static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume);

    }
}
