# UI Revamp - Phase 10 CommandCenter + Quest + ServerInfo Migration

## Objective
Apply the same runtime-only modernization pattern to `UBot.CommandCenter`, `UBot.Quest`, and `UBot.ServerInfo` while preserving all existing behavior and settings flow.

## Implemented

### 1. `UBot.CommandCenter` runtime modernization
- Updated `Plugins/UBot.CommandCenter/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - theme-change subscription/unsubscription
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic styling for:
  - tab shell/pages
  - emote action card panel
  - command description area
  - reset/save action hierarchy
- Added responsive runtime layout for:
  - tab viewport sizing
  - chat-commands and notifications tab content
  - bottom action row reflow (`Reset`, `Enable`, `Save`)

### 2. `UBot.Quest` runtime modernization
- Updated `Plugins/UBot.Quest/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - runtime theme hooks
  - responsive `ApplyModernLayout()`
- Applied semantic styling for:
  - quest tree surface
  - completion toggle row
  - quest context menu entries
- Added adaptive spacing/margins and scroll-safe layout for narrow windows.

### 3. `UBot.ServerInfo` runtime modernization
- Updated `Plugins/UBot.ServerInfo/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - runtime theme hooks
  - responsive list viewport layout
- Applied semantic styling for server list surface.
- Added dynamic server/capacity column sizing at runtime.

## Behavior safety
- No `.Designer.cs` changes.
- No config key changes.
- No event contract changes.
- Existing quest and command-center workflows preserved.

## Build note
- Build script execution in this environment still depends on local `MSBuild.exe` path resolution in `build.ps1`.
