using NAudio.Wave;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using System.IO;
using CrewChiefV4.Audio;

namespace CrewChiefV4
{
    class DriverTrainingService
    {
        private static long expireUnplayedMessagesAfter = 500;  // milliseconds
        private static int combineEntriesCloserThan = 20; // if a new entry's lap distance is within 20 metres of an existing entry's lap distance, combine them
        public static Boolean isPlayingPaceNotes = false;
        public static Boolean isRecordingPaceNotes = false;
        private static Boolean isRecordingSound = false;
        private static MetaData recordingMetaData;
        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;

        private static GameEnum gameEnum;
        private static String trackName;
        private static CarData.CarClassEnum carClass;

        private static String folderPathForPaceNotes;

        private static Object _lock = new Object();

        public static Boolean loadPaceNotes(GameEnum gameEnum, String trackName, CarData.CarClassEnum carClass)
        {
            if (!isRecordingPaceNotes && !isPlayingPaceNotes)
            {
                Console.WriteLine("Playing pace notes for circuit " + trackName + " with car class " + carClass.ToString());

                isRecordingPaceNotes = false;
                isRecordingSound = false;
                if (carClass != CarData.CarClassEnum.USER_CREATED && carClass != CarData.CarClassEnum.UNKNOWN_RACE)
                {
                    DriverTrainingService.folderPathForPaceNotes = getCarSpecificFolderPath(gameEnum, trackName, carClass);
                    if (!Directory.Exists(DriverTrainingService.folderPathForPaceNotes))
                    {
                        Console.WriteLine("No pace notes folder exists for car class " + carClass + ", game " + gameEnum + ", track " + trackName +
                            ". Checking for pace notes folder applicable to any car");
                        DriverTrainingService.folderPathForPaceNotes = getAnyCarFolderPath(gameEnum, trackName);
                        if (!Directory.Exists(DriverTrainingService.folderPathForPaceNotes))
                        {
                            Console.WriteLine("Unable to find any pace notes set for game " + gameEnum + ", track " + trackName);
                            return false;
                        }
                    }
                }
                else
                {
                    DriverTrainingService.folderPathForPaceNotes = getAnyCarFolderPath(gameEnum, trackName);
                    if (!Directory.Exists(DriverTrainingService.folderPathForPaceNotes))
                    {
                        Console.WriteLine("Unable to find any pace notes set for game " + gameEnum + ", track " + trackName);
                        return false;
                    }
                }

                String fileName = System.IO.Path.Combine(folderPathForPaceNotes, "metadata.json");
                if (File.Exists(fileName))
                {
                    try
                    {
                        DriverTrainingService.recordingMetaData = JsonConvert.DeserializeObject<MetaData>(File.ReadAllText(fileName));
                        if (DriverTrainingService.recordingMetaData.description != null && !DriverTrainingService.recordingMetaData.description.Equals(""))
                        {
                            Console.WriteLine("Playing pace notes with description " + DriverTrainingService.recordingMetaData.description);
                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to parse pace notes metadata file: " + e.Message);
                        return false;
                    }
                    foreach (MetaDataEntry entry in DriverTrainingService.recordingMetaData.entries)
                    {
                        for (int i = 0; i < entry.recordingNames.Count; i++)
                        {
                            try
                            {
                                SoundCache.loadSingleSound(entry.recordingNames[i], System.IO.Path.Combine(DriverTrainingService.folderPathForPaceNotes, entry.fileNames[i]));
                            }
                            catch (Exception e)
                            {
                                Console.WriteLine("Unable to load a sound from pace notes set " + DriverTrainingService.folderPathForPaceNotes + " : " + e.Message);
                                return false;
                            }
                        }
                    }
                    isPlayingPaceNotes = true;
                }
                else
                {
                    Console.WriteLine("No metadata.json file exists in the pace notes folder " + DriverTrainingService.folderPathForPaceNotes);
                }
                return true;
            }
            else
            {
                if (isRecordingPaceNotes)
                {
                    Console.WriteLine("A recording is already in progress, complete this first");
                }
                else
                {
                    Console.WriteLine("Already playing a session");
                }
                return false;
            }
        }

        public static void stopPlayingPaceNotes()
        {
            isPlayingPaceNotes = false;
        }

        public static void stopRecordingPaceNotes()
        {
            isRecordingPaceNotes = false;
        }

        public static void checkDistanceAndPlayIfNeeded(DateTime now, float previousDistanceRoundTrack, float currentDistanceRoundTrack, AudioPlayer audioPlayer)
        {
            if (isPlayingPaceNotes && !isRecordingPaceNotes && DriverTrainingService.recordingMetaData != null)
            {
                foreach (MetaDataEntry entry in DriverTrainingService.recordingMetaData.entries)
                {
                    if (previousDistanceRoundTrack < entry.distanceRoundTrack && currentDistanceRoundTrack > entry.distanceRoundTrack)
                    {
                        if (entry.description != null && !entry.description.Equals(""))
                        {
                            Console.WriteLine("Playing entry at distance " + entry.distanceRoundTrack + " with description " + entry.description);
                        }
                        else
                        {
                            Console.WriteLine("Playing entry at distance " + entry.distanceRoundTrack);
                        }
                        QueuedMessage message = new QueuedMessage(entry.getRandomRecordingName(), 0, null) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 0) };
                        message.expiryTime = (now.Ticks / TimeSpan.TicksPerMillisecond) + expireUnplayedMessagesAfter;
                        audioPlayer.playMessageImmediately(message);
                    }
                }
            }
        }

