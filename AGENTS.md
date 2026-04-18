# UBOT - Agent Guide

Bu dosya, `C:\Users\auguu\Desktop\UbotAva` repository'si uzerinde calisan AI agent'lar icin operasyon kilavuzudur.

Kapsam: build, test, mimari, config, plugin/botbase gelistirme akisi.

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

---

## 3) Cozum Haritasi (Guncel)

### Application
- `Application\UBot` - Ana exe (net8.0-windows, WinForms bootstrap)
- `Application\UBot.Avalonia` - Avalonia UI katmani (net8.0-windows)
- `Application\UBot.Updater` - updater exe

### Library
- `Library\UBot.Core` - Core runtime, Kernel, Game, Network, Plugins
- `Library\UBot.FileSystem` - Dosya sistemi yardimcilari
- `Library\UBot.NavMeshApi` - NavMesh collision detection
- `Library\UBot.Loader.Library` - C++ detours DLL (vcxproj)

### Botbases
- `Botbases\UBot.Training` - leveling bot
- `Botbases\UBot.Alchemy` - alchemy bot
- `Botbases\UBot.Trade` - trade bot
- `Botbases\UBot.Lure` - lure bot

### Plugins (16 adet)
- `Plugins\UBot.General` - baglanti, login, client control
- `Plugins\UBot.Skills` - skill yonetimi
- `Plugins\UBot.Protection` - HP/MP pot, ölme, envanter kontrolü
- `Plugins\UBot.Party` - party yonetimi
- `Plugins\UBot.Inventory` - envanter islemleri
- `Plugins\UBot.Items` - item yonetimi
- `Plugins\UBot.Map` - harita, NavMesh entity render
- `Plugins\UBot.Statistics` - istatistikler
- `Plugins\UBot.Chat` - chat islemleri
- `Plugins\UBot.Log` - log görüntüleyici
- `Plugins\UBot.ServerInfo` - server bilgileri
- `Plugins\UBot.CommandCenter` - komut merkezi
- `Plugins\UBot.Quest` (runtime plugin id: `UBot.QuestLog`)
- `Plugins\UBot.AutoDungeon` - auto dungeon
- `Plugins\UBot.PacketInspector` - packet inceleyici
- `Plugins\UBot.TargetAssist` - target yardimcisi

### Test/Tooling
- `Tests\UBot.Core.Tests` - Core unit testleri
- `tools\config\Export-UsedConfigKeyInventory.ps1` - config key envanteri
- `tools\config\Test-ConfigKeyMigrations.ps1` - key migration testi

---

## 4) Build Cikti Yapisi

- `Build\UBot.exe` - Ana executable
- `Build\UBot.Updater.exe` - Updater
- `Build\UBot.Avalonia.dll` - UI katmani
- `Build\Client.Library.dll` - ClientLibrary native wrapper
- `Build\Data\Bots\*.dll` - Botbase assemblyler
- `Build\Data\Plugins\*.dll` - Plugin assemblyler
- `Build\Data\Plugins\*.manifest.json` - Plugin manifestler
- `Build\Data\Languages\*` - Dil dosyalari
- `Build\Data\Scripts\*` - Script dosyalari
- `Build\User\*` - Kullanici profilleri

Notlar:
- Plugin manifest dosyalari build script tarafindan `plugin.manifest.json` => `<AssemblyName>.manifest.json` olarak kopyalanir.
- Build script runtime dosyalari eksikse `git restore Build/Data/Languages Build/Data/Scripts/Towns` dener.

---

## 5) Mimari (Avalonia UI)

### Bootstrap Akisi

```
UBot.exe (WinForms entry)
    |
    +-> Program.Main()
    |       |
    |       +-> AvaloniaHost.Run(args)
    |               |
    |               +-> App.OnFrameworkInitializationCompleted()
    |                       |
    |                       +-> MainWindow (creates UbotCoreService)
    |                               |
    |                               +-> UbotCoreService (IBotCoreService)
    |
    +-> Kernel.Initialize() (UBot.Core)
            |
            +-> Bot instance
            +-> Network handlers/hooks
            +-> ExtensionManager.LoadAssemblies<IPlugin>()
            +-> ExtensionManager.LoadAssemblies<IBotbase>()
```

### Katmanlar

1. **UBot.exe** (Bootstrap)
   - WinForms Application (net8.0-windows)
   - Command-line parsing
   - Profile management
   - Final shutdown sequence

2. **UBot.Avalonia** (UI)
   - Avalonia 11.1.0 (Fluent theme)
   - MVVM with CommunityToolkit.Mvvm
   - Feature-based views (Features\Training, Protection, Map, etc.)
   - ViewModels: MainWindowViewModel, PluginViewModelBase

3. **UBot.Core** (Runtime)
   - Kernel (static) - Proxy, Bot, Language, LaunchMode
   - Game - Player, ReferenceManager, Clientless, Started, Ready
   - ExtensionManager - Plugin/Botbase loading
   - ClientManager - Client lifecycle
   - Proxy - Network proxy
   - GlobalConfig / PlayerConfig - Settings

---

## 6) Config Yonetimi

Bu kod tabaninda config key'ler runtime'da string key ile kullanilir.

- `GlobalConfig.Get/Set/...` - Genel ayarlar (profile bazli degil)
- `PlayerConfig.Get/Set/...` - Karakter bazli ayarlar

Ornek:

```csharp
var enabled = PlayerConfig.Get("UBot.Protection.checkUseHPPotionsPlayer", true);
GlobalConfig.Set("UBot.SilkroadDirectory", path);
PlayerConfig.Set("UBot.Area.Region", (ushort)region);
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

## 7) Plugin/Botbase Sozlesmesi

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

Ornek (`Plugins\UBot.General\plugin.manifest.json`):

```json
{
    "schemaVersion": 1,
    "pluginName": "UBot.General",
    "pluginVersion": "1.0.0",
    "capabilities": ["connection-control", "client-launch", "session-bootstrap"],
    "dependencies": [],
    "hostCompatibility": { "minVersion": "1.0.0", "maxVersionExclusive": "2.0.0" },
    "isolation": {
        "mode": "inproc",
        "tier": "critical",
        "restartPolicy": { "enabled": true, "maxRestarts": 2, "windowSeconds": 60 }
    }
}
```

Yukleme sirasinda `ExtensionManager`:
- manifest parse/validate
- host compatibility kontrolu
- dependency + capability kontrolu
- isolation mode (`inproc`/`outproc`) kaydi

---

## 8) Test Calisma Noktalari

### Core test

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

Mevcut test odagi:
- `ScriptManagerValidationTests` (lint + dry-run davranisi)

---

## 9) Agent Uygulama Checklist

Kod degisikligi oncesi:
- Hedef dosyalar generated mi kontrol et (yoksa zaten yok)
- UI katmani degisikliginde `Application\UBot.Avalonia\` altina bak

Kod degisikligi sonrasi:
- Etkilenen modulde compile path'i dogrula
- Yeni plugin ekleme: csproj olustur, UBot.sln'e ekle, manifest json ekle
- Yeni botbase ekleme: csproj olustur, UBot.sln'e ekle

Cevaplama tarzi:
- Bulgulari dosya/akis referansli ver.
- Varsayimlari acik yaz.
- Build/test kosulamadiysa acikca belirt.

---

## 10) Faydali Dosyalar

- `build.ps1`
- `build.log`
- `client-signatures.cfg`
- `ubot_keys.txt`
- `Application\UBot.Avalonia\README.md` (mevcutsa)