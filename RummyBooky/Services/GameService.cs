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

    public void RecalculateGameScores(GameModel game) => RecalculateGame(game);

    public void RecalculateGame(GameModel game)
    {
        if (game is null || game.Players.Count == 0)
            return;

        // 1. Reset player running totals
        foreach (var player in game.Players)
        {
            player.PlayerScore = 0;
            player.HighestScoredHand = int.MinValue;
            player.LowestScoredHand = int.MaxValue;
        }

        PlayerModel? latestLeader = null;
        PlayerModel? latestHighestPlayer = null;
        int latestHighestValue = int.MinValue;
        PlayerModel? latestLowestPlayer = null;
        int latestLowestValue = int.MaxValue;

        // 2. Iterate through each RoundModel in game.Round (1..N)
        for (int r = 0; r < game.Round.Count; r++)
        {
            var round = game.Round[r];

            // Sync RoundScores from PlayersScoredHandThisRound if RoundScores is empty
            if (round.RoundScores.Count == 0 && round.PlayersScoredHandThisRound.Count > 0)
            {
                foreach (var p in round.PlayersScoredHandThisRound)
                {
                    int s = 0;
                    if (!string.IsNullOrWhiteSpace(p.PlayerScoreText) && int.TryParse(p.PlayerScoreText, out var parsed))
                    {
                        s = parsed;
                    }
                    round.RoundScores.Add(new RoundScoreModel { PlayerId = p.ID, Score = s });
                }
            }

            bool isScoredRound = round.RoundScores.Count > 0;

            if (isScoredRound)
            {
                // Ensure every player has a RoundScore entry
                foreach (var player in game.Players)
                {
                    var scoreEntry = round.RoundScores.FirstOrDefault(rs => rs.PlayerId == player.ID);
                    if (scoreEntry is null)
                    {
                        scoreEntry = new RoundScoreModel { PlayerId = player.ID, Score = 0 };
                        round.RoundScores.Add(scoreEntry);
                    }
                }

                round.CurrentHighestScoredHandValue = int.MinValue;
                round.PlayerHighestScoringHand = null;
                round.CurrentLowestScoredHandValue = int.MaxValue;
                round.PlayerLowestScoringHand = null;

                foreach (var player in game.Players)
                {
                    var scoreEntry = round.RoundScores.First(rs => rs.PlayerId == player.ID);
                    int score = scoreEntry.Score;

                    player.PlayerScore += score;

                    if (score > player.HighestScoredHand)
                    {
                        player.HighestScoredHand = score;
                    }
                    if (score < player.LowestScoredHand)
                    {
                        player.LowestScoredHand = score;
                    }

                    if (score > round.CurrentHighestScoredHandValue)
                    {
                        round.CurrentHighestScoredHandValue = score;
                        round.PlayerHighestScoringHand = player;
                    }

                    if (score < round.CurrentLowestScoredHandValue)
                    {
                        round.CurrentLowestScoredHandValue = score;
                        round.PlayerLowestScoringHand = player;
                    }
                }

                round.LeadingPlayer = game.Players.OrderByDescending(p => p.PlayerScore).FirstOrDefault();

                latestLeader = round.LeadingPlayer;
                latestHighestPlayer = round.PlayerHighestScoringHand;
                latestHighestValue = round.CurrentHighestScoredHandValue;
                latestLowestPlayer = round.PlayerLowestScoringHand;
                latestLowestValue = round.CurrentLowestScoredHandValue;
            }
            else
            {
                round.LeadingPlayer = latestLeader;
                round.PlayerHighestScoringHand = latestHighestPlayer;
                round.CurrentHighestScoredHandValue = latestHighestValue;
                round.PlayerLowestScoringHand = latestLowestPlayer;
                round.CurrentLowestScoredHandValue = latestLowestValue;
            }
        }

        foreach (var player in game.Players)
        {
            if (player.HighestScoredHand == int.MinValue)
                player.HighestScoredHand = 0;
            if (player.LowestScoredHand == int.MaxValue)
                player.LowestScoredHand = 0;
        }
    }

    public NewGameModel GetNewGameModel()
    {
        return new NewGameModel();
    }
    public async Task<bool> AddPlayerToNewGameAsync(GameModel gameModel, string playerName)
    {
        var newPlayer = new PlayerModel
        {
            PlayerName = playerName,
            IsNewPlayer = true
        };
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
        if (int.TryParse(player.PlayerScoreText, out var scoreVal))
        {
            player.PlayerScore += scoreVal;
            results = true;
        }
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
        var gameJson = JsonSerializer.Serialize(game, typeof(GameModel));
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

        foreach (var file in EnumerateGameFiles())
        {
            try
            {
                var gameJson = await File.ReadAllTextAsync(file);
                var game = JsonSerializer.Deserialize<GameModel>(gameJson);
                if (game is CurrentGameModel current && current.IsGameActive && current.Players.Count >= IntConstants.MinimumPlayerCount)
                {
                    activeGames.Add(current);
                }
            }
            catch
            {
                // Ignore corrupted or incompatible legacy game files safely
            }
        }
        return activeGames;
    }

    public async Task<List<GameModel>> LoadPlayedGamesAsync()
    {
        var playedGames = new List<GameModel>();
        foreach (var file in EnumerateGameFiles())
        {
            try
            {
                var gameJson = await File.ReadAllTextAsync(file);
                var game = JsonSerializer.Deserialize<GameModel>(gameJson);
                if (game is { IsGameActive: false })
                {
                    playedGames.Add(game);
                }
            }
            catch
            {
                // Ignore corrupted or incompatible legacy game files safely
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
        if (currentGame?.Players == null || currentGame.Players.Count == 0) return false;

        var currentDealer = currentGame.Players.FirstOrDefault(p => p.IsDealer);
        if (currentDealer == null)
        {
            currentGame.Players[0].IsDealer = true;
            return true;
        }

        var currentDealerIndex = currentGame.Players.IndexOf(currentDealer);
        if (currentDealerIndex == -1) return false;

        var nextDealerIndex = (currentDealerIndex + 1) % currentGame.Players.Count;
        currentGame.Players[currentDealerIndex].IsDealer = false; //no longer the dealer.
        currentGame.Players[nextDealerIndex].IsDealer = true; // next dealer.
        return true;
    }


    public async Task<bool> LoadAllPlayersDictionaryAsync()
    {
        var results = false;
        _allPlayers = new Dictionary<Guid, PlayerModel>();

        foreach (var filePath in EnumerateGameFiles())
        {
            try
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
            catch
            {
                // Safely skip legacy or corrupted files missing type discriminators
            }
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

        var rosterPlayer = ToRosterPlayer(profile);
        rosterPlayer.IsNewPlayer = false;
        gameModelTemplate.Players.Add(rosterPlayer);
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
            ID = profile.ID == Guid.Empty ? Guid.NewGuid() : profile.ID,
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
        await LoadAllPlayersDictionaryAsync();

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
            try
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
            catch
            {
                // Safely skip legacy or corrupted files missing type discriminators
            }
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

        for (int i = round.RoundScores.Count - 1; i >= 0; i--)
        {
            if (round.RoundScores[i].PlayerId == playerId)
            {
                round.RoundScores.RemoveAt(i);
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
        if (renamePlayer is null || string.IsNullOrWhiteSpace(newName))
            return false;

        renamePlayer.PlayerName = newName;
        if (_allPlayers.TryGetValue(renamePlayer.ID, out var cachedPlayer))
        {
            cachedPlayer.PlayerName = newName;
        }

        var results = true;
        var gameFiles = EnumerateGameFiles().ToList();

        foreach (var filePath in gameFiles)
        {
            try
            {
                var gameJson = await File.ReadAllTextAsync(filePath);
                var game = JsonSerializer.Deserialize<GameModel>(gameJson);

                if (game is null)
                    continue;

                var changed = RenamePlayerReferencesInGame(game, renamePlayer.ID, newName);
                if (!changed)
                    continue;

                await SaveGameAsync(game);
            }
            catch
            {
                // Safely skip legacy or corrupted files missing type discriminators
            }
        }

        await LoadAllPlayersDictionaryAsync();
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
