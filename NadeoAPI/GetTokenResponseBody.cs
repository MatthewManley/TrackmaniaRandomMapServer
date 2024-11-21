using System.Text.Json.Serialization;

namespace NadeoAPI
{
    public class GetTokenResponseBody
    {
        public DecodedToken? DecodedAccessToken { get; private set; } = null;

        private string? _accessToken = null;

        [JsonPropertyName("accessToken")]
        public string? AccessToken
        {
            get => _accessToken;
            set {
                _accessToken = value;
                DecodedAccessToken = DecodedToken.FromString(value);
            }
        }

        [JsonPropertyName("refreshToken")]
        public string? RefreshToken { get; set; }
    }
}
