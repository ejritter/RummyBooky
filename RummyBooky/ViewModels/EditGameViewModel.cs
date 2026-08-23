using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Maui.Controls;
using RummyBooky.Models;
using RummyBooky.Pages;
using RummyBooky.Services;

namespace RummyBooky.ViewModels;

public partial class EditGameViewModel(IPopupService popupService, GameService gameService)
    : BaseViewModel(popupService, gameService), IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("Game", out var gameObj) && gameObj is GameModel gameModel)
        {
            Game = gameModel;
        }
    }

    [ObservableProperty]
    public partial GameModel? Game { get; set; }

    public bool IsSaved { get; private set; } = false;

    [ObservableProperty]
    public partial string GameTitle { get; set; } = "Edit Game";

    [ObservableProperty]
    public partial int ScoreLimit { get; set; } = 500;

    [ObservableProperty]
    public partial string SelectedStatus { get; set; } = "In-Progress";

    [ObservableProperty]
    public partial bool IsWinnerPickerVisible { get; set; } = false;

    [ObservableProperty]
    public partial PlayerModel? SelectedWinner { get; set; } = null;

    public ObservableCollection<string> StatusOptions { get; } = ["In-Progress", "Won", "Draw", "Forfeit"];

    public ObservableCollection<PlayerModel> AvailablePlayers { get; set; } = [];

    public ObservableCollection<EditRoundItemViewModel> Rounds { get; set; } = [];

    private int _initialScoreLimit = 500;
    private string _initialStatus = "In-Progress";
    private Guid? _initialWinnerId;
    private string? _initialWinnerName;
    private readonly Dictionary<(int roundIndex, Guid playerId), int> _initialRoundScores = [];

    partial void OnGameChanged(GameModel? value)
    {
        if (value is null)
            return;

        IsSaved = false;
        _gameService.RecalculateGame(value);

        AvailablePlayers.Clear();
        foreach (var p in value.Players)
        {
            AvailablePlayers.Add(p);
        }

        _initialRoundScores.Clear();
        for (int r = 0; r < value.Round.Count; r++)
        {
            var round = value.Round[r];
            foreach (var p in value.Players)
            {
                var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == p.ID);
                _initialRoundScores[(r, p.ID)] = rs?.Score ?? 0;
            }
        }

        if (value is PlayedGameModel played)
        {
            ScoreLimit = played.ScoreLimit;
            _initialScoreLimit = played.ScoreLimit;

            SelectedStatus = played.GameState.ToString();
            _initialStatus = played.GameState.ToString();

            SelectedWinner = AvailablePlayers.FirstOrDefault(p => p.ID == played.WinningPlayer?.ID);
            _initialWinnerId = played.WinningPlayer?.ID;
            _initialWinnerName = played.WinningPlayer?.PlayerName;

            IsWinnerPickerVisible = played.GameState == GameStatus.Won;
            GameTitle = $"Edit Played Game ({played.GameState})";
        }
        else if (value is CurrentGameModel current)
        {
            ScoreLimit = current.ScoreLimit;
            _initialScoreLimit = current.ScoreLimit;

            SelectedStatus = "In-Progress";
            _initialStatus = "In-Progress";

            SelectedWinner = null;
            _initialWinnerId = null;
            _initialWinnerName = null;

            IsWinnerPickerVisible = false;
            GameTitle = "Edit Active Game";
        }

        RebuildRoundsList(value);
    }

    partial void OnSelectedStatusChanged(string value)
    {
        IsWinnerPickerVisible = value == "Won";
        if (value != "Won")
        {
            SelectedWinner = null;
        }
        else if (SelectedWinner is null && AvailablePlayers.Count > 0)
        {
            SelectedWinner = AvailablePlayers.OrderByDescending(p => p.PlayerScore).FirstOrDefault();
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

        for (int r = 0; r < Rounds.Count; r++)
        {
            var roundVm = Rounds[r];
            foreach (var pScore in roundVm.PlayerScores)
            {
                int currentScore = int.TryParse(pScore.ScoreText, out var parsed) ? parsed : 0;
                _initialRoundScores.TryGetValue((r, pScore.PlayerId), out int initialScore);

                if (currentScore != initialScore)
                {
                    changes.Add($"Round {r + 1} ({pScore.PlayerName}): {initialScore} ➔ {currentScore}");
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

    private void RebuildRoundsList(GameModel game)
    {
        Rounds.Clear();

        for (int r = 0; r < game.Round.Count; r++)
        {
            var round = game.Round[r];
            var roundVm = new EditRoundItemViewModel { RoundNumber = r + 1 };

            foreach (var player in game.Players)
            {
                var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == player.ID);
                int currentScore = rs?.Score ?? 0;

                var scoreItem = new EditPlayerScoreItemViewModel(
                    player.ID,
                    player.PlayerName,
                    currentScore,
                    () => OnRoundScoreChanged(round, player.ID));

                roundVm.PlayerScores.Add(scoreItem);
            }

            Rounds.Add(roundVm);
        }
    }

    private void OnRoundScoreChanged(RoundModel round, Guid playerId)
    {
        if (Game is null)
            return;

        int roundIdx = Game.Round.IndexOf(round);
        if (roundIdx >= 0 && roundIdx < Rounds.Count)
        {
            var roundVm = Rounds[roundIdx];
            var scoreVm = roundVm.PlayerScores.FirstOrDefault(ps => ps.PlayerId == playerId);
            if (scoreVm != null && int.TryParse(scoreVm.ScoreText, out var scoreVal))
            {
                var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == playerId);
                if (rs is null)
                {
                    rs = new RoundScoreModel { PlayerId = playerId, Score = scoreVal };
                    round.RoundScores.Add(rs);
                }
                else
                {
                    rs.Score = scoreVal;
                }

                _gameService.RecalculateGame(Game);
            }
        }
    }

    [RelayCommand]
    public async Task SaveAsync()
    {
        if (Game is null)
            return;

        var confirmMessage = GenerateConfirmationMessage();
        var confirmResult = await ShowPopupAsync(
            title: "Confirm Game Edits",
            message: confirmMessage,
            isDismissable: false,
            showOkay: true,
            showCancel: true,
            showQuit: false,
            okayText: "Confirm",
            cancelText: "Cancel");

        if (!confirmResult.Confirmed)
            return;

        // Apply all score entries from UI to Round.RoundScores
        for (int r = 0; r < Rounds.Count; r++)
        {
            var roundVm = Rounds[r];
            if (r < Game.Round.Count)
            {
                var roundModel = Game.Round[r];
                foreach (var pScore in roundVm.PlayerScores)
                {
                    int score = 0;
                    if (int.TryParse(pScore.ScoreText, out var parsed))
                        score = parsed;

                    var rs = roundModel.RoundScores.FirstOrDefault(x => x.PlayerId == pScore.PlayerId);
                    if (rs is null)
                    {
                        rs = new RoundScoreModel { PlayerId = pScore.PlayerId, Score = score };
                        roundModel.RoundScores.Add(rs);
                    }
                    else
                    {
                        rs.Score = score;
                    }
                }
            }
        }

        if (Game is CurrentGameModel cgModel)
            cgModel.ScoreLimit = ScoreLimit;
        else if (Game is PlayedGameModel pgModel)
            pgModel.ScoreLimit = ScoreLimit;
        _gameService.RecalculateGame(Game);

        GameModel finalGameToSave;

        if (SelectedStatus == "In-Progress")
        {
            finalGameToSave = new CurrentGameModel
            {
                GameId = Game.GameId,
                Players = Game.Players,
                Round = Game.Round,
                IsGameActive = true,
                IsGameFinished = false,
                ScoreLimit = ScoreLimit,
                GameStart = Game is CurrentGameModel cg ? cg.GameStart : DateTime.Now
            };
        }
        else
        {
            GameStatus status = SelectedStatus switch
            {
                "Won" => GameStatus.Won,
                "Draw" => GameStatus.Draw,
                "Forfeit" => GameStatus.Forfeit,
                _ => GameStatus.Unknown
            };

            PlayerModel? winner = status == GameStatus.Won ? SelectedWinner : null;

            finalGameToSave = new PlayedGameModel
            {
                GameId = Game.GameId,
                Players = Game.Players,
                Round = Game.Round,
                IsGameActive = false,
                IsGameFinished = true,
                ScoreLimit = ScoreLimit,
                GameState = status,
                WinningPlayer = winner,
                GameStart = Game is CurrentGameModel cg ? cg.GameStart : DateTime.Now,
                GameEnd = Game is PlayedGameModel pg ? pg.GameEnd : DateTime.Now
            };
        }

        await _gameService.SaveGameAsync(finalGameToSave);
        await _gameService.LoadAllPlayersDictionaryAsync();
        IsSaved = true;

        _ = await ShowPopupAsync(
            title: "Success",
            message: "Game updated successfully.",
            isDismissable: true,
            showOkay: true,
            showCancel: false,
            showQuit: false,
            okayText: "Okay");

        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
    }

    public void RevertToInitialState()
    {
        if (Game is null)
            return;

        for (int r = 0; r < Game.Round.Count; r++)
        {
            var round = Game.Round[r];
            foreach (var p in Game.Players)
            {
                if (_initialRoundScores.TryGetValue((r, p.ID), out int initialScore))
                {
                    var rs = round.RoundScores.FirstOrDefault(s => s.PlayerId == p.ID);
                    if (rs != null)
                    {
                        rs.Score = initialScore;
                    }
                }
            }
        }

        if (Game is CurrentGameModel cg)
        {
            cg.ScoreLimit = _initialScoreLimit;
        }
        else if (Game is PlayedGameModel pg)
        {
            pg.ScoreLimit = _initialScoreLimit;
        }

        _gameService.RecalculateGame(Game);
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
        RevertToInitialState();
        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
    }
}

public partial class EditRoundItemViewModel : ObservableObject
{
    public int RoundNumber { get; set; }
    public string RoundTitle => $"Round {RoundNumber}";
    public ObservableCollection<EditPlayerScoreItemViewModel> PlayerScores { get; set; } = [];
}

public partial class EditPlayerScoreItemViewModel : ObservableObject
{
    private readonly Action? _onScoreChanged;

    public Guid PlayerId { get; init; }
    public string PlayerName { get; init; } = string.Empty;

    [ObservableProperty]
    public partial string ScoreText { get; set; } = "0";

    public EditPlayerScoreItemViewModel(Guid playerId, string playerName, int initialScore, Action? onScoreChanged = null)
    {
        PlayerId = playerId;
        PlayerName = playerName;
        _onScoreChanged = onScoreChanged;
        ScoreText = initialScore.ToString();
    }

    partial void OnScoreTextChanged(string value)
    {
        _onScoreChanged?.Invoke();
    }
}
