using Newtonsoft.Json;
using System.Collections.Generic;

namespace TrackmaniaRandomMapServer.Models
{
    public class TmxQueryResult
    {
        [JsonProperty("results")]
        public List<TmxMap> results { get; set; }

        [JsonProperty("totalItemCount")]
        public int totalItemCount { get; set; }
    }
}
