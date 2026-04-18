# UBot

UBot, Silkroad Online icin gelistirilmis moduler bir otomasyon platformudur.

Bu README, dogrudan kaynak koddan cikarilan guncel teknik durumu anlatir.
- Cekirdek: C# (`net8.0-windows`, x86)
- Masaustu kabuk: Avalonia UI (.NET 8)
- Core <-> UI iletisimi: in-process servis katmani (`IUbotCoreService`)

## 1) Gereksinimler

### Windows / Build
- Windows 10 veya 11
- Visual Studio 2022
  - `.NET desktop development`
  - `Desktop development with C++` (Loader.Library + Detours icin)
- MSBuild (Visual Studio ile gelir)
- Git (build script runtime asset restore adiminda kullanilir)

## 2) Build ve Calistirma

Onemli:
- `dotnet build` kullanmayin.
- Cozum sadece `MSBuild` ile derlenir.

### Ana build script

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug
```

Yaygin secenekler:
- `-Configuration Debug|Release`
- `-Clean` (Build klasorunu temizler, `Build\User` altini gecici tasiyip geri koyar)
- `-DoNotStart` (derler, `UBot.exe` baslatmaz)
- `-SkipIconCacheRefresh`

Ornekler:

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Release
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Clean -Configuration Debug
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart
```

Build script ek olarak su islemleri yapar:
- `UBot.exe` ve `sro_client.exe` sureclerini sonlandirmaya calisir
- Repo dosyalarindan Mark-of-the-Web flaglerini kaldirir (`Unblock-File`)
- `Dependencies\*` icerigini `Build\Data\` altina kopyalar
- Plugin manifest dosyalarini `Build\Data\Plugins\<AssemblyName>.manifest.json` seklinde kopyalar
- Gerekli runtime dosyalari eksikse (`Build\Data\Languages\langs.rsl`, `Build\Data\Scripts\Towns\22106.rbs`) `git restore` ile geri getirir

Build log:
- `build.log`

## 3) Calisma Akisi (Runtime)

`UBot.exe` basladiginda:
1. Komut satiri argumanlari parse edilir.
2. Profil/karakter/launch modu argumanlari uygulanir.
3. Avalonia host baslatilir (`UBot.Avalonia.AvaloniaHost.Run`).
4. `IUbotCoreService` implementasyonu (`UbotCoreService`) core/runtime katmanina baglanir.
5. ViewModel katmani plugin/botbase etkileşimlerini servis metotlari uzerinden yonetir.

## 4) Komut Satiri Argumanlari (`UBot.exe`)

- `--character <name>`
- `--profile <name>`
- `--launch-client`
- `--launch-clientless`
- `--plugin-host`
- `--plugin-name <internalName>`
- `--plugin-path <absoluteDllPath>`

`--plugin-host` modu, out-of-proc plugin host olarak tek plugin calistirmak icin vardir.

## 5) UI/Core Service Yuzeyi

Avalonia tarafi dogrudan `IUbotCoreService` uzerinden core ile konusur.

Temel plugin akis metotlari:
- `InvokePluginActionAsync(...)`
- `SetPluginConfigAsync(...)`
- `GetPluginStateAsync(...)`

## 6) Cozum Haritasi

### Application
- `Application\UBot` - Ana exe + Avalonia host bootstrap
- `Application\UBot.Avalonia` - Avalonia UI (view, viewmodel, service adaptoru)
- `Application\UBot.Updater` - Zip tabanli guncelleme uygulayici

### Library
- `Library\UBot.Core` - Runtime motoru (network, event bus, plugin/botbase manager, script manager)
- `Library\UBot.FileSystem` - PK2 okuma katmani
- `Library\UBot.NavMeshApi` - NavMesh/terrain/pathfinding katmani
- `Library\UBot.Loader.Library` - C++ detours loader (`Client.Library.dll`)

### Botbases
- `UBot.Training`
- `UBot.Alchemy`
- `UBot.Trade`
- `UBot.Lure`

### Plugins
- `UBot.General`
- `UBot.Skills`
- `UBot.Protection`
- `UBot.Party`
- `UBot.Inventory`
- `UBot.Items`
- `UBot.Map`
- `UBot.Statistics`
- `UBot.Chat`
- `UBot.Log`
- `UBot.ServerInfo`
- `UBot.CommandCenter`
- `UBot.Quest` (runtime plugin adi: `UBot.QuestLog`)
- `UBot.AutoDungeon`
- `UBot.PacketInspector`
- `UBot.TargetAssist`

### Test / Tooling
- `Tests\UBot.Core.Tests` (xUnit)
- `_ipc_inventory\09_test_strategy_three_layers.md`

## 7) Plugin Contract ve Isolation

Her plugin icin `plugin.manifest.json` bulunur.

Manifestte:
- `pluginName`, `pluginVersion`
- `capabilities`
- `dependencies` (plugin + version/capability seviyesinde)
- `hostCompatibility`
- `isolation` (`inproc`/`outproc`, tier, restartPolicy)

`ExtensionManager` startup'ta:
1. DLL'leri yukler (`Build\Data\Plugins` / `Build\Data\Bots`)
2. Plugin manifestlerini dogrular
3. Dependency ve host version uyumlulugunu kontrol eder
4. Gerekirse out-of-proc host manager kaydi yapar

Ek not:
- Mevcut manifestlerde tum pluginler `inproc` olarak ayarli.
- Altyapi `outproc` host'u destekliyor (`--plugin-host` yolu hazir).

## 8) Konfigurasyon ve Profil Dosyalari

### Dosya yapisi
- `Build\User\Profiles.rs` - Profil listesi + secili profil
- `Build\User\<Profile>.rs` - Global config
- `Build\User\<Profile>\<Character>.rs` - Player config
- `Build\User\<Profile>\autologin.data` - hesap/giris datasi

### Format
`Config` sinifi satir bazli `key{value}` formati kullanir.

### Kaynaklar
- `GlobalConfig` => profile-level dosya (`<Profile>.rs`)
- `PlayerConfig` => character-level dosya (`<Profile>\<Character>.rs`)

### Anahtar referansi
- `ubot_keys.txt`

Not: Dosya iceriginde namespace/artefact satirlari da bulunur; tum satirlar dogrudan runtime key olmayabilir.

### Governance Scriptleri

Kullanilan key envanteri:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1
```

