# Handoff Report: Survey Explorer R1 & R2

## 1. Observation
1. **File `RummyBooky/Views/CardBoxView.xaml.cs` lines 89–100**:
   ```csharp
   private IReadOnlyList<PlayerModel> GetOrderedPlayers()
   {
       if (Players is null)
       {
           return new List<PlayerModel>();
       }

       return Players
           .OrderByDescending(player => player.LifetimeScore)
           .ThenBy(player => player.PlayerName)
           .ToList();
   }
   ```
   Directly observed: Sort is descending by `LifetimeScore` instead of ascending by active current game `PlayerScore`.

2. **File `RummyBooky/Views/CardBoxView.xaml.cs` lines 178–194**:
   ```csharp
   for (int index = orderedPlayers.Count - 1; index >= 0; index--)
   {
       double top = topBase - ((index - 1) * stackStep);
       top = Math.Max(0d, top);

       var playerCardView = new PlayerCardView
       {
           AssignedPlayerModel = orderedPlayers[index],
           WidthRequest = cardWidth,
           HeightRequest = cardHeight,
           InputTransparent = true
       };

       AbsoluteLayout.SetLayoutBounds(playerCardView, new Rect(0d, top, cardWidth, cardHeight));
       AbsoluteLayout.SetLayoutFlags(playerCardView, AbsoluteLayoutFlags.None);
       CollapsedCardsCanvas.Children.Add(playerCardView);
   }
   ```
   Directly observed: Reversal of iteration order (`orderedPlayers.Count - 1` down to `0`), wrong offset calculation (`topBase - (index - 1) * stackStep` where `stackStep = viewportHeight * 0.08d` rather than $+20\%$ card height), and reversed Z-order insertion.

3. **File `RummyBooky/Views/PlayerCardView.xaml.cs` lines 183–210**:
   ```csharp
   private void UpdatePlayerCardDimensions()
   {
       var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);

       if (IsInCardBox)
       {
           // ...
       }

       CardBorder.WidthRequest = desiredWidth;
       CardBorder.HeightRequest = desiredHeight;
   }
   ```
   Directly observed: `CardBorder.WidthRequest` and `CardBorder.HeightRequest` are unconditionally overwritten with fixed values from `GetWidthAndHeight` (360–400dp width, 470–495dp height) when `IsInCardBox` is false.

4. **File `RummyBooky/Views/CardBoxView.xaml.cs` lines 104–106 and 132–134**:
   ```csharp
   var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);
   WidthRequest = desiredWidth;
   HeightRequest = desiredHeight;
   // ...
   double expandedPlayerWidth = Math.Max(220d, desiredWidth - 95d);
   ExpandedPlayersList.WidthRequest = expandedPlayerWidth;
   ExpandedPlayersList.HeightRequest = imageHeight;
   ```
   Directly observed: `CardBoxView.WidthRequest`/`HeightRequest` and `ExpandedPlayersList.WidthRequest`/`HeightRequest` are rigidly constrained, squeezing Column 1 into ~228dp while `PlayerCardView` forces a 360dp width.

5. **File `RummyBooky/Views/CardBoxView.xaml` line 52**:
   ```xml
   Text="{Binding CurrentGame.StartedDate, StringFormat='Started: {0:MMM dd, yyyy}', Source={x:Reference thisCardBoxView}}"
   ```
   Directly observed: `CurrentGameModel.cs` defines `public DateTime GameStart { get; init; } = DateTime.Now;` (line 9); `StartedDate` does not exist.

6. **File `RummyBooky/Extensions/ViewExtensions.cs` lines 38–84**:
   `TransitionCardBoxAsync` contains the cross-fade and scale animation logic for toggling collapsed and expanded states.

7. **Build Command Output**:
   `dotnet build RummyBooky/RummyBooky.csproj -f net10.0-windows10.0.19041.0` -> `Build succeeded. 0 Warning(s), 0 Error(s)`.

---

