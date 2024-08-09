using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class ManiaplanetTurn
    {
        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("valid")]

        public bool Valid { get; set; }
    }
}
