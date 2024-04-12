using Newtonsoft.Json;

namespace TrackmaniaRandomMapServer.Events
{
    public class ManiaplanetMap
    {
        [JsonProperty("uid")]
        public string Uid { get; set; }

        [JsonProperty("name")]
        public string Name { get; set; }

        [JsonProperty("filename")]
        public string Filename { get; set; }

        [JsonProperty("author")]
        public string Author { get; set; }

        [JsonProperty("authornickname")]
        public string AuthorNickname { get; set; }

        [JsonProperty("environment")]
        public string Environment { get; set; }

        [JsonProperty("mood")]
        public string Mood { get; set; }

        [JsonProperty("bronzetime")]
        public int BronzeTime { get; set; }

        [JsonProperty("silvertime")]
        public int SilverTime { get; set; }

        [JsonProperty("goldtime")]
        public int GoldTime { get; set; }

        [JsonProperty("authortime")]
        public int AuthorTime { get; set; }

        [JsonProperty("laprace")]
        public bool LapRace { get; set; }

        [JsonProperty("lapstyle")]
        public string LapStyle { get; set; }
    }
}
