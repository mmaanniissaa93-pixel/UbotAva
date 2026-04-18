# UBOT - Agent Guide

Bu dosya, `C:\Users\auguu\Desktop\ubot-new` repository'si uzerinde calisan AI agent'lar icin operasyon kilavuzudur.

Kapsam: build, test, mimari, IPC, config, plugin/botbase gelistirme akisi.

---

## 1) Zorunlu Kurallar

- Derleme araci: `MSBuild` (x86).
- `dotnet build` kullanma.
- `dotnet` komutlari yalnizca paket/test gibi ihtiyaclar icin kullanilabilir (`dotnet restore`, `dotnet test`, `dotnet add`).
- Cozum dosyasi: `UBot.sln`.

Kullanici onayi olmadan dokunma:
- `.sln`
- `.csproj` / `.vcxproj`
- `*.Designer.cs`
- generated dosyalar (`Application\UBot.Desktop\src\generated\*`, `Application\UBot.Desktop\electron\generated\*`) - sadece ilgili sync workflow kapsaminda guncelle.

Git/safety:
- Kullanici tarafindan yapilan mevcut degisiklikleri geri alma.
- `git reset --hard` / `git checkout --` gibi yikici komutlar kullanma.

---

## 2) Canonical Build Komutlari

### Ana build

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug
```

Parametreler:
- `-Configuration Debug|Release`
- `-Clean`
- `-DoNotStart`
- `-SkipIconCacheRefresh`

Debug flag:
- `/p:Configuration=Debug /p:Platform=x86`

Release flag:
- `/p:Configuration=Release /p:Platform=x86`

### Electron UI (ilk kurulum)

```powershell
cd Application\UBot.Desktop
npm install
npm run build
```

Build fail durumunda:
- `build.log`

---

## 3) Cozum Haritasi (Guncel)

### Application
- `Application\UBot` - Ana exe + Desktop Bridge runtime
- `Application\UBot.Desktop` - Electron + React shell
- `Application\UBot.Updater` - updater exe

### Library
- `Library\UBot.Core`
- `Library\UBot.FileSystem`
- `Library\UBot.NavMeshApi`
- `Library\UBot.Loader.Library` (C++ detours DLL)

### Botbases
- `Botbases\UBot.Training`
- `Botbases\UBot.Alchemy`
- `Botbases\UBot.Trade`
- `Botbases\UBot.Lure`

### Plugins
- `Plugins\UBot.General`
- `Plugins\UBot.Skills`
- `Plugins\UBot.Protection`
- `Plugins\UBot.Party`
- `Plugins\UBot.Inventory`
- `Plugins\UBot.Items`
- `Plugins\UBot.Map`
- `Plugins\UBot.Statistics`
- `Plugins\UBot.Chat`
- `Plugins\UBot.Log`
- `Plugins\UBot.ServerInfo`
- `Plugins\UBot.CommandCenter`
- `Plugins\UBot.Quest` (runtime plugin id: `UBot.QuestLog`)
- `Plugins\UBot.AutoDungeon`
- `Plugins\UBot.PacketInspector`
- `Plugins\UBot.TargetAssist`

### Test/Tooling
- `Tests\UBot.Core.Tests`
- `tools\sync-ipc-contracts.ps1`
- `tools\tests\Test-IpcContractParity.ps1`
- `_ipc_inventory\09_test_strategy_three_layers.md`

---

## 4) Build Cikti Yapisi

- `Build\UBot.exe`
- `Build\UBot.Updater.exe`
- `Build\Client.Library.dll`
- `Build\Data\Bots\*.dll`
- `Build\Data\Plugins\*.dll`
- `Build\Data\Plugins\*.manifest.json`
- `Build\Data\Languages\*`
- `Build\Data\Scripts\*`
- `Build\User\*`

Notlar:
- Plugin manifest dosyalari build script tarafinda `plugin.manifest.json` => `<AssemblyName>.manifest.json` olarak kopyalanir.
- Build script runtime dosyalari eksikse `git restore Build/Data/Languages Build/Data/Scripts/Towns` dener.

---

## 5) Desktop Bridge Mimarisi

`UBot.exe` bootstrap:
1. `DesktopBridgeRuntime.Initialize()`
2. Pipe adi: `ubot.desktop.{pid}.{guid}`
3. `DesktopPipeBridgeServer` start
4. `DesktopShellLauncher` Electron baslatir (`UBOT_PIPE_NAME` env)
5. Electron `ubot:invoke` uzerinden komut gonderir, `ubot:event` ile event alir

### IPC Contract

Kaynak:
- `Application\UBot\DesktopBridge\DesktopBridgeIpcContracts.cs`

Generated:
- `Application\UBot.Desktop\src\generated\ipc-contracts.ts`
- `Application\UBot.Desktop\electron\generated\ipc-contracts.json`

Version:
- `ProtocolVersion = 3`
- `EventCatalogVersion = 1`

### Event backlog/backpressure
- responses: `4000`, `wait`
- high-frequency: `900`, `drop_newest`
- durable: `3000`, `wait`
- ui-only: `600`, `drop_oldest`

### Runtime resiliency
`DesktopBridgeRuntime` tarafinda:
- rate limiting
- circuit breaker
- idempotency cache
- failed-command history + replay
- operation telemetry + IPC metrics endpoint

---

## 6) DesktopBridgeRuntime Dosya Dagilimi (Guncel)

Dizin: `Application\UBot\DesktopBridge\`

- `DesktopBridgeRuntime.cs` (~2022 satir)
  - initialize, invoke, core lifecycle, diagnostics/rate-limit/circuit/idempotency
- `DesktopBridgeRuntime.Router.cs` (~56 satir)
  - command -> handler routing map
- `DesktopBridgeRuntime.Config.cs` (~3247 satir)
  - plugin/botbase config get/set
- `DesktopBridgeRuntime.State.cs` (~3262 satir)
  - status snapshot + plugin/botbase state payloadlari
- `DesktopBridgeRuntime.Actions.cs` (~1635 satir)
  - plugin/botbase action dispatcher
- `DesktopBridgeRuntime.Helpers.cs` (~683 satir)
  - helper parsing/mapping/resolve fonksiyonlari
- `DesktopBridgeIpcContracts.cs` (~476 satir)
  - command/event contract + schema catalog
- `DesktopPipeBridgeServer.cs` (~805 satir)
  - pipe listener, queueing, backpressure, publish latency metrics
- `DesktopShellLauncher.cs` (~83 satir)
  - Electron launch ve env wiring

---

## 7) Yeni IPC Komutu Ekleme Rehberi

1. `DesktopBridgeIpcContracts.cs` icine command sabitini ekle.
2. `AllCommands` dizisine ekle.
3. Gerekliyse idempotency/circuit/diagnostics listelerine ekle.
4. `DesktopBridgeRuntime.Router.cs` icinde handler map'e bagla.
5. Handler implementasyonunu uygun partial dosyada yaz (`Config/State/Actions/Runtime`).
6. UI contractlerini sync et:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\sync-ipc-contracts.ps1
```

