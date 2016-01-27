using CrewChiefV4.Events;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

/**
 * Holds all the data collected from the memory mapped file for the current tick
 */
namespace CrewChiefV4.GameState
{
    public enum SessionType
    {
        Unavailable, Practice, Qualify, Race, HotLap
    }
    public enum SessionPhase
    {
        Unavailable, Garage, Gridwalk, Formation, Countdown, Green, Checkered, Finished
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
        Hard, Medium, Soft, Wet, Intermediate, Prime, Option, Road, Bias_Ply, Unknown_Race
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
        public Single EngineRpm = 0;

        // Maximum engine speed
        public Single MaxEngineRpm = 0;

        // Unit: Celcius
        public Single EngineWaterTemp = 0;

        // Unit: Celcius
        public Single EngineOilTemp = 0;

        // Unit: ?
        public Single EngineOilPressure = 0;

        public int MinutesIntoSessionBeforeMonitoring = 0;
        
    }

    public class FuelData
    {
        // Unit: ?
        public Single FuelPressure = 0;

        // Current amount of fuel in the tank(s)
        // Unit: Liters (l)
        public Single FuelLeft = 0;

        // Maximum capacity of fuel tank(s)
        // Unit: Liters (l)
        public Single FuelCapacity = 0;

        public Boolean FuelUseActive = false;
    }

    public class CarDamageData
    {
        public Boolean DamageEnabled = false;

        public DamageLevel OverallTransmissionDamage = DamageLevel.UNKNOWN;

        public DamageLevel OverallEngineDamage = DamageLevel.UNKNOWN;

        public DamageLevel OverallAeroDamage = DamageLevel.UNKNOWN;

        public CornerData SuspensionDamageStatus = new CornerData();

        public CornerData BrakeDamageStatus = new CornerData();
    }

    public class SessionData    
    {
        public List<String> formattedPlayerLapTimes = new List<String>();

        public TrackDefinition TrackDefinition = null;

        public Boolean IsDisqualified = false;

        public FlagEnum Flag = FlagEnum.GREEN;

        public Boolean IsNewSession = false;

        public Boolean SessionHasFixedTime = false;

        public SessionType SessionType = SessionType.Unavailable;

        public DateTime SessionStartTime;

        // in minutes, 0 if this session is a fixed number of laps rather than a fixed time.
        public float SessionRunTime = 0;

        public int SessionNumberOfLaps = 0;

        public int SessionStartPosition = 0;

        public int NumCarsAtStartOfSession = 0;
        
        // race number in ongoing championship (zero indexed)
        public int EventIndex = 0;

        // zero indexed - you multi iteration sessions like DTM qual
        public int SessionIteration = 0;

        // as soon as the player leaves the racing surface this is set to false
        public Boolean CurrentLapIsValid = true;

        public Boolean PreviousLapWasValid = true;

        public SessionPhase SessionPhase = SessionPhase.Unavailable;

        public Boolean IsNewLap = false;

        // How many laps the player has completed. If this value is 6, the player is on his 7th lap.
        public int CompletedLaps = 0;
        
        // Unit: Seconds (-1.0 = none)
        public Single LapTimePrevious = -1;

        public Single LapTimePreviousEstimateForInvalidLap = -1;

        // Unit: Seconds (-1.0 = none)
        public Single LapTimeCurrent = -1;

        public Boolean LeaderHasFinishedRace = false;

        // Current position (1 = first place)
        public int Position = 0;

        public int UnFilteredPosition = 0;

        public float GameTimeAtLastPositionFrontChange = 0;

        public float GameTimeAtLastPositionBehindChange = 0;

        // Number of cars (including the player) in the race
        public int NumCars = 0;

        public Single SessionRunningTime = 0;

        // ...
        public Single SessionTimeRemaining = 0;

        // ...
        public Single PlayerLapTimeSessionBest = -1;

        public Single OpponentsLapTimeSessionBestOverall = -1;

