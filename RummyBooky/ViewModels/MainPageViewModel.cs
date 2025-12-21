namespace RummyBooky.ViewModels;

public partial class MainPageViewModel(IPopupService popupService, GameService gameService) 
    : BaseViewModel(popupService, gameService)
{

    public ObservableCollection<GameModel> ActiveGames { get; set; } = [];
    public ObservableCollection<GameModel> PlayedGames { get; set; } = [];

    private bool CanResumeGame => SelectedGame is null ? false : true;

    [ObservableProperty]
    public partial GameModel? SelectedGame { get; set; } = null;

    partial void OnSelectedGameChanged(GameModel? oldValue, GameModel? newValue)
    {
        ResumeGameCommand.NotifyCanExecuteChanged();
    }


    [RelayCommand]
    private async Task Appearing()
    {
        await LoadActiveGamesAsync();
        await LoadPlayedGamesAsync();
        await LoadAllPlayersAsync();
    }

    private async Task<bool> LoadAllPlayersAsync()
    {
        var results = false;
        results = await _gameService.LoadAllPlayersAsync();
        return results;
    }

    private async Task<bool> LoadActiveGamesAsync()
    {
        var results = false;
        var games = await _gameService.LoadActiveGamesAsync();
        var gamesSorted = games
            .OrderBy(g => g.GameStart)
            .ToList<CurrentGameModel>();
        ActiveGames.Clear();
        foreach (var game in gamesSorted)
        {
            ActiveGames.Add(game);
        }
        results = true;
        return results;
    }

    private async Task<bool> LoadPlayedGamesAsync()
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

    [RelayCommand]
    private async Task NewGame()
    {
        //var playersArray = await _gameService.GetAllPlayerModelsArray();
        //await Shell.Current.GoToAsync(nameof(NewGamePage), new Dictionary<string, object>
        //{
        //    ["AllPlayerModels"] = playersArray
        //});
        await Shell.Current.GoToAsync(nameof(NewGamePage));
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
