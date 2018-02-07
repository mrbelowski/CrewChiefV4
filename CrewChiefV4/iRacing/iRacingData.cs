using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using iRSDKSharp;
namespace CrewChiefV4.iRacing
{
    [Serializable]
    public class iRacingData
    {
        public iRacingData( iRacingSDK sdk, bool hasNewSessionData, bool isNewSession)
        {
            
            if(hasNewSessionData)
            {
                SessionInfo = new SessionInfo(sdk.GetSessionInfoString()).Yaml;
            }
            else
            {
                SessionInfo = "";
            }

            SessionInfoUpdate = sdk.Header.SessionInfoUpdate;
            IsNewSession = isNewSession;

            SessionTime = (System.Double)sdk.GetData("SessionTime");
            SessionTick = (System.Int32)sdk.GetData("SessionTick");
            SessionNum = (System.Int32)sdk.GetData("SessionNum");
            SessionState = (SessionStates)sdk.GetData("SessionState");
            SessionFlags = (int)sdk.GetData("SessionFlags");
            SessionTimeRemain = (System.Double)sdk.GetData("SessionTimeRemain");
            SessionLapsRemain = (System.Int32)sdk.GetData("SessionLapsRemain");
            SessionLapsRemainEx = (System.Int32)sdk.GetData("SessionLapsRemainEx");
            DisplayUnits = (DisplayUnits)sdk.GetData("DisplayUnits");
            DriverMarker = (System.Boolean)sdk.GetData("DriverMarker");
            PushToPass = (System.Boolean)sdk.GetData("PushToPass");
            IsOnTrack = (System.Boolean)sdk.GetData("IsOnTrack");
            PlayerCarPosition = (System.Int32)sdk.GetData("PlayerCarPosition");
            PlayerCarClassPosition = (System.Int32)sdk.GetData("PlayerCarClassPosition");
            PlayerTrackSurface = (TrackSurfaces)sdk.GetData("PlayerTrackSurface");
            PlayerTrackSurfaceMaterial = (TrackSurfaceMaterial)sdk.GetData("PlayerTrackSurfaceMaterial");
            PlayerCarIdx = (System.Int32)sdk.GetData("PlayerCarIdx");
            PlayerCarTeamIncidentCount = (System.Int32)sdk.GetData("PlayerCarTeamIncidentCount");
            PlayerCarMyIncidentCount = (System.Int32)sdk.GetData("PlayerCarMyIncidentCount");
            PlayerCarDriverIncidentCount = (System.Int32)sdk.GetData("PlayerCarDriverIncidentCount");
            CarIdxLap = (System.Int32[])sdk.GetData("CarIdxLap");
            CarIdxLapCompleted = (System.Int32[])sdk.GetData("CarIdxLapCompleted");
            CarIdxLapDistPct = (System.Single[])sdk.GetData("CarIdxLapDistPct");
            CarIdxTrackSurface = (TrackSurfaces[])sdk.GetData("CarIdxTrackSurface");
            CarIdxTrackSurfaceMaterial = (TrackSurfaceMaterial[])sdk.GetData("CarIdxTrackSurfaceMaterial");
            CarIdxOnPitRoad = (System.Boolean[])sdk.GetData("CarIdxOnPitRoad");
            CarIdxPosition = (System.Int32[])sdk.GetData("CarIdxPosition");
            CarIdxClassPosition = (System.Int32[])sdk.GetData("CarIdxClassPosition");
            CarIdxF2Time = (System.Single[])sdk.GetData("CarIdxF2Time");
            CarIdxEstTime = (System.Single[])sdk.GetData("CarIdxEstTime");
            OnPitRoad = (System.Boolean)sdk.GetData("OnPitRoad");
            CarIdxRPM = (System.Single[])sdk.GetData("CarIdxRPM");
            CarIdxGear = (System.Int32[])sdk.GetData("CarIdxGear");
            Throttle = (System.Single)sdk.GetData("Throttle");
            Brake = (System.Single)sdk.GetData("Brake");
            Clutch = (System.Single)sdk.GetData("Clutch");
            Gear = (System.Int32)sdk.GetData("Gear");
            RPM = (System.Single)sdk.GetData("RPM");
            Lap = (System.Int32)sdk.GetData("Lap");
            LapCompleted = (System.Int32)sdk.GetData("LapCompleted");
            LapDist = (System.Single)sdk.GetData("LapDist");
            LapDistPct = (System.Single)sdk.GetData("LapDistPct");
            RaceLaps = (System.Int32)sdk.GetData("RaceLaps");
            LapBestLap = (System.Int32)sdk.GetData("LapBestLap");
            LapBestLapTime = (System.Single)sdk.GetData("LapBestLapTime");
            LapLastLapTime = (System.Single)sdk.GetData("LapLastLapTime");
            LapCurrentLapTime = (System.Single)sdk.GetData("LapCurrentLapTime");
            TrackTemp = (System.Single)sdk.GetData("TrackTemp");
            TrackTempCrew = (System.Single)sdk.GetData("TrackTempCrew");
            AirTemp = (System.Single)sdk.GetData("AirTemp");
            WeatherType = (WeatherType)sdk.GetData("WeatherType");
            Skies = (Skies)sdk.GetData("Skies");
            AirDensity = (System.Single)sdk.GetData("AirDensity");
            AirPressure = (System.Single)sdk.GetData("AirPressure");
            WindVel = (System.Single)sdk.GetData("WindVel");
            WindDir = (System.Single)sdk.GetData("WindDir");
            RelativeHumidity = (System.Single)sdk.GetData("RelativeHumidity");
            CarLeftRight = (System.Int32)sdk.GetData("CarLeftRight");
            PitRepairLeft = (System.Single)sdk.GetData("PitRepairLeft");
            PitOptRepairLeft = (System.Single)sdk.GetData("PitOptRepairLeft");
            IsOnTrackCar = (System.Boolean)sdk.GetData("IsOnTrackCar");
            IsInGarage = (System.Boolean)sdk.GetData("IsInGarage");
            EngineWarnings = (EngineWarnings)(int)sdk.GetData("EngineWarnings");
            FuelLevel = (System.Single)sdk.GetData("FuelLevel");
            FuelLevelPct = (System.Single)sdk.GetData("FuelLevelPct");
            WaterTemp = (System.Single)sdk.GetData("WaterTemp");
            WaterLevel = (System.Single)sdk.GetData("WaterLevel");
            FuelPress = (System.Single)sdk.GetData("FuelPress");
            FuelUsePerHour = (System.Single)sdk.GetData("FuelUsePerHour");
            OilTemp = (System.Single)sdk.GetData("OilTemp");
            OilPress = (System.Single)sdk.GetData("OilPress");
            OilLevel = (System.Single)sdk.GetData("OilLevel");
            Speed = (System.Single)sdk.GetData("Speed");
            IsReplayPlaying = (System.Boolean)sdk.GetData("IsReplayPlaying");
            
            RRcoldPressure = (System.Single)sdk.GetData("RRcoldPressure");
            RRtempCL = (System.Single)sdk.GetData("RRtempCL");
            RRtempCM = (System.Single)sdk.GetData("RRtempCM");
            RRtempCR = (System.Single)sdk.GetData("RRtempCR");
            RRwearL = (System.Single)sdk.GetData("RRwearL");
            RRwearM = (System.Single)sdk.GetData("RRwearM");
            RRwearR = (System.Single)sdk.GetData("RRwearR");
            LRcoldPressure = (System.Single)sdk.GetData("LRcoldPressure");
            LRtempCL = (System.Single)sdk.GetData("LRtempCL");
            LRtempCM = (System.Single)sdk.GetData("LRtempCM");
            LRtempCR = (System.Single)sdk.GetData("LRtempCR");
            LRwearL = (System.Single)sdk.GetData("LRwearL");
            LRwearM = (System.Single)sdk.GetData("LRwearM");
            LRwearR = (System.Single)sdk.GetData("LRwearR");
            RFcoldPressure = (System.Single)sdk.GetData("RFcoldPressure");
            RFtempCL = (System.Single)sdk.GetData("RFtempCL");
            RFtempCM = (System.Single)sdk.GetData("RFtempCM");
            RFtempCR = (System.Single)sdk.GetData("RFtempCR");
            RFwearL = (System.Single)sdk.GetData("RFwearL");
            RFwearM = (System.Single)sdk.GetData("RFwearM");
            RFwearR = (System.Single)sdk.GetData("RFwearR");
            LFcoldPressure = (System.Single)sdk.GetData("LFcoldPressure");
            LFtempCL = (System.Single)sdk.GetData("LFtempCL");
            LFtempCM = (System.Single)sdk.GetData("LFtempCM");
            LFtempCR = (System.Single)sdk.GetData("LFtempCR");
            LFwearL = (System.Single)sdk.GetData("LFwearL");
            LFwearM = (System.Single)sdk.GetData("LFwearM");
            LFwearR = (System.Single)sdk.GetData("LFwearR");

            Pitch = (System.Single)sdk.GetData("Pitch");
            Yaw = (System.Single)sdk.GetData("Yaw");
            Roll = (System.Single)sdk.GetData("Roll");
            Voltage = (System.Single)sdk.GetData("Voltage");
        }
        public iRacingData()
        {

        }
        public System.Boolean IsNewSession;

