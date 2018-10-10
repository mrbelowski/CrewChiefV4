using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using System.Threading;
using System.Media;
using CrewChiefV4.Events;
using System.Windows.Media;
using System.Collections.Specialized;
using CrewChiefV4.GameState;
using System.Collections;
using System.Runtime.Remoting.Contexts;
using System.Diagnostics;

namespace CrewChiefV4.Audio
{
    public class AudioPlayer
    {
        public static String PAUSE_ID = "insert_pause";

        public Boolean disablePearlsOfWisdom = false;   // used for the last 2 laps / 3 minutes of a race session only
        public Boolean mute = false;

        public static Boolean playWithNAudio = UserSettings.GetUserSettings().getBoolean("use_naudio");

        public static Boolean delayMessagesInHardParts = UserSettings.GetUserSettings().getBoolean("enable_delayed_messages_on_hardparts");

        public enum TTS_OPTION { NEVER, ONLY_WHEN_NECESSARY, ANY_TIME }
        public static TTS_OPTION ttsOption = TTS_OPTION.ONLY_WHEN_NECESSARY;

        public static int naudioMessagesPlaybackDeviceId = 0;
        public static int naudioBackgroundPlaybackDeviceId = 0;
        public static Dictionary<string, Tuple<string, int>> playbackDevices = new Dictionary<string, Tuple<string, int>>();
        
        public static String folderAcknowlegeOK = "acknowledge/OK";
        public static String folderYellowEnabled = "acknowledge/yellowEnabled";
        public static String folderYellowDisabled = "acknowledge/yellowDisabled";
        public static String folderAcknowlegeEnableKeepQuiet = "acknowledge/keepQuietEnabled";
        public static String folderAcknowlegeDisableKeepQuiet = "acknowledge/keepQuietDisabled";
        public static String folderDidntUnderstand = "acknowledge/didnt_understand";
        public static String folderNoData = "acknowledge/no_data";
        public static String folderNoMoreData = "acknowledge/no_more_data";
        public static String folderYes = "acknowledge/yes";
        public static String folderNo = "acknowledge/no";
        public static String folderDeltasEnabled = "acknowledge/deltasEnabled";
        public static String folderDeltasDisabled = "acknowledge/deltasDisabled";
        public static String folderRadioCheckResponse = "acknowledge/radio_check";
        public static String folderStandBy = "acknowledge/stand_by";
        public static String folderFuelToEnd = "acknowledge/fuel_to_end";

        public static String folderAcknowledgeEnableDelayInHardParts = "acknowledge/keep_quiet_in_corners_enabled";
        public static String folderAcknowledgeDisableDelayInHardParts = "acknowledge/keep_quiet_in_corners_disabled";

        private String folderRants = "rants/general";
        private Boolean playedRantInThisSession = false;
        public static Boolean rantWaitingToPlay = false;
        public static Boolean enableRants = UserSettings.GetUserSettings().getBoolean("enable_rants");
        private static float rantLikelihood = enableRants ? 0.1f : 0;

        public static Boolean useAlternateBeeps = UserSettings.GetUserSettings().getBoolean("use_alternate_beeps");
        public static float pauseBetweenMessages = UserSettings.GetUserSettings().getFloat("pause_between_messages");

        private QueuedMessage lastMessagePlayed = null;

        private Boolean allowPearlsOnNextPlay = true;
        private Dictionary<String, int> playedMessagesCount = new Dictionary<String, int>();

        public Boolean monitorRunning = false;

        private Boolean keepQuiet = false;
        private Boolean channelOpen = false;

        private Boolean holdChannelOpen = false;
        private Boolean useShortBeepWhenOpeningChannel = false;

        private TimeSpan maxTimeToHoldEmptyChannelOpen = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("spotter_hold_repeat_frequency") + 1);
        private DateTime timeOfLastMessageEnd = DateTime.MinValue;

        private Boolean useListenBeep = UserSettings.GetUserSettings().getBoolean("use_listen_beep");

