namespace RummyBooky.Models;

public partial class PlayerModel : BaseModel
{
    public PlayerModel()
    {
        ImageSource = 
            CurrentTheme == AppTheme.Dark ? "spade_pink.png" : "spade_deepred.png";
    }

    [ObservableProperty]
    public partial int Rank { get; set; } = 0;

    [ObservableProperty]
    public partial string PlayerName { get; set; } = string.Empty;

    public DateTime PlayerCreatedDate { get; init; } = DateTime.Now;

    [ObservableProperty]
    public partial string PlayerScoreText { get; set; } = string.Empty;

    [ObservableProperty]
    public partial int PlayerScore { get; set; } = 0;

    public Guid ID { get; init; } = Guid.NewGuid();

    [ObservableProperty]
    public partial double LifetimeScore { get; set; } = 0;

    [ObservableProperty]
    public partial double TotalGamesPlayed { get; set; } = 0;

    [ObservableProperty]
    public partial double GamesWon { get; set; } = 0;

    [ObservableProperty]
    public partial double GamesLost { get; set; } = 0;

    [ObservableProperty]
    public partial double GamesForfeit { get; set; } = 0;

    [ObservableProperty]
    public partial double GameDraws { get; set; } = 0;

    [ObservableProperty]
    public partial int HighestScoredHand { get; set; } = int.MinValue;

    [ObservableProperty]
    public partial int LowestScoredHand { get; set; } = int.MaxValue;

    [ObservableProperty]
    public partial bool IsDealer { get; set; } = false;

    [ObservableProperty]
    public partial string ImageSource { get; set; } = "";

    [ObservableProperty]
    public partial CardRanks CardRank { get; set; } = CardRanks.NotAssigned;

    [ObservableProperty]
    public partial string CardRankSymbol { get; set; } = string.Empty;

    [ObservableProperty]
    public partial bool IsNewPlayer { get; set; } = false;

    public string PlayerTypeIconSource => IsNewPlayer
        ? (CurrentTheme == AppTheme.Dark ? "player_new_dark.png" : "player_new_light.png")
        : (CurrentTheme == AppTheme.Dark ? "player_existing_dark.png" : "player_existing_light.png");

    partial void OnIsNewPlayerChanged(bool value)
    {
        OnPropertyChanged(nameof(PlayerTypeIconSource));
    }
}
