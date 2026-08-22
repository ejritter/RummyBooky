# Comprehensive Survey Report: Requirement R4 and Build/Test Infrastructure

**Author**: Survey Explorer (explorer_survey_r4build)  
**Date**: 2026-08-14  
**Target Repository**: `c:\Dev\RummyBookyMaui`  
**Solution File**: `RummyBookyMaui.slnx`  
**Authoritative Reference**: `.agents/ORIGINAL_REQUEST.md`

---

## 1. Executive Summary

This investigation surveys **Requirement R4** (Player Search Synchronization & Instant Enter Trigger) and the **Build & Test Infrastructure** for the .NET MAUI `RummyBooky` application.

Key findings:
1. **R4 Root Cause Identified**:
   - In `NewGamePage.xaml` (line 18), `ReturnCommand="{Binding AddPlayerCommand}"` erroneously triggers player addition when pressing Enter/Return instead of executing the search query.
   - In `NewGamePage.xaml` (line 20), `UserStoppedTypingBehavior` has `StoppedTypingTimeThreshold="3000"` (3000ms delay), creating a severe 3-second lag before suggestions appear.
   - In `NewGameViewModel.cs` (lines 75-106), `UserStoppedTyping` lacks cancellation tokens (`CancellationTokenSource`) and race-condition guards. While `OnPlayerNameTextChanged` clears the collection on keypress, delayed asynchronous executions can populate stale results or race against newer queries.
   - In `NewGamePage.xaml` (line 31), `CarouselView` does not bind `CurrentItem` two-way, and line 61's double-tap gesture lacks a `CommandParameter`, preventing robust selection of suggestions.
2. **Build Infrastructure Verified**:
   - Multi-targeting .NET 10.0 (`net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`, `net10.0-windows10.0.19041.0`).
   - Clean builds verified:
     - `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-windows10.0.19041.0` -> **0 Errors, 0 Warnings** (12.67s).
     - `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-android` -> **0 Errors, 0 Warnings** (17.36s).
3. **Test Infrastructure Blueprint**:
   - Currently, no unit test project exists in the solution.
   - A complete xUnit test project specification (`RummyBooky.Tests`) has been designed covering unit tests for R1 (score ordering & layout math), R2 (expand bounds & animations), R3 (navigation & event routing), and R4 (search synchronization, debounce cancellation, instant Enter trigger).

---

## 2. Requirement R4 Deep Dive: Player Search Synchronization & Instant Enter Trigger

### 2.1 Problem Statements & Acceptance Criteria
- **Acceptance Criteria 1**: In `NewGamePage`, searching "bob" immediately after "eric" must immediately clear stale suggestions and synchronize the observable suggestions collection without retaining prior query results.
- **Acceptance Criteria 2**: Pressing the Enter / Return key on the player search entry must immediately execute the search query without delay or debouncing lag.

### 2.2 Root Cause Analysis (Exact Files & Line Numbers)

#### A. Flawed ReturnKey Command Binding (NewGamePage.xaml:18)
- **Location**: `RummyBooky/Pages/NewGamePage.xaml`, line 18:
  ```xaml
  <Entry x:Name="EntryPlayerName" 
         Placeholder="{Binding PlayerBoundaries}" 
         ReturnCommand="{Binding AddPlayerCommand}" 
         ReturnCommandParameter="{Binding Source={x:Reference EntryPlayerName}}" 
         Text="{Binding PlayerNameText, Mode=TwoWay}" 
         Style="{StaticResource TagEntry}">
  ```
- **Analysis**:
  - When the user types a query (e.g. "eric" or "bob") and presses the Enter/Return key, `ReturnCommand` executes `AddPlayerCommand`.
  - This immediately adds the typed text as a brand-new player rather than triggering the search query to show matching existing players.
  - This directly violates R4's requirement: *"Pressing the Enter / Return key on the player search entry must immediately execute the search query without delay or debouncing lag."*

#### B. 3000ms Debounce Lag in UserStoppedTypingBehavior (NewGamePage.xaml:20)
- **Location**: `RummyBooky/Pages/NewGamePage.xaml`, lines 19-22:
  ```xaml
  <Entry.Behaviors>
      <toolkit:UserStoppedTypingBehavior 
          BindingContext="{Binding Path=BindingContext, Source={x:Reference EntryPlayerName}, x:DataType=Entry}" 
          StoppedTypingTimeThreshold="3000" 
          ShouldDismissKeyboardAutomatically="False" 
          Command="{Binding UserStoppedTypingCommand}" />
  </Entry.Behaviors>
  ```
