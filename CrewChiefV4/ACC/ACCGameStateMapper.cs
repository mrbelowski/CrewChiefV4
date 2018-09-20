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
        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            ACCSharedMemoryReader.ACCStructWrapper wrapper = (ACCSharedMemoryReader.ACCStructWrapper)structWrapper;
            long ticks = wrapper.ticksWhenRead;

            // TODO: one or two minor things here ;)
            return new GameStateData(ticks);
        }

        private PitWindow mapToPitWindow(GameStateData currentGameState, uint pitSchedule, uint pitMode)
        {
            return PitWindow.Unavailable;
        }
    }
}
