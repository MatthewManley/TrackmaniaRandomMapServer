// See https://aka.ms/new-console-template for more information

// This isn't really apart of the project but just testing stuff as I develop it
using NadeoAPI;
using TrackmaniaExchangeAPI;

var httpClient = new HttpClient();

var nadeoRestClient = new NadeoRestClient(httpClient, new NadeoRestClientOptions
{
    Username = "",
    Password = ""
});

var tmxRestClient = new TmxRestClient(httpClient);
for (int i = 0; i < 500; i++)
{
    var random = await tmxRestClient.GetRandomMapChallengeMap();
    var result = await nadeoRestClient.GetMapInfo(random.TrackUID);
    if (result is null || !result.Valid || !result.Playable || !result.Public)
    {
        Console.WriteLine();
    }
    Console.WriteLine(i);
    await Task.Delay(500);
}
Console.WriteLine();