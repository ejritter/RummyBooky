using System;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace RummyBooky.Tests;

public class PlayerModelStub
{
    public string PlayerName { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public double LifetimeScore { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }
    public int GamesForfeit { get; set; }
    public int GameDraws { get; set; }
    public int HighestScoredHand { get; set; }
    public int LowestScoredHand { get; set; }
    public DateTime PlayerCreatedDate { get; set; } = DateTime.Now;
}

public class ScoreOrderingTests
{
    [Fact]
    public void ScoreOrdering_ActivePlayers_SortedAscendingByPlayerScore()
    {
        // Arrange: Players with varying active PlayerScore vs LifetimeScore
        var players = new List<PlayerModelStub>
        {
            new() { PlayerName = "Alice", PlayerScore = 150, LifetimeScore = 5000 },
            new() { PlayerName = "Bob", PlayerScore = 20, LifetimeScore = 1000 },
            new() { PlayerName = "Charlie", PlayerScore = 85, LifetimeScore = 9000 },
            new() { PlayerName = "Diana", PlayerScore = 0, LifetimeScore = 300 },
            new() { PlayerName = "Evan", PlayerScore = 20, LifetimeScore = 800 },
            new() { PlayerName = "Frank", PlayerScore = 210, LifetimeScore = 200 }
        };

        // Act: LINQ IntroSort matching CardBoxView.xaml.cs:96-99
        var ordered = players
            .OrderBy(player => player.PlayerScore)
            .ThenBy(player => player.PlayerName)
            .ToList();

        // Assert: Lowest score (0, Diana) first, highest score (210, Frank) last
        Assert.Equal("Diana", ordered[0].PlayerName);
        Assert.Equal(0, ordered[0].PlayerScore);

        Assert.Equal("Bob", ordered[1].PlayerName);
        Assert.Equal(20, ordered[1].PlayerScore);

        Assert.Equal("Evan", ordered[2].PlayerName);
        Assert.Equal(20, ordered[2].PlayerScore);

        Assert.Equal("Charlie", ordered[3].PlayerName);
        Assert.Equal(85, ordered[3].PlayerScore);

        Assert.Equal("Alice", ordered[4].PlayerName);
        Assert.Equal(150, ordered[4].PlayerScore);

        Assert.Equal("Frank", ordered[5].PlayerName);
        Assert.Equal(210, ordered[5].PlayerScore);
    }

    [Fact]
    public void ScoreOrdering_EmptyOrSinglePlayer_HandlesGracefully()
    {
        var emptyList = new List<PlayerModelStub>();
        var orderedEmpty = emptyList.OrderBy(p => p.PlayerScore).ThenBy(p => p.PlayerName).ToList();
        Assert.Empty(orderedEmpty);

        var singleList = new List<PlayerModelStub> { new() { PlayerName = "Solo", PlayerScore = 42 } };
        var orderedSingle = singleList.OrderBy(p => p.PlayerScore).ThenBy(p => p.PlayerName).ToList();
        Assert.Single(orderedSingle);
        Assert.Equal("Solo", orderedSingle[0].PlayerName);
    }

    [Fact]
    public void ScoreOrdering_NegativeAndLargeScores_SortedCorrectly()
    {
        var players = new List<PlayerModelStub>
        {
            new() { PlayerName = "P1", PlayerScore = 1_000_000 },
            new() { PlayerName = "P2", PlayerScore = -50 },
            new() { PlayerName = "P3", PlayerScore = -100 },
            new() { PlayerName = "P4", PlayerScore = 0 }
        };

        var ordered = players.OrderBy(p => p.PlayerScore).ThenBy(p => p.PlayerName).ToList();
        Assert.Equal("P3", ordered[0].PlayerName);
        Assert.Equal("P2", ordered[1].PlayerName);
        Assert.Equal("P4", ordered[2].PlayerName);
        Assert.Equal("P1", ordered[3].PlayerName);
    }
}