- **Analysis**:
  - `StoppedTypingTimeThreshold="3000"` sets the inactivity threshold to **3000 milliseconds (3 seconds)**.
  - Users must wait 3 full seconds after typing before suggestions are populated.
  - This produces noticeable UI lag and creates a disconnect between user typing and search feedback.

#### C. Asynchronous Race Conditions & ObservableCollection Updates (NewGameViewModel.cs:75-106, 137-145)
- **Location**: `RummyBooky/ViewModels/NewGameViewModel.cs`, lines 75-106:
  ```csharp
  [RelayCommand]
  public async Task UserStoppedTyping()
  {
      if (GameModelTemplate.Players.Count >= IntConstants.MaximumPlayerCount)
          return;

      SelectedSuggestedPlayerModel = null;
      FilteredPlayerModelsByName.Clear();

      if (string.IsNullOrWhiteSpace(PlayerNameText))
          return;

      var matches = AllPlayerModels
          .Where(p => p.PlayerName.StartsWith(PlayerNameText, StringComparison.OrdinalIgnoreCase) &&
                      GameModelTemplate.Players.Any(gp => gp.ID == p.ID) == false)
          .ToList();

      if (MainThread.IsMainThread)
      {
          foreach (var player in matches)
              FilteredPlayerModelsByName.Add(player);
      }
      else
      {
          await MainThread.InvokeOnMainThreadAsync(() =>
          {
              foreach (var player in matches)
                  FilteredPlayerModelsByName.Add(player);
          });
      }

      SelectedSuggestedPlayerModel = FilteredPlayerModelsByName.FirstOrDefault();
  }
  ```
- **Location**: `RummyBooky/ViewModels/NewGameViewModel.cs`, lines 137-145:
  ```csharp
  partial void OnPlayerNameTextChanged(string value)
  {
      SelectedSuggestedPlayerModel = null;
      FilteredPlayerModelsByName.Clear();
      ShowPlayerSuggestions = false;
      SwipeEnabled = false;
      CanAddPlayer();
      AddPlayerCommand.NotifyCanExecuteChanged();
  }
  ```
- **Analysis**:
  1. **No CancellationToken**: If query "eric" is dispatched and then the user types "bob", there is no cancellation token to abort the pending search or debounce timer for "eric".
  2. **Sequential ObservableCollection mutations**: Iterating with `FilteredPlayerModelsByName.Add(player)` raises `CollectionChanged` for every item. In `FilteredPlayerModelsByName_CollectionChanged` (line 108), each event updates `ShowPlayerSuggestions`, which triggers `OnShowPlayerSuggestionsChanged` (line 46) and flips `ShowGridTemplate`.
  3. **Circular Property Dependency**: Line 23 (`[NotifyPropertyChangedFor(nameof(ShowGridTemplate))]`) and Line 169 (`[NotifyPropertyChangedFor(nameof(ShowPlayerSuggestions))]`) create cross-property notifications that can trigger UI churn during collection updates.

#### D. CarouselView Selection & Gesture Parameter Deficiencies (NewGamePage.xaml:31, 61)
- **Location**: `RummyBooky/Pages/NewGamePage.xaml`, lines 31 and 61:
  ```xaml
  <CarouselView x:Name="SuggestedPlayersCollection" ItemsSource="{Binding FilteredPlayerModelsByName}" IsSwipeEnabled="{Binding SwipeEnabled}" PeekAreaInsets="0">
  ...
  <TapGestureRecognizer NumberOfTapsRequired="2" Command="{Binding Source={RelativeSource AncestorType={x:Type pages:NewGamePage}}, Path=BindingContext.AddSuggestedPlayerCommand}" />
  ```
- **Analysis**:
  - `CarouselView` does not bind `CurrentItem="{Binding SelectedSuggestedPlayerModel, Mode=TwoWay}"`.
  - When the user swipes through suggestions, `SelectedSuggestedPlayerModel` is not synchronized with the visible card in the CarouselView.
  - When double tapping a suggestion, `AddSuggestedPlayerCommand` (line 189 in `NewGameViewModel.cs`) adds `SelectedSuggestedPlayerModel` without receiving the tapped player item as `CommandParameter`.