        public System.Int32 SessionInfoUpdate;

        public System.String SessionInfo;
        /// <summary>
        /// Seconds since session start
        /// </summary>
        public System.Double 	SessionTime;

        /// <summary>
        /// Current update number
        /// </summary>
        public System.Int32 	SessionTick;

        /// <summary>
        /// Session number
        /// </summary>
        public System.Int32 	SessionNum;

        /// <summary>
        /// Session state
        /// </summary>
        public SessionStates 	SessionState;

        /// <summary>
        /// Session flags
        /// </summary>
        public int 	SessionFlags;

        /// <summary>
        /// Seconds left till session ends
        /// </summary>
        public System.Double 	SessionTimeRemain;

        /// <summary>
        /// Old laps left till session ends use SessionLapsRemainEx
        /// </summary>
        public System.Int32 	SessionLapsRemain;

        /// <summary>
        /// New improved laps left till session ends
        /// </summary>
        public System.Int32 	SessionLapsRemainEx;

        /// <summary>
        /// Default units for the user interface 0 = english 1 = metric
        /// </summary>
        public DisplayUnits 	DisplayUnits;

        /// <summary>
        /// Driver activated flag
        /// </summary>
        public System.Boolean 	DriverMarker;

        /// <summary>
        /// Push to pass button state
        /// </summary>
        public System.Boolean 	PushToPass;

