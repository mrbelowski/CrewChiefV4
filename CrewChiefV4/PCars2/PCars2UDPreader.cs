﻿using System;
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
        private long stringsPacketCount = 0;
        private long additionalStringsPacketCount = 0;

        private long inSequenceTelemCount = 0;
        private long discardedTelemCount = 0;
        private long acceptedOutOfSequenceTelemCount = 0;

        private float lastValidTelemCurrentLapTime = -1;
        private float lastValidTelemLapsCompleted = 0;

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

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null || filename != lastReadFileName)
            {
                dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<CrewChiefV4.PCars2.PCars2SharedMemoryReader.PCars2StructWrapper[]>(dataFilesPath + filename);
                lastReadFileName = filename;
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
                stringsPacketCount = 0;
                additionalStringsPacketCount = 0;
                lastValidTelemCurrentLapTime = -1;
                lastValidTelemLapsCompleted = 0;

                packetCountAtStartOfCurrentRateCheck = 0;
                packetCountAtStartOfNextRateCheck = packetRateCheckInterval;
                ticksAtStartOfCurrentPacketRateCheck = DateTime.Now.Ticks;
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
            structWrapper.ticksWhenRead = DateTime.Now.Ticks;
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

            uint frameLength = 0;
            if (packetType == EUDPStreamerPacketHandlerType.eCarPhysics)
            {
                telemPacketCount++;
                if (telemPacketCount > packetCountAtStartOfNextRateCheck)
                {
                    lastPacketRateEstimate = (int)((float)TimeSpan.TicksPerSecond * (float)(telemPacketCount - packetCountAtStartOfCurrentRateCheck) / (float)(DateTime.Now.Ticks - ticksAtStartOfCurrentPacketRateCheck));
                    Console.WriteLine("Packet rate = " + lastPacketRateEstimate + "Hz, totals: type0 = " + telemPacketCount + " type1 = " + stringsPacketCount + " type2 = " + additionalStringsPacketCount +
                        " in sequence = " + inSequenceTelemCount + " oos accepted = " + acceptedOutOfSequenceTelemCount + " oos rejected = " + discardedTelemCount);
                    packetCountAtStartOfCurrentRateCheck = telemPacketCount;
                    packetCountAtStartOfNextRateCheck = packetCountAtStartOfCurrentRateCheck + packetRateCheckInterval;
                    ticksAtStartOfCurrentPacketRateCheck = DateTime.Now.Ticks;
                }
                frameLength = sTelemetryData.sPacketSize;
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
                    GCHandle handle = GCHandle.Alloc(rawData.Take(frameLength).ToArray(), GCHandleType.Pinned);
                    try
                    {
                        sTelemetryData telem = (sTelemetryData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sTelemetryData));
                        if (sequenceCheckOK || !telemIsOutOfSequence(telem))
                        {
                            buttonsState = ConvertBytesToBoolArray(telem.sDPad, telem.sJoyPad1, telem.sJoyPad2);
                            lastSequenceNumberForTelemPacket = sequence;
                            workingGameState = StructHelper.MergeWithExistingState(workingGameState, telem);
                            newSpotterData = workingGameState.hasNewPositionData;
                        }
                    }
                    finally
                    {
                        handle.Free();
                    }
                }    
            }
            else if (frameType == 1)
            {
                stringsPacketCount++;
                frameLength = sParticipantInfoStrings_PacketSize;
                GCHandle handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                try
                {
                    sParticipantInfoStrings strings = (sParticipantInfoStrings)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStrings));
                    workingGameState = StructHelper.MergeWithExistingState(workingGameState, strings);
                }
                finally
                {
                    handle.Free();
                }
            }
            else if (frameType == 2)
            {
                additionalStringsPacketCount++;
                frameLength = sParticipantInfoStringsAdditional_PacketSize;
                GCHandle handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                try
                {
                    sParticipantInfoStringsAdditional additional = (sParticipantInfoStringsAdditional)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(sParticipantInfoStringsAdditional));
                    workingGameState = StructHelper.MergeWithExistingState(workingGameState, additional);
                }
                finally
                {
                    handle.Free();
                }
            }
            else
            {
                Console.WriteLine("Unrecognised frame type " + frameType + " from byte " + rawData[offset + 2]);
            }
            return frameLength + offset;
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
            if (telem.sViewedParticipantIndex >= 0 && telem.sParticipantInfo.Length > telem.sViewedParticipantIndex)
            {
                int lapsCompletedInTelem = telem.sParticipantInfo[telem.sViewedParticipantIndex].sLapsCompleted;
                float lapTimeInTelem = telem.sCurrentTime;
                if (lapTimeInTelem > 0 && lastValidTelemCurrentLapTime > 0)
                {
                    // if the number of completed laps has decreased, or our laptime has decreased without starting
                    // a new lap then we need to discard the packet. The lapsCompleted is unreliable, this may end badly
                    if (lastValidTelemLapsCompleted > lapsCompletedInTelem ||
                        (lapTimeInTelem < lastValidTelemCurrentLapTime && lastValidTelemLapsCompleted == lapsCompletedInTelem))
                    {
                        discardedTelemCount++;
                        return true;
                    }
                }
                lastValidTelemCurrentLapTime = lapTimeInTelem;
                lastValidTelemLapsCompleted = lapsCompletedInTelem;
                acceptedOutOfSequenceTelemCount++;
            }            
            return false;
        }
    
        public override void Dispose()
        {
            if (udpClient != null)
            {
                try
                {
                    stop();
                    udpClient.Close();
                }
                catch (Exception) { }
            }
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
            stringsPacketCount = 0;
            additionalStringsPacketCount = 0;
            lastValidTelemCurrentLapTime = -1;
            lastValidTelemLapsCompleted = 0; 
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
            DateTime timeout = DateTime.Now.Add(TimeSpan.FromSeconds(10));
            while (pressedIndex == -1 && DateTime.Now < timeout)
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
