using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    public class TrackData
    {
        // any track over 3000 metres will use gap points - 4 of them for tracks around 3000 metres long
        public static float gapPointsThreshold = 3000f;
        public static float gapPointSpacing = 700f;

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

        public static TrackDefinition getTrackDefinition(String trackName, float trackLength)
        {
            if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT || CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK) 
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
                String nameToLog = trackName != null ? trackName : "null";
                return new TrackDefinition("unknown track - name " + nameToLog + ", length = " + trackLength, trackLength); 
            }
            else
            {
                return new TrackDefinition("R3E track, length = " + trackLength, trackLength); 
            }
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
        public String name;
        public float trackLength;
        public Boolean hasPitLane;
        public float[] pitEntryPoint = new float[] { 0, 0 };
        public float[] pitExitPoint = new float[] { 0, 0 };
        public float[] gapPoints = new float[] { };
        public float pitEntryExitPointsDiameter = 3;   // if we're within this many metres of the pit entry point, we're entering the pit

        public TrackDefinition(String name, float pitEntryExitPointsDiameter, float trackLength, float[] pitEntryPoint, float[] pitExitPoint)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.hasPitLane = true;
            this.pitEntryPoint = pitEntryPoint;
            this.pitExitPoint = pitExitPoint;
            this.pitEntryExitPointsDiameter = pitEntryExitPointsDiameter;
            setGapPoints();
        }

        public TrackDefinition(String name, float trackLength)
        {
            this.name = name;
            this.trackLength = trackLength;
            this.hasPitLane = false;
            setGapPoints();
        }

        private void setGapPoints()
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
    }
}
