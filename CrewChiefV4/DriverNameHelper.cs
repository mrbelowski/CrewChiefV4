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
        public static HashSet<String> unvocalizedNames = new HashSet<string>();

        // if there's more than 2 names, and the second to last name isn't one of the common middle bits, 
        // use the last part
        private static Boolean optimisticSurnameExtraction = true;

        private static String[] middleBits = new String[] { "de la", "de le", "van der", "van de", "van", "de", "da", "le", "la", "von", "di", "eg", "du", "el", "del", "saint", "st" };

        private static Dictionary<String, String> lowerCaseRawNameToUsableName = new Dictionary<String, String>();

        private static Dictionary<String, String> usableNamesForSession = new Dictionary<String, String>();

        private static Boolean useLastNameWherePossible = true;

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
                            // add new or replace the existing mapping - last one wins
                            lowerCaseRawNameToUsableName[lowerCaseRawName] = usableName;
                        }
                    }
                    counter++;
                }
                file.Close();
                Console.WriteLine("Read " + counter + " driver name mappings");
            }
            catch (IOException)
            {}
        }
        
        private static String validateAndCleanUpName(String name)
        {
            try
            {
                name = replaceObviousChars(name);
                name = cleanBrackets(name);
                if (name.Count() < 2)
                {
                    return null;
                }
                name = undoNumberSubstitutions(name);
                name = trimNumbersOffEnd(name);
                if (name.Count() < 2)
                {
                    return null;
                }
                name = trimNumbersOffStart(name);
                if (name.Count() < 2)
                {
                    return null;
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
        private static String replaceObviousChars(String name)
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
            name = name.Replace("$", "s");
            return name.Trim();
        }

        private static String cleanBrackets(String name)
        {
            if (name.EndsWith("]") && name.Contains("["))
            {
                name = name.Substring(0, name.IndexOf('['));
                name = name.Trim();
            }
            if (name.StartsWith("[") && name.Contains("]"))
            {
                name = name.Substring(name.LastIndexOf(']') + 1);
                name = name.Trim();
            }
            if (name.EndsWith(")") && name.Contains("("))
            {
                name = name.Substring(0, name.LastIndexOf('('));
                name = name.Trim();
            }
            if (name.StartsWith("(") && name.Contains(")"))
            {
                name = name.Substring(name.LastIndexOf(')') + 1);
                name = name.Trim();
            }
            if (name.EndsWith(">") && name.Contains("<"))
            {
                name = name.Substring(0, name.LastIndexOf('<'));
                name = name.Trim();
            }
            if (name.StartsWith("<") && name.Contains(">"))
            {
                name = name.Substring(name.LastIndexOf('>') + 1);
                name = name.Trim();
            }
            if (name.EndsWith("}") && name.Contains("{"))
            {
                name = name.Substring(0, name.LastIndexOf('{'));
                name = name.Trim();
            }
            if (name.StartsWith("{") && name.Contains("}"))
            {
                name = name.Substring(name.LastIndexOf('}') + 1);
                name = name.Trim();
            }
            return name;
        }

        private static String undoNumberSubstitutions(String name)
        {
            // handle letter -> number substitutions
            String nameWithLetterSubstitutions = "";
            for (int i = 0; i < name.Count(); i++)
            {
                char ch = name[i];
                Boolean changedNumberForLetter = false;
                // see if this is a letter -> number subtitution - can only handle one of these
                if (i > 0 && i < name.Count() - 1)
                {
                    if (Char.IsNumber(ch) && Char.IsLetter(name[i - 1]) && Char.IsLetter(name[i + 1]))
                    {
                        if (ch == '1')
                        {
                            changedNumberForLetter = true;
                            nameWithLetterSubstitutions = nameWithLetterSubstitutions + 'l';
                        }
                        else if (ch == '3')
                        {
                            changedNumberForLetter = true;
                            nameWithLetterSubstitutions = nameWithLetterSubstitutions + 'e';
                        }
                        else if (ch == '0')
                        {
                            changedNumberForLetter = true;
                            nameWithLetterSubstitutions = nameWithLetterSubstitutions + 'o';
                        }
                    }
                }
                if (!changedNumberForLetter)
                {
                    nameWithLetterSubstitutions = nameWithLetterSubstitutions + ch;
                }
            }
            return nameWithLetterSubstitutions;
        }

        private static String trimNumbersOffEnd(String name)
        {
            // trim numbers off the end
            while (name.Count() > 2 && char.IsNumber(name[name.Count() - 1]))
            {
                name = name.Substring(0, name.Count() - 1);
            }
            return name;
        }

        private static String trimNumbersOffStart(String name)
        {
            int index = 0;
            while (name.Count() > 2 && index < name.Count() - 1 && char.IsNumber(name[index]))
            {
                name = name.Substring(index + 1);
            }
            return name;
        }



        public static String getUsableDriverName(String rawDriverName)
        {
            if (!usableNamesForSession.ContainsKey(rawDriverName))
            {
                String usableDriverName = null;
                if (lowerCaseRawNameToUsableName.TryGetValue(rawDriverName.ToLower(), out usableDriverName))
                {
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
                                if (lowerCaseRawNameToUsableName.TryGetValue(lastName.ToLower(), out usableDriverName))
                                {
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

        public static void dumpUnvocalizedNames()
        {
            HashSet<String> existingNamesInFile = getNamesAlreadyInFile(getUnvocalizedDriverNamesFileLocation());
            existingNamesInFile.UnionWith(unvocalizedNames);
            List<String> namesToAdd = new List<String>(existingNamesInFile);
            namesToAdd.Sort();
            TextWriter tw = new StreamWriter(getUnvocalizedDriverNamesFileLocation(), false);
            foreach (String name in namesToAdd)
            {
                tw.WriteLine(name);
            }
            tw.Close();
        }

        private static HashSet<String> getNamesAlreadyInFile(String fullFilePath)
        {
            HashSet<String> names = new HashSet<string>();
            StreamReader file = null;
            try
            {
                file = new StreamReader(fullFilePath);
                String line;
                while ((line = file.ReadLine()) != null)
                {
                    names.Add(line.Trim());
                }
            }
            catch (Exception)
            {
                // ignore - file doesn't exist so it'll be created
            }
            finally
            {
                if (file != null)
                {
                    file.Close();
                }
            }
            return names;
        }

        private static String getUnvocalizedDriverNamesFileLocation()
        {
            return System.IO.Path.Combine(Environment.GetFolderPath(
                Environment.SpecialFolder.MyDocuments), "CrewChiefV4", "unvocalized_driver_names.txt");
        }
    }
}
