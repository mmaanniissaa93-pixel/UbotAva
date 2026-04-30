# Dependency Injection Refactoring Design

**Date:** 2026-04-30  
**Version:** 2.0 (Revised)  
**Scope:** Clean DI Architecture Migration  
**Strategy:** Hard cut, composition root pattern, high-frequency first  

---

## Executive Summary

Remove static class dependencies (`Game`, `Kernel`, `GlobalConfig`, `PlayerConfig`, `EventManager`, `PacketManager`) and implement proper dependency injection using `Microsoft.Extensions.DependencyInjection`. Target: 100% constructor injection, zero static service locator, fully testable architecture.

**Approach:** Hard cut (no façades), composition root (single bootstrap point), instance-based implementations.

---

## Current State Problems

1. **Static everywhere** — Hundreds of direct references to `Game.`, `Kernel.`, `GlobalConfig.`, `PlayerConfig.` make unit testing impossible.
2. **No clear dependencies** — Silent coupling, hard to trace what each class needs.
3. **ServiceRuntime static** — Current service locator only handles some services, inconsistently.
4. **Initialization chaos** — Multiple initialization paths, unclear lifecycle.
5. **Testing nightmare** — Cannot mock global state, integration tests only.

---

## Proposed Architecture

### Project Structure

```
Library/
├── UBot.Core.Abstractions/          ← Interface definitions
│   ├── IGameSession.cs
│   ├── IKernelRuntime.cs
│   ├── IGlobalSettings.cs
│   ├── IPlayerSettings.cs           ← Phase 1'e alındı (Phase 2'den taşındı)
│   ├── IPacketDispatcher.cs
│   ├── IScriptEventBus.cs
│   └── Services/ (existing)
│
├── UBot.Core/                       ← Implementation + business logic
│   ├── Runtime/
│   │   ├── GameSession.cs           (formerly Game static)
│   │   ├── KernelRuntime.cs         (formerly Kernel static)
│   │   └── CoreGameStateRuntimeContext.cs (refactored)
│   ├── Config/
│   │   ├── GlobalSettings.cs        (formerly GlobalConfig static)
│   │   └── PlayerSettings.cs        (formerly PlayerConfig static)
│   ├── Network/
│   │   └── PacketDispatcher.cs      (formerly PacketManager static)
│   ├── Event/
│   │   └── ScriptEventBus.cs        (formerly EventManager static)
│   ├── Components/
│   │   ├── ClientManager.cs         (refactored - constructor injection)
│   │   ├── ClientlessManager.cs     (refactored)
│   │   └── SkillManager.cs          (refactored)
│   └── ...
│
├── UBot.Protocol/
│   ├── Services/
│   │   ├── ProtocolServices.cs      (refactored - grouped protocol deps)
│   │   └── ...
│   └── ...
│
└── UBot.Core.Bootstrap/             ← NEW: Composition root
    ├── GameServiceCollectionExtensions.cs
    ├── ServiceProviderFactory.cs
    └── BootstrapConfiguration.cs
```

---

## Entry Point — Bootstrap Bağlantısı

`ServiceProviderFactory.CreateServices()` uygulama başlangıcında tek bir noktadan çağrılır.

**Avalonia (App.xaml.cs):**
```csharp
public override void OnFrameworkInitializationCompleted()
{
    var provider = ServiceProviderFactory.CreateServices();
    // DataContext veya DI container'a aktar
    base.OnFrameworkInitializationCompleted();
}
```

**WinForms (Program.cs):**
```csharp
static void Main()
{
    var provider = ServiceProviderFactory.CreateServices();
    Application.Run(new MainForm(provider.GetRequiredService<IKernelRuntime>()));
}
```

> **Kural:** `ServiceProviderFactory.CreateServices()` yalnızca bu entry point'te çağrılır. Başka hiçbir yerde `new ServiceCollection()` oluşturulmaz.

---

## Interfaces (UBot.Core.Abstractions)

**IGameSession** — replaces Game static
- Properties: `ClientType`, `Player`, `Started`, `Ready`, `Clientless`, `Port`, `ReferenceManager`
- Methods: `Initialize()`, `Start()`, `ShowNotification()`, `InitializeArchiveFiles()`

