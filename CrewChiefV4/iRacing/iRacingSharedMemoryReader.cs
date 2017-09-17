using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRacingSDK;

namespace CrewChiefV4.iRacing
{
    public class iRacingSharedMemoryReader : GameDataReader
    {


        private Boolean initialised = false;
        private List<iRacingStructWrapper> dataToDump;
        private iRacingStructWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;
        private iRacingConnection iRacingConnection = null;
        
        public class iRacingStructWrapper
        {
            public long ticksWhenRead;
            public DataSample data;
            public iRacingConnection iRacingConnection;
        }

        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<iRacingStructWrapper>(), filenameToDump);
            }
        }

        public override void ResetGameDataFromFile()
        {
            dataReadFromFileIndex = 0;
        }

        public override Object ReadGameDataFromFile(String filename)
        {
            return null;
        }

        protected override Boolean InitialiseInternal()
        {
            if (dumpToFile)
            {
                dataToDump = new List<iRacingStructWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        initialised = true;
                        Console.WriteLine("Initialised iRacing Shared Memory");
                        iRacingConnection = new iRacingConnection();                        
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
                DataSample iRacingShared = new DataSample();
                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    iRacingShared = iRacingConnection.GetDataFeed().First();
                    iRacingStructWrapper structWrapper = new iRacingStructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                    structWrapper.data = iRacingShared;
                    structWrapper.iRacingConnection = iRacingConnection;
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

        public override void Dispose()
        {
            return;
        }
    }
}
