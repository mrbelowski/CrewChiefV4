using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iRSDKSharp
{
    public static class YamlParser
    {
        enum StateType { Space, Key, KeySep, Value, NewLine };

        public static string Parse(string data, string path)
        {
            int depth = 0;
            StateType state = StateType.Space;
            
            string keystr = null;
            int keylen = 0;

            string valuestr = null;
            int valuelen = 0;

            int pathdepth = 0;

            while(data != "")//for (int i = 0; i < data.Length; i++)
            {
                switch (data[0])
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
                            valuestr = data;
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
                                        if(path != "")
                                            path = path.Substring(keylen);

                                        if (path == "")
                                        {
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
                            keystr = data;
                            keylen = 0;
                        }
                        else if (state == StateType.KeySep)
                        {
                            state = StateType.Value;
                            valuestr = data;
                            valuelen = 0;
                        }
                        if (state == StateType.Key)
                            keylen++;
                        if (state == StateType.Value)
                            valuelen++;
                        break;
                }
                data = data.Substring(1);
            }

            return null;
        }
    }
}
