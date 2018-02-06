using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CrewChiefV4.PCars
{
    public class StructHelper
    {
        public static Encoding ENCODING;
        public static String NULL_CHAR;
                   
        static StructHelper() {
            try {
                ENCODING = Encoding.GetEncoding(UserSettings.GetUserSettings().getString("pcars_character_encoding"));
                NULL_CHAR = ENCODING.GetString(new byte[] { 0 }, 0, 1);
            }
            catch (System.ArgumentException)
            {
                Console.WriteLine("Using default encoding");
                ENCODING = Encoding.Default;
                NULL_CHAR = ENCODING.GetString(new byte[] { 0 }, 0, 1);
            }
        }
        public static pCarsAPIStruct Clone<pCarsAPIStruct>(pCarsAPIStruct pcarsStruct)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, pcarsStruct);
                ms.Position = 0;
                return (pCarsAPIStruct)formatter.Deserialize(ms);
            }
        }

        public static pCarsAPIStruct MergeWithExistingState(pCarsAPIStruct existingState, sTelemetryData udpTelemetryData)
        {
            if (existingState.isSameClassAsPlayer == null)
            {
                existingState.isSameClassAsPlayer = new Boolean[(int)eAPIStructLengths.NUM_PARTICIPANTS];
            }
            existingState.hasOpponentClassData = false;
            existingState.hasNewPositionData = false;
            existingState.mGameState = (uint) udpTelemetryData.sGameSessionState & 7;
            existingState.mSessionState = (uint) udpTelemetryData.sGameSessionState >> 4;
            existingState.mRaceState = (uint) udpTelemetryData.sRaceStateFlags & 7;

            // Participant Info
            existingState.mViewedParticipantIndex = udpTelemetryData.sViewedParticipantIndex;
            existingState.mNumParticipants = udpTelemetryData.sNumParticipants;            

            // Unfiltered Input
            existingState.mUnfilteredThrottle = (float)udpTelemetryData.sUnfilteredThrottle / 255f;
            existingState.mUnfilteredBrake = (float)udpTelemetryData.sUnfilteredBrake / 255f;
            existingState.mUnfilteredSteering = (float)udpTelemetryData.sUnfilteredSteering / 127f;
            existingState.mUnfilteredClutch = (float)udpTelemetryData.sUnfilteredClutch / 255f;

            existingState.mLapsInEvent = udpTelemetryData.sLapsInEvent;
            existingState.mTrackLength = udpTelemetryData.sTrackLength; 

            // Timing & Scoring
            existingState.mLapInvalidated = (udpTelemetryData.sRaceStateFlags >> 3 & 1) == 1;
            existingState.mSessionFastestLapTime = udpTelemetryData.sBestLapTime;
            existingState.mLastLapTime = udpTelemetryData.sLastLapTime;
            existingState.mCurrentTime = udpTelemetryData.sCurrentTime;
            existingState.mSplitTimeAhead = udpTelemetryData.sSplitTimeAhead;
            existingState.mSplitTimeBehind = udpTelemetryData.sSplitTimeBehind;
            existingState.mSplitTime = udpTelemetryData.sSplitTime;
            existingState.mEventTimeRemaining = udpTelemetryData.sEventTimeRemaining; 
            existingState.mPersonalFastestLapTime = udpTelemetryData.sPersonalFastestLapTime;
            existingState.mWorldFastestLapTime = udpTelemetryData.sWorldFastestLapTime;
            existingState.mCurrentSector1Time = udpTelemetryData.sCurrentSector1Time;
            existingState.mCurrentSector2Time = udpTelemetryData.sCurrentSector2Time; 
            existingState.mCurrentSector3Time = udpTelemetryData.sCurrentSector3Time; 
            existingState.mSessionFastestSector1Time = udpTelemetryData.sFastestSector1Time; 
            existingState.mSessionFastestSector2Time = udpTelemetryData.sFastestSector2Time; 
            existingState.mSessionFastestSector3Time = udpTelemetryData.sFastestSector3Time; 
            existingState.mPersonalFastestSector1Time = udpTelemetryData.sPersonalFastestSector1Time; 
            existingState.mPersonalFastestSector2Time = udpTelemetryData.sPersonalFastestSector2Time; 
            existingState.mPersonalFastestSector3Time = udpTelemetryData.sPersonalFastestSector3Time;
            existingState.mWorldFastestSector1Time = udpTelemetryData.sWorldFastestSector1Time; 
            existingState.mWorldFastestSector2Time = udpTelemetryData.sWorldFastestSector2Time;
            existingState.mWorldFastestSector3Time = udpTelemetryData.sWorldFastestSector3Time;

            // Flags
            existingState.mHighestFlagColour = (uint) udpTelemetryData.sHighestFlag & 7; 
            existingState.mHighestFlagReason = (uint) udpTelemetryData.sHighestFlag >> 3 & 3;

            // Pit Info
            existingState.mPitMode = (uint) udpTelemetryData.sPitModeSchedule & 7;
            existingState.mPitSchedule = (uint) udpTelemetryData.sPitModeSchedule >> 3 & 3;

            // Car State
            existingState.mCarFlags = udpTelemetryData.sCarFlags;
            existingState.mOilTempCelsius = udpTelemetryData.sOilTempCelsius; 
            existingState.mOilPressureKPa = udpTelemetryData.sOilPressureKPa; 
            existingState.mWaterTempCelsius = udpTelemetryData.sWaterTempCelsius; 
            existingState.mWaterPressureKPa = udpTelemetryData.sWaterPressureKpa;
            existingState.mFuelPressureKPa = udpTelemetryData.sFuelPressureKpa;
            existingState.mFuelLevel = udpTelemetryData.sFuelLevel;
            existingState.mFuelCapacity = udpTelemetryData.sFuelCapacity; 
            existingState.mSpeed = udpTelemetryData.sSpeed; 
            existingState.mRPM = udpTelemetryData.sRpm;
            existingState.mMaxRPM = udpTelemetryData.sMaxRpm;
            existingState.mBrake = (float)udpTelemetryData.sBrake / 255f;
            existingState.mThrottle = (float)udpTelemetryData.sThrottle / 255f;
            existingState.mClutch = (float)udpTelemetryData.sClutch / 255f;
            existingState.mSteering = (float)udpTelemetryData.sSteering / 127f;
            existingState.mGear = udpTelemetryData.sGearNumGears & 15;
            existingState.mNumGears = udpTelemetryData.sGearNumGears >> 4;
            existingState.mOdometerKM = udpTelemetryData.sOdometerKM;                               
            existingState.mAntiLockActive = (udpTelemetryData.sRaceStateFlags >> 4 & 1) == 1;
            existingState.mBoostActive = (udpTelemetryData.sRaceStateFlags >> 5 & 1) == 1;
            existingState.mBoostAmount = udpTelemetryData.sBoostAmount;

            // Motion & Device Related
            existingState.mOrientation = udpTelemetryData.sOrientation; 
            existingState.mLocalVelocity = udpTelemetryData.sLocalVelocity;
            existingState.mWorldVelocity = udpTelemetryData.sWorldVelocity;
            existingState.mAngularVelocity = udpTelemetryData.sAngularVelocity;
            existingState.mLocalAcceleration = udpTelemetryData.sLocalAcceleration;
            existingState.mWorldAcceleration = udpTelemetryData.sWorldAcceleration;
            existingState.mExtentsCentre = udpTelemetryData.sExtentsCentre; 


            existingState.mTyreFlags = toUIntArray(udpTelemetryData.sTyreFlags); 
            existingState.mTerrain = toUIntArray(udpTelemetryData.sTerrain);
            existingState.mTyreY = udpTelemetryData.sTyreY;
            existingState.mTyreRPS = udpTelemetryData.sTyreRPS;
            existingState.mTyreSlipSpeed = udpTelemetryData.sTyreSlipSpeed;
            existingState.mTyreTemp = toFloatArray(udpTelemetryData.sTyreTemp, 255); 
            existingState.mTyreGrip = toFloatArray(udpTelemetryData.sTyreGrip, 255);
            existingState.mTyreHeightAboveGround = udpTelemetryData.sTyreHeightAboveGround;
            existingState.mTyreLateralStiffness = udpTelemetryData.sTyreLateralStiffness;
            existingState.mTyreWear = toFloatArray(udpTelemetryData.sTyreWear, 255); 
            existingState.mBrakeDamage = toFloatArray(udpTelemetryData.sBrakeDamage, 255);
            existingState.mSuspensionDamage = toFloatArray(udpTelemetryData.sSuspensionDamage, 255);    
            existingState.mBrakeTempCelsius = toFloatArray(udpTelemetryData.sBrakeTempCelsius, 1);
            existingState.mTyreTreadTemp = toFloatArray(udpTelemetryData.sTyreTreadTemp, 1);            
            existingState.mTyreLayerTemp = toFloatArray(udpTelemetryData.sTyreLayerTemp, 1); 
            existingState.mTyreCarcassTemp = toFloatArray(udpTelemetryData.sTyreCarcassTemp, 1); 
            existingState.mTyreRimTemp = toFloatArray(udpTelemetryData.sTyreRimTemp, 1);    
            existingState.mTyreInternalAirTemp = toFloatArray(udpTelemetryData.sTyreInternalAirTemp, 1);
            existingState.mWheelLocalPosition = udpTelemetryData.sWheelLocalPositionY;
            existingState.mRideHeight = udpTelemetryData.sRideHeight;
            existingState.mSuspensionTravel = udpTelemetryData.sSuspensionTravel;
            existingState.mSuspensionVelocity = udpTelemetryData.sSuspensionVelocity;
            existingState.mAirPressure = toFloatArray(udpTelemetryData.sAirPressure, 1);

            existingState.mEngineSpeed = udpTelemetryData.sEngineSpeed;
            existingState.mEngineTorque = udpTelemetryData.sEngineTorque;
            existingState.mEnforcedPitStopLap = udpTelemetryData.sEnforcedPitStopLap;

            // Car Damage
            existingState.mCrashState = udpTelemetryData.sCrashState;
            existingState.mAeroDamage = (float)udpTelemetryData.sAeroDamage / 255f;
            existingState.mEngineDamage = (float)udpTelemetryData.sEngineDamage / 255f; 

            // Weather
            existingState.mAmbientTemperature = udpTelemetryData.sAmbientTemperature;
            existingState.mTrackTemperature = udpTelemetryData.sTrackTemperature;
            existingState.mRainDensity = (float)udpTelemetryData.sRainDensity / 255f;         
            existingState.mWindSpeed = udpTelemetryData.sWindSpeed * 2;
            existingState.mWindDirectionX = (float)udpTelemetryData.sWindDirectionX / 127f;
            existingState.mWindDirectionY = (float)udpTelemetryData.sWindDirectionY / 127f;
            //existingState.mCloudBrightness = udpTelemetryData.sCloudBrightness / 255;

            if (existingState.mParticipantData == null)
            {
                existingState.mParticipantData = new pCarsAPIParticipantStruct[56];
            }

            if (existingState.mLastSectorData == null)
            {
                existingState.mLastSectorData = new float[56];
            }

            if (existingState.mLapInvalidatedData == null)
            {
                existingState.mLapInvalidatedData = new Boolean[56];
            }
            for (int i = 0; i < udpTelemetryData.sParticipantInfo.Count(); i++) 
            {
                sParticipantInfo newPartInfo = udpTelemetryData.sParticipantInfo[i];
                Boolean isActive = (newPartInfo.sRacePosition >> 7) == 1;
                pCarsAPIParticipantStruct existingPartInfo = existingState.mParticipantData[i];
                
                if (isActive)
                {
                    existingPartInfo.mIsActive = i < existingState.mNumParticipants;
                    existingPartInfo.mCurrentLap = newPartInfo.sCurrentLap;
                    existingPartInfo.mCurrentLapDistance = newPartInfo.sCurrentLapDistance;
                    existingPartInfo.mLapsCompleted = (uint) newPartInfo.sLapsCompleted & 127;
                    Boolean lapInvalidated = (newPartInfo.sLapsCompleted >> 7) == 1;
                    existingPartInfo.mRacePosition = (uint) newPartInfo.sRacePosition & 127;
                    existingPartInfo.mCurrentSector = (uint)newPartInfo.sSector & 7;
                    Boolean sameClassAsPlayer = (newPartInfo.sSector >> 3 & 1) == 1;
                    if (sameClassAsPlayer) {
                        existingState.hasOpponentClassData = true;
                    }
                    existingState.isSameClassAsPlayer[i] = sameClassAsPlayer;


                    // and now the bit magic for the extra position precision...
                    float[] newWorldPositions = toFloatArray(newPartInfo.sWorldPosition, 1);
                    float xAdjustment = ((float)((uint)newPartInfo.sSector >> 6 & 3)) / 4f;
                    float zAdjustment = ((float)((uint)newPartInfo.sSector >> 4 & 3)) / 4f;

                    newWorldPositions[0] = newWorldPositions[0] + xAdjustment;
                    newWorldPositions[2] = newWorldPositions[2] + zAdjustment;
                    if (!existingState.hasNewPositionData && i != udpTelemetryData.sViewedParticipantIndex && 
                        (existingPartInfo.mWorldPosition == null || (newWorldPositions[0] != existingPartInfo.mWorldPosition[0] || newWorldPositions[2] != existingPartInfo.mWorldPosition[2])))
                    {
                        existingState.hasNewPositionData = true;
                    }
                    existingPartInfo.mWorldPosition = newWorldPositions;

                    // LastSectorTime is now in the UDP data, but there's no slot for this in the participants struct
                    // so bung it in a separate array at the end
                    existingState.mLastSectorData[i] = newPartInfo.sLastSectorTime;
                    existingState.mLapInvalidatedData[i] = lapInvalidated;
                }
                else
                {
                    existingPartInfo.mWorldPosition = new float[] { 0, 0, 0 };
                    existingPartInfo.mIsActive = false;
                }
                existingState.mParticipantData[i] = existingPartInfo;
            }

            return existingState;
        }

        public static pCarsAPIStruct MergeWithExistingState(pCarsAPIStruct existingState, sParticipantInfoStringsAdditional udpAdditionalStrings)
        {
            int offset = udpAdditionalStrings.sOffset;
            if (existingState.mParticipantData == null)
            {
                existingState.mParticipantData = new pCarsAPIParticipantStruct[56];
            }
            for (int i = offset; i < offset + 16 && i<existingState.mParticipantData.Length; i++) 
            {
                existingState.mParticipantData[i].mName = udpAdditionalStrings.sName[i - offset].nameByteArray;
            }
            return existingState;
        }

        public static pCarsAPIStruct MergeWithExistingState(pCarsAPIStruct existingState, sParticipantInfoStrings udpParticipantStrings)
        {
            existingState.mCarClassName = udpParticipantStrings.sCarClassName;
            existingState.mCarName = udpParticipantStrings.sCarName;

            existingState.mTrackLocation = udpParticipantStrings.sTrackLocation;
            existingState.mTrackVariation = udpParticipantStrings.sTrackVariation;
            if (existingState.mParticipantData == null)
            {
                existingState.mParticipantData = new pCarsAPIParticipantStruct[56];
            }
            for (int i = 0; i < udpParticipantStrings.sName.Count(); i++)
            {
                existingState.mParticipantData[i].mName = udpParticipantStrings.sName[i].nameByteArray;
            }
            return existingState;
        }

        public static String getNameFromBytes(byte[] bytes)
        {
            if (bytes == null || bytes.Length == 0)
            {
                return "";
            }
            // check if the first char is null and skip it if so
            int startIndex = bytes[0] == (byte)0 ? 1 : 0;
            // get the first (or second if char[0] is null) null char
            int numBytesToDecode = Array.IndexOf(bytes, (byte)0, startIndex);
            if (numBytesToDecode == 0)
            {
                // first and second chars are null. Return empty string
                return "";
            }
            if (numBytesToDecode == -1)
            {
                // no null char after char 0, so we want the whole array. Note that the number of bytes we want
                // here will be 1 less if we've skipped char 0
                numBytesToDecode = startIndex == 1 ? bytes.Length - 1 : bytes.Length;
            }
            // If start index is 1, prefix the decoded string with the magic null-standin-char
            return startIndex == 1 ? PCarsGameStateMapper.NULL_CHAR_STAND_IN + ENCODING.GetString(bytes, startIndex, numBytesToDecode) :
                ENCODING.GetString(bytes, startIndex, numBytesToDecode);
        }

        private static float[] toFloatArray(byte[] byteArray, float factor)
        {
            List<float> l = new List<float>();
            foreach (byte i in byteArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static float[] toFloatArray(short[] shortArray, float factor)
        {
            List<float> l = new List<float>();
            foreach (short i in shortArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static float[] toFloatArray(ushort[] ushortArray, float factor)
        {
            List<float> l = new List<float>();
            foreach (ushort i in ushortArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
        }

        private static uint[] toUIntArray(byte[] byteArray)
        {
            List<uint> l = new List<uint>();
            foreach (byte i in byteArray)
            {
                l.Add((byte)i);
            }
            return l.ToArray();
        }
    }

    [Serializable]
    public struct pCarsAPIParticipantStruct
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool mIsActive;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public byte[] mName;                                    // [ string ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mWorldPosition;                          // [ UNITS = World Space  X  Y  Z ]

        public float mCurrentLapDistance;                       // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]
        public uint mRacePosition;                              // [ RANGE = 1->... ]   [ UNSET = 0 ]
        public uint mLapsCompleted;                             // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentLap;                                // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentSector;                             // [ enum (Type#4) Current Sector ]
    }

    [Serializable]
    public struct pCarsAPIStruct
    {
        //SMS supplied data structure
        // Version Number
        public uint mVersion;                           // [ RANGE = 0->... ]
        public uint mBuildVersion;                      // [ RANGE = 0->... ]   [ UNSET = 0 ]

        // Session type
        public uint mGameState;                         // [ enum (Type#1) Game state ]
        public uint mSessionState;                      // [ enum (Type#2) Session state ]
        public uint mRaceState;                         // [ enum (Type#3) Race State ]

        // Participant Info
        public int mViewedParticipantIndex;                      // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public int mNumParticipants;                             // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = (int)eAPIStructLengths.NUM_PARTICIPANTS)]
        public pCarsAPIParticipantStruct[] mParticipantData;

        // Unfiltered Input
        public float mUnfilteredThrottle;                       // [ RANGE = 0.0f->1.0f ]
        public float mUnfilteredBrake;                          // [ RANGE = 0.0f->1.0f ]
        public float mUnfilteredSteering;                       // [ RANGE = -1.0f->1.0f ]
        public float mUnfilteredClutch;                         // [ RANGE = 0.0f->1.0f ]

        // Vehicle & Track information
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public byte[] mCarName;                                 // [ string ]
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eAPIStructLengths.STRING_LENGTH_MAX)]
        public byte[] mCarClassName;                            // [ string ]

        public uint mLapsInEvent;                               // [ RANGE = 0->... ]   [ UNSET = 0 ]
   
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = ((int)eAPIStructLengths.STRING_LENGTH_MAX))]
        public byte[] mTrackLocation;
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = ((int)eAPIStructLengths.STRING_LENGTH_MAX))]
        public byte[] mTrackVariation;                          // [ string ]

        public float mTrackLength;                              // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]

        // Timing & Scoring

        // NOTE: 
        // The mSessionFastest... times are only for the player. The overall session fastest time is NOT in the block. Anywhere...
        // The mPersonalFastest... times are often -1. Perhaps they're the player's hotlap / offline practice records for this track.
        //
        public bool mLapInvalidated;                            // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public float mSessionFastestLapTime;                              // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mLastLapTime;                              // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mCurrentTime;                              // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mSplitTimeAhead;                            // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSplitTimeBehind;                           // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSplitTime;                                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mEventTimeRemaining;                       // [ UNITS = milli-seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestLapTime;                    // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestLapTime;                       // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector1Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector2Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector3Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSessionFastestSector1Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSessionFastestSector2Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSessionFastestSector3Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestSector1Time;                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestSector2Time;                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestSector3Time;                // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestSector1Time;                   // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestSector2Time;                   // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestSector3Time;                   // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]

        // Flags
        public uint mHighestFlagColour;                 // [ enum (Type#5) Flag Colour ]
        public uint mHighestFlagReason;                 // [ enum (Type#6) Flag Reason ]

        // Pit Info
        public uint mPitMode;                           // [ enum (Type#7) Pit Mode ]
        public uint mPitSchedule;                       // [ enum (Type#8) Pit Stop Schedule ]

        // Car State
        public uint mCarFlags;                          // [ enum (Type#6) Car Flags ]
        public float mOilTempCelsius;                           // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public float mOilPressureKPa;                           // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mWaterTempCelsius;                         // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public float mWaterPressureKPa;                         // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mFuelPressureKPa;                          // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mFuelLevel;                                // [ RANGE = 0.0f->1.0f ]
        public float mFuelCapacity;                             // [ UNITS = Liters ]   [ RANGE = 0.0f->1.0f ]   [ UNSET = 0.0f ]
        public float mSpeed;                                    // [ UNITS = Metres per-second ]   [ RANGE = 0.0f->... ]
        public float mRPM;                                      // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mMaxRPM;                                   // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mBrake;                                    // [ RANGE = 0.0f->1.0f ]
        public float mThrottle;                                 // [ RANGE = 0.0f->1.0f ]
        public float mClutch;                                   // [ RANGE = 0.0f->1.0f ]
        public float mSteering;                                 // [ RANGE = -1.0f->1.0f ]
        public int mGear;                                       // [ RANGE = -1 (Reverse)  0 (Neutral)  1 (Gear 1)  2 (Gear 2)  etc... ]   [ UNSET = 0 (Neutral) ]
        public int mNumGears;                                   // [ RANGE = 0->... ]   [ UNSET = -1 ]
        public float mOdometerKM;                               // [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public bool mAntiLockActive;                            // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public int mLastOpponentCollisionIndex;                 // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public float mLastOpponentCollisionMagnitude;           // [ RANGE = 0.0f->... ]
        public bool mBoostActive;                               // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public float mBoostAmount;                              // [ RANGE = 0.0f->100.0f ] 

        // Motion & Device Related
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mOrientation;                     // [ UNITS = Euler Angles ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mLocalVelocity;                   // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mWorldVelocity;                   // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mAngularVelocity;                 // [ UNITS = Radians per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mLocalAcceleration;               // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mWorldAcceleration;               // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eVector.VEC_MAX)]
        public float[] mExtentsCentre;                   // [ UNITS = Local Space  X  Y  Z ]

        // Wheels / Tyres
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public uint[] mTyreFlags;               // [ enum (Type#7) Tyre Flags ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public uint[] mTerrain;                 // [ enum (Type#3) Terrain Materials ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreY;                          // [ UNITS = Local Space  Y ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreRPS;                        // [ UNITS = Revolutions per second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreSlipSpeed;                  // [ UNITS = Metres per-second ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreTemp;                       // [ UNITS = Celsius ]   [ UNSET = 0.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreGrip;                       // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreHeightAboveGround;          // [ UNITS = Local Space  Y ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreLateralStiffness;           // [ UNITS = Lateral stiffness coefficient used in tyre deformation ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreWear;                       // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mBrakeDamage;                    // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mSuspensionDamage;               // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mBrakeTempCelsius;               // [ RANGE = 0.0f->1.0f ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreTreadTemp;                  // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreLayerTemp;                  // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreCarcassTemp;                // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreRimTemp;                    // [ UNITS = Kelvin ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = (int)eTyres.TYRE_MAX)]
        public float[] mTyreInternalAirTemp;            // [ UNITS = Kelvin ]


        // Car Damage
        public uint mCrashState;                        // [ enum (Type#4) Crash Damage State ]
        public float mAeroDamage;                               // [ RANGE = 0.0f->1.0f ]
        public float mEngineDamage;                             // [ RANGE = 0.0f->1.0f ]

        // Weather
        public float mAmbientTemperature;                       // [ UNITS = Celsius ]   [ UNSET = 25.0f ]
        public float mTrackTemperature;                         // [ UNITS = Celsius ]   [ UNSET = 30.0f ]
        public float mRainDensity;                              // [ UNITS = How much rain will fall ]   [ RANGE = 0.0f->1.0f ]
        public float mWindSpeed;                                // [ RANGE = 0.0f->100.0f ]   [ UNSET = 2.0f ]
        public float mWindDirectionX;                           // [ UNITS = Normalised Vector X ]
        public float mWindDirectionY;                           // [ UNITS = Normalised Vector Y ]
        public float mCloudBrightness;                          // [ RANGE = 0.0f->... ]

        // extras from the UDP data
        public float[] mWheelLocalPosition;

        public float[] mRideHeight;

        public float[] mSuspensionTravel;

        public float[] mSuspensionVelocity;

        public float[] mAirPressure;

        public float mEngineSpeed;

        public float mEngineTorque;

        public int mEnforcedPitStopLap;
        
        public Boolean hasNewPositionData;

        public Boolean[] isSameClassAsPlayer;

        public Boolean hasOpponentClassData;

        public float[] mLastSectorData;

        public Boolean[] mLapInvalidatedData;
    }
}