---

### 2.3 Proposed Remediation for Requirement R4

#### Architecture Strategy:
1. **Instant Enter Execution**:
   - Define `ExecuteSearchCommand` / `SearchPlayerSuggestionsCommand` in `NewGameViewModel`.
   - In `NewGamePage.xaml`, set `EntryPlayerName.ReturnCommand="{Binding SearchPlayerSuggestionsCommand}"`.
   - When Enter is pressed, cancel pending debounce and execute search immediately.
2. **Deterministic Debounce & Cancellation Management**:
   - Introduce `CancellationTokenSource? _searchCts` in `NewGameViewModel`.
   - On `OnPlayerNameTextChanged`:
     - Cancel `_searchCts` immediately.
     - Synchronously clear `FilteredPlayerModelsByName`, set `SelectedSuggestedPlayerModel = null`, and set `ShowPlayerSuggestions = false`.
     - Trigger debounced search with standard ~250ms threshold (or coordinate with `UserStoppedTypingBehavior` set to ~250-300ms).
3. **Synchronized Atomic ObservableCollection Population**:
   - Filter `AllPlayerModels` on background thread or main thread.
   - Atomically populate `FilteredPlayerModelsByName` on `MainThread`.
   - Synchronize `ShowPlayerSuggestions = FilteredPlayerModelsByName.Count > 0` and `SwipeEnabled = FilteredPlayerModelsByName.Count > 1`.
4. **CarouselView Two-Way Synchronization**:
   - Bind `CurrentItem="{Binding SelectedSuggestedPlayerModel, Mode=TwoWay}"` on `CarouselView`.
   - Pass `CommandParameter="{Binding .}"` on the double-tap `TapGestureRecognizer`.
   - Update `AddSuggestedPlayer(PlayerModel? player = null)` to prioritize passed parameter over `SelectedSuggestedPlayerModel`.

#### Code Proposal: `NewGameViewModel.cs` Search Logic
```csharp
private CancellationTokenSource? _searchCts;

[ObservableProperty]
[NotifyCanExecuteChangedFor(nameof(AddPlayerCommand))]
public partial string PlayerNameText { get; set; } = string.Empty;

partial void OnPlayerNameTextChanged(string value)
{
    _searchCts?.Cancel();
    _searchCts?.Dispose();
    _searchCts = null;

    SelectedSuggestedPlayerModel = null;
    FilteredPlayerModelsByName.Clear();
    ShowPlayerSuggestions = false;
    SwipeEnabled = false;

    CanAddPlayer();
    AddPlayerCommand.NotifyCanExecuteChanged();
}

[RelayCommand]
public async Task SearchPlayerSuggestions()
{
    // Instant Enter trigger: cancels any pending debounce and searches immediately
    _searchCts?.Cancel();
    _searchCts?.Dispose();
    _searchCts = null;

    await PerformSearchAsync(PlayerNameText, CancellationToken.None);
}

[RelayCommand]
public async Task UserStoppedTyping()
{
    // Debounced trigger from UserStoppedTypingBehavior
    _searchCts?.Cancel();
    _searchCts?.Dispose();
    _searchCts = new CancellationTokenSource();
    var token = _searchCts.Token;

    await PerformSearchAsync(PlayerNameText, token);
}

private async Task PerformSearchAsync(string query, CancellationToken token)
{
    if (GameModelTemplate.Players.Count >= IntConstants.MaximumPlayerCount)
        return;

    if (string.IsNullOrWhiteSpace(query))
    {
        await MainThread.InvokeOnMainThreadAsync(() =>
        {
            FilteredPlayerModelsByName.Clear();
            SelectedSuggestedPlayerModel = null;
            ShowPlayerSuggestions = false;
            SwipeEnabled = false;
        });
        return;
    }

    var currentAddedIds = GameModelTemplate.Players.Select(p => p.ID).ToHashSet();
    var matches = AllPlayerModels
        .Where(p => p.PlayerName.StartsWith(query.Trim(), StringComparison.OrdinalIgnoreCase) &&
                    !currentAddedIds.Contains(p.ID))
        .ToList();

    if (token.IsCancellationRequested)
        return;

    await MainThread.InvokeOnMainThreadAsync(() =>
    {
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
    });
}
```

