# Plan: CurrentGamePage Rendering & Gameplay Flow Completion

## Objective
Diagnose and resolve the CurrentGamePage active game player rendering issue in RummyBooky, complete the end-to-end scoring, round calculation, dealer rotation, and round editing flows, ensure all 68 unit tests pass with 0 errors, and perform live physical Pixel Tablet E2E deployment and UI verification at 10.0.0.66:45305.

## Survey Phase (Current)
- Spawn Explorer 1: CurrentGamePage active game player row rendering, ViewModel collections, bindings, data templates, dealer badge, score entry, lifecycle.
- Spawn Explorer 2: Round calculation, Calculate Scores command, dealer rotation, previous round editing (◀/▶), EditGamePage tie resolutions & game management, disk persistence.
- Spawn Explorer 3: 68 unit tests in tests/RummyBooky.Tests, build/packaging pipeline, physical Pixel tablet setup at 10.0.0.66:45305, maui-devflow tools.

## Milestones

### Milestone 1: CurrentGamePage Player Rendering & Binding Fix
- **Objective**: Fix player row rendering on CurrentGamePage so all participating players (e.g. Brodie & Renegade) render immediately upon navigation with dealer badges, running total scores, and round score input entries.
- **Verification**: Unit tests / ViewModel tests + UI inspection.

### Milestone 2: Scoring, Round Advancement & Dealer Rotation
- **Objective**: Complete end-to-end round calculation when user enters round scores and taps 'Calculate Scores', advancing to Round 2, updating totals, rotating dealer clockwise, and persisting state to disk.
- **Verification**: Unit tests for round calculation, dealer rotation, and persistence.

### Milestone 3: Previous Round Editing & Game Management (EditGamePage)
- **Objective**: Ensure navigating back to previous rounds (◀) allows editing previous round scores with dynamic total recalculation. Verify EditGamePage allows editing score limits, game status, and winner tie resolutions.
- **Verification**: Unit tests covering tie resolution, score edits, and game status changes.

### Milestone 4: Test Suite & Build Verification
- **Objective**: Ensure all 68 unit tests in `tests/RummyBooky.Tests` pass with 0 errors (`dotnet test`). Build signed Release APK for Android (`net10.0-android`).
- **Verification**: `dotnet test` 68/68 passed, `dotnet publish` Android APK ready.

### Milestone 5: Physical Pixel Tablet Deployment & Live E2E Verification
- **Objective**: Deploy signed Release APK to user profile 0 on Google Pixel Tablet at 10.0.0.66:45305, perform live UI flow walkthrough (create game with Brodie & Renegade, enter Round 1 scores, calculate, advance to Round 2, rotate dealer, edit Round 1, verify EditGamePage), capturing screenshot artifacts.
- **Verification**: Live E2E test execution with screencaps.

