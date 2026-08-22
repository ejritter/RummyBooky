using System.Collections.Immutable;

namespace RummyBooky.ViewModels;

public partial class MainPageViewModel(IPopupService popupService, GameService gameService, IAppAudioService appAudioService) 
    : BaseViewModel(popupService, gameService)
{
    private readonly IAppAudioService _appAudioService = appAudioService;
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
    private async Task<bool> MuteUnmuteGambler()
    {
        var results = false;
        switch(_appAudioService.Volume)
        {
            case 0:
                _appAudioService.Unmute();
                results = true;
                break;
            case > 0:
                _appAudioService.Mute();
                results = true;
                break;

        }

        return results;
    }

    [RelayCommand]
    private async Task Appearing()
    {
     //   await CopyModifiedGamesBackToDevice();
        await LoadActiveGamesAsync();
        await LoadPlayedGamesAsync();
        await LoadAllPlayersAsync();
    }

    private async Task<bool> LoadAllPlayersAsync()
    {
        var results = false;
        results = await _gameService.LoadAllPlayersDictionaryAsync();
        return results;
    }

    private async Task<bool> LoadActiveGamesAsync()
    {
        var results = false;
        var games = await _gameService.LoadActiveGamesAsync();
        var gamesSorted = games
            .OrderBy(g => g.GameStart)
            .ToList<CurrentGameModel>();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            ActiveGames.Clear();
            foreach (var game in gamesSorted)
            {
                ActiveGames.Add(game);
            }
        });
        results = true;
        return results;
    }

    private async Task<bool> LoadPlayedGamesAsync()
    {
        var results = false;
        var playedGames = await _gameService.LoadPlayedGamesAsync();
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            PlayedGames.Clear();
            foreach( var game in playedGames)
            {
                PlayedGames.Add(game);
            }
        });
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

    [RelayCommand]
    private async Task Leaderboard()
    {
        await Shell.Current.GoToAsync(nameof(LeaderboardPage));
    }

    [RelayCommand(CanExecute = nameof(CanResumeGame))]
    private async Task<bool> ResumeGame()
    {
        try
        {
            if (SelectedGame is null)
                return false;

            if (SelectedGame.Players.Count < IntConstants.MinimumPlayerCount)
            {
                await ShowPopupAsync(title: "Invalid Game", message: "This game does not have enough players to resume.", isDismissable: true);
                return false;
            }

            await Shell.Current.GoToAsync(nameof(CurrentGamePage), new Dictionary<string, object>
            {
                ["CurrentGame"] = SelectedGame
            });
            SelectedGame = null;
            return true;
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowPopupAsync(title: "Error Resuming Game", message: ex.Message, isDismissable: true);
            });
            return false;
        }
    }

    [RelayCommand]
    private async Task EditGame(GameModel? game)
    {
        var targetGame = game ?? SelectedGame;
        if (targetGame is null)
            return;

        try
        {
            await Shell.Current.GoToAsync(nameof(EditGamePage), new Dictionary<string, object>
            {
                ["Game"] = targetGame
            });
        }
        catch (Exception ex)
        {
            await MainThread.InvokeOnMainThreadAsync(async () =>
            {
                await ShowPopupAsync(title: "Error Opening Game Editor", message: ex.Message, isDismissable: true);
            });
        }
    }
}
