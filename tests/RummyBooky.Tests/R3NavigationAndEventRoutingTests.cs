using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace RummyBooky.Tests;

public class MockPlayerModel
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string PlayerName { get; set; } = string.Empty;
    public int PlayerScore { get; set; }
    public double LifetimeScore { get; set; }
    public int TotalGamesPlayed { get; set; }
    public int GamesWon { get; set; }
    public int GamesLost { get; set; }
    public int GamesForfeit { get; set; }
    public int GameDraws { get; set; }
}

public class MockLeaderboardPlayerModel
{
    public MockPlayerModel Player { get; set; } = new();
    public int Rank { get; set; }
}

public class MockGameModel
{
    public Guid ID { get; set; } = Guid.NewGuid();
    public string GameName { get; set; } = string.Empty;
    public List<MockPlayerModel> Players { get; set; } = [];
}

public class MockCurrentGameModel : MockGameModel
{
    public int ScoreLimit { get; set; } = 500;
    public DateTime GameStart { get; set; } = DateTime.UtcNow;
}

public record NavigationCall(string Route, bool Animate, IDictionary<string, object> Parameters);

public class RoutingDecisionEngine
{
    public static (bool Handled, string ActionTaken, NavigationCall? NavCall, MockPlayerModel? UpdatedCurrentPlayer) RouteEditPlayer(
        MockPlayerModel? assignedPlayer,
        object? bindingContext,
        ICommand? boundCommand,
        bool isCurrentPageEditPlayerPage,
        Action<MockPlayerModel>? onUpdateCurrentPlayer,
        Action<NavigationCall>? onShellNavigate)
    {
        var targetPlayer = assignedPlayer ?? bindingContext as MockPlayerModel;
        if (targetPlayer is null)
        {
            return (false, "IgnoredNullPlayer", null, null);
        }

        if (boundCommand != null)
        {
            if (boundCommand.CanExecute(targetPlayer))
            {
                boundCommand.Execute(targetPlayer);
                return (true, "ExecutedBoundCommand", null, null);
            }
            return (false, "BoundCommandCannotExecute", null, null);
        }

        if (isCurrentPageEditPlayerPage)
        {
            onUpdateCurrentPlayer?.Invoke(targetPlayer);
            return (true, "UpdatedCurrentPlayerInPlace", null, targetPlayer);
        }

        var navCall = new NavigationCall(
            "EditPlayerPage",
            true,
            new Dictionary<string, object> { ["CurrentPlayer"] = targetPlayer }
        );
        onShellNavigate?.Invoke(navCall);
        return (true, "NavigatedViaShellFallback", navCall, null);
    }
}

public class MockEditPlayerViewModel
{
    public MockPlayerModel? CurrentPlayer { get; set; }
    public bool DisplayGames { get; set; }
    public bool DisplayPlayers { get; set; }
    public ObservableCollection<MockGameModel> ActiveGames { get; } = [];
    public ObservableCollection<MockGameModel> PlayedGames { get; } = [];
    public ObservableCollection<MockPlayerModel> AllPlayers { get; } = [];

    public void OnCurrentPlayerChanged(MockPlayerModel? newValue, List<MockCurrentGameModel> activeGamesSource, List<MockGameModel> playedGamesSource)
    {
        CurrentPlayer = newValue;
        if (newValue is not null)
        {
            DisplayGames = true;
            DisplayPlayers = false;
            LoadGameCollections(newValue, activeGamesSource, playedGamesSource);
        }
        else
        {
            DisplayGames = false;
            DisplayPlayers = true;
        }
    }

    public void PageLoaded(List<MockPlayerModel> allPlayersSource, List<MockCurrentGameModel> activeGamesSource, List<MockGameModel> playedGamesSource)
    {
        if (CurrentPlayer is not null)
        {
            DisplayPlayers = false;
            DisplayGames = true;
            LoadGameCollections(CurrentPlayer, activeGamesSource, playedGamesSource);
        }
        else
        {
            DisplayPlayers = true;
            DisplayGames = false;
            lock (_mainThreadLock)
            {
                AllPlayers.Clear();
                foreach (var p in allPlayersSource)
                {
                    AllPlayers.Add(p);
                }
            }
        }
    }

