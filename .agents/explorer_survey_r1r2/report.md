# Investigation Report: Requirements R1 & R2
**Project**: RummyBooky .NET MAUI Application  
**Working Directory**: `c:\Dev\RummyBookyMaui`  
**Investigator**: Teamwork Explorer (Survey Explorer R1 & R2)  
**Date**: 2026-08-14  
**Authoritative Reference**: `.agents/ORIGINAL_REQUEST.md`  

---

## 1. Executive Summary

This investigation delivers an exhaustive architectural and mathematical survey of **Requirement R1 (Resume Game View Cascading Layout & Score Ordering)** and **Requirement R2 (Resume Game View Expand Animation & Bounds Constraints)** in the RummyBooky .NET MAUI application.

### Key Investigation Takeaways:
1. **R1 Score Ordering Bug**: In `CardBoxView.xaml.cs` (lines 89–100), player sorting currently invokes `.OrderByDescending(player => player.LifetimeScore)`. R1 explicitly requires ascending sort by active current game score (`PlayerScore`) with $O(n \log n)$ time complexity ($Score_{Lowest} \to Score_{Highest}$).
2. **R1 Cascading Canvas & Z-Order Inversion**: In `CardBoxView.xaml.cs` (lines 178–194), the collapsed rendering loop runs backwards (`for (int index = orderedPlayers.Count - 1; index >= 0; index--)`), adding elements in reverse order and causing the lowest-scoring player to render at the top Z-order instead of the base layer. The Y-offset formula uses an arbitrary $8\%$ step (`viewportHeight * 0.08d`) with an inverted subtraction (`topBase - (index - 1) * stackStep`), violating the $+20\%$ card height progressive offset.
3. **R1 CardBox Container Positioning**: The collapsed `CardBoxImage` and container currently occupy fixed coordinates $(0, 0, \text{imageWidth}, \text{imageHeight})$ overlapping the cards via an artificial clipped viewport `CollapsedCardsViewport` (`IsClippedToBounds="True"`), rather than being positioned dynamically $20\%$ down from the bottom of the final rendered player card.
4. **R2 Clipping & Sizing Root Cause**: In `PlayerCardView.xaml.cs` (lines 183–210), `UpdatePlayerCardDimensions()` unconditionally forces `CardBorder.WidthRequest = desiredWidth` (360–400dp from `BaseView.GetWidthAndHeight`) and `CardBorder.HeightRequest = desiredHeight` whenever `IsInCardBox` is false. When hosted in `CardBoxView.xaml`'s `ExpandedContainer` (Column 1, next to the 100dp docked `EmptyCardBoxImage`), the available width is only ~236dp. The hardcoded 360dp width causes the entire right side of the card—including stats values (Column 2), the pencil edit button, and the border—to be clipped and truncated. Furthermore, hardcoding `CardBoxView.WidthRequest`, `CardBoxView.HeightRequest`, and `ExpandedPlayersList.WidthRequest` prevents responsive layout expansion.
5. **Additional Discovered Binding Defect**: In `CardBoxView.xaml` (line 52), `GameStartedLabel` binds to `CurrentGame.StartedDate`, whereas the property on `CurrentGameModel.cs` is `GameStart`.

---

## 2. Requirement R1: Resume Game View Cascading Layout & Score Ordering

### 2.1 Score Ordering Analysis & Algorithmic Complexity

#### Current Implementation (`RummyBooky/Views/CardBoxView.xaml.cs:89-100`):
```csharp
89: 	private IReadOnlyList<PlayerModel> GetOrderedPlayers()
90: 	{
91: 		if (Players is null)
92: 		{
93: 			return new List<PlayerModel>();
94: 		}
95: 
96: 		return Players
97: 			.OrderByDescending(player => player.LifetimeScore)
98: 			.ThenBy(player => player.PlayerName)
99: 			.ToList();
100: 	}
```

