using System;
using System.Runtime.InteropServices;

namespace CrewChiefV4.ACC
{
    class ACCConstant
    {
        public const string SharedMemoryName = "Local\\CrewChief_ACC";
    }    
    namespace Data
    {
        public enum CarLocation  : byte
        {
	        ECarLocation__Null = 0,
	        ECarLocation__Track = 1,
	        ECarLocation__PitLane = 2,
	        ECarLocation__PitEntry = 3,
	        ECarLocation__PitExit = 4,
	        ECarLocation__ECarLocation_MAX = 5
        };

        public enum RaceSessionType : byte
	    {
		    FreePractice1 = 0,
		    FreePractice2 = 1,
		    PreQualifying = 2,
		    WarmUp = 3,
		    Qualifying = 4,
		    Qualifying1 = 5,
		    Qualifying2 = 6,
		    Qualifying3 = 7,
		    Qualifying4 = 8,
		    Superpole = 9,
		    Race = 10,
		    Hotlap = 11,
		    Hotstint = 12,
		    HotlapSuperpole = 13,
            RaceSessionType_Max = 14,
	    };

	    public enum  RaceSessionPhase  : byte
	    {
		    StartingUI = 0,
		    PreFormationTime = 1,
		    FormationTime = 2,
		    PreSessionTime = 3,
		    SessionTime = 4,
		    SessionOverTime = 5,
		    PostSessionTime = 6,
		    ResultUI = 7,
            RaceSessionPhase_Max = 8, 
	    };

	    public enum  DriverCategory  : byte
	    {
		    EDriverCategory__Platinum = 0,
		    EDriverCategory__Gold = 1,
		    EDriverCategory__Silver = 2,
		    EDriverCategory__Bronze = 3,
		    EDriverCategory__EDriverCategory_MAX = 4
	    };

	    public enum  Nationality  : byte
	    {
		    ENationality__Any = 0,
		    ENationality__Italy = 1,
		    ENationality__Germany = 2,
		    ENationality__France = 3,
		    ENationality__Spain = 4,
		    ENationality__GreatBritain = 5,
		    ENationality__Hungary = 6,
		    ENationality__Belgium = 7,
		    ENationality__Switzerland = 8,
		    ENationality__Austria = 9,
		    ENationality__Russia = 10,
		    ENationality__Thailand = 11,
		    ENationality__Netherlands = 12,
		    ENationality__Poland = 13,
		    ENationality__Argentina = 14,
		    ENationality__Monaco = 15,
		    ENationality__Ireland = 16,
		    ENationality__Brazil = 17,
		    ENationality__SouthAfrica = 18,
		    ENationality__PuertoRico = 19,
		    ENationality__Slovakia = 20,
		    ENationality__Oman = 21,
		    ENationality__Greece = 22,
		    ENationality__SaudiArabia = 23,
		    ENationality__Norway = 24,
		    ENationality__Turkey = 25,
		    ENationality__SouthKorea = 26,
		    ENationality__Lebanon = 27,
		    ENationality__Armenia = 28,
		    ENationality__Mexico = 29,
		    ENationality__Sweden = 30,
		    ENationality__Finland = 31,
		    ENationality__Denmark = 32,
		    ENationality__Croatia = 33,
		    ENationality__Canada = 34,
		    ENationality__China = 35,
		    ENationality__Portugal = 36,
		    ENationality__ENationality_MAX = 37
	    };

	    public enum  CarModelType  : byte
	    {
		    ECarModelType__Porsche_991_GT3_R = 0,
		    ECarModelType__Mercedes_AMG_GT3 = 1,
		    ECarModelType__Ferrari_488_GT3 = 2,
		    ECarModelType__Audi_R8_LMS = 3,
		    ECarModelType__Lamborghini_Huracan_GT3 = 4,
		    ECarModelType__Mclaren_650s_GT3 = 5,
		    ECarModelType__Nissan_GT_R_Nismo_GT3 = 6,
		    ECarModelType__BMW_M6_GT3 = 7,
		    ECarModelType__Bentley_Continental_GT3 = 8,
		    ECarModelType__Porsche_991II_GT3_Cup = 9,
		    ECarModelType__Nissan_GT_R_Nismo_GT301 = 10,
		    ECarModelType__Bentley_Continental_GT301 = 11,
		    ECarModelType__Aston_Martin_Vantage_V12_GT3 = 12,
		    ECarModelType__Lamborghini_Gallardo_R_EX = 13,
		    ECarModelType__Jaguar_G3 = 14,
		    ECarModelType__Lexus_RC_F_GT3 = 15,
		    ECarModelType__Lamborghini_Huracan_GT301 = 16,
		    ECarModelType__ECarModelType_MAX = 17
	    };

