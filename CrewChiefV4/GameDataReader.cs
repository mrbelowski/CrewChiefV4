using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Serialization;

namespace CrewChiefV4
{
    public abstract class GameDataReader
    {
        protected String filenameToDump;
        
        public Boolean dumpToFile = false;        

        protected abstract Boolean InitialiseInternal();

        public abstract Object ReadGameData(Boolean forSpotter);
                
        public abstract void Dispose();

        public abstract void DumpRawGameData();

        public abstract Object ReadGameDataFromFile(String filename);

        public abstract void ResetGameDataFromFile();

        protected String dataFilesPath;

        public Boolean Initialise()
        {
            Console.WriteLine("initialising");
            if (System.Diagnostics.Debugger.IsAttached)
            {
                dataFilesPath = Path.Combine(Path.GetDirectoryName(System.Reflection.Assembly.GetEntryAssembly().Location), @"..\", @"..\dataFiles\");
            }
            else
            {
                dataFilesPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "debugLogs");
                try
                {
                    System.IO.Directory.CreateDirectory(dataFilesPath);
                }
                catch (Exception)
                {
                    Console.WriteLine("Unable to create folder for data file, no session record will be available");
                    dumpToFile = false;
                }
            }
            Boolean initialised = InitialiseInternal();
            if (initialised && dumpToFile)
            {
                filenameToDump = dataFilesPath + "\\recording_" + DateTime.Now.ToString("yyyy-dd-M--HH-mm-ss") + ".xml";
                Console.WriteLine("session recording will be dumped to file: " + filenameToDump);
            }
            return initialised;
        }

        protected void SerializeObject<T>(T serializableObject, string fileName)
        {
            if (serializableObject == null) { return; }

            try
            {
                Console.WriteLine("About to dump game data - this may take a while");
                XmlSerializer serializer = new XmlSerializer(serializableObject.GetType());
                using (FileStream fileStream = new FileStream(fileName, FileMode.Create))
                {
                    serializer.Serialize(fileStream, serializableObject);
                }
                Console.WriteLine("Done writing session data log to: " + fileName);
                Console.WriteLine("PLEASE RESTART THE APPLICATION BEFORE ATTEMPTING TO RECORD ANOTHER SESSION");
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to write raw game data: " + ex.Message);
            }
        }

        protected T DeSerializeObject<T>(string fileName)
        {
            Console.WriteLine("About to load recorded game data from file " + fileName + " - this may take a while");
            if (string.IsNullOrEmpty(fileName)) { return default(T); }

            T objectOut = default(T);

            try
            {
                using (FileStream fileStream = new FileStream(fileName, FileMode.Open))
                {
                    Type outType = typeof(T);

                    XmlSerializer serializer = new XmlSerializer(outType);
                    using (XmlReader reader = new XmlTextReader(fileStream))
                    {
                        objectOut = (T)serializer.Deserialize(reader);
                    }
                }
                Console.WriteLine("Done reading session data from: " + fileName);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Unable to read raw game data: " + ex.Message);
            }

            return objectOut;
        }

        public virtual Boolean hasNewSpotterData()
        {
            return true;
        }

        public virtual void stop()
        {
            // no op - only implemented by UDP reader
        }
    }
}
