using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrewChiefV4
{
    public class Configuration
    {
        private static String UI_TEXT_FILENAME = "ui_text.txt";
        private static String SPEECH_RECOGNITION_CONFIG_FILENAME = "speech_recognition_config.txt";
        private static String SOUNDS_CONFIG_FILENAME = "sounds_config.txt";

        private static Dictionary<String, String> UIStrings = LoadUIStrings();
        private static Dictionary<String, String> SpeechRecognitionConfig = LoadSpeechRecognitionConfig();
        private static Dictionary<String, String> SoundsConfig = LoadSoundsConfig();

        public static String getUIString(String key) {
            string uiString = null;
            if (UIStrings.TryGetValue(key, out uiString)) {
                return uiString;
            }
            return key;
        }

        public static String getUIStringStrict(String key)
        {
            string uiString = null;
            if (UIStrings.TryGetValue(key, out uiString))
            {
                return uiString;
            }
            return null;
        }

        public static String getSoundConfigOption(String key)
        {
            string soundConfig = null;
            if (SoundsConfig.TryGetValue(key, out soundConfig))
            {
                return soundConfig;
            }
            return key;
        }

        public static String getSpeechRecognitionConfigOption(String key)
        {
            string sreConfig = null;
            if (SpeechRecognitionConfig.TryGetValue(key, out sreConfig))
            {
                return sreConfig;
            }
            return key;
        }

        public static String[] getSpeechRecognitionPhrases(String key)
        {
            string options = null;
            if (SpeechRecognitionConfig.TryGetValue(key, out options))
            {
                if (options.Contains(":"))
                {
                    List<String> phrasesList = new List<string>();
                    var phrases = options.Split(':');
                    for (int i = 0; i < phrases.Length; ++i)
                    {
                        String phrase = phrases[i].Trim();
                        if (phrase.Length > 0)
                        {
                            phrasesList.Add(phrase);
                        }
                    }
                    return phrasesList.ToArray();
                }
                else
                {
                    return new String[] {options};
                }
            }
            return new String[] {};
        }

        public static String getDefaultFileLocation(String filename) {
            if (CrewChief.Debugging)
            {
                return Application.StartupPath + @"\..\..\" + filename;
            }
            else
            {
                return Application.StartupPath + @"\" + filename;
            }
        }

        public static String getUserOverridesFileLocation(String filename)
        {
            return Environment.ExpandEnvironmentVariables(@"%USERPROFILE%\AppData\Local\CrewChiefV4\" + filename);
        }

        private static void merge(StreamReader file, Dictionary<String, String> dict)
        {
            String line;
            while ((line = file.ReadLine()) != null)
            {
                if (!line.StartsWith("#") && line.Contains("="))
                {
                    try
                    {
                        String[] split = line.Split('=');
                        String key = split[0].Trim();
                        dict.Remove(key);
                        dict.Add(split[0].Trim(), split[1].Trim());
                    }
                    catch (Exception)
                    {
                    }
                }
            }
        }

        private static Dictionary<string, string> LoadConfigHelper(string configFileName)
        {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            StreamReader file = null;
            try
            {
                file = new StreamReader(getDefaultFileLocation(configFileName));
                merge(file, dict);
            }
            catch (Exception)
            {
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
            StreamReader overridesFile = null;
            try
            {
                var overrideFileName = getUserOverridesFileLocation(configFileName);
                if (File.Exists(overrideFileName))
                {
                    overridesFile = new StreamReader(overrideFileName);
                    merge(overridesFile, dict);
                }
            }
            catch (Exception)
            {

            }
            finally
            {
                if (overridesFile != null)
                {
                    overridesFile.Close();
                }
            }
            return dict;
        }

        private static Dictionary<String, String> LoadSpeechRecognitionConfig()
        {
            return LoadConfigHelper(SPEECH_RECOGNITION_CONFIG_FILENAME);
        }

        private static Dictionary<String, String> LoadSoundsConfig()
        {
            return LoadConfigHelper(SOUNDS_CONFIG_FILENAME);
        }

        private static Dictionary<String, String> LoadUIStrings()
        {
            return LoadConfigHelper(UI_TEXT_FILENAME);
        }
    }
}
