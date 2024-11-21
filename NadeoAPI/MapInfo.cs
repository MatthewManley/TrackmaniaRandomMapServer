using System.Text.Json.Serialization;

namespace NadeoAPI
{
    public class MapInfo
    {
        [JsonPropertyName("uid")]
        public string UID { get; set; }

        [JsonPropertyName("mapId")]
        public string ID { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("")]
        public string Author { get; set; }

        [JsonPropertyName("submitter")]
        public string Submitter { get; set; }

        [JsonPropertyName("authorTime")]
        public int AuthorTime { get; set; }

        [JsonPropertyName("goldTime")]
        public int GoldTime { get; set; }

        [JsonPropertyName("silverTime")]
        public int SilverTime { get; set; }

        [JsonPropertyName("bronzeTime")]
        public int BronzeTime { get; set; }

        [JsonPropertyName("nbLaps")]
        public int NumberOfLaps { get; set; }

        [JsonPropertyName("valid")]
        public bool Valid { get; set; }

        [JsonPropertyName("downloadUrl")]
        public string DownloadUrl { get; set; }

        [JsonPropertyName("thumbnailUrl")]
        public string ThumbnailUrl { get; set; }

        [JsonPropertyName("uploadTimestamp")]
        public int UploadTimestamp { get; set; }

        [JsonPropertyName("updateTimestamp")]
        public int UpdateTimestamp { get; set; }

        [JsonPropertyName("public")]
        public bool Public { get; set; }

        [JsonPropertyName("favorite")]
        public bool Favorite { get; set; }

        [JsonPropertyName("playable")]
        public bool Playable { get; set; }

        [JsonPropertyName("mapStyle")]
        public string MapStyle { get; set; }

        [JsonPropertyName("mapType")]
        public string MapType { get; set; }

        [JsonPropertyName("collectionName")]
        public string CollectionName { get; set; }
    }
}
