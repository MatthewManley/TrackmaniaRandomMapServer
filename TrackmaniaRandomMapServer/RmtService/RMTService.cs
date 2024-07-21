using Discord.Webhook;
using GbxRemoteNet;
using GbxRemoteNet.Enums;
using GbxRemoteNet.Events;
using ManiaTemplates;
using ManiaTemplates.Lib;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using TrackmaniaRandomMapServer.Events;
using TrackmaniaRandomMapServer.Models;
using TrackmaniaRandomMapServer.Options;
using TrackmaniaRandomMapServer.Storage;

namespace TrackmaniaRandomMapServer.RmtService
{
    public partial class RMTService : BackgroundService
    {
        private Difficulty winDifficulty = Difficulty.Author;
        private Difficulty goodSkipDifficulty = Difficulty.Gold;

        private readonly RMTOptions rmtOptions;
        private readonly TrackmaniaRemoteClient tmClient;
        private readonly IStorageHandler storageHandler;
        private readonly DiscordWebhookClient discordWebhookClient;
        private readonly ILogger<RMTService> logger;
        private readonly TmxRestClient tmxRestClient;
        private readonly PlayerStateService playerStateService;
        private ManiaTemplateEngine templateEngine;

        // Game State s
        private RmtPosition rmtPosition = RmtPosition.NotStartedHub;
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

        private Assembly ExecutingAssembly = Assembly.GetExecutingAssembly();
        private IEnumerable<Assembly> assemblies;


        public RMTService(ILogger<RMTService> logger,
                          IOptions<RMTOptions> rmtOptions,
                          TmxRestClient tmxRestClient,
                          PlayerStateService playerStateService,
                          TrackmaniaRemoteClient trackmaniaRemoteClient,
                          IStorageHandler storageHandler,
                          IServiceProvider serviceProvider)
        {
            this.rmtOptions = rmtOptions.Value;
            this.tmClient = trackmaniaRemoteClient;
            this.storageHandler = storageHandler;
            this.discordWebhookClient = serviceProvider.GetService<DiscordWebhookClient>();
            this.logger = logger;
            this.tmxRestClient = tmxRestClient;
            this.playerStateService = playerStateService;
            assemblies = [ExecutingAssembly];
        }

        public int MinimumVotes() => (int)Math.Ceiling(playerStateService.CurrentPlayerCount() / 2d);
        public bool CanVoteSkip() => goldCredit is null && (rmtPosition == RmtPosition.Preround || rmtPosition == RmtPosition.InRound);
        public bool CanVoteGoldSkip() => goldCredit is not null && (rmtPosition == RmtPosition.Preround || rmtPosition == RmtPosition.InRound);
        public bool CanVoteQuit() => rmtPosition == RmtPosition.Preround || rmtPosition == RmtPosition.InRound;
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
            tmClient.OnStartMapStart += TmClient_OnStartMapStart;
            tmClient.OnEndMapEnd += Client_OnEndMapEnd;
            tmClient.OnEndMapStart += Client_OnEndMapStart;
            tmClient.OnPlayerChat += TmClient_OnPlayerChat;

            await SetupManialinkTemplateEngine();


            var (username, password) = await GetLoginCreds(stoppingToken);

            if (!await tmClient.LoginAsync(username, password))
            {
                logger.LogInformation("Failed to login");
                // This should cause the container to restart iff configured
                throw new Exception();
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

            var multicall = new TmMultiCall();
            await UpdateView(multicall);
            SetTmScoreboardVisibility(multicall, true);

            await tmClient.MultiCallAsync(multicall);

            await Task.Delay(-1);
        }

