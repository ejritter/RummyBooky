using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class MockCurrentGameViewModelHarness
{
    public TestCurrentGameModel CurrentGame { get; set; }
    public TestRoundModel CurrentRound { get; set; }
    public int SelectedRoundIndex { get; set; } = 0;
    public bool IsViewingPreviousRound { get; set; } = false;
    public bool IsNotViewingPreviousRound { get; set; } = true;
    public bool CanGoToPreviousRound { get; set; } = false;
    public bool CanGoToNextRound { get; set; } = false;
    public string RoundText { get; set; } = string.Empty;
    public bool DisplayPlayersHighestLowestHands { get; set; } = false;

    private readonly Dictionary<Guid, string> _activeRoundDraftScores = new();
    private bool _isNavigatingRounds = false;

    public MockCurrentGameViewModelHarness(TestCurrentGameModel game)
    {
        CurrentGame = game;
        TestGameRecomputationEngine.RecalculateGame(game);

        SelectedRoundIndex = game.Round.Count > 0 ? game.Round.Count - 1 : 0;
        CurrentRound = game.Round.Count > 0 ? game.Round[SelectedRoundIndex] : new TestRoundModel { GameId = game.GameId };
        UpdateRoundNavigationState();
    }

    public void UpdateRoundNavigationState()
    {
        if (CurrentGame?.Round is null || CurrentGame.Round.Count == 0)
        {
            CanGoToPreviousRound = false;
            CanGoToNextRound = false;
            IsViewingPreviousRound = false;
            IsNotViewingPreviousRound = true;
            RoundText = "Round 0";
            return;
        }

        int activeIndex = CurrentGame.Round.Count - 1;
        IsViewingPreviousRound = SelectedRoundIndex < activeIndex;
        IsNotViewingPreviousRound = !IsViewingPreviousRound;
        CanGoToPreviousRound = SelectedRoundIndex > 0;
        CanGoToNextRound = SelectedRoundIndex < activeIndex;

        if (IsViewingPreviousRound)
        {
            RoundText = $"Round {SelectedRoundIndex + 1} of {CurrentGame.Round.Count} (Editing)";
        }
        else
        {
            RoundText = $"Round {CurrentGame.Round.Count}";
        }
    }

    public bool CanExecuteCalculatePlayerScores()
    {
        if (IsViewingPreviousRound)
            return false;

        if (CurrentGame.Players.Count == 0)
            return false;

        foreach (var player in CurrentGame.Players)
        {
            if (string.IsNullOrWhiteSpace(player.PlayerScoreText))
            {
                return false;
            }
        }
        return true;
    }

    public void PreviousRound()
    {
        if (SelectedRoundIndex > 0 && CurrentGame.Round.Count > 0)
        {
            int activeIndex = CurrentGame.Round.Count - 1;
            if (SelectedRoundIndex == activeIndex)
            {
                _activeRoundDraftScores.Clear();
                foreach (var p in CurrentGame.Players)
                {
                    _activeRoundDraftScores[p.ID] = p.PlayerScoreText;
                }
            }

            SelectedRoundIndex--;
            _isNavigatingRounds = true;
            var targetRound = CurrentGame.Round[SelectedRoundIndex];
            foreach (var p in CurrentGame.Players)
            {
                var rs = targetRound.RoundScores.FirstOrDefault(r => r.PlayerId == p.ID);
                p.PlayerScoreText = rs != null ? rs.Score.ToString() : "0";
            }
            _isNavigatingRounds = false;

            CurrentRound = targetRound;
            UpdateRoundNavigationState();
        }
    }

    public void NextRound()
    {
        int activeIndex = CurrentGame.Round.Count - 1;
        if (SelectedRoundIndex < activeIndex)
        {
            SelectedRoundIndex++;
            _isNavigatingRounds = true;
            if (SelectedRoundIndex == activeIndex)
            {
                foreach (var p in CurrentGame.Players)
                {
                    p.PlayerScoreText = _activeRoundDraftScores.TryGetValue(p.ID, out var draft) ? draft : string.Empty;
                }
            }
            else
            {
                var targetRound = CurrentGame.Round[SelectedRoundIndex];
                foreach (var p in CurrentGame.Players)
                {
                    var rs = targetRound.RoundScores.FirstOrDefault(r => r.PlayerId == p.ID);
                    p.PlayerScoreText = rs != null ? rs.Score.ToString() : "0";
                }
            }
            _isNavigatingRounds = false;

            CurrentRound = CurrentGame.Round[SelectedRoundIndex];
            UpdateRoundNavigationState();
        }
    }

    public void ReturnToActiveRound()
    {
        if (CurrentGame?.Round is null || CurrentGame.Round.Count == 0)
            return;

        int activeIndex = CurrentGame.Round.Count - 1;
        SelectedRoundIndex = activeIndex;
        _isNavigatingRounds = true;
        foreach (var p in CurrentGame.Players)
        {
            p.PlayerScoreText = _activeRoundDraftScores.TryGetValue(p.ID, out var draft) ? draft : string.Empty;
        }
        _isNavigatingRounds = false;

        CurrentRound = CurrentGame.Round[activeIndex];
        UpdateRoundNavigationState();
    }

    public void EditScoreInCurrentView(TestPlayerModel player, string newScoreText)
    {
        player.PlayerScoreText = newScoreText;
        if (_isNavigatingRounds)
            return;

        if (IsViewingPreviousRound)
        {
            if (int.TryParse(newScoreText, out int newScore))
            {
                if (SelectedRoundIndex >= 0 && SelectedRoundIndex < CurrentGame.Round.Count)
                {
                    var round = CurrentGame.Round[SelectedRoundIndex];
                    var rs = round.RoundScores.FirstOrDefault(r => r.PlayerId == player.ID);
                    if (rs is null)
                    {
                        rs = new TestRoundScoreModel { PlayerId = player.ID, Score = newScore };
                        round.RoundScores.Add(rs);
                    }
                    else
                    {
                        rs.Score = newScore;
                    }

                    TestGameRecomputationEngine.RecalculateGame(CurrentGame);
                }
            }
        }
    }

    public string GetDraftScore(Guid playerId)
    {
        return _activeRoundDraftScores.TryGetValue(playerId, out var draft) ? draft : string.Empty;
    }
}

