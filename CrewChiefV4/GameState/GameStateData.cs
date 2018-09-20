using CrewChiefV4.Audio;
using CrewChiefV4.Events;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

/**
 * Holds all the data collected from the memory mapped file for the current tick
 */
namespace CrewChiefV4.GameState
{
    public enum SessionType
    {
        Unavailable, Practice, Qualify, Race, HotLap, LonePractice
    }
    public enum SessionPhase
    {
        Unavailable, Garage, Gridwalk, Formation, Countdown, Green, FullCourseYellow, Checkered, Finished
    }
    public enum ControlType
    {
        Unavailable, Player, AI, Remote, Replay
    }
    public enum PitWindow
    {
        Unavailable, Disabled, Closed, Open, StopInProgress, Completed
    }
    public enum TyreType
    {
        // separate enum for compound & weather, and prime / option?
        Hard, Medium, Soft, Super_Soft, Ultra_Soft, Wet, Intermediate, Road, Bias_Ply, Unknown_Race, R3E_2017, R3E_2016,
        R3E_2016_SOFT, R3E_2016_MEDIUM, R3E_2016_HARD, Prime, Option, Alternate, Primary, Ice, Snow, AllTerrain
    }

    public enum BrakeType
    {
        // pretty coarse grained here.
        Iron_Road, Iron_Race, Ceramic, Carbon
    }

    public enum TyreCondition
    {
        UNKNOWN, NEW, SCRUBBED, MINOR_WEAR, MAJOR_WEAR, WORN_OUT
    }
    public enum TyreTemp
    {
        UNKNOWN, COLD, WARM, HOT, COOKING
    }
    public enum BrakeTemp
    {
        UNKNOWN, COLD, WARM, HOT, COOKING
    }
    public enum DamageLevel
    {
        UNKNOWN = 0, NONE = 1, TRIVIAL = 2, MINOR = 3, MAJOR = 4, DESTROYED = 5
    }
    public enum FlagEnum
    {
        // note that chequered isn't used at the moment
        GREEN, YELLOW, DOUBLE_YELLOW, BLUE, WHITE, BLACK, CHEQUERED, UNKNOWN
    }

    public enum FullCourseYellowPhase
    {
        PENDING, IN_PROGRESS, PITS_CLOSED, PITS_OPEN_LEAD_LAP_VEHICLES, PITS_OPEN, LAST_LAP_NEXT, LAST_LAP_CURRENT, RACING
    }

    public enum PassAllowedUnderYellow
    {
        YES, NO, NO_DATA
    }

    public enum StockCarRule
    {
        NONE,
        LEADER_CHOOSE_LANE,
        LUCKY_DOG_PASS_ON_LEFT,  // Player's LD
        LUCKY_DOG_ALLOW_TO_PASS_ON_LEFT,  // Opponent's LD
        MOVE_TO_EOLL,
        WAVE_AROUND_PASS_ON_RIGHT  // Or left??
    }

    public class FlagData
    {
        // holds newer (AMS, RF2 & Raceroom) flag data. This is game dependent - only AMS, RF2 and R3E will use this.
        private FlagEnum[] _sectorFlags;
        public FlagEnum[] sectorFlags {
            get
            {
                if (_sectorFlags == null)
                {
                    _sectorFlags = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };                    
                }
                return _sectorFlags;
            }
        }
        public Boolean isFullCourseYellow; // FCY rules apply, no other announcements
        public Boolean isLocalYellow;  // local yellow - no overtaking, slow down
        // note that for RaceRoom we might have to calculate this. < 0 means we've passed the incident.
        public float distanceToNearestIncident = -1;
        public FullCourseYellowPhase fcyPhase = FullCourseYellowPhase.RACING;
        public Boolean currentLapIsFCY;
        public Boolean previousLapWasFCY;
        public int lapCountWhenLastWentGreen = -1;
        // cars passed under yellow - need to give back this many places to avoid penalty (only implemented for R3E)
        public int numCarsPassedIllegally = 0;
        public PassAllowedUnderYellow canOvertakeCarInFront = PassAllowedUnderYellow.NO_DATA;

