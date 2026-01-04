namespace RummyBooky.Models;

public partial class LeaderboardPlayerModel : BaseModel
{
    [ObservableProperty]
    public partial PlayerModel? Player { get; set; } = null;
}
