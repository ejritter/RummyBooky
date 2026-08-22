# XAML Anti-Pattern & Control Structure Audit Report (R4)

**Auditor**: Explorer 3 (Anti-Pattern & Control Structure Auditor)  
**Date**: 2026-08-05  
**Target Repository**: `c:\Dev\RummyBookyMaui`  

---

## Executive Summary

A comprehensive audit of all 16 source `.xaml` files across pages, views, controls, and styles in `c:\Dev\RummyBookyMaui` was conducted against the **Impeccable UI Detector Rules (R4)**:
- **R4a (Legacy `<Frame>` Usage)**: **PASSED** (0 `<Frame>` elements found; 100% `<Border>` migration).
- **R4b (Nested `<Border>` Cards)**: **FAILED (4 Violations)** (Nested Border card hierarchies detected in `PlayerCardView.xaml`, `GeneralPopupPage.xaml`, `LeaderboardPage.xaml`, and `NewGamePage.xaml`).
- **R4c (Missing VisualStateManager on Interactive Elements)**: **FAILED (3 Violations)** (Interactive elements missing `Normal`, `PointerOver`, `Pressed` states in `NewGamePage.xaml`).
- **R4d (Third-Party Toolkit Namespaces)**: **PASSED** (0 Telerik/Syncfusion third-party controls or namespaces; 100% native MAUI).

---

## Scope of Audited Files

1. `RummyBooky/App.xaml`
2. `RummyBooky/AppShell.xaml`
3. `RummyBooky/Pages/MainPage.xaml`
4. `RummyBooky/Pages/CurrentGamePage.xaml`
5. `RummyBooky/Pages/EditPlayerPage.xaml`
6. `RummyBooky/Pages/GeneralPopupPage.xaml`
7. `RummyBooky/Pages/LeaderboardPage.xaml`
8. `RummyBooky/Pages/NewGamePage.xaml`
9. `RummyBooky/Platforms/Windows/App.xaml`
10. `RummyBooky/Resources/Styles/Colors.xaml`
11. `RummyBooky/Resources/Styles/Dimensions.xaml`
12. `RummyBooky/Resources/Styles/Styles.xaml`
13. `RummyBooky/Resources/Styles/Theme.xaml`
14. `RummyBooky/Resources/Styles/Typography.xaml`
15. `RummyBooky/Views/CardBoxView.xaml`
16. `RummyBooky/Views/PlayerCardView.xaml`

---

## Detailed Violation Audit Findings

### Violation 1: R4b — Internal Nested `<Border>` Card Hierarchy
- **File**: `RummyBooky/Views/PlayerCardView.xaml`
- **Line Numbers**: Lines 12 & 49-62
- **Rule Violated**: R4b — Nested `<Border>` cards (flatten the Z-axis, avoid Border inside Border card hierarchies).
- **Code Snippet**:
```xaml
<!-- Line 12: Outer card border -->
<Border x:Name="CardBorder"
        Background="{StaticResource CardBackground}"
        Stroke="{StaticResource CardBorderColor}"
        StrokeShape="RoundRectangle 16"
        Padding="16">
    ...
    <!-- Line 49: Inner nested border for player name chip -->
    <Border x:Name="PlayerNameBorder"
            Grid.Column="0"
            Padding="24,8"
            Margin="8,0,0,0"
            StrokeShape="RoundRectangle 16"
            Background="{StaticResource AccentPrimary}"
            VerticalOptions="Fill">
        <Label x:Name="PlayerNameLabel"
               Style="{StaticResource PlayerLabel}"
               Text="{Binding PlayerName}"
               HorizontalOptions="Center"
               VerticalOptions="Center"
               TextColor="{StaticResource CardBackground}"/>
    </Border>
```
- **Description**: `<Border x:Name="PlayerNameBorder">` is nested inside outer card `<Border x:Name="CardBorder">`. This creates unnecessary Z-axis card layering within the reusable component.
- **Recommended XAML Fix**:
Replace `PlayerNameBorder` with a flat `<Grid>` or styled `<Label>` with background color:
```xaml
<Grid x:Name="PlayerNameChip"
      Grid.Column="0"
      Padding="24,8"
      Margin="8,0,0,0"
      BackgroundColor="{StaticResource AccentPrimary}"
      VerticalOptions="Fill">
    <Label x:Name="PlayerNameLabel"
           Style="{StaticResource PlayerLabel}"
           Text="{Binding PlayerName}"
           HorizontalOptions="Center"
           VerticalOptions="Center"
           TextColor="{StaticResource CardBackground}"/>
</Grid>
```

---

