# UBot Regression Checklist (Plugin + Script)

## 0) Hazirlik
- [ ] `dotnet restore UBot.sln` basarili.
- [ ] `powershell.exe -ExecutionPolicy Bypass .\build.ps1 -Configuration Debug -DoNotStart` ile derleme alinabiliyor.
- [ ] Test icin en az 1 plugin DLL'i ve 1 bozuk/eksik komut iceren script dosyasi hazir.

## 1) Plugin Enable / Disable
- [ ] Baslangicta disabled olarak isaretli bir pluginin UI elementi (tab/menu) gorunmuyor.
- [ ] Ayni plugin acildiginda (enable) UI elementi olusuyor ve kullanilabilir oluyor.
- [ ] Plugin kapatildiginda (disable) UI elementi gizleniyor.
- [ ] Disable edilmis plugin icin karakter yuklendikten sonra `OnLoadCharacter` yan etkisi gorulmuyor.
- [ ] Plugin tekrar enable edildiginde yeniden initialize spam'i olmadan calisiyor (tekil initialize davranisi).

## 2) Runtime Load / Reload
- [ ] `LoadPluginFromFile` ile yeni plugin runtime yuklenebiliyor.
- [ ] Ayni plugin tekrar yuklenmeye calisildiginda duplicate eklenmiyor, "already loaded" uyari akisi calisiyor.
- [ ] Runtime yuklenen ve enabled plugin UI'ya dinamik olarak ekleniyor.
- [ ] Runtime yuklenen ama disabled plugin UI'ya eklenmiyor.
- [ ] `ReloadPlugin` cagrisi plugin enabled ise disable+enable dongusunu tamamlayip tekrar aktif hale getiriyor.
- [ ] `UnloadPlugin` sonrasi plugin UI'dan tamamen kaldiriliyor.

## 3) Script Parser Edge Cases
- [ ] Bos satirlar, `#` ve `//` satirlari scripti bozmeden atlanir.
- [ ] Fazladan bosluklu `move` komutlari (`move   x y z sx sy`) dogru parse edilir.
- [ ] Eksik argumanli `move` satiri uygulamayi dusurmez; satir guvenli sekilde gecilir.
- [ ] Gecersiz sayisal degerli `move` satiri uygulamayi dusurmez; satir guvenli sekilde gecilir.
- [ ] Script son satira esit index durumunda out-of-range/exception olusmaz.

## 4) Kisa Kabul Kriteri
- [ ] Uygulama hicbir adimda crash olmuyor.
- [ ] Plugin durum gecisleri UI ile tutarli.
- [ ] Script parser bozuk satirlarda fail-safe davranis gosteriyor.

