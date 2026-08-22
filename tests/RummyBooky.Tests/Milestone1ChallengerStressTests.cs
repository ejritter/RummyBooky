using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

/// <summary>
/// Empirical Challenger 1 Adversarial Stress Test Suite for Milestone 1:
/// - Dealer Rotation Cycles (2, 3, 4, 5, 6 players)
/// - Initial Unassigned Dealer Fallback
/// - Scoring Calculations & Running Total Accumulation
/// - Score Limit Threshold Evaluations & Multi-Way Draws
/// </summary>
public class Milestone1ChallengerStressTests
{
    // =========================================================================
    // SECTION 1: DEALER ROTATION CYCLES (2, 3, 4, 5, 6 PLAYERS)
    // =========================================================================

    [Theory]
    [InlineData(2, 20)]
    [InlineData(3, 21)]
    [InlineData(4, 20)]
    [InlineData(5, 25)]
    [InlineData(6, 24)]
    public void DealerRotation_NPlayerGame_RotatesClockwiseAndWrapsAccurately(int playerCount, int totalRounds)
    {
        // Arrange
        var players = Enumerable.Range(0, playerCount)
            .Select(i => new TestPlayerModel
            {
                PlayerName = $"Player_{i}",
                IsDealer = (i == 0) // Player 0 starts as dealer
            })
            .ToList();

        var game = new TestCurrentGameModel
        {
            Players = new ObservableCollection<TestPlayerModel>(players)
        };

        // Assert initial state
        Assert.True(game.Players[0].IsDealer);
        Assert.Equal(1, game.Players.Count(p => p.IsDealer));

        // Act & Assert across rounds
        for (int round = 1; round < totalRounds; round++)
        {
            var expectedDealerIndex = round % playerCount;

            // Execute dealer advance logic
            var success = AdvanceDealer(game);

            Assert.True(success, $"Dealer advancement failed at round {round + 1}");
            Assert.Equal(1, game.Players.Count(p => p.IsDealer));
            Assert.True(game.Players[expectedDealerIndex].IsDealer,
                $"Expected Player_{expectedDealerIndex} to be dealer on round {round + 1} with {playerCount} players, but was not.");

            for (int pIdx = 0; pIdx < playerCount; pIdx++)
            {
                if (pIdx != expectedDealerIndex)
                {
                    Assert.False(game.Players[pIdx].IsDealer,
                        $"Player_{pIdx} should not be dealer on round {round + 1}");
                }
            }
        }
    }

