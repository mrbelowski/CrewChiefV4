using System.Collections.Generic;
using System.Globalization;

namespace CrewChiefV4.iRacing
{
    public class Track
    {
        public Track()
        {
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string CodeName { get; set; }
        public double Length { get; set; }
        public bool NightMode { get; set; }
        public bool IsOval { get; set; }
        public static Track FromSessionInfo(SessionInfo info)
        {
            var track = new Track();

            var query = info["WeekendInfo"];
            track.Id = Parser.ParseInt(query["TrackID"].GetValue());
            track.Name = query["TrackDisplayName"].GetValue();
            track.CodeName = query["TrackName"].GetValue();
            track.Length = Parser.ParseTrackLength(query["TrackLength"].GetValue());
            track.NightMode = query["WeekendOptions"]["NightMode"].GetValue() == "1";
            track.IsOval = query["Category"].GetValue() != "Road";

            return track;
        }


    }
}
