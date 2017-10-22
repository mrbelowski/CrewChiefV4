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

        public string RaceLaps { get; set; }
        public double RaceTime { get; set; }

        public void Update(SessionInfo info, int sessionNumber)
        {
            this.Track = Track.FromSessionInfo(info);

            var weekend = info["WeekendInfo"];
            this.SubsessionId = Parser.ParseInt(weekend["SubSessionID"].GetValue());
            this.EventType = weekend["EventType"].GetValue();

            var session = info["SessionInfo"]["Sessions"]["SessionNum", sessionNumber];
            this.SessionType = session["SessionType"].GetValue();

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
