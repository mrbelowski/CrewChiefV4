using System;
using System.Collections.Generic;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Xml;
using System.Xml.Serialization;
using CrewChiefV4.ACC;
using CrewChiefV4.ACC.Data;
using System.Threading;

namespace CrewChiefV4.ACC
{
    public class ACCSharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedCrewChiefFile;
        private int sharedmemoryCrewChiefsize;
        private byte[] sharedMemoryCrewChiefReadBuffer;
        private GCHandle handleCrewChief;


        private Boolean initialised = false;
        private List<ACCStructWrapper> dataToDump;
        private ACCStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;

        public class ACCStructWrapper
        {
            public long ticksWhenRead;
            public ACCSharedMemoryData data;

        }
        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                foreach (ACCStructWrapper wrapper in dataToDump)
                {
                    wrapper.data.opponentDrivers = getPopulatedDriverDataArray(wrapper.data.opponentDrivers);
                }
                SerializeObject(dataToDump.ToArray<ACCStructWrapper>(), filenameToDump);
            }
        }

        public override void ResetGameDataFromFile()
        {
            dataReadFromFileIndex = 0;
        }

        public override Object ReadGameDataFromFile(String filename, int pauseBeforeStart )
        {

            if (dataReadFromFile == null || filename != lastReadFileName)
            {
                dataReadFromFileIndex = 0;
                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<ACCStructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                ACCStructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
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
                dataToDump = new List<ACCStructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedCrewChiefFile = MemoryMappedFile.OpenExisting(ACCConstant.SharedMemoryName);
                        sharedmemoryCrewChiefsize = Marshal.SizeOf(typeof(ACCSharedMemoryData));
                        sharedMemoryCrewChiefReadBuffer = new byte[sharedmemoryCrewChiefsize];

                        initialised = true;
                        Console.WriteLine("Initialised Assetto Corsa Competizione shared memory");
                    }
                    catch (Exception)
                    {
                        initialised = false;
                    }
                }
                return initialised;
            }
        }
        public static String getNameFromBytes(byte[] name)
        {
            return Encoding.Unicode.GetString(name);
        }
        public override Object ReadGameData(Boolean forSpotter)
        {
            lock (this)
            {
                ACCSharedMemoryData accShared = new ACCSharedMemoryData();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    using (var sharedMemoryStreamView = memoryMappedCrewChiefFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryCrewChiefReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemoryCrewChiefsize);
                        handleCrewChief = GCHandle.Alloc(sharedMemoryCrewChiefReadBuffer, GCHandleType.Pinned);
                        accShared = (ACCSharedMemoryData)Marshal.PtrToStructure(handleCrewChief.AddrOfPinnedObject(), typeof(ACCSharedMemoryData));
                        handleCrewChief.Free();
                    }

                    ACCStructWrapper structWrapper = new ACCStructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                    structWrapper.data = accShared;

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
        private Driver[] getPopulatedDriverDataArray(Driver[] raw)
        {
            List<Driver> populated = new List<Driver>();
            foreach (Driver rawData in raw)
            {
                populated.Add(rawData);
            }
            if (populated.Count == 0)
            {
                populated.Add(raw[0]);
            }
            return populated.ToArray();
        }

        public override void Dispose()
        {
            if (memoryMappedCrewChiefFile != null)
            {
                try
                {
                    memoryMappedCrewChiefFile.Dispose();
                }
                catch (Exception) { }
            }
        }
    }
}
