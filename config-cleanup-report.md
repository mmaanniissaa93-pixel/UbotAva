# UBot Config Key Temizlik Raporu

> 815 anahtar üzerinde yapılan isimlendirme standartları ve tutarlılık analizi.

---

## 📊 Kategori Bazlı Grup (815 Anahtar)

| Kategori | Adet | Not |
|----------|------|-----|
| UBot.Core (RuntimeAccess, Events, Packets, Objects) | ~210 | En büyük kategori |
| UBot.Protocol | 78 | Handler, Hook, Commands, Legacy |
| UBot.Training | 33 | Bundle yapısı, check-* kuralları |
| UBot.Protection | 36 | check*, num* |
| UBot.General | 32 | AutoLogin, LoginDelay, Queue vb. |
| UBot.Sounds | 25 | Path*, Play*, Regex* |
| UBot.Party | 39 | Matching, Buffing, AutoJoin |
| UBot.Alchemy | 15 | Bundle, Enhance, Magic |
| UBot.Desktop | 19 | Theme, Language, ConnectionMode |
| UBot.Skills | 23 | Attacks_, Buffs, check* |
| UBot.Trade | 23 | RouteScript, Buy/Sell |
| UBot.Avalonia | 24 | Features, Services, Views |
| UBot.TargetAssist | 8 | RoleMode, MaxRange |
| UBot.RuSro | 9 | login, token, session |
| UBot.Navigation (Map, NavMeshApi) | 15 | Renderer, Dungeon, Terrain |
| Diğerleri | ~80 | FileSystem, Shopping, QuestLog, vb. |

---

## 🧹 Temizlik Raporu

### 1️⃣ Tutarsız Kısıtlamalar (Kritik)

| Mevcut | Sorun | Önerilen |
|--------|-------|----------|
| `UBot.JCPlanet.login` | 2 harfli kısaltma | `UBot.JCPlanet.Login` |
| `UBot.JSRO.login` | "JSRO" - tutarsız | `UBot.JSRO.Login` |
| `UBot.JSRO.token` | "JSRO" - tutarsız | `UBot.JSRO.Token` |
| `UBot.RuSro.session` | `sessionId` kullanılıyor | **Silinmeli** (migration var) |
| `UBot.Skills.Attacks_` | Sondaki alt çizgi | `UBot.Skills.Attacks` |

### 2️⃣ Standart Dışı Prefix (Orta)

| Prefix | Örnek | Sorun | Önerilen |
|--------|-------|-------|----------|
| `check*` | `UBot.Protection.checkDead` | Metod gibi görünüyor | `UBot.Protection.CheckDead` |
| `num*` | `UBot.Protection.numHPPotionsLeft` | Sayısal prefix | `UBot.Protection.HPPotionsLeft` |
| `radio*` | `UBot.Training.radioCenter` | Tutarsız naming | `UBot.Training.CenterMode` |

### 3️⃣ Çift/Artık Anahtarlar

> Bu anahtarlar migration ile yeni haline taşınmış, eski anahtarlar silinebilir:

| Eski Anahtar | Yeni Anahtar | Durum |
|--------------|--------------|-------|
| `UBot.Network.Bind` | `UBot.Network.BindIp` | Silinebilir |
| `UBot.Trade.SelectedRouteList` | `UBot.Trade.SelectedRouteListIndex` | Silinebilir |
| `UBot.Sounds.PlayAlarmUnique` | `UBot.Sounds.PlayAlarmUniqueInRange` | Silinebilir |
| `UBot.Sounds.PathAlarmUnique` | `UBot.Sounds.PathAlarmUniqueInRange` | Silinebilir |
| `UBot.Sounds.PlayUniqueAlarm` | `UBot.Sounds.PlayUniqueAlarmGeneral` | Silinebilir |
| `UBot.Sounds.PathUniqueAlarm` | `UBot.Sounds.PathUniqueAlarmGeneral` | Silinebilir |
| `UBot.Sounds.PlayUniqueAlarmCaptain` | `UBot.Sounds.PlayUniqueAlarmCaptainIvy` | Silinebilir |
| `UBot.Sounds.PathUniqueAlarmCaptain` | `UBot.Sounds.PathUniqueAlarmCaptainIvy` | Silinebilir |
| `UBot.RuSro.session` | `UBot.RuSro.sessionId` | Silinebilir |

> Not: `UBot.Lure.NumMonsterType` ve `UBot.Lure.StopIfNumMonsterType` farklı semantiklere sahip (int threshold vs bool toggle) - ikisi de korunmalı.

### 4️⃣ Tek/Kullanışsız Kategoriler

| Anahtar | Sorun | Öneri |
|---------|-------|-------|
| `UBot.Other` | Tek başına, muhtemelen kullanılmıyor | Kaldır veya birleştir |
| `UBot.Valid` | Tek başına, muhtemelen kullanılmıyor | Kaldır |
| `UBot.Default` | Tek başına | Kaldır |
| `UBot.DebugEnvironment` | Tek başına | Kaldır |
| `UBot.Dependency` | Tek başına | Kaldır |
| `UBot.Test` | "Test" tek başına gereksiz | Alt anahtarları koru, üstü kaldır |
| `UBot.Updater` | Tek başına | Kaldır |
| `UBot.Translation` | Tek başına | Kaldır |

### 5️⃣ İsimlendirme Hataları

| Mevcut | Önerilen |
|--------|----------|
| `UBot.Protection.check` | `UBot.Protection.Check` (tek başına, anlamsız) |
| `UBot.Protection.num` | `UBot.Protection.NumericSettings` |
| `UBot.Walkback.File` | `UBot.Walkback.FilePath` |
| `UBot.Training.radioCenter` | `UBot.Training.CenterMode` |
| `UBot.Training.radioWalkAround` | `UBot.Training.WalkAroundMode` |

### 6️⃣ Core İçindeki Özel Durumlar

`UBot.Core.RuntimeAccess.*` altında ~150+ API key var:

- **Doğru**: `UBot.Core.RuntimeAccess.Services.*` - servis erişim noktaları
- **Sorun**: Bir kısmı metod imzası gibi (örn: `UBot.Core.RuntimeAccess.Player.Position.XOffset.ToString`) - bunlar API dokümantasyonu, gerçek config key değil

---

## 📋 Önerilen Eylemler

| Öncelik | Eylem | Adet |
|---------|-------|------|
| 🔴 Yüksek | Eski migration anahtarlarını kaldır (Bind, SelectedRouteList, Sounds eski anahtarlar, session) | 10 |
| 🔴 Yüksek | `UBot.RuSro.session` → `sessionId` (zaten yapıldı) | 1 |
| 🟠 Orta | `check*` → `Check*` formatla | ~20 |
| 🟠 Orta | `num*` → anlamlı isimler | ~15 |
| 🟡 Düşük | Tek başına kategorileri birleştir veya sil | ~8 |
| 🟡 Düşük | `Attacks_` → `Attacks` | 1 |

**Toplam Potansiyel Temizlik**: ~55 anahtar

---

## ✅ Yapılmaması Gerekenler

- `UBot.Lure.NumMonsterType` - farklı semantics (int threshold)
- `UBot.Lure.NumPartyMember` - farklı semantics (int threshold)
- `UBot.Lure.NumPartyMemberDead` - farklı semantics (int threshold)
- `UBot.Lure.NumPartyMembersOnSpot` - farklı semantics (int threshold)

Bunlar `StopIf*` bool anahtarlarıyla birlikte kullanılıyor; ikisi de korunmalı.

---

*Raporing tarihi: 2026-04-30*