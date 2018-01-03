using CrewChiefV4.rFactor2;
using CrewChiefV4.rFactor2.rFactor2Data;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Xml;
using System.Xml.Serialization;

namespace CrewChiefV4.rFactor2
{
    public class RF2SharedMemoryReader : GameDataReader
    {
        private class MappedDoubleBuffer<MappedBufferT>
        {
            readonly int RF2_BUFFER_HEADER_SIZE_BYTES = Marshal.SizeOf(typeof(rF2BufferHeader));
            readonly int RF2_BUFFER_HEADER_WITH_SIZE_SIZE_BYTES = Marshal.SizeOf(typeof(rF2MappedBufferHeaderWithSize));

            readonly int BUFFER_SIZE_BYTES;
            readonly string BUFFER1_NAME;
            readonly string BUFFER2_NAME;
            readonly string MUTEX_NAME;

            // Holds the entire byte array that can be marshalled to a MappedBufferT.  Partial updates
            // only read changed part of buffer, ignoring trailing uninteresting bytes.  However,
            // to marshal we still need to supply entire structure size.  So, on update new bytes are copied
            // (outside of the mutex).
            byte[] fullSizeBuffer = null;

            Mutex mutex = null;
            MemoryMappedFile memoryMappedFile1 = null;
            MemoryMappedFile memoryMappedFile2 = null;

            public MappedDoubleBuffer(string buff1Name, string buff2Name, string mutexName)
            {
                this.BUFFER_SIZE_BYTES = Marshal.SizeOf(typeof(MappedBufferT));
                this.BUFFER1_NAME = buff1Name;
                this.BUFFER2_NAME = buff2Name;
                this.MUTEX_NAME = mutexName;
            }

            public void Connect()
            {
                this.mutex = Mutex.OpenExisting(this.MUTEX_NAME);
                this.memoryMappedFile1 = MemoryMappedFile.OpenExisting(this.BUFFER1_NAME);
                this.memoryMappedFile2 = MemoryMappedFile.OpenExisting(this.BUFFER2_NAME);

                // NOTE: Make sure that BUFFER_SIZE matches the structure size in the plugin (debug mode prints that).
                this.fullSizeBuffer = new byte[this.BUFFER_SIZE_BYTES];
            }

            public void Disconnect()
            {
                if (this.memoryMappedFile1 != null)
                    this.memoryMappedFile1.Dispose();

                if (this.memoryMappedFile2 != null)
                    this.memoryMappedFile2.Dispose();

                if (this.mutex != null)
                    this.mutex.Dispose();

                this.memoryMappedFile1 = null;
                this.memoryMappedFile2 = null;
                this.fullSizeBuffer = null;
                this.mutex = null;
            }

            public void GetMappedData(ref MappedBufferT mappedData)
            {
                //
                // IMPORTANT:  Clients that do not need consistency accross the whole buffer, like dashboards that visualize data, _do not_ need to use mutexes.
                //

                // Note: if it is critical for client minimize wait time, same strategy as plugin uses can be employed.
                // Pass 0 timeout and skip update if someone holds the lock.
                if (this.mutex.WaitOne(5000))
                {
                    byte[] sharedMemoryReadBuffer = null;
                    try
                    {
                        bool buf1Current = false;
                        // Try buffer 1:
                        using (var sharedMemoryStreamView = this.memoryMappedFile1.CreateViewStream())
                        {
                            var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.RF2_BUFFER_HEADER_SIZE_BYTES);

                            // Marhsal header.
                            var headerHandle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                            var header = (rF2MappedBufferHeader)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2MappedBufferHeader));
                            headerHandle.Free();

                            if (header.mCurrentRead == 1)
                            {
                                sharedMemoryStream.BaseStream.Position = 0;
                                sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.BUFFER_SIZE_BYTES);
                                buf1Current = true;
                            }
                        }

