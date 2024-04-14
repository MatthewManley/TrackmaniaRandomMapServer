using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Models;

namespace TrackmaniaRandomMapServer
{
    public class TmxRestClient
    {
        private readonly ILogger<TmxRestClient> logger;
        private HttpClient httpClient;
        private readonly RMTOptions options;

        public TmxRestClient(ILogger<TmxRestClient> logger, HttpClient httpClient, IOptions<RMTOptions> options)
        {
            this.logger = logger;
            this.httpClient = httpClient;
            this.options = options.Value;
        }

        public async Task<TmxMap> GetRandomMap()
        {
            string content = null;
            try
            {

                var uriBuilder = new UriBuilder();
                uriBuilder.Host = "trackmania.exchange";
                uriBuilder.Scheme = "https";
                uriBuilder.Path = "/mapsearch2/search";
                uriBuilder.Query = "?api=on&random=1&lengthop=1&length=9&etags=23,46,40,41,42,37";
                var resultResponse = await httpClient.GetAsync(uriBuilder.Uri);
                content = await resultResponse.Content.ReadAsStringAsync();
                var result = JsonConvert.DeserializeObject<TmxQueryResult>(content);
                return result.results[0];

            }
            catch (Exception ex)
            {
                logger.LogError($"Error getting random map: {ex.Message}");
                throw;
            }
        }

        public async Task<(string, byte[])> DownloadMap(TmxMap tmxMap)
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Host = "trackmania.exchange";
            uriBuilder.Scheme = "https";
            uriBuilder.Path = $"/maps/download/{tmxMap.TrackID}";
            var resultResponse = await httpClient.GetAsync(uriBuilder.Uri);
            var filename = $"RMT/{tmxMap.TrackID}.Map.Gbx";
            //var filepath = Path.Combine("/data/UserData/Maps", filename);
            var bytes = await resultResponse.Content.ReadAsByteArrayAsync();
            //using (var fs = new FileStream(filepath, FileMode.CreateNew))
            //{
            //    await resultResponse.Content.CopyToAsync(fs);
            //}
            return (filename, bytes);
        }
    }
}
