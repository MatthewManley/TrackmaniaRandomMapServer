using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer.Storage
{
    public interface IStorageHandler
    {
        public Task Write(string fileName, Stream contents, CancellationToken cancellationToken);
        public Task<bool> Exists(string fileName, CancellationToken cancellationToken);
        public Task Delete(string fileName, CancellationToken cancellationToken);

        public bool CanExists { get; }
        public bool CanDelete { get; }
    }
}