        /// <summary>
        /// 1=Car on track physics running with player in car
        /// </summary>
        public System.Boolean 	IsOnTrack;

        /// <summary>
        /// Players position in race
        /// </summary>
        public System.Int32 	PlayerCarPosition;

        /// <summary>
        /// Players class position in race
        /// </summary>
        public System.Int32 	PlayerCarClassPosition;

        /// <summary>
        /// Players car track surface type
        /// </summary>
        public TrackSurfaces PlayerTrackSurface;

        /// <summary>
        /// Players car track surface material type
        /// </summary>
        public TrackSurfaceMaterial PlayerTrackSurfaceMaterial;

        /// <summary>
        /// Players carIdx
        /// </summary>
        public System.Int32 	PlayerCarIdx;

        /// <summary>
        /// Players team incident count for this session
        /// </summary>
        public System.Int32 	PlayerCarTeamIncidentCount;

        /// <summary>
        /// Players own incident count for this session
        /// </summary>
        public System.Int32 	PlayerCarMyIncidentCount;

        /// <summary>
        /// Teams current drivers incident count for this session
        /// </summary>
        public System.Int32 	PlayerCarDriverIncidentCount;

        /// <summary>
        /// Laps started by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Int32[] 	CarIdxLap;

        /// <summary>
        /// Laps completed by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Int32[] 	CarIdxLapCompleted;

        /// <summary>
        /// Percentage distance around lap by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Single[] 	CarIdxLapDistPct;

        /// <summary>
        /// Track surface type by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public TrackSurfaces[] 	CarIdxTrackSurface;

        /// <summary>
        /// Track surface material type by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public TrackSurfaceMaterial[] CarIdxTrackSurfaceMaterial;

        /// <summary>
        /// On pit road between the cones by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Boolean[] 	CarIdxOnPitRoad;

        /// <summary>
        /// Cars position in race by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Int32[] 	CarIdxPosition;

        /// <summary>
        /// Cars class position in race by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Int32[] 	CarIdxClassPosition;

        /// <summary>
        /// Race time behind leader or fastest lap time otherwise
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Single[] 	CarIdxF2Time;

        /// <summary>
        /// Estimated time to reach current location on track
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Single[] 	CarIdxEstTime;

