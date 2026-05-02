# Stabilization Checkpoint

> **Tarih:** 2026-05-02
> **Amaç:** UI/Core boundary ve runtime stabilization çalışmalarının proje içinde belgelenmesi.
> Sonraki AI/refactor turlarında aynı konuların tekrar yanlış ele alınmaması için referans dokümanı.

---

## 1. Amaç

- UI code-behind / ViewModel / Dialog dosyalarında doğrudan `RuntimeAccess` kullanılmamalı.
- Core erişimi `IUbotCoreService` üzerinden yapılmalı.
- `Services/UbotCoreService.*` bridge katmanı `RuntimeAccess` kullanabilir.
- `Ubot*PluginService` / `Ubot*BotbaseService` config adapter dosyaları şimdilik **allowed technical debt** olarak kabul edilir.

---

## 2. Tamamlanan İşler

- [x] **ProxyConfigWindow.axaml.cs** RuntimeAccess temizliği
- [x] **GeneralFeatureView.axaml.cs** RuntimeAccess temizliği
- [x] **ProfileSelectionWindow.axaml.cs** RuntimeAccess temizliği
- [x] **MainWindow.axaml.cs** RuntimeAccess temizliği
- [x] **verify-ui-runtimeaccess.ps1** guard script oluşturuldu
- [x] **build.ps1** içine guard entegrasyonu (MSBuild öncesi çalışır, ihlalde exit 1)
- [x] **LureRecorderWindow** duplicate subscription cleanup (UnsubscribeEvent eklendi)
- [x] **Protection handler** SubscribeAll/UnsubscribeAll lifecycle düzeltildi
- [x] **Botbase.Tick** fault isolation ve critical fallback uygulandı
- [x] **Botbase manifest warning spam** fix (Log.Warn sadece failure path'e taşındı)
- [x] **AppState** ShowDebug=false varsayılanı ayarlandı, WARNING logları her zaman görünür
- [x] **UbotTrainingBotbaseService** double-save fix (gereksiz Player.Save() kaldırıldı)
- [x] **UbotPartyPluginService** wrapper cleanup (UbotPluginServices.cs'deki gereksiz delegasyon wrapper'ları silindi)

---

## 3. Kalan Bilinçli Teknik Borç

### LureRecorderWindow (TEMP Exception)
- **Neden:** Realtime event subscription (`OnPlayerMove`, `OnCastSkill`) ve `Session.Player` readonly kullanımı var.
- **Durum:** Duplicate subscription cleanup düzeltildi (`TryCleanupEventSubscriptions` içine `UnsubscribeEvent` eklendi).
- **Plan:** Full bridge migration ayrı refactor olarak planlanacak.

### Services/Adapter RuntimeAccess Kullanımları
- **Neden:** `UbotCoreService.*` ve `Ubot*PluginService` / `Ubot*BotbaseService` dosyalarında `RuntimeAccess` kullanımı şimdilik allowed.
- **Durum:** Technical debt olarak sayılır, ancak büyük refactorgerektirir.
- **Kapsam:** 23 dosya, ~646 satır (toplam).
- **Plan:** Tek dosya tek patch şeklinde azaltılabilir.

### PacketInspector Feature Mapping
- **Durum:** Backlog'ta, acil değil.
- **Plan:** Zamanı geldiğinde ayrı refactor.

### CommandCenter
- **Durum:** Emotion/start debug flow kapsam dışı bırakıldı.
- **Plan:** Şimdilik dokunulmayacak.

---

## 4. Guard Kuralları

`tools/verify-ui-runtimeaccess.ps1` guard script'i aşağıdaki kuralları uygular:

### Tarama Alanları
- ✅ **Features/** (Recurse, `*.cs`)
- ✅ **Dialogs/** (`*.cs`)
- ✅ **ViewModels/** (`*.cs`)
- ✅ **MainWindow.axaml.cs**

### Hariç Tutulanlar
- ❌ **Services/** klasörü (tamamen hariç tutulur)
- ⚠️ **LureRecorderWindow.cs** (TEMP exception olarak allowed)

### Yasak Patternler
- `RuntimeAccess`
- `UBot.Core.RuntimeAccess`
- `using UBot.Core;` (UI dosyalarında)

### Allowed `using` Patternleri (UI dosyalarında)
- `using UBot.Core.Components;`
- `using UBot.Core.Plugins;`
- `using UBot.Core.Network;`

### Build Entegrasyonu
`build.ps1` içinde guard script MSBuild öncesi çalıştırılır:
```powershell
powershell.exe -ExecutionPolicy Bypass -File ".\tools\verify-ui-runtimeaccess.ps1"
if ($LASTEXITCODE -ne 0) {
    Write-Error "UI RuntimeAccess guard failed. Please fix violations before building."
    exit 1
}
```

---

## 5. Bundan Sonra Yeni UI Değişikliği Yaparken Kurallar

1. **UI dosyasına `RuntimeAccess` ekleme.**
   - Code-behind, ViewModel, Dialog dosyalarında `UBot.Core.RuntimeAccess` kullanımı yasaktır.

2. **`IUbotCoreService` üzerinden erişim.**
   - Yeni bir core işlevi gerekiyorsa, önce `IUbotCoreService.cs` içine minimal bridge metodu ekle.
   - `UbotCoreService` implementation içinde `RuntimeAccess` kullanarak implemente et.

3. **Async void handler'larda try/catch kullan.**
   - `await` edilen çağrıları `try/catch` ile koru.
   - Hata durumunda UI yıkılmasın.

4. **Log filtreleme kuralları.**
   - `[WARNING]` ve `[ERROR]` / `[FATAL]` logları her zaman görünür kalmalı.
   - `[DEBUG]` logları `ShowDebug=false` (varsayılan) ise gizlenmeli.
   - `ShowErrorsOnly` sadece hata/uyarıları göstermeli, diğerlerini gizlemeli.

---

## 6. Sonraki Önerilen Küçük İşler

### 1. LureRecorderWindow Full Event Bridge Migration
- **Scope:** `Application/UBot.Avalonia/Features/Lure/LureRecorderWindow.cs`
- **Hedef:** `RuntimeAccess.Events.SubscribeEvent` / `UnsubscribeEvent` çağrılarını `IUbotCoreService` event bridge üzerinden yap.
- **Risk:** Orta - event subscription mekanizması değişecek.
- **Not:** Duplicate cleanup yapıldı, şimdi full bridge migration zamanı.

### 2. Services/Adapter RuntimeAccess Azaltma
- **Scope:** Tek dosya tek patch şeklinde.
- **Önerilen sıra:**
  1. `UbotPluginServices.cs` (4 satır, en kolay)
  2. `UbotPluginConfigHelpers.cs` (7 satır)
  3. `UbotPluginStateAuxService.cs` (5 satır)
- **Hedef:** `IUbotCoreService` metotlarına geçiş.
- **Risk:** Düşük - küçük dosyalar.

### 3. PacketInspector Feature Mapping (Backlog)
- **Scope:** `Application/UBot.Avalonia/Features/` altına view/viewmodel ekle.
- **Hedef:** `FeatureViewFactory`'ye mapping ekle.
- **Risk:** Orta - UI tasarımı gerektirir.
- **Not:** Acil değil, backlog'ta kalsın.

### 4. Regression Checklist
- **Scope:** Küçük test senaryoları.
- **Hedef:** UI RuntimeAccess guard'ın çalıştığını doğrula.
- **Kontrol:** `tools/verify-ui-runtimeaccess.ps1` her build öncesi çalışmalı.

---

## 7. Kategori Dağılımı (Son Durum)

| Kategori | Dosya Sayısı | Satır Sayısı | Durum |
|----------|--------------|--------------|-------|
| A) UI Temiz | 0 | 0 | ✅ CLOSED |
| B) Allowed Bridge (UbotCoreService.*) | 9 | 243 | ✅ Allowed |
| C) Allowed Adapter (Services) | 14 | 403 | ⚠️ Technical Debt |
| D) TEMP Exception (LureRecorderWindow) | 1 | 6 | ⚠️ Migration Planned |
| E) Gerçek İhlal | 0 | 0 | ✅ CLOSED |

---

## 8. Guard Script Doğrulama

```powershell
> powershell.exe -ExecutionPolicy Bypass -File ".\tools\verify-ui-runtimeaccess.ps1"
Scanning UI files for forbidden RuntimeAccess patterns...
Project Root: C:\Users\auguu\Desktop\UbotAva

========================================
Scan Results: 0 violation(s) found
========================================

PASS: No forbidden RuntimeAccess patterns found in UI files.
```

**Sonuç:** Guard script başarıyla çalışıyor, 0 ihlal tespit edildi.

---

*Bu doküman, UBot Avalonia UI/Core boundary stabilizasyon çalışmalarının checkpoint'idir. Sonraki refactor turlarında referans olarak kullanılmalıdır.*
