using Newtonsoft.Json;
using System.Web;
using TrackmaniaExchangeAPI.Models;

namespace TrackmaniaExchangeAPI
{
    public class TmxRestClient
    {
        private HttpClient httpClient;
        private readonly TmxRestClientOptions options;

        public TmxRestClient(HttpClient httpClient, TmxRestClientOptions? options)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options;
        }

        public async Task<SearchMapResult?> SearchMaps(SearchMapsParameters searchMaps, CancellationToken cancellationToken = default)
        {
            if (searchMaps is null)
                throw new ArgumentNullException(nameof(searchMaps));

            var uriBuilder = new UriBuilder();
            uriBuilder.Host = options.HostName;
            uriBuilder.Scheme = options.Scheme;
            uriBuilder.Path = "/api/maps";

            var parameters = new Dictionary<string, string>();

            var fields = new List<string>
            {
                "MapId",
                "MapUid",
                "Medals.Author",
                "Medals.Bronze",
                "Medals.Gold",
                "Medals.Silver",
                "UpdatedAt",
                "UploadedAt",
                "Tags",
            };

            parameters.Add("fields", HttpUtility.UrlEncode(string.Join(',', fields)));

            if (searchMaps.Random.HasValue)
                parameters.Add("random", searchMaps.Random.Value.ToString());

            if (searchMaps.ExcludedTags is not null)
            {
                var stringTags = searchMaps.ExcludedTags.Select(x => x.ToString());
                parameters.Add("etags", string.Join(',', stringTags));
            }

            if (searchMaps.AuthorTimeMax.HasValue)
                parameters.Add("authortimemax", searchMaps.AuthorTimeMax.Value.ToString());

            if (searchMaps.Count.HasValue)
                parameters.Add("count", searchMaps.Count.Value.ToString());

            uriBuilder.Query = BuildQueryString(parameters);

            var resultResponse = await httpClient.GetAsync(uriBuilder.Uri, cancellationToken);
            cancellationToken.ThrowIfCancellationRequested();
            resultResponse.EnsureSuccessStatusCode();
            var content = await resultResponse.Content.ReadAsStringAsync(cancellationToken);
            return JsonConvert.DeserializeObject<SearchMapResult>(content);
        }

        public async Task<TmxMap?> GetRandomMapChallengeMap(CancellationToken cancellationToken = default)
        {
            var searchParams = new SearchMapsParameters
            {
                Random = 1,
                AuthorTimeMax = 180000,
                ExcludedTags = [23, 37, 40],
                Count = 1,
            };
            var result = await SearchMaps(searchParams, cancellationToken);
            return result?.Results.FirstOrDefault();
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            return "?" + string.Join('&', parameters.Select(x => $"{x.Key}={x.Value}"));
        }

        public async Task<Stream> DownloadMap(TmxMap tmxMap, CancellationToken cancellationToken = default)
            => await DownloadMap(tmxMap.MapId, cancellationToken);

        public async Task<Stream> DownloadMap(long trackId, CancellationToken cancellationToken = default)
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Host = options.HostName;
            uriBuilder.Scheme = options.Scheme;
            uriBuilder.Path = $"/maps/download/{trackId}";
            var resultResponse = await httpClient.GetAsync(uriBuilder.Uri, cancellationToken);
            return await resultResponse.Content.ReadAsStreamAsync(cancellationToken);
        }
    }
}
