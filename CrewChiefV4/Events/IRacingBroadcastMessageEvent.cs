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



    class IRacingBroadcastMessageEvent : AbstractEvent
    {
        public IRacingBroadcastMessageEvent(AudioPlayer audioPlayer)
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
                foreach (KeyValuePair<String, int> entry in SpeechRecogniser.bigNumberToNumber)
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
                    AddFuel(amount);
                    audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                    return;
                }
            }
            else if(SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_TEAROFF))
            {
                Tearoff();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_FAST_REPAIR))
            {
                FastRepair();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_ALL))
            {
                ClearAll();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_TYRES))
            {
                ClearTires();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_WIND_SCREEN))
            {
                ClearTearoff();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_FAST_REPAIR))
            {
                ClearFastRepair();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CLEAR_FUEL))
            {
                ClearFuel();
                audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                return;
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_ALL_TYRES) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_LEFT_TYRE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_RIGHT_TYRE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_LEFT_TYRE) ||
                SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_RIGHT_TYRE))
            {
                int amount = 0;
                foreach (KeyValuePair<String, int> entry in SpeechRecogniser.bigNumberToNumber)
                {
                    if (voiceMessage.Contains(" " + entry.Key))
                    {
                        amount = entry.Value;
                        break;
                    }
                }
                if(amount != 0)
                {
                    if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_ALL_TYRES))
                    {
                        ChangeTire(PitCommandModeTypes.LF, amount);
                        ChangeTire(PitCommandModeTypes.RF, amount);
                        ChangeTire(PitCommandModeTypes.LR, amount);
                        ChangeTire(PitCommandModeTypes.RR, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_LEFT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.LF, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_RIGHT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.RF, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_LEFT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.LR, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_RIGHT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.RR, amount);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                }
                else
                {
                    if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_ALL_TYRES))
                    {
                        ChangeTire(PitCommandModeTypes.LF, lastColdFLPressure);
                        ChangeTire(PitCommandModeTypes.RF, lastColdFRPressure);
                        ChangeTire(PitCommandModeTypes.LR, lastColdRLPressure);
                        ChangeTire(PitCommandModeTypes.RR, lastColdRRPressure);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_LEFT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.LF, lastColdFLPressure);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_FRONT_RIGHT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.RF, lastColdFRPressure);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_LEFT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.LR, lastColdRLPressure);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                    else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.PIT_STOP_CHANGE_REAR_RIGHT_TYRE))
                    {
                        ChangeTire(PitCommandModeTypes.RR, lastColdRRPressure);
                        audioPlayer.playMessageImmediately(new QueuedMessage(AudioPlayer.folderAcknowlegeOK, 0, null));
                        return;
                    }
                }                                   
            }
        }

        /// <summary>
        /// Schedule to add the specified amount of fuel (in liters) in the next pitstop.
        /// </summary>
        /// <param name="amount">The amount of fuel (in liters) to add. Use 0 to leave at current value.</param>
        public void AddFuel(int amount)
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.Fuel, amount, 0);
        }

        private void ChangeTire(PitCommandModeTypes type, int pressure)
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)type, pressure);
        }

        /// <summary>
        /// Schedule to change one or more tires and set their new pressures.
        /// </summary>
        /// <param name="change">The scheduled tire changes.</param>
        /*public void ChangeTires(PitCommandModeTypes change)
        {
            if (change.LeftFront != null && change.LeftFront.Change)
                ChangeTire(PitCommandModeTypes.LF, change.LeftFront.Pressure);

            if (change.RightFront != null && change.RightFront.Change)
                ChangeTire(PitCommandModeTypes.RF, change.RightFront.Pressure);

            if (change.LeftRear != null && change.LeftRear.Change)
                ChangeTire(PitCommandModeTypes.LR, change.LeftRear.Pressure);

            if (change.RightRear != null && change.RightRear.Change)
                ChangeTire(PitCommandModeTypes.RR, change.RightRear.Pressure);
        }*/

        /// <summary>
        /// Schedule to use a windshield tear-off in the next pitstop.
        /// </summary>
        public void Tearoff()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.WS, 0);
        }

        /// <summary>
        /// Schedule to use a fast repair in the next pitstop.
        /// </summary>
        public void FastRepair()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.FastRepair, 0);
        }

        /// <summary>
        /// Clear all pit commands.
        /// </summary>
        public static void ClearAll()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.Clear, 0);
        }

        /// <summary>
        /// Clear all tire changes.
        /// </summary>
        public void ClearTires()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearTires, 0);
        }

        /// <summary>
        /// Clear tearoff.
        /// </summary>
        public void ClearTearoff()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearWS, 0);
        }

        /// <summary>
        /// Clear fast repair.
        /// </summary>
        public void ClearFastRepair()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearFR, 0);
        }

        /// <summary>
        /// Clear clear fuel.
        /// </summary>
        public void ClearFuel()
        {
            iRacingSDK.BroadcastMessage(BroadcastMessageTypes.PitCommand, (int)PitCommandModeTypes.ClearFuel, 0);
        }
    }
}
