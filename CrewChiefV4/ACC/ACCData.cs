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

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct Vec3
        {
            public float x;
            public float y;
            public float z;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct Rotation
        {
            public float pitch;
            public float yaw;
            public float roll;
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct Track
        {
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 260)]
            public String name;
            public int length;
            public int corners;
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
	        public float speed;
	        public int lapCount;
	
        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct SessionData
        {


        }

        [StructLayout(LayoutKind.Sequential, Pack = 4, CharSet = CharSet.Ansi)]
        [Serializable]
        public struct  ACCSharedMemoryData
        {
            public bool isReady;
            public double update;
            public SessionData sessionData;
            public Track track;
            public Driver playerDriver;
            [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
            public Driver[]opponentDrivers;
	        public int opponentDriverCount;
        }
        
    }    
}
