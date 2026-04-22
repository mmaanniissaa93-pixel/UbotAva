# AGENTS Guide (UBot)

Bu dosya, [UbotAva](https://github.com/mmaanniissaa93-pixel/UbotAva) repository'sinde calisan AI agent ve contributor'lar icin operasyon rehberidir.

Amac: degisikliklerin build/runtime uyumlulugunu korumak, plugin kontratlarini bozmamak, UI-core entegrasyonunu stabil tutmak.

## 1) Kesin Kurallar

- Cozum derlemesi icin canonical yol: `build.ps1`.
- Cozumu `dotnet build UBot.sln` ile degil, MSBuild/x86 akisi ile derleyin.
- Platform varsayimi x86'dir; csproj/loader ayarlari buna baglidir.
- Kullanici degisikliklerini geri alma (`reset --hard`, `checkout --`) yasak.
- `*.Designer.cs`, `.sln`, `*.csproj`, `*.vcxproj` dosyalarina sadece zorunlu ise dokunun.
- Plugin manifest ve runtime plugin metadata (`Name`, `Version`) tutarliligini bozmayin.

## 2) Hizli Komut Referansi

Ana build:

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug
```

Temiz build:

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Clean -Configuration Debug -DoNotStart
```

Unit test:

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

Config key envanter/migration:

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

## 3) Mimari Yol Haritasi

### Application

- `Application/UBot`: process entrypoint, CLI options, shutdown orchestration
- `Application/UBot.Avalonia`: aktif masaustu UI
- `Application/UBot.Updater`: update zip uygulayici

### Core ve Library

- `Library/UBot.Core`: Kernel, Game, network handlers/hooks, extension manager
- `Library/UBot.FileSystem`: dosya/arsiv yardimcilari
- `Library/UBot.NavMeshApi`: navmesh/pathfinding
- `Library/UBot.Loader.Library`: native detours DLL (`Client.Library.dll`)

### Extension katmani

- Botbase DLL hedefi: `Build/Data/Bots`
- Plugin DLL hedefi: `Build/Data/Plugins`
- Manifestler: `Plugins/<PluginName>/plugin.manifest.json`

## 4) Plugin ve Botbase Gelistirme Kurallari

### Plugin eklenecekse

1. `Plugins/<YeniPlugin>/<YeniPlugin>.csproj` olustur.
2. `IPlugin` implement eden public class ekle.
3. `plugin.manifest.json` olustur ve runtime metadata ile eslestir.
4. Output path'i `Build/Data/Plugins` olacak sekilde ayarla.
5. `UBot.sln` icine projeyi ekle.
6. Gerekirse Avalonia tarafinda `Features/<YeniFeature>` ve service mapping ekle.

### Botbase eklenecekse

1. `Botbases/<YeniBot>/<YeniBot>.csproj` olustur.
2. `IBotbase` implement eden class ekle.
3. Output path'i `Build/Data/Bots` olacak sekilde ayarla.
4. `UBot.sln` icine ekle.
5. `UbotCoreService.GetPluginsAsync()` botbase listesine dahil olmasi gerektigini unutma.

### Manifest uyumluluk

`PluginContractManifestLoader` asagidaki tutarliliklari zorlar:

- `pluginName` == runtime `IPlugin.Name`
- `pluginVersion` == runtime `IPlugin.Version`
- `isolation.mode` sadece `inproc` veya `outproc`
- dependency + capability kontrolleri
- hostCompatibility version range kontrolleri

Zorunlu out-of-proc pluginler:

- `UBot.PacketInspector`
- `UBot.AutoDungeon`

Bu pluginlerde `isolation.mode` degeri `outproc` kalmalidir.

## 5) UI Degisikliklerinde Dikkat

- UI shell `MainWindow.axaml` sabit 1440x900 pencere varsayimina gore tasarlanmis.
- Yeni feature eklerken `FeatureViewFactory` ve ilgili service/config/state katmanlarini birlikte guncelleyin.
- UI, core ile `IUbotCoreService` uzerinden in-process bagli calisir; dogrudan core static sinif cagrilarini view code-behind'e dagitmayin.
- Tema/language gibi global davranislari `MainWindowViewModel` + `AppState` modeliyle uyumlu tutun.

## 6) Config ve Veri Kurallari

- Config formati: satir bazli `key{value}`.
- Profile dosyalari `Build/User` altinda tutulur.
- Yeni config key ekliyorsaniz naming'i `UBot.<Module>.<Key>` kalibinda tutun.
- Migration gerekiyorsa `tools/config/config-key-migrations.json` guncellemesini degerlendirin.

## 7) Build Cikti ve Runtime Asset Notlari

- `Dependencies/**` build sirasinda `Build/Data/**` altina kopyalanir.
- Build script gerekli runtime data eksiginde `git restore Build/Data/Languages Build/Data/Scripts/Towns` dener.
- Plugin manifestleri build sonunda `Build/Data/Plugins/<AssemblyName>.manifest.json` olarak yazilir.

## 8) Test ve Validation Beklentisi

Kod degisikligi tamamlandiginda minimum:

1. Build komutu calismali (`build.ps1`).
2. Etkilenen alan core ise unit test calisimi kontrol edilmeli.
3. Plugin/botbase degisikliginde load/enable/disable akisi gozden gecirilmeli.
4. UI degisikliginde feature acilisi ve temel aksiyon butonlari manuel test edilmeli.

## 9) Dokumantasyon Politikasi

Asagidaki degisikliklerde README/AGENTS guncellemesi beklenir:

- Yeni plugin/botbase ekleme veya silme
- Plugin isolation/dependency model degisikligi
- Build script davranis degisikligi
- UI shell veya service bridge mimarisinde buyuk degisim
- Config depolama veya migration model degisikligi

## 10) Sık Hata Kaynakları

- Manifest dosyasi var ama runtime DLL yanina kopyalanmamis olmasi
- Plugin `Name` ve manifest `pluginName` mismatch
- x86 yerine farkli platform hedefi ile build denemek
- `build.ps1` atlanip dogrudan farkli build yolu kullanmak
- UI feature eklendiginde service/action router baglantilarinin unutulmasi

