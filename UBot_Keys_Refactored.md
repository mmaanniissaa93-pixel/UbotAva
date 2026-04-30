# UBot Keys - DI Refactoring Sonrası Organizasyon

> Bu dosya yeni mimariye uygun reorganizasyonunu içerir.

---

## Yeni Mimariye Göre Constructor Injection Örneği

```csharp
// ESKİ YAPI (Statik Sınıf Erişimi)
var settings = Game.Settings.Get<IGlobalSettings>();

// YENİ YAPI (Constructor Injection)
public class MyPlugin : IPlugin
{
    private readonly IGlobalSettings _settings;
    private readonly IKernelRuntime _kernel;
    private readonly IPacketDispatcher _dispatcher;

    public MyPlugin(
        IGlobalSettings settings,
        IKernelRuntime kernel,
        IPacketDispatcher dispatcher)
    {
        _settings = settings;
        _kernel = kernel;
        _dispatcher = dispatcher;
    }
}
```

---

## Servis Sorumluluk Haritası

### 1. IGlobalSettings — Global UBot Konfigürasyonu

**Servis Sınıfı:** `UBot.Core.GlobalSettings`  
**Erişim:** `IGlobalSettings` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.General | Active | Genel UI ve davranış ayarları |
| UBot.General.AutoHidePendingWindow | Active | |
| UBot.General.AutoLoginAccountUsername | Active | |
| UBot.General.CharacterAutoSelect | Active | |
| UBot.General.CharacterAutoSelectFirst | Active | |
| UBot.General.CharacterAutoSelectHigher | Active | |
| UBot.General.Components | Active | |
| UBot.General.EnableAutomatedLogin | Active | |
| UBot.General.EnableLoginDelay | Active | |
| UBot.General.EnableQueueNotification | Active | |
| UBot.General.EnableQuqueNotification | Active | |
| UBot.General.EnableStaticCaptcha | Active | |
| UBot.General.EnableWaitAfterDC | Active | |
| UBot.General.HideOnStartClient | Active | |
| UBot.General.LoginDelay | Active | |
| UBot.General.Models | Active | |
| UBot.General.Models.Server | Active | |
| UBot.General.PacketHandler | Active | |
| UBot.General.PendingEnableQueueLogs | Active | |
| UBot.General.Properties | Active | |
| UBot.General.Properties.Resources | Active | |
| UBot.General.QueueLeft | Active | |
| UBot.General.StartBot | Active | |
| UBot.General.StaticCaptcha | Active | |
| UBot.General.StayConnected | Active | |
| UBot.General.TrayWhenMinimize | Active | |
| UBot.General.UseReturnScroll | Active | |
| UBot.General.Views | Active | |
| UBot.General.Views.View | Active | |
| UBot.General.WaitAfterDC | Active | |
| UBot.Profiles | Active | Profil listesi |
| UBot.Properties | Active | |
| UBot.Properties.Resources | Active | |
| UBot.Properties.Resources.app | Active | |
| UBot.SelectedProfile | Active | Aktif profil |
| UBot.Language | Active | Dil seçimi |
| UBot.Theme.Auto | Active | Otomatik tema |
| UBot.Translation | Active | Çeviri kaynağı |
| UBot.TranslationIndex | Active | |
| UBot.DisabledPlugins | Active | Devre dışı plugin'ler |
| UBot.Default | Active | Varsayılan değerler |
| UBot.Core | Active | Core ayarları |
| UBot.Core.Extensions | Active | |
| UBot.Core.Extensions.ListViewExtensions | Active | |
| UBot.Core.Extensions.NativeExtensions | Active | |
| UBot.Core.Event | Active | |
| UBot.Core.Components | Active | |
| UBot.Core.Components.Command | Active | |
| UBot.Core.Components.Scripting | Active | |
| UBot.Core.Components.Scripting.Commands | Active | |
| UBot.Core.Cryptography | Active | Şifreleme ayarları |
| UBot.Core.Game | Active | |
| UBot.Core.Tests | Active | Test modu |

---

### 2. IKernelRuntime — Çekirdek Çalışma Zamanı

