using System.Speech.Synthesis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using CrewChiefV4.GameState;

namespace CrewChiefV4.Audio
{
    public class SoundCache
    {
        public static String TTS_IDENTIFIER = "TTS_IDENTIFIER";
        private Boolean useAlternateBeeps = UserSettings.GetUserSettings().getBoolean("use_alternate_beeps");
        public static Boolean recordVarietyData = UserSettings.GetUserSettings().getBoolean("record_sound_variety_data");
        public static Boolean dumpListOfUnvocalizedNames = UserSettings.GetUserSettings().getBoolean("save_list_of_unvocalized_names");
        private double minSecondsBetweenPersonalisedMessages = (double)UserSettings.GetUserSettings().getInt("min_time_between_personalised_messages");
        public static Boolean eagerLoadSoundFiles = UserSettings.GetUserSettings().getBoolean("load_sound_files_on_startup");
        public static float ttsVolumeBoost = UserSettings.GetUserSettings().getFloat("tts_volume_boost");
        public static float spotterVolumeBoost = UserSettings.GetUserSettings().getFloat("spotter_volume_boost");
        public static int ttsTrimStartMilliseconds = UserSettings.GetUserSettings().getInt("tts_trim_start_milliseconds");
        public static int ttsTrimEndMilliseconds = UserSettings.GetUserSettings().getInt("tts_trim_end_milliseconds");
               
        private static LinkedList<String> dynamicLoadedSounds = new LinkedList<String>();
        public static Dictionary<String, SoundSet> soundSets = new Dictionary<String, SoundSet>();
        private static Dictionary<String, SingleSound> singleSounds = new Dictionary<String, SingleSound>();
        public static HashSet<String> availableDriverNames = new HashSet<String>();
        public static HashSet<String> availableSounds = new HashSet<String>();
        public static HashSet<String> availablePrefixesAndSuffixes = new HashSet<String>();
        private Boolean useSwearyMessages;
        private static Boolean allowCaching;
        private String[] eventTypesToKeepCached;
        private int maxSoundPlayerCacheSize = 500;
        private int soundPlayerPurgeBlockSize = 100;
        public static int currentSoundsLoaded;
        public static int activeSoundPlayerObjects;
        public static int prefixesAndSuffixesCount = 0;

        private Boolean purging = false;
        private Thread expireCachedSoundsThread = null;
        public static String OPTIONAL_PREFIX_IDENTIFIER = "op_prefix";
        public static String OPTIONAL_SUFFIX_IDENTIFIER = "op_suffix";
        public static String REQUIRED_PREFIX_IDENTIFIER = "rq_prefix";
        public static String REQUIRED_SUFFIX_IDENTIFIER = "rq_suffix";

        private DateTime lastPersonalisedMessageTime = DateTime.MinValue;

        public static DateTime lastSwearyMessageTime = DateTime.MinValue;

        public static SpeechSynthesizer synthesizer;

        public static Boolean hasSuitableTTSVoice = false;

        public static Boolean cancelLazyLoading = false;

        private static Dictionary<String, Tuple<int, int>> varietyData = new Dictionary<string, Tuple<int, int>>();

