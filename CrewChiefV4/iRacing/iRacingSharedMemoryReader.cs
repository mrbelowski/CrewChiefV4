using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRSDKSharp;
using System.Xml;
using System.Xml.Serialization;

namespace CrewChiefV4.iRacing
{
    public class iRacingSharedMemoryReader : GameDataReader
    {

        private iRacingSDK sdk = null;
        private Sim sim = null; 
        private Boolean initialised = false;
        private List<iRacingStructDumpWrapper> dataToDump;
        private iRacingStructDumpWrapper[] dataReadFromFile = null;
        private int dataReadFromFileIndex = 0;
        private String lastReadFileName = null;
        int lastUpdate = -1;
        private int _DriverId = -1;
        public int DriverId { get { return _DriverId; } }

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
            if(sim == null)
            {
                sim = new Sim();
            }
            if (dataReadFromFile == null || filename != lastReadFileName)
            {
                dataReadFromFileIndex = 0;
                var filePathResolved = Utilities.ResolveDataFile(this.dataFilesPath, filename);
                dataReadFromFile = DeSerializeObject<iRacingStructDumpWrapper[]>(filePathResolved);
                lastReadFileName = filename;
            }
            if (dataReadFromFile != null && dataReadFromFile.Length > dataReadFromFileIndex)
            {
                bool IsNewSession = false;
                iRacingStructDumpWrapper structDumpWrapperData = dataReadFromFile[dataReadFromFileIndex];
                if (structDumpWrapperData.data.SessionInfoUpdate != lastUpdate && structDumpWrapperData.data.SessionInfo.Length > 0)
                {
                    SessionInfo sessionInfo = new SessionInfo(structDumpWrapperData.data.SessionInfo);
                    IsNewSession = sim.SdkOnSessionInfoUpdated(sessionInfo, structDumpWrapperData.data.SessionNum, structDumpWrapperData.data.PlayerCarIdx, structDumpWrapperData.data.SessionInfoUpdate);
                    lastUpdate = structDumpWrapperData.data.SessionInfoUpdate;
                }
                sim.SdkOnTelemetryUpdated(structDumpWrapperData.data);
                iRacingStructWrapper structWrapperData = new iRacingStructWrapper() { data = sim, ticksWhenRead = structDumpWrapperData.ticksWhenRead };
                structWrapperData.data.Telemetry.IsNewSession = IsNewSession;
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
                        if (sdk == null)
                        {
                            sdk = new iRacingSDK();
                        }
                        sdk.Shutdown();

                        if (!sdk.IsInitialized)
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

                    if (sdk.IsConnected())
                    {
                        if(sim == null)
                        {
                            sim = new Sim();
                        }
                        if (forSpotter)
                        {
                            return (int)sdk.GetData("CarLeftRight");
                        }

                        _DriverId = (int)sdk.GetData("PlayerCarIdx");

                        int newUpdate = sdk.Header.SessionInfoUpdate;
                        bool hasNewSessionData = false;
                        bool isNewSession = false;
                        if (newUpdate != lastUpdate)
                        {
                            // Get the session info string
                            SessionInfo sessionInfo = new SessionInfo(sdk.GetSessionInfoString());
                            // Raise the SessionInfoUpdated event and pass along the session info and session time.
                            isNewSession = sim.SdkOnSessionInfoUpdated(sessionInfo, (int)TryGetSessionNum(), DriverId, newUpdate);
                            lastUpdate = newUpdate;
                            hasNewSessionData = true;
                        }
                        iRacingData irData = new iRacingData(sdk, hasNewSessionData && dumpToFile, isNewSession);

                        sim.SdkOnTelemetryUpdated(irData);

                        iRacingStructWrapper structWrapper = new iRacingStructWrapper();
                        structWrapper.ticksWhenRead = DateTime.Now.Ticks;
                        structWrapper.data = sim;

                        if (dumpToFile && dataToDump != null)
                        {
                            dataToDump.Add(new iRacingStructDumpWrapper() { ticksWhenRead = structWrapper.ticksWhenRead, data = irData });
                        }
                        return structWrapper;
                    }
                    else
                    {
                        return null;
                    }                    
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
                if (sdk != null)
                {
                    sdk.Shutdown();
                    sdk = null;
                }
                if(sim != null)
                {
                    sim = null;
                }
                
                if (initialised)
                {
                    lastUpdate = -1;
                    _DriverId = -1;
                    initialised = false;
                    Console.WriteLine("Disconnected from iRacing Shared Memory");
                }

            }

        }
    }
}
