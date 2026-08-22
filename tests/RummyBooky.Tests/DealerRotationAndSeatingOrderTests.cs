using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class DealerRotationAndSeatingOrderTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
        public int PlayerScore { get; set; }
        public bool IsDealer { get; set; }
    }

    public class GameModelStub
    {
        public ObservableCollection<PlayerStub> Players { get; set; } = [];
    }

    public class DealerRotationSimulation
    {
        public static bool SetNextDealerForNewRound(GameModelStub game)
        {
            if (game.Players.Count == 0) return false;

            var currentDealerIndex = -1;
            for (int i = 0; i < game.Players.Count; i++)
            {
                if (game.Players[i].IsDealer)
                {
                    currentDealerIndex = i;
                    break;
                }
            }

            if (currentDealerIndex == -1)
            {
                game.Players[0].IsDealer = true;
                return true;
            }

            var nextDealerIndex = (currentDealerIndex + 1) % game.Players.Count;
            game.Players[currentDealerIndex].IsDealer = false;
            game.Players[nextDealerIndex].IsDealer = true;
            return true;
        }

        public static bool SetRandomDealer(GameModelStub game, Random rng)
        {
            if (game.Players.Count == 0) return false;
            foreach (var p in game.Players) p.IsDealer = false;

            var randomIndex = rng.Next(0, game.Players.Count);
            game.Players[randomIndex].IsDealer = true;
            return true;
        }

        public static bool SetPlayerScoreSafely(PlayerStub player, string scoreText)
        {
            if (int.TryParse(scoreText, out var scoreVal))
            {
                player.PlayerScore += scoreVal;
                return true;
            }
            return false;
        }
    }

    [Fact]
    public void TwoPlayerGame_DealerAlternatesEachRound()
    {
        var alice = new PlayerStub { PlayerName = "Alice", IsDealer = true };
        var bob = new PlayerStub { PlayerName = "Bob", IsDealer = false };
        var game = new GameModelStub { Players = [alice, bob] };

        // Round 1: Alice is dealer
        Assert.True(alice.IsDealer);
        Assert.False(bob.IsDealer);

        // Advance to Round 2
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.False(alice.IsDealer);
        Assert.True(bob.IsDealer);

        // Advance to Round 3
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.True(alice.IsDealer);
        Assert.False(bob.IsDealer);

        // Advance to Round 4
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.False(alice.IsDealer);
        Assert.True(bob.IsDealer);
    }

    [Fact]
    public void ThreePlusPlayerGame_DealerRotatesClockwiseToPlayersLeft()
    {
        var p1 = new PlayerStub { PlayerName = "Seat 1 (Dealer)", IsDealer = true };
        var p2 = new PlayerStub { PlayerName = "Seat 2", IsDealer = false };
        var p3 = new PlayerStub { PlayerName = "Seat 3", IsDealer = false };
        var p4 = new PlayerStub { PlayerName = "Seat 4", IsDealer = false };
        var game = new GameModelStub { Players = [p1, p2, p3, p4] };

        // Round 1 -> p1 deals
        Assert.True(p1.IsDealer);

        // Round 2 -> p2 deals (p1's left)
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.False(p1.IsDealer);
        Assert.True(p2.IsDealer);

        // Round 3 -> p3 deals (p2's left)
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.False(p2.IsDealer);
        Assert.True(p3.IsDealer);

        // Round 4 -> p4 deals (p3's left)
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.False(p3.IsDealer);
        Assert.True(p4.IsDealer);

        // Round 5 -> p1 deals (wrap around to p4's left)
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.False(p4.IsDealer);
        Assert.True(p1.IsDealer);
    }

    [Fact]
    public void SeatingOrder_PreservedAcrossRoundsRegardlessOfScores()
    {
        var alice = new PlayerStub { PlayerName = "Alice", PlayerScore = 100, IsDealer = true };
        var charlie = new PlayerStub { PlayerName = "Charlie", PlayerScore = 500, IsDealer = false };
        var bob = new PlayerStub { PlayerName = "Bob", PlayerScore = 250, IsDealer = false };

        // Seating order around the table: Alice, Charlie, Bob
        var game = new GameModelStub { Players = [alice, charlie, bob] };

        // Ensure order is not scrambled by score or name
        Assert.Equal("Alice", game.Players[0].PlayerName);
        Assert.Equal("Charlie", game.Players[1].PlayerName);
        Assert.Equal("Bob", game.Players[2].PlayerName);

        // After round 1, Charlie deals (seated at index 1)
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.True(game.Players[1].IsDealer);
        Assert.Equal("Charlie", game.Players[1].PlayerName);

        // After round 2, Bob deals (seated at index 2)
        DealerRotationSimulation.SetNextDealerForNewRound(game);
        Assert.True(game.Players[2].IsDealer);
        Assert.Equal("Bob", game.Players[2].PlayerName);
    }

    [Fact]
    public void RandomDealerSelection_AssignsExactlyOneDealer()
    {
        var rng = new Random(12345);
        var players = Enumerable.Range(1, 5)
            .Select(i => new PlayerStub { PlayerName = $"Player {i}" })
            .ToList();
        var game = new GameModelStub { Players = new ObservableCollection<PlayerStub>(players) };

        DealerRotationSimulation.SetRandomDealer(game, rng);

        Assert.Equal(1, game.Players.Count(p => p.IsDealer));
    }

    [Fact]
    public void SetNextDealer_WhenNoDealerAssigned_AssignsFirstPlayerAsDealer()
    {
        var p1 = new PlayerStub { PlayerName = "Brodie", IsDealer = false };
        var p2 = new PlayerStub { PlayerName = "Renegade", IsDealer = false };
        var game = new GameModelStub { Players = [p1, p2] };

        var result = DealerRotationSimulation.SetNextDealerForNewRound(game);

        Assert.True(result);
        Assert.True(p1.IsDealer);
        Assert.False(p2.IsDealer);
    }

    [Fact]
    public void SetPlayerScore_SafeParsing_HandlesValidAndMalformedInput()
    {
        var player = new PlayerStub { PlayerName = "Brodie", PlayerScore = 100 };

        var validResult = DealerRotationSimulation.SetPlayerScoreSafely(player, "50");
        Assert.True(validResult);
        Assert.Equal(150, player.PlayerScore);

        var emptyResult = DealerRotationSimulation.SetPlayerScoreSafely(player, "");
        Assert.False(emptyResult);
        Assert.Equal(150, player.PlayerScore);

        var invalidResult = DealerRotationSimulation.SetPlayerScoreSafely(player, "abc");
        Assert.False(invalidResult);
        Assert.Equal(150, player.PlayerScore);
    }
}
