using Microsoft.Extensions.Logging;
using System.Text;
using System.Text.Json;

namespace NadeoAPI
{
    public class DecodedToken
    {
        public string jti { get; set; }
        public string iss { get; set; }
        public long iat { get; set; }
        public long rat { get; set; }
        public long exp { get; set; }
        public string aud { get; set; }
        public string usg { get; set; }
        public string sid { get; set; }
        public long sat { get; set; }
        public string sub { get; set; }
        public string aun { get; set; }
        public bool rtk { get; set; }
        public bool pce { get; set; }
        public string ubiservices_uid { get; set; }

        public static DecodedToken? FromString(string? token)
        { 
            if (token is null)
                return null;
            var parts = token.Split('.');
            if (parts.Length != 3)
                throw new Exception();
            var base64JwtBody = parts[1];

            var jwtBody = Utils.Base64Decode(base64JwtBody);

            return JsonSerializer.Deserialize<DecodedToken>(jwtBody);
        }
    }
}
