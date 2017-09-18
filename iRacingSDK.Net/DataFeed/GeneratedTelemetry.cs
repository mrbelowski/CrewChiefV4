
// This file is part of iRacingSDK.
//
// Copyright 2014 Dean Netherton
// https://github.com/vipoo/iRacingSDK.Net
//
// iRacingSDK is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// iRacingSDK is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with iRacingSDK.  If not, see <http://www.gnu.org/licenses/>.

using System;
using System.Collections.Generic;
using System.Linq;

namespace iRacingSDK
{
    public partial class Telemetry : Dictionary<string, object>
    {
        public SessionData SessionData { get; set; }

        /// <summary>
        /// Seconds since session start
        /// </summary>
        public System.Double SessionTime { get { return (System.Double)this["SessionTime"]; } }

        /// <summary>
        /// Session number
        /// </summary>
        public System.Int32 SessionNum { get { return (System.Int32)this["SessionNum"]; } }

        /// <summary>
        /// Session state
        /// </summary>
        public iRacingSDK.SessionState SessionState { get { return (iRacingSDK.SessionState)this["SessionState"]; } }

        /// <summary>
        /// Session ID
        /// </summary>
        public System.Int32 SessionUniqueID { get { return (System.Int32)this["SessionUniqueID"]; } }

        /// <summary>
        /// Session flags
        /// </summary>
        public iRacingSDK.SessionFlags SessionFlags { get { return (iRacingSDK.SessionFlags)(int)this["SessionFlags"]; } }

        /// <summary>
        /// Seconds left till session ends
        /// </summary>
        public System.Double SessionTimeRemain { get { return (System.Double)this["SessionTimeRemain"]; } }

        /// <summary>
        /// Old laps left till session ends use SessionLapsRemainEx
        /// </summary>
        public System.Int32 SessionLapsRemain { get { return (System.Int32)this["SessionLapsRemain"]; } }

        /// <summary>
        /// New improved laps left till session ends
        /// </summary>
        public System.Int32 SessionLapsRemainEx { get { return (System.Int32)this["SessionLapsRemainEx"]; } }

        /// <summary>
        /// The car index of the current person speaking on the radio
        /// </summary>
        public System.Int32 RadioTransmitCarIdx { get { return (System.Int32)this["RadioTransmitCarIdx"]; } }

        /// <summary>
        /// The radio index of the current person speaking on the radio
        /// </summary>
        public System.Int32 RadioTransmitRadioIdx { get { return (System.Int32)this["RadioTransmitRadioIdx"]; } }

        /// <summary>
        /// The frequency index of the current person speaking on the radio
        /// </summary>
        public System.Int32 RadioTransmitFrequencyIdx { get { return (System.Int32)this["RadioTransmitFrequencyIdx"]; } }

        /// <summary>
        /// Default units for the user interface 0 = english 1 = metric
        /// </summary>
        public iRacingSDK.DisplayUnits DisplayUnits { get { return (iRacingSDK.DisplayUnits)(System.Int32)this["DisplayUnits"]; } }

        /// <summary>
        /// Driver activated flag
        /// </summary>
        public System.Boolean DriverMarker { get { return (System.Boolean)this["DriverMarker"]; } }

        /// <summary>
        /// 1=Car on track physics running with player in car
        /// </summary>
        public System.Boolean IsOnTrack { get { return (System.Boolean)this["IsOnTrack"]; } }

        /// <summary>
        /// 0=replay not playing  1=replay playing
        /// </summary>
        public System.Boolean IsReplayPlaying { get { return (System.Boolean)this["IsReplayPlaying"]; } }

        /// <summary>
        /// Integer replay frame number (60 per second)
        /// </summary>
        public System.Int32 ReplayFrameNum { get { return (System.Int32)this["ReplayFrameNum"]; } }

        /// <summary>
        /// Integer replay frame number from end of tape
        /// </summary>
        public System.Int32 ReplayFrameNumEnd { get { return (System.Int32)this["ReplayFrameNumEnd"]; } }

        /// <summary>
        /// 0=disk based telemetry turned off  1=turned on
        /// </summary>
        public System.Boolean IsDiskLoggingEnabled { get { return (System.Boolean)this["IsDiskLoggingEnabled"]; } }

        /// <summary>
        /// 0=disk based telemetry file not being written  1=being written
        /// </summary>
        public System.Boolean IsDiskLoggingActive { get { return (System.Boolean)this["IsDiskLoggingActive"]; } }

        /// <summary>
        /// Average frames per second
        /// </summary>
        public System.Single FrameRate { get { return (System.Single)this["FrameRate"]; } }

