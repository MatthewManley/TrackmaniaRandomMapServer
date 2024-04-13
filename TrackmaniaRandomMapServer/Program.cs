using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer;
using TrackmaniaRandomMapServer.Models;
using TrackmaniaRandomMapServer.RmtService;

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
                    httpClient.DefaultRequestHeaders.Add("User-Agent", "Trackmania Random Map Server");
                });
            });
            var section = hostContext.Configuration.GetSection("RMT");
            services.Configure<RMTOptions>(hostContext.Configuration.GetSection("RMT"));
            services.AddHostedService<RMTService>();
        });
}