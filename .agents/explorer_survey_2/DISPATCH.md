## 2026-08-21T21:54:37Z
Investigate the full scoring, round advancement, dealer rotation, round history navigation/editing, and EditGamePage flow.
Examine:
1. CurrentGameViewModel.cs:
   - Entering scores per player
   - "Calculate Scores" command logic
   - Round advancement (Round 1 -> Round 2)
   - Running total calculations and highest/lowest played hands update
   - Dealer rotation clockwise logic
   - Previous round score viewing and editing (◀/▶ buttons), ensuring dynamic recomputation of running totals
2. EditGamePage.xaml & EditGameViewModel.cs:
   - Editing Game Status (Won, Draw, Forfeit, In-Progress)
   - Winning Player selection & tie resolution
   - Score Limit editing
   - Editing round scores across all rounds
3. Persistence:
   - Saving game state to disk (GameService, StorageService, JSON serialization)
   - Ensuring zero data corruption and synchronized player stats.
4. Identify any missing logic, bugs, or edge cases and recommend concrete implementation fixes for Worker.
