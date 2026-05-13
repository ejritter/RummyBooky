using RummyBooky.Models;

namespace RummyBooky.ViewModels;

public partial class LeaderboardViewModel(IPopupService popupService, GameService gameService)
    : BaseViewModel(popupService, gameService)
{

    public ObservableCollection<LeaderboardPlayerModel> TopPlayers { get; } = [];

    [ObservableProperty]
    public partial string HeaderText { get; set; } = "Leaderboard";

    [ObservableProperty]
    public partial bool DisplayLeaderboard { get; set; } = false;


    [RelayCommand]
    private async Task EditPlayer(object? sender)
    {
        if (sender is PlayerModel playerModel)
        {
            await Shell.Current.GoToAsync(nameof(EditPlayerPage), animate: true, parameters: new Dictionary<string, object>
            {
                [nameof(EditPlayerViewModel.CurrentPlayer)] = playerModel
            });
        }

    }
    [RelayCommand]
    private async Task Appearing()
    {
        await LoadLeaderboardAsync();
        await FLagDisplayLeaderboardBool();
        SetHeaderText();
    }

    private async Task LoadLeaderboardAsync()
    {
        var players = await _gameService.GetTopPlayersAsync(10);
        TopPlayers.Clear();
        foreach (var player in players)
        {
            TopPlayers.Add(new LeaderboardPlayerModel
            {
                Player = player,
            });
        }

    }

    private async Task<bool> FLagDisplayLeaderboardBool()
    {
        var results = false;
        if (MainThread.IsMainThread)
        {
            results = DisplayLeaderboard = TopPlayers.Count > 0;
        }
        else
        {
            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                results = DisplayLeaderboard = TopPlayers.Count > 0;
            });
        }
        return results;
    }

    private void SetHeaderText()
    {
        if (DisplayLeaderboard)
            HeaderText = "Leaderboard";
        else
            HeaderText = "No player stats to provide. Please play a game.";
    }
}
