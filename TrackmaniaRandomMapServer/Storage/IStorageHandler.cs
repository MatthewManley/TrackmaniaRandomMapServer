using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer.Storage
{
    public interface IStorageHandler
    {
        public Task<string> ReadConfig(CancellationToken cancellationToken);
        public Task WriteMap(string fileName, Stream contents, CancellationToken cancellationToken = default);
        public Task<bool> MapExists(string fileName, CancellationToken cancellationToken = default);
        public Task DeleteMap(string fileName, CancellationToken cancellationToken = default);
    }
}
