using Newtonsoft.Json;
using System.Text.Json;

namespace TrackmaniaExchangeAPI.Models
{
    public class TmxMap
    {
        [JsonProperty("MapId")]
        public long MapId { get; set; }

        [JsonProperty("MapUid")]
        public string MapUid { get; set; }

        [JsonProperty("Medals")]
        public Medals Medals { get; set; }

        [JsonProperty("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("UploadedAt")]
        public DateTime UploadedAt { get; set; }

        [JsonProperty("Tags")]
        public List<MapTag> Tags { get; set; }

        public bool IsPrepatchIce
        {
            get
            {
                var hasIceTag = Tags.Select(x => x.TagId).Any(x => x == 14 || x == 44);
                return UpdatedAt.Date <= new DateTime(2022, 10, 1) && hasIceTag;
            }
        }

        public bool IsOverThreeMinutes
        {
            get
            {
                return Medals.Author >= 3 * 60 * 1000;
            }
        }
    }
}
