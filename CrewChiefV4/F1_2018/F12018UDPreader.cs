using F1UdpNet;
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
        private long packetRateCheckInterval = 1000;
        private long packetCountAtStartOfNextRateCheck = 0;
        private long ticksAtStartOfCurrentPacketRateCheck = 0;

        int packetCount = 0;

        private Boolean newSpotterData = true;
        private Boolean running = false;
        private Boolean initialised = false;
        private List<F12018StructWrapper> dataToDump;
        private F12018StructWrapper workingData = new F12018StructWrapper();
        private F12018StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private int udpPort = UserSettings.GetUserSettings().getInt("f1_2018_udp_data_port");

        private byte[] receivedDataBuffer;

        private IPEndPoint broadcastAddress;
        private UdpClient udpClient;

        private String lastReadFileName = null;

        private AsyncCallback socketCallback;

        private static Boolean[] buttonsState = new Boolean[32];

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

        public override Object ReadGameDataFromFile(String filename, int pauseBeforeStart)
        {
            if (dataReadFromFile == null || filename != lastReadFileName)
            {
                dataReadFromFileIndex = 0;
                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<F12018StructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                F12018StructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
                workingData = structWrapperData;
                newSpotterData = true;
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
                packetCount = 0;
                packetCountAtStartOfNextRateCheck = packetRateCheckInterval;
                ticksAtStartOfCurrentPacketRateCheck = DateTime.UtcNow.Ticks;

                if (dumpToFile)
                {
                    dataToDump = new List<F12018StructWrapper>();
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
                        packetCount++;
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
            F12018StructWrapper latestData = workingData.CreateCopy(DateTime.UtcNow.Ticks, forSpotter);
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
            if (!forSpotter && dumpToFile && dataToDump != null && workingData != null /* && latestData has some sane data?*/)
            {
                dataToDump.Add(latestData);
            }
            return latestData;
        }

        private int getFrameLength(e_PacketId packetId)
        {
            switch (packetId)
            {
                case e_PacketId.Motion:
                    return 1341;
                case e_PacketId.Session:
                    return 147;
                case e_PacketId.LapData:
                    return 841;
                case e_PacketId.Event:
                    return 25;
                case e_PacketId.Participants:
                    return 1082;
                case e_PacketId.CarSetups:
                    return 841;
                case e_PacketId.CarTelemetry:
                    return 1085;
                case e_PacketId.CarStatus:
                    return 1061;
            }
            return -1;
        }

        private int readFromOffset(int offset, byte[] rawData)
        {
            e_PacketId packetId = (e_PacketId) rawData[3];
            int frameLength = getFrameLength(packetId);
            if (frameLength > 0)
            {
                GCHandle handle = GCHandle.Alloc(rawData.Skip(offset).Take(frameLength).ToArray(), GCHandleType.Pinned);
                try
                {
                    switch (packetId)
                    {
                        case e_PacketId.CarSetups:
                            workingData.packetCarSetupData = (PacketCarSetupData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketCarSetupData));
                            break;
                        case e_PacketId.CarStatus:
                            workingData.packetCarStatusData = (PacketCarStatusData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketCarStatusData));
                            break;
                        case e_PacketId.CarTelemetry:
                            workingData.packetCarTelemetryData = (PacketCarTelemetryData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketCarTelemetryData));
                            buttonsState = ConvertBytesToBoolArray(workingData.packetCarTelemetryData.m_buttonStatus1, workingData.packetCarTelemetryData.m_buttonStatus2, 
                                workingData.packetCarTelemetryData.m_buttonStatus3, workingData.packetCarTelemetryData.m_buttonStatus4);
                            break;
                        case e_PacketId.Event:
                            workingData.packetEventData = (PacketEventData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketEventData));
                            break;
                        case e_PacketId.LapData:
                            workingData.packetLapData = (PacketLapData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketLapData));
                            break;
                        case e_PacketId.Motion:
                            workingData.packetMotionData = (PacketMotionData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketMotionData));
                            newSpotterData = true;
                            break;
                        case e_PacketId.Participants:
                            workingData.packetParticipantsData = (PacketParticipantsData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketParticipantsData));
                            break;
                        case e_PacketId.Session:
                            workingData.packetSessionData = (PacketSessionData)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(PacketSessionData));
                            break;
                    }
                }
                finally
                {
                    handle.Free();
                }
            }
            return frameLength + offset;
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
            Console.WriteLine("Stopped UDP data receiver, received " + packetCount + " packets");
            this.initialised = false;
            packetCount = 0;
        }

        public static bool[] ConvertBytesToBoolArray(byte buttons1, byte buttons2, byte buttons3, byte buttons4)
        {
            bool[] result = new bool[32];
            // check each bit in each of the bytes. if 1 set to true, if 0 set to false
            for (int i = 0; i < 8; i++)
            {
                result[i] = (buttons1 & (1 << i)) == 0 ? false : true;
            }
            for (int i = 0; i < 8; i++)
            {
                result[i + 8] = (buttons2 & (1 << i)) == 0 ? false : true;
            }
            for (int i = 0; i < 8; i++)
            {
                result[i + 16] = (buttons3 & (1 << i)) == 0 ? false : true;
            }
            for (int i = 0; i < 8; i++)
            {
                result[i + 24] = (buttons4 & (1 << i)) == 0 ? false : true;
            }
            return result;
        }
    }
}
