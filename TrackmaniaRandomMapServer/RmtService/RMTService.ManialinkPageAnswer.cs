using GbxRemoteNet.Events;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

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
                    await OnForceSkip();
                    break;
                case "VoteGoldSkip":
                    await OnVoteGoldSkip(e.Login);
                    break;
                case "ForceGoldSkip":
                    await OnForceGoldSkip();
                    break;
                case "VoteQuit":
                    await OnVoteQuit(e.Login);
                    break;
                case "ForceQuit":
                    await OnForceQuit();
                    break;
                default:
                    break;
            }
        }

        private async Task OnForceQuit()
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnForceQuit enter semaphor");
                if (!RmtRunning)
                {
                    logger.LogTrace("OnForceQuit exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                if (CanForceQuit())
                {
                    logger.LogTrace("OnForceQuit exit semaphor");
                    RmtRunning = false;
                    remainingTime = 60 * 60;
                    semaphoreSlim.Release();
                    await SetRemainingTime(60 * 60);
                    await UpdateView();
                    await tmClient.RestartMapAsync();
                    
                }
                else
                {
                    logger.LogTrace("OnForceQuit exit semaphor");
                    semaphoreSlim.Release();
                    await tmClient.ChatSendServerMessageAsync($"Not enough votes to skip: {playerStateService.QuitVotes()}/{MinimumVotes()}");
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
                if (!RmtRunning || mapFinished)
                {
                    logger.LogTrace("OnVoteQuit exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(login);
                playerState.VoteQuit = !playerState.VoteQuit;
                logger.LogTrace("OnVoteQuit exit semaphor");
                semaphoreSlim.Release();

                if (playerState.VoteQuit)
                {
                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} voted to Quit RMT. {playerStateService.QuitVotes()}/{MinimumVotes()}");
                }
                else
                {
                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Quit RMT. {playerStateService.QuitVotes()}/{MinimumVotes()}");
                }
                await UpdateView();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnVoteQuit");
                throw;
            }
        }

        private async Task OnForceGoldSkip()
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnForceGoldSkip enter semaphor");
                if (!RmtRunning || mapFinished)
                {
                    logger.LogTrace("OnForceGoldSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(goldCredit);
                if (CanForceGoldSkip())
                {
                    playerState.GoodSkips += 1;
                    mapFinished = true;
                    goldCredit = null;
                    goodSkipScore += 1;

                    var diffTime = DateTime.UtcNow - mapStartTime.Value;
                    remainingTime -= (int)diffTime.TotalSeconds;
                    playerStateService.CancelAllVotes();
                    mapStartTime = null;

                    logger.LogTrace("OnForceGoldSkip exit semaphor");
                    semaphoreSlim.Release();

                    await UpdateView();
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
                if (!RmtRunning || mapFinished)
                {
                    logger.LogTrace("OnVoteGoldSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(login);
                playerState.VoteGoldSkip = !playerState.VoteGoldSkip;
                logger.LogTrace("OnVoteGoldSkip exit semaphor");
                semaphoreSlim.Release();

                if (playerState.VoteGoldSkip)
                {
                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} voted to Gold Skip. {playerStateService.GoldSkipVotes()}/{MinimumVotes()}");
                }
                else
                {
                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Gold Skip. {playerStateService.GoldSkipVotes()}/{MinimumVotes()}");
                }
                await UpdateView();
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
                if (RmtRunning)
                {
                    logger.LogTrace("OnStartRMT exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }

                RmtRunning = true;
                scoreboardVisible = false;
                playerStateService.CancelAllVotes();
                playerStateService.ClearBestTimes();
                playerStateService.ClearPlayerScores();
                goldCredit = null;
                mapStartTime = null;
                remainingTime = 60 * 60;
                mapFinished = true;
                winScore = 0;
                goodSkipScore = 0;
                badSkipScore = 0;

                logger.LogTrace("OnStartRMT exit semaphor");
                semaphoreSlim.Release();

                await SetRemainingTime(remainingTime);
                await UpdateView();
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
                if (!RmtRunning || mapFinished)
                {
                    logger.LogTrace("OnVoteSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                var playerState = playerStateService.GetPlayerState(login);
                playerState.VoteSkip = !playerState.VoteSkip;
                logger.LogTrace("OnVoteSkip exit semaphor");
                semaphoreSlim.Release();
                if (playerState.VoteSkip)
                {
                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} voted to Skip. {playerStateService.SkipVotes()}/{MinimumVotes()}");
                }
                else
                {
                    await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Skip. {playerStateService.SkipVotes()}/{MinimumVotes()}");
                }
                await UpdateView();
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in OnVoteSkip");
                throw;
            }
        }

        private async Task OnForceSkip()
        {
            try
            {
                await semaphoreSlim.WaitAsync();
                logger.LogTrace("OnForceSkip enter semaphor");
                if (!RmtRunning || mapFinished)
                {
                    logger.LogTrace("OnForceSkip exit semaphor");
                    semaphoreSlim.Release();
                    return;
                }
                if (CanForceSkip())
                {
                    mapFinished = true;
                    badSkipScore += 1;

                    var diffTime = DateTime.UtcNow - mapStartTime.Value;
                    remainingTime -= (int)diffTime.TotalSeconds;
                    mapStartTime = null;

                    playerStateService.CancelAllVotes();
                    logger.LogTrace("OnForceSkip exit semaphor");
                    semaphoreSlim.Release();

                    await UpdateView();
                    await AdvanceMap();
                }
                else
                {
                    logger.LogTrace("OnForceSkip exit semaphor");
                    semaphoreSlim.Release();
                    await tmClient.ChatSendServerMessageAsync($"Not enough votes to skip: {playerStateService.SkipVotes()}/{MinimumVotes()}");
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
