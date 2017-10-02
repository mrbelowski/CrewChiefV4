using System;
using System.Collections.Generic;
using iRSDKSharp;
using System.Linq;

namespace CrewChiefV4.iRacing
{
    /// <summary>
    /// Represents an object from which you can get Telemetry var headers by name
    /// </summary>
    public sealed class TelemetryInfo
    {
        private readonly iRacingSDK sdk;

        public TelemetryInfo(iRacingSDK sdk)
        {
            this.sdk = sdk;
        }

        public IEnumerable<TelemetryValue> GetValues()
        {
            var values = new List<TelemetryValue>();
            values.AddRange(new TelemetryValue[]
                                {
                                    this.SessionTime,
                                    this.SessionNum,
                                    this.SessionState,
                                    this.SessionUniqueID,
                                    this.SessionFlags,
                                    this.DriverMarker,
                                    this.IsReplayPlaying,
                                    this.ReplayFrameNum,
                                    this.CarIdxLap,
                                    this.CarIdxLapCompleted,
                                    this.CarIdxLapDistPct,
                                    this.CarIdxTrackSurface,
                                    this.CarIdxSteer,
                                    this.CarIdxRPM,
                                    this.CarIdxGear,
                                    this.CarIdxF2Time,
                                    this.CarIdxEstTime,
                                    this.CarIdxOnPitRoad,
                                    this.CarIdxPosition,
                                    this.CarIdxClassPosition,
                                    this.SteeringWheelAngle,
                                    this.Throttle,
                                    this.Brake,
                                    this.Clutch,
                                    this.Gear,
                                    this.RPM,
                                    this.Lap,
                                    this.LapDist,
                                    this.LapDistPct,
                                    this.RaceLaps,
                                    this.LongAccel,
                                    this.LatAccel,
                                    this.VertAccel,
                                    this.RollRate,
                                    this.PitchRate,
                                    this.YawRate,
                                    this.Speed,
                                    this.VelocityX,
                                    this.VelocityY,
                                    this.VelocityZ,
                                    this.Yaw,
                                    this.Pitch,
                                    this.Roll,
                                    this.CamCarIdx,
                                    this.CamCameraNumber,
                                    this.CamCameraState,
                                    this.CamGroupNumber,
                                    this.IsOnTrack,
                                    this.IsInGarage,
                                    this.SteeringWheelTorque,
                                    this.SteeringWheelPctTorque,
                                    this.ShiftIndicatorPct,
                                    this.EngineWarnings,
                                    this.FuelLevel,
                                    this.FuelLevelPct,
                                    this.ReplayPlaySpeed,
                                    this.ReplaySessionTime,
                                    this.ReplaySessionNum,
                                    this.WaterTemp,
                                    this.WaterLevel,
                                    this.FuelPress,
                                    this.OilTemp,
                                    this.OilPress,
                                    this.OilLevel,
                                    this.Voltage,
                                    this.SessionTimeRemain,
                                    this.ReplayFrameNumEnd,
                                    this.AirDensity,
                                    this.AirPressure,
                                    this.AirTemp,
                                    this.FogLevel,
                                    this.Skies,
                                    this.TrackTemp,
                                    this.TrackTempCrew,
                                    this.RelativeHumidity,
                                    this.WeatherType,
                                    this.WindDir,
                                    this.WindVel,
                                    this.MGUKDeployAdapt,
                                    this.MGUKDeployFixed,
                                    this.MGUKRegenGain,
                                    this.EnergyBatteryToMGU,
                                    this.EnergyBudgetBattToMGU,
                                    this.EnergyERSBattery,
                                    this.PowerMGUH,
                                    this.PowerMGUK,
                                    this.TorqueMGUK,
                                    this.DrsStatus,
                                    this.LapCompleted,
                                    this.PlayerCarDriverIncidentCount,
                                    this.PlayerCarTeamIncidentCount,
                                    this.PlayerCarMyIncidentCount,
                                    this.PlayerTrackSurface,
                                    this.PlayerCarIdx,
                                    this.CarLeftRight
                                });
            return values;
        }

        public TelemetryValue<float> MGUKDeployAdapt { get { return new TelemetryValue<float>(sdk, "dcMGUKDeployAdapt"); } }

