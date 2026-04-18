# UI Revamp - Phase 6 Protection + Skills Migration

## Objective
Apply the same runtime-only modernization pattern to `UBot.Protection` and `UBot.Skills` while preserving all existing behavior and event wiring.

## Implemented

### 1. `UBot.Protection` runtime modernization
- Updated `Plugins/UBot.Protection/Views/Main.cs` with:
  - `InitializeModernUi()`
  - `ApplyModernTheme()`
  - `ApplyModernLayout()`
- Added runtime theme integration:
  - subscribe to `ColorScheme.ThemeChanged`
  - reapply theme/layout on theme changes
  - unsubscribe on dispose
- Added responsive placement logic:
  - two-column layout on wide windows
  - single-column stacked layout on narrower windows
  - dynamic `AutoScrollMinSize` for small viewports
- Preserved all protection logic and settings persistence paths.

### 2. `UBot.Skills` runtime modernization
- Updated `Plugins/UBot.Skills/Views/Main.cs` with:
  - `InitializeModernUi()`
  - runtime theme + layout hooks
  - responsive tab shell layout (`tabControl1` + `tabControl2`)
- Added runtime card/list modernization:
  - card radius/shadow profile alignment
  - list surfaces/text colors from semantic theme tokens
  - icon/action button visual hierarchy
- Added adaptive inner reflow for:
  - attacking/buffing skill cards
  - advanced setup cards
  - player-skill filter row
  - list column resizing based on current viewport
- Preserved all skill handling behavior, packet/event flows, and existing config semantics.

## Behavior safety
- No `.Designer.cs` changes.
- No event contract or persistence key changes.
- No automation logic changes, only runtime UI presentation/layout code.

## Build note
- Local build script could not run in this environment because `MSBuild.exe` path was not available to `build.ps1`.
