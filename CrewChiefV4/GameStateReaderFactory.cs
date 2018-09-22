using CrewChiefV4.GameState;
using CrewChiefV4.PCars;
using CrewChiefV4.RaceRoom;
using CrewChiefV4.rFactor1;
using CrewChiefV4.assetto;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.rFactor2;
using CrewChiefV4.iRacing;
using CrewChiefV4.PCars2;
using CrewChiefV4.F1_2018;
using CrewChiefV4.ACC;
namespace CrewChiefV4
{
    class GameStateReaderFactory
    {
        private static GameStateReaderFactory INSTANCE = new GameStateReaderFactory();

        private PCarsUDPreader pcarsUDPreader;
        private PCars2UDPreader pcars2UDPreader;
        private PCarsSharedMemoryReader pcarsSharedMemoryReader;
        private PCars2SharedMemoryReader pcars2SharedMemoryReader;
        private R3ESharedMemoryReader r3eSharedMemoryReader;
        private RF1SharedMemoryReader rf1SharedMemoryReader;
        private RF2SharedMemoryReader rf2SharedMemoryReader;
        private ACSSharedMemoryReader ascSharedMemoryReader;
        private iRacingSharedMemoryReader iracingSharedMemoryReader;
        private F12018UDPreader f12018UDPReader;
        private ACCSharedMemoryReader accSharedMemoryReader;

        private PCarsGameStateMapper pcarsGameStateMapper;
        private PCars2GameStateMapper pcars2GameStateMapper;
        private R3EGameStateMapper r3eGameStateMapper;
        private RF1GameStateMapper rf1GameStateMapper;
        private RF2GameStateMapper rf2GameStateMapper;
        private ACSGameStateMapper ascGameStateMapper;
        private iRacingGameStateMapper iracingGameStateMapper;
        private F12018GameStateMapper f12018GameStateMapper;
        private ACCGameStateMapper accGameStateMapper;

        public static GameStateReaderFactory getInstance()
        {
            return INSTANCE;
        }

        public void clearCachedReaders()
        {
            lock (this)
            {
                 pcarsUDPreader = null;
                 pcars2UDPreader = null;
                 pcarsSharedMemoryReader = null;
                 pcars2SharedMemoryReader = null;
                 r3eSharedMemoryReader = null;
                 rf1SharedMemoryReader = null;
                 ascSharedMemoryReader = null;
                 rf2SharedMemoryReader = null;
                 iracingSharedMemoryReader = null;
                 f12018UDPReader = null;
                 accSharedMemoryReader = null;
            }
        }

        public GameDataReader getGameStateReader(GameDefinition gameDefinition)
        {
            lock (this)
            {
                switch (gameDefinition.gameEnum)
                {
                    case GameEnum.PCARS_NETWORK:
                        if (pcarsUDPreader == null)
                        {
                            pcarsUDPreader = new PCarsUDPreader();
                        }
                        return pcarsUDPreader;
                    case GameEnum.PCARS2_NETWORK:
                        if (pcars2UDPreader == null)
                        {
                            pcars2UDPreader = new PCars2UDPreader();
                        }
                        return pcars2UDPreader;
                    case GameEnum.PCARS_32BIT:
                    case GameEnum.PCARS_64BIT:                    
                        if (pcarsSharedMemoryReader == null)
                        {
                            pcarsSharedMemoryReader = new PCarsSharedMemoryReader();
                        }
                        return pcarsSharedMemoryReader;
                    case GameEnum.PCARS2:
                        if (pcars2SharedMemoryReader == null)
                        {
                            pcars2SharedMemoryReader = new PCars2SharedMemoryReader();
                        }
                        return pcars2SharedMemoryReader;
                    case GameEnum.RACE_ROOM:
                        if (r3eSharedMemoryReader == null)
                        {
                            r3eSharedMemoryReader = new R3ESharedMemoryReader();
                        }
                        return r3eSharedMemoryReader;
                    case GameEnum.RF1:
                        if (rf1SharedMemoryReader == null)
                        {
                            rf1SharedMemoryReader = new RF1SharedMemoryReader();
                        }
                        return rf1SharedMemoryReader;
                    case GameEnum.ASSETTO_64BIT:
                    case GameEnum.ASSETTO_32BIT:
                        if (ascSharedMemoryReader == null)
                        {
                            ascSharedMemoryReader = new ACSSharedMemoryReader();
                        }
                        return ascSharedMemoryReader;
                    case GameEnum.RF2_64BIT:
                        if (rf2SharedMemoryReader == null)
                        {
                            rf2SharedMemoryReader = new RF2SharedMemoryReader();
                        }
                        return rf2SharedMemoryReader;
                    case GameEnum.IRACING:
                        if (iracingSharedMemoryReader == null)
                        {
                            iracingSharedMemoryReader = new iRacingSharedMemoryReader();
                        }
                        return iracingSharedMemoryReader;
                    case GameEnum.F1_2018:
                        if (f12018UDPReader == null)
                        {
                            f12018UDPReader = new F12018UDPreader();
                        }
                        return f12018UDPReader;
                    case GameEnum.ACC:
                        if (accSharedMemoryReader == null)
                        {
                            accSharedMemoryReader = new ACCSharedMemoryReader();
                        }
                        return accSharedMemoryReader;
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
                    case GameEnum.PCARS2_NETWORK:
                    case GameEnum.PCARS2:
                        if (pcars2GameStateMapper == null)
                        {
                            pcars2GameStateMapper = new PCars2GameStateMapper();
                        }
                        return pcars2GameStateMapper;
                    case GameEnum.RACE_ROOM:
                        if (r3eGameStateMapper == null)
                        {
                            r3eGameStateMapper = new R3EGameStateMapper();
                        }
                        return r3eGameStateMapper;
                    case GameEnum.RF1:
                        if (rf1GameStateMapper == null)
                        {
                            rf1GameStateMapper = new RF1GameStateMapper();
                        }
                        return rf1GameStateMapper;
                    case GameEnum.ASSETTO_64BIT:
                    case GameEnum.ASSETTO_32BIT:
                        if (ascGameStateMapper == null)
                        {
                            ascGameStateMapper = new ACSGameStateMapper();
                        }
                        return ascGameStateMapper;
                    case GameEnum.RF2_64BIT:
                        if (rf2GameStateMapper == null)
                        {
                            rf2GameStateMapper = new RF2GameStateMapper();
                        }
                        return rf2GameStateMapper;
                    case GameEnum.IRACING:
                        if (iracingGameStateMapper == null)
                        {
                            iracingGameStateMapper = new iRacingGameStateMapper();
                        }
                        return iracingGameStateMapper;
                    case GameEnum.F1_2018:
                        if (f12018GameStateMapper == null)
                        {
                            f12018GameStateMapper = new F12018GameStateMapper();
                        }
                        return f12018GameStateMapper;
                    case GameEnum.ACC:
                        if (accGameStateMapper == null)
                        {
                            accGameStateMapper = new ACCGameStateMapper();
                        }
                        return accGameStateMapper;
                        
                }
            }
            return null;
        }
    }
}
