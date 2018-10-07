using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using CrewChiefV4.assetto.assettoData;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 */

namespace CrewChiefV4.assetto
{
    public class ACSGameStateMapper : GameStateMapper
    {
        public static String playerName = null;
        public static Boolean versionChecked = false;
        public static double lastCountDown = 10000.0;
        public static int numberOfSectorsOnTrack = 3;

        private class AcTyres
        {
            public List<CornerData.EnumWithThresholds> tyreWearThresholdsForAC = new List<CornerData.EnumWithThresholds>();
            public List<CornerData.EnumWithThresholds> tyreTempThresholdsForAC = new List<CornerData.EnumWithThresholds>();
            public float tyreWearMinimumValue;

            public AcTyres(List<CornerData.EnumWithThresholds> tyreWearThresholds, List<CornerData.EnumWithThresholds> tyreTempThresholds, float tyreWearMinimum)
            {
                tyreWearThresholdsForAC = tyreWearThresholds;
                tyreTempThresholdsForAC = tyreTempThresholds;
                tyreWearMinimumValue = tyreWearMinimum;
            }
        }


        List<CornerData.EnumWithThresholds> tyreTempThresholds = new List<CornerData.EnumWithThresholds>();
        private static Dictionary<string, AcTyres> acTyres = new Dictionary<string, AcTyres>();

        private Boolean logUnknownTrackSectors = UserSettings.GetUserSettings().getBoolean("enable_acs_log_sectors_for_unknown_tracks");

        private Boolean disableYellowFlag = UserSettings.GetUserSettings().getBoolean("disable_acs_yellow_flag_warnings");

        private int singleplayerPracticTime = UserSettings.GetUserSettings().getInt("acs_practice_time_minuts");
        // these are set when we start a new session, from the car name / class
        private TyreType defaultTyreTypeForPlayersCar = TyreType.Unknown_Race;

        private float[] loggedSectorStart = new float[] { -1f, -1f };

        private List<CornerData.EnumWithThresholds> brakeTempThresholdsForPlayersCar = null;
        private static string expectedVersion = "1.7";
        private static string expectedPluginVersion = "1.0.0";

        private int lapCountAtSector1End = -1;

        // next track conditions sample due after:
        private DateTime nextConditionsSampleDue = DateTime.MinValue;        

        private List<CornerData.EnumWithThresholds> suspensionDamageThresholds = new List<CornerData.EnumWithThresholds>();

        private float trivialSuspensionDamageThreshold = 0.01f;
        private float minorSuspensionDamageThreshold = 0.05f;
        private float severeSuspensionDamageThreshold = 0.15f;
        private float destroyedSuspensionDamageThreshold = 0.60f;

        private float trivialEngineDamageThreshold = 900.0f;
        private float minorEngineDamageThreshold = 600.0f;
        private float severeEngineDamageThreshold = 350.0f;
        private float destroyedEngineDamageThreshold = 25.0f;

        private float trivialAeroDamageThreshold = 40.0f;
        private float minorAeroDamageThreshold = 100.0f;
        private float severeAeroDamageThreshold = 200.0f;
        private float destroyedAeroDamageThreshold = 400.0f;