        private async Task<(string, string)> GetLoginCreds(CancellationToken cancellationToken)
        {
            var config = await storageHandler.ReadConfig(cancellationToken);
            XElement root = XElement.Parse(config);

            // Parse authorization levels
            var authorizationLevels = root.Element("authorization_levels")
                                          .Elements("level")
                                          .Select(level => new
                                          {
                                              Name = level.Element("name").Value,
                                              Password = level.Element("password").Value
                                          });
            var superAdmin = authorizationLevels.First(x => x.Name == "SuperAdmin");
            return (superAdmin.Name, superAdmin.Password);
        }
        private async Task SetupManialinkTemplateEngine()
        {
            var resources = ExecutingAssembly.GetManifestResourceNames();
            var templates = resources.Where(x => x.StartsWith("TrackmaniaRandomMapServer.Manialinks.Templates"));
            var scripts = resources.Where(x => x.StartsWith("TrackmaniaRandomMapServer.Manialinks.Scripts"));
            templateEngine = new ManiaTemplateEngine();

            foreach (var item in templates)
            {
                var content = await Helper.GetEmbeddedResourceContentAsync(item, ExecutingAssembly);
                templateEngine.AddManiaScriptFromString(item, content);
            }

            foreach (var item in templates)
            {
                templateEngine.LoadTemplateFromEmbeddedResource(item);
                await templateEngine.PreProcessAsync(item, assemblies);
            }
        }

