# UI Revamp - Phase 4 General Page Migration

## Objective
Migrate `UBot.General` to the new design language while preserving all existing functionality and event wiring.

## Implemented

### 1. Runtime theme integration (no designer edits)
- Updated `Plugins/UBot.General/Views/Main.cs` to apply semantic theme tokens at runtime.
- Added theme hooks:
  - subscribe to `ColorScheme.ThemeChanged`
  - reapply styles/layout on theme updates
  - safely unsubscribe on dispose

### 2. Responsive shell layout for General page
- Added runtime layout engine in `Main.cs`:
  - adaptive header row (path + client type + browse)
  - two-column layout on wide sizes
  - single-column stacked layout on narrow sizes
- Added dynamic `AutoScrollMinSize` to keep accessibility on small windows.

### 3. Section/card modernization
- Restyled all major section cards with updated radius/shadow profile:
  - Start game
  - Automated login
  - Client settings
  - Bot settings
  - Server pending
  - Sound notifications
- Button hierarchy updated to match new semantic actions:
  - primary action emphasis on `Start Client`
  - neutral secondary action styling for utility controls

### 4. Internal panel reflow
- `Automated login` internals now reflow with section width:
  - account/character dropdowns
  - captcha row controls
  - login-delay and wait-after-DC controls
- `Server pending` actions and thresholds re-position responsively.

### 5. Behavior safety
- No logic/event contracts changed.
- No `.Designer.cs` file changes.
- Existing handlers and config persistence remain intact.

## Next phase candidate (Phase 5)
- Apply the same migration pattern to `UBot.Inventory` and `UBot.Protection` pages for visual consistency across daily-use tabs.
