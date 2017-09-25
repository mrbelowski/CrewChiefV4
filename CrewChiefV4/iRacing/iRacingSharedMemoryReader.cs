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
        private iRacingConnection iracingConnection = new iRacingConnection();
        bool doOnce = false;
        bool connected = false;
        void eventDisconnected()
        {
            connected = false;
            //Console.WriteLine("Notified by iRacingConnection of application data disconnection from event handler");
        }

        void eventConnected()
        {
            connected = true;
            //Console.WriteLine("Notified by iRacingConnection of application data connection from event handler");
        }


        
        
        public class iRacingStructWrapper
        {
            public long ticksWhenRead;
            public DataSample data;
            public iRacingConnection iracingConnection;
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
                        if (!doOnce)
                        {
                            iracingConnection.Connected += eventConnected;
                            iracingConnection.Disconnected += eventDisconnected;
                            doOnce = true;
                        }

                        if (iracingConnection.GetDataFeed(logging: false).First().IsConnected)
                        {
                            initialised = true;
                            Console.WriteLine("Initialised iRacing Shared Memory");
                        }     
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

                if (!initialised)
                {
                    if (!InitialiseInternal())
                    {
                        throw new GameDataReadException("Failed to initialise shared memory");
                    }
                }
                try
                {
                    if(!connected)
                    {
                        return null;
                    }
                    IEnumerable<DataSample> iRacingShared = iracingConnection.GetDataFeed(logging: false); //WithFinishingStatus().WithCorrectedDistances().WithCorrectedPercentages().WithCurrentLapTime().First();
                    if(iRacingShared.First().IsConnected)
                    {
                        iRacingStructWrapper structWrapper = new iRacingStructWrapper();
                        structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                        structWrapper.data = iRacingShared.WithFinishingStatus().WithCorrectedDistances().WithCorrectedPercentages().WithCurrentLapTime().First();
                        structWrapper.iracingConnection = iracingConnection;

                        if (!forSpotter && dumpToFile && dataToDump != null)
                        {

                            dataToDump.Add(structWrapper);
                        }
                        return structWrapper;
                    }
                    return null;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }
        public override void DisconnectFromProcess()
        {
            Dispose();
            return;
        }
        public override void stop()
        {
            Dispose();
            return;
        }
        public override void Dispose()
        {
            lock (this)
            {
                initialised = false;
                connected = false;
            }
            return;
        }
    }
}
