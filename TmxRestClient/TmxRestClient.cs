using Newtonsoft.Json;
using TrackmaniaExchangeAPI.Models;

namespace TrackmaniaExchangeAPI
{
    public class TmxRestClient
    {
        private HttpClient httpClient;
        private readonly TmxRestClientOptions options;

        public TmxRestClient(HttpClient httpClient, TmxRestClientOptions? options = null)
        {
            this.httpClient = httpClient ?? throw new ArgumentNullException(nameof(httpClient));
            this.options = options ?? new();
        }

        public async Task<SearchMapResult?> SearchMaps(SearchMapsParameters searchMaps, CancellationToken cancellationToken = default)
        {
            if (searchMaps is null)
                throw new ArgumentNullException(nameof(searchMaps));

            var uriBuilder = new UriBuilder();
            uriBuilder.Host = options.HostName;
            uriBuilder.Scheme = options.Scheme;
            uriBuilder.Path = "/mapsearch2/search";

            var parameters = new Dictionary<string, string>
            {
                { "api", "on" }
            };

            if (searchMaps.Random.HasValue)
                parameters.Add("random", searchMaps.Random.Value.ToString());

            if (searchMaps.ExcludedTags is not null)
            {
                var stringTags = searchMaps.ExcludedTags.Select(x => x.ToString());
                parameters.Add("etags", string.Join(',', stringTags));
            }

            if (searchMaps.Length.HasValue)
                parameters.Add("length", searchMaps.Length.Value.ToString());

            if (searchMaps.LengthOp.HasValue)
                parameters.Add("lengthop", ((int)searchMaps.LengthOp.Value).ToString());

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
                LengthOp = LengthOp.LessThan,
                Length = 9,
                ExcludedTags = [23, 37, 40]
            };
            var result = await SearchMaps(searchParams, cancellationToken);
            return result?.results.FirstOrDefault();
        }

        private static string BuildQueryString(IEnumerable<KeyValuePair<string, string>> parameters)
        {
            return "?" + string.Join('&', parameters.Select(x => $"{x.Key}={x.Value}"));
        }

        public async Task<Stream> DownloadMap(TmxMap tmxMap, CancellationToken cancellationToken = default)
            => await DownloadMap(tmxMap.TrackID, cancellationToken);

        public async Task<Stream> DownloadMap(int trackId, CancellationToken cancellationToken = default)
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
