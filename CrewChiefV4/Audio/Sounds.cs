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
        public static Random random = new Random();
        private Boolean useAlternateBeeps = UserSettings.GetUserSettings().getBoolean("use_alternate_beeps");
        public static Boolean useTTS = UserSettings.GetUserSettings().getBoolean("use_tts_for_missing_names");
        private double minSecondsBetweenPersonalisedMessages = (double)UserSettings.GetUserSettings().getInt("min_time_between_personalised_messages");
        public static Boolean eagerLoadSoundFiles = UserSettings.GetUserSettings().getBoolean("load_sound_files_on_startup");
        
        private List<String> dynamicLoadedSounds = new List<String>();
        public static Dictionary<String, SoundSet> soundSets = new Dictionary<String, SoundSet>();
        private static Dictionary<String, SingleSound> singleSounds = new Dictionary<String, SingleSound>();
        public static List<String> sortedAvailableDriverNames = new List<String>();
        public static List<String> sortedAvailableSounds = new List<String>();
        public static List<String> sortedAvailablePrefixesAndSuffixes = new List<String>();
        private Boolean useSwearyMessages;
        private static Boolean allowCaching;
        private String[] eventTypesToKeepCached;
        private int maxCacheSize = 600;
        private int purgeBlockSize = 100;
        public static int currentSoundsLoaded;
        public static int activeSoundPlayers;
        public static int prefixesAndSuffixesCount = 0;

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
                        catch (Exception e) { }
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

                    synthesizer.SelectVoiceByHints(VoiceGender.Male, hasAdult ? VoiceAge.Adult : VoiceAge.Senior);
                    synthesizer.SetOutputToDefaultAudioDevice();
                    synthesizer.Volume = 100;
                    synthesizer.Rate = 0;
                }
                catch (Exception e) {
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
            foreach (DirectoryInfo soundFolder in soundsFolders) {
                if (soundFolder.Name == "fx")
                {
                    prepareFX(soundFolder);
                }
                else if (soundFolder.Name == "personalisations") {
                    if (selectedPersonalisation != AudioPlayer.NO_PERSONALISATION_SELECTED)
                    {
                        preparePrefixesAndSuffixes(soundFolder, selectedPersonalisation);
                    }
                    else
                    {
                        Console.WriteLine("No name has been selected for personalised messages");
                    }
                }
                else if (soundFolder.Name == "voice")
                {
                    prepareVoiceWithoutLoading(soundFolder);
                    // now spawn a Thread to load the voices in the background
                    if (allowCaching)
                    {
                        new Thread(() =>
                        {
                            DateTime start = DateTime.Now;
                            Thread.CurrentThread.IsBackground = true;
                            foreach (SoundSet soundSet in soundSets.Values)
                            {
                                soundSet.loadAll();
                            }
                            Console.WriteLine("Took " + (DateTime.Now - start).TotalMilliseconds + " ms to load voice sounds");
                        }).Start();
                    }
                }
                else if (soundFolder.Name == "driver_names")
                {
                    prepareDriverNamesWithoutLoading(soundFolder);
                    // these are lazy-loaded on session start
                }                
            }
            Console.WriteLine("Finished preparing sounds cache, loaded " + singleSounds.Count + " single sounds and " + soundSets.Count +
                " sound sets, loaded " + SoundCache.currentSoundsLoaded + " sound files with " + SoundCache.activeSoundPlayers + " active SoundPlayer objects");

            if (prefixesAndSuffixesCount > 0)
            {
                Console.WriteLine(prefixesAndSuffixesCount + " sounds have personalisations");
            }
        }

        public static void loadDriverNameSounds(List<String> names)
        {
            foreach (String name in names)
            {
                loadDriverNameSound(name);
            }
        }

        public static void loadDriverNameSound(String name)
        {
            if (allowCaching && singleSounds.ContainsKey(name))
            {
                singleSounds[name].loadAndCache(true);
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
                due = random.NextDouble() < (secondsSinceLastPersonalisedMessage / minSecondsBetweenPersonalisedMessages) - 1;
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
                            if (dynamicLoadedSounds.Contains(soundName))
                            {
                                dynamicLoadedSounds.Remove(soundName);
                            }
                            dynamicLoadedSounds.Add(soundName);
                        }
                    }
                    else if (singleSounds.ContainsKey(soundName))
                    {
                        singleSound = singleSounds[soundName];
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
            if (SoundCache.activeSoundPlayers > maxCacheSize)
            {
                int purgeCount = 0;
                List<String> purgedList = new List<string>();
                for (int i = 0; i < maxCacheSize && i < dynamicLoadedSounds.Count && i < purgeBlockSize; i++)
                {
                    String soundToPurge = dynamicLoadedSounds[i];
                    purgedList.Add(soundToPurge);
                    if (soundSets.ContainsKey(soundToPurge))
                    {
                        purgeCount += soundSets[soundToPurge].UnLoadAll();
                    }
                    else if (singleSounds.ContainsKey(soundToPurge))
                    {
                        if (singleSounds[soundToPurge].UnLoad())
                        {
                            purgeCount++;
                        }
                    }
                }
                foreach (String purged in purgedList)
                {
                    dynamicLoadedSounds.Remove(purged);
                }
                SoundCache.activeSoundPlayers = SoundCache.activeSoundPlayers - purgeCount;
                Console.WriteLine("Purged " + purgedList.Count + " sounds, there are now " + SoundCache.activeSoundPlayers + " active SoundPlayer objects");
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
                catch (Exception e) { }
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
            Console.WriteLine("Preparing sound effects");
            foreach (FileInfo bleepFile in bleepFiles)
            {
                if (bleepFile.Name.EndsWith(".wav"))
                {
                    if (bleepFile.Name.StartsWith(alternate_prefix + "start") && !singleSounds.ContainsKey("start_bleep"))
                    {
                        singleSounds.Add("start_bleep", new SingleSound(bleepFile.FullName, allowCaching, allowCaching, allowCaching));
                        sortedAvailableSounds.Add("start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "end") && !singleSounds.ContainsKey("end_bleep"))
                    {
                        singleSounds.Add("end_bleep", new SingleSound(bleepFile.FullName, allowCaching, allowCaching, allowCaching));
                        sortedAvailableSounds.Add("end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "short_start") && !singleSounds.ContainsKey("short_start_bleep"))
                    {
                        singleSounds.Add("short_start_bleep", new SingleSound(bleepFile.FullName, allowCaching, allowCaching, allowCaching));
                        sortedAvailableSounds.Add("short_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith("listen_start") && !singleSounds.ContainsKey("listen_start_sound"))
                    {
                        singleSounds.Add("listen_start_sound", new SingleSound(bleepFile.FullName, allowCaching, allowCaching, allowCaching));
                        sortedAvailableSounds.Add("listen_start_sound");
                    }
                }
            }
            sortedAvailableSounds.Sort();
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
                Boolean alwaysKeepCached = allowCaching && this.eventTypesToKeepCached.Contains(eventFolder.Name);
                try
                {
                    DirectoryInfo[] eventDetailFolders = eventFolder.GetDirectories();
                    foreach (DirectoryInfo eventDetailFolder in eventDetailFolders)
                    {
                        String fullEventName = eventFolder.Name + "/" + eventDetailFolder.Name;
                        SoundSet soundSet = new SoundSet(eventDetailFolder, this.useSwearyMessages, false, false, allowCaching, alwaysKeepCached);
                        if (soundSet.hasSounds)
                        {
                            sortedAvailableSounds.Add(fullEventName);
                            soundSets.Add(fullEventName, soundSet);
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine("Unable to find events folder");
                }
            }
            sortedAvailableSounds.Sort();
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
                    sortedAvailableDriverNames.Add(name);
                }
            }
            sortedAvailableDriverNames.Sort();
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
                            Boolean alwaysKeepCached = allowCaching && this.eventTypesToKeepCached.Contains(prefixesAndSuffixesFolder.Name);
                            // always keep the personalisations cached as they're reused frequently
                            SoundSet soundSet = new SoundSet(prefixesAndSuffixesFolder, this.useSwearyMessages, allowCaching, allowCaching, allowCaching, true);
                            if (soundSet.hasSounds)
                            {
                                sortedAvailablePrefixesAndSuffixes.Add(prefixesAndSuffixesFolder.Name);
                                soundSets.Add(prefixesAndSuffixesFolder.Name, soundSet);
                            }
                        }
                    }
                    break;
                }
            }
            sortedAvailablePrefixesAndSuffixes.Sort();
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
        public Boolean loadSoundPlayers;
        public Boolean cachePermanently;
        private Boolean initialised = false;
        public Boolean hasSounds = false;
        public int soundsCount;
        public Boolean hasPrefixOrSuffix = false;
        private List<int> prefixOrSuffixIndexes = null;
        private int prefixOrSuffixIndexesPosition = 0;
        private List<int> indexes = null;
        private int indexesPosition = 0;

        public SoundSet(DirectoryInfo soundFolder, Boolean useSwearyMessages, Boolean loadFiles, Boolean loadSoundPlayers, Boolean allowCaching, Boolean cachePermanently)
        {
            this.soundsCount = 0;
            this.soundFolder = soundFolder;
            this.useSwearyMessages = useSwearyMessages;
            this.loadFiles = loadFiles;
            this.loadSoundPlayers = loadSoundPlayers;
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
                int k = SoundCache.random.Next(n + 1);
                int value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public void loadAll()
        {
            foreach (SingleSound sound in singleSoundsNoPrefixOrSuffix)
            {
                sound.loadAndCache(loadSoundPlayers);
            }
            foreach (SingleSound sound in singleSoundsWithPrefixOrSuffix)
            {
                sound.loadAndCache(loadSoundPlayers);
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
                        if (this.useSwearyMessages || !isSweary)
                        {
                            if (soundFile.Name.Contains(SoundCache.REQUIRED_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.REQUIRED_SUFFIX_IDENTIFIER) ||
                                soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER))
                            {
                                Boolean isOptional = soundFile.Name.Contains(SoundCache.OPTIONAL_PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.OPTIONAL_SUFFIX_IDENTIFIER);
                                foreach (String prefixSuffixName in SoundCache.sortedAvailablePrefixesAndSuffixes)
                                {
                                    if (soundFile.Name.Contains(prefixSuffixName) && SoundCache.soundSets.ContainsKey(prefixSuffixName))
                                    {                                       
                                        SoundSet additionalSoundSet = SoundCache.soundSets[prefixSuffixName];
                                        if (additionalSoundSet.hasSounds)
                                        {
                                            hasPrefixOrSuffix = true;
                                            hasSounds = true;
                                            SingleSound singleSound = new SingleSound(soundFile.FullName, this.loadFiles, this.loadSoundPlayers, this.allowCaching);
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
                                    SingleSound singleSound = new SingleSound(soundFile.FullName, this.loadFiles, this.loadSoundPlayers, this.allowCaching);
                                    singleSound.isSweary = isSweary;
                                    singleSoundsNoPrefixOrSuffix.Add(singleSound);
                                    soundsCount++;
                                }
                            }
                            else
                            {
                                hasSounds = true;
                                SingleSound singleSound = new SingleSound(soundFile.FullName, this.loadFiles, this.loadSoundPlayers, this.allowCaching);
                                singleSound.isSweary = isSweary;
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
        public int pauseLength = 0;
        private String fullPath;
        private byte[] fileBytes;
        private MemoryStream memoryStream;
        private SoundPlayer soundPlayer;
        private Boolean allowCaching;
        private Boolean loadedSoundPlayer = false;
        private Boolean loadedFile = false;

        public SoundSet prefixSoundSet = null;
        public SoundSet suffixSoundSet = null;

        public SingleSound(int pauseLength)
        {
            this.isPause = true;
            this.pauseLength = pauseLength;
        }

        public SingleSound(String textToRender)
        {
            this.ttsString = textToRender;
        }

        public SingleSound(String fullPath, Boolean loadFile, Boolean loadSoundPlayer, Boolean allowCaching)
        {
            this.allowCaching = allowCaching;
            this.fullPath = fullPath;
            if (loadFile && allowCaching)
            {
                loadAndCache(loadSoundPlayer);
            }
        }

        public void loadAndCache(Boolean loadSoundPlayer)
        {
            LoadFile();
            if (loadSoundPlayer)
            {
                LoadSoundPlayer();
            }
        }

        public void Play()
        {
            if (ttsString != null && SoundCache.synthesizer != null)
            {
                try { 
                    SoundCache.synthesizer.Speak(ttsString);
                }
                catch (Exception e)
                {
                    Console.WriteLine("TTS failed with sound " + ttsString + ", " + e.Message);
                }
            }
            else
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
                    this.fileBytes = File.ReadAllBytes(fullPath);
                    loadedFile = true;
                    SoundCache.currentSoundsLoaded++;
                }
            }
        }

        public void LoadSoundPlayer()
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

        public Boolean UnLoad()
        {
            Boolean unloaded = false;
            if (loadedSoundPlayer)
            {
                unloaded = true;
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
            return unloaded;
        }

        public void Stop()
        {
            if (this.soundPlayer != null)
            {
                this.soundPlayer.Stop();
            }
        }
    }
}