        /// <summary>
        /// Percent of available tim bg thread took with a 1 sec avg
        /// </summary>
        public System.Single CpuUsageBG { get { return (System.Single)this["CpuUsageBG"]; } }

        /// <summary>
        /// Players position in race
        /// </summary>
        public System.Int32 PlayerCarPosition { get { return (System.Int32)this["PlayerCarPosition"]; } }

        /// <summary>
        /// Players class position in race
        /// </summary>
        public System.Int32 PlayerCarClassPosition { get { return (System.Int32)this["PlayerCarClassPosition"]; } }

        /// <summary>
        /// Laps started by car index
        /// </summary>
        public System.Int32[] CarIdxLap { get { return (System.Int32[])this["CarIdxLap"]; } }

        /// <summary>
        /// Laps completed by car index
        /// </summary>
        public System.Int32[] CarIdxLapCompleted { get { return (System.Int32[])this["CarIdxLapCompleted"]; } }

        /// <summary>
        /// Percentage distance around lap by car index
        /// </summary>
        public System.Single[] CarIdxLapDistPct { get { return (System.Single[])this["CarIdxLapDistPct"]; } }

        /// <summary>
        /// Track surface type by car index
        /// </summary>
        public iRacingSDK.TrackLocation[] CarIdxTrackSurface { get { return (iRacingSDK.TrackLocation[])this["CarIdxTrackSurface"]; } }

        /// <summary>
        /// On pit road between the cones by car index
        /// </summary>
        public System.Boolean[] CarIdxOnPitRoad { get { return (System.Boolean[])this["CarIdxOnPitRoad"]; } }

        /// <summary>
        /// Cars position in race by car index
        /// </summary>
        public System.Int32[] CarIdxPosition { get { return (System.Int32[])this["CarIdxPosition"]; } }

        /// <summary>
        /// Cars class position in race by car index
        /// </summary>
        public System.Int32[] CarIdxClassPosition { get { return (System.Int32[])this["CarIdxClassPosition"]; } }

        /// <summary>
        /// Race time behind leader or fastest lap time otherwise
        /// </summary>
        public System.Single[] CarIdxF2Time { get { return (System.Single[])this["CarIdxF2Time"]; } }

        /// <summary>
        /// Estimated time to reach current location on track
        /// </summary>
        public System.Single[] CarIdxEstTime { get { return (System.Single[])this["CarIdxEstTime"]; } }

        /// <summary>
        /// Is the player car on pit road between the cones
        /// </summary>
        public System.Boolean OnPitRoad { get { return (System.Boolean)this["OnPitRoad"]; } }

        /// <summary>
        /// Steering wheel angle by car index
        /// </summary>
        public System.Single[] CarIdxSteer { get { return (System.Single[])this["CarIdxSteer"]; } }

        /// <summary>
        /// Engine rpm by car index
        /// </summary>
        public System.Single[] CarIdxRPM { get { return (System.Single[])this["CarIdxRPM"]; } }

        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear by car index
        /// </summary>
        public System.Int32[] CarIdxGear { get { return (System.Int32[])this["CarIdxGear"]; } }

        /// <summary>
        /// Steering wheel angle
        /// </summary>
        public System.Single SteeringWheelAngle { get { return (System.Single)this["SteeringWheelAngle"]; } }

        /// <summary>
        /// 0=off throttle to 1=full throttle
        /// </summary>
        public System.Single Throttle { get { return (System.Single)this["Throttle"]; } }

        /// <summary>
        /// 0=brake released to 1=max pedal force
        /// </summary>
        public System.Single Brake { get { return (System.Single)this["Brake"]; } }

        /// <summary>
        /// 0=disengaged to 1=fully engaged
        /// </summary>
        public System.Single Clutch { get { return (System.Single)this["Clutch"]; } }

        /// <summary>
        /// -1=reverse  0=neutral  1..n=current gear
        /// </summary>
        public System.Int32 Gear { get { return (System.Int32)this["Gear"]; } }

        /// <summary>
        /// Engine rpm
        /// </summary>
        public System.Single RPM { get { return (System.Single)this["RPM"]; } }

        /// <summary>
        /// Laps started count
        /// </summary>
        public System.Int32 Lap { get { return (System.Int32)this["Lap"]; } }

        /// <summary>
        /// Laps completed count
        /// </summary>
        public System.Int32 LapCompleted { get { return (System.Int32)this["LapCompleted"]; } }

        /// <summary>
        /// Meters traveled from S/F this lap
        /// </summary>
        public System.Single LapDist { get { return (System.Single)this["LapDist"]; } }

