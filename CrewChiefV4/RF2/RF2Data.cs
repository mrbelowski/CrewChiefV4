using System;
using System.Runtime.InteropServices;

namespace CrewChiefV4.rFactor2
{
    // Marshalled types:
    // C++                 C#
    // char          ->    byte
    // unsigned char ->    byte
    // signed char   ->    sbyte
    // bool          ->    byte
    // long          ->    int
    class rFactor2Constants
    {
        internal const string MM_FILE_NAME1 = "$rFactor2SMMPBuffer1$";
        internal const string MM_FILE_NAME2 = "$rFactor2SMMPBuffer2$";
        internal const string MM_FILE_ACCESS_MUTEX = @"Global\$rFactor2SMMPMutex";
        internal const int MAX_VSI_SIZE = 128;
        internal const string RFACTOR2_PROCESS_NAME = "rFactor2";

        internal const byte RowX = 0;
        internal const byte RowY = 1;
        internal const byte RowZ = 2;

        // 0 Before session has begun
        // 1 Reconnaissance laps (race only)
        // 2 Grid walk-through (race only)
        // 3 Formation lap (race only)
        // 4 Starting-light countdown has begun (race only)
        // 5 Green flag
        // 6 Full course yellow / safety car
        // 7 Session stopped
        // 8 Session over
        internal enum rF2GamePhase
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
        internal enum rF2YellowFlagState
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
        internal enum rF2SurfaceType
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
        internal enum rF2Sector
        {
            Sector3 = 0,
            Sector1 = 1,
            Sector2 = 2
        }

        // 0=none, 1=finished, 2=dnf, 3=dq
        internal enum rF2FinishStatus
        {
            None = 0,
            Finished = 1,
            Dnf = 2,
            Dq = 3
        }

        // who's in control: -1=nobody (shouldn't get this), 0=local player, 1=local AI, 2=remote, 3=replay (shouldn't get this)
        internal enum rF2Control
        {
            Nobody = -1,
            Player = 0,
            AI = 1,
            Remote = 2,
            Replay = 3
        }

        // wheel info (front left, front right, rear left, rear right)
        internal enum rF2WheelIndex
        {
            FrontLeft = 0,
            FrontRight = 1,
            RearLeft = 2,
            RearRight = 3
        }
    }

