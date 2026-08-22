# Project: RummyBooky CurrentGamePage Rendering & Gameplay Flow Completion

## Architecture
- **Framework**: .NET MAUI 10.0 (`net10.0-windows10.0.19041.0`, `net10.0-android`).
- **Pattern**: MVVM with CommunityToolkit.Mvvm, Shell Navigation, Reactive UI, and XAML Data Binding.
- **Key Modules**:
  - `CurrentGamePage` & `CurrentGameViewModel`: Active game interface with player scores, round calculation, dealer badge rotation, and previous round score editing.
  - `EditGamePage` & `EditGameViewModel`: Dedicated game management screen for updating status, score limits, tie resolutions, and round score history.
  - `GameService` / `StorageService` / `PlayerService`: Persistence and business logic calculations.
  - `tests/RummyBooky.Tests`: xUnit test suite covering all 68 unit tests.
  - Physical Google Pixel Tablet (10.0.0.66:45305): Android target verification.

## Feature Inventory
| # | Feature | Description | Milestone | Source |
|---|---------|-------------|-----------|--------|
| 1 | R1: Active Game Player Row Rendering | Immediate rendering of all participating players in CurrentGamePage upon navigation with names, dealer badges, running totals, and round score input entries. | M1 | ORIGINAL_REQUEST §R1 |
| 2 | R2: Round Calculation & Dealer Rotation | Round score submission, running total calculations, advancing round number, rotating dealer badge clockwise, and saving state to disk. | M2 | ORIGINAL_REQUEST §R2 |
| 3 | R3: Previous Round Editing & Dynamic Recalculation | Viewing and editing scores from earlier rounds (◀/▶) with dynamic recomputation of running totals and highest/lowest hands during active game. | M3 | ORIGINAL_REQUEST §R3 |
| 4 | R3: Dedicated EditGamePage Management | EditGamePage support for editing Game Status, Score Limits, winner tie resolutions, and round history across all rounds. | M3 | ORIGINAL_REQUEST §R3 |
| 5 | R4: Automated Unit Tests (68 Tests) | Ensure all 68 unit tests in `tests/RummyBooky.Tests` pass with 0 errors via `dotnet test`. | M4 | ORIGINAL_REQUEST §R4 |
| 6 | R4: Physical Pixel Tablet Live E2E Verification | Deploy signed Release APK to user profile 0 on Google Pixel Tablet at 10.0.0.66:45305 and perform live E2E UI testing with screencap artifacts. | M5 | ORIGINAL_REQUEST §R4 |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Player Row Rendering Fix | `CurrentGamePage.xaml`, `CurrentGamePage.xaml.cs`, `CurrentGameViewModel.cs` | None | DONE |
| M2 | Scoring & Dealer Rotation Flow | `CurrentGameViewModel.cs`, `GameService.cs`, `RoundScore.cs` | M1 | DONE |
| M3 | Round Editing & EditGamePage | `CurrentGameViewModel.cs`, `EditGamePage.xaml`, `EditGameViewModel.cs` | M2 | DONE |
| M4 | Unit Tests & Signed Release Build | `tests/RummyBooky.Tests`, `RummyBooky.csproj`, APK signing & packaging | M1, M2, M3 | DONE |
| M5 | Physical Tablet E2E Verification | `10.0.0.66:45305`, adb / maui-devflow, live UI verification & screenshots | M4 | DONE |

## Code Layout
- `RummyBooky/Pages/CurrentGamePage.xaml` & `.cs` — Active game screen.
- `RummyBooky/ViewModels/CurrentGameViewModel.cs` — Active game ViewModel managing rounds, players, scores, dealer rotation, and history.
- `RummyBooky/Pages/EditGamePage.xaml` & `.cs` — Game editing screen.
- `RummyBooky/ViewModels/EditGameViewModel.cs` — Edit game ViewModel for tie resolution, status, and score corrections.
- `RummyBooky/Services/` — Business logic and data persistence.
- `tests/RummyBooky.Tests/` — Unit test project (68 tests).

