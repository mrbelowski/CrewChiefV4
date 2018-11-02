using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.PCars2
{
    public class PCars2UDPreader : GameDataReader
    {        
        private long packetRateCheckInterval = 1000;
        private long packetCountAtStartOfCurrentRateCheck = 0;
        private long packetCountAtStartOfNextRateCheck = 0;
        private long ticksAtStartOfCurrentPacketRateCheck = 0;
        private float lastPacketRateEstimate = -1;

        private uint sequenceWrapsAt = uint.MaxValue;
        private Boolean strictPacketOrdering = false;    // when false, out-of-order packets are checked before being discarded

        // we only check the telem packets, not the strings...
        private int lastSequenceNumberForTelemPacket = -1;

        private long telemPacketCount = 0;
        private long raceDefinitionPacketCount = 0; 
        private long participantsPacketCount = 0; 
        private long timingsPacketCount = 0; 
        private long gameStatePacketCount = 0; 
        private long weatherStatePacketCount = 0;
        private long vehicleNamesPacketCount = 0;
        private long timeStatsPacketCount = 0;
        private long participantVehicleNamesPacketCount = 0;

        private long inSequenceTelemCount = 0;
        private long discardedTelemCount = 0;
        private long acceptedOutOfSequenceTelemCount = 0;

        private Boolean newSpotterData = true;
        private Boolean running = false;
        private Boolean initialised = false;
        private List<CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper> dataToDump;
        private CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private int udpPort = UserSettings.GetUserSettings().getInt("udp_data_port");

        private pCars2APIStruct workingGameState = new pCars2APIStruct();
        private pCars2APIStruct currentGameState = new pCars2APIStruct();
        private pCars2APIStruct previousGameState = new pCars2APIStruct();

        private byte[] receivedDataBuffer;

        private IPEndPoint broadcastAddress;
        private UdpClient udpClient;

        private String lastReadFileName = null;

        private static Boolean[] buttonsState = new Boolean[24];

        private AsyncCallback socketCallback;

        public static Boolean getButtonState(int index) 
        {
            return buttonsState[index];
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper>(), filenameToDump);
            }
        }

        public override void ResetGameDataFromFile()
        {
            dataReadFromFileIndex = 0;
        }

        public override Object ReadGameDataFromFile(String filename, int pauseBeforeStart)
        {
            if (dataReadFromFile == null || filename != lastReadFileName)
            {
                dataReadFromFileIndex = 0;
                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                return structWrapperData;
            }
            else
            {
                return null;
            }
        }

        protected override Boolean InitialiseInternal()
        {
            if (!this.initialised)
            {
                socketCallback = new AsyncCallback(ReceiveCallback);
                workingGameState.mVersion = 5;
                currentGameState.mVersion = 5;
                previousGameState.mVersion = 5;
                acceptedOutOfSequenceTelemCount = 0;
                discardedTelemCount = 0;
                inSequenceTelemCount = 0;
                telemPacketCount = 0;
                raceDefinitionPacketCount = 0; 
                participantsPacketCount = 0; 
                timingsPacketCount = 0; 
                gameStatePacketCount = 0; 
                weatherStatePacketCount = 0;
                vehicleNamesPacketCount = 0;
                timeStatsPacketCount = 0;
                participantVehicleNamesPacketCount = 0;

                packetCountAtStartOfCurrentRateCheck = 0;
                packetCountAtStartOfNextRateCheck = packetRateCheckInterval;
                ticksAtStartOfCurrentPacketRateCheck = DateTime.UtcNow.Ticks;
                lastPacketRateEstimate = -1;

                if (dumpToFile)
                {
                    dataToDump = new List<CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper>();
                }
                this.broadcastAddress = new IPEndPoint(IPAddress.Any, udpPort);
                this.udpClient = new UdpClient();
                this.udpClient.Client.SetSocketOption(SocketOptionLevel.Socket, SocketOptionName.ReuseAddress, true);
                this.udpClient.ExclusiveAddressUse = false; // only if you want to send/receive on same machine.
                this.udpClient.Client.Bind(this.broadcastAddress);
                this.receivedDataBuffer = new byte[this.udpClient.Client.ReceiveBufferSize];
                this.running = true;
                this.udpClient.Client.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, ReceiveCallback, this.udpClient.Client);
                this.initialised = true;
                Console.WriteLine("Listening for UDP data on port " + udpPort);                
            }
            return this.initialised;
        }

        private void ReceiveCallback(IAsyncResult result)
        {
            //Socket was the passed in as the state
            try
            {
                Socket socket = (Socket)result.AsyncState;
                int received = socket.EndReceive(result);
                if (received > 0)
                {
                    // do something with the data
                    lock (this)
                    {
                        try
                        {
                            readRawData(this.receivedDataBuffer);
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine("Error reading UDP data " + e.Message);
                        }
                    }
                }
                if (running)
                {
                    // socket.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), socket);
                    socket.BeginReceive(this.receivedDataBuffer, 0, this.receivedDataBuffer.Length, SocketFlags.None, socketCallback, socket);
                }
            }
            catch (Exception e)
            {
                this.initialised = false;
                if (e is ObjectDisposedException || e is SocketException)
                {
                    Console.WriteLine("Socket is closed");                    
                    return;
                }
                throw;
            }
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper structWrapper = new CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper();
            structWrapper.ticksWhenRead = DateTime.UtcNow.Ticks;
            lock (this)
            {
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise UDP client");
                    }
                }
                previousGameState = StructHelper.Clone(currentGameState);
                currentGameState = StructHelper.Clone(workingGameState);
                if (forSpotter)
                {
                    newSpotterData = false;
                }
            }
            structWrapper.data = currentGameState;
            if (!forSpotter && dumpToFile && dataToDump != null && currentGameState.mTrackLocation != null &&
                currentGameState.mTrackLocation.Length > 0)
            {
                dataToDump.Add(structWrapper);
            }
            return structWrapper;
        }

        private int readRawData(byte[] rawData)
        {
            // unpack the packet header manually before attempting to load into a struct
            uint packetNumber = BitConverter.ToUInt32(rawData, 0);
            uint categoryPacketNumber = BitConverter.ToUInt32(rawData, 4);
            int partialPacketIndexBytes = rawData[8];
            int partialPacketNumber = rawData[9];
            EUDPStreamerPacketHandlerType packetType = (EUDPStreamerPacketHandlerType) rawData[10];
            int packetVersion = rawData[11];

            int frameLength = 0;
            switch (packetType)
            {
                case EUDPStreamerPacketHandlerType.eCarPhysics:
                    telemPacketCount++;
                    if (telemPacketCount > packetCountAtStartOfNextRateCheck)
                    {
                        lastPacketRateEstimate = (int)((float)TimeSpan.TicksPerSecond * (float)(telemPacketCount - packetCountAtStartOfCurrentRateCheck) / (float)(DateTime.UtcNow.Ticks - ticksAtStartOfCurrentPacketRateCheck));
                        Console.WriteLine("Packet rate = " + lastPacketRateEstimate + 
                            "Hz, totals:" +
                            "\rtelem = " + telemPacketCount +
                            "\rraceDefinition = " + raceDefinitionPacketCount +
                            "\rparticipants = " + participantsPacketCount +
                            "\rtimings = " + timingsPacketCount +
                            "\rgameState = " + gameStatePacketCount +
                            "\rweather = " + weatherStatePacketCount +
                            "\rvehicleNames = " + vehicleNamesPacketCount +
                            "\rtimeStats = " + timeStatsPacketCount +
                            "\rparticipantVehicleNames = " + participantVehicleNamesPacketCount +
                            "\rin sequence = " + inSequenceTelemCount + " oos accepted = " + acceptedOutOfSequenceTelemCount + " oos rejected = " + discardedTelemCount);
                        packetCountAtStartOfCurrentRateCheck = telemPacketCount;
                        packetCountAtStartOfNextRateCheck = packetCountAtStartOfCurrentRateCheck + packetRateCheckInterval;
                        ticksAtStartOfCurrentPacketRateCheck = DateTime.UtcNow.Ticks;
                    }
                    frameLength = UDPPacketSizes.telemetryPacketSize;
                    Boolean sequenceCheckOK = isNextInSequence(packetNumber);
                    if (sequenceCheckOK)
                    {
                        inSequenceTelemCount++;
                    }
                    if (strictPacketOrdering && !sequenceCheckOK)
                    {
                        discardedTelemCount++;
                    }
                    else
                    {
                        GCHandle telemHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                        try
                        {
                            sTelemetryData telem = (sTelemetryData)Marshal.PtrToStructure(telemHandle.AddrOfPinnedObject(), typeof(sTelemetryData));
                            if (sequenceCheckOK || !telemIsOutOfSequence(telem))
                            {
                                buttonsState = ConvertBytesToBoolArray(telem.sDPad, telem.sJoyPad1, telem.sJoyPad2);
                                lastSequenceNumberForTelemPacket = (int)packetNumber;
                                workingGameState = StructHelper.MergeWithExistingState(workingGameState, telem);
                                newSpotterData = true;
                            }
                        }
                        finally
                        {
                            telemHandle.Free();
                        }
                    }
                    break;
                case EUDPStreamerPacketHandlerType.eRaceDefinition:
                    raceDefinitionPacketCount++;
                    frameLength = UDPPacketSizes.raceDataPacketSize;
                    GCHandle raceDefHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sRaceData raceDefinition = (sRaceData)Marshal.PtrToStructure(raceDefHandle.AddrOfPinnedObject(), typeof(sRaceData));
                        workingGameState = StructHelper.MergeWithExistingState(workingGameState, raceDefinition);
                    }
                    finally
                    {
                        raceDefHandle.Free();
                    }
                    break;
                case EUDPStreamerPacketHandlerType.eParticipants:
                    participantsPacketCount++;
                    frameLength = UDPPacketSizes.participantsDataPacketSize;
                    GCHandle participantsHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sParticipantsData participants = (sParticipantsData)Marshal.PtrToStructure(participantsHandle.AddrOfPinnedObject(), typeof(sParticipantsData));
                        workingGameState = StructHelper.MergeWithExistingState(workingGameState, participants);
                    }
                    finally
                    {
                        participantsHandle.Free();
                    }
                    break;
                case EUDPStreamerPacketHandlerType.eTimings:
                    timingsPacketCount++;
                    frameLength = UDPPacketSizes.timingsDataPacketSize;
                    GCHandle timingsHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sTimingsData timings = (sTimingsData)Marshal.PtrToStructure(timingsHandle.AddrOfPinnedObject(), typeof(sTimingsData));
                        workingGameState = StructHelper.MergeWithExistingState(workingGameState, timings);
                    }
                    finally
                    {
                        timingsHandle.Free();
                    }
                    break;
                case EUDPStreamerPacketHandlerType.eGameState:
                    gameStatePacketCount++;
                    frameLength = UDPPacketSizes.gameStateDataPacketSize;
                    GCHandle gameStateHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sGameStateData gameState = (sGameStateData)Marshal.PtrToStructure(gameStateHandle.AddrOfPinnedObject(), typeof(sGameStateData));
                        workingGameState = StructHelper.MergeWithExistingState(workingGameState, gameState);
                    }
                    finally
                    {
                        gameStateHandle.Free();
                    }
                    break;
                case EUDPStreamerPacketHandlerType.eWeatherState:
                    weatherStatePacketCount++;
                    Console.WriteLine("Got an undocumented and unsupported weather packet");
                    break;
                case EUDPStreamerPacketHandlerType.eVehicleNames:
                    weatherStatePacketCount++;
                    Console.WriteLine("Got an undocumented and unsupported vehicle names packet");
                    break;
                case EUDPStreamerPacketHandlerType.eTimeStats:
                    participantsPacketCount++;
                    frameLength = UDPPacketSizes.timeStatsPacketSize;
                    GCHandle timeStatsHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sTimeStatsData timeStatsData = (sTimeStatsData)Marshal.PtrToStructure(timeStatsHandle.AddrOfPinnedObject(), typeof(sTimeStatsData));
                        workingGameState = StructHelper.MergeWithExistingState(workingGameState, timeStatsData);
                    }
                    finally
                    {
                        timeStatsHandle.Free();
                    }
                    break;
                case EUDPStreamerPacketHandlerType.eParticipantVehicleNames:
                    participantsPacketCount++;
                    frameLength = UDPPacketSizes.participantVehicleNamesPacketSize;
                    GCHandle vehNamesHandle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sParticipantVehicleNamesData participantVehicleNames = (sParticipantVehicleNamesData)Marshal.PtrToStructure(vehNamesHandle.AddrOfPinnedObject(), typeof(sParticipantVehicleNamesData));
                        workingGameState = StructHelper.MergeWithExistingState(workingGameState, participantVehicleNames);
                    }
                    finally
                    {
                        vehNamesHandle.Free();
                    }
                    break;
            }
            return frameLength;
        }

        private Boolean isNextInSequence(uint thisPacketSequenceNumber)
        {
            if (lastSequenceNumberForTelemPacket != -1)
            {
                int expected = lastSequenceNumberForTelemPacket + 1;
                if (expected > sequenceWrapsAt)
                {
                    expected = 0;
                }
                if (expected != thisPacketSequenceNumber)
                {
                    return false;
                }
            }
            return true;
        }

        private Boolean telemIsOutOfSequence(sTelemetryData telem)
        {
            // TODO: 
            return false;
        }
    
        public override void Dispose()
        {
            if (udpClient != null)
            {
                try
                {
                    if (running)
                    {
                        stop();
                    }
                    udpClient.Close();
                }
                catch (Exception) { }
            }
            initialised = false;
        }

        public override bool hasNewSpotterData()
        {
            return newSpotterData;
        }

        public override void stop()
        {
            running = false;
            if (udpClient != null && udpClient.Client != null && udpClient.Client.Connected)
            {
                udpClient.Client.Disconnect(true);
            }
            Console.WriteLine("Stopped UDP data receiver, received " + telemPacketCount + 
                " telem packets, accepted " + acceptedOutOfSequenceTelemCount + " out-of-sequence packets, discarded " + discardedTelemCount + " packets");
            this.initialised = false;
            acceptedOutOfSequenceTelemCount = 0;
            inSequenceTelemCount = 0;
            discardedTelemCount = 0;
            telemPacketCount = 0;
            raceDefinitionPacketCount = 0;
            participantsPacketCount = 0;
            timingsPacketCount = 0;
            gameStatePacketCount = 0;
            weatherStatePacketCount = 0;
            vehicleNamesPacketCount = 0;
            timeStatsPacketCount = 0;
            participantVehicleNamesPacketCount = 0;
            buttonsState = new Boolean[24];
        }

        public int getButtonIndexForAssignment()
        {
            Boolean isAlreadyRunning = this.initialised;
            if (!isAlreadyRunning)
            {
                InitialiseInternal();
            }
            int pressedIndex = -1;
            DateTime timeout = DateTime.UtcNow.Add(TimeSpan.FromSeconds(10));
            while (pressedIndex == -1 && DateTime.UtcNow < timeout)
            {
                for (int i = 0; i < buttonsState.Count(); i++)
                {
                    if (buttonsState[i])
                    {
                        pressedIndex = i;
                        break;
                    }
                }
            }
            if (!isAlreadyRunning)
            {
                udpClient.Close();
                this.initialised = false;
            }
            buttonsState = new Boolean[24];
            initialised = false;
            return pressedIndex;
        }

        public static bool[] ConvertBytesToBoolArray(byte dpad, byte joypad1, byte joypad2)
        {
            // Console.WriteLine(dpad + ", " + joypad1 + ", " + joypad2);
            bool[] result = new bool[24];
            // check each bit in the byte. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
            {
                result[i] = (dpad & (1 << i)) == 0 ? false : true;
            }
            for (int i = 0; i < 8; i++)
            {
                result[i + 8] = (joypad1 & (1 << i)) == 0 ? false : true;
            } 
            for (int i = 0; i < 8; i++)
            {
                result[i + 16] = (joypad2 & (1 << i)) == 0 ? false : true;
            }
            return result;
        }
    }
}
