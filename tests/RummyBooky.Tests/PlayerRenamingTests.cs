using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class PlayerRenamingTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
        public int PlayerScore { get; set; }
    }

    public class RoundStub
    {
        public Guid GameId { get; init; }
        public PlayerStub? LeadingPlayer { get; set; }
        public PlayerStub? PlayerHighestScoringHand { get; set; }
        public PlayerStub? PlayerLowestScoringHand { get; set; }
        public ObservableCollection<PlayerStub> PlayersScoredHandThisRound { get; set; } = [];
    }

    public class GameStub
    {
        public Guid GameId { get; init; } = Guid.NewGuid();
        public ObservableCollection<PlayerStub> Players { get; set; } = [];
        public ObservableCollection<RoundStub> Round { get; set; } = [];
        public bool IsGameActive { get; set; } = true;
    }

    public class GameServiceRenamingSimulation
    {
        private readonly Dictionary<Guid, PlayerStub> _allPlayers = [];
        private readonly List<GameStub> _games = [];

        public void SeedPlayer(PlayerStub player) => _allPlayers[player.ID] = player;
        public void SeedGame(GameStub game) => _games.Add(game);
        public PlayerStub? GetPlayer(Guid id) => _allPlayers.TryGetValue(id, out var p) ? p : null;
        public IReadOnlyList<GameStub> GetAllGames() => _games.AsReadOnly();

        public async Task<bool> UpdatePlayerNameHistoryAsync(PlayerStub renamePlayer, string newName)
        {
            if (renamePlayer is null || string.IsNullOrWhiteSpace(newName))
                return false;

            renamePlayer.PlayerName = newName.Trim();
            if (_allPlayers.TryGetValue(renamePlayer.ID, out var cached))
            {
                cached.PlayerName = newName.Trim();
            }

            foreach (var game in _games)
            {
                RenamePlayerReferencesInGame(game, renamePlayer.ID, newName.Trim());
            }

            return await Task.FromResult(true);
        }

        private static bool RenamePlayerReferencesInGame(GameStub game, Guid playerId, string newName)
        {
            var changed = false;
            var inGame = game.Players.FirstOrDefault(p => p.ID == playerId);
            if (inGame != null)
            {
                inGame.PlayerName = newName;
                changed = true;
            }

            foreach (var round in game.Round)
            {
                foreach (var p in round.PlayersScoredHandThisRound)
                {
                    if (p.ID == playerId)
                    {
                        p.PlayerName = newName;
                        changed = true;
                    }
                }

                if (round.LeadingPlayer?.ID == playerId)
                {
                    round.LeadingPlayer.PlayerName = newName;
                    changed = true;
                }

                if (round.PlayerHighestScoringHand?.ID == playerId)
                {
                    round.PlayerHighestScoringHand.PlayerName = newName;
                    changed = true;
                }

                if (round.PlayerLowestScoringHand?.ID == playerId)
                {
                    round.PlayerLowestScoringHand.PlayerName = newName;
                    changed = true;
                }
            }

            return changed;
        }
    }

    [Fact]
    public async Task UpdatePlayerNameHistory_MutatesPlayerNameAndPropagatesAcrossActiveAndPlayedGames()
    {
        var service = new GameServiceRenamingSimulation();
        var player = new PlayerStub { PlayerName = "Eric" };
        var otherPlayer = new PlayerStub { PlayerName = "Brodie" };
        service.SeedPlayer(player);
        service.SeedPlayer(otherPlayer);

        var activeGame = new GameStub
        {
            IsGameActive = true,
            Players = [new PlayerStub { ID = player.ID, PlayerName = "Eric" }, otherPlayer],
            Round = [new RoundStub { LeadingPlayer = new PlayerStub { ID = player.ID, PlayerName = "Eric" } }]
        };

        var playedGame = new GameStub
        {
            IsGameActive = false,
            Players = [new PlayerStub { ID = player.ID, PlayerName = "Eric" }, otherPlayer],
            Round = [new RoundStub { PlayerHighestScoringHand = new PlayerStub { ID = player.ID, PlayerName = "Eric" } }]
        };

        service.SeedGame(activeGame);
        service.SeedGame(playedGame);

        var result = await service.UpdatePlayerNameHistoryAsync(player, "Eric Ritter");

        Assert.True(result);
        Assert.Equal("Eric Ritter", player.PlayerName);
        Assert.Equal("Eric Ritter", service.GetPlayer(player.ID)?.PlayerName);
        Assert.Equal("Eric Ritter", activeGame.Players.First(p => p.ID == player.ID).PlayerName);
        Assert.Equal("Eric Ritter", activeGame.Round[0].LeadingPlayer?.PlayerName);
        Assert.Equal("Eric Ritter", playedGame.Players.First(p => p.ID == player.ID).PlayerName);
        Assert.Equal("Eric Ritter", playedGame.Round[0].PlayerHighestScoringHand?.PlayerName);
    }

    [Fact]
    public async Task UpdatePlayerNameHistory_ZeroHistoricalGames_ReturnsTrueAndUpdatesCache()
    {
        var service = new GameServiceRenamingSimulation();
        var newPlayer = new PlayerStub { PlayerName = "Rookie" };
        service.SeedPlayer(newPlayer);

        var result = await service.UpdatePlayerNameHistoryAsync(newPlayer, "Veteran");

        Assert.True(result);
        Assert.Equal("Veteran", newPlayer.PlayerName);
        Assert.Equal("Veteran", service.GetPlayer(newPlayer.ID)?.PlayerName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task UpdatePlayerNameHistory_InvalidOrWhitespaceName_ReturnsFalseWithoutMutating(string? invalidName)
    {
        var service = new GameServiceRenamingSimulation();
        var player = new PlayerStub { PlayerName = "Original" };
        service.SeedPlayer(player);

        var result = await service.UpdatePlayerNameHistoryAsync(player, invalidName!);

        Assert.False(result);
        Assert.Equal("Original", player.PlayerName);
    }
}