        private int TimeForDifficulty(ManiaplanetMap map, Difficulty difficulty)
        {
            switch (difficulty)
            {
                case Difficulty.Bronze:
                    return map.BronzeTime;
                case Difficulty.Silver:
                    return map.SilverTime;
                case Difficulty.Gold:
                    return map.GoldTime;
                case Difficulty.Finish:
                    return int.MaxValue;
                case Difficulty.Author:
                    return map.AuthorTime;
                default:
                    throw new NotImplementedException();
            }
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
            if (rmtPosition != RmtPosition.InRound)
            {
                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();
                return;
            }

            //TODO: Fetch from some sort of setting
            int winTime = TimeForDifficulty(currentMap, winDifficulty);
            int skipTime = TimeForDifficulty(currentMap, goodSkipDifficulty);

            var playerState = playerStateService.GetPlayerState(e.Login);

            if (playerState.BestMapTime is null || e.RaceTime < playerState.BestMapTime)
            {
                playerState.BestMapTime = e.RaceTime;
            }

            // Player got the required medal to win the map
            if (e.RaceTime <= winTime)
            {
                rmtPosition = RmtPosition.PostRound;
                goldCredit = null;
                winScore += 1;
                playerState.NumWins += 1;

                var diffTime = DateTime.UtcNow - mapStartTime.Value;
                remainingTime -= (int)diffTime.TotalSeconds;
                mapStartTime = null;

                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();

                var message = $"Got AT on map: <https://trackmania.exchange/maps/{currentMapDetails.TrackID}>\nCredit: {playerState.NickName ?? e.Login}\n";
                message += LeaderboardToString();
                if (discordWebhookClient is not null)
                    _ = discordWebhookClient.SendMessageAsync(message);

                var multicall = new TmMultiCall();
                multicall.ChatSendServerMessageAsync($"{playerState.NickName ?? e.Login} got {winDifficulty.DisplayName()} Medal!");
                await UpdateView(multicall);
                await tmClient.MultiCallAsync(multicall);
                await AdvanceMap();
            }

            // Player got the required medal to good skip the map
            else if (e.RaceTime <= skipTime && goldCredit is null)
            {
                goldCredit = e.Login;
                logger.LogTrace("Client_OnWaypoint exit semaphor");
                semaphoreSlim.Release();
                var multicall = new TmMultiCall();
                await UpdateView(multicall);
                var medalName = goodSkipDifficulty.DisplayName();
                multicall.ChatSendServerMessageAsync($"{playerState.NickName ?? e.Login} got the first {medalName} Medal, {medalName} skip is now available");
                await tmClient.MultiCallAsync(multicall);
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

        private async Task<TmMultiCall> UpdateView(TmMultiCall multicall, string playerLogin = null)
        {
            string xml = null;
            if (rmtPosition == RmtPosition.NotStartedHub)
            {
                xml = await templateEngine.RenderAsync("TrackmaniaRandomMapServer.Manialinks.Templates.startrmt.mt", new { }, assemblies);
            }
            else if (rmtPosition == RmtPosition.Preround || rmtPosition == RmtPosition.InRound)
            {
                xml = await templateEngine.RenderAsync("TrackmaniaRandomMapServer.Manialinks.Templates.rmtwidget.mt", new
                {
                    Wins = winScore,
                    GoodSkips = goodSkipScore,
                    BadSkips = badSkipScore,
                    CanGoldSkip = CanVoteGoldSkip(),
                    CanSkip = CanVoteSkip(),
                    CanQuit = CanVoteQuit(),
                    CanForceGoldSkip = CanForceGoldSkip(),
                    CanForceSkip = CanForceSkip(),
                    CanForceQuit = CanForceQuit(),
                    PublishDate = currentMapDetails?.UpdatedAt.ToString("MM/dd/yy"),
                    WinMedal = winDifficulty.MedalString(),
                    GoodSkipMedal = goodSkipDifficulty.MedalString(),
                    GoodSkipMedalName = goodSkipDifficulty.DisplayName(),
                }, assemblies);
            }
            else if (rmtPosition == RmtPosition.PostRound || rmtPosition == RmtPosition.EndedScoreboard)
            {
                xml = await templateEngine.RenderAsync("TrackmaniaRandomMapServer.Manialinks.Templates.scoreboard.mt", new
                {
                    Wins = winScore,
                    GoodSkips = goodSkipScore,
                    BadSkips = badSkipScore,
                    TimeLeft = $"{remainingTime / 60:D2}:{remainingTime % 60:D2}",
                    Players = playerStateService.GetLeaderboard().Take(10).ToArray(),
                    WinMedal = winDifficulty.MedalString(),
                    GoodSkipMedal = goodSkipDifficulty.MedalString(),
                    WinColor = winDifficulty.HexColor(),
                    GoodSkipColor = goodSkipDifficulty.HexColor(),
                }, assemblies);
            }
            if (xml is null)
                return multicall;

            if (playerLogin is null)
            {
                multicall.SendHideManialinkPageAsync();
                multicall.SendDisplayManialinkPageAsync(xml, 0, false);
            }
            else
            {
                multicall.SendDisplayManialinkPageToLoginAsync(playerLogin, xml, 0, false);
            }
            return multicall;
        }

        private async Task Client_OnEndMapStart(object sender, ManiaplanetEndMap e)
        {
            await semaphoreSlim.WaitAsync();
            bool finishRmt = false;
            if (rmtPosition != RmtPosition.PostRound && rmtPosition != RmtPosition.StartedHub)
            {
                rmtPosition = RmtPosition.EndedScoreboard;
                finishRmt = true;
                remainingTime = 60 * 60;
                //TODO: goto hub
            }
            semaphoreSlim.Release();
            var multicall = new TmMultiCall();

            if (finishRmt)
            {
                var message = $"RMT ended on map: <https://trackmania.exchange/maps/{currentMapDetails.TrackID}>\n";
                message += LeaderboardToString();
                if (discordWebhookClient is not null)
                    _ = discordWebhookClient.SendMessageAsync(message);
            }

            SetTmScoreboardVisibility(multicall, false);
            await UpdateView(multicall);
            if (finishRmt)
            {
                await SetRemainingTime(multicall, 60 * 60);
            }
            await tmClient.MultiCallAsync(multicall);
        }

        private async Task Client_OnEndMapEnd(object sender, ManiaplanetEndMap e)
        {
            await semaphoreSlim.WaitAsync();
            if (rmtPosition == RmtPosition.EndedScoreboard)
            {
                rmtPosition = RmtPosition.NotStartedHub;
            }
            semaphoreSlim.Release();
            var multicall = new TmMultiCall();
            SetTmScoreboardVisibility(multicall, true);
            await UpdateView(multicall);
            await tmClient.MultiCallAsync(multicall);
        }

        private MultiCall SetTmScoreboardVisibility(TmMultiCall multicall, bool visible)
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
            return multicall.TriggerModeScriptEventArrayAsync("Common.UIModules.SetProperties", param);
        }

        private async Task TmClient_OnStartMapStart(object sender, ManiaplanetStartMap e)
        {
            await semaphoreSlim.WaitAsync();
            logger.LogTrace("Client_OnStartMapEnd enter semaphor");
            if (rmtPosition != RmtPosition.PostRound && rmtPosition != RmtPosition.StartedHub)
            {
                logger.LogTrace("Client_OnStartMapEnd exit semaphor");
                semaphoreSlim.Release();
                return;
            }

            rmtPosition = RmtPosition.Preround;
            goldCredit = null;
            currentMap = e.Map;
            playerStateService.ClearBestTimes();
            playerStateService.CancelAllVotes();
            logger.LogTrace("Client_OnStartMapEnd exit semaphor");
            semaphoreSlim.Release();

            var multicall = new TmMultiCall();
            await UpdateView(multicall);
            await SetRemainingTime(multicall, remainingTime);

            if (currentMapDetails is not null)
            {
                //await tmClient.ChatSendServerMessageAsync($"Map Details: {currentMapDetails.Name} by {currentMapDetails.Username}");
                var tags = currentMapDetails.Tags.Split(',');
                var hasIceTag = tags.Contains("14") || tags.Contains("44");
                if (currentMapDetails.UpdatedAt.Date <= new DateTime(2022, 10, 1) && hasIceTag)
                {
                    multicall.ChatSendServerMessageAsync("Possible Prepatch Ice Map");
                }
            }
            await tmClient.MultiCallAsync(multicall);
        }

        private async Task Client_OnStartline(object sender, TrackmaniaStartline e)
        {
            await semaphoreSlim.WaitAsync();
            logger.LogTrace("Client_OnStartline enter semaphor");
            if (rmtPosition != RmtPosition.Preround)
            {
                logger.LogTrace("Client_OnStartline exit semaphor");
                semaphoreSlim.Release();
                return;
            }
            rmtPosition = RmtPosition.InRound;
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
                var multicall = new TmMultiCall();
                await UpdateView(multicall, e.Login);
                await tmClient.MultiCallAsync(multicall);
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
                var allMaps = await tmClient.GetMapListAsync(100, 0);

                var multicall = new TmMultiCall();
                multicall.InsertMapAsync(nextMap);
                multicall.RemoveMapListAsync(allMaps.Select(x => x.FileName).ToArray());
                multicall.NextMapAsync();
                await tmClient.MultiCallAsync(multicall);

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
                var filename = $"RMT/{tmp.TrackID}.Map.Gbx";
                if (!await storageHandler.Exists(filename, CancellationToken.None))
                {
                    var stream = await tmxRestClient.DownloadMap(tmp);
                    await storageHandler.Write(filename, stream, CancellationToken.None);
                }
                nextMap = filename;
                nextMapDetails = tmp;
                return true;
            }
            catch (Exception ex)
            {
                //logger.LogWarning("FAILED TO DOWNLOAD MAP");
                logger.LogError(ex, "FAILED TO DOWNLOAD MAP");
                return false;
            }
        }

        private async Task<TmMultiCall> SetRemainingTime(TmMultiCall multicall, int time)
        {
            var settings = await tmClient.GetModeScriptSettingsAsync();
            settings["S_TimeLimit"] = time;
            return multicall.SetModeScriptSettingsAsync(settings);
        }

        private Task Client_OnModeScriptCallback(string method, Newtonsoft.Json.Linq.JObject data)
        {
            try
            {
                logger.LogTrace($"Client_OnModeScriptCallback: {method}");
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in Client_OnModeScriptCallback");
                throw;
            }
            return Task.CompletedTask;
        }
    }
}