        public static void startRecordingPaceNotes(GameEnum gameEnum, String trackName, CarData.CarClassEnum carClass)
        {
            if (!isPlayingPaceNotes && !isRecordingPaceNotes)
            {
                Console.WriteLine("Recording a pace notes session for circuit " + trackName + " with car class " + carClass.ToString());
                DriverTrainingService.gameEnum = gameEnum;
                DriverTrainingService.trackName = trackName;
                DriverTrainingService.carClass = carClass;
                if (carClass == CarData.CarClassEnum.UNKNOWN_RACE || carClass == CarData.CarClassEnum.USER_CREATED)
                {
                    Console.WriteLine("Recording pace notes for any car class");
                    DriverTrainingService.folderPathForPaceNotes = getAnyCarFolderPath(gameEnum, trackName);
                }
                else
                {
                    Console.WriteLine("Recording pace notes for car class " + carClass.ToString());
                    DriverTrainingService.folderPathForPaceNotes = getCarSpecificFolderPath(gameEnum, trackName, carClass);
                }
                Boolean createFolder = true;
                Boolean createNewMetaData = true;
                if (System.IO.Directory.Exists(folderPathForPaceNotes))
                {
                    createFolder = false;
                    String fileName = System.IO.Path.Combine(folderPathForPaceNotes, "metadata.json");
                    if (File.Exists(fileName))
                    {
                        try
                        {
                            DriverTrainingService.recordingMetaData = JsonConvert.DeserializeObject<MetaData>(File.ReadAllText(fileName));
                            Console.WriteLine("Pace notes for this game / track / car combination already exists. This will be extended");
                            createNewMetaData = false;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Unable to load existing metadata - renaming to 'broken_" + fileName + "', " + e.Message);
                            File.Move(fileName, "broken_" + fileName);
                        }
                    }
                }
                if (createFolder)
                {
                    System.IO.Directory.CreateDirectory(folderPathForPaceNotes);
                } 
                if (createNewMetaData) 
                {
                    DriverTrainingService.recordingMetaData = new MetaData(gameEnum.ToString(), carClass.ToString(), trackName);
                }
                isRecordingPaceNotes = true;
            }
        }

