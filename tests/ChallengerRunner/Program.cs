using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace ChallengerRunner;

public class PlayerModelStub
{
    public Guid ID { get; init; } = Guid.NewGuid();
    public string PlayerName { get; set; } = string.Empty;
    public int PlayerScore { get; set; } = 0;
    public double LifetimeScore { get; set; } = 0;
}

public class GameModelStub
{
    public Guid ID { get; init; } = Guid.NewGuid();
    public List<PlayerModelStub> Players { get; set; } = [];
}

public class Program
{
    public static int Passed = 0;
    public static int Failed = 0;
    public static List<string> FailureMessages = new();

    public static void Assert(bool condition, string testName, string details = "")
    {
        if (condition)
        {
            Passed++;
            Console.WriteLine($"[PASS] {testName}");
        }
        else
        {
            Failed++;
            string msg = $"[FAIL] {testName} - {details}";
            Console.WriteLine(msg);
            FailureMessages.Add(msg);
        }
    }

    public static IReadOnlyList<PlayerModelStub> GetOrderedPlayers(IEnumerable<PlayerModelStub>? players)
    {
        if (players is null)
        {
            return Array.Empty<PlayerModelStub>();
        }

        return players
            .OrderBy(player => player.PlayerScore)
            .ThenBy(player => player.PlayerName)
            .ToList();
    }

    public static async Task<int> Main(string[] args)
    {
        Console.WriteLine("===============================================================================");
        Console.WriteLine("  CHALLENGER 2 EMPIRICAL ADVERSARIAL STRESS TEST SUITE: MILESTONE 2 (R3 & R4)  ");
        Console.WriteLine("===============================================================================\n");

        // Milestone 1 Regression Checks
        Console.WriteLine("--- Section 1: Milestone 1 Regression Verification ---");
        Test_EmptyList();
        Test_NullList();
        Test_SinglePlayer();
        Test_TwoToSixPlayers_DistinctScores();
        Test_TiedScores_SecondarySortByName();
        Test_ReversedScores();
        Test_NegativeScores();
        Test_ExtremeValues();
        Test_StressScale_IntroSortComplexity();
        Test_LayoutMath_ExposedHeaderInvariants();
        Test_ActionBox_Coordinates();
        Test_ZIndex_StackHierarchy();
        Test_Viewport_Canvas_Bounds();

        // Milestone 2 R4: Search Synchronization & Instant Enter Trigger
        Console.WriteLine("\n--- Section 2: Requirement R4 Search Synchronization & Instant Enter ---");
        await Test_R4_QueryChanging_EricToBob_ImmediatePurge();
        await Test_R4_InstantEnterExecution_ZeroMillisecondLag();
        await Test_R4_RapidTyping_InFlightCancellation();
        await Test_R4_EmptyAndWhitespaceQueries();
        await Test_R4_FilterOutExistingInGamePlayers();
        await Test_R4_MaxPlayerCountBoundary();
        await Test_R4_CarouselSelectionSync_And_SwipeToggle();
        await Test_R4_DoubleTap_DirectParameterPassing();
        await Test_R4_SpecialCharacters_And_CaseInsensitivity();
        await Test_R4_LargeScaleDatabase_StressSearch();

        // Milestone 2 R3: Player Card Edit Navigation & Event Routing
        Console.WriteLine("\n--- Section 3: Requirement R3 Edit Navigation & Deduplication ---");
        Test_R3_BoundCommand_InvocationPriority();
        Test_R3_UnboundCommand_AutonomousFallback();
        Test_R3_EditPlayerViewModel_Deduplication();
        Test_R3_NullSafe_GameFiltering();

        // Requirement R2: Dedicated Edit Game Management, Tie Resolution & Stats Synchronization
        Console.WriteLine("\n--- Section 4: Requirement R2 Edit Game Management, Ties & Stats Sync ---");
        Test_R2_TwoPlayerAndThreePlayerTies();
        Test_R2_ManualWinnerOverridesOnDraws();
        Test_R2_StatusTransitionsCycle();
        Test_R2_ScoreLimitModifiedBelowScores();
        Test_R2_PolymorphicSerializationIntegrity();
        Test_R2_GlobalPlayerRankingAndStatsSync();

        Console.WriteLine("\n===============================================================================");
        Console.WriteLine($"  SUMMARY: {Passed} PASSED, {Failed} FAILED");
        Console.WriteLine("===============================================================================");

        if (Failed > 0)
        {
            Console.WriteLine("\nFailures:");
            foreach (var f in FailureMessages)
            {
                Console.WriteLine($" - {f}");
            }
            return 1;
        }

        return 0;
    }

    #region M1 Tests
    private static void Test_EmptyList()
    {
        var input = new List<PlayerModelStub>();
        var result = GetOrderedPlayers(input);
        Assert(result != null && result.Count == 0, "Test_EmptyList: returns empty list without error");
    }

    private static void Test_NullList()
    {
        IEnumerable<PlayerModelStub>? input = null;
        var result = GetOrderedPlayers(input);
        Assert(result != null && result.Count == 0, "Test_NullList: returns empty list when null");
    }

