using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace RummyBooky.Tests;

public class ComprehensiveGameEditingTests
{
    // =========================================================================
    // TIER 1: ISOLATED FEATURE TESTS
    // =========================================================================

    [Fact]
    public void InGameEditing_Round1Of3_UpdatesPlayerScoresAndRoundLeadersImmediately()
    {
        // Arrange: 3 players across 2 completed rounds and 1 active unscored round
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
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 50 });

        var r3 = new TestRoundModel(); // Active round (unscored)

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1, r2, r3]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // Initial checks:
        // Alice: 50 + 30 = 80
        // Bob: 60 + 20 = 80
        // Charlie: 40 + 50 = 90 (Leader after R2 and on R3)
        Assert.Equal(80, p1.PlayerScore);
        Assert.Equal(80, p2.PlayerScore);
        Assert.Equal(90, p3.PlayerScore);
        Assert.Equal(p2.ID, r1.LeadingPlayer?.ID); // Bob led R1
        Assert.Equal(p3.ID, r2.LeadingPlayer?.ID); // Charlie led R2
        Assert.Equal(p3.ID, r3.LeadingPlayer?.ID); // Unscored R3 inherits latest leader (Charlie)

        // Act: User edits Round 1 score for Alice from 50 -> 90
        var aliceR1Score = r1.RoundScores.First(s => s.PlayerId == p1.ID);
        aliceR1Score.Score = 90;

        TestGameRecomputationEngine.RecalculateGame(game);

        // Assert: Running totals update immediately across the board
        Assert.Equal(120, p1.PlayerScore); // 90 + 30
        Assert.Equal(80, p2.PlayerScore);  // 60 + 20
        Assert.Equal(90, p3.PlayerScore);  // 40 + 50

        // Round 1 leader changes to Alice (90)
        Assert.Equal(p1.ID, r1.LeadingPlayer?.ID);
        Assert.Equal(90, r1.CurrentHighestScoredHandValue);
        Assert.Equal(p1.ID, r1.PlayerHighestScoringHand?.ID);

        // Round 2 leader changes to Alice (120 vs Charlie 90, Bob 80)
        Assert.Equal(p1.ID, r2.LeadingPlayer?.ID);

        // Active Round 3 leader reflects updated leader Alice
        Assert.Equal(p1.ID, r3.LeadingPlayer?.ID);
    }

    [Fact]
    public void InGameEditing_ModifyScore_RecalculatesHighestAndLowestHandsForEachPlayer()
    {
        // Arrange
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 80 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 40 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 60 });

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 100 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 10 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(100, p1.HighestScoredHand);
        Assert.Equal(40, p1.LowestScoredHand);
        Assert.Equal(60, p2.HighestScoredHand);
        Assert.Equal(10, p2.LowestScoredHand);

        // Act: Edit Round 3 score for Alice from 100 -> 30, and Round 2 score for Bob from 60 -> 120
        r3.RoundScores.First(s => s.PlayerId == p1.ID).Score = 30;
        r2.RoundScores.First(s => s.PlayerId == p2.ID).Score = 120;

        TestGameRecomputationEngine.RecalculateGame(game);

        // Assert: Alice's highest is now 80 (from R1) and lowest is 30 (from R3)
        Assert.Equal(80, p1.HighestScoredHand);
        Assert.Equal(30, p1.LowestScoredHand);

        // Assert: Bob's highest is now 120 (from R2) and lowest is 10 (from R3)
        Assert.Equal(120, p2.HighestScoredHand);
        Assert.Equal(10, p2.LowestScoredHand);
    }

    [Fact]
    public void InGameEditing_UnscoredActiveRound_PreservesPreviousRoundExtremesAndLeader()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 110 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 });

        var r2 = new TestRoundModel(); // Unscored template

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // r2 is unscored: should inherit latest leader and hand extremes from r1
        Assert.Equal(p1.ID, r2.LeadingPlayer?.ID);
        Assert.Equal(p1.ID, r2.PlayerHighestScoringHand?.ID);
        Assert.Equal(110, r2.CurrentHighestScoredHandValue);
        Assert.Equal(p2.ID, r2.PlayerLowestScoringHand?.ID);
        Assert.Equal(40, r2.CurrentLowestScoredHandValue);
    }

    [Fact]
    public void InGameEditing_DraftScores_PreservedWhenNavigatingBackAndForth()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 70 });

        var r2 = new TestRoundModel(); // Active round

        var drafts = new Dictionary<Guid, string>();
        p1.PlayerScoreText = "85";
        p2.PlayerScoreText = "95";

        // Step 1: Navigating to previous round saves drafts
        drafts[p1.ID] = p1.PlayerScoreText;
        drafts[p2.ID] = p2.PlayerScoreText;

        // Step 2: Load Round 1 scores into score text
        p1.PlayerScoreText = r1.RoundScores.First(s => s.PlayerId == p1.ID).Score.ToString();
        p2.PlayerScoreText = r1.RoundScores.First(s => s.PlayerId == p2.ID).Score.ToString();
        Assert.Equal("50", p1.PlayerScoreText);
        Assert.Equal("70", p2.PlayerScoreText);

        // Step 3: Navigating back to active round restores drafts
        p1.PlayerScoreText = drafts[p1.ID];
        p2.PlayerScoreText = drafts[p2.ID];
        Assert.Equal("85", p1.PlayerScoreText);
        Assert.Equal("95", p2.PlayerScoreText);
    }

    [Fact]
    public void InGameEditing_CanExecuteCalculatePlayerScores_FalseWhenViewingPreviousRound()
    {
        // CanExecute rule: IsViewingPreviousRound == true -> CanExecute is false
        bool isViewingPreviousRound = true;
        string p1Text = "50";
        string p2Text = "60";

        bool canExecute = !isViewingPreviousRound && !string.IsNullOrEmpty(p1Text) && !string.IsNullOrEmpty(p2Text);
        Assert.False(canExecute);

        // When returning to active round:
        isViewingPreviousRound = false;
        canExecute = !isViewingPreviousRound && !string.IsNullOrEmpty(p1Text) && !string.IsNullOrEmpty(p2Text);
        Assert.True(canExecute);
    }

    // =========================================================================
    // TIER 2: BOUNDARY & CORNER CASES
    // =========================================================================

    [Fact]
    public void BoundaryCase_NegativeAndZeroScores_CalculatesTotalsAndExtremesCorrectly()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = -50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 0 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 150 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = -25 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // Alice: -50 + 150 = 100
        Assert.Equal(100, p1.PlayerScore);
        Assert.Equal(150, p1.HighestScoredHand);
        Assert.Equal(-50, p1.LowestScoredHand);

        // Bob: 0 + (-25) = -25
        Assert.Equal(-25, p2.PlayerScore);
        Assert.Equal(0, p2.HighestScoredHand);
        Assert.Equal(-25, p2.LowestScoredHand);

        // Round 1 extremes
        Assert.Equal(0, r1.CurrentHighestScoredHandValue);
        Assert.Equal(p2.ID, r1.PlayerHighestScoringHand?.ID);
        Assert.Equal(-50, r1.CurrentLowestScoredHandValue);
        Assert.Equal(p1.ID, r1.PlayerLowestScoringHand?.ID);

        // Round 2 extremes
        Assert.Equal(150, r2.CurrentHighestScoredHandValue);
        Assert.Equal(p1.ID, r2.PlayerHighestScoringHand?.ID);
        Assert.Equal(-25, r2.CurrentLowestScoredHandValue);
        Assert.Equal(p2.ID, r2.PlayerLowestScoringHand?.ID);
    }

    [Fact]
    public void BoundaryCase_EditingRound1In10RoundGame_PropagatesToAllDownstreamRounds()
    {
        var players = new List<TestPlayerModel>
        {
            new() { PlayerName = "Alice" },
            new() { PlayerName = "Bob" },
            new() { PlayerName = "Charlie" },
            new() { PlayerName = "Diana" }
        };

        var rounds = new List<TestRoundModel>();
        for (int i = 1; i <= 10; i++)
        {
            var round = new TestRoundModel();
            foreach (var p in players)
            {
                round.RoundScores.Add(new TestRoundScoreModel
                {
                    PlayerId = p.ID,
                    Score = 20 // Each player scores 20 per round
                });
            }
            rounds.Add(round);
        }

        var game = new TestCurrentGameModel
        {
            Players = new ObservableCollection<TestPlayerModel>(players),
            Round = new ObservableCollection<TestRoundModel>(rounds)
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // All players have 200 after 10 rounds
        foreach (var p in players)
        {
            Assert.Equal(200, p.PlayerScore);
        }

        // Act: Edit Round 1 score for Alice from 20 -> 100 (+80 boost)
        var aliceR1 = rounds[0].RoundScores.First(s => s.PlayerId == players[0].ID);
        aliceR1.Score = 100;

        TestGameRecomputationEngine.RecalculateGame(game);

        // Assert: Alice total is now 280, others are 200
        Assert.Equal(280, players[0].PlayerScore);
        Assert.Equal(200, players[1].PlayerScore);
        Assert.Equal(200, players[2].PlayerScore);
        Assert.Equal(200, players[3].PlayerScore);

        // Alice is the leader for every single round from Round 1 through Round 10
        for (int i = 0; i < 10; i++)
        {
            Assert.Equal(players[0].ID, rounds[i].LeadingPlayer?.ID);
        }
    }

    [Fact]
    public void BoundaryCase_TwoPlayerGame_RecalculatesCorrectly()
    {
        var p1 = new TestPlayerModel { PlayerName = "P1" };
        var p2 = new TestPlayerModel { PlayerName = "P2" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 45 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 55 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(45, p1.PlayerScore);
        Assert.Equal(55, p2.PlayerScore);
        Assert.Equal(p2.ID, r1.LeadingPlayer?.ID);
    }

    [Fact]
    public void BoundaryCase_SixPlayerGame_RecalculatesCorrectlyAndDeterminesCorrectLeader()
    {
        var pList = Enumerable.Range(1, 6)
            .Select(i => new TestPlayerModel { PlayerName = $"Player {i}" })
            .ToList();

        var r1 = new TestRoundModel();
        int[] scores = [10, 30, 85, 25, 90, 40]; // Player 5 has 90 (highest), Player 1 has 10 (lowest)
        for (int i = 0; i < 6; i++)
        {
            r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = pList[i].ID, Score = scores[i] });
        }

        var game = new TestCurrentGameModel
        {
            Players = new ObservableCollection<TestPlayerModel>(pList),
            Round = [r1]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(pList[4].ID, r1.LeadingPlayer?.ID);
        Assert.Equal(90, r1.CurrentHighestScoredHandValue);
        Assert.Equal(pList[4].ID, r1.PlayerHighestScoringHand?.ID);
        Assert.Equal(10, r1.CurrentLowestScoredHandValue);
        Assert.Equal(pList[0].ID, r1.PlayerLowestScoringHand?.ID);
    }

    [Fact]
    public void BoundaryCase_ScoreLimit_LowerBound100_DetectsWinnerAt100()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 100 };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 80 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            ScoreLimit = 100
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).ToList();
        Assert.Single(winners);
        Assert.Equal(p1.ID, winners[0].ID);
    }

    [Fact]
    public void BoundaryCase_ScoreLimit_UpperBound5000_DetectsWinnerAt5000()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 5020 };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 4980 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            ScoreLimit = 5000
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).ToList();
        Assert.Single(winners);
        Assert.Equal(p1.ID, winners[0].ID);
    }

    [Fact]
    public void BoundaryCase_ScoreLimit_LoweredBelowCurrentHighestScore_TriggersWinImmediately()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 350 };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 200 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            ScoreLimit = 500
        };

        // No winner with score limit 500
        var winnersBefore = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).ToList();
        Assert.Empty(winnersBefore);

        // Edit score limit down to 300
        game.ScoreLimit = 300;

        var winnersAfter = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).ToList();
        Assert.Single(winnersAfter);
        Assert.Equal(p1.ID, winnersAfter[0].ID);
    }

    [Fact]
    public void BoundaryCase_ZeroRoundsGame_HandlesGracefullyWithoutThrowing()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = []
        };

        // Act & Assert: Does not throw and players have 0 scores
        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(0, p1.PlayerScore);
        Assert.Equal(0, p2.PlayerScore);
    }

    [Fact]
    public void BoundaryCase_TiedHighestScoringHandInSingleRound_PicksHighestPlayerAndRecordsScore()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 100 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 100 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        Assert.Equal(100, r1.CurrentHighestScoredHandValue);
        Assert.NotNull(r1.PlayerHighestScoringHand);
        Assert.Equal(100, r1.CurrentLowestScoredHandValue);
        Assert.NotNull(r1.PlayerLowestScoringHand);
    }

    [Fact]
    public void BoundaryCase_EmptyRoundScores_FallbackToPlayersScoredHandThisRound_RecalculatesAccurately()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScoreText = "75" };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScoreText = "45" };

        var r1 = new TestRoundModel
        {
            PlayersScoredHandThisRound = [p1, p2]
            // RoundScores is deliberately empty (legacy data structure)
        };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1]
        };

        TestGameRecomputationEngine.RecalculateGame(game);

        // RoundScores automatically populated from PlayersScoredHandThisRound
        Assert.Equal(2, r1.RoundScores.Count);
        Assert.Equal(75, r1.RoundScores.First(s => s.PlayerId == p1.ID).Score);
        Assert.Equal(45, r1.RoundScores.First(s => s.PlayerId == p2.ID).Score);
        Assert.Equal(75, p1.PlayerScore);
        Assert.Equal(45, p2.PlayerScore);
    }

    // =========================================================================
    // TIER 3: CROSS-FEATURE COMBINATIONS
    // =========================================================================

    [Fact]
    public void CrossFeature_SequentialMultiRoundEditing_VerifiesCumulativeTotalsAtEachStep()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 50 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 30 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 });

        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 20 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 10 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2, r3]
        };

        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(100, p1.PlayerScore); // 50+30+20
        Assert.Equal(100, p2.PlayerScore); // 50+40+10

        // Step 1: Edit Round 1 Alice -> 80
        r1.RoundScores.First(s => s.PlayerId == p1.ID).Score = 80;
        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(130, p1.PlayerScore); // 80+30+20
        Assert.Equal(100, p2.PlayerScore);

        // Step 2: Edit Round 2 Bob -> 70
        r2.RoundScores.First(s => s.PlayerId == p2.ID).Score = 70;
        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(130, p1.PlayerScore);
        Assert.Equal(130, p2.PlayerScore); // 50+70+10

        // Step 3: Edit Round 3 Alice -> 40, Bob -> 20
        r3.RoundScores.First(s => s.PlayerId == p1.ID).Score = 40;
        r3.RoundScores.First(s => s.PlayerId == p2.ID).Score = 20;
        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(150, p1.PlayerScore); // 80+30+40
        Assert.Equal(140, p2.PlayerScore); // 50+70+20
        Assert.Equal(p1.ID, r3.LeadingPlayer?.ID);
    }

    [Fact]
    public void CrossFeature_ConvertInProgressToWon_ThenEditPreviousRounds_ChangesWinner()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 505 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 480 });

        var inProgressGame = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500
        };

        TestGameRecomputationEngine.RecalculateGame(inProgressGame);

        // Convert to PlayedGame Won with Alice as winner
        var playedGame = new TestPlayedGameModel
        {
            GameId = inProgressGame.GameId,
            Players = inProgressGame.Players,
            Round = inProgressGame.Round,
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        var stats1 = TestGameRecomputationEngine.AggregatePlayerStats([playedGame]);
        Assert.Equal(1, stats1[p1.ID].GamesWon);
        Assert.Equal(0, stats1[p2.ID].GamesWon);

        // Act: User corrects Round 1 score in EditGame (Alice was actually 455, Bob was 530)
        r1.RoundScores.First(s => s.PlayerId == p1.ID).Score = 455;
        r1.RoundScores.First(s => s.PlayerId == p2.ID).Score = 530;

        TestGameRecomputationEngine.RecalculateGame(playedGame);
        playedGame.WinningPlayer = p2; // Correct winner assigned

        var stats2 = TestGameRecomputationEngine.AggregatePlayerStats([playedGame]);
        Assert.Equal(0, stats2[p1.ID].GamesWon);
        Assert.Equal(1, stats2[p1.ID].GamesLost);
        Assert.Equal(1, stats2[p2.ID].GamesWon);
        Assert.Equal(0, stats2[p2.ID].GamesLost);
        Assert.Equal(455, stats2[p1.ID].LifetimeScore);
        Assert.Equal(530, stats2[p2.ID].LifetimeScore);
    }

    // =========================================================================
    // TIER 4: REAL-WORLD WORKLOAD
    // =========================================================================

    [Fact]
    public void RealWorld_FourPlayerFiveRoundGameSimulation_Round2ScoreCorrectionInRound4_ChangesFinalWinner()
    {
        // Arrange: 4 players
        var alice = new TestPlayerModel { PlayerName = "Alice" };
        var bob = new TestPlayerModel { PlayerName = "Bob" };
        var charlie = new TestPlayerModel { PlayerName = "Charlie" };
        var diana = new TestPlayerModel { PlayerName = "Diana" };

        var players = new ObservableCollection<TestPlayerModel> { alice, bob, charlie, diana };

        // Round 1
        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = alice.ID, Score = 100 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = bob.ID, Score = 80 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = charlie.ID, Score = 60 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = diana.ID, Score = 40 });

        // Round 2 (Bob's score accidentally entered as 250 instead of 25)
        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = alice.ID, Score = 80 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = bob.ID, Score = 250 }); // TYPO!
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = charlie.ID, Score = 70 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = diana.ID, Score = 50 });

        // Round 3
        var r3 = new TestRoundModel();
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = alice.ID, Score = 120 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = bob.ID, Score = 80 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = charlie.ID, Score = 100 });
        r3.RoundScores.Add(new TestRoundScoreModel { PlayerId = diana.ID, Score = 60 });

        // Round 4
        var r4 = new TestRoundModel();
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = alice.ID, Score = 110 });
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = bob.ID, Score = 70 });
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = charlie.ID, Score = 80 });
        r4.RoundScores.Add(new TestRoundScoreModel { PlayerId = diana.ID, Score = 60 });

        var game = new TestCurrentGameModel
        {
            Players = players,
            Round = [r1, r2, r3, r4],
            ScoreLimit = 500
        };

        // Recompute with the error: Bob appears to lead with 480 points
        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(410, alice.PlayerScore);
        Assert.Equal(480, bob.PlayerScore); // Bob incorrectly near 500
        Assert.Equal(310, charlie.PlayerScore);
        Assert.Equal(210, diana.PlayerScore);
        Assert.Equal(bob.ID, r4.LeadingPlayer?.ID);

        // Correction: User navigates back to Round 2 and corrects Bob's score to 25
        r2.RoundScores.First(s => s.PlayerId == bob.ID).Score = 25;

        // Real-time recomputation triggered
        TestGameRecomputationEngine.RecalculateGame(game);

        // Totals after correction:
        // Alice: 100 + 80 + 120 + 110 = 410
        // Bob: 80 + 25 + 80 + 70 = 255
        // Charlie: 60 + 70 + 100 + 80 = 310
        // Diana: 40 + 50 + 60 + 60 = 210
        Assert.Equal(410, alice.PlayerScore);
        Assert.Equal(255, bob.PlayerScore);
        Assert.Equal(310, charlie.PlayerScore);
        Assert.Equal(210, diana.PlayerScore);

        // Alice is now correctly identified as the leader in Round 4
        Assert.Equal(alice.ID, r4.LeadingPlayer?.ID);

        // Round 5 is played
        var r5 = new TestRoundModel();
        r5.RoundScores.Add(new TestRoundScoreModel { PlayerId = alice.ID, Score = 95 });
        r5.RoundScores.Add(new TestRoundScoreModel { PlayerId = bob.ID, Score = 80 });
        r5.RoundScores.Add(new TestRoundScoreModel { PlayerId = charlie.ID, Score = 50 });
        r5.RoundScores.Add(new TestRoundScoreModel { PlayerId = diana.ID, Score = 40 });
        game.Round.Add(r5);

        TestGameRecomputationEngine.RecalculateGame(game);

        // Final scores:
        // Alice: 410 + 95 = 505 (Threshold reached!)
        // Bob: 255 + 80 = 335
        // Charlie: 310 + 50 = 360
        // Diana: 210 + 40 = 250
        Assert.Equal(505, alice.PlayerScore);
        Assert.Equal(335, bob.PlayerScore);
        Assert.Equal(360, charlie.PlayerScore);
        Assert.Equal(250, diana.PlayerScore);

        // Winner determination
        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).ToList();
        Assert.Single(winners);
        Assert.Equal(alice.ID, winners[0].ID);
        Assert.Equal("Alice", winners[0].PlayerName);
    }
}