        public TelemetryValue<float> MGUKDeployFixed { get { return new TelemetryValue<float>(sdk, "dcMGUKDeployFixed"); } }

        public TelemetryValue<float> MGUKRegenGain { get { return new TelemetryValue<float>(sdk, "dcMGUKRegenGain"); } }

        public TelemetryValue<float> EnergyBatteryToMGU { get { return new TelemetryValue<float>(sdk, "EnergyBatteryToMGU_KLap"); } }

        public TelemetryValue<float> EnergyBudgetBattToMGU { get { return new TelemetryValue<float>(sdk, "EnergyBudgetBattToMGU_KLap"); } }

        public TelemetryValue<float> EnergyERSBattery { get { return new TelemetryValue<float>(sdk, "EnergyERSBattery"); } }

        public TelemetryValue<float> PowerMGUH { get { return new TelemetryValue<float>(sdk, "PowerMGU_H"); } }

        public TelemetryValue<float> PowerMGUK { get { return new TelemetryValue<float>(sdk, "PowerMGU_K"); } }

        public TelemetryValue<float> TorqueMGUK { get { return new TelemetryValue<float>(sdk, "TorqueMGU_K"); } }

        /// <summary>
        /// Current DRS status. 0 = inactive, 1 = can be activated in next DRS zone, 2 = can be activated now, 3 = active.
        /// </summary>
        public TelemetryValue<int> DrsStatus { get { return new TelemetryValue<int>(sdk, "DRS_Status"); } }

        /// <summary>
        /// The number of laps you have completed. Note: on Nordschleife Tourist layout, you can complete a lap without starting a new one!
        /// </summary>
        public TelemetryValue<int> LapCompleted { get { return new TelemetryValue<int>(sdk, "LapCompleted"); } }


        /// <summary>
        /// Seconds since session start. Unit: s
        /// </summary>
        public TelemetryValue<double> SessionTime { get { return new TelemetryValue<double>(sdk, "SessionTime"); } }


        /// <summary>
        /// Session number. 
        /// </summary>
        public TelemetryValue<int> SessionNum { get { return new TelemetryValue<int>(sdk, "SessionNum"); } }


        /// <summary>
        /// Session state. Unit: irsdk_SessionState
        /// </summary>
        public TelemetryValue<SessionStates> SessionState { get { return new TelemetryValue<SessionStates>(sdk, "SessionState"); } }


        /// <summary>
        /// Session ID. 
        /// </summary>
        public TelemetryValue<int> SessionUniqueID { get { return new TelemetryValue<int>(sdk, "SessionUniqueID"); } }


        /// <summary>
        /// Session flags. Unit: irsdk_Flags
        /// </summary>
        public TelemetryValue<SessionFlag> SessionFlags { get { return new TelemetryValue<SessionFlag>(sdk, "SessionFlags"); } }


        /// <summary>
        /// Driver activated flag. 
        /// </summary>
        public TelemetryValue<bool> DriverMarker { get { return new TelemetryValue<bool>(sdk, "DriverMarker"); } }


        /// <summary>
        /// 0=replay not playing  1=replay playing. 
        /// </summary>
        public TelemetryValue<bool> IsReplayPlaying { get { return new TelemetryValue<bool>(sdk, "IsReplayPlaying"); } }


        /// <summary>
        /// Integer replay frame number (60 per second). 
        /// </summary>
        public TelemetryValue<int> ReplayFrameNum { get { return new TelemetryValue<int>(sdk, "ReplayFrameNum"); } }


        /// <summary>
        /// Current lap number by car index
        /// </summary>
        public TelemetryValue<int[]> CarIdxLap { get { return new TelemetryValue<int[]>(sdk, "CarIdxLap"); } }

        /// <summary>
        /// Current number of completed laps by car index. Note: On Nordschleife Tourist layout, cars can complete a lap without starting a new lap!
        /// </summary>
        public TelemetryValue<int[]> CarIdxLapCompleted { get { return new TelemetryValue<int[]>(sdk, "CarIdxLapCompleted"); } }

        /// <summary>
        /// Percentage distance around lap by car index. Unit: %
        /// </summary>
        public TelemetryValue<float[]> CarIdxLapDistPct { get { return new TelemetryValue<float[]>(sdk, "CarIdxLapDistPct"); } }


