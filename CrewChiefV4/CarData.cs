using CrewChiefV4.Events;
using CrewChiefV4.GameState;
using CrewChiefV4.PCars;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace CrewChiefV4
{
    public class CarData
    {
        // some temperatures - maybe externalise these
        // These are the peaks. If the tyre exceeds these temps even for one tick over a lap, we'll warn about it. This is why they look so high
        private static float maxColdRoadTyreTempPeak = 65;
        private static float maxWarmRoadTyreTempPeak = 106;
        private static float maxHotRoadTyreTempPeak = 120;

        private static float maxColdUnknownRaceTyreTempPeak = 70;
        private static float maxWarmUnknownRaceTyreTempPeak = 117;
        private static float maxHotUnknownRaceTyreTempPeak = 137;

        private static float maxColdBiasPlyTyreTempPeak = 70;
        private static float maxWarmBiasPlyTyreTempPeak = 103;
        private static float maxHotBiasPlyTyreTempPeak = 123;
        
        // special case for RaceRoom tyres on the new tire model 
        // - the game sends the core temp, not the surface temp
        private static float maxColdR3ENewTyreTempPeak = 90;
        private static float maxWarmR3ENewTyreTempPeak = 99;
        private static float maxHotR3ENewTyreTempPeak = 104;

        private static float maxColdR3ENewPrimeTyreTempPeak = 88;
        private static float maxWarmR3ENewPrimeTyreTempPeak = 98;
        private static float maxHotR3ENewPrimeTyreTempPeak = 103;

        private static float maxColdR3ENewOptionTyreTempPeak = 75;
        private static float maxWarmR3ENewOptionTyreTempPeak = 95;
        private static float maxHotR3ENewOptionTyreTempPeak = 100;

        private static float maxColdIronRoadBrakeTemp = 80;
        private static float maxWarmIronRoadBrakeTemp = 500;
        private static float maxHotIronRoadBrakeTemp = 750;

        private static float maxColdIronRaceBrakeTemp = 150;
        private static float maxWarmIronRaceBrakeTemp = 700;
        private static float maxHotIronRaceBrakeTemp = 900;

        private static float maxColdCeramicBrakeTemp = 150;
        private static float maxWarmCeramicBrakeTemp = 950;
        private static float maxHotCeramicBrakeTemp = 1200;

        private static float maxColdCarbonBrakeTemp = 400;
        private static float maxWarmCarbonBrakeTemp = 1200;
        private static float maxHotCarbonBrakeTemp = 1500;

        public enum CarClassEnum
        {
            GT1X, GT1, GTE, GT2, GTC, GTLM, GT3, GT4, GT5, Kart_1, Kart_2, LMP1, LMP2, LMP3, ROAD_B, ROAD_C1, ROAD_C2, ROAD_D, ROAD_SUPERCAR, GROUPC, GROUPA, GROUP4, GROUP5, GROUP6, GTO,
            VINTAGE_INDY_65, VINTAGE_F3_A, VINTAGE_F1_A, VINTAGE_F1_A1, VINTAGE_GT3, VINTAGE_GT, HISTORIC_TOURING_1, HISTORIC_TOURING_2, VINTAGE_F1_B, VINTAGE_F1_C, STOCK_CAR,
            F1, F2, F3, F4, FF, TC1, TC2, TC1_2014, AUDI_TT_CUP, AUDI_TT_VLN, CLIO_CUP, DTM, DTM_2012, DTM_2013, V8_SUPERCAR, DTM_2014, DTM_2015, DTM_2016, TRANS_AM, HILL_CLIMB_ICONS, FORMULA_RENAULT,
            MEGANE_TROPHY, NSU_TT, KTM_RR, INDYCAR, HYPER_CAR, HYPER_CAR_RACE, UNKNOWN_RACE
        }

        // use different thresholds for newer R3E car classes:
        public static CarClassEnum[] r3eNewTyreModelClasses = new CarClassEnum[] {
            CarClassEnum.GT3, CarClassEnum.GT4, CarClassEnum.LMP1, CarClassEnum.LMP2, CarClassEnum.GROUP5, CarClassEnum.GROUP4, CarClassEnum.GTO, CarClassEnum.F2, CarClassEnum.F4,
            CarClassEnum.FF, CarClassEnum.TC1, CarClassEnum.AUDI_TT_CUP, CarClassEnum.DTM_2013, CarClassEnum.DTM_2014, CarClassEnum.DTM_2015, CarClassEnum.DTM_2016, CarClassEnum.NSU_TT,
            CarClassEnum.F3, CarClassEnum.AUDI_TT_VLN, CarClassEnum.KTM_RR, CarClassEnum.FF, CarClassEnum.FF, CarClassEnum.FF};

        public static Dictionary<TyreType, List<CornerData.EnumWithThresholds>> tyreTempThresholds = new Dictionary<TyreType, List<CornerData.EnumWithThresholds>>();
        public static Dictionary<BrakeType, List<CornerData.EnumWithThresholds>> brakeTempThresholds = new Dictionary<BrakeType, List<CornerData.EnumWithThresholds>>();

        static CarData()
        {
            List<CornerData.EnumWithThresholds> roadTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdRoadTyreTempPeak));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdRoadTyreTempPeak, maxWarmRoadTyreTempPeak));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmRoadTyreTempPeak, maxHotRoadTyreTempPeak));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotRoadTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Road, roadTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> unknownRaceTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdUnknownRaceTyreTempPeak));
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdUnknownRaceTyreTempPeak, maxWarmUnknownRaceTyreTempPeak));
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmUnknownRaceTyreTempPeak, maxHotUnknownRaceTyreTempPeak));
            unknownRaceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotUnknownRaceTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Unknown_Race, unknownRaceTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> biasPlyTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdBiasPlyTyreTempPeak));
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdBiasPlyTyreTempPeak, maxWarmBiasPlyTyreTempPeak));
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmBiasPlyTyreTempPeak, maxHotBiasPlyTyreTempPeak));
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotBiasPlyTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Bias_Ply, biasPlyTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3eNewTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3eNewTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3ENewTyreTempPeak));
            r3eNewTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3ENewTyreTempPeak, maxWarmR3ENewTyreTempPeak));
            r3eNewTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3ENewTyreTempPeak, maxHotR3ENewTyreTempPeak));
            r3eNewTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3ENewTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_NEW, r3eNewTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3eNewPrimeTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3eNewPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3ENewPrimeTyreTempPeak));
            r3eNewPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3ENewPrimeTyreTempPeak, maxWarmR3ENewPrimeTyreTempPeak));
            r3eNewPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3ENewPrimeTyreTempPeak, maxHotR3ENewPrimeTyreTempPeak));
            r3eNewPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3ENewPrimeTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_NEW_Prime, r3eNewPrimeTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3eNewOptionTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3eNewOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3ENewOptionTyreTempPeak));
            r3eNewOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3ENewOptionTyreTempPeak, maxWarmR3ENewOptionTyreTempPeak));
            r3eNewOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3ENewOptionTyreTempPeak, maxHotR3ENewOptionTyreTempPeak));
            r3eNewOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3ENewOptionTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_NEW_Option, r3eNewOptionTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> ironRoadBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COLD, -10000, maxColdIronRoadBrakeTemp));
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.WARM, maxColdIronRoadBrakeTemp, maxWarmIronRoadBrakeTemp));
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.HOT, maxWarmIronRoadBrakeTemp, maxHotIronRoadBrakeTemp));
            ironRoadBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COOKING, maxHotIronRoadBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Iron_Road, ironRoadBrakeTempsThresholds);

            List<CornerData.EnumWithThresholds> ironRaceBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COLD, -10000, maxColdIronRaceBrakeTemp));
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.WARM, maxColdIronRaceBrakeTemp, maxWarmIronRaceBrakeTemp));
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.HOT, maxWarmIronRaceBrakeTemp, maxHotIronRaceBrakeTemp));
            ironRaceBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COOKING, maxHotIronRaceBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Iron_Race, ironRaceBrakeTempsThresholds);

            List<CornerData.EnumWithThresholds> ceramicBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COLD, -10000, maxColdCeramicBrakeTemp));
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.WARM, maxColdCeramicBrakeTemp, maxWarmCeramicBrakeTemp));
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.HOT, maxWarmCeramicBrakeTemp, maxHotCeramicBrakeTemp));
            ceramicBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COOKING, maxHotCeramicBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Ceramic, ceramicBrakeTempsThresholds);

            List<CornerData.EnumWithThresholds> carbonBrakeTempsThresholds = new List<CornerData.EnumWithThresholds>();
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COLD, -10000, maxColdCarbonBrakeTemp));
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.WARM, maxColdCarbonBrakeTemp, maxWarmCarbonBrakeTemp));
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.HOT, maxWarmCarbonBrakeTemp, maxHotCarbonBrakeTemp));
            carbonBrakeTempsThresholds.Add(new CornerData.EnumWithThresholds(BrakeTemp.COOKING, maxHotCarbonBrakeTemp, 10000));
            brakeTempThresholds.Add(BrakeType.Carbon, carbonBrakeTempsThresholds);
        }



        private static CarClasses CAR_CLASSES;

        // used for PCars only, where we only know if the opponent is the same class as us or not
        public static CarClass DEFAULT_PCARS_OPPONENT_CLASS = new CarClass();

        private static Dictionary<string, CarClass> nameToCarClass;
        private static Dictionary<int, CarClass> intToCarClass;

        public class CarClass
        {
            [JsonConverter(typeof(StringEnumConverter))]
            public CarClassEnum carClassEnum { get; set; }
            public List<int> raceroomClassIds { get; set; }
            public List<string> pCarsClassNames { get; set; }
            public List<string> rf1ClassNames { get; set; }
            public List<string> rf2ClassNames { get; set; }
            public List<string> acClassNames { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public BrakeType brakeType { get; set; }
            [JsonConverter(typeof(StringEnumConverter))]
            public TyreType defaultTyreType { get; set; }
            public float maxSafeWaterTemp { get; set; }
            public float maxSafeOilTemp { get; set; }
            public float minTyreCircumference { get; set; }
            public float maxTyreCircumference { get; set; }

            public String placeholderClassId = "";

            public List<Regex> pCarsClassNamesRegexs = new List<Regex>();
            public List<Regex> rf1ClassNamesRegexs = new List<Regex>();
            public List<Regex> rf2ClassNamesRegexs = new List<Regex>();
            public List<Regex> acClassNamesRegexs = new List<Regex>();

            public CarClass()
            {
                // initialise with default values
                this.carClassEnum = CarClassEnum.UNKNOWN_RACE;
                this.raceroomClassIds = new List<int>();
                this.pCarsClassNames = new List<string>();
                this.rf1ClassNames = new List<string>();
                this.rf2ClassNames = new List<string>();
                this.acClassNames = new List<string>();
                this.brakeType = BrakeType.Iron_Race;
                this.defaultTyreType = TyreType.Unknown_Race;
                this.maxSafeWaterTemp = 105;
                this.maxSafeOilTemp = 125;
                this.minTyreCircumference = 0.4f * (float)Math.PI;
                this.maxTyreCircumference = 1.2f * (float)Math.PI;
            }

            public String getClassIdentifier()
            {
                if (this.carClassEnum == CarClassEnum.UNKNOWN_RACE)
                {
                    // this class has been generated from an unrecognised ID, so return the
                    // ID we used to generate this new class.
                    return placeholderClassId;
                }
                else
                {
                    return this.carClassEnum.ToString();
                }
            }

            public void setupRegexs()
            {
                setupRegexs(rf1ClassNames, rf1ClassNamesRegexs);
                setupRegexs(rf2ClassNames, rf2ClassNamesRegexs);
                setupRegexs(acClassNames, acClassNamesRegexs);
                setupRegexsForPCars(pCarsClassNames, pCarsClassNamesRegexs);
            }

            /**
             * Create regexs for classnames with wildcards.
             */
            private void setupRegexs(List<string> classNames, List<Regex> regexs)
            {
                foreach (String className in classNames)
                {
                    if (className.Count() > 0)
                    {
                        if (className.Contains("*") || className.Contains("?"))
                        {
                            String regexStr = wildcardToRegex(className);
                            regexs.Add(new Regex(regexStr, RegexOptions.IgnoreCase));                            
                        }
                    }
                }
            }

            /**
             * Separate method for Pcars which adds a copy of each classname with the null-standin char as the first char - 
             * i.e. "GT3" has another class called "?T3" for when PCars deems the first character of the classname unimportant.
             * Note that we also do this for regexs but only if they don't already start with a wildcard.
             * 
             */
            private void setupRegexsForPCars(List<string> classNames, List<Regex> regexs)
            {
                List<String> classesWithNullStandinChar = new List<string>();
                foreach (String className in classNames)
                {
                    if (className.Count() > 0)
                    {
                        if (className.Contains("*") || className.Contains("?"))
                        {
                            String regexStr = wildcardToRegex(className);
                            regexs.Add(new Regex(regexStr, RegexOptions.IgnoreCase));
                            if (!className.StartsWith("*") && !className.StartsWith("?"))
                            {
                                // sometimes PCars sends null instead of the proper first char of the String. We replace this with "?", 
                                // so assemble another regex here who's first 2 characters are replaced with the literal "^?", but only
                                // if this regex doesn't start with a wildcard
                                regexs.Add(new Regex("^\\" + PCarsGameStateMapper.NULL_CHAR_STAND_IN + regexStr.Substring(2), RegexOptions.IgnoreCase));
                            }
                        }
                        // sometimes PCars sends null instead of the proper first char of the String. We replace this with "?", 
                        // add another class name here who's first character is replaced with "?"
                        classesWithNullStandinChar.Add(PCarsGameStateMapper.NULL_CHAR_STAND_IN + className.Substring(1));
                    }
                }
                classNames.AddRange(classesWithNullStandinChar);
            }

            private String wildcardToRegex(string pattern)
            {
                return "^" + Regex.Escape(pattern).Replace("\\*", ".*").Replace("\\?", ".") + "$";
            }
        }

        public class CarClasses
        {
            public List<CarClass> carClasses { get; set; }
            public CarClasses()
            {
                this.carClasses = new List<CarClass>();
            }
        }

        public static void loadCarClassData()
        {
            CarClasses defaultCarClassData = getCarClassDataFromFile(getDefaultCarClassFileLocation());
            CarClasses userCarClassData = getCarClassDataFromFile(getUserCarClassFileLocation());
            mergeCarClassData(defaultCarClassData, userCarClassData);
            foreach (CarClass carClass in userCarClassData.carClasses)
            {
                carClass.setupRegexs();
            }
            CAR_CLASSES = userCarClassData;

            // reset session scoped cache of class name / ID to CarClass Dictionary.
            nameToCarClass = new Dictionary<string, CarClass>();
            intToCarClass = new Dictionary<int, CarClass>();
        }

        private static void mergeCarClassData(CarClasses defaultCarClassData, CarClasses userCarClassData)
        {
            int userCarClassesCount = 0;
            List<CarClass> classesToAddFromDefault = new List<CarClass>();
            foreach (CarClass defaultCarClass in defaultCarClassData.carClasses)
            {
                Boolean isInUserCarClasses = false;
                foreach (CarClass userCarClass in userCarClassData.carClasses)
                {
                    if (userCarClass.carClassEnum == defaultCarClass.carClassEnum)
                    {
                        isInUserCarClasses = true;
                        userCarClassesCount++;
                        break;
                    }
                }
                if (!isInUserCarClasses)
                {
                    classesToAddFromDefault.Add(defaultCarClass);
                }
            }
            userCarClassData.carClasses.AddRange(classesToAddFromDefault);
            Console.WriteLine("Loaded " + defaultCarClassData.carClasses.Count + " default car class definitions and " +
                userCarClassesCount + " user defined car class definitions");
        }

        private static CarClasses getCarClassDataFromFile(String filename)
        {
            if (filename != null)
            {
                try
                {
                    return JsonConvert.DeserializeObject<CarClasses>(getFileContents(filename));
                }
                catch (Exception e)
                {
                    Console.WriteLine("Error pasing " + filename + ": " + e.Message);
                }
            }
            return new CarClasses();
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

        private static String getDefaultCarClassFileLocation()
        {
            String path = Configuration.getDefaultFileLocation("carClassData.json");
            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }

        private static String getUserCarClassFileLocation()
        {
            String path = System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "carClassData.json");

            if (File.Exists(path))
            {
                return path;
            }
            else
            {
                return null;
            }
        }

        public static CarClass getCarClassFromEnum(CarClassEnum carClassEnum)
        {
            foreach (CarClass carClass in CAR_CLASSES.carClasses)
            {
                if (carClass.carClassEnum == carClassEnum)
                {
                    return carClass;
                }
            }
            return new CarClass();
        }

        public static CarClass getCarClassForRaceRoomId(int carClassId)
        {
            // first check if it's in the cache
            if (intToCarClass.ContainsKey(carClassId))
            {
                return intToCarClass[carClassId];
            }
            foreach (CarClass carClass in CAR_CLASSES.carClasses)
            {
                if (carClass.raceroomClassIds.Contains(carClassId))
                {
                    intToCarClass.Add(carClassId, carClass);
                    return carClass;
                }
            }

            // create one if it doesn't exist
            CarClass newCarClass = new CarClass();
            intToCarClass.Add(carClassId, newCarClass);
            newCarClass.placeholderClassId = carClassId.ToString();
            return newCarClass;
        }

        public static CarClass getCarClassForClassName(String className)
        {
            if (className == null)
            {
                // should we return null here? Throw an IllegalArgumentException?
                Console.WriteLine("Null car class name");
                return new CarClass();
            }
            else
            {
                // first check if it's in the cache
                if (nameToCarClass.ContainsKey(className))
                {
                    return nameToCarClass[className];
                }
                else
                {
                    String classNamesPropName = null;
                    String regexsPropName = null;
                    switch (CrewChief.gameDefinition.gameEnum)
                    {
                        case GameEnum.PCARS_64BIT:
                        case GameEnum.PCARS_32BIT:
                        case GameEnum.PCARS_NETWORK:
                            classNamesPropName = "pCarsClassNames";
                            regexsPropName = "pCarsClassNamesRegexs";
                            break;
                        case GameEnum.RF1:
                            classNamesPropName = "rf1ClassNames";
                            regexsPropName = "rf1ClassNamesRegexs";
                            break;
                        case GameEnum.ASSETTO_64BIT:
                        case GameEnum.ASSETTO_32BIT:
                            classNamesPropName = "acClassNames";
                            regexsPropName = "acClassNamesRegexs";
                            break;
                        case GameEnum.RF2_64BIT:
                            classNamesPropName = "rf2ClassNames";
                            regexsPropName = "rf2ClassNamesRegexs";
                            break;
                        default:
                            // err....
                            return new CarClass();
                            break;
                    }
                    foreach (CarClass carClass in CAR_CLASSES.carClasses)
                    {
                        List<String> classNames = (List<String>) carClass.GetType().GetProperty(classNamesPropName).GetValue(carClass, null);
                        foreach (String thisClassName in classNames)
                        {
                            if (thisClassName == className)
                            {
                                nameToCarClass.Add(className, carClass);
                                return carClass;
                            }
                        }
                    }
                    foreach (CarClass carClass in CAR_CLASSES.carClasses)
                    {
                        List<Regex> regexs = (List<Regex>) carClass.GetType().GetField(regexsPropName).GetValue(carClass);
                        foreach (Regex regex in regexs)
                        {
                            if (regex.IsMatch(className))
                            {
                                nameToCarClass.Add(className, carClass);
                                return carClass;
                            }
                        }
                    }
                    // no match, try matching on the enum directly
                    CarClassEnum carClassID = CarClassEnum.UNKNOWN_RACE;
                    if (Enum.TryParse<CarClassEnum>(className, out carClassID))
                    {
                        CarClass existingClass = CarData.getCarClassFromEnum(carClassID);
                        nameToCarClass.Add(className, existingClass);
                        return existingClass;
                    }
                    else
                    {
                        // no match - we really don't know what this class is, so create one
                        CarClass newCarClass = new CarClass();
                        newCarClass.placeholderClassId = className;
                        nameToCarClass.Add(className, newCarClass);
                        return newCarClass;
                    }
                }
            }
        }

        public static List<CornerData.EnumWithThresholds> getBrakeTempThresholds(CarClass carClass)
        {
            return brakeTempThresholds[carClass.brakeType];
        }

        public static TyreType getDefaultTyreType(CarClass carClass)
        {
            return carClass.defaultTyreType;
        }
    }
}