        /// <summary>
        /// Is the player car on pit road between the cones
        /// </summary>
        public System.Boolean 	OnPitRoad;


        /// <summary>
        /// Engine rpm by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Single[] 	CarIdxRPM;

        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear by car index
        /// </summary>
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 64)]
        public System.Int32[] 	CarIdxGear;

        /// <summary>
        /// 0=off throttle to 1=full throttle
        /// </summary>
        public System.Single 	Throttle;

        /// <summary>
        /// 0=brake released to 1=max pedal force
        /// </summary>
        public System.Single 	Brake;

        /// <summary>
        /// 0=disengaged to 1=fully engaged
        /// </summary>
        public System.Single 	Clutch;

        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear
        /// </summary>
        public System.Int32 	Gear;

        /// <summary>
        /// Engine rpm
        /// </summary>
        public System.Single 	RPM;

        /// <summary>
        /// Laps started count
        /// </summary>
        public System.Int32 	Lap;

        /// <summary>
        /// Laps completed count
        /// </summary>
        public System.Int32 	LapCompleted;

        /// <summary>
        /// Meters traveled from S/F this lap
        /// </summary>
        public System.Single 	LapDist;

        /// <summary>
        /// Percentage distance around lap
        /// </summary>
        public System.Single 	LapDistPct;

        /// <summary>
        /// Laps completed in race
        /// </summary>
        public System.Int32 	RaceLaps;

        /// <summary>
        /// Players best lap number
        /// </summary>
        public System.Int32 	LapBestLap;

        /// <summary>
        /// Players best lap time
        /// </summary>
        public System.Single 	LapBestLapTime;

        /// <summary>
        /// Players last lap time
        /// </summary>
        public System.Single 	LapLastLapTime;

        /// <summary>
        /// Estimate of players current lap time as shown in F3 box
        /// </summary>
        public System.Single 	LapCurrentLapTime;

        /// <summary>
        /// Temperature of track at start/finish line
        /// </summary>
        public System.Single 	TrackTemp;

        /// <summary>
        /// Temperature of track measured by crew around track
        /// </summary>
        public System.Single TrackTempCrew;

        /// <summary>
        /// Temperature of air at start/finish line
        /// </summary>
        public System.Single AirTemp;

        /// <summary>
        /// Weather type (0=constant  1=dynamic)
        /// </summary>
        public WeatherType WeatherType;

        /// <summary>
        /// Skies (0=clear/1=p cloudy/2=m cloudy/3=overcast)
        /// </summary>
        public Skies Skies;

        /// <summary>
        /// Density of air at start/finish line
        /// </summary>
        public System.Single AirDensity;

        /// <summary>
        /// Pressure of air at start/finish line
        /// </summary>
        public System.Single AirPressure;

        /// <summary>
        /// Wind velocity at start/finish line
        /// </summary>
        public System.Single WindVel;

        /// <summary>
        /// Wind direction at start/finish line
        /// </summary>
        public System.Single WindDir;

        /// <summary>
        /// Relative Humidity
        /// </summary>
        public System.Single RelativeHumidity;
        /// <summary>
        /// Notify if car is to the left or right of driver
        /// </summary>
        public System.Int32 CarLeftRight;

        /// <summary>
        /// Time left for mandatory pit repairs if repairs are active
        /// </summary>
        public System.Single PitRepairLeft;

        /// <summary>
        /// Time left for optional repairs if repairs are active
        /// </summary>
        public System.Single PitOptRepairLeft;

        /// <summary>
        /// 1=Car on track physics running
        /// </summary>
        public System.Boolean IsOnTrackCar;

        /// <summary>
        /// 1=Car in garage physics running
        /// </summary>
        public System.Boolean IsInGarage;

        /// <summary>
        /// Bitfield for warning lights
        /// </summary>
        public EngineWarnings 	EngineWarnings;

        /// <summary>
        /// Liters of fuel remaining
        /// </summary>
        public System.Single FuelLevel;

        /// <summary>
        /// Percent fuel remaining
        /// </summary>
        public System.Single FuelLevelPct;

        /// <summary>
        /// Engine coolant temp
        /// </summary>
        public System.Single WaterTemp;

        /// <summary>
        /// Engine coolant level
        /// </summary>
        public System.Single WaterLevel;

        /// <summary>
        /// Engine fuel pressure
        /// </summary>
        public System.Single FuelPress;

        /// <summary>
        /// Engine fuel used instantaneous
        /// </summary>
        public System.Single FuelUsePerHour;

        /// <summary>
        /// Engine oil temperature
        /// </summary>
        public System.Single OilTemp;

        /// <summary>
        /// Engine oil pressure
        /// </summary>
        public System.Single OilPress;

        /// <summary>
        /// Engine oil level
        /// </summary>
        public System.Single OilLevel;

        public System.Single Speed;

        public System.Boolean IsReplayPlaying;

        /// <summary>
        /// RR tire cold pressure  as set in the garage
        /// </summary>
        public System.Single RRcoldPressure;

        /// <summary>
        /// RR tire left carcass temperature
        /// </summary>
        public System.Single RRtempCL;

        /// <summary>
        /// RR tire middle carcass temperature
        /// </summary>
        public System.Single RRtempCM;

        /// <summary>
        /// RR tire right carcass temperature
        /// </summary>
        public System.Single RRtempCR;

        /// <summary>
        /// RR tire left percent tread remaining
        /// </summary>
        public System.Single RRwearL;

        /// <summary>
        /// RR tire middle percent tread remaining
        /// </summary>
        public System.Single RRwearM;

        /// <summary>
        /// RR tire right percent tread remaining
        /// </summary>
        public System.Single RRwearR;

        /// <summary>
        /// LR tire cold pressure  as set in the garage
        /// </summary>
        public System.Single LRcoldPressure;

        /// <summary>
        /// LR tire left carcass temperature
        /// </summary>
        public System.Single LRtempCL;

        /// <summary>
        /// LR tire middle carcass temperature
        /// </summary>
        public System.Single LRtempCM;

        /// <summary>
        /// LR tire right carcass temperature
        /// </summary>
        public System.Single LRtempCR;

        /// <summary>
        /// LR tire left percent tread remaining
        /// </summary>
        public System.Single LRwearL;

        /// <summary>
        /// LR tire middle percent tread remaining
        /// </summary>
        public System.Single LRwearM;

        /// <summary>
        /// LR tire right percent tread remaining
        /// </summary>
        public System.Single LRwearR;

        /// <summary>
        /// RF tire cold pressure  as set in the garage
        /// </summary>
        public System.Single RFcoldPressure;

        /// <summary>
        /// RF tire left carcass temperature
        /// </summary>
        public System.Single RFtempCL;

        /// <summary>
        /// RF tire middle carcass temperature
        /// </summary>
        public System.Single RFtempCM;

        /// <summary>
        /// RF tire right carcass temperature
        /// </summary>
        public System.Single RFtempCR;

        /// <summary>
        /// RF tire left percent tread remaining
        /// </summary>
        public System.Single RFwearL;

        /// <summary>
        /// RF tire middle percent tread remaining
        /// </summary>
        public System.Single RFwearM;

        /// <summary>
        /// RF tire right percent tread remaining
        /// </summary>
        public System.Single RFwearR;

        /// <summary>
        /// LF tire cold pressure  as set in the garage
        /// </summary>
        public System.Single LFcoldPressure;

        /// <summary>
        /// LF tire left carcass temperature
        /// </summary>
        public System.Single LFtempCL;

        /// <summary>
        /// LF tire middle carcass temperature
        /// </summary>
        public System.Single LFtempCM;

        /// <summary>
        /// LF tire right carcass temperature
        /// </summary>
        public System.Single LFtempCR;

        /// <summary>
        /// LF tire left percent tread remaining
        /// </summary>
        public System.Single LFwearL;

        /// <summary>
        /// LF tire middle percent tread remaining
        /// </summary>
        public System.Single LFwearM;

        /// <summary>
        /// LF tire right percent tread remaining
        /// </summary>
        public System.Single LFwearR;
        
        /// <summary>
        /// Yaw orientation. Unit: rad
        /// </summary>
        public System.Single Yaw;


        /// <summary>
        /// Pitch orientation. Unit: rad
        /// </summary>
        public System.Single Pitch;


        /// <summary>
        /// Roll orientation. Unit: rad
        /// </summary>
        public System.Single Roll;

        /// <summary>
        /// Engine voltage. Unit: V
        /// </summary>
        public System.Single Voltage;

    }
}
