using System.Collections.Generic;
using System.Linq;

namespace CrewChiefV4.iRacing
{
    public class SessionData
    {
        public SessionData()
        {

        }

        public Track Track { get; set; }
        public string EventType { get; set; }
        public string SessionType { get; set; }
        public int SubsessionId { get; set; }

        public double SessionTime { get; set; }
        public string SessionTimeString { get; set; }
        public double TimeRemaining { get; set; }
        public int LeaderLap { get; set; }

        public bool TrackCleanup { get; set; }
        public bool DynamicTrack { get; set; }
        public TrackConditions.TrackUsageTypes TrackUsage { get; set; }
        public string TrackUsageText { get; set; }

        public string RaceLaps { get; set; }
        public double RaceTime { get; set; }




        public SessionFlags Flags { get; set; }
        public SessionStates State { get; set; }

        /// <summary>
        /// Is the checkered flag shown? (e.g. winner has passed the finish, but other drivers may still be racing)
        /// </summary>
        public bool IsCheckered { get; set; }
        /// <summary>
        /// Is the session finished? (e.g. all drivers have finished and session is in cool-down)
        /// </summary>
        public bool IsFinished { get; set; }

        public void Update(SessionInfo info, int sessionNumber)
        {
            this.Track = Track.FromSessionInfo(info);

            var weekend = info["WeekendInfo"];
            this.SubsessionId = Parser.ParseInt(weekend["SubSessionID"].GetValue());
            this.EventType = weekend["EventType"].GetValue();

            var session = info["SessionInfo"]["Sessions"]["SessionNum", sessionNumber];
            this.SessionType = session["SessionType"].GetValue();

            this.TrackUsageText = session["SessionTrackRubberState"].GetValue();
            this.TrackUsage = TrackConditions.TrackUsageFromString(this.TrackUsageText);
            
            this.TrackCleanup = weekend["TrackCleanup"].GetValue() == "1"; 
            this.DynamicTrack = weekend["TrackDynamicTrack"].GetValue() == "1";

            var laps = session["SessionLaps"].GetValue();
            this.SessionTimeString = session["SessionTime"].GetValue();
            
            var time = Parser.ParseSec(SessionTimeString);
            
            this.RaceLaps = laps;
            this.RaceTime = time;
        }

        public void Update(iRacingData telemetry)
        {
            this.SessionTime = telemetry.SessionTime;
            this.TimeRemaining = telemetry.SessionTimeRemain;
            this.Flags = telemetry.SessionFlags;
        }

        public void UpdateState(SessionStates state)
        {
            this.State = state;
            this.IsFinished = state == SessionStates.CoolDown;
            this.IsCheckered = (state == SessionStates.CoolDown || state == SessionStates.Checkered);
        }

        public bool IsLimitedSessionLaps
        {
            get
            {
                return RaceLaps.ToLower() != "unlimited";
            }
        }

        public bool IsLimitedTime
        {
            get
            {
                return SessionTimeString.ToLower() != "unlimited";
            }
        }

    }
}
