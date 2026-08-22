# R3: Theme & Color Audit Report

**Auditor**: Explorer 2 (Theme & Color Auditor)  
**Target**: `c:\Dev\RummyBookyMaui`  
**Date**: 2026-08-05  

---

## Executive Summary

Audit of all 16 `.xaml` files in the `RummyBooky` project against **R3: Theme & Color Audit** standards (Impeccable UI rules):
1. **Dynamic Resource Adherence**: Widespread use of `{StaticResource ...}` instead of `{DynamicResource ...}` when binding semantic theme tokens (`BackgroundPrimary`, `BackgroundSecondary`, `CardBackground`, `CardBorderColor`, `TextPrimary`, `TextSecondary`, `AccentPrimary`, etc.). On theme switching at runtime, `{StaticResource}` fails to reactively re-bind to updated theme tokens.
2. **Untinted Grays & Hardcoded Primitive Colors**:
   - `Colors.xaml` defines untinted primitive colors (`White`, `Black`, `Gray100` through `Gray950`) and untinted gray brushes.
   - `Theme.xaml` uses pure `#000000` with alpha for `ShadowColor` (`#20000000` and `#80000000`) and pure `White` for light mode card backgrounds.
   - `Styles.xaml` heavily relies on untinted grays (`Gray100`–`Gray950`), pure `White`, and pure `Black` for default control states, disabled states, shell items, pages, entries, pickers, search bars, etc.
3. **Hardcoded Color Bindings on Controls**: Multiple pages (`NewGamePage.xaml`, `CurrentGamePage.xaml`, `EditPlayerPage.xaml`, `LeaderboardPage.xaml`, `MainPage.xaml`, `GeneralPopupPage.xaml`, `CardBoxView.xaml`, `PlayerCardView.xaml`) bind color properties to `{StaticResource}` instead of `{DynamicResource}` or direct `{AppThemeBinding}` tokens.

---

## Audit Findings & Recommended XAML Fixes

### 1. `RummyBooky/Resources/Styles/Colors.xaml`

#### Finding 1.1: Untinted Primitive Color Definitions & Untinted Grays
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Resources\Styles\Colors.xaml`
- **Line Numbers**: 15–16, 37–45, 52–59
- **Code Snippet**:
  ```xaml
  15: <Color x:Key="White">White</Color>
  16: <Color x:Key="Black">Black</Color>
  ...
  37: <Color x:Key="Gray100">#E1E1E1</Color>
  38: <Color x:Key="Gray200">#C8C8C8</Color>
  39: <Color x:Key="Gray300">#ACACAC</Color>
  40: <Color x:Key="Gray400">#919191</Color>
  41: <Color x:Key="Gray500">#6E6E6E</Color>
  42: <Color x:Key="Gray600">#404040</Color>
  43: <Color x:Key="Gray900">#212121</Color>
  44: <Color x:Key="Gray950">#141414</Color>
  ```
- **Rule Violation**: R3.b - Untinted grays and pure `#000000`/`Black`/`#FFFFFF`/`White` defined as static primitives without slate/tinting.
- **Recommended XAML Fix**:
  Replace pure untinted grays with palette-tinted slate equivalents (e.g. `Slate50` through `Slate950`) or warm/cool tinted tokens:
  ```xaml
  <!-- Slate-Tinted Themed Tokens -->
  <Color x:Key="White">#F7FAFC</Color>
  <Color x:Key="Black">#0F172A</Color>
  <Color x:Key="Gray100">#EDF2F7</Color>
  <Color x:Key="Gray200">#E2E8F0</Color>
  <Color x:Key="Gray300">#CBD5E0</Color>
  <Color x:Key="Gray400">#A0AEC0</Color>
  <Color x:Key="Gray500">#718096</Color>
  <Color x:Key="Gray600">#4A5568</Color>
  <Color x:Key="Gray900">#171923</Color>
  <Color x:Key="Gray950">#0F172A</Color>
  ```

---

### 2. `RummyBooky/Resources/Styles/Theme.xaml`

