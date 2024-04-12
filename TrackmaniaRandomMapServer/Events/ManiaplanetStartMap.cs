using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class ManiaplanetStartMap
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("valid")]
        public int Valid { get; set; }

        [JsonProperty("restarted")]
        public bool Restarted { get; set; }

        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("map")]
        public ManiaplanetMap Map { get; set; }
    }
}
