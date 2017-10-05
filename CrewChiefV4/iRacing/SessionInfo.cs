using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using iRSDKSharp;
using YamlDotNet.RepresentationModel;
namespace CrewChiefV4.iRacing
{
    public class SessionInfo
    {
        public SessionInfo(string yaml, double updateTime)
        {
            _updateTime = updateTime;

            _rawYaml = yaml;

            this.FixYaml(yaml);
            this.ParseYaml();
        }

        #region Properties

        public double _updateTime;
        /// <summary>
        /// The time of this update.
        /// </summary>
        public double UpdateTime { get { return _updateTime; } }

        public string _yaml;
        /// <summary>
        /// The YAML string representing the session info, modified to ensure correct parsing.
        /// </summary>
        public string Yaml { get { return _yaml; } }

        public string _rawYaml;
        /// <summary>
        /// The raw YAML string as originally returned from the sim.
        /// </summary>
        public string RawYaml { get { return _rawYaml; } }

        public bool _isValidYaml;
        public bool IsValidYaml { get { return _isValidYaml; } }

        public YamlStream _yamlStream;
        public YamlStream YamlStream { get { return _yamlStream; } }

        public YamlMappingNode _yamlRoot;
        public YamlMappingNode YamlRoot { get { return _yamlRoot; } }

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

            // Incorrect setup info dump fix: remove the setup info
            var indexOfSetup = _yaml.IndexOf("CarSetup:");
            if (indexOfSetup > 0)
            {
                _yaml = _yaml.Substring(0, indexOfSetup);
            }
        }

        public void ParseYaml()
        {
            try
            {
                using (var reader = new StringReader(this.Yaml))
                {
                    _yamlStream = new YamlStream();
                    _yamlStream.Load(reader);
                    _yamlRoot = (YamlMappingNode)_yamlStream.Documents[0].RootNode;
                }
                _isValidYaml = true;
            }
            catch (Exception ex)
            {
                _isValidYaml = false;
            }

        }

        public YamlQuery this[string key]
        {
            get
            {
                return YamlQuery.Mapping(_yamlRoot, key);
            }
        }

        /// <summary>
        /// Gets a value from the session info YAML, or null if there is an error.
        /// </summary>
        /// <param name="query">The YAML query path to the value.</param>
        public string TryGetValue(string query)
        {
            if (!this.IsValidYaml) return null;
            try
            {
                return YamlParser.Parse(_yaml, query);
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Gets a value from the session info YAML. Returns true if successfull, false if there is an error.
        /// </summary>
        /// <param name="query">The YAML query path to the value.</param>
        /// <param name="value">When this method returns, contains the requested value if the query was valid, or null if the query was invalid.</param>
        public bool TryGetValue(string query, out string value)
        {
            if (!this.IsValidYaml)
            {
                value = null;
                return false;
            }
            try
            {
                value = YamlParser.Parse(_yaml, query);
                return true;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }

        /// <summary>
        /// Gets a value from the session info YAML. May throw an exception. Use <see cref="TryGetValue"/> for safer operation.
        /// </summary>
        /// <param name="query">The YAML query path to the value.</param>
        public string GetValue(string query)
        {
            if (!this.IsValidYaml) return null;
            return YamlParser.Parse(_yaml, query);
        }

        #endregion
    }
    
}
