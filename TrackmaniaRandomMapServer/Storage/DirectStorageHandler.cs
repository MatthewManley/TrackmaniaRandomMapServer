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
        private readonly DirectStorageOptions directStorageOptions;

        public DirectStorageHandler(IOptions<DirectStorageOptions> options)
        {
            this.directStorageOptions = options.Value;
        }

        public bool CanExists => true;

        public bool CanDelete => true;

        public Task Delete(string fileName, CancellationToken cancellationToken)
        {
            var path = Path.Join(directStorageOptions.MapsPath, fileName);
            File.Delete(path);
            return Task.CompletedTask;
        }

        public Task<bool> Exists(string fileName, CancellationToken cancellationToken)
        {
            var path = Path.Join(directStorageOptions.MapsPath, fileName);
            var exists = File.Exists(path);
            return Task.FromResult(exists);
        }

        public async Task Write(string fileName, Stream contents, CancellationToken cancellationToken)
        {
            var path = Path.Join(directStorageOptions.MapsPath, fileName);
            using var fileStream = File.Create(path);
            await contents.CopyToAsync(fileStream, cancellationToken);
        }
    }
}
