using System;

namespace iRacingSDK
{
    [Flags]
    public enum CarLeftRight : uint
    {
        irsdk_LROff,
        irsdk_LRClear, // no cars around us.
        irsdk_LRCarLeft, // there is a car to our left.
        irsdk_LRCarRight, // there is a car to our right.
        irsdk_LRCarLeftRight, // there are cars on each side.
        irsdk_LR2CarsLeft, // there are two cars to our left.
        irsdk_LR2CarsRight // there are two cars to our right. 
    };
}
