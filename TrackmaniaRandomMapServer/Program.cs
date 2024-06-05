using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer;
using TrackmaniaRandomMapServer.Options;
using TrackmaniaRandomMapServer.RmtService;
using TrackmaniaRandomMapServer.Storage;

internal class Program
{
    public static async Task Main(string[] args) => await CreateHostBuilder(args).Build().RunAsync();

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
            services.AddHttpClient();
            services.ConfigureHttpClientDefaults(client =>
            {
                client.ConfigureHttpClient(httpClient =>
                {
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "TM RMT - https%3A%2F%2Fgithub.com%2FMatthewManley%2FTrackmaniaRandomMapServer");
                });
            });
            services.Configure<RMTOptions>(hostContext.Configuration.GetSection("RMT"));
            services.AddSingleton((serviceProvider) =>
            {
                var rmtOptions = serviceProvider.GetRequiredService<IOptions<RMTOptions>>().Value;
                return new TrackmaniaRemoteClient(rmtOptions.IpAddress, rmtOptions.Port);
            });
            var storageType = hostContext.Configuration.GetValue<string>("StorageType");
            switch (storageType?.ToUpperInvariant())
            {
                case "SFTP":
                    services.Configure<SftpOptions>(hostContext.Configuration.GetSection("Sftp"));
                    services.AddHostedService<SftpHostService>();
                    services.AddTransient<IStorageHandler, SftpStorageHandler>();
                    break;
                case "DIRECT":
                    services.Configure<DirectStorageOptions>(hostContext.Configuration.GetSection("DirectStorage"));
                    services.AddTransient<IStorageHandler, DirectStorageHandler>();
                    break;
                case "XMLRPC":
                default:
                    services.AddTransient<IStorageHandler, XmlRpcStorageHandler>();
                    break;
            }
            services.AddSingleton((serviceProvider) =>
            {
                var rmtOptions = serviceProvider.GetRequiredService<IOptions<RMTOptions>>();
                return new Discord.Webhook.DiscordWebhookClient(rmtOptions.Value.DiscordWebhook);
            });
            services.AddHostedService<RMTService>();
        });
}