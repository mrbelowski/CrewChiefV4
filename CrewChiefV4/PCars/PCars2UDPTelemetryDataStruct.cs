using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace CrewChiefV4.PCars2
{
    // simple type to hold a name, so we can map to an array of these
    public struct nameString
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] nameByteArray;
    }

    public enum EUDPStreamerPacketHandlerType
    {
	    eCarPhysics = 0,
	    eRaceDefinition = 1,
	    eParticipants = 2,
	    eTimings = 3,
	    eGameState = 4,
	    eWeatherState = 5, // not sent at the moment, information can be found in the game state packet
	    eVehicleNames = 6, //not sent at the moment
	    eTimeStats = 7,
	    eParticipantVehicleNames = 8
    };

    public struct packetBase
    {
        // starts with packet base (0-12)
        public uint mPacketNumber;						//0 counter reflecting all the packets that have been sent during the game run
        public uint mCategoryPacketNumber;		//4 counter of the packet groups belonging to the given category
        public byte mPartialPacketIndex;			//8 If the data from this class had to be sent in several packets, the index number
        public byte mPartialPacketNumber;			//9 If the data from this class had to be sent in several packets, the total number
        public byte mPacketType;							//10 what is the type of this packet (see EUDPStreamerPacketHanlderType for details)
        public byte mPacketVersion;						//11 what is the version of protocol for this handler, to be bumped with data structure change
    };

    
    /*******************************************************************************************************************
    //
    //	Telemetry data for the viewed participant. 
    //
    //	Frequency: Each tick of the UDP streamer how it is set in the options
    //	When it is sent: in race
    //
    *******************************************************************************************************************/
    public struct sTelemetryData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        // Participant info
        public sbyte sViewedParticipantIndex;	// 12

        public byte sCarFlags;												// 17 1
        public short sOilTempCelsius;									// 18 2
        public ushort sOilPressureKPa;									// 20 2
        public short sWaterTempCelsius;								// 22 2
        public ushort sWaterPressureKpa;								// 24 2
        public ushort sFuelPressureKpa;									// 26 2
        public byte sFuelCapacity;										// 28 1
        public byte sBrake;														// 29 1
        public byte sThrottle;												// 30 1
        public byte sClutch;													// 31 1
        public float sFuelLevel;												// 32 4
        public float sSpeed;														// 36 4
        public ushort sRpm;															// 40 2
        public ushort sMaxRpm;													// 42 2
        public sbyte sSteering;												// 44 1
        public byte sGearNumGears;										// 45 1
        public byte sBoostAmount;											// 46 1
        public byte sCrashState;											// 47 1
        public float sOdometerKM;											// 48 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sOrientation;									// 52 12
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sLocalVelocity;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sWorldVelocity;								// 76 12
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sAngularVelocity;							// 88 12
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sLocalAcceleration;						// 100 12
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sWorldAcceleration;						// 112 12
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] sExtentsCentre;								// 124 12
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTyreFlags;										// 136 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTerrain;											// 140 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sTyreY;												// 144 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sTyreRPS;											// 160 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTyreTemp;											// 176 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sTyreHeightAboveGround;				// 180 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sTyreWear;											// 196 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sBrakeDamage;									// 200 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[] sSuspensionDamage;							// 204 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public short[] sBrakeTempCelsius;							// 208 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTreadTemp;								// 216 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreLayerTemp;								// 224 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreCarcassTemp;							// 232 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreRimTemp;									// 240 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreInternalAirTemp;					// 248 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTempLeft;									// 256 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTempCenter;								// 264 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sTyreTempRight;								// 272 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sWheelLocalPositionY;					// 280 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sRideHeight;										// 296 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sSuspensionTravel;							// 312 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] sSuspensionVelocity;						// 328 16
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sSuspensionRideHeight;					// 344 8
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public ushort[] sAirPressure;									// 352 8
        public float sEngineSpeed;											// 360 4
        public float sEngineTorque;										// 364 4
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2)]
        public byte[] sWings;												// 368 2
        public byte sHandBrake;												// 370 1
        // Car damage
        public byte sAeroDamage;											// 371 1
        public byte sEngineDamage;										// 372 1
        //  HW state
        public uint sJoyPad0;													// 376 4
        public byte sDPad;														// 377 1
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] lfTyreCompound;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] rfTyreCompound;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] lrTyreCompound;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 40)]
        public byte[] rrTyreCompound;
    }

    
    /*******************************************************************************************************************
    //
    //	Race stats data.  
    //
    //	Frequency: Logaritmic decrease
    //	When it is sent: Counter resets on entering InRace state and again each time any of the values changes
    //
    *******************************************************************************************************************/
    public struct sRaceData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        public float sWorldFastestLapTime;								// 12
        public float sPersonalFastestLapTime;							// 16
        public float sPersonalFastestSector1Time;						// 20
        public float sPersonalFastestSector2Time;						// 24
        public float sPersonalFastestSector3Time;						// 28
        public float sWorldFastestSector1Time;							// 32
        public float sWorldFastestSector2Time;							// 36
        public float sWorldFastestSector3Time;							// 40
        public float sTrackLength;										// 44
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] sTrackLocation;				// 48
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] sTrackVariation;				// 112
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] sTranslatedTrackLocation;		// 176
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] sTranslatedTrackVariation;	// 240
        public ushort sLapsTimeInEvent;												// 304 contains lap number for lap based session or quantized session duration (number of 5mins) for timed sessions, the top bit is 1 for timed sessions
        public sbyte sEnforcedPitStopLap;										// 306
    };																						// 308

    
    /*******************************************************************************************************************
    //
    //	Participant names data.  
    //
    //	Frequency: Logarithmic decrease
    //	When it is sent: Counter resets on entering InRace state and again each  the participants change. 
    //	The sParticipantsChangedTimestamp represent last time the participants has changed andis  to be used to sync 
    //	this information with the rest of the participant related packets
    //
    *******************************************************************************************************************/
    public struct sParticipantsData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        public uint sParticipantsChangedTimestamp;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
        public nameString[] sName;	// 323
    };

    /*******************************************************************************************************************
    //
    //	Participant timings data.  
    //
    //	Frequency: Each tick of the UDP streamer how it is set in the options.
    //	When it is sent: in race
    //
    *******************************************************************************************************************/
    public struct sParticipantInfo
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public short[] sWorldPosition;								// 0 -- 
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public short[] sOrientation;								// 6 -- Quantized heading (-PI .. +PI) , Quantized pitch (-PI / 2 .. +PI / 2),  Quantized bank (-PI .. +PI).
        public ushort sCurrentLapDistance;							// 12 --
        public byte sRacePosition;									// 14 -- holds the race position, + top bit shows if the participant is active or not
        public byte sSector;										// 15 -- sector + extra precision bits for x/z position
        public byte sHighestFlag;									// 16 --
        public byte sPitModeSchedule;								// 17 --
        public ushort sCarIndex;										// 18 -- top bit shows if participant is (local or remote) human player or not
        public byte sRaceState;										// 20 -- race state flags + invalidated lap indication --
        public byte sCurrentLap;									// 21 -- 
        public float sCurrentTime;									// 22 --
        public float sCurrentSectorTime;								// 26 --
    };	


    public struct sTimingsData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        public sbyte sNumParticipants;										// 12 --
        public uint sParticipantsChangedTimestamp;							// 13 -- 
        public float sEventTimeRemaining;									// 17  // time remaining, -1 for invalid time,  -1 - laps remaining in lap based races  --
        public float sSplitTimeAhead;										// 21 --
        public float sSplitTimeBehind;										// 25 -- 
        public float sSplitTime;												// 29 --
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        sParticipantInfo[] sPartcipants;		// 33 960
    };																						// 30

    /*******************************************************************************************************************
    //
    //	Game State. 
    //
    //	Frequency: Each 5s while being in Main Menu, Each 10s while being in race + on each change Main Menu<->Race several times.
    //	the frequency in Race is increased in case of weather timer being faster  up to each 5s for 30x time progression
    //	When it is sent: Always
    //
    *******************************************************************************************************************/
    public struct sGameStateData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        public ushort mBuildVersionNumber; 		//12
        public byte mGameState;					//15 -- first 3 bits are used for game state enum, second 3 bits for session state enum See shared memory example file for the enums
        public sbyte sAmbientTemperature;		//16
        public sbyte sTrackTemperature;			//17
        public byte sRainDensity;				//18
        public byte sSnowDensity;				//19
        public sbyte sWindSpeed;					//20
        public sbyte sWindDirectionX;			//21
        public sbyte sWindDirectionY;			//22 padded to 24
    };


    /*******************************************************************************************************************
    //
    //	Participant Stats and records
    //
    //	Frequency: When entering the race and each time any of the values change, so basically each time any of the participants
    //						crosses a sector boundary.
    //	When it is sent: In Race
    //
    *******************************************************************************************************************/
    public struct sParticipantStatsInfo
    {
        public float sFastestLapTime;								// 0
        public float sLastLapTime;									// 4
        public float sLastSectorTime;								// 8
        public float sFastestSector1Time;							// 11
        public float sFastestSector2Time;							// 16
        public float sFastestSector3Time;							// 20
    };																					// 24

    public struct sParticipantsStats
    {
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 32)]
        public sParticipantStatsInfo sParticipants; //768
    };

    public struct sTimeStatsData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        public uint sParticipantsChangedTimestamp;					// 12
        public sParticipantsStats sStats;											// 16 + 768
    };																					// 784


    /*******************************************************************************************************************
    //
    //	Participant Vehicle names
    //
    //	Frequency: Logarithmic decrease
    //	When it is sent: Counter resets on entering InRace state and again each  the participants change. 
    //	The sParticipantsChangedTimestamp represent last time the participants has changed and is  to be used to sync 
    //	this information with the rest of the participant related packets
    //
    //	Note: This data is always sent with at least 2 packets. The 1-(n-1) holds the vehicle name for each participant
    //	The last one holding the class names.
    //
    *******************************************************************************************************************/
    public struct sVehicleInfo
    {
        public ushort sIndex; // 0 2
        public uint sClass; // 2 6 
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] sName; // 6 70
    }; // padded to 72


    public struct sParticipantVehicleNamesData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
        public sVehicleInfo[] sVehicles; //12 16*72
    };	// 1164

    public struct sClassInfo
    {
        public uint sClassIndex; // 0 4 
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 20)]
        public byte[] sName; // 4 24
    };

    public struct sVehicleClassNamesData
    {
        // starts with packet base (0-12)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 12)]
        public packetBase mPacketBase;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 60)]
        public sClassInfo[] sClasses; //12 24*60
    };				 			// 1452
}
