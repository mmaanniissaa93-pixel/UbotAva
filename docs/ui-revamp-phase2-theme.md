# UI Revamp - Phase 2 Theme Foundation

## Objective
Establish a brand-new visual language for UBot without changing existing bot behavior and plugin workflows.

## Implemented

### 1. Semantic theme system
- Added `Library/UBot.Core/UI/SDUI/ThemePalette.cs`.
- Introduced semantic tokens instead of single-color derivation:
  - window, surface, raised surface, subtle surface
  - text, muted text
  - outline, strong outline
  - primary + on-primary
  - success + on-success
  - warning, danger
  - shadow, status bar

### 2. Preset model
- Added `ThemePreset`:
  - `NovaDark`
  - `DawnLight`
  - `Seeded` (custom color driven)
- Added seeded palette generation to keep custom color mode working while producing a coherent UI palette.

### 3. Backward-compatible `ColorScheme`
- Refactored `Library/UBot.Core/UI/SDUI/ColorScheme.cs` to use semantic palette tokens.
- Kept compatibility fields (`BackColor`, `ForeColor`, `BorderColor`, `BackColor2`, `AccentColor`) so existing controls keep working.
- Added new semantic accessors for updated controls.

### 4. Core control skin migration
Updated integrated UI controls to consume semantic tokens:
- `Controls/Button.cs`
- `Controls/Panel.cs`
- `Controls/FlowLayoutPanel.cs`
- `Controls/GroupBox.cs`
- `Controls/TabControl.cs`
- `Renderers/MenuRenderer.cs`

### 5. Main shell integration
- Updated `Application/UBot/Views/Main.cs` to apply semantic shell colors (`ApplyShellTheme`).
- Theme menu actions now map to semantic presets.
- Updated `Application/UBot/Views/SplashScreen.cs` to initialize preset-based themes consistently.

## Behavior Safety
- Plugin loading, botbase selection, and runtime logic are unchanged.
- No `.Designer.cs` files were edited in this phase.

## Next Phase (Phase 3)
- Shell layout modernization pass (navigation hierarchy, spacing scale, typography rhythm).
- Plugin page-by-page visual migration (starting from `UBot.General`).
- Optional style variants pack (clean, tactical, compact).
