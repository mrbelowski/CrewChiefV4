using CrewChiefV4.rFactor1;
using CrewChiefV4.rFactor1.rFactor1Data;
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

namespace CrewChiefV4.rFactor1
{
    public class RF1SharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private GCHandle handle;
        private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private Boolean initialised = false;
        private List<RF1StructWrapper> dataToDump;
        private RF1StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;

        public class RF1StructWrapper
        {
            public long ticksWhenRead;
            public rfShared data;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                foreach (RF1StructWrapper wrapper in dataToDump)
                {
                    wrapper.data.vehicle = getPopulatedVehicleInfoArray(wrapper.data.vehicle);
                }
                SerializeObject(dataToDump.ToArray<RF1StructWrapper>(), filenameToDump);
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
                dataReadFromFile = DeSerializeObject<RF1StructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                RF1StructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
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
            if (dumpToFile)
            {
                dataToDump = new List<RF1StructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedFile = MemoryMappedFile.OpenExisting(rFactor1Constant.SharedMemoryName);
                        sharedmemorysize = Marshal.SizeOf(typeof(rfShared));
                        sharedMemoryReadBuffer = new byte[sharedmemorysize];
                        initialised = true;
                        Console.WriteLine("Initialised rFactor 1 shared memory");
                    }
                    catch (Exception)
                    {
                        initialised = false;
                    }
                }
                return initialised;
            }
        }

        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                rfShared _rf1apistruct = new rfShared();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemorysize);
                        handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        _rf1apistruct = (rfShared)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(rfShared));
                        handle.Free();
                    }
                    RF1StructWrapper structWrapper = new RF1StructWrapper();
                    structWrapper.ticksWhenRead = DateTime.UtcNow.Ticks;
                    structWrapper.data = _rf1apistruct;
                    if (!forSpotter && dumpToFile && dataToDump != null)
                    {
                        dataToDump.Add(structWrapper);
                    }
                    return structWrapper;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }

        private rfVehicleInfo[] getPopulatedVehicleInfoArray(rfVehicleInfo[] raw)
        {
            List<rfVehicleInfo> populated = new List<rfVehicleInfo>();
            foreach (rfVehicleInfo rawData in raw)
            {
                if (rawData.place > 0)
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

        public override void Dispose()
        {
            if (memoryMappedFile != null)
            {
                try
                {
                    memoryMappedFile.Dispose();
                    memoryMappedFile = null;
                }
                catch (Exception) { }
            }
            initialised = false;
        }
    }
}
