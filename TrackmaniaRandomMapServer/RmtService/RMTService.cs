using GbxRemoteNet.Enums;
using GbxRemoteNet.Events;
using GbxRemoteNet.XmlRpc.ExtraTypes;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using SuperXML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Events;
using TrackmaniaRandomMapServer.Models;

namespace TrackmaniaRandomMapServer.RmtService
{
    public partial class RMTService : BackgroundService
    {
        private readonly RMTOptions rmtOptions;
        private readonly TrackmaniaRemoteClient tmClient;
        private readonly ILogger<RMTService> logger;
        private readonly TmxRestClient tmxRestClient;
        private readonly PlayerStateService playerStateService;

        // Game State
        private bool RmtRunning = false;
        private bool scoreboardVisible = false;
        private bool mapFinished = false;
        private string goldCredit = null;
        private DateTime? mapStartTime = null;
        private int remainingTime = 60 * 60;

        private int winScore = 0;
        private int goodSkipScore = 0;
        private int badSkipScore = 0;

        private string nextMap = null;
        private TmxMap nextMapDetails = null;
        private TmxMap currentMapDetails = null;
        private ManiaplanetMap currentMap = null;
        private SemaphoreSlim semaphoreSlim = new(1, 1);
        private SemaphoreSlim downloadSemaphor = new(1, 1);

        public RMTService(ILogger<RMTService> logger, IOptions<RMTOptions> rmtOptions, TmxRestClient tmxRestClient, PlayerStateService playerStateService)
        {
            this.rmtOptions = rmtOptions.Value;
            tmClient = new TrackmaniaRemoteClient(this.rmtOptions.IpAddress, this.rmtOptions.Port);
            this.logger = logger;
            this.tmxRestClient = tmxRestClient;
            this.playerStateService = playerStateService;
        }

        public int MinimumVotes() => (int)Math.Ceiling(playerStateService.CurrentPlayerCount() / 2d);
        public bool CanVoteSkip() => goldCredit is null;
        public bool CanVoteGoldSkip() => goldCredit is not null;
        public bool CanVoteQuit() => true;
        private bool CanForceSkip() => CanVoteSkip() && playerStateService.SkipVotes() >= MinimumVotes();
        public bool CanForceGoldSkip() => CanVoteGoldSkip() && playerStateService.GoldSkipVotes() >= MinimumVotes();
        private bool CanForceQuit() => CanVoteQuit() && playerStateService.QuitVotes() >= MinimumVotes();

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
            tmClient.OnStartMapStart += TmClient_OnStartMapStart;
            tmClient.OnEndMapEnd += Client_OnEndMapEnd;
            tmClient.OnEndMapStart += Client_OnEndMapStart;
            tmClient.OnPlayerChat += TmClient_OnPlayerChat;

            if (!await tmClient.LoginAsync(rmtOptions.Username, rmtOptions.Password))
            {
                logger.LogInformation("Failed to login");
                return;
            }
            else
            {
                logger.LogInformation("Logged in");
            }

            var success = false;
            while (!success)
            {
                await downloadSemaphor.WaitAsync();
                success = await DownloadRandomMap();
                downloadSemaphor.Release();
            }

            await tmClient.EnableCallbackTypeAsync(GbxCallbackType.Checkpoints | GbxCallbackType.Internal | GbxCallbackType.ModeScript);

            var players = await tmClient.GetPlayerListAsync();
            foreach (var item in players)
            {
                var newPlayerState = new PlayerState
                {
                    NickName = item.NickName,
                    IsSpectator = item.SpectatorStatus != 0
                };
                playerStateService.UpsertPlayerState(item.Login, newPlayerState);
            }

            await UpdateView();
            await SetTmScoreboardVisibility(true);

            await Task.Delay(-1);
        }

