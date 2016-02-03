using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace CrewChiefV4.UserInterface
{
    public class UIText
    {
        private static Dictionary<String, String> UIStrings = LoadStrings();
        
        public static String getString(String key) {
            if (UIStrings.ContainsKey(key)) {
                return UIStrings[key];
            }
            return key;
        }

        private static Dictionary<String, String> LoadStrings() {
            Dictionary<string, string> dict = new Dictionary<string, string>();
            String filePath;
            if (Debugger.IsAttached)
            {
                filePath = Application.StartupPath + @"\..\..\ui_text.txt";
            }
            else
            {
                filePath = Application.StartupPath + @"ui_text.txt";
            }
            StreamReader file = new StreamReader(filePath);
            String line;
            while ((line = file.ReadLine()) != null)
            {
                if (!line.StartsWith("#") && line.Contains("="))
                {
                    String[] split = line.Split('=');
                    dict.Add(split[0].Trim(), split[1].Trim());
                }
            }
            return dict;
        }
    }
}
