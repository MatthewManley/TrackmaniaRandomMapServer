using Microsoft.Extensions.Options;
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
        private HttpClient httpClient;
        private readonly RMTOptions options;
        
        public TmxRestClient(HttpClient httpClient, IOptions<RMTOptions> options)
        {
            this.httpClient = httpClient;
            this.options = options.Value;
        }

        public async Task<TmxMap> GetRandomMap()
        {
            var uriBuilder = new UriBuilder();
            uriBuilder.Host = "trackmania.exchange";
            uriBuilder.Scheme = "https";
            uriBuilder.Path = "/mapsearch2/search";
            uriBuilder.Query = "?api=on&random=1&lengthop=1&length=9&etags=23,46,40,41,42,37";
            var resultResponse = await httpClient.GetAsync(uriBuilder.Uri);
            var result = await resultResponse.Content.ReadFromJsonAsync<TmxQueryResult>();
            return result.results[0];
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