        /// <summary>
        /// Executed anytime a player goes through a checkpoint or a finish line
        /// We only care about the finish line and throw away other events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        /// <returns></returns>
        private async Task Client_OnWaypoint(object sender, TrackmaniaWaypoint e)
        {
            if (!e.IsEndRace)
                return;
            await semaphoreSlim.WaitAsync();
            logger.LogTrace("Client_OnWaypoint enter semaphor");
            if (!RmtRunning || mapFinished)
            {
                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();
                return;
            }

            //TODO: Fetch from some sort of setting
            var winTime = currentMap.AuthorTime;
            var skipTime = currentMap.GoldTime;

            var playerState = playerStateService.GetPlayerState(e.Login);

            if (playerState.BestMapTime is null || e.RaceTime < playerState.BestMapTime)
            {
                playerState.BestMapTime = e.RaceTime;
            }

            // Player got the required medal to win the map
            if (e.RaceTime <= winTime)
            {
                mapFinished = true;
                goldCredit = null;
                winScore += 1;
                playerState.NumWins += 1;

                var diffTime = DateTime.UtcNow - mapStartTime.Value;
                remainingTime -= (int)diffTime.TotalSeconds;
                mapStartTime = null;

                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName ?? e.Login} got AT Medal!");
                await UpdateView();
                await AdvanceMap();
            }

