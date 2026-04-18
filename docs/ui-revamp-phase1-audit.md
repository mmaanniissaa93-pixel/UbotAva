# UI Revamp - Phase 1 Audit

## Goal
Build a brand-new visual identity for UBot while preserving all existing behavior, plugin flow, and bot functions.

## Non-Negotiable Constraints
- Keep current feature set and workflows intact.
- Preserve plugin and botbase loading behavior.
- Do not edit auto-generated UI files (`*.Designer.cs`) unless explicitly approved.
- Theme and layout changes should be implemented through reusable UI components and shell-level composition.

## Current UI Architecture (Inventory)

### Main shell
- Main form: `Application/UBot/Views/Main.cs`
- Main designer: `Application/UBot/Views/Main.Designer.cs`
- Dynamic tab host: `windowPageControl` (`SDUI.Controls.WindowPageControl`, now integrated in `UBot.Core`)
- Primary action row: `btnStartStop`, `btnSave`, `comboDivision`, `comboServer`, `buttonConfig`
- Side/system containers: `pSidebar`, `pSidebarCustom`, `topCharacter`, `cosController`, `stripStatus`

### Runtime extension model
- Tab-based plugins are loaded into `windowPageControl`.
- Non-tab plugins are added under `menuPlugins` and opened as separate windows.
- Botbase view is swapped dynamically and inserted as a page in `windowPageControl`.

## Plugin Display Map

### DisplayAsTab = true
- `UBot.Chat`
- `UBot.General`
- `UBot.Inventory`
- `UBot.Items`
- `UBot.Log`
- `UBot.Map`
- `UBot.Party`
- `UBot.Protection`
- `UBot.Skills`
- `UBot.Statistics`

### DisplayAsTab = false
- `UBot.AutoDungeon`
- `UBot.CommandCenter`
- `UBot.PacketInspector`
- `UBot.Quest`
- `UBot.ServerInfo`

## Main UI Surface Files

### App-level views
- `Application/UBot/Views/Main.Designer.cs`
- `Application/UBot/Views/Main.cs`
- `Application/UBot/Views/PluginManager.Designer.cs`
- `Application/UBot/Views/ScriptRecorder.Designer.cs`
- `Application/UBot/Views/Updater.Designer.cs`

### Plugin tab views
- `Plugins/UBot.Chat/Views/Main.Designer.cs`
- `Plugins/UBot.CommandCenter/Views/Main.Designer.cs`
- `Plugins/UBot.General/Views/Main.Designer.cs`
- `Plugins/UBot.Inventory/Views/Main.Designer.cs`
- `Plugins/UBot.Items/Views/Main.Designer.cs`
- `Plugins/UBot.Log/Views/Main.Designer.cs`
- `Plugins/UBot.Map/Views/Main.Designer.cs`
- `Plugins/UBot.Party/Views/Main.Designer.cs`
- `Plugins/UBot.Protection/Views/Main.Designer.cs`
- `Plugins/UBot.Quest/Views/Main.Designer.cs`
- `Plugins/UBot.ServerInfo/Views/Main.Designer.cs`
- `Plugins/UBot.Skills/Views/Main.Designer.cs`
- `Plugins/UBot.Statistics/Views/Main.Designer.cs`

### Botbase views
- `Botbases/UBot.Alchemy/Views/Main.Designer.cs`
- `Botbases/UBot.Lure/Views/Main.Designer.cs`
- `Botbases/UBot.Trade/Views/Main.Designer.cs`
- `Botbases/UBot.Training/Views/Main.Designer.cs`

## Theme System Findings (Integrated UI)
- Central theme source: `Library/UBot.Core/UI/SDUI/ColorScheme.cs`
- Current model is mostly derived from one `BackColor`.
- `AccentColor` is currently static (`Color.FromArgb(0, 92, 252)`).
- Most custom controls already consume `ColorScheme` values, which is good for global re-skin.

## Redesign Impact (What to touch first)

### Low-risk, high-impact starting points
1. Expand `ColorScheme` from single-color logic to token-based palette (surface, text, border, accent, success, warning, danger).
2. Update integrated UI controls to consume semantic tokens (without changing behavior contracts).
3. Refresh shell composition in `Main.cs` and non-designer custom controls around page host and sidebar behavior.

### Keep stable during first implementation pass
- Plugin loading and tab/menu routing.
- Botbase switching flow.
- Event wiring and profile logic.

## Phase 2 Entry Criteria
- New design language approved (colors, type scale, spacing, corner radius, elevation).
- Token map defined in integrated UI layer.
- Migration strategy selected: incremental shell-first rollout, then plugin pages.
