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
            "messages_volume", "last_game_definition", "REPEAT_LAST_MESSAGE_BUTTON", "UpdateSettings", "VOLUME_UP", "VOLUME_DOWN", ControllerConfiguration.ControllerData.PROPERTY_CONTAINER};
        private UserSettings()
        {
            // Copy user settings from previous application version if necessary
            if (Properties.Settings.Default.UpdateSettings)
            {
                Properties.Settings.Default.UpdateSettings = false;
                try
                {
                    Properties.Settings.Default.Upgrade();
                    Properties.Settings.Default.Save();
                }
                catch
                {
                    Console.WriteLine("Unable to upgrade properties from previous version, settings will be reset to default");
                }
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
