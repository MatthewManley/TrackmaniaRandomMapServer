using GbxRemoteNet;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using NadeoAPI;
using NetCord.Rest;
using System;
using System.Threading.Tasks;
using TrackmaniaExchangeAPI;
using TrackmaniaRandomMapServer;
using TrackmaniaRandomMapServer.Options;
using TrackmaniaRandomMapServer.RmtService;
using TrackmaniaRandomMapServer.Storage;

internal class Program
{
    public static async Task Main(string[] args)
    {
        Console.WriteLine("Startup!");
        await CreateHostBuilder(args).Build().RunAsync();
    }

    public static IHostBuilder CreateHostBuilder(string[] args) =>
        Host.CreateDefaultBuilder(args)
        .ConfigureAppConfiguration((hostingContext, config) =>
        {
            if (hostingContext.HostingEnvironment.IsDevelopment())
            {
                config.AddUserSecrets<Program>();
            }
        })
        .ConfigureServices((hostContext, services) =>
        {

            services.AddLogging(builder => builder.AddConsole());
            services.AddOptions();
            services.AddSingleton<PlayerStateService>();
            services.AddTransient<TmxRestClient>();
            services.ConfigureHttpClientDefaults(client =>
            {
                client.ConfigureHttpClient(httpClient =>
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "TM RMT - https%3A%2F%2Fgithub.com%2FMatthewManley%2FTrackmaniaRandomMapServer");
                });
            });
            services.AddHttpClient<TmxRestClient>();
            services.Configure<RMTOptions>(hostContext.Configuration.GetSection("RMT"));
            services.Configure<TmxRestClientOptions>(hostContext.Configuration.GetSection("TMX"));
            services.AddTransient((services) => services.GetRequiredService<IOptions<TmxRestClientOptions>>().Value);
            services.AddSingleton((serviceProvider) =>
            {
                var rmtOptions = serviceProvider.GetRequiredService<IOptions<RMTOptions>>().Value;
                var gbxRemoteOptions = new GbxRemoteClientOptions
                {
                    ConnectionRetries = 10, // TODO: make configurable, along with retry timeout
                };
                var logger = serviceProvider.GetRequiredService<ILogger<TrackmaniaRemoteClient>>();
                return new TrackmaniaRemoteClient(rmtOptions.IpAddress, rmtOptions.Port, gbxRemoteOptions, logger);
            });
            services.AddSingleton<RandomMapService>();
            services.AddSingleton<TmxRestClient>();
            services.AddSingleton<NadeoRestClient>();
            services.AddSingleton((services) =>
            {
                var rmtOptions = services.GetRequiredService<IOptions<RMTOptions>>();
                return new NadeoRestClientOptions()
                {
                    Username = rmtOptions.Value.NadeoUsername,
                    Password = rmtOptions.Value.NadeoPassword,
                };
            }) ;
            var storageType = hostContext.Configuration.GetValue<string>("StorageType");
            switch (storageType?.ToUpperInvariant())
            {
                // TODO: I intend to support SFTP so the controller and server can be on different devices
                // but for easy of development I had set this aside for another time
                //case "SFTP":
                //    services.Configure<SftpOptions>(hostContext.Configuration.GetSection("Sftp"));
                //    services.AddHostedService<SftpHostService>();
                //    services.AddTransient<IStorageHandler, SftpStorageHandler>();
                //    break;
                case "DIRECT":
                default:
                    services.AddTransient<IStorageHandler, DirectStorageHandler>();
                    break;
            }
            var webhook = hostContext.Configuration.GetSection("RMT").GetValue<string>("DiscordWebhook", null);
            if (!string.IsNullOrWhiteSpace(webhook))
            {
                services.AddSingleton((serviceProvider) =>
                {
                    var rmtOptions = serviceProvider.GetRequiredService<IOptions<RMTOptions>>();
                    var webhookParts = rmtOptions.Value.DiscordWebhook.Split('/');
                    var webhookId = Convert.ToUInt64(webhookParts[webhookParts.Length - 2]);
                    var webhookToken = webhookParts[webhookParts.Length - 1];
                    return new WebhookClient(webhookId, webhookToken);
                });
            }
            services.AddHostedService<RMTService>();
        });
}