#### Finding 2.1: Hardcoded Pure Black Shadow & Pure White Light Background
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Resources\Styles\Theme.xaml`
- **Line Numbers**: 11, 15, 16
- **Code Snippet**:
  ```xaml
  11: <AppThemeBinding x:Key="CardBackground" Light="{StaticResource White}" Dark="{StaticResource Slate800}" />
  15: <AppThemeBinding x:Key="SurfaceElevation1" Light="{StaticResource White}" Dark="{StaticResource Slate800}" />
  16: <AppThemeBinding x:Key="ShadowColor" Light="#20000000" Dark="#80000000" />
  ```
- **Rule Violation**: R3.b - Hardcoded pure black (`#000000`) with alpha in `ShadowColor` and pure `White` in `CardBackground` / `SurfaceElevation1`.
- **Recommended XAML Fix**:
  Use tinted slate colors for light surface/card backgrounds and tinted alpha hex for shadows:
  ```xaml
  <AppThemeBinding x:Key="CardBackground" Light="{StaticResource Slate50}" Dark="{StaticResource Slate800}" />
  <AppThemeBinding x:Key="SurfaceElevation1" Light="{StaticResource Slate50}" Dark="{StaticResource Slate800}" />
  <AppThemeBinding x:Key="ShadowColor" Light="#200F172A" Dark="#800F172A" />
  ```

---

### 3. `RummyBooky/Resources/Styles/Styles.xaml`

#### Finding 3.1: Reliance on Untinted Grays, Pure White & Black across Control Styles
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Resources\Styles\Styles.xaml`
- **Line Numbers**: 27, 33, 40, 79, 86, 90, 94-95, 99, 105, 109, 129-130, 151, 161, 169, 181, 190, 194, 203, 212, 216, 225, 290, 297, 304-305, 317-318, 327, 333, 344, 355, 364, 382-383, 403-404, 420-422, 429-431, 440, 452-453, 473, 485, 519, 523-533, 536-538, 542-545
- **Code Snippets**:
  ```xaml
  <!-- Line 27 --> <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource DeepRed}, Dark={StaticResource White}}" />
  <!-- Line 94 --> <Setter Property="IndicatorColor" Value="{AppThemeBinding Light={StaticResource Gray200}, Dark={StaticResource Gray500}}"/>
  <!-- Line 99 --> <Setter Property="Stroke" Value="{AppThemeBinding Light={StaticResource Gray200}, Dark={StaticResource Gray500}}" />
  <!-- Line 105 --> <Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource Gray950}, Dark={StaticResource Gray200}}" />
  <!-- Line 129 --> <Setter Property="TextColor" Value="{AppThemeBinding Light={StaticResource Gray950}, Dark={StaticResource Gray200}}" />
  <!-- Line 519 --> <Setter Property="BackgroundColor" Value="{AppThemeBinding Light={StaticResource White}, Dark={StaticResource OffBlack}}" />
  <!-- Lines 523-533 --> Shell styles using White, Black, OffBlack, Gray200, Gray950
  ```
- **Rule Violation**: R3.a & R3.b - Default control styles hardcode untinted grays (`Gray200`, `Gray500`, `Gray950`) and pure `White`/`Black` instead of using semantic theme tokens (`CardBorderColor`, `TextPrimary`, `TextSecondary`, `BackgroundPrimary`, `BackgroundSecondary`, `AccentPrimary`, etc.).
- **Recommended XAML Fix**:
  Update control styles to consume semantic `AppThemeBinding` tokens (`{DynamicResource TextPrimary}`, `{DynamicResource TextSecondary}`, `{DynamicResource CardBorderColor}`, `{DynamicResource BackgroundPrimary}`, `{DynamicResource AccentPrimary}`).
  Example for `Border`:
  ```xaml
  <Style TargetType="Border">
      <Setter Property="Stroke" Value="{DynamicResource CardBorderColor}" />
      <Setter Property="StrokeShape" Value="Rectangle"/>
      <Setter Property="StrokeThickness" Value="1"/>
  </Style>
  ```
  Example for `Page`:
  ```xaml
  <Style TargetType="Page" ApplyToDerivedTypes="True">
      <Setter Property="Padding" Value="0"/>
      <Setter Property="BackgroundColor" Value="{DynamicResource BackgroundPrimary}" />
  </Style>
  ```
  Example for `Shell`:
  ```xaml
  <Style TargetType="Shell" ApplyToDerivedTypes="True">
      <Setter Property="Shell.BackgroundColor" Value="{DynamicResource BackgroundPrimary}" />
      <Setter Property="Shell.ForegroundColor" Value="{DynamicResource TextPrimary}" />
      <Setter Property="Shell.TitleColor" Value="{DynamicResource TextPrimary}" />
      <Setter Property="Shell.DisabledColor" Value="{DynamicResource TextSecondary}" />
      <Setter Property="Shell.UnselectedColor" Value="{DynamicResource TextSecondary}" />
      <Setter Property="Shell.NavBarHasShadow" Value="False" />
      <Setter Property="Shell.TabBarBackgroundColor" Value="{DynamicResource BackgroundSecondary}" />
      <Setter Property="Shell.TabBarForegroundColor" Value="{DynamicResource AccentPrimary}" />
      <Setter Property="Shell.TabBarTitleColor" Value="{DynamicResource AccentPrimary}" />
      <Setter Property="Shell.TabBarUnselectedColor" Value="{DynamicResource TextSecondary}" />
  </Style>
  ```

---

### 4. `RummyBooky/Pages/MainPage.xaml`

#### Finding 4.1: StaticResource Theme Token Bindings
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Pages\MainPage.xaml`
- **Line Numbers**: 12, 157, 162, 167, 177, 182
- **Code Snippet**:
  ```xaml
  12:  Background="{StaticResource BackgroundPrimary}"
  157: <Setter Property="BackgroundColor" Value="{StaticResource BackgroundSecondary}" />
  162: <Setter Property="BackgroundColor" Value="{StaticResource CardBackground}" />
  167: <Setter Property="BackgroundColor" Value="{StaticResource CardBackground}" />
  177: TextColor="{StaticResource TextSecondary}"
  182: TextColor="{StaticResource TextSecondary}"
  ```
