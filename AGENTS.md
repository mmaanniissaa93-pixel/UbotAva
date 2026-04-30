# AGENTS.md — UBot Geliştirici & AI Agent Rehberi

> Bu dosya, [UbotAva](https://github.com/mmaanniissaa93-pixel/UbotAva) repository'sinde çalışan **AI agent'lar**, **otomatik kod düzenleme araçları** ve **contributor'lar** için birincil operasyon rehberidir.
>
> Amaç: değişikliklerin build/runtime uyumluluğunu korumak, plugin kontratlarını bozmamak, UI-core entegrasyonunu stabil tutmak.

---

## İçindekiler

- [Kesin Kurallar](#kesin-kurallar)
- [Hızlı Komut Referansı](#hızlı-komut-referansı)
- [Mimari Yol Haritası](#mimari-yol-haritası)
- [Plugin Geliştirme Kuralları](#plugin-geliştirme-kuralları)
- [Botbase Geliştirme Kuralları](#botbase-geliştirme-kuralları)
- [Manifest Uyumluluğu](#manifest-uyumluluğu)
- [Fault Isolation Mekanizmaları](#fault-isolation-mekanizmaları)
- [Script Komut Sistemi](#script-komut-sistemi)
- [Paket Handler Sistemi](#paket-handler-sistemi)
- [UI Değişikliklerinde Dikkat](#ui-değişikliklerinde-dikkat)
- [Config ve Veri Kuralları](#config-ve-veri-kuralları)
- [Config Migration Sözleşmesi](#config-migration-sözleşmesi)
- [Build Çıktı ve Runtime Asset Notları](#build-çıktı-ve-runtime-asset-notları)
- [Test ve Validasyon Beklentisi](#test-ve-validasyon-beklentisi)
- [Belgeleme Politikası](#belgeleme-politikası)
- [Sık Hata Kaynakları](#sık-hata-kaynakları)

---

## Kesin Kurallar

Aşağıdaki kurallar **istisna kabul etmez:**

1. **Canonical build yolu `build.ps1`'dir.** `dotnet build UBot.sln` veya başka bir yol kullanmayın; MSBuild/x86 akışını atlayan buildler hatalı çıktı üretir.
2. **Platform varsayımı x86'dır.** Tüm `.csproj` ve `.vcxproj` dosyalarındaki `PlatformTarget` değeri `x86` olarak korunmalıdır. Farklı platform hedefi native loader uyumsuzluğuna yol açar.
3. **Kullanıcı değişikliklerini geri alma yasaktır.** `git reset --hard`, `git checkout --` veya benzeri yıkıcı git komutları çalıştırılmamalıdır.
4. **Proje dosyalarına minimal dokunun.** `*.sln`, `*.csproj`, `*.vcxproj`, `*.Designer.cs` dosyalarına yalnızca zorunluysa değişiklik yapın ve değişikliği açık biçimde belgeleyin.
5. **Plugin manifest ile runtime metadata tutarlı kalmalıdır.** `pluginName`, `pluginVersion`, `isolation.mode` ve bağımlılık bildirimleri her zaman senkronda olmalıdır.
6. **Zorunlu out-of-proc plugin'lerin isolation mode'unu değiştirmeyin.** `UBot.AutoDungeon` ve `UBot.PacketInspector` her zaman `outproc` modda kalır; `ExtensionManager._requiredOutOfProcPlugins` kümesine hardcode edilmiştir.
7. **`UBot.Quest` runtime adı `UBot.QuestLog`'dur.** Proje klasörü ve assembly adı `UBot.Quest` olsa da `IPlugin.Name` ve manifest `pluginName` alanı kesinlikle `UBot.QuestLog` olmalıdır. Değiştirmeyin.
8. **`CultureInfo.CurrentCulture` `en-US`'tir.** Float sayılar noktayla (`"."`) kaydedilir; virgüle (`","`) geçiş yapılmamalıdır. Bu `Program.cs`'de uygulama başlangıcında atanır.

---

## Hızlı Komut Referansı

### Build

```powershell
# Standart Debug build
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug

# Release build (otomatik başlatmadan)
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Release -DoNotStart

# Temiz build (user verileri korunur)
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Clean -Configuration Debug -DoNotStart
```

### Test

```powershell
# Unit test çalıştır
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

### Config Key Araçları

```powershell
# Kullanılan tüm config key envanterini dışa aktar
powershell.exe -ExecutionPolicy Bypass .\tools\config\Export-UsedConfigKeyInventory.ps1

# Migration kontrolü (envanter yenilenerek)
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

---

## Mimari Yol Haritası

Agent'ın değişiklik yaptığı alanı doğru katmana yerleştirmesi için:

### Application Katmanı

| Proje | Sorumluluk |
|-------|-----------|
| `Application/UBot` | Process entry point (`Program.cs`), CLI argüman parse, shutdown orchestration, `PluginHostRuntime` |
| `Application/UBot.Avalonia` | Masaüstü UI (Avalonia 11), feature view'ları, partial `UbotCoreService`, `FeatureViewFactory` |
| `Application/UBot.Updater` | `update_temp/update.zip` açıp güncelleme uygulayan bileşen |

### Library Katmanı

| Proje | Sorumluluk |
|-------|-----------|
| `Library/UBot.Core` | `Kernel`, `ExtensionManager`, `EventManager`, `PluginFaultIsolationManager`, `PluginOutOfProcessHostManager`, scripting |
| `Library/UBot.Core.Abstractions` | `IPlugin`, `IBotbase`, `IExtension`, `IKernelRuntime`, `IInventoryRuntime`, `ISkillService`, `IPickupSettings` ve 15+ diğer kontrat |
| `Library/UBot.Core.Bootstrap` | `ServiceProviderFactory`, `GameServiceCollectionExtensions.AddGameRuntime()`, `CoreRuntimeBootstrapper` |
| `Library/UBot.Core.Common` | Yardımcı tipler, extension methodlar |
| `Library/UBot.Core.Domain` | Domain DTO'ları, lightweight game-state kontratları |
| `Library/UBot.Core.GameState` | `SpawnManager`, `Player`, `SpawnedEntity` modelleri |
| `Library/UBot.Core.Network` | `PacketDispatcher`, `IPacketHandler`, `IPacketHook`, paket event'leri |
| `Library/UBot.Core.Services` | `PickupService`, `ShoppingService`, `AlchemyService`, `ProfileService` ve diğerleri |
| `Library/UBot.FileSystem` | Yerel dosya sistemi + PK2 arşiv okuyucu (Blowfish şifreleme) |
| `Library/UBot.GameData` | `RefObjCommon`, `RefSkill`, `RefQuest` vb. Silkroad referans parser'ları |
| `Library/UBot.NavMeshApi` | NavMesh, dungeon, terrain, path altyapısı |
| `Library/UBot.Protocol` | Silkroad paket okuyucu/yazıcıları; `ProtocolRuntime` statik erişim |
| `Library/UBot.Loader.Library` | C++ Detours tabanlı native `Client.Library.dll` |

### DI Composition Root

```
Library/UBot.Core.Bootstrap/ServiceProviderFactory.cs
    └── CreateServices()   → çağrılan yer: Application/UBot/Program.cs ve AvaloniaHost.cs
    └── Dispose()          → çağrılan yer: Program.PerformFinalShutdown() sonrası finally bloğu

Library/UBot.Core.Bootstrap/GameServiceCollectionExtensions.cs
    └── AddGameRuntime()   → tüm core singleton kayıtları buraya eklenmeli

Library/UBot.Core/Runtime/CoreRuntimeBootstrapper.cs
    └── Initialize()       → RuntimeAccess.Services.* ve ProtocolRuntime.* statik noktaları doldurur
```

- **Yeni core servisler** `AddGameRuntime()` içine `singleton` olarak kaydedilmeli.
- `ServiceProviderFactory.CreateServices()` yalnızca **uygulama entry point'lerinde** çağrılmalıdır; plugin veya library kodu hiçbir zaman çağırmamalıdır.
- `ServiceProviderFactory.Dispose()` uygulama kapanışında tek kez çalışır; `IDisposable` servisler container üzerinden temizlenir.

### Shutdown Zinciri (Program.cs)

```
1. Bot.Stop()               (varsa, çalışıyorsa)
2. RuntimeAccess.Core.Shutdown()
3. Proxy.Shutdown() + ClientManager.Kill()
4. ExtensionManager.Shutdown()
5. ServiceProviderFactory.Dispose()
```

Her adım ayrı `try/catch` bloğuyla sarılıdır; biri başarısız olsa diğerleri devam eder.

### Extension Katmanı

| Tip | Kaynak | Çıktı Hedefi |
|-----|--------|-------------|
| Plugin DLL'leri | `Plugins/<X>/` | `Build/Data/Plugins/` |
| Plugin manifest'leri | `Plugins/<X>/plugin.manifest.json` | `Build/Data/Plugins/<AssemblyName>.manifest.json` |
| Botbase DLL'leri | `Botbases/<X>/` | `Build/Data/Bots/` |

---

## Plugin Geliştirme Kuralları

### Yeni Plugin Ekleme — Adım Adım

**1. Proje oluştur:**

```
Plugins/<YeniPlugin>/<YeniPlugin>.csproj
```

`PlatformTarget` = `x86`, `TargetFramework` = `net8.0` olmalı.

**2. Interface implement et:**

```csharp
public class YeniPlugin : IPlugin
{
    public string Author      => "UBot Team";
    public string Description => "Plugin açıklaması.";
    public string Name        => "UBot.YeniPlugin";    // manifest pluginName ile TAM eşleşmeli
    public string Title       => "Yeni Plugin";        // UI görüntü adı
    public string Version     => "1.0.0";              // manifest pluginVersion ile TAM eşleşmeli
    public bool   Enabled     { get; set; }
    public bool   DisplayAsTab => true;
    public int    Index        => 50;                  // sıralama; küçük = önce
    public bool   RequireIngame => true;

    public void Initialize()   { /* event subscribe, handler init, ScriptCommand register */ }
    public void Enable()       { if (View != null) View.Enabled = true; }
    public void Disable()      { /* UnsubscribeAll() çağır; */ if (View != null) View.Enabled = false; }
    public void OnLoadCharacter() { /* config yenile, view/state sıfırla */ }
    public void Translate()    { LanguageManager.Translate(View, RuntimeAccess.Core.Language); }
    public Control View        => Views.View.Instance;
}
```

**3. Manifest oluştur:**

```json
// Plugins/YeniPlugin/plugin.manifest.json
{
  "schemaVersion": 1,
  "pluginName": "UBot.YeniPlugin",
  "pluginVersion": "1.0.0",
  "capabilities": ["yetenek-1", "yetenek-2"],
  "dependencies": [],
  "hostCompatibility": { "minVersion": "1.0.0", "maxVersionExclusive": "2.0.0" },
  "isolation": {
    "mode": "inproc",
    "tier": "standard",
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

**4. Çıktı yolunu ayarla:** `<OutputPath>..\..\Build\Data\Plugins\</OutputPath>` — csproj içinde doğrula.

**5. Solution'a ekle:** `UBot.sln` dosyasına projeyi dahil et.

**6. UI bağlantısı (gerekiyorsa):**
- `Application/UBot.Avalonia/Features/<YeniFeature>/` altına view/viewmodel ekle.
- `FeatureViewFactory`'ye mapping ekle.
- `IUbotCoreService`'e gerekli metot/event tanımla ve `UbotCoreService` partial dosyasına implemente et.
- Gerekirse `Ubot<YeniPlugin>PluginService.cs` oluştur.

**7. Bu dosyaları güncelle:** `README.md` ve `AGENTS.md`.

### Plugin Kaldırma Kontrol Listesi

```
[ ] Plugin DLL ve manifest'i Build/Data/Plugins/ çıktısından kaldır
[ ] UBot.sln'dan projeyi çıkar
[ ] FeatureViewFactory kaydını temizle
[ ] IUbotCoreService'teki metot/event tanımlarını kaldır
[ ] UbotCoreService partial dosyasındaki implementasyonu kaldır
[ ] Ubot<X>PluginService.cs dosyasını sil
[ ] Bağımlı plugin varsa dependency bildirimini güncelle
[ ] UBot.DisabledPlugins config key'inde referans varsa temizle
[ ] README.md ve AGENTS.md'yi güncelle
```

### Handler Tabanlı Plugin Yapısı

`UBot.Protection` örneği: büyük plugin'ler mantığı alt handler sınıflarına bölmeli.

```csharp
// Initialize() içinde handler'ları kaydet
public void Initialize()
{
    MyFeatureHandler.Initialize();      // event subscribe
    AnotherFeatureHandler.Initialize();
}

// Disable() içinde temizlik — UnsubscribeAll() zorunludur
public void Disable()
{
    MyFeatureHandler.UnsubscribeAll();
    AnotherFeatureHandler.UnsubscribeAll();
    if (View != null) View.Enabled = false;
}
```

### HeadlessView Sözleşmesi

Out-of-proc plugin'ler (`UBot.AutoDungeon`, `UBot.PacketInspector`) ve bazı lightweight plugin'ler `HeadlessView.cs` içerir. Bu dosya boş bir `Control` döndürür; UI'ya bağlanacak gerçek bir form yoktur.

---

## Botbase Geliştirme Kuralları

### Yeni Botbase Ekleme

**1. Proje oluştur:**

```
Botbases/<YeniBot>/<YeniBot>.csproj
```

**2. Interface implement et:**

```csharp
public class YeniBot : IBotbase
{
    public string Author      => "UBot Team";
    public string Description => "Bot açıklaması.";
    public string Name        => "UBot.YeniBot";
    public string Title       => "Yeni Bot";
    public string Version     => "1.0.0";
    public bool   Enabled     { get; set; }
    public Area   Area        => new Area("HedefAlan");

    public void Initialize() { }
    public void Start()      { /* başlangıç mantığı */ }
    public void Tick()       { /* ana oyun döngüsü */ }
    public void Stop()       { /* temizlik */ }
    public void Enable()     { if (View != null) View.Enabled = true; }
    public void Disable()    { if (View != null) View.Enabled = false; }
    public void Translate()  { LanguageManager.Translate(View, RuntimeAccess.Core.Language); }
    public Control View      => Views.View.Instance;
}
```

**3. Çıktı yolunu ayarla:** `Build/Data/Bots/` hedef olmalı.

**4. Solution'a ekle:** `UBot.sln` içine dahil et.

**5. UI servis:** `Application/UBot.Avalonia/Services/Ubot<YeniBot>BotbaseService.cs` oluştur.

**6. Feature view:** `Features/<YeniBot>/<YeniBot>FeatureView.axaml` ve ViewModel ekle, `FeatureViewFactory`'ye kaydet.

---

## Manifest Uyumluluğu

`PluginContractManifestLoader`, her plugin yüklenirken şu kontrolleri yapar. Başarısız olan plugin yüklenmez ve hata `ExtensionManager.LastLoadError` alanına yazılır.

| Kontrol | Kural | Hata Sonucu |
|---------|-------|-------------|
| `pluginName` | Runtime `IPlugin.Name` ile **birebir** eşleşmeli | Plugin yüklenmez |
| `pluginVersion` | Runtime `IPlugin.Version` ile **birebir** eşleşmeli | Plugin yüklenmez |
| `isolation.mode` | Yalnızca `"inproc"` veya `"outproc"` | Plugin yüklenmez |
| `hostCompatibility` | `[minVersion, maxVersionExclusive)` aralığı | Plugin yüklenmez |
| Bağımlılıklar | `required: true` olanlar yüklü ve versiyon aralığında | Plugin yüklenmez |
| Yetenekler | `requiredCapabilities` sağlayan plugin mevcut | Plugin yüklenmez |

### Manifest Yolu Çözümlemesi

Manifest dosyası, assembly ile aynı dizinde `<AssemblyName>.manifest.json` adıyla aranır. Build script bunu `Build/Data/Plugins/` altına kopyalar.

### Zorunlu Out-of-Proc Plugin'ler

Bu iki plugin'in `isolation.mode` değeri **hiçbir koşulda değiştirilemez:**

| Plugin | Tier | Neden outproc? |
|--------|------|----------------|
| `UBot.PacketInspector` | experimental | Paket yakalama süreç izolasyonu gerektirir |
| `UBot.AutoDungeon` | experimental | Dungeon routing crash riski ana süreci etkilememeli |

`UBot.AutoDungeon`, `Initialize()` içinde iki script komutunu kaydettiğinden `ScriptManager.RegisterCommandHandler()` / `UnregisterCommandHandler()` çiftlerinin `Enable()` ve `Disable()` içinde doğru çağrılması zorunludur.

---

## Fault Isolation Mekanizmaları

### PluginFaultIsolationManager (inproc)

Her in-process plugin aksiyonu `TryExecute()` koruması altında çalışır:

```
TryExecute(pluginName, actionName, restartPolicy, action)
    ├─ Başarı → TrackSuccess() → true döner
    └─ Hata   → TrackFailure() → restartPolicy.MaxRestarts kadar tekrar
                               → Delay: baseDelayMs * 2^(attempt-1), maxDelayMs'e kadar
                               → Tüm denemeler başarısız → false döner
```

Restart politikası manifest `restartPolicy` bloğundan alınır (varsayılan: `maxRestarts=2`, `windowSeconds=60`, `baseDelayMs=250`, `maxDelayMs=3000`).

`GetSnapshot()` metodu, tüm plugin'lerin fault durumunu anlık görüntü olarak döner — UI hata gösterimi için kullanılabilir.

### PluginOutOfProcessHostManager (outproc)

Out-of-proc plugin'ler `UBot.exe --plugin-host --plugin-name <name> --plugin-path <path>` komutuyla ayrı bir süreçte çalışır.

- `Register()` → çalışma kaydı oluşturur (henüz başlatmaz)
- `Enable()` → süreci başlatır, `ProcessLifetimeManager` ile ana süreç öldüğünde alt süreç de sonlanır
- `Disable()` → süreci durdurur
- `Dispose()` → tüm alt süreçleri temizler
- `GetSnapshot()` → çalışan host süreçlerinin anlık görüntüsü

---

## Script Komut Sistemi

`UBot.AutoDungeon` iki script komutunu kayıt eder; bu kalıbı izleyin:

```csharp
// IScriptCommand implementasyonu
public class MyScriptCommand : IScriptCommand
{
    public string Name => "MyCommand";    // .rbs script'lerinde kullanılan ad
    public bool IsBusy { get; private set; }
    public Dictionary<string, string> Arguments => new()
    {
        { "ParamAdı", "Açıklama" }
    };

    public bool Execute(string[] arguments = null)
    {
        IsBusy = true;
        try   { /* iş mantığı */ return true; }
        finally { IsBusy = false; }
    }

    public void Stop()
    {
        _stopRequested = true;
        IsBusy = false;
    }
}

// Plugin.Initialize() ve Enable() içinde:
ScriptManager.RegisterCommandHandler(new MyScriptCommand());

// Plugin.Disable() içinde:
ScriptManager.UnregisterCommandHandler("MyCommand");
```

Mevcut kayıtlı script komutları: `AttackArea`, `GoDimensional` (her ikisi de `UBot.AutoDungeon`'dan).

---

## Paket Handler Sistemi

`IPacketHandler` ve `IPacketHook`, `UBot.Core.Network` namespace'indedir.

```csharp
// Gelen/giden paket handler örneği
public class MyPacketHandler : IPacketHandler
{
    public ushort Opcode           => 0xXXXX;
    public PacketDestination Destination => PacketDestination.Client; // veya Server

    public void Invoke(Packet packet)
    {
        var success = packet.ReadByte() == 0x01;
        // state güncelle, event fırlat, vs.
    }
}
```

Handler'lar `ExtensionManager.LoadAssemblies<IPlugin>()` sonrasında plugin `Initialize()` aşamasında kayıt edilir. `Disable()` içinde kayıt kaldırılmalıdır.

Gerçek kullanım örnekleri: `DimensionalItemUseResponseHandler` (opcode `0xB04C`), `ForgottenWorldInvitationHandler` (opcode `0x751A`).

---

## UI Değişikliklerinde Dikkat

### Temel Kısıtlamalar

- **Pencere boyutu sabittir:** `1440×900`, `CanResize=False`. Layout hesaplamaları bu boyuta göre yapılmıştır; değiştirirseniz tüm feature view'larını test edin.
- **UI-Core köprüsü:** View code-behind'e doğrudan `RuntimeAccess.*` veya static core sınıf çağrısı **yazmayın**. Her core erişimi `IUbotCoreService` üzerinden geçmelidir.
- **State yönetimi:** Global davranışlar (tema, dil, aktif profil) `MainWindowViewModel` + `AppState` modeline bağlı kalmalı.
- **`UbotCoreService` partial dosyası sistemi:** `UbotCoreService` 10 partial dosyaya bölünmüştür. Yeni sorumluluk için uygun partial dosyayı düzenleyin veya yeni bir `UbotCoreService.<Alan>.cs` oluşturun. Tek dosyayı büyütmeyin.

### Yeni Feature Ekleme Kontrol Listesi

```
[ ] Features/<YeniFeature>/<YeniFeature>View.axaml         oluşturuldu
[ ] Features/<YeniFeature>/<YeniFeature>ViewModel.cs       oluşturuldu
[ ] FeatureViewFactory'ye kayıt eklendi
[ ] IUbotCoreService'e gerekli metot/event tanımlandı
[ ] UbotCoreService partial dosyasına implementasyon yazıldı
[ ] Ubot<YeniFeature>PluginService.cs oluşturuldu (gerekiyorsa)
[ ] AppState'e yeni state alanı eklendiyse MainWindowViewModel güncellendi
[ ] 1440×900 çözünürlükte görsel test yapıldı
[ ] Tema geçişinde görsel bütünlük korunuyor
```

### Servis Partial Dosyaları

| Partial Dosya | İçerik |
|---------------|--------|
| `UbotCoreService.cs` | Ana partial; bağımlılıklar, ctor |
| `UbotCoreService.Initialization.cs` | Core başlatma ve yükleme akışı |
| `UbotCoreService.Connection.cs` | Bağlantı yönetimi (connect/disconnect) |
| `UbotCoreService.AutoLogin.cs` | Otomatik giriş mantığı |
| `UbotCoreService.Actions.cs` | Bot start/stop, character load aksiyonları |
| `UbotCoreService.Map.cs` | Harita verisi, bölge bilgileri |
| `UbotCoreService.Icons.cs` | Oyun içi ikon çözümleme |
| `UbotCoreService.CommandCenter.cs` | Komut merkezi köprüsü |
| `UbotCoreService.SoundNotifications.cs` | Ses bildirim ayarları |
| `UbotCoreService.Dialogs.cs` | Dialog/popup yönetimi |
| `UbotCoreService.Helpers.cs` | Yardımcı metotlar |

---

## Config ve Veri Kuralları

- **Format:** `key{value}` — satır bazlı, boşluk yoktur.
- **Dosya uzantısı:** `.rs`
- **Konum:** `Build/User/` altında (build sürecinde silinmez, `git` tarafından izlenmez).
- **Yeni key naming:** `UBot.<Modül>.<Key>` kalıbı zorunludur.
  - ✅ `UBot.Skills.BuffDelay`
  - ❌ `BuffDelay`, `skills_buff_delay`, `UBot.skills.buffDelay`
- **Migration:** Mevcut key'i rename ediyorsanız `tools/config/config-key-migrations.json` dosyasını güncelleyin.
- **Hassas veri:** Config dosyalarına şifre veya auth token yazmayın.
- **Float format:** Her zaman `"."` (nokta) ondalık ayırıcısı kullanılır — `CultureInfo.CurrentCulture = "en-US"` garantisi.

### Sistem Config Key'leri

| Key | Tür | Kullanıcı | Açıklama |
|-----|-----|-----------|----------|
| `UBot.EnableCollisionDetection` | `bool` | `Kernel` | NavMeshApi aktifleştirme |
| `UBot.DisabledPlugins` | `string` | `ExtensionManager` | Disable listesi |

---

## Config Migration Sözleşmesi

`tools/config/config-key-migrations.json` dosyası, yeniden adlandırılan veya kaldırılan config key'lerinin kaydını tutar.

### Migration Şeması

```json
{
  "migrations": [
    {
      "id":             "benzersiz_migration_kimliği",
      "from":           "UBot.Modul.EskiKey",
      "to":             "UBot.Modul.YeniKey",
      "severity":       "warning",      // "warning" | "error"
      "configSeverity": "warning",      // config dosyasındaki eski key için
      "allowCodeUsage": false,          // true → eski key kod içinde yazılabilir (backward compat)
      "description":    "Değişiklik açıklaması."
    }
  ]
}
```

### Mevcut Kayıtlı Migration'lar (14 adet)

| ID | Eski Key | Yeni Key | Severity |
|----|---------|---------|----------|
| `general_queue_notification_typo` | `UBot.General.EnableQuqueNotification` | `UBot.General.EnableQueueNotification` | warning |
| `rusro_session_key` | `UBot.RuSro.session` | `UBot.RuSro.sessionId` | **error** |
| `network_bind_key` | `UBot.Network.Bind` | `UBot.Network.BindIp` | warning |
| `trade_selected_route_index_key` | `UBot.Trade.SelectedRouteList` | `UBot.Trade.SelectedRouteListIndex` | warning |
| `lure_stop_if_num_monster_type` | `UBot.Lure.NumMonsterType` | `UBot.Lure.StopIfNumMonsterType` | warning |
| `lure_stop_if_num_party_member` | `UBot.Lure.NumPartyMember` | `UBot.Lure.StopIfNumPartyMember` | warning |
| `lure_stop_if_num_party_member_dead` | `UBot.Lure.NumPartyMemberDead` | `UBot.Lure.StopIfNumPartyMemberDead` | warning |
| `lure_stop_if_num_party_members_on_spot` | `UBot.Lure.NumPartyMembersOnSpot` | `UBot.Lure.StopIfNumPartyMembersOnSpot` | warning |
| `sounds_alarm_unique_in_range_play` | `UBot.Sounds.PlayAlarmUnique` | `UBot.Sounds.PlayAlarmUniqueInRange` | warning |
| `sounds_alarm_unique_in_range_path` | `UBot.Sounds.PathAlarmUnique` | `UBot.Sounds.PathAlarmUniqueInRange` | warning |
| `sounds_unique_general_play` | `UBot.Sounds.PlayUniqueAlarm` | `UBot.Sounds.PlayUniqueAlarmGeneral` | warning |
| `sounds_unique_general_path` | `UBot.Sounds.PathUniqueAlarm` | `UBot.Sounds.PathUniqueAlarmGeneral` | warning |
| `sounds_captain_ivy_play` | `UBot.Sounds.PlayUniqueAlarmCaptain` | `UBot.Sounds.PlayUniqueAlarmCaptainIvy` | warning |
| `sounds_captain_ivy_path` | `UBot.Sounds.PathUniqueAlarmCaptain` | `UBot.Sounds.PathUniqueAlarmCaptainIvy` | warning |

### Migration Ekleme Kuralı

Mevcut bir config key'ini yeniden adlandırıyorsanız:

1. `tools/config/config-key-migrations.json`'a yeni `migration` nesnesi ekleyin.
2. `id` alanı repo genelinde benzersiz olmalıdır.
3. `severity: "error"` → kod içinde eski key kullanımını da kaldırın.
4. `allowCodeUsage: true` → geriye dönük uyumluluk yazması için eski key geçici olarak koda bırakılabilir.
5. Migration eklendikten sonra `Test-ConfigKeyMigrations.ps1 -RefreshInventory` çalıştırın.

---

## Build Çıktı ve Runtime Asset Notları

### Asset Kopyalama Akışı

```
repo: Dependencies/Languages/**          → Build/Data/Languages/**
repo: Dependencies/Scripts/Towns/*.rbs   → Build/Data/Scripts/Towns/*.rbs
repo: Plugins/*/plugin.manifest.json     → Build/Data/Plugins/<Assembly>.manifest.json
```

`Build/Data/Languages/` veya `Build/Data/Scripts/Towns/` eksikse, build script `git restore` ile geri yükler. **Bu dizinleri manuel silmeyin.**

`Dependencies/Languages/` klasörü **kasıtlı olarak** `build.ps1`'in `Copy-Item` döngüsünden hariç tutulmuştur; dil dosyaları ayrı bir mekanizmayla yönetilir.

### Kritik Çıktılar

| Dosya | Açıklama | x86 Zorunlu? |
|-------|----------|:---:|
| `Build/UBot.exe` | Ana executable | ✅ |
| `Build/Client.Library.dll` | Native hook DLL (C++) | ✅ |
| `Build/UBot.Updater.exe` | Güncelleme bileşeni | ✅ |
| `Build/UBot.Avalonia.dll` | UI katmanı | ✅ |
| `Build/Data/Plugins/*.manifest.json` | Runtime plugin meta | — |
| `Build/Data/Bots/*.dll` | Botbase assembly'leri | ✅ |

### TargetAssist Özel Durumu

`UBot.TargetAssist.dll`, solution build'inde `.sln` bağımlılık grafiğinin dışında kalabilir. Build script bunu algılar ve proje doğrudan derlenir:

```powershell
if (-not (Test-Path ".\Build\Data\Plugins\UBot.TargetAssist.dll")) {
    & $msBuildPath /restore /p:Configuration=$Configuration /p:Platform=x86 $targetAssistProjectPath
}
```

---

## Test ve Validasyon Beklentisi

Her kod değişikliğinin ardından aşağıdaki minimum validasyon yapılmalıdır.

### Her Zaman

```powershell
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart
```

Build başarılı olmalı. `build.log` uyarı için taranmalıdır.

### Core / Library Değişikliğinde

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

### Plugin / Botbase Değişikliğinde

```
1. Plugin load akışını doğrula (manifest hatasız yüklenmeli).
2. Enable → Disable → Enable döngüsünü test et.
3. Bağımlı plugin varsa bağımlılık zinciri çalışıyor mu kontrol et.
4. out-of-proc ise --plugin-host modu ayrıca test edilmeli.
```

### UI Değişikliğinde

```
1. Feature ekranı açılıyor mu?
2. Temel aksiyon butonları çalışıyor mu?
3. Tema geçişi görsel bütünlüğü bozuyor mu?
4. 1440×900 çözünürlükte overflow/layout kırılması var mı?
5. FeatureViewFactory mapping doğru mu?
```

### Config Değişikliğinde

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

### Script Komut Değişikliğinde

```
1. ScriptManager.RegisterCommandHandler() ve UnregisterCommandHandler() çiftleri tam mı?
2. IScriptCommand.IsBusy yönetimi hatasız mı?
3. Stop() çağrısı _stopRequested bayrağını doğru set ediyor mu?
```

---

## Belgeleme Politikası

Aşağıdaki değişikliklerde **hem `README.md` hem de `AGENTS.md` güncellenmelidir:**

| Değişiklik | README | AGENTS |
|-----------|:------:|:------:|
| Yeni plugin / botbase ekleme | ✅ | ✅ |
| Plugin silme | ✅ | ✅ |
| Plugin isolation / tier / sürüm değişikliği | ✅ | ✅ |
| Plugin bağımlılık değişikliği | ✅ | ✅ |
| Build script davranış değişikliği | ✅ | ✅ |
| UI shell veya service bridge mimarisinde büyük değişim | ✅ | ✅ |
| Config key ekleme / yeniden adlandırma / silme | ✅ | ✅ |
| Yeni CLI argümanı | ✅ | — |
| Yeni script komutu ekleme / kaldırma | — | ✅ |
| Yeni paket handler / opcode kaydı | — | ✅ |
| DI servis kaydı değişikliği | ✅ | ✅ |
| Test kapsamı genişletme | — | ✅ |
| Shutdown zinciri değişikliği | — | ✅ |
| Migration kaydı ekleme | — | ✅ |

---

## Sık Hata Kaynakları

| Hata | Kök Neden | Çözüm |
|------|-----------|-------|
| Plugin yüklenmiyor — manifest hatası | `pluginName` ≠ runtime `IPlugin.Name` | İkisini birebir eşitle |
| Plugin yüklenmiyor — sürüm uyumsuzluğu | `pluginVersion` ≠ `IPlugin.Version` | Manifest ve kod sürümünü senkronize et |
| `UBot.Quest` yüklenmiyor | Runtime adı `UBot.QuestLog`, manifest ile eşleşmeli | `Name => "UBot.QuestLog"` ve manifest `pluginName` doğrula |
| Plugin DLL bulunamıyor | DLL `Build/Data/Plugins/` altında değil | `csproj` çıktı yolunu kontrol et |
| Build başarısız — platform hatası | x86 dışı platform hedefi | Tüm `.csproj`'larda `PlatformTarget=x86` doğrula |
| Build başarısız — MSBuild bulunamadı | VS 2022 eksik veya PATH'te değil | VS Installer'dan MSBuild bileşenini kur |
| TargetAssist DLL eksik | Solution build'inde derlenmedi | Build script otomatik yeniden dener; log'a bak |
| Out-of-proc plugin başlamıyor | `UBot.exe` ve plugin DLL farklı dizinlerde | İkisi de `Build/` altında olmalı |
| Runtime data yüklenmiyor | `media.pk2` yolu yanlış | Profil ayarlarında Silkroad kurulum yolunu kontrol et |
| UI feature açılmıyor | `FeatureViewFactory` mapping eksik | Factory'ye yeni feature mapping ekle |
| Config okunmuyor | Format yanlış veya yanlış dizin | `key{value}` formatını ve `Build/User/` konumunu doğrula |
| Float sayı hataları | Kültür virgülle kaydedilmiş | `CultureInfo.CurrentCulture = "en-US"` doğrula |
| Protection handler çalışmıyor | `UnsubscribeAll()` çağrılmadı | `Disable()` içinde tüm handler'ların `UnsubscribeAll()`'ı çağırıldığını doğrula |
| Script komutu `IsBusy = true` takılı | `Stop()` flag'i set edilmedi | `_stopRequested` bayrağını kontrol et; `finally` bloğunda `IsBusy = false` yap |
| out-of-proc `AutoDungeon` script komutu kayıt dışı | `Enable()` / `Disable()` içinde `ScriptManager` çiftleri eksik | `RegisterCommandHandler` / `UnregisterCommandHandler` çiftlerini doğrula |
| Config migration tespit edilemiyor | Eski key migration.json'da yok | `tools/config/config-key-migrations.json`'a yeni giriş ekle |
| `sro_client` kill başarısız | Süreç farklı oturumda çalışıyor | Manuel olarak Task Manager'dan sonlandır |
| Script lint hatası | `.rbs` script sözdizim hatası | `UBot.Core.Tests` ile script doğrula |

---

*Bu dosya `UbotAva` repository kaynak kodu, plugin manifest'leri, interface tanımları, DI composition root, build script ve config migration sistemi tersine mühendislik edilerek oluşturulmuştur. Repository mimarisinde önemli bir değişiklik yapıldığında lütfen güncelleyin.*