## 2. Logic Chain
1. From **Observation 1**, `GetOrderedPlayers()` sorts by `LifetimeScore` in descending order. Requirement R1 requires sorting ascending by `PlayerScore` ($Score_{Lowest} \to Score_{Highest}$) with $O(n \log n)$ complexity. Changing this to `Players.OrderBy(p => p.PlayerScore).ThenBy(p => p.PlayerName).ToList()` uses .NET's $O(n \log n)$ IntroSort and satisfies R1 sorting criteria.
2. From **Observation 2**, the backward loop (`index = Count - 1` down to `0`) appends player 0 last to `CollapsedCardsCanvas.Children`. In MAUI `AbsoluteLayout`, elements appended last are drawn on top. Therefore, the lowest-scoring player (index 0) is drawn at the highest Z-order rather than at the base layer ($Y=0, Z=0$). Furthermore, calculating `topBase - (index - 1) * stackStep` with an $8\%$ factor inverts the progressive $+20\%$ card height vertical offset rule. Reversing the loop to ascending order ($i = 0 \to N-1$) with $Y_i = i \times 0.20 \times H_{\text{card}}$ directly establishes base layer Y=0 for player 0, progressive $+20\%$ offsets for subsequent cards, exposed name headers for up to 6 players, and ascending Z-order.
3. From **Observation 3 & 4**, `PlayerCardView` sets `CardBorder.WidthRequest = desiredWidth` (360dp) and `CardBorder.HeightRequest = desiredHeight` (470dp). In `CardBoxView.xaml`, `ExpandedContainer` is a 2-column Grid with `EmptyCardBoxImage` (width 100dp) in Column 0 and `ExpandedPlayersList` in Column 1. When `CardBoxView.WidthRequest` is constrained to 360dp, Column 1 has only ~228dp of space. Forcing `CardBorder.WidthRequest = 360dp` causes the right ~132dp of each player card (stats column 2, pencil icon, right border) to overflow and clip.
4. Eliminating hardcoded `WidthRequest` and `HeightRequest` on `CardBorder` in full mode allows the card to dynamically fill Column 1 (`HorizontalOptions="Fill"`) and size its height intrinsically to its content, completely resolving card clipping and footer truncation during expansion (Requirement R2).
5. From **Observation 5**, binding to `StartedDate` instead of `GameStart` causes a silent XAML binding failure, leaving the start date label blank in the collapsed card box.

---

## 3. Caveats
- R3 (Player Card Edit Navigation & Event Routing) and R4 (Player Search Synchronization & Instant Enter Trigger) are surveyed and implemented in separate work streams, but `PlayerCardView.xaml` and `PlayerCardView.xaml.cs` changes for R2 must preserve the `Command` binding and event routing for the pencil edit button.
- When `CardBorder.WidthRequest` is removed for expanded mode in `PlayerCardView`, other views using `PlayerCardView` (`LeaderboardPage`, `EditPlayerPage`, `NewGamePage`) rely on their own parent containers (such as `MaximumWidthRequest="400"` or `Grid` columns) to constrain card width. Spot checks on those pages show they already provide outer container bounds.

---

## 4. Conclusion
The root causes for both R1 and R2 defects have been pinpointed to exact lines in `CardBoxView.xaml`, `CardBoxView.xaml.cs`, `PlayerCardView.xaml`, `PlayerCardView.xaml.cs`, and `BaseView.cs`.
- **R1 Solution**: Update `GetOrderedPlayers()` to `OrderBy(p => p.PlayerScore).ThenBy(p => p.PlayerName)`; re-implement `RenderCollapsedCards()` with ascending loop index $i = 0 \dots N-1$, exact $+20\%$ card height vertical step ($Y_i = i \times 0.20 \times H_{\text{card}}$), and positioning the card box container relative to the final card bottom edge; fix `GameStartedLabel` binding to `GameStart`.
- **R2 Solution**: Remove rigid `WidthRequest` and `HeightRequest` overrides on `CardBorder` in `PlayerCardView.xaml.cs` (when not in collapsed cardbox mode); remove restrictive width/height caps on `CardBoxView` and `ExpandedPlayersList` to let them fill available space smoothly during the expand/collapse animation.

All findings, formulas, line numbers, and refactoring steps are comprehensively detailed in `report.md`.

---

## 5. Verification Method
1. **Compilation Command**:
   ```powershell
   dotnet build c:\Dev\RummyBookyMaui\RummyBooky\RummyBooky.csproj -f net10.0-windows10.0.19041.0
   ```
   Must succeed with 0 errors and 0 warnings.
2. **Score Ordering & Layout Bounds Verification**:
   - Inspect `CardBoxView.xaml.cs` line 97 to confirm `OrderBy(p => p.PlayerScore)`.
   - Inspect `RenderCollapsedCards()` to confirm $i=0$ is at $Y=0$, step is $+0.20 \times H_{\text{card}}$, and insertion order is $i = 0 \to N-1$.
   - Inspect `PlayerCardView.xaml.cs` to confirm `CardBorder` does not force 360dp width when `IsInCardBox == false`.
3. **Runtime Visual Invalidation Conditions**:
   - If player with lowest score is not at the top-most visual header position (base layer $Y=0$), R1 is invalid.
   - If player cards in expanded state clip stats column 2 or right border, R2 is invalid.
