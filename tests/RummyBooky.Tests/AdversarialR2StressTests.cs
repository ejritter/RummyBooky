using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using Xunit;

namespace RummyBooky.Tests;

public class AdversarialR2StressTests
{
    private const string Alice = "Alice";
    private const string Bob = "Bob";
    private const string Charlie = "Charlie";
    private const string Diana = "Diana";

    // =========================================================================
    // SECTION 1: 2-PLAYER, 3-PLAYER, AND N-PLAYER TIE RESOLUTION CHALLENGES
    // =========================================================================

    [Fact]
    public void TieResolution_TwoPlayersExactScoreLimit_DetectedAsDraw()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice, PlayerScore = 500 };
        var p2 = new TestPlayerModel { PlayerName = Bob, PlayerScore = 500 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert.Equal(2, winners.Count);
        Assert.Contains(p1, winners);
        Assert.Contains(p2, winners);
    }

    [Fact]
    public void TieResolution_TwoPlayersTiedAboveScoreLimit_DetectedAsDraw()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice, PlayerScore = 780 };
        var p2 = new TestPlayerModel { PlayerName = Bob, PlayerScore = 780 };
        var p3 = new TestPlayerModel { PlayerName = Charlie, PlayerScore = 650 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert.Equal(2, winners.Count);
        Assert.Contains(p1, winners);
        Assert.Contains(p2, winners);
        Assert.DoesNotContain(p3, winners);
    }

    [Fact]
    public void TieResolution_ThreePlayersTiedAtHighestScore_DetectedAsThreeWayDraw()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice, PlayerScore = 540 };
        var p2 = new TestPlayerModel { PlayerName = Bob, PlayerScore = 540 };
        var p3 = new TestPlayerModel { PlayerName = Charlie, PlayerScore = 540 };
        var p4 = new TestPlayerModel { PlayerName = Diana, PlayerScore = 510 };

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
        Assert.DoesNotContain(p4, winners);
    }

    [Fact]
    public void TieResolution_SixPlayersAllTiedAtScoreLimit_DetectedAsSixWayDraw()
    {
        var players = Enumerable.Range(1, 6)
            .Select(i => new TestPlayerModel { PlayerName = $"Player {i}", PlayerScore = 500 })
            .ToList();

        var game = new TestCurrentGameModel
        {
            Players = new ObservableCollection<TestPlayerModel>(players),
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert.Equal(6, winners.Count);
    }

    [Fact]
    public void TieResolution_HigherScoreBeatsLowerTieAboveLimit_YieldsSingleWinner()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice, PlayerScore = 510 };
        var p2 = new TestPlayerModel { PlayerName = Bob, PlayerScore = 510 };
        var p3 = new TestPlayerModel { PlayerName = Charlie, PlayerScore = 560 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        winners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert.Single(winners);
        Assert.Equal(p3.ID, winners[0].ID);
    }

    [Fact]
    public void TieResolution_PlayersTiedBelowScoreLimit_NoWinnersDetected()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice, PlayerScore = 490 };
        var p2 = new TestPlayerModel { PlayerName = Bob, PlayerScore = 490 };

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            ScoreLimit = 500
        };

        var winners = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).OrderByDescending(p => p.PlayerScore).ToList();
        Assert.Empty(winners);
    }

    // =========================================================================
    // SECTION 2: MANUAL WINNER OVERRIDES ON DRAWS AND REVERSALS
    // =========================================================================

    [Fact]
    public void ManualWinnerOverride_TwoPlayerDraw_OverriddenToPlayer2Win()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };

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
        var statsBefore = TestGameRecomputationEngine.AggregatePlayerStats([drawGame]);
        Assert.Equal(1, statsBefore[p1.ID].GameDraws);
        Assert.Equal(1, statsBefore[p2.ID].GameDraws);
        Assert.Equal(0, statsBefore[p1.ID].GamesWon);
        Assert.Equal(0, statsBefore[p2.ID].GamesWon);

        // Act: User overrides to Won with Bob as winner
        var vm = new MockEditGameViewModelState(drawGame);
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

    [Fact]
    public void ManualWinnerOverride_ThreePlayerDraw_OverriddenToPlayer3Win()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };
        var p3 = new TestPlayerModel { PlayerName = Charlie };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 500 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 500 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 500 });

        var drawGame = new TestPlayedGameModel
        {
            Players = [p1, p2, p3],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Draw,
            WinningPlayer = null
        };

        TestGameRecomputationEngine.RecalculateGame(drawGame);

        var vm = new MockEditGameViewModelState(drawGame);
        vm.ChangeStatus("Won");
        vm.SelectedWinner = p3; // Charlie chosen manually
        var wonGame = (TestPlayedGameModel)vm.CreateFinalGameToSave();

        var stats = TestGameRecomputationEngine.AggregatePlayerStats([wonGame]);
        Assert.Equal(1, stats[p3.ID].GamesWon);
        Assert.Equal(0, stats[p3.ID].GamesLost);
        Assert.Equal(0, stats[p1.ID].GamesWon);
        Assert.Equal(1, stats[p1.ID].GamesLost);
        Assert.Equal(0, stats[p2.ID].GamesWon);
        Assert.Equal(1, stats[p2.ID].GamesLost);
        Assert.Equal(0, stats[p1.ID].GameDraws);
        Assert.Equal(0, stats[p2.ID].GameDraws);
        Assert.Equal(0, stats[p3.ID].GameDraws);
    }

    [Fact]
    public void ManualWinnerOverride_OverrideReversal_WonBackToDraw_RestoresDrawStats()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 510 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 510 });

        var wonGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        var vm = new MockEditGameViewModelState(wonGame);
        Assert.Equal("Won", vm.SelectedStatus);
        Assert.Equal(p1.ID, vm.SelectedWinner?.ID);

        // Revert to Draw
        vm.ChangeStatus("Draw");
        Assert.Equal("Draw", vm.SelectedStatus);
        Assert.Null(vm.SelectedWinner);
        Assert.False(vm.IsWinnerPickerVisible);

        var drawGame = (TestPlayedGameModel)vm.CreateFinalGameToSave();
        Assert.Equal(TestGameStatus.Draw, drawGame.GameState);
        Assert.Null(drawGame.WinningPlayer);

        var stats = TestGameRecomputationEngine.AggregatePlayerStats([drawGame]);
        Assert.Equal(1, stats[p1.ID].GameDraws);
        Assert.Equal(1, stats[p2.ID].GameDraws);
        Assert.Equal(0, stats[p1.ID].GamesWon);
        Assert.Equal(0, stats[p2.ID].GamesLost);
        Assert.Equal(0, stats[p2.ID].GamesWon);
        Assert.Equal(0, stats[p1.ID].GamesLost);
    }

    // =========================================================================
    // SECTION 3: STATUS TRANSITIONS ACROSS ALL 4 STATES (WON, DRAW, FORFEIT, IN-PROGRESS)
    // =========================================================================

    [Fact]
    public void StatusTransition_FullCycle_InProgress_Won_Draw_Forfeit_InProgress_PreservesDataIntegrity()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 300 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 250 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 220 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 200 });

        var initialGame = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1, r2],
            ScoreLimit = 500
        };

        var vm = new MockEditGameViewModelState(initialGame);

        // 1. Initial State: In-Progress
        Assert.Equal("In-Progress", vm.SelectedStatus);
        Assert.False(vm.IsWinnerPickerVisible);

        // 2. Transition to Won
        vm.ChangeStatus("Won");
        Assert.Equal("Won", vm.SelectedStatus);
        Assert.True(vm.IsWinnerPickerVisible);
        Assert.Equal(p1.ID, vm.SelectedWinner?.ID); // Highest scorer Alice (520) selected
        var wonSave = vm.CreateFinalGameToSave();
        Assert.IsType<TestPlayedGameModel>(wonSave);
        Assert.Equal(TestGameStatus.Won, ((TestPlayedGameModel)wonSave).GameState);

        // 3. Transition to Draw
        vm.ChangeStatus("Draw");
        Assert.Equal("Draw", vm.SelectedStatus);
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
        var drawSave = vm.CreateFinalGameToSave();
        Assert.IsType<TestPlayedGameModel>(drawSave);
        Assert.Equal(TestGameStatus.Draw, ((TestPlayedGameModel)drawSave).GameState);
        Assert.Null(((TestPlayedGameModel)drawSave).WinningPlayer);

        // 4. Transition to Forfeit
        vm.ChangeStatus("Forfeit");
        Assert.Equal("Forfeit", vm.SelectedStatus);
        Assert.False(vm.IsWinnerPickerVisible);
        Assert.Null(vm.SelectedWinner);
        var forfeitSave = vm.CreateFinalGameToSave();
        Assert.IsType<TestPlayedGameModel>(forfeitSave);
        Assert.Equal(TestGameStatus.Forfeit, ((TestPlayedGameModel)forfeitSave).GameState);
        Assert.Null(((TestPlayedGameModel)forfeitSave).WinningPlayer);

        // 5. Transition back to In-Progress
        vm.ChangeStatus("In-Progress");
        Assert.Equal("In-Progress", vm.SelectedStatus);
        Assert.False(vm.IsWinnerPickerVisible);
        var inProgressSave = vm.CreateFinalGameToSave();
        Assert.IsType<TestCurrentGameModel>(inProgressSave);
        Assert.True(inProgressSave.IsGameActive);
        Assert.False(inProgressSave.IsGameFinished);
    }

    [Fact]
    public void StatusTransition_PlayedGameConvertedToInProgress_ExcludesGameFromHistoricalLifetimeStats()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 520 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 400 });

        var playedGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        TestGameRecomputationEngine.RecalculateGame(playedGame);
        var statsBefore = TestGameRecomputationEngine.AggregatePlayerStats([playedGame]);
        Assert.Equal(1, statsBefore[p1.ID].TotalGamesPlayed);
        Assert.Equal(1, statsBefore[p1.ID].GamesWon);
        Assert.Equal(520, statsBefore[p1.ID].LifetimeScore);

        // Act: User re-opens game in EditGame and converts status back to In-Progress
        var vm = new MockEditGameViewModelState(playedGame);
        vm.ChangeStatus("In-Progress");
        var activeGame = vm.CreateFinalGameToSave();

        var statsAfter = TestGameRecomputationEngine.AggregatePlayerStats([activeGame]);
        // Active game is omitted from played game statistics
        Assert.Equal(0, statsAfter[p1.ID].TotalGamesPlayed);
        Assert.Equal(0, statsAfter[p1.ID].GamesWon);
        Assert.Equal(0, statsAfter[p1.ID].LifetimeScore);
    }

    // =========================================================================
    // SECTION 4: SCORE LIMIT MODIFICATIONS
    // =========================================================================

    [Fact]
    public void ScoreLimit_ModifiedBelowCurrentScore_TriggersWinOnNextCheck()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 350 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 200 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500
        };

        TestGameRecomputationEngine.RecalculateGame(game);
        var winnersBefore = game.Players.Where(p => p.PlayerScore >= game.ScoreLimit).ToList();
        Assert.Empty(winnersBefore);

        // Modify ScoreLimit to 300 (below Alice's 350)
        var vm = new MockEditGameViewModelState(game);
        vm.ScoreLimit = 300;
        var savedGame = (TestCurrentGameModel)vm.CreateFinalGameToSave();

        Assert.Equal(300, savedGame.ScoreLimit);
        var winnersAfter = savedGame.Players.Where(p => p.PlayerScore >= savedGame.ScoreLimit).ToList();
        Assert.Single(winnersAfter);
        Assert.Equal(p1.ID, winnersAfter[0].ID);
    }

    [Fact]
    public void ScoreLimit_IncreasedAboveCurrentScore_RevertsToActivePlay()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice };
        var p2 = new TestPlayerModel { PlayerName = Bob };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 520 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 400 });

        var playedGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        // User edits game: raises limit to 1000 and sets status to In-Progress
        var vm = new MockEditGameViewModelState(playedGame);
        vm.ScoreLimit = 1000;
        vm.ChangeStatus("In-Progress");
        var activeGame = (TestCurrentGameModel)vm.CreateFinalGameToSave();

        Assert.True(activeGame.IsGameActive);
        Assert.False(activeGame.IsGameFinished);
        Assert.Equal(1000, activeGame.ScoreLimit);

        var winners = activeGame.Players.Where(p => p.PlayerScore >= activeGame.ScoreLimit).ToList();
        Assert.Empty(winners); // No winner yet under 1000 limit
    }

    // =========================================================================
    // SECTION 5: DISK PERSISTENCE SERIALIZATION / DESERIALIZATION INTEGRITY
    // =========================================================================

    [Fact]
    public void DiskPersistence_PolymorphicSerialization_RoundTripsCurrentAndPlayedGameModels()
    {
        var p1 = new TestPlayerModel { PlayerName = Alice, IsDealer = true };
        var p2 = new TestPlayerModel { PlayerName = Bob, IsDealer = false };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 150 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 120 });

        TestGameModel activeGame = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            IsGameActive = true,
            IsGameFinished = false
        };

        TestGameModel playedGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            IsGameActive = false,
            IsGameFinished = true,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        // Serialize both
        var activeJson = JsonSerializer.Serialize(activeGame);
        var playedJson = JsonSerializer.Serialize(playedGame);

        // Deserializing as base polymorphic GameModel
        var deserializedActive = JsonSerializer.Deserialize<TestGameModel>(activeJson);
        var deserializedPlayed = JsonSerializer.Deserialize<TestGameModel>(playedJson);

        Assert.NotNull(deserializedActive);
        Assert.IsType<TestCurrentGameModel>(deserializedActive);
        Assert.True(deserializedActive.IsGameActive);
        Assert.False(deserializedActive.IsGameFinished);
        Assert.Equal(500, ((TestCurrentGameModel)deserializedActive).ScoreLimit);
        Assert.Equal(2, deserializedActive.Players.Count);
        Assert.Single(deserializedActive.Round);
        Assert.Equal(150, deserializedActive.Round[0].RoundScores[0].Score);

        Assert.NotNull(deserializedPlayed);
        Assert.IsType<TestPlayedGameModel>(deserializedPlayed);
        Assert.False(deserializedPlayed.IsGameActive);
        Assert.True(deserializedPlayed.IsGameFinished);
        var playedCast = (TestPlayedGameModel)deserializedPlayed;
        Assert.Equal(TestGameStatus.Won, playedCast.GameState);
        Assert.Equal(p1.ID, playedCast.WinningPlayer?.ID);
        Assert.Equal(Alice, playedCast.WinningPlayer?.PlayerName);
    }

    // =========================================================================
    // SECTION 6: GLOBAL PLAYER STATS & RANKING SYNCHRONIZATION
    // =========================================================================

    [Fact]
    public void GlobalStats_MultiGameComplexAggregation_AccuratelySynchronizesTotalsAndRanks()
    {
        var aliceId = Guid.NewGuid();
        var bobId = Guid.NewGuid();
        var charlieId = Guid.NewGuid();
        var dianaId = Guid.NewGuid();

        // Game 1: Alice Won (500), Bob Lost (400), Charlie Lost (300), Diana Lost (200)
        var a1 = new TestPlayerModel { ID = aliceId, PlayerName = Alice, PlayerScore = 500 };
        var b1 = new TestPlayerModel { ID = bobId, PlayerName = Bob, PlayerScore = 400 };
        var c1 = new TestPlayerModel { ID = charlieId, PlayerName = Charlie, PlayerScore = 300 };
        var d1 = new TestPlayerModel { ID = dianaId, PlayerName = Diana, PlayerScore = 200 };
        var g1 = new TestPlayedGameModel { Players = [a1, b1, c1, d1], GameState = TestGameStatus.Won, WinningPlayer = a1 };

        // Game 2: Charlie Won (600), Bob Lost (500), Alice Lost (400), Diana Lost (100)
        var a2 = new TestPlayerModel { ID = aliceId, PlayerName = Alice, PlayerScore = 400 };
        var b2 = new TestPlayerModel { ID = bobId, PlayerName = Bob, PlayerScore = 500 };
        var c2 = new TestPlayerModel { ID = charlieId, PlayerName = Charlie, PlayerScore = 600 };
        var d2 = new TestPlayerModel { ID = dianaId, PlayerName = Diana, PlayerScore = 100 };
        var g2 = new TestPlayedGameModel { Players = [a2, b2, c2, d2], GameState = TestGameStatus.Won, WinningPlayer = c2 };

        // Game 3: Bob & Diana Draw (500 each)
        var b3 = new TestPlayerModel { ID = bobId, PlayerName = Bob, PlayerScore = 500 };
        var d3 = new TestPlayerModel { ID = dianaId, PlayerName = Diana, PlayerScore = 500 };
        var g3 = new TestPlayedGameModel { Players = [b3, d3], GameState = TestGameStatus.Draw, WinningPlayer = null };

        // Game 4: Alice & Diana Forfeit (0 score added)
        var a4 = new TestPlayerModel { ID = aliceId, PlayerName = Alice, PlayerScore = 150 };
        var d4 = new TestPlayerModel { ID = dianaId, PlayerName = Diana, PlayerScore = 100 };
        var g4 = new TestPlayedGameModel { Players = [a4, d4], GameState = TestGameStatus.Forfeit, WinningPlayer = null };

        // Game 5: Bob & Charlie Active In-Progress (0 stats)
        var b5 = new TestPlayerModel { ID = bobId, PlayerName = Bob, PlayerScore = 200 };
        var c5 = new TestPlayerModel { ID = charlieId, PlayerName = Charlie, PlayerScore = 250 };
        var g5 = new TestCurrentGameModel { Players = [b5, c5], IsGameActive = true, IsGameFinished = false };

        var stats = TestGameRecomputationEngine.AggregatePlayerStats([g1, g2, g3, g4, g5]);

        // Totals Check:
        // Alice: 3 played games (G1, G2, G4). Score: 500+400=900 (G4 forfeit adds 0). Wins: 1, Losses: 1, Draws: 0, Forfeits: 1.
        Assert.Equal(3, stats[aliceId].TotalGamesPlayed);
        Assert.Equal(900, stats[aliceId].LifetimeScore);
        Assert.Equal(1, stats[aliceId].GamesWon);
        Assert.Equal(1, stats[aliceId].GamesLost);
        Assert.Equal(0, stats[aliceId].GameDraws);
        Assert.Equal(1, stats[aliceId].GamesForfeit);

        // Bob: 3 played games (G1, G2, G3). Score: 400+500+500=1400. Wins: 0, Losses: 2, Draws: 1, Forfeits: 0.
        Assert.Equal(3, stats[bobId].TotalGamesPlayed);
        Assert.Equal(1400, stats[bobId].LifetimeScore);
        Assert.Equal(0, stats[bobId].GamesWon);
        Assert.Equal(2, stats[bobId].GamesLost);
        Assert.Equal(1, stats[bobId].GameDraws);

        // Charlie: 2 played games (G1, G2). Score: 300+600=900. Wins: 1, Losses: 1, Draws: 0, Forfeits: 0.
        Assert.Equal(2, stats[charlieId].TotalGamesPlayed);
        Assert.Equal(900, stats[charlieId].LifetimeScore);
        Assert.Equal(1, stats[charlieId].GamesWon);
        Assert.Equal(1, stats[charlieId].GamesLost);

        // Diana: 3 played games (G1, G2, G3, G4 = 4 played games). Score: 200+100+500=800. Wins: 0, Losses: 2, Draws: 1, Forfeits: 1.
        Assert.Equal(4, stats[dianaId].TotalGamesPlayed);
        Assert.Equal(800, stats[dianaId].LifetimeScore);
        Assert.Equal(0, stats[dianaId].GamesWon);
        Assert.Equal(2, stats[dianaId].GamesLost);
        Assert.Equal(1, stats[dianaId].GameDraws);
        Assert.Equal(1, stats[dianaId].GamesForfeit);

        // Rankings:
        // Rank 1: Bob (1400 pts)
        // Rank 2: Alice (900 pts, 1 Win, 'Alice' < 'Charlie')
        // Rank 3: Charlie (900 pts, 1 Win)
        // Rank 4: Diana (800 pts)
        var ranked = stats.Values
            .OrderByDescending(p => p.LifetimeScore)
            .ThenByDescending(p => p.GamesWon)
            .ThenBy(p => p.PlayerName)
            .ToList();

        Assert.Equal(bobId, ranked[0].ID);
        Assert.Equal(aliceId, ranked[1].ID);
        Assert.Equal(charlieId, ranked[2].ID);
        Assert.Equal(dianaId, ranked[3].ID);
    }
}
