# Plan: RummyBooky Round & Game Editing Feature

## Objectives
1. Allow editing previous rounds during active game with real-time score and metric recomputation (R1).
2. Create dedicated EditGamePage / EditGameViewModel for managing finished and in-progress games (R2).
3. Provide full unit test coverage and ensure build passes with 0 errors (R3).

## Execution Phases

### Phase 0: Discovery & Survey (3 Explorers in parallel)
- Explorer 1: Data models, GameSession/Game state, Scoring logic, Round recomputation, Metric calculation.
- Explorer 2: UI/UX & MVVM (CurrentGamePage, CurrentGameViewModel, MainPage cards, Navigation, Dialogs/Popups).
- Explorer 3: Storage persistence, Global stats/rankings sync, Existing test suite structure and test runner.

### Phase 1: Architecture & PROJECT.md
- Synthesize findings into `PROJECT.md` with Feature Inventory, Interface Contracts, and Milestone Decomposition.

### Phase 2: Milestone Execution (Workers, Reviewers, Challengers, Auditor)
- Milestone 1: Game logic & dynamic recomputation service methods for previous round modification.
- Milestone 2: CurrentGamePage active game round editing UI & real-time updates.
- Milestone 3: EditGamePage / EditGameViewModel & navigation wiring (MainPage + CurrentGamePage).
- Milestone 4: Storage persistence, player statistics & rankings synchronization.
- Milestone 5: Comprehensive unit test suite in `tests/RummyBooky.Tests`.

### Phase 3: Verification & Auditing
- Reviewers: Code review & build/test pass confirmation.
- Challengers: Edge case testing (ties, round count boundaries, score limit recalculation).
- Forensic Auditor: Integrity check.

### Phase 4: Final Reporting & Handoff
- Send final completion message to sentinel.
