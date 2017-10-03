using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.iRacing
{
    public class DriverPrivateInfo
    {
        public DriverPrivateInfo(Driver driver)
        {
            _driver = driver;
        }

        private readonly Driver _driver;

        public Driver Driver
        {
            get { return _driver; }
        }

        public double Speed { get; private set; }
        public float Throttle { get; private set; }
        public float Brake { get; private set; }
        public float Clutch { get; private set; }

        public float Fuel { get; private set; }
        public float FuelPercentage { get; private set; }
        public float FuelPressure { get; private set; }

        public void ParseTelemetry(iRacingData e)
        {
            this.Speed = e.Speed;
            this.Throttle = e.Throttle;
            this.Brake = e.Brake;
            this.Clutch = e.Clutch;
            this.Fuel = e.FuelLevel;
            this.FuelPercentage = e.FuelLevelPct;
            this.FuelPressure = e.FuelPress;

            // TODO: add remaining parameters
        }
    }
}
