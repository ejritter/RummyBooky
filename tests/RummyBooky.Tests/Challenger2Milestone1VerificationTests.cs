using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace RummyBooky.Tests;

/// <summary>
/// Challenger 2 Empirical Stress and Adversarial Verification Suite for Milestone 1.
/// Covers:
/// 1. Previous round score modification & real-time metric recalculation (totals, extremes, round leaders).
/// 2. Active round draft preservation across arbitrary navigation sequences.
/// 3. EditGamePage state machine, tie resolutions, status conversions, and global lifetime stats synchronization.
/// 4. Property-based randomized stress test oracle comparing incremental recalculation with ground-truth summation.
/// </summary>
public class Challenger2Milestone1VerificationTests
{
    // =========================================================================
    // SECTION 1: PREVIOUS ROUND EDITING & DYNAMIC RECALCULATION
    // =========================================================================

    [Fact]
    public void PreviousRoundEditing_ModifyingRound1Scores_UpdatesAllDownstreamRoundLeadersAndExtremes()
    {
        // Arrange: 4 players across 3 completed rounds + 1 active unscored round
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };
        var p4 = new TestPlayerModel { PlayerName = "Diana" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 80 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 40 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p4.ID, Score = 20 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 30 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 90 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p4.ID, Score = 60 });

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 70 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 10 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p4.ID, Score = 50 });

        var r4 = new TestRoundModel(); // Active round template (unscored)

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3, p4],
            Round = [r1, r2, r3, r4]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // Initial check:
        // Alice: 50+30+70 = 150
        // Bob: 80+40+20 = 140
        // Charlie: 40+90+10 = 140
        // Diana: 20+60+50 = 130
        // Alice leads after R3 and on active R4
        Assert.Equal(150, p1.PlayerScore);
        Assert.Equal(140, p2.PlayerScore);
        Assert.Equal(140, p3.PlayerScore);
        Assert.Equal(130, p4.PlayerScore);
        Assert.Equal(p1.ID, r3.LeadingPlayer?.ID);
        Assert.Equal(p1.ID, r4.LeadingPlayer?.ID);

        // Act: Navigate to Round 1 and edit Diana's score from 20 -> 120 (+100 surge)
        vm.PreviousRound(); // to R3
        vm.PreviousRound(); // to R2
        vm.PreviousRound(); // to R1
        Assert.Equal(0, vm.SelectedRoundIndex);

        vm.EditScoreInCurrentView(p4, "120");

        // Assert: Dynamic recalculation
        // Diana: 120+60+50 = 230 (Now leading everyone!)
        Assert.Equal(150, p1.PlayerScore);
        Assert.Equal(140, p2.PlayerScore);
        Assert.Equal(140, p3.PlayerScore);
        Assert.Equal(230, p4.PlayerScore);

        // Round 1 metrics
        Assert.Equal(120, r1.CurrentHighestScoredHandValue);
        Assert.Equal(p4.ID, r1.PlayerHighestScoringHand?.ID);
        Assert.Equal(p4.ID, r1.LeadingPlayer?.ID);

        // Diana extremes
        Assert.Equal(120, p4.HighestScoredHand);
        Assert.Equal(50, p4.LowestScoredHand);

        // Downstream leaders update
        Assert.Equal(p4.ID, r2.LeadingPlayer?.ID); // After R2: Diana has 120+60 = 180 > Charlie 130, Bob 120, Alice 80
        Assert.Equal(p4.ID, r3.LeadingPlayer?.ID); // After R3: Diana has 230
        Assert.Equal(p4.ID, r4.LeadingPlayer?.ID); // Active round inherits Diana
    }

    [Fact]
    public void PreviousRoundEditing_MultiplePlayersModifiedInDifferentRounds_MaintainsFullConsistency()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 100 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 40 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 80 });

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 60 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 70 });

        var r4 = new TestRoundModel(); // Active round

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3, r4]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // Alice: 100 + 40 + 60 = 200
        // Bob: 50 + 80 + 70 = 200
        Assert.Equal(200, p1.PlayerScore);
        Assert.Equal(200, p2.PlayerScore);

        // Edit Round 1: Alice 100 -> 30 (-70)
        vm.PreviousRound(); // R3
        vm.PreviousRound(); // R2
        vm.PreviousRound(); // R1
        vm.EditScoreInCurrentView(p1, "30");

        // Edit Round 2: Bob 80 -> 20 (-60)
        vm.NextRound(); // R2
        vm.EditScoreInCurrentView(p2, "20");

        // Edit Round 3: Alice 60 -> 110 (+50)
        vm.NextRound(); // R3
        vm.EditScoreInCurrentView(p1, "110");

        // Totals check:
        // Alice: 30 + 40 + 110 = 180
        // Bob: 50 + 20 + 70 = 140
        Assert.Equal(180, p1.PlayerScore);
        Assert.Equal(140, p2.PlayerScore);
        Assert.Equal(110, p1.HighestScoredHand);
        Assert.Equal(30, p1.LowestScoredHand);
        Assert.Equal(70, p2.HighestScoredHand);
        Assert.Equal(20, p2.LowestScoredHand);
        Assert.Equal(p1.ID, r3.LeadingPlayer?.ID);
    }

    // =========================================================================
    // SECTION 2: DRAFT SCORE PRESERVATION ACROSS NAVIGATION
    // =========================================================================

    [Fact]
    public void DraftPreservation_DeepNavigationAndEdits_DraftScoresOnActiveRoundRemainIntact()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 60 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 70 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 40 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 60 });

        var r3 = new TestRoundModel(); // Active round

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1, r2, r3]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // User enters draft scores on active round 3
        p1.PlayerScoreText = "123";
        p2.PlayerScoreText = "456";
        p3.PlayerScoreText = "789";

        // Navigate to Round 1 and edit scores
        vm.PreviousRound(); // R2
        vm.PreviousRound(); // R1
        Assert.Equal("50", p1.PlayerScoreText);
        Assert.Equal("60", p2.PlayerScoreText);
        Assert.Equal("70", p3.PlayerScoreText);

        vm.EditScoreInCurrentView(p1, "80");
        Assert.Equal("80", p1.PlayerScoreText);

        // Return to active round directly
        vm.ReturnToActiveRound();

        Assert.Equal(2, vm.SelectedRoundIndex);
        Assert.False(vm.IsViewingPreviousRound);
        Assert.Equal("123", p1.PlayerScoreText);
        Assert.Equal("456", p2.PlayerScoreText);
        Assert.Equal("789", p3.PlayerScoreText);
        Assert.True(vm.CanExecuteCalculatePlayerScores());
    }

    [Fact]
    public void DraftPreservation_EmptyOrPartialDrafts_RestoresAccurately()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });

        var r2 = new TestRoundModel(); // Active round

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // Only player 1 has entered a draft
        p1.PlayerScoreText = "95";
        p2.PlayerScoreText = string.Empty;

        Assert.False(vm.CanExecuteCalculatePlayerScores());

        vm.PreviousRound();
        Assert.Equal("50", p1.PlayerScoreText);
        Assert.Equal("50", p2.PlayerScoreText);

        vm.NextRound();
        Assert.Equal("95", p1.PlayerScoreText);
        Assert.Equal(string.Empty, p2.PlayerScoreText);
        Assert.False(vm.CanExecuteCalculatePlayerScores());
    }

    // =========================================================================
    // SECTION 3: EDITGAMEPAGE STATUS SWITCHING & TIE RESOLUTIONS
    // =========================================================================

    [Fact]
    public void EditGame_FourWayStatusCycle_PreservesAndRecomputesCorrectly()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 520 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 480 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500
        };

        var vm = new MockEditGameViewModelState(game);

        // Status: Won
        vm.ChangeStatus("Won");
        Assert.True(vm.IsWinnerPickerVisible);
        Assert.Equal(p1.ID, vm.SelectedWinner?.ID);
        var won = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Won, won.GameState);
        Assert.Equal(p1.ID, won.WinningPlayer?.ID);

        // Status: Draw
        vm.ChangeStatus("Draw");
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
        var draw = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Draw, draw.GameState);
        Assert.Null(draw.WinningPlayer);

        // Status: Forfeit
        vm.ChangeStatus("Forfeit");
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
        var forfeit = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Forfeit, forfeit.GameState);
        Assert.Null(forfeit.WinningPlayer);

        // Status: In-Progress
        vm.ChangeStatus("In-Progress");
        Assert.False(vm.IsWinnerPickerVisible);
        var current = (TestCurrentGameModel)vm.CreateFinalGameToSave();
        Assert.True(current.IsGameActive);
        Assert.False(current.IsGameFinished);
    }

    [Fact]
    public void EditGame_TieResolution_ManualOverrideAndStatsSync()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 550 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 550 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 500 });

        var initialPlayed = new TestPlayedGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Draw,
            WinningPlayer = null
        };

        TestGameRecomputationEngine.RecalculateGame(initialPlayed);
        var statsDraw = TestGameRecomputationEngine.AggregatePlayerStats([initialPlayed]);
        Assert.Equal(1, statsDraw[p1.ID].GameDraws);
        Assert.Equal(1, statsDraw[p2.ID].GameDraws);
        Assert.Equal(1, statsDraw[p3.ID].GameDraws);

        // User resolves tie in favor of Bob
        var vm = new MockEditGameViewModelState(initialPlayed);
        vm.ChangeStatus("Won");
        vm.SelectedWinner = p2;
        var resolved = (TestPlayedGameModel)vm.CreateFinalGameToSave();

        var statsWon = TestGameRecomputationEngine.AggregatePlayerStats([resolved]);
        Assert.Equal(1, statsWon[p2.ID].GamesWon);
        Assert.Equal(0, statsWon[p2.ID].GamesLost);
        Assert.Equal(0, statsWon[p1.ID].GamesWon);
        Assert.Equal(1, statsWon[p1.ID].GamesLost);
        Assert.Equal(0, statsWon[p3.ID].GamesWon);
        Assert.Equal(1, statsWon[p3.ID].GamesLost);
    }

    // =========================================================================
    // SECTION 4: PROPERTY-BASED RANDOMIZED STRESS TEST GENERATOR
    // =========================================================================

    [Fact]
    public void PropertyBased_100RandomizedGameSimulations_RecalculationMatchesExactSums()
    {
        var rng = new Random(42);

        for (int gameIdx = 0; gameIdx < 100; gameIdx++)
        {
            int playerCount = rng.Next(2, 7); // 2 to 6 players
            int roundCount = rng.Next(1, 15); // 1 to 14 rounds

            var players = Enumerable.Range(1, playerCount)
                .Select(i => new TestPlayerModel { PlayerName = $"P{i}_{Guid.NewGuid():N}" })
                .ToList();

            var rounds = new List<TestRoundModel>();
            var groundTruthTotals = players.ToDictionary(p => p.ID, p => 0);
            var groundTruthHighest = players.ToDictionary(p => p.ID, p => int.MinValue);
            var groundTruthLowest = players.ToDictionary(p => p.ID, p => int.MaxValue);

            for (int r = 0; r < roundCount; r++)
            {
                var round = new TestRoundModel();
                foreach (var p in players)
                {
                    int score = rng.Next(-50, 200); // Random score
                    round.RoundScores.Add(new TestRoundScoreModel { PlayerId = p.ID, Score = score });

                    groundTruthTotals[p.ID] += score;
                    if (score > groundTruthHighest[p.ID]) groundTruthHighest[p.ID] = score;
                    if (score < groundTruthLowest[p.ID]) groundTruthLowest[p.ID] = score;
                }
                rounds.Add(round);
            }

            var game = new TestCurrentGameModel
            {
                Players = new ObservableCollection<TestPlayerModel>(players),
                Round = new ObservableCollection<TestRoundModel>(rounds)
            };

            // Run recomputation engine
            TestGameRecomputationEngine.RecalculateGame(game);

            // Oracle verification
            foreach (var p in players)
            {
                Assert.Equal(groundTruthTotals[p.ID], p.PlayerScore);
                Assert.Equal(groundTruthHighest[p.ID], p.HighestScoredHand);
                Assert.Equal(groundTruthLowest[p.ID], p.LowestScoredHand);
            }

            // Perform a random edit on a random round
            int editRound = rng.Next(0, roundCount);
            int editPlayer = rng.Next(0, playerCount);
            int newScore = rng.Next(-50, 200);

            var targetPlayer = players[editPlayer];
            var targetScoreModel = rounds[editRound].RoundScores.First(s => s.PlayerId == targetPlayer.ID);

            // Update ground truth
            groundTruthTotals[targetPlayer.ID] -= targetScoreModel.Score;
            groundTruthTotals[targetPlayer.ID] += newScore;
            targetScoreModel.Score = newScore;

            // Recalculate true extremes for edited player
            groundTruthHighest[targetPlayer.ID] = rounds.Max(r => r.RoundScores.First(s => s.PlayerId == targetPlayer.ID).Score);
            groundTruthLowest[targetPlayer.ID] = rounds.Min(r => r.RoundScores.First(s => s.PlayerId == targetPlayer.ID).Score);

            // Re-run recomputation engine
            TestGameRecomputationEngine.RecalculateGame(game);

            // Re-verify oracle
            foreach (var p in players)
            {
                Assert.Equal(groundTruthTotals[p.ID], p.PlayerScore);
                Assert.Equal(groundTruthHighest[p.ID], p.HighestScoredHand);
                Assert.Equal(groundTruthLowest[p.ID], p.LowestScoredHand);
            }
        }
    }
}
