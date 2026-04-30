using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;

namespace UBot.Core.Services;

public static class ServiceRuntime
{
    public static IGameStateRuntimeContext GameState { get; set; }
    public static IPacketDispatcher PacketDispatcher { get; set; }
    public static IServiceRuntimeEnvironment Environment { get; set; }
    public static IServiceLog Log { get; set; }
    public static IPickupRuntime PickupRuntime { get; set; }
    public static IPickupSettings PickupSettings { get; set; }
    public static IPickupService Pickup { get; set; }
    public static IInventoryRuntime InventoryRuntime { get; set; }
    public static IShoppingRuntime ShoppingRuntime { get; set; }
    public static IShoppingService Shopping { get; set; }
    public static IAlchemyRuntime AlchemyRuntime { get; set; }
    public static IAlchemyProgress AlchemyProgress { get; set; }
    public static IAlchemyService Alchemy { get; set; }
    public static IScriptRuntime ScriptRuntime { get; set; }
    public static IScriptProgress ScriptProgress { get; set; }
    public static ISpawnRuntime SpawnRuntime { get; set; }
    public static ILanguageService Language { get; set; }
    public static ISkillRuntime SkillRuntime { get; set; }
    public static ISkillConfig SkillConfig { get; set; }
    public static ISkillService Skill { get; set; }
    public static IClientConnectionRuntime ClientConnectionRuntime { get; set; }
    public static IClientlessService Clientless { get; set; }
    public static IProfileStorage ProfileStorage { get; set; }
    public static IProfileService Profile { get; set; }
}
