namespace RummyBooky.ViewModels;

public partial class NewGameViewModel(IPopupService popupService, GameService gameService)
        : BaseViewModel(popupService, gameService)
{

    private int _tapCount = 0;

    //[ObservableProperty]
    //public partial PlayerModel? HighlightedSuggestedPlayer { get; set; } = null;
    [RelayCommand]
    private async Task Appearing()
    {
        GameModelTemplate = _gameService.GetNewGameModel();
        AllPlayerModels = await _gameService.GetAllPlayerModelsArray();
        GameModelTemplate.Players.CollectionChanged += Players_CollectionChanged;
        FilteredPlayerModelsByName.CollectionChanged += FilteredPlayerModelsByName_CollectionChanged;
    }

    [RelayCommand]
    private async Task Disappearing()
    {
        GameModelTemplate.Players.CollectionChanged -= Players_CollectionChanged;
        FilteredPlayerModelsByName.CollectionChanged -= FilteredPlayerModelsByName_CollectionChanged;
    }

    [ObservableProperty] 
    public partial PlayerModel[] AllPlayerModels { get; set; } = [];

    public ObservableCollection<PlayerModel> FilteredPlayerModelsByName { get; set; } = [];


    [ObservableProperty]
    public partial PlayerModel? SelectedSuggestedPlayerModel { get; set; } = null;
    
    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowGridTemplate))]
    public partial bool ShowPlayerSuggestions { get; set; } = false;

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
    private async Task<bool> UserStoppedTyping()
    {
        var results = false;
        if (GameModelTemplate.Players.Count >= IntConstants.MaximumPlayerCount)
            return results;//can't add players at this point. Don't bother suggesting.
        await HideKeyboard();
        FilteredPlayerModelsByName.Clear();
        //HighlightedSuggestedPlayer = null;
        if (string.IsNullOrWhiteSpace(PlayerNameText))
            return results;
        var matches = AllPlayerModels
                        //Grab by name
            .Where(p => p.PlayerName.StartsWith(PlayerNameText, StringComparison.OrdinalIgnoreCase) &&
                        //now whether or not that model is already in the Players collection
                        //if not, return it.
                        GameModelTemplate.Players.Any(gp => gp.ID == p.ID) == false)
            
            .ToList<PlayerModel>();
        foreach (var player in matches)
            FilteredPlayerModelsByName.Add(player);
        return results;
    }

    private void FilteredPlayerModelsByName_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        ShowPlayerSuggestions = FilteredPlayerModelsByName.Count > 0;
    }

    public string ScoreBoundaries { get; init; } = $"{IntConstants.MinimumScoreLimit} - {IntConstants.MaximumScoreLimit}";

    public string PlayerBoundaries { get; init; } = $"{IntConstants.MinimumPlayerCount} - {IntConstants.MaximumPlayerCount}";

    [ObservableProperty]
    public partial NewGameModel GameModelTemplate { get; set; }


    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(StartGameCommand))]
    public partial string ScoreLimitText { get; set; } = string.Empty;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(AddPlayerCommand))]
    public partial string PlayerNameText { get; set; } = string.Empty;


    partial void OnPlayerNameTextChanged(string value)
    {
        CanAddPlayer();
    }
    partial void OnScoreLimitTextChanged(string value)
    {
        CanStartGame();
    }

    private bool CanAddPlayer()
    {
        var results = !string.IsNullOrEmpty(PlayerNameText) &&
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
                        GameModelTemplate.Players.Count >= IntConstants.MinimumPlayerCount;
        return results;
    }

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(ShowPlayerSuggestions))]
    public partial bool? ShowGridTemplate { get; set; } = false;

    [RelayCommand(CanExecute = nameof(CanAddPlayer))]
    private async Task<bool> AddPlayer(Entry entry)
    {
        var results = await _gameService.AddPlayerToNewGameAsync(GameModelTemplate, PlayerNameText);
        PlayerNameText = string.Empty;
        FilteredPlayerModelsByName.Clear();
        //HighlightedSuggestedPlayer = null;
        CanStartGame();
        if (GameModelTemplate.Players.Count == IntConstants.MaximumPlayerCount)
        {
            entry.Unfocus();
            await entry.HideKeyboardAsync();
        }
        return results;
    }

    [RelayCommand]
    private async Task<bool> AddSuggestedPlayer()
    {
        var results = await _gameService.AddExistingPlayerModelToNewGameAsync(GameModelTemplate, SelectedSuggestedPlayerModel);
        PlayerNameText = string.Empty;
        FilteredPlayerModelsByName.Clear();
        //HighlightedSuggestedPlayer = null;
        CanStartGame();
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
        await _gameService.CreateFreshPlayerTemplatesForCurrentGame(GameModelTemplate);
        var currentGame = GameModelTemplate.ConvertToCurrentGame();
        //currentGame.Round.Add(currentGame);
        await _gameService.SetCurrentGameScoreLimitAsync(currentGame, int.Parse(ScoreLimitText));
        await _gameService.SaveGameAsync(currentGame);
        if (MainThread.IsMainThread)
        {
            ResetNewGameViewModelStates();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                ResetNewGameViewModelStates();
            });
        }
        await Shell.Current.GoToAsync(nameof(CurrentGamePage), new Dictionary<string, object>
        {
            ["CurrentGame"] = currentGame
        });

    }

    private async Task<bool> HideKeyboard()
    {
        if (Application.Current?.MainPage is Page page)
        {
            var focusedElement = page.GetVisualTreeDescendants()
                .OfType<Entry>()
                .FirstOrDefault(e => e.IsFocused);
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
            return true;
            
        }
        else
        {
            return false;
        }
    }

    private void ResetNewGameViewModelStates()
    {
        ShowGridTemplate = false;
        GameModelTemplate = null;
        ScoreLimitText = string.Empty;
        FilteredPlayerModelsByName.Clear();
        //HighlightedSuggestedPlayer = null;
        GameModelTemplate = _gameService.GetNewGameModel();
    }

    [RelayCommand]
    private async Task<bool> RemovePlayer(PlayerModel playerModel)
    {
        var results = false;
        await _gameService.RemovePlayerFromNewGameAsync(GameModelTemplate, playerModel);
        StartGameCommand.NotifyCanExecuteChanged();
        AddPlayerCommand.NotifyCanExecuteChanged();
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
