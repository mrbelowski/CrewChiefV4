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

        rF2State currrF2State;


        //private MemoryMappedFile memoryMappedFile;
        //private GCHandle handle;
        //private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private bool initialized = false;
        private List<RF2StructWrapper> dataToDump;
        private RF2StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private string lastReadFileName = null;

        internal class RF2StructWrapper
        {
            internal long ticksWhenRead;
            internal rF2State state;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && this.dataToDump != null && this.dataToDump.Count > 0 && filenameToDump != null)
            {
                foreach (var wrapper in this.dataToDump)
                {
                    // TODO: wtf is going on here?
                    wrapper.state.mVehicles = getPopulatedVehicleInfoArray(wrapper.state.mVehicles);
                }
                SerializeObject(this.dataToDump.ToArray<RF2StructWrapper>(), filenameToDump);
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
                if (!this.initialized)
                {
                    /*try
                    {
                        this.memoryMappedFile = MemoryMappedFile.OpenExisting(rFactor2Constant.SharedMemoryName);
                        sharedmemorysize = Marshal.SizeOf(typeof(rf2Shared));
                        sharedMemoryReadBuffer = new byte[sharedmemorysize];
                        initialised = true;
                        Console.WriteLine("Initialised rFactor 1 shared memory");
                    }*/
                    try
                    {
                        this.fileAccessMutex = Mutex.OpenExisting(rFactor2Constants.MM_FILE_ACCESS_MUTEX);
                        this.memoryMappedFile1 = MemoryMappedFile.OpenExisting(rFactor2Constants.MM_FILE_NAME1);
                        this.memoryMappedFile2 = MemoryMappedFile.OpenExisting(rFactor2Constants.MM_FILE_NAME2);
                        // NOTE: Make sure that SHARED_MEMORY_SIZE_BYTES matches the structure size in the plugin (plugin debug mode prints that).
                        this.sharedMemoryReadBuffer = new byte[this.SHARED_MEMORY_SIZE_BYTES];
                        this.initialized = true;
                    }
                    catch (Exception)
                    {
                        initialized = false;
                    }
                }
                return initialized;
            }
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                //rf2Shared _rf2apistruct = new rf2Shared();
                if (!initialized)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                /*
                try
                {
                    using (var sharedMemoryStreamView = this.memoryMappedFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemorysize);
                        this.handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        _rf2apistruct = (rf2Shared)Marshal.PtrToStructure(this.handle.AddrOfPinnedObject(), typeof(rf2Shared));
                        this.handle.Free();
                    }
                    RF2StructWrapper structWrapper = new RF2StructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                    structWrapper.state = _rf2apistruct;
                    if (!forSpotter && dumpToFile && this.dataToDump != null)
                    {
                        this.dataToDump.Add(structWrapper);
                    }
                    return structWrapper;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }*/
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
                        this.currrF2State = (rF2State)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(rF2State));
                        handle.Free();

                        RF2StructWrapper structWrapper = new RF2StructWrapper();
                        structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                        // TODO: WTF, why do we copy here??????????
                        structWrapper.state = this.currrF2State;
                        if (!forSpotter && dumpToFile && this.dataToDump != null)
                        {
                            this.dataToDump.Add(structWrapper);
                        }
                        return structWrapper;
                    }
                    else
                    {
                        // TODO investigate.
                        // Return old version if we timed out, really this should be disconnect.
                        RF2StructWrapper structWrapper = new RF2StructWrapper();
                        structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                        // TODO: WTF, why do we copy here??????????
                        structWrapper.state = this.currrF2State;
                        return structWrapper;
                    }
                }
                catch (Exception ex)
                {
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
            if (this.memoryMappedFile1 != null)
                this.memoryMappedFile1.Dispose();

            if (this.memoryMappedFile2 != null)
                this.memoryMappedFile2.Dispose();

            if (this.fileAccessMutex != null)
                this.fileAccessMutex.Dispose();

            this.memoryMappedFile1 = null;
            this.memoryMappedFile2 = null;
            this.sharedMemoryReadBuffer = null;
//            this.connected = false;
            this.fileAccessMutex = null;

//            this.EnableControls(false);
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
