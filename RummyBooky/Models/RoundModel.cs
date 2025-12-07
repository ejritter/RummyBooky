namespace RummyBooky.Models;

public partial class RoundModel : BaseModel
{
    public Guid GameId { get; init; }

    [ObservableProperty]
    public partial PlayerModel? LeadingPlayer { get; set; } = null;

    [ObservableProperty]
    public partial PlayerModel? PlayerHighestScoringHand { get; set; } = null;

    // Allow negatives to be considered as “highest”; start at MinValue.
    [ObservableProperty]
    public partial int CurrentHighestScoredHandValue { get; set; } = int.MinValue;

    [ObservableProperty]
    public partial PlayerModel? PlayerLowestScoringHand { get; set; } = null;

    // Start at MaxValue so any real hand is lower.
    [ObservableProperty]
    public partial int CurrentLowestScoredHandValue { get; set; } = int.MaxValue;

    public ObservableCollection<PlayerModel> PlayersScoredHandThisRound { get; set; } = new();
}
