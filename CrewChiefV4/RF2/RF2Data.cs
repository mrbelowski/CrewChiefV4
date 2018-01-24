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
    // ULONGLONG     ->    Int64
    public class rFactor2Constants
    {
        public const string MM_TELEMETRY_FILE_NAME1 = "$rFactor2SMMP_TelemetryBuffer1$";
        public const string MM_TELEMETRY_FILE_NAME2 = "$rFactor2SMMP_TelemetryBuffer2$";
        public const string MM_TELEMETRY_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMP_TelemeteryMutex";

        public const string MM_SCORING_FILE_NAME1 = "$rFactor2SMMP_ScoringBuffer1$";
        public const string MM_SCORING_FILE_NAME2 = "$rFactor2SMMP_ScoringBuffer2$";
        public const string MM_SCORING_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMP_ScoringMutex";

        public const string MM_RULES_FILE_NAME1 = "$rFactor2SMMP_RulesBuffer1$";
        public const string MM_RULES_FILE_NAME2 = "$rFactor2SMMP_RulesBuffer2$";
        public const string MM_RULES_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMP_RulesMutex";

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
            SessionOver = 8,
            Undocumented_PreRace = 9  // I suspect 9 means we're in a garage/monitor, waiting for race to start.
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
            [XmlIgnore] public double mSuspensionDeflection;  // meters
            [XmlIgnore] public double mRideHeight;            // meters
            [XmlIgnore] public double mSuspForce;             // pushrod load in Newtons
            public double mBrakeTemp;             // Celsius
            [XmlIgnore] public double mBrakePressure;         // currently 0.0-1.0, depending on driver input and brake balance; will convert to true brake pressure (kPa) in future
            public double mRotation;              // radians/sec
            [XmlIgnore] public double mLateralPatchVel;       // lateral velocity at contact patch
            [XmlIgnore] public double mLongitudinalPatchVel;  // longitudinal velocity at contact patch
            [XmlIgnore] public double mLateralGroundVel;      // lateral velocity at contact patch
            [XmlIgnore] public double mLongitudinalGroundVel; // longitudinal velocity at contact patch
            [XmlIgnore] public double mCamber;                // radians (positive is left for left-side wheels, right for right-side wheels)
            [XmlIgnore] public double mLateralForce;          // Newtons
            [XmlIgnore] public double mLongitudinalForce;     // Newtons
            [XmlIgnore] public double mTireLoad;              // Newtons

            [XmlIgnore] public double mGripFract;             // an approximation of what fraction of the contact patch is sliding
            public double mPressure;              // kPa (tire pressure)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public double[] mTemperature;         // Kelvin (subtract 273.15 to get Celsius), left/center/right (not to be confused with inside/center/outside!)
            public double mWear;                  // wear (0.0-1.0, fraction of maximum) ... this is not necessarily proportional with grip loss

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            [XmlIgnore] public byte[] mTerrainName;           // the material prefixes from the TDF file
            public byte mSurfaceType;             // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
            public byte mFlat;                    // whether tire is flat
            public byte mDetached;                // whether wheel is detached

            [XmlIgnore] public double mVerticalTireDeflection;// how much is tire deflected from its (speed-sensitive) radius
            [XmlIgnore] public double mWheelYLocation;        // wheel's y location relative to vehicle y location
            [XmlIgnore] public double mToe;                   // current toe angle w.r.t. the vehicle

            [XmlIgnore] public double mTireCarcassTemperature;       // rough average of temperature samples from carcass (Kelvin)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            [XmlIgnore] public double[] mTireInnerLayerTemperature;  // rough average of temperature samples from innermost layer of rubber (before carcass) (Kelvin)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 24)]
            [XmlIgnore] byte[] mExpansion;                    // for future use
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2VehicleTelemetry
        {
            // Time
            public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)
            [XmlIgnore] public double mDeltaTime;             // time since last update (seconds)
            public double mElapsedTime;           // game session time
            [XmlIgnore] public int mLapNumber;               // current lap number
            [XmlIgnore] public double mLapStartET;            // time this lap was started

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            [XmlIgnore] public byte[] mVehicleName;         // current vehicle name

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            [XmlIgnore] public byte[] mTrackName;           // current track name

            // Position and derivatives
            public rF2Vec3 mPos;                  // world position in meters
            public rF2Vec3 mLocalVel;             // velocity (meters/sec) in local vehicle coordinates
            [XmlIgnore] public rF2Vec3 mLocalAccel;           // acceleration (meters/sec^2) in local vehicle coordinates

            // Orientation and derivatives
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            public rF2Vec3[] mOri;               // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
                                                             // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively

            [XmlIgnore] public rF2Vec3 mLocalRot;             // rotation (radians/sec) in local vehicle coordinates

            [XmlIgnore] public rF2Vec3 mLocalRotAccel;        // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // Vehicle status
            public int mGear;                    // -1=reverse, 0=neutral, 1+=forward gears
            public double mEngineRPM;             // engine RPM
            public double mEngineWaterTemp;       // Celsius
            public double mEngineOilTemp;         // Celsius
            [XmlIgnore] public double mClutchRPM;             // clutch RPM

            // Driver input
            public double mUnfilteredThrottle;    // ranges  0.0-1.0
            public double mUnfilteredBrake;       // ranges  0.0-1.0
            [XmlIgnore] public double mUnfilteredSteering;    // ranges -1.0-1.0 (left to right)
            public double mUnfilteredClutch;      // ranges  0.0-1.0

            // Filtered input (various adjustments for rev or speed limiting, TC, ABS?, speed sensitive steering, clutch work for semi-automatic shifting, etc.)
            [XmlIgnore] public double mFilteredThrottle;      // ranges  0.0-1.0
            [XmlIgnore] public double mFilteredBrake;         // ranges  0.0-1.0
            [XmlIgnore] public double mFilteredSteering;      // ranges -1.0-1.0 (left to right)
            [XmlIgnore] public double mFilteredClutch;        // ranges  0.0-1.0

            // Misc
            [XmlIgnore] public double mSteeringShaftTorque;   // torque around steering shaft (used to be mSteeringArmForce, but that is not necessarily accurate for feedback purposes)
            [XmlIgnore] public double mFront3rdDeflection;    // deflection at front 3rd spring
            [XmlIgnore] public double mRear3rdDeflection;     // deflection at rear 3rd spring

            // Aerodynamics
            [XmlIgnore] public double mFrontWingHeight;       // front wing height
            [XmlIgnore] public double mFrontRideHeight;       // front ride height
            [XmlIgnore] public double mRearRideHeight;        // rear ride height
            [XmlIgnore] public double mDrag;                  // drag
            [XmlIgnore] public double mFrontDownforce;        // front downforce
            [XmlIgnore] public double mRearDownforce;         // rear downforce

            // State/damage info
            public double mFuel;                  // amount of fuel (liters)
            public double mEngineMaxRPM;          // rev limit
            public byte mScheduledStops; // number of scheduled pitstops
            public byte mOverheating;            // whether overheating icon is shown
            public byte mDetached;               // whether any parts (besides wheels) have been detached
            [XmlIgnore] public byte mHeadlights;             // whether headlights are on

            // mDentSeverity is always zero as of 7/3/2017.
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            [XmlIgnore] public byte[] mDentSeverity;// dent severity at 8 locations around the car (0=none, 1=some, 2=more)
            public double mLastImpactET;          // time of last impact
            [XmlIgnore] public double mLastImpactMagnitude;   // magnitude of last impact
            [XmlIgnore] public rF2Vec3 mLastImpactPos;        // location of last impact

            // Expanded
            [XmlIgnore] public double mEngineTorque;          // current engine torque (including additive torque) (used to be mEngineTq, but there's little reason to abbreviate it)
            [XmlIgnore] public int mCurrentSector;           // the current sector (zero-based) with the pitlane stored in the sign bit (example: entering pits from third sector gives 0x80000002)
            public byte mSpeedLimiter;   // whether speed limiter is on
            [XmlIgnore] public byte mMaxGears;       // maximum forward gears
            public byte mFrontTireCompoundIndex;   // index within brand
            [XmlIgnore] public byte mRearTireCompoundIndex;    // index within brand
            [XmlIgnore] public double mFuelCapacity;          // capacity in liters
            [XmlIgnore] public byte mFrontFlapActivated;       // whether front flap is activated
            public byte mRearFlapActivated;        // whether rear flap is activated
            public byte mRearFlapLegalStatus;      // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
            [XmlIgnore] public byte mIgnitionStarter;          // 0=off 1=ignition 2=ignition+starter

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 18)]
            public byte[] mFrontTireCompoundName;         // name of front tire compound

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 18)]
            [XmlIgnore] public byte[] mRearTireCompoundName;          // name of rear tire compound

            public byte mSpeedLimiterAvailable;    // whether speed limiter is available
            [XmlIgnore] public byte mAntiStallActivated;       // whether (hard) anti-stall is activated

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 2)]
            [XmlIgnore] public byte[] mUnused;                //

            [XmlIgnore] public float mVisualSteeringWheelRange;         // the *visual* steering wheel range

            [XmlIgnore] public double mRearBrakeBias;                   // fraction of brakes on rear
            [XmlIgnore] public double mTurboBoostPressure;              // current turbo boost pressure if available

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            [XmlIgnore] public float[] mPhysicsToGraphicsOffset;       // offset from static CG to graphical center

            [XmlIgnore] public float mPhysicalSteeringWheelRange;       // the *physical* steering wheel range

            // Future use
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 152)]
            [XmlIgnore] public byte[] mExpansion;           // for future use (note that the slot ID has been moved to mID above)

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
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            [XmlIgnore] public byte[] pointer1;

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
            [XmlIgnore] public byte mStartLight;       // start light frame (number depends on track)
            [XmlIgnore] public byte mNumRedLights;     // number of red lights in start sequence
            public byte mInRealtime;                // in realtime as opposed to at the monitor

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            [XmlIgnore] public byte[] mPlayerName;            // player name (including possible multiplayer override)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            [XmlIgnore] public byte[] mPlrFileName;           // may be encoded to be a legal filename

            // weather
            [XmlIgnore] public double mDarkCloud;               // cloud darkness? 0.0-1.0
            public double mRaining;                 // raining severity 0.0-1.0
            public double mAmbientTemp;             // temperature (Celsius)
            public double mTrackTemp;               // temperature (Celsius)
            public rF2Vec3 mWind;                // wind speed
            [XmlIgnore] public double mMinPathWetness;          // minimum wetness on main path 0.0-1.0
            [XmlIgnore] public double mMaxPathWetness;          // maximum wetness on main path 0.0-1.0

            // Future use
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 256)]
            [XmlIgnore] public byte[] mExpansion;

            // MM_NOT_USED
            // keeping this at the end of the structure to make it easier to replace in future versions
            // VehicleScoringInfoV01 *mVehicle; // array of vehicle scoring info's
            // MM_NEW
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            [XmlIgnore] public byte[] pointer2;
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2VehicleScoring
        {
            public int mID;                      // slot ID (note that it can be re-used in multiplayer after someone leaves)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            public byte[] mDriverName;          // driver name

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            [XmlIgnore] public byte[] mVehicleName;         // vehicle name

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
            [XmlIgnore] public int mLapsBehindNext;           // laps behind vehicle in next higher place
            [XmlIgnore] public double mTimeBehindLeader;      // time behind leader
            [XmlIgnore] public int mLapsBehindLeader;         // laps behind leader
            public double mLapStartET;            // time this lap was started

            // Position and derivatives
            // TODO: remove these from serialization, no telemetry case is corner case.
            public rF2Vec3 mPos;                  // world position in meters
            public rF2Vec3 mLocalVel;             // velocity (meters/sec) in local vehicle coordinates
            public rF2Vec3 mLocalAccel;           // acceleration (meters/sec^2) in local vehicle coordinates

            // Orientation and derivatives
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            [XmlIgnore] public rF2Vec3[] mOri;               // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
                                                 // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively

            [XmlIgnore] public rF2Vec3 mLocalRot;             // rotation (radians/sec) in local vehicle coordinates

            [XmlIgnore] public rF2Vec3 mLocalRotAccel;        // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // tag.2012.03.01 - stopped casting some of these so variables now have names and mExpansion has shrunk, overall size and old data locations should be same
            [XmlIgnore] public byte mHeadlights;     // status of headlights
            public byte mPitState;       // 0=none, 1=request, 2=entering, 3=stopped, 4=exiting
            [XmlIgnore] public byte mServerScored;   // whether this vehicle is being scored by server (could be off in qualifying or racing heats)
            [XmlIgnore] public byte mIndividualPhase;// game phases (described below) plus 9=after formation, 10=under yellow, 11=under blue (not used)

            [XmlIgnore] public int mQualification;           // 1-based, can be -1 when invalid

            [XmlIgnore] public double mTimeIntoLap;           // estimated time into lap
            [XmlIgnore] public double mEstimatedLapTime;      // estimated laptime used for 'time behind' and 'time into lap' (note: this may changed based on vehicle and setup!?)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 24)]
            [XmlIgnore] public byte[] mPitGroup;            // pit group (same as team name unless pit is shared)
            public byte mFlag;           // primary flag being shown to vehicle (currently only 0=green or 6=blue)
            [XmlIgnore] public byte mUnderYellow;             // whether this car has taken a full-course caution flag at the start/finish line
            public byte mCountLapFlag;   // 0 = do not count lap or time, 1 = count lap but not time, 2 = count lap and time
            public byte mInGarageStall;           // appears to be within the correct garage stall

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            [XmlIgnore] public byte[] mUpgradePack;  // Coded upgrades

            // Future use
            // tag.2012.04.06 - SEE ABOVE!
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 60)]
            [XmlIgnore] public byte[] mExpansion;  // for future use
        }


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2PhysicsOptions
        {
            [XmlIgnore] public byte mTractionControl;  // 0 (off) - 3 (high)
            [XmlIgnore] public byte mAntiLockBrakes;   // 0 (off) - 2 (high)
            [XmlIgnore] public byte mStabilityControl; // 0 (off) - 2 (high)
            [XmlIgnore] public byte mAutoShift;        // 0 (off), 1 (upshifts), 2 (downshifts), 3 (all)
            [XmlIgnore] public byte mAutoClutch;       // 0 (off), 1 (on)
            public byte mInvulnerable;     // 0 (off), 1 (on)
            [XmlIgnore] public byte mOppositeLock;     // 0 (off), 1 (on)
            [XmlIgnore] public byte mSteeringHelp;     // 0 (off) - 3 (high)
            [XmlIgnore] public byte mBrakingHelp;      // 0 (off) - 2 (high)
            [XmlIgnore] public byte mSpinRecovery;     // 0 (off), 1 (on)
            [XmlIgnore] public byte mAutoPit;          // 0 (off), 1 (on)
            [XmlIgnore] public byte mAutoLift;         // 0 (off), 1 (on)
            [XmlIgnore] public byte mAutoBlip;         // 0 (off), 1 (on)

            public byte mFuelMult;         // fuel multiplier (0x-7x)
            [XmlIgnore] public byte mTireMult;         // tire wear multiplier (0x-7x)
            [XmlIgnore] public byte mMechFail;         // mechanical failure setting; 0 (off), 1 (normal), 2 (timescaled)
            [XmlIgnore] public byte mAllowPitcrewPush; // 0 (off), 1 (on)
            [XmlIgnore] public byte mRepeatShifts;     // accidental repeat shift prevention (0-5; see PLR file)
            [XmlIgnore] public byte mHoldClutch;       // for auto-shifters at start of race: 0 (off), 1 (on)
            [XmlIgnore] public byte mAutoReverse;      // 0 (off), 1 (on)
            [XmlIgnore] public byte mAlternateNeutral; // Whether shifting up and down simultaneously equals neutral

            // tag.2014.06.09 - yes these are new, but no they don't change the size of the structure nor the address of the other variables in it (because we're just using the existing padding)
            [XmlIgnore] public byte mAIControl;        // Whether player vehicle is currently under AI control
            [XmlIgnore] public byte mUnused1;          //
            [XmlIgnore] public byte mUnused2;          //

            [XmlIgnore] public float mManualShiftOverrideTime;  // time before auto-shifting can resume after recent manual shift
            [XmlIgnore] public float mAutoShiftOverrideTime;    // time before manual shifting can resume after recent auto shift
            [XmlIgnore] public float mSpeedSensitiveSteering;   // 0.0 (off) - 1.0
            [XmlIgnore] public float mSteerRatioSpeed;          // speed (m/s) under which lock gets expanded to full
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesCommandV01, except where noted by MM_NEW/MM_NOT_USED comments.  Renamed to match plugin convention.
        //////////////////////////////////////////////////////////////////////////////////////////
        public enum rF2TrackRulesCommand
        {
            AddFromTrack = 0,             // crossed s/f line for first time after full-course yellow was called
            AddFromPit,                   // exited pit during full-course yellow
            AddFromUndq,                  // during a full-course yellow, the admin reversed a disqualification
            RemoveToPit,                  // entered pit during full-course yellow
            RemoveToDnf,                  // vehicle DNF'd during full-course yellow
            RemoveToDq,                   // vehicle DQ'd during full-course yellow
            RemoveToUnloaded,             // vehicle unloaded (possibly kicked out or banned) during full-course yellow
            MoveToBack,                   // misbehavior during full-course yellow, resulting in the penalty of being moved to the back of their current line
            LongestTime,                  // misbehavior during full-course yellow, resulting in the penalty of being moved to the back of the longest line
                                          //------------------
            Maximum                       // should be last
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesActionV01, except where noted by MM_NEW/MM_NOT_USED comments.
        //////////////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2TrackRulesAction
        {
            // input only
            [XmlIgnore] public rF2TrackRulesCommand mCommand;        // recommended action
            [XmlIgnore] public int mID;                              // slot ID if applicable
            [XmlIgnore] public double mET;                           // elapsed time that event occurred, if applicable
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesColumnV01, except where noted by MM_NEW/MM_NOT_USED comments.  Renamed to match plugin convention.
        //////////////////////////////////////////////////////////////////////////////////////////
        public enum rF2TrackRulesColumn
        {
            LeftLane = 0,                  // left (inside)
            MidLefLane,                    // mid-left
            MiddleLane,                    // middle
            MidrRghtLane,                  // mid-right
            RightLane,                     // right (outside)
                                           //------------------
            MaxLanes,                      // should be after the valid static lane choices
                                           //------------------
            Invalid = MaxLanes,            // currently invalid (hasn't crossed line or in pits/garage)
            FreeChoice,                    // free choice (dynamically chosen by driver)
            Pending,                       // depends on another participant's free choice (dynamically set after another driver chooses)
                                           //------------------
            Maximum                        // should be last
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesParticipantV01, except where noted by MM_NEW/MM_NOT_USED comments.
        //////////////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2TrackRulesParticipant
        {
            // input only
            public int mID;                             // slot ID
            [XmlIgnore] public short mFrozenOrder;                   // 0-based place when caution came out (not valid for formation laps)
            public short mPlace;                         // 1-based place (typically used for the initialization of the formation lap track order)
            [XmlIgnore] public float mYellowSeverity;                // a rating of how much this vehicle is contributing to a yellow flag (the sum of all vehicles is compared to TrackRulesV01::mSafetyCarThreshold)
            [XmlIgnore] public double mCurrentRelativeDistance;      // equal to ( ( ScoringInfoV01::mLapDist * this->mRelativeLaps ) + VehicleScoringInfoV01::mLapDist )

            // input/output
            public int mRelativeLaps;                   // current formation/caution laps relative to safety car (should generally be zero except when safety car crosses s/f line); this can be decremented to implement 'wave around' or 'beneficiary rule' (a.k.a. 'lucky dog' or 'free pass')
            public rF2TrackRulesColumn mColumnAssignment;// which column (line/lane) that participant is supposed to be in
            public int mPositionAssignment;             // 0-based position within column (line/lane) that participant is supposed to be located at (-1 is invalid)
            public byte mAllowedToPit;                   // whether the rules allow this particular vehicle to enter pits right now

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            [XmlIgnore] public byte[] mUnused;                    //

            [XmlIgnore] public double mGoalRelativeDistance;         // calculated based on where the leader is, and adjusted by the desired column spacing and the column/position assignments

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 96)]
            public byte[] mMessage;                  // a message for this participant to explain what is going on (untranslated; it will get run through translator on client machines)

            // future expansion
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 192)]
            [XmlIgnore] public byte[] mExpansion;
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesStageV01, except where noted by MM_NEW/MM_NOT_USED comments.  Renamed to match plugin convention.
        //////////////////////////////////////////////////////////////////////////////////////////
        public enum rF2TrackRulesStage
        {
            FormationInit = 0,           // initialization of the formation lap
            FormationUpdate,             // update of the formation lap
            Normal,                      // normal (non-yellow) update
            CautionInit,                 // initialization of a full-course yellow
            CautionUpdate,               // update of a full-course yellow
                                         //------------------
            Maximum                      // should be last
        }


        //////////////////////////////////////////////////////////////////////////////////////////
        // Identical to TrackRulesV01, except where noted by MM_NEW/MM_NOT_USED comments.
        //////////////////////////////////////////////////////////////////////////////////////////
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2TrackRules
        {
            // input only
            [XmlIgnore] public double mCurrentET;                    // current time
            public rF2TrackRulesStage mStage;            // current stage
            public rF2TrackRulesColumn mPoleColumn;      // column assignment where pole position seems to be located
            [XmlIgnore] public int mNumActions;                     // number of recent actions

            // MM_NOT_USED
            // TrackRulesActionV01 *mAction;         // array of recent actions
            // MM_NEW

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            [XmlIgnore] public byte[] pointer1;

            public int mNumParticipants;                // number of participants (vehicles)

            [XmlIgnore] public byte mYellowFlagDetected;             // whether yellow flag was requested or sum of participant mYellowSeverity's exceeds mSafetyCarThreshold
            [XmlIgnore] public byte mYellowFlagLapsWasOverridden;    // whether mYellowFlagLaps (below) is an admin request
            [XmlIgnore] public byte mSafetyCarExists;                // whether safety car even exists
            public byte mSafetyCarActive;                // whether safety car is active

            public int mSafetyCarLaps;                  // number of laps
            [XmlIgnore] public float mSafetyCarThreshold;            // the threshold at which a safety car is called out (compared to the sum of TrackRulesParticipantV01::mYellowSeverity for each vehicle) 
            public double mSafetyCarLapDist;             // safety car lap distance

            [XmlIgnore] public float mSafetyCarLapDistAtStart;       // where the safety car starts from
            public float mPitLaneStartDist;              // where the waypoint branch to the pits breaks off (this may not be perfectly accurate)
            [XmlIgnore] public float mTeleportLapDist;               // the front of the teleport locations (a useful first guess as to where to throw the green flag)

            // future input expansion
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 256)]
            [XmlIgnore] public byte[] mInputExpansion;

            // input/output
            [XmlIgnore] public sbyte mYellowFlagState;         // see ScoringInfoV01 for values
            [XmlIgnore] public short mYellowFlagLaps;                // suggested number of laps to run under yellow (may be passed in with admin command)
            [XmlIgnore] public int mSafetyCarInstruction;           // 0=no change, 1=go active, 2=head for pits
            public float mSafetyCarSpeed;                // maximum speed at which to drive

            [XmlIgnore] public float mSafetyCarMinimumSpacing;       // minimum spacing behind safety car (-1 to indicate no limit)
            [XmlIgnore] public float mSafetyCarMaximumSpacing;       // maximum spacing behind safety car (-1 to indicate no limit)
            [XmlIgnore] public float mMinimumColumnSpacing;          // minimum desired spacing between vehicles in a column (-1 to indicate indeterminate/unenforced)
            [XmlIgnore] public float mMaximumColumnSpacing;          // maximum desired spacing between vehicles in a column (-1 to indicate indeterminate/unenforced)

            [XmlIgnore] public float mMinimumSpeed;                  // minimum speed that anybody should be driving (-1 to indicate no limit)
            [XmlIgnore] public float mMaximumSpeed;                  // maximum speed that anybody should be driving (-1 to indicate no limit)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 96)]
            public byte[] mMessage;                  // a message for everybody to explain what is going on (which will get run through translator on client machines)

            // MM_NOT_USED
            // TrackRulesParticipantV01 *mParticipant;         // array of partipants (vehicles)
            // MM_NEW
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            [XmlIgnore] public byte[] pointer2;

            // future input/output expansion
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 256)]
            [XmlIgnore] public byte[] mInputOutputExpansion;
        };


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


        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 4)]
        public struct rF2Rules
        {
            public byte mCurrentRead;                 // True indicates buffer is safe to read under mutex.
            public int mBytesUpdatedHint;             // How many bytes of the structure were written during the last update.
                                                      // 0 means unknown (whole buffer should be considered as updated).

            public rF2TrackRules mTrackRules;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_VEHICLES)]
            [XmlIgnore] public rF2TrackRulesAction[] mActions;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_VEHICLES)]
            public rF2TrackRulesParticipant[] mParticipants;
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
            [XmlIgnore] public sbyte mFinishStatus;     // 0=none, 1=finished, 2=dnf, 3=dq
        };


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2SessionTransitionCapture
        {
            // ScoringInfoV01 members:
            [XmlIgnore] public byte mGamePhase;
            [XmlIgnore] public int mSession;

            // VehicleScoringInfoV01 members:
            public int mNumScoringVehicles;
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_MAPPED_VEHICLES)]
            public rF2VehScoringCapture[] mScoringVehicles;
        };


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2HostedPluginVars
        {
            public byte StockCarRules_IsHosted;        // Is StockCarRules.dll successfully loaded into SM plugin?
            public int StockCarRules_DoubleFileType;   // DoubleFileType plugin variable value.
        }


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
            [XmlIgnore] public byte mMultimediaThreadStarted;              // multimedia thread started (reported via ThreadStarted/ThreadStopped calls).
            [XmlIgnore] public byte mSimulationThreadStarted;              // simulation thread started (reported via ThreadStarted/ThreadStopped calls).

            public byte mSessionStarted;                       // Set to true on Session Started, set to false on Session Ended.
            [XmlIgnore] public Int64 mTicksSessionStarted;                 // Ticks when session started.
            public Int64 mTicksSessionEnded;                   // Ticks when session ended.
            public rF2SessionTransitionCapture mSessionTransitionCapture;  // Contains partial internals capture at session transition time.

            // Captured non-empty MessageInfoV01::mText message.
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 128)]
            public byte[] mDisplayedMessageUpdateCapture;

            public rF2HostedPluginVars mHostedPluginVars;
        }


        [StructLayout(LayoutKind.Sequential, Pack = 4)]
        public struct rF2BufferHeader
        {
            internal byte mCurrentRead;                        // True indicates buffer is safe to read under mutex.
        }
    }
}
