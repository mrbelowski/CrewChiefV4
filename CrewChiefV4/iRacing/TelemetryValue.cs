using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using iRSDKSharp;

namespace CrewChiefV4.iRacing
{
    public abstract class TelemetryValue
    {
        protected TelemetryValue(iRSDKSharp.iRacingSDK sdk, string name)
        {
            if (sdk == null) throw new ArgumentNullException("sdk");

            _exists = sdk.VarHeaders.ContainsKey(name);
            if (_exists)
            {
                var header = sdk.VarHeaders[name];
                _name = name;
                _description = header.Desc;
                _unit = header.Unit;
                _type = header.Type;
            }
        }

        private readonly bool _exists;
        /// <summary>
        /// Whether or not a telemetry value with this name exists on the current car.
        /// </summary>
        public bool Exists { get { return _exists; } }

        private readonly string _name;
        /// <summary>
        /// The name of this telemetry value parameter.
        /// </summary>
        public string Name { get { return _name; } }

        private readonly string _description;
        /// <summary>
        /// The description of this parameter.
        /// </summary>
        public string Description { get { return _description; } }

        private readonly string _unit;
        /// <summary>
        /// The real world unit for this parameter.
        /// </summary>
        public string Unit { get { return _unit; } }

        private readonly CVarHeader.VarType _type;
        /// <summary>
        /// The data-type for this parameter.
        /// </summary>
        public CVarHeader.VarType Type { get { return _type; } }

        public abstract object GetValue();
    }

    /// <summary>
    /// Represents a telemetry parameter of the specified type.
    /// </summary>
    /// <typeparam name="T">The .NET type of this parameter (int, char, float, double, bool, or arrays)</typeparam>
    public sealed class TelemetryValue<T> : TelemetryValue 
    {
        public TelemetryValue(iRSDKSharp.iRacingSDK sdk, string name)
            : base(sdk, name)
        {
            this.GetData(sdk);
        }

        private void GetData(iRacingSDK sdk)
        {
            try
            {
                var data = sdk.GetData(this.Name);

                var type = typeof(T);
                if (type.BaseType != null && type.BaseType.IsGenericType && type.BaseType.GetGenericTypeDefinition() == typeof(BitfieldBase<>))
                {
                    _Value = (T)Activator.CreateInstance(type, new[] { data });
                }
                else
                {
                    _Value = (T)data;
                }
            }
            catch (Exception)
            {
            }
        }

        private T _Value;
        /// <summary>
        /// The value of this parameter.
        /// </summary>
        public T Value { get { return _Value; } }

        public override object GetValue()
        {
            return this.Value;
        }

        public override string ToString()
        {
            return string.Format("{0} {1}", this.Value, this.Unit);
        }
    }
}
