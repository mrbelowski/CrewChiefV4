using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.GameState
{
    interface GameStateMapper
    {
        /** May return null if the game state raw data is considered invalid */
        GameStateData mapToGameStateData(Object memoryMappedFileStruct, GameStateData previousGameState);

        void versionCheck(Object memoryMappedFileStruct);
        
        SessionType mapToSessionType(Object memoryMappedFileStruct);

        void setSpeechRecogniser(SpeechRecogniser speechRecogniser);
    }
}
