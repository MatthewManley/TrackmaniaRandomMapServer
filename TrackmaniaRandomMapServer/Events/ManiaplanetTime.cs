using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class ManiaplanetTime
    {
        [JsonProperty("count")]
        public int Time { get; set; }
    }
}