        /// <summary>
        /// Percentage distance around lap
        /// </summary>
        public System.Single LapDistPct { get { return (System.Single)this["LapDistPct"]; } }

        /// <summary>
        /// Laps completed in race
        /// </summary>
        public System.Int32 RaceLaps { get { return (System.Int32)this["RaceLaps"]; } }

        /// <summary>
        /// Players best lap number
        /// </summary>
        public System.Int32 LapBestLap { get { return (System.Int32)this["LapBestLap"]; } }

        /// <summary>
        /// Players best lap time
        /// </summary>
        public System.Single LapBestLapTime { get { return (System.Single)this["LapBestLapTime"]; } }

        /// <summary>
        /// Players last lap time
        /// </summary>
        public System.Single LapLastLapTime { get { return (System.Single)this["LapLastLapTime"]; } }

        /// <summary>
        /// Estimate of players current lap time as shown in F3 box
        /// </summary>
        public System.Single LapCurrentLapTime { get { return (System.Single)this["LapCurrentLapTime"]; } }

        /// <summary>
        /// Player num consecutive clean laps completed for N average
        /// </summary>
        public System.Int32 LapLasNLapSeq { get { return (System.Int32)this["LapLasNLapSeq"]; } }

        /// <summary>
        /// Player last N average lap time
        /// </summary>
        public System.Single LapLastNLapTime { get { return (System.Single)this["LapLastNLapTime"]; } }

        /// <summary>
        /// Player last lap in best N average lap time
        /// </summary>
        public System.Int32 LapBestNLapLap { get { return (System.Int32)this["LapBestNLapLap"]; } }

        /// <summary>
        /// Player best N average lap time
        /// </summary>
        public System.Single LapBestNLapTime { get { return (System.Single)this["LapBestNLapTime"]; } }

        /// <summary>
        /// Delta time for best lap
        /// </summary>
        public System.Single LapDeltaToBestLap { get { return (System.Single)this["LapDeltaToBestLap"]; } }

        /// <summary>
        /// Rate of change of delta time for best lap
        /// </summary>
        public System.Single LapDeltaToBestLap_DD { get { return (System.Single)this["LapDeltaToBestLap_DD"]; } }

        /// <summary>
        /// Delta time for best lap is valid
        /// </summary>
        public System.Boolean LapDeltaToBestLap_OK { get { return (System.Boolean)this["LapDeltaToBestLap_OK"]; } }

        /// <summary>
        /// Delta time for optimal lap
        /// </summary>
        public System.Single LapDeltaToOptimalLap { get { return (System.Single)this["LapDeltaToOptimalLap"]; } }

        /// <summary>
        /// Rate of change of delta time for optimal lap
        /// </summary>
        public System.Single LapDeltaToOptimalLap_DD { get { return (System.Single)this["LapDeltaToOptimalLap_DD"]; } }

        /// <summary>
        /// Delta time for optimal lap is valid
        /// </summary>
        public System.Boolean LapDeltaToOptimalLap_OK { get { return (System.Boolean)this["LapDeltaToOptimalLap_OK"]; } }

        /// <summary>
        /// Delta time for session best lap
        /// </summary>
        public System.Single LapDeltaToSessionBestLap { get { return (System.Single)this["LapDeltaToSessionBestLap"]; } }

        /// <summary>
        /// Rate of change of delta time for session best lap
        /// </summary>
        public System.Single LapDeltaToSessionBestLap_DD { get { return (System.Single)this["LapDeltaToSessionBestLap_DD"]; } }

        /// <summary>
        /// Delta time for session best lap is valid
        /// </summary>
        public System.Boolean LapDeltaToSessionBestLap_OK { get { return (System.Boolean)this["LapDeltaToSessionBestLap_OK"]; } }

        /// <summary>
        /// Delta time for session optimal lap
        /// </summary>
        public System.Single LapDeltaToSessionOptimalLap { get { return (System.Single)this["LapDeltaToSessionOptimalLap"]; } }

        /// <summary>
        /// Rate of change of delta time for session optimal lap
        /// </summary>
        public System.Single LapDeltaToSessionOptimalLap_DD { get { return (System.Single)this["LapDeltaToSessionOptimalLap_DD"]; } }

        /// <summary>
        /// Delta time for session optimal lap is valid
        /// </summary>
        public System.Boolean LapDeltaToSessionOptimalLap_OK { get { return (System.Boolean)this["LapDeltaToSessionOptimalLap_OK"]; } }

        /// <summary>
        /// Delta time for session last lap
        /// </summary>
        public System.Single LapDeltaToSessionLastlLap { get { return (System.Single)this["LapDeltaToSessionLastlLap"]; } }