**Servis Sınıfı:** `UBot.Core.Runtime.KernelRuntime`  
**Erişim:** `IKernelRuntime` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Core | Active | Kernel runtime kapsamında |
| UBot.Core.Network | Active | |
| UBot.Core.Network.Handler.Agent | Active | |
| UBot.Core.Network.Handler.Agent.Action | Active | |
| UBot.Core.Network.Handler.Agent.Alchemy | Active | |
| UBot.Core.Network.Handler.Agent.Character | Active | |
| UBot.Core.Network.Handler.Agent.CharacterSelection | Active | |
| UBot.Core.Network.Handler.Agent.Entity | Active | |
| UBot.Core.Network.Handler.Agent.Exchange | Active | |
| UBot.Core.Network.Handler.Agent.Job | Active | |
| UBot.Core.Network.Handler.Agent.Logout | Active | |
| UBot.Core.Network.Handler.Agent.Party | Active | |
| UBot.Core.Network.Handler.Agent.Quest | Active | |
| UBot.Core.Network.Handler.Agent.Skill | Active | |
| UBot.Core.Network.Handler.Agent.StorageBox | Active | |
| UBot.Core.Network.Handler.Agent.Teleport | Active | |
| UBot.Core.Network.Hooks | Active | |
| UBot.Core.Network.Hooks.Agent.Action | Active | |
| UBot.Core.Network.Protocol | Active | |
| UBot.Core.Objects | Active | |
| UBot.Core.Objects.Cos | Active | |
| UBot.Core.Objects.Cos.Ability | Active | |
| UBot.Core.Objects.Cos.Cos | Active | |
| UBot.Core.Objects.Cos.Fellow | Active | |
| UBot.Core.Objects.Cos.Growth | Active | |
| UBot.Core.Objects.Cos.JobTransport | Active | |
| UBot.Core.Objects.Cos.Transport | Active | |
| UBot.Core.Objects.Exchange | Active | |
| UBot.Core.Objects.Job | Active | |
| UBot.Core.Objects.Party | Active | |
| UBot.Core.Objects.Quests | Active | |
| UBot.Core.Objects.Region | Active | |
| UBot.Core.Objects.Skill | Active | |
| UBot.Core.Objects.Spawn | Active | |
| UBot.Core.Plugins | Active | Plugin yönetimi |
| UBot.Core.Client | Active | Client referansları |
| UBot.Core.Client.ReferenceObjects | Active | |
| UBot.Core.Client.ReferenceObjects.RefObj | Active | |
| UBot.Core.Client.ReferenceObjects.Region | Active | |
| UBot.Log | Active | Log servisi |
| UBot.Log.logEnabled | Active | |
| UBot.Log.Views | Active | |
| UBot.Statistics | Active | İstatistikler |
| UBot.Statistics.Stats | Active | |
| UBot.Statistics.Stats.Calculators | Active | |
| UBot.Statistics.Stats.Calculators.Live | Active | |
| UBot.Statistics.Stats.Calculators.Static | Active | |
| UBot.Statistics.Views | Active | |
| UBot.Loader.DebugMode | Active | Debug modu |

---

### 3. IGameSession — Oyun Oturumu Yönetimi

**Servis Sınıfı:** `UBot.Core.GameSession` (Shared instance)  
**Erişim:** `IGameSession` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Party | Active | Party yönetimi |
| UBot.Party.Accept | Active | |
| UBot.Party.AcceptAll | Active | |
| UBot.Party.AcceptList | Active | |
| UBot.Party.Allow | Active | |
| UBot.Party.AlwaysFollowPartyMaster | Active | |
| UBot.Party.AtTrainingPlace | Active | |
| UBot.Party.AutoJoin.ByName | Active | |
| UBot.Party.AutoJoin.ByTitle | Active | |
| UBot.Party.AutoJoin.Name | Active | |
| UBot.Party.AutoJoin.Title | Active | |
| UBot.Party.AutoPartyList | Active | |
| UBot.Party.Buffing | Active | |
| UBot.Party.Buffing.Groups | Active | |
| UBot.Party.Buffing.HideLowerLevelSkills | Active | |
| UBot.Party.Buffing.SelectedGroup | Active | |
| UBot.Party.Bundle | Active | |
| UBot.Party.Bundle.AutoParty | Active | |
| UBot.Party.Bundle.Commands | Active | |
| UBot.Party.Bundle.PartyMatching | Active | |
| UBot.Party.Bundle.PartyMatching.Network | Active | |
| UBot.Party.Bundle.PartyMatching.Objects | Active | |
| UBot.Party.Commands.ListenFromMaster | Active | |
| UBot.Party.Commands.ListenOnlyList | Active | |
| UBot.Party.Commands.PlayersList | Active | |
| UBot.Party.EXPAutoShare | Active | |
| UBot.Party.Leave | Active | |
| UBot.Party.Main.TabControl | Active | |
| UBot.Party.Matching.AutoAccept | Active | |
| UBot.Party.Matching.AutoReform | Active | |
| UBot.Party.Matching.LevelFrom | Active | |
| UBot.Party.Matching.LevelTo | Active | |
| UBot.Party.Matching.Purpose | Active | |
| UBot.Party.Matching.Title | Active | |
| UBot.Party.Properties | Active | |
| UBot.Party.Properties.Resources | Active | |
| UBot.Party.Subscribers | Active | |
| UBot.Party.Views | Active | |
| UBot.Quest | Active | Görev sistemi |
| UBot.Quest.Views | Active | |
| UBot.Quest.Views.Sidebar | Active | |
| UBot.QuestLog | Active | Görev günlüğü |
| UBot.QuestLog.TrackedQuests | Active | |

