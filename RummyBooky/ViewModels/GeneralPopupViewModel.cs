using CommunityToolkit.Maui.Core;

namespace RummyBooky.ViewModels;

public partial class GeneralPopupViewModel(IPopupService popupService) : BasePopupViewModel(popupService)
{
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);
        if (query.TryGetValue("players", out var playersList) &&
            playersList is IEnumerable<PlayerModel> players)
        {
            foreach (var player in players)
            {
                WinningPlayers.Add(player);
            }
        }
        CurrentGameStatus = (GameStatus)query["gameStatus"];
        
        bool hasPlayers = WinningPlayers.Count > 0;
        DisplayWinners = hasPlayers;
        DisplayWinnerButton = hasPlayers;
        DisplayDrawButton = CurrentGameStatus == GameStatus.Draw;
        ConfirmButtonText = CurrentGameStatus == GameStatus.Draw ? "Winner" : "Select";

        DisplayOkayButton = CurrentGameStatus == GameStatus.Won;
        DisplayQuitButton = CurrentGameStatus == GameStatus.Unknown && !hasPlayers;
        PopupResults = new PopupResultsModel();
    }

    [ObservableProperty]
    public partial string ConfirmButtonText { get; set; } = "Select";

    [ObservableProperty]
    public partial PopupResultsModel? PopupResults { get; set; } = null;

    [ObservableProperty]
    public partial bool DisplayQuitButton { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayOkayButton { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayWinnerButton { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayDrawButton { get; set; } = false;

    [ObservableProperty]
    public partial GameStatus CurrentGameStatus { get; set; } = GameStatus.Unknown;

    [ObservableProperty]
    public partial PlayerModel? SelectedPlayer { get; set; } = null;
    public ObservableCollection<PlayerModel> WinningPlayers { get; set; } = [];

    [ObservableProperty]
    public partial bool DisplayWinners { get; set; } = false;
    [RelayCommand]
    private async Task QuitClicked()
    {
        PopupResults.Confirmed = true;
        PopupResults.GameState = GameStatus.Forfeit;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }
    [RelayCommand]
    private async Task OkayClicked()
    {
        PopupResults.Confirmed = true;
        PopupResults.GameState = GameStatus.Won;
        PopupResults.SelectedWinner = WinningPlayers.First();
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }
    [RelayCommand]
    private async Task DrawGame()
    {
        PopupResults.Confirmed = true;
        PopupResults.GameState = GameStatus.Draw;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    partial void OnSelectedPlayerChanged(PlayerModel? oldValue, PlayerModel? newValue)
    {
        CanExecuteConfirmWinner();
        ConfirmWinnerCommand.NotifyCanExecuteChanged();
    }
    [RelayCommand(CanExecute = nameof(CanExecuteConfirmWinner))]
    private async Task ConfirmWinner()
    {
        PopupResults.Confirmed = true;
        PopupResults.GameState = CurrentGameStatus == GameStatus.Unknown ? GameStatus.Unknown : GameStatus.Won;
        PopupResults.SelectedWinner = SelectedPlayer;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    [RelayCommand]
    private async Task CancelClicked()
    {
        PopupResults.Confirmed = false;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    [RelayCommand]
    private void SelectPlayer(PlayerModel player)
    {
        SelectedPlayer = player;
        ConfirmWinnerCommand.NotifyCanExecuteChanged();
    }

    private bool CanExecuteConfirmWinner()
    {
        return SelectedPlayer != null;
    }
}
