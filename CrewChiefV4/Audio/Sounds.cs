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

        //public static String playerPersonalName = UserSettings.GetUserSettings().getString("player_name");
        public static String playerPersonalName = "";
        private List<String> dynamicLoadedSounds = new List<String>();
        private Dictionary<String, SoundSet> soundSets = new Dictionary<String, SoundSet>();
        private Dictionary<String, SingleSound> singleSounds = new Dictionary<String, SingleSound>();
        public static List<String> availableDriverNames = new List<String>();
        public static List<String> availableSounds = new List<String>();
        public static SingleSound currentPlayerName = null;
        private Boolean useSwearyMessages;
        private Boolean allowCaching;
        private String[] eventTypesToKeepCached;
        private int maxCacheSize = 600;
        private int purgeBlockSize = 100;
        private int currentLoadedCount;

        public static String CURRENT_PLAYER_NAME_IDENTIFIER = "CURRENT_PLAYER_NAME";

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
                else if (soundFolder.Name == "voice")
                {
                    prepareVoice(soundFolder);
                }
                else if (soundFolder.Name == "driver_names")
                {
                    prepareDriverNames(soundFolder);
                }
                else if (soundFolder.Name == "current_player_names")
                {
                    prepareCurrentPlayerName(soundFolder);
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

        private void prepareCurrentPlayerName(DirectoryInfo currentPlayerNamesDirectory)
        {
            FileInfo[] currentPlayerNamesFiles = currentPlayerNamesDirectory.GetFiles();
            foreach (FileInfo currentPlayerNamesFile in currentPlayerNamesFiles)
            {
                if (currentPlayerNamesFile.Name.EndsWith(".wav") && currentPlayerNamesFile.Name == SoundCache.playerPersonalName.ToLower()+".wav")
                {
                    currentPlayerName = new SingleSound(currentPlayerNamesFile.FullName, false, false, this.allowCaching);                    
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
                Dictionary<String, List<String>> personalSounds = new Dictionary<String, List<String>>();
                foreach (FileInfo soundFile in soundFiles)
                {
                    if (soundFile.Name.EndsWith(".wav")) {                        
                        if (this.useSwearyMessages || !soundFile.Name.StartsWith("sweary"))
                        {
                            if (soundFile.Name.Contains("personal") && SoundCache.currentPlayerName != null)
                            {
                                // "personal_start_1.wav", "personal_end_1.wav" or "swearypersonal_start_1.wav"
                                String[] nameParts = soundFile.Name.Split('_');
                                if (nameParts.Count() == 3)
                                {
                                    if (!personalSounds.ContainsKey(nameParts[2]))
                                    {
                                        personalSounds.Add(nameParts[2], new List<String>());
                                        personalSounds[nameParts[2]].Add(SoundCache.CURRENT_PLAYER_NAME_IDENTIFIER);
                                    }
                                    List<String> existingParts = personalSounds[nameParts[2]];
                                    if (nameParts[1] == "start")
                                    {
                                        hasSounds = true;
                                        soundsCount++;
                                        existingParts.Insert(0, soundFile.FullName);
                                    }
                                    else if (nameParts[1] == "end")
                                    {
                                        hasSounds = true;
                                        soundsCount++;
                                        existingParts.Add(soundFile.FullName);
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
                foreach (List<String> personalSoundList in personalSounds.Values)
                {
                    if (personalSoundList.Count > 1)
                    {
                        singleSounds.Add(new SingleSound(personalSoundList, this.allowCaching, this.keepCached, this.allowCaching));
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
    enum CurrentPlayerPosition {
        FIRST, SECOND, THIRD, NONE
    }

    public class SingleSound
    {
        private List<String> fullPaths;
        private List<byte[]> filesBytes;
        private List<MemoryStream> memoryStreams;
        private List<SoundPlayer> soundPlayers;
        private Boolean allowCaching;
        private Boolean loadedSoundPlayer = false;
        private Boolean loadedFile = false;
        private CurrentPlayerPosition currentPlayerPosition = CurrentPlayerPosition.NONE;

        public SingleSound(String fullPath, Boolean loadFile, Boolean loadSoundPlayer, Boolean allowCaching)
        {
            this.allowCaching = allowCaching;
            this.fullPaths = new List<String>();
            this.fullPaths.Add(fullPath);
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

        public SingleSound(List<String> fullPaths, Boolean loadFile, Boolean loadSoundPlayer, Boolean allowCaching)
        {
            this.allowCaching = allowCaching;
            this.fullPaths = fullPaths;
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
            if (!allowCaching)
            {
                foreach (String fullPath in fullPaths) 
                {
                    SoundPlayer soundPlayer;
                    if (fullPath == SoundCache.CURRENT_PLAYER_NAME_IDENTIFIER)
                    {
                        SoundCache.currentPlayerName.Play();
                    }
                    else
                    {
                        soundPlayer = new SoundPlayer(fullPath);
                        soundPlayer.Load();
                        soundPlayer.PlaySync();
                        soundPlayer.Dispose();
                    }
                }
            }
            else
            {
                if (!loadedSoundPlayer)
                {
                    LoadSoundPlayer();
                    hadToLoadSound = true;
                }
                int position = 0;
                foreach (SoundPlayer soundPlayer in soundPlayers)
                {
                    if (position == 0 && currentPlayerPosition == CurrentPlayerPosition.FIRST ||
                        position == 1 && currentPlayerPosition == CurrentPlayerPosition.SECOND ||
                        position == 2 && currentPlayerPosition == CurrentPlayerPosition.THIRD)
                    {
                        SoundCache.currentPlayerName.Play();
                    }
                    soundPlayer.PlaySync();
                    position++;
                }
            }
            return hadToLoadSound;
        }

        public void LoadFile()
        {
            this.filesBytes = new List<byte[]>();
            int position = 0;
            foreach (String fullPath in fullPaths) 
            {
                if (fullPath == SoundCache.CURRENT_PLAYER_NAME_IDENTIFIER)
                {
                    if (position == 0) {
                        this.currentPlayerPosition = CurrentPlayerPosition.FIRST;
                    }
                    else if (position == 1) {
                        this.currentPlayerPosition = CurrentPlayerPosition.SECOND;
                    }
                    else if (position == 2) {
                        this.currentPlayerPosition = CurrentPlayerPosition.THIRD;
                    }
                }
                this.filesBytes.Add(File.ReadAllBytes(fullPath));
                position++;
            }
            loadedFile = true;
        }

        public void LoadSoundPlayer()
        {
            if (!loadedFile)
            {
                LoadFile();
            }
            this.memoryStreams = new List<MemoryStream>();
            this.soundPlayers = new List<SoundPlayer>();
            foreach (byte[] fileBytes in filesBytes)
            {
                MemoryStream stream = new MemoryStream(fileBytes);
                this.memoryStreams.Add(stream);
                SoundPlayer soundPlayer = new SoundPlayer(stream);
                soundPlayer.Load();
                this.soundPlayers.Add(soundPlayer);
            }
            loadedSoundPlayer = true;
        }

        public Boolean UnLoad()
        {
            Boolean unloaded = false;
            if (loadedSoundPlayer)
            {
                unloaded = true;
                foreach (SoundPlayer soundPlayer in soundPlayers)
                {
                    soundPlayer.Stop();
                    soundPlayer.Dispose();
                }
                soundPlayers.Clear();
                foreach (MemoryStream memoryStream in memoryStreams)
                {
                    memoryStream.Dispose();
                }
                this.memoryStreams.Clear();
                loadedSoundPlayer = false;
            }
            return unloaded;
        }

        public void Stop()
        {
            if (this.soundPlayers != null)
            {
                foreach (SoundPlayer soundPlayer in soundPlayers) 
                {
                    soundPlayer.Stop();
                }
            }
        }
    }
}
