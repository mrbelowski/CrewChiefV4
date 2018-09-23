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

namespace CrewChiefV4
{
    class GameStateReaderFactory
    {
        private static GameStateReaderFactory INSTANCE = new GameStateReaderFactory();

        // the Reader objects may be used by other Threads, so the factory must cache them and return the same instance
        // when called.
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

        public static GameStateReaderFactory getInstance()
        {
            return INSTANCE;
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
                }
            }
            return null;
        }

        public GameStateMapper getGameStateMapper(GameDefinition gameDefinition)
        {
            switch (gameDefinition.gameEnum)
            {
                case GameEnum.PCARS_NETWORK:
                case GameEnum.PCARS_32BIT:
                case GameEnum.PCARS_64BIT:
                    return new PCarsGameStateMapper();
                case GameEnum.PCARS2_NETWORK:
                case GameEnum.PCARS2:
                    return new PCars2GameStateMapper();
                case GameEnum.RACE_ROOM:
                    return new R3EGameStateMapper();
                case GameEnum.RF1:
                    return new RF1GameStateMapper();
                case GameEnum.ASSETTO_64BIT:
                case GameEnum.ASSETTO_32BIT:
                    return new ACSGameStateMapper();
                case GameEnum.RF2_64BIT:
                    return new RF2GameStateMapper();
                case GameEnum.IRACING:
                    return new iRacingGameStateMapper();
                case GameEnum.F1_2018:
                    return new F12018GameStateMapper();
                default:
                    Console.WriteLine("No mapper is defined for GameDefinition " + gameDefinition.friendlyName);
                    return null;
            }
        }
    }
}
