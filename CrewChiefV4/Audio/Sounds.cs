using System.Speech.Synthesis;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    public class SoundCache
    {
        public static String TTS_IDENTIFIER = "TTS_IDENTIFIER";
        private Boolean useAlternateBeeps = UserSettings.GetUserSettings().getBoolean("use_alternate_beeps");
        public static Boolean useTTS = UserSettings.GetUserSettings().getBoolean("use_tts_for_missing_names");
        private double minSecondsBetweenPersonalisedMessages = (double)UserSettings.GetUserSettings().getInt("min_time_between_personalised_messages");
        public static Boolean eagerLoadSoundFiles = UserSettings.GetUserSettings().getBoolean("load_sound_files_on_startup");
               
        private static LinkedList<String> dynamicLoadedSounds = new LinkedList<String>();
        public static Dictionary<String, SoundSet> soundSets = new Dictionary<String, SoundSet>();
        private static Dictionary<String, SingleSound> singleSounds = new Dictionary<String, SingleSound>();
        public static HashSet<String> availableDriverNames = new HashSet<String>();
        public static HashSet<String> availableSounds = new HashSet<String>();
        public static HashSet<String> availablePrefixesAndSuffixes = new HashSet<String>();
        private Boolean useSwearyMessages;
        private static Boolean allowCaching;
        private String[] eventTypesToKeepCached;
        private int maxCacheSize = 600;
        private int soundPlayerPurgeBlockSize = 100;
        private int nAudioPurgeBlockSize = 50;
        public static int currentSoundsLoaded;
        public static int activeSoundPlayers;
        public static int prefixesAndSuffixesCount = 0;

        private Boolean purging = false;
        
        public static String OPTIONAL_PREFIX_IDENTIFIER = "op_prefix";
        public static String OPTIONAL_SUFFIX_IDENTIFIER = "op_suffix";
        public static String REQUIRED_PREFIX_IDENTIFIER = "rq_prefix";
        public static String REQUIRED_SUFFIX_IDENTIFIER = "rq_suffix";

        private DateTime lastPersonalisedMessageTime = DateTime.MinValue;

        public static DateTime lastSwearyMessageTime = DateTime.MinValue;

        public static SpeechSynthesizer synthesizer;

        public static Boolean hasSuitableTTSVoice = false;
        
        public SoundCache(DirectoryInfo soundsFolder, String[] eventTypesToKeepCached, Boolean useSwearyMessages, Boolean allowCaching, String selectedPersonalisation)
        {
            // ensure the static state is nuked before we start updating it
            SoundCache.dynamicLoadedSounds.Clear();
            SoundCache.soundSets.Clear();
            SoundCache.singleSounds.Clear();
            SoundCache.availableDriverNames.Clear();
            SoundCache.availableSounds.Clear();
            SoundCache.availablePrefixesAndSuffixes.Clear();
            if (useTTS)
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
                    synthesizer.SetOutputToDefaultAudioDevice();
                    synthesizer.Volume = 100;
                    synthesizer.Rate = 0;
                }
                catch (Exception) {
                    Console.WriteLine("Unable to initialise the TTS engine, TTS will not be available. " +
                                "Check a suitable Microsoft TTS voice pack is installed");
                    useTTS = false;
                }
            }
            SoundCache.currentSoundsLoaded = 0;
            SoundCache.activeSoundPlayers = 0;
            this.eventTypesToKeepCached = eventTypesToKeepCached;
            this.useSwearyMessages = useSwearyMessages;
            SoundCache.allowCaching = allowCaching;
            DirectoryInfo[] soundsFolders = soundsFolder.GetDirectories();
            foreach (DirectoryInfo soundFolder in soundsFolders)
            {
                if (soundFolder.Name == "fx")
                {
                    // these are eagerly loaded on the main thread, soundPlayers are created and they're always in the SoundPlayer cache.
                    prepareFX(soundFolder);
                }
                else if (soundFolder.Name == "personalisations")
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
                    prepareVoiceWithoutLoading(soundFolder);
                    // now spawn a Thread to load the sound files (and in some cases soundPlayers) in the background:
                    if (allowCaching && eagerLoadSoundFiles)
                    {
                        new Thread(() =>
                        {
                            DateTime start = DateTime.Now;
                            Thread.CurrentThread.IsBackground = true;
                            // load the permanently cached sounds first, then the rest
                            foreach (SoundSet soundSet in soundSets.Values)
                            {
                                if (soundSet.cachePermanently)
                                {
                                    soundSet.loadAll();
                                }
                            }
                            foreach (SoundSet soundSet in soundSets.Values)
                            {
                                if (!soundSet.cachePermanently)
                                {
                                    soundSet.loadAll();
                                }
                            }
                            Console.WriteLine("Took " + (DateTime.Now - start).TotalSeconds.ToString("0.00") + "s to lazy load remaining message sounds, there are now " +
                            SoundCache.currentSoundsLoaded + " loaded message sounds with " + SoundCache.activeSoundPlayers + " active SoundPlayer objects");
                        }).Start();
                    }
                }
                else if (soundFolder.Name == "driver_names")
                {
                    // The folder of driver names is processed on the main thread and objects are created to hold the sounds, 
                    // but the sound files are lazy-loaded on session start, along with the corresponding SoundPlayer objects.
                    prepareDriverNamesWithoutLoading(soundFolder);                    
                }                
            }
            Console.WriteLine("Finished preparing sounds cache, found " + singleSounds.Count + " driver names and " + soundSets.Count +
                " sound sets. Loaded " + SoundCache.currentSoundsLoaded + " message sounds with " + SoundCache.activeSoundPlayers + " active SoundPlayer objects");

            if (prefixesAndSuffixesCount > 0)
            {
                Console.WriteLine(prefixesAndSuffixesCount + " sounds have personalisations");
            }
        }

        public static void loadSingleSound(String soundName, String fullPath)
        {
            if (!singleSounds.ContainsKey(soundName))
            {
                SingleSound singleSound = new SingleSound(fullPath, true, true, true);
                singleSounds.Add(soundName, singleSound);
            }
        }

        public static Boolean hasSingleSound(String soundName)
        {
            return availableDriverNames.Contains(soundName) || singleSounds.ContainsKey(soundName);
        }

        public static void loadDriverNameSounds(List<String> names)
        {
            new Thread(() =>
            {
                int loadedCount = 0;
                DateTime start = DateTime.Now;
                foreach (String name in names)
                {
                    loadedCount++;
                    loadDriverNameSound(name);
                }
                Console.WriteLine("Took " + (DateTime.Now - start).TotalSeconds.ToString("0.00") + " seconds to load " + 
                    loadedCount + " driver name sounds. There are now " + SoundCache.currentSoundsLoaded + 
                    " sound files loaded with " + SoundCache.activeSoundPlayers + " active SoundPlayer objects");
            }).Start();            
        }

        public static void loadDriverNameSound(String name)
        {
            // if the name is in the availableDriverNames array then we have a sound file for it, so we can load it
            if (!allowCaching)
            {
                return;
            }
            if (availableDriverNames.Contains(name))
            {
                singleSounds[name].loadAndCache(true);
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
            return soundSets.ContainsKey(eventName) && soundSets[eventName].hasPrefixOrSuffix;
        }
        
        public Boolean personalisedMessageIsDue()
        {
            double secondsSinceLastPersonalisedMessage = (DateTime.Now - lastPersonalisedMessageTime).TotalSeconds;
            Boolean due = false;
            if (minSecondsBetweenPersonalisedMessages == 0)
            {
                due = false;
            }
            else if (secondsSinceLastPersonalisedMessage > minSecondsBetweenPersonalisedMessages)
            {
                // we can now select a personalised message, but we don't always do this - the probability is based 
                // on the time since the last one
                due = Utilities.random.NextDouble() < (secondsSinceLastPersonalisedMessage / minSecondsBetweenPersonalisedMessages) - 1;
            }
            return due;
        }

        public void Play(List<String> soundNames)
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
                    singleSoundsToPlay.Add(new SingleSound(soundName.Substring(TTS_IDENTIFIER.Count())));
                }
                else
                {
                    Boolean preferPersonalised = personalisedMessageIsDue();
                    SingleSound singleSound = null;
                    if (soundSets.ContainsKey(soundName))
                    {
                        SoundSet soundSet = soundSets[soundName];
                        singleSound = soundSet.getSingleSound(preferPersonalised);
                        if (!soundSet.cachePermanently)
                        {
                            lock (SoundCache.dynamicLoadedSounds)
                            {
                                SoundCache.dynamicLoadedSounds.Remove(soundName);
                                SoundCache.dynamicLoadedSounds.AddLast(soundName);
                            }
                        }
                    }
                    else if (singleSounds.ContainsKey(soundName))
                    {
                        singleSound = singleSounds[soundName];
                        if (!singleSound.cachePermanently)
                        {
                            lock (SoundCache.dynamicLoadedSounds)
                            {
                                SoundCache.dynamicLoadedSounds.Remove(soundName);
                                SoundCache.dynamicLoadedSounds.AddLast(soundName);
                            }
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
                    lastPersonalisedMessageTime = DateTime.Now;
                }
                if (suffix != null)
                {
                    singleSoundsToPlay.Add(suffix.getSingleSound(false));
                    lastPersonalisedMessageTime = DateTime.Now;
                }
                foreach (SingleSound singleSound in singleSoundsToPlay)
                {
                    if (singleSound.isPause)
                    {
                        Thread.Sleep(singleSound.pauseLength);
                    }
                    else
                    {
                        singleSound.Play();
                    }
                }
            }
        }

        public void Play(String soundName)
        {
            List<String> l = new List<String>();
            l.Add(soundName);
            Play(l);
        }

        public void ExpireCachedSounds()
        {
            if (!purging && SoundCache.activeSoundPlayers > maxCacheSize)
            {
                purging = true;
                new Thread(() =>
                {
                    int purgeBlockSize = AudioPlayer.playWithNAudio ? nAudioPurgeBlockSize : soundPlayerPurgeBlockSize;
                    var watch = System.Diagnostics.Stopwatch.StartNew();
                    int purgeCount = 0;
                    LinkedListNode<String> soundToPurge;
                    lock (SoundCache.dynamicLoadedSounds)
                    {
                        soundToPurge = SoundCache.dynamicLoadedSounds.First;
                    }
                    while (soundToPurge != null && purgeCount <= purgeBlockSize)
                    {
                        String soundToPurgeValue = soundToPurge.Value;
                        if (soundSets.ContainsKey(soundToPurgeValue))
                        {
                            purgeCount += soundSets[soundToPurgeValue].UnLoadAll();
                        }
                        else if (singleSounds.ContainsKey(soundToPurgeValue))
                        {
                            if (singleSounds[soundToPurgeValue].UnLoad())
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
                    Console.WriteLine("Purged " + purgeCount + " sounds in " + elapsedMs + "ms, there are now " + SoundCache.activeSoundPlayers + " active SoundPlayer objects");
                    purging = false;
                }).Start();
            }
        }

        public void StopAndUnloadAll()
        {
            new Thread(() =>
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
            }).Start();
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
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
                        singleSounds.Add("start_bleep", sound);
                        availableSounds.Add("start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "end") && !singleSounds.ContainsKey("end_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
                        singleSounds.Add("end_bleep", sound);
                        availableSounds.Add("end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "short_start") && !singleSounds.ContainsKey("short_start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
                        singleSounds.Add("short_start_bleep", sound);
                        availableSounds.Add("short_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith("listen_start") && !singleSounds.ContainsKey("listen_start_sound"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
                        singleSounds.Add("listen_start_sound", sound);
                        availableSounds.Add("listen_start_sound");
                    }
                    else if (bleepFile.Name.StartsWith(opposite_prefix + "start") && !singleSounds.ContainsKey("alternate_start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
                        singleSounds.Add("alternate_start_bleep", sound);
                        availableSounds.Add("alternate_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(opposite_prefix + "end") && !singleSounds.ContainsKey("alternate_end_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
                        singleSounds.Add("alternate_end_bleep", sound);
                        availableSounds.Add("alternate_end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(opposite_prefix + "short_start") && !singleSounds.ContainsKey("alternate_short_start_bleep"))
                    {
                        SingleSound sound = new SingleSound(bleepFile.FullName, eagerLoadSoundFiles, allowCaching, allowCaching);
                        sound.cachePermanently = true;
                        sound.isBleep = true;
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

        private void prepareVoiceWithoutLoading(DirectoryInfo voiceDirectory)
        {
            Console.WriteLine("Preparing voice messages");
            DirectoryInfo[] eventFolders = voiceDirectory.GetDirectories();
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
                        SoundSet soundSet = new SoundSet(eventDetailFolder, this.useSwearyMessages, false, cachePermanently, allowCaching, cachePermanently);
                        if (soundSet.hasSounds)
                        {
                            if (soundSets.ContainsKey(fullEventName))
                            {
                                Console.WriteLine("event " + fullEventName + " sound set is already loaded");
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
                    singleSounds.Add(name, new SingleSound(driverNameFile.FullName, false, false, allowCaching));
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
                    DirectoryInfo[] prefixesAndSuffixesFolders = namesFolder.GetDirectories();
                    if (prefixesAndSuffixesFolders.Length == 1)
                    {
                        foreach (DirectoryInfo prefixesAndSuffixesFolder in prefixesAndSuffixesFolders[0].GetDirectories())
                        {
                            // always keep the personalisations cached as they're reused frequently, so create the sound players immediately after the files are loaded
                            SoundSet soundSet = new SoundSet(prefixesAndSuffixesFolder, this.useSwearyMessages, eagerLoadSoundFiles, allowCaching, allowCaching, true);
                            if (soundSet.hasSounds)
                            {
                                availablePrefixesAndSuffixes.Add(prefixesAndSuffixesFolder.Name);
                                soundSets.Add(prefixesAndSuffixesFolder.Name, soundSet);
                            }
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
        public Boolean allowCaching;
        public Boolean loadFiles;
        public Boolean createSoundPlayersImmediatelyAfterLoading;
        public Boolean cachePermanently;
        private Boolean initialised = false;
        public Boolean hasSounds = false;
        public int soundsCount;
        public Boolean hasPrefixOrSuffix = false;
        private List<int> prefixOrSuffixIndexes = null;
        private int prefixOrSuffixIndexesPosition = 0;
        private List<int> indexes = null;
        private int indexesPosition = 0;

        public SoundSet(DirectoryInfo soundFolder, Boolean useSwearyMessages, Boolean loadFiles, Boolean createSoundPlayersImmediatelyAfterLoading, 
            Boolean allowCaching, Boolean cachePermanently)
        {
            this.soundsCount = 0;
            this.soundFolder = soundFolder;
            this.useSwearyMessages = useSwearyMessages;
            this.loadFiles = loadFiles;
            this.createSoundPlayersImmediatelyAfterLoading = createSoundPlayersImmediatelyAfterLoading;
            this.allowCaching = allowCaching;
            this.cachePermanently = allowCaching && cachePermanently;
            initialise();
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
                sound.loadAndCache(createSoundPlayersImmediatelyAfterLoading);
            }
            foreach (SingleSound sound in singleSoundsWithPrefixOrSuffix)
            {
                sound.loadAndCache(createSoundPlayersImmediatelyAfterLoading);
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
                        Boolean isSpotter = soundFile.FullName.Contains(@"\spotter") || soundFile.FullName.Contains(@"\radio_check_");
                        if (this.useSwearyMessages || !isSweary)
                        {
                            if (soundFile.Name.Contains(SoundCache.REQUIRED_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.REQUIRED_SUFFIX_IDENTIFIER) ||
                                soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER))
                            {
                                Boolean isOptional = soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.OPTIONAL_SUFFIX_IDENTIFIER);
                                foreach (String prefixSuffixName in SoundCache.availablePrefixesAndSuffixes)
                                {
                                    if (soundFile.Name.Contains(prefixSuffixName) && SoundCache.soundSets.ContainsKey(prefixSuffixName))
                                    {                                       
                                        SoundSet additionalSoundSet = SoundCache.soundSets[prefixSuffixName];
                                        if (additionalSoundSet.hasSounds)
                                        {
                                            hasPrefixOrSuffix = true;
                                            hasSounds = true;
                                            SingleSound singleSound = new SingleSound(soundFile.FullName, this.loadFiles, this.createSoundPlayersImmediatelyAfterLoading, this.allowCaching);
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
                                    SingleSound singleSound = new SingleSound(soundFile.FullName, this.loadFiles, this.createSoundPlayersImmediatelyAfterLoading, this.allowCaching);
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
                                SingleSound singleSound = new SingleSound(soundFile.FullName, this.loadFiles, this.createSoundPlayersImmediatelyAfterLoading, this.allowCaching);
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
            if (preferPersonalised && singleSoundsWithPrefixOrSuffix.Count > 0)
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
                        if (prefixOrSuffixIndexesPosition == prefixOrSuffixIndexes.Count || DateTime.Now > SoundCache.lastSwearyMessageTime + TimeSpan.FromSeconds(10))
                        {
                            SoundCache.lastSwearyMessageTime = DateTime.Now;
                            break;
                        }
                    }
                }
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
                        if (indexesPosition == indexes.Count || DateTime.Now > SoundCache.lastSwearyMessageTime + TimeSpan.FromSeconds(10))
                        {
                            SoundCache.lastSwearyMessageTime = DateTime.Now;
                            break;
                        }
                    }
                }
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
        private byte[] fileBytes;
        private MemoryStream memoryStream;
        private SoundPlayer soundPlayer;

        private NAudio.Wave.WaveOutEvent waveOut;
        private NAudio.Wave.WaveFileReader reader;
        private float volumeWhenCached = 0;
        private int deviceIdWhenCached = 0;

        private Boolean allowCaching;
        private Boolean loadedSoundPlayer = false;
        private Boolean loadedFile = false;
        public Boolean cachePermanently = false;

        public SoundSet prefixSoundSet = null;
        public SoundSet suffixSoundSet = null;

        AutoResetEvent playWaitHandle = new AutoResetEvent(false);

        public SingleSound(int pauseLength)
        {
            this.isPause = true;
            this.pauseLength = pauseLength;
        }

        public SingleSound(String textToRender)
        {
            this.ttsString = textToRender;
        }

        public SingleSound(String fullPath, Boolean loadFile, Boolean createSoundPlayerImmediatelyAfterLoading, Boolean allowCaching)
        {
            this.allowCaching = allowCaching;
            this.fullPath = fullPath;
            if (loadFile && allowCaching)
            {
                loadAndCache(createSoundPlayerImmediatelyAfterLoading);
            }
        }

        public void loadAndCache(Boolean loadSoundPlayer)
        {
            LoadFile();
            if (loadSoundPlayer)
            {
                if (AudioPlayer.playWithNAudio)
                {
                    LoadNAudioWaveOutAndCache();
                }
                else
                {
                    LoadSoundPlayer();
                }
            }
        }

        public void Play()
        {
            if (!PlaybackModerator.ShouldPlaySound(this))
                return;

            PlaybackModerator.PreProcessSound(this);

            if (ttsString != null && SoundCache.synthesizer != null)
            {
                try
                {
                    SoundCache.synthesizer.Speak(ttsString);
                }
                catch (Exception e)
                {
                    Console.WriteLine("TTS failed with sound " + ttsString + ", " + e.Message);
                }
            }
            else
            {
                if (AudioPlayer.playWithNAudio)
                {
                    PlayNAudio();
                }
                else
                {
                    PlaySoundPlayer();
                }
            }
        }

        private void PlayNAudio()
        {
            // if the file isn't yet loaded, play by reading it directly
            if (!allowCaching || !loadedFile)
            {
                NAudio.Wave.WaveOutEvent uncachedWaveOut = new NAudio.Wave.WaveOutEvent();
                uncachedWaveOut.DeviceNumber = AudioPlayer.naudioMessagesPlaybackDeviceId;
                NAudio.Wave.WaveFileReader uncachedReader = new NAudio.Wave.WaveFileReader(fullPath);
                uncachedWaveOut.PlaybackStopped += new EventHandler<NAudio.Wave.StoppedEventArgs>(playbackStopped);
                NAudio.Wave.SampleProviders.SampleChannel sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(uncachedReader);
                sampleChannel.Volume = getVolume();

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
                lock (this)
                {
                    try
                    {
                        if (getVolume() != volumeWhenCached || this.deviceIdWhenCached != AudioPlayer.naudioMessagesPlaybackDeviceId)
                        {
                            UnLoad();
                        }
                        if (!loadedSoundPlayer)
                        {
                            LoadNAudioWaveOutAndCache();
                        }
                        if (loadedSoundPlayer)
                        {
                            this.reader.CurrentTime = TimeSpan.Zero;
                            this.waveOut.Play();
                            this.playWaitHandle.WaitOne(30000);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Exception " + e.Message + " playing sound " + this.fullPath + " stack trace " + e.StackTrace);
                    }
                }
            }
        }

        private void playbackStopped(object sender, NAudio.Wave.StoppedEventArgs e)
        {
            this.playWaitHandle.Set();
        }

        private float getVolume()
        {
            float volume = UserSettings.GetUserSettings().getFloat("messages_volume");
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

        private void PlaySoundPlayer()
        {
            if (!allowCaching)
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
                    if (!loadedSoundPlayer)
                    {
                        LoadSoundPlayer();
                    }
                    this.soundPlayer.PlaySync();
                }
            }
        }

        public void LoadFile()
        {
            lock (this)
            {
                if (!loadedFile)
                {
                    try
                    {
                        this.fileBytes = File.ReadAllBytes(fullPath);
                        loadedFile = true;
                        SoundCache.currentSoundsLoaded++;
                    }
                    catch (Exception ex)
                    {
                        // CC was reported to crash here.  Not sure how's that possible, AFAIK all paths come the file system.
                        // Maybe we have a race somewhere, or there's something going on during sound unpacking.  For now, trace
                        // and keep an eye on this.
                        Console.WriteLine(string.Format("Exception loading file:{0}  msg:{1}  stack:{2}"), fullPath, ex.Message,
                            ex.StackTrace + (ex.InnerException != null ? ex.InnerException.Message + " " + ex.InnerException.StackTrace : ""));
                    }
                }
            }
        }

        private void LoadSoundPlayer()
        {
            lock (this)
            {
                if (!loadedFile)
                {
                    LoadFile();
                }
                if (!loadedSoundPlayer)
                {
                    this.memoryStream = new MemoryStream(this.fileBytes);
                    this.soundPlayer = new SoundPlayer(memoryStream);
                    this.soundPlayer.Load();
                    loadedSoundPlayer = true;
                    SoundCache.activeSoundPlayers++;
                }
            }
        }

        private void LoadNAudioWaveOutAndCache()
        {
            lock (this)
            {
                if (!loadedFile)
                {
                    LoadFile();
                }
                if (!loadedSoundPlayer)
                {
                    try
                    {
                        this.waveOut = new NAudio.Wave.WaveOutEvent();
                        this.deviceIdWhenCached = AudioPlayer.naudioMessagesPlaybackDeviceId;
                        this.waveOut.DeviceNumber = this.deviceIdWhenCached;
                        this.memoryStream = new MemoryStream(this.fileBytes);
                        this.reader = new NAudio.Wave.WaveFileReader(this.memoryStream);

                        this.waveOut.PlaybackStopped += new EventHandler<NAudio.Wave.StoppedEventArgs>(playbackStopped);
                        NAudio.Wave.SampleProviders.SampleChannel sampleChannel = new NAudio.Wave.SampleProviders.SampleChannel(reader);
                        this.volumeWhenCached = getVolume();
                        sampleChannel.Volume = volumeWhenCached;
                        this.waveOut.Init(new NAudio.Wave.SampleProviders.SampleToWaveProvider(sampleChannel));
                        loadedSoundPlayer = true;
                        SoundCache.activeSoundPlayers++;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to initialise nAudio sound " + this.fullPath + " stack trace " + e.StackTrace);
                    }
                }
            }
        }

        public Boolean UnLoad()
        {
            Boolean unloaded = false;
            lock(this)
            {
                if (loadedSoundPlayer)
                {                
                    unloaded = true;
                    if (AudioPlayer.playWithNAudio)
                    {
                        if (this.reader != null)
                        {
                            try
                            {
                                this.reader.Dispose();
                            }
                            catch (Exception) { }
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
                    else
                    {
                        if (this.soundPlayer != null)
                        {
                            this.soundPlayer.Stop();
                            try
                            {
                                this.soundPlayer.Dispose();
                            }
                            catch (Exception) { }
                            this.soundPlayer = null;
                        }
                    }
                    if (this.memoryStream != null)
                    {
                        try
                        {
                            this.memoryStream.Dispose();
                        }
                        catch (Exception) { }
                        this.memoryStream = null;
                    }
                    loadedSoundPlayer = false;
                    SoundCache.activeSoundPlayers--;
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
    }
}