7. Parity testi kos:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\tests\Test-IpcContractParity.ps1
```

---

## 8) Config Yonetimi

Bu kod tabaninda config key'ler runtime'da string key ile kullanilir.
- `GlobalConfig.Get/Set/...`
- `PlayerConfig.Get/Set/...`

Ornek:

```csharp
var enabled = PlayerConfig.Get("UBot.Party.EXPAutoShare", true);
PlayerConfig.Set("UBot.Party.EXPAutoShare", false);
```

Notlar:
- Central `ConfigKeys` static sinifi bulunmuyor.
- Key referans listesi: `ubot_keys.txt`
- Key governance scriptleri:
  - `powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1`
  - `powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory`
- Profil dosyalari:
  - `User\Profiles.rs`
  - `User\<Profile>.rs`
  - `User\<Profile>\<Character>.rs`

---

## 9) Plugin/Botbase Sozlesmesi

### IPlugin
- `Name`, `Title`, `Version`, `Enabled`
- `DisplayAsTab`, `Index`, `RequireIngame`
- `Initialize`, `Enable`, `Disable`, `Translate`, `OnLoadCharacter`

### IBotbase
- `Name`, `Title`, `Version`, `Enabled`
- `Area`
- `Start`, `Tick`, `Stop`

### Plugin manifest
Her plugin klasorunde `plugin.manifest.json` olmalidir.

Yukleme sirasinda `ExtensionManager`:
- manifest parse/validate
- host compatibility kontrolu
- dependency + capability kontrolu
- isolation mode (`inproc`/`outproc`) kaydi

---

## 10) Test Calisma Noktalari

### IPC contract parity

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\tests\Test-IpcContractParity.ps1
```

### Core test

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

Mevcut test odagi:
- `ScriptManagerValidationTests` (lint + dry-run davranisi)

---

## 11) Agent Uygulama Checklist

Kod degisikligi oncesi:
- Hedef dosyalar generated mi kontrol et.
- `DesktopBridgeRuntime` degisikliginde uygun partial dosyayi sec.

Kod degisikligi sonrasi:
- Gerekliyse IPC sync + parity test kos.
- En azindan etkilenen modulde compile path'i dogrula.
- Dokuman/contract drift olusuyorsa ayni PR'da duzelt.

Cevaplama tarzi:
- Bulgulari dosya/akıs referansli ver.
- Varsayimlari acik yaz.
- Build/test kosulamadiysa acikca belirt.

---

## 12) Faydalı Dosyalar

- `build.ps1`
- `build.log`
- `client-signatures.cfg`
- `ubot_keys.txt`
- `_ipc_inventory\09_test_strategy_three_layers.md`
- `Application\UBot.Desktop\README.md`