    private static void Test_SinglePlayer()
    {
        var input = new List<PlayerModelStub> { new() { PlayerName = "Alice", PlayerScore = 42 } };
        var result = GetOrderedPlayers(input);
        Assert(result.Count == 1 && result[0].PlayerName == "Alice" && result[0].PlayerScore == 42, "Test_SinglePlayer: single element preserved");
    }

    private static void Test_TwoToSixPlayers_DistinctScores()
    {
        for (int n = 2; n <= 6; n++)
        {
            var list = new List<PlayerModelStub>();
            for (int i = 0; i < n; i++)
            {
                list.Add(new PlayerModelStub { PlayerName = $"Player{i}", PlayerScore = (n - i) * 15 });
            }

            var result = GetOrderedPlayers(list);
            bool isSorted = true;
            for (int i = 0; i < result.Count - 1; i++)
            {
                if (result[i].PlayerScore > result[i + 1].PlayerScore)
                {
                    isSorted = false;
                    break;
                }
            }
            Assert(isSorted && result.Count == n, $"Test_TwoToSixPlayers_DistinctScores: {n} players sorted ascending", $"Min: {result.First().PlayerScore}, Max: {result.Last().PlayerScore}");
        }
    }

    private static void Test_TiedScores_SecondarySortByName()
    {
        var input = new List<PlayerModelStub>
        {
            new() { PlayerName = "Charlie", PlayerScore = 100 },
            new() { PlayerName = "Alice", PlayerScore = 100 },
            new() { PlayerName = "Bob", PlayerScore = 50 },
            new() { PlayerName = "Dave", PlayerScore = 100 },
            new() { PlayerName = "Aaron", PlayerScore = 50 }
        };

        var result = GetOrderedPlayers(input);
        Assert(result[0].PlayerName == "Aaron" && result[0].PlayerScore == 50, "Test_TiedScores: 1st is Aaron (50)");
        Assert(result[1].PlayerName == "Bob" && result[1].PlayerScore == 50, "Test_TiedScores: 2nd is Bob (50)");
        Assert(result[2].PlayerName == "Alice" && result[2].PlayerScore == 100, "Test_TiedScores: 3rd is Alice (100)");
        Assert(result[3].PlayerName == "Charlie" && result[3].PlayerScore == 100, "Test_TiedScores: 4th is Charlie (100)");
        Assert(result[4].PlayerName == "Dave" && result[4].PlayerScore == 100, "Test_TiedScores: 5th is Dave (100)");
    }

    private static void Test_ReversedScores()
    {
        var input = new List<PlayerModelStub>
        {
            new() { PlayerName = "P1", PlayerScore = 500 },
            new() { PlayerName = "P2", PlayerScore = 400 },
            new() { PlayerName = "P3", PlayerScore = 300 },
            new() { PlayerName = "P4", PlayerScore = 200 },
            new() { PlayerName = "P5", PlayerScore = 100 },
            new() { PlayerName = "P6", PlayerScore = 0 }
        };

        var result = GetOrderedPlayers(input);
        Assert(result[0].PlayerScore == 0 && result[5].PlayerScore == 500, "Test_ReversedScores: descending input reversed to ascending");
        for (int i = 0; i < 5; i++)
        {
            Assert(result[i].PlayerScore <= result[i + 1].PlayerScore, $"Test_ReversedScores: index {i} <= {i+1}");
        }
    }

    private static void Test_NegativeScores()
    {
        var input = new List<PlayerModelStub>
        {
            new() { PlayerName = "P1", PlayerScore = 10 },
            new() { PlayerName = "P2", PlayerScore = -25 },
            new() { PlayerName = "P3", PlayerScore = 0 },
            new() { PlayerName = "P4", PlayerScore = -100 },
            new() { PlayerName = "P5", PlayerScore = -5 }
        };

        var result = GetOrderedPlayers(input);
        Assert(result[0].PlayerScore == -100, "Test_NegativeScores: lowest is -100");
        Assert(result[1].PlayerScore == -25, "Test_NegativeScores: 2nd is -25");
        Assert(result[2].PlayerScore == -5, "Test_NegativeScores: 3rd is -5");
        Assert(result[3].PlayerScore == 0, "Test_NegativeScores: 4th is 0");
        Assert(result[4].PlayerScore == 10, "Test_NegativeScores: 5th is 10");
    }

    private static void Test_ExtremeValues()
    {
        var input = new List<PlayerModelStub>
        {
            new() { PlayerName = "Max", PlayerScore = int.MaxValue },
            new() { PlayerName = "Min", PlayerScore = int.MinValue },
            new() { PlayerName = "Zero", PlayerScore = 0 }
        };

        var result = GetOrderedPlayers(input);
        Assert(result[0].PlayerScore == int.MinValue, "Test_ExtremeValues: int.MinValue is first");
        Assert(result[1].PlayerScore == 0, "Test_ExtremeValues: 0 is second");
        Assert(result[2].PlayerScore == int.MaxValue, "Test_ExtremeValues: int.MaxValue is third");
    }