	    public enum  MarshalFlagType  : byte
	    {
		    EMarshalFlagType__White = 0,
		    EMarshalFlagType__Green = 1,
		    EMarshalFlagType__Red = 2,
		    EMarshalFlagType__Blue = 3,
		    EMarshalFlagType__Yellow = 4,
		    EMarshalFlagType__Black = 5,
		    EMarshalFlagType__BlackWhite = 6,
		    EMarshalFlagType__Checkered = 7,
		    EMarshalFlagType__OrangeCircle = 8,
		    EMarshalFlagType__RedYellowStipes = 9,
		    EMarshalFlagType__None = 10,
		    EMarshalFlagType__EMarshalFlagType_MAX = 11
	    };

	    public enum  CupCategory  : byte
	    {
		    ECupCategory__Overall = 0,
		    ECupCategory__ProAm = 1,
		    ECupCategory__Am = 2,
		    ECupCategory__Silver = 3,
		    ECupCategory__National = 4,
		    ECupCategory__ECupCategory_MAX = 5
	    };

	    public enum RaceEventType  : byte
	    {
		    ERaceEventType__A_3H = 0,
		    ERaceEventType__B_24H = 1,
		    ERaceEventType__C_6H = 2,
		    ERaceEventType__D_1H = 3,
		    ERaceEventType__ERaceEventType_MAX = 4
	    };

	    public enum LapStateFlags : byte
	    {
		    HasCut = 0x0,
		    IsInvalidLap = 1,
		    HasPenalty = 2,
		    IsOutLap = 3,
		    IsInLap = 4,
		    IsFormationLap = 5,
		    IsSafetyCarOnTrack = 6,
		    IsFullCourseYellow = 7,
		    IsRetired = 8,
		    IsDisqualified = 9,
		    IsOnPitWorkingZone = 10,
		    DriverSwap = 11,
	    };

	    public enum PitStopRepairType : byte
	    {
		    Chassis = 0x0,
		    SuspensionLF = 0x1,
		    SuspensionRF = 0x2,
		    SuspensionLR = 0x3,
		    SuspensionRR = 0x4,
		    Brakes = 0x5,
		    Radiator = 0x6,
		    GearBox = 0x7,
	    };

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [Serializable]
        public struct Vec3
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [Serializable]
        public struct Rotation
        {
            public float pitch;
            public float yaw;
            public float roll;
        }
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        [Serializable]
        public struct WeatherStatus
        {
            public float ambientTemperature;
            public float roadTemperature;
            public float wetLevel;
            public float windSpeed;
            public float windDirection;
            public float rainLevel;
            public float cloudLevel;
        };
        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct Track
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String name;
            public int Id;
            public float length;
            public int sectors;
            public int corners;
            public bool isPolesitterOnLeft;
            public WeatherStatus weatherState;
            
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct Driver
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
	        public String name;
	        public int nation;
	        public Vec3 location;
	        public Rotation rotation;
            public float distanceRoundTrack;
            public float speed;
            public float lastSectorTimeStamp;
            public int position;
            public int realTimePosition;
            public int lapCount;
            public int totalTime;
	        public int currentDelta;
	        public uint currentSector;
            public int currentlaptime;
            public float trottle;
            public float brake;
            public float clutch;
            public float rpm;
	        public bool isBetweenSafetyCarLines;	        
	        public bool isSessionOver;
	        public bool isDisqualified;
	        public bool isRetired;
            char pad1;
            char pad2;   
            public UInt16 driverIndex;
            public byte formationLapCounter;
            public CarLocation trackLocation;	
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct SessionData
        {
            public float physicsTime;
            public float sessionStartTimeStamp;
            public float receivedServerTime;
            public float serverTimeOffset;
            public float sessionStartTime;
            public float sessionEndTime;
            public bool isServer;
            public bool isClient;
            public bool areCarsInitializated;
            public bool isTimeStopped;
            public bool isEventInitializated;
            public bool isSessionInitializated;        
            public UInt16 currentEventIndex;
            public UInt16 currentSessionIndex;
            public RaceSessionType currentSessionType;
            public RaceSessionPhase currentSessionPhase;
            char pad1;
            char pad2;        
        }

        [StructLayout(LayoutKind.Sequential, Pack = 1)]
        [Serializable]
        public struct  ACCSharedMemoryData
        {
            public SessionData sessionData;
            public Track track;
            public bool isReady;
            public float update;            
            public Driver playerDriver;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public Driver[]opponentDrivers;
	        public int opponentDriverCount;

        }        
    }    
}
