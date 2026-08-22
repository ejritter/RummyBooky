using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class NewGameRosterAndSearchTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
        public int GamesWon { get; set; }
        public bool IsNewPlayer { get; set; }
    }

    public class NewGameViewModelSimulation
    {
        public const int MaximumPlayerCount = 6;
        public const int MinimumPlayerCount = 2;

        public ObservableCollection<PlayerStub> Players { get; set; } = [];
        public PlayerStub[] AllPlayers { get; set; } = [];
        public ObservableCollection<PlayerStub> FilteredPlayerModelsByName { get; set; } = [];
        public PlayerStub? SelectedSuggestedPlayerModel { get; set; }
        public bool ShowPlayerSuggestions { get; set; }
        public bool ShowGridTemplate { get; set; }

        public PlayerStub? LastAddedPlayer { get; set; }
        public string LastSearchQuery { get; set; } = string.Empty;
        public string PlayerNameText { get; set; } = string.Empty;

        public bool CanAddPlayer =>
            !string.IsNullOrWhiteSpace(PlayerNameText) &&
            Players.Count < MaximumPlayerCount;

        public void OnPlayerNameTextChanged(string value)
        {
            PlayerNameText = value;
            if (string.IsNullOrWhiteSpace(value))
            {
                FilteredPlayerModelsByName.Clear();
                SelectedSuggestedPlayerModel = null;
                ShowPlayerSuggestions = false;
                ShowGridTemplate = Players.Count > 0;
            }
        }

        public async Task SearchPlayerSuggestionsAsync(string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredPlayerModelsByName.Clear();
                SelectedSuggestedPlayerModel = null;
                ShowPlayerSuggestions = false;
                ShowGridTemplate = Players.Count > 0;
                return;
            }

            var currentIds = Players.Select(p => p.ID).ToHashSet();
            var trimmed = query.Trim();
            var matches = AllPlayers
                .Where(p => !string.IsNullOrEmpty(p.PlayerName) &&
                            p.PlayerName.Contains(trimmed, StringComparison.OrdinalIgnoreCase) &&
                            !currentIds.Contains(p.ID))
                .ToList();

            FilteredPlayerModelsByName = new ObservableCollection<PlayerStub>(matches);
            SelectedSuggestedPlayerModel = matches.FirstOrDefault();
            ShowPlayerSuggestions = matches.Count > 0;
            ShowGridTemplate = !ShowPlayerSuggestions && Players.Count > 0;
            await Task.CompletedTask;
        }

        public async Task<bool> AddPlayerAsync(string playerName)
        {
            if (string.IsNullOrWhiteSpace(playerName) || Players.Count >= MaximumPlayerCount)
                return false;

            var query = playerName.Trim();
            LastSearchQuery = query;
            var newPlayer = new PlayerStub { PlayerName = query, IsNewPlayer = true };
            Players.Add(newPlayer);
            LastAddedPlayer = newPlayer;

            PlayerNameText = string.Empty;
            FilteredPlayerModelsByName.Clear();
            SelectedSuggestedPlayerModel = null;
            ShowPlayerSuggestions = false;
            ShowGridTemplate = Players.Count > 0;
            return await Task.FromResult(true);
        }

        public async Task<bool> AddSuggestedPlayerAsync(PlayerStub suggestedPlayer)
        {
            if (suggestedPlayer == null || Players.Count >= MaximumPlayerCount)
                return false;

            LastSearchQuery = PlayerNameText.Trim();
            Players.Add(suggestedPlayer);
            LastAddedPlayer = suggestedPlayer;

            PlayerNameText = string.Empty;
            FilteredPlayerModelsByName.Clear();
            SelectedSuggestedPlayerModel = null;
            ShowPlayerSuggestions = false;
            ShowGridTemplate = Players.Count > 0;
            return await Task.FromResult(true);
        }

        public async Task<bool> RemovePlayerAsync(PlayerStub player)
        {
            if (player == null || !Players.Contains(player))
                return false;

            Players.Remove(player);

            // Undo accidental creation: restore query and rerun suggestions if this was the last added player
            if (player == LastAddedPlayer && !string.IsNullOrWhiteSpace(LastSearchQuery))
            {
                var queryToRestore = LastSearchQuery;
                LastAddedPlayer = null;
                LastSearchQuery = string.Empty;
                PlayerNameText = queryToRestore;
                await SearchPlayerSuggestionsAsync(queryToRestore);
            }
            else if (player == LastAddedPlayer)
            {
                LastAddedPlayer = null;
            }

            ShowGridTemplate = !ShowPlayerSuggestions && Players.Count > 0;
            return await Task.FromResult(true);
        }
    }

    [Fact]
    public async Task SearchSafeguard_EmptyOrWhitespaceQuery_ClearsSuggestionsAndRestoresRoster()
    {
        var vm = new NewGameViewModelSimulation
        {
            AllPlayers = [new PlayerStub { PlayerName = "Eric" }]
        };
        vm.Players.Add(new PlayerStub { PlayerName = "Brodie" });

        // Search for Eric
        await vm.SearchPlayerSuggestionsAsync("Eric");
        Assert.True(vm.ShowPlayerSuggestions);
        Assert.Single(vm.FilteredPlayerModelsByName);
        Assert.False(vm.ShowGridTemplate);

        // User clears input or enters whitespace
        vm.OnPlayerNameTextChanged("   ");
        Assert.False(vm.ShowPlayerSuggestions);
        Assert.Empty(vm.FilteredPlayerModelsByName);
        Assert.Null(vm.SelectedSuggestedPlayerModel);
        Assert.True(vm.ShowGridTemplate);
    }

    [Fact]
    public async Task UndoAccidentalPlayerCreation_RestoresPreviousSearchQueryAndSuggestions()
    {
        var existingEric = new PlayerStub { PlayerName = "Eric", GamesWon = 42 };
        var vm = new NewGameViewModelSimulation
        {
            AllPlayers = [existingEric]
        };

        // User searches for "eric"
        await vm.SearchPlayerSuggestionsAsync("eric");
        Assert.Single(vm.FilteredPlayerModelsByName);

        // User accidentally clicks "Add Player" instead of choosing the card
        await vm.AddPlayerAsync("eric");
        Assert.Single(vm.Players);
        var createdPlayer = vm.Players[0];
        Assert.NotEqual(existingEric.ID, createdPlayer.ID); // Accidental duplicate!
        Assert.Empty(vm.FilteredPlayerModelsByName);

        // User removes the accidental player
        await vm.RemovePlayerAsync(createdPlayer);

        // Assert search is restored and existing card is returned from auto search
        Assert.Empty(vm.Players);
        Assert.Equal("eric", vm.PlayerNameText);
        Assert.True(vm.ShowPlayerSuggestions);
        Assert.Single(vm.FilteredPlayerModelsByName);
        Assert.Equal(existingEric.ID, vm.FilteredPlayerModelsByName[0].ID);
        Assert.Equal(42, vm.FilteredPlayerModelsByName[0].GamesWon);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("\t\n")]
    public void CanAddPlayer_WhitespaceQuery_ReturnsFalse(string whitespace)
    {
        var vm = new NewGameViewModelSimulation { PlayerNameText = whitespace };
        Assert.False(vm.CanAddPlayer);
    }

    [Fact]
    public void MaxPlayerCount_EnforcesRosterLimitAtSix()
    {
        var vm = new NewGameViewModelSimulation();
        for (int i = 0; i < 6; i++)
        {
            vm.Players.Add(new PlayerStub { PlayerName = $"Player {i + 1}" });
        }

        vm.PlayerNameText = "ExtraPlayer";
        Assert.False(vm.CanAddPlayer);
    }

    [Fact]
    public async Task AddPlayerVsAddSuggestedPlayer_SetsIsNewPlayerCorrectly()
    {
        var existingPlayer = new PlayerStub { PlayerName = "Existing Veteran", IsNewPlayer = false };
        var vm = new NewGameViewModelSimulation
        {
            AllPlayers = [existingPlayer]
        };

        // Add newly created player
        await vm.AddPlayerAsync("Brand New Rookie");
        Assert.Single(vm.Players);
        Assert.True(vm.Players[0].IsNewPlayer);

        // Add existing player
        await vm.AddSuggestedPlayerAsync(existingPlayer);
        Assert.Equal(2, vm.Players.Count);
        Assert.True(vm.Players[0].IsNewPlayer);
        Assert.False(vm.Players[1].IsNewPlayer);
    }
}
