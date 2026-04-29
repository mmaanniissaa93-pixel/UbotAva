using UBot.Core.Abstractions;
using UBot.Core.Abstractions.Network;
using UBot.Core.Abstractions.Services;

namespace UBot.Protocol;

public static class ProtocolRuntime
{
    public static IGameStateRuntimeContext GameState { get; set; }
    public static IPacketDispatcher PacketDispatcher { get; set; }
    public static IProtocolLegacyHandler LegacyHandler { get; set; }
    public static ISpawnController SpawnController { get; set; }
    public static IScriptEventBus EventBus { get; set; }
    public static IUIFeedbackService Feedback { get; set; }
    public static IShoppingController Shopping { get; set; }
    public static ICosController Cos { get; set; }

    public static void Dispatch(UBot.Core.Network.Packet packet, UBot.Core.Network.PacketDestination destination)
    {
        PacketDispatcher?.Dispatch(packet, destination);
    }
}
