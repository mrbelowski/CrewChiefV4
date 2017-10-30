using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.PCars2
{
    public class PCars2SharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private Boolean initialised = false;
        private List<PCars2RawStructWrapper> dataToDump;
        private PCars2RawStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;
        private long tornFramesCount = 0;

        public class PCars2RawStructWrapper
        {
            public long ticksWhenRead;
            public byte[] data;
        }

        public class PCars2StructWrapper
        {
            public long ticksWhenRead;
            public pCars2APIStruct data;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<PCars2RawStructWrapper>(), filenameToDump);
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
                dataReadFromFile = DeSerializeObject<PCars2RawStructWrapper[]>(dataFilesPath + filename);
                lastReadFileName = filename;
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                PCars2RawStructWrapper rawStructWrapperData = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                PCars2StructWrapper wrapperToReturn = new PCars2StructWrapper();
                wrapperToReturn.ticksWhenRead = rawStructWrapperData.ticksWhenRead;
                wrapperToReturn.data = BytesToStructure(rawStructWrapperData.data);
                return wrapperToReturn;
            }
            else
            {
                return null;
            }
        }

        public pCars2APIStruct BytesToStructure(byte[] bytes)
        {
            IntPtr ptr = Marshal.AllocHGlobal(bytes.Length);
            try
            {
                Marshal.Copy(bytes, 0, ptr, bytes.Length);
                return (pCars2APIStruct)Marshal.PtrToStructure(ptr, typeof(pCars2APIStruct));
            }
            finally
            {
                Marshal.FreeHGlobal(ptr);
            }
        }

        protected override Boolean InitialiseInternal()
        {
            if (dumpToFile)
            {
                dataToDump = new List<PCars2RawStructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedFile = MemoryMappedFile.OpenExisting("$pcars2$");
                        sharedmemorysize = Marshal.SizeOf(typeof(pCars2APIStruct));
                        sharedMemoryReadBuffer = new byte[sharedmemorysize];
                        initialised = true;
                        Console.WriteLine("Initialised pcars shared memory");
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine(e.Message);
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
                pCars2APIStruct _pcarsapistruct = new pCars2APIStruct();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    int retries = -1;
                    do {
                        retries++;
                        using (var sharedMemoryStreamView = memoryMappedFile.CreateViewStream())
                        {
                            BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                            sharedMemoryReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemorysize);
                            GCHandle handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                            try
                            {
                                _pcarsapistruct = (pCars2APIStruct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(pCars2APIStruct));
                            }
                            finally
                            {
                                handle.Free();
                            }
                        }
                    } while (_pcarsapistruct.mSequenceNumber % 2 != 0);
                    tornFramesCount += retries;
                    long now = DateTime.Now.Ticks;
                    if (!forSpotter && dumpToFile && dataToDump != null && _pcarsapistruct.mTrackLocation != null &&
                        _pcarsapistruct.mTrackLocation.Length > 0)
                    {
                        PCars2RawStructWrapper rawStructWrapper = new PCars2RawStructWrapper();
                        rawStructWrapper.ticksWhenRead = now;
                        rawStructWrapper.data = sharedMemoryReadBuffer;
                        dataToDump.Add(rawStructWrapper);
                    }
                    PCars2StructWrapper structWrapper = new PCars2StructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                    structWrapper.data = _pcarsapistruct;
                    return structWrapper;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }
            }            
        }

        public override void Dispose()
        {
            if (memoryMappedFile != null)
            {
                try
                {
                    memoryMappedFile.Dispose();
                }
                catch (Exception) { }
            }
        }
    }
}
