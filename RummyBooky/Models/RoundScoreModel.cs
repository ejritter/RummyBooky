namespace RummyBooky.Models;

public partial class RoundScoreModel : BaseModel
{
    public Guid PlayerId { get; set; }

    [ObservableProperty]
    public partial int Score { get; set; } = 0;
}
