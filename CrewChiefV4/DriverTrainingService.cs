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
        public static Boolean isPlayingSession = false;
        public static Boolean isRecordingSession = false;
        private static Boolean isRecordingSound = false;
        private static MetaData recordingMetaData;
        private static WaveInEvent waveSource = null;
        private static WaveFileWriter waveFile = null;

        private static GameEnum gameEnum;
        private static String trackName;
        private static CarData.CarClassEnum carClass;

        private static String folderPathForSession;

        public static void loadTrainingSession(GameEnum gameEnum, String trackName, CarData.CarClassEnum carClass)
        {
            if (!isRecordingSession && !isPlayingSession)
            {
                Console.WriteLine("Playing a training session for circuit " + trackName + " with car class " + carClass.ToString());

                isRecordingSession = false;
                isRecordingSound = false;
                DriverTrainingService.folderPathForSession = getFolderPathForSession(gameEnum, trackName, carClass);
                if (Directory.Exists(DriverTrainingService.folderPathForSession))
                {
                    String fileName = System.IO.Path.Combine(folderPathForSession, "metadata.json");
                    if (File.Exists(fileName))
                    {
                        DriverTrainingService.recordingMetaData = JsonConvert.DeserializeObject<MetaData>(File.ReadAllText(System.IO.Path.Combine(folderPathForSession, "metadata.json")));
                        foreach (MetaDataEntry entry in DriverTrainingService.recordingMetaData.entries)
                        {
                            SoundCache.loadSingleSound(entry.recordingName, System.IO.Path.Combine(DriverTrainingService.folderPathForSession, entry.fileName));
                        }
                        isPlayingSession = true;
                    }
                    else
                    {
                        Console.WriteLine("No metadata.json file exists in the training session folder " + DriverTrainingService.folderPathForSession);
                    }
                }
                else
                {
                    Console.WriteLine("Unable to find a training session with path " + DriverTrainingService.folderPathForSession);
                }
            }
        }

        public static void stopPlayingTrainingSession()
        {
            isPlayingSession = false;
        }

        public static void stopRecordingTrainingSession()
        {
            isRecordingSession = false;
        }

        public static void checkDistanceAndPlayIfNeeded(float previousDistanceRoundTrack, float currentDistanceRoundTrack, AudioPlayer audioPlayer)
        {
            if (isPlayingSession && !isRecordingSession && DriverTrainingService.recordingMetaData != null)
            {
                foreach (MetaDataEntry entry in DriverTrainingService.recordingMetaData.entries)
                {
                    if (previousDistanceRoundTrack < entry.distanceRoundTrack && currentDistanceRoundTrack > entry.distanceRoundTrack)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage(entry.recordingName, 0, null));
                    }
                }
            }
        }

        public static void startRecordingSession(GameEnum gameEnum, String trackName, CarData.CarClassEnum carClass)
        {
            if (!isPlayingSession && !isRecordingSession)
            {
                Console.WriteLine("Recording a training session for circuit " + trackName + " with car class " + carClass.ToString());
                isRecordingSession = true;
                DriverTrainingService.gameEnum = gameEnum;
                DriverTrainingService.trackName = trackName;
                DriverTrainingService.carClass = carClass;
                DriverTrainingService.recordingMetaData = new MetaData(gameEnum.ToString(), carClass.ToString(), trackName);
                DriverTrainingService.folderPathForSession = getFolderPathForSession(gameEnum, trackName, carClass);
                if (!System.IO.Directory.Exists(folderPathForSession))
                {
                    System.IO.Directory.CreateDirectory(folderPathForSession);
                }
            }
        }

        private static String getFolderPathForSession(GameEnum gameEnum, String trackName, CarData.CarClassEnum carClass)
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "training_sounds", gameEnum.ToString(), trackName, carClass.ToString());
        }

        public static void abortRecordingSession()
        {
            if (isRecordingSound)
            {
                stopRecordingMessage();
            }
            System.IO.Directory.Delete(folderPathForSession, true);
            isRecordingSession = false;
        }

        public static void completeRecordingSession()
        {
            if (isRecordingSound)
            {
                stopRecordingMessage();
            }
            if (isRecordingSession)
            {
                File.WriteAllText(System.IO.Path.Combine(folderPathForSession, "metadata.json"), JsonConvert.SerializeObject(DriverTrainingService.recordingMetaData));
                isRecordingSession = false;
            }
        }

        public static void stopRecordingMessage()
        {
            if (DriverTrainingService.isRecordingSound)
            {
                DriverTrainingService.waveSource.StopRecording();
                DriverTrainingService.isRecordingSound = false;
            }
        }

        public static void startRecordingMessage(int distanceRoundTrack)
        {
            if (isRecordingSession)
            {
                if (DriverTrainingService.isRecordingSound)
                {
                    Console.WriteLine("bugger off i'm busy");
                }
                else
                {
                    DriverTrainingService.isRecordingSound = true;
                    String fileName = distanceRoundTrack + ".wav";
                    String recordingName = DriverTrainingService.trackName + "_" + DriverTrainingService.carClass.ToString() + "_" + fileName;
                    DriverTrainingService.recordingMetaData.entries.Add(new MetaDataEntry(distanceRoundTrack, recordingName, fileName));
                    DriverTrainingService.isRecordingSound = true;
                    DriverTrainingService.waveSource = new WaveInEvent();
                    DriverTrainingService.waveSource.WaveFormat = new WaveFormat(22050, 1);
                    DriverTrainingService.waveSource.DataAvailable += new EventHandler<WaveInEventArgs>(waveSource_DataAvailable);
                    DriverTrainingService.waveSource.RecordingStopped += new EventHandler<StoppedEventArgs>(waveSource_RecordingStopped);
                    DriverTrainingService.waveFile = new WaveFileWriter(createFileName(fileName), waveSource.WaveFormat);
                    DriverTrainingService.waveSource.StartRecording();
                }
            }
        }

        private static String createFileName(String name)
        {
            return System.IO.Path.Combine(DriverTrainingService.folderPathForSession, name);
        }

        static void waveSource_DataAvailable(object sender, WaveInEventArgs e)
        {
            if (DriverTrainingService.waveFile != null)
            {
                DriverTrainingService.waveFile.Write(e.Buffer, 0, e.BytesRecorded);
                DriverTrainingService.waveFile.Flush();
            }
        }

        static void waveSource_RecordingStopped(object sender, StoppedEventArgs e)
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

    public class MetaData
    {
        public String gameEnumName {get; set;}
        public String carClassName { get; set; }
        public String trackName { get; set; }
        public List<MetaDataEntry> entries { get; set; }
        public MetaData()
        {
            this.entries = new List<MetaDataEntry>();
        }
        public MetaData(String gameEnumName, String carClassName, String trackName)
        {
            this.gameEnumName = gameEnumName;
            this.carClassName = carClassName;
            this.trackName = trackName;
            this.entries = new List<MetaDataEntry>();
        }
    }

    public class MetaDataEntry
    {
        public int distanceRoundTrack { get; set; }
        public String recordingName { get; set; }
        public String fileName { get; set; }
        public MetaDataEntry()
        {

        }
        public MetaDataEntry(int distanceRoundTrack, String recordingName, String fileName)
        {
            this.distanceRoundTrack = distanceRoundTrack;
            this.recordingName = recordingName;
            this.fileName = fileName;
        }
    }
}
