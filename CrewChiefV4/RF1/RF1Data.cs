using System;
using System.Runtime.InteropServices;

namespace CrewChiefV4.rFactor1
{
    class rFactor1Constant
    {
        public const string SharedMemoryName = "$rFactorShared$";

        public enum rfGamePhase
        {
			garage = 0,
			warmUp = 1,
			gridWalk = 2,
			formation = 3,
			countdown = 4,
			greenFlag = 5,
			fullCourseYellow = 6,
			sessionStopped = 7,
			sessionOver = 8
        }

        public enum rfYellowFlagState
        {
			invalid = -1,
			noFlag = 0,
			pending = 1,
			pitClosed = 2,
			pitLeadLap = 3,
			pitOpen = 4,
			lastLap = 5,
			resume = 6,
			raceHalt = 7
        }

        public enum rfSurfaceType
        {
			dry = 0,
			wet = 1,
			grass = 2,
			dirt = 3,
			gravel = 4,
			kerb = 5
        }

        public enum rfSector
        {
			sector3 = 0,
			sector1 = 1,
			sector2 = 2
        }

        public enum rfFinishStatus
        {
			none = 0,
			finished = 1,
			dnf = 2,
			dq = 3
        }

        public enum rfControl
        {
			nobody = -1,
			player = 0,
			ai = 1,
			remote = 2,
			replay = 3
        }

        public enum rfWheelIndex
        {
			frontLeft = 0,
			frontRight = 1,
			rearLeft = 2,
			rearRight = 3
        }
    }

    namespace rFactor1Data
    {
        [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)]
        public struct rfVec3
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 67, Pack = 1)]
        public struct rfWheel
        {
            public float rotation;
            public float suspensionDeflection;
            public float rideHeight;
            public float tireLoad;
            public float lateralForce;
            public float gripFract;
            public float brakeTemp;
            public float pressure;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] temperature;
            public float wear;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] terrainName;
            public byte surfaceType;
            public byte flat;
            public byte detached;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi, Size = 304, Pack = 1)]
        public struct rfVehicleInfo
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] driverName;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] vehicleName;
            public short totalLaps;
            public sbyte sector;
            public sbyte finishStatus;
            public float lapDist;
            public float pathLateral;
            public float trackEdge;
            public float bestSector1;
            public float bestSector2;
            public float bestLapTime;
            public float lastSector1;
            public float lastSector2;
            public float lastLapTime;
            public float curSector1;
            public float curSector2;
            public short numPitstops;
            public short numPenalties;
            public byte isPlayer;
            public sbyte control;
            public byte inPits;
            public byte place;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] vehicleClass;
            public float timeBehindNext;
            public int lapsBehindNext;
            public float timeBehindLeader;
            public int lapsBehindLeader;
            public float lapStartET;
            public rfVec3 pos;
            public rfVec3 localVel;
            public rfVec3 localAccel;
            public rfVec3 oriX;
            public rfVec3 oriY;
            public rfVec3 oriZ;
            public rfVec3 localRot;
            public rfVec3 localRotAccel;
            public float speed;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi, Size = 39647, Pack = 1)]
        public struct rfShared
        {
            public float deltaTime;
            public int lapNumber;
            public float lapStartET;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] vehicleName;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] trackName;

            public rfVec3 pos;
            public rfVec3 localVel;
            public rfVec3 localAccel;

            public rfVec3 oriX;
            public rfVec3 oriY;
            public rfVec3 oriZ;
            public rfVec3 localRot;
            public rfVec3 localRotAccel;

            public float speed;

            public int gear;
            public float engineRPM;
            public float engineWaterTemp;
            public float engineOilTemp;
            public float clutchRPM;

            public float unfilteredThrottle;
            public float unfilteredBrake;
            public float unfilteredSteering;
            public float unfilteredClutch;

            public float steeringArmForce;
            public float fuel;
            public float engineMaxRPM;
            public byte scheduledStops;
            public byte overheating;
            public byte detached;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] dentSeverity;
            public float lastImpactET;
            public float lastImpactMagnitude;
            public rfVec3 lastImpactPos;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
            public rfWheel[] wheel;

            public int session;
            public float currentET;
            public float endET;
            public int maxLaps;
            public float lapDist;

            public int numVehicles;

            public byte gamePhase;

            public sbyte yellowFlagState;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public sbyte[] sectorFlag;
            public byte startLight;
            public byte numRedLights;
            public byte inRealtime;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] playerName;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] plrFileName;

            public float ambientTemp;
            public float trackTemp;
            public rfVec3 wind;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 128)]
            public rfVehicleInfo[] vehicle;
        }
    }
}