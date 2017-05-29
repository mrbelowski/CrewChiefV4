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
     */
    class GlobalBehaviourSettings
    {
        private static float defaultSpotterVehicleLength = 4.5f;
        private static float defaultSpotterVehicleWidth = 1.8f;

        public static Boolean realisticMode = UserSettings.GetUserSettings().getBoolean("realistic_mode");
        public static Boolean useAmericanTerms = false; // if true we use american phrasing where appropriate ("pace car" etc).
        public static Boolean useOvalLogic = false;    // if true, we don't care about cold brakes and cold left side tyres (?)
        public static Boolean useHundredths = false;
        public static float spotterVehicleLength = defaultSpotterVehicleLength;
        public static float spotterVehicleWidth = defaultSpotterVehicleWidth;

        public static Boolean spotterEnabled = UserSettings.GetUserSettings().getBoolean("enable_spotter");

        public static List<MessageTypes> defaultEnabledMessageTypes = new List<MessageTypes> { 
            MessageTypes.TYRE_TEMPS, MessageTypes.TYRE_WEAR, MessageTypes.BRAKE_TEMPS, MessageTypes.BRAKE_DAMAGE, MessageTypes.FUEL };
        public static List<MessageTypes> enabledMessageTypes = new List<MessageTypes>();
        
        public static void UpdateFromCarClass(CarData.CarClass carClass) 
        {
            useAmericanTerms = carClass.useAmericanTerms;
            useHundredths = carClass.timesInHundredths;
            enabledMessageTypes.Clear();            
            if (realisticMode && carClass.enabledMessageTypes != null && carClass.enabledMessageTypes.Length > 0)
            {
                parseMessageTypes(carClass.enabledMessageTypes);
            }
            else
            {
                enabledMessageTypes.AddRange(defaultEnabledMessageTypes);
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
                    case GameEnum.PCARS_NETWORK:
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
        }

        public static void UpdateFromTrackDefinition(TrackDefinition trackDefinition)
        {
            useOvalLogic = trackDefinition.isOval;
            if (realisticMode && !useOvalLogic)
            {
                spotterEnabled = false;
            }
        }

        private static void parseMessageTypes(String messageTypes)
        {
            String[] messageTypesArray = messageTypes.Split(',');
            foreach (String messageType in messageTypesArray)
            {
                try
                {
                    enabledMessageTypes.Add((MessageTypes)Enum.Parse(typeof(MessageTypes), messageType));
                }
                catch (Exception)
                {
                    Console.WriteLine("Unrecognised message type " + messageType);
                }
            }
            Console.WriteLine("enabling message types " + String.Join(", ", enabledMessageTypes));            
        }
    }

    /**
     * enums for messages that can be disabled on a per-class basis.
     */
    public enum MessageTypes
    {
        TYRE_TEMPS, TYRE_WEAR, BRAKE_TEMPS, BRAKE_DAMAGE, FUEL
    }
}