    namespace rFactor2Data
    {
        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        struct rF2Vec3
        {
            internal double x, y, z;
        }

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 16)]
        struct rF2Wheel
        {
            internal double mSuspensionDeflection;  // meters
            internal double mRideHeight;            // meters
            internal double mSuspForce;             // pushrod load in Newtons
            internal double mBrakeTemp;             // Celsius
            internal double mBrakePressure;         // currently 0.0-1.0, depending on driver input and brake balance; will convert to true brake pressure (kPa) in future

            internal double mRotation;              // radians/sec
            internal double mLateralPatchVel;       // lateral velocity at contact patch
            internal double mLongitudinalPatchVel;  // longitudinal velocity at contact patch
            internal double mLateralGroundVel;      // lateral velocity at contact patch
            internal double mLongitudinalGroundVel; // longitudinal velocity at contact patch
            internal double mCamber;                // radians (positive is left for left-side wheels, right for right-side wheels)
            internal double mLateralForce;          // Newtons
            internal double mLongitudinalForce;     // Newtons
            internal double mTireLoad;              // Newtons

            internal double mGripFract;             // an approximation of what fraction of the contact patch is sliding
            internal double mPressure;              // kPa (tire pressure)
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            internal double[] mTemperature;         // Kelvin (subtract 273.15 to get Celsius), left/center/right (not to be confused with inside/center/outside!)
            internal double mWear;                  // wear (0.0-1.0, fraction of maximum) ... this is not necessarily proportional with grip loss
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            internal byte[] mTerrainName;           // the material prefixes from the TDF file
            internal byte mSurfaceType;             // 0=dry, 1=wet, 2=grass, 3=dirt, 4=gravel, 5=rumblestrip, 6=special
            internal byte mFlat;                    // whether tire is flat
            internal byte mDetached;                // whether wheel is detached

            internal double mVerticalTireDeflection;// how much is tire deflected from its (speed-sensitive) radius
            internal double mWheelYLocation;        // wheel's y location relative to vehicle y location
            internal double mToe;                   // current toe angle w.r.t. the vehicle

            internal double mTireCarcassTemperature;       // rough average of temperature samples from carcass (Kelvin)
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            internal double[] mTireInnerLayerTemperature;  // rough average of temperature samples from innermost layer of rubber (before carcass) (Kelvin)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 24)]
            byte[] mExpansion;                    // for future use
        };

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 16)]
        struct rF2VehScoringInfo
        {
            internal int mID;                       // slot ID (note that it can be re-used in multiplayer after someone leaves)
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            internal byte[] mDriverName;            // driver name
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            internal byte[] mVehicleName;           // vehicle name
            internal short mTotalLaps;              // laps completed
            internal sbyte mSector;                 // 0=sector3, 1=sector1, 2=sector2 (don't ask why)
            internal sbyte mFinishStatus;           // 0=none, 1=finished, 2=dnf, 3=dq
            internal double mLapDist;               // current distance around track
            internal double mPathLateral;           // lateral position with respect to *very approximate* "center" path
            internal double mTrackEdge;             // track edge (w.r.t. "center" path) on same side of track as vehicle

            internal double mBestSector1;           // best sector 1
            internal double mBestSector2;           // best sector 2 (plus sector 1)
            internal double mBestLapTime;           // best lap time
            internal double mLastSector1;           // last sector 1
            internal double mLastSector2;           // last sector 2 (plus sector 1)
            internal double mLastLapTime;           // last lap time
            internal double mCurSector1;            // current sector 1 if valid
            internal double mCurSector2;            // current sector 2 (plus sector 1) if valid
            // no current laptime because it instantly becomes "last"

            internal short mNumPitstops;            // number of pitstops made
            internal short mNumPenalties;           // number of outstanding penalties
            internal byte mIsPlayer;                // is this the player's vehicle

            internal sbyte mControl;                // who's in control: -1=nobody (shouldn't get this), 0=local player, 1=local AI, 2=remote, 3=replay (shouldn't get this)
            internal byte mInPits;                  // between pit entrance and pit exit (not always accurate for remote vehicles)
            internal byte mPlace;                   // 1-based position
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            internal byte[] mVehicleClass;          // vehicle class

            // Dash Indicators
            internal double mTimeBehindNext;        // time behind vehicle in next higher place
            internal int mLapsBehindNext;           // laps behind vehicle in next higher place
            internal double mTimeBehindLeader;      // time behind leader
            internal int mLapsBehindLeader;         // laps behind leader
            internal double mLapStartET;            // time this lap was started

            // Position and derivatives
            internal rF2Vec3 mPos;                  // world position in meters
            internal double mYaw;                    // rad, use (360-yaw*57.2978)%360 for heading in degrees
            internal double mPitch;                  // rad
            internal double mRoll;                   // rad
            internal double mSpeed;                  // meters/sec

            internal byte mHeadlights;     // status of headlights
            internal byte mPitState;       // 0=none, 1=request, 2=entering, 3=stopped, 4=exiting
            internal byte mServerScored;   // whether this vehicle is being scored by server (could be off in qualifying or racing heats)
            internal byte mIndividualPhase;// game phases (described below) plus 9=after formation, 10=under yellow, 11=under blue (not used)

            internal int mQualification;            // 1-based, can be -1 when invalid

            internal double mTimeIntoLap;           // estimated time into lap
            internal double mEstimatedLapTime;      // estimated laptime used for 'time behind' and 'time into lap' (note: this may changed based on vehicle and setup!?)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 24)]
            internal byte[] mPitGroup;              // pit group (same as team name unless pit is shared)
            internal byte mFlag;                    // primary flag being shown to vehicle (currently only 0=green or 6=blue)
            internal byte mUnderYellow;             // whether this car has taken a full-course caution flag at the start/finish line
            internal byte mCountLapFlag;            // 0 = do not count lap or time, 1 = count lap but not time, 2 = count lap and time
            internal byte mInGarageStall;           // appears to be within the correct garage stall

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 16)]
            byte[] mUpgradePack;                    // Coded upgrades

