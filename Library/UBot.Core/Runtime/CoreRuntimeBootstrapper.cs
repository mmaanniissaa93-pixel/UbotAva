using System;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.IO;
using UBot.Core.Objects;
using UBot.Core.ProtocolServices;
using UBot.Core.Services;
using UBot.Protocol;

namespace UBot.Core.Runtime;

public static class CoreRuntimeBootstrapper
{
    public static void Initialize(IServiceProvider provider = null)
    {
        var eventBus = Get<IScriptEventBus>(provider) ?? new CoreScriptEventBus();
        var feedback = new CoreUIFeedbackService();
        var packetDispatcher = Get<IPacketDispatcher>(provider) ?? new Network.PacketDispatcher();
        var gameState = Get<IGameStateRuntimeContext>(provider)
            ?? CreateGameStateRuntimeContext(provider);

        GameStateRuntimeProvider.Instance = gameState;

        ProtocolRuntime.GameState = gameState;
        ProtocolRuntime.PacketDispatcher = packetDispatcher;
        ProtocolRuntime.LegacyRuntime = new CoreProtocolLegacyRuntime();
        ProtocolRuntime.EventBus = eventBus;
        ProtocolRuntime.Feedback = feedback;
        ProtocolRuntime.SpawnController = new CoreSpawnController(eventBus, feedback);
        ProtocolRuntime.Shopping = new CoreShoppingController();
        ProtocolRuntime.Cos = new CoreCosController();

        ServiceRuntime.GameState = gameState;
        ServiceRuntime.PacketDispatcher = packetDispatcher;
        ServiceRuntime.Environment = Get<IServiceRuntimeEnvironment>(provider) ?? new CoreServiceRuntimeEnvironment();
        ServiceRuntime.Log = Get<IServiceLog>(provider) ?? new CoreServiceLog();
        ServiceRuntime.PickupRuntime = Get<IPickupRuntime>(provider) ?? new CorePickupRuntime();
        ServiceRuntime.PickupSettings = Get<IPickupSettings>(provider) ?? new CorePickupSettings();
        ServiceRuntime.InventoryRuntime = Get<IInventoryRuntime>(provider) ?? new CoreInventoryRuntime();
        ServiceRuntime.ShoppingRuntime = Get<IShoppingRuntime>(provider) ?? new CoreShoppingRuntime();
        ServiceRuntime.AlchemyRuntime = Get<IAlchemyRuntime>(provider) ?? new CoreAlchemyRuntime();
        ServiceRuntime.AlchemyProgress = new CoreAlchemyProgress(feedback);
        ServiceRuntime.ScriptRuntime = Get<IScriptRuntime>(provider) ?? new CoreScriptRuntime();
        ServiceRuntime.ScriptProgress = new CoreScriptProgress(feedback);
        ServiceRuntime.SpawnRuntime = Get<ISpawnRuntime>(provider) ?? new CoreSpawnRuntime();
        ServiceRuntime.SkillRuntime = Get<ISkillRuntime>(provider) ?? new SkillRuntimeAdapter();
        ServiceRuntime.SkillConfig = Get<ISkillConfig>(provider) ?? new CoreSkillConfig();
        ServiceRuntime.ClientConnectionRuntime = Get<IClientConnectionRuntime>(provider) ?? new ClientConnectionRuntimeAdapter();
        ServiceRuntime.Clientless = Get<IClientlessService>(provider) ?? new ClientlessService();
        ServiceRuntime.ClientNativeRuntime = Get<IClientNativeRuntime>(provider) ?? new ClientNativeRuntimeAdapter();
        ServiceRuntime.ClientLaunchConfigProvider = Get<IClientLaunchConfigProvider>(provider) ?? new CoreClientLaunchConfigProvider();
        ServiceRuntime.ClientLaunchPolicy = Get<IClientLaunchPolicy>(provider) ?? new ClientLaunchPolicyService();
        ServiceRuntime.ProfileStorage = Get<IProfileStorage>(provider) ?? new ProfileFileStorage(new CoreAppPaths());
        ServiceRuntime.Profile = Get<IProfileService>(provider) ?? new ProfileService();

        PickupManager.Initialize(Get<IPickupService>(provider) ?? new PickupService());
        ShoppingManager.Initialize(Get<IShoppingService>(provider) ?? new ShoppingService());
        AlchemyManager.Initialize(Get<IAlchemyService>(provider) ?? new AlchemyService());
        LanguageManager.Initialize(Get<ILanguageService>(provider) ?? new LanguageService());
        ClientManager.Initialize(ServiceRuntime.ClientLaunchPolicy);
    }

    public static IGameStateRuntimeContext CreateGameStateRuntimeContext(IServiceProvider provider = null)
    {
        return new CoreGameStateRuntimeContext(
            Get<IGameSession>(provider) ?? GameSession.Shared,
            Get<IKernelRuntime>(provider) ?? new KernelRuntime(),
            Get<IGlobalSettings>(provider) ?? new GlobalSettings(),
            Get<IPlayerSettings>(provider) ?? new PlayerSettings(),
            Get<IPacketDispatcher>(provider) ?? new Network.PacketDispatcher(),
            Get<IScriptEventBus>(provider) ?? new Event.ScriptEventBus());
    }

    private static T Get<T>(IServiceProvider provider)
        where T : class
    {
        return provider?.GetService(typeof(T)) as T;
    }
}
