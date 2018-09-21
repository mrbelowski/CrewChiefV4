using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.GameState;
using CrewChiefV4.ACC;
using CrewChiefV4.ACC.Data;
namespace CrewChiefV4.ACC
{
    class ACCGameStateMapper : GameStateMapper
    {
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
        RaceSessionType previousRaceSessionType = RaceSessionType.RaceSessionType_Max;
        RaceSessionPhase previousRaceSessionPhase = RaceSessionPhase.RaceSessionPhase_Max;
        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            ACCSharedMemoryReader.ACCStructWrapper wrapper = (ACCSharedMemoryReader.ACCStructWrapper)structWrapper;
            long ticks = wrapper.ticksWhenRead;
            ACCSharedMemoryData data = wrapper.data;
            if (!previousRaceSessionType.Equals(data.sessionData.currentSessionType))
            {
                Console.WriteLine("physicsTime " + data.sessionData.physicsTime + " size of SessionData " + System.Runtime.InteropServices.Marshal.SizeOf(typeof(CrewChiefV4.ACC.Data.SessionData)) +
                    " size of float " + sizeof(float));

                Console.WriteLine(System.Runtime.InteropServices.Marshal.OffsetOf(typeof(CrewChiefV4.ACC.Data.SessionData), "currentSessionPhase"));
                previousRaceSessionType = data.sessionData.currentSessionType;
                Console.WriteLine("currentSessionType " + data.sessionData.currentSessionType);
            }
            if (!previousRaceSessionPhase.Equals(data.sessionData.currentSessionPhase))
            {
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
