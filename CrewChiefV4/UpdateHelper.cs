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

        public static void RemoveDeletedFiles(String source)
        {
            try {
                StreamReader file = new StreamReader(source + @"\deletions.txt");
                int count = 0;
                string line;
                while ((line = file.ReadLine()) != null)
                {
                    try { 
                        File.Delete(AudioPlayer.soundFilesPath + @"\" + line);
                        count++;
                    }
                    catch (Exception e)
                    {

                    }
                }
                Console.WriteLine("Successfully deleted " + count + " outdated sound files");
                file.Close();
                File.Delete(source + @"\deletions.txt");
            } catch (Exception e) {

            }
        }
    }
}
