# UBot.Core Duzenleme Plani

Bu dokuman `Library/UBot.Core` analizi sonucunda bulunan kod hatalari, eksikler ve daha dogru dosya/kod yapisi icin uygulanabilir bir yol haritasidir.

## Mevcut Durum

- Canonical build komutu basarili:
  `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart`
- Core testleri basarili:
  `dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj`
- UBot.Core tarafinda derleme hatasi gorulmedi.
- Son dogrulamada build 0 uyari / 0 hata ile tamamlandi.
- UBot.Core yaklasik 334 kaynak dosyasi ve 33.900+ satirdan olusuyor.

## Uygulama Durumu

- Faz 1 tamamlandi: Kernel initialize/shutdown lifecycle idempotent hale getirildi ve application shutdown akisi Kernel updater task'ini kapatacak sekilde duzenlendi.
- Faz 2 tamamlandi: Proxy/socket connection state basarili baglanti sonrasi set ediliyor, gateway event typo icin geriye uyumlu dogru event eklendi ve socket catch bloklari context loglariyla guclendirildi.
- Faz 3 tamamlandi: PacketManager handler/hook duplicate guard, hook null/drop guvenligi ve AwaitCallback timeout cleanup davranisi eklendi.
- Faz 4 tamamlandi: ExtensionManager plugin enable/disable/unload akisi, paylasilan handler/hook kayitlarini baska aktif plugin kullaniyorsa koruyacak sekilde duzenlendi.
- Faz 5 tamamlandi: Config parser, invariant culture conversion, enum parse, atomic save ve PlayerConfig.Load(charName) davranisi duzeltildi.
- Faz 6 tamamlandi: Clientless keep-alive/relogin worker akisi tek worker garantisi ve cancellation token tabanli shutdown davranisiyla duzeltildi.
- Test kapsami genisletildi: PacketManager, ExtensionManager, Config ve ClientlessManager icin regresyon testleri eklendi; statik runtime state kullanan core testleri seri calisacak sekilde ayarlandi.
- Son dogrulama: `dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj` sonucunda 21/21 test basarili.
- Son dogrulama: `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart` sonucunda build basarili, 0 uyari, 0 hata.

## Oncelik Sirasi

1. Runtime lifecycle ve network state hatalarini duzelt.
2. Packet/plugin kayit sahipligi ve duplicate risklerini azalt.
3. Config parser ve save davranisini guclendir.
4. Core icindeki UI/WinForms bagimliligini ayir.
5. Test kapsamini kritik runtime davranislarina genislet.
6. Uzun vadede dosya/proje yapisini katmanlara bol.

## Faz 1 - Kernel ve Runtime Lifecycle

### Sorunlar

- `Kernel.Initialize()` tekrar cagrilirsa yeni updater task ve yeni `CancellationTokenSource` baslatabilir.
- Updater task icin public shutdown/cancel akisi yok.
- `Program.PerformFinalShutdown()` bot, proxy ve plugin hostlarini kapatiyor ama Kernel updater dongusunu durdurmuyor.

### Plan

- `Kernel` icine `_initialized`, `_updaterTask` ve lifecycle lock ekle.
- `Kernel.Initialize()` idempotent hale getir.
- `ComponentUpdaterAsync` icinde `Task.Delay(10, token)` kullan.
- `Kernel.Shutdown()` ekle:
  - updater token cancel edilsin,
  - updater task makul sure beklenerek kapatilsin,
  - token dispose edilsin.
- Application shutdown akisi icinde `Kernel.Shutdown()` cagrilsin.

### Beklenen Kazanc

- Uygulama kapanisinda background task sizintisi azalir.
- Tekrar initialize senaryolarinda cift tick/update calismasi engellenir.

### Dogrulama

- Build calistir.
- Uygulamayi ac/kapat smoke test yap.
- `OnTick` listener ve perf log davranisi gozlemlensin.

## Faz 2 - Proxy, Socket ve Network State

### Sorunlar

- Gateway/agent connection state flag'leri socket baglantisi kesinlesmeden set ediliyor.
- `Server.Connect` hata aldiginda caller tarafinda state yanlis kalabilir.
- `OnGatewayServerConntected` event adinda typo var.
- Socket katmaninda sessiz `catch` bloklari fazla.
- `Server.Connect` proxy baglantisini `.Result` ile blokluyor.

### Plan

- `Server.Connect` bool/result donecek sekilde duzenle.
- `Proxy.ConnectToGatewayserver()` ve `Proxy.ConnectToAgentserver()` state flag'lerini sadece basarili baglanti sonrasi veya `Server_OnConnected` icinde set et.
- `OnGatewayServerConntected` yerine dogru event adi `OnGatewayServerConnected` kullan.
- Geriye uyumluluk icin gecici olarak iki event de fire edilebilir:
  - `OnGatewayServerConnected`
  - `OnGatewayServerConntected`
- Socket catch bloklarini context bilgisiyle logla:
  - endpoint,
  - socket error,
  - packet dispatcher state,
  - operation name.
