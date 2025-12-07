namespace RummyBooky.ViewModels;

public partial class MainPageViewModel(IPopupService popupService, GameService gameService) 
    : BaseViewModel(popupService, gameService)
{



    public ObservableCollection<GameModel> ActiveGames { get; set; } = new();
    public ObservableCollection<GameModel> PlayedGames { get; set; } = new();

    private bool CanResumeGame => SelectedGame is null ? false : true;

    [ObservableProperty]
    public partial GameModel? SelectedGame { get; set; } = null;

    partial void OnSelectedGameChanged(GameModel? oldValue, GameModel? newValue)
    {
        ResumeGameCommand.NotifyCanExecuteChanged();
    }

    [RelayCommand]
    private async Task NewGame()
    {
        await Shell.Current.GoToAsync(nameof(NewGamePage));
    }

    [RelayCommand]
    private async Task Appearing()
    {
        await LoadActiveGames();
        await LoadPlayedGames();
    }

    private async Task<bool> LoadActiveGames()
    {
        var results = false;
        var games = await _gameService.LoadActiveGamesAsync();
        ActiveGames.Clear();
        foreach (var game in games)
        {
            ActiveGames.Add(game);
        }
        results = true;
        return results;
    }

    private async Task<bool> LoadPlayedGames()
    {
        var results = false;
        var playedGames = await _gameService.LoadPlayedGamesAsync();
        PlayedGames.Clear();
        foreach( var game in playedGames)
        {
            PlayedGames.Add(game);
        }
        results = true;
        return results;
    }


    [RelayCommand(CanExecute = nameof(CanResumeGame))]
    private async Task<bool> ResumeGame()
    {
        var results = false;
        await Shell.Current.GoToAsync(nameof(CurrentGamePage), new Dictionary<string, object>
        {
            ["CurrentGame"] = SelectedGame
        });
        results = true;
        SelectedGame = null;
        return results;
    }
}
