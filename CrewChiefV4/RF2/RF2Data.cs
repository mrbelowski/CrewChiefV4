using System;
using System.Runtime.InteropServices;

namespace CrewChiefV4.rFactor2
{
    class rFactor2Constant
    {
        public const string SharedMemoryName = "$rFactor2Shared$";

        public enum rf2GamePhase
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

        public enum rf2YellowFlagState
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

        public enum rf2SurfaceType
        {
			dry = 0,
			wet = 1,
			grass = 2,
			dirt = 3,
			gravel = 4,
			kerb = 5
        }

        public enum rf2Sector
        {
			sector3 = 0,
			sector1 = 1,
			sector2 = 2
        }

        public enum rf2FinishStatus
        {
			none = 0,
			finished = 1,
			dnf = 2,
			dq = 3
        }

        public enum rf2Control
        {
			nobody = -1,
			player = 0,
			ai = 1,
			remote = 2,
			replay = 3
        }

        public enum rf2WheelIndex
        {
			frontLeft = 0,
			frontRight = 1,
			rearLeft = 2,
			rearRight = 3
        }
    }

    namespace rFactor2Data
    {
        [StructLayout(LayoutKind.Sequential, Size = 12, Pack = 1)]
        public struct rf2Vec3
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Size = 67, Pack = 1)]
        public struct rf2Wheel
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
        public struct rf2VehicleInfo
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
            public rf2Vec3 pos;
            public rf2Vec3 localVel;
            public rf2Vec3 localAccel;
            public rf2Vec3 oriX;
            public rf2Vec3 oriY;
            public rf2Vec3 oriZ;
            public rf2Vec3 localRot;
            public rf2Vec3 localRotAccel;
            public float speed;
        }

        [StructLayout(LayoutKind.Sequential, CharSet=CharSet.Ansi, Size = 39647, Pack = 1)]
        public struct rf2Shared
        {
            public float deltaTime;
            public int lapNumber;
            public float lapStartET;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] vehicleName;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] trackName;

            public rf2Vec3 pos;
            public rf2Vec3 localVel;
            public rf2Vec3 localAccel;

            public rf2Vec3 oriX;
            public rf2Vec3 oriY;
            public rf2Vec3 oriZ;
            public rf2Vec3 localRot;
            public rf2Vec3 localRotAccel;

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
            public rf2Vec3 lastImpactPos;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
            public rf2Wheel[] wheel;

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
            public rf2Vec3 wind;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 128)]
            public rf2VehicleInfo[] vehicle;
        }
    }
}