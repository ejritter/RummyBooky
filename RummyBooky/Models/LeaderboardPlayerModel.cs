namespace RummyBooky.Models;

public partial class LeaderboardPlayerModel : BaseModel
{
    [ObservableProperty]
    public partial int Rank { get; set; } = 0;

    [ObservableProperty]
    public partial PlayerModel? Player { get; set; } = null;
}
