using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4
{
    class UserSettings
    {
        private String[] reservedNameStarts = new String[] { "CHANNEL_", "TOGGLE_", "VOICE_OPTION", "background_volume", 
            "messages_volume", "last_game_definition", "REPEAT_LAST_MESSAGE_BUTTON", "UpdateSettings"};
        public Dictionary<String, String> propertyHelp = new Dictionary<String, String>();
        private UserSettings()
        {
            // Copy user settings from previous application version if necessary
            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.Upgrade();
                Properties.Settings.Default.UpdateSettings = false;
                Properties.Settings.Default.Save();
            }
            propertyHelp.Add("sound_files_path", "The path (relative to CrewChief.exe) of the sound pack you want to use");
            propertyHelp.Add("background_volume", "The volume of the background sounds (0 - 1)");
            propertyHelp.Add("update_interval", "The time (milliseconds) between app updates");
            propertyHelp.Add("use_sweary_messages", "A few messages contain swearing - then enables / disables these");
            propertyHelp.Add("enable_spotter", "The spotter can be enabled and disabled with a button. This setting sets it initial state");
            propertyHelp.Add("speech_recognition_location", "Optional - the localisation to use for speech recognition. If this isn't set, the app will use which ever English pack you've installed (or the first it finds if you have more than one). If you set it, the value must be en-[something].");
            propertyHelp.Add("spotter_car_length", "The length of a car, used to check if there's an overlap. Decrease this if the spotter calls 'hold your line' when you're not overlapping. "+
                "Increase it if the spotter doesn't call 'hold your line' when you clearly are overlapping");
            propertyHelp.Add("time_after_race_start_for_spotter", "Wait this many seconds after race start before enabling the spotter");
            propertyHelp.Add("min_speed_for_spotter", "Don't use the spotter if your speed is less than this");
            propertyHelp.Add("max_closing_speed_for_spotter", "Don't call 'hold your line' if the closing speed between you and the other car is greater than this");
            propertyHelp.Add("spotter_only_when_being_passed", "Only 'spot' for cars overtaking you");
            propertyHelp.Add("spotter_clear_delay", "You need to be clear for this many milliseconds before the spotter calls 'clear'");
            propertyHelp.Add("spotter_overlap_delay", "You need to be overlapping for this many milliseconds before the spotter calls 'hold your line'");
            propertyHelp.Add("custom_device_guid", "Manually set a controller GUID if the app doesn't display your controller in the devices list");
            propertyHelp.Add("disable_immediate_messages", "Disables all spotter messages and all voice recognition responses. " +
                "Might allow the app to run in non-interactive mode on slow systems");
            propertyHelp.Add("max_safe_oil_temp_over_baseline", "Baseline oil temp is taken after a few minutes. If the oil temp goes more than this value over the baseline " +
                "a warning message is played. Reduce this to make the 'high oil temp' warning more sensitive");
            propertyHelp.Add("max_safe_water_temp_over_baseline", "Baseline water temp is taken after a few minutes. If the water temp goes more than this value over the baseline " +
                "a warning message is played. Reduce this to make the 'high water temp' warning more sensitive");
            propertyHelp.Add("r3e_launch_params", "This is used to tell Steam what app to run - raceroom's Steam app ID is 211500. If you're using a non-Steam version of RaceRoom this can be empty");
            propertyHelp.Add("r3e_launch_exe", "This is the program used to start RaceRoom. For most users this should be the full path to their steam.exe "+
                "(e.g. C:/program files/steam/steam.exe). Use forward slashes to separate paths. If you have a non-steam version of RaceRoom use the full path to the RaceRoom exe");
            propertyHelp.Add("launch_raceroom", "If this is true the application will attempt to launch RaceRoom when CrewChief starts (when you click 'Start application'" + 
                " or the app auto starts CrewChief if you set run_immediately to true)");
            propertyHelp.Add("run_immediately", "If this is true the application will start running CrewChief as soon as you start it up, using whatever options you previously set");
            propertyHelp.Add("pcars_steam_id", "PCars sometimes sends the participant data in the wrong order. If this value " +
                "is set, the app will use it to try and work around this issue, preventing the app 'seeing' the wrong data for the player");
            propertyHelp.Add("cumulative_lap_lockup_warning_threshold", "If the player has locked wheels for more than this many seconds over the course of a lap, play a warning");
            propertyHelp.Add("cumulative_lap_wheelspin_warning_threshold", "If the player has wheelspin for more than this many seconds over the course of a lap, play a warning");
            propertyHelp.Add("minimum_name_voice_recognition_confidence", "When processing voice commands asking about opponent drivers, the speech recognition engine must be at least this confident it understood before the app responds (0.0 - 1.0, default 0.4)");
            propertyHelp.Add("minimum_voice_recognition_confidence", "When processing voice commands, the speech recognition engine must be at least this confident it understood before the app responds (0.0 - 1.0, default 0.5)");
            propertyHelp.Add("display_session_lap_times", "Write all the lap times from the completed session to the console (separated by ;)");
            propertyHelp.Add("enable_opponent_laptime_reporting_in_race", "Call out opponent lap times in the race (car front, behind & leader) if the lap time is their best lap.");
            propertyHelp.Add("report_sector_deltas_race", "Give sector delta information during a race.");
            propertyHelp.Add("report_sector_deltas_race_likelihood", "Probability of giving a sectors report after each race lap (0 = disabled, 1 = every lap, default 0.3 - 30% chance of sector information on each lap)");
            propertyHelp.Add("report_sector_deltas_practice_and_qual", "Give sector delta information during practice and qualifying.");
            propertyHelp.Add("frequency_of_car_close_ahead_reports", "How often to report a car close behind (being pressured). 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_car_close_behind_reports", "How often to report a car close in front (pressuring an opponent). 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_gap_ahead_reports", "How often to report a the gap to the car in front. 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_gap_behind_reports", "How often to report a the gap to the car behind. 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_opponent_race_lap_times", "How often to report fast laps times for race leader and car in front and behind. 0 (never) to 10 (as often as possible). If this is 1, times are only report if they're the session best. This criteria becomes less strict up to 10 where the time just needs to be a few tenths off session best.");
            propertyHelp.Add("frequency_of_pearls_of_wisdom", "How often to play general encouragement messages. 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_player_race_lap_time_reports", "How often to report player lap times in the race. 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_prac_and_qual_sector_delta_reports", "How often to report player sector deltas in practice and qualifying. 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("frequency_of_race_sector_delta_reports", "How often to report a player sector deltas in race. 0 (never) to 10 (as often as possible)");
            propertyHelp.Add("race_sector_reports_at_each_sector", "Play sector delta reports as you complete each sector");
            propertyHelp.Add("race_sector_reports_at_lap_end", "Play sector delta reports for whole lap when you complete the lap");
            propertyHelp.Add("ambient_temp_check_interval_seconds", "How often to check for changes in ambient (air) temperature, in seconds");
            propertyHelp.Add("track_temp_check_interval_seconds", "How often to check for changes in track temperature, in seconds");
            propertyHelp.Add("report_ambient_temp_changes_greater_than", "Report ambient (air) temperature if it's changed by more than this amount (in celsius)");
            propertyHelp.Add("report_track_temp_changes_greater_than", "Report track temperature if it's changed by more than this amount (in celsius)");
            propertyHelp.Add("pause_between_messages", "Time pause between messages (in seconds) when reading multiple messages together in a single group.");
        }

        public String getHelp(String propertyId)
        {
            if (propertyHelp.ContainsKey(propertyId))
            {
                return propertyHelp[propertyId];
            }
            else
            {
                return "";
            }
        }

        public List<SettingsProperty> getProperties(Type requiredType, String nameMustStartWith, String nameMustNotStartWith)
        {
            List<SettingsProperty> props = new List<SettingsProperty>();
            foreach (SettingsProperty prop in Properties.Settings.Default.Properties)
            {
                Boolean isReserved = false;
                foreach (String reservedNameStart in reservedNameStarts)
                {
                    if (prop.Name.StartsWith(reservedNameStart))
                    {
                        isReserved = true;
                        break;
                    }
                }
                if (!isReserved && 
                    (nameMustStartWith == null || nameMustStartWith.Length == 0 || prop.Name.StartsWith(nameMustStartWith)) &&
                    (nameMustNotStartWith == null || nameMustNotStartWith.Length == 0 || !prop.Name.StartsWith(nameMustNotStartWith)) &&
                    !prop.IsReadOnly && prop.PropertyType == requiredType)
                {
                    props.Add(prop);
                }
            }
            return props.OrderBy(x => x.Name).ToList();
        }

        private static readonly UserSettings _userSettings = new UserSettings();

        private Boolean propertiesUpdated = false;

        public static UserSettings GetUserSettings()
        {
            return _userSettings;
        }

        public String getString(String name)
        {
            return (String)Properties.Settings.Default[name];
        }

        public float getFloat(String name)
        {
            return (float) Properties.Settings.Default[name];
        }

        public Boolean getBoolean(String name)
        {
            return (Boolean)Properties.Settings.Default[name];
        }

        public int getInt(String name)
        {
            return (int)Properties.Settings.Default[name];
        }

        public void setProperty(String name, Object value)
        {
            if (value != Properties.Settings.Default[name])
            {
                Properties.Settings.Default[name] = value;
                propertiesUpdated = true;
            }
        }

        public void saveUserSettings()
        {
            if (propertiesUpdated)
            {
                Properties.Settings.Default.Save();
            }
        }
    }
}
