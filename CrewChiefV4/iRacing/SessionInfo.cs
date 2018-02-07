using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRSDKSharp;
namespace CrewChiefV4.iRacing
{
    public class SessionInfo
    {
        public SessionInfo(string yaml)
        {
            this.FixYaml(yaml);
        }

        #region Properties

        public string _yaml;
        /// <summary>
        /// The YAML string representing the session info, modified to ensure correct parsing.
        /// </summary>
        public string Yaml { get { return _yaml; } }

        #endregion

        #region Methods

        public void FixYaml(string yaml)
        {
            // Quick hack: if there's more than 1 colon ":" in a line, keep only the first
            using (var reader = new StringReader(yaml))
            {
                var builder = new StringBuilder();
                string line;
                while ((line = reader.ReadLine()) != null)
                {
                    if (line.Count(c => c == ':') > 1)
                    {
                        var chars = line.ToCharArray();
                        bool foundFirst = false;
                        for (int i = 0; i < chars.Length; i++)
                        {
                            var c = chars[i];
                            if (c == ':')
                            {
                                if (!foundFirst)
                                {
                                    foundFirst = true;
                                    continue;
                                }
                                chars[i] = '-';
                            }
                        }
                        line = new string(chars);
                    }
                    builder.AppendLine(line);
                }
                _yaml = builder.ToString();
            }

            //Remove info that we do not need and that will just cause the dumping of data to be stupid large.
            int indexOfCameraInfo = _yaml.IndexOf("CameraInfo:");            
            int indexOfDriverInfo = _yaml.IndexOf("DriverInfo:");
            //Console.WriteLine("indexOfDriverInfo " + indexOfCameraInfo + " indexOfDriverInfo " + indexOfDriverInfo);
            if(indexOfCameraInfo > 0 && indexOfDriverInfo > 0)
            {
                _yaml = _yaml.Remove(indexOfCameraInfo, indexOfDriverInfo - indexOfCameraInfo);
            }
            
            // Incorrect setup info dump fix: remove the setup info
            var indexOfSetup = _yaml.IndexOf("CarSetup:");
            if (indexOfSetup > 0)
            {
                _yaml = _yaml.Substring(0, indexOfSetup);
            }
        }
        #endregion
    }
    
}
