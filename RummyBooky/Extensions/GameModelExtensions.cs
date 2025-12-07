using RummyBooky.Models;

namespace RummyBooky.Extensions;

public static class GameModelExtensions
{
    public static CurrentGameModel ConvertToCurrentGame(this NewGameModel newGame)
    {
        var currentGameModel = new CurrentGameModel
        {
            GameId = newGame.GameId,
            IsGameActive = newGame.IsGameActive,
            IsGameFinished = newGame.IsGameFinished,
            Round =
                { new RoundModel() { GameId = newGame.GameId} }//Brand new game. Should just be guid.
        };
        var sortedList = new List<PlayerModel>(newGame.Players);
        newGame.Players.Clear();
        foreach (var player in sortedList.OrderBy(p => p.PlayerName))
        {
            newGame.Players.Add(player);
        }
        currentGameModel.Players = newGame.Players;
        return currentGameModel;
    }

    public static PlayedGameModel ConvertToPlayedGame(this CurrentGameModel currentGame, GameStatus gameState, PlayerModel winningPlayer)
    {
        return new PlayedGameModel
        {
            GameId = currentGame.GameId,
            Players = currentGame.Players,
            IsGameActive = false,
            IsGameFinished = true,
            Round = currentGame.Round,
            GameEnd = DateTime.Now,
            GameStart = currentGame.GameStart,
            ScoreLimit = currentGame.ScoreLimit,
            GameState = gameState,
            WinningPlayer = winningPlayer
        };
    }

    public static CurrentGameModel CreateNextRoundTemplate(this CurrentGameModel currentGame)
    {
        var last = currentGame.Round.Last();

        var newRoundTemplate = new RoundModel
        {
            GameId = currentGame.GameId,
            LeadingPlayer = last.LeadingPlayer,
            PlayerHighestScoringHand = last.PlayerHighestScoringHand,
            CurrentHighestScoredHandValue = last.CurrentHighestScoredHandValue,
            // Preserve lowest-hand context into the next round as well.
            PlayerLowestScoringHand = last.PlayerLowestScoringHand,
            CurrentLowestScoredHandValue = last.CurrentLowestScoredHandValue
        };
        currentGame.Round.Add(newRoundTemplate);
        return currentGame;
    }
}
