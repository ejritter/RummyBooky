using CommunityToolkit.Maui.Core.Platform;

namespace RummyBooky.ViewModels;

[QueryProperty(nameof(CurrentGame), "CurrentGame")]
public partial class CurrentGameViewModel(IPopupService popupService, GameService gameService)
    : BaseViewModel(popupService, gameService)
{
    private RoundModel? _lastRoundSubscribed;

    [ObservableProperty]
    public partial bool DisplayPlayersHighestLowestHands { get; set; } = false;

    [ObservableProperty]
    public partial string RoundText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int ScoreLimit { get; set; } = 0;

    [ObservableProperty]
    public partial DateTime GameStart { get; set; }

    [ObservableProperty]
    public partial CurrentGameModel CurrentGame { get; set; } = new();

    [ObservableProperty]
    public partial RoundModel CurrentRound { get; set; }

    [RelayCommand(CanExecute = nameof(CanExecuteCalculatePlayerScores))]
    private async Task<bool> CalculatePlayerScores(object sender)
    {
        try
        {
            await HideKeyboard();
            // Snapshot mutable state for rollback
            var playerSnapshots = CurrentGame.Players
                .Select(p => new
                {
                    Player = p,
                    Score = p.PlayerScore,
                    ScoreText = p.PlayerScoreText,
                    Highest = p.HighestScoredHand,
                    Lowest = p.LowestScoredHand
                })
                .ToList();

            var roundSnapshot = new
            {
                Leading = CurrentRound.LeadingPlayer,
                HighestPlayer = CurrentRound.PlayerHighestScoringHand,
                HighestValue = CurrentRound.CurrentHighestScoredHandValue,
                LowestPlayer = CurrentRound.PlayerLowestScoringHand,
                LowestValue = CurrentRound.CurrentLowestScoredHandValue,
                ScoredPlayers = CurrentRound.PlayersScoredHandThisRound.ToList() // copy
            };

            // Apply mutations
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetPlayerScoreCurrentGameScoreAsync(player)));
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetPlayersHighestScoredHandAsync(player)));
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetPlayersLowestScoredHandAsync(player)));

            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetRoundHighestPlayedHandAsync(player, CurrentRound)));
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetRoundLowestPlayedHandAsync(player, CurrentRound)));
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetRoundLeadingPlayerAsync(player, CurrentRound)));
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetRoundPlayersScoredHandsAsync(player, CurrentRound)));

            // Clear input scores (mutation)
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetPlayersScoreTextToEmpty(player)));

            // Winners popup
            var winnerResults = await _gameService.CheckForWinnersAsync(CurrentGame);
            if (winnerResults.Results)
            {
                var userConfirmed = false;
                if (winnerResults.GameStatus == GameStatus.Won)
                {
                    userConfirmed = await ShowPopupAsync(
                        title: "We have a winner!",
                        message: $"Congratulations {winnerResults.Winners.First().PlayerName}!!!!",
                        isDismissable: false);
                }
                else if (winnerResults.GameStatus == GameStatus.Draw)
                {
                    userConfirmed = await ShowPopupAsync(
                        title: "Who is the winner?",
                        message: "Choose a winner or make it a draw.",
                        players: winnerResults.Winners,
                        isDismissable: false);
                }

                if (!userConfirmed)
                {
                    // ROLLBACK all mutations
                    foreach (var snap in playerSnapshots)
                    {
                        snap.Player.PlayerScore = snap.Score;
                        snap.Player.PlayerScoreText = snap.ScoreText;
                        snap.Player.HighestScoredHand = snap.Highest;
                        snap.Player.LowestScoredHand = snap.Lowest;
                    }

                    CurrentRound.LeadingPlayer = roundSnapshot.Leading;
                    CurrentRound.PlayerHighestScoringHand = roundSnapshot.HighestPlayer;
                    CurrentRound.CurrentHighestScoredHandValue = roundSnapshot.HighestValue;
                    CurrentRound.PlayerLowestScoringHand = roundSnapshot.LowestPlayer;
                    CurrentRound.CurrentLowestScoredHandValue = roundSnapshot.LowestValue;

                    CurrentRound.PlayersScoredHandThisRound.Clear();
                    foreach (var p in roundSnapshot.ScoredPlayers)
                    {
                        CurrentRound.PlayersScoredHandThisRound.Add(p);
                    }

                    // Reorder back to appropriate display after rollback
                    ReorderPlayersForDisplay();

                    // Do NOT create next round, do NOT save
                    return false;
                }

                // Reorder for display (Round > 1 typically by score)
                ReorderPlayersForDisplay();

                // User confirmed winner: you might mark game finished and save here

                if (winnerResults.GameStatus == GameStatus.Won)
                {
                    var playedGame = CurrentGame
                       .ConvertToPlayedGame(gameState: winnerResults.GameStatus,
                                            winningPlayer: winnerResults.Winners.First());
                    await _gameService.SaveGameAsync(playedGame);
                }
                if (winnerResults.GameStatus == GameStatus.Draw)
                {
                    var playedGame = CurrentGame
                        .ConvertToPlayedGame(winnerResults.GameStatus, null);
                    await _gameService.SaveGameAsync(playedGame);
                }
                if (MainThread.IsMainThread)
                {
                    await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
                        Shell.Current.GoToAsync($"///{nameof(MainPage)}");
                    });
                }
                return true;
            }
            else
            {
                // No winner: proceed to next round and save
                if (MainThread.IsMainThread)
                {
                    CurrentGame = CurrentGame.CreateNextRoundTemplate();
                    CurrentRound = CurrentGame.Round.Last();
                    await _gameService.SaveGameAsync(CurrentGame);
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        CurrentGame = CurrentGame.CreateNextRoundTemplate();
                        CurrentRound = CurrentGame.Round.Last();
                        await _gameService.SaveGameAsync(CurrentGame);
                    });
                }

                RoundText = $"Round {CurrentGame.Round.Count}";

                // Update visibility based on scored hands
                DisplayPlayersHighestLowestHands = CurrentGame.Round.Count > 1;

                // After moving to next round, ensure ordering is correct for display
                ReorderPlayersForDisplay();
                return true;
            }
        }
        catch (AggregateException allEx)
        {
            var errorBuilder = new StringBuilder();
            errorBuilder.AppendLine("Error encountered while calculating scores:");
            foreach (Exception ex in allEx.InnerExceptions)
            {
                errorBuilder.AppendLine($" - {ex.GetType().Name}: {ex.Message}");
            }
            _ = await ShowPopupAsync(title: "Errors!", message: errorBuilder.ToString(), isDismissable: false);
            return false;
        }
    }

    private async Task<bool> HideKeyboard()
    {
        if (Application.Current?.MainPage is Page page)
        {
            var focusedElement = page.GetVisualTreeDescendants()
                .OfType<Entry>()
                .FirstOrDefault(e => e.IsFocused);
            if (focusedElement != null)
            await focusedElement.HideKeyboardAsync();
            return true;
            
        }
        else
        {
            return false;
        }
    }

    private bool CanExecuteCalculatePlayerScores()
    {
        var results = false;
        foreach (var player in CurrentGame.Players)
        {
            if (player.PlayerScoreText == string.Empty)
            {
                return results;
            }
        }
        results = true;
        return results;
    }

    partial void OnCurrentGameChanged(CurrentGameModel value)
    {
        if (value is not null)
        {
            foreach (var player in value.Players)
            {
                player.PropertyChanged -= Player_PropertyChanged;
                player.PropertyChanged += Player_PropertyChanged;
            }

            CurrentRound = value.Round.LastOrDefault();
            RoundText = $"Round {value.Round.Count}";
            ScoreLimit = value.ScoreLimit;
            GameStart = value.GameStart;
            SubscribeRoundObservers(CurrentRound);

            // Initial visibility based on round state
            UpdateHighestLowestVisibility();

            ReorderPlayersForDisplay();
        }
    }

    partial void OnCurrentRoundChanged(RoundModel value)
    {
        if (value is not null)
        {
            foreach (var player in CurrentGame.Players)
            {
                player.PropertyChanged -= Player_PropertyChanged;
                player.PropertyChanged += Player_PropertyChanged;
            }

            RoundText = $"Round {CurrentGame.Round.Count}";

            SubscribeRoundObservers(value);

            UpdateHighestLowestVisibility();

            ReorderPlayersForDisplay();
        }
    }

    private void SubscribeRoundObservers(RoundModel? round)
    {
        // Unsubscribe previous
        if (_lastRoundSubscribed is not null)
        {
            _lastRoundSubscribed.PlayersScoredHandThisRound.CollectionChanged -= PlayersScoredHandThisRound_CollectionChanged;
            _lastRoundSubscribed.PropertyChanged -= CurrentRound_PropertyChanged;
        }

        _lastRoundSubscribed = round;

        if (round is not null)
        {
            round.PlayersScoredHandThisRound.CollectionChanged += PlayersScoredHandThisRound_CollectionChanged;
            round.PropertyChanged += CurrentRound_PropertyChanged;
        }
    }

    private void PlayersScoredHandThisRound_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        UpdateHighestLowestVisibility();
    }

    private void CurrentRound_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // When any of the round’s “highest/lowest” properties update, reflect visibility
        if (e.PropertyName == nameof(RoundModel.CurrentHighestScoredHandValue)
            || e.PropertyName == nameof(RoundModel.PlayerHighestScoringHand)
            || e.PropertyName == nameof(RoundModel.CurrentLowestScoredHandValue)
            || e.PropertyName == nameof(RoundModel.PlayerLowestScoringHand))
        {
            UpdateHighestLowestVisibility();
        }
    }

    private void UpdateHighestLowestVisibility()
    {
        if (CurrentRound is null)
        {
            DisplayPlayersHighestLowestHands = false;
            return;
        }

        // Only consider “ready” when both value and player are set away from sentinels
        bool highestReady =
            CurrentRound.PlayerHighestScoringHand is not null &&
            CurrentRound.CurrentHighestScoredHandValue != int.MinValue;

        bool lowestReady =
            CurrentRound.PlayerLowestScoringHand is not null &&
            CurrentRound.CurrentLowestScoredHandValue != int.MaxValue;

        DisplayPlayersHighestLowestHands = highestReady || lowestReady || CurrentRound.PlayersScoredHandThisRound.Count > 0;
    }

    [RelayCommand]
    private async Task GoToMainPage()
    {
        await Shell.Current.GoToAsync($"//{nameof(MainPage)}");
    }

    [RelayCommand]
    private async Task<bool> QuitGame()
    {
        var quitGame = await ShowPopupAsync(title: "Quit Game!?", message: "Are you sure you want to quit this game?", isDismissable: true);
        if (quitGame)
        {
            var forfeitGame = CurrentGame.ConvertToPlayedGame(GameStatus.Forfeit, null);
            await _gameService.SaveGameAsync(forfeitGame);
            await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Player_PropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(PlayerModel.PlayerScoreText))
        {
            CalculatePlayerScoresCommand.NotifyCanExecuteChanged();
        }
    }

    // Reorders CurrentGame.Players to match UI rules:
    // Round 1: alphabetical by PlayerName; Round > 1: by PlayerScore desc.
    private void ReorderPlayersForDisplay()
    {
        if (CurrentGame is null || CurrentGame.Players is null)
        {
            return;
        }

        var roundCount = CurrentGame.Round?.Count ?? 0;

        IEnumerable<PlayerModel> ordered =
            roundCount <= 1
                ? CurrentGame.Players.OrderBy(p => p.PlayerName, StringComparer.CurrentCultureIgnoreCase)
                : CurrentGame.Players.OrderByDescending(p => p.PlayerScore);

        // Apply ordering in-place to preserve the same ObservableCollection instance (important for bindings)
        var orderedList = ordered.ToList();

        // If already in this order, skip touching the collection
        bool sameOrder = CurrentGame.Players.SequenceEqual(orderedList);
        if (sameOrder)
        {
            return;
        }

        // Rebuild the collection in the new order
        CurrentGame.Players.Clear();
        foreach (var p in orderedList)
        {
            CurrentGame.Players.Add(p);
        }
    }
}
