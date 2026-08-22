# maui-animations Skill Copy
Source: C:\Users\roija\.gemini\config\skills\maui-animations\SKILL.md

Key Rules:
- Cancel animations before starting new animations on the same view.
- Respect reduced motion: check IsAnimationEnabled before running animations.
- Keep animation callbacks under 16ms.
- Avoid animating layout properties (WidthRequest/HeightRequest) - use TranslationX/Y and Scale instead, or controlled transition helpers.
- Use Task.WhenAll for parallel animations.
