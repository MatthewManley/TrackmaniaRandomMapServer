using System;
using System.Collections.Generic;
using System.Linq;
using TrackmaniaRandomMapServer.Models;

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

        public string GetLoginFromDisplayName(string displayName)
        {
            var lower = displayName.ToLower();
            return playerStates.FirstOrDefault(x => x.Value.NickName.ToLower() == lower).Key;
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

        public void ClearPlayerScores()
        {
            foreach (var ps in playerStates.Values)
            {
                ps.GoodSkips = 0;
                ps.NumWins = 0;
            }
        }

        public void ClearBestTimes()
        {
            foreach (var playerState in playerStates.Values)
            {
                playerState.BestMapTime = null;
            }
        }

        public IEnumerable<KeyValuePair<string, PlayerState>> Players()
        {
            return playerStates.Select(x => x);
        }

        private string FormatTime(int? time)
        {
            if (time is null)
            {
                return "--:--.---";
            }
            var ts = TimeSpan.FromMilliseconds(time.Value);
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        public IEnumerable<LeaderboardItem> GetLeaderboard()
        {
            return playerStates
                .Where(x => !x.Value.IsSpectator || x.Value.NumWins > 0 || x.Value.GoodSkips > 0 || x.Value.BestMapTime.HasValue)
                .OrderByDescending(x => x.Value.NumWins)
                .ThenByDescending(x => x.Value.GoodSkips)
                .ThenByDescending(x => x.Value.BestMapTime.HasValue ? 1 : 0)
                .ThenBy(x => x.Value.BestMapTime)
                .Select(x => new LeaderboardItem
                {
                    DisplayName = x.Value.NickName ?? x.Key,
                    NumWins = x.Value.NumWins,
                    GoodSkips = x.Value.GoodSkips,
                    BestTime = FormatTime(x.Value.BestMapTime),
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