#### Defect Analysis:
- **Target Property**: Currently orders by `LifetimeScore` (historical cumulative score). Requirement R1 mandates ordering by active current game score: `PlayerModel.PlayerScore`.
- **Direction**: Currently uses `OrderByDescending` (highest first). R1 mandates ascending order ($Score_{Lowest} \to Score_{Highest}$) so the lowest scoring player is rendered first at the base layer.
- **Complexity**: LINQ's `OrderBy(...).ThenBy(...)` in .NET utilizes IntroSort (Dual-Pivot Quicksort falling back to Heapsort when recursion depth exceeds $2 \log n$), guaranteeing $O(n \log n)$ time complexity and $O(n)$ auxiliary space.

#### Required Refactoring:
```csharp
private IReadOnlyList<PlayerModel> GetOrderedPlayers()
{
    if (Players is null)
    {
        return Array.Empty<PlayerModel>();
    }

    return Players
        .OrderBy(player => player.PlayerScore)
        .ThenBy(player => player.PlayerName)
        .ToList();
}
```

---

### 2.2 Mathematical Model for Cascading Stack & Z-Order

In .NET MAUI, `AbsoluteLayout.Children` are rendered in insertion order:
- **Index 0 (First Added)**: Lowest Z-order (rendered at the bottom/base layer).
- **Index $N-1$ (Last Added)**: Highest Z-order (rendered on top of preceding elements).

Let:
- $N$: Number of active players in the game ($1 \le N \le 6$).
- $P = [p_0, p_1, \dots, p_{N-1}]$: Sorted players collection where $p_0.\text{PlayerScore} \le p_1.\text{PlayerScore} \le \dots \le p_{N-1}.\text{PlayerScore}$.
- $W_{\text{card}}$: Player card width in device-independent units.
- $H_{\text{card}}$: Player card height in device-independent units.
- $k_{\text{step}} = 0.20$ ($+20\%$ vertical offset relative to card height).
- $\Delta Y = k_{\text{step}} \times H_{\text{card}} = 0.20 \times H_{\text{card}}$.

#### Per-Card Coordinate & Boundary Equations:
For each player card $i \in \{0, 1, \dots, N-1\}$:
$$Y_i = i \times \Delta Y = i \times 0.20 \times H_{\text{card}}$$
$$X_i = 0 \quad \text{(or centered in canvas)}$$
$$\text{Bounds}_i = \text{Rect}(X_i, Y_i, W_{\text{card}}, H_{\text{card}})$$
$$\text{Z-Index}_i = i \quad (Z_0 < Z_1 < \dots < Z_{N-1})$$

#### Exposure of Player Name Headers:
`PlayerCardView.xaml` places `HeaderGrid` (containing the rank symbol, suit image, and player name chip) in the top $20\%$ of the card height ($[0, 0.20 \times H_{\text{card}}]$).
- For player card $i$ ($0 \le i < N-1$):
  - Card top edge: $Y_i = i \times 0.20 \times H_{\text{card}}$
  - Overlapped by card $i+1$ starting at: $Y_{i+1} = (i+1) \times 0.20 \times H_{\text{card}}$
  - Visible exposed header band: $[Y_i, Y_{i+1}]$
  - Exposed header height:
    $$H_{\text{exposed}, i} = Y_{i+1} - Y_i = 0.20 \times H_{\text{card}}$$
- For the topmost player card ($i = N-1$):
  - Card top edge: $Y_{N-1} = (N-1) \times 0.20 \times H_{\text{card}}$
  - Card bottom edge: $Y_{\text{bottom}, N-1} = Y_{N-1} + H_{\text{card}} = ((N-1) \times 0.20 + 1.0) \times H_{\text{card}}$

#### Example Coordinate Mapping ($H_{\text{card}} = 100\text{dp}$, $\Delta Y = 20\text{dp}$):

