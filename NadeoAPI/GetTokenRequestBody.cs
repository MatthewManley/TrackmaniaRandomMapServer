using System.Text.Json.Serialization;

namespace NadeoAPI
{
    public class GetTokenRequestBody
    {
        [JsonPropertyName("audience")]
        public string Audience { get; set; }
    }
}
