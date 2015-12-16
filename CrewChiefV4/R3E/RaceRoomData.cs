using System;
using System.Runtime.InteropServices;

namespace CrewChiefV4.RaceRoom
{
    class RaceRoomConstant
    {
        public const string SharedMemoryName = "$Race$";

        public enum Session
        {
            Unavailable = -1,
            Practice = 0,
            Qualify = 1,
            Race = 2
        }

        public enum SessionPhase
        {
            Unavailable = -1,
            Garage = 0,
            Gridwalk = 2,
            Formation = 3,
            Countdown = 4,
            Green = 5,
            Checkered = 6,
            Terminated = 7 // seems to be 7 when the session is 'killed'
        }

        public enum Control
        {
            Unavailable = -1,
            Player = 0,
            AI = 1,
            Remote = 2,
            Replay = 3
        }

        public enum PitWindow
        {
            Unavailable = -1,
            Disabled = 0,
            Closed = 1,
            Open = 2,
            StopInProgress = 3,
            Completed = 4
        }

        public enum TireType
        {
            Unavailable = -1,
            DTM_Option = 0,
            Prime = 1
        }

        public enum PitStopStatus
        {
            // No mandatory pitstops
            R3E_PITSTOP_STATUS_UNAVAILABLE = -1,

            // Mandatory pitstop not served yet
            R3E_PITSTOP_STATUS_UNSERVED = 0,

            // Mandatory pitstop served
            R3E_PITSTOP_STATUS_SERVED = 1
        }

        public enum FinishStatus
        {
            // N/A
            R3E_FINISH_STATUS_UNAVAILABLE = -1,

            // Still on track, not finished
            R3E_FINISH_STATUS_NONE = 0,

            // Finished session normally
            R3E_FINISH_STATUS_FINISHED = 1,

            // Did not finish
            R3E_FINISH_STATUS_DNF = 2,

            // Did not qualify
            R3E_FINISH_STATUS_DNQ = 3,

            // Did not start
            R3E_FINISH_STATUS_DNS = 4,

            // Disqualified
            R3E_FINISH_STATUS_DQ = 5
        }
    }

