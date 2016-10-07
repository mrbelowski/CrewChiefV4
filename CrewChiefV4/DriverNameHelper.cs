using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

/**
 * Utility class to ease some of the pain of managing driver names.
 */
namespace CrewChiefV4
{
    class DriverNameHelper
    {
        // if there's more than 2 names, and the second to last name isn't one of the common middle bits, 
        // use the last part
        private static Boolean optimisticSurnameExtraction = true;

        private static String[] middleBits = new String[] { "de la", "de le", "van der", "van de", "van", "de", "da", "le", "la", "von", "di", "eg", "du", "el", "del", "saint", "st" };

        private static Dictionary<String, String> lowerCaseRawNameToUsableName = new Dictionary<String, String>();

        private static Dictionary<String, String> usableNamesForSession = new Dictionary<String, String>();

        private static Boolean useLastNameWherePossible = true;

        private static Boolean rawNamesToUsableNamesFileRead = false;

        public static void readRawNamesToUsableNamesFiles(String soundsFolderName)
        {
            readRawNamesToUsableNamesFile(soundsFolderName, @"\driver_names\additional_names.txt");
            readRawNamesToUsableNamesFile(soundsFolderName, @"\driver_names\names.txt");
        }

        private static void readRawNamesToUsableNamesFile(String soundsFolderName, String filename)
        {
            Console.WriteLine("Reading driver name mappings");
            int counter = 0;
            string line;
            try
            {
                StreamReader file = new StreamReader(soundsFolderName + filename);
                while ((line = file.ReadLine()) != null)
                {
                    int separatorIndex = line.LastIndexOf(":");
                    if (separatorIndex > 0 && line.Length > separatorIndex + 1)
                    {
                        String lowerCaseRawName = line.Substring(0, separatorIndex).ToLower();
                        String usableName = line.Substring(separatorIndex + 1).Trim().ToLower();
                        if (usableName != null && usableName.Length > 0) 
                        {
                            if (lowerCaseRawNameToUsableName.ContainsKey(lowerCaseRawName))
                            {
                                // replace the existing mapping - last one wins
                                lowerCaseRawNameToUsableName[lowerCaseRawName] = usableName;
                            } 
                            else
                            {
                                // add a new mapping
                                lowerCaseRawNameToUsableName.Add(lowerCaseRawName, usableName);
                            }
                        }
                    }
                    counter++;
                }
                file.Close();
                Console.WriteLine("Read " + counter + " driver name mappings");
            }
            catch (IOException e)
            {

            }
            rawNamesToUsableNamesFileRead = true;
        }

        private static String validateAndCleanUpName(String name)
        {
            try
            {
                name = name.Replace('_', ' ');
                // be a bit careful with hypens - if it's before the first space, just remove it as
                // it's a separated firstname
                if (name.IndexOf(' ') > 0 && name.IndexOf('-') > 0 && name.IndexOf('-') < name.IndexOf(' '))
                {
                    name = name.Replace("-", "");
                }
                name = name.Replace('-', ' ');
                name = name.Replace('.', ' ');
                if (name.EndsWith("]") && name.Contains("["))
                {
                    name = name.Substring(0, name.LastIndexOf('['));
                }
                if (name.StartsWith("[") && name.Contains("]"))
                {
                    name = name.Substring(name.LastIndexOf(']') + 1);
                }
                if (name.EndsWith(")") && name.Contains("("))
                {
                    name = name.Substring(0, name.LastIndexOf('('));
                }
                if (name.StartsWith("(") && name.Contains(")"))
                {
                    name = name.Substring(name.LastIndexOf(')') + 1);
                }
                if (name.EndsWith(">") && name.Contains("<"))
                {
                    name = name.Substring(0, name.LastIndexOf('<'));
                }
                if (name.StartsWith("<") && name.Contains(">"))
                {
                    name = name.Substring(name.LastIndexOf('>') + 1);
                }
                if (name.EndsWith("}") && name.Contains("{"))
                {
                    name = name.Substring(0, name.LastIndexOf('{'));
                }
                if (name.StartsWith("{") && name.Contains("}"))
                {
                    name = name.Substring(name.LastIndexOf('}') + 1);
                }
                for (int i = 0; i < 4; i++)
                {
                    if (name.Count() > 1 && Char.IsNumber(name[name.Count() - 1]))
                    {
                        name = name.Substring(0, name.Count() - 1);
                    }
                    else
                    {
                        break;
                    }
                }
                Boolean allCharsValid = true;
                String charsFromName = "";
                for (int i = 0; i < name.Count(); i++)
                {
                    char ch = name[i];
                    if (Char.IsLetter(ch) || ch == ' ' || ch == '\'')
                    {
                        charsFromName = charsFromName + ch;
                    }
                    else
                    {
                        allCharsValid = false;
                    }
                }
                if (allCharsValid && name.Trim().Count() > 1)
                {
                    return name.Trim().ToLower();
                }
                else if (charsFromName.Trim().Count() > 1)
                {
                    return charsFromName.ToLower().Trim();
                }                
            }
            catch (Exception)
            {
                
            }
            return null;
        }