- `.Result` yerine kontrollu async/timeout modeli degerlendir.

### Beklenen Kazanc

- UI ve bot state ekranlari gercek baglanti durumunu daha dogru gosterir.
- Network sorunlari loglardan daha kolay teshis edilir.
- Deadlock/freeze riski azalir.

### Dogrulama

- Gateway baglanti basarisiz senaryosu.
- Agent disconnect/reconnect senaryosu.
- Clientless ve client mode smoke test.

## Faz 3 - PacketManager Handler/Hook Guvenligi

### Sorunlar

- `RegisterHandler` ve `RegisterHook` duplicate guard kullanmiyor.
- Hook zincirinde bir hook `null` packet dondururse sonraki hook null ile calisabilir.
- Callback listesi temizligi sadece callback call sirasinda yapiliyor; timeout callback'ler response gelmezse listede kalabilir.

### Plan

- Handler/hook kaydinda duplicate engelle:
  - ayni instance,
  - gerekirse ayni type + opcode + destination kombinasyonu.
- `CallHook` icinde hook sonrasi `packet == null` kontrolu ekle.
- Callback timeout sonrasi listeden temizleme stratejisi ekle:
  - periyodik cleanup,
  - veya `AwaitCallback` kapaninca manager'a bildirilen bir mekanizma.
- Unit test ekle:
  - duplicate register,
  - hook drop/null,
  - callback timeout cleanup.

### Beklenen Kazanc

- Cift initialize/plugin reload sonrasi duplicate packet isleme riski azalir.
- Hook'larin packet drop semantigi daha guvenli olur.
- Uzun sureli calismada callback listesi sismez.

### Dogrulama

- `dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj`
- Packet replay harness ile kritik opcode senaryolari.

## Faz 4 - Plugin Manager ve Isolation Sahipligi

### Sorunlar

- Ayni assembly icinde birden fazla plugin varsa handler/hook listeleri her plugin adina ayni listeyle baglanabilir.
- Enable/disable akisi baska plugin'in handler'larini etkileyebilir.
- Out-of-proc host restart task'i iptal edilebilir bir lifecycle'a bagli degil.

### Plan

- `ExtensionManager.GetExtensionsFromAssembly` icinde plugin-handler sahipligini netlestir.
- Handler/hook tiplerinin hangi plugin'e ait oldugunu belirlemek icin su seceneklerden biri uygulanabilir:
  - assembly basina tek plugin varsayimini manifest/loader ile zorlamak,
  - handler/hook uzerinde plugin owner metadata attribute'u kullanmak,
  - plugin manifest'e handler/hook ownership bilgisi eklemek.
- Enable/disable sirasinda sadece ilgili plugin'e ait handler/hook kayitlari eklenip kaldirilsin.
- Out-of-proc restart task'lari icin manager-level cancellation token ekle.
- `ExtensionManager.Shutdown()` tum pending restart task'larini iptal etsin.

### Beklenen Kazanc

- Plugin reload/disable davranisi daha ongorulebilir olur.
- Bir plugin'in hatasi diger plugin'in packet handler kaydini bozmaz.

### Dogrulama

- In-proc plugin enable/disable testi.
- Out-of-proc zorunlu plugin manifest testi:
  - `UBot.PacketInspector`
  - `UBot.AutoDungeon`
- Missing manifest ve version mismatch testleri.

## Faz 5 - Config Parser ve Storage Guvenligi

### Sorunlar

- `key{value}` parser ilk `}` karakterinde value'yu kesiyor.
- `Convert.ChangeType` current culture ile calisiyor.
- Save islemi atomic degil.
- `PlayerConfig.Load(string charName)` parametresi alinip kullanilmiyor.

### Plan

- Config parser icin kucuk, testli bir parse/serialize yardimcisi yaz.
- Value icinde `{` ve `}` desteklenecekse escape kurali belirle.
- `Convert.ChangeType(..., CultureInfo.InvariantCulture)` kullan.
- Enum parse icin case-insensitive opsiyon degerlendir.
- Save islemini temp dosyaya yazip replace edecek sekilde atomic hale getir.
- `PlayerConfig.Load(string charName)` icinde parametreyi kullan veya parametreyi kaldir:
  - Tercih: `charName` normalize edilip `ProfileManager.SelectedCharacter` ile tutarli hale getirilsin.

### Beklenen Kazanc

- Config dosyasi bozulma riski azalir.
- Farkli sistem culture ayarlarinda sayi/bool parse sorunlari azalir.
- API davranisi daha anlasilir olur.

### Dogrulama

- Config roundtrip unit testleri.
- Bos/malformed satir testleri.
- Brace iceren value testleri.
- Array ve enum parse testleri.

## Faz 6 - ClientlessManager Async Akisi

### Sorunlar

- `async void` event handler ve keep-alive worker kullaniliyor.
- Birden fazla keep-alive worker baslama riski var.
- Worker iptal mekanizmasi yok.

### Plan

