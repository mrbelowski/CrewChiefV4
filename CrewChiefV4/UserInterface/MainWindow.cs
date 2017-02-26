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
using AutoUpdaterDotNET;
using System.Net;
using System.Xml.Linq;
using System.IO.Compression;
using CrewChiefV4.Audio;
using CrewChiefV4.UserInterface;

namespace CrewChiefV4
{
    public partial class MainWindow : Form
    {
        private String baseDriverNamesDownloadLocation;
        private String updateDriverNamesDownloadLocation;
        private String driverNamesTempFileName = "temp_driver_names.zip";
        private Boolean getBaseDriverNames = false;

        private String baseSoundPackDownloadLocation;
        private String updateSoundPackDownloadLocation;
        private String soundPackTempFileName = "temp_sound_pack.zip";
        private Boolean getBaseSoundPack = false;

        private Boolean isDownloadingDriverNames = false;
        private Boolean isDownloadingSoundPack = false;
        private Boolean newSoundPackAvailable = false;
        private Boolean newDriverNamesAvailable = false;

        private ControllerConfiguration controllerConfiguration;
        
        private CrewChief crewChief;

        private Boolean isAssigningButton = false;

        private bool _IsAppRunning;

        private Boolean runListenForChannelOpenThread = false;

        private Boolean runListenForButtonPressesThread = false;

        private TimeSpan buttonCheckInterval = TimeSpan.FromMilliseconds(100);

        private VoiceOptionEnum voiceOption;

        private static String autoUpdateXMLURL = "https://drive.google.com/uc?export=download&id=0B4KQS820QNFbWWFjaDAzRldMNUE";

        private float latestSoundPackVersion = -1;
        private float latestDriverNamesVersion = -1;

        private ControlWriter cw = null;

        private float currentVolume = -1;
                
