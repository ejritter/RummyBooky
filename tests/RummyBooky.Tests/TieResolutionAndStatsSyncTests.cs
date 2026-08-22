using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace RummyBooky.Tests;

public class MockEditGameViewModelState
{
    public TestGameModel Game { get; set; }
    public string SelectedStatus { get; set; } = "In-Progress";
    public bool IsWinnerPickerVisible { get; set; } = false;
    public TestPlayerModel? SelectedWinner { get; set; } = null;
    public int ScoreLimit { get; set; } = 500;
    public List<TestPlayerModel> AvailablePlayers { get; set; } = [];

    public MockEditGameViewModelState(TestGameModel game)
    {
        Game = game;
        TestGameRecomputationEngine.RecalculateGame(game);

        AvailablePlayers = game.Players.ToList();

        if (game is TestPlayedGameModel played)
        {
            ScoreLimit = played.ScoreLimit;
            SelectedStatus = played.GameState.ToString();
            SelectedWinner = AvailablePlayers.FirstOrDefault(p => p.ID == played.WinningPlayer?.ID);
            IsWinnerPickerVisible = played.GameState == TestGameStatus.Won;
        }
        else if (game is TestCurrentGameModel current)
        {
            ScoreLimit = current.ScoreLimit;
            SelectedStatus = "In-Progress";
            SelectedWinner = null;
            IsWinnerPickerVisible = false;
        }
    }

    public void ChangeStatus(string newStatus)
    {
        SelectedStatus = newStatus;
        IsWinnerPickerVisible = newStatus == "Won";
        if (newStatus != "Won")
        {
            SelectedWinner = null;
        }
        else if (SelectedWinner is null && AvailablePlayers.Count > 0)
        {
            SelectedWinner = AvailablePlayers.OrderByDescending(p => p.PlayerScore).FirstOrDefault();
        }
    }

    public TestGameModel CreateFinalGameToSave()
    {
        TestGameRecomputationEngine.RecalculateGame(Game);

        if (SelectedStatus == "In-Progress")
        {
            return new TestCurrentGameModel
            {
                GameId = Game.GameId,
                Players = Game.Players,
                Round = Game.Round,
                IsGameActive = true,
                IsGameFinished = false,
                ScoreLimit = ScoreLimit
            };
        }
        else
        {
            var status = SelectedStatus switch
            {
                "Won" => TestGameStatus.Won,
                "Draw" => TestGameStatus.Draw,
                "Forfeit" => TestGameStatus.Forfeit,
                _ => TestGameStatus.Unknown
            };

            var winner = status == TestGameStatus.Won ? SelectedWinner : null;

            return new TestPlayedGameModel
            {
                GameId = Game.GameId,
                Players = Game.Players,
                Round = Game.Round,
                IsGameActive = false,
                IsGameFinished = true,
                ScoreLimit = ScoreLimit,
                GameState = status,
                WinningPlayer = winner
            };
        }
    }
}

public class TieResolutionAndStatsSyncTests
{
    // =========================================================================
    // TIER 1: EDIT GAME PAGE STATE MANAGEMENT & TRANSITIONS
    // =========================================================================

