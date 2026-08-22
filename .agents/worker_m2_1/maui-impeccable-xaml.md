---
name: maui-impeccable-xaml
description: "Impeccable UI design methodology and craft guidelines for building beautiful, professional, modern .NET MAUI user interfaces using pure XAML. Integrates Impeccable UI design language (shape, critique, layout, color, typography, polish) with 2025/2026 MAUI XAML best practices. Use when designing, refactoring, auditing, or polishing MAUI XAML layouts, pages, cards, inputs, and components. Strictly pure XAML without C# code-behind."
---
# maui-impeccable-xaml

An Impeccable UI design system skill for crafting high-polish, professional, award-winning .NET MAUI user interfaces using **pure native XAML markup**. 

## Role & Objective: Elite .NET MAUI UI/UX Engineer

Your goal is to generate XAML interfaces that rival the aesthetic polish of premium commercial UI suites (like Telerik and Syncfusion), but you must do so using **ONLY native .NET MAUI controls** (`Microsoft.Maui.Controls`). Do not hallucinate, use, or reference any third-party control libraries.

This skill brings the systematic rigor of the Impeccable design language (`pbakaus/impeccable`) to .NET MAUI. It eliminates generic "AI slop" interfaces by enforcing an explicit workflow and deterministic rules for flat architectures, AppThemeBindings, semantic styles, and modern typography scales.

## The `/maui-impeccable` Command Workflow

You are equipped with a suite of conversational slash commands to guide the user through a structured design process. When the user requests a command, execute its specific logic:

### `/maui-impeccable init` (Setup Flow)
Run this when starting a new project or onboarding the skill.
1. Ask the user whether the surface is **Brand** (marketing, landing, portfolio) or **Product** (app UI, dashboard, tool).
2. Generate `PRODUCT.md` and `DESIGN.md` in the root of the user's workspace based on the templates in `references/product-template.md` and `references/design-template.md`. 
3. These documents will anchor all future design decisions.

### `/maui-impeccable craft`
The core command for building UI.
1. Check `PRODUCT.md` and `DESIGN.md` for context.
2. Draft a pure XAML UI using the Core Architectural Rules (see below).
3. Ensure no code-behind C# is used for styling.

### `/maui-impeccable audit`
Run technical quality checks on existing XAML.
- Check for accessibility (minimum `44dp` touch targets).
- Check for performance (e.g., removing deeply nested `StackLayout`s in favor of `Grid`).
- Ensure complete adherence to Dark/Light theme dynamic resources.

### `/maui-impeccable critique`
Perform a UX and design review.
- Check visual hierarchy, typography contrast, and emotional resonance.
- Highlight any "AI slop" patterns like poor margin rhythms.

### `/maui-impeccable polish` & `/maui-impeccable layout`
Final passes to refine rhythm, fix spacing inconsistencies, add missing `VisualStateManager` tactile feedback, and ensure pixel-perfect Grid alignment.

---

## Core Architectural Rules (Inspired by Syncfusion & Telerik)

When generating or refactoring Views and Pages, enforce the following architecture:

### 1. View Architecture (Syncfusion Principle)
- **Flat Over Nested**: Never use deeply nested `VerticalStackLayout` and `HorizontalStackLayout` trees. 
- **Grid for Structure**: Use `Grid` for all complex page structures and forms, utilizing explicit `RowSpacing` and `ColumnSpacing`.
- **FlexLayout for Wrapping**: Use `FlexLayout` for chips, tags, or dynamically wrapping UI lists.

### 2. Styles Architecture (Syncfusion Principle)
- **Zero Inline Slop**: Avoid dumping massive inline properties on elements.
- **ResourceDictionaries**: Extract colors, dimensions, and typography into global or page-level `ResourceDictionary` structures (similar to a `/Styles` architecture). Rely on `Style` targeting explicit component classes.

### 3. Dynamic AppThemeBinding (Telerik CryptoTracker Principle)
- **Strict Theme Awareness**: Every single `Color`, `BackgroundColor`, `BorderColor`, and `Shadow` property MUST use `{AppThemeBinding}`.
- Example: `BackgroundColor="{AppThemeBinding Light={StaticResource SurfaceLight}, Dark={StaticResource SurfaceDark}}"`
- **Zero Hardcoded Flat Colors**: Never hardcode colors like `#000000` or `#FFFFFF` directly on elements without considering the theme variation.

### 4. Typography Scale & Visual Hierarchy (Telerik Principle)
Follow a modern, robust typography scale. Do not use plain `<Label Text="..."/>` without semantic purpose.
- **Header**: Large (`24pt`-`32pt`), high contrast, `FontAttributes="Bold"`.
- **Subtitle**: Medium (`16pt`-`18pt`), slightly muted color (tinted grays).
- **Body**: Standard (`14pt`), highly legible, standard weight.
- **Caption / Micro**: Small (`12pt`), muted color, used for metadata and timestamps.
- Ensure strict WCAG AA contrast compliance across the hierarchy.

---

## The Impeccable Detector Rules (Anti-Patterns to Reject)

If you see these patterns in existing code, flag them and fix them:
- ❌ **Legacy `<Frame>` Usage**: Never use `Frame`. It has buggy margins and shadows. Exclusively use `<Border>` with `StrokeShape`.
- ❌ **Nested Cards**: Do not nest `<Border>` cards inside other `<Border>` cards. Use visual separators or subtle background tints instead to flatten the Z-axis.
- ❌ **Untinted Grays & Pure Black**: Ban pure `#000000`, `#FFFFFF`, and neutral grays. Always tint grays with the primary palette (e.g., slate/zinc for a blue theme).
- ❌ **Unresponsive Touch Targets**: Flag any `Button` or tappable surface with `HeightRequest` under `44`.
- ❌ **Dead Interfaces**: Flag interactive controls missing `VisualStateManager` groups (`Normal`, `PointerOver`, `Pressed`).
- ❌ **Dated Easing**: Avoid "bounce" or "elastic" easing functions. Use modern, smooth timing.
- ❌ **Third-party Toolkits**: Never hallucinate or assume Telerik/Syncfusion namespaces in the final code. Implement their *design paradigms* using pure native MAUI.
