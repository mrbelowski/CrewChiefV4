using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using iRacingSDK;

using System.Diagnostics;
using System.Globalization;

namespace CrewChiefV4.iRacing
{
    public class  iRacingGameStateMapper : GameStateMapper
    {
        public static String playerName = null;
        private SpeechRecogniser speechRecogniser;
        private TimeDelta timeDelta = null;
        private Car playerCar = null;
        public iRacingGameStateMapper()
        {

        }

        public void versionCheck(Object memoryMappedFileStruct)
        {
            // no version number in r3e shared data so this is a no-op
        }

        public void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }

        public GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState)
        {
            if(memoryMappedFileStruct == null)
            {
                return previousGameState;
            }
            CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper wrapper = (CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            DataSample shared = wrapper.data;
            if(!shared.IsConnected)
            {
                return null;
            }
            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            SessionType lastSessionType = SessionType.Unavailable;
            
            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                lastSessionType = previousGameState.SessionData.SessionType;
            }
            
            currentGameState.SessionData.SessionType = mapToSessionType(shared.Telemetry.Session.SessionType);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Telemetry.SessionTime;
            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;
            
            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase,
                shared.Telemetry.SessionState, 
                currentGameState.SessionData.SessionType, 
                lastSessionRunningTime, 
                (float)shared.Telemetry.SessionTime, 
                previousLapsCompleted,shared.Telemetry.LapCompleted,
                shared.Telemetry.SessionFlags); 
            currentGameState.SessionData.NumCarsAtStartOfSession = shared.SessionData.DriverInfo.CompetingDrivers.Length;
            int sessionNumber = shared.Telemetry.SessionNum;
            iRacingSDK.SessionData._SessionInfo._Sessions currentSessionData  = shared.SessionData.SessionInfo.Sessions[sessionNumber];
            int PlayerCarIdx = shared.Telemetry.PlayerCarIdx;
            Boolean justGoneGreen = false;

            /*if ((lastSessionPhase != currentGameState.SessionData.SessionPhase && (lastSessionPhase == SessionPhase.Unavailable || lastSessionPhase == SessionPhase.Finished)) ||
                ((lastSessionPhase == SessionPhase.Checkered || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow) &&
                    currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) ||
                lastSessionRunningTime > currentGameState.SessionData.SessionRunningTime)*/
            if (currentGameState.SessionData.SessionType != SessionType.Unavailable 
                && lastSessionPhase != SessionPhase.Countdown 
                && lastSessionType != currentGameState.SessionData.SessionType)
            {
                currentGameState.SessionData.IsNewSession = true;
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("sessionType = " + currentGameState.SessionData.SessionType);
                Console.WriteLine("lastSessionPhase = " + lastSessionPhase);
                Console.WriteLine("lastSessionRunningTime = " + lastSessionRunningTime);
                Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                Console.WriteLine("rawSessionPhase = " + shared.Telemetry.SessionState);
                Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                Console.WriteLine("NumCarsAtStartOfSession = " + currentGameState.SessionData.NumCarsAtStartOfSession);

                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                
                currentGameState.SessionData.TrackDefinition = TrackData.getTrackDefinition(shared.SessionData.WeekendInfo.TrackDisplayShortName + ":" + shared.SessionData.WeekendInfo.TrackConfigName, 
                    (float)ParseTrackLength(shared.SessionData.WeekendInfo.TrackLength),3);
                if (currentGameState.SessionData.TrackDefinition.unknownTrack)
                {
                    Console.WriteLine("Track is unknown, setting virtual sectors");
                    currentGameState.SessionData.TrackDefinition.setSectorPointsForUnknownTracks();
                }

                currentGameState.SessionData.SessionNumberOfLaps = ParseInt(currentSessionData.SessionLaps);
                currentGameState.SessionData.LeaderHasFinishedRace = false;
                currentGameState.SessionData.SessionStartPosition = shared.Telemetry.PlayerCarPosition;
                Console.WriteLine("SessionStartPosition = " + currentGameState.SessionData.SessionStartPosition);
                currentGameState.PitData.IsRefuellingAllowed = true;
                if(currentSessionData.IsLimitedTime)
                {
                    currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;
                    currentGameState.SessionData.SessionHasFixedTime = true;
                    Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                }               
                foreach(Car car in shared.Telemetry.RaceCars)
                {
                    
                    String driverName = car.Details.Driver.UserName.ToLower();
                    if (car.CarIdx == PlayerCarIdx)
                    {
                        playerCar = car;
                        if (playerName == null)
                        {
                            NameValidator.validateName(driverName);
                            playerName = driverName;
                        }
                        currentGameState.PitData.InPitlane = shared.Telemetry.CarIdxTrackSurface[PlayerCarIdx] == TrackLocation.InPitStall;                        
                        currentGameState.PositionAndMotionData.DistanceRoundTrack = shared.Telemetry.LapDist;
                    }
                    else
                    {
                        currentGameState.OpponentData.Add(driverName, createOpponentData(car, driverName,
                            false, CarData.CarClassEnum.GT1X));
                    }
                }                
            }
            else
            {
                if (lastSessionPhase != currentGameState.SessionData.SessionPhase)
                {
                    Console.WriteLine("New session phase, was " + lastSessionPhase + " now " + currentGameState.SessionData.SessionPhase);
                    if (currentGameState.SessionData.SessionPhase == SessionPhase.Green)
                    {
                        justGoneGreen = true;
                        // just gone green, so get the session data
                        if (currentSessionData.IsLimitedSessionLaps)
                        {
                            currentGameState.SessionData.SessionTotalRunTime = (float)shared.Telemetry.SessionTimeRemain;

                            currentGameState.SessionData.SessionHasFixedTime = true;
                        }
                        else 
                        {
                            currentGameState.SessionData.SessionNumberOfLaps = ParseInt(currentSessionData.SessionLaps);
                            currentGameState.SessionData.SessionHasFixedTime = false;
                        }
                        Console.WriteLine("Player is using car class " + currentGameState.carClass.getClassIdentifier());

                        if (previousGameState != null)
                        {
                            currentGameState.PitData.IsRefuellingAllowed = previousGameState.PitData.IsRefuellingAllowed;
                            currentGameState.OpponentData = previousGameState.OpponentData;
                            currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                            currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                        }
                        currentGameState.SessionData.SessionStartTime = currentGameState.Now;

                        Console.WriteLine("Just gone green, session details...");

                        Console.WriteLine("SessionType " + currentGameState.SessionData.SessionType);
                        Console.WriteLine("SessionPhase " + currentGameState.SessionData.SessionPhase);
                        Console.WriteLine("EventIndex " + currentGameState.SessionData.EventIndex);
                        Console.WriteLine("SessionIteration " + currentGameState.SessionData.SessionIteration);
                        Console.WriteLine("HasMandatoryPitStop " + currentGameState.PitData.HasMandatoryPitStop);
                        Console.WriteLine("PitWindowStart " + currentGameState.PitData.PitWindowStart);
                        Console.WriteLine("PitWindowEnd " + currentGameState.PitData.PitWindowEnd);
                        Console.WriteLine("NumCarsAtStartOfSession " + currentGameState.SessionData.NumCarsAtStartOfSession);
                        Console.WriteLine("SessionNumberOfLaps " + currentGameState.SessionData.SessionNumberOfLaps);
                        Console.WriteLine("SessionRunTime " + currentGameState.SessionData.SessionTotalRunTime);
                        Console.WriteLine("SessionStartPosition " + currentGameState.SessionData.SessionStartPosition);
                        Console.WriteLine("SessionStartTime " + currentGameState.SessionData.SessionStartTime);
                        String trackName = currentGameState.SessionData.TrackDefinition == null ? "unknown" : currentGameState.SessionData.TrackDefinition.name;
                        Console.WriteLine("TrackName " + trackName);
                    }
                }
                if (!justGoneGreen && previousGameState != null)
                {
                    //Console.WriteLine("regular update, session type = " + currentGameState.SessionData.SessionType + " phase = " + currentGameState.SessionData.SessionPhase);

                    currentGameState.SessionData.SessionStartTime = previousGameState.SessionData.SessionStartTime;
                    currentGameState.SessionData.SessionTotalRunTime = previousGameState.SessionData.SessionTotalRunTime;
                    currentGameState.SessionData.SessionNumberOfLaps = previousGameState.SessionData.SessionNumberOfLaps;
                    currentGameState.SessionData.SessionHasFixedTime = previousGameState.SessionData.SessionHasFixedTime;
                    currentGameState.SessionData.HasExtraLap = previousGameState.SessionData.HasExtraLap;
                    currentGameState.SessionData.SessionStartPosition = previousGameState.SessionData.SessionStartPosition;
                    currentGameState.SessionData.NumCarsAtStartOfSession = previousGameState.SessionData.NumCarsAtStartOfSession;
                    currentGameState.SessionData.EventIndex = previousGameState.SessionData.EventIndex;
                    currentGameState.SessionData.SessionIteration = previousGameState.SessionData.SessionIteration;
                    currentGameState.SessionData.PositionAtStartOfCurrentLap = previousGameState.SessionData.PositionAtStartOfCurrentLap;
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
                    currentGameState.PitData.NumPitStops = previousGameState.PitData.NumPitStops;
                    currentGameState.SessionData.TrackDefinition = previousGameState.SessionData.TrackDefinition;
                    currentGameState.SessionData.formattedPlayerLapTimes = previousGameState.SessionData.formattedPlayerLapTimes;
                    currentGameState.SessionData.PlayerLapTimeSessionBest = previousGameState.SessionData.PlayerLapTimeSessionBest;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestOverall = previousGameState.SessionData.OpponentsLapTimeSessionBestOverall;
                    currentGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass = previousGameState.SessionData.OpponentsLapTimeSessionBestPlayerClass;
                    currentGameState.carClass = previousGameState.carClass;
                    currentGameState.SessionData.PlayerClassSessionBestLapTimeByTyre = previousGameState.SessionData.PlayerClassSessionBestLapTimeByTyre;
                    currentGameState.SessionData.PlayerBestLapTimeByTyre = previousGameState.SessionData.PlayerBestLapTimeByTyre;
                    currentGameState.SessionData.DriverRawName = previousGameState.SessionData.DriverRawName;
                    currentGameState.SessionData.SessionTimesAtEndOfSectors = previousGameState.SessionData.SessionTimesAtEndOfSectors;
                    currentGameState.SessionData.LapTimePreviousEstimateForInvalidLap = previousGameState.SessionData.LapTimePreviousEstimateForInvalidLap;
                    currentGameState.SessionData.OverallSessionBestLapTime = previousGameState.SessionData.OverallSessionBestLapTime;
                    currentGameState.SessionData.PlayerClassSessionBestLapTime = previousGameState.SessionData.PlayerClassSessionBestLapTime;
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
                    currentGameState.SessionData.trackLandmarksTiming = previousGameState.SessionData.trackLandmarksTiming;

                    currentGameState.FlagData.useImprovisedIncidentCalling = previousGameState.FlagData.useImprovisedIncidentCalling;
                }
            }

            return currentGameState;
           

        }
        private static Dictionary<String, SessionType> sessionTypeMap = new Dictionary<String, SessionType>()
        {
            {"Offline Testing", SessionType.Practice},
            {"Practice", SessionType.Practice},
            {"Open Qualify", SessionType.Qualify},
            {"Lone Qualify", SessionType.Qualify},
            {"Race", SessionType.Race}
        };

        public SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            String sessionString = (String)memoryMappedFileStruct;
            //Console.WriteLine(sessionString);
            if (sessionTypeMap.ContainsKey(sessionString))
            {
                return sessionTypeMap[sessionString];
            }
            return SessionType.Unavailable;
        }

        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionState sessionState, SessionType currentSessionType, float lastSessionRunningTime, float thisSessionRunningTime, int previousLapsCompleted, int currentLapsCompleted, SessionFlags sessionFlags)
        {
            /*
                Invalid = 0,
                GetInCar = 1,
                Warmup = 2,
                ParadeLaps = 3,
                Racing = 4,
                Checkered = 5,
                CoolDown = 6,
            */
            if (currentSessionType == SessionType.Practice)
            {
                //Console.WriteLine("Practice");
                if (sessionState.HasFlag(SessionState.CoolDown))
                {
                    return SessionPhase.Finished;
                }
                else if (sessionState.HasFlag(SessionState.Checkered))
                {
                    return SessionPhase.Checkered;
                }
                else if (lastSessionPhase.HasFlag(SessionPhase.Unavailable) && !lastSessionPhase.HasFlag(SessionPhase.Countdown))
                {
                    return SessionPhase.Countdown;
                }

                return SessionPhase.Green;
            }
            else if (currentSessionType.HasFlag(SessionType.Race))
            {
                if (sessionState.HasFlag(SessionState.Checkered) || sessionState.HasFlag(SessionState.CoolDown))
                {
                    if (lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow)
                    {
                        if (previousLapsCompleted != currentLapsCompleted || sessionState.HasFlag(SessionState.CoolDown))
                        {
                            Console.WriteLine("finished - completed " + currentLapsCompleted + " laps (was " + previousLapsCompleted + "), session running time = " +
                                thisSessionRunningTime);
                            return SessionPhase.Finished;
                        }
                    }
                }
                else if (sessionFlags.HasFlag(SessionFlags.startReady) || sessionFlags.HasFlag(SessionFlags.startSet))
                {
                    // don't allow a transition to Countdown if the game time has increased
                    //if (lastSessionRunningTime < thisSessionRunningTime)
                    //{
                    return SessionPhase.Countdown;
                    //}
                }
                else if (sessionState.HasFlag(SessionState.ParadeLaps))
                {
                    return SessionPhase.Formation;
                }
                else if (sessionState.HasFlag(SessionState.Racing) || sessionFlags.HasFlag(SessionFlags.green))
                {
                    return SessionPhase.Green;
                }
            }
            return lastSessionPhase;
        }

        private OpponentData createOpponentData(Car opponentCar, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass)
        {
            if (loadDriverName && CrewChief.enableDriverNames)
            {
                speechRecogniser.addNewOpponentName(driverName);
            }
            OpponentData opponentData = new OpponentData();
            opponentData.DriverRawName = driverName;
            opponentData.Position = opponentCar.OfficialPostion;
            opponentData.UnFilteredPosition = opponentData.Position;
            opponentData.CompletedLaps = opponentCar.LapCompleated;
            opponentData.CurrentSectorNumber = opponentCar.LapSector.Sector;
            //opponentData.WorldPosition = new float[] { opponentCar.Position.X, opponentCar.Position.Z };
            opponentData.DistanceRoundTrack = opponentCar.DistanceRoundTrack;
            //opponentData.CarClass = CarData.getCarClassForRaceRoomId(opponentCar.DriverInfo.ClassId);
            //opponentData.CurrentTyres = mapToTyreType(opponentCar.TireTypeFront, opponentCar.TireSubTypeFront,
                //opponentCar.TireTypeRear, opponentCar.TireSubTypeRear, playerCarClass);
            //Console.WriteLine("New driver " + driverName + " is using car class " +
                //opponentData.CarClass.getClassIdentifier() + " (class ID " + opponentCar.DriverInfo.ClassId + ") with tyres " + opponentData.CurrentTyres);
            //opponentData.TyreChangesByLap[0] = opponentData.CurrentTyres;
            return opponentData;
        }
        public static double ParseTrackLength(string value)
        {
            // value = "6.93 km"
            double length = 0;

            var indexOfKm = value.IndexOf("km");
            if (indexOfKm > 0) value = value.Substring(0, indexOfKm);

            if (double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out length))
            {
                return length;
            }
            return 0;
        }

        public static int ParseInt(string value, int @default = 0)
        {
            int val;
            if (int.TryParse(value, out val)) return val;
            return @default;
        }

        public static float ParseFloat(string value, float @default = 0f)
        {
            float val;
            if (float.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowLeadingSign | NumberStyles.AllowTrailingWhite,
                CultureInfo.InvariantCulture, out val)) return val;
            return @default;
        }

        public static double ParseSec(string value)
        {
            // value = "600.00 sec"
            double length = 0;

            var indexOfSec = value.IndexOf(" sec");
            if (indexOfSec > 0) value = value.Substring(0, indexOfSec);

            if (double.TryParse(value, NumberStyles.AllowDecimalPoint | NumberStyles.AllowTrailingWhite, CultureInfo.InvariantCulture, out length))
            {
                return length;
            }
            return 0;
        }
    }

}