| Player Index $i$ | Score Rank | Insertion Order (Z-Order) | $Y_i$ Top (dp) | Bottom Edge (dp) | Visible Header Range |
| :---: | :---: | :---: | :---: | :---: | :---: |
| $i = 0$ | Lowest (1st) | 1 (Base Layer) | $0.0$ | $100.0$ | $[0.0, 20.0]$ |
| $i = 1$ | 2nd Lowest | 2 | $20.0$ | $120.0$ | $[20.0, 40.0]$ |
| $i = 2$ | 3rd Lowest | 3 | $40.0$ | $140.0$ | $[40.0, 60.0]$ |
| $i = 3$ | 4th Lowest | 4 | $60.0$ | $160.0$ | $[60.0, 80.0]$ |
| $i = 4$ | 5th Lowest | 5 | $80.0$ | $180.0$ | $[80.0, 100.0]$ |
| $i = 5$ | Highest (6th) | 6 (Topmost) | $100.0$ | $200.0$ | $[100.0, 200.0]$ (or to Box) |

---

### 2.3 Resume Action Box Container Positioning

R1 specifies:
> "Resume action box container (`CardBoxImage` / collapsed container) positioned 20% down from the bottom of the final rendered player card."

#### Mathematical Formulation:
- Final rendered player card bottom edge:
  $$Y_{\text{bottom, final}} = Y_{N-1} + H_{\text{card}} = (N-1) \times 0.20 \times H_{\text{card}} + H_{\text{card}}$$
- Action box position:
  - **Positioned 20% down from the bottom edge**:
    $$Y_{\text{box}} = Y_{\text{bottom, final}} + 0.20 \times H_{\text{card}} = [(N-1) \times 0.20 + 1.20] \times H_{\text{card}}$$
  - **Docked overlapping position** (where the card box holds the base of the card stack, starting 20% down from the top of the final card, exposing the final card header):
    $$Y_{\text{box}} = Y_{N-1} + 0.20 \times H_{\text{card}} = N \times 0.20 \times H_{\text{card}}$$
- Total Required Canvas Height:
  $$H_{\text{canvas}} = Y_{\text{box}} + H_{\text{box}}$$

---

### 2.4 Codebase Defect Survey in `CardBoxView.xaml` and `CardBoxView.xaml.cs`

#### Current `RenderCollapsedCards` (`CardBoxView.xaml.cs:155-195`):
```csharp
155: 	private void RenderCollapsedCards()
156: 	{
157: 		CollapsedCardsCanvas.Children.Clear();
158: 		var orderedPlayers = GetOrderedPlayers();
159: 		if (orderedPlayers.Count == 0)
160: 		{
161: 			return;
162: 		}
163: 
164: 		double viewportWidth = CollapsedCardsViewport.WidthRequest > 0d
165: 			? CollapsedCardsViewport.WidthRequest
166: 			: Math.Max(0d, WidthRequest * 0.82d);
167: 
168: 		double viewportHeight = CollapsedCardsViewport.HeightRequest > 0d
169: 			? CollapsedCardsViewport.HeightRequest
170: 			: Math.Max(0d, HeightRequest * 0.62d);
171: 
172: 		double cardWidth = Math.Max(0d, viewportWidth - 8d);
173: 		double cardHeight = Math.Max(95d, viewportHeight * 0.90d);
174: 		double leadOffset = Math.Max(8d, viewportHeight * 0.06d);
175: 		double stackStep = Math.Max(10d, viewportHeight * 0.08d);
176: 		double topBase = Math.Max(0d, viewportHeight - cardHeight - leadOffset);
177: 
178: 		for (int index = orderedPlayers.Count - 1; index >= 0; index--)
179: 		{
180: 			double top = topBase - ((index - 1) * stackStep);
181: 			top = Math.Max(0d, top);
182: 
183: 			var playerCardView = new PlayerCardView
184: 			{
185: 				AssignedPlayerModel = orderedPlayers[index],
186: 				WidthRequest = cardWidth,
187: 				HeightRequest = cardHeight,
188: 				InputTransparent = true
189: 			};
190: 
191: 			AbsoluteLayout.SetLayoutBounds(playerCardView, new Rect(0d, top, cardWidth, cardHeight));
192: 			AbsoluteLayout.SetLayoutFlags(playerCardView, AbsoluteLayoutFlags.None);
193: 			CollapsedCardsCanvas.Children.Add(playerCardView);
194: 		}
195: 	}
```