        public static String getUsableDriverName(String rawDriverName)
        {            
            if (!usableNamesForSession.ContainsKey(rawDriverName))
            {
                String usableDriverName = null;
                if (lowerCaseRawNameToUsableName.ContainsKey(rawDriverName.ToLower()))
                {
                    usableDriverName = lowerCaseRawNameToUsableName[rawDriverName.ToLower()];
                    Console.WriteLine("Using mapped drivername " + usableDriverName + " for raw driver name " + rawDriverName);
                    usableNamesForSession.Add(rawDriverName, usableDriverName);
                }
                else
                {
                    usableDriverName = validateAndCleanUpName(rawDriverName);
                    if (usableDriverName != null)
                    {
                        Boolean usedLastName = false;
                        if (useLastNameWherePossible)
                        {
                            String lastName = getUnambiguousLastName(usableDriverName);
                            if (lastName != null && lastName.Count() > 1)
                            {
                                if (lowerCaseRawNameToUsableName.ContainsKey(lastName.ToLower()))
                                {
                                    usableDriverName = lowerCaseRawNameToUsableName[lastName.ToLower()];
                                    Console.WriteLine("Using mapped driver last name " + usableDriverName + " for raw driver last name " + lastName);
                                    usableNamesForSession.Add(rawDriverName, usableDriverName);
                                    usedLastName = true;
                                }
                                else
                                {
                                    Console.WriteLine("Using unmapped driver last name " + lastName + " for raw driver name " + rawDriverName);
                                    usableDriverName = lastName;
                                    usableNamesForSession.Add(rawDriverName, usableDriverName);
                                    usedLastName = true;
                                }
                            }
                        }
                        if (!usedLastName)
                        {
                            Console.WriteLine("Using unmapped drivername " + usableDriverName + " for raw driver name " + rawDriverName);
                            usableNamesForSession.Add(rawDriverName, usableDriverName);
                        }
                    }
                    else
                    {
                        Console.WriteLine("Unable to create a usable driver name for " + rawDriverName);
                    }
                }
                return usableDriverName;
            }
            else
            {
                return usableNamesForSession[rawDriverName];
            }
        }
        
        public static List<String> getUsableDriverNames(List<String> rawDriverNames)
        {
            usableNamesForSession.Clear();
            foreach (String rawDriverName in rawDriverNames)
            {
                getUsableDriverName(rawDriverName);                
            }
            return usableNamesForSession.Values.ToList();
        }

        private static String getUnambiguousLastName(String fullName)
        {
            if (fullName.Count(Char.IsWhiteSpace) == 0) 
            {
                return fullName;
            } 
            else
            {
                foreach (String middleBit in middleBits) {
                    if (fullName.Contains(" " + middleBit + " ")) {
                        String[] split = fullName.Split(' ');
                        return middleBit + " " + split[split.Count() - 1];
                    }
                }
                String[] fullNameSplit = trimEmptyStrings(fullName.Split(' '));
                if (fullNameSplit.Count() == 2)
                {
                    String[] split = fullName.Split(' ');
                    if (split[1].Count() > 1)
                    {
                        return split[1];
                    }
                    else
                    {
                        return split[0];
                    }
                }
                else if (fullNameSplit[fullNameSplit.Count() - 2].Length == 1) 
                {
                    return fullNameSplit[fullNameSplit.Count() - 1];
                }
                else if (middleBits.Contains(fullNameSplit[fullNameSplit.Count() - 2].ToLower()))
                {
                    return fullNameSplit[fullNameSplit.Count() - 2] + " " + fullNameSplit[fullNameSplit.Count() - 1];
                }
                else if (fullNameSplit.Length > 3 && middleBits.Contains((fullNameSplit[fullNameSplit.Count() - 3] + " " + fullNameSplit[fullNameSplit.Count() - 2]).ToLower()))
                {
                    return fullNameSplit[fullNameSplit.Count() - 3] + " " + fullNameSplit[fullNameSplit.Count() - 2] + " " + fullNameSplit[fullNameSplit.Count() - 1];
                }
                else if (optimisticSurnameExtraction)
                {
                    return fullNameSplit[fullNameSplit.Count() - 1];
                }
            }
            return null;
        }

        private static String[] trimEmptyStrings(String[] strings)
        {
            List<String> trimmedList = new List<string>();
            foreach (String str in strings) {
                if (str.Trim().Length > 0)
                {
                    trimmedList.Add(str.Trim());
                }
            }
            return trimmedList.ToArray();
        }
    }
}
