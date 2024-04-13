using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Models
{
    public class UiModule
    {

        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("visible")]
        public bool Visible { get; set; }

        [JsonProperty("visible_update")]
        public bool VisibleUpdate { get; set; }
    }
}