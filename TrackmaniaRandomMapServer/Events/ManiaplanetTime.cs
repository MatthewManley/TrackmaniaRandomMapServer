using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class ManiaplanetTime
    {
        [JsonProperty("time")]
        public int Time { get; set; }
    }
}
