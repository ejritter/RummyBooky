


namespace RummyBooky.Services;

public class GameService
{
    public GameService()
    {
        _savedGamesFolder = Path.Combine(FileSystem.AppDataDirectory, "savedgames");
        if (!Directory.Exists(_savedGamesFolder))
        {
            Directory.CreateDirectory(_savedGamesFolder);
        }
    }
    private readonly string _savedGamesFolder = string.Empty;
    private readonly string[] _gameFiles = [];

    public NewGameModel GetNewGameModel()
    {
        return new NewGameModel();
    }
    public async Task<bool> AddPlayerToNewGameAsync(GameModel gameModel, string playerName)
    {
        var newPlayer = new PlayerModel { PlayerName = playerName };
        gameModel.Players.Add(newPlayer);
        return true;
    }

    public async Task<bool> RemovePlayerFromNewGameAsync(GameModel gameModel, PlayerModel playerModel)
    {
        var playerFound = gameModel.Players.FirstOrDefault(p => p.ID == playerModel.ID);
        if (playerModel is not null)
        {
            gameModel.Players.Remove(playerFound);
        }
        return true;
    }

    public async Task<bool> SetCurrentGameScoreLimitAsync(CurrentGameModel currentGame, int scoreLimit)
    {
        currentGame.ScoreLimit = scoreLimit;
        return true;
    }

    public async Task<bool> SetPlayerScoreCurrentGameScoreAsync(PlayerModel player)
    {
        var results = false;
        player.PlayerScore += int.Parse(player.PlayerScoreText);
        results = true;
        return results;

    }

    public async Task<bool> SetPlayersHighestScoredHandAsync(PlayerModel player)
    {
        var results = false;
        if (int.Parse(player.PlayerScoreText) > player.HighestScoredHand)
        {
            player.HighestScoredHand = player.PlayerScore;
        }
        results = true;
        return results;
    }


    public async Task<bool> SetPlayersLowestScoredHandAsync(PlayerModel player)
    {
        var results = false;
        if (int.Parse(player.PlayerScoreText) < player.LowestScoredHand)
        {
            player.LowestScoredHand = player.PlayerScore;
        }
        results = true;
        return results;
    }

    public async Task<bool> SetPlayersScoreTextToEmpty(PlayerModel player)
    {
        var results = false;
        player.PlayerScoreText = string.Empty;
        results = true;
        return results;
    }

    public async Task<bool> SetRoundHighestPlayedHandAsync(PlayerModel player, RoundModel currentRound)
    {
        var results = false;
        if (currentRound.PlayerHighestScoringHand is null)
        {
            currentRound.PlayerHighestScoringHand = player;
            currentRound.CurrentHighestScoredHandValue = int.Parse(player.PlayerScoreText);
            results = true;
        }
        else
        {
            if (int.Parse(player.PlayerScoreText) > currentRound.CurrentHighestScoredHandValue)
            {
                currentRound.PlayerHighestScoringHand = player;
                currentRound.CurrentHighestScoredHandValue = int.Parse(player.PlayerScoreText);
            }
            results = true;
        }
        return results;
    }

    public async Task<bool> SetRoundLowestPlayedHandAsync(PlayerModel player, RoundModel currentRound)
    {
        var results = false;
        if (currentRound.PlayerLowestScoringHand is null)
        {
            currentRound.PlayerLowestScoringHand = player;
            currentRound.CurrentLowestScoredHandValue = int.Parse(player.PlayerScoreText);
            results = true;
        }
        else
        {
            if (int.Parse(player.PlayerScoreText) < currentRound.CurrentLowestScoredHandValue)
            {
                currentRound.PlayerLowestScoringHand = player;
                currentRound.CurrentLowestScoredHandValue = int.Parse(player.PlayerScoreText);
            }
            results = true;
        }
        return results;
    }

    public async Task<(bool Results, List<PlayerModel> Winners, GameStatus GameStatus)> CheckForWinnersAsync(CurrentGameModel currentGame)
    {
        var results = (false, new List<PlayerModel>(),GameStatus.Unknown);
        var winners = currentGame
            .Players
            .Where(player => player.PlayerScore >= currentGame.ScoreLimit)
            .OrderByDescending(player => player.PlayerScore)
            .ToList();

        if (winners.Count == 0)
            return results;

        var highestScore = winners.Max(player => player.PlayerScore);
        winners = winners.Where(player => player.PlayerScore == highestScore).ToList();
        
        if (winners.Count > 1)//we have a draw
            results = (true, winners, GameStatus.Draw);
        else // winner
            results = (true, winners, GameStatus.Won);

        return results;
    }

    public async Task<bool> SetCurrentGameStatus(CurrentGameModel currentGame)
    {
        var results = false;
        return results;
    }


    public async Task<bool> SaveGameAsync(GameModel game)
    {
        var results = false;
        //var options = new JsonSerializerOptions
        //{
        //    ReferenceHandler = ReferenceHandler.Preserve,
        //    MaxDepth = 256,
        //    WriteIndented = true
        //};

        //string json = JsonSerializer.Serialize(game, options);

        var filePath = Path.Combine(_savedGamesFolder, $"game_{game.GameId}.json");
        var gameJson = JsonSerializer.Serialize(game);
        await File.WriteAllTextAsync(filePath, gameJson);
        results = true;
        return true;
    }

    public async Task<List<GameModel>> LoadActiveGamesAsync()
    {
        var activeGames = new List<GameModel>();
        var gameFiles = Directory.GetFiles(_savedGamesFolder, "game_*.json");
        //var options = new JsonSerializerOptions
        //{
        //    ReferenceHandler = ReferenceHandler.Preserve,
        //    MaxDepth = 256
        //};

        foreach (var file in gameFiles)
        {

            var gameJson = await File.ReadAllTextAsync(file);
            var game = JsonSerializer.Deserialize<GameModel>(gameJson);
            if (game is { IsGameActive: true })
            {
                activeGames.Add(game);
            }
        }
        return activeGames;
    }

    public async Task<List<GameModel>> LoadPlayedGamesAsync()
    {
        var playedGames = new List<GameModel>();
        var gameFiles = Directory.GetFiles(_savedGamesFolder, "game_*.json");
        //var options = new JsonSerializerOptions
        //{
        //    ReferenceHandler = ReferenceHandler.Preserve,
        //    MaxDepth = 256
        //};

        foreach (var file in gameFiles)
        {

            var gameJson = await File.ReadAllTextAsync(file);
            var game = JsonSerializer.Deserialize<GameModel>(gameJson);
            if (game is { IsGameActive: false })//only playedGames.
            {
                playedGames.Add(game);
            }
        }
        return playedGames;
    }

    public async Task<bool> SetRoundLeadingPlayerAsync(PlayerModel player, RoundModel currentRound)
    {
        var results = false;
        if (currentRound.LeadingPlayer is null)
        {
            currentRound.LeadingPlayer = player;
            results = true;
        }
        else
        {
            if (player.PlayerScore > currentRound.LeadingPlayer.PlayerScore)
            {
                currentRound.LeadingPlayer = player;
            }
            results = true;
        }
        return results;
    }

    public async Task<bool> SetRoundPlayersScoredHandsAsync(PlayerModel player, RoundModel roundModel)
    {
        var results = false;
        roundModel.PlayersScoredHandThisRound.Add(player);
        results = true;
        return results;
    }
}
