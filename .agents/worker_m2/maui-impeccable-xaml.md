# maui-impeccable-xaml Skill Copy
Source: C:\Users\roija\.gemini\config\skills\maui-impeccable-xaml\SKILL.md

## Core Architectural Rules (Inspired by Syncfusion & Telerik)
1. View Architecture: Flat Over Nested. Never use deeply nested VerticalStackLayout/HorizontalStackLayout. Use Grid with RowSpacing and ColumnSpacing for layout structure.
2. Styles Architecture: Zero inline slop. Extract colors, dimensions, typography into ResourceDictionary. Use Style targeting explicit component classes.
3. Dynamic AppThemeBinding: Every Color/BackgroundColor/BorderColor/Shadow property MUST use {AppThemeBinding}.
4. Typography Scale & Visual Hierarchy: Header, Subtitle, Body, Caption.
5. Impeccable Detector Rules:
   - No legacy <Frame> usage. Exclusively use <Border> with StrokeShape.
   - No nested cards (<Border> inside <Border>).
   - Untinted grays and pure black forbidden.
   - Unresponsive touch targets: Button/tappable surface height under 44dp.
   - Dead interfaces: interactive controls missing VisualStateManager groups (Normal, PointerOver, Pressed).
   - Dated easing: smooth timing.