        private void FormMain_Load(object sender, EventArgs e)
        {            
            // Some update test code - uncomment this to allow the app to process an update .zip file in the root of the sound pack
            /*
            ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName, AudioPlayer.soundFilesPath + @"\sounds_temp");
            UpdateHelper.ProcessFileUpdates(AudioPlayer.soundFilesPath + @"\sounds_temp");
            UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\sounds_temp", AudioPlayer.soundFilesPath);                   
            */

            // do the auto updating stuff in a separate Thread
            new Thread(() =>
            {
                Console.WriteLine("Checking for updates");
                Thread.CurrentThread.IsBackground = true;                
                try
                {
                    AutoUpdater.Start(autoUpdateXMLURL);
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to start auto updater");
                }
                try 
                {
                    // now the sound packs
                    downloadSoundPackButton.Text = Configuration.getUIString("checking_sound_pack_version");
                    downloadDriverNamesButton.Text = Configuration.getUIString("checking_driver_names_version");
                    string xml = new WebClient().DownloadString(autoUpdateXMLURL);
                    XDocument doc = XDocument.Parse(xml);

                    String languageToCheck = AudioPlayer.soundPackLanguage == null ? "en" : AudioPlayer.soundPackLanguage;
                    Boolean gotLanguageSpecificUpdateInfo = false;
                    foreach (XElement element in doc.Descendants("soundpack"))
                    {
                        XAttribute languageAttribute = element.Attribute(XName.Get("language", ""));
                        if (languageAttribute.Value == languageToCheck)
                        {
                            // this is the update set for this language
                            float.TryParse(element.Descendants("soundpackversion").First().Value, out latestSoundPackVersion);
                            float.TryParse(element.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion);
                            baseSoundPackDownloadLocation = element.Descendants("basesoundpackurl").First().Value;
                            baseDriverNamesDownloadLocation = element.Descendants("basedrivernamesurl").First().Value;
                            updateSoundPackDownloadLocation = element.Descendants("updatesoundpackurl").First().Value;
                            updateDriverNamesDownloadLocation = element.Descendants("updatedrivernamesurl").First().Value;
                            gotLanguageSpecificUpdateInfo = true;
                            break;
                        }
                    }
                    if (!gotLanguageSpecificUpdateInfo && AudioPlayer.soundPackLanguage == null)
                    {
                        float.TryParse(doc.Descendants("soundpackversion").First().Value, out latestSoundPackVersion);
                        float.TryParse(doc.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion);
                        baseSoundPackDownloadLocation = doc.Descendants("basesoundpackurl").First().Value;
                        baseDriverNamesDownloadLocation = doc.Descendants("basedrivernamesurl").First().Value;
                        updateSoundPackDownloadLocation = doc.Descendants("updatesoundpackurl").First().Value;
                        updateDriverNamesDownloadLocation = doc.Descendants("updatedrivernamesurl").First().Value;
                    }

                    if (latestSoundPackVersion == -1 && AudioPlayer.soundPackVersion == -1)
                    {
                        downloadSoundPackButton.Text = Configuration.getUIString("no_sound_pack_detected_unable_to_locate_update");
                        downloadSoundPackButton.Enabled = false;
                        downloadSoundPackButton.BackColor = Color.LightGray;
                    }
                    else if (latestSoundPackVersion > AudioPlayer.soundPackVersion)
                    {
                        downloadSoundPackButton.Enabled = true;
                        downloadSoundPackButton.BackColor = Color.LightGreen;
                        if (AudioPlayer.soundPackVersion == -1)
                        {
                            downloadSoundPackButton.Text = Configuration.getUIString("no_sound_pack_detected_press_to_download");
                            getBaseSoundPack = true;
                        }
                        else
                        {
                            downloadSoundPackButton.Text = Configuration.getUIString("updated_sound_pack_available_press_to_download");
                        }
                        newSoundPackAvailable = true;
                        downloadSoundPackButton.Enabled = true;
                    }
                    else
                    {
                        downloadSoundPackButton.Text = Configuration.getUIString("sound_pack_is_up_to_date");
                        downloadSoundPackButton.BackColor = Color.LightGray;
                    }
                    if (latestDriverNamesVersion == -1 && AudioPlayer.driverNamesVersion == -1) {
                        downloadDriverNamesButton.Text = Configuration.getUIString("no_driver_names_detected_unable_to_locate_update");
                        downloadDriverNamesButton.Enabled = false;
                        downloadDriverNamesButton.BackColor = Color.LightGray;
                    }
                    else if (latestDriverNamesVersion > AudioPlayer.driverNamesVersion)
                    {
                        downloadDriverNamesButton.Enabled = true;
                        downloadDriverNamesButton.BackColor = Color.LightGreen;
                        if (AudioPlayer.driverNamesVersion == -1)
                        {
                            downloadDriverNamesButton.Text = Configuration.getUIString("no_driver_names_detected_press_to_download");
                            getBaseDriverNames = true;
                        }
                        else
                        {
                            downloadDriverNamesButton.Text = Configuration.getUIString("updated_driver_names_available_press_to_download");
                        }
                        newDriverNamesAvailable = true;
                    }
                    else
                    {
                        downloadDriverNamesButton.Text = Configuration.getUIString("driver_names_are_up_to_date");
                        downloadDriverNamesButton.Enabled = false;
                        downloadDriverNamesButton.BackColor = Color.LightGray;
                    }
                    Console.WriteLine("Check for updates completed");
                }
                catch (Exception error)
                {
                    Console.WriteLine("Unable to get auto update details: " + error.Message);
                }
            }).Start();
        }

        public void updateMessagesVolume(float messagesVolume)
        {
            currentVolume = messagesVolume;
            setMessagesVolume(messagesVolume);
            messagesVolumeSlider.Value = (int)(messagesVolume * 10f);
        }
                
