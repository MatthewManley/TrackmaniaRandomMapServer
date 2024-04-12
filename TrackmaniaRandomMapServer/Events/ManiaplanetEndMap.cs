using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class ManiaplanetEndMap
    {
        [JsonProperty("count")]
        public int Count { get; set; }

        [JsonProperty("valid")]
        public int Valid { get; set; }

        [JsonProperty("map")]
        public ManiaplanetMap Map { get; set; }
    }
}
