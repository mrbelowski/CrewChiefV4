﻿using System;
using System.Security.Cryptography;
using System.Text;
using System.IO;
using Microsoft.Win32;
using System.Text.RegularExpressions;
using System.Collections.Generic;
using System.Windows.Forms;


namespace CrewChiefV4
{
    class PluginInstaller
    {
        Boolean messageBoxPresented;
        Boolean messageBoxResult;
        public PluginInstaller()
        {
            messageBoxPresented = false;
            messageBoxResult = false;
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
        
        private Boolean presentMessagebox()
        {
            if (messageBoxPresented == false)
            {
                messageBoxPresented = true;
                if (DialogResult.OK == MessageBox.Show("Crew Chief needs additional files to be installed for the selected game. Click OK to install or Cancel to Return", "Plugin Needed",
                    MessageBoxButtons.OKCancel, MessageBoxIcon.Information))
                {
                    messageBoxResult = true;
                    return true;
                }
                else
                {
                    messageBoxResult = false;
                    return false;
                }
            }
            return messageBoxResult;
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
                                File.Copy(element, destinationFile, true);
                                Console.WriteLine("Updated plugin file: " + destinationFile);
                            }
                        }
                        else
                        {
                            File.Copy(element, destinationFile, true);
                            Console.WriteLine("Installed plugin file: " + destinationFile);
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
            //try to get the install folder from steam common install folders.
            if (!Directory.Exists(gameInstallPath))
            {
                //Present a messagebox to the user asking if they want to install plugins
                if (presentMessagebox())
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
                if (presentMessagebox())
                {
                    FolderBrowserDialog dialog = new FolderBrowserDialog();
                    dialog.ShowNewFolderButton = false;
                    dialog.Description = "Please select " + gameDefinition.gameInstallDirectory + " installation directory";
                    DialogResult result = dialog.ShowDialog();
                    if (result == DialogResult.OK && dialog.SelectedPath.Length > 0)
                    {
                        //check that the users actualy selected the correct folder by comparing with expected install folder.
                        //im unsure if we should use this check as it does prevent user from having installed in costum folder,
                        //like "rf2" instead of default folder name "rFactor 2"
                        if (Path.GetFileName(dialog.SelectedPath).Equals(gameDefinition.gameInstallDirectory))
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
            //we have a gameInstallPath so we can go on with installation/updating assuming
            if (Directory.Exists(gameInstallPath))
            {
                if (gameDefinition.gameEnum == GameEnum.RF2_64BIT)
                {
                    UserSettings.GetUserSettings().setProperty("rf2_install_path", gameInstallPath);
                }
                else if (gameDefinition.gameEnum == GameEnum.ASSETTO_32BIT || gameDefinition.gameEnum == GameEnum.ASSETTO_64BIT)
                {
                    UserSettings.GetUserSettings().setProperty("acs_install_path", gameInstallPath);
                }
                UserSettings.GetUserSettings().saveUserSettings();
                installOrUpdatePlugin(Path.Combine(Configuration.getDefaultFileLocation("plugins"), gameDefinition.gameInstallDirectory), gameInstallPath);
            }
        }
    }
}
