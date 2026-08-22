# Investigation & Root Cause Analysis Report: CurrentGamePage Active Game Player Row Rendering

## 1. Observation

### Codebase Observations:

1. **Multiple Collections for Players in `CurrentGameViewModel.cs`**:
   - `CurrentGameViewModel.cs` (lines 32-38):
     ```csharp
     [ObservableProperty]
     public partial CurrentGameModel CurrentGame { get; set; } = new();

     [ObservableProperty]
     public partial ObservableCollection<PlayerModel> CurrentGamePlayers { get; set; } = [];

     public ObservableCollection<PlayerModel> Players { get; } = [];
     ```
   - `CurrentGamePage.xaml` (line 34):
     ```xml
     <CollectionView Grid.Row="2" ItemsSource="{Binding Players}" SelectionMode="None">
     ```
   - In `GameModel.cs` (line 12):
     ```csharp
     public ObservableCollection<PlayerModel> Players { get; set; } = new();
     ```
   - There are three distinct player collections: `CurrentGame.Players` (domain model collection), `CurrentGamePlayers` (unused `[ObservableProperty]`), and `Players` (empty initialized collection bound to CollectionView).

2. **Shadow Collection Synchronization via `SyncPlayers()`**:
   - In `CurrentGameViewModel.cs` (lines 317-344):
     ```csharp
     private void SyncPlayers()
     {
         if (MainThread.IsMainThread)
         {
             Players.Clear();
             if (CurrentGame?.Players != null)
             {
                 foreach (var p in CurrentGame.Players)
                 {
                     Players.Add(p);
                 }
             }
         }
         else
         {
             MainThread.BeginInvokeOnMainThread(() =>
             {
                 Players.Clear();
                 if (CurrentGame?.Players != null)
                 {
                     foreach (var p in CurrentGame.Players)
                     {
                         Players.Add(p);
                     }
                 }
             });
         }
     }
     ```
   - Calling `Players.Clear()` and sequentially calling `Players.Add(p)` emits multiple `CollectionChanged` notifications (`Reset`, followed by `Add` for each item).

3. **Lifecycle & Navigation Parameter Timing**:
   - In `CurrentGamePage.xaml.cs` (lines 9-19 and 26-35):
     ```csharp
     public partial class CurrentGamePage : BasePage<CurrentGameViewModel>, IQueryAttributable
     {
         public CurrentGamePage(CurrentGameViewModel vm) : base(vm)
         {
             InitializeComponent();
         }

         public void ApplyQueryAttributes(IDictionary<string, object> query)
         {
             ViewModel.ApplyQueryAttributes(query);
         }

         protected override void OnAppearing()
         {
             base.OnAppearing();
             ViewModel?.OnAppearing();
             ...
         }
     ```
   - In `CurrentGameViewModel.cs` (lines 5-14 and 305-315):
     ```csharp
     public partial class CurrentGameViewModel(IPopupService popupService, GameService gameService)
         : BaseViewModel(popupService, gameService), IQueryAttributable
     {
         public void ApplyQueryAttributes(IDictionary<string, object> query)
         {
             if (query.TryGetValue("CurrentGame", out var gameObj) && gameObj is CurrentGameModel gameModel)
             {
                 CurrentGame = gameModel;
             }
         }

         public void OnAppearing()
         {
             if (CurrentGame != null)
             {
                 ScoreLimit = CurrentGame.ScoreLimit;
                 _gameService.RecalculateGame(CurrentGame);
                 UpdateRoundNavigationState();
                 CurrentGamePlayers = CurrentGame.Players;
                 SyncPlayers();
             }
         }
     ```
   - When navigating via Shell, `OnAppearing` may fire prior to `ApplyQueryAttributes`. At `OnAppearing` time, `CurrentGame` is the default instance initialized with 0 players. If `SyncPlayers()` runs at that point, `Players` remains empty.
   - When `ApplyQueryAttributes` subsequently runs, `CurrentGame = gameModel` invokes `OnCurrentGameChanged`, which calls `SyncPlayers()`. However, because `SyncPlayers()` uses `MainThread.BeginInvokeOnMainThread` when not on the main thread, or clears and re-adds items during active layout passes, Android's `RecyclerView` measure/layout pass can finish with 0-height or fail to render item views immediately until an explicit interaction or relayout triggers.

