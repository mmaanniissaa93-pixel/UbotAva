# UI Revamp - Phase 9 Chat + Map Migration

## Objective
Continue the runtime-only modernization rollout for `UBot.Chat` and `UBot.Map` while preserving existing packet/event behavior and in-game functionality.

## Implemented

### 1. `UBot.Chat` runtime modernization
- Updated `Plugins/UBot.Chat/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - theme-change subscription/unsubscription
  - runtime `ApplyModernTheme()` + `ApplyModernLayout()` hooks
- Applied semantic styling for:
  - chat tab shell and page surfaces
  - output RichText surfaces
  - message input controls
  - divider/separator lines
- Added responsive runtime layout for:
  - main tab viewport margins
  - private-message receiver/message row sizing
  - bottom input row sizing consistency

### 2. `UBot.Map` runtime modernization
- Updated `Plugins/UBot.Map/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - theme hooks (`ColorScheme.ThemeChanged`, resize, dispose)
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic styling for:
  - map tab shell and side panel surfaces
  - monster list and control rows
  - collision/unique options area
  - navmesh footer action hierarchy
- Added responsive runtime layout for:
  - split between minimap/navmesh host and right-side entity list panel
  - dynamic filter row and info text sizing
  - dynamic list column sizing
- Improved resize stability by re-allocating buffered graphics on map canvas size changes (`EnsureBufferedGraphics()`).

## Behavior safety
- No `.Designer.cs` changes.
- No config key changes.
- No event contract changes.
- Existing chat routing, map rendering, selection, and unique-check logic preserved.

## Build note
- Build script execution in this environment still depends on local `MSBuild.exe` path resolution in `build.ps1`.
