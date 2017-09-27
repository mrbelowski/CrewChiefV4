using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.iRacing
{
    public abstract class BitfieldBase<T>
       where T : struct, IConvertible, IComparable
    {
        protected BitfieldBase() : this(0)
        { } 

        protected BitfieldBase(int value)
        {
            _value = (uint)value;
        }

        private uint _value;
        public uint Value { get { return _value; } }

        public void Add(T bit)
        {
            if (!Contains(bit))
                _value = _value | (uint)Convert.ChangeType(bit, bit.GetTypeCode());
        }

        public void Remove(T bit)
        {
            if (Contains(bit))
                _value = _value & ~(uint)Convert.ChangeType(bit, bit.GetTypeCode());
        }

        public bool Contains(T bit)
        {
            var bitValue = (uint) Convert.ChangeType(bit, bit.GetTypeCode());
            return (this.Value & bitValue) == bitValue;
        }

        public override string ToString()
        {
            var values = new List<T>();
            foreach (var value in Enum.GetValues(typeof (T)))
            {
                if (this.Contains((T) value))
                {
                    values.Add((T)value);
                }
            }
            return string.Join(" | ", values.Select(v => v.ToString()));
        }
    }
}
