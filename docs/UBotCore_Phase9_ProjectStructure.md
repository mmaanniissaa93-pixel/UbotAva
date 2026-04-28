# UBot.Core Phase 9 Project Structure Migration

This document is the Phase 9 output for splitting `Library/UBot.Core` into safer long-term project boundaries.

Phase 9 should not move files in one large change. `UBot.Core` is referenced by the main app, tests, 17 plugins, and 4 botbases. It also exposes plugin contracts that currently depend on WinForms through `IExtension.View`. A single-shot split would create high plugin/runtime risk.

## Current Inventory

`Library/UBot.Core` source distribution:

- `Network`: 122 files
- `Objects`: 102 files
- `Client`: 49 files
- `Components`: 22 files
- `Extensions`: 16 files
- `Plugins`: 9 files
- `Config`: 3 files
- `Event`: 2 files
- `Cryptography`: 2 files
- root runtime files: `Kernel`, `Game`, `Bot`, `Log`, `GameClientType`

Projects referencing `UBot.Core` directly:

- `Application/UBot`
- `Tests/UBot.Core.Tests`
- Botbases: `UBot.Alchemy`, `UBot.Lure`, `UBot.Trade`, `UBot.Training`
- Plugins: `UBot.AutoDungeon`, `UBot.Chat`, `UBot.CommandCenter`, `UBot.General`, `UBot.Inventory`, `UBot.Items`, `UBot.Log`, `UBot.Map`, `UBot.PacketInspector`, `UBot.Party`, `UBot.Protection`, `UBot.Quest`, `UBot.ServerInfo`, `UBot.Skills`, `UBot.Statistics`, `UBot.TargetAssist`

Important blockers:

- `IExtension.View` returns `System.Windows.Forms.Control`, so plugin contracts are still UI-framework-bound.
- `LanguageManager.Translate(Control)` is WinForms-specific.
- `DDSImage`, reference objects, and icon helpers use `System.Drawing`.
- `UBot.Core` currently targets `net8.0-windows` with `UseWindowsForms=true`.
- Plugin and botbase output/loading conventions depend on current project reference behavior.

## Target Project Shape

Recommended final layout:

- `UBot.Core.Abstractions`
  - packet interfaces: `IPacketHandler`, `IPacketHook`
  - extension contracts: `IPlugin`, `IBotbase`, `IExtension`
  - event contract primitives
  - shared enums and DTOs that do not depend on runtime state

- `UBot.Core.Runtime`
  - `Kernel`
  - `Game`
  - `Bot`
  - managers under `Components`
  - lifecycle orchestration

- `UBot.Core.Network`
  - `Packet`
  - `PacketManager`
  - socket/proxy/server/client
  - protocol/security code
  - built-in handlers/hooks

- `UBot.Core.Plugins`
  - `ExtensionManager`
  - plugin manifest loader
  - plugin repository/download
  - fault isolation
  - out-of-process host manager

- `UBot.GameData`
  - `ReferenceManager`
  - `ReferenceParser`
  - reference objects
  - PK2/DDS/reference data helpers

- `UBot.Core.WinFormsLegacy`
  - WinForms `Control` based plugin view adapter
  - `LanguageManager.Translate(Control)` or a compatibility shim
  - legacy UI helper code if it is reintroduced

## Migration Order

### Step 0 - Freeze Contract Surface

- Keep `UBot.Core` as the compatibility facade while new projects are introduced.
- Do not force plugins/botbases to reference the new projects immediately.
- Preserve runtime names and plugin manifest behavior.
- Keep x86 build assumptions.

Validation:

- `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart`
- `dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj`

### Step 1 - Add Abstractions Without Moving Runtime

Create `Library/UBot.Core.Abstractions`.

Move only dependency-light contracts first:

- `Network/PacketDestination.cs`
- minimal plugin metadata contracts after UI split is designed

Do not move `Network/IPacketHandler.cs` or `Network/IPacketHook.cs` in the first code split. They still reference the concrete `Packet` type, so moving them before `Packet` or a minimal packet abstraction would create an invalid dependency direction.

Important: `IExtension` cannot be moved cleanly until `Control View` is abstracted or kept in a WinForms compatibility package.

Recommended contract transition:

- Introduce a UI-neutral property such as `object ViewModelOrView` or a dedicated view adapter contract.
- Keep `Control View` as obsolete compatibility surface during one migration window.
- Update Avalonia bridge services to prefer UI-neutral data where possible.

### Step 2 - Split Plugin Infrastructure

Create `Library/UBot.Core.Plugins`.

Move after abstractions exist:

- `Plugins/PluginContractManifest.cs`
- `Plugins/PluginRepository.cs`
- `Plugins/PluginFaultIsolationManager.cs`
- `Plugins/PluginOutOfProcessHostManager.cs`
- `Plugins/DownloadProgressEventArgs.cs`
- later: `Plugins/ExtensionManager.cs`

Keep `ExtensionManager` until network contracts and runtime lifecycle are stable, because it touches packet registrations and runtime plugin state.

Required tests:

- manifest missing/name mismatch/version mismatch
- unsupported isolation mode
- dependency range/capability checks
- enable/disable handler ownership
- required out-of-proc validation

### Step 3 - Split Network

Create `Library/UBot.Core.Network`.

Move:

- `Network/Packet.cs`
- `Network/PacketManager.cs`
- `Network/AwaitCallback.cs`
- `Network/PacketReplayHarness.cs`
- `Network/Socket/**`
- `Network/Protocol/**`
- `Network/Handler/**`
- `Network/Hooks/**`

Expected dependency direction:

- `UBot.Core.Network` depends on `UBot.Core.Abstractions`.
- Runtime depends on Network.
- Plugins depend on Abstractions first, Network only when they need packet primitives.

Required tests:

- duplicate handler/hook registration
- hook packet drop behavior
- callback timeout cleanup
- proxy gateway/agent state transitions
- packet replay smoke tests

### Step 4 - Split Game Data

Create `Library/UBot.GameData`.

Move:

- `Client/ReferenceManager.cs`
- `Client/ReferenceParser.cs`
- `Client/ReferenceObjects/**`
- `Client/DDSImage.cs`
- `Client/GatewayInfo.cs`
- `Client/DivisionInfo.cs`
- reference-related icon/data helpers

This is likely the hardest non-runtime split because many plugins and Avalonia services consume reference objects directly.

Required tests:

- ref text lookup
- reference object parsing
- DDS/icon conversion smoke tests
- low-memory reference loading behavior

### Step 5 - Split Runtime

Create `Library/UBot.Core.Runtime`.

Move:

- `Kernel.cs`
- `Game.cs`
- `Bot.cs`
- `Components/**`
- `Config/**`
- `Event/**`
- `Log.cs`

`UBot.Core` should remain as a facade package/project temporarily, re-exporting or type-forwarding where practical.

Required tests:

- Kernel initialize/shutdown idempotency
- Clientless worker lifecycle
- config parser/save roundtrip
- event manager dispatch behavior

### Step 6 - WinForms Legacy Compatibility

Create `Library/UBot.Core.WinFormsLegacy` only after the new contracts exist.

Move or recreate:

- `LanguageManager.Translate(Control)`
- legacy WinForms view adapters
- any future replacement for old `ListViewExtensions`

Keep `UBot.Core` on `net8.0-windows` until this step is complete. Only then evaluate making core projects `net8.0`.

### Step 7 - Update Consumers Gradually

Recommended order:

1. Tests
2. Application/UBot.Avalonia
3. Application/UBot
4. Built-in plugins
5. Built-in botbases
6. External plugin compatibility notes

Each consumer migration should be a separate small change with build/test validation.

## Dependency Rules

Desired dependency direction:

```text
Application/UBot
Application/UBot.Avalonia
        |
        v
UBot.Core.Runtime
        |
        +--> UBot.Core.Network
        +--> UBot.Core.Plugins
        +--> UBot.GameData
                |
                v
        UBot.Core.Abstractions
```

Forbidden long-term dependencies:

- `UBot.Core.Abstractions` must not depend on WinForms, Avalonia, runtime managers, sockets, PK2 readers, or plugin host processes.
- `UBot.Core.Network` must not depend on Avalonia or WinForms.
- `UBot.GameData` must not depend on socket/proxy/runtime lifecycle.
- Plugin infrastructure must not directly own UI framework behavior.

## First Safe Code PR After This Document

The safest first code split is not moving `IExtension` yet. Start with pure packet abstractions:

1. Add `Library/UBot.Core.Abstractions`.
2. Move `PacketDestination`.
3. Reference `UBot.Core.Abstractions` from `UBot.Core`.
4. Add direct `UBot.Core.Abstractions` references to projects that consume `UBot.Core` public packet APIs.
5. Keep namespaces unchanged initially to reduce downstream edits.
6. Run the full canonical build and tests.

Do not move `Packet`, `IPacketHandler`, `IPacketHook`, `IPlugin`, `IBotbase`, or `IExtension` in the first structural PR.

`IPacketHandler` and `IPacketHook` still reference the concrete `Packet` type. Moving them before either moving `Packet` or introducing a small packet abstraction would create a circular dependency from `UBot.Core.Abstractions` back to `UBot.Core`.

## First Code Split Completed

The first Phase 9 code split has been applied:

- Added `Library/UBot.Core.Abstractions`.
- Moved `PacketDestination` to `UBot.Core.Abstractions` while preserving namespace `UBot.Core.Network`.
- Added a `ProjectReference` from `UBot.Core` to `UBot.Core.Abstractions`.
- Added direct `ProjectReference` entries from app/plugin/botbase/test projects that compile against `UBot.Core` packet APIs. This is required because `PacketDestination` appears on public API surfaces consumed outside `UBot.Core`.
- Added `UBot.Core.Abstractions` to the `Libraries` solution folder.

Next safe structural step:

1. Introduce a dependency-light packet interface, or move `Packet` and its minimal dependencies into `UBot.Core.Abstractions`/`UBot.Core.Network`.
2. Then move `IPacketHandler` and `IPacketHook`.
3. Keep namespace compatibility until all plugin/botbase projects are migrated intentionally.

## Validation Gate For Every Structural PR

Minimum:

1. `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart`
2. `dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj`
3. Plugin manifest copy/load check
4. Plugin enable/disable smoke check
5. Clientless and client mode smoke check when network/runtime code moves
6. Avalonia feature open smoke check when UI bridge or view contracts move

## Phase 9 Decision

Phase 9 is complete as a migration blueprint, dependency inventory, and first safe code split.

Further project splitting should continue as separate small phases. This avoids breaking plugin contracts, x86 build assumptions, and runtime plugin loading in a single high-risk change.
