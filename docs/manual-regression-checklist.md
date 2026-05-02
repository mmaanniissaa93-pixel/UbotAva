# Manual Regression Checklist

> **Tarih:** 2026-05-02
> **Amaç:** Son stabilizasyon ve UI/Core boundary değişikliklerinden sonra runtime davranışlarını tek tek doğrulamak için pratik test listesi.

---

## 1. Build Guard

### Test 1.1: Guard Script MSBuild Öncesi Çalışıyor
**Adımlar:**
1. `Application/UBot.Avalonia/Features` altında herhangi bir `.cs` dosyasına `UBot.Core.RuntimeAccess` satırı ekle
2. `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart` çalıştır

**Beklenen Sonuç:**
- Guard script MSBuild öncesi çalışır
- RuntimeAccess ihlali tespit edilir
- Build durur, exit code 1 döner
- "UI RuntimeAccess guard failed" hatası görünür

**Başarısız Olursa Bakılacak:**
- `tools/verify-ui-runtimeaccess.ps1` (guard script doğru mu?)
- `build.ps1` satır 30-34 (guard entegrasyonu var mı?)

---

### Test 1.2: Guard Script Başarılı Durum
**Adımlar:**
1. Tüm UI dosyalarının temiz olduğundan emin ol
2. `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart` çalıştır

**Beklenen Sonuç:**
- Guard script: "PASS: No forbidden RuntimeAccess patterns found"
- MSBuild devam eder
- Build başarılı olur (0 hata)

**Başarısız Olursa Bakılacak:**
- `tools/verify-ui-runtimeaccess.ps1` (LureRecorderWindow TEMP exception doğru mu?)
- UI dosyalarında kalan RuntimeAccess kullanımları

---

## 2. ProxyConfigWindow

### Test 2.1: Mevcut Proxy/Bind IP Değerleri Yükleniyor
**Adımlar:**
1. `UBot.Network.BindIp` ve `UBot.Network.Proxy` config değerlerini `Build/User/` altında set et
2. Uygulamayı başlat
3. ProxyConfigWindow'u aç