---

### 4. IPacketDispatcher — Ağ Paketi Dağıtımı

**Servis Sınıfı:** `UBot.Core.Network.PacketDispatcher`  
**Erişim:** `IPacketDispatcher` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Network.Bind | Active | Bind socket |
| UBot.Network.BindIp | Active | Bind IP |
| UBot.Network.Proxy | Active | Proxy ayarları |
| UBot.Packet | Active | Paket işleme |
| UBot.Core.Network | Active | Network katmanı |
| UBot.Core.Network.Hooks.Agent | Active | |

---

### 5. IProfileService — Profil Yönetimi

**Servis Sınıfı:** `UBot.Core.Services.ProfileService`  
**Erişim:** `IProfileService` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.BotName | Active | Bot adı |
| UBot.Server | Active | Sunucu bilgisi |
| UBot.Division | Active | Bölüm |
| UBot.DivisionIndex | Active | |
| UBot.Gateway | Active | Gateway |
| UBot.GatewayIndex | Active | |
| UBot.SilkroadDirectory | Active | Silkroad klasörü |
| UBot.SilkroadExecutable | Active | Executable yolu |
| UBot.Pk2Key | Active | PK2 anahtarı |
| UBot.Game.ClientType | Active | Client tipi |
| UBot.JCPlanet.login | Active | JCPlanet giriş |
| UBot.JSRO.login | Active | JSRO giriş |
| UBot.JSRO.token | Active | JSRO token |
| UBot.RuSro.accessToken | Active | RuSro token |
| UBot.RuSro.hwid | Active | HWID |
| UBot.RuSro.launcherid | Active | Launcher ID |
| UBot.RuSro.login | Active | |
| UBot.RuSro.password | Active | |
| UBot.RuSro.refreshToken | Active | |
| UBot.RuSro.session | Active | |
| UBot.RuSro.sessionId | Active | |
| UBot.RuSro.confirmationCode | Active | |
| UBot.Updater | Active |Updater ayarları |
| UBot.Updater.exe | Active | |
| UBot.exe | Active | UBot executable |
| UBot.DebugEnvironment | Active | Debug ortamı |

---

### 6. IShoppingService — Alışveriş Servisi

**Servis Sınıfı:** `UBot.Core.Services.ShoppingService`  
**Erişim:** `IShoppingService` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Shopping | Active | Alışveriş modülü |
| UBot.Shopping.Enabled | Active | |
| UBot.Shopping.Pickup | Active | |
| UBot.Shopping.RepairGear | Active | |
| UBot.Shopping.Sell | Active | |
| UBot.Shopping.SellPet | Active | |
| UBot.Shopping.Store | Active | |
| UBot.Shopping.StorePet | Active | |
| UBot.Inventory | Active | Envanter yönetimi |
| UBot.Inventory.AutoSort | Active | |
| UBot.Inventory.AutoUseAccordingToPurpose | Active | |
| UBot.Inventory.ItemsAtTrainplace | Active | |

---

### 7. IPickupService — Item Toplama Servisi

**Servis Sınıfı:** `UBot.Core.Services.PickupService`  
**Erişim:** `IPickupService` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Items.Pickup | Active | Item toplama |
| UBot.Items.Pickup.AnyEquips | Active | |
| UBot.Items.Pickup.Blue | Active | |
| UBot.Items.Pickup.DontPickupInBerzerk | Active | |
| UBot.Items.Pickup.DontPickupWhileBotting | Active | |
| UBot.Items.Pickup.EnableAbilityPet | Active | |
| UBot.Items.Pickup.Everything | Active | |
| UBot.Items.Pickup.Gold | Active | |
| UBot.Items.Pickup.JustPickMyItems | Active | |
| UBot.Items.Pickup.Quest | Active | |
| UBot.Items.Pickup.Rare | Active | |

