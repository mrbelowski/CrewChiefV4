using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Forms;
using Newtonsoft.Json;
using System.Runtime.InteropServices;


namespace CrewChiefV4
{
    class PluginInstaller
    {
        //decided to import instead of "hacking" the ini
        [DllImport("kernel32", CharSet = CharSet.Unicode)]
        private static extern int GetPrivateProfileString(string section, string key,
            string defaultValue, StringBuilder value, int size, string filePath);

        [DllImport("kernel32", CharSet = CharSet.Unicode, SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        private static extern bool WritePrivateProfileString(string section, string key,
            string value, string filePath);

        Boolean messageBoxPresented;
        Boolean messageBoxResult;
        private readonly String rf2PluginFileName = "rFactor2SharedMemoryMapPlugin64.dll";

        public PluginInstaller()
        {
            messageBoxPresented = false;
            messageBoxResult = false;
        }

        public static string ReadValue(string section, string key, string filePath, string defaultValue = "")
        {
            var value = new StringBuilder(512);
            GetPrivateProfileString(section, key, defaultValue, value, value.Capacity, filePath);
            return value.ToString();
        }

        public static bool WriteValue(string section, string key, string value, string filePath)
        {
            bool result = WritePrivateProfileString(section, key, value, filePath);
            return result;
        }

        private string checkMD5(string filename)
        {
            using (var md5 = MD5.Create())
            {
                using (var stream = File.OpenRead(filename))
                {
                    return Encoding.Default.GetString(md5.ComputeHash(stream));
                }
            }
        }

        //referance https://github.com/Jamedjo/RSTabExplorer/blob/master/RockSmithTabExplorer/Services/RocksmithLocator.cs
        private string getSteamFolder()
        {
            string steamInstallPath = "";
            try
            {
                RegistryKey steamKey = Registry.LocalMachine.OpenSubKey(@"Software\Valve\Steam") ?? Registry.LocalMachine.OpenSubKey(@"Software\Wow6432Node\Valve\Steam");
                if (steamKey != null)
                {
                    steamInstallPath = steamKey.GetValue("InstallPath").ToString();
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Exception getting steam folder: " + e.Message);
            }
            return steamInstallPath;
        }

        private List<string> getSteamLibraryFolders()
        {
            List<string> folders = new List<string>();

            string steamFolder = getSteamFolder();
            if (Directory.Exists(steamFolder))
            {
                folders.Add(steamFolder);
                string configFile = Path.Combine(steamFolder, @"config\config.vdf");

                if (File.Exists(configFile))
                {
                    Regex regex = new Regex("BaseInstallFolder[^\"]*\"\\s*\"([^\"]*)\"");
                    using (StreamReader reader = new StreamReader(configFile))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            Match match = regex.Match(line);
                            if (match.Success)
                            {
                                folders.Add(Regex.Unescape(match.Groups[1].Value));
                            }
                        }
                    }
                }

            }
            return folders;
        }

        private Boolean presentInstallMessagebox()
        {
            if (messageBoxPresented == false)
            {
                messageBoxPresented = true;
                if (DialogResult.OK == MessageBox.Show(Configuration.getUIString("install_plugin_popup_text"), Configuration.getUIString("install_plugin_popup_title"),
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                {
                    messageBoxResult = true;
                }
            }
            return messageBoxResult;
        }
        private Boolean presentEnableMessagebox()
        {
            if (DialogResult.OK == MessageBox.Show(Configuration.getUIString("install_plugin_popup_enable_text"), Configuration.getUIString("install_plugin_popup_enable_title"),
                MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
            {
                return true;
            }
            return false;
        }
        //I stole this from the internetz(http://stackoverflow.com/questions/3201598/how-do-i-create-a-file-and-any-folders-if-the-folders-dont-exist)
        private bool installOrUpdatePlugin(string source, string destination)
        {
            try
            {
                string[] files = null;

                if (destination[destination.Length - 1] != Path.DirectorySeparatorChar)
                {
                    destination += Path.DirectorySeparatorChar;
                }

                if (!Directory.Exists(destination))
                {
                    Directory.CreateDirectory(destination);
                }

                files = Directory.GetFileSystemEntries(source);
                foreach (string element in files)
                {
                    // Sub directories                    
                    if (Directory.Exists(element))
                    {
                        installOrUpdatePlugin(element, destination + Path.GetFileName(element));
                    }
                    else
                    {
                        // Files in directory
                        string destinationFile = destination + Path.GetFileName(element);
                        //if the file exists we will check if it needs updating
                        if (File.Exists(destinationFile))
                        {
                            if (!checkMD5(element).Equals(checkMD5(destinationFile)))
                            {
                                //ask the user if they want to update the plugin
                                if (presentInstallMessagebox())
                                {
                                    File.Copy(element, destinationFile, true);
                                    Console.WriteLine("Updated plugin file: " + destinationFile);    
                                }

                            }
                        }
                        else
                        {
                            if (presentInstallMessagebox())
                            {
                                File.Copy(element, destinationFile, true);
                                Console.WriteLine("Installed plugin file: " + destinationFile);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Failed to Copy plugin files: " + e.Message);
                return false;
            }
            return true;
        }

        public void InstallOrUpdatePlugins(GameDefinition gameDefinition)
        {
            //appInstallPath is also used to check if the user allready was asked to update
            string gameInstallPath = "";

            if (gameDefinition.gameEnum == GameEnum.RF2_64BIT)
            {
                gameInstallPath = UserSettings.GetUserSettings().getString("rf2_install_path");
            }
            else if (gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT)
            {
                gameInstallPath = UserSettings.GetUserSettings().getString("acs_install_path");
            }
            else if (gameDefinition.gameEnum == GameEnum.RF1)
            {
                //special case here, will figure something clever out so we dont need to have Dan's dll included in every plugin folder.
                if (gameDefinition.gameInstallDirectory.Equals("Automobilista"))
                {
                    gameInstallPath = UserSettings.GetUserSettings().getString("ams_install_path");
                }
                if (gameDefinition.gameInstallDirectory.Equals("rFactor"))
                {
                    gameInstallPath = UserSettings.GetUserSettings().getString("rf1_install_path");
                }
            }
            //try to get the install folder from steam common install folders.
            if (!Directory.Exists(gameInstallPath))
            {
                //Present a messagebox to the user asking if they want to install plugins
                if (presentInstallMessagebox())
                {
                    List<string> steamLibs = getSteamLibraryFolders();
                    foreach (string lib in steamLibs)
                    {
                        string commonPath = Path.Combine(lib, @"steamapps\common\" + gameDefinition.gameInstallDirectory);
                        if (Directory.Exists(commonPath))
                        {
                            gameInstallPath = commonPath;
                            break;
                        }
                    }
                }
            }
            //Not found in steam folders ask the user to locate the directory
            if (!Directory.Exists(gameInstallPath))
            {
                //Present a messagebox to the user asking if they want to install plugins
                if (presentInstallMessagebox())
                {
                    FolderBrowserDialog dialog = new FolderBrowserDialog();
                    dialog.ShowNewFolderButton = false;
                    dialog.Description = Configuration.getUIString("install_plugin_select_directory_start") + " " +
                        gameDefinition.gameInstallDirectory + " " + Configuration.getUIString("install_plugin_select_directory_end");

                    DialogResult result = dialog.ShowDialog();

                    if (result == DialogResult.OK && dialog.SelectedPath.Length > 0)
                    {
                        
                        //This should now take care of checking against the main .exe instead of the folder name, special case for rFactor 2 as its has the file installed in ..\Bin64
                        if(gameDefinition.gameEnum == GameEnum.RF2_64BIT)                                                
                        {
                            if (File.Exists(Path.Combine(dialog.SelectedPath, @"Bin64", gameDefinition.processName + ".exe")))
                            {
                                gameInstallPath = dialog.SelectedPath;
                            }
                        }
                        else if(File.Exists(Path.Combine(dialog.SelectedPath, gameDefinition.processName + ".exe")))
                        {
                            gameInstallPath = dialog.SelectedPath;
                        }
                        else
                        {
                            //present again if user didn't select the correct folder 
                            InstallOrUpdatePlugins(gameDefinition);
                        }
                    }
                    else if (result == DialogResult.Cancel)
                    {
                        return;
                    }
                }
            }
            //we have a gameInstallPath so we can go on with installation/updating assuming that the user wants to enable the plugin.
            if (Directory.Exists(gameInstallPath))
            {
                installOrUpdatePlugin(Path.Combine(Configuration.getDefaultFileLocation("plugins"), gameDefinition.gameInstallDirectory), gameInstallPath);
                if (gameDefinition.gameEnum == GameEnum.RF2_64BIT)
                {
                    UserSettings.GetUserSettings().setProperty("rf2_install_path", gameInstallPath);
                    
                    try
                    {
                        string configPath = Path.Combine(gameInstallPath, @"UserData\player\CustomPluginVariables.JSON");
                        if (File.Exists(configPath))
                        {
                            string json = File.ReadAllText(configPath);
                            Dictionary<string, Dictionary<string, int>> plugins = JsonConvert.DeserializeObject<Dictionary<string, Dictionary<string, int>>>(json);
                            Dictionary<string, int> plugin = null;
                            if (plugins.TryGetValue(rf2PluginFileName, out plugin))
                            {
                                //the whitespace is intended, this is how the game writes it.
                                if(plugin[" Enabled"] == 0)
                                {
                                    if(presentEnableMessagebox())
                                    {
                                        plugin[" Enabled"] = 1;
                                        json = JsonConvert.SerializeObject(plugins, Formatting.Indented);
                                        File.WriteAllText(configPath, json);
                                    }
                                }
                            }
                            else
                            {
                                if (presentEnableMessagebox())
                                {
                                    plugins.Add(rf2PluginFileName, new Dictionary<string, int>() { { " Enabled", 1 } });
                                    json = JsonConvert.SerializeObject(plugins, Formatting.Indented);
                                    File.WriteAllText(configPath, json);
                                }
                            }

                        }
                    }
                    catch (Exception e)
                    {
                        Console.WriteLine("Failed to enable plugin" + e.Message);
                    }
                }
                else if (gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT)
                {
                    UserSettings.GetUserSettings().setProperty("acs_install_path", gameInstallPath);
                    string pythonConfigPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), @"Assetto Corsa\cfg", @"python.ini");
                    if (File.Exists(pythonConfigPath))
                    {
                        string valueActive = ReadValue("CREWCHIEFEX", "ACTIVE", pythonConfigPath, "0");
                        if (!valueActive.Equals("1"))
                        {
                            if (presentEnableMessagebox())
                            {
                                WriteValue("CREWCHIEFEX", "ACTIVE", "1", pythonConfigPath);
                            }
                            
                        }
                    }
                }
                else if (gameDefinition.gameEnum == GameEnum.RF1)
                {
                    if (gameDefinition.gameInstallDirectory.Equals("Automobilista"))
                    {
                        UserSettings.GetUserSettings().setProperty("ams_install_path", gameInstallPath);
                    }
                    if (gameDefinition.gameInstallDirectory.Equals("rFactor"))
                    {
                        UserSettings.GetUserSettings().setProperty("rf1_install_path", gameInstallPath);
                    }
                }
                UserSettings.GetUserSettings().saveUserSettings();
                
            }
        }
    }
}
