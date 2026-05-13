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
    private Dictionary<Guid, PlayerModel> _allPlayers = [];

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
        if (int.TryParse(player.PlayerScoreText, out var scored) && scored > player.HighestScoredHand)
        {
            player.HighestScoredHand = scored;
        }
        results = true;
        return results;
    }


    public async Task<bool> SetPlayersLowestScoredHandAsync(PlayerModel player)
    {
        var results = false;
        if (int.TryParse(player.PlayerScoreText, out var scored) && scored < player.LowestScoredHand)
        {
            player.LowestScoredHand = scored;
        }
        results = true;
        return results;
    }

    public async Task<bool> SetPlayersScoreTextToEmptyAsync(PlayerModel player)
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
        var results = (false, new List<PlayerModel>(), GameStatus.Unknown);
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

    //public async Task<bool> SetCurrentGameStatusAsync(CurrentGameModel currentGame)
    //{
    //    var results = false;
    //    return results;
    //}


    public async Task<bool> SaveGameAsync(GameModel game)
    {
        var results = false;
        //var options = new JsonSerializerOptions
        // {
        //     ReferenceHandler = ReferenceHandler.Preserve,
        //     MaxDepth = 256,
        //     WriteIndented = true
        // };

        //string json = JsonSerializer.Serialize(game, options);

        var filePath = Path.Combine(_savedGamesFolder, $"game_{game.GameId}.json");
        var gameJson = JsonSerializer.Serialize(game);
        await File.WriteAllTextAsync(filePath, gameJson);
        results = true;
        return results;
    }


    private IEnumerable<string> EnumerateGameFiles()
    {
        return Directory.EnumerateFiles(_savedGamesFolder, "game_*.json");
    }
    public async Task<List<CurrentGameModel>> LoadActiveGamesAsync()
    {
        var activeGames = new List<CurrentGameModel>();
        //var gameFiles = Directory.GetFiles(_savedGamesFolder, "game_*.json");
        //var options = new JsonSerializerOptions
        // {
        //     ReferenceHandler = ReferenceHandler.Preserve,
        //     MaxDepth = 256
        // };

        foreach (var file in EnumerateGameFiles())
        {

            var gameJson = await File.ReadAllTextAsync(file);
            var game = JsonSerializer.Deserialize<GameModel>(gameJson);
            if (game is { IsGameActive: true })
            {
                activeGames.Add((CurrentGameModel)game);
            }
        }
        return activeGames;
    }

    public async Task<List<GameModel>> LoadPlayedGamesAsync()
    {
        var playedGames = new List<GameModel>();
        //gameFiles = Directory.GetFiles(_savedGamesFolder, "game_*.json");
        var options = new JsonSerializerOptions
        {
            ReferenceHandler = ReferenceHandler.Preserve,
            MaxDepth = 256
        };
        foreach (var file in EnumerateGameFiles())
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

    public async Task<bool> SetGamesDealerAsync(GameModel currentGame, PlayerModel playerModel)
    {
        var results = false;
        foreach (var player in currentGame.Players)
        {
            if (player.ID == playerModel.ID)
            {

                //if current player is the dealer already, then reset all playersDictionary to false.
                if (player.IsDealer == true)
                    player.IsDealer = false;
                else
                    player.IsDealer = true;
                results = true;
            }
            else
            {
                player.IsDealer = false;
            }
        }
        return results;
    }

    /// <summary>
    /// This is used if no dealer is set by the player
    /// </summary>
    /// <param name="currentGame">The Current Game Model that will get assigned a random dealer.</param>
    /// <returns></returns>
    public async Task<bool> SetRandomDealerForCurrentGameAsync(GameModel currentGame)
    {
        var results = false;
        var playerCount = currentGame.Players.Count;
        if (playerCount == 0) return results;
        else
        {
            var randomIndex = new Random().Next(0, playerCount);
            var chosenPlayer = currentGame.Players[randomIndex];
            chosenPlayer.IsDealer = true;
            results = true;
            return results;
        }
    }

    /// <summary>
    /// This is used for when a new round starts. Do not invoke on Round 1.
    /// </summary>
    /// <param name="currentGame"></param>
    /// <returns></returns>
    public async Task<bool> SetNextDealerForNewRoundAsync(GameModel currentGame)
    {
        var results = false;
        var currentDealerIndex = currentGame
            .Players
            .IndexOf(currentGame
                        .Players
                        .First(p => p.IsDealer));

        if (currentDealerIndex == -1) return results;

        var nextDealerIndex = (currentDealerIndex + 1) % currentGame.Players.Count;
        currentGame.Players[currentDealerIndex].IsDealer = false; //no longer the dealer.
        currentGame.Players[nextDealerIndex].IsDealer = true; // next dealer.
        results = true;
        return true;
    }


    public async Task<bool> LoadAllPlayersDictionaryAsync()
    {
        var results = false;
        _allPlayers = new Dictionary<Guid, PlayerModel>();

        foreach (var filePath in EnumerateGameFiles())
        {
            var gameJson = await File.ReadAllTextAsync(filePath);
            var tempGameModel = JsonSerializer.Deserialize<GameModel>(gameJson);

            if (tempGameModel is not { Players: { } playerList })
                continue;

            var isPlayedGame = tempGameModel is PlayedGameModel;

            foreach (var player in playerList)
            {
                if (!_allPlayers.TryGetValue(player.ID, out var agg))
                {
                    agg = ToRosterPlayer(player);
                    _allPlayers.Add(player.ID, agg);
                }

                if (isPlayedGame)
                {
                    agg.TotalGamesPlayed += 1;
                    var playedGame = (PlayedGameModel)tempGameModel;

                    if (playedGame.GameState is GameStatus.Won)
                    {
                        agg.LifetimeScore += player.PlayerScore;
                        if (playedGame.WinningPlayer?.ID == player.ID)
                            agg.GamesWon += 1;
                        else
                            agg.GamesLost += 1;

                        UpdatePlayerAggregateHighestLowestHands(aggregate: agg, source: player);
                    }
                    else if (playedGame.GameState is GameStatus.Draw)
                    {
                        //Draw. Just calculate lifetime score
                        //no one lost or won.
                        agg.LifetimeScore += player.PlayerScore;
                        agg.GameDraws += 1;
                        UpdatePlayerAggregateHighestLowestHands(aggregate: agg, source: player);
                    }
                    else if (playedGame.GameState is GameStatus.Forfeit)
                    {
                        agg.GamesForfeit += 1;
                    }
                }
            }
            results = true;
        }
        return results;
    }

    private static void UpdatePlayerAggregateHighestLowestHands(PlayerModel aggregate, PlayerModel source)
    {
        if (source.HighestScoredHand > aggregate.HighestScoredHand)
        {
            aggregate.HighestScoredHand = source.HighestScoredHand;
        }
        if (source.LowestScoredHand < aggregate.LowestScoredHand)
        {
            aggregate.LowestScoredHand = source.LowestScoredHand;
        }
    }
    public async Task<PlayerModel[]> GetAllPlayerModelsArray()
    {
        // Build a global ranking map from full players dictionary
        var rankMap = BuildRankMap();

        // Return roster copies ordered by name, but annotate with global rank and symbols
        var roster = _allPlayers.Values
            .OrderBy(p => p.PlayerName)
            .Select(p =>
            {
                var r = ToRosterPlayer(p);
                if (rankMap.TryGetValue(p.ID, out var rnk))
                {
                    r.Rank = rnk;
                    CardRanks cardRank = Enum.IsDefined(typeof(CardRanks), rnk) ? (CardRanks)rnk : CardRanks.NotAssigned;
                    r.CardRank = cardRank;
                    r.CardRankSymbol = RummyBooky.Converters.CardRankConverter.ConvertRankToSymbol(cardRank);
                }
                return r;
            })
            .ToArray();

        return roster;
    }

    public async Task<bool> AddExistingPlayerModelToNewGameAsync(NewGameModel gameModelTemplate, PlayerModel player)
    {
        var results = false;
        if (!_allPlayers.TryGetValue(player.ID, out var profile))
        {
            throw new InvalidDataException($"Player not found: {player.PlayerName} ID: {player.ID}");
        }

        gameModelTemplate.Players.Add(ToRosterPlayer(profile));
        results = true;
        return results;
    }

    public async Task<bool> SetFinalStatsOfDrawGame(PlayedGameModel playedGame)
    {
        var results = false;
        foreach (var player in playedGame.Players)
        {
            player.TotalGamesPlayed += 1;
            player.GameDraws += 1;
            results = true;
        }
        return results;
    }

    public async Task<bool> SetFinalStatsOfForfeitGame(PlayedGameModel playedGame)
    {
        var results = false;
        foreach (var player in playedGame.Players)
        {
            player.TotalGamesPlayed += 1;
            player.GamesForfeit += 1;
            results = true;
        }
        return results;
    }

    public async Task<bool> SetFinalStatsOfPlayedFinishedGame(PlayedGameModel playedGame)
    {
        var results = false;
        foreach (var player in playedGame.Players)
        {
            var historicalPlayerModel = _allPlayers[player.ID];
            historicalPlayerModel.TotalGamesPlayed += 1;
            historicalPlayerModel.LifetimeScore += player.PlayerScore;
            if (playedGame.WinningPlayer.ID == historicalPlayerModel.ID)
                historicalPlayerModel.GamesWon += 1;
            else
                historicalPlayerModel.GamesLost += 1;
        }
        results = true;
        return results;
    }

    private static PlayerModel ToRosterPlayer(PlayerModel source)
    {
        // Keep identity and lifetime stats; clear per-game fields
        return new PlayerModel
        {
            ID = source.ID,
            PlayerName = source.PlayerName,
            PlayerCreatedDate = source.PlayerCreatedDate,
            LifetimeScore = source.LifetimeScore,
            TotalGamesPlayed = source.TotalGamesPlayed,
            GamesWon = source.GamesWon,
            GamesLost = source.GamesLost,
            GameDraws = source.GameDraws,
            GamesForfeit = source.GamesForfeit,
            HighestScoredHand = source.HighestScoredHand,
            LowestScoredHand = source.LowestScoredHand,
            PlayerScore = 0,
            PlayerScoreText = string.Empty,
            IsDealer = false
        };
    }

    private static PlayerModel FreshGameInstance(PlayerModel profile)
    {
        // New instance for this game; same identity, per-game fields reset
        return new PlayerModel
        {
            ID = profile.ID,
            PlayerName = profile.PlayerName,
            PlayerCreatedDate = profile.PlayerCreatedDate,
            LifetimeScore = profile.LifetimeScore,
            TotalGamesPlayed = profile.TotalGamesPlayed,
            GamesWon = profile.GamesWon,
            GamesLost = profile.GamesLost,
            GameDraws = profile.GameDraws,
            GamesForfeit = profile.GamesForfeit,
            HighestScoredHand = 0,
            LowestScoredHand = 0,
            PlayerScore = 0,
            PlayerScoreText = string.Empty,
            IsDealer = profile.IsDealer
        };
    }

    public async Task<bool> CreateFreshPlayerTemplatesForCurrentGame(NewGameModel gameModelTemplate)
    {
        var results = false;
        var players = gameModelTemplate.Players.ToList<PlayerModel>();
        gameModelTemplate.Players.Clear();
        foreach (var player in players)
        {
            gameModelTemplate.Players.Add(FreshGameInstance(player));
        }
        results = true;
        return results;
    }

    public async Task<List<PlayerModel>> GetTopPlayersAsync(int count = 10)
    {
        // Build rank map from the full players collection
        var rankMap = BuildRankMap();

        // Get the top players by score and return roster copies annotated with rank info
        var top = _allPlayers.Values
            .OrderByDescending(p => p.LifetimeScore)
            .ThenByDescending(p => p.GamesWon)
            .ThenBy(p => p.PlayerName)
            .Take(count)
            .Select(p =>
            {
                var r = ToRosterPlayer(p);
                if (rankMap.TryGetValue(p.ID, out var rnk))
                {
                    r.Rank = rnk;
                    CardRanks cardRank = Enum.IsDefined(typeof(CardRanks), rnk) ? (CardRanks)rnk : CardRanks.NotAssigned;
                    r.CardRank = cardRank;
                    r.CardRankSymbol = RummyBooky.Converters.CardRankConverter.ConvertRankToSymbol(cardRank);
                }
                return r;
            })
            .ToList();

        return top;
    }

    /// <summary>
    /// Builds a ranking map from player ID -> rank (1 = highest lifetime score)
    /// </summary>
    /// <returns></returns>
    private Dictionary<Guid, int> BuildRankMap()
    {
        var map = new Dictionary<Guid, int>();
        var ordered = _allPlayers.Values
            .OrderByDescending(p => p.LifetimeScore)
            .ThenByDescending(p => p.GamesWon)
            .ThenBy(p => p.PlayerName)
            .ToList();

        int rank = 1;
        foreach (var p in ordered)
        {
            map[p.ID] = rank++;
        }

        return map;
    }

    public async Task<bool> RemovePlayerFromHistory(PlayerModel removePlayer)
    {
        var results = false;
        var gameFiles = EnumerateGameFiles().ToList();

        foreach (var filePath in gameFiles)
        {
            var gameJson = await File.ReadAllTextAsync(filePath);
            var game = JsonSerializer.Deserialize<GameModel>(gameJson);

            if (game is null)
                continue;

            var changed = RemovePlayerReferencesFromGame(game, removePlayer.ID);
            if (!changed)
                continue;

            // If the game is no longer valid, remove it entirely.
            if (game.Players.Count < IntConstants.MinimumPlayerCount)
            {
                File.Delete(filePath);
                results = true;
                continue;
            }

            if (game is PlayedGameModel playedGame)
            {
                var rebuiltPlayedGame = RebuildPlayedGameAfterPlayerRemoval(playedGame);

                // No remaining valid outcome for this historical game.
                if (rebuiltPlayedGame is null)
                {
                    File.Delete(filePath);
                    results = true;
                    continue;
                }

                await SaveGameAsync(rebuiltPlayedGame);
            }
            else
            {
                EnsureSingleDealer(game);
                await SaveGameAsync(game);
            }

            results = true;
        }

        if (results)
        {
            await LoadAllPlayersDictionaryAsync();
        }

        return results;
    }

    private static bool RemovePlayerReferencesFromGame(GameModel game, Guid playerId)
    {
        var changed = false;

        var playerInGame = game.Players.FirstOrDefault(p => p.ID == playerId);
        if (playerInGame is not null)
        {
            game.Players.Remove(playerInGame);
            changed = true;
        }

        foreach (var round in game.Round)
        {
            if (RemovePlayerReferencesFromRound(round, playerId))
            {
                changed = true;
            }
        }

        return changed;
    }

    private static bool RenamePlayerReferencesInGame(GameModel game, Guid playerId, string newName)
    {
        var changed = false;

        var playerInGame = game.Players.FirstOrDefault(p => p.ID == playerId);
        if (playerInGame is not null)
        {
            playerInGame.PlayerName = newName;
            changed = true;
        }

        foreach (var round in game.Round)
        {
            if (RenamePlayerReferencesInRound(round, playerId, newName))
            {
                changed = true;
            }
        }

        return changed;
    }

    private static bool RenamePlayerReferencesInRound(RoundModel round, Guid playerId, string newName)
    {
        var changed = false;

        // Update any entries in the players-scored collection
        for (int i = 0; i < round.PlayersScoredHandThisRound.Count; i++)
        {
            var player = round.PlayersScoredHandThisRound[i];
            if (player.ID == playerId && player.PlayerName != newName)
            {
                player.PlayerName = newName;
                changed = true;
            }
        }

        // Update LeadingPlayer if it's the same logical player (may be a separate instance after deserialization)
        if (round.LeadingPlayer?.ID == playerId && round.LeadingPlayer.PlayerName != newName)
        {
            round.LeadingPlayer.PlayerName = newName;
            changed = true;
        }

        // Update highest/lowest round references similarly
        if (round.PlayerHighestScoringHand?.ID == playerId && round.PlayerHighestScoringHand.PlayerName != newName)
        {
            round.PlayerHighestScoringHand.PlayerName = newName;
            changed = true;
        }

        if (round.PlayerLowestScoringHand?.ID == playerId && round.PlayerLowestScoringHand.PlayerName != newName)
        {
            round.PlayerLowestScoringHand.PlayerName = newName;
            changed = true;
        }

        return changed;
    }
    private static bool RemovePlayerReferencesFromRound(RoundModel round, Guid playerId)
    {
        var changed = false;

        for (int i = round.PlayersScoredHandThisRound.Count - 1; i >= 0; i--)
        {
            if (round.PlayersScoredHandThisRound[i].ID == playerId)
            {
                round.PlayersScoredHandThisRound.RemoveAt(i);
                changed = true;
            }
        }

        if (round.LeadingPlayer?.ID == playerId)
        {
            round.LeadingPlayer = null;
            changed = true;
        }

        if (round.PlayerHighestScoringHand?.ID == playerId)
        {
            round.PlayerHighestScoringHand = null;
            round.CurrentHighestScoredHandValue = int.MinValue;
            changed = true;
        }

        if (round.PlayerLowestScoringHand?.ID == playerId)
        {
            round.PlayerLowestScoringHand = null;
            round.CurrentLowestScoredHandValue = int.MaxValue;
            changed = true;
        }

        return changed;
    }


    public async Task<bool> UpdatePlayerNameHistory(PlayerModel renamePlayer, string newName)
    {
        var results = false;
        var gameFiles = EnumerateGameFiles().ToList();

        foreach (var filePath in gameFiles)
        {
            var gameJson = await File.ReadAllTextAsync(filePath);
            var game = JsonSerializer.Deserialize<GameModel>(gameJson);

            if (game is null)
                continue;

            var changed = RenamePlayerReferencesInGame(game, renamePlayer.ID, newName);
            if (!changed)
                continue;


            results = await SaveGameAsync(game);
        }

        if (results)
        {
            await LoadAllPlayersDictionaryAsync();
        }

        return results;
    }
    private static void EnsureSingleDealer(GameModel game)
    {
        var dealers = game.Players.Where(p => p.IsDealer).ToList();
        if (dealers.Count == 1)
            return;

        foreach (var player in game.Players)
        {
            player.IsDealer = false;
        }

        if (game.Players.Count > 0)
        {
            game.Players[0].IsDealer = true;
        }
    }

    private static PlayedGameModel? RebuildPlayedGameAfterPlayerRemoval(PlayedGameModel playedGame)
    {
        if (playedGame.Players.Count < IntConstants.MinimumPlayerCount)
            return null;

        if (playedGame.GameState is GameStatus.Forfeit)
        {
            return CreatePlayedGameCopy(
                playedGame,
                GameStatus.Forfeit,
                null);
        }

        var winners = playedGame
            .Players
            .Where(player => player.PlayerScore >= playedGame.ScoreLimit)
            .OrderByDescending(player => player.PlayerScore)
            .ToList();

        // After removing the player, the saved historical result no longer makes sense.
        if (winners.Count == 0)
            return null;

        var highestScore = winners.Max(player => player.PlayerScore);
        winners = winners.Where(player => player.PlayerScore == highestScore).ToList();

        return winners.Count > 1
            ? CreatePlayedGameCopy(playedGame, GameStatus.Draw, null)
            : CreatePlayedGameCopy(playedGame, GameStatus.Won, winners[0]);
    }

    private static PlayedGameModel CreatePlayedGameCopy(
        PlayedGameModel source,
        GameStatus gameState,
        PlayerModel? winningPlayer)
    {
        return new PlayedGameModel
        {
            GameId = source.GameId,
            Players = source.Players,
            IsGameActive = false,
            IsGameFinished = true,
            Round = source.Round,
            GameEnd = source.GameEnd,
            GameStart = source.GameStart,
            ScoreLimit = source.ScoreLimit,
            GameState = gameState,
            WinningPlayer = winningPlayer
        };
    }
}