        #region WaYToManyTyres
        public ACSGameStateMapper()
        {

            CornerData.EnumWithThresholds suspensionDamageNone = new CornerData.EnumWithThresholds(DamageLevel.NONE, -10000, trivialSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageTrivial = new CornerData.EnumWithThresholds(DamageLevel.TRIVIAL, trivialSuspensionDamageThreshold, minorSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageMinor = new CornerData.EnumWithThresholds(DamageLevel.MINOR, trivialSuspensionDamageThreshold, severeSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageMajor = new CornerData.EnumWithThresholds(DamageLevel.MAJOR, severeSuspensionDamageThreshold, destroyedSuspensionDamageThreshold);
            CornerData.EnumWithThresholds suspensionDamageDestroyed = new CornerData.EnumWithThresholds(DamageLevel.DESTROYED, destroyedSuspensionDamageThreshold, 10000);
            suspensionDamageThresholds.Add(suspensionDamageNone);
            suspensionDamageThresholds.Add(suspensionDamageTrivial);
            suspensionDamageThresholds.Add(suspensionDamageMinor);
            suspensionDamageThresholds.Add(suspensionDamageMajor);
            suspensionDamageThresholds.Add(suspensionDamageDestroyed);

            //GTE Classes

            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlickSuperSoft = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 3.685061f));
            tyreWearThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 3.685061f, 7.407379f));
            tyreWearThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 7.407379f, 72.5275f));
            tyreWearThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 72.5275f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlickSuperSoft = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 50f));
            tyreTempsThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 50f, 80f));
            tyreTempsThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 140f));
            tyreTempsThresholdsSlickSuperSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Slick SuperSoft (SS)", new AcTyres(tyreWearThresholdsSlickSuperSoft, tyreTempsThresholdsSlickSuperSoft, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlickSoft = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlickSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlickSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 3.685061f));
            tyreWearThresholdsSlickSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 3.685061f, 14.96601f));
            tyreWearThresholdsSlickSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 14.96601f, 46.8085f));
            tyreWearThresholdsSlickSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 46.8085f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSoftSlick = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSoftSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 70f));
            tyreTempsThresholdsSoftSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 70f, 85f));
            tyreTempsThresholdsSoftSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 85f, 100f));
            tyreTempsThresholdsSoftSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 100f, 10000f));
            acTyres.Add("Slick Soft (S)", new AcTyres(tyreWearThresholdsSlickSoft, tyreTempsThresholdsSoftSlick, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlickMedium = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlickMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlickMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 3.685061f));
            tyreWearThresholdsSlickMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 3.685061f, 14.96601f));
            tyreWearThresholdsSlickMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 14.96601f, 30.55553f));
            tyreWearThresholdsSlickMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsMediumSlick = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsMediumSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 90f));
            tyreTempsThresholdsMediumSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 90f, 115f));
            tyreTempsThresholdsMediumSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 115f, 140f));
            tyreTempsThresholdsMediumSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Slick Medium (M)", new AcTyres(tyreWearThresholdsSlickMedium, tyreTempsThresholdsMediumSlick, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlickHard = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlickHard.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlickHard.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 14.96601f));
            tyreWearThresholdsSlickHard.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 14.96601f, 22.68041f));
            tyreWearThresholdsSlickHard.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 30.55553f));
            tyreWearThresholdsSlickHard.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsHardSlick = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsHardSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsHardSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsHardSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 180f));
            tyreTempsThresholdsHardSlick.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 180f, 10000f));
            acTyres.Add("Slick Hard (H)", new AcTyres(tyreWearThresholdsSlickHard, tyreTempsThresholdsHardSlick, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlickSuperHard = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 1.469612f));
            tyreWearThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 1.469612f, 22.68041f));
            tyreWearThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 30.55553f));
            tyreWearThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlickSuperHard = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 85f));
            tyreTempsThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 85f, 110f));
            tyreTempsThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 110f, 160f));
            tyreTempsThresholdsSlickSuperHard.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 160f, 10000f));
            acTyres.Add("Slick SuperHard (SH)", new AcTyres(tyreWearThresholdsSlickSuperHard, tyreTempsThresholdsSlickSuperHard, 88f));

            //Some Car(s) :D
            List<CornerData.EnumWithThresholds> tyreWearThresholdsBFGoodrich = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 3.685061f));
            tyreWearThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 3.685061f, 30.55553f));
            tyreWearThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 30.55553f, 46.8085f));
            tyreWearThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 46.8085f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsBFGoodrich = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 59f));
            tyreTempsThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 59f, 110f));
            tyreTempsThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 110f, 160f));
            tyreTempsThresholdsBFGoodrich.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 160f, 10000f));
            acTyres.Add("BFGoodrich (g) Slicks (S)", new AcTyres(tyreWearThresholdsBFGoodrich, tyreTempsThresholdsBFGoodrich, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsCinturato = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 12.06036f));
            tyreWearThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 12.06036f, 24.2424f));
            tyreWearThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 24.2424f, 48.97957f));
            tyreWearThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 48.97957f, 1000f));


            List<CornerData.EnumWithThresholds> tyreTempsThresholdsCinturato = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 85f));
            tyreTempsThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 85f, 160f));
            tyreTempsThresholdsCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 160f, 10000f));
            acTyres.Add("Cinturato (V)", new AcTyres(tyreWearThresholdsCinturato, tyreTempsThresholdsCinturato, 96f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsECO = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 2.01004f));
            tyreWearThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 2.01004f, 8.163261f));
            tyreWearThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 8.163261f, 44.44443f));
            tyreWearThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 44.44443f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsECO = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 85f));
            tyreTempsThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 85f, 140f));
            tyreTempsThresholdsECO.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("ECO (E)", new AcTyres(tyreWearThresholdsECO, tyreTempsThresholdsECO, 80f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsGP54 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 1.0f));
            tyreWearThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 1.0f, 49.49493f));
            tyreWearThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 49.49493f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsGP54 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 40f));
            tyreTempsThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 40f, 90f));
            tyreTempsThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 110f));
            tyreTempsThresholdsGP54.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("GP54 (V)", new AcTyres(tyreWearThresholdsGP54, tyreTempsThresholdsGP54, 98f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsGP57 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 1.0f));
            tyreWearThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 1.0f, 49.49493f));
            tyreWearThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 49.49493f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsGP57 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 40f));
            tyreTempsThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 40f, 90f));
            tyreTempsThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 110f));
            tyreTempsThresholdsGP57.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("GP57 (V)", new AcTyres(tyreWearThresholdsGP57, tyreTempsThresholdsGP57, 98f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsGP63 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 1.0f));
            tyreWearThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 1.0f, 49.49493f));
            tyreWearThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 49.49493f, 1000f));


            List<CornerData.EnumWithThresholds> tyreTempsThresholdsGP63 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 40f));
            tyreTempsThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 40f, 90f));
            tyreTempsThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 110f));
            tyreTempsThresholdsGP63.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("GP63 (V)", new AcTyres(tyreWearThresholdsGP63, tyreTempsThresholdsGP63, 98f));

            List<CornerData.EnumWithThresholds> tyreWearThresholdsGP67 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 1.0f));
            tyreWearThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 1.0f, 49.49493f));
            tyreWearThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 49.49493f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsGP67 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 40f));
            tyreTempsThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 40f, 90f));
            tyreTempsThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 110f));
            tyreTempsThresholdsGP67.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("GP67 (V)", new AcTyres(tyreWearThresholdsGP67, tyreTempsThresholdsGP67, 98f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSoftGP70 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 4.522629f));
            tyreWearThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 4.522629f, 18.36731f));
            tyreWearThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 18.36731f, 27.83508f));
            tyreWearThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 27.83508f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSoftGP70 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 90f));
            tyreTempsThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 140f));
            tyreTempsThresholdsSoftGP70.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Soft GP70 (S)", new AcTyres(tyreWearThresholdsSoftGP70, tyreTempsThresholdsSoftGP70, 90f));

            List<CornerData.EnumWithThresholds> tyreWearThresholdsGP70H = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 19.19189f));
            tyreWearThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 19.19189f, 38.77548f));
            tyreWearThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 38.77548f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsGP70H = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 90f));
            tyreTempsThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 140f));
            tyreTempsThresholdsGP70H.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Hard GP70 (H)", new AcTyres(tyreWearThresholdsSoftGP70, tyreTempsThresholdsSoftGP70, 95f));

            List<CornerData.EnumWithThresholds> tyreWearThresholdsGP86H = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 23.85788f));
            tyreWearThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 23.85788f, 48.45365f));
            tyreWearThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 48.45365f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsGP86H = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsGP86H.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Hard GP86 (H)", new AcTyres(tyreWearThresholdsGP86H, tyreTempsThresholdsGP86H, 94f));

            List<CornerData.EnumWithThresholds> tyreWearThresholdsSoftGP86 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, -1.0f));
            tyreWearThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, -1.0f, 0.0f));
            tyreWearThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 0.0f, 15.82489f));
            tyreWearThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 15.82489f, 48.45365f));
            tyreWearThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 48.45365f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSoftGP86 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsSoftGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Soft GP86 (S)", new AcTyres(tyreWearThresholdsSoftGP86, tyreTempsThresholdsSoftGP86, 94f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsHardSlicks90 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 4.522629f));
            tyreWearThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 4.522629f, 13.7056f));
            tyreWearThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 13.7056f, 23.07693f));
            tyreWearThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 23.07693f, 1000f));


            List<CornerData.EnumWithThresholds> tyreTempsThresholdsHardSlicks90 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 95f));
            tyreTempsThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 95f, 115f));
            tyreTempsThresholdsHardSlicks90.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 115f, 10000f));
            acTyres.Add("Hard Slicks 90s (H)", new AcTyres(tyreWearThresholdsHardSlicks90, tyreTempsThresholdsHardSlicks90, 90f));

            //
            List<CornerData.EnumWithThresholds> tyreWearThresholdsHypercarCup = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 12.06036f));
            tyreWearThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 12.06036f, 36.54823f));
            tyreWearThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 36.54823f, 74.22676f));
            tyreWearThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 74.22676f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsHypercarCup = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 95f));
            tyreTempsThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 95f, 105f));
            tyreTempsThresholdsHypercarCup.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 105f, 10000f));
            acTyres.Add("Hypercar Cup (HC)", new AcTyres(tyreWearThresholdsHypercarCup, tyreTempsThresholdsHypercarCup, 96f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsHypercarRoad = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 1.172536f));
            tyreWearThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 1.172536f, 3.553289f));
            tyreWearThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 3.553289f, 4.761912f));
            tyreWearThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 4.761912f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsHypercarRoad = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsHypercarRoad.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Hypercar road (HR)", new AcTyres(tyreWearThresholdsHypercarRoad, tyreTempsThresholdsHypercarRoad, 70f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsHypercarTrofeo = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 14.96601f));
            tyreWearThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 14.96601f, 22.68041f));
            tyreWearThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 30.55553f));
            tyreWearThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));


            List<CornerData.EnumWithThresholds> tyreTempsThresholdsHypercarTrofeo = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsHypercarTrofeo.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Hypercar Trofeo (I)", new AcTyres(tyreWearThresholdsHypercarTrofeo, tyreTempsThresholdsHypercarTrofeo, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsTrofeoHSlicks = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 14.96601f));
            tyreWearThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 14.96601f, 22.68041f));
            tyreWearThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 30.55553f));
            tyreWearThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsTrofeoHSlicks = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsTrofeoHSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Trofeo H Slicks (H)", new AcTyres(tyreWearThresholdsTrofeoHSlicks, tyreTempsThresholdsTrofeoHSlicks, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsTrofeoMSlicks = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 4.522629f));
            tyreWearThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 4.522629f, 18.36731f));
            tyreWearThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 18.36731f, 27.83508f));
            tyreWearThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 27.83508f, 1000f));


            List<CornerData.EnumWithThresholds> tyreTempsThresholdsTrofeoMSlicks = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 90f));
            tyreTempsThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 120f));
            tyreTempsThresholdsTrofeoMSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 120f, 10000f));
            acTyres.Add("Trofeo M Slicks (M)", new AcTyres(tyreWearThresholdsTrofeoMSlicks, tyreTempsThresholdsTrofeoMSlicks, 90f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsMediumGP86 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 23.85788f));
            tyreWearThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 23.85788f, 31.97276f));
            tyreWearThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 31.97276f, 48.45365f));
            tyreWearThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 48.45365f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsMediumGP86 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsMediumGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Medium GP86 (M)", new AcTyres(tyreWearThresholdsMediumGP86, tyreTempsThresholdsMediumGP86, 94f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSoftSlicks90s = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 2.01004f));
            tyreWearThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 2.01004f, 8.163261f));
            tyreWearThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 8.163261f, 34.7826f));
            tyreWearThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 34.7826f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSoftSlicks90s = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 65f));
            tyreTempsThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 65f, 80f));
            tyreTempsThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 100f));
            tyreTempsThresholdsSoftSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 100f, 10000f));
            acTyres.Add("Soft Slicks 90s (S)", new AcTyres(tyreWearThresholdsSoftSlicks90s, tyreTempsThresholdsSoftSlicks90s, 80f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsMediumSlicks90s = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 3.685061f));
            tyreWearThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 3.685061f, 22.68041f));
            tyreWearThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 38.59647f));
            tyreWearThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 38.59647f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsMediumSlicks90s = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 70f));
            tyreTempsThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 70f, 90f));
            tyreTempsThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 105f));
            tyreTempsThresholdsMediumSlicks90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 105f, 10000f));
            acTyres.Add("Medium Slicks 90s (M)", new AcTyres(tyreWearThresholdsMediumSlicks90s, tyreTempsThresholdsMediumSlicks90s, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsPirelliCinturato = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 7.407379f));
            tyreWearThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 7.407379f, 11.16753f));
            tyreWearThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 11.16753f, 30.55553f));
            tyreWearThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsPirelliCinturato = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 60f));
            tyreTempsThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 60f, 80f));
            tyreTempsThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 95f));
            tyreTempsThresholdsPirelliCinturato.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 95f, 10000f));
            acTyres.Add("Pirelli Cinturato (V)", new AcTyres(tyreWearThresholdsPirelliCinturato, tyreTempsThresholdsPirelliCinturato, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsQualifyingGP86 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 1.172536f));
            tyreWearThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 1.172536f, 4.761912f));
            tyreWearThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 4.761912f, 41.17648f));
            tyreWearThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 41.17648f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsQualifyingGP86 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 70f));
            tyreTempsThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 170f));
            tyreTempsThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 170f, 200f));
            tyreTempsThresholdsQualifyingGP86.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 200f, 10000f));
            acTyres.Add("Qualifying GP86 (Q)", new AcTyres(tyreWearThresholdsQualifyingGP86, tyreTempsThresholdsQualifyingGP86, 70f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSemislick = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 1.172536f));
            tyreWearThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 1.172536f, 3.553289f));
            tyreWearThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 3.553289f, 4.761912f));
            tyreWearThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 4.761912f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSemislick = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 95f));
            tyreTempsThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 95f, 140f));
            tyreTempsThresholdsSemislick.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Semislick (SM)", new AcTyres(tyreWearThresholdsSemislick, tyreTempsThresholdsSemislick, 70f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSemislicks = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 1.172536f));
            tyreWearThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 1.172536f, 3.553289f));
            tyreWearThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 3.553289f, 4.761912f));
            tyreWearThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 4.761912f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSemislicks = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 95f));
            tyreTempsThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 95f, 140f));
            tyreTempsThresholdsSemislicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Semislicks (SM)", new AcTyres(tyreWearThresholdsSemislicks, tyreTempsThresholdsSemislicks, 70f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicks70 = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 12.06036f));
            tyreWearThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 12.06036f, 36.54823f));
            tyreWearThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 36.54823f, 48.97957f));
            tyreWearThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 48.97957f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicks70 = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 85f));
            tyreTempsThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 85f, 140f));
            tyreTempsThresholdsSlicks70.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Slicks 70s (S)", new AcTyres(tyreWearThresholdsSlicks70, tyreTempsThresholdsSlicks70, 96f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicksSoftDTM90s = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 2.01004f));
            tyreWearThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 2.01004f, 8.163261f));
            tyreWearThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 8.163261f, 34.7826f));
            tyreWearThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 34.7826f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicksSoftDTM90s = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 65f));
            tyreTempsThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 65f, 80f));
            tyreTempsThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 100f));
            tyreTempsThresholdsSlicksSoftDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 100f, 10000f));
            acTyres.Add("Slicks Soft DTM90s (S)", new AcTyres(tyreWearThresholdsSlicksSoftDTM90s, tyreTempsThresholdsSlicksSoftDTM90s, 80f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicksHardDTM90s = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 4.522629f));
            tyreWearThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 4.522629f, 13.7056f));
            tyreWearThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 13.7056f, 23.07693f));
            tyreWearThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 23.07693f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicksHardDTM90s = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 60f));
            tyreTempsThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 60f, 90f));
            tyreTempsThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 115f));
            tyreTempsThresholdsSlicksHardDTM90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 115f, 10000f));
            acTyres.Add("Slicks Hard DTM90s (H)", new AcTyres(tyreWearThresholdsSlicksHardDTM90s, tyreTempsThresholdsSlicksHardDTM90s, 90f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicksMedium = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 3.685061f));
            tyreWearThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 3.685061f, 7.407379f));
            tyreWearThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 7.407379f, 30.55553f));
            tyreWearThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicksMedium = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 60f));
            tyreTempsThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 60f, 100f));
            tyreTempsThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 110f));
            tyreTempsThresholdsSlicksMedium.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("Slicks Medium (M)", new AcTyres(tyreWearThresholdsSlicksMedium, tyreTempsThresholdsSlicksMedium, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicksHard = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 14.96601f));
            tyreWearThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 14.96601f, 22.68041f));
            tyreWearThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 30.55553f));
            tyreWearThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicksHard = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 70f));
            tyreTempsThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 70f, 120f));
            tyreTempsThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 120f, 140f));
            tyreTempsThresholdsSlicksHard.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Slicks Hard (H)", new AcTyres(tyreWearThresholdsSlicksHard, tyreTempsThresholdsSlicksHard, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicksSoft = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 14.96601f));
            tyreWearThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 14.96601f, 46.8085f));
            tyreWearThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 46.8085f, 81.48149f));
            tyreWearThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 81.48149f, 1000f));


            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicksSoft = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 50f));
            tyreTempsThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 50f, 90f));
            tyreTempsThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 90f, 105f));
            tyreTempsThresholdsSlicksSoft.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 105f, 10000f));
            acTyres.Add("Slicks Soft (S)", new AcTyres(tyreWearThresholdsSlicksSoft, tyreTempsThresholdsSlicksSoft, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsSlicks = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 14.96601f));
            tyreWearThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 14.96601f, 22.68041f));
            tyreWearThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 22.68041f, 30.55553f));
            tyreWearThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 30.55553f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsSlicks = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 100f));
            tyreTempsThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 100f, 140f));
            tyreTempsThresholdsSlicks.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 140f, 10000f));
            acTyres.Add("Slicks (H)", new AcTyres(tyreWearThresholdsSlicks, tyreTempsThresholdsSlicks, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsStreet90s = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 11.16753f));
            tyreWearThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 11.16753f, 30.55553f));
            tyreWearThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 30.55553f, 55.19714f));
            tyreWearThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 55.19714f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsStreet90s = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 80f));
            tyreTempsThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 95f));
            tyreTempsThresholdsStreet90s.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 95f, 10000f));
            acTyres.Add("Street 90s (SV)", new AcTyres(tyreWearThresholdsStreet90s, tyreTempsThresholdsStreet90s, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsStreet90SSV = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 7.407379f));
            tyreWearThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 7.407379f, 30.55553f));
            tyreWearThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 30.55553f, 55.19714f));
            tyreWearThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 55.19714f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsStreet90SSV = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 80f));
            tyreTempsThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 95f));
            tyreTempsThresholdsStreet90SSV.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 95f, 10000f));
            acTyres.Add("Street90S (SV)", new AcTyres(tyreWearThresholdsStreet90SSV, tyreTempsThresholdsStreet90SSV, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsStreetvintage = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 11.16753f));
            tyreWearThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 11.16753f, 30.55553f));
            tyreWearThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 30.55553f, 55.19714f));
            tyreWearThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 55.19714f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsStreetvintage = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 80f));
            tyreTempsThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 95f));
            tyreTempsThresholdsStreetvintage.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 95f, 10000f));
            acTyres.Add("Street vintage (SV)", new AcTyres(tyreWearThresholdsStreetvintage, tyreTempsThresholdsStreetvintage, 88f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsStreet = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 2.01004f));
            tyreWearThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 2.01004f, 6.091385f));
            tyreWearThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 6.091385f, 21.05263f));
            tyreWearThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 21.05263f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsStreet = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 75f));
            tyreTempsThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 75f, 85f));
            tyreTempsThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 85f, 110f));
            tyreTempsThresholdsStreet.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("Street (ST)", new AcTyres(tyreWearThresholdsStreet, tyreTempsThresholdsStreet, 80f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsVintage60s = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 9.090881f));
            tyreWearThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 9.090881f, 18.36731f));
            tyreWearThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 18.36731f, 18.46731f));
            tyreWearThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 18.46731f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsVintage60s = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 40f));
            tyreTempsThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 40f, 80f));
            tyreTempsThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 110f));
            tyreTempsThresholdsVintage60s.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("Vintage 60s (V)", new AcTyres(tyreWearThresholdsVintage60s, tyreTempsThresholdsVintage60s, 90f));


            List<CornerData.EnumWithThresholds> tyreWearThresholdsVintage = new List<CornerData.EnumWithThresholds>();
            tyreWearThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreCondition.NEW, -10000f, 0.000f));
            tyreWearThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreCondition.SCRUBBED, 0.000f, 19.19189f));
            tyreWearThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreCondition.MINOR_WEAR, 19.19189f, 38.77548f));
            tyreWearThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreCondition.MAJOR_WEAR, 38.77548f, 39.77548f));
            tyreWearThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreCondition.WORN_OUT, 38.77548f, 1000f));

            List<CornerData.EnumWithThresholds> tyreTempsThresholdsVintage = new List<CornerData.EnumWithThresholds>();
            tyreTempsThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, 40f));
            tyreTempsThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, 40f, 80f));
            tyreTempsThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, 80f, 110f));
            tyreTempsThresholdsVintage.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, 110f, 10000f));
            acTyres.Add("Vintage (V)", new AcTyres(tyreWearThresholdsVintage, tyreTempsThresholdsVintage, 95f));

        }
        #endregion

        public override void versionCheck(Object memoryMappedFileStruct)
        {

            AssettoCorsaShared shared = ((ACSSharedMemoryReader.ACSStructWrapper)memoryMappedFileStruct).data;
            String currentVersion = shared.acsStatic.smVersion;
            String currentPluginVersion = getNameFromBytes(shared.acsChief.pluginVersion);
            if (currentVersion.Length != 0 && currentPluginVersion.Length != 0 && versionChecked == false)
            {
                Console.WriteLine("Shared Memory Version: " + shared.acsStatic.smVersion);
                if (!currentVersion.Equals(expectedVersion, StringComparison.Ordinal))
                {
                    throw new GameDataReadException("Expected shared data version " + expectedVersion + " but got version " + currentVersion);
                }
                Console.WriteLine("Plugin Version: " + currentPluginVersion);
                if (!currentPluginVersion.Equals(expectedPluginVersion, StringComparison.Ordinal))
                {

                    throw new GameDataReadException("Expected python plugin version " + expectedPluginVersion + " but got version " + currentPluginVersion);
                }
                versionChecked = true;
            }
        }
        
        public static OpponentData getOpponentForName(GameStateData gameState, String nameToFind)
        {
            if (gameState.OpponentData == null || gameState.OpponentData.Count == 0 || nameToFind == null || nameToFind.Length == 0)
            {
                return null;
            }

            OpponentData od = null;
            if (gameState.OpponentData.TryGetValue(nameToFind, out od))
            {
                return od;
            }
            return null;
        }

        public override GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            ACSSharedMemoryReader.ACSStructWrapper wrapper = (ACSSharedMemoryReader.ACSStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            AssettoCorsaShared shared = wrapper.data;
            AC_STATUS status = shared.acsGraphic.status;
            /*if (status == AC_STATUS.AC_OFF)
            {
                return null;
            }*/

            if (status == AC_STATUS.AC_REPLAY)
            {
                CrewChief.trackName = shared.acsStatic.track + ":" + shared.acsStatic.trackConfiguration;
                CrewChief.carClass = CarData.getCarClassForClassName(shared.acsStatic.carModel).carClassEnum;
                CrewChief.viewingReplay = true;
                CrewChief.distanceRoundTrack = spLineLengthToDistanceRoundTrack(shared.acsChief.vehicle[0].spLineLength, shared.acsStatic.trackSPlineLength);
            }

            if (status == AC_STATUS.AC_REPLAY || status == AC_STATUS.AC_OFF || shared.acsChief.numVehicles <= 0)
            {
                return previousGameState;
            }
            acsVehicleInfo playerVehicle = shared.acsChief.vehicle[0];

            Boolean isOnline = getNameFromBytes(shared.acsChief.serverName).Length > 0;
            Boolean isSinglePlayerPracticeSession = shared.acsChief.numVehicles == 1 && !isOnline && shared.acsGraphic.session == AC_SESSION_TYPE.AC_PRACTICE;
            float distanceRoundTrack = spLineLengthToDistanceRoundTrack(playerVehicle.spLineLength, shared.acsStatic.trackSPlineLength);


            playerName = getNameFromBytes(playerVehicle.driverName);
            Validator.validate(playerName);
            AC_SESSION_TYPE sessionType = shared.acsGraphic.session;

            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;
            float lastSessionRunningTime = 0;
            int lastSessionLapsCompleted = 0;
            TrackDefinition lastSessionTrack = null;
            Boolean lastSessionHasFixedTime = false;
            int lastSessionNumberOfLaps = 0;
            float lastSessionTotalRunTime = 0;
            float lastSessionTimeRemaining = 0;

            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionType = previousGameState.SessionData.SessionType;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
                lastSessionTrack = previousGameState.SessionData.TrackDefinition;
                lastSessionLapsCompleted = previousGameState.SessionData.CompletedLaps;
                lastSessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                lastSessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                lastSessionTimeRemaining = previousGameState.SessionData.SessionTimeRemaining;
                currentGameState.carClass = previousGameState.carClass;

                currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                currentGameState.SessionData.PlayerLapTimeSessionBestPrevious = previousGameState.SessionData.PlayerLapTimeSessionBestPrevious;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                currentGameState.SessionData.OverallSessionBestLapTime = previousGameState.SessionData.OverallSessionBestLapTime;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = previousGameState.SessionData.PlayerClassSessionBestLapTime;
                currentGameState.SessionData.CurrentLapIsValid = previousGameState.SessionData.CurrentLapIsValid;
                currentGameState.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;
                currentGameState.readLandmarksForThisLap = previousGameState.readLandmarksForThisLap;
            }

            if (currentGameState.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
            {
                CarData.CarClass newClass = CarData.getCarClassForClassName(shared.acsStatic.carModel);
                CarData.CLASS_ID = shared.acsStatic.carModel;
                if (!CarData.IsCarClassEqual(newClass, currentGameState.carClass))
                {
                    currentGameState.carClass = newClass;
                    GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                    Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                    brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                    // no tyre data in the block so get the default tyre types for this car
                    defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                }
            }

            currentGameState.SessionData.SessionType = mapToSessionState(sessionType);

            Boolean leaderHasFinished = previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace;
            currentGameState.SessionData.LeaderHasFinishedRace = leaderHasFinished;

            int numberOfLapsInSession = (int)shared.acsGraphic.numberOfLaps;

            float gameSessionTimeLeft = 0.0f;
            if (!Double.IsInfinity(shared.acsGraphic.sessionTimeLeft))
            {
                gameSessionTimeLeft = shared.acsGraphic.sessionTimeLeft / 1000f;
            }

            float sessionTimeRemaining = -1;
            if (numberOfLapsInSession == 0 || shared.acsStatic.isTimedRace == 1)
            {
                currentGameState.SessionData.SessionHasFixedTime = true;
                sessionTimeRemaining = isSinglePlayerPracticeSession ? (float)TimeSpan.FromMinutes(singleplayerPracticTime).TotalSeconds - Math.Abs(gameSessionTimeLeft) : gameSessionTimeLeft;
            }

            Boolean isCountDown = false;
            TimeSpan countDown = TimeSpan.FromSeconds(gameSessionTimeLeft);

            if (sessionType == AC_SESSION_TYPE.AC_RACE || sessionType == AC_SESSION_TYPE.AC_DRIFT || sessionType == AC_SESSION_TYPE.AC_DRAG)
            {
                //Make sure to check for both numberOfLapsInSession and isTimedRace as latter sometimes tells lies!
                if (shared.acsStatic.isTimedRace == 1 || numberOfLapsInSession == 0)
                {
                    isCountDown = playerVehicle.currentLapTimeMS <= 0 && playerVehicle.lapCount <= 0;
                }
                else
                {
                    isCountDown = countDown.TotalMilliseconds >= 0.25;
                }
            }


            int realTimeLeaderBoardValid = isCarRealTimeLeaderBoardValid(shared.acsChief.vehicle, shared.acsChief.numVehicles);
            AC_FLAG_TYPE currentFlag = shared.acsGraphic.flag;
            if (sessionType == AC_SESSION_TYPE.AC_PRACTICE || sessionType == AC_SESSION_TYPE.AC_QUALIFY)
            {
                currentGameState.SessionData.OverallPosition = playerVehicle.carLeaderboardPosition;
            }
            else
            {
                // Prevent positsion change first 10 sec of race else we get invalid position report when crossing the start/finish line when the light turns green.
                // carRealTimeLeaderboardPosition does not allways provide valid data doing this fase.
                if ((previousGameState != null && previousGameState.SessionData.SessionRunningTime < 10 && shared.acsGraphic.session == AC_SESSION_TYPE.AC_RACE) ||
                    currentFlag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
                {
                    //Console.WriteLine("using carLeaderboardPosition");
                    currentGameState.SessionData.OverallPosition = playerVehicle.carLeaderboardPosition;
                }
                else
                {
                    currentGameState.SessionData.OverallPosition = playerVehicle.carRealTimeLeaderboardPosition + realTimeLeaderBoardValid;
                }
            }

            currentGameState.SessionData.IsDisqualified = currentFlag == AC_FLAG_TYPE.AC_BLACK_FLAG;
            bool isInPits = shared.acsGraphic.isInPit == 1;
            int lapsCompleted = shared.acsGraphic.completedLaps;
            ACSGameStateMapper.numberOfSectorsOnTrack = shared.acsStatic.sectorCount;

            Boolean raceFinished = lapsCompleted == numberOfLapsInSession || (previousGameState != null && previousGameState.SessionData.LeaderHasFinishedRace && previousGameState.SessionData.IsNewLap);
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(currentGameState.SessionData.SessionType, currentFlag, status, isCountDown, lastSessionPhase, sessionTimeRemaining, lastSessionTotalRunTime, isInPits, lapsCompleted, raceFinished);

            currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track
                + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength, shared.acsStatic.sectorCount);            

            Boolean sessionOfSameTypeRestarted = ((currentGameState.SessionData.SessionType == SessionType.Race && lastSessionType == SessionType.Race) ||
                (currentGameState.SessionData.SessionType == SessionType.Practice && lastSessionType == SessionType.Practice) ||
                (currentGameState.SessionData.SessionType == SessionType.Qualify && lastSessionType == SessionType.Qualify)) &&
                ((lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow) || lastSessionPhase == SessionPhase.Finished) &&
                currentGameState.SessionData.SessionPhase == SessionPhase.Countdown &&
                (currentGameState.SessionData.SessionType == SessionType.Race ||
                    currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1);

            if (sessionOfSameTypeRestarted ||
                (currentGameState.SessionData.SessionType != SessionType.Unavailable &&
                 currentGameState.SessionData.SessionPhase != SessionPhase.Finished &&
                    (lastSessionType != currentGameState.SessionData.SessionType ||
                        lastSessionTrack == null || lastSessionTrack.name != currentGameState.SessionData.TrackDefinition.name ||
                            (currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1))))
            {
                Console.WriteLine("New session, trigger...");
                if (sessionOfSameTypeRestarted)
                {
                    Console.WriteLine("Session of same type (" + lastSessionType + ") restarted (green / finished -> countdown)");
                }
                if (lastSessionType != currentGameState.SessionData.SessionType)
                {
                    Console.WriteLine("lastSessionType = " + lastSessionType + " currentGameState.SessionData.SessionType = " + currentGameState.SessionData.SessionType);
                }
                else if (lastSessionTrack != currentGameState.SessionData.TrackDefinition)
                {
                    String lastTrackName = lastSessionTrack == null ? "unknown" : lastSessionTrack.name;
                    String currentTrackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                    Console.WriteLine("lastSessionTrack = " + lastTrackName + " currentGameState.SessionData.Track = " + currentTrackName);
                    if (currentGameState.SessionData.TrackDefinition.unknownTrack)
                    {
                        Console.WriteLine("Track is unknown, setting virtual sectors");
                        currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();
                    }

                }
                else if (currentGameState.SessionData.SessionHasFixedTime && sessionTimeRemaining > lastSessionTimeRemaining + 1)
                {
                    Console.WriteLine("sessionTimeRemaining = " + sessionTimeRemaining.ToString("0.000") + " lastSessionTimeRemaining = " + lastSessionTimeRemaining.ToString("0.000"));
                }
                lapCountAtSector1End = -1;
                currentGameState.SessionData.IsNewSession = true;
                currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionTotalRunTime = sessionTimeRemaining;
                    currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                    currentGameState.SessionData.HasExtraLap = shared.acsStatic.hasExtraLap == 1;
                    if (currentGameState.SessionData.SessionTotalRunTime == 0)
                    {
                        Console.WriteLine("Setting session run time to 0");
                    }
                    Console.WriteLine("Time in this new session = " + sessionTimeRemaining.ToString("0.000"));
                }
                currentGameState.SessionData.DriverRawName = playerName;
                currentGameState.PitData.IsRefuellingAllowed = true;

                //add carclasses for assetto corsa.
                currentGameState.carClass = CarData.getCarClassForClassName(shared.acsStatic.carModel);
                GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                CarData.CLASS_ID = shared.acsStatic.carModel;

                Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                Utilities.TraceEventClass(currentGameState);

                if (acTyres.Count > 0 && !acTyres.ContainsKey(shared.acsGraphic.tyreCompound))
                {
                    Console.WriteLine("Tyre information is disabled. Player is using unknown Tyre Type " + shared.acsGraphic.tyreCompound);
                }
                else
                {
                    Console.WriteLine("Player is using Tyre Type " + shared.acsGraphic.tyreCompound);
                }
                currentGameState.TyreData.TyreTypeName = shared.acsGraphic.tyreCompound;
                tyreTempThresholds = getTyreTempThresholds(currentGameState.carClass, currentGameState.TyreData.TyreTypeName);
                brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                // no tyre data in the block so get the default tyre types for this car
                defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);

                for (int i = 0; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];
                    if (participantStruct.isConnected == 1)
                    {
                        String participantName = getNameFromBytes(participantStruct.driverName).ToLower();
                        if (i != 0 && participantName != null && participantName.Length > 0)
                        {
                            CarData.CarClass opponentCarClass = CarData.getCarClassForClassName(getNameFromBytes(participantStruct.carModel));
                            addOpponentForName(participantName, createOpponentData(participantStruct, false, opponentCarClass, shared.acsStatic.trackSPlineLength), currentGameState);
                        }
                    }
                }

                currentGameState.SessionData.PlayerLapTimeSessionBest = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = -1;
                currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = -1;
                currentGameState.SessionData.OverallSessionBestLapTime = -1;
                currentGameState.SessionData.PlayerClassSessionBestLapTime = -1;
                TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(currentGameState.SessionData.TrackDefinition.name, shared.acsStatic.trackSPlineLength);
                currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;
                currentGameState.SessionData.TrackDefinition.setGapPoints();
                GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                {
                    if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                    {
                        if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                        {
                            currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                        }
                    }
                }
                currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, distanceRoundTrack, currentGameState.Now);
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)
                    {
                        // just gone green, so get the session data.
                        if (currentGameState.SessionData.SessionType == SessionType.Race)
                        {
                            currentGameState.SessionData.JustGoneGreen = true;
                            if (currentGameState.SessionData.SessionHasFixedTime)
                            {
                                currentGameState.SessionData.SessionTotalRunTime = sessionTimeRemaining;
                                currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                                if (currentGameState.SessionData.SessionTotalRunTime == 0)
                                {
                                    Console.WriteLine("Setting session run time to 0");
                                }
                            }
                            currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                            currentGameState.SessionData.SessionNumberOfLaps = numberOfLapsInSession;
                        }
                        lapCountAtSector1End = -1;
                        currentGameState.SessionData.LeaderHasFinishedRace = false;
                        currentGameState.SessionData.NumCarsOverallAtStartOfSession = shared.acsChief.numVehicles;
                        currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.acsStatic.track + ":" + shared.acsStatic.trackConfiguration, shared.acsStatic.trackSPlineLength, shared.acsStatic.sectorCount);
                        if (currentGameState.SessionData.TrackDefinition.unknownTrack)
                        {
                            currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();
                        }
                        TrackDataContainer tdc = TrackData.TRACK_LANDMARKS_DATA.getTrackDataForTrackName(currentGameState.SessionData.TrackDefinition.name, shared.acsStatic.trackSPlineLength);
                        currentGameState.SessionData.TrackDefinition.trackLandmarks = tdc.trackLandmarks;
                        currentGameState.SessionData.TrackDefinition.isOval = tdc.isOval;                        
                        currentGameState.SessionData.TrackDefinition.setGapPoints();
                        GlobalBehaviourSettings.UpdateFromTrackDefinition(currentGameState.SessionData.TrackDefinition);
                        if (previousGameState != null && previousGameState.SessionData.TrackDefinition != null)
                        {
                            if (previousGameState.SessionData.TrackDefinition.name.Equals(currentGameState.SessionData.TrackDefinition.name))
                            {
                                if (previousGameState.hardPartsOnTrackData.hardPartsMapped)
                                {
                                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;
                                }
                            }
                        }
                        currentGameState.SessionData.DeltaTime = new DeltaTime(currentGameState.SessionData.TrackDefinition.trackLength, distanceRoundTrack, currentGameState.Now);

                        currentGameState.carClass = CarData.getCarClassForClassName(shared.acsStatic.carModel);                        
                        CarData.CLASS_ID = shared.acsStatic.carModel;
                        GlobalBehaviourSettings.UpdateFromCarClass(currentGameState.carClass);
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());
                        brakeTempThresholdsForPlayersCar = CarData.getBrakeTempThresholds(currentGameState.carClass);
                        // no tyre data in the block so get the default tyre types for this car
                        defaultTyreTypeForPlayersCar = CarData.getDefaultTyreType(currentGameState.carClass);
                        
                        currentGameState.TyreData.TyreTypeName = shared.acsGraphic.tyreCompound;
                        tyreTempThresholds = getTyreTempThresholds(currentGameState.carClass, currentGameState.TyreData.TyreTypeName);

                        if (currentGameState.SessionData.SessionHasFixedTime)
                        {
                            currentGameState.PitData.PitWindowStart = shared.acsStatic.PitWindowStart;
                            currentGameState.PitData.PitWindowEnd = shared.acsStatic.PitWindowEnd;
                        }
                        else
                        {
                            currentGameState.PitData.PitWindowStart = shared.acsStatic.PitWindowStart - 1;
                            currentGameState.PitData.PitWindowEnd = shared.acsStatic.PitWindowEnd - 1;
                        }
                        currentGameState.PitData.HasMandatoryPitStop = shared.acsStatic.PitWindowStart > 0 || shared.acsStatic.PitWindowEnd > 0;

                        if (previousGameState != null)
                        {
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            if (currentGameState.SessionData.SessionType != SessionType.Race)
                            {
                                currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                                currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                                currentGameState.SessionData.SessionTimeRemaining = previousGameState.SessionData.SessionTimeRemaining;
                                currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                            }
                        }

                        Console.WriteLine("Just gone green, session details...");
                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        if (previousGameState != null)
                        {
                            Console.WriteLine("previous SessionPhase " + previousGameState.SessionData.SessionPhase);
                        }
                        Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
                        Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("PitWindowStart " + currentGameState.PitData.PitWindowStart);
                        Console.WriteLine("PitWindowEnd " + currentGameState.PitData.PitWindowEnd);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime.ToString("0.000"));
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime.ToString("0.000"));
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName);
                    }
                }
                if (!currentGameState.SessionData.JustGoneGreen && previousGameState != null)
                {
                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.HasExtraLap = previousGameState.SessionData.HasExtraLap;
                    currentGameState.SessionData.NumCarsOverallAtStartOfSession = previousGameState.SessionData.NumCarsOverallAtStartOfSession;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = previousGameState.SessionData.PositionAtStartOfCurrentLap;
                    currentGameState.SessionData.SessionStartClassPosition = previousGameState.SessionData.SessionStartClassPosition;
                    currentGameState.SessionData.ClassPositionAtStartOfCurrentLap = previousGameState.SessionData.ClassPositionAtStartOfCurrentLap;
                    currentGameState.SessionData.CompletedLaps = previousGameState.SessionData.CompletedLaps;

                    currentGameState.OpponentData = previousGameState.OpponentData;
                    currentGameState.PitData.PitWindowStart = previousGameState.PitData.PitWindowStart;
                    currentGameState.PitData.PitWindowEnd = previousGameState.PitData.PitWindowEnd;
                    currentGameState.PitData.HasMandatoryPitStop = previousGameState.PitData.HasMandatoryPitStop;
                    currentGameState.PitData.HasMandatoryTyreChange = previousGameState.PitData.HasMandatoryTyreChange;
                    currentGameState.PitData.MandatoryTyreChangeRequiredTyreType = previousGameState.PitData.MandatoryTyreChangeRequiredTyreType;
                    currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                    currentGameState.PitData.MaxPermittedDistanceOnCurrentTyre = previousGameState.PitData.MaxPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.MinPermittedDistanceOnCurrentTyre = previousGameState.PitData.MinPermittedDistanceOnCurrentTyre;
                    currentGameState.PitData.OnInLap = previousGameState.PitData.OnInLap;
                    currentGameState.PitData.OnOutLap = previousGameState.PitData.OnOutLap;
                    // the other properties of PitData are updated each tick, and shouldn't be copied over here. Nasty...
                    currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState.SessionData.SessionTimesAtEndOfSectors;
                    currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
                    currentGameState.SessionData.GameTimeAtLastPositionFrontChange = previousGameState.SessionData.GameTimeAtLastPositionFrontChange;
                    currentGameState.SessionData.GameTimeAtLastPositionBehindChange = previousGameState.SessionData.GameTimeAtLastPositionBehindChange;
                    currentGameState.SessionData.LastSector1Time = previousGameState.SessionData.LastSector1Time;
                    currentGameState.SessionData.LastSector2Time = previousGameState.SessionData.LastSector2Time;
                    currentGameState.SessionData.LastSector3Time = previousGameState.SessionData.LastSector3Time;
                    currentGameState.SessionData.PlayerBestSector1Time = previousGameState.SessionData.PlayerBestSector1Time;
                    currentGameState.SessionData.PlayerBestSector2Time = previousGameState.SessionData.PlayerBestSector2Time;
                    currentGameState.SessionData.PlayerBestSector3Time = previousGameState.SessionData.PlayerBestSector3Time;
                    currentGameState.SessionData.PlayerBestLapSector1Time = previousGameState.SessionData.PlayerBestLapSector1Time;
                    currentGameState.SessionData.PlayerBestLapSector2Time = previousGameState.SessionData.PlayerBestLapSector2Time;
                    currentGameState.SessionData.PlayerBestLapSector3Time = previousGameState.SessionData.PlayerBestLapSector3Time;
                    currentGameState.SessionData.LapTimePrevious = previousGameState.SessionData.LapTimePrevious;
                    currentGameState.Conditions.samples = previousGameState.Conditions.samples;
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;
                    currentGameState.TyreData.TyreTypeName = previousGameState.TyreData.TyreTypeName;

                    currentGameState.SessionData.DeltaTime = previousGameState.SessionData.DeltaTime;
                    currentGameState.hardPartsOnTrackData = previousGameState.hardPartsOnTrackData;

                    currentGameState.SessionData.PlayerLapData = previousGameState.SessionData.PlayerLapData;

                    currentGameState.TimingData = previousGameState.TimingData;

                    currentGameState.SessionData.JustGoneGreenTime = previousGameState.SessionData.JustGoneGreenTime;
                }

                //------------------- Variable session data ---------------------------

                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    currentGameState.SessionData.SessionRunningTime = currentGameState.SessionData.SessionTotalRunTime - sessionTimeRemaining;
                    currentGameState.SessionData.SessionTimeRemaining = sessionTimeRemaining;
                }
                else
                {
                    currentGameState.SessionData.SessionRunningTime = (float)(currentGameState.Now - currentGameState.SessionData.SessionStartTime).TotalSeconds;
                }

                if (logUnknownTrackSectors && !isOnline && !isSinglePlayerPracticeSession)
                {
                    currentGameState.SessionData.SectorNumber = shared.acsGraphic.currentSectorIndex + 1;
                }
                else
                {
                    currentGameState.SessionData.SectorNumber = getCurrentSector(currentGameState.SessionData.TrackDefinition, distanceRoundTrack);
                }
                if (currentGameState.SessionData.OverallPosition == 1)
                {
                    currentGameState.SessionData.LeaderSectorNumber = currentGameState.SessionData.SectorNumber;
                }
                currentGameState.SessionData.IsNewSector = previousGameState == null || currentGameState.SessionData.SectorNumber != previousGameState.SessionData.SectorNumber;
                if (currentGameState.SessionData.IsNewSector && previousGameState.SessionData.SectorNumber == 1)
                {
                    lapCountAtSector1End = shared.acsGraphic.completedLaps;
                    // belt & braces, just in case we never had 'new lap data' so never updated the lap count on crossing the line
                    currentGameState.SessionData.CompletedLaps = lapCountAtSector1End;
                }
                currentGameState.SessionData.LapTimeCurrent = mapToFloatTime(shared.acsGraphic.iCurrentTime);

                if (previousGameState != null && previousGameState.SessionData.CurrentLapIsValid /*&& shared.acsStatic.penaltiesEnabled == 1*/)
                {
                    currentGameState.SessionData.CurrentLapIsValid = shared.acsPhysics.numberOfTyresOut < 3;
                }

                bool hasCrossedSFLine = currentGameState.SessionData.IsNewSector && currentGameState.SessionData.SectorNumber == 1;
                float lastLapTime = mapToFloatTime(shared.acsGraphic.iLastTime);
                currentGameState.SessionData.IsNewLap = currentGameState.HasNewLapData(previousGameState, lastLapTime, hasCrossedSFLine)
                    || ((lastSessionPhase == SessionPhase.Countdown)
                    && (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow));

                if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.readLandmarksForThisLap = false;
                    // correct IsNewSector so it's in sync with IsNewLap
                    currentGameState.SessionData.IsNewSector = true;
                    // if we have new lap data, update the lap count using the laps completed at sector1 end + 1, or the game provided data (whichever is bigger)
                    currentGameState.SessionData.CompletedLaps = Math.Max(lapCountAtSector1End + 1, shared.acsGraphic.completedLaps);

                    currentGameState.SessionData.playerCompleteLapWithProvidedLapTime(currentGameState.SessionData.OverallPosition, currentGameState.SessionData.SessionRunningTime,
                        lastLapTime, currentGameState.SessionData.CurrentLapIsValid, currentGameState.PitData.InPitlane, false,
                        shared.acsPhysics.roadTemp, shared.acsPhysics.airTemp, currentGameState.SessionData.SessionHasFixedTime,
                        currentGameState.SessionData.SessionTimeRemaining, numberOfSectorsOnTrack, currentGameState.TimingData);
                    currentGameState.SessionData.playerStartNewLap(currentGameState.SessionData.CompletedLaps + 1,
                        currentGameState.SessionData.OverallPosition, currentGameState.PitData.InPitlane, currentGameState.SessionData.SessionRunningTime);
                }
                else if (previousGameState != null && currentGameState.SessionData.SectorNumber == 1 && currentGameState.SessionData.IsNewSector && previousGameState.SessionData.SectorNumber != 0)
                {
                    // don't allow IsNewSector to be true if IsNewLap is not - roll back to the previous sector number and correct the flag
                    currentGameState.SessionData.SectorNumber = previousGameState.SessionData.SectorNumber;
                    currentGameState.SessionData.IsNewSector = false;
                }

                //Sector Log
                if (currentGameState.SessionData.TrackDefinition.unknownTrack && logUnknownTrackSectors && !isOnline && currentGameState.SessionData.IsNewSector &&
                    (shared.acsGraphic.currentSectorIndex + 1 == 2 || shared.acsGraphic.currentSectorIndex + 1 == 3))
                {
                    logSectorsForUnknownTracks(currentGameState.SessionData.TrackDefinition, distanceRoundTrack, shared.acsGraphic.currentSectorIndex + 1);
                }

                //Sector
                if (currentGameState.SessionData.IsNewSector && !currentGameState.SessionData.IsNewLap && 
                    previousGameState.SessionData.SectorNumber != 0 && currentGameState.SessionData.SessionRunningTime > 10)
                {
                    currentGameState.SessionData.playerAddCumulativeSectorData(previousGameState.SessionData.SectorNumber, currentGameState.SessionData.OverallPosition,
                        currentGameState.SessionData.LapTimeCurrent, currentGameState.SessionData.SessionRunningTime, currentGameState.SessionData.CurrentLapIsValid, false,
                        shared.acsPhysics.roadTemp, shared.acsPhysics.airTemp);
                }

                currentGameState.SessionData.Flag = mapToFlagEnum(currentFlag, disableYellowFlag);
                /*if (currentGameState.SessionData.Flag == FlagEnum.YELLOW && previousGameState != null && previousGameState.SessionData.Flag != FlagEnum.YELLOW)
                {
                    currentGameState.SessionData.YellowFlagStartTime = currentGameState.Now;
                }*/
                currentGameState.SessionData.NumCarsOverall = shared.acsChief.numVehicles;
                
                /*previousGameState != null && previousGameState.SessionData.IsNewLap == false &&
                    (shared.acsGraphic.completedLaps == previousGameState.SessionData.CompletedLaps + 1 || ((lastSessionPhase == SessionPhase.Countdown)
                    && (currentGameState.SessionData.SessionPhase == SessionPhase.Green || currentGameState.SessionData.SessionPhase == SessionPhase.FullCourseYellow)));
                */
                if (previousGameState != null)
                {
                    String stoppedInLandmark = currentGameState.SessionData.trackLandmarksTiming.updateLandmarkTiming(currentGameState.SessionData.TrackDefinition,
                        currentGameState.SessionData.SessionRunningTime, previousGameState.PositionAndMotionData.DistanceRoundTrack, distanceRoundTrack, playerVehicle.speedMS);
                    currentGameState.SessionData.stoppedInLandmark = shared.acsGraphic.isInPitLane == 1 ? null : stoppedInLandmark;
                    if (currentGameState.SessionData.IsNewLap)
                    {
                        currentGameState.SessionData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                    }
                }

                currentGameState.SessionData.DeltaTime.SetNextDeltaPoint(distanceRoundTrack, currentGameState.SessionData.CompletedLaps, playerVehicle.speedMS, currentGameState.Now);


                if (currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass == -1 ||
                    currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass > mapToFloatTime(playerVehicle.bestLapMS))
                {
                    currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = mapToFloatTime(playerVehicle.bestLapMS);
                }
                // get all the duplicate names
                List<string> driversToBeProcessed = new List<string>();
                List<string> duplicateNames = new List<string>();
                for (int i = 0; i < shared.acsChief.numVehicles; i++)
                {
                    String participantName = getNameFromBytes(shared.acsChief.vehicle[i].driverName).ToLower();
                    if (driversToBeProcessed.Contains(participantName))
                    {
                        if (!duplicateNames.Contains(participantName))
                        {
                            duplicateNames.Add(participantName);
                        }
                    }
                    else
                    {
                        driversToBeProcessed.Add(participantName);
                    }
                }

                for (int i = 0; i < shared.acsChief.numVehicles; i++)
                {
                    acsVehicleInfo participantStruct = shared.acsChief.vehicle[i];

                    String participantName = getNameFromBytes(participantStruct.driverName).ToLower();
                    OpponentData currentOpponentData = getOpponentForName(currentGameState, participantName);

                    if (i != 0 && participantName != null && participantName.Length > 0 && driversToBeProcessed.Contains(participantName))
                    {
                        if (currentOpponentData != null)
                        {
                            if (participantStruct.isConnected == 1)
                            {
                                driversToBeProcessed.Remove(participantName);
                                currentOpponentData.IsReallyDisconnectedCounter = 0;
                                if (previousGameState != null)
                                {
                                    int previousOpponentSectorNumber = 1;
                                    int previousOpponentCompletedLaps = 0;
                                    int previousOpponentPosition = 0;
                                    int currentOpponentSector = 0;
                                    Boolean previousOpponentIsEnteringPits = false;
                                    Boolean previousOpponentIsExitingPits = false;
                                     /* previous tick data for hasNewLapData check*/
                                    Boolean previousOpponentDataWaitingForNewLapData = false;
                                    DateTime previousOpponentNewLapDataTimerExpiry = DateTime.MaxValue;
                                    float previousOpponentLastLapTime = -1;
                                    Boolean previousOpponentLastLapValid = false;

                                    float[] previousOpponentWorldPosition = new float[] { 0, 0, 0 };
                                    float previousOpponentSpeed = 0;
                                    float previousDistanceRoundTrack = 0;
                                    int currentOpponentRacePosition = 0;
                                    OpponentData previousOpponentData = getOpponentForName(previousGameState, participantName);
                                    int previousCompletedLapsWhenHasNewLapDataWasLastTrue = -2;
                                    float previousOpponentGameTimeWhenLastCrossedStartFinishLine = -1;
                                    // store some previous opponent data that we'll need later
                                    if (previousOpponentData != null)
                                    {
                                        previousOpponentSectorNumber = previousOpponentData.CurrentSectorNumber;
                                        previousOpponentCompletedLaps = previousOpponentData.CompletedLaps;
                                        previousOpponentPosition = previousOpponentData.OverallPosition;
                                        previousOpponentIsEnteringPits = previousOpponentData.isEnteringPits();
                                        previousOpponentIsExitingPits = previousOpponentData.isExitingPits();
                                        previousOpponentWorldPosition = previousOpponentData.WorldPosition;
                                        previousOpponentSpeed = previousOpponentData.Speed;
                                        previousDistanceRoundTrack = previousOpponentData.DistanceRoundTrack;

                                        previousOpponentDataWaitingForNewLapData = previousOpponentData.WaitingForNewLapData;
                                        previousOpponentNewLapDataTimerExpiry = previousOpponentData.NewLapDataTimerExpiry;
                                        previousCompletedLapsWhenHasNewLapDataWasLastTrue = previousOpponentData.CompletedLapsWhenHasNewLapDataWasLastTrue;
                                        previousOpponentGameTimeWhenLastCrossedStartFinishLine = previousOpponentData.GameTimeWhenLastCrossedStartFinishLine;

                                        previousOpponentLastLapTime = previousOpponentData.LastLapTime;
                                        previousOpponentLastLapValid = previousOpponentData.LastLapValid;
                                        currentOpponentData.ClassPositionAtPreviousTick = previousOpponentData.ClassPosition;
                                        currentOpponentData.OverallPositionAtPreviousTick = previousOpponentData.OverallPosition;
                                    }
                                    float currentOpponentLapDistance = spLineLengthToDistanceRoundTrack(shared.acsStatic.trackSPlineLength, participantStruct.spLineLength);
                                    currentOpponentSector = getCurrentSector(currentGameState.SessionData.TrackDefinition, currentOpponentLapDistance);
                                    
                                    currentOpponentData.DeltaTime.SetNextDeltaPoint(currentOpponentLapDistance, participantStruct.lapCount,
                                        participantStruct.speedMS, currentGameState.Now);
                                    
                                    Boolean useCarLeaderBoardPosition = previousOpponentSectorNumber != currentOpponentSector && currentOpponentSector == 1;

                                    if (shared.acsGraphic.session == AC_SESSION_TYPE.AC_PRACTICE || shared.acsGraphic.session == AC_SESSION_TYPE.AC_QUALIFY
                                        || currentFlag == AC_FLAG_TYPE.AC_CHECKERED_FLAG || useCarLeaderBoardPosition)
                                    {
                                        currentOpponentRacePosition = participantStruct.carLeaderboardPosition;
                                    }
                                    else
                                    {
                                        currentOpponentRacePosition = participantStruct.carRealTimeLeaderboardPosition + realTimeLeaderBoardValid;
                                    }
                                    int currentOpponentLapsCompleted = participantStruct.lapCount;

                                    //Using same approach here as in R3E
                                    Boolean finishedAllottedRaceLaps = currentGameState.SessionData.SessionNumberOfLaps > 0 && currentGameState.SessionData.SessionNumberOfLaps == currentOpponentLapsCompleted;
                                    Boolean finishedAllottedRaceTime = false;
                                    if (currentGameState.SessionData.HasExtraLap &&
                                        currentGameState.SessionData.SessionType == SessionType.Race)
                                    {
                                        if (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
                                            previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                                        {
                                            if (!currentOpponentData.HasStartedExtraLap)
                                            {
                                                currentOpponentData.HasStartedExtraLap = true;
                                            }
                                            else
                                            {
                                                finishedAllottedRaceTime = true;
                                            }
                                        }
                                    }
                                    else if (currentGameState.SessionData.SessionTotalRunTime > 0 && currentGameState.SessionData.SessionTimeRemaining <= 0 &&
                                        previousOpponentCompletedLaps < currentOpponentLapsCompleted)
                                    {
                                        finishedAllottedRaceTime = true;
                                    }

                                    if (currentOpponentRacePosition == 1 && (finishedAllottedRaceTime || finishedAllottedRaceLaps))
                                    {
                                        currentGameState.SessionData.LeaderHasFinishedRace = true;
                                    }

                                    Boolean isEnteringPits = participantStruct.isCarInPitline == 1 && currentOpponentSector == ACSGameStateMapper.numberOfSectorsOnTrack;
                                    Boolean isLeavingPits = participantStruct.isCarInPitline == 1 && currentOpponentSector == 1;

                                    float secondsSinceLastUpdate = (float)new TimeSpan(currentGameState.Ticks - previousGameState.Ticks).TotalSeconds;

                                    upateOpponentData(currentOpponentData, previousOpponentData, currentOpponentRacePosition, participantStruct.carLeaderboardPosition, currentOpponentLapsCompleted,
                                        currentOpponentSector, mapToFloatTime(participantStruct.currentLapTimeMS), mapToFloatTime(participantStruct.lastLapTimeMS),
                                        participantStruct.isCarInPitline == 1, participantStruct.currentLapInvalid == 0,
                                        currentGameState.SessionData.SessionRunningTime, secondsSinceLastUpdate,
                                        new float[] { participantStruct.worldPosition.x, participantStruct.worldPosition.z }, participantStruct.speedMS, currentOpponentLapDistance,
                                        currentGameState.SessionData.SessionHasFixedTime, currentGameState.SessionData.SessionTimeRemaining,
                                        shared.acsPhysics.airTemp, shared.acsPhysics.roadTemp, currentGameState.SessionData.SessionType == SessionType.Race,
                                        currentGameState.SessionData.TrackDefinition.distanceForNearPitEntryChecks,
                                        previousOpponentCompletedLaps, previousOpponentDataWaitingForNewLapData,
                                        previousOpponentNewLapDataTimerExpiry, previousOpponentLastLapTime, previousOpponentLastLapValid, previousCompletedLapsWhenHasNewLapDataWasLastTrue,
                                        previousOpponentGameTimeWhenLastCrossedStartFinishLine,
                                        currentGameState.TimingData, currentGameState.carClass);

                                    if (previousOpponentData != null)
                                    {
                                        currentOpponentData.trackLandmarksTiming = previousOpponentData.trackLandmarksTiming;
                                        String stoppedInLandmark = currentOpponentData.trackLandmarksTiming.updateLandmarkTiming(
                                            currentGameState.SessionData.TrackDefinition, currentGameState.SessionData.SessionRunningTime,
                                            previousDistanceRoundTrack, currentOpponentData.DistanceRoundTrack, currentOpponentData.Speed);
                                        currentOpponentData.stoppedInLandmark = participantStruct.isCarInPitline == 1 ? null : stoppedInLandmark;
                                    }
                                    if (currentGameState.SessionData.JustGoneGreen)
                                    {
                                        currentOpponentData.trackLandmarksTiming = new TrackLandmarksTiming();
                                    }
                                    if (currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass == -1 ||
                                            currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass > mapToFloatTime(participantStruct.bestLapMS))
                                    {
                                        currentGameState.SessionData.SessionFastestLapTimeFromGamePlayerClass = mapToFloatTime(participantStruct.bestLapMS);
                                    }
                                    if (currentOpponentData.IsNewLap)
                                    {
                                        currentOpponentData.trackLandmarksTiming.cancelWaitingForLandmarkEnd();
                                        if (currentOpponentData.CurrentBestLapTime > 0)
                                        {
                                            if (currentGameState.SessionData.OpponentsLapTimeSessionBestOverall == -1 ||
                                                currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestOverall)
                                            {
                                                currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = currentOpponentData.CurrentBestLapTime;
                                                if (currentGameState.SessionData.OverallSessionBestLapTime == -1 ||
                                                    currentGameState.SessionData.OverallSessionBestLapTime > currentOpponentData.CurrentBestLapTime)
                                                {
                                                    currentGameState.SessionData.OverallSessionBestLapTime = currentOpponentData.CurrentBestLapTime;
                                                }
                                            }
                                            if (CarData.IsCarClassEqual(currentOpponentData.CarClass, currentGameState.carClass))
                                            {
                                                if (currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass == -1 ||
                                                    currentOpponentData.CurrentBestLapTime < currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass)
                                                {
                                                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = currentOpponentData.CurrentBestLapTime;
                                                    if (currentGameState.SessionData.PlayerClassSessionBestLapTime == -1 ||
                                                        currentGameState.SessionData.PlayerClassSessionBestLapTime > currentOpponentData.CurrentBestLapTime)
                                                    {
                                                        currentGameState.SessionData.PlayerClassSessionBestLapTime = currentOpponentData.CurrentBestLapTime;
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                            else if (!duplicateNames.Contains(participantName))
                            {
                                // this drivers has disconnected, but only remove him from the OpponentData if he's not a duplicate
                                driversToBeProcessed.Remove(participantName);
                                currentOpponentData.IsActive = false;
                                currentOpponentData.IsReallyDisconnectedCounter++;
                                if (currentOpponentData.IsReallyDisconnectedCounter > 5)
                                {
                                    Console.WriteLine("Removing " + participantName + " -> " + currentOpponentData.DriverRawName);
                                    currentGameState.OpponentData.Remove(participantName);
                                }

                            }
                        }
                        else
                        {
                            if (participantStruct.isConnected == 1 && participantName != null && participantName.Length > 0)
                            {
                                addOpponentForName(participantName, createOpponentData(participantStruct, true, CarData.getCarClassForClassName(getNameFromBytes(participantStruct.carModel)),
                                    shared.acsStatic.trackSPlineLength), currentGameState);
                            }
                        }
                    }

                }

                currentGameState.sortClassPositions();
                currentGameState.setPracOrQualiDeltas();
            }

            // engine/transmission data
            currentGameState.EngineData.EngineRpm = shared.acsPhysics.rpms;
            currentGameState.EngineData.MaxEngineRpm = shared.acsStatic.maxRpm;
            currentGameState.EngineData.MinutesIntoSessionBeforeMonitoring = 2;

            currentGameState.FuelData.FuelCapacity = shared.acsStatic.maxFuel;
            currentGameState.FuelData.FuelLeft = shared.acsPhysics.fuel;
            currentGameState.FuelData.FuelUseActive = shared.acsStatic.aidFuelRate > 0;

            currentGameState.TransmissionData.Gear = shared.acsPhysics.gear - 1;

            currentGameState.ControlData.BrakePedal = shared.acsPhysics.brake;
            currentGameState.ControlData.ThrottlePedal = shared.acsPhysics.gas;
            currentGameState.ControlData.ClutchPedal = shared.acsPhysics.clutch;

            // penalty data
            currentGameState.PenaltiesData.HasDriveThrough = currentFlag == AC_FLAG_TYPE.AC_PENALTY_FLAG && shared.acsGraphic.penaltyTime <= 0;
            currentGameState.PenaltiesData.HasSlowDown = currentFlag == AC_FLAG_TYPE.AC_PENALTY_FLAG && shared.acsGraphic.penaltyTime > 0;

            // motion data
            currentGameState.PositionAndMotionData.CarSpeed = playerVehicle.speedMS;
            currentGameState.PositionAndMotionData.DistanceRoundTrack = distanceRoundTrack;

            //------------------------ Pit stop data -----------------------
            currentGameState.PitData.InPitlane = shared.acsGraphic.isInPitLane == 1;

            if (currentGameState.PitData.InPitlane)
            {
                if (previousGameState != null && !previousGameState.PitData.InPitlane)
                {
                    if (currentGameState.SessionData.SessionRunningTime > 30 && currentGameState.SessionData.SessionType == SessionType.Race)
                    {
                        currentGameState.PitData.NumPitStops++;
                    }
                    currentGameState.PitData.OnInLap = true;
                    currentGameState.PitData.OnOutLap = false;
                }
                else if (currentGameState.SessionData.IsNewLap)
                {
                    currentGameState.PitData.OnInLap = false;
                    currentGameState.PitData.OnOutLap = true;
                }
            }
            else if (previousGameState != null && previousGameState.PitData.InPitlane)
            {
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = true;
                currentGameState.PitData.IsAtPitExit = true;
            }
            else if (currentGameState.SessionData.IsNewLap)
            {
                // starting a new lap while not in the pitlane so clear the in / out lap flags
                currentGameState.PitData.OnInLap = false;
                currentGameState.PitData.OnOutLap = false;
            }

            if (previousGameState != null && currentGameState.PitData.OnOutLap && previousGameState.PitData.InPitlane && !currentGameState.PitData.InPitlane)
            {
                currentGameState.PitData.IsAtPitExit = true;
            }

            if (currentGameState.PitData.HasMandatoryPitStop)
            {
                int lapsOrMinutes;
                if (currentGameState.SessionData.SessionHasFixedTime)
                {
                    lapsOrMinutes = (int)Math.Floor(currentGameState.SessionData.SessionRunningTime / 60f);
                }
                else
                {
                    lapsOrMinutes = playerVehicle.lapCount;
                }
                currentGameState.PitData.PitWindow = mapToPitWindow(lapsOrMinutes, currentGameState.PitData.InPitlane,
                    currentGameState.PitData.PitWindowStart, currentGameState.PitData.PitWindowEnd, shared.acsGraphic.MandatoryPitDone == 1);
            }
            else
            {
                currentGameState.PitData.PitWindow = PitWindow.Unavailable;
            }

            currentGameState.PitData.IsMakingMandatoryPitStop = (currentGameState.PitData.PitWindow == PitWindow.StopInProgress);
            if (previousGameState != null)
            {
                currentGameState.PitData.MandatoryPitStopCompleted = previousGameState.PitData.MandatoryPitStopCompleted || shared.acsGraphic.MandatoryPitDone == 1;
            }

            //damage data
            if (shared.acsChief.isInternalMemoryModuleLoaded == 1)
            {
                currentGameState.CarDamageData.DamageEnabled = true;
                currentGameState.CarDamageData.SuspensionDamageStatus = CornerData.getCornerData(suspensionDamageThresholds,
                    playerVehicle.suspensionDamage[0], playerVehicle.suspensionDamage[1], playerVehicle.suspensionDamage[2], playerVehicle.suspensionDamage[3]);

                currentGameState.CarDamageData.OverallEngineDamage = mapToEngineDamageLevel(playerVehicle.engineLifeLeft);

                currentGameState.CarDamageData.OverallAeroDamage = mapToAeroDamageLevel(shared.acsPhysics.carDamage[0] +
                    shared.acsPhysics.carDamage[1] +
                    shared.acsPhysics.carDamage[2] +
                    shared.acsPhysics.carDamage[3]);
            }
            else
            {
                currentGameState.CarDamageData.DamageEnabled = false;
                playerVehicle.tyreInflation[0] = 1;
                playerVehicle.tyreInflation[1] = 1;
                playerVehicle.tyreInflation[2] = 1;
                playerVehicle.tyreInflation[3] = 1;
            }

            //tyre data
            currentGameState.TyreData.HasMatchedTyreTypes = true;
            currentGameState.TyreData.TyreWearActive = shared.acsStatic.aidTireRate > 0;

            currentGameState.TyreData.FrontLeftPressure = playerVehicle.tyreInflation[0] == 1.0f ? shared.acsPhysics.wheelsPressure[0] * 6.894f : 0.0f;
            currentGameState.TyreData.FrontRightPressure = playerVehicle.tyreInflation[1] == 1.0f ? shared.acsPhysics.wheelsPressure[1] * 6.894f : 0.0f;
            currentGameState.TyreData.RearLeftPressure = playerVehicle.tyreInflation[2] == 1.0f ? shared.acsPhysics.wheelsPressure[2] * 6.894f : 0.0f;
            currentGameState.TyreData.RearRightPressure = playerVehicle.tyreInflation[3] == 1.0f ? shared.acsPhysics.wheelsPressure[3] * 6.894f : 0.0f;

            String currentTyreCompound = shared.acsGraphic.tyreCompound;
            if (previousGameState != null && !previousGameState.TyreData.TyreTypeName.Equals(currentTyreCompound))
            {
                tyreTempThresholds = getTyreTempThresholds(currentGameState.carClass, currentTyreCompound);
                currentGameState.TyreData.TyreTypeName = currentTyreCompound;
            }
            //Front Left
            currentGameState.TyreData.FrontLeft_CenterTemp = shared.acsPhysics.tyreTempM[0];
            currentGameState.TyreData.FrontLeft_LeftTemp = shared.acsPhysics.tyreTempO[0];
            currentGameState.TyreData.FrontLeft_RightTemp = shared.acsPhysics.tyreTempI[0];
            currentGameState.TyreData.FrontLeftTyreType = defaultTyreTypeForPlayersCar;
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontLeftTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = currentGameState.TyreData.FrontLeft_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.FrontLeft_CenterTemp > previousGameState.TyreData.PeakFrontLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontLeftTemperatureForLap = currentGameState.TyreData.FrontLeft_CenterTemp;
            }
            //Front Right
            currentGameState.TyreData.FrontRight_CenterTemp = shared.acsPhysics.tyreTempM[1];
            currentGameState.TyreData.FrontRight_LeftTemp = shared.acsPhysics.tyreTempI[1];
            currentGameState.TyreData.FrontRight_RightTemp = shared.acsPhysics.tyreTempO[1];
            currentGameState.TyreData.FrontRightTyreType = defaultTyreTypeForPlayersCar;
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakFrontRightTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = currentGameState.TyreData.FrontRight_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.FrontRight_CenterTemp > previousGameState.TyreData.PeakFrontRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakFrontRightTemperatureForLap = currentGameState.TyreData.FrontRight_CenterTemp;
            }
            //Rear Left
            currentGameState.TyreData.RearLeft_CenterTemp = shared.acsPhysics.tyreTempM[2];
            currentGameState.TyreData.RearLeft_LeftTemp = shared.acsPhysics.tyreTempO[2];
            currentGameState.TyreData.RearLeft_RightTemp = shared.acsPhysics.tyreTempI[2];
            currentGameState.TyreData.RearLeftTyreType = defaultTyreTypeForPlayersCar;
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearLeftTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = currentGameState.TyreData.RearLeft_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.RearLeft_CenterTemp > previousGameState.TyreData.PeakRearLeftTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearLeftTemperatureForLap = currentGameState.TyreData.RearLeft_CenterTemp;
            }
            //Rear Right
            currentGameState.TyreData.RearRight_CenterTemp = shared.acsPhysics.tyreTempM[3];
            currentGameState.TyreData.RearRight_LeftTemp = shared.acsPhysics.tyreTempI[3];
            currentGameState.TyreData.RearRight_RightTemp = shared.acsPhysics.tyreTempO[3];
            currentGameState.TyreData.RearRightTyreType = defaultTyreTypeForPlayersCar;
            if (currentGameState.SessionData.IsNewLap || currentGameState.TyreData.PeakRearRightTemperatureForLap == 0)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = currentGameState.TyreData.RearRight_CenterTemp;
            }
            else if (previousGameState == null || currentGameState.TyreData.RearRight_CenterTemp > previousGameState.TyreData.PeakRearRightTemperatureForLap)
            {
                currentGameState.TyreData.PeakRearRightTemperatureForLap = currentGameState.TyreData.RearRight_CenterTemp;
            }

            currentGameState.TyreData.TyreTempStatus = CornerData.getCornerData(tyreTempThresholds, currentGameState.TyreData.PeakFrontLeftTemperatureForLap,
                    currentGameState.TyreData.PeakFrontRightTemperatureForLap, currentGameState.TyreData.PeakRearLeftTemperatureForLap,
                    currentGameState.TyreData.PeakRearRightTemperatureForLap);
            
            Boolean currentTyreValid = currentTyreCompound != null && currentTyreCompound.Length > 0 &&
                acTyres.Count > 0 && acTyres.ContainsKey(currentTyreCompound);

            if (currentTyreValid)
            {

                float currentTyreWearMinimumValue = acTyres[currentTyreCompound].tyreWearMinimumValue;
                currentGameState.TyreData.FrontLeftPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[0], currentTyreWearMinimumValue);
                currentGameState.TyreData.FrontRightPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[0], currentTyreWearMinimumValue);
                currentGameState.TyreData.RearLeftPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[0], currentTyreWearMinimumValue);
                currentGameState.TyreData.RearRightPercentWear = getTyreWearPercentage(shared.acsPhysics.tyreWear[0], currentTyreWearMinimumValue);
                if (!currentGameState.PitData.OnOutLap)
                {
                    currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(acTyres[currentTyreCompound].tyreWearThresholdsForAC,
                        currentGameState.TyreData.FrontLeftPercentWear, currentGameState.TyreData.FrontRightPercentWear,
                        currentGameState.TyreData.RearLeftPercentWear, currentGameState.TyreData.RearRightPercentWear);
                }
                else
                {
                    currentGameState.TyreData.TyreConditionStatus = CornerData.getCornerData(acTyres[currentTyreCompound].tyreWearThresholdsForAC, -1f, -1f, -1f, -1f);
                }
            }

            currentGameState.PenaltiesData.IsOffRacingSurface = shared.acsPhysics.numberOfTyresOut > 2;
            if (!currentGameState.PitData.OnOutLap && previousGameState != null && !previousGameState.PenaltiesData.IsOffRacingSurface && currentGameState.PenaltiesData.IsOffRacingSurface &&
                !(shared.acsGraphic.session == AC_SESSION_TYPE.AC_RACE && isCountDown))
            {
                currentGameState.PenaltiesData.CutTrackWarnings = previousGameState.PenaltiesData.CutTrackWarnings + 1;
            }


            if (playerVehicle.speedMS > 7 && currentGameState.carClass != null)
            {
                float minRotatingSpeed = (float)Math.PI * playerVehicle.speedMS / currentGameState.carClass.maxTyreCircumference;
                currentGameState.TyreData.LeftFrontIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[0]) < minRotatingSpeed;
                currentGameState.TyreData.RightFrontIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[1]) < minRotatingSpeed;
                currentGameState.TyreData.LeftRearIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[2]) < minRotatingSpeed;
                currentGameState.TyreData.RightRearIsLocked = Math.Abs(shared.acsPhysics.wheelAngularSpeed[3]) < minRotatingSpeed;

                float maxRotatingSpeed = 3 * (float)Math.PI * playerVehicle.speedMS / currentGameState.carClass.minTyreCircumference;
                currentGameState.TyreData.LeftFrontIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[0]) > maxRotatingSpeed;
                currentGameState.TyreData.RightFrontIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[1]) > maxRotatingSpeed;
                currentGameState.TyreData.LeftRearIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[2]) > maxRotatingSpeed;
                currentGameState.TyreData.RightRearIsSpinning = Math.Abs(shared.acsPhysics.wheelAngularSpeed[3]) > maxRotatingSpeed;
            }

            //conditions
            if (currentGameState.Now > nextConditionsSampleDue)
            {
                nextConditionsSampleDue = currentGameState.Now.Add(ConditionsMonitor.ConditionsSampleFrequency);
                currentGameState.Conditions.addSample(currentGameState.Now, currentGameState.SessionData.CompletedLaps, currentGameState.SessionData.SectorNumber,
                    shared.acsPhysics.airTemp, shared.acsPhysics.roadTemp, 0, 0, 0, 0, 0, currentGameState.SessionData.IsNewLap);
            }

            if (currentGameState.SessionData.TrackDefinition != null)
            {
                CrewChief.trackName = currentGameState.SessionData.TrackDefinition.name;
            }
            if (currentGameState.carClass != null)
            {
                CrewChief.carClass = currentGameState.carClass.carClassEnum;
            }
            CrewChief.distanceRoundTrack = currentGameState.PositionAndMotionData.DistanceRoundTrack;
            CrewChief.viewingReplay = false;

            currentGameState.PositionAndMotionData.Orientation.Pitch = shared.acsPhysics.pitch;
            currentGameState.PositionAndMotionData.Orientation.Roll = shared.acsPhysics.roll;
            currentGameState.PositionAndMotionData.Orientation.Yaw = shared.acsPhysics.heading;

            if (currentGameState.SessionData.IsNewLap)
            {
                if (currentGameState.hardPartsOnTrackData.updateHardPartsForNewLap(currentGameState.SessionData.LapTimePrevious))
                {
                    currentGameState.SessionData.TrackDefinition.adjustGapPoints(currentGameState.hardPartsOnTrackData.processedHardPartsForBestLap);
                }
            }
            else if (!currentGameState.PitData.OnOutLap && !currentGameState.SessionData.TrackDefinition.isOval && 
                !(currentGameState.SessionData.SessionType == SessionType.Race && 
                   (currentGameState.SessionData.CompletedLaps < 1 || (GameStateData.useManualFormationLap && currentGameState.SessionData.CompletedLaps < 2))))
            {
                currentGameState.hardPartsOnTrackData.mapHardPartsOnTrack(currentGameState.ControlData.BrakePedal, currentGameState.ControlData.ThrottlePedal,
                    currentGameState.PositionAndMotionData.DistanceRoundTrack, currentGameState.SessionData.CurrentLapIsValid, currentGameState.SessionData.TrackDefinition.trackLength);
            }

            return currentGameState;
        }

        private List<CornerData.EnumWithThresholds> getTyreTempThresholds(CarData.CarClass carClass, string currentTyreCompound)
        {
            List<CornerData.EnumWithThresholds> tyreTempThresholds = new List<CornerData.EnumWithThresholds>();
            CarData.TyreTypeData tyreTypeData = null;
            AcTyres acTyre = null;
            if(carClass.acTyreTypeData.TryGetValue(currentTyreCompound, out tyreTypeData))
            {
                tyreTempThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COLD, -10000f, tyreTypeData.maxColdTyreTemp));
                tyreTempThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.WARM, tyreTypeData.maxColdTyreTemp, tyreTypeData.maxWarmTyreTemp));
                tyreTempThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.HOT, tyreTypeData.maxWarmTyreTemp, tyreTypeData.maxHotTyreTemp));
                tyreTempThresholds.Add(new CornerData.EnumWithThresholds(TyreTemp.COOKING, tyreTypeData.maxHotTyreTemp, 10000f));
                Console.WriteLine("Using user defined temperature thresholds for TyreType: " + currentTyreCompound);
            }
            else if (acTyres.TryGetValue(currentTyreCompound, out acTyre))
            {
                tyreTempThresholds = acTyre.tyreTempThresholdsForAC;
                Console.WriteLine("Using buildin defined temperature thresholds for TyreType: " + currentTyreCompound);
            }
            else
            {
                tyreTempThresholds = CarData.getTyreTempThresholds(carClass);
                Console.WriteLine("Using temperature thresholds for TyreType: " + carClass.defaultTyreType.ToString() +
                    " maxColdTyreTemp: " + tyreTempThresholds[0].upperThreshold + " maxWarmTyreTemp: " + tyreTempThresholds[1].upperThreshold +
                    " maxHotTyreTemp: " + tyreTempThresholds[2].upperThreshold);
            }
            return tyreTempThresholds;
        }

        private SessionPhase mapToSessionPhase(SessionType sessionType, AC_FLAG_TYPE flag, AC_STATUS status, Boolean isCountdown,
            SessionPhase previousSessionPhase, float sessionTimeRemaining, float sessionRunTime, bool isInPitLane, int lapCount, Boolean raceIsFinished)
        {
            if (status == AC_STATUS.AC_PAUSE)
                return previousSessionPhase;

            if (sessionType == SessionType.Race)
            {
                if (isCountdown)
                {
                    return SessionPhase.Countdown;
                }
                else if ((flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG && raceIsFinished)
                    || (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG && sessionTimeRemaining <= 0))
                {
                    return SessionPhase.Finished;
                }
                else if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
                {
                    return SessionPhase.Checkered;
                }
                else if (!isCountdown)
                {
                    return SessionPhase.Green;
                }
                return previousSessionPhase;

            }
            else if (sessionType == SessionType.Practice || sessionType == SessionType.HotLap)
            {
                if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG || sessionTimeRemaining < 1)
                {
                    return SessionPhase.Finished;
                }
                if (isInPitLane && lapCount == 0)
                {
                    return SessionPhase.Countdown;
                }
                return SessionPhase.Green;

            }
            else if (sessionType == SessionType.Qualify)
            {
                if (isInPitLane && lapCount == 0)
                {
                    return SessionPhase.Countdown;
                }
                else if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG && isInPitLane)
                {
                    return SessionPhase.Finished;
                }
                return SessionPhase.Green;
            }
            return SessionPhase.Unavailable;
        }

        private void upateOpponentData(OpponentData opponentData, OpponentData previousOpponentData, int racePosition, int leaderBoardPosition, int completedLaps, int sector,
            float completedLapTime, float lastLapTime, Boolean isInPits, Boolean lapIsValid, float sessionRunningTime, float secondsSinceLastUpdate,
            float[] currentWorldPosition, float speed, float distanceRoundTrack, Boolean sessionLengthIsTime, float sessionTimeRemaining,
            float airTemperature, float trackTempreture, Boolean isRace, float nearPitEntryPointDistance,
            /* previous tick data for hasNewLapData check*/
            int previousOpponentDataLapsCompleted, Boolean previousOpponentDataWaitingForNewLapData,
            DateTime previousOpponentNewLapDataTimerExpiry, float previousOpponentLastLapTime, Boolean previousOpponentLastLapValid,
            int previousCompletedLapsWhenHasNewLapDataWasLastTrue, float previousOpponentGameTimeWhenLastCrossedStartFinishLine,
            TimingData timingData, CarData.CarClass playerCarClass)
        {
            if (opponentData.CurrentSectorNumber == 0)
            {
                opponentData.CurrentSectorNumber = sector;
            }
            float previousDistanceRoundTrack = opponentData.DistanceRoundTrack;
            opponentData.DistanceRoundTrack = distanceRoundTrack;
            Boolean validSpeed = true;
            if (speed > 500)
            {
                // faster than 500m/s (1000+mph) suggests the player has quit to the pit. Might need to reassess this as the data are quite noisy
                validSpeed = false;
                opponentData.Speed = 0;
            }
            opponentData.Speed = speed;
            if (opponentData.OverallPosition != racePosition)
            {
                opponentData.SessionTimeAtLastPositionChange = sessionRunningTime;
            }
            opponentData.OverallPosition = racePosition;
            if (previousDistanceRoundTrack < nearPitEntryPointDistance && opponentData.DistanceRoundTrack > nearPitEntryPointDistance)
            {
                opponentData.PositionOnApproachToPitEntry = opponentData.OverallPosition;
            }
            opponentData.WorldPosition = currentWorldPosition;
            opponentData.IsNewLap = false;
            opponentData.JustEnteredPits = !opponentData.InPits && isInPits;
            if (opponentData.JustEnteredPits)
            {
                opponentData.NumPitStops++;
            }
            opponentData.InPits = isInPits;

            bool hasCrossedSFline = opponentData.CurrentSectorNumber == ACSGameStateMapper.numberOfSectorsOnTrack && sector == 1;

            bool hasNewLapData = opponentData.HasNewLapData(lastLapTime, hasCrossedSFline, completedLaps, isRace, sessionRunningTime, previousOpponentDataWaitingForNewLapData,
                 previousOpponentNewLapDataTimerExpiry, previousOpponentLastLapTime, previousOpponentLastLapValid, previousCompletedLapsWhenHasNewLapDataWasLastTrue, previousOpponentGameTimeWhenLastCrossedStartFinishLine);

            if (opponentData.CurrentSectorNumber == ACSGameStateMapper.numberOfSectorsOnTrack && sector == ACSGameStateMapper.numberOfSectorsOnTrack && (!lapIsValid || !validSpeed))
            {
                // special case for s3 - need to invalidate lap immediately
                opponentData.InvalidateCurrentLap();
            }
            if (opponentData.CurrentSectorNumber != sector || hasNewLapData)
            {
                if (hasNewLapData)
                {
                    int correctedLapCount = Math.Max(completedLaps, opponentData.lapCountAtSector1End + 1);
                    // if we have new lap data, we must be in sector 1
                    opponentData.CurrentSectorNumber = 1;
                    if (opponentData.OpponentLapData.Count > 0)
                    {
                        // special case here: if there's only 1 lap in the list, and it's marked as an in-lap, and we don't have a laptime, remove it.
                        // This is because we might have created a new LapData entry to hold a partially completed in-lap if we join mid-session, but
                        // this also results in each opponent having a spurious 'empty' LapData element.
                        if (opponentData.OpponentLapData.Count == 1 && opponentData.OpponentLapData[0].InLap && lastLapTime == 0)
                        {
                            opponentData.OpponentLapData.Clear();
                        }
                        else
                        {
                            opponentData.CompleteLapWithProvidedLapTime(leaderBoardPosition, sessionRunningTime, lastLapTime, isInPits,
                                false, trackTempreture, airTemperature, sessionLengthIsTime, sessionTimeRemaining, ACSGameStateMapper.numberOfSectorsOnTrack,
                                timingData, CarData.IsCarClassEqual(opponentData.CarClass, playerCarClass));
                        }
                    }

                    opponentData.StartNewLap(correctedLapCount + 1, leaderBoardPosition, isInPits, sessionRunningTime, false, trackTempreture, airTemperature);
                    opponentData.IsNewLap = true;
                    opponentData.CompletedLaps = correctedLapCount;
                    // recheck the car class here?
                }
                else if (opponentData.OpponentLapData.Count > 0 &&
                    ((opponentData.CurrentSectorNumber == 1 && sector == 2) || 
                     (opponentData.CurrentSectorNumber == 2 && sector == 3)))
                {
                    opponentData.AddCumulativeSectorData(opponentData.CurrentSectorNumber, racePosition, completedLapTime, sessionRunningTime,
                        lapIsValid && validSpeed, false, trackTempreture, airTemperature);

                    // if we've just finished sector 1, capture the laps completed (and ensure the CompleteLaps count is up to date)
                    if (opponentData.CurrentSectorNumber == 1)
                    {
                        opponentData.lapCountAtSector1End = completedLaps;
                        opponentData.CompletedLaps = completedLaps;
                    }

                    // only update the sector number if it's one of the above cases. This prevents us from moving the opponent sector number to 1 before
                    // he has new lap data
                    opponentData.CurrentSectorNumber = sector;
                }
            }
            if (sector == ACSGameStateMapper.numberOfSectorsOnTrack && isInPits)
            {
                opponentData.setInLap();
            }
        }

        private OpponentData createOpponentData(acsVehicleInfo participantStruct, Boolean loadDriverName, CarData.CarClass carClass, float trackSplineLength)
        {
            OpponentData opponentData = new OpponentData();
            String participantName = getNameFromBytes(participantStruct.driverName).ToLower();
            opponentData.DriverRawName = participantName;
            opponentData.DriverNameSet = true;
            if (participantName != null && participantName.Length > 0 && loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(opponentData.DriverRawName);
            }

            opponentData.OverallPosition = (int)participantStruct.carLeaderboardPosition;
            opponentData.CompletedLaps = (int)participantStruct.lapCount;
            opponentData.CurrentSectorNumber = 0;
            opponentData.WorldPosition = new float[] { participantStruct.worldPosition.x, participantStruct.worldPosition.z };
            opponentData.DistanceRoundTrack = spLineLengthToDistanceRoundTrack(trackSplineLength, participantStruct.spLineLength);
            opponentData.DeltaTime = new DeltaTime(trackSplineLength, opponentData.DistanceRoundTrack, DateTime.UtcNow);
            opponentData.CarClass = carClass;
            opponentData.IsActive = true;
            String nameToLog = opponentData.DriverRawName == null ? "unknown" : opponentData.DriverRawName;
            Console.WriteLine("New driver " + nameToLog + " is using car class " + opponentData.CarClass.getClassIdentifier());
            return opponentData;
        }

        public static void addOpponentForName(String name, OpponentData opponentData, GameStateData gameState)
        {
            if (name == null || name.Length == 0)
            {
                return;
            }
            if (gameState.OpponentData == null)
            {
                gameState.OpponentData = new Dictionary<string, OpponentData>();
            }
            gameState.OpponentData.Remove(name);
            gameState.OpponentData.Add(name, opponentData);
        }

        public float mapToFloatTime(int time)
        {
            TimeSpan ts = TimeSpan.FromTicks(time);
            return (float)ts.TotalMilliseconds * 10;
        }

        private FlagEnum mapToFlagEnum(AC_FLAG_TYPE flag, Boolean disableYellowFlag)
        {
            if (flag == AC_FLAG_TYPE.AC_CHECKERED_FLAG)
            {
                return FlagEnum.CHEQUERED;
            }
            else if (flag == AC_FLAG_TYPE.AC_BLACK_FLAG)
            {
                return FlagEnum.BLACK;
            }
            else if (flag == AC_FLAG_TYPE.AC_YELLOW_FLAG)
            {
                if (disableYellowFlag)
                {
                    return FlagEnum.UNKNOWN;
                }
                return FlagEnum.YELLOW;
            }
            else if (flag == AC_FLAG_TYPE.AC_WHITE_FLAG)
            {
                return FlagEnum.WHITE;
            }
            else if (flag == AC_FLAG_TYPE.AC_BLUE_FLAG)
            {
                return FlagEnum.BLUE;
            }
            else if (flag == AC_FLAG_TYPE.AC_NO_FLAG)
            {
                return FlagEnum.GREEN;
            }
            return FlagEnum.UNKNOWN;
        }

        private float mapToPercentage(float level, float minimumIn, float maximumIn, float minimumOut, float maximumOut)
        {
            return (level - minimumIn) * (maximumOut - minimumOut) / (maximumIn - minimumIn) + minimumOut;
        }

        private float getTyreWearPercentage(float wearLevel, float minimumLevel)
        {
            if (wearLevel == -1)
            {
                return -1;
            }
            return Math.Min(100, mapToPercentage((minimumLevel / wearLevel) * 100, minimumLevel, 100, 0, 100));
        }

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            return SessionType.Unavailable;
        }

        private SessionType mapToSessionState(AC_SESSION_TYPE sessionState)
        {
            if (sessionState == AC_SESSION_TYPE.AC_RACE || sessionState == AC_SESSION_TYPE.AC_DRIFT || sessionState == AC_SESSION_TYPE.AC_DRAG)
            {
                return SessionType.Race;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_PRACTICE)
            {
                return SessionType.Practice;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_QUALIFY)
            {
                return SessionType.Qualify;
            }
            else if (sessionState == AC_SESSION_TYPE.AC_TIME_ATTACK || sessionState == AC_SESSION_TYPE.AC_HOTLAP)
            {
                return SessionType.HotLap;
            }
            else
            {
                return SessionType.Unavailable;
            }

        }

        private TyreType mapToTyreType(int r3eTyreType, CarData.CarClassEnum carClass)
        {
            return TyreType.Unknown_Race;
        }

        private ControlType mapToControlType(int controlType)
        {
            return ControlType.Player;
        }

        private DamageLevel mapToEngineDamageLevel(float engineDamage)
        {
            if (engineDamage >= 1000.0)
            {
                return DamageLevel.NONE;
            }
            else if (engineDamage <= destroyedEngineDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            }
            else if (engineDamage <= severeEngineDamageThreshold)
            {
                return DamageLevel.MAJOR;
            }
            else if (engineDamage <= minorEngineDamageThreshold)
            {
                return DamageLevel.MINOR;
            }
            else if (engineDamage <= trivialEngineDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            }
            return DamageLevel.NONE;
        }

        private PitWindow mapToPitWindow(int lapsOrMinutes, Boolean isInPits, int pitWindowStart, int pitWindowEnd, Boolean mandatoryPitDone)
        {
            if (lapsOrMinutes < pitWindowStart && lapsOrMinutes > pitWindowEnd)
            {
                return PitWindow.Closed;
            }
            if (mandatoryPitDone)
            {
                return PitWindow.Completed;
            }
            else if (lapsOrMinutes >= pitWindowStart && lapsOrMinutes <= pitWindowEnd)
            {
                return PitWindow.Open;
            }
            else if (isInPits && lapsOrMinutes >= pitWindowStart && lapsOrMinutes <= pitWindowEnd)
            {
                return PitWindow.StopInProgress;
            }
            else
            {
                return PitWindow.Unavailable;
            }
        }

        private DamageLevel mapToAeroDamageLevel(float aeroDamage)
        {

            if (aeroDamage >= destroyedAeroDamageThreshold)
            {
                return DamageLevel.DESTROYED;
            }
            else if (aeroDamage >= severeAeroDamageThreshold)
            {
                return DamageLevel.MAJOR;
            }
            else if (aeroDamage >= minorAeroDamageThreshold)
            {
                return DamageLevel.MINOR;
            }
            else if (aeroDamage >= trivialAeroDamageThreshold)
            {
                return DamageLevel.TRIVIAL;
            }
            else
            {
                return DamageLevel.NONE;
            }

        }
        public Boolean isBehindWithinDistance(float trackLength, float minDistance, float maxDistance, float playerTrackDistance, float opponentTrackDistance)
        {
            float difference = playerTrackDistance - opponentTrackDistance;
            if (difference > 0)
            {
                return difference < maxDistance && difference > minDistance;
            }
            else
            {
                difference = (playerTrackDistance + trackLength) - opponentTrackDistance;
                return difference < maxDistance && difference > minDistance;
            }
        }

        private int getCurrentSector(TrackDefinition trackDef, float distanceRoundtrack)
        {

            int ret = 3;
            if (distanceRoundtrack >= 0 && distanceRoundtrack < trackDef.sectorPoints[0])
            {
                ret = 1;
            }
            if (distanceRoundtrack >= trackDef.sectorPoints[0] && (trackDef.sectorPoints[1] == 0 || distanceRoundtrack < trackDef.sectorPoints[1]))
            {
                ret = 2;
            }
            return ret;
        }
        void logSectorsForUnknownTracks(TrackDefinition trackDef, float distanceRoundTrack, int currentSector)
        {
            if (loggedSectorStart[0] == -1 && currentSector == 2)
            {
                loggedSectorStart[0] = distanceRoundTrack;
            }
            if (loggedSectorStart[1] == -1 && currentSector == 3)
            {
                loggedSectorStart[1] = distanceRoundTrack;
            }
            if (trackDef.sectorsOnTrack == 2 && loggedSectorStart[0] != -1)
            {
                Console.WriteLine("new TrackDefinition(\"" + trackDef.name + "\", " + trackDef.trackLength + ", " + trackDef.sectorsOnTrack + ", new float[] {" + loggedSectorStart[0] + "f, " + 0 + "f})");
            }
            else if (trackDef.sectorsOnTrack == 3 && loggedSectorStart[0] != -1 && loggedSectorStart[1] != -1)
            {
                Console.WriteLine("new TrackDefinition(\"" + trackDef.name + "\", " + trackDef.trackLength + "f, " + trackDef.sectorsOnTrack + ", new float[] {" + loggedSectorStart[0] + "f, " + loggedSectorStart[1] + "f})");
            }
        }

        public static String getNameFromBytes(byte[] name)
        {
            String s = Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
            int pos = s.IndexOf('\0');
            if (pos >= 0)
            {
                s = s.Substring(0, pos);
            }
            return s;
        }

        private float spLineLengthToDistanceRoundTrack(float trackLength, float spLine)
        {
            if (spLine < 0.0f)
            {
                spLine -= 1f;
            }
            return spLine * trackLength;
        }

        private int isCarRealTimeLeaderBoardValid(acsVehicleInfo[] vehicles, int numVehicles)
        {
            for (int i = 0; i < numVehicles; i++)
            {
                if (vehicles[i].carRealTimeLeaderboardPosition == 0)
                {
                    return 1;
                }
            }
            return 0;
        }

    }
}
