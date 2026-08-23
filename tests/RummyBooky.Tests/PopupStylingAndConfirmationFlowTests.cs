using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class PopupStylingAndConfirmationFlowTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
        public int PlayerScore { get; set; }
    }

    public class RoundScoreStub
    {
        public Guid PlayerId { get; set; }
        public int Score { get; set; }
    }

    public class RoundStub
    {
        public ObservableCollection<RoundScoreStub> RoundScores { get; set; } = [];
    }

    public class GameStub
    {
        public Guid GameId { get; init; } = Guid.NewGuid();
        public ObservableCollection<PlayerStub> Players { get; set; } = [];
        public ObservableCollection<RoundStub> Round { get; set; } = [];
        public int ScoreLimit { get; set; } = 500;
        public string GameState { get; set; } = "In-Progress";
        public PlayerStub? WinningPlayer { get; set; }
    }

    public class PopupPromptRecord
    {
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool? ShowOkay { get; set; }
        public bool? ShowCancel { get; set; }
        public bool? ShowQuit { get; set; }
        public string? OkayText { get; set; }
        public string? CancelText { get; set; }
        public bool ReturnConfirmed { get; set; } = true;
    }

    public class MockPopupEngine
    {
        public List<PopupPromptRecord> RecordedPopups { get; } = [];
        public bool NextResultConfirmed { get; set; } = true;

        public Task<bool> ShowPopupAsync(
            string title,
            string message,
            bool isDismissable = true,
            bool? showOkay = null,
            bool? showCancel = null,
            bool? showQuit = null,
            string? okayText = null,
            string? cancelText = null)
        {
            RecordedPopups.Add(new PopupPromptRecord
            {
                Title = title,
                Message = message,
                ShowOkay = showOkay,
                ShowCancel = showCancel,
                ShowQuit = showQuit,
                OkayText = okayText,
                CancelText = cancelText,
                ReturnConfirmed = NextResultConfirmed
            });

            return Task.FromResult(NextResultConfirmed);
        }
    }

    public class EditPlayerFlowSimulator
    {
        private readonly MockPopupEngine _popupEngine;
        public PlayerStub? CurrentPlayer { get; set; }
        public string NewPlayerNameText { get; set; } = string.Empty;
        public bool HistoryUpdated { get; private set; }

        public EditPlayerFlowSimulator(MockPopupEngine popupEngine)
        {
            _popupEngine = popupEngine;
        }

        public async Task<bool> UpdatePlayerNameAsync()
        {
            if (CurrentPlayer is null || string.IsNullOrWhiteSpace(NewPlayerNameText))
                return false;

            var newName = NewPlayerNameText.Trim();
            if (string.Equals(newName, CurrentPlayer.PlayerName, StringComparison.Ordinal))
                return false;

            var oldName = CurrentPlayer.PlayerName;
            var confirmMessage = $"Player name will change from \"{oldName}\" to \"{newName}\". Are you sure you want to continue?";
            var confirmed = await _popupEngine.ShowPopupAsync(
                title: "Confirm Name Change",
                message: confirmMessage,
                isDismissable: false,
                showOkay: true,
                showCancel: true,
                showQuit: false,
                okayText: "Confirm",
                cancelText: "Cancel");

            if (!confirmed)
                return false;

            CurrentPlayer.PlayerName = newName;
            NewPlayerNameText = string.Empty;
            HistoryUpdated = true;

            await _popupEngine.ShowPopupAsync(
                title: "Success",
                message: $"Player '{oldName}' updated to '{newName}'.",
                isDismissable: true,
                showOkay: true,
                showCancel: false,
                showQuit: false,
                okayText: "Okay");

            return true;
        }
    }

    public class EditGameFlowSimulator
    {
        private readonly MockPopupEngine _popupEngine;
        public GameStub? Game { get; private set; }
        public int ScoreLimit { get; set; }
        public string SelectedStatus { get; set; } = "In-Progress";
        public PlayerStub? SelectedWinner { get; set; }
        public Dictionary<(int roundIndex, Guid playerId), int> RoundScoresUI { get; } = [];

        private int _initialScoreLimit;
        private string _initialStatus = "In-Progress";
        private string? _initialWinnerName;
        private readonly Dictionary<(int roundIndex, Guid playerId), int> _initialRoundScores = [];

        public bool GamePersisted { get; private set; }

        public EditGameFlowSimulator(MockPopupEngine popupEngine)
        {
            _popupEngine = popupEngine;
        }

        public void LoadGame(GameStub game)
        {
            Game = game;
            ScoreLimit = game.ScoreLimit;
            _initialScoreLimit = game.ScoreLimit;
            SelectedStatus = game.GameState;
            _initialStatus = game.GameState;
            SelectedWinner = game.WinningPlayer;
            _initialWinnerName = game.WinningPlayer?.PlayerName;

            _initialRoundScores.Clear();
            RoundScoresUI.Clear();

            for (int r = 0; r < game.Round.Count; r++)
            {
                var round = game.Round[r];
                foreach (var p in game.Players)
                {
                    var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == p.ID);
                    int score = rs?.Score ?? 0;
                    _initialRoundScores[(r, p.ID)] = score;
                    RoundScoresUI[(r, p.ID)] = score;
                }
            }
        }

        public List<string> GetDetectedChanges()
        {
            var changes = new List<string>();

            if (ScoreLimit != _initialScoreLimit)
            {
                changes.Add($"Score Limit: {_initialScoreLimit} ➔ {ScoreLimit}");
            }

            if (!string.Equals(SelectedStatus, _initialStatus, StringComparison.OrdinalIgnoreCase))
            {
                changes.Add($"Game Status: {_initialStatus} ➔ {SelectedStatus}");
            }

            if (SelectedStatus == "Won")
            {
                var currentWinnerName = SelectedWinner?.PlayerName ?? "None";
                var origWinnerName = _initialWinnerName ?? "None";
                if (!string.Equals(currentWinnerName, origWinnerName, StringComparison.Ordinal) || !string.Equals(_initialStatus, "Won", StringComparison.OrdinalIgnoreCase))
                {
                    changes.Add($"Winner: {origWinnerName} ➔ {currentWinnerName}");
                }
            }
            else if (!string.Equals(SelectedStatus, "Won", StringComparison.OrdinalIgnoreCase) && string.Equals(_initialStatus, "Won", StringComparison.OrdinalIgnoreCase))
            {
                changes.Add($"Winner: {_initialWinnerName ?? "None"} ➔ None");
            }

            if (Game != null)
            {
                for (int r = 0; r < Game.Round.Count; r++)
                {
                    foreach (var p in Game.Players)
                    {
                        RoundScoresUI.TryGetValue((r, p.ID), out int currentScore);
                        _initialRoundScores.TryGetValue((r, p.ID), out int initialScore);

                        if (currentScore != initialScore)
                        {
                            changes.Add($"Round {r + 1} ({p.PlayerName}): {initialScore} ➔ {currentScore}");
                        }
                    }
                }
            }

            return changes;
        }

        public string GenerateConfirmationMessage()
        {
            var changes = GetDetectedChanges();
            var sb = new System.Text.StringBuilder();

            if (changes.Count == 0)
            {
                sb.AppendLine("No modifications detected.");
                sb.AppendLine();
                sb.Append("Are you sure you want to save?");
            }
            else
            {
                sb.AppendLine("The following changes will be applied:");
                foreach (var change in changes)
                {
                    sb.AppendLine($"• {change}");
                }
                sb.AppendLine();
                sb.Append("Are you sure you want to save these changes?");
            }

            return sb.ToString();
        }

        public void RevertToInitialState()
        {
            if (Game is null)
                return;

            ScoreLimit = _initialScoreLimit;
            SelectedStatus = _initialStatus;
            SelectedWinner = Game.WinningPlayer;
            RoundScoresUI.Clear();

            for (int r = 0; r < Game.Round.Count; r++)
            {
                var round = Game.Round[r];
                foreach (var p in Game.Players)
                {
                    if (_initialRoundScores.TryGetValue((r, p.ID), out int score))
                    {
                        var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == p.ID);
                        if (rs != null)
                            rs.Score = score;
                        RoundScoresUI[(r, p.ID)] = score;
                    }
                }
            }
        }

        public async Task<bool> SaveAsync()
        {
            if (Game is null)
                return false;

            var confirmMessage = GenerateConfirmationMessage();
            var confirmed = await _popupEngine.ShowPopupAsync(
                title: "Confirm Game Edits",
                message: confirmMessage,
                isDismissable: false,
                showOkay: true,
                showCancel: true,
                showQuit: false,
                okayText: "Confirm",
                cancelText: "Cancel");

            if (!confirmed)
                return false;

            // Persist
            Game.ScoreLimit = ScoreLimit;
            Game.GameState = SelectedStatus;
            Game.WinningPlayer = SelectedStatus == "Won" ? SelectedWinner : null;
            for (int r = 0; r < Game.Round.Count; r++)
            {
                var round = Game.Round[r];
                foreach (var p in Game.Players)
                {
                    if (RoundScoresUI.TryGetValue((r, p.ID), out int newScore))
                    {
                        var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == p.ID);
                        if (rs != null)
                            rs.Score = newScore;
                    }
                }
            }
            GamePersisted = true;

            await _popupEngine.ShowPopupAsync(
                title: "Success",
                message: "Game updated successfully.",
                isDismissable: true,
                showOkay: true,
                showCancel: false,
                showQuit: false,
                okayText: "Okay");

            return true;
        }
    }

    [Fact]
    public async Task EditPlayer_ConfirmationPrompt_DisplaysOldAndNewPlayerNames()
    {
        var popupMock = new MockPopupEngine { NextResultConfirmed = true };
        var player = new PlayerStub { PlayerName = "Brodie" };
        var simulator = new EditPlayerFlowSimulator(popupMock)
        {
            CurrentPlayer = player,
            NewPlayerNameText = "Brodie the Cowboy"
        };

        var result = await simulator.UpdatePlayerNameAsync();

        Assert.True(result);
        Assert.Equal(2, popupMock.RecordedPopups.Count);

        var confirmPopup = popupMock.RecordedPopups[0];
        Assert.Equal("Confirm Name Change", confirmPopup.Title);
        Assert.Contains("Brodie", confirmPopup.Message);
        Assert.Contains("Brodie the Cowboy", confirmPopup.Message);
        Assert.True(confirmPopup.ShowOkay);
        Assert.True(confirmPopup.ShowCancel);
        Assert.False(confirmPopup.ShowQuit);
        Assert.Equal("Confirm", confirmPopup.OkayText);
        Assert.Equal("Cancel", confirmPopup.CancelText);

        var successPopup = popupMock.RecordedPopups[1];
        Assert.Equal("Success", successPopup.Title);
        Assert.True(successPopup.ShowOkay);
        Assert.False(successPopup.ShowCancel);
        Assert.False(successPopup.ShowQuit);
        Assert.Equal("Okay", successPopup.OkayText);
    }

    [Fact]
    public async Task EditPlayer_CancelConfirmation_DoesNotMutateNameOrUpdateHistory()
    {
        var popupMock = new MockPopupEngine { NextResultConfirmed = false };
        var player = new PlayerStub { PlayerName = "Renegade" };
        var simulator = new EditPlayerFlowSimulator(popupMock)
        {
            CurrentPlayer = player,
            NewPlayerNameText = "Renegade AI"
        };

        var result = await simulator.UpdatePlayerNameAsync();

        Assert.False(result);
        Assert.Equal("Renegade", player.PlayerName); // Not mutated!
        Assert.False(simulator.HistoryUpdated);
        Assert.Single(popupMock.RecordedPopups);
        Assert.Equal("Confirm Name Change", popupMock.RecordedPopups[0].Title);
    }

    [Fact]
    public void EditGame_ChangeDetection_IdentifiesScoreLimitStatusWinnerAndRoundScores()
    {
        var popupMock = new MockPopupEngine();
        var p1 = new PlayerStub { PlayerName = "Alice" };
        var p2 = new PlayerStub { PlayerName = "Bob" };

        var r1 = new RoundStub();
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 60 });

        var r2 = new RoundStub();
        r2.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 40 });
        r2.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 30 });

        var game = new GameStub
        {
            Players = [p1, p2],
            Round = [r1, r2],
            ScoreLimit = 500,
            GameState = "In-Progress"
        };

        var simulator = new EditGameFlowSimulator(popupMock);
        simulator.LoadGame(game);

        // Before any change: no detected changes
        Assert.Empty(simulator.GetDetectedChanges());

        // Modify ScoreLimit, Status, Winner, and Round 2 score for Alice
        simulator.ScoreLimit = 300;
        simulator.SelectedStatus = "Won";
        simulator.SelectedWinner = p1;
        simulator.RoundScoresUI[(1, p1.ID)] = 90; // 40 -> 90

        var changes = simulator.GetDetectedChanges();
        Assert.Equal(4, changes.Count);
        Assert.Contains("Score Limit: 500 ➔ 300", changes);
        Assert.Contains("Game Status: In-Progress ➔ Won", changes);
        Assert.Contains("Winner: None ➔ Alice", changes);
        Assert.Contains("Round 2 (Alice): 40 ➔ 90", changes);

        var confirmMessage = simulator.GenerateConfirmationMessage();
        Assert.Contains("The following changes will be applied:", confirmMessage);
        Assert.Contains("• Score Limit: 500 ➔ 300", confirmMessage);
        Assert.Contains("• Game Status: In-Progress ➔ Won", confirmMessage);
        Assert.Contains("• Winner: None ➔ Alice", confirmMessage);
        Assert.Contains("• Round 2 (Alice): 40 ➔ 90", confirmMessage);
        Assert.Contains("Are you sure you want to save these changes?", confirmMessage);
    }

    [Fact]
    public async Task EditGame_Save_CancelConfirmation_AbortsWithoutPersistingChanges()
    {
        var popupMock = new MockPopupEngine { NextResultConfirmed = false };
        var p1 = new PlayerStub { PlayerName = "Alice" };
        var p2 = new PlayerStub { PlayerName = "Bob" };

        var r1 = new RoundStub();
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 60 });

        var game = new GameStub
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = "In-Progress"
        };

        var simulator = new EditGameFlowSimulator(popupMock);
        simulator.LoadGame(game);

        simulator.ScoreLimit = 250;
        simulator.RoundScoresUI[(0, p1.ID)] = 100;

        var result = await simulator.SaveAsync();

        Assert.False(result);
        Assert.False(simulator.GamePersisted);
        Assert.Equal(500, game.ScoreLimit);
        Assert.Equal(50, r1.RoundScores[0].Score);
        Assert.Single(popupMock.RecordedPopups);
        Assert.Equal("Confirm Game Edits", popupMock.RecordedPopups[0].Title);
    }

    [Fact]
    public async Task EditGame_Save_Confirmed_PersistsChangesAndDisplaysSuccessPopupWithOnlyOkay()
    {
        var popupMock = new MockPopupEngine { NextResultConfirmed = true };
        var p1 = new PlayerStub { PlayerName = "Alice" };
        var p2 = new PlayerStub { PlayerName = "Bob" };

        var r1 = new RoundStub();
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 60 });

        var game = new GameStub
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = "In-Progress"
        };

        var simulator = new EditGameFlowSimulator(popupMock);
        simulator.LoadGame(game);

        simulator.ScoreLimit = 250;
        simulator.SelectedStatus = "Won";
        simulator.SelectedWinner = p2;
        simulator.RoundScoresUI[(0, p2.ID)] = 150;

        var result = await simulator.SaveAsync();

        Assert.True(result);
        Assert.True(simulator.GamePersisted);
        Assert.Equal(250, game.ScoreLimit);
        Assert.Equal("Won", game.GameState);
        Assert.Equal(p2.ID, game.WinningPlayer?.ID);
        Assert.Equal(150, r1.RoundScores[1].Score);

        Assert.Equal(2, popupMock.RecordedPopups.Count);

        var confirmPopup = popupMock.RecordedPopups[0];
        Assert.Equal("Confirm Game Edits", confirmPopup.Title);
        Assert.True(confirmPopup.ShowOkay);
        Assert.True(confirmPopup.ShowCancel);
        Assert.False(confirmPopup.ShowQuit);

        var successPopup = popupMock.RecordedPopups[1];
        Assert.Equal("Success", successPopup.Title);
        Assert.Equal("Game updated successfully.", successPopup.Message);
        Assert.True(successPopup.ShowOkay);
        Assert.False(successPopup.ShowCancel);
        Assert.False(successPopup.ShowQuit);
        Assert.Equal("Okay", successPopup.OkayText);
    }

    [Fact]
    public void EditGame_Cancel_RevertsInPlaceRoundScoresAndScoreLimit()
    {
        var popupMock = new MockPopupEngine();
        var p1 = new PlayerStub { PlayerName = "Alice" };
        var p2 = new PlayerStub { PlayerName = "Bob" };

        var r1 = new RoundStub();
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 50 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 60 });

        var game = new GameStub
        {
            Players = [p1, p2],
            Round = [r1],
            ScoreLimit = 500,
            GameState = "In-Progress"
        };

        var simulator = new EditGameFlowSimulator(popupMock);
        simulator.LoadGame(game);

        // Edit round scores in UI
        simulator.ScoreLimit = 150;
        simulator.SelectedStatus = "Won";
        simulator.SelectedWinner = p1;
        r1.RoundScores[0].Score = 999; // Mutated in memory during typing

        // Cancel and revert
        simulator.RevertToInitialState();

        Assert.Equal(500, simulator.ScoreLimit);
        Assert.Equal("In-Progress", simulator.SelectedStatus);
        Assert.Null(simulator.SelectedWinner);
        Assert.Equal(50, r1.RoundScores[0].Score); // Cleanly reverted!
    }

    [Fact]
    public void EditGame_MultiPlayer_4Players_MultiRound_ChangeDetection()
    {
        var popupMock = new MockPopupEngine();
        var p1 = new PlayerStub { PlayerName = "P1" };
        var p2 = new PlayerStub { PlayerName = "P2" };
        var p3 = new PlayerStub { PlayerName = "P3" };
        var p4 = new PlayerStub { PlayerName = "P4" };

        var r1 = new RoundStub();
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 10 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 20 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p3.ID, Score = 30 });
        r1.RoundScores.Add(new RoundScoreStub { PlayerId = p4.ID, Score = 40 });

        var r2 = new RoundStub();
        r2.RoundScores.Add(new RoundScoreStub { PlayerId = p1.ID, Score = 15 });
        r2.RoundScores.Add(new RoundScoreStub { PlayerId = p2.ID, Score = 25 });
        r2.RoundScores.Add(new RoundScoreStub { PlayerId = p3.ID, Score = 35 });
        r2.RoundScores.Add(new RoundScoreStub { PlayerId = p4.ID, Score = 45 });

        var game = new GameStub
        {
            Players = [p1, p2, p3, p4],
            Round = [r1, r2],
            ScoreLimit = 1000,
            GameState = "In-Progress"
        };

        var simulator = new EditGameFlowSimulator(popupMock);
        simulator.LoadGame(game);

        // Edit Round 1 P3 and Round 2 P4
        simulator.RoundScoresUI[(0, p3.ID)] = 100;
        simulator.RoundScoresUI[(1, p4.ID)] = 200;

        var changes = simulator.GetDetectedChanges();
        Assert.Equal(2, changes.Count);
        Assert.Contains("Round 1 (P3): 30 ➔ 100", changes);
        Assert.Contains("Round 2 (P4): 45 ➔ 200", changes);
    }
}
