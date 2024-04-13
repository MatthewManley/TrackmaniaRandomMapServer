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
        private async Task TmClient_OnPlayerChat(object sender, PlayerChatGbxEventArgs e)
        {
            var parts = e.Text.Split(' ');
            if (e.IsRegisteredCmd)
                return;
            if (parts.Length == 0)
                return;
            var cmd = parts[0].ToLower();
            switch (cmd)
            {
                case "/players":
                    await PlayersCommand(parts, e);
                    break;
                default:
                    break;
            }
        }

        private async Task PlayersCommand(string[] parts, PlayerChatGbxEventArgs e)
        {
            var response = "Players: " + string.Join(", ", ConnectedPlayers.Select(keyValue => $"{keyValue.Key}:{keyValue.Value.NickName ?? "null"}"));
            await tmClient.ChatSendServerMessageToLoginAsync(response, e.Login);
        }
    }
}
