using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.IO;
using Microsoft.Extensions.Options;
using TrackmaniaRandomMapServer.Options;

namespace TrackmaniaRandomMapServer.Storage
{
    public class DirectStorageHandler : IStorageHandler
    {
        const string UserDataPath = "/server/UserData";
        const string RmtMapsPath = "/server/UserData/Maps/RMT";

        public Task DeleteMap(string fileName, CancellationToken cancellationToken)
        {
            var path = Path.Join(RmtMapsPath, fileName);
            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<bool> MapExists(string fileName, CancellationToken cancellationToken)
        {
            if (!Directory.Exists(RmtMapsPath))
            {
                return Task.FromResult(false);
            }
            var path = Path.Join(RmtMapsPath, fileName);
            var exists = File.Exists(path);
            return Task.FromResult(exists);
        }

        public async Task<string> ReadConfig(CancellationToken cancellationToken)
        {
            return await File.ReadAllTextAsync("/server/UserData/Config/dedicated_cfg.txt", cancellationToken);
        }

        public async Task WriteMap(string fileName, Stream contents, CancellationToken cancellationToken)
        {
            if (!Directory.Exists("/server/UserData/Maps/RMT"))
            {
                Directory.CreateDirectory(RmtMapsPath);
            }
            var path = Path.Join(RmtMapsPath, fileName);
            using var fileStream = File.Create(path);
            await contents.CopyToAsync(fileStream, cancellationToken);
        }
    }
}
