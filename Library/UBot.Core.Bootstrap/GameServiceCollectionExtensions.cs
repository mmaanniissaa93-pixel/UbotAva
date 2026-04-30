using Microsoft.Extensions.DependencyInjection;
using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;
using UBot.Core.Components;
using UBot.Core.Event;
using UBot.Core.Network;
using UBot.Core.Runtime;

namespace UBot.Core.Bootstrap;

public static class GameServiceCollectionExtensions
{
    public static IServiceCollection AddGameRuntime(this IServiceCollection services)
    {
        services.AddSingleton<IGlobalSettings, GlobalSettings>();
        services.AddSingleton<IPlayerSettings, PlayerSettings>();
        services.AddSingleton<IKernelRuntime, KernelRuntime>();
        services.AddSingleton<IGameSession>(_ => GameSession.Shared);
        services.AddSingleton<IPacketDispatcher, PacketDispatcher>();
        services.AddSingleton<IScriptEventBus, ScriptEventBus>();

        services.AddSingleton<IGameStateRuntimeContext>(provider =>
            CoreRuntimeBootstrapper.CreateGameStateRuntimeContext(provider));

        services.AddSingleton<IClientLaunchPolicy, ClientLaunchPolicyService>();
        services.AddSingleton<IClientlessService, ClientlessService>();
        services.AddSingleton<IPickupService, PickupService>();
        services.AddSingleton<IShoppingService, ShoppingService>();
        services.AddSingleton<IAlchemyService, AlchemyService>();
        services.AddSingleton<ILanguageService, LanguageService>();
        services.AddSingleton<IProfileService, ProfileService>();
        services.AddSingleton<UBot.Protocol.Services.ProtocolServices>();

        return services;
    }
}
