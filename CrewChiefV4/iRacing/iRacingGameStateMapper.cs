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
                return null;
            }
            CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper wrapper = (CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            DataSample shared = wrapper.data;
            if(!shared.IsConnected)
            {
                return null;
            }

            SessionType sessionType = mapToSessionType(shared.Telemetry.Session.SessionType);
            SessionPhase lastSessionPhase = SessionPhase.Unavailable;
            
            float lastSessionRunningTime = 0;
            if (previousGameState != null)
            {
                lastSessionPhase = previousGameState.SessionData.SessionPhase;
                lastSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
            }

            currentGameState.SessionData.SessionRunningTime = (float)shared.Telemetry.SessionTime;
            //currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain;
            int previousLapsCompleted = previousGameState == null ? 0 : previousGameState.SessionData.CompletedLaps;
            currentGameState.SessionData.SessionPhase = mapToSessionPhase(lastSessionPhase, shared.Telemetry.SessionState,currentGameState.SessionData.SessionType); 
            currentGameState.SessionData.NumCarsAtStartOfSession = shared.SessionData.DriverInfo.CompetingDrivers.Length;
            int sessionNumber = shared.Telemetry.SessionNum;
            iRacingSDK.SessionData._SessionInfo._Sessions currentSessionData  = shared.SessionData.SessionInfo.Sessions[sessionNumber];
            int PlayerCarIdx = shared.Telemetry.PlayerCarIdx;
            Boolean justGoneGreen = false;

            if ((lastSessionPhase != currentGameState.SessionData.SessionPhase && (lastSessionPhase == SessionPhase.Unavailable || lastSessionPhase == SessionPhase.Finished)) ||
                ((lastSessionPhase == SessionPhase.Checkered || lastSessionPhase == SessionPhase.Finished || lastSessionPhase == SessionPhase.Green || lastSessionPhase == SessionPhase.FullCourseYellow) &&
                    currentGameState.SessionData.SessionPhase == SessionPhase.Countdown) 
                    |lastSessionRunningTime > currentGameState.SessionData.SessionRunningTime)
            {
                currentGameState.SessionData.IsNewSession = true;
                Console.WriteLine("New session, trigger data:");
                Console.WriteLine("sessionType = " + sessionType);
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
                    currentGameState.SessionData.SessionTotalRunTime = (float)shared.Telemetry.SessionTimeRemain;
                    currentGameState.SessionData.SessionHasFixedTime = true;
                    Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                }

                timeDelta = new TimeDelta((float)ParseTrackLength(shared.SessionData.WeekendInfo.TrackLength) * 1000f, 20, 64);
                
                foreach(Car car in shared.Telemetry.RaceCars)
                {
                    
                    String driverName = car.Details.Driver.UserName.ToLower();
                    Console.WriteLine("Driver Added: " + driverName);
                    if (car.CarIdx == PlayerCarIdx)
                    {
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
                timeDelta.Update(shared.Telemetry.SessionTime, shared.Telemetry.CarIdxLapDistPct);
                Console.WriteLine("timeDelta:" + TimeDelta.DeltaToString(TimeSpan.FromSeconds(timeDelta.currentlapTime[PlayerCarIdx])));
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
            if (sessionTypeMap.ContainsKey(sessionString))
            {
                return sessionTypeMap[sessionString];
            }
            return SessionType.Unavailable;
        }

        private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionState sessionState,  SessionType currentSessionType )
        {

            return SessionPhase.Garage;
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
