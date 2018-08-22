using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using CrewChiefV4.GameState;
using CrewChiefV4.Audio;

namespace CrewChiefV4.Events
{

    public class AlarmClock : AbstractEvent
    {
        private DateTime alarmTime;
        private bool enabled = false;
        public static String wakeUp = "alarm_clock/alarms";
        public static String notifyYouAt = "alarm_clock/notify";

        public AlarmClock(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }
        void SetAlarm(DateTime alarmTime)
        {
            this.alarmTime = alarmTime;
            enabled = true;
        }
        public override void clearState()
        {

        }
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (enabled && DateTime.Now > alarmTime)
            {
                enabled = false;
                audioPlayer.playMessageImmediately(new QueuedMessage(AlarmClock.wakeUp, 0, null) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
            }
        }
        Tuple<int,int> getHourDigits(String voiceMessage)
        {
            foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.hourMappings)
            {
                foreach (String numberStr in entry.Key)
                {
                    if(voiceMessage.Contains(" " + numberStr + " "))
                    {
                        int index = voiceMessage.IndexOf(" " + numberStr + " ") + numberStr.Length + 2;
                        if (index != -1)
                        {
                            return new Tuple<int, int>(entry.Value, index);
                        }
                    }

                }
            }
            return new Tuple<int, int>(-1, -1);
        }
        int getMinuteDigits(String voiceMessage)
        {
            foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.minuteMappings)
            {
                foreach (String numberStr in entry.Key)
                {
                    if (voiceMessage.Equals(numberStr))
                    {
                        return entry.Value;
                    }
                }
            }
            return -1;
        }

        public override void respond(String voiceMessage)
        {
            int index = voiceMessage.Length;
            Boolean isPastMidDay = false;
            foreach (String ams in SpeechRecogniser.AM)
            {
                if(voiceMessage.Contains(" " + ams))
                {
                    index = voiceMessage.IndexOf(" " + ams);
                }
            }
            foreach (String pms in SpeechRecogniser.PM)
            {
                if(voiceMessage.Contains(" " + pms))
                {
                    index = voiceMessage.IndexOf(" " + pms);
                    isPastMidDay = true;
                }
            }
            voiceMessage = voiceMessage.Substring(0, index);
            Tuple<int, int> hour = getHourDigits(voiceMessage);
            String minutes = voiceMessage.Substring(hour.Item2);
            int minute = getMinuteDigits(minutes);
            if(hour.Item1 != -1 && minute != -1)
            {
                SetAlarm(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, isPastMidDay ? hour.Item1 + 12 : hour.Item1, minute, 00));
                Console.WriteLine("Alarm has been set to " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, isPastMidDay ? hour.Item1 + 12 : hour.Item1, minute, 00).ToString());
                List<MessageFragment> messages = new List<MessageFragment>();
                if (minute < 10)
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("alarm", MessageContents(AudioPlayer.folderAcknowlegeOK, notifyYouAt, hour, NumberReader.folderOh, minute ), 0, null));
                }
                else
                {
                    audioPlayer.playMessageImmediately(new QueuedMessage("alarm", MessageContents(AudioPlayer.folderAcknowlegeOK, notifyYouAt, hour, minute ), 0, null));
                }
            }
        }
    }
    
}
