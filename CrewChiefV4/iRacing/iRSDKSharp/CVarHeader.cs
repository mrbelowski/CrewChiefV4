using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace iRSDKSharp
{
    public class CVarHeader
    {
        public enum VarType { irChar, irBool, irInt, irBitField, irFloat, irDouble };
        VarType type;
        int offset;
        int count;
        string name;
        string desc;
        string unit;

        public CVarHeader(int type, int offset, int count, string name, string desc, string unit)
        {
            this.type = (VarType)type;
            this.offset = offset;
            this.count = count;
            this.name = name;
            this.desc = desc;
            this.unit = unit;
        }

        public VarType Type
        {
            get { return type; }
            //set { type = value; }
        }

        public int Offset
        {
            get { return offset; }
            //set { offset = value; }
        }

        public int Count
        {
            get { return count; }
            //set { count = value; }
        }

        public string Name
        {
            get { return name; }
            //set { name = value; }
        }

        public string Desc
        {
            get { return desc; }
            //set { desc = value; }
        }

        public string Unit
        {
            get { return unit; }
            //set { unit = value; }
        }

        public int Bytes
        {
            get
            {
                if (this.type == VarType.irChar || this.type == VarType.irBool)
                    return 1;
                else if (this.type == VarType.irInt || this.type == VarType.irBitField || this.type == VarType.irFloat)
                    return 4;
                else if (this.type == VarType.irDouble)
                    return 8;

                return 0;
            }
        }

        public int Length
        {
            get
            {
                return Bytes * Count;
            }
        }
    }
}