#### Code Proposal: `NewGamePage.xaml` Entry Configuration
```xaml
<Border Grid.Column="1" Grid.Row="1" HorizontalOptions="End" Style="{StaticResource TagEntryBorder}">
    <Entry x:Name="EntryPlayerName" 
           Placeholder="{Binding PlayerBoundaries}" 
           ReturnCommand="{Binding SearchPlayerSuggestionsCommand}" 
           Text="{Binding PlayerNameText, Mode=TwoWay}" 
           Style="{StaticResource TagEntry}">
        <Entry.Behaviors>
            <toolkit:UserStoppedTypingBehavior 
                BindingContext="{Binding Path=BindingContext, Source={x:Reference EntryPlayerName}, x:DataType=Entry}" 
                StoppedTypingTimeThreshold="250" 
                ShouldDismissKeyboardAutomatically="False" 
                Command="{Binding UserStoppedTypingCommand}" />
        </Entry.Behaviors>
    </Entry>
</Border>
```

---

## 3. Build & Solution Infrastructure

### 3.1 Solution Structure
- **Solution File**: `RummyBookyMaui.slnx`
  ```xml
  <Solution>
    <Project Path="RummyBooky/RummyBooky.csproj">
      <Deploy Solution="Debug|*" />
    </Project>
  </Solution>
  ```
- **Project File**: `RummyBooky/RummyBooky.csproj`
  - Sdk: `Microsoft.NET.Sdk`
  - `<UseMaui>true</UseMaui>`
  - Target Frameworks:
    - `net10.0-android`
    - `net10.0-ios`
    - `net10.0-maccatalyst`
    - `net10.0-windows10.0.19041.0` (conditional on Windows)
  - Key Package References:
    - `CommunityToolkit.Maui` (14.1.0)
    - `CommunityToolkit.Mvvm` (8.4.2)
    - `Microsoft.Maui.Controls` (10.0.51)
    - `Microsoft.Extensions.Logging.Debug` (10.0.7)
    - `Plugin.Maui.Audio` (4.0.0)
    - `Serilog` (4.3.1)
    - `Serilog.Extensions.Hosting` (10.0.0)
    - `Serilog.Sinks.File` (7.0.0)

### 3.2 Compilation & Build Verification Matrix

| Target Framework | Command | Build Status | Warnings | Errors | Duration |
| :--- | :--- | :--- | :--- | :--- | :--- |
| `net10.0-windows10.0.19041.0` | `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-windows10.0.19041.0` | **SUCCEEDED** | 0 | 0 | 12.67s |
| `net10.0-android` | `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-android` | **SUCCEEDED** | 0 | 0 | 17.36s |

Both target frameworks build cleanly with zero warnings and zero errors.

---

## 4. Test Infrastructure & Verification Strategy

### 4.1 Current Test Posture
- Currently, **no unit test projects** exist in the repository.
- There are no automated test suites for ViewModels, Services, Converters, or Layout calculations.

### 4.2 Recommended Test Project Architecture (`RummyBooky.Tests`)
- Add a new project `tests/RummyBooky.Tests/RummyBooky.Tests.csproj` to the solution (`RummyBookyMaui.slnx`).
- Target Framework: `net10.0` (or `net10.0-windows10.0.19041.0`).
- Project configuration:
  ```xml
  <Project Sdk="Microsoft.NET.Sdk">
    <PropertyGroup>
      <TargetFramework>net10.0-windows10.0.19041.0</TargetFramework>
      <ImplicitUsings>enable</ImplicitUsings>
      <Nullable>enable</Nullable>
      <IsPackable>false</IsPackable>
      <IsTestProject>true</IsTestProject>
    </PropertyGroup>
    <ItemGroup>
      <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
      <PackageReference Include="xunit" Version="2.9.3" />
      <PackageReference Include="xunit.runner.visualstudio" Version="3.0.1">
        <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
        <PrivateAssets>all</PrivateAssets>
      </PackageReference>
      <PackageReference Include="Moq" Version="4.20.72" />
      <PackageReference Include="FluentAssertions" Version="7.0.0" />
    </ItemGroup>
    <ItemGroup>
      <ProjectReference Include="..\..\RummyBooky\RummyBooky.csproj" />
    </ItemGroup>
  </Project>
  ```

### 4.3 Test Suite Coverage Matrix for Requirements R1 - R4

