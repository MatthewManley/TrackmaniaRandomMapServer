using Newtonsoft.Json;

namespace TrackmaniaExchangeAPI.Models
{
    public class Medals
    {
        [JsonProperty("Author")]
        public int Author { get; set; }

        [JsonProperty("Bronze")]
        public int Bronze { get; set; }

        [JsonProperty("Silver")]
        public int Silver { get; set; }

        [JsonProperty("Gold")]
        public int Gold { get; set; }
    }
}