### Violation 2: R4b — Popup Container & Item Card Border Hierarchy
- **File**: `RummyBooky/Pages/GeneralPopupPage.xaml`
- **Line Numbers**: Lines 13 & 43-83
- **Rule Violated**: R4b — Nested `<Border>` cards (flatten the Z-axis, avoid Border inside Border card hierarchies).
- **Code Snippet**:
```xaml
<!-- Line 13: Outer Popup Border card -->
<Border Style="{StaticResource ThemeBorder}"
        Padding="16"
        Margin="16"
        HorizontalOptions="Center"
        VerticalOptions="Center"
        MaximumWidthRequest="450">
    <Grid RowDefinitions="Auto,Auto,*,Auto" RowSpacing="16">
        ...
        <!-- Line 43: Inner Item Border card inside CollectionView -->
        <CollectionView Grid.Row="2"
                        IsVisible="{Binding DisplayWinners}"
                        ItemsSource="{Binding WinningPlayers}">
            <CollectionView.ItemTemplate>
                <DataTemplate x:DataType="models:PlayerModel">
                    <Border x:Name="GridBorder"
                            Style="{StaticResource ThemeBorder}"
                            Padding="12"
                            Margin="4">
                        <VisualStateManager.VisualStateGroups>...</VisualStateGroupList>
                        <Grid x:Name="WinnerGrid" ColumnDefinitions="*" RowDefinitions="Auto">
                            <Label Grid.Column="0" Grid.Row="0"
                                   Text="{Binding PlayerName}"
                                   Style="{StaticResource PlayerLabel}" />
                        </Grid>
                    </Border>
                </DataTemplate>
            </CollectionView.ItemTemplate>
        </CollectionView>
    </Grid>
</Border>
```
- **Description**: The popup page itself is wrapped in an outer `<Border>` (Line 13). Inside the `CollectionView`, each winning player item is wrapped in another `<Border x:Name="GridBorder">` (Line 43). This creates a card inside a card elevation hierarchy.
- **Recommended XAML Fix**:
Flatten the CollectionView item template by using a `<Grid>` with visual state background changes instead of an inner `<Border>` card:
```xaml
<DataTemplate x:DataType="models:PlayerModel">
    <Grid x:Name="WinnerGrid"
          Padding="12"
          Margin="4"
          ColumnDefinitions="*"
          RowDefinitions="Auto">
        <VisualStateManager.VisualStateGroups>
            <VisualStateGroup x:Name="CommonStates">
                <VisualState x:Name="Normal">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="Transparent" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="PointerOver">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{StaticResource BackgroundSecondary}" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Pressed">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{StaticResource CardBackground}" />
                    </VisualState.Setters>
                </VisualState>
                <VisualState x:Name="Selected">
                    <VisualState.Setters>
                        <Setter Property="BackgroundColor" Value="{StaticResource AccentPrimary}" />
                    </VisualState.Setters>
                </VisualState>
            </VisualStateGroup>
        </VisualStateManager.VisualStateGroups>

        <Label Grid.Column="0" Grid.Row="0"
               Text="{Binding PlayerName}"
               Style="{StaticResource PlayerLabel}"
               VerticalOptions="Center"
               HorizontalOptions="Center" />
    </Grid>
</DataTemplate>
```

---

### Violation 3: R4b — Multi-Tier Border Hierarchy on Leaderboard Item Container
- **File**: `RummyBooky/Pages/LeaderboardPage.xaml`
- **Line Numbers**: Lines 53-91
- **Rule Violated**: R4b — Nested `<Border>` cards (flatten the Z-axis, avoid Border inside Border card hierarchies).
- **Code Snippet**:
```xaml
<!-- Line 53: Outer Item Border in Leaderboard CollectionView -->
<Border x:Name="RankItemBorder"
        Background="{StaticResource CardBackground}"
        Stroke="{StaticResource CardBorderColor}"
        StrokeShape="RoundRectangle 16"
        Padding="8">
    <Border.GestureRecognizers>
        <TapGestureRecognizer Tapped="OnRankItemTapped" Command="{Binding ...}" />
    </Border.GestureRecognizers>

    <!-- Line 89: Host for PlayerCardView, which contains its own inner Border (CardBorder) and inner-inner Border (PlayerNameBorder) -->
    <views:PlayerCardView AssignedPlayerModel="{Binding Player}"
                          Command="{Binding ...}" />
</Border>
```
- **Description**: `RankItemBorder` wraps `<views:PlayerCardView />`, creating a 3-tier deep nested `<Border>` structure (`RankItemBorder` -> `PlayerCardView.CardBorder` -> `PlayerCardView.PlayerNameBorder`).
- **Recommended XAML Fix**:
Remove the outer `RankItemBorder` container and attach the `TapGestureRecognizer` / VisualStateManager directly to `PlayerCardView` (or make `PlayerCardView` the root item):
```xaml
<DataTemplate x:DataType="models:LeaderboardPlayerModel">
    <views:PlayerCardView AssignedPlayerModel="{Binding Player}"
                          Command="{Binding Source={RelativeSource AncestorType={x:Type viewmodels:LeaderboardViewModel}}, Path=EditPlayerCommand}" />
</DataTemplate>
```

