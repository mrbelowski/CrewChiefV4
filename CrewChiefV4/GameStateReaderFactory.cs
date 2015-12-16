using CrewChiefV4.GameState;
using CrewChiefV4.PCars;
using CrewChiefV4.RaceRoom;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4
{
    class GameStateReaderFactory
    {
        private static GameStateReaderFactory INSTANCE = new GameStateReaderFactory();

        private PCarsUDPreader pcarsUDPreader;
        private PCarsSharedMemoryReader pcarsSharedMemoryReader;
        private R3ESharedMemoryReader r3eSharedMemoryReader;

        private PCarsGameStateMapper pcarsGameStateMapper;
        private R3EGameStateMapper r3eGameStateMapper;

        public static GameStateReaderFactory getInstance()
        {
            return INSTANCE;
        }

        public GameDataReader getGameStateReader(GameDefinition gameDefinition)
        {
            lock(this)
            {
                switch (gameDefinition.gameEnum)
                {
                    case GameEnum.PCARS_NETWORK:
                        if (pcarsUDPreader == null) 
                        {
                            pcarsUDPreader = new PCarsUDPreader();
                        }
                        return pcarsUDPreader;
                    case GameEnum.PCARS_32BIT:                        
                    case GameEnum.PCARS_64BIT:
                        if (pcarsSharedMemoryReader == null) 
                        {
                            pcarsSharedMemoryReader = new PCarsSharedMemoryReader();
                        }
                        return pcarsSharedMemoryReader;
                    case GameEnum.RACE_ROOM:
                        if (r3eSharedMemoryReader == null) 
                        {
                            r3eSharedMemoryReader = new R3ESharedMemoryReader();
                        }
                        return r3eSharedMemoryReader;
                }
            }
            return null;
        }

        public GameStateMapper getGameStateMapper(GameDefinition gameDefinition)
        {
            lock (this)
            {
                switch (gameDefinition.gameEnum)
                {
                    case GameEnum.PCARS_NETWORK:                        
                    case GameEnum.PCARS_32BIT:
                    case GameEnum.PCARS_64BIT:
                        if (pcarsGameStateMapper == null)
                        {
                            pcarsGameStateMapper = new PCarsGameStateMapper();
                        }
                        return pcarsGameStateMapper;
                    case GameEnum.RACE_ROOM:
                        if (r3eGameStateMapper == null)
                        {
                            r3eGameStateMapper = new R3EGameStateMapper();
                        }
                        return r3eGameStateMapper;
                }
            }
            return null;
        }
    }
}
