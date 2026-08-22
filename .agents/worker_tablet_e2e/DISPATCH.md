## 2026-08-22T00:44:27Z
You are the Specialist Worker for Android Build, Deployment, and Live Physical Tablet E2E Verification for RummyBooky.

Working directory: c:\Dev\RummyBookyMaui\.agents\worker_tablet_e2e (Create your directory and metadata files here).
Authoritative request: c:\Dev\RummyBookyMaui\.agents\ORIGINAL_REQUEST.md

MANDATORY INTEGRITY WARNING:
DO NOT CHEAT. All implementations must be genuine. DO NOT hardcode test results, create dummy/facade implementations, or circumvent the intended task. A forensic auditor will independently verify your work. Integrity violations WILL be detected and your work WILL be rejected.

Your Mission:
1. Verify all automated unit tests in tests/RummyBooky.Tests pass cleanly (0 errors) using `dotnet test tests/RummyBooky.Tests/RummyBooky.Tests.csproj`.
2. Inspect and confirm `RummyBooky/Pages/GeneralPopupPage.xaml` has the updated dealer selection popup styling (theme-aware background, shadow elevation, dealer icon badge header, player selection cards).
3. Build and package the signed Release APK for Android (`net10.0-android`) using `dotnet publish RummyBooky/RummyBooky.csproj -f net10.0-android -c Release` (or `dotnet build RummyBooky/RummyBooky.csproj -f net10.0-android -c Release`).
4. Connect to the physical Google Pixel Tablet at `10.0.0.66:45305` via `adb connect 10.0.0.66:45305`.
5. Install the APK to user profile 0 on the tablet using `adb -s 10.0.0.66:45305 install -r --user 0 <apk_path>`.
6. Launch the app `com.companyname.rummybooky` on user profile 0 on the Pixel tablet.
7. Execute a complete live E2E walkthrough on the physical tablet, capturing PNG screenshots via adb (`adb -s 10.0.0.66:45305 exec-out screencap -p > ...`) or maui-devflow tools for each step:
   - Step A: MainPage & New Game page with players "Brodie" and "Renegade".
   - Step B: Dealer selection popup (`GeneralPopupPage.xaml`) showing theme-aware dialog and selected dealer.
   - Step C: Navigation to `CurrentGamePage` showing immediate rendering of Brodie and Renegade with dealer badge, running total score 0, and interactive round score entry.
   - Step D: Entering Round 1 scores (e.g. 50 for Brodie, 0 for Renegade), tapping "Calculate Scores", advancing to Round 2, updating totals, rotating dealer badge clockwise to Renegade, and disk persistence.
   - Step E: Navigating back to Round 1 via ◀ (Previous Round button), editing score, and verifying dynamic total score recalculation.
   - Step F: Navigating to `EditGamePage`, verifying game management (status, winner tie resolution, score limit, round score editing).
8. Save all screenshots in `c:\Dev\RummyBookyMaui\.agents\worker_tablet_e2e\screenshots\`.
9. Document all command outputs, test results, device logs, and screenshot paths in `c:\Dev\RummyBookyMaui\.agents\worker_tablet_e2e\handoff.md` and report back when finished.