---

### Violation 4: R4b — Outer Border Wrapping PlayerCardView in Carousel
- **File**: `RummyBooky/Pages/NewGamePage.xaml`
- **Line Numbers**: Lines 143-174
- **Rule Violated**: R4b — Nested `<Border>` cards (flatten the Z-axis, avoid Border inside Border card hierarchies).
- **Code Snippet**:
```xaml
<!-- Line 143: Outer TagEntryBorder wrapping CarouselView -->
<Border Grid.Row="3"
        Grid.ColumnSpan="2"
        Style="{StaticResource TagEntryBorder}"
        IsVisible="{Binding ShowPlayerSuggestions}">
    <CarouselView x:Name="SuggestedPlayersCollection"
                  ItemsSource="{Binding FilteredPlayerModelsByName}">
        <CarouselView.ItemTemplate>
            <DataTemplate x:DataType="models:PlayerModel">
                <VerticalStackLayout Spacing="8" Padding="8,4">
                    ...
                    <!-- Line 168: PlayerCardView containing internal CardBorder -->
                    <views:PlayerCardView AssignedPlayerModel="{Binding .}" ... />
                </VerticalStackLayout>
            </DataTemplate>
        </CarouselView.ItemTemplate>
    </CarouselView>
</Border>
```
- **Description**: The `<CarouselView>` is wrapped inside a `<Border>` (`TagEntryBorder`), and each item in the carousel renders `<views:PlayerCardView>`, which has its own `<Border x:Name="CardBorder">`.
- **Recommended XAML Fix**:
Replace the outer `<Border>` around `<CarouselView>` with a layout container like `<Grid>`:
```xaml
<Grid Grid.Row="3"
      Grid.ColumnSpan="2"
      IsVisible="{Binding ShowPlayerSuggestions}">
    <CarouselView x:Name="SuggestedPlayersCollection" ...>
        ...
    </CarouselView>
</Grid>
```

---

### Violation 5: R4c — Missing VisualStateManager on Tappable StackLayout
- **File**: `RummyBooky/Pages/NewGamePage.xaml`
- **Line Numbers**: Lines 162-167
- **Rule Violated**: R4c — Missing `VisualStateManager` groups (`Normal`, `PointerOver`, `Pressed`) on interactive elements.
- **Code Snippet**:
```xaml
<!-- Line 162: Interactive container with double-tap gesture recognizer -->
<VerticalStackLayout Spacing="8" Padding="8,4">
    <VerticalStackLayout.GestureRecognizers>
        <TapGestureRecognizer NumberOfTapsRequired="2"
                                Command="{Binding Source={RelativeSource AncestorType={x:Type pages:NewGamePage}}, 
                                        Path=BindingContext.AddSuggestedPlayerCommand}" />
    </VerticalStackLayout.GestureRecognizers>
    <views:PlayerCardView AssignedPlayerModel="{Binding .}" ... />
</VerticalStackLayout>
```
- **Description**: The `VerticalStackLayout` is interactive (receives double-tap gestures to add a player), but lacks visual feedback via `VisualStateManager` (`Normal`, `PointerOver`, `Pressed`).
- **Recommended XAML Fix**:
Add `VisualStateManager` feedback for hover and press states:
```xaml
<VerticalStackLayout Spacing="8" Padding="8,4">
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="CommonStates">
            <VisualState x:Name="Normal">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="1.0" />
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="PointerOver">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="1.02" />
                    <Setter Property="Opacity" Value="0.95" />
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="Pressed">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="0.97" />
                    <Setter Property="Opacity" Value="0.85" />
                </VisualState.Setters>
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
    <VerticalStackLayout.GestureRecognizers>
        <TapGestureRecognizer NumberOfTapsRequired="2"
                              Command="{Binding Source={RelativeSource AncestorType={x:Type pages:NewGamePage}}, Path=BindingContext.AddSuggestedPlayerCommand}" />
    </VerticalStackLayout.GestureRecognizers>
    <views:PlayerCardView AssignedPlayerModel="{Binding .}" ... />
</VerticalStackLayout>
```

---

