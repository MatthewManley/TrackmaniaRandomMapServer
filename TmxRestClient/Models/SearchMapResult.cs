using Newtonsoft.Json;

namespace TrackmaniaExchangeAPI.Models
{
    public class SearchMapResult
    {
        [JsonProperty("results")]
        public List<TmxMap> results { get; set; }

        [JsonProperty("totalItemCount")]
        public int totalItemCount { get; set; }
    }
}
