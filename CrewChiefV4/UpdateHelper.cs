using CrewChiefV4.Audio;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    class UpdateHelper
    {
        public static void MoveDirectory(string source, string target)
        {
            var sourcePath = source.TrimEnd('\\', ' ');
            var targetPath = target.TrimEnd('\\', ' ');
            var files = Directory.EnumerateFiles(sourcePath, "*", SearchOption.AllDirectories)
                                 .GroupBy(s => Path.GetDirectoryName(s));
            foreach (var folder in files)
            {
                var targetFolder = folder.Key.Replace(sourcePath, targetPath);
                Directory.CreateDirectory(targetFolder);
                foreach (var file in folder)
                {
                    var targetFile = Path.Combine(targetFolder, Path.GetFileName(file));
                    if (File.Exists(targetFile)) File.Delete(targetFile);
                    File.Move(file, targetFile);
                }
            }
            Directory.Delete(source, true);
        }

        public static void ProcessFileUpdates(String source)
        {
            try {
                StreamReader file = new StreamReader(source + @"\updates.txt");
                int deletedCount = 0;
                int renamedCount = 0;
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    lock (MainWindow.instanceLock)
                    {
                        if (MainWindow.instance == null)
                        {
                            return;
                        }
                    }
                    if (line.Trim().Length > 0 && !line.StartsWith("#"))
                    {
                        try
                        {
                            String[] directives = line.Split('|');
                            if (directives[0] == "rename")
                            {
                                File.Move(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + directives[1], AudioPlayer.soundFilesPathNoChiefOverride + @"\" + directives[2]);
                                renamedCount++;
                            }
                            else if (directives[0] == "delete")
                            {
                                File.Delete(AudioPlayer.soundFilesPathNoChiefOverride + @"\" + directives[1]);
                                deletedCount++;
                            }
                        }
                        catch (Exception)
                        {}
                    }
                }
                Console.WriteLine("Successfully deleted " + deletedCount + " and renamed " + renamedCount + " sound files");
                file.Close();
                File.Delete(source + @"\updates.txt");
            }
            catch (Exception)
            {}
        }
    }
}