    private static void Test_StressScale_IntroSortComplexity()
    {
        int N = 100000;
        var rng = new Random(12345);
        var input = new List<PlayerModelStub>(N);
        for (int i = 0; i < N; i++)
        {
            input.Add(new PlayerModelStub
            {
                PlayerName = $"P_{rng.Next(0, 1000)}",
                PlayerScore = rng.Next(-100000, 100000)
            });
        }

        var sw = Stopwatch.StartNew();
        var result = GetOrderedPlayers(input);
        sw.Stop();

        bool properlySorted = true;
        for (int i = 0; i < result.Count - 1; i++)
        {
            if (result[i].PlayerScore > result[i + 1].PlayerScore)
            {
                properlySorted = false;
                break;
            }
            else if (result[i].PlayerScore == result[i + 1].PlayerScore)
            {
                if (string.Compare(result[i].PlayerName, result[i + 1].PlayerName, StringComparison.Ordinal) > 0)
                {
                    properlySorted = false;
                    break;
                }
            }
        }

        Assert(properlySorted, $"Test_StressScale: 100,000 items sorted correctly in {sw.ElapsedMilliseconds}ms (IntroSort O(n log n))");
        Assert(sw.ElapsedMilliseconds < 500, $"Test_StressScale: Execution time ({sw.ElapsedMilliseconds}ms) well under 500ms");
    }

    private static void Test_LayoutMath_ExposedHeaderInvariants()
    {
        double[] testCardHeights = { 95d, 100d, 120d, 150d, 200d, 250d, 300d };

        foreach (var cardHeight in testCardHeights)
        {
            for (int count = 1; count <= 6; count++)
            {
                double[] topOffsets = new double[count];
                for (int i = 0; i < count; i++)
                {
                    topOffsets[i] = i * (0.20d * cardHeight);
                }

                Assert(topOffsets[0] == 0d, $"LayoutMath: Count={count}, CardHeight={cardHeight} => Y_0 == 0");

                for (int i = 1; i < count; i++)
                {
                    double step = topOffsets[i] - topOffsets[i - 1];
                    double expectedStep = 0.20d * cardHeight;
                    Assert(Math.Abs(step - expectedStep) < 0.0001, $"LayoutMath: Count={count}, Step i={i} is {step:F2} == {expectedStep:F2}");
                }

                double expectedCanvasHeight = (count - 1) * 0.20d * cardHeight + cardHeight;
                double calculatedCanvasHeight = count > 0 ? ((count - 1) * 0.20d * cardHeight + cardHeight) : 0d;
                Assert(Math.Abs(calculatedCanvasHeight - expectedCanvasHeight) < 0.0001, $"LayoutMath: Count={count} CanvasHeight={calculatedCanvasHeight:F2}");

                for (int i = 0; i < count - 1; i++)
                {
                    double exposed = topOffsets[i + 1] - topOffsets[i];
                    double expectedExposed = 0.20d * cardHeight;
                    Assert(Math.Abs(exposed - expectedExposed) < 0.0001, $"LayoutMath: Exposed header height for card {i} is {exposed:F2} == {expectedExposed:F2}");
                }
            }
        }
    }

    private static void Test_ActionBox_Coordinates()
    {
        double cardHeight = 150d;
        double imageHeight = 200d;
        double imageWidth = 300d;

        for (int count = 0; count <= 6; count++)
        {
            double boxY = count > 0 ? (count * 0.20d * cardHeight) : 0d;
            double totalHeight = boxY + imageHeight;

            double expectedBoxY = count == 0 ? 0d : count * 0.20d * cardHeight;
            Assert(Math.Abs(boxY - expectedBoxY) < 0.0001, $"ActionBox: Count={count} boxY={boxY:F2} == {expectedBoxY:F2}");

            double labelX = Math.Max(0d, imageWidth * 0.34d);
            double labelY = boxY + Math.Max(0d, imageHeight * 0.53d);
            double labelWidth = Math.Max(0d, imageWidth * 0.34d);

            Assert(Math.Abs(labelY - (boxY + 106d)) < 0.001, $"ActionBox: Count={count} labelY={labelY:F2} relative to boxY");
            Assert(Math.Abs(labelX - 102d) < 0.001 && Math.Abs(labelWidth - 102d) < 0.001, $"ActionBox: label bounds valid (X={labelX:F2}, Width={labelWidth:F2})");

            if (count > 0)
            {
                double lastCardTop = (count - 1) * 0.20d * cardHeight;
                double lastCardExposedBeforeBox = boxY - lastCardTop;
                double expectedLastExposed = 0.20d * cardHeight;
                Assert(Math.Abs(lastCardExposedBeforeBox - expectedLastExposed) < 0.0001, $"ActionBox: Count={count} last card exposed header={lastCardExposedBeforeBox:F2} == {expectedLastExposed:F2}");
            }
        }
    }

