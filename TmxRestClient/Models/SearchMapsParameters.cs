namespace TrackmaniaExchangeAPI.Models
{
    /// <summary>
    /// https://api2.mania.exchange/Method/Index/53
    /// </summary>
    public class SearchMapsParameters
    {
        public int? Random { get; set; }

        public int[]? ExcludedTags { get; set; }

        public int? AuthorTimeMax { get; set; }

        public int? Count { get; set; }
    }
}