            // Player got the required medal to good skip the map
            else if (e.RaceTime <= skipTime && goldCredit is null)
            {
                goldCredit = e.Login;
                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();
                await UpdateView();
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName ?? e.Login} got the first Gold Medal, gold skip is now available");
            }
            else
            {
                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();
            }
        }

        private Task Client_OnPlayerDisconnect(object sender, PlayerDisconnectGbxEventArgs e)
        {
            try
            {
                var ps = playerStateService.GetPlayerState(e.Login);
                ps.IsSpectator = true;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Client_OnPlayerDisconnect");
                throw;
            }
        }

        private async Task UpdateView(string playerLogin = null)
        {
            string xml = null;
            if (!RmtRunning && !scoreboardVisible)
            {
                xml = new Compiler()
                    .CompileXml("Templates/startrmt.xml");
            }
            else if (RmtRunning && !scoreboardVisible)
            {
                xml = new Compiler()
                .AddKey("wins", winScore)
                .AddKey("skips", goodSkipScore)
                .AddKey("CanGoldSkip", CanVoteGoldSkip())
                .AddKey("CanForceGoldSkip", CanForceGoldSkip())
                .AddKey("CanSkip", CanVoteSkip())
                .AddKey("CanForceSkip", CanForceSkip())
                .AddKey("CanQuit", CanVoteQuit())
                .AddKey("CanForceQuit", CanForceQuit())
                .CompileXml("Templates/rmtwidget.xml");
            }
            else if (scoreboardVisible)
            {
                var leaderboard = playerStateService.GetLeaderboard().ToArray();
                xml = new Compiler()
                .AddKey("wins", winScore)
                .AddKey("skips", goodSkipScore)
                .AddKey("badSkips", badSkipScore)
                .AddKey("Scoreboard", leaderboard)
                .AddKey("time_left", $"{remainingTime / 60:D2}:{remainingTime % 60:D2}")
                .CompileXml("Templates/scoreboard.xml");
            }
            if (xml is null)
                return;

            if (playerLogin is null)
            {
                await tmClient.SendDisplayManialinkPageAsync(xml, 0, false);
            }
            else
            {
                await tmClient.SendDisplayManialinkPageToLoginAsync(playerLogin, xml, 0, false);
            }
        }

        private async Task Client_OnEndMapStart(object sender, ManiaplanetEndMap e)
        {
            await semaphoreSlim.WaitAsync();
            bool wasRMTRunning = RmtRunning;
            if (!mapFinished)
            {
                RmtRunning = false;
                remainingTime = 0;
                //TODO: goto hub
            }
            semaphoreSlim.Release();
            if (wasRMTRunning)
            {
                scoreboardVisible = true;
                await SetTmScoreboardVisibility(false);
            }
            await UpdateView();
        }

        private async Task Client_OnEndMapEnd(object sender, ManiaplanetEndMap e)
        {
            scoreboardVisible = false;
            await SetTmScoreboardVisibility(true);
            await UpdateView();
        }

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
        }

        private async Task TmClient_OnStartMapStart(object sender, ManiaplanetStartMap e)
        {
            if (currentMapDetails is not null)
            {
                await tmClient.ChatSendServerMessageAsync($"Map Details: {currentMapDetails.Name} by {currentMapDetails.Username}");
                var tags = currentMapDetails.Tags.Split(',');
                var hasIceTag = tags.Contains("14") || tags.Contains("44");
                if (currentMapDetails.UpdatedAt.Date <= new DateTime(2022, 10, 1) && hasIceTag)
                {
                    await tmClient.ChatSendServerMessageAsync("Possible Prepatch Ice Map");
                }
            }
        }

        private async Task Client_OnStartMapEnd(object sender, ManiaplanetStartMap e)
        {
            await semaphoreSlim.WaitAsync();
            logger.LogTrace("Client_OnStartMapEnd enter semaphor");
            if (!RmtRunning)
            {
                logger.LogTrace("Client_OnStartMapEnd exit semaphor");
                semaphoreSlim.Release();
                return;
            }

            mapFinished = false;
            goldCredit = null;
            currentMap = e.Map;
            playerStateService.ClearBestTimes();
            logger.LogTrace("Client_OnStartMapEnd exit semaphor");
            semaphoreSlim.Release();

            await UpdateView();
            await SetRemainingTime(remainingTime);
        }

        private async Task Client_OnStartline(object sender, TrackmaniaStartline e)
        {
            await semaphoreSlim.WaitAsync();
            logger.LogTrace("Client_OnStartline enter semaphor");
            if (!RmtRunning || mapStartTime != null)
            {
                logger.LogTrace("Client_OnStartline exit semaphor");
                semaphoreSlim.Release();
                return;
            }
            mapStartTime = DateTime.UtcNow;
            logger.LogTrace("Client_OnStartline exit semaphor");
            semaphoreSlim.Release();
        }

        private Task Client_OnPlayerInfoChanged(object sender, PlayerInfoChangedGbxEventArgs e)
        {
            try
            {
                var playerState = playerStateService.GetPlayerState(e.PlayerInfo.Login);
                playerState.NickName = e.PlayerInfo.NickName;
                playerState.IsSpectator = e.PlayerInfo.SpectatorStatus != 0;
                return Task.CompletedTask;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Client_OnPlayerInfoChanged");
                throw;
            }
        }

        private async Task Client_OnPlayerConnect(object sender, PlayerConnectGbxEventArgs e)
        {
            try
            {
                var ps = playerStateService.GetPlayerState(e.Login);
                ps.IsSpectator = e.IsSpectator;
                await UpdateView(e.Login);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Client_OnPlayerConnect");
                throw;
            }
        }


        private async Task AdvanceMap()
        {
            try
            {
                logger.LogTrace("ONE");
                var allMaps = await tmClient.GetMapListAsync(100, 0);
                logger.LogInformation("TWO");
                await tmClient.InsertMapAsync(nextMap);
                logger.LogInformation("THREE");
                await tmClient.RemoveMapListAsync(allMaps.Select(x => x.FileName).ToArray());
                logger.LogInformation("FOUR");
                await tmClient.NextMapAsync();
                logger.LogInformation("FIVE");

                await downloadSemaphor.WaitAsync();
                var success = false;
                while (!success)
                {
                    success = await DownloadRandomMap();
                }
                downloadSemaphor.Release();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in AdvanceMap");
                throw;
            }
        }

        private async Task<bool> DownloadRandomMap()
        {
            try
            {
                currentMapDetails = nextMapDetails;
                var tmp = await tmxRestClient.GetRandomMap();
                var (nMap, mapData) = await tmxRestClient.DownloadMap(tmp);
                var dataObj = new GbxBase64(mapData);
                await tmClient.WriteFileAsync(nMap, dataObj);
                nextMap = nMap;
                nextMapDetails = tmp;
                return true;
            }
            catch (Exception)
            {
                logger.LogWarning("FAILED TO DOWNLOAD MAP");
                return false;
            }
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
                logger.LogTrace($"Client_OnModeScriptCallback: {method}");
                //var filename = $"{DateTime.Now:yyyyMMddHHmmssfff}_{method}.json";
                //using (var writer = new StreamWriter(filename, false))
                //{
                //    await writer.WriteAsync(data.ToString());
                //}
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Client_OnModeScriptCallback");
                throw;
            }
        }
    }
}
