# UI Revamp - Phase 5 Inventory Page Migration

## Objective
Continue the same migration approach on `UBot.Inventory`: new design language, no behavior changes, and no designer-file edits.

## Implemented

### 1. Runtime UI modernization layer
- Updated `Plugins/UBot.Inventory/Views/Main.cs` with a runtime modernization stack:
  - `InitializeModernUi()`
  - `ApplyModernTheme()`
  - `ApplyModernLayout()`
  - `RefreshTabStyles()`

### 2. Theme token integration
- Inventory view now reacts to `ColorScheme.ThemeChanged`.
- Applied semantic surfaces, outline colors, primary/success accents, and text color hierarchy.
- Existing action semantics preserved:
  - selected tab = primary style
  - sort action = success style

### 3. Responsive inventory tab header
- Replaced fixed-position top buttons with runtime wrapped layout.
- Buttons now auto-flow to additional row(s) based on width, preserving tab order and click behavior.

### 4. Responsive bottom status bar
- Re-positioned free-slot indicator, sort button, and auto-sort toggle dynamically.
- Bottom bar kept behavior-identical while aligning with new shell spacing and token palette.

### 5. List area adaptation
- Inventory list columns now resize dynamically:
  - fixed widths for amount/rarity
  - flexible width for item name
- Existing inventory update logic untouched.

## Behavior safety
- Inventory data fetch/update logic unchanged.
- Item actions/context menu logic unchanged.
- No `.Designer.cs` changes.

## Next phase candidate (Phase 6)
- Apply same migration pattern to `UBot.Protection` and `UBot.Skills` pages for cross-tab consistency.