- **Rule Violation**: R3.a & R3.c - Color properties use `{StaticResource}` instead of `{DynamicResource}` for dynamic theme switching tokens.
- **Recommended XAML Fix**:
  Change `{StaticResource ...}` to `{DynamicResource ...}`:
  ```xaml
  Background="{DynamicResource BackgroundPrimary}"
  ...
  <Setter Property="BackgroundColor" Value="{DynamicResource BackgroundSecondary}" />
  <Setter Property="BackgroundColor" Value="{DynamicResource CardBackground}" />
  ...
  TextColor="{DynamicResource TextSecondary}"
  ```

---

### 5. `RummyBooky/Pages/NewGamePage.xaml`

#### Finding 5.1: StaticResource Theme Token Bindings & Color Properties on Controls
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Pages\NewGamePage.xaml`
- **Line Numbers**: 13, 194, 240, 245, 250, 264, 277, 317, 330, 343, 356, 369, 381
- **Code Snippet**:
  ```xaml
  13: Background="{StaticResource BackgroundPrimary}"
  194: Color="{StaticResource AccentPrimary}"
  240: Value="{StaticResource BackgroundSecondary}"
  245: Value="{StaticResource CardBackground}"
  250: Value="{StaticResource CardBackground}"
  264: TextColor="{StaticResource AccentPrimary}"
  277: TextColor="{StaticResource AccentPrimary}"
  317, 330, 343, 356, 369, 381: Color="{StaticResource CardBorderColor}"
  ```
- **Rule Violation**: R3.a & R3.c - Direct control color properties and VisualState setters use `{StaticResource}` instead of `{DynamicResource}`.
- **Recommended XAML Fix**:
  Update all lines to use `{DynamicResource}`:
  ```xaml
  Background="{DynamicResource BackgroundPrimary}"
  Color="{DynamicResource AccentPrimary}"
  Value="{DynamicResource BackgroundSecondary}"
  Value="{DynamicResource CardBackground}"
  TextColor="{DynamicResource AccentPrimary}"
  Color="{DynamicResource CardBorderColor}"
  ```

---

### 6. `RummyBooky/Pages/CurrentGamePage.xaml`

#### Finding 6.1: StaticResource Theme Token Bindings & Deprecated BackgroundColor
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Pages\CurrentGamePage.xaml`
- **Line Numbers**: 12, 113, 149, 154, 159, 193, 229, 242, 274
- **Code Snippet**:
  ```xaml
  12: Background="{StaticResource BackgroundPrimary}"
  113: BackgroundColor="{StaticResource AccentPrimary}"
  149: Value="{StaticResource BackgroundSecondary}"
  154: Value="{StaticResource CardBackground}"
  159: Value="{StaticResource CardBackground}"
  193: TextColor="{StaticResource AccentPrimary}"
  229, 242, 274: Color="{StaticResource CardBorderColor}"
  ```