---

### 8. IAlchemyService — Simya Servisi

**Servis Sınıfı:** `UBot.Core.Services.AlchemyService`  
**Erişim:** `IAlchemyService` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Alchemy | Active | Simya modülü |
| UBot.Alchemy.Bot | Active | |
| UBot.Alchemy.Bundle | Active | |
| UBot.Alchemy.Bundle.Attribute | Active | |
| UBot.Alchemy.Bundle.Enhance | Active | |
| UBot.Alchemy.Bundle.Magic | Active | |
| UBot.Alchemy.Client.ReferenceObjects | Active | |
| UBot.Alchemy.Extension | Active | |
| UBot.Alchemy.Helper | Active | |
| UBot.Alchemy.Subscriber | Active | |
| UBot.Alchemy.Views | Active | |
| UBot.Alchemy.Views.Settings | Active | |

---

### 9. IReferenceManager — Referans Nesne Yönetimi

**Servis Sınıfı:** `UBot.Core.ReferenceManager`  
**Erişim:** `IReferenceManager` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Alchemy.Client.ReferenceObjects | Active | RefObjCommon okuma |
| UBot.Core.Client.ReferenceObjects | Active | |
| UBot.Core.Client.ReferenceObjects.RefObj | Active | |
| UBot.Core.Client.ReferenceObjects.Region | Active | |

---

### 10. IScriptService / IScriptRuntime — Script Çalıştırma

**Servis Sınıfı:** `UBot.Core.Services.ScriptService`  
**Erişim:** `IScriptService` veya `IScriptRuntime` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.CommandCenter | Active | Komut merkezi |
| UBot.CommandCenter.Components | Active | |
| UBot.CommandCenter.Components.Command | Active | |
| UBot.CommandCenter.Enabled | Active | |
| UBot.CommandCenter.MappedEmotes | Active | |
| UBot.CommandCenter.Network.Handler | Active | |
| UBot.CommandCenter.Network.Hook | Active | |
| UBot.CommandCenter.Views | Active | |
| UBot.CommandCenter.Views.Controls | Active | |
| UBot.Trade | Active | Ticaret scriptleri |
| UBot.Trade.AttackThiefNpcs | Active | |
| UBot.Trade.AttackThiefPlayers | Active | |
| UBot.Trade.Bundle | Active | |
| UBot.Trade.BuyGoods | Active | |
| UBot.Trade.BuyGoodsQuantity | Active | |
| UBot.Trade.CastBuffs | Active | |
| UBot.Trade.Components | Active | |
| UBot.Trade.Components.Scripting | Active | |
| UBot.Trade.CounterAttack | Active | |
| UBot.Trade.MaxTransportDistance | Active | |
| UBot.Trade.MountTransport | Active | |
| UBot.Trade.ProtectTransport | Active | |
| UBot.Trade.RouteScriptList | Active | |
| UBot.Trade.RunTownScript | Active | |
| UBot.Trade.SelectedRouteList | Active | |
| UBot.Trade.SellGoods | Active | |
| UBot.Trade.TracePlayer | Active | |
| UBot.Trade.TracePlayerName | Active | |
| UBot.Trade.UseRouteScripts | Active | |
| UBot.Trade.Views | Active | |
| UBot.Trade.WaitForHunter | Active | |
| UBot.Trade.SelectedRouteListIndex | Active | |
| UBot.Training | Active | Eğitim scriptleri |
| UBot.Training.Areas | Active | |
| UBot.Training.Bot | Active | |
| UBot.Training.Bundle | Active | |
| UBot.Training.Bundle.Attack | Active | |
| UBot.Training.Bundle.Avoidance | Active | |
| UBot.Training.Bundle.Berzerk | Active | |
| UBot.Training.Bundle.Buff | Active | |
| UBot.Training.Bundle.Loop | Active | |
| UBot.Training.Bundle.Loot | Active | |
| UBot.Training.Bundle.Movement | Active | |
| UBot.Training.Bundle.PartyBuffing | Active | |
| UBot.Training.Bundle.Protection | Active | |
| UBot.Training.Bundle.Resurrect | Active | |
| UBot.Training.Bundle.Target | Active | |
| UBot.Training.Components | Active | |
| UBot.Training.Subscriber | Active | |
| UBot.Training.Views | Active | |
| UBot.Training.Views.Dialogs | Active | |
| UBot.Lure | Active | Hasım çekme |
| UBot.Lure.Bundle | Active | |
| UBot.Lure.Components | Active | |
| UBot.Lure.SelectedScriptPath | Active | |
| UBot.Lure.Views | Active | |
| UBot.Walkback.File | Active | Geri yürüme scripti |
| UBot.Lure.Walkback.File | Active | |
| UBot.Trade.RecorderScriptPath | Active | Kaydedilen script yolu |