    namespace RaceRoomData
    {
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Vector3<T>
        {
            public T X;
            public T Y;
            public T Z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Orientation<T>
        {
            public T Pitch;
            public T Yaw;
            public T Roll;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct UserInput
        {
            public Single _1, _2, _3, _4, _5, _6;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TireTemperature
        {
            public Single FrontLeft_Left;
            public Single FrontLeft_Center;
            public Single FrontLeft_Right;

            public Single FrontRight_Left;
            public Single FrontRight_Center;
            public Single FrontRight_Right;

            public Single RearLeft_Left;
            public Single RearLeft_Center;
            public Single RearLeft_Right;

            public Single RearRight_Left;
            public Single RearRight_Center;
            public Single RearRight_Right;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PlayerData
        {
            // Virtual physics time
            // Unit: Ticks (1 tick = 1/400th of a second)
            public Int32 GameSimulationTicks;

            // Padding to accomodate for legacy alignment
            [ObsoleteAttribute("Used for padding", true)]
            public Int32 Padding1;

            // Virtual physics time
            // Unit: Seconds
            public Double GameSimulationTime;

            // Car world-space position
            public Vector3<Double> Position;

            // Car world-space velocity
            // Unit: Meter per second (m/s)
            public Vector3<Double> Velocity;

            // Car world-space acceleration
            // Unit: Meter per second squared (m/s^2)
            public Vector3<Double> Acceleration;

            // Car local-space acceleration
            // Unit: Meter per second squared (m/s^2)
            public Vector3<Double> LocalAcceleration;

            // Car body orientation
            // Unit: Euler angles
            public Vector3<Double> Orientation;

            // Car body rotation
            public Vector3<Double> Rotation;

            // Car body angular acceleration (torque divided by inertia)
            public Vector3<Double> AngularAcceleration;

            // Acceleration of driver's body in local coordinates
            public Vector3<Double> DriverBodyAcceleration;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Flags
        {
            // Whether yellow flag is currently active
            // -1 = no data
            //  0 = not active
            //  1 = active
            public Int32 Yellow;

            // Whether blue flag is currently active
            // -1 = no data
            //  0 = not active
            //  1 = active
            public Int32 Blue;

            // Whether black flag is currently active
            // -1 = no data
            //  0 = not active
            //  1 = active
            public Int32 Black;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CarDamage
        {
            // ...
            public Single Engine;

            // ...
            public Single Transmission;

            // ...
            public Single Aerodynamics;

            // ...
            public Single TireFrontLeft;

            // ...
            public Single TireFrontRight;

            // ...
            public Single TireRearLeft;

            // ...
            public Single TireRearRight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TirePressure
        {
            // ...
            public Single FrontLeft;

            // ...
            public Single FrontRight;

            // ...
            public Single RearLeft;

            // ...
            public Single RearRight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct BrakeTemperatures
        {
            // ...
            public Single FrontLeft;

            // ...
            public Single FrontRight;

            // ...
            public Single RearLeft;

            // ...
            public Single RearRight;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct CutTrackPenalties
        {
            // ...
            public Int32 DriveThrough;

            // ...
            public Int32 StopAndGo;

            // ...
            public Int32 PitStop;

            // ...
            public Int32 TimeDeduction;

            // ...
            public Int32 SlowDown;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Sectors<T>
        {
            public T Sector1;
            public T Sector2;
            public T Sector3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TyreDirt
        {
            public Single front_left;
            public Single front_right;
            public Single rear_left;
            public Single rear_right;
        } 

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct WheelSpeed
        {
            public Single front_left;
            public Single front_right;
            public Single rear_left;
            public Single rear_right;
        } 

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TrackInfo
        {
	        public Int32 track_id;
	        public Int32 layout_id;
	        public Single length;
        } 

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PushToPass
        {
	        public Int32 available;
	        public Int32 engaged;
	        public Int32 amount_left;
	        public Single engaged_time_left;
	        public Single wait_time_left;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi, Pack = 1)]
        public struct DriverInfo
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] nameByteArray;
	        public Int32 car_number;
	        public Int32 class_id;
	        public Int32 model_id;
	        public Int32 team_id;
	        public Int32 livery_id;
	        public Int32 manufacturer_id;
	        public Int32 slot_id;
	        public Int32 class_performance_index;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DriverData
        {
	        public DriverInfo driver_info;
            public Int32 finish_status;
	        public Int32 place;
	        public Single lap_distance;
	        public Vector3<Single> position;
	        public Int32 track_sector;
            public Int32 completed_laps;
	        public Int32 current_lap_valid;
            public Single lap_time_current_self;
            public Sectors<Single> sector_time_current_self;
            public Sectors<Single> sector_time_previous_self;
            public Sectors<Single> sector_time_best_self;
            public Single time_delta_front;
            public Single time_delta_behind;
            public Int32 pitstop_status;
	        public Int32 in_pitlane;
	        public Int32 num_pitstops;
            public CutTrackPenalties penalties;
            public Single car_speed;
            public Int32 tire_type;
        }     

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RaceRoomShared
        {
            [ObsoleteAttribute("Not set anymore", false)]
            public UserInput UserInput;

            // Engine speed
            // Unit: Radians per second (rad/s)
            public Single EngineRps;

            // Maximum engine speed
            // Unit: Radians per second (rad/s)
            public Single MaxEngineRps;

            // Unit: Kilopascals (KPa)
            public Single FuelPressure;

            // Current amount of fuel in the tank(s)
            // Unit: Liters (l)
            public Single FuelLeft;

            // Maximum capacity of fuel tank(s)
            // Unit: Liters (l)
            public Single FuelCapacity;

            // Unit: Kelvin (K)
            public Single EngineWaterTemp;

            // Unit: Kelvin (K)
            public Single EngineOilTemp;

            // Unit: Kilopascals (KPa)
            public Single EngineOilPressure;

            // Unit: Meter per second (m/s)
            public Single CarSpeed;

            // Total number of laps in the race, or -1 if player is not in race mode (practice, test mode, etc.)
            public Int32 NumberOfLaps;

            // How many laps the player has completed. If this value is 6, the player is on his 7th lap. -1 = n/a
            public Int32 CompletedLaps;

            // Unit: Seconds (-1.0 = none)
            public Single LapTimeBest;

            // Unit: Seconds (-1.0 = none)
            public Single LapTimePrevious;

            // Unit: Seconds (-1.0 = none)
            public Single LapTimeCurrent;

            // Current position (1 = first place)
            public Int32 Position;

            // Number of cars (including the player) in the race
            public Int32 NumCars;

            // -2 = no data
            // -1 = reverse,
            //  0 = neutral
            //  1 = first gear
            // (... up to 7th)
            public Int32 Gear;

            // Temperature of three points across the tread of each tire
            // Unit: Kelvin (K)
            public TireTemperature TireTemp;

            // Number of penalties pending for the player
            public Int32 NumPenalties;

            // Physical location of car's center of gravity in world space (X, Y, Z) (Y = up)
            public Vector3<Single> CarCgLoc;

            // Pitch, yaw, roll
            // Unit: Radians (rad)
            public Orientation<Single> CarOrientation;

            // Acceleration in three axes (X, Y, Z) of car body in local-space.
            // From car center, +X=left, +Y=up, +Z=back.
            // Unit: Meter per second squared (m/s^2)
            public Vector3<Single> LocalAcceleration;

            // -1 = no data for DRS
            //  0 = not available
            //  1 = available
            public Int32 DrsAvailable;

            // -1 = no data for DRS
            //  0 = not engaged
            //  1 = engaged
            public Int32 DrsEngaged;

            // Padding to accomodate for legacy alignment
            [ObsoleteAttribute("Used for padding", true)]
            public Int32 Padding1;

            // High precision data for player's vehicle only
            public PlayerData Player;

            // ...
            public Int32 EventIndex;

            // ...
            public Int32 SessionType;

            // ...
            public Int32 SessionPhase;

            // ...
            public Int32 SessionIteration;

            // ...
            public Int32 ControlType;

            // ...
            public Single ThrottlePedal;

            // ...
            public Single BrakePedal;

            // ...
            public Single ClutchPedal;

            // ...
            public Single BrakeBias;

            // ...
            public TirePressure TirePressure;

            // ...
            public Int32 TireWearActive;

            // ...
            public Int32 TireType;

            // ...
            public BrakeTemperatures BrakeTemperatures;

            // -1 = no data
            //  0 = not active
            //  1 = active
            public Int32 FuelUseActive;

            // ...
            public Single SessionTimeRemaining;

            // ...
            public Single LapTimeBestLeader;

            // ...
            public Single LapTimeBestLeaderClass;

            // ...
            public Single LapTimeDeltaSelf;

            // ...
            public Single LapTimeDeltaLeader;

            // ...
            public Single LapTimeDeltaLeaderClass;

            // ...
            public Sectors<Single> SectorTimeDeltaSelf;

            // ...
            public Sectors<Single> SectorTimeDeltaLeader;

            // ...
            public Sectors<Single> SectorTimeDeltaLeaderClass;

            // ...
            public Single TimeDeltaFront;

            // ...
            public Single TimeDeltaBehind;

            // ...
            public Int32 PitWindowStatus;

            // The minute/lap into which you're allowed/obligated to pit
            // Unit: Minutes in time-based sessions, otherwise lap
            public Int32 PitWindowStart;

            // The minute/lap into which you can/should pit
            // Unit: Minutes in time based sessions, otherwise lap
            public Int32 PitWindowEnd;

            // Total number of cut track warnings
            public Int32 CutTrackWarnings;

            // ...
            public CutTrackPenalties Penalties;

            // ...
            public Flags Flags;

            // ...
            public CarDamage CarDamage;

            public Int32 slot_id;

	        // Amount of dirt built up per tyre
	        // Range: 0.0 - 1.0
	        public TyreDirt tyre_dirt;

	        // -1 = no data
	        //  0 = not active
	        //  1 = active
	        public Int32 pit_limiter;

	        // Wheel speed
	        // Unit: Radians per second (rad/s)
	        public WheelSpeed wheel_speed;

	        // Info about track and layout
	        public TrackInfo track_info;

	        // Push to pass data
	        public PushToPass push_to_pass;

	        // Contains name and vehicle info for all drivers in place order
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
	        public DriverData[] all_drivers_data;
        }
    }
}