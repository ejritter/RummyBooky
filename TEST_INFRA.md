# Test Infrastructure & Plan: RummyBooky

## Test Philosophy
- Requirement-driven verification covering all user requirements R1, R2, and R3.
- Automated testing in `tests/RummyBooky.Tests` using xUnit.net v3 on `net10.0`.
- 100% pass requirement with 0 failures and 0 warnings on Windows build.

## Feature Inventory & Test Coverage Goals
| # | Feature | Requirement | Tier 1 (Isolated) | Tier 2 (Boundary/Edge) | Tier 3 (Cross-Feature) | Tier 4 (Full Workflow) |
|---|---------|-------------|:-----------------:|:----------------------:|:----------------------:|:----------------------:|
| 1 | In-game Previous Round Edit | R1 | 5 | 5 | ✓ | ✓ |
| 2 | Real-time Score & Metric Recomputation | R1 | 5 | 5 | ✓ | ✓ |
| 3 | EditGamePage Status & Winner Editing | R2 | 5 | 5 | ✓ | ✓ |
| 4 | Tie Correction & Manual Winner Selection | R2 | 5 | 5 | ✓ | ✓ |
| 5 | Score Limit Modification & Winner Detection | R2 | 5 | 5 | ✓ | ✓ |
| 6 | Lifetime Player Stats & Global Rankings Sync | R2 | 5 | 5 | ✓ | ✓ |
| 7 | Storage Persistence & JSON Serialization | R1, R2 | 5 | 5 | ✓ | ✓ |

## Test Execution Commands
- Unit Tests: `dotnet test tests/RummyBooky.Tests/RummyBooky.Tests.csproj`
- Windows Build: `dotnet build RummyBooky/RummyBooky.csproj -f net10.0-windows10.0.19041.0`
