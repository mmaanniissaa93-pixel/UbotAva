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
- [Dependency Injection ve Bootstrap](#dependency-injection-ve-bootstrap)
- [Kernel ve RuntimeAccess](#kernel-ve-runtimeaccess)
- [Botbase Katmanı](#botbase-katmanı)
- [Plugin Katmanı](#plugin-katmanı)
- [Plugin Manifest Sistemi](#plugin-manifest-sistemi)
- [Konfigurasyon Sistemi](#konfigurasyon-sistemi)
- [Runtime Başlatma Akışı](#runtime-başlatma-akışı)
- [CLI Argümanları](#cli-argümanları)
- [UI Mimarisi (Avalonia)](#ui-mimarisi-avalonia)
- [UI Servis Katmanı](#ui-servis-katmanı)
- [Lokalizasyon](#lokalizasyon)
- [Native Loader (C++)](#native-loader-c)
- [Test ve Doğrulama](#test-ve-doğrulama)
- [Sorun Giderme](#sorun-giderme)
- [Lisans](#lisans)

---

## Proje Nedir?

**UBot**, Silkroad Online için geliştirilmiş, tamamen modüler bir otomasyon platformudur. Temel özellikler:

- **Plugin/Botbase ayrımı:** Oyun içi her işlev bağımsız bir plugin veya botbase olarak yüklenir/kaldırılır.
- **İzolasyon modelleri:** In-process fault izolasyonu (`PluginFaultIsolationManager`) ve out-of-process süreç izolasyonu (`PluginOutOfProcessHostManager`) desteklenir.
- **Güncel UI:** Avalonia 11 tabanlı masaüstü arayüzü; 488 lokalizasyon çiftiyle çok dil desteği.
- **Native entegrasyon:** C++ Detours tabanlı `Client.Library.dll` ile Silkroad client'ına hook atar.
- **Güvenli konfigurasyon:** Satır bazlı `key{value}` formatında profil + karakter bazlı config yönetimi.
- **Servis kontratları:** `RuntimeAccess` üzerinden sabit erişim noktası; 20'den fazla `IXxxRuntime` ve `IXxxService` arayüzü.

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
| Lokalizasyon | JSON tabanlı çeviri sistemi (488 çift, TR/EN) |
| DI Container | Microsoft.Extensions.DependencyInjection |

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

> **Not:** `build.ps1`, vswhere üzerinden VS 2022 Preview sürümlerini de tanır (`-prerelease` bayrağı etkin). Insider/Preview sürümü kullanıyorsanız ek yapılandırma gerekmez.

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

# Build sonrası UBot.exe'yi başlatmadan çık
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart
```

### Build Script Parametreleri

| Parametre | Varsayılan | Açıklama |
|-----------|------------|----------|
| `-Configuration` | `Debug` | `Debug` veya `Release` |
| `-Clean` | `$false` | Build klasörünü sıfırlar; `Build/User/` verileri geçici olarak `temp/` altına taşınır, silinmez |
| `-DoNotStart` | `$false` | Build sonrası `UBot.exe` otomatik başlatılmaz |
| `-SkipIconCacheRefresh` | `$false` | Icon cache yenileme adımını atlar |

### Build Akışı (build.ps1 özeti)

```
1. UBot.exe ve sro_client.exe sonlandırılır (varsa)
2. Repo dosyaları Unblock-File ile MOTW flag'lerinden arındırılır
3. MSBuild x86 → UBot.sln derlenir → build.log
4. UBot.TargetAssist DLL eksikse ayrıca doğrudan derlenir
5. build.log son 100 satırı ekrana yazdırılır
6. Dependencies/* (Languages hariç) → Build/Data/ altına kopyalanır
7. Plugins/*/plugin.manifest.json → Build/Data/Plugins/<Assembly>.manifest.json
8. Runtime data eksikse git restore ile Languages/Scripts/Towns toparlanır
9. (isteğe bağlı) Icon cache yenilenir → UBot.exe başlatılır
```

> **TargetAssist notu:** `UBot.TargetAssist.dll`, solution build'inde çıktı üretemezse build script bunu otomatik olarak tek proje olarak yeniden derler.

### Build Çıktıları

```
Build/
├── UBot.exe                          ← Ana executable
├── UBot.Updater.exe                  ← Otomatik güncelleyici
├── UBot.Avalonia.dll                 ← UI katmanı
├── Client.Library.dll                ← Native hook DLL (C++ / x86)
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
│   ├── UBot.Core.Abstractions/   ← IPlugin, IBotbase, 20+ servis kontratı
│   ├── UBot.Core.Common/         ← Yardımcı tipler ve extensions
│   ├── UBot.Core.Domain/         ← Domain DTO'ları, game-state kontratları
│   ├── UBot.Core.GameState/      ← Oyun durumu modeli (karakter, NPC, item, skill)
│   ├── UBot.Core.Network/        ← Network altyapısı, paket event'leri
│   ├── UBot.Core.Services/       ← Uygulama servisleri (Pickup, Shopping, Alchemy…)
│   ├── UBot.Core.Bootstrap/      ← DI composition root; ServiceProviderFactory
│   ├── UBot.FileSystem/          ← Yerel + PK2 dosya sistemi (Blowfish şifreleme)
│   ├── UBot.GameData/            ← RefObj*, RefSkill*, RefQuest* parser'ları
│   ├── UBot.NavMeshApi/          ← NavMesh, dungeon, terrain, path altyapısı
│   ├── UBot.Protocol/            ← SRO paket okuyucu/yazıcıları; ProtocolRuntime
│   └── UBot.Loader.Library/      ← C++ Detours tabanlı native DLL
│
├── Botbases/
│   ├── UBot.Training/            ← Grinding / levelling döngüsü
│   ├── UBot.Alchemy/             ← Efsun / alchemy otomasyonu
│   ├── UBot.Trade/               ← Ticaret rota ve işlem yönetimi
│   └── UBot.Lure/                ← Monster çekme stratejisi
│
├── Plugins/
│   ├── UBot.General/             ← Bağlantı, client başlatma (tier: critical)
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
│   ├── UBot.TargetAssist/        ← Hedef yardımı (v0.1.0, tier: standard)
│   ├── UBot.AutoDungeon/         ← Dungeon routing/control (outproc, v0.1.0)
│   └── UBot.PacketInspector/     ← Paket yakalama/inceleme (outproc, v0.1.0)
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
└── UBot_Keys_Refactored.md       ← Key referansları
```

---

## Dependency Injection ve Bootstrap

### Composition Root

`Library/UBot.Core.Bootstrap` tüm DI kaydını ve runtime başlatmayı yönetir:

```
ServiceProviderFactory.CreateServices()
    └── GameServiceCollectionExtensions.AddGameRuntime(services)
            ├── IGlobalSettings       → GlobalSettings        (singleton)
            ├── IPlayerSettings       → PlayerSettings        (singleton)
            ├── IKernelRuntime        → KernelRuntime         (singleton)
            ├── IGameSession          → GameSession.Shared    (singleton)
            ├── IPacketDispatcher     → PacketDispatcher      (singleton)
            ├── IScriptEventBus       → ScriptEventBus        (singleton)
            ├── IGameStateRuntimeContext → (via CoreRuntimeBootstrapper)
            ├── IClientLaunchPolicy   → ClientLaunchPolicyService
            ├── IClientlessService    → ClientlessService
            ├── IPickupService        → PickupService
            ├── IShoppingService      → ShoppingService
            ├── IAlchemyService       → AlchemyService
            ├── ILanguageService      → LanguageService
            ├── IProfileService       → ProfileService
            └── ProtocolServices
    └── CoreRuntimeBootstrapper.Initialize(provider)
```

### CoreRuntimeBootstrapper

`CoreRuntimeBootstrapper.Initialize()`, DI container yoksa `new CoreXxx()` ile fallback nesneleri oluşturarak `RuntimeAccess.Services.*` ve `ProtocolRuntime.*` statik erişim noktalarını doldurur. Atanan servisler (kısmi liste):

```
PickupRuntime, PickupSettings, InventoryRuntime, ShoppingRuntime,
AlchemyRuntime, AlchemyProgress, ScriptRuntime, ScriptProgress,
SpawnRuntime, SkillRuntime, SkillConfig, ClientConnectionRuntime,
Clientless, ClientNativeRuntime, ClientLaunchConfigProvider,
ClientLaunchPolicy, ProfileStorage, Profile, Log, Environment
```

### Uygulama Kapanışı

```csharp
ServiceProviderFactory.Dispose()  // IDisposable servislerin dispose zinciri çalışır
```

---

## Kernel ve RuntimeAccess

`Library/UBot.Core/Kernel.cs` statik bir kernel sınıfı sunar:

| Üye | Tip | Açıklama |
|-----|-----|----------|
| `Proxy` | `Proxy` | Silkroad ağ proxy nesnesi |
| `Bot` | `Bot` | Aktif botbase bağlantısı |
| `Language` | `string` | Uygulama dili |
| `LaunchMode` | `string` | CLI başlatma modu |
| `TickCount` | `int` | `Environment.TickCount & int.MaxValue` |
| `BasePath` | `string` | `AppDomain.CurrentDomain.BaseDirectory` |
| `EnableCollisionDetection` | `bool` | NavMeshApi aktifleştirme (config key: `UBot.EnableCollisionDetection`) |
| `Debug` | `bool` | `#if DEBUG` preprocessor direktifiyle belirlenir |

---

## Botbase Katmanı

Botbase'ler `IBotbase` interface'ini implemente eder. Her botbase bağımsız bir oyun döngüsü stratejisidir.

### IBotbase Kontratı (tam)

```csharp
public interface IBotbase : IExtension
{
    Area Area { get; }   // UBot.Core.Objects.Area — oyun bölgesi tanımı
    void Start();
    void Tick();
    void Stop();
}
```

### IExtension Kontratı (tam)

```csharp
public interface IExtension
{
    string Author { get; }
    string Description { get; }
    string Name { get; }        // manifest pluginName ile TAM eşleşmeli
    string Title { get; }       // UI'da gösterilen görüntü adı
    string Version { get; }     // manifest pluginVersion ile TAM eşleşmeli
    bool Enabled { get; set; }

    void Initialize();
    Control View { get; }       // System.Windows.Forms.Control
    void Translate();
    void Enable();
    void Disable();
}
```

### Mevcut Botbase'ler

| Botbase | Alan | Açıklama |
|---------|------|----------|
| `UBot.Training` | Training | Standart grinding / levelling döngüsü |
| `UBot.Alchemy` | Alchemy | Efsun / alchemy otomasyonu |
| `UBot.Trade` | Trade | Ticaret rota ve işlem yönetimi |
| `UBot.Lure` | Lure | Monster çekme stratejisi |

Botbase DLL çıktı hedefi: `Build/Data/Bots/`

---

## Plugin Katmanı

### IPlugin Kontratı (tam)

```csharp
public interface IPlugin : IExtension
{
    bool DisplayAsTab { get; }   // true → sekme olarak gösterilir
    int Index { get; }           // küçük değer = daha önce görünür
    bool RequireIngame { get; }  // false → her zaman etkin

    void OnLoadCharacter();
    void OnProfileChanged() => OnLoadCharacter();  // default implementasyon
}
```

### Isolation Tier Matrisi (tam)

| Plugin | Runtime Adı | Sürüm | Mode | Tier | Tab | Index |
|--------|-------------|-------|------|------|-----|-------|
| `UBot.General` | UBot.General | 1.0.0 | inproc | **critical** | ✅ | — |
| `UBot.Protection` | UBot.Protection | 1.0.0 | inproc | **critical** | ✅ | 2 |
| `UBot.AutoDungeon` | UBot.AutoDungeon | **0.1.0** | **outproc** | experimental | — | — |
| `UBot.PacketInspector` | UBot.PacketInspector | **0.1.0** | **outproc** | experimental | — | — |
| `UBot.Skills` | UBot.Skills | 1.0.0 | inproc | standard | — | — |
| `UBot.Items` | UBot.Items | 1.0.0 | inproc | standard | — | — |
| `UBot.Inventory` | UBot.Inventory | 1.0.0 | inproc | standard | ✅ | 4 |
| `UBot.Party` | UBot.Party | 1.0.0 | inproc | standard | — | — |
| `UBot.Chat` | UBot.Chat | 1.0.0 | inproc | standard | — | — |
| `UBot.CommandCenter` | UBot.CommandCenter | 1.0.0 | inproc | standard | — | — |
| `UBot.Map` | UBot.Map | 1.0.0 | inproc | standard | — | — |
| `UBot.Statistics` | UBot.Statistics | 1.0.0 | inproc | standard | ✅ | 97 |
| `UBot.Log` | UBot.Log | 1.0.0 | inproc | standard | — | — |
| `UBot.ServerInfo` | UBot.ServerInfo | 1.0.0 | inproc | standard | — | — |
| `UBot.Quest` | **UBot.QuestLog** | 1.0.0 | inproc | standard | — | 0 |
| `UBot.TargetAssist` | UBot.TargetAssist | **0.1.0** | inproc | standard | — | — |

> **⚠️ Kritik:** `UBot.Quest` proje ve assembly adı, ancak runtime `IPlugin.Name` ve manifest `pluginName` değeri **`UBot.QuestLog`**'dur. Bu fark `PluginContractManifestLoader` tarafından zorunlu tutulur; eşleşmezse plugin yüklenmez.

### Plugin Yetenekleri

| Plugin | Capabilities |
|--------|-------------|
| `UBot.General` | connection-control, client-launch, session-bootstrap |
| `UBot.Protection` | hp-mp-protection, pet-protection |
| `UBot.AutoDungeon` | dungeon-routing, dungeon-control |
| `UBot.PacketInspector` | packet-capture, packet-inspection |
| `UBot.Skills` | skill-rotation, buff-management |
| `UBot.Items` | item-filtering, autoloot-policy |
| `UBot.Inventory` | inventory-tracking, inventory-actions |
| `UBot.Party` | party-automation, party-sharing, party-matching |
| `UBot.Chat` | chat-send, chat-events |
| `UBot.CommandCenter` | command-routing, emote-commands |
| `UBot.Map` | world-map, spawn-visualization, route-helpers |
| `UBot.Statistics` | runtime-metrics, session-stats |
| `UBot.Log` | log-stream, log-clear |
| `UBot.ServerInfo` | server-inspection |
| `UBot.Quest` | quest-tracking |
| `UBot.TargetAssist` | target-assist |

### Plugin Bağımlılıkları

```
UBot.Items      → UBot.Inventory   (required: true,  1.0.0–2.0.0)
UBot.Statistics → UBot.Log         (required: false, 1.0.0–2.0.0)
UBot.Party      → UBot.General     (required: false, 1.0.0–2.0.0)
```

### Protection Plugin Bileşenleri

`UBot.Protection` alt handler'larla organize edilmiştir:

**Oyuncu handler'ları:**
`HealthManaRecoveryHandler`, `UniversalPillHandler`, `VigorRecoveryHandler`, `StatPointsHandler`

**Pet handler'ları:**
`CosHealthRecoveryHandler`, `CosHGPRecoveryHandler`, `CosBadStatusHandler`, `CosReviveHandler`, `AutoSummonAttackPet`

**Kasaya dönüş handler'ları:**
`DeadHandler`, `AmmunitionHandler`, `InventoryFullHandler`, `PetInventoryFullHandler`, `NoManaPotionsHandler`, `NoHealthPotionsHandler`, `LevelUpHandler`, `DurabilityLowHandler`, `FatigueHandler`, `StartPrecheckHandler`

### İstatistik Hesaplayıcılar (UBot.Statistics)

**Canlı (saatlik oran):** `KillsPerHour`, `ExperiencePerHour`, `LootPerHour`, `GoldPerHour`, `SkillPointsPerHour`

**Statik (toplam sayım):** `Kills`, `Deaths`, `LevelUps`, `Experience`, `BottingTime`, `Loot`, `Gold`, `SkillPoints`

### Inventory Subscriber'lar (UBot.Inventory)

`BuyItemSubscriber`, `InventoryUpdateSubscriber`, `UseItemAtTrainplaceSubscriber`

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

| Kontrol | Kural |
|---------|-------|
| `pluginName` | Runtime `IPlugin.Name` ile **birebir** eşleşmeli |
| `pluginVersion` | Runtime `IPlugin.Version` ile **birebir** eşleşmeli |
| `isolation.mode` | Yalnızca `"inproc"` veya `"outproc"` kabul edilir |
| `hostCompatibility` | Host sürümü `[minVersion, maxVersionExclusive)` aralığında olmalı |
| Bağımlılıklar | `required: true` olanlar yüklü ve sürüm aralığında olmalı |
| Yetenekler | `requiredCapabilities` sağlayan plugin mevcut olmalı |

### Zorunlu Out-of-Proc Plugin'ler

`ExtensionManager._requiredOutOfProcPlugins` kümesine hardcode edilmiştir:

| Plugin | Neden outproc? |
|--------|----------------|
| `UBot.PacketInspector` | Paket yakalama süreç izolasyonu gerektirir |
| `UBot.AutoDungeon` | Dungeon routing crash riski ana süreci etkilememeli |

---

## Konfigurasyon Sistemi

### Format

```
key{value}
```

Dosyalar `.rs` uzantılıdır ve `Build/User/` altında tutulur (build sürecinde silinmez).

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

### Özel Config Key'leri

| Key | Açıklama |
|-----|----------|
| `UBot.EnableCollisionDetection` | NavMeshApi aktifleştirme |
| `UBot.DisabledPlugins` | Devre dışı plugin adları listesi |

### Config Key Yönetimi

```powershell
# Tüm key envanterini dışa aktar
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1

# Migration kontrolü (envanter yenilenerek)
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
4. Normal modda         → UBot.Avalonia.AvaloniaHost.Run(args)
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

### Profil Değişikliği Akışı

`ExtensionManager.OnProfileChanged()` çağrıldığında etkin her plugin'e `_faultIsolation.TryExecute(...)` koruması altında `OnProfileChanged()` iletilir; ardından `RuntimeAccess.Events.FireEvent("OnLoadCharacter")` tetiklenir.

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
├── App.axaml / App.axaml.cs
├── MainWindow.axaml              ← Ana pencere (1440×900, CanResize=False)
├── AvaloniaHost.cs               ← Avalonia uygulama entry point
├── FeatureViewFactory.cs         ← Dinamik feature view üretici
├── app.manifest
│
├── Services/
│   ├── IUbotCoreService.cs       ← Core bridge arayüzü
│   ├── UbotCoreService.cs        ← Ana in-process adaptör
│   ├── UbotCoreService.Initialization.cs
│   ├── UbotCoreService.Connection.cs
│   ├── UbotCoreService.AutoLogin.cs
│   ├── UbotCoreService.Actions.cs
│   ├── UbotCoreService.Map.cs
│   ├── UbotCoreService.Icons.cs
│   ├── UbotCoreService.CommandCenter.cs
│   ├── UbotCoreService.SoundNotifications.cs
│   ├── UbotCoreService.Dialogs.cs
│   ├── UbotCoreService.Helpers.cs
│   ├── AppState.cs               ← Global UI durumu
│   ├── DesktopLanguageService.cs
│   └── RuntimeTypes.cs
│
├── Features/                     ← Plugin/botbase başına feature ekranı
│   ├── General/      ← GeneralFeatureView + AccountSetupWindow + SoundNotificationsWindow
│   ├── Training/     ← TrainingFeatureView
│   ├── Alchemy/      ← AlchemyFeatureView
│   ├── Lure/         ← LureFeatureView
│   ├── Skills/       ← SkillsFeatureView
│   ├── Items/        ← ItemsFeatureView
│   ├── Party/        ← PartyFormWindow + TextPromptWindow
│   ├── Chat/         ← ChatFeatureView
│   ├── CommandCenter/← CommandCenterFeatureView + CommandCenterPopupWindow
│   ├── Map/          ← MapFeatureView
│   ├── Logging/      ← LogFeatureView
│   ├── ServerInfo/   ← ServerInfoFeatureView
│   ├── Quest/        ← QuestFeatureView
│   ├── TargetAssist/ ← TargetAssistFeatureView
│   ├── AutoDungeon/  ← AutoDungeonFeatureView
│   └── GenericFeatureView.axaml  ← Fallback view
│
├── Controls/                     ← Paylaşılan custom Avalonia control'lar
├── Dialogs/
├── ViewModels/
├── Styles/
└── Assets/
    └── Localization/translations.json
```

**Teknik notlar:**
- Pencere boyutu sabit: **1440×900**, `CanResize=False`
- `UbotCoreService`, 10 ayrı partial dosyaya bölünmüştür
- Her plugin ve botbase için ayrı `Ubot<X>PluginService` / `Ubot<X>BotbaseService` sınıfı mevcuttur
- Tema ve dil geçişleri `MainWindowViewModel` + `AppState` üzerinden yönetilir

---

## UI Servis Katmanı

Her plugin/botbase için ayrı bir servis sınıfı bulunur:

| Servis Sınıfı | Karşılık |
|---------------|----------|
| `UbotGeneralPluginService` | UBot.General |
| `UbotProtectionPluginService` | UBot.Protection |
| `UbotSkillsPluginService` | UBot.Skills |
| `UbotItemsPluginService` | UBot.Items |
| `UbotPartyPluginService` | UBot.Party |
| `UbotCommandCenterPluginService` | UBot.CommandCenter |
| `UbotMapPluginService` | UBot.Map |
| `UbotTargetAssistPluginService` | UBot.TargetAssist |
| `UbotTrainingBotbaseService` | Training |
| `UbotAlchemyBotbaseService` | Alchemy |
| `UbotTradeBotbaseService` | Trade |
| `UbotLureBotbaseService` | Lure |

---

## Lokalizasyon

`Assets/Localization/translations.json` dosyası **488 anahtar-değer çifti** barındırır. Dil servisi `DesktopLanguageService` / `ILanguageService` aracılığıyla yönetilir. Her plugin `IExtension.Translate()` içinde `LanguageManager.Translate(View, language)` çağrısı yapar.

---

## Native Loader (C++)

`Library/UBot.Loader.Library` projesi, Microsoft Detours kullanarak Silkroad client sürecine hook atar.

- **Çıktı:** `Build/Client.Library.dll`
- **Platform:** x86 (DLL injection hedefi 32-bit process)
- **Teknoloji:** C++ / Detours
- **Erişim:** `IClientNativeRuntime` arayüzü üzerinden `RuntimeAccess.Services.ClientNativeRuntime`

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
| `UBot.Quest` yüklenmiyor | Runtime adının **`UBot.QuestLog`** olduğunu ve manifest ile eşleştiğini doğrulayın |
| Referans data eksik | Profil ayarlarında Silkroad yolu ve `media.pk2` varlığını doğrulayın |
| Out-of-proc plugin başlamıyor | `UBot.exe` ve plugin DLL'inin `Build/` altında olduğundan emin olun |
| MSBuild bulunamadı | Visual Studio Installer'da **MSBuild bileşeni** kurulu mu kontrol edin |
| `sro_client` çakışması | Build script zaten `taskkill` çalıştırır; manuel olarak görevi sonlandırın |
| TargetAssist DLL eksik | Build script otomatik yeniden derleme yapar; yine eksikse `PlatformTarget=x86` kontrol edin |
| Protection handler çalışmıyor | `Bootstrap.Initialize()` kayıtlarını ve `Disable()` içindeki `UnsubscribeAll()` çağrılarını inceleyin |
| UI feature açılmıyor | `FeatureViewFactory` kaydını ve ilgili `Ubot<X>PluginService` sınıfını kontrol edin |
| Config okunmuyor | `key{value}` formatını ve `Build/User/` konumunu doğrulayın |

---

## Lisans

Bu proje **GPL-3.0** lisansı altında dağıtılmaktadır.  
Detaylar için [`LICENSE`](LICENSE) ve [`AGREEMENT.md`](AGREEMENT.md) dosyalarına bakın.  
Copyright © 2018 – 2026 UBot Team

---

*Bu README, `UbotAva` repository'sindeki kaynak kod, interface tanımları, plugin manifest'leri, DI composition root, servis katmanı ve build script tersine mühendislik edilerek oluşturulmuştur.*