        /// <summary>
        /// Track surface type by car index. Unit: irsdk_TrkLoc
        /// </summary>
        public TelemetryValue<TrackSurfaces[]> CarIdxTrackSurface { get { return new TelemetryValue<TrackSurfaces[]>(sdk, "CarIdxTrackSurface"); } }


        /// <summary>
        /// Steering wheel angle by car index. Unit: rad
        /// </summary>
        public TelemetryValue<float[]> CarIdxSteer { get { return new TelemetryValue<float[]>(sdk, "CarIdxSteer"); } }


        /// <summary>
        /// Engine rpm by car index. Unit: revs/min
        /// </summary>
        public TelemetryValue<float[]> CarIdxRPM { get { return new TelemetryValue<float[]>(sdk, "CarIdxRPM"); } }


        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear by car index. 
        /// </summary>
        public TelemetryValue<int[]> CarIdxGear { get { return new TelemetryValue<int[]>(sdk, "CarIdxGear"); } }

        public TelemetryValue<float[]> CarIdxF2Time { get { return new TelemetryValue<float[]>(sdk, "CarIdxF2Time"); } }

        public TelemetryValue<float[]> CarIdxEstTime { get { return new TelemetryValue<float[]>(sdk, "CarIdxEstTime"); } }

        public TelemetryValue<bool[]> CarIdxOnPitRoad { get { return new TelemetryValue<bool[]>(sdk, "CarIdxOnPitRoad"); } }

        public TelemetryValue<int[]> CarIdxPosition { get { return new TelemetryValue<int[]>(sdk, "CarIdxPosition"); } }

        public TelemetryValue<int[]> CarIdxClassPosition { get { return new TelemetryValue<int[]>(sdk, "CarIdxClassPosition"); } }


        /// <summary>
        /// Steering wheel angle. Unit: rad
        /// </summary>
        public TelemetryValue<float> SteeringWheelAngle { get { return new TelemetryValue<float>(sdk, "SteeringWheelAngle"); } }


        /// <summary>
        /// 0=off throttle to 1=full throttle. Unit: %
        /// </summary>
        public TelemetryValue<float> Throttle { get { return new TelemetryValue<float>(sdk, "Throttle"); } }


        /// <summary>
        /// 0=brake released to 1=max pedal force. Unit: %
        /// </summary>
        public TelemetryValue<float> Brake { get { return new TelemetryValue<float>(sdk, "Brake"); } }


        /// <summary>
        /// 0=disengaged to 1=fully engaged. Unit: %
        /// </summary>
        public TelemetryValue<float> Clutch { get { return new TelemetryValue<float>(sdk, "Clutch"); } }


        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear. 
        /// </summary>
        public TelemetryValue<int> Gear { get { return new TelemetryValue<int>(sdk, "Gear"); } }


        /// <summary>
        /// Engine rpm. Unit: revs/min
        /// </summary>
        public TelemetryValue<float> RPM { get { return new TelemetryValue<float>(sdk, "RPM"); } }


        /// <summary>
        /// Lap count. 
        /// </summary>
        public TelemetryValue<int> Lap { get { return new TelemetryValue<int>(sdk, "Lap"); } }


        /// <summary>
        /// Meters traveled from S/F this lap. Unit: m
        /// </summary>
        public TelemetryValue<float> LapDist { get { return new TelemetryValue<float>(sdk, "LapDist"); } }


        /// <summary>
        /// Percentage distance around lap. Unit: %
        /// </summary>
        public TelemetryValue<float> LapDistPct { get { return new TelemetryValue<float>(sdk, "LapDistPct"); } }


        /// <summary>
        /// Laps completed in race. 
        /// </summary>
        public TelemetryValue<int> RaceLaps { get { return new TelemetryValue<int>(sdk, "RaceLaps"); } }


        /// <summary>
        /// Longitudinal acceleration (including gravity). Unit: m/s^2
        /// </summary>
        public TelemetryValue<float> LongAccel { get { return new TelemetryValue<float>(sdk, "LongAccel"); } }


        /// <summary>
        /// Lateral acceleration (including gravity). Unit: m/s^2
        /// </summary>
        public TelemetryValue<float> LatAccel { get { return new TelemetryValue<float>(sdk, "LatAccel"); } }


