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
        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct rfVec3
        {
            public Single x;
            public Single y;
            public Single z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct rfWheel
        {
            public Single rotation;
            public Single suspensionDeflection;
            public Single rideHeight;
            public Single tireLoad;
            public Single lateralForce;
            public Single gripFract;
            public Single brakeTemp;
            public Single pressure;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public Single[] temperature;
            public Single wear;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] terrainName;
            public byte surfaceType;
            public bool flat;
            public bool detached;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct rfVehicleInfo
        {
        	[MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] driverName;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] vehicleName;
            public Int16 totalLaps;
            public sbyte sector;
            public sbyte finishStatus;
            public Single lapDist;
            public Single pathLateral;
            public Single trackEdge;

            public Single bestSector1;
            public Single bestSector2;
            public Single bestLapTime;
            public Single lastSector1;
            public Single lastSector2;
            public Single lastLapTime;
            public Single curSector1;
            public Single curSector2;

            public Int16 numPitstops;
            public Int16 numPenalties;
            public bool isPlayer;
            public sbyte control;
            public bool inPits;
            public byte place;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] vehicleClass;

            public Single timeBehindNext;
            public Int64 lapsBehindNext;
            public Single timeBehindLeader;
            public Int64 lapsBehindLeader;
            public Single lapStartET;

            public rfVec3 pos;
            public rfVec3 localVel;
            public rfVec3 localAccel;

            public rfVec3 oriX;
            public rfVec3 oriY;
            public rfVec3 oriZ;
            public rfVec3 localRot;
            public rfVec3 localRotAccel;

            public Single speed;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        public struct rfShared
        {
            public Single deltaTime;
            public Int64 lapNumber;
            public Single lapStartET;
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

            public Single speed;

            public Int64 gear;
            public Single engineRPM;
            public Single engineWaterTemp;
            public Single engineOilTemp;
            public Single clutchRPM;

            public Single unfilteredThrottle;
            public Single unfilteredBrake;
            public Single unfilteredSteering;
            public Single unfilteredClutch;

            public Single steeringArmForce;
            public Single fuel;
            public Single engineMaxRPM;
            public byte scheduledStops;
            public bool overheating;
            public bool detached;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] dentSeverity;
            public Single lastImpactET;
            public Single lastImpactMagnitude;
            public rfVec3 lastImpactPos;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
            public rfWheel[] wheel;

            public Int64 session;
            public Single currentET;
            public Single endET;
            public Int64 maxLaps;
            public Single lapDist;

            public Int64 numVehicles;

            public byte gamePhase;

            public sbyte yellowFlagState;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public sbyte[] sectorFlag;
            public byte startLight;
            public byte numRedLights;
            public bool inRealtime;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst =32)]
            public byte[] playerName;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] plrFileName;

            public Single ambientTemp;
            public Single trackTemp;
            public rfVec3 wind;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 128)]
            public rfVehicleInfo[] vehicle;
        }
    }
}