**Beklenen Sonuç:**
- Bind IP değeri textbox'ta görünür
- Proxy ayarları doğru yüklenir

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.Connection.cs` (LoadProxyConfig bridge)
- `IUbotCoreService.cs` (LoadProxyConfig metodu)

---

### Test 2.2: Kaydet Sonrası Değerler Korunuyor
**Adımlar:**
1. ProxyConfigWindow'u aç
2. Bind IP veya Proxy değerlerini değiştir
3. Kaydet butonuna tıkla
4. Pencereyi kapat ve tekrar aç

**Beklenen Sonuç:**
- Yeni değerler kaydedilir
- Tekrar açıldığında değerler korunmuş olur
- `Build/User/*.rs` dosyasında değerler doğru yazılır

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.Connection.cs` (SaveProxyConfig bridge)
- `IUbotCoreService.cs` (SaveProxyConfig metodu)

---

### Test 2.3: Hata Durumunda Dialog Kapanmıyor
**Adımlar:**
1. ProxyConfigWindow'u aç
2. Geçersiz bir IP adresi gir
3. Kaydet butonuna tıkla

**Beklenen Sonuç:**
- Hata dialog'u görünür
- ProxyConfigWindow açık kalır (kapanmaz)
- Kullanıcı hatayı görür ve düzeltebilir

**Başarısız Olursa Bakılacak:**
- `Dialogs/ProxyConfigWindow.axaml.cs` (error handling)
- `IUbotCoreService.cs` (SaveProxyConfig try/catch)

---

## 3. ProfileSelectionWindow

### Test 3.1: Global Config Load Çalışıyor
**Adımlar:**
1. Uygulamayı başlat
2. ProfileSelectionWindow'u aç

**Beklenen Sonuç:**
- Mevcut profiller listelenir
- `UBot.Language` gibi global config değerleri doğru yüklenir

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.cs` (LoadGlobalConfig bridge)
- `IUbotCoreService.cs` (LoadGlobalConfig metodu)

---

### Test 3.2: Karakter Seçince Player Config Load Çalışıyor
**Adımlar:**
1. Bir karakter seç
2. ProfileSelectionWindow'u kapat
3. İlgili plugin/config ayarlarının yüklendiğini doğrula

**Beklenen Sonuç:**
- Seçilen karaktere ait config'ler yüklenir
- `RuntimeAccess.Player.Load(character)` çağrılır (service bridge üzerinden)

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.cs` (LoadPlayerConfig bridge)
- `IUbotCoreService.cs` (OnLoadCharacter metodu)

---

### Test 3.3: AutoLoginCharacter Değeri Doğru Okunuyor
**Adımlar:**
1. Bir profil/karakter seç
2. Config'de `UBot.AutoLoginCharacter` değerini kontrol et

**Beklenen Sonuç:**
- AutoLogin için doğru karakter ismi okunur
- Uygulama otomatik giriş yapar

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.AutoLogin.cs` (AutoLogin logic)
- `IUbotCoreService.cs` (AutoLogin metotları)

---

## 4. GeneralFeatureView

### Test 4.1: Critical Bool/Int Ayarlar Değişince Global.Set + Save Bridge Çalışıyor
**Adımlar:**
1. GeneralFeatureView'u aç
2. Herhangi bir bool/int ayarını değiştir (örn: `UBot.EnableCollisionDetection`)
3. Config dosyasını kontrol et

**Beklenen Sonuç:**
- `IUbotCoreService.SetGlobalConfig()` çağrılır
- Değer `RuntimeAccess.Global.Set()` ile kaydedilir
- `RuntimeAccess.Global.Save()` ile diske yazılır

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.cs` (SetGlobalConfig bridge)
- `IUbotCoreService.cs` (SetGlobalConfig metodu)

---

### Test 4.2: WARNING/ERROR Logları Görünür Kalıyor
**Adımlar:**
1. Uygulamayı başlat
2. Log ekranını aç
3. WARNING veya ERROR içeren bir işlem yap

**Beklenen Sonuç:**
- WARNING logları her zaman görünür (ShowErrorsOnly false iken)
- ERROR logları her zaman görünür
- `ShowErrorsOnly=true` iken sadece ERROR/WARNING görünür

**Başarısız Olursa Bakılacak:**
- `Services/AppState.cs` (ShouldShowLog metodu, satır 132-155)
- WARNING kontrolü: `upper.Contains("[WARNING]")` var mı?

---

### Test 4.3: DEBUG Varsayılan Kapalı
**Adımlar:**
1. Uygulamayı fresh başlat (config yoksa)
2. Log ekranını aç
3. DEBUG log üreten bir işlem yap

**Beklenen Sonuç:**
- DEBUG logları görünmez (ShowDebug varsayılan false)
- ShowDebug=true yapınca DEBUG logları görünür

**Başarısız Olursa Bakılacak:**
- `Services/AppState.cs` (satır 40: `ShowDebug = false`)
- `ShouldShowLog` metodu: `upper.Contains("[DEBUG]") && !ShowDebug` kontrolü

---

## 5. MainWindow

### Test 5.1: Language Değişince UI Anında Değişiyor
**Adımlar:**
1. MainWindow'u aç
2. Dil değiştir (Settings/General üzerinden)

**Beklenen Sonuç:**
- Tüm UI elemanları anında yeni dilde görünür
- `LanguageManager.Translate()` çağrılır

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.cs` (SetLanguage bridge)
- `IUbotCoreService.cs` (SetLanguage metodu)
- `Application/UBot.Avalonia/MainWindow.axaml.cs` (language change event)

---

### Test 5.2: Restart Sonrası Language Korunuyor
**Adımlar:**
1. Bir dil seç (örn: English)
2. Uygulamayı kapat
3. Tekrar aç

**Beklenen Sonuç:**
- Seçilen dil korunmuş olur
- `UBot.Language` config değeri doğru okunur

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.cs` (LoadGlobalConfig içinde language yükleme)
- `Build/User/*.rs` dosyasında `UBot.Language` değeri var mı?

---

### Test 5.3: Desktop Preferences Load/Save Çalışıyor
**Adımlar:**
1. MainWindow'da herhangi bir ayar değiştir (örn: theme)
2. Uygulamayı kapatıp aç

**Beklenen Sonuç:**
- Desktop preferences korunur
- `RuntimeAccess.Global.Set()` ve `Save()` çağrılır

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.cs` (desktop preferences bridge)
- `IUbotCoreService.cs` (relevant metotlar)

---

## 6. Protection Plugin Lifecycle

### Test 6.1: Enable → Disable → Enable Sonrası Handlerlar Tekrar Çalışıyor
**Adımlar:**
1. Protection plugin'ini Enable et
2. Disable et
3. Tekrar Enable et
4. Bir koruma event'inin çalıştığını doğrula

**Beklenen Sonuç:**
- Enable sonrası handler'lar subscribe olur
- Disable sonrası handler'lar unsubscribe olur
- Tekrar Enable sonrası handler'lar tekrar çalışır

**Başarısız Olursa Bakılacak:**
- `Plugins/UBot.Protection/Plugin.cs` (Initialize, Enable, Disable metotları)
- Handler'larda `UnsubscribeAll()` çağrılıyor mu?

---

### Test 6.2: Duplicate Event Oluşmuyor
**Adımlar:**
1. Protection plugin'ini Enable et
2. Disable et
3. Enable et (birden fazla kez tekrarla)
4. Bir event tetikle

**Beklenen Sonuç:**
- Event handler'ı sadece bir kez çağrılır
- Duplicate subscription yoktur

**Başarısız Olursa Bakılacak:**
- `Plugins/UBot.Protection/Handlers/*Handler.cs` (Subscribe/Unsubscribe logic)
- `Plugin.cs` (Disable içinde `UnsubscribeAll()` var mı?)

---

### Test 6.3: Disable Sonrası Handlerlar Event Almıyor
**Adımlar:**
1. Protection plugin'ini Enable et
2. Disable et
3. Bir koruma event'ini tetikle

**Beklenen Sonuç:**
- Disable sonrası handler'lar event almaz
- `UnsubscribeAll()` tüm event'leri temizler

**Başarısız Olursa Bakılacak:**
- `Plugin.cs` (Disable metodu)
- Her handler'ın `UnsubscribeAll()` metodu düzgün mü?

---

## 7. Botbase.Tick Fault Isolation

### Test 7.1: Training/Trade/Lure/Alchemy Tick Exception Testinde Bot Güvenli Duruyor
**Adımlar:**
1. Herhangi bir botbase'i başlat
2. Tick sırasında exception üret (test ortamında)
3. Bot davranışını izle

**Beklenen Sonuç:**
- Botbase.Tick() exception fırlatırsa fault isolation devreye girer
- `PluginFaultIsolationManager.TryExecute()` çalışır
- Restart policy uygulanır (standard tier için maxRetries kadar)
- Tüm denemeler başarısız olursa bot güvenli şekilde durur (critical tier)

**Başarısız Olursa Bakılacak:**
- `Library/UBot.Core/Bot.cs` (ExecuteStandardTierTick, ExecuteCriticalTierTick)
- `Library/UBot.Core/Runtime/PluginFaultIsolationManager.cs` (TryExecute)

---

### Test 7.2: Normal Tick Sırasında Manifest Warning Spam Yok
**Adımlar:**
1. Botbase'i başlat
2. Normal çalışma sırasında logları izle
3. Botbase.Tick() normal çalışıyor olsun

**Beklenen Sonuç:**
- Loglarda `Log.Warn($"Botbase [{botbaseName}] Tick() failed...")` spam'ı yok
- Warning sadece gerçek failure durumunda (tüm retry'ler bittikten sonra) yazılır

**Başarısız Olursa Bakılacak:**
- `Library/UBot.Core/Bot.cs` satır 110-175 (ResolveBotbaseTier, ExecuteStandardTierTick, ExecuteCriticalTierTick)
- Log.Warn sadece failure path'te mi?

---

## 8. LureRecorderWindow

### Test 8.1: Pencere Aç/Kapat/Aç Sonrası Duplicate Event Oluşmuyor
**Adımlar:**
1. LureRecorderWindow'u aç
2. Kapat
3. Tekrar aç
4. `OnPlayerMove` event'ini tetikle

**Beklenen Sonuç:**
- Event handler'ı sadece bir kez çağrılır
- Duplicate subscription yoktur
- `_eventsSubscribed` ve handler referansları doğru yönetiliyor

**Başarısız Olursa Bakılacak:**
- `Features/Lure/LureRecorderWindow.cs` (EnsureGlobalEventSubscriptions, TryCleanupEventSubscriptions)
- `_playerMoveHandler`, `_skillCastHandler` field'ları doğru mu?

---

### Test 8.2: OnPlayerMove / OnCastSkill Eventleri Kayıt Sırasında Çalışıyor
**Adımlar:**
1. LureRecorderWindow'u aç
2. Kayıt başlat
3. Karakteri hareket ettir (OnPlayerMove)
4. Skill cast et (OnCastSkill)

**Beklenen Sonuç:**
- Eventler tetiklenir
- Lure komutları kaydedilir
- `_activeRecorder` ve `HandleAutoPlayerMove`/`HandleAutoCast` çalışır

**Başarısız Olursa Bakılacak:**
- `Features/Lure/LureRecorderWindow.cs` (OnGlobalPlayerMove, OnGlobalCastSkill)
- Event subscription: `RuntimeAccess.Events.SubscribeEvent()`

---

### Test 8.3: Kapatınca Gerçek UnsubscribeEvent Çalışıyor
**Adımlar:**
1. LureRecorderWindow'u aç
2. Kapat
3. Event tetikle (OnPlayerMove veya OnCastSkill)

**Beklenen Sonuç:**
- `TryCleanupEventSubscriptions()` çağrılır
- `RuntimeAccess.Events.UnsubscribeEvent()` gerçekten çalışır
- `_playerMoveHandler` ve `_skillCastHandler` null olur
- `_eventsSubscribed = false` olur

**Başarısız Olursa Bakılacak:**
- `Features/Lure/LureRecorderWindow.cs` satır 277-289 (TryCleanupEventSubscriptions)
- `UnsubscribeEvent` çağrılıyor mu?

---

## 9. Training Config

### Test 9.1: Training Ayarları Kaydediliyor
**Adımlar:**
1. TrainingFeatureView'u aç
2. Herhangi bir ayarı değiştir (örn: checkCastBuffs)
3. Config dosyasını kontrol et

**Beklenen Sonuç:**
- `UBot.Training.*` config değerleri kaydedilir
- `RuntimeAccess.Player.Set()` çağrılır
- `SetPluginConfigAsync` üzerinden Save yapılır

**Başarısız Olursa Bakılacak:**
- `Services/UbotTrainingBotbaseService.cs` (ApplyPatch metodu)
- `Services/UbotCoreService.Actions.cs` (SetPluginConfigAsync)

---

### Test 9.2: Botu Kapatıp Açınca Ayarlar Korunuyor
**Adımlar:**
1. Training ayarlarını değiştir
2. Uygulamayı kapat
3. Tekrar aç ve karakter yükle
4. Ayarların yüklendiğini kontrol et

**Beklenen Sonuç:**
- Ayarlar korunmuş olur
- `RuntimeAccess.Player.Load(character)` ile config'ler yüklenir
- TrainingFeatureView doğru değerleri gösterir

**Başarısız Olursa Bakılacak:**
- `Services/UbotCoreService.Actions.cs` (OnLoadCharacter, LoadPlayerConfig)
- `Services/UbotTrainingBotbaseService.cs` (CreatePatch metodu)

---

### Test 9.3: Double-Save Kaldırıldıktan Sonra Kayıp Yok
**Adımlar:**
1. Training ayarlarını değiştir
2. Loglarda `Player.Save()` çağrılarını say (debug modda)
3. Ayarların düzgün kaydedildiğini doğrula

**Beklenen Sonuç:**
- `UbotTrainingBotbaseService.ApplyPatch()` içinde `Player.Save()` yok
- Save işlemi caller tarafında (`SetPluginConfigAsync` içinde `changed=true` ise) yapılır
- Double-save kaldırıldı, ama tek Save hala çalışıyor

**Başarısız Olursa Bakılacak:**
- `Services/UbotTrainingBotbaseService.cs` (ApplyPatch içinde Player.Save() var mı?)
- `Services/UbotCoreService.Actions.cs` (SetPluginConfigAsync, changed && Save logic)

---

## 10. Log Ekranı

### Test 10.1: DEBUG Varsayılan Gizli
**Adımlar:**
1. Fresh başlat (config yoksa)
2. Log ekranını aç
3. DEBUG log üreten işlemleri yap
4. Log listesini izle

**Beklenen Sonuç:**
- DEBUG logları görünmez (ShowDebug varsayılan false)
- `ShouldShowLog()` metodu `upper.Contains("[DEBUG]") && !ShowDebug` döner

**Başarısız Olursa Bakılacak:**
- `Services/AppState.cs` satır 40 (`ShowDebug = false`)
- satır 151 (`if (upper.Contains("[DEBUG]") && !ShowDebug)`)

---

### Test 10.2: Show Debug Açınca Debug Geliyor
**Adımlar:**
1. Log ekranını aç
2. "Show Debug" checkbox'ını işaretle
3. DEBUG log üreten işlemleri yap

**Beklenen Sonuç:**
- DEBUG logları görünür olur
- `OnShowDebugChanged` event'i `RefreshLogDisplay()` çağırır

**Başarısız Olursa Bakılacak:**
- `Services/AppState.cs` (OnShowDebugChanged partial metodu)
- `RefreshLogDisplay()` metodu doğru mu?

---

### Test 10.3: WARNING ve ERROR Her Zaman Görünüyor
**Adımlar:**
1. Log ekranını aç
2. ShowErrorsOnly = false iken WARNING/ERROR logları görünür mü?
3. ShowErrorsOnly = true iken sadece WARNING/ERROR görünüyor mu?

**Beklenen Sonuç:**
- WARNING ve ERROR her zaman görünür (ShowErrorsOnly false iken)
- `ShouldShowLog()` içinde `upper.Contains("[ERROR]") || upper.Contains("[FATAL]") || upper.Contains("[WARNING]")` her zaman true döner

**Başarısız Olursa Bakılacak:**
- `Services/AppState.cs` satır 136-137 (ERROR/FATAL/WARNING kontrolü)

---

### Test 10.4: Log Listesi Aşırı Spam Yapmıyor
**Adımlar:**
1. Uygulamayı uzun süre çalıştır
2. Log ekranını izle
3. Log sayısını kontrol et

**Beklenen Sonuç:**
- Log listesi max 2000 entry'yi geçmez
- `AppState._allLogs` listesi yönetilir
- Eski loglar temizlenir

**Başarısız Olursa Bakılacak:**
- `Services/AppState.cs` (AddLog metodu, maxLogs kontrolü)
- `_allLogs` listesinin boyutu yönetiliyor mu?

---

## 11. UbotPartyPluginService Wrapper Temizliği

### Test 11.1: Wrapper Metotlar UbotPluginServices.cs'de Yok
**Adımlar:**
1. `Application/UBot.Avalonia/Services/UbotPluginServices.cs` dosyasını aç
2. `ApplyLivePartySettingsFromConfig` ve `RefreshPartyPluginRuntime` ara

**Beklenen Sonuç:**
- Wrapper metotlar yok
- Sadece `UbotPartyPluginService.cs` içinde implementation var
- Çağrı yerleri doğrudan `UbotPartyPluginService.` metotlarını çağırıyor

**Başarısız Olursa Bakılacak:**
- `Services/UbotPluginServices.cs` (wrapper'lar silindi mi?)
- `Services/UbotPartyPluginService.cs` (implementation var mı?)

---

## Checkpoint Notları

- Tüm testler manuel olarak yapılmalıdır.
- Otomatik test altyapısı yoksa, bu checklist dokümanı referans alınır.
- Her test başarısız olursa belirtilen dosya/metot kontrol edilmelidir.
- Guard script (`tools/verify-ui-runtimeaccess.ps1`) her build öncesi otomatik çalışır.

---

*Bu doküman, UBot Avalonia stabilizasyon çalışmaları sonrası oluşturulmuştur. Sonraki test turlarında referans olarak kullanılmalıdır.*
