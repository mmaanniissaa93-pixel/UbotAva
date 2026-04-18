# UI Revamp - Phase 3 Shell Modernization

## Objective
Transform the main shell layout into a new-product feel while preserving all existing behavior.

## Implemented in this phase

### 1. Runtime shell layout engine
- Added runtime layout orchestration in `Application/UBot/Views/Main.cs`:
  - `InitializeModernShell()`
  - `ApplyModernShellLayout()`
  - `ApplyModernWindowChrome()`
  - `ApplyModernContainers()`
  - `ApplyCommandBarLayout()`

### 2. Modern window chrome styling
- Updated UIWindow chrome from `Main` runtime settings:
  - `TitleHeight = 40`
  - `TitleTabDesingMode = Chromed`
  - Disabled hard title border
  - Applied semantic gradient and text colors
  - Applied semantic border color

### 3. Main container modernization
- Updated shell structure visuals (without altering logic):
  - `windowPageControl` now has modern internal spacing and subtle surface background.
  - `topCharacter` resized for better header rhythm.
  - `pSidebar` widened and given semantic border/surface treatment.
  - `bottomPanel` resized and restyled as a modern action bar.

### 4. Adaptive action bar layout
- Implemented responsive bottom action row placement at runtime:
  - Right cluster: `START BOT`, `Save`
  - Left cluster: `Division`, `Server`, `IP Bind`
- Layout recomputes on size changes for stable spacing.

### 5. Theme + layout integration
- `RefreshTheme()` now reapplies shell layout after theme changes.
- `ConfigureSidebar()` and `Main_Resize()` now trigger layout refresh for consistent composition.

## Behavior safety
- No plugin loading flow changed.
- No botbase logic changed.
- No event contract changed.
- No `.Designer.cs` edits.

## Next phase candidate (Phase 4)
- Migrate highest-traffic plugin pages to new spacing/section patterns (start with `UBot.General`), preserving control behavior and bindings.
