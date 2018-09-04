using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;
using F1UdpNet;

namespace CrewChiefV4.F1_2018
{
    public class F12018StructWrapper
    {
        public long ticksWhenRead = 0;

        public PacketCarSetupData packetCarSetupData;
        public PacketCarStatusData packetCarStatusData;
        public PacketCarTelemetryData packetCarTelemetryData;
        public PacketEventData packetEventData;
        public PacketLapData packetLapData;
        public PacketMotionData packetMotionData;
        public PacketParticipantsData packetParticipantsData;
        public PacketSessionData packetSessionData;

        public F12018StructWrapper CreateCopy(long ticksWhenCopied, Boolean forSpotter)
        {
            F12018StructWrapper copy = new F12018StructWrapper();
            copy.ticksWhenRead = ticksWhenCopied;
            copy.packetLapData = this.packetLapData;
            copy.packetSessionData = this.packetSessionData;
            copy.packetMotionData = this.packetMotionData;
            copy.packetCarTelemetryData = this.packetCarTelemetryData;

            if (!forSpotter)
            {
                copy.packetCarSetupData = this.packetCarSetupData;
                copy.packetCarStatusData = this.packetCarStatusData;
                copy.packetEventData = this.packetEventData;
                copy.packetParticipantsData = this.packetParticipantsData;
            }
            return copy;
        }
    }
}