    public void LoadGameCollections(MockPlayerModel targetPlayer, List<MockCurrentGameModel> activeGamesSource, List<MockGameModel> playedGamesSource)
    {
        var (activeFound, playedFound) = IdentifyPlayerInGames(targetPlayer, activeGamesSource, playedGamesSource);
        PopulateCollections(activeFound, playedFound);
    }

    public (List<MockCurrentGameModel> activeFound, List<MockGameModel> playedFound) IdentifyPlayerInGames(
        MockPlayerModel? player,
        List<MockCurrentGameModel> activeGamesSource,
        List<MockGameModel> playedGamesSource)
    {
        var activeFound = new List<MockCurrentGameModel>();
        var playedFound = new List<MockGameModel>();

        if (player is null)
            return (activeFound, playedFound);

        foreach (var game in activeGamesSource)
        {
            if (game?.Players != null && game.Players.Any(p => p?.ID == player.ID))
            {
                activeFound.Add(game);
            }
        }

        foreach (var game in playedGamesSource)
        {
            if (game?.Players != null && game.Players.Any(p => p?.ID == player.ID))
            {
                playedFound.Add(game);
            }
        }

        return (activeFound, playedFound);
    }

    private readonly object _mainThreadLock = new();

    public void PopulateCollections(List<MockCurrentGameModel> activeGamesList, List<MockGameModel> playedGamesList)
    {
        lock (_mainThreadLock)
        {
            ActiveGames.Clear();
            PlayedGames.Clear();
            foreach (var game in activeGamesList)
            {
                ActiveGames.Add(game);
            }
            foreach (var game in playedGamesList)
            {
                PlayedGames.Add(game);
            }
        }
    }

    public bool CanExecuteRemoveCommand() => CurrentPlayer is not null;
}

public class R3NavigationAndEventRoutingTests
{
    [Fact]
    public void Context1_CardBoxViewExpandedList_FallbackRoutesToEditPlayerPage()
    {
        // Arrange: In CardBoxView expanded list, AssignedPlayerModel is bound, Command is null, not currently on EditPlayerPage
        var player = new MockPlayerModel { PlayerName = "Alice" };
        NavigationCall? capturedNav = null;

        // Act
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: player,
            bindingContext: null,
            boundCommand: null,
            isCurrentPageEditPlayerPage: false,
            onUpdateCurrentPlayer: null,
            onShellNavigate: nav => capturedNav = nav
        );