        /// <summary>
        /// Vertical acceleration (including gravity). Unit: m/s^2
        /// </summary>
        public TelemetryValue<float> VertAccel { get { return new TelemetryValue<float>(sdk, "VertAccel"); } }


        /// <summary>
        /// Roll rate. Unit: rad/s
        /// </summary>
        public TelemetryValue<float> RollRate { get { return new TelemetryValue<float>(sdk, "RollRate"); } }


        /// <summary>
        /// Pitch rate. Unit: rad/s
        /// </summary>
        public TelemetryValue<float> PitchRate { get { return new TelemetryValue<float>(sdk, "PitchRate"); } }


        /// <summary>
        /// Yaw rate. Unit: rad/s
        /// </summary>
        public TelemetryValue<float> YawRate { get { return new TelemetryValue<float>(sdk, "YawRate"); } }


        /// <summary>
        /// GPS vehicle speed. Unit: m/s
        /// </summary>
        public TelemetryValue<float> Speed { get { return new TelemetryValue<float>(sdk, "Speed"); } }


        /// <summary>
        /// X velocity. Unit: m/s
        /// </summary>
        public TelemetryValue<float> VelocityX { get { return new TelemetryValue<float>(sdk, "VelocityX"); } }


        /// <summary>
        /// Y velocity. Unit: m/s
        /// </summary>
        public TelemetryValue<float> VelocityY { get { return new TelemetryValue<float>(sdk, "VelocityY"); } }


        /// <summary>
        /// Z velocity. Unit: m/s
        /// </summary>
        public TelemetryValue<float> VelocityZ { get { return new TelemetryValue<float>(sdk, "VelocityZ"); } }


        /// <summary>
        /// Yaw orientation. Unit: rad
        /// </summary>
        public TelemetryValue<float> Yaw { get { return new TelemetryValue<float>(sdk, "Yaw"); } }


        /// <summary>
        /// Pitch orientation. Unit: rad
        /// </summary>
        public TelemetryValue<float> Pitch { get { return new TelemetryValue<float>(sdk, "Pitch"); } }


        /// <summary>
        /// Roll orientation. Unit: rad
        /// </summary>
        public TelemetryValue<float> Roll { get { return new TelemetryValue<float>(sdk, "Roll"); } }


        /// <summary>
        /// Active camera's focus car index. 
        /// </summary>
        public TelemetryValue<int> CamCarIdx { get { return new TelemetryValue<int>(sdk, "CamCarIdx"); } }


        /// <summary>
        /// Active camera number. 
        /// </summary>
        public TelemetryValue<int> CamCameraNumber { get { return new TelemetryValue<int>(sdk, "CamCameraNumber"); } }


        /// <summary>
        /// Active camera group number. 
        /// </summary>
        public TelemetryValue<int> CamGroupNumber { get { return new TelemetryValue<int>(sdk, "CamGroupNumber"); } }


        /// <summary>
        /// State of camera system. Unit: irsdk_CameraState
        /// </summary>
        public TelemetryValue<CameraState> CamCameraState { get { return new TelemetryValue<CameraState>(sdk, "CamCameraState"); } }


        /// <summary>
        /// 1=Car on track physics running. 
        /// </summary>
        public TelemetryValue<bool> IsOnTrack { get { return new TelemetryValue<bool>(sdk, "IsOnTrack"); } }


        /// <summary>
        /// 1=Car in garage physics running. 
        /// </summary>
        public TelemetryValue<bool> IsInGarage { get { return new TelemetryValue<bool>(sdk, "IsInGarage"); } }


        /// <summary>
        /// Output torque on steering shaft. Unit: N*m
        /// </summary>
        public TelemetryValue<float> SteeringWheelTorque { get { return new TelemetryValue<float>(sdk, "SteeringWheelTorque"); } }


        /// <summary>
        /// Force feedback % max torque on steering shaft. Unit: %
        /// </summary>
        public TelemetryValue<float> SteeringWheelPctTorque { get { return new TelemetryValue<float>(sdk, "SteeringWheelPctTorque"); } }


        /// <summary>
        /// Percent of shift indicator to light up. Unit: %
        /// </summary>
        public TelemetryValue<float> ShiftIndicatorPct { get { return new TelemetryValue<float>(sdk, "ShiftIndicatorPct"); } }