    [Fact]
    public void EditGame_StatusTransition_ToWon_ShowsWinnerPickerAndSelectsHighestScorerAsDefault()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 520 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 480 });

        var inProgressGame = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500
        };

        var vm = new MockEditGameViewModelState(inProgressGame);
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);

        // Transition status to "Won"
        vm.ChangeStatus("Won");

        Assert.True(vm.IsWinnerPickerVisible);
        Assert.NotNull(vm.SelectedWinner);
        Assert.Equal(p1.ID, vm.SelectedWinner.ID); // Alice (520) is highest scorer
    }

    [Fact]
    public void EditGame_StatusTransition_ToDrawOrForfeitOrInProgress_HidesWinnerPickerAndClearsWinner()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 500 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 500 });

        var playedGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        var vm = new MockEditGameViewModelState(playedGame);
        Assert.True(vm.IsWinnerPickerVisible);
        Assert.Equal(p1.ID, vm.SelectedWinner?.ID);

        // Transition to Draw
        vm.ChangeStatus("Draw");
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);

        // Transition back to Won
        vm.ChangeStatus("Won");
        Assert.True(vm.IsWinnerPickerVisible);
        Assert.NotNull(vm.SelectedWinner);

        // Transition to Forfeit
        vm.ChangeStatus("Forfeit");
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);

        // Transition to In-Progress
        vm.ChangeStatus("In-Progress");
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
    }

    [Fact]
    public void EditGame_Save_ConstructsCorrectPlayedGameOrCurrentGameModel()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 250 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 200 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500
        };

        var vm = new MockEditGameViewModelState(game);

        // Case 1: Save as In-Progress with modified score limit
        vm.ScoreLimit = 750;
        var savedInProgress = vm.CreateFinalGameToSave();
        Assert.IsType<TestCurrentGameModel>(savedInProgress);
        var current = (TestCurrentGameModel)savedInProgress;
        Assert.True(current.IsGameActive);
        Assert.False(current.IsGameFinished);
        Assert.Equal(750, current.ScoreLimit);

        // Case 2: Save as Won with Bob as manually selected winner
        vm.ChangeStatus("Won");
        vm.SelectedWinner = p2;
        var savedWon = vm.CreateFinalGameToSave();
        Assert.IsType<TestPlayedGameModel>(savedWon);
        var played = (TestPlayedGameModel)savedWon;
        Assert.False(played.IsGameActive);
        Assert.True(played.IsGameFinished);
        Assert.Equal(TestGameStatus.Won, played.GameState);
        Assert.Equal(p2.ID, played.WinningPlayer?.ID);
    }

    // =========================================================================
    // TIER 2: TIE RESOLUTION & SCORE LIMIT BOUNDARIES
    // =========================================================================

    [Fact]
    public void TieResolution_TwoPlayersTiedAboveScoreLimit_DetectedAsDraw()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 520 };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 520 };
        var p3 = new TestPlayerModel { PlayerName = "Charlie", PlayerScore = 400 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            ScoreLimit = 500
        };

        // Winner check logic:
        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        var isDraw = winners.Count > 1;
        Assert.True(isDraw);
        Assert.Equal(2, winners.Count);
        Assert.Contains(p1, winners);
        Assert.Contains(p2, winners);
    }

    [Fact]
    public void TieResolution_ThreePlayersTiedAboveScoreLimit_DetectedAsDrawWithThreeWinners()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 510 };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 510 };
        var p3 = new TestPlayerModel { PlayerName = "Charlie", PlayerScore = 510 };
        var p4 = new TestPlayerModel { PlayerName = "Diana", PlayerScore = 300 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3, p4],
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert.Equal(3, winners.Count);
        Assert.Contains(p1, winners);
        Assert.Contains(p2, winners);
        Assert.Contains(p3, winners);
    }

    [Fact]
    public void TieResolution_HigherScoreBeatsLowerTie_WinnerIsHighestPlayerOnly()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice", PlayerScore = 500 };
        var p2 = new TestPlayerModel { PlayerName = "Bob", PlayerScore = 500 };
        var p3 = new TestPlayerModel { PlayerName = "Charlie", PlayerScore = 540 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        // Charlie has 540 > 500, so Charlie is the single winner
        Assert.Single(winners);
        Assert.Equal(p3.ID, winners[0].ID);
    }

    [Fact]
    public void TieResolution_ManualWinnerPicker_AllowsOverridingDrawWithSelectedWinner()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 500 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 500 });

        var initialDrawGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Draw,
            WinningPlayer = null
        };

        TestGameRecomputationEngine.RecalculateGame(initialDrawGame);
        var statsBefore = TestGameRecomputationEngine.AggregatePlayerStats([initialDrawGame]);
        Assert.Equal(1, statsBefore[p1.ID].GameDraws);
        Assert.Equal(1, statsBefore[p2.ID].GameDraws);
        Assert.Equal(0, statsBefore[p1.ID].GamesWon);
        Assert.Equal(0, statsBefore[p2.ID].GamesWon);

        // Act: Edit game, change status to Won and manually select Bob as winner
        var vm = new MockEditGameViewModelState(initialDrawGame);
        vm.ChangeStatus("Won");
        vm.SelectedWinner = p2;
        var resolvedGame = (TestPlayedGameModel)vm.CreateFinalGameToSave();

        var statsAfter = TestGameRecomputationEngine.AggregatePlayerStats([resolvedGame]);
        Assert.Equal(0, statsAfter[p1.ID].GameDraws);
        Assert.Equal(0, statsAfter[p2.ID].GameDraws);
        Assert.Equal(0, statsAfter[p1.ID].GamesWon);
        Assert.Equal(1, statsAfter[p1.ID].GamesLost);
        Assert.Equal(1, statsAfter[p2.ID].GamesWon);
        Assert.Equal(0, statsAfter[p2.ID].GamesLost);
    }

    // =========================================================================
    // TIER 3: LIFETIME STATS SYNC & GLOBAL RANKINGS
    // =========================================================================

    [Fact]
    public void LifetimeStats_ForfeitGame_IncrementsGamesForfeitAndZeroesPointsInLifetimeScore()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 150 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 100 });

        var forfeitGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Forfeit,
            WinningPlayer = null
        };

        TestGameRecomputationEngine.RecalculateGame(forfeitGame);
        var stats = TestGameRecomputationEngine.AggregatePlayerStats([forfeitGame]);

        // Forfeit game increments TotalGamesPlayed and GamesForfeit, but adds 0 to LifetimeScore
        Assert.Equal(1, stats[p1.ID].TotalGamesPlayed);
        Assert.Equal(1, stats[p1.ID].GamesForfeit);
        Assert.Equal(0, stats[p1.ID].LifetimeScore);
        Assert.Equal(0, stats[p1.ID].GamesWon);
        Assert.Equal(0, stats[p1.ID].GamesLost);
        Assert.Equal(0, stats[p1.ID].GameDraws);

        Assert.Equal(1, stats[p2.ID].TotalGamesPlayed);
        Assert.Equal(1, stats[p2.ID].GamesForfeit);
        Assert.Equal(0, stats[p2.ID].LifetimeScore);
    }

    [Fact]
    public void LifetimeStats_DrawGame_IncrementsGameDrawsAndAddsLifetimeScoreWithoutWinOrLoss()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 500 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 500 });

        var drawGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Draw,
            WinningPlayer = null
        };

        TestGameRecomputationEngine.RecalculateGame(drawGame);
        var stats = TestGameRecomputationEngine.AggregatePlayerStats([drawGame]);

        Assert.Equal(1, stats[p1.ID].TotalGamesPlayed);
        Assert.Equal(1, stats[p1.ID].GameDraws);
        Assert.Equal(500, stats[p1.ID].LifetimeScore);
        Assert.Equal(0, stats[p1.ID].GamesWon);
        Assert.Equal(0, stats[p1.ID].GamesLost);

        Assert.Equal(1, stats[p2.ID].TotalGamesPlayed);
        Assert.Equal(1, stats[p2.ID].GameDraws);
        Assert.Equal(500, stats[p2.ID].LifetimeScore);
        Assert.Equal(0, stats[p2.ID].GamesWon);
        Assert.Equal(0, stats[p2.ID].GamesLost);
    }

    [Fact]
    public void LifetimeStats_WonGame_IncrementsWonForWinnerAndLostForLosers()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 550 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 420 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 380 });

        var wonGame = new TestPlayedGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        TestGameRecomputationEngine.RecalculateGame(wonGame);
        var stats = TestGameRecomputationEngine.AggregatePlayerStats([wonGame]);

        // Winner Alice
        Assert.Equal(1, stats[p1.ID].GamesWon);
        Assert.Equal(0, stats[p1.ID].GamesLost);
        Assert.Equal(550, stats[p1.ID].LifetimeScore);

        // Loser Bob
        Assert.Equal(0, stats[p2.ID].GamesWon);
        Assert.Equal(1, stats[p2.ID].GamesLost);
        Assert.Equal(420, stats[p2.ID].LifetimeScore);

        // Loser Charlie
        Assert.Equal(0, stats[p3.ID].GamesWon);
        Assert.Equal(1, stats[p3.ID].GamesLost);
        Assert.Equal(380, stats[p3.ID].LifetimeScore);
    }

    [Fact]
    public void CrossFeature_TransitionLifecycle_WonToDrawToForfeitToWon()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 520 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 480 });

        var game = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        var vm = new MockEditGameViewModelState(game);

        // Step 1: Initially Won
        Assert.Equal("Won", vm.SelectedStatus);
        Assert.True(vm.IsWinnerPickerVisible);
        Assert.Equal(p1.ID, vm.SelectedWinner?.ID);

        // Step 2: Switch to Draw
        vm.ChangeStatus("Draw");
        Assert.Equal("Draw", vm.SelectedStatus);
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
        var drawModel = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Draw, drawModel.GameState);
        Assert.Null(drawModel.WinningPlayer);

        // Step 3: Switch to Forfeit
        vm.ChangeStatus("Forfeit");
        Assert.Equal("Forfeit", vm.SelectedStatus);
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
        var forfeitModel = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Forfeit, forfeitModel.GameState);
        Assert.Null(forfeitModel.WinningPlayer);

        // Step 4: Switch back to Won
        vm.ChangeStatus("Won");
        Assert.Equal("Won", vm.SelectedStatus);
        Assert.True(vm.IsWinnerPickerVisible);
        Assert.NotNull(vm.SelectedWinner); // Defaults to highest scorer (Alice)
        Assert.Equal(p1.ID, vm.SelectedWinner.ID);
        var wonModel = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Won, wonModel.GameState);
        Assert.Equal(p1.ID, wonModel.WinningPlayer?.ID);
    }

    [Fact]
    public void CrossFeature_ConvertWonGameToForfeit_RemovesLifetimeStatsAndUpdatesRanks()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 500 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 400 });

        var game = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        TestGameRecomputationEngine.RecalculateGame(game);
        var statsWon = TestGameRecomputationEngine.AggregatePlayerStats([game]);
        Assert.Equal(500, statsWon[p1.ID].LifetimeScore);
        Assert.Equal(1, statsWon[p1.ID].GamesWon);
        Assert.Equal(400, statsWon[p2.ID].LifetimeScore);
        Assert.Equal(1, statsWon[p2.ID].GamesLost);

        // Act: Convert Game to Forfeit
        var vm = new MockEditGameViewModelState(game);
        vm.ChangeStatus("Forfeit");
        var forfeitGame = (TestPlayedGameModel)vm.CreateFinalGameToSave();

        var statsForfeit = TestGameRecomputationEngine.AggregatePlayerStats([forfeitGame]);

        // Assert: Lifetime scores removed, GamesWon/GamesLost removed, GamesForfeit added
        Assert.Equal(0, statsForfeit[p1.ID].LifetimeScore);
        Assert.Equal(0, statsForfeit[p1.ID].GamesWon);
        Assert.Equal(0, statsForfeit[p1.ID].GamesLost);
        Assert.Equal(1, statsForfeit[p1.ID].GamesForfeit);

        Assert.Equal(0, statsForfeit[p2.ID].LifetimeScore);
        Assert.Equal(0, statsForfeit[p2.ID].GamesWon);
        Assert.Equal(0, statsForfeit[p2.ID].GamesLost);
        Assert.Equal(1, statsForfeit[p2.ID].GamesForfeit);
    }

    [Fact]
    public void GlobalRankings_MultipleGames_OrdersByLifetimeScoreThenGamesWonThenName()
    {
        var charlieId = Guid.NewGuid();
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var dianaId = Guid.NewGuid();

        // Game 1: Diana (500, won), Alice (300), Bob (200), Charlie (100)
        var d1 = new TestPlayerModel { ID = dianaId, PlayerName = "Diana", PlayerScore = 500 };
        var a1 = new TestPlayerModel { ID = aliceId, PlayerName = "Alice", PlayerScore = 300 };
        var b1 = new TestPlayerModel { ID = bobId, PlayerName = "Bob", PlayerScore = 200 };
        var c1 = new TestPlayerModel { ID = charlieId, PlayerName = "Charlie", PlayerScore = 100 };
        var g1 = new TestPlayedGameModel
        {
            Players = [d1, a1, b1, c1],
            GameState = TestGameStatus.Won,
            WinningPlayer = d1
        };

        // Game 2: Diana (1000), Alice (700), Bob (800), Charlie (900, won)
        var d2 = new TestPlayerModel { ID = dianaId, PlayerName = "Diana", PlayerScore = 1000 };
        var a2 = new TestPlayerModel { ID = aliceId, PlayerName = "Alice", PlayerScore = 700 };
        var b2 = new TestPlayerModel { ID = bobId, PlayerName = "Bob", PlayerScore = 800 };
        var c2 = new TestPlayerModel { ID = charlieId, PlayerName = "Charlie", PlayerScore = 900 };
        var g2 = new TestPlayedGameModel
        {
            Players = [d2, a2, b2, c2],
            GameState = TestGameStatus.Won,
            WinningPlayer = c2
        };

        // Game 3: Alice won vs Charlie (0 points each)
        var a3 = new TestPlayerModel { ID = aliceId, PlayerName = "Alice", PlayerScore = 0 };
        var c3 = new TestPlayerModel { ID = charlieId, PlayerName = "Charlie", PlayerScore = 0 };
        var g3 = new TestPlayedGameModel
        {
            Players = [a3, c3],
            GameState = TestGameStatus.Won,
            WinningPlayer = a3
        };

        // Game 4: Alice won vs Charlie (0 points each)
        var a4 = new TestPlayerModel { ID = aliceId, PlayerName = "Alice", PlayerScore = 0 };
        var c4 = new TestPlayerModel { ID = charlieId, PlayerName = "Charlie", PlayerScore = 0 };
        var g4 = new TestPlayedGameModel
        {
            Players = [a4, c4],
            GameState = TestGameStatus.Won,
            WinningPlayer = a4
        };

        // Game 5: Charlie won vs Bob (0 points each)
        var c5 = new TestPlayerModel { ID = charlieId, PlayerName = "Charlie", PlayerScore = 0 };
        var b5 = new TestPlayerModel { ID = bobId, PlayerName = "Bob", PlayerScore = 0 };
        var g5 = new TestPlayedGameModel
        {
            Players = [c5, b5],
            GameState = TestGameStatus.Won,
            WinningPlayer = c5
        };

        // Game 6: Bob won vs Diana (0 points each)
        var b6 = new TestPlayerModel { ID = bobId, PlayerName = "Bob", PlayerScore = 0 };
        var d6 = new TestPlayerModel { ID = dianaId, PlayerName = "Diana", PlayerScore = 0 };
        var g6 = new TestPlayedGameModel
        {
            Players = [b6, d6],
            GameState = TestGameStatus.Won,
            WinningPlayer = b6
        };

        var stats = TestGameRecomputationEngine.AggregatePlayerStats([g1, g2, g3, g4, g5, g6]);

        // Totals:
        // Diana: 1500 lifetime score, 1 win
        // Alice: 1000 lifetime score, 2 wins (Name: Alice)
        // Charlie: 1000 lifetime score, 2 wins (Name: Charlie)
        // Bob: 1000 lifetime score, 1 win (Name: Bob)

        var ranked = stats.Values
            .OrderByDescending(p => p.LifetimeScore)
            .ThenByDescending(p => p.GamesWon)
            .ThenBy(p => p.PlayerName)
            .ToList();

        // Expected Rank 1: Diana (1500 score)
        Assert.Equal("Diana", ranked[0].PlayerName);
        // Expected Rank 2: Alice (1000 score, 2 wins, 'A' before 'C')
        Assert.Equal("Alice", ranked[1].PlayerName);
        // Expected Rank 3: Charlie (1000 score, 2 wins)
        Assert.Equal("Charlie", ranked[2].PlayerName);
        // Expected Rank 4: Bob (1000 score, 1 win)
        Assert.Equal("Bob", ranked[3].PlayerName);
    }

    // =========================================================================
    // TIER 4: POLYMORPHIC SERIALIZATION & LEGACY COMPATIBILITY
    // =========================================================================

    [Fact]
    public void PolymorphicSerialization_CurrentAndPlayedGameModels_RoundTripWithIntegrity()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 120 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 80 });

        TestGameModel current = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500
        };

        TestGameModel played = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        var currentJson = JsonSerializer.Serialize(current);
        var playedJson = JsonSerializer.Serialize(played);

        var deserializedCurrent = JsonSerializer.Deserialize<TestGameModel>(currentJson);
        var deserializedPlayed = JsonSerializer.Deserialize<TestGameModel>(playedJson);

        Assert.NotNull(deserializedCurrent);
        Assert.IsType<TestCurrentGameModel>(deserializedCurrent);
        Assert.True(deserializedCurrent.IsGameActive);

        Assert.NotNull(deserializedPlayed);
        Assert.IsType<TestPlayedGameModel>(deserializedPlayed);
        var playedCast = (TestPlayedGameModel)deserializedPlayed;
        Assert.Equal(TestGameStatus.Won, playedCast.GameState);
        Assert.Equal(p1.ID, playedCast.WinningPlayer?.ID);
    }
}
