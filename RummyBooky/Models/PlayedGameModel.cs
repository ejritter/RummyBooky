namespace RummyBooky.Models;

public partial class PlayedGameModel : CurrentGameModel
{
    [ObservableProperty]
    public partial PlayerModel? WinningPlayer { get; set; } = null;

    [ObservableProperty]
    public partial GameStatus GameState { get; set; }

    public DateTime GameEnd { get; set; } = DateTime.Now;
}
