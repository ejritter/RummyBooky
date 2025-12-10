namespace RummyBooky.Models;

public partial class PlayerModel : BaseModel
{
    [ObservableProperty]
    public partial string PlayerName { get; set; } = string.Empty;

    [ObservableProperty]
    public partial string PlayerScoreText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PlayerScore { get; set; } = 0;

    public Guid ID { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    public partial double LifeTimeScore { get; set; } = 0;

    [ObservableProperty]
    public partial  double TotalGamesPlayed { get; set; } = 0;

    [ObservableProperty]
    public partial double GamesWon { get; set; } = 0;

    [ObservableProperty]
    public partial double GamesLost { get; set; } = 0;

    [ObservableProperty]
    public partial int HighestScoredHand { get; set; } = 0;

    [ObservableProperty]
    public partial int LowestScoredHand { get; set; } = 0;

    [ObservableProperty]
    public partial bool IsDealer { get; set; } = false;
}
