# UBot

UBot, Silkroad Online icin gelistirilmis moduler bir otomasyon platformudur.

Bu README, repodaki mevcut kaynak kod, cozum yapisi ve build ciktilari tersine muhendislik edilerek yeniden yazilmistir.

## 1) Teknoloji Ozeti

- Dil: C# ve C++ (native loader)
- Ana runtime: .NET 8 (`net8.0-windows` agirlikli)
- UI: Avalonia 11.1.0 (`Application/UBot.Avalonia`)
- Core: `Library/UBot.Core`
- Plugin/Botbase modeli: `IPlugin` ve `IBotbase`
- Hedef platform: x86

## 2) Gereksinimler

- Windows 10/11
- Visual Studio 2022
- MSBuild (VS ile gelir)
- .NET 8 SDK
- C++ Build Tools (Loader.Library icin)
- Git (build script runtime asset restore adimi icin)

## 3) Build ve Calistirma

Bu repo icin canonical build yolu `build.ps1` scriptidir.

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug
```

Yaygin parametreler:

- `-Configuration Debug|Release`
- `-Clean`
- `-DoNotStart`
- `-SkipIconCacheRefresh`

Ornekler:

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Release
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Clean -Configuration Debug
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart
```

`build.ps1` ozet akis:

1. `UBot.exe` ve `sro_client.exe` sureclerini sonlandirmayi dener.
2. Repodaki dosyalari `Unblock-File` ile MOTW flag'lerinden arindirir.
3. `UBot.sln` icin x86 MSBuild cagrisi yapar.
4. Gerekirse `UBot.TargetAssist` projesini tekil derler.
5. `Dependencies/*` icerigini `Build/Data` altina kopyalar.
6. `Plugins/*/plugin.manifest.json` dosyalarini `Build/Data/Plugins/<Assembly>.manifest.json` formatinda yazar.
7. Runtime data eksiginde `Build/Data/Languages` ve `Build/Data/Scripts/Towns` klasorlerini `git restore` ile toparlamayi dener.
8. Parametreye gore icon cache yeniler ve `UBot.exe` baslatir.

Log dosyasi: `build.log`

## 4) Cozum Haritasi

### Application

- `Application/UBot` : Ana executable (`UBot.exe`), CLI arguman parse, shutdown orchestration
- `Application/UBot.Avalonia` : Yeni UI katmani (View, ViewModel, service adapter)
- `Application/UBot.Updater` : `update_temp/update.zip` acip update uygulayan updater

### Library

- `Library/UBot.Core` : Kernel, network, event bus, plugin/botbase manager, config, scripting
- `Library/UBot.FileSystem` : Dosya sistemi/arsiv yardimcilari
- `Library/UBot.NavMeshApi` : NavMesh, dungeon, terrain, path altyapisi
- `Library/UBot.Loader.Library` : C++ detours tabanli native `Client.Library.dll`

### Botbases

- `UBot.Training`
- `UBot.Alchemy`
- `UBot.Trade`
- `UBot.Lure`

### Plugins

- `UBot.General`
- `UBot.Chat`
- `UBot.CommandCenter`
- `UBot.Inventory`
- `UBot.Items`
- `UBot.Log`
- `UBot.Map`
- `UBot.Party`
- `UBot.Protection`
- `UBot.Quest` (runtime id: `UBot.QuestLog`)
- `UBot.ServerInfo`
- `UBot.Skills`
- `UBot.Statistics`
- `UBot.TargetAssist`
- `UBot.AutoDungeon`
- `UBot.PacketInspector`

### Tests

- `Tests/UBot.Core.Tests` (xUnit)
- Mevcut test dosyasi: `ScriptManagerValidationTests.cs`

### Tools

- `tools/config/Export-UsedConfigKeyInventory.ps1`
- `tools/config/Test-ConfigKeyMigrations.ps1`
- `tools/config/config-key-migrations.json`
- `tools/tests` su an sadece README iceriyor (script yok)

## 5) Runtime Baslatma Akisi

`Application/UBot/Program.cs`:

1. CLI argumanlari parse edilir (`--character`, `--profile`, `--launch-client`, `--launch-clientless`, plugin host argumanlari).
2. `ProcessLifetimeManager` ile child process cleanup politikasi devreye alinir.
3. `--plugin-host` modunda `PluginHostRuntime.Run(...)` ile tek plugin host process olarak calisir.
4. Normal modda `UBot.Avalonia.AvaloniaHost.Run(args)` cagrilir.
5. Kapanista bot/proxy/plugin host/config shutdown-save zinciri calisir.

## 6) CLI Argumanlari

Desteklenen argumanlar:

