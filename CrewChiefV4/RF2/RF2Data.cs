/*
rF2 internal state mapping structures.  Allows access to native C++ structs from C#.
Must be kept in sync with Include\rF2State.h.

See: MainForm.MainUpdate for sample on how to marshall from native in memory struct.

Author: The Iron Wolf (vleonavicius@hotmail.com)
Website: thecrewchief.org
*/
using System;
using System.Runtime.InteropServices;
using System.Xml.Serialization;

// CC specific: Mark more common unused members with XmlIgnore for reduced trace sizes.

namespace CrewChiefV4.rFactor2
{
    // Marshalled types:
    // C++                 C#
    // char          ->    byte
    // unsigned char ->    byte
    // signed char   ->    sbyte
    // bool          ->    byte
    // long          ->    int
    public class rFactor2Constants
    {
        public const string MM_TELEMETRY_FILE_NAME1 = "$rFactor2SMMP_TelemetryBuffer1$";
        public const string MM_TELEMETRY_FILE_NAME2 = "$rFactor2SMMP_TelemetryBuffer2$";
        public const string MM_TELEMETRY_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMP_TelemeteryMutex";

        public const string MM_SCORING_FILE_NAME1 = "$rFactor2SMMP_ScoringBuffer1$";
        public const string MM_SCORING_FILE_NAME2 = "$rFactor2SMMP_ScoringBuffer2$";
        public const string MM_SCORING_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMP_ScoringMutex";

        public const string MM_EXTENDED_FILE_NAME1 = "$rFactor2SMMP_ExtendedBuffer1$";
        public const string MM_EXTENDED_FILE_NAME2 = "$rFactor2SMMP_ExtendedBuffer2$";
        public const string MM_EXTENDED_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMP_ExtendedMutex";

        public const int MAX_MAPPED_VEHICLES = 128;
        public const int MAX_MAPPED_IDS = 512;
        public const string RFACTOR2_PROCESS_NAME = "rFactor2";

        public const byte RowX = 0;
        public const byte RowY = 1;
        public const byte RowZ = 2;

        // 0 Before session has begun
        // 1 Reconnaissance laps (race only)
        // 2 Grid walk-through (race only)
        // 3 Formation lap (race only)
        // 4 Starting-light countdown has begun (race only)
        // 5 Green flag
        // 6 Full course yellow / safety car
        // 7 Session stopped
        // 8 Session over
        public enum rF2GamePhase
        {
            Garage = 0,
            WarmUp = 1,
            GridWalk = 2,
            Formation = 3,
            Countdown = 4,
            GreenFlag = 5,
            FullCourseYellow = 6,
            SessionStopped = 7,
            SessionOver = 8
        }

        // Yellow flag states (applies to full-course only)
        // -1 Invalid
        //  0 None
        //  1 Pending
        //  2 Pits closed
        //  3 Pit lead lap
        //  4 Pits open
        //  5 Last lap
        //  6 Resume
        //  7 Race halt (not currently used)
        public enum rF2YellowFlagState
        {
            Invalid = -1,
            NoFlag = 0,
            Pending = 1,
            PitClosed = 2,
            PitLeadLap = 3,
            PitOpen = 4,
            LastLap = 5,
            Resume = 6,
            RaceHalt = 7
        }

        // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
        public enum rF2SurfaceType
        {
            Dry = 0,
            Wet = 1,
            Grass = 2,
            Dirt = 3,
            Gravel = 4,
            Kerb = 5,
            Special = 6
        }

        // 0=sector3, 1=sector1, 2=sector2 (don't ask why)
        public enum rF2Sector
        {
            Sector3 = 0,
            Sector1 = 1,
            Sector2 = 2
        }

        // 0=none, 1=finished, 2=dnf, 3=dq
        public enum rF2FinishStatus
        {
            None = 0,
            Finished = 1,
            Dnf = 2,
            Dq = 3
        }

        // who's in control: -1=nobody (shouldn't get this), 0=local player, 1=local AI, 2=remote, 3=replay (shouldn't get this)
        public enum rF2Control
        {
            Nobody = -1,
            Player = 0,
            AI = 1,
            Remote = 2,
            Replay = 3
        }

