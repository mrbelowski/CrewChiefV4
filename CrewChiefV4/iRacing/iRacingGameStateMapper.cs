using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;


namespace CrewChiefV4.iRacing
{
    class iRacingGameStateMapper : GameStateMapper
    {
        public static String playerName = null;
        private SpeechRecogniser speechRecogniser;
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
            CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper wrapper = (CrewChiefV4.iRacing.iRacingSharedMemoryReader.iRacingStructWrapper)memoryMappedFileStruct;
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            Sim shared = wrapper.data;
    
            if(memoryMappedFileStruct == null)
            {
                return previousGameState;
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

            currentGameState.SessionData.SessionType = mapToSessionType(shared.SessionData.SessionType);
            currentGameState.SessionData.SessionRunningTime = (float)shared.Telemetry.SessionTime.Value;
            currentGameState.SessionData.SessionTimeRemaining = (float)shared.Telemetry.SessionTimeRemain.Value;


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
        /*
                private SessionPhase mapToSessionPhase(SessionPhase lastSessionPhase, SessionState sessionState, 
                    SessionType currentSessionType, float lastSessionRunningTime, float thisSessionRunningTime, 
                    int previousLapsCompleted, int laps, SessionFlags sessionFlags, bool isInPit)
                {
            
                        Invalid = 0,
                        GetInCar = 1,
                        Warmup = 2,
                        ParadeLaps = 3,
                        Racing = 4,
                        Checkered = 5,
                        CoolDown = 6,
            
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
                        else if (isInPit && laps <= 0)
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
                                if (previousLapsCompleted != laps || sessionState.HasFlag(SessionState.CoolDown))
                                {
                                    Console.WriteLine("finished - completed " + laps + " laps (was " + previousLapsCompleted + "), session running time = " +
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

                private OpponentData createOpponentData(Driver opponentCar, String driverName, Boolean loadDriverName, CarData.CarClassEnum playerCarClass)
                {
                    if (loadDriverName && CrewChief.enableDriverNames)
                    {
                        speechRecogniser.addNewOpponentName(driverName);
                    }
                    OpponentData opponentData = new OpponentData();
                    opponentData.DriverRawName = driverName;
                    opponentData.Position = opponentCar.Live.Position;
                    opponentData.UnFilteredPosition = opponentData.Position;
                    //opponentData.CompletedLaps = opponentCar.LapCompleated;
                    //opponentData.CurrentSectorNumber = opponentCar.LapSector.Sector;
                    //opponentData.WorldPosition = new float[] { opponentCar.Position.X, opponentCar.Position.Z };
                    //opponentData.DistanceRoundTrack = opponentCar.DistanceRoundTrack;
                    //opponentData.CarClass = CarData.getCarClassForRaceRoomId(opponentCar.DriverInfo.ClassId);
                    //opponentData.CurrentTyres = mapToTyreType(opponentCar.TireTypeFront, opponentCar.TireSubTypeFront,
                        //opponentCar.TireTypeRear, opponentCar.TireSubTypeRear, playerCarClass);
                    //Console.WriteLine("New driver " + driverName + " is using car class " +
                        //opponentData.CarClass.getClassIdentifier() + " (class ID " + opponentCar.DriverInfo.ClassId + ") with tyres " + opponentData.CurrentTyres);
                    //opponentData.TyreChangesByLap[0] = opponentData.CurrentTyres;
                    return opponentData;
                }
 * */

    }
}