                        // Read buffer 2
                        if (!buf1Current)
                        {
                            using (var sharedMemoryStreamView = this.memoryMappedFile2.CreateViewStream())
                            {
                                var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                                sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.BUFFER_SIZE_BYTES);
                            }
                        }
                    }
                    finally
                    {
                        this.mutex.ReleaseMutex();
                    }

                    // Marshal rF2 State buffer
                    var handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);

                    mappedData = (MappedBufferT)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MappedBufferT));

                    handle.Free();
                }
            }

            public void GetMappedDataPartial(ref MappedBufferT mappedData)
            {
                //
                // IMPORTANT:  Clients that do not need consistency accross the whole buffer, like dashboards that visualize data, _do not_ need to use mutexes.
                //

                // Note: if it is critical for client minimize wait time, same strategy as plugin uses can be employed.
                // Pass 0 timeout and skip update if someone holds the lock.

                // Using partial buffer copying reduces time under lock.  Scoring by 30%, telemetry by 70%.
                // TODO: think about what could possibly cause AbandonedMutex exception, this is likely what users are hitting (crash).
                if (this.mutex.WaitOne(5000))
                {
                    byte[] sharedMemoryReadBuffer = null;
                    try
                    {
                        bool buf1Current = false;
                        // Try buffer 1:
                        using (var sharedMemoryStreamView = this.memoryMappedFile1.CreateViewStream())
                        {
                            var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                            sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.RF2_BUFFER_HEADER_WITH_SIZE_SIZE_BYTES);

                            // Marhsal header.
                            var headerHandle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                            var header = (rF2MappedBufferHeaderWithSize)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2MappedBufferHeaderWithSize));
                            headerHandle.Free();

                            if (header.mCurrentRead == 1)
                            {
                                sharedMemoryStream.BaseStream.Position = 0;
                                sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(header.mBytesUpdatedHint != 0 ? header.mBytesUpdatedHint : this.BUFFER_SIZE_BYTES);
                                buf1Current = true;
                            }
                        }

                        // Read buffer 2
                        if (!buf1Current)
                        {
                            using (var sharedMemoryStreamView = this.memoryMappedFile2.CreateViewStream())
                            {
                                var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                                sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.RF2_BUFFER_HEADER_WITH_SIZE_SIZE_BYTES);

                                // Marhsal header.
                                var headerHandle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                                var header = (rF2MappedBufferHeaderWithSize)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2MappedBufferHeaderWithSize));
                                headerHandle.Free();

                                sharedMemoryStream.BaseStream.Position = 0;
                                sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(header.mBytesUpdatedHint != 0 ? header.mBytesUpdatedHint : this.BUFFER_SIZE_BYTES);
                            }
                        }
                    }
                    finally
                    {
                        this.mutex.ReleaseMutex();
                    }

                    Array.Copy(sharedMemoryReadBuffer, this.fullSizeBuffer, sharedMemoryReadBuffer.Length);

                    // Marshal rF2 State buffer
                    var handle = GCHandle.Alloc(this.fullSizeBuffer, GCHandleType.Pinned);

                    mappedData = (MappedBufferT)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(MappedBufferT));

                    handle.Free();
                }
            }
        }

        MappedDoubleBuffer<rF2Telemetry> telemetryBuffer = new MappedDoubleBuffer<rF2Telemetry>(rFactor2Constants.MM_TELEMETRY_FILE_NAME1,
            rFactor2Constants.MM_TELEMETRY_FILE_NAME2, rFactor2Constants.MM_TELEMETRY_FILE_ACCESS_MUTEX);

        MappedDoubleBuffer<rF2Scoring> scoringBuffer = new MappedDoubleBuffer<rF2Scoring>(rFactor2Constants.MM_SCORING_FILE_NAME1,
            rFactor2Constants.MM_SCORING_FILE_NAME2, rFactor2Constants.MM_SCORING_FILE_ACCESS_MUTEX);

        MappedDoubleBuffer<rF2Rules> rulesBuffer = new MappedDoubleBuffer<rF2Rules>(rFactor2Constants.MM_RULES_FILE_NAME1,
            rFactor2Constants.MM_RULES_FILE_NAME2, rFactor2Constants.MM_RULES_FILE_ACCESS_MUTEX);

        MappedDoubleBuffer<rF2Extended> extendedBuffer = new MappedDoubleBuffer<rF2Extended>(rFactor2Constants.MM_EXTENDED_FILE_NAME1,
              rFactor2Constants.MM_EXTENDED_FILE_NAME2, rFactor2Constants.MM_EXTENDED_FILE_ACCESS_MUTEX);

        private bool initialised = false;
        private List<RF2StructWrapper> dataToDump;
        private RF2StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private string lastReadFileName = null;

        // Capture mCurrentET from scoring to prevent dumping double frames to the file.
        private double lastScoringET = -1.0;
        // Capture mElapsedTime from telemetry of the first vehicle to prevent dumping double frames to the file.
        private double lastTelemetryET = -1.0;

        public class RF2StructWrapper
        {
            public long ticksWhenRead;
            public rF2Telemetry telemetry;
            public rF2Scoring scoring;
            public rF2Rules rules;
            public rF2Extended extended;
        }

        public override void DumpRawGameData()
        {
            if (this.dumpToFile && this.dataToDump != null && this.dataToDump.Count > 0 && this.filenameToDump != null)
                this.SerializeObject(this.dataToDump.ToArray<RF2StructWrapper>(), this.filenameToDump);
        }

        public override void ResetGameDataFromFile()
        {
            this.dataReadFromFileIndex = 0;
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            if (this.dataReadFromFile == null || filename != this.lastReadFileName)
            {
                this.dataReadFromFileIndex = 0;

                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<RF2StructWrapper[]>(filePathResolved);

                this.lastReadFileName = filename;
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > this.dataReadFromFileIndex)
            {
                RF2StructWrapper structWrapperData = dataReadFromFile[this.dataReadFromFileIndex];
                this.dataReadFromFileIndex++;
                return structWrapperData;
            }
            else
            {
                return null;
            }
        }

        protected override Boolean InitialiseInternal()
        {
            this.lastScoringET = -1.0;

            // This needs to be synchronized, because disconnection happens from CrewChief.Run and MainWindow.Dispose.
            lock (this)
            {
                if (!this.initialised)
                {
                    try
                    {
                        this.telemetryBuffer.Connect();
                        this.scoringBuffer.Connect();
                        this.rulesBuffer.Connect();
                        this.extendedBuffer.Connect();

                        if (dumpToFile)
                            this.dataToDump = new List<RF2StructWrapper>();

                        this.initialised = true;

                        Console.WriteLine("Initialized rFactor 2 Shared Memory");
                    }
                    catch (Exception)
                    {
                        this.initialised = false;
                        this.DisconnectInternal();
                    }
                }
                return initialised;
            }
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                if (!initialised)
                {
                    if (!this.InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try 
                {
                    RF2StructWrapper wrapper = new RF2StructWrapper();

                    extendedBuffer.GetMappedData(ref wrapper.extended);
                    telemetryBuffer.GetMappedDataPartial(ref wrapper.telemetry);
                    rulesBuffer.GetMappedDataPartial(ref wrapper.rules);

                    // Scoring is the most important game data in Crew Chief sense, 
                    // so acquire it last, hoping it will be most recent view of all buffer types.
                    scoringBuffer.GetMappedDataPartial(ref wrapper.scoring);

                    wrapper.ticksWhenRead = DateTime.Now.Ticks;

                    if (!forSpotter && dumpToFile && this.dataToDump != null)
                    {
                        // Note: this is lossy save, because we only save update if Telemtry or Scoring changed.
                        // Other buffers don't change that much, so it should be fine.

                        // Exclude empty frames.
                        if (wrapper.scoring.mScoringInfo.mNumVehicles > 0
                            && wrapper.extended.mSessionStarted == 1)
                        {
                            var hasTelemetryChanged = false;
                            if (wrapper.telemetry.mNumVehicles > 0)
                            {
                                var currTelET = wrapper.telemetry.mVehicles[0].mElapsedTime;
                                hasTelemetryChanged = currTelET != this.lastTelemetryET;
                                this.lastTelemetryET = currTelET;
                            }

                            var currScoringET = wrapper.scoring.mScoringInfo.mCurrentET;
                            if (currScoringET != this.lastScoringET  // scoring contains new payload
                                || hasTelemetryChanged)  // Or, telemetry updated.
                            {
                                // NOTE: truncation code could be moved to DumpRawGameData method for reduced CPU use.
                                // However, this causes memory pressure (~250Mb/minute with 22 vehicles), so probably better done here.
                                wrapper.telemetry.mVehicles = this.GetPopulatedVehicleInfoArray<rF2VehicleTelemetry>(wrapper.telemetry.mVehicles, wrapper.telemetry.mNumVehicles);
                                wrapper.scoring.mVehicles = this.GetPopulatedVehicleInfoArray<rF2VehicleScoring>(wrapper.scoring.mVehicles, wrapper.scoring.mScoringInfo.mNumVehicles);

                                // For rules, exclude empty messages from serialization.
                                wrapper.rules.mTrackRules.mMessage = wrapper.rules.mTrackRules.mMessage[0] != 0 ? wrapper.rules.mTrackRules.mMessage : null;
                                wrapper.rules.mParticipants = this.GetPopulatedVehicleInfoArray<rF2TrackRulesParticipant>(wrapper.rules.mParticipants, wrapper.rules.mTrackRules.mNumParticipants);
                                for (int i = 0; i < wrapper.rules.mParticipants.Length; ++i)
                                    wrapper.rules.mParticipants[i].mMessage = wrapper.rules.mParticipants[i].mMessage[0] != 0 ? wrapper.rules.mParticipants[i].mMessage : null;

                                int maxmID = 0;
                                foreach (var vehicleScoring in wrapper.scoring.mVehicles)
                                    maxmID = Math.Max(maxmID, vehicleScoring.mID);

                                if (maxmID < rFactor2Constants.MAX_MAPPED_IDS)
                                {
                                    // Since serialization to XML produces a lot of useless tags even for small arrays, truncate tracked damage array.
                                    // It is indexed by mID.  Max mID in current set is equal to mNumVehicles in 99% of cases, so just truncate to this size.
                                    wrapper.extended.mTrackedDamages = this.GetPopulatedVehicleInfoArray<rF2TrackedDamage>(wrapper.extended.mTrackedDamages, maxmID + 1);
                                }

                                this.dataToDump.Add(wrapper);
                                this.lastScoringET = currScoringET;
                            }
                        }
                    }

                    return wrapper;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("rFactor 2 Shared Memory connection failed.");
                    this.DisconnectInternal();
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }

        private VehicleInfoT[] GetPopulatedVehicleInfoArray<VehicleInfoT>(VehicleInfoT[] vehicles, int numPopulated)
        {
            // To reduce serialized size, only return non-empty vehicles.
            var populated = new List<VehicleInfoT>();
            for (int i = 0; i < numPopulated; ++i)
                populated.Add(vehicles[i]);

            // Not sure why this is needed.
            if (populated.Count == 0)
                populated.Add(vehicles[0]);  // In case of mNumVehicles == 0 this is all zero, should be.

            return populated.ToArray();
        }

        public override void DisconnectFromProcess()
        {
            var wasInitialised = this.initialised;
            this.DisconnectInternal();

            // There's still possibility of double message, but who cares.
            if (wasInitialised)
                Console.WriteLine("Disconnected from rFactor 2 Shared Memory");
        }

        private void DisconnectInternal()
        {
            // This needs to be synchronized, because disconnection happens from CrewChief.Run and MainWindow.Dispose.
            lock (this)
            {
                this.initialised = false;

                this.telemetryBuffer.Disconnect();
                this.scoringBuffer.Disconnect();
                this.rulesBuffer.Disconnect();
                this.extendedBuffer.Disconnect();

                // Hack to re-check plugin version.
                RF2GameStateMapper.pluginVerified = false;
            }
        }

        public override void Dispose()
        {
            try
            {
                this.DisconnectInternal();
            }
            catch (Exception) { }
        }
    }
}
