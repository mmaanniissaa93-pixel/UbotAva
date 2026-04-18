# UI Revamp - Phase 13 Lure + Trade Migration

## Objective
Finalize the staged botbase modernization by applying runtime-only UI theming and responsive layout behavior to `UBot.Lure` and `UBot.Trade`, while preserving all existing automation logic.

## Implemented

### 1. `UBot.Lure` runtime modernization
- Updated `Botbases/UBot.Lure/Views/Main.cs` with:
  - modern UI lifecycle (`InitializeModernUi`, resize/theme/dispose hooks)
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic styling to:
  - location/settings cards and center panel framing
  - neutral action buttons and primary CTA emphasis (`Set center`)
  - script inputs, combo, and record link accent
- Added responsive behavior for:
  - two-column desktop composition
  - stacked single-column fallback
  - dynamic script input/button alignment
  - adaptive settings row alignment for numeric thresholds

### 2. `UBot.Trade` runtime modernization
- Updated `Botbases/UBot.Trade/Views/Main.cs` with:
  - modern UI lifecycle (`InitializeModernUi`, resize/theme/dispose hooks)
  - runtime `ApplyModernTheme()` and `ApplyModernLayout()`
- Applied semantic styling to:
  - tab surfaces/cards (`Route`, `Settings`, `Job overview`)
  - route list + context menu colors
  - action hierarchy for list management controls
  - route hint/link accenting
- Added responsive layout behavior for:
  - main tab container and bottom hint band
  - route tab controls, list viewport, and bottom panel
  - settings tab top toggles + card split/stack behavior
  - route list column auto-sizing

## Behavior safety
- No `.Designer.cs` changes.
- No config schema/key changes.
- No event contract changes.
- Route, trace, and trade logic preserved.

## Build note
- Build script execution in this environment still depends on local `MSBuild.exe` path resolution in `build.ps1`.
