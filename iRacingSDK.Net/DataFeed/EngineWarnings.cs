using System;

namespace iRacingSDK
{
    [Flags]
    public enum EngineWarnings : uint
    {    
        None = 0x00,
        WaterTempWarning = 0x01,
        FuelPressureWarning = 0x02,
        OilPressureWarning = 0x04,
        EngineStalled = 0x08,
        PitSpeedLimiter = 0x10,
        RevLimiterActive = 0x20,
    };

}
