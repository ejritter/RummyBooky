namespace RummyBooky.ViewModels;

[QueryProperty(nameof(CurrentPlayer), nameof(CurrentPlayer))]
public sealed partial class EditPlayerViewModel(IPopupService popupService, GameService gameService) : BaseViewModel(popupService, gameService)
{
    #region properties
    [ObservableProperty]
    public partial bool DisplayGames { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayPlayers { get; set; } = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemovePlayerCommand))]
    public partial PlayerModel CurrentPlayer { get; set; } = null;

    [ObservableProperty]
    public partial string NewPlayerNameText { get; set; } = string.Empty;

    public ObservableCollection<GameModel> ActiveGames { get; set; } = [];
    public ObservableCollection<GameModel> PlayedGames { get; set; } = [];

    public ObservableCollection<PlayerModel> AllPlayers { get; set; } = [];
    #endregion

    #region commands
    [RelayCommand(CanExecute = nameof(CanExecuteRemoveCommand))]
    public async Task RemovePlayer()
    {
        if (CurrentPlayer is null)
            return;
        var message = GenerateRemovePlayerMessage();
        var confirm = await ShowPopupAsync(title: "Warning!", message: message, isDismissable: false);

        if (confirm.Confirmed == true)
        {
            var results = await _gameService.RemovePlayerFromHistory(CurrentPlayer);
            if(results == true)
            {
                _ = await ShowPopupAsync(title: "Info!", message: "Completed.", isDismissable: true);
                await Shell.Current.GoToAsync("..");
            }
            else
            {
                _ = await ShowPopupAsync(title: "Warning"!, message: "Something went wrong, please consult the logs.", isDismissable: false);
            }
        }
    }


    [RelayCommand]
    public async Task PageLoaded()
    {
        var activeGamesTask = gameService.LoadActiveGamesAsync();
        var playedGamesTask = gameService.LoadPlayedGamesAsync();
        var allPlayersTask = gameService.GetAllPlayerModelsArray();
        if (CurrentPlayer is not null)
        {
            DisplayPlayers = false;
            DisplayGames = true;
            await Task.WhenAll(activeGamesTask, playedGamesTask);
            var activeGamesList = await activeGamesTask;
            var playedGamesList = await playedGamesTask;
            var results = await IdentifyPlayerInGames(activeGamesList, playedGamesList);
            LoadGameCollectionsWithPlayerName(results.activeGamesFound, results.playedGamesFound);
        }
        else
        {
            DisplayPlayers = true;
            DisplayGames = false;
            var allPlayerList = await allPlayersTask;
            AllPlayers = new ObservableCollection<PlayerModel>(allPlayerList);
        }
    }
    #endregion

    #region private methods
    private string GenerateRemovePlayerMessage()
    {
        StringBuilder output = new();
        output.AppendLine("Are you sure you wish to remove this player?");
        output.AppendLine("This operation cannot be undone. Removing this player may cause some games to become invalid.");
        output.AppendLine("Those games will be removed and other players scores can be impacted.");
        return output.ToString();
    }
    partial void OnCurrentPlayerChanged(PlayerModel oldValue, PlayerModel newValue)
    {
        Task.Run(async () =>
        {
            _ = await LoadGameCollectionsWithSelectedPlayer(newValue);
        });
        DisplayGames = true;
        DisplayPlayers = false;
    }
    private async Task<bool> LoadGameCollectionsWithSelectedPlayer(PlayerModel selectedPlayer)
    {
        var activeGamesTask = gameService.LoadActiveGamesAsync();
        var playedGamesTask = gameService.LoadPlayedGamesAsync();
        await Task.WhenAll(activeGamesTask, playedGamesTask);
        var activeGameList = await activeGamesTask;
        var playedGameList = await playedGamesTask;
        var results = await IdentifyPlayerInGames(activeGameList, playedGameList);
        return LoadGameCollectionsWithPlayerName(results.activeGamesFound, results.playedGamesFound);

    }

    private async Task<(List<CurrentGameModel> activeGamesFound, List<GameModel> playedGamesFound)>
        IdentifyPlayerInGames(List<CurrentGameModel> activeGamesList, List<GameModel> playedGamesList)
    {
        var activeGamesFound = new List<CurrentGameModel>();
        var playedGamesFound = new List<GameModel>();
        foreach (CurrentGameModel game in activeGamesList)
        {
            foreach (PlayerModel player in game.Players)
            {
                if (player.ID == CurrentPlayer.ID)
                {
                    activeGamesFound.Add(game);
                }
            }
        }
        foreach (GameModel game in playedGamesList)
        {
            foreach (PlayerModel player in game.Players)
            {
                if (player.ID == CurrentPlayer.ID)
                {
                    playedGamesFound.Add(game);
                }
            }
        }

        return await Task.FromResult<(List<CurrentGameModel>, List<GameModel>)>((activeGamesFound, playedGamesFound));
    }
    private bool LoadGameCollectionsWithPlayerName(List<CurrentGameModel> activeGamesList, List<GameModel> playedGamesList)
    {
        if (MainThread.IsMainThread)
        {
            foreach (CurrentGameModel game in activeGamesList)
            {
                ActiveGames.Add(game);
            }
            foreach (PlayedGameModel game in playedGamesList)
            {
                PlayedGames.Add(game);
            }
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(() =>
            {
                foreach (CurrentGameModel game in activeGamesList)
                {
                    ActiveGames.Add(game);
                }
                foreach (PlayedGameModel game in playedGamesList)
                {
                    PlayedGames.Add(game);
                }

            });
        }
        return true;
    }

    private bool CanExecuteRemoveCommand()
    {
        return CurrentPlayer is not null;
    }
    #endregion

}
