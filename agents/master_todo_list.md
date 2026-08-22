# Master To-Do List - RummyBooky

## Active Tasks & Issue Tracking

### [High] Task 1: Fix Edit Player Screen Name Editing (R1)
- **Status**: Completed
- **Files**: `RummyBooky/Pages/EditPlayerPage.xaml`, `RummyBooky/Pages/EditPlayerPage.xaml.cs`, `RummyBooky/ViewModels/EditPlayerViewModel.cs`, `RummyBooky/Services/GameService.cs`
- **Details**:
  - Added `UpdatePlayerNameCommand` and `CanExecuteUpdatePlayerNameCommand` to `EditPlayerViewModel`.
  - Added `UpdatePlayerNameButton` and ReturnCommand in `EditPlayerPage.xaml` with press animations in `.xaml.cs`.
  - Implemented `GameService.UpdatePlayerNameHistory` to mutate in-memory player names, update `_allPlayers` dictionary cache, propagate across all saved active and played games on disk, and handle zero-game players.
  - Verified via `PlayerRenamingTests.cs` (3 unit tests passing).

### [High] Task 2: Fix Player Selection / Unselection & Accidentally Created Player Undo on New Game Screen (R2)
- **Status**: Completed
- **Files**: `RummyBooky/Pages/NewGamePage.xaml`, `RummyBooky/Pages/NewGamePage.xaml.cs`, `RummyBooky/ViewModels/NewGameViewModel.cs`
- **Details**:
  - Added instant unselect/remove button ("✕") directly on roster table rows in `NewGamePage.xaml` and Flyout/SwipeView items.
  - Tracked `LastAddedPlayer` and `LastSearchQuery` in `NewGameViewModel`. If a user accidentally creates a new player and removes them, `RemovePlayer` automatically restores the search text and suggestions carousel so the existing player card can be chosen.
  - Verified via `NewGameRosterAndSearchTests.cs` (4 unit tests passing).

### [High] Task 3: Add Manual Search Button & Safeguard Search on New Game Screen (R2)
- **Status**: Completed
- **Files**: `RummyBooky/Pages/NewGamePage.xaml`, `RummyBooky/Pages/NewGamePage.xaml.cs`, `RummyBooky/ViewModels/NewGameViewModel.cs`
- **Details**:
  - Added magnifying glass Search button beside `EntryPlayerName` bound to `SearchPlayerSuggestionsCommand`.
  - Configured `ReturnType="Search"` and `ReturnCommand="{Binding SearchPlayerSuggestionsCommand}"` on `EntryPlayerName`.
  - Safeguarded search logic: empty/whitespace queries immediately clear suggestions, cancel debounce timers, and restore default roster view.
  - Verified via `NewGameRosterAndSearchTests.cs`.

### [High] Task 4: Fix Table Grid Alignment & Borders on Current Game Page (R4)
- **Status**: Completed
- **Files**: `RummyBooky/Pages/CurrentGamePage.xaml`
- **Details**:
  - Aligned header grid and row item grid column definitions (`*,2,95,2,115`) and row definitions (`65,1`).
  - Added matching vertical divider BoxViews in columns 1 and 3 in header and data rows.
  - Styled `PlayerScoreEntry` in a neat `TagEntryBorder` for centered, crisp alignment.
  - Verified via `ScoreboardAlignmentTests.cs` (2 unit tests passing).

### [High] Task 5: Implement Dealer Selection Choice & Seating Order Left-Rotation (R3)
- **Status**: Completed
- **Files**: `RummyBooky/ViewModels/NewGameViewModel.cs`, `RummyBooky/ViewModels/CurrentGameViewModel.cs`, `RummyBooky/Services/GameService.cs`, `RummyBooky/Extensions/GameModelExtensions.cs`
- **Details**:
  - Removed `OrderBy(p => p.PlayerName)` in `GameModelExtensions.ConvertToCurrentGame` to lock in table seating order from NewGamePage.
  - Removed `ReorderPlayersForDisplay()` from `CurrentGameViewModel` to prevent destroying seating order on round advance or score calculation.
  - In `NewGameViewModel.StartGame`, if no dealer was manually selected, prompted user to assign random dealer or choose starting dealer (with 2-player vs 3+ player seating rotation).
  - Advanced dealer clockwise to the player's left (`(currentDealerIndex + 1) % count`) on each round progression.
  - Verified via `DealerRotationAndSeatingOrderTests.cs` (4 unit tests passing).

