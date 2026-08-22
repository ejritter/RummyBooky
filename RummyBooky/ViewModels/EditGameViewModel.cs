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

    partial void OnGameChanged(GameModel? value)
    {
        if (value is null)
            return;

        _gameService.RecalculateGame(value);

        AvailablePlayers.Clear();
        foreach (var p in value.Players)
        {
            AvailablePlayers.Add(p);
        }

        if (value is PlayedGameModel played)
        {
            ScoreLimit = played.ScoreLimit;
            SelectedStatus = played.GameState.ToString();
            SelectedWinner = AvailablePlayers.FirstOrDefault(p => p.ID == played.WinningPlayer?.ID);
            IsWinnerPickerVisible = played.GameState == GameStatus.Won;
            GameTitle = $"Edit Played Game ({played.GameState})";
        }
        else if (value is CurrentGameModel current)
        {
            ScoreLimit = current.ScoreLimit;
            SelectedStatus = "In-Progress";
            SelectedWinner = null;
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

        if (Shell.Current.Navigation.NavigationStack.Count > 1)
        {
            await Shell.Current.GoToAsync("..");
        }
        else
        {
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
        }
    }

    [RelayCommand]
    public async Task CancelAsync()
    {
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