        private static String getCarSpecificFolderPath(GameEnum gameEnum, String trackName, CarData.CarClassEnum carClass)
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "pace_notes", 
                makeValidForPathName(gameEnum.ToString()), makeValidForPathName(carClass.ToString()), makeValidForPathName(trackName));
        }

        private static String getAnyCarFolderPath(GameEnum gameEnum, String trackName)
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "pace_notes",
                makeValidForPathName(gameEnum.ToString()), makeValidForPathName(trackName));
        }

        public static void abortRecordingPaceNotes()
        {
            if (isRecordingSound)
            {
                stopRecordingMessage();
            }
            System.IO.Directory.Delete(folderPathForPaceNotes, true);
            isRecordingPaceNotes = false;
        }

        public static void completeRecordingPaceNotes()
        {
            if (isRecordingSound)
            {
                stopRecordingMessage();
            }
            if (isRecordingPaceNotes)
            {
                try
                {
                    File.WriteAllText(System.IO.Path.Combine(folderPathForPaceNotes, "metadata.json"), 
                        JsonConvert.SerializeObject(DriverTrainingService.recordingMetaData, Formatting.Indented));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to complete recording session : " + e.Message);
                }
            } 
            isRecordingPaceNotes = false;
        }

        public static void stopRecordingMessage()
        {
            if (DriverTrainingService.isRecordingSound)
            {
                try
                {
                    DriverTrainingService.waveSource.StopRecording();
                }
                catch (Exception e)
                {
                    Console.WriteLine("Failed to record a pace notes session sound " + e.Message);
                }
                DriverTrainingService.isRecordingSound = false;
            }
        }

        public static void startRecordingMessage(int distanceRoundTrack)
        {
            if (isRecordingPaceNotes)
            {
                if (DriverTrainingService.isRecordingSound)
                {
                    Console.WriteLine("Sound already being recorded");
                }
                else
                {
                    Boolean addMetaDataEntry = false;
                    MetaDataEntry entry = DriverTrainingService.recordingMetaData.getClosestEntryInRange(distanceRoundTrack, combineEntriesCloserThan);
                    if (entry == null)
                    {
                        addMetaDataEntry = true;
                        entry = new MetaDataEntry(distanceRoundTrack);
                    }
                    int recordingIndex = entry.recordingNames.Count;
                    String fileName = distanceRoundTrack + "_" + recordingIndex + ".wav";
                    String recordingName = DriverTrainingService.trackName + "_" + DriverTrainingService.carClass.ToString() + "_" + fileName;
                    try
                    {
                        lock (_lock) 
                        { 
                            DriverTrainingService.waveSource = new WaveInEvent();
                            DriverTrainingService.waveSource.WaveFormat = new WaveFormat(22050, 1);
                            DriverTrainingService.waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
                            DriverTrainingService.waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);
                            DriverTrainingService.waveFile = new WaveFileWriter(createFileName(fileName), waveSource.WaveFormat);
                            DriverTrainingService.waveSource.StartRecording();                            
                        }
                        entry.recordingNames.Add(recordingName);
                        entry.fileNames.Add(fileName);
                        if (addMetaDataEntry)
                        {
                            DriverTrainingService.recordingMetaData.entries.Add(entry);
                        }
                        DriverTrainingService.isRecordingSound = true;
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to create a pace notes sound " + e.Message);
                    }
                }
            }
        }

        private static String createFileName(String name)
        {
            return System.IO.Path.Combine(DriverTrainingService.folderPathForPaceNotes, name);
        }

        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            lock (_lock)
            {
                if (DriverTrainingService.waveFile != null)
                {
                    DriverTrainingService.waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                    DriverTrainingService.waveFile.Flush();
                }
            }
        }

        static void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
        {
            lock (_lock)
            {
                if (DriverTrainingService.waveSource != null)
                {
                    DriverTrainingService.waveSource.Dispose();
                    DriverTrainingService.waveSource = null;
                }

                if (DriverTrainingService.waveFile != null)
                {
                    DriverTrainingService.waveFile.Dispose();
                    DriverTrainingService.waveFile = null;
                }
            }
        }

        // replaces reserved characters so we can use this string in a path name
        private static String makeValidForPathName(String text)
        {
            foreach (char c in System.IO.Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }
            return text;
        }
    }

    public class MetaData
    {
        public String description { get; set; }
        public String gameEnumName {get; set;}
        public String carClassName { get; set; }
        public String trackName { get; set; }
        public List<MetaDataEntry> entries { get; set; }

        public MetaData()
        {
            this.entries = new List<MetaDataEntry>();
            this.description = "";
        }

        public MetaData(String gameEnumName, String carClassName, String trackName)
        {
            this.gameEnumName = gameEnumName;
            this.carClassName = carClassName;
            this.trackName = trackName;
            this.description = "";
            this.entries = new List<MetaDataEntry>();
        }

        public MetaDataEntry getClosestEntryInRange(int distanceRoundTrack, int range)
        {
            int closestDifference = int.MaxValue;
            MetaDataEntry closestEntry = null;
            foreach (MetaDataEntry entry in entries)
            {
                // TODO: include track length in this calculation
                int difference = Math.Abs(entry.distanceRoundTrack - distanceRoundTrack);
                if (difference < closestDifference)
                {
                    closestDifference = difference;
                    closestEntry = entry;
                }
            }
            if (closestDifference <= range)
            {
                if (closestEntry.description != null && !closestEntry.description.Equals(""))
                {
                    Console.WriteLine("Adding this recording to existing entry " + closestEntry.description + " at distance " + closestEntry.distanceRoundTrack);
                }
                else
                {
                    Console.WriteLine("Adding this recording to existing entry at distance " + closestEntry.distanceRoundTrack);
                }
                return closestEntry;
            }
            else
            {
                return null;
            }
        }
    }

    public class MetaDataEntry
    {
        public String description { get; set; }
        public int distanceRoundTrack { get; set; }
        public List<String> recordingNames { get; set; }
        public List<String> fileNames { get; set; }

        public MetaDataEntry()
        {
            this.recordingNames = new List<string>();
            this.fileNames = new List<string>();
            this.description = "";
        }

        public MetaDataEntry(int distanceRoundTrack)
        {
            this.distanceRoundTrack = distanceRoundTrack;
            this.description = "";
            this.recordingNames = new List<string>();
            this.fileNames = new List<string>();
        }

        public String getRandomRecordingName()
        {
            int index = Utilities.random.Next(recordingNames.Count);
            return recordingNames[index];
        }
    }
}