        // wheel info (front left, front right, rear left, rear right)
        public enum rF2WheelIndex
        {
            FrontLeft = 0,
            FrontRight = 1,
            RearLeft = 2,
            RearRight = 3
        }

        // 0=none, 1=request, 2=entering, 3=stopped, 4=exiting
        public enum rF2PitState
        {
            None = 0,
            Request = 1,
            Entering = 2,
            Stopped = 3,
            Exiting = 4
        }

        // primary flag being shown to vehicle (currently only 0=green or 6=blue)
        public enum rF2PrimaryFlag
        {
            Green = 0,
            Blue = 6
        }

        // 0 = do not count lap or time, 1 = count lap but not time, 2 = count lap and time
        public enum rF2CountLapFlag
        {
            DoNotCountLap = 0,
            CountLapButNotTime = 1,
            CountLapAndTime = 2,
        }

        // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
        public enum rF2RearFlapLegalStatus
        {
            Disallowed = 0,
            DetectedButNotAllowedYet = 1,
            Alllowed = 2
        }

        // 0=off 1=ignition 2=ignition+starter
        public enum rF2IgnitionStarterStatus
        {
            Off = 0,
            Ignition = 1,
            IgnitionAndStarter = 2
        }
    }

    namespace rFactor2Data
    {
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2Vec3
        {
            public double x, y, z;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2Wheel
        {
            [XmlIgnore]
            public double mSuspensionDeflection;  // meters
            [XmlIgnore]
            public double mRideHeight;            // meters
            [XmlIgnore]
            public double mSuspForce;             // pushrod load in Newtons
            public double mBrakeTemp;             // Celsius
            [XmlIgnore]
            public double mBrakePressure;         // currently 0.0-1.0, depending on driver input and brake balance; will convert to true brake pressure (kPa) in future

            public double mRotation;              // radians/sec
            [XmlIgnore]
            public double mLateralPatchVel;       // lateral velocity at contact patch
            [XmlIgnore]
            public double mLongitudinalPatchVel;  // longitudinal velocity at contact patch
            [XmlIgnore]
            public double mLateralGroundVel;      // lateral velocity at contact patch
            [XmlIgnore]
            public double mLongitudinalGroundVel; // longitudinal velocity at contact patch
            [XmlIgnore]
            public double mCamber;                // radians (positive is left for left-side wheels, right for right-side wheels)
            [XmlIgnore]
            public double mLateralForce;          // Newtons
            [XmlIgnore]
            public double mLongitudinalForce;     // Newtons
            [XmlIgnore]
            public double mTireLoad;              // Newtons

            [XmlIgnore]
            public double mGripFract;             // an approximation of what fraction of the contact patch is sliding
            public double mPressure;              // kPa (tire pressure)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] mTemperature;         // Kelvin (subtract 273.15 to get Celsius), left/center/right (not to be confused with inside/center/outside!)
            public double mWear;                  // wear (0.0-1.0, fraction of maximum) ... this is not necessarily proportional with grip loss

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] mTerrainName;           // the material prefixes from the TDF file
            public byte mSurfaceType;             // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
            public byte mFlat;                    // whether tire is flat
            public byte mDetached;                // whether wheel is detached

            [XmlIgnore]
            public double mVerticalTireDeflection;// how much is tire deflected from its (speed-sensitive) radius
            [XmlIgnore]
            public double mWheelYLocation;        // wheel's y location relative to vehicle y location
            [XmlIgnore]
            public double mToe;                   // current toe angle w.r.t. the vehicle

            [XmlIgnore]
            public double mTireCarcassTemperature;       // rough average of temperature samples from carcass (Kelvin)

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] mTireInnerLayerTemperature;  // rough average of temperature samples from innermost layer of rubber (before carcass) (Kelvin)

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 24)]
            byte[] mExpansion;                    // for future use
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2VehicleTelemetry
        {
            // Time
            public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)
            public double mDeltaTime;             // time since last update (seconds)
            public double mElapsedTime;           // game session time
            public int mLapNumber;               // current lap number
            public double mLapStartET;            // time this lap was started

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mVehicleName;         // current vehicle name

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mTrackName;           // current track name

            // Position and derivatives
            public rF2Vec3 mPos;                  // world position in meters
            public rF2Vec3 mLocalVel;             // velocity (meters/sec) in local vehicle coordinates
            public rF2Vec3 mLocalAccel;           // acceleration (meters/sec^2) in local vehicle coordinates

            // Orientation and derivatives

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public rF2Vec3[] mOri;               // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
                                                 // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively
            [XmlIgnore]
            public rF2Vec3 mLocalRot;             // rotation (radians/sec) in local vehicle coordinates

            [XmlIgnore]
            public rF2Vec3 mLocalRotAccel;        // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // Vehicle status
            public int mGear;                    // -1=reverse, 0=neutral, 1+=forward gears
            public double mEngineRPM;             // engine RPM
            public double mEngineWaterTemp;       // Celsius
            public double mEngineOilTemp;         // Celsius
            public double mClutchRPM;             // clutch RPM

            // Driver input
            public double mUnfilteredThrottle;    // ranges  0.0-1.0
            public double mUnfilteredBrake;       // ranges  0.0-1.0
            public double mUnfilteredSteering;    // ranges -1.0-1.0 (left to right)
            public double mUnfilteredClutch;      // ranges  0.0-1.0

            // Filtered input (various adjustments for rev or speed limiting, TC, ABS?, speed sensitive steering, clutch work for semi-automatic shifting, etc.)
            public double mFilteredThrottle;      // ranges  0.0-1.0
            public double mFilteredBrake;         // ranges  0.0-1.0
            public double mFilteredSteering;      // ranges -1.0-1.0 (left to right)
            public double mFilteredClutch;        // ranges  0.0-1.0

            // Misc
            public double mSteeringShaftTorque;   // torque around steering shaft (used to be mSteeringArmForce, but that is not necessarily accurate for feedback purposes)
            public double mFront3rdDeflection;    // deflection at front 3rd spring
            public double mRear3rdDeflection;     // deflection at rear 3rd spring

            // Aerodynamics
            public double mFrontWingHeight;       // front wing height
            public double mFrontRideHeight;       // front ride height
            public double mRearRideHeight;        // rear ride height
            public double mDrag;                  // drag
            public double mFrontDownforce;        // front downforce
            public double mRearDownforce;         // rear downforce

            // State/damage info
            public double mFuel;                  // amount of fuel (liters)
            public double mEngineMaxRPM;          // rev limit
            public byte mScheduledStops; // number of scheduled pitstops
            public byte mOverheating;            // whether overheating icon is shown
            public byte mDetached;               // whether any parts (besides wheels) have been detached
            public byte mHeadlights;             // whether headlights are on

            [XmlIgnore] // mDentSeverity is always zero as of 7/3/2017.
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] mDentSeverity;// dent severity at 8 locations around the car (0=none, 1=some, 2=more)
            public double mLastImpactET;          // time of last impact
            public double mLastImpactMagnitude;   // magnitude of last impact
            public rF2Vec3 mLastImpactPos;        // location of last impact

            // Expanded
            public double mEngineTorque;          // current engine torque (including additive torque) (used to be mEngineTq, but there's little reason to abbreviate it)
            public int mCurrentSector;           // the current sector (zero-based) with the pitlane stored in the sign bit (example: entering pits from third sector gives 0x80000002)
            public byte mSpeedLimiter;   // whether speed limiter is on
            public byte mMaxGears;       // maximum forward gears
            public byte mFrontTireCompoundIndex;   // index within brand
            public byte mRearTireCompoundIndex;    // index within brand
            public double mFuelCapacity;          // capacity in liters
            public byte mFrontFlapActivated;       // whether front flap is activated
            public byte mRearFlapActivated;        // whether rear flap is activated
            public byte mRearFlapLegalStatus;      // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
            public byte mIgnitionStarter;          // 0=off 1=ignition 2=ignition+starter

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 18)]
            public byte[] mFrontTireCompoundName;         // name of front tire compound

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 18)]
            public byte[] mRearTireCompoundName;          // name of rear tire compound

            public byte mSpeedLimiterAvailable;    // whether speed limiter is available
            public byte mAntiStallActivated;       // whether (hard) anti-stall is activated

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2)]
            public byte[] mUnused;                //

            public float mVisualSteeringWheelRange;         // the *visual* steering wheel range

            public double mRearBrakeBias;                   // fraction of brakes on rear
            public double mTurboBoostPressure;              // current turbo boost pressure if available

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public float[] mPhysicsToGraphicsOffset;       // offset from static CG to graphical center

            public float mPhysicalSteeringWheelRange;       // the *physical* steering wheel range

            // Future use
            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 152)]
            public byte[] mExpansion;           // for future use (note that the slot ID has been moved to mID above)

            // keeping this at the end of the structure to make it easier to replace in future versions
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
            public rF2Wheel[] mWheels;                      // wheel info (front left, front right, rear left, rear right)
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2ScoringInfo
        {
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mTrackName;           // current track name
            public int mSession;                 // current session (0=testday 1-4=practice 5-8=qual 9=warmup 10-13=race)
            public double mCurrentET;             // current time
            public double mEndET;                 // ending time
            public int mMaxLaps;                // maximum laps
            public double mLapDist;               // distance around track
            // MM_NOT_USED
            //char *mResultsStream;          // results stream additions since last update (newline-delimited and NULL-terminated)
            // MM_NEW
            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] pointer1;

            public int mNumVehicles;             // current number of vehicles

            // Game phases:
            // 0 Before session has begun
            // 1 Reconnaissance laps (race only)
            // 2 Grid walk-through (race only)
            // 3 Formation lap (race only)
            // 4 Starting-light countdown has begun (race only)
            // 5 Green flag
            // 6 Full course yellow / safety car
            // 7 Session stopped
            // 8 Session over
            public byte mGamePhase;

            // Yellow flag states (applies to full-course only)
            // -1 Invalid
            //  0 None
            //  1 Pending
            //  2 Pits closed
            //  3 Pit lead lap
            //  4 Pits open
            //  5 Last lap
            //  6 Resume
            //  7 Race halt (not currently used)
            public sbyte mYellowFlagState;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public sbyte[] mSectorFlag;      // whether there are any local yellows at the moment in each sector (not sure if sector 0 is first or last, so test)
            public byte mStartLight;       // start light frame (number depends on track)
            public byte mNumRedLights;     // number of red lights in start sequence
            public byte mInRealtime;                // in realtime as opposed to at the monitor

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mPlayerName;            // player name (including possible multiplayer override)

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mPlrFileName;           // may be encoded to be a legal filename

            // weather
            public double mDarkCloud;               // cloud darkness? 0.0-1.0
            public double mRaining;                 // raining severity 0.0-1.0
            public double mAmbientTemp;             // temperature (Celsius)
            public double mTrackTemp;               // temperature (Celsius)
            public rF2Vec3 mWind;                // wind speed
            public double mMinPathWetness;          // minimum wetness on main path 0.0-1.0
            public double mMaxPathWetness;          // maximum wetness on main path 0.0-1.0

            // Future use
            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 256)]
            public byte[] mExpansion;

            // MM_NOT_USED
            // keeping this at the end of the structure to make it easier to replace in future versions
            // VehicleScoringInfoV01 *mVehicle; // array of vehicle scoring info's
            // MM_NEW
            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] pointer2;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2VehicleScoring
        {
            public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mDriverName;          // driver name

            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            public byte[] mVehicleName;         // vehicle name

            public short mTotalLaps;              // laps completed
            public sbyte mSector;           // 0=sector3, 1=sector1, 2=sector2 (don't ask why)
            public sbyte mFinishStatus;     // 0=none, 1=finished, 2=dnf, 3=dq
            public double mLapDist;               // current distance around track
            public double mPathLateral;           // lateral position with respect to *very approximate* "center" path
            public double mTrackEdge;             // track edge (w.r.t. "center" path) on same side of track as vehicle

            public double mBestSector1;           // best sector 1
            public double mBestSector2;           // best sector 2 (plus sector 1)
            public double mBestLapTime;           // best lap time
            public double mLastSector1;           // last sector 1
            public double mLastSector2;           // last sector 2 (plus sector 1)
            public double mLastLapTime;           // last lap time
            public double mCurSector1;            // current sector 1 if valid
            public double mCurSector2;            // current sector 2 (plus sector 1) if valid
                                                  // no current laptime because it instantly becomes "last"

            public short mNumPitstops;            // number of pitstops made
            public short mNumPenalties;           // number of outstanding penalties
            public byte mIsPlayer;                // is this the player's vehicle

            public sbyte mControl;          // who's in control: -1=nobody (shouldn't get this), 0=local player, 1=local AI, 2=remote, 3=replay (shouldn't get this)
            public byte mInPits;                  // between pit entrance and pit exit (not always accurate for remote vehicles)
            public byte mPlace;          // 1-based position
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mVehicleClass;        // vehicle class

            // Dash Indicators
            public double mTimeBehindNext;        // time behind vehicle in next higher place
            public int mLapsBehindNext;           // laps behind vehicle in next higher place
            public double mTimeBehindLeader;      // time behind leader
            public int mLapsBehindLeader;         // laps behind leader
            public double mLapStartET;            // time this lap was started

            // Position and derivatives
            public rF2Vec3 mPos;                  // world position in meters
            public rF2Vec3 mLocalVel;             // velocity (meters/sec) in local vehicle coordinates
            public rF2Vec3 mLocalAccel;           // acceleration (meters/sec^2) in local vehicle coordinates

            // Orientation and derivatives
            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public rF2Vec3[] mOri;               // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
                                                 // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively

            [XmlIgnore]
            public rF2Vec3 mLocalRot;             // rotation (radians/sec) in local vehicle coordinates

            [XmlIgnore]
            public rF2Vec3 mLocalRotAccel;        // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // tag.2012.03.01 - stopped casting some of these so variables now have names and mExpansion has shrunk, overall size and old data locations should be same
            public byte mHeadlights;     // status of headlights
            public byte mPitState;       // 0=none, 1=request, 2=entering, 3=stopped, 4=exiting
            public byte mServerScored;   // whether this vehicle is being scored by server (could be off in qualifying or racing heats)
            public byte mIndividualPhase;// game phases (described below) plus 9=after formation, 10=under yellow, 11=under blue (not used)

            public int mQualification;           // 1-based, can be -1 when invalid

            public double mTimeIntoLap;           // estimated time into lap
            public double mEstimatedLapTime;      // estimated laptime used for 'time behind' and 'time into lap' (note: this may changed based on vehicle and setup!?)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 24)]
            public byte[] mPitGroup;            // pit group (same as team name unless pit is shared)
            public byte mFlag;           // primary flag being shown to vehicle (currently only 0=green or 6=blue)
            public byte mUnderYellow;             // whether this car has taken a full-course caution flag at the start/finish line
            public byte mCountLapFlag;   // 0 = do not count lap or time, 1 = count lap but not time, 2 = count lap and time
            public byte mInGarageStall;           // appears to be within the correct garage stall

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            public byte[] mUpgradePack;  // Coded upgrades

            // Future use
            // tag.2012.04.06 - SEE ABOVE!
            [XmlIgnore]
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 60)]
            public byte[] mExpansion;  // for future use
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2PhysicsOptions
        {
            public byte mTractionControl;  // 0 (off) - 3 (high)
            public byte mAntiLockBrakes;   // 0 (off) - 2 (high)
            public byte mStabilityControl; // 0 (off) - 2 (high)
            public byte mAutoShift;        // 0 (off), 1 (upshifts), 2 (downshifts), 3 (all)
            public byte mAutoClutch;       // 0 (off), 1 (on)
            public byte mInvulnerable;     // 0 (off), 1 (on)
            public byte mOppositeLock;     // 0 (off), 1 (on)
            public byte mSteeringHelp;     // 0 (off) - 3 (high)
            public byte mBrakingHelp;      // 0 (off) - 2 (high)
            public byte mSpinRecovery;     // 0 (off), 1 (on)
            public byte mAutoPit;          // 0 (off), 1 (on)
            public byte mAutoLift;         // 0 (off), 1 (on)
            public byte mAutoBlip;         // 0 (off), 1 (on)

            public byte mFuelMult;         // fuel multiplier (0x-7x)
            public byte mTireMult;         // tire wear multiplier (0x-7x)
            public byte mMechFail;         // mechanical failure setting; 0 (off), 1 (normal), 2 (timescaled)
            public byte mAllowPitcrewPush; // 0 (off), 1 (on)
            public byte mRepeatShifts;     // accidental repeat shift prevention (0-5; see PLR file)
            public byte mHoldClutch;       // for auto-shifters at start of race: 0 (off), 1 (on)
            public byte mAutoReverse;      // 0 (off), 1 (on)
            public byte mAlternateNeutral; // Whether shifting up and down simultaneously equals neutral

            // tag.2014.06.09 - yes these are new, but no they don't change the size of the structure nor the address of the other variables in it (because we're just using the existing padding)
            public byte mAIControl;        // Whether player vehicle is currently under AI control
            public byte mUnused1;          //
            public byte mUnused2;          //

            public float mManualShiftOverrideTime;  // time before auto-shifting can resume after recent manual shift
            public float mAutoShiftOverrideTime;    // time before manual shifting can resume after recent auto shift
            public float mSpeedSensitiveSteering;   // 0.0 (off) - 1.0
            public float mSteerRatioSpeed;          // speed (m/s) under which lock gets expanded to full
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2MappedBufferHeader
        {
            public byte mCurrentRead;                 // True indicates buffer is safe to read under mutex.
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2MappedBufferHeaderWithSize
        {
            public byte mCurrentRead;                 // True indicates buffer is safe to read under mutex.
            public int mBytesUpdatedHint;             // How many bytes of the structure were written during the last update.
                                                      // 0 means unknown (whole buffer should be considered as updated).
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2Telemetry
        {
            public byte mCurrentRead;                 // True indicates buffer is safe to read under mutex.
            public int mBytesUpdatedHint;             // How many bytes of the structure were written during the last update.
                                                      // 0 means unknown (whole buffer should be considered as updated).

            public int mNumVehicles;                  // current number of vehicles
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_VEHICLES)]
            public rF2VehicleTelemetry[] mVehicles;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2Scoring
        {
            public byte mCurrentRead;                 // True indicates buffer is safe to read under mutex.
            public int mBytesUpdatedHint;             // How many bytes of the structure were written during the last update.
                                                      // 0 means unknown (whole buffer should be considered as updated).

            public rF2ScoringInfo mScoringInfo;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_VEHICLES)]
            public rF2VehicleScoring[] mVehicles;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2TrackedDamage
        {
            public double mMaxImpactMagnitude;                 // Max impact magnitude.  Tracked on every telemetry update, and reset on visit to pits or Session restart.
            public double mAccumulatedImpactMagnitude;         // Accumulated impact magnitude.  Tracked on every telemetry update, and reset on visit to pits or Session restart.
        };


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2VehScoringCapture
        {
            // VehicleScoringInfoV01 members:
            public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)
            public byte mPlace;
            public byte mIsPlayer;
            public sbyte mFinishStatus;     // 0=none, 1=finished, 2=dnf, 3=dq
        };


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2SessionTransitionCapture
        {
            // ScoringInfoV01 members:
            public byte mGamePhase;
            public int mSession;

            // VehicleScoringInfoV01 members:
            public int mNumScoringVehicles;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_VEHICLES)]
            public rF2VehScoringCapture[] mScoringVehicles;
        };


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2Extended
        {
            public byte mCurrentRead;                          // True indicates buffer is safe to read under mutex.

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            public byte[] mVersion;                            // API version
            public byte is64bit;                               // Is 64bit plugin?

            // Physics options (updated on session start):
            public rF2PhysicsOptions mPhysics;

            // Damage tracking for each vehicle (indexed by mID % rF2MappedBufferHeader::MAX_MAPPED_IDS):
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_IDS)]
            public rF2TrackedDamage[] mTrackedDamages;

            // Function call based flags:
            public byte mInRealtimeFC;                         // in realtime as opposed to at the monitor (reported via last EnterRealtime/ExitRealtime calls).
            public byte mMultimediaThreadStarted;              // multimedia thread started (reported via ThreadStarted/ThreadStopped calls).
            public byte mSimulationThreadStarted;              // simulation thread started (reported via ThreadStarted/ThreadStopped calls).

            public byte mSessionStarted;                       // Set to true on Session Started, set to false on Session Ended.
            public Int64 mTicksSessionStarted;                 // Ticks when session started.
            public Int64 mTicksSessionEnded;                   // Ticks when session ended.
            public rF2SessionTransitionCapture mSessionTransitionCapture;  // Contains partial internals capture at session transition time.
        }


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2BufferHeader
        {
            internal byte mCurrentRead;                        // True indicates buffer is safe to read under mutex.
        }
    }
}