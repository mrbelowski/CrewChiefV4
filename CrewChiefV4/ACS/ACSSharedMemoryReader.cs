using CrewChiefV4.assetto;
using CrewChiefV4.assetto.assettoData;
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


namespace CrewChiefV4.assetto
{
    public class ACSSharedMemoryReader : GameDataReader
    {
        private MemoryMappedFile memoryMappedPhysicsFile;
        private int sharedmemoryPhysicssize;
        private byte[] sharedMemoryPhysicsReadBuffer;
        private GCHandle handlePhysics;

        private MemoryMappedFile memoryMappedGraphicFile;
        private int sharedmemoryGraphicsize;
        private byte[] sharedMemoryGraphicReadBuffer;
        private GCHandle handleGraphic;

        private MemoryMappedFile memoryMappedStaticFile;
        private int sharedmemoryStaticsize;
        private byte[] sharedMemoryStaticReadBuffer;

        private GCHandle handleStatic;

        private MemoryMappedFile memoryMappedCrewChiefFile;
        private int sharedmemoryCrewChiefsize;
        private byte[] sharedMemoryCrewChiefReadBuffer;
        private GCHandle handleCrewChief;


        private Boolean initialised = false;
        private List<ACSStructWrapper> dataToDump;
        private ACSStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;

        public class ACSStructWrapper
        {
            public long ticksWhenRead;
            public AssettoCorsaShared data;

        }
        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                foreach (ACSStructWrapper wrapper in dataToDump)
                {
                    wrapper.data.acsChief.vehicle = getPopulatedDriverDataArray(wrapper.data.acsChief.vehicle);
                }
                SerializeObject(dataToDump.ToArray<ACSStructWrapper>(), filenameToDump);
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
                dataReadFromFile = DeSerializeObject<ACSStructWrapper[]>(filePathResolved);
                lastReadFileName = filename;
                Thread.Sleep(pauseBeforeStart);
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                ACSStructWrapper structWrapperData = dataReadFromFile[dataReadFromFileIndex];
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
                dataToDump = new List<ACSStructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        memoryMappedPhysicsFile = MemoryMappedFile.OpenExisting(assettoConstant.SharedMemoryNamePhysics);
                        sharedmemoryPhysicssize = Marshal.SizeOf(typeof(SPageFilePhysics));
                        sharedMemoryPhysicsReadBuffer = new byte[sharedmemoryPhysicssize];

                        memoryMappedGraphicFile = MemoryMappedFile.OpenExisting(assettoConstant.SharedMemoryNameGraphic);
                        sharedmemoryGraphicsize = Marshal.SizeOf(typeof(SPageFileGraphic));
                        sharedMemoryGraphicReadBuffer = new byte[sharedmemoryGraphicsize];

                        memoryMappedStaticFile = MemoryMappedFile.OpenExisting(assettoConstant.SharedMemoryNameStatic);
                        sharedmemoryStaticsize = Marshal.SizeOf(typeof(SPageFileStatic));
                        sharedMemoryStaticReadBuffer = new byte[sharedmemoryStaticsize];
                        
                        memoryMappedCrewChiefFile = MemoryMappedFile.OpenExisting(assettoConstant.SharedMemoryNameCrewChief);
                        sharedmemoryCrewChiefsize = Marshal.SizeOf(typeof(SPageFileCrewChief));
                        sharedMemoryCrewChiefReadBuffer = new byte[sharedmemoryCrewChiefsize];

                        initialised = true;
                        Console.WriteLine("Initialised Assetto Corsa 1 shared memory");
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
                AssettoCorsaShared acsShared = new AssettoCorsaShared();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    if(!forSpotter)
                    {
                        using (var sharedMemoryStreamView = memoryMappedStaticFile.CreateViewStream())
                        {
                            BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                            sharedMemoryStaticReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemoryStaticsize);
                            handleStatic = GCHandle.Alloc(sharedMemoryStaticReadBuffer, GCHandleType.Pinned);
                            acsShared.acsStatic = (SPageFileStatic)Marshal.PtrToStructure(handleStatic.AddrOfPinnedObject(), typeof(SPageFileStatic));
                            handleStatic.Free();
                        }
                    }
                    using (var sharedMemoryStreamView = memoryMappedGraphicFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryGraphicReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemoryGraphicsize);
                        handleGraphic = GCHandle.Alloc(sharedMemoryGraphicReadBuffer, GCHandleType.Pinned);
                        acsShared.acsGraphic = (SPageFileGraphic)Marshal.PtrToStructure(handleGraphic.AddrOfPinnedObject(), typeof(SPageFileGraphic));
                        handleGraphic.Free();
                    }
                    using (var sharedMemoryStreamView = memoryMappedPhysicsFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryPhysicsReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemoryPhysicssize);
                        handlePhysics = GCHandle.Alloc(sharedMemoryPhysicsReadBuffer, GCHandleType.Pinned);
                        acsShared.acsPhysics = (SPageFilePhysics)Marshal.PtrToStructure(handlePhysics.AddrOfPinnedObject(), typeof(SPageFilePhysics));
                        handlePhysics.Free();
                    }
                    using (var sharedMemoryStreamView = memoryMappedCrewChiefFile.CreateViewStream())
                    {
                        BinaryReader _SharedMemoryStream = new BinaryReader(sharedMemoryStreamView);
                        sharedMemoryCrewChiefReadBuffer = _SharedMemoryStream.ReadBytes(sharedmemoryCrewChiefsize);
                        handleCrewChief = GCHandle.Alloc(sharedMemoryCrewChiefReadBuffer, GCHandleType.Pinned);
                        acsShared.acsChief = (SPageFileCrewChief)Marshal.PtrToStructure(handleCrewChief.AddrOfPinnedObject(), typeof(SPageFileCrewChief));
                        handleCrewChief.Free();
                    }

                    ACSStructWrapper structWrapper = new ACSStructWrapper();
                    structWrapper.ticksWhenRead = DateTime.UtcNow.Ticks;
                    structWrapper.data = acsShared;

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
        private acsVehicleInfo[] getPopulatedDriverDataArray(acsVehicleInfo[] raw)
        {
            List<acsVehicleInfo> populated = new List<acsVehicleInfo>();
            foreach (acsVehicleInfo rawData in raw)
            {
                if (rawData.carLeaderboardPosition > 0)
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
            if (memoryMappedPhysicsFile != null)
            {
                try
                {
                    memoryMappedPhysicsFile.Dispose();
                    memoryMappedPhysicsFile = null;
                }
                catch (Exception) { }
            }
            if (memoryMappedGraphicFile != null)
            {
                try
                {
                    memoryMappedGraphicFile.Dispose();
                    memoryMappedGraphicFile = null;
                }
                catch (Exception) { }
            }
            if (memoryMappedStaticFile != null)
            {
                try
                {
                    memoryMappedStaticFile.Dispose();
                    memoryMappedStaticFile = null;
                }
                catch (Exception) { }
            }
            if (memoryMappedCrewChiefFile != null)
            {
                try
                {
                    memoryMappedCrewChiefFile.Dispose();
                    memoryMappedCrewChiefFile = null;
                }
                catch (Exception) { }
            }
            initialised = false;
        }
    }
}