---

### 11. ISkillRuntime — Beceri Çalışma Zamanı

**Servis Sınıfı:** `UBot.Core.Runtime.SkillRuntime`  
**Erişim:** `ISkillRuntime` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Skills | Active | Beceri yönetimi |
| UBot.Skills.Attacks | Active | |
| UBot.Skills.Buffs | Active | |
| UBot.Skills.Components | Active | |
| UBot.Skills.Main.TabControl | Active | |
| UBot.Skills.Subscriber | Active | |
| UBot.Skills.Views | Active | |
| UBot.Skills.numResDelay | Active | |
| UBot.Skills.numResRadius | Active | |
| UBot.Skills.ResurrectionSkill | Active | |
| UBot.Skills.selectedMastery | Active | |
| UBot.Skills.TeleportSkill | Active | |
| UBot.Desktop.Skills | Active | Desktop beceri ayarları |
| UBot.Desktop.Skills.AttackTypeIndex | Active | |
| UBot.Desktop.Skills.EnableAttacks | Active | |
| UBot.Desktop.Skills.EnableBuffs | Active | |
| UBot.Desktop.Skills.ImbueSkillId | Active | |

---

### 12. ISpawnRuntime — Spawn Çalışma Zamanı

**Servis Sınıfı:** `UBot.Core.Runtime.SpawnRuntime`  
**Erişim:** `ISpawnRuntime` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Area | Active | Alan yönetimi |
| UBot.Area.Radius | Active | |
| UBot.Area.Region | Active | |
| UBot.Area.X | Active | |
| UBot.Area.Y | Active | |
| UBot.Area.Z | Active | |
| UBot.Map | Active | Harita görüntüleme |
| UBot.Map.AutoSelectUnique | Active | |
| UBot.Map.Renderer | Active | |
| UBot.Map.Views | Active | |
| UBot.Map.Views.Dialog | Active | |
| UBot.Desktop.Map | Active | Desktop harita ayarları |
| UBot.Desktop.Map.AutoSelectUniques | Active | |
| UBot.Desktop.Map.CollisionDetection | Active | |
| UBot.Desktop.Map.EntityFilter | Active | |
| UBot.Desktop.Map.ResetToPlayerAt | Active | |
| UBot.Desktop.Map.ShowFilter | Active | |
| UBot.TargetAssist | Active | Hedef asistansı |
| UBot.TargetAssist.CustomPlayers | Active | |
| UBot.TargetAssist.Enabled | Active | |
| UBot.TargetAssist.IgnoreBloodyStormTargets | Active | |
| UBot.TargetAssist.IgnoredGuilds | Active | |
| UBot.TargetAssist.IgnoreSnowShieldTargets | Active | |
| UBot.TargetAssist.IncludeDeadTargets | Active | |
| UBot.TargetAssist.MaxRange | Active | |
| UBot.TargetAssist.OnlyCustomPlayers | Active | |
| UBot.TargetAssist.RoleMode | Active | |
| UBot.TargetAssist.TargetCycleKey | Active | |

---

### 13. IClientlessService — Clientless Bağlantı

**Servis Sınıfı:** `UBot.Core.Services.ClientlessService`  
**Erişim:** `IClientlessService` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.EnableCollisionDetection | Active | Collision algılama |
| UBot.showExitDialog | Active | Çıkış dialogu göster |
| UBot.ShowProfileDialog | Active | Profil dialogu göster |
| UBot.ShowSidebar | Active | Sidebar göster |
| UBot.AppUsageCount | Active | Kullanım sayısı |
| UBot.DonationReminderDisabled | Active | Bağış hatırlatıcı devre dışı |
| UBot.LastDonationReminderDate | Active | Son hatırlatıcı tarihi |

---

### 14. ILanguageService — Dil Servisi

**Servis Sınıfı:** `UBot.Core.Services.LanguageService`  
**Erişim:** `ILanguageService` interface'inin enjekte edilmesiyle

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Language | Active | Dil ayarı |
| UBot.Translation | Active | Çeviri kaynakları |

---

### 15. IProtectionRuntime — Koruma Sistemleri

