using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Threading;
using System.IO;
using System.Runtime.InteropServices;
using AutoUpdaterDotNET;
using System.Net;
using System.Xml.Linq;
using System.IO.Compression;
using CrewChiefV4.Audio;
using CrewChiefV4.UserInterface;
using System.Diagnostics;
using CrewChiefV4.commands;
using CrewChiefV4.GameState;

namespace CrewChiefV4
{
    public partial class MainWindow : Form
    {
        // used when retrying downloads:
        private String retryReplace = null;
        private String retryReplaceWith = null;
        private Boolean usingRetryAddressForSoundPack = false;
        private Boolean usingRetryAddressForDriverNames = false;
        private Boolean usingRetryAddressForPersonalisations = false;


        private String baseDriverNamesDownloadLocation;
        private String updateDriverNamesDownloadLocation;
        private String driverNamesTempFileName = "temp_driver_names.zip";
        private Boolean getBaseDriverNames = false;

        private String baseSoundPackDownloadLocation;
        private String updateSoundPackDownloadLocation;
        private String update2SoundPackDownloadLocation;
        private String soundPackTempFileName = "temp_sound_pack.zip";
        private Boolean getBaseSoundPack = false;
        private Boolean getFirstUpdateSoundPack = false;

        private String basePersonalisationsDownloadLocation;
        private String updatePersonalisationsDownloadLocation;
        private String update2PersonalisationsDownloadLocation;
        private String personalisationsTempFileName = "temp_personalisations.zip";
        private Boolean getBasePersonalisations = false;
        private Boolean getFirstUpdatePersonalisations = false;


        private Boolean isDownloadingDriverNames = false;
        private Boolean isDownloadingSoundPack = false;
        private Boolean isDownloadingPersonalisations = false;
        private Boolean newSoundPackAvailable = false;
        private Boolean newDriverNamesAvailable = false;
        private Boolean newPersonalisationsAvailable = false;

        private ControllerConfiguration controllerConfiguration;

        private CrewChief crewChief;

        private Boolean isAssigningButton = false;

        private bool _IsAppRunning;

        private Boolean runListenForChannelOpenThread = false;

        private Boolean runListenForButtonPressesThread = false;

        private TimeSpan buttonCheckInterval = TimeSpan.FromMilliseconds(100);

        public static VoiceOptionEnum voiceOption;

        // the new update stuff all hosted on the CrewChief website
        // this is the physical file:
        // private static String autoUpdateXMLURL1 = "http://thecrewchief.org/downloads/auto_update_data_primary.xml";
        // this is the file accessed via the PHP download script:
        private static String autoUpdateXMLURL1 = "http://thecrewchief.org/downloads.php?do=downloadxml";

        // the legacy update stuff hosted on GoogleDrive with downloads on the isnais ftp server
        private static String autoUpdateXMLURL2 = "https://drive.google.com/uc?export=download&id=0B4KQS820QNFbWWFjaDAzRldMNUE";

        private Boolean preferAlternativeDownloadSite = UserSettings.GetUserSettings().getBoolean("prefer_alternative_download_site");
        private Boolean minimizeToTray = UserSettings.GetUserSettings().getBoolean("minimize_to_tray");
        private Boolean rejectMessagesWhenTalking = UserSettings.GetUserSettings().getBoolean("reject_message_when_talking");
        public static Boolean forceMinWindowSize = UserSettings.GetUserSettings().getBoolean("force_min_window_size");

        private float latestSoundPackVersion = -1;
        private float latestDriverNamesVersion = -1;
        private float latestPersonalisationsVersion = -1;

        private ControlWriter cw = null;

        private float currentVolume = -1;
        private NotifyIcon notificationTrayIcon;
        private ToolStripItem contextMenuStartItem;
        private ToolStripItem contextMenuStopItem;
        private ToolStripMenuItem contextMenuGamesMenu;
        private ToolStripItem contextMenuPreferencesItem;

        public static MainWindow instance = null;

