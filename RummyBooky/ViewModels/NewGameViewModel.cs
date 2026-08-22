namespace RummyBooky.ViewModels;

public partial class NewGameViewModel(IPopupService popupService, GameService gameService)
        : BaseViewModel(popupService, gameService)
{

    
    private CancellationTokenSource? _searchCts;

    [ObservableProperty]
    public partial bool SwipeEnabled { get; set; } = false;

    //[ObservableProperty]
    //public partial AssignedPlayerModel? HighlightedSuggestedPlayer { get; set; } = null;

    [ObservableProperty] 
    public partial PlayerModel[] AllPlayerModels { get; set; } = [];

    [ObservableProperty]
    public partial ObservableCollection<PlayerModel> FilteredPlayerModelsByName { get; set; } = [];

    [ObservableProperty]
    public partial PlayerModel? SelectedSuggestedPlayerModel { get; set; } = null;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGridTemplate))]
    public partial bool ShowPlayerSuggestions { get; set; } = false;

    [ObservableProperty]
    public partial int SelectedSuggestedPlayerPosition { get; set; } = 0;

    [RelayCommand]
    private async Task Appearing()
    {
        GameModelTemplate ??= _gameService.GetNewGameModel();
        await _gameService.LoadAllPlayersDictionaryAsync();
        AllPlayerModels = await _gameService.GetAllPlayerModelsArray();
        GameModelTemplate.Players.CollectionChanged -= Players_CollectionChanged;
        GameModelTemplate.Players.CollectionChanged += Players_CollectionChanged;
    }

    [RelayCommand]
    private async Task Disappearing()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        GameModelTemplate.Players.CollectionChanged -= Players_CollectionChanged;
        FilteredPlayerModelsByName = [];
        SelectedSuggestedPlayerModel = null;
        SelectedSuggestedPlayerPosition = 0;
        ShowPlayerSuggestions = false;
    }
    partial void OnShowPlayerSuggestionsChanged(bool oldValue, bool newValue)
    {
        if (newValue == true)
            ShowGridTemplate = false;
        else
            ShowGridTemplate = GameModelTemplate.Players.Count > 0;
    }
    private void Players_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        ShowGridTemplate = GameModelTemplate.Players.Count > 0 && 
            ShowPlayerSuggestions == false;
        StartGameCommand.NotifyCanExecuteChanged();
        AddPlayerCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task EditPlayer(object? sender)
    {
        if (sender is PlayerModel playerModel)
        {
            await Shell.Current.GoToAsync(nameof(EditPlayerPage), animate: true, parameters: new Dictionary<string, object>
            {
                [nameof(EditPlayerViewModel.CurrentPlayer)] = playerModel
            });
        }

    }

    [RelayCommand]
    public async Task SearchPlayerSuggestions()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;

        await PerformSearchAsync(PlayerNameText, CancellationToken.None);
    }

    [RelayCommand]
    public async Task UserStoppedTyping()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        await PerformSearchAsync(PlayerNameText, token);
    }

    private async Task PerformSearchAsync(string query, CancellationToken token)
    {
        if (GameModelTemplate == null || GameModelTemplate.Players.Count >= IntConstants.MaximumPlayerCount)
            return;

        if (AllPlayerModels == null || AllPlayerModels.Length == 0)
        {
            await _gameService.LoadAllPlayersDictionaryAsync();
            AllPlayerModels = await _gameService.GetAllPlayerModelsArray();
        }

        if (string.IsNullOrWhiteSpace(query))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilteredPlayerModelsByName = [];
                SelectedSuggestedPlayerModel = null;
                SelectedSuggestedPlayerPosition = 0;
                ShowPlayerSuggestions = false;
                SwipeEnabled = false;
            });
            return;
        }

        var currentAddedIds = GameModelTemplate.Players.Select(p => p.ID).ToHashSet();
        var trimmedQuery = query.Trim();
        var matches = AllPlayerModels
            .Where(p => p != null && !string.IsNullOrEmpty(p.PlayerName) &&
                        (p.PlayerName.StartsWith(trimmedQuery, StringComparison.OrdinalIgnoreCase) ||
                         p.PlayerName.Contains(trimmedQuery, StringComparison.OrdinalIgnoreCase)) &&
                        !currentAddedIds.Contains(p.ID))
            .ToList();

        if (token.IsCancellationRequested)
            return;

        MainThread.BeginInvokeOnMainThread(() =>
        {
            if (token.IsCancellationRequested)
                return;

            FilteredPlayerModelsByName = new ObservableCollection<PlayerModel>(matches);
            SelectedSuggestedPlayerModel = matches.FirstOrDefault();
            SelectedSuggestedPlayerPosition = 0;
            ShowPlayerSuggestions = matches.Count > 0;
            SwipeEnabled = matches.Count > 1;
        });
    }

    public string ScoreBoundaries { get; init; } = $"{IntConstants.MinimumScoreLimit} - {IntConstants.MaximumScoreLimit}";

    public string PlayerBoundaries { get; init; } = $"{IntConstants.MinimumPlayerCount} - {IntConstants.MaximumPlayerCount}";

    [ObservableProperty]
    public partial NewGameModel GameModelTemplate { get; set; }


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    public partial string ScoreLimitText { get; set; } = "500";

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPlayerCommand))]
    public partial string PlayerNameText { get; set; } = string.Empty;


    partial void OnPlayerNameTextChanged(string value)
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = new CancellationTokenSource();
        var token = _searchCts.Token;

        CanAddPlayer();
        AddPlayerCommand.NotifyCanExecuteChanged();

        if (string.IsNullOrWhiteSpace(value))
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                FilteredPlayerModelsByName = [];
                SelectedSuggestedPlayerModel = null;
                SelectedSuggestedPlayerPosition = 0;
                ShowPlayerSuggestions = false;
                SwipeEnabled = false;
            });
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                await Task.Delay(150, token);
                if (!token.IsCancellationRequested)
                {
                    await PerformSearchAsync(value, token);
                }
            }
            catch (OperationCanceledException)
            {
                // Ignored
            }
        }, token);
    }
    partial void OnScoreLimitTextChanged(string value)
    {
        StartGameCommand.NotifyCanExecuteChanged();
    }

    public PlayerModel? LastAddedPlayer { get; set; }
    public string LastSearchQuery { get; set; } = string.Empty;

    private bool CanAddPlayer()
    {
        var results = !string.IsNullOrWhiteSpace(PlayerNameText) &&
                        GameModelTemplate != null &&
                        GameModelTemplate.Players.Count < IntConstants.MaximumPlayerCount;
        return results;
    }
    private bool CanStartGame()
    {
        var scoreLimitInt = 0;
        var results = !string.IsNullOrEmpty(ScoreLimitText) &&
                        int.TryParse(ScoreLimitText, out scoreLimitInt) &&
                        scoreLimitInt >= IntConstants.MinimumScoreLimit &&
                        scoreLimitInt <= IntConstants.MaximumScoreLimit &&
                        GameModelTemplate != null &&
                        GameModelTemplate.Players.Count >= IntConstants.MinimumPlayerCount;
        return results;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlayerSuggestions))]
    public partial bool? ShowGridTemplate { get; set; } = false;

    [RelayCommand(CanExecute = nameof(CanAddPlayer))]
    private async Task<bool> AddPlayer(Entry entry)
    {
        var query = PlayerNameText.Trim();
        LastSearchQuery = query;
        var results = await _gameService.AddPlayerToNewGameAsync(GameModelTemplate, query);
        LastAddedPlayer = GameModelTemplate.Players.LastOrDefault();
        PlayerNameText = string.Empty;
        FilteredPlayerModelsByName = [];
        SelectedSuggestedPlayerModel = null;
        SelectedSuggestedPlayerPosition = 0;
        ShowPlayerSuggestions = false;
        StartGameCommand.NotifyCanExecuteChanged();
        if (GameModelTemplate.Players.Count == IntConstants.MaximumPlayerCount)
        {
            entry?.Unfocus();
            if (entry != null)
            {
                await entry.HideKeyboardAsync();
            }
        }
        return results;
    }

    [RelayCommand]
    private async Task<bool> AddSuggestedPlayer(PlayerModel? player = null)
    {
        var targetPlayer = player ?? SelectedSuggestedPlayerModel;
        if (targetPlayer is null)
            return false;

        LastSearchQuery = PlayerNameText.Trim();
        var results = await _gameService.AddExistingPlayerModelToNewGameAsync(GameModelTemplate, targetPlayer);
        LastAddedPlayer = GameModelTemplate.Players.LastOrDefault();
        PlayerNameText = string.Empty;
        FilteredPlayerModelsByName = [];
        SelectedSuggestedPlayerModel = null;
        SelectedSuggestedPlayerPosition = 0;
        ShowPlayerSuggestions = false;
        StartGameCommand.NotifyCanExecuteChanged();
        if (GameModelTemplate.Players.Count == IntConstants.MaximumPlayerCount)
        {
           await HideKeyboard();
        }
        return results;
    }

    [RelayCommand(CanExecute = nameof(CanStartGame))]
    private async Task StartGame()
    {
        await HideKeyboard();

        // If no dealer is selected, prompt the user to choose or assign random
        var currentDealer = GameModelTemplate.Players.FirstOrDefault(p => p.IsDealer);
        if (currentDealer == null)
        {
            var promptTitle = GameModelTemplate.Players.Count == 2 ? "First Dealer" : "Starting Dealer";
            var promptMessage = GameModelTemplate.Players.Count == 2
                ? "Select who will deal first (or Cancel for random):"
                : "Select the starting dealer (seating order rotates clockwise to player's left, or Cancel for random):";

            var choice = await ShowPopupAsync(
                title: promptTitle,
                message: promptMessage,
                players: GameModelTemplate.Players.ToList(),
                isDismissable: true);

            if (choice.Confirmed && choice.SelectedWinner != null)
            {
                await _gameService.SetGamesDealerAsync(GameModelTemplate, choice.SelectedWinner);
            }
            else
            {
                await _gameService.SetRandomDealerForCurrentGameAsync(GameModelTemplate);
            }
        }

        Console.WriteLine($"[DEBUG_NEWGAME] StartGame: GameModelTemplate.Players count before fresh = {GameModelTemplate.Players.Count}");
        await _gameService.CreateFreshPlayerTemplatesForCurrentGame(GameModelTemplate);
        Console.WriteLine($"[DEBUG_NEWGAME] StartGame: GameModelTemplate.Players count after fresh = {GameModelTemplate.Players.Count}");
        var currentGame = GameModelTemplate.ConvertToCurrentGame();
        Console.WriteLine($"[DEBUG_NEWGAME] StartGame: currentGame.Players count = {currentGame.Players.Count}");
        await _gameService.SetCurrentGameScoreLimitAsync(currentGame, int.Parse(ScoreLimitText));
        await _gameService.SaveGameAsync(currentGame);
        await Shell.Current.GoToAsync(nameof(CurrentGamePage), new Dictionary<string, object>
        {
            ["CurrentGame"] = currentGame
        });
        ResetNewGameViewModelStates();

    }

    private async Task<bool> HideKeyboard()
    {
        if (Application.Current?.MainPage is Page page)
        {
            var entries = page.GetVisualTreeDescendants()
                .OfType<Entry>();
            foreach (var focusedElement in entries)
            {
                if (focusedElement != null)
                {
                    if (MainThread.IsMainThread)
                    {
                        await focusedElement.HideKeyboardAsync();
                    }
                    else
                    {
                        await MainThread.InvokeOnMainThreadAsync(async () =>
                        {
                            await focusedElement.HideKeyboardAsync();
                        });
                    }
                } 
            }
            return true;
        }
        else
        {
            return false;
        }
    }

    private void ResetNewGameViewModelStates()
    {
        _searchCts?.Cancel();
        _searchCts?.Dispose();
        _searchCts = null;
        ShowGridTemplate = false;
        GameModelTemplate = null;
        ScoreLimitText = "500";
        FilteredPlayerModelsByName = [];
        SelectedSuggestedPlayerModel = null;
        SelectedSuggestedPlayerPosition = 0;
        ShowPlayerSuggestions = false;
        LastAddedPlayer = null;
        LastSearchQuery = string.Empty;
        GameModelTemplate = _gameService.GetNewGameModel();
    }

    [RelayCommand]
    private async Task<bool> RemovePlayer(PlayerModel playerModel)
    {
        if (playerModel is null)
            return false;

        var results = false;
        await _gameService.RemovePlayerFromNewGameAsync(GameModelTemplate, playerModel);
        StartGameCommand.NotifyCanExecuteChanged();
        AddPlayerCommand.NotifyCanExecuteChanged();

        // If this player was just added and we had an active search query, restore search
        if (playerModel == LastAddedPlayer && !string.IsNullOrWhiteSpace(LastSearchQuery))
        {
            var queryToRestore = LastSearchQuery;
            LastAddedPlayer = null;
            LastSearchQuery = string.Empty;
            PlayerNameText = queryToRestore;
            await SearchPlayerSuggestions();
        }
        else if (playerModel == LastAddedPlayer)
        {
            LastAddedPlayer = null;
        }

        results = true;
        return results;
    }

    [RelayCommand]
    private async Task<bool> SetPlayerAsDealer(PlayerModel playerModel)
    {
        var results = false;
        if (MainThread.IsMainThread)
        {
            await _gameService.SetGamesDealerAsync(GameModelTemplate, playerModel);
            results = true;
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await _gameService.SetGamesDealerAsync(GameModelTemplate, playerModel);
                results = true;
            });
        }
        return results;
    }


}