**Servis Sınıfı:** `UBot.Core.Runtime.ProtectionRuntime`  
**Erişim:** `IProtectionRuntime` üzerinden erişim

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Protection | Active | Koruma sistemi |
| UBot.Protection.BadStatusSkill | Active | |
| UBot.Protection.check | Active | |
| UBot.Protection.checkAutoSummonAttackPet | Active | |
| UBot.Protection.checkDead | Active | |
| UBot.Protection.checkDurability | Active | |
| UBot.Protection.checkFullPet | Active | |
| UBot.Protection.checkLevelUp | Active | |
| UBot.Protection.checkNoArrows | Active | |
| UBot.Protection.checkNoHPPotions | Active | |
| UBot.Protection.checkNoMPPotions | Active | |
| UBot.Protection.checkReviveAttackPet | Active | |
| UBot.Protection.checkShardFatigue | Active | |
| UBot.Protection.checkStopBotOnReturnToTown | Active | |
| UBot.Protection.checkUseAbnormalStatePotion | Active | |
| UBot.Protection.checkUseBadStatusSkill | Active | |
| UBot.Protection.checkUseHGP | Active | |
| UBot.Protection.checkUseHPPotionsPlayer | Active | |
| UBot.Protection.checkUseMPPotionsPlayer | Active | |
| UBot.Protection.checkUsePetHP | Active | |
| UBot.Protection.checkUseSkillHP | Active | |
| UBot.Protection.checkUseSkillMP | Active | |
| UBot.Protection.checkUseUniversalPills | Active | |
| UBot.Protection.checkUseVigorHP | Active | |
| UBot.Protection.checkUseVigorMP | Active | |
| UBot.Protection.Components.Pet | Active | |
| UBot.Protection.Components.Player | Active | |
| UBot.Protection.Components.Town | Active | |
| UBot.Protection.HpSkill | Active | |
| UBot.Protection.MpSkill | Active | |
| UBot.Protection.Network.Handler | Active | |
| UBot.Protection.num | Active | |
| UBot.Protection.numDeadTimeout | Active | |
| UBot.Protection.numHPPotionsLeft | Active | |
| UBot.Protection.numMPPotionsLeft | Active | |
| UBot.Protection.numPetMinHGP | Active | |
| UBot.Protection.numPetMinHP | Active | |
| UBot.Protection.numPlayerHPPotionMin | Active | |
| UBot.Protection.numPlayerHPVigorPotionMin | Active | |
| UBot.Protection.numPlayerMPPotionMin | Active | |
| UBot.Protection.numPlayerMPVigorPotionMin | Active | |
| UBot.Protection.numPlayerSkillHPMin | Active | |
| UBot.Protection.numPlayerSkillMPMin | Active | |
| UBot.Protection.numShardFatigueMinToDC | Active | |
| UBot.Protection.Views | Active | |

---

### 16. IFileSystemService — Dosya Sistemi Servisi

**Servis Sınıfı:** `UBot.FileSystem.FileSystemService`  
**Erişim:** `IFileSystemService` üzerinden erişim

| Key | Durum | Not |
|-----|-------|-----|
| UBot.FileSystem | Active | Dosya sistemi |
| UBot.FileSystem.Local | Active | Yerel dosyalar |
| UBot.FileSystem.PackFile | Active | PK2 paket dosyaları |
| UBot.FileSystem.PackFile.Component | Active | |
| UBot.FileSystem.PackFile.Cryptography | Active | |
| UBot.FileSystem.PackFile.Struct | Active | |

---

### 17. INavMeshApiService — NavMesh API Servisi

**Servis Sınıfı:** `UBot.NavMeshApi.NavMeshService`  
**Erişim:** `INavMeshApiService` üzerinden erişim

| Key | Durum | Not |
|-----|-------|-----|
| UBot.NavMeshApi | Active | NavMesh API |
| UBot.NavMeshApi.Cells | Active | |
| UBot.NavMeshApi.Dungeon | Active | |
| UBot.NavMeshApi.Edges | Active | |
| UBot.NavMeshApi.Extensions | Active | |
| UBot.NavMeshApi.Helper | Active | |
| UBot.NavMeshApi.Mathematics | Active | |
| UBot.NavMeshApi.Object | Active | |
| UBot.NavMeshApi.Terrain | Active | |

---

### 18. UI/View Modelleri — Avalonia UI Katmanı

