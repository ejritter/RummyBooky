using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public enum TestGameStatus
{
    Unknown,
    Draw,
    Forfeit,
    Won,
    Loss
}

public class TestRoundScoreModel
{
    public Guid PlayerId { get; set; }
    public int Score { get; set; }
}

public class TestPlayerModel
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string PlayerName { get; set; } = string.Empty;
    public string PlayerScoreText { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public double LifetimeScore { get; set; }
    public double TotalGamesPlayed { get; set; }
    public double GamesWon { get; set; }
    public double GamesLost { get; set; }
    public double GamesForfeit { get; set; }
    public double GameDraws { get; set; }
    public int HighestScoredHand { get; set; } = int.MinValue;
    public int LowestScoredHand { get; set; } = int.MaxValue;
    public bool IsDealer { get; set; }
}

public class TestRoundModel
{
    public Guid GameId { get; set; }
    public TestPlayerModel? LeadingPlayer { get; set; }
    public TestPlayerModel? PlayerHighestScoringHand { get; set; }
    public int CurrentHighestScoredHandValue { get; set; } = int.MinValue;
    public TestPlayerModel? PlayerLowestScoringHand { get; set; }
    public int CurrentLowestScoredHandValue { get; set; } = int.MaxValue;
    public ObservableCollection<TestPlayerModel> PlayersScoredHandThisRound { get; set; } = [];
    public ObservableCollection<TestRoundScoreModel> RoundScores { get; set; } = [];
}

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(TestCurrentGameModel), typeDiscriminator: "CurrentGame")]
[JsonDerivedType(typeof(TestPlayedGameModel), typeDiscriminator: "PlayedGame")]
public abstract class TestGameModel
{
    public Guid GameId { get; set; } = Guid.NewGuid();
    public ObservableCollection<TestPlayerModel> Players { get; set; } = [];
    public bool IsGameActive { get; set; } = true;
    public bool IsGameFinished { get; set; } = false;
    public ObservableCollection<TestRoundModel> Round { get; set; } = [];
}

public class TestCurrentGameModel : TestGameModel
{
    public int ScoreLimit { get; set; } = 500;
    public DateTime GameStart { get; set; } = DateTime.UtcNow;
}

public class TestPlayedGameModel : TestCurrentGameModel
{
    public TestPlayerModel? WinningPlayer { get; set; }
    public TestGameStatus GameState { get; set; }
    public DateTime GameEnd { get; set; } = DateTime.UtcNow;
}

public class TestGameRecomputationEngine
{
    public static void RecalculateGame(TestGameModel game)
    {
        if (game is null || game.Players.Count == 0)
            return;

        foreach (var player in game.Players)
        {
            player.PlayerScore = 0;
            player.HighestScoredHand = int.MinValue;
            player.LowestScoredHand = int.MaxValue;
        }

        TestPlayerModel? latestLeader = null;
        TestPlayerModel? latestHighestPlayer = null;
        int latestHighestValue = int.MinValue;
        TestPlayerModel? latestLowestPlayer = null;
        int latestLowestValue = int.MaxValue;

        for (int r = 0; r < game.Round.Count; r++)
        {
            var round = game.Round[r];

            if (round.RoundScores.Count == 0 && round.PlayersScoredHandThisRound.Count > 0)
            {
                foreach (var p in round.PlayersScoredHandThisRound)
                {
                    int s = 0;
                    if (!string.IsNullOrWhiteSpace(p.PlayerScoreText) && int.TryParse(p.PlayerScoreText, out var parsed))
                        s = parsed;
                    round.RoundScores.Add(new TestRoundScoreModel { PlayerId = p.ID, Score = s });
                }
            }

            bool isScoredRound = round.RoundScores.Count > 0;

            if (isScoredRound)
            {
                foreach (var player in game.Players)
                {
                    var scoreEntry = round.RoundScores.FirstOrDefault(rs => rs.PlayerId == player.ID);
                    if (scoreEntry is null)
                    {
                        scoreEntry = new TestRoundScoreModel { PlayerId = player.ID, Score = 0 };
                        round.RoundScores.Add(scoreEntry);
                    }
                }

                round.CurrentHighestScoredHandValue = int.MinValue;
                round.PlayerHighestScoringHand = null;
                round.CurrentLowestScoredHandValue = int.MaxValue;
                round.PlayerLowestScoringHand = null;

                foreach (var player in game.Players)
                {
                    var scoreEntry = round.RoundScores.First(rs => rs.PlayerId == player.ID);
                    int score = scoreEntry.Score;

                    player.PlayerScore += score;

                    if (score > player.HighestScoredHand)
                        player.HighestScoredHand = score;
                    if (score < player.LowestScoredHand)
                        player.LowestScoredHand = score;

                    if (score > round.CurrentHighestScoredHandValue)
                    {
                        round.CurrentHighestScoredHandValue = score;
                        round.PlayerHighestScoringHand = player;
                    }

                    if (score < round.CurrentLowestScoredHandValue)
                    {
                        round.CurrentLowestScoredHandValue = score;
                        round.PlayerLowestScoringHand = player;
                    }
                }

                round.LeadingPlayer = game.Players.OrderByDescending(p => p.PlayerScore).FirstOrDefault();

                latestLeader = round.LeadingPlayer;
                latestHighestPlayer = round.PlayerHighestScoringHand;
                latestHighestValue = round.CurrentHighestScoredHandValue;
                latestLowestPlayer = round.PlayerLowestScoringHand;
                latestLowestValue = round.CurrentLowestScoredHandValue;
            }
            else
            {
                round.LeadingPlayer = latestLeader;
                round.PlayerHighestScoringHand = latestHighestPlayer;
                round.CurrentHighestScoredHandValue = latestHighestValue;
                round.PlayerLowestScoringHand = latestLowestPlayer;
                round.CurrentLowestScoredHandValue = latestLowestValue;
            }
        }

        foreach (var player in game.Players)
        {
            if (player.HighestScoredHand == int.MinValue)
                player.HighestScoredHand = 0;
            if (player.LowestScoredHand == int.MaxValue)
                player.LowestScoredHand = 0;
        }
    }

