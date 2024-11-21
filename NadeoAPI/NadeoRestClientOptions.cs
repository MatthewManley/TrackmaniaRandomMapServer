namespace NadeoAPI
{
    public class NadeoRestClientOptions
    {
        public string? Username { get; set; }
        public string? Password { get; set; }
        public string CoreScheme { get; set; } = "https";
        public string CoreHost { get; set; } = "prod.trackmania.core.nadeo.online";
        public string LiveScheme { get; set; } = "https";
        public string LiveHost { get; set; } = "live-services.trackmania.nadeo.live";

    }
}