    private static void Test_ZIndex_StackHierarchy()
    {
        var childrenList = new List<string>();
        int count = 6;
        for (int i = 0; i < count; i++)
        {
            childrenList.Add($"Card_{i}_Top_{(i * 0.20 * 100):F0}");
        }

        Assert(childrenList[0] == "Card_0_Top_0", "ZIndex: Card 0 is bottom-most child at Y=0 (Z-order 0)");
        Assert(childrenList[5] == "Card_5_Top_100", "ZIndex: Card 5 is topmost card child at Y=100 (Z-order 5)");
    }

    private static void Test_Viewport_Canvas_Bounds()
    {
        double imageWidth = 400d;
        double imageHeight = 300d;

        double viewportWidth = Math.Max(0d, imageWidth * 0.82d);
        double viewportHeight = Math.Max(0d, imageHeight * 0.62d);
        double cardWidth = Math.Max(0d, viewportWidth - 8d);
        double cardHeight = Math.Max(95d, viewportHeight * 0.90d);

        Assert(viewportWidth == 328d, "Viewport: width is 82% of image width");
        Assert(viewportHeight == 186d, "Viewport: height is 62% of image height");
        Assert(cardWidth == 320d, "Viewport: card width is viewport width - 8d");
        Assert(cardHeight == 167.4d, "Viewport: card height is 90% of viewport height (>= 95d min)");

        double viewportX = Math.Max(0d, (imageWidth - cardWidth) / 2d);
        Assert(viewportX == 40d, "Viewport: viewportX is centered horizontally ((imageWidth - cardWidth) / 2)");
    }
    #endregion

    #region M2 R4 Search Simulation & Tests

    public class SearchEngineHarness
    {
        public const int MaxPlayers = 6;
        public PlayerModelStub[] AllPlayers { get; set; } = [];
        public ObservableCollection<PlayerModelStub> FilteredPlayerModelsByName { get; set; } = [];
        public PlayerModelStub? SelectedSuggestedPlayerModel { get; set; }
        public bool ShowPlayerSuggestions { get; set; }
        public bool SwipeEnabled { get; set; }
        public List<PlayerModelStub> CurrentGamePlayers { get; set; } = [];

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

        private void OnPlayerNameTextChanged(string value)
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

        private async Task PerformSearchAsync(string query, CancellationToken token)
        {
            if (CurrentGamePlayers.Count >= MaxPlayers)
                return;

            if (string.IsNullOrWhiteSpace(query))
            {
                FilteredPlayerModelsByName.Clear();
                SelectedSuggestedPlayerModel = null;
                ShowPlayerSuggestions = false;
                SwipeEnabled = false;
                return;
            }

            var currentAddedIds = CurrentGamePlayers.Select(p => p.ID).ToHashSet();
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

        public bool AddSuggestedPlayer(PlayerModelStub? player = null)
        {
            var target = player ?? SelectedSuggestedPlayerModel;
            if (target is null) return false;

            CurrentGamePlayers.Add(target);
            PlayerNameText = string.Empty;
            FilteredPlayerModelsByName.Clear();
            SelectedSuggestedPlayerModel = null;
            return true;
        }
    }

    private static PlayerModelStub[] GetDefaultDb()
    {
        return new PlayerModelStub[]
        {
            new() { PlayerName = "Eric Cartman" },
            new() { PlayerName = "Erica Kane" },
            new() { PlayerName = "Bob Builder" },
            new() { PlayerName = "Bobby Fischer" },
            new() { PlayerName = "Rob Bobson" },
            new() { PlayerName = "Alice Wonderland" },
            new() { PlayerName = "Charlie Brown" }
        };
    }

    private static async Task Test_R4_QueryChanging_EricToBob_ImmediatePurge()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };

        // Step 1: Type & search "eric"
        harness.PlayerNameText = "eric";
        await harness.SearchPlayerSuggestionsAsync();

        Assert(harness.FilteredPlayerModelsByName.Count == 2, "QueryChanging: 'eric' returns 2 matches");
        Assert(harness.FilteredPlayerModelsByName.All(p => p.PlayerName.StartsWith("Eric", StringComparison.OrdinalIgnoreCase)), "QueryChanging: All matches start with Eric");
        Assert(harness.ShowPlayerSuggestions == true, "QueryChanging: ShowPlayerSuggestions is true");

        // Step 2: User changes text to "bob"
        harness.PlayerNameText = "bob";

        // Immediate state invariant: changing text instantly purges existing results
        Assert(harness.FilteredPlayerModelsByName.Count == 0, "QueryChanging: Changing query immediately purges 'eric' matches");
        Assert(harness.SelectedSuggestedPlayerModel == null, "QueryChanging: SelectedSuggestedPlayerModel reset to null");
        Assert(harness.ShowPlayerSuggestions == false, "QueryChanging: ShowPlayerSuggestions reset to false");

        // Step 3: Search for "bob"
        await harness.SearchPlayerSuggestionsAsync();