        /// <summary>
        /// Rate of change of delta time for session last lap
        /// </summary>
        public System.Single LapDeltaToSessionLastlLap_DD { get { return (System.Single)this["LapDeltaToSessionLastlLap_DD"]; } }

        /// <summary>
        /// Delta time for session last lap is valid
        /// </summary>
        public System.Boolean LapDeltaToSessionLastlLap_OK { get { return (System.Boolean)this["LapDeltaToSessionLastlLap_OK"]; } }

        /// <summary>
        /// Longitudinal acceleration (including gravity)
        /// </summary>
        public System.Single LongAccel { get { return (System.Single)this["LongAccel"]; } }

        /// <summary>
        /// Lateral acceleration (including gravity)
        /// </summary>
        public System.Single LatAccel { get { return (System.Single)this["LatAccel"]; } }

        /// <summary>
        /// Vertical acceleration (including gravity)
        /// </summary>
        public System.Single VertAccel { get { return (System.Single)this["VertAccel"]; } }

        /// <summary>
        /// Roll rate
        /// </summary>
        public System.Single RollRate { get { return (System.Single)this["RollRate"]; } }

        /// <summary>
        /// Pitch rate
        /// </summary>
        public System.Single PitchRate { get { return (System.Single)this["PitchRate"]; } }

        /// <summary>
        /// Yaw rate
        /// </summary>
        public System.Single YawRate { get { return (System.Single)this["YawRate"]; } }

        /// <summary>
        /// GPS vehicle speed
        /// </summary>
        public System.Single Speed { get { return (System.Single)this["Speed"]; } }

        /// <summary>
        /// X velocity
        /// </summary>
        public System.Single VelocityX { get { return (System.Single)this["VelocityX"]; } }

        /// <summary>
        /// Y velocity
        /// </summary>
        public System.Single VelocityY { get { return (System.Single)this["VelocityY"]; } }

        /// <summary>
        /// Z velocity
        /// </summary>
        public System.Single VelocityZ { get { return (System.Single)this["VelocityZ"]; } }

        /// <summary>
        /// Yaw orientation
        /// </summary>
        public System.Single Yaw { get { return (System.Single)this["Yaw"]; } }

        /// <summary>
        /// Yaw orientation relative to north
        /// </summary>
        public System.Single YawNorth { get { return (System.Single)this["YawNorth"]; } }

        /// <summary>
        /// Pitch orientation
        /// </summary>
        public System.Single Pitch { get { return (System.Single)this["Pitch"]; } }

        /// <summary>
        /// Roll orientation
        /// </summary>
        public System.Single Roll { get { return (System.Single)this["Roll"]; } }

        /// <summary>
        /// Indicate action the reset key will take 0 enter 1 exit 2 reset
        /// </summary>
        public System.Int32 EnterExitReset { get { return (System.Int32)this["EnterExitReset"]; } }

        /// <summary>
        /// Temperature of track at start/finish line
        /// </summary>
        public System.Single TrackTemp { get { return (System.Single)this["TrackTemp"]; } }

        /// <summary>
        /// Temperature of track measured by crew around track
        /// </summary>
        public System.Single TrackTempCrew { get { return (System.Single)this["TrackTempCrew"]; } }

        /// <summary>
        /// Temperature of air at start/finish line
        /// </summary>
        public System.Single AirTemp { get { return (System.Single)this["AirTemp"]; } }

        /// <summary>
        /// Weather type (0=constant  1=dynamic)
        /// </summary>
        public iRacingSDK.WeatherType WeatherType { get { return (iRacingSDK.WeatherType)(System.Int32)this["WeatherType"]; } }

        /// <summary>
        /// Skies (0=clear/1=p cloudy/2=m cloudy/3=overcast)
        /// </summary>
        public iRacingSDK.Skies Skies { get { return (iRacingSDK.Skies)(System.Int32)this["Skies"]; } }

        /// <summary>
        /// Density of air at start/finish line
        /// </summary>
        public System.Single AirDensity { get { return (System.Single)this["AirDensity"]; } }

        /// <summary>
        /// Pressure of air at start/finish line
        /// </summary>
        public System.Single AirPressure { get { return (System.Single)this["AirPressure"]; } }

        /// <summary>
        /// Wind velocity at start/finish line
        /// </summary>
        public System.Single WindVel { get { return (System.Single)this["WindVel"]; } }

        /// <summary>
        /// Wind direction at start/finish line
        /// </summary>
        public System.Single WindDir { get { return (System.Single)this["WindDir"]; } }