### [High] Task 7: Fix Leaderboard Standings Refresh & New Game UI/SVG Polish
- **Status**: Completed
- **Files**: `RummyBooky/Services/GameService.cs`, `RummyBooky/ViewModels/LeaderboardViewModel.cs`, `RummyBooky/Pages/LeaderboardPage.xaml`, `RummyBooky/Views/PlayerCardView.xaml.cs`, `RummyBooky/Pages/NewGamePage.xaml`, `RummyBooky/Resources/Images/player_new.svg`, `RummyBooky/Resources/Images/player_existing.svg`, `tests/RummyBooky.Tests/LeaderboardTests.cs`
- **Details**:
  - Fixed `LeaderboardViewModel` to bind `ObservableCollection<PlayerModel>` directly to avoid empty bindings in `PlayerCardView`.
  - Added `await LoadAllPlayersDictionaryAsync()` in `GameService.GetTopPlayersAsync` to reload fresh stats from disk whenever Standings are refreshed.
  - Synced `BindingContext` in `PlayerCardView.xaml.cs` to prevent stale/null player data.
  - Aligned Score Limit box, Player Name box, and Add Player button with matching 180px widths and left-alignment on New Game page.
  - Created and embedded crisp SVG icons (`player_new.svg` and `player_existing.svg`) for New vs Existing player indication.
  - Optimized New Game roster table column definitions (`*,2,65,2,65,2,65,2,65,2,65`) giving player names ample room (~360px) so names are fully visible.
  - Added unit test suite `LeaderboardTests.cs` (68 total tests passing).

### [High] Task 9: In-Game Previous Round Editing & Dedicated Edit Game Screen (R1, R2, R3)
- **Status**: Completed
- **Files**: `RummyBooky/Pages/CurrentGamePage.xaml`, `RummyBooky/ViewModels/CurrentGameViewModel.cs`, `RummyBooky/Pages/EditGamePage.xaml`, `RummyBooky/Pages/EditGamePage.xaml.cs`, `RummyBooky/ViewModels/EditGameViewModel.cs`, `RummyBooky/Services/GameService.cs`, `tests/RummyBooky.Tests/PreviousRoundAndGameEditingTests.cs`
- **Details**:
  - Added in-game round navigation (`◀`, `▶`, `Return to Current Round`) and round score editing in `CurrentGamePage.xaml` / `CurrentGameViewModel.cs`. Modifying earlier round scores recomputes all players' running total scores, highest/lowest scored hands, leading players, and updates disk persistence.
  - Implemented full dedicated `EditGamePage` and `EditGameViewModel` accessible from game cards and Current Game navigation. Supports editing Score Limit, Game Status (Won, Draw, Forfeit, In-Progress), Winner selection (for resolving ties), and individual round scores.
  - Added 7 unit tests in `PreviousRoundAndGameEditingTests.cs` verifying dynamic recalculation and game updates.

### [High] Task 10: Theme-Aware Vector Assets & Android Hardware Verification
- **Status**: Completed
- **Files**: `RummyBooky/Resources/Images/player_new_light.svg`, `RummyBooky/Resources/Images/player_new_dark.svg`, `RummyBooky/Resources/Images/player_existing_light.svg`, `RummyBooky/Resources/Images/player_existing_dark.svg`, `RummyBooky/Pages/NewGamePage.xaml`, `RummyBooky/Models/PlayerModel.cs`
- **Details**:
  - Created dedicated Light and Dark theme SVG vector assets for New and Existing player badges.
  - Configured `{AppThemeBinding}` in XAML `Image.Triggers` for dynamic theme adaptation.
  - Connected via ADB to physical Android device `10.0.0.66:45305`, built and deployed `net10.0-android` package, forwarded DevFlow ports, and verified live runtime behavior, layout alignments, theme badges, and gameplay navigation.

### [High] Task 12: Leaderboard Layout Optimization & Live Production Data Recovery
- **Status**: Completed
- **Files**: `RummyBooky/Pages/LeaderboardPage.xaml`, `RummyBooky/ViewModels/LeaderboardViewModel.cs`, `RummyBooky/Services/GameService.cs`
- **Details**:
  - Replaced nested `CollectionView` with `BindableLayout` inside `ScrollView` to eliminate Android measurement/collapse bugs and enable smooth vertical scrolling.
  - Corrected polymorphic serialization order (`$type` first) to ensure clean loading of historical games into `_allPlayers` dictionary.
  - Verified live on Google Pixel Tablet: Leaderboard displays ranked standings (Eric Ritter [Ace - 225 High Hand], Bruce Marchegiani [King], Shawn Tambeau [Queen]).
  - Autocomplete search on New Game screen immediately retrieves saved player profiles.

### [Medium] Task 13: Full Solution Testing & Validation
- **Status**: Completed
- **Files**: `tests/RummyBooky.Tests/*`
- **Details**:
  - All 167 unit tests across the test suite pass cleanly (`0 failed, 167 passed`).
  - Both Windows (`net10.0-windows10.0.19041.0`) and Android (`net10.0-android`) build with 0 errors and 0 warnings.
  - Verified live on physical Android device (`10.0.0.66:45305`) and Windows Desktop.

