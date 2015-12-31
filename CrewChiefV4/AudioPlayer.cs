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

namespace CrewChiefV4
{
    public class AudioPlayer
    {
        public Boolean disablePearlsOfWisdom = false;   // used for the last 2 laps / 3 minutes of a race session only
        public Boolean mute = false;
        public static float minimumSoundPackVersion = 41f;

        private CrewChief crewChief;

        public static String folderAcknowlegeOK = "acknowledge/OK";
        public static String folderAcknowlegeEnableKeepQuiet = "acknowledge/keepQuietEnabled";
        public static String folderEnableSpotter = "acknowledge/spotterEnabled";
        public static String folderDisableSpotter = "acknowledge/spotterDisabled";
        public static String folderAcknowlegeDisableKeepQuiet = "acknowledge/keepQuietDisabled";
        public static String folderDidntUnderstand = "acknowledge/didnt_understand";
        public static String folderNoData = "acknowledge/no_data";
        public static String folderYes = "acknowledge/yes";
        public static String folderNo = "acknowledge/no";
        public static String folderDeltasEnabled = "acknowledge/deltasEnabled";
        public static String folderDeltasDisabled = "acknowledge/deltasDisabled";

        public static Boolean useAlternateBeeps = UserSettings.GetUserSettings().getBoolean("use_alternate_beeps");
        public static float pauseBetweenMessages = UserSettings.GetUserSettings().getFloat("pause_between_messages");

        private QueuedMessage lastMessagePlayed = null;

        private Boolean allowPearlsOnNextPlay = true;

        private Dictionary<String, int> playedMessagesCount = new Dictionary<String, int>();

        public static List<String> allMessageNames = new List<String>();

        public static List<String> availableDriverNames = new List<String>();

        private Boolean monitorRunning = false;

        private Boolean keepQuiet = false;
        private Boolean channelOpen = false;

        private Boolean requestChannelOpen = false;
        private Boolean requestChannelClose = false;
        private Boolean holdChannelOpen = false;
        private Boolean useShortBeepWhenOpeningChannel = false;

        private readonly TimeSpan queueMonitorInterval = TimeSpan.FromMilliseconds(1000);

        private readonly int immediateMessagesMonitorInterval = 20;

        private Dictionary<String, List<SoundPlayer>> clips = new Dictionary<String, List<SoundPlayer>>();

        private String soundFolderName = UserSettings.GetUserSettings().getString("sound_files_path");
        private Boolean useListenBeep = UserSettings.GetUserSettings().getBoolean("use_listen_beep");

        private String voiceFolderPath;

        private String fxFolderPath;

        private String driverNamesFolderPath;

        private readonly TimeSpan minTimeBetweenPearlsOfWisdom = TimeSpan.FromSeconds(UserSettings.GetUserSettings().getInt("minimum_time_between_pearls_of_wisdom"));

        private Boolean sweary = UserSettings.GetUserSettings().getBoolean("use_sweary_messages");

        // if this is true, no 'green green green', 'get ready', or spotter messages are played
        private Boolean disableImmediateMessages = UserSettings.GetUserSettings().getBoolean("disable_immediate_messages");

        private Random random = new Random();

        private OrderedDictionary queuedClips = new OrderedDictionary();

        private OrderedDictionary immediateClips = new OrderedDictionary();

        List<String> enabledSounds = new List<String>();

        Boolean enableStartBleep = false;

        Boolean enableEndBleep = false;

        MediaPlayer backgroundPlayer;

        public static String soundFilesPath;

        private String backgroundFilesPath;

        // TODO: sort looping callback out so we don't need this...
        private int backgroundLeadout = 30;

        public static String dtmPitWindowOpenBackground = "dtm_pit_window_open.wav";

        public static String dtmPitWindowClosedBackground = "dtm_pit_window_closed.wav";

        // only the monitor Thread can request a reload of the background wav file, so
        // the events thread will have to set these variables to ask for a reload
        private Boolean loadNewBackground = false;
        private String backgroundToLoad;

        private PearlsOfWisdom pearlsOfWisdom;

