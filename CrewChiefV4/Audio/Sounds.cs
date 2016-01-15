using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.Audio
{
    public class SoundCache
    {
        private Boolean useAlternateBeeps = UserSettings.GetUserSettings().getBoolean("use_alternate_beeps");

        // TODO: proper implementation of current player name - need multiple reusable recordings with it it - 
        // e.g. "come on [jim]...", "OK [jim]...", "fuck's sake [jim]..." and a way to represent where to
        // put this recording and which one to select based on (I think) the filename

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

        public static String PREFIX_IDENTIFIER = "prefix";
        public static String SUFFIX_IDENTIFIER = "suffix";

        public SoundCache(DirectoryInfo soundsFolder, String[] eventTypesToKeepCached, Boolean useSwearyMessages, Boolean allowCaching)
        {
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
        }

        public void Play(String soundName)
        {
            if (soundSets.ContainsKey(soundName))
            {
                SoundSet soundSet = soundSets[soundName];
                Boolean newlyLoaded = soundSets[soundName].Play();
                // now if this sound is a dynamic one, move it to the front of the cache (end of the list)
                if (!soundSet.keepCached)
                {
                    if (dynamicLoadedSounds.Contains(soundName))
                    {
                        dynamicLoadedSounds.Remove(soundName);
                    }
                    dynamicLoadedSounds.Add(soundName);
                }
                if (newlyLoaded)
                {
                    currentLoadedCount++;
                }
            }
            else if (singleSounds.ContainsKey(soundName))
            {
                if (singleSounds[soundName].Play())
                {
                    currentLoadedCount++;
                }
            }
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
            StopAll();
            foreach (SoundSet soundSet in soundSets.Values)
            {
                soundSet.UnLoadAll();
            }
            foreach (SingleSound singleSound in singleSounds.Values)
            {
                singleSound.UnLoad();
            }
        }

        public void StopAll()
        {
            foreach (SoundSet soundSet in soundSets.Values)
            {
                soundSet.StopAll();
            }
            foreach (SingleSound singleSound in singleSounds.Values)
            {
                singleSound.Stop();
            }
        }

        private void prepareFX(DirectoryInfo fxSoundDirectory) {
            FileInfo[] bleepFiles = fxSoundDirectory.GetFiles();
            String alternate_prefix = useAlternateBeeps ? "alternate_" : "";
            foreach (FileInfo bleepFile in bleepFiles)
            {
                if (bleepFile.Name.EndsWith(".wav"))
                {
                    if (bleepFile.Name.StartsWith(alternate_prefix + "start"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        }
                        singleSounds.Add("start_bleep", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "end"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        } 
                        singleSounds.Add("end_bleep", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("end_bleep");
                    }
                    else if (bleepFile.Name.StartsWith(alternate_prefix + "short_start"))
                    {
                        if (allowCaching)
                        {
                            currentLoadedCount++;
                        } 
                        singleSounds.Add("short_start_bleep", new SingleSound(bleepFile.FullName, this.allowCaching, this.allowCaching, this.allowCaching));
                        availableSounds.Add("short_start_bleep");
                    }
                    else if (bleepFile.Name.StartsWith("listen_start"))
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
                    String name = driverNameFile.Name.ToLowerInvariant().Split(new[] { ".wav" }, StringSplitOptions.None)[0];
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
        private static Random random = new Random();

        private List<SingleSound> singleSounds = new List<SingleSound>();
        private DirectoryInfo soundFolder;
        private Boolean useSwearyMessages;
        public Boolean keepCached;
        private Boolean allowCaching;
        private Boolean initialised = false;
        public Boolean hasSounds = false;
        public int soundsCount;

        public SoundSet(DirectoryInfo soundFolder, Boolean useSwearyMessages, Boolean keepCached, Boolean allowCaching)
        {
            this.soundsCount = 0;
            this.soundFolder = soundFolder;
            this.useSwearyMessages = useSwearyMessages;
            this.allowCaching = allowCaching;
            this.keepCached = keepCached;
            initialise();
        }

        private void initialise()
        {
            try
            {
                FileInfo[] soundFiles = this.soundFolder.GetFiles();
                foreach (FileInfo soundFile in soundFiles)
                {
                    if (soundFile.Name.EndsWith(".wav")) {                        
                        if (this.useSwearyMessages || !soundFile.Name.StartsWith("sweary"))
                        {                            
                            if (soundFile.Name.Contains(SoundCache.PREFIX_IDENTIFIER) || soundFile.Name.Contains(SoundCache.SUFFIX_IDENTIFIER))
                            {
                                // for this to be valid it must have a corresponding prefix
                                foreach (String prefixSuffixName in SoundCache.availablePrefixesAndSuffixes)
                                {
                                    if (soundFile.Name.Contains(prefixSuffixName) && SoundCache.soundSets.ContainsKey(prefixSuffixName))
                                    {
                                        SoundSet additionalSoundSet = SoundCache.soundSets[prefixSuffixName];
                                        if (additionalSoundSet.hasSounds)
                                        {
                                            hasSounds = true;
                                            SingleSound singleSound = new SingleSound(soundFile.FullName, this.allowCaching, this.keepCached, this.allowCaching);
                                            if (soundFile.Name.Contains(SoundCache.SUFFIX_IDENTIFIER))
                                            {
                                                singleSound.suffixSoundSet = additionalSoundSet;
                                            }
                                            else
                                            {
                                                singleSound.prefixSoundSet = additionalSoundSet;
                                            }
                                            singleSounds.Add(singleSound);
                                            soundsCount++;
                                        }
                                        break;
                                    }
                                }
                            }
                            else
                            {
                                hasSounds = true;
                                singleSounds.Add(new SingleSound(soundFile.FullName, this.allowCaching, this.keepCached, this.allowCaching));
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

        public Boolean Play() {
            if (!initialised)
            {
                initialise();
            }
            Boolean hadToLoadSound = false;
            if (singleSounds.Count > 0)
            {
                hadToLoadSound = singleSounds[SoundSet.random.Next(0, singleSounds.Count)].Play();
            }
            return hadToLoadSound;
        }

        public int UnLoadAll()
        {
            int unloadedCount = 0;
            foreach (SingleSound singleSound in singleSounds) {
                if (singleSound.UnLoad())
                {
                    unloadedCount++;
                }
            }
            return unloadedCount;
        }

        public void StopAll()
        {
            foreach (SingleSound singleSound in singleSounds)
            {
                singleSound.Stop();
            }
        }
    }

    class SingleSound
    {
        private String fullPath;
        private byte[] fileBytes;
        private MemoryStream memoryStream;
        private SoundPlayer soundPlayer;
        private Boolean allowCaching;
        private Boolean loadedSoundPlayer = false;
        private Boolean loadedFile = false;

        public SoundSet prefixSoundSet = null;
        public SoundSet suffixSoundSet = null;

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
            if (prefixSoundSet != null)
            {
                prefixSoundSet.Play();
            }
            Boolean hadToLoadSound = false;
            if (!allowCaching)
            {
                SoundPlayer soundPlayer = new SoundPlayer(fullPath);
                soundPlayer.Load();
                soundPlayer.PlaySync();
                soundPlayer.Dispose();
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
            if (suffixSoundSet != null)
            {
                suffixSoundSet.Play();
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
                this.soundPlayer.Stop();
                this.soundPlayer.Dispose();
                this.soundPlayer = null;
                this.memoryStream.Dispose();
                this.memoryStream = null;
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