        // bit of a hack... allow the mapper to decide which flag implemenation to use for yellow calls
        // as this is game dependent and option dependent (i.e. R3E players may have 'full flag rules' off)
        public Boolean useImprovisedIncidentCalling = true;
    }

    public class StockCarRulesData
    {
        public StockCarRule stockCarRuleApplicable = StockCarRule.NONE;
        public String luckyDogNameRaw;
        public Boolean stockCarRulesEnabled;
    }

    public class TransmissionData
    {
        // -2 = no data
        // -1 = reverse,
        //  0 = neutral
        //  1 = first gear
        // (... up to 7th)
        public int Gear = -2;
    }

    public class EngineData
    {
        // Engine speed
        public Single EngineRpm;

        // Maximum engine speed
        public Single MaxEngineRpm;

        // Unit: Celcius
        public Single EngineWaterTemp;

        // Unit: Celcius
        public Single EngineOilTemp;

        // Unit: ?
        public Single EngineOilPressure;

        public int MinutesIntoSessionBeforeMonitoring;

        public Boolean EngineWaterTempWarning;

        public Boolean EngineOilPressureWarning;

        public Boolean EngineFuelPressureWarning;

        public Boolean EngineStalledWarning;

    }

    public class FuelData
    {
        // Unit: ?
        public Single FuelPressure;

        // Current amount of fuel in the tank(s)
        // Unit: Liters (l)
        public Single FuelLeft;

        // Maximum capacity of fuel tank(s)
        // Unit: Liters (l)
        public Single FuelCapacity;

        public Boolean FuelUseActive;
    }

    public class BatteryData
    {
        // Current battery charge level
        // Unit: % of full charge (100%)
        public Single BatteryPercentageLeft;

        public Boolean BatteryUseActive;
    }


    public class CarDamageData
    {
        public Boolean DamageEnabled;

        public DamageLevel OverallTransmissionDamage = DamageLevel.UNKNOWN;

        public DamageLevel OverallEngineDamage = DamageLevel.UNKNOWN;

        public DamageLevel OverallAeroDamage = DamageLevel.UNKNOWN;

        private CornerData _SuspensionDamageStatus;
        public CornerData SuspensionDamageStatus
        {
            get
            {
                if (_SuspensionDamageStatus == null)
                {
                    _SuspensionDamageStatus = new CornerData();
                }
                return _SuspensionDamageStatus;
            }
            set
            {
                _SuspensionDamageStatus = value;
            }
        }

        private CornerData _BrakeDamageStatus;
        public CornerData BrakeDamageStatus
        {
            get
            {
                if (_BrakeDamageStatus == null)
                {
                    _BrakeDamageStatus = new CornerData();
                }
                return _BrakeDamageStatus;
            }
            set
            {
                _BrakeDamageStatus = value;
            }
        }

        public float LastImpactTime = -1.0f;
    }

    public enum FrozenOrderPhase
    {
        None,
        FullCourseYellow,
        FormationStanding,
        Rolling,
        FastRolling
    }

    public enum FrozenOrderColumn
    {
        None,
        Left,
        Right
    }

    public enum FrozenOrderAction
    {
        None,
        Follow,
        CatchUp,
        AllowToPass,
        StayInPole,  // Case of being assigned pole/pole row with no SC present (Rolling start in rF2 Karts, for example).
        MoveToPole  // Case of falling behind assigned pole/pole row with no SC present (Rolling start in rF2 Karts, for example).
    }

    public class FrozenOrderData
    {
        public FrozenOrderPhase Phase = FrozenOrderPhase.None;
        public FrozenOrderAction Action = FrozenOrderAction.None;

        // If column is assigned, p1 and p2 follows SC.  Otherwise,
        // only p1 follows SC.
        public int AssignedPosition = -1;

        public FrozenOrderColumn AssignedColumn = FrozenOrderColumn.None;
        // Only matters if AssignedColumn != None
        public int AssignedGridPosition = -1;

        public string DriverToFollowRaw = "";

        // Meters/s.  If -1, SC either left or not present.
        public float SafetyCarSpeed = -1.0f;
    }

    public enum StartType
    {
        None,
        Standing,
        Rolling
    }

    public class TimingData
    {
        public Boolean conditionsHaveChanged;
        private ConditionsEnum initialConditions = ConditionsEnum.ANY;  // not set

        // used for delaying transitions to allow track conditions to catch up with weather conditions
        private ConditionsEnum previousTrackConditions = ConditionsEnum.ANY; // not set
        private ConditionsEnum pendingTrackConditions = ConditionsEnum.ANY; // not set
        private DateTime timeWhenTrackConditionsHaveCaughtUp = DateTime.MaxValue;
        private Boolean waitingForTrackConditionsToCatchUp = false;

        // in order of potential pace - WARM_DRY is fastest, CURRENT is whatever the current conditions are,
        // ANY is all conditions
        public enum ConditionsEnum {
            SNOW = 0, ICE, VERY_WET, COLD_WET, WARM_WET, COLD_DAMP, WARM_DAMP, COLD_DRY, HOT_DRY, WARM_DRY, CURRENT, ANY
        }

        private Dictionary<ConditionsEnum, List<float>> playerSector1TimesByConditions = new Dictionary<ConditionsEnum, List<float>>();
        private Dictionary<ConditionsEnum, List<float>> playerSector2TimesByConditions = new Dictionary<ConditionsEnum, List<float>>();
        private Dictionary<ConditionsEnum, List<float>> playerSector3TimesByConditions = new Dictionary<ConditionsEnum, List<float>>();
        private Dictionary<ConditionsEnum, List<float>> playerLapTimesByConditions = new Dictionary<ConditionsEnum, List<float>>();

        private List<float> allPlayerSector1Times = new List<float>();
        private List<float> allPlayerSector2Times = new List<float>();
        private List<float> allPlayerSector3Times = new List<float>();
        private List<float> allPlayerLapTimes = new List<float>();


        // Player only best times
        private Dictionary<ConditionsEnum, float> playerBestLapSector1TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerBestLapSector2TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerBestLapSector3TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerBestLapTimeByConditions = new Dictionary<ConditionsEnum, float>();

        private float playerBestLapSector1Time = -1;
        private float playerBestLapSector2Time = -1;
        private float playerBestLapSector3Time = -1;
        private float playerBestLapTime = -1;


        // Player class best times (player + opponents)
        private Dictionary<ConditionsEnum, float> playerClassBestLapSector1TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerClassBestLapSector2TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerClassBestLapSector3TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerClassBestLapTimeByConditions = new Dictionary<ConditionsEnum, float>();

        private float playerClassBestLapSector1Time = -1;
        private float playerClassBestLapSector2Time = -1;
        private float playerClassBestLapSector3Time = -1;
        private float playerClassBestLapTime = -1;


        // opponets in player class best times
        private Dictionary<ConditionsEnum, float> playerClassOpponentBestLapSector1TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerClassOpponentBestLapSector2TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerClassOpponentBestLapSector3TimeByConditions = new Dictionary<ConditionsEnum, float>();
        private Dictionary<ConditionsEnum, float> playerClassOpponentBestLapTimeByConditions = new Dictionary<ConditionsEnum, float>();

        private float playerClassOpponentBestLapSector1Time = -1;
        private float playerClassOpponentBestLapSector2Time = -1;
        private float playerClassOpponentBestLapSector3Time = -1;
        private float playerClassOpponentBestLapTime = -1;

        private Dictionary<ConditionsEnum, int> totalLapsInEachCondition = new Dictionary<ConditionsEnum, int>();
        
        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player
        // laptime set in conditions similar to the current conditions. You can also request a best laptime from
        // some other conditions
        public float getPlayerBestLapTime(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerBestLapTime, playerBestLapTimeByConditions, true, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player
        // lap sector1 time set in conditions similar to the current conditions. You can also request a best lap
        // sector1 time from some other conditions
        public float getPlayerBestLapSector1Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerBestLapSector1Time, playerBestLapSector1TimeByConditions, true, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player
        // lap sector2 time set in conditions similar to the current conditions. You can also request a best lap
        // sector2 time from some other conditions
        public float getPlayerBestLapSector2Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerBestLapSector2Time, playerBestLapSector2TimeByConditions, true, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player
        // lap sector3 time set in conditions similar to the current conditions. You can also request a best lap
        // sector3 time from some other conditions
        public float getPlayerBestLapSector3Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerBestLapSector3Time, playerBestLapSector3TimeByConditions, true, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap time set in conditions similar to the current conditions. You can also request a player class best laptime from
        // some other conditions
        public float getPlayerClassBestLapTime(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassBestLapTime, playerClassBestLapTimeByConditions, false, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap sector1 time set in conditions similar to the current conditions. You can also request a player class best lap
        // sector1 time from some other conditions
        public float getPlayerClassBestLapSector1Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassBestLapSector1Time, playerClassBestLapSector1TimeByConditions, false, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap sector2 time set in conditions similar to the current conditions. You can also request a player class best lap
        // sector2 time from some other conditions
        public float getPlayerClassBestLapSector2Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassBestLapSector2Time, playerClassBestLapSector2TimeByConditions, false, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap sector3 time set in conditions similar to the current conditions. You can also request a player class best lap
        // sector3 time from some other conditions
        public float getPlayerClassBestLapSector3Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassBestLapSector3Time, playerClassBestLapSector3TimeByConditions, false, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap time set in conditions similar to the current conditions. You can also request a player class best laptime from
        // some other conditions
        public float getPlayerClassOpponentBestLapTime(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassOpponentBestLapTime, playerClassOpponentBestLapTimeByConditions, false, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap sector1 time set in conditions similar to the current conditions. You can also request a player class best lap
        // sector1 time from some other conditions
        public float getPlayerClassOpponentBestLapSector1Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassOpponentBestLapSector1Time, playerClassOpponentBestLapSector1TimeByConditions, false, requestedConditionsEnum);
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap sector2 time set in conditions similar to the current conditions. You can also request a player class best lap
        // sector2 time from some other conditions
        public float getPlayerClassOpponentBestLapSector2Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassOpponentBestLapSector2Time, playerClassOpponentBestLapSector2TimeByConditions, false, requestedConditionsEnum);            
        }

        // if requestedConditionsEnum aren't specified we assume 'current conditions' - that is, get the best player class
        // lap sector3 time set in conditions similar to the current conditions. You can also request a player class best lap
        // sector3 time from some other conditions
        public float getPlayerClassOpponentBestLapSector3Time(ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            return getBestTime(playerClassOpponentBestLapSector3Time, playerClassOpponentBestLapSector3TimeByConditions, false, requestedConditionsEnum);
        }

        private ConditionsEnum getConditionsEnumForSample(Conditions.ConditionsSample sample)
        {
            if (sample == null)
            {
                return ConditionsEnum.WARM_DRY;
            }
            ConditionsMonitor.RainLevel rainLevel = ConditionsMonitor.RainLevel.NONE;
            if (CrewChief.gameDefinition.gameEnum == GameEnum.PCARS2 || CrewChief.gameDefinition.gameEnum == GameEnum.RF2_64BIT)
            {
                rainLevel = ConditionsMonitor.getRainLevel(sample.RainDensity);
            }
            else if ((CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_32BIT ||
                     CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_64BIT ||
                     CrewChief.gameDefinition.gameEnum == GameEnum.PCARS_NETWORK) &&
                sample.RainDensity > 0)
            {
                rainLevel = ConditionsMonitor.RainLevel.MID;
            }

            ConditionsEnum conditionsEnum = ConditionsEnum.WARM_DRY;

            if (rainLevel == ConditionsMonitor.RainLevel.NONE)
            {
                if (sample.AmbientTemperature < 12)
                {
                    conditionsEnum = ConditionsEnum.COLD_DRY;
                }
                else if (sample.AmbientTemperature < 30)
                {
                    conditionsEnum = ConditionsEnum.WARM_DRY;
                }
                else
                {
                    conditionsEnum = ConditionsEnum.HOT_DRY;
                }
            }
            else if (sample.AmbientTemperature < 0)
            {
                if (rainLevel >= ConditionsMonitor.RainLevel.LIGHT)
                {
                    conditionsEnum = ConditionsEnum.SNOW;
                }
                else
                {
                    conditionsEnum = ConditionsEnum.ICE;
                }
            }
            else if (rainLevel >= ConditionsMonitor.RainLevel.HEAVY)
            {
                return ConditionsEnum.VERY_WET;
            }
            else if (rainLevel == ConditionsMonitor.RainLevel.MID)
            {
                if (sample.AmbientTemperature < 12)
                {
                    conditionsEnum = ConditionsEnum.COLD_WET;
                }
                else
                {
                    conditionsEnum = ConditionsEnum.WARM_WET;
                }
            }
            else if (rainLevel == ConditionsMonitor.RainLevel.LIGHT)
            {
                if (sample.AmbientTemperature < 12)
                {
                    conditionsEnum = ConditionsEnum.COLD_DAMP;
                }
                else
                {
                    conditionsEnum = ConditionsEnum.WARM_DAMP;
                }
            }

            // so now we have the weather conditions we need to apply the delay to estimate the track conditions
            if (previousTrackConditions == ConditionsEnum.ANY)
            {
                previousTrackConditions = conditionsEnum;
                pendingTrackConditions = conditionsEnum;
                waitingForTrackConditionsToCatchUp = false;
                return conditionsEnum;
            }
            else if (waitingForTrackConditionsToCatchUp)
            {
                if (sample.Time > timeWhenTrackConditionsHaveCaughtUp)
                {
                    // conditions changed some time ago and we've allowed the track conditions to catch up
                    previousTrackConditions = pendingTrackConditions;
                    waitingForTrackConditionsToCatchUp = false;
                    return pendingTrackConditions;
                }
                else if (conditionsEnum < pendingTrackConditions)
                {
                    // special case - if current conditions are worse than the pending conditions, skip straight to the pending
                    // conditions and reset the timer
                    timeWhenTrackConditionsHaveCaughtUp = sample.Time.Add(ConditionsMonitor.getTrackConditionsChangeDelay());
                    previousTrackConditions = pendingTrackConditions;
                    pendingTrackConditions = conditionsEnum;
                    return previousTrackConditions;
                }
                else
                {
                    return previousTrackConditions;
                }
            }
            else if (previousTrackConditions != conditionsEnum)
            {
                // conditions have changed so start the timer for the track to catch up
                timeWhenTrackConditionsHaveCaughtUp = sample.Time.Add(ConditionsMonitor.getTrackConditionsChangeDelay());
                waitingForTrackConditionsToCatchUp = true;
                pendingTrackConditions = conditionsEnum;
                return previousTrackConditions;
            }
            else
            {
                return previousTrackConditions;
            }
        }

        private float getBestTime(float overallBest, Dictionary<ConditionsEnum, float> timesByCondition, Boolean checkForSufficientPlayerData,
            ConditionsEnum requestedConditionsEnum = ConditionsEnum.CURRENT)
        {
            if (requestedConditionsEnum == ConditionsEnum.ANY)
            {
                return overallBest;
            }
            ConditionsEnum conditionsEnum = getConditionsEnum(requestedConditionsEnum);
            if (!hasEnoughData(conditionsEnum, checkForSufficientPlayerData))
            {
                return overallBest;
            }
            float time;
            if (timesByCondition.TryGetValue(conditionsEnum, out time))
            {
                return time;
            }
            return -1;
        }

        private Boolean hasEnoughData(ConditionsEnum requestedConditionsEnum, Boolean checkForSufficientPlayerData)
        {
            int minLapsUnderConditions = 2;
            // eeewwwww
            if (CrewChief.currentGameState != null && CrewChief.currentGameState.SessionData.TrackDefinition != null)
            {
                switch (CrewChief.currentGameState.SessionData.TrackDefinition.trackLengthClass)
                {
                    case TrackData.TrackLengthClass.VERY_LONG:
                    case TrackData.TrackLengthClass.LONG:
                        minLapsUnderConditions = 1;
                        break;
                    case TrackData.TrackLengthClass.SHORT:
                    case TrackData.TrackLengthClass.VERY_SHORT:
                        minLapsUnderConditions = 3;
                        break;
                    default:
                        break;
                }
            }
            if (checkForSufficientPlayerData)
            {
                // 'enough data' means enough recorded player laps
                List<float> list;
                return playerLapTimesByConditions.TryGetValue(requestedConditionsEnum, out list) && list.Count >= minLapsUnderConditions;
            }
            else
            {
                // 'enough data' means enough recorded laps for any participant in the player's class
                int totalLapsInTheseConditions;
                return totalLapsInEachCondition.TryGetValue(requestedConditionsEnum, out totalLapsInTheseConditions) && totalLapsInTheseConditions >= minLapsUnderConditions;
            }            
        }
        
        // add a player lap, updating the best lap / sectors for player and player class if necessary
        public void addPlayerLap(float lapTime, float s1, float s2, float s3)
        {
            if (lapTime > 0)
            {
                ConditionsEnum conditionsEnum = getConditionsEnum(ConditionsEnum.CURRENT);
                if (initialConditions == ConditionsEnum.ANY)
                {
                    initialConditions = conditionsEnum;
                }
                else if (initialConditions != conditionsEnum)
                {
                    conditionsHaveChanged = true;
                }
                int totalLapsInTheseConditions;
                if (totalLapsInEachCondition.TryGetValue(conditionsEnum, out totalLapsInTheseConditions))
                {
                    totalLapsInEachCondition[conditionsEnum] = totalLapsInTheseConditions + 1;
                }
                else
                {
                    totalLapsInEachCondition[conditionsEnum] = 1;
                }
                addToData(conditionsEnum, playerLapTimesByConditions, lapTime);
                allPlayerLapTimes.Add(lapTime);
                addToData(conditionsEnum, playerSector1TimesByConditions, s1);
                allPlayerSector1Times.Add(s1);
                addToData(conditionsEnum, playerSector2TimesByConditions, s2);
                allPlayerSector2Times.Add(s2);
                addToData(conditionsEnum, playerSector3TimesByConditions, s3);
                allPlayerSector3Times.Add(s3);
                updateBestTimes(conditionsEnum, lapTime, s1, s2, s3, true);
            }
        }

        // Note that this should only be called if we've already checked this opponent is in the same class as the player
        public void addOpponentPlayerClassLap(float lapTime, float s1, float s2, float s3)
        {
            if (lapTime > 0)
            {
                ConditionsEnum conditionsEnum = getConditionsEnum(ConditionsEnum.CURRENT);
                if (initialConditions == ConditionsEnum.ANY)
                {
                    initialConditions = conditionsEnum;
                }
                else if (initialConditions != conditionsEnum)
                {
                    conditionsHaveChanged = true;
                }
                int totalLapsInTheseConditions;
                if (totalLapsInEachCondition.TryGetValue(conditionsEnum, out totalLapsInTheseConditions))
                {
                    totalLapsInEachCondition[conditionsEnum] = totalLapsInTheseConditions + 1;
                }
                else
                {
                    totalLapsInEachCondition[conditionsEnum] = 1;
                }
                updateBestTimes(conditionsEnum, lapTime, s1, s2, s3, false);
            }
        }

        private ConditionsEnum getConditionsEnum(ConditionsEnum requestedConditionsEnum)
        {
            if (requestedConditionsEnum == ConditionsEnum.CURRENT)
            {
                // TODO: need a better way to link the Conditions state with this TimingData state object, without needlessly invoking the ctor
                if (CrewChief.currentGameState == null || CrewChief.currentGameState.Conditions == null || CrewChief.currentGameState.Conditions.samples == null)
                {
                    return ConditionsEnum.WARM_DRY;
                }
                return getConditionsEnumForSample(CrewChief.currentGameState.Conditions.getMostRecentConditions());
            }
            else
            {
                return requestedConditionsEnum;
            }
        }


        private void updateBestTimes(ConditionsEnum conditionsEnum, float lapTime, float s1, float s2, float s3, Boolean isPlayer)
        {
            if (playerClassBestLapTime == -1 || lapTime < playerClassBestLapTime)
            {
                playerClassBestLapTime = lapTime;
                playerClassBestLapSector1Time = s1;
                playerClassBestLapSector2Time = s2;
                playerClassBestLapSector3Time = s3;
            }
            if (isPlayer) 
            {
                if (playerBestLapTime == -1 || lapTime < playerBestLapTime)
                {
                    playerBestLapTime = lapTime;
                    playerBestLapSector1Time = s1;
                    playerBestLapSector2Time = s2;
                    playerBestLapSector3Time = s3;
                }
            }
            else
            {
                if (playerClassOpponentBestLapTime == -1 || lapTime < playerClassOpponentBestLapTime)
                {
                    playerClassOpponentBestLapTime = lapTime;
                    playerClassOpponentBestLapSector1Time = s1;
                    playerClassOpponentBestLapSector2Time = s2;
                    playerClassOpponentBestLapSector3Time = s3;
                }
            }
            Boolean isBest;
            float existingPlayerClassBestLap;
            isBest = !playerClassBestLapTimeByConditions.TryGetValue(conditionsEnum, out existingPlayerClassBestLap) ||
                lapTime < existingPlayerClassBestLap;
            if (isBest)
            {
                playerClassBestLapTimeByConditions[conditionsEnum] = lapTime;
                playerClassBestLapSector1TimeByConditions[conditionsEnum] = s1;
                playerClassBestLapSector2TimeByConditions[conditionsEnum] = s2;
                playerClassBestLapSector3TimeByConditions[conditionsEnum] = s3;
                if (isPlayer)
                {
                    playerBestLapTimeByConditions[conditionsEnum] = lapTime;
                    playerBestLapSector1TimeByConditions[conditionsEnum] = s1;
                    playerBestLapSector2TimeByConditions[conditionsEnum] = s2;
                    playerBestLapSector3TimeByConditions[conditionsEnum] = s3;
                }
            }
            else if (isPlayer)
            {
                float existingPlayerBestLap;
                isBest = !playerBestLapTimeByConditions.TryGetValue(conditionsEnum, out existingPlayerBestLap) ||
                    lapTime < existingPlayerBestLap;
                if (isBest)
                {
                    playerBestLapTimeByConditions[conditionsEnum] = lapTime;
                    playerBestLapSector1TimeByConditions[conditionsEnum] = s1;
                    playerBestLapSector2TimeByConditions[conditionsEnum] = s2;
                    playerBestLapSector3TimeByConditions[conditionsEnum] = s3;
                }
            }
            if (!isPlayer)
            {
                float existingPlayerClassOpponentBestLap;
                isBest = !playerClassOpponentBestLapTimeByConditions.TryGetValue(conditionsEnum, out existingPlayerClassOpponentBestLap) ||
                    lapTime < existingPlayerClassOpponentBestLap;
                if (isBest)
                {
                    playerClassOpponentBestLapTimeByConditions[conditionsEnum] = lapTime;
                    playerClassOpponentBestLapSector1TimeByConditions[conditionsEnum] = s1;
                    playerClassOpponentBestLapSector2TimeByConditions[conditionsEnum] = s2;
                    playerClassOpponentBestLapSector3TimeByConditions[conditionsEnum] = s3;
                }
            }
        }

        private void addToData(ConditionsEnum conditionsEnum, Dictionary<ConditionsEnum, List<float>> data, float value) {
            List<float> existingData;
            if (data.TryGetValue(conditionsEnum, out existingData))
            {
                existingData.Add(value);
            }
            else
            {
                existingData = new List<float>();
                existingData.Add(value);
                data.Add(conditionsEnum, existingData);
            }
        }
    }

    public class SessionData
    {
        private List<String> _formattedPlayerLapTimes;
        public List<String> formattedPlayerLapTimes
        {
            get
            {
                if (_formattedPlayerLapTimes == null)
                {
                    _formattedPlayerLapTimes = new List<String>();
                }
                return _formattedPlayerLapTimes;
            }
            set
            {
                _formattedPlayerLapTimes = value;
            }
        }

        public TrackDefinition TrackDefinition;

        public Boolean IsDisqualified;

        public Boolean IsDNF;

        public FlagEnum Flag = FlagEnum.GREEN;

        public DateTime YellowFlagStartTime = DateTime.UtcNow;

        // used for race sessions that have just started
        public Boolean JustGoneGreen;

        public Boolean IsNewSession;

        public Boolean SessionHasFixedTime;

        public SessionType SessionType = SessionType.Unavailable;

        public DateTime SessionStartTime;

        // in minutes, 0 if this session is a fixed number of laps rather than a fixed time.
        public float SessionTotalRunTime;

        public int SessionNumberOfLaps;

        // some timed sessions have an extra lap added after the timer reaches zero
        public Boolean HasExtraLap;

        public int SessionStartClassPosition;

        public int NumCarsOverallAtStartOfSession;
        public int NumCarsInPlayerClassAtStartOfSession;

        // race number in ongoing championship (zero indexed)
        public int EventIndex;

        // zero indexed - you multi iteration sessions like DTM qual
        public int SessionIteration;

        //iRacing session id, if changed we have a new session(usefull for detecting practice session to practice session change)
        public int SessionId = -1;

        // as soon as the player leaves the racing surface this is set to false
        public Boolean CurrentLapIsValid = true;

        public Boolean PreviousLapWasValid = true;

        public SessionPhase SessionPhase = SessionPhase.Unavailable;

        public Boolean IsNewLap;

        // How many laps the player has completed. If this value is 6, the player is on his 7th lap.
        public int CompletedLaps;

        // how many laps are left for the player. In fixed lap sessions this is totalLaps - leaderCompletedLaps, to allow for being
        // lapped. In all other sessions it's MaxInt
        public int SessionLapsRemaining = int.MaxValue;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimePrevious = -1;

        public Single LapTimePreviousEstimateForInvalidLap = -1;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimeCurrent = -1;

        public Boolean LeaderHasFinishedRace;

        public int LeaderSectorNumber;

        public int PositionAtStartOfCurrentLap;
        public int ClassPositionAtStartOfCurrentLap;

        // Current position (1 = first place)
        public int OverallPosition;

        public int ClassPosition;

        public float GameTimeAtLastPositionFrontChange;

        public float GameTimeAtLastPositionBehindChange;

        // Number of cars (including the player) in the race
        public int NumCarsOverall;
        public int NumCarsInPlayerClass;

        public Single SessionRunningTime;

        // ...
        public Single SessionTimeRemaining;

        // ...
        public Single PlayerLapTimeSessionBest = -1;

        public Single OpponentsLapTimeSessionBestOverall = -1;

        public Single OpponentsLapTimeSessionBestPlayerClass = -1;

        public Single OverallSessionBestLapTime = -1;

        public Single PlayerClassSessionBestLapTime = -1;

        public Single PlayerLapTimeSessionBestPrevious = -1;

        // Absolute time delta within a lap.
        public Single TimeDeltaFront = -1;
        // O means vehicles are on the same lap. >= 1 means user is lapped.
        public int LapsDeltaFront = -1;

        // Absolute time delta within a lap.
        public Single TimeDeltaBehind = -1;
        // O means vehicles are on the same lap. >= 1 means user lapped car behind him/her.
        public int LapsDeltaBehind = -1;

        // 0 means we don't know what sector we're in. This is 1-indexed
        public int SectorNumber;

        public Boolean IsNewSector;

        // these are used for quick n dirty checks to see if we're racing the same opponent in front / behind,
        // without iterating over the Opponents list. Or for cases (like R3E) where we don't have an opponents list
        public Boolean IsRacingSameCarInFront = true;

        public Boolean IsRacingSameCarBehind = true;

        public Boolean HasLeadChanged;

        private Dictionary<int, float> _SessionTimesAtEndOfSectors;
        public Dictionary<int, float> SessionTimesAtEndOfSectors {
            get {
                if (_SessionTimesAtEndOfSectors == null) {
                    _SessionTimesAtEndOfSectors = new Dictionary<int, float>();
                }
                return _SessionTimesAtEndOfSectors;
            }
            set
            {
                _SessionTimesAtEndOfSectors = value;
            }
        }

        public String DriverRawName;

        public float LastSector1Time = -1;
        public float LastSector2Time = -1;
        public float LastSector3Time = -1;

        // best sector times for the player
        public float PlayerBestSector1Time = -1;
        public float PlayerBestSector2Time = -1;
        public float PlayerBestSector3Time = -1;

        // sector times set on the player's fastest lap
        public float PlayerBestLapSector1Time = -1;
        public float PlayerBestLapSector2Time = -1;
        public float PlayerBestLapSector3Time = -1;

        // data sent by the game, rather than derived (useful for mid-session joining)
        public float SessionFastestLapTimeFromGame = -1;
        public float SessionFastestLapTimeFromGamePlayerClass = -1;

        private TrackLandmarksTiming _trackLandmarksTiming;
        public TrackLandmarksTiming trackLandmarksTiming
        {
            get
            {
                if (_trackLandmarksTiming == null)
                {
                    _trackLandmarksTiming = new TrackLandmarksTiming();
                }
                return _trackLandmarksTiming;
            }
            set
            {
                _trackLandmarksTiming = value;
            }
        }

        // Player lap times with sector information
        private List<LapData> _PlayerLapData;
        public List<LapData> PlayerLapData
        {
            get
            {
                if (_PlayerLapData == null)
                {
                    _PlayerLapData = new List<LapData>();
                }
                return _PlayerLapData;
            }
            set
            {
                _PlayerLapData = value;
            }
        }

        public String stoppedInLandmark;

        // Currently, used by rFactor family of games to indicate that user finished session
        // by proceeding to the next session while in the monitor.  Currently, those games do not go
        // in into "Finished" phase in such case.  If this is true, SessionPhase is set to Finished
        // artificially by mappers, not by the game.
        public Boolean AbruptSessionEndDetected;

        private Dictionary<TyreType, float> _PlayerClassSessionBestLapTimeByTyre;
        public Dictionary<TyreType, float> PlayerClassSessionBestLapTimeByTyre
        {
            get
            {
                if (_PlayerClassSessionBestLapTimeByTyre == null)
                {
                    _PlayerClassSessionBestLapTimeByTyre = new Dictionary<TyreType, float>();
                }
                return _PlayerClassSessionBestLapTimeByTyre;
            }
            set
            {
                _PlayerClassSessionBestLapTimeByTyre = value;
            }
        }

        // as above, but for the player only
        private Dictionary<TyreType, float> _PlayerBestLapTimeByTyre;
        public Dictionary<TyreType, float> PlayerBestLapTimeByTyre
        {
            get
            {
                if (_PlayerBestLapTimeByTyre == null)
                {
                    _PlayerBestLapTimeByTyre = new Dictionary<TyreType, float>();
                }
                return _PlayerBestLapTimeByTyre;
            }
            set
            {
                _PlayerBestLapTimeByTyre = value;
            }
        }

        private DeltaTime _DeltaTime;
        public DeltaTime DeltaTime
        {
            get
            {
                if (_DeltaTime == null)
                {
                    _DeltaTime = new DeltaTime();
                }
                return _DeltaTime;
            }
            set
            {
                _DeltaTime = value;
            }
        }

        public int PlayerCarNr = -1;

        // Currently only used in iRacing.
        public int MaxIncidentCount = -1;

        public int CurrentIncidentCount ;

        public int CurrentTeamIncidentCount ;

        public int CurrentDriverIncidentCount ;

        public Boolean HasLimitedIncidents;

        private Tuple<String, float> _LicenseLevel;
        public Tuple<String, float> LicenseLevel
        {
            get
            {
                if (_LicenseLevel == null)
                {
                    _LicenseLevel = new Tuple<String, float>("invalid", -1);;
                }
                return _LicenseLevel;
            }
            set
            {
                _LicenseLevel = value;
            }
        }

        public int iRating;

        public int StrengthOfField;

        public Boolean IsLastLap;

        public StartType StartType = StartType.None;

        public Boolean HasCompletedSector2ThisLap;

        public SessionData()
        {
            SessionTimesAtEndOfSectors.Add(1, -1);
            SessionTimesAtEndOfSectors.Add(2, -1);
            SessionTimesAtEndOfSectors.Add(3, -1);
        }

        public void restorePlayerTimings(SessionData restoreTo)
        {
            restoreTo.PlayerBestSector1Time = PlayerBestSector1Time;
            restoreTo.PlayerBestSector2Time = PlayerBestSector2Time;
            restoreTo.PlayerBestSector3Time = PlayerBestSector3Time;

            restoreTo.PlayerBestLapSector1Time = PlayerBestLapSector1Time;
            restoreTo.PlayerBestLapSector2Time = PlayerBestLapSector2Time;
            restoreTo.PlayerBestLapSector3Time = PlayerBestLapSector3Time;

            restoreTo.PlayerLapTimeSessionBest = PlayerLapTimeSessionBest;
            restoreTo.PlayerLapTimeSessionBestPrevious = PlayerLapTimeSessionBestPrevious;

            restoreTo.PreviousLapWasValid = PreviousLapWasValid;
            restoreTo.LapTimePrevious = LapTimePrevious;

            foreach (var ld in PlayerLapData)
                restoreTo.PlayerLapData.Add(ld);

            foreach (var entry in PlayerClassSessionBestLapTimeByTyre)
                restoreTo.PlayerClassSessionBestLapTimeByTyre.Add(entry.Key, entry.Value);

            foreach (var entry in PlayerBestLapTimeByTyre)
                restoreTo.PlayerBestLapTimeByTyre.Add(entry.Key, entry.Value);
        }

        public void playerStartNewLap(int lapNumber, int overallPosition, Boolean inPits, float gameTimeAtStart)
        {
            if (PlayerLapData.Count > 0)
            {
                verifyPlayerPreviousLap();
            }
            LapData thisLapData = new LapData();
            thisLapData.GameTimeAtLapStart = gameTimeAtStart;
            thisLapData.OutLap = inPits;
            thisLapData.PositionAtStart = overallPosition;
            thisLapData.LapNumber = lapNumber;
            CurrentLapIsValid = true;
            PlayerLapData.Add(thisLapData);
        }

        // This method takes care of marking abandoned laps as invalid and missing sectors.
        // It is intended to be called when IsNewLap is true, and _before_ new lap is added to this.PlayerLapata.
        private void verifyPlayerPreviousLap()
        {
            if (!IsNewLap)
            {
                Debug.Assert(IsNewLap, "IsNewLap is false, please fix the mapper.");
                return;
            }
            // Verify we have LapData flags in a sane state.  This is necessary, because if player jumps to pits
            // without completing the lap, IsValid and hasMissingSectors members may not have correct values set.
            if (PlayerLapData.Count > 0)
            {
                LapData previousLap = PlayerLapData[PlayerLapData.Count - 1];
                if (previousLap.LapTime < 0.0f)
                {
                    previousLap.IsValid = false;
                    PreviousLapWasValid = false;
                }
                foreach (var sectorTime in previousLap.SectorTimes)
                {
                    if (sectorTime == 0.0f)
                    {
                        previousLap.hasMissingSectors = true;
                        break;
                    }
                }
            }
            else
            {
                // No previous lap.
                PreviousLapWasValid = false;
            }
        }

        public void playerCompleteLapWithProvidedLapTime(int overallPosition, float gameTimeAtLapEnd, float providedLapTime,
            Boolean lapIsValid /*IMPORTANT: this is 'current lap is valid'*/, Boolean inPitLane, Boolean isRaining, float trackTemp, 
            float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining, int numberOfSectors, TimingData timingData)
        {
            if (PlayerLapData.Count == 0)
            {
                return;
            }
            CurrentLapIsValid = true;
            formattedPlayerLapTimes.Add(TimeSpan.FromSeconds(providedLapTime).ToString(@"mm\:ss\.fff"));
            PositionAtStartOfCurrentLap = overallPosition;
            
            LapData lapData = PlayerLapData[PlayerLapData.Count - 1];
            
            LapTimePreviousEstimateForInvalidLap = SessionRunningTime - SessionTimesAtEndOfSectors[numberOfSectors - 1];
            playerAddCumulativeSectorData(numberOfSectors, overallPosition, providedLapTime, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
            lapData.LapTime = providedLapTime;
            lapData.InLap = inPitLane;

            LapTimePrevious = providedLapTime;

            verifyPlayerPreviousLap();

            if (lapData.IsValid && !lapData.OutLap && !lapData.InLap)
            {
                if (PlayerLapTimeSessionBest == -1 || PlayerLapTimeSessionBest > lapData.LapTime)
                {
                    PlayerLapTimeSessionBestPrevious = PlayerLapTimeSessionBest;
                    PlayerLapTimeSessionBest = lapData.LapTime;

                    PlayerBestLapSector1Time = lapData.SectorTimes[0];
                    PlayerBestLapSector2Time = lapData.SectorTimes[1];
                    if (numberOfSectors > 2)
                    {
                        PlayerBestLapSector3Time = lapData.SectorTimes[2];
                    }
                }
                timingData.addPlayerLap(lapData.LapTime, lapData.SectorTimes[0], lapData.SectorTimes[1], lapData.SectorTimes[2]);
            }
            PreviousLapWasValid = lapData.IsValid;
            if (PreviousLapWasValid && LapTimePrevious > 0 && PlayerLapTimeSessionBest == -1 || LapTimePrevious == PlayerLapTimeSessionBest)
            {
                if (OverallSessionBestLapTime == -1 || LapTimePrevious < OverallSessionBestLapTime)
                {
                    OverallSessionBestLapTime = LapTimePrevious;
                }
                if (PlayerClassSessionBestLapTime == -1 || LapTimePrevious < PlayerClassSessionBestLapTime)
                {
                    PlayerClassSessionBestLapTime = LapTimePrevious;
                }
            }                
        }


        public void playerAddCumulativeSectorData(int sectorNumberJustCompleted, int overallPosition, float cumulativeSectorTime,
            float gameTimeAtSectorEnd, Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {
            SessionTimesAtEndOfSectors[sectorNumberJustCompleted] = gameTimeAtSectorEnd;
            LapData lapData;
            if (PlayerLapData.Count == 0)
            {
                playerStartNewLap(0, overallPosition, true, -1);
                lapData = PlayerLapData[0];
                lapData.hasMissingSectors = true;
                lapData.IsValid = false;
                lapIsValid = false;
            }
            else
            {
                lapData = PlayerLapData[PlayerLapData.Count - 1];
            }
            if (cumulativeSectorTime <= 0 && gameTimeAtSectorEnd > 0 && lapData.GameTimeAtLapStart > 0)
            {
                cumulativeSectorTime = gameTimeAtSectorEnd - lapData.GameTimeAtLapStart;
            }
            float thisSectorTime;
            if (cumulativeSectorTime > 0 && sectorNumberJustCompleted == 3 && lapData.SectorTimes[0] > 0 && lapData.SectorTimes[1] > 0)
            {
                thisSectorTime = cumulativeSectorTime - lapData.SectorTimes[0] - lapData.SectorTimes[1];
            }
            else if (cumulativeSectorTime > 0 && sectorNumberJustCompleted == 2 && lapData.SectorTimes[0] > 0)
            {
                thisSectorTime = cumulativeSectorTime - lapData.SectorTimes[0];
            }
            else if (cumulativeSectorTime > 0 && sectorNumberJustCompleted == 1)
            {
                thisSectorTime = cumulativeSectorTime;
            }
            else
            {
                // we don't have enough data to calculate this sector time - given that we always drop back to calculated cumulative sector
                // times when the provided time <= 0, this should only happen if we've never actually completed a previous sector. So it's
                // safe to assume any sector < 0 means missing data.
                thisSectorTime = -1;
                lapData.hasMissingSectors = true;
                lapData.IsValid = false;
                lapIsValid = false;
            }
            if (lapIsValid && thisSectorTime > 0)
            {
                if (sectorNumberJustCompleted == 1)
                {
                    LastSector1Time = thisSectorTime;
                    if (PlayerBestSector1Time == -1 || thisSectorTime < PlayerBestSector1Time)
                    {
                        PlayerBestSector1Time = thisSectorTime;
                    }
                }
                else if (sectorNumberJustCompleted == 2)
                {
                    LastSector2Time = thisSectorTime;
                    if (PlayerBestSector2Time == -1 || thisSectorTime < PlayerBestSector2Time)
                    {
                        PlayerBestSector2Time = thisSectorTime;
                    }
                }
                else if (sectorNumberJustCompleted == 3)
                {
                    LastSector3Time = thisSectorTime;
                    if (PlayerBestSector3Time == -1 || thisSectorTime < PlayerBestSector3Time)
                    {
                        PlayerBestSector3Time = thisSectorTime;
                    }
                }                    
            }
            else
            {
                if (sectorNumberJustCompleted == 1)
                {
                    LastSector1Time = -1;
                }
                else if (sectorNumberJustCompleted == 2)
                {
                    LastSector2Time = -1;
                }
                else if (sectorNumberJustCompleted == 3)
                {
                    LastSector3Time = -1;
                }
            }
            lapData.SectorTimes[sectorNumberJustCompleted - 1] = thisSectorTime;
            lapData.SectorPositions[sectorNumberJustCompleted - 1] = overallPosition;
            lapData.GameTimeAtSectorEnd[sectorNumberJustCompleted - 1] = gameTimeAtSectorEnd;
            lapData.Conditions[sectorNumberJustCompleted - 1] = new LapConditions(isRaining, trackTemp, airTemp);
            if (lapData.IsValid && !lapIsValid)
            {
                lapData.IsValid = false;
            }
            
        }

        public float[] getPlayerTimeAndSectorsForBestLap(bool ignoreLast)
        {
            float[] bestLapTimeAndSectorsSectors = new float[] { -1, -1, -1, -1 };
            // Count-1 because we're not interested in the current lap
            int lapsToCheck = PlayerLapData.Count - 1;
            if (ignoreLast)
            {
                --lapsToCheck;
            }
            for (int i = 0; i < lapsToCheck; ++i)
            {
                LapData thisLapTime = PlayerLapData[i];
                if (thisLapTime.IsValid)
                {
                    if (bestLapTimeAndSectorsSectors[0] == -1 ||
                        (thisLapTime.LapTime > 0 && thisLapTime.LapTime < bestLapTimeAndSectorsSectors[0]))
                    {
                        bestLapTimeAndSectorsSectors[0] = thisLapTime.LapTime;
                        if (!thisLapTime.hasMissingSectors)
                        {
                            bestLapTimeAndSectorsSectors[1] = thisLapTime.SectorTimes[0];
                            bestLapTimeAndSectorsSectors[2] = thisLapTime.SectorTimes[1];
                            bestLapTimeAndSectorsSectors[3] = thisLapTime.SectorTimes[2];
                        }
                    }
                }
            }
            return bestLapTimeAndSectorsSectors;
        }
    }

    public class PositionAndMotionData
    {
        public class Rotation
        {
            public float Pitch = 0.0f;
            public float Roll = 0.0f;
            public float Yaw = 0.0f;
        }

        // Unit: Meter per second (m/s).
        public Single CarSpeed = 0;

        // distance (m) from the start line (around the track)
        public Single DistanceRoundTrack = 0;

        public float[] WorldPosition;

        // other stuff: acceleration, orientation, ...

        // not set for all games. Pitch, roll, yaw (all in radians. Not sure what 0 means here - 
        // presumably it's relative to the world rather than the track orientation under the car. Is yaw relative to the track spline or 'north'?).
        // This is only set for R3E currently, and is only used to detect the car rolling over.
        public Rotation Orientation = new Rotation();
    }
    
    public class OpponentData
    {
        // Sometimes the name is corrupted with previous session's data. Worst case is that the name is entirely readable
        // but completely invalid. We must prevent such names being read.
        public Boolean CanUseName = true;

        // set this to false if this opponent drops out of the race (i.e. leaves a server)
        public Boolean IsActive = true;

        // the name read directly from the game data - might be a 'handle' with all kinds of random crap in it
        public String DriverRawName;
        
        //iRacing costumer ID used to check for driver changes.
        public int CostId = -1;

        public Boolean DriverNameSet;

        public int OverallPosition;

        public int OverallPositionAtPreviousTick;

        public int ClassPosition;

        public int ClassPositionAtPreviousTick;

        public float SessionTimeAtLastPositionChange = -1;

        public int CompletedLaps;

        public int CurrentSectorNumber;

        public float Speed;

        public float[] WorldPosition;

        public Boolean IsNewLap;

        public float DistanceRoundTrack;

        public float CurrentBestLapTime = -1;

        public float PreviousBestLapTime = -1;

        public float bestSector1Time = -1;

        public float bestSector2Time = -1;

        public float bestSector3Time = -1;

        public float LastLapTime = -1;

        public Boolean LastLapValid = true;

        private List<LapData> _OpponentLapData;
        public List<LapData> OpponentLapData
        {
            get
            {
                if (_OpponentLapData == null)
                {
                    _OpponentLapData = new List<LapData>();
                }
                return _OpponentLapData;
            }
            set
            {
                _OpponentLapData = value;
            }
        }

        private CarData.CarClass _CarClass;
        public CarData.CarClass CarClass
        {
            get
            {
                if (_CarClass == null)
                {
                    _CarClass = new CarData.CarClass();
                }
                return _CarClass;
            }
            set
            {
                _CarClass = value;
            }
        }

        public Boolean HasStartedExtraLap ;

        public TyreType CurrentTyres = TyreType.Unknown_Race;

        public Boolean isProbablyLastLap ;

        public int IsReallyDisconnectedCounter;

        // be careful with this one, not all games actually set it...
        public Boolean InPits ;
        public Boolean JustEnteredPits ; // true for 1 tick only
        // and this one:
        public int NumPitStops;

        private TrackLandmarksTiming _trackLandmarksTiming;
        public TrackLandmarksTiming trackLandmarksTiming
        {
            get
            {
                if (_trackLandmarksTiming == null)
                {
                    _trackLandmarksTiming = new TrackLandmarksTiming();
                }
                return _trackLandmarksTiming;
            }
            set
            {
                _trackLandmarksTiming = value;
            }
        }

        public String stoppedInLandmark;

        public int PitStopCount;

        // these are only set for R3E
        public Dictionary<int, TyreType> _TyreChangesByLap;
        public Dictionary<int, TyreType> TyreChangesByLap
        {
            get
            {
                if (_TyreChangesByLap == null)
                {
                    _TyreChangesByLap = new Dictionary<int, TyreType>();
                }
                return _TyreChangesByLap;
            }
            set
            {
                _TyreChangesByLap = value;
            }
        }

        public Dictionary<int, TyreType> _BestLapTimeByTyreType;
        public Dictionary<int, TyreType> BestLapTimeByTyreType
        {
            get
            {
                if (_BestLapTimeByTyreType == null)
                {
                    _BestLapTimeByTyreType = new Dictionary<int, TyreType>();
                }
                return _BestLapTimeByTyreType;
            }
            set
            {
                _BestLapTimeByTyreType = value;
            }
        }

        // will be true for 1 tick
        public Boolean hasJustChangedToDifferentTyreType ;

        // this is a bit of a guess - it's actually the race position when the car is 300m(?) from the start line
        public int PositionOnApproachToPitEntry = -1;

        public DeltaTime DeltaTime;

        public bool isApporchingPits;

        public int CarNr = -1;

        private Tuple<String, float> _LicensLevel;
        public Tuple<String, float> LicensLevel
        {
            get
            {
                if (_LicensLevel == null)
                {
                    _LicensLevel = new Tuple<String, float>("invalid", -1);
                }
                return _LicensLevel;
            }
            set
            {
                _LicensLevel = value;
            }
        }

        public int iRating = -1;

        // hack for assetto corsa only. Lap count may be delayed so we capture it at the end of sector1 and use this at lap end
        public int lapCountAtSector1End = -1;

        public override string ToString()
        {
            return DriverRawName + " " + CarClass.getClassIdentifier() + " class position " + ClassPosition + " overall position " 
                + OverallPosition + " lapsCompleted " + CompletedLaps + " lapDist " + DistanceRoundTrack;
        }

        public LapData getCurrentLapData()
        {
            if (OpponentLapData.Count > 0)
            {
                return OpponentLapData[OpponentLapData.Count - 1];
            }
            else
            {
                return null;
            }
        }

        public LapData getLastLapData()
        {
            if (OpponentLapData.Count > 1)
            {
                return OpponentLapData[OpponentLapData.Count - 2];
            }
            else
            {
                return null;
            }
        }

        public float[] getTimeAndSectorsForBestLapInWindow(int lapsToCheck)
        {
            float[] bestLapTimeAndSectorsSectors = new float[] { -1, -1, -1, -1 };
            if (OpponentLapData.Count > 1)
            {
                if (lapsToCheck == -1)
                {
                    lapsToCheck = OpponentLapData.Count;
                }
                // count-2 because we're not interested in the current lap
                for (int i = OpponentLapData.Count - 2; i >= OpponentLapData.Count - lapsToCheck - 1 && i >= 0; i--)
                {
                    LapData thisLapTime = OpponentLapData[i];
                    if (thisLapTime.IsValid)
                    {
                        // note the <= here. Because we're counting backwards this means we'll retrieve the earliest of any identical
                        // laps. Bit of an edge case I suppose...
                        if (bestLapTimeAndSectorsSectors[0] == -1 ||
                            (thisLapTime.LapTime > 0 && thisLapTime.LapTime <= bestLapTimeAndSectorsSectors[0]))
                        {
                            bestLapTimeAndSectorsSectors[0] = thisLapTime.LapTime;
                            if (!thisLapTime.hasMissingSectors)
                            {
                                bestLapTimeAndSectorsSectors[1] = thisLapTime.SectorTimes[0];
                                bestLapTimeAndSectorsSectors[2] = thisLapTime.SectorTimes[1];
                                bestLapTimeAndSectorsSectors[3] = thisLapTime.SectorTimes[2];
                            }
                        }
                    }
                }
            }
            return bestLapTimeAndSectorsSectors;
        }

        public Boolean isEnteringPits()
        {
            LapData currentLap = getCurrentLapData();
            return currentLap != null && currentLap.InLap;
        }

        public Boolean isExitingPits()
        {
            LapData currentLap = getCurrentLapData();
            return currentLap != null && currentLap.OutLap;
        }

        public float getLastLapTime()
        {
            LapData lastLap = getLastLapData();
            if (lastLap != null)
            {
                return lastLap.LapTime;
            }
            else
            {
                return -1;
            }
        }

        public float getGameTimeWhenSectorWasLastCompleted(int sectorNumber)
        {
            // try the last lap
            if (OpponentLapData.Count > 0)
            {
                float time = OpponentLapData[OpponentLapData.Count - 1].GameTimeAtSectorEnd[sectorNumber - 1];
                if (time > 0)
                {
                    return time;
                }
                else if (OpponentLapData.Count > 1)
                {
                    // got back to the lap before
                    time = OpponentLapData[OpponentLapData.Count - 2].GameTimeAtSectorEnd[sectorNumber - 1];
                    if (time > 0)
                    {
                        return time;
                    }
                }
            }
            return -1;
        }

        public void StartNewLap(int lapNumber, int position, Boolean inPits, float gameTimeAtStart, Boolean isRaining, float trackTemp, float airTemp)
        {
            LapData thisLapData = new LapData();
            thisLapData.GameTimeAtLapStart = gameTimeAtStart;
            thisLapData.OutLap = inPits;
            thisLapData.PositionAtStart = position;
            thisLapData.LapNumber = lapNumber;
            OpponentLapData.Add(thisLapData);
        }

        public void CompleteLapWithEstimatedLapTime(int position, float gameTimeAtLapEnd, float worldRecordLapTime, float worldRecordS1Time, float worldRecordS2Time, float worldRecordS3Time,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining,
            TimingData timingData, Boolean isPlayerCarClass)
        {
            // only used by PCars where all tracks have 3 sectors
            AddCumulativeSectorData(3, position, -1, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (OpponentLapData.Count == 1 || !lapData.hasMissingSectors)
                {
                    float estimatedLapTime = lapData.SectorTimes.Sum();
                    LastLapValid = lapData.IsValid;
                    // pcars-specific sanity checks
                    if (lapData.SectorTimes[0] > worldRecordS1Time - 0.1 && lapData.SectorTimes[1] > worldRecordS2Time - 0.1 && lapData.SectorTimes[2] > worldRecordS3Time - 0.1 &&
                        estimatedLapTime > worldRecordLapTime - 0.1 && estimatedLapTime > 0)
                    {
                        lapData.LapTime = estimatedLapTime;
                        LastLapTime = estimatedLapTime;
                        if (lapData.IsValid && lapData.LapTime > 0)
                        {
                            if (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime)
                            {
                                PreviousBestLapTime = CurrentBestLapTime;
                                CurrentBestLapTime = lapData.LapTime;
                            }
                            if (isPlayerCarClass)
                            {
                                timingData.addOpponentPlayerClassLap(lapData.LapTime, lapData.SectorTimes[0], lapData.SectorTimes[1], lapData.SectorTimes[2]);
                            }              
                        }
                    }
                    else
                    {
                        LastLapValid = false;
                        LastLapTime = -1;
                        lapData.IsValid = false;
                    }
                }
                else
                {
                    OpponentLapData.Remove(lapData);
                }
            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }

        public void CompleteLapWithLastSectorTime(int position, float lastSectorTime, float gameTimeAtLapEnd,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining, int numberOfSectors,
            TimingData timingData, Boolean isPlayerCarClass)
        {
            AddSectorData(numberOfSectors, position, lastSectorTime, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (OpponentLapData.Count == 1 || !lapData.hasMissingSectors)
                {
                    float lapTime = lapData.SectorTimes.Sum();
                    LastLapValid = lapData.IsValid;
                    if (LastLapValid)
                    {
                        lapData.LapTime = lapTime;
                        LastLapTime = lapTime;
                        if (lapData.IsValid && lapData.LapTime > 0)
                        {
                            if (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime)
                            {
                                PreviousBestLapTime = CurrentBestLapTime;
                                CurrentBestLapTime = lapData.LapTime;
                            }
                            if (isPlayerCarClass)
                            {
                                timingData.addOpponentPlayerClassLap(lapData.LapTime, lapData.SectorTimes[0], lapData.SectorTimes[1], lapData.SectorTimes[2]);
                            }
                        }
                    }
                    else
                    {
                        LastLapValid = false;
                        LastLapTime = -1;
                        lapData.IsValid = false;
                    }
                }
                else
                {
                     OpponentLapData.Remove(lapData);
                }
            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }

        // used to immediately invalidate a lap only when we're in sector3 - this is necessary because when we start a new lap,
        // the 'current lap is valid' flag will apply to the new lap, not the one we just completed
        public void InvalidateCurrentLap()
        {
            if (OpponentLapData.Count > 0)
            {
                OpponentLapData[OpponentLapData.Count - 1].IsValid = false;
            }
        }

        public void CompleteLapWithProvidedLapTime(int position, float gameTimeAtLapEnd, float providedLapTime, Boolean lapWasValid, Boolean inLap,
            Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining, int numberOfSectors,
            TimingData timingData, Boolean isPlayerCarClass)
        {
            // if this completed lap is invalid, mark it as such *before* we complete it
            if (!lapWasValid)
            {
                InvalidateCurrentLap();
            }
            CompleteLapWithProvidedLapTime(position, gameTimeAtLapEnd, providedLapTime, InPits, isRaining, trackTemp, airTemp, sessionLengthIsTime, 
                sessionTimeRemaining, numberOfSectors, timingData, isPlayerCarClass);
        }

        public void CompleteLapWithProvidedLapTime(int position, float gameTimeAtLapEnd, float providedLapTime, Boolean inPits,
            Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining, int numberOfSectors,
            TimingData timingData, Boolean isPlayerCarClass)
        {
            if (OpponentLapData.Count > 0)
            {                
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1]; 
                if (OpponentLapData.Count == 1 || !lapData.hasMissingSectors) 
                {
                    AddCumulativeSectorData(numberOfSectors, position, providedLapTime, gameTimeAtLapEnd, lapData.IsValid, isRaining, trackTemp, airTemp);
                    lapData.LapTime = providedLapTime;
                    lapData.InLap = inPits;
                    LastLapTime = providedLapTime;
                    if (lapData.IsValid && lapData.LapTime > 0)
                    {
                        if (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime)
                        {
                            PreviousBestLapTime = CurrentBestLapTime;
                            CurrentBestLapTime = lapData.LapTime;
                        }
                        if (isPlayerCarClass)
                        {
                            timingData.addOpponentPlayerClassLap(lapData.LapTime, lapData.SectorTimes[0], lapData.SectorTimes[1], lapData.SectorTimes[2]);
                        }
                    }
                    LastLapValid = lapData.IsValid;
                } 
                else
                { 
                    OpponentLapData.Remove(lapData);
                }
            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }
        public void CompleteLapThatMightHaveMissingSectorTimes(int position, float gameTimeAtLapEnd, float providedLapTime, Boolean lapWasValid,
            Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining, int numberOfSectors,
            TimingData timingData, Boolean isPlayerCarClass)
        {
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                lapData.IsValid = lapWasValid;
                if (!lapData.hasMissingSectors || lapData.SectorTimes[0] > 0 && lapData.SectorTimes[1] > 0)
                {
                    AddCumulativeSectorData(numberOfSectors, position, providedLapTime, gameTimeAtLapEnd, lapData.IsValid, isRaining, trackTemp, airTemp);
                }
                else
                {
                    for (int i = 0; i < 2; i++)
                    {
                        lapData.SectorTimes[i] = -1;
                        lapData.SectorPositions[i] = position;
                        lapData.GameTimeAtSectorEnd[i] = gameTimeAtLapEnd;
                        lapData.Conditions[i] = new LapConditions(isRaining, trackTemp, airTemp);
                    }
                }
                lapData.LapTime = providedLapTime;
                LastLapTime = providedLapTime;
                if (lapData.IsValid && lapData.LapTime > 0)
                {
                    if (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime)
                    {
                        PreviousBestLapTime = CurrentBestLapTime;
                        CurrentBestLapTime = lapData.LapTime;
                    }
                    if (isPlayerCarClass)
                    {
                        timingData.addOpponentPlayerClassLap(lapData.LapTime, lapData.SectorTimes[0], lapData.SectorTimes[1], lapData.SectorTimes[2]);
                    }
                }
                LastLapValid = lapData.IsValid;

            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }
        public void AddCumulativeSectorData(int sectorNumberJustCompleted, int position, float cumulativeSectorTime, float gameTimeAtSectorEnd, Boolean lapIsValid, 
            Boolean isRaining, float trackTemp, float airTemp)
        {
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (cumulativeSectorTime <= 0)
                {
                    cumulativeSectorTime = gameTimeAtSectorEnd - lapData.GameTimeAtLapStart;
                }
                float thisSectorTime;
                if (sectorNumberJustCompleted >= 3 && lapData.SectorTimes[0] > 0 && lapData.SectorTimes[1] > 0)
                {
                    thisSectorTime = cumulativeSectorTime - lapData.SectorTimes[0] - lapData.SectorTimes[1];
                }
                else if (sectorNumberJustCompleted == 2 && lapData.SectorTimes[0] > 0)
                {
                    thisSectorTime = cumulativeSectorTime - lapData.SectorTimes[0];
                }
                else if (sectorNumberJustCompleted == 1)
                {
                    thisSectorTime = cumulativeSectorTime;
                }
                else
                {
                    // we don't have enough data to calculate this sector time - given that we always drop back to calculated cumulative sector
                    // times when the provided time <= 0, this should only happen if we've never actually completed a previous sector. So it's
                    // safe to assume any sector < 0 means missing data.
                    thisSectorTime = -1;
                    lapData.hasMissingSectors = true;
                }
                
                if (lapIsValid && thisSectorTime > 0)
                {
                    if (sectorNumberJustCompleted == 1 && (bestSector1Time == -1 || thisSectorTime < bestSector1Time))
                    {
                        bestSector1Time = thisSectorTime;
                    }
                    if (sectorNumberJustCompleted == 2 && (bestSector2Time == -1 || thisSectorTime < bestSector2Time))
                    {
                        bestSector2Time = thisSectorTime;
                    }
                    if (sectorNumberJustCompleted == 3 && (bestSector3Time == -1 || thisSectorTime < bestSector3Time))
                    {
                        bestSector3Time = thisSectorTime;
                    }
                }
                // special case here - if a track has > 3 sectors, accumulate the data for all sectors > 3 into sector3
                if (sectorNumberJustCompleted > 3)
                {
                    sectorNumberJustCompleted = 3;
                }
                
                lapData.SectorTimes[sectorNumberJustCompleted - 1] = thisSectorTime;
                lapData.SectorPositions[sectorNumberJustCompleted - 1] = position;
                lapData.GameTimeAtSectorEnd[sectorNumberJustCompleted - 1] = gameTimeAtSectorEnd;
                lapData.Conditions[sectorNumberJustCompleted - 1] = new LapConditions(isRaining, trackTemp, airTemp);
                if (lapData.IsValid && !lapIsValid)
                {
                    lapData.IsValid = false;
                }
            }
        }

        public void AddSectorData(int sectorNumberJustCompleted, int position, float thisSectorTime, float gameTimeAtSectorEnd,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                lapData.SectorTimes[sectorNumberJustCompleted - 1] = thisSectorTime;

                // fragile code here. If the lap is invalid PCars network mode sends -1 (-123 actually but never mind). If the data is just missing (we had no sectorX time info) 
                // then we'll have 0. So looking for sectorTime[x] == 0 is different from looking for -1
                if ((sectorNumberJustCompleted == 2 && lapData.SectorTimes[0] == 0) || (sectorNumberJustCompleted == 3 && lapData.SectorTimes[1] == 0))
                {
                    lapData.hasMissingSectors = true;
                }
                if (lapIsValid && thisSectorTime > 0)
                {
                    if (sectorNumberJustCompleted == 1 && (bestSector1Time == -1 || thisSectorTime < bestSector1Time))
                    {
                        bestSector1Time = thisSectorTime;
                    }
                    if (sectorNumberJustCompleted == 2 && (bestSector2Time == -1 || thisSectorTime < bestSector2Time))
                    {
                        bestSector2Time = thisSectorTime;
                    }
                    if (sectorNumberJustCompleted == 3 && (bestSector3Time == -1 || thisSectorTime < bestSector3Time))
                    {
                        bestSector3Time = thisSectorTime;
                    }
                }
                lapData.SectorTimes[sectorNumberJustCompleted - 1] = thisSectorTime;
                lapData.SectorPositions[sectorNumberJustCompleted - 1] = position;
                lapData.GameTimeAtSectorEnd[sectorNumberJustCompleted - 1] = gameTimeAtSectorEnd;
                lapData.Conditions[sectorNumberJustCompleted - 1] = new LapConditions(isRaining, trackTemp, airTemp);
                if (lapData.IsValid && !lapIsValid)
                {
                    lapData.IsValid = false;
                }
            }
        }

        public void setInLap()
        {
            if (OpponentLapData.Count > 0)
            {
                OpponentLapData[OpponentLapData.Count - 1].InLap = true;
            }
            else
            {
                LapData lapData = new LapData();
                lapData.InLap = true;
                OpponentLapData.Add(lapData);
            }
        }

        public DateTime NewLapDataTimerExpiry = DateTime.MaxValue;
        public Boolean WaitingForNewLapData = false;
        public int CompletedLapsWhenHasNewLapDataWasLastTrue = -2;
        public float GameTimeWhenLastCrossedStartFinishLine = -1;

        public bool HasNewLapData(float gameProvidedLastLapTime, bool hasCrossedSFLine, int completedLaps, Boolean isRace, float sessionRunningTime, Boolean previousOpponentDataWaitingForNewLapData,
            DateTime previousOpponentNewLapDataTimerExpiry, float previousOpponentLastLapTime, Boolean previousOpponentLastLapValid,
            int previousOpponentCompletedLapsWhenHasNewLapDataWasLastTrue, float previousOpponentGameTimeWhenLastCrossedStartFinishLine)
        {
            // here we need to make sure that CompletedLaps is bigger then CompletedLapsWhenHasNewLapDataWasLastTrue
            // else the user will have jumped to pits 
            if ((hasCrossedSFLine && completedLaps > CompletedLapsWhenHasNewLapDataWasLastTrue) || (isRace && hasCrossedSFLine))
            {
                // reset the timer and start waiting for an updated laptime...
                this.WaitingForNewLapData = true;
                this.NewLapDataTimerExpiry = DateTime.UtcNow.Add(TimeSpan.FromSeconds(3));
                this.GameTimeWhenLastCrossedStartFinishLine = sessionRunningTime;
            }
            else
            {
                // not a new lap but may be waiting, so copy over the wait variables
                this.WaitingForNewLapData = previousOpponentDataWaitingForNewLapData;
                this.NewLapDataTimerExpiry = previousOpponentNewLapDataTimerExpiry;
                this.GameTimeWhenLastCrossedStartFinishLine = previousOpponentGameTimeWhenLastCrossedStartFinishLine;
            }
            // if we're waiting, see if the timer has expired or we have a change in the previous laptime value
            if (this.WaitingForNewLapData && (previousOpponentLastLapTime != gameProvidedLastLapTime || DateTime.UtcNow > this.NewLapDataTimerExpiry))
            {
                // the timer has expired or we have new data
                this.WaitingForNewLapData = false;
                this.LastLapTime = gameProvidedLastLapTime;
                this.LastLapValid = gameProvidedLastLapTime > 1;
                this.CompletedLapsWhenHasNewLapDataWasLastTrue = completedLaps;
                return true;
            }
            else
            {
                this.LastLapTime = previousOpponentLastLapTime;
                this.LastLapValid = previousOpponentLastLapValid;
                this.CompletedLapsWhenHasNewLapDataWasLastTrue = previousOpponentCompletedLapsWhenHasNewLapDataWasLastTrue;
            }
            return false;
        }
    }

    public class TrackLandmarksTiming
    {
        // value object for a single set of timings for 1 landmark
        private class TrackLandmarksTimingData
        {
            // [time, startSpeed, endSpeed]
            public List<float[]> timesAndSpeeds = new List<float[]>();
            public Boolean isCommonOvertakingSpot;
            public TrackLandmarksTimingData(Boolean isCommonOvertakingSpot)
            {
                this.isCommonOvertakingSpot = isCommonOvertakingSpot;
            }
            public void addTimeAndSpeeds(float time, float startSpeed, float endSpeed)
            {
                timesAndSpeeds.Insert(0, new float[] { time, startSpeed, endSpeed });
            }
        }

        public enum DeltaType
        {
            EntrySpeed, Time
        }
        public class LandmarkAndDeltaType
        {
            public DeltaType deltaType;
            public String landmarkName;
            public LandmarkAndDeltaType(DeltaType deltaType, String landmarkName)
            {
                this.deltaType = deltaType;
                this.landmarkName = landmarkName;
            }
        }

        // value object for the biggest difference (speed or time)
        private class LandmarkDeltaContainer
        {
            public float biggestTimeDifference = -1;
            public float biggestStartSpeedDifference = -1;
            public String biggestTimeDifferenceLandmark ;
            public String biggestStartSpeedDifferenceLandmark;
            public LandmarkDeltaContainer(float biggestTimeDifference, String biggestTimeDifferenceLandmark, float biggestStartSpeedDifference, String biggestStartSpeedDifferenceLandmark)
            {
                this.biggestTimeDifference = biggestTimeDifference;
                this.biggestTimeDifferenceLandmark = biggestTimeDifferenceLandmark;
                this.biggestStartSpeedDifference = biggestStartSpeedDifference;
                this.biggestStartSpeedDifferenceLandmark = biggestStartSpeedDifferenceLandmark;
            }

            public LandmarkAndDeltaType selectLandmark()
            {
                if (biggestTimeDifferenceLandmark != null && biggestStartSpeedDifferenceLandmark != null)
                {
                    // which to choose?? If the entry speed delta > minSignificantRelativeTimeDiffOvertakingSpot
                    if (biggestStartSpeedDifference > minSignificantRelativeTimeDiffOvertakingSpot)
                    {
                        Console.WriteLine("Biggest speed delta into " + biggestStartSpeedDifferenceLandmark + ": " + biggestStartSpeedDifference * 100 + "% difference");
                        return new LandmarkAndDeltaType(DeltaType.EntrySpeed, biggestStartSpeedDifferenceLandmark);
                    }
                    else
                    {
                        Console.WriteLine("Biggest time delta through " + biggestTimeDifferenceLandmark + ": " + biggestTimeDifference * 100 + "% difference");
                        return new LandmarkAndDeltaType(DeltaType.Time, biggestTimeDifferenceLandmark);
                    }
                }
                else if (biggestStartSpeedDifferenceLandmark != null)
                {
                    Console.WriteLine("Biggest speed delta into " + biggestStartSpeedDifferenceLandmark + ": " + biggestStartSpeedDifference * 100 + "% difference");
                    return new LandmarkAndDeltaType(DeltaType.EntrySpeed, biggestStartSpeedDifferenceLandmark);
                }
                else
                {
                    if (biggestTimeDifferenceLandmark != null)
                    {
                        Console.WriteLine("Biggest time delta through " + biggestTimeDifferenceLandmark + ": " + biggestTimeDifference * 100 + "% difference");
                    }
                    return new LandmarkAndDeltaType(DeltaType.Time, biggestTimeDifferenceLandmark);
                }
            }
        }
        
        
        // the timing difference will have errors in it, depending on how accurate the vehicle speed data is

        // don't count time differences shorter than these - no point in being told to defend into a corner when
        // the other guys is only 0.01 seconds faster through that corner
        // These are used when we're checking time / speed difference at common overtaking spots
        private static float minSignificantRelativeTimeDiffOvertakingSpot = 0.07f;    // 7% - is this a good value?
        private static float minSignificantRelativeStartSpeedDiffOvertakingSpot = 0.1f;   // 10% - is this a good value? 

        // these values are used when we're responding to a voice command, so are more generous
        private static float minSignificantRelativeTimeDiffOvertakingSpotForVoiceCommand = 0f;    // as long as we're not slower we'll report
        private static float minSignificantRelativeStartSpeedDiffOvertakingSpotForVoiceCommand = 0f;   // as long as we're not slower we'll report

        // these are used when we're checking time / speed difference at places where overtaking is rare, so need to be bigger 
        private static float minSignificantRelativeTimeDiff = 0.10f;    // 10% - is this a good value?
        private static float minSignificantRelativeStartSpeedDiff = 0.13f;   // 13% - is this a good value? 

        // these values are used when we're responding to a voice command, so are more generous
        private static float minSignificantRelativeTimeDiffForVoiceCommand = 0.03f;    // 3% - is this a good value?
        private static float minSignificantRelativeStartSpeedDiffForVoiceCommand = 0.05f;   // 5% - is this a good value?

        private Dictionary<string, TrackLandmarksTimingData> sessionData = new Dictionary<string, TrackLandmarksTimingData>();

        // temporary variables for tracking landmark timings during a session - we add a timing when these are non-null and
        // we hit the end of this named landmark.
        private String landmarkNameStart ;
        private float landmarkStartTime = -1;
        private float landmarkStartSpeed = -1;
        private int landmarkStoppedCount;

        // wonder if this'll work...
        private String nearLandmarkName;

        // quick n dirty tracking of when we're at the mid-point of a landmark - maybe the apex. This is only non-null for a single tick.
        public String atMidPointOfLandmark;

        private void addTimeAndSpeeds(String landmarkName, float time, float startSpeed, float endSpeed, Boolean isCommonOvertakingSpot)
        {
            if (time > 0)
            {
                TrackLandmarksTimingData tltd = null;
                if (!sessionData.TryGetValue(landmarkName, out tltd))
                {
                    tltd = new TrackLandmarksTimingData(isCommonOvertakingSpot);
                    sessionData.Add(landmarkName, tltd);
                }
                tltd.addTimeAndSpeeds(time, startSpeed, endSpeed);
            }
        }
        
        // returns [timeInSection, entrySpeed, exitSpeed] for the quickest time through that section
        public float[] getBestTimeAndSpeeds(String landmarkName, int lapsToCheck, int minTimesRequired)
        {
            TrackLandmarksTimingData trackLandmarksTimingData = null;
            if (!sessionData.TryGetValue(landmarkName, out trackLandmarksTimingData))
            {
                return null;
            }
            float[] bestTimeAndSpeeds = new float[] { float.MaxValue, -1f, 1f };
            if (trackLandmarksTimingData.timesAndSpeeds.Count < minTimesRequired)
            {
                return null;
            }
            for (int i = 0; i < lapsToCheck; i++)
            {
                if (trackLandmarksTimingData.timesAndSpeeds.Count > i && trackLandmarksTimingData.timesAndSpeeds[i][0] < bestTimeAndSpeeds[0])
                {
                    bestTimeAndSpeeds = trackLandmarksTimingData.timesAndSpeeds[i];
                }
            }
            return bestTimeAndSpeeds;
        }

        // get the landmark name where I'm either much faster through the section or
        // am about as fast but have significantly higher entry speed
        public LandmarkAndDeltaType getLandmarkWhereIAmFaster(TrackLandmarksTiming otherVehicleTrackLandMarksTiming, Boolean preferCommonOvertakingSpots, Boolean forVoiceCommand)
        {
            LandmarkDeltaContainer deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, true, preferCommonOvertakingSpots, forVoiceCommand);
            if (deltasForCommonOvertakingSpots.biggestStartSpeedDifferenceLandmark == null && deltasForCommonOvertakingSpots.biggestTimeDifferenceLandmark == null && preferCommonOvertakingSpots)
            {
                // no hits for common overtaking spots so try again
                deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, true, false, forVoiceCommand);
            }
            // this can contain 2 different results (one for the biggest entry speed difference, one for the biggest relative time difference)
            return deltasForCommonOvertakingSpots.selectLandmark();
        }

        // get the landmark name where I'm either much faster through the section or
        // am about as fast but have significantly higher entry speed
        public LandmarkAndDeltaType getLandmarkWhereIAmSlower(TrackLandmarksTiming otherVehicleTrackLandMarksTiming, Boolean preferCommonOvertakingSpots, Boolean forVoiceCommand)
        {
            LandmarkDeltaContainer deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, false, preferCommonOvertakingSpots, forVoiceCommand);
            if (deltasForCommonOvertakingSpots.biggestStartSpeedDifferenceLandmark == null && deltasForCommonOvertakingSpots.biggestTimeDifferenceLandmark == null && preferCommonOvertakingSpots)
            {
                // no hits for common overtaking spots so try again
                deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, false, false, forVoiceCommand);
            }
            // this can contain 2 different results (one for the biggest entry speed difference, one for the biggest relative time difference)
            return deltasForCommonOvertakingSpots.selectLandmark();
        }

	    private LandmarkDeltaContainer getLandmarksWithBiggestDeltas(TrackLandmarksTiming otherVehicleTrackLandMarksTiming, Boolean whereImFaster, Boolean preferCommonOvertakingSpots, Boolean forVoiceCommand)
        {
            int lapsToCheck = 5;
            int minTimesRequired = forVoiceCommand ? 2 : 3;
            float biggestTimeDifference = -1;
            float biggestStartSpeedDifference = -1;
            String biggestTimeDifferenceLandmark = null;
            String biggestSpeedDifferenceLandmark = null;
            foreach (KeyValuePair<string, TrackLandmarksTimingData> entry in this.sessionData)
            {
                String landmarkName = entry.Key;
                TrackLandmarksTimingData thisTiming = entry.Value;
                if (!preferCommonOvertakingSpots || thisTiming.isCommonOvertakingSpot)
                {
                    float minSignificantRelativeTimeDiffToUse;
                    float minSignificantRelativeStartSpeedDiffToUse;
                    if (thisTiming.isCommonOvertakingSpot)
                    {
                        minSignificantRelativeTimeDiffToUse = forVoiceCommand ? minSignificantRelativeTimeDiffOvertakingSpotForVoiceCommand : minSignificantRelativeTimeDiffOvertakingSpot;
                        minSignificantRelativeStartSpeedDiffToUse = forVoiceCommand ? minSignificantRelativeStartSpeedDiffOvertakingSpotForVoiceCommand : minSignificantRelativeStartSpeedDiffOvertakingSpot;
                    }
                    else
                    {
                        minSignificantRelativeTimeDiffToUse = forVoiceCommand ? minSignificantRelativeTimeDiffForVoiceCommand : minSignificantRelativeTimeDiff;
                        minSignificantRelativeStartSpeedDiffToUse = forVoiceCommand ? minSignificantRelativeStartSpeedDiffForVoiceCommand : minSignificantRelativeStartSpeedDiff;
                    }

                    float[] myBestTimeAndSpeeds = getBestTimeAndSpeeds(landmarkName, lapsToCheck, minTimesRequired);
                    float[] otherBestTimeAndSpeeds = otherVehicleTrackLandMarksTiming.getBestTimeAndSpeeds(landmarkName, lapsToCheck, minTimesRequired);
                    // for times, other - mine if we want sections where I'm faster (more positive => better), 
                    // or mine - other if we want sections where he's faster (more positive => worse)
                    if (myBestTimeAndSpeeds != null && otherBestTimeAndSpeeds != null)
                    {
                        float relativeTimeDelta = whereImFaster ? (otherBestTimeAndSpeeds[0] - myBestTimeAndSpeeds[0]) / myBestTimeAndSpeeds[0] :
                                                          (myBestTimeAndSpeeds[0] - otherBestTimeAndSpeeds[0]) / myBestTimeAndSpeeds[0];
                        // for speeds, mine - other if we want sections where I'm faster (more positive => better),
                        // or other - mine if we want sections where he's faster (more positive => worse)
                        float relativeStartSpeedDelta = whereImFaster ? (myBestTimeAndSpeeds[1] - otherBestTimeAndSpeeds[1]) / myBestTimeAndSpeeds[1] :
                                                                (otherBestTimeAndSpeeds[1] - myBestTimeAndSpeeds[1]) / myBestTimeAndSpeeds[1];
                        // Console.WriteLine(landmarkName + " entry diff = " + relativeStartSpeedDelta + " through diff = " + relativeTimeDelta);
                        if (relativeTimeDelta >= minSignificantRelativeTimeDiffToUse && relativeTimeDelta > biggestTimeDifference)
                        {
                            // this is the biggest (so far) relative time difference
                            biggestTimeDifference = relativeTimeDelta;
                            biggestTimeDifferenceLandmark = landmarkName;
                        }

                        // additional check here - compare the entry speeds but only if the total speed through this section is no worse than our opponent
                        // - there's no point in barrelling in and ballsing up the exit
                        if (relativeStartSpeedDelta > minSignificantRelativeStartSpeedDiffToUse && relativeStartSpeedDelta > biggestStartSpeedDifference &&
                            relativeTimeDelta > 0)
                        {
                            // this is the biggest (so far) relative speed difference
                            biggestStartSpeedDifference = relativeStartSpeedDelta;
                            biggestSpeedDifferenceLandmark = landmarkName;
                        }
                    }
                }
            }
            return new LandmarkDeltaContainer(biggestTimeDifference, biggestTimeDifferenceLandmark, biggestStartSpeedDifference, biggestSpeedDifferenceLandmark);
        }

        // called for every opponent and the player for each tick
        // TODO: does including current speed in this calculation really reduce the max error? The speed data can be noisy for some
        // games so this might cause more problems than it solves.
        //
        // returns null or a landmark name this car is stopped in
        public String updateLandmarkTiming(TrackDefinition trackDefinition, float gameTime, float previousDistanceRoundTrack, float currentDistanceRoundTrack, float speed) 
        {
            if (trackDefinition == null || trackDefinition.trackLandmarks == null || trackDefinition.trackLandmarks.Count == 0 ||
                gameTime < 30 || 
                (CrewChief.isPCars() && (currentDistanceRoundTrack == 0 || speed == 0)))
            {
                // don't collect data if the session has been running < 30 seconds or we're PCars and the distanceRoundTrack or speed is exactly zero
                return null;
            }
            // yuk...
            atMidPointOfLandmark = null;
            if (landmarkNameStart == null) 
            {
                // looking for landmark start only
                foreach (TrackLandmark trackLandmark in trackDefinition.trackLandmarks)
                {
                    if (previousDistanceRoundTrack < trackLandmark.distanceRoundLapStart && currentDistanceRoundTrack >= trackLandmark.distanceRoundLapStart) 
                    {
                        if (currentDistanceRoundTrack - 20 < trackLandmark.distanceRoundLapStart && currentDistanceRoundTrack + 20 > trackLandmark.distanceRoundLapStart)
                        {
                            // only start the timing process if we're near the landmark start point
                            // adjust the landmarkStartTime a bit to accommodate position errors
                            float error = speed > 0 && speed < 120 ? (currentDistanceRoundTrack - trackLandmark.distanceRoundLapStart) / speed : 0;
                            landmarkStartTime = gameTime - error;
                            landmarkStartSpeed = speed;                            
                        }
                        landmarkNameStart = trackLandmark.landmarkName;
                        // don't reset the landmarkStoppedCount when we enter the landmark - do this in the proximity check below
                        break;
                    }		
                }
            } else {
                // looking for landmark end only
                foreach (TrackLandmark trackLandmark in trackDefinition.trackLandmarks) 
                {
                    if (trackLandmark.landmarkName == landmarkNameStart) 
                    {
                        if (currentDistanceRoundTrack >= trackLandmark.distanceRoundLapStart && currentDistanceRoundTrack < trackLandmark.distanceRoundLapEnd)
                        {
                            // we're in the landmark zone somewhere
                            // if this car is very slow, increment the stopped counter
                            if (speed < 5)
                            {
                                landmarkStoppedCount++;
                            }
                            if (previousDistanceRoundTrack < trackLandmark.getMidPoint() && currentDistanceRoundTrack >= trackLandmark.getMidPoint())
                            {
                                atMidPointOfLandmark = trackLandmark.landmarkName;
                            }
                        }
                        else if (previousDistanceRoundTrack < trackLandmark.distanceRoundLapEnd && currentDistanceRoundTrack >= trackLandmark.distanceRoundLapEnd)
                        {
                            // we've reached the end of a landmark section
                            // update the timing if it's the landmark we're expecting, we're actually close to the endpoint and
                            // we collected some proper data when we entered the landmark
                            if (currentDistanceRoundTrack - 20 < trackLandmark.distanceRoundLapEnd && currentDistanceRoundTrack + 20 > trackLandmark.distanceRoundLapEnd && 
                                landmarkStartTime != -1)
                            {
                                // only save the timing if we're near the landmark end point
                                // adjust the landmarkEndTime a bit to accommodate position errors
                                float error = speed > 0 && speed < 120 ? (currentDistanceRoundTrack - trackLandmark.distanceRoundLapEnd) / speed : 0;
                                addTimeAndSpeeds(landmarkNameStart, (gameTime - error) - landmarkStartTime, landmarkStartSpeed, speed, trackLandmark.isCommonOvertakingSpot);
                            }
                            landmarkNameStart = null;
                            landmarkStartTime = -1;
                            landmarkStartSpeed = -1;
                        }
                        else
                        {
                            // we're not in the landmark at all but we never reached the end, so stop looking for the end
                            // This happens when we quit to the pits or when a car leaves the track and rejoins at a different location
                            landmarkNameStart = null;
                            landmarkStartTime = -1;
                            landmarkStartSpeed = -1;
                            // we've left the landmark but haven't crossed the end trigger. We could be anywhere - even in the pit (for PCars). We
                            // don't want the stopped count for this section to carry over as we might reappear in the middle of a different
                            // section, so zero the counter
                            landmarkStoppedCount = 0;
                        }
                        break;
                    }
                }
            }
            Boolean nearLandmark = false;
            // now some landmark proximity stuff
            if (landmarkNameStart == null)
            {
                // again, we're waiting to enter a landmark zone - perhaps we've just left a zone so still check for stopped cars       
  
                // TODO: refactor this - there's already a method in TrackData to get a landmark for a given track distance, with a 70 metre 'near' zone
                foreach (TrackLandmark trackLandmark in trackDefinition.trackLandmarks) 
                {
                    if (currentDistanceRoundTrack > Math.Max(0, trackLandmark.distanceRoundLapStart - 70) &&
                        currentDistanceRoundTrack < Math.Min(trackDefinition.trackLength, trackLandmark.distanceRoundLapEnd + 70))
                    {
                        if (nearLandmarkName != trackLandmark.landmarkName)
                        {
                            landmarkStoppedCount = 0;
                        }
                        nearLandmarkName = trackLandmark.landmarkName;
                        nearLandmark = true;
                        // if this car is very slow, increment the stopped counter
                        if (speed < 5)
                        {
                            landmarkStoppedCount++;
                        }
                        break;
                    }
                }
                if (!nearLandmark)
                {
                    landmarkStoppedCount = 0;
                    nearLandmarkName = null;
                }
            }

            if (landmarkStoppedCount >= 20)
            {
                // slow for more than 2 seconds - this assumes 1 tick is 100ms, which isn't necessarily valid but it's close enough. 
                return landmarkNameStart == null ? nearLandmarkName : landmarkNameStart;
            }
            else
            {
                return null;
            }
        }

        // call this at the start of every lap so we don't end up waiting for ever (or for 1lap + landmark time).
        // Note that this means no landmarks can include the start line, but this is probably OK.
        public void cancelWaitingForLandmarkEnd()
        {
            landmarkNameStart = null;
            landmarkStartTime = -1;
            landmarkStartSpeed = -1;
        }
    }

    public class LapData
    {
        public int LapNumber;
        public int PositionAtStart;
        public Boolean IsValid = true;
        public float LapTime = -1;
        public float GameTimeAtLapStart;
        private float[] _SectorTimes;
        public float[] SectorTimes
        {
            get
            {
                if (_SectorTimes == null)
                {
                    _SectorTimes = new float[3];
                }
                return _SectorTimes;
            }
            set
            {
                _SectorTimes = value;
            }
        }
        private float[] _GameTimeAtSectorEnd;
        public float[] GameTimeAtSectorEnd
        {
            get
            {
                if (_GameTimeAtSectorEnd == null)
                {
                    _GameTimeAtSectorEnd = new float[3];
                }
                return _GameTimeAtSectorEnd;
            }
            set
            {
                _GameTimeAtSectorEnd = value;
            }
        }
        private LapConditions[] _Conditions;
        public LapConditions[] Conditions
        {
            get
            {
                if (_Conditions == null)
                {
                    _Conditions = new LapConditions[3];
                }
                return _Conditions;
            }
            set
            {
                _Conditions = value;
            }
        }
        private int[] _SectorPositions;
        public int[] SectorPositions
        {
            get
            {
                if (_SectorPositions == null)
                {
                    _SectorPositions = new int[3];
                }
                return _SectorPositions;
            }
            set
            {
                _SectorPositions = value;
            }
        }
        public Boolean OutLap;
        public Boolean InLap;
        public Boolean hasMissingSectors;
    }

    public class LapConditions
    {
        public Boolean Wet = false;
        public float TrackTemp = 30;
        public float AirTemp = 25;
        public LapConditions(Boolean wet, float trackTemp, float airTemp)
        {
            this.Wet = wet;
            this.TrackTemp = trackTemp;
            this.AirTemp = airTemp;
        }
    }

    public class ControlData
    {
        // ...
        public ControlType ControlType = ControlType.Unavailable;

        // ...
        public Single ThrottlePedal;

        // ...
        public Single BrakePedal;

        // ...
        public Single ClutchPedal;

        // ...
        public Single BrakeBias;
    }

    public class PitData
    {
        public PitWindow PitWindow = PitWindow.Unavailable;

        // The minute/lap into which you're allowed/obligated to pit
        // Unit: Minutes in time-based sessions, otherwise lap

        public Boolean InPitlane;

        public Boolean IsApproachingPitlane;

        public Boolean OnInLap;

        public Boolean OnOutLap;

        public Boolean IsMakingMandatoryPitStop;

        // the pit window stuff isn't right here - the state can be 'completed' but then change to something
        // else, so we need to keep track of whether we've completed a mandatory stop separately.
        public Boolean MandatoryPitStopCompleted;

        // this is true for one tick, when the player is about to exit the pits
        public Boolean IsAtPitExit;

        public Boolean IsRefuellingAllowed;

        public Boolean IsElectricVehicleSwapAllowed;

        public Boolean HasRequestedPitStop;

        public Boolean PitStallOccupied;

        public Boolean LeaderIsPitting;

        public Boolean CarInFrontIsPitting;

        public Boolean CarBehindIsPitting;

        // yuk...
        public OpponentData OpponentForLeaderPitting;
        public OpponentData OpponentForCarAheadPitting;
        public OpponentData OpponentForCarBehindPitting;

        public int PitWindowStart;

        // The minute/lap into which you can/should pit
        // Unit: Minutes in time based sessions, otherwise lap
        public int PitWindowEnd;

        public Boolean HasMandatoryPitStop;

        public Boolean HasMandatoryTyreChange;

        public TyreType MandatoryTyreChangeRequiredTyreType = TyreType.Unknown_Race;

        // might be a number of laps or a number of minutes. These are (currently) for DTM 2014. If we start on Options, 
        // MaxPermittedDistanceOnCurrentTyre will be half race distance (rounded down), if we start on Primes 
        // MinPermittedDistanceOnCurrentTyre will be half race distance (rounded up)
        public int MaxPermittedDistanceOnCurrentTyre = -1;
        public int MinPermittedDistanceOnCurrentTyre = -1;

        // -1 == n/a; 0 = inactive; 1 = active
        public int limiterStatus = -1;

        // RF1/RF2 hack for mandatory pit stop windows, which are used to trigger 'box now' messages
        public Boolean ResetEvents;

        public int NumPitStops;

        public Boolean IsPitCrewDone;

        public Boolean IsPitCrewReady;

        public float PitSpeedLimit = -1.0f;

        // distance round track of pit box
        public float PitBoxPositionEstimate = -1.0f;

        public Boolean IsTeamRacing;

        public Boolean JumpedToPits;

        public Boolean IsInGarage;
    }

    public class PenatiesData
    {
        public Boolean HasDriveThrough;

        public Boolean HasStopAndGo;

        // from R3E data - what is this??
        public Boolean HasPitStop;

        public Boolean HasTimeDeduction;

        public Boolean HasSlowDown;

        // Number of penalties pending for the player
        public int NumPenalties;

        // Total number of cut track warnings
        public int CutTrackWarnings;

        public Boolean IsOffRacingSurface;

        public Boolean PossibleTrackLimitsViolation;
    }

    public class TyreData
    {
        public Boolean LeftFrontAttached = true;
        public Boolean RightFrontAttached = true;
        public Boolean LeftRearAttached = true;
        public Boolean RightRearAttached = true;

        public Boolean TyreWearActive;

        // true if all tyres are the same type
        public Boolean HasMatchedTyreTypes = true;

        public TyreType FrontLeftTyreType = TyreType.Unknown_Race;
        public TyreType FrontRightTyreType = TyreType.Unknown_Race;
        public TyreType RearLeftTyreType = TyreType.Unknown_Race;
        public TyreType RearRightTyreType = TyreType.Unknown_Race;
        public String TyreTypeName = "";

        public Single FrontLeft_LeftTemp;
        public Single FrontLeft_CenterTemp;
        public Single FrontLeft_RightTemp;

        public Single FrontRight_LeftTemp;
        public Single FrontRight_CenterTemp;
        public Single FrontRight_RightTemp;

        public Single RearLeft_LeftTemp;
        public Single RearLeft_CenterTemp;
        public Single RearLeft_RightTemp;

        public Single RearRight_LeftTemp;
        public Single RearRight_CenterTemp;
        public Single RearRight_RightTemp;

        public Single PeakFrontLeftTemperatureForLap;
        public Single PeakFrontRightTemperatureForLap;
        public Single PeakRearLeftTemperatureForLap;
        public Single PeakRearRightTemperatureForLap;

        public float FrontLeftPercentWear;
        public float FrontRightPercentWear;
        public float RearLeftPercentWear;
        public float RearRightPercentWear;

        public Single FrontLeftPressure;
        public Single FrontRightPressure;
        public Single RearLeftPressure;
        public Single RearRightPressure;

        private CornerData _TyreTempStatus;
        public CornerData TyreTempStatus
        {
            get
            {
                if (_TyreTempStatus == null)
                {
                    _TyreTempStatus = new CornerData();
                }
                return _TyreTempStatus;
            }
            set
            {
                _TyreTempStatus = value;
            }
        }

        private CornerData _TyreConditionStatus;
        public CornerData TyreConditionStatus
        {
            get
            {
                if (_TyreConditionStatus == null)
                {
                    _TyreConditionStatus = new CornerData();
                }
                return _TyreConditionStatus;
            }
            set
            {
                _TyreConditionStatus = value;
            }
        }

        private CornerData _BrakeTempStatus;
        public CornerData BrakeTempStatus
        {
            get
            {
                if (_BrakeTempStatus == null)
                {
                    _BrakeTempStatus = new CornerData();
                }
                return _BrakeTempStatus;
            }
            set
            {
                _BrakeTempStatus = value;
            }
        }

        public Single LeftFrontBrakeTemp;
        public Single RightFrontBrakeTemp;
        public Single LeftRearBrakeTemp;
        public Single RightRearBrakeTemp;

        public Boolean LeftFrontIsLocked;
        public Boolean RightFrontIsLocked;
        public Boolean LeftRearIsLocked;
        public Boolean RightRearIsLocked;
        public Boolean LeftFrontIsSpinning;
        public Boolean RightFrontIsSpinning;
        public Boolean LeftRearIsSpinning;
        public Boolean RightRearIsSpinning;
    }

    public class Conditions
    {
        private List<ConditionsSample> _samples;
        public List<ConditionsSample> samples
        {
            get
            {
                if (_samples == null)
                {
                    _samples = new List<ConditionsSample>();
                }
                return _samples;
            }
            set
            {
                _samples = value;
            }
        }
        public class ConditionsSample
        {
            public DateTime Time;
            public int LapCount;
            public int SectorNumber;
            // copied straight from PCars
            public float AmbientTemperature;
            public float TrackTemperature;
            public float RainDensity;
            public float WindSpeed;
            public float WindDirectionX;
            public float WindDirectionY;
            public float CloudBrightness;
            public Boolean atStartLine;

            public ConditionsSample(DateTime time, int lapCount, int sectorNumber, float AmbientTemperature, float TrackTemperature, float RainDensity,
                float WindSpeed, float WindDirectionX, float WindDirectionY, float CloudBrightness, Boolean atStartLine)
            {
                this.Time = time;
                this.LapCount = lapCount;
                this.SectorNumber = sectorNumber;
                this.AmbientTemperature = AmbientTemperature;
                this.TrackTemperature = TrackTemperature;
                this.RainDensity = RainDensity;
                this.WindSpeed = WindSpeed;
                this.WindDirectionX = WindDirectionX;
                this.WindDirectionY = WindDirectionY;
                this.CloudBrightness = CloudBrightness;
                this.atStartLine = atStartLine;
            }
        }

        public void addSample(DateTime time, int lapCount, int sectorNumber, float AmbientTemperature, float TrackTemperature, float RainDensity,
                float WindSpeed, float WindDirectionX, float WindDirectionY, float CloudBrightness, Boolean atStartLine)
        {
            samples.Add(new ConditionsSample(time, lapCount, sectorNumber, AmbientTemperature, TrackTemperature, RainDensity,
                WindSpeed, WindDirectionX, WindDirectionY, CloudBrightness, atStartLine));
        }

        public ConditionsSample getMostRecentConditions()
        {
            if (samples.Count == 0)
            {
                return null;
            }
            else
            {
                return samples[samples.Count - 1];
            }
        }

        public List<ConditionsSample> getStartLineConditions()
        {
            List<ConditionsSample> startLineSamples = new List<ConditionsSample>();
            foreach (ConditionsSample sample in samples)
            {
                if (sample.atStartLine)
                {
                    startLineSamples.Add(sample);
                }
            }
            return startLineSamples;
        }
    }

    public class OvertakingAids
    {
        public Boolean PushToPassAvailable;
        public Boolean PushToPassEngaged;
        public int PushToPassActivationsRemaining;
        public Single PushToPassEngagedTimeLeft;
        public Single PushToPassWaitTimeLeft;

        public Boolean DrsEnabled;
        public Boolean DrsAvailable;
        public Boolean DrsEngaged;
        public Single DrsRange;
    }

    public class DeltaTime
    {
        public Dictionary<float, DateTime> deltaPoints =  new Dictionary<float, DateTime>();
        public Dictionary<float, float> speedTrapPoints = new Dictionary<float, float>();
        // this array holds the keyset of the above dictionaries:
        private float[] deltaPointsKeysArray = new float[] {};

        public float currentDeltaPoint = -1;
        public float nextDeltaPoint = -1;
        public float distanceRoundTrackOnCurrentLap = -1;
        public float totalDistanceTravelled = -1;
        public int lapsCompleted = -1;
        public float trackLength = 0;
        public DeltaTime()
        {
            this.currentDeltaPoint = -1;
            this.nextDeltaPoint = -1;
            this.distanceRoundTrackOnCurrentLap = -1;
            this.totalDistanceTravelled = -1;
            this.lapsCompleted = -1;
            this.trackLength = 0;
        }
        public DeltaTime(float trackLength, float distanceRoundTrackOnCurrentLap, DateTime now, float spacing = 20f)
        {
            this.distanceRoundTrackOnCurrentLap = distanceRoundTrackOnCurrentLap;
            this.totalDistanceTravelled = distanceRoundTrackOnCurrentLap;
            this.trackLength = trackLength;
            float totalSpacing = 0;
            while (totalSpacing < trackLength)
            {
                //first one at s/f line
                if (totalSpacing == 0)
                {
                    deltaPoints.Add(totalSpacing, now);
                    speedTrapPoints.Add(totalSpacing, 0);
                }
                totalSpacing += spacing;
                Boolean addedDeltaPoint = false;
                if (totalSpacing < trackLength - spacing)
                {
                    deltaPoints.Add(totalSpacing, now);
                    speedTrapPoints.Add(totalSpacing, 0);
                    addedDeltaPoint = true;
                }
                if (distanceRoundTrackOnCurrentLap >= totalSpacing)
                {
                    if (addedDeltaPoint)
                    {
                        currentDeltaPoint = totalSpacing;
                    }
                }
            }
            // extract the keyset to a float array so we can iterate it much more efficiently - the keyset doesn't
            // change after the dictionary has been constructed
            deltaPointsKeysArray = deltaPoints.Keys.ToArray();
        }
        public void SetNextDeltaPoint(float distanceRoundTrackOnCurrentLap, int lapsCompleted, float speed, DateTime now)
        {
            this.distanceRoundTrackOnCurrentLap = distanceRoundTrackOnCurrentLap;
            this.lapsCompleted = lapsCompleted;
            this.totalDistanceTravelled = (lapsCompleted * this.trackLength) + distanceRoundTrackOnCurrentLap;

            float deltaPoint = 0;
            foreach (float key in deltaPointsKeysArray)
            {
                if (key >= distanceRoundTrackOnCurrentLap)
                {
                    deltaPoint = key;
                    break;
                }
            }
            this.nextDeltaPoint = deltaPoint;
            //

            if (currentDeltaPoint != nextDeltaPoint || speed < 5)
            {
                deltaPoints[nextDeltaPoint] = now;
                speedTrapPoints[nextDeltaPoint] = speed;
                currentDeltaPoint = nextDeltaPoint;
            }
        }

        // get the delta to otherCar in whole laps and seconds.
        public Tuple<int, float> GetSignedDeltaTimeWithLapDifference(DeltaTime otherCarDelta)
        {
            TimeSpan splitTime = new TimeSpan(0);
            int lapDifference = 0;
            if (otherCarDelta.deltaPoints.Count > 0 && deltaPoints.Count > 0 && currentDeltaPoint != -1 && otherCarDelta.currentDeltaPoint != -1)
            {
                lapDifference = GetSignedLapDifference(otherCarDelta);

                DateTime otherCarTime;
                DateTime thisCarTime;
                if (totalDistanceTravelled < otherCarDelta.totalDistanceTravelled)
                {
                    // I'm behind otherCar, so we want to know time between otherCar reaching the last deltaPoint I've just hit, and me reaching it.
                    // Because otherCar reached it further in the past than me, this will be negative
                    if (otherCarDelta.deltaPoints.TryGetValue(currentDeltaPoint, out otherCarTime)
                        && deltaPoints.TryGetValue(currentDeltaPoint, out thisCarTime))
                    {
                        splitTime = otherCarTime - thisCarTime;
                    }
                }
                else if (totalDistanceTravelled > otherCarDelta.totalDistanceTravelled)
                {
                    // I'm ahead of otherCar, so we want to know time between otherCar reaching the last deltaPoint he's just hit, and me reaching 
                    // that delta point.
                    // Because otherCar reached it more recently than me, this will be positive
                    if (otherCarDelta.deltaPoints.TryGetValue(otherCarDelta.currentDeltaPoint, out otherCarTime)
                        && deltaPoints.TryGetValue(otherCarDelta.currentDeltaPoint, out thisCarTime))
                    {
                        splitTime = otherCarTime - thisCarTime;
                    }
                }
            }
            return new Tuple<int, float>(lapDifference, (float) splitTime.TotalSeconds);
        }

        public int GetSignedLapDifference(DeltaTime otherCarDelta)
        {
            int lapDifference = 0;
            if (otherCarDelta.deltaPoints.Count > 0 && deltaPoints.Count > 0 && currentDeltaPoint != -1 && otherCarDelta.currentDeltaPoint != -1)
            {
                // +ve means I've travelled further than him:
                float totalDistanceTravelledDifference = totalDistanceTravelled - otherCarDelta.totalDistanceTravelled;
                // +ve means I've completed more laps:
                lapDifference = lapsCompleted - otherCarDelta.lapsCompleted;
                if (lapDifference > 0 && Math.Abs(totalDistanceTravelledDifference) < this.trackLength)
                {
                    // OK, I've completed more laps, but I'm one less complete lap ahead than the lapDifference suggests
                    lapDifference--;
                }
                else if (lapDifference < 0 && Math.Abs(totalDistanceTravelledDifference) < this.trackLength)
                {
                    // I've completed less laps, but I'm one less complete lap behind than the lapDifference suggests
                    lapDifference++;
                }
            }

            return lapDifference;
        }

        // get the time difference between this car and another car, allowing for partial laps completed differences
        public Tuple<int, float> GetAbsoluteTimeDeltaAllowingForLapDifferences(DeltaTime otherCarDelta)
        {
            var deltaTime = GetSignedDeltaTimeWithLapDifference(otherCarDelta);
            // Not sure lap delta needs to be absolute.
            return new Tuple<int, float>(Math.Abs(deltaTime.Item1), Math.Abs(deltaTime.Item2));
        }
        
        // return a signed delta based only on track position
        public float GetSignedDeltaTimeOnly(DeltaTime otherCarDelta)
        {
            TimeSpan splitTime = new TimeSpan(0);
            if (otherCarDelta.deltaPoints.Count > 0 && deltaPoints.Count > 0 && currentDeltaPoint != -1 && otherCarDelta.currentDeltaPoint != -1)
            {
                DateTime otherCarTime;
                DateTime thisCarTime;
                //opponent is behind
                if (distanceRoundTrackOnCurrentLap < otherCarDelta.distanceRoundTrackOnCurrentLap)
                {
                    if(otherCarDelta.deltaPoints.TryGetValue(currentDeltaPoint, out otherCarTime)
                        && deltaPoints.TryGetValue(currentDeltaPoint, out thisCarTime))
                    {
                        splitTime = otherCarTime - thisCarTime;
                    }
                }
                else if (distanceRoundTrackOnCurrentLap > otherCarDelta.distanceRoundTrackOnCurrentLap)
                {
                    if (otherCarDelta.deltaPoints.TryGetValue(otherCarDelta.currentDeltaPoint, out otherCarTime)
                        && deltaPoints.TryGetValue(otherCarDelta.currentDeltaPoint, out thisCarTime))
                    {
                        splitTime = otherCarTime - thisCarTime;
                    }
                }
                else
                {
                    return 0f;
                }
            }
            return (float)splitTime.TotalSeconds;
        }
    }

    public class HardPartsOnTrackData
    {
        public List<Tuple<float, float>> rawHardPartsForThisLap = new List<Tuple<float, float>>();
        public List<Tuple<float, float>> processedHardPartsForBestLap = new List<Tuple<float, float>>();
        public Boolean isAlreadyBraking = false;        
        public Boolean hardPartsMapped = false;
        public Boolean gapsAdjusted = false;
        public float hardPartStart = -1;
        private float lapTimeForHardPartsData = -1;
        private Boolean currentLapValid = true;
        private float trackLength;

        private float trackLengthForLastMinSectionLengthCheck = 0;
        private float sectionStartBuffer = 150;
        private float sectionEndBuffer = 25;
        private float minSectionLength = 175;
        private float minDistanceBetweenSections = 150;
        private float startLineStartBuffer = 10;
        private float startLineEndBuffer = 30;

        // called when we complete a lap. If it's our best lap we use this data
        public Boolean updateHardPartsForNewLap(float lapTime)
        {
            Boolean useNewData = false;
            // started a new lap, previous was valid and we have data so see if we want to use the data
            if (currentLapValid && lapTime > 0 && (lapTimeForHardPartsData == -1 || lapTimeForHardPartsData > lapTime) && rawHardPartsForThisLap.Count > 0)
            {
                lapTimeForHardPartsData = lapTime;
                float totalDistanceCoveredByHardPoints = 0;
                foreach (Tuple<float, float> hardPart in rawHardPartsForThisLap)
                {
                    if (CrewChief.Debugging)
                    {
                        Console.WriteLine("raw lap Hard parts. Starts at: " + hardPart.Item1.ToString("0.000") + "    Ends at: " + hardPart.Item2.ToString("0.000"));
                    }                    
                    totalDistanceCoveredByHardPoints += (hardPart.Item2 - hardPart.Item1);
                }
                if (CrewChief.Debugging)
                {
                    Console.WriteLine("Proportion of track considered hard (raw data) = " + totalDistanceCoveredByHardPoints/trackLength);
                }
                updateSectionParameters(totalDistanceCoveredByHardPoints);
                processedHardPartsForBestLap = adjustAndCombineHardParts(rawHardPartsForThisLap);
                float totalProcessed = 0;
                if (CrewChief.Debugging)
                {
                    foreach (Tuple<float, float> hardPart in processedHardPartsForBestLap)
                    {
                        Console.WriteLine("Processed lap Hard parts. Starts at: " + hardPart.Item1.ToString("0.000") + "    Ends at: " + hardPart.Item2.ToString("0.000"));
                        totalProcessed += (hardPart.Item2 - hardPart.Item1);
                    }
                    Console.WriteLine("Proportion of track considered hard (processed data) = " + totalProcessed/trackLength);
                }
                hardPartsMapped = true;
                useNewData = true;
            }
            currentLapValid = true;
            rawHardPartsForThisLap = new List<Tuple<float, float>>();
            return useNewData;
        }

        // called on every tick
        public void mapHardPartsOnTrack(float brakePedal, float loudPedal, float distanceRoundTrack, Boolean lapIsValid, float trackLength)
        {
            this.trackLength = trackLength;
            if (!lapIsValid || !currentLapValid)
            {
                currentLapValid = false;
                isAlreadyBraking = false;
                return;
            }
            if (!isAlreadyBraking && brakePedal > 0.1)
            {
                isAlreadyBraking = true;
                hardPartStart = distanceRoundTrack;
            }
            setMinSectionLength();
            if (loudPedal > 0.9 && isAlreadyBraking && distanceRoundTrack > hardPartStart + minSectionLength)
            {
                float endPoint = distanceRoundTrack;
                if (hardPartStart < endPoint)
                {
                    // don't allow sections which cross the start line
                    rawHardPartsForThisLap.Add(new Tuple<float, float>(hardPartStart, endPoint));
                    // Console.WriteLine("Hard part on track mapped.  Starts at: " + hardPartStart.ToString("0.000") + "    Ends at: " +  (distanceRoundTrack + 25).ToString("0.000"));
                }
                isAlreadyBraking = false;
            }
        }

        private void setMinSectionLength()
        {
            if (trackLength != trackLengthForLastMinSectionLengthCheck)
            {
                trackLengthForLastMinSectionLengthCheck = trackLength;
                if (trackLength < 1000)
                {
                    minSectionLength = 50;
                }
                else if (trackLength < 2000)
                {
                    minSectionLength = 100;
                }
                else if (trackLength < 3000)
                {
                    minSectionLength = 130;
                }
                else
                {
                    minSectionLength = 175;
                }
            }
        }
        // using the proportion of track length spent in hard-parts (the raw unprocessed data), adjust
        // the parameters we're going to use to adjust and combine these raw hard parts sections
        private void updateSectionParameters(float totalDistanceCoveredByHardPoints)
        {
            float proportionOfTrack = totalDistanceCoveredByHardPoints / trackLength;
             if (proportionOfTrack < 0.2)
            {
                // few hard parts, use generous params
                sectionStartBuffer = 150;
                sectionEndBuffer = 25;
                minDistanceBetweenSections = 150;
                startLineStartBuffer = 10;
                startLineEndBuffer = 20;
            }
            else if (proportionOfTrack < 0.3)
            {
                sectionStartBuffer = 110;
                sectionEndBuffer = 15;
                minDistanceBetweenSections = 120;
                startLineStartBuffer = 10;
                startLineEndBuffer = 30;
            }
            else if (proportionOfTrack < 0.4)
            {
                sectionStartBuffer = 90;
                sectionEndBuffer = 10;
                minDistanceBetweenSections = 100;
                startLineStartBuffer = 10;
                startLineEndBuffer = 40;
            }
            else if (proportionOfTrack < 0.5)
            {
                sectionStartBuffer = 70;
                sectionEndBuffer = 0;
                minDistanceBetweenSections = 70;
                startLineStartBuffer = 10;
                startLineEndBuffer = 50;
            }
            else
            {
                // most of the track is 'hard', so extend the hard parts as little as we can
                sectionStartBuffer = 50;
                sectionEndBuffer = -10; // is this safe?
                minDistanceBetweenSections = 50;
                startLineStartBuffer = 10;
                startLineEndBuffer = 60;
            }
        }

        private List<Tuple<float, float>> adjustAndCombineHardParts(List<Tuple<float, float>> hardParts)
        {
            List<Tuple<float, float>> adjustedHardParts = new List<Tuple<float, float>>();
            // don't allow a hard part to end within startLineStartBuffer metres of the line:
            float maxAllowedEndPoint = trackLength - startLineStartBuffer;
            // the last end point we checked in the nested loop
            float lastEndPoint = 0;

            for (int index = 0; index < hardParts.Count; index++)
            {
                Tuple<float, float> thisPart = hardParts[index];
                // the adjusted start point of this part, using the start buffer and ensuring it's not too close to the line:
                float thisStart = Math.Max(startLineEndBuffer, thisPart.Item1 - sectionStartBuffer);
                if (thisStart < lastEndPoint)
                {
                    // after adjusting this start point, it's before the last end point so don't use it
                    continue;
                }
                // the end point of this hard part, adjusted
                float thisEnd = Math.Min(maxAllowedEndPoint, thisPart.Item2 + sectionEndBuffer);

                // now see if any of the other adjusted data points overlap
                for (int remainingIndex = index + 1; remainingIndex < hardParts.Count; remainingIndex++)
                {
                    float nextStart = Math.Max(startLineEndBuffer, hardParts[remainingIndex].Item1 - sectionStartBuffer);
                    float nextEnd = Math.Min(maxAllowedEndPoint, hardParts[remainingIndex].Item2 + sectionEndBuffer);
                    if (nextStart > nextEnd)
                    {
                        // after adjusting this start and end points, its start is after its end so don't use it
                        // increment the outer loop counter as we're not interested in this pair's start point
                        index++;
                    }
                    else if (nextStart < thisEnd + minDistanceBetweenSections)
                    {
                        // this start point overlaps, or is close enough to be considered overlapping, so we use its end point
                        thisEnd = nextEnd;
                        // increment the outer loop counter as we're not interested in this pair's start point
                        index++;
                    }
                    else
                    {
                        // the next start point doesn't overlap so we use whatever end point we hard on the previous iteration
                        break;
                    }
                }
                // if we have a valid pair, add them
                if (thisStart < thisEnd)
                {
                    adjustedHardParts.Add(new Tuple<float, float>(thisStart, thisEnd));
                    lastEndPoint = thisEnd;
                }
            }
            return adjustedHardParts;
        }

        public Boolean isInHardPart(float distanceRoundTrack)
        {
            if (AudioPlayer.delayMessagesInHardParts && hardPartsMapped)
            {
                foreach (Tuple<float, float> part in processedHardPartsForBestLap)
                {
                    if (distanceRoundTrack >= part.Item1 && distanceRoundTrack <= part.Item2)
                    {
                        return true;
                    }
                }

            }
            return false;
        }
    }

    public class GameStateData
    {
        // first some static crap to ensure the code is sufficiently badly factored

        // public because who the fuck knows what'll set and unset these...
        public static Boolean useManualFormationLap;
        public static Boolean onManualFormationLap;

        // This is updated on every tick so should always be accurate. NOTE THIS IS NOT SET FOR IRACING!
        public static int NumberOfClasses = 1;
        public static Boolean Multiclass;

        public static DateTime CurrentTime = DateTime.UtcNow;

        public Boolean sortClassPositionsCompleted;

        public long Ticks;

        public DateTime Now;
        // lazily initialised only when we're using trace playback:
        public String CurrentTimeStr;

        private CarData.CarClass _carClass;
        public CarData.CarClass carClass
        {
            get
            {
                if (_carClass == null)
                {
                    _carClass = new CarData.CarClass();
                }
                return _carClass;
            }
            set
            {
                _carClass = value;
            }
        }

        private EngineData _EngineData;
        public EngineData EngineData
        {
            get
            {
                if (_EngineData == null)
                {
                    _EngineData = new EngineData();
                }
                return _EngineData;
            }
            set
            {
                _EngineData = value;
            }
        }

        private TransmissionData _TransmissionData;
        public TransmissionData TransmissionData
        {
            get
            {
                if (_TransmissionData == null)
                {
                    _TransmissionData = new TransmissionData();
                }
                return _TransmissionData;
            }
            set
            {
                _TransmissionData = value;
            }
        }

        private FuelData _FuelData;
        public FuelData FuelData
        {
            get
            {
                if (_FuelData == null)
                {
                    _FuelData  = new FuelData();
                }
                return _FuelData;
            }
            set
            {
                _FuelData = value;
            }
        }

        private BatteryData _BatteryData;
        public BatteryData BatteryData
        {
            get
            {
                if (_BatteryData == null)
                {
                    _BatteryData = new BatteryData();
                }
                return _BatteryData;
            }
            set
            {
                _BatteryData = value;
            }
        }

        private CarDamageData _CarDamageData;
        public CarDamageData CarDamageData
        {
            get
            {
                if (_CarDamageData == null)
                {
                    _CarDamageData  = new CarDamageData();
                }
                return _CarDamageData;
            }
            set
            {
                _CarDamageData = value;
            }
        }

        private ControlData _ControlData;
        public ControlData ControlData
        {
            get
            {
                if (_ControlData == null)
                {
                    _ControlData = new ControlData();
                }
                return _ControlData;
            }
            set
            {
                _ControlData = value;
            }
        }

        private SessionData _SessionData;
        public SessionData SessionData
        {
            get
            {
                if (_SessionData == null)
                {
                    _SessionData = new SessionData();
                }
                return _SessionData;
            }
            set
            {
                _SessionData = value;
            }
        }

        private PitData _PitData;
        public PitData PitData
        {
            get
            {
                if (_PitData == null)
                {
                    _PitData = new PitData();
                }
                return _PitData;
            }
            set
            {
                _PitData = value;
            }
        }

        private PenatiesData _PenaltiesData;
        public PenatiesData PenaltiesData
        {
            get
            {
                if (_PenaltiesData == null)
                {
                    _PenaltiesData = new PenatiesData();
                }
                return _PenaltiesData;
            }
            set
            {
                _PenaltiesData = value;
            }
        }

        private TyreData _TyreData;
        public TyreData TyreData
        {
            get
            {
                if (_TyreData == null)
                {
                    _TyreData = new TyreData();
                }
                return _TyreData;
            }
            set
            {
                _TyreData = value;
            }
        }

        private PositionAndMotionData _PositionAndMotionData;
        public PositionAndMotionData PositionAndMotionData
        {
            get
            {
                if (_PositionAndMotionData == null)
                {
                    _PositionAndMotionData = new PositionAndMotionData();
                }
                return _PositionAndMotionData;
            }
            set
            {
                _PositionAndMotionData = value;
            }
        }

        private Dictionary<string, OpponentData> _OpponentData;
        public Dictionary<string, OpponentData> OpponentData
        {
            get
            {
                if (_OpponentData == null)
                {
                    _OpponentData = new Dictionary<string, OpponentData>();
                }
                return _OpponentData;
            }
            set
            {
                _OpponentData = value;
            }
        }

        private Conditions _Conditions;
        public Conditions Conditions
        {
            get
            {
                if (_Conditions == null)
                {
                    _Conditions = new Conditions();
                }
                return _Conditions;
            }
            set
            {
                _Conditions = value;
            }
        }

        private TimingData _TimingData;
        public TimingData TimingData
        {
            get
            {
                if (_TimingData == null)
                {
                    _TimingData = new TimingData();
                }
                return _TimingData;
            }
            set
            {
                _TimingData = value;
            }
        }

        private OvertakingAids _OvertakingAids;
        public OvertakingAids OvertakingAids
        {
            get
            {
                if (_OvertakingAids == null)
                {
                    _OvertakingAids = new OvertakingAids();
                }
                return _OvertakingAids;
            }
            set
            {
                _OvertakingAids = value;
            }
        }

        private FlagData _FlagData;
        public FlagData FlagData
        {
            get
            {
                if (_FlagData == null)
                {
                    _FlagData = new FlagData();
                }
                return _FlagData;
            }
            set
            {
                _FlagData = value;
            }
        }

        private StockCarRulesData _StockCarRulesData;
        public StockCarRulesData StockCarRulesData
        {
            get
            {
                if (_StockCarRulesData == null)
                {
                    _StockCarRulesData = new StockCarRulesData();
                }
                return _StockCarRulesData;
            }
            set
            {
                _StockCarRulesData = value;
            }
        }

        private FrozenOrderData _FrozenOrderData;
        public FrozenOrderData FrozenOrderData
        {
            get
            {
                if (_FrozenOrderData == null)
                {
                    _FrozenOrderData = new FrozenOrderData();
                }
                return _FrozenOrderData;
            }
            set
            {
                _FrozenOrderData = value;
            }
        }

        private HashSet<String> _retriedDriverNames;
        public HashSet<String> retriedDriverNames
        {
            get
            {
                if (_retriedDriverNames == null)
                {
                    _retriedDriverNames = new HashSet<String>();
                }
                return _retriedDriverNames;
            }
            set
            {
                _retriedDriverNames = value;
            }
        }

        private HashSet<String> _disqualifiedDriverNames;
        public HashSet<String> disqualifiedDriverNames
        {
            get
            {
                if (_disqualifiedDriverNames == null)
                {
                    _disqualifiedDriverNames = new HashSet<String>();
                }
                return _disqualifiedDriverNames;
            }
            set
            {
                _disqualifiedDriverNames = value;
            }
        }
        
        private static TimeSpan MaxWaitForNewLapData = TimeSpan.FromSeconds(3);

        private DateTime NewLapDataTimerExpiry = DateTime.MaxValue;

        private Boolean WaitingForNewLapData;

        private HardPartsOnTrackData _hardPartsOnTrackData;
        public HardPartsOnTrackData hardPartsOnTrackData
        {
            get
            {
                if (_hardPartsOnTrackData == null)
                {
                    _hardPartsOnTrackData = new HardPartsOnTrackData();
                }
                return _hardPartsOnTrackData;
            }
            set
            {
                _hardPartsOnTrackData = value;
            }
        }
                
        // special case for pcars2 CloudBrightness and rain because we want to track this in real-time
        public float CloudBrightness = -1;
        public float RainDensity = -1;

        public Boolean readLandmarksForThisLap;
        public float GameTimeWhenLastCrossedStartFinishLine = -1;
        public int CompletedLapsWhenHasNewLapDataWasLastTrue = -2;
        //call this after setting currentGameState.SessionData.SectorNumber and currentGameState.SessionData.IsNewSector
        public bool HasNewLapData(GameStateData previousGameState, float gameProvidedLastLapTime, bool hasCrossedSFLine)
        {
            if (previousGameState != null)
            {
                if ((hasCrossedSFLine && CompletedLapsWhenHasNewLapDataWasLastTrue < this.SessionData.CompletedLaps) || 
                    (this.SessionData.SessionType == SessionType.Race && hasCrossedSFLine))
                {
                    // reset the timer and start waiting for an updated laptime...
                    this.WaitingForNewLapData = true;
                    this.NewLapDataTimerExpiry = this.Now.Add(GameStateData.MaxWaitForNewLapData);
                    this.GameTimeWhenLastCrossedStartFinishLine = this.SessionData.SessionRunningTime;
                }
                else
                {
                    // not a new lap but may be waiting, so copy over the wait variables
                    this.WaitingForNewLapData = previousGameState.WaitingForNewLapData;
                    this.NewLapDataTimerExpiry = previousGameState.NewLapDataTimerExpiry;
                    this.GameTimeWhenLastCrossedStartFinishLine = previousGameState.GameTimeWhenLastCrossedStartFinishLine;
                }
                // if we're waiting, see if the timer has expired or we have a change in the previous laptime value
                if (this.WaitingForNewLapData && 
                    (previousGameState.SessionData.LapTimePrevious != gameProvidedLastLapTime || this.Now > this.NewLapDataTimerExpiry))
                {
                    // the timer has expired or we have new data
                    this.WaitingForNewLapData = false;
                    this.SessionData.LapTimePrevious = gameProvidedLastLapTime;
                    this.SessionData.PreviousLapWasValid = gameProvidedLastLapTime > 1;
                    this.CompletedLapsWhenHasNewLapDataWasLastTrue = this.SessionData.CompletedLaps;
                    return true;
                }
                else
                {
                    this.SessionData.LapTimePrevious = previousGameState.SessionData.LapTimePrevious;
                    this.SessionData.PreviousLapWasValid = previousGameState.SessionData.PreviousLapWasValid;
                    this.CompletedLapsWhenHasNewLapDataWasLastTrue = previousGameState.CompletedLapsWhenHasNewLapDataWasLastTrue;
                    
                }
            }
            return false;
        }
        
        public GameStateData(long ticks)
        {
            this.Ticks = ticks;
            this.Now = new DateTime(ticks);
            CurrentTime = Now;
        }

        // some convenience methods
        public Boolean isLast()
        {
            if (!GameStateData.Multiclass)
            {
                return SessionData.OverallPosition == SessionData.NumCarsOverall;
            }
            else
            {
                return SessionData.ClassPosition == SessionData.NumCarsInPlayerClass;
            }
        }

        public List<String> getRawDriverNames()
        {
            List<String> rawDriverNames = new List<String>();
            foreach (KeyValuePair<string, OpponentData> entry in OpponentData)
            {
                if (!rawDriverNames.Contains(entry.Value.DriverRawName))
                {
                    rawDriverNames.Add(entry.Value.DriverRawName);
                }
            }
            rawDriverNames.Sort();
            return rawDriverNames;
        }

        public OpponentData getOpponentAtClassPosition(int position, CarData.CarClass carClass)
        {
            return getOpponentAtClassPosition(position, carClass, false);
        }

        public OpponentData getOpponentAtClassPosition(int position, CarData.CarClass carClass, Boolean previousTick)
        {
            string opponentKey = getOpponentKeyAtClassPosition(position, carClass, previousTick);
            OpponentData opponent = null;
            if (opponentKey != null && OpponentData.TryGetValue(opponentKey, out opponent))
            {
                return opponent;
            }

            return null;
        }

        public OpponentData getOpponentAtOverallPosition(int position)
        {
            return getOpponentAtOverallPosition(position, false);
        }

        public OpponentData getOpponentAtOverallPosition(int position, Boolean previousTick)
        {
            string opponentKey = getOpponentKeyAtOverallPosition(position, previousTick);
            OpponentData opponent = null;
            if (opponentKey != null && OpponentData.TryGetValue(opponentKey, out opponent))
            {
                return opponent;
            }

            return null;
        }

        public string getOpponentKeyInFrontOnTrack()
        {
            string opponentKeyClosestInFront = null;
            string opponentKeyFurthestBehind = null;
            float closestDistanceFront = SessionData.TrackDefinition.trackLength;
            float furthestDistanceBehind = 0.0f;
            foreach (var opponent in OpponentData)
            {
                if (opponent.Value.Speed > 0.5 && !opponent.Value.isEnteringPits())
                {
                    if (opponent.Value.DistanceRoundTrack > PositionAndMotionData.DistanceRoundTrack &&
                        opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack < closestDistanceFront)
                    {
                        closestDistanceFront = opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack;
                        opponentKeyClosestInFront = opponent.Key;
                    }
                    else if (opponent.Value.DistanceRoundTrack < PositionAndMotionData.DistanceRoundTrack &&
                        PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack > furthestDistanceBehind)
                    {
                        furthestDistanceBehind = PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack;
                        opponentKeyFurthestBehind = opponent.Key;
                    }
                }
            }
            if (opponentKeyClosestInFront != null)
                return opponentKeyClosestInFront;
            else
                return opponentKeyFurthestBehind;
        }

        public string getOpponentKeyBehindOnTrack()
        {
            return getOpponentKeyBehindOnTrack(false);
        }


        public string getOpponentKeyBehindOnTrack(Boolean onlyIncludeCarsLappingThePlayer)
        {
            string opponentKeyClosestBehind = null;
            string opponentKeyFurthestInFront = null;
            float closestDistanceBehind = SessionData.TrackDefinition.trackLength;
            float furthestDistanceInFront = 0.0f;
            foreach (var opponent in OpponentData)
            {
                if (onlyIncludeCarsLappingThePlayer && opponent.Value.ClassPosition > SessionData.ClassPosition)
                {
                    // we're ahead of this car in the race and are only interested in cars lapping us, so ignore him
                    continue;
                }
                if (opponent.Value.Speed > 0.5 && !opponent.Value.isEnteringPits())
                {
                    if (PositionAndMotionData.DistanceRoundTrack > opponent.Value.DistanceRoundTrack &&
                        PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack < closestDistanceBehind)
                    {
                        closestDistanceBehind = PositionAndMotionData.DistanceRoundTrack - opponent.Value.DistanceRoundTrack;
                        opponentKeyClosestBehind = opponent.Key;
                    }
                    else if (PositionAndMotionData.DistanceRoundTrack < opponent.Value.DistanceRoundTrack &&
                        opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack > furthestDistanceInFront)
                    {
                        furthestDistanceInFront = opponent.Value.DistanceRoundTrack - PositionAndMotionData.DistanceRoundTrack;
                        opponentKeyFurthestInFront = opponent.Key;
                    }
                }
            }
            if (opponentKeyClosestBehind != null)
                return opponentKeyClosestBehind;
            else
                return opponentKeyFurthestInFront;
        }

        public string getOpponentKeyInFront(CarData.CarClass carClass, Boolean previousTick)
        {
            if (SessionData.ClassPosition > 1)
            {
                return getOpponentKeyAtClassPosition(SessionData.ClassPosition - 1, carClass, previousTick);
            }
            else
            {
                return null;
            }
        }

        public string getOpponentKeyInFront(CarData.CarClass carClass)
        {
            return getOpponentKeyInFront(carClass, false);
        }

        public string getOpponentKeyBehind(CarData.CarClass carClass, Boolean previousTick)
        {
            if (SessionData.ClassPosition < SessionData.NumCarsInPlayerClass)
            {
                return getOpponentKeyAtClassPosition(SessionData.ClassPosition + 1, carClass, previousTick);
            }
            else
            {
                return null;
            }
        }

        public string getOpponentKeyBehind(CarData.CarClass carClass)
        {
            return getOpponentKeyBehind(carClass, false);
        }

        public string getOpponentKeyAtClassPosition(int position, CarData.CarClass carClass)
        {
            return getOpponentKeyAtClassPosition(position, carClass, false);
        }

        public string getOpponentKeyAtClassPosition(int position, CarData.CarClass carClass, Boolean previousTick)
        {
            if (OpponentData.Count != 0)
            {
                String opponentWithSamePositionAsPlayer = null;
                int opponentsWithSamePositionAsPlayer = 0;
                foreach (KeyValuePair<string, OpponentData> entry in OpponentData)
                {
                    int opponentPosition = previousTick ? entry.Value.ClassPositionAtPreviousTick : entry.Value.ClassPosition;
                    if (CarData.IsCarClassEqual(entry.Value.CarClass, carClass))
                    {
                        if (opponentPosition == position)
                        {
                            return entry.Key;
                        }
                        else if (!previousTick && opponentPosition == this.SessionData.ClassPosition &&
                            this.SessionData.SessionType == SessionType.Race && this.SessionData.SessionRunningTime > 30)
                        {
                            // there's an opponent with the same position as the player - usually caused by delays in updating
                            // opponent position data when the player passes an opponent car. Once the race start data have settled
                            // down, this opponent key might be the one we want. Note that we can't use this hack when inspecting 
                            // opponents' previous positions (previousTick == false) because the player position will be a tick newer
                            if (opponentsWithSamePositionAsPlayer == 0)
                            {
                                opponentWithSamePositionAsPlayer = entry.Key;
                            }
                            opponentsWithSamePositionAsPlayer++;
                        }
                    }
                }
                // if we reach this point there's no opponent car in that position, so we might want to return an opponent who has the same
                // position as the player
                if ((position == this.SessionData.ClassPosition + 1 || position == this.SessionData.ClassPosition - 1) && opponentsWithSamePositionAsPlayer == 1)
                {
                    // we've asked for a position that's +/-1 from the player's position, there is one opponent with the same position as player, so return him
                    return opponentWithSamePositionAsPlayer;
                }
            }
            return null;
        }

        public string getOpponentKeyAtOverallPosition(int position, Boolean previousTick)
        {
            if (OpponentData.Count != 0)
            {
                String opponentWithSamePositionAsPlayer = null;
                int opponentsWithSamePositionAsPlayer = 0;
                foreach (KeyValuePair<string, OpponentData> entry in OpponentData)
                {
                    int opponentPosition = previousTick ? entry.Value.OverallPositionAtPreviousTick : entry.Value.OverallPosition;
                    if (opponentPosition == position)
                    {
                        return entry.Key;
                    }
                    else if (!previousTick && opponentPosition == this.SessionData.OverallPosition && 
                        this.SessionData.SessionType == SessionType.Race && this.SessionData.SessionRunningTime > 30)
                    {
                        // there's an opponent with the same position as the player - usually caused by delays in updating
                        // opponent position data when the player passes an opponent car. Once the race start data have settled
                        // down, this opponent key might be the one we want. Note that we can't use this hack when inspecting 
                        // opponents' previous positions (previousTick == false) because the player position will be a tick newer
                        if (opponentsWithSamePositionAsPlayer == 0)
                        {
                            opponentWithSamePositionAsPlayer = entry.Key;
                        }
                        opponentsWithSamePositionAsPlayer++;
                    }
                }
                // if we reach this point there's no opponent car in that position, so we might want to return an opponent who has the same
                // position as the player
                if ((position == this.SessionData.OverallPosition + 1 || position == this.SessionData.OverallPosition - 1) && opponentsWithSamePositionAsPlayer == 1)
                {
                    // we've asked for a position that's +/-1 from the player's position, there is one opponent with the same position as player, so return him
                    return opponentWithSamePositionAsPlayer;
                }
            }
            return null;
        }

        public void sortClassPositions()
        {
            if (CrewChief.forceSingleClass)
            {
                this.SessionData.ClassPosition = this.SessionData.OverallPosition;
                foreach (OpponentData opponentData in OpponentData.Values)
                {
                    opponentData.ClassPosition = opponentData.OverallPosition;
                }
                GameStateData.NumberOfClasses = 1;
            }
            else
            {
                // if we group all classes together, set everyone's ClassPosition to their Position. We still count the number of classes here.
                // If the number of classes at the previous check was 1, don't do the full sorting. This will allow single class sessions to skip
                // the expensive sort call. In multiclass sessions we'll still update NumberOfClasses to be correct here, then on the next tick
                // the class positions will be sorted properly. So we'll be behind for 1 tick in practice / qual if a new class car joins. For races
                // cars tend to only leave, so this will probably be OK

                HashSet<string> unknownClassIds = new HashSet<string>();
                int numberOfClasses;
                if (GameStateData.NumberOfClasses == 1)
                {
                    HashSet<String> classIds = new HashSet<string>();
                    String playerClassId = this.carClass.getClassIdentifier();
                    classIds.Add(playerClassId);
                    if (CrewChief.gameDefinition.allowsUserCreatedCars && this.carClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                    {
                        unknownClassIds.Add(playerClassId);
                    }
                    this.SessionData.ClassPosition = this.SessionData.OverallPosition;
                    foreach (OpponentData opponentData in OpponentData.Values)
                    {
                        opponentData.ClassPosition = opponentData.OverallPosition;
                        String opponentClassId = opponentData.CarClass.getClassIdentifier();
                        classIds.Add(opponentClassId);
                        if (CrewChief.gameDefinition.allowsUserCreatedCars && opponentData.CarClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                        {
                            unknownClassIds.Add(playerClassId);
                        }
                    }
                    numberOfClasses = classIds.Count;
                }
                else
                {
                    List<OpponentData> participants = this.OpponentData.Values.ToList();
                    OpponentData player = new OpponentData() { OverallPosition = this.SessionData.OverallPosition, CarClass = this.carClass };
                    participants.Add(player);

                    // can't sort this list on construction because it contains a dummy entry for the player, so sort it here:
                    participants.Sort(delegate(OpponentData d1, OpponentData d2)
                    {
                        return d1.OverallPosition.CompareTo(d2.OverallPosition);
                    });

                    Dictionary<string, int> classCounts = new Dictionary<string, int>();
                    foreach (OpponentData participant in participants)
                    {
                        String classId = participant.CarClass.getClassIdentifier();
                        if (CrewChief.gameDefinition.allowsUserCreatedCars && participant.CarClass.carClassEnum == CarData.CarClassEnum.UNKNOWN_RACE)
                        {
                            unknownClassIds.Add(classId);
                        }
                        // because the source list is sorted by position, the number of cars we've encountered so far for this participant's
                        // class will be his class position. If this is the first time we've seen this class, he must be leading it:
                        int countForThisClass;
                        if (classCounts.TryGetValue(classId, out countForThisClass))
                        {
                            countForThisClass++;
                            classCounts[classId] = countForThisClass;
                        }
                        else
                        {
                            countForThisClass = 1;
                            classCounts[classId] = 1;
                        }

                        participant.ClassPosition = countForThisClass;
                        // if this is the dummy participant for the player, update the player ClassPosition
                        if (this.SessionData.OverallPosition == participant.OverallPosition)
                        {
                            this.SessionData.ClassPosition = countForThisClass;
                        }
                    }
                    numberOfClasses = classCounts.Count;
                }
                if (hasTooManyUnknownClasses(numberOfClasses, unknownClassIds))
                {
                    GameStateData.NumberOfClasses = 1;
                }
                else
                {
                    GameStateData.NumberOfClasses = numberOfClasses;
                }
                GameStateData.Multiclass = GameStateData.NumberOfClasses > 1;
            }
            sortClassPositionsCompleted = true;
        }

        public void setPracOrQualiDeltas()
        {
            if (this.SessionData.SessionType != SessionType.Race)
            {
                //  Allow gaps in qual and prac, delta here is not on track delta but diff on fastest time.  Race gaps are set in populateDerivedRaceSessionData.
                foreach (var opponent in this.OpponentData.Values)
                {
                    if (opponent.ClassPosition == this.SessionData.ClassPosition + 1)
                    {
                        this.SessionData.TimeDeltaBehind = Math.Abs(opponent.CurrentBestLapTime - this.SessionData.PlayerLapTimeSessionBest);
                        this.SessionData.LapsDeltaBehind = 0;
                    }

                    if (opponent.ClassPosition == this.SessionData.ClassPosition - 1)
                    {
                        this.SessionData.TimeDeltaFront = Math.Abs(this.SessionData.PlayerLapTimeSessionBest - opponent.CurrentBestLapTime);
                        this.SessionData.LapsDeltaFront = 0;
                    }
                }
            }

        }

        private Boolean hasTooManyUnknownClasses(int totalNumberOfClassesIds, HashSet<String> unknownClassIds)
        {
            if (CrewChief.gameDefinition.allowsUserCreatedCars)
            {
                int numberOfUnknownClassIds = unknownClassIds.Count;
                if (numberOfUnknownClassIds == 0)
                {
                    return false;
                }
                // For games that allow user-created cars but that still have sensible 'car class' data,
                // if the number of unknown class IDs exceeds the number of known class IDs, disable multiclass.
                // Assetto has no car class concept, only car model. So we need to quite strict here
                if (CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || CrewChief.gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT)
                {
                    return numberOfUnknownClassIds > CrewChief.maxUnknownClassesForAC;
                }
                else
                {
                    int numberOfKnownClassIds = totalNumberOfClassesIds - numberOfUnknownClassIds;
                    return numberOfUnknownClassIds > numberOfKnownClassIds;
                }
            }
            return false;
        }

        public void display()
        {
            Console.WriteLine("Laps completed = " + SessionData.CompletedLaps);
            Console.WriteLine("Time elapsed = " + SessionData.SessionRunningTime.ToString("0.000"));
            Console.WriteLine("Overall Position = " + SessionData.OverallPosition);
            Console.WriteLine("Class Position = " + SessionData.ClassPosition);
            Console.WriteLine("Session phase = " + SessionData.SessionPhase);
        }

        public void displayOpponentData()
        {
            Console.WriteLine("Got " + OpponentData.Count + " opponents");
            foreach (KeyValuePair<string, OpponentData> opponent in OpponentData)
            {
                Console.WriteLine("Last laptime " + opponent.Value.getLastLapTime() + " completed laps " + opponent.Value.CompletedLaps +
                    " ID " + opponent.Key + " name " + opponent.Value.DriverRawName + " active " + opponent.Value.IsActive +
                    " approx speed " + opponent.Value.Speed + " class position " + opponent.Value.ClassPosition +
                    " overall position " + opponent.Value.OverallPosition);
            }
        }

        public float[] getTimeAndSectorsForBestOpponentLapInWindow(int lapsToCheck, CarData.CarClass carClassToCheck)
        {
            float[] bestLapWithSectors = new float[] { -1, -1, -1, -1 };

            foreach (KeyValuePair<string, OpponentData> entry in OpponentData)
            {
                if (CrewChief.forceSingleClass
                    || CarData.IsCarClassEqual(entry.Value.CarClass, carClassToCheck))
                {
                    float[] thisOpponentsBest = entry.Value.getTimeAndSectorsForBestLapInWindow(lapsToCheck);
                    if (bestLapWithSectors[0] == -1 || (thisOpponentsBest[0] > 0 && thisOpponentsBest[0] < bestLapWithSectors[0]))
                    {
                        bestLapWithSectors = thisOpponentsBest;
                    }
                }
            }

            // special case for practice and qual - if we're looking for all the laps in the session, we might want to use the data sent by the game
            // because the play may have joined mid-session. In these cases there might be an historical lap (before the player joined) that's actually faster.
            if (lapsToCheck == -1 && SessionData.SessionFastestLapTimeFromGamePlayerClass > 0 &&
                (SessionData.PlayerLapTimeSessionBest == -1 || SessionData.PlayerLapTimeSessionBest > SessionData.SessionFastestLapTimeFromGamePlayerClass))
            {
                // the player isn't the fastest in his class. This means that the game-sent best lap data will be an opponent lap
                if (bestLapWithSectors[0] == -1 || bestLapWithSectors[0] > SessionData.SessionFastestLapTimeFromGamePlayerClass)
                {
                    // there's an historical lap which is quicker than all the data we currently hold. Due to limitations in the shared memory blocks,
                    // we never have sector times for this historical lap, so we have to remove them and disable sector deltas until a better lap is recorded
                    bestLapWithSectors[0] = SessionData.SessionFastestLapTimeFromGamePlayerClass;
                    bestLapWithSectors[1] = -1;
                    bestLapWithSectors[2] = -1;
                    bestLapWithSectors[3] = -1;
                }
            }
            return bestLapWithSectors;
        }
    }
}