### Violation 6: R4c — Missing VisualStateManager on Interactive SwipeItemView (Delete Action)
- **File**: `RummyBooky/Pages/NewGamePage.xaml`
- **Line Numbers**: Lines 258-267
- **Rule Violated**: R4c — Missing `VisualStateManager` groups (`Normal`, `PointerOver`, `Pressed`) on interactive elements.
- **Code Snippet**:
```xaml
<!-- Line 258: Tappable SwipeItemView for Delete action -->
<SwipeItemView VerticalOptions="Center"
               HorizontalOptions="End"
               Command="{Binding Source={x:Reference thisPage}, Path=BindingContext.RemovePlayerCommand}"
               CommandParameter="{Binding .}">
    <Label VerticalOptions="Center" 
           Text="Delete"
           TextColor="{StaticResource AccentPrimary}"/>
</SwipeItemView>
```
- **Description**: The `SwipeItemView` triggers `RemovePlayerCommand` when tapped, but lacks `VisualStateManager` states (`Normal`, `PointerOver`, `Pressed`), leaving interaction feedback unresponsive.
- **Recommended XAML Fix**:
Add `VisualStateManager.VisualStateGroups` matching `CurrentGamePage.xaml`:
```xaml
<SwipeItemView VerticalOptions="Center"
               HorizontalOptions="End"
               Command="{Binding Source={x:Reference thisPage}, Path=BindingContext.RemovePlayerCommand}"
               CommandParameter="{Binding .}">
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="CommonStates">
            <VisualState x:Name="Normal">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="1.0"/>
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="PointerOver">
                <VisualState.Setters>
                    <Setter Property="Opacity" Value="0.8"/>
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="Pressed">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="0.95"/>
                </VisualState.Setters>
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
    <Label VerticalOptions="Center" 
           Text="Delete"
           TextColor="{StaticResource AccentPrimary}"/>
</SwipeItemView>
```

---

### Violation 7: R4c — Missing VisualStateManager on Interactive SwipeItemView (Dealer Action)
- **File**: `RummyBooky/Pages/NewGamePage.xaml`
- **Line Numbers**: Lines 271-280
- **Rule Violated**: R4c — Missing `VisualStateManager` groups (`Normal`, `PointerOver`, `Pressed`) on interactive elements.
- **Code Snippet**:
```xaml
<!-- Line 271: Tappable SwipeItemView for Dealer action -->
<SwipeItemView VerticalOptions="Center"
               HorizontalOptions="Start"
               Command="{Binding Source={x:Reference thisPage}, Path=BindingContext.SetPlayerAsDealerCommand}"
               CommandParameter="{Binding .}">
    <Label VerticalOptions="Center" 
           Text="Dealer"
           TextColor="{StaticResource AccentPrimary}"/>
</SwipeItemView>
```
- **Description**: The `SwipeItemView` triggers `SetPlayerAsDealerCommand` when tapped, but lacks `VisualStateManager` states (`Normal`, `PointerOver`, `Pressed`).
- **Recommended XAML Fix**:
Add `VisualStateManager.VisualStateGroups` matching `CurrentGamePage.xaml`:
```xaml
<SwipeItemView VerticalOptions="Center"
               HorizontalOptions="Start"
               Command="{Binding Source={x:Reference thisPage}, Path=BindingContext.SetPlayerAsDealerCommand}"
               CommandParameter="{Binding .}">
    <VisualStateManager.VisualStateGroups>
        <VisualStateGroup x:Name="CommonStates">
            <VisualState x:Name="Normal">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="1.0"/>
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="PointerOver">
                <VisualState.Setters>
                    <Setter Property="Opacity" Value="0.8"/>
                </VisualState.Setters>
            </VisualState>
            <VisualState x:Name="Pressed">
                <VisualState.Setters>
                    <Setter Property="Scale" Value="0.95"/>
                </VisualState.Setters>
            </VisualState>
        </VisualStateGroup>
    </VisualStateManager.VisualStateGroups>
    <Label VerticalOptions="Center" 
           Text="Dealer"
           TextColor="{StaticResource AccentPrimary}"/>
</SwipeItemView>
```

---

## Compliance Matrix Summary

| Category | Audited Files | Violations Found | Status | Key Action Required |
| --- | --- | --- | --- | --- |
| **R4a: Legacy `<Frame>` Usage** | 16 | 0 | **PASSED** | None (All migrated to `<Border>`) |
| **R4b: Nested `<Border>` Cards** | 16 | 4 | **ACTION REQ.** | Flatten Z-axis in `PlayerCardView.xaml`, `GeneralPopupPage.xaml`, `LeaderboardPage.xaml`, `NewGamePage.xaml` |
| **R4c: Missing VisualStateManager** | 16 | 3 | **ACTION REQ.** | Add `Normal`, `PointerOver`, `Pressed` states to interactive layout/swipe views in `NewGamePage.xaml` |
| **R4d: Third-Party Toolkits** | 16 | 0 | **PASSED** | None (100% native MAUI controls used) |
