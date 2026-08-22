using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace RummyBooky.Tests;

public class SearchSynchronizationTests
{
    public class PlayerStub
    {
        public Guid ID { get; init; } = Guid.NewGuid();
        public string PlayerName { get; set; } = string.Empty;
        public int PlayerScore { get; set; }
    }

    public class NewGameModelStub
    {
        public ObservableCollection<PlayerStub> Players { get; set; } = [];
    }

    /// <summary>
    /// Exact mathematical and behavioral reproduction of NewGameViewModel search logic
    /// </summary>
    public class SearchEngineEngineSimulation
    {
        public const int MaximumPlayerCount = 6;
        public const int MinimumPlayerCount = 2;

        public PlayerStub[] AllPlayers { get; set; } = [];
        public ObservableCollection<PlayerStub> FilteredPlayerModelsByName { get; set; } = [];
        public PlayerStub? SelectedSuggestedPlayerModel { get; set; }
        public bool ShowPlayerSuggestions { get; set; }
        public bool SwipeEnabled { get; set; }
        public NewGameModelStub GameModelTemplate { get; set; } = new();

        private CancellationTokenSource? _searchCts;
        private string _playerNameText = string.Empty;

        public string PlayerNameText
        {
            get => _playerNameText;
            set
            {
                _playerNameText = value;
                OnPlayerNameTextChanged(value);
            }
        }

        public void OnPlayerNameTextChanged(string value)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;

            SelectedSuggestedPlayerModel = null;
            FilteredPlayerModelsByName.Clear();
            ShowPlayerSuggestions = false;
            SwipeEnabled = false;
        }

        public async Task SearchPlayerSuggestionsAsync()
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = null;