- **Rule Violation**: R3.a & R3.c - Color properties use `{StaticResource}` instead of `{DynamicResource}`. Line 113 also uses `BackgroundColor` on BoxView instead of `Color`.
- **Recommended XAML Fix**:
  ```xaml
  Background="{DynamicResource BackgroundPrimary}"
  Color="{DynamicResource AccentPrimary}"
  Value="{DynamicResource BackgroundSecondary}"
  Value="{DynamicResource CardBackground}"
  TextColor="{DynamicResource AccentPrimary}"
  Color="{DynamicResource CardBorderColor}"
  ```

---

### 7. `RummyBooky/Pages/EditPlayerPage.xaml`

#### Finding 7.1: StaticResource Theme Token Bindings
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Pages\EditPlayerPage.xaml`
- **Line Numbers**: 12, 42, 102, 115, 129
- **Code Snippet**:
  ```xaml
  12: Background="{StaticResource BackgroundPrimary}"
  42: PlaceholderColor="{StaticResource TextSecondary}"
  102: TextColor="{StaticResource TextSecondary}"
  115: TextColor="{StaticResource TextSecondary}"
  129: TextColor="{StaticResource TextSecondary}"
  ```
- **Rule Violation**: R3.a & R3.c - Color properties use `{StaticResource}` instead of `{DynamicResource}`.
- **Recommended XAML Fix**:
  ```xaml
  Background="{DynamicResource BackgroundPrimary}"
  PlaceholderColor="{DynamicResource TextSecondary}"
  TextColor="{DynamicResource TextSecondary}"
  ```

---

### 8. `RummyBooky/Pages/LeaderboardPage.xaml`

#### Finding 8.1: StaticResource Theme Token Bindings
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Pages\LeaderboardPage.xaml`
- **Line Numbers**: 11, 35, 36, 44, 54, 55, 64, 71
- **Code Snippet**:
  ```xaml
  11: Background="{StaticResource BackgroundPrimary}"
  35: Background="{StaticResource CardBackground}"
  36: Stroke="{StaticResource CardBorderColor}"
  44: TextColor="{StaticResource TextSecondary}"
  54: Background="{StaticResource CardBackground}"
  55: Stroke="{StaticResource CardBorderColor}"
  64: Value="{StaticResource CardBorderColor}"
  71: Value="{StaticResource AccentPrimary}"
  ```
- **Rule Violation**: R3.a & R3.c - Color properties use `{StaticResource}` instead of `{DynamicResource}`.
- **Recommended XAML Fix**:
  ```xaml
  Background="{DynamicResource BackgroundPrimary}"
  Background="{DynamicResource CardBackground}"
  Stroke="{DynamicResource CardBorderColor}"
  TextColor="{DynamicResource TextSecondary}"
  Value="{DynamicResource CardBorderColor}"
  Value="{DynamicResource AccentPrimary}"
  ```

