namespace RummyBooky.Models;

public partial class CurrentGameModel : NewGameModel
{
    
    [ObservableProperty]
    public partial int ScoreLimit { get; set; } = 0;
   
    public DateTime GameStart { get; init; } = DateTime.Now;
}
