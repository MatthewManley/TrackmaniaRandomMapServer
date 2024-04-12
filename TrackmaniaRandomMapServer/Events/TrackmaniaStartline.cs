using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class TrackmaniaStartline
    {
        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("accountid")]
        public string AccountId { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }
    }
}
