using GbxRemoteNet.Enums;
using GbxRemoteNet.Events;
using GbxRemoteNet.XmlRpc.ExtraTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperXML;
using System;
using System.Collections.Generic;
using System.Diagnostics.Eventing.Reader;
using System.IO;
using System.Linq;
using System.Net.NetworkInformation;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Events;

namespace TrackmaniaRandomMapServer.RmtService
{
    public partial class RMTService : BackgroundService
    {
        private readonly RMTOptions rmtOptions;
        private readonly TrackmaniaRemoteClient tmClient;
        private readonly TmxRestClient tmxRestClient;

        // Game State
        private bool RmtRunning = false;
        private bool mapFinished = false;
        private string goldCredit = null;
        private DateTime? mapStartTime = null;
        private int remainingTime = 60 * 60;
        private int winScore = 0;
        private int skipScore = 0;

        private Dictionary<string, PlayerState> ConnectedPlayers = new();
        private string nextMap = null;
        private ManiaplanetMap currentMap = null;
        private SemaphoreSlim semaphoreSlim = new(1, 1);

        private int VotesNeeded => (int)Math.Floor(ConnectedPlayers.Where(x => !x.Value.IsSpectator).Count() / 2d);
        private int GoldSkipVotes => ConnectedPlayers.Values.Count(x => !x.IsSpectator && x.VoteGoldSkip);
        private int SkipVotes => ConnectedPlayers.Values.Count(x => !x.IsSpectator && x.VoteSkip);
        private int QuitVotes => ConnectedPlayers.Values.Count(x => !x.IsSpectator && x.VoteQuit);

        private bool CanGoldSkip => goldCredit is not null;
        private bool CanForceGoldSkip => CanGoldSkip && GoldSkipVotes > VotesNeeded;

        private bool CanSkip => !CanGoldSkip;
        private bool CanForceSkip => CanSkip && SkipVotes > VotesNeeded;

        private bool CanQuit => true;
        private bool CanForceQuit => CanQuit && QuitVotes > VotesNeeded;


        public RMTService(IOptions<RMTOptions> rmtOptions, TmxRestClient tmxRestClient)
        {
            this.rmtOptions = rmtOptions.Value;
            tmClient = new TrackmaniaRemoteClient(this.rmtOptions.IpAddress, this.rmtOptions.Port);
            this.tmxRestClient = tmxRestClient;
        }

        public async Task<PlayerState> GetPlayerState(string login)
        {
            if (!ConnectedPlayers.TryGetValue(login, out var playerState))
            {
                var info = await tmClient.GetPlayerInfoAsync(login);
                playerState = new PlayerState()
                {
                    NickName = info.NickName,
                    IsSpectator = info.SpectatorStatus == 0
                };
                ConnectedPlayers[login] = playerState;
            }
            return playerState;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
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
            tmClient.OnPlayerChat += TmClient_OnPlayerChat;

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
                ConnectedPlayers.Add(item.Login, new PlayerState { NickName = item.NickName, IsSpectator = item.SpectatorStatus != 0 });
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
            var skipTime = currentMap.BronzeTime;

            if (e.RaceTime <= winTime)
            {
                mapFinished = true;
                goldCredit = null;
                winScore += 1;

                var diffTime = DateTime.UtcNow - mapStartTime.Value;
                remainingTime -= (int)diffTime.TotalSeconds;
                mapStartTime = null;

                semaphoreSlim.Release();
                await UpdateView();
                await AdvanceMap();
            }
            else if (e.RaceTime <= skipTime && goldCredit is null)
            {
                goldCredit = e.Login;
                semaphoreSlim.Release();
                await UpdateView();
                var playerState = await GetPlayerState(e.Login);
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName ?? e.Login} got the first Gold Medal, gold skip is now available");
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
                ConnectedPlayers[e.Login].IsSpectator = true;
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
                .AddKey("CanGoldSkip", CanGoldSkip)
                .AddKey("CanForceGoldSkip", CanForceGoldSkip)
                .AddKey("CanSkip", CanSkip)
                .AddKey("CanForceSkip", CanForceSkip)
                .AddKey("CanQuit", CanQuit)
                .AddKey("CanForceQuit", CanForceQuit)
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

        private async Task CancelAllVotes()
        {
            await semaphoreSlim.WaitAsync();
            foreach (var item in ConnectedPlayers.Values)
            {
                item.VoteGoldSkip = false;
                item.VoteSkip = false;
                item.VoteQuit = false;
            }
            semaphoreSlim.Release();
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
            await SetTmScoreboardVisibility(true);
        }

        private async Task Client_OnStartline(object sender, TrackmaniaStartline e)
        {
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapStartTime != null)
            {
                semaphoreSlim.Release();
                return;
            }
            mapStartTime = DateTime.UtcNow;
            semaphoreSlim.Release();
        }

        private Task Client_OnPlayerInfoChanged(object sender, PlayerInfoChangedGbxEventArgs e)
        {
            try
            {
                if (!ConnectedPlayers.TryGetValue(e.PlayerInfo.Login, out var playerState))
                {
                    playerState = new PlayerState();
                    ConnectedPlayers[e.PlayerInfo.Login] = playerState;
                }
                playerState.NickName = e.PlayerInfo.NickName;
                playerState.IsSpectator = e.PlayerInfo.SpectatorStatus == 0;
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
                ConnectedPlayers[e.Login] = new PlayerState()
                {
                    IsSpectator = e.IsSpectator,
                };
                await UpdateView(e.Login);
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
