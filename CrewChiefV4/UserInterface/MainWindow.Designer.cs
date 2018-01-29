using System.Windows.Forms;
namespace CrewChiefV4
{
    partial class MainWindow
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            this.runListenForChannelOpenThread = false;
            this.runListenForButtonPressesThread = false;
            if (crewChief != null)
            {
                crewChief.stop();
                crewChief.Dispose();
            }
            if (controllerConfiguration != null)
            {
                controllerConfiguration.Dispose();
            }
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWindow));
            this.textBox1 = new System.Windows.Forms.TextBox();
            this.startApplicationButton = new System.Windows.Forms.Button();
            this.forceVersionCheckButton = new System.Windows.Forms.Button();
            this.buttonActionSelect = new System.Windows.Forms.ListBox();
            this.controllersList = new System.Windows.Forms.ListBox();
            this.assignButtonToAction = new System.Windows.Forms.Button();
            this.deleteAssigmentButton = new System.Windows.Forms.Button();
            this.label1 = new System.Windows.Forms.Label();
            this.label2 = new System.Windows.Forms.Label();
            this.button1 = new System.Windows.Forms.Button();
            this.aboutButton = new System.Windows.Forms.Button();
            this.helpButton = new System.Windows.Forms.Button();
            this.scanControllersButton = new System.Windows.Forms.Button();
            this.groupBox1 = new System.Windows.Forms.GroupBox();
            this.alwaysOnButton = new System.Windows.Forms.RadioButton();
            this.toggleButton = new System.Windows.Forms.RadioButton();
            this.holdButton = new System.Windows.Forms.RadioButton();
            this.voiceDisableButton = new System.Windows.Forms.RadioButton();
            this.button2 = new System.Windows.Forms.Button();
            this.messagesVolumeSlider = new System.Windows.Forms.TrackBar();
            this.messagesAudioDeviceBox = new System.Windows.Forms.ComboBox();
            this.speechRecognitionDeviceBox = new System.Windows.Forms.ComboBox();
            this.backgroundAudioDeviceBox = new System.Windows.Forms.ComboBox();
            this.label3 = new System.Windows.Forms.Label();
            this.backgroundVolumeSlider = new System.Windows.Forms.TrackBar();
            this.label4 = new System.Windows.Forms.Label();
            this.gameDefinitionList = new System.Windows.Forms.ListBox();
            this.label5 = new System.Windows.Forms.Label();
            this.personalisationLabel = new System.Windows.Forms.Label();
            this.filenameTextbox = new System.Windows.Forms.TextBox();
            this.filenameLabel = new System.Windows.Forms.Label();
            this.recordSession = new System.Windows.Forms.CheckBox();
            this.playbackInterval = new System.Windows.Forms.TextBox();
            this.app_version = new System.Windows.Forms.Label();
            this.soundPackProgressBar = new System.Windows.Forms.ProgressBar();
            this.downloadSoundPackButton = new System.Windows.Forms.Button();
            this.downloadDriverNamesButton = new System.Windows.Forms.Button();
            this.downloadPersonalisationsButton = new System.Windows.Forms.Button();
            this.driverNamesProgressBar = new System.Windows.Forms.ProgressBar();
            this.personalisationsProgressBar = new System.Windows.Forms.ProgressBar();
            this.personalisationBox = new System.Windows.Forms.ComboBox();
            this.spotterNameLabel = new System.Windows.Forms.Label();
            this.messagesAudioDeviceLabel = new System.Windows.Forms.Label();
            this.speechRecognitionDeviceLabel = new System.Windows.Forms.Label();
            this.backgroundAudioDeviceLabel = new System.Windows.Forms.Label();
            this.spotterNameBox = new System.Windows.Forms.ComboBox();
            this.donateLink = new System.Windows.Forms.LinkLabel();
            this.groupBox1.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.messagesVolumeSlider)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.backgroundVolumeSlider)).BeginInit();
            this.SuspendLayout();
            // 
            // textBox1
            // 
            this.textBox1.Location = new System.Drawing.Point(41, 215);
            this.textBox1.MaxLength = 99999999;
            this.textBox1.Multiline = true;
            this.textBox1.Name = "textBox1";
            this.textBox1.ReadOnly = true;
            this.textBox1.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.textBox1.Size = new System.Drawing.Size(1093, 285);
            this.textBox1.TabIndex = 1;
            // 
            // startApplicationButton
            // 
            this.startApplicationButton.Location = new System.Drawing.Point(41, 28);
            this.startApplicationButton.Name = "startApplicationButton";
            this.startApplicationButton.Size = new System.Drawing.Size(137, 38);
            this.startApplicationButton.TabIndex = 5;
            this.startApplicationButton.Text = Configuration.getUIString("start_application");
            this.startApplicationButton.UseVisualStyleBackColor = true;
            this.startApplicationButton.Click += new System.EventHandler(this.startApplicationButton_Click);
            // 
            // buttonActionSelect
            // 
            this.buttonActionSelect.FormattingEnabled = true;
            this.buttonActionSelect.Location = new System.Drawing.Point(295, 520);
            this.buttonActionSelect.Name = "buttonActionSelect";
            this.buttonActionSelect.Size = new System.Drawing.Size(528, 115);
            this.buttonActionSelect.TabIndex = 7;
            this.buttonActionSelect.SelectedIndexChanged += new System.EventHandler(this.buttonActionSelect_SelectedIndexChanged);
            // 
            // controllersList
            // 
            this.controllersList.FormattingEnabled = true;
            this.controllersList.Location = new System.Drawing.Point(41, 520);
            this.controllersList.Name = "controllersList";
            this.controllersList.Size = new System.Drawing.Size(248, 85);
            this.controllersList.TabIndex = 8;
            this.controllersList.SelectedIndexChanged += new System.EventHandler(this.controllersList_SelectedIndexChanged);
            // 
            // assignButtonToAction
            // 
            this.assignButtonToAction.Location = new System.Drawing.Point(830, 520);
            this.assignButtonToAction.Name = "assignButtonToAction";
            this.assignButtonToAction.Size = new System.Drawing.Size(130, 39);
            this.assignButtonToAction.TabIndex = 9;
            this.assignButtonToAction.Text = Configuration.getUIString("assign_control");
            this.assignButtonToAction.UseVisualStyleBackColor = true;
            this.assignButtonToAction.Click += new System.EventHandler(this.assignButtonToActionClick);
            // 
            // deleteAssigmentButton
            // 
            this.deleteAssigmentButton.Location = new System.Drawing.Point(830, 564);
            this.deleteAssigmentButton.Name = "deleteAssigmentButton";
            this.deleteAssigmentButton.Size = new System.Drawing.Size(130, 40);
            this.deleteAssigmentButton.TabIndex = 10;
            this.deleteAssigmentButton.Text = Configuration.getUIString("delete_assignment");
            this.deleteAssigmentButton.UseVisualStyleBackColor = true;
            this.deleteAssigmentButton.Click += new System.EventHandler(this.deleteAssignmentButtonClicked);
            // 
            // label1
            // 
            this.label1.AutoSize = true;
            this.label1.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label1.Location = new System.Drawing.Point(38, 500);
            this.label1.Name = "label1";
            this.label1.Size = new System.Drawing.Size(138, 17);
            this.label1.TabIndex = 11;
            this.label1.Text = Configuration.getUIString("available_controllers");

            this.scanControllersButton.Location = new System.Drawing.Point(38, 610);
            this.scanControllersButton.Name = "scan_controllers_button";
            this.scanControllersButton.Size = new System.Drawing.Size(143, 30);
            this.scanControllersButton.TabIndex = 99;
            this.scanControllersButton.Text = Configuration.getUIString("scan_controllers");
            this.scanControllersButton.UseVisualStyleBackColor = true;
            this.scanControllersButton.Click += new System.EventHandler(this.scanControllersButtonClicked);
            // 
            // label2
            // 
            this.label2.AutoSize = true;
            this.label2.Font = new System.Drawing.Font("Microsoft Sans Serif", 10F, System.Drawing.FontStyle.Regular, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.label2.Location = new System.Drawing.Point(323, 501);
            this.label2.Name = "label2";
            this.label2.Size = new System.Drawing.Size(117, 17);
            this.label2.TabIndex = 12;
            this.label2.Text = Configuration.getUIString("available_actions");
            // 
            // button1
            // 
            this.button1.Location = new System.Drawing.Point(991, 105);
            this.button1.Name = "button1";
            this.button1.Size = new System.Drawing.Size(143, 31);
            this.button1.TabIndex = 14;
            this.button1.Text = Configuration.getUIString("properties");
            this.button1.UseVisualStyleBackColor = true;
            this.button1.Click += new System.EventHandler(this.editPropertiesButtonClicked);

            // 
            // help button
            // 
            this.helpButton.Location = new System.Drawing.Point(991, 137);
            this.helpButton.Name = "help";
            this.helpButton.Size = new System.Drawing.Size(143, 31);
            this.helpButton.TabIndex = 97;
            this.helpButton.Text = Configuration.getUIString("help");
            this.helpButton.UseVisualStyleBackColor = true;
            this.helpButton.Click += new System.EventHandler(this.helpButtonClicked);
            
            // 
            // about button
            // 
            this.aboutButton.Location = new System.Drawing.Point(991, 169);
            this.aboutButton.Name = "about";
            this.aboutButton.Size = new System.Drawing.Size(143, 31);
            this.aboutButton.TabIndex = 98;
            this.aboutButton.Text = Configuration.getUIString("about");
            this.aboutButton.UseVisualStyleBackColor = true;
            this.aboutButton.Click += new System.EventHandler(this.aboutButtonClicked);
            
            // 
            // groupBox1
            // 
            this.groupBox1.Controls.Add(this.alwaysOnButton);
            this.groupBox1.Controls.Add(this.toggleButton);
            this.groupBox1.Controls.Add(this.holdButton);
            this.groupBox1.Controls.Add(this.voiceDisableButton);
            this.groupBox1.Location = new System.Drawing.Point(970, 520);
            this.groupBox1.Name = "groupBox1";
            this.groupBox1.Size = new System.Drawing.Size(164, 121);
            this.groupBox1.TabIndex = 15;
            this.groupBox1.TabStop = false;
            this.groupBox1.Text = Configuration.getUIString("voice_recognition_mode");
            // 
            // alwaysOnButton
            // 
            this.alwaysOnButton.AutoSize = true;
            this.alwaysOnButton.Location = new System.Drawing.Point(7, 91);
            this.alwaysOnButton.Name = "alwaysOnButton";
            this.alwaysOnButton.Size = new System.Drawing.Size(75, 17);
            this.alwaysOnButton.TabIndex = 3;
            this.alwaysOnButton.TabStop = true;
            this.alwaysOnButton.Text = Configuration.getUIString("always_on");
            this.alwaysOnButton.UseVisualStyleBackColor = true;
            this.alwaysOnButton.CheckedChanged += new System.EventHandler(this.alwaysOnButton_CheckedChanged);
            // 
            // toggleButton
            // 
            this.toggleButton.AutoSize = true;
            this.toggleButton.Location = new System.Drawing.Point(7, 67);
            this.toggleButton.Name = "toggleButton";
            this.toggleButton.Size = new System.Drawing.Size(90, 17);
            this.toggleButton.TabIndex = 2;
            this.toggleButton.TabStop = true;
            this.toggleButton.Text = Configuration.getUIString("toggle_button");
            this.toggleButton.UseVisualStyleBackColor = true;
            this.toggleButton.CheckedChanged += new System.EventHandler(this.toggleButton_CheckedChanged);
            // 
            // holdButton
            // 
            this.holdButton.AutoSize = true;
            this.holdButton.Location = new System.Drawing.Point(7, 44);
            this.holdButton.Name = "holdButton";
            this.holdButton.Size = new System.Drawing.Size(81, 17);
            this.holdButton.TabIndex = 1;
            this.holdButton.TabStop = true;
            this.holdButton.Text = Configuration.getUIString("hold_button");
            this.holdButton.UseVisualStyleBackColor = true;
            this.holdButton.CheckedChanged += new System.EventHandler(this.holdButton_CheckedChanged);
            // 
            // voiceDisableButton
            // 
            this.voiceDisableButton.AutoSize = true;
            this.voiceDisableButton.Location = new System.Drawing.Point(7, 20);
            this.voiceDisableButton.Name = "voiceDisableButton";
            this.voiceDisableButton.Size = new System.Drawing.Size(64, 17);
            this.voiceDisableButton.TabIndex = 0;
            this.voiceDisableButton.TabStop = true;
            this.voiceDisableButton.Text = Configuration.getUIString("disabled");
            this.voiceDisableButton.UseVisualStyleBackColor = true;
            this.voiceDisableButton.CheckedChanged += new System.EventHandler(this.voiceDisableButton_CheckedChanged);
            // 
            // button2
            // 
            this.button2.Location = new System.Drawing.Point(184, 28);
            this.button2.Name = "button2";
            this.button2.Size = new System.Drawing.Size(137, 38);
            this.button2.TabIndex = 16;
            this.button2.Text = Configuration.getUIString("clear_console");
            this.button2.UseVisualStyleBackColor = true;
            this.button2.Click += new System.EventHandler(this.clearConsole);
            // 
            // messagesVolumeSlider
            // 
            this.messagesVolumeSlider.Location = new System.Drawing.Point(327, 28);
            this.messagesVolumeSlider.Name = "messagesVolumeSlider";
            this.messagesVolumeSlider.Size = new System.Drawing.Size(176, 45);
            this.messagesVolumeSlider.TabIndex = 17;
            this.messagesVolumeSlider.Scroll += new System.EventHandler(this.messagesVolumeSlider_Scroll);
            // 
            // label3
            // 
            this.label3.AutoSize = true;
            this.label3.Location = new System.Drawing.Point(367, 12);
            this.label3.Name = "label3";
            this.label3.Size = new System.Drawing.Size(94, 13);
            this.label3.TabIndex = 18;
            this.label3.Text = Configuration.getUIString("messages_volume");
            // 
            // backgroundVolumeSlider
            // 
            this.backgroundVolumeSlider.Location = new System.Drawing.Point(558, 28);
            this.backgroundVolumeSlider.Name = "backgroundVolumeSlider";
            this.backgroundVolumeSlider.Size = new System.Drawing.Size(184, 45);
            this.backgroundVolumeSlider.TabIndex = 19;
            this.backgroundVolumeSlider.Scroll += new System.EventHandler(this.backgroundVolumeSlider_Scroll);
            // 
            // label4
            // 
            this.label4.AutoSize = true;
            this.label4.Location = new System.Drawing.Point(567, 11);
            this.label4.Name = "label4";
            this.label4.Size = new System.Drawing.Size(104, 13);
            this.label4.TabIndex = 20;
            this.label4.Text = Configuration.getUIString("background_volume");
            // 
            // gameDefinitionList
            // 
            this.gameDefinitionList.AllowDrop = true;
            this.gameDefinitionList.FormattingEnabled = true;
            this.gameDefinitionList.Items.AddRange(GameDefinition.getGameDefinitionFriendlyNames());
            this.gameDefinitionList.Location = new System.Drawing.Point(782, 28);
            this.gameDefinitionList.Name = "gameDefinitionList";
            this.gameDefinitionList.MinimumSize = new System.Drawing.Size(203, 173);
            this.gameDefinitionList.MaximumSize = new System.Drawing.Size(203, 173);
            this.gameDefinitionList.TabIndex = 21;
            this.gameDefinitionList.SelectedValueChanged += new System.EventHandler(this.updateSelectedGameDefinition);
            // 
            // label5
            // 
            this.label5.AutoSize = true;
            this.label5.Location = new System.Drawing.Point(779, 9);
            this.label5.Name = "label5";
            this.label5.Size = new System.Drawing.Size(33, 13);
            this.label5.TabIndex = 22;
            this.label5.Text = Configuration.getUIString("game");

            // 
            // label5
            // 
            this.personalisationLabel.AutoSize = true;
            this.personalisationLabel.Location = new System.Drawing.Point(991, 9);
            this.personalisationLabel.Name = "personalisationLabel";
            this.personalisationLabel.Size = new System.Drawing.Size(33, 13);
            this.personalisationLabel.TabIndex = 22;
            this.personalisationLabel.Text = Configuration.getUIString("personalisation_label");

            // 
            // filenameTextbox
            // 
            this.filenameTextbox.Location = new System.Drawing.Point(150, 2);
            this.filenameTextbox.Name = "filenameTextbox";
            this.filenameTextbox.Size = new System.Drawing.Size(108, 20);
            this.filenameTextbox.TabIndex = 23;
            // 
            // filenameLabel
            // 
            this.filenameLabel.AutoSize = true;
            this.filenameLabel.Location = new System.Drawing.Point(68, 5);
            this.filenameLabel.Name = "filenameLabel";
            this.filenameLabel.Size = new System.Drawing.Size(82, 13);
            this.filenameLabel.TabIndex = 24;
            this.filenameLabel.Text = "File name to run";
            // 
            // recordSession
            // 
            this.recordSession.AutoSize = true;
            this.recordSession.Location = new System.Drawing.Point(7, 4);
            this.recordSession.Name = "recordSession";
            this.recordSession.Size = new System.Drawing.Size(61, 17);
            this.recordSession.TabIndex = 25;
            this.recordSession.Text = "Record";
            this.recordSession.UseVisualStyleBackColor = true;
            // 
            // playbackInterval
            // 
            this.playbackInterval.Location = new System.Drawing.Point(261, 2);
            this.playbackInterval.Name = "playbackInterval";
            this.playbackInterval.Size = new System.Drawing.Size(100, 20);
            this.playbackInterval.TabIndex = 26;
            this.playbackInterval.TextChanged += new System.EventHandler(this.playbackIntervalChanged);
            // 
            // app_version
            // 
            this.app_version.AutoSize = true;
            this.app_version.Location = new System.Drawing.Point(1045, 650);
            this.app_version.Name = "app_version";
            this.app_version.Size = new System.Drawing.Size(65, 13);
            this.app_version.TabIndex = 27;
            this.app_version.Text = Configuration.getUIString("app_version");
            // 
            // reset_app_version
            // 
            this.forceVersionCheckButton.AutoSize = true;
            this.forceVersionCheckButton.Location = new System.Drawing.Point(1030, 665);
            this.forceVersionCheckButton.Name = "forceVersionCheckButton";
            this.forceVersionCheckButton.Size = new System.Drawing.Size(65, 13);
            this.forceVersionCheckButton.TabIndex = 90;
            this.forceVersionCheckButton.Text = Configuration.getUIString("check_for_updates");
            this.forceVersionCheckButton.UseVisualStyleBackColor = true;
            this.forceVersionCheckButton.Click += new System.EventHandler(this.forceVersionCheckButtonClicked);
            // 
            // soundPackProgressBar
            // 
            this.soundPackProgressBar.Location = new System.Drawing.Point(39, 176);
            this.soundPackProgressBar.Name = "soundPackProgressBar";
            this.soundPackProgressBar.Size = new System.Drawing.Size(220, 23);
            this.soundPackProgressBar.TabIndex = 28;
            // 
            // downloadSoundPackButton
            // 
            this.downloadSoundPackButton.Enabled = false;
            this.downloadSoundPackButton.Location = new System.Drawing.Point(39, 123);
            this.downloadSoundPackButton.Name = "downloadSoundPackButton";
            this.downloadSoundPackButton.Size = new System.Drawing.Size(220, 37);
            this.downloadSoundPackButton.TabIndex = 29;
            this.downloadSoundPackButton.Text = Configuration.getUIString("sound_pack_is_up_to_date");
            this.downloadSoundPackButton.UseVisualStyleBackColor = true;
            this.downloadSoundPackButton.Click += new System.EventHandler(this.downloadSoundPackButtonPress);
            // 
            // downloadDriverNamesButton
            // 
            this.downloadDriverNamesButton.Enabled = false;
            this.downloadDriverNamesButton.Location = new System.Drawing.Point(295, 123);
            this.downloadDriverNamesButton.Name = "downloadDriverNamesButton";
            this.downloadDriverNamesButton.Size = new System.Drawing.Size(220, 37);
            this.downloadDriverNamesButton.TabIndex = 30;
            this.downloadDriverNamesButton.Text = Configuration.getUIString("driver_names_are_up_to_date");
            this.downloadDriverNamesButton.UseVisualStyleBackColor = true;
            this.downloadDriverNamesButton.Click += new System.EventHandler(this.downloadDriverNamesButtonPress);
            // 
            // driverNamesProgressBar
            // 
            this.driverNamesProgressBar.Location = new System.Drawing.Point(295, 176);
            this.driverNamesProgressBar.Name = "driverNamesProgressBar";
            this.driverNamesProgressBar.Size = new System.Drawing.Size(220, 23);
            this.driverNamesProgressBar.TabIndex = 31;
            // 
            // downloadPersonalisationsButton
            // 
            this.downloadPersonalisationsButton.Enabled = false;
            this.downloadPersonalisationsButton.Location = new System.Drawing.Point(550, 123);
            this.downloadPersonalisationsButton.Name = "downloadPersonalisationsButton";
            this.downloadPersonalisationsButton.Size = new System.Drawing.Size(220, 37);
            this.downloadPersonalisationsButton.TabIndex = 95;
            this.downloadPersonalisationsButton.Text = Configuration.getUIString("personalisations_are_up_to_date");
            this.downloadPersonalisationsButton.UseVisualStyleBackColor = true;
            this.downloadPersonalisationsButton.Click += new System.EventHandler(this.downloadPersonalisationsButtonPress);
            // 
            // personalisationsProgressBar
            // 
            this.personalisationsProgressBar.Location = new System.Drawing.Point(550, 176);
            this.personalisationsProgressBar.Name = "personalisationsProgressBar";
            this.personalisationsProgressBar.Size = new System.Drawing.Size(220, 23);
            this.personalisationsProgressBar.TabIndex = 31;

            this.personalisationBox.Location = new System.Drawing.Point(991, 28);
            this.personalisationBox.IntegralHeight = false;
            this.personalisationBox.MaxDropDownItems = 5;
            this.personalisationBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.personalisationBox.Name = "personalisationBox";
            this.personalisationBox.Size = new System.Drawing.Size(142, 400);
            this.personalisationBox.TabIndex = 94;

            this.spotterNameLabel.AutoSize = true;
            this.spotterNameLabel.Location = new System.Drawing.Point(991, 55);
            this.spotterNameLabel.Name = "spotterNameLabel";
            this.spotterNameLabel.Size = new System.Drawing.Size(33, 13);
            this.spotterNameLabel.TabIndex = 22;
            this.spotterNameLabel.Text = Configuration.getUIString("spotter_name_label");

            this.spotterNameBox.Location = new System.Drawing.Point(991, 70);
            this.spotterNameBox.IntegralHeight = false;
            this.spotterNameBox.MaxDropDownItems = 5;
            this.spotterNameBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.spotterNameBox.Name = "spotterNameBox";
            this.spotterNameBox.Size = new System.Drawing.Size(142, 400);
            this.spotterNameBox.TabIndex = 93;
            this.spotterNameBox.SelectedIndexChanged += new System.EventHandler(this.spotterNameBox_SelectedIndexChanged);

            this.donateLink.Location = new System.Drawing.Point(35, 650);
            this.donateLink.Size = new System.Drawing.Size(250, 15);
            this.donateLink.Text = Configuration.getUIString("donate_link_text");
            this.donateLink.Click += new System.EventHandler(this.internetPanHandler);
            
            // Associate the event-handling method with the 
            // SelectedIndexChanged event.
            this.personalisationBox.SelectedIndexChanged += new System.EventHandler(this.personalisationBox_SelectedIndexChanged);

            this.speechRecognitionDeviceLabel.AutoSize = true;
            this.speechRecognitionDeviceLabel.Location = new System.Drawing.Point(115, 70);
            this.speechRecognitionDeviceLabel.Name = "speechRecognitionDeviceLabel";
            this.speechRecognitionDeviceLabel.Size = new System.Drawing.Size(100, 13);
            this.speechRecognitionDeviceLabel.TabIndex = 22;
            this.speechRecognitionDeviceLabel.Text = Configuration.getUIString("speech_recognition_device_label");
            this.speechRecognitionDeviceLabel.Visible = false;

            this.speechRecognitionDeviceBox.Location = new System.Drawing.Point(115, 90);
            this.speechRecognitionDeviceBox.IntegralHeight = false;
            this.speechRecognitionDeviceBox.MaxDropDownItems = 5;
            this.speechRecognitionDeviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.speechRecognitionDeviceBox.Name = "speechRecognitionDeviceBox";
            this.speechRecognitionDeviceBox.Size = new System.Drawing.Size(190, 400);
            this.speechRecognitionDeviceBox.TabIndex = 94;
            this.speechRecognitionDeviceBox.Visible = false;
            this.speechRecognitionDeviceBox.Enabled = false;

            this.messagesAudioDeviceBox.Location = new System.Drawing.Point(330, 90);
            this.messagesAudioDeviceBox.IntegralHeight = false;
            this.messagesAudioDeviceBox.MaxDropDownItems = 5;
            this.messagesAudioDeviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.messagesAudioDeviceBox.Name = "messagesAudioDeviceBox";
            this.messagesAudioDeviceBox.Size = new System.Drawing.Size(190, 400);
            this.messagesAudioDeviceBox.TabIndex = 94;
            this.messagesAudioDeviceBox.Visible = false;
            this.messagesAudioDeviceBox.Enabled = false;

            this.messagesAudioDeviceLabel.AutoSize = true;
            this.messagesAudioDeviceLabel.Location = new System.Drawing.Point(330, 70);
            this.messagesAudioDeviceLabel.Name = "messagesAudioDeviceLabel";
            this.messagesAudioDeviceLabel.Size = new System.Drawing.Size(100, 13);
            this.messagesAudioDeviceLabel.TabIndex = 22;
            this.messagesAudioDeviceLabel.Text = Configuration.getUIString("messages_audio_device_label");
            this.messagesAudioDeviceLabel.Visible = false;

            this.messagesAudioDeviceBox.Location = new System.Drawing.Point(330, 90);
            this.messagesAudioDeviceBox.IntegralHeight = false;
            this.messagesAudioDeviceBox.MaxDropDownItems = 5;
            this.messagesAudioDeviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.messagesAudioDeviceBox.Name = "messagesAudioDeviceBox";
            this.messagesAudioDeviceBox.Size = new System.Drawing.Size(190, 400);
            this.messagesAudioDeviceBox.TabIndex = 94;
            this.messagesAudioDeviceBox.Visible = false;
            this.messagesAudioDeviceBox.Enabled = false;

            this.backgroundAudioDeviceLabel.AutoSize = true;
            this.backgroundAudioDeviceLabel.Location = new System.Drawing.Point(550, 70);
            this.backgroundAudioDeviceLabel.Name = "backgroundAudioDeviceLabel";
            this.backgroundAudioDeviceLabel.Size = new System.Drawing.Size(100, 13);
            this.backgroundAudioDeviceLabel.TabIndex = 22;
            this.backgroundAudioDeviceLabel.Text = Configuration.getUIString("background_audio_device_label");
            this.backgroundAudioDeviceLabel.Visible = false;

            this.backgroundAudioDeviceBox.Location = new System.Drawing.Point(550, 90);
            this.backgroundAudioDeviceBox.IntegralHeight = false;
            this.backgroundAudioDeviceBox.MaxDropDownItems = 5;
            this.backgroundAudioDeviceBox.DropDownStyle = ComboBoxStyle.DropDownList;
            this.backgroundAudioDeviceBox.Name = "backgroundAudioDeviceBox";
            this.backgroundAudioDeviceBox.Size = new System.Drawing.Size(190, 400);
            this.backgroundAudioDeviceBox.TabIndex = 94;
            this.backgroundAudioDeviceBox.Visible = false;
            this.backgroundAudioDeviceBox.Enabled = false;
            // the handler for this is added when we initialise

            // 
            // MainWindow
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(1146, 692);
            this.MaximizeBox = false;
            this.Controls.Add(this.driverNamesProgressBar);
            this.Controls.Add(this.downloadDriverNamesButton);
            this.Controls.Add(this.downloadSoundPackButton); 
            this.Controls.Add(this.downloadPersonalisationsButton);
            this.Controls.Add(this.soundPackProgressBar);
            this.Controls.Add(this.personalisationsProgressBar);
            this.Controls.Add(this.app_version);
            this.Controls.Add(this.playbackInterval);
            this.Controls.Add(this.recordSession);
            this.Controls.Add(this.filenameLabel);
            this.Controls.Add(this.filenameTextbox);
            this.Controls.Add(this.label5);
            this.Controls.Add(this.gameDefinitionList);
            this.Controls.Add(this.label4);
            this.Controls.Add(this.backgroundVolumeSlider);
            this.Controls.Add(this.label3);
            this.Controls.Add(this.messagesVolumeSlider);
            this.Controls.Add(this.messagesAudioDeviceBox);
            this.Controls.Add(this.speechRecognitionDeviceBox);
            this.Controls.Add(this.backgroundAudioDeviceBox);
            this.Controls.Add(this.button2);
            this.Controls.Add(this.groupBox1);
            this.Controls.Add(this.button1);
            this.Controls.Add(this.helpButton);
            this.Controls.Add(this.aboutButton);
            this.Controls.Add(this.scanControllersButton);
            this.Controls.Add(this.label2);
            this.Controls.Add(this.label1);
            this.Controls.Add(this.deleteAssigmentButton);
            this.Controls.Add(this.assignButtonToAction);
            this.Controls.Add(this.controllersList);
            this.Controls.Add(this.buttonActionSelect);
            this.Controls.Add(this.startApplicationButton);
            this.Controls.Add(this.forceVersionCheckButton);
            this.Controls.Add(this.textBox1);
            this.Controls.Add(this.personalisationBox);
            this.Controls.Add(this.spotterNameBox);
            this.Controls.Add(this.personalisationLabel);
            this.Controls.Add(this.spotterNameLabel);
            this.Controls.Add(this.messagesAudioDeviceLabel);
            this.Controls.Add(this.speechRecognitionDeviceLabel);
            this.Controls.Add(this.backgroundAudioDeviceLabel);
            this.Controls.Add(this.donateLink);
            this.Name = "MainWindow";
            this.Text = "Crew Chief V4";
            this.Icon = ((System.Drawing.Icon)(resources.GetObject("$this.Icon")));
            this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.stopApp);
            this.Load += new System.EventHandler(this.FormMain_Load);
            this.groupBox1.ResumeLayout(false);
            this.groupBox1.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.messagesVolumeSlider)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.backgroundVolumeSlider)).EndInit();
            this.ResumeLayout(false);
            this.PerformLayout();

        }

        #endregion

        private System.Windows.Forms.TextBox textBox1;
        private System.Windows.Forms.Button startApplicationButton;
        private System.Windows.Forms.Button forceVersionCheckButton;
        private System.Windows.Forms.ListBox buttonActionSelect;
        private System.Windows.Forms.ListBox controllersList;
        private System.Windows.Forms.Button assignButtonToAction;
        private System.Windows.Forms.Button deleteAssigmentButton;
        private System.Windows.Forms.Label label1;
        private System.Windows.Forms.Label label2;
        private System.Windows.Forms.Button button1;
        private System.Windows.Forms.Button helpButton;
        private System.Windows.Forms.Button aboutButton;
        private System.Windows.Forms.Button scanControllersButton;
        private System.Windows.Forms.GroupBox groupBox1;
        private System.Windows.Forms.RadioButton alwaysOnButton;
        private System.Windows.Forms.RadioButton toggleButton;
        private System.Windows.Forms.RadioButton holdButton;
        private System.Windows.Forms.RadioButton voiceDisableButton;
        private System.Windows.Forms.Button button2;
        private System.Windows.Forms.TrackBar messagesVolumeSlider;
        private System.Windows.Forms.ComboBox speechRecognitionDeviceBox;
        private System.Windows.Forms.ComboBox messagesAudioDeviceBox;
        private System.Windows.Forms.ComboBox backgroundAudioDeviceBox;
        private System.Windows.Forms.Label label3;
        private System.Windows.Forms.TrackBar backgroundVolumeSlider;
        private System.Windows.Forms.Label label4;
        private System.Windows.Forms.ListBox gameDefinitionList;
        private System.Windows.Forms.Label label5;
        private System.Windows.Forms.Label personalisationLabel;
        private System.Windows.Forms.TextBox filenameTextbox;
        private System.Windows.Forms.Label filenameLabel;
        private System.Windows.Forms.CheckBox recordSession;
        private System.Windows.Forms.TextBox playbackInterval;
        private System.Windows.Forms.Label app_version;
        private System.Windows.Forms.ProgressBar soundPackProgressBar;
        private System.Windows.Forms.Button downloadSoundPackButton;
        private System.Windows.Forms.Button downloadDriverNamesButton;
        private System.Windows.Forms.Button downloadPersonalisationsButton;
        private System.Windows.Forms.ProgressBar driverNamesProgressBar;
        private System.Windows.Forms.ProgressBar personalisationsProgressBar;
        private System.Windows.Forms.ComboBox personalisationBox;
        private System.Windows.Forms.Label spotterNameLabel;
        private System.Windows.Forms.Label messagesAudioDeviceLabel;
        private System.Windows.Forms.Label speechRecognitionDeviceLabel;
        private System.Windows.Forms.Label backgroundAudioDeviceLabel;
        private System.Windows.Forms.ComboBox spotterNameBox;
        private System.Windows.Forms.LinkLabel donateLink;
    }
}
