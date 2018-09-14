using CrewChiefV4.Events;
using CrewChiefV4.GameState;
using CrewChiefV4.PCars;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
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
        private static float maxColdRoadTyreTempPeak = 40;
        private static float maxWarmRoadTyreTempPeak = 90;
        private static float maxHotRoadTyreTempPeak = 110;

        // very wide range for unknown tyres
        private static float maxColdUnknownRaceTyreTempPeak = 60;
        private static float maxWarmUnknownRaceTyreTempPeak = 117;
        private static float maxHotUnknownRaceTyreTempPeak = 137;

        private static float maxColdHardTyreTempPeak = 78;
        private static float maxWarmHardTyreTempPeak = 110;
        private static float maxHotHardTyreTempPeak = 124;

        private static float maxColdMediumTyreTempPeak = 75;
        private static float maxWarmMediumTyreTempPeak = 105;
        private static float maxHotMediumTyreTempPeak = 120;

        private static float maxColdSoftTyreTempPeak = 70;
        private static float maxWarmSoftTyreTempPeak = 100;
        private static float maxHotSoftTyreTempPeak = 115;

        private static float maxColdSuperSoftTyreTempPeak = 68;
        private static float maxWarmSuperSoftTyreTempPeak = 98;
        private static float maxHotSuperSoftTyreTempPeak = 110;

        private static float maxColdUltraSoftTyreTempPeak = 65;
        private static float maxWarmUltraSoftTyreTempPeak = 95;
        private static float maxHotUltraSoftTyreTempPeak = 107;

        private static float maxColdWetTyreTempPeak = 40;
        private static float maxWarmWetTyreTempPeak = 80;
        private static float maxHotWetTyreTempPeak = 105;

        private static float maxColdIntermediateTyreTempPeak = 60;
        private static float maxWarmIntermediateTyreTempPeak = 95;
        private static float maxHotIntermediateTyreTempPeak = 110;

        // no idea about these - use similar thresholds to inters?
        private static float maxColdAllTerrainTyreTempPeak = 50;
        private static float maxWarmAllTerrainTyreTempPeak = 95;
        private static float maxHotAllTerrainTyreTempPeak = 110;

        // no idea what range to use here, so use a massive ranges
        private static float maxColdIceTyreTempPeak = -100;
        private static float maxWarmIceTyreTempPeak = 200;
        private static float maxHotIceTyreTempPeak = 300;

        private static float maxColdSnowTyreTempPeak = -100;
        private static float maxWarmSnowTyreTempPeak = 200;
        private static float maxHotSnowTyreTempPeak = 300;
        //

        private static float maxColdBiasPlyTyreTempPeak = 60;
        private static float maxWarmBiasPlyTyreTempPeak = 103;
        private static float maxHotBiasPlyTyreTempPeak = 123;

        // special cases for RaceRoom tyres on different tire model models
        // - the game sends the core temp, not the surface temp

        // model circa 2016:
        private static float maxColdR3E2016TyreTempPeak = 88;
        private static float maxWarmR3E2016TyreTempPeak = 100;
        private static float maxHotR3E2016TyreTempPeak = 105;

        private static float maxColdR3E2016PrimeTyreTempPeak = 88;
        private static float maxWarmR3E2016PrimeTyreTempPeak = 105;
        private static float maxHotR3E2016PrimeTyreTempPeak = 110;

        private static float maxColdR3E2016OptionTyreTempPeak = 75;
        private static float maxWarmR3E2016OptionTyreTempPeak = 110;
        private static float maxHotR3E2016OptionTyreTempPeak = 120;

        private static float maxColdR3E2016SoftTyreTempPeak = 85;
        private static float maxWarmR3E2016SoftTyreTempPeak = 100;
        private static float maxHotR3E2016SoftTyreTempPeak = 103;

        private static float maxColdR3E2016MediumTyreTempPeak = 90;
        private static float maxWarmR3E2016MediumTyreTempPeak = 104;
        private static float maxHotR3E2016MediumTyreTempPeak = 108;

        private static float maxColdR3E2016HardTyreTempPeak = 95;
        private static float maxWarmR3E2016HardTyreTempPeak = 108;
        private static float maxHotR3E2016HardTyreTempPeak = 113;

        // model late 2017:
        private static float maxColdR3E2017TyreTempPeak = 67;
        private static float maxWarmR3E2017TyreTempPeak = 90;
        private static float maxHotR3E2017TyreTempPeak = 102;


        private static float maxColdIronRoadBrakeTemp = 80;
        private static float maxWarmIronRoadBrakeTemp = 500;
        private static float maxHotIronRoadBrakeTemp = 780;

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
            GT1X, GT1, GTE, GT2, GTC, GTLM, GT3, GT4, GT5, GT300, GT500, Kart_1, Kart_2, KART_JUNIOR, KART_F1, KART_X30_SENIOR, KART_X30_RENTAL, LMP1, LMP2, LMP3, LMP900, ROAD_B, ROAD_C1, ROAD_C2, ROAD_D,
            ROAD_E, ROAD_F, ROAD_G, ROAD_SUPERCAR, GROUPC, GROUPB, GROUPA, GROUP4, GROUP5, GROUP6, GTO,
            VINTAGE_INDY_65, VINTAGE_F3_A, VINTAGE_F1_A, VINTAGE_F1_A1, VINTAGE_PROTOTYPE_B, VINTAGE_GT_D, VINTAGE_GT_C, HISTORIC_TOURING_1, HISTORIC_TOURING_2, VINTAGE_F1_B,
            VINTAGE_F1_C, VINTAGE_STOCK_CAR,
            F1, F2, F3, F4, FF, FORMULA_E, F1_70S, F1_90S, TC1, TC2, TCR, TC1_2014, AUDI_TT_CUP, AUDI_TT_VLN, CLIO_CUP, DTM, DTM_2013, V8_SUPERCAR, DTM_2014, DTM_2015, DTM_2016, TRANS_AM, HILL_CLIMB_ICONS, FORMULA_RENAULT,
            MEGANE_TROPHY, NSU_TT, KTM_RR, INDYCAR, HYPER_CAR, HYPER_CAR_RACE, UNKNOWN_RACE, STOCK_V8, BOXER_CUP, NASCAR_2016, ISI_STOCKCAR_2015, RADICAL_SR3, USER_CREATED,
            RS01_TROPHY, TRACKDAY_A, TRACKDAY_B, BMW_235I, CARRERA_CUP, R3E_SILHOUETTE, SPEC_MIATA, SKIP_BARBER, CAYMAN_CLUBSPORT, CAN_AM, FORMULA_RENAULT20, INDYCAR_DALLARA_2011, INDYCAR_DALLARA_DW12
        }

        // use different thresholds for R3E car classes - there are a few different tyre models in the game with different heating characteristics:
        public static CarClassEnum[] r3e2016TyreModelClasses = new CarClassEnum[] {
            CarClassEnum.LMP1, CarClassEnum.LMP2, CarClassEnum.GROUP5, CarClassEnum.GROUP4, CarClassEnum.GTO, CarClassEnum.F2, CarClassEnum.F4,
            CarClassEnum.FF, CarClassEnum.TC1, CarClassEnum.AUDI_TT_CUP, CarClassEnum.DTM_2013, CarClassEnum.DTM_2014, CarClassEnum.DTM_2015, CarClassEnum.DTM_2016, CarClassEnum.NSU_TT,
            CarClassEnum.F3, CarClassEnum.AUDI_TT_VLN, CarClassEnum.KTM_RR, CarClassEnum.TRACKDAY_A, CarClassEnum.R3E_SILHOUETTE, CarClassEnum.BMW_235I};

        public static CarClassEnum[] r3e2017TyreModelClasses = new CarClassEnum[] {
            CarClassEnum.GT1, CarClassEnum.GT3, CarClassEnum.GT4, CarClassEnum.CARRERA_CUP, CarClassEnum.TCR, CarClassEnum.GT1X, CarClassEnum.CAYMAN_CLUBSPORT};

        private static Dictionary<TyreType, List<CornerData.EnumWithThresholds>> tyreTempThresholds = new Dictionary<TyreType, List<CornerData.EnumWithThresholds>>();
        private static Dictionary<BrakeType, List<CornerData.EnumWithThresholds>> brakeTempThresholds = new Dictionary<BrakeType, List<CornerData.EnumWithThresholds>>();

        private static Dictionary<string, List<CarClassEnum>> groupedClasses = new Dictionary<string, List<CarClassEnum>>();
        
        static CarData()
        {
            List<CarClassEnum> r3eDTMClasses = new List<CarClassEnum>();
            r3eDTMClasses.Add(CarClassEnum.DTM_2013); r3eDTMClasses.Add(CarClassEnum.DTM_2014); r3eDTMClasses.Add(CarClassEnum.DTM_2015); r3eDTMClasses.Add(CarClassEnum.DTM_2016);
            groupedClasses.Add("DTM", r3eDTMClasses);

            List<CarClassEnum> r3eTC1Classes = new List<CarClassEnum>();
            r3eTC1Classes.Add(CarClassEnum.TC1); r3eTC1Classes.Add(CarClassEnum.TC1_2014);
            groupedClasses.Add("TC1", r3eTC1Classes);

            List<CornerData.EnumWithThresholds> roadTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdRoadTyreTempPeak));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdRoadTyreTempPeak, maxWarmRoadTyreTempPeak));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmRoadTyreTempPeak, maxHotRoadTyreTempPeak));
            roadTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotRoadTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Road, roadTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> ultraSoftTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            ultraSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdUltraSoftTyreTempPeak));
            ultraSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdUltraSoftTyreTempPeak, maxWarmUltraSoftTyreTempPeak));
            ultraSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmUltraSoftTyreTempPeak, maxHotUltraSoftTyreTempPeak));
            ultraSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotUltraSoftTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Ultra_Soft, ultraSoftTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> superSoftTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            superSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdSuperSoftTyreTempPeak));
            superSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdSuperSoftTyreTempPeak, maxWarmSuperSoftTyreTempPeak));
            superSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmSuperSoftTyreTempPeak, maxHotSuperSoftTyreTempPeak));
            superSoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotSuperSoftTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Super_Soft, superSoftTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> softTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            softTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdSoftTyreTempPeak));
            softTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdSoftTyreTempPeak, maxWarmSoftTyreTempPeak));
            softTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmSoftTyreTempPeak, maxHotSoftTyreTempPeak));
            softTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotSoftTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Soft, softTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> mediumTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            mediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdMediumTyreTempPeak));
            mediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdMediumTyreTempPeak, maxWarmMediumTyreTempPeak));
            mediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmMediumTyreTempPeak, maxHotMediumTyreTempPeak));
            mediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotMediumTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Medium, mediumTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> hardTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            hardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdHardTyreTempPeak));
            hardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdHardTyreTempPeak, maxWarmHardTyreTempPeak));
            hardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmHardTyreTempPeak, maxHotHardTyreTempPeak));
            hardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotHardTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Hard, hardTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> wetTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            wetTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdWetTyreTempPeak));
            wetTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdWetTyreTempPeak, maxWarmWetTyreTempPeak));
            wetTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmWetTyreTempPeak, maxHotWetTyreTempPeak));
            wetTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotWetTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Wet, wetTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> intermediateTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            intermediateTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdIntermediateTyreTempPeak));
            intermediateTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdIntermediateTyreTempPeak, maxWarmIntermediateTyreTempPeak));
            intermediateTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmIntermediateTyreTempPeak, maxHotIntermediateTyreTempPeak));
            intermediateTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotIntermediateTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Intermediate, intermediateTyreTempsThresholds);
            
            List<CornerData.EnumWithThresholds> iceTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            iceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdIceTyreTempPeak));
            iceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdIceTyreTempPeak, maxWarmIceTyreTempPeak));
            iceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmIceTyreTempPeak, maxHotIceTyreTempPeak));
            iceTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotIceTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Ice, iceTyreTempsThresholds);
            
            List<CornerData.EnumWithThresholds> snowTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            snowTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdSnowTyreTempPeak));
            snowTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdSnowTyreTempPeak, maxWarmSnowTyreTempPeak));
            snowTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmSnowTyreTempPeak, maxHotSnowTyreTempPeak));
            snowTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotSnowTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Snow, snowTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> allTerrainTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            allTerrainTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdAllTerrainTyreTempPeak));
            allTerrainTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdAllTerrainTyreTempPeak, maxWarmAllTerrainTyreTempPeak));
            allTerrainTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmAllTerrainTyreTempPeak, maxHotAllTerrainTyreTempPeak));
            allTerrainTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotAllTerrainTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.AllTerrain, allTerrainTyreTempsThresholds);

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

            List<CornerData.EnumWithThresholds> r3e2017TyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2017TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2017TyreTempPeak));
            r3e2017TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2017TyreTempPeak, maxWarmR3E2017TyreTempPeak));
            r3e2017TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2017TyreTempPeak, maxHotR3E2017TyreTempPeak));
            r3e2017TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2017TyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_2017, r3e2017TyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3e2016TyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2016TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2016TyreTempPeak));
            r3e2016TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2016TyreTempPeak, maxWarmR3E2016TyreTempPeak));
            r3e2016TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2016TyreTempPeak, maxHotR3E2016TyreTempPeak));
            r3e2016TyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2016TyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_2016, r3e2016TyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3e2016PrimeTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2016PrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2016PrimeTyreTempPeak));
            r3e2016PrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2016PrimeTyreTempPeak, maxWarmR3E2016PrimeTyreTempPeak));
            r3e2016PrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2016PrimeTyreTempPeak, maxHotR3E2016PrimeTyreTempPeak));
            r3e2016PrimeTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2016PrimeTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Prime, r3e2016PrimeTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3e2016OptionTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2016OptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2016OptionTyreTempPeak));
            r3e2016OptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2016OptionTyreTempPeak, maxWarmR3E2016OptionTyreTempPeak));
            r3e2016OptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2016OptionTyreTempPeak, maxHotR3E2016OptionTyreTempPeak));
            r3e2016OptionTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2016OptionTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.Option, r3e2016OptionTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3e2016HardTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2016HardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2016HardTyreTempPeak));
            r3e2016HardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2016HardTyreTempPeak, maxWarmR3E2016HardTyreTempPeak));
            r3e2016HardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2016HardTyreTempPeak, maxHotR3E2016HardTyreTempPeak));
            r3e2016HardTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2016HardTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_2016_HARD, r3e2016HardTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3e2016MediumTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2016MediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2016MediumTyreTempPeak));
            r3e2016MediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2016MediumTyreTempPeak, maxWarmR3E2016MediumTyreTempPeak));
            r3e2016MediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2016MediumTyreTempPeak, maxHotR3E2016MediumTyreTempPeak));
            r3e2016MediumTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2016MediumTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_2016_MEDIUM, r3e2016MediumTyreTempsThresholds);

            List<CornerData.EnumWithThresholds> r3e2016SoftTyreTempsThresholds = new List<CornerData.EnumWithThresholds>();
            r3e2016SoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000, maxColdR3E2016SoftTyreTempPeak));
            r3e2016SoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, maxColdR3E2016SoftTyreTempPeak, maxWarmR3E2016SoftTyreTempPeak));
            r3e2016SoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, maxWarmR3E2016SoftTyreTempPeak, maxHotR3E2016SoftTyreTempPeak));
            r3e2016SoftTyreTempsThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, maxHotR3E2016SoftTyreTempPeak, 10000));
            tyreTempThresholds.Add(TyreType.R3E_2016_SOFT, r3e2016SoftTyreTempsThresholds);

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

        public class CarClassEnumConverter : Newtonsoft.Json.Converters.StringEnumConverter
        {
            public override object ReadJson(JsonReader reader, Type objectType, object existingValue, JsonSerializer serializer)
            {
                var className = (string)reader.Value;
                CarClassEnum carClassID = CarClassEnum.UNKNOWN_RACE;
                if (Enum.TryParse<CarClassEnum>(className, out carClassID) && Enum.IsDefined(typeof(CarClassEnum), carClassID))
                {
                    return carClassID;
                }
                else
                {
                    // no match - we really don't know what this class is, so create one
                    Console.WriteLine("Car class enum for " + className + " not found");
                    userCarClassIds.Add(className);
                    return CarClassEnum.USER_CREATED;
                }
            }
        }

        private static CarClasses CAR_CLASSES;

        // used for PCars only, where we only know if the opponent is the same class as us or not
        public static CarClass DEFAULT_PCARS_OPPONENT_CLASS = new CarClass();

        private static Dictionary<string, CarClass> nameToCarClass;
        private static Dictionary<int, CarClass> intToCarClass;

        private static Dictionary<int, CarClass> iracingCarIdToCarClass;
        private static Dictionary<int, CarClass> iracingCarClassIdToCarClass;

        private static List<String> userCarClassIds = new List<string>();
        public static int RACEROOM_CLASS_ID = -1;
        public static int IRACING_CLASS_ID = -1;
        public static String CLASS_ID = "";

        public static void clearCachedIRacingClassData() {
            iracingCarClassIdToCarClass.Clear();
            iracingCarIdToCarClass.Clear();
        }

        public class TyreTypeData
        {
            public float maxColdTyreTemp { get; set; }
            public float maxWarmTyreTemp { get; set; }
            public float maxHotTyreTemp { get; set; }
        }

        public class CarClass
        {
            [JsonConverter(typeof(CarClassEnumConverter))]
            public CarClassEnum carClassEnum { get; set; }
            public List<int> raceroomClassIds { get; set; }
            public List<int> iracingCarIds { get; set; }
            public List<string> pCarsClassNames { get; set; }
            public List<string> rf1ClassNames { get; set; }
            public List<string> rf2ClassNames { get; set; }
            public List<string> acClassNames { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public BrakeType brakeType { get; set; }
            public float maxColdBrakeTemp { get; set; }
            public float maxWarmBrakeTemp { get; set; }
            public float maxHotBrakeTemp { get; set; }

            [JsonConverter(typeof(StringEnumConverter))]
            public TyreType defaultTyreType { get; set; }
            public Dictionary<string, TyreTypeData> acTyreTypeData { get; set; }
            public float maxColdTyreTemp { get; set; }
            public float maxWarmTyreTemp { get; set; }
            public float maxHotTyreTemp { get; set; }

            public float maxSafeWaterTemp { get; set; }
            public float maxSafeOilTemp { get; set; }
            public float minTyreCircumference { get; set; }
            public float maxTyreCircumference { get; set; }
            public float spotterVehicleWidth { get; set; }
            public float spotterVehicleLength { get; set; }

            public bool timesInHundredths { get; set; }
            public bool useAmericanTerms { get; set; }
            public String enabledMessageTypes { get; set; }
            public bool isDRSCapable { get; set; }
            public float DRSRange { get; set; }
            public int pitCrewPreparationTime { get; set; }
            public bool isBatteryPowered { get; set; }
            public bool limiterAvailable { get; set; }
            public bool allMembersAreFWD { get; set; }
            public bool allMembersAreRWD { get; set; }

            public String placeholderClassId = "";

            public Boolean grouped = false;

            public List<Regex> pCarsClassNamesRegexs = new List<Regex>();
            public List<Regex> rf1ClassNamesRegexs = new List<Regex>();
            public List<Regex> rf2ClassNamesRegexs = new List<Regex>();
            public List<Regex> acClassNamesRegexs = new List<Regex>();

            // Turns out enum.ToString() is costly, so cache string representation of enum value.
            private String carClassEnumString = null;
            public CarClass()
            {
                // initialise with default values
                this.carClassEnum = CarClassEnum.UNKNOWN_RACE;
                this.raceroomClassIds = new List<int>();
                this.iracingCarIds = new List<int>();
                this.pCarsClassNames = new List<string>();
                this.rf1ClassNames = new List<string>();
                this.rf2ClassNames = new List<string>();
                this.acClassNames = new List<string>();
                this.brakeType = BrakeType.Iron_Race;
                this.maxColdBrakeTemp = -1;
                this.maxWarmBrakeTemp = -1;
                this.maxHotBrakeTemp = -1;
                this.defaultTyreType = TyreType.Unknown_Race;
                this.acTyreTypeData = new Dictionary<string, TyreTypeData>();
                this.maxColdTyreTemp = -1;
                this.maxWarmTyreTemp = -1;
                this.maxHotTyreTemp = -1;
                this.maxSafeWaterTemp = 110;
                this.maxSafeOilTemp = 125;
                this.minTyreCircumference = 0.5f * (float)Math.PI;
                this.maxTyreCircumference = 1.2f * (float)Math.PI;
                this.enabledMessageTypes = "";
                this.isDRSCapable = false;
                this.DRSRange = -1.0f;
                this.pitCrewPreparationTime = 25;
                this.isBatteryPowered = false;
                this.limiterAvailable = true;
            }


            public String getClassIdentifier()
            {
                if (this.carClassEnum == CarClassEnum.UNKNOWN_RACE || this.carClassEnum == CarClassEnum.USER_CREATED)
                {
                    // this class has been generated from an unrecognised ID, so return the
                    // ID we used to generate this new class.
                    return placeholderClassId;
                }
                else
                {
                    if (this.carClassEnumString == null)
                    {
                        // if this car class is part of a group, the class identifier will be the group name, 
                        // not the enum name. This dictionary lookup and iteration is only done once per class because
                        // the result is cached, so this shouldn't affect performance too much
                        foreach (KeyValuePair<string, List<CarClassEnum>> keyValuePair in groupedClasses)
                        {
                            foreach (CarClassEnum carClassEnum in keyValuePair.Value)
                            {
                                if (carClassEnum == this.carClassEnum)
                                {
                                    this.carClassEnumString = keyValuePair.Key;
                                    grouped = true;
                                    break;
                                }
                            }
                            if (grouped)
                            {
                                break;
                            }
                        }
                        if (!grouped)
                        {
                            this.carClassEnumString = this.carClassEnum.ToString();
                        }
                    }
                    return this.carClassEnumString;
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

        public static Boolean IsCarClassEqual(CarClass class1, CarClass class2)
        {
            if (class1 == class2)
            {
                return true;
            }
            if (class1 == null && class2 != null)
            {
                return false;
            }
            if (class2 == null && class1 != null)
            {
                return false;
            }
            // check gropued classes separately
            if (class1.grouped && class2.grouped && String.Equals(class1.getClassIdentifier(), class2.getClassIdentifier()))
            {
                return true;
            }
            if (class1.carClassEnum == class2.carClassEnum)
            {
                // If car class enums are matching, we need to check if it isn't a special case of
                // ambigous enums, that is UNKNOWN_RACE or USER_CREATED.  Both of those can only be disambiguated
                // via identifier comparison.
                if ((class1.carClassEnum == CarClassEnum.UNKNOWN_RACE || class1.carClassEnum == CarClassEnum.USER_CREATED)
                    && (class2.carClassEnum == CarClassEnum.UNKNOWN_RACE || class2.carClassEnum == CarClassEnum.USER_CREATED)
                    && !String.Equals(class1.getClassIdentifier(), class2.getClassIdentifier()))
                {
                    return false;  // Both are UNKNOWN_RACE/USER_CREATED, but identifiers don't match.  Those are different classes.
                }

                return true;  // Same, unambigous enum values, classes are equal.
            }

            // The grouping is processed in the getClassIdentifier method, so we don't need to check for it here
            return false;
        }

        public static void loadCarClassData()
        {
            userCarClassIds = new List<string>();
            CarClasses defaultCarClassData = getCarClassDataFromFile(getDefaultCarClassFileLocation());
            CarClasses userCarClassData = getCarClassDataFromFile(getUserCarClassFileLocation());
            mergeCarClassData(defaultCarClassData, userCarClassData);
            foreach (CarClass carClass in userCarClassData.carClasses)
            {
                carClass.setupRegexs();
                // eagerly initialise these - this ensures the grouped flag is set correctly from the outset
                carClass.getClassIdentifier();
            }
            CAR_CLASSES = userCarClassData;

            // reset session scoped cache of class name / ID to CarClass Dictionary.
            nameToCarClass = new Dictionary<string, CarClass>();
            intToCarClass = new Dictionary<int, CarClass>();
            iracingCarIdToCarClass = new Dictionary<int, CarClass>();
            iracingCarClassIdToCarClass = new Dictionary<int, CarClass>();
        }

        private static void mergeCarClassData(CarClasses defaultCarClassData, CarClasses userCarClassData)
        {
            int userCarClassesCount = 0;
            List<CarClass> classesToAddFromDefault = new List<CarClass>();
            foreach (CarClass defaultCarClass in defaultCarClassData.carClasses)
            {
                Boolean isInUserCarClasses = false;
                int userCarClassIndex = 0;
                foreach (CarClass userCarClass in userCarClassData.carClasses)
                {
                    if (userCarClass.carClassEnum == defaultCarClass.carClassEnum)
                    {
                        isInUserCarClasses = true;
                        userCarClassesCount++;
                        break;
                    }
                    else if (userCarClass.carClassEnum == CarClassEnum.USER_CREATED && userCarClass.placeholderClassId.Length == 0 &&
                        userCarClassIds.Count > userCarClassIndex)
                    {
                        userCarClass.placeholderClassId = userCarClassIds[userCarClassIndex];
                        Console.WriteLine("Adding a user defined class with ID " + userCarClass.placeholderClassId);
                        userCarClassIndex++;
                        userCarClassesCount++;
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
                    Console.WriteLine("Error parsing " + filename + ": " + e.Message);
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
            CarClass carClassCached = null;
            if (intToCarClass.TryGetValue(carClassId, out carClassCached))
            {
                return carClassCached;
            }
            foreach (CarClass carClass in CAR_CLASSES.carClasses)
            {
                if (carClass.raceroomClassIds.Contains(carClassId))
                {
                    Console.WriteLine("Mapped car class from ID:\"{0}\"  to:\"{1}\"", carClassId, carClass.getClassIdentifier());
                    intToCarClass.Add(carClassId, carClass);
                    return carClass;
                }
            }

            // create one if it doesn't exist
            CarClass newCarClass = new CarClass();
            Console.WriteLine("Unmapped car class added:\"{0}\"", carClassId);
            intToCarClass.Add(carClassId, newCarClass);
            newCarClass.placeholderClassId = carClassId.ToString();
            return newCarClass;
        }

        public static CarClass getCarClassForIRacingId(int carClassId, int carId)
        {
            // first check if it's in the one of the caches
            CarClass carClassCached = null;
            if (iracingCarIdToCarClass.TryGetValue(carId, out carClassCached))
            {
                return carClassCached;
            }
            if (iracingCarClassIdToCarClass.TryGetValue(carClassId, out carClassCached))
            {
                return carClassCached;
            }
            foreach (CarClass carClass in CAR_CLASSES.carClasses)
            {
                if (carClass.iracingCarIds.Contains(carId))
                {
                    iracingCarIdToCarClass.Add(carId, carClass);
                    return carClass;
                }
            }

            // create one if it doesn't exist
            CarClass newCarClass = new CarClass();
            iracingCarClassIdToCarClass.Add(carClassId, newCarClass);
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
                CarClass cachedCarClass = null;
                if (nameToCarClass.TryGetValue(className, out cachedCarClass))
                {
                    return cachedCarClass;
                }
                else
                {
                    String classNamesPropName = null;
                    String regexsPropName = null;
                    switch (CrewChief.gameDefinition.gameEnum)
                    {
                        case GameEnum.PCARS_64BIT:
                        case GameEnum.PCARS_32BIT:
                        case GameEnum.PCARS2:
                        case GameEnum.PCARS_NETWORK:
                        case GameEnum.PCARS2_NETWORK:
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
                    }
                    foreach (CarClass carClass in CAR_CLASSES.carClasses)
                    {
                        List<String> classNames = (List<String>)carClass.GetType().GetProperty(classNamesPropName).GetValue(carClass, null);
                        foreach (String thisClassName in classNames)
                        {
                            if (string.Compare(thisClassName, className, StringComparison.InvariantCultureIgnoreCase) == 0)
                            {
                                Console.WriteLine("Mapped car class from ID:\"{0}\"  to:\"{1}\"", className, carClass.getClassIdentifier());
                                nameToCarClass.Add(className, carClass);
                                return carClass;
                            }
                        }
                    }
                    foreach (CarClass carClass in CAR_CLASSES.carClasses)
                    {
                        List<Regex> regexs = (List<Regex>)carClass.GetType().GetField(regexsPropName).GetValue(carClass);
                        foreach (Regex regex in regexs)
                        {
                            if (regex.IsMatch(className))
                            {
                                Console.WriteLine("Mapped car class from ID:\"{0}\"  to:\"{1}\"", className, carClass.getClassIdentifier());
                                nameToCarClass.Add(className, carClass);
                                return carClass;
                            }
                        }
                    }
                    // no match, try matching on the enum directly
                    CarClassEnum carClassID = CarClassEnum.UNKNOWN_RACE;
                    if (Enum.TryParse<CarClassEnum>(className, out carClassID) && Enum.IsDefined(typeof(CarClassEnum), carClassID))
                    {
                        CarClass existingClass = CarData.getCarClassFromEnum(carClassID);
                        Console.WriteLine("Mapped car class from ID:\"{0}\"  to:\"{1}\"", className, existingClass.getClassIdentifier());
                        nameToCarClass.Add(className, existingClass);
                        return existingClass;
                    }
                    else
                    {
                        // no match - we really don't know what this class is, so create one
                        CarClass newCarClass = new CarClass();
                        newCarClass.placeholderClassId = className;
                        Console.WriteLine("Unmapped car class added:\"{0}\"", className);
                        nameToCarClass.Add(className, newCarClass);
                        return newCarClass;
                    }
                }
            }
        }

        public static List<CornerData.EnumWithThresholds> getBrakeTempThresholds(CarClass carClass)
        {
            var predefinedBrakeThresholds = brakeTempThresholds[carClass.brakeType];
            // Copy predefined thresholds to avoid overriding defaults.
            var btt = new List<CornerData.EnumWithThresholds>();
            foreach (var threshold in predefinedBrakeThresholds)
            {
                btt.Add(new CornerData.EnumWithThresholds(threshold.e, threshold.lowerThreshold, threshold.upperThreshold));
            }
            // Apply overrides from .json, if user provided them.
            if (btt.Count == 4) // COLD, WARM, HOT, COOKING thresholds
            {
                // Should we validate thresholds to see if they make any sense?
                if (carClass.maxColdBrakeTemp > 0)
                {
                    Debug.Assert((BrakeTemp)btt[0].e == BrakeTemp.COLD);
                    btt[0].upperThreshold = carClass.maxColdBrakeTemp;
                    Debug.Assert((BrakeTemp)btt[1].e == BrakeTemp.WARM);
                    btt[1].lowerThreshold = carClass.maxColdBrakeTemp;
                }
                if (carClass.maxWarmBrakeTemp > 0)
                {
                    Debug.Assert((BrakeTemp)btt[1].e == BrakeTemp.WARM);
                    btt[1].upperThreshold = carClass.maxWarmBrakeTemp;
                    Debug.Assert((BrakeTemp)btt[2].e == BrakeTemp.HOT);
                    btt[2].lowerThreshold = carClass.maxWarmBrakeTemp;
                }
                if (carClass.maxHotBrakeTemp > 0)
                {
                    Debug.Assert((BrakeTemp)btt[2].e == BrakeTemp.HOT);
                    btt[2].upperThreshold = carClass.maxHotBrakeTemp;
                    Debug.Assert((BrakeTemp)btt[3].e == BrakeTemp.COOKING);
                    btt[3].lowerThreshold = carClass.maxHotBrakeTemp;
                }
            }
            return btt;
        }

        public static TyreType getDefaultTyreType(CarClass carClass)
        {
            return carClass.defaultTyreType;
        }

        public static List<CornerData.EnumWithThresholds> getTyreTempThresholds(CarClass carClass)
        {
            var predefinedTyreThresholds = tyreTempThresholds[carClass.defaultTyreType];
            // Copy predefined thresholds to avoid overriding defaults.
            var ttt = new List<CornerData.EnumWithThresholds>();
            foreach (var threshold in predefinedTyreThresholds)
            {
                ttt.Add(new CornerData.EnumWithThresholds(threshold.e, threshold.lowerThreshold, threshold.upperThreshold));
            }
            // Apply overrides from .json, if user provided them.
            if (ttt.Count == 4) // COLD, WARM, HOT, COOKING thresholds
            {
                // Should we validate thresholds to see if they make any sense?
                if (carClass.maxColdTyreTemp > 0)
                {
                    Debug.Assert((TyreTemp)ttt[0].e == TyreTemp.COLD);
                    ttt[0].upperThreshold = carClass.maxColdTyreTemp;
                    Debug.Assert((TyreTemp)ttt[1].e == TyreTemp.WARM);
                    ttt[1].lowerThreshold = carClass.maxColdTyreTemp;
                }
                if (carClass.maxWarmTyreTemp > 0)
                {
                    Debug.Assert((TyreTemp)ttt[1].e == TyreTemp.WARM);
                    ttt[1].upperThreshold = carClass.maxWarmTyreTemp;
                    Debug.Assert((TyreTemp)ttt[2].e == TyreTemp.HOT);
                    ttt[2].lowerThreshold = carClass.maxWarmTyreTemp;
                }
                if (carClass.maxHotTyreTemp > 0)
                {
                    Debug.Assert((TyreTemp)ttt[2].e == TyreTemp.HOT);
                    ttt[2].upperThreshold = carClass.maxHotTyreTemp;
                    Debug.Assert((TyreTemp)ttt[3].e == TyreTemp.COOKING);
                    ttt[3].lowerThreshold = carClass.maxHotTyreTemp;
                }
            }
            return ttt;
        }

        public static List<CornerData.EnumWithThresholds> getTyreTempThresholds(CarClass carClass, TyreType tyreType)
        {
            if (!tyreTempThresholds.ContainsKey(tyreType))
            {
                tyreType = TyreType.Unknown_Race;
            }
            var predefinedTyreThresholds = tyreTempThresholds[tyreType];
            // Copy predefined thresholds to avoid overriding defaults.
            var ttt = new List<CornerData.EnumWithThresholds>();
            foreach (var threshold in predefinedTyreThresholds)
            {
                ttt.Add(new CornerData.EnumWithThresholds(threshold.e, threshold.lowerThreshold, threshold.upperThreshold));
            }
            // Apply overrides from .json, if user provided them.
            if (ttt.Count == 4) // COLD, WARM, HOT, COOKING thresholds
            {
                // Should we validate thresholds to see if they make any sense?
                if (carClass.maxColdTyreTemp > 0)
                {
                    Debug.Assert((TyreTemp)ttt[0].e == TyreTemp.COLD);
                    ttt[0].upperThreshold = carClass.maxColdTyreTemp;
                    Debug.Assert((TyreTemp)ttt[1].e == TyreTemp.WARM);
                    ttt[1].lowerThreshold = carClass.maxColdTyreTemp;
                }
                if (carClass.maxWarmTyreTemp > 0)
                {
                    Debug.Assert((TyreTemp)ttt[1].e == TyreTemp.WARM);
                    ttt[1].upperThreshold = carClass.maxWarmTyreTemp;
                    Debug.Assert((TyreTemp)ttt[2].e == TyreTemp.HOT);
                    ttt[2].lowerThreshold = carClass.maxWarmTyreTemp;
                }
                if (carClass.maxHotTyreTemp > 0)
                {
                    Debug.Assert((TyreTemp)ttt[2].e == TyreTemp.HOT);
                    ttt[2].upperThreshold = carClass.maxHotTyreTemp;
                    Debug.Assert((TyreTemp)ttt[3].e == TyreTemp.COOKING);
                    ttt[3].lowerThreshold = carClass.maxHotTyreTemp;
                }
            }
            return ttt;
        }
    }
}
