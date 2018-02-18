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
    public enum ReasonOutId
    {
	    IDS_REASON_OUT_NOT_OUT,
	    IDS_REASON_OUT_DID_NOT_START,
	    IDS_REASON_OUT_BRAKE_FAILURE,
	    IDS_REASON_OUT_COOLANT_LEAK,
	    IDS_REASON_OUT_RADIATOR_PROBLEM,
	    IDS_REASON_OUT_ENGINE_FAILURE,
	    IDS_REASON_OUT_ENGINE_HEADER,
	    IDS_REASON_OUT_ENGINE_VALVE,
	    IDS_REASON_OUT_ENGINE_PISTON,
	    IDS_REASON_OUT_ENGINE_GEARBOX,
	    IDS_REASON_OUT_ENGINE_CLUTCH,
	    IDS_REASON_OUT_ENGINE_CAMSHAFT,
	    IDS_REASON_OUT_ENGINE_IGNITION,
	    IDS_REASON_OUT_ENGINE_FIRE,
	    IDS_REASON_OUT_ENGINE_ELECTRICAL,
	    IDS_REASON_OUT_FUEL_LEAK,
	    IDS_REASON_OUT_FUEL_INJECTOR,
	    IDS_REASON_OUT_FUEL_PUMP,
	    IDS_REASON_OUT_FUEL_LINE,
	    IDS_REASON_OUT_OIL_LEAK,
	    IDS_REASON_OUT_OIL_LINE,
	    IDS_REASON_OUT_OIL_PUMP,
	    IDS_REAONS_OUT_OIL_PRESSURE,
	    IDS_REASON_OUT_SUSPENSION_FAILURE,
	    IDS_REASON_OUT_TIRE_PUNCTURE,
	    IDS_REASON_OUT_TIRE_PROBLEM,
	    IDS_REASON_OUT_WHEEL_PROBLEM,
	    IDS_REASON_OUT_ACCIDENT,
	    IDS_RETIRED,
	    IDS_DISQUALIFIED,
	    IDS_REASON_OUT_NO_FUEL,
	    IDS_REASON_OUT_BRAKE_LINE,
	    IDS_REASON_OUT_LOST_CONNECTION,
	    IDS_REASON_OUT_EJECTED,
    }

}