#### Exact Root Causes:
1. **Reversed Loop Order**: Line 178 loops backwards (`index = orderedPlayers.Count - 1; index >= 0; index--`), adding `orderedPlayers[0]` last. This gives player 0 the highest Z-order rather than the base layer.
2. **Inverted/Non-standard Formula**: Line 180 calculates `top = topBase - ((index - 1) * stackStep)` with `stackStep = viewportHeight * 0.08d` ($8\%$) instead of $+20\%$ of card height ($0.20 \times \text{cardHeight}$).
3. **Viewport Clipping**: `CollapsedCardsViewport` (`CardBoxView.xaml:42-46`) has `IsClippedToBounds="True"` and rigid layout bounds `(0.09 * W, 0.06 * H, 0.82 * W, 0.62 * H)`, cutting off stacked cards and preventing smooth cascading.
4. **CardBoxImage Positioning**: `CardBoxImage` is fixed at `Rect(0, 0, imageWidth, imageHeight)` covering the entire layout instead of being offset relative to the final card bottom.

---

## 3. Requirement R2: Resume Game View Expand Animation & Bounds Constraints

### 3.1 Expand/Collapse Transition Mechanics (`ViewExtensions.cs:38-84`)

```csharp
38: public static async Task TransitionCardBoxAsync(this VisualElement collapsedView, VisualElement expandedView, bool expand, uint duration = 250)
39: {
40:     if (collapsedView == null || expandedView == null) return;
41: 
42:     if (!collapsedView.IsAnimationEnabled() || !expandedView.IsAnimationEnabled())
43:     {
44:         collapsedView.IsVisible = !expand;
45:         expandedView.IsVisible = expand;
46:         collapsedView.Opacity = expand ? 0 : 1;
47:         expandedView.Opacity = expand ? 1 : 0;
48:         return;
49:     }
50: 
51:     collapsedView.CancelAnimations();
52:     expandedView.CancelAnimations();
53: 
54:     if (expand)
55:     {
56:         expandedView.Opacity = 0;
57:         expandedView.Scale = 0.95;
58:         expandedView.IsVisible = true;
59: 
60:         await Task.WhenAll(
61:             collapsedView.FadeTo(0, duration, Easing.CubicInOut),
62:             collapsedView.ScaleTo(0.95, duration, Easing.CubicInOut),
63:             expandedView.FadeTo(1, duration, Easing.CubicInOut),
64:             expandedView.ScaleTo(1.0, duration, Easing.CubicInOut)
65:         );
66: 
67:         collapsedView.IsVisible = false;
68:     }
69:     else
70:     {
71:         collapsedView.Opacity = 0;
72:         collapsedView.Scale = 0.95;
73:         collapsedView.IsVisible = true;
74: 
75:         await Task.WhenAll(
76:             expandedView.FadeTo(0, duration, Easing.CubicInOut),
77:             expandedView.ScaleTo(0.95, duration, Easing.CubicInOut),
78:             collapsedView.FadeTo(1, duration, Easing.CubicInOut),
79:             collapsedView.ScaleTo(1.0, duration, Easing.CubicInOut)
80:         );
81: 
82:         expandedView.IsVisible = false;
83:     }
84: }
```

#### Evaluation:
- The async transition handles cross-fading and scaling cleanly with cancellation safety (`CancelAnimations()`).
- Tapping `CollapsedContainer` invokes `OnCardBoxTapped` (`_isExpanded = true`).
- Tapping `EmptyCardBoxImage` invokes `OnEmptyCardBoxTapped` (`_isExpanded = false`).
- The transition is functionally sound but is severely visually degraded by downstream bounds constraints.

