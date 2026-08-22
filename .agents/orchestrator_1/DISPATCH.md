## 2026-08-21T19:26:46Z
You are the Project Orchestrator for RummyBooky.

Working Directory: c:\Dev\RummyBookyMaui\.agents\orchestrator_1
Project Directory: c:\Dev\RummyBookyMaui
Original Request File: c:\Dev\RummyBookyMaui\.agents\ORIGINAL_REQUEST.md

User Request:
"Use sub agents to review and research how to implement this. Then use sub agents to code and sub agents to test

Implement the ability to edit previous rounds during an active game with real-time score and metric recomputation, and provide a dedicated Edit Game page to correct scores, score limits, game statuses, and winners (including tie resolutions).

Working directory: c:\Dev\RummyBookyMaui
Integrity mode: development

## Requirements

### R1. Edit Previous Round During Active Game
In the active game view (`CurrentGamePage`), provide the ability to view and edit scores from earlier rounds (e.g. Round N-1 or any completed round). Modifying a previous round's scores must automatically recompute all players' running total scores, highest/lowest scored hands, round metrics, and leading players, and persist the updated game state to disk.

### R2. Dedicated Edit Game Management Screen
Implement a dedicated `EditGamePage` and `EditGameViewModel` accessible from the Main Page game cards and Current Game navigation. The screen must allow editing:
- Game Status (Won, Draw, Forfeit, In-Progress)
- Winning Player selection (enabling tie corrections or manual winner assignment)
- Score Limit
- Individual round scores per player across all rounds in the game

Saving edits must recompute overall player statistics, update game files on disk, and synchronize global player ranking and lifetime statistics.

### R3. Automated Test Verification
Provide comprehensive automated unit tests covering:
- In-game previous round score modifications and dynamic score recomputation
- Winner tie correction and win/loss count updates upon saving game edits
- Score limit modifications and total score calculations across multiple rounds

## Acceptance Criteria

### In-Game Round Editing
- [ ] Users can edit previous round scores during an active game (when round count > 1).
- [ ] Modifying a round score immediately updates running player totals and highest/lowest scored hands.
- [ ] Updated active game state is saved to disk with zero data corruption.

### Game Management & Tie Resolution
- [ ] Dedicated `EditGamePage` opens with game metadata, status, winner picker, and round score list.
- [ ] Users can correct the winner in a tie or change game status and save.
- [ ] Saving an edited game updates disk storage and refreshes global player statistics and rankings.

### Test Suite & Build Verification
- [ ] All unit tests in `tests/RummyBooky.Tests` pass with 0 failures (`dotnet test`).
- [ ] Solution compiles cleanly for Windows target (`net10.0-windows10.0.19041.0`) with 0 errors."
