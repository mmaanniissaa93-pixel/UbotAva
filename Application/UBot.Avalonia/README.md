# UBot.Avalonia — Complete Electron/React → Avalonia UI Port

Full UI port of `UBot.Desktop` (Electron + React + TypeScript) to **Avalonia UI 11** (.NET 8).

---

## Project Structure

```
UBot.Avalonia/
├── Assets/
│   ├── ubot_banner_day.png          ✅ copied from original
│   └── ubot_banner_night.png        ✅ copied from original
│
├── Controls/                        ← 1-to-1 React component ports
│   ├── Sidebar          ← Sidebar.tsx       (grouped nav, banner, icon map)
│   ├── Topbar           ← Topbar.tsx        (status, lang/theme, divs, actions)
│   ├── TabStrip         ← TabStrip.tsx      (horizontal tab buttons)
│   ├── MetricCard       ← MetricCard.tsx    (stat tile with progress bar)
│   ├── ToggleSetting    ← ToggleSetting.tsx (label + toggle switch row)
│   └── CustomSelect     ← CustomSelect.tsx  (popup dropdown with icons)
│
├── Features/                        ← Feature views (plugin screens)
│   ├── General/    ← general module (SRO path, auto-login, pending, sound)
│   ├── Training/   ← training module (area, back, berserk, avoidance table)
│   ├── Protection/ ← protection module (recovery thresholds, back-to-town, pet)
│   ├── Map/        ← map module (canvas viewport, entity table, filters)
│   ├── Chat/       ← chat module (tabs, send bar, log stream)
│   ├── Log/        ← log + diagnostics (IPC metrics, filtered log)
│   ├── Skills/     ← skills module (stub — wire from state)
│   ├── Party/      ← party module (stub — wire from state)
│   ├── Alchemy/    ← alchemy module (stub — wire from state)
│   ├── Trade/      ← trade module (stub — wire from state)
│   ├── Lure/       ← lure module (stub — wire from state)
│   ├── Quest/      ← quest module (stub — wire from state)
│   ├── Inventory/  ← inventory module (stub — wire from state)
│   ├── Items/      ← items module (stub — wire from state)
│   ├── Statistics/ ← statistics module (stub — wire from state)
│   ├── TargetAssist/ ← target assist module (stub — wire from state)
│   ├── AutoDungeon/  ← auto dungeon module (stub — wire from state)
│   └── ServerInfo/   ← server info module (stub — wire from state)
│
├── Services/
│   ├── UbotBridgeService.cs  ← WebSocket IPC bridge (mirrors window.ubotBridge)
│   ├── MockUbotBridge.cs     ← offline/mock bridge for design-time
│   ├── RuntimeTypes.cs       ← all DTO types (RuntimeStatus, PluginDescriptor, …)
│   ├── AppState.cs           ← centralised reactive state (mirrors App.tsx useState)
│   └── BridgeWorker.cs       ← background event subscription + state polling loop
│
├── ViewModels/
│   ├── PluginViewModelBase.cs  ← shared base (BoolCfg/NumCfg/PatchConfig/PluginAction)
│   ├── GeneralViewModel.cs     ← general-plugin-specific VM
│   └── MainWindowViewModel.cs  ← app-level VM (translations, theme, section)
│
├── Styles/
│   ├── Theme.axaml     ← CSS :root vars → ResourceDictionary (dark + light)
│   ├── Controls.axaml  ← all control CSS classes → Avalonia Style Selectors
│   └── Features.axaml  ← section-panel, legacy-input, legacy-btn, etc.
│
├── App.axaml / .cs          ← entry point with DI wiring
├── MainWindow.axaml / .cs   ← shell (sidebar + workspace grid + full navigation)
├── FeatureViewFactory.cs    ← creates/caches feature views by plugin id
└── Program.cs
```

---

## Architecture

```
UbotBridgeService (WebSocket)
        │
        ▼
   BridgeWorker ──▶ AppState (ObservableObject)
        │                   │
        │            Plugins, LogLines,
        │            PlayerStats, Config
        │
   FeatureViewFactory ──creates──▶ Feature UserControls
        │                                   │
        │                           bound to PluginViewModelBase
        │                           which calls Bridge + State
        ▼
   MainWindow (shell)
   ├── Sidebar (nav)
   ├── Topbar (controls)
   └── ContentHost (active feature view)
```

---

## Getting Started

### 1. Reference this project
```
dotnet sln YourSolution.sln add UBot.Avalonia/UBot.Avalonia.csproj
```

### 2. Configure the WebSocket endpoint
In `App.axaml.cs`, change the URL to match your backend:
```csharp
var bridge = new UbotBridgeService("ws://localhost:7400/bridge");
```

### 3. Build and run
```bash
cd UBot.Avalonia
dotnet run
```

### 4. Expanding stub feature views
Each stub feature view has `Initialize(vm, state)` and `UpdateFromState(JsonElement)`.
The factory calls both — just fill in the AXAML layout and code-behind.

Example for SkillsFeatureView:
```csharp
public void Initialize(PluginViewModelBase vm, AppState state)
{
    _vm = vm;
    // set up TabStrip, bind lists from state
}
public void UpdateFromState(JsonElement moduleState)
{
    // parse moduleState.GetProperty("skills") and populate grids
}
```

---

## Component Mapping

| React (Electron)              | Avalonia                              |
|-------------------------------|---------------------------------------|
| `App.tsx` shell               | `MainWindow.axaml`                    |
| `Sidebar.tsx`                 | `Controls/Sidebar`                    |
| `Topbar.tsx`                  | `Controls/Topbar`                     |
| `TabStrip.tsx`                | `Controls/TabStrip`                   |
| `MetricCard.tsx`              | `Controls/MetricCard`                 |
| `ToggleSetting.tsx`           | `Controls/ToggleSetting`              |
| `CustomSelect.tsx`            | `Controls/CustomSelect`               |
| `styles.css :root`            | `Styles/Theme.axaml`                  |
| CSS class rules               | `Styles/Controls.axaml + Features.axaml` |
| `app-store.ts`                | `Services/AppState.cs`                |
| `ubotBridge` IPC              | `Services/UbotBridgeService.cs`       |
| IPC event subscription loop   | `Services/BridgeWorker.cs`            |
| `localization.ts`             | `TranslationBundle` in MainWindowViewModel |
| `renderGeneralFeature`        | `Features/General/GeneralFeatureView` |
| `renderTrainingSection`       | `Features/Training/TrainingFeatureView` |
| `renderProtectionSection`     | `Features/Protection/ProtectionFeatureView` |
| `renderMapFeature`            | `Features/Map/MapFeatureView`         |
| `renderChatSection`           | `Features/Chat/ChatFeatureView`       |
| `renderDiagnosticsFeature`    | `Features/Log/LogFeatureView`         |

---

## NuGet Packages

| Package                        | Version |
|--------------------------------|---------|
| Avalonia                       | 11.1.0  |
| Avalonia.Desktop               | 11.1.0  |
| Avalonia.Themes.Fluent         | 11.1.0  |
| Avalonia.Fonts.Inter           | 11.1.0  |
| Avalonia.Controls.DataGrid     | 11.1.0  |
| CommunityToolkit.Mvvm          | 8.3.2   |
