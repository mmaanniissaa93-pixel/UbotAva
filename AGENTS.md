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
- [UI Değişikliklerinde Dikkat](#ui-değişikliklerinde-dikkat)
- [Config ve Veri Kuralları](#config-ve-veri-kuralları)
- [Build Çıktı ve Runtime Asset Notları](#build-çıktı-ve-runtime-asset-notları)
- [Test ve Validasyon Beklentisi](#test-ve-validasyon-beklentisi)
- [Belgeleme Politikası](#belgeleme-politikası)
- [Sık Hata Kaynakları](#sık-hata-kaynakları)

---

## Kesin Kurallar

Aşağıdaki kurallar **istisna kabul etmez:**

1. **Canonical build yolu `build.ps1`'dir.** `dotnet build UBot.sln` veya başka bir yol kullanmayın; MSBuild/x86 akışını atlayan buildler hatalı çıktı üretir.
2. **Platform varsayımı x86'dır.** Tüm `.csproj` ve `.vcxproj` dosyalarındaki `PlatformTarget` değeri `x86` olarak korunmalıdır. Farklı platform hedefi ile derleme denemesi native loader uyumsuzluğuna yol açar.
3. **Kullanıcı değişikliklerini geri alma yasaktır.** `git reset --hard`, `git checkout --` veya benzeri yıkıcı git komutları çalıştırılmamalıdır.
4. **Proje dosyalarına minimal dokunun.** `*.sln`, `*.csproj`, `*.vcxproj`, `*.Designer.cs` dosyalarına yalnızca zorunluysa değişiklik yapın ve değişikliği açık biçimde belgeleyin.
5. **Plugin manifest ile runtime metadata tutarlı kalmalıdır.** `pluginName`, `pluginVersion`, `isolation.mode` ve bağımlılık bildirimleri her zaman senkronda olmalıdır.
6. **Zorunlu out-of-proc plugin'lerin isolation mode'unu değiştirmeyin.** `UBot.AutoDungeon` ve `UBot.PacketInspector` her zaman `outproc` modda kalır.

---

## Hızlı Komut Referansı

### Build

```powershell
# Standart Debug build
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug

# Release build (otomatik başlatmadan)
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Release -DoNotStart

# Temiz build (user verilerini korur)
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
| `Application/UBot` | Process entrypoint, CLI argüman parse, shutdown orchestration |
| `Application/UBot.Avalonia` | Masaüstü UI (Avalonia 11), feature view'ları, service bridge |
| `Application/UBot.Updater` | `update_temp/update.zip` açıp güncelleme uygulayan bileşen |

### Library Katmanı

| Proje | Sorumluluk |
|-------|-----------|
| `Library/UBot.Core` | Kernel, event bus, plugin/botbase manager, config, scripting |
| `Library/UBot.Core.Abstractions` | `IPlugin`, `IBotbase`, `IExtension` — paylaşılan kontratlar |
| `Library/UBot.Core.Common` | Yardımcı tipler, extension methodlar |
| `Library/UBot.Core.Domain` | Domain DTO'ları, lightweight game-state kontratları |
| `Library/UBot.Core.GameState` | Oyun durumu modeli (karakter, NPC, item, skill) |
| `Library/UBot.Core.Network` | Network altyapısı, paket event'leri |
| `Library/UBot.Core.Services` | Uygulama düzeyinde servisler |
| `Library/UBot.FileSystem` | Yerel dosya sistemi + PK2 arşiv okuyucu (Blowfish şifreleme) |
| `Library/UBot.GameData` | RefObjCommon, RefSkill, RefQuest vb. Silkroad referans parser'ları |
| `Library/UBot.NavMeshApi` | NavMesh, dungeon, terrain, path altyapısı |
| `Library/UBot.Protocol` | Silkroad paket okuyucu/yazıcıları |
| `Library/UBot.Loader.Library` | C++ Detours tabanlı native `Client.Library.dll` |

### Extension Katmanı

| Tip | Çıktı Hedefi |
|-----|-------------|
| Plugin DLL'leri | `Build/Data/Plugins/` |
| Plugin manifest'leri | `Build/Data/Plugins/<AssemblyName>.manifest.json` |
| Botbase DLL'leri | `Build/Data/Bots/` |

---

## Plugin Geliştirme Kuralları

### Yeni Plugin Ekleme — Adım Adım

1. **Proje oluştur:**
   ```
   Plugins/<YeniPlugin>/<YeniPlugin>.csproj
   ```
   `PlatformTarget` = `x86`, `TargetFramework` = `net8.0` olmalı.

2. **Interface implement et:**
   ```csharp
   public class YeniPlugin : IPlugin
   {
       public string Name => "UBot.YeniPlugin";     // manifest ile TAM eşleşmeli
       public string Version => "1.0.0";            // manifest ile TAM eşleşmeli
       // ...
   }
   ```

3. **Manifest oluştur:**
   ```json
   // Plugins/YeniPlugin/plugin.manifest.json
   {
     "schemaVersion": 1,
     "pluginName": "UBot.YeniPlugin",
     "pluginVersion": "1.0.0",
     "capabilities": ["yeteneğiniz"],
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

4. **Çıktı yolunu ayarla:** `Build/Data/Plugins/` hedef olmalı.

5. **Solution'a ekle:** `UBot.sln` dosyasına projeyi dahil et.

6. **UI bağlantısı (gerekiyorsa):** `Application/UBot.Avalonia/Features/<YeniFeature>/` altına view/viewmodel ekle ve `FeatureViewFactory`'ye kaydet.

### Plugin Kaldırma

- Plugin DLL ve manifest'ini `Build/Data/Plugins/` çıktısından kaldır.
- `UBot.sln`'dan projeyi çıkar.
- `FeatureViewFactory` kaydını temizle.
- `UBot.DisabledPlugins` config key'inde referans varsa temizle.
- Bu AGENTS.md ve README.md'yi güncelle.

---

## Botbase Geliştirme Kuralları

### Yeni Botbase Ekleme

1. **Proje oluştur:**
   ```
   Botbases/<YeniBot>/<YeniBot>.csproj
   ```

2. **Interface implement et:**
   ```csharp
   public class YeniBot : IBotbase
   {
       public string Name => "UBot.YeniBot";
       public string Version => "1.0.0";
       public string Area => "HediyeAlanı";

       public void Start() { /* başlangıç mantığı */ }
       public void Tick() { /* döngü mantığı */ }
       public void Stop() { /* temizlik */ }
   }
   ```

3. **Çıktı yolunu ayarla:** `Build/Data/Bots/` hedef olmalı.

4. **Solution'a ekle:** `UBot.sln` içine dahil et.

5. **Servis mapping kontrolü:** `UbotCoreService.GetPluginsAsync()` botbase listesine dahil olup olmadığını doğrula.

---

## Manifest Uyumluluğu

`PluginContractManifestLoader`, her plugin yüklenirken aşağıdaki kontrolleri yapar. Bu kontrolleri geçemeyen plugin yüklenmez.

| Kontrol | Kural |
|---------|-------|
| `pluginName` | Runtime `IPlugin.Name` ile **birebir** eşleşmeli |
| `pluginVersion` | Runtime `IPlugin.Version` ile **birebir** eşleşmeli |
| `isolation.mode` | Yalnızca `"inproc"` veya `"outproc"` kabul edilir |
| `hostCompatibility` | Host sürümü `[minVersion, maxVersionExclusive)` aralığında olmalı |
| Bağımlılıklar | `required: true` olan bağımlılıklar yüklü ve sürüm aralığında olmalı |
| Yetenekler | Talep edilen `requiredCapabilities` sağlayan plugin mevcut olmalı |

### Zorunlu Out-of-Proc Pluginler

Bu iki plugin'in `isolation.mode` değeri **değiştirilemez:**

| Plugin | Tier | Neden outproc? |
|--------|------|----------------|
| `UBot.PacketInspector` | experimental | Paket yakalama süreç izolasyonu gerektirir |
| `UBot.AutoDungeon` | experimental | Dungeon routing crash riski ana süreci etkilememeli |

---

## UI Değişikliklerinde Dikkat

### Temel Kısıtlamalar

- **Pencere boyutu sabittir:** `1440×900`, `CanResize=False`. Layout hesaplamaları bu boyuta göre yapılmıştır; değiştirmeden önce tüm feature view'larını test edin.
- **UI-Core köprüsü:** View code-behind'e doğrudan core static sınıf çağrısı **yazmayın**. Her core erişimi `IUbotCoreService` üzerinden geçmelidir.
- **State yönetimi:** Global davranışlar (tema, dil, aktif profil) `MainWindowViewModel` + `AppState` modeline bağlı kalmalı.

### Yeni Feature Ekleme Kontrol Listesi

```
[ ] Features/<YeniFeature>/<YeniFeature>View.axaml   oluşturuldu
[ ] Features/<YeniFeature>/<YeniFeature>ViewModel.cs oluşturuldu
[ ] FeatureViewFactory'ye kayıt eklendi
[ ] IUbotCoreService'e gerekli metot/event tanımlandı
[ ] UbotCoreService'e implementasyon yazıldı
[ ] AppState'e yeni state alanı eklendiyse MainWindowViewModel güncellendi
[ ] 1440×900 çözünürlükte görsel test yapıldı
```

---

## Config ve Veri Kuralları

- **Format:** `key{value}` — satır bazlı, boşluk yoktur.
- **Dosya uzantısı:** `.rs`
- **Konum:** `Build/User/` altında (build sürecinde silinmez, `git` tarafından izlenmez).
- **Yeni key naming:** `UBot.<Modül>.<Key>` kalıbı zorunludur.
  - ✅ `UBot.Skills.BuffDelay`
  - ❌ `BuffDelay`, `skills_buff_delay`
- **Migration:** Mevcut key'i rename ediyorsanız `tools/config/config-key-migrations.json` dosyasını güncelleyin.
- **Hassas veri:** Config dosyalarına şifre veya auth token yazmayın.

---

## Build Çıktı ve Runtime Asset Notları

### Asset Kopyalama Akışı

```
repo: Dependencies/Languages/**      → Build/Data/Languages/**
repo: Dependencies/Scripts/Towns/*.rbs → Build/Data/Scripts/Towns/*.rbs
repo: Plugins/*/plugin.manifest.json → Build/Data/Plugins/<Assembly>.manifest.json
```

Eğer `Build/Data/Languages/` veya `Build/Data/Scripts/Towns/` eksikse, build script `git restore` ile bu dizinleri geri yükler. Manuel olarak silmeyin.

### Kritik Çıktılar

| Dosya | Açıklama |
|-------|----------|
| `Build/UBot.exe` | Ana executable |
| `Build/Client.Library.dll` | Native hook DLL — x86 zorunlu |
| `Build/UBot.Updater.exe` | Güncelleme bileşeni |
| `Build/Data/Plugins/*.manifest.json` | Runtime plugin meta |

---

## Test ve Validasyon Beklentisi

Her kod değişikliğinin ardından aşağıdaki minimum validasyon yapılmalıdır:

### Her Zaman

```powershell
# Build başarılı olmalı
powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart
```

### Core / Library Değişikliğinde

```powershell
dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj
```

### Plugin / Botbase Değişikliğinde

1. Plugin **load** akışını doğrula (manifest hatasız yüklenmeli).
2. Plugin **enable / disable** döngüsünü test et.
3. Bağımlı plugin varsa bağımlılık zinciri çalışıyor mu kontrol et.

### UI Değişikliğinde

1. Feature ekranı açılıyor mu?
2. Temel aksiyon butonları çalışıyor mu?
3. Tema geçişi görsel bütünlüğü bozuyor mu?
4. 1440×900 çözünürlükte overflow/layout kırılması var mı?

### Config Değişikliğinde

```powershell
powershell.exe -ExecutionPolicy Bypass .\tools\config\Test-ConfigKeyMigrations.ps1 -RefreshInventory
```

---

## Belgeleme Politikası

Aşağıdaki değişikliklerde **hem `README.md` hem de `AGENTS.md` güncellenmelidir:**

| Değişiklik | README | AGENTS |
|-----------|--------|--------|
| Yeni plugin / botbase ekleme | ✅ | ✅ |
| Plugin silme | ✅ | ✅ |
| Plugin isolation / tier değişikliği | ✅ | ✅ |
| Build script davranış değişikliği | ✅ | ✅ |
| UI shell veya service bridge mimarisinde büyük değişim | ✅ | ✅ |
| Config depolama veya migration model değişikliği | ✅ | ✅ |
| Yeni CLI argümanı | ✅ | — |
| Test kapsamı genişletme | — | ✅ |

---

## Sık Hata Kaynakları

| Hata | Kök Neden | Çözüm |
|------|-----------|-------|
| Plugin yüklenmiyor | Manifest `pluginName` ≠ runtime `IPlugin.Name` | İkisini birebir eşitle |
| Plugin yüklenmiyor | DLL `Build/Data/Plugins/` altında değil | csproj çıktı yolunu kontrol et |
| Build başarısız (platform hatası) | x86 dışı platform hedefi | `PlatformTarget=x86` doğrula |
| Build başarısız (MSBuild bulunamadı) | VS 2022 eksik veya PATH'te değil | VS Installer'dan MSBuild bileşenini kur |
| Out-of-proc plugin başlamıyor | UBot.exe ve plugin DLL farklı dizinlerde | İkisi de `Build/` altında olmalı |
| Runtime data yüklenmiyor | `media.pk2` yolu yanlış | Profil ayarlarında Silkroad kurulum yolunu kontrol et |
| UI feature açılmıyor | `FeatureViewFactory` kaydı eksik | Factory'ye yeni feature mapping ekle |
| Config okunmuyor | Key format yanlış | `key{value}` formatını ve dosya konumunu doğrula |
| `sro_client` kill başarısız | Süreç farklı oturumda çalışıyor | Manuel olarak Task Manager'dan sonlandır |
| Script lint hatası | `.rbs` script sözdizim hatası | `UBot.Core.Tests` ile script doğrula |

---

*Bu dosya `UbotAva` repository kaynak kodu, plugin manifest'leri ve build script tersine mühendislik edilerek oluşturulmuştur. Repository mimarisinde önemli bir değişiklik yapıldığında lütfen güncelleyin.*