        private static void loadExistingVarietyData()
        {
            if (SoundCache.recordVarietyData)
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "sounds-variety-data.txt");
                StringBuilder fileString = new StringBuilder();
                StreamReader file = null;
                try
                {
                    file = new StreamReader(path);
                    String line;
                    while ((line = file.ReadLine()) != null)
                    {
                        if (!line.Trim().StartsWith("#"))
                        {
                            // split the line. Sound path, files count, played count, variety score
                            String[] lineData = line.Split(',');
                            varietyData[lineData[0]] = new Tuple<int, int>(int.Parse(lineData[1]), int.Parse(lineData[2]));
                        }
                    }
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error reading file " + path + ": " + e.Message);
                }
                finally
                {
                    if (file != null)
                    {
                        file.Close();
                    }
                }
            }
        }

        public static void saveVarietyData()
        {
            if (SoundCache.recordVarietyData)
            {
                string path = System.IO.Path.Combine(Environment.GetFolderPath(
                    Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "sounds-variety-data.txt");
                StringBuilder fileString = new StringBuilder();
                TextWriter tw = new StreamWriter(path, false);
                List<SoundVarietyDataPoint> data = new List<SoundVarietyDataPoint>();
                foreach (KeyValuePair<String, Tuple<int, int>> entry in varietyData)
                {
                    data.Add(new SoundVarietyDataPoint(entry.Key, entry.Value.Item1, entry.Value.Item2));
                }
                data.Sort();
                foreach (SoundVarietyDataPoint dataPoint in data)
                {
                    tw.WriteLine(dataPoint.soundName + "," + dataPoint.numSounds + "," + dataPoint.timesPlayed + "," + dataPoint.score);
                }
                tw.Close();
            }
        }

        public static void addUseToVarietyData(String soundPath, int soundsInThisSet)
        {
            // want the last 4 folders from the full sound path:
            String[] pathFragments = soundPath.Split('\\');
            if (pathFragments.Length > 3)
            {
                String interestingSoundPath = pathFragments[pathFragments.Length - 4] + "/" + pathFragments[pathFragments.Length - 3] + 
                    "/" + pathFragments[pathFragments.Length - 2] + "/" + pathFragments[pathFragments.Length - 1];
                if (varietyData.ContainsKey(interestingSoundPath))
                {
                    varietyData[interestingSoundPath] = new Tuple<int, int>(varietyData[interestingSoundPath].Item1, varietyData[interestingSoundPath].Item2 + 1);
                }
                else
                {
                    varietyData.Add(interestingSoundPath, new Tuple<int, int>(soundsInThisSet, 1));
                }
            }
        }

        public SoundCache(DirectoryInfo soundsFolder, DirectoryInfo sharedSoundsFolder, String[] eventTypesToKeepCached, Boolean useSwearyMessages, Boolean allowCaching, String selectedPersonalisation)
        {
            loadExistingVarietyData();
            // ensure the static state is nuked before we start updating it
            SoundCache.dynamicLoadedSounds.Clear();
            SoundCache.soundSets.Clear();
            SoundCache.singleSounds.Clear();
            SoundCache.availableDriverNames.Clear();
            SoundCache.availableSounds.Clear();
            SoundCache.availablePrefixesAndSuffixes.Clear();
            if (AudioPlayer.ttsOption != AudioPlayer.TTS_OPTION.NEVER)
            {
                try
                {
                    if (synthesizer != null)
                    {
                        try
                        {
                            synthesizer.Dispose();
                            synthesizer = null;
                        }
                        catch (Exception) { }
                    }
                    synthesizer = new SpeechSynthesizer();
                    Boolean hasMale = false;
                    Boolean hasAdult = false;
                    Boolean hasSenior = false;
                    foreach (InstalledVoice voice in synthesizer.GetInstalledVoices())
                    {
                        if (voice.VoiceInfo.Age == VoiceAge.Adult)
                        {
                            hasAdult = true;
                        }
                        if (voice.VoiceInfo.Age == VoiceAge.Senior)
                        {
                            hasSenior = true;
                        }
                        if (voice.VoiceInfo.Gender == VoiceGender.Male)
                        {
                            hasMale = true;
                        }
                    }
                    if (hasMale && (hasAdult || hasSenior))
                    {
                        hasSuitableTTSVoice = true;
                    }
                    else
                    {
                        Console.WriteLine("No suitable TTS voice pack found - TTS will only be used in response to voice commands (and will probably sound awful). " +
                            "US versions of Windows 8.1 and Windows 10 should be able to use Microsoft's 'David' voice - " +
                            "this can be selected in the Control Panel");
                        hasSuitableTTSVoice = false;
                        if (synthesizer.GetInstalledVoices().Count == 1)
                        {
                            Console.WriteLine("Defaulting to voice " + synthesizer.GetInstalledVoices()[0].VoiceInfo.Name);
                        }
                    }

                    // this appears to just hang indefinitely. So don't bother trying to set it and let the system use the default voice
                    // which will probably be shit, but MS TTS is shit anyway and now it's even shitter because it crashes the fucking
                    // app on start up. Nobbers.
                    // synthesizer.SelectVoiceByHints(VoiceGender.Male, hasAdult ? VoiceAge.Adult : VoiceAge.Senior);
                    synthesizer.Volume = 100;
                    synthesizer.Rate = 0;
                }
                catch (Exception) {
                    Console.WriteLine("Unable to initialise the TTS engine, TTS will not be available. " +
                                "Check a suitable Microsoft TTS voice pack is installed");
                    AudioPlayer.ttsOption = AudioPlayer.TTS_OPTION.NEVER;
                }
            }
            SoundCache.currentSoundsLoaded = 0;
            SoundCache.activeSoundPlayerObjects = 0;
            this.eventTypesToKeepCached = eventTypesToKeepCached;
            this.useSwearyMessages = useSwearyMessages;
            SoundCache.allowCaching = allowCaching;
            DirectoryInfo[] sharedSoundsFolders = sharedSoundsFolder.GetDirectories();
            foreach (DirectoryInfo soundFolder in sharedSoundsFolders)
            {
                if (soundFolder.Name == "fx")
                {
                    // these are eagerly loaded on the main thread, soundPlayers are created and they're always in the SoundPlayer cache.
                    prepareFX(soundFolder);
                }
            }
            DirectoryInfo[] soundsFolders = soundsFolder.GetDirectories();
            foreach (DirectoryInfo soundFolder in soundsFolders)
            {
                if (soundFolder.Name == "personalisations")
                {
                    if (selectedPersonalisation != AudioPlayer.NO_PERSONALISATION_SELECTED)
                    {
                        // these are eagerly loaded on the main thread, soundPlayers are created and they're always in the SoundPlayer cache.
                        // If the number of prefixes and suffixes keeps growing this will need to be moved to a background thread but take care
                        // to ensure the objects which hold the sounds are all created on the main thread, with only the file reading and 
                        // SoundPlayer creation part done in the background (just like we do for voice messages).
                        preparePrefixesAndSuffixes(soundFolder, selectedPersonalisation);
                    }
                    else
                    {
                        Console.WriteLine("No name has been selected for personalised messages");
                    }
                }
                else if (soundFolder.Name == "voice")
                {
                    // these are eagerly loaded on a background Thread. For frequently played sounds we create soundPlayers 
                    // and hold them in the cache. For most sounds we just load the file(s) and create sound players when needed,
                    // allowing them to be cached until evicted.

                    // this creates empty sound objects:
                    prepareVoiceWithoutLoading(soundFolder, new DirectoryInfo(sharedSoundsFolder.FullName + "/voice"));
                    // now spawn a Thread to load the sound files (and in some cases soundPlayers) in the background:
                    if (allowCaching && eagerLoadSoundFiles)
                    {
                        var cacheSoundsThread = new Thread(() =>
                        {
                            DateTime start = DateTime.UtcNow;
                            Thread.CurrentThread.IsBackground = true;
                            // load the permanently cached sounds first, then the rest
                            foreach (SoundSet soundSet in soundSets.Values)
                            {
                                if (SoundCache.cancelLazyLoading)
                                {
                                    break;
                                }
                                else if (soundSet.cacheSoundPlayersPermanently)
                                {
                                    soundSet.loadAll();
                                }
                            }
                            foreach (SoundSet soundSet in soundSets.Values)
                            {
                                if (SoundCache.cancelLazyLoading)
                                {
                                    break;
                                }
                                else if (!soundSet.cacheSoundPlayersPermanently)
                                {
                                    soundSet.loadAll();
                                }
                            }
                            SoundCache.cancelLazyLoading = false;
                            if (AudioPlayer.playWithNAudio)
                            {
                                Console.WriteLine("Took " + (DateTime.UtcNow - start).TotalSeconds.ToString("0.00") + "s to lazy load remaining message sounds, there are now " +
                                    SoundCache.currentSoundsLoaded + " loaded message sounds");
                            }
                            else
                            {
                                Console.WriteLine("Took " + (DateTime.UtcNow - start).TotalSeconds.ToString("0.00") + "s to lazy load remaining message sounds, there are now " +
                                    SoundCache.currentSoundsLoaded + " loaded message sounds with " + SoundCache.activeSoundPlayerObjects + " active SoundPlayer objects");
                            }
                        });
                        cacheSoundsThread.Name = "SoundCache.cacheSoundsThread";
                        ThreadManager.RegisterResourceThread(cacheSoundsThread);
                        cacheSoundsThread.Start();
                    }
                }
                else if (soundFolder.Name == "driver_names")
                {
                    // The folder of driver names is processed on the main thread and objects are created to hold the sounds, 
                    // but the sound files are lazy-loaded on session start, along with the corresponding SoundPlayer objects.
                    prepareDriverNamesWithoutLoading(soundFolder);
                }
            }
            if (AudioPlayer.playWithNAudio)
            {
                Console.WriteLine("Finished preparing sounds cache, found " + singleSounds.Count + " driver names and " + soundSets.Count +
                    " sound sets. Loaded " + SoundCache.currentSoundsLoaded + " message sounds");
            }
            else
            {
                Console.WriteLine("Finished preparing sounds cache, found " + singleSounds.Count + " driver names and " + soundSets.Count +
                    " sound sets. Loaded " + SoundCache.currentSoundsLoaded + " message sounds with " + SoundCache.activeSoundPlayerObjects + " active SoundPlayer objects");
            }
            
            if (prefixesAndSuffixesCount > 0)
            {
                Console.WriteLine(prefixesAndSuffixesCount + " sounds have personalisations");
            }
        }

        public static void loadSingleSound(String soundName, String fullPath)
        {
            if (!singleSounds.ContainsKey(soundName))
            {
                SingleSound singleSound = new SingleSound(fullPath, true, true, false);
                singleSounds.Add(soundName, singleSound);
            }
        }

        public static Boolean hasSingleSound(String soundName)
        {
            return availableDriverNames.Contains(soundName) || singleSounds.ContainsKey(soundName);
        }

        public static void loadDriverNameSounds(List<String> names)
        {
            var loadDriverNameSoundsThread = new Thread(() =>
            {
                int loadedCount = 0;
                DateTime start = DateTime.UtcNow;
                // No need to early terminate this thread on form close, because it only loads driver names in 
                // a session, which isn't 1000's.
                foreach (String name in names)
                {
                    loadedCount++;
                    loadDriverNameSound(name);
                }
                if (AudioPlayer.playWithNAudio)
                {
                    Console.WriteLine("Took " + (DateTime.UtcNow - start).TotalSeconds.ToString("0.00") + " seconds to load " +
                        loadedCount + " driver name sounds. There are now " + SoundCache.currentSoundsLoaded + " sound files loaded");
                }
                else
                {
                    Console.WriteLine("Took " + (DateTime.UtcNow - start).TotalSeconds.ToString("0.00") + " seconds to load " +
                        loadedCount + " driver name sounds. There are now " + SoundCache.currentSoundsLoaded +
                        " sound files loaded with " + SoundCache.activeSoundPlayerObjects + " active SoundPlayer objects");
                }
            });
            loadDriverNameSoundsThread.Name = "SoundCache.loadDriverNameSoundsThread";
            ThreadManager.RegisterResourceThread(loadDriverNameSoundsThread);
            loadDriverNameSoundsThread.Start();
        }

        public static void loadDriverNameSound(String name)
        {
            Boolean isInAvailableNames = availableDriverNames.Contains(name);
            if (dumpListOfUnvocalizedNames && !isInAvailableNames)
            {
                DriverNameHelper.unvocalizedNames.Add(name);
            }
            // if the name is in the availableDriverNames array then we have a sound file for it, so we can load it
            if (!allowCaching)
            {
                return;
            }
            if (isInAvailableNames)
            {
                singleSounds[name].LoadAndCacheSound();
                lock (SoundCache.dynamicLoadedSounds)
                {
                    SoundCache.dynamicLoadedSounds.Remove(name);
                    SoundCache.dynamicLoadedSounds.AddLast(name);
                }
            }
            else
            {
                Console.WriteLine("Unvocalized driver name: " + name);
            }
        }

        public Boolean eventHasPersonalisedPrefixOrSuffix(String eventName)
        {
            SoundSet ss = null;
            if (soundSets.TryGetValue(eventName, out ss))
            {
                return ss.hasPrefixOrSuffix;
            }
            return false;
        }
        
        public Boolean personalisedMessageIsDue()
        {
            double secondsSinceLastPersonalisedMessage = (GameStateData.CurrentTime - lastPersonalisedMessageTime).TotalSeconds;
            Boolean due = false;
            if (minSecondsBetweenPersonalisedMessages == 0)
            {
                due = false;
            }
            else if (secondsSinceLastPersonalisedMessage > minSecondsBetweenPersonalisedMessages)
            {
                // we can now select a personalised message, but we don't always do this - the probability is based 
                // on the time since the last one
                due = Utilities.random.NextDouble() < 1.2 - minSecondsBetweenPersonalisedMessages / secondsSinceLastPersonalisedMessage;
            }
            return due;
        }

        private void moveToTopOfCache(String soundName)
        {
            if (!AudioPlayer.playWithNAudio)
            {
                lock (SoundCache.dynamicLoadedSounds)
                {
                    SoundCache.dynamicLoadedSounds.Remove(soundName);
                    SoundCache.dynamicLoadedSounds.AddLast(soundName);
                }
            }
        }

        /*
         * canInterrupt will be true for regular messages triggered by the app's normal event logic. When a message
         * is played from the 'immediate' queue this will be false (spotter calls, command responses, some edge cases 
         * where the message is time-critical). If this flag is true the presence of a message in the immediate queue
         * can make the app skip playing this sound.
         */
        public void Play(List<String> soundNames, SoundMetadata soundMetadata)
        {           
            SoundSet prefix = null;
            SoundSet suffix = null;
            List<SingleSound> singleSoundsToPlay = new List<SingleSound>();      
            foreach (String soundName in soundNames)
            {
                if (soundName.StartsWith(AudioPlayer.PAUSE_ID))
                {
                    int pauseLength = 500;
                    try
                    {
                        String[] split = soundName.Split(':');
                        if (split.Count() == 2)
                        {
                            pauseLength = int.Parse(split[1]);
                        }
                    }
                    catch (Exception) { }
                    singleSoundsToPlay.Add(new SingleSound(pauseLength));
                }
                else if (soundName.StartsWith(TTS_IDENTIFIER))
                {
                    SingleSound singleSound = null;
                    if (!singleSounds.TryGetValue(soundName, out singleSound))
                    {
                        singleSound = new SingleSound(soundName.Substring(TTS_IDENTIFIER.Count()));
                        singleSounds.Add(soundName, singleSound);
                    }
                    moveToTopOfCache(soundName);
                    singleSoundsToPlay.Add(singleSound);
                }
                else
                {
                    Boolean preferPersonalised = personalisedMessageIsDue();
                    SingleSound singleSound = null;
                    SoundSet soundSet = null;
                    if (soundSets.TryGetValue(soundName, out soundSet))
                    {
                        // double check whether this soundSet wants to allow personalisations at this point - 
                        // this prevents the app always choosing the personalised version of a sound if this sound is infrequent
                        if (soundSet.forceNonPersonalisedVersion())
                        {
                            preferPersonalised = false;
                        }
                        singleSound = soundSet.getSingleSound(preferPersonalised);
                        if (!soundSet.cacheSoundPlayersPermanently)
                        {
                            moveToTopOfCache(soundName);
                        }
                    }
                    else if (singleSounds.TryGetValue(soundName, out singleSound))
                    {
                        if (!singleSound.cacheSoundPlayerPermanently)
                        {
                            moveToTopOfCache(soundName);
                        }
                    }
                    if (singleSound != null)
                    {
                        // hack... we double check the prefer setting here and only play the prefix / suffix if it's true.
                        // The list without prefixes and suffixes includes items which have optional ones, so we might want to
                        // play a sound that can have the prefix / suffix, but not the associated prefix / suffix
                        if (preferPersonalised && singleSound.prefixSoundSet != null)
                        {
                            prefix = singleSound.prefixSoundSet;
                        }
                        if (preferPersonalised && singleSound.suffixSoundSet != null)
                        {
                            suffix = singleSound.suffixSoundSet;
                        }
                        singleSoundsToPlay.Add(singleSound);
                    }
                }
            }
            if (singleSounds.Count > 0)
            {
                if (prefix != null)
                {
                    singleSoundsToPlay.Insert(0, prefix.getSingleSound(false));
                    lastPersonalisedMessageTime = GameStateData.CurrentTime;
                }
                if (suffix != null)
                {
                    singleSoundsToPlay.Add(suffix.getSingleSound(false));
                    lastPersonalisedMessageTime = GameStateData.CurrentTime;
                }
                foreach (SingleSound singleSound in singleSoundsToPlay)
                {
                    if (singleSound.isPause)
                    {
                        Thread.Sleep(singleSound.pauseLength);
                    }
                    else
                    {
                        singleSound.Play(soundMetadata);
                    }
                }
            }
        }

        /*
         * canInterrupt will be true for regular messages triggered by the app's normal event logic. When a message
         * is played from the 'immediate' queue this will be false (spotter calls, command responses, some edge cases 
         * where the message is time-critical). If this flag is true the presence of a message in the immediate queue
         * can make the app skip playing this sound.
         */
        public void Play(String soundName, SoundMetadata soundMetadata)
        {
            List<String> l = new List<String>();
            l.Add(soundName);
            Play(l, soundMetadata);
        }

        public void ExpireCachedSounds()
        {
            if (AudioPlayer.playWithNAudio)
            {
                return;
            }
            if (!purging && SoundCache.activeSoundPlayerObjects > maxSoundPlayerCacheSize)
            {
                purging = true;
                ThreadManager.UnregisterTemporaryThread(expireCachedSoundsThread);
                expireCachedSoundsThread = new Thread(() =>
                {
                    Thread.CurrentThread.IsBackground = true;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    int purgeCount = 0;
                    LinkedListNode<String> soundToPurge;
                    lock (SoundCache.dynamicLoadedSounds)
                    {
                        soundToPurge = SoundCache.dynamicLoadedSounds.First;
                    }
                    // No need to support cancellation of this thread, as it is not slow enough and we can wait for it.
                    while (soundToPurge != null && purgeCount <= soundPlayerPurgeBlockSize)
                    {
                        String soundToPurgeValue = soundToPurge.Value;
                        SoundSet soundSet = null;
                        SingleSound singleSound = null;
                        if (soundSets.TryGetValue(soundToPurgeValue, out soundSet))
                        {
                            purgeCount += soundSet.UnLoadAll();
                        }
                        else if (singleSounds.TryGetValue(soundToPurgeValue, out singleSound))
                        {
                            if (singleSound.UnLoad())
                            {
                                purgeCount++;
                            }
                        }
                        lock (SoundCache.dynamicLoadedSounds)
                        {
                            var nextSoundToPurge = soundToPurge.Next;
                            SoundCache.dynamicLoadedSounds.Remove(soundToPurge);
                            soundToPurge = nextSoundToPurge;
                        }
                    }
                    watch.Stop();
                    var elapsedMs = watch.ElapsedMilliseconds;
                    Console.WriteLine("Purged " + purgeCount + " sounds in " + elapsedMs + "ms, there are now " + SoundCache.activeSoundPlayerObjects + " active SoundPlayer objects");
                    purging = false;
                });
                expireCachedSoundsThread.Name = "SoundCache.expireCachedSoundsThread";
                ThreadManager.RegisterTemporaryThread(expireCachedSoundsThread);
                expireCachedSoundsThread.Start();
            }
        }

        public void StopAndUnloadAll()
        {
            if (synthesizer != null)
            {
                try
                {
                    synthesizer.Dispose();
                    synthesizer = null;
                }
                catch (Exception) { }
            }
            foreach (SoundSet soundSet in soundSets.Values)
            {
                try
                {
                    soundSet.StopAll();
                    soundSet.UnLoadAll();
                }
                catch (Exception) { }
            }
            foreach (SingleSound singleSound in singleSounds.Values)
            {
                try
                {
                    singleSound.Stop();
                    singleSound.UnLoad();
                }
                catch (Exception) { }
            }
        }

        public void StopAll()
        {
            foreach (SoundSet soundSet in soundSets.Values)
            {
                try
                {
                    soundSet.StopAll();
                }
                catch (Exception) { }
            }
            foreach (SingleSound singleSound in singleSounds.Values)
            {
                try { 
                    singleSound.Stop();
                }
                catch (Exception) { }
            }
        }

        private void prepareFX(DirectoryInfo fxSoundDirectory) {
            FileInfo[] bleepFiles = fxSoundDirectory.GetFiles();
            String alternate_prefix = useAlternateBeeps ? "alternate_" : "";
            String opposite_prefix = !useAlternateBeeps ? "alternate_" : "";
            Console.WriteLine("Preparing sound effects");
            foreach (FileInfo bleepFile in bleepFiles)
            {
                if (bleepFile.Name.EndsWith(".wav"))
                {
                    if (bleepFile.Name.StartsWith(alternate_prefix + "start") && !singleSounds.ContainsKey("start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("start_bleep", sound);
                        availableSounds.Add("start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "end") && !singleSounds.ContainsKey("end_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("end_bleep", sound);
                        availableSounds.Add("end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "short_start") && !singleSounds.ContainsKey("short_start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("short_start_bleep", sound);
                        availableSounds.Add("short_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith("listen_start") && !singleSounds.ContainsKey("listen_start_sound"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("listen_start_sound", sound);
                        availableSounds.Add("listen_start_sound");
                    }
                    else if (bleepFile.Name.StartsWith(opposite_prefix + "start") && !singleSounds.ContainsKey("alternate_start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("alternate_start_bleep", sound);
                        availableSounds.Add("alternate_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(opposite_prefix + "end") && !singleSounds.ContainsKey("alternate_end_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("alternate_end_bleep", sound);
                        availableSounds.Add("alternate_end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(opposite_prefix + "short_start") && !singleSounds.ContainsKey("alternate_short_start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, true, allowCaching, allowCaching);
                        sound.isBleep = true;
                        if (eagerLoadSoundFiles)
                        {
                            sound.LoadAndCacheFile();
                        }
                        singleSounds.Add("alternate_short_start_bleep", sound);
                        availableSounds.Add("alternate_short_start_bleep");
                    }
                }
            }
            Console.WriteLine("Prepare sound effects completed");
        }

        private void loadVoices()
        {
                       
        }

        private void prepareVoiceWithoutLoading(DirectoryInfo voiceDirectory, DirectoryInfo sharedVoiceDirectory)
        {
            Console.WriteLine("Preparing voice messages");

            DirectoryInfo[] eventFolders = null;
            if (!String.Equals(voiceDirectory.FullName, sharedVoiceDirectory.FullName, StringComparison.InvariantCultureIgnoreCase))
            {
                // Get shared voice directories (spotter sounds).
                DirectoryInfo[] spotterFolders = sharedVoiceDirectory.GetDirectories("spotter*");
                DirectoryInfo[] radioCheckFolders = sharedVoiceDirectory.GetDirectories("radio_check*");

                // Get redirected voice directories.
                DirectoryInfo[] voiceFolders = voiceDirectory.GetDirectories();

                eventFolders = new DirectoryInfo[spotterFolders.Length + radioCheckFolders.Length + voiceFolders.Length];
                spotterFolders.CopyTo(eventFolders, 0);
                radioCheckFolders.CopyTo(eventFolders, spotterFolders.Length);
                voiceFolders.CopyTo(eventFolders, spotterFolders.Length + radioCheckFolders.Length);
            }
            else
            {
                eventFolders = voiceDirectory.GetDirectories();
            }
            foreach (DirectoryInfo eventFolder in eventFolders)
            {
                Boolean cachePermanently = allowCaching && this.eventTypesToKeepCached.Contains(eventFolder.Name);
                try
                {
                    DirectoryInfo[] eventDetailFolders = eventFolder.GetDirectories();
                    foreach (DirectoryInfo eventDetailFolder in eventDetailFolders)
                    {
                        String fullEventName = eventFolder.Name + "/" + eventDetailFolder.Name;
                        // if we're caching this sound set permanently, create the sound players immediately after the files are loaded
                        SoundSet soundSet = new SoundSet(eventDetailFolder, this.useSwearyMessages, allowCaching, allowCaching, cachePermanently, cachePermanently);                        
                        if (soundSet.hasSounds)
                        {
                            if (soundSets.ContainsKey(fullEventName))
                            {
                                Console.WriteLine("Event " + fullEventName + " sound set is already loaded");
                            }
                            else 
                            {
                                availableSounds.Add(fullEventName);
                                soundSets.Add(fullEventName, soundSet);
                            }
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine("Unable to find events folder");
                }
            }
            Console.WriteLine("Prepare voice message completed");
        }

        private void prepareDriverNamesWithoutLoading(DirectoryInfo driverNamesDirectory)
        {
            Console.WriteLine("Preparing driver names");
            FileInfo[] driverNameFiles = driverNamesDirectory.GetFiles();
            foreach (FileInfo driverNameFile in driverNameFiles)
            {
                if (driverNameFile.Name.EndsWith(".wav"))
                {                    
                    String name = driverNameFile.Name.ToLower().Split(new[] { ".wav" }, StringSplitOptions.None)[0];
                    singleSounds.Add(name, new SingleSound(driverNameFile.FullName, allowCaching, allowCaching, false));
                    availableDriverNames.Add(name);
                }
            }
            Console.WriteLine("Prepare driver names completed");
        }

        private void preparePrefixesAndSuffixes(DirectoryInfo personalisationsDirectory, String selectedPersonalisation)
        {
            Console.WriteLine("Preparing personalisations for selected name " + selectedPersonalisation);
            DirectoryInfo[] namesFolders = personalisationsDirectory.GetDirectories();
            foreach (DirectoryInfo namesFolder in namesFolders)
            {
                if (namesFolder.Name.Equals(selectedPersonalisation, StringComparison.InvariantCultureIgnoreCase))
                {
                    DirectoryInfo[] nameSubfolders = namesFolder.GetDirectories();
                    foreach (DirectoryInfo nameSubfolder in nameSubfolders)
                    {
                        if (nameSubfolder.Name.Equals("prefixes_and_suffixes"))
                        {
                            foreach (DirectoryInfo prefixesAndSuffixesFolder in nameSubfolder.GetDirectories())
                            {
                                // always keep the personalisations cached as they're reused frequently, so create the sound players immediately after the files are loaded
                                SoundSet soundSet = new SoundSet(prefixesAndSuffixesFolder, this.useSwearyMessages, allowCaching, allowCaching, true, true);
                                if (soundSet.hasSounds)
                                {
                                    availablePrefixesAndSuffixes.Add(prefixesAndSuffixesFolder.Name);
                                    soundSets.Add(prefixesAndSuffixesFolder.Name, soundSet);
                                }
                            }
                            break;
                        }
                    }
                    break;
                }
            }
            Console.WriteLine("Prepare personalisations completed");
        }
    }

    public class SoundSet
    {
        private List<SingleSound> singleSoundsNoPrefixOrSuffix = new List<SingleSound>();
        private List<SingleSound> singleSoundsWithPrefixOrSuffix = new List<SingleSound>();
        private DirectoryInfo soundFolder;
        private Boolean useSwearyMessages;
        public Boolean cacheSoundPlayers;
        public Boolean cacheFileData;
        public Boolean eagerlyCreateSoundPlayers;
        public Boolean cacheSoundPlayersPermanently;
        private Boolean initialised = false;
        public Boolean hasSounds = false;
        public int soundsCount;
        public Boolean hasPrefixOrSuffix = false;
        private List<int> prefixOrSuffixIndexes = null;
        private int prefixOrSuffixIndexesPosition = 0;
        private List<int> indexes = null;
        private int indexesPosition = 0;

        // allow the non-personalised versions of this soundset to play, if it's not frequent and has personalisations
        private Boolean lastVersionWasPersonalised = false;

        public SoundSet(DirectoryInfo soundFolder, Boolean useSwearyMessages, Boolean cacheFileData, Boolean cacheSoundPlayers, 
            Boolean cacheSoundPlayersPermanently, Boolean eagerlyCreateSoundPlayers)
        {
            this.soundsCount = 0;
            this.soundFolder = soundFolder;
            this.useSwearyMessages = useSwearyMessages;
            this.cacheFileData = cacheFileData;
            this.cacheSoundPlayers = cacheSoundPlayers;
            this.eagerlyCreateSoundPlayers = eagerlyCreateSoundPlayers;
            this.cacheSoundPlayersPermanently = cacheSoundPlayersPermanently;
            initialise();
        }

        public Boolean forceNonPersonalisedVersion()
        {
            return lastVersionWasPersonalised && singleSoundsNoPrefixOrSuffix.Count > 0;
        }

        private void shuffle(List<int> list)
        {
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = Utilities.random.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public void loadAll()
        {
            foreach (SingleSound sound in singleSoundsNoPrefixOrSuffix)
            {
                if (eagerlyCreateSoundPlayers)
                {
                    sound.LoadAndCacheSound();
                }
                else
                {
                    sound.LoadAndCacheFile();
                }
            }
            foreach (SingleSound sound in singleSoundsWithPrefixOrSuffix)
            {
                if (eagerlyCreateSoundPlayers)
                {
                    sound.LoadAndCacheSound();
                }
                else
                {
                    sound.LoadAndCacheFile();
                }
            }
        }

        private void initialise()
        {
            try
            {
                FileInfo[] soundFiles = this.soundFolder.GetFiles();
                foreach (FileInfo soundFile in soundFiles)
                {
                    if (soundFile.Name.EndsWith(".wav")) {
                        Boolean isSweary = soundFile.Name.Contains("sweary");
                        Boolean isBleep = soundFile.Name.Contains("bleep");
                        Boolean isSpotter = soundFile.FullName.Contains(@"\spotter");
                        if (!isSpotter && NoisyCartesianCoordinateSpotter.folderSpotterRadioCheckBSlash != null)
                        {
                            isSpotter = soundFile.FullName.Contains(NoisyCartesianCoordinateSpotter.folderSpotterRadioCheckBSlash);
                        }
                        if (this.useSwearyMessages || !isSweary)
                        {
                            if (soundFile.Name.Contains(SoundCache.REQUIRED_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.REQUIRED_SUFFIX_IDENTIFIER) ||
                                soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER))
                            {
                                Boolean isOptional = soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.OPTIONAL_SUFFIX_IDENTIFIER);
                                foreach (String prefixSuffixName in SoundCache.availablePrefixesAndSuffixes)
                                {
                                    SoundSet additionalSoundSet = null;
                                    if (soundFile.Name.Contains(prefixSuffixName) && SoundCache.soundSets.TryGetValue(prefixSuffixName, out additionalSoundSet))
                                    {
                                        if (additionalSoundSet.hasSounds)
                                        {
                                            hasPrefixOrSuffix = true;
                                            hasSounds = true;
                                            SingleSound singleSound = new SingleSound(soundFile.FullName, this.cacheFileData, this.cacheSoundPlayers, this.cacheSoundPlayersPermanently);
                                            if (eagerlyCreateSoundPlayers)
                                            {
                                                singleSound.LoadAndCacheSound();
                                            }
                                            singleSound.isSweary = isSweary;
                                            if (soundFile.Name.Contains(SoundCache.OPTIONAL_SUFFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.REQUIRED_SUFFIX_IDENTIFIER))
                                            {
                                                singleSound.suffixSoundSet = additionalSoundSet;
                                            }
                                            else
                                            {
                                                singleSound.prefixSoundSet = additionalSoundSet;
                                            }
                                            singleSoundsWithPrefixOrSuffix.Add(singleSound);
                                            SoundCache.prefixesAndSuffixesCount++;
                                            soundsCount++;
                                        }
                                        break;
                                    }
                                }
                                if (isOptional)
                                {
                                    hasSounds = true;
                                    SingleSound singleSound = new SingleSound(soundFile.FullName, this.cacheFileData, this.cacheSoundPlayers, this.cacheSoundPlayersPermanently);
                                    if (eagerlyCreateSoundPlayers)
                                    {
                                        singleSound.LoadAndCacheSound();
                                    }
                                    singleSound.isSweary = isSweary;
                                    singleSound.isSpotter = isSpotter;
                                    singleSound.isBleep = isBleep;
                                    singleSoundsNoPrefixOrSuffix.Add(singleSound);
                                    soundsCount++;
                                }
                            }
                            else
                            {
                                hasSounds = true;
                                SingleSound singleSound = new SingleSound(soundFile.FullName, this.cacheFileData, this.cacheSoundPlayers, this.cacheSoundPlayersPermanently);
                                if (eagerlyCreateSoundPlayers)
                                {
                                    singleSound.LoadAndCacheSound();
                                }
                                singleSound.isSweary = isSweary;
                                singleSound.isSpotter = isSpotter;
                                singleSound.isBleep = isBleep;
                                singleSoundsNoPrefixOrSuffix.Add(singleSound);
                                soundsCount++;
                            }
                        }
                    }
                }
            }
            catch (DirectoryNotFoundException)
            {
            }            
            initialised = true;
        }
        
        public SingleSound getSingleSound(Boolean preferPersonalised)
        {
            if (!initialised)
            {
                initialise();
            }
            if (SoundCache.recordVarietyData)
            {
                SoundCache.addUseToVarietyData(this.soundFolder.FullName, this.soundsCount);
            }
            if (!AudioPlayer.rantWaitingToPlay && preferPersonalised && singleSoundsWithPrefixOrSuffix.Count > 0)
            {
                if (prefixOrSuffixIndexes == null || prefixOrSuffixIndexesPosition == prefixOrSuffixIndexes.Count)
                {
                    prefixOrSuffixIndexes = createIndexes(singleSoundsWithPrefixOrSuffix.Count());
                    prefixOrSuffixIndexesPosition = 0;
                }
                SingleSound ss = null;
                while (prefixOrSuffixIndexesPosition < prefixOrSuffixIndexes.Count())
                {
                    ss = singleSoundsWithPrefixOrSuffix[prefixOrSuffixIndexes[prefixOrSuffixIndexesPosition]];
                    prefixOrSuffixIndexesPosition++;
                    if (!ss.isSweary)
                    {
                        break;
                    }
                    else
                    {
                        // this is a sweary message - can we play it? do we have to play it?
                        if (prefixOrSuffixIndexesPosition == prefixOrSuffixIndexes.Count || GameStateData.CurrentTime > SoundCache.lastSwearyMessageTime + TimeSpan.FromSeconds(10))
                        {
                            SoundCache.lastSwearyMessageTime = GameStateData.CurrentTime;
                            break;
                        }
                    }
                }
                lastVersionWasPersonalised = true;
                return ss;
            } 
            else if (singleSoundsNoPrefixOrSuffix.Count > 0)
            {
                if (indexes == null || indexesPosition == indexes.Count)
                {
                    indexes = createIndexes(singleSoundsNoPrefixOrSuffix.Count());
                    indexesPosition = 0;
                }
                SingleSound ss = null;
                while (indexesPosition < indexes.Count())
                {
                    ss = singleSoundsNoPrefixOrSuffix[indexes[indexesPosition]];
                    indexesPosition++;
                    if (!ss.isSweary)
                    {
                        break;
                    }
                    else
                    {
                        // this is a sweary message - can we play it? do we have to play it?
                        if (indexesPosition == indexes.Count || GameStateData.CurrentTime > SoundCache.lastSwearyMessageTime + TimeSpan.FromSeconds(10))
                        {
                            SoundCache.lastSwearyMessageTime = GameStateData.CurrentTime;
                            break;
                        }
                    }
                }
                lastVersionWasPersonalised = false;
                return ss;
            }
            else
            {
                return null;
            }
        }

        private List<int> createIndexes(int count)
        {
            List<int> indexes = new List<int>();
            for (int i = 0; i < count; i++)
            {
                indexes.Add(i);
            }
            shuffle(indexes);
            return indexes;
        }

        public int UnLoadAll()
        {
            int unloadedCount = 0;
            foreach (SingleSound singleSound in singleSoundsNoPrefixOrSuffix)
            {
                if (singleSound.UnLoad())
                {
                    unloadedCount++;
                }
            } 
            foreach (SingleSound singleSound in singleSoundsWithPrefixOrSuffix)
            {
                if (singleSound.UnLoad())
                {
                    unloadedCount++;
                }
            }
            return unloadedCount;
        }

        public void StopAll()
        {
            foreach (SingleSound singleSound in singleSoundsNoPrefixOrSuffix)
            {
                singleSound.Stop();
            } 
            foreach (SingleSound singleSound in singleSoundsWithPrefixOrSuffix)
            {
                singleSound.Stop();
            }
        }
    }

    public class SingleSound
    {
        public String ttsString = null;
        public Boolean isSweary = false;
        public Boolean isPause = false;
        public Boolean isSpotter = false;
        public Boolean isBleep = false;
        public int pauseLength = 0;
        public String fullPath;
        private byte[] fileBytes = null;
        private MemoryStream memoryStream;
        private SoundPlayer soundPlayer;

        public Boolean cacheFileData;
        public Boolean cacheSoundPlayer;
        public Boolean cacheSoundPlayerPermanently;
        private Boolean loadedSoundPlayer = false;
        private Boolean loadedFile = false;

        public SoundSet prefixSoundSet = null;
        public SoundSet suffixSoundSet = null;

        private NAudio.Wave.WaveOut waveOut;
        private NAudio.Wave.WaveFileReader reader;
        // only used for bleeps
        private int deviceIdWhenCached = 0;

        AutoResetEvent playWaitHandle = new AutoResetEvent(false);

        public SingleSound(int pauseLength)
        {
            this.isPause = true;
            this.pauseLength = pauseLength;
        }

        public SingleSound(String textToRender)
        {
            this.ttsString = textToRender;
            // always eagerly load and cache TTS phrases:
            cacheFileData = true;
            cacheSoundPlayer = true;
            LoadAndCacheFile();
        }

        public SingleSound(String fullPath, Boolean cacheFileData, Boolean cacheSoundPlayer, Boolean cacheSoundPlayerPermanently)
        {
            this.fullPath = fullPath;
            this.cacheFileData = cacheFileData || cacheSoundPlayer || cacheSoundPlayerPermanently;
            this.cacheSoundPlayer = cacheSoundPlayer || cacheSoundPlayerPermanently;
            this.cacheSoundPlayerPermanently = cacheSoundPlayerPermanently;
        }

        public void LoadAndCacheFile()
        {
            lock (this)
            {
                if (!loadedFile)
                {
                    try
                    {
                        if (ttsString != null)
                        {
                            MemoryStream rawStream = new MemoryStream();
                            SoundCache.synthesizer.SetOutputToWaveStream(rawStream);
                            SoundCache.synthesizer.Speak(ttsString);
                            rawStream.Position = 0;
                            try
                            {
                                this.fileBytes = ConvertTTSWaveStreamToBytes(rawStream, SoundCache.ttsTrimStartMilliseconds, SoundCache.ttsTrimEndMilliseconds);
                            }
                            catch (Exception e)
                            {
                                // unable to trim and convert the tts stream, so save the raw stream and use that instead
                                Console.WriteLine("Failed to pre-process TTS audio data: " + e.StackTrace);
                                this.memoryStream = rawStream;
                            }
                            SoundCache.synthesizer.SetOutputToNull();
                        }
                        else
                        {
                            this.fileBytes = File.ReadAllBytes(fullPath);
                        }
                        loadedFile = true;
                        SoundCache.currentSoundsLoaded++;
                    }
                    catch (Exception ex)
                    {
                        // CC was reported to crash here.  Not sure how's that possible, AFAIK all paths come the file system.
                        // Maybe we have a race somewhere, or there's something going on during sound unpacking.  For now, trace
                        // and keep an eye on this.
                        Console.WriteLine(string.Format("Exception loading file:{0}  msg:{1}  stack:{2}", fullPath, ex.Message,
                            ex.StackTrace + (ex.InnerException != null ? ex.InnerException.Message + " " + ex.InnerException.StackTrace : "")));
                    }
                }
            }
        }

        public void LoadAndCacheSound()
        {            
            lock (this)
            {
                if (!loadedFile)
                {
                    LoadAndCacheFile();
                }
                // only beeps are cached when using nAudio
                if (AudioPlayer.playWithNAudio && isBleep)
                {
                    if (loadedSoundPlayer && AudioPlayer.naudioMessagesPlaybackDeviceId != deviceIdWhenCached)
                    {
                        // naudio device ID has changed since the beep was cached, so unload and re-cache it
                        try
                        {
                            this.reader.Dispose();
                        }
                        catch (Exception)
                        { }
                        try
                        {
                            this.waveOut.Stop();
                            this.waveOut.Dispose();
                        }
                        catch (Exception) { }
                        loadedSoundPlayer = false;
                    }
                    if (!loadedSoundPlayer)
                    {
                        LoadNAudioWaveOut();
                        loadedSoundPlayer = true;
                        deviceIdWhenCached = AudioPlayer.naudioMessagesPlaybackDeviceId;
                    }
                }
                else if (!AudioPlayer.playWithNAudio && !loadedSoundPlayer)
                {
                    // if we have file bytes, load them
                    if (this.fileBytes != null)
                    {
                        this.memoryStream = new MemoryStream(this.fileBytes);
                    }
                    // if we have the TTS memory stream, use it
                    else if (this.memoryStream != null && ttsString != null)
                    {
                        this.memoryStream.Position = 0;
                        Console.WriteLine("Loading TTS sound for " + ttsString);
                    }
                    else
                    {
                        Console.WriteLine("No sound data available");
                        return;
                    }
                    this.soundPlayer = new SoundPlayer(memoryStream);
                    this.soundPlayer.Load();
                    loadedSoundPlayer = true;
                    SoundCache.activeSoundPlayerObjects++;
                }
            }
        }

        /*
         * canInterrupt will be true for regular messages triggered by the app's normal event logic. When a message
         * is played from the 'immediate' queue this will be false (spotter calls, command responses, some edge cases 
         * where the message is time-critical). If this flag is true the presence of a message in the immediate queue
         * can make the app skip playing this sound.
         */
        public void Play(SoundMetadata soundMetadata)
        {
            if (!PlaybackModerator.ShouldPlaySound(this, soundMetadata))
                return;

            PlaybackModerator.PreProcessSound(this, soundMetadata);
            if (AudioPlayer.playWithNAudio)
            {
                PlayNAudio();
            }
            else
            {
                PlaySoundPlayer();
            }
        }

        private void PlaySoundPlayer()
        {
            if (!cacheFileData)
            {
                SoundPlayer soundPlayer = new SoundPlayer(fullPath);
                soundPlayer.Load();
                soundPlayer.PlaySync();
                try
                {
                    soundPlayer.Dispose();
                }
                catch (Exception) { }
            }
            else
            {
                lock (this)
                {
                    LoadAndCacheSound();
                    this.soundPlayer.PlaySync();
                }
                if (!cacheSoundPlayer)
                {
                    try
                    {
                        this.soundPlayer.Dispose();
                    }
                    catch (Exception) { }
                    this.loadedSoundPlayer = false;
                    SoundCache.activeSoundPlayerObjects--;
                }
            }
        }

        private void PlayNAudio()
        {
            if (!cacheFileData)
            {
                // if caching is switched off, load and play the file
                NAudio.Wave.WaveOutEvent uncachedWaveOut = new NAudio.Wave.WaveOutEvent();
                uncachedWaveOut.DeviceNumber = AudioPlayer.naudioMessagesPlaybackDeviceId;
                NAudio.Wave.WaveFileReader uncachedReader = new NAudio.Wave.WaveFileReader(fullPath);
                uncachedWaveOut.PlaybackStopped += new EventHandler<NAudio.Wave.StoppedEventArgs>(playbackStopped);
                float volume = getVolume(isSpotter ? SoundCache.spotterVolumeBoost : 1f);

                if (volume == 1f)
                {
                    try
                    {
                        uncachedWaveOut.Init(uncachedReader);
                        uncachedWaveOut.Play();
                        // stop waiting after 30 seconds
                        this.playWaitHandle.WaitOne(30000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception " + e.Message + " playing sound " + this.fullPath + " stack trace " + e.StackTrace);
                    }
                }
                else
                {
                    NAudio.Wave.SampleProviders.SampleChannel sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(uncachedReader);
                    sampleChannel.Volume = volume;
                    try
                    {
                        uncachedWaveOut.Init(new NAudio.Wave.SampleProviders.SampleToWaveProvider(sampleChannel));
                        uncachedWaveOut.Play();
                        // stop waiting after 30 seconds
                        this.playWaitHandle.WaitOne(30000);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception " + e.Message + " playing sound " + this.fullPath + " stack trace " + e.StackTrace);
                    }
                }
                try
                {
                    uncachedReader.Dispose();
                }
                catch (Exception) { }
                try
                {
                    uncachedWaveOut.Dispose();
                }
                catch (Exception) { }
            }
            else
            {
                // ensure the file is loaded then play it
                lock (this)
                {
                    try
                    {
                        if (!loadedFile)
                        {
                            LoadAndCacheFile();
                        }
                        // beeps are forceably cached:
                        if (isBleep)
                        {
                            LoadAndCacheSound();
                            this.reader.CurrentTime = TimeSpan.Zero;
                            this.waveOut.Play();
                            this.playWaitHandle.WaitOne(30000);
                        }
                        else
                        {
                            LoadNAudioWaveOut();
                            this.waveOut.Play();
                            this.playWaitHandle.WaitOne(30000);
                            // if we loaded this from the raw file bytes, we can close the associated stream after playing
                            if (this.fileBytes != null)
                            {
                                try
                                {
                                    this.memoryStream.Dispose();
                                }
                                catch (Exception)
                                { }
                            }
                            try
                            {
                                this.reader.Dispose();
                            }
                            catch (Exception)
                            { }
                            try
                            {
                                this.waveOut.Stop();
                                this.waveOut.Dispose();
                            }
                            catch (Exception) { }
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception " + e.Message + " playing sound " + this.fullPath + " stack trace " + e.StackTrace);
                    }
                }
            }
        }

        private void LoadNAudioWaveOut()
        {
            this.waveOut = new NAudio.Wave.WaveOut();
            this.waveOut.DeviceNumber = AudioPlayer.naudioMessagesPlaybackDeviceId;

            float volumeBoost = isSpotter ? SoundCache.spotterVolumeBoost : 1f;
            // if we have file bytes, load them
            if (this.fileBytes != null)
            {
                this.memoryStream = new MemoryStream(this.fileBytes);
            }
            // if we have the TTS memory stream, use it
            else if (this.memoryStream != null && this.ttsString != null)
            {
                volumeBoost = SoundCache.ttsVolumeBoost;
                this.memoryStream.Position = 0;
            }
            else
            {
                Console.WriteLine("No sound data available");
                return;
            }
            this.reader = new NAudio.Wave.WaveFileReader(this.memoryStream);
            this.waveOut.PlaybackStopped += new EventHandler<NAudio.Wave.StoppedEventArgs>(playbackStopped);
            float volume = getVolume(volumeBoost);
            if (volume == 1f)
            {
                this.waveOut.Init(this.reader);
            }
            else
            {
                NAudio.Wave.SampleProviders.SampleChannel sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(this.reader);
                sampleChannel.Volume = getVolume(volumeBoost);
                this.waveOut.Init(new NAudio.Wave.SampleProviders.SampleToWaveProvider(sampleChannel));
            }
            this.reader.CurrentTime = TimeSpan.Zero;
        }

        private void playbackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            this.playWaitHandle.Set();
        }

        private float getVolume(float boost)
        {
            float volume = UserSettings.GetUserSettings().getFloat("messages_volume") * boost;
            // volume can be higher than 1, it seems. Not sure if this is device dependent
            /*if (volume > 1)
            {
                volume = 1;
            }*/
            if (volume < 0)
            {
                volume = 0;
            }
            return volume;
        }

        public Boolean UnLoad()
        {
            Boolean unloaded = false;
            lock(this)
            {
                if (this.soundPlayer != null)
                {
                    this.soundPlayer.Stop();
                }
                if (this.waveOut != null && this.waveOut.PlaybackState != NAudio.Wave.PlaybackState.Stopped)
                {
                    this.waveOut.Stop();
                }
                if (this.memoryStream != null)
                {
                    try
                    {
                        this.memoryStream.Dispose();
                    }
                    catch (Exception) { }
                }
                if (this.soundPlayer != null)
                {
                    try
                    {
                        this.soundPlayer.Dispose();
                    }
                    catch (Exception) { }
                    this.loadedSoundPlayer = false;
                    unloaded = true;
                    SoundCache.activeSoundPlayerObjects--;
                }
                if (this.waveOut != null)
                {
                    try
                    {
                        this.waveOut.Dispose();
                    }
                    catch (Exception) { }
                }
            }
            return unloaded;
        }

        public void Stop()
        {
            if (AudioPlayer.playWithNAudio && this.waveOut != null && this.waveOut.PlaybackState == NAudio.Wave.PlaybackState.Playing)
            {
                this.waveOut.Stop();
            }
            else if (!AudioPlayer.playWithNAudio && this.soundPlayer != null)
            {
                this.soundPlayer.Stop();
            }
        }

        private byte[] ConvertTTSWaveStreamToBytes(MemoryStream inputStream, int startMillisecondsToTrim, int endMillisecondsToTrim)
        {
            NAudio.Wave.WaveFileReader reader = new NAudio.Wave.WaveFileReader(inputStream);
            // can only do volume stuff if it's a 16 bit wav stream (which it should be)
            Boolean canProcessVolume = reader.WaveFormat.BitsPerSample == 16;

            // work out how many bytes to trim off the start and end
            int totalMilliseconds = (int)reader.TotalTime.TotalMilliseconds;

            // don't trim the start if the resulting sound file would be < 1 second long - some issue prevents nAudio loading the byte array
            // if the start is trimmed and the sound is very short
            if (totalMilliseconds - startMillisecondsToTrim - endMillisecondsToTrim < 1000)
            {
                startMillisecondsToTrim = 0;
            }
            double bytesPerMillisecond = (double)reader.WaveFormat.AverageBytesPerSecond / 1000d;
            int startPos = (int)(startMillisecondsToTrim * bytesPerMillisecond);
            startPos = startPos - startPos % reader.WaveFormat.BlockAlign;

            int endBytesToTrim = (int)(endMillisecondsToTrim * bytesPerMillisecond);
            endBytesToTrim = endBytesToTrim - endBytesToTrim % reader.WaveFormat.BlockAlign;
            int endPos = (int)reader.Length - endBytesToTrim;

            if (startPos > endPos)
            {
                startPos = 0;
            }

            byte[] buffer = new byte[reader.BlockAlign * 100];
            MemoryStream outputStream = new MemoryStream();

            // process the wave file header
            int headerLength = (int) (inputStream.Length - reader.Length);  // PCM wave file header size should be 46 bytes, the last 4 bytes are the sample count
            byte[] header = new byte[headerLength];
            outputStream.SetLength((endPos - startPos) + headerLength);
            inputStream.Position = 0;
            inputStream.Read(header, 0, headerLength);
            uint dataSize = BitConverter.ToUInt32(header, headerLength - 4);
            dataSize = dataSize - ((uint)endBytesToTrim + (uint)startPos);
            byte[] newSize = BitConverter.GetBytes(dataSize);
            header[headerLength - 4] = newSize[0];
            header[headerLength - 3] = newSize[1];
            header[headerLength - 2] = newSize[2];
            header[headerLength - 1] = newSize[3];
            outputStream.Write(header, 0, headerLength);
            reader.Position = startPos;
            while (reader.Position < endPos)
            {
                int bytesRequired = (int)(endPos - reader.Position);
                if (bytesRequired > 0)
                {
                    int bytesToRead = Math.Min(bytesRequired, buffer.Length);
                    int bytesRead = reader.Read(buffer, 0, bytesToRead);

                    if (canProcessVolume)
                    { 
                        for (int i = 0; i < buffer.Length; i+=2)
                        {
                            Int16 sample = BitConverter.ToInt16(buffer, i);
                            if (sample > 0)
                            {
                                sample = (short)Math.Min(short.MaxValue, sample * SoundCache.ttsVolumeBoost);
                            }
                            else
                            {
                                sample = (short)Math.Max(short.MinValue, sample * SoundCache.ttsVolumeBoost);
                            }
                            byte[] bytes = BitConverter.GetBytes(sample);
                            buffer[i] = bytes[0];
                            buffer[i+1] = bytes[1];
                        }
                    }
                    if (bytesRead > 0)
                    {
                        outputStream.Write(buffer, 0, bytesRead);
                    }
                }
            }
            return outputStream.ToArray();
        }
    }

    public class SoundVarietyDataPoint : IComparable<SoundVarietyDataPoint>
    {
        public String soundName;
        public int numSounds;
        public int timesPlayed;
        public float score;
        public SoundVarietyDataPoint(String soundName, int numSounds, int timesPlayed)
        {
            this.soundName = soundName;
            this.numSounds = numSounds;
            this.timesPlayed = timesPlayed;
            this.score = (float)numSounds / (float)timesPlayed;
        }

        // sort worst-first
        public int CompareTo(SoundVarietyDataPoint that)
        {
            if (this.score < that.score) return -1;
            if (this.score == that.score) return 0;
            return 1;
        }
    }
}
