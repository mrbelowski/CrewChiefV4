using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.PCars2
{
    public class PCars2SharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private Boolean initialised = false;
        private List<PCars2StructWrapper> dataToDump;
        private PCars2StructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;
        private long tornFramesCount = 0;
        
        public class PCars2StructWrapper
        {
            public long ticksWhenRead;
            public pCars2APIStruct data;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<PCars2StructWrapper>(), filenameToDump);
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
                dataReadFromFile = DeSerializeObject<PCars2StructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                PCars2StructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
                dataReadFromFileIndex++;
                return structWrapperData;
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
                dataToDump = new List<PCars2StructWrapper>();
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
                        Console.WriteLine("Initialised pcars2 shared memory");
                    }
                    catch (Exception)
                    {
                        Console.WriteLine("Found PCars2 process, but can't find PCars2 shared memory - check the game isn't running in PCars1 mode");
                        initialised = false;
                    }
                }
                return initialised;
            }            
        }

        public override void stop()
        {
            Console.WriteLine("Stopped reading pcars data, discarded " + tornFramesCount + " torn frames");
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
                    PCars2StructWrapper structWrapper = new PCars2StructWrapper();
                    structWrapper.ticksWhenRead = DateTime.UtcNow.Ticks;
                    structWrapper.data = _pcarsapistruct;
                    if (!forSpotter && dumpToFile && dataToDump != null && _pcarsapistruct.mTrackLocation != null &&
                        _pcarsapistruct.mTrackLocation.Length > 0)
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
