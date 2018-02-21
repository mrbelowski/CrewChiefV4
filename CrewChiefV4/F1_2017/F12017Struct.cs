using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Formatters.Binary;
using System.IO;

namespace CrewChiefV4.F1_2017
{
    [Serializable]
    public struct UDPPacket
    {
        public float m_time;
        public float m_lapTime;
        public float m_lapDistance;
        public float m_totalDistance;
        public float m_x;	// World space position
        public float m_y;	// World space position
        public float m_z;	// World space position
        public float m_speed;	// Speed of car in MPH
        public float m_xv;	// Velocity in world space
        public float m_yv;	// Velocity in world space
        public float m_zv;	// Velocity in world space
        public float m_xr;	// World space right direction
        public float m_yr;	// World space right direction
        public float m_zr;	// World space right direction
        public float m_xd;	// World space forward direction
        public float m_yd;	// World space forward direction
        public float m_zd;	// World space forward direction

        // Note: All wheel arrays have the order: RL, RR, FL, FR
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_susp_pos;	
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_susp_vel;	
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_wheel_speed;
        public float m_throttle;
        public float m_steer;
        public float m_brake;
        public float m_clutch;
        public float m_gear;
        public float m_gforce_lat;
        public float m_gforce_lon;
        public float m_lap;
        public float m_engineRate;
        public float m_sli_pro_native_support;	// SLI Pro support
        public float m_car_position;	// car race position
        public float m_kers_level;	// kers energy left
        public float m_kers_max_level;	// kers maximum energy
        public float m_drs;	// 0 = off, 1 = on
        public float m_traction_control;	// 0 (off) - 2 (high)
        public float m_anti_lock_brakes;	// 0 (off) - 1 (on)
        public float m_fuel_in_tank;	// current fuel mass
        public float m_fuel_capacity;	// fuel capacity
        public float m_in_pits;	// 0 = none, 1 = pitting, 2 = in pit area
        public float m_sector;	// 0 = sector1, 1 = sector2, 2 = sector3
        public float m_sector1_time;	// time of sector1 (or 0)
        public float m_sector2_time;	// time of sector2 (or 0)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_brakes_temp;	// brakes temperature (centigrade)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_tyres_pressure;	// tyres pressure PSI
        public float m_team_info;	// team ID 
        public float m_total_laps;	// total number of laps in this race
        public float m_track_size;	// track size meters
        public float m_last_lap_time;	// last lap time
        public float m_max_rpm;	// cars max RPM, at which point the rev limiter will kick in
        public float m_idle_rpm;	// cars idle RPM
        public float m_max_gears;	// maximum number of gears
        public float m_sessionType;	// 0 = unknown, 1 = practice, 2 = qualifying, 3 = race
        public float m_drsAllowed;	// 0 = not allowed, 1 = allowed, -1 = invalid / unknown
        public float m_track_number;	// -1 for unknown, 0-21 for tracks
        public float m_vehicleFIAFlags;	// -1 = invalid/unknown, 0 = none, 1 = green, 2 = blue, 3 = yellow, 4 = red
        public float m_era;                    	// era, 2017 (modern) or 1980 (classic)
        public float m_engine_temperature;  	// engine temperature (centigrade)
        public float m_gforce_vert;	// vertical g-force component
        public float m_ang_vel_x;	// angular velocity x-component
        public float m_ang_vel_y;	// angular velocity y-component
        public float m_ang_vel_z;	// angular velocity z-component
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[]  m_tyres_temperature;	// tyres temperature (centigrade)
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[]  m_tyres_wear;	// tyre wear percentage
        public byte  m_tyre_compound;	// compound of tyre – 0 = ultra soft, 1 = super soft, 2 = soft, 3 = medium, 4 = hard, 5 = inter, 6 = wet
        public byte  m_front_brake_bias;         // front brake bias (percentage)
        public byte  m_fuel_mix;                 // fuel mix - 0 = lean, 1 = standard, 2 = rich, 3 = max
        public byte  m_currentLapInvalid;    	// current lap invalid - 0 = valid, 1 = invalid
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public byte[]  m_tyres_damage;	// tyre damage (percentage)
        public byte  m_front_left_wing_damage;	// front left wing damage (percentage)
        public byte  m_front_right_wing_damage;	// front right wing damage (percentage)
        public byte  m_rear_wing_damage;	// rear wing damage (percentage)
        public byte  m_engine_damage;	// engine damage (percentage)
        public byte  m_gear_box_damage;	// gear box damage (percentage)
        public byte  m_exhaust_damage;	// exhaust damage (percentage)
        public byte  m_pit_limiter_status;	// pit limiter status – 0 = off, 1 = on
        public byte  m_pit_speed_limit;	// pit speed limit in mph
        public float m_session_time_left;  // NEW: time left in session in seconds 
        public byte  m_rev_lights_percent;  // NEW: rev lights indicator (percentage)
        public byte  m_is_spectating;  // NEW: whether the player is spectating
        public byte  m_spectator_car_index;  // NEW: index of the car being spectated
        // Car data
        public byte  m_num_cars;              	// number of cars in data
        public byte  m_player_car_index;        	// index of player's car in the array
    
        [MarshalAs(UnmanagedType.ByValArray, SizeConst = 20)]
        public CarUDPData[]  m_car_data;   // data for all cars on track

        public float m_yaw;  // NEW (v1.8)
        public float m_pitch;  // NEW (v1.8)
        public float m_roll;  // NEW (v1.8)
        public float m_x_local_velocity;          // NEW (v1.8) Velocity in local space
        public float m_y_local_velocity;          // NEW (v1.8) Velocity in local space
        public float m_z_local_velocity;          // NEW (v1.8) Velocity in local space
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 4)]
        public float[] m_susp_acceleration;   // NEW (v1.8) RL, RR, FL, FR
        public float m_ang_acc_x;                 // NEW (v1.8) angular acceleration x-component
        public float m_ang_acc_y;                 // NEW (v1.8) angular acceleration y-component
        public float m_ang_acc_z;                 // NEW (v1.8) angular acceleration z-component   
    }

    [Serializable]
    public struct CarUDPData
    {
        [MarshalAsAttribute(UnmanagedType.ByValArray, SizeConst = 3)]
        public float[] m_worldPosition; // world co-ordinates of vehicle
        public float m_lastLapTime;
        public float m_currentLapTime;
        public float m_bestLapTime;
        public float m_sector1Time;
        public float m_sector2Time;
        public float m_lapDistance;
        public byte m_driverId;
        public byte m_teamId;
        public byte m_carPosition;     // UPDATED: track positions of vehicle
        public byte m_currentLapNum;
        public byte m_tyreCompound;	// compound of tyre – 0 = ultra soft, 1 = super soft, 2 = soft, 3 = medium, 4 = hard, 5 = inter, 6 = wet
        public byte m_inPits;           // 0 = none, 1 = pitting, 2 = in pit area
        public byte m_sector;           // 0 = sector1, 1 = sector2, 2 = sector3
        public byte m_currentLapInvalid; // current lap invalid - 0 = valid, 1 = invalid
        public byte m_penalties;  // NEW: accumulated time penalties in seconds to be added
    }
}