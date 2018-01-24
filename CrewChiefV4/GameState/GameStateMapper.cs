using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.GameState
{
    public abstract class GameStateMapper
    {
        protected SpeechRecogniser speechRecogniser;

        /** May return null if the game state raw data is considered invalid */
        public abstract GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState);

        public abstract void versionCheck(Object memoryMappedFileStruct);

        public abstract SessionType mapToSessionType(Object memoryMappedFileStruct);

        public virtual void setSpeechRecogniser(SpeechRecogniser speechRecogniser)
        {
            this.speechRecogniser = speechRecogniser;
        }
    }
}
