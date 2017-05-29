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
        private static float defaultSpotterCarLength = 4.5f;
        private static float defaultSpotterCarWidth = 1.8f;

        public static Boolean useAmericanTerms = false; // if true we use american phrasing where appropriate ("pace car" etc).
        public static Boolean usePaceCarAndCaution = false;
        public static Boolean useOvalLogic = false;    // if true, we don't care about cold brakes and cold left side tyres (?)
        public static Boolean useHundredths = false;
        public static float spotterCarLength = defaultSpotterCarLength;   
        public static float spotterCarWidth = defaultSpotterCarWidth;  
        
        private static List<MessageTypes> enabledMessageTypes = new List<MessageTypes> { MessageTypes.SPOTTER, 
            MessageTypes.TYRE_TEMPS, MessageTypes.TYRE_WEAR, MessageTypes.BRAKE_TEMPS, MessageTypes.BRAKE_DAMAGE, MessageTypes.FUEL };
        
        public static void UpdateFromCarClass(CarData.CarClass carClass) 
        {
            useAmericanTerms = carClass.useAmericanTerms;
            useHundredths = carClass.timesInHundredths;
            enabledMessageTypes = carClass.enabledMessageTypes;

            if (carClass.spotterVehicleLength > 0)
            {
                spotterCarLength = carClass.spotterVehicleLength;
            }
            else
            {
                switch (CrewChief.gameDefinition.gameEnum)
                {
                    case GameEnum.PCARS_64BIT:
                    case GameEnum.PCARS_32BIT:
                    case GameEnum.PCARS_NETWORK:
                        spotterCarLength = UserSettings.GetUserSettings().getFloat("pcars_spotter_car_length");
                        break;
                    case GameEnum.RF1:
                        spotterCarLength = UserSettings.GetUserSettings().getFloat("rf1_spotter_car_length");
                        break;
                    case GameEnum.ASSETTO_64BIT:
                    case GameEnum.ASSETTO_32BIT:
                        spotterCarLength = UserSettings.GetUserSettings().getFloat("acs_spotter_car_length");
                        break;
                    case GameEnum.RF2_64BIT:
                        spotterCarLength = UserSettings.GetUserSettings().getFloat("rf2_spotter_car_length");
                        break;
                    case GameEnum.RACE_ROOM:
                        spotterCarLength = UserSettings.GetUserSettings().getFloat("r3e_spotter_car_length");
                        break;
                    default:
                        break;
                }
            }
            if (carClass.spotterVehicleWidth > 0)
            {
                spotterCarWidth = carClass.spotterVehicleWidth;
            }
            else
            {
                spotterCarWidth = defaultSpotterCarWidth;
            }
        }

        public static void UpdateFromTrackDefinition(TrackDefinition trackDefinition)
        {
            useOvalLogic = trackDefinition.isOval;
        }
    }

    /**
     * enums for messages that can be disabled on a per-class basis.
     */
    public enum MessageTypes
    {
        SPOTTER, TYRE_TEMPS, TYRE_WEAR, BRAKE_TEMPS, BRAKE_DAMAGE, FUEL
    }
}
