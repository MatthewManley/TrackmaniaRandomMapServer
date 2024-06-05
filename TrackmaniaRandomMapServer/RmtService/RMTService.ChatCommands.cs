using GbxRemoteNet;
using GbxRemoteNet.Events;
using GbxRemoteNet.Exceptions;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer.RmtService
{
    public partial class RMTService : BackgroundService
    {
        private async Task TmClient_OnPlayerChat(object sender, PlayerChatGbxEventArgs e)
        {
            var parts = e.Text.Split(' ');
            if (parts.Length == 0)
                return;
            var cmd = parts[0].ToLower();
            switch (cmd)
            {
                case "/players":
                    await PlayersCommand(parts, e);
                    break;
                case "/ml-refresh":
                    await Refresh(parts, e);
                    break;
                case "/text":
                    await TextSize(parts, e);
                    break;
                case "/roll":
                    await Roll(parts, e);
                    break;
                default:
                    break;
            }
        }

        private async Task Roll(string[] parts, PlayerChatGbxEventArgs e)
        {
            if (!admins.Contains(e.Login))
            {
                await tmClient.ChatSendServerMessageToLoginAsync("Nice try ;)", e.Login);
                return;
            }
            var logins = parts.Skip(1).Select(playerStateService.GetLoginFromDisplayName).Where(x => x != null).Distinct().ToList();
            var loginsParm = string.Join(',', logins);
            var multicall = new TmMultiCall();
            if (logins.Count > 0)
            {
                multicall.Add("SendOpenLinkToLogin", loginsParm, "https://shattereddisk.github.io/rickroll/rickroll.mp4", 0);
                multicall.ChatSendServerMessageToLoginAsync("I heard you like Rick Astley", loginsParm);
            }
            multicall.ChatSendServerMessageToLoginAsync($"Rolled {logins.Count} players", e.Login);
            var result = await tmClient.MultiCallAsync(multicall);
        }

        private static readonly IReadOnlyCollection<string> admins = ["q5p7fE1sQca_f5BgXWvOxQ", "eR7sNZWCRyeP6oGeq_ZEMg"];

        private async Task PlayersCommand(string[] parts, PlayerChatGbxEventArgs e)
        {
            var multicall = new TmMultiCall();
            foreach (var kvPlayer in playerStateService.Players())
            {
                var login = kvPlayer.Key;
                var displayName = kvPlayer.Value.NickName;
                var msg = $"{login} : {displayName}";
                multicall.ChatSendServerMessageToLoginAsync(msg, e.Login);
            }
            await tmClient.MultiCallAsync(multicall);
        }

        private async Task Refresh(string[] parts, PlayerChatGbxEventArgs e)
        {
            var multicall = new TmMultiCall();
            multicall.SendHideManialinkPageToLoginAsync(e.Login);
            await UpdateView(multicall, e.Login);
            multicall.ChatSendServerMessageToLoginAsync("Refreshed!", e.Login);
            await tmClient.MultiCallAsync(multicall);
        }

        private async Task TextSize(string[] parts, PlayerChatGbxEventArgs e)
        {
            var values = parts.Skip(1).ToDictionary(x => x.Split('=')[0], x => x.Split('=')[1], StringComparer.OrdinalIgnoreCase);

            var quadPosXstr = values.GetValueOrDefault("quadPosX", "-150");
            var quadPosYstr = values.GetValueOrDefault("quadPosY", "0");
            var quadSizeXStr = values.GetValueOrDefault("quadSizeX", "300");
            var quadSizeYStr = values.GetValueOrDefault("quadSizeY", "10");
            var labelPosXStr = values.GetValueOrDefault("labelPosX", "-150");
            var labelPosYStr = values.GetValueOrDefault("labelPosY", "0");
            var labelSizeXStr = values.GetValueOrDefault("labelSizeX", "300");
            var labelSizeYStr = values.GetValueOrDefault("labelSizeY", "10");
            var textSizeStr = values.GetValueOrDefault("textSize", "1");

            var halign = values.GetValueOrDefault("halign", "left");
            var valign = values.GetValueOrDefault("valign", "top");


            var quadPosXGood = double.TryParse(quadPosXstr, out var quadPosX);
            var quadPosYGood = double.TryParse(quadPosYstr, out var quadPosY);
            var quadSizeXGood = double.TryParse(quadSizeXStr, out var quadSizeX);
            var quadSizeYGood = double.TryParse(quadSizeYStr, out var quadSizeY);
            var labelPosXGood = double.TryParse(labelPosXStr, out var labelPosX);
            var labelPosYGood = double.TryParse(labelPosYStr, out var labelPosY);
            var labelSizeXGood = double.TryParse(labelSizeXStr, out var labelSizeX);
            var labelSizeYGood = double.TryParse(labelSizeYStr, out var labelSizeY);
            var textSizeGood = double.TryParse(textSizeStr, out var textSize);

            var validHAlign = new[] { "left", "center", "right" };
            var validVAlign = new[] { "top", "center", "center2", "bottom" };

            var hAlignGood = validHAlign.Contains(halign);
            var vAlignGood = validVAlign.Contains(valign);

            if (!quadPosXGood || !quadPosYGood || !quadSizeXGood || !quadSizeYGood || !labelPosXGood || !labelPosYGood || !labelSizeXGood || !labelSizeYGood || !hAlignGood || !vAlignGood || !textSizeGood)
            {
                await tmClient.ChatSendServerMessageToLoginAsync("Invalid parameters", e.Login);
                return;
            }

            string xml = await templateEngine.RenderAsync("TrackmaniaRandomMapServer.Manialinks.Templates.textsizing.mt", new
            {
                quadPosX,
                quadPosY,
                quadSizeX,
                quadSizeY,
                labelPosX,
                labelPosY,
                labelSizeX,
                labelSizeY,
                halign,
                valign,
                textSize
            }, assemblies);

            var multicall = new TmMultiCall();
            var msg = $"quadPosX={quadPosX} quadPosY={quadPosY} quadSizeX={quadSizeX} quadSizeY={quadSizeY} labelPosX={labelPosX} labelPosY={labelPosY} labelSizeX={labelSizeX} labelSizeY={labelSizeY} halign={halign} valign={valign} textSize={textSize}";
            multicall.ChatSendServerMessageToLoginAsync(msg, e.Login);
            multicall.SendHideManialinkPageToLoginAsync(e.Login);
            multicall.SendDisplayManialinkPageToLoginAsync(e.Login, xml, 0, false);
            await tmClient.MultiCallAsync(multicall);
        }
    }
}
