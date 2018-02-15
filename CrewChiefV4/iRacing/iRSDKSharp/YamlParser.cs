using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace iRSDKSharp
{
    // Unsafe (eg - uses pointers and is much faster) implementation of the YAML parser
    // Written by Tomasz Terlecki
    public class YamlParser
    {
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

        public static unsafe string Parse(string data, string path)
        {
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
            
            fixed (char* pathptrFixed = path.ToCharArray())
            {
                int pathdepth = 0;
                GCHandle dataHandle = GCHandle.Alloc(data, GCHandleType.Pinned);
                fixed (char* dataptrFixed = data.ToCharArray())
                {

                    char* pathPtr = pathptrFixed;
                    char* dataPtr = dataptrFixed;

                    while (*dataPtr > 0)
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
                    }
                }
                dataHandle.Free();
            }

            if (!ok || val == null)
            {
                return null;
            }
            return new string(val, 0, len);
            
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
