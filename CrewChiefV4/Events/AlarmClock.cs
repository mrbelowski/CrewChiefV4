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
        private Boolean initialised = false;
        private List<Alarm> alarmTimes = new List<Alarm>();
        public static String wakeUp = "alarm_clock/alarms";
        public static String notifyYouAt = "alarm_clock/notify";
        public static String folderAM = "alarm_clock/am";
        public static String folderPM = "alarm_clock/pm";
        private static String preSetAlarms = UserSettings.GetUserSettings().getString("alarm_clock_times");

        private class Alarm
        {
            public Alarm(DateTime time, Boolean enabled)
            {
                this.alarmTime = time;
                this.enabled = enabled;
            }
            public DateTime alarmTime;
            public Boolean enabled;
        }
        public AlarmClock(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }
        void SetAlarm(DateTime alarmTime, Boolean enabled = true)
        {
            this.alarmTimes.Add(new Alarm(alarmTime, enabled));
        }
        public override void clearState()
        {
            initialised = false;
        }

        private void setFromProperties()
        {
            if (preSetAlarms != null && preSetAlarms.Length > 0)
            {
                String[] alarms = preSetAlarms.Split(';');
                foreach (String alarm in alarms)
                {
                    int hours = -1;
                    int minutes = -1;
                    int index = alarm.IndexOf(":");
                    String hourString = alarm.Substring(0, index);
                    Int32.TryParse(hourString, out hours);
                    String minutesString = alarm.Substring(index + 1);
                    if (minutesString.ToLower().Contains("pm"))
                    {
                        hours = hours + 12;
                        minutesString = minutesString.Substring(0, minutesString.Length - 2);
                    }
                    else if (minutesString.ToLower().Contains("am"))
                    {
                        minutesString = minutesString.Substring(0, minutesString.Length - 2);
                    }

                    Int32.TryParse(minutesString, out minutes);
                    if (hours != -1 && minutes != -1)
                    {
                        DateTime now = DateTime.Now;
                        SetAlarm(new DateTime(now.Year, now.Month, now.Day, hours, minutes, 00));
                        Console.WriteLine("Alarm has been set to " + new DateTime(now.Year, now.Month, now.Day, hours, minutes, 00).ToString());
                    }
                }
            }

        }
        override protected void triggerInternal(GameStateData previousGameState, GameStateData currentGameState)
        {
            if (!initialised)
            {
                setFromProperties();
                initialised = true;
            }
            foreach (Alarm alarm in alarmTimes)
            {
                if (alarm.enabled && (DateTime.Now > alarm.alarmTime && DateTime.Now < alarm.alarmTime + TimeSpan.FromSeconds(60)))
                {
                    alarm.enabled = false;
                    audioPlayer.playMessageImmediately(new QueuedMessage(AlarmClock.wakeUp, 0, null) { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 15) });
                }
            }
        }
        Tuple<int, int> getHourDigits(String voiceMessage)
        {
            foreach (KeyValuePair<String[], int> entry in SpeechRecogniser.hourMappings)
            {
                foreach (String numberStr in entry.Key)
                {
                    if (voiceMessage.Contains(" " + numberStr + " "))
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

            if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.SET_ALARM_CLOCK))
            {
                int index = voiceMessage.Length;
                Boolean isPastMidDay = false;
                Boolean isAfterMidnight = false;
                foreach (String ams in SpeechRecogniser.AM)
                {
                    if (voiceMessage.Contains(" " + ams))
                    {
                        index = voiceMessage.IndexOf(" " + ams);
                        isAfterMidnight = true;
                    }
                }
                foreach (String pms in SpeechRecogniser.PM)
                {
                    if (voiceMessage.Contains(" " + pms))
                    {
                        index = voiceMessage.IndexOf(" " + pms);
                        isPastMidDay = true;
                    }
                }
                voiceMessage = voiceMessage.Substring(0, index);
                Tuple<int, int> hourWithIndex = getHourDigits(voiceMessage);
                int hour = isPastMidDay ? hourWithIndex.Item1 + 12 : hourWithIndex.Item1;
                if (hour >= 12 && !isAfterMidnight)
                {
                    isPastMidDay = true;
                }
                String minutesString = voiceMessage.Substring(hourWithIndex.Item2);
                int minutes = getMinuteDigits(minutesString);
                if (hourWithIndex.Item1 != -1 && minutes != -1)
                {
                    SetAlarm(new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minutes, 00));
                    Console.WriteLine("Alarm has been set to " + new DateTime(DateTime.Now.Year, DateTime.Now.Month, DateTime.Now.Day, hour, minutes, 00).ToString());
                    if (hour == 0)
                    {
                        // not 100% sure its needed but there just in case.
                        isPastMidDay = false;
                        hour = 24;
                    }
                    if (hour > 12)
                    {
                        hour = hour - 12;
                    }
                    if (minutes < 10)
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("alarm",
                            MessageContents(notifyYouAt, hour, NumberReader.folderOh, minutes, isPastMidDay ? folderPM : folderAM), 0, null)
                            { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 0) });
                    }
                    else
                    {
                        audioPlayer.playMessageImmediately(new QueuedMessage("alarm",
                            MessageContents(notifyYouAt, hour, minutes, isPastMidDay ? folderPM : folderAM), 0, null)
                            { metadata = new SoundMetadata(SoundType.CRITICAL_MESSAGE, 0) });
                    }
                }
            }
            else if (SpeechRecogniser.ResultContains(voiceMessage, SpeechRecogniser.CLEAR_ALARM_CLOCK))
            {
                alarmTimes.Clear();
                audioPlayer.playMessageImmediately(new QueuedMessage("alarm", MessageContents(AudioPlayer.folderAcknowlegeOK), 0, null));
            }

        }
    }
}
