# RummyBooky Master Project Requirements

## 1. Project Overview & Purpose
**RummyBooky** is a cross-platform .NET MAUI application designed to score, track, record, and manage Rummy card games. The application supports creating new games, tracking hands and rounds, computing player rankings and leaderboards, maintaining lifetime historical statistics, and resuming in-progress games.

## 2. Technology Stack
- **Framework**: .NET MAUI 10.0 (`Microsoft.Maui.Controls 10.0.90`)
- **Target Frameworks**: `net10.0-windows10.0.19041.0`, `net10.0-android`, `net10.0-ios`, `net10.0-maccatalyst`
- **Architecture & MVVM**: `CommunityToolkit.Mvvm` (v8.4.2), Source Generators (`[ObservableProperty]`, `[RelayCommand]`, `[QueryProperty]`)
- **UI & Toolkit**: `CommunityToolkit.Maui` (v15.0.0), Pure XAML styling, XAML Data Binding
- **Audio & Logging**: `Plugin.Maui.Audio` (v4.0.0), `Serilog` (v4.4.0) with File Sinks
- **Testing**: xUnit test suite (`tests/RummyBooky.Tests`)

## 3. Architecture & Code Paths
- **Views & Pages**:
  - `MainPage`: Entry dashboard displaying collapsed/expanded active game resume stacks (`CardBoxView`) and quick navigation.
  - `NewGamePage`: New game setup, score limit definition, debounced & manual player search, suggestion carousel, player roster management, dealer assignment, and seating order.
  - `CurrentGamePage`: Active game scoreboard, round progression, dealer indicator rotation, score calculation, winner/draw detection, and round history summary.
  - `EditPlayerPage`: Player profile management, player name editing/renaming across historical records, lifetime statistics, and played/active games lists.
  - `LeaderboardPage`: Global player rankings, rank badges, and lifetime statistics.
  - `GeneralPopupPage`: Modal popups for game outcome confirmations, dealer selection, warnings, and error alerts.
- **Services**:
  - `GameService`: Game persistence (`savedgames/*.json`), aggregate player profile calculations, dealer selection & clockwise rotation, player removal, and player renaming.
  - `AppAudioService`: Audio feedback for game actions.
  - `IPopupService`: Modal dialog and popup management.

## 4. Functional Specifications & Requirements

### R1. Player Profile Management (`EditPlayerPage`)
- Users must be able to view player profile details and historical stats.
- Users must be able to edit and update a player's name.
- Renaming a player must update their identity across all active and saved historical game files and reload the player aggregate dictionary.
- Users must be able to remove a player with confirmation and game integrity preservation.

### R2. New Game Creation & Player Management (`NewGamePage`)
- **Player Selection & Unselection**:
  - Users can search existing players or add new player names.
  - If a user accidentally selects an existing player, they must be able to unselect/remove that player from the new game roster.
  - If a user accidentally creates a new player instead of picking an existing match, they must be able to delete/undo the creation and choose the matching card from search suggestions.
- **Search Controls & Safeguards**:
  - Auto-search triggers on debounce when typing stops (250ms).
  - Manual search executes immediately upon pressing Enter/Return or tapping the magnifying glass search icon.
  - Search input is safeguarded against whitespace/empty queries: clearing the query immediately clears suggestions and restores the default new game view.

### R3. Dealer Selection & Seating Order Rotation
- When starting a game, users can choose between:
  1. Assigning the first dealer at random.
  2. Selecting the specific player who will deal first.
- In 2-player games: prompt/allow selection of the initial dealer (or random).
- In 3+ player games: seating order determines the clockwise rotation of the dealer chip to the player's left on subsequent rounds (`(currentDealerIndex + 1) % playerCount`).
- Player order in active game maintains consistent seating order rather than scrambling each round.

### R4. Game Play Scoreboard & Table Alignment (`CurrentGamePage`)
- Scoreboard grid columns (Player, Total Score, Round Score input) and vertical/horizontal borders must align cleanly between header and data rows across platforms.
- Player score inputs validate numeric entries and update scores upon round calculation.
- Dealer badge displays next to the active dealer for the current round and rotates clockwise on round completion.
- Highest and lowest played hands are tracked per round and displayed in the summary footer.
