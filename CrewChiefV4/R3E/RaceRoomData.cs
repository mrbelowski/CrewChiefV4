using System;
using System.Runtime.InteropServices;

namespace CrewChiefV4.RaceRoom
{
    class RaceRoomConstant
    {
        public const string SharedMemoryName = "$R3E";

        enum VersionMajor
        {
            // Major version number to test against
            R3E_VERSION_MAJOR = 1
        };

        enum VersionMinor
        {
            // Minor version number to test against
            R3E_VERSION_MINOR = 1
        };


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
            // TODO: verify this. Why has "garage" changed from 0?
            Garage = 1,
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

        public enum PitStopStatus
        {
             // No mandatory pitstops
             Unavailable = -1,
 
             // Mandatory pitstop not served yet
             Unserved = 0,
 
             // Mandatory pitstop served
             Served = 1,
        };

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

        public enum TireType
        {
            Unavailable = -1,
            DTM_Option = 0,
            Prime = 1
        }        

        // TODO: is this still used?
        public enum PitStopStatus
        {
            // No mandatory pitstops
            R3E_PITSTOP_STATUS_UNAVAILABLE = -1,

            // Mandatory pitstop not served yet
            R3E_PITSTOP_STATUS_UNSERVED = 0,

            // Mandatory pitstop served
            R3E_PITSTOP_STATUS_SERVED = 1
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

            // Virtual physics time
            // Unit: Seconds
            public Double GameSimulationTime;

            // Car world-space position
            public Vector3<Double> Position;

            // Car world-space velocity
            // Unit: Meter per second (m/s)
            public Vector3<Double> Velocity;

            // Car local-space velocity
            // Unit: Meter per second (m/s)
            public Vector3<Double> LocalVelocity;

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

            // Car world-space angular velocity
            // Unit: Radians per second
            public Vector3<Double> AngularVelocity;

            // Car local-space angular velocity
            // Unit: Radians per second
            public Vector3<Double> LocalAngularVelocity;
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

