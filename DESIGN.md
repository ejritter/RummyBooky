# Design System

This document serves as the aesthetic anchor for `/maui-impeccable` commands.

## Core Vibe
Premium, sleek, and tactile. A sophisticated card-table aesthetic (modern casino vibe, deep colors, or a sleek dark mode) that feels trustworthy, snappy, and clear during gameplay.

## Color Palette
*Note: Grays must always be tinted towards the primary brand color.*
- **Primary**: Deep Ruby Red or Emerald Green
- **Secondary**: Rich Gold or Brass (for highlights, winner badges)
- **Backgrounds (Light/Dark)**: 
  - Light: Tinted off-white
  - Dark: Deep charcoal/slate
- **Surface & Cards (Light/Dark)**: 
  - Light: White with soft, tinted shadows
  - Dark: Slightly lighter charcoal with subtle borders and elevation
- **Text Constraints**: Avoid pure `#000000` or `#FFFFFF`. Use deep slate for dark text and off-white/silver for light text.

## Typography Scale
*Modern, responsive baseline.*
- **Header**: Large (24-32pt), high contrast, Bold. Used for page titles and game status.
- **Subtitle**: Medium (16-18pt), slightly muted contrast. Used for player names, section headers.
- **Body**: Standard (14pt), highly legible. Used for scores, basic list items.
- **Caption/Micro**: Small (12pt), muted, semantic. Used for timestamps, subtle meta-information (e.g. highest/lowest hand tags).

## Structural Rules
- Layouts prefer `Grid` (for complex alignments like score tables) and `FlexLayout` (for wrapping lists/chips) over nested `StackLayout`s.
- `Padding` and `Spacing` strictly follow a 4dp/8dp rhythm (e.g., 8, 16, 24, 32).
- Zero legacy `<Frame>` elements; exclusively use `<Border>`.
- All interactive elements must implement `VisualStateManager` groups (Normal, PointerOver, Pressed) for tactile feedback.
- All color and background assignments must use `{AppThemeBinding}`.