---

### 9. `RummyBooky/Pages/GeneralPopupPage.xaml`

#### Finding 9.1: StaticResource Theme Token Bindings
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Pages\GeneralPopupPage.xaml`
- **Line Numbers**: 11, 52, 57, 62, 67, 68
- **Code Snippet**:
  ```xaml
  11: Background="{StaticResource BackgroundPrimary}"
  52: Setter Property="Stroke" Value="{StaticResource CardBorderColor}"
  57: Setter Property="BackgroundColor" Value="{StaticResource BackgroundSecondary}"
  62: Setter Property="BackgroundColor" Value="{StaticResource CardBackground}"
  67: Setter Property="BackgroundColor" Value="{StaticResource AccentPrimary}"
  68: Setter Property="Stroke" Value="{StaticResource AccentPrimary}"
  ```
- **Rule Violation**: R3.a & R3.c - Color properties use `{StaticResource}` instead of `{DynamicResource}`.
- **Recommended XAML Fix**:
  ```xaml
  Background="{DynamicResource BackgroundPrimary}"
  Value="{DynamicResource CardBorderColor}"
  Value="{DynamicResource BackgroundSecondary}"
  Value="{DynamicResource CardBackground}"
  Value="{DynamicResource AccentPrimary}"
  ```

---

### 10. `RummyBooky/Views/CardBoxView.xaml`

#### Finding 10.1: StaticResource Theme Token Binding
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Views\CardBoxView.xaml`
- **Line Number**: 51
- **Code Snippet**:
  ```xaml
  51: TextColor="{StaticResource TextPrimary}"
  ```
- **Rule Violation**: R3.a & R3.c - Color property uses `{StaticResource}` instead of `{DynamicResource}`.
- **Recommended XAML Fix**:
  ```xaml
  TextColor="{DynamicResource TextPrimary}"
  ```

---

### 11. `RummyBooky/Views/PlayerCardView.xaml`

#### Finding 11.1: StaticResource Theme Token Bindings
- **File**: `c:\Dev\RummyBookyMaui\RummyBooky\Views\PlayerCardView.xaml`
- **Line Numbers**: 13, 14, 33, 54, 61, 102, 103, 104, 106, 107, 108, 110, 111, 112, 114, 115, 116, 118, 119, 120, 122, 123, 124, 126, 127, 128, 130, 131, 132, 144, 145, 148
- **Code Snippet**:
  ```xaml
  13: Background="{StaticResource CardBackground}"
  14: Stroke="{StaticResource CardBorderColor}"
  33: TextColor="{StaticResource AccentPrimary}"
  54: Background="{StaticResource AccentPrimary}"
  61: TextColor="{StaticResource CardBackground}"
  102-132: TextColor="{StaticResource TextSecondary}", TextColor="{StaticResource AccentPrimary}", Color="{StaticResource CardBorderColor}"
  144, 145: TextColor="{StaticResource TextSecondary}"
  148: TextColor="{StaticResource AccentPrimary}"
  ```
- **Rule Violation**: R3.a & R3.c - Component card elements bind color properties to `{StaticResource}` instead of `{DynamicResource}`.
- **Recommended XAML Fix**:
  Update all occurrences of `{StaticResource CardBackground}`, `{StaticResource CardBorderColor}`, `{StaticResource AccentPrimary}`, `{StaticResource TextSecondary}`, `{StaticResource TextPrimary}` to `{DynamicResource ...}`.

---

## Verification Plan

1. **Static Audit Verification**:
   Inspect all `.xaml` files to confirm no raw untinted gray primitives or hardcoded black/white strings exist, and that all color attributes consume `{DynamicResource}` pointing to semantic tokens in `Theme.xaml`.
2. **Runtime Theme Switch Verification**:
   Run `.NET MAUI` build (`dotnet build -f net10.0-windows10.0.19041.0`) and toggle OS light/dark theme. Verify all page backgrounds, card borders, text labels, buttons, and box views reactively transition without residual white/black artifacts or layout jumps.
