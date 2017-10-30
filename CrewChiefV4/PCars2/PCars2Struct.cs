using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CrewChiefV4.PCars2
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
        public static pCars2APIStruct Clone<pCars2APIStruct>(pCars2APIStruct pcars2Struct)
        {
            using (var ms = new MemoryStream())
            {
                var formatter = new BinaryFormatter();
                formatter.Serialize(ms, pcars2Struct);
                ms.Position = 0;
                return (pCars2APIStruct)formatter.Deserialize(ms);
            }
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sTelemetryData udpTelemetryData)
        {
            // Participant Info
            existingState.mViewedParticipantIndex = udpTelemetryData.sViewedParticipantIndex;

            // Unfiltered Input
            existingState.mUnfilteredThrottle = (float)udpTelemetryData.sUnfilteredThrottle / 255f;
            existingState.mUnfilteredBrake = (float)udpTelemetryData.sUnfilteredBrake / 255f;
            existingState.mUnfilteredSteering = (float)udpTelemetryData.sUnfilteredSteering / 127f;
            existingState.mUnfilteredClutch = (float)udpTelemetryData.sUnfilteredClutch / 255f;

            // Timing & Scoring are not in this packet
           

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
            existingState.mMaxRPM = udpTelemetryData.sMaxRpm;
            existingState.mBrake = (float)udpTelemetryData.sBrake / 255f;
            existingState.mThrottle = (float)udpTelemetryData.sThrottle / 255f;
            existingState.mClutch = (float)udpTelemetryData.sClutch / 255f;
            existingState.mSteering = (float)udpTelemetryData.sSteering / 127f;
            existingState.mGear = udpTelemetryData.sGearNumGears & 15;
            existingState.mNumGears = udpTelemetryData.sGearNumGears >> 4;
            existingState.mOdometerKM = udpTelemetryData.sOdometerKM;                             
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
            existingState.mTyreTemp = toFloatArray(udpTelemetryData.sTyreTemp, 255); 
            existingState.mTyreHeightAboveGround = udpTelemetryData.sTyreHeightAboveGround;
            existingState.mTyreWear = toFloatArray(udpTelemetryData.sTyreWear, 255); 
            existingState.mBrakeDamage = toFloatArray(udpTelemetryData.sBrakeDamage, 255);
            existingState.mSuspensionDamage = toFloatArray(udpTelemetryData.sSuspensionDamage, 255);    
            existingState.mBrakeTempCelsius = toFloatArray(udpTelemetryData.sBrakeTempCelsius, 1);
            existingState.mTyreTreadTemp = toFloatArray(udpTelemetryData.sTyreTreadTemp, 1);            
            existingState.mTyreLayerTemp = toFloatArray(udpTelemetryData.sTyreLayerTemp, 1); 
            existingState.mTyreCarcassTemp = toFloatArray(udpTelemetryData.sTyreCarcassTemp, 1); 
            existingState.mTyreRimTemp = toFloatArray(udpTelemetryData.sTyreRimTemp, 1);    
            existingState.mTyreInternalAirTemp = toFloatArray(udpTelemetryData.sTyreInternalAirTemp, 1);
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
                pCars2APIParticipantStruct existingPartInfo = existingState.mParticipantData[i];
                
                if (isActive)
                {
                    existingPartInfo.mIsActive = i < existingState.mNumParticipants;
                    existingPartInfo.mCurrentLap = newPartInfo.sCurrentLap;
                    existingPartInfo.mCurrentLapDistance = newPartInfo.sCurrentLapDistance;
                    existingPartInfo.mLapsCompleted = (uint) newPartInfo.sLapsCompleted & 127;
                    // TODO: there's a 'lapInvalidated' flag here but nowhere to put it in the existing struct
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

            // TODO: buttons
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sRaceData raceData)
        {
            Boolean isTimedSession = (raceData.sLapsTimeInEvent >> 15) == 1;
            int sessionLength = raceData.sLapsTimeInEvent / 2;  // need the bottom bits of this short here - is it valid to just halve it?
            if (isTimedSession)
            {
                existingState.mLapsInEvent = (uint) sessionLength;
                existingState.mSessionLengthTimeFromGame = 0;
            }
            else
            {
                existingState.mSessionLengthTimeFromGame = 300 * (uint) sessionLength; // *300 because this is in 5 minutes blocks
                existingState.mLapsInEvent = 0;
            }
            existingState.mTrackLength = raceData.sTrackLength;
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sParticipantsData participantsData)
        {
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sTimingsData timingsData)
        {
            existingState.mNumParticipants = timingsData.sNumParticipants;            
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sGameStateData gameStateData)
        {
            existingState.mGameState = (uint)gameStateData.mGameState & 7;
            existingState.mSessionState = (uint)gameStateData.mGameState>> 4;
            //existingState.mRaceState = (uint)gameStateData.sRaceStateFlags & 7;
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sTimeStatsData timeStatsData)
        {
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sParticipantVehicleNamesData participantVehicleNamesData)
        {
            return existingState;
        }

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sVehicleClassNamesData vehicleClassNamesData)
        {
            return existingState;
        }

        /*public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sParticipantInfoStringsAdditional udpAdditionalStrings)
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

        public static pCars2APIStruct MergeWithExistingState(pCars2APIStruct existingState, sParticipantInfoStrings udpParticipantStrings)
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
        }*/

        public static String getNameFromBytes(byte[] name)
        {
            //return Encoding.UTF8.GetString(name).TrimEnd('\0').Trim();
            if (name == null || name.Length == 0)
            {
                return "";
            }
            String firstChar = ENCODING.GetString(name, 0, 1).TrimEnd('\0');
            if (name.Length == 1)
            {
                return firstChar;
            }
            String rest = ENCODING.GetString(name, 1, name.Length - 1).TrimEnd('\0');
            if ((firstChar == null || firstChar.Trim().Length == 0) && (rest != null && rest.Trim().Length > 0))
            {
                firstChar = PCars2GameStateMapper.NULL_CHAR_STAND_IN;
            }
            else
            {
                firstChar = firstChar.Trim();
            }
            // the game sometimes doesn't clear the byte array for a string when this string changes. This means we sometimes get the 
            // actual string bytes, followed by whatever was in the remaining positions in that byte array from the previous String it
            // contained. We can't do much about this except trim off any remaining characters after the first null. 
            if (rest.Contains(NULL_CHAR))
            {
                rest = rest.Substring(0, rest.IndexOf(NULL_CHAR));
            }
            return (firstChar + rest).Trim();
        } 

        private static float[] toFloatArray(int[] intArray, float factor)
        {
            List<float> l = new List<float>();
            foreach (int i in intArray)
            {
                l.Add(((float)i) / factor);
            }
            return l.ToArray();
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
    public struct pCars2APIParticipantStruct
    {
        [MarshalAs(UnmanagedType.I1)]
        public bool mIsActive;

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mName;                                    // [ string ]

        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mWorldPosition;                          // [ UNITS = World Space  X  Y  Z ]

        public float mCurrentLapDistance;                       // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]
        public uint mRacePosition;                              // [ RANGE = 1->... ]   [ UNSET = 0 ]
        public uint mLapsCompleted;                             // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentLap;                                // [ RANGE = 0->... ]   [ UNSET = 0 ]
        public uint mCurrentSector;                             // [ enum (Type#4) Current Sector ]

    }

    [Serializable]
    public struct pCars2APIParticipantAdditionalDataStruct
    {
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mOrientations;      // [ UNITS = Euler Angles ]
         public float mSpeed;     
         [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
         public byte[] mCarNames; // [ string ]
                 [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
         public byte[] mCarClassNames; // [ string ]

    }

    [Serializable]
    public struct pCars2APIStruct
    {
         // Version Number
        public uint mVersion;                           // [ RANGE = 0->... ]
        public uint mBuildVersionNumber;                // [ RANGE = 0->... ]   [ UNSET = 0 ]

  // Game States
        public uint mGameState;                         // [ enum (Type#1) Game state ]
        public uint mSessionState;                      // [ enum (Type#2) Session state ]
        public uint mRaceState;                         // [ enum (Type#3) Race State ]

  // Participant Info
        public int mViewedParticipantIndex;                                  // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
        public int mNumParticipants;                                         // [ RANGE = 0->STORED_PARTICIPANTS_MAX ]   [ UNSET = -1 ]
  [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public pCars2APIParticipantStruct[] mParticipantData;    // [ struct (Type#13) ParticipantInfo struct ]

  // Unfiltered Input
  public float mUnfilteredThrottle;                        // [ RANGE = 0.0f->1.0f ]
  public float mUnfilteredBrake;                           // [ RANGE = 0.0f->1.0f ]
  public float mUnfilteredSteering;                        // [ RANGE = -1.0f->1.0f ]
  public float mUnfilteredClutch;                          // [ RANGE = 0.0f->1.0f ]

  // Vehicle information
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
  public byte[] mCarName;                 // [ string ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mCarClassName;            // [ string ]

  // Event information
        public uint mLapsInEvent;                        // [ RANGE = 0->... ]   [ UNSET = 0 ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTrackLocation;           // [ string ] - untranslated shortened English name
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTrackVariation;          // [ string ]- untranslated shortened English variation description
        public float mTrackLength;                               // [ UNITS = Metres ]   [ RANGE = 0.0f->... ]    [ UNSET = 0.0f ]

  // Timings - DO  NOT USE THESE
        public int mNumSectors;                                  // [ RANGE = 0->... ]   [ UNSET = -1 ]
        public bool mLapInvalidated;                             // [ UNITS = boolean ]   [ RANGE = false->true ]   [ UNSET = false ]
        public float mBestLapTime;                               // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mLastLapTime;                               // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mCurrentTime;                               // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mSplitTimeAhead;                            // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSplitTimeBehind;                           // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mSplitTime;                                 // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mEventTimeRemaining;                        // [ UNITS = milli-seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mPersonalFastestLapTime;                    // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mWorldFastestLapTime;                       // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector1Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector2Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mCurrentSector3Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mFastestSector1Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mFastestSector2Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        public float mFastestSector3Time;                        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
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
        public uint mCarFlags;                          // [ enum (Type#9) Car Flags ]
        public float mOilTempCelsius;                           // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public float mOilPressureKPa;                           // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mWaterTempCelsius;                         // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        public float mWaterPressureKPa;                         // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mFuelPressureKPa;                          // [ UNITS = Kilopascal ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
        public float mFuelLevel;                                // [ RANGE = 0.0f->1.0f ]
        public float mFuelCapacity;                             // [ UNITS = Liters ]   [ RANGE = 0.0f->1.0f ]   [ UNSET = 0.0f ]
        public float mSpeed;                                    // [ UNITS = Metres per-second ]   [ RANGE = 0.0f->... ]
        public float mRpm;                                      // [ UNITS = Revolutions per minute ]   [ RANGE = 0.0f->... ]   [ UNSET = 0.0f ]
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
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mOrientation;                     // [ UNITS = Euler Angles ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mLocalVelocity;                   // [ UNITS = Metres per-second ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mWorldVelocity;                   // [ UNITS = Metres per-second ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mAngularVelocity;                 // [ UNITS = Radians per-second ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mLocalAcceleration;               // [ UNITS = Metres per-second ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mWorldAcceleration;               // [ UNITS = Metres per-second ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] mExtentsCentre;                   // [ UNITS = Local Space  X  Y  Z ]

  // Wheels / Tyres
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] mTyreFlags;               // [ enum (Type#10) Tyre Flags ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public uint[] mTerrain;                 // [ enum (Type#11) Terrain Materials ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreY;                          // [ UNITS = Local Space  Y ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreRPS;                        // [ UNITS = Revolutions per second ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreSlipSpeed;                  // OBSOLETE, kept for backward compatibility only
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreTemp;                       // [ UNITS = Celsius ]   [ UNSET = 0.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreGrip;                       // OBSOLETE, kept for backward compatibility only
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreHeightAboveGround;          // [ UNITS = Local Space  Y ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float mTyreLateralStiffness;           // OBSOLETE, kept for backward compatibility only
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreWear;                       // [ RANGE = 0.0f->1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mBrakeDamage;                    // [ RANGE = 0.0f->1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mSuspensionDamage;               // [ RANGE = 0.0f->1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mBrakeTempCelsius;               // [ UNITS = Celsius ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreTreadTemp;                  // [ UNITS = Kelvin ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreLayerTemp;                  // [ UNITS = Kelvin ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreCarcassTemp;                // [ UNITS = Kelvin ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreRimTemp;                    // [ UNITS = Kelvin ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mTyreInternalAirTemp;            // [ UNITS = Kelvin ]

  // Car Damage
        public uint mCrashState;                        // [ enum (Type#12) Crash Damage State ]
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

  //PCars2 additions start, version 8
	// Sequence Number to help slightly with data integrity reads
        public uint mSequenceNumber;          // 0 at the start, incremented at start and end of writing, so odd when Shared Memory is being filled, even when the memory is not being touched

	//Additional car variables
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mWheelLocalPositionY;           // [ UNITS = Local Space  Y ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mSuspensionTravel;              // [ UNITS = meters ] [ RANGE 0.f =>... ]  [ UNSET =  0.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mSuspensionVelocity;            // [ UNITS = Rate of change of pushrod deflection ] [ RANGE 0.f =>... ]  [ UNSET =  0.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] mAirPressure;                   // [ UNITS = PSI ]  [ RANGE 0.f =>... ]  [ UNSET =  0.0f ]
        public float mEngineSpeed;                             // [ UNITS = Rad/s ] [UNSET = 0.f ]
        public float mEngineTorque;                            // [ UNITS = Newton Meters] [UNSET = 0.f ] [ RANGE = 0.0f->... ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 2)]
        public float[] mWings;                                // [ RANGE = 0.0f->1.0f ] [UNSET = 0.f ]
        public float mHandBrake;                               // [ RANGE = 0.0f->1.0f ] [UNSET = 0.f ]

	// additional race variables
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mCurrentSector1Times;        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mCurrentSector2Times;        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mCurrentSector3Times;        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mFastestSector1Times;        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mFastestSector2Times;        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mFastestSector3Times;        // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mFastestLapTimes;            // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public float[] mLastLapTimes;               // [ UNITS = seconds ]   [ RANGE = 0.0f->... ]   [ UNSET = -1.0f ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public bool[] mLapsInvalidated;            // [ UNITS = boolean for all participants ]   [ RANGE = false->true ]   [ UNSET = false ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public uint[] mRaceStates;         // [ enum (Type#3) Race State ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public uint[] mPitModes;           // [ enum (Type#7)  Pit Mode ]

        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
	    pCars2APIParticipantAdditionalDataStruct[] mAdditionalParticipantData;      // 

																											// additional race variables
        public int mEnforcedPitStopLap;                          // [ UNITS = in which lap there will be a mandatory pitstop] [ RANGE = 0.0f->... ] [ UNSET = -1 ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTranslatedTrackLocation;  // [ string ]
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public byte[] mTranslatedTrackVariation; // [ string ]]

        // extra from the UDP data
        public uint mSessionLengthTimeFromGame;  // seconds, 0 => not a timed session

        /*
        // extras from the UDP data - pcars1


        
        public float[] mWheelLocalPosition;

        public float[] mRideHeight;

        public float[] mSuspensionTravel;

        public float[] mSuspensionVelocity;

        public float[] mAirPressure;

        public float mEngineSpeed;

        public float mEngineTorque;

        public int mEnforcedPitStopLap;

        // TODO: front wing and rear wing 

        public Boolean hasNewPositionData;

        public Boolean[] isSameClassAsPlayer;

        public Boolean hasOpponentClassData;

        public float[] mLastSectorData;

        public Boolean[] mLapInvalidatedData;*/
    }
}