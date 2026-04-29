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
}
