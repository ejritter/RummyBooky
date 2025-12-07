namespace RummyBooky.Models;

public partial class PlayedGameModel : CurrentGameModel
{
    public PlayerModel? WinningPlayer { get; init; } = null;
    public GameStatus GameState { get; init; }

    public DateTime GameEnd {get; init;}  = DateTime.Now;
}
