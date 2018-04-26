using CrewChiefV4;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace iRSDKSharp
{
    // Unsafe (eg - uses pointers and is much faster) implementation of the YAML parser
    // Written by Tomasz Terlecki
    public class YamlParser
    {
        // only allow the yaml + path to be dumped once per run
        private static Boolean dumpedYamlInThisSession = false;

        public static Boolean useUnsafeParser = UserSettings.GetUserSettings().getBoolean("iracing_fast_parsing");

        enum StateType { Space, Key, KeySep, Value, NewLine };

        static unsafe int strncmp(char* s1, char* s2, int keylen)
        {
            while (*s1 == *s2 && keylen-- > 0)
            {
                if (*s1 == '\0' || keylen == 0)
                    return (0);
                s1++;
                s2++;
            }
            return (*s1 - *s2);
        }

        public static string Parse(string data, string path)
        {
            if (useUnsafeParser)
            {
                return UnsafeParse(data, path);
            }
            else
            {
                return SafeParse(data, path);
            }
        }

        private static string SafeParse(string data, string path)
        {
            try
            {
                int depth = 0;
                StateType state = StateType.Space;

                string keystr = null;
                int keylen = 0;

                string valuestr = null;
                int valuelen = 0;

                int pathdepth = 0;

                char[] stream = data.ToCharArray();
                int idx = 0;

                while (idx < stream.Length /* data != ""*/ )//for (int i = 0; i < data.Length; i++)
                {
                    //switch (data[0])
                    switch (stream[idx])
                    {
                        case ' ':
                        case '-':
                            if (state == StateType.NewLine)
                                state = StateType.Space;
                            if (state == StateType.Space)
                                depth++;
                            else if (state == StateType.Key)
                                keylen++;
                            else if (state == StateType.Value)
                                valuelen++;
                            break;
                        case ':':
                            if (state == StateType.Key)
                            {
                                state = StateType.KeySep;
                                keylen++;
                            }
                            else if (state == StateType.KeySep)
                            {
                                state = StateType.Value;
                                //valuestr = data;
                                valuestr = new string(stream, idx, stream.Length - idx);
                            }
                            break;
                        case '\n':
                        case '\r':
                            if (state != StateType.NewLine)
                            {
                                if (depth < pathdepth)
                                {
                                    return null;
                                }
                                else if (keylen > 0)
                                {
                                    string key = keystr.Substring(0, keystr.Length > keylen ? keylen : keystr.Length);
                                    string pa = path.Substring(0, path.Length > keylen ? keylen : path.Length);
                                    if (key.Equals(pa))
                                    {
                                        bool found = true;
                                        if (path.Length > keylen && path[keylen] == '{')
                                        {
                                            if (valuestr == null)
                                            {
                                                return null;
                                            }
                                            string val = valuestr.Substring(0, valuelen);
                                            string p2 = path.Substring(keylen + 1, path.IndexOf('}') - (keylen + 1));
                                            if (val.Equals(p2))
                                                path = path.Substring(valuelen + 2);
                                            else
                                                found = false;
                                        }

                                        if (found)
                                        {
                                            pathdepth = depth;
                                            if (path != "")
                                                path = path.Substring(keylen);

                                            if (path == "")
                                            {
                                                if (valuestr == null)
                                                {
                                                    return null;
                                                }
                                                string val = valuestr.Substring(0, valuelen);
                                                return val;
                                            }
                                        }
                                    }
                                }
                                depth = 0;
                                keylen = 0;
                                valuelen = 0;
                            }
                            state = StateType.NewLine;
                            break;
                        default:
                            if (state == StateType.Space || state == StateType.NewLine)
                            {
                                state = StateType.Key;
                                //keystr = data;
                                keystr = new string(stream, idx, stream.Length - idx);
                                keylen = 0;
                            }
                            else if (state == StateType.KeySep)
                            {
                                state = StateType.Value;
                                //valuestr = data;
                                valuestr = new string(stream, idx, stream.Length - idx);
                                valuelen = 0;
                            }
                            if (state == StateType.Key)
                                keylen++;
                            if (state == StateType.Value)
                                valuelen++;
                            break;
                    }
                    //data = data.Substring(1);
                    idx++;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Error parsing session info: " + e.StackTrace);
                Console.WriteLine("Path = " + path);
                Console.WriteLine("Data = " + data);
                Console.WriteLine("Critical error in YAML parser");
                Console.WriteLine("PLEASE FORWARD ALL OF THIS CONSOLE LOG TO THE CC DEV TEAM");
                MainWindow.instance.killChief();
            }
            return null;
        }

        private static unsafe string UnsafeParse(string data, string path)
        {
            GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
            GCHandle pathHandle = GCHandle.Alloc(path, GCHandleType.Pinned);
            char* val = null;
            int len = 0;

            int depth = 0;
            StateType state = StateType.Space;

            char* keystr = null;
            int keylen = 0;

            char* valuestr = null;
            int valuelen = 0;

            bool ok = false;
            bool end = false;

            int dataStringLength = data.Length;
            int pointerPosition = 0;

            String extractedString = null;
            fixed (char* pathptrFixed = path.ToCharArray())
            {
                int pathdepth = 0;
                fixed (char* dataptrFixed = data.ToCharArray())
                {
                    char* pathPtr = pathptrFixed;
                    char* dataPtr = dataptrFixed;

                    while (pointerPosition < dataStringLength && *dataPtr > 0)
                    {
                        switch (*dataPtr)
                        {
                            case ' ':
                            case '-':
                                if (state == StateType.NewLine)
                                    state = StateType.Space;
                                if (state == StateType.Space)
                                    depth++;
                                else if (state == StateType.Key)
                                    keylen++;
                                else if (state == StateType.Value)
                                    valuelen++;
                                break;
                            case ':':
                                if (state == StateType.Key)
                                {
                                    state = StateType.KeySep;
                                    keylen++;
                                }
                                else if (state == StateType.KeySep)
                                {
                                    state = StateType.Value;
                                    valuestr = dataPtr;
                                }
                                else if (state == StateType.Value)
                                    valuelen++;
                                break;
                            case '\n':
                            case '\r':
                                if (state != StateType.NewLine)
                                {
                                    if (depth < pathdepth)
                                    {
                                        ok = false;
                                        end = true;
                                        break;
                                    }
                                    else if (keylen > 0 && 0 == strncmp(keystr, pathPtr, keylen))
                                    {
                                        bool found = true;
                                        //do we need to test the value?
                                        if (*(pathPtr + keylen) == '{')
                                        {
                                            //search for closing brace
                                            int pathvaluelen = keylen + 1;
                                            while (*(pathPtr + pathvaluelen) > 0 && *(pathPtr + pathvaluelen) != '}')
                                                pathvaluelen++;

                                            if (valuelen == pathvaluelen - (keylen + 1) && 0 == strncmp(valuestr, (pathPtr + keylen + 1), valuelen))
                                                pathPtr += valuelen + 2;
                                            else
                                                found = false;
                                        }

                                        if (found)
                                        {
                                            pathPtr += keylen;
                                            pathdepth = depth;

                                            if (*pathPtr == '\0')
                                            {
                                                val = valuestr;
                                                len = valuelen;
                                                ok = true;
                                                end = true;
                                                break;
                                            }
                                        }
                                    }

                                    depth = 0;
                                    keylen = 0;
                                    valuelen = 0;
                                }
                                state = StateType.NewLine;
                                break;
                            default:
                                if (state == StateType.Space || state == StateType.NewLine)
                                {
                                    state = StateType.Key;
                                    keystr = dataPtr;
                                    keylen = 0; //redundant?
                                }
                                else if (state == StateType.KeySep)
                                {
                                    state = StateType.Value;
                                    valuestr = dataPtr;
                                    valuelen = 0; //redundant?
                                }
                                if (state == StateType.Key)
                                    keylen++;
                                if (state == StateType.Value)
                                    valuelen++;
                                break;
                        }

                        if (end)
                        {
                            break;
                        }

                        // important, increment our pointer
                        dataPtr++;
                        pointerPosition++;
                    }

                    if (ok && val != null && len > 0)
                    {
                        if (pointerPosition + len > dataStringLength)
                        {
                            Console.WriteLine("Pointer position error in Yaml parser");
                            if (!dumpedYamlInThisSession)
                            {
                                dumpedYamlInThisSession = true;
                                Console.WriteLine("Path = " + path);
                                Console.WriteLine("Data = " + data);
                                Console.WriteLine("Critical error in YAML parser");
                                Console.WriteLine("PLEASE FORWARD ALL OF THIS CONSOLE LOG TO THE CC DEV TEAM");
                            }
                        }
                        else
                        {
                            extractedString = new string(val, 0, len);
                        }
                    }
                }  // fixed (char* dataptrFixed = data.ToCharArray())
            }  // fixed (char* pathptrFixed = path.ToCharArray())

            dataHandle.Free();
            pathHandle.Free();
            return extractedString;
        }

        public static bool TryGetValue(string yaml, string query, out string value)
        {
            try
            {
                value = Parse(yaml, query);
                return value != null;
            }
            catch (Exception)
            {
                value = null;
                return false;
            }
        }
    }
}