**IKernelRuntime** — replaces Kernel static
- Properties: `Proxy`, `Bot`, `Language`, `LaunchMode`, `TickCount`, `BasePath`, `EnableCollisionDetection`, `Debug`
- Methods: `Initialize()`, `StartAsync()`, `Shutdown()`

**IGlobalSettings** — replaces GlobalConfig static
- Methods: `Load()`, `Exists()`, `Get<T>()`, `GetEnum<T>()`, `Set<T>()`, `GetArray<T>()`

**IPlayerSettings** — replaces PlayerConfig static  
- Methods: `Get<T>()`, `GetEnum<T>()`, `Set<T>()`, `Save()`
- **Not:** `CoreGameStateRuntimeContext` buna bağımlı olduğu için Phase 1'e alındı.

**IPacketDispatcher** — replaces PacketManager static
- Methods: `RegisterHandler()`, `RemoveHandler()`, `RegisterHook()`, `RemoveHook()`, `SendPacket()`, `HandlePacket()`

**IScriptEventBus** — replaces EventManager static
- Methods: `SubscribeEvent()`, `UnsubscribeEvent()`, `RaiseEvent()`, `ClearSubscribers()`

---

## Implementations

| Interface | Implementation | Lifetime | Not |
|-----------|---------------|----------|-----|
| IGameSession | GameSession | Singleton | Eski `Game` static |
| IKernelRuntime | KernelRuntime | Singleton | Eski `Kernel` static |
| IGlobalSettings | GlobalSettings | Singleton | Eski `GlobalConfig` static |
| IPlayerSettings | PlayerSettings | Singleton | Eski `PlayerConfig` static |
| IPacketDispatcher | PacketDispatcher | Singleton | IDisposable implemente eder |
| IScriptEventBus | ScriptEventBus | Singleton | IDisposable implemente eder |

---

## IDisposable Stratejisi

`PacketDispatcher` ve `ScriptEventBus` timer, event subscription veya connection tutabilir. Bunlar `IDisposable` implemente eder ve uygulama kapanışında dispose edilir.

```csharp
// ServiceProviderFactory.cs
public static class ServiceProviderFactory
{
    private static ServiceProvider? _provider;

    public static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddGameRuntime();
        _provider = services.BuildServiceProvider();
        return _provider;
    }

    // Uygulama kapanışında entry point'ten çağrılır
    public static void Dispose()
    {
        _provider?.Dispose();
        _provider = null;
    }
}
```

**Entry point kapanış örneği (WinForms):**
```csharp
Application.ApplicationExit += (_, _) => ServiceProviderFactory.Dispose();
```

**IDisposable implemente eden servisler:**
```csharp
public sealed class PacketDispatcher : IPacketDispatcher, IDisposable
{
    public void Dispose()
    {
        // Timer durdur, handler'ları temizle, bağlantıları kapat
    }
}

public sealed class ScriptEventBus : IScriptEventBus, IDisposable
{
    public void Dispose()
    {
        // Subscriber listesini temizle
    }
}
```

---

## Composition Root

**ServiceProviderFactory.cs**
```csharp
public static class ServiceProviderFactory
{
    private static ServiceProvider? _provider;

    public static IServiceProvider CreateServices()
    {
        var services = new ServiceCollection();
        services.AddGameRuntime();
        _provider = services.BuildServiceProvider();
        return _provider;
    }

    public static void Dispose()
    {
        _provider?.Dispose();
        _provider = null;
    }
}
```