        /// <summary>
        /// Relative Humidity
        /// </summary>
        public System.Single RelativeHumidity { get { return (System.Single)this["RelativeHumidity"]; } }

        /// <summary>
        /// Fog level
        /// </summary>
        public System.Single FogLevel { get { return (System.Single)this["FogLevel"]; } }

        /// <summary>
        /// Status of driver change lap requirements
        /// </summary>
        public System.Int32 DCLapStatus { get { return (System.Int32)this["DCLapStatus"]; } }

        /// <summary>
        /// Number of team drivers who have run a stint
        /// </summary>
        public System.Int32 DCDriversSoFar { get { return (System.Int32)this["DCDriversSoFar"]; } }

        /// <summary>
        /// True if it is ok to reload car textures at this time
        /// </summary>
        public System.Boolean OkToReloadTextures { get { return (System.Boolean)this["OkToReloadTextures"]; } }

        /// <summary>
        /// Time left for mandatory pit repairs if repairs are active
        /// </summary>
        public System.Single PitRepairLeft { get { return (System.Single)this["PitRepairLeft"]; } }

        /// <summary>
        /// Time left for optional repairs if repairs are active
        /// </summary>
        public System.Single PitOptRepairLeft { get { return (System.Single)this["PitOptRepairLeft"]; } }

        /// <summary>
        /// Active camera's focus car index
        /// </summary>
        public System.Int32 CamCarIdx { get { return (System.Int32)this["CamCarIdx"]; } }

        /// <summary>
        /// Active camera number
        /// </summary>
        public System.Int32 CamCameraNumber { get { return (System.Int32)this["CamCameraNumber"]; } }

        /// <summary>
        /// Active camera group number
        /// </summary>
        public System.Int32 CamGroupNumber { get { return (System.Int32)this["CamGroupNumber"]; } }

        /// <summary>
        /// State of camera system
        /// </summary>
        public System.Int32 CamCameraState { get { return (System.Int32)this["CamCameraState"]; } }

        /// <summary>
        /// 1=Car on track physics running
        /// </summary>
        public System.Boolean IsOnTrackCar { get { return (System.Boolean)this["IsOnTrackCar"]; } }

        /// <summary>
        /// 1=Car in garage physics running
        /// </summary>
        public System.Boolean IsInGarage { get { return (System.Boolean)this["IsInGarage"]; } }

        /// <summary>
        /// Output torque on steering shaft
        /// </summary>
        public System.Single SteeringWheelTorque { get { return (System.Single)this["SteeringWheelTorque"]; } }

        /// <summary>
        /// Force feedback % max torque on steering shaft unsigned
        /// </summary>
        public System.Single SteeringWheelPctTorque { get { return (System.Single)this["SteeringWheelPctTorque"]; } }

        /// <summary>
        /// Force feedback % max torque on steering shaft signed
        /// </summary>
        public System.Single SteeringWheelPctTorqueSign { get { return (System.Single)this["SteeringWheelPctTorqueSign"]; } }

        /// <summary>
        /// Force feedback % max torque on steering shaft signed stops
        /// </summary>
        public System.Single SteeringWheelPctTorqueSignStops { get { return (System.Single)this["SteeringWheelPctTorqueSignStops"]; } }

        /// <summary>
        /// Force feedback % max damping
        /// </summary>
        public System.Single SteeringWheelPctDamper { get { return (System.Single)this["SteeringWheelPctDamper"]; } }

        /// <summary>
        /// Steering wheel max angle
        /// </summary>
        public System.Single SteeringWheelAngleMax { get { return (System.Single)this["SteeringWheelAngleMax"]; } }

        /// <summary>
        /// DEPRECATED use DriverCarSLBlinkRPM instead
        /// </summary>
        public System.Single ShiftIndicatorPct { get { return (System.Single)this["ShiftIndicatorPct"]; } }

        /// <summary>
        /// Friction torque applied to gears when shifting or grinding
        /// </summary>
        public System.Single ShiftPowerPct { get { return (System.Single)this["ShiftPowerPct"]; } }

        /// <summary>
        /// RPM of shifter grinding noise
        /// </summary>
        public System.Single ShiftGrindRPM { get { return (System.Single)this["ShiftGrindRPM"]; } }

        /// <summary>
        /// Raw throttle input 0=off throttle to 1=full throttle
        /// </summary>
        public System.Single ThrottleRaw { get { return (System.Single)this["ThrottleRaw"]; } }

        /// <summary>
        /// Raw brake input 0=brake released to 1=max pedal force
        /// </summary>
        public System.Single BrakeRaw { get { return (System.Single)this["BrakeRaw"]; } }

