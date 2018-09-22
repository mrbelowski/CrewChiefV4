using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace CrewChiefV4.PCars
{
    public class PCarsSharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedFile;
        private int sharedmemorysize;
        private byte[] sharedMemoryReadBuffer;
        private Boolean initialised = false;
        private List<PCarsStructWrapper> dataToDump;
        private PCarsStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;

        public class PCarsStructWrapper
        {
            public long ticksWhenRead;
            public pCarsAPIStruct data;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<PCarsStructWrapper>(), filenameToDump);
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
                dataReadFromFile = DeSerializeObject<PCarsStructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                PCarsStructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
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
                dataToDump = new List<PCarsStructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedFile = MemoryMappedFile.OpenExisting("$pcars$");
                        sharedmemorysize = Marshal.SizeOf(typeof(pCarsAPIStruct));
                        sharedMemoryReadBuffer = new byte[sharedmemorysize];
                        initialised = true;
                        Console.WriteLine("Initialised pcars shared memory");
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
                pCarsAPIStruct _pcarsapistruct = new pCarsAPIStruct();
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
                        GCHandle handle = GCHandle.Alloc(sharedMemoryReadBuffer, GCHandleType.Pinned);
                        try
                        {
                            _pcarsapistruct = (pCarsAPIStruct)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(pCarsAPIStruct));
                        }
                        finally
                        {
                            handle.Free();
                        }
                    }
                    PCarsStructWrapper structWrapper = new PCarsStructWrapper();
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
