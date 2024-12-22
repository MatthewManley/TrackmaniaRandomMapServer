using GbxRemoteNet.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using NetCord.Gateway;
using NetCord.Rest;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TrackmaniaRandomMapServer.Models;

namespace TrackmaniaRandomMapServer.RmtService
{
    public partial class RMTService : BackgroundService
    {
        private async Task Client_OnPlayerManialinkPageAnswer(object sender, ManiaLinkPageActionGbxEventArgs e)
        {
            switch (e.Answer)
            {
                case "StartRMT":
                    await OnStartRMT();
                    break;
                case "VoteSkip":
                    await OnVoteSkip(e.Login);
                    break;
                case "ForceSkip":
                    await OnForceSkip(e.Login);
                    break;
                case "VoteGoldSkip":
                    await OnVoteGoldSkip(e.Login);
                    break;
                case "ForceGoldSkip":
                    await OnForceGoldSkip(e.Login);
                    break;
                case "VoteQuit":
                    await OnVoteQuit(e.Login);
                    break;
                case "ForceQuit":
                    await OnForceQuit(e.Login);
                    break;
                default:
                    break;
            }
        }

        private async Task OnForceQuit(string login)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnForceQuit enter semaphor");
                if (rmtPosition == RmtPosition.NotStartedHub || rmtPosition == RmtPosition.EndedScoreboard)
                {
                    logger.LogTrace("OnForceQuit exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                if (CanForceQuit())
                {
                    var player = playerStateService.GetPlayerState(login);
                    rmtPosition = RmtPosition.NotStartedHub;
                    remainingTime = 60 * 60;
                    playerStateService.CancelAllVotes();
                    logger.LogTrace("OnForceQuit exit semaphor");
                    semaphoreSlim.Release();
                    var multicall = new TmMultiCall();
                    await SetRemainingTime(multicall, 60 * 60);
                    await UpdateView(multicall);
                    multicall.RestartMapAsync();
                    multicall.ChatSendServerMessageAsync($"{player.NickName ?? login} clicked Force Quit.");
                    await tmClient.MultiCallAsync(multicall);
                }
                else
                {
                    logger.LogTrace("OnForceQuit exit semaphor");
                    semaphoreSlim.Release();
                    await tmClient.ChatSendToLoginAsync($"Not enough votes to skip: {playerStateService.QuitVotes()}/{MinimumVotes()}", login);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnForceQuit");
                throw;
            }
        }

        private async Task OnVoteQuit(string login)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnVoteQuit enter semaphor");
                if (rmtPosition == RmtPosition.NotStartedHub || rmtPosition == RmtPosition.EndedScoreboard)
                {
                    logger.LogTrace("OnVoteQuit exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(login);
                playerState.VoteQuit = !playerState.VoteQuit;
                logger.LogTrace("OnVoteQuit exit semaphor");
                semaphoreSlim.Release();

                var multicall = new TmMultiCall();
                if (playerState.VoteQuit)
                {
                    multicall.ChatSendServerMessageAsync($"{playerState.NickName} voted to Quit RMT. {playerStateService.QuitVotes()}/{MinimumVotes()}");
                }
                else
                {
                    multicall.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Quit RMT. {playerStateService.QuitVotes()}/{MinimumVotes()}");
                }
                await UpdateView(multicall);
                await tmClient.MultiCallAsync(multicall);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnVoteQuit");
                throw;
            }
        }

        private string LeaderboardToString()
        {
            StringBuilder builder = new();
            var leaderboard = playerStateService.GetLeaderboard().ToList();
            var maxNameLength = Math.Max(leaderboard.Max(x => x.DisplayName.Length), "Player".Length);
            builder.AppendLine("```");
            builder.Append("Player".PadRight(maxNameLength));
            builder.AppendLine(" | Time      | AT | GO");
            foreach (var item in leaderboard)
            {
                builder.AppendLine($"{item.DisplayName.PadRight(maxNameLength)} | {item.BestTime} | {item.NumWins,2} | {item.GoodSkips,2}");
            }
            builder.Append("```");
            return builder.ToString();
        }

        private async Task OnForceGoldSkip(string login)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnForceGoldSkip enter semaphor");
                if (rmtPosition != RmtPosition.InRound && rmtPosition != RmtPosition.Preround)
                {
                    logger.LogTrace("OnForceGoldSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(goldCredit);
                if (CanForceGoldSkip())
                {
                    var player = playerStateService.GetPlayerState(login);
                    playerState.GoodSkips += 1;
                    rmtPosition = RmtPosition.PostRound;
                    goldCredit = null;
                    goodSkipScore += 1;

                    var diffTime = DateTime.UtcNow - mapStartTime.Value;
                    remainingTime -= (int)diffTime.TotalSeconds;
                    playerStateService.CancelAllVotes();
                    mapStartTime = null;

                    logger.LogTrace("OnForceGoldSkip exit semaphor");
                    semaphoreSlim.Release();

                    var message = $"Gold Skipped Map: <https://trackmania.exchange/maps/{currentMapDetails.TmxMapInfo.TrackID}>\nCredit: {playerState.NickName}\n";
                    message += LeaderboardToString();
                    if (discordWebhookClient is not null)
                    {
                        var msgProperties = new WebhookMessageProperties
                        {
                            Content = message,
                            AllowedMentions = AllowedMentionsProperties.None,
                        };
                        _ = discordWebhookClient.ExecuteAsync(msgProperties);
                    }

                    var multicall = new TmMultiCall();
                    await UpdateView(multicall);
                    multicall.ChatSendServerMessageAsync($"{player.NickName ?? login} clicked Force {goodSkipDifficulty.DisplayName()} Skip.");
                    await tmClient.MultiCallAsync(multicall);
                    await AdvanceMap();
                }
                else
                {
                    logger.LogTrace("OnForceGoldSkip exit semaphor");
                    semaphoreSlim.Release();
                    await tmClient.ChatSendServerMessageAsync($"Not enough votes to skip: {playerStateService.SkipVotes()}/{MinimumVotes()}");
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnForceGoldSkip");
                throw;
            }
        }

        private async Task OnVoteGoldSkip(string login)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnVoteGoldSkip enter semaphor");
                if (rmtPosition != RmtPosition.Preround && rmtPosition != RmtPosition.InRound)
                {
                    logger.LogTrace("OnVoteGoldSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(login);
                playerState.VoteGoldSkip = !playerState.VoteGoldSkip;
                logger.LogTrace("OnVoteGoldSkip exit semaphor");
                semaphoreSlim.Release();

                var multicall = new TmMultiCall();
                if (playerState.VoteGoldSkip)
                {
                    multicall.ChatSendServerMessageAsync($"{playerState.NickName} voted to {goodSkipDifficulty.DisplayName()} Skip. {playerStateService.GoldSkipVotes()}/{MinimumVotes()}");
                }
                else
                {
                    multicall.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to {goodSkipDifficulty.DisplayName()} Skip. {playerStateService.GoldSkipVotes()}/{MinimumVotes()}");
                }
                await UpdateView(multicall);
                await tmClient.MultiCallAsync(multicall);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnVoteGoldSkip");
                throw;
            }
        }

        private async Task OnStartRMT()
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnStartRMT enter semaphor");
                if (rmtPosition != RmtPosition.NotStartedHub)
                {
                    logger.LogTrace("OnStartRMT exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                if (discordWebhookClient is not null)
                {
                    var msgProperties = new WebhookMessageProperties
                    {
                        Content = "An RMT round is starting!",
                        AllowedMentions = AllowedMentionsProperties.None,
                    };
                    _ = discordWebhookClient.ExecuteAsync(msgProperties);
                }

                rmtPosition = RmtPosition.StartedHub;
                playerStateService.CancelAllVotes();
                playerStateService.ClearBestTimes();
                playerStateService.ClearPlayerScores();
                goldCredit = null;
                mapStartTime = null;
                remainingTime = 60 * 60;
                winScore = 0;
                goodSkipScore = 0;
                badSkipScore = 0;

                logger.LogTrace("OnStartRMT exit semaphor");
                semaphoreSlim.Release();
                var multicall = new TmMultiCall();
                await SetRemainingTime(multicall, remainingTime);
                await UpdateView(multicall);
                await tmClient.MultiCallAsync(multicall);
                await AdvanceMap();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnStartRMT");
                throw;
            }
        }

        private async Task OnVoteSkip(string login)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnVoteSkip enter semaphor");
                if (rmtPosition != RmtPosition.Preround && rmtPosition != RmtPosition.InRound)
                {
                    logger.LogTrace("OnVoteSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(login);
                playerState.VoteSkip = !playerState.VoteSkip;
                logger.LogTrace("OnVoteSkip exit semaphor");
                semaphoreSlim.Release();
                var multicall = new TmMultiCall();
                if (playerState.VoteSkip)
                {
                    multicall.ChatSendServerMessageAsync($"{playerState.NickName} voted to Skip. {playerStateService.SkipVotes()}/{MinimumVotes()}");
                }
                else
                {
                    multicall.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Skip. {playerStateService.SkipVotes()}/{MinimumVotes()}");
                }
                await UpdateView(multicall);
                await tmClient.MultiCallAsync(multicall);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnVoteSkip");
                throw;
            }
        }

        private async Task OnForceSkip(string login)
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnForceSkip enter semaphor");
                if (rmtPosition != RmtPosition.Preround && rmtPosition != RmtPosition.InRound)
                {
                    logger.LogTrace("OnForceSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                if (CanForceSkip())
                {
                    var player = playerStateService.GetPlayerState(login);
                    rmtPosition = RmtPosition.PostRound;
                    if (!currentMapDetails.TmxMapInfo.IsPrepatchIce && !currentMapDetails.TmxMapInfo.IsOverThreeMinutes)
                    {
                        badSkipScore += 1;
                    }

                    if (mapStartTime.HasValue)
                    {
                        var diffTime = DateTime.UtcNow - mapStartTime.Value;
                        remainingTime -= (int)diffTime.TotalSeconds;
                        mapStartTime = null;
                    }

                    playerStateService.CancelAllVotes();
                    logger.LogTrace("OnForceSkip exit semaphor");
                    semaphoreSlim.Release();

                    var message = $"Skipped Map: <https://trackmania.exchange/maps/{currentMapDetails.TmxMapInfo.TrackID}>\n";
                    message += LeaderboardToString();
                    if (discordWebhookClient is not null)
                    {
                        var msgProperties = new WebhookMessageProperties
                        {
                            Content = message,
                            AllowedMentions = AllowedMentionsProperties.None,
                        };
                        _ = discordWebhookClient.ExecuteAsync(msgProperties);
                    }

                    var multicall = new TmMultiCall();
                    await UpdateView(multicall);
                    multicall.ChatSendServerMessageAsync($"{player.NickName ?? login} clicked Force Skip.");
                    await tmClient.MultiCallAsync(multicall);
                    await AdvanceMap();
                }
                else
                {
                    logger.LogTrace("OnForceSkip exit semaphor");
                    semaphoreSlim.Release();
                    await tmClient.ChatSendServerMessageToLoginAsync($"Not enough votes to skip: {playerStateService.SkipVotes()}/{MinimumVotes()}", login);
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnForceSkip");
                throw;
            }
        }
    }
}
