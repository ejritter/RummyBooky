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
        //CurrentGameStatus == draw. 
        //allow user to select draw option or select a winner.
        DisplayWinners = CurrentGameStatus == GameStatus.Draw;
        DisplayWinnerButton = CurrentGameStatus == GameStatus.Draw;

        //if gamestatus is NOT won then false.
        DisplayOkayButton = CurrentGameStatus == GameStatus.Won;

        PopupResults = new PopupResultsModel();
    }

    [ObservableProperty]
    public partial PopupResultsModel? PopupResults{ get; set; } = null;

    [ObservableProperty]
    public partial bool DisplayOkayButton { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayWinnerButton { get; set; } = false;

    [ObservableProperty]
    public partial GameStatus CurrentGameStatus { get; set; } = GameStatus.Unknown;

    [ObservableProperty]
    public partial PlayerModel? SelectedPlayer { get; set; } = null;
    public ObservableCollection<PlayerModel> WinningPlayers { get; set; } = [];

    [ObservableProperty]
    public partial bool DisplayWinners { get; set; } = false;

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
        PopupResults.GameState = GameStatus.Won;
        PopupResults.SelectedWinner = SelectedPlayer;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    [RelayCommand]
    private async Task CancelClicked()
    {
        PopupResults.Confirmed = false;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    private bool CanExecuteConfirmWinner()
    {
        var results = false;
        if (SelectedPlayer == null)
            return results;
        else if (SelectedPlayer is not null)
            results = true;

        return results;
    }
}