        DateTime timeLastPearlOfWisdomPlayed = DateTime.UtcNow;

        private Boolean backgroundPlayerInitialised = false;

        public Boolean initialised = false;

        public static float soundPackVersion = 0;
        public static float driverNamesVersion = 0;

        public AudioPlayer(CrewChief crewChief)
        {
            this.crewChief = crewChief;
            if (soundFolderName.Length > 3 && (soundFolderName.Substring(1, 2) == @":\" || soundFolderName.Substring(1, 2) == @":/"))
            {
                soundFilesPath = soundFolderName;
            }
            else
            {
                if (System.Diagnostics.Debugger.IsAttached)
                {
                    soundFilesPath = Path.Combine(Path.GetDirectoryName(
                                            System.Reflection.Assembly.GetEntryAssembly().Location), @"..\", @"..\", soundFolderName);
                }
                else
                {
                    soundFilesPath = Path.Combine(Path.GetDirectoryName(
                                            System.Reflection.Assembly.GetEntryAssembly().Location), soundFolderName);
                }
            }
            DirectoryInfo soundDirectory = new DirectoryInfo(soundFilesPath);
            if (soundDirectory.Exists) 
            {
                float[] versions = getSoundPackAndDriverNamesVersions(soundDirectory);
                soundPackVersion = versions[0];
                driverNamesVersion = versions[1];
            }
            else
            {
                soundDirectory.Create();
            }
        }

        public void initialise()
        {
            voiceFolderPath = Path.Combine(soundFilesPath, "voice");
            fxFolderPath = Path.Combine(soundFilesPath, "fx");
            driverNamesFolderPath = Path.Combine(soundFilesPath, "driver_names");
            backgroundFilesPath = Path.Combine(soundFilesPath, "background_sounds");
            Console.WriteLine("Voice dir full path = " + voiceFolderPath);
            Console.WriteLine("FX dir full path = " + fxFolderPath);
            Console.WriteLine("driver names full path = " + driverNamesFolderPath);
            Console.WriteLine("Background sound dir full path = " + backgroundFilesPath);
            DirectoryInfo soundDirectory = new DirectoryInfo(soundFilesPath);
            if (!soundDirectory.Exists)
            {
                Console.WriteLine("Unable to find sound directory " + soundDirectory.FullName);
                return;
            }            
            if (soundPackVersion == -1 || soundPackVersion == 0)
            {
                Console.WriteLine("Unable to get sound pack version - expected a file called version_info with a single line containing a version number, e.g. 2.0");
            }
            else if (soundPackVersion < minimumSoundPackVersion)
            {
                Console.WriteLine("The sound pack version in use is " + soundPackVersion + " but this version of the app requires version "
                    + minimumSoundPackVersion + " or greater.");
                Console.WriteLine("You must update your sound pack to run this application");
                return;
            }
            else
            {
                Console.WriteLine("Minimum sound pack version = " + minimumSoundPackVersion + " using sound pack version " + soundPackVersion);
            }
            pearlsOfWisdom = new PearlsOfWisdom();
            int soundsCount = 0;
            try
            {
                DirectoryInfo fxSoundDirectory = new DirectoryInfo(fxFolderPath);
                if (!fxSoundDirectory.Exists)
                {
                    Console.WriteLine("Unable to find fx directory " + fxSoundDirectory.FullName);
                    return;
                }
                FileInfo[] bleepFiles = fxSoundDirectory.GetFiles();
                String alternate_prefix = useAlternateBeeps ? "alternate_" : "";
                foreach (FileInfo bleepFile in bleepFiles)
                {
                    if (bleepFile.Name.EndsWith(".wav"))
                    {
                        if (bleepFile.Name.StartsWith(alternate_prefix + "start"))
                        {
                            enableStartBleep = true;
                            openAndCacheClip("start_bleep", bleepFile.FullName);
                        }
                        else if (bleepFile.Name.StartsWith(alternate_prefix + "end"))
                        {
                            enableEndBleep = true;
                            openAndCacheClip("end_bleep", bleepFile.FullName);
                        }
                        else if (bleepFile.Name.StartsWith(alternate_prefix + "short_start"))
                        {
                            enableEndBleep = true;
                            openAndCacheClip("short_start_bleep", bleepFile.FullName);
                        }
                        else if (bleepFile.Name.StartsWith("listen_start"))
                        {
                            openAndCacheClip("listen_start_sound", bleepFile.FullName);
                        }
                    }
                }
                DirectoryInfo voiceSoundDirectory = new DirectoryInfo(voiceFolderPath);
                if (!voiceSoundDirectory.Exists)
                {
                    Console.WriteLine("Unable to find voice directory " + voiceSoundDirectory.FullName);
                    return;
                }
                DirectoryInfo[] eventFolders = voiceSoundDirectory.GetDirectories();
                foreach (DirectoryInfo eventFolder in eventFolders)
                {
                    try
                    {
                        //Console.WriteLine("Got event folder " + eventFolder.Name);
                        DirectoryInfo[] eventDetailFolders = eventFolder.GetDirectories();
                        foreach (DirectoryInfo eventDetailFolder in eventDetailFolders)
                        {
                            //Console.WriteLine("Got event detail subfolder " + eventDetailFolder.Name);
                            String fullEventName = eventFolder + "/" + eventDetailFolder;
                            try
                            {
                                FileInfo[] soundFiles = eventDetailFolder.GetFiles();
                                foreach (FileInfo soundFile in soundFiles)
                                {
                                    if (soundFile.Name.EndsWith(".wav") && (sweary || !soundFile.Name.StartsWith("sweary")))
                                    {
                                        //Console.WriteLine("Got sound file " + soundFile.FullName);
                                        soundsCount++;
                                        openAndCacheClip(eventFolder + "/" + eventDetailFolder, soundFile.FullName);
                                        if (!enabledSounds.Contains(fullEventName))
                                        {
                                            enabledSounds.Add(fullEventName);
                                        }
                                    }
                                }
                                if (!enabledSounds.Contains(fullEventName))
                                {
                                    Console.WriteLine("Event " + fullEventName + " has no sound files");
                                }
                            }
                            catch (DirectoryNotFoundException)
                            {
                                Console.WriteLine("Event subfolder " + fullEventName + " not found");
                            }
                        }
                    }
                    catch (DirectoryNotFoundException)
                    {
                        Console.WriteLine("Unable to find events folder");
                    }
                }
                Console.WriteLine("Cached " + soundsCount + " clips");
                initialised = true;
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Unable to find sounds directory - path: " + soundFolderName);
            }
        }

        public void cacheDriverName(String driverName)
        {
            DirectoryInfo driverNamesSoundDirectory = new DirectoryInfo(driverNamesFolderPath);
            if (!driverNamesSoundDirectory.Exists)
            {
                Console.WriteLine("Unable to find driver names directory " + driverNamesSoundDirectory.FullName);
                return;
            }
            FileInfo[] driverNamesFiles = driverNamesSoundDirectory.GetFiles();
            foreach (FileInfo driverNameFile in driverNamesFiles)
            {
                if (driverNameFile.Name.EndsWith(".wav"))
                {
                    if (driverNameFile.Name.ToLower().Equals(driverName.ToLower() + ".wav") ||
                            driverNameFile.Name.Equals(driverName + ".wav") ||
                        driverNameFile.Name.ToLowerInvariant().Equals(driverName.ToLower() + ".wav"))
                    {
                        if (!clips.ContainsKey(driverName))
                        {
                            Console.WriteLine("Caching driver name sound file for " + driverName);
                            SoundPlayer clip = new SoundPlayer(driverNameFile.FullName);
                            clip.Load();
                            List<SoundPlayer> driverNameClips = new List<SoundPlayer>();
                            driverNameClips.Add(clip);
                            clips.Add(driverName, driverNameClips);
                        }
                        if (!availableDriverNames.Contains(driverName))
                        {
                            availableDriverNames.Add(driverName);
                        }
                    }
                }
            }
        }

        public void cacheDriverNames(List<String> driverNames)
        {
            List<String> namesWithNoSoundFile = new List<string>();
            List<String> namesWithSoundFile = new List<String>();
            namesWithNoSoundFile.AddRange(driverNames);
            try
            {
                availableDriverNames.Clear();
                DirectoryInfo driverNamesSoundDirectory = new DirectoryInfo(driverNamesFolderPath);
                if (!driverNamesSoundDirectory.Exists)
                {
                    Console.WriteLine("Unable to find driver names directory " + driverNamesSoundDirectory.FullName);
                    return;
                }
                FileInfo[] driverNamesFiles = driverNamesSoundDirectory.GetFiles();
                foreach (FileInfo driverNameFile in driverNamesFiles)
                {
                    if (driverNameFile.Name.EndsWith(".wav"))
                    {
                        foreach (String driverName in driverNames)
                        {
                            if (driverNameFile.Name.ToLower().Equals(driverName.ToLower() + ".wav") ||
                                driverNameFile.Name.Equals(driverName + ".wav") ||
                                driverNameFile.Name.ToLowerInvariant().Equals(driverName.ToLower() + ".wav"))
                            {
                                namesWithNoSoundFile.Remove(driverName);
                                namesWithSoundFile.Add(driverName);
                                if (!clips.ContainsKey(driverName))
                                {
                                    SoundPlayer clip = new SoundPlayer(driverNameFile.FullName);
                                    clip.Load();
                                    List<SoundPlayer> driverNameClips = new List<SoundPlayer>();
                                    driverNameClips.Add(clip);
                                    clips.Add(driverName, driverNameClips);
                                }
                                if (!availableDriverNames.Contains(driverName))
                                {
                                    availableDriverNames.Add(driverName);
                                }
                            }
                        }
                    }
                }
                if (namesWithSoundFile.Count > 0)
                {
                    Console.WriteLine("Cached sound files for driver names:");
                    Console.WriteLine(String.Join(", ", namesWithSoundFile));
                }
                if (namesWithNoSoundFile.Count > 0)
                {
                    Console.WriteLine("These driver names have no sound file:");
                    Console.WriteLine(String.Join(", ", namesWithNoSoundFile));
                }
            }
            catch (DirectoryNotFoundException)
            {
                Console.WriteLine("Unable to find driver names directory - path: " + driverNamesFolderPath);
            }
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
                ThreadStart work;
                if (disableImmediateMessages)
                {
                    Console.WriteLine("Interupting and immediate messages are disabled - no spotter or 'green green green'");
                    work = monitorQueueNoImmediateMessages;
                }
                else
                {
                    work = monitorQueue;
                }
                Thread thread = new Thread(work);
                thread.Start();
            }
            new SmokeTest(this).trigger(new GameStateData(DateTime.Now.Ticks), new GameStateData(DateTime.Now.Ticks));
        }

        public void stopMonitor()
        {
            Console.WriteLine("Stopping queue monitor");
            monitorRunning = false;
        }

        private float getBackgroundVolume()
        {
            float volume = UserSettings.GetUserSettings().getFloat("background_volume");
            if (volume > 1)
            {
                volume = 1;
            }
            if (volume < 0)
            {
                volume = 0;
            }
            return volume;
        }

        public float[] getSoundPackAndDriverNamesVersions(DirectoryInfo soundDirectory)
        {
            FileInfo[] filesInSoundDirectory = soundDirectory.GetFiles();
            float[] versions = new float[] { 0, 0 };
            float soundfilesVersion = -1f;
            foreach (FileInfo fileInSoundDirectory in filesInSoundDirectory)
            {
                if (fileInSoundDirectory.Name == "version_info")
                {
                    String[] lines = File.ReadAllLines(Path.Combine(soundFilesPath, fileInSoundDirectory.Name));
                    foreach (String line in lines)
                    {
                        if (line.StartsWith("soundPack"))
                        {
                            float.TryParse(line.Split(':')[1], out versions[0]);
                        }
                        if (line.StartsWith("driverNames"))
                        {
                            float.TryParse(line.Split(':')[1], out versions[0]);
                        }
                    }
                }
            }
            return versions;
        }

        public void setBackgroundSound(String backgroundSoundName)
        {
            backgroundToLoad = backgroundSoundName;
            loadNewBackground = true;
        }

        private void initialiseBackgroundPlayer()
        {
            if (!backgroundPlayerInitialised && getBackgroundVolume() > 0)
            {
                backgroundPlayer = new MediaPlayer();
                backgroundPlayer.MediaEnded += new EventHandler(backgroundPlayer_MediaEnded);
                backgroundPlayer.Volume = getBackgroundVolume();
                setBackgroundSound(dtmPitWindowClosedBackground);
                backgroundPlayerInitialised = true;
            }
        }

        private void stopBackgroundPlayer()
        {
            if (backgroundPlayer != null && backgroundPlayerInitialised)
            {
                backgroundPlayer.Stop();
                backgroundPlayerInitialised = false;
                backgroundPlayer = null;
            }
        }

        private void monitorQueue()
        {
            Console.WriteLine("Monitor starting");
            initialiseBackgroundPlayer();
            DateTime nextQueueCheck = DateTime.Now;
            while (monitorRunning)
            {
                if (requestChannelOpen)
                {
                    openRadioChannelInternal();
                    requestChannelOpen = false;
                    holdChannelOpen = true;
                }
                if (!holdChannelOpen && channelOpen)
                {
                    closeRadioInternalChannel();
                }
                if (immediateClips.Count > 0)
                {
                    try
                    {
                        playQueueContents(immediateClips, true);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception processing immediate clips: " + e.Message);
                        lock (immediateClips)
                        {
                            immediateClips.Clear();
                        }
                    }
                }
                if (requestChannelClose)
                {
                    if (channelOpen)
                    {
                        if (!queueHasDueMessages(queuedClips, false) && !queueHasDueMessages(immediateClips, true))
                        {
                            requestChannelClose = false;
                            holdChannelOpen = false;
                            closeRadioInternalChannel();
                        }
                    }
                    else
                    {
                        requestChannelClose = false;
                        holdChannelOpen = false;
                    }
                }
                if (DateTime.Now > nextQueueCheck)
                {
                    nextQueueCheck = nextQueueCheck.Add(queueMonitorInterval);
                    try
                    {
                        playQueueContents(queuedClips, false);
                        allowPearlsOnNextPlay = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception processing queued clips: " + e.Message);
                        lock (queuedClips)
                        {
                            queuedClips.Clear();
                        }
                    }
                }
                else
                {
                    Thread.Sleep(immediateMessagesMonitorInterval);
                    continue;
                }
            }
            //writeMessagePlayedStats();
            playedMessagesCount.Clear();
            stopBackgroundPlayer();
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
            playClipImmediately(new QueuedMessage(folderAcknowlegeEnableKeepQuiet, 0, null), false);
            closeChannel();
            keepQuiet = true;
        }

        public void disableKeepQuietMode()
        {
            playClipImmediately(new QueuedMessage(folderAcknowlegeDisableKeepQuiet, 0, null), false);
            closeChannel();
            keepQuiet = false;
        }

        private void monitorQueueNoImmediateMessages()
        {
            initialiseBackgroundPlayer();
            while (monitorRunning)
            {
                Thread.Sleep(queueMonitorInterval);
                try
                {
                    playQueueContents(queuedClips, false);
                    allowPearlsOnNextPlay = true;
                }
                catch (Exception e)
                {
                    Console.WriteLine("Exception processing queued clips: " + e.Message);
                }
                if (!holdChannelOpen && channelOpen)
                {
                    closeRadioInternalChannel();
                }
            }
            writeMessagePlayedStats();
            playedMessagesCount.Clear();
            stopBackgroundPlayer();
        }

        private void playQueueContents(OrderedDictionary queueToPlay, Boolean isImmediateMessages)
        {
            long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
            List<String> keysToPlay = new List<String>();
            List<String> soundsProcessed = new List<String>();

            Boolean oneOrMoreEventsEnabled = false;
            lock (queueToPlay)
            {
                int willBePlayedCount = queueToPlay.Count;
                foreach (String key in queueToPlay.Keys)
                {
                    QueuedMessage queuedMessage = (QueuedMessage)queueToPlay[key];
                    if (isImmediateMessages || queuedMessage.dueTime <= milliseconds)
                    {
                        Boolean messageHasExpired = queuedMessage.expiryTime != 0 && queuedMessage.expiryTime < milliseconds;
                        Boolean messageIsStillValid = queuedMessage.isMessageStillValid(key, crewChief.currentGameState);
                        Boolean queueTooLongForMessage = queuedMessage.maxPermittedQueueLengthForMessage != 0 && willBePlayedCount > queuedMessage.maxPermittedQueueLengthForMessage;
                        if ((isImmediateMessages || !keepQuiet || queuedMessage.playEvenWhenSilenced) && queuedMessage.canBePlayed &&
                            messageIsStillValid && !keysToPlay.Contains(key) && !queueTooLongForMessage && !messageHasExpired)
                        {
                            keysToPlay.Add(key);
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
                            soundsProcessed.Add(key);
                            willBePlayedCount--;
                        }
                    }
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
                // block for immediate messages...
                if (isImmediateMessages)
                {
                    lock (queueToPlay)
                    {
                        openRadioChannelInternal();
                        soundsProcessed.AddRange(playSounds(keysToPlay, isImmediateMessages, out wasInterrupted));
                    }
                }
                else
                {
                    // for queued messages, allow other messages to be inserted into the queue while these are being read
                    openRadioChannelInternal();
                    soundsProcessed.AddRange(playSounds(keysToPlay, isImmediateMessages, out wasInterrupted));
                }
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
            if (queueHasDueMessages(queueToPlay, isImmediateMessages) && !wasInterrupted && !isImmediateMessages)
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
                long milliseconds = DateTime.Now.Ticks / TimeSpan.TicksPerMillisecond;
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
            //Console.WriteLine("Playing sounds, events: " + String.Join(", ", eventNames));
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
                            Thread.Sleep(TimeSpan.FromSeconds(pauseBetweenMessages));
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
                                timeLastPearlOfWisdomPlayed = DateTime.UtcNow;
                                List<SoundPlayer> clipsList = clips[eventName];
                                int index = random.Next(0, clipsList.Count);
                                SoundPlayer clip = clipsList[index];
                                if (!mute)
                                {
                                    clip.PlaySync();
                                }
                            }
                        }
                        else
                        {
                            lastMessagePlayed = thisMessage;
                            foreach (String message in thisMessage.messageFolders)
                            {
                                List<SoundPlayer> clipsList = clips[message];
                                int index = random.Next(0, clipsList.Count);
                                SoundPlayer clip = clipsList[index];
                                if (!mute)
                                {
                                    clip.PlaySync();
                                }
                            }
                            if (playedMessagesCount.ContainsKey(eventName))
                            {
                                int count = playedMessagesCount[eventName] + 1;
                                playedMessagesCount[eventName] = count;
                            }
                            else
                            {
                                playedMessagesCount.Add(eventName, 1);
                            }
                            soundsProcessed.Add(eventName);
                        }
                        playedEventCount++;
                    }
                }
                else
                {
                    Console.WriteLine("we've been interrupted after playing " + playedEventCount + " events");
                    wasInterrupted = true;
                    break;
                }
            }
            if (soundsProcessed.Count == 0)
            {
                Console.WriteLine("Processed no messages in this queue");
                holdChannelOpen = true;
            }
            else
            {
                Console.WriteLine("*** Processed " + String.Join(", ", soundsProcessed.ToArray()));
            }
            return soundsProcessed;
        }