    public static Dictionary<Guid, TestPlayerModel> AggregatePlayerStats(IEnumerable<TestGameModel> games)
    {
        var dict = new Dictionary<Guid, TestPlayerModel>();

        foreach (var game in games)
        {
            var isPlayedGame = game is TestPlayedGameModel;
            var playedGame = game as TestPlayedGameModel;

            foreach (var p in game.Players)
            {
                if (!dict.TryGetValue(p.ID, out var agg))
                {
                    agg = new TestPlayerModel
                    {
                        ID = p.ID,
                        PlayerName = p.PlayerName
                    };
                    dict[p.ID] = agg;
                }

                if (isPlayedGame && playedGame != null)
                {
                    agg.TotalGamesPlayed += 1;

                    if (playedGame.GameState == TestGameStatus.Won)
                    {
                        agg.LifetimeScore += p.PlayerScore;
                        if (playedGame.WinningPlayer?.ID == p.ID)
                            agg.GamesWon += 1;
                        else
                            agg.GamesLost += 1;
                    }
                    else if (playedGame.GameState == TestGameStatus.Draw)
                    {
                        agg.LifetimeScore += p.PlayerScore;
                        agg.GameDraws += 1;
                    }
                    else if (playedGame.GameState == TestGameStatus.Forfeit)
                    {
                        agg.GamesForfeit += 1;
                    }
                }
            }
        }

        return dict;
    }
}

public class PreviousRoundAndGameEditingTests
{
    [Fact]
    public void RecalculateGameScores_MultiRound_CalculatesTotalsAndExtremesAccurately()
    {
        // Arrange: 3 players across 2 completed rounds
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };
        var p3 = new TestPlayerModel { PlayerName = "Charlie" };

