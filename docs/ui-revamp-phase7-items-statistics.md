# UI Revamp - Phase 7 Items + Statistics Migration

## Objective
Continue the runtime-only modernization approach for `UBot.Items` and `UBot.Statistics` without changing existing behavior or designer-generated files.

## Implemented

### 1. `UBot.Items` runtime modernization
- Updated `Plugins/UBot.Items/Views/Main.cs` with:
  - `InitializeModernUi()`
  - theme-change subscription/unsubscription
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic theme styling for:
  - main tab shell and tab pages
  - shopping/filter list surfaces
  - section cards and bottom bars
  - primary/secondary action buttons
- Added responsive runtime layout for:
  - main tab viewport margins
  - shopping split area and general setup row flow
  - shopping header row adaptation for compact widths
  - sell-filter action bar and search row
  - pickup-settings checkbox flow/wrapping
  - dynamic list column widths

### 2. `UBot.Statistics` runtime modernization
- Updated `Plugins/UBot.Statistics/Views/Main.cs` with:
  - `InitializeModernUi()`
  - runtime theme hooks
  - responsive `ApplyModernLayout()`
- Added adaptive layout behavior:
  - vertical split on wide widths
  - horizontal split on narrower widths
  - dynamic filter-card heights
  - dynamic statistics column widths
  - reset button alignment in bottom bar
- Applied semantic theme styling to cards, list surface, and action button hierarchy.

## Behavior safety
- No `.Designer.cs` changes.
- No event contract changes.
- No settings key changes.
- Existing filtering, shopping, pickup, and statistics logic preserved.

## Build note
- Local build script still could not run in this environment because `build.ps1` could not resolve `MSBuild.exe`.