        /// <summary>
        /// Bitfield for warning lights. Unit: irsdk_EngineWarnings
        /// </summary>
        public TelemetryValue<EngineWarning> EngineWarnings { get { return new TelemetryValue<EngineWarning>(sdk, "EngineWarnings"); } }


        /// <summary>
        /// Liters of fuel remaining. Unit: l
        /// </summary>
        public TelemetryValue<float> FuelLevel { get { return new TelemetryValue<float>(sdk, "FuelLevel"); } }


        /// <summary>
        /// Percent fuel remaining. Unit: %
        /// </summary>
        public TelemetryValue<float> FuelLevelPct { get { return new TelemetryValue<float>(sdk, "FuelLevelPct"); } }


        /// <summary>
        /// Replay playback speed. 
        /// </summary>
        public TelemetryValue<int> ReplayPlaySpeed { get { return new TelemetryValue<int>(sdk, "ReplayPlaySpeed"); } }


        /// <summary>
        /// 0=not slow motion  1=replay is in slow motion. 
        /// </summary>
        public TelemetryValue<bool> ReplayPlaySlowMotion { get { return new TelemetryValue<bool>(sdk, "ReplayPlaySlowMotion"); } }


        /// <summary>
        /// Seconds since replay session start. Unit: s
        /// </summary>
        public TelemetryValue<double> ReplaySessionTime { get { return new TelemetryValue<double>(sdk, "ReplaySessionTime"); } }


        /// <summary>
        /// Replay session number. 
        /// </summary>
        public TelemetryValue<int> ReplaySessionNum { get { return new TelemetryValue<int>(sdk, "ReplaySessionNum"); } }


        /// <summary>
        /// Engine coolant temp. Unit: C
        /// </summary>
        public TelemetryValue<float> WaterTemp { get { return new TelemetryValue<float>(sdk, "WaterTemp"); } }


        /// <summary>
        /// Engine coolant level. Unit: l
        /// </summary>
        public TelemetryValue<float> WaterLevel { get { return new TelemetryValue<float>(sdk, "WaterLevel"); } }


        /// <summary>
        /// Engine fuel pressure. Unit: bar
        /// </summary>
        public TelemetryValue<float> FuelPress { get { return new TelemetryValue<float>(sdk, "FuelPress"); } }


        /// <summary>
        /// Engine oil temperature. Unit: C
        /// </summary>
        public TelemetryValue<float> OilTemp { get { return new TelemetryValue<float>(sdk, "OilTemp"); } }


        /// <summary>
        /// Engine oil pressure. Unit: bar
        /// </summary>
        public TelemetryValue<float> OilPress { get { return new TelemetryValue<float>(sdk, "OilPress"); } }


        /// <summary>
        /// Engine oil level. Unit: l
        /// </summary>
        public TelemetryValue<float> OilLevel { get { return new TelemetryValue<float>(sdk, "OilLevel"); } }


        /// <summary>
        /// Engine voltage. Unit: V
        /// </summary>
        public TelemetryValue<float> Voltage { get { return new TelemetryValue<float>(sdk, "Voltage"); } }

        public TelemetryValue<double> SessionTimeRemain { get { return new TelemetryValue<double>(sdk, "SessionTimeRemain"); } }

        public TelemetryValue<int> ReplayFrameNumEnd { get { return new TelemetryValue<int>(sdk, "ReplayFrameNumEnd"); } }

        public TelemetryValue<float> AirDensity { get { return new TelemetryValue<float>(sdk, "AirDensity"); } }

        public TelemetryValue<float> AirPressure { get { return new TelemetryValue<float>(sdk, "AirPressure"); } }

        public TelemetryValue<float> AirTemp { get { return new TelemetryValue<float>(sdk, "AirTemp"); } }

        public TelemetryValue<float> FogLevel { get { return new TelemetryValue<float>(sdk, "FogLevel"); } }

        public TelemetryValue<int> Skies { get { return new TelemetryValue<int>(sdk, "Skies"); } }

        public TelemetryValue<float> TrackTemp { get { return new TelemetryValue<float>(sdk, "TrackTemp"); } }

        public TelemetryValue<float> TrackTempCrew { get { return new TelemetryValue<float>(sdk, "TrackTempCrew"); } }

