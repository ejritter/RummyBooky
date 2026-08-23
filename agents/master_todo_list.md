# RummyBooky — Master Task Board & To-Do List

## 1. Recently Completed Tasks

- [x] **Continuous Gambler Soundtrack & Double-Tap Control**
  - Continuous looping playback of `the_gambler.mp3` on startup.
  - Double-tap logo gesture on `MainPage` toggling pause/resume or mute/unmute.
  - App lifecycle integration in `App.xaml.cs` (pause on sleep, resume on foreground).
  - Target files: `RummyBooky/Services/AppAudioService.cs`, `RummyBooky/ViewModels/MainPageViewModel.cs`, `RummyBooky/App.xaml.cs`, `tests/RummyBooky.Tests/GamblerAudioPlaybackTests.cs`.

- [x] **Popup Transparent Border Elimination**
  - Derived `BasePopupPage<TViewModel>` directly from `CommunityToolkit.Maui.Views.Popup`.
  - Set `Color="Transparent"` on popups and removed outer `Margin="24"` in `GeneralPopupPage.xaml`.
  - Set default `Border` stroke to transparent in `Styles.xaml` to eliminate ghost borders across all dialogs.
  - Enabled `CanBeDismissedByTappingOutsideOfPopup = true`.
  - Target files: `RummyBooky/Pages/BasePopupPage.cs`, `RummyBooky/Pages/GeneralPopupPage.xaml`, `RummyBooky/Resources/Styles/Styles.xaml`.

- [x] **Edit Player & Edit Game Confirmation Diffs & Single "Okay" Modals**
  - Pre-save confirmation diff modal showing before/after player name changes.
  - Pre-save confirmation diff modal showing all modified properties/scores when editing active games.
  - Success modals display only "Okay" / tap-to-dismiss without "Quit" or "Cancel" buttons.
  - Target files: `RummyBooky/ViewModels/EditPlayerViewModel.cs`, `RummyBooky/ViewModels/EditGameViewModel.cs`.

- [x] **Edit Player Placeholder Clipping Fix**
  - Reduced `TagEntry` font size from 25 to 16 and replaced fixed `WidthRequest="150"` with `HorizontalOptions="Fill"`.
  - Set `HorizontalTextAlignment="Center"` on `EntryNewPlayerName` in `EditPlayerPage.xaml`.
  - Target files: `RummyBooky/Resources/Styles/Styles.xaml`, `RummyBooky/Pages/EditPlayerPage.xaml`.

- [x] **Single Back Navigation Fix from Edit Player**
  - Removed duplicate `Command` / `CommandParameter` on `EditPlayerButton` in `PlayerCardView.xaml`.
  - Added navigation guard `_isNavigating` in `PlayerCardView.xaml.cs` and `_isNavigatingToEditPlayer` in `LeaderboardViewModel.cs` to prevent double-page pushing onto Shell navigation stack.
  - Target files: `RummyBooky/Views/PlayerCardView.xaml`, `RummyBooky/Views/PlayerCardView.xaml.cs`, `RummyBooky/ViewModels/LeaderboardViewModel.cs`.

---

## 2. Active & Backlog Tasks

- [ ] `[Low]` **Offline Data Export/Import**: Provide JSON or CSV backup/export options for lifetime player standings and game logs.
- [ ] `[Low]` **Custom Sound Effects**: Add optional sound effects for round completion and victory announcements.
