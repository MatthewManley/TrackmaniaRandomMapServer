using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Web;

namespace NadeoAPI
{
    public class NadeoRestClient
    {
        private readonly HttpClient httpClient;
        private readonly NadeoRestClientOptions options;

        private enum SubApi
        {
            Core,
            Live,
            Meet,
        }

        private GetTokenResponseBody? CoreToken = null;
        private GetTokenResponseBody? LiveToken = null;
        private GetTokenResponseBody? MeetToken = null;

        private ILogger<NadeoRestClient> logger { get; }

        public NadeoRestClient(HttpClient httpClient, NadeoRestClientOptions options, ILogger<NadeoRestClient> logger)
        {
            if (string.IsNullOrWhiteSpace(options.Username) || string.IsNullOrWhiteSpace(options.Password))
            {
                throw new ArgumentNullException(nameof(options));
            }
            this.httpClient = httpClient;
            this.options = options;
            this.logger = logger;
        }

        private GetTokenResponseBody? GetToken(SubApi subApi)
        {
            switch (subApi)
            {
                case SubApi.Core:
                    return CoreToken;
                case SubApi.Live:
                    return LiveToken;
                case SubApi.Meet:
                    return MeetToken;
                default:
                    throw new NotImplementedException();
            }
        }

        private async Task AuthorizeLive(CancellationToken cancellationToken = default)
        {
            if (LiveToken is null)
            {
                LiveToken = await GetToken("NadeoLiveServices", cancellationToken);

                if (LiveToken is null)
                    throw new Exception();
                return;
            }

            var expirationTime = Utils.ConvertEpochToDateTime(LiveToken.DecodedAccessToken.exp);
            if (DateTime.UtcNow + TimeSpan.FromSeconds(15) >= expirationTime)
            {
                // We should be performing a refresh here
                // instead I made the logic get a fresh token cause it was easier
                // TODO: Properly implement refreshing Nadeo API token
                LiveToken = await GetToken("NadeoLiveServices", cancellationToken);

                if (LiveToken is null)
                    throw new Exception();
                return;
            }
            if (LiveToken is null)
                throw new Exception();
            return;
        }

        private async Task<GetTokenResponseBody?> GetToken(string audience, CancellationToken cancellationToken = default)
        {
            var uriBuilder = new UriBuilder()
            {
                Scheme = options.CoreScheme,
                Host = options.CoreHost,
                Path = "/v2/authentication/token/basic",
            };
            var request = new HttpRequestMessage(HttpMethod.Post, uriBuilder.Uri);

            var encoded = Utils.Base64Encode($"{options.Username}:{options.Password}");

            request.Headers.Add("Authorization", $"Basic {encoded}");
            request.Content = JsonContent.Create(new GetTokenRequestBody { Audience = audience });

            var response = await httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();
            cancellationToken.ThrowIfCancellationRequested();

            var body = await response.Content.ReadAsStringAsync(cancellationToken);
            var result = JsonSerializer.Deserialize<GetTokenResponseBody>(body);
            return result;
        }

        /// <summary>
        /// https://webservices.openplanet.dev/live/maps/info
        /// </summary>
        public async Task<MapInfo?> GetMapInfo(string mapUid, CancellationToken cancellationToken = default)
        {
            await AuthorizeLive(cancellationToken);
            if (LiveToken is null)
                throw new Exception();

            cancellationToken.ThrowIfCancellationRequested();

            var encMapUid = HttpUtility.UrlEncode(mapUid);

            var uriBuilder = new UriBuilder()
            {
                Scheme = options.LiveScheme,
                Host = options.LiveHost,
                Path = $"/api/token/map/{encMapUid}",
            };

            var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

            request.Headers.Add("Authorization", $"nadeo_v1 t={LiveToken.AccessToken}");

            var response = await httpClient.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.NotFound)
                return null;
            cancellationToken.ThrowIfCancellationRequested();
            response.EnsureSuccessStatusCode();
            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            try
            {
                var result = JsonSerializer.Deserialize<MapInfo>(content);
                return result;
            }
            catch (FormatException)
            {
                this.logger.LogError("Bad format {token}", content);
                throw;
            }
        }

        ///// <summary>
        ///// https://webservices.openplanet.dev/live/leaderboards/top
        ///// </summary>
        //public async Task GetMapLeaderboards(string groupUid, string mapUid, int? length, bool? onlyWorld, int? offset, CancellationToken cancellationToken = default)
        //{
        //    await AuthorizeLive(cancellationToken);
        //    if (LiveToken is null)
        //        throw new Exception();

        //    cancellationToken.ThrowIfCancellationRequested();

        //    var encGroupUid = HttpUtility.UrlEncode(groupUid);
        //    var encMapUid = HttpUtility.UrlEncode(mapUid);

        //    var queryParams = new Dictionary<string, string>();
        //    if (length.HasValue)
        //        queryParams.Add("length", length.Value.ToString());
        //    if (onlyWorld.HasValue)
        //        queryParams.Add("onlyWorld", onlyWorld.Value.ToString());
        //    if (offset.HasValue)
        //        queryParams.Add("offset", offset.Value.ToString());

        //    var uriBuilder = new UriBuilder()
        //    {
        //        Scheme = options.LiveScheme,
        //        Host = options.LiveHost,
        //        Path = $"/api/token/leaderboard/group/{encGroupUid}/map/{encMapUid}/top",
        //        Query = Utils.BuildQueryString(queryParams),
        //    };

        //    var request = new HttpRequestMessage(HttpMethod.Get, uriBuilder.Uri);

        //    request.Headers.Add("Authorization", $"nadeo_v1 t={LiveToken.AccessToken}");

        //    var response = await httpClient.SendAsync(request, cancellationToken);
        //    cancellationToken.ThrowIfCancellationRequested();
        //    response.EnsureSuccessStatusCode();
        //    var content = await response.Content.ReadAsStringAsync(cancellationToken);
        //}
    }
}