**Servis Sınıfı:** Avalonia View'lar (constructor'da IUbotCoreService injection)  
**Erişim:** Feature view'lar üzerinden

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Views | Active | Genel view'lar |
| UBot.Views.Controls | Active | Kontroller |
| UBot.Views.Controls.Cos | Active | COS kontrolleri |
| UBot.Views.Controls.Cos.CosController | Active | |
| UBot.Views.Dialog | Active | Dialog'lar |
| UBot.Chat | Active | Sohbet view'ı |
| UBot.Chat.Bundle | Active | |
| UBot.Chat.Bundle.Network | Active | |
| UBot.Chat.Views | Active | |
| UBot.AutoDungeon.Views | Active | Auto dungeon view |
| UBot.Sounds | Active | Ses ayarları |
| UBot.Sounds.PathAlarmUnique | Active | |
| UBot.Sounds.PathUniqueAlarm | Active | |
| UBot.Sounds.PathUniqueAlarmCaptain | Active | |
| UBot.Sounds.PathUniqueAlarmCerberus | Active | |
| UBot.Sounds.PathUniqueAlarmDemonShaitan | Active | |
| UBot.Sounds.PathUniqueAlarmGeneral | Active | |
| UBot.Sounds.PathUniqueAlarmLordYarkan | Active | |
| UBot.Sounds.PathUniqueAlarmTigerGirl | Active | |
| UBot.Sounds.PathUniqueAlarmUruchi | Active | |
| UBot.Sounds.PathUniqueAlarmCaptainIvy | Active | |
| UBot.Sounds.PathUniqueAlarmIsyutaru | Active | |
| UBot.Sounds.PathAlarmUniqueInRange | Active | |
| UBot.Sounds.PlayAlarmUnique | Active | |
| UBot.Sounds.PlayUniqueAlarm | Active | |
| UBot.Sounds.PlayUniqueAlarmCaptain | Active | |
| UBot.Sounds.PlayUniqueAlarmCerberus | Active | |
| UBot.Sounds.PlayUniqueAlarmDemonShaitan | Active | |
| UBot.Sounds.PlayUniqueAlarmGeneral | Active | |
| UBot.Sounds.PlayUniqueAlarmLordYarkan | Active | |
| UBot.Sounds.PlayUniqueAlarmTigerGirl | Active | |
| UBot.Sounds.PlayUniqueAlarmUruchi | Active | |
| UBot.Sounds.PlayUniqueAlarmCaptainIvy | Active | |
| UBot.Sounds.PlayUniqueAlarmIsyutaru | Active | |
| UBot.Sounds.PlayAlarmUniqueInRange | Active | |
| UBot.Sounds.RegexUniqueAlarmGeneral | Active | |

---

### 19. Training/Combat Runtime — Eğitim ve Savaş Sistemi

**Servis Sınıfı:** Training runtime bileşenleri  
**Erişim:** `ITrainingRuntime` üzerinden

| Key | Durum | Not |
|-----|-------|-----|
| UBot.Training | Active | Eğitim sistemi |
| UBot.Training.checkAttackWeakerFirst | Active | |
| UBot.Training.checkBerserkOnMonsterRarity | Active | |
| UBot.Training.checkBerzerkAvoidance | Active | |
| UBot.Training.checkBerzerkMonsterAmount | Active | |
| UBot.Training.checkBerzerkWhenFull | Active | |
| UBot.Training.checkBoxDimensionPillar | Active | |
| UBot.Training.checkBoxDontFollowMobs | Active | |
| UBot.Training.checkBoxUseReverse | Active | |
| UBot.Training.checkCastBuffs | Active | |
| UBot.Training.checkUseMount | Active | |
| UBot.Training.checkUseSpeedDrug | Active | |
| UBot.Training.Main.GroupBox.checkBoxUseReverse | Active | |
| UBot.Training.Main.groupBox1 | Active | |
| UBot.Training.Main.groupBox2 | Active | |
| UBot.Training.Main.groupBox3 | Active | |
| UBot.Training.Main.groupBox4 | Active | |
| UBot.Training.numBerzerkMonsterAmount | Active | |
| UBot.Training.radioCenter | Active | |
| UBot.Training.radioWalkAround | Active | |
| UBot.Lure | Active | Lure sistemi |
| UBot.Lure.Area | Active | Lure alanı |
| UBot.Lure.Area.Radius | Active | |
| UBot.Lure.Area.Region | Active | |
| UBot.Lure.Area.X | Active | |
| UBot.Lure.Area.Y | Active | |
| UBot.Lure.Area.Z | Active | |
| UBot.Lure.NoHowlingAtCenter | Active | |
| UBot.Lure.NumMonsterType | Active | |
| UBot.Lure.NumPartyMember | Active | |
| UBot.Lure.NumPartyMemberDead | Active | |
| UBot.Lure.NumPartyMembersOnSpot | Active | |
| UBot.Lure.SelectedMonsterType | Active | |
| UBot.Lure.StayAtCenter | Active | |
| UBot.Lure.StayAtCenterFor | Active | |
| UBot.Lure.StayAtCenterForSeconds | Active | |
| UBot.Lure.Stop | Active | |
| UBot.Lure.UseAttackingSkills | Active | |
| UBot.Lure.UseHowlingShout | Active | |
| UBot.Lure.UseNormalAttack | Active | |
| UBot.Lure.UseScript | Active | |
| UBot.Lure.WalkRandomly | Active | |
| UBot.Lure.StopIfNumMonsterType | Active | |
| UBot.Lure.StopIfNumPartyMember | Active | |
| UBot.Lure.StopIfNumPartyMemberDead | Active | |
| UBot.Lure.StopIfNumPartyMembersOnSpot | Active | |
| UBot.Avoidance | Active | Kaçınma sistemi |
| UBot.Avoidance.Avoid | Active | |
| UBot.Avoidance.Berserk | Active | |
| UBot.Avoidance.Prefer | Active | |