        public void killChief()
        {
            crewChief.stop();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            if (forceMinWindowSize)
            {
                this.MinimumSize = new System.Drawing.Size(1160, 730);
            }
            // Some update test code - uncomment this to allow the app to process an update .zip file in the root of the sound pack
            /*
            ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName, AudioPlayer.soundFilesPath + @"\sounds_temp");
            UpdateHelper.ProcessFileUpdates(AudioPlayer.soundFilesPath + @"\sounds_temp");
            UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\sounds_temp", AudioPlayer.soundFilesPath);                   
            */

            // do the auto updating stuff in a separate Thread
            if (!CrewChief.Debugging || 
                AudioPlayer.soundPackVersion <= 0 || AudioPlayer.personalisationsVersion <= 0 || AudioPlayer.driverNamesVersion <=0)
            {
                new Thread(() =>
                {
                    Console.WriteLine("Checking for updates");
                    String firstUpdate = preferAlternativeDownloadSite ? autoUpdateXMLURL2 : autoUpdateXMLURL1;
                    String secondUpdate = preferAlternativeDownloadSite ? autoUpdateXMLURL1 : autoUpdateXMLURL2;

                    Thread.CurrentThread.IsBackground = true;
                    // now the sound packs
                    downloadSoundPackButton.Text = Configuration.getUIString("checking_sound_pack_version");
                    downloadDriverNamesButton.Text = Configuration.getUIString("checking_driver_names_version");
                    downloadPersonalisationsButton.Text = Configuration.getUIString("checking_personalisations_version");
                    XDocument doc = null;
                    Boolean gotUpdateData = false;
                    try
                    {
                        AutoUpdater.Start(firstUpdate);
                        string xml = new WebClient().DownloadString(firstUpdate);
                        doc = XDocument.Parse(xml);
                        if (doc.Descendants("soundpack").Count() > 0)
                        {
                            gotUpdateData = true;
                            Console.WriteLine("Got update data from primary URL: " + firstUpdate.Substring(0, 24));
                        }
                        else
                        {
                            Console.WriteLine("Unable to get update data with primary URL, trying secondary");
                        }
                    }
                    catch (Exception updateException)
                    {
                        Console.WriteLine("Unable to start auto updater with primary URL, trying secondary. Error: " + updateException.Message);
                    }
                    if (!gotUpdateData)
                    {
                        try
                        {
                            AutoUpdater.Start(secondUpdate);
                            string xml = new WebClient().DownloadString(secondUpdate);
                            doc = XDocument.Parse(xml);
                            if (doc.Descendants("soundpack").Count() > 0)
                            {
                                gotUpdateData = true;
                                Console.WriteLine("Got update data from secondary URL: " + secondUpdate.Substring(0, 24));
                            }
                            else
                            {
                                Console.WriteLine("Unable to get update data with secondary URL, unable to check for updates");
                            }
                        }
                        catch (Exception updateException)
                        {
                            Console.WriteLine("Unable to start auto updater with secondary URL, unable to check for updates. Error: " + updateException.Message);
                        }
                    }
                    if (gotUpdateData)
                    {
                        try
                        {
                            String languageToCheck = AudioPlayer.soundPackLanguage == null ? "en" : AudioPlayer.soundPackLanguage;
                            Boolean gotLanguageSpecificUpdateInfo = false;
                            try
                            {
                                retryReplace = doc.Descendants("retry_replace").First().Value;
                                retryReplaceWith = doc.Descendants("retry_replace_with").First().Value;
                            }
                            catch (Exception)
                            {
                                Console.WriteLine("No retry download location available");
                            }

                            try
                            {
                                foreach (XElement element in doc.Descendants("soundpack"))
                                {
                                    XAttribute languageAttribute = element.Attribute(XName.Get("language", ""));
                                    if (languageAttribute.Value == languageToCheck)
                                    {
                                        // this is the update set for this language
                                        float.TryParse(element.Descendants("soundpackversion").First().Value, out latestSoundPackVersion);
                                        float.TryParse(element.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion);
                                        float.TryParse(element.Descendants("personalisationsversion").First().Value, out latestPersonalisationsVersion);
                                        baseSoundPackDownloadLocation = element.Descendants("basesoundpackurl").First().Value;
                                        baseDriverNamesDownloadLocation = element.Descendants("basedrivernamesurl").First().Value;
                                        basePersonalisationsDownloadLocation = element.Descendants("basepersonalisationsurl").First().Value;
                                        updateSoundPackDownloadLocation = element.Descendants("updatesoundpackurl").First().Value;
                                        update2SoundPackDownloadLocation = element.Descendants("update2soundpackurl").First().Value;
                                        updateDriverNamesDownloadLocation = element.Descendants("updatedrivernamesurl").First().Value;
                                        updatePersonalisationsDownloadLocation = element.Descendants("updatepersonalisationsurl").First().Value;
                                        update2PersonalisationsDownloadLocation = doc.Descendants("update2personalisationsurl").First().Value;
                                        gotLanguageSpecificUpdateInfo = true;
                                        break;
                                    }
                                }
                                if (!gotLanguageSpecificUpdateInfo && AudioPlayer.soundPackLanguage == null)
                                {
                                    float.TryParse(doc.Descendants("soundpackversion").First().Value, out latestSoundPackVersion);
                                    float.TryParse(doc.Descendants("drivernamesversion").First().Value, out latestDriverNamesVersion);
                                    float.TryParse(doc.Descendants("personalisationsversion").First().Value, out latestPersonalisationsVersion);
                                    baseSoundPackDownloadLocation = doc.Descendants("basesoundpackurl").First().Value;
                                    baseDriverNamesDownloadLocation = doc.Descendants("basedrivernamesurl").First().Value;
                                    basePersonalisationsDownloadLocation = doc.Descendants("basepersonalisationsurl").First().Value;
                                    updateSoundPackDownloadLocation = doc.Descendants("updatesoundpackurl").First().Value;
                                    update2SoundPackDownloadLocation = doc.Descendants("update2soundpackurl").First().Value;
                                    updateDriverNamesDownloadLocation = doc.Descendants("updatedrivernamesurl").First().Value;
                                    updatePersonalisationsDownloadLocation = doc.Descendants("updatepersonalisationsurl").First().Value;
                                    update2PersonalisationsDownloadLocation = doc.Descendants("update2personalisationsurl").First().Value;
                                }
                            }
                            catch (Exception e2)
                            {
                                // can't parse the auto update XML
                                Console.WriteLine("Unable to process auto-update data" + e2.Message);
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
                                    // if we're on an old update sound pack, get the first (large) update
                                    getFirstUpdateSoundPack = AudioPlayer.soundPackVersion < AudioPlayer.lastUpdateSoundPackVersion;
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
                            if (latestDriverNamesVersion == -1 && AudioPlayer.driverNamesVersion == -1)
                            {
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
                            if (latestPersonalisationsVersion == -1 && AudioPlayer.personalisationsVersion == -1)
                            {
                                downloadPersonalisationsButton.Text = Configuration.getUIString("no_personalisations_detected_unable_to_locate_update");
                                downloadPersonalisationsButton.Enabled = false;
                                downloadPersonalisationsButton.BackColor = Color.LightGray;
                            }
                            else if (latestPersonalisationsVersion > AudioPlayer.personalisationsVersion)
                            {
                                downloadPersonalisationsButton.Enabled = true;
                                downloadPersonalisationsButton.BackColor = Color.LightGreen;
                                if (AudioPlayer.personalisationsVersion == -1)
                                {
                                    downloadPersonalisationsButton.Text = Configuration.getUIString("no_personalisations_detected_press_to_download");
                                    getBasePersonalisations = true;
                                }
                                else
                                {
                                    getFirstUpdatePersonalisations = AudioPlayer.personalisationsVersion < AudioPlayer.lastUpdatePersonalisationsVersion;
                                    downloadPersonalisationsButton.Text = Configuration.getUIString("updated_personalisations_available_press_to_download");
                                }
                                newPersonalisationsAvailable = true;
                            }
                            else
                            {
                                downloadPersonalisationsButton.Text = Configuration.getUIString("personalisations_are_up_to_date");
                                downloadPersonalisationsButton.Enabled = false;
                                downloadPersonalisationsButton.BackColor = Color.LightGray;
                            }

                            if (newSoundPackAvailable || newPersonalisationsAvailable || newDriverNamesAvailable)
                            {
                                // Ok, we have something available for download (any of the buttons is green).
                                // Restore CC once so that user gets higher chance of noticing.
                                // I am not sure what is the best approach, we could also have text in the context menu,
                                // but I definitely dislike Balloons and other distracting methods.  But, basically if we choose
                                // to do anything, do it here.
                                // This has limitation if, say, we have sound pack available, and at next startup we have driver pack
                                // available, one property is not enough.  But this is ultra rare and not worth complications.

                                if (!UserSettings.GetUserSettings().getBoolean("update_notify_attempted"))
                                {
                                    // Do this once per update availability.
                                    UserSettings.GetUserSettings().setProperty("update_notify_attempted", true);
                                    UserSettings.GetUserSettings().saveUserSettings();

                                    // Slight race with minimize on startup :D
                                    this.Invoke((MethodInvoker)delegate
                                    {
                                        this.RestoreFromTray();
                                    });
                                }
                            }
                            else
                            {
                                // If there are no updates available, clear the update_notify_attempted flag if it is set.
                                if (UserSettings.GetUserSettings().getBoolean("update_notify_attempted"))
                                {
                                    UserSettings.GetUserSettings().setProperty("update_notify_attempted", false);
                                    UserSettings.GetUserSettings().saveUserSettings();
                                }
                            }

                            Console.WriteLine("Check for updates completed");
                        }
                        catch (Exception error)
                        {
                            Console.WriteLine("Unable to get auto update details: " + error.Message);
                        }
                    }
                }).Start();
            }
            else
            {
                Console.WriteLine("Skipping update check in debug mode");
            }

            if (UserSettings.GetUserSettings().getBoolean("minimize_on_startup"))
            {
                if (this.minimizeToTray)
                    this.HideToTray();
                else
                    this.WindowState = FormWindowState.Minimized;
            }
        }

        private void HideToTray()
        {
            if (!this.minimizeToTray)
                return;

            this.ShowInTaskbar = false;
            this.Hide();
            this.notificationTrayIcon.Visible = true;

            // Do not mess with WindowState here, causes weirdest problems.
        }

        private void RestoreFromTray()
        {
            if (!this.minimizeToTray)
                return;

            this.ShowInTaskbar = true;
            this.notificationTrayIcon.Visible = false;
            this.Show();

            // This is necessary to bring window to the foreground.  Why ffs BringToFront doesn't work is beyound me.
            this.WindowState = FormWindowState.Normal;
        }

        public void updateMessagesVolume(float messagesVolume)
        {
            currentVolume = messagesVolume;
            setMessagesVolume(messagesVolume, false);
            messagesVolumeSlider.Value = (int)(messagesVolume * 10f);
        }

        private void messagesVolumeSlider_Scroll(object sender, EventArgs e)
        {
            float volFloat = (float)messagesVolumeSlider.Value / 10;
            setMessagesVolume(volFloat, false);
            currentVolume = volFloat;
            UserSettings.GetUserSettings().setProperty("messages_volume", volFloat);
            UserSettings.GetUserSettings().saveUserSettings();
        }

        private void setMessagesVolume(float vol, Boolean changeEvenIfUsingNaudio)
        {
            if (changeEvenIfUsingNaudio || !UserSettings.GetUserSettings().getBoolean("use_naudio"))
            {
                int NewVolume = (int)(((float)ushort.MaxValue) * vol);
                // Set the same volume for both the left and the right channels
                uint NewVolumeAllChannels = (((uint)NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));
                // Set the volume
                NativeMethods.waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
            }
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
                downloadPersonalisationsButton.Enabled = !value && newPersonalisationsAvailable;
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
                    else if (arg.Equals(GameDefinition.pCars2.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set PCars 2 mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.pCars2.friendlyName;
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
                    else if (arg.Equals(GameDefinition.pCars2Network.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set PCars2 network mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.pCars2Network.friendlyName;
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
                    else if (arg.Equals(GameDefinition.iracing.gameEnum.ToString()))
                    {
                        Console.WriteLine("Set iRacing mode from command line");
                        this.gameDefinitionList.Text = GameDefinition.iracing.friendlyName;
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
                            Console.WriteLine("Set " + gameDefinition.friendlyName + " mode from previous launch");
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
            MacroManager.stop();
            lock (cw)
            {
                cw.textbox = null;
                cw.Dispose();
            }
        }

        private void SetupNotificationTrayIcon()
        {
            Debug.Assert(notificationTrayIcon == null, "Supposed to be called once");

            notificationTrayIcon = new NotifyIcon();

            // Load the icon.
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            notificationTrayIcon.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));

            notificationTrayIcon.DoubleClick += NotifyIcon_DoubleClick;

            // Setup the context menu.
            var cms = new ContextMenuStrip();

            // Restore item.
            var cmi = cms.Items.Add(Configuration.getUIString("restore_context_menu"));
            cmi.Click += NotifyIcon_DoubleClick;

            // Start/Stop items.
            cms.Items.Add(new ToolStripSeparator());
            contextMenuStartItem = cms.Items.Add(Configuration.getUIString("start_application"), null, this.startApplicationButton_Click);
            contextMenuStopItem = cms.Items.Add(Configuration.getUIString("stop"), null, this.startApplicationButton_Click);
            cms.Items.Add(new ToolStripSeparator());

            // Form Game context submenu.
            cmi = cms.Items.Add(Configuration.getUIString("game"));
            contextMenuGamesMenu = cmi as ToolStripMenuItem;
            foreach (var game in this.gameDefinitionList.Items)
            {
                var ddi = contextMenuGamesMenu.DropDownItems.Add(game.ToString());
                ddi.Click += (sender, e) =>
                {
                    var gameSelected = sender as ToolStripMenuItem;
                    if (gameSelected == null)
                        return;

                    this.gameDefinitionList.Text = gameSelected.Text;
                };
            }

            contextMenuGamesMenu.DropDownOpening += (sender, e) =>
            {
                var currGameFriendlyName = this.gameDefinitionList.Text;
                foreach (var game in contextMenuGamesMenu.DropDownItems)
                {
                    var tsmi = game as ToolStripMenuItem;
                    tsmi.Checked = tsmi.Text == currGameFriendlyName;
                }
            };

            // Preferences and Close items
            contextMenuPreferencesItem = cms.Items.Add(Configuration.getUIString("properties"), null, this.editPropertiesButtonClicked);
            cms.Items.Add(new ToolStripSeparator());
            cmi = cms.Items.Add(Configuration.getUIString("close_context_menu"));
            cmi.Click += (sender, e) =>
            {
                this.notificationTrayIcon.Visible = false;
                this.Close();
            };

            cms.Opening += (sender, e) =>
            {
                this.contextMenuStartItem.Enabled = !this._IsAppRunning;
                this.contextMenuStopItem.Enabled = this._IsAppRunning;

                this.contextMenuStartItem.Text = string.IsNullOrWhiteSpace(this.gameDefinitionList.Text)
                    ? Configuration.getUIString("start_application")
                    : string.Format(Configuration.getUIString("start_context_menu"), this.gameDefinitionList.Text);

                // Only allow game selection if we're in a Stopped state.
                foreach (var game in this.contextMenuGamesMenu.DropDownItems)
                    (game as ToolStripMenuItem).Enabled = !this._IsAppRunning;
            };


            notificationTrayIcon.ContextMenuStrip = cms;
        }

        private void NotifyIcon_DoubleClick(object sender, EventArgs e)
        {
            this.RestoreFromTray();
        }

        public MainWindow()
        {
            MainWindow.instance = this;

            InitializeComponent();
            SetupNotificationTrayIcon();

            if (CrewChief.Debugging)
            {
                // Restore last saved trace file name.
                filenameTextbox.Text = UserSettings.GetUserSettings().getString("last_trace_file_name");
                filenameTextbox.TextChanged += MainWindow_TextChanged;
            }

            CheckForIllegalCrossThreadCalls = false;
            cw = new ControlWriter(consoleTextBox);
            consoleTextBox.KeyDown += TextBoxConsole_KeyDown;
            Console.SetOut(cw);

            // if we can't init the UserSettings the app will basically be fucked. So try to nuke the Britton_IT_Ltd directory from
            // orbit (it's the only way to be sure) then restart the app. This shit is comically flakey but what else can we do here?
            if (UserSettings.GetUserSettings().initFailed)
            {
                Console.WriteLine("Unable to upgrade properties from previous version, settings will be reset to default");
                try
                {
                    UserSettings.ForceablyDeleteConfigDirectory();
                    // note we can't load these from the UI settings because loading stuff will be broken at this point
                    doRestart("Failed to load user settings, app must be restarted to try again.", "Failed to load user settings");
                }
                catch (Exception)
                {
                    // oh dear, now we are in a pickle.
                    Console.WriteLine("Unable to remove broken app settings file\n Please exit the app and manually delete folder " + UserSettings.userConfigFolder);
                }
            }

            Console.WriteLine("Starting app");
            controllerConfiguration = new ControllerConfiguration(this);
            setSelectedGameType();
            this.app_version.Text = Configuration.getUIString("version") + ": " + System.Reflection.Assembly.GetExecutingAssembly().GetName().Version.ToString();
            this.filenameLabel.Visible = CrewChief.Debugging;
            this.filenameTextbox.Visible = CrewChief.Debugging;
            this.playbackInterval.Visible = CrewChief.Debugging;
            if (CrewChief.Debugging)
            {
                this.recordSession.Visible = true;
            }
            else
            {
                this.recordSession.Visible = false;
                String[] commandLineArgs = Environment.GetCommandLineArgs();
                if (commandLineArgs != null)
                {
                    foreach (String arg in commandLineArgs)
                    {
                        if (arg.Equals("DEBUG"))
                        {
                            Console.WriteLine("Dump-to-file enabled");
                            this.recordSession.Visible = true;
                            this.recordSession.Checked = true;
                            break;
                        }
                        if (arg.Equals("DEBUG_WITH_PLAYBACK"))
                        {
                            Console.WriteLine("Dump-to-file and playback controls enabled");
                            this.recordSession.Visible = true;
                            this.recordSession.Checked = false;
                            this.playbackInterval.Visible = true;
                            this.filenameLabel.Visible = true;
                            this.filenameTextbox.Visible = true;
                            break;
                        }
                    }
                }
            }

            if (!UserSettings.GetUserSettings().getBoolean("enable_console_logging"))
            {
                Console.WriteLine("Console logging has been disabled ('enable_console_logging' property)");
            }
            cw.enable = UserSettings.GetUserSettings().getBoolean("enable_console_logging");

            if (UserSettings.GetUserSettings().getBoolean("use_naudio"))
            {
                this.messagesAudioDeviceBox.Enabled = true;
                this.messagesAudioDeviceBox.Visible = true;
                this.messagesAudioDeviceLabel.Visible = true;
                this.messagesAudioDeviceBox.Items.AddRange(AudioPlayer.playbackDevices.Keys.ToArray());
                // only register the value changed listener after loading the available values
                String messagesPlaybackGuid = UserSettings.GetUserSettings().getString("NAUDIO_DEVICE_GUID_MESSAGES");
                Boolean foundMessagesDeviceGuid = false;
                if (messagesPlaybackGuid != null)
                {
                    foreach (KeyValuePair<string, Tuple<string, int>> entry in AudioPlayer.playbackDevices)
                    {
                        if (messagesPlaybackGuid.Equals(entry.Value.Item1))
                        {
                            this.messagesAudioDeviceBox.Text = entry.Key;
                            AudioPlayer.naudioMessagesPlaybackDeviceId = entry.Value.Item2;
                            foundMessagesDeviceGuid = true;
                            break;
                        }
                    }
                }
                if (!foundMessagesDeviceGuid && AudioPlayer.playbackDevices.Count > 0)
                {
                    this.messagesAudioDeviceBox.Text = AudioPlayer.playbackDevices.First().Key;
                }
                this.messagesAudioDeviceBox.SelectedValueChanged += new System.EventHandler(this.messagesAudioDeviceSelected);

                this.backgroundAudioDeviceBox.Enabled = true;
                this.backgroundAudioDeviceBox.Visible = true;
                this.backgroundAudioDeviceLabel.Visible = true;
                this.backgroundAudioDeviceBox.Items.AddRange(AudioPlayer.playbackDevices.Keys.ToArray());
                String backgroundPlaybackGuid = UserSettings.GetUserSettings().getString("NAUDIO_DEVICE_GUID_BACKGROUND");
                // only register the value changed listener after loading the available values
                Boolean foundBackgroundDeviceGuid = false;
                if (backgroundPlaybackGuid != null)
                {
                    foreach (KeyValuePair<string, Tuple<string, int>> entry in AudioPlayer.playbackDevices)
                    {
                        if (backgroundPlaybackGuid.Equals(entry.Value.Item1))
                        {
                            this.backgroundAudioDeviceBox.Text = entry.Key;
                            AudioPlayer.naudioBackgroundPlaybackDeviceId = entry.Value.Item2;
                            foundBackgroundDeviceGuid = true;
                            break;
                        }
                    }
                }
                if (!foundBackgroundDeviceGuid && AudioPlayer.playbackDevices.Count > 0)
                {
                    this.backgroundAudioDeviceBox.Text = AudioPlayer.playbackDevices.First().Key;
                }
                this.messagesAudioDeviceBox.SelectedValueChanged += new System.EventHandler(this.messagesAudioDeviceSelected);
                this.backgroundAudioDeviceBox.SelectedValueChanged += new System.EventHandler(this.backgroundAudioDeviceSelected);
            }

            if (UserSettings.GetUserSettings().getBoolean("use_naudio_for_speech_recognition"))
            {
                this.speechRecognitionDeviceBox.Enabled = true;
                this.speechRecognitionDeviceBox.Visible = true;
                this.speechRecognitionDeviceLabel.Visible = true;
                this.speechRecognitionDeviceBox.Items.AddRange(SpeechRecogniser.speechRecognitionDevices.Keys.ToArray());
                // only register the value changed listener after loading the available values
                String speechRecognitionDeviceGuid = UserSettings.GetUserSettings().getString("NAUDIO_RECORDING_DEVICE_GUID");
                Boolean foundspeechRecognitionDeviceGuid = false;
                if (speechRecognitionDeviceGuid != null)
                {
                    foreach (KeyValuePair<string, Tuple<string, int>> entry in SpeechRecogniser.speechRecognitionDevices)
                    {
                        if (speechRecognitionDeviceGuid.Equals(entry.Value.Item1))
                        {
                            this.speechRecognitionDeviceBox.Text = entry.Key;
                            SpeechRecogniser.initialSpeechInputDeviceIndex = entry.Value.Item2;
                            foundspeechRecognitionDeviceGuid = true;
                            break;
                        }
                    }
                }
                if (!foundspeechRecognitionDeviceGuid && SpeechRecogniser.speechRecognitionDevices.Count > 0)
                {
                    this.speechRecognitionDeviceBox.Text = SpeechRecogniser.speechRecognitionDevices.First().Key;
                }
                this.speechRecognitionDeviceBox.SelectedValueChanged += new System.EventHandler(this.speechRecognitionDeviceSelected);
            }

            crewChief = new CrewChief();
            this.personalisationBox.Items.AddRange(this.crewChief.audioPlayer.personalisationsArray);
            this.spotterNameBox.Items.AddRange(NoisyCartesianCoordinateSpotter.availableSpotters.ToArray());
            if (crewChief.audioPlayer.selectedPersonalisation == null || crewChief.audioPlayer.selectedPersonalisation.Length == 0 ||
                crewChief.audioPlayer.selectedPersonalisation.Equals(AudioPlayer.NO_PERSONALISATION_SELECTED) ||
                !this.crewChief.audioPlayer.personalisationsArray.Contains(crewChief.audioPlayer.selectedPersonalisation))
            {
                this.personalisationBox.Text = AudioPlayer.NO_PERSONALISATION_SELECTED;
            }
            else
            {
                this.personalisationBox.Text = crewChief.audioPlayer.selectedPersonalisation;
            }

            String savedSpotter = UserSettings.GetUserSettings().getString("spotter_name");
            if (savedSpotter != null && savedSpotter.Length > 0 && NoisyCartesianCoordinateSpotter.availableSpotters.Contains(savedSpotter))
            {
                this.spotterNameBox.Text = savedSpotter;
            }
            else
            {
                this.spotterNameBox.Text = NoisyCartesianCoordinateSpotter.defaultSpotterId;
            }
            // only register the value changed listener after loading the saved values
            this.personalisationBox.SelectedValueChanged += new System.EventHandler(this.personalisationSelected);
            this.spotterNameBox.SelectedValueChanged += new System.EventHandler(this.spotterNameSelected);

            float messagesVolume = UserSettings.GetUserSettings().getFloat("messages_volume");
            float backgroundVolume = UserSettings.GetUserSettings().getFloat("background_volume");
            updateMessagesVolume(messagesVolume);
            backgroundVolumeSlider.Value = (int)(backgroundVolume * 10f);

            Console.WriteLine("Loading controller settings");
            getControllers();
            controllerConfiguration.loadSettings(this);
            String customDeviceGuid = UserSettings.GetUserSettings().getString("custom_device_guid");
            if (customDeviceGuid != null && customDeviceGuid.Length > 0)
            {
                try
                {
                    Guid guid;
                    if (Guid.TryParse(customDeviceGuid, out guid))
                    {
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
            }
            else if (voiceOption == VoiceOptionEnum.HOLD)
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

            this.Resize += MainWindow_Resize;
            this.KeyPreview = true;
            this.KeyDown += MainWindow_KeyDown;
        }

        private void MainWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if ( e.KeyCode == Keys.F1)
            {
                this.helpButtonClicked(sender, e);
                e.Handled = true;
            }
        }

        private void MainWindow_TextChanged(object sender, EventArgs e)
        {
            UserSettings.GetUserSettings().setProperty("last_trace_file_name", filenameTextbox.Text);

            // It's awful to save on each character entered, but alternatives are far hairier, so let it be (debug only stuff anyway).
            UserSettings.GetUserSettings().saveUserSettings();
        }

        private void MainWindow_Resize(object sender, EventArgs e)
        {
            if (this.WindowState == FormWindowState.Minimized)
                this.HideToTray();
        }

        private void TextBoxConsole_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Control && e.KeyCode == Keys.A)
            {
                consoleTextBox.SelectAll();
            }
            else if (e.KeyCode == Keys.Escape)
            {
                consoleTextBox.DeselectAll();
            }
        }

        private void listenForChannelOpen()
        {
            Boolean channelOpen = false;
            if (crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised && voiceOption == VoiceOptionEnum.HOLD)
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
                        if (rejectMessagesWhenTalking)
                            crewChief.audioPlayer.muteBackgroundPlayer(true /*mute*/);

                        if (DriverTrainingService.isRecordingPaceNotes)
                        {
                            if (CrewChief.distanceRoundTrack > 0)
                            {
                                Console.WriteLine("Recording pace note...");
                                DriverTrainingService.startRecordingMessage((int)CrewChief.distanceRoundTrack);
                            }
                        }
                        else
                        {
                            Console.WriteLine("Listening for voice command...");
                            crewChief.speechRecogniser.recognizeAsync();
                        }

                        if (rejectMessagesWhenTalking)
                            setMessagesVolume(0.0f, true);
                    }
                    else if (channelOpen && !controllerConfiguration.isChannelOpen())
                    {
                        if (rejectMessagesWhenTalking)
                        {
                            // Drop any outstanding messages queued while user was talking, this should prevent weird half phrases.
                            crewChief.audioPlayer.purgeQueues();

                            setMessagesVolume(currentVolume, true);
                            crewChief.audioPlayer.muteBackgroundPlayer(false /*mute*/);
                        }

                        if (DriverTrainingService.isRecordingPaceNotes)
                        {
                            Console.WriteLine("Saving recorded pace note");
                            DriverTrainingService.stopRecordingMessage();
                        }
                        else
                        {
                            Console.WriteLine("Invoking speech recognition...");
                            crewChief.speechRecogniser.recognizeAsyncCancel();
                            new Thread(() =>
                            {
                                Thread.Sleep(2000);
                                if (!channelOpen && !SpeechRecogniser.gotRecognitionResult)
                                {
                                    crewChief.youWot(false);
                                }
                            }).Start();
                        }
                        channelOpen = false;
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
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_MANUAL_FORMATION_LAP))
                    {
                        Console.WriteLine("Toggling manual formation lap mode");
                        crewChief.toggleManualFormationLapMode();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.READ_CORNER_NAMES_FOR_LAP))
                    {
                        Console.WriteLine("Enabling corner name reading for current lap");
                        crewChief.playCornerNamesForCurrentLap();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.REPEAT_LAST_MESSAGE_BUTTON))
                    {
                        Console.WriteLine("Repeating last message");
                        crewChief.audioPlayer.repeatLastMessage();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_YELLOW_FLAG_MESSAGES))
                    {
                        Console.WriteLine("Toggling yellow flag messages to: " + (CrewChief.yellowFlagMessagesEnabled ? "disabled" : "enabled"));
                        crewChief.toggleEnableYellowFlagsMode();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.GET_FUEL_STATUS))
                    {
                        Console.WriteLine("Getting fuel/battery status");
                        crewChief.reportFuelStatus();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.GET_STATUS))
                    {
                        Console.WriteLine("Getting full status");
                        CrewChief.getStatus();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.GET_SESSION_STATUS))
                    {
                        Console.WriteLine("Getting session status");
                        CrewChief.getSessionStatus();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.GET_DAMAGE_REPORT))
                    {
                        Console.WriteLine("Getting damage report");
                        CrewChief.getDamageReport();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.GET_CAR_STATUS))
                    {
                        Console.WriteLine("Getting car status");
                        CrewChief.getCarStatus();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_PACE_NOTES_RECORDING))
                    {
                        Console.WriteLine("Start / stop pace notes recording");
                        crewChief.togglePaceNotesRecording();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_PACE_NOTES_PLAYBACK))
                    {
                        Console.WriteLine("Start / stop pace notes playback");
                        crewChief.togglePaceNotesPlayback();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.TOGGLE_TRACK_LANDMARKS_RECORDING))
                    {
                        Console.WriteLine("Start / stop track landmark recording");
                        crewChief.toggleTrackLandmarkRecording();
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.ADD_TRACK_LANDMARK))
                    {
                        //dont confirm press here we do that in addLandmark
                        crewChief.toggleAddTrackLandmark();
                        nextPollWait = 1000;
                    }    
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.PRINT_TRACK_DATA))
                    {
                        if (CrewChief.currentGameState != null && CrewChief.currentGameState.SessionData != null &&
                            CrewChief.currentGameState.SessionData.TrackDefinition != null)
                        {
                            string posInfo = "";
                            var worldPos = CrewChief.currentGameState.PositionAndMotionData.WorldPosition;
                            if (worldPos != null && worldPos.Length > 2)
                            {
                                posInfo = string.Format(", position x: {0} y: {1} z:{2}", worldPos[0], worldPos[1], worldPos[2]);
                            }
                            if (CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)
                            {
                                Console.WriteLine("RaceroomLayoutId: " + CrewChief.currentGameState.SessionData.TrackDefinition.id + ", distanceRoundLap = " +
                                    CrewChief.currentGameState.PositionAndMotionData.DistanceRoundTrack + ", player's car ID: " + CrewChief.currentGameState.carClass.getClassIdentifier() + posInfo);
                            }
                            else
                            {
                                Console.WriteLine("TrackName: " + CrewChief.currentGameState.SessionData.TrackDefinition.name + ", distanceRoundLap = " +
                                    CrewChief.currentGameState.PositionAndMotionData.DistanceRoundTrack + ", player's car ID: " + CrewChief.currentGameState.carClass.getClassIdentifier() + posInfo);
                            }
                        }
                        else
                        {
                            Console.WriteLine("No track data available");
                        }
                        nextPollWait = 1000;
                    }
                    else if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.VOLUME_UP))
                    {
                        if (currentVolume == -1)
                        {
                            Console.WriteLine("Initial volume not set, ignoring");
                        }
                        else if (currentVolume >= 1)
                        {
                            Console.WriteLine("Volume at max");
                        }
                        else
                        {
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
                        }
                        else if (currentVolume <= 0)
                        {
                            Console.WriteLine("Volume at min");
                        }
                        else
                        {
                            Console.WriteLine("Decreasing volume");
                            updateMessagesVolume(currentVolume - 0.1f);
                        }
                        nextPollWait = 200;
                    }
                    else if (crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised && voiceOption == VoiceOptionEnum.TOGGLE)
                    {
                        if (controllerConfiguration.hasOutstandingClick(ControllerConfiguration.CHANNEL_OPEN_FUNCTION))
                        {
                            crewChief.speechRecogniser.voiceOptionEnum = VoiceOptionEnum.TOGGLE;
                            if (SpeechRecogniser.waitingForSpeech)
                            {
                                Console.WriteLine("Cancelling...");
                                SpeechRecogniser.waitingForSpeech = false;
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

                MacroManager.initialise(crewChief.audioPlayer, crewChief.speechRecogniser);
                CarData.loadCarClassData();
                TrackData.loadTrackLandmarksData();
                this.runListenForButtonPressesThread = controllerConfiguration.listenForButtons(voiceOption == VoiceOptionEnum.TOGGLE);
                this.assignButtonToAction.Enabled = false;
                this.deleteAssigmentButton.Enabled = false;
                this.groupBox1.Enabled = false;
                this.button1.Enabled = false;
                this.scanControllersButton.Enabled = false;
                this.personalisationBox.Enabled = false;
                this.spotterNameBox.Enabled = false;
                this.recordSession.Enabled = false;
                ThreadStart crewChiefWork = runApp;
                Thread crewChiefThread = new Thread(crewChiefWork);

                // this call is not part of the standard AutoUpdater API - I added a 'stopped' flag to prevent the auto updater timer
                // or other Threads firing when the game is running. It's not needed 99% of the time, it just stops that edge case where
                // the AutoUpdater triggers and steals focus while the player is racing
                AutoUpdater.Stop();

                crewChiefThread.Start();
                runListenForChannelOpenThread = controllerConfiguration.listenForChannelOpen()
                    && voiceOption == VoiceOptionEnum.HOLD && crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised;
                if (runListenForChannelOpenThread && voiceOption == VoiceOptionEnum.HOLD && crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised)
                {
                    Console.WriteLine("Listening on default audio input device");
                    ThreadStart channelOpenButtonListenerWork = listenForChannelOpen;
                    Thread channelOpenButtonListenerThread = new Thread(channelOpenButtonListenerWork);
                    channelOpenButtonListenerThread.Start();
                }
                else if (voiceOption == VoiceOptionEnum.ALWAYS_ON && crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised)
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
                MacroManager.stop();
                if ((voiceOption == VoiceOptionEnum.ALWAYS_ON || voiceOption == VoiceOptionEnum.TOGGLE) && crewChief.speechRecogniser != null && crewChief.speechRecogniser.initialised)
                {
                    Console.WriteLine("Stopping listening...");
                    try
                    {
                        SpeechRecogniser.waitingForSpeech = false;
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
                this.scanControllersButton.Enabled = true;
                this.personalisationBox.Enabled = true;
                this.spotterNameBox.Enabled = true;
                this.recordSession.Enabled = true;
                DriverTrainingService.completeRecordingPaceNotes();
                DriverTrainingService.stopPlayingPaceNotes();
            }

            this.gameDefinitionList.Enabled = !this._IsAppRunning;
            this.contextMenuPreferencesItem.Enabled = !this._IsAppRunning;

            if (this._IsAppRunning)
                this.notificationTrayIcon.Text = string.Format(Configuration.getUIString("running_context_menu"), this.gameDefinitionList.Text);
            else
                this.notificationTrayIcon.Text = Configuration.getUIString("idling_context_menu");  // Or idling, smoking, any good jokes?
        }

        private void stopApp(object sender, FormClosedEventArgs e)
        {
            stopApp();
        }

        private void runApp()
        {
            String filenameToRun = null;
            Boolean record = false;
            if (filenameTextbox.Text != null && filenameTextbox.Text.Count() > 0)
            {
                // This will be reenabled once file is deserialized or on failure.
                this.startApplicationButton.Enabled = false;

                filenameToRun = filenameTextbox.Text;
                if (this.playbackInterval.Text.Length > 0)
                {
                    CrewChief.playbackIntervalMilliseconds = int.Parse(playbackInterval.Text);
                }
            }
            if (recordSession.Checked)
            {
                record = true;
            }
            if (!crewChief.Run(filenameToRun, record))
            {
                this.deleteAssigmentButton.Enabled = this.buttonActionSelect.SelectedIndex > -1 &&
                    this.controllerConfiguration.buttonAssignments[this.buttonActionSelect.SelectedIndex].joystick != null;
                this.assignButtonToAction.Enabled = this.buttonActionSelect.SelectedIndex > -1 && this.controllersList.SelectedIndex > -1;
                stopApp();
                this.button1.Enabled = true;
                this.scanControllersButton.Enabled = true;
                this.personalisationBox.Enabled = true;
                IsAppRunning = false;
            }
        }

        private void stopApp()
        {
            if (this.recordSession.Checked)
            {
                // This will be reenabled once dump to file succeeds or fails.
                this.startApplicationButton.Enabled = false;
            }

            runListenForChannelOpenThread = false;
            runListenForButtonPressesThread = false;
            crewChief.stop();
        }

        private void playbackIntervalChanged(object sender, EventArgs e)
        {
            if (this.playbackInterval.Text.Length > 0)
            {
                try
                {
                    CrewChief.playbackIntervalMilliseconds = int.Parse(playbackInterval.Text);
                }
                catch (Exception)
                {
                    // swallow - not much we can do here
                }
            }
            else
            {
                CrewChief.playbackIntervalMilliseconds = 0;
            }
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

        public void getControllers()
        {
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
                if (crewChief.speechRecogniser != null && !crewChief.speechRecogniser.initialised)
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
            else
            {
                isAssigningButton = false;
            }
            this.assignButtonToAction.Text = Configuration.getUIString("assign");            
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
            // If minized to tray, hide tray icon while properties dialog is shown,
            // and it again when dialog is gone.  The goal is to prevent weird scenarios while
            // option dialog is visible.
            var minimizedToTray = this.notificationTrayIcon.Visible;
            if (minimizedToTray)
                this.notificationTrayIcon.Visible = false;

            try
            {
                var form = new PropertiesForm(this);
                form.ShowDialog(this);
            }
            finally
            {
                if (minimizedToTray)
                    this.notificationTrayIcon.Visible = true;
            }
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

        private void forceVersionCheckButtonClicked(object sender, EventArgs e)
        {
            try
            {
                Microsoft.Win32.Registry.CurrentUser.DeleteSubKeyTree("Software\\Britton IT Ltd");
            }
            catch
            {
            }
            doRestart(Configuration.getUIString("the_application_must_be_restarted_to_check_for_updates"), Configuration.getUIString("check_for_updates"));
        }

        private void saveConsoleOutputButtonClicked(object sender, EventArgs e)
        {
            try
            {
                if(consoleTextBox.Text.Length > 0) 
                {
                    String filename = "console_" + DateTime.Now.ToString("yyyy_MM_dd-HH-mm-ss") + ".txt";
                    String path = System.IO.Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "debugLogs");
                    if (!Directory.Exists(path))
                    {
                        Directory.CreateDirectory(path);
                    }
                    path = System.IO.Path.Combine(path, filename);
                    File.WriteAllText(path, consoleTextBox.Text);
                    Console.WriteLine("Console output saved to " + path);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to save console output, message = " + ex.Message);
            }
        }

        private void scanControllersButtonClicked(object sender, EventArgs e)
        {
            controllerConfiguration.controllers = this.controllerConfiguration.scanControllers();
            this.controllersList.Items.Clear();
            if (this.gameDefinitionList.Text.Equals(GameDefinition.pCarsNetwork.friendlyName) || this.gameDefinitionList.Text.Equals(GameDefinition.pCars2Network.friendlyName))
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

        private void personalisationSelected(object sender, EventArgs e)
        {
            if (!UserSettings.GetUserSettings().getString("PERSONALISATION_NAME").Equals(this.personalisationBox.Text))
            {
                UserSettings.GetUserSettings().setProperty("PERSONALISATION_NAME", this.personalisationBox.Text);
                UserSettings.GetUserSettings().saveUserSettings();
                doRestart(Configuration.getUIString("the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
            }
        }

        private void messagesAudioDeviceSelected(object sender, EventArgs e)
        {
            Tuple<string, int> device = null;
            if (AudioPlayer.playbackDevices.TryGetValue(this.messagesAudioDeviceBox.Text, out device))
            {
                int deviceId = device.Item2;
                AudioPlayer.naudioMessagesPlaybackDeviceId = deviceId;
                UserSettings.GetUserSettings().setProperty("NAUDIO_DEVICE_GUID_MESSAGES",
                    AudioPlayer.playbackDevices[this.messagesAudioDeviceBox.Text].Item1);
                UserSettings.GetUserSettings().saveUserSettings();
            }
        }

        private void speechRecognitionDeviceSelected(object sender, EventArgs e)
        {
            Tuple<string, int> device = null;
            if (SpeechRecogniser.speechRecognitionDevices.TryGetValue(this.speechRecognitionDeviceBox.Text, out device))
            {
                int deviceId = device.Item2;
                crewChief.speechRecogniser.changeInputDevice(deviceId);
                UserSettings.GetUserSettings().setProperty("NAUDIO_RECORDING_DEVICE_GUID",
                    SpeechRecogniser.speechRecognitionDevices[this.speechRecognitionDeviceBox.Text].Item1);
                UserSettings.GetUserSettings().saveUserSettings();
            }
        }

        private void backgroundAudioDeviceSelected(object sender, EventArgs e)
        {
            Tuple<string, int> device = null;
            if (AudioPlayer.playbackDevices.TryGetValue(this.backgroundAudioDeviceBox.Text, out device))
            {
                int deviceId = device.Item2; 
                AudioPlayer.naudioBackgroundPlaybackDeviceId = deviceId;
                UserSettings.GetUserSettings().setProperty("NAUDIO_DEVICE_GUID_BACKGROUND",
                    AudioPlayer.playbackDevices[this.backgroundAudioDeviceBox.Text].Item1);
                UserSettings.GetUserSettings().saveUserSettings();
            }
        }

        private void spotterNameSelected(object sender, EventArgs e)
        {
            if (!UserSettings.GetUserSettings().getString("spotter_name").Equals(this.spotterNameBox.Text))
            {
                UserSettings.GetUserSettings().setProperty("spotter_name", this.spotterNameBox.Text);
                UserSettings.GetUserSettings().saveUserSettings();
                doRestart(Configuration.getUIString("the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
            }
        }

        private VoiceOptionEnum getVoiceOptionEnum(String enumStr)
        {
            VoiceOptionEnum enumVal = VoiceOptionEnum.DISABLED;
            if (enumStr != null && enumStr.Length > 0)
            {
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
            if (!consoleTextBox.IsDisposed)
            {
                try
                {
                    lock (this)
                    {
                        consoleTextBox.Text = "";
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
            if (this.gameDefinitionList.Text.Equals(GameDefinition.pCarsNetwork.friendlyName) || this.gameDefinitionList.Text.Equals(GameDefinition.pCars2Network.friendlyName))
            {
                controllerConfiguration.addNetworkControllerToList();
            }
            else
            {
                controllerConfiguration.removeNetworkControllerFromList();
            }
            getControllers();
        }

        private enum DownloadType
        {
            DRIVER_NAMES, SOUND_PACK, PERSONALISATIONS
        }

        private void startDownload(DownloadType downloadType)
        {
            using (WebClient wc = new WebClient())
            {
                if (downloadType == DownloadType.SOUND_PACK)
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
                    else if (getFirstUpdateSoundPack)
                    {
                        wc.DownloadFileAsync(new Uri(updateSoundPackDownloadLocation), AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName);
                    }
                    else
                    {
                        wc.DownloadFileAsync(new Uri(update2SoundPackDownloadLocation), AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName);
                    }
                }
                else if (downloadType == DownloadType.DRIVER_NAMES)
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
                        wc.DownloadFileAsync(new Uri(baseDriverNamesDownloadLocation), AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName);
                    }
                    else
                    {
                        wc.DownloadFileAsync(new Uri(updateDriverNamesDownloadLocation), AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName);
                    }
                }
                else if (downloadType == DownloadType.PERSONALISATIONS)
                {
                    isDownloadingPersonalisations = true;
                    wc.DownloadProgressChanged += new DownloadProgressChangedEventHandler(personalisations_DownloadProgressChanged);
                    wc.DownloadFileCompleted += new AsyncCompletedEventHandler(personalisations_DownloadFileCompleted);
                    try
                    {
                        File.Delete(AudioPlayer.soundFilesPath + @"\" + personalisationsTempFileName);
                    }
                    catch (Exception) { }
                    if (getBasePersonalisations)
                    {
                        wc.DownloadFileAsync(new Uri(basePersonalisationsDownloadLocation), AudioPlayer.soundFilesPath + @"\" + personalisationsTempFileName);
                    }
                    else if (getFirstUpdatePersonalisations)
                    {
                        wc.DownloadFileAsync(new Uri(updatePersonalisationsDownloadLocation), AudioPlayer.soundFilesPath + @"\" + personalisationsTempFileName);
                    }
                    else
                    {
                        wc.DownloadFileAsync(new Uri(update2PersonalisationsDownloadLocation), AudioPlayer.soundFilesPath + @"\" + personalisationsTempFileName);
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
        void personalisations_DownloadProgressChanged(object sender, DownloadProgressChangedEventArgs e)
        {
            double bytesIn = double.Parse(e.BytesReceived.ToString());
            double totalBytes = double.Parse(e.TotalBytesToReceive.ToString());
            double percentage = bytesIn / totalBytes * 100;
            if (percentage > 0)
            {
                personalisationsProgressBar.Value = int.Parse(Math.Truncate(percentage).ToString());
            }
        }

        void soundpack_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                String extractingButtonText = Configuration.getUIString("extracting_sound_pack");
                downloadSoundPackButton.Text = extractingButtonText;
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Boolean success = false;
                    Thread progressThread = null;
                    try
                    {
                        if (Directory.Exists(AudioPlayer.soundFilesPath + @"\sounds_temp"))
                        {
                            Directory.Delete(AudioPlayer.soundFilesPath + @"\sounds_temp", true);
                        }
                        progressThread = createProgressThread(downloadSoundPackButton, extractingButtonText);
                        progressThread.Start(); 
                        ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + soundPackTempFileName, AudioPlayer.soundFilesPath + @"\sounds_temp");
                        // It's important to note that the order of these two calls must *not* matter. If it does, the update process results will be inconsistent.
                        // The update pack can contain file rename instructions and file delete instructions but it can *never* contain obsolete files (or files
                        // with old names). As long as this is the case, it shouldn't matter what order we do these in...
                        UpdateHelper.ProcessFileUpdates(AudioPlayer.soundFilesPath + @"\sounds_temp");
                        UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\sounds_temp", AudioPlayer.soundFilesPath);
                        success = true;
                    }
                    catch (Exception) { }
                    finally
                    {
                        if (progressThread != null)
                        {
                            progressThread.Abort();
                            Thread.Sleep(100);
                            downloadSoundPackButton.Text = Configuration.getUIString("sound_pack_is_up_to_date");
                        }
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
                        if (success && !isDownloadingDriverNames && !isDownloadingPersonalisations)
                        {
                            doRestart(Configuration.getUIString("the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
                        }
                    }
                    if (!success)
                    {
                        soundPackUpdateFailed(false);
                    }
                }).Start();
            }
            else
            {
                soundPackUpdateFailed(e.Cancelled);
            }
        }

        void drivernames_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                String extractingButtonText = Configuration.getUIString("extracting_driver_names");
                downloadDriverNamesButton.Text = extractingButtonText;
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Boolean success = false;
                    Thread progressThread = null;
                    try
                    {
                        if (Directory.Exists(AudioPlayer.soundFilesPath + @"\driver_names_temp"))
                        {
                            Directory.Delete(AudioPlayer.soundFilesPath + @"\driver_names_temp", true);
                        }
                        progressThread = createProgressThread(downloadDriverNamesButton, extractingButtonText);
                        progressThread.Start(); 
                        ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + driverNamesTempFileName, AudioPlayer.soundFilesPath + @"\driver_names_temp", Encoding.UTF8);
                        UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\driver_names_temp", AudioPlayer.soundFilesPath);
                        success = true;
                    }
                    catch (Exception) { }
                    finally
                    {
                        if (progressThread != null)
                        {
                            progressThread.Abort();
                            Thread.Sleep(100);
                            downloadDriverNamesButton.Text = Configuration.getUIString("driver_names_are_up_to_date");
                        }
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
                        if (success && !isDownloadingSoundPack && !isDownloadingPersonalisations)
                        {
                            doRestart(Configuration.getUIString("the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
                        }
                    }
                    if (!success)
                    {
                        driverNamesUpdateFailed(false);
                    }
                }).Start();
            }
            else
            {
                driverNamesUpdateFailed(e.Cancelled);
            }
        }

        void personalisations_DownloadFileCompleted(object sender, AsyncCompletedEventArgs e)
        {
            if (e.Error == null && !e.Cancelled)
            {
                String extractingButtonText = Configuration.getUIString("extracting_personalisations");
                downloadPersonalisationsButton.Text = extractingButtonText;
                new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    Boolean success = false;
                    Thread progressThread = null;
                    try
                    {
                        if (e.Error == null && !e.Cancelled)
                        {

                            if (Directory.Exists(AudioPlayer.soundFilesPath + @"\personalisations_temp"))
                            {
                                Directory.Delete(AudioPlayer.soundFilesPath + @"\personalisations_temp", true);
                            }
                            progressThread = createProgressThread(downloadPersonalisationsButton, extractingButtonText);
                            progressThread.Start();
                            ZipFile.ExtractToDirectory(AudioPlayer.soundFilesPath + @"\" + personalisationsTempFileName, AudioPlayer.soundFilesPath + @"\personalisations_temp", Encoding.UTF8);
                            UpdateHelper.MoveDirectory(AudioPlayer.soundFilesPath + @"\personalisations_temp", AudioPlayer.soundFilesPath + @"\personalisations");
                            success = true;
                        }
                    }
                    catch (Exception e2)
                    {
                        Console.WriteLine("Error extracting, " + e2.Message);
                    }
                    finally
                    {
                        if (progressThread != null)
                        {
                            progressThread.Abort();
                            Thread.Sleep(100);
                            downloadPersonalisationsButton.Text = Configuration.getUIString("personalisations_are_up_to_date");
                        }
                        if (success)
                        {
                            try
                            {
                                File.Delete(AudioPlayer.soundFilesPath + @"\" + personalisationsTempFileName);
                            }
                            catch (Exception) { }
                        }
                        personalisationsProgressBar.Value = 0;
                        isDownloadingPersonalisations = false;
                        if (success && !isDownloadingSoundPack && !isDownloadingDriverNames)
                        {
                            doRestart(Configuration.getUIString("the_application_must_be_restarted_to_load_the_new_sounds"), Configuration.getUIString("load_new_sounds"));
                        }
                    }
                    if (!success)
                    {
                        personalisationsUpdateFailed(false);
                    }
                }).Start();
            }
            else
            {
                personalisationsUpdateFailed(e.Cancelled);
            }
        }

        // 'ticks' the button so the user knows something's happening
        private Thread createProgressThread(Button button, String text)
        {
            return new Thread(() =>
            {
                Boolean cancelled = false;
                try
                {
                    while (!cancelled)
                    {
                        button.Text = text + ".";
                        Thread.Sleep(300);
                        button.Text = text + "..";
                        Thread.Sleep(300);
                        button.Text = text + "...";
                        Thread.Sleep(300);
                        button.Text = text;
                        Thread.Sleep(300);
                    }
                }
                catch (ThreadAbortException)
                {
                    cancelled = true;
                    Thread.ResetAbort();
                }
            });
        }
        private void driverNamesUpdateFailed(Boolean cancelled)
        {
            if (!cancelled && !usingRetryAddressForDriverNames && retryReplace != null && retryReplaceWith != null)
            {
                Console.WriteLine("Unable to get driver names from " + retryReplace + " will try from " + retryReplaceWith);
                usingRetryAddressForDriverNames = true;
                baseDriverNamesDownloadLocation = baseDriverNamesDownloadLocation.Replace(retryReplace, retryReplaceWith);
                updateDriverNamesDownloadLocation = updateDriverNamesDownloadLocation.Replace(retryReplace, retryReplaceWith);
                startDownload(DownloadType.DRIVER_NAMES);
            }
            else
            {
                startApplicationButton.Enabled = !isDownloadingSoundPack && !isDownloadingPersonalisations;
                if (AudioPlayer.driverNamesVersion == -1)
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("no_driver_names_detected_press_to_download");
                }
                else
                {
                    downloadDriverNamesButton.Text = Configuration.getUIString("updated_driver_names_available_press_to_download");
                }
                downloadDriverNamesButton.Enabled = true;
                if (!cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_driver_names"), Configuration.getUIString("unable_to_download_driver_names"),
                        MessageBoxButtons.OK);
                }
            }
        }

        private void soundPackUpdateFailed(Boolean cancelled)
        {
            if (!cancelled && !usingRetryAddressForSoundPack && retryReplace != null && retryReplaceWith != null)
            {
                Console.WriteLine("Unable to get sound pack from " + retryReplace + " will try from " + retryReplaceWith);
                usingRetryAddressForSoundPack = true;
                baseSoundPackDownloadLocation = baseSoundPackDownloadLocation.Replace(retryReplace, retryReplaceWith);
                updateSoundPackDownloadLocation = updateSoundPackDownloadLocation.Replace(retryReplace, retryReplaceWith);
                update2SoundPackDownloadLocation = update2SoundPackDownloadLocation.Replace(retryReplace, retryReplaceWith);
                startDownload(DownloadType.SOUND_PACK);
            }
            else
            {
                startApplicationButton.Enabled = !isDownloadingDriverNames && !isDownloadingPersonalisations;
                if (AudioPlayer.soundPackVersion == -1)
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("no_sound_pack_detected_press_to_download");
                }
                else
                {
                    downloadSoundPackButton.Text = Configuration.getUIString("updated_sound_pack_available_press_to_download");
                }
                downloadSoundPackButton.Enabled = true;
                if (!cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_sound_pack"), Configuration.getUIString("unable_to_download_sound_pack"),
                        MessageBoxButtons.OK);
                }
            }
        }

        private void personalisationsUpdateFailed(Boolean cancelled)
        {
            if (!cancelled && !usingRetryAddressForPersonalisations && retryReplace != null && retryReplaceWith != null)
            {
                Console.WriteLine("Unable to get personalisations from " + retryReplace + " will try from " + retryReplaceWith);
                usingRetryAddressForPersonalisations = true;
                basePersonalisationsDownloadLocation = basePersonalisationsDownloadLocation.Replace(retryReplace, retryReplaceWith);
                updatePersonalisationsDownloadLocation = updatePersonalisationsDownloadLocation.Replace(retryReplace, retryReplaceWith);
                update2PersonalisationsDownloadLocation = update2PersonalisationsDownloadLocation.Replace(retryReplace, retryReplaceWith);
                startDownload(DownloadType.PERSONALISATIONS);
            }
            else
            {
                startApplicationButton.Enabled = !isDownloadingSoundPack && !isDownloadingDriverNames;
                if (AudioPlayer.personalisationsVersion == -1)
                {
                    downloadPersonalisationsButton.Text = Configuration.getUIString("no_personalisations_detected_press_to_download");
                }
                else
                {
                    downloadPersonalisationsButton.Text = Configuration.getUIString("updated_personalisations_available_press_to_download");
                }
                downloadPersonalisationsButton.Enabled = true;
                if (!cancelled)
                {
                    MessageBox.Show(Configuration.getUIString("error_downloading_personalisations"), Configuration.getUIString("unable_to_download_personalisations"),
                        MessageBoxButtons.OK);
                }
            }
        }
        
        private void doRestart(String warningMessage, String warningTitle)
        {
            if (CrewChief.Debugging)
            {
                warningMessage = "The app must be restarted manually";
            }

            // Make app visible first.
            this.RestoreFromTray();

            if (MessageBox.Show(warningMessage, warningTitle,
                CrewChief.Debugging ? MessageBoxButtons.OK : MessageBoxButtons.OKCancel) == DialogResult.OK)
            {
                if (!CrewChief.Debugging)
                {
                    // have to add "multi" to the start args so the app can restart
                    List<String> startArgs = new List<string>();
                    startArgs.AddRange(Environment.GetCommandLineArgs());
                    if (!startArgs.Contains("multi"))
                    {
                        startArgs.Add("multi");
                    }
                    System.Diagnostics.Process.Start(Application.ExecutablePath, String.Join(" ", startArgs.ToArray())); // to start new instance of application
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
                    startDownload(DownloadType.SOUND_PACK);
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
                startDownload(DownloadType.SOUND_PACK);
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
                    startDownload(DownloadType.DRIVER_NAMES);
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
                startDownload(DownloadType.DRIVER_NAMES);
            }
        }
        private void downloadPersonalisationsButtonPress(object sender, EventArgs e)
        {
            if (AudioPlayer.soundPackLanguage == null)
            {
                DialogResult dialogResult = MessageBox.Show(Configuration.getUIString("unknown_personalisations_language_text"),
                    Configuration.getUIString("unknown_personalisations_language_title"), MessageBoxButtons.YesNo);
                if (dialogResult == DialogResult.Yes)
                {
                    startApplicationButton.Enabled = false;
                    downloadPersonalisationsButton.Text = Configuration.getUIString("downloading_personalisations");
                    downloadPersonalisationsButton.Enabled = false;
                    startDownload(DownloadType.PERSONALISATIONS);
                }
                else if (dialogResult == DialogResult.No)
                {
                }
            }
            else
            {
                startApplicationButton.Enabled = false;
                downloadPersonalisationsButton.Text = Configuration.getUIString("downloading_personalisations");
                downloadPersonalisationsButton.Enabled = false;
                startDownload(DownloadType.PERSONALISATIONS);
            }
        }

        private void personalisationBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void spotterNameBox_SelectedIndexChanged(object sender, EventArgs e)
        {
        }

        private void internetPanHandler(object sender, EventArgs e)
        {
            Process.Start("http://thecrewchief.org/misc.php?do=donate");
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
                Boolean gotDateStamp = false;
                StringBuilder sb = new StringBuilder();
                if (CrewChief.loadDataFromFile)
                {
                    if (CrewChief.currentGameState != null)
                    {
                        if (CrewChief.currentGameState.CurrentTimeStr == null)
                        {
                            CrewChief.currentGameState.CurrentTimeStr = GameStateData.CurrentTime.ToString("HH:mm:ss.fff");
                        }
                        sb.Append(DateTime.Now.ToString("HH:mm:ss.fff")).Append(" (").Append(CrewChief.currentGameState.CurrentTimeStr).Append(")");
                        gotDateStamp = true;
                    }
                }
                if (!gotDateStamp)
                {
                    sb.Append(DateTime.Now.ToString("HH:mm:ss.fff"));
                }
                sb.Append(" : ").Append(value).AppendLine();
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
