# UI Revamp - Phase 12 Alchemy + Training Migration

## Objective
Continue runtime-only UI modernization for botbase pages `UBot.Alchemy` and `UBot.Training` while preserving all existing automation behavior.

## Implemented

### 1. `UBot.Alchemy` runtime modernization
- Updated `Botbases/UBot.Alchemy/Views/Main.cs` with:
  - modern UI lifecycle (`InitializeModernUi`, resize/theme/dispose hooks)
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic styling to:
  - item card and settings card surfaces
  - settings header strip
  - list/tab surfaces and text
  - combo and refresh affordance
- Added responsive layout for:
  - top split (`Item` + settings area)
  - bottom log viewport
  - dynamic log column widths
  - item-info section resizing

### 2. `UBot.Training` runtime modernization
- Updated `Botbases/UBot.Training/Views/Main.cs` with:
  - modern UI lifecycle (`InitializeModernUi`, resize/theme/dispose hooks)
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic styling to:
  - all major cards (`Area`, `Avoidance`, `Back to training`, `Berserk`, `Advanced`)
  - avoidance list/context menu
  - action button hierarchy and link accents
- Added responsive layout behavior:
  - two-column composition on wide windows
  - single-column stacked composition on narrow windows
  - adaptive walkback row controls
  - dynamic avoidance list column sizing

## Behavior safety
- No `.Designer.cs` changes.
- No config key changes.
- No event contract changes.
- Alchemy and training logic preserved.

## Build note
- Build script execution in this environment still depends on local `MSBuild.exe` path resolution in `build.ps1`.
