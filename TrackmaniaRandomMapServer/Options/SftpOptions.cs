namespace TrackmaniaRandomMapServer.Options
{
    public class SftpOptions
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string MapsPath { get; set; }
        public string Username { get; set; }
        public string Password { get; set; }
        public string KeyFile { get; set; }
        public string KeyFilePassword { get; set; }
    }
}
