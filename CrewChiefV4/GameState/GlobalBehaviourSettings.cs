using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CrewChiefV4.GameState
{
    /**
     * Various flags and options pulled from car class and track data to override the app's
     * default behaviours. Needs to be accessed from anywhere by anything.
     *
     * realisticMode uses the list of enabled message types declared in the car class. The (current) possible types
     * are TYRE_TEMPS, TYRE_WEAR, BRAKE_TEMPS, BRAKE_DAMAGE, FUEL and LOCKING_AND_SPINNING. The idea here is that
     * modern expensive cars are bristling with telemetry devices and send huge amounts of data back to the overworked
     * Chief who'll pass on lots of info. Shitty old bangers have no telemetry and the Chief is generally too busy having
     * a fag to report what little data he has, so the richness of the data provided is car class dependent.
     * realisticMode also controls if useHundredths is pulled from car class. For most modern stuff this will be true,
     * for (much) older stuff it'll be false. It's supposed to reflect the accuracy of timing data available to driver
     * of whatever car you're in.
     *
     * The useOvalLogic flag is pulled from the track definition - enabling it means we only care about right side
     * tyre temps and don't care about brake temps, and will have a spotter enabled by default. And possibly other things.
     *
     * The useAmericanTerms is separate from useOvalLogic and is taken from the car class. It's for NASCAR and Indycar
     * and enables American announcements like "pace car" instead of "safety car", and white flag being the last lap.
     *
     *
     * The spotterEnabled flag starts out as whatever the use has set it to in properties screen. This is the initial
     * state of the spotter and it can be enabled (or disabled) at any time via a button or voice command. In realisticMode
     * the spotter will be 'off' at the start of each session unless you're on an oval (useOvalLogic is true).
     */
    class GlobalBehaviourSettings
    {
        private static float defaultSpotterVehicleLength = 4.5f;
        private static float defaultSpotterVehicleWidth = 1.8f;

        public static Boolean realisticMode = UserSettings.GetUserSettings().getBoolean("realistic_mode");
        public static Boolean alwaysUseHundredths = UserSettings.GetUserSettings().getBoolean("always_report_time_in_hundredths");
        public static Boolean defaultToAmericanTerms = UserSettings.GetUserSettings().getBoolean("use_american_terms");
        public static Boolean useAmericanTerms = false; // if true we use american phrasing where appropriate ("pace car" etc).
        public static Boolean useOvalLogic = false;    // if true, we don't care about cold brakes and cold left side tyres (?)
        public static Boolean useHundredths = false;
        public static float spotterVehicleLength = defaultSpotterVehicleLength;
        public static float spotterVehicleWidth = defaultSpotterVehicleWidth;

        public static Boolean spotterEnabledInitialState = UserSettings.GetUserSettings().getBoolean("enable_spotter");
        public static Boolean spotterEnabled = spotterEnabledInitialState;

        public static readonly List<MessageTypes> defaultEnabledMessageTypes = new List<MessageTypes> { 
            MessageTypes.TYRE_TEMPS, MessageTypes.TYRE_WEAR, MessageTypes.BRAKE_TEMPS, MessageTypes.BRAKE_DAMAGE, MessageTypes.FUEL, MessageTypes.LOCKING_AND_SPINNING };
        public static readonly List<MessageTypes> defaultBatteryPoweredEnabledMessageTypes = new List<MessageTypes> {
            MessageTypes.TYRE_TEMPS, MessageTypes.TYRE_WEAR, MessageTypes.BRAKE_TEMPS, MessageTypes.BRAKE_DAMAGE, MessageTypes.BATTERY, MessageTypes.LOCKING_AND_SPINNING };
        public static List<MessageTypes> enabledMessageTypes = new List<MessageTypes>();

        static GlobalBehaviourSettings()
        {
            enabledMessageTypes.AddRange(defaultEnabledMessageTypes);
        }

        public static void UpdateFromCarClass(CarData.CarClass carClass) 
        {
            useAmericanTerms = carClass.useAmericanTerms || defaultToAmericanTerms;
            useHundredths = carClass.timesInHundredths || alwaysUseHundredths;
            enabledMessageTypes.Clear();
            if (realisticMode && carClass.enabledMessageTypes != null && carClass.enabledMessageTypes.Length > 0)
            {
                parseMessageTypes(carClass.enabledMessageTypes);
            }
            else
            {
                enabledMessageTypes.AddRange(carClass.isBatteryPowered ? defaultBatteryPoweredEnabledMessageTypes : defaultEnabledMessageTypes);
            }

            if (carClass.spotterVehicleLength > 0)
            {
                spotterVehicleLength = carClass.spotterVehicleLength;
            }
            else
            {
                switch (CrewChief.gameDefinition.gameEnum)
                {
                    case GameEnum.PCARS_64BIT:
                    case GameEnum.PCARS_32BIT:
                    case GameEnum.PCARS2:
                    case GameEnum.PCARS_NETWORK:
                    case GameEnum.PCARS2_NETWORK:
                        spotterVehicleLength = UserSettings.GetUserSettings().getFloat("pcars_spotter_car_length");
                        break;
                    case GameEnum.RF1:
                        spotterVehicleLength = UserSettings.GetUserSettings().getFloat("rf1_spotter_car_length");
                        break;
                    case GameEnum.ASSETTO_64BIT:
                    case GameEnum.ASSETTO_32BIT:
                        spotterVehicleLength = UserSettings.GetUserSettings().getFloat("acs_spotter_car_length");
                        break;
                    case GameEnum.RF2_64BIT:
                        spotterVehicleLength = UserSettings.GetUserSettings().getFloat("rf2_spotter_car_length");
                        break;
                    case GameEnum.RACE_ROOM:
                        spotterVehicleLength = UserSettings.GetUserSettings().getFloat("r3e_spotter_car_length");
                        break;
                    default:
                        break;
                }
            }
            if (carClass.spotterVehicleWidth > 0)
            {
                spotterVehicleWidth = carClass.spotterVehicleWidth;
            }
            else
            {
                spotterVehicleWidth = defaultSpotterVehicleWidth;
            }

            Console.WriteLine("Enabled message types:");
            foreach (var m in GlobalBehaviourSettings.enabledMessageTypes)
            {
                Console.WriteLine('\t' + m.ToString());
            }

            Console.WriteLine("Spotter enabled: " + GlobalBehaviourSettings.spotterEnabled);
            Console.WriteLine("Realistic mode: " + GlobalBehaviourSettings.realisticMode);
            Console.WriteLine("Oval logic enabled: " + GlobalBehaviourSettings.useOvalLogic);
            Console.WriteLine("Using American terms: " + GlobalBehaviourSettings.useAmericanTerms);
        }

        public static void UpdateFromTrackDefinition(TrackDefinition trackDefinition)
        {
            useOvalLogic = trackDefinition.isOval;
            // this is called when we start a session, so update the spotter enabled flag based on the initial state
            spotterEnabled = spotterEnabledInitialState && (useOvalLogic || !realisticMode);
            if (useOvalLogic)
            {
                Console.WriteLine("Track is marked as oval");
            }
        }

        private static void parseMessageTypes(String messageTypes)
        {
            String[] messageTypesArray = messageTypes.Split(',');
            foreach (String messageType in messageTypesArray)
            {
                try
                {
                    MessageTypes messageTypeEnum = (MessageTypes)Enum.Parse(typeof(MessageTypes), messageType.Trim());
                    if (messageTypeEnum == MessageTypes.ALL)
                    {
                        enabledMessageTypes.Clear();
                        enabledMessageTypes.AddRange(defaultEnabledMessageTypes);
                        break;
                    }
                    else if (messageTypeEnum == MessageTypes.NONE)
                    {
                        enabledMessageTypes.Clear();
                        break;
                    }
                    else
                    {
                        enabledMessageTypes.Add(messageTypeEnum);
                    }
                }
                catch (Exception)
                {
                    Console.WriteLine("Unrecognised message type " + messageType);
                }
            }
        }
    }

    /**
     * enums for messages that can be disabled on a per-class basis.
     */
    public enum MessageTypes
    {
        TYRE_TEMPS, TYRE_WEAR, BRAKE_TEMPS, BRAKE_DAMAGE, FUEL, BATTERY, LOCKING_AND_SPINNING, ALL, NONE
    }
}
