using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.PCars2
{
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

    // (Type#1) GameState (to be used with 'mGameState')
    public enum eGameState
    {
      GAME_EXITED = 0,
      GAME_FRONT_END,
      GAME_INGAME_PLAYING,
      GAME_INGAME_PAUSED,
	    GAME_INGAME_INMENU_TIME_TICKING,
      GAME_INGAME_RESTARTING,
      GAME_INGAME_REPLAY,
      GAME_FRONT_END_REPLAY,
      //-------------
      GAME_MAX
    };

    // (Type#2) Session state (to be used with 'mSessionState')
    public enum eSessionState
    {
      SESSION_INVALID = 0,
      SESSION_PRACTICE,
      SESSION_TEST,
      SESSION_QUALIFY,
      SESSION_FORMATION_LAP,
      SESSION_RACE,
      SESSION_TIME_ATTACK,
      //-------------
      SESSION_MAX
    };

    // (Type#3) RaceState (to be used with 'mRaceState' and 'mRaceStates')
    public enum eRaceState
    {
      RACESTATE_INVALID,
      RACESTATE_NOT_STARTED,
      RACESTATE_RACING,
      RACESTATE_FINISHED,
      RACESTATE_DISQUALIFIED,
      RACESTATE_RETIRED,
      RACESTATE_DNF,
      //-------------
      RACESTATE_MAX
    };

    // (Type#5) Flag Colours (to be used with 'mHighestFlagColour')
    public enum eFlagColour
    {
      FLAG_COLOUR_NONE = 0,             // Not used for actual flags, only for some query functions
      FLAG_COLOUR_GREEN,                // End of danger zone, or race started
      FLAG_COLOUR_BLUE,                 // Faster car wants to overtake the participant
      FLAG_COLOUR_WHITE_SLOW_CAR,       // Slow car in area
      FLAG_COLOUR_WHITE_FINAL_LAP,      // Final Lap
      FLAG_COLOUR_RED,                  // Huge collisions where one or more cars become wrecked and block the track
      FLAG_COLOUR_YELLOW,               // Danger on the racing surface itself
      FLAG_COLOUR_DOUBLE_YELLOW,        // Danger that wholly or partly blocks the racing surface
      FLAG_COLOUR_BLACK_AND_WHITE,      // Unsportsmanlike conduct
      FLAG_COLOUR_BLACK_ORANGE_CIRCLE,  // Mechanical Failure
      FLAG_COLOUR_BLACK,                // Participant disqualified
      FLAG_COLOUR_CHEQUERED,            // Chequered flag
      //-------------
      FLAG_COLOUR_MAX
    };

    // (Type#6) Flag Reason (to be used with 'mHighestFlagReason')
    public enum eflagReason
    {
      FLAG_REASON_NONE = 0,
      FLAG_REASON_SOLO_CRASH,
      FLAG_REASON_VEHICLE_CRASH,
      FLAG_REASON_VEHICLE_OBSTRUCTION,
      //-------------
      FLAG_REASON_MAX
    };

    // (Type#7) Pit Mode (to be used with 'mPitMode')
    public enum ePitMode
    {
      PIT_MODE_NONE = 0,
      PIT_MODE_DRIVING_INTO_PITS,
      PIT_MODE_IN_PIT,
      PIT_MODE_DRIVING_OUT_OF_PITS,
      PIT_MODE_IN_GARAGE,
      PIT_MODE_DRIVING_OUT_OF_GARAGE,
      //-------------
      PIT_MODE_MAX
    };

    // (Type#8) Pit Stop Schedule (to be used with 'mPitSchedule')
    public enum ePitSchedule
    {
      PIT_SCHEDULE_NONE = 0,            // Nothing scheduled
      PIT_SCHEDULE_PLAYER_REQUESTED,    // Used for standard pit sequence - requested by player
      PIT_SCHEDULE_ENGINEER_REQUESTED,  // Used for standard pit sequence - requested by engineer
      PIT_SCHEDULE_DAMAGE_REQUESTED,    // Used for standard pit sequence - requested by engineer for damage
      PIT_SCHEDULE_MANDATORY,           // Used for standard pit sequence - requested by engineer from career enforced lap number
      PIT_SCHEDULE_DRIVE_THROUGH,       // Used for drive-through penalty
      PIT_SCHEDULE_STOP_GO,             // Used for stop-go penalty
      PIT_SCHEDULE_PITSPOT_OCCUPIED,    // Used for drive-through when pitspot is occupied
      //-------------
      PIT_SCHEDULE_MAX
    };

    // (Type#9) Car Flags (to be used with 'mCarFlags')
    public enum eCarFlags
    {
      CAR_HEADLIGHT         = (1<<0),
      CAR_ENGINE_ACTIVE     = (1<<1),
      CAR_ENGINE_WARNING    = (1<<2),
      CAR_SPEED_LIMITER     = (1<<3),
      CAR_ABS               = (1<<4),
      CAR_HANDBRAKE         = (1<<5),
    };

    // (Type#10) Tyre Flags (to be used with 'mTyreFlags')
    public enum eTyreFlags
    {
      TYRE_ATTACHED         = (1<<0),
      TYRE_INFLATED         = (1<<1),
      TYRE_IS_ON_GROUND     = (1<<2),
    };

    // (Type#11) Terrain Materials (to be used with 'mTerrain')
    public enum eTerrainMaterials
    {
      TERRAIN_ROAD = 0,
      TERRAIN_LOW_GRIP_ROAD,
      TERRAIN_BUMPY_ROAD1,
      TERRAIN_BUMPY_ROAD2,
      TERRAIN_BUMPY_ROAD3,
      TERRAIN_MARBLES,
      TERRAIN_GRASSY_BERMS,
      TERRAIN_GRASS,
      TERRAIN_GRAVEL,
      TERRAIN_BUMPY_GRAVEL,
      TERRAIN_RUMBLE_STRIPS,
      TERRAIN_DRAINS,
      TERRAIN_TYREWALLS,
      TERRAIN_CEMENTWALLS,
      TERRAIN_GUARDRAILS,
      TERRAIN_SAND,
      TERRAIN_BUMPY_SAND,
      TERRAIN_DIRT,
      TERRAIN_BUMPY_DIRT,
      TERRAIN_DIRT_ROAD,
      TERRAIN_BUMPY_DIRT_ROAD,
      TERRAIN_PAVEMENT,
      TERRAIN_DIRT_BANK,
      TERRAIN_WOOD,
      TERRAIN_DRY_VERGE,
      TERRAIN_EXIT_RUMBLE_STRIPS,
      TERRAIN_GRASSCRETE,
      TERRAIN_LONG_GRASS,
      TERRAIN_SLOPE_GRASS,
      TERRAIN_COBBLES,
      TERRAIN_SAND_ROAD,
      TERRAIN_BAKED_CLAY,
      TERRAIN_ASTROTURF,
      TERRAIN_SNOWHALF,
      TERRAIN_SNOWFULL,
      TERRAIN_DAMAGED_ROAD1,
      TERRAIN_TRAIN_TRACK_ROAD,
      TERRAIN_BUMPYCOBBLES,
      TERRAIN_ARIES_ONLY,
      TERRAIN_ORION_ONLY,
      TERRAIN_B1RUMBLES,
      TERRAIN_B2RUMBLES,
      TERRAIN_ROUGH_SAND_MEDIUM,
      TERRAIN_ROUGH_SAND_HEAVY,
      TERRAIN_SNOWWALLS,
      TERRAIN_ICE_ROAD,
      TERRAIN_RUNOFF_ROAD,
      TERRAIN_ILLEGAL_STRIP,

      //-------------
      TERRAIN_MAX
    };

    // (Type#12) Crash Damage State  (to be used with 'mCrashState')
    public enum eCrashDamageState
    {
      CRASH_DAMAGE_NONE = 0,
      CRASH_DAMAGE_OFFTRACK,
      CRASH_DAMAGE_LARGE_PROP,
      CRASH_DAMAGE_SPINNING,
      CRASH_DAMAGE_ROLLING,
      //-------------
      CRASH_MAX
    };
}