            // note that tyre damage has been moved
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct TireData
        {
            public Single FrontLeft;
            public Single FrontRight;
            public Single RearLeft;
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
        public struct DRS
        {
            // If DRS is equipped and allowed
            // 0 = No, 1 = Yes, -1 = N/A
            public Int32 Equipped;
            // Got DRS activation left
            // 0 = No, 1 = Yes, -1 = N/A
            public Int32 Available;
            // Number of DRS activations left this lap
            // Note: In sessions with 'endless' amount of drs activations per lap this value starts at int32::max
            // -1 = N/A
            public Int32 NumActivationsLeft;
            // DRS engaged
            // 0 = No, 1 = Yes, -1 = N/A
            public Int32 Engaged;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct PushToPass
        {
            public Int32 Available;
            public Int32 Engaged;
            public Int32 AmountLeft;
            public Single EngagedTimeLeft;
            public Single WaitTimeLeft;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct Sectors<T>
        {
            public T Sector1;
            public T Sector2;
            public T Sector3;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DriverInfo
        {
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] Name; // UTF-8
            public Int32 CarNumber;
            public Int32 ClassId;
            public Int32 ModelId;
            public Int32 TeamId;
            public Int32 LiveryId;
            public Int32 ManufacturerId;
            public Int32 SlotId;
            public Int32 ClassPerformanceIndex;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct DriverData
        {
            public DriverInfo DriverInfo;
            // Note: See the R3E.Constant.FinishStatus enum
            public Int32 FinishStatus;
            public Int32 Place;
            public Single LapDistance;
            public Vector3<Single> Position;
            public Int32 TrackSector;
            public Int32 CompletedLaps;
            public Int32 CurrentLapValid;
            public Single LapTimeCurrentSelf;
            public Sectors<Single> SectorTimeCurrentSelf;
            public Sectors<Single> SectorTimePreviousSelf;
            public Sectors<Single> SectorTimeBestSelf;
            public Single TimeDeltaFront;
            public Single TimeDeltaBehind;
            public Int32 PitStopStatus;
            public Int32 InPitlane;
            public Int32 NumPitstops;
            public CutTrackPenalties Penalties;
            public Single CarSpeed;
            // Note: See the R3E.Constant.TireType enum
            public Int32 TireType;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct RaceRoomShared
        {
            //////////////////////////////////////////////////////////////////////////
            // Version
            //////////////////////////////////////////////////////////////////////////
            public Int32 VersionMajor;
            public Int32 VersionMinor;
            public Int32 AllDriversOffset; // Offset to NumCars variable
            public Int32 DriverDataSize; // Size of DriverData

            //////////////////////////////////////////////////////////////////////////
            // Game State
            //////////////////////////////////////////////////////////////////////////

            public Int32 GamePaused;
            public Int32 GameInMenus;

            //////////////////////////////////////////////////////////////////////////
            // High Detail
            //////////////////////////////////////////////////////////////////////////

            // High precision data for player's vehicle only
            public PlayerData Player;

            //////////////////////////////////////////////////////////////////////////
            // Event And Session
            //////////////////////////////////////////////////////////////////////////

            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] TrackName; // UTF-8
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] LayoutName; // UTF-8

            public Int32 TrackId;
            public Int32 LayoutId;

            // Layout length in meters
            public Single LayoutLength;

            // The current race event index, for championships with multiple events
            // Note: 0-indexed, -1 = N/A
            public Int32 EventIndex;

            // Which session the player is in (practice, qualifying, race, etc.)
            // Note: See the R3E.Constant.Session enum
            public Int32 SessionType;

            // The current iteration of the current type of session (second qualifying session, etc.)
            // Note: 0-indexed, -1 = N/A
            public Int32 SessionIteration;

            // Which phase the current session is in (gridwalk, countdown, green flag, etc.)
            // Note: See the R3E.Constant.SessionPhase enum
            public Int32 SessionPhase;

            // -1 = no data available
            //  0 = not active
            //  1 = active
            public Int32 TireWearActive;

            // -1 = no data
            //  0 = not active
            //  1 = active
            public Int32 FuelUseActive;

            // Total number of laps in the race, or -1 if player is not in race mode (practice, test mode, etc.)
            public Int32 NumberOfLaps;

            // Amount of time remaining for the current session
            // Note: Only available in time-based sessions, -1.0 = N/A
            // Units: Seconds
            public Single SessionTimeRemaining;

            //////////////////////////////////////////////////////////////////////////
            // Pit
            //////////////////////////////////////////////////////////////////////////

            // Current status of the pit stop
            // Note: See the R3E.Constant.PitWindow enum
            public Int32 PitWindowStatus;

            // The minute/lap from which you're obligated to pit (-1 = N/A)
            // Unit: Minutes in time-based sessions, otherwise lap
            public Int32 PitWindowStart;

            // The minute/lap into which you need to have pitted (-1 = N/A)
            // Unit: Minutes in time-based sessions, otherwise lap
            public Int32 PitWindowEnd;

            // If current vehicle is in pitline (-1 = N/A)
            public Int32 InPitlane;

            // Number of pitstops the current vehicle has performed (-1 = N/A)
            public Int32 NumPitstopsPerformed;

            //////////////////////////////////////////////////////////////////////////
            // Scoring & Timings
            //////////////////////////////////////////////////////////////////////////

            // The current state of each type of flag
            public Flags Flags;

            // Current position (1 = first place)
            public Int32 Position;

            // Note: See the R3E.Constant.FinishStatus enum
            public Int32 FinishStatus;

            // Total number of cut track warnings (-1 = N/A)
            public Int32 CutTrackWarnings;

            // The number of penalties the car currently has pending of each type (-1 = N/A)
            public CutTrackPenalties Penalties;
            // Total number of penalties pending for the car
            // Note: See the 'penalties' field
            public Int32 NumPenalties;

            // How many laps the player has completed. If this value is 6, the player is on his 7th lap. -1 = n/a
            public Int32 CompletedLaps;
            public Int32 CurrentLapValid;
            public Int32 TrackSector;
            public Single LapDistance;
            // fraction of lap completed, 0.0-1.0, -1.0 = N/A
            public Single LapDistanceFraction;

            // The current best lap time for the leader of the session (-1.0 = N/A)
            public Single LapTimeBestLeader;
            // The current best lap time for the leader of the player's class in the current session (-1.0 = N/A)
            public Single LapTimeBestLeaderClass;
            // Sector times of fastest lap by anyone in session
            // Unit: Seconds (-1.0 = N/A)
            public Sectors<Single> SectorTimesSessionBestLap;
            // Unit: Seconds (-1.0 = none)
            public Single LapTimeBestSelf;
            public Sectors<Single> SectorTimesBestSelf;
            // Unit: Seconds (-1.0 = none)
            public Single LapTimePreviousSelf;
            public Sectors<Single> SectorTimesPreviousSelf;
            // Unit: Seconds (-1.0 = none)
            public Single LapTimeCurrentSelf;
            public Sectors<Single> SectorTimesCurrentSelf;
            // The time delta between the player's time and the leader of the current session (-1.0 = N/A)
            public Single LapTimeDeltaLeader;
            // The time delta between the player's time and the leader of the player's class in the current session (-1.0 = N/A)
            public Single LapTimeDeltaLeaderClass;
            // Time delta between the player and the car placed in front (-1.0 = N/A)
            // Units: Seconds
            public Single TimeDeltaFront;
            // Time delta between the player and the car placed behind (-1.0 = N/A)
            // Units: Seconds
            public Single TimeDeltaBehind;

            //////////////////////////////////////////////////////////////////////////
            // Vehicle information
            //////////////////////////////////////////////////////////////////////////

            public DriverInfo VehicleInfo;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] PlayerName; // UTF-8

            //////////////////////////////////////////////////////////////////////////
            // Vehicle State
            //////////////////////////////////////////////////////////////////////////

            // Which controller is currently controlling the player's car (AI, player, remote, etc.)
            // Note: See the R3E.Constant.Control enum
            public Int32 ControlType;

            // Unit: Meter per second (m/s)
            public Single CarSpeed;

            // Unit: Radians per second (rad/s)
            public Single EngineRps;
            public Single MaxEngineRps;

            // -2 = N/A, -1 = reverse, 0 = neutral, 1 = first gear, ...
            public Int32 Gear;
            // -1 = N/A
            public Int32 NumGears;

            // Physical location of car's center of gravity in world space (X, Y, Z) (Y = up)
            public Vector3<Single> CarCgLocation;
            // Pitch, yaw, roll
            // Unit: Radians (rad)
            public Orientation<Single> CarOrientation;
            // Acceleration in three axes (X, Y, Z) of car body in local-space.
            // From car center, +X=left, +Y=up, +Z=back.
            // Unit: Meter per second squared (m/s^2)
            public Vector3<Single> LocalAcceleration;

            // Unit: Liters (l)
            // Note: Not valid for AI or remote players
            public Single FuelLeft;
            public Single FuelCapacity;
            // Unit: Celsius (C)
            // Note: Not valid for AI or remote players
            public Single EngineWaterTemp;
            public Single EngineOilTemp;
            // Unit: Kilopascals (KPa)
            // Note: Not valid for AI or remote players
            public Single FuelPressure;
            // Unit: Kilopascals (KPa)
            // Note: Not valid for AI or remote players
            public Single EngineOilPressure;

            // How pressed the throttle pedal is 
            // Range: 0.0 - 1.0 (-1.0 = N/A)
            // Note: Not valid for AI or remote players
            public Single ThrottlePedal;
            // How pressed the brake pedal is
            // Range: 0.0 - 1.0 (-1.0 = N/A)
            // Note: Not valid for AI or remote players
            public Single BrakePedal;
            // How pressed the clutch pedal is 
            // Range: 0.0 - 1.0 (-1.0 = N/A)
            // Note: Not valid for AI or remote players
            public Single ClutchPedal;

            // DRS data
            public DRS Drs;

            // Pit limiter (-1 = N/A, 0 = inactive, 1 = active)
            public Int32 PitLimiter;

            // Push to pass data
            public PushToPass PushToPass;

            // How much the vehicle's brakes are biased towards the back wheels (0.3 = 30%, etc.) (-1.0 = N/A)
            // Note: Not valid for AI or remote players
            public Single BrakeBias;

            //////////////////////////////////////////////////////////////////////////
            // Tires
            //////////////////////////////////////////////////////////////////////////

            // Which type of tires the player's car has (option, prime, etc.)
            // Note: See the R3E.Constant.TireType enum
            public Int32 TireType;

            // Rotation speed
            // Uint: Radians per second
            public TireData TireRps;
            // Range: 0.0 - 1.0 (-1.0 = N/A)
            public TireData TireGrip;
            // Range: 0.0 - 1.0 (-1.0 = N/A)
            public TireData TireWear;
            // Unit: Kilopascals (KPa) (-1.0 = N/A)
            // Note: Not valid for AI or remote players
            public TireData TirePressure;
            // Percentage of dirt on tire (-1.0 = N/A)
            // Range: 0.0 - 1.0
            public TireData TireDirt;
            // Brake temperature (-1.0 = N/A)
            // Unit: Celsius (C)
            // Note: Not valid for AI or remote players
            public TireData BrakeTemp;
            // Temperature of three points across the tread of the tire (-1.0 = N/A)
            // Unit: Celsius (C)
            // Note: Not valid for AI or remote players
            public TireTemperature TireTemp;

            //////////////////////////////////////////////////////////////////////////
            // Damage
            //////////////////////////////////////////////////////////////////////////

            // The current state of various parts of the car
            // Note: Not valid for AI or remote players
            public CarDamage CarDamage;

            //////////////////////////////////////////////////////////////////////////
            // Driver Info
            //////////////////////////////////////////////////////////////////////////

            // Number of cars (including the player) in the race
            public Int32 NumCars;

            // Contains name and basic vehicle info for all drivers in place order
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 128)]
            public DriverData[] DriverData;
        }
    }
}