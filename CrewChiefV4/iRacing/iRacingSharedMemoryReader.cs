using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRSDKSharp;

namespace CrewChiefV4.iRacing
{
    class iRacingSharedMemoryReader : GameDataReader
    {

        iRacingSDK sdk = new iRacingSDK();
        Sim sim = new Sim();
        private Boolean initialised = false;
        private List<iRacingStructWrapper> dataToDump;
        private iRacingStructWrapper[] dataReadFromFile = null;
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

        public TelemetryValue<T> GetTelemetryValue<T>(string name)
        {
            return new TelemetryValue<T>(sdk, name);
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
                            var sessionInfoString = sdk.GetSessionInfo();
                            SessionInfo sessionInfo = new SessionInfo(sessionInfoString, (double)time);
                            // Raise the SessionInfoUpdated event and pass along the session info and session time.
                            sim.SdkOnSessionInfoUpdated(sessionInfo, (int)TryGetSessionNum(),DriverId);
                            lastUpdate = newUpdate;
                        }
                    }
                    sim.SdkOnTelemetryUpdated(new TelemetryInfo(sdk));
                    structWrapper.data = sim;                 
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
        private void DisconnectInternal()
        {
            // This needs to be synchronized, because disconnection happens from CrewChief.Run and MainWindow.Dispose.
            lock (this)
            {
                
                sdk.Shutdown();
                initialised = false;
                Console.WriteLine("Disconnected from iRacing Shared Memory");
            }
        }
        public override void Dispose()
        {
            lock (this)
            {
                
                sdk.Shutdown();
                initialised = false;
                Console.WriteLine("Disconnected from iRacing Shared Memory");
            }
            
        }
    }
}
