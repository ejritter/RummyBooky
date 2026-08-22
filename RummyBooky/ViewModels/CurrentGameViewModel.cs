

namespace RummyBooky.ViewModels;

public partial class CurrentGameViewModel(IPopupService popupService, GameService gameService)
    : BaseViewModel(popupService, gameService), IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        Console.WriteLine($"[DEBUG_CURRENTGAME] VM ApplyQueryAttributes called. Query keys: {string.Join(", ", query.Keys)}");
        if (query.TryGetValue("CurrentGame", out var gameObj))
        {
            Console.WriteLine($"[DEBUG_CURRENTGAME] VM ApplyQueryAttributes found CurrentGame type: {gameObj?.GetType().FullName}");
            if (gameObj is CurrentGameModel gameModel)
            {
                Console.WriteLine($"[DEBUG_CURRENTGAME] VM ApplyQueryAttributes gameModel.Players count: {gameModel.Players?.Count ?? -1}");
                CurrentGame = gameModel;
                SyncPlayersCollection(gameModel);
            }
            else if (gameObj is NewGameModel newGame)
            {
                var converted = newGame.ConvertToCurrentGame();
                CurrentGame = converted;
                SyncPlayersCollection(converted);
            }
        }
        else
        {
            Console.WriteLine("[DEBUG_CURRENTGAME] VM ApplyQueryAttributes: 'CurrentGame' NOT in query!");
        }
    }
    private RoundModel? _lastRoundSubscribed;
    private readonly Dictionary<Guid, string> _activeRoundDraftScores = new();
    private bool _isNavigatingRounds = false;

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
    public partial ObservableCollection<PlayerModel> Players { get; set; } = [];

    [ObservableProperty]
    public partial RoundModel CurrentRound { get; set; }

    [ObservableProperty]
    public partial int SelectedRoundIndex { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsViewingPreviousRound { get; set; } = false;

    [ObservableProperty]
    public partial bool IsNotViewingPreviousRound { get; set; } = true;

    [ObservableProperty]
    public partial bool CanGoToPreviousRound { get; set; } = false;

    [ObservableProperty]
    public partial bool CanGoToNextRound { get; set; } = false;

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

            // Apply mutations sequentially to avoid concurrency issues
            foreach (var player in CurrentGame.Players)
            {
                await _gameService.SetPlayerScoreCurrentGameScoreAsync(player);
                await _gameService.SetPlayersHighestScoredHandAsync(player);
                await _gameService.SetPlayersLowestScoredHandAsync(player);

                await _gameService.SetRoundHighestPlayedHandAsync(player, CurrentRound);
                await _gameService.SetRoundLowestPlayedHandAsync(player, CurrentRound);
                await _gameService.SetRoundLeadingPlayerAsync(player, CurrentRound);
                await _gameService.SetRoundPlayersScoredHandsAsync(player, CurrentRound);
            }

            // Record round score models
            foreach (var player in CurrentGame.Players)
            {
                if (int.TryParse(player.PlayerScoreText, out var scoreVal))
                {
                    var rs = CurrentRound.RoundScores.FirstOrDefault(r => r.PlayerId == player.ID);
                    if (rs is null)
                    {
                        rs = new RoundScoreModel { PlayerId = player.ID, Score = scoreVal };
                        CurrentRound.RoundScores.Add(rs);
                    }
                    else
                    {
                        rs.Score = scoreVal;
                    }
                }
            }

            // Clear input scores (mutation)
            await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetPlayersScoreTextToEmptyAsync(player)));
            _activeRoundDraftScores.Clear();

            // Winners popup
            var winnerResults = await _gameService.CheckForWinnersAsync(CurrentGame);
            if (winnerResults.Results)
            {
                var popupResults = new PopupResultsModel();
                if (winnerResults.GameStatus == GameStatus.Won)
                {
                    popupResults = await ShowPopupAsync(
                        title: "We have a winner!",
                        message: $"Congratulations {winnerResults.Winners.First().PlayerName}!!!!",
                        players: winnerResults.Winners,
                        gameStatus: winnerResults.GameStatus,
                        isDismissable: false);
                }
                else if (winnerResults.GameStatus == GameStatus.Draw)
                {
                    popupResults = await ShowPopupAsync(
                        title: "We have a draw!",
                        message: "Choose a winner or make it a draw.",
                        players: winnerResults.Winners,
                        gameStatus: winnerResults.GameStatus,
                        isDismissable: false);
                }

                if (!popupResults.Confirmed)
                {
                    // ROLLBACK all mutations
                    await MainThread.InvokeOnMainThreadAsync(() =>
                    {
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
                    });

                    // Do NOT create next round, do NOT save
                    return false;
                }

                // User confirmed winner: mark game finished and save here
                if (popupResults.GameState == GameStatus.Won)
                {
                    var playedGame = CurrentGame
                       .ConvertToPlayedGame(gameState: popupResults.GameState,
                                            winningPlayer: popupResults.SelectedWinner);

                    await _gameService.SaveGameAsync(playedGame);
                }
                if (popupResults.GameState == GameStatus.Draw)
                {
                    var playedGame = CurrentGame
                        .ConvertToPlayedGame(popupResults.GameState, null);
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
                // No winner: proceed to next round, rotate dealer clockwise to player's left, and save
                if (MainThread.IsMainThread)
                {
                    CurrentRound = CurrentGame
                        .CreateNextRoundTemplate()
                        .Round
                        .Last();
                    SelectedRoundIndex = CurrentGame.Round.Count - 1;
                    UpdateRoundNavigationState();
                    await _gameService.SetNextDealerForNewRoundAsync(CurrentGame);
                    await _gameService.SaveGameAsync(CurrentGame);
                }
                else
                {
                    await MainThread.InvokeOnMainThreadAsync(async () =>
                    {
                        CurrentRound = CurrentGame
                            .CreateNextRoundTemplate()
                            .Round
                            .Last();
                        SelectedRoundIndex = CurrentGame.Round.Count - 1;
                        UpdateRoundNavigationState();
                        await _gameService.SetNextDealerForNewRoundAsync(CurrentGame);
                        await _gameService.SaveGameAsync(CurrentGame);
                    });
                }

                // Update visibility based on scored hands
                DisplayPlayersHighestLowestHands = CurrentGame.Round.Count > 1;
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
        if (IsViewingPreviousRound)
            return false;

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
            if (value.Players != null)
            {
                foreach (var player in value.Players)
                {
                    player.PropertyChanged -= Player_PropertyChanged;
                    player.PropertyChanged += Player_PropertyChanged;
                }
            }

            _gameService.RecalculateGame(value);

            SelectedRoundIndex = value.Round.Count > 0 ? value.Round.Count - 1 : 0;
            CurrentRound = value.Round.Count > 0 ? value.Round[SelectedRoundIndex] : new RoundModel { GameId = value.GameId };
            UpdateRoundNavigationState();

            ScoreLimit = value.ScoreLimit;
            GameStart = value.GameStart;
            _ = CheckDealerStatus(value);
            SyncPlayersCollection(value);
        }
    }

    private void SyncPlayersCollection(CurrentGameModel? game)
    {
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Players.Clear();
            if (game?.Players != null)
            {
                foreach (var p in game.Players)
                {
                    Players.Add(p);
                }
            }
            OnPropertyChanged(nameof(Players));
            OnPropertyChanged(nameof(CurrentGame));
        });
    }

    public async void OnAppearing()
    {
        if (CurrentGame != null && CurrentGame.GameId != Guid.Empty && CurrentGame.Players?.Count > 0)
        {
            foreach (var player in CurrentGame.Players)
            {
                player.PropertyChanged -= Player_PropertyChanged;
                player.PropertyChanged += Player_PropertyChanged;
            }

            ScoreLimit = CurrentGame.ScoreLimit;
            _gameService.RecalculateGame(CurrentGame);
            UpdateRoundNavigationState();
            SyncPlayersCollection(CurrentGame);
            return;
        }

        var activeGames = await _gameService.LoadActiveGamesAsync();
        var latest = activeGames.LastOrDefault();
        if (latest != null && (CurrentGame == null || CurrentGame.GameId == Guid.Empty || CurrentGame.Players == null || CurrentGame.Players.Count == 0))
        {
            CurrentGame = latest;
        }

        if (CurrentGame != null)
        {
            if (CurrentGame.Players != null)
            {
                foreach (var player in CurrentGame.Players)
                {
                    player.PropertyChanged -= Player_PropertyChanged;
                    player.PropertyChanged += Player_PropertyChanged;
                }
            }

            ScoreLimit = CurrentGame.ScoreLimit;
            _gameService.RecalculateGame(CurrentGame);
            UpdateRoundNavigationState();
            SyncPlayersCollection(CurrentGame);
        }
    }

    private void UpdateRoundNavigationState()
    {
        if (CurrentGame?.Round is null || CurrentGame.Round.Count == 0)
        {
            CanGoToPreviousRound = false;
            CanGoToNextRound = false;
            IsViewingPreviousRound = false;
            IsNotViewingPreviousRound = true;
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

        CalculatePlayerScoresCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task PreviousRound()
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

    [RelayCommand]
    private async Task NextRound()
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

    [RelayCommand]
    private async Task ReturnToActiveRound()
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

    [RelayCommand]
    private async Task EditGame()
    {
        await Shell.Current.GoToAsync(nameof(EditGamePage), new Dictionary<string, object>
        {
            ["Game"] = CurrentGame
        });
    }

    private async Task<bool> CheckDealerStatus(CurrentGameModel value)
    {
        var dealerFound = value.Players.FirstOrDefault(p => p.IsDealer);
        if (dealerFound is null)
        {
            return await _gameService.SetRandomDealerForCurrentGameAsync(value);
        }
        return true;
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

            SubscribeRoundObservers(value);
            _ = CheckDealerStatus(CurrentGame);
            UpdateHighestLowestVisibility();
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
        await _gameService.SaveGameAsync(CurrentGame);
        await Shell.Current.GoToAsync($"///{nameof(MainPage)}");
    }

    [RelayCommand]
    private async Task<bool> QuitGame()
    {
        var title = "Quit Game!?";
        var message = "Are you sure you want to quit? This will mark the game as Forfeit and no points will be counted. No hand highest or lowest ranking will be set.";
        var popupResults = await ShowPopupAsync(title: title, message: message, isDismissable: true);
        if (popupResults.Confirmed)
        {
            var forfeitGame = CurrentGame.ConvertToPlayedGame(popupResults.GameState, null);

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
            if (_isNavigatingRounds)
                return;

            if (IsViewingPreviousRound && sender is PlayerModel modifiedPlayer)
            {
                if (int.TryParse(modifiedPlayer.PlayerScoreText, out int newScore))
                {
                    if (SelectedRoundIndex >= 0 && SelectedRoundIndex < CurrentGame.Round.Count)
                    {
                        var round = CurrentGame.Round[SelectedRoundIndex];
                        var rs = round.RoundScores.FirstOrDefault(r => r.PlayerId == modifiedPlayer.ID);
                        if (rs is null)
                        {
                            rs = new RoundScoreModel { PlayerId = modifiedPlayer.ID, Score = newScore };
                            round.RoundScores.Add(rs);
                        }
                        else
                        {
                            rs.Score = newScore;
                        }

                        _gameService.RecalculateGame(CurrentGame);
                        _ = _gameService.SaveGameAsync(CurrentGame);
                        UpdateHighestLowestVisibility();
                    }
                }
            }
            else
            {
                CalculatePlayerScoresCommand.NotifyCanExecuteChanged();
            }
        }
    }

    [RelayCommand]
    private async Task<bool> SetPlayerAsDealer(PlayerModel playerModel)
    {
        var results = false;
        if (MainThread.IsMainThread)
        {
            await _gameService.SetGamesDealerAsync(CurrentGame, playerModel);
            results = true;
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _gameService.SetGamesDealerAsync(CurrentGame, playerModel);
                results = true;
            });
        }
        return results;
    }
}