        /// <summary>
        /// Peak torque mapping to direct input units for FFB
        /// </summary>
        public System.Single SteeringWheelPeakForceNm { get { return (System.Single)this["SteeringWheelPeakForceNm"]; } }

        /// <summary>
        /// Bitfield for warning lights
        /// </summary>
        public iRacingSDK.EngineWarnings EngineWarnings { get { return (iRacingSDK.EngineWarnings)(System.Int32)this["EngineWarnings"]; } }

        /// <summary>
        /// Liters of fuel remaining
        /// </summary>
        public System.Single FuelLevel { get { return (System.Single)this["FuelLevel"]; } }

        /// <summary>
        /// Percent fuel remaining
        /// </summary>
        public System.Single FuelLevelPct { get { return (System.Single)this["FuelLevelPct"]; } }

        /// <summary>
        /// Bitfield of pit service checkboxes
        /// </summary>
        public System.Int32 PitSvFlags { get { return (System.Int32)this["PitSvFlags"]; } }

        /// <summary>
        /// Pit service left front tire pressure
        /// </summary>
        public System.Single PitSvLFP { get { return (System.Single)this["PitSvLFP"]; } }

        /// <summary>
        /// Pit service right front tire pressure
        /// </summary>
        public System.Single PitSvRFP { get { return (System.Single)this["PitSvRFP"]; } }

        /// <summary>
        /// Pit service left rear tire pressure
        /// </summary>
        public System.Single PitSvLRP { get { return (System.Single)this["PitSvLRP"]; } }

        /// <summary>
        /// Pit service right rear tire pressure
        /// </summary>
        public System.Single PitSvRRP { get { return (System.Single)this["PitSvRRP"]; } }

        /// <summary>
        /// Pit service fuel add amount
        /// </summary>
        public System.Single PitSvFuel { get { return (System.Single)this["PitSvFuel"]; } }

        /// <summary>
        /// Replay playback speed
        /// </summary>
        public System.Int32 ReplayPlaySpeed { get { return (System.Int32)this["ReplayPlaySpeed"]; } }

        /// <summary>
        /// 0=not slow motion  1=replay is in slow motion
        /// </summary>
        public System.Boolean ReplayPlaySlowMotion { get { return (System.Boolean)this["ReplayPlaySlowMotion"]; } }

        /// <summary>
        /// Seconds since replay session start
        /// </summary>
        public System.Double ReplaySessionTime { get { return (System.Double)this["ReplaySessionTime"]; } }

        /// <summary>
        /// Replay session number
        /// </summary>
        public System.Int32 ReplaySessionNum { get { return (System.Int32)this["ReplaySessionNum"]; } }

        /// <summary>
        /// In car front anti roll bar adjustment
        /// </summary>
        public System.Single dcAntiRollFront { get { return (System.Single)this["dcAntiRollFront"]; } }

        /// <summary>
        /// In car brake bias adjustment
        /// </summary>
        public System.Single dcBrakeBias { get { return (System.Single)this["dcBrakeBias"]; } }

        /// <summary>
        /// In car traction control adjustment
        /// </summary>
        public System.Single dcTractionControl { get { return (System.Single)this["dcTractionControl"]; } }

        /// <summary>
        /// In car abs adjustment
        /// </summary>
        public System.Single dcABS { get { return (System.Single)this["dcABS"]; } }

        /// <summary>
        /// In car throttle shape adjustment
        /// </summary>
        public System.Single dcThrottleShape { get { return (System.Single)this["dcThrottleShape"]; } }

        /// <summary>
        /// In car fuel mixture adjustment
        /// </summary>
        public System.Single dcFuelMixture { get { return (System.Single)this["dcFuelMixture"]; } }

        /// <summary>
        /// Pitstop qtape adjustment
        /// </summary>
        public System.Single dpQtape { get { return (System.Single)this["dpQtape"]; } }

        /// <summary>
        /// Pitstop wedge adjustment
        /// </summary>
        public System.Single dpWedgeAdj { get { return (System.Single)this["dpWedgeAdj"]; } }
        
        /// <summary>
        /// In car rear anti roll bar adjustment
        /// </summary>
        public System.Single dcAntiRollRear { get { return (System.Single)this["dcAntiRollRear"]; } }

        /// <summary>
        /// Pitstop rear wing adjustment
        /// </summary>
        public System.Single dpRWingSetting { get { return (System.Single)this["dpRWingSetting"]; } }

        /// <summary>
        /// Engine coolant temp
        /// </summary>
        public System.Single WaterTemp { get { return (System.Single)this["WaterTemp"]; } }

