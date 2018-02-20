using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using CrewChiefV4.GameState;
using CrewChiefV4.Events;
using System.Diagnostics;

/**
 * Maps memory mapped file to a local game-agnostic representation.
 * */
namespace CrewChiefV4.F1_2017
{
    class F12017GameStateMapper : GameStateMapper
    {
        public F12017GameStateMapper()
        {
        }

        public override void versionCheck(Object memoryMappedFileStruct)
        {
            // no version data in the stream so this is a no-op
        }

        public override GameStateData mapToGameStateData(Object structWrapper, GameStateData previousGameState)
        {
            F12017UDPreader.F12017StructWrapper wrapper = (F12017UDPreader.F12017StructWrapper)structWrapper;
            long ticks = wrapper.ticksWhenRead;
            UDPPacket rawData = wrapper.data;

            // TODO: one or two minor things here ;)
            return new GameStateData(ticks);
        }
        
        public override SessionType mapToSessionType(Object memoryMappedFileStruct)
        {
            return SessionType.Unavailable;
        }
            
        private PitWindow mapToPitWindow(GameStateData currentGameState, uint pitSchedule, uint pitMode)
        {
            return PitWindow.Unavailable;
        }
    }
}