**GameServiceCollectionExtensions.cs**
```csharp
public static IServiceCollection AddGameRuntime(this IServiceCollection services)
{
    // Core runtime
    services.AddSingleton<IGlobalSettings, GlobalSettings>();
    services.AddSingleton<IPlayerSettings, PlayerSettings>();       // Phase 1'de eklendi
    services.AddSingleton<IKernelRuntime, KernelRuntime>();
    services.AddSingleton<IGameSession, GameSession>();
    services.AddSingleton<IPacketDispatcher, PacketDispatcher>();
    services.AddSingleton<IScriptEventBus, ScriptEventBus>();

    // Game state
    services.AddSingleton<IGameStateRuntimeContext, CoreGameStateRuntimeContext>();

    // Protocol services
    services.AddSingleton<IClientLaunchPolicy, ClientLaunchPolicyService>();
    services.AddSingleton<IClientNativeRuntime, ClientNativeRuntimeAdapter>();
    services.AddSingleton<IClientConnectionRuntime, ClientConnectionRuntimeAdapter>();

    return services;
}
```

---

## Constructor Injection — High-Frequency Classes

**CoreGameStateRuntimeContext**
```csharp
public class CoreGameStateRuntimeContext : IGameStateRuntimeContext
{
    private readonly IGameSession _game;
    private readonly IKernelRuntime _kernel;
    private readonly IGlobalSettings _globalSettings;
    private readonly IPlayerSettings _playerSettings; // Phase 2'den Phase 1'e taşındı

    public CoreGameStateRuntimeContext(
        IGameSession game,
        IKernelRuntime kernel,
        IGlobalSettings globalSettings,
        IPlayerSettings playerSettings)
    {
        _game = game;
        _kernel = kernel;
        _globalSettings = globalSettings;
        _playerSettings = playerSettings;
    }
}
```

**ClientManager**
```csharp
public class ClientManager : IDisposable
{
    private readonly IClientLaunchPolicy _launchPolicy;
    private readonly IClientNativeRuntime _nativeRuntime;
    private readonly IClientLaunchConfigProvider _configProvider;

    public ClientManager(
        IClientLaunchPolicy launchPolicy,
        IClientNativeRuntime nativeRuntime,
        IClientLaunchConfigProvider configProvider)
    { ... }
}
```

**ClientlessManager**
```csharp
public class ClientlessManager
{
    private readonly IGameSession _game;
    private readonly IKernelRuntime _kernel;
    private readonly IScriptEventBus _eventBus;

    public ClientlessManager(
        IGameSession game,
        IKernelRuntime kernel,
        IScriptEventBus eventBus)
    { ... }
}
```

**ProtocolServices**
```csharp
public class ProtocolServices
{
    private readonly IGameSession _game;
    private readonly IKernelRuntime _kernel;
    private readonly IPacketDispatcher _packetDispatcher;
    private readonly IScriptEventBus _eventBus;
    private readonly IGlobalSettings _globalSettings;

    public ProtocolServices(
        IGameSession game,
        IKernelRuntime kernel,
        IPacketDispatcher packetDispatcher,
        IScriptEventBus eventBus,
        IGlobalSettings globalSettings)
    { ... }
}
```

---

## Static Field Temizliği

`GameSession` içindeki `_skillEventsRegistered` ve `_clientlessEventsRegistered` alanları **static olmamalı**, instance-level `bool` olmalı.

**Yanlış (önceki durum):**
```csharp
public class GameSession : IGameSession
{
    private static bool _skillEventsRegistered;       // ❌ static — iki instance arasında leak
    private static bool _clientlessEventsRegistered;  // ❌ static
}
```

**Doğru:**
```csharp
public class GameSession : IGameSession
{
    private bool _skillEventsRegistered;       // ✅ instance-level
    private bool _clientlessEventsRegistered;  // ✅ instance-level
}
```

> **Gerekçe:** DI container Singleton olarak yönetse bile, static field kullanımı test izolasyonunu kırar ve ilerleyen fazlarda multi-instance senaryolarını engeller.

---

## Migration Strategy

### Phase 1: High-Frequency Core (Week 1)

**Interfaces (Abstractions):**
1. `IGameSession.cs`
2. `IKernelRuntime.cs`
3. `IGlobalSettings.cs`
4. `IPlayerSettings.cs` ← Phase 2'den buraya taşındı
5. `IPacketDispatcher.cs`
6. `IScriptEventBus.cs`

**Implementations (Core):**
7. `GlobalSettings.cs`
8. `PlayerSettings.cs`
9. `KernelRuntime.cs`
10. `GameSession.cs` (static field'lar instance'a çevrilmiş)
11. `PacketDispatcher.cs` (IDisposable)
12. `ScriptEventBus.cs` (IDisposable)

