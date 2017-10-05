using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CrewChiefV4.iRacing
{
    [Flags]
    public enum TrackSurfaces
    {
        NotInWorld = -1,
        OffTrack,
        InPitStall,
        AproachingPits,
        OnTrack
    }
    [Flags]
    public enum TrackSurfaceMaterial
    {
        SurfaceNotInWorld = -1,
        UndefinedMaterial = 0,

        Asphalt1Material,
        Asphalt2Material,
        Asphalt3Material,
        Asphalt4Material,
        Concrete1Material,
        Concrete2Material,
        RacingDirt1Material,
        RacingDirt2Material,
        Paint1Material,
        Paint2Material,
        Rumble1Material,
        Rumble2Material,
        Rumble3Material,
        Rumble4Material,

        Grass1Material,
        Grass2Material,
        Grass3Material,
        Grass4Material,
        Dirt1Material,
        Dirt2Material,
        Dirt3Material,
        Dirt4Material,
        SandMaterial,
        Gravel1Material,
        Gravel2Material,
        GrasscreteMaterial,
        AstroturfMaterial,
    };

    [Flags]
    public enum SessionStates
    {
        Invalid,
        GetInCar,
        Warmup,
        ParadeLaps,
        Racing,
        Checkered,
        CoolDown
    }
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
    [Flags]
    public enum DisplayUnits
    {
        EnglishImperial = 0,
        Metric = 1
    }
    [Flags]
    public enum WeatherType
    {
        Constant = 0,
        Dynamic = 1
    }
    [Flags]
    public enum Skies
    {
        Clear = 0,
        PartlyCloudy = 1,
        MostlyCloudy = 2,
        Overcast = 3
    }

}
