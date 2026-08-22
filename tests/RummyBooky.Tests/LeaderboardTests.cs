using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class LeaderboardTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
        public int LifetimeScore { get; set; }
        public int GamesWon { get; set; }
        public int TotalGamesPlayed { get; set; }
        public int Rank { get; set; }
    }

    public class LeaderboardViewModelSimulation
    {
        private readonly Func<Task<List<PlayerStub>>> _getTopPlayersFunc;

        public ObservableCollection<PlayerStub> TopPlayers { get; } = [];
        public bool DisplayLeaderboard { get; set; }
        public string HeaderText { get; set; } = "Leaderboard";

        public LeaderboardViewModelSimulation(Func<Task<List<PlayerStub>>> getTopPlayersFunc)
        {
            _getTopPlayersFunc = getTopPlayersFunc;
        }

        public async Task AppearingAsync()
        {
            await LoadLeaderboardAsync();
            DisplayLeaderboard = TopPlayers.Count > 0;
            HeaderText = DisplayLeaderboard ? "Leaderboard" : "No player stats to provide. Please play a game.";
        }

        public async Task RefreshStandingsAsync()
        {
            await AppearingAsync();
        }

        private async Task LoadLeaderboardAsync()
        {
            var players = await _getTopPlayersFunc();
            TopPlayers.Clear();
            foreach (var player in players)
            {
                TopPlayers.Add(player);
            }
        }
    }

    [Fact]
    public async Task RefreshStandings_PopulatesTopPlayersDirectlyWithoutBlanks()
    {
        var db = new List<PlayerStub>
        {
            new() { PlayerName = "Brodie", LifetimeScore = 500, GamesWon = 5, TotalGamesPlayed = 6, Rank = 1 },
            new() { PlayerName = "Eric", LifetimeScore = 320, GamesWon = 2, TotalGamesPlayed = 6, Rank = 2 }
        };

        var vm = new LeaderboardViewModelSimulation(() => Task.FromResult(db.OrderByDescending(p => p.LifetimeScore).ToList()));

        await vm.AppearingAsync();

        Assert.True(vm.DisplayLeaderboard);
        Assert.Equal("Leaderboard", vm.HeaderText);
        Assert.Equal(2, vm.TopPlayers.Count);
        Assert.Equal("Brodie", vm.TopPlayers[0].PlayerName);
        Assert.Equal(500, vm.TopPlayers[0].LifetimeScore);
        Assert.Equal(1, vm.TopPlayers[0].Rank);
        Assert.Equal("Eric", vm.TopPlayers[1].PlayerName);
        Assert.Equal(320, vm.TopPlayers[1].LifetimeScore);
        Assert.Equal(2, vm.TopPlayers[1].Rank);

        // Add a new game / score update to db
        db[1].LifetimeScore = 600; // Eric takes lead
        db[1].GamesWon = 6;
        db[0].Rank = 2;
        db[1].Rank = 1;

        // Refresh standings
        await vm.RefreshStandingsAsync();

        Assert.Equal(2, vm.TopPlayers.Count);
        Assert.Equal("Eric", vm.TopPlayers[0].PlayerName);
        Assert.Equal(600, vm.TopPlayers[0].LifetimeScore);
        Assert.Equal(1, vm.TopPlayers[0].Rank);
        Assert.Equal("Brodie", vm.TopPlayers[1].PlayerName);
        Assert.Equal(500, vm.TopPlayers[1].LifetimeScore);
        Assert.Equal(2, vm.TopPlayers[1].Rank);
    }

    [Fact]
    public async Task RefreshStandings_EmptyDb_DisplaysEmptyMessage()
    {
        var vm = new LeaderboardViewModelSimulation(() => Task.FromResult(new List<PlayerStub>()));

        await vm.RefreshStandingsAsync();

        Assert.False(vm.DisplayLeaderboard);
        Assert.Equal("No player stats to provide. Please play a game.", vm.HeaderText);
        Assert.Empty(vm.TopPlayers);
    }
}