        private readonly TimeSpan minTimeBetweenPearlsOfWisdom = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("minimum_time_between_pearls_of_wisdom"));

        private Boolean sweary = UserSettings.GetUserSettings().getBoolean("use_sweary_messages");
        private Boolean allowCaching = UserSettings.GetUserSettings().getBoolean("cache_sounds");

        private OrderedDictionary queuedClips = new OrderedDictionary();

        private OrderedDictionary immediateClips = new OrderedDictionary();

        private BackgroundPlayer backgroundPlayer;

        public static String soundFilesPath;

        // Default voice pack root path.
        // Spotter files location does not depend on Chief voice location, and comes from the default voice pack, for now.
        public static String soundFilesPathNoChiefOverride;

        private String backgroundFilesPath;

        public static String dtmPitWindowOpenBackground = "dtm_pit_window_open.wav";

        public static String dtmPitWindowClosedBackground = "dtm_pit_window_closed.wav";

        private PearlsOfWisdom pearlsOfWisdom = new PearlsOfWisdom();

        DateTime timeLastPearlOfWisdomPlayed = DateTime.UtcNow;

        public Boolean initialised = false;

        public static String soundPackLanguage = null;

        private String lastImmediateMessageName = null;
        private DateTime lastImmediateMessageTime = DateTime.MinValue;

        private Boolean regularQueuePaused = false;

        private SoundCache soundCache;

        public static String NO_PERSONALISATION_SELECTED = "(non selected)";
        public String[] personalisationsArray = new String[] { NO_PERSONALISATION_SELECTED };

        public String selectedPersonalisation = NO_PERSONALISATION_SELECTED;

        private SynchronizationContext mainThreadContext = null;

        private int messageId = 0;

        public static String defaultChiefId = "Jim (default)";
        public static List<String> availableChiefVoices = new List<String>();
        public static String folderChiefRadioCheck = null;
        private Thread monitorQueueThread = null;


        private AutoResetEvent monitorQueueWakeUpEvent = new AutoResetEvent(false);
        private AutoResetEvent hangingChannelCloseWakeUpEvent = new AutoResetEvent(false);
        private DateTime nextWakeupCheckTime = DateTime.MinValue;
        private Thread playDelayedImmediateMessageThread = null;
        private Thread pauseQueueThread = null;
        private Thread hangingChannelCloseThread = null;

        static AudioPlayer()
        {
            // Inintialize sound file paths.  Handle user specified override, or pick default.
            String soundPackLocationOverride = UserSettings.GetUserSettings().getString("override_default_sound_pack_location");
            String defaultSoundFilesPath = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData) + @"\CrewChiefV4\sounds";
            DirectoryInfo defaultSoundDirectory = new DirectoryInfo(defaultSoundFilesPath);
            DirectoryInfo overrideSoundDirectory = null;
            Boolean useOverride = false;
            if (soundPackLocationOverride != null && soundPackLocationOverride.Length > 0)
            {
                try
                {
                    overrideSoundDirectory = new DirectoryInfo(soundPackLocationOverride);
                    if (overrideSoundDirectory.Exists)
                    {
                        useOverride = true;
                    }
                    else
                    {
                        Console.WriteLine("Specified sound pack override folder " + soundPackLocationOverride + " doesn't exist, using default");
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to set override sound folder " + e.Message);
                }
            }
            if (useOverride && overrideSoundDirectory != null)
            {
                soundFilesPath = soundFilesPathNoChiefOverride = soundPackLocationOverride;
            }
            else
            {
                soundFilesPath = soundFilesPathNoChiefOverride = defaultSoundFilesPath;
            }

            // Process alternative Chief voice locations.
            availableChiefVoices.Clear();
            availableChiefVoices.Add(defaultChiefId);
            try
            {
                DirectoryInfo altVoicePackDirectory = new DirectoryInfo(AudioPlayer.soundFilesPath + "/alt/");
                if (altVoicePackDirectory.Exists)
                {
                    DirectoryInfo[] directories = altVoicePackDirectory.GetDirectories();
                    foreach (DirectoryInfo folder in directories)
                    {
                        if (!availableChiefVoices.Contains(folder.Name))
                        {
                            availableChiefVoices.Add(folder.Name);
                        }
                        else
                        {
                            Console.WriteLine("Error: skipping alternative voice pack directory, because it already exists in a list: " + folder.FullName);
                        }
                    }
                }

                String selectedChief = UserSettings.GetUserSettings().getString("chief_name");
                if (!String.IsNullOrWhiteSpace(selectedChief) && !defaultChiefId.Equals(selectedChief))
                {
                    if (Directory.Exists(AudioPlayer.soundFilesPath + "/alt/" + selectedChief))
                    {
                        Console.WriteLine("Using Chief voice: " + selectedChief);
                        AudioPlayer.soundFilesPath = AudioPlayer.soundFilesPath + "/alt/" + selectedChief;

                        // Prefer test_chief folder, and fall back to test if it doesn't exist.
                        if (Directory.Exists(AudioPlayer.soundFilesPathNoChiefOverride + "/voice/radio_check_" + selectedChief + "/test_chief"))
                        {
                            folderChiefRadioCheck = "radio_check_" + selectedChief + "/test_chief";
                        }
                        else if (Directory.Exists(AudioPlayer.soundFilesPathNoChiefOverride + "/voice/radio_check_" + selectedChief + "/test"))
                        {
                            folderChiefRadioCheck = "radio_check_" + selectedChief + "/test";
                        }
                    }
                    else
                    {
                        Console.WriteLine("No Chief called " + selectedChief + " exists, dropping back to the default (Jim)");
                        UserSettings.GetUserSettings().setProperty("chief_name", defaultChiefId);
                        UserSettings.GetUserSettings().saveUserSettings();
                    }
                }
                else
                {
                    // Prefer test_chief folder, and fall back to test if it doesn't exist.
                    if (Directory.Exists(AudioPlayer.soundFilesPathNoChiefOverride + "/voice/radio_check/test_chief"))
                    {
                        folderChiefRadioCheck = "radio_check/test_chief";
                    }
                    else if (Directory.Exists(AudioPlayer.soundFilesPathNoChiefOverride + "/voice/radio_check/test"))
                    {
                        folderChiefRadioCheck = "radio_check/test";
                    }
                }
            }
            catch (Exception)
            {
                Console.WriteLine("No Chief voice sound folders available");
            }

            Enum.TryParse(UserSettings.GetUserSettings().getString("tts_setting_listprop"), out ttsOption);
            Debug.Assert(Enum.IsDefined(typeof(TTS_OPTION), ttsOption));

            // Initialize optional nAudio playback.
            if (UserSettings.GetUserSettings().getBoolean("use_naudio"))
            {
                playbackDevices.Clear();
                for (int deviceId = 0; deviceId < NAudio.Wave.WaveOut.DeviceCount; deviceId++)
                {
                    // the audio device stuff makes no guarantee as to the presence of sensible device and product guids,
                    // so we have to do the best we can here
                    NAudio.Wave.WaveOutCapabilities capabilities = NAudio.Wave.WaveOut.GetCapabilities(deviceId);
                    Boolean hasNameGuid = capabilities.NameGuid != null && !capabilities.NameGuid.Equals(Guid.Empty);
                    Boolean hasProductGuid = capabilities.ProductGuid != null && !capabilities.ProductGuid.Equals(Guid.Empty);
                    String rawName = capabilities.ProductName;
                    String name = rawName;
                    int nameAddition = 0;
                    while (playbackDevices.Keys.Contains(name))
                    {
                        nameAddition++;
                        name = rawName += "(" + nameAddition + ")";
                    }
                    String guidToUse;
                    if (hasNameGuid)
                    {
                        guidToUse = capabilities.NameGuid.ToString();
                    }
                    else if (hasProductGuid)
                    {
                        guidToUse = capabilities.ProductGuid.ToString() + "_" + name;
                    }
                    else
                    {
                        guidToUse = name;
                    }
                    playbackDevices.Add(name, new Tuple<string, int>(guidToUse, deviceId));
                }
            }
        }

        public AudioPlayer()
        {
            this.mainThreadContext = SynchronizationContext.Current;

            // Only update main pack for now?
            DirectoryInfo soundDirectory = new DirectoryInfo(soundFilesPathNoChiefOverride);
            if (soundDirectory.Exists)
            {
                SoundPackVersionsHelper.currentSoundPackVersion = getSoundPackVersion(soundDirectory);
                SoundPackVersionsHelper.currentDriverNamesVersion = getDriverNamesVersion(soundDirectory);
                SoundPackVersionsHelper.currentPersonalisationsVersion = getPersonalisationsVersion(soundDirectory);
            }
            else
            {
                soundDirectory.Create();
            }

            soundDirectory = new DirectoryInfo(soundFilesPath);
            if (soundDirectory.Exists)
            {
                // Pick language from possibly overriden voice location.
                soundPackLanguage = getSoundPackLanguage(soundDirectory);
            }
            if (soundPackLanguage == null)
            {
                soundPackLanguage = "en";  // Default to Queen's English, or Northern?
            }

            // populate the personalisations list
            DirectoryInfo personalisationsDirectory = new DirectoryInfo(soundFilesPath + @"\personalisations");
            if (personalisationsDirectory.Exists)
            {
                List<String> personalisationsList = new List<string>();
                personalisationsList.Add(NO_PERSONALISATION_SELECTED);
                foreach (DirectoryInfo folderInPersonalisationsDirectory in personalisationsDirectory.GetDirectories())
                {
                    personalisationsList.Add(folderInPersonalisationsDirectory.Name);
                }
                personalisationsArray = personalisationsList.ToArray();
            }
            String savedPersonalisation = UserSettings.GetUserSettings().getString("PERSONALISATION_NAME");
            if (savedPersonalisation != null && savedPersonalisation.Length > 0)
            {
                selectedPersonalisation = savedPersonalisation;
            }
        }

        // if it's more than 200ms since the last call, and message have been queued, wake the monitor thread
        public void wakeMonitorThreadForRegularMessages(DateTime now)
        {
            if (now > nextWakeupCheckTime && queuedClips.Count > 0)
            {
                Boolean doHardPartsCheck = delayMessagesInHardParts &&
                            CrewChief.currentGameState != null &&
                            CrewChief.currentGameState.PositionAndMotionData.CarSpeed > 5 &&
                            (CrewChief.currentGameState.SessionData.SessionPhase == SessionPhase.Green || CrewChief.currentGameState.SessionData.SessionPhase == SessionPhase.Checkered) &&
                            !GameStateData.onManualFormationLap;
                if (!doHardPartsCheck ||
                    !CrewChief.currentGameState.hardPartsOnTrackData.isInHardPart(CrewChief.currentGameState.PositionAndMotionData.DistanceRoundTrack))
                {
                    monitorQueueWakeUpEvent.Set();
                }
                nextWakeupCheckTime = now.AddMilliseconds(200);
            }
        }

        // for debugging the moderator message block process
        public String getMessagesBlocking(SoundType blockLevel)
        {
            List<String> blockingMessages = new List<string>();
            lock (immediateClips)
            {
                foreach (Object entry in immediateClips.Values)
                {
                    QueuedMessage message = (QueuedMessage)entry;
                    if (message.metadata.type <= blockLevel)
                    {
                        blockingMessages.Add(message.messageName + "(" + message.metadata.type + ")");
                    }
                }
            }
            return String.Join(", ", blockingMessages);
        }

        public void initialise()
        {
            DirectoryInfo soundDirectory = new DirectoryInfo(soundFilesPath);
            backgroundFilesPath = Path.Combine(soundFilesPathNoChiefOverride, "background_sounds");

            if (UserSettings.GetUserSettings().getBoolean("use_naudio"))
            {
                this.backgroundPlayer = new NAudioBackgroundPlayer(backgroundFilesPath, dtmPitWindowClosedBackground);
                try
                {
                    this.backgroundPlayer.initialise(dtmPitWindowClosedBackground);
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to initialise nAudio background player: " + e.Message);
                    Console.WriteLine("Using WindowsMediaPlayer for background sounds");
                    this.backgroundPlayer = new MediaPlayerBackgroundPlayer(mainThreadContext, backgroundFilesPath, dtmPitWindowClosedBackground);
                    this.backgroundPlayer.initialise(dtmPitWindowClosedBackground);
                }
            }
            else
            {
                this.backgroundPlayer = new MediaPlayerBackgroundPlayer(mainThreadContext, backgroundFilesPath, dtmPitWindowClosedBackground);
                this.backgroundPlayer.initialise(dtmPitWindowClosedBackground);
            }

            if (!soundDirectory.Exists)
            {
                Console.WriteLine("Unable to find sound directory " + soundDirectory.FullName);
                return;
            }
            if (SoundPackVersionsHelper.currentSoundPackVersion == -1)
            {
                Console.WriteLine("Unable to get sound pack version");
            }
            else
            {
                Console.WriteLine("Using sound pack version " + SoundPackVersionsHelper.currentSoundPackVersion +
                    ", driver names version " + SoundPackVersionsHelper.currentDriverNamesVersion +
                    " and personalisations version " + SoundPackVersionsHelper.currentPersonalisationsVersion);
            }
            if (this.soundCache == null)
            {
                soundCache = new SoundCache(new DirectoryInfo(soundFilesPath), new DirectoryInfo(soundFilesPathNoChiefOverride),
                    new String[] { "spotter", "acknowledge" }, sweary, allowCaching, selectedPersonalisation);
            }
            initialised = true;
            PlaybackModerator.SetAudioPlayer(this);
        }
        
        public SoundCache getSoundCache()
        {
            return soundCache;
        }

        public void startMonitor()
        {
            if (monitorRunning)
            {
                Console.WriteLine("Monitor is already running");
            }
            else
            {
                Console.WriteLine("Starting queue monitor");
                monitorRunning = true;
                // spawn a Thread to monitor the queue
                Debug.Assert(monitorQueueThread == null);

                // This thread is managed by the Chief Run thread directly.
                monitorQueueThread = new Thread(monitorQueue);
                monitorQueueThread.Name = "AudioPlayer.monitorQueueThread";
                monitorQueueThread.Start();
            }
            new SmokeTest(this).trigger(new GameStateData(DateTime.UtcNow.Ticks), new GameStateData(DateTime.UtcNow.Ticks));
        }

        public void stopMonitor()
        {
            monitorRunning = false;
            monitorQueueWakeUpEvent.Set();
            stopHangingChannelCloseThread();
            // Wait for monitor queue thread to exit.
            if (monitorQueueThread != null)
            {
                if (monitorQueueThread.IsAlive)
                {
                    Console.WriteLine("Waiting for queue monitor to stop...");
                    if (!monitorQueueThread.Join(5000))
                    {
                        Console.WriteLine("Warning: Timed out waiting for queue monitor to stop");
                    }
                }
                monitorQueueThread = null;
                Console.WriteLine("Monitor queue stopped");
            }
            channelOpen = false;
        }

        public float getSoundPackVersion(DirectoryInfo soundDirectory)
        {
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            float version = -1;
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "sound_pack_version_info.txt")
                {
                    String[] lines = File.ReadAllLines(fileInSoundDirectory.FullName/*Path.Combine(soundFilesPath, fileInSoundDirectory.Name)*/);
                    foreach (String line in lines)
                    {
                        if (float.TryParse(line, out version))
                        {
                            return version;
                        }
                    }
                    break;
                }
            }
            return version;
        }

        public String getSoundPackLanguage(DirectoryInfo soundDirectory)
        {
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "sound_pack_language.txt")
                {
                    String[] lines = File.ReadAllLines(Path.Combine(soundFilesPath, fileInSoundDirectory.Name));
                    if (lines.Length > 0)
                    {
                        return lines[0];
                    }
                }
            }
            return null;
        }

        public float getDriverNamesVersion(DirectoryInfo soundDirectory)
        {
            DirectoryInfo[] directories = soundDirectory.GetDirectories();
            float version = -1;

            foreach (DirectoryInfo folderInSoundDirectory in directories)
            {
                if (folderInSoundDirectory.Name == "driver_names")
                {
                    FileInfo[] filesInDriverNamesDirectory = folderInSoundDirectory.GetFiles();
                    foreach (FileInfo fileInDriverNameDirectory in filesInDriverNamesDirectory)
                    {
                        if (fileInDriverNameDirectory.Name == "driver_names_version_info.txt")
                        {
                            String[] lines = File.ReadAllLines(fileInDriverNameDirectory.FullName);
                            foreach (String line in lines)
                            {
                                if (float.TryParse(line, out version))
                                {
                                    return version;
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            return version;
        }

        public float getPersonalisationsVersion(DirectoryInfo soundDirectory)
        {
            DirectoryInfo[] directories = soundDirectory.GetDirectories();
            float version = -1;

            foreach (DirectoryInfo folderInSoundDirectory in directories)
            {
                if (folderInSoundDirectory.Name == "personalisations")
                {
                    FileInfo[] filesInDriverNamesDirectory = folderInSoundDirectory.GetFiles();
                    foreach (FileInfo fileInDriverNameDirectory in filesInDriverNamesDirectory)
                    {
                        if (fileInDriverNameDirectory.Name == "personalisations_version_info.txt")
                        {
                            String[] lines = File.ReadAllLines(fileInDriverNameDirectory.FullName);
                            foreach (String line in lines)
                            {
                                if (float.TryParse(line, out version))
                                {
                                    return version;
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            return version;
        }

        public void setBackgroundSound(String backgroundSoundName)
        {
            this.backgroundPlayer.setBackgroundSound(backgroundSoundName);
        }
        
        public void muteBackgroundPlayer(bool doMute)
        {
            this.backgroundPlayer.mute(doMute);
        }


        private void monitorQueue()
        {
            Console.WriteLine("Monitor starting");
            // ensure the BGP is initialised:
            this.backgroundPlayer.initialise(dtmPitWindowClosedBackground);
            while (monitorRunning)
            {
                int waitTimeout = -1;
                DateTime now = CrewChief.currentGameState == null ? DateTime.UtcNow : CrewChief.currentGameState.Now;
                if (channelOpen && (!holdChannelOpen || now > timeOfLastMessageEnd + maxTimeToHoldEmptyChannelOpen))
                {
                    if (!queueHasDueMessages(queuedClips, false) && !queueHasDueMessages(immediateClips, true))
                    {
                        holdChannelOpen = false;
                        stopHangingChannelCloseThread();
                        closeRadioInternalChannel();
                    }
                }
                if (immediateClips.Count > 0)
                {
                    try
                    {
                        playQueueContents(immediateClips, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception processing immediate clips: " + e.Message + " stack " + e.StackTrace);
                        lock (immediateClips)
                        {
                            immediateClips.Clear();
                        }
                    }
                    waitTimeout = 10;
                }
                else if (!regularQueuePaused && queuedClips.Count > 0)
                {
                    try
                    {
                        playQueueContents(queuedClips, false);
                        allowPearlsOnNextPlay = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception processing queued clips: " + e.Message + " stack " + e.StackTrace);
                        lock (queuedClips)
                        {
                            queuedClips.Clear();
                        }
                    }
                    waitTimeout = 10;
                }
                // -1 timeout means wait-till-notified. 10 timeout kicks the loop off again almost immediately. This
                // should happen once after playing messages, to allow the channel to be closed. After the channel is closed
                // we're back to a -1 timeout
                monitorQueueWakeUpEvent.WaitOne(waitTimeout);
            }
            //writeMessagePlayedStats();
            playedMessagesCount.Clear();

            this.backgroundPlayer.stop();
        }

        private void writeMessagePlayedStats()
        {
            Console.WriteLine("Count, event name");
            foreach (KeyValuePair<String, int> entry in playedMessagesCount)
            {
                Console.WriteLine(entry.Value + " instances of event " + entry.Key);
            }
        }

        public void enableKeepQuietMode()
        {
            playMessageImmediately(new QueuedMessage(folderAcknowlegeEnableKeepQuiet, 0, null));
            keepQuiet = true;
        }

        public void disableKeepQuietMode()
        {
            playMessageImmediately(new QueuedMessage(folderAcknowlegeDisableKeepQuiet, 0, null));
            keepQuiet = false;
        }

        private void playQueueContents(OrderedDictionary queueToPlay, Boolean isImmediateMessages)
        {
            long milliseconds = GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond;
            List<String> keysToPlay = new List<String>();
            List<String> soundsProcessed = new List<String>();

            Boolean oneOrMoreEventsEnabled = false;

            lock (queueToPlay)
            {
                int willBePlayedCount = queueToPlay.Count;
                String firstMovableEventWithPrefixOrSuffix = null;
                foreach (String key in queueToPlay.Keys)
                {
                    QueuedMessage queuedMessage = (QueuedMessage)queueToPlay[key];
                    if (isImmediateMessages || queuedMessage.dueTime <= milliseconds)
                    {
                        Boolean messageHasExpired = queuedMessage.expiryTime != 0 && queuedMessage.expiryTime < milliseconds;
                        Boolean messageIsStillValid = queuedMessage.isMessageStillValid(key, CrewChief.currentGameState);
                        Boolean queueTooLongForMessage = queuedMessage.maxPermittedQueueLengthForMessage != 0 && willBePlayedCount > queuedMessage.maxPermittedQueueLengthForMessage;
                        Boolean hasJustPlayedAsAnImmediateMessage = !isImmediateMessages && lastImmediateMessageName != null &&
                            key == lastImmediateMessageName && GameStateData.CurrentTime - lastImmediateMessageTime < TimeSpan.FromSeconds(5);
                        if ((isImmediateMessages || !keepQuiet || queuedMessage.playEvenWhenSilenced) && queuedMessage.canBePlayed &&
                            messageIsStillValid && !keysToPlay.Contains(key) && !queueTooLongForMessage && !messageHasExpired && !hasJustPlayedAsAnImmediateMessage)
                        {
                            // special case for 'get ready' event here - we don't want to move this to the top of the queue because 
                            // it makes it sound shit. Bit of a hack, needs a better solution
                            if (firstMovableEventWithPrefixOrSuffix == null && key != LapCounter.folderGetReady && soundCache.eventHasPersonalisedPrefixOrSuffix(key))
                            {
                                firstMovableEventWithPrefixOrSuffix = key;
                            }
                            else
                            {
                                keysToPlay.Add(key);
                            }
                        }
                        else
                        {
                            if (!messageIsStillValid)
                            {
                                Console.WriteLine("Clip " + key + " is not valid");
                            }
                            else if (messageHasExpired)
                            {
                                Console.WriteLine("Clip " + key + " has expired");
                            }
                            else if (queueTooLongForMessage)
                            {
                                List<String> keysToDisplay = new List<string>();
                                foreach (String keyToDisplay in queueToPlay.Keys)
                                {
                                    keysToDisplay.Add(keyToDisplay);
                                }
                                Console.WriteLine("Queue is too long to play clip " + key + " max permitted items for this message = "
                                    + queuedMessage.maxPermittedQueueLengthForMessage + " queue: " + String.Join(", ", keysToDisplay));
                            }
                            else if (!queuedMessage.canBePlayed)
                            {
                                Console.WriteLine("Clip " + key + " has some missing sound files");
                            }
                            else if (hasJustPlayedAsAnImmediateMessage)
                            {
                                Console.WriteLine("Clip " + key + " has just been played in response to a voice command, skipping");
                            }
                            else
                            {
                                Console.WriteLine("Clip " + key + " will not be played");
                            }
                            soundsProcessed.Add(key);
                            willBePlayedCount--;
                        }
                    }
                    // if we've just processed a 'rant' here, set the flag to false
                    if (queuedMessage.isRant)
                    {
                        AudioPlayer.rantWaitingToPlay = false;
                    }
                }
                if (firstMovableEventWithPrefixOrSuffix != null)
                {
                    keysToPlay.Insert(0, firstMovableEventWithPrefixOrSuffix);
                }
                if (keysToPlay.Count > 0)
                {
                    if (keysToPlay.Count == 1 && clipIsPearlOfWisdom(keysToPlay[0]))
                    {
                        if (hasPearlJustBeenPlayed())
                        {
                            Console.WriteLine("Rejecting pearl of wisdom " + keysToPlay[0] +
                                " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                            soundsProcessed.Add(keysToPlay[0]);
                        }
                        else if (disablePearlsOfWisdom)
                        {
                            Console.WriteLine("Rejecting pearl of wisdom " + keysToPlay[0] +
                                   " because pearls have been disabled for the last phase of the race");
                            soundsProcessed.Add(keysToPlay[0]);
                        }
                    }
                    else
                    {
                        oneOrMoreEventsEnabled = true;
                    }
                }
            }

            Boolean wasInterrupted = false;
            if (oneOrMoreEventsEnabled)
            {
                PlaybackModerator.PreProcessAddedKeys(keysToPlay);

                openRadioChannelInternal();
                soundsProcessed.AddRange(playSounds(keysToPlay, isImmediateMessages, out wasInterrupted));
            }
            else
            {
                soundsProcessed.AddRange(keysToPlay);
            }
            if (soundsProcessed.Count > 0)
            {
                lock (queueToPlay)
                {
                    foreach (String key in soundsProcessed)
                    {
                        if (queueToPlay.Contains(key))
                        {
                            queueToPlay.Remove(key);
                        }
                    }
                }
            }
            // now we go back and play anything else that's been inserted into the queue since we started, but only if
            // we've not been interrupted
            if (queueHasDueMessages(queueToPlay, isImmediateMessages) && (isImmediateMessages || !wasInterrupted))
            {
                Console.WriteLine("There are " + queueToPlay.Count + " more events in the queue, playing them...");
                playQueueContents(queueToPlay, isImmediateMessages);
            }
        }

        private Boolean queueHasDueMessages(OrderedDictionary queueToCheck, Boolean isImmediateMessages)
        {
            if (isImmediateMessages)
            {
                // immediate messages can't be delayed so no point in checking their due times
                return queueToCheck.Count > 0;
            }
            else if (queueToCheck.Count == 0)
            {
                return false;
            }
            else
            {
                long milliseconds = GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond;
                lock (queueToCheck)
                {
                    foreach (String key in queueToCheck.Keys)
                    {
                        if (((QueuedMessage)queueToCheck[key]).dueTime <= milliseconds)
                        {
                            return true;
                        }
                    }
                }
                return false;
            }
        }

        private List<String> playSounds(List<String> eventNames, Boolean isImmediateMessages, out Boolean wasInterrupted)
        {
            Console.WriteLine("Playing sounds, events: " + String.Join(", ", eventNames));
            List<String> soundsProcessed = new List<String>();

            OrderedDictionary thisQueue = isImmediateMessages ? immediateClips : queuedClips;
            wasInterrupted = false;
            int playedEventCount = 0;
            foreach (String eventName in eventNames)
            {
                // if there's anything in the immediateClips queue, stop processing
                if (isImmediateMessages || immediateClips.Count == 0)
                {
                    if (thisQueue.Contains(eventName))
                    {
                        QueuedMessage thisMessage = (QueuedMessage)thisQueue[eventName];
                        if (!isImmediateMessages && playedEventCount > 0 && pauseBetweenMessages > 0)
                        {
                            Console.WriteLine("Pausing before " + eventName);
                            Utilities.InterruptedSleep((int)Math.Round(pauseBetweenMessages * 1000.0f) /*totalWaitMillis*/, 10 /*waitWindowMillis*/, () => monitorRunning /*keepWaitingPredicate*/);
                        }
                        //  now double check this is still valid
                        if (!isImmediateMessages)
                        {
                            Boolean messageHasExpired = thisMessage.expiryTime != 0 && thisMessage.expiryTime < GameStateData.CurrentTime.Ticks / TimeSpan.TicksPerMillisecond; ;
                            Boolean messageIsStillValid = thisMessage.isMessageStillValid(eventName, CrewChief.currentGameState);
                            Boolean hasJustPlayedAsAnImmediateMessage = lastImmediateMessageName != null &&
                                eventName == lastImmediateMessageName && GameStateData.CurrentTime - lastImmediateMessageTime < TimeSpan.FromSeconds(5);
                            if (messageHasExpired || !messageIsStillValid || hasJustPlayedAsAnImmediateMessage)
                            {
                                Console.WriteLine("Skipping message " + eventName +
                                    ", messageHasExpired:" + messageHasExpired + ", messageIsStillValid:" + messageIsStillValid + ", hasJustPlayedAsAnImmediateMessage:" + hasJustPlayedAsAnImmediateMessage);
                                soundsProcessed.Add(eventName);
                                continue;
                            }
                        }
                        if (clipIsPearlOfWisdom(eventName))
                        {
                            soundsProcessed.Add(eventName);
                            if (hasPearlJustBeenPlayed())
                            {
                                Console.WriteLine("Rejecting pearl of wisdom " + eventName +
                                    " because one has been played in the last " + minTimeBetweenPearlsOfWisdom + " seconds");
                                continue;
                            }
                            else if (!allowPearlsOnNextPlay)
                            {
                                Console.WriteLine("Rejecting pearl of wisdom " + eventName +
                                    " because they've been temporarily disabled");
                                continue;
                            }
                            else if (disablePearlsOfWisdom)
                            {
                                Console.WriteLine("Rejecting pearl of wisdom " + eventName +
                                        " because pearls have been disabled for the last phase of the race");
                                continue;
                            }
                            else
                            {
                                timeLastPearlOfWisdomPlayed = GameStateData.CurrentTime;
                                String messageStringContent = thisMessage.ToString();
                                if (messageStringContent != "")
                                {
                                    Console.WriteLine(messageStringContent);
                                }
                                if (!mute)
                                {
                                    soundCache.Play(eventName, thisMessage.metadata);
                                    timeOfLastMessageEnd = GameStateData.CurrentTime;
                                }
                                else
                                {
                                    Console.WriteLine("Skipping message " + eventName + " because we're muted");
                                }
                            }
                        }
                        else
                        {
                            if (thisMessage.messageName != AudioPlayer.folderDidntUnderstand &&
                                thisMessage.messageName != AudioPlayer.folderStandBy && thisMessage.metadata.type != SoundType.SPOTTER)
                            {
                                // only cache the last message for repeat if it's an actual message
                                lastMessagePlayed = thisMessage;
                            }
                            lastMessagePlayed = thisMessage;
                            String messageStringContent = thisMessage.ToString();
                            if (messageStringContent != "")
                            {
                                Console.WriteLine(messageStringContent);
                            }
                            if (!mute)
                            {
                                if (thisMessage.delayMessageResolution)
                                {
                                    thisMessage.resolveDelayedContents();
                                }
                                soundCache.Play(thisMessage.messageFolders, thisMessage.metadata);
                                timeOfLastMessageEnd = GameStateData.CurrentTime;
                            }
                            else
                            {
                                Console.WriteLine("Skipping message " + eventName + " because we're muted");
                            }
                            int playedCount = -1;
                            if(playedMessagesCount.TryGetValue(eventName, out playedCount))
                            {
                                playedMessagesCount[eventName] = ++playedCount;
                            }
                            else
                            {
                                playedMessagesCount.Add(eventName, 1);
                            }
                            soundsProcessed.Add(eventName);
                        }
                        playedEventCount++;
                    }
                    else
                    {
                        Console.WriteLine("Event " + eventName + " is no longer in the queue");
                        if (CrewChief.Debugging)
                        {
                            Console.WriteLine("The " + (isImmediateMessages ? "immediate" : "regular") + " queue contains " + String.Join(", ", thisQueue.Keys));
                        }
                    }
                }
                else
                {
                    Console.WriteLine("We've been interrupted after playing " + playedEventCount + " events");
                    wasInterrupted = true;
                    break;
                }
            }
            return soundsProcessed;
        }

        private void openRadioChannelInternal()
        {
            if (!channelOpen)
            {
                channelOpen = true;
                if (!mute)
                {
                    try
                    {
                        this.backgroundPlayer.play();
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
                if (useShortBeepWhenOpeningChannel)
                {
                    playShortStartSpeakingBeep();
                }
                else
                {
                    playStartSpeakingBeep();
                }
            }
        }

        private void closeRadioInternalChannel()
        {
            if (channelOpen)
            {
                playEndSpeakingBeep();
                if (!mute)
                {
                    try
                    {
                        this.backgroundPlayer.stop();
                    }
                    catch (Exception)
                    {
                        // ignore
                    }
                }
                if (soundCache != null)
                {
                    soundCache.ExpireCachedSounds();
                }
            }
            useShortBeepWhenOpeningChannel = false;
            channelOpen = false;
        }
        
        public void playStartSpeakingBeep()
        {
            if (!mute)
            {
                var soundToPlay = PlaybackModerator.GetSuggestedBleepStart();
                soundCache.Play(soundToPlay, SoundMetadata.beep);
            }
        }

        public void playStartListeningBeep()
        {
            if (useListenBeep)
            {
                if (!mute)
                {
                    soundCache.Play("listen_start_sound", SoundMetadata.beep);
                }
            }
        }

        public void playShortStartSpeakingBeep()
        {
            if (!mute)
            {
                var soundToPlay = PlaybackModerator.GetSuggestedBleepShorStart();
                soundCache.Play(soundToPlay, SoundMetadata.beep);
            }
        }

        public void playEndSpeakingBeep()
        {
            if (!mute)
            {
                var soundToPlay = PlaybackModerator.GetSuggestedBleepEnd();
                soundCache.Play(soundToPlay, SoundMetadata.beep);
            }
        }

        public int purgeQueues()
        {
            return purgeQueue(queuedClips, false) + purgeQueue(immediateClips, true);
        }

        private int purgeQueue(OrderedDictionary queue, bool isImmediateQueue)
        {
            Console.WriteLine("Purging " + (isImmediateQueue ? "immediate" : "regular") + " queue" );
            int purged = 0;
            lock (queue)
            {
                ArrayList keysToPurge = new ArrayList(queue.Keys);
                foreach (String keyStr in keysToPurge)
                {
                    try
                    {
                        if (!keyStr.Contains(SessionEndMessages.sessionEndMessageIdentifier) &&
                            !keyStr.Contains(SmokeTest.SMOKE_TEST) &&
                            !keyStr.Contains(SmokeTest.SMOKE_TEST_SPOTTER))
                        {
                            queue.Remove(keyStr);
                            purged++;
                        }
                    }
                    catch (Exception)
                    {
                        // ignore - not sure why I'm try-catching here :)
                    }
                }
            }
            return purged;
        }

        public Boolean isChannelOpen()
        {
            return channelOpen;
        }

        public void playMessage(QueuedMessage queuedMessage, int priority = SoundMetadata.DEFAULT_PRIORITY)
        {
            if (GlobalBehaviourSettings.enabledMessageTypes.Contains(MessageTypes.NONE))
            {
                Console.WriteLine("All messages disabled for this car class. Message " + queuedMessage.messageName + " will not be played");
            } 
            else
            {
                playMessage(queuedMessage, PearlsOfWisdom.PearlType.NONE, 0, priority);
            }
        }

        // WIP... sometimes the chief loses his shit
        public Boolean playRant(String messageIdentifier, List<MessageFragment> messagesToPlayBeforeRanting)
        {
            if (sweary && !playedRantInThisSession && Utilities.random.NextDouble() < rantLikelihood)
            {
                playedRantInThisSession = true;
                AudioPlayer.rantWaitingToPlay = true;
                List<MessageFragment> messageContents = new List<MessageFragment>();
                if (messagesToPlayBeforeRanting != null)
                {
                    messageContents.AddRange(messagesToPlayBeforeRanting);
                }
                messageContents.Add(MessageFragment.Text(folderRants));
                QueuedMessage rant = new QueuedMessage(messageIdentifier, messageContents, 0, null);
                rant.isRant = true;
                playMessage(rant, PearlsOfWisdom.PearlType.NONE, 0);
                return true;
            }
            return false;
        }

        // this should only be called in response to a voice message, following a 'standby' request. We want to play the 
        // message via the 'immediate' mechanism, but not until the secondsDelay has expired.
        public void playDelayedImmediateMessage(QueuedMessage queuedMessage)
        {
            ThreadManager.UnregisterTemporaryThread(playDelayedImmediateMessageThread);
            playDelayedImmediateMessageThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if (queuedMessage.secondsDelay > 0)
                {
                    Utilities.InterruptedSleep(queuedMessage.secondsDelay * 1000 /*totalWaitMillis*/, 500 /*waitWindowMillis*/, () => monitorRunning /*keepWaitingPredicate*/);
                }
                playMessageImmediately(queuedMessage);
            });
            playDelayedImmediateMessageThread.Name = "AudioPlayer.playDelayedImmediateMessageThread";
            playDelayedImmediateMessageThread.Start();
            ThreadManager.RegisterTemporaryThread(playDelayedImmediateMessageThread);
        }

        // when we keep the channel open for long running spotter repeat calls, there's a chance that
        // it'll remain open indefinitely (if the last spotter call was an overlap and no clear was 
        // received, e.g. the game was closed). So when openning the channel in 'hold' mode, we spawn 
        // a Thread that waits for 6 seconds and cleans up if necessary
        private void startHangingChannelCloseThread()
        {
            // ensure an existing thread is stopped properly - can one be created while another is waiting on the monitor?
            hangingChannelCloseWakeUpEvent.Set();
            if (hangingChannelCloseThread != null)
            {
                if (!hangingChannelCloseThread.Join(3000))
                {
                    Console.WriteLine("Warning: Timed out waiting for thread: " + hangingChannelCloseThread.Name);
                }
            }
            ThreadManager.UnregisterTemporaryThread(hangingChannelCloseThread);
            // reset the wait monitor after the .Set call
            hangingChannelCloseWakeUpEvent.Reset();
            hangingChannelCloseThread = new Thread(() =>
            {
                Thread.CurrentThread.IsBackground = true;
                if (!hangingChannelCloseWakeUpEvent.WaitOne(6000))
                {
                    // if we timeout here it means the channel was left open, so close it
                    closeRadioInternalChannel();
                }
            });
            hangingChannelCloseThread.Name = "AudioPlayer.hangingChannelCloseThread";
            hangingChannelCloseThread.Start();
            ThreadManager.RegisterTemporaryThread(hangingChannelCloseThread);
        }

        private void stopHangingChannelCloseThread()
        {
            hangingChannelCloseWakeUpEvent.Set();
        }

        public SoundType getPriortyOfFirstWaitingImmediateMessage()
        {
            QueuedMessage message = getFirstWaitingImmediateMessage(SoundType.OTHER);
            return message == null ? SoundType.OTHER : message.metadata.type;
        }

        public QueuedMessage getFirstWaitingImmediateMessage(SoundType minType)
        {
            lock (immediateClips)
            {
                foreach (Object value in immediateClips.Values)
                {
                    QueuedMessage message = (QueuedMessage)value;
                    if (message.metadata.type <= minType)
                    {
                        return message;
                    }
                }
            }
            return null;
        }

        public void playMessageImmediately(QueuedMessage queuedMessage, Boolean keepChannelOpen = false)
        {
            if (queuedMessage.canBePlayed)
            {
                lock (immediateClips)
                {
                    if (immediateClips.Contains(queuedMessage.messageName))
                    {
                        Console.WriteLine("Clip for event " + queuedMessage.messageName + " is already queued, ignoring");
                        return;
                    }
                    else
                    {
                        lastImmediateMessageName = queuedMessage.messageName;
                        lastImmediateMessageTime = GameStateData.CurrentTime;
                        this.useShortBeepWhenOpeningChannel = false;
                        this.holdChannelOpen = keepChannelOpen;
                        if (this.holdChannelOpen)
                        {
                            startHangingChannelCloseThread();
                        }

                        // here we assume the message is a voice command response, which is the most common use case 
                        // for non-spotter immediate messages
                        populateSoundMetadata(queuedMessage, SoundType.VOICE_COMMAND_RESPONSE, 5);
                        // sanity check...
                        if (queuedMessage.metadata.type == SoundType.REGULAR_MESSAGE)
                        {
                            // a regular message will not play from the immediate queue
                            Console.WriteLine("Message " + queuedMessage.messageName + " is in the immediate queue but is type 'regular' - this will not play. Setting the type to 'important'");
                            queuedMessage.metadata.type = SoundType.IMPORTANT_MESSAGE;
                        }
                        immediateClips.Insert(getInsertionIndex(immediateClips, queuedMessage), queuedMessage.messageName, queuedMessage);

                        // wake up the monitor thread immediately
                        monitorQueueWakeUpEvent.Set();
                    }
                }
            }            
        }

        public void playSpotterMessage(QueuedMessage queuedMessage, Boolean keepChannelOpen)
        {
            if (queuedMessage.canBePlayed)
            {
                lock (immediateClips)
                {
                    if (immediateClips.Contains(queuedMessage.messageName))
                    {
                        Console.WriteLine("Clip for event " + queuedMessage.messageName + " is already queued, ignoring");
                        return;
                    }
                    else
                    {
                        this.useShortBeepWhenOpeningChannel = true;
                        this.holdChannelOpen = keepChannelOpen;
                        if (this.holdChannelOpen)
                        {
                            startHangingChannelCloseThread();
                        }
                        // default spotter priority is 10
                        populateSoundMetadata(queuedMessage, SoundType.SPOTTER, 10);
                        immediateClips.Insert(getInsertionIndex(immediateClips, queuedMessage), queuedMessage.messageName, queuedMessage);

                        // wake up the monitor thread immediately
                        monitorQueueWakeUpEvent.Set();
                    }
                }
            }            
        }

        private int getInsertionIndex(OrderedDictionary queue, QueuedMessage queuedMessage)
        {
            int index = 0;
            foreach (Object value in queue.Values)
            {
                int existingMessagePriority = ((QueuedMessage)value).metadata.priority;
                if (queuedMessage.metadata.priority > existingMessagePriority)
                {
                    // the existing message is lower priorty than the one we're adding so we've found the index we want                 
                    break;
                }
                index++;
            }
            return index;
        }

        public void playMessage(QueuedMessage queuedMessage, PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability, int priority = SoundMetadata.DEFAULT_PRIORITY)
        {
            if (queuedMessage.canBePlayed)
            {
                lock (queuedClips)
                {
                    if (queuedClips.Contains(queuedMessage.messageName))
                    {
                        Console.WriteLine("Clip for event " + queuedMessage.messageName + " is already queued, ignoring");
                        return;
                    }
                    else
                    {
                        // default 'regular' message priority is 0, which is lowest
                        populateSoundMetadata(queuedMessage, SoundType.REGULAR_MESSAGE, priority);
                        DateTime now = CrewChief.currentGameState == null ? DateTime.UtcNow : CrewChief.currentGameState.Now;
                        if (PlaybackModerator.MessageCanBeQueued(queuedMessage, queuedClips.Count, now))
                        {
                            PearlsOfWisdom.PearlMessagePosition pearlPosition = PearlsOfWisdom.PearlMessagePosition.NONE;
                            if (pearlType != PearlsOfWisdom.PearlType.NONE && checkPearlOfWisdomValid(pearlType))
                            {
                                pearlPosition = pearlsOfWisdom.getMessagePosition(pearlMessageProbability);
                            }

                            int insertionIndex = getInsertionIndex(queuedClips, queuedMessage);
                            if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.BEFORE)
                            {
                                QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                                pearlQueuedMessage.metadata = queuedMessage.metadata;
                                pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                                queuedClips.Insert(insertionIndex, PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                                insertionIndex++;
                            }
                            queuedClips.Insert(insertionIndex, queuedMessage.messageName, queuedMessage);
                            if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.AFTER)
                            {
                                QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                                pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                                pearlQueuedMessage.metadata = queuedMessage.metadata;
                                insertionIndex++;
                                queuedClips.Insert(insertionIndex, PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                            }

                            // note that we don't wake the monitor Thread here - we wait until all the events have completed on this tick
                            // then check the queue
                        }
                    }
                }
            }            
        }

        public Boolean removeQueuedMessage(String eventName)
        {
            lock (queuedClips)
            {
                if (queuedClips.Contains(eventName))
                {
                    queuedClips.Remove(eventName);
                    return true;
                }
                return false;
            }
        }

        public void removeImmediateMessages(String[] eventNames)
        {
            lock (immediateClips)
            {
                foreach (String eventName in eventNames)
                {
                    if (immediateClips.Contains(eventName))
                    {
                        Console.WriteLine("Removing immediate clip " + eventName);
                        immediateClips.Remove(eventName);
                    }
                }
            }
        }
        
        // checks that another pearl isn't already queued. If one of the same type is already
        // in the queue this method just returns false. If a conflicting pearl is in the queue
        // this method removes it and returns false, so we don't end up with, for example, 
        // a 'keep it up' message in a block that contains a 'your lap times are worsening' message
        private Boolean checkPearlOfWisdomValid(PearlsOfWisdom.PearlType newPearlType)
        {
            Boolean isValid = true;
            if (queuedClips != null && queuedClips.Count > 0)
            {
                List<String> pearlsToPurge = new List<string>();
                foreach (String eventName in queuedClips.Keys)
                {
                    if (clipIsPearlOfWisdom(eventName))
                    {
                        Console.WriteLine("There's already a pearl in the queue, can't add another");
                        isValid = false;
                        if (eventName != PearlsOfWisdom.getMessageFolder(newPearlType))
                        {
                            pearlsToPurge.Add(eventName);
                        }
                    }
                }
                foreach (String pearlToPurge in pearlsToPurge)
                {
                    queuedClips.Remove(pearlToPurge);
                    Console.WriteLine("Queue contains a pearl " + pearlToPurge + " which conflicts with " + newPearlType);
                }
            }
            return isValid;
        }

        private Boolean clipIsPearlOfWisdom(String eventName)
        {
            foreach (PearlsOfWisdom.PearlType pearlType in Enum.GetValues(typeof(PearlsOfWisdom.PearlType)))
            {
                if (pearlType != PearlsOfWisdom.PearlType.NONE && PearlsOfWisdom.getMessageFolder(pearlType) == eventName)
                {
                    return true;
                }
            }
            return false;
        }

        private Boolean hasPearlJustBeenPlayed()
        {
            return timeLastPearlOfWisdomPlayed.Add(minTimeBetweenPearlsOfWisdom) > GameStateData.CurrentTime;
        }

        public void suspendPearlsOfWisdom()
        {
            allowPearlsOnNextPlay = false;
        }

        public void repeatLastMessage()
        {
            if (lastMessagePlayed != null)
            {
                // clear the validation, expiry and other data
                lastMessagePlayed.prepareToBeRepeated(getMessageId());
                playMessageImmediately(lastMessagePlayed);
            }
        }

        public void Dispose()
        {
            backgroundPlayer.dispose();
            if (soundCache != null)
            {
                try
                {
                    soundCache.StopAndUnloadAll();
                }
                catch (Exception) { }
                soundCache = null;
            }
        }

        public void pauseQueue(int seconds)
        {
            if (!regularQueuePaused)
            {
                regularQueuePaused = true;

                ThreadManager.UnregisterTemporaryThread(pauseQueueThread);
                pauseQueueThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    if (seconds > 0)
                    {
                        Utilities.InterruptedSleep(seconds * 1000 /*totalWaitMillis*/, 500 /*waitWindowMillis*/, () => monitorRunning /*keepWaitingPredicate*/);
                    }
                    regularQueuePaused = false;
                    // wake the monitor thread as soon as the pause has expired
                    this.monitorQueueWakeUpEvent.Set();
                });
                pauseQueueThread.Name = "AudioPlayer.pauseQueueThread";
                ThreadManager.RegisterTemporaryThread(pauseQueueThread);
                pauseQueueThread.Start();
            }
        }

        public void unpauseQueue()
        {
            regularQueuePaused = false;
        }

        public static Boolean canReadName(String rawName)
        {
            Debug.Assert(!string.IsNullOrWhiteSpace(rawName), "rawName should never be an empty string, this is a bug.");

            return !string.IsNullOrWhiteSpace(rawName) && CrewChief.enableDriverNames &&
                ((SoundCache.hasSuitableTTSVoice && ttsOption != TTS_OPTION.NEVER) || SoundCache.availableDriverNames.Contains(DriverNameHelper.getUsableDriverName(rawName)));
        }

        // defaultSoundType is only used if we've not already added metadata
        // defaultPriority is only used if we've not already added metadata
        private void populateSoundMetadata(QueuedMessage queuedMessage, SoundType defaultSoundType, int defaultPriority)
        {
            if (queuedMessage.metadata == null)
            {
                queuedMessage.metadata = new SoundMetadata(defaultSoundType, defaultPriority);
            }
            queuedMessage.metadata.messageId = getMessageId();
        }

        private int getMessageId()
        {
            lock (this)
            {
                this.messageId++;
                return this.messageId;
            }
        }
    }
}
