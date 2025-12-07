

namespace RummyBooky.Models;

[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]
[JsonDerivedType(typeof(NewGameModel), typeDiscriminator: "NewGame")]
[JsonDerivedType(typeof(CurrentGameModel), typeDiscriminator: "CurrentGame")]
[JsonDerivedType(typeof(PlayedGameModel), typeDiscriminator: "PlayedGame")]
public abstract partial class GameModel : BaseModel
{
    public Guid GameId { get; init; } = Guid.NewGuid();
    public ObservableCollection<PlayerModel> Players { get; set; } = new();
    [ObservableProperty]
    public  partial bool IsGameActive { get; set; } = true;
   [ObservableProperty] 
    public partial bool IsGameFinished { get; set; } = false;
    public ObservableCollection<RoundModel> Round { get; set; } = new();
}
