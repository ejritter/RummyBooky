using CommunityToolkit.Maui.Core.Platform;

namespace RummyBooky.ViewModels;

public partial class NewGameViewModel : BaseViewModel
{
    public NewGameViewModel(IPopupService popupService, GameService gameService)
        : base(popupService, gameService)
    {
        GameModelTemplate = _gameService.GetNewGameModel();
        GameModelTemplate.Players.CollectionChanged += Players_CollectionChanged;
    }

    private void Players_CollectionChanged(object? sender, NotifyCollectionChangedEventArgs eventArgs)
    {
        ShowGridTemplate = GameModelTemplate.Players.Count > 0;
        StartGameCommand.NotifyCanExecuteChanged();
        AddPlayerCommand.NotifyCanExecuteChanged();
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
    public partial bool? ShowGridTemplate { get; set; } = false;

    [RelayCommand(CanExecute = nameof(CanAddPlayer))]
    private async Task<bool> AddPlayer(Entry entry)
    {
        var results = await _gameService.AddPlayerToNewGameAsync(GameModelTemplate, PlayerNameText);
        PlayerNameText = string.Empty;
        CanStartGame();
        if (GameModelTemplate.Players.Count == IntConstants.MaximumPlayerCount)
        {
            entry.Unfocus();
            await entry.HideKeyboardAsync();
        }
        return results;
    }

    [RelayCommand(CanExecute = nameof(CanStartGame))]
    private async Task StartGame()
    {
        await HideKeyboard();
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
            await focusedElement.HideKeyboardAsync();
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
}
