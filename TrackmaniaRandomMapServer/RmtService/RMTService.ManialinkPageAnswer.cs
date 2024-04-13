using GbxRemoteNet.Events;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer.RmtService
{
    public partial class RMTService : BackgroundService
    {
        private async Task Client_OnPlayerManialinkPageAnswer(object sender, ManiaLinkPageActionGbxEventArgs e)
        {
            var playerState = await GetPlayerState(e.Login);
            try
            {
                switch (e.Answer)
                {
                    case "StartRMT":
                        await OnStartRMT();
                        break;
                    case "VoteSkip":
                        await OnVoteSkip(playerState);
                        break;
                    case "ForceSkip":
                        await OnForceSkip(playerState);
                        break;
                    case "VoteGoldSkip":
                        await OnVoteGoldSkip(playerState);
                        break;
                    case "ForceGoldSkip":
                        await OnForceGoldSkip(playerState);
                        break;
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

        private async Task OnForceGoldSkip(PlayerState playerState)
        {
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapFinished)
            {
                semaphoreSlim.Release();
                return;
            }
            if (CanForceGoldSkip)
            {
                mapFinished = true;
                goldCredit = null;
                skipScore += 1;

                var diffTime = DateTime.UtcNow - mapStartTime.Value;
                remainingTime -= (int)diffTime.TotalSeconds;
                mapStartTime = null;

                semaphoreSlim.Release();
                await CancelAllVotes();
                await UpdateView();
                await AdvanceMap();
            }
            else
            {
                semaphoreSlim.Release();
                await tmClient.ChatSendServerMessageAsync($"Not enough votes to skip: {SkipVotes}/{VotesNeeded + 1}");
            }
        }

        private async Task OnVoteGoldSkip(PlayerState playerState)
        {
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapFinished)
            {
                semaphoreSlim.Release();
                return;
            }
            playerState.VoteGoldSkip = !playerState.VoteGoldSkip;
            semaphoreSlim.Release();
            if (playerState.VoteGoldSkip)
            {
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} voted to Gold Skip. {GoldSkipVotes}/{VotesNeeded + 1}");
            }
            else
            {
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Gold Skip. {GoldSkipVotes}/{VotesNeeded + 1}");
            }
            await UpdateView();
        }

        private async Task OnStartRMT()
        {
            await semaphoreSlim.WaitAsync();
            if (RmtRunning)
            {
                semaphoreSlim.Release();
                return;
            }

            RmtRunning = true;
            semaphoreSlim.Release();
            await CancelAllVotes();
            await UpdateView();
            await AdvanceMap();
            await SetRemainingTime(60 * 60);
        }

        private async Task OnVoteSkip(PlayerState playerState)
        {
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapFinished)
            {
                semaphoreSlim.Release();
                return;
            }
            playerState.VoteSkip = !playerState.VoteSkip;
            semaphoreSlim.Release();
            if (playerState.VoteSkip)
            {
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} voted to Skip. {SkipVotes}/{VotesNeeded + 1}");
            }
            else
            {
                await tmClient.ChatSendServerMessageAsync($"{playerState.NickName} canceled their vote to Skip. {SkipVotes}/{VotesNeeded + 1}");
            }
            await UpdateView();
        }

        private async Task OnForceSkip(PlayerState playerState)
        {
            await semaphoreSlim.WaitAsync();
            if (!RmtRunning || mapFinished)
            {
                semaphoreSlim.Release();
                return;
            }
            if (CanForceSkip)
            {
                mapFinished = true;
                semaphoreSlim.Release();
                await CancelAllVotes();
                await UpdateView();
                await AdvanceMap();
            }
            else
            {
                semaphoreSlim.Release();
                await tmClient.ChatSendServerMessageAsync($"Not enough votes to skip: {SkipVotes}/{VotesNeeded + 1}");
            }
        }
    }
}
