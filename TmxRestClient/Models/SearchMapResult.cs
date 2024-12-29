using Newtonsoft.Json;

namespace TrackmaniaExchangeAPI.Models
{
    public class SearchMapResult
    {
        [JsonProperty("Results")]
        public List<TmxMap> Results { get; set; }
    }
}
