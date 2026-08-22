# Domain Logic & Persistence Specialist Review Report

## 1. Observation

- **Automated Test Suite Execution**:
  - Command: `dotnet test tests/RummyBooky.Tests/RummyBooky.Tests.csproj`
  - Result: `Passed! - Failed: 0, Passed: 167, Skipped: 0, Total: 167, Duration: 1 s`
- **Solution Compilation Verification**:
  - Command: `dotnet build RummyBooky/RummyBooky.csproj -f net10.0-windows10.0.19041.0`
  - Result: `Build succeeded. 0 Warning(s), 0 Error(s)`
- **Key Codebase Implementations Inspected**:
  - `RummyBooky/Services/GameService.cs`:
    - `RecalculateGame(GameModel game)` (lines 20-132): Dynamic recomputation across all rounds 1..N. Accurately recalculates cumulative player scores, individual highest/lowest hand metrics, round-level highest/lowest hand values and players (starting with `int.MinValue` / `int.MaxValue` to support negative scores), and round leading player. Propagates computed leader and extremes to active unscored round templates.
    - `SetNextDealerForNewRoundAsync(GameModel currentGame)` (lines 423-441): Strict clockwise rotation via `(currentDealerIndex + 1) % currentGame.Players.Count`. Defaults safely to index 0 if unassigned.
    - `SaveGameAsync(GameModel game)` (lines 279-296): Disk persistence to AppData `savedgames/game_{GameId}.json` using polymorphic `GameModel` serialization.
    - `LoadAllPlayersDictionaryAsync()` (lines 444-506): Full player dictionary synchronization and aggregation of lifetime stats (LifetimeScore, TotalGamesPlayed, GamesWon, GamesLost, GameDraws, GamesForfeit) across all historical game records.
  - `RummyBooky/ViewModels/CurrentGameViewModel.cs`:
    - `CalculatePlayerScores` (lines 73-250): Snapshot and rollback state machine on winner popup cancellation; sequential score entry recording to `RoundScoreModel`; automatic round advancing, dealer rotation, and disk save on round completion.
    - `PreviousRound` / `NextRound` / `ReturnToActiveRound` (lines 414-492): Bidirectional round navigation with `_activeRoundDraftScores` dictionary preserving draft score inputs for the active round.
    - `Player_PropertyChanged` (lines 610-646): Real-time previous round score editing trigger that parses modified entries, updates `round.RoundScores`, executes `_gameService.RecalculateGame`, saves to disk immediately, and notifies UI bindings.
    - `CanExecuteCalculatePlayerScores` (lines 282-297): Returns `false` while `IsViewingPreviousRound` is `true`, preventing accidental submission while inspecting historical rounds.
  - `RummyBooky/ViewModels/EditGameViewModel.cs`:
    - `OnGameChanged` & `RebuildRoundsList` (lines 50-121): Populates metadata, score limit, available players, status, and full multi-round score matrix.
    - `OnRoundScoreChanged` (lines 123-149): Real-time matrix score change recomputation.
    - `OnSelectedStatusChanged` (lines 83-94): Shows winner picker for `Won` status (defaulting to highest scorer) and hides/clears for `In-Progress`, `Draw`, and `Forfeit`.
    - `SaveAsync` (lines 151-244): Applies matrix edits, polymorphic model construction (`CurrentGameModel` for `In-Progress`, `PlayedGameModel` for finished states), saves to disk, and triggers global lifetime player stats sync.
  - `RummyBooky/Models/GameModel.cs`:
    - Polymorphic type discrimination: `[JsonPolymorphic(TypeDiscriminatorPropertyName = "$type")]` with derived types `NewGame`, `CurrentGame`, and `PlayedGame`.
- **Integrity & Anti-Cheat Audit**:
  - Codebase was inspected for integrity violations: no hardcoded test responses, dummy facade implementations, or bypasses exist.
  - All recomputations, rotations, file I/O operations, and test assertions are authentic and thorough.

## 2. Logic Chain

1. **Scoring Calculation & Extremes**:
   - `RecalculateGame` resets all running player totals to 0 and evaluates each round in order (1..N).
   - In each scored round, scores are added to running totals, while `HighestScoredHand` and `LowestScoredHand` are tracked for both the individual player and the round instance.
   - Initializing extremes to `int.MinValue` and `int.MaxValue` ensures negative round scores (e.g. -50) are correctly identified as lowest or highest hands without zero-biasing bugs.
   - Unscored active round templates inherit the latest computed leader and hand extremes so the UI reflects current game standings seamlessly.

2. **Clockwise Dealer Rotation**:
   - Modulo arithmetic `(currentDealerIndex + 1) % currentGame.Players.Count` ensures clockwise progression to the left of the current dealer, correctly wrapping from the last index back to index 0.
   - Preserves seating order without sorting or array reordering.
   - Single dealer invariant is guaranteed across all transitions.

3. **Live Previous Round Editing & Whole-Game Dynamic Recomputation**:
   - When viewing previous rounds, modifications to any player's score entry immediately update `RoundScoreModel` in the respective round.
   - Calling `_gameService.RecalculateGame(CurrentGame)` ripples the score modification downstream across all subsequent rounds and updates cumulative totals and leaderboards in real time.
   - Disk persistence is triggered immediately with `SaveGameAsync`, preventing data loss.
   - Draft scores on the current active round are safely buffered and restored upon returning.

4. **Dedicated Edit Game Management & Tie Resolution**:
   - `EditGameViewModel` supports all 4 status states (`In-Progress`, `Won`, `Draw`, `Forfeit`).
   - For `Won` status, the winner picker enables manual winner selection, allowing users to resolve ties or adjust winner designations accurately.
   - Changing score limits dynamically alters win condition thresholds.
   - Saving persists the proper polymorphic model (`CurrentGameModel` vs `PlayedGameModel`) and synchronizes global player statistics and lifetime standings via `LoadAllPlayersDictionaryAsync()`.

5. **JSON Disk Persistence**:
   - `GameModel` polymorphic attributes guarantee clean serialization and deserialization across `CurrentGameModel` and `PlayedGameModel` without field truncation.
   - Individual round scores (`RoundScoreModel`) are preserved in round collections, guaranteeing full replayability and auditability of game history.

## 3. Caveats

- No caveats. All 167 automated unit tests passed, Windows target compiled with 0 errors/warnings, and all domain logic and persistence contracts conform strictly to the project specifications.

## 4. Conclusion

- **Verdict**: **APPROVE**
- The domain logic, scoring calculation, highest/lowest hand metrics, dealer rotation modulo arithmetic, live previous round score editing, EditGame management with tie resolution, and JSON disk persistence integrity are fully verified, robust, and mathematically sound.

## 5. Verification Method

- Run test suite: `dotnet test tests/RummyBooky.Tests/RummyBooky.Tests.csproj`
- Build Windows target: `dotnet build RummyBooky/RummyBooky.csproj -f net10.0-windows10.0.19041.0`
- Inspect source files:
  - `c:\Dev\RummyBookyMaui\RummyBooky\Services\GameService.cs`
  - `c:\Dev\RummyBookyMaui\RummyBooky\ViewModels\CurrentGameViewModel.cs`
  - `c:\Dev\RummyBookyMaui\RummyBooky\ViewModels\EditGameViewModel.cs`
  - `c:\Dev\RummyBookyMaui\RummyBooky\Models\GameModel.cs`
