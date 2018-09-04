﻿using F1UdpNet;
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

namespace CrewChiefV4.F1_2018
{
    public class F12018UDPreader : GameDataReader
    {
        public class F12018StructWrapper
        {
            public long ticksWhenRead;
            public UDPPacket data;
        }

        int frameLength = 1289;

        private long packetRateCheckInterval = 1000;
        private long packetCountAtStartOfCurrentRateCheck = 0;
        private long packetCountAtStartOfNextRateCheck = 0;
        private long ticksAtStartOfCurrentPacketRateCheck = 0;
        private float lastPacketRateEstimate = -1;

        private long telemPacketCount = 0;

        private Boolean newSpotterData = true;
        private Boolean running = false;
        private Boolean initialised = false;
        private List<F12018StructWrapper> dataToDump;
        private F12018StructWrapper latestData;
        private F12018StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private int udpPort = UserSettings.GetUserSettings().getInt("f1_2018_udp_data_port");

        private byte[] receivedDataBuffer;

        private IPEndPoint broadcastAddress;
        private UdpClient udpClient;

        private String lastReadFileName = null;

        private AsyncCallback socketCallback;

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<F12018StructWrapper>(), filenameToDump);
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
                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<F12018StructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                F12018StructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
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
                telemPacketCount = 0;

                packetCountAtStartOfCurrentRateCheck = 0;
                packetCountAtStartOfNextRateCheck = packetRateCheckInterval;
                ticksAtStartOfCurrentPacketRateCheck = DateTime.Now.Ticks;
                lastPacketRateEstimate = -1;

                if (dumpToFile)
                {
                    dataToDump = new List<F12018StructWrapper>();
                }
                // TODO: f1 2018 can operate in broadcast or point to point mode, need to add options here so we're not tied to broadcast
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
                            readFromOffset(0, this.receivedDataBuffer);
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
            F12018StructWrapper structWrapper = new F12018StructWrapper();
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
                if (forSpotter)
                {
                    newSpotterData = false;
                }
            }
            if (!forSpotter && dumpToFile && dataToDump != null && latestData != null /* && latestData has some sane data?*/)
            {
                dataToDump.Add(structWrapper);
            }
            return structWrapper;
        }

        private int readFromOffset(int offset, byte[] rawData)
        {
            e_PacketId packetId = (e_PacketId) rawData[3];

            GCHandle handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
            try
            {
                switch (packetId)
                {
                    case e_PacketId.CarSetups:
                        PacketCarSetupData carSetupData = (PacketCarSetupData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketCarSetupData));
                        // merge with the existing gamestate
                        break;
                    case e_PacketId.CarStatus:
                        PacketCarStatusData carStatusData = (PacketCarStatusData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketCarStatusData));
                        // merge with the existing gamestate
                        break;
                    case e_PacketId.CarTelemetry:
                        PacketCarTelemetryData carTelemetryData = (PacketCarTelemetryData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketCarTelemetryData));
                        // merge with the existing gamestate
                        break;
                    case e_PacketId.Event:
                        PacketEventData eventData = (PacketEventData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketEventData));
                        // merge with the existing gamestate
                        break;
                    case e_PacketId.LapData:
                        PacketLapData lapData = (PacketLapData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketLapData));
                        // merge with the existing gamestate                    
                        break;
                    case e_PacketId.Motion:
                        PacketMotionData carMotionData = (PacketMotionData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketMotionData));
                        // merge with the existing gamestate                    
                        break;
                    case e_PacketId.Participants:
                        PacketParticipantsData participantData = (PacketParticipantsData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketParticipantsData));
                        // merge with the existing gamestate
                        break;
                    case e_PacketId.Session:
                        PacketSessionData sessionData = (PacketSessionData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketSessionData));
                        // merge with the existing gamestate
                        break;
                }
            }
            finally
            {
                handle.Free();
            }
            return frameLength + offset;
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
            Console.WriteLine("Stopped UDP data receiver, received " + telemPacketCount + " telem packets");
            this.initialised = false;
            telemPacketCount = 0;
        }
    }
}
