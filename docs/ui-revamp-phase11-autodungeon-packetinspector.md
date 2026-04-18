# UI Revamp - Phase 11 AutoDungeon + PacketInspector Migration

## Objective
Apply the runtime-only modernization approach to `UBot.AutoDungeon` and `UBot.PacketInspector`, preserving all existing behavior and plugin data flow.

## Implemented

### 1. `UBot.AutoDungeon` runtime modernization
- Updated `Plugins/UBot.AutoDungeon/Views/AutoDungeonView.cs` with:
  - runtime theme lifecycle (`ColorScheme.ThemeChanged`, resize, dispose)
  - `ApplyModernTheme()` and `ApplyModernLayout()` hooks
- Added adaptive layout shell:
  - 3-column layout on wide widths
  - 2x2 layout on medium widths
  - single-column stacked layout on narrow widths
- Applied semantic surface/text styling to:
  - name ignore list section
  - type filter checked lists
  - monster counter list and status labels
  - action buttons and input controls
- Added dynamic list column sizing for counter rows.

### 2. `UBot.PacketInspector` runtime modernization
- Updated `Plugins/UBot.PacketInspector/Views/PacketInspectorView.cs` with:
  - runtime theme lifecycle (`ColorScheme.ThemeChanged`, resize, dispose)
  - `ApplyModernTheme()` and `ApplyModernLayout()` hooks
- Added responsive layout behavior:
  - wrapping top filter/action toolbar
  - adaptive packet table viewport sizing
  - fixed status row and dynamic content area
- Applied semantic styling to:
  - capture/filter controls
  - clear/export/copy action hierarchy
  - packet list surface and status text
- Added dynamic packet list column sizing at runtime.

## Behavior safety
- No `.Designer.cs` changes (these views are code-built).
- No config key changes.
- No packet parsing / capture logic changes.
- No dungeon automation logic changes.

## Build note
- Build script execution in this environment still depends on local `MSBuild.exe` path resolution in `build.ps1`.
