using Newtonsoft.Json;
using System.Text.Json;

namespace TrackmaniaExchangeAPI.Models
{
    public class TmxMap
    {
        [JsonProperty("TrackID")]
        public int TrackID { get; set; }

        [JsonProperty("UserID")]
        public int UserID { get; set; }

        [JsonProperty("Username")]
        public string Username { get; set; }

        [JsonProperty("GbxMapName")]
        public string GbxMapName { get; set; }

        [JsonProperty("AuthorLogin")]
        public string AuthorLogin { get; set; }

        [JsonProperty("MapType")]
        public string MapType { get; set; }

        [JsonProperty("TitlePack")]
        public string TitlePack { get; set; }

        [JsonProperty("TrackUID")]
        public string TrackUID { get; set; }

        [JsonProperty("Mood")]
        public string Mood { get; set; }

        [JsonProperty("DisplayCost")]
        public int DisplayCost { get; set; }

        [JsonProperty("ModName")]
        public object ModName { get; set; }

        [JsonProperty("Lightmap")]
        public int Lightmap { get; set; }

        [JsonProperty("ExeVersion")]
        public string ExeVersion { get; set; }

        [JsonProperty("ExeBuild")]
        public string ExeBuild { get; set; }

        [JsonProperty("AuthorTime")]
        public int AuthorTime { get; set; }

        [JsonProperty("ParserVersion")]
        public int ParserVersion { get; set; }

        [JsonProperty("UploadedAt")]
        public DateTime UploadedAt { get; set; }

        [JsonProperty("UpdatedAt")]
        public DateTime UpdatedAt { get; set; }

        [JsonProperty("Name")]
        public string Name { get; set; }

        [JsonProperty("Tags")]
        public string Tags { get; set; }

        [JsonProperty("TypeName")]
        public string TypeName { get; set; }

        [JsonProperty("StyleName")]
        public string StyleName { get; set; }

        [JsonProperty("EnvironmentName")]
        public string EnvironmentName { get; set; }

        [JsonProperty("VehicleName")]
        public string VehicleName { get; set; }

        [JsonProperty("UnlimiterRequired")]
        public bool UnlimiterRequired { get; set; }

        [JsonProperty("RouteName")]
        public string RouteName { get; set; }

        [JsonProperty("LengthName")]
        public string LengthName { get; set; }

        [JsonProperty("DifficultyName")]
        public string DifficultyName { get; set; }

        [JsonProperty("Laps")]
        public int Laps { get; set; }

        [JsonProperty("ReplayWRID")]
        public int? ReplayWRID { get; set; }

        [JsonProperty("ReplayWRTime")]
        public int? ReplayWRTime { get; set; }

        [JsonProperty("ReplayWRUserID")]
        public int? ReplayWRUserID { get; set; }

        [JsonProperty("ReplayWRUsername")]
        public string ReplayWRUsername { get; set; }

        [JsonProperty("TrackValue")]
        public int TrackValue { get; set; }

        [JsonProperty("Comments")]
        public string Comments { get; set; }

        [JsonProperty("MappackID")]
        public int MappackID { get; set; }

        [JsonProperty("Unlisted")]
        public bool Unlisted { get; set; }

        [JsonProperty("Unreleased")]
        public bool Unreleased { get; set; }

        [JsonProperty("Downloadable")]
        public bool Downloadable { get; set; }

        [JsonProperty("RatingVoteCount")]
        public int RatingVoteCount { get; set; }

        [JsonProperty("RatingVoteAverage")]
        public double RatingVoteAverage { get; set; }

        [JsonProperty("HasScreenshot")]
        public bool HasScreenshot { get; set; }

        [JsonProperty("HasThumbnail")]
        public bool HasThumbnail { get; set; }

        [JsonProperty("HasGhostBlocks")]
        public bool HasGhostBlocks { get; set; }

        [JsonProperty("EmbeddedObjectsCount")]
        public int EmbeddedObjectsCount { get; set; }

        [JsonProperty("EmbeddedItemsSize")]
        public int EmbeddedItemsSize { get; set; }

        [JsonProperty("AuthorCount")]
        public int AuthorCount { get; set; }

        [JsonProperty("IsMP4")]
        public bool IsMP4 { get; set; }

        [JsonProperty("SizeWarning")]
        public bool SizeWarning { get; set; }

        [JsonProperty("AwardCount")]
        public int AwardCount { get; set; }

        [JsonProperty("CommentCount")]
        public int CommentCount { get; set; }

        [JsonProperty("ReplayCount")]
        public int ReplayCount { get; set; }

        [JsonProperty("ImageCount")]
        public int ImageCount { get; set; }

        [JsonProperty("VideoCount")]
        public int VideoCount { get; set; }

        public bool IsPrepatchIce
        {
            get
            {
                var tags = this.Tags.Split(',');
                var hasIceTag = tags.Contains("14") || tags.Contains("44");
                return this.UpdatedAt.Date <= new DateTime(2022, 10, 1) && hasIceTag;
            }
        }

        public bool IsOverThreeMinutes
        {
            get
            {
                return this.AuthorTime >= 3 * 60 * 1000;
            }
        }
    }
}