#### Suite 1: Requirement R1 (Resume Game View Layout & Score Ordering)
- **File**: `CardBoxOrderingTests.cs`
- **Tests**:
  1. `GetOrderedPlayers_OrdersAscendingByCurrentScore`: Verify players are sorted $Score_{Lowest} \to Score_{Highest}$.
  2. `GetOrderedPlayers_PreservesTieBreaking`: Verify secondary sort on `PlayerName` or insertion order.
  3. `GetOrderedPlayers_Complexity`: Verify ordering executes in $O(n \log n)$.
  4. `CalculateCardPositions_Applies20PercentOffset`: Verify card $Y$ offsets are $0, 0.2H, 0.4H, \dots$ for up to 6 players.
  5. `CalculateResumeContainerPosition_Applies20PercentBottomOffset`: Verify resume box is placed at $Y_{LastCard} + 1.20 \times H$.

#### Suite 2: Requirement R2 (Expand Animation & Bounds Constraints)
- **File**: `CardBoxAnimationBoundsTests.cs`
- **Tests**:
  1. `ApplyExpandedState_TogglesContainersVisibility`: CollapsedContainer hidden, ExpandedContainer visible when expanded.
  2. `ExpandedPlayersList_WidthCalculation`: Verify `ExpandedPlayersList` width accommodates full player card width without clipping.
  3. `PlayerCardView_MeasureDetails`: Verify stats grid (8 rows) renders without vertical truncation.

#### Suite 3: Requirement R3 (Player Card Edit Navigation & Event Routing)
- **File**: `PlayerCardNavigationTests.cs`
- **Tests**:
  1. `EditPlayerCommand_InCardBoxView_PassesTargetPlayer`: Verify `EditPlayerCommand` executes with target `PlayerModel`.
  2. `EditPlayerCommand_InNewGamePage_RoutesToEditPlayerPage`: Verify `Shell.GoToAsync(nameof(EditPlayerPage))` receives dictionary with `CurrentPlayer`.
  3. `PlayerCardView_EditImageButtonClick_FiresBoundCommand`: Verify `OnEditPlayerButtonClicked` triggers bound command parameter.

#### Suite 4: Requirement R4 (Player Search Synchronization & Instant Enter Trigger)
- **File**: `PlayerSearchSynchronizationTests.cs`
- **Tests**:
  1. `Search_InstantEnter_PopulatesSuggestionsImmediately`: Pressing Enter / invoking `SearchPlayerSuggestionsCommand` executes search query synchronously without debounce delay.
  2. `Search_ChangingQuery_ImmediatelyClearsStaleSuggestions`: Typing "bob" after "eric" immediately clears "eric" matches.
  3. `Search_DispatchedQueries_CancelPreviousInFlightToken`: Fast consecutive typing cancels prior in-flight tokens and yields only the latest query matches.
  4. `Search_EmptyQuery_HidesSuggestions`: Whitespace/empty text clears suggestions and sets `ShowPlayerSuggestions = false`.
  5. `AddSuggestedPlayer_WithParameter_AddsExactSelectedPlayer`: Double-tapping suggestion adds target player to `GameModelTemplate.Players`.

---

## 5. Summary of Actionable Recommendations

| Area | Recommended Action | Affected Files | Priority |
| :--- | :--- | :--- | :--- |
| **R4 Search Enter Key** | Bind `EntryPlayerName.ReturnCommand` to `SearchPlayerSuggestionsCommand` instead of `AddPlayerCommand`. | `RummyBooky/Pages/NewGamePage.xaml:18` | **High (R4)** |
| **R4 Search Debounce** | Set `StoppedTypingTimeThreshold="250"` and implement `CancellationTokenSource` in `NewGameViewModel`. | `NewGamePage.xaml:20`, `NewGameViewModel.cs:75` | **High (R4)** |
| **R4 Stale Result Prevention** | Atomically clear and reset collection on `MainThread` checking cancellation token before mutation. | `NewGameViewModel.cs:81, 137` | **High (R4)** |
| **R4 Carousel Binding** | Add `CurrentItem="{Binding SelectedSuggestedPlayerModel, Mode=TwoWay}"` and `CommandParameter="{Binding .}"`. | `NewGamePage.xaml:31, 61` | **Medium (R4)** |
| **Test Infrastructure** | Create `RummyBooky.Tests` xUnit project and implement test suites for R1, R2, R3, and R4. | `tests/RummyBooky.Tests/*` | **High (Build/QA)** |

---
