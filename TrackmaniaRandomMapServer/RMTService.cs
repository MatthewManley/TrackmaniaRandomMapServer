using GbxRemoteNet.Enums;
using GbxRemoteNet.Events;
using GbxRemoteNet.XmlRpc.ExtraTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperXML;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Events;

namespace TrackmaniaRandomMapServer
{
    public class RMTService : BackgroundService
    {
        private readonly RMTOptions rmtOptions;
        private readonly TrackmaniaRemoteClient tmClient;
        private readonly TmxRestClient tmxRestClient;

        // Game State
        private bool RmtRunning = false;
        private bool mapFinished = false;
        private string goldCredit = null;
        private int? mapStartTime = null;
        private int remainingTime = 60 * 60;
        private int winScore = 0;
        private int skipScore = 0;

        private Dictionary<string, PlayerState> ConnectedPlayers = new();
        private string nextMap = null;
        private ManiaplanetMap currentMap = null;
        private SemaphoreSlim semaphoreSlim = new(1, 1);

        public RMTService(IOptions<RMTOptions> rmtOptions, TmxRestClient tmxRestClient)
        {
            this.rmtOptions = rmtOptions.Value;
            tmClient = new TrackmaniaRemoteClient(this.rmtOptions.IpAddress, this.rmtOptions.Port);
            this.tmxRestClient = tmxRestClient;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            //var dataDir = Directory.GetDirectories("/data/");
            //var userDataDir = Directory.GetDirectories("/data/UserData");
            //var mapsDir = Directory.GetDirectories("/data/UserData/Maps");
            //var rmtDir = Directory.GetDirectories("/data/UserData/Maps/RMT");

            tmClient.OnWaypoint += Client_OnWaypoint;
            tmClient.OnModeScriptCallback += Client_OnModeScriptCallback;
            tmClient.OnPlayerManialinkPageAnswer += Client_OnPlayerManialinkPageAnswer;
            tmClient.OnPlayerConnect += Client_OnPlayerConnect;
            tmClient.OnPlayerInfoChanged += Client_OnPlayerInfoChanged;
            tmClient.OnPlayerDisconnect += Client_OnPlayerDisconnect;
            tmClient.OnStartline += Client_OnStartline;
            tmClient.OnStartMapEnd += Client_OnStartMapEnd;
            tmClient.OnEndMapEnd += Client_OnEndMapEnd;
            tmClient.OnEndMapStart += Client_OnEndMapStart;

            if (!await tmClient.LoginAsync(rmtOptions.Username, rmtOptions.Password))
            {
                Console.WriteLine("Failed to login");
                return;
            }
            else
            {
                Console.WriteLine("Logged in");
            }

            await DownloadRandomMap();

            //var settings = await trackmaniaRemoteClient.GetModeScriptSettingsAsync();
            //var var = await trackmaniaRemoteClient.GetModeScriptVariablesAsync();
            //var info = await trackmaniaRemoteClient.GetModeScriptInfoAsync();
            //var method = await trackmaniaRemoteClient.SystemListMethodsAsync();

            //var firstMap = await tmxClient.GetRandomMap();
            //nextMap = await tmxClient.DownloadMap(firstMap);

            await tmClient.EnableCallbackTypeAsync(GbxCallbackType.Checkpoints | GbxCallbackType.Internal | GbxCallbackType.ModeScript);
            var players = await tmClient.GetPlayerListAsync();
            foreach (var item in players)
            {
                ConnectedPlayers.Add(item.Login, new PlayerState { NickName = item.NickName });
            }

            //var test = await trackmaniaRemoteClient.TriggerModeScriptEventArrayAsync("Common.UIModules.GetProperties");

            await UpdateView();
            await SetTmScoreboardVisibility(true);

            await Task.Delay(-1);
        }

        private async Task Client_OnWaypoint(object sender, TrackmaniaWaypoint e)
        {
            if (!e.IsEndRace)
                return;

            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapFinished)
            {
                semaphoreSlim.Release();
                return;
            }

            var winTime = currentMap.GoldTime;
            var skipTime = currentMap.SilverTime;

            if (e.RaceTime <= winTime)
            {
                mapFinished = true;
                goldCredit = null;

                var diffTime = e.Time - mapStartTime.Value;
                remainingTime -= diffTime / 1000;
                mapStartTime = null;

                semaphoreSlim.Release();

                await UpdateView();
                await AdvanceMap();
            }
            else if (e.RaceTime <= skipTime)
            {
                goldCredit = e.Login;
                semaphoreSlim.Release();
            }
            else
            {
                semaphoreSlim.Release();
            }
        }

