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
        const string MapsPath = "/server/UserData/Maps";

        public Task DeleteMap(string fileName, CancellationToken cancellationToken)
        {
            var path = Path.Join(MapsPath, fileName);
            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<bool> MapExists(string fileName, CancellationToken cancellationToken)
        {
            var path = Path.Join(MapsPath, fileName);
            var directory = Path.GetDirectoryName(path);
            if (!Directory.Exists(directory))
            {
                return Task.FromResult(false);
            }
            var exists = File.Exists(path);
            return Task.FromResult(exists);
        }

        public async Task<string> ReadConfig(CancellationToken cancellationToken)
        {
            return await File.ReadAllTextAsync("/server/UserData/Config/dedicated_cfg.txt", cancellationToken);
        }

        public async Task WriteMap(string fileName, Stream contents, CancellationToken cancellationToken)
        {
            var path = Path.Join(MapsPath, fileName);
            var tmpFileName = Path.GetFileName(fileName);
            var dirName = path.Substring(0, path.Length - tmpFileName.Length);
            if (!Directory.Exists(dirName))
            {
                Directory.CreateDirectory(dirName);
            }
            using var fileStream = File.Create(path);
            await contents.CopyToAsync(fileStream, cancellationToken);
        }
    }
}