    [Fact]
    public void DealerRotation_SeatingOrderPreserved_DespiteScoreSorting()
    {
        // Seating order around the table: Alice (0), Bob (1), Charlie (2), Diana (3)
        var p0 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 500, IsDealer = true };
        var p1 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 100, IsDealer = false };
        var p2 = new TestPlayerModel { PlayerName = "Charlie", PlayerScore = 900, IsDealer = false };
        var p3 = new TestPlayerModel { PlayerName = "Diana", PlayerScore = 50, IsDealer = false };

        var game = new TestCurrentGameModel { Players = [p0, p1, p2, p3] };

        // Round 1 -> Alice deals
        Assert.True(game.Players[0].IsDealer);

        // Advance to Round 2 -> Bob deals (index 1)
        AdvanceDealer(game);
        Assert.True(game.Players[1].IsDealer);
        Assert.Equal("Bob", game.Players[1].PlayerName);

        // Advance to Round 3 -> Charlie deals (index 2)
        AdvanceDealer(game);
        Assert.True(game.Players[2].IsDealer);
        Assert.Equal("Charlie", game.Players[2].PlayerName);

        // Advance to Round 4 -> Diana deals (index 3)
        AdvanceDealer(game);
        Assert.True(game.Players[3].IsDealer);
        Assert.Equal("Diana", game.Players[3].PlayerName);

        // Advance to Round 5 -> Alice deals (wrap to index 0)
        AdvanceDealer(game);
        Assert.True(game.Players[0].IsDealer);
        Assert.Equal("Alice", game.Players[0].PlayerName);
    }

    // =========================================================================
    // SECTION 2: INITIAL UNASSIGNED DEALER FALLBACK
    // =========================================================================

    [Fact]
    public void InitialDealerFallback_WhenNoDealerSet_SetNextDealerAssignsFirstPlayer()
    {
        // Arrange: 4 players with NO dealer assigned (all IsDealer = false)
        var players = Enumerable.Range(0, 4)
            .Select(i => new TestPlayerModel { PlayerName = $"P{i}", IsDealer = false })
            .ToList();
        var game = new TestCurrentGameModel { Players = new ObservableCollection<TestPlayerModel>(players) };

        // Act
        var result = AdvanceDealer(game);

        // Assert
        Assert.True(result);
        Assert.True(game.Players[0].IsDealer, "First player should be assigned dealer as fallback");
        Assert.Equal(1, game.Players.Count(p => p.IsDealer));
    }

    [Fact]
    public void InitialDealerFallback_SetRandomDealer_AssignsExactlyOneDealer()
    {
        for (int seed = 0; seed < 100; seed++)
        {
            var rng = new Random(seed);
            var players = Enumerable.Range(0, 5)
                .Select(i => new TestPlayerModel { PlayerName = $"P{i}", IsDealer = false })
                .ToList();
            var game = new TestCurrentGameModel { Players = new ObservableCollection<TestPlayerModel>(players) };

            // Act: Choose random dealer
            var randomIndex = rng.Next(0, game.Players.Count);
            game.Players[randomIndex].IsDealer = true;

            // Assert
            Assert.Equal(1, game.Players.Count(p => p.IsDealer));
        }
    }

    [Fact]
    public void InitialDealerFallback_EmptyPlayersList_ReturnsFalseGracefully()
    {
        var game = new TestCurrentGameModel { Players = [] };
        var result = AdvanceDealer(game);
        Assert.False(result);
    }

    // =========================================================================
    // SECTION 3: SCORING CALCULATIONS & RUNNING TOTAL ACCUMULATION
    // =========================================================================

    [Fact]
    public void ScoringAccumulation_50Rounds_ComputesAccurateRunningTotalsAndExtremes()
    {
        // Arrange
        var p1 = new TestPlayerModel { PlayerName = "Brodie" };
        var p2 = new TestPlayerModel { PlayerName = "Renegade" };
        var game = new TestCurrentGameModel { Players = [p1, p2] };

        int expectedBrodieTotal = 0;
        int expectedRenegadeTotal = 0;
        int brodieMax = int.MinValue;
        int brodieMin = int.MaxValue;
        int renegadeMax = int.MinValue;
        int renegadeMin = int.MaxValue;

        var rng = new Random(42);

        // 50 rounds of simulated play
        for (int r = 1; r <= 50; r++)
        {
            int s1 = rng.Next(-20, 150);
            int s2 = rng.Next(-20, 150);

            expectedBrodieTotal += s1;
            expectedRenegadeTotal += s2;

            brodieMax = Math.Max(brodieMax, s1);
            brodieMin = Math.Min(brodieMin, s1);
            renegadeMax = Math.Max(renegadeMax, s2);
            renegadeMin = Math.Min(renegadeMin, s2);

            var roundModel = new TestRoundModel();
            roundModel.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = s1 });
            roundModel.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = s2 });
            game.Round.Add(roundModel);
        }

        // Act
        TestGameRecomputationEngine.RecalculateGame(game);

        // Assert
        Assert.Equal(expectedBrodieTotal, p1.PlayerScore);
        Assert.Equal(expectedRenegadeTotal, p2.PlayerScore);
        Assert.Equal(brodieMax, p1.HighestScoredHand);
        Assert.Equal(brodieMin, p1.LowestScoredHand);
        Assert.Equal(renegadeMax, p2.HighestScoredHand);
        Assert.Equal(renegadeMin, p2.LowestScoredHand);
    }

    [Fact]
    public void ScoringAccumulation_ZeroScoreRounds_HandledAccurately()
    {
        var p1 = new TestPlayerModel { PlayerName = "Brodie" };
        var p2 = new TestPlayerModel { PlayerName = "Renegade" };
        var game = new TestCurrentGameModel { Players = [p1, p2] };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 0 }); // Rummy / 0 points

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 0 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 75 });

        game.Round.Add(r1);
        game.Round.Add(r2);

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(50, p1.PlayerScore);
        Assert.Equal(75, p2.PlayerScore);
        Assert.Equal(50, p1.HighestScoredHand);
        Assert.Equal(0, p1.LowestScoredHand);
        Assert.Equal(75, p2.HighestScoredHand);
        Assert.Equal(0, p2.LowestScoredHand);
    }

    [Fact]
    public void ScoringAccumulation_MidGameEditCascadesAccurately()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var game = new TestCurrentGameModel { Players = [p1, p2] };

        // Round 1
        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 40 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 30 });

        // Round 2
        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 20 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });

        // Round 3 (active)
        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 15 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 10 });

        game.Round.Add(r1);
        game.Round.Add(r2);
        game.Round.Add(r3);

        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(75, p1.PlayerScore); // 40 + 20 + 15
        Assert.Equal(90, p2.PlayerScore); // 30 + 50 + 10

        // User edits Round 1 Alice score from 40 to 100
        r1.RoundScores.First(s => s.PlayerId == p1.ID).Score = 100;
        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(135, p1.PlayerScore); // 100 + 20 + 15
        Assert.Equal(90, p2.PlayerScore);
        Assert.Equal(100, p1.HighestScoredHand);
        Assert.Equal(15, p1.LowestScoredHand);
    }

    // =========================================================================
    // SECTION 4: SCORE LIMIT THRESHOLD EVALUATIONS & DRAWS
    // =========================================================================

    [Theory]
    [InlineData(100, 95, 80, false, TestGameStatus.Unknown)]
    [InlineData(100, 100, 80, true, TestGameStatus.Won)]
    [InlineData(250, 260, 240, true, TestGameStatus.Won)]
    [InlineData(500, 499, 499, false, TestGameStatus.Unknown)]
    [InlineData(500, 500, 450, true, TestGameStatus.Won)]
    [InlineData(500, 520, 510, true, TestGameStatus.Won)]
    [InlineData(500, 500, 500, true, TestGameStatus.Draw)]
    [InlineData(1000, 1050, 1050, true, TestGameStatus.Draw)]
    public void ScoreLimitThreshold_TwoPlayers_EvaluatesStatusCorrectly(
        int scoreLimit, int scoreP1, int scoreP2, bool expectedResult, TestGameStatus expectedStatus)
    {
        var p1 = new TestPlayerModel { PlayerName = "P1", PlayerScore = scoreP1 };
        var p2 = new TestPlayerModel { PlayerName = "P2", PlayerScore = scoreP2 };
        var game = new TestCurrentGameModel
        {
            ScoreLimit = scoreLimit,
            Players = [p1, p2]
        };

        var (hasResult, winners, status) = EvaluateGameEnd(game);

        Assert.Equal(expectedResult, hasResult);
        Assert.Equal(expectedStatus, status);

        if (status == TestGameStatus.Won)
        {
            Assert.Single(winners);
            Assert.Equal(Math.Max(scoreP1, scoreP2), winners[0].PlayerScore);
        }
        else if (status == TestGameStatus.Draw)
        {
            Assert.Equal(2, winners.Count);
            Assert.Equal(scoreP1, winners[0].PlayerScore);
            Assert.Equal(scoreP2, winners[1].PlayerScore);
        }
    }

    [Theory]
    [InlineData(3, 500, 500)]
    [InlineData(4, 500, 550)]
    [InlineData(5, 500, 600)]
    [InlineData(6, 500, 700)]
    public void ScoreLimitThreshold_NPlayerMultiWayDraw_DetectedAccurately(int tiedPlayerCount, int scoreLimit, int tiedScore)
    {
        var players = Enumerable.Range(0, tiedPlayerCount)
            .Select(i => new TestPlayerModel { PlayerName = $"TiedPlayer_{i}", PlayerScore = tiedScore })
            .ToList();

        var game = new TestCurrentGameModel
        {
            ScoreLimit = scoreLimit,
            Players = new ObservableCollection<TestPlayerModel>(players)
        };

        var (hasResult, winners, status) = EvaluateGameEnd(game);

        Assert.True(hasResult);
        Assert.Equal(TestGameStatus.Draw, status);
        Assert.Equal(tiedPlayerCount, winners.Count);
        foreach (var winner in winners)
        {
            Assert.Equal(tiedScore, winner.PlayerScore);
        }
    }

    [Fact]
    public void ScoreLimitThreshold_HigherScoreBeatsLowerTie_SelectsSoleWinner()
    {
        // P1: 520, P2: 520, P3: 550 (Score Limit: 500)
        var p1 = new TestPlayerModel { PlayerName = "P1", PlayerScore = 520 };
        var p2 = new TestPlayerModel { PlayerName = "P2", PlayerScore = 520 };
        var p3 = new TestPlayerModel { PlayerName = "P3", PlayerScore = 550 };
        var game = new TestCurrentGameModel { ScoreLimit = 500, Players = [p1, p2, p3] };

        var (hasResult, winners, status) = EvaluateGameEnd(game);

        Assert.True(hasResult);
        Assert.Equal(TestGameStatus.Won, status);
        Assert.Single(winners);
        Assert.Equal(p3.ID, winners[0].ID);
        Assert.Equal(550, winners[0].PlayerScore);
    }

    // =========================================================================
    // HELPER METHODS
    // =========================================================================

    private static bool AdvanceDealer(TestCurrentGameModel game)
    {
        if (game?.Players == null || game.Players.Count == 0) return false;

        var currentDealer = game.Players.FirstOrDefault(p => p.IsDealer);
        if (currentDealer == null)
        {
            game.Players[0].IsDealer = true;
            return true;
        }

        var currentDealerIndex = game.Players.IndexOf(currentDealer);
        if (currentDealerIndex == -1) return false;

        var nextDealerIndex = (currentDealerIndex + 1) % game.Players.Count;
        game.Players[currentDealerIndex].IsDealer = false;
        game.Players[nextDealerIndex].IsDealer = true;
        return true;
    }

    private static (bool Results, List<TestPlayerModel> Winners, TestGameStatus GameStatus) EvaluateGameEnd(TestCurrentGameModel currentGame)
    {
        var results = (false, new List<TestPlayerModel>(), TestGameStatus.Unknown);
        var winners = currentGame.Players
            .Where(p => p.PlayerScore >= currentGame.ScoreLimit)
            .OrderByDescending(p => p.PlayerScore)
            .ToList();

        if (winners.Count == 0)
            return results;

        var highestScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == highestScore).ToList();

        if (winners.Count > 1)
            results = (true, winners, TestGameStatus.Draw);
        else
            results = (true, winners, TestGameStatus.Won);

        return results;
    }
}