        private Task Client_OnPlayerDisconnect(object sender, PlayerDisconnectGbxEventArgs e)
        {
            try
            {
                ConnectedPlayers.Remove(e.Login);
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task UpdateView(string playerLogin = null)
        {
            var xml = new Compiler()
                .AddKey("DisplayWelcome", !RmtRunning)
                //.AddKey("DisplayWelcome", false)
                .AddKey("DisplayScore", RmtRunning && !mapFinished)
                //.AddKey("DisplayScore", true)
                .AddKey("wins", winScore)
                .AddKey("skips", skipScore)
                .CompileXml("template.xml");
            if (playerLogin is null)
            {
                await tmClient.SendDisplayManialinkPageAsync(xml, 0, false);
            }
            else
            {
                await tmClient.SendDisplayManialinkPageToLoginAsync(playerLogin, xml, 0, false);
            }
        }

        private async Task Client_OnEndMapStart(object sender, ManiaplanetEndMap e) => await SetTmScoreboardVisibility(false);

        private async Task Client_OnEndMapEnd(object sender, ManiaplanetEndMap e) => await SetTmScoreboardVisibility(false);

        private async Task SetTmScoreboardVisibility(bool visible)
        {
            var uimodules = new SetUiModules();
            uimodules.UiModules = new List<UiModule>()
        {
            new UiModule()
            {
                Id = "Race_ScoresTable",
                Visible = visible,
                VisibleUpdate = true
            },
            new UiModule()
            {
                Id = "Race_BigMessage",
                Visible = visible,
                VisibleUpdate = true
            }
        };
            var param = JsonConvert.SerializeObject(uimodules);
            var test = await tmClient.TriggerModeScriptEventArrayAsync("Common.UIModules.SetProperties", param);
            Console.WriteLine($"test: {test}");
        }

        private async Task Client_OnStartMapEnd(object sender, ManiaplanetStartMap e)
        {
            await SetTmScoreboardVisibility(true);
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning)
            {
                semaphoreSlim.Release();
                return;
            }

            mapFinished = false;
            goldCredit = null;
            currentMap = e.Map;
            semaphoreSlim.Release();

            await UpdateView();
            await SetRemainingTime(remainingTime);
        }

        private async Task Client_OnStartline(object sender, TrackmaniaStartline e)
        {
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapStartTime != null)
            {
                semaphoreSlim.Release();
                return;
            }
            mapStartTime = e.Time;
            semaphoreSlim.Release();
        }

        private Task Client_OnPlayerInfoChanged(object sender, PlayerInfoChangedGbxEventArgs e)
        {
            try
            {
                if (ConnectedPlayers.TryGetValue(e.PlayerInfo.Login, out var playerState))
                {
                    playerState.NickName = e.PlayerInfo.NickName;
                }
                else
                {
                    ConnectedPlayers[e.PlayerInfo.Login] = new PlayerState()
                    {
                        NickName = e.PlayerInfo.NickName,
                    };
                }
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task Client_OnPlayerConnect(object sender, PlayerConnectGbxEventArgs e)
        {
            try
            {
                ConnectedPlayers[e.Login] = null;
                await UpdateView(e.Login);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task Client_OnPlayerManialinkPageAnswer(object sender, ManiaLinkPageActionGbxEventArgs e)
        {
            if (!ConnectedPlayers.TryGetValue(e.Login, out var playerState))
            {
                var info = await tmClient.GetPlayerInfoAsync(e.Login);
                playerState = new PlayerState()
                {
                    NickName = info.NickName
                };
                ConnectedPlayers[e.Login] = playerState;
            }
            try
            {
                Console.WriteLine($"Manialink page answer: {e.Login} {e.Answer}");
                switch (e.Answer)
                {
                    case "StartRMT":
                        {
                            await semaphoreSlim.WaitAsync();
                            if (RmtRunning)
                            {
                                semaphoreSlim.Release();
                                return;
                            }

                            RmtRunning = true;
                            semaphoreSlim.Release();

                            await UpdateView();
                            await AdvanceMap();
                            await SetRemainingTime(60 * 60);
                            break;
                        }
                    case "VoteGoldSkip":
                        {
                            await semaphoreSlim.WaitAsync();
                            if (goldCredit is null)
                            {
                                playerState.VoteGoldSkip = !playerState.VoteGoldSkip;
                                if (playerState.VoteGoldSkip)
                                {
                                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Gold Skip");
                                }
                                else
                                {
                                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} voted to Gold Skip");
                                }
                            }
                            semaphoreSlim.Release();
                            break;
                        }
                    default:
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }

        private async Task AdvanceMap()
        {
            if (nextMap is null)
            {
                await DownloadRandomMap();
            }

            var allMaps = await tmClient.GetMapListAsync(100, 0);
            await tmClient.InsertMapAsync(nextMap);
            await tmClient.RemoveMapListAsync(allMaps.Select(x => x.FileName).ToArray());
            await tmClient.NextMapAsync();

            await DownloadRandomMap();
        }

        private async Task DownloadRandomMap()
        {
            var tmp = await tmxRestClient.GetRandomMap();
            var (nMap, mapData) = await tmxRestClient.DownloadMap(tmp);
            var dataObj = new GbxBase64(mapData);
            await tmClient.WriteFileAsync(nMap, dataObj);
            nextMap = nMap;
        }

        private async Task SetRemainingTime(int time)
        {
            var settings = await tmClient.GetModeScriptSettingsAsync();
            settings["S_TimeLimit"] = time;
            await tmClient.SetModeScriptSettingsAsync(settings);
        }

        private async Task Client_OnModeScriptCallback(string method, Newtonsoft.Json.Linq.JObject data)
        {
            try
            {
                Console.WriteLine($"Client_OnModeScriptCallback: {method}");
                //var filename = $"{DateTime.Now:yyyyMMddHHmmssfff}_{method}.json";
                //using (var writer = new StreamWriter(filename, false))
                //{
                //    await writer.WriteAsync(data.ToString());
                //}
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
                throw;
            }
        }
    }
}