- Keep-alive icin static `CancellationTokenSource` ve task referansi tut.
- `GoClientless()` ve `OnAgentServerConnected()` tek worker garantisi versin.
- Disconnect/shutdown sirasinda token cancel edilsin.
- Event handler icinde fire-and-forget gerekiyorsa wrapper ile exception logging garanti edilsin.

### Beklenen Kazanc

- Reconnect dongulerinde cift ping/worker riski azalir.
- Kapanis ve disconnect davranisi daha temiz olur.

### Dogrulama

- Clientless mode reconnect testi.
- Disconnect sonrasi keep-alive packet gonderiminin durdugunu dogrulama.

## Faz 7 - Core'dan UI Bagimliligini Ayirma

### Sorunlar

- UBot.Core `net8.0-windows` ve `UseWindowsForms=true`.
- `ListViewExtensions` Core icinde WinForms ve System.Drawing bagimliligi tasiyor.
- Aktif UI Avalonia oldugu icin Core'un UI framework'e bagli kalmasi uzun vadede maliyetli.

### Plan

- `Extensions/ListViewExtensions.cs` dosyasini Core disina tasima secenekleri:
  - `Application/UBot.LegacyWinForms` benzeri UI katmani,
  - veya yeni `Library/UBot.Core.WinFormsLegacy` projesi.
- Core icinde sadece icon byte/bitmap source gibi framework-neutral servisler kalsin.
- UBot.Core hedef framework'u uzun vadede `net8.0` yapilabilir mi degerlendir.
- Bu fazda `.csproj` etkisi olacagi icin dikkatli ilerle:
  - AGENTS kurali geregi csproj'a sadece zorunlu ise dokun.

### Beklenen Kazanc

- Core daha temiz ve test edilebilir olur.
- Avalonia UI ile core arasindaki sinir netlesir.
- Windows Forms bagimliligi runtime core'dan ayrilir.

### Dogrulama

- Build.
- UI icon/image kullanan eski ekranlar varsa manuel smoke test.
- Avalonia feature acilis testi.

## Faz 8 - Test Kapsamini Genisletme

### Oncelikli Testler

- `Config`:
  - parse,
  - save roundtrip,
  - malformed line,
  - escape/brace value,
  - invariant culture.
- `PacketManager`:
  - duplicate register,
  - hook null/drop,
  - callback timeout cleanup.
- `PluginContractManifestLoader`:
  - missing manifest,
  - name mismatch,
  - version mismatch,
  - unsupported isolation mode,
  - dependency version range.
- `ExtensionManager`:
  - enable/disable handler ownership,
  - out-of-proc required plugin validation.
- `Proxy`:
  - failed connect state flags,
  - gateway connected event names.
- `ClientlessManager`:
  - single keep-alive worker,
  - cancellation on disconnect.

## Faz 9 - Uzun Vadeli Dosya/Proje Yapisi

Onerilen katmanlama:

- `UBot.Core.Abstractions`
  - `IPlugin`, `IBotbase`, `IExtension`
  - packet interfaces
  - event contract'lari
  - shared DTO/entity modelleri
- `UBot.Core.Runtime`
  - `Kernel`
  - `Game`
  - runtime component manager'lar
- `UBot.Core.Network`
  - socket/protocol
  - packet manager
  - packet handlers/hooks
- `UBot.Core.Plugins`
  - manifest loader
  - plugin repository
  - fault isolation
  - out-of-proc host manager
- `UBot.GameData`
  - `ReferenceManager`
  - PK2/reference parser'lar
- `UBot.Core.WinFormsLegacy`
  - WinForms `ListViewExtensions`
  - legacy image list helpers

Bu bolme tek PR'da yapilmamali. Once test kapsami ve lifecycle/network duzeltmeleri tamamlanmali.

## Minimum Validation Checklist

Her faz sonunda:

1. `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart`
2. `dotnet test .\Tests\UBot.Core.Tests\UBot.Core.Tests.csproj`
3. Network/plugin etkisi varsa manuel smoke test.
4. Plugin manifest veya out-of-proc etkisi varsa load/enable/disable kontrolu.
5. UI etkisi varsa Avalonia feature acilis ve temel button aksiyonlari kontrolu.

## Onerilen Uygulama Sirasi

1. Kernel shutdown/idempotent init.
2. Gateway event typo + Proxy connection state duzeltmesi.
3. PacketManager duplicate/null hook testleri ve fixleri.
4. Config parser/save testleri ve fixleri.
5. Clientless keep-alive worker lifecycle.
6. ExtensionManager handler ownership netlestirme.
7. WinForms UI helper'larini Core disina tasima.
8. Daha buyuk proje katmanlama.

## Risk Notlari

- Network ve packet pipeline degisiklikleri runtime davranisini dogrudan etkiler; kucuk PR'lar halinde ilerlenmeli.
- Plugin ownership modeli manifest kontratini etkileyebilir; README/AGENTS guncellemesi gerekebilir.
- `UBot.Core.csproj` hedef framework veya WinForms bagimliligi degisirse downstream plugin/botbase build akisi kontrol edilmeli.
- Config serialization degisirse eski profile dosyalariyla backward compatibility korunmali.
