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
        Unavailable, Practice, Qualify, Race, HotLap
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
        Hard, Medium, Soft, Wet, Intermediate, Road, Bias_Ply, Unknown_Race, R3E_NEW, R3E_NEW_Prime, R3E_NEW_Option
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
        PENDING, PITS_CLOSED, PITS_OPEN_LEAD_LAP_VEHICLES, PITS_OPEN, LAST_LAP_NEXT, LAST_LAP_CURRENT, RACING
    }

    public class FlagData
    {
        // holds newer (RF2 & Raceroom) flag data. This is game dependent - only RF2 and R3E will use this.
        public FlagEnum[] sectorFlags = new FlagEnum[] { FlagEnum.GREEN, FlagEnum.GREEN, FlagEnum.GREEN };
        public Boolean isFullCourseYellow; // FCY rules apply, no other announcements
        public Boolean isLocalYellow;  // local yellow - no overtaking, slow down
        // note that for RaceRoom we might have to calculate this. < 0 means we've passed the incident.
        public float distanceToNearestIncident = -1;
        public FullCourseYellowPhase fcyPhase = FullCourseYellowPhase.RACING;
        public int lapCountWhenLastWentGreen = -1;
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

        public DateTime YellowFlagStartTime = DateTime.Now;

        public Boolean IsNewSession = false;

        public Boolean SessionHasFixedTime = false;

        public SessionType SessionType = SessionType.Unavailable;

        public DateTime SessionStartTime;

        // in minutes, 0 if this session is a fixed number of laps rather than a fixed time.
        public float SessionTotalRunTime = 0;

        public int SessionNumberOfLaps = 0;

        // some timed sessions have an extra lap added after the timer reaches zero
        public Boolean HasExtraLap = false;

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

        public int LeaderSectorNumber = 0;

        public int PositionAtStartOfCurrentLap = 0;

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

        public Single PlayerLapTimeSessionBestPrevious = -1;

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

        public TrackLandmarksTiming trackLandmarksTiming = new TrackLandmarksTiming();

        // Player lap times with sector information
        public List<LapData> PlayerLapData = new List<LapData>();

        public String stoppedInLandmark = null;

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
        }

        public void playerStartNewLap(int lapNumber, int position, Boolean inPits, float gameTimeAtStart, Boolean isRaining, float trackTemp, float airTemp)
        {
            LapData thisLapData = new LapData();
            thisLapData.Conditions.Add(new LapConditions(isRaining, trackTemp, airTemp));
            thisLapData.GameTimeAtLapStart = gameTimeAtStart;
            thisLapData.OutLap = inPits;
            thisLapData.PositionAtStart = position;
            thisLapData.LapNumber = lapNumber;
            PlayerLapData.Add(thisLapData);
        }

        public void playerCompleteLapWithProvidedLapTime(int position, float gameTimeAtLapEnd, float providedLapTime,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining)
        {
            if (PlayerLapData.Count > 0)
            {
                LapData lapData = PlayerLapData[PlayerLapData.Count - 1];
                playerAddCumulativeSectorData(position, providedLapTime, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
                lapData.LapTime = providedLapTime;

                LapTimePrevious = providedLapTime;
                if (lapData.IsValid && (PlayerLapTimeSessionBest == -1 || PlayerLapTimeSessionBest > lapData.LapTime))
                {
                    PlayerLapTimeSessionBestPrevious = PlayerLapTimeSessionBest;
                    PlayerLapTimeSessionBest = lapData.LapTime;

                    if (lapData.SectorTimes.Count > 0)
                        PlayerBestLapSector1Time = lapData.SectorTimes[0];
                    if (lapData.SectorTimes.Count > 1)
                        PlayerBestLapSector2Time = lapData.SectorTimes[1];
                    if (lapData.SectorTimes.Count > 2)
                        PlayerBestLapSector3Time = lapData.SectorTimes[2];
                }
                PreviousLapWasValid = lapData.IsValid;
            }

            // Not sure we need this for player.
            //if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            //{
            //  isProbablyLastLap = true;
            //}
        }

        public void playerAddCumulativeSectorData(int position, float cumulativeSectorTime, float gameTimeAtSectorEnd, Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {
            if (PlayerLapData.Count > 0)
            {
                LapData lapData = PlayerLapData[PlayerLapData.Count - 1];
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
                    if (sectorNumber == 1 && (PlayerBestSector1Time == -1 || thisSectorTime < PlayerBestSector1Time))
                    {
                        PlayerBestSector1Time = thisSectorTime;
                    }
                    if (sectorNumber == 2 && (PlayerBestSector2Time == -1 || thisSectorTime < PlayerBestSector2Time))
                    {
                        PlayerBestSector2Time = thisSectorTime;
                    }
                    if (sectorNumber == 3 && (PlayerBestSector3Time == -1 || thisSectorTime < PlayerBestSector3Time))
                    {
                        PlayerBestSector3Time = thisSectorTime;
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
            return bestLapTimeAndSectorsSectors;
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

        public CarData.CarClass CarClass = new CarData.CarClass();

        // for DTM 2015
        public Boolean HasStartedExtraLap = false;

        public TyreType CurrentTyres = TyreType.Unknown_Race;

        public Boolean isProbablyLastLap = false;

        public int IsReallyDisconnectedCounter = 0;

        // be careful with this one, not all games actually set it...
        public Boolean InPits = false;

        public TrackLandmarksTiming trackLandmarksTiming = new TrackLandmarksTiming();

        public String stoppedInLandmark = null;

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
                        if (bestLapTimeAndSectorsSectors[0] == -1 ||
                            (thisLapTime.LapTime > 0 && thisLapTime.LapTime < bestLapTimeAndSectorsSectors[0]))
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

        public void CompleteLapWithEstimatedLapTime(int position, float gameTimeAtLapEnd, float worldRecordLapTime, float worldRecordS1Time, float worldRecordS2Time, float worldRecordS3Time,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining)
        {
            AddCumulativeSectorData(position, -1, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (lapData.SectorTimes.Count > 2)
                {
                    float estimatedLapTime = lapData.SectorTimes.Sum();
                    LastLapValid = lapData.IsValid;
                    if (lapData.SectorTimes[0] > worldRecordS1Time - 0.1 && lapData.SectorTimes[1] > worldRecordS2Time - 0.1 && lapData.SectorTimes[2] > worldRecordS2Time - 0.1 &&
                        estimatedLapTime > worldRecordLapTime - 0.1 && estimatedLapTime > 0)
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
                        lapData.IsValid = false;
                    }
                }
            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }

        public void CompleteLapWithLastSectorTime(int position, float lastSectorTime, float gameTimeAtLapEnd,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining)
        {
            AddSectorData(position, lastSectorTime, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                if (lapData.SectorTimes.Count > 2)
                {
                    float lapTime = lapData.SectorTimes.Sum();
                    LastLapValid = lapData.IsValid;
                    if (LastLapValid)
                    {
                        lapData.LapTime = lapTime;
                        LastLapTime = lapTime;
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
                        lapData.IsValid = false;
                    }
                }
            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }

        public void CompleteLapWithProvidedLapTime(int position, float gameTimeAtLapEnd, float providedLapTime,
            Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp, Boolean sessionLengthIsTime, float sessionTimeRemaining)
        {
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                AddCumulativeSectorData(position, providedLapTime, gameTimeAtLapEnd, lapIsValid, isRaining, trackTemp, airTemp);
                lapData.LapTime = providedLapTime;
                LastLapTime = providedLapTime;
                if (lapData.IsValid && (CurrentBestLapTime == -1 || CurrentBestLapTime > lapData.LapTime))
                {
                    PreviousBestLapTime = CurrentBestLapTime;
                    CurrentBestLapTime = lapData.LapTime;
                }
                LastLapValid = lapData.IsValid;
            }
            if (sessionLengthIsTime && sessionTimeRemaining > 0 && CurrentBestLapTime > 0 && sessionTimeRemaining < CurrentBestLapTime - 5)
            {
                isProbablyLastLap = true;
            }
        }

        public void AddCumulativeSectorData(int position, float cumulativeSectorTime, float gameTimeAtSectorEnd, Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
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

        public void AddSectorData(int position, float thisSectorTime, float gameTimeAtSectorEnd, Boolean lapIsValid, Boolean isRaining, float trackTemp, float airTemp)
        {
            if (OpponentLapData.Count > 0)
            {
                LapData lapData = OpponentLapData[OpponentLapData.Count - 1];
                int sectorNumber = 1;
                foreach (float sectorTime in lapData.SectorTimes)
                {
                    sectorNumber++;
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
            public String biggestTimeDifferenceLandmark = null;
            public String biggestStartSpeedDifferenceLandmark = null;
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
        private static float minSignificantRelativeTimeDiffOvertakingSpot = 0.05f;    // 5% - is this a good value?
        private static float minSignificantRelativeStartSpeedDiffOvertakingSpot = 0.07f;   // 7% - is this a good value? 
        // these are used when we're checking time / speed difference at places where overtaking is rare, so need to be bigger 
        private static float minSignificantRelativeTimeDiff = 0.08f;    // 8% - is this a good value?
        private static float minSignificantRelativeStartSpeedDiff = 0.1f;   // 10% - is this a good value? 

        private Dictionary<string, TrackLandmarksTimingData> sessionData = new Dictionary<string, TrackLandmarksTimingData>();

        // temporary variables for tracking landmark timings during a session - we add a timing when these are non-null and
        // we hit the end of this named landmark.
        private String landmarkNameStart = null;
        private float landmarkStartTime = -1;
        private float landmarkStartSpeed = -1;
        private int landmarkStoppedCount = 0;
        private Boolean inLandmark = false;

        // wonder if this'll work...
        private String nearLandmarkName = null;

        // quick n dirty tracking of when we're at the mid-point of a landmark - maybe the apex. This is only non-null for a single tick.
        public String atMidPointOfLandmark = null;

        private void addTimeAndSpeeds(String landmarkName, float time, float startSpeed, float endSpeed, Boolean isCommonOvertakingSpot)
        {
            if (time > 0)
            {
                if (!sessionData.ContainsKey(landmarkName))
                {
                    sessionData.Add(landmarkName, new TrackLandmarksTimingData(isCommonOvertakingSpot));
                }
                sessionData[landmarkName].addTimeAndSpeeds(time, startSpeed, endSpeed);
            }
        }
        public float[] getBestTimeAndSpeeds(String landmarkName)
        {
            return getBestTimeAndSpeeds(landmarkName, 5, 2);
        }

        // returns [timeInSection, entrySpeed, exitSpeed] for the quickest time through that section
        public float[] getBestTimeAndSpeeds(String landmarkName, int lapsToCheck, int minTimesRequired)
        {
            if (!sessionData.ContainsKey(landmarkName))
            {
                return new float[] {- 1f, -1f, 1f };
            }
            float[] bestTimeAndSpeeds = new float[] { float.MaxValue, -1f, 1f };
            TrackLandmarksTimingData trackLandmarksTimingData = sessionData[landmarkName];
            if (trackLandmarksTimingData.timesAndSpeeds.Count < minTimesRequired)
            {
                return new float[] { -1f, -1f, 1f };
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
        public LandmarkAndDeltaType getLandmarkWhereIAmFaster(TrackLandmarksTiming otherVehicleTrackLandMarksTiming, Boolean preferCommonOvertakingSpots)
        {
            LandmarkDeltaContainer deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, true, preferCommonOvertakingSpots);
            if (deltasForCommonOvertakingSpots.biggestStartSpeedDifferenceLandmark == null && deltasForCommonOvertakingSpots.biggestTimeDifferenceLandmark == null && preferCommonOvertakingSpots)
            {
                // no hits for common overtaking spots so try again
                deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, true, false);
            }
            // this can contain 2 different results (one for the biggest entry speed difference, one for the biggest relative time difference)
            return deltasForCommonOvertakingSpots.selectLandmark();
        }

        // get the landmark name where I'm either much faster through the section or
        // am about as fast but have significantly higher entry speed
        public LandmarkAndDeltaType getLandmarkWhereIAmSlower(TrackLandmarksTiming otherVehicleTrackLandMarksTiming, Boolean preferCommonOvertakingSpots)
        {
            LandmarkDeltaContainer deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, false, preferCommonOvertakingSpots);
            if (deltasForCommonOvertakingSpots.biggestStartSpeedDifferenceLandmark == null && deltasForCommonOvertakingSpots.biggestTimeDifferenceLandmark == null && preferCommonOvertakingSpots)
            {
                // no hits for common overtaking spots so try again
                deltasForCommonOvertakingSpots = getLandmarksWithBiggestDeltas(otherVehicleTrackLandMarksTiming, false, false);
            }
            // this can contain 2 different results (one for the biggest entry speed difference, one for the biggest relative time difference)
            return deltasForCommonOvertakingSpots.selectLandmark();
        }

	private LandmarkDeltaContainer getLandmarksWithBiggestDeltas(TrackLandmarksTiming otherVehicleTrackLandMarksTiming, Boolean whereImFaster, Boolean preferCommonOvertakingSpots)
        {
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
                    float minSignificantRelativeTimeDiffToUse = thisTiming.isCommonOvertakingSpot ? minSignificantRelativeTimeDiffOvertakingSpot : minSignificantRelativeTimeDiff;
                    float minSignificantRelativeStartSpeedDiffToUse = thisTiming.isCommonOvertakingSpot ? minSignificantRelativeStartSpeedDiffOvertakingSpot : minSignificantRelativeStartSpeedDiff;

                    float[] myBestTimeAndSpeeds = getBestTimeAndSpeeds(landmarkName);
                    float[] otherBestTimeAndSpeeds = otherVehicleTrackLandMarksTiming.getBestTimeAndSpeeds(landmarkName);
                    // for times, other - mine if we want sections where I'm faster (more positive => better), 
                    // or mine - other if we want sections where he's faster (more positive => worse)
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
            return new LandmarkDeltaContainer(biggestTimeDifference, biggestTimeDifferenceLandmark, biggestStartSpeedDifference, biggestSpeedDifferenceLandmark);
        }

        // called for every opponent and the player for each tick
        // TODO: does including current speed in this calculation really reduce the max error? The speed data can be noisy for some
        // games so this might cause more problems than it solves.
        //
        // returns null or a landmark name this car is stopped in
        // TODO: Reformat me    
        public String updateLandmarkTiming(TrackDefinition trackDefinition, float gameTime, float previousDistanceRoundTrack, float currentDistanceRoundTrack, float speed) 
        {
            if (trackDefinition == null || trackDefinition.trackLandmarks == null || trackDefinition.trackLandmarks.Count == 0)
            {
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
                        inLandmark = true;
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
                            inLandmark = false;
                        }
                        else
                        {
                            // we're not in the landmark at all but we never reached the end, so stop looking for the end
                            // This happens when we quit to the pits or when a car leaves the track and rejoins at a different location
                            landmarkNameStart = null;
                            landmarkStartTime = -1;
                            landmarkStartSpeed = -1;
                            inLandmark = false;
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
                foreach (TrackLandmark trackLandmark in trackDefinition.trackLandmarks) 
                {
                    if (currentDistanceRoundTrack > Math.Max(0, trackLandmark.distanceRoundLapStart - 100) &&
                        currentDistanceRoundTrack < Math.Min(trackDefinition.trackLength, trackLandmark.distanceRoundLapEnd))
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

            if (landmarkStoppedCount >= 10)
            {
                // slow for more than 1 second - this assumes 1 tick is 100ms, which isn't necessarily valid but it's close enough. 
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

        public int PitWindowStart = 0;

        // The minute/lap into which you can/should pit
        // Unit: Minutes in time based sessions, otherwise lap
        public int PitWindowEnd = 0;

        public Boolean HasMandatoryPitStop = false;

        public Boolean HasMandatoryTyreChange = false;

        public TyreType MandatoryTyreChangeRequiredTyreType = TyreType.Unknown_Race;

        // might be a number of laps or a number of minutes. These are (currently) for DTM 2014. If we start on Options, 
        // MaxPermittedDistanceOnCurrentTyre will be half race distance (rounded down), if we start on Primes 
        // MinPermittedDistanceOnCurrentTyre will be half race distance (rounded up)
        public int MaxPermittedDistanceOnCurrentTyre = -1;
        public int MinPermittedDistanceOnCurrentTyre = -1;

        // -1 == n/a; 0 = inactive; 1 = active
        public int limiterStatus = -1;

        // RF1 hack for mandatory pit stop windows, which are used to trigger 'box now' messages
        public Boolean ResetEvents;
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
        public String TyreTypeName = "";

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

        public CarData.CarClass carClass = new CarData.CarClass();

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

        public Dictionary<string, OpponentData> OpponentData = new Dictionary<string, OpponentData>();

        public Conditions Conditions = new Conditions();

        public OvertakingAids OvertakingAids = new OvertakingAids();

        public FlagData FlagData = new FlagData();

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

        public OpponentData getOpponentAtPosition(int position, Boolean useUnfilteredPosition)
        {
            string opponentKey = getOpponentKeyAtPosition(position, useUnfilteredPosition);
            if (opponentKey != null && OpponentData.ContainsKey(opponentKey))
            {
                return OpponentData[opponentKey];
            }
            else
            {
                return null;
            }
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
            string opponentKeyClosestBehind = null;
            string opponentKeyFurthestInFront = null;
            float closestDistanceBehind = SessionData.TrackDefinition.trackLength;
            float furthestDistanceInFront = 0.0f;
            foreach (var opponent in OpponentData)
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
                return opponentKeyClosestBehind;
            else
                return opponentKeyFurthestInFront;
        }

        public string getOpponentKeyInFront(Boolean useUnfilteredPosition)
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

        public string getOpponentKeyBehind(Boolean useUnfilteredPosition)
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

        public string getOpponentKeyAtPosition(int position, Boolean useUnfilteredPosition)
        {
            if (OpponentData.Count != 0)
            {
                foreach (KeyValuePair<string, OpponentData> entry in OpponentData)
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
            foreach (KeyValuePair<string, OpponentData> opponent in OpponentData)
            {
                Console.WriteLine("last laptime " + opponent.Value.getLastLapTime() + " completed laps " + opponent.Value.CompletedLaps +
                    " ID " + opponent.Key + " name " + opponent.Value.DriverRawName + " active " + opponent.Value.IsActive +
                    " approx speed " + opponent.Value.Speed + " position " + opponent.Value.Position);
            }
        }

        public float[] getTimeAndSectorsForBestOpponentLapInWindow(int lapsToCheck, String carClassToCheck)
        {
            float[] bestLapWithSectors = new float[] { -1, -1, -1, -1 };
            foreach (KeyValuePair<string, OpponentData> entry in OpponentData)
            {
                if (entry.Value.CarClass.getClassIdentifier() == carClassToCheck)
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