**Bootstrap:**
13. `ServiceProviderFactory.cs` (Dispose dahil)
14. `GameServiceCollectionExtensions.cs` (IPlayerSettings kaydı dahil)
15. Entry point bağlantısı (`Program.cs` veya `App.xaml.cs`)

**Refactored Classes:**
16. `CoreGameStateRuntimeContext.cs`
17. `ClientManager.cs`
18. `ClientlessManager.cs`

### Phase 2: Secondary Managers (Week 2)

1. `SkillManager.cs` refactor → constructor injection
2. `ShoppingManager.cs`, `ProfileManager.cs` refactor
3. Diğer manager'ların constructor injection geçişi

### Phase 3: Protocol Services (Week 3)

1. `ProtocolServices.cs` refactor
2. Tüm packet handler'lar `IPacketDispatcher` üzerinden
3. `ProtocolLegacy` 72 dosya → `UBot.Protocol` namespace geçişi  
   *(Not: Bu taşıma sırasında yeni interface'ler kullanılacak; eski static çağrılar bırakılmayacak)*

### Phase 4: Remaining Refactors (Week 4)

1. Event subscriber'ları `IScriptEventBus` üzerinden
2. Kalan static referansların temizliği (aşağıdaki arama listesine bakın)
3. Unit testlerin yazılması

### Phase 5: Static Façades Kaldırma (Post-stabilization)

**Phase 5 öncesi — tüm call-site'lar şu pattern'ler aranarak temizlenir:**

| Aranacak | Değiştirilecek |
|----------|---------------|
| `Game.` | `_gameSession.` |
| `Kernel.` | `_kernelRuntime.` |
| `GlobalConfig.` | `_globalSettings.` |
| `PlayerConfig.` | `_playerSettings.` |
| `PacketManager.` | `_packetDispatcher.` |
| `EventManager.` | `_eventBus.` |
| `ServiceRuntime.Get` | Constructor injection |

**Silinecek dosyalar (arama temizlendikten sonra):**
- `Game.cs` (static)
- `Kernel.cs` (static)
- `GlobalConfig.cs` (static)
- `PlayerConfig.cs` (static)
- `ServiceRuntime.cs`

---

## ProtocolLegacy Koordinasyon Notu

`ProtocolLegacy` altındaki 72 dosya Phase 3'te `UBot.Protocol` namespace'ine taşınacak. Bu taşıma sırasında:

- Dosyalar taşınırken eski `Game.`, `Kernel.` static çağrıları **aynı anda** yeni interface'lere güncellenmeli.
- `CoreLegacyProtocolHandler` stub'a indirilmeli, Phase 3 bitmeden silinmemeli.
- Phase 3 tamamlanmadan Phase 5 başlatılmamalı — aksi hâlde `ProtocolLegacy` dosyaları eski static'lere bağımlı kalır.

---

## Dependency Graph

```
IGameSession
  ├─ IKernelRuntime
  ├─ IGlobalSettings
  ├─ IPlayerSettings
  ├─ IPacketDispatcher
  └─ IScriptEventBus

IKernelRuntime
  └─ IGlobalSettings

CoreGameStateRuntimeContext
  ├─ IGameSession
  ├─ IKernelRuntime
  ├─ IGlobalSettings
  └─ IPlayerSettings

ClientManager
  ├─ IClientLaunchPolicy
  ├─ IClientNativeRuntime
  └─ IClientLaunchConfigProvider

ProtocolServices
  ├─ IGameSession
  ├─ IKernelRuntime
  ├─ IPacketDispatcher
  ├─ IScriptEventBus
  └─ IGlobalSettings
```

---

## Service Lifetimes

Tüm core servisler: **Singleton**

- Game state uygulama ömrü boyunca globaldir.
- Kernel, config, packet dispatcher, event bus singleton-scoped olmalı.
- Singleton seçimi allocation overhead'ı sıfırlar.
- `IDisposable` implementasyonları `ServiceProvider.Dispose()` ile otomatik tetiklenir.

---

## Error Handling & Edge Cases

**Uninitialized Services:**
- DI container eksik dependency'de anında hata fırlatır → fail fast.
- Sessiz null referanslardan daha güvenli.

**Concurrent Access:**
- `PacketManager`, `EventManager` içindeki mevcut lock'lar implementasyonlarda korunur.
- Ek senkronizasyon gerekmez.

**Static Fields in Implementations:**
- `GameSession` içindeki `_skillEventsRegistered`, `_clientlessEventsRegistered` → instance `bool`'a dönüştürülür (yukarıya bakın).
- Interface üzerinden dışarı açılmaz.

---

## Testing Strategy

```csharp
[Test]
public void GameSession_Start_InitializesProxy()
{
    // Arrange
    var mockKernel = new Mock<IKernelRuntime>();
    var mockSettings = new Mock<IGlobalSettings>();
    var mockPlayerSettings = new Mock<IPlayerSettings>();
    var mockDispatcher = new Mock<IPacketDispatcher>();
    var mockEventBus = new Mock<IScriptEventBus>();

    var game = new GameSession(
        mockKernel.Object,
        mockSettings.Object,
        mockPlayerSettings.Object,
        mockDispatcher.Object,
        mockEventBus.Object);

    // Act
    game.Start();

    // Assert
    Assert.That(game.Started, Is.True);
    mockKernel.Verify(k => k.Proxy, Times.AtLeastOnce);
}
```

---

## Scope & Constraints

**In Scope:**
- 7 core static sınıfın interface-based instance'a dönüştürülmesi
- Bootstrap altyapısı (composition root + IDisposable + entry point bağlantısı)
- Phase 1 high-frequency sınıfların refactor'u
- Tüm `using` ve namespace güncellemeleri
- Çalışır tam kod (açıklama değil)

**Out of Scope:**
- Tüm codebase'in aynı anda refactor edilmesi
- Test framework değişiklikleri (unit testler refactor sonrası eklenebilir)
- UI/Avalonia katmanı değişiklikleri (DI ihtiyaç duyuldukça uygulanır)
- Database veya persistence katmanı

**Assumptions:**
- `Microsoft.Extensions.DependencyInjection` kabul edilebilir
- Singleton lifetime core servisler için uygundur
- Geçiş sürecinde public API'lerde breaking change olmaz
- Composition root bootstrap entry point'te yapılır

---

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Bootstrap runtime'da bulunamaz | ServiceProviderFactory'de tek merkez, net isimlendirme |
| Circular dependencies | Interface'ler cycle engellenecek şekilde tasarlandı; composition root kullanılıyor |
| Plugin uyumluluk sorunları | Refactoring aşamalı; breaking change'ler belgeleniyor |
| Performance regression | Singleton lifetime = allocation overhead yok; gerekirse ölçülür |
| ProtocolLegacy ile çakışma | Phase 3 bitmeden Phase 5 başlatılmaz; koordinasyon notu eklendi |
| IDisposable unutulması | ServiceProvider.Dispose() entry point'te ApplicationExit'e bağlanır |

---

## Success Criteria

- [ ] 7 core static sınıf interface-based instance'a dönüştürüldü  
- [ ] Composition root çalışır ve entry point'e bağlandı  
- [ ] `IDisposable` implementasyonları ve dispose çağrısı entry point'te mevcut  
- [ ] `IPlayerSettings` Phase 1'de tamamlandı  
- [ ] `GameSession` içinde static field kalmadı  
- [ ] `GameServiceCollectionExtensions` içinde `IPlayerSettings` kaydı mevcut  
- [ ] High-frequency sınıflar refactor edildi ve inject edildi  
- [ ] Yeni kodda sıfır `ServiceRuntime` static locator referansı  
- [ ] Phase 5 öncesi call-site arama listesi tamamlandı  
- [ ] ProtocolLegacy taşıması Phase 3'te yeni interface'lerle koordineli yapıldı  
- [ ] Refactor edilmiş kodda sıfır `Game.`, `Kernel.`, `GlobalConfig.` static çağrısı  
