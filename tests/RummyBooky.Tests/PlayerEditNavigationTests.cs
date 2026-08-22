using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading.Tasks;
using System.Windows.Input;
using Xunit;

namespace RummyBooky.Tests;

public class PlayerEditNavigationTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
    }

    public class GameStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public List<PlayerStub> Players { get; set; } = [];
    }

    public class RelayCommandStub<T> : ICommand
    {
        private readonly Action<T> _execute;
        private readonly Func<T, bool>? _canExecute;

        public RelayCommandStub(Action<T> execute, Func<T, bool>? canExecute = null)
        {
            _execute = execute;
            _canExecute = canExecute;
        }

        public bool CanExecute(object? parameter) => _canExecute?.Invoke((T)parameter!) ?? true;
        public void Execute(object? parameter) => _execute((T)parameter!);
        public event EventHandler? CanExecuteChanged;
        public void RaiseCanExecuteChanged() => CanExecuteChanged?.Invoke(this, EventArgs.Empty);
    }

    /// <summary>
    /// Simulates PlayerCardView pencil button click event routing logic
    /// </summary>
    public class PlayerCardViewNavigationRouter
    {
        public ICommand? Command { get; set; }
        public PlayerStub? AssignedPlayerModel { get; set; }
        public object? BindingContext { get; set; }

        public bool CommandExecuted { get; private set; }
        public PlayerStub? CommandParameterReceived { get; private set; }
        public bool FallbackNavigated { get; private set; }
        public PlayerStub? FallbackPlayerNavigated { get; private set; }

        public void OnEditPlayerButtonClicked(bool simulateShellAvailable = true)
        {
            var targetPlayer = AssignedPlayerModel ?? BindingContext as PlayerStub;
            if (targetPlayer is null)
                return;

            if (Command != null && Command.CanExecute(targetPlayer))
            {
                Command.Execute(targetPlayer);
                CommandExecuted = true;
                CommandParameterReceived = targetPlayer;
                return;
            }

            if (simulateShellAvailable)
            {
                FallbackNavigated = true;
                FallbackPlayerNavigated = targetPlayer;
            }
        }
    }

    /// <summary>
    /// Simulates EditPlayerViewModel game collection loading and deduplication logic
    /// </summary>
    public class EditPlayerViewModelSimulation
    {
        public PlayerStub? CurrentPlayer { get; set; }
        public ObservableCollection<GameStub> ActiveGames { get; set; } = [];
        public ObservableCollection<GameStub> PlayedGames { get; set; } = [];
        public bool DisplayGames { get; set; }
        public bool DisplayPlayers { get; set; }

        public void LoadGameCollectionsWithPlayerName(List<GameStub> activeGamesList, List<GameStub> playedGamesList)
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

        public void IdentifyAndLoadGames(PlayerStub player, List<GameStub> allActive, List<GameStub> allPlayed)
        {
            CurrentPlayer = player;
            DisplayGames = true;
            DisplayPlayers = false;

            var activeFound = new List<GameStub>();
            var playedFound = new List<GameStub>();

            foreach (var g in allActive)
            {
                if (g.Players.Exists(p => p.ID == player.ID))
                    activeFound.Add(g);
            }

            foreach (var g in allPlayed)
            {
                if (g.Players.Exists(p => p.ID == player.ID))
                    playedFound.Add(g);
            }

            LoadGameCollectionsWithPlayerName(activeFound, playedFound);
        }
    }

    [Fact]
    public void PlayerCardView_WithBoundCommand_ExecutesCommandWithTargetPlayer()
    {
        var player = new PlayerStub { PlayerName = "Alice" };
        PlayerStub? received = null;

        var command = new RelayCommandStub<PlayerStub>(p => received = p);
        var router = new PlayerCardViewNavigationRouter
        {
            AssignedPlayerModel = player,
            Command = command
        };

        router.OnEditPlayerButtonClicked();

        Assert.True(router.CommandExecuted);
        Assert.Equal(player, router.CommandParameterReceived);
        Assert.Equal("Alice", received?.PlayerName);
        Assert.False(router.FallbackNavigated);
    }

    [Fact]
    public void PlayerCardView_WithoutBoundCommand_ExecutesAutonomousFallbackNavigation()
    {
        var player = new PlayerStub { PlayerName = "Bob" };
        var router = new PlayerCardViewNavigationRouter
        {
            AssignedPlayerModel = player,
            Command = null // In CardBoxView or standalone, Command may not be bound
        };

        router.OnEditPlayerButtonClicked(simulateShellAvailable: true);

        Assert.False(router.CommandExecuted);
        Assert.True(router.FallbackNavigated);
        Assert.Equal(player, router.FallbackPlayerNavigated);
    }

    [Fact]
    public void EditPlayerViewModel_RepeatedLoads_StrictlyDeduplicatesCollections()
    {
        var player = new PlayerStub { PlayerName = "Charlie" };
        var g1 = new GameStub { Players = [player] };
        var g2 = new GameStub { Players = [player] };
        var allActive = new List<GameStub> { g1 };
        var allPlayed = new List<GameStub> { g2 };

        var vm = new EditPlayerViewModelSimulation();

        // Simulate first load (e.g. OnCurrentPlayerChanged)
        vm.IdentifyAndLoadGames(player, allActive, allPlayed);
        Assert.Single(vm.ActiveGames);
        Assert.Single(vm.PlayedGames);

        // Simulate second load (e.g. PageLoaded event)
        vm.IdentifyAndLoadGames(player, allActive, allPlayed);

        // Invariant: Collections must be cleared prior to adding; no duplicated items
        Assert.Single(vm.ActiveGames);
        Assert.Single(vm.PlayedGames);
        Assert.Equal(g1.ID, vm.ActiveGames[0].ID);
        Assert.Equal(g2.ID, vm.PlayedGames[0].ID);
    }
}
