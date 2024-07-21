//using Renci.SshNet;
//using System.IO;
//using System.Threading;
//using System.Threading.Tasks;

//namespace TrackmaniaRandomMapServer.Storage
//{
//    public class SftpStorageHandler : IStorageHandler
//    {
//        private readonly SftpHostService sftpHostService;

//        public SftpStorageHandler(SftpHostService sftpHostService)
//        {
//            this.sftpHostService = sftpHostService;
//        }

//        public async Task Write(string fileName, Stream contents, CancellationToken cancellationToken)
//        {
//            var sftpClient = sftpHostService.GetSftpClient();
//            using var writeStream = sftpClient.OpenWrite(fileName);
//            if (cancellationToken.IsCancellationRequested)
//                return;
//            await contents.CopyToAsync(writeStream, cancellationToken);
//            if (cancellationToken.IsCancellationRequested)
//                return;
//            await writeStream.FlushAsync(cancellationToken);
//        }

//        public async Task Delete(string fileName, CancellationToken cancellationToken)
//        {
//            var sftpClient = sftpHostService.GetSftpClient();
//            await sftpClient.DeleteFileAsync(fileName, cancellationToken);
//        }

//        public Task<bool> Exists(string fileName, CancellationToken cancellationToken)
//        {
//            var sftpClient = sftpHostService.GetSftpClient();
//            var exists = sftpClient.Exists(fileName);
//            return Task.FromResult(exists);
//        }

//        public Task<byte[]> Read(string fileName, CancellationToken cancellationToken)
//        {
//            var sftpClient = sftpHostService.GetSftpClient();
//            var bytes = sftpClient.ReadAllBytes(fileName);
//            return Task.FromResult(bytes);
//        }
//    }
//}