        private void openRadioChannelInternal()
        {
            if (!channelOpen)
            {
                channelOpen = true;
                if (getBackgroundVolume() > 0 && loadNewBackground && backgroundToLoad != null && !mute)
                {
                    Console.WriteLine("Setting background sounds file to  " + backgroundToLoad);
                    String path = Path.Combine(backgroundFilesPath, backgroundToLoad);
                    if (!backgroundPlayerInitialised)
                    {
                        initialiseBackgroundPlayer();
                    }
                    backgroundPlayer.Volume = getBackgroundVolume();
                    backgroundPlayer.Open(new System.Uri(path, System.UriKind.Absolute));
                    loadNewBackground = false;
                }

                // this looks like we're doing it the wrong way round but there's a short
                // delay playing the event sound, so if we kick off the background before the bleep
                if (getBackgroundVolume() > 0)
                {
                    if (!backgroundPlayerInitialised)
                    {
                        initialiseBackgroundPlayer();
                    }
                    backgroundPlayer.Volume = getBackgroundVolume();
                    int backgroundDuration = 0;
                    int backgroundOffset = 0;
                    if (backgroundPlayer.NaturalDuration.HasTimeSpan)
                    {
                        backgroundDuration = (backgroundPlayer.NaturalDuration.TimeSpan.Minutes * 60) +
                            backgroundPlayer.NaturalDuration.TimeSpan.Seconds;
                        //Console.WriteLine("Duration from file is " + backgroundDuration);
                        backgroundOffset = random.Next(0, backgroundDuration - backgroundLeadout);
                    }
                    //Console.WriteLine("Background offset = " + backgroundOffset);
                    backgroundPlayer.Position = TimeSpan.FromSeconds(backgroundOffset);
                    backgroundPlayer.Play();
                }

                if (enableStartBleep)
                {
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
        }

        private void closeRadioInternalChannel()
        {
            if (channelOpen)
            {
                if (enableEndBleep)
                {
                    playEndSpeakingBeep();
                }
                if (getBackgroundVolume() > 0 && !mute)
                {
                    if (!backgroundPlayerInitialised)
                    {
                        initialiseBackgroundPlayer();
                    }
                    try
                    {
                        backgroundPlayer.Stop();
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to stop background player");
                    }
                }
                channelOpen = false;
            }
            useShortBeepWhenOpeningChannel = false;
        }

        public void playStartSpeakingBeep()
        {
            List<SoundPlayer> bleeps = clips["start_bleep"];
            int bleepIndex = random.Next(0, bleeps.Count);
            //Console.WriteLine("*** Opening channel, using bleep start_bleep at position " + bleepIndex);
            if (!mute)
            {
                bleeps[bleepIndex].PlaySync();
            }
        }

        public void playStartListeningBeep()
        {
            if (useListenBeep && clips.ContainsKey("listen_start_sound"))
            {
                List<SoundPlayer> bleeps = clips["listen_start_sound"];
                int bleepIndex = random.Next(0, bleeps.Count);
                Console.WriteLine("*** Listening, using sound listen_start_sound at position " + bleepIndex);
                if (!mute)
                {
                    bleeps[bleepIndex].Play();
                }
            }
        }

        public void playShortStartSpeakingBeep()
        {
            List<SoundPlayer> bleeps = clips["short_start_bleep"];
            int bleepIndex = random.Next(0, bleeps.Count);
            // Console.WriteLine("*** Opening channel, using bleep short_bleep at position " + bleepIndex);
            if (!mute)
            {
                bleeps[bleepIndex].Play();
            }
        }

        public void playEndSpeakingBeep()
        {
            List<SoundPlayer> bleeps = clips["end_bleep"];
            int bleepIndex = random.Next(0, bleeps.Count);
            // Console.WriteLine("*** Closing channel, using bleep end_bleep at position " + bleepIndex);
            if (!mute)
            {
                bleeps[bleepIndex].PlaySync();
            }
        }

        public void purgeQueues()
        {
            foreach (KeyValuePair<string, List<SoundPlayer>> entry in clips)
            {
                foreach (SoundPlayer clip in entry.Value)
                {
                    clip.Stop();
                }
            }
            lock (queuedClips)
            {
                ArrayList keysToPurge = new ArrayList(queuedClips.Keys);
                foreach (String keyStr in keysToPurge)
                {
                    if (!keyStr.Contains(SessionEndMessages.sessionEndMessageIdentifier))
                    {
                        queuedClips.Remove(keyStr);
                    }
                    else
                    {
                        Console.WriteLine("Not purging session end message");
                    }
                }
            }
            lock (immediateClips)
            {
                immediateClips.Clear();
            }
        }

        private void openChannel(Boolean useShortBeep)
        {
            useShortBeepWhenOpeningChannel = useShortBeep;
            requestChannelOpen = true;
        }

        private void holdOpenChannel(Boolean useShortBeep)
        {
            useShortBeepWhenOpeningChannel = useShortBeep;
            requestChannelOpen = true;
            holdChannelOpen = true;
            requestChannelClose = false;
        }

        public void closeChannel()
        {
            requestChannelClose = true;
        }

        public Boolean isChannelOpen()
        {
            return channelOpen;
        }

        public void queueClip(QueuedMessage queuedMessage)
        {
            queueClip(queuedMessage, PearlsOfWisdom.PearlType.NONE, 0);
        }

        public void playClipImmediately(QueuedMessage queuedMessage, Boolean useShortBeep)
        {
            playClipImmediately(queuedMessage, false, useShortBeep);
        }

        public void playClipImmediately(QueuedMessage queuedMessage, Boolean keepChannelOpen, Boolean useShortBeep)
        {
            if (disableImmediateMessages)
            {
                return;
            }
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
                        if (keepChannelOpen)
                        {
                            holdOpenChannel(useShortBeep);
                        }
                        else
                        {
                            openChannel(useShortBeep);
                        }
                        immediateClips.Add(queuedMessage.messageName, queuedMessage);
                    }
                }
            }
        }

        public void queueClip(QueuedMessage queuedMessage, PearlsOfWisdom.PearlType pearlType, double pearlMessageProbability)
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
                        PearlsOfWisdom.PearlMessagePosition pearlPosition = PearlsOfWisdom.PearlMessagePosition.NONE;
                        if (pearlType != PearlsOfWisdom.PearlType.NONE && checkPearlOfWisdomValid(pearlType))
                        {
                            pearlPosition = pearlsOfWisdom.getMessagePosition(pearlMessageProbability);
                        }
                        if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.BEFORE)
                        {
                            QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                            pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                            queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                        }
                        queuedClips.Add(queuedMessage.messageName, queuedMessage);
                        if (pearlPosition == PearlsOfWisdom.PearlMessagePosition.AFTER)
                        {
                            QueuedMessage pearlQueuedMessage = new QueuedMessage(queuedMessage.abstractEvent);
                            pearlQueuedMessage.dueTime = queuedMessage.dueTime;
                            queuedClips.Add(PearlsOfWisdom.getMessageFolder(pearlType), pearlQueuedMessage);
                        }
                    }
                }
            }
        }

        public Boolean removeQueuedClip(String eventName)
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

        public Boolean removeImmediateClip(String eventName)
        {
            if (disableImmediateMessages)
            {
                return false;
            }
            lock (immediateClips)
            {
                if (immediateClips.Contains(eventName))
                {
                    immediateClips.Remove(eventName);
                    return true;
                }
                return false;
            }
        }

        private void openAndCacheClip(String eventName, String file)
        {
            SoundPlayer clip = new SoundPlayer(file);
            clip.Load();
            if (!clips.ContainsKey(eventName))
            {
                clips.Add(eventName, new List<SoundPlayer>());
                if (!allMessageNames.Contains(eventName))
                {
                    allMessageNames.Add(eventName);
                }
            }
            clips[eventName].Add(clip);
            // Console.WriteLine("cached clip " + file + " into set " + eventName);
        }

        private void backgroundPlayer_MediaEnded(object sender, EventArgs e)
        {
            Console.WriteLine("looping...");
            backgroundPlayer.Position = TimeSpan.FromMilliseconds(1);
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
            return timeLastPearlOfWisdomPlayed.Add(minTimeBetweenPearlsOfWisdom) > DateTime.UtcNow;
        }

        public void suspendPearlsOfWisdom()
        {
            allowPearlsOnNextPlay = false;
        }

        public void repeatLastMessage()
        {
            if (lastMessagePlayed != null)
            {
                playClipImmediately(lastMessagePlayed, false);
                closeChannel();
            }
        }
    }
}
