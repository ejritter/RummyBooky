using CommunityToolkit.Maui.Core;

namespace RummyBooky.ViewModels;

public partial class GeneralPopupViewModel(IPopupService popupService) : BasePopupViewModel(popupService)
{
    public override void ApplyQueryAttributes(IDictionary<string, object> query)
    {
        base.ApplyQueryAttributes(query);
        WinningPlayers.Clear();
        if (query.TryGetValue("players", out var playersList) &&
            playersList is IEnumerable<PlayerModel> players)
        {
            foreach (var player in players)
            {
                WinningPlayers.Add(player);
            }
        }
        CurrentGameStatus = query.TryGetValue("gameStatus", out var gs) && gs is GameStatus status
            ? status
            : GameStatus.Unknown;
        
        bool hasPlayers = WinningPlayers.Count > 0;
        DisplayWinners = hasPlayers;
        DisplayWinnerButton = hasPlayers;
        DisplayDrawButton = CurrentGameStatus == GameStatus.Draw;
        ConfirmButtonText = CurrentGameStatus == GameStatus.Draw ? "Winner" : "Select";

        if (hasPlayers)
        {
            DisplayOkayButton = false;
            DisplayQuitButton = false;
            DisplayCancelButton = true;
        }
        else if (CurrentGameStatus == GameStatus.Won)
        {
            DisplayOkayButton = true;
            DisplayQuitButton = false;
            DisplayCancelButton = false;
        }
        else
        {
            bool isQuitTitle = Title?.Contains("Quit", StringComparison.OrdinalIgnoreCase) == true;
            DisplayQuitButton = isQuitTitle;
            DisplayOkayButton = !isQuitTitle;
            DisplayCancelButton = isQuitTitle;
        }

        if (query.TryGetValue("showOkay", out var showOkayObj) && showOkayObj is bool showOkay)
            DisplayOkayButton = showOkay;
        if (query.TryGetValue("showCancel", out var showCancelObj) && showCancelObj is bool showCancel)
            DisplayCancelButton = showCancel;
        if (query.TryGetValue("showQuit", out var showQuitObj) && showQuitObj is bool showQuit)
            DisplayQuitButton = showQuit;

        if (query.TryGetValue("okayText", out var okayTextObj) && okayTextObj is string okayTxt)
            OkayButtonText = okayTxt;
        else
            OkayButtonText = "Okay";

        if (query.TryGetValue("cancelText", out var cancelTextObj) && cancelTextObj is string cancelTxt)
            CancelButtonText = cancelTxt;
        else
            CancelButtonText = "Cancel";

        if (query.TryGetValue("confirmText", out var confirmTextObj) && confirmTextObj is string confirmTxt)
            ConfirmButtonText = confirmTxt;

        PopupResults = new PopupResultsModel();
    }

    [ObservableProperty]
    public partial string OkayButtonText { get; set; } = "Okay";

    [ObservableProperty]
    public partial string CancelButtonText { get; set; } = "Cancel";

    [ObservableProperty]
    public partial string ConfirmButtonText { get; set; } = "Select";

    [ObservableProperty]
    public partial PopupResultsModel? PopupResults { get; set; } = null;

    [ObservableProperty]
    public partial bool DisplayQuitButton { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayOkayButton { get; set; } = false;

    [ObservableProperty]
    public partial bool DisplayCancelButton { get; set; } = false;

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
        PopupResults ??= new PopupResultsModel();
        PopupResults.Confirmed = true;
        PopupResults.GameState = GameStatus.Forfeit;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    [RelayCommand]
    private async Task OkayClicked()
    {
        PopupResults ??= new PopupResultsModel();
        PopupResults.Confirmed = true;
        PopupResults.GameState = CurrentGameStatus == GameStatus.Won ? GameStatus.Won : CurrentGameStatus;
        if (WinningPlayers.Count > 0)
        {
            PopupResults.SelectedWinner = WinningPlayers.First();
        }
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    [RelayCommand]
    private async Task DrawGame()
    {
        PopupResults ??= new PopupResultsModel();
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
        PopupResults ??= new PopupResultsModel();
        PopupResults.Confirmed = true;
        PopupResults.GameState = CurrentGameStatus == GameStatus.Unknown ? GameStatus.Unknown : GameStatus.Won;
        PopupResults.SelectedWinner = SelectedPlayer;
        await _popupService.ClosePopupAsync(Shell.Current, PopupResults);
    }

    [RelayCommand]
    private async Task CancelClicked()
    {
        PopupResults ??= new PopupResultsModel();
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

