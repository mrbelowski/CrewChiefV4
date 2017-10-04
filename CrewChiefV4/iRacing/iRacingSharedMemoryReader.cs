using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRSDKSharp;

namespace CrewChiefV4.iRacing
{
    public class iRacingSharedMemoryReader : GameDataReader
    {

        iRacingSDK sdk = new iRacingSDK();
        Sim sim = new Sim();
        private Boolean initialised = false;
        private List<iRacingStructDumpWrapper> dataToDump;
        private iRacingStructDumpWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;
        int lastUpdate = -1;
        private int _DriverId = -1;
        public int DriverId { get { return _DriverId; } }
        public string sessionInfoString;
        public object GetData(string headerName)
        {
            if (!sdk.IsConnected()) 
                return null;

            return sdk.GetData(headerName);
        }

        private object TryGetSessionNum()
        {
            try
            {
                var sessionnum = sdk.GetData("SessionNum");
                return sessionnum;
            }
            catch
            {
                return null;
            }
        }
        public class iRacingStructWrapper
        {
            public long ticksWhenRead;
            public Sim data;

        }
        public class iRacingStructDumpWrapper
        {
            public long ticksWhenRead;
            public string rewSessionData;
            public iRacingData data;


        }
        public override void DumpRawGameData()
        {
            if (dumpToFile && dataToDump != null && dataToDump.Count > 0 && filenameToDump != null)
            {
                SerializeObject(dataToDump.ToArray<iRacingStructDumpWrapper>(), filenameToDump);
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
                dataReadFromFile = DeSerializeObject<iRacingStructDumpWrapper[]>(dataFilesPath + filename);
                lastReadFileName = filename;
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                iRacingStructDumpWrapper structDumpWrapperData = dataReadFromFile[dataReadFromFileIndex];
                SessionInfo sessionInfo = new SessionInfo(structDumpWrapperData.rewSessionData, (double)structDumpWrapperData.data.SessionTime);
                sim.SdkOnSessionInfoUpdated(sessionInfo, structDumpWrapperData.data.SessionNum, structDumpWrapperData.data.PlayerCarIdx);
                sim.SdkOnTelemetryUpdated(structDumpWrapperData.data);  
                iRacingStructWrapper structWrapperData = new iRacingStructWrapper() { data = sim, ticksWhenRead = structDumpWrapperData.ticksWhenRead };
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
                dataToDump = new List<iRacingStructDumpWrapper>();
            }
            lock (this)
            {
                if (!initialised)
                {
                    try
                    {
                        sdk.Shutdown();

                        if(!sdk.IsInitialized)
                        {
                            sdk.Startup();
                        }
                        if (sdk.IsConnected())
                        {
                            initialised = true;
                            int attempts = 0;
                            const int maxAttempts = 99;

                            object sessionnum = this.TryGetSessionNum();
                            while (sessionnum == null && attempts <= maxAttempts)
                            {
                                attempts++;
                                sessionnum = this.TryGetSessionNum();
                            }
                            if (attempts >= maxAttempts)
                            {
                                Console.WriteLine("Session num too many attempts");
                            }
                            if (this.DriverId == -1)
                            {
                                _DriverId = (int)sdk.GetData("PlayerCarIdx");
                            }
                            Console.WriteLine("Initialised iRacing shared memory");
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

                    iRacingStructWrapper structWrapper = new iRacingStructWrapper();
                    structWrapper.ticksWhenRead = DateTime.Now.Ticks;

                    
                    if(forSpotter)
                    {
                        var carLeftRight = (int)sdk.GetData("CarLeftRight");
                        return carLeftRight;
                    }

                    int newUpdate = sdk.Header.SessionInfoUpdate;                   
                    if (newUpdate != lastUpdate)
                    {

                        var time = sdk.GetData("SessionTime");
                        if(time != null)
                        {
                            // Get the session info string
                            sessionInfoString = (string)sdk.GetSessionInfo();
                            SessionInfo sessionInfo = new SessionInfo(sessionInfoString, (double)time);
                            // Raise the SessionInfoUpdated event and pass along the session info and session time.
                            sim.SdkOnSessionInfoUpdated(sessionInfo, (int)TryGetSessionNum(),DriverId);
                            lastUpdate = newUpdate;
                        }
                    }
                    sim.SdkOnTelemetryUpdated(new iRacingData(sdk));                   
                    structWrapper.data = sim;                
                    if (!forSpotter && dumpToFile && dataToDump != null)
                    {
                        dataToDump.Add(new iRacingStructDumpWrapper() { ticksWhenRead = structWrapper.ticksWhenRead,
                            data = new iRacingData(sdk), rewSessionData=sessionInfoString });
                    }
                    return structWrapper;
                }
                catch (Exception ex)
                {
                    throw new GameDataReadException(ex.Message, ex);
                }
            }
        }
        public override void DisconnectFromProcess()
        {
            this.Dispose();
        }
        public override void Dispose()
        {
            lock (this)
            {
                sim.Reset();
                sdk.Shutdown();
                initialised = false;
                Console.WriteLine("Disconnected from iRacing Shared Memory");
            }
            
        }
    }
}
