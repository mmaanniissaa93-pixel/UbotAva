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

        UBot.Core.RuntimeAccess.Services.GameState = gameState;
        UBot.Core.RuntimeAccess.Services.PacketDispatcher = packetDispatcher;
        UBot.Core.RuntimeAccess.Services.Environment = Get<IServiceRuntimeEnvironment>(provider) ?? new CoreServiceRuntimeEnvironment();
        UBot.Core.RuntimeAccess.Services.Log = Get<IServiceLog>(provider) ?? new CoreServiceLog();
        UBot.Core.RuntimeAccess.Services.PickupRuntime = Get<IPickupRuntime>(provider) ?? new CorePickupRuntime();
        UBot.Core.RuntimeAccess.Services.PickupSettings = Get<IPickupSettings>(provider) ?? new CorePickupSettings();
        UBot.Core.RuntimeAccess.Services.InventoryRuntime = Get<IInventoryRuntime>(provider) ?? new CoreInventoryRuntime();
        UBot.Core.RuntimeAccess.Services.ShoppingRuntime = Get<IShoppingRuntime>(provider) ?? new CoreShoppingRuntime();
        UBot.Core.RuntimeAccess.Services.AlchemyRuntime = Get<IAlchemyRuntime>(provider) ?? new CoreAlchemyRuntime();
        UBot.Core.RuntimeAccess.Services.AlchemyProgress = new CoreAlchemyProgress(feedback);
        UBot.Core.RuntimeAccess.Services.ScriptRuntime = Get<IScriptRuntime>(provider) ?? new CoreScriptRuntime();
        UBot.Core.RuntimeAccess.Services.ScriptProgress = new CoreScriptProgress(feedback);
        UBot.Core.RuntimeAccess.Services.SpawnRuntime = Get<ISpawnRuntime>(provider) ?? new CoreSpawnRuntime();
        UBot.Core.RuntimeAccess.Services.SkillRuntime = Get<ISkillRuntime>(provider) ?? new SkillRuntimeAdapter();
        UBot.Core.RuntimeAccess.Services.SkillConfig = Get<ISkillConfig>(provider) ?? new CoreSkillConfig();
        UBot.Core.RuntimeAccess.Services.ClientConnectionRuntime = Get<IClientConnectionRuntime>(provider) ?? new ClientConnectionRuntimeAdapter();
        UBot.Core.RuntimeAccess.Services.Clientless = Get<IClientlessService>(provider) ?? new ClientlessService();
        UBot.Core.RuntimeAccess.Services.ClientNativeRuntime = Get<IClientNativeRuntime>(provider) ?? new ClientNativeRuntimeAdapter();
        UBot.Core.RuntimeAccess.Services.ClientLaunchConfigProvider = Get<IClientLaunchConfigProvider>(provider) ?? new CoreClientLaunchConfigProvider();
        UBot.Core.RuntimeAccess.Services.ClientLaunchPolicy = Get<IClientLaunchPolicy>(provider) ?? new ClientLaunchPolicyService();
        UBot.Core.RuntimeAccess.Services.ProfileStorage = Get<IProfileStorage>(provider) ?? new ProfileFileStorage(new CoreAppPaths());
        UBot.Core.RuntimeAccess.Services.Profile = Get<IProfileService>(provider) ?? new ProfileService();

        PickupManager.Initialize(Get<IPickupService>(provider) ?? new PickupService());
        ShoppingManager.Initialize(Get<IShoppingService>(provider) ?? new ShoppingService());
        AlchemyManager.Initialize(Get<IAlchemyService>(provider) ?? new AlchemyService());
        LanguageManager.Initialize(Get<ILanguageService>(provider) ?? new LanguageService());
        ClientManager.Initialize(UBot.Core.RuntimeAccess.Services.ClientLaunchPolicy);
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