        private void messagesVolumeSlider_Scroll(object sender, EventArgs e)
        {
            float volFloat = (float) messagesVolumeSlider.Value / 10;
            setMessagesVolume(volFloat);
            currentVolume = volFloat;
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
                startApplicationButton.Text = _IsAppRunning ? Configuration.getUIString("stop") : Configuration.getUIString("start_application");
                downloadDriverNamesButton.Enabled = !value && newDriverNamesAvailable;
                downloadSoundPackButton.Enabled = !value && newSoundPackAvailable;
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
                        Console.WriteLine("Set Raceroom mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.raceRoom.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.pCars32Bit.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set PCars 32bit mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.pCars32Bit.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.pCars64Bit.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set PCars 64bit mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.pCars64Bit.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.pCarsNetwork.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set PCars network mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.pCarsNetwork.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.assetto64Bit.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set Assetto Corsa mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.assetto64Bit.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals(GameDefinition.assetto32Bit.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set Assetto Corsa mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.assetto32Bit.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    // special cases for RF1 versions
                    else if (arg.Equals("AMS"))
                    {
                        Console.WriteLine("Set Autombilista mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.automobilista.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals("FTRUCK"))
                    {
                        Console.WriteLine("Set FTruck mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.ftruck.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals("RF1"))
                    {
                        Console.WriteLine("Set RF1 mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.rFactor1.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals("MARCAS"))
                    {
                        Console.WriteLine("Set Copa de Marcas mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.marcas.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals("GSC"))
                    {
                        Console.WriteLine("Set GSC mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.gameStockCar.friendlyName;
                        setFromCommandLine = true;
                        break;
                    }
                    else if (arg.Equals("RF2"))
                    {
                        Console.WriteLine("Set RF2 mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.rfactor2_64bit.friendlyName;
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
                    try
                    {
                        GameDefinition gameDefinition = GameDefinition.getGameDefinitionForEnumName(lastDef);
                        if (gameDefinition != null)
                        {
                            Console.WriteLine("Set "+ gameDefinition.friendlyName + " mode from previous launch");
                            this.gameDefinitionList.Text = gameDefinition.friendlyName;
                        }
                    }
                    catch (Exception)
                    {
                        //ignore, just don't set the value in the list
                    }
                }
            }
        }

        private void updateSelectedGame()
        {

        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            base.OnFormClosing(e);
            lock (cw)
            {
                cw.textbox = null;
                cw.Dispose();
            }
        } 

        public MainWindow()
        {
            InitializeComponent();
            CheckForIllegalCrossThreadCalls = false;
            cw = new ControlWriter(textBox1);
            Console.SetOut(cw);
            Console.WriteLine("Starting app");
            controllerConfiguration = new ControllerConfiguration();            
            setSelectedGameType();
            this.app_version.Text = System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.filenameLabel.Visible = System.Diagnostics.Debugger.IsAttached;
            this.filenameTextbox.Visible = System.Diagnostics.Debugger.IsAttached;
            this.recordSession.Visible = System.Diagnostics.Debugger.IsAttached;
            this.playbackInterval.Visible = System.Diagnostics.Debugger.IsAttached;
            
            if (!UserSettings.GetUserSettings().getBoolean("enable_console_logging"))
            {
                Console.WriteLine("Console logging has been disabled ('enable_console_logging' property)");
            }
            cw.enable = UserSettings.GetUserSettings().getBoolean("enable_console_logging");
            crewChief = new CrewChief();
            float messagesVolume = UserSettings.GetUserSettings().getFloat("messages_volume");
            float backgroundVolume = UserSettings.GetUserSettings().getFloat("background_volume");
            updateMessagesVolume(messagesVolume);
            backgroundVolumeSlider.Value = (int) (backgroundVolume * 10f);

            Console.WriteLine("Loading controller settings");
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
            Console.WriteLine("Load controller settings complete");
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
                        new Thread(() =>
                        {
                            Thread.Sleep(2000);
                            if (!channelOpen && crewChief.speechRecogniser.waitingForSpeech)
                            {
                                crewChief.speechRecogniser.waitingForSpeech = false;
                                crewChief.youWot();
                            }
                        }).Start();                        
                    }
                }        
            }            
        }

        private void listenForButtons()
        {
            DateTime lastButtoncheck = DateTime.Now;
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
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.VOLUME_UP))
                    {
                        if (currentVolume == -1)
                        {
                            Console.WriteLine("Initial volume not set, ignoring");
                        } else if (currentVolume >= 1) {
                            Console.WriteLine("Volume at max");
                        } else {
                            Console.WriteLine("Increasing volume");
                            updateMessagesVolume(currentVolume + 0.1f);
                        }
                        nextPollWait = 200;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.VOLUME_DOWN))
                    {
                        if (currentVolume == -1)
                        {
                            Console.WriteLine("Initial volume not set, ignoring");
                        } else if (currentVolume <= 0) {
                            Console.WriteLine("Volume at min");
                        } else {
                            Console.WriteLine("Decreasing volume");
                            updateMessagesVolume(currentVolume - 0.1f);
                        }
                        nextPollWait = 200;
                    }
                    else if (crewChief.speechRecogniser.initialised && voiceOption == VoiceOptionEnum.TOGGLE)
                    {
                        if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.CHANNEL_OPEN_FUNCTION))
                        {
                            crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.TOGGLE;
                            if (crewChief.speechRecogniser.waitingForSpeech)
                            {
                                Console.WriteLine("Cancelling...");
                                crewChief.speechRecogniser.waitingForSpeech = false;
                                crewChief.speechRecogniser.recognizeAsyncCancel();
                            }
                            Console.WriteLine("Listening...");
                            crewChief.audioPlayer.playStartListeningBeep();
                            crewChief.speechRecogniser.recognizeAsync();                            
                            nextPollWait = 1000;
                        }
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
                    MessageBox.Show(Configuration.getUIString("please_choose_a_game_option"), Configuration.getUIString("no_game_selected"),
                        MessageBoxButtons.OK, MessageBoxIcon.Information);
                    return;
                }
                CarData.loadCarClassData();
                this.runListenForButtonPressesThread = controllerConfiguration.listenForButtons(voiceOption == VoiceOptionEnum.TOGGLE);
                this.assignButtonToAction.Enabled = false;
                this.deleteAssigmentButton.Enabled = false;
                this.groupBox1.Enabled = false;
                this.button1.Enabled = false;
                ThreadStart crewChiefWork = runApp;
                Thread crewChiefThread = new Thread(crewChiefWork);

                // this call is not part of the standard AutoUpdater API - I added a 'stopped' flag to prevent the auto updater timer
                // or other Threads firing when the game is running. It's not needed 99% of the time, it just stops that edge case where
                // the AutoUpdater triggers and steals focus while the player is racing
                AutoUpdater.Stop();

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
                if ((voiceOption == VoiceOptionEnum.ALWAYS_ON || voiceOption == VoiceOptionEnum.TOGGLE) && crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised)
                {
                    Console.WriteLine("Stopping listening...");
                    try
                    {                        
                        crewChief.speechRecogniser.waitingForSpeech = false;
                        crewChief.speechRecogniser.recognizeAsyncCancel();
                    }
                    catch (Exception) { }
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
                    this.assignButtonToAction.Text = Configuration.getUIString("waiting_for_button_click_to_cancel");
                    ThreadStart assignButtonWork = assignButton;
                    Thread assignButtonThread = new Thread(assignButtonWork);
                    assignButtonThread.Start();
                }                
            }
            else
            {
                isAssigningButton = false;
                controllerConfiguration.listenForAssignment = false;
                this.assignButtonToAction.Text = Configuration.getUIString("assign");
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
            this.assignButtonToAction.Text = Configuration.getUIString("assign");
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

        private void helpButtonClicked(object sender, EventArgs e)
        {
            var form = new ShowHelp(this);
            form.ShowDialog(this);
        }

        private void aboutButtonClicked(object sender, EventArgs e)
        {
            var form = new ShowAbout(this);
            form.ShowDialog(this);
        }

        private void scanControllersButtonClicked(object sender, EventArgs e)
        {
            controllerConfiguration.controllers = this.controllerConfiguration.scanControllers();
            this.controllersList.Items.Clear();
            if (this.gameDefinitionList.Text.Equals(GameDefinition.pCarsNetwork.friendlyName))
            {
                controllerConfiguration.addNetworkControllerToList();
            }
            foreach (ControllerConfiguration.ControllerData configData in controllerConfiguration.controllers)
            {
                this.controllersList.Items.Add(configData.deviceType.ToString() + " " + configData.deviceName);
            }
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
                    lock (this)
                    {
                        textBox1.Text = "";
                    }
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

        private void startDownload(Boolean isSoundPack)
        {
            using (WebClient wc = new WebClient())
            {
                if (isSoundPack)
                {
                    isDownloadingSoundPack = true;
                    wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(soundpack_DownloadProgressChanged);
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(soundpack_DownloadFileCompleted);
                    try
                    {
                        File.Delete(AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName);
                    }
                    catch (Exception) { }
                    if (getBaseSoundPack)
                    {
                        wc.DownloadFileAsync(new Uri(baseSoundPackDownloadLocation), AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName);
                    }
                    else
                    {
                        wc.DownloadFileAsync(new Uri(updateSoundPackDownloadLocation), AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName);
                    }
                }
                else
                {
                    isDownloadingDriverNames = true;
                    wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(drivernames_DownloadProgressChanged);
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(drivernames_DownloadFileCompleted);
                    try
                    {
                        File.Delete(AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName);
                    }
                    catch (Exception) { }
                    if (getBaseDriverNames) 
                    {
                        wc.DownloadFileAsync(new Uri(baseDriverNamesDownloadLocation),  AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName);
                    }
                    else
                    {
                        wc.DownloadFileAsync(new Uri(updateDriverNamesDownloadLocation), AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName);
                    }
                }
            }
        }

        void soundpack_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            if (percentage > 0)
            {
                soundPackProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
            }
        }

        void drivernames_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            if (percentage > 0)
            {
                driverNamesProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
            }
        }
        void soundpack_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Boolean success = false;
            try
            {
                if (e.Error == null && !e.Cancelled)
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("extracting_sound_pack");
                    if (Directory.Exists(AudioPlayer.soundFilesPath + @"\sounds_temp"))
                    {
                        Directory.Delete(AudioPlayer.soundFilesPath + @"\sounds_temp", true);
                    }
                    ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName, AudioPlayer.soundFilesPath + @"\sounds_temp");
                    // It's important to note that the order of these two calls must *not* matter. If it does, the update process results will be inconsistent.
                    // The update pack can contain file rename instructions and file delete instructions but it can *never* contain obsolete files (or files
                    // with old names). As long as this is the case, it shouldn't matter what order we do these in...
                    UpdateHelper.ProcessFileUpdates(AudioPlayer.soundFilesPath + @"\sounds_temp");
                    UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\sounds_temp", AudioPlayer.soundFilesPath);    
                    success = true;
                    downloadSoundPackButton.Text = Configuration.getUIString("sound_pack_is_up_to_date");
                }
            }
            catch (Exception) { }
            finally
            {
                if (success)
                {
                    try
                    {
                        File.Delete(AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName);
                    }
                    catch (Exception) { }
                }
                soundPackProgressBar.Value = 0;
                isDownloadingSoundPack = false;                    
                if (success && !isDownloadingDriverNames)
                {
                    doRestart();
                }
            }
            if (!success)
            {
                startApplicationButton.Enabled = !isDownloadingDriverNames;
                if (AudioPlayer.soundPackVersion == -1)
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("no_sound_pack_detected_press_to_download");
                }
                else
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("updated_sound_pack_available_press_to_download");
                }
                downloadSoundPackButton.Enabled = true;
                if (!e.Cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_sound_pack"), Configuration.getUIString("unable_to_download_sound_pack"),
                        MessageBoxButtons.OK);
                }
            }
        }
        void drivernames_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            Boolean success = false;
            try
            {
                if (e.Error == null && !e.Cancelled)
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("extracting_driver_names");
                    if (Directory.Exists(AudioPlayer.soundFilesPath + @"\driver_names_temp"))
                    {
                        Directory.Delete(AudioPlayer.soundFilesPath + @"\driver_names_temp", true);
                    }
                    ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName, AudioPlayer.soundFilesPath + @"\driver_names_temp", Encoding.UTF8);
                    UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\driver_names_temp", AudioPlayer.soundFilesPath);
                    success = true;
                    downloadDriverNamesButton.Text = Configuration.getUIString("driver_names_are_up_to_date");
                }
            }
            catch (Exception) { }
            finally
            {
                if (success)
                {
                    try
                    {
                        File.Delete(AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName);
                    }
                    catch (Exception) { }
                }
                driverNamesProgressBar.Value = 0;
                isDownloadingDriverNames = false;
                if (success && !isDownloadingSoundPack)
                {
                    doRestart();
                }
            }
            if (!success)
            {
                startApplicationButton.Enabled = !isDownloadingSoundPack;
                if (AudioPlayer.soundPackVersion == -1)
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("no_driver_names_detected_press_to_download");
                }
                else
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("updated_driver_names_available_press_to_download");
                }
                downloadDriverNamesButton.Enabled = true;
                if (e.Error != null)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_driver_names"), Configuration.getUIString("unable_to_download_driver_names"),
                        MessageBoxButtons.OK);
                }
            }
        }

        private void doRestart()
        {
            String warningMessage = Configuration.getUIString("the_application_must_be_restarted_to_load_the_new_sounds");
            if (System.Diagnostics.Debugger.IsAttached)
            {
                warningMessage = "The app must be restarted manually to load the new sounds";
            }
            if (MessageBox.Show(warningMessage, Configuration.getUIString("load_new_sounds"), MessageBoxButtons.OK) == DialogResult.OK)
            {
                if (!System.Diagnostics.Debugger.IsAttached)
                {
                    System.Diagnostics.Process.Start(Application.ExecutablePath, String.Join(" ", Environment.GetCommandLineArgs())); // to start new instance of application
                    this.Close(); //to turn off current app
                }
            }   
        }

        private void downloadSoundPackButtonPress(object sender, EventArgs e)
        {
            if (AudioPlayer.soundPackLanguage == null)
            {
                DialogResult dialogResult = MessageBox.Show(Configuration.getUIString("unknown_sound_pack_language_text"),
                    Configuration.getUIString("unknown_sound_pack_language_title"), MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    startApplicationButton.Enabled = false;
                    downloadSoundPackButton.Text = Configuration.getUIString("downloading_sound_pack");
                    downloadSoundPackButton.Enabled = false;
                    startDownload(true);
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                startApplicationButton.Enabled = false;
                downloadSoundPackButton.Text = Configuration.getUIString("downloading_sound_pack");
                downloadSoundPackButton.Enabled = false;
                startDownload(true);
            }
        }
        private void downloadDriverNamesButtonPress(object sender, EventArgs e)
        {
            if (AudioPlayer.soundPackLanguage == null)
            {
                DialogResult dialogResult = MessageBox.Show(Configuration.getUIString("unknown_driver_names_language_text"),
                    Configuration.getUIString("unknown_driver_names_language_title"), MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    startApplicationButton.Enabled = false;
                    downloadDriverNamesButton.Text = Configuration.getUIString("downloading_driver_names");
                    downloadDriverNamesButton.Enabled = false;
                    startDownload(false);
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                startApplicationButton.Enabled = false;
                downloadDriverNamesButton.Text = Configuration.getUIString("downloading_driver_names");
                downloadDriverNamesButton.Enabled = false;
                startDownload(false);
            }
        }
    }

    public class ControlWriter : TextWriter
    {
        public TextBox textbox = null;
        public Boolean enable = true;
        public ControlWriter(TextBox textbox)
        {
            this.textbox = textbox;
        }

        public override void WriteLine(string value)
        {
            if (enable)
            {
                StringBuilder sb = new StringBuilder();
                sb.Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append(" : ").Append(value).AppendLine();
                if (textbox != null && !textbox.IsDisposed)
                {
                    try
                    {
                        lock (this)
                        {
                            textbox.AppendText(sb.ToString());
                        }
                    }
                    catch (Exception)
                    {
                        // swallow - nothing to log it to
                    }
                }
            }
        }

        public override Encoding Encoding
        {
            get { return Encoding.UTF8; }
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