- `--character <name>`
- `--profile <name>`
- `--launch-client`
- `--launch-clientless`
- `--plugin-host`
- `--plugin-name <internal-plugin-name>`
- `--plugin-path <absolute-dll-path>`

`--plugin-host` modu out-of-proc plugin yasam dongusu icin kullanilir.

## 7) Plugin ve Botbase Mimarisi

### Interface katmani

- `IExtension`: ortak metadata + `Initialize/Enable/Disable/Translate/View`
- `IPlugin`: `DisplayAsTab`, `Index`, `RequireIngame`, `OnLoadCharacter`
- `IBotbase`: `Area`, `Start/Tick/Stop`

### ExtensionManager davranisi

`Library/UBot.Core/Plugins/ExtensionManager.cs` su akisi uygular:

1. `Build/Data/Plugins` ve `Build/Data/Bots` altindan assembly toplar.
2. Plugin manifest yukler (`PluginContractManifestLoader`).
3. Host version, dependency, capability ve isolation validasyonu yapar.
4. Gerekli pluginleri out-of-proc host manager'a register eder.
5. Enabled pluginlerde initialize/enable ve packet hook/handler kayitlarini yonetir.
6. Disabled plugin listesini `UBot.DisabledPlugins` key'i uzerinden config'e yazar.

### Isolation

- In-proc fault izolasyonu: `PluginFaultIsolationManager`
- Out-of-proc process izolasyonu: `PluginOutOfProcessHostManager`
- Restart policy: manifestte `restartPolicy` alanindan okunur

Kod tarafinda out-of-proc zorunlu pluginler:

- `UBot.PacketInspector`
- `UBot.AutoDungeon`

### Mevcut plugin manifest izolasyon durumu

- Outproc: `UBot.AutoDungeon`, `UBot.PacketInspector`
- Inproc: diger tum pluginler

## 8) UI (Avalonia) Teknik Durum

UI katmani `Application/UBot.Avalonia` altinda feature tabanli organize edilmistir.

- Ana shell: `MainWindow.axaml`
- Service bridge: `IUbotCoreService` ve `UbotCoreService`
- State store: `Services/AppState.cs`
- Feature factory: `FeatureViewFactory.cs`
- Styles: `Styles/Theme.axaml`, `Styles/Controls.axaml`, `Styles/Features.axaml`

Dikkat ceken UI teknik detaylari:

- Ana pencere boyutu sabit: `1440x900`
- Resize kapali (`CanResize=False`)
- Tema/language toggle ve custom topbar/sidebar bilesenleri var
- Plugin + botbase ekranlari feature view uzerinden dinamik uretiliyor
- UI core ile in-process service siniflari uzerinden haberlesiyor (ws tabanli degil)

## 9) Konfigurasyon Sistemi

Config depolama formati:

- Satir bazli `key{value}`
- `Config` sinifi (`Library/UBot.Core/Config/Config.cs`) tarafindan okunur/yazilir

Dosya yapisi:

- `Build/User/Profiles.rs`
- `Build/User/<Profile>.rs` (global profile config)
- `Build/User/<Profile>/<Character>.rs` (player config)

Ana static facade'ler:

- `GlobalConfig`
- `PlayerConfig`
- `ProfileManager`

Config key governance yardimcilari:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

## 10) Runtime Data ve Build Ciktilari

Temel ciktilar:

- `Build/UBot.exe`
- `Build/UBot.Updater.exe`
- `Build/UBot.Avalonia.dll`
- `Build/Client.Library.dll`
- `Build/Data/Bots/*.dll`
- `Build/Data/Plugins/*.dll`
- `Build/Data/Plugins/*.manifest.json`
- `Build/Data/Languages/**`
- `Build/Data/Scripts/Towns/*.rbs`

Repo tarafinda runtime data kaynagi:

- `Dependencies/Languages/**`
- `Dependencies/Scripts/Towns/*.rbs`

## 11) Test ve Dogrulama

Unit test calistirma:

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

Mevcut test kapsaminda:

- Script lint davranisi
- Script dry-run davranisi

## 12) Sorun Giderme

- Build fail olursa: `build.log` ve `Build/boot-error.log` kontrol edin.
- Plugin acilis/manifest hatasi: `Build/Data/Plugins/*.manifest.json` ile runtime plugin version/name uyumunu kontrol edin.
- Reference data yuklenmiyorsa: profile ayarlarinda Silkroad yolu ve `media.pk2` varligini kontrol edin.
- Out-of-proc plugin baslamiyorsa: `UBot.exe` ve ilgili plugin DLL path'lerinin `Build` altinda oldugunu kontrol edin.

## 13) Lisans

- `LICENSE` (GPL-3.0)
- `AGREEMENT.md`
