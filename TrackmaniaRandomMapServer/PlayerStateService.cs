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

        public void ClearBestTimes()
        {
            foreach (var playerState in playerStates.Values)
            {
                playerState.BestMapTime = null;
            }
        }

        public string PlayerNames()
        {
            return string.Join(", ", playerStates.Select(kvp => kvp.Value.NickName ?? kvp.Key));
        }

        private string FormatTime(int? time)
        {
            if (time is null)
            {
                return "--:--.--";
            }
            var ts = TimeSpan.FromMilliseconds(time.Value);
            return $"{ts.Minutes:D2}:{ts.Seconds:D2}.{ts.Milliseconds:D3}";
        }

        public IEnumerable<LeaderboardItem> GetLeaderboard()
        {
            //var fakePlayers = new List<KeyValuePair<string, PlayerState>>
            //{
            //    new("FakePlayer1", new PlayerState { NumWins = 1, BestMapTime = 123456, GoodSkips = 2 }),
            //    new("FakePlayer2", new PlayerState { NumWins = 10, BestMapTime = 234567, GoodSkips = 1 }),
            //    new("FakePlayer3", new PlayerState { NumWins = 3, BestMapTime = 345678, GoodSkips = 10 }),
            //    new("FakePlayer4", new PlayerState { NumWins = 4, BestMapTime = 456789, GoodSkips = 3 }),
            //    new("FakePlayer5", new PlayerState { NumWins = 5, BestMapTime = 567890, GoodSkips = 4 }),
            //    new("FakePlayer6", new PlayerState { NumWins = 6, BestMapTime = 678901, GoodSkips = 5 }),
            //    new("FakePlayer7", new PlayerState { NumWins = 7, BestMapTime = 789012, GoodSkips = 6 }),
            //    new("FakePlayer8", new PlayerState { NumWins = 8, BestMapTime = 890123, GoodSkips = 7 }),
            //    new("FakePlayer9", new PlayerState { NumWins = 9, BestMapTime = 901234, GoodSkips = 8 }),
            //    new("FakePlayer10", new PlayerState { NumWins = 2, BestMapTime = 101234, GoodSkips = 9 }),
            //};
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