---

### 3.2 Layout & Bounds Constraints Causing Clipping

#### Defect 1: Hardcoded `CardBorder` Width/Height in `PlayerCardView.xaml.cs:183-210`
```csharp
183: 	private void UpdatePlayerCardDimensions()
184: 	{
185: 		var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);
186: 
187: 		if (IsInCardBox)
188: 		{
189: 			if (WidthRequest > 0)
190: 			{
191: 				desiredWidth = WidthRequest;
192: 			}
...
206: 		}
207: 
208: 		CardBorder.WidthRequest = desiredWidth;
209: 		CardBorder.HeightRequest = desiredHeight;
210: 	}
```
- **The Clipping Mechanism**:
  1. In `BaseView.cs:105-151`, `GetWidthAndHeight(...)` calculates a global screen-based card width: `desiredWidth = Math.Clamp(screenWidth * widthMultiplier, 260, 360)` (up to 400 on desktop).
  2. In `PlayerCardView.xaml.cs:208`, when `IsInCardBox` is false (full player card mode), `CardBorder.WidthRequest = desiredWidth` (e.g. 360dp) and `CardBorder.HeightRequest = desiredHeight` (e.g. 470dp).
  3. In `CardBoxView.xaml:61-116`, `ExpandedContainer` is a 2-column Grid:
     - Column 0: `EmptyCardBoxImage` (`WidthRequest="100"`, `Margin="0,0,8,0"`).
     - Column 1: `ExpandedPlayersList` (`Margin="8,0,0,0"`).
     - Column Spacing: `16`.
  4. If `CardBoxView.WidthRequest` is fixed to 360dp, the total width allocated to Column 1 is:
     $$\text{Width}_{\text{Col 1}} = 360 - 100 - 8 - 16 - 8 = 228\text{dp}$$
  5. However, `PlayerCardView` forces `CardBorder.WidthRequest = 360\text{dp}`!
  6. The card overflows Column 1 by $360 - 228 = 132\text{dp}$. The entire right side—including stats column 2 values (e.g., Games Won, Highest Scored Hand), the edit pencil button, and the right rounded border—is clipped out of view.

#### Defect 2: Rigid Width/Height on `CardBoxView` and `ExpandedPlayersList`
In `CardBoxView.xaml.cs:102-135`:
```csharp
104: 		var (desiredWidth, desiredHeight) = GetWidthAndHeight(DeviceDisplay.MainDisplayInfo);
105: 		WidthRequest = desiredWidth;
106: 		HeightRequest = desiredHeight;
...
132: 		double expandedPlayerWidth = Math.Max(220d, desiredWidth - 95d);
133: 		ExpandedPlayersList.WidthRequest = expandedPlayerWidth;
134: 		ExpandedPlayersList.HeightRequest = imageHeight;
```
- Hardcoding `CardBoxView.WidthRequest = desiredWidth` prevents the view from expanding into the available width of `MainPage.xaml`'s host container (which has `MaximumWidthRequest="600"`).
- Setting `ExpandedPlayersList.WidthRequest` and `ExpandedPlayersList.HeightRequest` constrains the `CollectionView`'s natural measure and scrolling behavior.

#### Defect 3: Stats & Footer Clipping from Height Constraints
- When `desiredHeight` is restricted on small screens or landscape mode (e.g., `minHeight = 300`, `maxHeight = 420`), `CardBorder.HeightRequest = desiredHeight` forces the card height below its intrinsic content height (Header + 7 stats rows + 8 divider BoxViews + Footer = ~390dp).
- This truncates `FooterGrid` (containing "Player Created [Date]") and the lower stats rows.

---

### 3.3 Discovered Binding Defect in `CardBoxView.xaml:51-58`

