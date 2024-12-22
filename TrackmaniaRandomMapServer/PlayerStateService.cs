using System;
using System.Collections.Generic;
using System.Linq;
using TrackmaniaRandomMapServer.Models;

namespace TrackmaniaRandomMapServer
{
    public class PlayerStateService
    {
        public readonly Dictionary<string, PlayerState> PlayerStates = new();

        public PlayerStateService()
        {
        }

        public int CurrentPlayerCount()
        {
            return PlayerStates.Values.ExcludeSpectators().Count();
        }

        public int GoldSkipVotes()
        {
            return PlayerStates.Values.ExcludeSpectators().Count(x => x.VoteGoldSkip);
        }

        public int SkipVotes()
        {
            return PlayerStates.Values.ExcludeSpectators().Count(x => x.VoteSkip);
        }

        public int QuitVotes()
        {
            return PlayerStates.Values.ExcludeSpectators().Count(x => x.VoteQuit);
        }

        public string GetLoginFromDisplayName(string displayName)
        {
            var lower = displayName.ToLower();
            return PlayerStates.FirstOrDefault(x => x.Value.NickName.ToLower() == lower).Key;
        }

        public PlayerState GetPlayerState(string login)
        {
            if (!PlayerStates.TryGetValue(login, out var playerState))
            {
                playerState = new PlayerState
                {
                    IsSpectator = true,
                };
                PlayerStates.Add(login, playerState);
            }
            return playerState;
        }

        public void UpsertPlayerState(string login, PlayerState playerState)
        {
            PlayerStates[login] = playerState;
        }

        public void CancelAllVotes()
        {
            foreach (var playerState in PlayerStates.Values)
            {
                playerState.VoteGoldSkip = false;
                playerState.VoteSkip = false;
                playerState.VoteQuit = false;
            }
        }

        public void ClearPlayerScores()
        {
            foreach (var ps in PlayerStates.Values)
            {
                ps.GoodSkips = 0;
                ps.NumWins = 0;
            }
        }

        public void ClearBestTimes()
        {
            foreach (var playerState in PlayerStates.Values)
            {
                playerState.BestMapTime = null;
            }
        }

        public IEnumerable<KeyValuePair<string, PlayerState>> Players()
        {
            return PlayerStates.Select(x => x);
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
            return PlayerStates
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
