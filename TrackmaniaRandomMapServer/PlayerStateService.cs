using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TrackmaniaRandomMapServer
{
    public class PlayerStateService
    {
        private readonly Dictionary<string, PlayerState> playerStates = new();

        public PlayerStateService()
        {
        }

        public int CurrentPlayerCount()
        {
            return playerStates.Values.ExcludeSpectators().Count();
        }

        public int GoldSkipVotes()
        {
            return playerStates.Values.ExcludeSpectators().Count(x => x.VoteGoldSkip);
        }

        public int SkipVotes()
        {
            return playerStates.Values.ExcludeSpectators().Count(x => x.VoteSkip);
        }

        public int QuitVotes()
        {
            return playerStates.Values.ExcludeSpectators().Count(x => x.VoteQuit);
        }

        public PlayerState GetPlayerState(string login)
        {
            if (!playerStates.TryGetValue(login, out var playerState))
            {
                playerState = new PlayerState
                {
                    IsSpectator = true,
                };
                playerStates.Add(login, playerState);
            }
            return playerState;
        }

        public void UpsertPlayerState(string login, PlayerState playerState)
        {
            playerStates[login] = playerState;
        }

        public void CancelAllVotes()
        {
            foreach (var playerState in playerStates.Values)
            {
                playerState.VoteGoldSkip = false;
                playerState.VoteSkip = false;
                playerState.VoteQuit = false;
            }
        }

        public string PlayerNames()
        {
            return string.Join(", ", playerStates.Select(kvp => kvp.Value.NickName ?? kvp.Key));
        }

        private string FormatTime(int time)
        {
            var ts = TimeSpan.FromMilliseconds(time);
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        public IEnumerable<LeaderboardItem> GetLeaderboard()
        {
            return playerStates.Where(x => x.Value.NumWins > 0 || x.Value.GoodSkips > 0 || x.Value.BestMapTime is not null)
                .OrderByDescending(x => x.Value.NumWins)
                .ThenByDescending(x => x.Value.GoodSkips)
                .ThenBy(x => x.Value.BestMapTime)
                .Take(10)
                .Select(x => new LeaderboardItem
                {
                    DisplayName = x.Value.NickName ?? x.Key,
                    NumWins = x.Value.NumWins,
                    GoodSkips = x.Value.GoodSkips,
                    BestTime = FormatTime(x.Value.BestMapTime ?? 0),
            });
        }
}

static class PlayerStateServiceExtensions
{
    public static IEnumerable<PlayerState> ExcludeSpectators(this IEnumerable<PlayerState> playerStates)
    {
        return playerStates.Where(x => !x.IsSpectator);
    }
}   
}
