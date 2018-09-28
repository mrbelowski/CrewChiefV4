using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.GameState;
using CrewChiefV4.ACC;
using CrewChiefV4.ACC.Data;
using System.Runtime.InteropServices;
namespace CrewChiefV4.ACC
{
    class ACCGameStateMapper : GameStateMapper
    {

        RaceSessionType previousRaceSessionType = RaceSessionType.FreePractice1;
        RaceSessionPhase previousRaceSessionPhase = RaceSessionPhase.RaceSessionPhase_Max;

        private void PrintProperties<T>(T myObj)
        {
            /*foreach (var prop in myObj.GetType().GetProperties())
            {
                Console.WriteLine(Marshal.OffsetOf(typeof(T), prop.Name).ToString("d") + " prop " + prop.Name + ": " + prop.GetValue(myObj, null));
            }*/
            Console.WriteLine("sizeOf: " + myObj.ToString() + " " + Marshal.SizeOf(typeof(T)).ToString("X"));
            foreach (var field in myObj.GetType().GetFields())
            {
                Console.WriteLine("0x" + Marshal.OffsetOf(typeof(T), field.Name).ToString("X") + " " + field.Name + ": " + field.GetValue(myObj));
            }
        }
        public ACCGameStateMapper()
        {

        }

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no version data in the stream so this is a no-op

        }
        public override void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            speechRecogniser.addiRacingSpeechRecogniser();
            this.speechRecogniser = speechRecogniser;
        }
        public float mapToFloatTime(int time)
        {
            TimeSpan ts = TimeSpan.FromTicks(time);
            return (float)ts.TotalMilliseconds * 10;
        }
        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            ACCSharedMemoryReader.ACCStructWrapper wrapper = (ACCSharedMemoryReader.ACCStructWrapper)structWrapper;            
            GameStateData currentGameState = new GameStateData(wrapper.ticksWhenRead);
            ACCSharedMemoryData data = wrapper.data;
            
            if(data.isReady != 1)
            {
                return previousGameState;
            }
            if (!previousRaceSessionType.Equals(data.sessionData.currentSessionType))
            {
                
                PrintProperties<CrewChiefV4.ACC.Data.Track>(data.track);
                PrintProperties<CrewChiefV4.ACC.Data.WeatherStatus>(data.track.weatherState);
                //PrintProperties<CrewChiefV4.ACC.Data.SPageFilePhysics>(wrapper.physicsData);
                //Console.WriteLine("physicsTime " + data.sessionData.physicsTime);
                previousRaceSessionType = data.sessionData.currentSessionType;
                Console.WriteLine("currentSessionType " + data.sessionData.currentSessionType);

            }
            if (!previousRaceSessionPhase.Equals(data.sessionData.currentSessionPhase))            
            {
                PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
                Console.WriteLine("previousRaceSessionPhase " + previousRaceSessionPhase );
                Console.WriteLine("currentRaceSessionPhase " + data.sessionData.currentSessionPhase);
            }
            
            //Console.WriteLine("tyre temp" + wrapper.physicsData.tyreTempM[0]);
            SessionType previousSessionType = SessionType.Unavailable;
            SessionPhase previousSessionPhase = SessionPhase.Unavailable;
            float previousSessionRunningTime = -1;
            if(previousGameState != null)
            {
                previousSessionType = previousGameState.SessionData.SessionType;
                previousSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
                previousSessionPhase = previousGameState.SessionData.SessionPhase;
            }
            //test commit
            SessionType currentSessionType = mapToSessionType(data.sessionData.currentSessionType);
            currentGameState.SessionData.SessionType = currentSessionType;
            SessionPhase currentSessioPhase = mapToSessionPhase(data.sessionData.currentSessionPhase, currentSessionType);
            currentGameState.SessionData.SessionPhase = currentSessioPhase;

            currentGameState.SessionData.SessionRunningTime = (float)TimeSpan.FromMilliseconds((data.sessionData.physicsTime - data.sessionData.sessionStartTimeStamp)).TotalSeconds;
            currentGameState.SessionData.SessionTotalRunTime = (float)data.sessionData.sessionDuration;
            currentGameState.SessionData.SessionTimeRemaining = (float)TimeSpan.FromMilliseconds((data.sessionData.sessionEndTime - data.sessionData.physicsTime)).TotalSeconds;
            
            currentGameState.SessionData.SessionHasFixedTime = true;
            Driver playerDriver = data.playerDriver;
            currentGameState.SessionData.NumCarsOverall = data.driverCount;
            //this still needs fixing
            if (currentSessionType != SessionType.Unavailable && 
                (previousRaceSessionPhase == RaceSessionPhase.StartingUI && data.sessionData.currentSessionPhase == RaceSessionPhase.PreFormationTime && currentSessionType == SessionType.Race))
            {
                currentGameState.SessionData.IsNewSession = true;
                PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
                //Console.WriteLine("New session, trigger data:");
                Console.WriteLine("SessionTimeRemaining = " + currentGameState.SessionData.SessionTimeRemaining);
                Console.WriteLine("SessionTotalRunTime = " + currentGameState.SessionData.SessionTotalRunTime);
                Console.WriteLine("SessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                currentGameState.SessionData.NumCarsOverallAtStartOfSession = data.driverCount;
                //Console.WriteLine("currentSessionPhase = " + currentGameState.SessionData.SessionPhase);
                //Console.WriteLine("currentSessionRunningTime = " + currentGameState.SessionData.SessionRunningTime);
                //Console.WriteLine("NumCarsAtStartOfSession = " + currentGameState.SessionData.NumCarsOverallAtStartOfSession);

                //currentGameState.SessionData.DriverRawName = playerName;
                currentGameState.OpponentData.Clear();
                currentGameState.SessionData.PlayerLapData.Clear();
                currentGameState.SessionData.SessionStartTime = currentGameState.Now;            
            }
            else
            {
                if (previousSessionPhase != currentGameState.SessionData.SessionPhase)
                {

                }
            }
            previousRaceSessionPhase = data.sessionData.currentSessionPhase;
            //currentGameState.SessionData.SessionPhase = SessionPhase.Green;
            return currentGameState;
        }

        private SessionType mapToSessionType(RaceSessionType sessionType)
        {
            switch (sessionType)
            {
                case RaceSessionType.FreePractice1:
                case RaceSessionType.FreePractice2:
                    return SessionType.Practice;
                case RaceSessionType.Hotstint:
                case RaceSessionType.Hotlap:
                    return SessionType.HotLap;
                case RaceSessionType.PreQualifying:
                case RaceSessionType.Qualifying:
                case RaceSessionType.Qualifying1:
                case RaceSessionType.Qualifying3:
                case RaceSessionType.Qualifying4:
                case RaceSessionType.Superpole:
                case RaceSessionType.HotlapSuperpole:
                    return SessionType.Qualify;
                case RaceSessionType.Race:
                    return SessionType.Race;        
            }
            return SessionType.Unavailable;
        }
        private SessionPhase mapToSessionPhase(RaceSessionPhase currentRaceSessionPhase, SessionType currentSessionType)
        {
            switch(currentSessionType)
            {
                case SessionType.Practice:
                case SessionType.HotLap:
                case SessionType.Qualify:
                        return SessionPhase.Green;
                case SessionType.Race:
                    {
                        switch(currentRaceSessionPhase)
                        {
                            case RaceSessionPhase.StartingUI:
                                return SessionPhase.Garage;
                            case RaceSessionPhase.PreFormationTime:
                            case RaceSessionPhase.FormationTime: //here we are in our car on the grid waition to roll
                                return SessionPhase.Gridwalk;
                            case RaceSessionPhase.PreSessionTime:                            
                                return SessionPhase.Formation;
                            case RaceSessionPhase.SessionTime:
                                return SessionPhase.Green;
                            case RaceSessionPhase.SessionOverTime:
                                return SessionPhase.Checkered;
                            case RaceSessionPhase.ResultUI:
                            case RaceSessionPhase.PostSessionTime:
                                return SessionPhase.Finished;
                            default:
                                return SessionPhase.Unavailable;
                        }
                    }
                default:
                    return SessionPhase.Unavailable;
            }
        }
    }
}
