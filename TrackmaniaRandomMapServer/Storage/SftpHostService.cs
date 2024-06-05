using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Renci.SshNet;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Options;

namespace TrackmaniaRandomMapServer.Storage
{
    public class SftpHostService : IHostedService
    {
        private readonly SftpOptions sftpOptions;
        private SftpClient sftpClient;

        public SftpHostService(IOptions<SftpOptions> sftpOptions)
        {
            this.sftpOptions = sftpOptions.Value;
        }

        public SftpClient GetSftpClient()
        {
            if (sftpClient == null || !sftpClient.IsConnected)
                throw new InvalidOperationException("SFTP client not connected");
            return sftpClient;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            var authenticationMethods = new List<AuthenticationMethod>();
            if (!string.IsNullOrWhiteSpace(sftpOptions.Password))
            {
                authenticationMethods.Add(new PasswordAuthenticationMethod(sftpOptions.Username, sftpOptions.Password));
            }
            if (!string.IsNullOrWhiteSpace(sftpOptions.KeyFile))
            {
                if (!string.IsNullOrWhiteSpace(sftpOptions.KeyFilePassword))
                {
                    authenticationMethods.Add(new PrivateKeyAuthenticationMethod(sftpOptions.Username, new PrivateKeyFile(sftpOptions.KeyFile, sftpOptions.KeyFilePassword)));
                }
                else
                {
                    authenticationMethods.Add(new PrivateKeyAuthenticationMethod(sftpOptions.Username, new PrivateKeyFile(sftpOptions.KeyFile)));
                }
            }
            var connectionInfo = new ConnectionInfo(sftpOptions.Host, sftpOptions.Port, sftpOptions.Username, authenticationMethods.ToArray());
            sftpClient = new SftpClient(connectionInfo);
            await sftpClient.ConnectAsync(cancellationToken);
            sftpClient.ChangeDirectory(sftpOptions.MapsPath);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            sftpClient.Disconnect();
            return Task.CompletedTask;
        }
    }
}
