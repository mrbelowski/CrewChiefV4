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

        RaceSessionType previousRaceSessionType = RaceSessionType.RaceSessionType_Max;
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
            //Console.WriteLine("OffsetOf currentSessionIndex " + Marshal.OffsetOf(typeof(CrewChiefV4.ACC.Data.SessionData), "currentSessionIndex").ToString("d"));
            Console.WriteLine("sizeOf Driver " + Marshal.SizeOf(typeof(CrewChiefV4.ACC.Data.Driver)).ToString("X"));
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
            if(!data.isReady)
            {
                return null;
            }
            if (!previousRaceSessionType.Equals(data.sessionData.currentSessionType))
            {
                PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
                PrintProperties<CrewChiefV4.ACC.Data.Track>(data.track);
                PrintProperties<CrewChiefV4.ACC.Data.WeatherStatus>(data.track.weatherState);

                //Console.WriteLine("physicsTime " + data.sessionData.physicsTime);
                previousRaceSessionType = data.sessionData.currentSessionType;
                Console.WriteLine("currentSessionType " + data.sessionData.currentSessionType);

            }
            if (!previousRaceSessionPhase.Equals(data.sessionData.currentSessionPhase))            
            {
                /*PrintProperties<CrewChiefV4.ACC.Data.ACCSessionData>(data.sessionData);
                for (int i = 0; i < data.opponentDriverCount; i++)
                {
                    PrintProperties<CrewChiefV4.ACC.Data.Driver>(data.opponentDrivers[i]);
                }
                for (int i = 0; i < data.marshals.marshalCount; i++)
                {
                    PrintProperties<CrewChiefV4.ACC.Data.ACCMarshal>(data.marshals.marshals[i]);
                }*/
                previousRaceSessionPhase = data.sessionData.currentSessionPhase;
                Console.WriteLine("currentSessionPhase " + data.sessionData.currentSessionPhase);
            }
            SessionType previousSessionType = SessionType.Unavailable;
            float previousSessionRunningTime = -1;
            if(previousGameState != null)
            {
                previousSessionType = previousGameState.SessionData.SessionType;
                previousSessionRunningTime = previousGameState.SessionData.SessionRunningTime;
            }
            //test commit
            SessionType currentSessionType = mapToSessionType(data.sessionData.currentSessionType);
            currentGameState.SessionData.SessionType = currentSessionType;
            currentGameState.SessionData.SessionRunningTime = (float)TimeSpan.FromMilliseconds((data.sessionData.physicsTime - data.sessionData.sessionStartTimeStamp)).TotalSeconds;          
            currentGameState.SessionData.SessionTotalRunTime = (float)TimeSpan.FromMilliseconds((data.sessionData.sessionEndTime - data.sessionData.sessionStartTime)).TotalSeconds;

            if (currentSessionType != SessionType.Unavailable && (previousSessionType != currentSessionType ||
                currentGameState.SessionData.SessionRunningTime < previousSessionRunningTime)) // session restarted.
            {
                //currentGameState.SessionData.IsNewSession = true;

                //Console.WriteLine("currentSessionPhase " + currentGameState.SessionData.SessionRunningTime);
                
                
                //currentGameState.SessionData.SessionStartTime = currentGameState.Now;
                
            }
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
        private SessionPhase mapToSessionPhase(RaceSessionPhase currentRaceSessionPhase, RaceSessionPhase previousRaceSessionPhase,
            SessionPhase previousPhase, SessionType currentSessionType)
        {
            /*public enum  RaceSessionPhase  : byte
	        {
		        StartingUI = 0,
		        PreFormationTime = 1,
		        FormationTime = 2,
		        PreSessionTime = 3,
		        SessionTime = 4,
		        SessionOverTime = 5,
		        PostSessionTime = 6,
		        ResultUI = 7,
                RaceSessionPhase_Max = 8, 
	        };*/
            /*if (previousRaceSessionPhase == RaceSessionPhase.StartingUI || previousPhase == SessionPhase.Unavailable)
            {
                switch(currentSessionType)
                {
                    case SessionType.Practice:
                    case SessionType.HotLap:
                    case SessionType.Qualify:
                }
                if(currentRaceSessionPhase == RaceSessionPhase.FormationTime || currentRaceSessionPhase )
                {
                    return SessionPhase.
                }
            }*/
            return SessionPhase.Green;
        }

    }
}