        public Single OpponentsLapTimeSessionBestPlayerClass = -1;

        public Single OverallSessionBestLapTime = -1;

        public Single PlayerClassSessionBestLapTime = -1;

        // ...
        public Single TimeDeltaFront = 0;

        // ...
        public Single TimeDeltaBehind = 0;

        // 0 means we don't know what sector we're in. This is 1-indexed
        public int SectorNumber = 0;

        public Boolean IsNewSector = false;

        // these are used for quick n dirty checks to see if we're racing the same opponent in front / behind,
        // without iterating over the Opponents list. Or for cases (like R3E) where we don't have an opponents list
        public Boolean IsRacingSameCarInFront = true;

        public Boolean IsRacingSameCarBehind = true;

        public Boolean HasLeadChanged = false;

        public Dictionary<int, float> SessionTimesAtEndOfSectors = new Dictionary<int, float>();

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

        public SessionData()
        {
            SessionTimesAtEndOfSectors.Add(1, -1); 
            SessionTimesAtEndOfSectors.Add(2, -1); 
            SessionTimesAtEndOfSectors.Add(3, -1);
        }
    }

    public class PositionAndMotionData
    {
        // Unit: Meter per second (m/s).
        public Single CarSpeed = 0;

        // distance (m) from the start line (around the track)
        public Single DistanceRoundTrack = 0;

        // other stuff like X/Y/Z co-ordinates, acceleration, orientation, ...
    }

    public class OpponentData
    {
        // set this to false if this opponent drops out of the race (i.e. leaves a server)
        public Boolean IsActive = true;

        // the name read directly from the game data - might be a 'handle' with all kinds of random crap in it
        public String DriverRawName = null;

        public Boolean DriverNameSet = false;

        public int Position = 0;

        public int UnFilteredPosition = 0;

        public float SessionTimeAtLastPositionChange = -1;
        
        public int CompletedLaps = 0;

        public int CurrentSectorNumber = 0;
               
        public float Speed = 0;

        public float[] WorldPosition;

        public Boolean IsNewLap = false;

        public float DistanceRoundTrack = 0;

        public float CurrentBestLapTime = -1;

        public float PreviousBestLapTime = -1;

        public float bestSector1Time = -1;

        public float bestSector2Time = -1;

        public float bestSector3Time = -1;

        public float LastLapTime = -1;

        public Boolean LastLapValid = true;
        
        public List<LapData> OpponentLapData = new List<LapData>();

        public CarData.CarClass CarClass = CarData.getDefaultCarClass();

        // for DTM 2015
        public Boolean HasStartedExtraLap = false;

