using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    public class TrackLandMarksRecorder
    {
        public static Boolean isRecordingTrackLandmarks = false;
        private static TrackLandmarksForTrack currentRecording = null;
        private static TrackLandmarksData trackLandmarksDataRecording = null;
        private static Boolean hasStartedNewTrackLandmark = false;
        private static TrackLandmark currentLandmark = null;
        private static String folderPathForTrackLandmark;

        public static void startRecordingTrackLandmarks(GameEnum gameEnum, String trackName, int trackId)
        {
            isRecordingTrackLandmarks = true;
            folderPathForTrackLandmark = getLandMarkSpecificFolderPath(gameEnum, trackName);
            Boolean createDirectory = true;
            Boolean fileBroken = true;
            if (Directory.Exists(folderPathForTrackLandmark))
            {
                createDirectory = false;
                String fileName = Path.Combine(folderPathForTrackLandmark, "trackLandmarks.json");
                if(File.Exists(fileName))
                {
                    try
                    {
                        trackLandmarksDataRecording = JsonConvert.DeserializeObject<TrackLandmarksData>(File.ReadAllText(fileName));
                        currentRecording = trackLandmarksDataRecording.trackLandmarksData.FirstOrDefault();
                        fileBroken = false;
                        Console.WriteLine("Recording started found existing tracklandmarksdata - appending to" + fileName);
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Unable to load existing tracklandmarksdata - renaming to 'broken_" + fileName + "', " + e.Message);
                        File.Move(fileName, "broken_" + fileName);
                        fileBroken = true;
                    }
                }               
            }
            if (createDirectory || fileBroken)
            {
                if (createDirectory)
                {
                    Directory.CreateDirectory(folderPathForTrackLandmark);
                }                                
                trackLandmarksDataRecording = new TrackLandmarksData();
                currentRecording = new TrackLandmarksForTrack();
                switch (gameEnum)
                {
                    case GameEnum.ASSETTO_32BIT:
                    case GameEnum.ASSETTO_64BIT:
                        currentRecording.acTrackNames = new string[] { trackName };
                        break;
                    case GameEnum.PCARS_32BIT:
                    case GameEnum.PCARS_64BIT:
                    case GameEnum.PCARS2:
                    case GameEnum.PCARS_NETWORK:
                    case GameEnum.PCARS2_NETWORK:
                        currentRecording.pcarsTrackName = trackName;
                        break;
                    case GameEnum.RF1:
                        currentRecording.rf1TrackNames = new string[] { trackName };
                        break;
                    case GameEnum.RF2_64BIT:
                        currentRecording.rf2TrackNames = new string[] { trackName };
                        break;
                    case GameEnum.IRACING:
                        currentRecording.irTrackName = trackName;
                        break;
                    case GameEnum.RACE_ROOM:
                        currentRecording.raceroomLayoutId = trackId;
                        break;
                    default:
                        break;
                }
                Console.WriteLine("Recording track landmark data for game " + gameEnum + " and track " + trackName);
            }
            
        }
        private static List<TrackLandmark> removeOverlappingLandmarks( TrackLandmark landmark )
        {
            List<TrackLandmark> landmarksToKeep = new List<TrackLandmark>();
            foreach(var lm in currentRecording.trackLandmarks)
            {
                //starting inside another landmark
                if((landmark.distanceRoundLapStart >= lm.distanceRoundLapStart && landmark.distanceRoundLapStart <= lm.distanceRoundLapEnd) ||
                    //wraps around another landmark
                    (landmark.distanceRoundLapStart <= lm.distanceRoundLapStart && landmark.distanceRoundLapEnd >= lm.distanceRoundLapEnd) ||
                    //ends inside another landmark
                    (landmark.distanceRoundLapEnd >= lm.distanceRoundLapStart && landmark.distanceRoundLapEnd <= lm.distanceRoundLapEnd))
                {
                    Console.WriteLine("Found overlapping landmark at " + lm.landmarkName + " so removing it");
                    continue;
                }
                else
                {
                    landmarksToKeep.Add(lm);
                }
            }
            landmarksToKeep.Add(landmark);
            return landmarksToKeep;
        }
        public static void addLandmark(float distanceRoundTrack)
        {
            if (currentRecording == null || !isRecordingTrackLandmarks)
            {
                Console.WriteLine("Please start a track landmark recoding session before adding landmarks");
                return;
            }
            if(!hasStartedNewTrackLandmark)
            {
                Console.WriteLine("Setting landmark start point at " + distanceRoundTrack + "m");
                hasStartedNewTrackLandmark = true;
                currentLandmark = new TrackLandmark();
                currentLandmark.distanceRoundLapStart = distanceRoundTrack;
                currentLandmark.isCommonOvertakingSpot = false;              
            }
            else
            {
                Console.WriteLine("Setting landmark end point at " + distanceRoundTrack + "m");
                currentLandmark.distanceRoundLapEnd = distanceRoundTrack;
                currentRecording.trackLandmarks = removeOverlappingLandmarks(currentLandmark);
                hasStartedNewTrackLandmark = false;
            }
        }

        public static void completeRecordingTrackLandmarks()
        {
            if (isRecordingTrackLandmarks)
            {
                //re-order landmarks by distanceRoundLapStart                   
                int turn = 1;
                List<TrackLandmark> tml = currentRecording.trackLandmarks.OrderBy(e => e.distanceRoundLapStart).ToList();
                foreach (TrackLandmark lm in tml)
                {
                    if (lm.landmarkName == null || lm.landmarkName.Length == 0)
                    {
                        lm.landmarkName = "turn" + turn;
                    }
                    turn++;
                }
                currentRecording.trackLandmarks = tml;
                trackLandmarksDataRecording.trackLandmarksData.Clear();
                trackLandmarksDataRecording.trackLandmarksData.Add(currentRecording);
                try
                {
                    File.WriteAllText(Path.Combine(folderPathForTrackLandmark, "trackLandmarks.json"),
                        JsonConvert.SerializeObject(trackLandmarksDataRecording, Formatting.Indented));

                    Console.WriteLine("Done Recording track landmark data to " + Path.Combine(folderPathForTrackLandmark, "trackLandmarks.json"));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Unable to complete landmark recording session : " + e.Message);
                }
            }
            isRecordingTrackLandmarks = false;
        }

        private static String getLandMarkSpecificFolderPath(GameEnum gameEnum, String trackName)
        {
            return Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "track_landmarks",
                makeValidForPathName(gameEnum.ToString()), makeValidForPathName(trackName));
        }

        private static String makeValidForPathName(String text)
        {
            foreach (char c in Path.GetInvalidFileNameChars())
            {
                text = text.Replace(c, '_');
            }
            return text;
        }
    }

    public class TrackLandmark
    {
        public String landmarkName { get; set; }
        public float distanceRoundLapStart { get; set; }
        public float distanceRoundLapEnd { get; set; }
        public Boolean isCommonOvertakingSpot { get; set; }        
        public float midPoint = -1;

        public float getMidPoint()
        {
            if (midPoint == -1)
            {
                midPoint = distanceRoundLapStart + ((distanceRoundLapEnd - distanceRoundLapStart) / 2);
            }
            return midPoint;
        }
    }

    public class TrackLandmarksForTrack
    {
        public String[] rf1TrackNames { get; set; }
        public String[] rf2TrackNames { get; set; }        
        public String[] acTrackNames { get; set; }
        public String irTrackName { get; set; }
        public String pcarsTrackName { get; set; }
        public int raceroomLayoutId { get; set; }
        public float approximateTrackLength { get; set; }   // this is optional and used to differentiate duplicated names
        public List<TrackLandmark> trackLandmarks { get; set; }
        public Boolean isOval { get; set; }
        public TrackLandmarksForTrack()
        {
            this.trackLandmarks = new List<TrackLandmark>();
            this.raceroomLayoutId = -1;
            this.approximateTrackLength = -1;
            this.acTrackNames = new string[] { };
            this.rf1TrackNames = new string[] { };
            this.rf2TrackNames = new string[] { };
            this.pcarsTrackName = "";
            this.irTrackName = "";
            this.isOval = false;
        }
    }

    public class TrackDataContainer
    {
        public List<TrackLandmark> trackLandmarks;
        public Boolean isOval = false;
        public TrackDataContainer(List<TrackLandmark> trackLandmarks, Boolean isOval)
        {
            this.trackLandmarks = trackLandmarks;
            this.isOval = isOval;
        }
    }

    public class TrackLandmarksData
    {
        public List<TrackLandmarksForTrack> trackLandmarksData { get; set; }
        public TrackLandmarksData()
        {
            this.trackLandmarksData = new List<TrackLandmarksForTrack>();
        }

        public Boolean checkForAndMatchOnLength(float lengthFromGame, float approximateLengthFromJSON)
        {
            if (lengthFromGame == -1 || approximateLengthFromJSON == -1)
            {
                return true;
            }
            else
            {
                // if an approximate length is specified in the JSON, check that the length from the game is close to this
                // pretty lenient here as it has to work for multiple games each with their own inaccuracies.
                return Math.Abs(lengthFromGame - approximateLengthFromJSON) < 200;
            }
        }

        public TrackDataContainer getTrackDataForTrackName(String trackName, float lengthFromGame)
        {
            foreach (TrackLandmarksForTrack trackLandmarksForTrack in trackLandmarksData)
            {
                switch (CrewChief.gameDefinition.gameEnum)
                {
                    case GameEnum.ASSETTO_32BIT:
                    case GameEnum.ASSETTO_64BIT:
                        if (trackLandmarksForTrack.acTrackNames != null)
                        {
                            foreach (String acTrackName in trackLandmarksForTrack.acTrackNames)
                            {
                                if (String.Equals(acTrackName, trackName, StringComparison.OrdinalIgnoreCase)
                                    && checkForAndMatchOnLength(lengthFromGame, trackLandmarksForTrack.approximateTrackLength))
                                {
                                    Console.WriteLine(trackLandmarksForTrack.trackLandmarks.Count + " landmarks defined for this track");
                                    return new TrackDataContainer(trackLandmarksForTrack.trackLandmarks, trackLandmarksForTrack.isOval);
                                }
                            }
                        }
                        break;
                    case GameEnum.PCARS_32BIT:
                    case GameEnum.PCARS_64BIT:
                    case GameEnum.PCARS2:
                    case GameEnum.PCARS_NETWORK:
                    case GameEnum.PCARS2_NETWORK:
                        if (String.Equals(trackLandmarksForTrack.pcarsTrackName, trackName, StringComparison.OrdinalIgnoreCase)
                            && checkForAndMatchOnLength(lengthFromGame, trackLandmarksForTrack.approximateTrackLength))
                        {
                            Console.WriteLine(trackLandmarksForTrack.trackLandmarks.Count + " landmarks defined for this track");
                            return new TrackDataContainer(trackLandmarksForTrack.trackLandmarks, trackLandmarksForTrack.isOval);
                        }
                        break;
                    case GameEnum.RF1:
                        if (trackLandmarksForTrack.rf1TrackNames != null)
                        {
                            foreach (String rf1TrackName in trackLandmarksForTrack.rf1TrackNames)
                            {
                                if (String.Equals(rf1TrackName, trackName, StringComparison.OrdinalIgnoreCase)
                                    && checkForAndMatchOnLength(lengthFromGame, trackLandmarksForTrack.approximateTrackLength))
                                {
                                    Console.WriteLine(trackLandmarksForTrack.trackLandmarks.Count + " landmarks defined for this track");
                                    return new TrackDataContainer(trackLandmarksForTrack.trackLandmarks, trackLandmarksForTrack.isOval);
                                }
                            }
                        }
                        break;
                    case GameEnum.RF2_64BIT:
                        if (trackLandmarksForTrack.rf2TrackNames != null)
                        {
                            foreach (String rf2TrackName in trackLandmarksForTrack.rf2TrackNames)
                            {
                                if (String.Equals(rf2TrackName, trackName, StringComparison.OrdinalIgnoreCase)
                                    && checkForAndMatchOnLength(lengthFromGame, trackLandmarksForTrack.approximateTrackLength))
                                {
                                    Console.WriteLine(trackLandmarksForTrack.trackLandmarks.Count + " landmarks defined for this track");
                                    return new TrackDataContainer(trackLandmarksForTrack.trackLandmarks, trackLandmarksForTrack.isOval);
                                }
                            }
                        }
                        break;
                    case GameEnum.IRACING:
                        if(trackLandmarksForTrack.irTrackName != null)
                        {
                            if (String.Equals(trackLandmarksForTrack.irTrackName, trackName, StringComparison.OrdinalIgnoreCase)
                                && checkForAndMatchOnLength(lengthFromGame, trackLandmarksForTrack.approximateTrackLength))
                            {
                                Console.WriteLine(trackLandmarksForTrack.trackLandmarks.Count + " landmarks defined for this track");
                                return new TrackDataContainer(trackLandmarksForTrack.trackLandmarks, trackLandmarksForTrack.isOval);
                            }
                        }
                        break;
                    default:
                        break;
                }
            }
            Console.WriteLine("No landmarks defined for this track");
            return new TrackDataContainer(new List<TrackLandmark>(), false);
        }

        public TrackDataContainer getTrackLandmarksForTrackLayoutId(int raceroomLayoutId)
        {
            foreach (TrackLandmarksForTrack trackLandmarksForTrack in trackLandmarksData)
            {
                if (trackLandmarksForTrack.raceroomLayoutId == raceroomLayoutId)
                {
                    Console.WriteLine(trackLandmarksForTrack.trackLandmarks.Count + " landmarks defined for this track");
                    return new TrackDataContainer(trackLandmarksForTrack.trackLandmarks, trackLandmarksForTrack.isOval);
                }
            }
            return new TrackDataContainer(new List<TrackLandmark>(), false);
        }

        public static TrackLandmarksData getTrackLandmarksDataFromFile(String filename)
        {
            if (filename != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<TrackLandmarksData>(getFileContents(filename));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error pasing " + filename + ": " + e.Message);
                }
            }
            return new TrackLandmarksData();
        }

        private static String getFileContents(String fullFilePath)
        {
            StringBuilder jsonString = new StringBuilder();
            StreamReader file = null;
            try
            {
                file = new StreamReader(fullFilePath);
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    if (!line.Trim().StartsWith("#"))
                    {
                        jsonString.AppendLine(line);
                    }
                }
                return jsonString.ToString();
            }
            catch (Exception e)
            {
                Console.WriteLine("Error reading file " + fullFilePath + ": " + e.Message);
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
            return null;
        }
    }

    public class TrackData
    {
        // any track over 3500 metres will use gap points
        public static float gapPointsThreshold = 3300f;
        public static float gapPointSpacing = 780f;

        public static TrackLandmarksData TRACK_LANDMARKS_DATA;

        // very long track = anything more than 20000 metres, long = anything more than 10000 metres, short track = anything less than 1 mile (1600 metres),
        // medium = everything else 
        // This allows a quick sanity check of some messages without having to fart about with track length. Expected use (to be done...)
        // is when checking how many laps to wait for enabling some messages like gaps and laps
        public enum TrackLengthClass { SHORT, MEDIUM, LONG, VERY_LONG }

        public static List<TrackDefinition> pCarsTracks = new List<TrackDefinition>()
        {
            new TrackDefinition("Autodromo Nazionale Monza:Grand Prix", 3, 5782.521f, new float[] {22.20312f, -437.1672f}, new float[] {63.60915f, -1.117797f}),
            new TrackDefinition("Autodromo Nazionale Monza:Short", 3, 2422.831f, new float[] {22.20312f, -437.1672f}, new float[] {63.60915f, -1.117797f}),
            new TrackDefinition("Azure Circuit:Grand Prix", 2f, 3325.762f, new float[] {-203.8109f, 613.3162f}, new float[] {105.2057f, 525.9147f}),
            new TrackDefinition("Bathurst:", 2, 6238.047f, new float[] {80.84997f, 7.21405f}, new float[] {-368.7227f, 12.93535f}),
            new TrackDefinition("Brands Hatch:Indy", 2.5f, 1924.475f, new float[] {-329.1295f, 165.8752f}, new float[] {-36.68332f, 355.611f}),
            new TrackDefinition("Brands Hatch:Grand Prix", 2.5f, 3890.407f, new float[] {-329.1295f, 165.8752f}, new float[] {-36.68332f, 355.611f}),
            new TrackDefinition("Brno:", 3, 5377.732f, new float[] {-194.1228f, -11.41852f}, new float[] {139.6739f, 0.06169825f}),
            new TrackDefinition("Cadwell:Club Circuit", 2346.271f),
            new TrackDefinition("Cadwell:Woodland", 2, 1335.426f, new float[] {45.92422f, 72.04858f}, new float[] {-10.31487f, -40.43255f}),
            new TrackDefinition("Cadwell:Grand Prix", 2, 3453.817f, new float[] {45.92422f, 72.04858f}, new float[] {-10.31487f, -40.43255f}),
            new TrackDefinition("Circuit de Barcelona-Catalunya:Club", 1691.681f),
            new TrackDefinition("Circuit de Barcelona-Catalunya:Grand Prix", 3, 4630.515f, new float[] {622.7108f, -137.3975f}, new float[] {26.52858f, -167.9301f}),
            new TrackDefinition("Circuit de Barcelona-Catalunya:National", 3, 2948.298f, new float[] {622.7108f, -137.3975f}, new float[] {26.52858f, -167.9301f}),
            new TrackDefinition("Circuit de Spa-Francorchamps:", 2.5f, 6997.816f, new float[] {-685.1871f, 1238.607f}, new float[] {-952.3125f, 1656.81f}),
            new TrackDefinition("Le Mans:Circuit des 24 Heures du Mans", 3, 13595.01f, new float[] {-737.9395f, 1107.367f}, new float[] {-721.3452f, 1582.873f}),
            new TrackDefinition("Dubai Autodrome:Club", 3, 2525.363f, new float[] {971.8023f, 199.1564f}, new float[] {452.5084f, 126.7626f}),
            new TrackDefinition("Dubai Autodrome:Grand Prix", 3, 5372.706f, new float[] {971.8023f, 199.1564f}, new float[] {452.5084f, 126.7626f}),
            new TrackDefinition("Dubai Autodrome:International", 3, 4290.958f, new float[] {971.8023f, 199.1564f}, new float[] {452.5084f, 126.7626f}),
            new TrackDefinition("Dubai Autodrome:National", 3, 3570.053f, new float[] {971.8023f, 199.1564f}, new float[] {452.5084f, 126.7626f}),
            new TrackDefinition("Donington Park:Grand Prix", 2, 3982.949f, new float[] {200.8843f, 144.8465f}, new float[] {486.4654f, 119.9713f}),
            new TrackDefinition("Donington Park:National", 2, 3175.802f, new float[] {200.8843f, 144.8465f}, new float[] {486.4654f, 119.9713f}),
            new TrackDefinition("Hockenheim:Grand Prix", 3, 4563.47f, new float[] {-483.1076f, -428.47f}, new float[] {-704.3397f, 11.15407f}),
            new TrackDefinition("Hockenheim:Short", 3, 2593.466f, new float[] {-483.1076f, -428.47f}, new float[] {-704.3397f, 11.15407f}),
            new TrackDefinition("Hockenheim:National", 3, 3685.798f, new float[] {-483.1076f, -428.47f}, new float[] {-704.3397f, 11.15407f}),
            new TrackDefinition("Imola:Grand Prix", 3, 4847.88f, new float[] {311.259f, 420.3269f}, new float[] {-272.6198f, 418.3795f}),
            new TrackDefinition("Le Mans:Le Circuit Bugatti", 3, 4149.839f, new float[] {-737.9395f, 1107.367f}, new float[] {-721.3452f, 1582.873f}),
            new TrackDefinition("Mazda Raceway:Laguna Seca", 2, 3593.582f, new float[] {-70.22401f, 432.3777f}, new float[] {-279.2681f, 228.165f}),
            new TrackDefinition("Nordschleife:Full", 3, 20735.4f,  new float[] {599.293f, 606.7135f}, new float[] {391.6694f, 694.4844f}),
            new TrackDefinition("Nürburgring:Grand Prix", 2.5f, 5122.845f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Nurburgring:GP", 2.5f, 5122.845f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Nürburgring:MuellenBach", 1488.941f),
            new TrackDefinition("Nürburgring:Sprint", 2.5f, 3603.18f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Nürburgring:Sprint Short", 2.5f, 3083.551f, new float[] {443.6332f, 527.8024f}, new float[] {66.84711f, 96.7378f}),
            new TrackDefinition("Oschersleben:C Circuit", 1061.989f),
            new TrackDefinition("Oschersleben:Grand Prix", 3, 3656.855f, new float[] {-350.7033f, 31.39084f}, new float[] {239.3137f, 91.73861f}),
            new TrackDefinition("Oschersleben:National", 3, 2417.65f, new float[] {-350.7033f, 31.39084f}, new float[] {239.3137f, 91.73861f}),
            new TrackDefinition("Oulton Park:Fosters", 2, 2649.91f, new float[] {46.9972f, 80.40176f}, new float[] {114.8132f, -165.5994f}),
            new TrackDefinition("Oulton Park:International", 2, 4302.739f, new float[] {46.9972f, 80.40176f}, new float[] {114.8132f, -165.5994f}),
            new TrackDefinition("Oulton Park:Island", 2, 3547.586f, new float[] {46.9972f, 80.40176f}, new float[] {114.8132f, -165.5994f}),
            new TrackDefinition("Road America:", 3.5f, 6482.489f, new float[] {430.8689f, 245.7329f}, new float[] {451.5659f, -330.7411f}),
            new TrackDefinition("Sakitto:Grand Prix", 2.5f, 5383.884f, new float[] {576.6671f, -142.1608f}, new float[] {607.291f, -646.9218f}),
            new TrackDefinition("Sakitto:International", 2, 2845.161f, new float[] {-265.1671f, 472.4344f}, new float[] {-154.9505f, 278.1627f}),
            new TrackDefinition("Sakitto:National", 2, 3100.48f, new float[] {-265.1671f, 472.4344f}, new float[] {-154.9505f, 278.1627f}),
            new TrackDefinition("Sakitto:Sprint", 2.5f, 3539.384f, new float[] {576.6671f -142.1608f}, new float[] {607.291f, -646.9218f}),
            new TrackDefinition("Silverstone:Grand Prix", 3, 5809.965f, new float[] {-504.739f, -1274.686f}, new float[] {-273.1427f, -861.1436f}),
            new TrackDefinition("Silverstone:International", 3, 2978.349f, new float[] {-504.739f, -1274.686f}, new float[] {-273.1427f, -861.1436f}),
            new TrackDefinition("Silverstone:National", 2, 2620.891f, new float[] {-323.1119f, -115.6939f},new float[] {157.4515f, 0.4208831f}),
            new TrackDefinition("Silverstone:Stowe", 2, 1712.277f, new float[] {-75.90499f, -1396.183f}, new float[] {-0.5095776f, -1096.397f}),
            new TrackDefinition("Snetterton:100 Circuit", 1544.868f),
            new TrackDefinition("Snetterton:200 Circuit", 2, 3170.307f, new float[] {228.4838f, -25.23679f}, new float[] {-44.5122f, -55.82156f}),
            new TrackDefinition("Snetterton:300 Circuit", 2, 4747.875f, new float[] {228.4838f, -25.23679f}, new float[] {-44.5122f, -55.82156f}),
            new TrackDefinition("Sonoma Raceway:Grand Prix", 2, 4025.978f, new float[] {-592.7792f, 87.43731f}, new float[] {-152.6224f, -30.71386f}),
            new TrackDefinition("Sonoma Raceway:National", 2, 3792.239f, new float[] {-592.7792f, 87.43731f}, new float[] {-152.6224f, -30.71386f}),
            new TrackDefinition("Sonoma Raceway:Short", 2, 3174.824f, new float[] {-592.7792f, 87.43731f}, new float[] {-152.6224f, -30.71386f}),
            new TrackDefinition("Watkins Glen International:Grand Prix", 3, 5112.122f, new float[] {589.6273f, -928.2814f}, new float[] {542.0042f, -1410.464f}),
            new TrackDefinition("Watkins Glen International:Short Circuit", 3, 3676.561f, new float[] {589.6273f, -928.2814f}, new float[] {542.0042f, -1410.464f}),
            new TrackDefinition("Willow Springs:Short Circuit", 3, 1627.787f, new float[] {-386.1919f, 818.131f}, new float[] {-317.1366f, 641.947f}),
            new TrackDefinition("Willow Springs:International Raceway", 3, 4038.008f, new float[] {319.4275f, -21.51243f}, new float[] {-44.84023f -23.41344f}),
            new TrackDefinition("Zhuhai:International Circuit", 3, 4293.098f, new float[] {-193.7068f, 123.679f}, new float[] {64.56277f, -71.51254f}),
            new TrackDefinition("Zolder:Grand Prix", 4, 4146.733f, new float[] {138.3811f, 132.7747f}, new float[] {682.2009f, 179.8147f}),

            new TrackDefinition("Bannochbrae:Road Circuit", 4, 7251.9004f, new float[]{-175f, 16f}, new float[]{131f, 14.5f}),
            new TrackDefinition("Rouen Les Essarts:", 4, 6499.198f, new float[]{117.25f, 25.5f}, new float[]{-84.75f, -13.5f}),
            new TrackDefinition("Rouen Les Essarts:Short", 4, 5390.12939f, new float[]{117.25f, 25.5f}, new float[]{-84.75f, -13.5f}),
            // this is the classic version. Which has *exactly* the same name as the non-classic version. Not cool.
            new TrackDefinition("Silverstone:Grand Prix", 3, 4697.72656f, new float[] {-347f, -165f}, new float[] {152.75f, -1.25f}),
            new TrackDefinition("Hockenheim:Classic", 3, 6763.425f, new float[] {-533f, -318.25f}, new float[] {-705.5f, -2f})
        };

        public static List<TrackDefinition> assettoTracks = new List<TrackDefinition>()
        {
            new TrackDefinition("spa:", 6946.062f, 3, new float[] {2265f, 5036f}),
            new TrackDefinition("ks_barcelona:layout_gp", 4591.887f, 3, new float[] {1643f, 3383f}),
            new TrackDefinition("ks_barcelona:layout_moto", 4683.986f, 3, new float[] {1643f, 3376f}),
            new TrackDefinition("acu_bathurst:", 6215.709f, 3, new float[] {2233f, 4262}),
            new TrackDefinition("ks_black_cat_county:layout_int", 4683.986f, 3, new float[] {1594f, 3781f}),
            new TrackDefinition("ks_black_cat_county:layout_long", 11176.96f, 3, new float[] {3618f, 7162f}),
            new TrackDefinition("ks_black_cat_county:layout_short", 6453.315f, 3, new float[] {2778f, 4813f}),
            new TrackDefinition("ks_brands_hatch:gp", 3887.807f, 3, new float[] {1082f, 2196f}),
            new TrackDefinition("ks_brands_hatch:indy", 1915.786f, 2, new float[] {1083f, 0f}),
            new TrackDefinition("circuit_de_la_sarthe:24hours", 13613.44f, 3, new float[] {4535f, 9090f}),
            new TrackDefinition("circuit_de_la_sarthe:nochicane", 13565.71f, 3, new float[] {4515f, 9036f}),
            new TrackDefinition("doningtonpark:gp", 3904.841f, 3, new float[] {1276f, 2402f}),
            new TrackDefinition("doningtonpark:national", 3126.198f, 3, new float[] {1274f, 2402f}),
            new TrackDefinition("imola:", 4864.107f, 3, new float[] {1533f, 3291f}),
            new TrackDefinition("magione:", 2455.794f, 2, new float[] {1291f, 0f}),
            new TrackDefinition("monza:", 5758.661f, 3, new float[] {2122f, 3879f}),
            new TrackDefinition("ks_monza66:full", 10019.83f, 3, new float[] {3213f, 5310f}),
            new TrackDefinition("ks_monza66:junior", 2395.745f, 3, new float[] {742f, 1532f}),
            new TrackDefinition("ks_monza66:road", 5758.69f, 3, new float[] {1989f, 3843f}),
            new TrackDefinition("mugello:", 5197.331f, 3, new float[] {1299f, 2889f}),
            new TrackDefinition("ks_nordschleife:endurance", 25358.28f, 3, new float[] {9690f, 16469f}),
            new TrackDefinition("ks_nordschleife:endurance_cup", 24315.77f, 3, new float[] {8659f, 15435f}),
            new TrackDefinition("ks_nordschleife:nordschleife", 20801.37f, 3, new float[] {5424f, 12200f}),
            new TrackDefinition("ks_nurburgring:layout_gp_a", 5077.904f, 3, new float[] {1454f, 3274f}),
            new TrackDefinition("ks_nurburgring:layout_sprint_a", 3566.685f, 2, new float[] {1609f, 0f}),
            new TrackDefinition("ks_nurburgring:layout_sprint_b", 3559.989f, 2, new float[] {1611f, 0f}),
            new TrackDefinition("ks_red_bull_ring:layout_gp", 4286.527f, 3, new float[] {1100f, 2701f}),
            new TrackDefinition("ks_red_bull_ring:layout_national", 2317.448f, 2, new float[] {1206f, 0f}),
            new TrackDefinition("ks_silverstone:gp", 5803.997f, 3, new float[] {1502f, 4152}),
            new TrackDefinition("ks_silverstone:international", 2941.82f, 2, new float[] {1290f, 0f}),
            new TrackDefinition("ks_silverstone:national", 2600.262f, 2, new float[] {1422f, 0f}),
            new TrackDefinition("tor_poznanl:laser", 4029.902f, 3, new float[] {1563f, 2919f}),
            new TrackDefinition("trento-bondone:", 17183.77f, 1, new float[] {0f, 0f}),
            new TrackDefinition("ks_vallelunga:classic_circuit", 3180.26f, 3, new float[] {1235f, 2197f}),
            new TrackDefinition("ks_vallelunga:club_circuit", 1720.172f, 2, new float[] {734f, 0f}),
            new TrackDefinition("ks_vallelunga:extended_circuit", 4030.934f, 3, new float[] {1749f, 3044f}),
            new TrackDefinition("vir:full course", 5221.266f, 3, new float[] {1608f, 3777f}),
            new TrackDefinition("vir:grand east course", 6383.806f, 3, new float[] {2152f, 4229f}),
            new TrackDefinition("vir:grand west course", 6564.385f, 3, new float[] {3061f, 4877f}),
            new TrackDefinition("vir:north course", 3614.832f, 3, new float[] {1599f, 2371f}),
            new TrackDefinition("vir:patriot course", 1535.824f, 2, new float[] {837f, 0f}),
            new TrackDefinition("vir:patriot reverse course", 1543.93f, 2, new float[] {704f, 3376f}),
            new TrackDefinition("vir:south course", 2586.035f, 2, new float[] {1370f, 0f}),
            new TrackDefinition("ks_zandvoort:", 4189.691f, 2, new float[] {1986f, 0f}),
            new TrackDefinition("paul_ricard:gt_24h", 5776.583f, 3, new float[] {2971.514f, 4184.762f}),
            new TrackDefinition("paul_ricard:wtcc", 3804.753f, 3, new float[] {759.0595f, 2207.232f})
            
        };

        public static void loadTrackLandmarksData()
        {
            TRACK_LANDMARKS_DATA = TrackLandmarksData.getTrackLandmarksDataFromFile(getDefaultTrackLandmarksFileLocation());
            loadUserCreatedTrackLandmarkFiles();
        }
        static void loadUserCreatedTrackLandmarkFiles()
        {
            String userLandmarks = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "track_landmarks");
            if(Directory.Exists(userLandmarks))
            {
                ProcessDirectory(userLandmarks);
            }
            else
            {
                Console.WriteLine("No user defined landmarks found");
            }             
        }

        public static void ProcessDirectory(string targetDirectory)
        {
            string[] fileEntries = Directory.GetFiles(targetDirectory);
            
            foreach (string fileName in fileEntries)
            {
                TrackLandmarksData data = TrackLandmarksData.getTrackLandmarksDataFromFile(fileName);
                if(TRACK_LANDMARKS_DATA == null)
                {
                    TRACK_LANDMARKS_DATA = data;
                }
                else
                {
                    TRACK_LANDMARKS_DATA.trackLandmarksData.AddRange(data.trackLandmarksData);
                }
            }
            string[] subdirectoryEntries = Directory.GetDirectories(targetDirectory);
            foreach (string subdirectory in subdirectoryEntries)
            {
                ProcessDirectory(subdirectory);
            }                
        }

        public static String getLandmarkForLapDistance(TrackDefinition currentTrack, float lapDistance)
        {
            if (currentTrack != null && lapDistance > 0 && currentTrack.trackLandmarks != null) 
            {
                foreach (TrackLandmark trackLandmark in currentTrack.trackLandmarks)
                {
                    if (lapDistance > trackLandmark.distanceRoundLapStart && lapDistance < trackLandmark.distanceRoundLapEnd)
                    {
                        return trackLandmark.landmarkName;
                    }
                } 
                // check again with more generous limits
                foreach (TrackLandmark trackLandmark in currentTrack.trackLandmarks)
                {
                    if (lapDistance > (Math.Max(0, trackLandmark.distanceRoundLapStart - 70)) && 
                        lapDistance < Math.Min(currentTrack.trackLength, trackLandmark.distanceRoundLapEnd + 70))
                    {
                        return trackLandmark.landmarkName;
                    }
                }                
             
            }
            return null;
        }

        private static String getDefaultTrackLandmarksFileLocation()
        {
            String path = Configuration.getDefaultFileLocation("trackLandmarksData.json");
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }

        public static TrackDefinition getTrackDefinition(String trackName, int trackId, float trackLength)
        {
            if (CrewChief.isPCars())
            {
                List<TrackDefinition> defsWhichMatchName = new List<TrackDefinition>();
                foreach (TrackDefinition def in pCarsTracks)
                {
                    if (def.name == trackName)
                    {
                        defsWhichMatchName.Add(def);
                    }
                }
                if (defsWhichMatchName.Count == 1)
                {
                    return defsWhichMatchName[0];
                }
                TrackDefinition defGuessedFromLength = null;
                if (defsWhichMatchName.Count > 1)
                {
                    defGuessedFromLength = getDefinitionForLength(defsWhichMatchName, trackLength, 2);
                }
                else
                {
                    defGuessedFromLength = getDefinitionForLength(pCarsTracks, trackLength, 2);
                }
                if (defGuessedFromLength != null)
                {
                    return defGuessedFromLength;
                }
                String nameToUse = trackName != null ? trackName : "unknown track (no name)";
                return new TrackDefinition(nameToUse, trackLength);
            }
            else if (CrewChief.gameDefinition.gameEnum == GameEnum.RACE_ROOM)
            {
                return new TrackDefinition(trackName, trackId, trackLength);
            }
            else if (trackName != null)
            {
                return new TrackDefinition(trackName, trackLength);
            }
            else
            {
                return new TrackDefinition("unknown track , length = " + trackLength, trackLength);
            }
        }
        public static TrackDefinition getTrackDefinition(String trackName, float trackLength, int sectorsOnTrack)
        {
            List<TrackDefinition> defsWhichMatchName = new List<TrackDefinition>();
            foreach (TrackDefinition def in assettoTracks)
            {
                if (def.name.Equals(trackName))
                {
                    return def;
                }
            }
            TrackDefinition unknownTrackDef = new TrackDefinition(trackName, trackLength, sectorsOnTrack, new float[] { 0f, 0f });
            unknownTrackDef.unknownTrack = true;
            unknownTrackDef.setSectorPointsForUnknownTracks();
            return unknownTrackDef;
        }
        private static TrackDefinition getDefinitionForLength(List<TrackDefinition> possibleDefinitions, float trackLength, int maxError)
        {
            TrackDefinition closestLengthDef = null;
            float closestLengthDifference = float.MaxValue;
            foreach (TrackDefinition def in possibleDefinitions)
            {
                if (def.trackLength == trackLength)
                {
                    return def;
                }
                else
                {
                    float thisDiff = Math.Abs(trackLength - def.trackLength);
                    if (thisDiff < maxError)
                    {
                        if (closestLengthDef == null || thisDiff < closestLengthDifference)
                        {
                            closestLengthDef = def;
                            closestLengthDifference = thisDiff;
                        }
                    }
                }
            }
            return closestLengthDef;
        }
    }

    // *very* flakey approach here... the car must be inside a circle for one tick to trigger.
    // All kinds of issues with this.
    public class TrackDefinition
    {
        public int id;
        public String name;
        public float trackLength;
        public Boolean hasPitLane;
        public float[] pitEntryPoint = new float[] { 0, 0 };
        public float[] pitExitPoint = new float[] { 0, 0 };
        public float[] gapPoints = new float[] { };
        public float pitEntryExitPointsDiameter = 3;   // if we're within this many metres of the pit entry point, we're entering the pit
        public int sectorsOnTrack = 3;
        public float[] sectorPoints = new float[] { 0, 0 };
        public Boolean unknownTrack = false;
        public List<TrackLandmark> trackLandmarks = new List<TrackLandmark>();
        public Boolean isOval = false;
        // this is the distance round the track where we save the opponent's race position before he slows for pit entry
        public float distanceForNearPitEntryChecks;
        public TrackData.TrackLengthClass trackLengthClass = TrackData.TrackLengthClass.MEDIUM;

        public TrackDefinition(String name, float pitEntryExitPointsDiameter, float trackLength, float[] pitEntryPoint, float[] pitExitPoint)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.trackLengthClass = getLengthClass(trackLength);
            // hard-code this to 300metres before start line for now, but it needs to be overrideable on a per-track basis
            this.distanceForNearPitEntryChecks = trackLength - 300;
            this.hasPitLane = true;
            this.pitEntryPoint = pitEntryPoint;
            this.pitExitPoint = pitExitPoint;
            this.pitEntryExitPointsDiameter = pitEntryExitPointsDiameter;
        }

        public TrackDefinition(String name, float trackLength)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.trackLengthClass = getLengthClass(trackLength);
            // hard-code this to 300metres before start line for now, but it needs to be overrideable on a per-track basis
            this.distanceForNearPitEntryChecks = trackLength - 300;
            this.hasPitLane = false;
        }

        public TrackDefinition(String name, int id, float trackLength)
        {
            this.name = name;
            this.id = id;
            this.trackLength = trackLength;
            this.trackLengthClass = getLengthClass(trackLength);
            // hard-code this to 300metres before start line for now, but it needs to be overrideable on a per-track basis
            this.distanceForNearPitEntryChecks = trackLength - 300;
            this.hasPitLane = false;
        }

        public TrackDefinition(String name, float trackLength, int sectorsOnTrack, float[] sectorPoints)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.trackLengthClass = getLengthClass(trackLength);
            // hard-code this to 300metres before start line for now, but it needs to be overrideable on a per-track basis
            this.distanceForNearPitEntryChecks = trackLength - 300;
            this.hasPitLane = true;
            this.sectorsOnTrack = sectorsOnTrack;
            this.sectorPoints = sectorPoints;
        }

        private TrackData.TrackLengthClass getLengthClass(float length)
        {
            if (length > 20000)
            {
                return TrackData.TrackLengthClass.VERY_LONG;
            }
            if (length > 10000)
            {
                return TrackData.TrackLengthClass.LONG;
            }
            if (length < 1600)
            {
                return TrackData.TrackLengthClass.SHORT;
            }
            return TrackData.TrackLengthClass.MEDIUM;
        }

        public void setGapPoints()
        {
            if (trackLength > TrackData.gapPointsThreshold)
            {
                float totalGaps = 0;
                List<float> gaps = new List<float>();
                while (totalGaps < trackLength)
                {
                    totalGaps += TrackData.gapPointSpacing;
                    if (totalGaps < trackLength - TrackData.gapPointSpacing)
                    {
                        gaps.Add(totalGaps);
                    }
                    else
                    {
                        break;
                    }
                }
                // the gapPoints are used instead of the sector / start-finish line for opponent gaps.
                // We need to ensure our 1st gap point is near, but not on, the start-finish line (so the lap
                // distance between previous and current game states has increased). Yes. A hack.
                // This final gap point is just before the start-finish line
                gaps.Add(trackLength - 50);

                gapPoints = gaps.ToArray();
            }
        }

        public void setSectorPointsForUnknownTracks()
        {
            if (sectorsOnTrack == 0)
            {
                sectorsOnTrack = 1;
            }
            float sectorSpacing = trackLength / sectorsOnTrack;
            sectorPoints[0] = sectorSpacing;
            sectorPoints[1] = sectorSpacing * 2;
        }

        public Boolean isAtPitEntry(float x, float y)
        {
            return hasPitLane && Math.Abs(pitEntryPoint[0] - x) < pitEntryExitPointsDiameter &&
                Math.Abs(pitEntryPoint[1] - y) < pitEntryExitPointsDiameter;
        }

        public Boolean isAtPitExit(float x, float y)
        {
            return hasPitLane && Math.Abs(pitExitPoint[0] - x) < pitEntryExitPointsDiameter &&
                Math.Abs(pitExitPoint[1] - y) < pitEntryExitPointsDiameter;
        }

        public void setTrackLandmarks(List<TrackLandmark> trackLandmarks)
        {
            this.trackLandmarks = trackLandmarks;
        }
    }
}
