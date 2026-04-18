# UI Revamp - Phase 8 Party + Log Migration

## Objective
Complete phase 8 by applying the same runtime-only modernization approach to `UBot.Party` and `UBot.Log`, preserving existing behavior and settings flows.

## Implemented

### 1. `UBot.Party` runtime modernization
- Updated `Plugins/UBot.Party/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - theme-change subscription/unsubscription
  - `ApplyModernTheme()` + `ApplyModernLayout()` runtime hooks
- Applied semantic styling for:
  - tab shell surfaces
  - party cards/panels/list surfaces
  - primary, destructive, and neutral button hierarchy
- Added responsive runtime layout for:
  - auto-party split sizing
  - party-matching top and bottom action bars
  - buffing tab proportional panels
  - dynamic list column widths
- Auto-join settings panel now uses runtime expanded/collapsed state (`_joinConfigExpanded`) without designer edits.

### 2. `UBot.Log` runtime modernization
- Updated `Plugins/UBot.Log/Views/Main.cs` with:
  - `InitializeModernUi()` lifecycle
  - theme hooks (`ColorScheme.ThemeChanged`, resize, dispose)
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied modern visual treatment for:
  - elevated top toolbar card
  - semantic text and log-surface colors
  - primary clear action button
- Added adaptive toolbar flow:
  - visible filter checkboxes (`Enabled/Debug/Normal/Warning/Error`) now wrap responsively
  - log viewport keeps consistent margins and minimum readable height

## Behavior safety
- No `.Designer.cs` changes.
- No config key changes.
- No event contract changes.
- Existing party/log logic and persistence behavior preserved.

## Build note
- Build script execution in this environment still depends on local `MSBuild.exe` path resolution in `build.ps1`.
