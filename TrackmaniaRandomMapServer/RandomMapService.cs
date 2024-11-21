using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Models;
using TrackmaniaExchangeAPI;
using TrackmaniaExchangeAPI.Models;
using TrackmaniaRandomMapServer.Storage;
using System.Threading;
using NadeoAPI;
using System.IO;

namespace TrackmaniaRandomMapServer
{
    public class RandomMapService
    {
        public RandomMapService(TmxRestClient tmxRestClient, NadeoRestClient nadeoRestClient, IStorageHandler storageHandler)
        {
            this.tmxRestClient = tmxRestClient;
            this.storageHandler = storageHandler;
            this.nadeoRestClient = nadeoRestClient;
        }

        //public Queue<TmxMap> DownloadedMaps = new();

        //public TmxMap CurrentMap = null;

        //public Queue<TmxMap> MapsToDelete = new();
        private readonly TmxRestClient tmxRestClient;
        private readonly NadeoRestClient nadeoRestClient;
        private readonly IStorageHandler storageHandler;

        public async Task<CombinedMapResult> DownloadRandomMap(CancellationToken cancellationToken = default)
        {
            while (true)
            {
                var map = await tmxRestClient.GetRandomMapChallengeMap();
                if (map is null)
                    continue;
                var nadeoResult = await nadeoRestClient.GetMapInfo(map.TrackUID, cancellationToken);
                if (nadeoResult is null)
                    continue;
                var filename = Path.Join("RMT", $"{map.TrackID}.Map.Gbx");

                // TODO: verify map is actually downloaded properly rather than just delete and redownload
                if (await storageHandler.MapExists(filename, cancellationToken))
                {
                    await storageHandler.DeleteMap(filename, cancellationToken);
                }

                var mapStream = await tmxRestClient.DownloadMap(map);
                await storageHandler.WriteMap(filename, mapStream, cancellationToken);
                return new CombinedMapResult
                {
                    FileName = filename,
                    TmxMapInfo = map,
                    NadeoMapInfo = nadeoResult
                };
            }
        }
    }

    public class CombinedMapResult
    {
        public string FileName { get; set; }
        public TmxMap TmxMapInfo { get; set; }
        public MapInfo NadeoMapInfo { get; set; }
    }
}
