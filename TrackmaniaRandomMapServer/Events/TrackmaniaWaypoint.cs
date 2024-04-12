using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace TrackmaniaRandomMapServer.Events
{
    public class TrackmaniaWaypoint
    {
        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }


        [JsonProperty("accountid")]
        public string AccountId { get; set; }

        [JsonProperty("nickname")]
        public string Nickname { get; set; }

        [JsonProperty("racetime")]
        public int RaceTime { get; set; }

        [JsonProperty("laptime")]
        public int LapTime { get; set; }

        [JsonProperty("checkpointinrace")]
        public int CheckpointInRace { get; set; }

        [JsonProperty("checkpointinlap")]
        public int CheckpointInLap { get; set; }

        [JsonProperty("isendrace")]
        public bool IsEndRace { get; set; }

        [JsonProperty("isendlap")]
        public bool IsEndLap { get; set; }

        [JsonProperty("isinfinitelaps")]
        public bool IsInfiniteLaps { get; set; }

        [JsonProperty("isindependentlaps")]
        public bool IsIndependentLaps { get; set; }

        [JsonProperty("curracecheckpoints")]
        public JArray CurRaceCheckpoints { get; set; }

        [JsonProperty("curlapcheckpoints")]
        public JArray CurLapCheckpoints { get; set; }

        [JsonProperty("blockid")]
        public string BlockId { get; set; }

        [JsonProperty("speed")]
        public float Speed { get; set; }
    }
}
