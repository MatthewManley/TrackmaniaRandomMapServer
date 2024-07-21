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
        public Task Write(string fileName, Stream contents, CancellationToken cancellationToken);
        public Task<bool> Exists(string fileName, CancellationToken cancellationToken);
        public Task Delete(string fileName, CancellationToken cancellationToken);
    }
}