In `CardBoxView.xaml`:
```xml
51:                 <Label x:Name="GameStartedLabel"
52:                        Text="{Binding CurrentGame.StartedDate, StringFormat='Started: {0:MMM dd, yyyy}', Source={x:Reference thisCardBoxView}}"
53:                        TextColor="{AppThemeBinding Light={StaticResource Slate900}, Dark={StaticResource Slate50}}"
54:                        FontSize="12"
55:                        HorizontalOptions="Center"
56:                        VerticalOptions="Start"
57:                        VerticalTextAlignment="Center" />
```
- In `CurrentGameModel.cs:9`, the date property is `public DateTime GameStart { get; init; } = DateTime.Now;`.
- `StartedDate` does not exist on `CurrentGameModel`, causing a binding failure and empty label text.
- **Fix**: Update binding to `CurrentGame.GameStart`.

---

## 4. Comprehensive Architectural Mapping & File Trace

| File Path | Role | Lines of Interest | Existing Behavior & Issues |
|---|---|---|---|
| `RummyBooky/Views/CardBoxView.xaml` | Collapsed & Expanded UI Template | 8–59, 61–116 | Viewport clipping on `CollapsedCardsViewport`; hardcoded column sizes; broken `StartedDate` binding on line 52. |
| `RummyBooky/Views/CardBoxView.xaml.cs` | CardBox Code-Behind & Layout Engine | 89–100, 102–135, 155–195, 197–210 | Sorts descending by `LifetimeScore` instead of ascending by `PlayerScore`; inverted rendering loop and wrong step formula; hardcodes `WidthRequest`/`HeightRequest`. |
| `RummyBooky/Views/PlayerCardView.xaml` | Player Card UI Template | 4–67 | Fixed padding and margins; child of `BaseView`; contains `HeaderGrid`, `PlayerStatsGrid`, `FooterGrid`. |
| `RummyBooky/Views/PlayerCardView.xaml.cs` | Player Card Code-Behind & Sizing | 69–77, 127–181, 183–210 | `UpdatePlayerCardDimensions()` overrides `CardBorder.WidthRequest`/`HeightRequest` with fixed screen-based dimensions, causing clipping when placed in narrow containers. |
| `RummyBooky/Views/BaseView.cs` | Common View Base Class | 86–153 | `GetWidthAndHeight` clamps card dimensions to hardcoded min/max ranges (260–360/400 width, 300–470/495 height). |
| `RummyBooky/Extensions/ViewExtensions.cs` | Animation Extensions | 38–84 | `TransitionCardBoxAsync` provides cross-fade and scale animation. |
| `RummyBooky/Pages/MainPage.xaml` | Resume View Host | 49–87 | Hosts `CardBoxView` inside `CollectionView.ItemTemplate` with `Grid.Row="1"`. Container max width is 600dp. |
| `RummyBooky/Models/PlayerModel.cs` | Player Domain Model | 23, 28 | `PlayerScore` (current game score, int); `LifetimeScore` (historical cumulative, double). |
| `RummyBooky/Models/CurrentGameModel.cs` | Current Game Model | 9 | `GameStart` (DateTime). |

---

## 5. Recommended Refactoring Plan

### Phase 1: Fix Score Ordering & Domain Model Binding
1. In `CardBoxView.xaml.cs` (`GetOrderedPlayers`):
   - Replace `.OrderByDescending(p => p.LifetimeScore)` with `.OrderBy(p => p.PlayerScore).ThenBy(p => p.PlayerName)`.
2. In `CardBoxView.xaml` (line 52):
   - Replace `CurrentGame.StartedDate` with `CurrentGame.GameStart`.

### Phase 2: Refactor Cascading Stack Layout Math (R1)
1. In `CardBoxView.xaml.cs` (`RenderCollapsedCards`):
   - Determine baseline card dimensions $W_{\text{card}}$ and $H_{\text{card}}$.
   - Loop in ascending index order: `for (int i = 0; i < orderedPlayers.Count; i++)`.
   - Compute top coordinate: `double top = i * (0.20 * cardHeight);`.
   - Instantiate `PlayerCardView` with `AssignedPlayerModel = orderedPlayers[i]`.
   - Set layout bounds: `AbsoluteLayout.SetLayoutBounds(playerCardView, new Rect(0, top, cardWidth, cardHeight))`.
   - Add to `CollapsedCardsCanvas.Children` in sequence ($i = 0$ is added 1st at $Y=0$, lowest Z-order; $i = N-1$ is added last, topmost Z-order).