4. **Style Conflicts on Round Score Entry**:
   - In `CurrentGamePage.xaml` (lines 66-68):
     ```xml
     <Border Grid.Row="0" Grid.Column="4" Style="{StaticResource TagEntryBorder}" HorizontalOptions="Center" VerticalOptions="Center" WidthRequest="70" Padding="0">
         <Entry Text="{Binding PlayerScoreText, Mode=TwoWay}" HorizontalOptions="Fill" VerticalOptions="Center" HorizontalTextAlignment="Center" FontSize="15" Keyboard="Numeric" Style="{StaticResource TagEntry}" />
     </Border>
     ```
   - In `Resources/Styles/Styles.xaml` (lines 39-43):
     ```xml
     <Style TargetType="Entry" x:Key="TagEntry">
         <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource DeepRed}, Dark={StaticResource White}}" />
         <Setter Property="FontSize" Value="25" />
         <Setter Property="WidthRequest" Value="150" />
     </Style>
     ```
   - `TagEntry` specifies `WidthRequest="150"`. The parent `Border` specifies `WidthRequest="70"`. Inside `Grid ColumnDefinitions="*,2,95,2,115"`, this causes measuring conflicts where `Entry` requests 150px within a 70px border in a 115px column.

5. **Concurrency and Safety in `CalculatePlayerScores`**:
   - In `CurrentGameViewModel.cs` (line 93):
     ```csharp
     await Task.WhenAll(CurrentGame.Players.Select(player => _gameService.SetRoundPlayersScoredHandsAsync(player, CurrentRound)));
     ```
   - In `GameService.cs` (lines 365-371):
     ```csharp
     public async Task<bool> SetRoundPlayersScoredHandsAsync(PlayerModel player, RoundModel roundModel)
     {
         var results = false;
         roundModel.PlayersScoredHandThisRound.Add(player);
         results = true;
         return results;
     }
     ```
   - `roundModel.PlayersScoredHandThisRound` is an `ObservableCollection<PlayerModel>`. `ObservableCollection.Add` is not thread-safe. Concurrent additions via `Task.WhenAll` can throw or corrupt internal structures.
   - In `GameService.cs` (line 428):
     ```csharp
     var currentDealerIndex = currentGame
         .Players
         .IndexOf(currentGame
                     .Players
                     .First(p => p.IsDealer));
     ```
     `.First(p => p.IsDealer)` will throw `InvalidOperationException` if no dealer was assigned.

---

## 2. Logic Chain

1. **Premise 1**: In `CurrentGamePage.xaml`, the player list is bound to `{Binding Players}` on `CurrentGameViewModel`.
2. **Premise 2**: `Players` in `CurrentGameViewModel` is a disconnected shadow `ObservableCollection<PlayerModel>` that starts empty `[]`, while the actual game state lives in `CurrentGame.Players`.
3. **Premise 3**: When Shell navigates to `CurrentGamePage`, the page is instantiated before navigation parameters are delivered via `ApplyQueryAttributes`.
4. **Premise 4**: If `OnAppearing()` executes before `ApplyQueryAttributes`, `SyncPlayers()` clears `Players` and sees `CurrentGame.Players` with 0 items.
5. **Premise 5**: When `ApplyQueryAttributes` delivers `CurrentGame`, `OnCurrentGameChanged` invokes `SyncPlayers()`, which performs `Players.Clear()` followed by sequential `Players.Add(p)`.
6. **Premise 6**: In .NET MAUI on Android, `CollectionView` backed by `RecyclerView` measuring inside a `Grid` row `*` can miscalculate item sizes or drop layout cycles when subjected to rapid `Reset` + multiple `Add` events during page presentation, causing player rows (e.g. Brodie and Renegade) to fail to render immediately.
7. **Premise 7**: Binding `CollectionView.ItemsSource` directly to `{Binding CurrentGame.Players}` (or providing a direct property that returns `CurrentGame.Players` with notification) eliminates the shadow collection, removes `SyncPlayers()` churn, and ensures that all items are present in the collection when the `CurrentGame` property is set.
8. **Premise 8**: Correcting the `Entry` `WidthRequest` conflict inside `Border WidthRequest="70"` prevents Android measurement anomalies for the round score input box.

---

## 3. Caveats

- Android device testing was inspected via codebase analysis; live on-device rendering timings can vary slightly between debug and release builds due to JIT vs AOT layout measurement differences.
- No other pages or viewmodels require changes to resolve this active game player row rendering issue, but `GameService.SetNextDealerForNewRoundAsync` and `SetRoundPlayersScoredHandsAsync` should be safeguarded against concurrency/null-dealer exceptions.