public class EmpiricalR1AdversarialStressTests
{
    // =========================================================================
    // SECTION 1: ROUND NAVIGATION & DRAFT CACHING ADVERSARIAL CHALLENGES
    // =========================================================================

    [Fact]
    public void Challenge_FullNavigationCycle_MaintainsStateAndPreservesActiveDrafts()
    {
        // Arrange: 3 players, 4 completed rounds, 1 active unscored round (total 5 rounds)
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 10 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 30 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 40 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 60 });

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 70 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 80 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 90 });

        var r4 = new TestRoundModel();
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 100 });
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 110 });
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 120 });

        var r5 = new TestRoundModel(); // Active round (unscored)

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1, r2, r3, r4, r5]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // Initial State at Round 5
        Assert.Equal(4, vm.SelectedRoundIndex);
        Assert.False(vm.IsViewingPreviousRound);
        Assert.True(vm.IsNotViewingPreviousRound);
        Assert.True(vm.CanGoToPreviousRound);
        Assert.False(vm.CanGoToNextRound);
        Assert.Equal("Round 5", vm.RoundText);

        // User enters draft scores on active round 5
        p1.PlayerScoreText = "150";
        p2.PlayerScoreText = "250";
        p3.PlayerScoreText = "350";
        Assert.True(vm.CanExecuteCalculatePlayerScores());

        // Act: Navigate back one by one to Round 1
        vm.PreviousRound(); // To Round 4 (index 3)
        Assert.Equal(3, vm.SelectedRoundIndex);
        Assert.True(vm.IsViewingPreviousRound);
        Assert.Equal("Round 4 of 5 (Editing)", vm.RoundText);
        Assert.True(vm.CanGoToPreviousRound);
        Assert.True(vm.CanGoToNextRound);
        Assert.Equal("100", p1.PlayerScoreText);
        Assert.Equal("110", p2.PlayerScoreText);
        Assert.Equal("120", p3.PlayerScoreText);
        Assert.False(vm.CanExecuteCalculatePlayerScores()); // Invariant: blocked while editing

        vm.PreviousRound(); // To Round 3 (index 2)
        Assert.Equal(2, vm.SelectedRoundIndex);
        Assert.Equal("Round 3 of 5 (Editing)", vm.RoundText);
        Assert.Equal("70", p1.PlayerScoreText);

        vm.PreviousRound(); // To Round 2 (index 1)
        Assert.Equal(1, vm.SelectedRoundIndex);
        Assert.Equal("Round 2 of 5 (Editing)", vm.RoundText);
        Assert.Equal("40", p1.PlayerScoreText);

        vm.PreviousRound(); // To Round 1 (index 0)
        Assert.Equal(0, vm.SelectedRoundIndex);
        Assert.Equal("Round 1 of 5 (Editing)", vm.RoundText);
        Assert.False(vm.CanGoToPreviousRound); // Cannot go back further
        Assert.True(vm.CanGoToNextRound);
        Assert.Equal("10", p1.PlayerScoreText);
        Assert.Equal("20", p2.PlayerScoreText);
        Assert.Equal("30", p3.PlayerScoreText);

        // Try navigating past 0
        vm.PreviousRound();
        Assert.Equal(0, vm.SelectedRoundIndex);

        // Act: Navigate forward one by one back to Round 5
        vm.NextRound(); // To Round 2 (index 1)
        Assert.Equal(1, vm.SelectedRoundIndex);
        Assert.Equal("40", p1.PlayerScoreText);

        vm.NextRound(); // To Round 3 (index 2)
        Assert.Equal(2, vm.SelectedRoundIndex);
        Assert.Equal("70", p1.PlayerScoreText);

        vm.NextRound(); // To Round 4 (index 3)
        Assert.Equal(3, vm.SelectedRoundIndex);
        Assert.Equal("100", p1.PlayerScoreText);

        vm.NextRound(); // To Round 5 (index 4 - Active)
        Assert.Equal(4, vm.SelectedRoundIndex);
        Assert.False(vm.IsViewingPreviousRound);
        Assert.True(vm.IsNotViewingPreviousRound);
        Assert.True(vm.CanGoToPreviousRound);
        Assert.False(vm.CanGoToNextRound);
        Assert.Equal("Round 5", vm.RoundText);

        // Assert: Drafts completely restored!
        Assert.Equal("150", p1.PlayerScoreText);
        Assert.Equal("250", p2.PlayerScoreText);
        Assert.Equal("350", p3.PlayerScoreText);
        Assert.True(vm.CanExecuteCalculatePlayerScores());
    }

    [Fact]
    public void Challenge_ReturnToActiveRound_BypassesIntermediateRoundsDirectly()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 30 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 30 });

        var r3 = new TestRoundModel(); // Active round

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        p1.PlayerScoreText = "99";
        p2.PlayerScoreText = "88";

        // Go to Round 1
        vm.PreviousRound(); // R2
        vm.PreviousRound(); // R1
        Assert.Equal(0, vm.SelectedRoundIndex);
        Assert.Equal("50", p1.PlayerScoreText);

        // Act: Click "Return To Active Round" directly
        vm.ReturnToActiveRound();

        // Assert
        Assert.Equal(2, vm.SelectedRoundIndex);
        Assert.False(vm.IsViewingPreviousRound);
        Assert.Equal("Round 3", vm.RoundText);
        Assert.Equal("99", p1.PlayerScoreText);
        Assert.Equal("88", p2.PlayerScoreText);
    }

    // =========================================================================
    // SECTION 2: LIVE SCORE EDITING & REAL-TIME RECOMPUTATION
    // =========================================================================

    [Fact]
    public void Challenge_EditPreviousRound_LiveRecomputesTotalsExtremesAndLeaders()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 60 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 40 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 30 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 70 });

        var r3 = new TestRoundModel(); // Active round

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1, r2, r3]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // Initial standings:
        // Alice: 50+30 = 80
        // Bob: 60+20 = 80
        // Charlie: 40+70 = 110 (Leader)
        Assert.Equal(80, p1.PlayerScore);
        Assert.Equal(80, p2.PlayerScore);
        Assert.Equal(110, p3.PlayerScore);
        Assert.Equal(p3.ID, r2.LeadingPlayer?.ID);
        Assert.Equal(p3.ID, r3.LeadingPlayer?.ID);

        // Act: Navigate to Round 1 and edit Alice's score from 50 -> 120 (+70 points)
        vm.PreviousRound(); // to R2
        vm.PreviousRound(); // to R1
        Assert.Equal(0, vm.SelectedRoundIndex);

        vm.EditScoreInCurrentView(p1, "120");

        // Assert: Live recomputation triggered immediately
        // Alice: 120 + 30 = 150 (New Leader!)
        // Bob: 60 + 20 = 80
        // Charlie: 40 + 70 = 110
        Assert.Equal(150, p1.PlayerScore);
        Assert.Equal(80, p2.PlayerScore);
        Assert.Equal(110, p3.PlayerScore);

        // Alice extremes updated: Highest hand is 120 (R1), Lowest is 30 (R2)
        Assert.Equal(120, p1.HighestScoredHand);
        Assert.Equal(30, p1.LowestScoredHand);

        // Round 1 metrics updated
        Assert.Equal(120, r1.CurrentHighestScoredHandValue);
        Assert.Equal(p1.ID, r1.PlayerHighestScoringHand?.ID);
        Assert.Equal(p1.ID, r1.LeadingPlayer?.ID);

        // Downstream Round 2 and Round 3 leaders updated to Alice
        Assert.Equal(p1.ID, r2.LeadingPlayer?.ID);
        Assert.Equal(p1.ID, r3.LeadingPlayer?.ID);
    }

    [Fact]
    public void Challenge_EditScoreToReduceMaxHand_CorrectlyDropsHighestScoredHandToSecondHighest()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 200 }); // Outlier high score
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 30 });

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 80 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 });

        var r4 = new TestRoundModel(); // Active

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3, r4]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        Assert.Equal(200, p1.HighestScoredHand);

        // Act: Navigate to Round 2 and correct typo: 200 -> 20
        vm.PreviousRound(); // R3
        vm.PreviousRound(); // R2
        Assert.Equal(1, vm.SelectedRoundIndex);

        vm.EditScoreInCurrentView(p1, "20");

        // Assert: Highest scored hand must recalculate from scratch and drop to 80 (from R3)
        Assert.Equal(80, p1.HighestScoredHand);
        Assert.Equal(20, p1.LowestScoredHand);
        Assert.Equal(150, p1.PlayerScore); // 50 + 20 + 80
    }

    // =========================================================================
    // SECTION 3: RAPID ROUND SWITCHING & STRESS ITERATION
    // =========================================================================

    [Fact]
    public void Challenge_RapidRoundSwitching_1000Iterations_ZeroDraftCorruptionOrLeakage()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 15 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 25 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 35 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 45 });

        var r3 = new TestRoundModel(); // Active

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        // Set draft
        p1.PlayerScoreText = "777";
        p2.PlayerScoreText = "888";

        // Stress: 1000 rapid back-and-forth transitions
        for (int i = 0; i < 1000; i++)
        {
            vm.PreviousRound(); // R2
            vm.PreviousRound(); // R1
            vm.NextRound();     // R2
            vm.NextRound();     // R3 (Active)
        }

        // Assert
        Assert.Equal(2, vm.SelectedRoundIndex);
        Assert.False(vm.IsViewingPreviousRound);
        Assert.Equal("777", p1.PlayerScoreText);
        Assert.Equal("888", p2.PlayerScoreText);

        // Check Round 1 & Round 2 models were not modified by navigation
        Assert.Equal(15, r1.RoundScores.First(s => s.PlayerId == p1.ID).Score);
        Assert.Equal(25, r1.RoundScores.First(s => s.PlayerId == p2.ID).Score);
        Assert.Equal(35, r2.RoundScores.First(s => s.PlayerId == p1.ID).Score);
        Assert.Equal(45, r2.RoundScores.First(s => s.PlayerId == p2.ID).Score);
    }

    // =========================================================================
    // SECTION 4: NEGATIVE, ZERO, AND EXTREME INTEGER SCORES
    // =========================================================================

    [Fact]
    public void Challenge_AllNegativeScores_AccuratelyComputesExtremesAndAlgebraicLeader()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = -100 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = -50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = -20 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = -40 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = -80 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = -10 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1, r2]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // Alice: -100 + (-40) = -140
        // Bob: -50 + (-80) = -130
        // Charlie: -20 + (-10) = -30 (Highest algebraic score -> Leader!)
        Assert.Equal(-140, p1.PlayerScore);
        Assert.Equal(-130, p2.PlayerScore);
        Assert.Equal(-30, p3.PlayerScore);
        Assert.Equal(p3.ID, r2.LeadingPlayer?.ID);

        // Extremes for Alice: High = -40 (closer to 0), Low = -100
        Assert.Equal(-40, p1.HighestScoredHand);
        Assert.Equal(-100, p1.LowestScoredHand);

        // Extremes for Charlie: High = -10, Low = -20
        Assert.Equal(-10, p3.HighestScoredHand);
        Assert.Equal(-20, p3.LowestScoredHand);

        // Round 1 extremes
        Assert.Equal(-20, r1.CurrentHighestScoredHandValue);
        Assert.Equal(p3.ID, r1.PlayerHighestScoringHand?.ID);
        Assert.Equal(-100, r1.CurrentLowestScoredHandValue);
        Assert.Equal(p1.ID, r1.PlayerLowestScoringHand?.ID);
    }

    [Fact]
    public void Challenge_LargeIntegerScores_HandlesMillionsWithoutOverflow()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 1_000_000 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 500_000 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 2_000_000 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 1_500_000 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(3_000_000, p1.PlayerScore);
        Assert.Equal(2_000_000, p2.PlayerScore);
        Assert.Equal(2_000_000, p1.HighestScoredHand);
        Assert.Equal(1_000_000, p1.LowestScoredHand);
    }

    // =========================================================================
    // SECTION 5: SINGLE-ROUND (ROUND 1) GAME BOUNDARY
    // =========================================================================

    [Fact]
    public void Challenge_SingleRoundGame_NavigationIsDisabled()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel(); // Active round 1

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1]
        };

        var vm = new MockCurrentGameViewModelHarness(game);

        Assert.Equal(0, vm.SelectedRoundIndex);
        Assert.False(vm.CanGoToPreviousRound);
        Assert.False(vm.CanGoToNextRound);
        Assert.False(vm.IsViewingPreviousRound);
        Assert.True(vm.IsNotViewingPreviousRound);
        Assert.Equal("Round 1", vm.RoundText);

        // PreviousRound is no-op
        vm.PreviousRound();
        Assert.Equal(0, vm.SelectedRoundIndex);
        Assert.False(vm.IsViewingPreviousRound);
    }

    // =========================================================================
    // SECTION 6: HIGH ROUND COUNTS (50 ROUNDS) & COMPLEXITY STRESS
    // =========================================================================

    [Fact]
    public void Challenge_FiftyRoundGame_EarlyRoundEditCascadesAccuratelyAndMeetsPerformanceBudget()
    {
        var players = Enumerable.Range(1, 6)
            .Select(i => new TestPlayerModel { PlayerName = $"Player_{i}" })
            .ToList();

        var rounds = new List<TestRoundModel>();
        for (int r = 1; r <= 50; r++)
        {
            var round = new TestRoundModel();
            for (int p = 0; p < 6; p++)
            {
                round.RoundScores.Add(new TestRoundScoreModel
                {
                    PlayerId = players[p].ID,
                    Score = (p + 1) * 10 // P1: 10, P2: 20, ..., P6: 60 per round
                });
            }
            rounds.Add(round);
        }

        var game = new TestCurrentGameModel
        {
            Players = new ObservableCollection<TestPlayerModel>(players),
            Round = new ObservableCollection<TestRoundModel>(rounds)
        };

        var sw = Stopwatch.StartNew();
        TestGameRecomputationEngine.RecalculateGame(game);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 50, $"FiftyRoundRecalculation took {sw.ElapsedMilliseconds}ms (< 50ms budget)");

        // Initial: P6 leads every round (60 * 50 = 3000)
        Assert.Equal(3000, players[5].PlayerScore);
        Assert.Equal(players[5].ID, rounds[49].LeadingPlayer?.ID);

        // Act: Give Player 1 a massive score in Round 1 (10 -> 5000)
        rounds[0].RoundScores.First(s => s.PlayerId == players[0].ID).Score = 5000;

        sw.Restart();
        TestGameRecomputationEngine.RecalculateGame(game);
        sw.Stop();

        Assert.True(sw.ElapsedMilliseconds < 50, $"Re-evaluating fifty rounds took {sw.ElapsedMilliseconds}ms");

        // Player 1 total: 5000 + (49 * 10) = 5490 (Leads all 50 rounds!)
        Assert.Equal(5490, players[0].PlayerScore);
        for (int r = 0; r < 50; r++)
        {
            Assert.Equal(players[0].ID, rounds[r].LeadingPlayer?.ID);
        }
    }

    // =========================================================================
    // SECTION 7: LEADING PLAYER TIES & REAL-TIME RE-EVALUATIONS
    // =========================================================================

    [Fact]
    public void Challenge_LeaderTiesAndMidGameFlips_TracksLeaderAtEveryRoundAccurately()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 }); // Tie after R1: 50 vs 50

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 30 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 }); // Bob leads R2: 90 vs 80

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 }); // Tie after R3: 130 vs 130

        var r4 = new TestRoundModel();
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 60 });
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 }); // Alice leads R4: 190 vs 150

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3, r4]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(p2.ID, r2.LeadingPlayer?.ID); // Bob led R2
        Assert.Equal(p1.ID, r4.LeadingPlayer?.ID); // Alice led R4

        // Act: Edit Round 2 Bob score from 40 -> 20 (R2 total becomes 70 for Bob vs 80 for Alice)
        r2.RoundScores.First(s => s.PlayerId == p2.ID).Score = 20;

        TestGameRecomputationEngine.RecalculateGame(game);

        // Now Alice leads R2 (80 vs 70), R3 (130 vs 110), and R4 (190 vs 130)
        Assert.Equal(p1.ID, r2.LeadingPlayer?.ID);
        Assert.Equal(p1.ID, r3.LeadingPlayer?.ID);
        Assert.Equal(p1.ID, r4.LeadingPlayer?.ID);
    }

    // =========================================================================
    // SECTION 8: POLYMORPHIC SERIALIZATION INTEGRITY AFTER EDITS
    // =========================================================================

    [Fact]
    public void Challenge_EditedGame_SerializesAndDeserializesWithCompleteFidelity()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 125 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 75 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 200 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 150 });

        TestGameModel game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2],
            ScoreLimit = 500
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // Act: Serialize to JSON
        var json = JsonSerializer.Serialize(game);

        // Act: Deserialize from JSON
        var deserialized = JsonSerializer.Deserialize<TestGameModel>(json);

        Assert.NotNull(deserialized);
        Assert.IsType<TestCurrentGameModel>(deserialized);
        var current = (TestCurrentGameModel)deserialized;

        Assert.Equal(2, current.Round.Count);
        Assert.Equal(2, current.Players.Count);
        Assert.Equal(325, current.Players.First(p => p.ID == p1.ID).PlayerScore);
        Assert.Equal(225, current.Players.First(p => p.ID == p2.ID).PlayerScore);

        // Verify RoundScores collection preserved in JSON
        Assert.Equal(2, current.Round[0].RoundScores.Count);
        Assert.Equal(125, current.Round[0].RoundScores.First(r => r.PlayerId == p1.ID).Score);
        Assert.Equal(75, current.Round[0].RoundScores.First(r => r.PlayerId == p2.ID).Score);
    }
}