        public TelemetryValue<float> RelativeHumidity { get { return new TelemetryValue<float>(sdk, "RelativeHumidity"); } }

        public TelemetryValue<int> WeatherType { get { return new TelemetryValue<int>(sdk, "WeatherType"); } }

        public TelemetryValue<float> WindDir { get { return new TelemetryValue<float>(sdk, "WindDir"); } }

        public TelemetryValue<float> WindVel { get { return new TelemetryValue<float>(sdk, "WindVel"); } }

        public TelemetryValue<int> PlayerCarTeamIncidentCount { get { return new TelemetryValue<int>(sdk, "PlayerCarTeamIncidentCount"); } }

        public TelemetryValue<int> PlayerCarMyIncidentCount { get { return new TelemetryValue<int>(sdk, "PlayerCarMyIncidentCount"); } }

        public TelemetryValue<int> PlayerCarDriverIncidentCount { get { return new TelemetryValue<int>(sdk, "PlayerCarDriverIncidentCount"); } }

        public TelemetryValue<TrackSurfaces> PlayerTrackSurface { get { return new TelemetryValue<TrackSurfaces>(sdk, "PlayerTrackSurface"); } }

        public TelemetryValue<int> PlayerCarIdx { get { return new TelemetryValue<int>(sdk, "PlayerCarIdx"); } }

        public TelemetryValue<int> CarLeftRight { get { return new TelemetryValue<int>(sdk, "CarLeftRight"); } }

        public TelemetryValue<int> PlayerCarPosition { get { return new TelemetryValue<int>(sdk, "PlayerCarPosition"); } }
        /// <summary>
        /// Players class position in race
        /// </summary>
        public TelemetryValue<int> PlayerCarClassPosition { get { return new TelemetryValue<int>(sdk, "PlayerCarClassPosition"); } }
        /// <summary>
        /// Players car track surface material type
        /// </summary>
        public TelemetryValue<int> PlayerTrackSurfaceMaterial { get { return new TelemetryValue<int>(sdk, "PlayerTrackSurfaceMaterial"); } }
        /// <summary>
        /// Estimate of players current lap time as shown in F3 box
        /// </summary>
        public TelemetryValue<System.Single> LapCurrentLapTime { get { return new TelemetryValue<System.Single>(sdk, "LapCurrentLapTime"); } }
        /// <summary>
        /// Players last lap time
        /// </summary>
        public TelemetryValue<System.Single> LapLastLapTime { get { return new TelemetryValue<System.Single>(sdk, "LapLastLapTime"); } }
        
        float[] carIdxDistance;
        public float[] CarIdxDistance
        {
            get
            {
                if (carIdxDistance == null)
                    carIdxDistance = Enumerable.Range(0, 64)
                        .Select(CarIdx => this.CarIdxLap.Value[CarIdx] + this.CarIdxLapDistPct.Value[CarIdx])
                        .ToArray();

                return carIdxDistance;
            }
            internal set
            {
                carIdxDistance = value;
            }
        }

        int[] positions;
        public int[] Positions
        {
            get
            {
                if (positions != null)
                    return positions;

                positions = new int[64];

                var runningOrder = CarIdxLapDistPct.Value.Select((d, idx) => new { CarIdx = idx, Distance = d })
                    .Where(d => d.Distance > 0)
                    .Where(c => c.CarIdx != 0)
                    .OrderByDescending(c => c.Distance)
                    .Select((c, order) => new { CarIdx = c.CarIdx, Position = order + 1, Distance = c.Distance })
                    .ToList();

                var maxRunningOrderIndex = runningOrder.Count == 0 ? 0 : runningOrder.Max(ro => ro.CarIdx);
                var maxSessionIndex = 64;// this.SessionData.DriverInfo.CompetingDrivers.Length;

                positions = new int[Math.Max(maxRunningOrderIndex, maxSessionIndex) + 1];

                positions[0] = int.MaxValue;
                foreach (var runner in runningOrder)
                    positions[runner.CarIdx] = runner.Position;

                var lastKnownPosition = (runningOrder.Count == 0 ? 0 : runningOrder.Max(ro => ro.Position)) + 1;
                for (var i = 0; i < positions.Length; i++)
                    if (positions[i] == 0)
                        positions[i] = lastKnownPosition++;

                return positions;
            }
        }
        

    }
}