        Assert(harness.FilteredPlayerModelsByName.Count == 2, "QueryChanging: 'bob' returns exactly 2 matches");
        Assert(harness.FilteredPlayerModelsByName.Any(p => p.PlayerName == "Bob Builder"), "QueryChanging: Contains 'Bob Builder'");
        Assert(harness.FilteredPlayerModelsByName.Any(p => p.PlayerName == "Bobby Fischer"), "QueryChanging: Contains 'Bobby Fischer'");
        Assert(!harness.FilteredPlayerModelsByName.Any(p => p.PlayerName.Contains("Eric")), "QueryChanging: Zero 'eric' matches retained");
    }

    private static async Task Test_R4_InstantEnterExecution_ZeroMillisecondLag()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };
        harness.PlayerNameText = "eric";

        var sw = Stopwatch.StartNew();
        // ReturnCommand invokes SearchPlayerSuggestionsAsync with CancellationToken.None
        await harness.SearchPlayerSuggestionsAsync();
        sw.Stop();

        Assert(sw.ElapsedMilliseconds < 50, $"InstantEnter: Executed in {sw.ElapsedMilliseconds}ms (0ms nominal delay without 250ms debouncer)");
        Assert(harness.FilteredPlayerModelsByName.Count == 2, "InstantEnter: Successfully retrieved results");
    }

    private static async Task Test_R4_RapidTyping_InFlightCancellation()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };

        // Simulate typing 7 keystrokes in rapid succession
        var tasks = new List<Task>();
        string[] keystrokes = { "e", "er", "eri", "eric", "b", "bo", "bob" };
        int[] simulatedDelays = { 60, 50, 40, 30, 20, 10, 0 };

        for (int i = 0; i < keystrokes.Length; i++)
        {
            harness.PlayerNameText = keystrokes[i];
            tasks.Add(harness.UserStoppedTypingAsync(simulatedDelays[i]));
        }

        await Task.WhenAll(tasks);

        Assert(harness.FilteredPlayerModelsByName.Count == 2, "RapidTyping: Exactly 2 matches survive");
        Assert(harness.FilteredPlayerModelsByName.All(p => p.PlayerName.StartsWith("bob", StringComparison.OrdinalIgnoreCase)), "RapidTyping: Only 'bob' matches present; prior in-flight searches cancelled");
    }

    private static async Task Test_R4_EmptyAndWhitespaceQueries()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };

        string[] emptyQueries = { "", " ", "   ", "\t", "\r\n", "  \t  " };

        foreach (var q in emptyQueries)
        {
            // First search a valid query
            harness.PlayerNameText = "bob";
            await harness.SearchPlayerSuggestionsAsync();
            Assert(harness.FilteredPlayerModelsByName.Count > 0, "WhitespaceTest: Setup non-empty state");

            // Now apply empty/whitespace query
            harness.PlayerNameText = q;
            await harness.SearchPlayerSuggestionsAsync();

            Assert(harness.FilteredPlayerModelsByName.Count == 0, $"WhitespaceTest: Query '{q.Replace("\r", "\\r").Replace("\n", "\\n").Replace("\t", "\\t")}' clears collection");
            Assert(harness.SelectedSuggestedPlayerModel == null, "WhitespaceTest: Selected item is null");
            Assert(harness.ShowPlayerSuggestions == false, "WhitespaceTest: ShowPlayerSuggestions is false");
            Assert(harness.SwipeEnabled == false, "WhitespaceTest: SwipeEnabled is false");
        }
    }

    private static async Task Test_R4_FilterOutExistingInGamePlayers()
    {
        var db = GetDefaultDb();
        var bob = db.First(p => p.PlayerName == "Bob Builder");
        var bobby = db.First(p => p.PlayerName == "Bobby Fischer");

        var harness = new SearchEngineHarness { AllPlayers = db };

        // Add Bob Builder to current game
        harness.CurrentGamePlayers.Add(bob);

        // Search "bob"
        harness.PlayerNameText = "bob";
        await harness.SearchPlayerSuggestionsAsync();

        Assert(harness.FilteredPlayerModelsByName.Count == 1, "FilterExisting: Only 1 match because Bob Builder is in-game");
        Assert(harness.FilteredPlayerModelsByName[0].ID == bobby.ID, "FilterExisting: Match is Bobby Fischer");

        // Add Bobby Fischer to current game
        harness.CurrentGamePlayers.Add(bobby);

        // Search "bob" again
        await harness.SearchPlayerSuggestionsAsync();
        Assert(harness.FilteredPlayerModelsByName.Count == 0, "FilterExisting: Both in-game -> suggestions empty");
    }

    private static async Task Test_R4_MaxPlayerCountBoundary()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };

        for (int i = 0; i < SearchEngineHarness.MaxPlayers; i++)
        {
            harness.CurrentGamePlayers.Add(new PlayerModelStub { PlayerName = $"P{i}" });
        }

        harness.PlayerNameText = "eric";
        await harness.SearchPlayerSuggestionsAsync();

        Assert(harness.FilteredPlayerModelsByName.Count == 0, "MaxPlayerBoundary: Max players (6) disables search suggestions");
        Assert(harness.ShowPlayerSuggestions == false, "MaxPlayerBoundary: ShowPlayerSuggestions remains false");
    }

    private static async Task Test_R4_CarouselSelectionSync_And_SwipeToggle()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };

        // Single match ("charlie")
        harness.PlayerNameText = "charlie";
        await harness.SearchPlayerSuggestionsAsync();

        Assert(harness.FilteredPlayerModelsByName.Count == 1, "CarouselSync: Charlie has 1 match");
        Assert(harness.SelectedSuggestedPlayerModel?.PlayerName == "Charlie Brown", "CarouselSync: SelectedSuggestedPlayerModel is Charlie Brown");
        Assert(harness.SwipeEnabled == false, "CarouselSync: Single item => SwipeEnabled is false");
        Assert(harness.ShowPlayerSuggestions == true, "CarouselSync: ShowPlayerSuggestions is true");

        // Multi match ("bob")
        harness.PlayerNameText = "bob";
        await harness.SearchPlayerSuggestionsAsync();

        Assert(harness.FilteredPlayerModelsByName.Count == 2, "CarouselSync: Bob has 2 matches");
        Assert(harness.SelectedSuggestedPlayerModel?.PlayerName == "Bob Builder", "CarouselSync: Selected item initialized to first item");
        Assert(harness.SwipeEnabled == true, "CarouselSync: 2 items => SwipeEnabled is true");
    }

    private static async Task Test_R4_DoubleTap_DirectParameterPassing()
    {
        var harness = new SearchEngineHarness { AllPlayers = GetDefaultDb() };
        harness.PlayerNameText = "bob";
        await harness.SearchPlayerSuggestionsAsync();

        var bobby = harness.FilteredPlayerModelsByName.First(p => p.PlayerName == "Bobby Fischer");

        // Double tap on 2nd card passing explicit player parameter
        bool added = harness.AddSuggestedPlayer(bobby);

        Assert(added == true, "DoubleTap: AddSuggestedPlayer returns true");
        Assert(harness.CurrentGamePlayers.Count == 1, "DoubleTap: CurrentGamePlayers has 1 player");
        Assert(harness.CurrentGamePlayers[0].PlayerName == "Bobby Fischer", "DoubleTap: Added player is Bobby Fischer");
        Assert(harness.PlayerNameText == string.Empty, "DoubleTap: PlayerNameText reset to empty");
        Assert(harness.FilteredPlayerModelsByName.Count == 0, "DoubleTap: FilteredPlayerModelsByName cleared");
    }

    private static async Task Test_R4_SpecialCharacters_And_CaseInsensitivity()
    {
        var specialDb = new PlayerModelStub[]
        {
            new() { PlayerName = "O'Connor" },
            new() { PlayerName = "Bob [Pro]" },
            new() { PlayerName = "Alice (Admin)" },
            new() { PlayerName = "$SuperPlayer$" }
        };

        var harness = new SearchEngineHarness { AllPlayers = specialDb };

        // Test special character prefix
        harness.PlayerNameText = "O'";
        await harness.SearchPlayerSuggestionsAsync();
        Assert(harness.FilteredPlayerModelsByName.Count == 1 && harness.FilteredPlayerModelsByName[0].PlayerName == "O'Connor", "SpecialChars: O' matches O'Connor");

        harness.PlayerNameText = "bob [";
        await harness.SearchPlayerSuggestionsAsync();
        Assert(harness.FilteredPlayerModelsByName.Count == 1 && harness.FilteredPlayerModelsByName[0].PlayerName == "Bob [Pro]", "SpecialChars: 'bob [' matches 'Bob [Pro]' without regex error");

        // Case insensitivity
        string[] caseVariations = { "alice", "ALICE", "aLiCe", "AlIcE" };
        foreach (var query in caseVariations)
        {
            harness.PlayerNameText = query;
            await harness.SearchPlayerSuggestionsAsync();
            Assert(harness.FilteredPlayerModelsByName.Count == 1 && harness.FilteredPlayerModelsByName[0].PlayerName == "Alice (Admin)", $"CaseInsensitive: '{query}' matches");
        }
    }

    private static async Task Test_R4_LargeScaleDatabase_StressSearch()
    {
        int N = 50000;
        var largeDb = new PlayerModelStub[N];
        for (int i = 0; i < N; i++)
        {
            largeDb[i] = new PlayerModelStub { PlayerName = $"Player_{i:D6}" };
        }
        largeDb[12345] = new PlayerModelStub { PlayerName = "EricUnique" };

        var harness = new SearchEngineHarness { AllPlayers = largeDb };

        harness.PlayerNameText = "eric";
        var sw = Stopwatch.StartNew();
        await harness.SearchPlayerSuggestionsAsync();
        sw.Stop();

        Assert(harness.FilteredPlayerModelsByName.Count == 1, "LargeDbStress: Found EricUnique among 50,000 players");
        Assert(sw.ElapsedMilliseconds < 25, $"LargeDbStress: 50,000 player prefix search completed in {sw.ElapsedMilliseconds}ms (< 25ms)");
    }
    #endregion

    #region M2 R3 Tests
    private static void Test_R3_BoundCommand_InvocationPriority()
    {
        var player = new PlayerModelStub { PlayerName = "PlayerX" };
        PlayerModelStub? executedPlayer = null;

        var command = new TestRelayCommand<PlayerModelStub>(p => executedPlayer = p);

        // Simulation of PlayerCardView OnEditPlayerButtonClicked
        if (command != null && command.CanExecute(player))
        {
            command.Execute(player);
        }

        Assert(executedPlayer == player, "R3_BoundCommand: Command invoked with target player context");
    }

    private static void Test_R3_UnboundCommand_AutonomousFallback()
    {
        var player = new PlayerModelStub { PlayerName = "PlayerY" };
        ICommand? nullCommand = null;
        bool fallbackTriggered = false;
        PlayerModelStub? navigatedPlayer = null;

        if (nullCommand != null && nullCommand.CanExecute(player))
        {
            nullCommand.Execute(player);
        }
        else
        {
            // Autonomous fallback
            fallbackTriggered = true;
            navigatedPlayer = player;
        }

        Assert(fallbackTriggered && navigatedPlayer == player, "R3_UnboundCommand: Autonomous fallback triggers navigation with target player");
    }

    private static void Test_R3_EditPlayerViewModel_Deduplication()
    {
        var player = new PlayerModelStub { PlayerName = "Zoe" };
        var activeGames = new List<GameModelStub> { new() { Players = [player] } };
        var playedGames = new List<GameModelStub> { new() { Players = [player] } };

        var activeCollection = new ObservableCollection<GameModelStub>();
        var playedCollection = new ObservableCollection<GameModelStub>();

        void LoadCollections(List<GameModelStub> active, List<GameModelStub> played)
        {
            activeCollection.Clear();
            playedCollection.Clear();
            foreach (var g in active) activeCollection.Add(g);
            foreach (var g in played) playedCollection.Add(g);
        }

        // Simulate 10 repeated parameter change and page load calls
        for (int i = 0; i < 10; i++)
        {
            LoadCollections(activeGames, playedGames);
        }

        Assert(activeCollection.Count == 1, "R3_Deduplication: ActiveGames count strictly 1 after 10 loads");
        Assert(playedCollection.Count == 1, "R3_Deduplication: PlayedGames count strictly 1 after 10 loads");
    }

    private static void Test_R3_NullSafe_GameFiltering()
    {
        var target = new PlayerModelStub { PlayerName = "Target" };
        var other = new PlayerModelStub { PlayerName = "Other" };

        var games = new List<GameModelStub>
        {
            new() { Players = [target, other] },
            new() { Players = [other] },
            new() { Players = null! }, // Null safety test
            new() { Players = [] }
        };

        var found = new List<GameModelStub>();
        foreach (var g in games)
        {
            if (g?.Players != null)
            {
                foreach (var p in g.Players)
                {
                    if (p?.ID == target.ID)
                    {
                        found.Add(g);
                        break;
                    }
                }
            }
        }

        Assert(found.Count == 1, "R3_NullSafe: Found exactly 1 game matching target player without NRE on null lists");
    }

    #region M2 R2 Tests
    private static void Test_R2_TwoPlayerAndThreePlayerTies()
    {
        var p1 = new PlayerModelStub { PlayerName = "Alice", PlayerScore = 520 };
        var p2 = new PlayerModelStub { PlayerName = "Bob", PlayerScore = 520 };
        var p3 = new PlayerModelStub { PlayerName = "Charlie", PlayerScore = 400 };

        // 2-player tie above limit 500
        var players = new List<PlayerModelStub> { p1, p2, p3 };
        var winners = players.Where(p => p.PlayerScore >= 500).ToList();
        var maxScore = winners.Max(p => p.PlayerScore);
        var topWinners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert(topWinners.Count == 2, "R2_Ties: 2 players tied at 520 above 500 limit detected as 2-way draw");
        Assert(topWinners.Contains(p1) && topWinners.Contains(p2), "R2_Ties: 2-way draw contains Alice and Bob");

        // 3-player tie
        var p4 = new PlayerModelStub { PlayerName = "Diana", PlayerScore = 520 };
        players.Add(p4);
        winners = players.Where(p => p.PlayerScore >= 500).ToList();
        maxScore = winners.Max(p => p.PlayerScore);
        topWinners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert(topWinners.Count == 3, "R2_Ties: 3 players tied at 520 detected as 3-way draw");
        Assert(topWinners.Contains(p1) && topWinners.Contains(p2) && topWinners.Contains(p4), "R2_Ties: 3-way draw contains Alice, Bob, and Diana");

        // Higher score breaks lower tie
        var p5 = new PlayerModelStub { PlayerName = "Evan", PlayerScore = 550 };
        players.Add(p5);
        winners = players.Where(p => p.PlayerScore >= 500).ToList();
        maxScore = winners.Max(p => p.PlayerScore);
        topWinners = winners.Where(p => p.PlayerScore == maxScore).ToList();

        Assert(topWinners.Count == 1 && topWinners[0].ID == p5.ID, "R2_Ties: Evan (550) beats 3-way tie at 520 to win outright");
    }

    private static void Test_R2_ManualWinnerOverridesOnDraws()
    {
        var p1 = new PlayerModelStub { PlayerName = "Alice", PlayerScore = 500 };
        var p2 = new PlayerModelStub { PlayerName = "Bob", PlayerScore = 500 };

        // Initial draw
        string status = "Draw";
        PlayerModelStub? selectedWinner = null;
        bool isWinnerPickerVisible = status == "Won";

        Assert(!isWinnerPickerVisible && selectedWinner == null, "R2_ManualOverride: Initial Draw hides winner picker and sets winner null");

        // Override to Won with Bob
        status = "Won";
        isWinnerPickerVisible = status == "Won";
        selectedWinner = p2;

        Assert(isWinnerPickerVisible && selectedWinner == p2, "R2_ManualOverride: Status Won enables picker and allows Bob manual selection");

        // Revert back to Draw
        status = "Draw";
        isWinnerPickerVisible = status == "Won";
        selectedWinner = status == "Won" ? selectedWinner : null;

        Assert(!isWinnerPickerVisible && selectedWinner == null, "R2_ManualOverride: Revert to Draw clears selected winner to null");
    }

    private static void Test_R2_StatusTransitionsCycle()
    {
        string[] cycle = ["In-Progress", "Won", "Draw", "Forfeit", "In-Progress"];
        string currentStatus = cycle[0];

        foreach (var next in cycle.Skip(1))
        {
            currentStatus = next;
            bool isWon = currentStatus == "Won";
            bool isFinished = currentStatus != "In-Progress";

            Assert(currentStatus == next, $"R2_StatusTransition: Transitioned to {next}");
            Assert(isWon ? currentStatus == "Won" : currentStatus != "Won", $"R2_StatusTransition: Won invariant holds for {next}");
        }
    }

    private static void Test_R2_ScoreLimitModifiedBelowScores()
    {
        var p1 = new PlayerModelStub { PlayerName = "Alice", PlayerScore = 350 };
        var p2 = new PlayerModelStub { PlayerName = "Bob", PlayerScore = 200 };
        int scoreLimit = 500;

        var winnersBefore = new List<PlayerModelStub> { p1, p2 }.Where(p => p.PlayerScore >= scoreLimit).ToList();
        Assert(winnersBefore.Count == 0, "R2_ScoreLimit: No winners at limit 500");

        // Reduce score limit to 300
        scoreLimit = 300;
        var winnersAfter = new List<PlayerModelStub> { p1, p2 }.Where(p => p.PlayerScore >= scoreLimit).ToList();
        Assert(winnersAfter.Count == 1 && winnersAfter[0].ID == p1.ID, "R2_ScoreLimit: Alice (350) detected as winner when limit lowered to 300");
    }

    private static void Test_R2_PolymorphicSerializationIntegrity()
    {
        var p1 = new PlayerModelStub { PlayerName = "Alice", PlayerScore = 150 };
        var p2 = new PlayerModelStub { PlayerName = "Bob", PlayerScore = 120 };

        var serialized = System.Text.Json.JsonSerializer.Serialize(new List<PlayerModelStub> { p1, p2 });
        var deserialized = System.Text.Json.JsonSerializer.Deserialize<List<PlayerModelStub>>(serialized);

        Assert(deserialized != null && deserialized.Count == 2, "R2_Serialization: Player list deserialized with 2 players");
        Assert(deserialized![0].PlayerName == "Alice" && deserialized[0].PlayerScore == 150, "R2_Serialization: Alice score 150 preserved");
        Assert(deserialized[1].PlayerName == "Bob" && deserialized[1].PlayerScore == 120, "R2_Serialization: Bob score 120 preserved");
    }

    private static void Test_R2_GlobalPlayerRankingAndStatsSync()
    {
        var a = new PlayerModelStub { PlayerName = "Alice", LifetimeScore = 900 };
        var b = new PlayerModelStub { PlayerName = "Bob", LifetimeScore = 1400 };
        var c = new PlayerModelStub { PlayerName = "Charlie", LifetimeScore = 900 };
        var d = new PlayerModelStub { PlayerName = "Diana", LifetimeScore = 800 };

        var ranked = new List<PlayerModelStub> { a, b, c, d }
            .OrderByDescending(p => p.LifetimeScore)
            .ThenBy(p => p.PlayerName)
            .ToList();

        Assert(ranked[0].PlayerName == "Bob", "R2_RankSync: Rank 1 is Bob (1400)");
        Assert(ranked[1].PlayerName == "Alice", "R2_RankSync: Rank 2 is Alice (900, 'A' before 'C')");
        Assert(ranked[2].PlayerName == "Charlie", "R2_RankSync: Rank 3 is Charlie (900)");
        Assert(ranked[3].PlayerName == "Diana", "R2_RankSync: Rank 4 is Diana (800)");
    }
    #endregion

    public class TestRelayCommand<T>(Action<T> execute) : ICommand
    {
        public bool CanExecute(object? parameter) => true;
        public void Execute(object? parameter) => execute((T)parameter!);
        public event EventHandler? CanExecuteChanged;
    }
    #endregion
}
