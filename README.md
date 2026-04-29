# UBot — Silkroad Online Otomasyon Platformu

> Modüler, izolasyonlu plugin mimarisi · Avalonia 11 UI · .NET 8 · x86

[![Platform](https://img.shields.io/badge/Platform-Windows%2010%2F11-blue?logo=windows)](https://github.com/mmaanniissaa93-pixel/UbotAva)
[![.NET](https://img.shields.io/badge/.NET-8.0-purple?logo=dotnet)](https://dotnet.microsoft.com/)
[![UI](https://img.shields.io/badge/UI-Avalonia%2011-brightgreen)](https://avaloniaui.net/)
[![License](https://img.shields.io/badge/License-GPL--3.0-red)](LICENSE)
[![Build](https://img.shields.io/badge/Build-build.ps1%20%2F%20MSBuild%20x86-orange)](build.ps1)

---

## İçindekiler

- [Proje Nedir?](#proje-nedir)
- [Teknoloji Özeti](#teknoloji-özeti)
- [Gereksinimler](#gereksinimler)
- [Kurulum ve Build](#kurulum-ve-build)
- [Çözüm Haritası](#çözüm-haritası)
- [Botbase Katmanı](#botbase-katmanı)
- [Plugin Katmanı](#plugin-katmanı)
- [Plugin Manifest Sistemi](#plugin-manifest-sistemi)
- [Konfigurasyon Sistemi](#konfigurasyon-sistemi)
- [Runtime Başlatma Akışı](#runtime-başlatma-akışı)
- [CLI Argümanları](#cli-argümanları)
- [UI Mimarisi (Avalonia)](#ui-mimarisi-avalonia)
- [Native Loader (C++)](#native-loader-c)
- [Test ve Doğrulama](#test-ve-doğrulama)
- [Sorun Giderme](#sorun-giderme)
- [Lisans](#lisans)

---

## Proje Nedir?

**UBot**, Silkroad Online için geliştirilmiş, tamamen modüler bir otomasyon platformudur. Temel özellikler:

- **Plugin/Botbase ayrımı:** Oyun içi her işlev bağımsız bir plugin veya botbase olarak yüklenir/kaldırılır.
- **İzolasyon modelleri:** In-process fault izolasyonu ve out-of-process process izolasyonu desteklenir.
- **Güncel UI:** Avalonia 11 tabanlı masaüstü arayüzü (WPF'den bağımsız, cross-runtime hazır).
- **Native entegrasyon:** C++ Detours tabanlı `Client.Library.dll` ile Silkroad client'ına hook atar.
- **Güvenli konfigurasyon:** Satır bazlı `key{value}` formatında profil + karakter bazlı config yönetimi.

---

## Teknoloji Özeti

| Katman | Teknoloji |
|--------|-----------|
| Ana Dil | C# (.NET 8, `net8.0-windows`) |
| Native Layer | C++ (Detours — `Client.Library.dll`) |
| UI Framework | Avalonia 11.1.0 |
| Platform Hedefi | x86 (tüm projeler) |
| Build Sistemi | MSBuild via `build.ps1` (Visual Studio 2022) |
| Test Framework | xUnit |
| Config Formatı | Satır bazlı `key{value}` (`.rs` uzantılı dosyalar) |

---

## Gereksinimler

| Gereksinim | Detay |
|------------|-------|
| İşletim Sistemi | Windows 10 / 11 |
| Visual Studio | 2022 (Community / Professional / Enterprise / Preview) |
| .NET SDK | 8.0 |
| MSBuild | VS 2022 ile birlikte gelir |
| C++ Build Tools | `UBot.Loader.Library` (native DLL) için gerekli |
| Git | Build script'i runtime asset restore adımında kullanır |

> **Not:** `build.ps1`, vswhere üzerinden VS 2022 Preview sürümlerini de tanır. Insider/Preview sürümü kullanıyorsanız ek yapılandırma gerekmez.

---

## Kurulum ve Build

### Hızlı Başlangıç

```powershell
# Debug build (varsayılan)
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug

# Release build
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Release

# Temiz build (User verilerini koruyarak)
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Clean -Configuration Debug

# Build sonrası UBot.exe'yi başlatma
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart
```

### Build Script Parametreleri

| Parametre | Varsayılan | Açıklama |
|-----------|------------|----------|
| `-Configuration` | `Debug` | `Debug` veya `Release` |
| `-Clean` | `$false` | Build klasörünü sıfırlar (User verileri korunur) |
| `-DoNotStart` | `$false` | Build sonrası `UBot.exe` otomatik başlatılmaz |
| `-SkipIconCacheRefresh` | `$false` | Icon cache yenileme adımını atlar |

### Build Akışı (build.ps1 özeti)

```
1. UBot.exe ve sro_client.exe sonlandırılır (varsa)
2. Repo dosyaları Unblock-File ile MOTW flag'lerinden arındırılır
3. MSBuild x86 → UBot.sln derlenir  →  build.log
4. UBot.TargetAssist DLL eksikse ayrıca derlenir
5. Dependencies/* → Build/Data/ altına kopyalanır
6. Plugins/*/plugin.manifest.json → Build/Data/Plugins/<Assembly>.manifest.json
7. Runtime data eksikse git restore ile Languages/Scripts/Towns toparlanır
8. (isteğe bağlı) Icon cache yenilenir → UBot.exe başlatılır
```

### Build Çıktıları

```
Build/
├── UBot.exe                          ← Ana executable
├── UBot.Updater.exe                  ← Otomatik güncelleyici
├── UBot.Avalonia.dll                 ← UI katmanı
├── Client.Library.dll                ← Native hook DLL (C++)
└── Data/
    ├── Bots/                         ← Botbase DLL'leri
    │   ├── UBot.Training.dll
    │   ├── UBot.Alchemy.dll
    │   ├── UBot.Trade.dll
    │   └── UBot.Lure.dll
    ├── Plugins/                      ← Plugin DLL'leri + manifest'ler
    │   ├── UBot.General.dll / .manifest.json
    │   ├── UBot.Protection.dll / .manifest.json
    │   └── ...
    ├── Languages/                    ← Dil dosyaları
    └── Scripts/Towns/                ← Town script'leri (.rbs)
```

---

## Çözüm Haritası

```
UbotAva/
├── Application/
│   ├── UBot/                     ← Ana executable, CLI, process lifecycle
│   ├── UBot.Avalonia/            ← Masaüstü UI (Avalonia 11)
│   └── UBot.Updater/             ← update_temp/update.zip uygulayıcı
│
├── Library/
│   ├── UBot.Core/                ← Kernel, event bus, plugin manager, scripting
│   ├── UBot.Core.Abstractions/   ← IPlugin, IBotbase, paylaşılan kontratlar
│   ├── UBot.Core.Common/         ← Yardımcı tipler ve extensions
│   ├── UBot.Core.Domain/         ← Domain DTO'ları, game-state kontratları
│   ├── UBot.Core.GameState/      ← Oyun durumu modeli (karakter, NPC, item)
│   ├── UBot.Core.Network/        ← Network altyapısı, event'ler
│   ├── UBot.Core.Services/       ← Uygulama servisleri
│   ├── UBot.FileSystem/          ← Yerel + PK2 dosya sistemi (Blowfish şifreleme)
│   ├── UBot.GameData/            ← RefObj*, RefSkill*, RefQuest* parser'ları
│   ├── UBot.NavMeshApi/          ← NavMesh, dungeon, terrain, path altyapısı
│   ├── UBot.Protocol/            ← SRO paket okuyucu/yazıcıları
│   └── UBot.Loader.Library/      ← C++ Detours tabanlı native DLL
│
├── Botbases/
│   ├── UBot.Training/
│   ├── UBot.Alchemy/
│   ├── UBot.Trade/
│   └── UBot.Lure/
│
├── Plugins/
│   ├── UBot.General/             ← Bağlantı kontrolü, client başlatma
│   ├── UBot.Protection/          ← HP/MP/pet koruma (tier: critical)
│   ├── UBot.Skills/              ← Skill rotasyonu, buff yönetimi
│   ├── UBot.Items/               ← Item filtresi, autoloot politikası
│   ├── UBot.Inventory/           ← Envanter takip ve aksiyonları
│   ├── UBot.Party/               ← Party otomasyonu, eşleşme
│   ├── UBot.Chat/                ← Chat gönderme, event'ler
│   ├── UBot.CommandCenter/       ← Komut yönlendirme, emote komutları
│   ├── UBot.Map/                 ← Dünya haritası, spawn görselleştirme
│   ├── UBot.Statistics/          ← Runtime metrikleri, oturum istatistikleri
│   ├── UBot.Log/                 ← Log akışı, temizleme
│   ├── UBot.ServerInfo/          ← Sunucu inceleme
│   ├── UBot.Quest/               ← Görev takibi (runtime id: UBot.QuestLog)
│   ├── UBot.TargetAssist/        ← Hedef yardımı (tier: standard)
│   ├── UBot.AutoDungeon/         ← Dungeon routing/control (out-of-proc)
│   └── UBot.PacketInspector/     ← Paket yakalama/inceleme (out-of-proc)
│
├── Tests/
│   └── UBot.Core.Tests/          ← xUnit test projesi
│
├── Dependencies/
│   ├── Languages/                ← Kaynak dil dosyaları
│   └── Scripts/                  ← Town script kaynakları (.rbs)
│
├── tools/
│   ├── config/                   ← Config key envanteri ve migration araçları
│   └── tests/
│
├── build.ps1                     ← Canonical build scripti
├── UBot.sln                      ← Visual Studio solution
├── client-signatures.cfg         ← Client imza tanımları
└── ubot_keys.txt                 ← Key referansları
```

---

## Botbase Katmanı

Botbase'ler `IBotbase` interface'ini implemente eder. Her botbase bağımsız bir oyun döngüsü stratejisidir.

| Botbase | Açıklama |
|---------|----------|
| `UBot.Training` | Standart grinding / levelling |
| `UBot.Alchemy` | Efsun / alchemy otomasyonu |
| `UBot.Trade` | Ticaret rota ve işlem yönetimi |
| `UBot.Lure` | Monster çekme stratejisi |

**IBotbase kontratı:**
```csharp
// Library/UBot.Core.Abstractions
interface IBotbase : IExtension
{
    string Area { get; }
    void Start();
    void Tick();
    void Stop();
}
```

Botbase DLL çıktı hedefi: `Build/Data/Bots/`

---

## Plugin Katmanı

### Isolation Tier Matrisi

| Plugin | Isolation Mode | Tier | Yetenekler |
|--------|---------------|------|------------|
| `UBot.General` | inproc | **critical** | connection-control, client-launch, session-bootstrap |
| `UBot.Protection` | inproc | **critical** | hp-mp-protection, pet-protection |
| `UBot.AutoDungeon` | **outproc** | experimental | dungeon-routing, dungeon-control |
| `UBot.PacketInspector` | **outproc** | experimental | packet-capture, packet-inspection |
| `UBot.Skills` | inproc | standard | skill-rotation, buff-management |
| `UBot.Items` | inproc | standard | item-filtering, autoloot-policy |
| `UBot.Inventory` | inproc | standard | inventory-tracking, inventory-actions |
| `UBot.Party` | inproc | standard | party-automation, party-sharing, party-matching |
| `UBot.Chat` | inproc | standard | chat-send, chat-events |
| `UBot.CommandCenter` | inproc | standard | command-routing, emote-commands |
| `UBot.Map` | inproc | standard | world-map, spawn-visualization, route-helpers |
| `UBot.Statistics` | inproc | standard | runtime-metrics, session-stats |
| `UBot.Log` | inproc | standard | log-stream, log-clear |
| `UBot.ServerInfo` | inproc | standard | server-inspection |
| `UBot.Quest` | inproc | standard | quest-tracking |
| `UBot.TargetAssist` | inproc | standard | target-assist |

### Plugin Bağımlılıkları

```
UBot.Items       → UBot.Inventory (required, 1.0.0–2.0.0)
UBot.Statistics  → UBot.Log      (optional, 1.0.0–2.0.0)
UBot.Party       → UBot.General  (optional, 1.0.0–2.0.0)
```

### IPlugin / IExtension Kontratı

```csharp
// Library/UBot.Core.Abstractions
interface IExtension
{
    string Name { get; }
    string Version { get; }
    void Initialize();
    void Enable();
    void Disable();
    void Translate();
    object View { get; }
}

interface IPlugin : IExtension
{
    bool DisplayAsTab { get; }
    int Index { get; }
    bool RequireIngame { get; }
    void OnLoadCharacter();
}
```

---

## Plugin Manifest Sistemi

Her plugin kendi `plugin.manifest.json` dosyasını taşır. Build sırasında `Build/Data/Plugins/<AssemblyName>.manifest.json` olarak kopyalanır.

### Manifest Şeması

```json
{
  "schemaVersion": 1,
  "pluginName": "UBot.General",
  "pluginVersion": "1.0.0",
  "capabilities": ["connection-control", "client-launch", "session-bootstrap"],
  "dependencies": [],
  "hostCompatibility": {
    "minVersion": "1.0.0",
    "maxVersionExclusive": "2.0.0"
  },
  "isolation": {
    "mode": "inproc",
    "tier": "critical",
    "restartPolicy": {
      "enabled": true,
      "maxRestarts": 2,
      "windowSeconds": 60,
      "baseDelayMs": 250,
      "maxDelayMs": 3000
    }
  }
}
```

### PluginContractManifestLoader Zorunlu Kuralları

- `pluginName` == runtime `IPlugin.Name` (tam eşleşme)
- `pluginVersion` == runtime `IPlugin.Version` (tam eşleşme)
- `isolation.mode` yalnızca `"inproc"` veya `"outproc"` olabilir
- `hostCompatibility` version aralığı kontrol edilir
- Bağımlılık ve yetenek validasyonu yapılır

---

## Konfigurasyon Sistemi

### Format

```
key{value}
```

Dosyalar `.rs` uzantılıdır ve `Build/User/` altında tutulur.

### Dosya Hiyerarşisi

```
Build/User/
├── Profiles.rs                     ← Profil listesi
├── <Profile>.rs                    ← Global profil config
└── <Profile>/
    └── <Character>.rs              ← Karakter bazlı config
```

### Facade Sınıfları

| Sınıf | Kapsam |
|-------|--------|
| `GlobalConfig` | Tüm profiller |
| `PlayerConfig` | Aktif karakter |
| `ProfileManager` | Profil seçimi / geçişi |

### Config Key Yönetimi

```powershell
# Kullanılan tüm config key'lerini dışa aktar
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1

# Migration kontrolü çalıştır
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

Yeni key naming standardı: `UBot.<Modül>.<Key>`

---

## Runtime Başlatma Akışı

`Application/UBot/Program.cs` başlangıç noktasıdır:

```
1. CLI argümanları parse edilir
2. ProcessLifetimeManager devreye alınır
3. --plugin-host modunda → PluginHostRuntime.Run(...)
4. Normal modda        → UBot.Avalonia.AvaloniaHost.Run(args)
5. Kapanışta bot / proxy / plugin host / config shutdown-save zinciri çalışır
```

### ExtensionManager Yükleme Akışı

```
Build/Data/Plugins  →  assembly topla
Build/Data/Bots     →  botbase topla
                     ↓
           manifest yükle (PluginContractManifestLoader)
                     ↓
    host version / dependency / capability / isolation validasyonu
                     ↓
     outproc plugin → PluginOutOfProcessHostManager'a register
     inproc plugin  → PluginFaultIsolationManager ile wrap
                     ↓
          initialize / enable / packet hook kayıtları
                     ↓
    UBot.DisabledPlugins config key'ine disable listesi yazılır
```

---

## CLI Argümanları

```
UBot.exe [seçenekler]

--character <name>          Başlangıçta yüklenecek karakter adı
--profile <name>            Kullanılacak profil adı
--launch-client             Silkroad client'ını başlatır
--launch-clientless         Client'sız (clientless) modda başlatır
--plugin-host               Out-of-proc plugin host modu (dahili kullanım)
--plugin-name <name>        Out-of-proc modda yüklenecek plugin adı
--plugin-path <path>        Out-of-proc modda plugin DLL'inin tam yolu
```

---

## UI Mimarisi (Avalonia)

`Application/UBot.Avalonia` feature-bazlı organize edilmiştir.

```
UBot.Avalonia/
├── MainWindow.axaml              ← Ana pencere (1440×900, resize kapalı)
├── MainWindowViewModel.cs        ← Ana VM, tema/dil toggle
├── Services/
│   ├── IUbotCoreService.cs       ← Core bridge arayüzü
│   ├── UbotCoreService.cs        ← In-process core adaptörü
│   └── AppState.cs               ← Global UI durumu
├── Features/                     ← Plugin/botbase feature view'ları
│   └── <Feature>/
│       ├── <Feature>View.axaml
│       └── <Feature>ViewModel.cs
├── FeatureViewFactory.cs         ← Dinamik view üretici
└── Styles/
    ├── Theme.axaml
    ├── Controls.axaml
    └── Features.axaml
```

**Teknik notlar:**
- Pencere boyutu sabit: **1440×900**, `CanResize=False`
- UI, core ile `IUbotCoreService` üzerinden in-process çalışır (WebSocket tabanlı değil)
- Tema ve dil geçişleri `MainWindowViewModel` + `AppState` üzerinden yönetilir
- Plugin ve botbase ekranları `FeatureViewFactory` tarafından dinamik üretilir

---

## Native Loader (C++)

`Library/UBot.Loader.Library` projesi, Microsoft Detours kullanarak Silkroad client sürecine hook atar.

- **Çıktı:** `Build/Client.Library.dll`
- **Platform:** x86 (DLL injection hedefi 32-bit process)
- **Teknoloji:** C++ / Detours
- **İlişki:** `UBot.Core` tarafından P/Invoke veya DLL injection mekanizmasıyla çağrılır

---

## Test ve Doğrulama

```powershell
# Tüm unit test'leri çalıştır
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

Mevcut test kapsamı (`ScriptManagerValidationTests.cs`):

- Script lint doğrulaması
- Script dry-run davranışı

---

## Sorun Giderme

| Belirti | Kontrol Edilecek |
|---------|-----------------|
| Build başarısız | `build.log` ve `Build/boot-error.log` dosyalarını inceleyin |
| Plugin yüklenmiyor | `Build/Data/Plugins/<Plugin>.manifest.json` ile runtime `IPlugin.Name` / `IPlugin.Version` uyumunu kontrol edin |
| Referans data eksik | Profil ayarlarında Silkroad yolu ve `media.pk2` varlığını doğrulayın |
| Out-of-proc plugin başlamıyor | `UBot.exe` ve plugin DLL'inin `Build/` altında olduğundan emin olun |
| MSBuild bulunamadı | Visual Studio Installer'da **MSBuild bileşeni** kurulu mu kontrol edin |
| `sro_client` çakışması | Build script zaten `taskkill` çalıştırır; manuel olarak görevi sonlandırın |

---

## Lisans

Bu proje **GPL-3.0** lisansı altında dağıtılmaktadır.  
Detaylar için [`LICENSE`](LICENSE) ve [`AGREEMENT.md`](AGREEMENT.md) dosyalarına bakın.

---

*Bu README, `UbotAva` repository'sindeki kaynak kod, çözüm yapısı, plugin manifest'leri ve build script tersine mühendislik edilerek oluşturulmuştur.*