        // Assert
        Assert.True(result.Handled);
        Assert.Equal("NavigatedViaShellFallback", result.ActionTaken);
        Assert.NotNull(capturedNav);
        Assert.Equal("EditPlayerPage", capturedNav.Route);
        Assert.True(capturedNav.Animate);
        Assert.Same(player, capturedNav.Parameters["CurrentPlayer"]);
    }

    [Fact]
    public void Context2_NewGamePageCarousel_BoundCommandExecutesWithTargetPlayer()
    {
        // Arrange: In NewGamePage carousel, Command is bound to NewGameViewModel.EditPlayerCommand
        var player = new MockPlayerModel { PlayerName = "Bob" };
        MockPlayerModel? commandExecutedWith = null;

        var boundCommand = new RelayCommandStub<MockPlayerModel>(p =>
        {
            commandExecutedWith = p;
        });

        // Act
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: player,
            bindingContext: player,
            boundCommand: boundCommand,
            isCurrentPageEditPlayerPage: false,
            onUpdateCurrentPlayer: null,
            onShellNavigate: null
        );

        // Assert
        Assert.True(result.Handled);
        Assert.Equal("ExecutedBoundCommand", result.ActionTaken);
        Assert.Same(player, commandExecutedWith);
    }

    [Fact]
    public void Context3_LeaderboardPage_BoundCommandExecutesWithLeaderboardPlayer()
    {
        // Arrange: In LeaderboardPage, ItemTemplate binds AssignedPlayerModel="{Binding Player}" and Command="{Binding EditPlayerCommand}"
        var innerPlayer = new MockPlayerModel { PlayerName = "Charlie" };
        var leaderboardItem = new MockLeaderboardPlayerModel { Player = innerPlayer, Rank = 1 };
        MockPlayerModel? commandExecutedWith = null;

        var boundCommand = new RelayCommandStub<MockPlayerModel>(p =>
        {
            commandExecutedWith = p;
        });

        // Act: PlayerCardView receives leaderboardItem.Player as AssignedPlayerModel
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: leaderboardItem.Player,
            bindingContext: leaderboardItem.Player,
            boundCommand: boundCommand,
            isCurrentPageEditPlayerPage: false,
            onUpdateCurrentPlayer: null,
            onShellNavigate: null
        );

        // Assert
        Assert.True(result.Handled);
        Assert.Equal("ExecutedBoundCommand", result.ActionTaken);
        Assert.Same(innerPlayer, commandExecutedWith);
    }

    [Fact]
    public void Context4_EditPlayerPageAllPlayersList_UpdatesCurrentPlayerInPlace()
    {
        // Arrange: On EditPlayerPage, user clicks pencil on a card in AllPlayers list (Command is null, on EditPlayerPage)
        var player = new MockPlayerModel { PlayerName = "Diana" };
        MockPlayerModel? updatedInPlace = null;
        NavigationCall? capturedNav = null;

        // Act
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: player,
            bindingContext: player,
            boundCommand: null,
            isCurrentPageEditPlayerPage: true,
            onUpdateCurrentPlayer: p => updatedInPlace = p,
            onShellNavigate: nav => capturedNav = nav
        );

        // Assert
        Assert.True(result.Handled);
        Assert.Equal("UpdatedCurrentPlayerInPlace", result.ActionTaken);
        Assert.Same(player, updatedInPlace);
        Assert.Null(capturedNav); // Must NOT push a new Shell page
    }

    [Fact]
    public void Context5_StandaloneCard_ResolvesFromBindingContextAndNavigates()
    {
        // Arrange: Standalone card where AssignedPlayerModel is null, but BindingContext is PlayerModel
        var player = new MockPlayerModel { PlayerName = "Evan" };
        NavigationCall? capturedNav = null;

        // Act
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: null,
            bindingContext: player,
            boundCommand: null,
            isCurrentPageEditPlayerPage: false,
            onUpdateCurrentPlayer: null,
            onShellNavigate: nav => capturedNav = nav
        );

        // Assert
        Assert.True(result.Handled);
        Assert.Equal("NavigatedViaShellFallback", result.ActionTaken);
        Assert.NotNull(capturedNav);
        Assert.Same(player, capturedNav.Parameters["CurrentPlayer"]);
    }

    [Fact]
    public void EdgeCase_NullPlayer_SafelyIgnoredWithoutErrorOrNavigation()
    {
        // Arrange: Both AssignedPlayerModel and BindingContext are null
        NavigationCall? capturedNav = null;
        bool commandInvoked = false;

        var command = new RelayCommandStub<MockPlayerModel>(_ => commandInvoked = true);

        // Act
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: null,
            bindingContext: null,
            boundCommand: command,
            isCurrentPageEditPlayerPage: false,
            onUpdateCurrentPlayer: null,
            onShellNavigate: nav => capturedNav = nav
        );

        // Assert
        Assert.False(result.Handled);
        Assert.Equal("IgnoredNullPlayer", result.ActionTaken);
        Assert.False(commandInvoked);
        Assert.Null(capturedNav);
    }

    [Fact]
    public void EdgeCase_BoundCommand_CanExecuteFalse_DoesNotExecuteOrFallback()
    {
        // Arrange: Command CanExecute returns false
        var player = new MockPlayerModel { PlayerName = "Frank" };
        bool commandInvoked = false;
        NavigationCall? capturedNav = null;

        var command = new RelayCommandStub<MockPlayerModel>(_ => commandInvoked = true, _ => false);

        // Act
        var result = RoutingDecisionEngine.RouteEditPlayer(
            assignedPlayer: player,
            bindingContext: player,
            boundCommand: command,
            isCurrentPageEditPlayerPage: false,
            onUpdateCurrentPlayer: null,
            onShellNavigate: nav => capturedNav = nav
        );

        // Assert
        Assert.False(result.Handled);
        Assert.Equal("BoundCommandCannotExecute", result.ActionTaken);
        Assert.False(commandInvoked);
        Assert.Null(capturedNav);
    }

    [Fact]
    public void EdgeCase_RapidMultiTaps_AreIdempotentAndThreadSafe()
    {
        // Arrange: 100 rapid multi-taps across multiple threads
        var player = new MockPlayerModel { PlayerName = "Grace" };
        int commandExecutions = 0;

        var command = new RelayCommandStub<MockPlayerModel>(_ =>
        {
            Interlocked.Increment(ref commandExecutions);
        });

        // Act: Run 50 parallel taps
        Parallel.For(0, 50, _ =>
        {
            RoutingDecisionEngine.RouteEditPlayer(
                assignedPlayer: player,
                bindingContext: player,
                boundCommand: command,
                isCurrentPageEditPlayerPage: false,
                onUpdateCurrentPlayer: null,
                onShellNavigate: null
            );
        });

        // Assert: All 50 executions completed without deadlock or exception
        Assert.Equal(50, commandExecutions);
    }

    [Fact]
    public void EditPlayerViewModel_DataLoading_NoDuplicatesOnRepeatedNavigations()
    {
        // Arrange
        var player1 = new MockPlayerModel { PlayerName = "Player1" };
        var player2 = new MockPlayerModel { PlayerName = "Player2" };

        var activeGames = new List<MockCurrentGameModel>
        {
            new() { GameName = "Active1", Players = [player1, player2] },
            new() { GameName = "Active2", Players = [player1] },
            new() { GameName = "Active3", Players = [player2] }
        };

        var playedGames = new List<MockGameModel>
        {
            new() { GameName = "Played1", Players = [player1, player2] },
            new() { GameName = "Played2", Players = [player1] },
            new() { GameName = "Played3", Players = [player2] }
        };

        var vm = new MockEditPlayerViewModel();

        // Act: Simulate 10 repeated navigations / parameter updates for player1
        for (int i = 0; i < 10; i++)
        {
            vm.OnCurrentPlayerChanged(player1, activeGames, playedGames);
            vm.PageLoaded([player1, player2], activeGames, playedGames);
        }

        // Assert: ActiveGames and PlayedGames must contain strictly the games belonging to player1 without duplicates
        Assert.Equal(2, vm.ActiveGames.Count);
        Assert.Equal(2, vm.PlayedGames.Count);
        Assert.All(vm.ActiveGames, g => Assert.Contains(g.Players, p => p.ID == player1.ID));
        Assert.All(vm.PlayedGames, g => Assert.Contains(g.Players, p => p.ID == player1.ID));
    }

    [Fact]
    public async Task EditPlayerViewModel_ConcurrentLoading_MaintainsDataIntegrity()
    {
        // Arrange
        var playerA = new MockPlayerModel { PlayerName = "PlayerA" };
        var playerB = new MockPlayerModel { PlayerName = "PlayerB" };

        var activeGames = new List<MockCurrentGameModel>
        {
            new() { GameName = "ActiveA1", Players = [playerA] },
            new() { GameName = "ActiveA2", Players = [playerA, playerB] },
            new() { GameName = "ActiveB1", Players = [playerB] }
        };

        var playedGames = new List<MockGameModel>
        {
            new() { GameName = "PlayedA1", Players = [playerA] },
            new() { GameName = "PlayedB1", Players = [playerB] }
        };

        var vm = new MockEditPlayerViewModel();

        // Act: Concurrently simulate OnCurrentPlayerChanged and PageLoaded on multiple threads
        var tasks = new List<Task>();
        for (int i = 0; i < 20; i++)
        {
            tasks.Add(Task.Run(() => vm.OnCurrentPlayerChanged(playerA, activeGames, playedGames), TestContext.Current.CancellationToken));
            tasks.Add(Task.Run(() => vm.PageLoaded([playerA, playerB], activeGames, playedGames), TestContext.Current.CancellationToken));
        }
        await Task.WhenAll(tasks);

        // Final load to ensure settled state
        vm.OnCurrentPlayerChanged(playerA, activeGames, playedGames);

        // Assert: No duplicates
        Assert.Equal(2, vm.ActiveGames.Count);
        Assert.Single(vm.PlayedGames);
        Assert.True(vm.DisplayGames);
        Assert.False(vm.DisplayPlayers);
    }

    [Fact]
    public void EditPlayerViewModel_IdentifyPlayerInGames_FiltersAccurately()
    {
        // Arrange
        var targetPlayer = new MockPlayerModel { PlayerName = "Target" };
        var otherPlayer = new MockPlayerModel { PlayerName = "Other" };

        var gameWithTarget = new MockCurrentGameModel { GameName = "G1", Players = [targetPlayer, otherPlayer] };
        var gameWithoutTarget = new MockCurrentGameModel { GameName = "G2", Players = [otherPlayer] };
        var gameWithNullPlayers = new MockCurrentGameModel { GameName = "G3", Players = null! };

        var vm = new MockEditPlayerViewModel();

        // Act
        var (activeFound, playedFound) = vm.IdentifyPlayerInGames(
            targetPlayer,
            [gameWithTarget, gameWithoutTarget, gameWithNullPlayers],
            []
        );

        // Assert
        Assert.Single(activeFound);
        Assert.Same(gameWithTarget, activeFound[0]);
        Assert.Empty(playedFound);
    }

    [Fact]
    public void EditPlayerViewModel_NullCurrentPlayer_PopulatesAllPlayers()
    {
        // Arrange
        var p1 = new MockPlayerModel { PlayerName = "P1" };
        var p2 = new MockPlayerModel { PlayerName = "P2" };
        var p3 = new MockPlayerModel { PlayerName = "P3" };

        var vm = new MockEditPlayerViewModel();

        // Act: PageLoaded with null CurrentPlayer
        vm.PageLoaded([p1, p2, p3], [], []);

        // Assert
        Assert.True(vm.DisplayPlayers);
        Assert.False(vm.DisplayGames);
        Assert.Equal(3, vm.AllPlayers.Count);
        Assert.False(vm.CanExecuteRemoveCommand());

        // Repeated PageLoaded should not duplicate AllPlayers
        vm.PageLoaded([p1, p2, p3], [], []);
        Assert.Equal(3, vm.AllPlayers.Count);
    }

    [Fact]
    public void EditPlayerViewModel_RemovePlayerCommand_CanExecuteReflectsState()
    {
        var vm = new MockEditPlayerViewModel();
        Assert.False(vm.CanExecuteRemoveCommand());

        vm.CurrentPlayer = new MockPlayerModel { PlayerName = "Test" };
        Assert.True(vm.CanExecuteRemoveCommand());

        vm.CurrentPlayer = null;
        Assert.False(vm.CanExecuteRemoveCommand());
    }
}

public class RelayCommandStub<T>(Action<T?> execute, Func<T?, bool>? canExecute = null) : ICommand
{
    private readonly Action<T?> _execute = execute ?? throw new ArgumentNullException(nameof(execute));
    private readonly Func<T?, bool>? _canExecute = canExecute;

    public event EventHandler? CanExecuteChanged;

    public bool CanExecute(object? parameter)
    {
        if (_canExecute == null) return true;
        if (parameter is T t) return _canExecute(t);
        if (parameter == null && default(T) == null) return _canExecute(default);
        return false;
    }

    public void Execute(object? parameter)
    {
        if (parameter is T t) _execute(t);
        else if (parameter == null && default(T) == null) _execute(default);
    }

    public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
}
