# PROJECT: RummyBooky .NET MAUI Impeccable UI Refactoring

## Architecture
- Framework: .NET MAUI (`net10.0-windows10.0.19041.0`)
- Pattern: MVVM + Pure Native XAML (`Microsoft.Maui.Controls`)
- Styling: Dynamic `{AppThemeBinding}` resource dictionaries (`Colors.xaml`, `Theme.xaml`, `Typography.xaml`, `Dimensions.xaml`, `Styles.xaml`)
- Animations: Custom `ViewExtensions.cs` using `maui-animations` standards (`IsAnimationEnabled` accessibility, `CancelAnimations()` safety, `Easing.CubicOut`)
- Controls: Exclusive use of `<Border>` with `StrokeShape` round rectangles (0 `<Frame>` elements), `<Grid>` and `<FlexLayout>` layout containers (0 nested StackLayouts)

## Feature Inventory
| # | Component / Feature | Scope & Description | Milestone | Source |
|---|---------------------|---------------------|-----------|--------|
| 1 | Colors & Theme System | Refactor `Colors.xaml` to Ruby Red/Slate/Gold, create `Theme.xaml`, `Typography.xaml`, `Dimensions.xaml` | M1 | survey (Explorer 2) |
| 2 | Global Styles & VSM | Update `Styles.xaml` with `Pressed` VSM states, fix `{x:StaticResource}` syntax, fix base Label color | M1 | survey (Explorer 1 & 2) |
| 3 | Animation Framework | Create `Extensions/ViewExtensions.cs` with `IsAnimationEnabled` and `CancelAnimations()` safety | M1 | survey (Explorer 3) |
| 4 | MainPage UI | Replace nested StackLayouts with Grid, add VSM and press animations to interactive elements | M2 | survey (Explorer 1) |
| 5 | PlayerCardView | Flatten nested `<Border>` cards (`InnerCardBorder` in `CardBorder`) into a single `<Border>` card | M2 | survey (Explorer 1 & 2) |
| 6 | CardBoxView | Add VSM visual states, replace instant visibility toggles with smooth `TransitionCardBoxAsync` animations | M2 | survey (Explorer 1 & 3) |
| 7 | NewGamePage UI | Remove redundant `<Border>` button wrappers, replace inline colors with theme tokens, add VSM/animations | M3 | survey (Explorer 1 & 2) |
| 8 | EditPlayerPage UI | Refactor outer layout to Grid, fix clipped GamesGrid row height (`50` to `Auto`), add VSM/animations | M3 | survey (Explorer 1) |
| 9 | CurrentGamePage UI | Refactor 3-level-deep nested StackLayouts to Grid, fix `{x:StaticResource}` syntax, add VSM/animations | M4 | survey (Explorer 1 & 2) |
| 10| GeneralPopupPage UI| Refactor StackLayout and 5-button action bar to FlexLayout/Grid, fix raw white/black bindings, add VSM | M4 | survey (Explorer 1 & 2) |
| 11| LeaderboardPage UI | Refactor layout spacing to 8dp grid, update dynamic bindings, add VSM and press feedback | M5 | survey (Explorer 1) |
| 12| Project Audit & Build| Verify `dotnet build` compiles cleanly, conduct audit sweep for Frame=0, AppThemeBinding=100%, VSM=100% | M5 | survey (All) |

## Milestones
| # | Name | Scope | Dependencies | Status |
|---|------|-------|-------------|--------|
| M1 | Global Styles & Animation Infrastructure | Colors, Theme, Typography, Dimensions, Styles, ViewExtensions | None | DONE |
| M2 | MainPage, CardBoxView & PlayerCardView | MainPage.xaml, CardBoxView.xaml/.cs, PlayerCardView.xaml | M1 | DONE |
| M3 | NewGamePage & EditPlayerPage | NewGamePage.xaml, EditPlayerPage.xaml | M1 | DONE |
| M4 | CurrentGamePage & GeneralPopupPage | CurrentGamePage.xaml, GeneralPopupPage.xaml | M1 | DONE |
| M5 | LeaderboardPage & Hardening Audit | LeaderboardPage.xaml, full build & static audit verification | M1, M2, M3, M4 | DONE |

## Code Layout
- `RummyBooky/Resources/Styles/Colors.xaml`: Primary, Secondary, Slate grays, semantic color definitions
- `RummyBooky/Resources/Styles/Theme.xaml`: AppThemeBinding semantic theme tokens for Light and Dark modes
- `RummyBooky/Resources/Styles/Typography.xaml`: HeaderLabel, SubtitleLabel, BodyLabel, CaptionLabel styles
- `RummyBooky/Resources/Styles/Dimensions.xaml`: Spacing, Padding, CornerRadius, IconSize tokens (4dp/8dp grid)
- `RummyBooky/Resources/Styles/Styles.xaml`: Control styles (`Button`, `ImageButton`, `Entry`, `Border`, `Label`, etc.)
- `RummyBooky/Extensions/ViewExtensions.cs`: Accessible, glitch-free view animation extensions
- `RummyBooky/Pages/MainPage.xaml` & `.xaml.cs`: Main menu and game launcher page
- `RummyBooky/Pages/NewGamePage.xaml` & `.xaml.cs`: Player setup and game configuration page
- `RummyBooky/Pages/CurrentGamePage.xaml` & `.xaml.cs`: Active match scoring and round tracking page
- `RummyBooky/Pages/EditPlayerPage.xaml` & `.xaml.cs`: Player profile editing page
- `RummyBooky/Pages/LeaderboardPage.xaml` & `.xaml.cs`: Standings and stats display page
- `RummyBooky/Pages/GeneralPopupPage.xaml` & `.xaml.cs`: Reusable popup dialog for winner selection / alerts
- `RummyBooky/Views/CardBoxView.xaml` & `.xaml.cs`: Expandable card container control
- `RummyBooky/Views/PlayerCardView.xaml` & `.xaml.cs`: Player status card view component
