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
        readonly int SHARED_MEMORY_SIZE_BYTES = Marshal.SizeOf(typeof(rF2State));
        readonly int SHARED_MEMORY_HEADER_SIZE_BYTES = Marshal.SizeOf(typeof(rF2StateHeader));

        Mutex fileAccessMutex = null;
        MemoryMappedFile memoryMappedFile1 = null;
        MemoryMappedFile memoryMappedFile2 = null;

        private byte[] sharedMemoryReadBuffer;
        private bool initialised = false;
        private List<RF2StructWrapper> dataToDump;
        private RF2StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private string lastReadFileName = null;

        public class RF2StructWrapper
        {
            public long ticksWhenRead;
            public rF2State state;
        }

        public override void DumpRawGameData()
        {
            if (this.dumpToFile && this.dataToDump != null && this.dataToDump.Count > 0 && this.filenameToDump != null)
            {
                foreach (var wrapper in this.dataToDump)
                    wrapper.state.mVehicles = getPopulatedVehicleInfoArray(wrapper.state.mVehicles);

                SerializeObject(this.dataToDump.ToArray<RF2StructWrapper>(), this.filenameToDump);
            }
        }

        public override void ResetGameDataFromFile()
        {
            this.dataReadFromFileIndex = 0;
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            if (dataReadFromFile == null || filename != this.lastReadFileName)
            {
                this.dataReadFromFileIndex = 0;
                dataReadFromFile = DeSerializeObject<RF2StructWrapper[]>(dataFilesPath + filename);
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
            if (dumpToFile)
            {
                this.dataToDump = new List<RF2StructWrapper>();
            }
            lock (this)
            {
                if (!this.initialised)
                {
                    try
                    {
                        this.fileAccessMutex = Mutex.OpenExisting(rFactor2Constants.MM_FILE_ACCESS_MUTEX);
                        this.memoryMappedFile1 = MemoryMappedFile.OpenExisting(rFactor2Constants.MM_FILE_NAME1);
                        this.memoryMappedFile2 = MemoryMappedFile.OpenExisting(rFactor2Constants.MM_FILE_NAME2);
                        // NOTE: Make sure that SHARED_MEMORY_SIZE_BYTES matches
                        // the structure size in the plugin (plugin debug mode prints that).
                        this.sharedMemoryReadBuffer = new byte[this.SHARED_MEMORY_SIZE_BYTES];
                        this.initialised = true;

                        Console.WriteLine("Initialized rFactor 2 Shared Memory");
                    }
                    catch (Exception)
                    {
                        initialised = false;
                        this.Disconnect();
                    }
                }
                return initialised;
            }
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                var rF2StateMarshalled = new rF2State();
                if (!initialised)
                {
                    if (!this.InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try 
                {
                    if (this.fileAccessMutex.WaitOne(5000))
                    {
                        try
                        {
                            bool buf1Current = false;
                            // Try buffer 1:
                            using (var sharedMemoryStreamView = this.memoryMappedFile1.CreateViewStream())
                            {
                                var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                                this.sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.SHARED_MEMORY_HEADER_SIZE_BYTES);

                                // Marhsal header
                                var headerHandle = GCHandle.Alloc(this.sharedMemoryReadBuffer, GCHandleType.Pinned);
                                var header = (rF2StateHeader)Marshal.PtrToStructure(headerHandle.AddrOfPinnedObject(), typeof(rF2StateHeader));
                                headerHandle.Free();

                                if (header.mCurrentRead == 1)
                                {
                                    sharedMemoryStream.BaseStream.Position = 0;
                                    this.sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.SHARED_MEMORY_SIZE_BYTES);
                                    buf1Current = true;
                                }
                            }

                            // Read buffer 2
                            if (!buf1Current)
                            {
                                using (var sharedMemoryStreamView = this.memoryMappedFile2.CreateViewStream())
                                {
                                    var sharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                                    this.sharedMemoryReadBuffer = sharedMemoryStream.ReadBytes(this.SHARED_MEMORY_SIZE_BYTES);
                                }
                            }
                        }
                        finally
                        {
                            this.fileAccessMutex.ReleaseMutex();
                        }

                        // Marshal rF2State
                        var handle = GCHandle.Alloc(this.sharedMemoryReadBuffer, GCHandleType.Pinned);
                        rF2StateMarshalled = (rF2State)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(rF2State));
                        handle.Free();

                        RF2StructWrapper structWrapper = new RF2StructWrapper();
                        structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                        structWrapper.state = rF2StateMarshalled;
                        if (!forSpotter && dumpToFile && this.dataToDump != null)
                        {
                            this.dataToDump.Add(structWrapper);
                        }
                        return structWrapper;
                    }
                    else
                    {
                        Console.WriteLine("Timed out waiting on rFactor 2 Shared Memory mutex.");
                        return null;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("rFactor 2 Shared Memory connection failed.");
                    this.Disconnect();
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }

        private rF2VehScoringInfo[] getPopulatedVehicleInfoArray(rF2VehScoringInfo[] raw)
        {
            var populated = new List<rF2VehScoringInfo>();
            foreach (var rawData in raw)
            {
                if (rawData.mPlace > 0)
                {
                    populated.Add(rawData);
                }
            }
            if (populated.Count == 0)
            {
                populated.Add(raw[0]);
            }
            return populated.ToArray();
        }

        private void Disconnect()
        {
            this.initialised = false;
            if (this.memoryMappedFile1 != null)
                this.memoryMappedFile1.Dispose();

            if (this.memoryMappedFile2 != null)
                this.memoryMappedFile2.Dispose();

            if (this.fileAccessMutex != null)
                this.fileAccessMutex.Dispose();

            this.memoryMappedFile1 = null;
            this.memoryMappedFile2 = null;
            this.sharedMemoryReadBuffer = null;
            this.fileAccessMutex = null;
        }


        public override void Dispose()
        {
            try
            {
                this.Disconnect();
            }
            catch (Exception) { }
        }
    }
}
