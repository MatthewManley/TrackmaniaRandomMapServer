using GbxRemoteNet.XmlRpc.ExtraTypes;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer.Storage
{
    public class XmlRpcStorageHandler : IStorageHandler
    {
        private readonly TrackmaniaRemoteClient trackmaniaRemoteClient;

        public bool CanExists => false;

        public bool CanDelete => false;

        public XmlRpcStorageHandler(TrackmaniaRemoteClient trackmaniaRemoteClient)
        {
            this.trackmaniaRemoteClient = trackmaniaRemoteClient;
        }

        public async Task Write(string fileName, Stream contents, CancellationToken cancellationToken)
        {
            using var memoryStream = new MemoryStream();
            await contents.CopyToAsync(memoryStream, cancellationToken);
            if (cancellationToken.IsCancellationRequested)
                return;
            byte[] bytes = memoryStream.ToArray();
            var data = new GbxBase64(bytes);
            await trackmaniaRemoteClient.WriteFileAsync(fileName, data);
        }

        public Task Delete(string fileName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }

        public Task<bool> Exists(string fileName, CancellationToken cancellationToken)
        {
            throw new NotImplementedException();
        }
    }
}