2. Position `CardBoxImage` / Collapsed Container:
   - Position `CardBoxImage` at $Y_{\text{box}} = \text{orderedPlayers.Count} \times 0.20 \times H_{\text{card}}$ (or $Y_{\text{bottom, final}} + 0.20 \times H_{\text{card}}$).
   - Dynamically size `CardBoxLayout` / `CardBoxView` height to $Y_{\text{box}} + H_{\text{box}}$.
   - Remove `IsClippedToBounds="True"` restriction from `CollapsedCardsViewport` or align viewport bounds to the total cascade bounding box.

### Phase 3: Eliminate Bounds & Width/Height Constraints (R2)
1. In `PlayerCardView.xaml.cs`:
   - Refactor `UpdatePlayerCardDimensions()` so that when `IsInCardBox` is false (or when used inside `ExpandedPlayersList`), `CardBorder` does NOT have rigid hardcoded `WidthRequest` / `HeightRequest` forced on it.
   - Set `CardBorder.HorizontalOptions = LayoutOptions.Fill` and `CardBorder.ClearValue(VisualElement.WidthRequestProperty)`.
   - Allow `CardBorder` height to size intrinsically to its content (`ClearValue(VisualElement.HeightRequestProperty)`), avoiding clipping of stats rows and `FooterGrid`.
2. In `CardBoxView.xaml.cs`:
   - In `UpdateDimensions()`, do not constrain `WidthRequest` and `HeightRequest` of `ExpandedPlayersList` to narrow single-card constants.
   - Allow `ExpandedContainer` and `ExpandedPlayersList` to fill available width (`HorizontalOptions="Fill"`).
   - In `CardBoxView.xaml`, ensure `ExpandedPlayersList` has `HorizontalOptions="Fill"` and items render full-width cards within Column 1.

### Phase 4: Preserve & Polish Expand/Collapse Animation (R2)
1. Verify `TransitionCardBoxAsync` smoothly executes fade and scale between `CollapsedContainer` and `ExpandedContainer`.
2. Ensure touch gesture hit-targets remain active and responsive on `CollapsedContainer` and `EmptyCardBoxImage`.

---

## 6. Independent Verification & Test Method

1. **Unit / Logic Verification**:
   - Verify ascending order: Given players with scores $[150, 20, 85, 0]$, sorted collection must be $[0, 20, 85, 150]$.
   - Verify offsets: Card 0 at $Y = 0$; Card 1 at $Y = 0.20 H$; Card 2 at $Y = 0.40 H$; Card 3 at $Y = 0.60 H$.
   - Verify Z-order: Canvas child 0 is score 0; child 3 is score 150.
2. **Visual & Interactive Verification on Windows / Android**:
   - Deploy/Run app on Windows (`net10.0-windows10.0.19041.0`) and Android emulator (`net10.0-android`).
   - Open `MainPage` with active resume game.
   - Confirm cascading stack shows all player name headers exposed cleanly for games with 2 to 6 players.
   - Tap collapsed card box: verify smooth expansion transition docking `EmptyCardBoxImage` to the left and presenting unclipped player cards on the right.
   - Verify that all stats rows, column 2 values, timestamps, edit pencil buttons, and rounded borders render completely with 0 horizontal or vertical clipping.
   - Tap docked empty card box: verify smooth collapse transition back to cascading stack.
3. **Compilation Command**:
   ```powershell
   dotnet build c:\Dev\RummyBookyMaui\RummyBooky\RummyBooky.csproj -f net10.0-windows10.0.19041.0
   ```
   Ensure build passes with **0 Errors, 0 Warnings**.
