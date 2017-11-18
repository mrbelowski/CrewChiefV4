using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CrewChiefV4.Audio;
using CrewChiefV4.GameState;
using CrewChiefV4.iRacing;
using iRSDKSharp;

namespace CrewChiefV4.Events
{
        /// <summary>
    /// Provides control over the pit commands.
    /// </summary>
    public class PitCommandControl
    {
        /// <summary>
        /// Schedule to add the specified amount of fuel (in liters) in the next pitstop.
        /// </summary>
        /// <param name="amount">The amount of fuel (in liters) to add. Use 0 to leave at current value.</param>
        static public void AddFuel(int amount)
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.Fuel, amount, 0);
        }

        static private void ChangeTire(PitCommandModeTypes type, int pressure)
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)type, pressure);
        }

        /// <summary>
        /// Schedule to change one or more tires and set their new pressures.
        /// </summary>
        /// <param name="change">The scheduled tire changes.</param>
        static public void ChangeTires(TireChange change)
        {
            if (change.LeftFront != null && change.LeftFront.Change)
                ChangeTire(PitCommandModeTypes.LF, change.LeftFront.Pressure);

            if (change.RightFront != null && change.RightFront.Change)
                ChangeTire(PitCommandModeTypes.RF, change.RightFront.Pressure);

            if (change.LeftRear != null && change.LeftRear.Change)
                ChangeTire(PitCommandModeTypes.LR, change.LeftRear.Pressure);

            if (change.RightRear != null && change.RightRear.Change)
                ChangeTire(PitCommandModeTypes.RR, change.RightRear.Pressure);
        }
        
        /// <summary>
        /// Schedule to use a windshield tear-off in the next pitstop.
        /// </summary>
        public static void Tearoff()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.WS, 0);
        }

        /// <summary>
        /// Schedule to use a fast repair in the next pitstop.
        /// </summary>
        public static void FastRepair()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.FastRepair, 0);
        }

        /// <summary>
        /// Clear all pit commands.
        /// </summary>
        public static void Clear()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.Clear, 0);
        }

        /// <summary>
        /// Clear all tire changes.
        /// </summary>
        public static void ClearTires()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearTires, 0);
        }

        /// <summary>
        /// Clear tearoff.
        /// </summary>
        public static void ClearTearoff()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearWS, 0);
        }

        /// <summary>
        /// Clear fast repair.
        /// </summary>
        public static void ClearFastRepair()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearFR, 0);
        }

        /// <summary>
        /// Clear clear fuel.
        /// </summary>
        public static void ClearFuel()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearFuel, 0);
        }

        public class Tire
        {
            internal Tire() { }

            /// <summary>
            /// Whether or not to change this tire.
            /// </summary>
            public bool Change { get; set; }

            /// <summary>
            /// The new pressure (in kPa) of this tire.
            /// </summary>
            public int Pressure { get; set; }
        }

        /// <summary>
        /// Encapsulates scheduled tire changes for each of the four tires separately.
        /// </summary>
        public class TireChange
        {
            public TireChange()
            {
                this.LeftFront = new Tire();
                this.RightFront = new Tire();
                this.LeftRear = new Tire();
                this.RightRear = new Tire();
            }

            public Tire LeftFront { get; set; }
            public Tire RightFront { get; set; }
            public Tire LeftRear { get; set; }
            public Tire RightRear { get; set; }
        }
    }

    class PitStopCommands : AbstractEvent
    {
        public PitStopCommands(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
            this.lastColdFLPressure = -1;
            this.lastColdFRPressure = -1;
            this.lastColdRLPressure = -1;
            this.lastColdRRPressure = -1;
        }
        int lastColdFLPressure = -1;
        int lastColdFRPressure = -1;
        int lastColdRLPressure = -1;
        int lastColdRRPressure = -1;

        public override void clearState()
        {
            this.lastColdFLPressure = -1;
            this.lastColdFRPressure = -1;
            this.lastColdRLPressure = -1;
            this.lastColdRRPressure = -1;
        }
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            lastColdFLPressure = (int)currentGameState.TyreData.FrontLeftPressure;
            lastColdFRPressure = (int)currentGameState.TyreData.FrontRightPressure;
            lastColdRLPressure = (int)currentGameState.TyreData.RearLeftPressure;
            lastColdRRPressure = (int)currentGameState.TyreData.RearRightPressure;
        }
        public override void respond(String voiceMessage)
        {
            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_ADD))
            {
                int amount = 0;
                foreach (KeyValuePair<String, int> entry in SpeechRecogniser.numberToNumber)
                {
                    if (voiceMessage.Contains(" " + entry.Key + " "))
                    {
                        amount = entry.Value;
                        break;
                    }
                }
                if (amount == 0)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderDidntUnderstand, 0, null));
                    return;
                }
                if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.LITERS))
                {
                    PitCommandControl.AddFuel(amount);
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                    return;
                }
            }
            else if(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_TEAROFF))
            {
                PitCommandControl.Tearoff();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_FAST_REPAIR))
            {
                PitCommandControl.FastRepair();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_ALL))
            {
                PitCommandControl.Clear();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_TYRES))
            {
                PitCommandControl.ClearTires();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_WIND_SCREEN))
            {
                PitCommandControl.ClearTearoff();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_FAST_REPAIR))
            {
                PitCommandControl.ClearFastRepair();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_FUEL))
            {
                PitCommandControl.ClearFuel();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }

        }
    }
}
