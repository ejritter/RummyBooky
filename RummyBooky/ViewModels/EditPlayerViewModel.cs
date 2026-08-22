namespace RummyBooky.ViewModels;

public sealed partial class EditPlayerViewModel(IPopupService popupService, GameService gameService) : BaseViewModel(popupService, gameService), IQueryAttributable
{
    public void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        if (query.TryGetValue("CurrentPlayer", out var playerObj) && playerObj is PlayerModel playerModel)
        {
            CurrentPlayer = playerModel;
        }
    }
    #region properties
    [ObservableProperty]
    public partial bool DisplayGames { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayPlayers { get; set; } = false;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(RemovePlayerCommand))]
    [NotifyCanExecuteChangedFor(nameof(UpdatePlayerNameCommand))]
    public partial PlayerModel CurrentPlayer { get; set; } = null;

    [ObservableProperty]
    [NotifyCanExecuteChangedFor(nameof(UpdatePlayerNameCommand))]
    public partial string NewPlayerNameText { get; set; } = string.Empty;

    public ObservableCollection<GameModel> ActiveGames { get; set; } = [];
    public ObservableCollection<GameModel> PlayedGames { get; set; } = [];

    public ObservableCollection<PlayerModel> AllPlayers { get; set; } = [];
    #endregion

    #region commands
    [RelayCommand(CanExecute = nameof(CanExecuteUpdatePlayerNameCommand))]
    public async Task UpdatePlayerName()
    {
        if (CurrentPlayer is null || string.IsNullOrWhiteSpace(NewPlayerNameText))
            return;

        var newName = NewPlayerNameText.Trim();
        if (string.Equals(newName, CurrentPlayer.PlayerName, StringComparison.Ordinal))
            return;

        var oldName = CurrentPlayer.PlayerName;
        var success = await _gameService.UpdatePlayerNameHistory(CurrentPlayer, newName);
        if (success)
        {
            CurrentPlayer.PlayerName = newName;
            NewPlayerNameText = string.Empty;
            await LoadGameCollectionsWithSelectedPlayer(CurrentPlayer);
            _ = await ShowPopupAsync(title: "Success", message: $"Player '{oldName}' updated to '{newName}'.", isDismissable: true);
        }
        else
        {
            _ = await ShowPopupAsync(title: "Warning", message: "Failed to update player name. Please consult the logs.", isDismissable: false);
        }
    }

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
        if (CurrentPlayer is not null)
        {
            DisplayPlayers = false;
            DisplayGames = true;
            await LoadGameCollectionsWithSelectedPlayer(CurrentPlayer);
        }
        else
        {
            DisplayPlayers = true;
            DisplayGames = false;
            var allPlayerList = await gameService.GetAllPlayerModelsArray();
            if (MainThread.IsMainThread)
            {
                AllPlayers.Clear();
                foreach (var player in allPlayerList)
                {
                    AllPlayers.Add(player);
                }
            }
            else
            {
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    AllPlayers.Clear();
                    foreach (var player in allPlayerList)
                    {
                        AllPlayers.Add(player);
                    }
                });
            }
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
        if (newValue is not null)
        {
            Task.Run(async () =>
            {
                _ = await LoadGameCollectionsWithSelectedPlayer(newValue);
            });
            DisplayGames = true;
            DisplayPlayers = false;
        }
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
        if (CurrentPlayer == null)
        {
            return (activeGamesFound, playedGamesFound);
        }

        foreach (CurrentGameModel game in activeGamesList)
        {
            if (game?.Players != null)
            {
                foreach (PlayerModel player in game.Players)
                {
                    if (player?.ID == CurrentPlayer.ID)
                    {
                        activeGamesFound.Add(game);
                        break;
                    }
                }
            }
        }
        foreach (GameModel game in playedGamesList)
        {
            if (game?.Players != null)
            {
                foreach (PlayerModel player in game.Players)
                {
                    if (player?.ID == CurrentPlayer.ID)
                    {
                        playedGamesFound.Add(game);
                        break;
                    }
                }
            }
        }

        return await Task.FromResult<(List<CurrentGameModel>, List<GameModel>)>((activeGamesFound, playedGamesFound));
    }
    private bool LoadGameCollectionsWithPlayerName(List<CurrentGameModel> activeGamesList, List<GameModel> playedGamesList)
    {
        void Populate()
        {
            ActiveGames.Clear();
            PlayedGames.Clear();
            foreach (var game in activeGamesList)
            {
                ActiveGames.Add(game);
            }
            foreach (var game in playedGamesList)
            {
                PlayedGames.Add(game);
            }
        }

        if (MainThread.IsMainThread)
        {
            Populate();
        }
        else
        {
            MainThread.BeginInvokeOnMainThread(Populate);
        }
        return true;
    }

    private bool CanExecuteRemoveCommand()
    {
        return CurrentPlayer is not null;
    }

    private bool CanExecuteUpdatePlayerNameCommand()
    {
        return CurrentPlayer is not null &&
               !string.IsNullOrWhiteSpace(NewPlayerNameText) &&
               !string.Equals(NewPlayerNameText.Trim(), CurrentPlayer.PlayerName, StringComparison.Ordinal);
    }
    #endregion
}