Migration check:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

Migration kurallari:
- `tools\config\config-key-migrations.json`

## 9) Build Cikti Yapisi

- `Build\UBot.exe`
- `Build\UBot.Updater.exe`
- `Build\Client.Library.dll`
- `Build\Data\Bots\*.dll`
- `Build\Data\Plugins\*.dll`
- `Build\Data\Plugins\*.manifest.json`
- `Build\Data\Languages\*`
- `Build\Data\Scripts\*`
- `Build\User\*`

## 10) Avalonia UI Gelistirme

Avalonia UI kaynaklari:
- `Application\UBot.Avalonia\*.axaml`
- `Application\UBot.Avalonia\*.axaml.cs`
- `Application\UBot.Avalonia\ViewModels\*`
- `Application\UBot.Avalonia\Services\IUbotCoreService.cs`
- `Application\UBot.Avalonia\Services\UbotCoreService.cs`

## 11) Test ve Dogrulama

### Core unit test

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

## 12) Sorun Giderme

- Build fail:
  - `build.log` son 100 satiri script tarafinda zaten yazdirilir.
- UI acilmiyor:
  - `Application\UBot.Avalonia` derlemesinin basarili oldugunu kontrol edin.
- Reference data yuklenmiyor:
  - Genel ayarlardan dogru Silkroad exe yolu verin.
  - `media.pk2` ve `data.pk2` dosyalari mevcut olmali.
- Plugin load validation fail:
  - `build\boot-error.log` ve plugin `.manifest.json` uyumunu kontrol edin.

## 13) Lisans

- `LICENSE` (GPL-3.0)
- `AGREEMENT.md`
