using System.Collections.Generic;
using System.Globalization;
using iRSDKSharp;
namespace CrewChiefV4.iRacing
{
    public class Track
    {
        public Track()
        {
            IsOval = false;
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string CodeName { get; set; }
        public double Length { get; set; }
        public bool NightMode { get; set; }
        public bool IsOval { get; set; }
        public string Category { get; set; }
        public string TrackType { get; set; }
        public static Track FromSessionInfo(string sessionString)
        {
            var track = new Track();
            track.Id = Parser.ParseInt(YamlParser.Parse(sessionString, "WeekendInfo:TrackID:"));
            track.Name = YamlParser.Parse(sessionString, "WeekendInfo:TrackDisplayName:");
            track.CodeName = YamlParser.Parse(sessionString, "WeekendInfo:TrackName:");
            track.Length = Parser.ParseTrackLength(YamlParser.Parse(sessionString, "WeekendInfo:TrackLength:"));            
            track.NightMode = YamlParser.Parse(sessionString, "WeekendInfo:NightMode:") == "1";
            track.Category = YamlParser.Parse(sessionString, "WeekendInfo:Category:");
            track.TrackType = YamlParser.Parse(sessionString, "WeekendInfo:TrackType:");
            track.IsOval = track.Category.ToLower().Contains("oval") && !track.TrackType.Equals("road course");

            return track;
        }


    }
}
