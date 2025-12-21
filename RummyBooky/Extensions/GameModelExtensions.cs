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
        if (gameState is GameStatus.Forfeit)
        {
            
            foreach (var player in currentGame.Players)
            {
                player.HighestScoredHand = 0;
                player.LowestScoredHand = int.MaxValue;
                player.PlayerScoreText = string.Empty;
                player.PlayerScore = 0;
            }

            foreach (var currentRound in currentGame.Round)
            {
                currentRound.PlayerHighestScoringHand = null;
                currentRound.CurrentHighestScoredHandValue = int.MinValue;
                currentRound.PlayerLowestScoringHand = null;
                currentRound.CurrentLowestScoredHandValue = int.MaxValue;
                currentRound.PlayersScoredHandThisRound.Clear(); 
            }
        }
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
            PlayerLowestScoringHand = last.PlayerLowestScoringHand,
            CurrentLowestScoredHandValue = last.CurrentLowestScoredHandValue
        };
        currentGame.Round.Add(newRoundTemplate);
        return currentGame;
    }
}
