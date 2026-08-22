# Project: RummyBooky — Previous Round & Game Editing with Real-Time Recomputation

## Architecture
- **Data Models (`RummyBooky/Models`)**:
  - `GameModel`: Abstract polymorphic base class for games with `Players`, `Round` collection, `IsGameActive`, `IsGameFinished`.
  - `CurrentGameModel`: Active game state with `ScoreLimit` and `GameStart`.
  - `PlayedGameModel`: Finished game state with mutable `WinningPlayer`, `GameState` (`GameStatus`), and `GameEnd`.
  - `RoundModel`: Round metrics (`LeadingPlayer`, `PlayerHighestScoringHand`, `CurrentHighestScoredHandValue`, `PlayerLowestScoringHand`, `CurrentLowestScoredHandValue`, `PlayersScoredHandThisRound`, and `RoundScores` collection of `RoundScoreModel`).
  - `RoundScoreModel`: Represents a single player's score entry for a round (`PlayerId`, `Score`).
- **Services (`RummyBooky/Services`)**:
  - `GameService`: Singleton managing game JSON file persistence in `FileSystem.AppDataDirectory/savedgames`, game calculation/recomputation engine, player lifetime statistics aggregation (`LoadAllPlayersDictionaryAsync`), and global ranking map (`BuildRankMap`).
- **UI & MVVM (`RummyBooky/Pages`, `RummyBooky/ViewModels`)**:
  - `CurrentGamePage` / `CurrentGameViewModel`: Active game view with round selector (`< Round K of N >`), enabling inspection and editing of previous rounds with immediate live recomputation of player running totals and extremes, plus "Edit Game" navigation button.
  - `EditGamePage` / `EditGameViewModel`: Dedicated game management screen supporting editing Game Status (`Won`, `Draw`, `Forfeit`, `In-Progress`), Winning Player selection (tie resolution), Score Limit, and all round scores across all players with dynamic recalculation and global stats synchronization.
  - `MainPage` / `MainPageViewModel`: Active game cards with "Edit Game" navigation triggers.
  - `AppShell` & `MauiProgram`: Shell route `EditGamePage` and DI registrations.
- **Test Suite (`tests/RummyBooky.Tests`)**:
  - Comprehensive unit tests covering in-game previous round score modifications, dynamic recomputation, tie resolution and win/loss count updates, score limit changes, and serialization integrity.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | Round Score Storage Model | Add explicit `RoundScores` collection to `RoundModel` and make `PlayedGameModel` status/winner properties mutable | M1 | survey |
| 2 | Pure Game Recomputation Engine | Add unified `RecalculateGameScores` / `RecalculateGame` method to `GameService` that recomputes player cumulative totals, round extremes, and leaders across all rounds | M1 | survey |
| 3 | In-Game Previous Round Editing (R1) | Add round selector `< Round K of N >` to `CurrentGamePage` and logic in `CurrentGameViewModel` to edit prior rounds, dynamically recalculate totals/metrics, and persist state to disk | M2 | ORIGINAL_REQUEST §R1 |
| 4 | Edit Game ViewModel & UI (R2) | Implement dedicated `EditGamePage.xaml` and `EditGameViewModel.cs` for editing Game Status, Winner / Tie Resolution, Score Limit, and multi-round score matrix | M3 | ORIGINAL_REQUEST §R2 |
| 5 | Navigation & Shell Route Wiring | Register `EditGamePage` in `AppShell.xaml.cs` and `MauiProgram.cs`, and add navigation entry points on `MainPage` game cards and `CurrentGamePage` action bar | M3 | ORIGINAL_REQUEST §R2 |
| 6 | Storage Persistence & Global Player Stats Sync | Saving edited games updates `game_{GameId}.json` on disk, re-aggregates player lifetime statistics via `LoadAllPlayersDictionaryAsync()`, and updates global rankings | M4 | ORIGINAL_REQUEST §R2 |
| 7 | Automated Test Suite (R3) | Implement comprehensive unit tests in `tests/RummyBooky.Tests` for in-game editing, dynamic recalculation, tie corrections, score limits, and stat updates | M5 | ORIGINAL_REQUEST §R3 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| 1 | Core Models & Recomputation Engine | Enhance `RoundModel`, `PlayedGameModel`, and implement `GameService.RecalculateGame` / `RecalculateGameScores` | none | DONE |
| 2 | In-Game Active Round Editing UI/UX | Enhance `CurrentGamePage.xaml` and `CurrentGameViewModel.cs` for previous round navigation, in-place editing, and real-time score updates | M1 | DONE |
| 3 | EditGamePage & EditGameViewModel | Implement `EditGamePage.xaml`, `EditGamePage.xaml.cs`, `EditGameViewModel.cs`, Shell routing, and navigation entry points | M1, M2 | DONE |
| 4 | Storage Persistence & Stats Synchronization | Ensure game save/conversion logic, polymorphic disk persistence, and global player stats/ranking refresh | M1, M3 | DONE |
| 5 | Automated Unit Test Suite & Verification | Implement comprehensive unit tests in `tests/RummyBooky.Tests` and verify full suite passes | M1, M2, M3, M4 | DONE |
| 6 | Review, Adversarial Challenge & Forensic Audit | Reviewers, Challengers, and Forensic Auditor verification across all requirements | M1, M2, M3, M4, M5 | DONE |

## Interface Contracts
### `GameService` ↔ `CurrentGameViewModel` / `EditGameViewModel`
- `public void RecalculateGame(GameModel game)`:
  - Resets each player's `PlayerScore = 0`, `HighestScoredHand = int.MinValue`, `LowestScoredHand = int.MaxValue`.
  - Iterates rounds $1 \dots N$.
  - Computes round high/low values, cumulative player totals, and assigns round `LeadingPlayer`.
  - Re-evaluates `ScoreLimit` threshold against player totals.
- `public Task SaveGameAsync(GameModel game)`:
  - Serializes polymorphic `CurrentGameModel` or `PlayedGameModel` to `savedgames/game_{GameId}.json`.
- `public Task<Dictionary<Guid, PlayerModel>> LoadAllPlayersDictionaryAsync()`:
  - Re-reads all game files and recomputes all lifetime player stats and global rankings.

### `EditGameViewModel` Query Navigation Contract
- Query Parameter: `[QueryProperty(nameof(Game), nameof(Game))]` or `[QueryProperty(nameof(GameId), nameof(GameId))]`
- Navigation: `Shell.Current.GoToAsync(nameof(EditGamePage), new Dictionary<string, object> { ["Game"] = game })`

## Code Layout
- `RummyBooky/Models/RoundModel.cs`: Holds `RoundScores` collection.
- `RummyBooky/Models/PlayedGameModel.cs`: Mutable `WinningPlayer` and `GameState`.
- `RummyBooky/Services/GameService.cs`: Central recomputation, persistence, and stats aggregation.
- `RummyBooky/Pages/CurrentGamePage.xaml`: Round selector and score editing UI.
- `RummyBooky/ViewModels/CurrentGameViewModel.cs`: Active round editing logic and recomputation calls.
- `RummyBooky/Pages/EditGamePage.xaml` & `.xaml.cs`: Dedicated game editor screen.
- `RummyBooky/ViewModels/EditGameViewModel.cs`: Dedicated game editor ViewModel.
- `RummyBooky/Pages/MainPage.xaml` & `ViewModels/MainPageViewModel.cs`: Game card edit actions.
- `RummyBooky/AppShell.xaml.cs` & `MauiProgram.cs`: Routing and DI registrations.
- `tests/RummyBooky.Tests/`: Unit test fixtures.
