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
        
    }

    public ObservableCollection<PlayerModel> WinningPlayers { get; set; } = new();

    [ObservableProperty]
    public partial PlayerModel? SelectedWinner { get; set; } = null;

    [ObservableProperty]
    public partial GameStatus DrawGame { get; set; } = GameStatus.Draw;

    [RelayCommand]
    private async Task OkayClicked()
    {
        // Build a result object: if a winner is selected, use Won; otherwise use selected status (e.g., Draw)
        //var result = new PopupResult
        //{
        //    IsConfirmed = true,
        //    WinningPlayer = SelectedWinner,
        //    Status = SelectedWinner is not null ? GameStatus.Won : DrawGame
        //};

        await _popupService.ClosePopupAsync(Shell.Current, true);
    }

    [RelayCommand]
    private async Task CancelClicked()
    {
        await _popupService.ClosePopupAsync(Shell.Current, false);
    }
}
