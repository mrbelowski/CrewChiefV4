using CrewChiefV4.Events;
using CrewChiefV4.GameState;
using CrewChiefV4.PCars;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

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

        private static float maxColdDtmOptionTyreTempPeak = 70;
        private static float maxWarmDtmOptionTyreTempPeak = 110;
        private static float maxHotDtmOptionTyreTempPeak = 127;

        private static float maxColdDtmPrimeTyreTempPeak = 80;
        private static float maxWarmDtmPrimeTyreTempPeak = 117;
        private static float maxHotDtmPrimeTyreTempPeak = 137;

        private static float maxColdBiasPlyTyreTempPeak = 70;
        private static float maxWarmBiasPlyTyreTempPeak = 103;
        private static float maxHotBiasPlyTyreTempPeak = 123;
        
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
        
        private static float maxRoadSafeWaterTemp = 96;
        private static float maxRoadSafeOilTemp = 115;

        private static float maxRaceSafeWaterTemp = 105;
        private static float maxRaceSafeOilTemp = 125;

        // for F1, GP2, LMP1 and DTM
        private static float maxExoticRaceSafeWaterTemp = 105;
        private static float maxExoticRaceSafeOilTemp = 140;

        // for locking / spinning check - the tolerance values are built into these tyre diameter values
        private static float carMinTyreCircumference = 0.4f * (float)Math.PI;  // 0.4m diameter
        private static float carMaxTyreCircumference = 1.2f * (float)Math.PI;

        // for locking / spinning check - the tolerance values are built into these tyre diameter values
        private static float kartMinTyreCircumference = 0.25f * (float)Math.PI;  // 0.15m diameter
        private static float kartMaxTyreCircumference = 0.4f * (float)Math.PI;

        public enum CarClassEnum
        {
            GT1X, GT1, GT2, ADAC_GTM_2014, ADAC_GTM_2013, GT3, GT4, GT5, Kart_1, Kart_2, LMP1, LMP2, LMP3, ROAD_B, ROAD_C1, ROAD_C2, ROAD_D, ROAD_SUPERCAR, GROUPC, GROUPA, GROUP4, GROUP5,
            VINTAGE_F1_A, VINTAGE_GT3, VINTAGE_GT, HISTORIC_TOURING_1, HISTORIC_TOURING_2, VINTAGE_F1_B, VINTAGE_F1_C, STOCK_CAR, F1, F2, F3, F4, FF, TC1, TC2, AUDI_TT_CUP, CLIO_CUP, DTM, DTM_2013, DTM_2014, DTM_2015, TRANS_AM, HILL_CLIMB_ICONS, UNKNOWN_RACE
        }

        public class CarClass
        {
            public CarClassEnum carClassEnum;
            public String[] pCarsClassNames;
            public int[] raceroomClassIds;
            public BrakeType brakeType;
            public TyreType defaultTyreType;
            public float maxSafeWaterTemp;
            public float maxSafeOilTemp;
            public float minTyreCircumference;
            public float maxTyreCircumference;

            public CarClass(CarClassEnum carClassEnum, String[] pCarsClassNames, int[] raceroomClassIds, BrakeType brakeType, TyreType defaultTyreType, float maxSafeWaterTemp,
                float maxSafeOilTemp, float minTyreCircumference, float maxTyreCircumference)
            {
                this.carClassEnum = carClassEnum;
                this.pCarsClassNames = pCarsClassNames;
                this.raceroomClassIds = raceroomClassIds;
                this.brakeType = brakeType;
                this.defaultTyreType = defaultTyreType;
                this.maxSafeOilTemp = maxSafeOilTemp;
                this.maxSafeWaterTemp = maxSafeWaterTemp;
                this.minTyreCircumference = minTyreCircumference;
                this.maxTyreCircumference = maxTyreCircumference;
            }

            public CarClass(CarClassEnum carClassEnum, String[] pCarsClassNames, int[] raceroomClassIds, BrakeType brakeType, TyreType defaultTyreType, float maxSafeWaterTemp,
               float maxSafeOilTemp)
            {
                this.carClassEnum = carClassEnum;
                this.pCarsClassNames = pCarsClassNames;
                this.raceroomClassIds = raceroomClassIds;
                this.brakeType = brakeType;
                this.defaultTyreType = defaultTyreType;
                this.maxSafeOilTemp = maxSafeOilTemp;
                this.maxSafeWaterTemp = maxSafeWaterTemp;
                this.minTyreCircumference = carMinTyreCircumference;
                this.maxTyreCircumference = carMaxTyreCircumference;
            }
        }

        private static List<CarClass> carClasses = new List<CarClass>();

        public static Dictionary<TyreType, List<CornerData.EnumWithThresholds>> tyreTempThresholds = new Dictionary<TyreType, List<CornerData.EnumWithThresholds>>();
        public static Dictionary<BrakeType, List<CornerData.EnumWithThresholds>> brakeTempThresholds = new Dictionary<BrakeType, List<CornerData.EnumWithThresholds>>();
        
        static CarData() 
        {
            carClasses.Add(new CarClass(CarClassEnum.UNKNOWN_RACE, new String[] { "" }, new int[] { -1 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.GT1X, new String[] { "GT1X" }, new int[] { 1710 }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT1, new String[] { "GT1" }, new int[] { 1687 }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT2, new String[] { "GT2" }, new int[] { 1704 }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT3, new String[] { "GT3" }, new int[] { 1703, 2922, 3375 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ADAC_GTM_2013, new String[] {  }, new int[] { 2922 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ADAC_GTM_2014, new String[] {  }, new int[] { 3375 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT4, new String[] { "GT4" }, new int[] { 1717 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GT5, new String[] { "GT5" }, new int[] { }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.Kart_1, new String[] { "Kart2" }, new int[] { }, BrakeType.Iron_Road, TyreType.Unknown_Race, maxRoadSafeWaterTemp, maxRoadSafeOilTemp,
                kartMinTyreCircumference, kartMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.Kart_2, new String[] { "Kart1" }, new int[] { }, BrakeType.Iron_Road, TyreType.Unknown_Race, maxRoadSafeWaterTemp, maxRoadSafeOilTemp,
                kartMinTyreCircumference, kartMaxTyreCircumference));

            carClasses.Add(new CarClass(CarClassEnum.LMP1, new String[] { "LMP1" }, new int[] { 1716 }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.LMP2, new String[] { "LMP2" }, new int[] { 2337 }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.LMP3, new String[] { "LMP3" }, new int[] { 1828, 255 }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.GROUPC, new String[] { "Group C1" }, new int[] { }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GROUP5, new String[] { "Group 5" }, new int[] { 1708, 1713 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GROUP4, new String[] { "Group 4" }, new int[] { 2378 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.GROUPA, new String[] { "Group A" }, new int[] { 1712, 3499 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp)); // just for reference...

            carClasses.Add(new CarClass(CarClassEnum.F1, new String[] { "FA" }, new int[] {  }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.F2, new String[] { "FB" }, new int[] {4597 }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.F3, new String[] { "FC" }, new int[] {  }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.F4, new String[] { "F4" }, new int[] {  }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.FF, new String[] { "F5" }, new int[] {  }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));   // formula ford

            // here we assume the old race cars (pre-radial tyres) will race on bias ply tyres
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_F1_C, new String[] { "Vintage F1 C" }, new int[] { }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_F1_B, new String[] { "Vintage F1 B" }, new int[] { }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_F1_A, new String[] { "Vintage F1 A" }, new int[] { }, BrakeType.Iron_Race, TyreType.Bias_Ply, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_GT3, new String[] { "Vintage GT3" }, new int[] { }, BrakeType.Iron_Race, TyreType.Bias_Ply, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.VINTAGE_GT, new String[] { "Vintage GT" }, new int[] { }, BrakeType.Iron_Race, TyreType.Bias_Ply, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.HISTORIC_TOURING_1, new String[] { "Historic Touring 1" }, new int[] { }, BrakeType.Iron_Race, TyreType.Bias_Ply, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));
            carClasses.Add(new CarClass(CarClassEnum.HISTORIC_TOURING_2, new String[] { "Historic Touring 2" }, new int[] { }, BrakeType.Iron_Race, TyreType.Bias_Ply, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));

            carClasses.Add(new CarClass(CarClassEnum.STOCK_CAR, new String[] { "Vintage Stockcar" }, new int[] { }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.TRANS_AM, new String[] { "Trans-Am" }, new int[] { 1707, 1706 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.DTM, new String[] { "TC3" }, new int[] { }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp)); // modern DTM
            carClasses.Add(new CarClass(CarClassEnum.DTM_2013, new String[] { }, new int[] { 1921 }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp)); // modern DTM
            carClasses.Add(new CarClass(CarClassEnum.DTM_2014, new String[] { }, new int[] { 3086 }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp)); // modern DTM
            carClasses.Add(new CarClass(CarClassEnum.DTM_2015, new String[] { }, new int[] { 4260 }, BrakeType.Carbon, TyreType.Unknown_Race, maxExoticRaceSafeWaterTemp, maxExoticRaceSafeOilTemp)); // modern DTM
            carClasses.Add(new CarClass(CarClassEnum.CLIO_CUP, new String[] { "TC1" }, new int[] { }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp)); // clios
            carClasses.Add(new CarClass(CarClassEnum.TC1, new String[] { "WTCC_2014" }, new int[] { 3905 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp)); // clios
            carClasses.Add(new CarClass(CarClassEnum.TC2, new String[] { "BTCC, WTCC_2013", "TC2" }, new int[] { 1922 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp)); // clios
            carClasses.Add(new CarClass(CarClassEnum.AUDI_TT_CUP, new String[] { "Audi TT Cup" }, new int[] { 4680 }, BrakeType.Iron_Race, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp, carMinTyreCircumference, carMaxTyreCircumference));

            carClasses.Add(new CarClass(CarClassEnum.ROAD_D, new String[] { "Road D" }, new int[] { }, BrakeType.Iron_Road, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ROAD_C2, new String[] { "Road C2"}, new int[] { }, BrakeType.Iron_Road, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ROAD_C1, new String[] { "Road C1" }, new int[] { }, BrakeType.Iron_Road, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ROAD_B, new String[] { "Road B" }, new int[] { }, BrakeType.Iron_Road, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));
            carClasses.Add(new CarClass(CarClassEnum.ROAD_SUPERCAR, new String[] { "Road A" }, new int[] { }, BrakeType.Ceramic, TyreType.Road, maxRoadSafeWaterTemp, maxRoadSafeOilTemp));

            carClasses.Add(new CarClass(CarClassEnum.HILL_CLIMB_ICONS, new String[] { }, new int[] { 1685 }, BrakeType.Ceramic, TyreType.Unknown_Race, maxRaceSafeWaterTemp, maxRaceSafeOilTemp));

            
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

            List<CornerData.EnumWithThresholds> dtmOptionTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdDtmOptionTyreTempPeak));
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdDtmOptionTyreTempPeak, maxWarmDtmOptionTyreTempPeak));
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmDtmOptionTyreTempPeak, maxHotDtmOptionTyreTempPeak));
            dtmOptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotDtmOptionTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Option, dtmOptionTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> dtmPrimeTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdDtmPrimeTyreTempPeak));
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdDtmPrimeTyreTempPeak, maxWarmDtmPrimeTyreTempPeak));
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmDtmPrimeTyreTempPeak, maxHotDtmPrimeTyreTempPeak));
            dtmPrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotDtmPrimeTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Prime, dtmPrimeTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> biasPlyTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdBiasPlyTyreTempPeak));
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdBiasPlyTyreTempPeak, maxWarmBiasPlyTyreTempPeak));
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmBiasPlyTyreTempPeak, maxHotBiasPlyTyreTempPeak));
            biasPlyTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotBiasPlyTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Bias_Ply, biasPlyTyreTempsThresholds);

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

        public static CarClass getCarClassForPCarsClassName(String carClassName)
        {
            if (carClassName != null && carClassName.Count() > 1)
            {
                Boolean skipFirstChar = false;
                if (carClassName.StartsWith(PCarsGameStateMapper.NULL_CHAR_STAND_IN)) {
                    carClassName = carClassName.Substring(1);
                    skipFirstChar = true;
                }
                foreach (CarClass carClass in carClasses)
                {
                    foreach (String className in carClass.pCarsClassNames) {
                        if ((!skipFirstChar && className.Equals(carClassName)) || (skipFirstChar && className.Substring(1).Equals(carClassName))) {
                            return carClass;
                        }
                    }
                }
            }
            return getDefaultCarClass();
        }

        public static CarClass getCarClassForRaceRoomId(int carClassId)
        {
            if (carClassId != -1)
            {
                foreach (CarClass carClass in carClasses)
                {
                    if (carClass.raceroomClassIds.Contains(carClassId))
                    {
                        return carClass;
                    }

                }
            }
            CarClass defaultClass = getDefaultCarClass();
            return defaultClass;
        }

        public static CarClass getCarClassFromEnum(CarClassEnum carClassEnum) 
        {
            foreach (CarClass carClass in carClasses)
            {
                if (carClass.carClassEnum == carClassEnum)
                {
                    return carClass;
                }
            }
            return getDefaultCarClass();
        }

        public static CarClass getDefaultCarClass()
        {
            return carClasses[0];
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