---

## 4. Conclusion & Implementation Recommendations for Worker

### Root Causes Identified:
1. **Shadow Collection & `SyncPlayers()` Desynchronization**: `CollectionView` was bound to a separate `Players` collection rather than `CurrentGame.Players`. The `Clear()` + multiple `Add()` pattern in `SyncPlayers()` caused collection notification churn and timing race conditions with Shell navigation and Android `RecyclerView` layout passes.
2. **Double `IQueryAttributable` Delivery**: Both `CurrentGamePage` and `CurrentGameViewModel` implemented `IQueryAttributable`, causing duplicate attribute handling passes.
3. **Entry WidthRequest Layout Conflict**: `TagEntry` style width (`150px`) exceeded parent `Border` width (`70px`), causing Android child layout constraints and text clipping.
4. **Missing PropertyChanged wireup in OnAppearing**: `Player_PropertyChanged` was only attached during `OnCurrentGameChanged` and `OnCurrentRoundChanged`, risking missing can-execute re-evaluations if the viewmodel was reused.
5. **Non-thread-safe collection mutations and unsafe `.First()` in `GameService`**: `Task.WhenAll` on `ObservableCollection.Add` and `.First(p => p.IsDealer)` in dealer rotation posed runtime stability risks during score calculation.

### Recommended Worker Changes:

1. **`CurrentGamePage.xaml`**:
   - Change CollectionView ItemsSource from `{Binding Players}` to `{Binding CurrentGame.Players}`:
     ```xml
     <CollectionView Grid.Row="2" ItemsSource="{Binding CurrentGame.Players}" SelectionMode="None">
     ```
   - On the Round Score `Entry` (line 67), override `WidthRequest` to fit cleanly inside the 70px border:
     ```xml
     <Border Grid.Row="0" Grid.Column="4" Style="{StaticResource TagEntryBorder}" HorizontalOptions="Center" VerticalOptions="Center" WidthRequest="70" Padding="0">
         <Entry Text="{Binding PlayerScoreText, Mode=TwoWay}" HorizontalOptions="Fill" VerticalOptions="Center" HorizontalTextAlignment="Center" FontSize="15" Keyboard="Numeric" WidthRequest="60" Style="{StaticResource TagEntry}" />
     </Border>
     ```

2. **`CurrentGameViewModel.cs`**:
   - Eliminate `SyncPlayers()` and shadow `Players` / `CurrentGamePlayers` collections, or have `Players` directly return `CurrentGame?.Players ?? []`.
   - In `OnAppearing()`, ensure `Player_PropertyChanged` is subscribed for all players in `CurrentGame.Players`.
   - In `CalculatePlayerScores`, perform round score additions sequentially on the main thread rather than concurrent `Task.WhenAll` on `ObservableCollection`.

3. **`GameService.cs`**:
   - In `SetNextDealerForNewRoundAsync`, replace `.First(p => p.IsDealer)` with `.FirstOrDefault(p => p.IsDealer)` and fallback to `SetRandomDealerForCurrentGameAsync(currentGame)` if null.

4. **Unit Tests (`tests/RummyBooky.Tests`)**:
   - Fix string quoting syntax in `AdversarialR2StressTests.cs` (e.g. line 89 `$"Player {i}"` and string literals) so all unit tests compile and run with 0 failures.

---

## 5. Verification Method

1. **Compilation Verification**:
   - Run: `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-windows10.0.19041.0`
   - Verify 0 errors, 0 warnings.
2. **Automated Unit Tests**:
   - Run: `dotnet test tests\RummyBooky.Tests\RummyBooky.Tests.csproj`
   - Verify all unit tests pass with 0 failures.
3. **Live UI Verification on Physical Pixel Tablet (10.0.0.66:45305)**:
   - Deploy signed Release APK to user profile 0.
   - Navigate to New Game -> Add Brodie -> Add Renegade -> Set Score Limit (e.g. 500) -> Tap Start Game.
   - Verify `CurrentGamePage` immediately displays both Brodie and Renegade with:
     - Player name labels
     - Dealer icon badge on the dealer
     - Running total score: 0
     - Interactive round score Entry box
   - Enter 50 for Brodie, 0 for Renegade -> Tap "Calculate Scores".
   - Verify Round advances to 2, Brodie score becomes 50, dealer icon rotates to Renegade, and game persists cleanly.
