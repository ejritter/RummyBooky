# Dispatch Task

## 2026-08-14T03:01:47Z

You are Worker 1 for Milestone 1 (Cascading Layout & Expand Animation - R1 & R2).
Working directory: c:\Dev\RummyBookyMaui\.agents\worker_m1

First, read the authoritative user request at: c:\Dev\RummyBookyMaui\.agents\ORIGINAL_REQUEST.md
Read the project specifications at: c:\Dev\RummyBookyMaui\.agents\PROJECT.md
Read the survey report at: c:\Dev\RummyBookyMaui\.agents\explorer_survey_r1r2\report.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Scope & Exclusive File Ownership:
- `c:\Dev\RummyBookyMaui\RummyBooky\Views\CardBoxView.xaml`
- `c:\Dev\RummyBookyMaui\RummyBooky\Views\CardBoxView.xaml.cs`
- `c:\Dev\RummyBookyMaui\RummyBooky\Views\PlayerCardView.xaml.cs`
- `c:\Dev\RummyBookyMaui\RummyBooky\Extensions\ViewExtensions.cs`

Task Implementation Instructions:
1. R1: Score Ordering
   - In `CardBoxView.xaml.cs` (`GetOrderedPlayers`), sort active players ascending by current game score (`PlayerScore`) with $O(n \log n)$ complexity: `.OrderBy(player => player.PlayerScore).ThenBy(player => player.PlayerName)`.
2. R1: Cascading Layout & Z-Order
   - In `CardBoxView.xaml.cs` (`RenderCollapsedCards`), render the lowest scoring player first at base layer ($Y = 0$).
   - Loop in ascending order ($i = 0$ to $N-1$).
   - Compute vertical offset: each subsequent card is vertically offset by progressive $+20\%$ relative to card height: `double top = i * (0.20 * cardHeight);`.
   - Add cards to `CollapsedCardsCanvas.Children` in ascending order so that lower scoring players have lower Z-indices (base layer) and higher scoring players are layered on top, keeping player name headers exposed for up to 6 players.
   - Position the resume action box container (`CardBoxImage` / collapsed container) $20\%$ down from the bottom of the final rendered player card ($Y_{\text{box}} = Y_{\text{bottom, final}} + 0.20 \times \text{cardHeight}$ or $N \times 0.20 \times \text{cardHeight}$ to expose headers and position the box).
   - Ensure `CollapsedCardsViewport` does not clip cards prematurely.
3. R1 Discovered Fix:
   - In `CardBoxView.xaml` (line 52), fix `GameStartedLabel` binding from `CurrentGame.StartedDate` to `CurrentGame.GameStart`.
4. R2: Expand/Collapse Animation & Bounds Constraints
   - In `PlayerCardView.xaml.cs` (`UpdatePlayerCardDimensions`), eliminate hardcoded rigid `CardBorder.WidthRequest` and `CardBorder.HeightRequest` when `IsInCardBox` is false or when placed in `ExpandedPlayersList`, so that stats grid (all columns and rows), timestamps, edit pencil buttons, and borders render completely without horizontal or vertical clipping.
   - In `CardBoxView.xaml` and `CardBoxView.xaml.cs`, ensure `ExpandedPlayersList` and `ExpandedContainer` fill the available width in Column 1 and allow cards to render cleanly.
   - Preserve smooth `TransitionCardBoxAsync` docking and expanding animation when toggling between collapsed and expanded states.

Verification Requirements:
- Build the project for both platforms:
  1. `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-windows10.0.19041.0`
  2. `dotnet build RummyBooky\RummyBooky.csproj -f net10.0-android`
- Confirm both builds succeed with 0 errors and 0 warnings.
- Write a full handoff report to `c:\Dev\RummyBookyMaui\.agents\worker_m1\handoff.md` and send a message when done.
