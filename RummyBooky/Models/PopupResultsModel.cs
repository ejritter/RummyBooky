namespace RummyBooky.Models;

public partial class PopupResultsModel : BaseModel
{
    [ObservableProperty]
    public partial bool Confirmed { get; set; } = false;

    [ObservableProperty]
    public partial PlayerModel? SelectedWinner { get; set; } = null;

    [ObservableProperty]
    public partial GameStatus GameState { get; set; } = GameStatus.Unknown;
}
