using RummyBooky.Models;

namespace RummyBooky.ViewModels;

public partial class LeaderboardViewModel(IPopupService popupService, GameService gameService) 
    : BaseViewModel(popupService, gameService)
{
    public ObservableCollection<LeaderboardPlayerModel> TopPlayers { get; } = [];



    [RelayCommand]
    private async Task Appearing()
    {
        await LoadLeaderboardAsync();
    }

    private async Task LoadLeaderboardAsync()
    {
        var players = await _gameService.GetTopPlayersAsync(10);
        TopPlayers.Clear();
        int rank = 1;
        foreach (var player in players)
            TopPlayers.Add(new LeaderboardPlayerModel{Rank = rank++, Player= player});
    }
}
