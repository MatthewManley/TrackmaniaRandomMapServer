using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer;

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
            services.AddOptions();
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