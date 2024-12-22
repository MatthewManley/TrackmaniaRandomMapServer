using Newtonsoft.Json;

namespace TrackmaniaExchangeAPI.Models
{
    public class MapTag
    {
        [JsonProperty("TagId")]
        public int TagId { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Color")]
        public string Color { get; set; }
    }
}