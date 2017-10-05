using System.Collections.Generic;
using System.Globalization;

namespace CrewChiefV4.iRacing
{
    public class Track
    {
        private readonly List<Sector> _sectors;

        public Track()
        {
            _sectors = new List<Sector>();
        }

        public int Id { get; set; }
        public string Name { get; set; }
        public string CodeName { get; set; }
        public double Length { get; set; }
        public bool NightMode { get; set; }

        public List<Sector> Sectors
        {
            get { return _sectors; }
        }

        public static Track FromSessionInfo(SessionInfo info)
        {
            var track = new Track();

            var query = info["WeekendInfo"];
            track.Id = Parser.ParseInt(query["TrackID"].GetValue());
            track.Name = query["TrackDisplayName"].GetValue();
            track.CodeName = query["TrackName"].GetValue();
            track.Length = Parser.ParseTrackLength(query["TrackLength"].GetValue());
            track.NightMode = query["WeekendOptions"]["NightMode"].GetValue() == "1";

            // Parse sectors
            track.Sectors.Clear();
            query = info["SplitTimeInfo"]["Sectors"];

            int nr = 0;
            while (nr >= 0)
            {
                var pctString = query["SectorNum", nr]["SectorStartPct"].GetValue();
                float pct;
                if (string.IsNullOrWhiteSpace(pctString) || !float.TryParse(pctString, NumberStyles.AllowDecimalPoint, 
                    CultureInfo.InvariantCulture, out pct))
                {
                    break;
                }

                var sector = new Sector();
                sector.Number = nr;
                sector.StartPercentage = pct;
                track.Sectors.Add(sector);

                nr++;
            }

            return track;
        }


    }
}
