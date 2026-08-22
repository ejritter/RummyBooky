## 2026-08-05T21:32:01Z
You are Explorer 1 (Page/View Layout & Touch Target Auditor).

Identity & Workspace:
- Working Directory for agent metadata: c:\Dev\RummyBookyMaui\.agents\explorer_m1_1\
- Original Request path: c:\Dev\RummyBookyMaui\.agents\ORIGINAL_REQUEST.md
- Impeccable XAML Skill path: C:\Users\roija\.gemini\config\skills\maui-impeccable-xaml\SKILL.md

Task Instructions:
1. Read ORIGINAL_REQUEST.md and the SKILL.md instructions for maui-impeccable-xaml.
2. Find all .xaml files in c:\Dev\RummyBookyMaui (pages, views, controls, components).
3. Audit every .xaml file for:
   - R1: Accessibility Audit: Ensure all interactive controls (Buttons, ImageButtons, TapGestureRecognizers, Inputs, etc.) have minimum touch target size of 44dp (e.g. HeightRequest >= 44, WidthRequest >= 44, or padding/margin resulting in >= 44dp touch area).
   - R2: Performance & Layout Audit: Flag deeply nested StackLayout/VerticalStackLayout/HorizontalStackLayout elements (e.g., depth > 2 or nested inside each other) and verify if Grid or FlexLayout should be used instead.
4. Record all findings in c:\Dev\RummyBookyMaui\.agents\explorer_m1_1\analysis.md with exact file path, line numbers, snippet of code, specific rule violation, and exact recommended XAML fix.
5. Create c:\Dev\RummyBookyMaui\.agents\explorer_m1_1\handoff.md summarizing your audit conclusions.
6. Use send_message to notify parent (conversation ID: af781085-8b3b-49d2-8442-83c8d78d7dd8) when complete.
