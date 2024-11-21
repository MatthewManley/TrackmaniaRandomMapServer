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
            var middlePart = parts[1];

            // In the JWT, it doesn't pad the base64 string with =
            // But C# really wants them to be there
            // so we need to add them ourselves if they are not there
            if (middlePart.Length % 4 != 0)
            {
                middlePart += new string('=', 4 - middlePart.Length % 4);
            }

            var converted = Convert.FromBase64String(middlePart);
            var middleDecoded = Encoding.UTF8.GetString(converted);
            return JsonSerializer.Deserialize<DecodedToken>(middleDecoded);
        }
    }
}
