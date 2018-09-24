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

        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            ACCSharedMemoryReader.ACCStructWrapper wrapper = (ACCSharedMemoryReader.ACCStructWrapper)structWrapper;            
            long ticks = wrapper.ticksWhenRead;
            ACCSharedMemoryData data = wrapper.data;
            if(!data.isReady)
            {
                return null;
            }
            if (!previousRaceSessionType.Equals(data.sessionData.currentSessionType))
            {
                PrintProperties<CrewChiefV4.ACC.Data.SessionData>(data.sessionData);
                PrintProperties<CrewChiefV4.ACC.Data.Track>(data.track);
                PrintProperties<CrewChiefV4.ACC.Data.WeatherStatus>(data.track.weatherState);

                //Console.WriteLine("physicsTime " + data.sessionData.physicsTime);
                previousRaceSessionType = data.sessionData.currentSessionType;
                Console.WriteLine("currentSessionType " + data.sessionData.currentSessionType);

            }
            if (!previousRaceSessionPhase.Equals(data.sessionData.currentSessionPhase))            
            {
                PrintProperties<CrewChiefV4.ACC.Data.SessionData>(data.sessionData);
                for (int i = 0; i < data.opponentDriverCount; i++)
                {
                    PrintProperties<CrewChiefV4.ACC.Data.Driver>(data.opponentDrivers[i]);
                }
                for (int i = 0; i < data.marshals.marshalCount; i++)
                {
                    PrintProperties<CrewChiefV4.ACC.Data.ACCMarshal>(data.marshals.marshals[i]);
                }
                previousRaceSessionPhase = data.sessionData.currentSessionPhase;
                Console.WriteLine("currentSessionPhase " + data.sessionData.currentSessionPhase);
            }
            // TODO: one or two minor things here ;)
            return new GameStateData(ticks);
        }

        private PitWindow mapToPitWindow(GameStateData currentGameState, uint pitSchedule, uint pitMode)
        {
            return PitWindow.Unavailable;
        }
    }
}