        /// <summary>
        /// Engine coolant level
        /// </summary>
        public System.Single WaterLevel { get { return (System.Single)this["WaterLevel"]; } }

        /// <summary>
        /// Engine fuel pressure
        /// </summary>
        public System.Single FuelPress { get { return (System.Single)this["FuelPress"]; } }

        /// <summary>
        /// Engine fuel used instantaneous
        /// </summary>
        public System.Single FuelUsePerHour { get { return (System.Single)this["FuelUsePerHour"]; } }

        /// <summary>
        /// Engine oil temperature
        /// </summary>
        public System.Single OilTemp { get { return (System.Single)this["OilTemp"]; } }

        /// <summary>
        /// Engine oil pressure
        /// </summary>
        public System.Single OilPress { get { return (System.Single)this["OilPress"]; } }

        /// <summary>
        /// Engine oil level
        /// </summary>
        public System.Single OilLevel { get { return (System.Single)this["OilLevel"]; } }

        /// <summary>
        /// Engine voltage
        /// </summary>
        public System.Single Voltage { get { return (System.Single)this["Voltage"]; } }

        /// <summary>
        /// Engine manifold pressure
        /// </summary>
        public System.Single ManifoldPress { get { return (System.Single)this["ManifoldPress"]; } }

        /// <summary>
        /// RR brake line pressure
        /// </summary>
        public System.Single RRbrakeLinePress { get { return (System.Single)this["RRbrakeLinePress"]; } }

        /// <summary>
        /// RR tire cold pressure  as set in the garage
        /// </summary>
        public System.Single RRcoldPressure { get { return (System.Single)this["RRcoldPressure"]; } }

        /// <summary>
        /// RR tire left carcass temperature
        /// </summary>
        public System.Single RRtempCL { get { return (System.Single)this["RRtempCL"]; } }

        /// <summary>
        /// RR tire middle carcass temperature
        /// </summary>
        public System.Single RRtempCM { get { return (System.Single)this["RRtempCM"]; } }

        /// <summary>
        /// RR tire right carcass temperature
        /// </summary>
        public System.Single RRtempCR { get { return (System.Single)this["RRtempCR"]; } }

        /// <summary>
        /// RR tire left percent tread remaining
        /// </summary>
        public System.Single RRwearL { get { return (System.Single)this["RRwearL"]; } }

        /// <summary>
        /// RR tire middle percent tread remaining
        /// </summary>
        public System.Single RRwearM { get { return (System.Single)this["RRwearM"]; } }

        /// <summary>
        /// RR tire right percent tread remaining
        /// </summary>
        public System.Single RRwearR { get { return (System.Single)this["RRwearR"]; } }

        /// <summary>
        /// LR brake line pressure
        /// </summary>
        public System.Single LRbrakeLinePress { get { return (System.Single)this["LRbrakeLinePress"]; } }

        /// <summary>
        /// LR tire cold pressure  as set in the garage
        /// </summary>
        public System.Single LRcoldPressure { get { return (System.Single)this["LRcoldPressure"]; } }

        /// <summary>
        /// LR tire left carcass temperature
        /// </summary>
        public System.Single LRtempCL { get { return (System.Single)this["LRtempCL"]; } }

        /// <summary>
        /// LR tire middle carcass temperature
        /// </summary>
        public System.Single LRtempCM { get { return (System.Single)this["LRtempCM"]; } }

        /// <summary>
        /// LR tire right carcass temperature
        /// </summary>
        public System.Single LRtempCR { get { return (System.Single)this["LRtempCR"]; } }

        /// <summary>
        /// LR tire left percent tread remaining
        /// </summary>
        public System.Single LRwearL { get { return (System.Single)this["LRwearL"]; } }

        /// <summary>
        /// LR tire middle percent tread remaining
        /// </summary>
        public System.Single LRwearM { get { return (System.Single)this["LRwearM"]; } }

        /// <summary>
        /// LR tire right percent tread remaining
        /// </summary>
        public System.Single LRwearR { get { return (System.Single)this["LRwearR"]; } }

        /// <summary>
        /// RF brake line pressure
        /// </summary>
        public System.Single RFbrakeLinePress { get { return (System.Single)this["RFbrakeLinePress"]; } }

        /// <summary>
        /// RF tire cold pressure  as set in the garage
        /// </summary>
        public System.Single RFcoldPressure { get { return (System.Single)this["RFcoldPressure"]; } }

        /// <summary>
        /// RF tire left carcass temperature
        /// </summary>
        public System.Single RFtempCL { get { return (System.Single)this["RFtempCL"]; } }

