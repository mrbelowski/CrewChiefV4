using Microsoft.Speech.Synthesis;
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
        private Dictionary<String, SingleSound> singleSounds = new Dictionary<String, SingleSound>();
        public static List<String> availableDriverNames = new List<String>();
        public static List<String> availableSounds = new List<String>();
        public static List<String> availablePrefixesAndSuffixes = new List<String>();
        private Boolean useSwearyMessages;
        private Boolean allowCaching;
        private String[] eventTypesToKeepCached;
        private int maxCacheSize = 600;
        private int purgeBlockSize = 100;
        private int currentLoadedCount;
        public static int prefixesAndSuffixesCount = 0;

        public static String OPTIONAL_PREFIX_IDENTIFIER = "op_prefix";
        public static String OPTIONAL_SUFFIX_IDENTIFIER = "op_suffix";
        public static String REQUIRED_PREFIX_IDENTIFIER = "rq_prefix";
        public static String REQUIRED_SUFFIX_IDENTIFIER = "rq_suffix";

        private DateTime lastPersonalisedMessageTime = DateTime.MinValue;

        public static DateTime lastSwearyMessageTime = DateTime.MinValue;

        public static SpeechSynthesizer synthesizer;

        public SoundCache(DirectoryInfo soundsFolder, String[] eventTypesToKeepCached, Boolean useSwearyMessages, Boolean allowCaching)
        {
            if (useTTS)
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
                synthesizer.SelectVoiceByHints(VoiceGender.Male, VoiceAge.Senior);
                synthesizer.SetOutputToDefaultAudioDevice();
                synthesizer.Volume = 100;
                synthesizer.Rate = 1;
            }
            this.currentLoadedCount = 0;
            this.eventTypesToKeepCached = eventTypesToKeepCached;
            this.useSwearyMessages = useSwearyMessages;
            this.allowCaching = allowCaching;
            DirectoryInfo[] soundsFolders = soundsFolder.GetDirectories();
            foreach (DirectoryInfo soundFolder in soundsFolders) {
                if (soundFolder.Name == "fx")
                {
                    prepareFX(soundFolder);
                }
                else if (soundFolder.Name == "prefixes_and_suffixes")
                {
                    preparePrefixesAndSuffixes(soundFolder);
                }
                else if (soundFolder.Name == "voice")
                {
                    prepareVoice(soundFolder);
                }
                else if (soundFolder.Name == "driver_names")
                {
                    prepareDriverNames(soundFolder);
                }                
            }
            Console.WriteLine("Finished preparing sounds cache, loaded " + singleSounds.Count + " single sounds and " + soundSets.Count + 
                " sound sets, with " + currentLoadedCount + " active SoundPlayer objects");

            if (prefixesAndSuffixesCount > 0)
            {
                Console.WriteLine(prefixesAndSuffixesCount + " sounds have personalisations");
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
                        if (!soundSet.keepCached)
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
                        if (singleSound.Play())
                        {
                            currentLoadedCount++;
                        }
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
            if (currentLoadedCount > maxCacheSize)
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
                currentLoadedCount = currentLoadedCount - purgeCount;
                Console.WriteLine("Purged " + purgedList.Count + " sounds, there are now " + currentLoadedCount + " active SoundPlayer objects");
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
            foreach (FileInfo bleepFile in bleepFiles)
            {
                if (bleepFile.Name.EndsWith(".wav"))
                {
                    if (bleepFile.Name.StartsWith(alternate_prefix + "start") && !singleSounds.ContainsKey("start_bleep"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        }
                        singleSounds.Add("start_bleep", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "end") && !singleSounds.ContainsKey("end_bleep"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        } 
                        singleSounds.Add("end_bleep", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "short_start") && !singleSounds.ContainsKey("short_start_bleep"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        } 
                        singleSounds.Add("short_start_bleep", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("short_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith("listen_start") && !singleSounds.ContainsKey("listen_start_sound"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        } 
                        singleSounds.Add("listen_start_sound", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("listen_start_sound");
                    }
                }
            }
        }

        private void prepareVoice(DirectoryInfo voiceDirectory)
        {
            DirectoryInfo[] eventFolders = voiceDirectory.GetDirectories();
            foreach (DirectoryInfo eventFolder in eventFolders)
            {
                Boolean alwaysKeepCached = this.allowCaching && this.eventTypesToKeepCached.Contains(eventFolder.Name);
                try
                {
                    DirectoryInfo[] eventDetailFolders = eventFolder.GetDirectories();
                    foreach (DirectoryInfo eventDetailFolder in eventDetailFolders)
                    {
                        String fullEventName = eventFolder.Name + "/" + eventDetailFolder.Name;
                        SoundSet soundSet = new SoundSet(eventDetailFolder, this.useSwearyMessages, alwaysKeepCached, this.allowCaching);
                        if (soundSet.hasSounds)
                        {
                            availableSounds.Add(fullEventName);
                            soundSets.Add(fullEventName, soundSet);
                            if (alwaysKeepCached)
                            {
                                currentLoadedCount += soundSet.soundsCount;
                            }
                        }
                    }
                }
                catch (DirectoryNotFoundException)
                {
                    Console.WriteLine("Unable to find events folder");
                }
            }
        }

        private void prepareDriverNames(DirectoryInfo driverNamesDirectory)
        {
            FileInfo[] driverNameFiles = driverNamesDirectory.GetFiles();
            foreach (FileInfo driverNameFile in driverNameFiles)
            {
                if (driverNameFile.Name.EndsWith(".wav"))
                {                    
                    String name = driverNameFile.Name.ToLower().Split(new[] { ".wav" }, StringSplitOptions.None)[0];
                    singleSounds.Add(name, new SingleSound(driverNameFile.FullName, false, false, this.allowCaching));
                    availableDriverNames.Add(name);
                }
            }
        }

        private void preparePrefixesAndSuffixes(DirectoryInfo prefixesAndSuffixesDirectory)
        {
            DirectoryInfo[] prefixesAndSuffixesFolders = prefixesAndSuffixesDirectory.GetDirectories();
            foreach (DirectoryInfo prefixesAndSuffixesFolder in prefixesAndSuffixesFolders)
            {
                Boolean alwaysKeepCached = this.allowCaching && this.eventTypesToKeepCached.Contains(prefixesAndSuffixesFolder.Name);
                
                SoundSet soundSet = new SoundSet(prefixesAndSuffixesFolder, this.useSwearyMessages, alwaysKeepCached, this.allowCaching);
                if (soundSet.hasSounds)
                {
                    availablePrefixesAndSuffixes.Add(prefixesAndSuffixesFolder.Name);
                    soundSets.Add(prefixesAndSuffixesFolder.Name, soundSet);
                    if (alwaysKeepCached)
                    {
                        currentLoadedCount += soundSet.soundsCount;
                    }
                }
            }
        }
    }

    public class SoundSet
    {
        private List<SingleSound> singleSoundsNoPrefixOrSuffix = new List<SingleSound>();
        private List<SingleSound> singleSoundsWithPrefixOrSuffix = new List<SingleSound>();
        private DirectoryInfo soundFolder;
        private Boolean useSwearyMessages;
        public Boolean keepCached;
        private Boolean allowCaching;
        private Boolean initialised = false;
        public Boolean hasSounds = false;
        public int soundsCount;
        public Boolean hasPrefixOrSuffix = false;
        private List<int> prefixOrSuffixIndexes = null;
        private int prefixOrSuffixIndexesPosition = 0;
        private List<int> indexes = null;
        private int indexesPosition = 0;

        public SoundSet(DirectoryInfo soundFolder, Boolean useSwearyMessages, Boolean keepCached, Boolean allowCaching)
        {
            this.soundsCount = 0;
            this.soundFolder = soundFolder;
            this.useSwearyMessages = useSwearyMessages;
            this.allowCaching = allowCaching;
            this.keepCached = keepCached;
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
                                foreach (String prefixSuffixName in SoundCache.availablePrefixesAndSuffixes)
                                {
                                    if (soundFile.Name.Contains(prefixSuffixName) && SoundCache.soundSets.ContainsKey(prefixSuffixName))
                                    {                                       
                                        SoundSet additionalSoundSet = SoundCache.soundSets[prefixSuffixName];
                                        if (additionalSoundSet.hasSounds)
                                        {
                                            hasPrefixOrSuffix = true;
                                            hasSounds = true;
                                            SingleSound singleSound = new SingleSound(soundFile.FullName, SoundCache.eagerLoadSoundFiles, this.keepCached, this.allowCaching);
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
                                    SingleSound singleSound = new SingleSound(soundFile.FullName, SoundCache.eagerLoadSoundFiles, this.keepCached, this.allowCaching);
                                    singleSound.isSweary = isSweary;
                                    singleSoundsNoPrefixOrSuffix.Add(singleSound);
                                    soundsCount++;
                                }
                            }
                            else
                            {
                                hasSounds = true;
                                SingleSound singleSound = new SingleSound(soundFile.FullName, SoundCache.eagerLoadSoundFiles, this.keepCached, this.allowCaching);
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
            if (allowCaching)
            {
                if (loadFile)
                {
                    LoadFile();
                }
                if (loadSoundPlayer)
                {
                    LoadSoundPlayer();
                }
            }
        }

        public Boolean Play()
        {
            Boolean hadToLoadSound = false;
            if (ttsString != null && SoundCache.synthesizer != null)
            {
                try { 
                    PromptBuilder builder = new PromptBuilder();
                    builder.AppendText(ttsString);
                    SoundCache.synthesizer.Speak(builder);
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
                        hadToLoadSound = true;
                    }
                    this.soundPlayer.PlaySync();
                }                
            }
            return hadToLoadSound;
        }

        public void LoadFile()
        {
            this.fileBytes = File.ReadAllBytes(fullPath);
            loadedFile = true;
        }

        public void LoadSoundPlayer()
        {
            if (!loadedFile)
            {
                LoadFile();
            }
            this.memoryStream = new MemoryStream(this.fileBytes);
            this.soundPlayer = new SoundPlayer(memoryStream);
            this.soundPlayer.Load();
            loadedSoundPlayer = true;
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