        public TyreType CurrentTyres = TyreType.Unknown_Race;

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
            float[] bestLapTimeAndSectorsSectors = new float[] {-1, -1, -1, -1};
            if (OpponentLapData.Count > 1)
            {
                if (lapsToCheck == -1)
                {
                    lapsToCheck = OpponentLapData.Count;
                }
                // count-2 because we're not interested in the current lap
                for (int i = OpponentLapData.Count - 2; i >= OpponentLapData.Count - lapsToCheck && i >= 0; i--)
                {
                    LapData thisLapTime = OpponentLapData[i];
                    if (thisLapTime.IsValid)
                    {
                        if (bestLapTimeAndSectorsSectors[0] == -1 ||
                            (thisLapTime.LapTime != -1 && thisLapTime.LapTime < bestLapTimeAndSectorsSectors[0]))
                        {
                            bestLapTimeAndSectorsSectors[0] = thisLapTime.LapTime;
                            int sectorCount = thisLapTime.SectorTimes.Count();
                            if (sectorCount > 0)
                            {
                                bestLapTimeAndSectorsSectors[1] = thisLapTime.SectorTimes[0];
                            }
                            if (sectorCount > 1)
                            {
                                bestLapTimeAndSectorsSectors[2] = thisLapTime.SectorTimes[1];
                            }
                            if (sectorCount > 2)
                            {
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
            if (OpponentLapData.Count > 0) 
            {
                if (OpponentLapData[OpponentLapData.Count - 1].GameTimeAtSectorEnd.Count > sectorNumber) 
                {
                    return OpponentLapData[OpponentLapData.Count - 1].GameTimeAtSectorEnd[sectorNumber];
                }
            }
            if (OpponentLapData.Count > 1) 
            {
                if (OpponentLapData[OpponentLapData.Count - 2].GameTimeAtSectorEnd.Count > sectorNumber)
                {
                    return OpponentLapData[OpponentLapData.Count - 2].GameTimeAtSectorEnd[sectorNumber];
                }
            }
            return -1;
        }

        public void StartNewLap(int lapNumber, int position, Boolean inPits, float gameTimeAtStart, Boolean isRaining, float trackTemp, float airTemp)
        {
            LapData thisLapData = new LapData();
            thisLapData.Conditions.Add(new LapConditions(isRaining, trackTemp, airTemp));
            thisLapData.GameTimeAtLapStart = gameTimeAtStart;
            thisLapData.OutLap = inPits;
            thisLapData.PositionAtStart = position;
            thisLapData.LapNumber = lapNumber;
            OpponentLapData.Add(thisLapData);
        }

        public void CompleteLapWithEstimatedLapTime(int position, float gameTimeAtLapEnd, float worldRecordLapTime,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {
            AddSectorData(position, -1, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (lapData.SectorTimes.Count > 2)
                {
                    float estimatedLapTime = lapData.SectorTimes.Sum();
                    LastLapValid = lapData.IsValid;
                    if (estimatedLapTime > worldRecordLapTime - 0.1)
                    {
                        lapData.LapTime = estimatedLapTime;
                        LastLapTime = estimatedLapTime;
                        if (lapData.IsValid && (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime))
                        {
                            PreviousBestLapTime = CurrentBestLapTime;
                            CurrentBestLapTime = lapData.LapTime;
                        }
                    }
                    else
                    {
                        LastLapValid = false;
                        LastLapTime = -1;
                    }
                }
            }
        }

        public void CompleteLapWithProvidedLapTime(int position, float gameTimeAtLapEnd, float providedLapTime,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {            
            if (OpponentLapData.Count > 0)
            {                
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                AddSectorData(position, providedLapTime, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
                lapData.LapTime = providedLapTime;
                LastLapTime = providedLapTime;
                if (lapData.IsValid && (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime))
                {
                    PreviousBestLapTime = CurrentBestLapTime;
                    CurrentBestLapTime = lapData.LapTime;
                }
                LastLapValid = lapData.IsValid;
            }
        }

        public void AddSectorData(int position, float cumulativeSectorTime, float gameTimeAtSectorEnd, Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (cumulativeSectorTime <= 0)
                {
                    cumulativeSectorTime = gameTimeAtSectorEnd - lapData.GameTimeAtLapStart;
                }
                float thisSectorTime = cumulativeSectorTime;
                int sectorNumber = 1;
                foreach (float sectorTime in lapData.SectorTimes)
                {
                    sectorNumber++;
                    thisSectorTime = thisSectorTime - sectorTime;
                }
                lapData.SectorTimes.Add(thisSectorTime);
                if (lapIsValid && thisSectorTime > 0)
                {
                    if (sectorNumber == 1 && (bestSector1Time == -1 || thisSectorTime < bestSector1Time))
                    {
                        bestSector1Time = thisSectorTime;
                    }
                    if (sectorNumber == 2 && (bestSector2Time == -1 || thisSectorTime < bestSector2Time))
                    {
                        bestSector2Time = thisSectorTime;
                    }
                    if (sectorNumber == 3 && (bestSector3Time == -1 || thisSectorTime < bestSector3Time))
                    {
                        bestSector3Time = thisSectorTime;
                    }
                }
                lapData.SectorPositions.Add(position);
                lapData.GameTimeAtSectorEnd.Add(gameTimeAtSectorEnd);
                if (lapData.IsValid && !lapIsValid)
                {
                    lapData.IsValid = false;
                }
                lapData.Conditions.Add(new LapConditions(isRaining, trackTemp, airTemp));
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

        public OpponentDelta getTimeDifferenceToPlayer(SessionData playerSessionData)
        {

            int lastSectorPlayerCompleted = playerSessionData.SectorNumber == 1 ? 3 : playerSessionData.SectorNumber - 1;
            float playerLapTimeToUse = playerSessionData.LapTimePrevious;
            if (playerLapTimeToUse == 0 || playerLapTimeToUse == -1) 
            {
                playerLapTimeToUse = playerSessionData.LapTimePreviousEstimateForInvalidLap;
            }

            if (playerSessionData.SessionTimesAtEndOfSectors[lastSectorPlayerCompleted] == -1 || getGameTimeWhenSectorWasLastCompleted(lastSectorPlayerCompleted - 1) == -1)
            {
                return null;
            }
            float timeDifference;
            if (Position == playerSessionData.Position + 1) 
            {
                timeDifference = -1 * playerSessionData.TimeDeltaBehind;
            }
            else if (Position == playerSessionData.Position - 1)
            {
                timeDifference = playerSessionData.TimeDeltaFront;
            }
            else 
            {
                timeDifference = playerSessionData.SessionTimesAtEndOfSectors[lastSectorPlayerCompleted] - getGameTimeWhenSectorWasLastCompleted(lastSectorPlayerCompleted - 1);
            }
            // if the player is ahead, the time difference is negative

            if (((playerSessionData.CompletedLaps == CompletedLaps + 1 && timeDifference < 0 && CurrentSectorNumber < playerSessionData.SectorNumber) || 
                playerSessionData.CompletedLaps > CompletedLaps + 1 ||
                (playerSessionData.CompletedLaps == CompletedLaps - 1 && timeDifference > 0 && CurrentSectorNumber >= playerSessionData.SectorNumber) || 
                playerSessionData.CompletedLaps < CompletedLaps - 1))
            {
                // there's more than a lap difference
                return new OpponentDelta(-1, playerSessionData.CompletedLaps - CompletedLaps);
            }
            else if (playerSessionData.CompletedLaps == CompletedLaps + 1 && timeDifference > 0)
            {
                // the player has completed 1 more lap but is behind on track
                return new OpponentDelta(timeDifference - playerLapTimeToUse, 0);
            }
            else if (playerSessionData.CompletedLaps == CompletedLaps - 1 && timeDifference < 0)
            {
                // the player has completed 1 less lap but is ahead on track
                return new OpponentDelta(playerLapTimeToUse - timeDifference, 0);
            } 
            else 
            {
                return new OpponentDelta(timeDifference, 0);
            }
        }

        public class OpponentDelta
        {
            public float time;
            public int lapDifference;
            public OpponentDelta(float time, int lapDifference)
            {
                this.time = time;
                this.lapDifference = lapDifference;
            }
        }
    }

    public class LapData
    {
        public int LapNumber = 0;
        public int PositionAtStart = 0;
        public Boolean IsValid = true;
        public float LapTime = -1;
        public float GameTimeAtLapStart = 0;
        public List<float> SectorTimes = new List<float>();
        public List<float> GameTimeAtSectorEnd = new List<float>();
        public List<LapConditions> Conditions = new List<LapConditions>();
        public List<int> SectorPositions = new List<int>();
        public Boolean OutLap = false;
        public Boolean InLap = false;
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
        public Single ThrottlePedal = 0;

        // ...
        public Single BrakePedal = 0;

        // ...
        public Single ClutchPedal = 0;

        // ...
        public Single BrakeBias = 0;
    }

    public class PitData
    {
        public PitWindow PitWindow = PitWindow.Unavailable;

        // The minute/lap into which you're allowed/obligated to pit
        // Unit: Minutes in time-based sessions, otherwise lap

        public Boolean InPitlane = false;

        public Boolean OnInLap = false;

        public Boolean OnOutLap = false;

        public Boolean IsMakingMandatoryPitStop = false;

        // this is true for one tick, when the player is about to exit the pits
        public Boolean IsAtPitExit = false;

        public Boolean IsRefuellingAllowed = false;

        public Boolean HasRequestedPitStop = false;

        public Boolean LeaderIsPitting = false;

        public Boolean CarInFrontIsPitting = false;

        public Boolean CarBehindIsPitting = false;

        // yuk...
        public OpponentData OpponentForLeaderPitting = null;
        public OpponentData OpponentForCarAheadPitting = null;
        public OpponentData OpponentForCarBehindPitting = null;


        // TODO: will this always be an Integer?
        public int PitWindowStart = 0;

        // The minute/lap into which you can/should pit
        // Unit: Minutes in time based sessions, otherwise lap
        public int PitWindowEnd = 0;

        public Boolean HasMandatoryPitStop = false;

        public Boolean HasMandatoryTyreChange = false;

        public Boolean HasMandatoryDriverChange = false;

        public TyreType MandatoryTyreChangeRequiredTyreType = TyreType.Unknown_Race;

        // might be a number of laps or a number of minutes. These are (currently) for DTM 2014. If we start on Options, 
        // MaxPermittedDistanceOnCurrentTyre will be half race distance (rounded down), if we start on Primes 
        // MinPermittedDistanceOnCurrentTyre will be half race distance (rounded up)
        public int MaxPermittedDistanceOnCurrentTyre = -1;
        public int MinPermittedDistanceOnCurrentTyre = -1;
    }

    public class PenatiesData
    {
        public Boolean HasDriveThrough = false;

        public Boolean HasStopAndGo = false;

        // from R3E data - what is this??
        public Boolean HasPitStop = false;

        public Boolean HasTimeDeduction = false;

        public Boolean HasSlowDown = false;

        // Number of penalties pending for the player
        public int NumPenalties = 0;

        // Total number of cut track warnings
        public int CutTrackWarnings = 0;

        public Boolean IsOffRacingSurface = false;
    }

    public class TyreData
    {
        public Boolean LeftFrontAttached = true;
        public Boolean RightFrontAttached = true;
        public Boolean LeftRearAttached = true;
        public Boolean RightRearAttached = true;

        public Boolean TireWearActive = false;

        // true if all tyres are the same type
        public Boolean HasMatchedTyreTypes = true;

        public TyreType FrontLeftTyreType = TyreType.Unknown_Race;
        public TyreType FrontRightTyreType = TyreType.Unknown_Race;
        public TyreType RearLeftTyreType = TyreType.Unknown_Race;
        public TyreType RearRightTyreType = TyreType.Unknown_Race;

        public Single FrontLeft_LeftTemp = 0;
        public Single FrontLeft_CenterTemp = 0;
        public Single FrontLeft_RightTemp = 0;

        public Single FrontRight_LeftTemp = 0;
        public Single FrontRight_CenterTemp = 0;
        public Single FrontRight_RightTemp = 0;

        public Single RearLeft_LeftTemp = 0;
        public Single RearLeft_CenterTemp = 0;
        public Single RearLeft_RightTemp = 0;

        public Single RearRight_LeftTemp = 0;
        public Single RearRight_CenterTemp = 0;
        public Single RearRight_RightTemp = 0;

        public Single PeakFrontLeftTemperatureForLap = 0;
        public Single PeakFrontRightTemperatureForLap = 0;
        public Single PeakRearLeftTemperatureForLap = 0;
        public Single PeakRearRightTemperatureForLap = 0;

        public float FrontLeftPercentWear = 0;
        public float FrontRightPercentWear = 0;
        public float RearLeftPercentWear = 0;
        public float RearRightPercentWear = 0;

        public Single FrontLeftPressure = 0;
        public Single FrontRightPressure = 0;
        public Single RearLeftPressure = 0;
        public Single RearRightPressure = 0;

        public CornerData TyreTempStatus = new CornerData();

        public CornerData TyreConditionStatus = new CornerData();

        public CornerData BrakeTempStatus = new CornerData();

        public Single LeftFrontBrakeTemp = 0;
        public Single RightFrontBrakeTemp = 0;
        public Single LeftRearBrakeTemp = 0;
        public Single RightRearBrakeTemp = 0;

        public Boolean LeftFrontIsLocked = false;
        public Boolean RightFrontIsLocked = false;
        public Boolean LeftRearIsLocked = false;
        public Boolean RightRearIsLocked = false;
        public Boolean LeftFrontIsSpinning = false;
        public Boolean RightFrontIsSpinning = false;
        public Boolean LeftRearIsSpinning = false;
        public Boolean RightRearIsSpinning = false;
    }

    public class Conditions
    {
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

            public ConditionsSample(DateTime time, int lapCount, int sectorNumber, float AmbientTemperature, float TrackTemperature, float RainDensity,
                float WindSpeed, float WindDirectionX, float WindDirectionY, float CloudBrightness)
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
            }
        }

        public DateTime timeOfMostRecentSample = DateTime.MinValue;
        public List<ConditionsSample> samples = new List<ConditionsSample>();

        public void addSample(DateTime time, int lapCount, int sectorNumber, float AmbientTemperature, float TrackTemperature, float RainDensity,
                float WindSpeed, float WindDirectionX, float WindDirectionY, float CloudBrightness)
        {
            timeOfMostRecentSample = time;
            samples.Add(new ConditionsSample(time, lapCount, sectorNumber, AmbientTemperature, TrackTemperature, RainDensity,
                WindSpeed, WindDirectionX, WindDirectionY, CloudBrightness));
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
    }

    public class OvertakingAids
    {
        public Boolean PushToPassAvailable = false;
	    public Boolean PushToPassEngaged = false;
	    public int PushToPassActivationsRemaining = 0;
	    public Single PushToPassEngagedTimeLeft = 0;
	    public Single PushToPassWaitTimeLeft = 0;

        public Boolean DrsEnabled = false;
        public Boolean DrsAvailable = false;
        public Boolean DrsEngaged = false;
        public Single DrsRange = 0;
    }

    public class GameStateData
    {
        public long Ticks;

        public DateTime Now;

        public CarData.CarClass carClass = CarData.getDefaultCarClass();

        public EngineData EngineData = new EngineData();

        public TransmissionData TransmissionData = new TransmissionData();

        public FuelData FuelData = new FuelData();

        public CarDamageData CarDamageData = new CarDamageData();

        public ControlData ControlData = new ControlData();

        public SessionData SessionData = new SessionData();

        public PitData PitData = new PitData();

        public PenatiesData PenaltiesData = new PenatiesData();

        public TyreData TyreData = new TyreData();

        public PositionAndMotionData PositionAndMotionData = new PositionAndMotionData();

        public Dictionary<Object, OpponentData> OpponentData = new Dictionary<Object, OpponentData>();

        public Conditions Conditions = new Conditions();

        public OvertakingAids OvertakingAids = new OvertakingAids();

        public GameStateData(long ticks)
        {
            this.Ticks = ticks;
            this.Now = new DateTime(ticks);
        }

        // some convenience methods
        public Boolean isLast()
        {
            return SessionData.UnFilteredPosition == SessionData.NumCars;
        }

        public List<String> getRawDriverNames()
        {
            List<String> rawDriverNames = new List<String>();
            foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
            {
                if (!rawDriverNames.Contains(entry.Value.DriverRawName))
                {
                    rawDriverNames.Add(entry.Value.DriverRawName);                
                }
            }
            rawDriverNames.Sort();
            return rawDriverNames;
        }

        public OpponentData getOpponentAtPosition(int position, Boolean useUnfilteredPosition) 
        {
            Object opponentKey = getOpponentKeyAtPosition(position, useUnfilteredPosition);
            if (opponentKey != null && OpponentData.ContainsKey(opponentKey))
            {
                return OpponentData[opponentKey];
            }
            else
            {
                return null;
            }
        }

        public Object getOpponentKeyInFrontOnTrack()
        {
            Object opponentKeyClosestInFront = null;
            Object opponentKeyFurthestBehind = null;
            float closestDistanceFront = SessionData.TrackDefinition.trackLength;
            float furthestDistanceBehind = 0;
            foreach (KeyValuePair<Object, OpponentData> opponent in OpponentData)
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
            {
                return opponentKeyClosestInFront;
            }
            else
            {
                return opponentKeyFurthestBehind;
            }
        }

        public Object getOpponentKeyBehindOnTrack()
        {
            Object opponentKeyClosestBehind = null;
            Object opponentKeyFurthestInFront = null;
            float closestDistanceBehind = SessionData.TrackDefinition.trackLength;
            float furthestDistanceInFront = 0;
            foreach (KeyValuePair<Object, OpponentData> opponent in OpponentData)
            {
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
            {
                return opponentKeyClosestBehind;
            }
            else
            {
                return opponentKeyFurthestInFront;
            }
        }

        public Object getOpponentKeyInFront(Boolean useUnfilteredPosition)
        {
            if (SessionData.Position > 1)
            {
                return getOpponentKeyAtPosition(SessionData.Position - 1, useUnfilteredPosition);
            }
            else
            {
                return null;
            }
        }

        public Object getOpponentKeyBehind(Boolean useUnfilteredPosition)
        {
            if (SessionData.Position < SessionData.NumCars)
            {
                return getOpponentKeyAtPosition(SessionData.Position + 1, useUnfilteredPosition);
            }
            else
            {
                return null;
            }
        }

        public Object getOpponentKeyAtPosition(int position, Boolean useUnfilteredPosition)
        {
            if (OpponentData.Count != 0)
            {
                foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
                {
                    if (useUnfilteredPosition)
                    {
                        if (entry.Value.UnFilteredPosition == position)
                        {
                            return entry.Key;
                        }
                    }
                    else
                    {
                        if (entry.Value.Position == position)
                        {
                            return entry.Key;
                        }
                    }                    
                }
            }
            return null;
        }
        
        public void display()
        {
            Console.WriteLine("Laps completed = " + SessionData.CompletedLaps);
            Console.WriteLine("Time elapsed = " + SessionData.SessionRunningTime);
            Console.WriteLine("Position = " + SessionData.Position);
            Console.WriteLine("Session phase = " + SessionData.SessionPhase);
        }

        public void displayOpponentData()
        {
            Console.WriteLine("got " + OpponentData.Count + " opponents");
            foreach (KeyValuePair<Object, OpponentData> opponent in OpponentData)
            {
                Console.WriteLine("last laptime " + opponent.Value.getLastLapTime() + " completed laps " + opponent.Value.CompletedLaps + 
                    " ID " + opponent.Key + " name " + opponent.Value.DriverRawName + " active " + opponent.Value.IsActive + 
                    " approx speed " + opponent.Value.Speed + " position " + opponent.Value.Position);
            }
        }

        public float[] getTimeAndSectorsForBestOpponentLapInWindow(int lapsToCheck, CarData.CarClassEnum carClassToCheck)
        {
            float[] bestLapWithSectors =  new float[] { -1, -1, -1, -1 };
            foreach (KeyValuePair<Object, OpponentData> entry in OpponentData)
            {
                if (entry.Value.CarClass.carClassEnum == carClassToCheck)
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