#if DEBUG_INTERPOLATION
      internal rF2Vec3 mPosScoring;
      internal rF2Vec3 mLocalVel;             // velocity (meters/sec) in local vehicle coordinates
      internal rF2Vec3 mLocalAccel;           // acceleration (meters/sec^2) in local vehicle coordinates

      // Orientation and derivatives
      [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
      internal rF2Vec3[] mOri;               // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
                                             // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively

      internal rF2Vec3 mLocalRot;             // rotation (radians/sec) in local vehicle coordinates
      internal rF2Vec3 mLocalRotAccel;        // rotational acceleration (radians/sec^2) in local vehicle coordinates
#endif
        }

        [StructLayout(LayoutKind.Sequential, Pack = 16)]
        struct rF2StateHeader
        {
            internal byte mCurrentRead;                 // True indicates buffer is safe to read under mutex.
        }

        // Our world coordinate system is left-handed, with +y pointing up.
        // The local vehicle coordinate system is as follows:
        //   +x points out the left side of the car (from the driver's perspective)
        //   +y points out the roof
        //   +z points out the back of the car
        // Rotations are as follows:
        //   +x pitches up
        //   +y yaws to the right
        //   +z rolls to the right
        // Note that ISO vehicle coordinates (+x forward, +y right, +z upward) are
        // right-handed.  If you are using that system, be sure to negate any rotation
        // or torque data because things rotate in the opposite direction.  In other
        // words, a -z velocity in rFactor is a +x velocity in ISO, but a -z rotation
        // in rFactor is a -x rotation in ISO!!!

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi, Pack = 16)]
        struct rF2State
        {
            internal byte mCurrentRead;             // True indicates buffer is safe to read under mutex.
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            internal byte[] mVersion;               // API version
            internal int mID;                       // slot ID (note that it can be re-used in multiplayer after someone leaves)

            // Time
            internal double mDeltaTime;             // time since last update (seconds)
            internal double mElapsedTime;           // game session time
            internal int mLapNumber;                // current lap number
            internal double mLapStartET;            // time this lap was started
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            internal byte[] mVehicleName;           // current vehicle name
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            internal byte[] mTrackName;             // current track name

            // Position and derivatives
            internal rF2Vec3 mPos;                  // world position in meters
            internal rF2Vec3 mLocalVel;             // velocity (meters/sec) in local vehicle coordinates
            internal rF2Vec3 mLocalAccel;           // acceleration (meters/sec^2) in local vehicle coordinates

            internal double mSpeed;                 // meters/sec

            // Orientation and derivatives
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            internal rF2Vec3[] mOri;                // rows of orientation matrix (use TelemQuat conversions if desired), also converts local
            // vehicle vectors into world X, Y, or Z using dot product of rows 0, 1, or 2 respectively

            internal rF2Vec3 mLocalRot;             // rotation (radians/sec) in local vehicle coordinates
            internal rF2Vec3 mLocalRotAccel;        // rotational acceleration (radians/sec^2) in local vehicle coordinates

            // Vehicle status
            internal int mGear;                    // -1=reverse, 0=neutral, 1+=forward gears
            internal double mEngineRPM;             // engine RPM
            internal double mEngineWaterTemp;       // Celsius
            internal double mEngineOilTemp;         // Celsius
            internal double mClutchRPM;             // clutch RPM

            // Driver input
            internal double mUnfilteredThrottle;    // ranges  0.0-1.0
            internal double mUnfilteredBrake;       // ranges  0.0-1.0
            internal double mUnfilteredSteering;    // ranges -1.0-1.0 (left to right)
            internal double mUnfilteredClutch;      // ranges  0.0-1.0

            // Filtered input (various adjustments for rev or speed limiting, TC, ABS?, speed sensitive steering, clutch work for semi-automatic shifting, etc.)
            internal double mFilteredThrottle;      // ranges  0.0-1.0
            internal double mFilteredBrake;         // ranges  0.0-1.0
            internal double mFilteredSteering;      // ranges -1.0-1.0 (left to right)
            internal double mFilteredClutch;        // ranges  0.0-1.0

            // Misc
            internal double mSteeringShaftTorque;   // torque around steering shaft (used to be mSteeringArmForce, but that is not necessarily accurate for feedback purposes)
            internal double mFront3rdDeflection;    // deflection at front 3rd spring
            internal double mRear3rdDeflection;     // deflection at rear 3rd spring

            // Aerodynamics
            internal double mFrontWingHeight;       // front wing height
            internal double mFrontRideHeight;       // front ride height
            internal double mRearRideHeight;        // rear ride height
            internal double mDrag;                  // drag
            internal double mFrontDownforce;        // front downforce
            internal double mRearDownforce;         // rear downforce

            // State/damage info
            internal double mFuel;                  // amount of fuel (liters)
            internal double mEngineMaxRPM;          // rev limit
            internal byte mScheduledStops;          // number of scheduled pitstops
            internal byte mOverheating;            // whether overheating icon is shown
            internal byte mDetached;               // whether any parts (besides wheels) have been detached
            internal byte mHeadlights;             // whether headlights are on
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 8)]
            internal byte[] mDentSeverity;         // dent severity at 8 locations around the car (0=none, 1=some, 2=more)
            internal double mLastImpactET;          // time of last impact
            internal double mLastImpactMagnitude;   // magnitude of last impact
            internal rF2Vec3 mLastImpactPos;        // location of last impact

            // Expanded
            internal double mEngineTorque;          // current engine torque (including additive torque) (used to be mEngineTq, but there's little reason to abbreviate it)
            internal int mCurrentSector;            // the current sector (zero-based) with the pitlane stored in the sign bit (example: entering pits from third sector gives 0x80000002)
            internal byte mSpeedLimiter;   // whether speed limiter is on
            internal byte mMaxGears;       // maximum forward gears
            internal byte mFrontTireCompoundIndex;   // index within brand
            internal byte mRearTireCompoundIndex;    // index within brand
            internal double mFuelCapacity;           // capacity in liters
            internal byte mFrontFlapActivated;       // whether front flap is activated
            internal byte mRearFlapActivated;        // whether rear flap is activated
            internal byte mRearFlapLegalStatus;      // 0=disallowed, 1=criteria detected but not allowed quite yet, 2=allowed
            internal byte mIgnitionStarter;          // 0=off 1=ignition 2=ignition+starter

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 18)]
            internal byte[] mFrontTireCompoundName;       // name of front tire compound
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 18)]
            internal byte[] mRearTireCompoundName;        // name of rear tire compound

            internal byte mSpeedLimiterAvailable;    // whether speed limiter is available
            internal byte mAntiStallActivated;       // whether (hard) anti-stall is activated
            float mVisualSteeringWheelRange;         // the *visual* steering wheel range

            internal double mRearBrakeBias;                   // fraction of brakes on rear
            internal double mTurboBoostPressure;              // current turbo boost pressure if available
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            float[] mPhysicsToGraphicsOffset;       // offset from static CG to graphical center
            float mPhysicalSteeringWheelRange;       // the *physical* steering wheel range

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 152)]
            byte[] mExpansionTelem;                    // for future use (note that the slot ID has been moved to mID above)

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
            internal rF2Wheel[] mWheels;            // wheel info (front left, front right, rear left, rear right)

            internal int mSession;                  // current session (0=testday 1-4=practice 5-8=qual 9=warmup 10-13=race)
            internal double mCurrentET;             // current time
            internal double mEndET;                 // ending time
            internal int mMaxLaps;                  // maximum laps
            internal double mLapDist;               // distance around track

            internal int mNumVehicles;             // current number of vehicles

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
            internal byte mGamePhase;

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
            internal sbyte mYellowFlagState;

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
            internal sbyte[] mSectorFlag;             // whether there are any local yellows at the moment in each sector (not sure if sector 0 is first or last, so test)
            internal byte mStartLight;                // start light frame (number depends on track)
            internal byte mNumRedLights;              // number of red lights in start sequence
            internal byte mInRealtime;                // in realtime as opposed to at the monitor
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 32)]
            internal byte[] mPlayerName;              // player name (including possible multiplayer override)
            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
            internal byte[] mPlrFileName;             // may be encoded to be a legal filename

            // Weather
            internal double mDarkCloud;               // cloud darkness? 0.0-1.0
            internal double mRaining;                 // raining severity 0.0-1.0
            internal double mAmbientTemp;             // temperature (Celsius)
            internal double mTrackTemp;               // temperature (Celsius)
            internal rF2Vec3 mWind;                   // wind speed
            internal double mMinPathWetness;          // minimum wetness on main path 0.0-1.0
            internal double mMaxPathWetness;          // maximum wetness on main path 0.0-1.0

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 256)]
            byte[] mExpansionScoring;                 // Future use.

            [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = rFactor2Constants.MAX_VSI_SIZE)]
            internal rF2VehScoringInfo[] mVehicles;  // array of vehicle scoring info's
            // NOTE: everything beyound mNumVehicles is trash.
        }
    }
}