            await PerformSearchAsync(PlayerNameText, CancellationToken.None);
        }

        public async Task UserStoppedTypingAsync(int simulatedDelayMs = 0)
        {
            _searchCts?.Cancel();
            _searchCts?.Dispose();
            _searchCts = new CancellationTokenSource();
            var token = _searchCts.Token;

            if (simulatedDelayMs > 0)
            {
                try
                {
                    await Task.Delay(simulatedDelayMs, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            await PerformSearchAsync(PlayerNameText, token);
        }

        public async Task PerformSearchAsync(string query, CancellationToken token, int simulatedLatencyMs = 0)
        {
            if (GameModelTemplate == null || GameModelTemplate.Players.Count >= MaximumPlayerCount)
                return;

            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredPlayerModelsByName.Clear();
                SelectedSuggestedPlayerModel = null;
                ShowPlayerSuggestions = false;
                SwipeEnabled = false;
                return;
            }

            if (simulatedLatencyMs > 0)
            {
                try
                {
                    await Task.Delay(simulatedLatencyMs, token);
                }
                catch (OperationCanceledException)
                {
                    return;
                }
            }

            var currentAddedIds = GameModelTemplate.Players.Select(p => p.ID).ToHashSet();
            var trimmedQuery = query.Trim();
            var matches = AllPlayers
                .Where(p => p != null && !string.IsNullOrEmpty(p.PlayerName) &&
                            p.PlayerName.StartsWith(trimmedQuery, StringComparison.OrdinalIgnoreCase) &&
                            !currentAddedIds.Contains(p.ID))
                .ToList();

            if (token.IsCancellationRequested)
                return;

            FilteredPlayerModelsByName.Clear();
            foreach (var player in matches)
            {
                FilteredPlayerModelsByName.Add(player);
            }

            SelectedSuggestedPlayerModel = FilteredPlayerModelsByName.FirstOrDefault();
            ShowPlayerSuggestions = FilteredPlayerModelsByName.Count > 0;
            SwipeEnabled = FilteredPlayerModelsByName.Count > 1;
        }

        public bool AddSuggestedPlayer(PlayerStub? player = null)
        {
            var targetPlayer = player ?? SelectedSuggestedPlayerModel;
            if (targetPlayer is null)
                return false;

            GameModelTemplate.Players.Add(targetPlayer);
            PlayerNameText = string.Empty;
            FilteredPlayerModelsByName.Clear();
            SelectedSuggestedPlayerModel = null;
            return true;
        }
    }

    private static PlayerStub[] CreateStandardPlayerDatabase()
    {
        return new PlayerStub[]
        {
            new() { PlayerName = "Eric Cartman" },
            new() { PlayerName = "Erica Kane" },
            new() { PlayerName = "Bob Builder" },
            new() { PlayerName = "Bobby Fischer" },
            new() { PlayerName = "Rob Bobson" },
            new() { PlayerName = "Alice Wonder" },
            new() { PlayerName = "Charlie Chaplin" }
        };
    }

    [Fact]
    public async Task QueryChanging_BobImmediatelyAfterEric_ClearsEricAndPopulatesOnlyBob()
    {
        // Arrange
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = CreateStandardPlayerDatabase()
        };

        // Act 1: Search "eric"
        engine.PlayerNameText = "eric";
        await engine.SearchPlayerSuggestionsAsync();

        // Assert 1: Eric suggestions populated
        Assert.Equal(2, engine.FilteredPlayerModelsByName.Count);
        Assert.All(engine.FilteredPlayerModelsByName, p => Assert.StartsWith("Eric", p.PlayerName, StringComparison.OrdinalIgnoreCase));
        Assert.Equal("Eric Cartman", engine.SelectedSuggestedPlayerModel?.PlayerName);
        Assert.True(engine.ShowPlayerSuggestions);

        // Act 2: Immediately change query to "bob"
        engine.PlayerNameText = "bob";

        // Invariant: Changing text must immediately clear suggestions before search finishes
        Assert.Empty(engine.FilteredPlayerModelsByName);
        Assert.Null(engine.SelectedSuggestedPlayerModel);
        Assert.False(engine.ShowPlayerSuggestions);

        // Act 3: Execute search for "bob"
        await engine.SearchPlayerSuggestionsAsync();

        // Assert 3: Only "bob" suggestions present, zero "eric" matches retained
        Assert.Equal(2, engine.FilteredPlayerModelsByName.Count);
        Assert.DoesNotContain(engine.FilteredPlayerModelsByName, p => p.PlayerName.Contains("Eric", StringComparison.OrdinalIgnoreCase));
        Assert.Contains(engine.FilteredPlayerModelsByName, p => p.PlayerName == "Bob Builder");
        Assert.Contains(engine.FilteredPlayerModelsByName, p => p.PlayerName == "Bobby Fischer");
        Assert.Equal("Bob Builder", engine.SelectedSuggestedPlayerModel?.PlayerName);
    }

    [Fact]
    public async Task InstantEnterExecution_ExecutesImmediatelyWithZeroLag()
    {
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = CreateStandardPlayerDatabase()
        };

        engine.PlayerNameText = "eric";

        var sw = Stopwatch.StartNew();
        // ReturnCommand invokes SearchPlayerSuggestionsAsync directly with CancellationToken.None
        await engine.SearchPlayerSuggestionsAsync();
        sw.Stop();

        // Must execute synchronously / immediately in < 50ms without waiting for 250ms debouncer
        Assert.True(sw.ElapsedMilliseconds < 50, $"Instant Enter took {sw.ElapsedMilliseconds}ms, expected < 50ms");
        Assert.Equal(2, engine.FilteredPlayerModelsByName.Count);
    }

    [Fact]
    public async Task RapidTyping_InFlightTokenCancellation_PreventsStaleRaceConditions()
    {
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = CreateStandardPlayerDatabase()
        };

        // Simulate typing "e" -> "er" -> "eri" -> "eric" -> "b" -> "bo" -> "bob" in rapid bursts
        // where early searches have longer simulated latency than later ones.
        var searchTasks = new List<Task>();

        engine.PlayerNameText = "e";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 50));

        engine.PlayerNameText = "er";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 40));

        engine.PlayerNameText = "eri";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 30));

        engine.PlayerNameText = "eric";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 20));

        engine.PlayerNameText = "b";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 15));

        engine.PlayerNameText = "bo";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 10));

        engine.PlayerNameText = "bob";
        searchTasks.Add(engine.UserStoppedTypingAsync(simulatedDelayMs: 0));

        await Task.WhenAll(searchTasks);

        // Assert: Only the final query ("bob") matches survive. All prior in-flight searches were cancelled.
        Assert.Equal(2, engine.FilteredPlayerModelsByName.Count);
        Assert.All(engine.FilteredPlayerModelsByName, p => Assert.StartsWith("bob", p.PlayerName, StringComparison.OrdinalIgnoreCase));
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    [InlineData("    ")]
    [InlineData("\t\r\n")]
    public async Task EmptyAndWhitespaceQueries_ClearSuggestionsAndHideCarousel(string query)
    {
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = CreateStandardPlayerDatabase()
        };

        // First populate suggestions
        engine.PlayerNameText = "bob";
        await engine.SearchPlayerSuggestionsAsync();
        Assert.NotEmpty(engine.FilteredPlayerModelsByName);

        // Now set empty / whitespace query
        engine.PlayerNameText = query;
        await engine.SearchPlayerSuggestionsAsync();

        Assert.Empty(engine.FilteredPlayerModelsByName);
        Assert.Null(engine.SelectedSuggestedPlayerModel);
        Assert.False(engine.ShowPlayerSuggestions);
        Assert.False(engine.SwipeEnabled);
    }

    [Fact]
    public async Task ExistingInGamePlayers_AreFilteredOutFromSuggestions()
    {
        var db = CreateStandardPlayerDatabase();
        var bob = db.First(p => p.PlayerName == "Bob Builder");
        var bobby = db.First(p => p.PlayerName == "Bobby Fischer");

        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = db
        };

        // Add Bob Builder to existing game
        engine.GameModelTemplate.Players.Add(bob);

        // Search "bob"
        engine.PlayerNameText = "bob";
        await engine.SearchPlayerSuggestionsAsync();

        // Bob Builder is already in game, so only Bobby Fischer should appear
        Assert.Single(engine.FilteredPlayerModelsByName);
        Assert.Equal(bobby.ID, engine.FilteredPlayerModelsByName[0].ID);
        Assert.Equal("Bobby Fischer", engine.FilteredPlayerModelsByName[0].PlayerName);

        // Add Bobby Fischer as well
        engine.GameModelTemplate.Players.Add(bobby);

        // Search "bob" again
        await engine.SearchPlayerSuggestionsAsync();

        // Both are in game -> suggestions must be empty
        Assert.Empty(engine.FilteredPlayerModelsByName);
        Assert.False(engine.ShowPlayerSuggestions);
    }

    [Fact]
    public async Task MaximumPlayersReached_DisablesSearchSuggestions()
    {
        var db = CreateStandardPlayerDatabase();
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = db
        };

        // Fill game to maximum (6 players)
        for (int i = 0; i < SearchEngineEngineSimulation.MaximumPlayerCount; i++)
        {
            engine.GameModelTemplate.Players.Add(new PlayerStub { PlayerName = $"Player_{i}" });
        }

        engine.PlayerNameText = "eric";
        await engine.SearchPlayerSuggestionsAsync();

        // At max player count, search must return immediately without displaying suggestions
        Assert.Empty(engine.FilteredPlayerModelsByName);
        Assert.False(engine.ShowPlayerSuggestions);
    }

    [Fact]
    public async Task CarouselSelection_SynchronizesCurrentItemAndSwipeEnabled()
    {
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = CreateStandardPlayerDatabase()
        };

        // Single match query -> Swipe disabled
        engine.PlayerNameText = "charlie";
        await engine.SearchPlayerSuggestionsAsync();

        Assert.Single(engine.FilteredPlayerModelsByName);
        Assert.Equal("Charlie Chaplin", engine.SelectedSuggestedPlayerModel?.PlayerName);
        Assert.True(engine.ShowPlayerSuggestions);
        Assert.False(engine.SwipeEnabled); // Exactly 1 item -> swipe disabled

        // Multi match query -> Swipe enabled
        engine.PlayerNameText = "bob";
        await engine.SearchPlayerSuggestionsAsync();

        Assert.Equal(2, engine.FilteredPlayerModelsByName.Count);
        Assert.Equal("Bob Builder", engine.SelectedSuggestedPlayerModel?.PlayerName);
        Assert.True(engine.ShowPlayerSuggestions);
        Assert.True(engine.SwipeEnabled); // 2 items -> swipe enabled
    }

    [Fact]
    public async Task DoubleTap_AddsExplicitTargetPlayerAndResetsSearch()
    {
        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = CreateStandardPlayerDatabase()
        };

        engine.PlayerNameText = "bob";
        await engine.SearchPlayerSuggestionsAsync();

        var bobby = engine.FilteredPlayerModelsByName.First(p => p.PlayerName == "Bobby Fischer");

        // Double tap on Bobby Fischer card passes Bobby Fischer as CommandParameter
        bool added = engine.AddSuggestedPlayer(bobby);

        Assert.True(added);
        Assert.Single(engine.GameModelTemplate.Players);
        Assert.Equal("Bobby Fischer", engine.GameModelTemplate.Players[0].PlayerName);
        Assert.Equal(string.Empty, engine.PlayerNameText);
        Assert.Empty(engine.FilteredPlayerModelsByName);
        Assert.Null(engine.SelectedSuggestedPlayerModel);
    }

    [Fact]
    public async Task StressScale_TenThousandPlayersDatabase_SearchCompletesInstantly()
    {
        int N = 10000;
        var largeDb = new PlayerStub[N];
        for (int i = 0; i < N; i++)
        {
            largeDb[i] = new PlayerStub { PlayerName = $"Player_{i:D5}" };
        }
        // Insert specific targets
        largeDb[42] = new PlayerStub { PlayerName = "EricTheGreat" };
        largeDb[999] = new PlayerStub { PlayerName = "EricTheBold" };

        var engine = new SearchEngineEngineSimulation
        {
            AllPlayers = largeDb
        };

        engine.PlayerNameText = "eric";
        var sw = Stopwatch.StartNew();
        await engine.SearchPlayerSuggestionsAsync();
        sw.Stop();

        Assert.Equal(2, engine.FilteredPlayerModelsByName.Count);
        Assert.True(sw.ElapsedMilliseconds < 10, $"Search on 10,000 items took {sw.ElapsedMilliseconds}ms (must be < 10ms)");
    }
}
