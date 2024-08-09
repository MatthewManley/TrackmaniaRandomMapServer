using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class TrackmaniaSkipOutro
    {
        [JsonProperty("time")]
        public int Time { get; set; }

        [JsonProperty("login")]
        public string Login { get; set; }

        [JsonProperty("accountid")]
        public string AccountId { get; set; }
    }
}