        /// <summary>
        /// RF tire middle carcass temperature
        /// </summary>
        public System.Single RFtempCM { get { return (System.Single)this["RFtempCM"]; } }

        /// <summary>
        /// RF tire right carcass temperature
        /// </summary>
        public System.Single RFtempCR { get { return (System.Single)this["RFtempCR"]; } }

        /// <summary>
        /// RF tire left percent tread remaining
        /// </summary>
        public System.Single RFwearL { get { return (System.Single)this["RFwearL"]; } }

        /// <summary>
        /// RF tire middle percent tread remaining
        /// </summary>
        public System.Single RFwearM { get { return (System.Single)this["RFwearM"]; } }

        /// <summary>
        /// RF tire right percent tread remaining
        /// </summary>
        public System.Single RFwearR { get { return (System.Single)this["RFwearR"]; } }

        /// <summary>
        /// LF brake line pressure
        /// </summary>
        public System.Single LFbrakeLinePress { get { return (System.Single)this["LFbrakeLinePress"]; } }

        /// <summary>
        /// LF tire cold pressure  as set in the garage
        /// </summary>
        public System.Single LFcoldPressure { get { return (System.Single)this["LFcoldPressure"]; } }

        /// <summary>
        /// LF tire left carcass temperature
        /// </summary>
        public System.Single LFtempCL { get { return (System.Single)this["LFtempCL"]; } }

        /// <summary>
        /// LF tire middle carcass temperature
        /// </summary>
        public System.Single LFtempCM { get { return (System.Single)this["LFtempCM"]; } }

        /// <summary>
        /// LF tire right carcass temperature
        /// </summary>
        public System.Single LFtempCR { get { return (System.Single)this["LFtempCR"]; } }

        /// <summary>
        /// LF tire left percent tread remaining
        /// </summary>
        public System.Single LFwearL { get { return (System.Single)this["LFwearL"]; } }

        /// <summary>
        /// LF tire middle percent tread remaining
        /// </summary>
        public System.Single LFwearM { get { return (System.Single)this["LFwearM"]; } }

        /// <summary>
        /// LF tire right percent tread remaining
        /// </summary>
        public System.Single LFwearR { get { return (System.Single)this["LFwearR"]; } }

        /// <summary>
        /// RR shock deflection
        /// </summary>
        public System.Single RRshockDefl { get { return (System.Single)this["RRshockDefl"]; } }

        /// <summary>
        /// RR shock velocity
        /// </summary>
        public System.Single RRshockVel { get { return (System.Single)this["RRshockVel"]; } }

        /// <summary>
        /// LR shock deflection
        /// </summary>
        public System.Single LRshockDefl { get { return (System.Single)this["LRshockDefl"]; } }

        /// <summary>
        /// LR shock velocity
        /// </summary>
        public System.Single LRshockVel { get { return (System.Single)this["LRshockVel"]; } }

        /// <summary>
        /// RF shock deflection
        /// </summary>
        public System.Single RFshockDefl { get { return (System.Single)this["RFshockDefl"]; } }

        /// <summary>
        /// RF shock velocity
        /// </summary>
        public System.Single RFshockVel { get { return (System.Single)this["RFshockVel"]; } }

        /// <summary>
        /// LF shock deflection
        /// </summary>
        public System.Single LFshockDefl { get { return (System.Single)this["LFshockDefl"]; } }

        /// <summary>
        /// LF shock velocity
        /// </summary>
        public System.Single LFshockVel { get { return (System.Single)this["LFshockVel"]; } }

        /// <summary>
        /// RRSH shock deflection
        /// </summary>
        public System.Single RRSHshockDefl { get { return (System.Single)this["RRSHshockDefl"]; } }

        /// <summary>
        /// LRSH shock deflection
        /// </summary>
        public System.Single LRSHshockDefl { get { return (System.Single)this["LRSHshockDefl"]; } }

        /// <summary>
        /// RFSH shock deflection
        /// </summary>
        public System.Single RFSHshockDefl { get { return (System.Single)this["RFSHshockDefl"]; } }

        /// <summary>
        /// LFSH shock deflection
        /// </summary>
        public System.Single LFSHshockDefl { get { return (System.Single)this["LFSHshockDefl"]; } }

        /// <summary>
        /// Notify if car is to the left or right of driver
        /// </summary>
        public System.Int32 CarLeftRight { get { return (System.Int32)this["CarLeftRight"]; } }

        /// <summary>
        /// 
        /// </summary>
        public System.Int32 TickCount { get { return (System.Int32)this["TickCount"]; } }

        public System.Int32 PlayerCarIdx { get { return (System.Int32)this["PlayerCarIdx"]; } }

    }
}
