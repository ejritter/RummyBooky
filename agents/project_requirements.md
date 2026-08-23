# RummyBooky — Project Requirements & Master Reference

## 1. Project Overview & Purpose
RummyBooky is a cross-platform .NET MAUI (targeting Android, Windows, iOS, Mac Catalyst) digital scorekeeper, game tracker, and player statistics management application for Rummy card games. It enables users to create games, track round-by-round player scores, recompute real-time totals and hand extremes, manage player identities and leaderboards, edit active and historical games, and enjoy custom background audio.

---

## 2. Technology Stack & Dependencies
- **Runtime & Target Frameworks**: .NET 10.0 (`net10.0-android`, `net10.0-windows10.0.19041.0`, `net10.0-ios`, `net10.0-maccatalyst`)
- **UI & Architecture**: .NET MAUI with MVVM Pattern, CommunityToolkit.Mvvm (Source Generators, Partial Properties, RelayCommands), CommunityToolkit.Maui (Popups, Animations, Converters)
- **Audio Engine**: `Plugin.Maui.Audio` (`AudioManager`) for looping background soundtracks and responsive muting/pausing
- **Serialization & Persistence**: `System.Text.Json` polymorphic serialization stored in `FileSystem.AppDataDirectory/savedgames`
- **Testing**: `xUnit`, `Moq`, and `FluentAssertions` in `tests/RummyBooky.Tests` (.NET 10.0)

---

## 3. Architecture & Key Code Paths

### A. Data Models (`RummyBooky/Models`)
- `GameModel`: Abstract polymorphic base class for games with `Players`, `Round` collection, `IsGameActive`, and `IsGameFinished`.
- `CurrentGameModel`: Active in-progress game state with `ScoreLimit` and `GameStart`.
- `PlayedGameModel`: Completed game state with `WinningPlayer`, `GameState` (`GameStatus`), and `GameEnd`.
- `RoundModel`: Per-round metrics (`LeadingPlayer`, `PlayerHighestScoringHand`, `CurrentHighestScoredHandValue`, `PlayerLowestScoringHand`, `CurrentLowestScoredHandValue`, `RoundScores`).
- `RoundScoreModel`: Player score entry for a round (`PlayerId`, `Score`).
- `PlayerModel`: Player identity, rank symbol, card suit asset, and aggregate statistics (`TotalGamesPlayed`, `GamesWon`, `GamesLost`, `GamesForfeit`, `GameDraws`, `HighestScoredHand`, `LowestScoredHand`, `LifetimeScore`).
- `PopupResultsModel`: Result structure for popup dialog confirmations.

### B. Core Services (`RummyBooky/Services`)
- `GameService`: Central recomputation engine, JSON disk persistence, player lifetime statistics aggregation (`LoadAllPlayersDictionaryAsync`), and global ranking map (`BuildRankMap`).
- `PopupService`: Wrapper for displaying custom modals via `CommunityToolkit.Maui.Views.Popup`.
- `AppAudioService`: Singleton managing `the_gambler.mp3` background audio playback, looping, app lifecycle pause/resume, and volume muting.

### C. Pages & ViewModels (`RummyBooky/Pages`, `RummyBooky/ViewModels`)
- `MainPage` / `MainPageViewModel`: Home dashboard displaying active games list, New Game / Leaderboard navigation buttons, and double-tap Gambler soundtrack pause/mute toggle on the logo.
- `NewGamePage` / `NewGameViewModel`: Player selection and score limit setup for starting new games.
- `CurrentGamePage` / `CurrentGameViewModel`: Active game scoreboard with round navigation (`< Round K of N >`), round score editing, real-time total recalculation, and game completion handling.
- `EditGamePage` / `EditGameViewModel`: Dedicated game editor for updating Game Status, Winner, Score Limit, and round scores with diff confirmation dialogs.
- `LeaderboardPage` / `LeaderboardViewModel`: Global standings sorted by wins and performance, with edit player navigation.
- `EditPlayerPage` / `EditPlayerViewModel`: Player name modification with before/after diff confirmation prompts and single "Okay" success modals.
- `GeneralPopupPage` / `GeneralPopupViewModel`: Reusable modal dialog with transparent window styling and clean card presentation.

---

## 4. Functional & Technical Specifications

### A. Audio Management
- Continuous looping background playback of `the_gambler.mp3` upon app startup.
- Double-tap gesture on the MainPage book logo toggles audio playback (pause/resume or mute/unmute).
- Automatic pause during application backgrounding (`OnSleep`) and resume on foregrounding (`OnResume`).

### B. Modal Dialogs & Visual Styling
- `BasePopupPage<TViewModel>` inherits from `CommunityToolkit.Maui.Views.Popup` with `Color="Transparent"`.
- Clean card rendering with zero outer ghost borders or see-through rectangular margins.
- `CanBeDismissedByTappingOutsideOfPopup = true` allows tapping outside to dismiss where appropriate.

### C. Confirmation & Success Flows
- **Edit Player**: Prompts with confirmation diff: `Player name will change from "{oldName}" to "{newName}". Are you sure you want to continue?`. On success, displays single "Okay" button.
- **Edit Game**: Computes itemized diff of changed properties/scores before saving. On success, displays single "Okay" button.

### D. Navigation Integrity
- Direct single-tap back navigation (`<`) between child pages (`EditPlayerPage`, `EditGamePage`) and parent views (`LeaderboardPage`, `MainPage`) with debouncing guards (`_isNavigating`) preventing duplicate navigation pushes.

---

## 5. Automated Verification Standard
- `dotnet test` test suite must pass with 0 failures before any commit.
- Target platform builds (`net10.0-android`, `net10.0-windows10.0.19041.0`) must compile with 0 errors.