---

### 20. Special Plugins — Özel Plugin'ler

**Servis Sınıfı:** Plugin runtime'ları (out-of-proc)  
**Erişim:** Plugin interface'leri üzerinden

| Key | Durum | Not |
|-----|-------|-----|
| UBot.AutoDungeon | Active | Out-of-proc |
| UBot.AutoDungeon.Network | Active | |
| UBot.AutoDungeon.Views | Active | |
| UBot.PacketInspector | Active | Out-of-proc |
| UBot.PacketInspector.CaptureEnabled | Active | |
| UBot.PacketInspector.MaxRows | Active | |

---

## Legacy - Refactored / Taşınan Anahtarlar

Aşağıdaki anahtarlar statik sınıf yapısından yeni DI mimarisine taşınmıştır:

| Key | Eski Konum | Yeni Konum | Not |
|-----|------------|------------|-----|
| EventManager.* | Statik çağrı | IScriptEventBus üzerinden | Refactored |
| PacketManager.* | Statik çağrı | IPacketDispatcher üzerinden | Refactored |
| ProtocolRuntime.* | Statik sınıf | UBot.Protocol.ProtocolRuntime (merkezi) | Refactored |
| Game.* | Statik sınıf | IGameSession üzerinden | Refactored |
| Kernel.* | Statik sınıf | IKernelRuntime üzerinden | Refactored |
| Settings.* | Statik sınıf | IGlobalSettings üzerinden | Refactored |

---

## Namespace Güncellemeleri (UBot.Core.Abstractions)

Aşağıdaki tipler `UBot.Core.Abstractions` katmanına taşınmıştır:

| Eski Namespace | Yeni Namespace |
|----------------|----------------|
| `UBot.Core.*` | `UBot.Core.Abstractions` |
| `UBot.Core.Network` | `UBot.Core.Abstractions.Network` |
| `UBot.Core.Services` | `UBot.Core.Abstractions.Services` |

---

## Özet

| Servis Sorumluluğu | Interface | Anahtar Sayısı |
|-------------------|-----------|-----------------|
| Global Konfigürasyon | IGlobalSettings | ~60 |
| Kernel Runtime | IKernelRuntime | ~50 |
| Oyun Oturumu | IGameSession | ~50 |
| Paket Dağıtımı | IPacketDispatcher | ~10 |
| Profil Yönetimi | IProfileService | ~25 |
| Alışveriş | IShoppingService | ~10 |
| Item Toplama | IPickupService | ~15 |
| Simya | IAlchemyService | ~15 |
| Referans Yönetimi | IReferenceManager | ~5 |
| Script Çalıştırma | IScriptService | ~80 |
| Beceri Runtime | ISkillRuntime | ~25 |
| Spawn Runtime | ISpawnRuntime | ~40 |
| Clientless | IClientlessService | ~10 |
| Dil Servisi | ILanguageService | ~5 |
| Koruma Sistemi | IProtectionRuntime | ~45 |
| Dosya Sistemi | IFileSystemService | ~10 |
| NavMesh API | INavMeshApiService | ~10 |
| UI Layer | Feature ViewModels | ~80 |
| Training/Combat | ITrainingRuntime | ~60 |
| Special Plugins | IPlugin | ~10 |

**Toplam:** ~483 anahtar reorganize edildi.