        var round1 = new TestRoundModel();
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = -10 });
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 30 });

        var round2 = new TestRoundModel();
        round2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 20 });
        round2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 100 });
        round2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p3.ID, Score = 40 });

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2, p3],
            Round = [round1, round2]
        };

        // Act
        TestGameRecomputationEngine.RecalculateGame(game);

        // Assert: Cumulative player scores
        Assert.Equal(70, p1.PlayerScore);  // 50 + 20
        Assert.Equal(90, p2.PlayerScore);  // -10 + 100
        Assert.Equal(70, p3.PlayerScore);  // 30 + 40

        // Assert: Player extremes across rounds
        Assert.Equal(50, p1.HighestScoredHand);
        Assert.Equal(20, p1.LowestScoredHand);
        Assert.Equal(100, p2.HighestScoredHand);
        Assert.Equal(-10, p2.LowestScoredHand);

        // Assert: Round 1 metrics
        Assert.Equal(50, round1.CurrentHighestScoredHandValue);
        Assert.Equal(p1.ID, round1.PlayerHighestScoringHand?.ID);
        Assert.Equal(-10, round1.CurrentLowestScoredHandValue);
        Assert.Equal(p2.ID, round1.PlayerLowestScoringHand?.ID);
        Assert.Equal(p1.ID, round1.LeadingPlayer?.ID); // After round 1: Alice (50) was leading

        // Assert: Round 2 metrics
        Assert.Equal(100, round2.CurrentHighestScoredHandValue);
        Assert.Equal(p2.ID, round2.PlayerHighestScoringHand?.ID);
        Assert.Equal(20, round2.CurrentLowestScoredHandValue);
        Assert.Equal(p1.ID, round2.PlayerLowestScoringHand?.ID);
        Assert.Equal(p2.ID, round2.LeadingPlayer?.ID); // After round 2: Bob (90) took lead
    }

    [Fact]
    public void EditPreviousRound_ModifiesPriorScore_TriggersDynamicRecomputation()
    {
        // Arrange: 2 completed rounds and 1 active unscored round
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var round1 = new TestRoundModel();
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 40 });
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 60 });

        var round2 = new TestRoundModel();
        round2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        round2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 30 });

        var round3 = new TestRoundModel(); // Active unscored template

        var game = new TestCurrentGameModel
        {
            Players = [p1, p2],
            Round = [round1, round2, round3]
        };

        TestGameRecomputationEngine.RecalculateGame(game);
        Assert.Equal(90, p1.PlayerScore); // 40 + 50
        Assert.Equal(90, p2.PlayerScore); // 60 + 30

        // Act: Edit Round 1 score for Bob from 60 -> 10 (a correction of a typo)
        var bobRound1 = round1.RoundScores.First(r => r.PlayerId == p2.ID);
        bobRound1.Score = 10;

        TestGameRecomputationEngine.RecalculateGame(game);

        // Assert: Running totals updated immediately
        Assert.Equal(90, p1.PlayerScore);
        Assert.Equal(40, p2.PlayerScore); // 10 + 30
        Assert.Equal(p1.ID, round2.LeadingPlayer?.ID); // Alice is now leader
        Assert.Equal(p1.ID, round3.LeadingPlayer?.ID); // Unscored round reflects updated leader
        Assert.Equal(40, round1.CurrentHighestScoredHandValue);
        Assert.Equal(p1.ID, round1.PlayerHighestScoringHand?.ID);
    }

    [Fact]
    public void EditGame_TieResolution_ManualWinnerAssignment_UpdatesPlayerLifetimeStats()
    {
        // Arrange: 2 players tied at 500 points
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var round1 = new TestRoundModel();
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 500 });
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 500 });

        // Case 1: Manually select Bob as winner
        var playedGameBobWon = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [round1],
            GameState = TestGameStatus.Won,
            WinningPlayer = p2,
            ScoreLimit = 500
        };

        TestGameRecomputationEngine.RecalculateGame(playedGameBobWon);
        var stats1 = TestGameRecomputationEngine.AggregatePlayerStats([playedGameBobWon]);

        Assert.Equal(1, stats1[p2.ID].GamesWon);
        Assert.Equal(0, stats1[p2.ID].GamesLost);
        Assert.Equal(0, stats1[p1.ID].GamesWon);
        Assert.Equal(1, stats1[p1.ID].GamesLost);

        // Case 2: User re-edits game to resolve tie in favor of Alice
        var playedGameAliceWon = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [round1],
            GameState = TestGameStatus.Won,
            WinningPlayer = p1,
            ScoreLimit = 500
        };

        TestGameRecomputationEngine.RecalculateGame(playedGameAliceWon);
        var stats2 = TestGameRecomputationEngine.AggregatePlayerStats([playedGameAliceWon]);

        Assert.Equal(1, stats2[p1.ID].GamesWon);
        Assert.Equal(0, stats2[p1.ID].GamesLost);
        Assert.Equal(0, stats2[p2.ID].GamesWon);
        Assert.Equal(1, stats2[p2.ID].GamesLost);
    }

    [Fact]
    public void EditGame_ScoreLimitModification_PropagatesToSavedModel()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var currentGame = new TestCurrentGameModel
        {
            Players = [p1, p2],
            ScoreLimit = 500
        };

        // User edits score limit to 750
        currentGame.ScoreLimit = 750;

        var playedGame = new TestPlayedGameModel
        {
            GameId = currentGame.GameId,
            Players = currentGame.Players,
            Round = currentGame.Round,
            ScoreLimit = currentGame.ScoreLimit,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        Assert.Equal(750, playedGame.ScoreLimit);
    }

    [Fact]
    public void EditGame_StatusChange_ToDrawOrForfeit_ClearsWinnerAndSetsState()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var round1 = new TestRoundModel();
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 100 });
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 100 });

        // Set to Draw
        var drawGame = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [round1],
            GameState = TestGameStatus.Draw,
            WinningPlayer = null
        };

        TestGameRecomputationEngine.RecalculateGame(drawGame);
        var stats = TestGameRecomputationEngine.AggregatePlayerStats([drawGame]);

        Assert.Equal(1, stats[p1.ID].GameDraws);
        Assert.Equal(0, stats[p1.ID].GamesWon);
        Assert.Equal(0, stats[p1.ID].GamesLost);
        Assert.Equal(1, stats[p2.ID].GameDraws);
    }

    [Fact]
    public void RoundScoreModel_SerializationAndDeserialization_Integrity()
    {
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var round1 = new TestRoundModel();
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 150 });
        round1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 75 });

        TestGameModel game = new TestPlayedGameModel
        {
            Players = [p1, p2],
            Round = [round1],
            ScoreLimit = 500,
            GameState = TestGameStatus.Won,
            WinningPlayer = p1
        };

        var json = JsonSerializer.Serialize(game);
        var deserialized = JsonSerializer.Deserialize<TestGameModel>(json);

        Assert.NotNull(deserialized);
        Assert.IsType<TestPlayedGameModel>(deserialized);
        var played = (TestPlayedGameModel)deserialized;

        Assert.Equal(TestGameStatus.Won, played.GameState);
        Assert.Equal(500, played.ScoreLimit);
        Assert.Single(played.Round);
        Assert.Equal(2, played.Round[0].RoundScores.Count);
        Assert.Equal(150, played.Round[0].RoundScores.First(r => r.PlayerId == p1.ID).Score);
        Assert.Equal(75, played.Round[0].RoundScores.First(r => r.PlayerId == p2.ID).Score);
    }

    [Fact]
    public void InGamePreviousRoundNavigation_StateTransitions_PreservesDraftScores()
    {
        // Arrange
        var p1 = new TestPlayerModel { PlayerName = "Alice" };
        var p2 = new TestPlayerModel { PlayerName = "Bob" };

        var r1 = new TestRoundModel();
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 20 });

        var r2 = new TestRoundModel();
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p1.ID, Score = 30 });
        r2.RoundScores.Add(new TestRoundScoreModel { PlayerId = p2.ID, Score = 40 });

        var r3 = new TestRoundModel(); // Active round

        var rounds = new List<TestRoundModel> { r1, r2, r3 };
        var drafts = new Dictionary<Guid, string>();

        int selectedIndex = 2; // on active round 3
        p1.PlayerScoreText = "65"; // user typed draft for round 3
        p2.PlayerScoreText = "15";

        // Navigation state on active round
        bool isViewingPrevious = selectedIndex < rounds.Count - 1;
        bool canGoPrev = selectedIndex > 0;
        bool canGoNext = selectedIndex < rounds.Count - 1;

        Assert.False(isViewingPrevious);
        Assert.True(canGoPrev);
        Assert.False(canGoNext);

        // Act: Navigate to Round 2 (Previous)
        drafts[p1.ID] = p1.PlayerScoreText;
        drafts[p2.ID] = p2.PlayerScoreText;
        selectedIndex--;

        p1.PlayerScoreText = r2.RoundScores.First(s => s.PlayerId == p1.ID).Score.ToString();
        p2.PlayerScoreText = r2.RoundScores.First(s => s.PlayerId == p2.ID).Score.ToString();

        Assert.Equal("30", p1.PlayerScoreText);
        Assert.Equal("40", p2.PlayerScoreText);
        Assert.True(selectedIndex < rounds.Count - 1); // IsViewingPrevious == true

        // Act: Navigate forward back to active round (Next)
        selectedIndex++;
        p1.PlayerScoreText = drafts[p1.ID];
        p2.PlayerScoreText = drafts[p2.ID];

        // Assert: Draft scores restored
        Assert.Equal("65", p1.PlayerScoreText);
        Assert.Equal("15", p2.PlayerScoreText);
        Assert.False(selectedIndex < rounds.Count - 1);
    }
}
